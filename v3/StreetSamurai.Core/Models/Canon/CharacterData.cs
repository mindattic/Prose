using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Strongly-typed character model matching the actual YAML structure.
/// Serialized/deserialized as JSON — no regex, no guessing.
/// </summary>
public class CharacterData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
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
    /// <summary>How this character's POV narration sounds — prose style, interior voice, what they notice first.</summary>
    [JsonPropertyName("narration_voice")] public string NarrationVoice { get; set; } = "";
    /// <summary>Numeric capability stats (1-10 scale).</summary>
    [JsonPropertyName("stats")] public CharacterStats Stats { get; set; } = new();
    /// <summary>Concrete behavioral patterns — how this character acts in specific situations.</summary>
    [JsonPropertyName("behavioral")] public CharacterBehavioral Behavioral { get; set; } = new();
    /// <summary>Installed cyberware/augmentations with body location and status.</summary>
    [JsonPropertyName("cyberware_inventory")] public List<CyberwareEntry> CyberwareInventory { get; set; } = [];
    /// <summary>
    /// What this character owns, uses, wears, drives, drinks, and carries.
    /// Cross-references other repos by name — the character's vehicle points to a
    /// transportation entry, their weapon to weaponry, their drink to consumer goods.
    /// This is how the story engine knows Kyle drives a specific motorcycle and
    /// Sable drinks a specific brand of synth-coffee.
    /// </summary>
    [JsonPropertyName("belongings")] public CharacterBelongings Belongings { get; set; } = new();
    /// <summary>Archetype scores — behavioral patterns this character exhibits (0.0-1.0 each).</summary>
    [JsonPropertyName("archetypes")] public Dictionary<string, double> Archetypes { get; set; } = new();
    /// <summary>Where this operator works — their home turf and surrounding areas they know well.</summary>
    [JsonPropertyName("operating_territory")] public OperatingTerritory Territory { get; set; } = new();
    /// <summary>Chronological timeline of events that have happened to this character across all stories.</summary>
    [JsonPropertyName("timeline")] public List<TimelineEvent> Timeline { get; set; } = [];
    /// <summary>
    /// Canonical data changelog — tracks significant changes to this character's
    /// permanent record (affiliation shifts, status changes, injuries, transformations).
    /// Only story-driven changes that alter who the character IS, not ephemeral scene state.
    /// Empty until stories produce permanent consequences.
    /// </summary>
    [JsonPropertyName("changelog")] public List<CharacterChangelog> Changelog { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    /// <summary>FBI/NCIC-style physical description — height, build, hair, eyes, scars, augmentations.</summary>
    [JsonPropertyName("physical_description")] public PhysicalDescription PhysicalDescription { get; set; } = new();
    /// <summary>Midjourney-ready image generation prompt derived from the physical description.</summary>
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    /// <summary>DALL-E 3 image generation prompt — plain English, no Midjourney params.</summary>
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
    /// <summary>Genetic ancestry — what a 23andMe test would show. Percentages by region. Independent of surname.</summary>
    [JsonPropertyName("genetic_ancestry")] public Dictionary<string, double> GeneticAncestry { get; set; } = new();
    /// <summary>Three-tier ancestry detail: region → sub-region → nationality with percentages.</summary>
    [JsonPropertyName("ancestry_detail")] public Dictionary<string, Dictionary<string, Dictionary<string, double>>> AncestryDetail { get; set; } = new();
    /// <summary>Active and passive BCI abilities powered by the bio-battery system.</summary>
    [JsonPropertyName("neural_abilities")] public List<NeuralAbilityDefinition> NeuralAbilities { get; set; } = [];
    /// <summary>Bio-battery system parameters — caloric conversion, depletion thresholds, recovery rules.</summary>
    [JsonPropertyName("bio_battery")] public BioBatteryDefinition? BioBattery { get; set; }
    /// <summary>Things the character knows and when they learned them. Drives knowledge-gating in dossier checks.</summary>
    [JsonPropertyName("knowledge")] public List<CharacterKnowledge> Knowledge { get; set; } = [];
    /// <summary>Medical/mental conditions, addictions, allergies. Anything that changes how the body acts under stress.</summary>
    [JsonPropertyName("conditions")] public List<CharacterCondition> Conditions { get; set; } = [];
}

/// <summary>
/// A discrete fact the character knows. Sourced from a chapter beat or an entity record.
/// Lets the precheck answer "could Sasha react to X here? — only if Knowledge contains X."
/// </summary>
public class CharacterKnowledge
{
    [JsonPropertyName("topic")] public string Topic { get; set; } = "";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("learned_chapter")] public int? LearnedChapter { get; set; }
    [JsonPropertyName("learned_chapter_id")] public string? LearnedChapterId { get; set; }
    [JsonPropertyName("source_beat")] public int? SourceBeat { get; set; }
    [JsonPropertyName("source_snippet")] public string? SourceSnippet { get; set; }
    /// <summary>Entity ids this knowledge concerns — so dossier expansion can pull related cards.</summary>
    [JsonPropertyName("entities")] public List<string> Entities { get; set; } = [];
}

