using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --mark-canon (--slug &lt;s&gt; | --id &lt;guid|prefix&gt;) [--off]</c> — the
/// author-only Canon trust gate (ARCHITECTURE.md §2c): mark a strand "strong
/// enough to draw conclusions about the characters and events." Canon strands are
/// what the voice-harvest learns from (`ss --harvest-voice --canon`). <c>--off</c>
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

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<StrandWorkbenchService>();

        Guid strandId; string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var q = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug)) strand = await q.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var g)) strand = await q.FirstOrDefaultAsync(s => s.Id == g);
            else strand = await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync() switch
            { { Count: 1 } m => m[0], _ => null };
            if (strand == null) { Console.Error.WriteLine("[mark-canon] Strand not found."); return 1; }
            strandId = strand.Id; title = strand.Title;
        }

        await workbench.SetCanonAsync(strandId, !off);
        Console.WriteLine($"[mark-canon] \"{title}\" canon = {(!off).ToString().ToLowerInvariant()}.");
        if (!off) Console.WriteLine("[mark-canon] Harvest its voice into the rules: ss --harvest-voice --canon");
        return 0;
    }
}
