using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests for XrefService — entity name resolution and inline text linking.
/// </summary>
[TestFixture]
public class XrefServiceTests
{
    private string tempDir = "";
    private XrefService svc = null!;
    private CharacterRepository chars = null!;
    private DistrictRepository districts = null!;
    private FactionRepository factions = null!;
    private CorponationRepository corps = null!;
    private TechnologyRepository technology = null!;
    private VocabularyRepository vocabulary = null!;
    private WeaponryRepository weaponry = null!;
    private AmmunitionRepository ammunition = null!;
    private EquipmentRepository equipment = null!;
    private CyberwareRepository cyberware = null!;
    private GenemodRepository genemods = null!;
    private TransportationRepository transportation = null!;
    private AutomatonRepository automata = null!;
    private SubsidiaryRepository subsidiaries = null!;
    private EntertainmentRepository entertainment = null!;
    private ApparelRepository apparel = null!;
    private MaterialRepository materials = null!;
    private PharmaceuticalRepository pharmaceuticals = null!;
    private ConsumerGoodRepository consumerGoods = null!;
    private ContractRepository contracts = null!;
    private LabSpecimenRepository labSpecimens = null!;
    private PsionicRepository psionics = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_xref_{Guid.NewGuid():N}");
        var engDir = Path.Combine(tempDir, "engine_data");
        foreach (var sub in new[] {
            "people", "places", "factions", "corponations", "technology", "vocabulary",
            "weaponry", "ammunition", "equipment", "cyberware", "genemods", "transportation", "automata",
            "subsidiaries", "entertainment", "apparel", "materials", "pharmaceuticals", "consumer_goods",
            "contracts", "lab_specimens", "psionics"
        })
            Directory.CreateDirectory(Path.Combine(engDir, sub));

        var paths = new TestPathProviderWithRoot(tempDir);
        chars = new(paths); districts = new(paths);
        factions = new(paths); corps = new(paths); technology = new(paths);
        vocabulary = new(paths); weaponry = new(paths); ammunition = new(paths);
        equipment = new(paths); cyberware = new(paths); genemods = new(paths);
        transportation = new(paths); automata = new(paths); subsidiaries = new(paths);
        entertainment = new(paths); apparel = new(paths); materials = new(paths);
        pharmaceuticals = new(paths); consumerGoods = new(paths); contracts = new(paths);
        labSpecimens = new(paths); psionics = new(paths);

        var settings = new SettingsService(tempDir);
        settings.EnablePlainTextNer = true;
        svc = new XrefService(
            chars, districts, factions, corps, technology, vocabulary,
            weaponry, ammunition, equipment, cyberware, genemods, transportation,
            automata, subsidiaries, entertainment, apparel, materials,
            pharmaceuticals, consumerGoods, contracts, labSpecimens, psionics,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<XrefService>.Instance,
            settings, null!);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Empty state ──────────────────────────────────────────────────────────

