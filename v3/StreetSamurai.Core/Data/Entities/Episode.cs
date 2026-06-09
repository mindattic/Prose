namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One bedtime adventure. Folk-hero Kyle, generated on demand, narrated to audio.
/// Distinct from Book/Chapter — episodes are ephemeral, scoreable, and stand alone.
/// Continuity with Bushido Coda canon is respected loosely; episodes do not drive
/// the saga arc.
/// </summary>
public class Episode
{
    /// <summary>UUIDv7 — globally unique, sortable by creation time. Matches the
    /// Entity convention used everywhere else in the canon DB.</summary>
    public Guid Id { get; set; }

    /// <summary>The one-line seed that fed the generator. e.g. "A child rides up to Mrs. Chen's stall with a contract written on a Carrion receipt."</summary>
    public string Seed { get; set; } = "";

    /// <summary>Short generated title used by /inbox.</summary>
    public string Title { get; set; } = "";

    /// <summary>URL-safe slug of the title. Set at generation time, immutable
    /// after. Used as the on-disk directory name under engine/episodes/{slug}/.
    /// Unique across episodes — collisions resolve by appending the integer id.</summary>
    public string Slug { get; set; } = "";

    /// <summary>ElevenLabs voice id used for narration. Null = default voice.</summary>
    public string? VoiceId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GenerationCompletedAt { get; set; }
    public DateTime? AudioCompletedAt { get; set; }

    /// <summary>queued | generating | narrating | ready | failed.</summary>
    public string Status { get; set; } = "queued";

    /// <summary>Sum of characters sent to TTS — cost monitoring.</summary>
    public int CharsNarrated { get; set; }

    /// <summary>Notes about why a run failed, if it did.</summary>
    public string? Error { get; set; }

    /// <summary>Relative path under data root to the Markdown script export.
    /// engine/episodes/{Id}/script.md. Written when generation completes.</summary>
    public string? ScriptMarkdownPath { get; set; }

    /// <summary>Relative path under data root to the PDF script export.
    /// engine/episodes/{Id}/script.pdf. Rendered alongside the .md.</summary>
    public string? ScriptPdfPath { get; set; }

    /// <summary>Relative path under data root to the single combined WAV file.
    /// engine/episodes/{Id}/episode.wav. Concatenation of all per-beat WAVs,
    /// written when narration finishes.</summary>
    public string? CombinedAudioPath { get; set; }

    /// <summary>Last beat the user was on when they walked away. Null = never played.</summary>
    public int? LastPlayedBeatIndex { get; set; }

    /// <summary>Seconds into the last beat. Combined with LastPlayedBeatIndex this
    /// is the resume point on /listen.</summary>
    public double? LastPlayedSec { get; set; }

    /// <summary>If this episode was spawned via "Continue this story" from another
    /// episode, points back at it. Lets us walk threads. Null for stand-alone.</summary>
    public Guid? ParentEpisodeId { get; set; }

    /// <summary>If this episode is a recording of a chapter from a book, points
    /// at the Book.Id. Used by the /recordings hierarchy view to group
    /// chapter-recordings under their parent book.</summary>
    public Guid? BookId { get; set; }

    /// <summary>If this episode is a recording of a chapter from a book, points
    /// at the Chapter's id (string-form GUID without dashes per Chapter model).
    /// Null = stand-alone bedtime episode.</summary>
    public string? ChapterId { get; set; }

    public List<EpisodeBeat>        Beats      { get; set; } = new();
    public List<EpisodeCorrection>  Corrections { get; set; } = new();
    public EpisodeSurvey? Survey { get; set; }
}

/// <summary>
/// One paragraph of the episode. Granular storage so corrections can target specific
/// beats and so audio can stream per-beat as MP3 files land on disk.
/// </summary>
public class EpisodeBeat
{
    public int Id { get; set; }

    public Guid EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    /// <summary>Stable identifier of this beat within its episode. Issued at
    /// creation time and never changes — audio file paths and URLs embed it.
    /// Use <see cref="SortKey"/> for display ordering, not this.</summary>
    public int Index { get; set; }

    /// <summary>Fractional-index ordering value. UI sorts by this ASC. A split
    /// inserts the new beat at (prev.SortKey + next.SortKey) / 2 — no O(N)
    /// renumbering required. Initial values are Index * 100 leaving plenty of
    /// room between siblings.</summary>
    public double SortKey { get; set; }

