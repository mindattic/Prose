using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rename-node --node &lt;id|slug|code&gt; --title "…" [--slug &lt;new-slug&gt;]</c>
///
/// Set a node's Title and, optionally, its Slug — the CLI twin of MCP <c>rename_node</c>
/// (<see cref="NodeRenameService"/>). A new slug is pinned and moves every slug-carrying reference
/// with it, exactly like <c>--set-node-slug --apply</c>. Validated before anything is written.
/// Exit codes: 0 renamed · 1 bad args, node not found, or refused.
/// </summary>
public static class RenameNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }

        var node = Flag("--node");
        var title = Flag("--title");
        var slug = Flag("--slug");
        if (string.IsNullOrWhiteSpace(node) || string.IsNullOrWhiteSpace(title))
        {
            Console.Error.WriteLine("Usage: prose --rename-node --node <id|slug|code> --title \"…\" [--slug <new-slug>]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        if (await NodeRefResolver.ResolveAsync(dbFactory, node) is not { } nodeId)
        {
            Console.Error.WriteLine($"[rename-node] {NodeRefResolver.NotFoundMessage(node)}");
            return 1;
        }

        try
        {
            var r = await services.GetRequiredService<NodeRenameService>().RenameAsync(nodeId, title, slug);
            Console.WriteLine($"[rename-node] {r.Id} ({r.Kind})");
            Console.WriteLine($"  title: \"{r.OldTitle}\" → \"{r.NewTitle}\"");
            Console.WriteLine(r.OldSlug == r.NewSlug ? $"  slug:  {r.NewSlug} (unchanged)" : $"  slug:  {r.OldSlug} → {r.NewSlug} (pinned)");
            foreach (var fx in r.SlugSideEffects) Console.WriteLine($"    + {fx}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[rename-node] REFUSED: {ex.Message}");
            return 1;
        }
    }
}
