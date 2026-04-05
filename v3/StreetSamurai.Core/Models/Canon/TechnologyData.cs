using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A technology, system, or scientific advancement in the Meridian 88 world.
/// Designed as a graph node — links to manufacturers, dependent technologies, users, and locations.
/// </summary>
public class TechnologyData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "technology";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("subcategory")] public string Subcategory { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("developers")] public List<string> Developers { get; set; } = [];
    [JsonPropertyName("base_technologies")] public List<string> BaseTechnologies { get; set; } = [];
    [JsonPropertyName("enables")] public List<string> Enables { get; set; } = [];
    [JsonPropertyName("social_impact")] public string SocialImpact { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
