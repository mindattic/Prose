using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests.Fixtures;

/// <summary>
/// "Which book does this belong to?" — asked everywhere, and answered six different ways before
/// these tests existed.
///
/// <para>Two of the private copies walked up to the tree root instead of to a book. That is only
/// the same answer when the book has no parent; several real books sit under a series node, and for
/// those the root walk returns the series, so every book-scoped operation downstream quietly
/// addressed the wrong node. A third matched on <c>Kind</c>, the free-form display label, rather
/// than on the node type.</para>
/// </summary>
[TestFixture]
[Category("Sqlite")]
[NonParallelizable] // UniverseScope.Current is a process-wide mutable static.
public class BookAncestorResolutionTests
{
    private CanonFixture fixture = null!;

    [SetUp]
    public void SetUp() => fixture = CanonFixture.Create();

    [TearDown]
    public void TearDown() => fixture.Dispose();

    [Test]
    public async Task ResolvesThroughTheFullLadderFromASceneBeat()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        // This beat sits under Scene → Sequence → Chapter → Book: four hops, not one.
        var beat = fixture.DeepBeats[0];
        var book = await NodeWorkbenchService.ResolveBookAncestorForBeatAsync(db, beat);

        Assert.That(book, Is.EqualTo(fixture.BookA),
            "A beat inside a scene inside a sequence must still resolve to its book.");
    }

    [Test]
    public async Task StopsAtTheBookRatherThanTheSeries()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        var book = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, fixture.Scene);

        Assert.That(book, Is.EqualTo(fixture.BookA));
        Assert.That(book, Is.Not.EqualTo(fixture.SeriesA),
            "Walking to the tree root returns the series. This is the bug that made book-scoped " +
            "work address the wrong node for every book in a series.");
    }

    [Test]
    public async Task StillResolvesTheFlatChapterShape()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        var book = await NodeWorkbenchService.ResolveBookAncestorForBeatAsync(db, fixture.BeatsA[3]);

        Assert.That(book, Is.EqualTo(fixture.BookA),
            "A beat hanging straight off a chapter must resolve exactly as a deeply nested one does.");
    }

    [Test]
    public async Task ResolvesAcrossUniversesRegardlessOfAmbientScope()
    {
        // Scoped to A while asking about a node in B: the id is explicit, so the ambient scope is
        // irrelevant. Without IgnoreQueryFilters the node looks parentless and the answer is null.
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        var book = await NodeWorkbenchService.ResolveBookAncestorForBeatAsync(db, fixture.BeatOnBookB);

        Assert.That(book, Is.EqualTo(fixture.BookB),
            "An explicit id must resolve even when the ambient universe is a different one.");
    }

    [Test]
    public async Task ReturnsNullWhenThereIsNoBookAbove()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        var book = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, fixture.SeriesA);

        Assert.That(book, Is.Null,
            "A series has no book above it. Returning the series itself would let a caller treat " +
            "it as a book.");
    }

    [Test]
    public async Task ABookResolvesToItself()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        Assert.That(await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, fixture.BookA),
            Is.EqualTo(fixture.BookA));
    }

    [Test]
    public async Task TheNodeTypeDecides_NotTheKindLabel()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using var db = fixture.Factory.CreateDbContext();

        // A chapter mislabelled Kind="book" must NOT be mistaken for one: NodeType is the
        // structural truth, Kind is a display string anyone can set.
        var chapter = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.FlatChapter);
        chapter.Kind = "book";
        await db.SaveChangesAsync();

        var book = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, fixture.FlatChapter);

        Assert.That(book, Is.EqualTo(fixture.BookA),
            "Resolution keyed on the Kind label would stop at the mislabelled chapter.");
    }
}
