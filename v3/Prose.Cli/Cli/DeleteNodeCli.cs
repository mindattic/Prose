using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --delete-node --id &lt;guid&gt;</c> — hard-delete a node and its BeatNode
/// memberships. Beats that are exclusively owned by this node are also deleted.
///
/// HARD RULE: never use raw sqlcmd DELETE on Nodes — use this command instead.
/// </summary>
public static class DeleteNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var idStr = args.SkipWhile(a => a != "--id").Skip(1).FirstOrDefault();
        if (!Guid.TryParse(idStr, out var deleteNodeId))
        {
            Console.Error.WriteLine("Usage: prose --delete-node --id <guid>");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var target = await db.Nodes.FindAsync(deleteNodeId);
        if (target == null) { Console.Error.WriteLine($"Node {deleteNodeId} not found."); return 1; }

        // 2026-08-09 bug fix: this used to cascade exactly one level deep ("nested chapters
        // are not supported", per the comment it replaces) — a child that was itself a split
        // Collection (chapter -> N sub-chapters -> beats, e.g. any book split via
        // --split-collection) left its own grandchildren untouched, so db.Nodes.Remove on that
        // mid-level chapter would hit FK_Nodes_ParentNode (grandchildren still reference it).
        // Deletion removes NODES themselves, not just beats, so the fix isn't the usual
        // GetLeafDescendantIdsAsync swap (that returns only leaves) — it needs a genuinely
        // recursive, depth-first, POST-order walk: fully delete every child's own subtree
        // before deleting the child, at any depth, then finally the target itself.
        async Task DeleteNodeSubtreeAsync(Guid id, int depth)
        {
            var childIds = await db.Nodes.Where(n => n.ParentNodeId == id).Select(n => n.Id).ToListAsync();
            foreach (var childId in childIds)
                await DeleteNodeSubtreeAsync(childId, depth + 1);

            var beatIds = await db.BeatNodes.Where(bn => bn.NodeId == id).Select(bn => bn.BeatId).ToListAsync();
            var sharedIds = await db.BeatNodes.Where(bn => beatIds.Contains(bn.BeatId) && bn.NodeId != id).Select(bn => bn.BeatId).Distinct().ToListAsync();
            var exclusiveIds = beatIds.Except(sharedIds).ToList();

            var blueprintIds = await db.NodeStructuralBlueprints.Where(bp => bp.NodeId == id).Select(bp => bp.Id).ToListAsync();
            if (blueprintIds.Count > 0)
            {
                db.NodeStructuralBlueprintBeatTags.RemoveRange(await db.NodeStructuralBlueprintBeatTags.Where(t => blueprintIds.Contains(t.BlueprintId)).ToListAsync());
                db.NodeStructuralBlueprints.RemoveRange(await db.NodeStructuralBlueprints.Where(bp => blueprintIds.Contains(bp.Id)).ToListAsync());
            }

            db.BeatNodes.RemoveRange(await db.BeatNodes.Where(bn => bn.NodeId == id).ToListAsync());
            if (exclusiveIds.Count > 0)
            {
                var beats = await db.Beats.Where(b => exclusiveIds.Contains(b.Id)).ToListAsync();
                db.Beats.RemoveRange(beats);
                Console.WriteLine($"  {new string(' ', depth * 2)}Deleting {beats.Count} exclusive beat(s) for {id}.");
            }

            var node = await db.Nodes.FindAsync(id);
            if (node != null)
            {
                db.Nodes.Remove(node);
                Console.WriteLine($"  {new string(' ', depth * 2)}→ {node.Title} ({id})");
            }
        }

        await DeleteNodeSubtreeAsync(deleteNodeId, 0);
        await db.SaveChangesAsync();
        Console.WriteLine($"[delete-node] Deleted: {target.Title} ({deleteNodeId})");
        return 0;
    }
}
