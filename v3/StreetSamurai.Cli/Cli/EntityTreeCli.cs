using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --entity-tree (--id &lt;guid&gt; | --slug &lt;slug&gt;) [--depth N] [--rel-types type1,type2] [--as-of "date"]
/// Prints a formatted relationship tree rooted at the entity.
/// </summary>
public static class EntityTreeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Guid? entityId = null;
        string? slug = null;
        int depth = 3;
        string[]? relTypes = null;
        DateTime? asOf = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--id":
                    if (Guid.TryParse(args[i + 1], out var g)) { entityId = g; i++; }
                    break;
                case "--slug":
                    slug = args[i + 1]; i++;
                    break;
                case "--depth":
                    if (int.TryParse(args[i + 1], out var d)) { depth = d; i++; }
                    break;
                case "--rel-types":
                    relTypes = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    i++;
                    break;
                case "--as-of":
                    if (DateTime.TryParse(args[i + 1], out var dt)) { asOf = dt; i++; }
                    break;
            }
        }

        if (entityId == null && slug != null)
        {
            var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
            using var db = dbFactory.CreateDbContext();
            entityId = await db.Entities.AsNoTracking()
                .Where(e => e.Slug == slug)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync();
        }

        if (entityId == null)
        {
            Console.Error.WriteLine("Usage: ss --entity-tree (--id <guid> | --slug <slug>) [--depth N] [--rel-types type1,type2] [--as-of date]");
            return 1;
        }

        var svc = services.GetRequiredService<EntityRelationshipService>();
        var tree = await svc.GetTreeAsync(entityId.Value, depth, relTypes, asOf);

        if (string.IsNullOrEmpty(tree.Name))
        {
            Console.Error.WriteLine($"Entity {entityId} not found.");
            return 1;
        }

        Console.WriteLine(svc.FormatTreeAsContextBlock(tree));
        Console.WriteLine($"[{CountNodes(tree)} related entities, depth {depth}]");
        return 0;
    }

    private static int CountNodes(EntityRelTree node)
    {
        int count = node.Children.Count;
        foreach (var child in node.Children)
            count += CountNodes(child);
        return count;
    }
}
