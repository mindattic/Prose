using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Prose.Cli;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.UnitTests;

/// <summary>
/// <c>prose --set-timeline</c> (PRS-T320): where a book sits on its universe's chronology.
/// Covers the direct set, the tie (<c>--same-as</c>), ordinary bisection, cross-universe refusal,
/// and the restripe fallback once a bracket's bisection room has collapsed.
/// </summary>
[TestFixture]
public class BookTimelineCliTests
{
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ServiceProvider services = null!;
    private Guid universeId;
    private readonly HashSet<Guid> seededUniverses = new();

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ss_timeline_{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        paths = new TestPathProviderWithRoot(root);
        dbFactory = TestDbFactory.For(paths, "timeline-cli");
        var sc = new ServiceCollection();
        sc.AddSingleton(dbFactory);
        services = sc.BuildServiceProvider();
        universeId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        services.Dispose();
        TestDbFactory.Reset(paths);
        try { Directory.Delete(paths.DataRoot, recursive: true); } catch { /* best effort */ }
    }

    private async Task<Guid> SeedBookAsync(string slug, decimal? timeline = null, Guid? universe = null)
    {
        var uid = universe ?? universeId;
        var id = Guid.CreateVersion7();
        await using var db = await dbFactory.CreateDbContextAsync();
        if (seededUniverses.Add(uid))
            db.Universes.Add(new Universe { Id = uid, Slug = "u-" + uid.ToString("N")[..8], Name = "U" });
        db.Nodes.Add(new BookNode { Id = id, Slug = slug, Title = slug, Kind = "book", UniverseId = uid, Timeline = timeline, SortKey = 100 });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<decimal?> TimelineOfAsync(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return (await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == id)).Timeline;
    }

    [Test]
    public async Task SetTimeline_Direct_WritesExactValue()
    {
        await SeedBookAsync("book-a");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-a", "--value", "1.5"], services);
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(await TimelineOfAsync(await ResolveAsync("book-a")), Is.EqualTo(1.5m));
    }

    [Test]
    public async Task SameAs_TiesExactly()
    {
        await SeedBookAsync("book-a", 1.2m);
        await SeedBookAsync("book-b");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-b", "--same-as", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(await TimelineOfAsync(await ResolveAsync("book-b")), Is.EqualTo(1.2m));
    }

    [Test]
    public async Task After_NoNextSibling_PicksAValueStrictlyInsideTheBracket()
    {
        await SeedBookAsync("book-a", 1.2m);
        await SeedBookAsync("book-c");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-c", "--after", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));
        var value = await TimelineOfAsync(await ResolveAsync("book-c"));
        Assert.That(value, Is.Not.Null);
        Assert.That(value!.Value, Is.GreaterThan(1.2m));
        Assert.That(value.Value, Is.LessThan(2.0m));
    }

    [Test]
    public async Task After_BetweenTwoAdjacentBooks_BisectsTheGap()
    {
        await SeedBookAsync("book-a", 1.2m);
        await SeedBookAsync("book-b", 1.3m);
        await SeedBookAsync("book-c");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-c", "--after", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));
        var value = await TimelineOfAsync(await ResolveAsync("book-c"));
        Assert.That(value!.Value, Is.GreaterThan(1.2m));
        Assert.That(value.Value, Is.LessThan(1.3m));
    }

    [Test]
    public async Task Before_WithRoomAboveTheBracketFloor_StaysInsideTheBracket()
    {
        await SeedBookAsync("book-a", 1.05m);
        await SeedBookAsync("book-z");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-z", "--before", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));
        var value = await TimelineOfAsync(await ResolveAsync("book-z"));
        Assert.That(value!.Value, Is.GreaterThanOrEqualTo(1.0m));
        Assert.That(value.Value, Is.LessThan(1.05m));
    }

    [Test]
    public async Task Before_FirstBookSittingExactlyAtTheBracketFloor_RestripesToMakeRoomInsideTheEra()
    {
        await SeedBookAsync("book-a", 1.0m);
        await SeedBookAsync("book-z");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-z", "--before", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));
        var a = await TimelineOfAsync(await ResolveAsync("book-a"));
        var z = await TimelineOfAsync(await ResolveAsync("book-z"));
        Assert.That(z, Is.GreaterThanOrEqualTo(1.0m));
        Assert.That(z, Is.LessThan(a));
        Assert.That(a, Is.LessThan(2.0m));
    }

