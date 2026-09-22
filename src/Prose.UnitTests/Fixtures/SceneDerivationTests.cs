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

    /// <summary>
    /// The whole requirement in one test: <b>derivation is invisible to the reader.</b>
    ///
    /// <para>Deriving scenes changes the node tree under a chapter and nothing else, so the spine
    /// every exporter walks must come out identical — same units, same headings, same word count.
    /// It did not: the derived scenes became chapters of their own, headed by their location
    /// strings, and the chapter they were split out of lost its heading entirely because it no
    /// longer held beats.</para>
    /// </summary>
    [Test]
    public async Task DerivingScenes_LeavesTheSpineTheReaderSeesUnchanged()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));

        var spines = new BookSpineService(fixture.Factory);
        var before = await spines.GetAsync(fixture.BookA);
        var headingsBefore = before.Chapters.Select(c => c.Heading).ToArray();

        await svc.DeriveAsync(fixture.BookA, apply: true);

        var after = await spines.GetAsync(fixture.BookA);

        Assert.Multiple(() =>
        {
            Assert.That(after.ChapterCount, Is.EqualTo(before.ChapterCount));
            Assert.That(after.Chapters.Select(c => c.Heading), Is.EqualTo(headingsBefore));
            Assert.That(after.BeatCount, Is.EqualTo(before.BeatCount));
            Assert.That(after.WordCount, Is.EqualTo(before.WordCount));
            Assert.That(after.Chapters.Select(c => c.Heading),
                        Has.None.EqualTo("The Noodle Counter"),
                        "A derived scene's location must never print as a chapter heading.");
        });
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

    /// <summary>
    /// The read-freshness hash has to see a scene-derived chapter's beats, or it reports "Current"
    /// for a chapter nobody can prove was read. Found live on Bushido Coda 2026-09-22: the tracker
    /// hashed 476 of 521 beats and the 45 it missed were Chapter 15's, sitting under 21 derived
    /// scene nodes, because the old walk took only beats hanging directly off a chapter node.
    /// </summary>
    [Test]
    public async Task DerivingScenes_DoesNotHideBeatsFromTheReadFreshnessHash()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));

        var tracker = new SequentialReadTrackingService(fixture.Factory);
        var (hashBefore, beatsBefore, chaptersBefore) =
            await tracker.ComputeBeatSequenceHashAsync(fixture.BookA);

        await svc.DeriveAsync(fixture.BookA, apply: true);

        var (hashAfter, beatsAfter, chaptersAfter) =
            await tracker.ComputeBeatSequenceHashAsync(fixture.BookA);

        Assert.Multiple(() =>
        {
            Assert.That(beatsAfter, Is.EqualTo(beatsBefore),
                "Derivation moves beats onto scene nodes without adding or removing one. If the "
                + "count drops, the hash has gone blind to a chapter and will report a read it "
                + "cannot vouch for.");
            Assert.That(chaptersAfter, Is.EqualTo(chaptersBefore),
                "A derived scene is not a chapter.");
            Assert.That(hashAfter, Is.Not.EqualTo(hashBefore),
                "The structure did change, so a recorded read must go Stale — staleness is "
                + "detected, not trusted.");
        });
    }

    /// <summary>The count the hash reports has to be every beat in the book, not every beat the
    /// walk happened to reach.</summary>
    [Test]
    public async Task ReadFreshnessHash_CountsEveryBeatInTheBook()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivePlacesAsync((3, "The Noodle Counter"), (4, "The Rain Outside"));
        await svc.DeriveAsync(fixture.BookA, apply: true);

        var (_, beatCount, _) =
            await new SequentialReadTrackingService(fixture.Factory)
                .ComputeBeatSequenceHashAsync(fixture.BookA);

        await using var db = fixture.Factory.CreateDbContext();
        var spine = await new BookSpineService(fixture.Factory).GetAsync(fixture.BookA);

        Assert.That(beatCount, Is.EqualTo(spine.BeatCount),
            "The tracker and the exporters must agree on what the book is. They are the same walk.");
    }

    /// <summary>
    /// A recorded read is a claim about prose. Rewriting a beat has to invalidate it, or the record
    /// says someone read this book front to back when what they read no longer exists. Found live
    /// 2026-09-22: a 45-beat fix pass ran against Bushido Coda and the read stayed "Current" the
    /// whole way through, because the hash only covered beat ids and their order.
    /// </summary>
    [Test]
    public async Task EditingABeat_MakesARecordedReadStale()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        var tracker = new SequentialReadTrackingService(fixture.Factory);

        var (hashBefore, beatsBefore, _) = await tracker.ComputeBeatSequenceHashAsync(fixture.BookA);

        await using (var db = fixture.Factory.CreateDbContext())
        {
            var beat = await db.Beats.IgnoreQueryFilters().FirstAsync(b => b.Id == fixture.BeatsA[0]);
            beat.Text = beat.Text + " One more sentence, which a reader would have to read.";
            beat.TextHash = Guid.NewGuid().ToString("N");   // what a real save recomputes
            await db.SaveChangesAsync();
        }

        var (hashAfter, beatsAfter, _) = await tracker.ComputeBeatSequenceHashAsync(fixture.BookA);

        Assert.Multiple(() =>
        {
            Assert.That(beatsAfter, Is.EqualTo(beatsBefore), "Editing prose adds no beats.");
            Assert.That(hashAfter, Is.Not.EqualTo(hashBefore),
                "The structure did not move, but the words did. A read recorded against the old words "
                + "no longer covers this book.");
        });
    }
}
