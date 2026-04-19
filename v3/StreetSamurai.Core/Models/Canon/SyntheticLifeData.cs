using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A synthetic life form — any non-biological sentient or semi-sentient entity.
/// Covers the full spectrum: Superminds (corporate AIs), Rogue AIs (Fragments to Leviathans),
/// E.L.F.s (Electronic Life Forms — paratechnological digital spirits),
/// and Ceramic Men (living gas contained in a porcelain humanoid vessel).
/// </summary>
public class SyntheticLifeData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
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
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];

    // ── Ceramic Man fields (only set when type == "ceramic_man") ──────────────
    [JsonPropertyName("known_age")] public string? KnownAge { get; set; }
    [JsonPropertyName("crack_pattern")] public string? CrackPattern { get; set; }
    [JsonPropertyName("current_role")] public string? CurrentRole { get; set; }
    [JsonPropertyName("known_location")] public string? KnownLocation { get; set; }
    [JsonPropertyName("diplomatic_specialty")] public string? DiplomaticSpecialty { get; set; }
    [JsonPropertyName("operating_history")] public string? OperatingHistory { get; set; }
    [JsonPropertyName("behavioral_notes")] public string? BehavioralNotes { get; set; }
    [JsonPropertyName("known_associations")] public List<string>? KnownAssociations { get; set; }
    [JsonPropertyName("damage_history")] public string? DamageHistory { get; set; }
    /// <summary>Pigment, inlay, or applied marking on the face — the Ceramic Man's primary individuation method.</summary>
    [JsonPropertyName("face_decoration")] public string? FaceDecoration { get; set; }
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}
