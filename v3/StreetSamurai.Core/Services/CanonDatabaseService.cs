using System.Text.Json;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Typed canon database. Reads from canon.json — no YAML parsing, no regex, no guessing.
/// Auto-rebuilds from source files when they change.
/// </summary>
public class CanonDatabaseService
{
    private readonly ICanonPathProvider _paths;
    private CanonDatabase? _db;
    private readonly object _lock = new();
    private string CanonJsonPath => Path.Combine(_paths.EngineDataDir, "canon.json");

    public CanonDatabaseService(ICanonPathProvider paths)
    {
        _paths = paths;
    }

    /// <summary>
    /// Get the loaded database. Builds/rebuilds if needed.
    /// </summary>
    public CanonDatabase Db
    {
        get
        {
            if (_db != null) return _db;
            lock (_lock)
            {
                if (_db != null) return _db;
                EnsureLoaded();
                return _db!;
            }
        }
    }

    public void EnsureLoaded()
    {
        var root = _paths.CanonRoot;
        var jsonPath = CanonJsonPath;

        if (CanonConverter.NeedsRebuild(root, jsonPath))
        {
            CanonConverter.BuildAndSave(root, jsonPath);
        }

        var json = File.ReadAllText(jsonPath);
        _db = JsonSerializer.Deserialize<CanonDatabase>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        }) ?? new();
    }

    public void ForceRebuild()
    {
        CanonConverter.BuildAndSave(_paths.CanonRoot, CanonJsonPath);
        _db = null;
        EnsureLoaded();
    }

    // ── Typed Accessors ─────────────────────────────────

    public List<CharacterData> Characters => Db.Characters;
    public List<FacetData> Facets => Db.Facets;
    public List<DistrictData> Districts => Db.Districts;
    public List<FactionData> Factions => Db.Factions;
    public List<CorponationData> Corponations => Db.Corponations;
    public List<WorldbuildingDocument> WorldbuildingDocs => Db.WorldbuildingDocs;
    public StoryBibleData StoryBible => Db.StoryBible;
    public LiteraryRulesData LiteraryRules => Db.LiteraryRules;
    public List<MotifData> Motifs => Db.Motifs;
    public CharacterProfileData CharacterProfile => Db.CharacterProfile;

    // ── Character Lookups ───────────────────────────────

    public CharacterData? FindCharacter(string nameOrAlias)
    {
        return Characters.FirstOrDefault(c =>
            c.Name.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)
            || c.Aliases.Any(a => a.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)));
    }

    public FacetWeights GetBlendedWeights(List<string> characterNames)
    {
        var weights = characterNames
            .Select(FindCharacter)
            .Where(c => c != null && (c.Psychology.FacetWeights.Wound > 0 || c.Psychology.FacetWeights.Ideal > 0))
            .Select(c => c!.Psychology.FacetWeights)
            .ToList();

        if (weights.Count == 0)
            return new FacetWeights { Wound = 0.6, Ideal = 0.5, Id = 0.4, Shadow = 0.55, Mask = 0.45, Ghost = 0.5 };

        return new FacetWeights
        {
            Wound = weights.Average(w => w.Wound),
            Ideal = weights.Average(w => w.Ideal),
            Id = weights.Average(w => w.Id),
            Shadow = weights.Average(w => w.Shadow),
            Mask = weights.Average(w => w.Mask),
            Ghost = weights.Average(w => w.Ghost),
        };
    }

    /// <summary>
    /// Builds a rich LLM prompt context for a character — psychology, speech, relationships.
    /// </summary>
    public string GetCharacterContext(string nameOrAlias)
    {
        var c = FindCharacter(nameOrAlias);
        if (c == null) return "";

        var lines = new List<string> { $"CHARACTER: {c.Name}" };
        if (c.Role.Length > 0) lines.Add($"ROLE: {c.Role}");
        if (c.Description.Length > 0) lines.Add($"DESCRIPTION: {Trunc(c.Description, 600)}");

        var p = c.Psychology;
        if (p.CoreFears.Any()) lines.Add($"CORE FEARS: {string.Join("; ", p.CoreFears)}");
        if (p.CoreDesires.Any()) lines.Add($"CORE DESIRES: {string.Join("; ", p.CoreDesires)}");
        if (p.CopingMechanisms.Any()) lines.Add($"COPING MECHANISMS: {string.Join("; ", p.CopingMechanisms)}");
        if (p.BlindSpots.Any()) lines.Add($"BLIND SPOTS: {string.Join("; ", p.BlindSpots)}");
        if (p.Secret.Length > 0) lines.Add($"SECRET: {p.Secret}");

        var fw = p.FacetWeights;
        lines.Add($"FACET WEIGHTS: wound={fw.Wound:F2} ideal={fw.Ideal:F2} id={fw.Id:F2} shadow={fw.Shadow:F2} mask={fw.Mask:F2} ghost={fw.Ghost:F2}");

        var sp = c.SpeechPatterns;
        if (sp.Vocabulary.Length > 0) lines.Add($"VOCABULARY: {sp.Vocabulary}");
        if (sp.Cadence.Length > 0) lines.Add($"CADENCE: {sp.Cadence}");
        if (sp.ExampleLines.Any()) lines.Add($"EXAMPLE DIALOGUE:\n{string.Join("\n", sp.ExampleLines.Select(l => $"  \"{l}\""))}");

        if (c.Relationships.Any())
        {
            lines.Add("RELATIONSHIPS:");
            foreach (var r in c.Relationships)
            {
                var line = $"  [{r.Type}] {r.Name}: {r.Description}";
                if (r.EmotionalCore.Length > 0) line += $" (core: {r.EmotionalCore})";
                lines.Add(line);
            }
        }

        if (c.NarrativeFunction.Length > 0) lines.Add($"NARRATIVE FUNCTION: {c.NarrativeFunction}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Builds rich location context for LLM prompts — description, atmosphere, connections.
    /// </summary>
    public string GetDistrictContext(string nameOrAlias)
    {
        var d = Districts.FirstOrDefault(x =>
            x.Name.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)
            || x.Aliases.Any(a => a.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)));
        if (d == null) return "";

        var lines = new List<string> { $"LOCATION: {d.Name}" };
        if (d.Description.Length > 0) lines.Add(Trunc(d.Description, 800));

        var a = d.Atmosphere;
        if (a.Sights.Any()) lines.Add($"SIGHTS: {string.Join("; ", a.Sights.Take(5))}");
        if (a.Sounds.Any()) lines.Add($"SOUNDS: {string.Join("; ", a.Sounds.Take(5))}");
        if (a.Smells.Any()) lines.Add($"SMELLS: {string.Join("; ", a.Smells.Take(4))}");
        if (a.Feel.Length > 0) lines.Add($"FEEL: {a.Feel}");
        if (d.Dangers.Any()) lines.Add($"DANGERS: {string.Join("; ", d.Dangers.Take(4))}");
        if (d.FrequentedBy.Any()) lines.Add($"FREQUENTED BY: {string.Join(", ", d.FrequentedBy.Take(6))}");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Full literary rules + motifs as a prompt string.
    /// </summary>
    public string GetLiteraryRulesPrompt()
    {
        var rules = LiteraryRules;
        var lines = new List<string>();

        lines.Add($"SENTENCE MAX: {rules.SentenceMaxWords} words");
        if (rules.ParagraphRequirements.Any())
            lines.Add($"PARAGRAPH REQUIREMENTS: {string.Join("; ", rules.ParagraphRequirements)}");
        if (rules.Prohibitions.Any())
            lines.Add($"PROHIBITIONS: {string.Join("; ", rules.Prohibitions)}");

        var s = rules.Structural;
        if (s.Pov.Length > 0) lines.Add($"POV: {s.Pov}");
        if (s.Pace.Length > 0) lines.Add($"PACE: {s.Pace}");
        if (s.Ending.Length > 0) lines.Add($"ENDING: {s.Ending}");

        var f = rules.FacetRules;
        if (f.LeadVoice.Length > 0) lines.Add($"LEAD VOICE: {f.LeadVoice}");
        if (f.Rotation.Length > 0) lines.Add($"ROTATION: {f.Rotation}");
        if (f.Interjections.Length > 0) lines.Add($"INTERJECTIONS: {f.Interjections}");

        if (Motifs.Any())
        {
            lines.Add("MOTIFS:");
            foreach (var m in Motifs)
                lines.Add($"  - {m.Name}: {m.Description}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Full-text search across worldbuilding documents.
    /// </summary>
    public List<SearchResult> Search(string query, int maxResults = 20)
    {
        var results = new List<SearchResult>();
        foreach (var doc in WorldbuildingDocs)
        {
            var lines = doc.Body.Split('\n');
            var currentHeading = "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith('#')) currentHeading = lines[i].TrimStart('#').Trim();
                if (!lines[i].Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

                var start = Math.Max(0, i - 1);
                var end = Math.Min(lines.Length, i + 2);
                results.Add(new SearchResult
                {
                    FileName = doc.FileName,
                    Heading = currentHeading,
                    LineNumber = i + 1,
                    Context = string.Join("\n", lines[start..end]),
                });
                if (results.Count >= maxResults) return results;
            }
        }
        return results;
    }

    private static string Trunc(string s, int max) =>
        s.Length > max ? s[..(max - 3)] + "..." : s;
}
