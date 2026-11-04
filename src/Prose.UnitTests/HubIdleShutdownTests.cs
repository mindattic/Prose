using Microsoft.Extensions.Configuration;
using Prose.Hub;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T129: the Hub stops itself once no Prose session has been open for the idle window,
/// and never while a client process, an in-flight request or a DCM run is still there.
/// </summary>
[TestFixture]
public class HubIdleShutdownTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ShutsDown_WhenEverySignalHasBeenIdleForTheWholeWindow() =>
        Assert.That(HubIdleShutdown.ShouldShutDown(Window, false, 0, false, Now - Window, Now), Is.True);

    [Test]
    public void StaysUp_BeforeTheWindowHasElapsed() =>
        Assert.That(HubIdleShutdown.ShouldShutDown(Window, false, 0, false, Now - TimeSpan.FromMinutes(4), Now), Is.False);

    [Test]
    public void StaysUp_WhileAProseClientProcessIsRunning() =>
        Assert.That(HubIdleShutdown.ShouldShutDown(Window, true, 0, false, Now - TimeSpan.FromHours(1), Now), Is.False);

    [Test]
    public void StaysUp_WhileARequestIsInFlight() =>
        Assert.That(HubIdleShutdown.ShouldShutDown(Window, false, 1, false, Now - TimeSpan.FromHours(1), Now), Is.False);

    [Test]
    public void StaysUp_WhileADcmRunIsActive() =>
        Assert.That(HubIdleShutdown.ShouldShutDown(Window, false, 0, true, Now - TimeSpan.FromHours(1), Now), Is.False);

    [Test]
    public void ZeroWindow_DisablesIdleShutdown() =>
        Assert.That(HubIdleShutdown.ShouldShutDown(TimeSpan.Zero, false, 0, false, Now - TimeSpan.FromDays(1), Now), Is.False);

    [TestCase(null, 5)]
    [TestCase("12", 12)]
    [TestCase("0", 0)]
    [TestCase("-3", 0)]
    public void ReadWindow_DefaultsToFiveMinutes_AndClampsNegativeToOff(string? configured, int expectedMinutes)
    {
        var values = new Dictionary<string, string?>();
        if (configured != null) values[HubIdleShutdown.ConfigKey] = configured;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        Assert.That(HubIdleShutdown.ReadWindow(config), Is.EqualTo(TimeSpan.FromMinutes(expectedMinutes)));
    }

    [Test]
    public void ClientProcesses_IncludeTheMcpServerEveryClaudeSessionRuns() =>
        Assert.That(HubIdleShutdown.ClientProcessNames, Does.Contain("Prose.Mcp").And.Contain("Writer"));
}
