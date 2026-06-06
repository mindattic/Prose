using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Aggregates data from individual typed JSON repositories.
/// Keeps the same public API so downstream services (StoryStarterService,
/// SceneGenerationService, WorldGraphService) don't break.
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly CharacterRepository characters;
    private readonly DistrictRepository districts;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;
    private readonly WorldbuildingDocRepository docs;
    private readonly WeaponryRepository weaponry;
    private readonly EquipmentRepository equipment;
    private readonly TechnologyRepository technology;
    private readonly StoryBibleRepository storyBible;
    private readonly LiteraryRulesRepository literaryRules;
    private readonly MotifRepository motifs;
    private readonly CharacterProfileRepository characterProfile;
    private readonly ToneBibleRepository toneBible;

    public DatabaseService(
        CharacterRepository characters,
        DistrictRepository districts, FactionRepository factions,
        CorponationRepository corponations, WorldbuildingDocRepository docs,
        WeaponryRepository weaponry, EquipmentRepository equipment,
        TechnologyRepository technology,
        StoryBibleRepository storyBible, LiteraryRulesRepository literaryRules,
        MotifRepository motifs, CharacterProfileRepository characterProfile,
        ToneBibleRepository toneBible)
    {
        this.characters = characters;
        this.districts = districts;
        this.factions = factions;
        this.corponations = corponations;
        this.docs = docs;
        this.weaponry = weaponry;
        this.equipment = equipment;
        this.technology = technology;
        this.storyBible = storyBible;
        this.literaryRules = literaryRules;
        this.motifs = motifs;
        this.characterProfile = characterProfile;
        this.toneBible = toneBible;
    }

    // ── Typed Accessors ─────────────────────────────────

    public List<CharacterData> Characters => characters.GetAll();
    public List<DistrictData> Districts => districts.GetAll();
    public List<FactionData> Factions => factions.GetAll();
    public List<CorponationData> Corponations => corponations.GetAll();
    public List<WeaponryData> Weaponry => weaponry.GetAll();
    public List<EquipmentData> Equipment => equipment.GetAll();
    public List<TechnologyData> Technology => technology.GetAll();
    public List<WorldbuildingDocument> WorldbuildingDocs => docs.GetAll();
    public StoryBibleData StoryBible => storyBible.Get();
    public LiteraryRulesData LiteraryRules => literaryRules.Get();
    public List<MotifData> Motifs => motifs.GetAll();
    public CharacterProfileData CharacterProfile => characterProfile.Get();

    public void Reload()
    {
        characters.Reload();
        districts.Reload();
        factions.Reload();
        corponations.Reload();
        weaponry.Reload();
        equipment.Reload();
        technology.Reload();
        docs.Reload();
        storyBible.Reload();
        literaryRules.Reload();
        motifs.Reload();
        characterProfile.Reload();
    }

    // ── Character Lookups ───────────────────────────────

    public CharacterData? FindCharacter(string nameOrAlias)
    {
        return Characters.FirstOrDefault(c =>
            c.Name.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)
            || c.Aliases.Any(a => a.Equals(nameOrAlias, StringComparison.OrdinalIgnoreCase)));
    }

    public string GetCharacterContext(string nameOrAlias)
    {
        var c = FindCharacter(nameOrAlias);
        if (c == null) return "";

        var lines = new List<string> { $"CHARACTER: {c.Name}" };
        if (c.Gender.Length > 0) lines.Add($"GENDER: {c.Gender}");
        if (c.Pronouns.Length > 0) lines.Add($"PRONOUNS: {c.Pronouns}");
        if (c.Role.Length > 0) lines.Add($"ROLE: {c.Role}");
        if (c.Description.Length > 0) lines.Add($"DESCRIPTION: {Trunc(c.Description, 600)}");

        var p = c.Psychology;
        if (p.CoreFears.Any()) lines.Add($"CORE FEARS: {string.Join("; ", p.CoreFears)}");
        if (p.CoreDesires.Any()) lines.Add($"CORE DESIRES: {string.Join("; ", p.CoreDesires)}");
        if (p.CopingMechanisms.Any()) lines.Add($"COPING MECHANISMS: {string.Join("; ", p.CopingMechanisms)}");
        if (p.BlindSpots.Any()) lines.Add($"BLIND SPOTS: {string.Join("; ", p.BlindSpots)}");
        if (p.Secret.Length > 0) lines.Add($"SECRET: {p.Secret}");

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

        // Behavioral patterns — concrete rules for how this character acts
        var b = c.Behavioral;
        if (b.DecisionRules.Any())
            lines.Add($"DECISION RULES:\n{string.Join("\n", b.DecisionRules.Select(r => $"  - {r}"))}");
        if (b.EscalationLadder.Any())
            lines.Add($"ESCALATION:\n{string.Join("\n", b.EscalationLadder.Select(s => $"  {s}"))}");
        if (b.InterpersonalModes.Any())
        {
            lines.Add("INTERPERSONAL MODES:");
            foreach (var (person, mode) in b.InterpersonalModes)
                lines.Add($"  [{person}]: {mode}");
        }
        if (b.StressResponses.Any())
        {
            lines.Add("STRESS RESPONSES:");
            foreach (var (level, response) in b.StressResponses)
                lines.Add($"  [{level}]: {response}");
        }
        if (b.Contradictions.Any())
            lines.Add($"INTERNAL CONTRADICTIONS:\n{string.Join("\n", b.Contradictions.Select(c2 => $"  - {c2}"))}");
        if (b.Habits.Any())
            lines.Add($"HABITS:\n{string.Join("\n", b.Habits.Select(h => $"  - {h}"))}");
        if (b.BreakingPoints.Any())
            lines.Add($"BREAKING POINTS:\n{string.Join("\n", b.BreakingPoints.Select(bp => $"  - {bp}"))}");

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

    public ToneBibleData ToneBible => toneBible.Get();

    public string GetToneBiblePrompt()
    {
        var tb = ToneBible;
        var lines = new List<string> { "NARRATIVE TONE — THESE DEFINE HOW THE STORY FEELS:" };
        foreach (var rule in tb.ToneRules.Take(8))
            lines.Add($"  - {rule}");
        if (tb.DialogueRules.Any())
        {
            lines.Add("DIALOGUE:");
            foreach (var rule in tb.DialogueRules.Take(4))
                lines.Add($"  - {rule}");
        }
        if (tb.StoryStructure.Any())
        {
            lines.Add("STORY STRUCTURE:");
            foreach (var rule in tb.StoryStructure.Take(4))
                lines.Add($"  - {rule}");
        }
        return string.Join("\n", lines);
    }

    public string GetSensoryPalettePrompt(string? location = null)
    {
        var tb = ToneBible;
        var lines = new List<string> { "SENSORY PALETTE — weave these into the prose:" };

        // If we have a specific location with atmosphere, use that
        if (location != null)
        {
            var district = Districts.FirstOrDefault(d =>
                d.Name.Equals(location, StringComparison.OrdinalIgnoreCase));
            if (district?.Atmosphere != null)
            {
                var a = district.Atmosphere;
                if (a.Sights.Any()) lines.Add($"  SIGHTS: {string.Join("; ", a.Sights.Take(4))}");
                if (a.Sounds.Any()) lines.Add($"  SOUNDS: {string.Join("; ", a.Sounds.Take(4))}");
                if (a.Smells.Any()) lines.Add($"  SMELLS: {string.Join("; ", a.Smells.Take(3))}");
                if (a.Feel.Length > 0) lines.Add($"  FEEL: {a.Feel}");
                return string.Join("\n", lines);
            }
        }

        // Fallback to global sensory palette
        var sp = tb.SensoryPalette;
        if (sp.Sights.Any()) lines.Add($"  SIGHTS: {string.Join("; ", sp.Sights.OrderBy(_ => Random.Shared.Next()).Take(4))}");
        if (sp.Sounds.Any()) lines.Add($"  SOUNDS: {string.Join("; ", sp.Sounds.OrderBy(_ => Random.Shared.Next()).Take(4))}");
        if (sp.Smells.Any()) lines.Add($"  SMELLS: {string.Join("; ", sp.Smells.OrderBy(_ => Random.Shared.Next()).Take(3))}");
        if (sp.Textures.Any()) lines.Add($"  TEXTURES: {string.Join("; ", sp.Textures.OrderBy(_ => Random.Shared.Next()).Take(3))}");
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

        // Inner monologue: italicized stand-alone lines, never labeled. Sourced from each
        // POV character's documented psychology — coping_mechanisms, core_fears, blind_spots,
        // secret. The six-archetype facet schema was retired 2026-04-26.
        lines.Add("INNER MONOLOGUE: italicized stand-alone lines on their own paragraph, NEVER labeled. Source from the POV character's psychology fields, not an archetype schema.");

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
        var q = query;

        // Search entities first (exact and partial name matches)
        foreach (var c in Characters.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = c.Id, EntityType = "character", EntityName = c.Name, Route = "/characters", FileName = "characters.json", Heading = c.Role, Context = Trunc(c.Description, 200) });
        foreach (var d in Districts.Where(d => d.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || d.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = d.Id, EntityType = "place", EntityName = d.Name, Route = "/places", FileName = "districts.json", Heading = "", Context = Trunc(d.Description, 200) });
        foreach (var f in Factions.Where(f => f.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || f.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = f.Id, EntityType = "faction", EntityName = f.Name, Route = "/factions", FileName = "factions.json", Heading = f.Motto, Context = Trunc(f.Description, 200) });
        foreach (var c in Corponations.Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Sector.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = c.Id, EntityType = "corponation", EntityName = c.Name, Route = "/corps", FileName = "corponations.json", Heading = c.Sector, Context = Trunc(c.FoundingStory, 200) });
        foreach (var w in Weaponry.Where(w => w.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || w.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = w.Id, EntityType = "weapon", EntityName = w.Name, Route = "/weaponry", FileName = "weaponry.json", Heading = w.Category, Context = Trunc(w.Description, 200) });
        foreach (var e in Equipment.Where(e => e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || e.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = e.Id, EntityType = "equipment", EntityName = e.Name, Route = "/equipment", FileName = "equipment.json", Heading = e.Category, Context = Trunc(e.Description, 200) });
        foreach (var t in Technology.Where(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || t.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
            results.Add(new SearchResult { EntityId = t.Id, EntityType = "technology", EntityName = t.Name, Route = "/technology", FileName = "technology.json", Heading = t.Subcategory, Context = Trunc(t.Description, 200) });

        if (results.Count >= maxResults) return results.Take(maxResults).ToList();

        // Search worldbuilding documents
        foreach (var doc in WorldbuildingDocs)
        {
            var lines = doc.Body.Split('\n');
            var currentHeading = "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith('#')) currentHeading = lines[i].TrimStart('#').Trim();
                if (!lines[i].Contains(q, StringComparison.OrdinalIgnoreCase)) continue;

                var start = Math.Max(0, i - 1);
                var end = Math.Min(lines.Length, i + 2);
                results.Add(new SearchResult
                {
                    FileName = doc.FileName,
                    Heading = currentHeading,
                    LineNumber = i + 1,
                    Context = string.Join("\n", lines[start..end]),
                    EntityType = "document",
                    EntityName = doc.Title,
                    Route = "/documents",
                });
                if (results.Count >= maxResults) return results;
            }
        }
        return results;
    }

    private static string Trunc(string s, int max) =>
        s.Length > max ? s[..(max - 3)] + "..." : s;
}

public record SearchResult
{
    public string EntityId { get; init; } = "";
    public string FileName { get; init; } = "";
    public string Heading { get; init; } = "";
    public int LineNumber { get; init; }
    public string Context { get; init; } = "";
    public string EntityType { get; init; } = "document";
    public string EntityName { get; init; } = "";
    public string Route { get; init; } = "";
}

public record Document
{
    public string FileName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public int LineCount { get; init; }
}
