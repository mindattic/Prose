namespace StreetSamurai.Core.Data.Entities;

// ─────────────────────────────────────────────────────────────────────────────
// Character — fully relational. Every CharacterData field that was previously
// stuffed into *Json columns is now either a real column on this table or a
// row in a bridge table. The whole application is about maintaining
// relationships, so the storage actually has to model those relationships.
//
// Layout:
//   Characters                          (this table — top-level scalars + 1:1 flat)
//   CharacterAliases                    (List<string> Aliases)
//   CharacterStoryHooks                 (List<string> StoryHooks)
//   CharacterArchetypeScores            (Dict<string,double> Archetypes)
//   CharacterGeneticAncestry            (Dict<string,double> GeneticAncestry)
//   CharacterAncestryDetail             (region → sub-region → nationality → %)
//   CharacterPsychologyTraits           (CoreFears/CoreDesires/CopingMechanisms/BlindSpots)
//   CharacterSpeechPhrases              (VerbalTics/ExampleLines/Avoidances)
//   CharacterBehavioralRules            (DecisionRules/EscalationLadder/Contradictions/Habits/BreakingPoints)
//   CharacterBehavioralMaps             (InterpersonalModes/StressResponses — Dict<string,string>)
//   CharacterStatScalars                (Stats.Physical/Mental/Social/Personality/Thresholds heterogeneous values)
//   CharacterStatPhrases                (Stats.Drives/Strengths/Weaknesses/StatTags)
//   CharacterPhysicalMarks              (PhysicalDescription.DistinguishingMarks)
//   CharacterTerritoryZones             (FamiliarZones/NoGoZones)
//   CharacterTerritoryReputations       (ZoneReputation Dict<string,string>)
//   CharacterBelongingsGear             (SignatureGear/Pharmaceuticals lists)
//   CharacterBelongingsExtras           (Belongings.Other Dict<string,string>)
//   CharacterBioBatteryThresholds       (BioBattery.DepletionThresholds Dict<string,string>)
//   CharacterNeuralAbilities            (List<NeuralAbilityDefinition>)
//   CharacterChangelog                  (List<CharacterChangelog>)
//   CharacterCyberware                  (already existed — kept)
//   CharacterKnowledge                  (already existed — KnowledgeEntities now its own bridge instead of EntitiesJson)
//   CharacterKnowledgeEntities          (CharacterKnowledge.Entities List<string>)
//   CharacterConditions                 (already existed — kept)
//   CharacterRelationships              (already existed — kept)
//   CharacterTimeline                   (already existed — TimelineBodyChanges now its own bridge instead of BodyChangesJson)
//   CharacterTimelineBodyChanges        (CharacterTimelineEvent.BodyChanges List<string>)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Top-level character row. Scalars from CharacterData + flattened 1:1 sub-objects
/// (Belongings scalar parts, Territory scalar parts, PhysicalDescription scalar
/// parts, Psychology.Secret, SpeechPatterns scalars, BioBattery scalars). Lists
/// and dictionaries live in their own bridge tables — no JSON columns.
/// </summary>
public class Character
{
    public Guid Id { get; set; }

    // ── Names (parsed from CharacterData.Name) ────────────────────────────
    /// <summary>Canonical name as written. Mirrors Entity.Name so the Characters table is queryable on its own.</summary>
    public string Name { get; set; } = "";
    /// <summary>Given name. Populated when Name has 2+ whitespace-separated tokens. Title prefixes (Dr., Mr., Mx., etc.) are skipped.</summary>
    public string FirstName { get; set; } = "";
    /// <summary>Middle name(s) when present. "Sasha Marie Võ" → "Marie". NULL for two-token names.</summary>
    public string? MiddleName { get; set; }
    /// <summary>Family name. The last token when Name has 2+ tokens; empty for mononyms ("Pixel").</summary>
    public string LastName  { get; set; } = "";
    /// <summary>Title / honorific prefix (Dr., Captain, etc.) extracted from Name. Empty when none.</summary>
    public string TitlePrefix { get; set; } = "";

    // ── Identity / classification ─────────────────────────────────────────
    /// <summary>human | ai | android | synthetic | cyborg | hybrid | unknown</summary>
    public string Species { get; set; } = "human";
    /// <summary>human | e_l_f | iowan_behemoth | automaton | ai_avatar | synthetic</summary>
    public string KindOfBeing { get; set; } = "human";

