using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to <see cref="BeatAuditService"/>'s total-lens-failure
/// path. When all three story lenses (causality, affect→behavior, interpersonal) failed to reach
/// the LLM, the service returned <c>IsClean: true</c> — "audit clean, no blockers" — even though
/// zero evidence was ever gathered. <c>AutoRunCli</c> acted on that literally: it printed "audit
/// clean — no blockers" and silently skipped the self-repair pass this service exists to run,
/// during exactly the compounding-errors scenario (a total LLM outage) its own class doc comment
/// warns against. Same defect family as the BookAuditService.GatewayReady and
/// StoryScopeAuditService.Ready fixes earlier this session — "the check never ran" must never be
/// indistinguishable from "the check ran and found nothing."
/// </summary>
[TestFixture]
public class BeatAuditServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private int beatNumber;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-beataudit-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private sealed class ThrowingLlmService : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            throw new InvalidOperationException(
                "400 Bad Request: Your credit balance is too low to access the Anthropic API.");
    }

    private async Task<Guid> SeedNodeWithBeatAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = id;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = "Some prose to audit." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = id, BeatId = beat.Id, SortKey = 1, IsEnabled = true });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task AuditAsync_AllThreeLensesFail_IsNotReportedClean()
    {
        var throwingLlm = new ThrowingLlmService();
        var findings = new FindingsService(dbFactory, paths);
        var causality = new CausalityService(throwingLlm, findings, dbFactory, NullLogger<CausalityService>.Instance);
        var affect = new AffectBehaviorService(throwingLlm, findings, dbFactory, NullLogger<AffectBehaviorService>.Instance);
        var interpersonal = new InterpersonalDynamicsService(throwingLlm, findings, dbFactory, NullLogger<InterpersonalDynamicsService>.Instance);
        var svc = new BeatAuditService(causality, affect, interpersonal, NullLogger<BeatAuditService>.Instance);

        var nodeId = await SeedNodeWithBeatAsync();

        var result = await svc.AuditAsync(nodeId);

        Assert.That(result.FailedLensCount, Is.EqualTo(3));
        Assert.That(result.IsClean, Is.False,
            "a beat whose audit lenses could not run at all must never be reported as a clean pass");
    }
}
