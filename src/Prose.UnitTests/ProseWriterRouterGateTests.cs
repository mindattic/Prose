using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for the locked-pipeline gate added to <see cref="ProseWriterRouter.WriteAsync"/>
/// 2026-09-01 (CLAUDE.md "New Story Workflow — LOCKED PIPELINE" — previously documentation-only,
/// nothing in code stopped prose generation for a book that never had an outline or structural
/// blueprint). The gate runs first, before any enrichment, so these tests can use a minimal
/// router (real <c>dbFactory</c>, everything else <c>null!</c> — matching
/// <see cref="ProseWriterRouterTests"/>'s established pattern for paths that never reach the
/// null dependencies) and assert on which exception surfaces: the gate's own
/// <see cref="InvalidOperationException"/> when it should block, or something else (a downstream
/// NullReferenceException from the null <c>generator</c>) proving the gate let the call through.
/// </summary>
[TestFixture]
public class ProseWriterRouterGateTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-gate-tests-" + Guid.NewGuid().ToString("N"));
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

    private ProseWriterRouter BuildRouter() => new(
        generator:    null!,
        methodology:  new StoryMethodologyService(),
        modeDetector: new BeatModeDetector(null!),
        monitor:      new WorkflowMonitorService(dbFactory),
        log:          NullLogger<ProseWriterRouter>.Instance,
        dbFactory:    dbFactory);

    private async Task<Guid> SeedBookAsync(bool hasOutline, bool hasBlueprint)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = NodeFactory.Create("book");
        node.Id = Guid.CreateVersion7();
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        if (hasOutline) node.NodeOutline = "# Outline\n\nSomething planned.";
        db.Nodes.Add(node);
        if (hasBlueprint)
        {
            db.NodeStructuralBlueprints.Add(new NodeStructuralBlueprint { NodeId = node.Id });
        }
        await db.SaveChangesAsync();
        return node.Id;
    }

    private static BeatContext Ctx(Guid nodeId) => new() { NodeId = nodeId, BeatGoal = "something happens" };

    [Test]
    public async Task WriteAsync_NoOutlineNoBlueprint_ThrowsLockedPipelineGate()
    {
        var nodeId = await SeedBookAsync(hasOutline: false, hasBlueprint: false);
        var router = BuildRouter();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            router.WriteAsync(Ctx(nodeId), totalBeats: 12));
        Assert.That(ex!.Message, Does.Contain("no outline").IgnoreCase);
        Assert.That(ex.Message, Does.Contain("no structural blueprint").IgnoreCase);
    }

    /// <summary>Asserts the gate did NOT fire: either nothing threw, or something other than the
    /// gate's own InvalidOperationException (typically a NullReferenceException from the
    /// deliberately-null <c>generator</c> further down the pipeline — proof the call got PAST
    /// the gate, not that it completed cleanly).</summary>
    private static void AssertGateDidNotBlock(Exception? ex)
    {
        if (ex is InvalidOperationException ioe)
            Assert.That(ioe.Message, Does.Not.Contain("locked pipeline"));
    }

    [Test]
    public async Task WriteAsync_HasOutlineNoBlueprint_DoesNotBlock()
    {
        var nodeId = await SeedBookAsync(hasOutline: true, hasBlueprint: false);
        var router = BuildRouter();

        var ex = Assert.CatchAsync<Exception>(() => router.WriteAsync(Ctx(nodeId), totalBeats: 12));
        AssertGateDidNotBlock(ex);
    }

    [Test]
    public async Task WriteAsync_NoOutlineHasBlueprint_DoesNotBlock()
    {
        var nodeId = await SeedBookAsync(hasOutline: false, hasBlueprint: true);
        var router = BuildRouter();

        var ex = Assert.CatchAsync<Exception>(() => router.WriteAsync(Ctx(nodeId), totalBeats: 12));
        AssertGateDidNotBlock(ex);
    }

    [Test]
    public async Task WriteAsync_NoOutlineNoBlueprint_AllowUnblueprintedOverrides()
    {
        var nodeId = await SeedBookAsync(hasOutline: false, hasBlueprint: false);
        var router = BuildRouter();

        var ex = Assert.CatchAsync<Exception>(() =>
            router.WriteAsync(Ctx(nodeId), totalBeats: 12, allowUnblueprinted: true));
        AssertGateDidNotBlock(ex);
    }

    [Test]
    public async Task WriteAsync_TotalBeatsZero_GateNeverEvaluated()
    {
        // totalBeats == 0 means "positional pacing/structural injection disabled" — the gate
        // is scoped out entirely for this case (previews, one-off snippet calls), matching the
        // existing convention every other totalBeats-gated enrichment stage in this method uses.
        var nodeId = await SeedBookAsync(hasOutline: false, hasBlueprint: false);
        var router = BuildRouter();

        var ex = Assert.CatchAsync<Exception>(() => router.WriteAsync(Ctx(nodeId), totalBeats: 0));
        AssertGateDidNotBlock(ex);
    }
}
