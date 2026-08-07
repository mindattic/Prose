namespace Prose.Core.Data.Entities;

/// <summary>
/// Typed temporal relationship between two entities. Replaces the in-memory
/// QuikGraph edges that today live in `world_graph.json`. Story-time validity
/// lets dossier queries answer "is Sasha Kyle's apprentice as of Ch5?" without
/// scanning property history.
/// </summary>
public class Edge
{
    public long Id { get; set; }

    /// <summary>The universe this relation belongs to (denormalized from the source entity). Source
    /// and target always share a universe — a cross-universe edge is a bug (RFC 0006).</summary>
    public Guid UniverseId { get; set; }

    public Guid SourceId { get; set; }
    public Guid TargetId { get; set; }

    /// <summary>carries | wields | wears | owns | partner_of | parent_of | child_of | employer_of | works_for | member_of | affiliated_with | located_at | …</summary>
    public string RelationType { get; set; } = "";

    public string? Description { get; set; }

    /// <summary>Edge strength used by adjacency ranking. Default 1.0.</summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>positive | neutral | negative — colors prose tone.</summary>
    public string Sentiment { get; set; } = "neutral";

    public DateTime? StoryValidFrom { get; set; }
    public DateTime? StoryValidUntil { get; set; }

    /// <summary>Hard-delete in DB time (rare; usually use story-time bounds).</summary>
    public DateTime? InvalidatedAt { get; set; }

    public string Source { get; set; } = "canon";

    public Entity? SourceEntity { get; set; }
    public Entity? TargetEntity { get; set; }
}
