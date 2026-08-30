namespace Prose.Core.Data.Entities;

/// <summary>
/// Structured sections of a story's NodeOutline — replaces the Nodes.NodeOutline text blob.
/// One row per (NodeId, SectionType) pair. The generated docs/nodes/&lt;CODE&gt;.md file is
/// assembled from these rows; generate_node_doc reads here, not the old blob.
///
/// SectionType values:
///   ArcSummary      — story arc and premise
///   Characters      — protagonist, antagonist, key cast rules
///   VoiceRegister   — prose voice, register, tone rules
///   NarrativeLocks  — immovable story facts the engine must never contradict
///   BeatSpine       — 14-beat spine outline (hand-authored portion; blueprint is Track B)
/// </summary>
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
