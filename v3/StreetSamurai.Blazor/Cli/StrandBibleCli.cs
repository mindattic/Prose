using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --bible-strand</c> — (re)generate the strand bible for an existing strand.
///
/// Use this to add a bible to a strand created before the bible system existed,
/// or to regenerate the plan when the story direction changes.
///
/// Args:
///   --slug &lt;slug&gt;    Target strand by slug. Required.
///   --beats N        Target beat count in the bible spine (default: use existing beat count or 12).
///   --replace-beats  Delete existing planned beats and recreate from the new spine.
/// </summary>
public static class StrandBibleCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        int targetBeats = 0;
        bool replaceBeats = args.Contains("--replace-beats");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":  if (i + 1 < args.Length) slug        = args[++i]; break;
                case "--beats": if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) targetBeats = n; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[bible-strand] --slug is required.");
            Console.Error.WriteLine("Usage: ss --bible-strand --slug <slug> [--beats N] [--replace-beats]");
            return 2;
        }

        var dbFactory    = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var bibleService = services.GetRequiredService<StrandBibleService>();

        // Resolve strand
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.FirstOrDefaultAsync(s => s.Slug == slug);
        if (strand == null)
        {
            Console.Error.WriteLine($"[bible-strand] No strand found with slug '{slug}'.");
            return 1;
        }

        var seed = strand.Seed ?? strand.Synopsis ?? strand.Title;
        if (string.IsNullOrWhiteSpace(seed))
        {
            Console.Error.WriteLine($"[bible-strand] Strand '{slug}' has no Seed or Synopsis to drive generation. Set one first.");
            return 1;
        }

        // Determine target beat count
        if (targetBeats <= 0)
        {
            targetBeats = await db.StrandBeats.CountAsync(sb => sb.StrandId == strand.Id && sb.IsEnabled);
            if (targetBeats <= 0) targetBeats = 12;
        }

        Console.WriteLine($"[bible-strand] Strand: {strand.Title} ({strand.Id})");
        Console.WriteLine($"[bible-strand] Seed: {seed}");
        Console.WriteLine($"[bible-strand] Target beats: {targetBeats}");

        if (replaceBeats)
        {
            // Remove existing planned beats (empty prose only — don't nuke written beats)
            var emptyBeats = await db.StrandBeats
                .Where(sb => sb.StrandId == strand.Id && sb.IsEnabled)
                .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { sb, b })
                .Where(x => string.IsNullOrEmpty(x.b.Text))
                .ToListAsync();

            if (emptyBeats.Count > 0)
            {
                foreach (var row in emptyBeats) row.sb.IsEnabled = false;
                await db.SaveChangesAsync();
                Console.WriteLine($"[bible-strand] Soft-deleted {emptyBeats.Count} empty planned beats.");
            }
        }

        Console.WriteLine($"[bible-strand] Generating bible…");
        string bibleText;
        try
        {
            bibleText = await bibleService.GenerateAndSaveAsync(strand.Id, seed, strand.Title, targetBeats);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[bible-strand] Bible generation failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine(bibleText);
        Console.WriteLine("─────────────────────────────────────────────────────────────");

        var beatPlans = StrandBibleService.ParseBeatSpine(bibleText);
        Console.WriteLine();
        Console.WriteLine($"[bible-strand] Done. {beatPlans.Count} spine entries parsed.");
        Console.WriteLine($"   URL: https://localhost:7103/strand/{strand.Slug}");

        return 0;
    }
}
