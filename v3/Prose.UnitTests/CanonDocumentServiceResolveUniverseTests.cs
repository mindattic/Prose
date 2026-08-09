using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to <see cref="CanonDocumentService.ResolveUniverseIdAsync"/>.
/// The old method was a hardcoded switch recognizing only "glmz"/"scry" (plus aliases) — every
/// universe registered afterward (nonfiction, fiction, horror, erotica) had no path to resolve at
/// all, so every canon-document MCP tool (get_canon_document, list_canon_sections,
/// set_canon_section, generate_canon_md) returned "unknown_universe" for those slugs no matter how
/// they were spelled, even though the universes were genuinely registered and — in NONFICTION's
/// case — held 4,110 beats of real content. Fixed by falling back to a live, case-insensitive
/// slug lookup against the Universe table for anything not covered by the fast-path aliases.
/// </summary>
[TestFixture]
public class CanonDocumentServiceResolveUniverseTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private CanonDocumentService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-canondoc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "universe");
        svc = new CanonDocumentService(dbFactory, paths, new CanonDocumentTypeRegistry(dbFactory));
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedUniverseAsync(string slug, string name)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        db.Universes.Add(new Universe { Id = id, Slug = slug, Name = name });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task ResolveUniverseIdAsync_WellKnownAliases_NeverHitTheDatabase()
    {
        Assert.That(await svc.ResolveUniverseIdAsync("glmz"), Is.EqualTo(Universe.GlmzId));
        Assert.That(await svc.ResolveUniverseIdAsync("cyberpunk"), Is.EqualTo(Universe.GlmzId));
        Assert.That(await svc.ResolveUniverseIdAsync("scry"), Is.EqualTo(Universe.FantasyId));
        Assert.That(await svc.ResolveUniverseIdAsync("fantasy"), Is.EqualTo(Universe.FantasyId));
        Assert.That(await svc.ResolveUniverseIdAsync("nonfiction"), Is.EqualTo(Universe.NonfictionId));
    }

    [Test]
    public async Task ResolveUniverseIdAsync_RawGuidString_ParsesDirectly()
    {
        var raw = Guid.NewGuid();
        Assert.That(await svc.ResolveUniverseIdAsync(raw.ToString()), Is.EqualTo(raw));
    }

    [Test]
    public async Task ResolveUniverseIdAsync_UniverseWithNoWellKnownAlias_ResolvesViaDatabase()
    {
        // This is the exact production bug: a real, registered universe with none of the two
        // hardcoded aliases used to return null ("unknown_universe") no matter what.
        var fictionId = await SeedUniverseAsync("fiction", "FICTION");

        var resolved = await svc.ResolveUniverseIdAsync("fiction");

        Assert.That(resolved, Is.EqualTo(fictionId));
    }

    [Test]
    public async Task ResolveUniverseIdAsync_DatabaseLookup_IsCaseInsensitive()
    {
        var horrorId = await SeedUniverseAsync("horror", "HORROR");

        Assert.That(await svc.ResolveUniverseIdAsync("HORROR"), Is.EqualTo(horrorId));
        Assert.That(await svc.ResolveUniverseIdAsync("HoRrOr"), Is.EqualTo(horrorId));
    }

    [Test]
    public async Task ResolveUniverseIdAsync_UnknownSlug_ReturnsNull()
    {
        Assert.That(await svc.ResolveUniverseIdAsync("not-a-real-universe"), Is.Null);
    }
}
