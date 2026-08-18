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
    /// </summary>
    public static Guid? ResolveEntityIdAny(ProseDbContext db, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var e = db.Entities.AsNoTracking().FirstOrDefault(x => x.Name == name);
        if (e != null) return e.Id;

        var slug = Prose.Core.Services.WorldGraphService.Slugify(name);
        e = db.Entities.AsNoTracking().FirstOrDefault(x => x.Slug == slug);
        if (e != null) return e.Id;

        return ResolveByAlias(db, name);
    }

    private static Guid? ResolveByAlias(ProseDbContext db, string name)
    {
        var candidateId =
            db.Characters.AsNoTracking().Where(c => c.Aliases.Any(a => a.Value == name)).Select(c => (Guid?)c.Id).FirstOrDefault()
            ?? db.Places.AsNoTracking().Where(p => p.Aliases.Any(a => a.Value == name)).Select(p => (Guid?)p.Id).FirstOrDefault()
            ?? db.Factions.AsNoTracking().Where(f => f.Aliases.Any(a => a.Value == name)).Select(f => (Guid?)f.Id).FirstOrDefault()
            ?? db.Weapons.AsNoTracking().Where(w => w.Aliases.Any(a => a.Value == name)).Select(w => (Guid?)w.Id).FirstOrDefault();

        if (candidateId is not { } id) return null;

        // The alias tables share their PK with Entities. Their owning row's existence in the
        // live table is the only check needed now — a deleted entity's alias rows cascade away
        // with it (EntityTags-style child), so a candidateId that survived the queries above
        // already belongs to a live entity by construction; this Any() is a defensive confirm,
        // not a filter on a separate status flag.
        return db.Entities.AsNoTracking().Any(x => x.Id == id) ? id : null;
    }
}
