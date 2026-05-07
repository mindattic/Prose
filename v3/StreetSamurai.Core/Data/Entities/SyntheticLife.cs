namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// SyntheticLife — fully relational. Covers the full SyntheticLifeData spectrum
// (Superminds / Rogue AIs / E.L.F.s / Ceramic Men) including the Ceramic-Man-
// only optional fields. Aliases / StoryHooks / KnownAssociations become
// bridges; KnownAssociations resolves to entity FKs.
// ─────────────────────────────────────────────────────────────────────────────

public class SyntheticLife
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name so the SyntheticLives table is queryable standalone.</summary>
    public string Name { get; set; } = "";

    // Indexed classification (kept from prior schema).
    public string KindOfBeing  { get; set; } = "synthetic";
    public string Manufacturer { get; set; } = "";
    public string Tier         { get; set; } = "";

    // Top-level scalars from SyntheticLifeData.
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Classification { get; set; } = "";
    public string Disposition { get; set; } = "";
    public string Habitat { get; set; } = "";
    public string Origin { get; set; } = "";
    public string LifeStatus { get; set; } = "active";
    public string Description { get; set; } = "";
    public string ObservedBehavior { get; set; } = "";
    public string EncounterFrequency { get; set; } = "";
    public int    ConfirmedSightings { get; set; }
    public string Location { get; set; } = "";
    public double DtiRating { get; set; }
    public bool   Paratechnological { get; set; }
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";

    // Ceramic Man optional fields.
    public string? KnownAge { get; set; }
    public string? CrackPattern { get; set; }
    public string? CurrentRole { get; set; }
    public string? KnownLocation { get; set; }
    public string? DiplomaticSpecialty { get; set; }
    public string? OperatingHistory { get; set; }
    public string? BehavioralNotes { get; set; }
    public string? DamageHistory { get; set; }
    public string? FaceDecoration { get; set; }

    public Entity? Entity { get; set; }
    public ICollection<SyntheticLifeAlias>          Aliases           { get; set; } = new List<SyntheticLifeAlias>();
    public ICollection<SyntheticLifeStoryHook>      StoryHooks        { get; set; } = new List<SyntheticLifeStoryHook>();
    public ICollection<SyntheticLifeKnownAssociation> KnownAssociations { get; set; } = new List<SyntheticLifeKnownAssociation>();
}

public class SyntheticLifeAlias
{
    public long Id { get; set; }
    public Guid SyntheticLifeId { get; set; }
    public int Position { get; set; }
    public string Value { get; set; } = "";
    public SyntheticLife? SyntheticLife { get; set; }
}

public class SyntheticLifeStoryHook
{
    public long Id { get; set; }
    public Guid SyntheticLifeId { get; set; }
    public int Position { get; set; }
    public string Hook { get; set; } = "";
    public SyntheticLife? SyntheticLife { get; set; }
}

/// <summary>"Known to associate with" — alias + resolved-FK to any-type entity.</summary>
public class SyntheticLifeKnownAssociation
{
    public long Id { get; set; }
    public Guid SyntheticLifeId { get; set; }
    public Guid? AssociateEntityId { get; set; }
    public string Alias { get; set; } = "";
    public int Position { get; set; }
    public SyntheticLife? SyntheticLife { get; set; }
    public Entity? Associate { get; set; }
}
