using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;
using static Prose.Core.Services.UniverseInterchangeService;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T916 / RFC 0017: replace imports retire what the consumer deleted (never deleting, never touching
/// Prose-authored entities or texts), a batched push is one session with one prune and one Outbox notice,
/// unchanged entities are not rewritten, the provenance block round-trips, and exported relations come from
/// the live edges.
/// </summary>
[TestFixture]
public class UniverseImportModesTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private OutboxService outbox = null!;
    private UniverseInterchangeService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-import-modes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "import-modes");
        outbox = new OutboxService(dbFactory);
        svc = new UniverseInterchangeService(dbFactory, outbox);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static string File(params (string id, string type, string[] rel)[] entities) => File(null, entities);

    private static string File(object? source, params (string id, string type, string[] rel)[] entities) =>
        JsonSerializer.Serialize(new
        {
            universe = new { id = "game", name = "Game" },
            source,
            entities = entities.Select(e => new
            {
                id = e.id, type = e.type, name = e.id.ToUpperInvariant(), summary = "s " + e.id,
                relations = e.rel.Select(r => new { to = r, kind = "knows" }),
                tags = new[] { "t" },
            }),
        });

    private async Task<string> StatusOf(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Entities.IgnoreQueryFilters().Where(e => e.Slug == slug).Select(e => e.Status).SingleAsync();
    }

    [Test]
    public async Task Merge_never_retires_and_replace_retires_what_the_file_dropped()
    {
        await svc.ImportAsync(File(("a", "character", ["b"]), ("b", "character", []), ("c", "place", [])));

        var merge = await svc.ImportAsync(File(("a", "character", ["b"]), ("b", "character", [])));
        Assert.That(merge.EntitiesRetired, Is.EqualTo(0));
        Assert.That(await StatusOf("c"), Is.EqualTo("canon"));

        var replace = await svc.ImportAsync(File(("a", "character", []), ("b", "character", [])), null, new ImportOptions(Mode: ImportModes.Replace));
        Assert.That(replace.Success, Is.True, string.Join("; ", replace.Errors));
        Assert.That(replace.PruneBasis, Is.EqualTo("manifest"));
        Assert.That(replace.Retired, Is.EqualTo(new[] { "c" }));
        Assert.That(replace.EdgesInvalidated, Is.EqualTo(1), "a -> b is no longer listed");
        Assert.That(await StatusOf("c"), Is.EqualTo("archived"), "retired, not deleted");

        var export = await svc.ExportAsync("game");
        Assert.That(export, Does.Not.Contain("\"c\""));
        Assert.That(export, Does.Not.Contain("\"knows\""));

        var revive = await svc.ImportAsync(File(("a", "character", []), ("b", "character", []), ("c", "place", [])), null, new ImportOptions(Mode: ImportModes.Replace));
        Assert.That(revive.EntitiesRevived, Is.EqualTo(1));
        Assert.That(await StatusOf("c"), Is.EqualTo("canon"));
    }

    [Test]
    public async Task Replace_never_retires_authored_entities_or_texts()
    {
        await svc.ImportAsync(File(("a", "character", []), ("b", "character", [])));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var b = await db.Entities.IgnoreQueryFilters().SingleAsync(e => e.Slug == "b");
            b.Provenance = ClaimProvenance.Authored; // a human promoted it in Prose
            await db.SaveChangesAsync();
        }
        var texts = new ProseTextService(dbFactory);
        var draft = await texts.DraftAsync("game", JsonSerializer.Serialize(new
        {
            id = "description-a", kind = "description", key = "a", about = new[] { "a" }, payload = new { @short = "A." },
        }));
        Assert.That(draft.Success, Is.True, string.Join("; ", draft.Errors));

        var r = await svc.ImportAsync(File(("a", "character", [])), null, new ImportOptions(Mode: ImportModes.Replace));
        Assert.That(r.Retired, Is.Empty);
        Assert.That(await StatusOf("b"), Is.EqualTo("canon"));
        Assert.That(await StatusOf("description-a"), Is.EqualTo("canon"));
    }

    [Test]
    public async Task Replace_without_a_manifest_prunes_against_imported_records()
    {
        await svc.ImportAsync(File(("a", "character", []), ("old", "character", [])));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Settings.RemoveRange(db.Settings.IgnoreQueryFilters().Where(s => s.Key == "interchange.manifest"));
            await db.SaveChangesAsync();
        }
        var r = await svc.ImportAsync(File(("a", "character", [])), null, new ImportOptions(Mode: ImportModes.Replace));
        Assert.That(r.PruneBasis, Is.EqualTo("records"));
        Assert.That(r.Retired, Is.EqualTo(new[] { "old" }));
    }

    [Test]
    public async Task A_batched_session_prunes_once_over_every_batch_and_sends_one_notice()
    {
        await svc.ImportAsync(File(("a", "character", []), ("b", "character", []), ("gone", "character", [])));
        await outbox.DrainAsync("game");

        var s = "push-1";
        var b1 = await svc.ImportAsync(File(("a", "character", ["b"])), null, new ImportOptions(ImportModes.Replace, s, Final: false));
        Assert.That(b1.Retired, Is.Empty, "a non-final batch prunes nothing");
        Assert.That(await outbox.DrainAsync("game", peek: true), Is.Empty, "and sends no notice");

        var b2 = await svc.ImportAsync(File(("b", "character", [])), null, new ImportOptions(ImportModes.Replace, s, Final: true));
        Assert.That(b2.Retired, Is.EqualTo(new[] { "gone" }), "a from batch 1 survives the final prune");
        Assert.That(await StatusOf("a"), Is.EqualTo("canon"));

        var events = await outbox.DrainAsync("game");
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Summary, Does.Contain("in 2 batches").And.Contain("1 retired"));

        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(await db.Settings.IgnoreQueryFilters().AnyAsync(x => x.Key.StartsWith("interchange.session.")), Is.False, "session state is cleared");
    }

    [Test]
    public async Task A_dangling_relation_in_an_early_batch_is_claimed_by_a_later_one()
    {
        var s = "push-2";
        var b1 = await svc.ImportAsync(File(("a", "character", ["b"])), null, new ImportOptions(ImportModes.Merge, s, Final: false));
        Assert.That(b1.StubsCreated, Is.EqualTo(1));
        var b2 = await svc.ImportAsync(File(("b", "character", [])), null, new ImportOptions(ImportModes.Merge, s, Final: true));
        Assert.That(b2.StubsPromoted, Is.EqualTo(1));

        var export = JsonDocument.Parse(await svc.ExportAsync("game"));
        var a = export.RootElement.GetProperty("entities").EnumerateArray().Single(e => e.GetProperty("id").GetString() == "a");
        Assert.That(a.GetProperty("relations")[0].GetProperty("to").GetString(), Is.EqualTo("b"));
    }

    [Test]
    public async Task Reimporting_an_unchanged_file_writes_nothing()
    {
        var json = File(("a", "character", ["b"]), ("b", "character", []));
        await svc.ImportAsync(json);
        DateTime before;
        await using (var db = await dbFactory.CreateDbContextAsync())
            before = await db.Entities.IgnoreQueryFilters().Where(e => e.Slug == "a").Select(e => e.ModifiedAt).SingleAsync();

        var again = await svc.ImportAsync(json);
        Assert.That(again.EntitiesUnchanged, Is.EqualTo(2));
        Assert.That(again.EntitiesUpdated, Is.EqualTo(2));
        Assert.That(again.EdgesCreated, Is.EqualTo(0));
        await using (var db = await dbFactory.CreateDbContextAsync())
            Assert.That(await db.Entities.IgnoreQueryFilters().Where(e => e.Slug == "a").Select(e => e.ModifiedAt).SingleAsync(), Is.EqualTo(before));
    }

    [Test]
    public async Task The_source_block_round_trips()
    {
        await svc.ImportAsync(File(new { consumer = "game", build = "abc123", seed = 1977 }, ("a", "character", [])));
        var export = JsonDocument.Parse(await svc.ExportAsync("game"));
        Assert.That(export.RootElement.GetProperty("source").GetProperty("build").GetString(), Is.EqualTo("abc123"));
        Assert.That(export.RootElement.GetProperty("source").GetProperty("seed").GetInt32(), Is.EqualTo(1977));
    }

    [Test]
    public async Task Export_reports_a_relation_whose_edge_is_gone()
    {
        await svc.ImportAsync(File(("a", "character", ["b"]), ("b", "character", [])));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var edge = await db.Edges.IgnoreQueryFilters().SingleAsync();
            edge.InvalidatedAt = DateTime.UtcNow; // an import cut short, or an edge removed since
            await db.SaveChangesAsync();
        }
        var export = JsonDocument.Parse(await svc.ExportAsync("game"));
        var a = export.RootElement.GetProperty("entities").EnumerateArray().Single(e => e.GetProperty("id").GetString() == "a");
        Assert.That(a.GetProperty("relations").GetArrayLength(), Is.EqualTo(0), "the record lists it, but the edge is what Prose holds");
    }

    [Test]
    public async Task An_unknown_mode_or_bad_session_is_refused()
    {
        var json = File(("a", "character", []));
        Assert.That((await svc.ImportAsync(json, null, new ImportOptions(Mode: "wipe"))).Errors.Single(), Does.Contain("unknown import mode"));
        Assert.That((await svc.ImportAsync(json, null, new ImportOptions(Session: "bad id!"))).Errors.Single(), Does.Contain("session"));
    }
}
