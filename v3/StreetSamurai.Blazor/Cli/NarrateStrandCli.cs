using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --narrate-strand</c> — (re)run TTS narration on an EXISTING strand,
/// resolved by id (full or prefix) or slug. The complement to
/// <c>--write-strand --narrate</c> (which only narrates a strand it just
/// generated). Runs the same <see cref="StrandWorkbenchService.NarrateAsync"/>
/// path the Record button uses, then prints the per-run tally.
///
/// Args (one of --id / --slug required):
///   --id &lt;guid|prefix&gt;  Strand id; a unique prefix is enough.
///   --slug &lt;slug&gt;       Strand slug.
///
/// Exit codes:
///   0 — strand finished with status "ready" (every beat rendered).
///   1 — bad args / strand not found / finished "failed" (some beats failed).
/// </summary>
public static class NarrateStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[narrate-strand] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --narrate-strand (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<StrandWorkbenchService>();

        Guid strandId;
        string strandSlug, strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug))
            {
                strand = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            }
            else if (Guid.TryParse(id, out var exact))
            {
                strand = await query.FirstOrDefaultAsync(s => s.Id == exact);
            }
            else
            {
                // Prefix match on the id's string form (e.g. "019e609c").
                var prefix = id!.ToLowerInvariant();
                var matches = await query
                    .Where(s => s.Id.ToString().StartsWith(prefix))
                    .Take(2)
                    .ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[narrate-strand] Id prefix '{id}' is ambiguous — matches multiple strands. Use a longer prefix or the full id.");
                    return 1;
                }
                strand = matches.FirstOrDefault();
            }

            if (strand == null)
            {
                Console.Error.WriteLine($"[narrate-strand] No strand found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            strandId    = strand.Id;
            strandSlug  = strand.Slug;
            strandTitle = strand.Title;
        }

        Console.WriteLine($"[narrate-strand] Narrating strand:");
        Console.WriteLine($"   Id:    {strandId}");
        Console.WriteLine($"   Slug:  {strandSlug}");
        Console.WriteLine($"   Title: {strandTitle}");
        Console.WriteLine($"[narrate-strand] Running TTS — this may take a while…");

        try
        {
            await workbench.NarrateAsync(strandId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[narrate-strand] Narration crashed: {ex.Message}");
            return 1;
        }

        // NarrateAsync swallows per-beat failures and records the outcome on the
        // strand row — re-read it to report the tally and pick the exit code.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var done = await db.Strands.AsNoTracking().FirstAsync(s => s.Id == strandId);
            Console.WriteLine($"[narrate-strand] Status: {done.Status}  ({done.NarratedBeatCount}/{done.TotalBeatsToNarrate} beats narrated)");
            if (!string.IsNullOrEmpty(done.Error))
                Console.WriteLine($"[narrate-strand] Error: {done.Error}");
            return done.Status == "ready" ? 0 : 1;
        }
    }
}
