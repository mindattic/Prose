using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --story-audit --slug &lt;nodeSlug&gt; [--json]
///
/// Audits a node against its 7 commandments:
///   • Gateway commandments — when PreviousNodeId is null (standalone / first in series)
///   • Sequel commandments  — when PreviousNodeId is set
///
/// Runs all 7 checks in parallel, reports pass/warn/fail per commandment
/// with evidence and a fix suggestion.
///
/// Exit codes: 0 = all pass, 1 = warnings only, 2 = blocking failures.
/// </summary>
public static class StoryAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --story-audit --slug <nodeSlug> [--json]");
            return 2;
        }

        var auditSvc  = services.GetRequiredService<StoryAuditService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        var isSequel = node.PreviousNodeId.HasValue;
        var mode = isSequel ? "sequel" : "gateway";

        if (!jsonMode)
            Console.WriteLine($"Auditing '{node.Title}' as {mode.ToUpperInvariant()} story — running 7 commandment checks…\n");

        var report = await auditSvc.AuditAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_slug     = report.NodeSlug,
                node_title    = report.NodeTitle,
                mode            = report.Mode,
                previous_node = report.PreviousNode,
                gateway_ready   = report.GatewayReady,
                blocking_count  = report.BlockingCount,
                advisory_count  = report.AdvisoryCount,
                plant_count     = report.PlantCount,
                orphaned_plants = report.OrphanedPlants,
                checks          = report.Checks,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return report.BlockingCount > 0 ? 2 : report.AdvisoryCount > 0 ? 1 : 0;
        }

        // ── Human-readable output ──────────────────────────────────────────────

        Console.WriteLine($"Mode:    {report.Mode.ToUpperInvariant()}");
        if (report.PreviousNode != null)
            Console.WriteLine($"Sequel to: {report.PreviousNode}");
        Console.WriteLine($"Plants:  {report.PlantCount} registered ({report.OrphanedPlants} orphaned)");
        Console.WriteLine();

        static string Icon(string status) => status switch
        {
            "pass" => "✅",
            "warn" => "⚠️ ",
            "fail" => "❌",
            _       => "  ",
        };

        foreach (var check in report.Checks)
        {
            Console.WriteLine($"{Icon(check.Status)} {check.Title}");
            Console.WriteLine($"   {check.Evidence}");
            if (check.Fix != null)
                Console.WriteLine($"   FIX: {check.Fix}");
            Console.WriteLine();
        }

        Console.WriteLine(new string('─', 60));

        if (report.GatewayReady)
        {
            Console.WriteLine($"✅ READY — all {mode} commandments satisfied.");
        }
        else
        {
            Console.WriteLine($"Blocking: {report.BlockingCount}   Advisory: {report.AdvisoryCount}");
            if (report.BlockingCount > 0)
                Console.WriteLine("Fix failing commandments before publishing this node.");
        }

        return report.BlockingCount > 0 ? 2 : report.AdvisoryCount > 0 ? 1 : 0;
    }
}
