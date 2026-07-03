namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One step in a node's audio lifecycle — the granular, append-only history
/// the publication process leaves behind. Node-scoped (so it spans both
/// recording and publishing) and optionally tied to a <see cref="NodePublication"/>
/// run via <see cref="PublicationId"/>.
///
/// <see cref="Kind"/> values:
/// <list type="bullet">
/// <item><c>beat-recorded</c> — a beat's audio was synthesised (during
///   narration / Live Broadcast). <see cref="PublicationId"/> is null.</item>
/// <item><c>publish-started</c> / <c>publish-completed</c> / <c>publish-failed</c>
///   — a Publish run's lifecycle markers.</item>
/// <item><c>beat-assembled</c> — a beat's audio was stitched into the combined
///   file during a Publish run.</item>
/// <item><c>wav-exported</c> — the combined lossless WAV was built.</item>
/// <item><c>mp3-produced</c> — the final MP3 was transcoded + written.</item>
/// </list>
/// </summary>
public class NodeAudioEvent
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    public Guid NodeId { get; set; }

    /// <summary>The beat this event concerns, when applicable (beat-recorded,
    /// beat-assembled). Null for run-level events.</summary>
    public Guid? BeatId { get; set; }

    /// <summary>The Publish run this event belongs to, when it happened inside
    /// one. Null for recording-phase events (beat-recorded).</summary>
    public Guid? PublicationId { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;

    /// <summary>Event kind — see the class summary for the vocabulary.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Human-readable specifics (voice, byte size, position, path…).</summary>
    public string? Detail { get; set; }
}
