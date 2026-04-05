using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A synthetic life form — any non-biological sentient or semi-sentient entity.
/// Covers the full spectrum: Superminds (corporate AIs), Rogue AIs (Fragments to Leviathans),
/// and E.L.F.s (Electronic Life Forms — paratechnological digital spirits).
/// </summary>
public class SyntheticLifeData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "elf";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("classification")] public string Classification { get; set; } = "";
    [JsonPropertyName("disposition")] public string Disposition { get; set; } = "";
    [JsonPropertyName("habitat")] public string Habitat { get; set; } = "";
    [JsonPropertyName("origin")] public string Origin { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "active";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("observed_behavior")] public string ObservedBehavior { get; set; } = "";
    [JsonPropertyName("encounter_frequency")] public string EncounterFrequency { get; set; } = "";
    [JsonPropertyName("confirmed_sightings")] public int ConfirmedSightings { get; set; }
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("dti_rating")] public double DtiRating { get; set; }
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("paratechnological")] public bool Paratechnological { get; set; }
}
