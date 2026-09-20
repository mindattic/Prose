namespace Prose.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Corponation — fully relational. Most of CorponationData is straight scalar
// fields; the only collection is CommonNames which becomes its own bridge.
// ─────────────────────────────────────────────────────────────────────────────

public class Corponation
{
    public Guid Id { get; set; }
    /// <summary>Common-use name (e.g. "Arcturus"). Mirrors Entity.Name. Distinct from FullLegalName.</summary>
    public string Name { get; set; } = "";

    // Indexed classification (kept from prior schema for filtering).
    public string Sector       { get; set; } = "";
    public string Tier         { get; set; } = "";
    public string Headquarters { get; set; } = "";

    // Top-level scalars from CorponationData.
    public int    Number { get; set; }
    public double Rating { get; set; }
    public int    VoteCount { get; set; }
    public string FullLegalName { get; set; } = "";
    public string StockDesignation { get; set; } = "";
    public string Valuation { get; set; } = "";
    public string Revenue { get; set; } = "";
    public string Employees { get; set; } = "";
    public string SovereignTerritory { get; set; } = "";
    public string FoundingStory { get; set; } = "";
    public string SecurityForce { get; set; } = "";
    public string KeyDetail { get; set; } = "";
    public string RelationshipToBig20 { get; set; } = "";
    public string FullText { get; set; } = "";
    public string MidjourneyPrompt { get; set; } = "";
    public string Dalle3Prompt { get; set; } = "";

    public Entity? Entity { get; set; }
    public ICollection<CorponationCommonName> CommonNames { get; set; } = new List<CorponationCommonName>();
}

public class CorponationCommonName
{
    public long Id { get; set; }
    public Guid CorponationId { get; set; }
    public int Position { get; set; }
    public string Value { get; set; } = "";
    public Corponation? Corponation { get; set; }
}
