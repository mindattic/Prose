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
    private readonly FlyoverEntityRepository flyoverEntities;
    private readonly PsionicRepository psionics;

    private List<SearchIndexEntry> index = [];
    private readonly object syncLock = new();

    public GlobalSearchService(
        CharacterRepository characters,
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
        FlyoverEntityRepository flyoverEntities,
        PsionicRepository psionics)
    {
        this.characters = characters;
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
        this.flyoverEntities = flyoverEntities;
        this.psionics = psionics;

        // Note: OnItemSaved fires with the entity's name (not id), so we look up
        // by name to fetch the freshly-saved row and re-project a single entry.
        characters.OnItemSaved      += n => UpdateOrAdd(characters.GetByName(n) is { } c      ? ProjectCharacter(c)      : null);
        corponations.OnItemSaved    += n => UpdateOrAdd(corponations.GetByName(n) is { } c    ? ProjectCorponation(c)    : null);
        districts.OnItemSaved       += n => UpdateOrAdd(districts.GetByName(n) is { } d       ? ProjectDistrict(d)       : null);
        factions.OnItemSaved        += n => UpdateOrAdd(factions.GetByName(n) is { } f        ? ProjectFaction(f)        : null);
        weaponry.OnItemSaved        += n => UpdateOrAdd(weaponry.GetByName(n) is { } w        ? ProjectWeapon(w)         : null);
        ammunition.OnItemSaved      += n => UpdateOrAdd(ammunition.GetByName(n) is { } a      ? ProjectAmmunition(a)     : null);
        equipment.OnItemSaved       += n => UpdateOrAdd(equipment.GetByName(n) is { } e       ? ProjectEquipment(e)      : null);
        technology.OnItemSaved      += n => UpdateOrAdd(technology.GetByName(n) is { } t      ? ProjectTechnology(t)     : null);
        cyberware.OnItemSaved       += n => UpdateOrAdd(cyberware.GetByName(n) is { } c       ? ProjectCyberware(c)      : null);
        apparel.OnItemSaved         += n => UpdateOrAdd(apparel.GetByName(n) is { } a         ? ProjectApparel(a)        : null);
        genemods.OnItemSaved        += n => UpdateOrAdd(genemods.GetByName(n) is { } g        ? ProjectGenemod(g)        : null);
        pharmaceuticals.OnItemSaved += n => UpdateOrAdd(pharmaceuticals.GetByName(n) is { } p ? ProjectPharmaceutical(p) : null);
        materials.OnItemSaved       += n => UpdateOrAdd(materials.GetByName(n) is { } m       ? ProjectMaterial(m)       : null);
        transportation.OnItemSaved  += n => UpdateOrAdd(transportation.GetByName(n) is { } t  ? ProjectTransportation(t) : null);
        automata.OnItemSaved        += n => UpdateOrAdd(automata.GetByName(n) is { } a        ? ProjectAutomaton(a)      : null);
        archetypes.OnItemSaved      += n => UpdateOrAdd(archetypes.GetByName(n) is { } a      ? ProjectArchetype(a)      : null);
        subsidiaries.OnItemSaved    += n => UpdateOrAdd(subsidiaries.GetByName(n) is { } s    ? ProjectSubsidiary(s)     : null);
        entertainment.OnItemSaved   += n => UpdateOrAdd(entertainment.GetByName(n) is { } e   ? ProjectEntertainment(e)  : null);
        consumerGoods.OnItemSaved   += n => UpdateOrAdd(consumerGoods.GetByName(n) is { } g   ? ProjectConsumerGood(g)   : null);
        vocabulary.OnItemSaved      += n => UpdateOrAdd(vocabulary.GetByName(n) is { } v      ? ProjectVocabulary(v)     : null);
        quotes.OnItemSaved          += n => UpdateOrAdd(quotes.GetByName(n) is { } q          ? ProjectQuote(q)          : null);
        news.OnItemSaved            += n => UpdateOrAdd(news.GetByName(n) is { } x            ? ProjectNews(x)           : null);
        contracts.OnItemSaved       += n => UpdateOrAdd(contracts.GetByName(n) is { } c       ? ProjectContract(c)       : null);
        documents.OnItemSaved       += n => UpdateOrAdd(documents.GetByName(n) is { } d       ? ProjectDocument(d)       : null);
        labSpecimens.OnItemSaved    += n => UpdateOrAdd(labSpecimens.GetByName(n) is { } s    ? ProjectLabSpecimen(s)    : null);
        flyoverEntities.OnItemSaved += n => UpdateOrAdd(flyoverEntities.GetByName(n) is { } w ? ProjectFlyoverEntity(w)  : null);
        psionics.OnItemSaved        += n => UpdateOrAdd(psionics.GetByName(n) is { } p        ? ProjectPsionic(p)        : null);
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

    /// <summary>
    /// Eagerly build the index; called by the warm-up hosted service so the
    /// first user-triggered <see cref="Search"/> doesn't pay the ~40 s cold
    /// deserialize-everything cost.
    /// </summary>
    public void WarmUp() => EnsureBuilt();

    private void Invalidate() { lock (syncLock) { index = []; } }

    private void EnsureBuilt()
    {
        lock (syncLock)
        {
            if (index.Count > 0) return;
            RebuildIndex();
        }
    }

    /// <summary>
    /// Replace (or append) a single entry on save. Avoids the prior full-index
    /// nuke that pushed the next page view back to a 40 s cold rebuild.
    /// If the index hasn't been built yet, a no-op — the deferred full build
    /// will pick up the saved row from the DB.
    /// </summary>
    private void UpdateOrAdd(SearchIndexEntry? entry)
    {
        if (entry is null) return;
        lock (syncLock)
        {
            if (index.Count == 0) return;
            index.RemoveAll(e => e.Id == entry.Id);
            index.Add(entry);
        }
    }

    private void RebuildIndex()
    {
        var entries = new List<SearchIndexEntry>(4096);

        foreach (var c in characters.GetAll())      entries.Add(ProjectCharacter(c));
        foreach (var c in corponations.GetAll())    entries.Add(ProjectCorponation(c));
        foreach (var d in districts.GetAll())       entries.Add(ProjectDistrict(d));
        foreach (var f in factions.GetAll())        entries.Add(ProjectFaction(f));
        foreach (var w in weaponry.GetAll())        entries.Add(ProjectWeapon(w));
        foreach (var a in ammunition.GetAll())      entries.Add(ProjectAmmunition(a));
        foreach (var e in equipment.GetAll())       entries.Add(ProjectEquipment(e));
        foreach (var t in technology.GetAll())      entries.Add(ProjectTechnology(t));
        foreach (var c in cyberware.GetAll())       entries.Add(ProjectCyberware(c));
        foreach (var a in apparel.GetAll())         entries.Add(ProjectApparel(a));
        foreach (var g in genemods.GetAll())        entries.Add(ProjectGenemod(g));
        foreach (var p in pharmaceuticals.GetAll()) entries.Add(ProjectPharmaceutical(p));
        foreach (var m in materials.GetAll())       entries.Add(ProjectMaterial(m));
        foreach (var t in transportation.GetAll())  entries.Add(ProjectTransportation(t));
        foreach (var a in automata.GetAll())        entries.Add(ProjectAutomaton(a));
        foreach (var a in archetypes.GetAll())      entries.Add(ProjectArchetype(a));
        foreach (var s in subsidiaries.GetAll())    entries.Add(ProjectSubsidiary(s));
        foreach (var e in entertainment.GetAll())   entries.Add(ProjectEntertainment(e));
        foreach (var g in consumerGoods.GetAll())   entries.Add(ProjectConsumerGood(g));
        foreach (var v in vocabulary.GetAll())      entries.Add(ProjectVocabulary(v));
        foreach (var q in quotes.GetAll())          entries.Add(ProjectQuote(q));
        foreach (var n in news.GetAll())            entries.Add(ProjectNews(n));
        foreach (var c in contracts.GetAll())       entries.Add(ProjectContract(c));
        foreach (var d in documents.GetAll())       entries.Add(ProjectDocument(d));
        foreach (var s in labSpecimens.GetAll())    entries.Add(ProjectLabSpecimen(s));
        foreach (var w in flyoverEntities.GetAll()) entries.Add(ProjectFlyoverEntity(w));
        foreach (var p in psionics.GetAll())        entries.Add(ProjectPsionic(p));

        index = entries;
    }

    // ── Per-type projections (shared between RebuildIndex and OnItemSaved) ────

    private static SearchIndexEntry ProjectCharacter(CharacterData c)
        => new(c.Id, "character", c.Name, c.Role, c.Description, c.Tags, "/characters");


    private static SearchIndexEntry ProjectCorponation(CorponationData c)
        => new(c.Id, "corponation", c.Name, c.Sector, $"{c.FoundingStory} {c.KeyDetail}", c.Tags, "/corps");

    private static SearchIndexEntry ProjectDistrict(DistrictData d)
        => new(d.Id, "place", d.Name, "", d.Description, d.Tags, "/places");

    private static SearchIndexEntry ProjectFaction(FactionData f)
        => new(f.Id, "faction", f.Name, f.Motto, $"{f.Description} {f.Ideology}", f.Tags, "/factions");

    private static SearchIndexEntry ProjectWeapon(WeaponryData w)
        => new(w.Id, "weapon", w.Name, w.Category, $"{w.Description} {w.CulturalContext}", w.Tags, "/weaponry");

    private static SearchIndexEntry ProjectAmmunition(AmmunitionData a)
        => new(a.Id, "ammunition", a.Name, a.Category, $"{a.Description} {a.CulturalContext}", a.Tags, "/ammunition");

    private static SearchIndexEntry ProjectEquipment(EquipmentData e)
        => new(e.Id, "equipment", e.Name, e.Category, $"{e.Description} {e.CulturalContext}", e.Tags, "/equipment");

    private static SearchIndexEntry ProjectTechnology(TechnologyData t)
        => new(t.Id, "technology", t.Name, t.Subcategory, t.Description, t.Tags, "/technology");

    private static SearchIndexEntry ProjectCyberware(CyberwareData c)
        => new(c.Id, "cyberware", c.Name, $"{c.Category} — {c.BodyLocation}", $"{c.Description} {c.CulturalContext}", c.Tags, "/cyberware");

    private static SearchIndexEntry ProjectApparel(ApparelData a)
        => new(a.Id, "apparel", a.Name, a.Category, $"{a.Description} {a.Functionality}", a.Tags, "/apparel");

    private static SearchIndexEntry ProjectGenemod(GenemodData g)
        => new(g.Id, "genemod", g.Name, g.Category, g.Description, g.Tags, "/genemods");

    private static SearchIndexEntry ProjectPharmaceutical(PharmaceuticalData p)
        => new(p.Id, "pharmaceutical", p.Name, p.Category, $"{p.Description} {p.CulturalContext}", p.Tags, "/pharmaceuticals");

    private static SearchIndexEntry ProjectMaterial(MaterialData m)
        => new(m.Id, "material", m.Name, m.Category, m.Description, m.Tags, "/materials");

    private static SearchIndexEntry ProjectTransportation(TransportationData t)
        => new(t.Id, "transportation", t.Name, t.Category, $"{t.Description} {t.CommonUsage}", t.Tags, "/transportation");

    private static SearchIndexEntry ProjectAutomaton(AutomatonData a)
        => new(a.Id, "automaton", a.Name, a.Classification, $"{a.Description} {a.CulturalContext}", a.Tags, "/automata");

    private static SearchIndexEntry ProjectArchetype(ArchetypeData a)
        => new(a.Id, "archetype", a.Name, a.Category, a.Description, a.Tags, "/archetypes");

    private static SearchIndexEntry ProjectSubsidiary(SubsidiaryData s)
        => new(s.Id, "subsidiary", s.Name, $"{s.ParentCorponation} — {s.LineOfBusiness}", s.Description, s.Tags, "/subsidiaries");

    private static SearchIndexEntry ProjectEntertainment(EntertainmentData e)
        => new(e.Id, "entertainment", e.Name, e.Category, e.Description, e.Tags, "/entertainment");

    private static SearchIndexEntry ProjectConsumerGood(ConsumerGoodData g)
        => new(g.Id, "consumer-good", g.Name, g.Category, $"{g.Description} {g.CulturalContext}", g.Tags, "/goods");

    private static SearchIndexEntry ProjectVocabulary(VocabularyData v)
        => new(v.Id, "vocabulary", v.Term, v.Category, $"{v.Definition} {v.Usage}", v.Tags, "/vocabulary");

    private static SearchIndexEntry ProjectQuote(QuoteData q)
        => new(q.Id, "quote", q.Attribution, q.Category, $"{q.Quote} {q.Context}", q.Tags, "/quotes");

    private static SearchIndexEntry ProjectNews(NewsData n)
        => new(n.Id, "news", n.Headline, n.Category, $"{n.Body} {n.Aftermath}", n.Tags, "/news");

    private static SearchIndexEntry ProjectContract(ContractData c)
        => new(c.Id, "contract", c.Codename, c.Category, $"{c.Description} {c.Objective}", c.Tags, "/contracts");

    private static SearchIndexEntry ProjectDocument(WorldbuildingDocument d)
        => new(d.Id, "document", d.Title, d.Category, d.Body, d.Tags, "/documents");

    private static SearchIndexEntry ProjectLabSpecimen(LabSpecimenData s)
        => new(s.Id, "lab-specimen", s.Name, s.Classification, $"{s.PhysicalDescription} {s.BehavioralProfile} {s.PitiableQualities}", s.Tags, "/specimens");

    private static SearchIndexEntry ProjectFlyoverEntity(FlyoverEntityData w)
        => new(w.Id, "flyover-entity", w.Name, w.Classification, $"{w.PhysicalDescription} {w.BehavioralProfile} {w.HumanRemnants}", w.Tags, "/flyover");

    private static SearchIndexEntry ProjectPsionic(PsionicData p)
        => new(p.Id, "psionic", p.Name, p.Classification, $"{p.Mechanism} {p.Abilities} {p.SideEffects}", p.Tags, "/psionics");

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
