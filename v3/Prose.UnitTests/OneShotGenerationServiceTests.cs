using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Locks in the fail-fast validation contract of <see cref="OneShotGenerationService"/>
/// (portable-writing-service plan, Phase 2) — the checks that must reject BEFORE ever reaching
/// <see cref="ProseWriterRouter.WriteAsync"/>, so a bad request never spends an LLM call.
/// WriteAsync itself requires a live LLM + DB and is out of scope for unit tests, matching
/// ProseWriterRouterTests's own stated convention ("WriteAsync requires a live LLM + DB —
/// skipped entirely").
/// </summary>
[TestFixture]
public class OneShotGenerationServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FakeUniverseContext universe = null!;
    private OneShotGenerationService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-oneshot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "oneshot");
        universe = new FakeUniverseContext();
        // router/canonDb are never reached by the validation paths under test — null! is safe,
        // same convention ProseWriterRouterTests itself already uses for untouched deps.
        svc = new OneShotGenerationService(null!, dbFactory, universe, null!);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Test]
    public void GenerateAsync_EmptyBeatGoal_ThrowsArgumentException()
    {
        var req = new OneShotGenerationService.OneShotGenerationRequest(BeatGoal: "   ");
        Assert.ThrowsAsync<ArgumentException>(() => svc.GenerateAsync(req));
    }

    [Test]
    public void GenerateAsync_UnknownUniverse_ThrowsInvalidOperationException()
    {
        var req = new OneShotGenerationService.OneShotGenerationRequest(
            BeatGoal: "Kat threatens the Observer", Universe: "no-such-universe");
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => svc.GenerateAsync(req));
        Assert.That(ex!.Message, Does.Contain("no-such-universe"));
    }

    [Test]
    public void GenerateAsync_UnknownNode_ThrowsInvalidOperationException()
    {
        var req = new OneShotGenerationService.OneShotGenerationRequest(
            BeatGoal: "Kat threatens the Observer", Node: "no-such-node-slug");
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.GenerateAsync(req));
    }

    // ── A minimal IUniverseContext for the tests — only CurrentId/ListUniverses drive resolution. ──
    private sealed class FakeUniverseContext : IUniverseContext
    {
        public Guid CurrentId { get; set; } = Guid.Empty;
        public string CurrentSlug => "test";
        public UniverseInfo? CurrentUniverse => null;
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo>();
        public bool IsGlmz => true;
        public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
        public void UseUniverse(Guid id) { CurrentId = id; }
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? id) { }
        public void PersistAsDefault(Guid id) { }
        public void Refresh() { }
    }
}
