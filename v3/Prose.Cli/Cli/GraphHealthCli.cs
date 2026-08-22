using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --graph-health --universe &lt;slug&gt; [--json] [--used-in-prose-only]
///
/// Runs GraphHealthService.Analyze() against the scoped universe's world graph — orphaned
/// nodes (zero edges), weakly-connected nodes (exactly one edge), and suspicious/malformed
/// node names (sentence fragments, junk parses). Zero LLM calls, pure graph traversal + string
/// heuristics over the already-cached UniverseGraphService data (run --rebuild-graph first if the
/// cache might be stale).
///
/// Added 2026-08-09: GraphHealthService existed with a complete, working Analyze() method but
/// had no CLI or MCP wrapper at all — same "unreachable service" pattern as DataConsistencyService
/// (also fixed this session), just found before it had a chance to silently rot.
///
/// Added 2026-08-15 (--used-in-prose-only): most orphaned/weakly-connected nodes are NOT bugs —
/// they're intentional flavor texture (guns, drugs, apparel, hundreds of them) seeded so the
/// world feels lived-in and is ready if a future scene needs to pull from a deep roster, but
/// that never actually appear on the page. The real interconnectivity problem is the much
/// smaller subset that DOES appear in shipped prose (per BeatEntityPresence) but isn't properly
/// linked — same "narrow the scope to what's actually live" principle DCM applies to context,
/// applied here to the graph. This flag filters the report down to just that actionable subset.
/// </summary>
public static class GraphHealthCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var json = args.Contains("--json");
        var proseOnly = args.Contains("--used-in-prose-only");
        var graph = services.GetRequiredService<UniverseGraphService>();
        var health = services.GetRequiredService<GraphHealthService>();

        if (!json) Console.WriteLine("Loading world graph…");
        graph.Rebuild();

        var report = health.Analyze();

        if (proseOnly && !report.ProseUsageAvailable)
        {
            Console.Error.WriteLine("[graph-health] --used-in-prose-only requested but no DB connection was available to resolve prose usage.");
            return 1;
        }

        if (proseOnly)
        {
            report.OrphanedNodes = report.OrphanedNodes.Where(o => o.ReferencedInProse == true).ToList();
            report.WeaklyConnected = report.WeaklyConnected.Where(o => o.ReferencedInProse == true).ToList();
        }

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return report.TotalSuspicious > 0 ? 1 : 0;
        }

        Console.WriteLine($"Total nodes: {report.TotalNodes}");
        if (proseOnly)
        {
            Console.WriteLine($"Orphaned, referenced in prose (0 edges): {report.OrphanedNodes.Count} (of {report.TotalOrphans} total orphans)");
            Console.WriteLine($"Weakly connected, referenced in prose (1 edge): {report.WeaklyConnected.Count} (of {report.TotalWeaklyConnected} total)");
        }
        else
        {
            Console.WriteLine($"Orphaned (0 edges): {report.TotalOrphans}");
            Console.WriteLine($"Weakly connected (1 edge): {report.TotalWeaklyConnected}");
            if (report.ProseUsageAvailable)
            {
                Console.WriteLine($"  … of which referenced in prose (real gap): {report.OrphansReferencedInProse} orphans, {report.WeaklyConnectedReferencedInProse} weakly-connected");
                Console.WriteLine($"  … rest is flavor/reserve texture — expected, not a bug (re-run with --used-in-prose-only to see just the real gap)");
            }
        }
        Console.WriteLine($"Suspicious names: {report.TotalSuspicious}");
        Console.WriteLine();

        PrintSection("SUSPICIOUS NAMES", report.SuspiciousNodes);
        PrintSection("ORPHANED (flagged as suspicious)", report.OrphanedNodes.Where(o => o.IsSuspicious).ToList());
        if (proseOnly)
        {
            PrintSection("ORPHANED — referenced in prose", report.OrphanedNodes);
            PrintSection("WEAKLY CONNECTED — referenced in prose", report.WeaklyConnected);
        }

        return report.TotalSuspicious > 0 ? 1 : 0;
    }

    static void PrintSection(string title, List<OrphanInfo> items)
    {
        if (items.Count == 0) return;
        Console.WriteLine($"── {title} ({items.Count}) ──");
        foreach (var o in items.Take(30))
            Console.WriteLine($"  [{o.NodeType}] \"{o.Name}\" ({o.Id}) — {o.Reason}");
        if (items.Count > 30)
            Console.WriteLine($"  … and {items.Count - 30} more");
        Console.WriteLine();
    }
}
