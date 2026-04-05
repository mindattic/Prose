using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A quote, saying, or notable line — from real-world inspiration, in-world characters,
/// or anonymous street wisdom. Some are attributed, some are just things people say.
/// </summary>
public class QuoteData
{
    [JsonPropertyName("quote")] public string Quote { get; set; } = "";
    [JsonPropertyName("attribution")] public string Attribution { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("context")] public string Context { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("in_world")] public bool InWorld { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
