using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --print-book</c> — print all beats of a node as continuous prose to stdout.
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
            Console.Error.WriteLine("[print-book] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: prose --print-book (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

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
                Console.Error.WriteLine($"[print-book] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                return 1;
            }
            node = matches.FirstOrDefault();
        }

        if (node == null)
        {
            var locator = slug != null ? $"slug '{slug}'" : $"id '{id}'";
            Console.Error.WriteLine($"[print-book] No node found for {locator}.");
            return 1;
        }

        // SS-A43: beats live on chapter children for book-mode stories.
        // Recurses past any nested Collection (2026-08-09 fix); searchIds is already in
        // correct global reading order — materialize first, THEN reorder by its list
        // position client-side (List<Guid>.IndexOf has no SQL translation).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);

        var rows = await db.BeatNodes
            .AsNoTracking()
            .Where(sb => searchIds.Contains(sb.NodeId) && sb.IsEnabled)
            .Join(db.Beats.AsNoTracking(),
                  sb => sb.BeatId,
                  b  => b.Id,
                  (sb, b) => new { sb.NodeId, sb.SortKey, b.Text })
            .ToListAsync();
        var beats = rows
            .OrderBy(r => searchIds.IndexOf(r.NodeId)).ThenBy(r => r.SortKey)
            .Select(r => r.Text).ToList();

        var prose = beats.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

        if (prose.Count == 0)
        {
            Console.Error.WriteLine($"[print-book] Node '{node.Slug}' has no prose beats.");
            return 1;
        }

        Console.WriteLine(string.Join("\n\n", prose));
        return 0;
    }
}
