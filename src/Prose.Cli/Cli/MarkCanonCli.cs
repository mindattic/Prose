using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --mark-canon (--slug &lt;s&gt; | --id &lt;guid|prefix&gt;) [--off]</c> — the
/// author-only Canon trust gate (ARCHITECTURE.md §2c): mark a node "strong
/// enough to draw conclusions about the characters and events." Canon nodes are
/// what the voice-harvest learns from (`prose --harvest-voice --canon`). <c>--off</c>
/// clears it.
/// </summary>
public static class MarkCanonCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null;
        bool off = args.Contains("--off");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[mark-canon] One of --slug or --id is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid nodeId; string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await Prose.Core.Services.NodeRefResolver.ResolveNodeAsync(db, slug ?? id); // NodeCode, other universes
            if (node == null) { Console.Error.WriteLine("[mark-canon] Node not found."); return 1; }
            nodeId = node.Id; title = node.Title;
        }

        await workbench.SetCanonAsync(nodeId, !off);
        Console.WriteLine($"[mark-canon] \"{title}\" canon = {(!off).ToString().ToLowerInvariant()}.");
        if (!off) Console.WriteLine("[mark-canon] Harvest its voice into the rules: prose --harvest-voice --canon");
        return 0;
    }
}
