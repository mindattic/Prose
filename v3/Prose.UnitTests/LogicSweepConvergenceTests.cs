using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Pins LogicSweepService.RunConvergenceRoundAsync (2026-08-14) — the "loop-until-dry"
/// convergence criterion this fix replaces "run the sweep N times" with. Directly responds to
/// the observed failure mode: 5 sweep rounds run on VIGL, a 6th independent round still found a
/// new continuity error. The fix isn't "run more rounds" — it's "stop counting rounds and start
/// counting consecutive CLEAN rounds," persisted so the campaign survives across sessions.
/// </summary>
[TestFixture]
public class LogicSweepConvergenceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private CapturingLlmService llm = null!;
    private LogicSweepService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-convergence-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        llm = new CapturingLlmService();
        var findingsSvc = new FindingsService(dbFactory, paths);
        var auditRunner = new AuditRunner(llm, findingsSvc);
        svc = new LogicSweepService(auditRunner, new PlantPayoffService(dbFactory), dbFactory, findingsSvc);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    private async Task<Guid> SeedBookAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "conv-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter" };
        db.Nodes.Add(chapter);
        var beat = new Beat { Id = Guid.NewGuid(), Number = 1, Text = "Some prose." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beat.Id, SortKey = 100 });
        await db.SaveChangesAsync();
        return chapter.Id;
    }

    private const string OneFinding =
        """[{"beat_number":1,"severity":"MODERATE","evidence":"a real problem","fix":"fix it"}]""";

    [Test]
    public async Task FirstCleanRound_NotYetConverged()
    {
        var nodeId = await SeedBookAsync();
        llm.Response = "[]";

        var result = await svc.RunConvergenceRoundAsync(nodeId);

        Assert.That(result.Skipped, Is.False);
        Assert.That(result.Converged, Is.False, "one clean round alone is not enough — needs 2 in a row");
        Assert.That(result.ConsecutiveDryRounds, Is.EqualTo(1));
    }

    [Test]
    public async Task TwoConsecutiveCleanRounds_Converges()
    {
        var nodeId = await SeedBookAsync();
        llm.Response = "[]";

        await svc.RunConvergenceRoundAsync(nodeId);
        var second = await svc.RunConvergenceRoundAsync(nodeId);

        Assert.That(second.Converged, Is.True);
        Assert.That(second.ConsecutiveDryRounds, Is.EqualTo(2));
    }

    [Test]
    public async Task ConvergedThenNothingChanged_NextCallSkipsWithoutCallingLlm()
    {
        var nodeId = await SeedBookAsync();
        llm.Response = "[]";
        await svc.RunConvergenceRoundAsync(nodeId);
        await svc.RunConvergenceRoundAsync(nodeId); // converges here
        var promptCountAtConvergence = llm.Prompts.Count;

        var third = await svc.RunConvergenceRoundAsync(nodeId);

        Assert.That(third.Skipped, Is.True);
        Assert.That(third.Converged, Is.True);
        Assert.That(llm.Prompts.Count, Is.EqualTo(promptCountAtConvergence),
            "an already-converged book with unchanged content must not trigger another LLM sweep");
    }

    [Test]
    public async Task DirtyRoundAfterCleanRound_ResetsConsecutiveDryRounds()
    {
        var nodeId = await SeedBookAsync();
        llm.Response = "[]";
        var first = await svc.RunConvergenceRoundAsync(nodeId);
        Assert.That(first.ConsecutiveDryRounds, Is.EqualTo(1));

        llm.Response = OneFinding;
        var second = await svc.RunConvergenceRoundAsync(nodeId);

        Assert.That(second.Converged, Is.False);
        Assert.That(second.ConsecutiveDryRounds, Is.EqualTo(0),
            "a round that finds something resets the streak — a fix pass is itself a source of risk");
    }

    [Test]
    public async Task RepeatedDirtyRounds_HitsSafetyCapAndFilesFinding()
    {
        var nodeId = await SeedBookAsync();
        llm.Response = OneFinding;

        ConvergenceRoundResult? last = null;
        for (var i = 0; i < LogicSweepService.DefaultMaxTotalRounds; i++)
            last = await svc.RunConvergenceRoundAsync(nodeId);

        Assert.That(last!.HitSafetyCap, Is.True);
        Assert.That(last.Converged, Is.False);
        Assert.That(last.ConsecutiveDryRounds, Is.EqualTo(0), "counters reset once the cap escalates");

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking().FirstAsync(n => n.Id == nodeId);
        var findings = new FindingsService(dbFactory, paths).ListByFilePathPrefix($"node:{node.Slug}");
        Assert.That(findings.Any(f => f.Summary.Contains("LOGICSWEEP-CONVERGENCE [not-converging]")), Is.True,
            "hitting the safety cap without converging must be surfaced as its own finding, not silently looped forever");
    }

    /// <summary>Fake LLM returning a configurable verdict for every rule call.</summary>
    private sealed class CapturingLlmService : ILlmService
    {
        public ConcurrentBag<string> Prompts { get; } = new();
        public string Response { get; set; } = "[]";

        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Prompts.Add(user);
            return Task.FromResult(Response);
        }
    }
}
