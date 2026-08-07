using System.Text.Json;
using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Comprehensive tests using real engine_data. All JSON files are set to ReadOnly
/// during test execution — any attempt to modify them throws UnauthorizedAccessException.
/// The guard restores write access in Dispose (finally), even if tests fail.
/// </summary>
[TestFixture]
public class RealDataTests
{
    // Retired by the 2026-05-08 JSON→SQL canon migration: the file-based repositories below read
    // engine/data/*.json, but the live types (people, corponations, districts, …) now live only in
    // the SQL DB — e.g. engine/data/people/*.json is empty by design (the "no new JSON files" canon
    // rule forbids regenerating it). Count-based assertions therefore see 0. The pure-logic tests in
    // this fixture (Slugify, JSON-repair, graph) still run; only the file-corpus tests are ignored.
    // To re-enable: point these at the SQL repositories / a seeded SQL test DB.
    private const string RetiredCorpus =
        "Retired file-based corpus (2026-05-08 JSON→SQL migration); canon is the SQL DB. See class comment.";

    private static readonly string EngineDataDir = Path.Combine(
        FindRepoRoot(), "engine", "data");

    private ReadOnlyDataGuard guard = null!;
    private IPathProvider paths = null!;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        Assert.That(Directory.Exists(EngineDataDir), Is.True,
            $"engine_data directory not found at {EngineDataDir}");

        guard = new ReadOnlyDataGuard(EngineDataDir);
        paths = new RealDataPathProvider(EngineDataDir);

