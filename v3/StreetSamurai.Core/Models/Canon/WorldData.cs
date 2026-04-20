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
    [JsonPropertyName("facet_rules")] public FacetRulesData FacetRules { get; set; } = new();
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

public class FacetRulesData
{
    [JsonPropertyName("interjections")] public string Interjections { get; set; } = "";
    [JsonPropertyName("disagreement")] public string Disagreement { get; set; } = "";
    [JsonPropertyName("lead_voice")] public string LeadVoice { get; set; } = "";
    [JsonPropertyName("rotation")] public string Rotation { get; set; } = "";
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
    [JsonPropertyName("facets")] public List<string> Facets { get; set; } = [];
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
    [JsonPropertyName("facets")] public List<FacetData> Facets { get; set; } = [];
    [JsonPropertyName("districts")] public List<DistrictData> Districts { get; set; } = [];
    [JsonPropertyName("factions")] public List<FactionData> Factions { get; set; } = [];
    [JsonPropertyName("corponations")] public List<CorponationData> Corponations { get; set; } = [];
    [JsonPropertyName("worldbuilding_docs")] public List<WorldbuildingDocument> WorldbuildingDocs { get; set; } = [];
    [JsonPropertyName("story_bible")] public StoryBibleData StoryBible { get; set; } = new();
    [JsonPropertyName("literary_rules")] public LiteraryRulesData LiteraryRules { get; set; } = new();
    [JsonPropertyName("motifs")] public List<MotifData> Motifs { get; set; } = [];
    [JsonPropertyName("character_profile")] public CharacterProfileData CharacterProfile { get; set; } = new();
}
