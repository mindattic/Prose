using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Equipment, gear, augmentations, implants, and devices in the GLMZ world.
/// Distinct from weaponry — covers tools, augments, armor, and utility gear.
/// </summary>
public class EquipmentData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("brand_name")] public string BrandName { get; set; } = "";
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
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
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
    [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraData { get; set; }
}
