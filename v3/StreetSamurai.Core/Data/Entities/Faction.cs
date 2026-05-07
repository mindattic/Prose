namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Faction — fully relational. Members and relationships resolve to Character /
// Faction FKs while preserving the alias string.
// ─────────────────────────────────────────────────────────────────────────────

public class Faction
{
    public Guid Id { get; set; }
    /// <summary>Canonical name. Mirrors Entity.Name so the Factions table is queryable standalone.</summary>
    public string Name { get; set; } = "";

    // Indexed classification.
    public string Sector     { get; set; } = "";
    public string Tier       { get; set; } = "";
    public string Allegiance { get; set; } = "";

    // Top-level scalars.
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string Motto { get; set; } = "";
    public string Description { get; set; } = "";
    public string Ideology { get; set; } = "";
    public string Territory { get; set; } = "";
    public string Leadership { get; set; } = "";
    public string NarrativeFunction { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";

    public Entity? Entity { get; set; }
    public ICollection<FactionAlias>         Aliases       { get; set; } = new List<FactionAlias>();
    public ICollection<FactionMethod>        Methods       { get; set; } = new List<FactionMethod>();
    public ICollection<FactionResource>      Resources     { get; set; } = new List<FactionResource>();
    public ICollection<FactionGoal>          Goals         { get; set; } = new List<FactionGoal>();
    public ICollection<FactionStoryHook>     StoryHooks    { get; set; } = new List<FactionStoryHook>();
    public ICollection<FactionRelationshipRow> Relationships { get; set; } = new List<FactionRelationshipRow>();
    public ICollection<FactionMemberRow>     Members       { get; set; } = new List<FactionMemberRow>();
}

public class FactionAlias
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public int Position { get; set; }
    public string Value { get; set; } = "";
    public Faction? Faction { get; set; }
}

public class FactionMethod
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public int Position { get; set; }
    public string Method { get; set; } = "";
    public Faction? Faction { get; set; }
}

public class FactionResource
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public int Position { get; set; }
    public string Resource { get; set; } = "";
    public Faction? Faction { get; set; }
}

public class FactionGoal
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public int Position { get; set; }
    public string Goal { get; set; } = "";
    public Faction? Faction { get; set; }
}

public class FactionStoryHook
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public int Position { get; set; }
    public string Hook { get; set; } = "";
    public Faction? Faction { get; set; }
}

/// <summary>Faction-to-faction relationships. TargetFactionId resolves to a Faction Entity.</summary>
public class FactionRelationshipRow
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public Guid? TargetFactionId { get; set; }
    public string Alias { get; set; } = "";
    public string RelationshipType { get; set; } = "";
    public string Description { get; set; } = "";
    public int Position { get; set; }
    public Faction? Faction { get; set; }
    public Entity? TargetFaction { get; set; }
}

/// <summary>Faction member. CharacterId resolves to a Character Entity.</summary>
public class FactionMemberRow
{
    public long Id { get; set; }
    public Guid FactionId { get; set; }
    public Guid? CharacterId { get; set; }
    public string Alias { get; set; } = "";
    public string Role { get; set; } = "";
    public string MemberStatus { get; set; } = "active";
    public string Notes { get; set; } = "";
    public int Position { get; set; }
    public Faction? Faction { get; set; }
    public Entity? Character { get; set; }
}
