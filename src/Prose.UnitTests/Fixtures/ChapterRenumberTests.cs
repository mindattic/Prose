using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests.Fixtures;

/// <summary>
/// Chapter numbers must run consecutively in reading order, and unnumbered units must keep both
/// their titles and their places.
/// </summary>
[TestFixture]
[Category("Sqlite")]
[NonParallelizable]
public class ChapterRenumberTests
{
    private CanonFixture fixture = null!;
    private ChapterRenumberService svc = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = CanonFixture.Create();
        svc = new ChapterRenumberService(fixture.Factory, NullLogger<ChapterRenumberService>.Instance);
    }

    [TearDown]
    public void TearDown() => fixture.Dispose();

    /// <summary>Rebuild the book's children as a gapped sequence with an interlude wedged in.</summary>
    private async Task GivenGappedNumberingAsync()
    {
        await using var db = fixture.Factory.CreateDbContext();
        var deep = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.DeepChapter);
        var flat = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.FlatChapter);
        deep.Title = "Chapter 1 — Teeth";
        flat.Title = "Chapter 5 — The Carousel";   // the gap a reader notices

        db.Nodes.Add(new ChapterNode
        {
            Id = Guid.CreateVersion7(), UniverseId = CanonFixture.UniverseA,
            ParentNodeId = fixture.BookA, Title = "Interlude: The Room After",
            Slug = "interlude-room-after", SortKey = 150,   // between the two
        });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task ClosesTheGapAndLeavesUnnumberedUnitsAlone()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivenGappedNumberingAsync();

        var report = await svc.RenumberAsync(fixture.BookA, apply: true);

        Assert.That(report.Numbered, Has.Count.EqualTo(2));
        Assert.That(report.Unnumbered, Is.EqualTo(new[] { "Interlude: The Room After" }));

        await using var db = fixture.Factory.CreateDbContext();
        var titles = await db.Nodes.IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == fixture.BookA).OrderBy(n => n.SortKey)
            .Select(n => n.Title).ToListAsync();

        Assert.That(titles, Is.EqualTo(new[]
        {
            "Chapter 1 — Teeth",
            "Interlude: The Room After",   // keeps its title AND its place between the chapters
            "Chapter 2 — The Carousel",    // was "Chapter 5"
        }));
    }

    [Test]
    public async Task RenumbersABareChapterTitle_SoNoTwoChaptersShareANumber()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            (await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.DeepChapter)).Title = "Chapter 3";
            (await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.FlatChapter)).Title = "Chapter 5 -- The Carousel";
            await db.SaveChangesAsync();
        }

        await svc.RenumberAsync(fixture.BookA, apply: true);

        await using var read = fixture.Factory.CreateDbContext();
        var titles = await read.Nodes.IgnoreQueryFilters()
            .Where(n => n.ParentNodeId == fixture.BookA).OrderBy(n => n.SortKey)
            .Select(n => n.Title).ToListAsync();
        Assert.That(titles, Is.EqualTo(new[] { "Chapter 1", "Chapter 2 — The Carousel" }));
    }

    [Test]
    public async Task IsIdempotent()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivenGappedNumberingAsync();

        await svc.RenumberAsync(fixture.BookA, apply: true);
        var second = await svc.RenumberAsync(fixture.BookA, apply: false);

        Assert.That(second.Changes, Is.Zero,
            "A correctly numbered book must propose nothing, or the tool can never be trusted to be done.");
    }

    [Test]
    public async Task ADryRunWritesNothing()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await GivenGappedNumberingAsync();

        var report = await svc.RenumberAsync(fixture.BookA, apply: false);
        Assert.That(report.Changes, Is.GreaterThan(0));

        await using var db = fixture.Factory.CreateDbContext();
        var flat = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.FlatChapter);
        Assert.That(flat.Title, Is.EqualTo("Chapter 5 — The Carousel"), "Dry run must not write.");
    }

    [Test]
    public async Task NormalisesTheSeparatorToTheHouseEmDash()
    {
        using var _ = CanonFixture.ScopeTo(CanonFixture.UniverseA);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var deep = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == fixture.DeepChapter);
            deep.Title = "Chapter 1 - Teeth";      // hyphen, not the house em dash
            await db.SaveChangesAsync();
        }

        await svc.RenumberAsync(fixture.BookA, apply: true);

        await using var db2 = fixture.Factory.CreateDbContext();
        var title = await db2.Nodes.IgnoreQueryFilters()
            .Where(n => n.Id == fixture.DeepChapter).Select(n => n.Title).FirstAsync();
        Assert.That(title, Is.EqualTo("Chapter 1 — Teeth"),
            "A title that drifted off the house style must be recognised as numbered, not skipped.");
    }
}
