using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Provides NullLogger instances for all service types — no output, no cost.
/// </summary>
public static class NullLoggers
{
    public static ILogger<T> For<T>() => NullLogger<T>.Instance;
}

/// <summary>
/// Fake LLM service for unit tests. Returns empty strings — never calls an API.
/// Tests that need LLM responses should mock specific return values.
/// </summary>
public class FakeLlmService : ILlmService
{
    public Task<bool> IsConfiguredAsync() => Task.FromResult(false);

    public Task<string> GenerateAsync(
        string system, string user, double temperature = 0.8,
        int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        => Task.FromResult("[]"); // Return empty JSON array — safe default for extraction services
}

/// <summary>
/// Test path provider that uses a custom root directory (for temp test dirs).
/// </summary>
public class TestPathProviderWithRoot : IPathProvider
{
    private readonly string root;

    public TestPathProviderWithRoot(string root) => this.root = root;

    public string DataRoot => root;
    public string WorldbuildingDir => Path.Combine(root, "worldbuilding");
    public string CharactersDir => Path.Combine(root, "people");
    public string EssencesDir => Path.Combine(root, "essences");
    public string NarrativeBiblePath => Path.Combine(root, "narrative_bible.md");
    public string WorldDir => Path.Combine(root, "world");
    public string FacetsDir => Path.Combine(root, "character", "facets");
    public string EngineDataDir => Path.Combine(root, "engine_data");
    public string MutableDataDir => Path.Combine(root, "engine_data");
    public string StoriesDir => Path.Combine(root, "stories");
    public string GraphDir => Path.Combine(root, "engine_data", "graph");
    public string LogDir => Path.Combine(root, "logs");
    public string ExportDir => Path.Combine(root, "exports");
    public string ArchiveDir => Path.Combine(root, "archives");
    public string MediaDir => Path.Combine(root, "engine_data", "media");
    public string MediaArchiveDir => Path.Combine(root, "archives", "media");
}

public static class TestDatabaseFactory
{
    public static (StreetSamurai.Core.Services.DatabaseService db, TestPathProviderWithRoot paths, string rootDir) Create()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        var engDir = Path.Combine(rootDir, "engine_data");
        foreach (var dir in new[] { "people", "corponations", "factions", "places",
            "technology", "weaponry", "equipment", "cyberware", "ammunition",
            "synthetics", "genemods", "transportation", "quotes", "contracts",
            "news", "archetypes", "materials", "pharmaceuticals", "consumer_goods",
            "automata", "apparel", "subsidiaries", "entertainment", "vocabulary",
            "documents", "facets", "motifs", "tone_bible", "story_bible",
            "character_profile", "literary_rules", "graph" })
        {
            Directory.CreateDirectory(Path.Combine(engDir, dir));
        }
        Directory.CreateDirectory(Path.Combine(rootDir, "worldbuilding"));
        Directory.CreateDirectory(Path.Combine(rootDir, "stories"));

        var paths = new TestPathProviderWithRoot(rootDir);
        var db = new StreetSamurai.Core.Services.DatabaseService(
            new StreetSamurai.Core.Services.CharacterRepository(paths),
            new StreetSamurai.Core.Services.FacetRepository(paths),
            new StreetSamurai.Core.Services.DistrictRepository(paths),
            new StreetSamurai.Core.Services.FactionRepository(paths),
            new StreetSamurai.Core.Services.CorponationRepository(paths),
            new StreetSamurai.Core.Services.WorldbuildingDocRepository(paths),
            new StreetSamurai.Core.Services.WeaponryRepository(paths),
            new StreetSamurai.Core.Services.EquipmentRepository(paths),
            new StreetSamurai.Core.Services.TechnologyRepository(paths),
            new StreetSamurai.Core.Services.StoryBibleRepository(paths),
            new StreetSamurai.Core.Services.LiteraryRulesRepository(paths),
            new StreetSamurai.Core.Services.MotifRepository(paths),
            new StreetSamurai.Core.Services.CharacterProfileRepository(paths),
            new StreetSamurai.Core.Services.ToneBibleRepository(paths)
        );

        return (db, paths, rootDir);
    }
}
