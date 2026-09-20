using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// RFC 0007 "Universe Interchange" — the Hub's outbound message queue toward other
/// MindAttic apps' Claude Code sessions (ExperimentEve first). A row here is Prose
/// deliberately telling a consumer's terminal "something changed" (a `UserPromptSubmit`
/// hook in the consumer repo drains <c>GET /api/outbox/{consumer}</c> on every prompt and
/// injects the summaries as context — see docs/rfc/0007-universe-interchange.md §5).
///
/// Not universe-scoped: <see cref="Consumer"/> is an external app identity ("eve"), not a
/// Prose Universe row — one consumer can care about events across several universes, and a
/// consumer need not be a Prose universe at all.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Who this event is for — e.g. "eve" (ExperimentEve's outbox hook).</summary>
    [MaxLength(64)]
    public string Consumer { get; set; } = "";

    public DateTime Ts { get; set; } = DateTime.UtcNow;

    /// <summary>Short event category — e.g. "hello", "import_completed", "entity_upserted",
    /// "beat_written". Free text, no enforced vocabulary (new kinds are additive by design).</summary>
    [MaxLength(64)]
    public string Kind { get; set; } = "";

    /// <summary>One-line human-readable summary — this is what actually lands in the
    /// consumer's injected context, so keep it quiet and specific.</summary>
    [MaxLength(1000)]
    public string Summary { get; set; } = "";

    /// <summary>Optional structured payload for a consumer that wants more than the summary.</summary>
    public string? DataJson { get; set; }

    /// <summary>Set the moment a GET without <c>?peek=true</c> reads this row — marks it
    /// delivered without deleting it, so the queue itself is a durable log.</summary>
    public DateTime? DeliveredTs { get; set; }
}
