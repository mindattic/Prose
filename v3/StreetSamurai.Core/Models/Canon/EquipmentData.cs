using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Equipment, gear, augmentations, implants, and devices in the Meridian 88 world.
/// Distinct from weaponry — covers tools, augments, armor, and utility gear.
/// </summary>
public class EquipmentData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "equipment";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("base_technologies")] public List<string> BaseTechnologies { get; set; } = [];
    [JsonPropertyName("specifications")] public Dictionary<string, string> Specifications { get; set; } = new();
    [JsonPropertyName("tactical_use")] public string TacticalUse { get; set; } = "";
    [JsonPropertyName("cultural_context")] public string CulturalContext { get; set; } = "";
    [JsonPropertyName("known_users")] public List<string> KnownUsers { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
}
