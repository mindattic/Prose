namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// A named, ordered composition of <see cref="Beat"/>s — plus optionally a
/// tree of sub-strands. Replaces Book / Chapter / Episode all at once.
/// 800 beats in a row? One Strand. A book of 12 chapters of 60 beats each?
/// One root Strand with 12 child Strands. A standalone bedtime story? One
/// Strand. The "100 Stories" corpus? One root Strand with 100 child Strands.
///
/// The <see cref="Kind"/> field is a free-form label for UI display — "book",
/// "chapter", "episode", "scene", "saga", "anthology", or whatever fits.
/// Nothing in the data layer treats kinds differently; they're a category
/// hint to the user, not a constraint.
/// </summary>
public class Strand
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    /// <summary>URL-safe slug, used as <c>/strand/{slug}</c> route key and as
    /// the on-disk directory name under <c>engine/strands/{slug}/</c>.</summary>
    public string Slug { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>Short synopsis — what this strand is about. Surfaces in
    /// listings and feeds LLM context.</summary>
    public string? Synopsis { get; set; }

    /// <summary>Free-form category label. Suggested values: "book", "chapter",
    /// "episode", "scene", "saga", "anthology", "vignette". UI groups by
    /// this. Storage doesn't constrain it.</summary>
    public string Kind { get; set; } = "strand";

    /// <summary>"draft" | "generating" | "narrating" | "ready" | "failed" |
    /// "stopped". Mirrors the old Episode.Status semantics.</summary>
    public string Status { get; set; } = "draft";

    /// <summary>Optional parent strand. A book strand has chapter-strand
    /// children; a saga strand has book-strand children; a standalone
    /// vignette has none. Walking the tree in SortKey order gives the
    /// reading sequence.</summary>
    public Guid? ParentStrandId { get; set; }
    public Strand? ParentStrand { get; set; }

    /// <summary>Fractional sort key within the parent. Initial values are
    /// 100, 200, 300… so inserts between siblings find midpoints without
    /// renumbering.</summary>
    public double SortKey { get; set; }

    // ── Audio / artefact paths ───────────────────────────────────────────

    /// <summary>Concatenated audio for this strand's beats (and, if it's a
    /// container, the recursive concat of its children's audio). Written
    /// after narration completes.</summary>
    public string? CombinedAudioPath { get; set; }

    /// <summary>Markdown export of the strand's prose. Used for offline
    /// reading and PDF generation.</summary>
    public string? ScriptMarkdownPath { get; set; }

    /// <summary>PDF export of the strand's prose.</summary>
    public string? ScriptPdfPath { get; set; }

    /// <summary>Default narrator voice for this strand. Beats with their own
    /// <c>VoiceId</c> override it; otherwise the strand's voice is used.</summary>
    public string? VoiceId { get; set; }

    // ── Voice profile snapshot (locked at first narration) ───────────────
    // Captured once, the first time any beat in this strand is narrated, from
    // the then-current default voice profile. Every later (re)record reuses
    // THESE values instead of the live global settings, so changing the
    // default profile/model later can't make a freshly-recorded beat drift
    // out of character with the beats already laid down. Null = not yet
    // narrated (the snapshot is taken on first synthesis).

    /// <summary>ElevenLabs model id locked for this strand (e.g. eleven_v3).</summary>
    public string? VoiceModel { get; set; }

    /// <summary>Locked baseline stability for this strand's narration.</summary>
    public double? VoiceStability { get; set; }

    /// <summary>Locked baseline similarity_boost for this strand's narration.</summary>
    public double? VoiceSimilarity { get; set; }

    /// <summary>Locked baseline style for this strand's narration.</summary>
    public double? VoiceStyle { get; set; }

    /// <summary>Deterministic ElevenLabs generation seed for this strand.
    /// Every beat is rendered with the SAME seed so the model anchors to one
    /// voice realization across the whole strand and single-beat re-records
    /// reproduce the surrounding delivery. Derived from <see cref="Id"/> on
    /// first narration, then frozen here. Distinct from <see cref="Seed"/>,
    /// which is the LLM generator's text prompt — unrelated.</summary>
    public int? VoiceSeed { get; set; }

    // ── Generation / cost / resume state ─────────────────────────────────

    /// <summary>For LLM-generated strands, the one-line seed that fed the
    /// generator.</summary>
    public string? Seed { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GenerationCompletedAt { get; set; }
    public DateTime? AudioCompletedAt { get; set; }

    /// <summary>Sum of characters sent to TTS across this strand's beats.</summary>
    public int CharsNarrated { get; set; }

    /// <summary>Number of beats that have been successfully narrated in the
    /// current run. The narration loop bumps this each iteration so the
    /// polling UI can show "narrated N of M" without re-loading the whole
    /// beats collection on every tick. Reset to 0 at the start of each
    /// NarrateAsync call (so the count reflects "this run" not "lifetime").</summary>
    public int NarratedBeatCount { get; set; }

    /// <summary>Total beats considered for narration this run (the snapshot
    /// of the strand's beat count when NarrateAsync started). Pairs with
    /// <see cref="NarratedBeatCount"/> to give the UI a stable denominator
    /// even if beats are added/removed mid-narration.</summary>
    public int TotalBeatsToNarrate { get; set; }

    /// <summary>Where the listener was when they walked away — a specific
    /// beat in this strand. Resume on /strand/{id}.</summary>
    public Guid? LastPlayedBeatId { get; set; }

    /// <summary>Seconds into <see cref="LastPlayedBeatId"/>.</summary>
    public double? LastPlayedSec { get; set; }

    /// <summary>Notes about why a run failed, if it did.</summary>
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ───────────────────────────────────────────────────────

    public List<Strand> Children { get; set; } = new();
    public List<StrandBeat> StrandBeats { get; set; } = new();

    /// <summary>Publish-run history (1:M). Each Publish appends one row; the
    /// latest completed run's file is what <see cref="CombinedAudioPath"/>
    /// points at.</summary>
    public List<StrandPublication> Publications { get; set; } = new();

    /// <summary>Persona reader-reviews (1:M). Each review run appends rows; the
    /// aggregate lives in <see cref="StrandReviewSummary"/>.</summary>
    public List<StrandReview> Reviews { get; set; } = new();
}
