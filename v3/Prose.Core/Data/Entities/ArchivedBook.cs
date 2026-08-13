namespace Prose.Core.Data.Entities;

/// <summary>
/// A full-text snapshot of a node's assembled manuscript at one point in time —
/// the only historical record kept once Beats/Nodes/BeatNodes stopped being
/// system-versioned. Written automatically whenever a complete markdown export
/// succeeds (<see cref="Prose.Core.Services.ManuscriptExportService.ExportMarkdownAsync"/>)
/// and, defensively, immediately before <c>--reimport-node</c> replaces a node's
/// live beats.
///
/// If something from the past is ever needed, it gets parsed back out of the
/// <see cref="Markdown"/> text here — the live Beats/BeatNodes rows are never
/// asked to hold more than one, current version of anything.
/// </summary>
public class ArchivedBook
{
    public Guid Id { get; set; }

    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    /// <summary>The exporting node's Title at snapshot time — kept redundant so
    /// this row is still legible if the node itself is later renamed or deleted.</summary>
    public string Title { get; set; } = "";

    /// <summary>Node.Version at the time of this snapshot (the KDP publish counter),
    /// or 0 for a pre-reimport safety snapshot that isn't tied to a publish.</summary>
    public int Version { get; set; }

    /// <summary>Why this snapshot was taken: "export" (a normal completed export)
    /// or "pre-reimport" (captured immediately before a wholesale beat replacement).</summary>
    public string Reason { get; set; } = "export";

    /// <summary>The full beat-marked markdown — same content <c>--publish-md</c> writes
    /// to disk, kept here as the durable copy.</summary>
    public string Markdown { get; set; } = "";

    public int BeatCount { get; set; }
    public int WordCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
