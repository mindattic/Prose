using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --sequential-read-status --slug &lt;slug&gt; | --all [--universe glmz|scry|gspl] [--json]
/// prose --sequential-read-record --slug &lt;slug&gt; --read-by &lt;name&gt; [--stages N] [--summary "text"] --universe glmz|scry|gspl
///
/// Added 2026-08-15 after BCODA's Ch23-37 reparenting (2026-08-14) revealed that a structural
/// fix (correcting Nodes.ParentNodeId) had never been followed by anyone actually reading what
/// was inside the reparented chapters — the first real read found a genuine spoiler-duplicate
/// beat that had sat there, live, since before the fix. See SequentialReadTrackingService for
/// the full rationale and the self-invalidating hash mechanism.
/// </summary>
public static class SequentialReadCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var command = args.Contains("--sequential-read-record") ? "record" : "status";
        var json = args.Contains("--json");
        var all = args.Contains("--all");
        string? slug = null, readBy = null, summary = null;
        int stages = 1;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": slug = args[i + 1]; break;
                case "--read-by": readBy = args[i + 1]; break;
                case "--summary": summary = args[i + 1]; break;
                case "--stages": int.TryParse(args[i + 1], out stages); break;
            }
        }

        var tracker = services.GetRequiredService<SequentialReadTrackingService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        if (command == "record")
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(readBy))
            {
                Console.Error.WriteLine("Usage: prose --sequential-read-record --slug <slug> --read-by <name> [--stages N] [--summary \"text\"] --universe <u>");
                return 2;
            }
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug);
            if (node is null) { Console.Error.WriteLine($"[sequential-read] No node found with slug '{slug}'."); return 1; }

            await tracker.RecordReadAsync(node.Id, readBy, stages, summary);
            Console.WriteLine($"[sequential-read] Recorded a {stages}-stage sequential read of '{node.Title}' by {readBy}.");
            return 0;
        }

        if (all)
        {
            var books = await db.Nodes.AsNoTracking()
                .Where(n => n.Kind == "book")
                .OrderBy(n => n.Title)
                .ToListAsync();

            var reports = new List<SequentialReadReport>();
            foreach (var b in books)
                reports.Add(await tracker.GetStatusAsync(b.Id));

            if (json)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(reports,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            Console.WriteLine($"{"Book",-40} {"Status",-8} {"Chapters",-9} {"Beats",-7} {"Last read",-12} {"By"}");
            foreach (var r in reports)
            {
                var lastRead = r.LastReadAt?.ToString("yyyy-MM-dd") ?? "-";
                Console.WriteLine($"{Truncate(r.BookTitle, 40),-40} {r.Status,-8} {r.CurrentChapterCount,-9} {r.CurrentBeatCount,-7} {lastRead,-12} {r.LastReadBy ?? "-"}");
            }
            var neverCount = reports.Count(r => r.Status == SequentialReadStatus.Never);
            var staleCount = reports.Count(r => r.Status == SequentialReadStatus.Stale);
            Console.WriteLine();
            Console.WriteLine($"{reports.Count} books: {neverCount} never read, {staleCount} stale, {reports.Count - neverCount - staleCount} current.");
            return neverCount + staleCount > 0 ? 1 : 0;
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --sequential-read-status --slug <slug> | --all [--universe <u>] [--json]");
            return 2;
        }

        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var single = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug);
        if (single is null) { Console.Error.WriteLine($"[sequential-read] No node found with slug '{slug}'."); return 1; }

        var report = await tracker.GetStatusAsync(single.Id);
        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return report.Status == SequentialReadStatus.Current ? 0 : 1;
        }

        Console.WriteLine($"Book:          {report.BookTitle}");
        Console.WriteLine($"Status:        {report.Status}");
        Console.WriteLine($"Current:       {report.CurrentChapterCount} chapters, {report.CurrentBeatCount} beats");
        if (report.LastReadAt != null)
        {
            Console.WriteLine($"Last read:     {report.LastReadAt:yyyy-MM-dd HH:mm} UTC by {report.LastReadBy}");
            Console.WriteLine($"Last read was: {report.LastReadChapterCount} chapters, {report.LastReadBeatCount} beats");
        }
        else
        {
            Console.WriteLine("Last read:     never");
        }
        return report.Status == SequentialReadStatus.Current ? 0 : 1;
    }

    static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
