using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Registry of runtime-defined repositories (custom entity types). A repository is just a
/// named <c>EntityType</c> discriminator on the universal <see cref="Entity"/> spine — no typed
/// table. Definitions are GLOBAL (not universe-scoped); universe separation happens on the Entity
/// rows via <c>UniverseId</c>, so an empty repo in a given universe is simply not shown there
/// (callers filter on <see cref="CountInCurrentUniverse"/> &gt; 0).
/// </summary>
public class RepositoryDefinitionService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    private static readonly string[] ValidCategories =
        ["Characters", "Organizations", "Gear", "World", "Culture"];

    public RepositoryDefinitionService(IDbContextFactory<ProseDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    /// <summary>All custom repository definitions, ordered by category then name.</summary>
    public List<RepositoryDefinition> List()
    {
        using var db = dbFactory.CreateDbContext();
        return db.RepositoryDefinitions.AsNoTracking()
            .OrderBy(r => r.Category).ThenBy(r => r.Name)
            .ToList();
    }

    public RepositoryDefinition? GetBySlug(string slug)
    {
        using var db = dbFactory.CreateDbContext();
        return db.RepositoryDefinitions.AsNoTracking().FirstOrDefault(r => r.Slug == slug);
    }

    /// <summary>Count of ACTIVE entities of this repo's type in the CURRENT universe (the EF query
    /// filter on <see cref="Entity"/> already scopes by universe), so empty repos can be hidden.</summary>
    public int CountInCurrentUniverse(string slug)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Entities.AsNoTracking().Count(e => e.EntityType == slug);
    }

    /// <summary>
    /// Create a new repository. Slug is derived from <paramref name="name"/> (lowercased, hyphenated)
    /// and must be unique — collisions (including built-in types) are rejected. Returns the created row.
    /// </summary>
    public RepositoryDefinition Create(string name, string? category = null, string? icon = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name is required.", nameof(name));

        var slug = UniverseGraphService.Slugify(name);
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException($"Name '{name}' does not produce a valid slug.", nameof(name));

        var cat = ValidCategories.FirstOrDefault(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)) ?? "World";

        using var db = dbFactory.CreateDbContext();
        if (db.RepositoryDefinitions.Any(r => r.Slug == slug))
            throw new InvalidOperationException($"A repository with slug '{slug}' already exists.");

        var def = new RepositoryDefinition
        {
            Id          = Guid.CreateVersion7(),
            Slug        = slug,
            Name        = name.Trim(),
            Category    = cat,
            Icon        = string.IsNullOrWhiteSpace(icon) ? "bi-box" : icon!.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim(),
            RoutePath   = $"/repo/{slug}",
            CreatedAt   = DateTime.UtcNow,
        };
        db.RepositoryDefinitions.Add(def);
        try { db.SaveChanges(); }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && (sql.Number == 2627 || sql.Number == 2601)
               || ex.InnerException?.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException($"A repository with slug '{slug}' already exists.");
        }
        return def;
    }
}
