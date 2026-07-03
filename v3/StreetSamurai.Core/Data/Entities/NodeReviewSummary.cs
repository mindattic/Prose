namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// The Amazon-style aggregate of a node's <see cref="NodeReview"/>s: average
/// score, score distribution, and a synthesized prose summary of what readers
/// liked and the recurring concrete improvements they asked for. One latest row
/// per node (unique on <see cref="NodeId"/>), regenerated/upserted after
/// each review run.
/// </summary>
public class NodeReviewSummary
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Number of reviews this summary was computed from.</summary>
    public int ReviewCount { get; set; }

    /// <summary>Mean score across the reviews, 1-100.</summary>
    public double AvgScore { get; set; }

    /// <summary>JSON of score-bucket counts (e.g. {"1-20":3,"21-40":...}).</summary>
    public string? ScoreDistributionJson { get; set; }

    /// <summary>Synthesized "what readers think" summary — recurring strengths,
    /// the top concrete improvements, and the score spread.</summary>
    public string SummaryMarkdown { get; set; } = "";

    /// <summary>Content fingerprint the summary was computed against (matches
    /// the reviews' <see cref="NodeReview.ContentHash"/>).</summary>
    public string? ContentHash { get; set; }
}
