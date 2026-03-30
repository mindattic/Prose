using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Provides read-only access to the canon vault (worldbuilding docs, characters, essences).
/// This is the primary lore browser service — text search, document listing, and corp index.
/// </summary>
public class CanonService
{
    private readonly ICanonPathProvider _paths;
    private List<Corponation>? _corpCache;

    public CanonService(ICanonPathProvider paths)
    {
        _paths = paths;
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

                if (!lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;

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

        if (string.IsNullOrWhiteSpace(filter))
            return _corpCache;

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

    public void InvalidateCache() => _corpCache = null;

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
        var path = Path.Combine(_paths.WorldbuildingDir, "..", "world", fileName);
        if (File.Exists(path)) return File.ReadAllText(path);
        // Also check the _paths.WorldDir if different
        return null;
    }

    public List<(string Name, string Content)> ListWorldRuleFiles()
    {
        var worldDir = Path.Combine(_paths.WorldbuildingDir, "..", "world");
        if (!Directory.Exists(worldDir)) return [];
        return Directory.GetFiles(worldDir, "*.yaml")
            .Select(f => (Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
            .OrderBy(x => x.Item1)
            .ToList();
    }

    // ── Character YAML ──────────────────────────────────────

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

    // ── Characters ──────────────────────────────────────────

    public List<Character> ListCharacters()
    {
        var characters = new List<Character>();

        foreach (var dir in new[] { _paths.CharactersDir, _paths.EssencesDir })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories))
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    // Basic YAML extraction — enough for listing
                    var name = ExtractYamlField(yaml, "name") ?? Path.GetFileNameWithoutExtension(file);
                    var type = ExtractYamlField(yaml, "type") ?? "character";

                    if (!type.Contains("character", StringComparison.OrdinalIgnoreCase)
                        && !type.Contains("npc", StringComparison.OrdinalIgnoreCase)
                        && !type.Contains("protagonist", StringComparison.OrdinalIgnoreCase))
                        continue;

                    characters.Add(new Character
                    {
                        Name = name,
                        Status = ExtractYamlField(yaml, "status") ?? "",
                        Affiliation = ExtractYamlField(yaml, "affiliation") ?? "",
                        SourceFile = Path.GetRelativePath(_paths.WorldbuildingDir + "/..", file),
                    });
                }
                catch { /* skip malformed files */ }
            }
        }

        return characters.OrderBy(c => c.Name).ToList();
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
        var m = Regex.Match(yaml, $@"^{Regex.Escape(field)}:\s*""?(.+?)""?\s*$", RegexOptions.Multiline);
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
