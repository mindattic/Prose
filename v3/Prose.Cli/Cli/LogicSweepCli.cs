using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// ss --logic-sweep --slug &lt;nodeSlug&gt; [--json]
///
/// See LogicSweepService's class doc for the honest scope note: this is a single-pass
/// approximation, not a replacement for the full /logic-sweep skill on a large book.
/// </summary>
public static class LogicSweepCli
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
            Console.Error.WriteLine("Usage: ss --logic-sweep --slug <nodeSlug> [--json]");
            return 2;
        }

        var svc = services.GetRequiredService<LogicSweepService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Logic sweep: '{node.Title}' — 6 dimensions…\n");

        var report = await svc.RunAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id = report.NodeId,
                slug,
                title = report.NodeTitle,
                beat_count = report.BeatCount,
                clean = report.Clean,
                blocker_count = report.BlockerCount,
                moderate_count = report.ModerateCount,
                minor_count = report.MinorCount,
                findings = report.Findings.Select(f => new
                {
                    dimension = f.RuleKey,
                    severity = f.Severity,
                    evidence = f.Evidence,
                    fix = f.Fix,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return report.BlockerCount > 0 ? 2 : report.Findings.Count > 0 ? 1 : 0;
        }

        if (report.Clean)
        {
            Console.WriteLine($"✓ Clean — {report.BeatCount} beats, no findings across all 6 dimensions.");
            return 0;
        }

        Console.WriteLine($"{report.Findings.Count} finding(s) — {report.BlockerCount} BLOCKER, " +
            $"{report.ModerateCount} MODERATE, {report.MinorCount} MINOR/DEVIATION\n");
        foreach (var f in report.Findings.OrderByDescending(f => f.Severity == "BLOCKER" ? 2 : f.Severity == "MODERATE" ? 1 : 0))
        {
            var icon = f.Severity switch { "BLOCKER" => "✗", "MODERATE" => "△", _ => "·" };
            Console.WriteLine($"  {icon} [{f.RuleKey}] {f.Severity}");
            Console.WriteLine($"      {f.Evidence}");
            if (!string.IsNullOrEmpty(f.Fix))
                Console.WriteLine($"      Fix: {f.Fix}");
            Console.WriteLine();
        }
        return report.BlockerCount > 0 ? 2 : 1;
    }
}
