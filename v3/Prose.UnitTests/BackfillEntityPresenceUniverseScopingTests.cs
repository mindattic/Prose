using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Cli;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 fix: <c>BackfillEntityPresenceCli</c>'s no-<c>--slug</c>
/// (corpus-wide) path queried <c>BeatNodes</c>/<c>Beats</c> directly with no join to
/// <c>Nodes</c> — the only table carrying the ambient <c>--universe</c> global query filter.
/// Result: a "--universe scry" run with no --slug silently processed the WHOLE corpus (all
/// universes), not just SCRY. Found live: a run against SCRY (431 total beats) reported 6598
/// candidate beats — exactly the leftover count from a prior GLMZ-scoped run. Fixed by always
/// joining through <c>Nodes</c> in <see cref="BackfillEntityPresenceCli.SelectCandidateBeatIdsAsync"/>.
/// </summary>
[TestFixture]
public class BackfillEntityPresenceUniverseScopingTests
{
    private static readonly Guid UniverseA = new("0197e9c9-0aaa-7000-8000-00000000000a");
    private static readonly Guid UniverseB = new("0197e9c9-0bbb-7000-8000-00000000000b");

    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private FakeUniverseContext universe = null!;
    private IUniverseContext? previousScope;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_backfill_scope_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "engine_data"));
        paths = new TestPathProviderWithRoot(root);
        factory = TestDbFactory.For(paths, "character");
        universe = new FakeUniverseContext();
        previousScope = UniverseScope.Current;
        UniverseScope.Current = universe;
    }

    [TearDown]
    public void TearDown()
    {
        UniverseScope.Current = previousScope;
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private int nextBeatNumber = 1;

    private Guid AddBookWithBeat(Guid forUniverse, string slug)
    {
        universe.CurrentId = forUniverse;
        using var db = factory.CreateDbContext();
        var node = new BookNode
        {
            Id = Guid.CreateVersion7(),
            Slug = slug,
            Title = slug,
            Kind = "book",
            Status = "draft",
            SortKey = 100,
        };
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Text = "Some prose text.", Number = nextBeatNumber++ };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = node.Id, BeatId = beat.Id, SortKey = 100 });
        db.SaveChanges();
        return beat.Id;
    }

    [Test]
    public async Task SelectCandidateBeatIdsAsync_WithNoScope_OnlyReturnsAmbientUniverseBeats()
    {
        var beatA = AddBookWithBeat(UniverseA, "book-a");
        var beatB = AddBookWithBeat(UniverseB, "book-b");

        universe.CurrentId = UniverseA;
        using var db = factory.CreateDbContext();
        var ids = await BackfillEntityPresenceCli.SelectCandidateBeatIdsAsync(db, nodeIdScope: null);

        Assert.That(ids, Does.Contain(beatA), "Universe A's own beat must be included");
        Assert.That(ids, Does.Not.Contain(beatB),
            "Universe B's beat must NOT leak into a Universe-A-scoped, no-slug corpus-wide run");
    }

    [Test]
    public async Task SelectCandidateBeatIdsAsync_SwitchingAmbientUniverse_ChangesResults()
    {
        var beatA = AddBookWithBeat(UniverseA, "book-a2");
        var beatB = AddBookWithBeat(UniverseB, "book-b2");

        universe.CurrentId = UniverseB;
        using var db = factory.CreateDbContext();
        var ids = await BackfillEntityPresenceCli.SelectCandidateBeatIdsAsync(db, nodeIdScope: null);

        Assert.That(ids, Does.Contain(beatB));
        Assert.That(ids, Does.Not.Contain(beatA));
    }

    // ── A minimal IUniverseContext for the tests — only CurrentId drives the filters. ──
    private sealed class FakeUniverseContext : IUniverseContext
    {
        public Guid CurrentId { get; set; } = Guid.Empty;
        // A fake that pins CurrentId HAS named its universe — that is exactly what an explicit
        // scope means (Story Ledger Phase 3, UnscopedUniverseWriteCheck). Guid.Empty means no
        // universe is wired at all, where scoping is a no-op and nothing gates on this.
        public bool IsExplicitlyScoped => CurrentId != Guid.Empty;

        public string CurrentSlug => CurrentId == Universe.GlmzId ? "glmz" : "test";
        public UniverseInfo? CurrentUniverse =>
            new(CurrentId, CurrentSlug, "Test", null, CurrentId == Universe.GlmzId ? null : "a test world", true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo>();
        public bool IsGlmz => CurrentId == Universe.GlmzId || CurrentId == Guid.Empty;
        public string UniverseGroundingOr(string glmzFallback) =>
            IsGlmz ? glmzFallback : (CurrentUniverse?.UniversePrimer ?? "a self-contained fictional world");
        public void UseUniverse(Guid id) { CurrentId = id; UniverseScope.BumpEpoch(); }
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? id) { }
        public void PersistAsDefault(Guid id) { }
        public void Refresh() { }
    }
}
