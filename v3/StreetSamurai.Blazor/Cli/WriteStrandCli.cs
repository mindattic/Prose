using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --write-strand</c> — create a new strand via the bible-first workflow:
///
///   1. Insert a Strand row (status=draft, no beats yet).
///   2. Call StrandBibleService to generate the strand bible and planned beats.
///   3. Print the bible + URL. Stop here with <c>--bible-only</c>.
///   4. With <c>--narrate</c>, also run TTS after the prose pass (future).
///
/// The bible's ## BEAT SPINE section is parsed into Beat rows with Synopsis set
/// to the planned goal. Open the strand in the UI to expand beats into prose.
///
/// Args:
///   --seed "..."         One-line prompt that drives the bible. Required.
///   --title "..."        Override the auto-generated working title.
///   --kind &lt;k&gt;          Kind tag: "episode" (default), "vignette", "chapter", etc.
///   --beats N            Target beat count in the spine (default: 12).
///   --bible-only         Stop after generating the bible; do not open the URL.
///   --narrate            (placeholder) Run TTS after prose expansion.
/// </summary>
public static class WriteStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? seed = null, title = null;
        string kind = "episode";
        int targetBeats = 12;
        bool bibleOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed":       if (i + 1 < args.Length) seed       = args[++i]; break;
                case "--title":      if (i + 1 < args.Length) title      = args[++i]; break;
                case "--kind":       if (i + 1 < args.Length) kind       = args[++i]; break;
                case "--beats":      if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) targetBeats = n; break;
                case "--bible-only": bibleOnly = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(seed))
        {
            Console.Error.WriteLine("[write-strand] --seed is required.");
            Console.Error.WriteLine("Usage: ss --write-strand --seed \"...\" [--title \"...\"] [--kind episode] [--beats 12] [--bible-only]");
            return 2;
        }

        var dbFactory  = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var bibleService = services.GetRequiredService<StrandBibleService>();

        // 1. Create the strand record
        var strandId = Guid.CreateVersion7();
        var workingTitle = !string.IsNullOrEmpty(title) ? title : DeriveTitle(seed);
        var slug = EpisodeGeneratorService.Slugify(workingTitle) + "-" + strandId.ToString("N")[..8];

        Console.WriteLine($"[write-strand] Creating strand: \"{workingTitle}\"");

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Strands.Add(new Strand
            {
                Id        = strandId,
                Title     = workingTitle,
                Slug      = slug,
                Seed      = seed,
                Kind      = kind,
                Status    = "draft",
                Synopsis  = seed.Length > 200 ? seed[..200] : seed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        Console.WriteLine($"[write-strand] Strand created: {strandId}");

        // 2. Generate the bible (and planned beats)
        Console.WriteLine($"[write-strand] Generating strand bible ({targetBeats} beats)…");
        string bibleText;
        try
        {
            bibleText = await bibleService.GenerateAndSaveAsync(strandId, seed!, workingTitle, targetBeats);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[write-strand] Bible generation failed: {ex.Message}");
            return 1;
        }

        // 3. Print the bible
        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine(bibleText);
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine();

        // Report planned beats
        var beatPlans = StrandBibleService.ParseBeatSpine(bibleText);
        Console.WriteLine($"[write-strand] {beatPlans.Count} planned beats created from the spine.");

        var url = $"https://localhost:7103/strand/{slug}";
        Console.WriteLine($"[write-strand] Open in the unified writer to expand beats into prose:");
        Console.WriteLine($"   Id:    {strandId}");
        Console.WriteLine($"   Slug:  {slug}");
        Console.WriteLine($"   Title: {workingTitle}");
        Console.WriteLine($"   Kind:  {kind}");
        Console.WriteLine($"   Beats: {beatPlans.Count} planned (prose not yet written)");
        Console.WriteLine($"   URL:   {url}");

        if (!bibleOnly)
            Console.WriteLine("   Next:  open the URL, then click ✨ on each beat to write prose from the plan.");

        return 0;
    }

    private static string DeriveTitle(string seed)
    {
        // Use the first ~8 words of the seed as a working title
        var words = seed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleWords = words.Take(8);
        var raw = string.Join(" ", titleWords);
        return raw.Length < seed.Length ? raw + "…" : raw;
    }
}
