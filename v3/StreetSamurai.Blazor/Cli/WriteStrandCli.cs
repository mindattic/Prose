using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --write-strand</c> — generate a new strand via the existing
/// LLM-driven episode pipeline, then run the strand migration to copy
/// the freshly-written rows into the unified <c>Beat</c>/<c>Strand</c>
/// schema. Output: the new Strand's slug + URL.
///
/// Args:
///   --seed "..."     The one-line prompt fed to the generator.
///   --voice &lt;id&gt;  Optional ElevenLabs voice id (audio rendering is OFF
///                      by default in CLI mode — set --narrate to enable).
///   --kind &lt;k&gt;   Kind tag on the resulting strand (default: "episode").
///   --title "..."    Override the auto-generated title.
///   --narrate        Also run TTS after generation finishes.
/// </summary>
public static class WriteStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? seed = null, voice = null, kind = "episode", title = null;
        bool narrate = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed":  if (i + 1 < args.Length) seed = args[++i]; break;
                case "--voice": if (i + 1 < args.Length) voice = args[++i]; break;
                case "--kind":  if (i + 1 < args.Length) kind = args[++i]; break;
                case "--title": if (i + 1 < args.Length) title = args[++i]; break;
                case "--narrate": narrate = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(seed))
        {
            Console.Error.WriteLine("[write-strand] --seed is required.");
            Console.Error.WriteLine("Usage: ss --write-strand --seed \"...\" [--voice id] [--kind episode] [--title \"...\"] [--narrate]");
            return 2;
        }

        var generator = services.GetRequiredService<EpisodeGeneratorService>();
        var migration = services.GetRequiredService<StrandMigrationService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var audio     = services.GetRequiredService<StrandWorkbenchService>();

        Console.WriteLine($"[write-strand] Generating episode from seed: {seed}");
        var episodeId = await generator.GenerateFromSeedAsync(seed!, voice);
        Console.WriteLine($"[write-strand] Episode generated: {episodeId}. Migrating into strand schema…");

        // Apply title override on the Episode BEFORE migration so the
        // migration's slug derivation uses the user's title, not the
        // LLM-generated one. (Migration computes slug = Slugify(Episode.Title)
        // + 8-char id suffix; doing this after migration would leave the slug
        // tied to the old title and the printed URL would point at a
        // non-existent route.)
        if (!string.IsNullOrEmpty(title))
        {
            await using var preDb = await dbFactory.CreateDbContextAsync();
            var ep = await preDb.Episodes.FirstOrDefaultAsync(e => e.Id == episodeId);
            if (ep != null)
            {
                ep.Title = title!;
                ep.Slug  = ""; // force migration to recompute from new title
                await preDb.SaveChangesAsync();
            }
        }

        // 2. Migrate the new episode → strand. Idempotent: existing strands skip.
        var report = await migration.MigrateAllAsync();
        Console.WriteLine($"[write-strand] Migration: {report}");

        // 3. Apply user-requested kind override (slug + title now coherent).
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Id == episodeId);
        if (strand == null)
        {
            Console.Error.WriteLine("[write-strand] Strand row missing after migration — generation succeeded but migration didn't pick it up.");
            return 1;
        }
        if (!string.IsNullOrEmpty(kind))  strand.Kind  = kind;
        strand.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var beatCount = await db.StrandBeats.CountAsync(sb => sb.StrandId == strand.Id);

        Console.WriteLine($"[write-strand] Strand ready — open in the unified writer/recorder/listener:");
        Console.WriteLine($"   Id:    {strand.Id}");
        Console.WriteLine($"   Slug:  {strand.Slug}");
        Console.WriteLine($"   Title: {strand.Title}");
        Console.WriteLine($"   Kind:  {strand.Kind}");
        Console.WriteLine($"   Beats: {beatCount}");
        Console.WriteLine($"   URL:   https://localhost:7103/strand/{strand.Slug}");
        Console.WriteLine($"          (or {Environment.GetEnvironmentVariable("CYPRESS_BASE_URL") ?? "http://localhost:5101"}/strand/{strand.Slug})");
        Console.WriteLine($"   Next:  click Record to narrate the strand, or open the URL to edit beats first.");

        if (narrate)
        {
            Console.WriteLine($"[write-strand] Narrating beats…");
            try { await audio.NarrateAsync(strand.Id); }
            catch (Exception ex) { Console.Error.WriteLine($"[write-strand] Narration failed: {ex.Message}"); return 1; }
            Console.WriteLine($"[write-strand] Narration complete.");
        }

        return 0;
    }
}