    public string Gender   { get; set; } = "";
    public string Pronouns { get; set; } = "";
    public int    Age      { get; set; }
    public DateTime? Birthdate { get; set; }
    public double Rating { get; set; }
    public int VoteCount { get; set; }

    /// <summary>alive | dead | missing | etc.</summary>
    public string LifeStatus { get; set; } = "alive";

    // Location was dropped from this entity 2026-05-08 — current location is
    // dynamic story-state and lives in EntityStateEvents under aspect:location
    // (read via WorldStateLedger.StateAtAsync, populated for free into
    // CharacterData by CharacterMapper). The denormalised column behind this
    // property has been ALTER TABLE DROP'd. See project_static_vs_dynamic_split.md.

    // ── Roles, prose blobs (single-string MAX) ────────────────────────────
    public string Role               { get; set; } = "";
    // Affiliation flat column dropped 2026-05-08 — canonical source is
    // the CharacterAffiliations bridge (Affiliations navigation below) plus
    // Edge `affiliated_with`.
    public string Description        { get; set; } = "";
    public string NarrativeFunction  { get; set; } = "";
    public string NarrationVoice     { get; set; } = "";
    public string Augmentations      { get; set; } = "";
    public string DailyLife          { get; set; } = "";
    public string MidjourneyPrompt   { get; set; } = "";
    public string Dalle3Prompt       { get; set; } = "";

    // ── Belongings (scalar columns dropped 2026-05-08) ────────────────────
    // The "current primary X" pointers are now single-row buckets in
    // CharacterBelongingsGear — bucket names: primary_weapon, secondary_weapon,
    // armor, vehicle, residence, clothing_style, favorite_drink, favorite_food,
    // stimulant, comm_device. List buckets (signature_gear, pharmaceuticals)
    // unchanged.

    // ── Operating territory (scalar parts) ────────────────────────────────
    // TerritoryHomeTurf and HomeTurf flat columns dropped 2026-05-08 —
    // canonical source is the CharacterHomeTurfs bridge (HomeTurfs navigation
    // below). The "primary" home turf is HomeTurfs.OrderBy(Position).First().Alias.
    /// <summary>local | regional | continental | global</summary>
    public string TerritoryRange    { get; set; } = "local";

    // ── Physical description (NCIC-style scalars) ─────────────────────────
    public string Heritage              { get; set; } = "";
    public int    HeightCm              { get; set; }
    public int    WeightKg              { get; set; }
    public string Build                 { get; set; } = "";
    public string HairColor             { get; set; } = "";
    public string HairStyle             { get; set; } = "";
    public string HairLength            { get; set; } = "";
    public string EyeColor              { get; set; } = "";
    public string SkinTone              { get; set; } = "";
    public string Complexion            { get; set; } = "";
    public string VisibleAugmentations  { get; set; } = "";
    public string PostureMovement       { get; set; } = "";
    public string PhysicalClothingStyle { get; set; } = "";

    // ── Psychology (only the scalar field is flattened) ───────────────────
    public string PsychologySecret { get; set; } = "";

    // ── Speech patterns (scalar fields) ───────────────────────────────────
    public string SpeechVocabulary       { get; set; } = "";
    public string SpeechCadence          { get; set; } = "";
    public string SpeechSubtext          { get; set; } = "";
    public string SpeechUnderPressure    { get; set; } = "";
    public string SpeechIntimacyRegister { get; set; } = "";

    // ── Bio-battery (scalars; thresholds bridge has the dict) ─────────────
    public string BioBatteryMaxCapacity { get; set; } = "";
    public string BioBatteryRecovery    { get; set; } = "";

    // ── Navigation ────────────────────────────────────────────────────────
    public Entity? Entity { get; set; }

