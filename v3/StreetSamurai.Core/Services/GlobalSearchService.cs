using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Full-text and tag search across all canon repositories.
/// In-memory index built lazily; invalidated on any save.
/// Results route to the entity's dict page with ?id= so it opens directly in the edit panel.
/// </summary>
public class GlobalSearchService
{
    private readonly CharacterRepository characters;
    private readonly SyntheticLifeRepository synthetics;
    private readonly CorponationRepository corponations;
    private readonly DistrictRepository districts;
    private readonly FactionRepository factions;
    private readonly WeaponryRepository weaponry;
    private readonly AmmunitionRepository ammunition;
    private readonly EquipmentRepository equipment;
    private readonly TechnologyRepository technology;
    private readonly CyberwareRepository cyberware;
    private readonly ApparelRepository apparel;
    private readonly GenemodRepository genemods;
    private readonly PharmaceuticalRepository pharmaceuticals;
    private readonly MaterialRepository materials;
    private readonly TransportationRepository transportation;
    private readonly AutomatonRepository automata;
    private readonly ArchetypeRepository archetypes;
    private readonly SubsidiaryRepository subsidiaries;
    private readonly EntertainmentRepository entertainment;
    private readonly ConsumerGoodRepository consumerGoods;
    private readonly VocabularyRepository vocabulary;
    private readonly QuoteRepository quotes;
    private readonly NewsRepository news;
    private readonly ContractRepository contracts;
    private readonly WorldbuildingDocRepository documents;
    private readonly LabSpecimenRepository labSpecimens;
    private readonly CeramicManRepository ceramicMen;
    private readonly WastelandEntityRepository wastelandEntities;
    private readonly PsionicRepository psionics;

    private List<SearchIndexEntry> index = [];
    private readonly object syncLock = new();

