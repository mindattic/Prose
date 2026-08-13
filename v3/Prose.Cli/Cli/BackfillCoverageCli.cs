using Prose.Core.Data;
using Prose.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Prose.Cli;

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
///   prose --backfill-coverage --slug &lt;book-or-chapter-slug&gt;
/// </summary>
public static class BackfillCoverageCli
{
    public static async Task RunAsync(IServiceProvider sp, string[] args)
    {
        var router    = sp.GetRequiredService<ProseWriterRouter>();
        var dbFactory = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        var slug = GetArg(args, "--slug");
        if (slug == null) { Console.Error.WriteLine("Missing --slug <book-or-chapter-slug>"); return; }

        await using var db = await dbFactory.CreateDbContextAsync();
        var root = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug)
            .Select(s => new { s.Id, s.Title, s.Slug, s.UniverseId })
            .FirstOrDefaultAsync();
        if (root == null) { Console.Error.WriteLine($"Node not found: {slug}"); return; }

        // A book fans out into its live chapters; a lone chapter backfills itself.
        // Descend to LEAF nodes (not just direct children) — a split-collection book
        // (BLST/ICFI/RTR/VIGL: Book -> "Chapter 1" container with 0 direct beats -> real
        // chapters -> beats) has real chapters two levels down, and direct-children-only
        // used to silently report "0 beats logged" for these books even though hundreds of
        // beats existed. Same bug class WorkflowMonitorService.GetNodeCoverageAsync fixed
        // 2026-08-09 via this same helper. Archived/unincorporated leaves aren't part of the
        // book — exclude them so the rollup reflects the actual canon chapters, not cut
        // scenes or draft scratch.
        // Preserve GetLeafDescendantIdsAsync's own return order rather than re-sorting by
        // Node.SortKey — SortKey is only comparable within one parent's sibling group, so a
        // flat re-sort across leaves from different branches would silently misorder anything
        // nested deeper than one split-collection level (same footgun documented in
        // NarrativeForkService/DcmVizCli for BeatNodes.SortKey; applies equally to Nodes here).
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, root.Id);
        var byId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(s => leafIds.Contains(s.Id)
                     && s.Status != "archived" && s.Status != "unincorporated")
            .Select(s => new { s.Id, s.Title, s.Slug, s.UniverseId })
            .ToDictionaryAsync(s => s.Id);
        var children = leafIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var chapters = children.Count > 0
            ? children
            : [root];

        Console.WriteLine($"\n=== Backfilling coverage: {root.Title} ({root.Slug}) ===");
        var totalBeatsLogged = 0;

        foreach (var ch in chapters)
        {
            // Enabled beats in reading order, joined through the BeatNodes bridge.
            var beats = await (
                from sb in db.BeatNodes.AsNoTracking()
                join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                where sb.NodeId == ch.Id && true
                orderby sb.SortKey
                select new { b.Id, b.Description, b.Title, b.Text }).ToListAsync();

            if (beats.Count == 0) continue;

            for (var i = 0; i < beats.Count; i++)
            {
                var beat = beats[i];
                // The description is the closest proxy to the original BeatGoal; fall back to
                // the title. A short prose tail sharpens mode detection (combat vs dialogue).
                var goal = !string.IsNullOrWhiteSpace(beat.Description) ? beat.Description
                         : beat.Title ?? "";
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
        Console.WriteLine($"Inspect with: prose --workflow-status --slug {root.Slug}");
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
