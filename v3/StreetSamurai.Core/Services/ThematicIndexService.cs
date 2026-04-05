using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Inverted thematic tag index across all repositories. Scans entity descriptions,
/// story hooks, categories, and other text fields at startup to extract thematic tags.
/// Provides fast retrieval of entities related to specific themes during story generation.
///
/// No LLM needed — uses keyword extraction from a curated thematic vocabulary.
/// No data duplication — indexes into existing repo data by name/type reference.
/// Rebuilds in milliseconds on demand.
/// </summary>
public class ThematicIndexService
{
    private readonly DatabaseService db;
    private readonly SyntheticLifeRepository synthRepo;
    private readonly GenewareRepository genewareRepo;
    private readonly TransportationRepository transportRepo;

    // theme -> list of (entityName, entityType, relevanceScore)
    private Dictionary<string, List<ThematicHit>> index = new(StringComparer.OrdinalIgnoreCase);
    private bool built;

    public ThematicIndexService(
        DatabaseService db, SyntheticLifeRepository synthRepo,
        GenewareRepository genewareRepo, TransportationRepository transportRepo)
    {
        this.db = db;
        this.synthRepo = synthRepo;
        this.genewareRepo = genewareRepo;
        this.transportRepo = transportRepo;
    }

    /// <summary>Build the index from all repos. Fast — no LLM calls.</summary>
    public void RebuildIndex()
    {
        var newIndex = new Dictionary<string, List<ThematicHit>>(StringComparer.OrdinalIgnoreCase);

        // Characters
        foreach (var c in db.Characters)
        {
            var text = $"{c.Description} {c.NarrativeFunction} {c.Role} {c.Affiliation} {string.Join(" ", c.StoryHooks)} {c.Psychology.Secret} {string.Join(" ", c.Psychology.CoreFears)} {string.Join(" ", c.Psychology.CoreDesires)}";
            IndexEntity(newIndex, c.Name, "character", text);
        }

        // Places
        foreach (var d in db.Districts)
        {
            var text = $"{d.Description} {d.Economy} {d.PowerStructure} {string.Join(" ", d.Dangers)} {string.Join(" ", d.Opportunities)} {string.Join(" ", d.StoryHooks)} {string.Join(" ", d.FrequentedBy)}";
            IndexEntity(newIndex, d.Name, "place", text);
        }

        // Factions
        foreach (var f in db.Factions)
        {
            var text = $"{f.Description} {f.Ideology} {f.Territory} {string.Join(" ", f.Methods)} {string.Join(" ", f.StoryHooks)}";
            IndexEntity(newIndex, f.Name, "faction", text);
        }

        // Corponations
        foreach (var c in db.Corponations)
        {
            var text = $"{c.Name} {c.Sector} {c.KeyDetail} {c.FoundingStory} {c.SecurityForce}";
            IndexEntity(newIndex, c.Name, "corponation", text);
        }

        // Weapons
        foreach (var w in db.Weaponry)
        {
            var text = $"{w.Name} {w.Description} {w.Category} {string.Join(" ", w.StoryHooks)}";
            IndexEntity(newIndex, w.Name, "weapon", text);
        }

        // Technology
        foreach (var t in db.Technology)
        {
            var text = $"{t.Name} {t.Description} {t.Subcategory} {t.SocialImpact} {string.Join(" ", t.StoryHooks)}";
            IndexEntity(newIndex, t.Name, "technology", text);
        }

        // Equipment
        foreach (var e in db.Equipment)
        {
            var text = $"{e.Name} {e.Description} {e.Category} {string.Join(" ", e.StoryHooks)}";
            IndexEntity(newIndex, e.Name, "equipment", text);
        }

        // Synthetics
        foreach (var s in synthRepo.GetAll())
        {
            var text = $"{s.Name} {s.Description} {s.ObservedBehavior} {s.Classification} {s.Disposition} {s.Habitat} {string.Join(" ", s.StoryHooks)}";
            IndexEntity(newIndex, s.Name, "synthetic", text);
        }

        // Geneware
        foreach (var g in genewareRepo.GetAll())
        {
            var text = $"{g.Name} {g.Description} {g.Category} {g.SourceOrganism} {g.SocialPerception} {string.Join(" ", g.StoryHooks)}";
            IndexEntity(newIndex, g.Name, "geneware", text);
        }

        // Transportation
        foreach (var t in transportRepo.GetAll())
        {
            var text = $"{t.Name} {t.Description} {t.Category} {t.CommonUsage} {string.Join(" ", t.StoryHooks)}";
            IndexEntity(newIndex, t.Name, "transportation", text);
        }

        index = newIndex;
        built = true;
    }

