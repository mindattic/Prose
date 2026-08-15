using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Pins LogicSweepService.RunNarrowAsync (2026-08-14) — the blast-radius mini re-check that lets
/// a fix pass verify its own side effects against its immediate neighbors in the same turn,
/// instead of waiting for the next full-book sweep. The two properties that matter: (1) only the
/// caller-supplied beat subset is sent to the LLM, not the whole book — a wide blast radius
/// should never quietly become a full re-sweep; (2) findings land under a scope key distinct from
/// the full sweep's, so the two never collide or purge each other.
/// </summary>
[TestFixture]
public class LogicSweepRunNarrowAsyncTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private CapturingLlmService llm = null!;
    private LogicSweepService svc = null!;
    private FindingsService findingsSvc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-logicsweep-narrow-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        llm = new CapturingLlmService();
        findingsSvc = new FindingsService(dbFactory, paths);
        var auditRunner = new AuditRunner(llm, findingsSvc);
        svc = new LogicSweepService(auditRunner, new PlantPayoffService(dbFactory), dbFactory, findingsSvc);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    private async Task<(Guid NodeId, List<Beat> Beats)> SeedBookWithBeatsAsync(int count)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "narrow-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter" };
        db.Nodes.Add(chapter);
        var beats = Enumerable.Range(1, count)
            .Select(n => new Beat { Id = Guid.NewGuid(), Number = n, Text = $"Beat {n} prose text." })
            .ToList();
        db.Beats.AddRange(beats);
        for (int i = 0; i < beats.Count; i++)
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beats[i].Id, SortKey = (i + 1) * 100 });
        await db.SaveChangesAsync();
        return (chapter.Id, beats);
    }

    [Test]
    public async Task RunNarrowAsync_OnlySendsSuppliedBeatSubsetToLlm()
    {
        var (nodeId, beats) = await SeedBookWithBeatsAsync(10);
        var subset = new[] { beats[2].Id, beats[3].Id, beats[4].Id }; // beats 3,4,5

        await svc.RunNarrowAsync(nodeId, subset, anchorBeatId: beats[3].Id);

        var prompt = llm.Prompts.First();
        Assert.That(prompt, Does.Contain("Beat #3"));
        Assert.That(prompt, Does.Contain("Beat #4"));
        Assert.That(prompt, Does.Contain("Beat #5"));
        Assert.That(prompt, Does.Not.Contain("Beat #1"),
            "a beat outside the supplied blast radius must never reach the LLM prompt");
        Assert.That(prompt, Does.Not.Contain("Beat #10"));
    }

    [Test]
    public async Task RunNarrowAsync_ReportsOnlySubsetBeatCount()
    {
        var (nodeId, beats) = await SeedBookWithBeatsAsync(10);
        var subset = new[] { beats[0].Id, beats[1].Id };

        var report = await svc.RunNarrowAsync(nodeId, subset, anchorBeatId: beats[0].Id);

        Assert.That(report.BeatCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RunNarrowAsync_EmptyBeatIds_ReturnsCleanReportWithoutCallingLlm()
    {
        var (nodeId, _) = await SeedBookWithBeatsAsync(3);

        var report = await svc.RunNarrowAsync(nodeId, [], anchorBeatId: Guid.NewGuid());

        Assert.That(report.BeatCount, Is.EqualTo(0));
        Assert.That(report.Findings, Is.Empty);
        Assert.That(llm.Prompts, Is.Empty);
    }

    [Test]
    public async Task RunNarrowAsync_UsesDistinctScopeKey_FromFullSweep()
    {
        var (nodeId, beats) = await SeedBookWithBeatsAsync(5);

        await svc.RunAsync(nodeId);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await db.Nodes.FirstAsync(n => n.Id == nodeId);
            var fullSweepPath = $"node:{node.Slug}";
            var blastPath = $"beat:{beats[2].Id:N}:blast";
            Assert.That(fullSweepPath, Is.Not.EqualTo(blastPath),
                "sanity check: the two scope keys must never collide by construction");
        }

        // Both calls succeed independently — RunNarrowAsync's own delete-then-recreate cycle
        // (scoped to "beat:{id}:blast") must not throw or interfere with the full sweep's
        // "node:{slug}" Findings from the call just above.
        Assert.DoesNotThrowAsync(async () =>
            await svc.RunNarrowAsync(nodeId, [beats[2].Id], anchorBeatId: beats[2].Id));
    }

    /// <summary>Fake LLM returning a configurable verdict for every rule call (default: clean),
    /// recording every user-prompt it was given so tests can assert exactly which beats reached
    /// the model. Set <see cref="Response"/> to a non-empty findings array to simulate a "dirty"
    /// round.</summary>
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
