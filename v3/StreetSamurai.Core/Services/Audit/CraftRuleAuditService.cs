using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services.Audit;

/// <summary>
/// Audits a node's live prose against docs/CRAFT.md §8 (Banned Mannerisms). Each numbered
/// item in that section is parsed live from CanonDocumentSections every run and becomes its
/// own ILlmAuditRule — there is no hand-duplicated C# array of mannerisms to drift out of
/// sync with CRAFT.md. Edit §8 via set_canon_section MCP, re-run ss --craft-audit, the new
/// wording is what gets checked next time.
/// </summary>
public class CraftRuleAuditService(
    AuditRunner auditRunner,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
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

        var prose = childChapters.Count > 0
            ? string.Join("\n\n", childChapters
                .SelectMany(ch => ch.BeatNodes
                    .Where(bn => bn.IsEnabled)
                    .OrderBy(bn => bn.SortKey)
                    .Select(bn => bn.Beat!.Text))
                .Where(t => !string.IsNullOrWhiteSpace(t)))
            : string.Join("\n\n", node.BeatNodes
                .Where(bn => bn.IsEnabled)
                .OrderBy(bn => bn.SortKey)
                .Select(bn => bn.Beat!.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

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
        var ctx = new AuditContext(nodeId, node.UniverseId, ClampProse(prose), [],
            new Dictionary<string, object?>());

        var verdicts = await auditRunner.RunAsync(
            "CRAFTAUDIT", $"node:{node.Slug}", FindingCategory.Other, rules, ctx, ct: ct);

        return new CraftAuditReport(
            NodeId:        node.Id,
            NodeSlug:      node.Slug,
            NodeTitle:     node.Title,
            Findings:      verdicts.Where(v => v.Severity != "PASS").ToList());
    }

    static string ClampProse(string p) =>
        p.Length <= 100_000
            ? p
            : p[..50_000] + "\n\n[... middle of the manuscript elided for length ...]\n\n" + p[^50_000..];

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
