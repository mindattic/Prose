using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Validates the IWorldRecord / ICanonEntity interface split, lore rules enforced
/// in code and data, and XrefService NER (named entity recognition) scanning.
/// </summary>
[TestFixture]
public class WorldLoreTests
{
    // ── Interface hierarchy ───────────────────────────────────────────────────

    [Test]
    public void ICanonEntity_Extends_IWorldRecord()
    {
        Assert.That(typeof(IWorldRecord).IsAssignableFrom(typeof(ICanonEntity)),
            "ICanonEntity must extend IWorldRecord");
    }

    // World records: ambient data — no graph position, no rating
    [TestCase(typeof(QuoteData))]
    [TestCase(typeof(VocabularyData))]
    [TestCase(typeof(ArchetypeData))]
    [TestCase(typeof(MotifData))]
    public void WorldRecordTypes_ImplementIWorldRecord_NotICanonEntity(Type t)
    {
        Assert.That(typeof(IWorldRecord).IsAssignableFrom(t),
            $"{t.Name} must implement IWorldRecord");
        Assert.That(!typeof(ICanonEntity).IsAssignableFrom(t),
            $"{t.Name} must NOT implement ICanonEntity — it is ambient world data, not a graph entity");
    }

    // World records must NOT have a Rating property (no LLMVoting on ambient data)
    [TestCase(typeof(QuoteData))]
    [TestCase(typeof(VocabularyData))]
    [TestCase(typeof(ArchetypeData))]
    [TestCase(typeof(MotifData))]
    public void WorldRecordTypes_DoNotHaveRating(Type t)
    {
        var prop = t.GetProperty("Rating");
        Assert.That(prop, Is.Null,
            $"{t.Name} must not have a Rating — world records are not rated by LLMVoting");
    }

    // Graph entities: have a creator/manufacturer/author, are graph-connected
    [TestCase(typeof(CharacterData))]
    [TestCase(typeof(WeaponryData))]
    [TestCase(typeof(EquipmentData))]
    [TestCase(typeof(TechnologyData))]
    [TestCase(typeof(CyberwareData))]
    [TestCase(typeof(FactionData))]
    [TestCase(typeof(GenemodData))]
    [TestCase(typeof(TransportationData))]
    [TestCase(typeof(PharmaceuticalData))]
    [TestCase(typeof(ConsumerGoodData))]
    [TestCase(typeof(MaterialData))]
    [TestCase(typeof(EntertainmentData))]
    [TestCase(typeof(ApparelData))]
    [TestCase(typeof(NewsData))]
    public void EntityTypes_ImplementICanonEntity(Type t)
    {
        Assert.That(typeof(ICanonEntity).IsAssignableFrom(t),
            $"{t.Name} must implement ICanonEntity");
        Assert.That(typeof(IWorldRecord).IsAssignableFrom(t),
            $"{t.Name} must also satisfy IWorldRecord (via ICanonEntity)");
    }

    // ── Repository ID handling via IWorldRecord ───────────────────────────────

