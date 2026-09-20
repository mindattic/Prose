using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Models.Canon;

/// <summary>
/// A slang term, jargon entry, or piece of street cant from the GLMZ.
/// Ambient world flavor: not a graph entity, not rated, not linked.
/// The language of a place exists without a creator — it just accumulates.
/// </summary>
public class VocabularyData : IWorldRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("term")] public string Term { get; set; } = "";
    [JsonPropertyName("definition")] public string Definition { get; set; } = "";
    [JsonPropertyName("origin")] public string Origin { get; set; } = "";
    [JsonPropertyName("usage")] public string Usage { get; set; } = "";
    [JsonPropertyName("tier")] public string Tier { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("example")] public string Example { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
