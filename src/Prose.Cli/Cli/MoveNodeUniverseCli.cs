using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --move-node-universe (--slug &lt;slug&gt; | --id &lt;id&gt;) --to-universe &lt;universeSlug&gt;</c>
///
/// Relocates a book node (and its full descendant chapter subtree) into a different universe —
/// the CLI wrapper around <see cref="NodeWorkbenchService.MoveSubtreeToUniverseAsync"/>. Node
/// resolution uses <see cref="NodeRefResolver"/> (GUID/prefix/slug/NodeCode,
/// <c>IgnoreQueryFilters</c>) since the source node may live outside the ambient universe scope.
/// </summary>
public static class MoveNodeUniverseCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, toUniverse = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":           if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":         if (i + 1 < args.Length) slug = args[++i]; break;
                case "--to-universe":  if (i + 1 < args.Length) toUniverse = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[move-node-universe] --id or --slug required to identify the node.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(toUniverse))
        {
            Console.Error.WriteLine("[move-node-universe] --to-universe <universeSlug> is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var nodeId = await NodeRefResolver.ResolveAsync(db, slug ?? id);
        if (nodeId == null)
        {
            Console.Error.WriteLine($"[move-node-universe] Node '{slug ?? id}' not found.");
            return 1;
        }

        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == toUniverse);
        if (universe == null)
        {
            Console.Error.WriteLine($"[move-node-universe] Unknown universe slug '{toUniverse}'. Run 'prose --universe list' to see available universes.");
            return 1;
        }

        var node = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == nodeId.Value);
        var count = await workbench.MoveSubtreeToUniverseAsync(nodeId.Value, universe.Id);

        Console.WriteLine($"[move-node-universe] \"{node.Title}\" and {count - 1} descendant node(s) moved to universe '{universe.Slug}' ({universe.Id}). Total nodes updated: {count}.");
        return 0;
    }
}
