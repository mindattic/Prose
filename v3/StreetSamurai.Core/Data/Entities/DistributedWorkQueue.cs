namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Distributed work queue — one row per unit of work to be processed by a remote worker.
/// Workers claim batches, call their local LLM, and POST results back to the coordinator REST API.
/// The coordinator (Blazor app) is the only process that writes to the canonical tables
/// (EntityReviews, NodeReviews, Edges, Beats).
///
/// Status flow: pending → claimed → done | failed
///
/// WorkType values:
///   entity-review   — score an entity (10 persona ballots → EntityReviews + edge extraction)
///   node-review   — score a node (N persona reads → NodeReviews + beat scores)
///   beat-review     — score a single beat (N persona reads → NodeReviewBeatScores)
///   beat-write      — generate prose for a beat (pre-built prompts in PayloadJson)
/// </summary>
public class DistributedWorkQueue
{
    public Guid Id { get; set; }

    /// <summary>entity-review | node-review | beat-review | beat-write</summary>
    public string WorkType { get; set; } = "entity-review";

    /// <summary>Entity GUID (N format), node GUID, or beat GUID depending on WorkType.</summary>
    public string TargetId { get; set; } = "";

    /// <summary>Entity type string, "node", or "beat".</summary>
    public string TargetType { get; set; } = "";

    /// <summary>Human-readable name for logging and status displays.</summary>
    public string TargetName { get; set; } = "";

    /// <summary>
    /// Work-type-specific payload serialized as JSON. Workers receive this alongside
    /// the claim so they never need to call the coordinator DB.
    ///
    /// entity-review:  { entityDescription, tags, ballots, proseCount, personas:[{personaId,name,blurb}] }
    /// node-review:  { nodeSlug, nodeTitle, beatTexts:[...], readers }
    /// beat-review:    { nodeSlug, beatIndex, beatText, readers }
    /// beat-write:     { nodeId, nodeSlug, beatIndex, totalBeats, systemPrompt, userPrompt }
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>pending | claimed | done | failed</summary>
    public string Status { get; set; } = "pending";

    /// <summary>Opaque worker identifier set by the worker, e.g. "pod-1", "laptop-ryan".</summary>
    public string? ClaimedBy { get; set; }

    public DateTime? ClaimedAt   { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Times this item has been recycled after a claim timeout.</summary>
    public int RetryCount { get; set; }

    /// <summary>Last error if Status=failed.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
