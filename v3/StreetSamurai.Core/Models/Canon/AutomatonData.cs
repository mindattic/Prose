using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// An automaton — soulless machine construct. War machines, domestic robots,
/// Iowan Behemoths, training bots, and industrial platforms. Not alive, not AI.
/// </summary>
public class AutomatonData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "automaton";
    [JsonPropertyName("classification")] public string Classification { get; set; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tier_availability")] public string TierAvailability { get; set; } = "";
    [JsonPropertyName("legality")] public string Legality { get; set; } = "";
    [JsonPropertyName("autonomy_level")] public string AutonomyLevel { get; set; } = "";
    [JsonPropertyName("dimensions")] public string Dimensions { get; set; } = "";
    [JsonPropertyName("weight")] public string Weight { get; set; } = "";
    [JsonPropertyName("power_source")] public string PowerSource { get; set; } = "";
    [JsonPropertyName("locomotion")] public string Locomotion { get; set; } = "";
    [JsonPropertyName("armament")] public List<string> Armament { get; set; } = [];
    [JsonPropertyName("sensors")] public List<string> Sensors { get; set; } = [];
    [JsonPropertyName("countermeasures")] public string Countermeasures { get; set; } = "";
    [JsonPropertyName("known_deployments")] public List<string> KnownDeployments { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("cultural_context")] public string CulturalContext { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
