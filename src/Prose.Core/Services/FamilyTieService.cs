using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Family-relationship API on top of the existing <see cref="Edge"/> table.
/// No new table — the Edge schema already advertises <c>parent_of</c> /
/// <c>child_of</c> in its doc-comment, so we extend the existing relation-type
/// vocabulary rather than carving out a parallel store.
///
/// <para><b>Storage convention.</b></para>
/// <list type="bullet">
///   <item><c>parent_of</c> — directional. Source = parent, Target = child.
///         Queried in both directions to derive parents-of-X and children-of-X.
///         We never write the reverse <c>child_of</c> edge; it would just
///         duplicate information that's already addressable.</item>
///   <item><c>sibling_of</c> — symmetric. We write TWO edges (a→b and b→a) on
///         add so every <see cref="GetSiblingsAsync"/> query is a single
///         WHERE-clause lookup with no direction conditional.</item>
///   <item><c>spouse_of</c> — symmetric. Same write-two-edges convention.</item>
/// </list>
///
/// <para>Cousin / grandparent / aunt / uncle / niece / nephew are NOT stored
/// as their own relation types — they're derived by walking parent_of edges
/// (a cousin = your parent's sibling's child; a grandparent = your parent's
/// parent). Storing only the minimal set keeps the family graph normalized
/// and prevents inconsistent state (e.g. saying X is Y's cousin while their
/// parent edges contradict it).</para>
///
/// <para><b>Maternal vs paternal.</b> We don't bake mother_of/father_of as
/// separate relation types. The genetics walker reads the parent's
/// <see cref="Character.Gender"/> at traversal time. That keeps Characters
/// authoritative for biological role and avoids drift between two stores.</para>
/// </summary>
public class FamilyTieService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<FamilyTieService> log;

    public FamilyTieService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<FamilyTieService> log)
    {
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    public const string ParentOf  = "parent_of";
    public const string SiblingOf = "sibling_of";
    public const string SpouseOf  = "spouse_of";

    /// <summary>Source = parent, Target = child.</summary>
    public async Task<List<Entity>> GetParentsAsync(Guid childId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Edges.AsNoTracking()
            .Where(e => e.TargetId == childId
                     && e.RelationType == ParentOf
                     && e.StoryValidUntil == null)
            .Join(db.Entities.AsNoTracking(),
                e  => e.SourceId,
                en => en.Id,
                (e, en) => en)
            .ToListAsync(ct);
    }

    public async Task<List<Entity>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Edges.AsNoTracking()
            .Where(e => e.SourceId == parentId
                     && e.RelationType == ParentOf
                     && e.StoryValidUntil == null)
            .Join(db.Entities.AsNoTracking(),
                e  => e.TargetId,
                en => en.Id,
                (e, en) => en)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Walks up two levels through parent_of edges. Distinct because both
    /// parents may share a grandparent (rare but possible in canon).
    /// </summary>
    public async Task<List<Entity>> GetGrandparentsAsync(Guid childId, CancellationToken ct = default)
    {
        var parents = await GetParentsAsync(childId, ct);
        var seen    = new HashSet<Guid>();
        var result  = new List<Entity>();
        foreach (var p in parents)
            foreach (var gp in await GetParentsAsync(p.Id, ct))
                if (seen.Add(gp.Id)) result.Add(gp);
        return result;
    }

    /// <summary>
    /// Stored as symmetric edges, so a single direction lookup is enough.
    /// Self is implicitly excluded since we never write a self-edge.
    /// </summary>
    public async Task<List<Entity>> GetSiblingsAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Edges.AsNoTracking()
            .Where(e => e.SourceId == characterId
                     && e.RelationType == SiblingOf
                     && e.StoryValidUntil == null)
            .Join(db.Entities.AsNoTracking(),
                e  => e.TargetId,
                en => en.Id,
                (e, en) => en)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Children of one's parents' siblings. Computed, not stored — keeps the
    /// graph normalized and avoids inconsistent-state bugs where a stored
    /// cousin edge contradicts the underlying parent edges.
    /// </summary>
    public async Task<List<Entity>> GetCousinsAsync(Guid characterId, CancellationToken ct = default)
    {
        var parents = await GetParentsAsync(characterId, ct);
        var seen    = new HashSet<Guid>();
        var result  = new List<Entity>();
        foreach (var p in parents)
        {
            foreach (var auntOrUncle in await GetSiblingsAsync(p.Id, ct))
            foreach (var cousin       in await GetChildrenAsync(auntOrUncle.Id, ct))
                if (cousin.Id != characterId && seen.Add(cousin.Id)) result.Add(cousin);
        }
        return result;
    }

    public async Task<List<Entity>> GetSpouseAsync(Guid characterId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Edges.AsNoTracking()
            .Where(e => e.SourceId == characterId
                     && e.RelationType == SpouseOf
                     && e.StoryValidUntil == null)
            .Join(db.Entities.AsNoTracking(),
                e  => e.TargetId,
                en => en.Id,
                (e, en) => en)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Add a parent_of edge. Idempotent — does nothing if the edge already
    /// exists with the same source/target/relation/story-bounds.
    /// </summary>
    public async Task AddParentAsync(Guid parentId, Guid childId,
        string source = "canon", string? description = null, CancellationToken ct = default)
    {
        if (parentId == childId) throw new ArgumentException("A character cannot be their own parent.");
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var exists = await db.Edges
            .AnyAsync(e => e.SourceId == parentId
                        && e.TargetId == childId
                        && e.RelationType == ParentOf
                        && e.StoryValidUntil == null, ct);
        if (exists) return;

        db.Edges.Add(new Edge
        {
            SourceId     = parentId,
            TargetId     = childId,
            RelationType = ParentOf,
            Description  = description,
            Source       = source,
            Sentiment    = "neutral",
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("Added parent_of {Parent}->{Child}", parentId, childId);
    }

    /// <summary>
    /// Add symmetric sibling_of edges (both directions). Idempotent.
    /// </summary>
    public async Task AddSiblingAsync(Guid aId, Guid bId,
        string source = "canon", CancellationToken ct = default)
    {
        if (aId == bId) throw new ArgumentException("A character cannot be their own sibling.");
        await AddSymmetricEdgeAsync(aId, bId, SiblingOf, source, ct);
    }

    /// <summary>
    /// Add symmetric spouse_of edges (both directions). Idempotent.
    /// </summary>
    public async Task AddSpouseAsync(Guid aId, Guid bId,
        string source = "canon", CancellationToken ct = default)
    {
        if (aId == bId) throw new ArgumentException("A character cannot be their own spouse.");
        await AddSymmetricEdgeAsync(aId, bId, SpouseOf, source, ct);
    }

    private async Task AddSymmetricEdgeAsync(Guid aId, Guid bId, string relation,
        string source, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existingAB = await db.Edges.AnyAsync(e =>
            e.SourceId == aId && e.TargetId == bId
            && e.RelationType == relation && e.StoryValidUntil == null, ct);
        var existingBA = await db.Edges.AnyAsync(e =>
            e.SourceId == bId && e.TargetId == aId
            && e.RelationType == relation && e.StoryValidUntil == null, ct);
        if (!existingAB)
            db.Edges.Add(new Edge { SourceId = aId, TargetId = bId, RelationType = relation, Source = source, Sentiment = "neutral" });
        if (!existingBA)
            db.Edges.Add(new Edge { SourceId = bId, TargetId = aId, RelationType = relation, Source = source, Sentiment = "neutral" });
        if (!existingAB || !existingBA)
        {
            await db.SaveChangesAsync(ct);
            log.LogInformation("Added symmetric {Relation} {A}<->{B}", relation, aId, bId);
        }
    }

    /// <summary>
    /// Find character entities whose Name matches a substring query. For
    /// the manual-add-tie UI: user types a few letters, we surface the top
    /// matches so they can wire two existing characters as family without
    /// spawning a duplicate. Excludes the subject themself.
    /// </summary>
    public async Task<List<Entity>> SearchCharactersByNameAsync(string query, Guid excludeId,
        int limit = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return new List<Entity>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var like = $"%{query.Trim()}%";
        return await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character"
                     && e.Id != excludeId
                     && EF.Functions.Like(e.Name, like))
            .OrderBy(e => e.Name.Length)   // shorter name = better match
            .ThenBy(e => e.Name)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Aggregate snapshot for a character. Convenient for UI panels and the
    /// genetics walker, which all want the whole family at once.
    /// </summary>
    public async Task<FamilySnapshot> GetSnapshotAsync(Guid characterId, CancellationToken ct = default)
    {
        var parents = await GetParentsAsync(characterId, ct);
        var children = await GetChildrenAsync(characterId, ct);
        var siblings = await GetSiblingsAsync(characterId, ct);
        var spouses = await GetSpouseAsync(characterId, ct);
        var grandparents = await GetGrandparentsAsync(characterId, ct);
        var cousins = await GetCousinsAsync(characterId, ct);
        return new FamilySnapshot(characterId, parents, children, siblings, spouses, grandparents, cousins);
    }

    public sealed record FamilySnapshot(
        Guid CharacterId,
        List<Entity> Parents,
        List<Entity> Children,
        List<Entity> Siblings,
        List<Entity> Spouses,
        List<Entity> Grandparents,
        List<Entity> Cousins);
}
