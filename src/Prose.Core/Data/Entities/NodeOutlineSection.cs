namespace Prose.Core.Data.Entities;

/// <summary>
/// Structured sections of a story's NodeOutline — replaces the Nodes.NodeOutline text blob.
/// One row per (NodeId, SectionType) pair. The book outline was removed 2026-09-22 (author ruling:
/// a book is its beats, drawing on entities); this table waits for the drop migration.
///
/// SectionType values:
///   ArcSummary      — story arc and premise
///   Characters      — protagonist, antagonist, key cast rules
///   VoiceRegister   — prose voice, register, tone rules
///   NarrativeLocks  — immovable story facts the engine must never contradict
///   BeatSpine       — 14-beat spine outline (hand-authored portion; blueprint is Track B)
/// </summary>
// PENDING DROP (author ruling 2026-09-22): no code reads/writes this; removed by the drop migration.
public class NodeOutlineSection
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public Guid   NodeId      { get; set; }

    /// <summary>ArcSummary | Characters | VoiceRegister | NarrativeLocks | BeatSpine</summary>
    public string SectionType { get; set; } = "";
    public string Content     { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Node? Node { get; set; }
}
