using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed character model matching the actual YAML structure.
/// Serialized/deserialized as JSON — no regex, no guessing.
/// </summary>
public record CharacterData
{
    [JsonPropertyName("type")] public string Type { get; init; } = "character";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; init; } = [];
    [JsonPropertyName("role")] public string Role { get; init; } = "";
    [JsonPropertyName("age")] public int Age { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = "alive";
    [JsonPropertyName("location")] public string Location { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("psychology")] public CharacterPsychology Psychology { get; init; } = new();
    [JsonPropertyName("speech_patterns")] public SpeechPatterns SpeechPatterns { get; init; } = new();
    [JsonPropertyName("relationships")] public List<CharacterRelationship> Relationships { get; init; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; init; } = [];
    [JsonPropertyName("narrative_function")] public string NarrativeFunction { get; init; } = "";
    [JsonPropertyName("augmentations")] public string Augmentations { get; init; } = "";
    [JsonPropertyName("daily_life")] public string DailyLife { get; init; } = "";
    [JsonPropertyName("affiliation")] public string Affiliation { get; init; } = "";
}

public record CharacterPsychology
{
    [JsonPropertyName("facet_weights")] public FacetWeights FacetWeights { get; init; } = new();
    [JsonPropertyName("core_fears")] public List<string> CoreFears { get; init; } = [];
    [JsonPropertyName("core_desires")] public List<string> CoreDesires { get; init; } = [];
    [JsonPropertyName("coping_mechanisms")] public List<string> CopingMechanisms { get; init; } = [];
    [JsonPropertyName("blind_spots")] public List<string> BlindSpots { get; init; } = [];
    [JsonPropertyName("secret")] public string Secret { get; init; } = "";
}

public record FacetWeights
{
    [JsonPropertyName("wound")] public double Wound { get; init; }
    [JsonPropertyName("ideal")] public double Ideal { get; init; }
    [JsonPropertyName("id")] public double Id { get; init; }
    [JsonPropertyName("shadow")] public double Shadow { get; init; }
    [JsonPropertyName("mask")] public double Mask { get; init; }
    [JsonPropertyName("ghost")] public double Ghost { get; init; }
}

public record SpeechPatterns
{
    [JsonPropertyName("vocabulary")] public string Vocabulary { get; init; } = "";
    [JsonPropertyName("cadence")] public string Cadence { get; init; } = "";
    [JsonPropertyName("verbal_tics")] public List<string> VerbalTics { get; init; } = [];
    [JsonPropertyName("example_lines")] public List<string> ExampleLines { get; init; } = [];
}

public record CharacterRelationship
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("type")] public string Type { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("emotional_core")] public string EmotionalCore { get; init; } = "";
    [JsonPropertyName("story_tension")] public string StoryTension { get; init; } = "";
}
