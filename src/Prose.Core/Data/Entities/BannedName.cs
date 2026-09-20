namespace Prose.Core.Data.Entities;

/// <summary>
/// Prose-wide hard-banned name registry (2026-08-26). Unlike <see cref="DeprecatedEntityName"/>
/// (a per-universe rename MAP consumed only after the fact by
/// <see cref="Services.NounConsistencyService"/>'s prose scan), a <c>BannedName</c> has no
/// canonical replacement and no universe scope — it is an outright ban, everywhere, enforced at
/// the write itself via <see cref="Services.WriteGate.BannedNameSyncCheck"/>. Matching is
/// whole-word, case-insensitive, and covers every <see cref="Entity.Name"/> plus every
/// CharacterAlias/PlaceAlias/FactionAlias/WeaponAlias value across all universes.
///
/// Forward-only by design (author ruling 2026-08-26): adding a name here does NOT touch any
/// existing row that already carries it — only new/modified writes going forward are rejected.
/// </summary>
public class BannedName
{
    public long Id { get; set; }

    /// <summary>The banned name/word (e.g. "Voss"). Matched whole-word, case-insensitive.</summary>
    public string Name { get; set; } = "";

    /// <summary>Human note explaining why this name is banned.</summary>
    public string? Notes { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
