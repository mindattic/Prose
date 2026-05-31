namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// The Amazon-style aggregate of a strand's <see cref="StrandReview"/>s: average
/// score, score distribution, and a synthesized prose summary of what readers
/// liked and the recurring concrete improvements they asked for. One latest row
/// per strand (unique on <see cref="StrandId"/>), regenerated/upserted after
/// each review run.
/// </summary>
public class StrandReviewSummary
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    public Guid StrandId { get; set; }
    public Strand? Strand { get; set; }

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
    /// the reviews' <see cref="StrandReview.ContentHash"/>).</summary>
    public string? ContentHash { get; set; }
}
