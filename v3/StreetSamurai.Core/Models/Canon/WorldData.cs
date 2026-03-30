using System.Text.Json.Serialization;

namespace StreetSamurai.Core.Models.Canon;

/// <summary>
/// Story bible, literary rules, and motifs — the constraints the AI must follow.
/// </summary>
public record StoryBibleData
{
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("genre")] public string Genre { get; init; } = "";
    [JsonPropertyName("tone")] public string Tone { get; init; } = "";
    [JsonPropertyName("core_theme")] public string CoreTheme { get; init; } = "";
    [JsonPropertyName("core_hook")] public string CoreHook { get; init; } = "";
    [JsonPropertyName("setting")] public string Setting { get; init; } = "";
    [JsonPropertyName("protagonist")] public string Protagonist { get; init; } = "";
    [JsonPropertyName("arc")] public string Arc { get; init; } = "";
    [JsonPropertyName("themes")] public List<string> Themes { get; init; } = [];
}

public record LiteraryRulesData
{
    [JsonPropertyName("sentence_max_words")] public int SentenceMaxWords { get; init; } = 25;
    [JsonPropertyName("paragraph_requirements")] public List<string> ParagraphRequirements { get; init; } = [];
    [JsonPropertyName("prohibitions")] public List<string> Prohibitions { get; init; } = [];
    [JsonPropertyName("structural")] public StructuralRulesData Structural { get; init; } = new();
    [JsonPropertyName("facet_rules")] public FacetRulesData FacetRules { get; init; } = new();
}

public record StructuralRulesData
{
    [JsonPropertyName("pov")] public string Pov { get; init; } = "";
    [JsonPropertyName("location")] public string Location { get; init; } = "";
    [JsonPropertyName("choice")] public string Choice { get; init; } = "";
    [JsonPropertyName("consequence")] public string Consequence { get; init; } = "";
    [JsonPropertyName("ending")] public string Ending { get; init; } = "";
    [JsonPropertyName("pace")] public string Pace { get; init; } = "";
}

public record FacetRulesData
{
    [JsonPropertyName("interjections")] public string Interjections { get; init; } = "";
    [JsonPropertyName("disagreement")] public string Disagreement { get; init; } = "";
    [JsonPropertyName("lead_voice")] public string LeadVoice { get; init; } = "";
    [JsonPropertyName("rotation")] public string Rotation { get; init; } = "";
}

public record MotifData
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("description")] public string Description { get; init; } = "";
    [JsonPropertyName("appearances")] public List<MotifAppearanceData> Appearances { get; init; } = [];
}

public record MotifAppearanceData
{
    [JsonPropertyName("scene")] public int Scene { get; init; }
    [JsonPropertyName("meaning")] public string Meaning { get; init; } = "";
}

/// <summary>
/// Character profile (Kyle's core identity).
/// </summary>
public record CharacterProfileData
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("core_contradiction")] public string CoreContradiction { get; init; } = "";
    [JsonPropertyName("era")] public string Era { get; init; } = "";
    [JsonPropertyName("genre")] public string Genre { get; init; } = "";
    [JsonPropertyName("arc")] public string Arc { get; init; } = "";
    [JsonPropertyName("augmentation")] public string Augmentation { get; init; } = "";
    [JsonPropertyName("facets")] public List<string> Facets { get; init; } = [];
}

/// <summary>
/// Worldbuilding document — converted from markdown to structured JSON.
/// The body text is preserved as-is (it's literature, not data).
/// Metadata is extracted and strongly typed.
/// </summary>
public record WorldbuildingDocument
{
    [JsonPropertyName("file_name")] public string FileName { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("body")] public string Body { get; init; } = "";
    [JsonPropertyName("line_count")] public int LineCount { get; init; }
    [JsonPropertyName("headings")] public List<string> Headings { get; init; } = [];
}

/// <summary>
/// Corponation — fully structured from markdown.
/// </summary>
public record CorponationData
{
    [JsonPropertyName("number")] public int Number { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("full_legal_name")] public string FullLegalName { get; init; } = "";
    [JsonPropertyName("common_names")] public List<string> CommonNames { get; init; } = [];
    [JsonPropertyName("stock_designation")] public string StockDesignation { get; init; } = "";
    [JsonPropertyName("sector")] public string Sector { get; init; } = "";
    [JsonPropertyName("valuation")] public string Valuation { get; init; } = "";
    [JsonPropertyName("revenue")] public string Revenue { get; init; } = "";
    [JsonPropertyName("employees")] public string Employees { get; init; } = "";
    [JsonPropertyName("sovereign_territory")] public string SovereignTerritory { get; init; } = "";
    [JsonPropertyName("founding_story")] public string FoundingStory { get; init; } = "";
    [JsonPropertyName("security_force")] public string SecurityForce { get; init; } = "";
    [JsonPropertyName("key_detail")] public string KeyDetail { get; init; } = "";
    [JsonPropertyName("relationship_to_big_20")] public string RelationshipToBig20 { get; init; } = "";
    [JsonPropertyName("full_text")] public string FullText { get; init; } = "";
}

/// <summary>
/// The master canon database — single JSON file containing all structured data.
/// </summary>
public record CanonDatabase
{
    [JsonPropertyName("version")] public int Version { get; init; } = 1;
    [JsonPropertyName("generated_at")] public DateTime GeneratedAt { get; init; }
    [JsonPropertyName("characters")] public List<CharacterData> Characters { get; init; } = [];
    [JsonPropertyName("facets")] public List<FacetData> Facets { get; init; } = [];
    [JsonPropertyName("districts")] public List<DistrictData> Districts { get; init; } = [];
    [JsonPropertyName("factions")] public List<FactionData> Factions { get; init; } = [];
    [JsonPropertyName("corponations")] public List<CorponationData> Corponations { get; init; } = [];
    [JsonPropertyName("worldbuilding_docs")] public List<WorldbuildingDocument> WorldbuildingDocs { get; init; } = [];
    [JsonPropertyName("story_bible")] public StoryBibleData StoryBible { get; init; } = new();
    [JsonPropertyName("literary_rules")] public LiteraryRulesData LiteraryRules { get; init; } = new();
    [JsonPropertyName("motifs")] public List<MotifData> Motifs { get; init; } = [];
    [JsonPropertyName("character_profile")] public CharacterProfileData CharacterProfile { get; init; } = new();
}
