using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression coverage for the fallback chain's model-pinning bug (found live 2026-08-24 by
/// <c>prose --reader-qa --slug bushido_coda</c>, fixed same day).
///
/// A caller that pins an explicit model — <c>ComprehensionProbeService</c> asking for
/// <c>claude-sonnet-5</c> — had that id forwarded verbatim to EVERY hop of the chain. So a
/// transient Anthropic outage did not degrade to another provider: it walked all ten, collecting
/// <c>model_not_found</c> from OpenAI, <c>NOT_FOUND</c> from Gemini, "The supported API model
/// names are deepseek-…" from DeepSeek and "Invalid model: claude-sonnet-5" from Mistral, then
/// reported all ten providers down. Eight of those eight-of-ten failures were self-inflicted —
/// each provider was being asked for a model that was never theirs.
///
/// The rule these tests pin down: a pinned model reaches only providers in its own family;
/// everyone else is asked with <c>null</c> and applies their own settings-driven default.
/// </summary>
[TestFixture]
public class LlmRouterModelFamilyTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-llmmodelfamily-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "llm-model-family");
    }

    [TearDown]
    public void TearDown()
    {
        LlmActionContext.Current = null;
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    /// <summary>Records the model id it was actually handed, so the assertion can be about the hop.</summary>
    private sealed class ModelRecordingLlm : ILlmService
    {
        private readonly string? response;
        private readonly string? throwMessage;
        public bool Called { get; private set; }
        public string? SeenModel { get; private set; }

        public ModelRecordingLlm(string? response = null, string? throwMessage = null)
        { this.response = response; this.throwMessage = throwMessage; }

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8,
            int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Called = true;
            SeenModel = model;
            if (throwMessage != null) throw new InvalidOperationException(throwMessage);
            return Task.FromResult(response ?? "");
        }
    }

    private LlmRouter Router(Dictionary<string, ILlmService> providers, string primary, params string[] chain)
        => new(providers, () => primary, () => chain, new LastPromptStore(), dbFactory,
               NullLogger<LlmRouter>.Instance);

    [Test]
    public async Task PinnedClaudeModel_IsNotForwardedToNonAnthropicFallback()
    {
        var openAi = new ModelRecordingLlm("OPENAI-OK");
        var providers = new Dictionary<string, ILlmService>
        {
            ["claude-api"] = new ModelRecordingLlm(throwMessage: "503 overloaded_error"),
            ["openai"] = openAi,
        };

        var response = await Router(providers, "claude-api", "openai")
            .GenerateAsync("sys", "usr", model: "claude-sonnet-5");

        Assert.That(response, Is.EqualTo("OPENAI-OK"),
            "an Anthropic outage must degrade to the next provider, not fail the whole chain");
        Assert.That(openAi.Called, Is.True);
        Assert.That(openAi.SeenModel, Is.Null,
            "the OpenAI hop must be asked with its OWN default — forwarding 'claude-sonnet-5' is a guaranteed model_not_found");
    }

    [Test]
    public async Task PinnedClaudeModel_IsPreservedForTheOtherAnthropicProvider()
    {
        var claudeTeam = new ModelRecordingLlm("TEAM-OK");
        var providers = new Dictionary<string, ILlmService>
        {
            ["claude-api"] = new ModelRecordingLlm(throwMessage: "boom"),
            ["claude-team"] = claudeTeam,
        };

        await Router(providers, "claude-api", "claude-team").GenerateAsync("sys", "usr", model: "claude-sonnet-5");

        Assert.That(claudeTeam.SeenModel, Is.EqualTo("claude-sonnet-5"),
            "claude-team serves the same family — dropping the pin here would silently downgrade the model");
    }

    [Test]
    public async Task PinnedModel_IsDroppedForEveryForeignFamily_AndKeptForItsOwn()
    {
        var cases = new (string Provider, string Model, string? Expected)[]
        {
            ("gemini",     "gemini-3-pro",       "gemini-3-pro"),
            ("gemini-cli", "gemini-3-pro",       "gemini-3-pro"),
            ("gemini",     "claude-sonnet-5",    null),
            ("deepseek",   "claude-sonnet-5",    null),
            ("mistral",    "claude-sonnet-5",    null),
            ("kimi",       "claude-sonnet-5",    null),
            ("perplexity", "claude-sonnet-5",    null),
            ("codex-cli",  "claude-sonnet-5",    null),
            ("codex-cli",  "gpt-5-codex",        "gpt-5-codex"),
            ("openai",     "o3-mini",            "o3-mini"),
            ("deepseek",   "deepseek-v4-pro",    "deepseek-v4-pro"),
            ("mistral",    "mistral-large",      "mistral-large"),
            ("kimi",       "moonshot-v2",        "moonshot-v2"),
            ("perplexity", "sonar-pro",          "sonar-pro"),
            ("claude-api", "gpt-5",              null),
        };

        foreach (var (provider, model, expected) in cases)
        {
            var stub = new ModelRecordingLlm("OK");
            var providers = new Dictionary<string, ILlmService> { [provider] = stub };
            await Router(providers, provider).GenerateAsync("sys", "usr", model: model);

            Assert.That(stub.SeenModel, Is.EqualTo(expected),
                $"provider '{provider}' asked for '{model}'");
        }
    }

    [Test]
    public async Task UnrecognizedModelId_IsPassedThroughUntouched()
    {
        // A fine-tune or a local build we can't classify: the caller knows better than the router,
        // so don't presume to null it out.
        var stub = new ModelRecordingLlm("OK");
        var providers = new Dictionary<string, ILlmService> { ["openai"] = stub };

        await Router(providers, "openai").GenerateAsync("sys", "usr", model: "ft:acme-house-voice-v3");

        Assert.That(stub.SeenModel, Is.EqualTo("ft:acme-house-voice-v3"));
    }

    [Test]
    public async Task LocalProvider_AcceptsAnyModelName()
    {
        var stub = new ModelRecordingLlm("OK");
        var providers = new Dictionary<string, ILlmService> { ["local"] = stub };

        await Router(providers, "local").GenerateAsync("sys", "usr", model: "claude-sonnet-5");

        Assert.That(stub.SeenModel, Is.EqualTo("claude-sonnet-5"),
            "a local runtime may legitimately serve a model under any name — never rewrite its pin");
    }

    [Test]
    public async Task CallHistory_RecordsTheModelEachHopWasActuallyAskedFor()
    {
        var providers = new Dictionary<string, ILlmService>
        {
            ["claude-api"] = new ModelRecordingLlm(throwMessage: "503 overloaded_error"),
            ["openai"] = new ModelRecordingLlm("OPENAI-OK"),
        };

        await Router(providers, "claude-api", "openai").GenerateAsync("sys", "usr", model: "claude-sonnet-5");

        await using var db = dbFactory.CreateDbContext();
        var rows = db.LlmCallHistories.OrderBy(r => r.Id).ToList();
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Model, Is.EqualTo("claude-sonnet-5"), "the Anthropic hop was asked for the pin");
        Assert.That(rows[1].Model, Is.EqualTo("(provider default)"),
            "the audit trail must not claim the OpenAI hop was asked for a Claude model it never saw");
    }
}
