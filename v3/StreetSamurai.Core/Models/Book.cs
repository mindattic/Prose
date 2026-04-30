using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models;

/// <summary>
/// A Book is the publishable unit. It owns an ordered list of Chapters, a state
/// vector that carries forward (open threads, character status, world deltas),
/// and an arc target that constrains extension generation. Chapters reference
/// their parent via <see cref="Chapter.BookId"/>; this list is the canonical order.
///
/// Series is a deferred parent — <see cref="SeriesId"/> is nullable for now and
/// will become non-null once Series ships. No need to migrate later: the field is
/// already on disk.
/// </summary>
public class Book
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    [JsonPropertyName("series_id")]
    public string? SeriesId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled Book";

    /// <summary>One-paragraph book premise. Feeds the chapter director when extending.</summary>
    [JsonPropertyName("premise")]
    public string Premise { get; set; } = "";

    /// <summary>What this book is *about* and where it lands. Used as the extension target.</summary>
    [JsonPropertyName("arc_target")]
    public string ArcTarget { get; set; } = "";

    /// <summary>Resolved character names — first entry is the lead.</summary>
    [JsonPropertyName("protagonists")]
    public List<string> Protagonists { get; set; } = [];

    /// <summary>Optional cover image URL or path. Bookshelf falls back to a templated card if absent.</summary>
    [JsonPropertyName("cover_image_url")]
    public string? CoverImageUrl { get; set; }

    /// <summary>Optional tagline shown beneath the title on the bookshelf card.</summary>
    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    /// <summary>Ordered list of Chapter ids. Single source of truth for chapter sequence within this book.</summary>
    [JsonPropertyName("chapter_ids")]
    public List<string> ChapterIds { get; set; } = [];

    /// <summary>State vector at end of last chapter. Seeds extension; updated on compose.</summary>
    [JsonPropertyName("state_at_end")]
    public BookState StateAtEnd { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "drafting";

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Running narrative state at a point in time. Persisted on each Chapter and rolled
/// up on Book. The chapter-level snapshot is the seed for the next chapter's generation;
/// the book-level snapshot is the head of the chain. Updating one without the other
/// causes drift, so the compose pass is the only thing allowed to write these.
/// </summary>
public class BookState
{
    /// <summary>Per-character status: wounds, location, mood, debts, possessions of note.</summary>
    [JsonPropertyName("character_status")]
    public Dictionary<string, string> CharacterStatus { get; set; } = [];

    /// <summary>Promises made to the reader that haven't paid off yet.</summary>
    [JsonPropertyName("open_threads")]
    public List<string> OpenThreads { get; set; } = [];

    /// <summary>Canon-mutating events the world graph should know about (deaths, retcons, new factions).</summary>
    [JsonPropertyName("canon_changes")]
    public List<string> CanonChanges { get; set; } = [];

    /// <summary>In-world time at the end of the last chapter. Free-form to accommodate
    /// both ISO timestamps (when a chapter pins an exact date) and descriptive prose
    /// (e.g. "approximately ten years before Bushido Coda" when timing is intentionally fuzzy).</summary>
    [JsonPropertyName("in_world_time")]
    public string? InWorldTime { get; set; }
}

/// <summary>
/// A Series groups Books that share continuity. Stub for now — fields exist so
/// Books can reference a SeriesId without migration churn later, but there is no
/// Series UI or repository yet.
/// </summary>
public class Series
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled Series";

    [JsonPropertyName("premise")]
    public string Premise { get; set; } = "";

    [JsonPropertyName("book_ids")]
    public List<string> BookIds { get; set; } = [];

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}
