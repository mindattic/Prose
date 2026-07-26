using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Story bible, literary rules, and motifs — the constraints the AI must follow.
/// </summary>
public class StoryBibleData
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("genre")] public string Genre { get; set; } = "";
    [JsonPropertyName("tone")] public string Tone { get; set; } = "";
    [JsonPropertyName("core_theme")] public string CoreTheme { get; set; } = "";
    [JsonPropertyName("core_hook")] public string CoreHook { get; set; } = "";
    // The setting field in story_bible.json is a rich nested object — use JsonElement to accept any shape
    [JsonPropertyName("setting")] public JsonElement Setting { get; set; }
    [JsonPropertyName("protagonist")] public string Protagonist { get; set; } = "";
    [JsonPropertyName("arc")] public string Arc { get; set; } = "";
    [JsonPropertyName("themes")] public List<string> Themes { get; set; } = [];
}

public class LiteraryRulesData
{
    [JsonPropertyName("sentence_max_words")] public int SentenceMaxWords { get; set; } = 25;
    [JsonPropertyName("paragraph_requirements")] public List<string> ParagraphRequirements { get; set; } = [];
    [JsonPropertyName("prohibitions")] public List<string> Prohibitions { get; set; } = [];
    [JsonPropertyName("structural")] public StructuralRulesData Structural { get; set; } = new();
    [JsonPropertyName("pov_voice_rules")] public PovVoiceRules PovVoice { get; set; } = new();
    [JsonPropertyName("paragraph_economy")] public ParagraphEconomyRules ParagraphEconomy { get; set; } = new();
    [JsonPropertyName("register_permissions")] public RegisterPermissions RegisterPermissions { get; set; } = new();
    [JsonPropertyName("beat_doctrine")] public BeatDoctrineRules BeatDoctrine { get; set; } = new();
}

/// <summary>
/// The canonical, codified definition of what a Beat is — the single source the
/// story generator AND the re-beater both read (surfaced by
/// <see cref="StreetSamurai.Core.Services.DatabaseService.GetLiteraryRulesPrompt"/>).
/// A beat is a complete SCENE or SEQUEL in Dwight Swain's Scene-Sequel doctrine.
/// NOT a paragraph, NOT an MRU. Defaults are baked in here so the doctrine is always
/// present even against a DB literary_rules row that predates it; the stored
/// setting may override.
/// </summary>
public class BeatDoctrineRules
{
    [JsonPropertyName("definition")]
    public string Definition { get; set; } =
        "A beat is one complete SCENE or one complete SEQUEL in Dwight Swain's Scene-Sequel doctrine. " +
        "NOT a paragraph. NOT a Motivation-Reaction Unit (MRU). MRUs are the sentence-level fabric INSIDE a beat. " +
        "A SCENE runs: Goal (POV character enters wanting something specific) → Conflict (obstacle blocks it) → Disaster (ends in failure or reversal — never full success). " +
        "A SEQUEL runs: Reaction (gut-level involuntary response to the disaster) → Dilemma (all choices are bad) → Decision (new goal that launches the next Scene).";

    [JsonPropertyName("rules")]
    public List<string> Rules { get; set; } =
    [
        "SCENE beat: the POV character enters with a specific, immediate Goal; faces Conflict (opposition, complication, obstacle); ends in Disaster (failure, reversal, or a worse situation — never full success that kills tension).",
        "SEQUEL beat: follows a Disaster; POV character Reacts (involuntary, before thought — grief, shock, anger); faces a Dilemma (all options are bad); makes a Decision that launches the next Scene.",
        "One beat = one complete Scene OR one complete Sequel. Never merge a Scene and its Sequel into one beat. Never split a Scene's Goal/Conflict/Disaster across multiple beats.",
        "Scale: a 400-page novel produces roughly 400–600 beats (approximately 1 beat per page). A 3-page scene with one goal/conflict/disaster is ONE beat — do not fragment it into paragraphs.",
        "Dialogue, action, description, and inner monologue are all INTERIOR to a beat. A 10-line back-and-forth is part of the Conflict inside one Scene beat — not 10 separate beats.",
        "Inside a beat: real sentences; each speaker's dialogue on its own line (NOT its own beat); questions end with '?'; question attribution uses asks/asked, not says/said; inner monologue is italic on its own line, never labeled.",
        "Mark sceneEnd=true on the last beat before a time/location jump. Mark it false everywhere else.",
    ];
}

public class PovVoiceRules
{
    [JsonPropertyName("principle")] public string Principle { get; set; } = "";
    [JsonPropertyName("differentiation")] public List<string> Differentiation { get; set; } = [];
    [JsonPropertyName("anti_cadence_check")] public string AntiCadenceCheck { get; set; } = "";
    [JsonPropertyName("shared_world_anchors")] public string SharedWorldAnchors { get; set; } = "";
}

