namespace Prose.Core.Data.Entities;

/// <summary>
/// Abstract base of the book tree (table-per-hierarchy on the Nodes table,
/// discriminated by the <c>NodeType</c> column). The hierarchy unifies
/// Series, Book, and Chapter under one polymorphic root:
///
///   SeriesNode  — top-level grouping (e.g. "Bushido Coda", a saga/anthology).
///     BookNode  — a single narrative arc; a book. May hold beats directly when it
///                 is a standalone/leaf book with no chapter children.
///       ChapterNode — organizational unit inside a book; holds beats.
///
/// Beats attach via <see cref="BeatNode"/> to ChapterNodes and to leaf
/// BookNodes; a SeriesNode never holds beats directly. Walking the tree in
/// SortKey order gives the reading sequence.
///
/// The <see cref="Kind"/> field remains a free-form display label ("book",
/// "novella", "episode", "scene", …). The CLR type / NodeType discriminator is
/// the structural truth; Kind is a category hint to the user.
/// </summary>
public abstract class Node
{
    /// <summary>UUIDv7.</summary>
    public Guid Id { get; set; }

    /// <summary>The universe this node belongs to (1:M). A story lives in exactly one universe;
    /// stamped on insert from the current universe, backfilled to GLMZ for pre-existing rows
    /// (SS-LAW-15).</summary>
    public Guid UniverseId { get; set; }

    /// <summary>URL-safe slug, used as <c>/node/{slug}</c> route key and as
    /// the on-disk directory name under <c>engine/strands/{slug}/</c>.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Short author-assigned reference code for quick CLI/prose lookup
    /// (e.g. "ATTE", "VATD", "DWIACE"). Unique across non-null values; not every
    /// node needs one. Use <c>prose --timeline --code ATTE</c> etc.</summary>
    public string? NodeCode { get; set; }

    public string Title { get; set; } = "";

    /// <summary>Back-of-book description — what this node is about. Surfaces in
    /// listings and feeds LLM context.</summary>
    public string? Description { get; set; }

    /// <summary>Back-of-book / KDP blurb — longer marketing summary distinct
    /// from the internal Synopsis. Optional.</summary>
    public string? Summary { get; set; }

/// <summary>Free-form category label. Suggested values: "book", "chapter",
    /// "episode", "scene", "saga", "anthology", "vignette". UI groups by
    /// this. Storage doesn't constrain it; the structural truth is the CLR
    /// type (NodeType discriminator). Defaulted per subclass.</summary>
    public string Kind { get; set; } = "book";

    /// <summary>"draft" | "generating" | "narrating" | "ready" | "failed" |
    /// "stopped". Mirrors the old Episode.Status semantics.</summary>
    public string Status { get; set; } = "draft";

    /// <summary>Latest-run overall reader score as a percentage (0-100): the mean
    /// of the most-recent focus-group reviews (one per persona). Null = not yet
    /// reviewed. Shown on the node; clicking it opens the full reviews.</summary>
    public double? Score { get; set; }

    /// <summary>When <see cref="Score"/> was last computed (the run it reflects).</summary>
    public DateTime? ScoredAt { get; set; }

    /// <summary>Author-only canon flag. Set true ONLY by hand once this node's
    /// voice and the characters' actions are true to what those characters are
    /// capable of — the gold standard. The rest are hit-or-miss. The voice-harvest
    /// prefers canon nodes as the source of truth for the voice.</summary>
    public bool IsCanon { get; set; }

    /// <summary>How this book's characters relate to authorial invention. Gates whether
    /// personality/goal-drift checks apply (<c>BookHealthService.SacredFlawAsync</c> /
    /// <c>NarrativeScienceService.AnalyzeSacredFlawAsync</c>):
    /// "original"   (default) — author-invented psychology; motivations are designed from
    ///              scratch and must stay internally consistent. Full drift-prevention applies.
    /// "retelling"  — a close/1:1 adaptation of a pre-existing fixed narrative (myth, scripture,
    ///              literary classic — e.g. Paradise Lost, the Gospels). Motivations are already
    ///              fixed by the source text; the sacred-flaw "ground this character's flaw"
    ///              nudge does not apply — fidelity to the source is the standard, not invention.
    /// "historical" — nonfiction; real people and events that already happened. No invented
    ///              psychology exists to enforce or drift from.
    /// Set via <c>prose --set-narrative-mode --slug &lt;slug&gt; --mode &lt;mode&gt;</c>.</summary>
    public string NarrativeMode { get; set; } = "original";

    /// <summary>When the author marked this node canon (null = never).</summary>
    public DateTime? CanonAt { get; set; }


