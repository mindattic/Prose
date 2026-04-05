using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// An ammunition type in the Meridian 88 world.
/// Cross-references compatible weapons by name.
/// </summary>
public class AmmunitionData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "ammunition";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("caliber")] public string Caliber { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("specifications")] public string Specifications { get; set; } = "";
    [JsonPropertyName("compatible_weapons")] public List<string> CompatibleWeapons { get; set; } = [];
    [JsonPropertyName("variants")] public List<string> Variants { get; set; } = [];
    [JsonPropertyName("cultural_context")] public string CulturalContext { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