    public ICollection<CharacterAlias>             Aliases             { get; set; } = new List<CharacterAlias>();
    public ICollection<CharacterStoryHook>         StoryHooks          { get; set; } = new List<CharacterStoryHook>();
    public ICollection<CharacterArchetypeScore>    ArchetypeScores     { get; set; } = new List<CharacterArchetypeScore>();
    public ICollection<CharacterGeneticAncestry>   GeneticAncestry     { get; set; } = new List<CharacterGeneticAncestry>();
    public ICollection<CharacterAncestryDetail>    AncestryDetail      { get; set; } = new List<CharacterAncestryDetail>();
    public ICollection<CharacterPsychologyTrait>   PsychologyTraits    { get; set; } = new List<CharacterPsychologyTrait>();
    public ICollection<CharacterSpeechPhrase>      SpeechPhrases       { get; set; } = new List<CharacterSpeechPhrase>();
    public ICollection<CharacterBehavioralRule>    BehavioralRules     { get; set; } = new List<CharacterBehavioralRule>();
    public ICollection<CharacterBehavioralMap>     BehavioralMaps      { get; set; } = new List<CharacterBehavioralMap>();
    public ICollection<CharacterStatScalar>        StatScalars         { get; set; } = new List<CharacterStatScalar>();
    public ICollection<CharacterStatPhrase>        StatPhrases         { get; set; } = new List<CharacterStatPhrase>();
    public ICollection<CharacterPhysicalMark>      PhysicalMarks       { get; set; } = new List<CharacterPhysicalMark>();
    public ICollection<CharacterTerritoryZone>     TerritoryZones      { get; set; } = new List<CharacterTerritoryZone>();
    public ICollection<CharacterTerritoryReputation> TerritoryReputations { get; set; } = new List<CharacterTerritoryReputation>();
    public ICollection<CharacterBelongingsGear>    BelongingsGear      { get; set; } = new List<CharacterBelongingsGear>();
    public ICollection<CharacterBelongingsExtra>   BelongingsExtras    { get; set; } = new List<CharacterBelongingsExtra>();
    public ICollection<CharacterBioBatteryThreshold> BioBatteryThresholds { get; set; } = new List<CharacterBioBatteryThreshold>();
    public ICollection<CharacterNeuralAbility>     NeuralAbilities     { get; set; } = new List<CharacterNeuralAbility>();
    public ICollection<CharacterChangelogRow>      Changelog           { get; set; } = new List<CharacterChangelogRow>();

    public ICollection<CharacterCyberware>         Cyberware           { get; set; } = new List<CharacterCyberware>();
    public ICollection<CharacterKnowledgeRow>      Knowledge           { get; set; } = new List<CharacterKnowledgeRow>();
    public ICollection<CharacterConditionRow>      Conditions          { get; set; } = new List<CharacterConditionRow>();
    public ICollection<CharacterRelationshipRow>   Relationships       { get; set; } = new List<CharacterRelationshipRow>();
    public ICollection<CharacterTimelineEvent>     Timeline            { get; set; } = new List<CharacterTimelineEvent>();

    // ── Resolved-entity bridges ───────────────────────────────────────────
    // Each row links the character to a canonical Entity row of the right type.
    // Alias preserves the original source string so we don't lose display names
    // when no canonical match exists yet.
    public ICollection<CharacterHomeTurf>     HomeTurfs     { get; set; } = new List<CharacterHomeTurf>();
    public ICollection<CharacterAffiliation>  Affiliations  { get; set; } = new List<CharacterAffiliation>();
}

// ── Resolved-entity bridges ────────────────────────────────────────────────

/// <summary>
/// Joins a Character to a Place entity (1:M — a character can have multiple
/// home turfs over time or simultaneously). PlaceId is NULL when the source
/// data named a place that doesn't have a canonical Place record yet — the
/// Alias string still holds the human-readable name so the dossier renders.
/// Use INNER JOIN on PlaceId for "characters with a known canonical home";
/// LEFT JOIN preserves all-as-written.
/// </summary>
public class CharacterHomeTurf
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>FK to Entities.Id where EntityType='place'. Null when unresolved.</summary>
    public Guid? PlaceId { get; set; }
    /// <summary>Original source string ("Sektor 9", "the Loop", etc). Always populated.</summary>
    public string Alias { get; set; } = "";
    /// <summary>Order if a character has multiple home turfs.</summary>
    public int Position { get; set; }
    public Character? Character { get; set; }
    public Entity? Place { get; set; }
}

