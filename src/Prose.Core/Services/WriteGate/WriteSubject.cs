namespace Prose.Core.Services.WriteGate;

/// <summary>
/// What kind of thing changed, for routing a <see cref="WriteEvent"/> to the right checks.
/// Deliberately coarse (a handful of families, not one value per table) — the routing table
/// in <see cref="IWriteAuditService"/> maps each subject to a small set of checks, and a new
/// table joining an existing family (e.g. a new *Aliases table) should reuse the family's
/// subject rather than growing this enum per-table.
/// </summary>
public enum WriteSubject
{
    /// <summary>A Beat's Text column changed (single or batch).</summary>
    BeatText,

    /// <summary>A Beat's non-Text metadata changed (StructureRole, Description, tone/pace, etc).</summary>
    BeatMetadata,

    /// <summary>A new Node (series/book/chapter) was created.</summary>
    NodeCreate,

    /// <summary>An existing Node's structural fields changed (ParentNodeId, PreviousNodeId, SortKey).</summary>
    NodeStructure,

    /// <summary>A Node (and its cascade) was deleted.</summary>
    NodeDelete,

    /// <summary>A canon entity's core fields changed (Character/Place/Faction/Weapon/... via EfRepository).</summary>
    EntityCore,

    /// <summary>A row was added/changed in a *Aliases bridge table (CharacterAliases, PlaceAliases, ...).</summary>
    EntityAlias,

    /// <summary>Entity.OriginNodeId changed — the book-scoping field responsible for the
    /// 2026-08-22 cross-book contamination bug (see EntityOriginService).</summary>
    EntityOrigin,

    /// <summary>A CharacterRelationships row changed. No dedicated checks exist yet (see
    /// project plan "make Prose.Hub the real gatekeeper" — left unrouted deliberately until a
    /// concrete problem surfaces, not a placeholder for future expansion by default).</summary>
    CharacterRelationship,
}
