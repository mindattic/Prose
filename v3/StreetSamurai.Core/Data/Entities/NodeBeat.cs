namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Ordered membership: a <see cref="Beat"/> appears in a <see cref="Node"/>
/// at a fractional <see cref="SortKey"/>. A single beat may live in many
/// nodes (composition over duplication); the prose and audio are owned
/// by the Beat row itself, so editing in one place updates everywhere.
/// </summary>
public class NodeBeat
{
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    public Guid BeatId { get; set; }
    public Beat? Beat { get; set; }

    /// <summary>Fractional sort key within the node. Inserts between
    /// existing siblings find a midpoint — no downstream renumbering.</summary>
    public double SortKey { get; set; }

    /// <summary>False when the beat has been soft-deleted from this node.
    /// The Beat row is never hard-deleted so all text and temporal history
    /// remain accessible. <see cref="NodeWorkbenchService.RestoreBeatAsync"/>
    /// sets this back to true.</summary>
    public bool IsEnabled { get; set; } = true;
}
