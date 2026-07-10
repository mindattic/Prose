using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --prune-disabled --slug &lt;slug&gt; [--dry-run]</c>
///
/// Hard-deletes every disabled (IsEnabled=false) beat from a story node and its
/// chapter children (SS-A43). Use this when the story is publish-ready and you
/// want to permanently remove placeholder beats that will never be used.
///
/// Safety:
///   • Requires explicit --slug / --id (no wildcards).
///   • Lists every beat that will be deleted and pauses for confirmation
///     unless --yes is passed.
///   • --dry-run previews without touching the DB.
///   • Temporal history (FOR SYSTEM_TIME ALL) still has every deleted beat;
///     recovery is possible but requires a DBA-level temporal query.
/// </summary>
public static class PruneDisabledCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slugOrId = null;
        bool dryRun = false, yes = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":
                case "--id":   if (i + 1 < args.Length) slugOrId = args[++i]; break;
                case "--dry-run": dryRun = true; break;
                case "--yes":     yes    = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slugOrId))
        {
            Console.Error.WriteLine("[prune-disabled] --slug <slug|id> is required.");
            Console.Error.WriteLine("  ss --prune-disabled --slug <slug> [--dry-run] [--yes]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Resolve the story node.
        var node = Guid.TryParse(slugOrId, out var gid)
            ? await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == gid)
            : await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slugOrId || s.NodeCode == slugOrId);

        if (node == null)
        {
            Console.Error.WriteLine($"[prune-disabled] Node '{slugOrId}' not found.");
            return 1;
        }

        // SS-A43: collect all node IDs (story + chapter children).
        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == node.Id)
            .Select(n => n.Id).ToListAsync();
        var nodeIds = childIds.Count > 0
            ? childIds
            : new List<Guid> { node.Id };

        // Find disabled BeatNodes.
        var disabledLinks = await db.BeatNodes.AsNoTracking()
            .Where(bn => nodeIds.Contains(bn.NodeId) && !bn.IsEnabled)
            .OrderBy(bn => bn.SortKey)
            .ToListAsync();

        if (disabledLinks.Count == 0)
        {
            Console.WriteLine($"[prune-disabled] '{node.Title}' has no disabled beats. Nothing to do.");
            return 0;
        }

        var beatIds = disabledLinks.Select(bn => bn.BeatId).ToList();
        var beats = await db.Beats.AsNoTracking()
            .Where(b => beatIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id);

        Console.WriteLine($"[prune-disabled] Node: {node.Title} ({node.Slug})");
        Console.WriteLine($"[prune-disabled] {disabledLinks.Count} disabled beat(s) to delete:");
        Console.WriteLine();

        foreach (var link in disabledLinks)
        {
            beats.TryGetValue(link.BeatId, out var beat);
            var label = beat?.Title ?? beat?.Description ?? "(no title)";
            var prose = beat?.Text;
            var proseSnippet = string.IsNullOrWhiteSpace(prose)
                ? "[no prose]"
                : prose.Length > 80 ? prose[..80].Replace('\n', ' ') + "…" : prose.Replace('\n', ' ');
            Console.WriteLine($"  beat {link.BeatId} | sk={link.SortKey:F0} | {label}");
            Console.WriteLine($"    {proseSnippet}");
        }

        Console.WriteLine();

        if (dryRun)
        {
            Console.WriteLine("[prune-disabled] --dry-run: no changes made.");
            return 0;
        }

        if (!yes)
        {
            Console.Write($"[prune-disabled] Permanently delete {disabledLinks.Count} beat(s)? [y/N] ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer != "y" && answer != "yes")
            {
                Console.WriteLine("[prune-disabled] Aborted.");
                return 0;
            }
        }

        // Hard-delete: remove BeatNodes first, then orphaned Beats.
        var linkBeatIds = disabledLinks.Select(l => l.BeatId).ToHashSet();

        // Re-open a write context (the read context above used AsNoTracking).
        await using var wdb = await dbFactory.CreateDbContextAsync();

        var linksToDelete = await wdb.BeatNodes
            .Where(bn => nodeIds.Contains(bn.NodeId) && !bn.IsEnabled)
            .ToListAsync();
        wdb.BeatNodes.RemoveRange(linksToDelete);
        await wdb.SaveChangesAsync();

        // Only delete Beat rows that are no longer referenced by ANY BeatNode.
        var stillReferenced = await wdb.BeatNodes.AsNoTracking()
            .Where(bn => linkBeatIds.Contains(bn.BeatId))
            .Select(bn => bn.BeatId)
            .ToListAsync();
        var orphanBeatIds = linkBeatIds.Except(stillReferenced).ToList();
        if (orphanBeatIds.Count > 0)
        {
            var orphans = await wdb.Beats.Where(b => orphanBeatIds.Contains(b.Id)).ToListAsync();
            wdb.Beats.RemoveRange(orphans);
            await wdb.SaveChangesAsync();
        }

        Console.WriteLine($"[prune-disabled] Deleted {linksToDelete.Count} BeatNode(s) and {orphanBeatIds.Count} Beat row(s).");
        Console.WriteLine($"[prune-disabled] Temporal history retains all deleted beats for forensic recovery.");
        return 0;
    }
}
