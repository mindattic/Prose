using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --duplicate-strand (--id &lt;guid|prefix&gt; | --slug &lt;slug&gt;) --title "New Title"</c>
/// — deep-copy a strand (and its sub-strand tree) into a fresh independent strand.
/// Every beat becomes a new row (prose + metadata preserved; audio/score/stale
/// reset), so editing the copy never touches the original.
/// </summary>
public static class DuplicateStrandCli
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
            Console.Error.WriteLine("[duplicate-strand] One of --id or --slug is required.");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.Error.WriteLine("[duplicate-strand] --title \"New Title\" is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<StrandWorkbenchService>();

        Guid sourceId; string sourceTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug)) strand = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) strand = await q.FirstOrDefaultAsync(s => s.Id == g);
            else strand = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (strand == null) { Console.Error.WriteLine("[duplicate-strand] Source strand not found."); return 1; }
            sourceId = strand.Id; sourceTitle = strand.Title;
        }

        Console.WriteLine($"[duplicate-strand] Duplicating \"{sourceTitle}\" -> \"{title}\"…");
        try
        {
            var (newId, newSlug) = await workbench.DuplicateStrandAsync(sourceId, title!);
            Console.WriteLine($"[duplicate-strand] OK");
            Console.WriteLine($"   Id:    {newId}");
            Console.WriteLine($"   Slug:  {newSlug}");
            Console.WriteLine($"   Title: {title}");
            Console.WriteLine($"   URL:   https://localhost:7103/strand/{newSlug}");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine($"[duplicate-strand] Failed: {ex.Message}"); return 1; }
    }
}
