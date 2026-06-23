namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One run of the Emotional Intelligence Examination for a strand (SS-A15).
/// Parent record; cascades to <see cref="EmotionalDimensionResult"/> and
/// <see cref="EmotionalBeatScore"/> children.
/// </summary>
public class EmotionalExamination
{
    public Guid Id { get; set; }
    public Guid StrandId { get; set; }
    public Strand? Strand { get; set; }

    /// <summary>"draft" | "standard" | "deep"</summary>
    public string EffortTier { get; set; } = "standard";

    /// <summary>Mean(dimension / 4) × 100 → 0–100 aggregate.</summary>
    public double EmotionalDepthScore { get; set; }

    /// <summary>Register read from the strand bible: "CODA" | "JOY" | "SORROW" | "Fantasy" | "".</summary>
    public string Register { get; set; } = "";

    /// <summary>SHA-256 of assembled beat text at examination time (staleness marker).</summary>
    public string ContentHash { get; set; } = "";

    public int BeatCount { get; set; }

    /// <summary>Number of blocking dimensions with Score &lt;= 1.</summary>
    public int BlockingCount { get; set; }

    public string? Model { get; set; }

    public DateTime ExaminedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<EmotionalDimensionResult> DimensionResults { get; set; } = new();
    public List<EmotionalBeatScore> BeatScores { get; set; } = new();
}

/// <summary>
/// One dimension's result within an <see cref="EmotionalExamination"/>.
/// PK is (ExaminationId, Dimension).
/// </summary>
public class EmotionalDimensionResult
{
    public Guid ExaminationId { get; set; }
    public EmotionalExamination? Examination { get; set; }

    /// <summary>0–7 enum ordinal for the 8 dimensions.</summary>
    public int Dimension { get; set; }

    /// <summary>0 Absent · 1 Asserted · 2 Mixed · 3 Embodied · 4 Instrument.</summary>
    public int Score { get; set; }

    public string? StrongestEvidence { get; set; }
    public string? WeakestEvidence { get; set; }
    public int? WeakestBeatNumber { get; set; }
    public string? Fix { get; set; }
    public string? CraftLaw { get; set; }

    /// <summary>True for WantNeedDivergence and CostFeltNotAsserted.</summary>
    public bool IsBlocking { get; set; }
}

/// <summary>
/// Per-beat emotional depth score within an <see cref="EmotionalExamination"/>
/// (Pass 2 — Standard/Deep only). PK is (ExaminationId, BeatNumber).
/// </summary>
public class EmotionalBeatScore
{
    public Guid ExaminationId { get; set; }
    public EmotionalExamination? Examination { get; set; }

    public int BeatNumber { get; set; }

    /// <summary>0–4 depth for this beat in context.</summary>
    public int Depth { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// Per-(strand, character) cache of Want/Need/Wound/Flaw parsed from the strand bible.
/// Cache-busted on <see cref="SourceBibleHash"/>. Unique on (StrandId, Character).
/// </summary>
public class CharacterEmotionalLedger
{
    public Guid Id { get; set; }
    public Guid StrandId { get; set; }
    public Strand? Strand { get; set; }

    public string Character { get; set; } = "";
    public string? Want { get; set; }
    public string? Need { get; set; }
    public string? Wound { get; set; }
    public string? Flaw { get; set; }
    public string? VoiceRegister { get; set; }

    /// <summary>True when ledger was inferred from prose (no bible heading found).</summary>
    public bool Inferred { get; set; }

    /// <summary>SHA-256 of Strand.StrandBible at extraction time. Null triggers refresh.</summary>
    public string? SourceBibleHash { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
