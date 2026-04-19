using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed facet definition. One per psychological voice.
/// </summary>
public class FacetData : IWorldRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("domain")] public string Domain { get; set; } = "";
    [JsonPropertyName("triggers")] public List<string> Triggers { get; set; } = [];
    [JsonPropertyName("voice")] public FacetVoice Voice { get; set; } = new();
    [JsonPropertyName("core_memories")] public List<string> CoreMemories { get; set; } = [];
    [JsonPropertyName("model")] public string Model { get; set; } = Constants.Defaults.DefaultModel;
    [JsonPropertyName("temperature")] public double Temperature { get; set; } = 0.8;
    [JsonPropertyName("system_prompt")] public string SystemPrompt { get; set; } = "";
}

public class FacetVoice
{
    [JsonPropertyName("tone")] public string Tone { get; set; } = "";
    [JsonPropertyName("style")] public string Style { get; set; } = "";
    [JsonPropertyName("prohibitions")] public List<string> Prohibitions { get; set; } = [];
}
