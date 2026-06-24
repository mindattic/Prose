namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Records the detected beat mode (Narrative/Combat/EmotionalClimax/etc.) for each beat.
/// Populated by BeatModeDetector via ProseWriterRouter.
/// Primary key is BeatId — one mode classification per beat.
/// </summary>
public class BeatModeLog
{
    public Guid BeatId { get; set; }
    public Guid UniverseId { get; set; }
    public string Mode { get; set; } = "";
    public float Confidence { get; set; }
    public string DetectionMethod { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
