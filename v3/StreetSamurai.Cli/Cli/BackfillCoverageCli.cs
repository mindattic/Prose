using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI handler for --backfill-coverage.
///
/// Populates BeatServiceLog + BeatModeLog for prose that was written BEFORE the
/// ProseWriterRouter existed, WITHOUT regenerating any beat text. For each existing
/// beat it runs the router's coverage-only path (ProseWriterRouter.LogCoverageAsync),
/// which computes which prose services would have fired from the beat's synopsis and
/// its position in the chapter — then records the logs the workflow monitor reads.
///
/// A book node (one with chapter children) rolls down into each chapter; pacing and
/// structural role are per-chapter arcs, so each chapter is logged with its own
/// beatIndex/totalBeats.
///
/// Usage:
///   ss --backfill-coverage --slug &lt;book-or-chapter-slug&gt;
/// </summary>
public static class BackfillCoverageCli
{
    public static async Task RunAsync(IServiceProvider sp, string[] args)
    {
        var router    = sp.GetRequiredService<ProseWriterRouter>();
        var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        var slug = GetArg(args, "--slug");
        if (slug == null) { Console.Error.WriteLine("Missing --slug <book-or-chapter-slug>"); return; }

        await using var db = await dbFactory.CreateDbContextAsync();
        var root = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id, s.Title, s.Slug, s.UniverseId })
            .FirstOrDefaultAsync();
        if (root == null) { Console.Error.WriteLine($"Node not found: {slug}"); return; }

        // A book fans out into its live chapters; a lone chapter backfills itself.
        // Archived/unincorporated children aren't part of the book — exclude them so the
        // rollup reflects the actual canon chapters, not cut scenes or draft scratch.
        var children = await db.Nodes.AsNoTracking()
            .Where(s => s.ParentNodeId == root.Id
                     && s.Status != "archived" && s.Status != "unincorporated")
            .OrderBy(s => s.SortKey)
            .Select(s => new { s.Id, s.Title, s.Slug, s.UniverseId })
            .ToListAsync();
        var chapters = children.Count > 0
            ? children
            : [root];

        Console.WriteLine($"\n=== Backfilling coverage: {root.Title} ({root.Slug}) ===");
        var totalBeatsLogged = 0;

        foreach (var ch in chapters)
        {
            // Enabled beats in reading order, joined through the NodeBeats bridge.
            var beats = await (
                from sb in db.NodeBeats.AsNoTracking()
                join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                where sb.NodeId == ch.Id && sb.IsEnabled
                orderby sb.SortKey
                select new { b.Id, b.Synopsis, b.BeatTitle, b.Text }).ToListAsync();

            if (beats.Count == 0) continue;

            for (var i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                // The synopsis is the closest proxy to the original BeatGoal; fall back to
                // the title. A short prose tail sharpens mode detection (combat vs dialogue).
                var goal = !string.IsNullOrWhiteSpace(beat.Synopsis) ? beat.Synopsis
                         : beat.BeatTitle ?? "";
                var proseHint = beat.Text is { Length: > 0 and < 500 } ? beat.Text : null;

                await router.LogCoverageAsync(
                    beat.Id, ch.Id, goal, proseHint,
                    beatIndex: i, totalBeats: beats.Count,
                    universeId: ch.UniverseId);
            }

            totalBeatsLogged += beats.Count;
            Console.WriteLine($"  {ch.Title,-44} {beats.Count,3} beats logged");
        }

        Console.WriteLine($"\nDone. {totalBeatsLogged} beats logged across {chapters.Count} node(s).");
        Console.WriteLine($"Inspect with: ss --workflow-status --slug {root.Slug}");
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
