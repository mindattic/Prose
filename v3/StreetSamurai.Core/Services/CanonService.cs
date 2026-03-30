using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class CanonService
{
    private readonly ICanonPathProvider _paths;
    private readonly YamlService _yaml;
    private List<Corponation>? _corpCache;
    private List<Character>? _charCache;

    public CanonService(ICanonPathProvider paths, YamlService yaml)
    {
        _paths = paths;
        _yaml = yaml;
    }

    // ── Documents ────────────────────────────────────────────

    public List<CanonDocument> ListDocuments()
    {
        var dir = _paths.WorldbuildingDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.md")
            .Where(f => !Path.GetFileName(f).StartsWith("ARCHIVED_"))
            .OrderBy(f => f)
            .Select(f => new CanonDocument
            {
                FileName = Path.GetFileNameWithoutExtension(f),
                Title = ExtractTitle(f),
                LineCount = File.ReadAllLines(f).Length,
                FullPath = f,
                Category = CategorizeDocument(Path.GetFileName(f)),
            })
            .ToList();
    }

    public string? ReadDocument(string nameOrPartial)
    {
        var dir = _paths.WorldbuildingDir;
        if (!Directory.Exists(dir)) return null;
        var match = Directory.GetFiles(dir, "*.md")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                .Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        return match != null ? File.ReadAllText(match) : null;
    }

    // ── Text Search ─────────────────────────────────────────

    public List<SearchResult> Search(string query, int maxResults = 20)
    {
        var results = new List<SearchResult>();
        var dir = _paths.WorldbuildingDir;
        if (!Directory.Exists(dir)) return results;

        foreach (var file in Directory.GetFiles(dir, "*.md").OrderBy(f => f))
        {
            if (Path.GetFileName(file).StartsWith("ARCHIVED_")) continue;
            var lines = File.ReadAllLines(file);
            var currentHeading = "";

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith('#'))
                    currentHeading = lines[i].TrimStart('#').Trim();
                if (!lines[i].Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

                var start = Math.Max(0, i - 1);
                var end = Math.Min(lines.Length, i + 2);
                var context = string.Join("\n", lines[start..end]);

                results.Add(new SearchResult
                {
                    FileName = Path.GetFileName(file),
                    Heading = currentHeading,
                    LineNumber = i + 1,
                    Context = context.Length > 500 ? context[..500] + "..." : context,
                });
                if (results.Count >= maxResults) return results;
            }
        }
        return results;
    }

    // ── Corponations ────────────────────────────────────────

    public List<Corponation> ListCorponations(string? filter = null)
    {
        _corpCache ??= BuildCorpIndex();
        if (string.IsNullOrWhiteSpace(filter)) return _corpCache;
        var ft = filter.ToLowerInvariant();
        return _corpCache
            .Where(c => c.Name.Contains(ft, StringComparison.OrdinalIgnoreCase)
                     || c.Sector.Contains(ft, StringComparison.OrdinalIgnoreCase)
                     || c.Number.ToString() == ft)
            .ToList();
    }

    public Corponation? GetCorponation(string identifier)
    {
        _corpCache ??= BuildCorpIndex();
        if (int.TryParse(identifier, out var num))
            return _corpCache.FirstOrDefault(c => c.Number == num);
        var ft = identifier.ToLowerInvariant();
        return _corpCache.FirstOrDefault(c =>
            c.Name.Contains(ft, StringComparison.OrdinalIgnoreCase));
    }

    public void InvalidateCache() { _corpCache = null; _charCache = null; }

    // ── Factions ─────────────────────────────────────────────

    public List<Faction> ListFactions()
    {
        var dir = Path.Combine(_paths.EssencesDir, "world", "factions");
        if (!Directory.Exists(dir)) return [];
        return Directory.GetFiles(dir, "*.yaml")
            .Select(f =>
            {
                try
                {
                    var yaml = File.ReadAllText(f);
                    return new Faction
                    {
                        Name = ExtractYamlField(yaml, "name") ?? Path.GetFileNameWithoutExtension(f),
                        Type = ExtractYamlField(yaml, "type") ?? "faction",
                        Description = ExtractYamlBlock(yaml, "description"),
                        Ideology = ExtractYamlBlock(yaml, "ideology"),
                        Territory = ExtractYamlField(yaml, "territory") ?? "",
                        Leadership = ExtractYamlField(yaml, "leadership") ?? "",
                        SourceFile = f,
                    };
                }
                catch { return null; }
            })
            .Where(f => f != null)
            .OrderBy(f => f!.Name)
            .ToList()!;
    }

    public string? ReadFactionYaml(string nameOrPartial)
    {
        var dir = Path.Combine(_paths.EssencesDir, "world", "factions");
        if (!Directory.Exists(dir)) return null;
        var match = Directory.GetFiles(dir, "*.yaml")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                .Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        return match != null ? File.ReadAllText(match) : null;
    }

    // ── Districts ───────────────────────────────────────────

    public List<District> ListDistricts()
    {
        var dir = Path.Combine(_paths.EssencesDir, "world", "districts");
        if (!Directory.Exists(dir)) return [];
        return Directory.GetFiles(dir, "*.yaml")
            .Select(f =>
            {
                try
                {
                    var yaml = File.ReadAllText(f);
                    return new District
                    {
                        Name = ExtractYamlField(yaml, "name") ?? Path.GetFileNameWithoutExtension(f),
                        Type = ExtractYamlField(yaml, "type") ?? "place",
                        Description = ExtractYamlBlock(yaml, "description"),
                        SourceFile = f,
                    };
                }
                catch { return null; }
            })
            .Where(d => d != null)
            .OrderBy(d => d!.Name)
            .ToList()!;
    }

    public string? ReadDistrictYaml(string nameOrPartial)
    {
        var dir = Path.Combine(_paths.EssencesDir, "world", "districts");
        if (!Directory.Exists(dir)) return null;
        var match = Directory.GetFiles(dir, "*.yaml")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                .Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        return match != null ? File.ReadAllText(match) : null;
    }

    // ── Technology ──────────────────────────────────────────

    public string? ReadTechnology()
    {
        var path = Path.Combine(_paths.EssencesDir, "world", "technology.yaml");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    // ── World Rules ─────────────────────────────────────────

    public string? ReadWorldFile(string fileName)
    {
        var path = Path.Combine(_paths.WorldDir, fileName);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public List<(string Name, string Content)> ListWorldRuleFiles()
    {
        var worldDir = _paths.WorldDir;
        if (!Directory.Exists(worldDir)) return [];
        return Directory.GetFiles(worldDir, "*.yaml")
            .Select(f => (Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
            .OrderBy(x => x.Item1)
            .ToList();
    }

    // ── Character YAML (raw) ────────────────────────────────

    public string? ReadCharacterYaml(string nameOrPartial)
    {
        foreach (var dir in new[] { _paths.CharactersDir, Path.Combine(_paths.EssencesDir, "characters") })
        {
            if (!Directory.Exists(dir)) continue;
            var match = Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                    .Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
            if (match != null) return File.ReadAllText(match);
        }
        return null;
    }

    // ── Characters (fully parsed) ───────────────────────────

    public List<Character> ListCharacters()
    {
        if (_charCache != null) return _charCache;

        var characters = new List<Character>();
        foreach (var dir in new[] { _paths.CharactersDir, Path.Combine(_paths.EssencesDir, "characters") })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories))
            {
                try
                {
                    var data = _yaml.LoadDynamic(file);
                    var type = GetStr(data, "type") ?? "character";
                    if (!type.Contains("character", StringComparison.OrdinalIgnoreCase)
                        && !type.Contains("npc", StringComparison.OrdinalIgnoreCase)
                        && !type.Contains("protagonist", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var c = ParseCharacter(data, file);
                    characters.Add(c);
                }
                catch { /* skip malformed files */ }
            }
        }

        // Also scan essences root for character-typed files
        if (Directory.Exists(_paths.EssencesDir))
        {
            foreach (var file in Directory.GetFiles(_paths.EssencesDir, "*.yaml"))
            {
                try
                {
                    var data = _yaml.LoadDynamic(file);
                    var type = GetStr(data, "type") ?? "";
                    if (!type.Contains("character", StringComparison.OrdinalIgnoreCase)) continue;
                    if (characters.Any(c => c.SourceFile == file)) continue;
                    characters.Add(ParseCharacter(data, file));
                }
                catch { }
            }
        }

        _charCache = characters.OrderBy(c => c.Name).ToList();
        return _charCache;
    }

    private Character ParseCharacter(Dictionary<string, object> data, string file)
    {
        var name = GetStr(data, "name") ?? Path.GetFileNameWithoutExtension(file);

        // Parse psychology.facet_weights
        var facets = new FacetState();
        if (data.TryGetValue("psychology", out var psych) && psych is Dictionary<object, object> psychDict)
        {
            if (psychDict.TryGetValue("facet_weights", out var fw) && fw is Dictionary<object, object> fwDict)
            {
                facets = new FacetState
                {
                    Wound = ParseDouble(fwDict, "wound"),
                    Ideal = ParseDouble(fwDict, "ideal"),
                    Id = ParseDouble(fwDict, "id"),
                    Shadow = ParseDouble(fwDict, "shadow"),
                    Mask = ParseDouble(fwDict, "mask"),
                    Ghost = ParseDouble(fwDict, "ghost"),
                };
            }
        }

        // Parse relationships
        var relationships = new List<Relationship>();
        if (data.TryGetValue("relationships", out var rels) && rels is List<object> relList)
        {
            foreach (var rel in relList)
            {
                if (rel is not Dictionary<object, object> rd) continue;
                relationships.Add(new Relationship
                {
                    Name = rd.GetValueOrDefault("name")?.ToString() ?? "",
                    Status = rd.GetValueOrDefault("type")?.ToString() ?? "",
                    FacetConnection = "",
                    Notes = rd.GetValueOrDefault("description")?.ToString() ?? "",
                });
            }
        }

        // Parse aliases
        var aliases = new List<string>();
        if (data.TryGetValue("aliases", out var al) && al is List<object> alList)
            aliases = alList.Select(a => a.ToString() ?? "").Where(a => a.Length > 0).ToList();

        // Parse history beats from story_hooks
        var history = new List<HistoryBeat>();
        if (data.TryGetValue("story_hooks", out var sh) && sh is List<object> shList)
            history = shList.Select((h, i) => new HistoryBeat { Event = h.ToString() ?? "", Age = i }).ToList();

        return new Character
        {
            Name = name,
            Aliases = aliases,
            Tier = int.TryParse(GetStr(data, "tier"), out var t) ? t : 0,
            Status = GetStr(data, "status") ?? "",
            Origin = GetStr(data, "origin") ?? GetBlock(data, "origins"),
            Age = int.TryParse(GetStr(data, "age"), out var a) ? a : 0,
            Augmentation = GetBlock(data, "augmentations") != "" ? GetBlock(data, "augmentations") : "",
            Occupation = GetStr(data, "role") ?? GetStr(data, "occupation") ?? "",
            Affiliation = GetStr(data, "affiliation") ?? "",
            Facets = facets,
            Relationships = relationships,
            VoiceNotes = GetBlock(data, "speech_patterns"),
            SourceFile = Path.GetRelativePath(_paths.WorldbuildingDir + "/..", file),
            History = history,
        };
    }

    /// <summary>
    /// Returns the full character psychology context for LLM prompts —
    /// fears, desires, coping mechanisms, blind spots, speech patterns.
    /// This is what makes each character distinct in generation.
    /// </summary>
    public string GetCharacterPsychologyContext(string nameOrAlias)
    {
        var yaml = ReadCharacterYaml(nameOrAlias);
        if (yaml == null) return "";

        var sections = new List<string>();

        // Extract key psychological sections directly from YAML text
        var name = ExtractYamlField(yaml, "name") ?? nameOrAlias;
        sections.Add($"CHARACTER: {name}");

        var desc = ExtractYamlBlock(yaml, "description");
        if (desc.Length > 0) sections.Add($"DESCRIPTION:\n{Truncate(desc, 600)}");

        var role = ExtractYamlField(yaml, "role");
        if (role != null) sections.Add($"ROLE: {role}");

        // Psychology — the behavioral baseline
        var coreFears = ExtractYamlList(yaml, "core_fears");
        if (coreFears.Any()) sections.Add($"CORE FEARS:\n{string.Join("\n", coreFears.Select(f => $"  - {f}"))}");

        var coreDesires = ExtractYamlList(yaml, "core_desires");
        if (coreDesires.Any()) sections.Add($"CORE DESIRES:\n{string.Join("\n", coreDesires.Select(d => $"  - {d}"))}");

        var coping = ExtractYamlList(yaml, "coping_mechanisms");
        if (coping.Any()) sections.Add($"COPING MECHANISMS:\n{string.Join("\n", coping.Select(c => $"  - {c}"))}");

        var blindSpots = ExtractYamlList(yaml, "blind_spots");
        if (blindSpots.Any()) sections.Add($"BLIND SPOTS:\n{string.Join("\n", blindSpots.Select(b => $"  - {b}"))}");

        var secret = ExtractYamlBlock(yaml, "secret");
        if (secret.Length > 0) sections.Add($"SECRET:\n{secret}");

        // Speech patterns — how they talk
        var vocab = ExtractYamlField(yaml, "vocabulary");
        if (vocab != null) sections.Add($"VOCABULARY: {vocab}");

        var cadence = ExtractYamlField(yaml, "cadence");
        if (cadence != null) sections.Add($"CADENCE: {cadence}");

        var exampleLines = ExtractYamlList(yaml, "example_lines");
        if (exampleLines.Any()) sections.Add($"EXAMPLE DIALOGUE:\n{string.Join("\n", exampleLines.Select(l => $"  \"{l}\""))}");

        // Narrative function
        var narrativeFunc = ExtractYamlBlock(yaml, "narrative_function");
        if (narrativeFunc.Length > 0) sections.Add($"NARRATIVE FUNCTION:\n{narrativeFunc}");

        return string.Join("\n\n", sections);
    }

    // ── Helpers ──────────────────────────────────────────────

    private List<Corponation> BuildCorpIndex()
    {
        var corps = new List<Corponation>();
        var dir = _paths.WorldbuildingDir;
        if (!Directory.Exists(dir)) return corps;

        foreach (var file in Directory.GetFiles(dir, "corponations_*.md").OrderBy(f => f))
        {
            var text = File.ReadAllText(file);
            var blocks = Regex.Split(text, @"\n(?=###?\s+(?:\d+\.?\s+)?[A-Z]|\*\*\d+\.\s)");

            foreach (var block in blocks)
            {
                var m = Regex.Match(block.Trim(), @"(?:###?\s*)?(?:\*\*)?(\d+)\.?\s*(.+?)(?:\*\*)?\s*\n");
                if (!m.Success) continue;

                corps.Add(new Corponation
                {
                    Number = int.Parse(m.Groups[1].Value),
                    Name = m.Groups[2].Value.Trim().TrimEnd('*'),
                    Sector = ExtractMarkdownField(block, "Sector"),
                    Valuation = ExtractMarkdownField(block, "Valuation"),
                    Origin = Truncate(ExtractMarkdownField(block, "Origin"), 300),
                    Territory = Truncate(ExtractMarkdownField(block, "Territory"), 300),
                    SecurityForce = Truncate(ExtractMarkdownField(block, "Security Force"), 300),
                    KeyDetail = Truncate(ExtractMarkdownField(block, "Key Detail"), 300),
                    RelationshipToBig20 = Truncate(ExtractMarkdownField(block, "Relationship to Big 20"), 300),
                    SourceFile = Path.GetFileName(file),
                    FullText = block.Trim(),
                });
            }
        }
        return corps.OrderBy(c => c.Number).ToList();
    }

    private static string ExtractMarkdownField(string text, string field)
    {
        var m = Regex.Match(text, $@"\*\*{Regex.Escape(field)}:\*\*\s*(.+)");
        return m.Success ? m.Groups[1].Value.Trim() : "";
    }

    private static string? ExtractYamlField(string yaml, string field)
    {
        var m = Regex.Match(yaml, $@"^  {Regex.Escape(field)}:\s*""?(.+?)""?\s*$", RegexOptions.Multiline);
        if (m.Success) return m.Groups[1].Value.Trim().Trim('"');
        m = Regex.Match(yaml, $@"^{Regex.Escape(field)}:\s*""?(.+?)""?\s*$", RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value.Trim().Trim('"') : null;
    }

    private static string ExtractYamlBlock(string yaml, string field)
    {
        var m = Regex.Match(yaml, $@"^{Regex.Escape(field)}:\s*\|?\s*\n((?:\s+.+\n?)+)", RegexOptions.Multiline);
        if (!m.Success) return ExtractYamlField(yaml, field) ?? "";
        return string.Join("\n", m.Groups[1].Value
            .Split('\n')
            .Select(l => l.TrimStart())
            .Where(l => !string.IsNullOrWhiteSpace(l)));
    }

    private static List<string> ExtractYamlList(string yaml, string field)
    {
        var m = Regex.Match(yaml, $@"^  {Regex.Escape(field)}:\s*\n((?:\s+- .+\n?)+)", RegexOptions.Multiline);
        if (!m.Success) return [];
        return m.Groups[1].Value
            .Split('\n')
            .Select(l => l.Trim().TrimStart('-').Trim().Trim('"'))
            .Where(l => l.Length > 0)
            .ToList();
    }

    private static string? GetStr(Dictionary<string, object> d, string key) =>
        d.TryGetValue(key, out var v) ? v?.ToString()?.Trim().Trim('"') : null;

    private static string GetBlock(Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return "";
        return v?.ToString()?.Trim() ?? "";
    }

    private static double ParseDouble(Dictionary<object, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return 0;
        return double.TryParse(v?.ToString(), out var r) ? r : 0;
    }

    private static string ExtractTitle(string path)
    {
        using var reader = new StreamReader(path);
        var firstLine = reader.ReadLine() ?? "";
        return firstLine.TrimStart('#').Trim();
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

    private static string Truncate(string s, int max) =>
        s.Length > max ? s[..(max - 3)] + "..." : s;
}
