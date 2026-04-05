using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A genetic modification — geneware. Unlike cyberware (machines installed in the body),
/// geneware alters the host's DNA. From innocuous cosmetic changes (color-changing hair,
/// bioluminescent skin) to functional modifications (toxin glands, enhanced musculature)
/// to radical body modification (tails, antlers, gills, cat ears). If it changes your genes,
/// it's geneware.
/// </summary>
public class GenewareData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("brand_name")] public string BrandName { get; set; } = "";
    [JsonPropertyName("product_name")] public string ProductName { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "geneware";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("target_system")] public string TargetSystem { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("source_organism")] public string SourceOrganism { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("procedure")] public string Procedure { get; set; } = "";
    [JsonPropertyName("expression_time")] public string ExpressionTime { get; set; } = "";
    [JsonPropertyName("reversibility")] public string Reversibility { get; set; } = "";
    [JsonPropertyName("side_effects")] public List<string> SideEffects { get; set; } = [];
    [JsonPropertyName("social_perception")] public string SocialPerception { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
