using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --chekhov-audit --slug &lt;nodeSlug&gt;
///
/// Chekhov's Gun audit: every concrete prop, environmental anchor, sensory
/// detail, and recurring character-specific physical trait is extracted from
/// the prose and tested for narrative function. A prop that appears once with
/// no payoff is ORPHANED; one that appears multiple times doing the same thing
/// each time is DECORATION; one whose appearances each serve a distinct
/// narrative purpose EARNS_IT.
///
/// Run this before trimming any prose detail. Before cutting, ask: why is
/// this here? Does it recur? Does each recurrence do something different?
///
/// Exit codes: 0 = clean, 1 = orphans/flags present, 2 = error.
/// </summary>
public static class ChekhovAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --chekhov-audit --slug <nodeSlug>");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        Console.WriteLine($"Chekhov audit of '{node.Title}' — extracting props and testing narrative function…\n");

        var svc = services.GetRequiredService<ChekhovAuditService>();
        ChekhovAuditReport report;
        try
        {
            report = await svc.AuditAsync(node.Id);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Audit failed: {ex.Message}");
            return 2;
        }

        Console.WriteLine($"Beats:  {report.BeatCount}");
        Console.WriteLine($"Props:  {report.Findings.Count} found");
        Console.WriteLine();

        static string Icon(string v) => v switch
        {
            "ORPHANED"   => "△ ORPHANED  ",
            "FLAG"       => "? FLAG      ",
            "DECORATION" => "◇ DECO     ",
            "EARNS_IT"   => "✓ EARNS IT ",
            "ATMOSPHERE" => "· ATMOS    ",
            _            => "  UNKNOWN  ",
        };

        foreach (var group in new[] { "ORPHANED", "FLAG", "DECORATION", "EARNS_IT", "ATMOSPHERE" })
        {
            var items = report.Findings.Where(f => f.Verdict == group).ToList();
            if (items.Count == 0) continue;

            Console.WriteLine($"── {group} ({items.Count}) ──────────────────────────────────────────");
            foreach (var f in items)
            {
                var beatList = string.Join(" → ", f.Appearances.Select(a => a.BeatLabel));
                Console.WriteLine($"{Icon(f.Verdict)}  {f.PropName,-30}  {beatList}");
                Console.WriteLine($"              {f.Reasoning}");
                if (f.Fix != null)
                    Console.WriteLine($"              FIX: {f.Fix}");
                Console.WriteLine();
            }
        }

        Console.WriteLine(new string('─', 70));
        if (report.OrphanedCount + report.FlagCount > 0)
        {
            Console.WriteLine($"⚠  {report.OrphanedCount} orphaned, {report.FlagCount} flagged, {report.DecorationCount} decorative.");
            Console.WriteLine("   Before cutting: confirm each orphan has no payoff in a later beat.");
            Console.WriteLine("   Before keeping: confirm each decoration earns its repetition.");
            return 1;
        }

        Console.WriteLine($"✅ CLEAN — {report.EarnsItCount} props earn their place, {report.Findings.Count(f => f.Verdict == "ATMOSPHERE")} atmospheric.");
        return 0;
    }
}