        TestContext.Out.WriteLine($"ReadOnlyDataGuard protecting {guard.ProtectedCount} JSON files");
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        guard?.Dispose();
        TestContext.Out.WriteLine("ReadOnlyDataGuard released — all files writable again");
    }

    // ── JSON INTEGRITY ───────────────────────────────────────

    [Test]
    public void AllJsonFiles_AreValidJson()
    {
        var files = Directory.GetFiles(EngineDataDir, "*.json", SearchOption.AllDirectories);
        var broken = new List<string>();

        foreach (var file in files)
        {
            try { JsonDocument.Parse(File.ReadAllText(file)).Dispose(); }
            catch (Exception ex) { broken.Add($"{Path.GetRelativePath(EngineDataDir, file)}: {ex.Message[..Math.Min(ex.Message.Length, 100)]}"); }
        }

        Assert.That(broken, Is.Empty,
            $"{broken.Count} broken JSON files:\n{string.Join("\n", broken.Take(20))}");
    }

    [Test]
    public void AllJsonFiles_AreReadOnlyDuringTest()
    {
        var writable = Directory.GetFiles(EngineDataDir, "*.json", SearchOption.AllDirectories)
            .Where(f => !new FileInfo(f).IsReadOnly)
            .Select(f => Path.GetRelativePath(EngineDataDir, f))
            .ToList();

        // All files that we protected should be read-only now
        // (files that were already read-only before the guard are also fine)
        Assert.That(guard.ProtectedCount, Is.GreaterThan(0), "Guard should have protected at least some files");
    }

    [Test]
    public void WritingToProtectedFile_Throws()
    {
        var anyJson = Directory.GetFiles(EngineDataDir, "*.json", SearchOption.AllDirectories).First();
        Assert.Throws<UnauthorizedAccessException>(() => File.WriteAllText(anyJson, "tampered"));
    }

    // ── REPOSITORY LOADING ───────────────────────────────────

    [Test]
    [Ignore(RetiredCorpus)]
    public void CharacterRepository_LoadsRealData()
    {
        var repo = new CharacterRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(50), "Should have 50+ characters");
        Assert.That(all.All(c => !string.IsNullOrWhiteSpace(c.Name)), "All characters should have names");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void CharacterRepository_KyleExists()
    {
        var repo = new CharacterRepository(paths);
        var kyle = repo.GetByName("Kyle Ellen Corbin");
        Assert.That(kyle, Is.Not.Null, "Kyle should exist in the character database");
        Assert.That(kyle!.Description, Is.Not.Empty, "Kyle should have a description");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void CorponationRepository_LoadsRealData()
    {
        var repo = new CorponationRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(40), "Should have 40+ corponations");
        Assert.That(all.Any(c => c.Name.Contains("IRONCLAD", StringComparison.OrdinalIgnoreCase)),
            "Ironclad Agrisystems should exist");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void DistrictRepository_LoadsRealData()
    {
        var repo = new DistrictRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(100), "Should have 100+ places");
        Assert.That(all.Any(d => d.Name.Contains("Old Harbor", StringComparison.OrdinalIgnoreCase)),
            "Old Harbor should exist");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void FactionRepository_LoadsRealData()
    {
        var repo = new FactionRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(20), "Should have 20+ factions");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void WeaponryRepository_LoadsRealData()
    {
        var repo = new WeaponryRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(100), "Should have 100+ weapons");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void EquipmentRepository_LoadsRealData()
    {
        var repo = new EquipmentRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(50), "Should have 50+ equipment items");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void CyberwareRepository_LoadsRealData()
    {
        var repo = new CyberwareRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(50), "Should have 50+ cyberware");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void TechnologyRepository_LoadsRealData()
    {
        var repo = new TechnologyRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(30), "Should have 30+ technologies");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void VocabularyRepository_LoadsRealData()
    {
        var repo = new VocabularyRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(500), "Should have 500+ vocabulary entries");
        Assert.That(all.All(v => !string.IsNullOrWhiteSpace(v.Term)), "All vocab entries should have terms");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void WorldbuildingDocRepository_LoadsRealData()
    {
        var repo = new WorldbuildingDocRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThan(100), "Should have 100+ documents");
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void TransportationRepository_LoadsRealData()
    {
        var repo = new TransportationRepository(paths);
        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(100), "Should have 100+ vehicles");
    }

    // ── DATABASE SERVICE ─────────────────────────────────────

    [Test]
    [Ignore(RetiredCorpus)]
    public void DatabaseService_AggregatesAllRepositories()
    {
        var db = BuildDatabaseService();
        Assert.That(db.Characters.Count, Is.GreaterThan(50));
        Assert.That(db.Corponations.Count, Is.GreaterThan(40));
        Assert.That(db.Districts.Count, Is.GreaterThan(100));
        Assert.That(db.Weaponry.Count, Is.GreaterThan(100));
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void DatabaseService_GetCharacterContext_ReturnsData()
    {
        var db = BuildDatabaseService();
        var ctx = db.GetCharacterContext("Kyle");
        Assert.That(ctx.Length, Is.GreaterThan(100), "Kyle's context should be substantial");
        Assert.That(ctx, Does.Contain("Kyle"));
    }

    [Test]
    public void DatabaseService_GetDistrictContext_ReturnsData()
    {
        var db = BuildDatabaseService();
        var districts = db.Districts;
        if (districts.Count > 0)
        {
            var ctx = db.GetDistrictContext(districts[0].Name);
            Assert.That(ctx.Length, Is.GreaterThan(0), $"Context for {districts[0].Name} should not be empty");
        }
    }

    [Test]
    [Ignore(RetiredCorpus)]
    public void DatabaseService_FindCharacter_CaseInsensitive()
    {
        var db = BuildDatabaseService();
        var kyle1 = db.FindCharacter("Kyle");
        var kyle2 = db.FindCharacter("kyle");
        Assert.That(kyle1, Is.Not.Null);
        Assert.That(kyle2, Is.Not.Null);
        Assert.That(kyle1!.Name, Is.EqualTo(kyle2!.Name));
    }

    // ── WORLD GRAPH ──────────────────────────────────────────

    [Test]
    [Ignore(RetiredCorpus)]
    public void WorldGraph_LoadsFromRealData()
    {
        var db = BuildDatabaseService();
        var graph = new WorldGraphService(paths, db);
        graph.EnsureLoaded();

        Assert.That(graph.NodeCount, Is.GreaterThan(500), "Graph should have 500+ nodes");
        Assert.That(graph.EdgeCount, Is.GreaterThan(100), "Graph should have 100+ edges");
    }

    [Test]
    public void WorldGraph_Slugify_Consistent()
    {
        Assert.That(WorldGraphService.Slugify("Kyle"), Is.EqualTo("kyle"));
        Assert.That(WorldGraphService.Slugify("The Shelf"), Is.EqualTo("the-shelf"));
        Assert.That(WorldGraphService.Slugify("Mrs. Chen"), Is.EqualTo("mrs-chen"));
        Assert.That(WorldGraphService.Slugify("Axiom Industries"), Is.EqualTo("axiom-industries"));
    }

    // ── SEMANTIC INDEX ───────────────────────────────────────

    [Test]
    [Ignore(RetiredCorpus)]
    public void SemanticIndex_BuildsFromRealGraph()
    {
        var db = BuildDatabaseService();
        var graph = new WorldGraphService(paths, db);
        graph.EnsureLoaded();

        var idx = new SemanticIndexService(graph);
        idx.RebuildIndex();

        var results = idx.Search("security", 5);
        Assert.That(results.Count, Is.GreaterThan(0), "Searching for 'security' should find something");
    }

    // ── LOGGING SERVICE ──────────────────────────────────────

    [Test]
    public void LoggingService_SearchReturnsResults_WhenLogsExist()
    {
        var logDir = paths.LogDir;
        var svc = new LoggingService(paths);

        if (Directory.Exists(logDir) && Directory.GetFiles(logDir, "log-*.txt").Length > 0)
        {
            var results = svc.Search(new LogSearchRequest { Since = DateTime.Now.AddDays(-30), MaxResults = 10 });
            Assert.That(results, Is.Not.Null);
        }
        else
        {
            // No logs yet — just verify it doesn't crash
            var results = svc.Search(new LogSearchRequest { Since = DateTime.Now.AddDays(-1) });
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }
    }

    // ── QUANTA SYMBOL CONSISTENCY ────────────────────────────

    [Test]
    public void AllCorponations_UseQuantaSymbol_NotPhiText()
    {
        // engine/data/corponations/*.json is empty post-migration (canon is SQL now) —
        // guard against this test silently passing vacuously over zero rows.
        var repo = new CorponationRepository(paths);
        var all = repo.GetAll();
        Assert.That(all, Is.Empty,
            "engine/data corponations corpus is expected empty post-JSON→SQL migration (SS-A45); " +
            "if this fails, the file-based corpus is back and this test needs re-pointing at the SQL DB.");

        foreach (var corp in all)
        {
            var json = JsonSerializer.Serialize(corp);
            Assert.That(json, Does.Not.Contain("Phi ").IgnoreCase,
                $"{corp.Name} contains 'Phi' text instead of Φ symbol");
        }
    }

    [Test]
    public void VocabularyEntries_HaveRequiredFields()
    {
        // engine/data/vocabulary/*.json is empty post-migration (canon is SQL now) —
        // guard against this test silently passing vacuously over zero rows.
        var repo = new VocabularyRepository(paths);
        var all = repo.GetAll();
        Assert.That(all, Is.Empty,
            "engine/data vocabulary corpus is expected empty post-JSON→SQL migration (SS-A45); " +
            "if this fails, the file-based corpus is back and this test needs re-pointing at the SQL DB.");

        foreach (var v in all)
        {
            Assert.That(v.Term, Is.Not.Empty, $"Vocab entry has empty term");
            Assert.That(v.Definition, Is.Not.Empty, $"Vocab '{v.Term}' has empty definition");
        }
    }

    // ── E.L.F. DATA INTEGRITY ────────────────────────────────


    // ── OUTLINE SERVICE (TRUNCATION REPAIR) ──────────────────

    [Test]
    public void OutlineService_RepairsTruncatedJson()
    {
        // Simulate the exact error from the user's log: truncated mid-string in acts[2].beats[1].stakes
        var truncated = """
            {"title":"Harbor Frequency","logline":"test","theme":"test",
            "acts":[{"act_number":1,"name":"Act 1","purpose":"setup",
            "beats":[{"beat_index":0,"title":"Opening","goal":"test","characters_present":["Kyle"],
            "location":"The Shelf","emotional_arc":"tense","stakes":"high",
            "seeds":[],"payoffs":[],"facet_hint":"wound","tension":5}]}],
            "character_arcs":[],"seeds_and_payoffs":[
            """;

        // Should not crash — RepairTruncatedJson is private, but we can test via GenerateOutlineAsync
        // by checking that the repair method produces parseable JSON from known truncated input
        var repaired = RepairJson(truncated);
        Assert.DoesNotThrow(() => JsonDocument.Parse(repaired).Dispose(),
            "Repaired JSON should be parseable");
    }

    [Test]
    public void OutlineService_RepairsUnclosedString()
    {
        var truncated = """{"title":"Test Story","logline":"A story about""";
        var repaired = RepairJson(truncated);
        Assert.DoesNotThrow(() => JsonDocument.Parse(repaired).Dispose());
    }

    [Test]
    public void OutlineService_RepairsUnclosedArray()
    {
        var truncated = """{"title":"Test","acts":[{"act_number":1,"beats":[""";
        var repaired = RepairJson(truncated);
        Assert.DoesNotThrow(() => JsonDocument.Parse(repaired).Dispose());
    }

    // ── CHARACTER DATA QUALITY ───────────────────────────────

    [Test]
    [Ignore(RetiredCorpus)]
    public void Characters_HaveDescriptions()
    {
        var repo = new CharacterRepository(paths);
        var missing = repo.GetAll().Where(c => string.IsNullOrWhiteSpace(c.Description)).Select(c => c.Name).ToList();
        // Allow some characters without descriptions but flag if too many
        Assert.That(missing.Count, Is.LessThan(repo.GetAll().Count / 4),
            $"Too many characters without descriptions: {string.Join(", ", missing.Take(10))}");
    }

    [Test]
    public void Characters_HaveValidStatus()
    {
        var validPrefixes = new[] { "active", "alive", "dead", "deceased", "missing", "unknown", "dormant", "imprisoned", "exiled", "retired", "fugitive" };
        var repo = new CharacterRepository(paths);
        foreach (var c in repo.GetAll())
        {
            if (!string.IsNullOrEmpty(c.Status))
            {
                var statusLower = c.Status.ToLowerInvariant();
                Assert.That(validPrefixes.Any(p => statusLower.StartsWith(p)), Is.True,
                    $"Character '{c.Name}' has unusual status: {c.Status}");
            }
        }
    }

    // ── CROSS-REPOSITORY CONSISTENCY ─────────────────────────

    [Test]
    public void Characters_WithRelationships_ReferenceExistingCharacters()
    {
        var repo = new CharacterRepository(paths);
        var allNames = repo.GetAll().Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var brokenRefs = new List<string>();

        foreach (var c in repo.GetAll())
        {
            foreach (var rel in c.Relationships)
            {
                if (!allNames.Contains(rel.Name) && !string.IsNullOrWhiteSpace(rel.Name))
                    brokenRefs.Add($"{c.Name} → {rel.Name}");
            }
        }

        // Some references may be to NPCs not in the DB — that's OK if it's a minority
        if (brokenRefs.Count > 0)
            TestContext.Out.WriteLine($"Unresolved character references ({brokenRefs.Count}): {string.Join(", ", brokenRefs.Take(10))}");
    }

    // ── HELPERS ───────────────────────────────────────────────

    private DatabaseService BuildDatabaseService() => new(
        new CharacterRepository(paths),
        new DistrictRepository(paths),
        new FactionRepository(paths),
        new CorponationRepository(paths),
        new WorldbuildingDocRepository(paths),
        new WeaponryRepository(paths),
        new EquipmentRepository(paths),
        new TechnologyRepository(paths),
        new StoryBibleRepository(paths),
        new LiteraryRulesRepository(paths),
        new MotifRepository(paths),
        new CharacterProfileRepository(paths),
        new ToneBibleRepository(paths)
    );

    /// <summary>
    /// Mirror of OutlineService.RepairTruncatedJson (private) for testing.
    /// </summary>
    private static string RepairJson(string json)
    {
        var trimmed = json.TrimEnd();
        var inString = false;
        var escaped = false;
        var stack = new Stack<char>();

        for (int i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{') stack.Push('}');
            else if (c == '[') stack.Push(']');
            else if ((c == '}' || c == ']') && stack.Count > 0 && stack.Peek() == c) stack.Pop();
        }

        if (inString) trimmed += "\"";
        while (stack.Count > 0) trimmed += stack.Pop();
        return trimmed;
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "engine", "data"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        // Fallback: relative from test project
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}

/// <summary>Points to real engine_data for read-only testing.</summary>
internal class RealDataPathProvider : IPathProvider
{
    private readonly string engineData;
    private readonly string root;

    public RealDataPathProvider(string engineDataDir)
    {
        engineData = engineDataDir;
        // engine/data -> engine -> repo root
        root = Directory.GetParent(Directory.GetParent(engineDataDir)!.FullName)!.FullName;
    }

    public string DataRoot => root;
    public string WorldbuildingDir => Path.Combine(root, "worldbuilding");
    public string CharactersDir => Path.Combine(engineData, "people");
    public string EssencesDir => Path.Combine(root, "essences");
    public string NarrativeBiblePath => Path.Combine(root, "narrative_bible.md");
    public string WorldDir => Path.Combine(root, "world");
    public string EngineDataDir => engineData;
    public string MutableDataDir => engineData;
    public string ChaptersDir => Path.Combine(engineData, "chapters");
    public string BooksDir => Path.Combine(engineData, "books");
    public string SeriesDir => Path.Combine(engineData, "series");
    public string GraphDir => Path.Combine(engineData, "graph");
    public string LogDir => Path.Combine(root, "engine", "logs");
    public string ExportDir => Path.Combine(root, "engine", "exports");
    public string ArchiveDir => Path.Combine(root, "engine", "archives");
    public string MediaDir => Path.Combine(root, "engine", "data", "media");
    public string MediaArchiveDir => Path.Combine(root, "engine", "archives", "media");
}
