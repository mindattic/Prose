using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A news report, broadcast, or historical event record from GLMZ's history.
/// Presented as a newscaster's review — wars, disasters, terrorism, corporate conflicts,
/// political upheavals, and the events that shaped a world where citizens hire runners
/// to solve problems the system won't.
/// </summary>
public class NewsData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("headline")] public string Headline { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "news";
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("reporter")] public string Reporter { get; set; } = "";
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("aftermath")] public string Aftermath { get; set; } = "";
    [JsonPropertyName("casualties")] public string Casualties { get; set; } = "";
    [JsonPropertyName("entities_involved")] public List<string> EntitiesInvolved { get; set; } = [];
    [JsonPropertyName("locations")] public List<string> Locations { get; set; } = [];
    [JsonPropertyName("runner_relevance")] public string RunnerRelevance { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