    [Test]
    public async Task Restripe_WithMoreThanTenDistinctValues_NeverCrossesTheIntegerBoundary()
    {
        // Twelve distinct rungs plus a collapsed gap: a 0.1 ladder would push the last ones past 2.0.
        for (int i = 0; i < 12; i++)
            await SeedBookAsync($"book-{i:00}", 1.5m + i * 0.000001m);
        await SeedBookAsync("book-new");

        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-new", "--after", "book-05"], services);
        Assert.That(exit, Is.EqualTo(0));

        await using var db = await dbFactory.CreateDbContextAsync();
        var values = await db.Nodes.IgnoreQueryFilters().Where(n => n.UniverseId == universeId)
            .Select(n => n.Timeline!.Value).ToListAsync();
        Assert.That(values.All(v => v >= 1.0m && v < 2.0m), Is.True, string.Join(", ", values));
        var five = await TimelineOfAsync(await ResolveAsync("book-05"));
        var six = await TimelineOfAsync(await ResolveAsync("book-06"));
        var placed = await TimelineOfAsync(await ResolveAsync("book-new"));
        Assert.That(placed, Is.GreaterThan(five));
        Assert.That(placed, Is.LessThan(six));
    }

    [Test]
    public async Task NoModeFlag_PrintsWithoutWriting()
    {
        await SeedBookAsync("book-a", 1.2m);
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));
        Assert.That(await TimelineOfAsync(await ResolveAsync("book-a")), Is.EqualTo(1.2m));
    }

    [Test]
    public async Task After_NeighborTooCloseToBisect_RestripesThenPlacesWithoutLooping()
    {
        // book-d sits a hair above book-a — closer than MinGap, so a plain bisect has nowhere to
        // go until the bracket is restriped onto a fresh 0.1 ladder.
        await SeedBookAsync("book-a", 1.1m);
        await SeedBookAsync("book-d", 1.100008m);
        await SeedBookAsync("book-c");

        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-c", "--after", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));

        var a = await TimelineOfAsync(await ResolveAsync("book-a"));
        var c = await TimelineOfAsync(await ResolveAsync("book-c"));
        var d = await TimelineOfAsync(await ResolveAsync("book-d"));
        Assert.That(a, Is.LessThan(c));
        Assert.That(c, Is.LessThan(d));
    }

    [Test]
    public async Task After_RestripeNeeded_PreservesAnExistingTie()
    {
        // book-a and book-b are tied (nothing in the prose orders them); book-d sits too close to
        // bisect against, forcing a restripe — the a/b tie must survive it.
        await SeedBookAsync("book-a", 1.1m);
        await SeedBookAsync("book-b", 1.1m);
        await SeedBookAsync("book-d", 1.100008m);
        await SeedBookAsync("book-c");

        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-c", "--after", "book-a"], services);
        Assert.That(exit, Is.EqualTo(0));

        var a = await TimelineOfAsync(await ResolveAsync("book-a"));
        var b = await TimelineOfAsync(await ResolveAsync("book-b"));
        var c = await TimelineOfAsync(await ResolveAsync("book-c"));
        var d = await TimelineOfAsync(await ResolveAsync("book-d"));
        Assert.That(a, Is.EqualTo(b), "the tie between A and B must survive the restripe");
        Assert.That(c, Is.GreaterThan(a));
        Assert.That(d, Is.GreaterThan(c));
    }

    [Test]
    public async Task DifferentUniverses_Refused()
    {
        var otherUniverse = Guid.NewGuid();
        await SeedBookAsync("book-a", 1.0m);
        await SeedBookAsync("book-x", universe: otherUniverse);
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-x", "--after", "book-a"], services);
        Assert.That(exit, Is.EqualTo(1));
    }

    [Test]
    public async Task ReferenceHasNoTimelineYet_Refused()
    {
        await SeedBookAsync("book-a");
        await SeedBookAsync("book-b");
        var exit = await BookTimelineCli.RunAsync(["--set-timeline", "--slug", "book-b", "--after", "book-a"], services);
        Assert.That(exit, Is.EqualTo(1));
    }

    private async Task<Guid> ResolveAsync(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return (await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Slug == slug)).Id;
    }
}
