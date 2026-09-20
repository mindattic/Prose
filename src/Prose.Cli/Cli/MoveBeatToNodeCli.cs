using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --move-beat-to-node</c> — relocate a beat OUT of one chapter-level node and INTO
/// another, unlike <c>--move-beat</c> which only re-slots a beat among its existing siblings in
/// a single node.
///
/// Built 2026-08-31 while fixing a real VIGL logic-sweep finding: a scene ("The Seal") was
/// drafted around Lyra already possessing the Oculus, but the book's established recovery point
/// for the Oculus is a later chapter (Caer Glas Moor) — the actual defect was the beat sitting
/// in the wrong chapter, not its prose. Wraps NodeWorkbenchService.MoveBeatToNodeAsync.
///
///   --slug &lt;slug&gt;         FROM node slug — the BOOK or a chapter-level node; --beat-number
///                         is read against WHATEVER node this resolves to's own reading order
///                         (pass the book slug to address a beat by its whole-book position,
///                         same numbering --read-beats reports).
///   --beat-number &lt;N&gt;     1-indexed beat position (in --slug's reading order) to move. The
///                         beat's ACTUAL owning chapter is resolved via GetOrderedBeatsAsync's
///                         OrderedBeat.NodeId — not assumed to be --slug itself — so passing
///                         the book slug still moves the beat out of its real chapter.
///   --to-slug &lt;slug&gt;      TO node — book or chapter-level, same addressing rule as --slug.
///   --after &lt;N&gt;           1-indexed position, in TO's reading order, to place it after (the
///                         beat lands in THAT anchor's actual chapter, resolved the same way;
///                         0 = move to the very top of --to-slug itself, which for a book-level
///                         --to-slug must be a chapter's own slug, not the book's).
///
/// Exit codes: 0 = success, 1 = bad args / node not found / beat number out of range.
/// </summary>
public static class MoveBeatToNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, toSlug = null;
        int beatNumber = 0;
        int after = -1;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":        if (i + 1 < args.Length) slug = args[++i]; break;
                case "--to-slug":     if (i + 1 < args.Length) toSlug = args[++i]; break;
                case "--beat-number": if (i + 1 < args.Length) int.TryParse(args[++i], out beatNumber); break;
                case "--after":       if (i + 1 < args.Length) int.TryParse(args[++i], out after); break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[move-beat-to-node] --slug (FROM node) is required.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(toSlug))
        {
            Console.Error.WriteLine("[move-beat-to-node] --to-slug (TO node) is required.");
            return 1;
        }
        if (beatNumber < 1)
        {
            Console.Error.WriteLine("[move-beat-to-node] --beat-number must be >=1.");
            return 1;
        }
        if (after < 0)
        {
            Console.Error.WriteLine("[move-beat-to-node] --after is required (0 = move to top of TO).");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid fromNodeId, toNodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17 convention).
            var fromNode = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug);
            if (fromNode == null) { Console.Error.WriteLine($"[move-beat-to-node] FROM node '{slug}' not found."); return 1; }
            fromNodeId = fromNode.Id;

            var toNode = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Slug == toSlug);
            if (toNode == null) { Console.Error.WriteLine($"[move-beat-to-node] TO node '{toSlug}' not found."); return 1; }
            toNodeId = toNode.Id;
        }

        var fromOrdered = await workbench.GetOrderedBeatsAsync(fromNodeId);
        if (beatNumber > fromOrdered.Count)
        {
            Console.Error.WriteLine($"[move-beat-to-node] --beat-number {beatNumber} exceeds beat count ({fromOrdered.Count}) in '{slug}'.");
            return 1;
        }
        var subjectOrdered = fromOrdered[beatNumber - 1];
        var subject = subjectOrdered.Beat;
        var actualFromNodeId = subjectOrdered.NodeId; // the beat's real owning chapter, not necessarily fromNodeId itself

        var toOrdered = await workbench.GetOrderedBeatsAsync(toNodeId);
        if (after > toOrdered.Count)
        {
            Console.Error.WriteLine($"[move-beat-to-node] --after {after} exceeds beat count ({toOrdered.Count}) in '{toSlug}'.");
            return 1;
        }
        Guid? afterId;
        Guid actualToNodeId;
        if (after == 0)
        {
            afterId = null;
            actualToNodeId = toNodeId; // top of --to-slug itself — must be a chapter-level node
        }
        else
        {
            var anchor = toOrdered[after - 1];
            afterId = anchor.Beat.Id;
            actualToNodeId = anchor.NodeId; // the anchor's real owning chapter
        }

        Console.Write($"[move-beat-to-node] Moving beat #{beatNumber} (id {subject.Id}, chapter {actualFromNodeId}) from '{slug}' to chapter {actualToNodeId} (under '{toSlug}') after position {after}… ");
        await workbench.MoveBeatToNodeAsync(subject.Id, actualFromNodeId, actualToNodeId, afterId);
        Console.WriteLine("ok.");
        return 0;
    }
}