    /// <summary>Optional parent node. A book node has chapter-node
    /// children; a saga node has book-node children; a standalone
    /// vignette has none. Walking the tree in SortKey order gives the
    /// reading sequence.</summary>
    public Guid? ParentNodeId { get; set; }
    public Node? ParentNode { get; set; }

    /// <summary>
    /// The node this one is a direct continuation of. Null = this is the
    /// first book in its series (or a standalone) — gateway commandments apply.
    /// Non-null = this is a sequel — sequel commandments apply in audits and
    /// beat-writing context injection.
    /// </summary>
    public Guid? PreviousNodeId { get; set; }
    public Node? PreviousNode { get; set; }

    /// <summary>Fractional sort key within the parent. Initial values are
    /// 100, 200, 300… so inserts between siblings find midpoints without
    /// renumbering.</summary>
    public double SortKey { get; set; }

    // ── Audio / artefact paths ───────────────────────────────────────────

    /// <summary>Concatenated audio for this node's beats (and, if it's a
    /// container, the recursive concat of its children's audio). Written
    /// after narration completes.</summary>
    public string? CombinedAudioPath { get; set; }

    /// <summary>Default narrator voice for this node. Beats with their own
    /// <c>VoiceId</c> override it; otherwise the node's voice is used.</summary>
    public string? VoiceId { get; set; }

    // ── Voice profile snapshot (locked at first narration) ───────────────
    // Captured once, the first time any beat in this node is narrated, from
    // the then-current default voice profile. Every later (re)record reuses
    // THESE values instead of the live global settings, so changing the
    // default profile/model later can't make a freshly-recorded beat drift
    // out of character with the beats already laid down. Null = not yet
    // narrated (the snapshot is taken on first synthesis).

    /// <summary>ElevenLabs model id locked for this node (e.g. eleven_v3).</summary>
    public string? VoiceModel { get; set; }

    /// <summary>Locked baseline stability for this node's narration.</summary>
    public double? VoiceStability { get; set; }

    /// <summary>Locked baseline similarity_boost for this node's narration.</summary>
    public double? VoiceSimilarity { get; set; }

    /// <summary>Locked baseline style for this node's narration.</summary>
    public double? VoiceStyle { get; set; }

    /// <summary>Deterministic ElevenLabs generation seed for this node.
    /// Every beat is rendered with the SAME seed so the model anchors to one
    /// voice realization across the whole node and single-beat re-records
    /// reproduce the surrounding delivery. Derived from <see cref="Id"/> on
    /// first narration, then frozen here. Distinct from <see cref="Seed"/>,
    /// which is the LLM generator's text prompt — unrelated.</summary>
    public int? VoiceSeed { get; set; }

    /// <summary>Which TTS backend narrates this node.
    /// NULL/"elevenlabs" → ElevenLabs (default, all legacy rows).
    /// "kokoro"          → Kokoro-82M (PythonTtsService).
    /// "piper"           → Piper (PiperTtsService).
    ///
    /// Column re-use for non-ElevenLabs engines (no extra columns needed):
    ///   VoiceId         → engine voice id (e.g. "af_sky" for kokoro, "en_US-ryan-high" for piper)
    ///   VoiceStyle      → piper: noise_scale (0.0–1.0); kokoro: speed (0.5–2.0)
    ///   VoiceStability  → piper: length_scale / speed (0.5–2.0); kokoro: unused
    ///   VoiceSimilarity → unused for local engines
    ///   VoiceModel      → unused for local engines
    /// </summary>
    public string? TtsEngine { get; set; }

    // ── KDP publication tracking ──────────────────────────────────────────

    /// <summary>KDP publication lifecycle status.
    /// "Published"      = live on KDP, beats unchanged since last publish.
    /// "Outdated"       = was published; prose has been edited since — needs republish.
    /// "WorkInProgress" = not yet on KDP; actively being written or not ready.
    /// Null             = not tracked (legacy / internal-only nodes).
    /// </summary>
    public string? PublicationStatus { get; set; }

    /// <summary>When this node was last successfully published to KDP. Null = never published.
    /// A node needs republishing when <c>MAX(beats.UpdatedAt) > KdpPublishedAt</c>.</summary>
    public DateTime? KdpPublishedAt { get; set; }

    /// <summary>KDP print-page count as measured in the final exported .docx (check in Word:
    /// File → Info → Properties → Pages). Used to select the correct inside-margin (gutter)
    /// per KDP's page-count table on the next export. Null = unknown; falls back to the
    /// maximum-safe gutter (0.875").</summary>
    public int? KdpPageCount { get; set; }

