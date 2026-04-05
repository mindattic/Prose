namespace StreetSamurai.UnitTests;

using StreetSamurai.Core.Services;

[TestFixture]
public class SettingsServiceTests
{
    private SettingsService svc = null!;

    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MindAttic", "StreetSamurai");

    [SetUp]
    public void Setup()
    {
        // Clear any saved defaults snapshot from prior tests
        var defaultsPath = Path.Combine(AppDataDir, "Defaults.json");
        if (File.Exists(defaultsPath)) File.Delete(defaultsPath);
        svc = new SettingsService();
    }

    [TearDown]
    public void Teardown()
    {
        // Reset settings file so tests don't leak state
        svc.ResetToDefaults();
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
        var fresh = new SettingsService();
        Assert.That(fresh.TimestampFormat, Is.EqualTo("yyyy-MM-dd hh:mm:sstt"));
    }

    [Test]
    public void Defaults_FontFamily_IsOutfit()
    {
        var fresh = new SettingsService();
        Assert.That(fresh.FontFamily, Is.EqualTo("Outfit"));
    }

    [Test]
    public void Defaults_ActiveLlmProvider_IsClaude()
    {
        var fresh = new SettingsService();
        Assert.That(fresh.ActiveLlmProvider, Is.EqualTo("claude"));
    }

    [Test]
    public void Defaults_MaxTokens_Is4096()
    {
        var fresh = new SettingsService();
        Assert.That(fresh.MaxTokens, Is.EqualTo(4096));
    }

    [Test]
    public void Defaults_EditorFontSize_Is14()
    {
        var fresh = new SettingsService();
        Assert.That(fresh.EditorFontSize, Is.EqualTo(14));
    }

    // ── SaveAsDefaults / ResetToDefaults ──────────────────────

    [Test]
    public void ResetToDefaults_WithNoSnapshot_ReturnsFactoryDefaults()
    {
        svc.MaxTokens = 999;
        svc.ApiKey = "sk-test-key";
        svc.ResetToDefaults();
        Assert.That(svc.MaxTokens, Is.EqualTo(4096));
        Assert.That(svc.ApiKey, Is.EqualTo(""));
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
}
