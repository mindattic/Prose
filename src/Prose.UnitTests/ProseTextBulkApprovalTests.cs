using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T918: bulk approval and publishing of game texts (<see cref="ProseTextService.BulkApproveAsync"/>,
/// <see cref="ProseTextService.BulkPublishAsync"/>, docs/CONDUIT.md §5). Locks in the kind / key-prefix / about
/// filters, the author-only gate, the dry run that changes nothing, the per-text re-check that refuses one
/// draft and approves the rest, and one texts.published Outbox notice per publish batch.
/// </summary>
[TestFixture]
public class ProseTextBulkApprovalTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private OutboxService outbox = null!;
    private ProseTextService svc = null!;
    private UniverseInterchangeService interchange = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-prose-text-bulk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "prose-text-bulk");
        outbox = new OutboxService(dbFactory);
        svc = new ProseTextService(dbFactory, outbox);
        interchange = new UniverseInterchangeService(dbFactory);

        var seed = await interchange.ImportAsync(JsonSerializer.Serialize(new
        {
            universe = new { id = "gridgame", name = "GridGame" },
            entities = new object[]
            {
                new { id = "class-ronin", type = "class", name = "Ronin", summary = "A masterless sword." },
                new { id = "class-lich", type = "class", name = "Lich", summary = "A dead mage." },
                new { id = "faction-ember", type = "faction", name = "Ember", summary = "The red rivers." },
            },
        }));
        Assert.That(seed.Success, Is.True, string.Join("; ", seed.Errors));

        // Three class descriptions, one about the Ember, and a glossary term: all drafts.
        foreach (var json in new[]
        {
            Text("description-class-ronin", "description", "class:ronin", new { @short = "A masterless sword." }, about: ["class-ronin"]),
            Text("description-class-lich", "description", "class:lich", new { @short = "A dead mage." }, about: ["class-lich"]),
            Text("description-faction-ember", "description", "faction:ember", new { @short = "The red rivers." }, about: ["faction-ember"]),
            Text("glossary-burning", "glossary", "glossary:burning", new { term = "Burning", definition = "On fire." }),
        })
        {
            var r = await svc.DraftAsync("gridgame", json, "agent");
            Assert.That(r.Success, Is.True, r.Id + ": " + string.Join("; ", r.Errors));
        }
        await outbox.DrainAsync("gridgame"); // the import's own notice
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static string Text(string id, string kind, string key, object payload, string[]? about = null, string[]? speakers = null) =>
        JsonSerializer.Serialize(new { id, kind, key, about = about ?? Array.Empty<string>(), speakers = speakers ?? Array.Empty<string>(), payload });

    private async Task<string[]> Statuses(string kind) =>
        (await svc.ListAsync("gridgame", kind: kind, status: "all")).OrderBy(d => d.Id).Select(d => $"{d.Id}:{d.Status}").ToArray();

    [Test]
    public async Task Bulk_approve_takes_every_draft_of_one_kind_and_nothing_else()
    {
        var r = await svc.BulkApproveAsync("gridgame", "description", "ryan");
        Assert.That(r.Success, Is.True, string.Join("; ", r.Errors.Concat(r.Refused.SelectMany(x => x.Errors))));
        Assert.That(r.Approved.Select(i => i.Id), Is.EquivalentTo(new[]
            { "description-class-ronin", "description-class-lich", "description-faction-ember" }));
        Assert.That(r.Published, Is.Empty, "approval alone publishes nothing");

        var pulled = await svc.ListAsync("gridgame");
        Assert.That(pulled.Select(d => d.Kind).Distinct(), Is.EqualTo(new[] { "description" }), "the glossary draft is untouched");
        Assert.That(pulled.All(d => d.Status == "approved" && d.ApprovedBy == "ryan"), Is.True);
        Assert.That(await Statuses("glossary"), Is.EqualTo(new[] { "glossary-burning:draft" }));
        Assert.That(await outbox.DrainAsync("gridgame"), Is.Empty, "approval sends no notice");
    }

    [Test]
    public async Task Key_prefix_and_about_narrow_the_batch()
    {
        var byPrefix = await svc.BulkApproveAsync("gridgame", "description", "ryan", keyPrefix: "class:", dryRun: true);
        Assert.That(byPrefix.Approved.Select(i => i.Id), Is.EquivalentTo(new[] { "description-class-ronin", "description-class-lich" }));

        var byAbout = await svc.BulkApproveAsync("gridgame", "description", "ryan", about: "faction-ember", dryRun: true);
        Assert.That(byAbout.Approved.Select(i => i.Id), Is.EqualTo(new[] { "description-faction-ember" }));

        var both = await svc.BulkApproveAsync("gridgame", "description", "ryan", keyPrefix: "class:", about: "class-lich");
        Assert.That(both.Approved.Select(i => i.Id), Is.EqualTo(new[] { "description-class-lich" }));
        Assert.That(await Statuses("description"), Is.EqualTo(new[]
            { "description-class-lich:approved", "description-class-ronin:draft", "description-faction-ember:draft" }));
    }

    [Test]
    public async Task Dry_run_lists_the_batch_and_changes_nothing()
    {
        var before = await Statuses("description");
        var dry = await svc.BulkApproveAsync("gridgame", "description", "ryan", dryRun: true, publish: true);
        Assert.That(dry.DryRun, Is.True);
        Assert.That(dry.Approved, Has.Count.EqualTo(3));
        Assert.That(dry.Published, Has.Count.EqualTo(3), "the dry run shows what --publish would publish");
        Assert.That(await Statuses("description"), Is.EqualTo(before));
        Assert.That(await outbox.DrainAsync("gridgame"), Is.Empty, "a dry run sends no notice");

        await svc.BulkApproveAsync("gridgame", "description", "ryan");
        var dryPublish = await svc.BulkPublishAsync("gridgame", "description", "ryan", dryRun: true);
        Assert.That(dryPublish.Published, Has.Count.EqualTo(3));
        Assert.That((await svc.ListAsync("gridgame", status: "published")), Is.Empty);
        Assert.That(await outbox.DrainAsync("gridgame"), Is.Empty);
    }

    [Test]
    public async Task Bulk_calls_are_author_only_and_need_a_known_kind()
    {
        var noBy = await svc.BulkApproveAsync("gridgame", "description", " ");
        Assert.That(noBy.Errors, Has.Some.Contains("approver's name"));
        var noKind = await svc.BulkApproveAsync("gridgame", "", "ryan");
        Assert.That(noKind.Errors, Has.Some.Contains("needs a kind"));
        var badKind = await svc.BulkPublishAsync("gridgame", "poem", "ryan");
        Assert.That(badKind.Errors, Has.Some.Contains("kind 'poem'"));
        var noPublisher = await svc.BulkPublishAsync("gridgame", "description", "");
        Assert.That(noPublisher.Errors, Has.Some.Contains("publisher's name"));
        var unknownUniverse = await svc.BulkApproveAsync("nowhere", "description", "ryan");
        Assert.That(unknownUniverse.Errors, Has.Some.Contains("unknown universe"));
        Assert.That((await svc.ListAsync("gridgame", status: "draft")).Count, Is.EqualTo(4), "a refused request changes nothing");
    }

    [Test]
    public async Task A_draft_that_fails_its_recheck_is_refused_and_the_rest_are_approved()
    {
        // The Lich is archived after its description was drafted: that draft now references a missing entity.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var lich = await db.Entities.IgnoreQueryFilters().SingleAsync(e => e.Slug == "class-lich");
            lich.Status = "archived";
            await db.SaveChangesAsync();
        }

        var r = await svc.BulkApproveAsync("gridgame", "description", "ryan", publish: true);
        Assert.That(r.Errors, Is.Empty);
        Assert.That(r.Success, Is.False, "a refusal makes the call unsuccessful");
        Assert.That(r.Refused.Single().Id, Is.EqualTo("description-class-lich"));
        Assert.That(r.Refused.Single().Errors, Has.Some.Contains("'class-lich' does not exist"));
        Assert.That(r.Approved.Select(i => i.Id), Is.EquivalentTo(new[] { "description-class-ronin", "description-faction-ember" }));
        Assert.That(await Statuses("description"), Is.EqualTo(new[]
            { "description-class-lich:draft", "description-class-ronin:published", "description-faction-ember:published" }));
    }

    [Test]
    public async Task Publish_batches_send_one_notice_each()
    {
        var approveAndPublish = await svc.BulkApproveAsync("gridgame", "description", "ryan", keyPrefix: "class:", publish: true);
        Assert.That(approveAndPublish.Published, Has.Count.EqualTo(2));
        var first = await outbox.DrainAsync("gridgame");
        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].Kind, Is.EqualTo("texts.published"));
        Assert.That(first[0].DataJson, Does.Contain("description-class-ronin").And.Contain("description-class-lich").And.Contain("sha256:"));

        await svc.BulkApproveAsync("gridgame", "description", "ryan");
        var publish = await svc.BulkPublishAsync("gridgame", "description", "ryan");
        Assert.That(publish.Published.Select(i => i.Id), Is.EqualTo(new[] { "description-faction-ember" }), "already published texts are left alone");
        Assert.That(await outbox.DrainAsync("gridgame"), Has.Count.EqualTo(1));

        var again = await svc.BulkPublishAsync("gridgame", "description", "ryan");
        Assert.That(again.Published, Is.Empty);
        Assert.That(await outbox.DrainAsync("gridgame"), Is.Empty, "nothing new, no notice");
        Assert.That((await svc.ListAsync("gridgame", status: "published")).Count, Is.EqualTo(3));
    }

    [Test]
    public async Task Retired_texts_are_left_out_of_bulk_calls()
    {
        await svc.ApproveAsync("gridgame", ["description-class-ronin"], "ryan");
        await svc.RetireAsync("gridgame", ["description-class-ronin"], "ryan");
        await svc.DraftAsync("gridgame", Text("description-class-ronin", "description", "class:ronin",
            new { @short = "A masterless sword, again." }, about: ["class-ronin"]));

        var r = await svc.BulkApproveAsync("gridgame", "description", "ryan", publish: true);
        Assert.That(r.Approved.Select(i => i.Id), Does.Not.Contain("description-class-ronin"),
            "bringing a retired text back stays a single, explicit approval");
        Assert.That((await svc.ListAsync("gridgame", status: "retired")).Single().Id, Is.EqualTo("description-class-ronin"));
    }
}