    /// <summary>Estimated Kindle e-reader page count (words / 250 -- the commonly-cited
    /// convention for Amazon's Kindle page display; distinct from <see cref="KdpPageCount"/>,
    /// which is the 6"x9" print-trim page count). Recomputed on every export.</summary>
    public int? KindlePages { get; set; }

    /// <summary>Estimated reading time in minutes (words / 200wpm -- the commonly-cited average
    /// adult silent-reading speed). Recomputed on every export.</summary>
    public int? ReadingMinutes { get; set; }

    /// <summary>Direct Amazon product URL for the published book. Null = not published on Amazon
    /// (or the URL has not been recorded yet). The authoritative marker of which stories are live:
    /// a non-null PublishUrl means the book exists on Amazon at that address.</summary>
    public string? PublishUrl { get; set; }

    /// <summary>The book's Amazon ASIN (e.g. "B0H8KK2GJ9"). Previously derived on the fly via
    /// regex from PublishUrl each time — stored directly instead (canon is the DB, not a
    /// recomputed value) since it's also used as an exact, unambiguous search key against KDP's
    /// own bookshelf search box (confirmed live: typing an ASIN into "Search by title" resolves
    /// to that exact book, unlike title text which commonly diverges from KDP's displayed title
    /// by a subtitle/series suffix). Null = not yet known (not published, or recorded before
    /// this column existed — falls back to parsing PublishUrl).</summary>
    public string? Asin { get; set; }

    /// <summary>KDP's internal "titleId" for this book's edit-content session (e.g.
    /// "A412I146N52A1", found in the bookshelf's data-link-parameters attribute and in the
    /// title-setup URL). Lets automation jump straight to the book's Edit eBook Content page —
    /// https://kdp.amazon.com/en_US/title-setup/kindle/{KdpTitleId}/content — with zero title
    /// or ASIN search needed at all. Recorded automatically the first time find_and_open_book
    /// locates this book. Null = not yet discovered.</summary>
    public string? KdpTitleId { get; set; }

    // ── Generation / cost / resume state ─────────────────────────────────

    /// <summary>For LLM-generated nodes, the one-line seed that fed the
    /// generator.</summary>
    public string? Seed { get; set; }

    /// <summary>Default scene location for this node (e.g. "Zone 4 civic district", "The Spine, Zone 6").
    /// ProseWriterRouter uses this to auto-populate BeatContext.Location when the caller doesn't set it,
    /// enabling SceneContextBuilder (ambient sensory grounding + New Weird anomaly layer) to fire.</summary>
    public string? DefaultLocation { get; set; }

    // ── Node Bible ──────────────────────────────────────────────────────
    // A dry, structural plot document generated before prose begins. Defines
    // logline / premise / register / characters / beat spine / seeds+payoffs.
    // The prose engine reads this as BookOutlineContext on every beat so the
    // full arc is always in view. Generated by NodeOutlineService.

    /// <summary>Full node bible markdown. Null = not yet generated.</summary>
    public string? NodeOutline { get; set; }

    /// <summary>When the node bible was last generated or updated.</summary>
    public DateTime? NodeOutlineGeneratedAt { get; set; }

    // ── User Stories ──────────────────────────────────────────────────────
    // Acceptance criteria for this node: what scenes, beats, character arcs,
    // and voice moments must be present for it to be "done". Written before
    // prose begins. Updated as goals evolve. System-versioned automatically.

    /// <summary>Acceptance-criteria markdown. Null = not yet written.</summary>
    public string? NodeUserStories { get; set; }

