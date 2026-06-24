namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Records which prose services were applicable and active for a beat write.
/// Populated by ProseWriterRouter. Use ss --workflow-status to inspect coverage gaps.
/// </summary>
public class BeatServiceLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UniverseId { get; set; }
    public Guid? BeatId { get; set; }
    public Guid StrandId { get; set; }
    public string Service { get; set; } = "";
    public bool WasApplicable { get; set; }
    public bool WasActive { get; set; }
    public int BlockSizeChars { get; set; }
    public DateTime WrittenAt { get; set; } = DateTime.UtcNow;
}
