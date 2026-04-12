using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// A Ceramic Man — an inorganic sentient entity of unknown origin. Bipedal, sexless,
/// smooth white porcelain-like exterior, all sharing the same base form as if cast from
/// a single mold. Face is a white mask, often chipped or cracked but never punctured
/// (puncture = death; their essence is a pressurized gas within the skull). Brittle by
/// nature and therefore consummate diplomats. Potentially immortal — no natural deaths
/// documented. Origins are unknown or closely guarded.
/// </summary>
public class CeramicManData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "ceramic_man";
    [JsonPropertyName("aliases")] public List<string> Aliases { get; set; } = [];
    /// <summary>Estimated age or period of observed activity — never confirmed, often contested.</summary>
    [JsonPropertyName("known_age")] public string KnownAge { get; set; } = "";
    /// <summary>The specific pattern of chips, cracks, and wear on this individual — their only distinguishing physical feature.</summary>
    [JsonPropertyName("crack_pattern")] public string CrackPattern { get; set; } = "";
    [JsonPropertyName("current_role")] public string CurrentRole { get; set; } = "";
    [JsonPropertyName("known_location")] public string KnownLocation { get; set; } = "";
    /// <summary>What negotiations, disputes, or domains they specialize in.</summary>
    [JsonPropertyName("diplomatic_specialty")] public string DiplomaticSpecialty { get; set; } = "";
    /// <summary>What they have been observed doing and how over the years.</summary>
    [JsonPropertyName("operating_history")] public string OperatingHistory { get; set; } = "";
    /// <summary>How this individual speaks and conducts itself — each Ceramic Man's affect is distinct even if their face is not.</summary>
    [JsonPropertyName("behavioral_notes")] public string BehavioralNotes { get; set; } = "";
    /// <summary>Known associations with factions, corponations, or individuals — always neutral on record.</summary>
    [JsonPropertyName("known_associations")] public List<string> KnownAssociations { get; set; } = [];
    /// <summary>How this individual's damage was incurred — each crack has a history.</summary>
    [JsonPropertyName("damage_history")] public string DamageHistory { get; set; } = "";
    [JsonPropertyName("story_hooks")] public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
