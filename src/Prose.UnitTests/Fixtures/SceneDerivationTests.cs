using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests.Fixtures;

/// <summary>
/// Scene derivation: it must split where the story moves, leave prose alone, preserve reading
/// order, and say so when it has nothing to go on.
/// </summary>
[TestFixture]
[Category("Sqlite")]
[NonParallelizable]
public class SceneDerivationTests
{
    private CanonFixture fixture = null!;
    private SceneDerivationService svc = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = CanonFixture.Create();
        svc = new SceneDerivationService(fixture.Factory, NullLogger<SceneDerivationService>.Instance);
    }

    [TearDown]
    public void TearDown() => fixture.Dispose();

    /// <summary>Give the flat chapter's five-beat run two places, so a split is derivable.</summary>
    private async Task GivePlacesAsync(params (int Index, string Place)[] assignments)
    {
        await using var db = fixture.Factory.CreateDbContext();
        foreach (var (index, place) in assignments)
        {
            var beat = await db.Beats.IgnoreQueryFilters().FirstAsync(b => b.Id == fixture.BeatsA[index]);
            beat.PlaceName = place;
        }
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task SplitsWhereThePlaceChanges()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        // Flat chapter holds beats 4 and 5 (indices 3 and 4).
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));

        var report = await svc.DeriveAsync(fixture.BookA);
        var flat = report.Chapters.Single(c => c.ChapterNodeId == fixture.FlatChapter);

        Assert.That(flat.Scenes, Has.Count.EqualTo(2), "A place change must end a scene.");
        Assert.That(flat.Scenes[0].Title, Is.EqualTo("The Noodle Counter"));
        Assert.That(flat.Scenes[1].Title, Is.EqualTo("The Rain Outside"));
        Assert.That(flat.Scenes[1].Reason, Does.Contain("place changes"));
    }

    [TestCase("dock", "dock, forty stories up", true, "a sub-location is the same place")]
    [TestCase("building exterior, fire escape", "building interior, floor", true, "one continuous infiltration")]
    [TestCase("Mrs. Chen's stall", "Mrs. Chen's stall, two blocks south", true, "same stall, more detail")]
    [TestCase("Pixel's apartment", "Dock 14", false, "a genuine move")]
    [TestCase("apartment, GLMZ", "freight elevator shaft, building", false, "leaving the apartment")]
    [TestCase("The Pivot hallway, West Town", "Vey's shop", false, "different locations")]
    public void SamePlaceDistinguishesACameraMoveFromASceneChange(string a, string b, bool same, string why)
        => Assert.That(SceneDerivationService.SamePlace(a, b), Is.EqualTo(same), why);

    [Test]
    public async Task SaysCouldNotLookWhenNoBeatCarriesPlaceOrTime()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);

        var report = await svc.DeriveAsync(fixture.BookA);
        var flat = report.Chapters.Single(c => c.ChapterNodeId == fixture.FlatChapter);

        Assert.That(flat.CouldNotLook, Is.True,
            "With no place and no timing on any beat every split rule is inert. Reporting one scene " +
            "as though the chapter were genuinely continuous is the silent-zero failure again.");
        Assert.That(flat.BeatsWithPlace, Is.Zero);
    }

    [Test]
    public async Task ApplyCreatesSceneNodesAndRepointsBeatsWithoutTouchingProse()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));

        string[] before;
        await using (var db = fixture.Factory.CreateDbContext())
            before = await db.Beats.IgnoreQueryFilters()
                .Where(b => fixture.BeatsA.Contains(b.Id)).OrderBy(b => b.Number)
                .Select(b => b.Text).ToArrayAsync();

        await svc.DeriveAsync(fixture.BookA, apply: true);

        await using (var db = fixture.Factory.CreateDbContext())
        {
            var scenes = await db.Nodes.IgnoreQueryFilters()
                .Where(n => n.ParentNodeId == fixture.FlatChapter && n is SceneNode).ToListAsync();
            Assert.That(scenes, Has.Count.EqualTo(2), "Two SceneNodes should now sit under the chapter.");

            // The chapter no longer owns the beats directly; the scenes do.
            var stillOnChapter = await db.BeatNodes.IgnoreQueryFilters()
                .CountAsync(bn => bn.NodeId == fixture.FlatChapter);
            Assert.That(stillOnChapter, Is.Zero, "Beats must be repointed at their scene.");

            // Prose is untouched — this derives structure, it does not rewrite.
            var after = await db.Beats.IgnoreQueryFilters()
                .Where(b => fixture.BeatsA.Contains(b.Id)).OrderBy(b => b.Number)
                .Select(b => b.Text).ToArrayAsync();
            Assert.That(after, Is.EqualTo(before), "No beat text may change.");

            // And the book still reaches all five beats, now one level deeper.
            var leaves = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, fixture.BookA);
            var reachable = await db.BeatNodes.IgnoreQueryFilters()
                .Where(bn => leaves.Contains(bn.NodeId)).Select(bn => bn.BeatId).ToListAsync();
            Assert.That(reachable, Is.EquivalentTo(fixture.BeatsA),
                "Adding a level must not lose a beat — this is exactly what the one-level walks got wrong.");
        }
    }

    [Test]
    public async Task ASingleChapterCanBeDerivedOnItsOwn()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));

        // Passing the chapter rather than the book: the chapter is its own leaf, which an earlier
        // guard skipped, so targeting one chapter silently derived nothing.
        var report = await svc.DeriveAsync(fixture.FlatChapter);

        Assert.That(report.Chapters, Has.Count.EqualTo(1));
        Assert.That(report.Chapters[0].Scenes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ReRunningIsANoOp()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));

        await svc.DeriveAsync(fixture.BookA, apply: true);
        await svc.DeriveAsync(fixture.BookA, apply: true);

        await using var db = fixture.Factory.CreateDbContext();
        var scenes = await db.Nodes.IgnoreQueryFilters()
            .CountAsync(n => n.ParentNodeId == fixture.FlatChapter && n is SceneNode);
        Assert.That(scenes, Is.EqualTo(2),
            "A second run must not wrap the scenes in more scenes. Derivation has to be repeatable.");
    }

    [Test]
    public async Task ASingleSceneChapterIsNotWrapped()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        // Both beats in one place — no boundary.
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Noodle Counter"));

        await svc.DeriveAsync(fixture.BookA, apply: true);

        await using var db = fixture.Factory.CreateDbContext();
        var scenes = await db.Nodes.IgnoreQueryFilters()
            .CountAsync(n => n.ParentNodeId == fixture.FlatChapter && n is SceneNode);
        Assert.That(scenes, Is.Zero,
            "Wrapping a whole chapter in one scene adds a tree level and says nothing.");
    }
}
