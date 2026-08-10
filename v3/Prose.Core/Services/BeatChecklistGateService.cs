using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

/// <summary>
/// Reader-Proxy QA Instrument 2 — the binary craft/delight checklist (docs/READER-QA.md).
///
/// <para>Replaces holistic 0-100 opinion scores with reproducible binary checks per
/// beat, evaluated in ONE cheap LLM call per beat: (a) does the beat violate any
/// CRAFT.md §8 banned mannerism (literal per-beat binaries — the DON'Ts), and
/// (b) does the beat land at least one DELIGHT move APPLICABLE TO ITS JOB (the DOs).</para>
///
/// <para><b>The DELIGHT §14 guard (a palette, not a stamp):</b> DELIGHT.md itself
/// documents a real regression (SPRW 2026-07-20) where uniformly applying all 13
/// moves per beat became a new tic. So the per-beat DO-check is only "≥1 applicable
/// move present" (a short transition beat is exempt), and monotony is checked at
/// BOOK level as a deterministic counter: one move carrying too much of the book is
/// the defect, never "beat N didn't do all 13".</para>
///
/// <para>Rules parse LIVE from CanonDocumentSections (CraftGuide SS-CRAFT-8 +
/// DelightGuide SS-DELIGHT-1..13) — edit the docs, the next run checks the new text.
/// Cache: <see cref="BeatChecklistResult"/> keyed on Beat.TextHash + rule-set hash;
/// unchanged beat + unchanged rules = no LLM call. Findings
/// (<see cref="FindingCategory.CraftChecklist"/>) auto-supersede per run. Emits no
/// scores — a measurement, not a vote (SS-A44 exempt).</para>
/// </summary>
public sealed class BeatChecklistGateService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    FindingsService findings,
    SettingsService settings,
    VerificationContextService verificationContext,
    ILogger<BeatChecklistGateService> log)
{
    private const string FindingSummaryPrefix = "CHECKLIST";
    /// <summary>Bump when the evaluation prompt changes shape — invalidates the cache.
    /// v2 (2026-08-10): added the POV voice-register guard — see <see cref="BuildPovVoiceGuidance"/>.</summary>
    private const string PromptVersion = "v2";

    /// <summary>Parses CRAFT.md §8's numbered "N. **Title** — description" list format.
    /// Ported from CraftRuleAuditService (2026-08-08 consolidation) — this is now the sole
    /// parser for that section; edit §8 via set_canon_section MCP and the next
    /// --craft-checklist run picks up the new wording.</summary>
    static readonly Regex MannerismPattern = new(
        @"(?:^|\n)\d+\.\s+\*\*(?<title>.+?)\*\*\s*[—-]+\s*(?<desc>.*?)(?=\n\d+\.\s+\*\*|\z)",
        RegexOptions.Singleline);

    internal static IReadOnlyList<(int Number, string Title, string Description)> ParseMannerisms(string sectionContent)
    {
        var results = new List<(int, string, string)>();
        foreach (Match m in MannerismPattern.Matches(sectionContent))
        {
            var numberText = m.Value.TrimStart('\n');
            var number = int.Parse(numberText[..numberText.IndexOf('.')]);
            var title = m.Groups["title"].Value.Trim();
            var desc = Regex.Replace(m.Groups["desc"].Value, @"\s+", " ").Trim();
            results.Add((number, title, desc));
        }
        return results;
    }
    /// <summary>Beats shorter than this are exempt from the "≥1 delight move" check —
    /// a two-line transition has no job that demands a move.</summary>
    private const int DelightExemptWordCount = 120;
    /// <summary>Book-level monotony threshold (DELIGHT §14): one move carrying more
    /// than this fraction of move-landing beats is a stamp, not a palette.</summary>
    private const double MonotonyFraction = 0.40;

    public sealed record BeatVerdict(
        Guid BeatId, int BeatNumber, double PassFraction, bool FromCache,
        IReadOnlyList<DontViolation> DontViolations, IReadOnlyList<string> MovesLanded,
        string BeatJob, int WordCount);

    public sealed record DontViolation(string Key, string Title, string Evidence);

    public sealed record ChecklistRunResult(
        Guid NodeId, string Slug, string Title, string RuleSetVersion,
        IReadOnlyList<BeatVerdict> Beats,
        IReadOnlyList<string> BookLevelFindings,
        int Evaluated, int FromCache, int FindingsFiled);

    public async Task<ChecklistRunResult> RunAsync(Guid nodeId, bool force = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // Book nodes hold the live manuscript on chapter children (same convention
        // as CraftRuleAuditService / BookAuditService). Recurses past any nested Collection
        // (2026-08-09 fix); returns leaves in reading order, which chapterOrder below relies on.
        var sourceIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

        var beatRows = await db.BeatNodes.AsNoTracking()
            .Where(bn => sourceIds.Contains(bn.NodeId) && bn.IsEnabled && bn.Beat != null && bn.Beat.Text != "")
            .Select(bn => new { bn.NodeId, bn.SortKey, bn.Beat!.Id, bn.Beat.Number, bn.Beat.Text, bn.Beat.TextHash })
            .ToListAsync(ct);
        // Chapter order is positional in sourceIds — EF can't translate IndexOf, so order client-side.
        var chapterOrder = sourceIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var beats = beatRows.OrderBy(b => chapterOrder[b.NodeId]).ThenBy(b => b.SortKey).ToList();
        if (beats.Count == 0)
            return new ChecklistRunResult(nodeId, node.Slug ?? "", node.Title, "", Array.Empty<BeatVerdict>(),
                Array.Empty<string>(), 0, 0, 0);

        // ── rules, parsed live from the canon DB ──────────────────────────────────
        var (donts, moves, ruleSetVersion) = await LoadRulesAsync(db, ct);

        // ── per-beat evaluation, hash-gated ───────────────────────────────────────
        var cachedRows = await db.BeatChecklistResults
            .Where(r => r.NodeId == nodeId).ToDictionaryAsync(r => r.BeatId, ct);

        var verdicts = new List<BeatVerdict>();
        var povVoiceCache = new Dictionary<Guid, string?>();
        int evaluated = 0, fromCache = 0;
        foreach (var beat in beats)
        {
            ct.ThrowIfCancellationRequested();
            var textHash = beat.TextHash ?? Beat.ComputeHash(beat.Text);

            if (!force && cachedRows.TryGetValue(beat.Id, out var row)
                && string.Equals(row.BeatTextHash, textHash, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.RuleSetVersion, ruleSetVersion, StringComparison.Ordinal))
            {
                verdicts.Add(FromRow(row, beat.Number, fromCacheFlag: true));
                fromCache++;
                continue;
            }

            var povVoiceHint = await verificationContext.GetPovVoiceHintAsync(beat.Id, povVoiceCache, ct);
            var (verdict, parseFailed) = await EvaluateBeatAsync(beat.Id, beat.Number, beat.Text, donts, moves, povVoiceHint, ct);
            evaluated++;

            // A truncated/non-JSON response is a degraded placeholder for THIS run only — caching
            // it under the current text hash would make the "will re-evaluate next run" promise a
            // lie, since an unchanged beat would then hit the cache and never be re-asked.
            if (!parseFailed)
            {
                var json = JsonSerializer.Serialize(new
                {
                    dontViolations = verdict.DontViolations,
                    delightMovesLanded = verdict.MovesLanded,
                    beatJob = verdict.BeatJob,
                    wordCount = verdict.WordCount,
                });
                if (cachedRows.TryGetValue(beat.Id, out var existing))
                {
                    existing.BeatTextHash = textHash;
                    existing.RuleSetVersion = ruleSetVersion;
                    existing.ResultsJson = json;
                    existing.PassFraction = verdict.PassFraction;
                    existing.EvaluatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.BeatChecklistResults.Add(new BeatChecklistResult
                    {
                        Id = Guid.CreateVersion7(),
                        BeatId = beat.Id,
                        NodeId = nodeId,
                        BeatTextHash = textHash,
                        RuleSetVersion = ruleSetVersion,
                        ResultsJson = json,
                        PassFraction = verdict.PassFraction,
                    });
                }
                await db.SaveChangesAsync(ct);
            }
            verdicts.Add(verdict);
        }

        // ── deterministic corpus-rate checks (ported from CraftRuleAuditService,
        // 2026-08-08 — a per-mannerism LLM rule over a whole-node blob can't see a rate
        // defect like "298 italics across 91 beats"; these two count instead of ask) ──
        var auditBeats = beats.Select(b => new AuditBeat(b.Id, b.Number, b.Text, b.SortKey)).ToList();
        var detCtx = new AuditContext(nodeId, node.UniverseId, "", auditBeats, new Dictionary<string, object?>());
        var interiorityVerdicts = await new InteriorityDensityRule().EvaluateAsync(detCtx, ct);
        var retiredTicVerdicts = await new RetiredTicRule().EvaluateAsync(detCtx, ct);
        var deterministicVerdicts = interiorityVerdicts.Concat(retiredTicVerdicts).ToList();

        // ── book-level monotony counters (DELIGHT §14 — deterministic, no LLM) ────
        var bookFindings = new List<string>();
        var landing = verdicts.Where(v => v.MovesLanded.Count > 0).ToList();
        if (verdicts.Count >= 20 && landing.Count >= 10)
        {
            var byMove = landing.SelectMany(v => v.MovesLanded)
                .GroupBy(m => m, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count());
            foreach (var g in byMove)
            {
                var share = (double)g.Count() / landing.Count;
                if (share > MonotonyFraction)
                {
                    var title = moves.FirstOrDefault(m => m.Key.Equals(g.Key, StringComparison.OrdinalIgnoreCase))?.Title ?? g.Key;
                    bookFindings.Add(
                        $"Move monotony (DELIGHT §14): '{title}' carries {g.Count()}/{landing.Count} move-landing beats " +
                        $"({share:P0}) — a stamp, not a palette. Vary which move does the work.");
                }
            }
        }

        // ── findings: delete-then-recreate per node run ───────────────────────────
        var filePathPrefix = $"node:{node.Slug}";
        findings.DeleteBySummaryPrefix(filePathPrefix, FindingSummaryPrefix);
        int filed = 0;
        foreach (var v in verdicts.Where(v => v.DontViolations.Count > 0))
        {
            var list = string.Join("; ", v.DontViolations.Select(d => $"{d.Title}: {d.Evidence}"));
            findings.Upsert(
                $"{filePathPrefix}/beat:{v.BeatId:N}", chapterId: null, FindingCategory.CraftChecklist,
                v.DontViolations.Count >= 2 ? FindingSeverity.Medium : FindingSeverity.Low,
                $"{FindingSummaryPrefix} beat #{v.BeatNumber}: {v.DontViolations.Count} banned-mannerism hit(s) — {Truncate(list, 300)}",
                snippet: v.DontViolations[0].Evidence,
                suggestedFix: "Rewrite the flagged phrasing per CRAFT.md §8; if the hit is literal/diegetic, dismiss.",
                sourceRuleVersion: ruleSetVersion);
            filed++;
        }
        // Flat beats (no DELIGHT move landed) are REPORTED but deliberately NOT filed as
        // findings. Calibration lesson (BCODA 2026-08-03): the cheap judge marks "landed"
        // conservatively — 448/507 beats filed as flat, drowning the inbox in Low noise
        // while the book's real corpus standing says otherwise. Zero-moves-landed only
        // carries signal in aggregate (the monotony counters below and the flat-rate in
        // the run report); per-beat it violates "fix what a finding names".
        foreach (var bf in bookFindings)
        {
            findings.Upsert(filePathPrefix, chapterId: null, FindingCategory.CraftChecklist,
                FindingSeverity.Medium, $"{FindingSummaryPrefix} {bf}", snippet: null, suggestedFix: null,
                sourceRuleVersion: ruleSetVersion);
            filed++;
        }
        // Deterministic corpus-rate findings (interiority density, retired tics) — same
        // FindingSummaryPrefix, so they're covered by the DeleteBySummaryPrefix call above
        // and auto-supersede on the next run just like the LLM-judged findings.
        foreach (var v in deterministicVerdicts)
        {
            var sev = v.Severity == "MODERATE" ? FindingSeverity.Medium : FindingSeverity.Low;
            var path = v.Location != null ? $"{filePathPrefix}/beat:{v.Location}" : filePathPrefix;
            findings.Upsert(path, chapterId: null, FindingCategory.CraftChecklist, sev,
                $"{FindingSummaryPrefix} {v.Title}: {v.Evidence}", snippet: null, suggestedFix: v.Fix,
                sourceRuleVersion: ruleSetVersion);
            filed++;
        }

        log.LogInformation("Checklist {Slug}: {Evaluated} evaluated, {Cached} cached, {Filed} finding(s).",
            node.Slug, evaluated, fromCache, filed);

        return new ChecklistRunResult(nodeId, node.Slug ?? "", node.Title, ruleSetVersion,
            verdicts, bookFindings, evaluated, fromCache, filed);
    }

    // ── deterministic corpus-rate rules (ported from CraftRuleAuditService 2026-08-08,
    // consolidating both CRAFT.md §8 checkers into this one service) ──────────────────

    /// <summary>
    /// CRAFT §2 / §8.6 — the italic-thought crutch, checked by counting instead of by opinion.
    ///
    /// §2 budgets interiority at "one or two flat lines per scene" and calls italic inner
    /// monologue "a last resort — a single sentence, never a paragraph, never a crutch"; §8.6
    /// bans it outright. Those are countable claims, so no model is asked to judge them.
    ///
    /// <b>Why this is deterministic.</b> TRNY (2026-08-02) shipped as "publication ready" with
    /// 298 italic segments across 91 beats — 3.27 per beat, 84 of 91 beats over budget, one beat
    /// with six. It had passed three logic sweeps and a craft audit. A per-mannerism LLM rule is
    /// asked "does the manuscript do this?" and answers yes-or-no about a clamped blob; it has no
    /// way to say "yes, 298 times, which is 10x the rest of the corpus". A counter does.
    ///
    /// Thresholds are calibrated against the real corpus, not invented: every other book measured
    /// between 0.02 and 0.76 italic segments per beat (median well under 0.3). Above 1.0 per beat
    /// is outside anything else that has shipped, so that is MODERATE; a single beat carrying 3+
    /// is the per-scene "never a paragraph" clause and is cited individually as MINOR.
    /// </summary>
    internal sealed class InteriorityDensityRule : IDeterministicAuditRule
    {
        public string Key => "interiority_density";
        public string Title => "Interiority density (italic-thought crutch)";

        /// <summary>Single-asterisk spans only. Doubles are bold and are not interiority, so
        /// <c>**bold**</c> must not be counted — hence the negative look-around on both sides.</summary>
        static readonly Regex ItalicSpan = new(@"(?<!\*)\*(?!\*)([^*\n]+)\*(?!\*)", RegexOptions.Compiled);

        internal const double PerBeatCeiling = 1.0;
        internal const int SingleBeatCeiling = 3;

        internal static int CountItalics(string text) => ItalicSpan.Matches(text).Count;

        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var results = new List<AuditVerdict>();
            if (ctx.Beats.Count == 0) return Task.FromResult<IReadOnlyList<AuditVerdict>>(results);

            var counts = ctx.Beats.Select(b => (Beat: b, N: CountItalics(b.Text))).ToList();
            var total = counts.Sum(c => c.N);
            var perBeat = (double)total / ctx.Beats.Count;

            if (perBeat > PerBeatCeiling)
                results.Add(new AuditVerdict(Key, Title, "MODERATE",
                    $"{total} italic inner-monologue segments across {ctx.Beats.Count} beats " +
                    $"({perBeat:N2} per beat). CRAFT §2 budgets italics as a last resort; §8.6 bans them " +
                    $"as a recurring device. Every other book in the corpus measures under 0.8 per beat.",
                    Location: null,
                    Fix: "Keep at most the single strongest italic per beat; convert the rest to a physical " +
                         "action, to plain free-indirect narration without asterisks, or delete where the " +
                         "surrounding prose already shows it."));

            foreach (var (beat, n) in counts.Where(c => c.N >= SingleBeatCeiling).OrderByDescending(c => c.N))
                results.Add(new AuditVerdict(Key, Title, "MINOR",
                    $"Beat #{beat.Number} carries {n} italic inner-monologue segments in one scene.",
                    Location: beat.Id.ToString(),
                    Fix: "Reduce to one at most; italics are never a paragraph and never a running device."));

            return Task.FromResult<IReadOnlyList<AuditVerdict>>(results);
        }
    }

    /// <summary>
    /// CRAFT §8.2 / §8.3 — retired cognitive-architecture and observation tics, matched literally.
    ///
    /// §8.2 retires "the arithmetic" / "did the math" and any ledger-or-filing framing of how a
    /// character THINKS; §8.3 retires "noted / logged / catalogued / filed" as thought-verbs.
    /// These are named phrases, so they are matched as phrases rather than described to a model.
    ///
    /// <b>The distinction this rule must not get wrong.</b> Literal, diegetic bookkeeping is not a
    /// violation and is frequently the plot — a real ledger chained to a belt, a clerk doing sums,
    /// a toll-master counting coins. Only the metaphorical use (a mind "doing the arithmetic",
    /// a thought being "filed") is retired. The patterns below therefore anchor on a subject
    /// doing the thinking, and the surrounding sentence is always emitted as evidence so a human
    /// can overrule a diegetic hit rather than being told a number with no context. Severity is
    /// MINOR for that reason: this rule points, it does not convict.
    /// </summary>
    internal sealed class RetiredTicRule : IDeterministicAuditRule
    {
        public string Key => "retired_tics";
        public string Title => "Retired cognitive-architecture tics";

        static readonly Regex[] Tics =
        [
            // "the arithmetic" / "the math" only — deliberately NOT "the sum(s)". CRAFT §8.2 names
            // the first two; "did the sum" is overwhelmingly literal in this corpus (a clerk, a
            // quartermaster, a character actually adding a debt up) and flagging it produced a
            // verified false positive on TRNY Ch1's "old Ferrin did the sum twice".
            new(@"\b(?:do(?:es|ing)?|did)\s+the\s+(?:arithmetic|math)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bthe\s+arithmetic\s+of\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\b(?:arithmetical|arithmetic)\s+(?:clarity|knowledge|certainty|precision)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bfiled\s+(?:it|that|them|this)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\b(?:noted|logged|catalogued|cataloged)\s+and\s+filed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bfiled\s+(?:it\s+)?(?:away\s+)?(?:under|in\s+the\s+same\s+column)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bledger\s+in\s+(?:his|her|their|its)\s+head\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ];

        internal static IReadOnlyList<string> FindTics(string text) =>
            Tics.SelectMany(rx => rx.Matches(text).Select(m => Excerpt(text, m.Index, m.Length))).ToList();

        static string Excerpt(string text, int index, int length)
        {
            var start = Math.Max(0, index - 70);
            var end = Math.Min(text.Length, index + length + 70);
            return "…" + Regex.Replace(text[start..end], @"\s+", " ").Trim() + "…";
        }

        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var results = new List<AuditVerdict>();
            foreach (var beat in ctx.Beats)
                foreach (var hit in FindTics(beat.Text))
                    results.Add(new AuditVerdict(Key, Title, "MINOR",
                        $"Beat #{beat.Number}: retired cognitive tic (CRAFT §8.2/§8.3) — {hit}",
                        Location: beat.Id.ToString(),
                        Fix: "Replace with the plain statement or the physical action. If this is literal, " +
                             "diegetic bookkeeping (a real ledger, a clerk actually doing sums), it is not a " +
                             "violation — dismiss the finding."));
            return Task.FromResult<IReadOnlyList<AuditVerdict>>(results);
        }
    }

    // ── rule loading ───────────────────────────────────────────────────────────────

    /// <summary>
    /// RFC 0011 Brick 2: what a fresh <c>--craft-checklist</c> run right now would consider
    /// "current" — unlike <see cref="BeatVerificationService.CurrentRuleVersion"/>, this isn't a
    /// compile-time constant: it's a hash of the code's <see cref="PromptVersion"/> AND the live
    /// CRAFT.md/DELIGHT.md content in <c>CanonDocumentSections</c>, so an author editing craft
    /// docs (no deploy) changes what "current" means just as much as a code change would. Callers
    /// building a staleness report need this async lookup rather than a bare constant.
    /// </summary>
    public async Task<string> GetCurrentRuleSetVersionAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var (_, _, version) = await LoadRulesAsync(db, ct);
        return version;
    }

    public sealed record DelightMove(string Key, string Title, string Gist);

    private static async Task<(List<(string Key, string Title, string Desc)> Donts, List<DelightMove> Moves, string Version)>
        LoadRulesAsync(ProseDbContext db, CancellationToken ct)
    {
        var craftSection = await db.CanonDocumentSections.AsNoTracking()
            .Where(s => s.Document!.DocumentType == "CraftGuide" && s.SectionKey == "SS-CRAFT-8")
            .Select(s => s.Content).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("CRAFT.md §8 not found in CanonDocumentSections — run prose --migrate-canon-docs --type CraftGuide.");

        var donts = ParseMannerisms(craftSection)
            .Select(m => (Key: $"craft_{m.Number}", m.Title, Desc: m.Description)).ToList();
        if (donts.Count == 0)
            throw new InvalidOperationException("CRAFT.md §8 parsed to zero mannerisms — numbered-list format changed?");

        // DELIGHT moves 1-13. §14 ("vary the moves") is deliberately NOT a per-beat
        // check — it is the book-level monotony counter.
        var delightSections = await db.CanonDocumentSections.AsNoTracking()
            .Where(s => s.Document!.DocumentType == "DelightGuide" && s.SectionKey.StartsWith("SS-DELIGHT-"))
            .Select(s => new { s.SectionKey, s.SectionTitle, s.Content })
            .ToListAsync(ct);
        var moves = delightSections
            .Where(s => int.TryParse(s.SectionKey["SS-DELIGHT-".Length..], out var n) && n is >= 1 and <= 13)
            .OrderBy(s => int.Parse(s.SectionKey["SS-DELIGHT-".Length..]))
            .Select(s => new DelightMove(s.SectionKey, s.SectionTitle ?? s.SectionKey, Gist(s.Content)))
            .ToList();
        if (moves.Count == 0)
            throw new InvalidOperationException("DELIGHT.md moves not found in CanonDocumentSections (DelightGuide).");

        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            PromptVersion + craftSection + string.Join("|", moves.Select(m => m.Key + m.Gist)))))[..16];
        return (donts, moves, version);
    }

    /// <summary>First ~2 sentences of a move's section — enough for recognition,
    /// small enough that 13 moves fit one system prompt.</summary>
    private static string Gist(string content)
    {
        var text = Regex.Replace(content, @"\s+", " ").Trim();
        var cut = 0; var sentences = 0;
        for (int i = 0; i < text.Length && sentences < 2; i++)
            if (text[i] is '.' or '!' or '?') { cut = i + 1; sentences++; }
        var gist = sentences >= 1 ? text[..cut] : text;
        return gist.Length > 320 ? gist[..320] + "…" : gist;
    }

    // ── per-beat evaluation ────────────────────────────────────────────────────────

    /// <summary>Pure so it can be unit-tested without a DB or LLM: the guidance block appended
    /// to Part A when this beat has a known POV voice, or empty when it doesn't.</summary>
    internal static string BuildPovVoiceGuidance(string? povVoiceHint) =>
        string.IsNullOrWhiteSpace(povVoiceHint) ? "" : $"""

            This beat's POV character has an established voice on file: {povVoiceHint}
            Phrasing that authentically reflects THIS character's own on-file vocabulary or
            cognitive style is NOT a violation, even if it resembles a banned mannerism in
            isolation — only flag phrasing that goes beyond their established register (a lazy
            repeated tic, not their consistent characterization).
            """;

    /// <summary>Returns the verdict plus whether the LLM response failed to parse (e.g. truncated
    /// JSON). Callers must NOT persist a failed-parse verdict to the cache — it is a degraded
    /// all-pass placeholder for this run only, not a real evaluation of the beat.</summary>
    private async Task<(BeatVerdict Verdict, bool ParseFailed)> EvaluateBeatAsync(
        Guid beatId, int beatNumber, string text,
        List<(string Key, string Title, string Desc)> donts, List<DelightMove> moves,
        string? povVoiceHint, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            You evaluate ONE beat (scene fragment) of a novel against binary craft checks.
            Answer mechanically — each check is yes/no with evidence, not an opinion score.

            PART A — BANNED MANNERISMS (the DON'Ts). For each, does this beat contain even
            one clear instance? Only flag CLEAR instances in THIS beat's text.
            """);
        sb.Append(BuildPovVoiceGuidance(povVoiceHint));
        sb.AppendLine();
        foreach (var d in donts) sb.AppendLine($"- {d.Key}: {d.Title} — {d.Desc}");
        sb.AppendLine();
        sb.AppendLine("""
            PART B — DELIGHT MOVES (the DOs). Which of these moves does the beat ACTUALLY
            LAND (not attempt — land)? A beat typically lands 0-2. Landing zero is normal
            for short connective beats. Do NOT stretch to find moves.
            """);
        foreach (var m in moves) sb.AppendLine($"- {m.Key}: {m.Title}. {m.Gist}");
        sb.AppendLine();
        sb.AppendLine("""
            Return STRICT JSON only, no markdown fence:
            {"beatJob":"one of: setpiece|two-hander|forensic|reveal|transition|grace-note|other",
             "dontViolations":[{"key":"craft_N","evidence":"short direct quote"}],
             "movesLanded":["SS-DELIGHT-N"]}
            """);

        // 8 DON'Ts + up to 13 possible movesLanded entries, each dontViolation carrying a quoted
        // evidence string — worst case (a beat that trips most checks) genuinely exceeds 800
        // tokens of JSON and gets cut off mid-string/array, not just under rare API flakiness.
        // 2400 gives real headroom without meaningfully changing per-beat cost on a Haiku-tier model.
        var raw = await llm.GenerateAsync(sb.ToString(), $"BEAT #{beatNumber}:\n\n{text}",
            temperature: 0.0, maxTokens: 2400, model: settings.ComprehensionProbeModel, ct: ct);
        raw = raw.Trim();

        var wordCount = Regex.Matches(text, @"\b\w+\b").Count;
        var violations = new List<DontViolation>();
        var landed = new List<string>();
        var job = "other";
        var parseFailed = false;
        try
        {
            // Haiku frequently ignores "STRICT JSON only" and appends free-text commentary
            // after a perfectly valid JSON object (observed empirically — not truncation:
            // response lengths are well under the token budget). JsonDocument.Parse rejects
            // any trailing content after a complete value, so extract just the balanced
            // {...} object rather than requiring the whole response to be pure JSON.
            var jsonSlice = ExtractJsonObject(raw) ?? raw;
            using var doc = JsonDocument.Parse(jsonSlice);
            var root = doc.RootElement;
            job = root.TryGetProperty("beatJob", out var j) ? j.GetString() ?? "other" : "other";
            if (root.TryGetProperty("dontViolations", out var dv) && dv.ValueKind == JsonValueKind.Array)
                foreach (var d in dv.EnumerateArray())
                {
                    var key = d.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                    var title = donts.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Title ?? key;
                    violations.Add(new DontViolation(key, title,
                        d.TryGetProperty("evidence", out var e) ? e.GetString() ?? "" : ""));
                }
            if (root.TryGetProperty("movesLanded", out var ml) && ml.ValueKind == JsonValueKind.Array)
                landed = ml.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString() ?? "").Where(s => s.StartsWith("SS-DELIGHT-")).ToList();
        }
        catch (JsonException)
        {
            parseFailed = true;
            log.LogWarning("Checklist beat #{Number}: non-JSON response (likely genuinely truncated mid-object) — treated as all-pass for this run, NOT cached, will re-evaluate next run.", beatNumber);
        }

        var totalChecks = donts.Count + 1; // DON'Ts + the "≥1 applicable move" DO
        var passed = donts.Count - violations.Count
                     + (landed.Count >= 1 || wordCount < DelightExemptWordCount ? 1 : 0);
        return (new BeatVerdict(beatId, beatNumber, Math.Round((double)passed / totalChecks, 4), false,
            violations, landed, job, wordCount), parseFailed);
    }

    private BeatVerdict FromRow(BeatChecklistResult row, int beatNumber, bool fromCacheFlag)
    {
        try
        {
            using var doc = JsonDocument.Parse(row.ResultsJson);
            var root = doc.RootElement;
            var violations = root.TryGetProperty("dontViolations", out var dv) && dv.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<List<DontViolation>>(dv.GetRawText()) ?? new() : new List<DontViolation>();
            var landed = root.TryGetProperty("delightMovesLanded", out var ml) && ml.ValueKind == JsonValueKind.Array
                ? ml.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList() : new List<string>();
            return new BeatVerdict(row.BeatId, beatNumber, row.PassFraction, fromCacheFlag, violations, landed,
                root.TryGetProperty("beatJob", out var j) ? j.GetString() ?? "other" : "other",
                root.TryGetProperty("wordCount", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetInt32() : 0);
        }
        catch
        {
            return new BeatVerdict(row.BeatId, beatNumber, row.PassFraction, fromCacheFlag,
                new List<DontViolation>(), new List<string>(), "other", 0);
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    /// <summary>Scans for the first balanced top-level JSON object in <paramref name="raw"/>,
    /// respecting string literals (so braces/quotes inside quoted evidence text don't confuse
    /// the brace count). Tolerates a leading markdown fence or preamble before the '{' and any
    /// trailing commentary after the matching '}'. Returns null if no balanced object is found
    /// (i.e. the response really was cut off mid-object).</summary>
    private static string? ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        if (start < 0) return null;
        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = start; i < raw.Length; i++)
        {
            var c = raw[i];
            if (inString)
            {
                if (escape) escape = false;
                else if (c == '\\') escape = true;
                else if (c == '"') inString = false;
                continue;
            }
            if (c == '"') { inString = true; continue; }
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return raw[start..(i + 1)];
            }
        }
        return null;
    }
}
