using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// MarkdownFiles universe scoping (RFC 0006 / SS-LAW-15).
///
/// MarkdownFiles was the last DCM-relevant table with no universe filter, so
/// <c>DocContextService</c>'s candidate query saw every doc in every universe on every beat.
/// <c>Scope</c> could not substitute, because entity docs are written with <c>Scope = ""</c> —
/// which is why a SCRY beat could pull in a GLMZ character doc through the keyword or embedding
/// pass despite <c>--universe scry</c>.
/// </summary>
[TestFixture]
public class MarkdownFileUniverseScopingTests
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
        var root = Path.Combine(Path.GetTempPath(), $"ss_mdscope_{Guid.NewGuid():N}");
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

    private void AddDoc(string relativePath, Guid universeId)
    {
        // Match UniverseSegregationTests.AddEntity: set the ambient scope before adding, so any
        // insert-stamping agrees with the explicit UniverseId rather than fighting it.
        universe.CurrentId = universeId;
        using var db = factory.CreateDbContext();
        db.MarkdownFiles.Add(new MarkdownFile
        {
            Id           = Guid.NewGuid(),
            FilePath     = "",
            FileRoot     = "project",
            RelativePath = relativePath,
            FileName     = Path.GetFileName(relativePath),
            Category     = "entity-doc",
            Content      = "# doc",
            ContentHash  = relativePath,
            LastSyncedAt = DateTime.UtcNow,
            SyncedBy     = "test",
            Tier         = "topic",
            Scope        = "",              // exactly how entity docs are written
            Triggers     = "kyle",
            AutoTier     = false,
            UniverseId   = universeId,
        });
        db.SaveChanges();
    }

    private List<string> VisiblePathsFor(Guid universeId)
    {
        universe.CurrentId = universeId;
        using var db = factory.CreateDbContext();
        return db.MarkdownFiles.Select(m => m.RelativePath).OrderBy(p => p).ToList();
    }

    [Test]
    public void ADocInAnotherUniverseIsInvisible()
    {
        AddDoc("docs/entities/kyle.md", UniverseA);
        AddDoc("docs/entities/lyra.md", UniverseB);

        Assert.That(VisiblePathsFor(UniverseA), Is.EqualTo(new[] { "docs/entities/kyle.md" }));
        Assert.That(VisiblePathsFor(UniverseB), Is.EqualTo(new[] { "docs/entities/lyra.md" }));
    }

    [Test]
    public void SharedDocsAreVisibleFromEveryUniverse()
    {
        AddDoc("docs/CRAFT.md", Universe.SharedId);
        AddDoc("docs/entities/kyle.md", UniverseA);

        Assert.That(VisiblePathsFor(UniverseA), Does.Contain("docs/CRAFT.md"));
        Assert.That(VisiblePathsFor(UniverseB), Does.Contain("docs/CRAFT.md"),
            "ENGINE.md / CRAFT.md / the digest apply everywhere and must not be scoped away");
        Assert.That(VisiblePathsFor(UniverseB), Does.Not.Contain("docs/entities/kyle.md"));
    }

    [Test]
    public void ScopeCannotSubstituteForTheFilter()
    {
        // Both docs carry Scope = "" and a shared trigger, which is exactly the entity-doc shape.
        // Scope-based routing has nothing to discriminate on here — only UniverseId does.
        AddDoc("docs/entities/glmz-character.md", UniverseA);
        AddDoc("docs/entities/scry-character.md", UniverseB);

        var visible = VisiblePathsFor(UniverseB);
        Assert.That(visible, Does.Not.Contain("docs/entities/glmz-character.md"),
            "this is the cross-universe contamination path: same empty Scope, same trigger");
        Assert.That(visible, Does.Contain("docs/entities/scry-character.md"));
    }

    [Test]
    public void IgnoreQueryFiltersStillSeesEverything()
    {
        AddDoc("docs/entities/kyle.md", UniverseA);
        AddDoc("docs/entities/lyra.md", UniverseB);

        universe.CurrentId = UniverseA;
        using var db = factory.CreateDbContext();
        var all = db.MarkdownFiles.IgnoreQueryFilters().Select(m => m.RelativePath).ToList();

        Assert.That(all, Has.Count.EqualTo(2),
            "maintenance paths (list/search/restore) and the (FileRoot,RelativePath) upsert lookup " +
            "must see across universes, or the unique index gets violated on insert");
    }

    [Test]
    public void NoUniverseScope_SeesEverything()
    {
        AddDoc("docs/entities/kyle.md", UniverseA);
        AddDoc("docs/entities/lyra.md", UniverseB);

        // Guid.Empty = no universe wired (tests, design-time, pre-migration): filter is a no-op.
        Assert.That(VisiblePathsFor(Guid.Empty), Has.Count.EqualTo(2));
    }

    // Minimal IUniverseContext — only CurrentId drives the query filters. (UniverseSegregationTests
    // has an equivalent, but it is private to that fixture.)
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
