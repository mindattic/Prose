using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// prose --ledger-adjudicate --slug &lt;slug&gt; [--dry] [--max N] [--json]
///
/// <para>Judges the fact ledger's same-predicate contradiction groups against the prose they came
/// from. The deterministic layer (volatile / set-valued / paraphrase exemptions) has already
/// removed everything that was never a contradiction; what reaches this command is dominated by
/// complementary facets and temporal states, and telling those from a genuine conflict needs the
/// prose in front of it.</para>
///
/// <para>Cost-gated: one Sonnet call per uncached group. <c>--dry</c> counts the groups and spends
/// nothing. Verdicts cache on the claim uids plus every anchor beat's current TextHash, so a
/// re-run costs only the groups whose prose actually changed — and an interrupted run resumes
/// free.</para>
/// </summary>
public static class LedgerAdjudicateCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<ClaimGroupAdjudicationService>();

        var slug = Flag(args, "--slug") ?? Flag(args, "--code") ?? Flag(args, "--id");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine(
                "Usage: prose --ledger-adjudicate --slug <slug-or-code-or-id> [--dry] [--max N] [--json]\n" +
                "  --dry   count the groups that would be adjudicated. ZERO spend. Start here.\n" +
                "  --max   cap groups this run (default 400). Verdicts cache, so re-running continues.");
            return 2;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = await NodeRefResolver.ResolveAsync(db, slug);
        if (nodeId == null) { Console.Error.WriteLine($"[ledger-adjudicate] No node matched '{slug}'."); return 2; }

        var dry = args.Contains("--dry");
        var max = int.TryParse(Flag(args, "--max"), out var m) && m > 0 ? m : 400;

        if (dry)
        {
            var store = services.GetRequiredService<Prose.Core.Services.ContinuityService>();
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => n.Id == nodeId).Select(n => new { n.Slug, n.Title }).FirstAsync();
            var groups = store.GetContradictionGroups(node.Slug);
            Console.WriteLine($"{node.Title}  [{node.Slug}]");
            Console.WriteLine($"  {groups.Count} contradiction group(s) would be adjudicated " +
                              $"({Math.Min(groups.Count, max)} this run at --max {max}).");
            Console.WriteLine("[ledger-adjudicate] Dry run — nothing adjudicated, nothing written, nothing spent.");
            return 0;
        }

        var report = await svc.RunAsync(nodeId.Value, new ClaimGroupAdjudicationService.Options(true, max));

        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{report.Title}  [{report.Slug}]");
        Console.WriteLine($"  {report.Groups} group(s) considered");
        Console.WriteLine($"  adjudicated {report.Adjudicated} (cache hits {report.CacheHits})");
        Console.WriteLine($"    real conflicts:   {report.Conflicts}");
        Console.WriteLine($"    compatible:       {report.Compatible}  → {report.ClaimsCleared} claim row(s) cleared to NEW");
        Console.WriteLine($"    ungrounded:       {report.GroundingRejected}  (quote not in the prose — verdict discarded)");
        Console.WriteLine($"    skipped, no anchor: {report.Unanchored}");
        foreach (var n in report.Notes) Console.WriteLine($"  note: {n}");

        if (report.Conflicting.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("REAL CONFLICTS:");
            foreach (var v in report.Conflicting)
            {
                Console.WriteLine($"  [{v.Severity}] {v.EntityName} :: {v.Predicate}");
                foreach (var val in v.Values) Console.WriteLine($"      • {Clip(val, 90)}");
                Console.WriteLine($"      {v.Note}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"[ledger-adjudicate] {report.Conflicts} finding(s) filed under \"{ClaimGroupAdjudicationService.SummaryPrefix.Trim()}\".");
        return 0;
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
