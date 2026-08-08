using NUnit.Framework;
using Prose.Cli;

namespace Prose.UnitTests;

/// <summary>
/// Guards the argv-dispatch rule for <c>--universe</c>.
///
/// Why this is guarded by tests: <c>--universe</c> is overloaded. It is both the name of a
/// management command (<c>prose --universe list</c>) and the universe-scoping flag that nearly every
/// other command accepts (<c>prose --universe source --export-node --slug x</c>). Program.cs used to
/// claim dispatch on <c>args[0] == "--universe"</c> alone, which meant a scoped invocation in
/// first position was swallowed: UniverseCli printed its usage text, the real command never ran,
/// and the process returned as though nothing was wrong. A silently skipped export looks identical
/// to a successful no-op, which is the worst possible failure mode for a content pipeline.
///
/// The dispatch guard now additionally requires args[1] to name a real subcommand, and that
/// decision routes through <see cref="UniverseCli.IsSubcommand"/> so the guard and the switch
/// inside <see cref="UniverseCli.RunAsync"/> cannot drift apart.
/// </summary>
[TestFixture]
public class UniverseCliDispatchTests
{
    /// <summary>
    /// Mirrors <c>isUniverseManagementCommand</c> in Program.cs. Program.cs uses this single
    /// predicate for two decisions — whether to dispatch to UniverseCli, and whether to SKIP
    /// parsing the next token as a universe slug — so both are covered by these tests.
    /// </summary>
    private static bool IsUniverseManagementCommand(params string[] args) =>
        args.Length > 0 && args[0] == "--universe"
        && (args.Length == 1 || UniverseCli.IsSubcommand(args[1]));

    /// <summary>Program.cs parses <c>--universe &lt;slug&gt;</c> into RequestedSlug only when this is NOT a management command.</summary>
    private static bool WouldParseNextTokenAsSlug(params string[] args) => !IsUniverseManagementCommand(args);

    private static bool WouldDispatchToUniverseCli(params string[] args) => IsUniverseManagementCommand(args);

    [TestCase("list")]
    [TestCase("current")]
    [TestCase("use")]
    public void IsSubcommand_RecognizesEveryRealSubcommand(string sub)
    {
        Assert.That(UniverseCli.IsSubcommand(sub), Is.True,
            $"'{sub}' is handled by UniverseCli.RunAsync, so the dispatch guard must recognize it.");
    }

    [TestCase("LIST")]
    [TestCase("Current")]
    [TestCase("USE")]
    public void IsSubcommand_IsCaseInsensitive(string sub)
    {
        Assert.That(UniverseCli.IsSubcommand(sub), Is.True);
    }

    [TestCase("source")]   // a universe slug — the flag usage that regressed
    [TestCase("glmz")]
    [TestCase("scry")]
    [TestCase("--slug")]
    [TestCase("")]
    [TestCase(null)]
    public void IsSubcommand_RejectsNonSubcommands(string? token)
    {
        Assert.That(UniverseCli.IsSubcommand(token), Is.False,
            $"'{token ?? "(null)"}' is not a universe subcommand; treating it as one swallows the real command.");
    }

    // ── the regression this file exists for ──────────────────────────────────

    [Test]
    public void ScopingFlagInFirstPosition_DoesNotHijackDispatch()
    {
        // prose --universe source --export-node --slug resistance-...
        Assert.That(
            WouldDispatchToUniverseCli("--universe", "source", "--export-node", "--slug", "resistance-x"),
            Is.False,
            "A scoped export must reach --export-node. Claiming dispatch here silently skipped the export.");
    }

    [Test]
    public void ScopingFlagInFirstPosition_StillDoesNotHijack_ForOtherCommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WouldDispatchToUniverseCli("--universe", "source", "--generate-node-doc", "--slug", "x"), Is.False);
            Assert.That(WouldDispatchToUniverseCli("--universe", "source", "--generate-blueprint", "--slug", "x"), Is.False);
            Assert.That(WouldDispatchToUniverseCli("--universe", "glmz", "--beat", "list", "--node", "x"), Is.False);
        });
    }

    [Test]
    public void RealSubcommands_StillDispatchToUniverseCli()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WouldDispatchToUniverseCli("--universe", "list"), Is.True);
            Assert.That(WouldDispatchToUniverseCli("--universe", "current"), Is.True);
            Assert.That(WouldDispatchToUniverseCli("--universe", "use", "--slug", "source"), Is.True);
        });
    }

    [Test]
    public void BareUniverseFlag_DispatchesSoUsageIsPrinted()
    {
        // Nothing follows it, so there is nothing to scope and nothing else to run. Dispatch so
        // UniverseCli prints its usage, rather than falling through the entire chain and exiting
        // silently with no output at all.
        Assert.That(WouldDispatchToUniverseCli("--universe"), Is.True);
    }

    // ── the slug-parsing half of the same predicate ──────────────────────────

    [Test]
    public void ManagementVerbs_AreNotParsedAsUniverseSlugs()
    {
        // Regression: ParseSlug takes the token after --universe verbatim, so `--universe current`
        // set RequestedSlug="current" and the process died in service construction with
        // "Unknown universe slug 'current'" before UniverseCli could print anything.
        Assert.Multiple(() =>
        {
            Assert.That(WouldParseNextTokenAsSlug("--universe", "current"), Is.False);
            Assert.That(WouldParseNextTokenAsSlug("--universe", "list"), Is.False);
            Assert.That(WouldParseNextTokenAsSlug("--universe", "use", "--slug", "source"), Is.False);
        });
    }

    [Test]
    public void RealSlugs_AreStillParsedAsUniverseSlugs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WouldParseNextTokenAsSlug("--universe", "source", "--export-node", "--slug", "x"), Is.True);
            Assert.That(WouldParseNextTokenAsSlug("--export-node", "--slug", "x", "--universe", "source"), Is.True);
        });
    }

    [Test]
    public void UniverseFlagLaterInArgv_NeverDispatches()
    {
        // The long-standing rule: only args[0] can be the command.
        Assert.That(WouldDispatchToUniverseCli("--export-node", "--slug", "x", "--universe", "source"), Is.False);
        Assert.That(WouldDispatchToUniverseCli("--beat", "list", "--universe", "list"), Is.False);
    }
}
