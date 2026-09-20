namespace Prose.Core.Data.Entities;

/// <summary>
/// Distributed work queue for entity review. Multiple remote workers (RunPod pods, etc.)
/// claim batches from this table, run LLM calls locally, and POST results back to the
/// coordinator REST API. The coordinator (Blazor app) is the only process that writes
/// to EntityReviews and Edges — workers are stateless and never touch the DB directly.
///
/// Status flow: pending → claimed → done | failed
/// </summary>
public class EntityReviewQueue
{
    public Guid Id { get; set; }

    public string EntityId { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityName { get; set; } = "";

    /// <summary>pending | claimed | done | failed</summary>
    public string Status { get; set; } = "pending";

    /// <summary>Opaque worker identifier, e.g. "pod-1", "runpod-abc123".</summary>
    public string? ClaimedBy { get; set; }

    public DateTime? ClaimedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Number of times this item has been retried after timeout/failure.</summary>
    public int RetryCount { get; set; }

    /// <summary>Error message if Status=failed.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
