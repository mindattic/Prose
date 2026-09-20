using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for GlobalSearchService — full-text and tag search across all repos.
/// </summary>
[TestFixture]
public class GlobalSearchServiceTests
{
    private string tempDir = "";
    private GlobalSearchService svc = null!;

    // All repos needed by GlobalSearchService
    private CharacterRepository chars = null!;
    private CorponationRepository corps = null!;
    private DistrictRepository districts = null!;
    private FactionRepository factions = null!;
    private WeaponryRepository weaponry = null!;
    private AmmunitionRepository ammunition = null!;
    private EquipmentRepository equipment = null!;
    private TechnologyRepository technology = null!;
    private CyberwareRepository cyberware = null!;
    private ApparelRepository apparel = null!;
    private GenemodRepository genemods = null!;
    private PharmaceuticalRepository pharmaceuticals = null!;
    private MaterialRepository materials = null!;
    private TransportationRepository transportation = null!;
    private AutomatonRepository automata = null!;
    private ArchetypeRepository archetypes = null!;
    private SubsidiaryRepository subsidiaries = null!;
    private EntertainmentRepository entertainment = null!;
    private ConsumerGoodRepository consumerGoods = null!;
    private VocabularyRepository vocabulary = null!;
    private QuoteRepository quotes = null!;
    private NewsRepository news = null!;
    private ContractRepository contracts = null!;
    private WorldbuildingDocRepository documents = null!;
    private LabSpecimenRepository labSpecimens = null!;
    private FlyoverEntityRepository flyoverEntities = null!;
    private PsionicRepository psionics = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_search_{Guid.NewGuid():N}");
        var engDir = Path.Combine(tempDir, "engine_data");
        foreach (var sub in new[] {
            "people", "corponations", "places", "factions",
            "weaponry", "ammunition", "equipment", "technology", "cyberware",
            "apparel", "genemods", "pharmaceuticals", "materials", "transportation",
            "automata", "archetypes", "subsidiaries", "entertainment", "consumer_goods",
            "vocabulary", "quotes", "news", "contracts", "documents",
            "lab_specimens", "flyover_entities", "psionics"
        }) Directory.CreateDirectory(Path.Combine(engDir, sub));

        var paths = new TestPathProviderWithRoot(tempDir);
        chars = new(paths);
        corps = new(paths); districts = new(paths); factions = new(paths);
        weaponry = new(paths); ammunition = new(paths); equipment = new(paths);
        technology = new(paths); cyberware = new(paths); apparel = new(paths);
        genemods = new(paths); pharmaceuticals = new(paths); materials = new(paths);
        transportation = new(paths); automata = new(paths); archetypes = new(paths);
        subsidiaries = new(paths); entertainment = new(paths); consumerGoods = new(paths);
        vocabulary = new(paths); quotes = new(paths); news = new(paths);
        contracts = new(paths); documents = new(paths);
        labSpecimens = new(paths); flyoverEntities = new(paths); psionics = new(paths);

        svc = new GlobalSearchService(
            chars, corps, districts, factions,
            weaponry, ammunition, equipment, technology, cyberware,
            apparel, genemods, pharmaceuticals, materials, transportation,
            automata, archetypes, subsidiaries, entertainment, consumerGoods,
            vocabulary, quotes, news, contracts, documents,
            labSpecimens, flyoverEntities, psionics);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Empty state ──────────────────────────────────────────────────────────

