using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --export-synopsis (--slug &lt;slug&gt; | --all) [--force]</c>
///
/// Standalone chapter-by-chapter synopsis export (the same artifact <c>--export-node</c>
/// emits): generates/refreshes NodeChapterSummaries from the live prose and writes
/// <c>story-synopsis.txt</c> into each book's export folder. <c>--force</c> ignores
/// the per-chapter content-hash cache and regenerates everything.
/// </summary>
public static class ExportSynopsisCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");
        bool force = args.Contains("--force");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[i + 1];

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: ss --export-synopsis (--slug <slug> | --all) [--force]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var synopsis = services.GetRequiredService<SynopsisExportService>();

        List<(Guid Id, string Title)> targets;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking().OfType<BookNode>().AsQueryable();
            if (!all) q = q.Where(n => n.Slug == slug);
            targets = (await q.Select(n => new { n.Id, n.Title }).ToListAsync())
                .Select(n => (n.Id, n.Title)).ToList();
        }
        if (targets.Count == 0) { Console.Error.WriteLine("[synopsis] No matching book."); return 1; }

        int failed = 0;
        foreach (var (nodeId, title) in targets)
        {
            try
            {
                Console.WriteLine($"[synopsis] {title}…");
                var path = await synopsis.ExportAsync(nodeId, force);
                Console.WriteLine(path != null ? $"[synopsis]   → {path}" : "[synopsis]   (no enabled prose — skipped)");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[synopsis]   FAILED: {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"[synopsis] Done — {targets.Count - failed}/{targets.Count} exported.");
        return failed == 0 ? 0 : 1;
    }
}
