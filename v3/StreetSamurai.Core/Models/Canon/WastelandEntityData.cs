using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A Flyover entity — the things that moved back in when humanity moved to the cities.
/// Recovering nature, escaped lab specimens, emergent species, feral machines,
/// and the biological experiments of a century of unchecked rewilding across
/// the 78% of the continent that the megacities don't occupy.
/// The Flyover is not a wasteland. It is what the land becomes when people leave.
/// </summary>
public class FlyoverEntityData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "flyover_entity";
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
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
