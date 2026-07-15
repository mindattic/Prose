namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Per-beat structural contract — the explicit declaration of what a beat must accomplish,
/// what world-state it assumes on entry, and what world-state it produces on exit.
///
/// This replaces the JSON blob columns on NodeStructuralBlueprints (EscalationCurveJson,
/// EventTypePaletteJson) with queryable, verifiable rows. One row per beat per story.
///
/// Set BEFORE prose generation (B3 generates these rows; B5 blocks generation without one).
/// Verified AFTER prose generation (Track C reads these rows to assess fulfillment).
/// </summary>
public class BeatBlueprintDecision
{
    public Guid Id          { get; set; } = Guid.NewGuid();

    /// <summary>The beat this contract governs. Unique — one decision per beat.</summary>
    public Guid BeatId      { get; set; }

    /// <summary>The story-level blueprint this row derives from.</summary>
    public Guid BlueprintId { get; set; }

    /// <summary>Revelation | Confrontation | Transition | EmotionalClimax | Chase |
    /// Confession | Ceremony | Negotiation | Ambush | Loss | Betrayal | Discovery |
    /// Ritual | Reckoning — from the story's EventTypePalette. No back-to-back repeats.</summary>
    public string? EventType       { get; set; }

    /// <summary>Minimum acceptable emotional intensity (1–10) for this beat as declared
    /// in the escalation curve. ProseWriterRouter passes this to PacingService.</summary>
    public decimal? EscalationFloor { get; set; }

    /// <summary>True when this beat is required to advance the subplot.</summary>
    public bool SubplotCarrier     { get; set; }

    /// <summary>Linear | Flashback | FlashForward | Parallel</summary>
    public string? AnachronyType   { get; set; }

    /// <summary>The beat's contract in plain language — "Rook learns Helix hired the hit."
    /// Injected into the generation prompt as an explicit target. Used by the semantic
    /// verification check (C3: DeclaredPurpose) to assess whether prose fulfilled it.</summary>
    public string? DeclaredPurpose { get; set; }

    /// <summary>Entity states and facts that must hold at the START of this beat.
    /// Verified mechanically against EntityStateAtBeat for the prior beat (C2: WorldStatePre).</summary>
    public string? WorldStatePre   { get; set; }

    /// <summary>Entity states that this beat changes — what becomes true after the prose ends.
    /// Written to EntityStateAtBeat with Source=Declared after generation (B5).</summary>
    public string? WorldStatePost  { get; set; }

    /// <summary>BREATHE | FLOW | TIGHTEN | STRIKE | SETTLE — passed to PacingService.</summary>
    public string? PacingDirective { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Beat?                     Beat      { get; set; }
    public NodeStructuralBlueprint?  Blueprint { get; set; }
}
