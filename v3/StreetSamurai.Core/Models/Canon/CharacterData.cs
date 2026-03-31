using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed character model matching the actual YAML structure.
/// Serialized/deserialized as JSON — no regex, no guessing.
/// </summary>
public class CharacterData
{
    [JsonPropertyName("type")] public string Type { get; set; } = "character";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("age")] public int Age { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "alive";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("psychology")] public CharacterPsychology Psychology { get; set; } = new();
    [JsonPropertyName("speech_patterns")] public SpeechPatterns SpeechPatterns { get; set; } = new();
    [JsonPropertyName("relationships")] public List<CharacterRelationship> Relationships { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("narrative_function")] public string NarrativeFunction { get; set; } = "";
    [JsonPropertyName("augmentations")] public string Augmentations { get; set; } = "";
    [JsonPropertyName("daily_life")] public string DailyLife { get; set; } = "";
    [JsonPropertyName("affiliation")] public string Affiliation { get; set; } = "";
    /// <summary>Whether this character uses the facet tag system in narration (only Kyle).</summary>
    [JsonPropertyName("uses_facets")] public bool UsesFacets { get; set; }
    /// <summary>How this character's POV narration sounds — prose style, interior voice, what they notice first.</summary>
    [JsonPropertyName("narration_voice")] public string NarrationVoice { get; set; } = "";
}

public class CharacterPsychology
{
    [JsonPropertyName("facet_weights")] public FacetWeights FacetWeights { get; set; } = new();
    [JsonPropertyName("core_fears")] public List<string> CoreFears { get; set; } = [];
    [JsonPropertyName("core_desires")] public List<string> CoreDesires { get; set; } = [];
    [JsonPropertyName("coping_mechanisms")] public List<string> CopingMechanisms { get; set; } = [];
    [JsonPropertyName("blind_spots")] public List<string> BlindSpots { get; set; } = [];
    [JsonPropertyName("secret")] public string Secret { get; set; } = "";
}

public class FacetWeights
{
    [JsonPropertyName("wound")] public double Wound { get; set; }
    [JsonPropertyName("ideal")] public double Ideal { get; set; }
    [JsonPropertyName("id")] public double Id { get; set; }
    [JsonPropertyName("shadow")] public double Shadow { get; set; }
    [JsonPropertyName("mask")] public double Mask { get; set; }
    [JsonPropertyName("ghost")] public double Ghost { get; set; }
}

public class SpeechPatterns
{
    [JsonPropertyName("vocabulary")] public string Vocabulary { get; set; } = "";
    [JsonPropertyName("cadence")] public string Cadence { get; set; } = "";
    [JsonPropertyName("verbal_tics")] public List<string> VerbalTics { get; set; } = [];
    [JsonPropertyName("example_lines")] public List<string> ExampleLines { get; set; } = [];
}

public class CharacterRelationship
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("emotional_core")] public string EmotionalCore { get; set; } = "";
    [JsonPropertyName("story_tension")] public string StoryTension { get; set; } = "";
}
