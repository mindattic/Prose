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
    public string CharactersDir => Path.Combine(root, "characters");
    public string EssencesDir => Path.Combine(root, "essences");
    public string StoriesDir => Path.Combine(root, "stories");
    public string EngineDataDir => Path.Combine(root, "engine_data");
    public string NarrativeBiblePath => Path.Combine(root, "narrative_bible.md");
    public string WorldDir => Path.Combine(root, "world");
    public string FacetsDir => Path.Combine(root, "character", "facets");
    public string GraphDir => Path.Combine(root, "engine_data", "graph");
}
