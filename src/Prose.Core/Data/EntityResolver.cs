using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Data;

/// <summary>
/// Shared, type-agnostic entity-name resolution for mapper bridges (<see cref="CharacterMapper"/>,
/// <see cref="PlaceMapper"/>) whose target can legitimately be any entity type and carries no
/// field saying which — e.g. <c>CharacterRelationship.TargetName</c>, <c>PlaceFrequentBy.Alias</c>.
///
/// Extracted 2026-08-10 after CharacterMapper's and PlaceMapper's private copies were found to be
/// identical (no drift yet) but both missed the same class of match: resolving a well-known
/// ALIAS ("Kyle") to its canonical entity ("Kyle Ellen Corbin"). A relationship-target resolver
/// living in two places is exactly the kind of duplication that let CharacterMapper's own
/// relationship-building loop silently skip the resolver call entirely for months — a single
/// shared implementation is the fix that can't drift.
/// </summary>
public static class EntityResolver
{
    /// <summary>
    /// Resolve <paramref name="name"/> to an entity's id: exact <c>Name</c>, then exact
    /// <c>Slug</c>, then a registered alias (Character/Place/Faction/Weapon — the same alias
    /// tables <c>EntityRamificationService</c>'s beat-text name index already draws on). Returns
    /// null rather than guessing on ambiguous or absent matches.
    ///
    /// <para><paramref name="scopeNodeId"/> is the calling row's own book scope (e.g. the source
    /// character's <see cref="Entity.OriginNodeId"/>). When two entities in one universe share a
    /// name, the one scoped to that book wins; that is not a guess, it is the entire purpose of
    /// book-scoping. Story Ledger Phase 3 — before this, the method was a bare
    /// <c>FirstOrDefault</c> on exact name, so a same-name collision resolved to whichever row the
    /// query happened to enumerate first, silently. Its own doc comment already promised "returns
    /// null rather than guessing on ambiguous matches" and did not keep that promise; it does
    /// now. Deliberately mirrors <c>EntityMentionScanner.BuildCandidateIndexAsync</c>'s already
    /// hardened rule ("exactly one book-scoped contender wins; zero or two-plus is genuinely
    /// ambiguous") rather than inventing a second set of resolution semantics.</para>
    /// </summary>
    public static Guid? ResolveEntityIdAny(ProseDbContext db, string name, Guid? scopeNodeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var byName = db.Entities.AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => new Candidate(x.Id, x.OriginNodeId)).Take(CandidateProbeLimit).ToList();
        if (Disambiguate(byName, scopeNodeId) is { } nameHit) return nameHit;

        var slug = Prose.Core.Services.UniverseGraphService.Slugify(name);
        var bySlug = db.Entities.AsNoTracking()
            .Where(x => x.Slug == slug)
            .Select(x => new Candidate(x.Id, x.OriginNodeId)).Take(CandidateProbeLimit).ToList();
        if (Disambiguate(bySlug, scopeNodeId) is { } slugHit) return slugHit;

        return ResolveByAlias(db, name, scopeNodeId);
    }

    /// <summary>An entity id plus the book scope that decides a same-name collision.</summary>
    private readonly record struct Candidate(Guid Id, Guid? OriginNodeId);

    /// <summary>
    /// Enough rows to tell "one match" from "several" without materializing a pathological set.
    /// A name with more than this many claimants is ambiguous by any reading.
    /// </summary>
    private const int CandidateProbeLimit = 25;

    /// <summary>
    /// Pick the single correct candidate, or null when the collision cannot be broken honestly.
    ///
    /// <para>Order: one candidate wins outright → the unique candidate scoped to the caller's own
    /// book wins → the unique universe-wide (<c>OriginNodeId == null</c>) candidate wins → null.
    /// The last step is the one that matters: two book-scoped claimants, or two universe-wide
    /// claimants, is a real data defect (a duplicate entity), and returning either of them at
    /// random is how a relationship ends up pointing at the wrong book's character.</para>
    /// </summary>
    private static Guid? Disambiguate(List<Candidate> candidates, Guid? scopeNodeId)
    {
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0].Id;

        if (scopeNodeId is { } scope)
        {
            var scoped = candidates.Where(c => c.OriginNodeId == scope).ToList();
            if (scoped.Count == 1) return scoped[0].Id;
        }

        var shared = candidates.Where(c => c.OriginNodeId == null).ToList();
        return shared.Count == 1 ? shared[0].Id : null;
    }

    private static Guid? ResolveByAlias(ProseDbContext db, string name, Guid? scopeNodeId)
    {
        // Alias tables are keyed by their owning entity's id and carry no OriginNodeId of their
        // own, so the book-scope tiebreaker is applied by joining the ids back to Entities —
        // per-type, keeping the existing character→place→faction→weapon precedence intact.
        var candidateId =
            ResolveAliasIn(db, db.Characters.AsNoTracking().Where(c => c.Aliases.Any(a => a.Value == name)).Select(c => c.Id), scopeNodeId)
            ?? ResolveAliasIn(db, db.Places.AsNoTracking().Where(p => p.Aliases.Any(a => a.Value == name)).Select(p => p.Id), scopeNodeId)
            ?? ResolveAliasIn(db, db.Factions.AsNoTracking().Where(f => f.Aliases.Any(a => a.Value == name)).Select(f => f.Id), scopeNodeId)
            ?? ResolveAliasIn(db, db.Weapons.AsNoTracking().Where(w => w.Aliases.Any(a => a.Value == name)).Select(w => w.Id), scopeNodeId);

        if (candidateId is not { } id) return null;

        // The alias tables share their PK with Entities. Their owning row's existence in the
        // live table is the only check needed now — a deleted entity's alias rows cascade away
        // with it (EntityTags-style child), so a candidateId that survived the queries above
        // already belongs to a live entity by construction; this Any() is a defensive confirm,
        // not a filter on a separate status flag.
        return db.Entities.AsNoTracking().Any(x => x.Id == id) ? id : null;
    }

    /// <summary>
    /// Resolve one type's alias hits through the same book-scope disambiguation the name/slug
    /// passes use, so an alias shared by two same-universe entities is dropped rather than
    /// arbitrarily assigned. Returns null when this type has no unambiguous claimant, letting the
    /// caller's <c>??</c> chain fall through to the next type.
    /// </summary>
    private static Guid? ResolveAliasIn(ProseDbContext db, IQueryable<Guid> ids, Guid? scopeNodeId)
    {
        var candidates = db.Entities.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new Candidate(x.Id, x.OriginNodeId))
            .Take(CandidateProbeLimit).ToList();
        return Disambiguate(candidates, scopeNodeId);
    }
}
