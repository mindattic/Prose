using System.Text.Json.Serialization;

namespace Prose.Core.Models;

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
/// One chapter's slice of the book outline. A single freeform <see cref="Body"/>
/// blurb captures synopsis / key beats / threads opened-or-closed / state
/// changes — six structured fields previously fought to stay in sync with each
/// other and with the prose, and an outline is a sketch, not a fact database.
///
/// <para><b>Legacy fields below.</b> The old short/long synopsis, key beats,
/// opens/closes threads, and state-changes columns are preserved on the model
/// for backward compatibility — older saved outlines still load without loss.
/// New writes go to <see cref="Body"/>; readers should prefer
/// <see cref="EffectiveBody"/> which returns Body when present and otherwise
/// composes the legacy fields into a single blurb.</para>
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

    /// <summary>POV character for this chapter. Kept structured because it's a single value, low cost, high downstream signal.</summary>
    [JsonPropertyName("pov_character")]
    public string PovCharacter { get; set; } = "";

    /// <summary>
    /// Freeform per-chapter outline blurb. Replaces the structured short/long
    /// synopsis + key_beats + opens_threads + closes_threads + state_changes
    /// fields. Authors think in prose, the Director can extract structure at
    /// prompt time, and one source of truth can't drift against itself.
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    // ── Legacy fields (backward compat) ─────────────────────────────────────

    [JsonPropertyName("short_synopsis")]
    public string ShortSynopsis { get; set; } = "";

    [JsonPropertyName("long_synopsis")]
    public string LongSynopsis { get; set; } = "";

    [JsonPropertyName("key_beats")]
    public List<string> KeyBeats { get; set; } = [];

    [JsonPropertyName("opens_threads")]
    public List<string> OpensThreads { get; set; } = [];

    [JsonPropertyName("closes_threads")]
    public List<string> ClosesThreads { get; set; } = [];

    [JsonPropertyName("state_changes")]
    public Dictionary<string, string> StateChanges { get; set; } = [];

    /// <summary>
    /// The text every reader should consume. Returns <see cref="Body"/> when
    /// non-empty; otherwise composes the legacy fields into one blurb so old
    /// outlines keep producing useful prompt context.
    /// </summary>
    [JsonIgnore]
    public string EffectiveBody
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Body)) return Body;
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(LongSynopsis))  sb.AppendLine(LongSynopsis);
            else if (!string.IsNullOrWhiteSpace(ShortSynopsis)) sb.AppendLine(ShortSynopsis);
            if (KeyBeats.Count       > 0) sb.AppendLine("Beats: "  + string.Join(" | ", KeyBeats));
            if (OpensThreads.Count   > 0) sb.AppendLine("Opens: "  + string.Join("; ",  OpensThreads));
            if (ClosesThreads.Count  > 0) sb.AppendLine("Closes: " + string.Join("; ",  ClosesThreads));
            if (StateChanges.Count   > 0)
                sb.AppendLine("State: " + string.Join("; ", StateChanges.Select(kv => $"{kv.Key}: {kv.Value}")));
            return sb.ToString().TrimEnd();
        }
    }
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
/// One detected drift between a chapter's outline body and the prose
/// actually written. <see cref="Kind"/> is "missing" (outline promised
/// something the prose didn't deliver), "contradiction" (the prose says
/// something the outline disagrees with), or "extra" (the prose introduced
/// something the outline didn't account for — could be welcome, could be
/// scope creep). <see cref="Summary"/> is the short headline; the
/// <c>OutlineSays</c>/<c>ProseSays</c> pair is the receipt.
/// </summary>
public record OutlineDriftFinding(string Kind, string Summary, string OutlineSays, string ProseSays);

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
