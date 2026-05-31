namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One persona-based reader review of a <see cref="Strand"/>. A Legion persona
/// (from the 1000-persona library) reads the whole strand and, IN CHARACTER,
/// writes an honest review with a 1-100 score plus concrete improvement notes —
/// like a reader on a book site. Append-only; a strand accrues many reviews
/// across runs.
///
/// Strands/Beats are NOT system-versioned (vector-index incompatible), so there
/// is no automatic row history. <see cref="ContentHash"/> + <see cref="BeatCount"/>
/// fingerprint the exact text the reviewer read, so a review can be tied to the
/// VERSION of the strand it was written against and flagged stale after edits.
/// </summary>
public class StrandReview
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    public Guid StrandId { get; set; }
    public Strand? Strand { get; set; }

    /// <summary>Stable Legion persona id, e.g. "persona-0042" — maps back to
    /// the exact reviewer in <c>PersonaLibrary</c>.</summary>
    public string PersonaId { get; set; } = "";

    /// <summary>Unique persona display name, e.g. "Margaret A.".</summary>
    public string PersonaName { get; set; } = "";

    /// <summary>First line of the persona's prompt (their who-they-are blurb) —
    /// kept for display so the reader's identity is legible at a glance.</summary>
    public string? PersonaBlurb { get; set; }

    /// <summary>Trusted provider that voiced this persona — "claude" / "openai"
    /// / "gemini" / "deepseek".</summary>
    public string ProviderId { get; set; } = "";

    /// <summary>Concrete model used, e.g. "claude-sonnet-4-6".</summary>
    public string? Model { get; set; }

    /// <summary>The reader's honest overall score, 1-100.</summary>
    public int Score { get; set; }

    /// <summary>Narrative-flow / cohesion score, 1-100 (study mode): does the
    /// story hang together as a SEQUENCE — momentum, setups paying off, clean
    /// transitions — independent of standalone beat quality. Guards against
    /// optimizing beats into "great paragraphs, no tissue." Null outside study mode.</summary>
    public int? FlowScore { get; set; }

    /// <summary>The full free-form review text, in the persona's voice.</summary>
    public string ReviewText { get; set; } = "";

    /// <summary>Concrete improvement notes, newline-joined (grammar, prose,
    /// dialogue, pacing, clarity, ending, …).</summary>
    public string? Improvements { get; set; }

    /// <summary>SHA-256 (hex) of the ordered beat text the reviewer read —
    /// identifies which version of the strand this review is about.</summary>
    public string ContentHash { get; set; } = "";

    /// <summary>Beat count of the reviewed version.</summary>
    public int BeatCount { get; set; }

    /// <summary>The focus group this review belongs to (which panel was in the
    /// room). Null for ungrouped/legacy runs.</summary>
    public Guid? FocusGroupId { get; set; }

    /// <summary>Denormalized group name (e.g. "Group A") for display/filtering.</summary>
    public string? FocusGroupName { get; set; }

    /// <summary>Emergent-cluster assignment from a segment study (set during
    /// post-run analysis; null outside study mode). Id is the cluster index;
    /// Label is the human signature ("loves the anomaly, hates the lore-dump").</summary>
    public int? ClusterId { get; set; }
    public string? ClusterLabel { get; set; }

    /// <summary>This review's per-beat micro-scores (study mode only).</summary>
    public List<StrandReviewBeatScore> BeatScores { get; set; } = new();

    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
