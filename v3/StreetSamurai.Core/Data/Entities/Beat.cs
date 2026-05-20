using System.ComponentModel.DataAnnotations;

namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One paragraph of prose, plus its one canonical audio rendering. The atom
/// of the storytelling engine — every word the user writes ends up in a Beat
/// row, every audio file points back at one. Replaces the old
/// <c>ChapterBeat</c> + <c>EpisodeBeat</c> split: prose and audio used to
/// live in two tables linked by a string-guid pointer with bidirectional
/// sync; now they're one row.
///
/// Beats belong to one or more <see cref="Strand"/>s through the
/// <see cref="StrandBeat"/> junction. A Beat can appear in many strands
/// (the same paragraph reused across an anthology, a chapter, and a
/// greatest-hits playlist) — the prose lives in one place, the audio file
/// lives in one place, edits propagate naturally.
/// </summary>
public class Beat
{
    /// <summary>UUIDv7 — sortable by creation time, globally unique.</summary>
    public Guid Id { get; set; }

    /// <summary>Optional human-readable identifier. Used for stable URLs and
    /// debugging. Not required — most beats won't have one.</summary>
    public string? Slug { get; set; }

    /// <summary>The paragraph. Authoritative; nothing else holds a copy.</summary>
    public string Text { get; set; } = "";

    /// <summary>SHA-256 hex of the prose at the last point it was either
    /// hand-edited or successfully narrated. The Stale flag is computed by
    /// comparing this against a fresh hash of <see cref="Text"/>. Survives
    /// re-record so single-beat re-renders can stitch from the persisted
    /// <see cref="LastRequestId"/> of neighbours.</summary>
    public string? TextHash { get; set; }

    // ── Narrative metadata ───────────────────────────────────────────────

    /// <summary>Optional short label for the beat. e.g. "The threshold".</summary>
    public string? BeatTitle { get; set; }

    /// <summary>One-line of what this beat is doing — "Kyle reads the room
    /// and decides he's not leaving." Feeds into LLM regeneration prompts
    /// and ElevenLabs tone direction.</summary>
    public string? Synopsis { get; set; }

    /// <summary>Story-structure role: "inciting-incident" / "rising-action"
    /// / "climax" / "denouement" / "transition" / "scene-break". Free-form.</summary>
    public string? StructureRole { get; set; }

    /// <summary>Plot-structure act number (1-5). Zero = unassigned.</summary>
    public int Act { get; set; }

    /// <summary>"scene" | "summary" | "transition" | "interstitial".</summary>
    public string SceneType { get; set; } = "scene";

    /// <summary>Character voice facet — "WOUND", "IDEAL", "MASK", "SHADOW".
    /// Drives narration tone.</summary>
    public string? FacetTag { get; set; }

    /// <summary>Emotional charge: "tense", "wry", "tender", "violent", "quiet".</summary>
    public string? EmotionalTone { get; set; }

    /// <summary>Pace hint: "clipped", "languorous", "staccato", "flowing".</summary>
    public string? PaceHint { get; set; }

    // ── Audio ────────────────────────────────────────────────────────────

    /// <summary>ElevenLabs voice id of the canonical rendering. Null = use
    /// the strand's default voice when narrating.</summary>
    public string? VoiceId { get; set; }

    /// <summary>Relative path under engine/audio/ to the .wav or .mp3 file.
    /// Null = not yet narrated, or invalidated by prose drift.</summary>
    public string? AudioPath { get; set; }

    /// <summary>When the audio file was last written.</summary>
    public DateTime? NarratedAt { get; set; }

    /// <summary>Narration duration in seconds. Null = not yet narrated.</summary>
    public double? DurationSec { get; set; }

    /// <summary>ElevenLabs request id from the last successful synthesis.
    /// Used to seed the stitching window for neighbouring re-records so
    /// single-beat re-renders match the surrounding cadence.</summary>
    public string? LastRequestId { get; set; }

    /// <summary>True when prose has been edited past the recorded audio.
    /// Combined-audio export skips stale beats; the unified writer/recorder
    /// surfaces them for re-record with one click.</summary>
    public bool Stale { get; set; }

    /// <summary>True if the beat has been manually rewritten since materialisation.</summary>
    public bool WasCorrected { get; set; }

    // ── Provenance ───────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last-modified timestamp + optimistic-concurrency token. Every
    /// write bumps this. EF's UPDATE includes <c>WHERE UpdatedAt = @loadedAt</c>
    /// so a same-instant race between two clients fails one of them with
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>.
    /// The workbench also exposes an <c>expectedUpdatedAt</c> parameter so
    /// the UI can detect the longer "user opened the editor, walked away,
    /// another tab edited it" race window.</summary>
    [ConcurrencyCheck]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Reverse navigation — which strands include this beat. Set
    /// by EF Core through the StrandBeats junction.</summary>
    public List<StrandBeat> StrandBeats { get; set; } = new();
}
