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
    private SyntheticLifeRepository synths = null!;
    private DistrictRepository districts = null!;
    private FactionRepository factions = null!;
    private CorponationRepository corps = null!;
    private TechnologyRepository technology = null!;
    private VocabularyRepository vocabulary = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_xref_{Guid.NewGuid():N}");
        var engDir = Path.Combine(tempDir, "engine_data");
        foreach (var sub in new[] { "people", "synthetics", "places", "factions", "corponations", "technology", "vocabulary" })
            Directory.CreateDirectory(Path.Combine(engDir, sub));

        var paths = new TestPathProviderWithRoot(tempDir);
        chars = new(paths); synths = new(paths); districts = new(paths);
        factions = new(paths); corps = new(paths); technology = new(paths);
        vocabulary = new(paths);

        svc = new XrefService(chars, synths, districts, factions, corps, technology, vocabulary);
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

    [Test]
    public void Index_Rebuild_SynthsIndexed()
    {
        synths.Save(new SyntheticLifeData { Name = "Unit Seven", Classification = "Security" });

        var result = svc.Resolve("Unit Seven");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Type, Is.EqualTo("synthetic"));
    }

    // ── VocabularyEntry uses Term ─────────────────────────────────────────────

    [Test]
    public void Resolve_VocabularyTerm_ReturnsEntry()
    {
        vocabulary.Save(new VocabularyEntry { Term = "Ghosting", Definition = "Erasing your digital trail." });

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
