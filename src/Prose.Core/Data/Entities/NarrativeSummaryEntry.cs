namespace Prose.Core.Data.Entities;

/// <summary>
/// One compressed scene summary produced by NarrativeSummaryService.
/// Persisted so the summary chain survives app restarts — without this, the chain
/// resets between sessions and coherence falls back to the (now capped) SceneSoFar.
/// </summary>
// PENDING DROP (author ruling 2026-09-22): no code reads/writes this; removed by the drop migration.
public class NarrativeSummaryEntry
{
    public Guid   Id        { get; set; }
    public Guid   NodeId    { get; set; }
    public Node?  Node      { get; set; }

    /// <summary>Beat this summary was produced from. Null when seeded manually.</summary>
    public Guid?  BeatId    { get; set; }

    /// <summary>1-based position in the summary chain for this node.</summary>
    public int    SortKey   { get; set; }

    /// <summary>Compressed 3–4 sentence scene summary (max 2000 chars).</summary>
    public string Summary   { get; set; } = "";

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
