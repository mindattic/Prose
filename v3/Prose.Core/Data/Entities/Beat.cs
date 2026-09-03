using System.ComponentModel.DataAnnotations;

namespace Prose.Core.Data.Entities;

/// <summary>
/// One paragraph of prose, plus its one canonical audio rendering. The atom
/// of the storytelling engine — every word the user writes ends up in a Beat
/// row, every audio file points back at one. Replaces the old
/// <c>ChapterBeat</c> + <c>EpisodeBeat</c> split: prose and audio used to
/// live in two tables linked by a string-guid pointer with bidirectional
/// sync; now they're one row.
///
/// Beats belong to one or more <see cref="Node"/>s through the
/// <see cref="BeatNode"/> junction. A Beat can appear in many nodes
/// (the same paragraph reused across an anthology, a chapter, and a
/// greatest-hits playlist) — the prose lives in one place, the audio file
/// lives in one place, edits propagate naturally.
/// </summary>
public class Beat
{
    /// <summary>UUIDv7 — sortable by creation time, globally unique.</summary>
    public Guid Id { get; set; }

    /// <summary>Small human-readable counter. Globally unique across all
    /// beats. Stable across reordering / inserts / deletions — unlike the
    /// positional "BEAT 042" badge in the writer UI, which shifts whenever
    /// the node is restructured. Users and CLI assistants reference beats
    /// as "Beat #134" using this column.</summary>
    public int Number { get; set; }

    /// <summary>
    /// Legacy/abandoned. An early-development attempt at a human-readable beat
    /// identifier (7 of 11,622+ beats have one, all from before <see cref="Number"/>
    /// existed) — never wired to any lookup path or generation hook. <see cref="Number"/>
    /// is the actual stable handle CLI/writer/MCP surfaces to humans and each other;
    /// nothing reads this column. Kept (not dropped) because <c>Beats</c> is
    /// system-versioned and a column drop requires disabling/re-enabling temporal
    /// versioning for no benefit — do not build new features on this field.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>The paragraph. Authoritative; nothing else holds a copy.</summary>
    public string Text { get; set; } = "";

    /// <summary>SHA-256 hex of <see cref="Text"/>, kept in lockstep with it.
    ///
    /// This is NOT what drives narration staleness — <see cref="Stale"/> is an
    /// explicit flag, set by every edit path and cleared on render. The real
    /// consumer is review-score invalidation: <c>NodeReviewService</c> compares
    /// this against the <c>NodeReviewBeatScore.BeatTextHash</c> recorded when the
    /// beat was last scored, to decide which beats changed and must be re-reviewed.
    ///
    /// So a hash left stale after a prose edit makes an edited beat look
    /// UNCHANGED: it silently keeps a score that was awarded to different words.
    /// That is why <c>ProseDbContext.StampBeatTextHash()</c> recomputes it
    /// on every save rather than trusting call sites to remember.</summary>
    public string? TextHash { get; set; }

    /// <summary>Canonical hash for <see cref="TextHash"/> — SHA-256 of the
    /// UTF-8 bytes of the trimmed text, lowercase hex. Must stay byte-identical
    /// to <c>NodeWorkbenchService.ComputeTextHash</c> and its two siblings, since
    /// hashes written by one are compared against hashes written by another.</summary>
    public static string ComputeHash(string? text)
    {
        var normalized = (text ?? "").Trim();
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }

    // ── Narrative metadata ───────────────────────────────────────────────

    /// <summary>Optional short label for the beat. e.g. "The threshold".
    /// When <see cref="IsChapterStart"/> is true, this doubles as the chapter
    /// heading rendered above the beat in the writer/listener.</summary>
    public string? Title { get; set; }

    /// <summary>True when this beat begins a new chapter. The UI renders a
    /// divider above the beat with <see cref="Title"/> as the heading.
    /// Replaces the old "chapters are child nodes" model: one flat node
    /// per work, chapters are just beats with this flag. Orthogonal to
    /// <see cref="Kind"/> — a quote/epigraph can start a chapter too.</summary>
    public bool IsChapterStart { get; set; }

