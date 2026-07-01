namespace StreetSamurai.UnitTests;

using Microsoft.Extensions.Configuration;
using MindAttic.Legion;
using StreetSamurai.Core.Services;

/// <summary>
/// Coverage for the cloud-native credential resolution chain introduced by the
/// MindAttic.Vault adoption (commit 18b999389): SettingsService.VaultConfiguration
/// MUST take precedence over env vars, the shared MindAtticCredentialStore, and the
/// legacy Settings.json field — and MUST fall through cleanly when unset, blank, or
/// whitespace so the existing test fleet keeps passing.
/// </summary>
[TestFixture]
public class SettingsServiceVaultTests
{
    private SettingsService svc = null!;
    private string tempDir = null!;
    private string? prevCredsEnv;
    private IConfiguration? prevVaultConfig;

    private static IConfiguration BuildConfig(params (string Key, string Value)[] pairs)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (k, v) in pairs) dict[k] = v;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [SetUp]
    public void Setup()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "ss_vault_test_" + Guid.NewGuid().ToString("N")[..8]);
        prevCredsEnv = Environment.GetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS");
        Environment.SetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS", Path.Combine(tempDir, "creds"));
        prevVaultConfig = SettingsService.VaultConfiguration;
        SettingsService.VaultConfiguration = null;
        svc = new SettingsService(tempDir);
    }

    [TearDown]
    public void Teardown()
    {
        svc.Dispose();
        SettingsService.VaultConfiguration = prevVaultConfig;
        Environment.SetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS", prevCredsEnv);
        ClearProviderEnvVars();
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    private static void ClearProviderEnvVars()
    {
        foreach (var v in new[]
        {
            "SS_CLAUDE_API_KEY", "SS_OPENAI_API_KEY", "SS_GEMINI_API_KEY",
            "SS_ELEVENLABS_API_KEY", "SS_MAP_API_KEY", "SS_GOOGLE_MAPS_API_KEY",
            "SS_DEEPSEEK_API_KEY", "SS_MISTRAL_API_KEY", "SS_GROK_API_KEY",
            "SS_GROQ_API_KEY", "SS_TOGETHER_API_KEY", "SS_OPENROUTER_API_KEY",
            "SS_FIREWORKS_API_KEY", "SS_COHERE_API_KEY",
        })
        {
            Environment.SetEnvironmentVariable(v, null);
        }
    }

    // ── Highest-priority source ───────────────────────────────────────────────

    [Test]
    public void Vault_BeatsEnvVar_ForClaude()
    {
        Environment.SetEnvironmentVariable("SS_CLAUDE_API_KEY", "env-claude");
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:claude:apiKey", "vault-claude"));

        Assert.That(svc.ApiKey, Is.EqualTo("vault-claude"));
    }

    [Test]
    public void Vault_BeatsCredentialStore_ForOpenAi()
    {
        MindAtticCredentialStore.SetKey("openai", "store-openai");
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:openai:apiKey", "vault-openai"));

        Assert.That(svc.OpenAiApiKey, Is.EqualTo("vault-openai"));
    }

    [Test]
    public void Vault_BeatsLegacySettings_ForGemini()
    {
        // Setter writes through to the shared store; clear it so legacy is the
        // only non-Vault source available for the read.
        svc.GeminiApiKey = "legacy-gemini";
        MindAtticCredentialStore.SetKey("gemini", "");
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:gemini:apiKey", "vault-gemini"));

        Assert.That(svc.GeminiApiKey, Is.EqualTo("vault-gemini"));
    }

    // ── Fallthrough behaviour ─────────────────────────────────────────────────

    [Test]
    public void Vault_NullConfiguration_FallsBackToEnvVar()
    {
        SettingsService.VaultConfiguration = null;
        Environment.SetEnvironmentVariable("SS_CLAUDE_API_KEY", "env-claude");

        Assert.That(svc.ApiKey, Is.EqualTo("env-claude"));
    }

    [Test]
    public void Vault_KeyMissing_FallsBackToEnvVar()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:openai:apiKey", "vault-openai"));
        Environment.SetEnvironmentVariable("SS_CLAUDE_API_KEY", "env-claude");

        Assert.That(svc.ApiKey, Is.EqualTo("env-claude"));
    }

    [Test]
    public void Vault_EmptyValue_FallsBackToEnvVar()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:claude:apiKey", ""));
        Environment.SetEnvironmentVariable("SS_CLAUDE_API_KEY", "env-claude");

        Assert.That(svc.ApiKey, Is.EqualTo("env-claude"));
    }

    [Test]
    public void Vault_WhitespaceValue_FallsBackToEnvVar()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:claude:apiKey", "   "));
        Environment.SetEnvironmentVariable("SS_CLAUDE_API_KEY", "env-claude");

        Assert.That(svc.ApiKey, Is.EqualTo("env-claude"));
    }

    [Test]
    public void Vault_NoVaultNoEnv_FallsBackToCredentialStore()
    {
        MindAtticCredentialStore.SetKey("openai", "store-openai");

        Assert.That(svc.OpenAiApiKey, Is.EqualTo("store-openai"));
    }

    // ── Trim & non-credential providers ───────────────────────────────────────

    [Test]
    public void Vault_TrimsWhitespaceAroundValue()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:claude:apiKey", "  vault-claude  "));

        Assert.That(svc.ApiKey, Is.EqualTo("vault-claude"));
    }

    [Test]
    public void Vault_ResolvesElevenLabsKey()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:elevenlabs:apiKey", "vault-eleven"));

        Assert.That(svc.ElevenLabsApiKey, Is.EqualTo("vault-eleven"));
    }

    [Test]
    public void Vault_ResolvesHereMapsKey()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:here-maps:apiKey", "vault-here"));

        Assert.That(svc.MapApiKey, Is.EqualTo("vault-here"));
    }

    [Test]
    public void Vault_ResolvesGoogleMapsKey()
    {
        SettingsService.VaultConfiguration = BuildConfig(
            ("MindAttic:Vault:LLM:google-maps:apiKey", "vault-google"));

        Assert.That(svc.GoogleMapsApiKey, Is.EqualTo("vault-google"));
    }

    // ── Provider matrix — every property that goes through ResolveApiKey ──────
    // Locks the provider-id contract. If any provider's key path drifts away
    // from "MindAttic:Vault:LLM:<id>:apiKey" this matrix fails fast.

    [TestCase("claude-api", nameof(SettingsService.ApiKey))]
    [TestCase("openai",     nameof(SettingsService.OpenAiApiKey))]
    [TestCase("gemini",     nameof(SettingsService.GeminiApiKey))]
    [TestCase("deepseek",   nameof(SettingsService.DeepSeekApiKey))]
    [TestCase("mistral",    nameof(SettingsService.MistralApiKey))]
    [TestCase("xai",        nameof(SettingsService.GrokApiKey))]
    [TestCase("groq",       nameof(SettingsService.GroqApiKey))]
    [TestCase("together",   nameof(SettingsService.TogetherApiKey))]
    [TestCase("openrouter", nameof(SettingsService.OpenRouterApiKey))]
    [TestCase("fireworks",  nameof(SettingsService.FireworksApiKey))]
    [TestCase("cohere",     nameof(SettingsService.CohereApiKey))]
    [TestCase("elevenlabs", nameof(SettingsService.ElevenLabsApiKey))]
    [TestCase("here-maps",  nameof(SettingsService.MapApiKey))]
    [TestCase("google-maps", nameof(SettingsService.GoogleMapsApiKey))]
    public void Vault_ProviderMatrix_RoutesEachKey(string providerId, string propertyName)
    {
        var expected = $"vault-{providerId}";
        SettingsService.VaultConfiguration = BuildConfig(
            ($"MindAttic:Vault:LLM:{providerId}:apiKey", expected));

        var prop = typeof(SettingsService).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property {propertyName} not found");
        var actual = (string?)prop.GetValue(svc);

        Assert.That(actual, Is.EqualTo(expected),
            $"VaultConfiguration[MindAttic:Vault:LLM:{providerId}:apiKey] should drive {propertyName}");
    }
}
