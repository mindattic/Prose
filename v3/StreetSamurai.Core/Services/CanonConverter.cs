using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using StreetSamurai.Core.Models.Canon;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// One-time converter: reads all YAML and MD files, produces canon.json.
/// Also callable at startup to rebuild if source files are newer than canon.json.
/// </summary>
public static class CanonConverter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly IDeserializer YamlParser = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static CanonDatabase BuildDatabase(string canonRoot)
    {
        var db = new CanonDatabase
        {
            GeneratedAt = DateTime.UtcNow,
            Characters = LoadCharacters(canonRoot),
            Facets = LoadFacets(canonRoot),
            Districts = LoadDistricts(canonRoot),
            Factions = LoadFactions(canonRoot),
            Corponations = LoadCorponations(canonRoot),
            WorldbuildingDocs = LoadWorldbuildingDocs(canonRoot),
            StoryBible = LoadStoryBible(canonRoot),
            LiteraryRules = LoadLiteraryRules(canonRoot),
            Motifs = LoadMotifs(canonRoot),
            CharacterProfile = LoadCharacterProfile(canonRoot),
        };
        return db;
    }

    public static string BuildAndSave(string canonRoot, string outputPath)
    {
        var db = BuildDatabase(canonRoot);
        var json = JsonSerializer.Serialize(db, JsonOpts);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json);
        return outputPath;
    }

    /// <summary>
    /// Returns true if canon.json needs rebuilding (source files are newer).
    /// </summary>
    public static bool NeedsRebuild(string canonRoot, string canonJsonPath)
    {
        if (!File.Exists(canonJsonPath)) return true;
        var jsonTime = File.GetLastWriteTimeUtc(canonJsonPath);
        var dirs = new[]
        {
            Path.Combine(canonRoot, "essences"),
            Path.Combine(canonRoot, "character"),
            Path.Combine(canonRoot, "world"),
            Path.Combine(canonRoot, "worldbuilding"),
        };
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                if (File.GetLastWriteTimeUtc(f) > jsonTime) return true;
            }
        }
        return false;
    }

    // ── Characters ──────────────────────────────────────

    private static List<CharacterData> LoadCharacters(string root)
    {
        var chars = new List<CharacterData>();
        var dir = Path.Combine(root, "essences", "characters");
        if (!Directory.Exists(dir)) return chars;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var raw = YamlParser.Deserialize<Dictionary<string, object>>(yaml);
                chars.Add(ParseCharacter(raw, file));
            }
            catch { }
        }
        return chars;
    }

    private static CharacterData ParseCharacter(Dictionary<string, object> d, string file)
    {
        var psych = GetDict(d, "psychology");
        var fw = psych != null ? GetDict(psych, "facet_weights") : null;
        var sp = GetDict(d, "speech_patterns");

        return new CharacterData
        {
            Type = GetStr(d, "type") ?? "character",
            Name = GetStr(d, "name") ?? Path.GetFileNameWithoutExtension(file),
            Aliases = GetList(d, "aliases"),
            Role = GetStr(d, "role") ?? "",
            Age = GetInt(d, "age"),
            Status = GetStr(d, "status") ?? "alive",
            Location = GetStr(d, "location") ?? "",
            Description = GetStr(d, "description") ?? "",
            Augmentations = GetStr(d, "augmentations") ?? "",
            DailyLife = GetStr(d, "daily_life") ?? "",
            NarrativeFunction = GetStr(d, "narrative_function") ?? "",
            Affiliation = GetStr(d, "affiliation") ?? "",
            Psychology = new CharacterPsychology
            {
                FacetWeights = fw != null ? new FacetWeights
                {
                    Wound = GetDouble(fw, "wound"),
                    Ideal = GetDouble(fw, "ideal"),
                    Id = GetDouble(fw, "id"),
                    Shadow = GetDouble(fw, "shadow"),
                    Mask = GetDouble(fw, "mask"),
                    Ghost = GetDouble(fw, "ghost"),
                } : new(),
                CoreFears = psych != null ? GetList(psych, "core_fears") : [],
                CoreDesires = psych != null ? GetList(psych, "core_desires") : [],
                CopingMechanisms = psych != null ? GetList(psych, "coping_mechanisms") : [],
                BlindSpots = psych != null ? GetList(psych, "blind_spots") : [],
                Secret = psych != null ? GetStr(psych, "secret") ?? "" : "",
            },
            SpeechPatterns = sp != null ? new SpeechPatterns
            {
                Vocabulary = GetStr(sp, "vocabulary") ?? "",
                Cadence = GetStr(sp, "cadence") ?? "",
                VerbalTics = GetList(sp, "verbal_tics"),
                ExampleLines = GetList(sp, "example_lines"),
            } : new(),
            Relationships = ParseRelationships(d),
            StoryHooks = GetList(d, "story_hooks"),
        };
    }

    private static List<CharacterRelationship> ParseRelationships(Dictionary<string, object> d)
    {
        var rels = new List<CharacterRelationship>();
        if (!d.TryGetValue("relationships", out var r) || r is not List<object> list) return rels;

        foreach (var item in list)
        {
            if (item is not Dictionary<object, object> rd) continue;
            var dd = rd.ToDictionary(kv => kv.Key.ToString()!, kv => kv.Value);
            rels.Add(new CharacterRelationship
            {
                Name = GetStr(dd, "name") ?? "",
                Type = GetStr(dd, "type") ?? "",
                Description = GetStr(dd, "description") ?? "",
                EmotionalCore = GetStr(dd, "emotional_core") ?? "",
                StoryTension = GetStr(dd, "story_tension") ?? "",
            });
        }
        return rels;
    }

    // ── Facets ──────────────────────────────────────────

    private static List<FacetData> LoadFacets(string root)
    {
        var facets = new List<FacetData>();
        var dir = Path.Combine(root, "character", "facets");
        if (!Directory.Exists(dir)) return facets;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var raw = YamlParser.Deserialize<Dictionary<string, object>>(yaml);
                var voice = GetDict(raw, "voice");

                facets.Add(new FacetData
                {
                    Name = GetStr(raw, "name") ?? Path.GetFileNameWithoutExtension(file),
                    Label = GetStr(raw, "label") ?? "",
                    Domain = GetStr(raw, "domain") ?? "",
                    Triggers = GetList(raw, "triggers"),
                    Voice = voice != null ? new FacetVoice
                    {
                        Tone = GetStr(voice, "tone") ?? "",
                        Style = GetStr(voice, "style") ?? "",
                        Prohibitions = GetList(voice, "prohibitions"),
                    } : new(),
                    CoreMemories = GetList(raw, "core_memories"),
                    Model = GetStr(raw, "model") ?? "claude-sonnet-4-6",
                    Temperature = GetDouble(raw, "temperature", 0.8),
                    SystemPrompt = GetStr(raw, "system_prompt") ?? "",
                });
            }
            catch { }
        }
        return facets;
    }

    // ── Districts ───────────────────────────────────────

    private static List<DistrictData> LoadDistricts(string root)
    {
        var districts = new List<DistrictData>();
        var dir = Path.Combine(root, "essences", "world", "districts");
        if (!Directory.Exists(dir)) return districts;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var raw = YamlParser.Deserialize<Dictionary<string, object>>(yaml);
                var atmos = GetDict(raw, "atmosphere");
                var conn = GetDict(raw, "connections");

                districts.Add(new DistrictData
                {
                    Type = GetStr(raw, "type") ?? "place",
                    Name = GetStr(raw, "name") ?? Path.GetFileNameWithoutExtension(file),
                    Aliases = GetList(raw, "aliases"),
                    Description = GetStr(raw, "description") ?? "",
                    Demographics = GetStr(raw, "demographics") ?? "",
                    Economy = GetStr(raw, "economy") ?? "",
                    PowerStructure = GetStr(raw, "power_structure") ?? "",
                    Dangers = GetList(raw, "dangers"),
                    Opportunities = GetList(raw, "opportunities"),
                    StoryHooks = GetList(raw, "story_hooks"),
                    FrequentedBy = GetList(raw, "frequented_by"),
                    Atmosphere = atmos != null ? new AtmosphereData
                    {
                        Sights = GetList(atmos, "sights"),
                        Sounds = GetList(atmos, "sounds"),
                        Smells = GetList(atmos, "smells"),
                        Feel = GetStr(atmos, "feel") ?? "",
                    } : new(),
                    Connections = conn != null ? new DistrictConnections
                    {
                        AdjacentTo = GetList(conn, "adjacent_to"),
                    } : new(),
                });
            }
            catch { }
        }
        return districts;
    }

    // ── Factions ────────────────────────────────────────

    private static List<FactionData> LoadFactions(string root)
    {
        var factions = new List<FactionData>();
        var dir = Path.Combine(root, "essences", "world", "factions");
        if (!Directory.Exists(dir)) return factions;

        foreach (var file in Directory.GetFiles(dir, "*.yaml"))
        {
            try
            {
                var yaml = File.ReadAllText(file);
                var raw = YamlParser.Deserialize<Dictionary<string, object>>(yaml);
                factions.Add(new FactionData
                {
                    Type = GetStr(raw, "type") ?? "faction",
                    Name = GetStr(raw, "name") ?? Path.GetFileNameWithoutExtension(file),
                    Aliases = GetList(raw, "aliases"),
                    Motto = GetStr(raw, "motto") ?? "",
                    Description = GetStr(raw, "description") ?? "",
                    Ideology = GetStr(raw, "ideology") ?? "",
                    Territory = GetStr(raw, "territory") ?? "",
                    Leadership = GetStr(raw, "leadership") ?? "",
                    Methods = GetList(raw, "methods"),
                    Resources = GetList(raw, "resources"),
                    Goals = GetList(raw, "goals"),
                    NarrativeFunction = GetStr(raw, "narrative_function") ?? "",
                    StoryHooks = GetList(raw, "story_hooks"),
                    Relationships = ParseFactionRelationships(raw),
                });
            }
            catch { }
        }
        return factions;
    }

    private static List<FactionRelationship> ParseFactionRelationships(Dictionary<string, object> d)
    {
        var rels = new List<FactionRelationship>();
        if (!d.TryGetValue("relationships", out var r) || r is not List<object> list) return rels;
        foreach (var item in list)
        {
            if (item is not Dictionary<object, object> rd) continue;
            var dd = rd.ToDictionary(kv => kv.Key.ToString()!, kv => kv.Value);
            rels.Add(new FactionRelationship
            {
                Name = GetStr(dd, "name") ?? "",
                Type = GetStr(dd, "type") ?? "",
                Description = GetStr(dd, "description") ?? "",
            });
        }
        return rels;
    }

    // ── Corponations ────────────────────────────────────

    private static List<CorponationData> LoadCorponations(string root)
    {
        var corps = new List<CorponationData>();
        var dir = Path.Combine(root, "worldbuilding");
        if (!Directory.Exists(dir)) return corps;

        foreach (var file in Directory.GetFiles(dir, "corponations_*.md").OrderBy(f => f))
        {
            var text = File.ReadAllText(file);
            var blocks = Regex.Split(text, @"(?=^## \d+\.)", RegexOptions.Multiline);

            foreach (var block in blocks)
            {
                var m = Regex.Match(block.Trim(), @"^## (\d+)\.\s+(.+?)(?:\s*\n)", RegexOptions.Multiline);
                if (!m.Success) continue;

                corps.Add(new CorponationData
                {
                    Number = int.Parse(m.Groups[1].Value),
                    Name = m.Groups[2].Value.Trim(),
                    FullLegalName = ExtractMdField(block, "Full Legal Name"),
                    CommonNames = ExtractMdField(block, "Common Names").Split(',', StringSplitOptions.TrimEntries).Where(s => s.Length > 0).ToList(),
                    StockDesignation = ExtractMdField(block, "Stock Designation"),
                    Sector = ExtractMdField(block, "Sector"),
                    Valuation = ExtractMdField(block, "Estimated Valuation"),
                    Revenue = ExtractMdField(block, "Annual Revenue"),
                    Employees = ExtractMdField(block, "Total Employees"),
                    SovereignTerritory = ExtractMdField(block, "Sovereign Territory"),
                    FoundingStory = ExtractMdSection(block, "Founding Story"),
                    SecurityForce = ExtractMdField(block, "Security Force"),
                    KeyDetail = ExtractMdField(block, "Key Detail"),
                    RelationshipToBig20 = ExtractMdSection(block, "Relationship"),
                    FullText = block.Trim(),
                });
            }
        }
        return corps.OrderBy(c => c.Number).ToList();
    }

    // ── Worldbuilding Documents ─────────────────────────

    private static List<WorldbuildingDocument> LoadWorldbuildingDocs(string root)
    {
        var docs = new List<WorldbuildingDocument>();
        var dir = Path.Combine(root, "worldbuilding");
        if (!Directory.Exists(dir)) return docs;

        foreach (var file in Directory.GetFiles(dir, "*.md").OrderBy(f => f))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith("ARCHIVED_") || name == "INDEX.md") continue;
            if (name.StartsWith("corponations_")) continue; // Handled separately

            var lines = File.ReadAllLines(file);
            var headings = lines.Where(l => l.StartsWith('#')).Select(l => l.TrimStart('#').Trim()).ToList();
            var title = headings.FirstOrDefault() ?? Path.GetFileNameWithoutExtension(file);

            docs.Add(new WorldbuildingDocument
            {
                FileName = Path.GetFileNameWithoutExtension(file),
                Title = title,
                Category = CategorizeDocument(name),
                Body = string.Join("\n", lines),
                LineCount = lines.Length,
                Headings = headings,
            });
        }
        return docs;
    }

    // ── Story Bible / Literary Rules / Motifs ───────────

    private static StoryBibleData LoadStoryBible(string root)
    {
        var path = Path.Combine(root, "world", "story_bible.yaml");
        if (!File.Exists(path)) return new();
        try
        {
            var raw = YamlParser.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            return new StoryBibleData
            {
                Title = GetStr(raw, "title") ?? "",
                Genre = GetStr(raw, "genre") ?? "",
                Tone = GetStr(raw, "tone") ?? "",
                CoreTheme = GetStr(raw, "core_theme") ?? "",
                CoreHook = GetStr(raw, "core_hook") ?? "",
                Setting = GetStr(raw, "setting") ?? "",
                Protagonist = GetStr(raw, "protagonist") ?? "",
                Arc = GetStr(raw, "arc") ?? "",
                Themes = GetList(raw, "themes"),
            };
        }
        catch { return new(); }
    }

    private static LiteraryRulesData LoadLiteraryRules(string root)
    {
        var path = Path.Combine(root, "world", "literary_rules.yaml");
        if (!File.Exists(path)) return new();
        try
        {
            var raw = YamlParser.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            var structural = GetDict(raw, "structural");
            var facetRules = GetDict(raw, "facet_rules");

            return new LiteraryRulesData
            {
                SentenceMaxWords = GetInt(raw, "sentence_max_words", 25),
                ParagraphRequirements = GetList(raw, "paragraph_requirements"),
                Prohibitions = GetList(raw, "prohibitions"),
                Structural = structural != null ? new StructuralRulesData
                {
                    Pov = GetStr(structural, "pov") ?? "",
                    Location = GetStr(structural, "location") ?? "",
                    Choice = GetStr(structural, "choice") ?? "",
                    Consequence = GetStr(structural, "consequence") ?? "",
                    Ending = GetStr(structural, "ending") ?? "",
                    Pace = GetStr(structural, "pace") ?? "",
                } : new(),
                FacetRules = facetRules != null ? new FacetRulesData
                {
                    Interjections = GetStr(facetRules, "interjections") ?? "",
                    Disagreement = GetStr(facetRules, "disagreement") ?? "",
                    LeadVoice = GetStr(facetRules, "lead_voice") ?? "",
                    Rotation = GetStr(facetRules, "rotation") ?? "",
                } : new(),
            };
        }
        catch { return new(); }
    }

    private static List<MotifData> LoadMotifs(string root)
    {
        var path = Path.Combine(root, "world", "motifs.yaml");
        if (!File.Exists(path)) return [];
        try
        {
            var raw = YamlParser.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            if (!raw.TryGetValue("motifs", out var m) || m is not List<object> list) return [];

            return list.Select(item =>
            {
                if (item is not Dictionary<object, object> d) return null;
                var dd = d.ToDictionary(kv => kv.Key.ToString()!, kv => kv.Value);
                return new MotifData
                {
                    Name = GetStr(dd, "name") ?? "",
                    Description = GetStr(dd, "description") ?? "",
                };
            }).Where(m => m != null).ToList()!;
        }
        catch { return []; }
    }

    private static CharacterProfileData LoadCharacterProfile(string root)
    {
        var path = Path.Combine(root, "character", "profile.yaml");
        if (!File.Exists(path)) return new();
        try
        {
            var raw = YamlParser.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            return new CharacterProfileData
            {
                Name = GetStr(raw, "name") ?? "",
                Title = GetStr(raw, "title") ?? "",
                CoreContradiction = GetStr(raw, "core_contradiction") ?? "",
                Era = GetStr(raw, "era") ?? "",
                Genre = GetStr(raw, "genre") ?? "",
                Arc = GetStr(raw, "arc") ?? "",
                Augmentation = GetStr(raw, "augmentation") ?? "",
                Facets = GetList(raw, "facets"),
            };
        }
        catch { return new(); }
    }

    // ── Helpers ──────────────────────────────────────────

    private static string? GetStr(Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return null;
        return v?.ToString()?.Trim();
    }

    private static int GetInt(Dictionary<string, object> d, string key, int def = 0)
    {
        if (!d.TryGetValue(key, out var v)) return def;
        return int.TryParse(v?.ToString(), out var i) ? i : def;
    }

    private static double GetDouble(Dictionary<string, object> d, string key, double def = 0)
    {
        if (!d.TryGetValue(key, out var v)) return def;
        return double.TryParse(v?.ToString(), out var r) ? r : def;
    }

    private static List<string> GetList(Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return [];
        if (v is List<object> list)
            return list.Select(x => x?.ToString()?.Trim() ?? "").Where(s => s.Length > 0).ToList();
        return [];
    }

    private static Dictionary<string, object>? GetDict(Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return null;
        if (v is Dictionary<object, object> raw)
            return raw.ToDictionary(kv => kv.Key.ToString()!, kv => kv.Value);
        if (v is Dictionary<string, object> typed) return typed;
        return null;
    }

    private static string ExtractMdField(string text, string label)
    {
        var m = Regex.Match(text, $@"\*\*{Regex.Escape(label)}[^*]*\*\*[:\s]*(.+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private static string ExtractMdSection(string text, string heading)
    {
        var m = Regex.Match(text, $@"###?\s+{Regex.Escape(heading)}.*?\n([\s\S]+?)(?=\n###?\s|\z)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private static string CategorizeDocument(string fileName) => fileName switch
    {
        _ when fileName.StartsWith("corponations") => "Power Structures",
        _ when fileName.Contains("culture") => "Culture",
        _ when fileName.Contains("arsenal") || fileName.Contains("weapon") => "Violence",
        _ when fileName.Contains("drug") || fileName.Contains("biotech") || fileName.Contains("medical") => "Medicine",
        _ when fileName.Contains("bci") || fileName.Contains("augment") || fileName.Contains("cyber") => "Technology",
        _ when fileName.Contains("rogue") => "AI",
        _ when fileName.Contains("lake") || fileName.Contains("megalopolis") || fileName.Contains("depth") => "Places",
        _ when fileName.Contains("exclusion") || fileName.Contains("labor") || fileName.Contains("criminal") || fileName.Contains("law") => "Social Control",
        _ => "Foundations",
    };
}
