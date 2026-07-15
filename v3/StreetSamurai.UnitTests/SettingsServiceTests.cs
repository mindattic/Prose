namespace StreetSamurai.UnitTests;

using StreetSamurai.Core.Services;

[TestFixture]
public class SettingsServiceTests
{
    private SettingsService svc = null!;
    private string tempDir = null!;
    private string? prevCredsEnv;

    [SetUp]
    public void Setup()
    {
        tempDir = Path.Combine(Path.GetTempPath(), "ss_settings_test_" + Guid.NewGuid().ToString("N")[..8]);
        // Redirect the shared MindAttic credential store to a per-test directory so we
        // don't read/write the user's real %APPDATA%/MindAttic/LLM/ folder.
        prevCredsEnv = Environment.GetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS");
        Environment.SetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS", Path.Combine(tempDir, "creds"));
        svc = new SettingsService(tempDir);
    }

    [TearDown]
    public void Teardown()
    {
        svc.Dispose();
        Environment.SetEnvironmentVariable("MINDATTIC_LLM_CREDENTIALS", prevCredsEnv);
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    // ── FormatTimestamp ─────────────────────────────────────

    [Test]
    public void FormatTimestamp_12hFormat_ContainsAmPm()
    {
        svc.TimestampFormat = "yyyy-MM-dd hh:mm:sstt";
        svc.TimezoneId = "Central Standard Time";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 18, 30, 0, DateTimeKind.Utc));
        Assert.That(result, Does.Contain("PM").Or.Contain("AM"));
    }

    [Test]
    public void FormatTimestamp_24hFormat_NoAmPm()
    {
        svc.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        svc.TimezoneId = "UTC";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 18, 30, 0, DateTimeKind.Utc));
        Assert.That(result, Does.Not.Contain("PM"));
        Assert.That(result, Does.Not.Contain("AM"));
    }

    [Test]
    public void FormatTimestamp_MatchesExpectedOutput()
    {
        svc.TimestampFormat = "yyyy-MM-dd hh:mm:sstt";
        svc.TimezoneId = "UTC";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 14, 1, 23, DateTimeKind.Utc));
        Assert.That(result, Is.EqualTo("2026-04-05 02:01:23PM"));
    }

    [Test]
    public void FormatTimestamp_SlashFormat_Works()
    {
        svc.TimestampFormat = "MM/dd/yyyy hh:mm:sstt";
        svc.TimezoneId = "UTC";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 14, 1, 23, DateTimeKind.Utc));
        Assert.That(result, Is.EqualTo("04/05/2026 02:01:23PM"));
    }

    // ── Defaults ────────────────────────────────────────────

    [Test]
    public void Defaults_TimestampFormat_IsDefault()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.TimestampFormat, Is.EqualTo("yyyy-MM-dd hh:mm:sstt"));
    }

    [Test]
    public void Defaults_FontFamily_IsOutfit()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.FontFamily, Is.EqualTo("Outfit"));
    }

    [Test]
    public void Defaults_ActiveLlmProvider_IsClaudeApi()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.ActiveLlmProvider, Is.EqualTo("claude-api"));
    }

    [Test]
    public void Defaults_MaxTokens_Is2048()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.MaxTokens, Is.EqualTo(2048));
    }

    [Test]
    public void Defaults_EditorFontSize_Is14()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.EditorFontSize, Is.EqualTo(14));
    }

    // ── SaveAsDefaults / ResetToDefaults ──────────────────────

    [Test]
    public void ResetToDefaults_WithNoSnapshot_ReturnsFactoryDefaults()
    {
        svc.MaxTokens = 999;
        svc.ApiKey = "sk-test-key";
        svc.ResetToDefaults();
        Assert.That(svc.MaxTokens, Is.EqualTo(2048));
        Assert.That(svc.ApiKey, Is.Empty);
    }

    [Test]
    public void SaveAsDefaults_ThenReset_RestoresSnapshot()
    {
        svc.ApiKey = "sk-saved";
        svc.MaxTokens = 2048;
        svc.SaveAsDefaults();

        svc.ApiKey = "sk-changed";
        svc.MaxTokens = 111;
        svc.ResetToDefaults();

        Assert.That(svc.ApiKey, Is.EqualTo("sk-saved"));
        Assert.That(svc.MaxTokens, Is.EqualTo(2048));
    }

    // ── Debounce ────────────────────────────────────────────

    [Test]
    public void Debounce_InMemoryValueIsImmediate()
    {
        svc.MaxTokens = 7777;
        Assert.That(svc.MaxTokens, Is.EqualTo(7777));
    }

    [Test]
    public async Task Debounce_FlushPersistsToDisk()
    {
        svc.MaxTokens = 8888;
        svc.Flush();

        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.MaxTokens, Is.EqualTo(8888));
        fresh.Dispose();
    }

    [Test]
    public async Task Debounce_TimerPersistsAfterDelay()
    {
        svc.MaxTokens = 9999;
        // Wait for debounce timer to fire (500ms + margin)
        await Task.Delay(700);

        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.MaxTokens, Is.EqualTo(9999));
        fresh.Dispose();
    }

    // ── Multi-process clobber (merge-on-write) ───────────────────────────────

    [Test]
    public void Flush_ConcurrentInstances_DoNotClobberEachOthersFields()
    {
        // Two independent instances over the same store, each loaded from the same starting file —
        // simulating two processes (e.g. a CLI publish and a running web host) sharing Settings.json.
        using var a = new SettingsService(tempDir);
        using var b = new SettingsService(tempDir);

        // Each changes a DIFFERENT field. b's in-memory baseline predates a's write, so a plain
        // whole-object overwrite by b would wipe a's change. Merge-on-write must preserve both.
        a.MaxTokens = 4242;
        a.Flush();
        b.MapMode = "clobber-b";
        b.Flush();

        using var fresh = new SettingsService(tempDir);
        Assert.Multiple(() =>
        {
            Assert.That(fresh.MaxTokens, Is.EqualTo(4242), "a's field was clobbered by b's stale flush");
            Assert.That(fresh.MapMode, Is.EqualTo("clobber-b"));
        });
    }

    [Test]
    public void Flush_ExportDirSurvivesUnrelatedHostFlush()
    {
        // The exact reported bug: a CLI publish writes the per-universe export dir, then a running web
        // host (holding a stale in-memory copy) flushes an unrelated field — the export dir must survive.
        using var cli  = new SettingsService(tempDir);  // simulates `ss --publish --export-dir ...`
        using var host = new SettingsService(tempDir);  // simulates a live Writer/Codex host

        cli.SetUniverseExportDirectory("scry", @"R:\X\Scry");
        cli.Flush();

        host.MapMode = "toggled";  // host changes something unrelated with a copy that predates cli's write
        host.Flush();

        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.GetExportDirectory("scry"), Is.EqualTo(@"R:\X\Scry"),
            "the export-dir map was clobbered by the host's stale flush");
    }

    // ── SMTP defaults ────────────────────────────────────────────────────────

    [Test]
    public void Defaults_SmtpPort_Is587()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.SmtpPort, Is.EqualTo(587));
    }

    [Test]
    public void Defaults_SmtpEnableSsl_IsTrue()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.SmtpEnableSsl, Is.True);
    }

    [Test]
    public void Defaults_SmtpHost_IsEmpty()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.SmtpHost, Is.Empty);
    }

    [Test]
    public void Defaults_SmtpFrom_IsEmpty()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.SmtpFrom, Is.Empty);
    }

    [Test]
    public void SmtpSettings_RoundTrip()
    {
        svc.SmtpHost = "smtp.example.com";
        svc.SmtpPort = 465;
        svc.SmtpUsername = "sender@example.com";
        svc.SmtpFrom = "sender@example.com";
        svc.SmtpEnableSsl = false;
        svc.Flush();

        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.SmtpHost, Is.EqualTo("smtp.example.com"));
        Assert.That(fresh.SmtpPort, Is.EqualTo(465));
        Assert.That(fresh.SmtpUsername, Is.EqualTo("sender@example.com"));
        Assert.That(fresh.SmtpFrom, Is.EqualTo("sender@example.com"));
        Assert.That(fresh.SmtpEnableSsl, Is.False);
    }

    // ── RepoListOnRight ───────────────────────────────────────────────────────

    [Test]
    public void Defaults_RepoListOnRight_IsTrue()
    {
        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.RepoListOnRight, Is.True);
    }

    [Test]
    public void RepoListOnRight_CanBeToggled()
    {
        svc.RepoListOnRight = false;
        svc.Flush();

        using var fresh = new SettingsService(tempDir);
        Assert.That(fresh.RepoListOnRight, Is.False);
    }

    // ── ImportAllVoicesAsProfiles ─────────────────────────────────────────────

    private static readonly TtsVoice[] TwoVoices =
    [
        new() { VoiceId = "vid-oliver", Name = "Oliver Silk" },
        new() { VoiceId = "vid-mara",   Name = "Mara Quint" },
    ];

    [Test]
    public void ImportAllVoices_CreatesTwoBaselinesPerVoice()
    {
        var n = svc.ImportAllVoicesAsProfiles(TwoVoices);

        Assert.That(n, Is.EqualTo(2));
        Assert.That(svc.VoiceProfiles, Has.Count.EqualTo(4));

        var v3 = svc.VoiceProfiles.Single(p => p.Id == "vox-oliver-silk-v3");
        Assert.Multiple(() =>
        {
            Assert.That(v3.Model, Is.EqualTo("eleven_v3"));
            Assert.That(v3.Stability, Is.EqualTo(1.0));         // Robust — the drift fix
            Assert.That(v3.VoiceId, Is.EqualTo("vid-oliver"));
            Assert.That(v3.Label, Is.EqualTo("Oliver Silk · v3"));
        });

        var v2 = svc.VoiceProfiles.Single(p => p.Id == "vox-oliver-silk-v2");
        Assert.Multiple(() =>
        {
            Assert.That(v2.Model, Is.EqualTo("eleven_multilingual_v2"));
            Assert.That(v2.Stability, Is.EqualTo(0.5));         // factory default
            Assert.That(v2.VoiceId, Is.EqualTo("vid-oliver"));
        });
    }

    [Test]
    public void ImportAllVoices_IsIdempotent_NoDuplicates()
    {
        svc.ImportAllVoicesAsProfiles(TwoVoices);
        svc.ImportAllVoicesAsProfiles(TwoVoices);

        Assert.That(svc.VoiceProfiles, Has.Count.EqualTo(4));
    }

    [Test]
    public void ImportAllVoices_PreservesUserTunedCopies()
    {
        svc.ImportAllVoicesAsProfiles(TwoVoices);
        // Simulate a copy-on-save tweak: a fresh-id profile the studio created.
        var tuned = svc.UpsertVoiceProfile(new StreetSamurai.Core.Models.VoiceProfile
        {
            Label = "Oliver gravelly", VoiceId = "vid-oliver", Model = "eleven_v3", Stability = 0.5,
        });

        svc.ImportAllVoicesAsProfiles(TwoVoices); // re-import must not touch the tuned copy

        var stillThere = svc.VoiceProfiles.SingleOrDefault(p => p.Id == tuned.Id);
        Assert.That(stillThere, Is.Not.Null);
        Assert.That(stillThere!.Stability, Is.EqualTo(0.5));
        Assert.That(svc.VoiceProfiles, Has.Count.EqualTo(5));
    }

    [Test]
    public void ImportAllVoices_SkipsVoicesWithNoId()
    {
        var n = svc.ImportAllVoicesAsProfiles([new TtsVoice { VoiceId = "", Name = "Bad" }]);
        Assert.That(n, Is.EqualTo(0));
        Assert.That(svc.VoiceProfiles, Is.Empty);
    }
}
