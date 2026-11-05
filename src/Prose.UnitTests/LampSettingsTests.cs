namespace Prose.UnitTests;

using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using MindAttic.Vault.Credentials;
using MindAttic.Vault.Launch;
using MindAttic.Vault.Settings;
using MindAttic.Vault.Settings.Manifest;
using Prose.Core;
using Prose.Core.Services;
using Prose.WriterUi.Services;

/// <summary>
/// PRS-T829 (LAMP): Prose's app-level settings live in MindAttic Vault (<see cref="ProseSettings"/>). One test per
/// setting proves it end to end — today's Settings.json value imports to the right Vault name, a value edited in
/// Vault is what <see cref="SettingsService"/> (which every consumer reads) returns, and a value set through the app
/// is saved to Vault — plus the import, save, secrets, manifest, env-override, key and cog paths.
/// Vault roots are temp folders for the whole assembly (<see cref="ProseVaultIsolation"/>).
/// </summary>
[TestFixture, NonParallelizable]
public sealed class LampSettingsTests
{
    private string dir = "";
    private string settingsDir = "";
    private string vaultDir = "";
    private string? prevCredsEnv;
    private IConfiguration? prevVaultConfig;
    private IVaultDesktopLauncher prevLauncher = null!;

    private static readonly string[] OverrideEnvVars =
    [
        "PROSE_SMTP_HOST", "PROSE_SMTP_PORT", "PROSE_SMTP_USERNAME", "PROSE_SMTP_PASSWORD", "PROSE_SMTP_FROM",
        "PROSE_MAP_APP_ID", "PROSE_CLAUDE_API_KEY", "PROSE_OPENAI_API_KEY", "PROSE_DATA_ROOT",
    ];

