using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --set-previous-node (--slug &lt;slug&gt; | --id &lt;id&gt;) (--previous-slug &lt;slug&gt; | --previous-id &lt;id&gt;)
/// — sets a book node's sequel link (Node.PreviousNodeId).
/// Use --clear to detach (no longer a sequel to anything).
///
/// Exists to unblock deleting/renaming a book another book's PreviousNodeId points at —
/// NodeWorkbenchService.DeleteNodeAsync refuses (DB-level FK_Nodes_PreviousNode is Restrict,
/// not just a C#-level guard, so --force on delete-node cannot bypass it) until the referencing
/// node's link is repointed or cleared first. Delegates to
/// <see cref="NodeWorkbenchService.SetPreviousNodeAsync"/> — the sanctioned write path (previously
/// only reachable from the Blazor UI, no CLI/MCP surface existed).
/// </summary>
public static class SetPreviousNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, previousId = null, previousSlug = null;
        bool clear = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":            if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":          if (i + 1 < args.Length) slug = args[++i]; break;
                case "--previous-id":   if (i + 1 < args.Length) previousId = args[++i]; break;
                case "--previous-slug": if (i + 1 < args.Length) previousSlug = args[++i]; break;
                case "--clear":         clear = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[set-previous-node] --id or --slug required to identify the node.");
            return 1;
        }
        if (!clear && string.IsNullOrWhiteSpace(previousId) && string.IsNullOrWhiteSpace(previousSlug))
        {
            Console.Error.WriteLine("[set-previous-node] --previous-id or --previous-slug required (or --clear to detach).");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // The shared resolver: NodeCode, other universes (a sequel may live in another one).
        var child = await Prose.Core.Services.NodeRefResolver.ResolveNodeAsync(db, slug ?? id);

        if (child == null) { Console.Error.WriteLine("[set-previous-node] Node not found."); return 1; }

        if (clear)
        {
            await workbench.SetPreviousNodeAsync(child.Id, null);
            Console.WriteLine($"[set-previous-node] \"{child.Title}\" PreviousNodeId cleared.");
            return 0;
        }

        var previous = await Prose.Core.Services.NodeRefResolver.ResolveNodeAsync(db, previousSlug ?? previousId);

        if (previous == null) { Console.Error.WriteLine("[set-previous-node] Previous node not found."); return 1; }

        try
        {
            await workbench.SetPreviousNodeAsync(child.Id, previous.Id);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[set-previous-node] {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[set-previous-node] \"{child.Title}\" -> previous \"{previous.Title}\".");
        return 0;
    }
}
