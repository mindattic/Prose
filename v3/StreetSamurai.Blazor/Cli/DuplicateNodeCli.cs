using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --duplicate-story (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) --title "New Title"</c>
/// — deep-copy a node (and its sub-node tree) into a fresh independent node.
/// Every beat becomes a new row (prose + metadata preserved; audio/score/stale
/// reset), so editing the copy never touches the original.
/// </summary>
public static class DuplicateNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, title = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":    if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":  if (i + 1 < args.Length) slug = args[++i]; break;
                case "--title": if (i + 1 < args.Length) title = args[++i]; break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[duplicate-story] One of --id or --slug is required.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.Error.WriteLine("[duplicate-story] --title \"New Title\" is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid sourceId; string sourceTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
            else node = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (node == null) { Console.Error.WriteLine("[duplicate-story] Source node not found."); return 1; }
            sourceId = node.Id; sourceTitle = node.Title;
        }

        Console.WriteLine($"[duplicate-story] Duplicating \"{sourceTitle}\" -> \"{title}\"…");
        try
        {
            var (newId, newSlug) = await workbench.DuplicateNodeAsync(sourceId, title!);
            Console.WriteLine($"[duplicate-story] OK");
            Console.WriteLine($"   Id:    {newId}");
            Console.WriteLine($"   Slug:  {newSlug}");
            Console.WriteLine($"   Title: {title}");
            Console.WriteLine($"   URL:   https://localhost:7103/node/{newSlug}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[duplicate-story] Failed: {ex.Message}"); return 1; }
    }
}
