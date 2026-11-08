using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T925: bulk approval can target one drafting pass. A drafter tags a pass with a batch (<c>--batch v4</c>), and
/// the author narrows <see cref="ProseTextService.BulkApproveAsync"/> / <see cref="ProseTextService.BulkPublishAsync"/>
/// by batch, drafter, minimum version and drafted-since, so "approve the v4 rewrite" never sweeps in the untouched seed
/// drafts. The batch is metadata on the version (not content, not the hash); approval stays the author's.
/// </summary>
[TestFixture]
public class ProseTextBulkFilterTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ProseTextService svc = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-prose-text-bulk-filter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "prose-text-bulk-filter");
        svc = new ProseTextService(dbFactory, new OutboxService(dbFactory));

        var seed = await new UniverseInterchangeService(dbFactory).ImportAsync(JsonSerializer.Serialize(new
        {
            universe = new { id = "gridgame", name = "GridGame" },
            entities = new object[] { new { id = "character-herald", type = "character", name = "Herald", summary = "A healer." } },
        }));
        Assert.That(seed.Success, Is.True, string.Join("; ", seed.Errors));

        // The seed pass: three v1 conversations drafted by the seeder, no batch.
        foreach (var id in new[] { "a", "b", "c" })
            Assert.That((await svc.DraftAsync("gridgame", Talk(id, "Seed line."), "seeder")).Success, Is.True);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static string Talk(string id, string line) => JsonSerializer.Serialize(new
    {
        id = $"conversation-{id}", kind = "conversation", key = $"dialogue:{id}",
        speakers = new[] { "character-herald" },
        payload = new { start = "n1", nodes = new[] { new { id = "n1", speaker = "character-herald", line } } },
    });

    private static IEnumerable<string> Ids(ProseTextService.BulkResult r) => r.Approved.Select(i => i.Id).OrderBy(x => x);

    [Test]
    public async Task A_draft_records_its_batch_and_drafter_and_the_pull_shows_them()
    {
        var r = await svc.DraftAsync("gridgame", Talk("a", "The v4 line."), " claude ", " v4 ");
        Assert.That(r.Success, Is.True, string.Join("; ", r.Errors));

        var record = (await svc.GetRecordAsync("gridgame", "conversation-a"))!;
        Assert.That(record.Head!.Batch, Is.EqualTo("v4"));
        Assert.That(record.Head.CreatedBy, Is.EqualTo("claude"));
        Assert.That(record.Versions.Single(v => v.Version == 1).Batch, Is.Null, "the seed version has no batch");

        var doc = (await svc.GetAsync("gridgame", "conversation-a", 2))!;
        Assert.That(doc.Batch, Is.EqualTo("v4"));
        Assert.That(doc.DraftedBy, Is.EqualTo("claude"));
    }

    [Test]
    public async Task An_unchanged_draft_keeps_its_old_batch_so_a_pass_never_sweeps_in_what_it_did_not_touch()
    {
        var again = await svc.DraftAsync("gridgame", Talk("b", "Seed line."), "claude", "v4");
        Assert.That(again.Unchanged, Is.True);
        Assert.That((await svc.GetRecordAsync("gridgame", "conversation-b"))!.Head!.Batch, Is.Null);

        var dry = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { Batch = "v4" });
        Assert.That(dry.Approved, Is.Empty);
    }

    [Test]
    public async Task A_bad_batch_tag_refuses_the_draft()
    {
        var r = await svc.DraftAsync("gridgame", Talk("a", "New."), "claude", "v4 rewrite");
        Assert.That(r.Success, Is.False);
        Assert.That(r.Errors.Single(), Does.Contain("batch 'v4 rewrite'"));

        var tooLong = await svc.DraftAsync("gridgame", Talk("a", "New."), "claude", new string('x', ProseTextService.MaxBatchLength + 1));
        Assert.That(tooLong.Success, Is.False);
    }

    [Test]
    public async Task Batch_drafter_and_min_version_each_narrow_bulk_approval_to_the_pass()
    {
        await svc.DraftAsync("gridgame", Talk("a", "The v4 line for a."), "claude", "v4");
        await svc.DraftAsync("gridgame", Talk("b", "The v4 line for b."), "claude", "v4");
        await svc.DraftAsync("gridgame", Talk("c", "A side fix."), "codex");

        var all = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true);
        Assert.That(Ids(all), Is.EqualTo(new[] { "conversation-a", "conversation-b", "conversation-c" }));

        var byBatch = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { Batch = "V4" });
        Assert.That(Ids(byBatch), Is.EqualTo(new[] { "conversation-a", "conversation-b" }), "batch matches case-insensitively");
        Assert.That(byBatch.Batch, Is.EqualTo("V4"), "the result echoes the filter");

        var byDrafter = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { DraftedBy = "codex" });
        Assert.That(Ids(byDrafter), Is.EqualTo(new[] { "conversation-c" }));

        var both = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { Batch = "v4", DraftedBy = "codex" });
        Assert.That(both.Approved, Is.Empty, "filters combine");

        var v2Up = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { MinVersion = 2 });
        Assert.That(v2Up.Approved, Has.Count.EqualTo(3));

        var approved = await svc.BulkApproveAsync("gridgame", "conversation", "ryan",
            filter: new ProseTextService.BulkFilter { Batch = "v4", DraftedBy = "claude" });
        Assert.That(approved.Success, Is.True);
        var statuses = (await svc.ListAsync("gridgame", kind: "conversation", status: "all"))
            .OrderBy(d => d.Id).Select(d => $"{d.Id}:{d.Status}").ToArray();
        Assert.That(statuses, Is.EqualTo(new[] { "conversation-a:approved", "conversation-b:approved", "conversation-c:draft" }));
    }

    [Test]
    public async Task Min_version_leaves_out_the_untouched_seed_drafts()
    {
        await svc.DraftAsync("gridgame", Talk("a", "Rewritten."), "claude");

        var r = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { MinVersion = 2 });
        Assert.That(Ids(r), Is.EqualTo(new[] { "conversation-a" }));
    }

    [Test]
    public async Task Since_keeps_only_versions_drafted_at_or_after_the_time()
    {
        var future = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { Since = DateTime.UtcNow.AddHours(1).ToString("O") });
        Assert.That(future.Approved, Is.Empty);
        Assert.That(future.DraftedSince, Is.Not.Null);

        var past = await svc.BulkApproveAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { Since = "2020-01-01" });
        Assert.That(past.Approved, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task A_malformed_filter_is_a_request_error_that_changes_nothing()
    {
        var badSince = await svc.BulkApproveAsync("gridgame", "conversation", "ryan",
            filter: new ProseTextService.BulkFilter { Since = "last tuesday" });
        Assert.That(badSince.Errors.Single(), Does.Contain("since 'last tuesday'"));
        Assert.That(badSince.Approved, Is.Empty);

        var badVersion = await svc.BulkApproveAsync("gridgame", "conversation", "ryan",
            filter: new ProseTextService.BulkFilter { MinVersion = 0 });
        Assert.That(badVersion.Errors.Single(), Does.Contain("minVersion"));

        Assert.That((await svc.ListAsync("gridgame", status: "approved")), Is.Empty);
    }

    [Test]
    public async Task Bulk_publish_takes_the_same_filters()
    {
        await svc.DraftAsync("gridgame", Talk("a", "The v4 line for a."), "claude", "v4");
        await svc.BulkApproveAsync("gridgame", "conversation", "ryan");

        var r = await svc.BulkPublishAsync("gridgame", "conversation", "ryan", dryRun: true,
            filter: new ProseTextService.BulkFilter { Batch = "v4" });
        Assert.That(r.Published.Select(i => i.Id), Is.EqualTo(new[] { "conversation-a" }));
    }
}
