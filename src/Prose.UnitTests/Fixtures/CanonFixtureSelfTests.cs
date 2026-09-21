using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests.Fixtures;

/// <summary>
/// Proves the fixture's traps are armed.
///
/// <para>Every anti-silent-zero test in the suite rests on <see cref="CanonFixture"/> being shaped
/// so that a shortcut query fails. If someone "tidies" the fixture — hangs a beat off the book
/// node, drops the deep branch, renames the decoy so the two universes stop colliding — every test
/// built on it keeps passing while proving nothing at all. That is the same failure the fixture
/// exists to prevent, so the fixture has to be held to it too.</para>
/// </summary>
[TestFixture]
[Category("Sqlite")]
[NonParallelizable] // UniverseScope.Current is a process-wide mutable static.
public class CanonFixtureSelfTests
{
    private CanonFixture fixture = null!;

    [SetUp]
    public void SetUp() => fixture = CanonFixture.Create();

    [TearDown]
    public void TearDown() => fixture.Dispose();

    [Test]
    public void BookNodeOwnsNoBeatsDirectly()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        using var db = fixture.Factory.CreateDbContext();

        var direct = db.BeatNodes.Count(bn => bn.NodeId == fixture.BookA);

        Assert.That(direct, Is.Zero,
            "The trap is disarmed: a beat now hangs directly off the book node, so a query that " +
            "joins beats straight to a book id would find rows and pass. Beats belong to scenes " +
            "or chapters, never to the book.");
    }

    [Test]
    public void EveryBeatIsReachableByRecursingButNotByOneLevel()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        using var db = fixture.Factory.CreateDbContext();

        // The correct walk: recurse to the leaves, then gather their beats.
        var leaves = await_(Prose.Core.Services.NodeWorkbenchService.GetLeafDescendantIdsAsync(db, fixture.BookA));
        var recursed = db.BeatNodes.Where(bn => leaves.Contains(bn.NodeId)).Select(bn => bn.BeatId).ToList();

        Assert.That(recursed, Is.EquivalentTo(fixture.BeatsA),
            "Recursing from the book must reach all five beats across both branches.");

        // The bug shape: descend exactly one level and take those nodes' beats.
        var oneLevelChildren = db.Nodes.Where(n => n.ParentNodeId == fixture.BookA).Select(n => n.Id).ToList();
        var oneLevel = db.BeatNodes.Where(bn => oneLevelChildren.Contains(bn.NodeId)).Select(bn => bn.BeatId).ToList();

        Assert.That(oneLevel, Has.Count.LessThan(fixture.BeatsA.Count),
            "The deep branch is gone: a one-level descent now finds every beat, so this fixture can " +
            "no longer catch the single most common bug in this codebase.");
        Assert.That(oneLevel, Is.Not.Empty,
            "The flat branch is gone: the fixture should also prove the older chapter-holds-beats " +
            "shape still works.");
    }

    [Test]
    public void TheLadderIsFullyRepresented()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        using var db = fixture.Factory.CreateDbContext();

        Assert.Multiple(() =>
        {
            Assert.That(db.Nodes.Any(n => n.Id == fixture.Sequence && n is SequenceNode), Is.True, "Sequence layer missing.");
            Assert.That(db.Nodes.Any(n => n.Id == fixture.Scene && n is SceneNode), Is.True, "Scene layer missing.");
            Assert.That(db.Nodes.Any(n => n.Id == fixture.Sequel && n is SceneNode), Is.True, "Sequel must be a SceneNode.");
        });

        // A sequel is a scene structurally; only its Kind distinguishes its dramatic job.
        var sequelKind = db.Nodes.Where(n => n.Id == fixture.Sequel).Select(n => n.Kind).Single();
        Assert.That(sequelKind, Is.EqualTo("sequel"),
            "The sequel lost its Kind label, so scene and sequel are no longer distinguishable.");
    }

    [Test]
    public void TheDecoyUniverseCollidesOnNameAndSlug()
    {
        using var _ = CanonFixture.Unscoped();
        using var db = fixture.Factory.CreateDbContext();

        var books = db.Nodes.IgnoreQueryFilters()
            .Where(n => n.Slug == CanonFixture.SharedBookSlug).ToList();
        Assert.That(books, Has.Count.EqualTo(2),
            "Both universes must carry the same book slug, so a lookup that forgets the universe " +
            "filter returns a WRONG row instead of an empty set — a far louder failure.");

        var characters = db.Entities.IgnoreQueryFilters()
            .Where(e => e.Name == CanonFixture.SharedCharacterName).ToList();
        Assert.That(characters, Has.Count.EqualTo(2), "The colliding character name is gone.");

        // And the decoy hangs a beat directly off its book node — the row a filterless
        // book-id join would wrongly pick up.
        Assert.That(db.BeatNodes.IgnoreQueryFilters().Any(bn => bn.NodeId == fixture.BookB), Is.True,
            "Universe B must keep a beat directly on its book node.");
    }

    [Test]
    public void AnUnscopedReadSeesBothUniverses()
    {
        using var db = fixture.Factory.CreateDbContext();

        int scoped;
        using (CanonFixture.ScopeTo(CanonFixture.UniverseA))
            scoped = db.Nodes.Count();

        int unscoped;
        using (CanonFixture.Unscoped())
            unscoped = db.Nodes.Count();

        Assert.That(UniverseScope.EffectiveId, Is.Not.EqualTo(CanonFixture.UniverseA),
            "Scope leaked out of the using block — every later test would fail at random.");
        Assert.That(unscoped, Is.GreaterThan(scoped),
            "With no universe wired, ScopedUniverseId is Guid.Empty and every filter switches off, " +
            "so an unscoped read must see more than a scoped one. If these are equal the leak " +
            "control is not testing anything.");
    }

    /// <summary>EF's async helpers are awkward inside sync NUnit bodies; the fixture is tiny and
    /// in-memory, so blocking is fine here and keeps the assertions readable.</summary>
    private static T await_<T>(Task<T> task) => task.GetAwaiter().GetResult();
}
