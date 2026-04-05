namespace StreetSamurai.Core.Services;

/// <summary>
/// Tag-based thematic index across all repositories. Uses the `tags` field on each entity
/// for instant lookup instead of text scanning. Provides context snippets (not just names)
/// and specialized injection for vocabulary, quotes, and motifs.
///
/// The single integration point between ALL repos and the story generation pipeline.
/// </summary>
public class ThematicIndexService
{
    private readonly DatabaseService db;
    private readonly SyntheticLifeRepository synthRepo;
    private readonly GenewareRepository genewareRepo;
    private readonly TransportationRepository transportRepo;
    private readonly VocabularyRepository vocabRepo;
    private readonly QuoteRepository quoteRepo;
    private readonly ConsumerGoodRepository goodsRepo;
    private readonly PharmaceuticalRepository pharmaRepo;
    private readonly SubstrateRepository substrateRepo;
    private readonly AmmunitionRepository ammoRepo;

    // tag -> list of hits with snippets
    private Dictionary<string, List<ThematicHit>> index = new(StringComparer.OrdinalIgnoreCase);
    private bool built;

    public ThematicIndexService(
        DatabaseService db, SyntheticLifeRepository synthRepo,
        GenewareRepository genewareRepo, TransportationRepository transportRepo,
        VocabularyRepository vocabRepo, QuoteRepository quoteRepo,
        ConsumerGoodRepository goodsRepo, PharmaceuticalRepository pharmaRepo,
        SubstrateRepository substrateRepo, AmmunitionRepository ammoRepo)
    {
        this.db = db;
        this.synthRepo = synthRepo;
        this.genewareRepo = genewareRepo;
        this.transportRepo = transportRepo;
        this.vocabRepo = vocabRepo;
        this.quoteRepo = quoteRepo;
        this.goodsRepo = goodsRepo;
        this.pharmaRepo = pharmaRepo;
        this.substrateRepo = substrateRepo;
        this.ammoRepo = ammoRepo;
    }

    /// <summary>Build the index from tags on all entities. Fast — reads tags arrays, no text scanning.</summary>
    public void RebuildIndex()
    {
        var newIndex = new Dictionary<string, List<ThematicHit>>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in db.Characters)
            IndexByTags(newIndex, c.Name, "character", FirstSentence(c.Description), c.Stats.Tags);
        foreach (var d in db.Districts)
            IndexByTags(newIndex, d.Name, "place", FirstSentence(d.Description), d.Tags);
        foreach (var f in db.Factions)
            IndexByTags(newIndex, f.Name, "faction", FirstSentence(f.Description), f.Tags);
        foreach (var c in db.Corponations)
            IndexByTags(newIndex, c.Name, "corponation", FirstSentence(c.KeyDetail), c.Tags);
        foreach (var w in db.Weaponry)
            IndexByTags(newIndex, w.Name, "weapon", FirstSentence(w.Description), w.Tags);
        foreach (var t in db.Technology)
            IndexByTags(newIndex, t.Name, "technology", FirstSentence(t.Description), t.Tags);
        foreach (var e in db.Equipment)
            IndexByTags(newIndex, e.Name, "equipment", FirstSentence(e.Description), e.Tags);
        foreach (var s in synthRepo.GetAll())
            IndexByTags(newIndex, s.Name, "synthetic", FirstSentence(s.Description), s.Tags);
        foreach (var g in genewareRepo.GetAll())
            IndexByTags(newIndex, g.Name, "geneware", FirstSentence(g.Description), g.Tags);
        foreach (var t in transportRepo.GetAll())
            IndexByTags(newIndex, t.Name, "transportation", FirstSentence(t.Description), t.Tags);
        foreach (var v in vocabRepo.GetAll())
            IndexByTags(newIndex, v.Term, "vocabulary", $"{v.Term} — {FirstSentence(v.Definition)}", v.Tags);
        foreach (var q in quoteRepo.GetAll())
            IndexByTags(newIndex, q.Attribution.Length > 0 ? q.Attribution : "Anonymous", "quote", q.Quote, q.Tags);
        foreach (var g in goodsRepo.GetAll())
            IndexByTags(newIndex, g.Name, "consumer_good", FirstSentence(g.Description), g.Tags);
        foreach (var p in pharmaRepo.GetAll())
            IndexByTags(newIndex, p.Name, "pharmaceutical", FirstSentence(p.Description), p.Tags);
        foreach (var s in substrateRepo.GetAll())
            IndexByTags(newIndex, s.Name, "substrate", FirstSentence(s.Description), s.Tags);
        foreach (var a in ammoRepo.GetAll())
            IndexByTags(newIndex, a.Name, "ammunition", FirstSentence(a.Description), a.Tags);