    /// <summary>What kind of beat this is. One of: "prose" (default),
    /// "book-title" (front-matter title page; Text=title, Title=author),
    /// "dedication" (centered italic line; Text=dedication),
    /// "quote" (blockquote; Text=quote, Title=attribution).
    /// Kept as a free-form string so new kinds can be added without a
    /// schema migration. IsChapterStart is orthogonal — set both for an
    /// epigraph that opens a chapter.</summary>
    public string Kind { get; set; } = "prose";

    /// <summary>One-line description of what this beat is doing — "Kyle reads the room
    /// and decides he's not leaving." Feeds into LLM regeneration prompts
    /// and ElevenLabs tone direction.</summary>
    public string? Description { get; set; }

    /// <summary><see cref="TextHash"/> value at the moment <see cref="Description"/> was last
    /// written FROM, or confirmed AGAINST, this beat's actual prose. Exactly the
    /// <see cref="EventSummaryHash"/> pattern, and never a Stale/audio signal.
    ///
    /// <para>Why this exists (Story Ledger Phase 1): <see cref="Description"/> was the only
    /// per-beat summary the read tools exposed, and it had no binding to the prose at all — so it
    /// could drift from <see cref="Text"/> permanently and silently. A reading agent that fell
    /// back to the Description spine for a book too large to hold verbatim reported fabricated
    /// detail as established fact, and nothing in the engine could tell it the spine was stale.</para>
    ///
    /// <para><b>Three states, and the middle one is the point:</b>
    /// <list type="bullet">
    /// <item>null — never verified against prose. Either the beat was planned before it was
    /// written (<c>NodeOutlineService</c> creating beats from a spine), or Description was set
    /// from an OUTLINE line rather than from the prose (<c>OutlineAdherenceService</c>,
    /// <c>NarrativeForkService</c>, <c>ImportNodeCli</c>) — those sites deliberately CLEAR this
    /// so an intent line can never masquerade as a verified one.</item>
    /// <item>set and equal to <see cref="TextHash"/> — Description describes the prose that is
    /// actually there. Trustworthy.</item>
    /// <item>set and DIFFERENT from <see cref="TextHash"/> — the prose changed after the
    /// Description was written. Provably stale, with zero LLM or embedding cost.
    /// <c>prose --description-drift</c> files these as findings; nothing auto-rewrites them
    /// (docs/LOGIC.md §4 — audits never write).</item>
    /// </list></para>
    ///
    /// <para>Stamped only by the sites that derive Description FROM prose
    /// (<c>MeaningBackfillService</c>, <c>BackfillBeatMetaCli</c>) or confirm it against prose
    /// (<c>NodeWorkbenchService.UpdateBeatMetadataAsync</c>), and only when the beat actually has
    /// text — stamping a textless planned beat would flag every beat as "drifted" the moment its
    /// prose was first written, which is the normal authoring flow, not a defect.</para></summary>
    public string? DescriptionHash { get; set; }

    /// <summary>
    /// How much a reader may trust one of this beat's summary fields, as an explicit word:
    /// <c>"current"</c> (<paramref name="stampedTextHash"/> matches
    /// <paramref name="currentTextHash"/> — the summary describes the prose that is actually
    /// there), <c>"stale"</c> (the prose changed after the summary was written),
    /// <c>"unverified"</c> (a summary exists but was never bound to prose — an outline/intent
    /// line, or a row predating hash stamping), or null when there is no summary to qualify.
    ///
    /// <para>Lives here rather than in a payload builder so the read tools, the CLI reports and
    /// any future reader all answer this question identically — the whole failure this guards
    /// against is one consumer treating a summary as trustworthy when another would not.
    /// A null <paramref name="currentTextHash"/> yields <c>"unverified"</c>, never
    /// <c>"stale"</c>: with no fingerprint for the prose there is nothing to compare against,
    /// and asserting drift there would be a claim about text we cannot see.</para>
    /// </summary>
    public static string? SummaryTrustState(string? summary, string? stampedTextHash, string? currentTextHash)
    {
        if (string.IsNullOrWhiteSpace(summary)) return null;
        if (string.IsNullOrWhiteSpace(stampedTextHash)) return "unverified";
        if (string.IsNullOrWhiteSpace(currentTextHash)) return "unverified";
        return string.Equals(stampedTextHash, currentTextHash, StringComparison.OrdinalIgnoreCase)
            ? "current" : "stale";
    }

