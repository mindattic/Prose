using System.Text.Json;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix: the comprehension-probe cache blob persists
/// probeHash, probeModel, AND arbiterModel, but the cache-hit check only ever compared
/// hash+probeModel — so upgrading settings.ComprehensionArbiterModel (e.g. for better judgment
/// quality) silently kept serving defects judged under the OLD arbiter forever, until the
/// chapter's own text happened to change. TryParseCache is the read half of that bug: it must
/// actually surface arbiterModel so the caller (ProbeChapterAsync) can compare it.
/// </summary>
[TestFixture]
public class ComprehensionProbeServiceCacheTests
{
    static string CacheJson(string hash, string probeModel, string arbiterModel, string? defectsJson = null) =>
        JsonSerializer.Serialize(new
        {
            probeHash = hash,
            probeModel,
            arbiterModel,
            probe = new { summary = "", facts = "", confusions = Array.Empty<string>(), prediction = "" },
            defects = defectsJson is null ? Array.Empty<object>() : JsonSerializer.Deserialize<object[]>(defectsJson),
            evaluatedAt = "2026-08-09T00:00:00Z",
        });

    [Test]
    public void TryParseCache_RoundTripsArbiterModel()
    {
        var json = CacheJson("abc123", "haiku-4.5", "sonnet-5");

        var result = ComprehensionProbeService.TryParseCache(json);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.hash, Is.EqualTo("abc123"));
        Assert.That(result.Value.probeModel, Is.EqualTo("haiku-4.5"));
        Assert.That(result.Value.arbiterModel, Is.EqualTo("sonnet-5"),
            "arbiterModel must be read back from the cache blob, not silently dropped");
    }

    [Test]
    public void TryParseCache_MissingArbiterModel_ReturnsEmptyStringNotThrow()
    {
        // Defensive: a cache row written before this field existed (or any malformed blob)
        // must degrade to a safe empty string, not throw or return a null tuple outright.
        var json = JsonSerializer.Serialize(new
        {
            probeHash = "abc123",
            probeModel = "haiku-4.5",
            probe = new { summary = "", facts = "", confusions = Array.Empty<string>(), prediction = "" },
            defects = Array.Empty<object>(),
        });

        var result = ComprehensionProbeService.TryParseCache(json);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.arbiterModel, Is.EqualTo(""));
    }

    [Test]
    public void TryParseCache_InvalidJson_ReturnsNull()
    {
        Assert.That(ComprehensionProbeService.TryParseCache("{not valid json"), Is.Null);
    }
}
