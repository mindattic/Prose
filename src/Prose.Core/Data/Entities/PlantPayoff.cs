namespace Prose.Core.Data.Entities;

/// <summary>
/// A seeded narrative detail (plant) and its payoff, persisted per node.
/// Enforces the principle: "reward re-reading without requiring it."
/// IsTransparent must be true before a node is considered gateway-ready.
/// </summary>
public class PlantPayoff
{
    /// <summary>Column limit for PlantDescription, PayoffDescription and TransparencyNote.</summary>
    public const int MaxDescriptionLength = 1500;

    public Guid   Id                { get; set; } = Guid.NewGuid();
    public Guid   UniverseId        { get; set; }
    public Guid   NodeId          { get; set; }

    /// <summary>Beat where the detail is seeded. Null = planned but not yet written.</summary>
    public Guid?  PlantBeatId       { get; set; }

    /// <summary>Beat where the detail pays off. Null = payoff not yet written.</summary>
    public Guid?  PayoffBeatId      { get; set; }

    /// <summary>What is seeded — visible to writer, invisible to the cold reader as a plant.</summary>
    public string PlantDescription  { get; set; } = "";

    /// <summary>How it pays off — what the re-reader gets that the first-timer doesn't.</summary>
    public string PayoffDescription { get; set; } = "";

    /// <summary>detail | echo | irony | motif | character-truth | structural</summary>
    public string Category          { get; set; } = "detail";

    /// <summary>
    /// True = the payoff beat reads completely for a first-time reader without the plant.
    /// False = writing bug — the payoff is opaque without catching the plant.
    /// </summary>
    public bool   IsTransparent     { get; set; } = true;

    /// <summary>What the returning reader gains that the cold reader doesn't.</summary>
    public string? TransparencyNote { get; set; }

    public double   SortKey    { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt  { get; set; } = DateTime.UtcNow;

    /// <summary>The ledger row this pair annotates (RFC 0013). A plant pair IS an obligation of
    /// kind <c>plant</c>; this back-link lets the trial balance and the Brief treat hand-curated
    /// pairs and extracted promises as one population.</summary>
    public Guid? ObligationId { get; set; }

    // Navigation
    public Node? Node     { get; set; }
    public Beat?   PlantBeat  { get; set; }
    public Beat?   PayoffBeat { get; set; }
    public NarrativeObligation? Obligation { get; set; }
}
