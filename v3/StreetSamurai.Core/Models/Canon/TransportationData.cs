using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A vehicle or transportation system — everything from subway cars to flying APCs.
/// 200 years of unregulated technological advancement. Flying cars exist. Most people
/// ride the L-train. The gap between what's possible and what's affordable is the gap
/// between tiers.
/// </summary>
public class TransportationData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "transportation";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("propulsion")] public string Propulsion { get; set; } = "";
    [JsonPropertyName("speed")] public string Speed { get; set; } = "";
    [JsonPropertyName("capacity")] public string Capacity { get; set; } = "";
    [JsonPropertyName("range")] public string Range { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("cost")] public string Cost { get; set; } = "";
    [JsonPropertyName("autonomy")] public string Autonomy { get; set; } = "";
    [JsonPropertyName("armament")] public string Armament { get; set; } = "";
    [JsonPropertyName("common_usage")] public string CommonUsage { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
}