    /// <summary>When the user stories were last updated.</summary>
    public DateTime? NodeUserStoriesUpdatedAt { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AudioCompletedAt { get; set; }

    /// <summary>Sum of characters sent to TTS across this node's beats.</summary>
    public int CharsNarrated { get; set; }

    /// <summary>Number of beats that have been successfully narrated in the
    /// current run. The narration loop bumps this each iteration so the
    /// polling UI can show "narrated N of M" without re-loading the whole
    /// beats collection on every tick. Reset to 0 at the start of each
    /// NarrateAsync call (so the count reflects "this run" not "lifetime").</summary>
    public int NarratedBeatCount { get; set; }

    /// <summary>Total beats considered for narration this run (the snapshot
    /// of the node's beat count when NarrateAsync started). Pairs with
    /// <see cref="NarratedBeatCount"/> to give the UI a stable denominator
    /// even if beats are added/removed mid-narration.</summary>
    public int TotalBeatsToNarrate { get; set; }

    /// <summary>Where the listener was when they walked away — a specific
    /// beat in this node. Resume on /node/{id}.</summary>
    public Guid? LastPlayedBeatId { get; set; }

    /// <summary>Seconds into <see cref="LastPlayedBeatId"/>.</summary>
    public double? LastPlayedSec { get; set; }

    /// <summary>Notes about why a run failed, if it did.</summary>
    public string? Error { get; set; }

    /// <summary>Monotonic publish counter. Incremented by one each time a docx
    /// is exported via <c>DocxExportService</c>. Zero = never published.
    /// Used to build versioned filenames (e.g. "Attendance V3.docx") so the
    /// author can keep copies without relying on timestamps.</summary>
    public int Version { get; set; }

    /// <summary>Published author / pen name shown on the title page and embedded
    /// in docx document properties. Defaults to "MindAttic" (the pen name for
    /// this story universe) when null or empty. Set per-node only if a book
    /// needs a different attribution.</summary>
    public string? Author { get; set; }

    /// <summary>Optional subtitle shown beneath the title on the title page
    /// (e.g. "Book 1: Matthew" under a series title of "Gospel: History vs.
    /// Heritage"). Null or empty means no subtitle line is printed.</summary>
    public string? Subtitle { get; set; }

    // ── Cover art ─────────────────────────────────────────────────────────

    /// <summary>LLM-authored visual description of what should appear on this
    /// book's cover — subject, setting, mood, palette, composition, art style —
    /// generated from the book's Summary/Description and universe. Contains no
    /// title/author typography (that's composited separately); this is the
    /// image-model prompt, not cover copy. Null = not yet generated. Written by
    /// <c>CoverPromptService</c>, regenerate via <c>prose --generate-cover-prompt</c>.</summary>
    public string? CoverPrompt { get; set; }

    /// <summary>When <see cref="CoverPrompt"/> was last generated. Null = never.</summary>
    public DateTime? CoverPromptGeneratedAt { get; set; }

    /// <summary>Relative path (under the media dir) to the generated cover image
    /// (png/jpg), e.g. "covers/atte.png". Written by <c>CoverImageService</c>
    /// after a successful image-provider call. Null = no image generated yet.</summary>
    public string? CoverImagePath { get; set; }

    /// <summary>Which image provider produced <see cref="CoverImagePath"/>:
    /// "openai" | "stability" | "google". Null = not yet generated.</summary>
    public string? CoverImageProvider { get; set; }

    /// <summary>When <see cref="CoverImagePath"/> was last generated. Null = never.</summary>
    public DateTime? CoverImageGeneratedAt { get; set; }

    /// <summary>Relative path (under the media dir) to the assembled #booktok
    /// announcement MP4, e.g. "booktok/booktok-atte.mp4". Written by
    /// <c>BookTokVideoService</c>. Null = no video generated yet.</summary>
    public string? BookTokVideoPath { get; set; }

    /// <summary>Which video provider produced <see cref="BookTokVideoPath"/>:
    /// "kling" | "runway" | "sora". Null = not yet generated.</summary>
    public string? BookTokVideoProvider { get; set; }

    /// <summary>When <see cref="BookTokVideoPath"/> was last generated. Null = never.</summary>
    public DateTime? BookTokVideoGeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ───────────────────────────────────────────────────────

    public List<Node> Children { get; set; } = new();
    public List<BeatNode> BeatNodes { get; set; } = new();

    /// <summary>Publish-run history (1:M). Each Publish appends one row; the
    /// latest completed run's file is what <see cref="CombinedAudioPath"/>
    /// points at.</summary>
    public List<NodePublication> Publications { get; set; } = new();

    /// <summary>Persona reader-reviews (1:M). Each review run appends rows; the
    /// aggregate lives in <see cref="NodeReviewSummary"/>.</summary>
    public List<NodeReview> Reviews { get; set; } = new();

    /// <summary>Amazon KDP / storefront search keywords for this node (up to 7,
    /// ordered by <see cref="NodeKeyword.SortOrder"/>).</summary>
    public List<NodeKeyword> Keywords { get; set; } = new();
}

public static class NodeFactory
{
    /// <summary>New empty node of the concrete type implied by a free-form
    /// kind label ("series"/"saga"/"anthology" → SeriesNode; "chapter"/"scene"/
    /// "episode"/"snippet" → ChapterNode; anything else → BookNode). Used where
    /// the type arrives as data (CLI flags, import files) rather than statically.
    /// The label itself is preserved on <see cref="Node.Kind"/> for display.</summary>
    public static Node Create(string? kind)
    {
        var node = kind?.Trim().ToLowerInvariant() switch
        {
            "series" or "saga" or "anthology"            => (Node)new SeriesNode(),
            "chapter" or "scene" or "episode" or "snippet" => new ChapterNode(),
            _                                            => new BookNode(),
        };
        if (!string.IsNullOrWhiteSpace(kind)) node.Kind = kind.Trim();
        return node;
    }

