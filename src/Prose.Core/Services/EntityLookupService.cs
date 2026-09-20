using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>One read-side lookup for every client that needs to resolve a partial entity name.</summary>
public sealed class EntityLookupService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<IReadOnlyList<EntityLookupMatch>> FindAsync(
        string query, string? entityType = null, int limit = 40, CancellationToken ct = default)
    {
        var needle = query?.Trim();
        if (string.IsNullOrWhiteSpace(needle)) return [];
        limit = Math.Clamp(limit, 1, 200);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var folded = needle.ToLowerInvariant();
        var entities = db.Entities.AsNoTracking()
            .Where(e => e.Name.ToLower().Contains(folded));
        if (!string.IsNullOrWhiteSpace(entityType))
            entities = entities.Where(e => e.EntityType == entityType.Trim().ToLowerInvariant());

        var nameMatches = await entities
            .OrderBy(e => e.Name.ToLower() == folded ? 0 : e.Name.ToLower().StartsWith(folded) ? 1 : 2)
            .ThenBy(e => e.Name.Length).ThenBy(e => e.Name)
            .Take(limit)
            .Select(e => new EntityLookupMatch(e.Id, e.Name, e.Slug, e.EntityType, e.OriginNodeId, null))
            .ToListAsync(ct);

        // Character aliases are the only alias bridge today. Keep the result model ready for
        // other entity-type alias bridges without making the lookup lie about what it searched.
        var aliasMatches = await db.CharacterAliases.AsNoTracking()
            .Where(a => a.Value.ToLower().Contains(folded))
            .Join(db.Entities.AsNoTracking(), a => a.CharacterId, e => e.Id,
                (a, e) => new { a, e })
            .Where(x => string.IsNullOrWhiteSpace(entityType) || x.e.EntityType == entityType.Trim().ToLowerInvariant())
            .OrderBy(x => x.a.Value.ToLower() == folded ? 0 : x.a.Value.ToLower().StartsWith(folded) ? 1 : 2)
            .ThenBy(x => x.e.Name.Length).ThenBy(x => x.e.Name)
            .Take(limit)
            .Select(x => new EntityLookupMatch(x.e.Id, x.e.Name, x.e.Slug, x.e.EntityType, x.e.OriginNodeId, x.a.Value))
            .ToListAsync(ct);

        var seen = new HashSet<Guid>();
        return nameMatches.Concat(aliasMatches)
            .Where(m => seen.Add(m.Id))
            .Take(limit)
            .ToList();
    }
}

public sealed record EntityLookupMatch(
    Guid Id,
    string Name,
    string Slug,
    string EntityType,
    Guid? OriginNodeId,
    string? MatchedAlias);
