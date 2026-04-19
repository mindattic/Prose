using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A psionic enhancement — what happens when intuition and deja vu become
/// cybernetically and chemically amplified past the point of human baseline.
/// Some are genuine breakthroughs. Some are expensive psychoses.
/// All of them are being studied by someone who wants to bottle them.
/// The question of whether this is the next stage of human evolution
/// or simply another way to ruin a person for profit has not been settled.
/// </summary>
public class PsionicData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "psionic";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("classification")] public string Classification { get; set; } = "";
    [JsonPropertyName("enhancement_type")] public string EnhancementType { get; set; } = "";
    [JsonPropertyName("mechanism")] public string Mechanism { get; set; } = "";
    [JsonPropertyName("abilities")] public string Abilities { get; set; } = "";
    [JsonPropertyName("side_effects")] public string SideEffects { get; set; } = "";
    [JsonPropertyName("acquisition_method")] public string AcquisitionMethod { get; set; } = "";
    [JsonPropertyName("detection_risk")] public string DetectionRisk { get; set; } = "";
    [JsonPropertyName("corporate_interest")] public string CorporateInterest { get; set; } = "";
    [JsonPropertyName("known_practitioners")] public List<string> KnownPractitioners { get; set; } = [];
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
