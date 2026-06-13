using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --reparent-strand (--slug &lt;slug&gt; | --id &lt;id&gt;) (--parent-slug &lt;slug&gt; | --parent-id &lt;id&gt;)
/// — sets ParentStrandId on an existing strand.
/// Use --clear to detach from any parent.
/// Use --sort-key N to set the strand's SortKey (can combine with parent change or use standalone).
/// </summary>
public static class ReparentStrandCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, parentId = null, parentSlug = null;
        bool clear = false;
        double? sortKey = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":          if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":        if (i + 1 < args.Length) slug = args[++i]; break;
                case "--parent-id":   if (i + 1 < args.Length) parentId = args[++i]; break;
                case "--parent-slug": if (i + 1 < args.Length) parentSlug = args[++i]; break;
                case "--clear":       clear = true; break;
                case "--sort-key":    if (i + 1 < args.Length && double.TryParse(args[++i], out var sk)) sortKey = sk; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[reparent-strand] --id or --slug required to identify the child strand.");
            return 1;
        }
        if (!clear && sortKey == null && string.IsNullOrWhiteSpace(parentId) && string.IsNullOrWhiteSpace(parentSlug))
        {
            Console.Error.WriteLine("[reparent-strand] --parent-id or --parent-slug required (or --clear to detach, or --sort-key N to reorder).");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var childQ = db.Strands.AsQueryable();
        var child = !string.IsNullOrWhiteSpace(slug)
            ? await childQ.FirstOrDefaultAsync(s => s.Slug == slug)
            : Guid.TryParse(id, out var cg)
                ? await childQ.FirstOrDefaultAsync(s => s.Id == cg)
                : await childQ.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync()
                    is { Count: 1 } cm ? cm[0] : null;

        if (child == null) { Console.Error.WriteLine("[reparent-strand] Child strand not found."); return 1; }

        if (clear)
        {
            child.ParentStrandId = null;
            if (sortKey.HasValue) child.SortKey = sortKey.Value;
            child.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            Console.WriteLine($"[reparent-strand] \"{child.Title}\" detached from parent." + (sortKey.HasValue ? $" SortKey={sortKey}" : ""));
            return 0;
        }

        // Sort-key-only update (no parent change needed).
        if (sortKey.HasValue && string.IsNullOrWhiteSpace(parentId) && string.IsNullOrWhiteSpace(parentSlug))
        {
            child.SortKey = sortKey.Value;
            child.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            Console.WriteLine($"[reparent-strand] \"{child.Title}\" SortKey={sortKey}.");
            return 0;
        }

        var parentQ = db.Strands.AsQueryable();
        var parent = !string.IsNullOrWhiteSpace(parentSlug)
            ? await parentQ.FirstOrDefaultAsync(s => s.Slug == parentSlug)
            : Guid.TryParse(parentId, out var pg)
                ? await parentQ.FirstOrDefaultAsync(s => s.Id == pg)
                : await parentQ.Where(s => s.Id.ToString().StartsWith(parentId!.ToLower())).Take(2).ToListAsync()
                    is { Count: 1 } pm ? pm[0] : null;

        if (parent == null) { Console.Error.WriteLine("[reparent-strand] Parent strand not found."); return 1; }
        if (parent.Id == child.Id) { Console.Error.WriteLine("[reparent-strand] A strand cannot be its own parent."); return 1; }

        child.ParentStrandId = parent.Id;
        if (sortKey.HasValue) child.SortKey = sortKey.Value;
        child.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Console.WriteLine($"[reparent-strand] \"{child.Title}\" → parent \"{parent.Title}\"." + (sortKey.HasValue ? $" SortKey={sortKey}" : ""));
        return 0;
    }
}
