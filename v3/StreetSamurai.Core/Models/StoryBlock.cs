using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models;

/// <summary>
/// A story project. The HTML body is the source of truth — everything
/// (text, formatting, embedded images, entity links, TTS tags) lives in one field.
/// The world graph provides all context; no need for blocks, chapters, or synopses.
/// </summary>
public class StoryProject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Untitled";

    [JsonPropertyName("characters")]
    public List<string> Characters { get; set; } = [];

    [JsonPropertyName("status")]
    public string Status { get; set; } = "draft";

    /// <summary>The rich HTML body — source of truth.</summary>
    [JsonPropertyName("html")]
    public string Html { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    /// <summary>Plain text derived from HTML — for search, word count, entity detection.</summary>
    [JsonIgnore]
    public string PlainText => System.Text.RegularExpressions.Regex.Replace(Html, "<[^>]+>", " ").Trim();
}
