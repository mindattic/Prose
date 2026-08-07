using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Audits a node's live prose against docs/CRAFT.md §8 (Banned Mannerisms). Each numbered
/// item in that section is parsed live from CanonDocumentSections every run and becomes its
/// own ILlmAuditRule — there is no hand-duplicated C# array of mannerisms to drift out of
/// sync with CRAFT.md. Edit §8 via set_canon_section MCP, re-run ss --craft-audit, the new
/// wording is what gets checked next time.
/// </summary>
public class CraftRuleAuditService(
    AuditRunner auditRunner,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    const string CraftDocumentType = "CraftGuide";
    const string BannedMannerismsSectionKey = "SS-CRAFT-8";

    static readonly Regex MannerismPattern = new(
        @"(?:^|\n)\d+\.\s+\*\*(?<title>.+?)\*\*\s*[—-]+\s*(?<desc>.*?)(?=\n\d+\.\s+\*\*|\z)",
        RegexOptions.Singleline);

    public async Task<CraftAuditReport> RunAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes
            .AsNoTracking()
            .Include(n => n.BeatNodes).ThenInclude(bn => bn.Beat)
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // Book nodes hold their live manuscript on child chapters, not their own beats
        // (which may be a legacy outline) — same convention as BookAuditService.
        var childChapters = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == node.Id && n is ChapterNode)
            .Include(n => n.BeatNodes).ThenInclude(bn => bn.Beat)
            .OrderBy(n => n.SortKey)
            .ToListAsync(ct);

        // Per-beat records as well as the concatenated prose: the deterministic rules below
        // (interiority density, retired tics) report a rate ACROSS beats and cite individual
        // beats, so they need the beats separated — a single blob can only ever yield a
        // whole-node verdict, which is what let a 3.27-italics-per-beat book pass unnoticed.
        var beatRows = (childChapters.Count > 0
                ? childChapters.SelectMany(ch => ch.BeatNodes)
                : node.BeatNodes)
            .Where(bn => bn.IsEnabled && bn.Beat != null && !string.IsNullOrWhiteSpace(bn.Beat.Text))
            .OrderBy(bn => bn.SortKey)
            .Select(bn => new AuditBeat(bn.Beat!.Id, bn.Beat.Number, bn.Beat.Text, bn.SortKey))
            .ToList();

        var prose = string.Join("\n\n", beatRows.Select(b => b.Text));

        var sectionContent = await db.CanonDocumentSections
            .AsNoTracking()
            .Where(s => s.Document!.DocumentType == CraftDocumentType && s.SectionKey == BannedMannerismsSectionKey)
            .Select(s => s.Content)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"CRAFT.md section '{BannedMannerismsSectionKey}' not found — has CRAFT.md been migrated " +
                "(ss --migrate-canon-docs --type CraftGuide ...)?");

        var mannerisms = ParseMannerisms(sectionContent);
        if (mannerisms.Count == 0)
            throw new InvalidOperationException(
                $"CRAFT.md section '{BannedMannerismsSectionKey}' parsed to zero mannerisms — " +
                "its numbered-list format may have changed; update MannerismPattern.");

        var rules = mannerisms
            .Select(m => (IAuditRule)new MannerismRule($"craft_{m.Number}", m.Title, m.Description))
            .ToList();

        // Two of CRAFT's rules are countable, so they are checked in code rather than by asking a
        // model whether prose "feels" over-italicised. Deterministic means free, repeatable, and
        // impossible to talk out of — see each rule's own remarks.
        rules.Add(new InteriorityDensityRule());
        rules.Add(new RetiredTicRule());

        var ctx = new AuditContext(nodeId, node.UniverseId, AuditProseUtils.ClampProse(prose), beatRows,
            new Dictionary<string, object?>());

        var verdicts = await auditRunner.RunAsync(
            "CRAFTAUDIT", $"node:{node.Slug}", FindingCategory.Other, rules, ctx, ct: ct);

        return new CraftAuditReport(
            NodeId:        node.Id,
            NodeSlug:      node.Slug,
            NodeTitle:     node.Title,
            Findings:      verdicts.Where(v => v.Severity != "PASS").ToList());
    }

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

    /// <summary>One banned mannerism, adapted to the shared ILlmAuditRule dispatch. A failure
    /// here is a style regression to explicitly-retired prose (SS-A46), not a plot-logic
    /// defect — MODERATE, not the interface's BLOCKER default.</summary>
    sealed class MannerismRule(string key, string title, string description) : ILlmAuditRule
    {
        public string Key => key;
        public string Title => title;
        public string SeverityOnFail => "MODERATE";
        public int MaxResponseTokens => 500;

        public (string System, string User) BuildPrompt(AuditContext ctx)
        {
            var system = """
                You are a prose-craft auditor checking a manuscript against ONE specific banned
                mannerism from docs/CRAFT.md §8 (Banned Mannerisms — retired 2026-07-20, must not
                appear in any current prose).

                Respond as JSON only — no prose wrapper.
                {
                  "status":   "pass" | "warn" | "fail",
                  "evidence": "a direct quote (or close paraphrase) of the offending prose, or a
                               1-sentence confirmation of absence if passing",
                  "fix":      "one concrete rewrite sentence, or null if passing"
                }
                """;
            var user = $"""
                BANNED MANNERISM: {title}
                DESCRIPTION: {description}

                MANUSCRIPT:
                {ctx.Prose}

                Scan the manuscript for even ONE instance of this specific mannerism.
                - "pass" = the manuscript never does this
                - "warn" = a borderline/mild instance, arguably present
                - "fail" = a clear instance found
                Quote the actual offending text as evidence — do not generalize.
                """;
            return (system, user);
        }
    }

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
}

public record CraftAuditReport(
    Guid NodeId,
    string NodeSlug,
    string NodeTitle,
    IReadOnlyList<AuditVerdict> Findings)
{
    public bool Clean => Findings.Count == 0;
    public int ModerateCount => Findings.Count(f => f.Severity == "MODERATE");
    public int MinorCount => Findings.Count(f => f.Severity != "MODERATE");
}
