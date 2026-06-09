using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models;

/// <summary>
/// A single chapter of a Book. Was previously called StoryProject and treated as
/// a self-contained story; renamed because the unit of generation has always been
/// a single chapter — multi-chapter narratives need a Book wrapper to track arc state.
///
/// The HTML body remains the rendered source of truth. Beats are now persisted
/// alongside it so chapter-level operations (regen one beat, score quality, splice
/// new prose) don't have to round-trip through generation state.
/// </summary>
public class Chapter
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    /// <summary>Parent Book id. Null for orphaned chapters that haven't been absorbed yet.</summary>
    [JsonPropertyName("book_id")]
    public string? BookId { get; set; }

    /// <summary>Position within the parent Book. Null for orphans. 1-indexed for human display.</summary>
    [JsonPropertyName("number")]
    public int? Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled";

    /// <summary>One-paragraph chapter synopsis. Feeds beat generation when extending or regenerating.</summary>
    [JsonPropertyName("synopsis")]
    public string Synopsis { get; set; } = "";

    [JsonPropertyName("characters")]
    public List<string> Characters { get; set; } = [];

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

    /// <summary>The rich HTML body — rendered source of truth.</summary>
    [JsonPropertyName("html")]
    public string Html { get; set; } = "";

    /// <summary>
    /// Raw markdown source typed by the writer. Round-trips losslessly through
    /// the records JSON blob so toolbar formatting (bold, italic, headings,
    /// blockquote, lists) survives reload. Html is regenerated from this on save
    /// so reading views and exports keep working.
    /// </summary>
    [JsonPropertyName("markdown")]
    public string Markdown { get; set; } = "";

    /// <summary>
    /// Persisted beats. Populated when a chapter is generated through the director,
    /// or when an absorbed chapter is split by an LLM pass. Empty for legacy chapters
    /// that only have HTML — those can be back-filled with a one-shot split.
    /// </summary>
    [JsonPropertyName("beats")]
    public List<ChapterBeat> Beats { get; set; } = [];

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    /// <summary>Plain text derived from HTML — for search, word count, entity detection.</summary>
    [JsonIgnore]
    public string PlainText => System.Text.RegularExpressions.Regex.Replace(Html, "<[^>]+>", " ").Trim();
}

/// <summary>
/// A single beat within a chapter. Persistent shape of what the generator
/// produces in <see cref="StreetSamurai.Core.Services.GeneratedStoryBeat"/>;
/// kept separate so chapters can be edited without dragging in generation state.
/// </summary>
public class ChapterBeat
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.CreateVersion7().ToString("N");

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("synopsis")]
    public string Synopsis { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("act")]
    public int Act { get; set; }

    [JsonPropertyName("structure_role")]
    public string StructureRole { get; set; } = "";

    [JsonPropertyName("scene_type")]
    public string SceneType { get; set; } = "scene";
}
