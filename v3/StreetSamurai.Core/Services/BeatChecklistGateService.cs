using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services.Audit;

namespace StreetSamurai.Core.Services;

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
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm,
    FindingsService findings,
    SettingsService settings,
    ILogger<BeatChecklistGateService> log)
{
    private const string FindingSummaryPrefix = "CHECKLIST";
    /// <summary>Bump when the evaluation prompt changes shape — invalidates the cache.</summary>
    private const string PromptVersion = "v1";
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
        // as CraftRuleAuditService / BookAuditService).
        var chapterIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId && n is ChapterNode)
            .OrderBy(n => n.SortKey).Select(n => n.Id).ToListAsync(ct);
        var sourceIds = chapterIds.Count > 0 ? chapterIds : new List<Guid> { nodeId };

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

            var verdict = await EvaluateBeatAsync(beat.Id, beat.Number, beat.Text, donts, moves, ct);
            evaluated++;

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
            verdicts.Add(verdict);
        }

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
                suggestedFix: "Rewrite the flagged phrasing per CRAFT.md §8; if the hit is literal/diegetic, dismiss.");
            filed++;
        }
        foreach (var v in verdicts.Where(v => v.MovesLanded.Count == 0 && v.WordCount >= DelightExemptWordCount))
        {
            findings.Upsert(
                $"{filePathPrefix}/beat:{v.BeatId:N}", chapterId: null, FindingCategory.CraftChecklist,
                FindingSeverity.Low,
                $"{FindingSummaryPrefix} beat #{v.BeatNumber}: flat beat — {v.WordCount} words, no DELIGHT move lands (job: {v.BeatJob})",
                snippet: null,
                suggestedFix: "Not every beat needs a move — but a full scene that lands none usually reads inert. " +
                              "Check DELIGHT.md for the 2-3 moves matching this beat's job.");
            filed++;
        }
        foreach (var bf in bookFindings)
        {
            findings.Upsert(filePathPrefix, chapterId: null, FindingCategory.CraftChecklist,
                FindingSeverity.Medium, $"{FindingSummaryPrefix} {bf}", snippet: null, suggestedFix: null);
            filed++;
        }

        log.LogInformation("Checklist {Slug}: {Evaluated} evaluated, {Cached} cached, {Filed} finding(s).",
            node.Slug, evaluated, fromCache, filed);

        return new ChecklistRunResult(nodeId, node.Slug ?? "", node.Title, ruleSetVersion,
            verdicts, bookFindings, evaluated, fromCache, filed);
    }

    // ── rule loading ───────────────────────────────────────────────────────────────

    public sealed record DelightMove(string Key, string Title, string Gist);

    private static async Task<(List<(string Key, string Title, string Desc)> Donts, List<DelightMove> Moves, string Version)>
        LoadRulesAsync(StreetSamuraiDbContext db, CancellationToken ct)
    {
        var craftSection = await db.CanonDocumentSections.AsNoTracking()
            .Where(s => s.Document!.DocumentType == "CraftGuide" && s.SectionKey == "SS-CRAFT-8")
            .Select(s => s.Content).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("CRAFT.md §8 not found in CanonDocumentSections — run ss --migrate-canon-docs --type CraftGuide.");

        var donts = CraftRuleAuditService.ParseMannerisms(craftSection)
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
            .Select(s => new DelightMove(s.SectionKey, s.SectionTitle, Gist(s.Content)))
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

    private async Task<BeatVerdict> EvaluateBeatAsync(
        Guid beatId, int beatNumber, string text,
        List<(string Key, string Title, string Desc)> donts, List<DelightMove> moves, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            You evaluate ONE beat (scene fragment) of a novel against binary craft checks.
            Answer mechanically — each check is yes/no with evidence, not an opinion score.

            PART A — BANNED MANNERISMS (the DON'Ts). For each, does this beat contain even
            one clear instance? Only flag CLEAR instances in THIS beat's text.
            """);
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

        var raw = await llm.GenerateAsync(sb.ToString(), $"BEAT #{beatNumber}:\n\n{text}",
            temperature: 0.0, maxTokens: 800, model: settings.ComprehensionProbeModel, ct: ct);
        raw = raw.Trim();
        if (raw.StartsWith("```"))
            raw = Regex.Replace(Regex.Replace(raw, @"^```(json)?\s*", ""), @"\s*```$", "");

        var wordCount = Regex.Matches(text, @"\b\w+\b").Count;
        var violations = new List<DontViolation>();
        var landed = new List<string>();
        var job = "other";
        try
        {
            using var doc = JsonDocument.Parse(raw);
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
            log.LogWarning("Checklist beat #{Number}: non-JSON response — treated as all-pass, will re-evaluate next run.", beatNumber);
        }

        var totalChecks = donts.Count + 1; // DON'Ts + the "≥1 applicable move" DO
        var passed = donts.Count - violations.Count
                     + (landed.Count >= 1 || wordCount < DelightExemptWordCount ? 1 : 0);
        return new BeatVerdict(beatId, beatNumber, Math.Round((double)passed / totalChecks, 4), false,
            violations, landed, job, wordCount);
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
}
