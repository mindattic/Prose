using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Integration tests for AuditRunner.RunAsync / WriteFindingsForRules against a real (SQLite
/// in-memory via TestDbFactory) FindingsService — the DB-writing half of AuditRunner that
/// AuditRunnerTests.cs (pure helpers only) doesn't cover. Uses IDeterministicAuditRule fakes so
/// no LLM call is needed; the whole point of this file is proving the delete-then-recreate
/// Findings lifecycle the class doc comment describes: "a rule whose violation count drops to
/// zero doesn't leave orphaned rows behind forever."
/// </summary>
[TestFixture]
public class AuditRunnerPersistenceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FindingsService findings = null!;
    private AuditRunner runner = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-auditrunner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "findings");
        findings = new FindingsService(dbFactory, paths);
        runner = new AuditRunner(new FakeLlmService(), findings);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private static AuditContext MakeContext(Guid nodeId) =>
        new(nodeId, Guid.Empty, "", [], new Dictionary<string, object?>());

    private sealed class FakeRule(string key, Func<IReadOnlyList<AuditVerdict>> verdicts) : IDeterministicAuditRule
    {
        public string Key => key;
        public string Title => key;
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct) =>
            Task.FromResult(verdicts());
    }

    private sealed class ThrowingRule(string key) : IDeterministicAuditRule
    {
        public string Key => key;
        public string Title => key;
        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct) =>
            throw new InvalidOperationException("Circuit breaker open for provider 'claude-api'.");
    }

    [Test]
    public async Task RunAsync_WritesFindingsForNonPassVerdicts()
    {
        var nodeId = Guid.NewGuid();
        var rule = new FakeRule("causality", () =>
        [
            new AuditVerdict("causality", "Causality chain", "BLOCKER", "beat 3 has no cause", "loc-3"),
            new AuditVerdict("causality", "Causality chain", "MODERATE", "beat 7 is weak", "loc-7"),
        ]);

        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [rule], MakeContext(nodeId));

        var rows = findings.ListByFilePathPrefix($"node:{nodeId}");
        Assert.That(rows, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task RunAsync_PassVerdict_WritesNoFinding()
    {
        var nodeId = Guid.NewGuid();
        var rule = new FakeRule("causality", () => [new AuditVerdict("causality", "Causality chain", "PASS", "all clear")]);

        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [rule], MakeContext(nodeId));

        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Is.Empty);
    }

    [Test]
    public async Task RunAsync_SecondRunWithFewerViolations_DeletesStaleRows()
    {
        // This is the exact bug the class doc comment says WriteFindingsForRules exists to fix:
        // a rule that used to fire on N things and now fires on fewer must not leave the
        // difference behind as permanently stale rows.
        var nodeId = Guid.NewGuid();
        var verdictCount = 3;
        var rule = new FakeRule("orphan_refs", () =>
            Enumerable.Range(0, verdictCount)
                .Select(i => new AuditVerdict("orphan_refs", "Orphan references", "MODERATE", $"orphan {i}", $"loc-{i}"))
                .ToList());

        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [rule], MakeContext(nodeId));
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Has.Count.EqualTo(3), "first run: 3 orphans found");

        verdictCount = 0; // the rule now reports clean
        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [rule], MakeContext(nodeId));

        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Is.Empty,
            "second run must delete the 3 stale rows, not leave them behind forever");
    }

    [Test]
    public async Task RunAsync_ClearingOneRulesFindings_DoesNotTouchAnotherRulesRow()
    {
        // End-to-end version of the pure '@'-boundary tests in AuditRunnerTests.cs — proves
        // "causality" and "causality_2" don't collide against real DB rows.
        var nodeId = Guid.NewGuid();
        var ruleAFires = true;
        var ruleA = new FakeRule("causality", () =>
            ruleAFires ? [new AuditVerdict("causality", "Causality chain", "BLOCKER", "a")] : []);
        var ruleB = new FakeRule("causality_2", () => [new AuditVerdict("causality_2", "Causality chain 2", "BLOCKER", "b")]);

        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [ruleA, ruleB], MakeContext(nodeId));
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Has.Count.EqualTo(2));

        // Re-run with only ruleA, now clean — ruleB's row (not part of this call's rule set) must survive.
        ruleAFires = false;
        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [ruleA], MakeContext(nodeId));

        var remaining = findings.ListByFilePathPrefix($"node:{nodeId}");
        Assert.That(remaining, Has.Count.EqualTo(1), "clearing ruleA's findings must not touch ruleB's");
        Assert.That(remaining[0].Summary, Does.Contain("causality_2@"));
    }

    [Test]
    public async Task RunAsync_WriteFindingsFalse_PersistsNothing()
    {
        var nodeId = Guid.NewGuid();
        var rule = new FakeRule("causality", () => [new AuditVerdict("causality", "Causality chain", "BLOCKER", "a")]);

        var verdicts = await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction,
            [rule], MakeContext(nodeId), writeFindings: false);

        Assert.That(verdicts, Has.Count.EqualTo(1), "verdicts are still returned");
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Is.Empty, "but nothing is persisted");
    }

    [Test]
    public async Task DeleteAllForRule_ClearsFindingsAcrossEveryNodeScope()
    {
        var nodeA = Guid.NewGuid();
        var nodeB = Guid.NewGuid();
        var rule = new FakeRule("retired_rule", () => [new AuditVerdict("retired_rule", "Retired Rule", "MODERATE", "x")]);

        await runner.RunAsync("LOGICSWEEP", $"node:{nodeA}", FindingCategory.Contradiction, [rule], MakeContext(nodeA));
        await runner.RunAsync("LOGICSWEEP", $"node:{nodeB}", FindingCategory.Contradiction, [rule], MakeContext(nodeB));
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeA}"), Has.Count.EqualTo(1));
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeB}"), Has.Count.EqualTo(1));

        runner.DeleteAllForRule("LOGICSWEEP", "retired_rule");

        Assert.That(findings.ListByFilePathPrefix($"node:{nodeA}"), Is.Empty, "retired rule's findings cleared from node A");
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeB}"), Is.Empty, "retired rule's findings cleared from node B too");
    }

    [Test]
    public async Task RunAsync_RuleThrowsOnSecondRun_DoesNotWipePriorRealFindings()
    {
        // The 2026-08-09 fix: a rule that threw used to be treated identically to a rule that
        // legitimately found nothing — the unconditional delete-then-recreate lifecycle deleted
        // the 2 real findings below and replaced them with a single meaningless "Evaluation
        // failed" row, permanently losing real signal to a transient provider outage.
        var nodeId = Guid.NewGuid();
        var key = "acronym_after_term";
        var rule = new FakeRule(key, () =>
        [
            new AuditVerdict(key, "Acronym after term", "MODERATE", "GLMZ used with no gloss", "loc-1"),
            new AuditVerdict(key, "Acronym after term", "MODERATE", "ISB used with no gloss", "loc-2"),
        ]);
        await runner.RunAsync("BOOKAUDIT", $"node:{nodeId}", FindingCategory.BookAudit, [rule], MakeContext(nodeId));
        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Has.Count.EqualTo(2), "first run: 2 real findings");

        var throwingRule = new ThrowingRule(key);
        var verdicts = await runner.RunAsync("BOOKAUDIT", $"node:{nodeId}", FindingCategory.BookAudit, [throwingRule], MakeContext(nodeId));

        Assert.That(findings.ListByFilePathPrefix($"node:{nodeId}"), Has.Count.EqualTo(2),
            "a transient evaluation failure must leave the prior real findings untouched, not delete them");
        Assert.That(verdicts, Has.Count.EqualTo(1).And.Some.Matches<AuditVerdict>(v => v.Evidence.Contains("Evaluation failed")),
            "the failure is still visible to the caller (e.g. CLI console output) even though it isn't persisted");
    }

    [Test]
    public async Task RunAsync_MultipleBeatsFromSameRule_EachGetsOwnFindingRow()
    {
        var nodeId = Guid.NewGuid();
        var rule = new FakeRule("timeline", () =>
        [
            new AuditVerdict("timeline", "Timeline", "BLOCKER", "impossible date at beat 2", "beat-2"),
            new AuditVerdict("timeline", "Timeline", "BLOCKER", "impossible date at beat 9", "beat-9"),
        ]);

        await runner.RunAsync("LOGICSWEEP", $"node:{nodeId}", FindingCategory.Contradiction, [rule], MakeContext(nodeId));

        var rows = findings.ListByFilePathPrefix($"node:{nodeId}");
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows.Select(r => r.Summary), Has.One.Contains("beat-2"));
        Assert.That(rows.Select(r => r.Summary), Has.One.Contains("beat-9"));
    }
}
