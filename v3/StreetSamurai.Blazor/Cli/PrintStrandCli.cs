using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --print-strand</c> — print all beats of a strand as continuous prose to stdout.
/// Each beat's Text is separated by a blank line. No headers, no beat numbers, no metadata.
///
/// Args (one of --id / --slug required):
///   --id <guid|prefix>  Strand id; a unique prefix is enough.
///   --slug <slug>       Strand slug.
///
/// Exit codes:
///   0 — success.
///   1 — bad args / strand not found / strand has no prose.
/// </summary>
public static class PrintStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":   if (i + 1 < args.Length) id   = args[++i]; break;
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[print-strand] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --print-strand (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Strands.AsNoTracking();
        Core.Data.Entities.Strand? strand;

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
            var prefix = id!.ToLowerInvariant();
            var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
            if (matches.Count > 1)
            {
                Console.Error.WriteLine($"[print-strand] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                return 1;
            }
            strand = matches.FirstOrDefault();
        }

        if (strand == null)
        {
            var locator = slug != null ? $"slug '{slug}'" : $"id '{id}'";
            Console.Error.WriteLine($"[print-strand] No strand found for {locator}.");
            return 1;
        }

        var beats = await db.StrandBeats
            .AsNoTracking()
            .Where(sb => sb.StrandId == strand.Id && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats.AsNoTracking(),
                  sb => sb.BeatId,
                  b  => b.Id,
                  (sb, b) => b.Text)
            .ToListAsync();

        var prose = beats.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

        if (prose.Count == 0)
        {
            Console.Error.WriteLine($"[print-strand] Strand '{strand.Slug}' has no prose beats.");
            return 1;
        }

        Console.WriteLine(string.Join("\n\n", prose));
        return 0;
    }
}