/// <summary>
/// A persistent condition affecting the character — addiction, allergy, prescription
/// dependency, chronic illness, mental health diagnosis. Severity drives prose tone;
/// since/until bound the period of effect.
/// </summary>
public class CharacterCondition
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = ""; // addiction | allergy | prescription | chronic | mental | injury
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("severity")] public string Severity { get; set; } = ""; // mild | moderate | severe | acute
    [JsonPropertyName("notes")] public string Notes { get; set; } = "";
    [JsonPropertyName("since_chapter")] public int? SinceChapter { get; set; }
    [JsonPropertyName("until_chapter")] public int? UntilChapter { get; set; }
}

/// <summary>
/// A BCI ability powered by the bio-battery. Passive abilities run continuously at low cost.
/// Active abilities are triggered and cost a fixed percentage per use.
/// </summary>
public class NeuralAbilityDefinition
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("cost_percent")] public int CostPercent { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("overdrawn_risk")] public string OverdrawnRisk { get; set; } = "";
    /// <summary>True = runs automatically when battery allows. False = deliberately triggered.</summary>
    [JsonPropertyName("passive")] public bool Passive { get; set; }
}

/// <summary>
/// Bio-battery system parameters. The battery converts calories to electrical energy.
/// What was eaten before a fight sets the ceiling — there is no refueling mid-combat.
/// </summary>
public class BioBatteryDefinition
{
    /// <summary>How food choices translate to starting charge percentage.</summary>
    [JsonPropertyName("max_capacity_description")] public string MaxCapacityDescription { get; set; } = "";
    /// <summary>Percent threshold → consequence description. Keys: "60", "40", "20", "10", "0".</summary>
    [JsonPropertyName("depletion_thresholds")] public Dictionary<string, string> DepletionThresholds { get; set; } = new();
    [JsonPropertyName("recovery")] public string Recovery { get; set; } = "";
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
/// What a character owns, uses, and is associated with. Cross-references other repos.
/// Each field is a name that matches an entry in the corresponding repo.
/// Empty strings mean unspecified — the story engine can assign or the user can fill in.
/// </summary>
public class CharacterBelongings
{
    [JsonPropertyName("primary_weapon")] public string PrimaryWeapon { get; set; } = "";
    [JsonPropertyName("secondary_weapon")] public string SecondaryWeapon { get; set; } = "";
    [JsonPropertyName("armor")] public string Armor { get; set; } = "";
    [JsonPropertyName("vehicle")] public string Vehicle { get; set; } = "";
    [JsonPropertyName("residence")] public string Residence { get; set; } = "";
    [JsonPropertyName("clothing_style")] public string ClothingStyle { get; set; } = "";
    [JsonPropertyName("favorite_drink")] public string FavoriteDrink { get; set; } = "";
    [JsonPropertyName("favorite_food")] public string FavoriteFood { get; set; } = "";
    [JsonPropertyName("stimulant")] public string Stimulant { get; set; } = "";
    [JsonPropertyName("comm_device")] public string CommDevice { get; set; } = "";
    [JsonPropertyName("signature_gear")] public List<string> SignatureGear { get; set; } = [];
    [JsonPropertyName("pharmaceuticals")] public List<string> Pharmaceuticals { get; set; } = [];
    [JsonPropertyName("other")] public Dictionary<string, string> Other { get; set; } = new();
}

/// <summary>
/// A permanent change to a character's canonical data, driven by story events.
/// <summary>
/// Where a runner operates. Home turf is where they know every alley, every blind spot,
/// every E.L.F., every fixer. Nearby zones are familiar but not home. Beyond that is
/// foreign territory where they're at a disadvantage. Reputation varies by zone.
/// </summary>
public class OperatingTerritory
{
    /// <summary>Primary base of operations — the place they know best.</summary>
    [JsonPropertyName("home_turf")] public string HomeTurf { get; set; } = "";
    /// <summary>Adjacent zones they regularly operate in (2-5 locations).</summary>
    [JsonPropertyName("familiar_zones")] public List<string> FamiliarZones { get; set; } = [];
    /// <summary>Reputation in different zones: zone name -> reputation description.</summary>
    [JsonPropertyName("zone_reputation")] public Dictionary<string, string> ZoneReputation { get; set; } = new();
    /// <summary>Zones they avoid and why.</summary>
    [JsonPropertyName("no_go_zones")] public List<string> NoGoZones { get; set; } = [];
    /// <summary>How far they'll travel for a contract — local, regional, continental, global.</summary>
    [JsonPropertyName("range")] public string Range { get; set; } = "local";
}

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

/// <summary>
/// Tolerates a stats sub-field that is a non-object JSON value (string, array, number)
/// by returning an empty dictionary instead of throwing.
/// </summary>
public class FlexibleDictConverter : JsonConverter<Dictionary<string, JsonElement>>
{
    public override Dictionary<string, JsonElement>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
        reader.Skip();
        return new Dictionary<string, JsonElement>();
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, JsonElement> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}

public class CharacterStats
{
    [JsonPropertyName("physical"), JsonConverter(typeof(FlexibleDictConverter))]
    public Dictionary<string, JsonElement> Physical { get; set; } = new();
    [JsonPropertyName("mental"), JsonConverter(typeof(FlexibleDictConverter))]
    public Dictionary<string, JsonElement> Mental { get; set; } = new();
    [JsonPropertyName("social"), JsonConverter(typeof(FlexibleDictConverter))]
    public Dictionary<string, JsonElement> Social { get; set; } = new();
    [JsonPropertyName("personality"), JsonConverter(typeof(FlexibleDictConverter))]
    public Dictionary<string, JsonElement> Personality { get; set; } = new();
    [JsonPropertyName("drives")] public List<string> Drives { get; set; } = [];
    [JsonPropertyName("thresholds"), JsonConverter(typeof(FlexibleDictConverter))]
    public Dictionary<string, JsonElement> Thresholds { get; set; } = new();
    [JsonPropertyName("strengths")] public List<string> Strengths { get; set; } = [];
    [JsonPropertyName("weaknesses")] public List<string> Weaknesses { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> StatTags { get; set; } = [];
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
    [JsonPropertyName("core_fears")] public List<string> CoreFears { get; set; } = [];
    [JsonPropertyName("core_desires")] public List<string> CoreDesires { get; set; } = [];
    [JsonPropertyName("coping_mechanisms")] public List<string> CopingMechanisms { get; set; } = [];
    [JsonPropertyName("blind_spots")] public List<string> BlindSpots { get; set; } = [];
    [JsonPropertyName("secret")] public string Secret { get; set; } = "";
}

public class SpeechPatterns
{
    [JsonPropertyName("vocabulary")] public string Vocabulary { get; set; } = "";
    [JsonPropertyName("cadence")] public string Cadence { get; set; } = "";
    [JsonPropertyName("verbal_tics")] public List<string> VerbalTics { get; set; } = [];
    [JsonPropertyName("example_lines")] public List<string> ExampleLines { get; set; } = [];
    /// <summary>Topics, words, or registers this character deflects from or never uses.</summary>
    [JsonPropertyName("avoidances")] public List<string> Avoidances { get; set; } = [];
    /// <summary>How this character says one thing while meaning another — the gap between surface and intent.</summary>
    [JsonPropertyName("subtext")] public string Subtext { get; set; } = "";
    /// <summary>Specific dialog behaviors under emotional pressure — tells, deflections, escalations.</summary>
    [JsonPropertyName("under_pressure")] public string UnderPressure { get; set; } = "";
    /// <summary>How their speech changes when they genuinely trust someone vs. performing normalcy.</summary>
    [JsonPropertyName("intimacy_register")] public string IntimacyRegister { get; set; } = "";
}

public class CharacterRelationship
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("emotional_core")] public string EmotionalCore { get; set; } = "";
    [JsonPropertyName("story_tension")] public string StoryTension { get; set; } = "";
    /// <summary>Current state: active | dating | engaged | married | divorced | estranged | deceased | severed.</summary>
    [JsonPropertyName("status")] public string Status { get; set; } = "active";
    /// <summary>Chapter number where this relationship became valid in story time.</summary>
    [JsonPropertyName("since_chapter")] public int? SinceChapter { get; set; }
    /// <summary>Chapter number where this relationship ended, if applicable.</summary>
    [JsonPropertyName("until_chapter")] public int? UntilChapter { get; set; }
}

