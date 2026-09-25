using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// <c>prose --splice-beats</c> / MCP <c>splice_beats</c>. The pure matcher (<see cref="BeatSplice"/>)
/// is attacked directly; the guard, dry run, write and read-back (<see cref="BeatSpliceService"/>)
/// run against the real workbench on in-memory SQLite.
/// </summary>
[TestFixture]
public class BeatSpliceTests
{
    private const string Kyle = "<entity repo=\"character\" guid=\"019d6143-a648-7876-9688-0f6d38d70075\">Kyle</entity>";
    private const string Pixel = "<entity repo=\"character\" guid=\"019d6143-c1d5-e7f8-a92b-34068c150e4d\">Pixel</entity>";

    // ── pure ─────────────────────────────────────────────────────────────────

    [Test]
    public void Tags_outside_the_edit_are_kept_byte_for_byte()
    {
        var stored = $"{Kyle} sat down. The bowl was cold. {Pixel} said nothing.";
        var o = BeatSplice.Apply(stored, [new SpliceEdit(1, "The bowl was cold.", "The bowl was hot.")]);

        Assert.That(o.Failures, Is.Empty);
        Assert.That(o.Text, Is.EqualTo($"{Kyle} sat down. The bowl was hot. {Pixel} said nothing."));
        Assert.That(o.UnwrappedTags, Is.EqualTo(0));
    }

    [Test]
    public void A_tag_the_edit_overlaps_is_unwrapped_so_the_save_re_derives_it()
    {
        var stored = $"{Kyle} sat down. {Pixel} said nothing.";
        var o = BeatSplice.Apply(stored, [new SpliceEdit(1, "Kyle sat", "Kyle stood")]);

        Assert.That(o.Failures, Is.Empty);
        Assert.That(o.Text, Is.EqualTo($"Kyle stood down. {Pixel} said nothing."));
        Assert.That(o.UnwrappedTags, Is.EqualTo(1));
    }

    [Test]
    public void An_edit_ending_at_a_tag_boundary_does_not_swallow_the_tag()
    {
        var stored = $"She looked at {Kyle}.";
        var o = BeatSplice.Apply(stored, [new SpliceEdit(1, "She looked at ", "She watched ")]);

        Assert.That(o.Text, Is.EqualTo($"She watched {Kyle}."));
        Assert.That(o.UnwrappedTags, Is.EqualTo(0));
    }

    [Test]
    public void Count_mismatch_is_a_guard_failure()
    {
        var o = BeatSplice.Apply("the rain, the rain", [new SpliceEdit(7, "the rain", "rain")]);

        Assert.That(o.Failures, Has.Count.EqualTo(1));
        Assert.That(o.Failures[0], Does.StartWith("#7: expected 1 got 2"));
    }

    [Test]
    public void Count_replaces_every_occurrence_and_edits_are_counted_in_order()
    {
        var o = BeatSplice.Apply("eleven, eleven, twelve", [
            new SpliceEdit(1, "eleven", "ten", Count: 2),
            new SpliceEdit(1, "ten, ten", "ten"),          // only exists because of the edit above
        ]);

        Assert.That(o.Failures, Is.Empty);
        Assert.That(o.Text, Is.EqualTo("ten, twelve"));
    }

    [Test]
    public void Inline_markers_are_matched_literally_not_stripped()
    {
        var o = BeatSplice.Apply("He read it once. *When you're ready.* Then he sent it.",
            [new SpliceEdit(1, "*When you're ready.*", "*Whenever.*")]);

        Assert.That(o.Failures, Is.Empty);
        Assert.That(o.Text, Is.EqualTo("He read it once. *Whenever.* Then he sent it."));
    }

    [Test]
    public void Malformed_replacement_markup_is_refused()
    {
        var o = BeatSplice.Apply("plain text", [new SpliceEdit(1, "plain", "<entity guid=\"x\">plain")]);
        Assert.That(o.Failures, Has.Count.EqualTo(1));
    }

    [Test]
    public void Read_back_that_differs_from_the_promise_is_reported_with_where()
    {
        Assert.That(BeatSpliceService.VerifyReadBack("The bowl was hot.", $"The bowl was hot."), Is.Null);
        Assert.That(BeatSpliceService.VerifyReadBack($"{"Kyle"} ate.", $"{Kyle} ate."), Is.Null, "tags on read-back are not a difference");

        var miss = BeatSpliceService.VerifyReadBack("The bowl was hot.", "The bowl was cold.");
        Assert.That(miss, Does.Contain("char 13"));
    }