    public string Text { get; set; } = "";

    /// <summary>Relative path under engine/audio/episodes/, set once TTS completes.</summary>
    public string? AudioPath { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NarratedAt { get; set; }

    /// <summary>Actual narration duration in seconds, populated once the WAV exists.
    /// Null until TTS completes — the /listen player falls back to a text-length
    /// estimate (~15 chars/sec) when null.</summary>
    public double? DurationSec { get; set; }

    /// <summary>True if a user correction rewrote this beat.</summary>
    public bool WasCorrected { get; set; }

    /// <summary>SHA-256 hex of the beat's text at record time. The writer
    /// rehashes ChapterBeat.Text on chapter save and compares; mismatch
    /// triggers Stale=true and AudioPath invalidation so the audio can't
    /// silently drift from the canonical prose.</summary>
    public string? TextHash { get; set; }

    /// <summary>When this beat was materialized from a source ChapterBeat,
    /// stores that ChapterBeat.Id (the JSON-side string Guid). 1:1 link so
    /// the desync check is an indexed point lookup, not a positional guess.</summary>
    public string? SourceBeatGuid { get; set; }

    /// <summary>True when the prose has drifted past the recording (text edit
    /// invalidated the audio without re-record). The /listen player and
    /// recording panel surface this; combined-audio export skips stale beats.</summary>
    public bool Stale { get; set; }

    /// <summary>The ElevenLabs <c>request-id</c> from the most recent successful
    /// synthesis of this beat. Persisted so a single-beat re-record (Step 1)
    /// can seed its stitching window from the neighbours' ids — without this,
    /// a lone re-record sounds like a different reader from the rest.</summary>
    public string? LastRequestId { get; set; }

    // ── Narrative metadata — "what is this beat accomplishing" ───────────
    // Populated either by the LLM at generation time, by the writer manually,
    // or copied from the source ChapterBeat at chapter-recording materialization.

    /// <summary>Optional human-readable label for the beat. e.g. "The threshold".</summary>
    public string? BeatTitle { get; set; }

    /// <summary>One-line of what this beat is doing — e.g. "Kyle reads the room
    /// and decides he's not leaving." Feeds into LLM regeneration prompts.</summary>
    public string? Synopsis { get; set; }

    /// <summary>Story-structure role of this beat: "inciting-incident",
    /// "rising-action", "climax", "denouement", "transition", "scene-break".
    /// Free-form; conventions live in the writer's docs.</summary>
    public string? StructureRole { get; set; }

    /// <summary>Plot-structure act number (1-5). Zero = unassigned.</summary>
    public int Act { get; set; }

    /// <summary>"scene" | "summary" | "transition" | "interstitial".</summary>
    public string SceneType { get; set; } = "scene";

    /// <summary>Emotional charge tag: "tense", "wry", "tender", "violent",
    /// "quiet". Optional; helps TTS direction.</summary>
    public string? EmotionalTone { get; set; }

    /// <summary>Pace hint: "clipped", "languorous", "staccato", "flowing".
    /// Optional; helps narration cadence.</summary>
    public string? PaceHint { get; set; }
}

/// <summary>
/// A note the user added — either mid-listen ("Kyle's out of ammo here") or via the
/// survey ("the Carrion plate joke landed twice in a row, ease up"). Corrections feed
/// back into future episodes as canon hints.
/// </summary>
public class EpisodeCorrection
{
    public int Id { get; set; }

    public Guid EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    /// <summary>Which beat the correction targets. Null = whole-episode note.</summary>
    public int? BeatIndex { get; set; }

    public string Note { get; set; } = "";

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    /// <summary>True once the correction has been folded into the canon-hint pool.</summary>
    public bool Applied { get; set; }
}

/// <summary>
/// End-of-episode rating. Filled in at /inbox if the user fell asleep, otherwise
/// inline at the end of /listen. Feeds seed-weight adjustments.
/// </summary>
public class EpisodeSurvey
{
    public int Id { get; set; }

    public Guid EpisodeId { get; set; }
    public Episode? Episode { get; set; }

    /// <summary>Overall 1-5.</summary>
    public int Score { get; set; }

    /// <summary>1-5 pacing.</summary>
    public int? Pacing { get; set; }

    /// <summary>1-5 Kyle's voice/feel.</summary>
    public int? Voice { get; set; }

    public string? Notes { get; set; }

    public bool WasInbox { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