        index = newIndex;
        built = true;
    }

    /// <summary>Get entities relevant to tags, ranked by match count. Returns snippets.</summary>
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
                {
                    existing.Score += 1;
                    if (!existing.Themes.Contains(theme)) existing.Themes.Add(theme);
                }
                else
                {
                    scores[key] = new ThematicHit
                    {
                        EntityName = hit.EntityName,
                        EntityType = hit.EntityType,
                        Snippet = hit.Snippet,
                        Score = 1,
                        Themes = [theme]
                    };
                }
            }
        }

        return scores.Values
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.Themes.Count)
            .Take(count)
            .ToList();
    }

    /// <summary>Get vocabulary terms matching tags. Returns term + definition pairs.</summary>
    public List<(string term, string definition)> GetRelevantVocabulary(IEnumerable<string> themes, int count = 8)
    {
        if (!built) RebuildIndex();
        var hits = GetRelevantEntities(themes, count * 3)
            .Where(h => h.EntityType == "vocabulary")
            .Take(count);
        return hits.Select(h => (h.EntityName, h.Snippet)).ToList();
    }

    /// <summary>Get quotes matching tags.</summary>
    public List<string> GetRelevantQuotes(IEnumerable<string> themes, int count = 2)
    {
        if (!built) RebuildIndex();
        return GetRelevantEntities(themes, count * 5)
            .Where(h => h.EntityType == "quote")
            .Take(count)
            .Select(h => h.Snippet)
            .ToList();
    }

    /// <summary>Get motifs matching tags.</summary>
    public List<(string name, string description)> GetRelevantMotifs(IEnumerable<string> themes, int count = 2)
    {
        var motifs = db.Motifs;
        if (motifs.Count == 0) return [];

        var themeSet = new HashSet<string>(themes, StringComparer.OrdinalIgnoreCase);
        // Match motifs by checking if any theme appears in motif name or description
        return motifs
            .Select(m => (m.Name, m.Description, score: themeSet.Count(t =>
                m.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                m.Description.Contains(t, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(count)
            .Select(x => (x.Name, x.Description))
            .ToList();
    }

    /// <summary>Extract tags from text for matching.</summary>
    public List<string> ExtractThemes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        var textLower = text.ToLowerInvariant();
        // Return tags that appear as words in the text
        return index.Keys.Where(tag => textLower.Contains(tag)).Take(20).ToList();
    }

    /// <summary>Build a complete context injection block for a beat.</summary>
    public string BuildBeatContext(string beatGoal, string beatTitle, string? location)
    {
        var themes = ExtractThemes($"{beatGoal} {beatTitle} {location ?? ""}");
        if (themes.Count == 0) return "";

        var parts = new List<string>();

        // Context snippets from all repos
        var entities = GetRelevantEntities(themes, 8);
        if (entities.Count > 0)
        {
            parts.Add("WORLD DETAILS RELEVANT TO THIS BEAT (weave naturally, don't list):");
            foreach (var h in entities)
                parts.Add($"  [{h.EntityType}] {h.EntityName} — {h.Snippet}");
        }

        // Vocabulary injection
        var vocab = GetRelevantVocabulary(themes, 5);
        if (vocab.Count > 0)
        {
            parts.Add("USE THESE TERMS NATURALLY IN DIALOGUE AND NARRATION:");
            foreach (var (term, def) in vocab)
                parts.Add($"  {term} — {def}");
        }

        // Quote injection
        var quotes = GetRelevantQuotes(themes, 1);
        if (quotes.Count > 0)
        {
            parts.Add("A CHARACTER MIGHT SAY OR THINK SOMETHING LIKE:");
            foreach (var q in quotes)
                parts.Add($"  \"{q}\"");
        }

        // Motif injection
        var motifs = GetRelevantMotifs(themes, 1);
        if (motifs.Count > 0)
        {
            parts.Add("MOTIF OPPORTUNITY (if it fits naturally):");
            foreach (var (name, desc) in motifs)
                parts.Add($"  {name} — {desc}");
        }

        return string.Join("\n", parts);
    }

    private static void IndexByTags(Dictionary<string, List<ThematicHit>> idx, string name, string type, string snippet, List<string>? tags)
    {
        if (tags == null || tags.Count == 0) return;
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag)) continue;
            var normalizedTag = tag.ToLowerInvariant().Trim();
            if (!idx.TryGetValue(normalizedTag, out var list))
            {
                list = [];
                idx[normalizedTag] = list;
            }
            list.Add(new ThematicHit { EntityName = name, EntityType = type, Snippet = snippet, Score = 1 });
        }
    }

    private static string FirstSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var clean = text.Replace("\\n", " ").Replace("\n", " ").Trim();
        var end = clean.IndexOfAny(['.', '!', '?']);
        if (end > 0 && end < 200) return clean[..(end + 1)];
        return clean.Length > 150 ? clean[..150] + "..." : clean;
    }
}

public class ThematicHit
{
    public string EntityName { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string Snippet { get; set; } = "";
    public double Score { get; set; }
    public List<string> Themes { get; set; } = [];
}
