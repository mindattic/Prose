using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// SS-A44 voting kill-switch. Verifies the central gate blocks ballot/score
/// flows by default, lifts on an explicit override, correctly reads the
/// legion.json default, and — critically — that prose-generation paths never
/// depend on the gate (generation must keep working when voting is off).
/// </summary>
[TestFixture]
public class VotingGateTests
{
    // ── (a) Blocks by default, with the exact SS-A44 message ──────────────────

    [Test]
    public void EnsureAllowed_Disabled_NoOverride_Throws_WithExactMessage()
    {
        var gate = new VotingGate(votingEnabledByDefault: false);

        var ex = Assert.Throws<VotingDisabledException>(
            () => gate.EnsureAllowed("review-node", explicitOverride: false));

        Assert.That(ex!.Message, Is.EqualTo(
            "Voting is disabled by default (SS-A44). Pass --allow-votes (CLI) / allowVotes:true (MCP) to run this explicitly."));
        // The public constant and the thrown message must stay in lockstep.
        Assert.That(ex.Message, Is.EqualTo(VotingGate.DisabledMessage));
    }

    [Test]
    public void IsAllowed_Disabled_NoOverride_IsFalse()
    {
        var gate = new VotingGate(votingEnabledByDefault: false);
        Assert.That(gate.IsAllowed(explicitOverride: false), Is.False);
    }

    // ── (b) Explicit override passes; enabled-by-default passes ────────────────

    [Test]
    public void EnsureAllowed_Disabled_WithOverride_DoesNotThrow()
    {
        var gate = new VotingGate(votingEnabledByDefault: false);
        Assert.DoesNotThrow(() => gate.EnsureAllowed("review-node", explicitOverride: true));
        Assert.That(gate.IsAllowed(explicitOverride: true), Is.True);
    }

    [Test]
    public void EnsureAllowed_EnabledByDefault_DoesNotThrow_EvenWithoutOverride()
    {
        var gate = new VotingGate(votingEnabledByDefault: true);
        Assert.DoesNotThrow(() => gate.EnsureAllowed("review-node", explicitOverride: false));
        Assert.That(gate.IsAllowed(explicitOverride: false), Is.True);
    }

    // ── legion.json default resolution ("votingEnabled") ──────────────────────

    [Test]
    public void ReadVotingEnabledDefault_KeyFalse_ReturnsFalse()
    {
        var dir = WriteLegionJson("{ \"votingEnabled\": false, \"voters\": [\"claude-team\"] }");
        try { Assert.That(VotingGate.ReadVotingEnabledDefault(dir), Is.False); }
        finally { CleanUp(dir); }
    }

    [Test]
    public void ReadVotingEnabledDefault_KeyTrue_ReturnsTrue()
    {
        var dir = WriteLegionJson("{ \"votingEnabled\": true, \"voters\": [\"claude-team\"] }");
        try { Assert.That(VotingGate.ReadVotingEnabledDefault(dir), Is.True); }
        finally { CleanUp(dir); }
    }

    [Test]
    public void ReadVotingEnabledDefault_KeyAbsent_ReturnsFalse()
    {
        // File present but the key is missing → OFF (fail-safe).
        var dir = WriteLegionJson("{ \"voters\": [\"claude-team\"], \"judge\": \"claude-team\" }");
        try { Assert.That(VotingGate.ReadVotingEnabledDefault(dir), Is.False); }
        finally { CleanUp(dir); }
    }

    [Test]
    public void ReadVotingEnabledDefault_NoFile_ReturnsFalse()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ss44-none-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { Assert.That(VotingGate.ReadVotingEnabledDefault(dir), Is.False); }
        finally { CleanUp(dir); }
    }

    [Test]
    public void CommittedLegionJson_ShipsVotingDisabled()
    {
        // The repo's committed legion.json must keep voting OFF by default (SS-A44).
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null, "could not locate repo root (legion.json)");
        Assert.That(VotingGate.ReadVotingEnabledDefault(repoRoot!), Is.False,
            "committed legion.json must keep votingEnabled=false");
    }

    // ── (c) Prose generation is NOT gated ─────────────────────────────────────

    [Test]
    public void ProseGenerationServices_DoNotDependOnVotingGate()
    {
        // Representative prose-generation entry points must not take a VotingGate
        // dependency — generation is never gated (SS-A44 gates scoring, not prose).
        foreach (var typeName in new[]
        {
            "Prose.Core.Services.BeatGeneratorService",
            "Prose.Core.Services.ProseWriterRouter",
        })
        {
            var t = typeof(VotingGate).Assembly.GetType(typeName);
            Assert.That(t, Is.Not.Null, $"{typeName} not found");
            var takesGate = t!.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(VotingGate));
            Assert.That(takesGate, Is.False,
                $"{typeName} must not depend on VotingGate — prose generation is not gated.");
        }
    }

    [Test]
    public void BallotSolicitingServices_DependOnVotingGate()
    {
        // The scoring/balloting services MUST take the gate — proving the switch is wired.
        foreach (var typeName in new[]
        {
            "Prose.Core.Services.NodeReviewService",
            "Prose.Core.Services.EntityReviewService",
            "Prose.Core.Services.BookReviewService",
            "Prose.Core.Services.ChapterCloseProcessorService",
        })
        {
            var t = typeof(VotingGate).Assembly.GetType(typeName);
            Assert.That(t, Is.Not.Null, $"{typeName} not found");
            var takesGate = t!.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Any(p => p.ParameterType == typeof(VotingGate));
            Assert.That(takesGate, Is.True,
                $"{typeName} must depend on VotingGate — it solicits LLM ballots/scores.");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string WriteLegionJson(string content)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ss44-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "legion.json"), content);
        return dir;
    }

    private static void CleanUp(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }

    private static string? FindRepoRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (int depth = 0; depth < 12 && current != null; depth++)
        {
            if (File.Exists(Path.Combine(current.FullName, "legion.json")))
                return current.FullName;
            current = current.Parent;
        }
        return null;
    }
}
