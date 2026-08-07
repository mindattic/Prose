namespace Prose.Core.Data.Entities;

/// <summary>
/// One append-only, verifiable record of a change to the world's <em>voice</em> —
/// the codified writing rules that shape generated prose. This is the audit layer
/// that makes the voice's evolution traceable instead of living in an `.md` file
/// that might never be parsed.
///
/// Three sources feed it:
/// <list type="bullet">
///   <item><b>directive</b> — the user asked, in conversation, for a tonal change
///     ("stop the wry universal-truth asides"). Logged when given.</item>
///   <item><b>manual_edit</b> — mined from the temporal beat-version history: the
///     diff between a beat as first generated and as the user hand-edited it.</item>
///   <item><b>harvest</b> — a rule the <c>VoiceHarvestService</c> distilled from a
///     ≥80% node's winning beats (often the commonality across several).</item>
/// </list>
///
/// Nothing here touches canon or the live rules until a <c>proposed</c> entry is
/// approved (→ <c>applied</c>); rejected proposals stay as <c>rejected</c> so the
/// trail shows what was considered and declined.
/// </summary>
public class VoiceChangeLogEntry
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    /// <summary>"directive" | "manual_edit" | "harvest".</summary>
    public string Source { get; set; } = "";

    /// <summary>The node this observation came from, when applicable.</summary>
    public Guid? NodeId { get; set; }

    /// <summary>The specific beat the evidence came from, when applicable.</summary>
    public Guid? BeatId { get; set; }

    /// <summary>For manual_edit: the prose before the user's edit (generated form).
    /// For directive/harvest: optional context. May be long; nullable.</summary>
    public string? Before { get; set; }

    /// <summary>For manual_edit: the prose after the user's edit (their vision).
    /// May be long; nullable.</summary>
    public string? After { get; set; }

    /// <summary>The voice move in verifiable, in-voice terms — "cut a wry
    /// universal-truth aside", "tightened fight choreography to short sentences",
    /// "glossed the corpo on first mention". The human-readable rule.</summary>
    public string Description { get; set; } = "";

    /// <summary>Which codified store this informs: "kyle.narration_voice",
    /// "kyle.speech.cadence", "literary_rules.prohibitions", "tone_bible.tone",
    /// etc. Drives where an approved change is written.</summary>
    public string RuleTarget { get; set; } = "";

    /// <summary>Provenance so the change is auditable — node slug + beat number,
    /// review score, the directive text, etc.</summary>
    public string? Evidence { get; set; }

    /// <summary>"observed" | "proposed" | "applied" | "rejected".</summary>
    public string Status { get; set; } = "observed";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