/// <summary>
/// Joins a Character to a Faction entity (1:M — burning a handler and joining
/// a new crew leaves both relationships in the historical record). FactionId
/// is NULL when the source named a faction that doesn't yet have a canonical
/// Faction record. The Alias preserves the source string.
/// </summary>
public class CharacterAffiliation
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>FK to Entities.Id where EntityType='faction'. Null when unresolved.</summary>
    public Guid? FactionId { get; set; }
    /// <summary>Original source string ("Iron Lotus", "Independent", etc). Always populated.</summary>
    public string Alias { get; set; } = "";
    public int Position { get; set; }
    public Character? Character { get; set; }
    public Entity? Faction { get; set; }
}

// ── Simple list bridges ─────────────────────────────────────────────────────

public class CharacterAlias
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public int Position { get; set; }
    public string Value { get; set; } = "";
    public Character? Character { get; set; }
}

public class CharacterStoryHook
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public int Position { get; set; }
    public string Hook { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Archetype scores (Dict<string,double>) ─────────────────────────────────

public class CharacterArchetypeScore
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string ArchetypeName { get; set; } = "";
    public double Score { get; set; }
    public Character? Character { get; set; }
}

// ── Genetic ancestry (Dict<string,double> — region → percent) ──────────────

public class CharacterGeneticAncestry
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Region { get; set; } = "";
    public double Percent { get; set; }
    public Character? Character { get; set; }
}

// ── Ancestry detail (3-deep nested dict — region → sub-region → nationality → %) ──

public class CharacterAncestryDetail
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Region { get; set; } = "";
    public string SubRegion { get; set; } = "";
    public string Nationality { get; set; } = "";
    public double Percent { get; set; }
    public Character? Character { get; set; }
}

// ── Psychology lists (CoreFears/Desires/Coping/BlindSpots) ─────────────────

