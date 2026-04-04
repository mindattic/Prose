using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.UnitTests;

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
    private readonly string _root;

    public TestPathProviderWithRoot(string root) => _root = root;

    public string DataRoot => _root;
    public string WorldbuildingDir => Path.Combine(_root, "worldbuilding");
    public string CharactersDir => Path.Combine(_root, "characters");
    public string EssencesDir => Path.Combine(_root, "essences");
    public string StoriesDir => Path.Combine(_root, "stories");
    public string EngineDataDir => Path.Combine(_root, "engine_data");
    public string NarrativeBiblePath => Path.Combine(_root, "narrative_bible.md");
    public string WorldDir => Path.Combine(_root, "world");
    public string FacetsDir => Path.Combine(_root, "character", "facets");
    public string GraphDir => Path.Combine(_root, "engine_data", "graph");
}
