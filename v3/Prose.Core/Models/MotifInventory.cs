using System.Text.Json.Serialization;

namespace Prose.Core.Models;

/// <summary>
/// Per-book motif inventory. A motif is a named object, phrase, or image that
/// recurs (or should recur) across chapters. The system records new motifs as
/// chapters are written, and the review pipeline checks subsequent chapters
/// for callback opportunities.
/// </summary>
public class MotifInventory
{
    [JsonPropertyName("book_id")]
    public string BookId { get; set; } = "";

    [JsonPropertyName("motifs")]
    public List<BookMotif> Motifs { get; set; } = [];

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

public class BookMotif
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>One sentence on what this motif is and what it represents.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("kind")]
    public MotifKind Kind { get; set; }

    /// <summary>Chapter where this motif first appeared. Used so the system doesn't ask a chapter to call back to itself.</summary>
    [JsonPropertyName("introduced_in_chapter_id")]
    public string IntroducedInChapterId { get; set; } = "";

    /// <summary>Chapters that have referenced the motif. Lets the system flag dropped motifs.</summary>
    [JsonPropertyName("referenced_in_chapter_ids")]
    public List<string> ReferencedInChapterIds { get; set; } = [];
}

/// <summary>
/// A proposed motif surfaced by <see cref="Prose.Core.Services.MotifService.ProposeFromChapter"/>.
/// Pending user confirmation. Distinct from <see cref="BookMotif"/>, which is the persisted form.
/// </summary>
public class MotifProposal
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("kind")]
    public MotifKind Kind { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    /// <summary>One-line note on what triggered the proposal (which chapter, how often it appeared).</summary>
    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = "";
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MotifKind
{
    /// <summary>A named object the character has named or returned to (Maeve, the boots, the brick wall).</summary>
    Object,
    /// <summary>A repeated phrase or sentence pattern (e.g. "I am not actually doing this badly").</summary>
    Phrase,
    /// <summary>A recurring image or sense-impression (the Behemoth on the horizon, fluorescent light, a particular smell).</summary>
    Image,
    /// <summary>A behavioral tic that recurs across chapters (the strop ritual, announcing herself).</summary>
    Ritual,
}
