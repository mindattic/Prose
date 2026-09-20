using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --delete-node --id &lt;guid&gt; [--force]</c> — hard-delete a node and its BeatNode
/// memberships. Beats that are exclusively owned by this node are also deleted.
///
/// HARD RULE: never use raw sqlcmd DELETE on Nodes — use this command instead.
///
/// Write-gate Phase 1 (2026-08-22 gap fix): the actual delete is now
/// <see cref="NodeWorkbenchService.DeleteNodeAsync"/> — this CLI resolved to a thin wrapper
/// around it, closing a gap where Phase 0 built the sanctioned method (moved verbatim from here)
/// but never rewired this file onto it. <c>--force</c> bypasses the method's
/// referenced-as-PreviousNodeId guard.
/// </summary>
public static class DeleteNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var idStr = args.SkipWhile(a => a != "--id").Skip(1).FirstOrDefault();
        if (!Guid.TryParse(idStr, out var deleteNodeId))
        {
            Console.Error.WriteLine("Usage: prose --delete-node --id <guid> [--force]");
            return 1;
        }
        var force = args.Contains("--force");

        await using var scope = services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = scope.ServiceProvider.GetRequiredService<NodeWorkbenchService>();
        await using var db = await dbFactory.CreateDbContextAsync();
        var target = await db.Nodes.FindAsync(deleteNodeId);
        if (target == null) { Console.Error.WriteLine($"Node {deleteNodeId} not found."); return 1; }

        try
        {
            await workbench.DeleteNodeAsync(deleteNodeId, force);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[delete-node] {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[delete-node] Deleted: {target.Title} ({deleteNodeId})");
        return 0;
    }
}