public class CharacterPsychologyTrait
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>core_fears | core_desires | coping_mechanisms | blind_spots</summary>
    public string Bucket { get; set; } = "";
    public int Position { get; set; }
    public string Trait { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Speech list-fields (verbal_tics / example_lines / avoidances) ──────────

public class CharacterSpeechPhrase
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>verbal_tics | example_lines | avoidances</summary>
    public string Bucket { get; set; } = "";
    public int Position { get; set; }
    public string Phrase { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Behavioral list-fields ─────────────────────────────────────────────────

public class CharacterBehavioralRule
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>decision_rules | escalation_ladder | contradictions | habits | breaking_points</summary>
    public string Bucket { get; set; } = "";
    public int Position { get; set; }
    public string Rule { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Behavioral dict-fields (interpersonal_modes / stress_responses) ────────

public class CharacterBehavioralMap
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>interpersonal_modes | stress_responses</summary>
    public string Bucket { get; set; } = "";
    public string KeyName { get; set; } = "";
    public string Value { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Stats heterogeneous (Dict<string, JsonElement>) ────────────────────────

public class CharacterStatScalar
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>physical | mental | social | personality | thresholds</summary>
    public string Bucket { get; set; } = "";
    public string KeyName { get; set; } = "";
    /// <summary>string | number | bool | null | array | object — the original JsonElement.ValueKind.</summary>
    public string ValueKind { get; set; } = "string";
    public string? ValueText   { get; set; }
    public double? ValueNumber { get; set; }
    public bool?   ValueBool   { get; set; }
    public Character? Character { get; set; }
}

public class CharacterStatPhrase
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>drives | strengths | weaknesses | tags</summary>
    public string Bucket { get; set; } = "";
    public int Position { get; set; }
    public string Phrase { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Physical description list-fields (DistinguishingMarks) ─────────────────

public class CharacterPhysicalMark
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public int Position { get; set; }
    public string Mark { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Operating territory lists / dicts ──────────────────────────────────────

public class CharacterTerritoryZone
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>familiar | no_go</summary>
    public string Bucket { get; set; } = "familiar";
    public int Position { get; set; }
    public string Zone { get; set; } = "";
    public Character? Character { get; set; }
}

public class CharacterTerritoryReputation
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Zone { get; set; } = "";
    public string Reputation { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Belongings lists / dicts ───────────────────────────────────────────────

public class CharacterBelongingsGear
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>signature_gear | pharmaceuticals</summary>
    public string Bucket { get; set; } = "";
    public int Position { get; set; }
    public string GearName { get; set; } = "";

    /// <summary>
    /// Optional FK to the Entities row for this gear (when GearName resolves to
    /// a real Weapon / Equipment / etc.). NULL when only a free-form name is
    /// known. Lets us join "what character owns this weapon" without LIKE-ing
    /// the GearName column.
    /// </summary>
    public Guid? GearEntityId { get; set; }

    public Character? Character { get; set; }
    public Entity? GearEntity { get; set; }
}

public class CharacterBelongingsExtra
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string KeyName { get; set; } = "";
    public string Value { get; set; } = "";
    public Character? Character { get; set; }
}

// ── BioBattery thresholds (Dict<string,string>) ────────────────────────────

public class CharacterBioBatteryThreshold
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    /// <summary>Threshold key — typically a percentage as a string ("60", "40", "20", "10", "0").</summary>
    public string Threshold { get; set; } = "";
    public string Consequence { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Neural abilities ───────────────────────────────────────────────────────

public class CharacterNeuralAbility
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public int Position { get; set; }
    public string Name { get; set; } = "";
    public int CostPercent { get; set; }
    public string Description { get; set; } = "";
    public string OverdrawnRisk { get; set; } = "";
    public bool Passive { get; set; }
    public Character? Character { get; set; }
}

// ── Canon changelog ────────────────────────────────────────────────────────

public class CharacterChangelogRow
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public int Position { get; set; }
    public string StoryId { get; set; } = "";
    public string Beat { get; set; } = "";
    public string Date { get; set; } = "";
    public DateTime? InWorldDate { get; set; }
    public string FieldName { get; set; } = "";
    public string FromValue { get; set; } = "";
    public string ToValue { get; set; } = "";
    public string Reason { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Cyberware (already columnar — unchanged) ───────────────────────────────

public class CharacterCyberware
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = "";
    public string BodyLocation { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Tier { get; set; } = "";
    public string Condition { get; set; } = "functional";
    public string InstalledDate { get; set; } = "";
    public string Description { get; set; } = "";
    public string Replaces { get; set; } = "";
    public Character? Character { get; set; }
}

// ── Knowledge (Entities List<string> now its own bridge) ──────────────────

public class CharacterKnowledgeRow
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Topic { get; set; } = "";
    public string Summary { get; set; } = "";
    public int? LearnedChapter { get; set; }
    public string? LearnedChapterId { get; set; }
    public int? SourceBeat { get; set; }
    public string? SourceSnippet { get; set; }
    public Character? Character { get; set; }
    public ICollection<CharacterKnowledgeEntity> RelatedEntities { get; set; } = new List<CharacterKnowledgeEntity>();
}

public class CharacterKnowledgeEntity
{
    public long Id { get; set; }
    public long KnowledgeId { get; set; }
    public int Position { get; set; }
    /// <summary>Entity id this knowledge concerns (so dossier expansion can pull related cards).</summary>
    public string EntityRef { get; set; } = "";
    public CharacterKnowledgeRow? Knowledge { get; set; }
}

public class CharacterConditionRow
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Notes { get; set; } = "";
    public int? SinceChapter { get; set; }
    public int? UntilChapter { get; set; }
    public Character? Character { get; set; }
}

public class CharacterRelationshipRow
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string TargetName { get; set; } = "";
    public Guid? TargetEntityId { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string EmotionalCore { get; set; } = "";
    public string StoryTension { get; set; } = "";
    public string Status { get; set; } = "active";
    public int? SinceChapter { get; set; }
    public int? UntilChapter { get; set; }
    public Character? Character { get; set; }
}

// ── Timeline (BodyChanges List<string> now its own bridge) ─────────────────

public class CharacterTimelineEvent
{
    public long Id { get; set; }
    public Guid CharacterId { get; set; }
    public string Date { get; set; } = "";
    public DateTime? InWorldDate { get; set; }
    public string StoryId { get; set; } = "";
    public string Event { get; set; } = "";
    public string Consequences { get; set; } = "";
    public string StatusChange { get; set; } = "";
    public Character? Character { get; set; }
    public ICollection<CharacterTimelineBodyChange> BodyChanges { get; set; } = new List<CharacterTimelineBodyChange>();
}

public class CharacterTimelineBodyChange
{
    public long Id { get; set; }
    public long TimelineEventId { get; set; }
    public int Position { get; set; }
    public string BodyChange { get; set; } = "";
    public CharacterTimelineEvent? TimelineEvent { get; set; }
}
