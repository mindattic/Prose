namespace Prose.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// CorpoNation hierarchies (PRS-T819) — who owns what, all the way down to ground
// level, and the private forces each CorpoNation can call up. World law: a parent
// always sits on a higher Tier than what it owns; tiers stacked on tiers are one
// corporate family. The rules live in CorporateHierarchyService, not here.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// One ownership edge: <see cref="OwnerEntityId"/> (a corponation or subsidiary) owns
/// <see cref="OwnedEntityId"/> (any entity — a subsidiary, a faction, a place, a transit line).
/// Unique per (owner, owned). <see cref="Subsidiary.ParentCorponationId"/> is backfilled into these
/// rows as wholly-owned; from then on this table is the record of who owns what.
/// </summary>
public class CorporateOwnership
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The owner's universe; owner and owned are always in the same one.</summary>
    public Guid UniverseId { get; set; }

    /// <summary>FK to Entities.Id — a corponation or subsidiary.</summary>
    public Guid OwnerEntityId { get; set; }

    /// <summary>FK to Entities.Id — any entity type.</summary>
    public Guid OwnedEntityId { get; set; }

    /// <summary>wholly-owned | controlling | minority | leased | licensed (<see cref="Kinds"/>).</summary>
    public string Kind { get; set; } = "wholly-owned";

    /// <summary>Percent of the owned thing the owner holds (0-100), when the story says.</summary>
    public decimal? StakePercent { get; set; }

    /// <summary>In-world year the ownership began, when known.</summary>
    public int? SinceYear { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public Entity? Owner { get; set; }
    public Entity? Owned { get; set; }

    public static readonly string[] Kinds = ["wholly-owned", "controlling", "minority", "leased", "licensed"];
}

/// <summary>
/// One of a CorpoNation's own forces. Every CorpoNation keeps a private military and can call up the
/// forces of every subsidiary beneath it (<c>CorporateHierarchyService.CallableForcesAsync</c>).
/// <see cref="ForceEntityId"/> points at the entity that IS the force (a faction, a subsidiary) when
/// it has one.
/// </summary>
public class CorporateForce
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UniverseId { get; set; }

    /// <summary>FK to Entities.Id — the corponation or subsidiary that keeps this force.</summary>
    public Guid CorponationEntityId { get; set; }

    public string Name { get; set; } = "";

    /// <summary>private-military | civil-security | strike | naval | drone | orbital | other (<see cref="Kinds"/>).</summary>
    public string Kind { get; set; } = "private-military";

    /// <summary>FK to Entities.Id — the entity that is this force, when there is one.</summary>
    public Guid? ForceEntityId { get; set; }

    /// <summary>Free text: "two divisions", "a dozen gunships", "every beat cop in the district".</summary>
    public string? Strength { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public Entity? Corponation { get; set; }
    public Entity? ForceEntity { get; set; }

    public static readonly string[] Kinds = ["private-military", "civil-security", "strike", "naval", "drone", "orbital", "other"];
}
