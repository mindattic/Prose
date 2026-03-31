using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed facet definition. One per psychological voice.
/// </summary>
public record FacetData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("domain")] public string Domain { get; set; } = "";
    [JsonPropertyName("triggers")] public List<string> Triggers { get; set; } = [];
    [JsonPropertyName("voice")] public FacetVoice Voice { get; set; } = new();
    [JsonPropertyName("core_memories")] public List<string> CoreMemories { get; set; } = [];
    [JsonPropertyName("model")] public string Model { get; set; } = "claude-sonnet-4-6";
    [JsonPropertyName("temperature")] public double Temperature { get; set; } = 0.8;
    [JsonPropertyName("system_prompt")] public string SystemPrompt { get; set; } = "";
}

public record FacetVoice
{
    [JsonPropertyName("tone")] public string Tone { get; set; } = "";
    [JsonPropertyName("style")] public string Style { get; set; } = "";
    [JsonPropertyName("prohibitions")] public List<string> Prohibitions { get; set; } = [];
}
