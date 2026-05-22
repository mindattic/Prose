namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// The silence (or short ambient audio) between two adjacent beats. First-class
/// entity so users can adjust the gap duration, attach a recorded clip, and
/// reference it by stable handle just like a beat — a strand is an alternating
/// sequence of <see cref="Beat"/>s and <see cref="Gap"/>s, all of which are
/// recordable and concat into the combined narration.
///
/// Lazy materialisation: a Gap row only exists when the user has customised
/// it. For default gaps, the silence is computed at concat time from the
/// adjacent beats' metadata (see <c>StrandWorkbenchService.ComputeTrailingSilenceMs</c>)
/// — no row needed. The trade-off keeps the table small at the cost of one
/// branch in the concat path.
///
/// The Gap is anchored by the (<see cref="AboveBeatId"/>, <see cref="BelowBeatId"/>)
/// pair. If the beats are later reordered so they're no longer adjacent, the
/// Gap row becomes orphaned — not rendered, can be swept by a cleanup pass.
/// </summary>
public class Gap
{
    /// <summary>UUIDv7 — sortable by creation time, globally unique.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable, globally unique integer handle. Mirrors
    /// <see cref="Beat.Number"/> so the writer and a CLI assistant can refer
    /// to a gap as "Gap #47" without copying a guid.</summary>
    public int Number { get; set; }

    /// <summary>The beat directly above this gap in the strand's reading order.</summary>
    public Guid AboveBeatId { get; set; }

    /// <summary>The beat directly below this gap in the strand's reading order.</summary>
    public Guid BelowBeatId { get; set; }

    /// <summary>Silence in milliseconds. When <see cref="AudioPath"/> is null,
    /// this many ms of digital silence is inserted between the two beats. When
    /// <see cref="AudioPath"/> is set, this field is informational only —
    /// the recorded clip's own length is what plays.</summary>
    public int DurationMs { get; set; }

    /// <summary>Optional path to a recorded audio clip for non-silence gaps
    /// (rain, ambient room tone, a long sigh, a phone vibrating). Relative to
    /// the strands audio root. When set, the silence engine inserts the file
    /// contents instead of generating digital silence.</summary>
    public string? AudioPath { get; set; }

    /// <summary>When the recorded clip was last written.</summary>
    public DateTime? NarratedAt { get; set; }

    /// <summary>ElevenLabs request id of the rendering, if this gap was
    /// synthesised from a prompt (rare — mostly for narrated transitions).</summary>
    public string? LastRequestId { get; set; }

    /// <summary>Free-form description of what the gap is doing — "long sigh",
    /// "rain", "phone vibrates", "sound of footsteps fading". The writer
    /// can fill this in before the audio is recorded; the recorder reads it
    /// as the prompt.</summary>
    public string? Notes { get; set; }

    /// <summary>True when the duration or notes were edited past the
    /// recorded audio. Concat skips stale gap audio and falls back to
    /// digital silence of <see cref="DurationMs"/>.</summary>
    public bool Stale { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
