using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed district/place model.
/// </summary>
public record DistrictData
{
    [JsonPropertyName("type")] public string Type { get; init; } = "place";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; init; } = [];
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("atmosphere")] public AtmosphereData Atmosphere { get; init; } = new();
    [JsonPropertyName("demographics")] public string Demographics { get; init; } = "";
    [JsonPropertyName("economy")] public string Economy { get; init; } = "";
    [JsonPropertyName("power_structure")] public string PowerStructure { get; init; } = "";
    [JsonPropertyName("dangers")] public List<string> Dangers { get; init; } = [];
    [JsonPropertyName("opportunities")] public List<string> Opportunities { get; init; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; init; } = [];
    [JsonPropertyName("connections")] public DistrictConnections Connections { get; init; } = new();
    [JsonPropertyName("frequented_by")] public List<string> FrequentedBy { get; init; } = [];
    [JsonPropertyName("notable_locations")] public List<NotableLocation> NotableLocations { get; init; } = [];
}

public record AtmosphereData
{
    [JsonPropertyName("sights")] public List<string> Sights { get; init; } = [];
    [JsonPropertyName("sounds")] public List<string> Sounds { get; init; } = [];
    [JsonPropertyName("smells")] public List<string> Smells { get; init; } = [];
    [JsonPropertyName("feel")] public string Feel { get; init; } = "";
}

public record DistrictConnections
{
    [JsonPropertyName("adjacent_to")] public List<string> AdjacentTo { get; init; } = [];
}

public record NotableLocation
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
}
