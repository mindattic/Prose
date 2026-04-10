using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A technology, system, or scientific advancement in the GLMZ world.
/// Designed as a graph node — links to manufacturers, dependent technologies, users, and locations.
/// </summary>
public class TechnologyData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("brand_name")] public string BrandName { get; set; } = "";
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
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
