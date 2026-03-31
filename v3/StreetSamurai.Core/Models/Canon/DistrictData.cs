using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed district/place model.
/// </summary>
public class DistrictData
{
    [JsonPropertyName("type")] public string Type { get; set; } = "place";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("atmosphere")] public AtmosphereData Atmosphere { get; set; } = new();
    [JsonPropertyName("demographics")] public string Demographics { get; set; } = "";
    [JsonPropertyName("economy")] public string Economy { get; set; } = "";
    [JsonPropertyName("power_structure")] public string PowerStructure { get; set; } = "";
    [JsonPropertyName("dangers")] public List<string> Dangers { get; set; } = [];
    [JsonPropertyName("opportunities")] public List<string> Opportunities { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("connections")] public DistrictConnections Connections { get; set; } = new();
    [JsonPropertyName("frequented_by")] public List<string> FrequentedBy { get; set; } = [];
    [JsonPropertyName("notable_locations")] public List<NotableLocation> NotableLocations { get; set; } = [];
}

public class AtmosphereData
{
    [JsonPropertyName("sights")] public List<string> Sights { get; set; } = [];
    [JsonPropertyName("sounds")] public List<string> Sounds { get; set; } = [];
    [JsonPropertyName("smells")] public List<string> Smells { get; set; } = [];
    [JsonPropertyName("feel")] public string Feel { get; set; } = "";
}

public class DistrictConnections
{
    [JsonPropertyName("adjacent_to")] public List<string> AdjacentTo { get; set; } = [];
}

public class NotableLocation
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}
