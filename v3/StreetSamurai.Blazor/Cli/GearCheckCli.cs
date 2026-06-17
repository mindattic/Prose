using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --gear-check --slug &lt;strandSlug&gt; --character &lt;characterId&gt; [--story-time "date"]
/// Scans each beat of the strand for gear usage that lacks a carry/wield edge.
/// </summary>
public static class GearCheckCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? strandSlug = null;
        Guid? characterId = null;
        DateTime? storyTime = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": strandSlug = args[i + 1]; i++; break;
                case "--character":
                    if (Guid.TryParse(args[i + 1], out var g)) { characterId = g; i++; }
                    i++;
                    break;
                case "--story-time":
                    if (DateTime.TryParse(args[i + 1], out var dt)) { storyTime = dt; i++; }
                    i++;
                    break;
            }
        }

        if (strandSlug == null || characterId == null)
        {
            Console.Error.WriteLine("Usage: ss --gear-check --slug <strandSlug> --character <characterId> [--story-time date]");
            return 1;
        }

        var enforcer = services.GetRequiredService<GearCarryEnforcer>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        using var db = dbFactory.CreateDbContext();

        var strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == strandSlug);
        if (strand == null) { Console.Error.WriteLine($"Strand '{strandSlug}' not found."); return 1; }

        var beats = await (
            from sb in db.StrandBeats
            join b in db.Beats on sb.BeatId equals b.Id
            where sb.StrandId == strand.Id
            orderby sb.SortKey
            select new { b.Id, b.Number, b.Text }
        ).ToListAsync();

        int totalViolations = 0;
        foreach (var beat in beats)
        {
            var violations = await enforcer.EnforceAsync(beat.Text ?? "", characterId.Value, storyTime);
            if (violations.Count == 0) continue;

            Console.WriteLine($"\nBeat #{beat.Number}: {violations.Count} gear violation(s)");
            foreach (var v in violations)
            {
                Console.WriteLine($"  • {v.VerbUsed} '{v.GearName}' — {v.Issue}");
            }
            totalViolations += violations.Count;
        }

        if (totalViolations == 0)
            Console.WriteLine("✔ No gear carry violations found.");
        else
            Console.WriteLine($"\n{totalViolations} total gear violation(s).");

        return totalViolations > 0 ? 1 : 0;
    }
}