public class ParagraphEconomyRules
{
    [JsonPropertyName("principle")] public string Principle { get; set; } = "";
    [JsonPropertyName("tests")] public List<string> Tests { get; set; } = [];
}

public class RegisterPermissions
{
    [JsonPropertyName("principle")] public string Principle { get; set; } = "";
    [JsonPropertyName("allowed_modes")] public List<string> AllowedModes { get; set; } = [];
    [JsonPropertyName("register_traps_to_still_avoid")] public List<string> Traps { get; set; } = [];
}

public class StructuralRulesData
{
    [JsonPropertyName("pov")] public string Pov { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("choice")] public string Choice { get; set; } = "";
    [JsonPropertyName("consequence")] public string Consequence { get; set; } = "";
    [JsonPropertyName("ending")] public string Ending { get; set; } = "";
    [JsonPropertyName("pace")] public string Pace { get; set; } = "";
}


public class MotifData : IWorldRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("appearances")] public List<MotifAppearanceData> Appearances { get; set; } = [];
}

public class MotifAppearanceData
{
    [JsonPropertyName("scene")] public int Scene { get; set; }
    [JsonPropertyName("meaning")] public string Meaning { get; set; } = "";
}

/// <summary>
/// Character profile (Kyle's core identity).
/// </summary>
public class CharacterProfileData
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("core_contradiction")] public string CoreContradiction { get; set; } = "";
    [JsonPropertyName("era")] public string Era { get; set; } = "";
    [JsonPropertyName("genre")] public string Genre { get; set; } = "";
    [JsonPropertyName("arc")] public string Arc { get; set; } = "";
    [JsonPropertyName("augmentation")] public string Augmentation { get; set; } = "";
}

/// <summary>
/// Worldbuilding document — converted from markdown to structured JSON.
/// The body text is preserved as-is (it's literature, not data).
/// Metadata is extracted and strongly typed.
/// </summary>
public class WorldbuildingDocument : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("file_name")] public string FileName { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("body")] public string Body { get; set; } = "";
    [JsonPropertyName("line_count")] public int LineCount { get; set; }
    [JsonPropertyName("headings")] public List<string> Headings { get; set; } = [];
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];

    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
    /// <summary>Captures any extra JSON fields not explicitly modeled (e.g., map_polygon, coordinates).</summary>
    [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraData { get; set; }
}

/// <summary>
/// Corponation — fully structured from markdown.
/// </summary>
public class CorponationData : ICanonEntity
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("rating")] public double Rating { get; set; } = 0.0;
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; } = 0;
    [JsonPropertyName("number")] public int Number { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("full_legal_name")] public string FullLegalName { get; set; } = "";
    [JsonPropertyName("common_names")] public List<string> CommonNames { get; set; } = [];
    [JsonPropertyName("stock_designation")] public string StockDesignation { get; set; } = "";
    [JsonPropertyName("sector")] public string Sector { get; set; } = "";
    [JsonPropertyName("valuation")] public string Valuation { get; set; } = "";
    [JsonPropertyName("revenue")] public string Revenue { get; set; } = "";
    [JsonPropertyName("employees")] public string Employees { get; set; } = "";
    [JsonPropertyName("sovereign_territory")] public string SovereignTerritory { get; set; } = "";
    [JsonPropertyName("founding_story")] public string FoundingStory { get; set; } = "";
    [JsonPropertyName("security_force")] public string SecurityForce { get; set; } = "";
    [JsonPropertyName("key_detail")] public string KeyDetail { get; set; } = "";
    [JsonPropertyName("relationship_to_big_20")] public string RelationshipToBig20 { get; set; } = "";
    [JsonPropertyName("full_text")] public string FullText { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("image_prompt")] public string MidjourneyPrompt { get; set; } = "";
    [JsonPropertyName("dalle3_prompt")] public string Dalle3Prompt { get; set; } = "";
}

/// <summary>
/// The master canon database — single JSON file containing all structured data.
/// </summary>
public class Database
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("generated_at")] public DateTime GeneratedAt { get; set; }
    [JsonPropertyName("characters")] public List<CharacterData> Characters { get; set; } = [];
    [JsonPropertyName("districts")] public List<DistrictData> Districts { get; set; } = [];
    [JsonPropertyName("factions")] public List<FactionData> Factions { get; set; } = [];
    [JsonPropertyName("corponations")] public List<CorponationData> Corponations { get; set; } = [];
    [JsonPropertyName("worldbuilding_docs")] public List<WorldbuildingDocument> WorldbuildingDocs { get; set; } = [];
    [JsonPropertyName("story_bible")] public StoryBibleData StoryBible { get; set; } = new();
    [JsonPropertyName("literary_rules")] public LiteraryRulesData LiteraryRules { get; set; } = new();
    [JsonPropertyName("motifs")] public List<MotifData> Motifs { get; set; } = [];
    [JsonPropertyName("character_profile")] public CharacterProfileData CharacterProfile { get; set; } = new();
}
