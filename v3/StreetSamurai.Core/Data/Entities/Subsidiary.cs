namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Subsidiary — fully relational. ParentCorponationId resolves to a Corponation
// entity. KnownProducts becomes a bridge with optional resolved-FK to whatever
// product entity (weapon/equipment/etc.) the name refers to.
// ─────────────────────────────────────────────────────────────────────────────

public class Subsidiary
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name so the Subsidiaries table is queryable standalone.</summary>
    public string Name { get; set; } = "";

    // Indexed classification (kept from prior schema).
    public string Sector { get; set; } = "";
    public string Tier   { get; set; } = "";

    /// <summary>FK to Entities.Id of the parent Corponation. Null when unresolved.</summary>
    public Guid? ParentCorponationId { get; set; }

    public string ParentCorponationAlias { get; set; } = "";
    public string LineOfBusiness { get; set; } = "";
    public string Description { get; set; } = "";
    public bool   PublicFacing { get; set; }
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";

    public Entity? Entity { get; set; }
    public Entity? ParentCorponation { get; set; }
    public ICollection<SubsidiaryProduct> KnownProducts { get; set; } = new List<SubsidiaryProduct>();
}

/// <summary>A product the subsidiary makes. ProductEntityId resolves to a weapon / equipment / etc. when canon.</summary>
public class SubsidiaryProduct
{
    public long Id { get; set; }
    public Guid SubsidiaryId { get; set; }
    public Guid? ProductEntityId { get; set; }
    public string Alias { get; set; } = "";
    public int Position { get; set; }
    public Subsidiary? Subsidiary { get; set; }
    public Entity? Product { get; set; }
}
