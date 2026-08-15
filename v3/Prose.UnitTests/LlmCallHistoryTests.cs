using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Integration tests for LlmRouter's LlmCallHistory writes against a real (SQLite in-memory
/// via TestDbFactory) ProseDbContext — the durable per-call audit trail from the Multi-LLM
/// Master Switch-Over plan ("log what model performed which action").
/// </summary>
[TestFixture]
public class LlmCallHistoryTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-llmcallhistory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "llm-call-history");
    }

    [TearDown]
    public void TearDown()
    {
        LlmActionContext.Current = null;
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private static Dictionary<string, ILlmService> Providers(string primaryId, string? primaryError, string fallbackId, string fallbackResponse) =>
        new()
        {
            [primaryId] = primaryError is null
                ? new StubLlm(fallbackResponse)
                : new StubLlm(throwMessage: primaryError),
            [fallbackId] = new StubLlm(fallbackResponse),
        };

    [Test]
    public async Task GenerateAsync_WritesSuccessRow_TaggedWithCurrentAction()
    {
        LlmActionContext.Current = "--write-story";
        var providers = new Dictionary<string, ILlmService> { ["claude-api"] = new StubLlm("STUB-OK") };
        var router = new LlmRouter(providers, () => "claude-api", () => [], new LastPromptStore(), dbFactory, NullLogger<LlmRouter>.Instance);

        await router.GenerateAsync("sys", "usr", model: "claude-sonnet-5");

        await using var db = dbFactory.CreateDbContext();
        var rows = db.LlmCallHistories.ToList();
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].ProviderId, Is.EqualTo("claude-api"));
        Assert.That(rows[0].Action, Is.EqualTo("--write-story"));
        Assert.That(rows[0].Success, Is.True);
        Assert.That(rows[0].FallbackHopIndex, Is.EqualTo(0));
        Assert.That(rows[0].Cost, Is.GreaterThan(0));
    }

    [Test]
    public async Task GenerateAsync_WritesOneFailureRowAndOneSuccessRow_OnFallback()
    {
        var providers = Providers("claude-api", "boom", "openai", "OPENAI-OK");
        var router = new LlmRouter(providers, () => "claude-api", () => ["openai"], new LastPromptStore(), dbFactory, NullLogger<LlmRouter>.Instance);

        var response = await router.GenerateAsync("sys", "usr");

        Assert.That(response, Is.EqualTo("OPENAI-OK"));
        await using var db = dbFactory.CreateDbContext();
        var rows = db.LlmCallHistories.OrderBy(r => r.Id).ToList();
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].ProviderId, Is.EqualTo("claude-api"));
        Assert.That(rows[0].Success, Is.False);
        Assert.That(rows[0].FallbackHopIndex, Is.EqualTo(0));
        Assert.That(rows[0].ErrorMessage, Does.Contain("boom"));
        Assert.That(rows[1].ProviderId, Is.EqualTo("openai"));
        Assert.That(rows[1].Success, Is.True);
        Assert.That(rows[1].FallbackHopIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task GenerateAsync_ZeroCostsSubscriptionCliProviders()
    {
        var providers = new Dictionary<string, ILlmService> { ["codex-cli"] = new StubLlm("CODEX-OK") };
        var router = new LlmRouter(providers, () => "codex-cli", () => [], new LastPromptStore(), dbFactory, NullLogger<LlmRouter>.Instance);

        await router.GenerateAsync("sys", "usr");

        await using var db = dbFactory.CreateDbContext();
        var row = db.LlmCallHistories.Single();
        Assert.That(row.ProviderId, Is.EqualTo("codex-cli"));
        Assert.That(row.Cost, Is.EqualTo(0));
        Assert.That(row.InputTokens, Is.GreaterThan(0)); // tokens still tracked, just not billed
    }

    [Test]
    public async Task GenerateAsync_DefaultsActionToUnspecified_WhenNoAmbientContextSet()
    {
        var providers = new Dictionary<string, ILlmService> { ["claude-api"] = new StubLlm("OK") };
        var router = new LlmRouter(providers, () => "claude-api", () => [], new LastPromptStore(), dbFactory, NullLogger<LlmRouter>.Instance);

        await router.GenerateAsync("sys", "usr");

        await using var db = dbFactory.CreateDbContext();
        Assert.That(db.LlmCallHistories.Single().Action, Is.EqualTo("(unspecified)"));
    }
}
