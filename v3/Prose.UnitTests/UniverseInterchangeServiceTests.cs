using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Locks in the contract of <see cref="UniverseInterchangeService"/> (RFC 0007):
/// idempotent upsert-by-slug import, dangling-relation stub creation + later promotion,
/// RepositoryDefinition auto-registration for novel EntityTypes, universe segregation, and
/// the import→export→import round-trip guarantee.
/// </summary>
[TestFixture]
public class UniverseInterchangeServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private UniverseInterchangeService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-universe-interchange-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "universe-interchange");
        svc = new UniverseInterchangeService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static string MakeFile(
        string universeId = "testu",
        params (string id, string type, string name, string summary, (string to, string kind)[]? relations, string[]? tags)[] entities)
    {
        var doc = new
        {
            universe = new
            {
                id = universeId,
                name = "Test Universe",
                tagline = "t",
                era = "e",
                setting = "s",
                logline = "l",
                rules = new[] { "rule-one", "rule-two" },
            },
            entities = entities.Select(e => new
            {
                id = e.id,
                type = e.type,
                name = e.name,
                summary = e.summary,
                relations = (e.relations ?? Array.Empty<(string to, string kind)>())
                    .Select(r => new { to = r.to, kind = r.kind }),
                tags = e.tags ?? Array.Empty<string>(),
            }),
        };
        return JsonSerializer.Serialize(doc);
    }

    [Test]
    public async Task ImportAsync_NewUniverse_CreatesUniverseRow()
    {
        var json = MakeFile(entities: ("hero", "character", "Hero", "The hero.", null, null));
        var result = await svc.ImportAsync(json);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UniverseCreated, Is.True);
        Assert.That(result.UniverseSlug, Is.EqualTo("testu"));
        Assert.That(result.EntitiesCreated, Is.EqualTo(1));

        await using var db = await dbFactory.CreateDbContextAsync();
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == "testu");
        Assert.That(universe, Is.Not.Null);
        Assert.That(universe!.Name, Is.EqualTo("Test Universe"));
        Assert.That(universe.WorldFacts, Does.Contain("rule-one"));
    }

    [Test]
    public async Task ImportAsync_ReimportSameFile_IsNoOpDiff()
    {
        var json = MakeFile(entities:
            ("hero", "character", "Hero", "The hero.", new[] { ("sidekick", "friend_of") }, new[] { "protagonist" }));

        var first = await svc.ImportAsync(json);
        Assert.That(first.Success, Is.True);
        Assert.That(first.EntitiesCreated, Is.EqualTo(1));
        Assert.That(first.StubsCreated, Is.EqualTo(1));
        Assert.That(first.EdgesCreated, Is.EqualTo(1));

        var second = await svc.ImportAsync(json);
        Assert.That(second.Success, Is.True);
        Assert.That(second.EntitiesCreated, Is.EqualTo(0));
        Assert.That(second.EntitiesUpdated, Is.EqualTo(1));
        Assert.That(second.StubsCreated, Is.EqualTo(0));
        Assert.That(second.EdgesCreated, Is.EqualTo(0));

        await using var db = await dbFactory.CreateDbContextAsync();
        var count = await db.Entities.IgnoreQueryFilters().CountAsync();
        Assert.That(count, Is.EqualTo(2)); // "hero" + stub "sidekick" — re-import creates nothing new
    }

    [Test]
    public async Task ImportAsync_DanglingRelation_CreatesStubEntity()
    {
        var json = MakeFile(entities: ("a", "character", "A", "desc", new[] { ("b", "knows") }, null));
        var result = await svc.ImportAsync(json);

        Assert.That(result.StubsCreated, Is.EqualTo(1));

        await using var db = await dbFactory.CreateDbContextAsync();
        var stub = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Slug == "b");
        Assert.That(stub, Is.Not.Null);
        Assert.That(stub!.Status, Is.EqualTo("stub"));
    }

    [Test]
    public async Task ImportAsync_LaterFileDefinesStubTarget_PromotesStub()
    {
        var first = MakeFile(entities: ("a", "character", "A", "desc", new[] { ("b", "knows") }, null));
        await svc.ImportAsync(first);

        var second = MakeFile("testu",
            ("a", "character", "A", "desc", new[] { ("b", "knows") }, null),
            ("b", "character", "B", "Now a real entity.", null, null));
        var result = await svc.ImportAsync(second);

        Assert.That(result.StubsPromoted, Is.EqualTo(1));

        await using var db = await dbFactory.CreateDbContextAsync();
        var b = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Slug == "b");
        Assert.That(b!.Status, Is.EqualTo("canon"));
        Assert.That(b.Name, Is.EqualTo("B"));
    }

    [Test]
    public async Task ImportAsync_UnknownEntityType_CreatesRepositoryDefinition()
    {
        var json = MakeFile(entities: ("thing", "gizmo", "Gizmo", "A gizmo.", null, null));
        await svc.ImportAsync(json);

        await using var db = await dbFactory.CreateDbContextAsync();
        var def = await db.RepositoryDefinitions.FirstOrDefaultAsync(d => d.Slug == "gizmo");
        Assert.That(def, Is.Not.Null);
    }

    [Test]
    public async Task ImportAsync_BuiltInEntityTypes_DoNotGetRepositoryDefinitions()
    {
        var json = MakeFile("testu",
            ("hero", "character", "Hero", "d", null, null),
            ("place1", "location", "Place", "d", null, null),
            ("fac1", "faction", "Faction", "d", null, null));
        await svc.ImportAsync(json);

        await using var db = await dbFactory.CreateDbContextAsync();
        var defs = await db.RepositoryDefinitions
            .Where(d => d.Slug == "character" || d.Slug == "place" || d.Slug == "faction")
            .CountAsync();
        Assert.That(defs, Is.EqualTo(0));
    }

    [Test]
    public async Task ImportThenExportThenImport_ProducesZeroDiff()
    {
        var json = MakeFile("testu",
            ("a", "character", "A", "Alpha.", new[] { ("b", "knows") }, new[] { "tag1" }),
            ("b", "location", "B", "Beta place.", null, new[] { "tag2" }));

        var import1 = await svc.ImportAsync(json);
        Assert.That(import1.Success, Is.True);

        var exported = await svc.ExportAsync(import1.UniverseSlug);

        var import2 = await svc.ImportAsync(exported, import1.UniverseSlug);
        Assert.That(import2.Success, Is.True);
        Assert.That(import2.EntitiesCreated, Is.EqualTo(0));
        Assert.That(import2.EdgesCreated, Is.EqualTo(0));
        Assert.That(import2.StubsCreated, Is.EqualTo(0));

        var exportedAgain = await svc.ExportAsync(import1.UniverseSlug);
        using var doc1 = JsonDocument.Parse(exported);
        using var doc2 = JsonDocument.Parse(exportedAgain);
        Assert.That(doc1.RootElement.GetProperty("entities").GetArrayLength(),
            Is.EqualTo(doc2.RootElement.GetProperty("entities").GetArrayLength()));

        var names1 = doc1.RootElement.GetProperty("entities").EnumerateArray()
            .Select(e => e.GetProperty("name").GetString()).OrderBy(n => n).ToList();
        var names2 = doc2.RootElement.GetProperty("entities").EnumerateArray()
            .Select(e => e.GetProperty("name").GetString()).OrderBy(n => n).ToList();
        Assert.That(names1, Is.EqualTo(names2));
    }

    [Test]
    public async Task ExportAsync_ExcludesUnpromotedStubs()
    {
        var json = MakeFile(entities: ("a", "character", "A", "desc", new[] { ("ghost", "knows") }, null));
        var import = await svc.ImportAsync(json);
        Assert.That(import.StubsCreated, Is.EqualTo(1));

        var exported = await svc.ExportAsync(import.UniverseSlug);
        using var doc = JsonDocument.Parse(exported);
        var ids = doc.RootElement.GetProperty("entities").EnumerateArray()
            .Select(e => e.GetProperty("id").GetString()).ToList();
        Assert.That(ids, Does.Not.Contain("ghost"));
        Assert.That(ids, Does.Contain("a"));
    }

    [Test]
    public async Task ImportAsync_SecondUniverse_NeverTouchesFirstUniverseRows()
    {
        var jsonA = MakeFile(universeId: "uniA", entities: ("x", "character", "X", "desc", null, null));
        var jsonB = MakeFile(universeId: "uniB", entities: ("x", "character", "X-in-B", "desc", null, null));

        await svc.ImportAsync(jsonA);
        await svc.ImportAsync(jsonB);

        await using var db = await dbFactory.CreateDbContextAsync();
        var uniA = await db.Universes.FirstAsync(u => u.Slug == "unia");
        var uniB = await db.Universes.FirstAsync(u => u.Slug == "unib");

        var entityA = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.UniverseId == uniA.Id && e.Slug == "x");
        var entityB = await db.Entities.IgnoreQueryFilters().FirstAsync(e => e.UniverseId == uniB.Id && e.Slug == "x");

        Assert.That(entityA.Name, Is.EqualTo("X"));
        Assert.That(entityB.Name, Is.EqualTo("X-in-B"));
        Assert.That(entityA.Id, Is.Not.EqualTo(entityB.Id));
    }

    [Test]
    public void ExportAsync_UnknownUniverse_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.ExportAsync("nonexistent"));
    }

    /// <summary>
    /// Regression test for a real bug found live: the service must be correct regardless of the
    /// AMBIENT UniverseScope (a Hub process is almost always scoped to some other universe, e.g.
    /// GLMZ, from prior CLI/MCP activity) — every query must use IgnoreQueryFilters() + an
    /// explicit UniverseId check rather than relying on the ambient scope. A prior version of
    /// UpsertUniverseSourceAsync's Settings lookup was missing IgnoreQueryFilters(): under a
    /// non-empty ambient scope that wasn't this test universe, the existing Setting row was
    /// invisible to the query filter, causing a re-import to attempt a duplicate INSERT and crash
    /// with a PK violation instead of updating in place. This was invisible to every other test in
    /// this file because they never set an ambient scope (UniverseScope.EffectiveId defaults to
    /// Guid.Empty, which makes the query filter a no-op — exactly the gap that let the bug reach
    /// production before this test existed).
    /// </summary>
    [Test]
    public async Task ImportAsync_ReimportUnderNonEmptyAmbientUniverseScope_DoesNotThrow()
    {
        var previousScope = UniverseScope.Current;
        try
        {
            UniverseScope.Current = new AmbientScopeStub(Universe.GlmzId);

            var json = MakeFile(entities: ("hero", "character", "Hero", "The hero.", null, null));
            var first = await svc.ImportAsync(json);
            Assert.That(first.Success, Is.True);

            var second = await svc.ImportAsync(json);
            Assert.That(second.Success, Is.True, string.Join("; ", second.Errors));
            Assert.That(second.EntitiesCreated, Is.EqualTo(0));
            Assert.That(second.EntitiesUpdated, Is.EqualTo(1));
        }
        finally
        {
            UniverseScope.Current = previousScope;
        }
    }

    private sealed class AmbientScopeStub : IUniverseContext
    {
        public AmbientScopeStub(Guid currentId) => CurrentId = currentId;
        public Guid CurrentId { get; set; }
        public string CurrentSlug => "ambient-test";
        public UniverseInfo? CurrentUniverse => new(CurrentId, CurrentSlug, "Ambient Test", null, null, true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo>();
        public bool IsGlmz => CurrentId == Universe.GlmzId;
        public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
        public void UseUniverse(Guid id) => CurrentId = id;
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? id) { }
        public void PersistAsDefault(Guid id) { }
        public void Refresh() { }
    }
}
