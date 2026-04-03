using QuikGraph;

namespace StreetSamurai.Core.Models.Graph;

/// <summary>
/// A relationship between two entities in the world graph.
/// Uses bi-temporal validity: ValidFrom/ValidUntil track when the relationship
/// is true in the STORY world (e.g. "Sable was alive from chapter 1 to chapter 12").
/// CreatedAt/InvalidatedAt track when the record was created/superseded in the DATABASE.
/// </summary>
public record WorldEdge : IEdge<string>
{
    public string Source { get; init; } = "";
    public string Target { get; init; } = "";
    public string RelationType { get; init; } = "";
    public double Weight { get; init; } = 1.0;
    public string Sentiment { get; init; } = "neutral";
    public string Description { get; init; } = "";
    public string Status { get; init; } = "canon";
    public DateTime LastModified { get; init; } = DateTime.UtcNow;
    public string ModifiedBy { get; init; } = "";

    // ── Temporal validity (story time) ──
    /// <summary>When this relationship became true in the story (e.g. "chapter:3" or a story timestamp).</summary>
    public string ValidFrom { get; init; } = "";
    /// <summary>When this relationship stopped being true. Empty = still current.</summary>
    public string ValidUntil { get; init; } = "";

    // ── Database time ──
    /// <summary>When this record was created in the graph.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    /// <summary>When this record was superseded by a newer version. Null = current.</summary>
    public DateTime? InvalidatedAt { get; init; }

    /// <summary>Is this edge currently valid (not invalidated)?</summary>
    public bool IsCurrent => InvalidatedAt == null && string.IsNullOrEmpty(ValidUntil);
}
