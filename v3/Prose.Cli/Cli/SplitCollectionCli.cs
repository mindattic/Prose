using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --split-collection (--slug &lt;s&gt; | --id &lt;guid|prefix&gt;)</c> — turn a
/// monolithic node into a Collection (ARCHITECTURE.md §2c): split its beats at
/// IsChapterStart boundaries into child nodes parented under it via
/// ParentNodeId. Beats are MOVED, never rewritten. Backs up to markdown first.
/// </summary>
public static class SplitCollectionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null;
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
            Console.Error.WriteLine("[split-collection] One of --slug or --id is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var export = services.GetRequiredService<ManuscriptExportService>();

        Guid sid; string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            Node? s;
            if (!string.IsNullOrWhiteSpace(slug)) s = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
            else if (Guid.TryParse(id, out var g)) s = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g);
            else s = await db.Nodes.AsNoTracking().Where(x => x.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (s == null) { Console.Error.WriteLine("[split-collection] Node not found (or id prefix ambiguous)."); return 1; }
            sid = s.Id; title = s.Title;
        }

        try
        {
            var backup = await export.ExportMarkdownAsync(sid);
            Console.WriteLine($"[split-collection] backup: {backup}");
        }
        catch (Exception ex) { Console.Error.WriteLine($"[split-collection] backup failed ({ex.Message}) — aborting."); return 1; }

        try
        {
            var (children, beats) = await workbench.SplitIntoCollectionAsync(sid);
            Console.WriteLine($"[split-collection] \"{title}\" → Collection of {children} child nodes ({beats} beats moved).");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[split-collection] {ex.Message}"); return 1; }
    }
}
