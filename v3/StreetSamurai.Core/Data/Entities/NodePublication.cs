namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One Publish run for a <see cref="Node"/> — the HEADER of a publication.
/// 1:M: a node accrues a history of publications (each Publish stitches a
/// fresh combined file and appends a row). The granular per-step history
/// (every beat assembled, the exported WAV, the final MP3) lives in
/// <see cref="NodeAudioEvent"/> rows linked by <see cref="Id"/>. The node's
/// <see cref="Node.CombinedAudioPath"/> always points at the most recent
/// completed publication's file.
/// </summary>
public class NodePublication
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>"running" | "completed" | "failed".</summary>
    public string Status { get; set; } = "running";

    /// <summary>Container format of the published file — "mp3" or "wav".</summary>
    public string Format { get; set; } = "";

    /// <summary>Relative combined-audio path, e.g. <c>{slug}/node.mp3</c>.</summary>
    public string? Path { get; set; }

    /// <summary>Number of beats stitched into this publication.</summary>
    public int BeatCount { get; set; }

    /// <summary>Size of the published file in bytes.</summary>
    public long ByteSize { get; set; }

    /// <summary>Failure note when <see cref="Status"/> is "failed".</summary>
    public string? Error { get; set; }

    // The per-step process events live in NodeAudioEvent rows carrying this
    // run's Id in their soft PublicationId column — no EF navigation, so the
    // ledger is a flat append-only log that outlives any deleted publication.
}
