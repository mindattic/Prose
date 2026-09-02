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

    /// <summary>Legacy in-fiction-calendar validity window. Confirmed dead in the live
    /// generation pipeline (2026-09-02 investigation) — nothing in the modern Nodes/Beats/
    /// BeatNodes schema ever populates or supplies a real story-time DateTime; use
    /// <see cref="ValidFromBeatId"/>/<see cref="ValidUntilBeatId"/> instead. Left in place,
    /// inert, rather than ripped out this pass.</summary>
    public DateTime? StoryValidFrom { get; set; }
    public DateTime? StoryValidUntil { get; set; }

    /// <summary>Reading-order lower bound: this edge is valid starting at this beat
    /// (inclusive), within the SAME book as the beat being checked. Null = valid from the
    /// book's start. See <see cref="Services.NodeWorkbenchService.CheckBeatInRangeAsync"/>.</summary>
    public Guid? ValidFromBeatId { get; set; }

    /// <summary>Reading-order upper bound: this edge is valid up to (exclusive of) this
    /// beat. Null = valid to the book's end (or forever, if <see cref="InvalidatedAt"/> is
    /// also null).</summary>
    public Guid? ValidUntilBeatId { get; set; }

    /// <summary>Hard-delete in DB time (rare; usually use story-time bounds).</summary>
    public DateTime? InvalidatedAt { get; set; }

    public string Source { get; set; } = "canon";

    public Entity? SourceEntity { get; set; }
    public Entity? TargetEntity { get; set; }
}
