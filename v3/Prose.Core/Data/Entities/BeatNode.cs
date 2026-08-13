namespace Prose.Core.Data.Entities;

/// <summary>
/// Ordered membership: a <see cref="Beat"/> appears in a <see cref="Node"/>
/// at a fractional <see cref="SortKey"/>. A single beat may live in many
/// nodes (composition over duplication); the prose and audio are owned
/// by the Beat row itself, so editing in one place updates everywhere.
/// </summary>
public class BeatNode
{
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    public Guid BeatId { get; set; }
    public Beat? Beat { get; set; }

    /// <summary>Fractional sort key within the node. Inserts between
    /// existing siblings find a midpoint — no downstream renumbering.</summary>
    public double SortKey { get; set; }

    // No IsEnabled. A BeatNode row exists or it doesn't — there is no disabled
    // state to be in. A superseded or removed beat gets its BeatNode (and, if
    // now orphaned, its Beat) row physically deleted — see NodeBeatWriter and
    // ReimportNodeCli in Prose.Cli.
}