    /// <summary>Trust state of <see cref="Description"/> — see <see cref="SummaryTrustState"/>.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? DescriptionState => SummaryTrustState(Description, DescriptionHash, TextHash);

    /// <summary>Trust state of <see cref="EventSummary"/> — see <see cref="SummaryTrustState"/>.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? EventSummaryState => SummaryTrustState(EventSummary, EventSummaryHash, TextHash);

    /// <summary>Terse, present-tense, name-anchored plot-EVENT line — "what happened" in
    /// this beat, not why it matters. Distinct register from <see cref="Description"/>
    /// (authorial intent). Written by BeatEventSummaryService; null = not yet generated.
    /// A beat with no new plot event honestly says so (e.g. "No new event — transitional
    /// beat") rather than inventing significance — repeated/near-duplicate lines are a
    /// legitimate pacing diagnostic, not a prompt bug to eliminate. Deliberately kept out
    /// of the node bible / DCM prose-generation context path — this is a human-readable
    /// QA artifact, not a story-generation input.</summary>
    public string? EventSummary { get; set; }

    /// <summary>TextHash value at the time EventSummary was last generated (or last
    /// manually confirmed via update_beat_metadata's eventSummary override). Compared
    /// against the beat's CURRENT TextHash to hash-gate regeneration: equal = skip (free),
    /// different = beat's prose changed since — regenerate. Same shape/maxlength as
    /// TextHash; NOT a general Version/Stale signal — touching this field must never
    /// mark the beat Stale or invalidate audio (see BeatEventSummaryService).</summary>
    public string? EventSummaryHash { get; set; }

    /// <summary>Where this beat's scene takes place, as free text (e.g. "Doc Stash's clinic,
    /// The Shelf"). Written by the scene-location slice of BeatExtractionService's consolidated
    /// post-write call, or by the `prose --extract-beat-locations` backfill; may also be
    /// authored directly. ProseWriterRouter uses the nearest prior beat's value as
    /// BeatContext.Location (scene-continuity assumption) when the caller doesn't set one,
    /// before falling back to the book's DefaultLocation. Null = not yet extracted.</summary>
    public string? PlaceName { get; set; }

    /// <summary>The place Entity this beat's <see cref="PlaceName"/> resolved to (matched
    /// against Places.Name / PlaceAliases.Value in the beat's universe). Soft reference — no FK
    /// constraint, same as BeatEntityPresence. Null = unresolved or not a canon place.</summary>
    public Guid? PlaceEntityId { get; set; }

    /// <summary>TextHash value when PlaceName was last extracted — hash gate, exactly the
    /// <see cref="EventSummaryHash"/> pattern: equal to current TextHash = skip (free);
    /// different = prose changed, re-extract. Never marks the beat Stale.</summary>
    public string? PlaceExtractedFromHash { get; set; }

    /// <summary>What is happening beneath the surface of this beat —
    /// foreshadowing, unspoken motivations, dramatic irony, hidden agendas.
    /// Visible to the prose writer LLM but never printed; it informs the
    /// writing without appearing in the final text.</summary>
    public string? Subtext { get; set; }

    /// <summary>Story-structure role: "inciting-incident" / "rising-action"
    /// / "climax" / "denouement" / "transition" / "scene-break". Free-form.</summary>
    public string? StructureRole { get; set; }

