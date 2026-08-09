using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --graph-health --universe &lt;slug&gt; [--json]
///
/// Runs GraphHealthService.Analyze() against the scoped universe's world graph — orphaned
/// nodes (zero edges), weakly-connected nodes (exactly one edge), and suspicious/malformed
/// node names (sentence fragments, junk parses). Zero LLM calls, pure graph traversal + string
/// heuristics over the already-cached WorldGraphService data (run --rebuild-graph first if the
/// cache might be stale).
///
/// Added 2026-08-09: GraphHealthService existed with a complete, working Analyze() method but
/// had no CLI or MCP wrapper at all — same "unreachable service" pattern as DataConsistencyService
/// (also fixed this session), just found before it had a chance to silently rot.
/// </summary>
public static class GraphHealthCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var json = args.Contains("--json");
        var graph = services.GetRequiredService<WorldGraphService>();
        var health = services.GetRequiredService<GraphHealthService>();

        if (!json) Console.WriteLine("Loading world graph…");
        graph.Rebuild();

        var report = health.Analyze();

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return report.TotalSuspicious > 0 ? 1 : 0;
        }

        Console.WriteLine($"Total nodes: {report.TotalNodes}");
        Console.WriteLine($"Orphaned (0 edges): {report.TotalOrphans}");
        Console.WriteLine($"Weakly connected (1 edge): {report.TotalWeaklyConnected}");
        Console.WriteLine($"Suspicious names: {report.TotalSuspicious}");
        Console.WriteLine();

        PrintSection("SUSPICIOUS NAMES", report.SuspiciousNodes);
        PrintSection("ORPHANED (flagged as suspicious)", report.OrphanedNodes.Where(o => o.IsSuspicious).ToList());

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
