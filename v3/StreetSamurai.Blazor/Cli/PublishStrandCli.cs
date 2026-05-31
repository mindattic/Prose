using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --publish-strand</c> — stitch an existing strand's recorded beats into
/// one combined file (WAV → final MP3), drop a friendly copy in the publish
/// output directory (Downloads by default), and record the 1:M publication run
/// plus its process-event ledger. Resolves the strand by id (full or prefix)
/// or slug. Headless equivalent of the in-app Publish button.
///
/// Args (one of --id / --slug required):
///   --id &lt;guid|prefix&gt;  Strand id; a unique prefix is enough.
///   --slug &lt;slug&gt;       Strand slug.
///
/// Exit codes: 0 — published; 1 — bad args / not found / nothing to publish.
/// </summary>
public static class PublishStrandCli
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
            Console.Error.WriteLine("[publish-strand] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --publish-strand (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<StrandWorkbenchService>();

        Guid strandId; string strandSlug, strandTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Strands.AsNoTracking();
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug))
                strand = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                strand = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[publish-strand] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                    return 1;
                }
                strand = matches.FirstOrDefault();
            }

            if (strand == null)
            {
                Console.Error.WriteLine($"[publish-strand] No strand found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            strandId = strand.Id; strandSlug = strand.Slug; strandTitle = strand.Title;
        }

        Console.WriteLine($"[publish-strand] Publishing:");
        Console.WriteLine($"   Id:    {strandId}");
        Console.WriteLine($"   Slug:  {strandSlug}");
        Console.WriteLine($"   Title: {strandTitle}");

        string? rel;
        try
        {
            rel = await workbench.ExportCombinedAsync(strandId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[publish-strand] Publish failed: {ex.Message}");
            return 1;
        }

        if (rel == null)
        {
            Console.Error.WriteLine("[publish-strand] Nothing to publish — record beats first (or beats are mixed-format).");
            return 1;
        }

        // Report the recorded publication run + ledger so the CLI run is self-verifying.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var pub = await db.StrandPublications.AsNoTracking()
                .Where(p => p.StrandId == strandId)
                .OrderByDescending(p => p.StartedAt)
                .FirstOrDefaultAsync();
            var eventCount = await db.StrandAudioEvents.CountAsync(e => e.StrandId == strandId);
            Console.WriteLine($"[publish-strand] Combined internal path: {rel}");
            if (pub != null)
            {
                Console.WriteLine($"[publish-strand] Publication: {pub.Status}, {pub.Format}, {pub.BeatCount} beats, {pub.ByteSize:N0} bytes");
                Console.WriteLine($"[publish-strand] Exported to: {pub.Path}");
            }
            Console.WriteLine($"[publish-strand] Audio-event ledger rows for this strand: {eventCount}");
        }
        Console.WriteLine("[publish-strand] Done.");
        return 0;
    }
}
