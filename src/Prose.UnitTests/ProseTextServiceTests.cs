using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Locks in RFC 0017's game texts (<see cref="ProseTextService"/>, docs/CONDUIT.md §5): the per-kind payload
/// checks, entity references, the draft → approved → published → retired lifecycle with every version kept,
/// the pull filters, one Outbox notice per publish batch, and that texts never leak into a universe export
/// or an interchange file.
/// </summary>
[TestFixture]
public class ProseTextServiceTests
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
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-prose-text-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "prose-text");
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
        await outbox.DrainAsync("gridgame"); // the import's own notice
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    private static string Conversation(string id = "conversation-recruit-ronin", string line = "Keep driving.", string next = "join") =>
        JsonSerializer.Serialize(new
        {
            schema = "prose-text/1",
            id,
            kind = "conversation",
            key = "dialogue:recruit_ronin",
            about = new[] { "class-ronin" },
            speakers = new[] { "class-ronin", "$hero" },
            payload = new
            {
                title = "A wounded ronin",
                start = "ask",
                nodes = new object[]
                {
                    new { id = "ask", speaker = "class-ronin", line, next = "reply" },
                    new
                    {
                        id = "reply", speaker = "$hero", line = "Stand with us.",
                        choices = new object[]
                        {
                            new { text = "We need you.", next, tags = new[] { "right-answer" } },
                            new { text = "You owe us.", next = "$outcome", tags = Array.Empty<string>() },
                        },
                    },
                    new { id = "join", speaker = "class-ronin", line = "My blade walks with yours." },
                },
            },
        });

    private static string Text(string id, string kind, string key, object payload, string[]? about = null, string[]? speakers = null) =>
        JsonSerializer.Serialize(new { id, kind, key, about = about ?? Array.Empty<string>(), speakers = speakers ?? Array.Empty<string>(), payload });

    [Test]
    public async Task Draft_stores_a_valid_conversation_as_version_one_draft()
    {
        var r = await svc.DraftAsync("gridgame", Conversation(), "agent");
        Assert.That(r.Success, Is.True, string.Join("; ", r.Errors));
        Assert.That(r.Created, Is.True);
        Assert.That(r.Version, Is.EqualTo(1));

        var doc = await svc.GetAsync("gridgame", "conversation-recruit-ronin");
        Assert.That(doc!.Status, Is.EqualTo("draft"));
        Assert.That(doc.Schema, Is.EqualTo("prose-text/1"));
        Assert.That(doc.Universe, Is.EqualTo("gridgame"));
        Assert.That(doc.Hash, Does.StartWith("sha256:"));
        Assert.That(doc.Payload.GetProperty("nodes").GetArrayLength(), Is.EqualTo(3));
    }

    [Test]
    public async Task Draft_of_identical_content_changes_nothing_and_new_content_adds_a_version()
    {
        await svc.DraftAsync("gridgame", Conversation());
        var same = await svc.DraftAsync("gridgame", Conversation());
        Assert.That(same.Unchanged, Is.True);
        Assert.That(same.Version, Is.EqualTo(1));

        var revised = await svc.DraftAsync("gridgame", Conversation(line: "Keep driving, I said."));
        Assert.That(revised.Version, Is.EqualTo(2));
        var record = await svc.GetRecordAsync("gridgame", "conversation-recruit-ronin");
        Assert.That(record!.Versions.Select(v => v.Status), Is.EqualTo(new[] { "superseded", "draft" }), "every version is kept");
    }

    [Test]
    public async Task Draft_refuses_unknown_entities_broken_graphs_and_bad_payloads()
    {
        var unknownAbout = await svc.DraftAsync("gridgame", Text("description-class-ghost", "description", "class:ghost",
            new { @short = "Boo." }, about: ["class-ghost"]));
        Assert.That(unknownAbout.Errors, Has.Some.Contains("'class-ghost' does not exist"));

        var badNext = await svc.DraftAsync("gridgame", Conversation(next: "nowhere"));
        Assert.That(badNext.Errors, Has.Some.Contains("'nowhere', which is not a node"));

        var entityRef = await svc.DraftAsync("gridgame", Text("crawl-prologue", "crawl", "crawl:prologue",
            new { title = "Tithe Day", paragraphs = new[] { "The {entity:class-wraith} came." } }));
        Assert.That(entityRef.Errors, Has.Some.Contains("'class-wraith' does not exist"));

        var emptyLines = await svc.DraftAsync("gridgame", Text("book-letter", "book", "book:letter", new { title = "A letter", pages = new[] { " " } }));
        Assert.That(emptyLines.Errors, Has.Some.Contains("pages[0]"));

        var badKind = await svc.DraftAsync("gridgame", Text("poem-x", "poem", "poem:x", new { }));
        Assert.That(badKind.Errors, Has.Some.Contains("kind 'poem'"));

        var badId = await svc.DraftAsync("gridgame", Text("Bad_Id", "glossary", "glossary:x", new { term = "x", definition = "y" }));
        Assert.That(badId.Errors, Has.Some.Contains("not a stable slug"));

        var speakerNotListed = await svc.DraftAsync("gridgame", Text("bark-lich", "bark", "bark:lich",
            new { speaker = "class-ronin", lines = new[] { "Hm." } }, speakers: ["class-lich"]));
        Assert.That(speakerNotListed.Errors, Has.Some.Contains("not one of the text's speakers"));
    }

    [Test]
    public async Task Every_kind_accepts_its_documented_payload()
    {
        var ok = new[]
        {
            Text("crawl-prologue", "crawl", "crawl:prologue", new { title = "Tithe Day", paragraphs = new[] { "Saltmoss under {entity:faction-ember} skies." } }),
            Text("description-class-lich", "description", "class:lich", new { @short = "A dead mage.", @long = "Longer." }, about: ["class-lich"]),
            Text("quest-wolves", "quest", "quest:q_wells_wolves", new { title = "Wolves at the Wells", giver = "Innkeeper", brief = "Thin the pack.", complete = "Drinks are on the house." }),
            Text("book-letter", "book", "book:letter", new { title = "A letter", author = "Marrit", pages = new[] { "Dear..." } }),
            Text("bark-lich", "bark", "bark:lich", new { speaker = "class-lich", context = "on cast", lines = new[] { "Kneel." } }, speakers: ["class-lich"]),
            Text("glossary-burning", "glossary", "glossary:burning", new { term = "Burning", aliases = new[] { "Burn" }, definition = "On fire." }),
        };
        foreach (var json in ok)
        {
            var r = await svc.DraftAsync("gridgame", json);
            Assert.That(r.Success, Is.True, r.Id + ": " + string.Join("; ", r.Errors));
        }
        Assert.That((await svc.ListAsync("gridgame", status: "draft")).Count, Is.EqualTo(ok.Length));
    }

    [Test]
    public async Task A_key_belongs_to_one_live_text()
    {
        await svc.DraftAsync("gridgame", Conversation());
        var clash = await svc.DraftAsync("gridgame", Conversation(id: "conversation-other"));
        Assert.That(clash.Errors, Has.Some.Contains("already used by text 'conversation-recruit-ronin'"));
    }

    [Test]
    public async Task Only_approved_versions_are_pullable_and_the_old_one_stays_live_until_the_new_is_approved()
    {
        await svc.DraftAsync("gridgame", Conversation());
        Assert.That(await svc.ListAsync("gridgame"), Is.Empty, "a draft never leaves Prose");

        var refused = await svc.ApproveAsync("gridgame", ["conversation-recruit-ronin"], "");
        Assert.That(refused.Success, Is.False, "approval names the approver");

        var approved = await svc.ApproveAsync("gridgame", ["conversation-recruit-ronin"], "ryan");
        Assert.That(approved.Success, Is.True, string.Join("; ", approved.Errors));
        var pulled = await svc.ListAsync("gridgame");
        Assert.That(pulled.Single().Version, Is.EqualTo(1));
        Assert.That(pulled.Single().Status, Is.EqualTo("approved"));
        Assert.That(pulled.Single().ApprovedBy, Is.EqualTo("ryan"));

        await svc.DraftAsync("gridgame", Conversation(line: "Keep driving, I said."));
        var stillLive = (await svc.ListAsync("gridgame")).Single();
        Assert.That(stillLive.Version, Is.EqualTo(1), "v1 stays live while v2 is a draft");
        Assert.That(stillLive.DraftVersion, Is.EqualTo(2));

        await svc.ApproveAsync("gridgame", ["conversation-recruit-ronin"], "ryan");
        Assert.That((await svc.ListAsync("gridgame")).Single().Version, Is.EqualTo(2));
        Assert.That((await svc.ListAsync("gridgame", since: "1")).Single().Version, Is.EqualTo(2));
        Assert.That(await svc.ListAsync("gridgame", since: "2"), Is.Empty);
        var v1 = await svc.GetAsync("gridgame", "conversation-recruit-ronin", version: 1);
        Assert.That(v1!.Status, Is.EqualTo("superseded"));
    }

    [Test]
    public async Task Publish_marks_a_batch_and_sends_one_outbox_notice()
    {
        await svc.DraftAsync("gridgame", Conversation());
        await svc.DraftAsync("gridgame", Text("glossary-burning", "glossary", "glossary:burning", new { term = "Burning", definition = "On fire." }));
        await svc.ApproveAsync("gridgame", ["conversation-recruit-ronin", "glossary-burning"], "ryan");

        var r = await svc.PublishAsync("gridgame", "ryan");
        Assert.That(r.Ids, Has.Count.EqualTo(2));
        Assert.That((await svc.ListAsync("gridgame", status: "published")).Count, Is.EqualTo(2));

        var events = await outbox.DrainAsync("gridgame");
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Kind, Is.EqualTo("texts.published"));
        Assert.That(events[0].DataJson, Does.Contain("glossary-burning"));

        var again = await svc.PublishAsync("gridgame", "ryan");
        Assert.That(again.Ids, Is.Empty);
        Assert.That(await outbox.DrainAsync("gridgame"), Is.Empty, "nothing new, no notice");
    }

    [Test]
    public async Task Retire_takes_a_text_out_of_the_pull_and_lists_it_as_retired()
    {
        await svc.DraftAsync("gridgame", Conversation());
        await svc.ApproveAsync("gridgame", ["conversation-recruit-ronin"], "ryan");
        await svc.RetireAsync("gridgame", ["conversation-recruit-ronin"], "ryan");

        Assert.That(await svc.ListAsync("gridgame"), Is.Empty);
        var retired = (await svc.ListAsync("gridgame", status: "retired")).Single();
        Assert.That(retired.Status, Is.EqualTo("retired"));
        Assert.That(retired.Key, Is.EqualTo("dialogue:recruit_ronin"));

        var reuse = await svc.DraftAsync("gridgame", Conversation(id: "conversation-other"));
        Assert.That(reuse.Success, Is.True, "a retired text's key is free again");
    }

    [Test]
    public async Task Texts_link_to_their_entities_and_stay_out_of_the_universe_export()
    {
        await svc.DraftAsync("gridgame", Conversation());

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var text = await db.Entities.IgnoreQueryFilters().SingleAsync(e => e.EntityType == ProseTextService.EntityType);
            var edges = await db.Edges.IgnoreQueryFilters().Where(e => e.SourceId == text.Id).Select(e => e.RelationType).ToListAsync();
            Assert.That(edges, Is.EquivalentTo(new[] { "about", "spoken-by" }));
        }

        var export = await interchange.ExportAsync("gridgame");
        Assert.That(export, Does.Not.Contain("conversation-recruit-ronin"));
        Assert.That(export, Does.Contain("class-ronin"));
    }

    [Test]
    public async Task An_interchange_file_may_not_use_the_reserved_text_type()
    {
        var r = await interchange.ImportAsync(JsonSerializer.Serialize(new
        {
            universe = new { id = "gridgame", name = "GridGame" },
            entities = new[] { new { id = "x", type = "prose-text", name = "X", summary = "" } },
        }));
        Assert.That(r.Success, Is.False);
        Assert.That(r.Errors.Single(), Does.Contain("reserved type"));
    }

    [Test]
    public void Hash_is_independent_of_key_order()
    {
        using var a = JsonDocument.Parse("""{"title":"T","paragraphs":["a"]}""");
        using var b = JsonDocument.Parse("""{ "paragraphs": ["a"], "title": "T" }""");
        Assert.That(ProseTextService.Hash(a.RootElement), Is.EqualTo(ProseTextService.Hash(b.RootElement)));
    }

    [Test]
    public async Task Texts_are_segregated_by_universe()
    {
        await svc.DraftAsync("gridgame", Conversation());
        await interchange.ImportAsync(JsonSerializer.Serialize(new
        {
            universe = new { id = "spacegod", name = "SpaceGod" },
            entities = new[] { new { id = "pilot-tomas", type = "character", name = "Tomas", summary = "A pilot." } },
        }));
        Assert.That(await svc.ListAsync("spacegod", status: "all"), Is.Empty);
        var crossRef = await svc.DraftAsync("spacegod", Text("description-class-ronin", "description", "class:ronin",
            new { @short = "x" }, about: ["class-ronin"]));
        Assert.That(crossRef.Errors, Has.Some.Contains("does not exist in universe 'spacegod'"));
    }
}
