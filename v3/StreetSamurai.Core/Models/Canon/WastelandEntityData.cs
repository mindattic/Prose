using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A wasteland entity — the things that moved into the empty spaces when humanity left.
/// Abandoned factories, flooded suburbs, collapsed malls, hollow church spires,
/// the post-industrial American interior left to rot when the last jobs relocated to the
/// megacity corridors. These creatures don't defy science; they define it.
/// They are what biology and chemistry and radiation and abandoned technology
/// produce when given a generation or two without oversight.
/// </summary>
public class WastelandEntityData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "wasteland_entity";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("classification")] public string Classification { get; set; } = "";
    [JsonPropertyName("origin")] public string Origin { get; set; } = "";
    [JsonPropertyName("substrate")] public string Substrate { get; set; } = "";
    [JsonPropertyName("territory")] public string Territory { get; set; } = "";
    [JsonPropertyName("physical_description")] public string PhysicalDescription { get; set; } = "";
    [JsonPropertyName("behavioral_profile")] public string BehavioralProfile { get; set; } = "";
    [JsonPropertyName("threat_level")] public string ThreatLevel { get; set; } = "";
    [JsonPropertyName("human_remnants")] public string HumanRemnants { get; set; } = "";
    [JsonPropertyName("known_locations")] public List<string> KnownLocations { get; set; } = [];
    [JsonPropertyName("glmz_migration_risk")] public string GlmzMigrationRisk { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
