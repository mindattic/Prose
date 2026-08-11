using NUnit.Framework;

namespace Prose.UnitTests;

/// <summary>
/// Covers the pure list-swap logic in SetLlmProviderCli (built 2026-08-11 to switch every
/// Settings.json field governing the active Claude credential path — pay-per-token "claude-api"
/// vs. Team-subscription-OAuth "claude-team" — in one command). RunAsync itself touches
/// SettingsService/disk and is exercised via live CLI invocation, not unit-tested here.
/// </summary>
[TestFixture]
public class SetLlmProviderCliTests
{
    private static string Invoke(string csv, string from, string to)
    {
        var method = typeof(Prose.Cli.SetLlmProviderCli).GetMethod(
            "SwapInList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string)method.Invoke(null, [csv, from, to])!;
    }

    [Test]
    public void SwapInList_ReplacesMatchingEntry_PreservesOthers()
    {
        var result = Invoke("claude-api,openai,gemini,deepseek,kimi", "claude-api", "claude-team");
        Assert.That(result, Is.EqualTo("claude-team,openai,gemini,deepseek,kimi"));
    }

    [Test]
    public void SwapInList_NoMatch_ReturnsInputUnchanged()
    {
        var result = Invoke("openai,gemini", "claude-api", "claude-team");
        Assert.That(result, Is.EqualTo("openai,gemini"));
    }

    [Test]
    public void SwapInList_SingleValueList_Swaps()
    {
        var result = Invoke("claude-api", "claude-api", "claude-team");
        Assert.That(result, Is.EqualTo("claude-team"));
    }

    [Test]
    public void SwapInList_MultipleMatches_SwapsAll()
    {
        var result = Invoke("claude-api,claude-api", "claude-api", "claude-team");
        Assert.That(result, Is.EqualTo("claude-team,claude-team"));
    }

    [Test]
    public void SwapInList_EmptyString_ReturnsUnchanged()
    {
        var result = Invoke("", "claude-api", "claude-team");
        Assert.That(result, Is.EqualTo(""));
    }
}
