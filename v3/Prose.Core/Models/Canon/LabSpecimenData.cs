using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Models.Canon;

/// <summary>
/// A lab specimen — any entity produced by unethical experimentation that has escaped
/// or been released into the dark places of GLMZ. Not evil, merely broken and dangerous.
/// Covers biological fusions, runaway geneware, nano-contamination, biomass collectives,
/// cognitive corruption outcomes, and hybrid bio-synthetic horrors. Eradicated on sight
/// by Dreadnaught Pacification Squads if found in public.
/// </summary>
public class LabSpecimenData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "lab_specimen";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("classification")] public string Classification { get; set; } = "";
    [JsonPropertyName("origin_lab")] public string OriginLab { get; set; } = "";
    [JsonPropertyName("origin_method")] public string OriginMethod { get; set; } = "";
    [JsonPropertyName("substrate")] public string Substrate { get; set; } = "";
    [JsonPropertyName("physical_description")] public string PhysicalDescription { get; set; } = "";
    [JsonPropertyName("behavioral_profile")] public string BehavioralProfile { get; set; } = "";
    [JsonPropertyName("threat_level")] public string ThreatLevel { get; set; } = "";
    [JsonPropertyName("containment_status")] public string ContainmentStatus { get; set; } = "";
    [JsonPropertyName("known_locations")] public List<string> KnownLocations { get; set; } = [];
    [JsonPropertyName("contamination_risk")] public string ContaminationRisk { get; set; } = "";
    [JsonPropertyName("pacification_protocol")] public string PacificationProtocol { get; set; } = "";
    [JsonPropertyName("pitiable_qualities")] public string PitiableQualities { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
