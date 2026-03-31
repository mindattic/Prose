using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed faction model.
/// </summary>
public class FactionData
{
    [JsonPropertyName("type")] public string Type { get; set; } = "faction";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("motto")] public string Motto { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("ideology")] public string Ideology { get; set; } = "";
    [JsonPropertyName("territory")] public string Territory { get; set; } = "";
    [JsonPropertyName("leadership")] public string Leadership { get; set; } = "";
    [JsonPropertyName("methods")] public List<string> Methods { get; set; } = [];
    [JsonPropertyName("resources")] public List<string> Resources { get; set; } = [];
    [JsonPropertyName("goals")] public List<string> Goals { get; set; } = [];
    [JsonPropertyName("relationships")] public List<FactionRelationship> Relationships { get; set; } = [];
    [JsonPropertyName("narrative_function")] public string NarrativeFunction { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
}

public class FactionRelationship
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}
