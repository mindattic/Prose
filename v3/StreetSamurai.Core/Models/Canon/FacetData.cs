using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed facet definition. One per psychological voice.
/// </summary>
public record FacetData
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("domain")] public string Domain { get; init; } = "";
    [JsonPropertyName("triggers")] public List<string> Triggers { get; init; } = [];
    [JsonPropertyName("voice")] public FacetVoice Voice { get; init; } = new();
    [JsonPropertyName("core_memories")] public List<string> CoreMemories { get; init; } = [];
    [JsonPropertyName("model")] public string Model { get; init; } = "claude-sonnet-4-6";
    [JsonPropertyName("temperature")] public double Temperature { get; init; } = 0.8;
    [JsonPropertyName("system_prompt")] public string SystemPrompt { get; init; } = "";
}

public record FacetVoice
{
    [JsonPropertyName("tone")] public string Tone { get; init; } = "";
    [JsonPropertyName("style")] public string Style { get; init; } = "";
    [JsonPropertyName("prohibitions")] public List<string> Prohibitions { get; init; } = [];
}
