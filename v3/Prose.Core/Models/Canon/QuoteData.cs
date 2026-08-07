using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Models.Canon;

/// <summary>
/// A quote, saying, or notable line from the world — in-world attribution or anonymous street wisdom.
/// Ambient world flavor: not a graph entity, not rated, not linked. Exists in the ether.
/// </summary>
public class QuoteData : IWorldRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("quote")] public string Quote { get; set; } = "";
    [JsonPropertyName("attribution")] public string Attribution { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("context")] public string Context { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("in_world")] public bool InWorld { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
