using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A material, compound, or substrate — the stuff that stuff is made of.
/// Carbon nanotubes, bio-metal hybrids, SNT-derived composites, graphene variants,
/// smart materials, programmable matter, exotic alloys. 200 years of materials science
/// unleashed by the ability to bond organic tissue to synthetic substrates.
/// </summary>
public class SubstrateData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("brand_name")] public string BrandName { get; set; } = "";
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "substrate";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("properties")] public List<string> Properties { get; set; } = [];
    [JsonPropertyName("developers")] public List<string> Developers { get; set; } = [];
    [JsonPropertyName("applications")] public List<string> Applications { get; set; } = [];
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("cost")] public string Cost { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