    [Test]
    public void Search_EmptyIndex_ReturnsEmpty()
    {
        var results = svc.Search("anything");
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void SearchByTag_EmptyIndex_ReturnsEmpty()
    {
        var results = svc.SearchByTag("mytag");
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void AllTags_EmptyIndex_ReturnsEmpty()
    {
        var tags = svc.AllTags();
        Assert.That(tags, Is.Empty);
    }

    // ── Name matching ────────────────────────────────────────────────────────

    [Test]
    public void Search_ExactNameMatch_ReturnsResult()
    {
        chars.Save(new CharacterData { Name = "Kyle Morrow", Description = "A runner." });

        var results = svc.Search("Kyle Morrow");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Kyle Morrow"));
    }

    [Test]
    public void Search_PartialNameMatch_ReturnsResult()
    {
        chars.Save(new CharacterData { Name = "Kyle Morrow", Description = "A runner." });

        var results = svc.Search("Kyle");

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public void Search_ExactNameMatch_ScoresHigherThanPartial()
    {
        chars.Save(new CharacterData { Name = "Street Runner", Description = "test" });
        chars.Save(new CharacterData { Name = "The Street Samurai of Old Street", Description = "test" });

        var results = svc.Search("Street Runner");

        Assert.That(results[0].Name, Is.EqualTo("Street Runner"));
    }

    // ── Cross-repo search ────────────────────────────────────────────────────

    [Test]
    public void Search_FindsAcrossMultipleRepos()
    {
        chars.Save(new CharacterData { Name = "Ghost Walker", Description = "Infiltrator." });
        weaponry.Save(new WeaponryData { Name = "Ghost Blade", Description = "Ceramic knife." });

        var results = svc.Search("Ghost");

        Assert.That(results, Has.Count.EqualTo(2));
        var types = results.Select(r => r.Type).ToHashSet();
        Assert.That(types, Does.Contain("character"));
        Assert.That(types, Does.Contain("weapon"));
    }

    [Test]
    public void Search_FindsInDescription()
    {
        weaponry.Save(new WeaponryData { Name = "Razor Edge", Description = "Used by street samurai." });

        var results = svc.Search("street samurai");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Razor Edge"));
    }

    // ── Tag search ───────────────────────────────────────────────────────────

    [Test]
    public void SearchByTag_FindsMatchingEntities()
    {
        chars.Save(new CharacterData { Name = "Neon Ghost", Tags = ["supermind", "augmented"] });
        weaponry.Save(new WeaponryData { Name = "Mindspike", Tags = ["supermind"] });
        apparel.Save(new ApparelData { Name = "Plain Jacket", Tags = ["street"] });

        var results = svc.SearchByTag("supermind");

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(r => r.Name), Does.Contain("Neon Ghost"));
        Assert.That(results.Select(r => r.Name), Does.Contain("Mindspike"));
    }

    [Test]
    public void SearchByTag_CaseInsensitive()
    {
        chars.Save(new CharacterData { Name = "Alpha", Tags = ["Augmented"] });

        var results = svc.SearchByTag("augmented");

        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public void SearchByTagCount_ReturnsCorrectCount()
    {
        chars.Save(new CharacterData { Name = "A", Tags = ["runner"] });
        chars.Save(new CharacterData { Name = "B", Tags = ["runner"] });
        chars.Save(new CharacterData { Name = "C", Tags = ["fixer"] });

        Assert.That(svc.SearchByTagCount("runner"), Is.EqualTo(2));
        Assert.That(svc.SearchByTagCount("fixer"), Is.EqualTo(1));
        Assert.That(svc.SearchByTagCount("nobody"), Is.EqualTo(0));
    }

    // ── Result routes ────────────────────────────────────────────────────────

    [Test]
    public void Search_ResultRouteUsesEntityId()
    {
        var c = new CharacterData { Name = "Marcus Veil", Description = "Fixer." };
        chars.Save(c);

        var results = svc.Search("Marcus Veil");

        Assert.That(results[0].Route, Is.EqualTo($"/characters?id={c.Id}"));
    }

    [Test]
    public void SearchByTag_ResultRouteUsesEntityId()
    {
        var w = new WeaponryData { Name = "Silence", Tags = ["legendary"] };
        weaponry.Save(w);

        var results = svc.SearchByTag("legendary");

        Assert.That(results[0].Route, Is.EqualTo($"/weaponry?id={w.Id}"));
    }

    // ── Pagination ───────────────────────────────────────────────────────────

    [Test]
    public void Search_Pagination_ReturnsCorrectPage()
    {
        for (int i = 0; i < 25; i++)
            chars.Save(new CharacterData { Name = $"Character{i:D2}", Description = "runner" });

        var page1 = svc.Search("runner", page: 1, pageSize: 10);
        var page2 = svc.Search("runner", page: 2, pageSize: 10);
        var page3 = svc.Search("runner", page: 3, pageSize: 10);

        Assert.That(page1, Has.Count.EqualTo(10));
        Assert.That(page2, Has.Count.EqualTo(10));
        Assert.That(page3, Has.Count.EqualTo(5));
    }

    [Test]
    public void SearchCount_ReturnsTotal()
    {
        for (int i = 0; i < 15; i++)
            chars.Save(new CharacterData { Name = $"Wanderer{i}", Description = "nomad" });

        var count = svc.SearchCount("nomad");

        Assert.That(count, Is.EqualTo(15));
    }

    // ── AllTags ───────────────────────────────────────────────────────────────

    [Test]
    public void AllTags_ReturnsTagsFromAllRepos_WithCounts()
    {
        chars.Save(new CharacterData { Name = "A", Tags = ["runner", "augmented"] });
        chars.Save(new CharacterData { Name = "B", Tags = ["runner"] });
        weaponry.Save(new WeaponryData { Name = "W", Tags = ["street"] });

        var tags = svc.AllTags();
        var dict = tags.ToDictionary(t => t.tag, t => t.count);

        Assert.That(dict["runner"], Is.EqualTo(2));
        Assert.That(dict["augmented"], Is.EqualTo(1));
        Assert.That(dict["street"], Is.EqualTo(1));
    }

    [Test]
    public void AllTags_SortedByFrequencyDescending()
    {
        for (int i = 0; i < 5; i++) chars.Save(new CharacterData { Name = $"X{i}", Tags = ["common"] });
        chars.Save(new CharacterData { Name = "Y", Tags = ["rare"] });

        var tags = svc.AllTags();

        Assert.That(tags[0].tag, Is.EqualTo("common"));
        Assert.That(tags[1].tag, Is.EqualTo("rare"));
    }

    // ── Index invalidation ────────────────────────────────────────────────────

    [Test]
    public void Index_InvalidatedOnSave_PicksUpNewEntity()
    {
        chars.Save(new CharacterData { Name = "First Save" });
        var before = svc.Search("First Save");
        Assert.That(before, Has.Count.EqualTo(1));

        chars.Save(new CharacterData { Name = "Second Save" });
        var after = svc.Search("Second Save");
        Assert.That(after, Has.Count.EqualTo(1));
    }

    // ── Snippet extraction ────────────────────────────────────────────────────

    [Test]
    public void Search_SnippetContainsQueryContext()
    {
        chars.Save(new CharacterData {
            Name = "Test Char",
            Description = "This character is known for exceptional skill in neural hacking techniques."
        });

        var results = svc.Search("neural hacking");

        Assert.That(results[0].Snippet, Does.Contain("neural hacking"));
    }

    // ── Quote repo uses Attribution as Name ──────────────────────────────────

    [Test]
    public void Search_QuoteRepo_FindsByAttribution()
    {
        quotes.Save(new QuoteData {
            Attribution = "Kira Voss",
            Quote = "The street remembers.",
            Tags = []
        });

        var results = svc.Search("Kira Voss");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("quote"));
    }

    // ── Contract repo uses Codename ───────────────────────────────────────────

    [Test]
    public void Search_ContractRepo_FindsByCodename()
    {
        contracts.Save(new ContractData {
            Codename = "Operation Nightfall",
            Description = "Extract target."
        });

        var results = svc.Search("Nightfall");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("contract"));
        Assert.That(results[0].Name, Is.EqualTo("Operation Nightfall"));
    }

    // ── Vocabulary uses Term ──────────────────────────────────────────────────

    [Test]
    public void Search_VocabularyRepo_FindsByTerm()
    {
        vocabulary.Save(new VocabularyData {
            Term = "Ghost Protocol",
            Definition = "A technique for erasing one's digital footprint."
        });

        var results = svc.Search("Ghost Protocol");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("vocabulary"));
    }

