using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Same-universe, cross-book entity-doc leakage in <see cref="DocContextService"/>'s step-3
/// keyword pass — found on RESIST (Irish rebellion, NONFICTION) pulling in NEPH's ("Sons of
/// God", also NONFICTION) biblical-figure entity docs. Universe scoping (see
/// <c>MarkdownFileUniverseScopingTests</c>) was already correct here — both books share a
/// universe. The leak is a plain textual false positive: an entity whose canonical Name is a
/// single word (e.g. "James") gets a bare trigger, which matches as a substring of any longer
/// name that starts or ends with it ("James Stephens", RESIST's own real entity).
/// </summary>
[TestFixture]
public class DocContextEntityDocCollisionTests
{
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private DocContextService svc = null!;
    private FakeUniverseContext universe = null!;
    private IUniverseContext? previousScope;
    private Guid universeId;
    private Guid nephBookId;
    private Guid resistBookId;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_dcmcollision_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "engine_data"));
        paths = new TestPathProviderWithRoot(root);
        factory = TestDbFactory.For(paths, "character");

        universeId = Guid.NewGuid();
        universe = new FakeUniverseContext { CurrentId = universeId };
        previousScope = UniverseScope.Current;
        UniverseScope.Current = universe;

        nephBookId = Guid.NewGuid();
        resistBookId = Guid.NewGuid();
        using (var db = factory.CreateDbContext())
        {
            db.Nodes.Add(new BookNode { Id = nephBookId, Kind = "book", Title = "NEPH", Slug = "neph-" + nephBookId.ToString("N")[..8], UniverseId = universeId });
            db.Nodes.Add(new BookNode { Id = resistBookId, Kind = "book", Title = "RESIST", Slug = "resist-" + resistBookId.ToString("N")[..8], UniverseId = universeId });
            db.SaveChanges();
        }

        var settings = new SettingsService(Path.Combine(root, "settings"));
        var embeddings = new EmbeddingService(factory, settings, null!, NullLogger<EmbeddingService>.Instance);
        var stack = new DocContextStack();
        var disambiguation = new EntityDisambiguationService(factory, NullLogger<EntityDisambiguationService>.Instance);
        svc = new DocContextService(factory, stack, embeddings, NullLogger<DocContextService>.Instance,
            userContext: null, entityDocs: null, disambiguation: disambiguation);
    }

    [TearDown]
    public void TearDown()
    {
        UniverseScope.Current = previousScope;
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private void AddEntityDoc(string slug, string triggers, Guid? originNodeId)
    {
        using var db = factory.CreateDbContext();
        var entityId = Guid.NewGuid();
        db.Entities.Add(new Entity
        {
            Id = entityId, EntityType = "character", Name = slug, Slug = slug,
            UniverseId = universeId,  OriginNodeId = originNodeId,
        });
        db.MarkdownFiles.Add(new MarkdownFile
        {
            Id = Guid.NewGuid(), FilePath = "", FileRoot = "project",
            RelativePath = $"docs/entities/{slug}.md", FileName = $"{slug}.md",
            Category = "entity-doc", Content = "# doc", ContentHash = slug,
            LastSyncedAt = DateTime.UtcNow, SyncedBy = "test", Tier = "topic", Scope = "",
            Triggers = triggers, AutoTier = false, UniverseId = universeId, EntityId = entityId,
        });
        db.SaveChanges();
    }

    private static bool Loaded(DocContextService.DocContextResult result, string relativePath) =>
        result.Loaded.Any(d => d.RelativePath == relativePath);

    [Test]
    public async Task BareNameFlankedByLongerName_IsExcluded()
    {
        // NEPH's "James" (bare trigger) vs RESIST's real "James Stephens" (multi-word trigger).
        AddEntityDoc("james-brother-of-jesus", "james", originNodeId: null);
        AddEntityDoc("james-stephens", "james stephens, stephens", originNodeId: null);

        var result = await svc.PrepareContextAsync(resistBookId, "RESIST",
            "James Stephens gathered his men.", useEmbedding: false);

        Assert.That(Loaded(result, "docs/entities/james-brother-of-jesus.md"), Is.False,
            "bare 'james' is flanked by 'Stephens' — plausibly part of a different, longer name");
        Assert.That(Loaded(result, "docs/entities/james-stephens.md"), Is.True,
            "RESIST's own entity, matched via its full multi-word trigger, must still admit");
    }

    [Test]
    public async Task SameBareTokenCollision_ResolvesByBookOrExcludesBoth()
    {
        // Two distinct NEPH entities both reduce to the identical bare token "james".
        AddEntityDoc("james-brother-of-jesus", "james", originNodeId: null);
        AddEntityDoc("james-son-of-zebedee", "james", originNodeId: null);

        var resistResult = await svc.PrepareContextAsync(resistBookId, "RESIST",
            "James spoke of persecution.", useEmbedding: false);
        Assert.That(Loaded(resistResult, "docs/entities/james-brother-of-jesus.md"), Is.False);
        Assert.That(Loaded(resistResult, "docs/entities/james-son-of-zebedee.md"), Is.False,
            "ambiguous same-token collision with no book match must exclude ALL colliding candidates, not guess");
    }

    [Test]
    public async Task SameBareTokenCollision_ExplicitOriginNodeIdWinsForItsOwnBook()
    {
        AddEntityDoc("james-brother-of-jesus", "james", originNodeId: nephBookId);
        AddEntityDoc("james-son-of-zebedee", "james", originNodeId: null);

        var nephResult = await svc.PrepareContextAsync(nephBookId, "NEPH",
            "James spoke of persecution.", useEmbedding: false);

        Assert.That(Loaded(nephResult, "docs/entities/james-brother-of-jesus.md"), Is.True,
            "explicit OriginNodeId matching the current book wins the collision outright");
        Assert.That(Loaded(nephResult, "docs/entities/james-son-of-zebedee.md"), Is.False,
            "the other colliding candidate is excluded even though it has no OriginNodeId at all");
    }

    [Test]
    public async Task BareFirstNamePrefixCollidesWithAnotherEntitysFullerName_IsExcluded()
    {
        // NEPH's bare "michael" vs RESIST's real "Michael Collins" — RESIST's own entity never
        // carries a bare "michael" trigger (CollectNameTokens never emits a bare FIRST word), so
        // this can't be caught by the same-token collision group; it needs the prefix check.
        AddEntityDoc("michael-archangel", "michael, michael archangel", originNodeId: null);
        AddEntityDoc("michael-collins", "michael collins, collins", originNodeId: null);

        var result = await svc.PrepareContextAsync(resistBookId, "RESIST",
            "Michael gave the order to retreat.", useEmbedding: false);

        Assert.That(Loaded(result, "docs/entities/michael-archangel.md"), Is.False,
            "a bare 'michael' hit with no adjacent surname is still ambiguous against RESIST's own Michael Collins");
    }

    [Test]
    public async Task UniqueMultiWordEntity_IsUnaffected()
    {
        AddEntityDoc("declan-doyle", "declan doyle, doyle", originNodeId: null);

        var result = await svc.PrepareContextAsync(resistBookId, "RESIST",
            "Declan Doyle walked the corridor.", useEmbedding: false);

        Assert.That(Loaded(result, "docs/entities/declan-doyle.md"), Is.True,
            "a multi-word trigger never routes through the bare-token guards at all");
    }

    [Test]
    public async Task StandaloneBareName_StillAdmitsInItsOwnBook()
    {
        // Regression guard: the adjacency heuristic must not over-trigger on a standalone name
        // with no flanking capitalized word.
        AddEntityDoc("james-brother-of-jesus", "james", originNodeId: null);

        var result = await svc.PrepareContextAsync(nephBookId, "NEPH",
            "James spoke of persecution.", useEmbedding: false);

        Assert.That(Loaded(result, "docs/entities/james-brother-of-jesus.md"), Is.True,
            "no adjacent capitalized word — a standalone reference to the bare-named entity must still admit");
    }

    // Minimal IUniverseContext — only CurrentId drives the query filters (mirrors the equivalent
    // private fixture-scoped fake in MarkdownFileUniverseScopingTests / UniverseSegregationTests).
    private sealed class FakeUniverseContext : IUniverseContext
    {
        public Guid CurrentId { get; set; } = Guid.Empty;
        // A fake that pins CurrentId HAS named its universe — that is exactly what an explicit
        // scope means (Story Ledger Phase 3, UnscopedUniverseWriteCheck). Guid.Empty means no
        // universe is wired at all, where scoping is a no-op and nothing gates on this.
        public bool IsExplicitlyScoped => CurrentId != Guid.Empty;

        public string CurrentSlug => "test";
        public UniverseInfo? CurrentUniverse => new(CurrentId, CurrentSlug, "Test", null, "a test world", true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo>();
        public bool IsGlmz => false;
        public string UniverseGroundingOr(string glmzFallback) => CurrentUniverse?.UniversePrimer ?? "a self-contained fictional world";
        public void UseUniverse(Guid id) { CurrentId = id; UniverseScope.BumpEpoch(); }
        public bool UseUniverseBySlug(string slug) => false;
        public void SetFlowUniverse(Guid? id) { }
        public void PersistAsDefault(Guid id) { }
        public void Refresh() { }
    }
}
