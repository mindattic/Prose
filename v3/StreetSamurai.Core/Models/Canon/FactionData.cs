using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed faction model.
/// </summary>
public record FactionData
{
    [JsonPropertyName("type")] public string Type { get; init; } = "faction";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; init; } = [];
    [JsonPropertyName("motto")] public string Motto { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("ideology")] public string Ideology { get; init; } = "";
    [JsonPropertyName("territory")] public string Territory { get; init; } = "";
    [JsonPropertyName("leadership")] public string Leadership { get; init; } = "";
    [JsonPropertyName("methods")] public List<string> Methods { get; init; } = [];
    [JsonPropertyName("resources")] public List<string> Resources { get; init; } = [];
    [JsonPropertyName("goals")] public List<string> Goals { get; init; } = [];
    [JsonPropertyName("relationships")] public List<FactionRelationship> Relationships { get; init; } = [];
    [JsonPropertyName("narrative_function")] public string NarrativeFunction { get; init; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; init; } = [];
}

public record FactionRelationship
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("type")] public string Type { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
}
