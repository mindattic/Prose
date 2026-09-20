namespace Prose.Core.Data.Entities;

/// <summary>
/// Records that a book was actually read front-to-back as one continuous sequence, as opposed
/// to swept in scoped/parallel chunks. See <see cref="Services.SequentialReadTrackingService"/>.
/// </summary>
public class BookSequentialRead
{
    public long Id { get; set; }
    public Guid NodeId { get; set; }
    public Guid UniverseId { get; set; }
    public string BeatSequenceHash { get; set; } = "";
    public int BeatCount { get; set; }
    public int ChapterCount { get; set; }
    public int StageCount { get; set; } = 1;
    public string ReadBy { get; set; } = "";
    public string? FindingsSummary { get; set; }
    public DateTime ReadAt { get; set; }
}
