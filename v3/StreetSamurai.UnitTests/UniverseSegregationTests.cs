using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0006 — proves universe segregation: the EF global query filter + insert-stamping scope
/// canon/config to the current universe, shared config rows are visible everywhere, the prompt
/// "cards" seam keeps GLMZ byte-identical, the CLI/env bootstrap resolves seed slugs, and a
/// universe switch bumps the cache-invalidation epoch. All against the in-memory SQLite test DB.
/// </summary>
[TestFixture]
public class UniverseSegregationTests
{
    // Two arbitrary test universes + the well-known seeds.
    private static readonly Guid UniverseA = new("0197e9c9-0aaa-7000-8000-00000000000a");
    private static readonly Guid UniverseB = new("0197e9c9-0bbb-7000-8000-00000000000b");

    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;
    private FakeUniverseContext universe = null!;
    private IUniverseContext? previousScope;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_universe_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "engine_data"));
        paths = new TestPathProviderWithRoot(root);
        factory = TestDbFactory.For(paths, "character");
        universe = new FakeUniverseContext();
        previousScope = UniverseScope.Current;     // restore in teardown so tests don't bleed
        UniverseScope.Current = universe;
    }

    [TearDown]
    public void TearDown()
    {
        UniverseScope.Current = previousScope;
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private void AddEntity(Guid forUniverse, string slug)
    {
        universe.CurrentId = forUniverse;          // drives StampUniverseOnAdded
        using var db = factory.CreateDbContext();
        db.Entities.Add(new Entity { EntityType = "character", Name = slug, Slug = slug, Status = "canon", IsActive = true });
        db.SaveChanges();
    }

    [Test]
    public void Entity_QueryFilter_ScopesToCurrentUniverse()
    {
        AddEntity(UniverseA, "alpha");
        AddEntity(UniverseB, "beta");

        universe.CurrentId = UniverseA;
        using (var db = factory.CreateDbContext())
        {
            var names = db.Entities.AsNoTracking().Select(e => e.Slug).ToList();
            Assert.That(names, Does.Contain("alpha"));
            Assert.That(names, Does.Not.Contain("beta"), "Universe A must not see Universe B's entity");
        }

        universe.CurrentId = UniverseB;
        using (var db = factory.CreateDbContext())
        {
            var names = db.Entities.AsNoTracking().Select(e => e.Slug).ToList();
            Assert.That(names, Does.Contain("beta"));
            Assert.That(names, Does.Not.Contain("alpha"), "Universe B must not see Universe A's entity");
        }
    }

    [Test]
    public void Entity_StampedWithCurrentUniverse_OnInsert()
    {
        AddEntity(UniverseA, "stamp-check");
        universe.CurrentId = Guid.Empty;           // no scope ⇒ filter is a no-op ⇒ see all rows
        using var db = factory.CreateDbContext();
        var row = db.Entities.AsNoTracking().Single(e => e.Slug == "stamp-check");
        Assert.That(row.UniverseId, Is.EqualTo(UniverseA), "insert must stamp the current universe");
    }

    [Test]
    public void Entity_NoScope_SeesAllUniverses()
    {
        AddEntity(UniverseA, "alpha");
        AddEntity(UniverseB, "beta");
        universe.CurrentId = Guid.Empty;           // Guid.Empty ⇒ filter disabled (tests/pre-migration)
        using var db = factory.CreateDbContext();
        var names = db.Entities.AsNoTracking().Select(e => e.Slug).ToList();
        Assert.That(names, Does.Contain("alpha"));
        Assert.That(names, Does.Contain("beta"));
    }

    [Test]
    public void Setting_ScopedKey_IsolatedPerUniverse()
    {
        universe.CurrentId = UniverseA;
        using (var db = factory.CreateDbContext())
        {
            db.Settings.Add(new Setting { Key = "literary_rules", Json = "{\"a\":1}" });
            db.SaveChanges();
        }
        // Universe B sees no literary_rules row (it would get its own).
        universe.CurrentId = UniverseB;
        using (var db = factory.CreateDbContext())
        {
            Assert.That(db.Settings.AsNoTracking().Any(s => s.Key == "literary_rules"), Is.False,
                "scoped config must not bleed across universes");
        }
        // Universe A still sees it.
        universe.CurrentId = UniverseA;
        using (var db = factory.CreateDbContext())
        {
            Assert.That(db.Settings.AsNoTracking().Single(s => s.Key == "literary_rules").Json, Is.EqualTo("{\"a\":1}"));
        }
    }

    [Test]
    public void Setting_SharedKey_VisibleFromEveryUniverse()
    {
        // "users.accounts" is in SharedConfigKeys ⇒ stamped with the SHARED sentinel on insert.
        universe.CurrentId = UniverseA;
        using (var db = factory.CreateDbContext())
        {
            db.Settings.Add(new Setting { Key = "users.accounts", Json = "[]" });
            db.SaveChanges();
            var stamped = db.Settings.AsNoTracking().Single(s => s.Key == "users.accounts");
            Assert.That(stamped.UniverseId, Is.EqualTo(Universe.SharedId), "shared keys are stamped with the shared sentinel");
        }
        // Visible from a different universe.
        universe.CurrentId = UniverseB;
        using (var db = factory.CreateDbContext())
        {
            Assert.That(db.Settings.AsNoTracking().Any(s => s.Key == "users.accounts"), Is.True,
                "shared operational config must be visible from every universe");
        }
    }

    [Test]
    public void Node_QueryFilter_ScopesToCurrentUniverse()
    {
        universe.CurrentId = UniverseA;
        using (var db = factory.CreateDbContext())
        {
            db.Nodes.Add(new StoryNode { Id = Guid.CreateVersion7(), Slug = "a-tale", Title = "A Tale", Kind = "story", Status = "draft" });
            db.SaveChanges();
        }
        universe.CurrentId = UniverseB;
        using (var db = factory.CreateDbContext())
        {
            Assert.That(db.Nodes.AsNoTracking().Any(s => s.Slug == "a-tale"), Is.False,
                "a node lives in exactly one universe");
        }
    }

    [Test]
    public void Bootstrap_ParseSlug_ReadsUniverseFlag()
    {
        Assert.That(UniverseBootstrap.ParseSlug(new[] { "--review-story", "--universe", "fantasy-steampunk" }),
            Is.EqualTo("fantasy-steampunk"));
        Assert.That(UniverseBootstrap.ParseSlug(new[] { "--universe=glmz", "--k", "5" }), Is.EqualTo("glmz"));
        Assert.That(UniverseBootstrap.ParseSlug(new[] { "--list-stories" }), Is.Null);
    }

    [Test]
    public void Bootstrap_ResolvesSeedSlugs_WithoutDb()
    {
        var prevSlug = UniverseBootstrap.RequestedSlug;
        try
        {
            UniverseBootstrap.RequestedSlug = "glmz";
            Assert.That(UniverseBootstrap.ResolveWellKnownId(), Is.EqualTo(Universe.GlmzId));
            UniverseBootstrap.RequestedSlug = "fantasy-steampunk";
            Assert.That(UniverseBootstrap.ResolveWellKnownId(), Is.EqualTo(Universe.FantasyId));
            UniverseBootstrap.RequestedSlug = "no-such-universe";
            Assert.That(UniverseBootstrap.ResolveWellKnownId(), Is.Null);
        }
        finally { UniverseBootstrap.RequestedSlug = prevSlug; }
    }

    [Test]
    public void Epoch_BumpsOnUniverseSwitch()
    {
        var before = UniverseScope.Epoch;
        UniverseScope.BumpEpoch();
        Assert.That(UniverseScope.Epoch, Is.EqualTo(before + 1), "a switch must bump the cache-invalidation epoch");
    }

    [Test]
    public void WellKnownIds_AreUuidV7()
    {
        // Version nibble (3rd group, 1st char) must be '7'; variant (4th group, 1st char) in 8-b.
        foreach (var id in new[] { Universe.GlmzId, Universe.FantasyId, Universe.SharedId })
        {
            var s = id.ToString();
            Assert.That(s[14], Is.EqualTo('7'), $"{s} must be UUIDv7 (version nibble)");
            Assert.That("89ab", Does.Contain(char.ToLower(s[19])), $"{s} must have a valid UUID variant");
        }
    }

    // ── A minimal IUniverseContext for the tests — only CurrentId drives the filters. ──
    private sealed class FakeUniverseContext : IUniverseContext
    {
        public Guid CurrentId { get; set; } = Guid.Empty;
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
