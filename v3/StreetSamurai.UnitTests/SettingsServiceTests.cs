namespace StreetSamurai.UnitTests;

using StreetSamurai.Core.Services;

[TestFixture]
public class SettingsServiceTests
{
    private SettingsService svc = null!;

    [SetUp]
    public void Setup() => svc = new SettingsService();

    // ── FormatTimestamp ─────────────────────────────────────

    [Test]
    public void FormatTimestamp_12h_ContainsAmPm()
    {
        svc.TimeFormat = "12h";
        svc.TimezoneId = "Central Standard Time";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 18, 30, 0, DateTimeKind.Utc));
        Assert.That(result, Does.Contain("PM").Or.Contain("AM"));
    }

    [Test]
    public void FormatTimestamp_24h_NoAmPm()
    {
        svc.TimeFormat = "24h";
        svc.TimezoneId = "UTC";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 18, 30, 0, DateTimeKind.Utc));
        Assert.That(result, Does.Not.Contain("PM"));
        Assert.That(result, Does.Not.Contain("AM"));
    }

    [Test]
    public void FormatTimestamp_IncludesMilliseconds_ByDefault()
    {
        svc.TimeFormat = "12h";
        svc.TimezoneId = "UTC";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 12, 0, 0, 123, DateTimeKind.Utc));
        Assert.That(result, Does.Contain("123"));
    }

    [Test]
    public void FormatTimestamp_ExcludeMilliseconds_NoFraction()
    {
        svc.TimeFormat = "12h";
        svc.TimezoneId = "UTC";
        var result = svc.FormatTimestamp(new DateTime(2026, 4, 5, 12, 0, 0, 123, DateTimeKind.Utc), includeMilliseconds: false);
        Assert.That(result, Does.Not.Contain("123"));
    }

    // ── Defaults ────────────────────────────────────────────

    [Test]
    public void Defaults_TimeFormat_Is12h()
    {
        var fresh = new SettingsService();
        Assert.That(fresh.TimeFormat, Is.EqualTo("12h"));
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

    // ── ResetToDefaults ─────────────────────────────────────

    [Test]
    public void ResetToDefaults_PreservesApiKey()
    {
        svc.ApiKey = "sk-test-key";
        svc.MaxTokens = 999;
        svc.ResetToDefaults();
        Assert.That(svc.ApiKey, Is.EqualTo("sk-test-key"));
        Assert.That(svc.MaxTokens, Is.EqualTo(4096));
    }

    [Test]
    public void ResetToDefaults_RestoresMaxTokens()
    {
        svc.MaxTokens = 999;
        svc.ResetToDefaults();
        Assert.That(svc.MaxTokens, Is.EqualTo(4096));
    }

    [Test]
    public void ResetToDefaults_PreservesCanonRootPath()
    {
        svc.CanonRootPath = @"D:\Custom\Path";
        svc.ResetToDefaults();
        Assert.That(svc.CanonRootPath, Is.EqualTo(@"D:\Custom\Path"));
    }
}
