using System.Text.Json;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

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
    [TestCase(typeof(FacetData))]
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
    [TestCase(typeof(FacetData))]
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
    [TestCase(typeof(SyntheticLifeData))]
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

    // ── Lore rules: banned terms in live data ─────────────────────────────────

    private static readonly string DataRoot = "engine/data";
    private static readonly string[] SkipDirs = ["graph", "archives"];

    private static IEnumerable<string> LiveJsonFiles()
    {
        if (!Directory.Exists(DataRoot)) yield break;
        foreach (var file in Directory.EnumerateFiles(DataRoot, "*.json", SearchOption.AllDirectories))
        {
            var parts = file.Replace('\\', '/').Split('/');
            if (parts.Any(p => SkipDirs.Contains(p, StringComparer.OrdinalIgnoreCase))) continue;
            yield return file;
        }
    }

    [Test]
    public void LiveData_NoRetiredShelfProperNoun()
    {
        var violations = new List<string>();
        foreach (var file in LiveJsonFiles())
        {
            var text = File.ReadAllText(file);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bThe Shelf\b|\bShelf District\b|\bShelf Community\b"))
                violations.Add(Path.GetRelativePath(DataRoot, file));
        }
        Assert.That(violations, Is.Empty,
            "Retired term 'The Shelf' (proper noun) found in:\n" + string.Join("\n", violations));
    }

    [Test]
    public void LiveData_NoRetiredSprawl()
    {
        var violations = new List<string>();
        foreach (var file in LiveJsonFiles())
        {
            var text = File.ReadAllText(file);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bthe [Ss]prawl\b"))
                violations.Add(Path.GetRelativePath(DataRoot, file));
        }
        Assert.That(violations, Is.Empty,
            "Retired term 'the Sprawl' found in:\n" + string.Join("\n", violations));
    }

    [Test]
    public void LiveData_NoRetiredFlyover()
    {
        var violations = new List<string>();
        foreach (var file in LiveJsonFiles())
        {
            var text = File.ReadAllText(file);
            // "Flyover" as a standalone GLMZ condescension term — retired in favour of "The Gap"
            // Allowed: "Flyover" inside fiction titles referencing the old term as a known cultural artifact
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bFlyover\b") &&
                !text.Contains("formerly called") && !text.Contains("old term"))
                violations.Add(Path.GetRelativePath(DataRoot, file));
        }
        // This is a warning-level check — "The Gap" is the current term
        Assert.Warn("Files still using 'Flyover': " + string.Join(", ", violations));
    }

    [Test]
    public void LiveData_NoMeridianPDAsActiveInstitution()
    {
        var violations = new List<string>();
        foreach (var file in LiveJsonFiles())
        {
            var text = File.ReadAllText(file);
            // Meridian PD dissolved 2208. References to it as a current institution are wrong.
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"Meridian PD|Meridian Police Department") &&
                !text.Contains("dissolved") && !text.Contains("former") && !text.Contains("disbanded"))
                violations.Add(Path.GetRelativePath(DataRoot, file));
        }
        Assert.That(violations, Is.Empty,
            "Meridian PD referenced as active institution in:\n" + string.Join("\n", violations));
    }

    [Test]
    public void LiveData_ShelfLifeNotReplaced()
    {
        // "shelf life" (product viability term) must not have been incorrectly scrubbed
        var violations = new List<string>();
        foreach (var file in LiveJsonFiles())
        {
            var text = File.ReadAllText(file);
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\bsprawl life\b|\bgray zone life:\s*\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                violations.Add(Path.GetRelativePath(DataRoot, file));
        }
        Assert.That(violations, Is.Empty,
            "Product 'shelf life' incorrectly scrubbed in:\n" + string.Join("\n", violations));
    }

    // ── Story bible structural validation ────────────────────────────────────

    [Test]
    public void StoryBible_HasRequiredWorldFields()
    {
        var path = Path.Combine(DataRoot, "story_bible.json");
        Assume.That(File.Exists(path), "story_bible.json not found");
        var doc = JsonDocument.Parse(File.ReadAllText(path)).RootElement;

        Assert.That(doc.TryGetProperty("setting", out _), "story_bible must have 'setting'");
        Assert.That(doc.TryGetProperty("currency", out _), "story_bible must have 'currency'");
        Assert.That(doc.TryGetProperty("world_rules", out _), "story_bible must have 'world_rules'");
        Assert.That(doc.TryGetProperty("entity_types", out _), "story_bible must have 'entity_types'");

        var setting = doc.GetProperty("setting");
        Assert.That(setting.TryGetProperty("names", out _), "setting must have 'names' (GLMZ/Meridian88/Glooms)");
        Assert.That(setting.TryGetProperty("spine", out _), "setting must have 'spine'");
        Assert.That(setting.TryGetProperty("infrastructure", out _), "setting must have 'infrastructure'");

        var worldRules = doc.GetProperty("world_rules");
        Assert.That(worldRules.TryGetProperty("gray_zone_as_dmz", out _), "world_rules must have 'gray_zone_as_dmz'");
        Assert.That(worldRules.TryGetProperty("no_police", out _), "world_rules must have 'no_police'");
        Assert.That(worldRules.TryGetProperty("behemoths", out _), "world_rules must have 'behemoths'");
    }

    [Test]
    public void StoryBible_CurrencyFieldMentionsPhi()
    {
        var path = Path.Combine(DataRoot, "story_bible.json");
        Assume.That(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.That(text, Does.Contain("Φ"), "story_bible currency field must reference the Φ symbol");
        Assert.That(text, Does.Contain("QUANTA"), "story_bible currency field must name QUANTA");
    }

    [Test]
    public void LiteraryRules_HasGrayZoneDmzProhibition()
    {
        var path = Path.Combine(DataRoot, "literary_rules.json");
        Assume.That(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.That(text, Does.Contain("DMZ").Or.Contain("corponation territories adjacent"),
            "literary_rules must prohibit adjacent corponation zones without Gray Zone buffer");
    }

    [Test]
    public void LiteraryRules_HasNoPoliceProhibition()
    {
        var path = Path.Combine(DataRoot, "literary_rules.json");
        Assume.That(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.That(text, Does.Contain("no police").Or.Contain("city police"),
            "literary_rules must prohibit invoking city police that do not exist");
    }

    [Test]
    public void ToneBible_HasGrayZoneTag()
    {
        var path = Path.Combine(DataRoot, "cyberpunk_tone_bible.json");
        Assume.That(File.Exists(path));
        var doc = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var tags = doc.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.That(tags, Does.Contain("gray-zone"), "tone_bible tags must include 'gray-zone'");
        Assert.That(tags, Does.Contain("pulse"), "tone_bible tags must include 'pulse'");
        Assert.That(tags, Does.Contain("arcturus"), "tone_bible tags must include 'arcturus'");
        Assert.That(tags, Does.Contain("dmz"), "tone_bible tags must include 'dmz'");
    }

    // ── XrefService NER (plain-text entity scanning) ─────────────────────────

    private string xrefTempDir = "";
    private XrefService xrefSvc = null!;
    private CharacterRepository xrefChars = null!;
    private FactionRepository xrefFactions = null!;

    [OneTimeSetUp]
    public void SetUpXref()
    {
        xrefTempDir = Path.Combine(Path.GetTempPath(), $"ss_ner_{Guid.NewGuid():N}");
        var engDir = Path.Combine(xrefTempDir, "engine_data");
        foreach (var sub in new[] {
            "people", "synthetics", "places", "factions", "corponations", "technology", "vocabulary",
            "weaponry", "ammunition", "equipment", "cyberware", "genemods", "transportation", "automata",
            "subsidiaries", "entertainment", "apparel", "materials", "pharmaceuticals", "consumer_goods",
            "contracts", "lab_specimens", "psionics"
        })
            Directory.CreateDirectory(Path.Combine(engDir, sub));

        var paths = new TestPathProviderWithRoot(xrefTempDir);
        xrefChars = new(paths);
        xrefFactions = new(paths);
        xrefSvc = new XrefService(
            xrefChars, new(paths), new(paths), xrefFactions, new(paths), new(paths),
            new(paths), new(paths), new(paths), new(paths), new(paths), new(paths),
            new(paths), new(paths), new(paths), new(paths), new(paths), new(paths),
            new(paths), new(paths), new(paths), new(paths), new(paths),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<XrefService>.Instance);
    }

    [OneTimeTearDown]
    public void TearDownXref()
    {
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

    // ── Pulse lore: vocabulary, technology, and connective tissue ────────────

    [Test]
    public void PulseLore_ThrumlineVocabularyEntryExists()
    {
        var vocabDir = Path.Combine(DataRoot, "vocabulary");
        Assume.That(Directory.Exists(vocabDir));
        var found = Directory.EnumerateFiles(vocabDir, "*.json")
            .Any(f => File.ReadAllText(f).Contains("\"Thrumline\"") ||
                      File.ReadAllText(f).Contains("\"thrumline\""));
        Assert.That(found, "A vocabulary entry for 'Thrumline' must exist — it is a core Gray Zone term for the 8 Hz Pulse sensation");
    }

    [Test]
    public void PulseLore_SlugVocabularyEntryExists()
    {
        var vocabDir = Path.Combine(DataRoot, "vocabulary");
        Assume.That(Directory.Exists(vocabDir));
        var found = Directory.EnumerateFiles(vocabDir, "*.json")
            .Any(f =>
            {
                var text = File.ReadAllText(f);
                var doc = JsonDocument.Parse(text).RootElement;
                return doc.TryGetProperty("term", out var term) &&
                       (term.GetString() == "Slug" || term.GetString() == "slug");
            });
        Assert.That(found, "A vocabulary entry for 'Slug' (Pulse transit pod) must exist");
    }

    [Test]
    public void PulseLore_GIDSTechnologyEntryExists()
    {
        var techDir = Path.Combine(DataRoot, "technology");
        Assume.That(Directory.Exists(techDir));
        var found = Directory.EnumerateFiles(techDir, "*.json")
            .Any(f =>
            {
                var text = File.ReadAllText(f);
                return text.Contains("Gradient Inertial Distribution") || text.Contains("GIDS");
            });
        Assert.That(found, "A technology entry for the GIDS (Gradient Inertial Distribution System) must exist — it explains how Mach 6 transit is survivable");
    }

    [Test]
    public void PulseLore_MainDocumentMentionsThrumlineAndEightHz()
    {
        var pulseDoc = Path.Combine(DataRoot, "documents", "eecbb1e789d840519cc67bc604e3738e.json");
        Assume.That(File.Exists(pulseDoc), "Main Pulse document must exist");
        var text = File.ReadAllText(pulseDoc);
        Assert.That(text, Does.Contain("thrumline"), "Pulse document must mention thrumline");
        Assert.That(text, Does.Contain("8 Hz"), "Pulse document must describe the 8 Hz coil vibration");
    }

    [Test]
    public void PulseLore_GIDSDocumentMentionsAugmentationCoupling()
    {
        var techDir = Path.Combine(DataRoot, "technology");
        Assume.That(Directory.Exists(techDir));
        // The GIDS science: augmentations are the coupling points for the field
        var gidsFile = Directory.EnumerateFiles(techDir, "*.json")
            .FirstOrDefault(f => File.ReadAllText(f).Contains("Gradient Inertial Distribution"));
        Assume.That(gidsFile is not null, "GIDS technology file must exist");
        var text = File.ReadAllText(gidsFile!);
        Assert.That(text, Does.Contain("augment").IgnoreCase,
            "GIDS entry must explain the augmentation coupling mechanism");
        Assert.That(text, Does.Contain("8 Hz").Or.Contains("8Hz"),
            "GIDS entry must reference the 8 Hz coil tick that becomes the thrumline");
    }

    [Test]
    public void AllJsonFiles_AreValidJson()
    {
        var failures = new List<string>();
        foreach (var file in LiveJsonFiles())
        {
            try { JsonDocument.Parse(File.ReadAllText(file)); }
            catch (JsonException ex) { failures.Add($"{Path.GetRelativePath(DataRoot, file)}: {ex.Message}"); }
        }
        Assert.That(failures, Is.Empty,
            "Invalid JSON found:\n" + string.Join("\n", failures));
    }
}
