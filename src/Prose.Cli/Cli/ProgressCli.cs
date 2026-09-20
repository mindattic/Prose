using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --progress [--json]</c> — the Strand Progress Dashboard (see <c>.claude/commands/
/// progress.md</c>). Built 2026-08-22 to replace that command's original raw-sqlcmd query
/// (against long-gone <c>Strands</c>/<c>StrandBeats</c>/<c>StrandReviewSummaries</c> tables that
/// predate the current Book/Chapter/Beat model and no longer exist) with a real Hub-routed
/// command against the live schema — see project memory
/// <c>feedback_all_writes_through_hub_2026_08_22</c>: nothing reaches the database except
/// through Prose.Hub, reads included.
///
/// Cross-universe by design (a dashboard of every book) — IgnoreQueryFilters() bypasses
/// ProseDbContext's ambient ScopedUniverseId filter deliberately, not by oversight.
/// </summary>
public static class ProgressCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool json = args.Contains("--json");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var books = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .OfType<BookNode>()
            .Where(n => n.Status != "archived")
            .Select(n => new { n.Id, n.NodeCode, n.Title, n.Kind, n.Status, n.Score, n.ScoredAt })
            .ToListAsync();

        var rows = new List<Row>();
        foreach (var b in books)
        {
            var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, b.Id);
            var texts = await db.BeatNodes.AsNoTracking()
                .Where(bn => leafIds.Contains(bn.NodeId))
                .Join(db.Beats, bn => bn.BeatId, bt => bt.Id, (bn, bt) => bt.Text)
                .Where(t => t != null && t != "")
                .ToListAsync();

            var words = texts.Sum(t => BeatMarkup.StripEntityTags(t)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);

            rows.Add(new Row(b.NodeCode ?? "", b.Title, b.Kind, b.Status,
                b.Score, words, words / 250, b.ScoredAt));
        }

        var ordered = rows
            .OrderBy(r => r.Score.HasValue ? 0 : 1)
            .ThenByDescending(r => r.Score ?? 0)
            .ToList();

        // Omit stub entries (no pages, no score) unless the total row count is already under 10.
        var nonStub = ordered.Where(r => !(r.Pages == 0 && r.Score == null)).ToList();
        var display = ordered.Count < 10 ? ordered : nonStub;

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(display,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{"Code",-8} {"Title",-40} {"Kind",-8} {"Status",-28} {"Score",6} {"Pages",6}");
        Console.WriteLine(new string('-', 100));
        foreach (var r in display)
        {
            var scoreStr = r.Score.HasValue ? r.Score.Value.ToString("F1") : "-";
            var title = r.Title.Length > 40 ? r.Title[..37] + "..." : r.Title;
            var status = r.Status.Length > 28 ? r.Status[..25] + "..." : r.Status;
            Console.WriteLine($"{r.Code,-8} {title,-40} {r.Kind,-8} {status,-28} {scoreStr,6} {r.Pages,6}");
        }

        var scored = display.Where(r => r.Score.HasValue).ToList();
        var meanScore = scored.Count > 0 ? scored.Average(r => r.Score!.Value) : 0;
        Console.WriteLine();
        Console.WriteLine($"{display.Count} strand(s) — {display.Sum(r => r.Pages)} total page(s) — mean score {meanScore:F1} across {scored.Count} scored strand(s).");

        return 0;
    }

    private sealed record Row(string Code, string Title, string Kind, string Status,
        double? Score, int Words, int Pages, DateTime? ScoredAt);
}
