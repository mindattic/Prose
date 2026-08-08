using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --altitude-audit (--slug &lt;slug&gt; | --all) [--force-synopsis]</c>
///
/// The three-altitudes agreement audit (docs/LOGIC.md §8): designed story (bible +
/// blueprint) vs told story (chapter synopses). Writes
/// <c>audit-outlines-&lt;date&gt;/logic/&lt;CODE&gt;-ALTITUDE.md</c> and files
/// OutlineDrift findings. <c>--force-synopsis</c> regenerates chapter synopses
/// (ignores the content-hash cache) before comparing.
/// </summary>
public static class AltitudeAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");
        bool force = args.Contains("--force-synopsis");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[i + 1];

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --altitude-audit (--slug <slug> | --all) [--force-synopsis]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var audit = services.GetRequiredService<AltitudeAuditService>();

        List<(Guid Id, string Title)> targets;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking().OfType<BookNode>().AsQueryable();
            if (!all) q = q.Where(n => n.Slug == slug);
            targets = (await q.Select(n => new { n.Id, n.Title }).ToListAsync())
                .Select(n => (n.Id, n.Title)).ToList();
        }
        if (targets.Count == 0) { Console.Error.WriteLine("[altitude] No matching story."); return 1; }

        int failed = 0;
        foreach (var (nodeId, title) in targets)
        {
            try
            {
                Console.WriteLine($"[altitude] {title}…");
                var result = await audit.AuditAsync(nodeId, force);
                if (result == null) { Console.WriteLine("[altitude]   (no bible or no prose — skipped)"); continue; }

                var b = result.Findings.Count(f => f.Severity == "BLOCKER");
                var m = result.Findings.Count(f => f.Severity == "MODERATE");
                var n = result.Findings.Count(f => f.Severity == "MINOR");
                Console.WriteLine(result.Findings.Count == 0
                    ? "[altitude]   CLEAN — designed and told stories agree."
                    : $"[altitude]   FINDINGS ({b} BLOCKER / {m} MODERATE / {n} MINOR)");
                Console.WriteLine($"[altitude]   report: {result.ReportPath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[altitude]   FAILED: {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"[altitude] Done — {targets.Count - failed}/{targets.Count} audited.");
        return failed == 0 ? 0 : 1;
    }
}