    private JsonSettingsStore<ProseSettings> Vault => new(vaultDir);
    private SettingsService Open() => new(settingsDir, Vault);
    private string LegacyPath => Path.Combine(settingsDir, "Settings.json");

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "prose-lamp-" + Guid.NewGuid().ToString("N"));
        settingsDir = Path.Combine(dir, "local");
        vaultDir = Path.Combine(dir, "vault");
        Directory.CreateDirectory(settingsDir);

        // Keys resolve through Vault's named lists (temp roaming root), not a bare override folder.
        prevCredsEnv = Environment.GetEnvironmentVariable(LlmCredentialStore.DirectoryEnvVar);
        Environment.SetEnvironmentVariable(LlmCredentialStore.DirectoryEnvVar, null);
        prevVaultConfig = SettingsService.VaultConfiguration;
        SettingsService.VaultConfiguration = null;
        foreach (var v in OverrideEnvVars) Environment.SetEnvironmentVariable(v, null);
        prevLauncher = VaultSettings.Launcher;
        ResetVaultKeys();
    }

    [TearDown]
    public void TearDown()
    {
        VaultSettings.Launcher = prevLauncher;
        SettingsService.VaultConfiguration = prevVaultConfig;
        Environment.SetEnvironmentVariable(LlmCredentialStore.DirectoryEnvVar, prevCredsEnv);
        foreach (var v in OverrideEnvVars) Environment.SetEnvironmentVariable(v, null);
        ResetVaultKeys();
        try { Directory.Delete(dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    /// <summary>Empties the (temp) Vault key files, assignments and key health between tests.</summary>
    private static void ResetVaultKeys()
    {
        foreach (var d in new[] { MindAttic.Vault.Paths.VaultPaths.RoamingRoot, MindAttic.Vault.Paths.VaultPaths.LocalRoot })
            try { if (Directory.Exists(d)) Directory.Delete(d, recursive: true); } catch (IOException) { }
    }

    private void WriteLegacy(JsonObject values) => File.WriteAllText(LegacyPath, values.ToJsonString());

    private static PropertyInfo VaultProp(string name) => typeof(ProseSettings).GetProperty(name)!;
    private static PropertyInfo ServiceProp(string name) => typeof(SettingsService).GetProperty(name)!;
    private static string Camel(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    /// <summary>Two distinct, valid values for a setting: (today's stored value, the value edited in Vault).</summary>
    private (object Legacy, object Edited) ValuesFor(string name)
    {
        var p = VaultProp(name);
        var def = p.GetValue(new ProseSettings());
        switch (name)
        {
            case nameof(ProseSettings.CanonRootPath):
                // Must look like a canon root (engine\data) or SettingsService auto-detects another one.
                return (CanonRoot("canon-a"), CanonRoot("canon-b"));
            case nameof(ProseSettings.Theme): return ("dark", "light");
            case nameof(ProseSettings.AudiobookFormat): return ("flac", "wav");
            case nameof(ProseSettings.MapService): return ("here", "google");
            case nameof(ProseSettings.TimestampFormat): return ("yyyy-MM-dd HH:mm", "dd MMM yyyy HH:mm:ss");
        }
        return def switch
        {
            bool b => (!b, b),
            int i => (i + 1, i + 2),
            double => (0.25, 0.875),
            _ => ($"legacy-{name}", $"vault-{name}"),
        };
    }

    private string CanonRoot(string leaf)
    {
        var root = Path.Combine(dir, leaf);
        Directory.CreateDirectory(Path.Combine(root, Constants.Folders.Engine, "data"));
        return root;
    }

    /// <summary>
    /// End to end for one setting: (1) today's Settings.json value imports into Vault under its full name and is
    /// what SettingsService returns; (2) a value edited in Vault (as Vault.Desktop does) is what SettingsService
    /// returns; (3) a value set through SettingsService is saved to Vault, and Settings.json keeps its old copy.
    /// </summary>
    private void Prove(string name)
    {
        var (legacy, edited) = ValuesFor(name);
        var vaultProp = VaultProp(name);
        var serviceProp = ServiceProp(name);
        Assert.That(serviceProp, Is.Not.Null, $"SettingsService.{name} is how consumers read it");

        // 1. Import of today's value.
        WriteLegacy(new JsonObject { [name] = JsonSerializer.SerializeToNode(legacy) });
        Assert.That(Vault.Exists(), Is.False);
        using (var svc = Open())
            Assert.That(serviceProp.GetValue(svc), Is.EqualTo(legacy), "imported value read through SettingsService");
        var file = JsonNode.Parse(File.ReadAllText(Vault.FilePath))!.AsObject();
        Assert.That(file[Camel(name)]?.ToJsonString(), Is.EqualTo(JsonSerializer.Serialize(legacy)),
            $"settings.json holds it as {SettingsManifest.PrefixFor(ProseSettings.AppId)}:{Camel(name)}");

        // 2. Edited in Vault → the app reads it.
        var v = Vault.Load();
        vaultProp.SetValue(v, edited);
        Vault.Save(v);
        using (var svc = Open())
            Assert.That(serviceProp.GetValue(svc), Is.EqualTo(edited), "the Vault value is what the app uses");

        // 3. Set through the app → saved to Vault; the legacy file keeps its pre-LAMP copy (the rollback).
        using (var svc = Open())
        {
            serviceProp.SetValue(svc, legacy);
            svc.Flush();
        }
        Assert.That(vaultProp.GetValue(Vault.Load()), Is.EqualTo(legacy), "a save from the app lands in Vault");
        var legacyFile = JsonNode.Parse(File.ReadAllText(LegacyPath))!.AsObject();
        Assert.That(legacyFile[name]?.ToJsonString(), Is.EqualTo(JsonSerializer.Serialize(legacy)),
            "Settings.json is never rewritten for a Vault-owned field");
    }

    // ── One test per setting ─────────────────────────────────────────────────
    [Test]
    public void DefaultStorageDirectory_FollowsTheVaultLocalRoot_SoTestsNeverTouchTheRealSettingsJson()
    {
        // PRS-T832: the DI singleton (new SettingsService()) reads and rewrites Settings.json in this folder; it must
        // move with MINDATTIC_VAULT_LOCAL_ROOT like Vault does, or the isolated suite rewrites the user's real file.
        var localRoot = Environment.GetEnvironmentVariable(MindAttic.Vault.Paths.VaultPaths.LocalRootEnvVar);
        Assert.That(localRoot, Is.Not.Null.And.Not.Empty, "ProseVaultIsolation sets the local root");
        Assert.That(SettingsService.DefaultStorageDirectory, Is.EqualTo(Path.Combine(localRoot!, "Prose")));
        Assert.That(SettingsService.DefaultStorageDirectory, Is.Not.EqualTo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MindAttic", "Prose")).IgnoreCase);
    }

    [Test] public void ActiveLlmProvider_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ActiveLlmProvider));
    [Test] public void ActiveLlmProviderChain_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ActiveLlmProviderChain));
    [Test] public void Model_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.Model));
    [Test] public void MaxTokens_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.MaxTokens));
    [Test] public void OpenAiModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.OpenAiModel));
    [Test] public void GeminiModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.GeminiModel));
    [Test] public void DeepSeekModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.DeepSeekModel));
    [Test] public void MistralModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.MistralModel));
    [Test] public void KimiModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.KimiModel));
    [Test] public void PerplexityModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.PerplexityModel));
    [Test] public void GrokModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.GrokModel));
    [Test] public void GroqModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.GroqModel));
    [Test] public void TogetherModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TogetherModel));
    [Test] public void OpenRouterModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.OpenRouterModel));
    [Test] public void FireworksModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.FireworksModel));
    [Test] public void CohereModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.CohereModel));
    [Test] public void ReviewBallots_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewBallots));
    [Test] public void ReviewProse_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewProse));
    [Test] public void ReviewPanel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewPanel));
    [Test] public void ReviewReaders_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewReaders));
    [Test] public void ReviewJudgeProvider_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewJudgeProvider));
    [Test] public void ReviewAllowedProviders_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewAllowedProviders));
    [Test] public void ReviewMaxConcurrency_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewMaxConcurrency));
    [Test] public void ReviewAutoRunEnabled_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReviewAutoRunEnabled));
    [Test] public void ReaderQaJuryProviders_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ReaderQaJuryProviders));
    [Test] public void ExtraJuryProvidersJson_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ExtraJuryProvidersJson));
    [Test] public void ComprehensionProbeModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ComprehensionProbeModel));
    [Test] public void ComprehensionArbiterModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ComprehensionArbiterModel));
    [Test] public void LocalReviewBaseUrl_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalReviewBaseUrl));
    [Test] public void LocalReviewModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalReviewModel));
    [Test] public void LocalReviewLabel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalReviewLabel));
    [Test] public void LocalReviewMaxConcurrency_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalReviewMaxConcurrency));
    [Test] public void LocalReviewContextTokens_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalReviewContextTokens));
    [Test] public void LocalLlmBaseUrl_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalLlmBaseUrl));
    [Test] public void LocalLlmModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalLlmModel));
    [Test] public void LocalEmbeddingBaseUrl_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalEmbeddingBaseUrl));
    [Test] public void LocalEmbeddingModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.LocalEmbeddingModel));
    [Test] public void ElevenLabsVoiceId_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.ElevenLabsVoiceId));
    [Test] public void NarratorVoiceName_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.NarratorVoiceName));
    [Test] public void TtsModel_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsModel));
    [Test] public void TtsStability_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsStability));
    [Test] public void TtsSimilarityBoost_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsSimilarityBoost));
    [Test] public void TtsStyle_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsStyle));
    [Test] public void TtsUseAudioTags_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsUseAudioTags));
    [Test] public void AudiobookFormat_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.AudiobookFormat));
    [Test] public void TtsPauseSectionMs_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsPauseSectionMs));
    [Test] public void TtsPauseSceneMs_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsPauseSceneMs));
    [Test] public void TtsPauseParagraphMs_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsPauseParagraphMs));
    [Test] public void TtsPauseContinuationMs_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TtsPauseContinuationMs));
    [Test] public void Theme_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.Theme));
    [Test] public void FontFamily_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.FontFamily));
    [Test] public void EditorFontSize_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.EditorFontSize));
    [Test] public void AutoSaveIntervalMs_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.AutoSaveIntervalMs));
    [Test] public void TimestampFormat_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TimestampFormat));
    [Test] public void TimezoneId_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.TimezoneId));
    [Test] public void RepoListOnRight_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.RepoListOnRight));
    [Test] public void CanonRootPath_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.CanonRootPath));
    [Test] public void DocContextEnabled_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.DocContextEnabled));
    [Test] public void DcmLoggingEnabled_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.DcmLoggingEnabled));
    [Test] public void EnablePlainTextNer_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.EnablePlainTextNer));
    [Test] public void SaveStoriesAsMarkdown_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.SaveStoriesAsMarkdown));
    [Test] public void AutoCanonGrounding_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.AutoCanonGrounding));
    [Test] public void AutoHarvestRevealedDetails_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.AutoHarvestRevealedDetails));
    [Test] public void PublishOutputDirectory_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.PublishOutputDirectory));
    [Test] public void PublishExportDirectory_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.PublishExportDirectory));
    [Test] public void DocxIncludeToc_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.DocxIncludeToc));
    [Test] public void MapService_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.MapService));
    [Test] public void MapAppId_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.MapAppId));
    [Test] public void MapMode_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.MapMode));
    [Test] public void SmtpHost_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.SmtpHost));
    [Test] public void SmtpPort_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.SmtpPort));
    [Test] public void SmtpUsername_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.SmtpUsername));
    [Test] public void SmtpFrom_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.SmtpFrom));
    [Test] public void SmtpEnableSsl_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.SmtpEnableSsl));
    [Test] public void HubNarrationEnabled_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.HubNarrationEnabled));
    [Test] public void CorobsEnabled_ImportsRoundTripsAndIsRead() => Prove(nameof(ProseSettings.CorobsEnabled));

    [Test]
    public void EverySettingHasItsOwnTest()
    {
        var tests = typeof(LampSettingsTests).GetMethods()
            .Where(m => m.GetCustomAttribute<TestAttribute>() is not null).Select(m => m.Name).ToHashSet();
        var settings = typeof(ProseSettings).GetProperties()
            .Where(p => p.GetCustomAttribute<SettingAttribute>() is not null).Select(p => p.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(settings, Has.Count.EqualTo(76));
            Assert.That(typeof(ProseSettings).GetProperties().Where(p => p.GetCustomAttribute<SettingAttribute>() is null), Is.Empty,
                "every public property is a labeled [Setting]");
            Assert.That(settings.Where(s => !tests.Contains(s + "_ImportsRoundTripsAndIsRead")), Is.Empty);
        });
    }

    [Test]
    public void EverySetting_IsAPropertyOfSettingsServiceAndOfTheOldSettingsFile()
    {
        var legacyType = typeof(SettingsService).GetNestedType("SettingsData", BindingFlags.NonPublic)!;
        foreach (var p in typeof(ProseSettings).GetProperties())
        {
            Assert.That(typeof(SettingsService).GetProperty(p.Name)?.PropertyType, Is.EqualTo(p.PropertyType), $"SettingsService.{p.Name}");
            Assert.That(legacyType.GetProperty(p.Name)?.PropertyType, Is.EqualTo(p.PropertyType), $"Settings.json field {p.Name} (import is by name)");
        }
    }

    // ── Defaults / seed ──────────────────────────────────────────────────────

    [Test]
    public void LoadOrSeed_OnAnEmptyFolder_SeedsTodaysDefaults()
    {
        var seeded = Vault.LoadOrSeed();
        var legacyDefaults = Activator.CreateInstance(typeof(SettingsService).GetNestedType("SettingsData", BindingFlags.NonPublic)!)!;
        Assert.Multiple(() =>
        {
            Assert.That(Vault.Exists(), Is.True);
            foreach (var p in typeof(ProseSettings).GetProperties())
                Assert.That(p.GetValue(seeded), Is.EqualTo(legacyDefaults.GetType().GetProperty(p.Name)!.GetValue(legacyDefaults)),
                    $"{p.Name}: the first-run default is what Prose used before LAMP");
            Assert.That(File.Exists(Path.Combine(vaultDir, SettingsManifest.FileName)), Is.True, "LoadOrSeed writes the manifest");
        });
    }

    [Test]
    public void FreshInstall_WithNoSettingsJson_SeedsDefaultsIntoVault()
    {
        using (var svc = Open())
            Assert.That(svc.MaxTokens, Is.EqualTo(2048));
        Assert.That(Vault.Exists(), Is.True);
        Assert.That(Vault.Load().ReviewBallots, Is.EqualTo(20));
    }

    // ── Import ───────────────────────────────────────────────────────────────

    [Test]
    public void FirstLoad_ImportsTodaysRealShapedSettingsJson()
    {
        // The shape of the real %LOCALAPPDATA%\MindAttic\Prose\Settings.json (2026-10-31), secrets as canaries.
        WriteLegacy(new JsonObject
        {
            ["ApiKey"] = "CANARY-claude",
            ["Model"] = "claude-haiku-4-5-20251001",
            ["Theme"] = "dark",
            ["TtsModel"] = "eleven_v3",
            ["PublishExportDirectory"] = @"D:\Projects\MindAttic\ePub\MindAttic\",
            ["CohereModel"] = "command-r-plus-08-2024",
            ["MapService"] = "here",
            ["MapAppId"] = "app-id-1",
            ["MapApiKey"] = "CANARY-here",
            ["SmtpPassword"] = "CANARY-smtp",
            ["FtpPassword"] = "CANARY-ftp",
            ["ReviewJudgeProvider"] = "claude-api",
            ["ReviewMaxConcurrency"] = 3,
            ["LocalReviewBaseUrl"] = "https://pod.example/v1/chat/completions",
            ["LocalReviewModel"] = "qwen2.5-72b-32k",
            ["LocalReviewLabel"] = "runpod",
            ["LocalReviewApiKey"] = "CANARY-pod",
            ["LocalReviewContextTokens"] = 32768,
            ["HubApiKey"] = "CANARY-hub",
            ["UniverseExportDirectories"] = new JsonObject { ["glmz"] = "GLMZ" },
            ["AutoGenerateCoverOnExport"] = false,
            ["WorldTickEnabled"] = false,
        });

        using var svc = Open();
        var v = Vault.Load();
        Assert.Multiple(() =>
        {
            Assert.That(v.Model, Is.EqualTo("claude-haiku-4-5-20251001"));
            Assert.That(v.Theme, Is.EqualTo("dark"));
            Assert.That(v.TtsModel, Is.EqualTo("eleven_v3"));
            Assert.That(v.PublishExportDirectory, Is.EqualTo(@"D:\Projects\MindAttic\ePub\MindAttic\"));
            Assert.That(v.CohereModel, Is.EqualTo("command-r-plus-08-2024"));
            Assert.That(v.MapService, Is.EqualTo("here"));
            Assert.That(v.MapAppId, Is.EqualTo("app-id-1"));
            Assert.That(v.ReviewJudgeProvider, Is.EqualTo("claude-api"), "imported as-is (only the provider lists are normalized)");
            Assert.That(v.ReviewMaxConcurrency, Is.EqualTo(3));
            Assert.That(v.LocalReviewLabel, Is.EqualTo("runpod"));
            Assert.That(v.LocalReviewContextTokens, Is.EqualTo(32768));
            Assert.That(v.ReviewBallots, Is.EqualTo(20), "a field missing from Settings.json gets today's default");
            // What stays out of Vault still works from Settings.json.
            Assert.That(svc.GetExportDirectory("glmz"), Is.EqualTo(Path.Combine(@"D:\Projects\MindAttic\ePub\MindAttic\", "GLMZ")));
            Assert.That(svc.SmtpPassword, Is.EqualTo("CANARY-smtp"));
            Assert.That(svc.HubApiKey, Is.EqualTo("CANARY-hub"));
            Assert.That(svc.LocalReviewApiKey, Is.EqualTo("CANARY-pod"));
        });
        Assert.That(File.ReadAllText(Vault.FilePath), Does.Not.Contain("CANARY"));
    }

    [Test]
    public void Import_HappensOnce_AnExistingVaultFileIsNeverOverwritten()
    {
        WriteLegacy(new JsonObject { ["MaxTokens"] = 1111 });
        using (Open()) { }
        WriteLegacy(new JsonObject { ["MaxTokens"] = 2222 });
        using var svc = Open();
        Assert.That(svc.MaxTokens, Is.EqualTo(1111));
    }

    [Test]
    public void LegacyClaudeProviderIds_AreNormalizedInVault()
    {
        WriteLegacy(new JsonObject
        {
            ["ActiveLlmProvider"] = "claude-api",
            ["ActiveLlmProviderChain"] = "claude-api,claude-team,openai",
            ["ReviewAllowedProviders"] = "claude-team",
        });
        using (var svc = Open())
            Assert.That(svc.ActiveLlmProvider, Is.EqualTo("claude"));
        var v = Vault.Load();
        Assert.That(v.ActiveLlmProviderChain, Is.EqualTo("claude,openai"));
        Assert.That(v.ReviewAllowedProviders, Is.EqualTo("claude"));
    }

    // ── Save / merge / clamps ────────────────────────────────────────────────

    [Test]
    public void Save_RoundTrip_ThroughTheDebouncedFlushAndANewInstance()
    {
        using (var svc = Open())
        {
            svc.Model = "claude-test";
            svc.ReviewReaders = 77;
            svc.TtsStability = 0.3;
            svc.Flush();
        }
        using var again = Open();
        Assert.Multiple(() =>
        {
            Assert.That(again.Model, Is.EqualTo("claude-test"));
            Assert.That(again.ReviewReaders, Is.EqualTo(77));
            Assert.That(again.TtsStability, Is.EqualTo(0.3));
        });
    }

    [Test]
    public void TwoProcesses_DoNotClobberEachOthersVaultFields()
    {
        using var a = Open();
        using var b = Open();
        a.MaxTokens = 4242;
        a.Flush();
        b.MapMode = "light";
        b.Flush();

        var v = Vault.Load();
        Assert.That(v.MaxTokens, Is.EqualTo(4242), "b's stale copy must not overwrite a's change");
        Assert.That(v.MapMode, Is.EqualTo("light"));
    }

    [Test]
    public void Clamps_StillApply_ToValuesEditedInVault()
    {
        using (Open()) { }
        var v = Vault.Load();
        v.ReviewBallots = 0;
        v.ReviewProse = -5;
        v.ReviewPanel = 0;
        v.ReviewReaders = -1;
        v.ReviewMaxConcurrency = 500;
        v.LocalReviewMaxConcurrency = 99;
        v.LocalReviewContextTokens = 10;
        Vault.Save(v);

        using var svc = Open();
        Assert.Multiple(() =>
        {
            Assert.That(svc.ReviewBallots, Is.EqualTo(1));
            Assert.That(svc.ReviewProse, Is.EqualTo(0));
            Assert.That(svc.ReviewPanel, Is.EqualTo(1));
            Assert.That(svc.ReviewReaders, Is.EqualTo(1));
            Assert.That(svc.ReviewMaxConcurrency, Is.EqualTo(50));
            Assert.That(svc.LocalReviewMaxConcurrency, Is.EqualTo(16));
            Assert.That(svc.LocalReviewContextTokens, Is.EqualTo(4096));
        });
    }

    [Test]
    public void ResetToDefaults_ResetsTheVaultValues()
    {
        using var svc = Open();
        svc.MaxTokens = 999;
        svc.Flush();
        svc.ResetToDefaults();
        Assert.That(svc.MaxTokens, Is.EqualTo(2048));
        Assert.That(Vault.Load().MaxTokens, Is.EqualTo(2048));
    }

    // ── Live apply (Vault.Desktop saves while Prose runs) ────────────────────

    [Test]
    public void ReloadVault_AppliesAnExternalSave_AndRaisesThemeChanged()
    {
        using var svc = Open();
        string? raised = null;
        var changed = 0;
        svc.ThemeChanged += t => raised = t;
        svc.Changed += () => changed++;

        var v = Vault.Load();
        v.Theme = "light";
        v.MaxTokens = 3333;
        Vault.Save(v);
        svc.ReloadVault();

        Assert.Multiple(() =>
        {
            Assert.That(svc.Theme, Is.EqualTo("light"));
            Assert.That(svc.MaxTokens, Is.EqualTo(3333));
            Assert.That(raised, Is.EqualTo("light"));
            Assert.That(changed, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReloadVault_KeepsThisProcesssUnsavedChange()
    {
        using var svc = Open();
        svc.EditorFontSize = 21;                 // pending, not flushed yet
        var v = Vault.Load();
        v.FontFamily = "Georgia";
        Vault.Save(v);
        svc.ReloadVault();
        Assert.That(svc.EditorFontSize, Is.EqualTo(21));
        Assert.That(svc.FontFamily, Is.EqualTo("Georgia"));
        Assert.That(Vault.Load().EditorFontSize, Is.EqualTo(21));
    }

    [Test]
    public async Task Watch_PicksUpAVaultDesktopSave_WithoutARestart()
    {
        using var svc = Open();
        svc.Watch();
        var themed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.ThemeChanged += t => themed.TrySetResult(t);

        var v = Vault.Load();
        v.Theme = "light";
        Vault.Save(v);

        var done = await Task.WhenAny(themed.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.That(done, Is.SameAs(themed.Task), "ThemeChanged fired from the file watcher");
        Assert.That(svc.Theme, Is.EqualTo("light"));
    }

    // ── Secrets ──────────────────────────────────────────────────────────────

    [Test]
    public void Secrets_NeverReachTheVaultSettingsFileOrManifest()
    {
        WriteLegacy(new JsonObject
        {
            ["ApiKey"] = "CANARY-1", ["OpenAiApiKey"] = "CANARY-2", ["ElevenLabsApiKey"] = "CANARY-3",
            ["SmtpPassword"] = "CANARY-4", ["FtpPassword"] = "CANARY-5", ["HubApiKey"] = "CANARY-6",
            ["LocalReviewApiKey"] = "CANARY-7", ["LocalLlmApiKey"] = "CANARY-8", ["LocalEmbeddingApiKey"] = "CANARY-9",
            ["GoogleMapsApiKey"] = "CANARY-10", ["KlingApiKey"] = "CANARY-11",
        });
        using (var svc = Open())
        {
            svc.SmtpPassword = "CANARY-12";
            svc.HubApiKey = "CANARY-13";
            svc.LocalLlmApiKey = "CANARY-14";
            svc.Flush();
        }
        var files = Directory.GetFiles(vaultDir).Select(File.ReadAllText).ToList();
        Assert.That(files, Has.Count.GreaterThanOrEqualTo(2), "settings.json and settings.manifest.json");
        Assert.That(files, Has.None.Contains("CANARY"));
        Assert.That(typeof(ProseSettings).GetProperties().Select(p => p.Name),
            Has.None.Matches<string>(n => System.Text.RegularExpressions.Regex.IsMatch(n, "(apikey|password|secret|token)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)));
    }

    // ── Manifest ─────────────────────────────────────────────────────────────

    [Test]
    public void Manifest_DescribesEverySetting_LabeledAndGrouped_WithItsKeys()
    {
        var manifest = SettingsManifestBuilder.Build<ProseSettings>();
        var fields = manifest.AllFields().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(manifest.AppId, Is.EqualTo("Prose"));
            Assert.That(manifest.ConfigurationPrefix, Is.EqualTo("MindAttic:Settings:Prose"));
            Assert.That(fields, Has.Count.EqualTo(76));
            Assert.That(fields, Has.All.Matches<SettingField>(f => !string.IsNullOrWhiteSpace(f.Label) && f.Group != SettingsManifest.DefaultGroup));
            Assert.That(fields.Select(f => f.Group).Distinct(), Is.EquivalentTo(new[]
            {
                "LLM models", "Review and jury", "Local LLM", "TTS and narration", "Editor and appearance",
                "Writing pipeline", "Export and publish", "Maps", "Email (SMTP)", "Hub",
            }));
            foreach (var p in typeof(ProseSettings).GetProperties())
                Assert.That(fields.Select(f => f.Name), Does.Contain($"MindAttic:Settings:Prose:{Camel(p.Name)}"));
            Assert.That(manifest.Credentials.Select(c => c.ProviderId), Is.SupersetOf(new[]
            {
                "claude", "openai", "gemini", "deepseek", "mistral", "kimi", "perplexity", "grok", "groq",
                "together", "openrouter", "fireworks", "cohere", "elevenlabs", "here-maps", "google-maps",
            }));
            Assert.That(manifest.Credentials.Single(c => c.ProviderId == "claude").Required, Is.True);
        });
    }

    [Test]
    public void WriteSettingsManifest_ImportsTodaysValues_WritesTheManifest_AndExits()
    {
        WriteLegacy(new JsonObject { ["Model"] = "claude-from-today", ["ApiKey"] = "CANARY" });
        var output = new StringWriter();

        var ran = SettingsService.TryRunManifestCommand(["--write-settings-manifest"], output, settingsDir, Vault);

        Assert.Multiple(() =>
        {
            Assert.That(ran, Is.True);
            Assert.That(output.ToString().Trim(), Is.EqualTo(vaultDir));
            Assert.That(Vault.Load().Model, Is.EqualTo("claude-from-today"));
            Assert.That(File.Exists(Path.Combine(vaultDir, SettingsManifest.FileName)), Is.True);
            Assert.That(SettingsService.TryRunManifestCommand(["--universe", "glmz"], output, settingsDir, Vault), Is.False);
        });
    }

    // ── Keys (KeySelector / named keys, app "Prose") ─────────────────────────

    private static NamedKeyStore Keys => NamedKeyStore.ForRoamingRoot();

    [Test]
    public void ApiKey_ComesFromTheSharedNamedList()
    {
        Keys.Add("claude", "key1", "shared-claude");
        using var svc = Open();
        Assert.That(svc.ApiKey, Is.EqualTo("shared-claude"));
    }

    [Test]
    public void ApiKey_ProsesAssignmentWins_OverTheSharedOrder()
    {
        Keys.Add("claude", "key1", "shared-claude");
        Keys.Add("claude", "2026-10-31 Prose", "prose-claude");
        Keys.Assignments.Set("Prose", "claude", ["2026-10-31 Prose"]);
        using var svc = Open();
        Assert.That(svc.ApiKey, Is.EqualTo("prose-claude"));
        Assert.That(SettingsService.AssignedVaultKeys("claude"), Is.EqualTo(new[] { "prose-claude" }));
    }

    [Test]
    public void EnvVarOverride_StillBeatsTheSharedList()
    {
        Keys.Add("claude", "key1", "shared-claude");
        Environment.SetEnvironmentVariable("PROSE_CLAUDE_API_KEY", "env-claude");
        using var svc = Open();
        Assert.That(svc.ApiKey, Is.EqualTo("env-claude"));
    }

    [Test]
    public void ProsesAssignedKey_StillBeatsTheEnvVar_AsItsOwnKeyDidBeforeLamp()
    {
        Keys.Add("openai", "mine", "prose-openai");
        Keys.Assignments.Set("Prose", "openai", ["mine"]);
        Environment.SetEnvironmentVariable("PROSE_OPENAI_API_KEY", "env-openai");
        using var svc = Open();
        Assert.That(svc.OpenAiApiKey, Is.EqualTo("prose-openai"));
    }

    [Test]
    public void VaultConfiguration_StillBeatsEverything()
    {
        Keys.Add("claude", "mine", "prose-claude");
        Keys.Assignments.Set("Prose", "claude", ["mine"]);
        Environment.SetEnvironmentVariable("PROSE_CLAUDE_API_KEY", "env-claude");
        SettingsService.VaultConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MindAttic:Vault:LLM:claude:apiKey"] = "cloud-claude" }).Build();
        using var svc = Open();
        Assert.That(svc.ApiKey, Is.EqualTo("cloud-claude"));
    }

    [Test]
    public void NonKeyEnvOverrides_StillWin_OverVaultValues()
    {
        using (Open()) { }
        var v = Vault.Load();
        v.SmtpHost = "vault-host"; v.SmtpPort = 25; v.SmtpUsername = "vault-user"; v.SmtpFrom = "vault@x"; v.MapAppId = "vault-app";
        Vault.Save(v);
        Environment.SetEnvironmentVariable("PROSE_SMTP_HOST", "env-host");
        Environment.SetEnvironmentVariable("PROSE_SMTP_PORT", "2525");
        Environment.SetEnvironmentVariable("PROSE_SMTP_USERNAME", "env-user");
        Environment.SetEnvironmentVariable("PROSE_SMTP_FROM", "env@x");
        Environment.SetEnvironmentVariable("PROSE_MAP_APP_ID", "env-app");

        using var svc = Open();
        Assert.Multiple(() =>
        {
            Assert.That(svc.SmtpHost, Is.EqualTo("env-host"));
            Assert.That(svc.SmtpPort, Is.EqualTo(2525));
            Assert.That(svc.SmtpUsername, Is.EqualTo("env-user"));
            Assert.That(svc.SmtpFrom, Is.EqualTo("env@x"));
            Assert.That(svc.MapAppId, Is.EqualTo("env-app"));
        });
    }

    [Test]
    public void GrokKey_ResolvesThroughTheXaiAlias()
    {
        Keys.Add("grok", "key1", "shared-grok");
        using var svc = Open();
        Assert.That(svc.GrokApiKey, Is.EqualTo("shared-grok"));
    }

    [Test]
    public async Task LlmCall_FailsOver_FirstKey401_SecondKeyUsed_AndProseThenSkipsTheBadKey()
    {
        Keys.Add("claude", "bad", "key-bad");
        Keys.Add("claude", "good", "key-good");
        Keys.Assignments.Set("Prose", "claude", ["bad", "good"]);
        var used = new List<string>();
        using var http = new HttpClient(new ScriptedHandler(req =>
        {
            var key = req.Headers.GetValues("x-api-key").Single();
            used.Add(key);
            return new HttpResponseMessage(key == "key-bad" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
        }));
        var selector = new KeySelector(new FileKeySource(Keys), KeyHealthStore.ForLocalRoot());

        using var response = await selector.SendAsync("claude", ProseSettings.AppId, (key, ct) =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.test/v1/messages");
            req.Headers.Add("x-api-key", key);
            return http.SendAsync(req, ct);
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(used, Is.EqualTo(new[] { "key-bad", "key-good" }));
        // The 401 flagged the key in shared health: Prose's own resolution now skips it too.
        using var svc = Open();
        Assert.That(svc.ApiKey, Is.EqualTo("key-good"));
        Assert.That(SettingsService.AssignedVaultKeys("claude"), Is.EqualTo(new[] { "key-good" }));
    }

    [Test]
    public void SettingAKeyThroughTheApp_StoresItInVault_AsProsesActiveKey()
    {
        Keys.Add("deepseek", "key1", "shared-deepseek");
        using (var svc = Open()) svc.DeepSeekApiKey = "typed-deepseek";
        Assert.That(Keys.Assignments.Get("Prose", "deepseek").First(), Is.EqualTo("Prose"));
        Assert.That(Keys.Find("deepseek", "Prose")?.Value, Is.EqualTo("typed-deepseek"));
        Assert.That(File.ReadAllText(Vault.FilePath), Does.Not.Contain("typed-deepseek"));
    }

    [Test]
    public void OwnKeyCopies_UnderThePreLampProsePrefix_AreRecognizedByVaultsImporter() =>
        Assert.That(KeyImporter.AppPrefixes["prose"], Is.EqualTo(ProseSettings.AppId));

    // ── Old settings UI gone; cog opens Vault ────────────────────────────────

    private sealed class FakeLauncher(bool answers) : IVaultDesktopLauncher
    {
        public List<VaultOpenRequest> Requests { get; } = new();
        public bool TrySend(VaultOpenRequest request) { Requests.Add(request); return answers; }
        public bool TryStart(VaultOpenRequest request) { Requests.Add(request); return answers; }
    }

    [Test]
    public void OldKeyEditingUi_IsGone()
    {
        var methods = typeof(SettingsUiService).GetMethods().Select(m => m.Name).ToList();
        Assert.That(methods.Intersect(new[] { "AddKey", "RemoveAt", "SetElevenLabsKey", "KeysFor" }), Is.Empty);
        var panel = typeof(Prose.WriterUi.Components.SettingsPanel)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Select(m => m.Name).ToList();
        Assert.That(panel.Intersect(new[] { "Add", "Remove", "SaveElevenLabs" }), Is.Empty);
        Assert.That(panel, Does.Contain("OpenVault"));
    }

    [Test]
    public void SettingsUi_OpenVault_OpensProsesTab()
    {
        var fake = new FakeLauncher(answers: true);
        VaultSettings.Launcher = fake;
        var ui = new SettingsUiService(null!, null!);

        Assert.That(ui.OpenVault(), Is.True);
        Assert.That(fake.Requests.Single().AppId, Is.EqualTo("Prose"));
        Assert.That(ui.VaultLink, Does.StartWith("mindattic-vault://").And.Contain("Prose"));
    }

    [Test]
    public void WriterCog_OpensVault_AndFallsBackToThePanelWhenVaultIsMissing()
    {
        var shellType = typeof(Prose.WriterUi.Components.WriterShell);
        var open = shellType.GetMethod("OpenVaultSettings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var settingsOpen = shellType.GetField("settingsOpen", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var injected = shellType.GetProperty("SettingsUi", BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var installed in new[] { true, false })
        {
            var fake = new FakeLauncher(installed);
            VaultSettings.Launcher = fake;
            var shell = Activator.CreateInstance(shellType)!;
            injected.SetValue(shell, new SettingsUiService(null!, null!));

            open.Invoke(shell, null);

            Assert.That(fake.Requests.First().AppId, Is.EqualTo("Prose"));
            Assert.That(settingsOpen.GetValue(shell), Is.EqualTo(!installed),
                installed ? "Vault opened; no in-app settings screen" : "Vault missing: the panel with the Vault link opens");
        }
    }

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
