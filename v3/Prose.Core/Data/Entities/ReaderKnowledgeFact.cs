namespace Prose.Core.Data.Entities;

/// <summary>
/// One fact the reader has been told, extracted from a just-written beat. Read back by
/// <see cref="Prose.Core.Services.ReaderKnowledgeService.BuildKnowledgeBlockAsync"/> to inject
/// "what the reader currently knows" into the next beat's prompt.
///
/// This is live write-time working state, not a human-triaged defect. It used to live in the
/// Findings table (Category=ReaderKnows), which borrowed the wrong lifecycle: FindingStatus.New
/// meant "reader still holds this fact," not "unreviewed defect," and it was permanently inflating
/// the Findings inbox — 1,071 rows that could never be Applied/Dismissed by a human without
/// breaking the context-injection feature that depends on them staying New. Moved to its own
/// table 2026-08-13 so the Findings inbox reflects only things a human should actually triage.
/// </summary>
public class ReaderKnowledgeFact
{
    public long Id { get; set; }
    public Guid NodeId { get; set; }
    public string Fact { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
