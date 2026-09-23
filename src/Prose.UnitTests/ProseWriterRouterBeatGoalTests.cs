using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The rule that replaced the locked-pipeline gate (author ruling 2026-09-22: the book is the
/// beats, drawing on entities — there is no outline or structural blueprint behind a write). A beat
/// is written from its own goal: the caller's, else the beat's Description, else its Title. A beat
/// with none of the three has nothing to be written from and is refused before any enrichment or
/// LLM call.
///
/// <para>Same minimal-router pattern the deleted gate tests used: a real <c>dbFactory</c> and a
/// deliberately null <c>generator</c>, so a write that gets PAST the rule fails later with
/// something other than the rule's own <see cref="InvalidOperationException"/>.</para>
/// </summary>
[TestFixture]
public class ProseWriterRouterBeatGoalTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-beatgoal-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private ProseWriterRouter BuildRouter() => new(
        generator:    null!,
        methodology:  new StoryMethodologyService(),
        modeDetector: new BeatModeDetector(null!),
        monitor:      new WorkflowMonitorService(dbFactory),
        log:          NullLogger<ProseWriterRouter>.Instance,
        dbFactory:    dbFactory);

    private async Task<Guid> SeedBeatAsync(string? title, string? description)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = new Beat
        {
            Id = Guid.CreateVersion7(),
            Number = new Random().Next(1, 999_999),
            Text = "",
            Title = title,
            Description = description,
        };
        db.Beats.Add(beat);
        await db.SaveChangesAsync();
        return beat.Id;
    }

    private static bool IsTheRefusal(Exception? ex) =>
        ex is InvalidOperationException ioe && ioe.Message.Contains("no Description and no Title");

    [Test]
    public async Task A_beat_with_no_description_and_no_title_is_refused()
    {
        var beatId = await SeedBeatAsync(title: null, description: null);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildRouter().WriteAsync(new BeatContext { BeatGoal = "" }, beatId));

        Assert.That(IsTheRefusal(ex), Is.True, ex?.Message);
    }

    [Test]
    public void A_preview_write_with_no_goal_is_refused()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            BuildRouter().WriteAsync(new BeatContext { BeatGoal = "   " }));

        Assert.That(IsTheRefusal(ex), Is.True, ex?.Message);
    }

    [Test]
    public async Task A_title_alone_is_enough_to_write_from()
    {
        var beatId = await SeedBeatAsync(title: "Kyle takes the stairs", description: null);

        var ex = Assert.CatchAsync<Exception>(() =>
            BuildRouter().WriteAsync(new BeatContext { BeatGoal = "" }, beatId));

        Assert.That(IsTheRefusal(ex), Is.False, "a titled beat must get past the rule");
    }

    [Test]
    public async Task A_description_is_enough_to_write_from()
    {
        var beatId = await SeedBeatAsync(title: null, description: "Kyle climbs to the roof and waits.");

        var ex = Assert.CatchAsync<Exception>(() =>
            BuildRouter().WriteAsync(new BeatContext { BeatGoal = "" }, beatId));

        Assert.That(IsTheRefusal(ex), Is.False, "a described beat must get past the rule");
    }

    [Test]
    public void A_caller_supplied_goal_is_never_refused()
    {
        var ex = Assert.CatchAsync<Exception>(() =>
            BuildRouter().WriteAsync(new BeatContext { BeatGoal = "Something happens." }));

        Assert.That(IsTheRefusal(ex), Is.False);
    }
}
