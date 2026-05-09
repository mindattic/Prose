using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models;

/// <summary>
/// Book-level plot spine. Shared across every chapter in the book — the canonical
/// answer to "what is this book about, what happens in each chapter, how do
/// chapters depend on each other." Loaded by the Director on every chapter
/// generation so chapter N knows what chapter N-1 set up and what chapter N+1
/// will need.
///
/// Persisted to engine/data/books/{bookId}.outline.json, sibling to the book file.
/// </summary>
public class BookOutline
{
    [JsonPropertyName("book_id")]
    public string BookId { get; set; } = "";

    /// <summary>One paragraph: the book in a single emotional sentence.</summary>
    [JsonPropertyName("premise")]
    public string Premise { get; set; } = "";

    /// <summary>Where the book lands. The arc target — what the protagonist becomes / loses / refuses.</summary>
    [JsonPropertyName("arc_target")]
    public string ArcTarget { get; set; } = "";

    /// <summary>Thematic argument — what the book is *about* underneath what happens.</summary>
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "";

    /// <summary>Three-act / five-act / freeform — labels how the chapter sequence is shaped.</summary>
    [JsonPropertyName("structure")]
    public string Structure { get; set; } = "freeform";

    /// <summary>
    /// Outline workflow state. Books can't be generated through the Director's beat
    /// pipeline until the outline is Approved — protects against writing prose against
    /// a half-baked plot. Draft = under construction, InReview = reconsideration pending,
    /// Approved = green-lit for chapter generation.
    /// </summary>
    [JsonPropertyName("status")]
    public OutlineStatus Status { get; set; } = OutlineStatus.Draft;

    /// <summary>Per-chapter outline entries, ordered to match Book.ChapterIds.</summary>
    [JsonPropertyName("chapters")]
    public List<BookChapterOutline> Chapters { get; set; } = [];

    /// <summary>Book-level promises to the reader, planted by chapter X, paid off by chapter Y.</summary>
    [JsonPropertyName("threads")]
    public List<BookThread> Threads { get; set; } = [];

    /// <summary>Pending LLM-proposed adjustments from the most recent reconsideration pass.</summary>
    [JsonPropertyName("pending_adjustments")]
    public List<OutlineAdjustment> PendingAdjustments { get; set; } = [];

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One chapter's slice of the book outline. Distinct from the chapter's
/// <see cref="Chapter.Synopsis"/> — that's freeform; this is structured for
/// generation use (key beats, threads opened/closed).
/// </summary>
public class BookChapterOutline
{
    [JsonPropertyName("chapter_id")]
    public string ChapterId { get; set; } = "";

    /// <summary>1-indexed position. Mirrored from <see cref="Chapter.Number"/>.</summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    /// <summary>One sentence — what this chapter is, in shortest form. Drives TOC and the Plot view.</summary>
    [JsonPropertyName("short_synopsis")]
    public string ShortSynopsis { get; set; } = "";

    /// <summary>One paragraph — the chapter's premise, conflict, and end-state.</summary>
    [JsonPropertyName("long_synopsis")]
    public string LongSynopsis { get; set; } = "";

    /// <summary>Ordered key beats — the must-include plot points. NOT prose-level beats; one rung up.</summary>
    [JsonPropertyName("key_beats")]
    public List<string> KeyBeats { get; set; } = [];

    /// <summary>Threads (open promises) that this chapter introduces.</summary>
    [JsonPropertyName("opens_threads")]
    public List<string> OpensThreads { get; set; } = [];

    /// <summary>Threads from earlier chapters that this chapter resolves.</summary>
    [JsonPropertyName("closes_threads")]
    public List<string> ClosesThreads { get; set; } = [];

    /// <summary>Character state changes by end of chapter — wounds, debts, reveals.</summary>
    [JsonPropertyName("state_changes")]
    public Dictionary<string, string> StateChanges { get; set; } = [];

    /// <summary>POV character for this chapter. Lets the book outline track multi-POV books cleanly.</summary>
    [JsonPropertyName("pov_character")]
    public string PovCharacter { get; set; } = "";
}

/// <summary>
/// A book-level promise to the reader — a thread planted in chapter X that pays off in chapter Y.
/// Distinct from per-chapter <see cref="BookChapterOutline.OpensThreads"/> which is just a tag;
/// this is the structural through-line that the review pipeline checks.
/// </summary>
public class BookThread
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>Chapter id where the thread was planted.</summary>
    [JsonPropertyName("planted_in_chapter_id")]
    public string PlantedInChapterId { get; set; } = "";

    /// <summary>Chapter id where the thread is intended to pay off. Empty = unresolved by design.</summary>
    [JsonPropertyName("pays_off_in_chapter_id")]
    public string PaysOffInChapterId { get; set; } = "";

    [JsonPropertyName("status")]
    public ThreadStatus Status { get; set; } = ThreadStatus.Open;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThreadStatus { Open, Resolved, Abandoned }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutlineStatus
{
    /// <summary>Editable, not yet ready for chapter generation. Director refuses to write against this.</summary>
    Draft,
    /// <summary>An edit triggered LLM reconsideration of other chapters; suggestions awaiting user accept/reject.</summary>
    InReview,
    /// <summary>User has approved the outline. Chapter generation is unlocked.</summary>
    Approved,
}

/// <summary>
/// Thrown when book-context prose generation is attempted before the book's
/// outline is <see cref="OutlineStatus.Approved"/>. Callers that drive prose
/// generation against a specific book should call
/// <c>BookOutlineService.EnsureApprovedForGeneration(bookId)</c> first.
/// </summary>
public class OutlineNotApprovedException : InvalidOperationException
{
    public string BookId { get; }
    public OutlineStatus CurrentStatus { get; }

    public OutlineNotApprovedException(string bookId, OutlineStatus currentStatus)
        : base($"Book '{bookId}' outline must be Approved before chapter generation; currently {currentStatus}.")
    {
        BookId = bookId;
        CurrentStatus = currentStatus;
    }
}

/// <summary>
/// One LLM-proposed adjustment to keep the outline coherent after a user edit.
/// Lives transiently on <see cref="BookOutline"/> until accepted or dismissed.
/// </summary>
public class OutlineAdjustment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    /// <summary>Chapter being adjusted (the one the suggestion would change).</summary>
    [JsonPropertyName("chapter_id")]
    public string ChapterId { get; set; } = "";

    /// <summary>Which field to adjust: "long_synopsis", "short_synopsis", "key_beats", "opens_threads", "closes_threads".</summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = "";

    [JsonPropertyName("before")]
    public string Before { get; set; } = "";

    [JsonPropertyName("after")]
    public string After { get; set; } = "";

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = "";

    /// <summary>Direction relative to the edited chapter — "before" (setup needed) or "after" (consequence to handle).</summary>
    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "";

    [JsonPropertyName("status")]
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
}
