using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --print-story</c> — print all beats of a node as continuous prose to stdout.
/// Each beat's Text is separated by a blank line. No headers, no beat numbers, no metadata.
///
/// Args (one of --id / --slug required):
///   --id <guid|prefix>  Node id; a unique prefix is enough.
///   --slug <slug>       Node slug.
///
/// Exit codes:
///   0 — success.
///   1 — bad args / node not found / node has no prose.
/// </summary>
public static class PrintNodeCli
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
            Console.Error.WriteLine("[print-story] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --print-story (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Nodes.AsNoTracking();
        Core.Data.Entities.Node? node;

        if (!string.IsNullOrWhiteSpace(slug))
        {
            node = await query.FirstOrDefaultAsync(s => s.Slug == slug);
        }
        else if (Guid.TryParse(id, out var exact))
        {
            node = await query.FirstOrDefaultAsync(s => s.Id == exact);
        }
        else
        {
            var prefix = id!.ToLowerInvariant();
            var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
            if (matches.Count > 1)
            {
                Console.Error.WriteLine($"[print-story] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                return 1;
            }
            node = matches.FirstOrDefault();
        }

        if (node == null)
        {
            var locator = slug != null ? $"slug '{slug}'" : $"id '{id}'";
            Console.Error.WriteLine($"[print-story] No node found for {locator}.");
            return 1;
        }

        var beats = await db.BeatNodes
            .AsNoTracking()
            .Where(sb => sb.NodeId == node.Id && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats.AsNoTracking(),
                  sb => sb.BeatId,
                  b  => b.Id,
                  (sb, b) => b.Text)
            .ToListAsync();

        var prose = beats.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

        if (prose.Count == 0)
        {
            Console.Error.WriteLine($"[print-story] Node '{node.Slug}' has no prose beats.");
            return 1;
        }

        Console.WriteLine(string.Join("\n\n", prose));
        return 0;
    }
}