    /// <summary>
    /// Get entities relevant to a set of themes, ranked by relevance.
    /// Returns up to `count` results with the highest combined theme match scores.
    /// </summary>
    public List<ThematicHit> GetRelevantEntities(IEnumerable<string> themes, int count = 15)
    {
        if (!built) RebuildIndex();

        var scores = new Dictionary<string, ThematicHit>();
        foreach (var theme in themes)
        {
            if (!index.TryGetValue(theme, out var hits)) continue;
            foreach (var hit in hits)
            {
                var key = $"{hit.EntityType}:{hit.EntityName}";
                if (scores.TryGetValue(key, out var existing))
                    existing.Score += hit.Score;
                else
                    scores[key] = new ThematicHit { EntityName = hit.EntityName, EntityType = hit.EntityType, Score = hit.Score, Themes = [theme] };

                if (!scores[key].Themes.Contains(theme))
                    scores[key].Themes.Add(theme);
            }
        }

        return scores.Values
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.Themes.Count)
            .Take(count)
            .ToList();
    }

    /// <summary>Get all themes in the index with their entity counts.</summary>
    public Dictionary<string, int> GetThemeCounts()
    {
        if (!built) RebuildIndex();
        return index.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
    }

    /// <summary>Extract themes from a text (for matching a beat's goal/premise to the index).</summary>
    public List<string> ExtractThemes(string text)
    {
        var textLower = text.ToLowerInvariant();
        return ThematicVocabulary.Where(t => textLower.Contains(t)).ToList();
    }

    private static void IndexEntity(Dictionary<string, List<ThematicHit>> idx, string name, string type, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var textLower = text.ToLowerInvariant();

        foreach (var theme in ThematicVocabulary)
        {
            var count = CountOccurrences(textLower, theme);
            if (count == 0) continue;

            if (!idx.TryGetValue(theme, out var list))
            {
                list = [];
                idx[theme] = list;
            }
            list.Add(new ThematicHit { EntityName = name, EntityType = type, Score = count });
        }
    }

    private static int CountOccurrences(string text, string word)
    {
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(word, idx, StringComparison.Ordinal)) != -1)
        {
            // Check word boundary
            bool leftOk = idx == 0 || !char.IsLetterOrDigit(text[idx - 1]);
            bool rightOk = idx + word.Length >= text.Length || !char.IsLetterOrDigit(text[idx + word.Length]);
            if (leftOk && rightOk) count++;
            idx += word.Length;
        }
        return count;
    }

    // Curated thematic vocabulary — the themes the index tracks
    private static readonly string[] ThematicVocabulary =
    [
        // Violence & conflict
        "violence", "combat", "fight", "battle", "kill", "murder", "weapon", "gun", "blade", "knife",
        "sniper", "ambush", "war", "assault", "explosion", "bomb", "siege", "raid",
        // Crime & underworld
        "crime", "theft", "smuggle", "smuggling", "heist", "robbery", "gang", "cartel", "syndicate",
        "black market", "contraband", "forgery", "extortion", "bounty",
        // Betrayal & trust
        "betrayal", "betray", "trust", "loyalty", "traitor", "deception", "lie", "secret", "hidden",
        // Loss & grief
        "loss", "grief", "death", "mourning", "funeral", "orphan", "widow", "missing",
        // Love & intimacy
        "love", "romance", "intimate", "tender", "kiss", "partner", "marriage", "family",
        // Identity & self
        "identity", "memory", "amnesia", "consciousness", "self", "mirror", "mask", "disguise",
        // Technology & augmentation
        "augment", "cyberware", "prosthetic", "implant", "neural", "bci", "interface",
        "chrome", "geneware", "genetic", "mutation", "biocompute",
        // AI & synthetic life
        "ai", "artificial", "synthetic", "android", "robot", "elf", "rogue", "leviathan",
        "supermind", "sentient", "conscious", "hive mind",
        // Corporate & power
        "corporate", "corponation", "executive", "contract", "merger", "hostile", "takeover",
        "surveillance", "control", "power", "authority", "sovereignty",
        // Economic
        "quanta", "money", "debt", "poverty", "wealth", "ubc", "compute", "wallet",
        "scrip", "broker", "trade",
        // Medical & body
        "medical", "surgery", "hospital", "clinic", "doctor", "disease", "injury", "wound",
        "pain", "healing", "organ", "blood", "transplant",
        // Location types
        "underworld", "tunnel", "subway", "harbor", "dock", "rooftop", "alley", "market",
        "club", "bar", "clinic", "warehouse", "factory",
        // Atmosphere & mood
        "rain", "neon", "dark", "shadow", "fog", "night", "storm", "cold", "heat",
        "silence", "noise", "crowd", "alone", "abandoned",
        // Morality & philosophy
        "moral", "ethics", "justice", "revenge", "redemption", "sacrifice", "guilt",
        "innocent", "corrupt", "freedom", "slavery", "rights",
        // Transportation
        "vehicle", "motorcycle", "hover", "airship", "zeppelin", "train", "subway",
        "mass driver", "hyperlane", "transit",
        // Survival
        "survival", "hunger", "shelter", "refugee", "escape", "chase", "hide",
        "desperate", "starving", "homeless",
    ];
}

public class ThematicHit
{
    public string EntityName { get; set; } = "";
    public string EntityType { get; set; } = "";
    public double Score { get; set; }
    public List<string> Themes { get; set; } = [];
}
