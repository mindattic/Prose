using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

public class ToneBibleData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Neo-noir Tone Bible";
    [JsonPropertyName("tone_rules")] public List<string> ToneRules { get; set; } = [];
    [JsonPropertyName("sensory_palette")] public SensoryPalette SensoryPalette { get; set; } = new();
    [JsonPropertyName("dialogue_rules")] public List<string> DialogueRules { get; set; } = [];
    [JsonPropertyName("story_structure")] public List<string> StoryStructure { get; set; } = [];
}

public class SensoryPalette
{
    [JsonPropertyName("sights")] public List<string> Sights { get; set; } = [];
    [JsonPropertyName("sounds")] public List<string> Sounds { get; set; } = [];
    [JsonPropertyName("smells")] public List<string> Smells { get; set; } = [];
    [JsonPropertyName("textures")] public List<string> Textures { get; set; } = [];
    [JsonPropertyName("tastes")] public List<string> Tastes { get; set; } = [];
}
