using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --split-collection (--slug &lt;s&gt; | --id &lt;guid|prefix&gt;)</c> — turn a
/// monolithic strand into a Collection (ARCHITECTURE.md §2c): split its beats at
/// IsChapterStart boundaries into child strands parented under it via
/// ParentStrandId. Beats are MOVED, never rewritten. Backs up to markdown first.
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

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<StrandWorkbenchService>();
        var export = services.GetRequiredService<ManuscriptExportService>();

        Guid sid; string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            Strand? s;
            if (!string.IsNullOrWhiteSpace(slug)) s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
            else if (Guid.TryParse(id, out var g)) s = await db.Strands.AsNoTracking().FirstOrDefaultAsync(x => x.Id == g);
            else s = await db.Strands.AsNoTracking().Where(x => x.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (s == null) { Console.Error.WriteLine("[split-collection] Strand not found (or id prefix ambiguous)."); return 1; }
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
            Console.WriteLine($"[split-collection] \"{title}\" → Collection of {children} child strands ({beats} beats moved).");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[split-collection] {ex.Message}"); return 1; }
    }
}
