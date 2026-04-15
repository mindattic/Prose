using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A pharmaceutical, recreational drug, combat stimulant, or mind-altering substance.
/// How people in 2200 get high, stay focused, fight harder, sleep deeper, forget,
/// remember, and everything in between.
/// </summary>
public class PharmaceuticalData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "pharmaceutical";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("subcategory")] public string Subcategory { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("method_of_use")] public string MethodOfUse { get; set; } = "";
    [JsonPropertyName("effects")] public List<string> Effects { get; set; } = [];
    [JsonPropertyName("side_effects")] public List<string> SideEffects { get; set; } = [];
    [JsonPropertyName("duration")] public string Duration { get; set; } = "";
    [JsonPropertyName("addiction_risk")] public string AddictionRisk { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("street_price")] public string StreetPrice { get; set; } = "";
    [JsonPropertyName("cultural_context")] public string CulturalContext { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
