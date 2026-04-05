using System.Text.Json;
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
    /// <summary>Species classification: human, ai, android, robot, cyborg, synthetic, hybrid, unknown.</summary>
    [JsonPropertyName("species")] public string Species { get; set; } = "human";
    [JsonPropertyName("gender")] public string Gender { get; set; } = "";
    [JsonPropertyName("pronouns")] public string Pronouns { get; set; } = "";
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
    /// <summary>Numeric capability stats (1-10 scale).</summary>
    [JsonPropertyName("stats")] public CharacterStats Stats { get; set; } = new();
    /// <summary>Concrete behavioral patterns — how this character acts in specific situations.</summary>
    [JsonPropertyName("behavioral")] public CharacterBehavioral Behavioral { get; set; } = new();
    /// <summary>Installed cyberware/augmentations with body location and status.</summary>
    [JsonPropertyName("cyberware_inventory")] public List<CyberwareEntry> CyberwareInventory { get; set; } = [];
    /// <summary>Chronological timeline of events that have happened to this character across all stories.</summary>
    [JsonPropertyName("timeline")] public List<TimelineEvent> Timeline { get; set; } = [];
    /// <summary>
    /// Canonical data changelog — tracks significant changes to this character's
    /// permanent record (affiliation shifts, status changes, injuries, transformations).
    /// Only story-driven changes that alter who the character IS, not ephemeral scene state.
    /// Empty until stories produce permanent consequences.
    /// </summary>
    [JsonPropertyName("changelog")] public List<CharacterChangelog> Changelog { get; set; } = [];
}

/// <summary>
/// A specific piece of installed cyberware. Tracks body location, manufacturer,
/// condition, and when it was installed. If a character loses a flesh arm in
/// story 3 and gets a cybernetic replacement, the timeline reflects this and
/// future stories know they have a chrome arm, not flesh.
/// </summary>
public class CyberwareEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("body_location")] public string BodyLocation { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("tier")] public string Tier { get; set; } = "";
    [JsonPropertyName("condition")] public string Condition { get; set; } = "functional";
    [JsonPropertyName("installed_date")] public string InstalledDate { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("replaces")] public string Replaces { get; set; } = "";
}

/// <summary>
/// A timestamped event in a character's life. These accumulate across stories
/// and form the character's personal history. The system checks this timeline
/// before writing to prevent continuity errors (dead characters acting,
/// lost limbs reappearing, forgotten injuries).
/// </summary>
public class TimelineEvent
{
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("story_id")] public string StoryId { get; set; } = "";
    [JsonPropertyName("event")] public string Event { get; set; } = "";
    [JsonPropertyName("consequences")] public string Consequences { get; set; } = "";
    [JsonPropertyName("body_changes")] public List<string> BodyChanges { get; set; } = [];
    [JsonPropertyName("status_change")] public string StatusChange { get; set; } = "";
}

/// <summary>
/// A permanent change to a character's canonical data, driven by story events.
/// "Sable's affiliation changed from Iron Lotus to Independent after story_042:beat_7
/// because she burned her handler." This is the narrative version history.
/// </summary>
public class CharacterChangelog
{
    [JsonPropertyName("story_id")] public string StoryId { get; set; } = "";
    [JsonPropertyName("beat")] public string Beat { get; set; } = "";
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("field")] public string Field { get; set; } = "";
    [JsonPropertyName("from")] public string From { get; set; } = "";
    [JsonPropertyName("to")] public string To { get; set; } = "";
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
}

public class CharacterStats
{
    [JsonPropertyName("physical")] public Dictionary<string, JsonElement> Physical { get; set; } = new();
    [JsonPropertyName("mental")] public Dictionary<string, JsonElement> Mental { get; set; } = new();
    [JsonPropertyName("social")] public Dictionary<string, JsonElement> Social { get; set; } = new();
    [JsonPropertyName("personality")] public Dictionary<string, JsonElement> Personality { get; set; } = new();
    [JsonPropertyName("drives")] public List<string> Drives { get; set; } = [];
    [JsonPropertyName("thresholds")] public Dictionary<string, JsonElement> Thresholds { get; set; } = new();
    [JsonPropertyName("strengths")] public List<string> Strengths { get; set; } = [];
    [JsonPropertyName("weaknesses")] public List<string> Weaknesses { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

/// <summary>
/// Concrete behavioral patterns that tell the LLM exactly how a character acts.
/// Abstract stats say "integrity: 8" — behavioral rules say "will abandon a contract
/// to protect a child, no exceptions."
/// </summary>
public class CharacterBehavioral
{
    /// <summary>Hard rules for decision-making. "Will always X", "Will never Y".</summary>
    [JsonPropertyName("decision_rules")] public List<string> DecisionRules { get; set; } = [];
    /// <summary>How the character escalates from observation to lethal force.</summary>
    [JsonPropertyName("escalation_ladder")] public List<string> EscalationLadder { get; set; } = [];
    /// <summary>How the character behaves differently with specific people.</summary>
    [JsonPropertyName("interpersonal_modes")] public Dictionary<string, string> InterpersonalModes { get; set; } = new();
    /// <summary>What happens at each stress level.</summary>
    [JsonPropertyName("stress_responses")] public Dictionary<string, string> StressResponses { get; set; } = new();
    /// <summary>What the character does when internal values conflict.</summary>
    [JsonPropertyName("contradictions")] public List<string> Contradictions { get; set; } = [];
    /// <summary>Habitual actions in common situations.</summary>
    [JsonPropertyName("habits")] public List<string> Habits { get; set; } = [];
    /// <summary>What triggers this character to break their own rules.</summary>
    [JsonPropertyName("breaking_points")] public List<string> BreakingPoints { get; set; } = [];
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