    public GlobalSearchService(
        CharacterRepository characters, SyntheticLifeRepository synthetics,
        CorponationRepository corponations, DistrictRepository districts,
        FactionRepository factions, WeaponryRepository weaponry,
        AmmunitionRepository ammunition, EquipmentRepository equipment,
        TechnologyRepository technology, CyberwareRepository cyberware,
        ApparelRepository apparel, GenemodRepository genemods,
        PharmaceuticalRepository pharmaceuticals, MaterialRepository materials,
        TransportationRepository transportation, AutomatonRepository automata,
        ArchetypeRepository archetypes, SubsidiaryRepository subsidiaries,
        EntertainmentRepository entertainment, ConsumerGoodRepository consumerGoods,
        VocabularyRepository vocabulary, QuoteRepository quotes,
        NewsRepository news, ContractRepository contracts,
        WorldbuildingDocRepository documents, LabSpecimenRepository labSpecimens,
        CeramicManRepository ceramicMen, WastelandEntityRepository wastelandEntities,
        PsionicRepository psionics)
    {
        this.characters = characters; this.synthetics = synthetics;
        this.corponations = corponations; this.districts = districts;
        this.factions = factions; this.weaponry = weaponry;
        this.ammunition = ammunition; this.equipment = equipment;
        this.technology = technology; this.cyberware = cyberware;
        this.apparel = apparel; this.genemods = genemods;
        this.pharmaceuticals = pharmaceuticals; this.materials = materials;
        this.transportation = transportation; this.automata = automata;
        this.archetypes = archetypes; this.subsidiaries = subsidiaries;
        this.entertainment = entertainment; this.consumerGoods = consumerGoods;
        this.vocabulary = vocabulary; this.quotes = quotes;
        this.news = news; this.contracts = contracts;
        this.documents = documents; this.labSpecimens = labSpecimens;
        this.ceramicMen = ceramicMen; this.wastelandEntities = wastelandEntities;
        this.psionics = psionics;

        characters.OnItemSaved += _ => Invalidate();
        synthetics.OnItemSaved += _ => Invalidate();
        corponations.OnItemSaved += _ => Invalidate();
        districts.OnItemSaved += _ => Invalidate();
        factions.OnItemSaved += _ => Invalidate();
        weaponry.OnItemSaved += _ => Invalidate();
        ammunition.OnItemSaved += _ => Invalidate();
        equipment.OnItemSaved += _ => Invalidate();
        technology.OnItemSaved += _ => Invalidate();
        cyberware.OnItemSaved += _ => Invalidate();
        apparel.OnItemSaved += _ => Invalidate();
        genemods.OnItemSaved += _ => Invalidate();
        pharmaceuticals.OnItemSaved += _ => Invalidate();
        materials.OnItemSaved += _ => Invalidate();
        transportation.OnItemSaved += _ => Invalidate();
        automata.OnItemSaved += _ => Invalidate();
        archetypes.OnItemSaved += _ => Invalidate();
        subsidiaries.OnItemSaved += _ => Invalidate();
        entertainment.OnItemSaved += _ => Invalidate();
        consumerGoods.OnItemSaved += _ => Invalidate();
        vocabulary.OnItemSaved += _ => Invalidate();
        quotes.OnItemSaved += _ => Invalidate();
        news.OnItemSaved += _ => Invalidate();
        contracts.OnItemSaved += _ => Invalidate();
        documents.OnItemSaved += _ => Invalidate();
        labSpecimens.OnItemSaved += _ => Invalidate();
        ceramicMen.OnItemSaved += _ => Invalidate();
        wastelandEntities.OnItemSaved += _ => Invalidate();
        psionics.OnItemSaved += _ => Invalidate();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public List<CanonSearchResult> Search(string query, int page = 1, int pageSize = 20)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.Trim();
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return index
            .Select(e => (entry: e, score: Score(e, q, words)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToResult(x.entry, q, x.score))
            .ToList();
    }

    public int SearchCount(string query)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(query)) return 0;
        var q = query.Trim();
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return index.Count(e => Score(e, q, words) > 0);
    }

    public List<CanonSearchResult> SearchByTag(string tag, int page = 1, int pageSize = 20)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(tag)) return [];
        return index
            .Where(e => e.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(e => e.Type).ThenBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => ToResult(e, tag, 0))
            .ToList();
    }

    public int SearchByTagCount(string tag)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(tag)) return 0;
        return index.Count(e => e.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>All tags across every repo, sorted by frequency.</summary>
    public List<(string tag, int count)> AllTags()
    {
        EnsureBuilt();
        return index
            .SelectMany(e => e.Tags)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ThenBy(x => x.Item1)
            .ToList();
    }

    /// <summary>
    /// Tiered search: tier 1 = name/tag/subtitle matches (fast, high-confidence);
    /// tier 2 = body-only matches (slower, lower confidence).
    /// Caller can render tier 1 immediately, then append tier 2.
    /// </summary>
    public (List<CanonSearchResult> tier1, List<CanonSearchResult> tier2) SearchTiered(string query, int pageSize = 50)
    {
        EnsureBuilt();
        if (string.IsNullOrWhiteSpace(query)) return ([], []);
        var q = query.Trim();
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var t1 = new List<(SearchIndexEntry e, int s)>(128);
        var t2 = new List<(SearchIndexEntry e, int s)>(128);

        foreach (var e in index)
        {
            var s1 = ScoreNameTagSubtitle(e, q, words);
            if (s1 > 0) { t1.Add((e, s1)); continue; }
            var s2 = ScoreBody(e, q, words);
            if (s2 > 0) t2.Add((e, s2));
        }

        return (
            t1.OrderByDescending(x => x.s).Take(pageSize).Select(x => ToResult(x.e, q, x.s)).ToList(),
            t2.OrderByDescending(x => x.s).Take(pageSize).Select(x => ToResult(x.e, q, x.s)).ToList()
        );
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Invalidate() { lock (syncLock) { index = []; } }

    private void EnsureBuilt()
    {
        lock (syncLock)
        {
            if (index.Count > 0) return;
            RebuildIndex();
        }
    }

    private void RebuildIndex()
    {
        var entries = new List<SearchIndexEntry>(4096);

        foreach (var c in characters.GetAll())
            entries.Add(new(c.Id, "character", c.Name, c.Role, c.Description, c.Tags, "/characters"));
        foreach (var s in synthetics.GetAll())
            entries.Add(new(s.Id, "synthetic", s.Name, s.Classification, $"{s.Description} {s.ObservedBehavior}", s.Tags, "/synthetics"));
        foreach (var c in corponations.GetAll())
            entries.Add(new(c.Id, "corponation", c.Name, c.Sector, $"{c.FoundingStory} {c.KeyDetail}", c.Tags, "/corps"));
        foreach (var d in districts.GetAll())
            entries.Add(new(d.Id, "place", d.Name, "", d.Description, d.Tags, "/places"));
        foreach (var f in factions.GetAll())
            entries.Add(new(f.Id, "faction", f.Name, f.Motto, $"{f.Description} {f.Ideology}", f.Tags, "/factions"));
        foreach (var w in weaponry.GetAll())
            entries.Add(new(w.Id, "weapon", w.Name, w.Category, $"{w.Description} {w.CulturalContext}", w.Tags, "/weaponry"));
        foreach (var a in ammunition.GetAll())
            entries.Add(new(a.Id, "ammunition", a.Name, a.Category, $"{a.Description} {a.CulturalContext}", a.Tags, "/ammunition"));
        foreach (var e in equipment.GetAll())
            entries.Add(new(e.Id, "equipment", e.Name, e.Category, $"{e.Description} {e.CulturalContext}", e.Tags, "/equipment"));
        foreach (var t in technology.GetAll())
            entries.Add(new(t.Id, "technology", t.Name, t.Subcategory, t.Description, t.Tags, "/technology"));
        foreach (var c in cyberware.GetAll())
            entries.Add(new(c.Id, "cyberware", c.Name, $"{c.Category} — {c.BodyLocation}", $"{c.Description} {c.CulturalContext}", c.Tags, "/cyberware"));
        foreach (var a in apparel.GetAll())
            entries.Add(new(a.Id, "apparel", a.Name, a.Category, $"{a.Description} {a.Functionality}", a.Tags, "/apparel"));
        foreach (var g in genemods.GetAll())
            entries.Add(new(g.Id, "genemod", g.Name, g.Category, g.Description, g.Tags, "/genemods"));
        foreach (var p in pharmaceuticals.GetAll())
            entries.Add(new(p.Id, "pharmaceutical", p.Name, p.Category, $"{p.Description} {p.CulturalContext}", p.Tags, "/pharmaceuticals"));
        foreach (var m in materials.GetAll())
            entries.Add(new(m.Id, "material", m.Name, m.Category, m.Description, m.Tags, "/materials"));
        foreach (var t in transportation.GetAll())
            entries.Add(new(t.Id, "transportation", t.Name, t.Category, $"{t.Description} {t.CommonUsage}", t.Tags, "/transportation"));
        foreach (var a in automata.GetAll())
            entries.Add(new(a.Id, "automaton", a.Name, a.Classification, $"{a.Description} {a.CulturalContext}", a.Tags, "/automata"));
        foreach (var a in archetypes.GetAll())
            entries.Add(new(a.Id, "archetype", a.Name, a.Category, a.Description, a.Tags, "/archetypes"));
        foreach (var s in subsidiaries.GetAll())
            entries.Add(new(s.Id, "subsidiary", s.Name, $"{s.ParentCorponation} — {s.LineOfBusiness}", s.Description, s.Tags, "/subsidiaries"));
        foreach (var e in entertainment.GetAll())
            entries.Add(new(e.Id, "entertainment", e.Name, e.Category, e.Description, e.Tags, "/entertainment"));
        foreach (var g in consumerGoods.GetAll())
            entries.Add(new(g.Id, "consumer-good", g.Name, g.Category, $"{g.Description} {g.CulturalContext}", g.Tags, "/goods"));
        foreach (var v in vocabulary.GetAll())
            entries.Add(new(v.Id, "vocabulary", v.Term, v.Category, $"{v.Definition} {v.Usage}", v.Tags, "/vocabulary"));
        foreach (var q in quotes.GetAll())
            entries.Add(new(q.Id, "quote", q.Attribution, q.Category, $"{q.Quote} {q.Context}", q.Tags, "/quotes"));
        foreach (var n in news.GetAll())
            entries.Add(new(n.Id, "news", n.Headline, n.Category, $"{n.Body} {n.Aftermath}", n.Tags, "/news"));
        foreach (var c in contracts.GetAll())
            entries.Add(new(c.Id, "contract", c.Codename, c.Category, $"{c.Description} {c.Objective}", c.Tags, "/contracts"));
        foreach (var d in documents.GetAll())
            entries.Add(new(d.Id, "document", d.Title, d.Category, d.Body, d.Tags, "/documents"));
        foreach (var s in labSpecimens.GetAll())
            entries.Add(new(s.Id, "lab-specimen", s.Name, s.Classification, $"{s.PhysicalDescription} {s.BehavioralProfile} {s.PitiableQualities}", s.Tags, "/specimens"));
        foreach (var c in ceramicMen.GetAll())
            entries.Add(new(c.Id, "ceramic-man", c.Name, c.CurrentRole, $"{c.OperatingHistory} {c.BehavioralNotes} {c.DiplomaticSpecialty}", c.Tags, "/ceramic-men"));
        foreach (var w in wastelandEntities.GetAll())
            entries.Add(new(w.Id, "wasteland-entity", w.Name, w.Classification, $"{w.PhysicalDescription} {w.BehavioralProfile} {w.HumanRemnants}", w.Tags, "/wasteland"));
        foreach (var p in psionics.GetAll())
            entries.Add(new(p.Id, "psionic", p.Name, p.Classification, $"{p.Mechanism} {p.Abilities} {p.SideEffects}", p.Tags, "/psionics"));

        index = entries;
    }

    private static CanonSearchResult ToResult(SearchIndexEntry e, string query, int score) =>
        new(e.Id, e.Type, e.Name, e.Subtitle, Snippet(e.Body, query), e.Tags, $"{e.RepoRoute}?id={e.Id}", score);

    private static int Score(SearchIndexEntry e, string q, string[] words) =>
        ScoreNameTagSubtitle(e, q, words) + ScoreBody(e, q, words);

    private static int ScoreNameTagSubtitle(SearchIndexEntry e, string q, string[] words)
    {
        int score = 0;
        if (e.Name.Equals(q, StringComparison.OrdinalIgnoreCase)) score += 100;
        else if (e.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase)) score += 80;
        else if (e.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) score += 60;
        else if (words.Length > 1 && words.All(w => e.Name.Contains(w, StringComparison.OrdinalIgnoreCase))) score += 55;
        foreach (var w in words)
            if (e.Tags.Any(t => t.Contains(w, StringComparison.OrdinalIgnoreCase))) score += 30;
        if (!string.IsNullOrWhiteSpace(e.Subtitle) && e.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase)) score += 15;
        return score;
    }

    private static int ScoreBody(SearchIndexEntry e, string q, string[] words)
    {
        if (string.IsNullOrWhiteSpace(e.Body)) return 0;
        if (e.Body.Contains(q, StringComparison.OrdinalIgnoreCase)) return 20;
        int score = 0;
        foreach (var w in words)
            if (e.Body.Contains(w, StringComparison.OrdinalIgnoreCase)) score += 5;
        return score;
    }

    private static string Snippet(string body, string query)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        var idx = body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return body.Length > 160 ? body[..157] + "…" : body;
        var start = Math.Max(0, idx - 60);
        var end = Math.Min(body.Length, idx + query.Length + 100);
        var result = body[start..end].Trim();
        if (start > 0) result = "…" + result;
        if (end < body.Length) result += "…";
        return result;
    }

    private record SearchIndexEntry(
        string Id, string Type, string Name, string Subtitle,
        string Body, List<string> Tags, string RepoRoute);
}

public record CanonSearchResult(
    string Id,
    string Type,
    string Name,
    string Subtitle,
    string Snippet,
    List<string> Tags,
    string Route,
    int Score
);
