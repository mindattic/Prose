namespace Prose.Core.Data.Entities;

/// <summary>
/// Amazon-style aggregate summary of all <see cref="EntityReview"/> rows for one
/// entity. Upserted after each review run — one row per entity at most.
/// </summary>
public class EntityReviewSummary
{
    public Guid Id { get; set; }

    public string EntityId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityName { get; set; } = "";

    public int ReviewCount { get; set; }
    public double AvgScore { get; set; }

    /// <summary>JSON: {"10-19":0,"20-29":2,...,"90-100":5} — score bucket counts.</summary>
    public string? ScoreDistributionJson { get; set; }

    /// <summary>LLM-synthesized "what readers say" summary.</summary>
    public string? SummaryMarkdown { get; set; }

    /// <summary>ContentHash of the latest review batch — indicates which version
    /// of the entity the summary reflects.</summary>
    public string ContentHash { get; set; } = "";

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
