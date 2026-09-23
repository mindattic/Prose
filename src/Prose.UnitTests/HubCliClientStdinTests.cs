using Prose.Cli;

namespace Prose.UnitTests;

/// <summary>
/// A forwarded CLI command is sent stdin only when it reads stdin (2026-09-23). Reading it for
/// every command blocked forever whenever the caller's stdin was a pipe that never closes, so the
/// command never reached the Hub: three silent five-minute hangs in one session.
/// </summary>
[TestFixture]
public class HubCliClientStdinTests
{
    [TestCase("ImportMarkdownCli", new[] { "--import-md", "--file", "-" }, true)]
    [TestCase("ReimportNodeCli", new[] { "--reimport-node", "--file", "-" }, true)]
    [TestCase("BeatCli", new[] { "--beat", "set-text", "--number", "7", "--text", "-" }, true)]
    [TestCase("DocContextHookCli", new string[0], true)]
    [TestCase("ReadBeatsCli", new[] { "--read-beats", "--slug", "bushido-coda", "--mark-read" }, false)]
    [TestCase("FactoryCli", new[] { "--order", "add", "--title", "a - b", "--detail", "x — y" }, false)]
    [TestCase("SpliceBeatsCli", new[] { "--splice-beats", "--node", "x", "--file", "docket.json" }, false)]
    public void Only_a_command_that_reads_stdin_is_sent_it(string handler, string[] args, bool expected) =>
        Assert.That(HubCliClient.CommandReadsStdin(handler, args), Is.EqualTo(expected));
}
