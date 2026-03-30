using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Graph;

/// <summary>
/// Result of LLM entity extraction from story text.
/// Designed to map directly to WorldNode + WorldEdge creation.
/// </summary>
public record ExtractionResult
{
    [JsonPropertyName("entities")]
    public List<ExtractedEntity> Entities { get; init; } = [];

    [JsonPropertyName("relationships")]
    public List<ExtractedRelationship> Relationships { get; init; } = [];
}

public record ExtractedEntity
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; init; } = new();
}

public record ExtractedRelationship
{
    [JsonPropertyName("source")]
    public string Source { get; init; } = "";

    [JsonPropertyName("target")]
    public string Target { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("sentiment")]
    public string Sentiment { get; init; } = "neutral";
}
