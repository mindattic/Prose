using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A slang term, phrase, or jargon entry from GLMZ.
/// The vocabulary of the GLM is a living document — each tier, faction,
/// and district has its own dialect. Shelf Cant is different from Spire Speak.
/// </summary>
public class VocabularyEntry : ICanonEntity
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