    // ── News uses Headline ────────────────────────────────────────────────────

    [Test]
    public void Search_NewsRepo_FindsByHeadline()
    {
        news.Save(new NewsData {
            Headline = "Corp Breach Exposes Millions",
            Body = "A major security failure..."
        });

        var results = svc.Search("Corp Breach");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("news"));
    }

    // ── New repos: LabSpecimen, FlyoverEntity, Psionic ───────────────────────

    [Test]
    public void Search_LabSpecimen_FindsByName()
    {
        labSpecimens.Save(new LabSpecimenData { Name = "Spliced Wraith", Classification = "Chimera" });

        var results = svc.Search("Spliced Wraith");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("lab-specimen"));
    }

    [Test]
    public void Search_LabSpecimen_FindsInBody()
    {
        labSpecimens.Save(new LabSpecimenData {
            Name = "Unit Omega",
            PhysicalDescription = "Six-limbed with acid secretion."
        });

        var results = svc.Search("acid secretion");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Unit Omega"));
    }

    [Test]
    public void Search_FlyoverEntity_FindsByName()
    {
        flyoverEntities.Save(new FlyoverEntityData { Name = "Cloud Pilgrim", Classification = "Ascended" });

        var results = svc.Search("Cloud Pilgrim");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("flyover-entity"));
    }

    [Test]
    public void Search_Psionic_FindsByName()
    {
        psionics.Save(new PsionicData { Name = "Neuroshear", Classification = "Combat" });

        var results = svc.Search("Neuroshear");

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Type, Is.EqualTo("psionic"));
    }

    // ── SearchTiered ──────────────────────────────────────────────────────────

    [Test]
    public void SearchTiered_EmptyQuery_ReturnsBothEmpty()
    {
        chars.Save(new CharacterData { Name = "Vex" });
        var (t1, t2) = svc.SearchTiered("");
        Assert.That(t1, Is.Empty);
        Assert.That(t2, Is.Empty);
    }

    [Test]
    public void SearchTiered_NameMatch_AppearsInTier1()
    {
        chars.Save(new CharacterData { Name = "Axel Rain", Description = "Irrelevant body text." });

        var (t1, t2) = svc.SearchTiered("Axel Rain");

        Assert.That(t1.Any(r => r.Name == "Axel Rain"), Is.True);
        Assert.That(t2.Any(r => r.Name == "Axel Rain"), Is.False);
    }

    [Test]
    public void SearchTiered_BodyOnlyMatch_AppearsInTier2()
    {
        chars.Save(new CharacterData {
            Name = "Random Person",
            Description = "Expert in quantum entanglement protocols."
        });

        var (t1, t2) = svc.SearchTiered("quantum entanglement");

        Assert.That(t1.Any(r => r.Name == "Random Person"), Is.False);
        Assert.That(t2.Any(r => r.Name == "Random Person"), Is.True);
    }

    [Test]
    public void SearchTiered_TagMatch_AppearsInTier1()
    {
        chars.Save(new CharacterData { Name = "Tag Target", Tags = ["psionic-adept"] });

        var (t1, _) = svc.SearchTiered("psionic-adept");

        Assert.That(t1.Any(r => r.Name == "Tag Target"), Is.True);
    }

    [Test]
    public void SearchTiered_Tier1AndTier2_NeverOverlap()
    {
        chars.Save(new CharacterData { Name = "Overlap Test", Description = "unique body phrase here." });
        chars.Save(new CharacterData { Name = "unique body phrase here" });

        var (t1, t2) = svc.SearchTiered("unique body phrase here");

        var t1Ids = t1.Select(r => r.Id).ToHashSet();
        var t2Ids = t2.Select(r => r.Id).ToHashSet();
        Assert.That(t1Ids.Intersect(t2Ids), Is.Empty);
    }
}
