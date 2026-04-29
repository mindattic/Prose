using System.Text.Json;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Runtime canon access. Delegates to DatabaseService (canon.json).
/// </summary>
public class LoreService
{
    private readonly DatabaseService db;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public LoreService(DatabaseService db)
    {
        this.db = db;
    }

    // ── Documents ────────────────────────────────────────────

    public List<Document> ListDocuments()
    {
        return db.WorldbuildingDocs
            .Select(d => new Document
            {
                FileName = d.FileName,
                Title = d.Title,
                Category = d.Category,
                LineCount = d.LineCount,
            })
            .ToList();
    }

    public string? ReadDocument(string nameOrPartial)
    {
        var doc = db.WorldbuildingDocs
            .FirstOrDefault(d => d.FileName.Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        return doc?.Body;
    }

    // ── Text Search ─────────────────────────────────────────

    public List<SearchResult> Search(string query, int maxResults = 20)
        => db.Search(query, maxResults);

    // ── Corponations ────────────────────────────────────────

    public List<Corponation> ListCorponations(string? filter = null)
    {
        var corps = db.Corponations.Select(MapCorponation).ToList();
        if (string.IsNullOrWhiteSpace(filter)) return corps;
        var ft = filter.ToLowerInvariant();
        return corps
            .Where(c => c.Name.Contains(ft, StringComparison.OrdinalIgnoreCase)
                     || c.Sector.Contains(ft, StringComparison.OrdinalIgnoreCase)
                     || c.Number.ToString() == ft)
            .ToList();
    }

    public Corponation? GetCorponation(string identifier)
    {
        CorponationData? data;
        if (int.TryParse(identifier, out var num))
            data = db.Corponations.FirstOrDefault(c => c.Number == num);
        else
            data = db.Corponations.FirstOrDefault(c =>
                c.Name.Contains(identifier, StringComparison.OrdinalIgnoreCase));
        return data != null ? MapCorponation(data) : null;
    }

    public void InvalidateCache() { /* no-op — data comes from canon.json */ }

    // ── Factions ─────────────────────────────────────────────

    public List<Faction> ListFactions()
    {
        return db.Factions
            .Select(f => new Faction
            {
                Name = f.Name,
                Type = f.Type,
                Aliases = f.Aliases,
                Description = f.Description,
                Ideology = f.Ideology,
                Territory = f.Territory,
                Leadership = f.Leadership,
                Methods = f.Methods,
                Resources = f.Resources,
                Goals = f.Goals,
            })
            .OrderBy(f => f.Name)
            .ToList();
    }

    public string? ReadFactionJson(string nameOrPartial)
    {
        var faction = db.Factions
            .FirstOrDefault(f => f.Name.Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        return faction != null ? JsonSerializer.Serialize(faction, JsonOpts) : null;
    }

    // ── Districts ───────────────────────────────────────────

    public List<District> ListDistricts()
    {
        return db.Districts
            .Select(d => new District
            {
                Name = d.Name,
                Type = d.Type,
                Aliases = d.Aliases,
                Description = d.Description,
                Atmosphere = new DistrictAtmosphere
                {
                    Sights = d.Atmosphere.Sights,
                    Sounds = d.Atmosphere.Sounds,
                    Smells = d.Atmosphere.Smells,
                    Feel = d.Atmosphere.Feel,
                },
                Dangers = d.Dangers,
                Opportunities = d.Opportunities,
                StoryHooks = d.StoryHooks,
            })
            .OrderBy(d => d.Name)
            .ToList();
    }

    public string? ReadDistrictJson(string nameOrPartial)
    {
        var district = db.Districts
            .FirstOrDefault(d => d.Name.Contains(nameOrPartial, StringComparison.OrdinalIgnoreCase));
        return district != null ? JsonSerializer.Serialize(district, JsonOpts) : null;
    }

    // ── Technology ──────────────────────────────────────────

    public string? ReadTechnology()
    {
        var doc = db.WorldbuildingDocs
            .FirstOrDefault(d => d.FileName.Equals("technology", StringComparison.OrdinalIgnoreCase));
        return doc?.Body;
    }

    // ── World Rules ─────────────────────────────────────────

    public List<(string Name, string Content)> ListWorldRuleFiles()
    {
        var results = new List<(string, string)>();

        results.Add(("story_bible", JsonSerializer.Serialize(db.StoryBible, JsonOpts)));
        results.Add(("literary_rules", JsonSerializer.Serialize(db.LiteraryRules, JsonOpts)));
        results.Add(("motifs", JsonSerializer.Serialize(db.Motifs, JsonOpts)));

        return results;
    }

    // ── Character JSON (structured) ─────────────────────────

    public string? ReadCharacterJson(string nameOrPartial)
    {
        var character = db.FindCharacter(nameOrPartial);
        return character != null ? JsonSerializer.Serialize(character, JsonOpts) : null;
    }

    // ── Characters (fully parsed) ───────────────────────────

    public List<Character> ListCharacters()
    {
        return db.Characters
            .Select(c => new Character
            {
                Name = c.Name,
                Aliases = c.Aliases,
                Status = c.Status,
                Age = c.Age,
                Occupation = c.Role,
                Affiliation = c.Affiliation,
                Augmentation = c.Augmentations,
                Relationships = c.Relationships.Select(r => new Relationship
                {
                    Name = r.Name,
                    Status = r.Type,
                    Notes = r.Description,
                }).ToList(),
                VoiceNotes = $"{c.SpeechPatterns.Vocabulary} {c.SpeechPatterns.Cadence}".Trim(),
                History = c.StoryHooks.Select((h, i) => new HistoryBeat { Event = h, Age = i }).ToList(),
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    /// <summary>
    /// Returns the full character psychology context for LLM prompts.
    /// </summary>
    public string GetCharacterPsychologyContext(string nameOrAlias)
        => db.GetCharacterContext(nameOrAlias);

    // ── Helpers ──────────────────────────────────────────────

    private static Corponation MapCorponation(CorponationData c) => new()
    {
        Number = c.Number,
        Name = c.Name,
        Sector = c.Sector,
        Valuation = c.Valuation,
        Origin = c.FoundingStory,
        Territory = c.SovereignTerritory,
        SecurityForce = c.SecurityForce,
        KeyDetail = c.KeyDetail,
        RelationshipToBig20 = c.RelationshipToBig20,
        FullText = c.FullText,
    };
}
