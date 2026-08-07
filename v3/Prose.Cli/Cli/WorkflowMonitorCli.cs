using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Prose.Cli;

/// <summary>
/// CLI handler for --workflow-status.
///
/// Usage:
///   ss --workflow-status --slug &lt;slug&gt;           Per-node coverage matrix + gaps
///   ss --workflow-status --slug &lt;slug&gt; --json     Machine-readable JSON
///   ss --workflow-status --all                    Global stats across all nodes
///   ss --workflow-status --all --json             Machine-readable JSON
/// </summary>
public static class WorkflowMonitorCli
{
    public static async Task RunAsync(IServiceProvider sp, string[] args)
    {
        var monitor   = sp.GetRequiredService<WorkflowMonitorService>();
        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        var slug = GetArg(args, "--slug");
        var json = args.Contains("--json");
        var all  = args.Contains("--all");

        if (slug != null && !all)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await db.Nodes.AsNoTracking()
                .Where(s => s.Slug == slug)
                .Select(s => new { s.Id, s.Title })
                .FirstOrDefaultAsync();
            if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return; }

            var report = await monitor.GetNodeCoverageAsync(node.Id);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                return;
            }

            Console.WriteLine($"\n=== Workflow Coverage: {report.NodeTitle} ({report.NodeSlug}) ===");
            Console.WriteLine($"Beats logged: {report.TotalBeatsLogged}");
            Console.WriteLine();

            Console.WriteLine("Service coverage:");
            foreach (var s in report.ServiceStats)
            {
                var bar = s.ApplicableCalls > 0 ? $"{s.ActivationRate:P0}" : "n/a";
                Console.WriteLine($"  {s.Service,-20} {bar,6}  ({s.ActiveCalls}/{s.ApplicableCalls} applicable, {s.TotalCalls} total calls)");
            }

            if (report.Gaps.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("GAPS (services applicable but under-utilized):");
                foreach (var g in report.Gaps)
                    Console.WriteLine($"  !  {g}");
            }
            else if (report.TotalBeatsLogged > 0)
            {
                Console.WriteLine("\nNo coverage gaps.");
            }
            else
            {
                Console.WriteLine("\n(No beats logged yet — use ProseWriterRouter to generate beats with coverage tracking)");
            }
            return;
        }

        // --all or no slug: global stats
        var stats          = await monitor.GetGlobalStatsAsync();
        var gaps           = await monitor.GetAllNodesWithGapsAsync();
        var unseededCount  = await monitor.GetEntityEmbeddingGapCountAsync();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { GlobalStats = stats, NodesWithGaps = gaps },
                new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine("\n=== Global Workflow Coverage ===");
        Console.WriteLine("Service utilization across all nodes:");
        foreach (var s in stats)
        {
            var bar = s.ApplicableCalls > 0 ? $"{s.ActivationRate:P0}" : "n/a";
            Console.WriteLine($"  {s.Service,-20} {bar,6}  ({s.ActiveCalls}/{s.ApplicableCalls} applicable)");
        }

        if (gaps.Count > 0)
        {
            Console.WriteLine($"\nNodes with coverage gaps ({gaps.Count}):");
            foreach (var g in gaps)
                Console.WriteLine($"  {g.Slug,-30} {g.GapCount} gap(s)");
        }
        else if (stats.Count > 0)
        {
            Console.WriteLine("\nNo coverage gaps across any node.");
        }
        else
        {
            Console.WriteLine("\n(No beats logged yet — use ProseWriterRouter to generate beats with coverage tracking)");
        }

        if (unseededCount > 0)
            Console.WriteLine($"\n  !  EntityEmbeddings: {unseededCount} active {(unseededCount == 1 ? "entity has" : "entities have")} no embedding row — embedding lookup silently misses them. Run 'ss --reembed --universe <slug>'.");
        else
            Console.WriteLine($"\n  ✓  EntityEmbeddings: all active entities have embeddings.");
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
