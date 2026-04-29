using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models;

/// <summary>
/// Result of running <see cref="StreetSamurai.Core.Services.BookReviewService"/> against
/// the ordered chapter sequence of a Book. Persisted to the book's folder so reruns
/// can skip chapters whose content checksum hasn't changed.
/// </summary>
public class BookReviewReport
{
    [JsonPropertyName("book_id")]
    public string BookId { get; set; } = "";

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Number of LLM voters that returned valid findings.</summary>
    [JsonPropertyName("voter_count")]
    public int VoterCount { get; set; }

    /// <summary>Per-chapter content hash at the time of review. Lets reruns reuse cached findings.</summary>
    [JsonPropertyName("chapter_checksums")]
    public Dictionary<string, string> ChapterChecksums { get; set; } = [];

    /// <summary>Whole-book findings — arc, motif, voice consistency.</summary>
    [JsonPropertyName("book_findings")]
    public List<ReviewFinding> BookFindings { get; set; } = [];

    /// <summary>Per-chapter findings — what each chapter establishes vs leaves dangling.</summary>
    [JsonPropertyName("chapter_findings")]
    public List<ReviewFinding> ChapterFindings { get; set; } = [];

    /// <summary>
    /// Per-seam findings — between chapters N and N+1. Continuity, status carry-through,
    /// motif callbacks. The "thoughtful undercurrent" lives here.
    /// </summary>
    [JsonPropertyName("seam_findings")]
    public List<ReviewFinding> SeamFindings { get; set; } = [];

    /// <summary>Set on failure — the report still saves so the UI can show the error.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// A single review finding. Some findings are diagnostic only (no actionable edit);
/// others carry a <see cref="BeforeText"/> / <see cref="AfterText"/> pair that the
/// user can apply in one click. The flow is always preview-and-confirm — edits never
/// apply automatically.
/// </summary>
public class ReviewFinding
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    [JsonPropertyName("layer")]
    public ReviewLayer Layer { get; set; }

    [JsonPropertyName("kind")]
    public ReviewKind Kind { get; set; }

    [JsonPropertyName("severity")]
    public ReviewSeverity Severity { get; set; }

    /// <summary>Target chapter for the edit. Null for whole-book findings that don't bind to one chapter.</summary>
    [JsonPropertyName("chapter_id")]
    public string? ChapterId { get; set; }

    /// <summary>For seam findings: the chapter that follows <see cref="ChapterId"/> in the book sequence.</summary>
    [JsonPropertyName("next_chapter_id")]
    public string? NextChapterId { get; set; }

    /// <summary>One sentence describing the issue in human terms.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    /// <summary>Why this matters — the through-line, the missed callback, the dropped thread.</summary>
    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = "";

    /// <summary>
    /// Exact text in the chapter HTML to be replaced. Must match exactly once for safe apply.
    /// Empty string = diagnostic-only finding with no actionable edit.
    /// </summary>
    [JsonPropertyName("before_text")]
    public string BeforeText { get; set; } = "";

    /// <summary>The proposed replacement text. Empty when <see cref="BeforeText"/> is empty.</summary>
    [JsonPropertyName("after_text")]
    public string AfterText { get; set; } = "";

    /// <summary>How many of the LLM voters surfaced this finding (or one substantively similar).</summary>
    [JsonPropertyName("voter_agreement")]
    public int VoterAgreement { get; set; }

    [JsonPropertyName("status")]
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    [JsonPropertyName("applied_at")]
    public DateTime? AppliedAt { get; set; }

    [JsonPropertyName("rejected_at")]
    public DateTime? RejectedAt { get; set; }

    /// <summary>True if there is a concrete edit attached.</summary>
    [JsonIgnore]
    public bool HasEdit => !string.IsNullOrWhiteSpace(BeforeText) && !string.IsNullOrWhiteSpace(AfterText);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewLayer { Book, Chapter, Seam }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewKind
{
    /// <summary>Hard continuity: timeline, location, injuries, character status conflicts.</summary>
    Continuity,
    /// <summary>Recurring object/phrase/image that should appear and doesn't, or appears once and dies.</summary>
    Motif,
    /// <summary>Character state at end of chapter N not acknowledged in chapter N+1.</summary>
    StatusCarry,
    /// <summary>Tonal/register inconsistency or POV slip across chapters.</summary>
    Voice,
    /// <summary>Whole-book arc shape, act structure, climax placement.</summary>
    Arc,
    /// <summary>Anaphoric callback opportunity — earlier chapter introduced something a later chapter could echo.</summary>
    Anaphora,
    /// <summary>Chapter opens flat — generic first sentence, no character-specific observation, no concrete detail.</summary>
    FirstLine,
    /// <summary>Paragraph that does no work — neither advances plot, reveals character, builds world via specificity, establishes stakes, pays off a planted detail, nor creates rhythm.</summary>
    ParagraphService,
    /// <summary>Pacing collapse: too many low-tension beats in a row, or whiplash from a single jump.</summary>
    TensionDelta,
    /// <summary>Voice cadence drift — paragraph's vocabulary fingerprint matches a different POV character better than the chapter's.</summary>
    VoiceCadence,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewSeverity { Critical, Warning, Suggestion }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewStatus { Pending, Applied, Rejected }