    // ── service: guard, dry run, write, membership ───────────────────────────

    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NodeWorkbenchService workbench = null!;
    private BeatSpliceService splicer = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-splice-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "splice");
        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        splicer = new BeatSpliceService(dbFactory, workbench,
            new EditSessionService(dbFactory, NullLogger<EditSessionService>.Instance));
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> MakeNodeAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var n = new BookNode
        {
            Id = Guid.CreateVersion7(), Slug = "splice-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Splice", Kind = "book", Status = "draft", SortKey = 100,
        };
        db.Nodes.Add(n);
        await db.SaveChangesAsync();
        return n.Id;
    }

    private async Task<(Guid Id, int Number)> AddBeatAsync(Guid node, string text)
    {
        var b = await workbench.InsertBeatAsync(node, afterBeatId: null, text);
        await using var db = await dbFactory.CreateDbContextAsync();
        return (b.Id, await db.Beats.Where(x => x.Id == b.Id).Select(x => x.Number).SingleAsync());
    }

    private async Task<string> TextOf(Guid id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return (await db.Beats.AsNoTracking().SingleAsync(x => x.Id == id)).Text ?? "";
    }

    [Test]
    public async Task One_bad_count_anywhere_aborts_the_whole_docket_with_nothing_written()
    {
        var node = await MakeNodeAsync();
        var a = await AddBeatAsync(node, "The bowl was cold.");
        var b = await AddBeatAsync(node, "It was the rain, the rain.");

        var r = await splicer.RunAsync(node, [
            new SpliceEdit(a.Number, "cold", "hot"),       // fine on its own
            new SpliceEdit(b.Number, "the rain", "rain"),  // occurs twice → guard
        ], apply: true);

        Assert.That(r.Aborted, Is.True);
        Assert.That(r.Applied, Is.False);
        Assert.That(await TextOf(a.Id), Is.EqualTo("The bowl was cold."), "the good edit must not land either");
        Assert.That(await TextOf(b.Id), Is.EqualTo("It was the rain, the rain."));
    }

    [Test]
    public async Task Dry_run_plans_but_writes_nothing()
    {
        var node = await MakeNodeAsync();
        var a = await AddBeatAsync(node, "The bowl was cold.");

        var r = await splicer.RunAsync(node, [new SpliceEdit(a.Number, "cold", "hot")], apply: false);

        Assert.That(r.Aborted, Is.False);
        Assert.That(r.Applied, Is.False);
        Assert.That(r.Results.Single().Status, Is.EqualTo("planned"));
        Assert.That(await TextOf(a.Id), Is.EqualTo("The bowl was cold."));
    }

    [Test]
    public async Task Apply_writes_each_beat_as_an_author_edit_and_verifies_it()
    {
        var node = await MakeNodeAsync();
        var a = await AddBeatAsync(node, "The bowl was cold. He ate.");

        var r = await splicer.RunAsync(node, [
            new SpliceEdit(a.Number, "cold", "hot"),
            new SpliceEdit(a.Number, " He ate.", ""),
        ], apply: true);

        Assert.That(r.Applied, Is.True);
        Assert.That(r.VerifyMisses, Is.EqualTo(0));
        Assert.That(r.Results.Single().Status, Is.EqualTo("verified"));
        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Beats.AsNoTracking().SingleAsync(x => x.Id == a.Id);
        Assert.That(row.Text, Is.EqualTo("The bowl was hot."));
        Assert.That(row.LastWriteReason, Is.EqualTo(nameof(BeatWriteReason.AuthorEdit)));
    }

    [Test]
    public async Task A_beat_number_from_another_node_is_a_guard_failure()
    {
        var node = await MakeNodeAsync();
        var other = await MakeNodeAsync();
        await AddBeatAsync(node, "Mine.");
        var theirs = await AddBeatAsync(other, "Theirs.");

        var r = await splicer.RunAsync(node, [new SpliceEdit(theirs.Number, "Theirs", "Mine")], apply: true);

        Assert.That(r.Aborted, Is.True);
        Assert.That(r.GuardFailures.Single(), Does.Contain("not a beat of this node"));
        Assert.That(await TextOf(theirs.Id), Is.EqualTo("Theirs."));
    }

    [Test]
    public void Docket_parses_count_default_and_null_new()
    {
        var d = BeatSpliceService.ParseDocket("""[{"beat":5,"old":"a","new":"b"},{"Beat":6,"old":"c","new":null,"count":3}]""");
        Assert.That(d, Is.EqualTo(new[] { new SpliceEdit(5, "a", "b", 1), new SpliceEdit(6, "c", "", 3) }));
    }

    [Test]
    public void A_misspelt_or_missing_new_is_refused_never_read_as_a_deletion()
    {
        Assert.Throws<FormatException>(() => BeatSpliceService.ParseDocket("""[{"beat":5,"old":"Grey-blue","replace":"Gray-blue"}]"""));
        Assert.Throws<FormatException>(() => BeatSpliceService.ParseDocket("""[{"beat":5,"old":"Grey-blue"}]"""));
        Assert.That(BeatSpliceService.ParseDocket("""[{"beat":5,"old":"x","new":""}]""").Single().New, Is.EqualTo(""),
            "an explicit empty new is still a deliberate deletion");
    }
}