/// <summary>
/// FBI/NCIC-inspired physical description standard adapted for neo-noir characters.
/// </summary>
public class PhysicalDescription
{
    [JsonPropertyName("heritage")] public string Heritage { get; set; } = "";
    [JsonPropertyName("height_cm")] public int HeightCm { get; set; }
    [JsonPropertyName("weight_kg")] public int WeightKg { get; set; }
    [JsonPropertyName("build")] public string Build { get; set; } = "";
    [JsonPropertyName("hair_color")] public string HairColor { get; set; } = "";
    [JsonPropertyName("hair_style")] public string HairStyle { get; set; } = "";
    [JsonPropertyName("hair_length")] public string HairLength { get; set; } = "";
    [JsonPropertyName("eye_color")] public string EyeColor { get; set; } = "";
    [JsonPropertyName("skin_tone")] public string SkinTone { get; set; } = "";
    [JsonPropertyName("complexion")] public string Complexion { get; set; } = "";
    [JsonPropertyName("distinguishing_marks")] public List<string> DistinguishingMarks { get; set; } = [];
    [JsonPropertyName("visible_augmentations")] public string VisibleAugmentations { get; set; } = "";
    [JsonPropertyName("posture_movement")] public string PostureMovement { get; set; } = "";
    [JsonPropertyName("clothing_style")] public string ClothingStyle { get; set; } = "";
}