    [Test]
    public void Resolve_UnknownName_ReturnsNull()
    {
        var result = svc.Resolve("Nobody");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseSegments_EmptyText_ReturnsSinglePlainSegment()
    {
        var segments = svc.ParseSegments("");
        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0], Is.InstanceOf<PlainSegment>());
    }

    [Test]
    public void ParseSegments_NoMatches_ReturnsSinglePlainSegment()
    {
        var segments = svc.ParseSegments("no entities mentioned here");
        Assert.That(segments, Has.Count.EqualTo(1));
        Assert.That(segments[0], Is.InstanceOf<PlainSegment>());
        Assert.That(segments[0].Text, Is.EqualTo("no entities mentioned here"));
    }

    // ── Character indexing ───────────────────────────────────────────────────

    [Test]
    public void Resolve_CharacterName_ReturnsEntry()
    {
        chars.Save(new CharacterData { Name = "Kyle Morrow", Role = "Fixer" });

        var result = svc.Resolve("Kyle Morrow");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.DisplayName, Is.EqualTo("Kyle Morrow"));
        Assert.That(result.Type, Is.EqualTo("character"));
    }

    [Test]
    public void Resolve_CharacterAlias_ReturnsEntry()
    {
        var c = new CharacterData { Name = "Elena Vasquez", Role = "Fixer" };
        c.Aliases.Add("Ghost");
        chars.Save(c);

        var result = svc.Resolve("Ghost");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(c.Id));
        Assert.That(result.DisplayName, Is.EqualTo("Ghost"));
    }

    [Test]
    public void Resolve_CaseInsensitive()
    {
        chars.Save(new CharacterData { Name = "Marcus Veil", Role = "Runner" });

        var result = svc.Resolve("marcus veil");

        Assert.That(result, Is.Not.Null);
    }

    // ── Route format ─────────────────────────────────────────────────────────

    [Test]
    public void Resolve_Route_IsRepoListRoute()
    {
        chars.Save(new CharacterData { Name = "Test Character", Role = "Test" });
        var result = svc.Resolve("Test Character");
        Assert.That(result!.Route, Is.EqualTo("/characters"));
    }

    [Test]
    public void Resolve_DistrictRoute_IsPlaces()
    {
        districts.Save(new DistrictData { Name = "The Warrens" });
        var result = svc.Resolve("The Warrens");
        Assert.That(result!.Route, Is.EqualTo("/places"));
        Assert.That(result.Type, Is.EqualTo("place"));
    }

    [Test]
    public void Resolve_FactionRoute_IsFactions()
    {
        factions.Save(new FactionData { Name = "Iron Veil" });
        var result = svc.Resolve("Iron Veil");
        Assert.That(result!.Route, Is.EqualTo("/factions"));
        Assert.That(result.Type, Is.EqualTo("faction"));
    }

    [Test]
    public void Resolve_CorponationRoute_IsCorps()
    {
        corps.Save(new CorponationData { Name = "Axiom Corp", Sector = "Defense" });
        var result = svc.Resolve("Axiom Corp");
        Assert.That(result!.Route, Is.EqualTo("/corps"));
        Assert.That(result.Type, Is.EqualTo("corponation"));
    }

    // ── ParseSegments ────────────────────────────────────────────────────────

    [Test]
    public void ParseSegments_EntityInMiddle_SplitsCorrectly()
    {
        chars.Save(new CharacterData { Name = "Kai Morrow", Role = "Runner" });

        var segments = svc.ParseSegments("Meet Kai Morrow at the bar.");

        Assert.That(segments.Count, Is.GreaterThanOrEqualTo(3));
        var xrefSeg = segments.OfType<XrefSegment>().FirstOrDefault();
        Assert.That(xrefSeg, Is.Not.Null);
        Assert.That(xrefSeg!.Text, Is.EqualTo("Kai Morrow"));
    }

    [Test]
    public void ParseSegments_MultipleEntities_AllLinked()
    {
        chars.Save(new CharacterData { Name = "Kai Morrow", Role = "Runner" });
        factions.Save(new FactionData { Name = "Iron Veil", Motto = "Silence is strength." });

        var segments = svc.ParseSegments("Kai Morrow joined Iron Veil last cycle.");

        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Has.Count.EqualTo(2));
        var names = xrefs.Select(x => x.Text).ToHashSet();
        Assert.That(names, Does.Contain("Kai Morrow"));
        Assert.That(names, Does.Contain("Iron Veil"));
    }

    [Test]
    public void ParseSegments_LongerNameBeatsSubstring()
    {
        chars.Save(new CharacterData { Name = "The Circuit", Role = "Netrunner" });
        factions.Save(new FactionData { Name = "Circuit", Motto = "Test" });

        var segments = svc.ParseSegments("The Circuit runs the net.");

        // "The Circuit" should win over "Circuit" due to longest-match
        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Has.Count.EqualTo(1));
        Assert.That(xrefs[0].Text, Is.EqualTo("The Circuit"));
    }

    // ── Heuristics: capital-letter rule, longest-match, stop words ──────────────

    [Test]
    public void ParseSegments_LowercaseSourceDoesNotMatchProperNoun()
    {
        // Entity "War Machine" (4471-K codename, hypothetical) should not link in
        // a sentence where the source word is lowercase ("war").
        factions.Save(new FactionData { Name = "Storm Brigade", Motto = "Ride or die." });

        var segments = svc.ParseSegments("a storm brigade of cyclists rolled past");

        // Source is fully lowercase — should remain plain.
        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Is.Empty);
    }

    [Test]
    public void ParseSegments_FirearmBeatsManufacturer_LongestMatchWins()
    {
        // Classic case: corporation "Vector Arms" makes firearm "Vector Arms M9".
        // Text mentions the firearm — the firearm's longer name should win.
        corps.Save(new CorponationData { Name = "Vector Arms", Sector = "Firearms" });
        weaponry.Save(new WeaponryData { Name = "Vector Arms M9", Category = "pistol" });

        var segments = svc.ParseSegments("She drew a Vector Arms M9 from her holster.");

        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Has.Count.EqualTo(1));
        Assert.That(xrefs[0].Text, Is.EqualTo("Vector Arms M9"));
        Assert.That(xrefs[0].Entry.Type, Is.EqualTo("weapon"));
    }

    [Test]
    public void ParseSegments_StopWordsNotLinked_EvenWhenIndexed()
    {
        // If someone names a faction "There" or "Time", common-word stop list
        // should keep narration prose ("Time is money") from auto-linking it.
        factions.Save(new FactionData { Name = "There", Motto = "test" });
        factions.Save(new FactionData { Name = "Time",  Motto = "test" });

        var segments = svc.ParseSegments("There is no Time like the present.");

        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Is.Empty);
    }

    [Test]
    public void ParseSegments_ShortNamesNotAutoLinked()
    {
        // 3-character names land in the index but are too noisy for plain-text auto-linking.
        // Explicit [[wiki]] markup should still resolve them (covered in Pass 1).
        chars.Save(new CharacterData { Name = "Kai", Role = "Runner" });

        var segments = svc.ParseSegments("Kai walks the wire.");

        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Is.Empty);
    }

    [Test]
    public void ParseSegments_ExplicitWikiLinkBypassesAutoLinkRules()
    {
        // Even if "Kai" is too short for auto-linking, [[Kai]] markup must still resolve.
        chars.Save(new CharacterData { Name = "Kai", Role = "Runner" });

        var segments = svc.ParseSegments("[[Kai]] walks the wire.");

        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Has.Count.EqualTo(1));
        Assert.That(xrefs[0].Text, Is.EqualTo("Kai"));
    }

    [Test]
    public void ParseSegments_LowercaseEntityNotAutoLinked_ButExplicitWorks()
    {
        // Lowercase slang ("ghosting") should not auto-link in narration —
        // prose like "she was ghosting him" would false-positive.
        // Explicit [[ghosting]] markup should still resolve.
        vocabulary.Save(new VocabularyData { Term = "ghosting", Definition = "Erasing trail." });

        var auto = svc.ParseSegments("She was ghosting him for weeks.");
        Assert.That(auto.OfType<XrefSegment>(), Is.Empty);

        var explicitSeg = svc.ParseSegments("[[ghosting]] is the move.");
        Assert.That(explicitSeg.OfType<XrefSegment>().Count(), Is.EqualTo(1));
    }

    // ── AllEntries ────────────────────────────────────────────────────────────

    [Test]
    public void AllEntries_ReturnsAllIndexedEntities()
    {
        chars.Save(new CharacterData { Name = "Alpha" });
        chars.Save(new CharacterData { Name = "Beta" });
        factions.Save(new FactionData { Name = "The Grid" });

        var entries = svc.AllEntries().ToList();

        Assert.That(entries, Has.Count.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void AllEntries_AliasDoesNotDuplicate_ById()
    {
        var c = new CharacterData { Name = "Ghost Runner" };
        c.Aliases.Add("Ghost");
        chars.Save(c);

        var entries = svc.AllEntries().ToList();

        // Even though name + alias both indexed, AllEntries should dedup by Id
        var matching = entries.Where(e => e.Id == c.Id).ToList();
        Assert.That(matching, Has.Count.EqualTo(1));
    }

    // ── Index invalidation ────────────────────────────────────────────────────

    [Test]
    public void Index_InvalidatedOnSave_PicksUpNewCharacter()
    {
        var before = svc.Resolve("Neon Shadow");
        Assert.That(before, Is.Null);

        chars.Save(new CharacterData { Name = "Neon Shadow" });

        var after = svc.Resolve("Neon Shadow");
        Assert.That(after, Is.Not.Null);
    }


    // ── VocabularyData uses Term ─────────────────────────────────────────────

    [Test]
    public void Resolve_VocabularyTerm_ReturnsEntry()
    {
        vocabulary.Save(new VocabularyData { Term = "Ghosting", Definition = "Erasing your digital trail." });

        var result = svc.Resolve("Ghosting");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Type, Is.EqualTo("vocabulary"));
        Assert.That(result.Route, Is.EqualTo("/vocabulary"));
    }

    // ── Technology uses ProductName ────────────────────────────────────────────

    [Test]
    public void Resolve_Technology_UsesBestAvailableName()
    {
        technology.Save(new TechnologyData { Name = "Neural Bridge", ProductName = "BridgeOS v3", Subcategory = "BCI" });

        // Should find by ProductName (preferred)
        var byProduct = svc.Resolve("BridgeOS v3");
        Assert.That(byProduct, Is.Not.Null);
        Assert.That(byProduct!.Type, Is.EqualTo("technology"));
    }
}
