using System.Text.Json;
using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Models.Canon;

/// <summary>
/// A synthetic life form in the GLMZ world — ELFs (Electronic Life Forms),
/// rogue AI instances, firmware-evolved entities, and other non-biological
/// intelligences that are catalogued but not fully understood.
/// </summary>
public class SyntheticLifeData : ICanonEntity
{
    [JsonPropertyName("id")]                 public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")]             public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")]         public int VoteCount { get; set; } = 0;
    [JsonPropertyName("name")]               public string Name { get; set; } = "";
    [JsonPropertyName("type")]               public string Type { get; set; } = "synthetic";
    [JsonPropertyName("aliases")]            public List<string> Aliases { get; set; } = [];
    [JsonPropertyName("kind_of_being")]      public string KindOfBeing { get; set; } = "";
    [JsonPropertyName("manufacturer")]       public string Manufacturer { get; set; } = "";
    [JsonPropertyName("tier")]               public string Tier { get; set; } = "";
    [JsonPropertyName("classification")]     public string Classification { get; set; } = "";
    [JsonPropertyName("disposition")]        public string Disposition { get; set; } = "";
    [JsonPropertyName("habitat")]            public string Habitat { get; set; } = "";
    [JsonPropertyName("origin")]             public string Origin { get; set; } = "";
    [JsonPropertyName("status")]             public string LifeStatus { get; set; } = "";
    [JsonPropertyName("description")]        public string Description { get; set; } = "";
    [JsonPropertyName("observed_behavior")]  public string ObservedBehavior { get; set; } = "";
    [JsonPropertyName("encounter_frequency")] public string EncounterFrequency { get; set; } = "";
    [JsonPropertyName("confirmed_sightings")] public int ConfirmedSightings { get; set; }
    [JsonPropertyName("location")]           public string Location { get; set; } = "";
    [JsonPropertyName("dti_rating")]         public double DtiRating { get; set; }
    [JsonPropertyName("paratechnological")]  public bool Paratechnological { get; set; }
    [JsonPropertyName("known_age")]          public string? KnownAge { get; set; }
    [JsonPropertyName("crack_pattern")]      public string? CrackPattern { get; set; }
    [JsonPropertyName("current_role")]       public string? CurrentRole { get; set; }
    [JsonPropertyName("known_location")]     public string? KnownLocation { get; set; }
    [JsonPropertyName("diplomatic_specialty")] public string? DiplomaticSpecialty { get; set; }
    [JsonPropertyName("operating_history")]  public string? OperatingHistory { get; set; }
    [JsonPropertyName("behavioral_notes")]   public string? BehavioralNotes { get; set; }
    [JsonPropertyName("damage_history")]     public string? DamageHistory { get; set; }
    [JsonPropertyName("face_decoration")]    public string? FaceDecoration { get; set; }
    [JsonPropertyName("known_associations")] public List<string> KnownAssociations { get; set; } = [];
    [JsonPropertyName("story_hooks")]        public List<string> StoryHooks { get; set; } = [];
    [JsonPropertyName("tags")]               public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")]       public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")]      public string Dalle3Prompt { get; set; } = "";
    [JsonExtensionData]                      public Dictionary<string, JsonElement>? ExtraData { get; set; }
}
