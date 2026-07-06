using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --sanitize-beats [--slug SLUG | --all] [--dry-run]
///
/// Scans beats for UTF-8-as-Windows-1252 mojibake and repairs in place.
/// Prints a summary of affected beats. Idempotent.
/// </summary>
static class SanitizeBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug    = args.SkipWhile(a => a != "--slug").Skip(1).FirstOrDefault();
        var all     = args.Contains("--all");
        var dryRun  = args.Contains("--dry-run");

        if (slug is null && !all)
        {
            Console.Error.WriteLine("--sanitize-beats requires --slug <slug> or --all");
            return 1;
        }

        await using var db = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>()
                                     .CreateDbContext();

        // Build the beat query
        var query = db.Beats.AsQueryable();

        if (slug is not null)
        {
            var node = await db.Nodes.AsNoTracking()
                                         .FirstOrDefaultAsync(s => s.Slug == slug);
            if (node is null)
            {
                Console.Error.WriteLine($"Node not found: {slug}");
                return 1;
            }

            var beatIds = await db.BeatNodes.AsNoTracking()
                                              .Where(sb => sb.NodeId == node.Id)
                                              .Select(sb => sb.BeatId)
                                              .ToListAsync();
            query = query.Where(b => beatIds.Contains(b.Id));
        }

        var beats = await query.Where(b => b.Text != null && b.Text != "").ToListAsync();

        int dirty = 0, fixed_ = 0;
        foreach (var beat in beats)
        {
            if (!TextSanitizerService.HasMojibake(beat.Text)) continue;
            dirty++;

            var clean = TextSanitizerService.Sanitize(beat.Text);
            Console.WriteLine($"  Beat {beat.Number}: {(dryRun ? "[DRY-RUN] " : "")}mojibake found");

            if (!dryRun)
            {
                beat.Text      = clean;
                beat.TextHash  = ComputeHash(clean);
                beat.UpdatedAt = DateTime.UtcNow;
                fixed_++;
            }
        }

        if (!dryRun && fixed_ > 0)
            await db.SaveChangesAsync();

        var scope = slug is not null ? $" in [{slug}]" : " across all nodes";
        Console.WriteLine(dryRun
            ? $"\nFound {dirty} beat(s) with mojibake{scope} (dry-run — no changes written)."
            : $"\nFixed {fixed_}/{dirty} beat(s){scope}.");
        return 0;
    }

    static string ComputeHash(string text)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
