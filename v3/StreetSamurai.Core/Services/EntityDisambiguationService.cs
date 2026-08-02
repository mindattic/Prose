using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Resolves which entity a Name actually refers to when more than one entity in the same
/// Universe shares that Name — e.g. GSPL's historical/citation-grounded "Raphael" (the Gospel
/// books' research entry, <see cref="Data.Entities.Entity.OriginNodeId"/> = null, universe-wide)
/// versus EPIC's literary-fictional "Raphael" (Milton's character in TFAH, OriginNodeId = TFAH's
/// BookNode.Id). A fictional literary character and a historical/nonfiction research entry for
/// the same name are genuinely different entities, not the same canon fact reused — this service
/// exists to keep that distinction real at lookup time, not just documented in a memory file.
///
/// Resolution precedence, given a scene's current Node (a beat's owning chapter, typically):
///   1. An entity whose OriginNodeId matches the current book/series context exactly.
///   2. An entity with OriginNodeId == null (a universe-wide shared entity — the default/legacy
///      behavior for every entity created before this field existed).
///   3. Otherwise the first candidate, with a warning logged — a genuine ambiguity that should be
///      fixed by giving one of the colliding entities an explicit OriginNodeId.
/// </summary>
public sealed class EntityDisambiguationService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILogger<EntityDisambiguationService> log)
{
    /// <summary>
    /// Walks <see cref="Data.Entities.Node.ParentNodeId"/> up from <paramref name="nodeId"/>
    /// (inclusive of the starting node) to find the nearest ancestor whose Kind is "book" or
    /// "series" — the owning context a same-named entity's OriginNodeId should be compared
    /// against. Mirrors the ancestor-walk already used by
    /// <see cref="DocContextService"/>'s ResolveSeriesScopeKeysAsync/ResolveEffectiveNodeCodeAsync.
    /// Returns null if no such ancestor exists (e.g. a session-key context id that isn't a real
    /// Node) — in that case disambiguation falls back to universe-wide (OriginNodeId == null)
    /// entities only.
    /// </summary>
    public async Task<Guid?> ResolveNearestBookOrSeriesNodeIdAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var current = await db.Nodes.AsNoTracking()
            .Where(n => n.Id == nodeId)
            .Select(n => new { n.Id, n.Kind, n.ParentNodeId })
            .FirstOrDefaultAsync(ct);
        if (current == null) return null;
        if (current.Kind is "book" or "series") return current.Id;

        var parentId = current.ParentNodeId;
        for (var depth = 0; depth < 5 && parentId is { } pid; depth++)
        {
            var parent = await db.Nodes.AsNoTracking()
                .Where(n => n.Id == pid)
                .Select(n => new { n.Id, n.Kind, n.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (parent == null) break;
            if (parent.Kind is "book" or "series") return parent.Id;
            parentId = parent.ParentNodeId;
        }
        return null;
    }

    /// <summary>
    /// Picks the best candidate from a set of same-named entities, given the resolved book/series
    /// context (from <see cref="ResolveNearestBookOrSeriesNodeIdAsync"/>) the current scene
    /// belongs to. <paramref name="candidates"/> must be non-empty. Logs a warning (does not
    /// throw) when the tie is broken arbitrarily because no candidate matched either rule.
    /// </summary>
    public T ResolveBestMatch<T>(
        IReadOnlyList<T> candidates,
        Func<T, Guid?> originNodeIdSelector,
        Guid? contextBookOrSeriesId,
        string nameForLogging)
    {
        if (candidates.Count == 1) return candidates[0];

        if (contextBookOrSeriesId is { } ctxId)
        {
            var exact = candidates.FirstOrDefault(c => originNodeIdSelector(c) == ctxId);
            if (exact is not null) return exact;
        }

        var shared = candidates.FirstOrDefault(c => originNodeIdSelector(c) is null);
        if (shared is not null) return shared;

        log.LogWarning(
            "EntityDisambiguationService: {Count} entities named '{Name}' collide with no OriginNodeId " +
            "match for context {Context} and no universe-wide (null OriginNodeId) fallback available — " +
            "picking the first candidate arbitrarily. Fix by setting OriginNodeId on the colliding entities.",
            candidates.Count, nameForLogging, contextBookOrSeriesId);
        return candidates[0];
    }
}
