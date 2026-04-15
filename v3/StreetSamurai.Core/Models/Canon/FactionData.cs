using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed faction model.
/// </summary>
public class FactionData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
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
    [JsonPropertyName("known_members")] public List<FactionMember> KnownMembers { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}

public class FactionMember
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "active";
    [JsonPropertyName("notes")] public string Notes { get; set; } = "";
}

public class FactionRelationship
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