    /// <summary>New empty node of the same concrete type as <paramref name="like"/>.</summary>
    public static Node CreateLike(Node like) => like switch
    {
        SeriesNode  => new SeriesNode(),
        ChapterNode => new ChapterNode(),
        _           => new BookNode(),
    };
}

// ── Concrete node types (TPH discriminator NodeType) ─────────────────────

/// <summary>Top-level grouping: a saga, series, or anthology. Children are
/// BookNodes. Never holds beats directly.</summary>
public class SeriesNode : Node
{
    public SeriesNode() { Kind = "series"; }
}

/// <summary>A single narrative arc — a book, novella, or standalone piece. Parent
/// (optional) is a SeriesNode; children (optional) are ChapterNodes. A leaf
/// BookNode with no chapters holds its beats directly.</summary>
public class BookNode : Node
{
    public BookNode() { Kind = "book"; }
}

/// <summary>Organizational unit inside a book. Parent is a BookNode; holds
/// beats, never child nodes.</summary>
public class ChapterNode : Node
{
    public ChapterNode() { Kind = "chapter"; }
}

// ── NodeAmendment ───────────────────────────────────────────────────────
// Append-only change log for a node's narrative spine. Each row records a
// decision made during writing that alters the bible, characters, arc, or
// world rules in scope for this node. SequenceNo is monotonically
// increasing per node. System-versioned so no amendment is ever truly lost.

public class NodeAmendment
{
    public Guid     Id         { get; set; }
    public Guid     NodeId   { get; set; }
    /// <summary>1-based index per node. Assigned by service on insert.</summary>
    public int      SequenceNo { get; set; }
    /// <summary>Short reference code: SA-1, SA-2, … (node-amendment).</summary>
    public string   Code       { get; set; } = "";
    /// <summary>One-line description of the change.</summary>
    public string   Summary    { get; set; } = "";
    /// <summary>Full amendment text (markdown).</summary>
    public string   Body       { get; set; } = "";
    public DateTime CreatedAt  { get; set; }
    /// <summary>"cli" | "mcp" or a user-facing label.</summary>
    public string   CreatedBy  { get; set; } = "";
}

// ── NodeSpineVersion ────────────────────────────────────────────────────
// Bridge table: records which version of the spine (bible + user stories +
// amendments) was in effect when a particular docx version of the node was
// pinned. One row per (NodeId, NodeVersion) — the docx publish counter.
//
// Lets the engine answer:
//   "what was the bible when we scored 85%?" (get by NodeVersion=N)
//   "has the spine drifted since the last pin?" (compare hashes)
//   "how many amendments were applied at version 3?" (AmendmentCount)

public class NodeSpineVersion
{
    public Guid     Id                { get; set; }
    public Guid     NodeId          { get; set; }
    /// <summary>Mirrors Node.Version (docx publish counter) at time of pin.</summary>
    public int      NodeVersion     { get; set; }
    /// <summary>SHA-256 hex of NodeOutline content at pinning time. Empty = no bible yet.</summary>
    public string   OutlineHash         { get; set; } = "";
    /// <summary>SHA-256 hex of NodeUserStories content at pinning time. Empty = not yet written.</summary>
    public string   UserStoriesHash   { get; set; } = "";
    /// <summary>SequenceNo of the latest NodeAmendment applied at this pin. 0 = none.</summary>
    public int      AmendmentCount    { get; set; }
    public DateTime PinnedAt          { get; set; }
    /// <summary>"cli" | "mcp" | "auto-review" | "auto-publish".</summary>
    public string   PinnedBy          { get; set; } = "";
    /// <summary>Optional human note about what changed at this version.</summary>
    public string   Notes             { get; set; } = "";
}

// ── NodeKeyword ─────────────────────────────────────────────────────────────
// Amazon KDP / storefront search keywords. Up to 7 per node, ordered by
// SortOrder (1-based). Written by --seed-keywords and copied to keywords.txt
// on every --export-node run.

public class NodeKeyword
{
    public Guid     Id        { get; set; }
    public Guid     NodeId  { get; set; }
    public Node?  Node    { get; set; }
    /// <summary>Keyword phrase — max 100 chars (Amazon allows 50; extra room for flexibility).</summary>
    public string   Keyword   { get; set; } = "";
    /// <summary>1-based display order.</summary>
    public int      SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
