using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services.Audit;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --craft-audit --slug &lt;nodeSlug&gt; [--json]
///
/// Audits a node's live prose against docs/CRAFT.md §8 (Banned Mannerisms), parsed live from
/// CanonDocumentSections — edit §8 via set_canon_section MCP and the next run picks it up,
/// no code change needed. Findings auto-heal (delete-then-recreate) on re-run.
///
/// Exit codes: 0 = clean, 1 = findings present (all MODERATE-severity — a style regression,
/// not a plot-logic blocker).
/// </summary>
public static class CraftAuditCli
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
            Console.Error.WriteLine("Usage: ss --craft-audit --slug <nodeSlug> [--json]");
            return 2;
        }

        var svc = services.GetRequiredService<CraftRuleAuditService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Craft audit: '{node.Title}' — CRAFT.md §8 Banned Mannerisms…\n");

        var report = await svc.RunAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id = report.NodeId,
                slug,
                title = report.NodeTitle,
                clean = report.Clean,
                findings = report.Findings.Select(f => new
                {
                    mannerism = f.Title,
                    severity  = f.Severity,
                    evidence  = f.Evidence,
                    fix       = f.Fix,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return report.Findings.Count > 0 ? 1 : 0;
        }

        if (report.Clean)
        {
            Console.WriteLine("✓ Clean — no banned mannerisms found.");
            return 0;
        }

        Console.WriteLine($"{report.Findings.Count} finding(s):\n");
        foreach (var f in report.Findings)
        {
            Console.WriteLine($"  △ {f.Title} [{f.Severity}]");
            Console.WriteLine($"      {f.Evidence}");
            if (!string.IsNullOrEmpty(f.Fix))
                Console.WriteLine($"      Fix: {f.Fix}");
            Console.WriteLine();
        }
        return 1;
    }
}
