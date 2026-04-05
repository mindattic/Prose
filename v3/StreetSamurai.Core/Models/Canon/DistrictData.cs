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
    [JsonPropertyName("coordinates")] public GeoCoordinates Coordinates { get; set; } = new();
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

public class GeoCoordinates
{
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lng")] public double Lng { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

public class AtmosphereData
{
    [JsonPropertyName("sights")] public List<string> Sights { get; set; } = [];
    [JsonPropertyName("sounds")] public List<string> Sounds { get; set; } = [];
    [JsonPropertyName("smells")] public List<string> Smells { get; set; } = [];
    [JsonPropertyName("feel")] public string Feel { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

public class DistrictConnections
{
    [JsonPropertyName("adjacent_to")] public List<string> AdjacentTo { get; set; } = [];

    /// <summary>Directional exits — Zork-style. Each exit has a direction, destination, and description of the passage.</summary>
    [JsonPropertyName("exits")] public List<PlaceExit> Exits { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

/// <summary>
/// A directional exit from a place. Not just a link — each exit is unique.
/// Some are open roads, others are guarded checkpoints, tunnels, maglev stations,
/// waterways, or maintenance corridors. The exit description affects how characters
/// experience the transition and what dangers they face.
/// </summary>
public class PlaceExit
{
    [JsonPropertyName("direction")] public string Direction { get; set; } = "";
    [JsonPropertyName("destination")] public string Destination { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "road";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("restricted")] public bool Restricted { get; set; }
    [JsonPropertyName("danger_level")] public int DangerLevel { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

public class NotableLocation
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