    /// <summary>Plot-structure act number (1-5). Zero = unassigned.</summary>
    public int Act { get; set; }

    /// <summary>"scene" | "summary" | "transition" | "interstitial".</summary>
    public string SceneType { get; set; } = "scene";

    /// <summary>Emotional charge: "tense", "wry", "tender", "violent", "quiet".</summary>
    public string? EmotionalTone { get; set; }

    /// <summary>Pace hint: "clipped", "languorous", "staccato", "flowing".</summary>
    public string? PaceHint { get; set; }

    // ── Audio ────────────────────────────────────────────────────────────

    /// <summary>ElevenLabs voice id of the canonical rendering. Null = use
    /// the node's default voice when narrating.</summary>
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

    /// <summary>True when a canon entity mentioned in this beat was updated
    /// after the beat was written. Signals the author to review whether the
    /// prose still matches entity canon. Cleared manually after review.
    /// Separate from <see cref="Stale"/> which is audio-only.</summary>
    public bool EntityStale { get; set; }

    /// <summary>True if the beat has been manually rewritten since materialisation.</summary>
    public bool WasCorrected { get; set; }

    /// <summary>Latest-run reader score for this beat as a percentage (0-100), derived
    /// from the most recent segment-study per-beat micro-scores (mean of 1-5 → %).
    /// Null = not yet scored. Surfaced in the writer so the author sees where to
    /// concentrate effort; clicking it opens that beat's micro-reviews.</summary>
    public double? Score { get; set; }

    /// <summary>When <see cref="Score"/> was last computed (the run it reflects).</summary>
    public DateTime? ScoredAt { get; set; }

    /// <summary>Most-recent emotional depth score (0–4) from the last EmotionalDepthService
    /// examination. Written by Pass 2 (per-beat curve); never overwrites reader-panel
    /// <see cref="Score"/>. Null = not yet examined or effort=Draft (Pass 1 only).</summary>
    public double? EmotionalScore { get; set; }

    // ── Trailing gap (silence after this beat, before the next) ─────────
    // Each Beat owns the gap that follows it. The last beat in a node
    // ignores this field. Null = "use the computed default" from
    // <see cref="NodeWorkbenchService.ComputeTrailingSilenceMs"/>
    // (SceneType + terminator punctuation → 200/400/1000/1800ms). A value
    // (including 0) is an explicit override the user set in the UI.
    // Replaces the separate Gap table — gap is a property of the upper beat.

    /// <summary>Explicit silence in ms after this beat, before the next one.
    /// Null = use the auto-computed default. 0 = no silence (explicit).</summary>
    public int? GapAfterMs { get; set; }

    /// <summary>Optional recorded clip (rain, ambient, sigh) to play instead
    /// of digital silence in the gap after this beat. Path relative to the
    /// nodes audio root. Null = digital silence.</summary>
    public string? GapAfterAudioPath { get; set; }

    // ── Provenance ───────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last-modified timestamp + optimistic-concurrency token. Every
    /// write bumps this. EF's UPDATE includes <c>WHERE UpdatedAt = @loadedAt</c>
    /// so a same-instant race between two clients fails one of them with
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>.
    /// The workbench also exposes an <c>expectedUpdatedAt</c> parameter so
    /// the UI can detect the longer "user opened the editor, walked away,
    /// another tab edited it" race window.</summary>
    /// <summary>Monotonic edit counter. Incremented by one each time the beat's
    /// prose is saved via <c>UpdateBeatTextAsync</c>. Zero = never edited after
    /// creation. Surfaces in the writer's version cycler as a stable label.</summary>
    public int Version { get; set; }

    [ConcurrencyCheck]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Reverse navigation — which nodes include this beat. Set
    /// by EF Core through the BeatNodes junction.</summary>
    public List<BeatNode> BeatNodes { get; set; } = new();
}
