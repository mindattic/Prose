using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Aggregates data from individual typed JSON repositories.
/// Keeps the same public API so downstream services (StoryStarterService,
/// SceneGenerationService, WorldGraphService) don't break.
/// </summary>
public class CanonDatabaseService
{
    private readonly CharacterRepository _characters;
    private readonly FacetRepository _facets;
    private readonly DistrictRepository _districts;
    private readonly FactionRepository _factions;
    private readonly CorponationRepository _corponations;
    private readonly WorldbuildingDocRepository _docs;
    private readonly StoryBibleRepository _storyBible;
    private readonly LiteraryRulesRepository _literaryRules;
    private readonly MotifRepository _motifs;
    private readonly CharacterProfileRepository _characterProfile;

    public CanonDatabaseService(
        CharacterRepository characters, FacetRepository facets,
        DistrictRepository districts, FactionRepository factions,
        CorponationRepository corponations, WorldbuildingDocRepository docs,
        StoryBibleRepository storyBible, LiteraryRulesRepository literaryRules,
        MotifRepository motifs, CharacterProfileRepository characterProfile)
    {
        _characters = characters;
        _facets = facets;
        _districts = districts;
        _factions = factions;
        _corponations = corponations;
        _docs = docs;
        _storyBible = storyBible;
        _literaryRules = literaryRules;
        _motifs = motifs;
        _characterProfile = characterProfile;
    }

    // ── Typed Accessors ─────────────────────────────────

    public List<CharacterData> Characters => _characters.GetAll();
    public List<FacetData> Facets => _facets.GetAll();
    public List<DistrictData> Districts => _districts.GetAll();
    public List<FactionData> Factions => _factions.GetAll();
    public List<CorponationData> Corponations => _corponations.GetAll();
    public List<WorldbuildingDocument> WorldbuildingDocs => _docs.GetAll();
    public StoryBibleData StoryBible => _storyBible.Get();
    public LiteraryRulesData LiteraryRules => _literaryRules.Get();
    public List<MotifData> Motifs => _motifs.GetAll();
    public CharacterProfileData CharacterProfile => _characterProfile.Get();

    public void Reload()
    {
        _characters.Reload();
        _facets.Reload();
        _districts.Reload();
        _factions.Reload();
        _corponations.Reload();
        _docs.Reload();
        _storyBible.Reload();
        _literaryRules.Reload();
        _motifs.Reload();
        _characterProfile.Reload();
    }

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