    [Test]
    public void QuoteRepository_SaveAndRetrieveById_ViaIWorldRecord()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ss_wltest_{Guid.NewGuid():N}");
        var paths = new TestPathProviderWithRoot(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "engine_data", "quotes"));
        try
        {
            var repo = new QuoteRepository(paths);
            var quote = new QuoteData { Quote = "The Glooms never sleeps.", Category = "proverb" };
            repo.Save(quote);
            var retrieved = repo.GetById(quote.Id);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Quote, Is.EqualTo("The Glooms never sleeps."));
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Test]
    public void VocabularyRepository_SaveAndRetrieveById_ViaIWorldRecord()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ss_wltest_{Guid.NewGuid():N}");
        var paths = new TestPathProviderWithRoot(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "engine_data", "vocabulary"));
        try
        {
            var repo = new VocabularyRepository(paths);
            var entry = new VocabularyData { Term = "thrumline", Definition = "8Hz vibration above Pulse corridors" };
            repo.Save(entry);
            var retrieved = repo.GetById(entry.Id);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Term, Is.EqualTo("thrumline"));
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    [Test]
    public void ArchetypeRepository_SaveAndRetrieveById_ViaIWorldRecord()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ss_wltest_{Guid.NewGuid():N}");
        var paths = new TestPathProviderWithRoot(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "engine_data", "archetypes"));
        try
        {
            var repo = new ArchetypeRepository(paths);
            var archetype = new ArchetypeData { Name = "The Survivor", Description = "Outlasts everything." };
            repo.Save(archetype);
            var retrieved = repo.GetById(archetype.Id);
            Assert.That(retrieved, Is.Not.Null);
            Assert.That(retrieved!.Name, Is.EqualTo("The Survivor"));
        }
        finally { Directory.Delete(tempDir, recursive: true); }
    }

    // ── XrefService NER (plain-text entity scanning) ─────────────────────────

    private string xrefTempDir = "";
    private XrefService xrefSvc = null!;
    private CharacterRepository xrefChars = null!;
    private FactionRepository xrefFactions = null!;
    private SettingsService xrefSettings = null!;

    [OneTimeSetUp]
    public void SetUpXref()
    {
        xrefTempDir = Path.Combine(Path.GetTempPath(), $"ss_ner_{Guid.NewGuid():N}");
        var engDir = Path.Combine(xrefTempDir, "engine_data");
        foreach (var sub in new[] {
            "people", "places", "factions", "corponations", "technology", "vocabulary",
            "weaponry", "ammunition", "equipment", "cyberware", "genemods", "transportation", "automata",
            "subsidiaries", "entertainment", "apparel", "materials", "pharmaceuticals", "consumer_goods",
            "contracts", "lab_specimens", "psionics"
        })
            Directory.CreateDirectory(Path.Combine(engDir, sub));

        var paths = new TestPathProviderWithRoot(xrefTempDir);
        xrefChars = new(paths);
        xrefFactions = new(paths);
        xrefSettings = new SettingsService(xrefTempDir);
        xrefSettings.EnablePlainTextNer = true;
        xrefSvc = new XrefService(
            xrefChars, new(paths), xrefFactions, new(paths), new(paths),
            new(paths), new(paths), new(paths), new(paths), new(paths), new(paths),
            new(paths), new(paths), new(paths), new(paths), new(paths), new(paths),
            new(paths), new(paths), new(paths), new(paths), new(paths),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<XrefService>.Instance,
            xrefSettings, null!);
    }

    [OneTimeTearDown]
    public void TearDownXref()
    {
        xrefSettings?.Dispose();
        if (Directory.Exists(xrefTempDir)) Directory.Delete(xrefTempDir, recursive: true);
    }

    [Test]
    public void NER_FindsEntityInPlainText()
    {
        xrefChars.Save(new CharacterData { Name = "Sable Orr", Role = "Fixer" });
        var segments = xrefSvc.ParseSegments("Sable Orr owes you one.");
        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(xrefs[0].Text, Is.EqualTo("Sable Orr"));
    }

    [Test]
    public void NER_LongestMatchWins()
    {
        xrefChars.Save(new CharacterData { Name = "Gray Zone Runner", Role = "Courier" });
        xrefFactions.Save(new FactionData { Name = "Gray Zone", Motto = "Survive." });
        var segments = xrefSvc.ParseSegments("The Gray Zone Runner took the job.");
        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs.Any(x => x.Text == "Gray Zone Runner"),
            "Longer name 'Gray Zone Runner' should win over 'Gray Zone'");
    }

    [Test]
    public void NER_WikiLinkSyntaxStillWorks()
    {
        xrefChars.Save(new CharacterData { Name = "Mika Sorel", Role = "Analyst" });
        var segments = xrefSvc.ParseSegments("[[Mika Sorel]] filed the report.");
        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs, Has.Count.EqualTo(1));
        Assert.That(xrefs[0].Text, Is.EqualTo("Mika Sorel"));
    }

    [Test]
    public void NER_DoesNotMatchSubstring()
    {
        xrefChars.Save(new CharacterData { Name = "Arc", Role = "Test" });
        // "Arc" should not match inside "Arcturus"
        var segments = xrefSvc.ParseSegments("Arcturus held the contract.");
        var xrefs = segments.OfType<XrefSegment>().ToList();
        Assert.That(xrefs.All(x => x.Text != "Arc"),
            "'Arc' must not match inside 'Arcturus' — word boundary required");
    }
}
