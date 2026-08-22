using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Interfaces;

namespace Prose.UnitTests;

/// <summary>
/// Provides NullLogger instances for all service types — no output, no cost.
/// </summary>
public static class NullLoggers
{
    public static ILogger<T> For<T>() => NullLogger<T>.Instance;
}

/// <summary>
/// Shared classifier for "this exception is just a real SQL Server / LocalDB connection being
/// unavailable in this environment" — extracted 2026-08-09 after CI (which never ran any test at
/// all before this session) surfaced that this exact check was needed in more than one test
/// class. Any DI-resolution test that transitively touches a service which connects to the DB at
/// construction time (UniverseGraphService, StoryDirectorService, ...) needs this guard, or it's
/// only really testing "is LocalDB installed on the machine running this," not the DI wiring
/// itself — true on a dev box with LocalDB, false on a bare CI runner with no SQL Server at all.
/// </summary>
public static class SqlAvailability
{
    public static bool IsUnavailable(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is Microsoft.Data.SqlClient.SqlException) return true;
            if (e.Message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase)) return true;
            if (e.Message.Contains("Login failed", StringComparison.OrdinalIgnoreCase)) return true;
            if (e.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
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
/// RFC 0011 Brick 3: the shared "provider is down" fake — simulates the standing Anthropic
/// credit-exhaustion outage this project has actually lived through. Every LLM-dependent service
/// must be tested against this: a real outage must surface as a visible failure (an exception,
/// an explicit "not evaluated" result) and never get swallowed into a false Pass or false Clean.
/// Extracted 2026-08-10 after finding it independently duplicated byte-for-byte in
/// <c>BeatAuditServiceTests.cs</c> and <c>BookAuditChapterAssemblyTests.cs</c> — the same
/// "nobody remembered this already exists elsewhere" duplication pattern RFC 0011 diagnosed in
/// production code (Brick 1), showing up in test code too.
/// </summary>
public class ThrowingLlmService : ILlmService
{
    public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

    public Task<string> GenerateAsync(string system, string user,
        double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
        throw new InvalidOperationException(
            "400 Bad Request: Your credit balance is too low to access the Anthropic API.");
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
    public string EngineDataDir => Path.Combine(root, "engine_data");
    public string MutableDataDir => Path.Combine(root, "engine_data");
    public string ChaptersDir => Path.Combine(root, "chapters");
    public string BooksDir => Path.Combine(root, "books");
    public string SeriesDir => Path.Combine(root, "series");
    public string GraphDir => Path.Combine(root, "engine_data", "graph");
    public string LogDir => Path.Combine(root, "logs");
    public string ExportDir => Path.Combine(root, "exports");
    public string ArchiveDir => Path.Combine(root, "archives");
    public string MediaDir => Path.Combine(root, "engine_data", "media");
    public string MediaArchiveDir => Path.Combine(root, "archives", "media");
}

public static class TestDatabaseFactory
{
    public static (Prose.Core.Services.DatabaseService db, TestPathProviderWithRoot paths, string rootDir) Create()
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
        Directory.CreateDirectory(Path.Combine(rootDir, "chapters"));

        var paths = new TestPathProviderWithRoot(rootDir);
        var db = new Prose.Core.Services.DatabaseService(
            new Prose.Core.Services.CharacterRepository(paths),
            new Prose.Core.Services.DistrictRepository(paths),
            new Prose.Core.Services.FactionRepository(paths),
            new Prose.Core.Services.CorponationRepository(paths),
            new Prose.Core.Services.WorldbuildingDocRepository(paths),
            new Prose.Core.Services.WeaponryRepository(paths),
            new Prose.Core.Services.EquipmentRepository(paths),
            new Prose.Core.Services.TechnologyRepository(paths),
            new Prose.Core.Services.StoryBibleRepository(paths),
            new Prose.Core.Services.LiteraryRulesRepository(paths),
            new Prose.Core.Services.MotifRepository(paths),
            new Prose.Core.Services.CharacterProfileRepository(paths),
            new Prose.Core.Services.ToneBibleRepository(paths)
        );

        return (db, paths, rootDir);
    }
}
