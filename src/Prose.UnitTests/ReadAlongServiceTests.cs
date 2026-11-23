using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.ReadAlong;

namespace Prose.UnitTests;

/// <summary>RFC 0019 end to end on SQLite: a book is read in order, drafts are kept or tossed,
/// a disagreeing later line becomes a Continuity Inbox card, and the atlas and ask-your-book read
/// what was kept.</summary>
[TestFixture]
public class ReadAlongServiceTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ReadAlongService readAlong = null!;
    private StoryBibleService bible = null!;
    private ContinuityInboxService inbox = null!;
    private StoryAtlasService atlas = null!;
    private BookChatService chat = null!;
    private BrainstormService brainstorm = null!;
    private ReadAlongAllowance allowance = null!;
    private int number = 1;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using (var ctx = dbFactory.CreateDbContext()) ctx.Database.EnsureCreated();
        allowance = new ReadAlongAllowance(dbFactory, new SettingsKvStore(dbFactory));
        readAlong = new ReadAlongService(dbFactory, allowance, NullLogger<ReadAlongService>.Instance);
        bible = new StoryBibleService(dbFactory, NullLogger<StoryBibleService>.Instance);
        inbox = new ContinuityInboxService(dbFactory);
        atlas = new StoryAtlasService(dbFactory);
        brainstorm = new BrainstormService(dbFactory);
        chat = new BookChatService(dbFactory, readAlong, allowance, new ServiceCollection().BuildServiceProvider(), NullLogger<BookChatService>.Instance);
    }

    [TearDown]
    public void TearDown() { conn.Close(); conn.Dispose(); }

    private async Task<Guid> BookAsync(params (string Title, string[] Paragraphs)[] chapters)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var book = new BookNode { Id = Guid.NewGuid(), Slug = "b-" + Guid.NewGuid().ToString("N")[..8], Title = "The Ashen Test", UniverseId = Universe.GlmzId };
        db.Nodes.Add(book);
        var sort = 0;
        foreach (var (title, paragraphs) in chapters)
        {
            var ch = new ChapterNode { Id = Guid.NewGuid(), ParentNodeId = book.Id, Slug = "c-" + Guid.NewGuid().ToString("N")[..8], Title = title, SortKey = sort += 100, UniverseId = Universe.GlmzId };
            db.Nodes.Add(ch);
            var i = 0;
            foreach (var p in paragraphs)
            {
                var beat = new Beat { Id = Guid.NewGuid(), Number = number++, Text = p, TextHash = Beat.ComputeHash(p), UpdatedAt = DateTime.UtcNow.AddHours(-1) };
                db.Beats.Add(beat);
                db.BeatNodes.Add(new BeatNode { NodeId = ch.Id, BeatId = beat.Id, SortKey = ++i * 100 });
            }
        }
        await db.SaveChangesAsync();
        return book.Id;
    }

    private async Task AddChapterAsync(Guid bookId, string title, params string[] paragraphs)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ch = new ChapterNode { Id = Guid.NewGuid(), ParentNodeId = bookId, Slug = "c-" + Guid.NewGuid().ToString("N")[..8], Title = title, SortKey = 10_000, UniverseId = Universe.GlmzId };
        db.Nodes.Add(ch);
        var i = 0;
        foreach (var p in paragraphs)
        {
            var beat = new Beat { Id = Guid.NewGuid(), Number = number++, Text = p, TextHash = Beat.ComputeHash(p) };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode { NodeId = ch.Id, BeatId = beat.Id, SortKey = ++i * 100 });
        }
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task Reads_in_order_drafts_wait_for_the_author_and_a_kept_fact_raises_a_card()
    {
        var book = await BookAsync(
            ("Chapter 1: The Broken Gate", ["Elena crossed the last stretch of open ground. She was twenty-eight.", "Kael said nothing. Kael's eyes were grey."]),
            ("Chapter 2: Stored Light", ["Elena found Kael at the gate. Kael watched the road."]));

        var results = await readAlong.ReadBookAsync(book);
        Assert.That(results.Select(r => r.Index), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(results[0].Met, Does.Contain("Elena").And.Contain("Kael"));

        // Nothing final until the author says so: drafts, no entities, empty codex entries.
        var drafts = await bible.GetDraftsAsync(book, "entry");
        var elena = drafts.Single(d => d.Name == "Elena");
        Assert.That(elena.Attributes.Any(a => a.Key == "age" && a.Value == "28" && a.Quote == "She was twenty-eight."));

        var kept = await bible.KeepAsync(elena.Id);
        Assert.That(kept.Ok, Is.True);
        var kael = drafts.Single(d => d.Name == "Kael");
        Assert.That((await bible.KeepAsync(kael.Id)).Ok, Is.True);

        var entry = await bible.GetEntryAsync(book, kept.EntityId!.Value);
        Assert.That(entry!.Details.Single(d => d.Attribute == "age").ChapterTitle, Is.EqualTo("Chapter 1: The Broken Gate"));

        // Re-reading an unchanged book reads nothing.
        Assert.That((await readAlong.ReadBookAsync(book)).All(r => r.Skipped));

        // A later chapter disagrees with a confirmed detail → a card, not an overwrite.
        await AddChapterAsync(book, "Chapter 3: Three Days South", "Kael turned. Kael's eyes were green in the lamplight.");
        var third = await readAlong.ReadBookAsync(book);
        Assert.That(third.Single(r => !r.Skipped).Contradictions, Is.EqualTo(1));

        var card = (await inbox.ListAsync(book)).Single();
        Assert.That(card.ExistingValue, Is.EqualTo("grey"));
        Assert.That(card.ProposedValue, Is.EqualTo("green"));
        Assert.That(card.Existing.ChapterTitle, Is.EqualTo("Chapter 1: The Broken Gate"));
        Assert.That(card.Confidence, Is.GreaterThan(90));
        Assert.That(await inbox.ExplainAsync(card.Id), Does.Contain("grey").And.Contain("green"));

        Assert.That((await inbox.ResolveAsync(card.Id, "accept-new")).Ok, Is.True);
        var kaelEntry = await bible.GetEntryAsync(book, (await bible.GetCodexAsync(book)).SelectMany(c => c.Items).Single(i => i.Name == "Kael").Id);
        Assert.That(kaelEntry!.Details.Single(d => d.Attribute == "eyes").Value, Is.EqualTo("green"));
        Assert.That(kaelEntry.Timeline.Any(t => t.Value == "grey" && t.SupersededAt != null), "the old value stays on the timeline");
        Assert.That(await inbox.CountOpenAsync(book), Is.EqualTo(0));
    }

    [Test]
    public async Task A_world_rule_and_an_item_look_that_change_raise_cards()
    {
        var book = await BookAsync(
            ("One", ["The lantern needs three nights to recharge.", "Wren crossed the flats. Wren pulled her grey cloak tight."]),
            ("Two", ["The lantern needs six nights to recharge.", "Wren crossed the yard. Wren pulled her black cloak tight."]));
        await readAlong.ReadBookAsync(book);
        var cards = await inbox.ListAsync(book);
        Assert.That(cards.Any(c => c.Attribute == "time to recharge" && c.ExistingValue == "3 nights" && c.ProposedValue == "6 nights" && c.Category == "world-rule"));
        Assert.That(cards.Any(c => c.Attribute == "cloak" && c.ExistingValue == "grey" && c.ProposedValue == "black" && c.Category == "description"));
    }

    [Test]
    public async Task Rewriting_a_quoted_line_closes_its_card_as_fixed()
    {
        var book = await BookAsync(
            ("One", ["Kael said nothing. Kael's eyes were grey."]),
            ("Two", ["Kael turned. Kael's eyes were green in the light."]));
        await readAlong.ReadBookAsync(book);
        var card = (await inbox.ListAsync(book)).Single();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.SingleAsync(b => b.Text.Contains("green"));
            beat.Text = "Kael turned. Kael's eyes were grey in the light.";
            await db.SaveChangesAsync();
        }
        await readAlong.ReadBookAsync(book);
        Assert.That((await inbox.GetAsync(card.Id))!.Status, Is.EqualTo("fixed"));
        Assert.That(await inbox.CountOpenAsync(book), Is.EqualTo(0));
    }

    [Test]
    public async Task Highlight_names_include_aliases_and_can_exclude_drafts()
    {
        var book = await BookAsync(("One", ["She told no one but Hollis Marsh. Hollis said nothing. Brannoc watched the moor, and Brannoc waited."]));
        await readAlong.ReadBookAsync(book);
        var hollis = (await bible.GetDraftsAsync(book, "entry")).Single(d => d.Name == "Hollis Marsh");
        await bible.KeepAsync(hollis.Id);

        var all = await bible.GetHighlightNamesAsync(book);
        var kept = await bible.GetHighlightNamesAsync(book, includeDrafts: false);
        Assert.That(all.Any(n => n.Name == "Brannoc" && n.Draft));
        Assert.That(kept.Select(n => n.Name), Is.EquivalentTo(new[] { "Hollis Marsh", "Hollis" }));
        Assert.That(kept.All(n => n.EntityId is not null));
    }

    [Test]
    public async Task A_scene_moved_to_another_chapter_is_read_with_it_and_both_chapters_are_due()
    {
        var book = await BookAsync(("One", ["Wren crossed the flats."]), ("Two", ["Kael waited at the gate."]));
        Guid sceneId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var one = await db.Nodes.SingleAsync(n => n.Title == "One");
            var scene = new SceneNode { Id = Guid.NewGuid(), ParentNodeId = one.Id, Title = "Night", Slug = "night-" + Guid.NewGuid().ToString("N")[..6], SortKey = 500, UniverseId = Universe.GlmzId };
            db.Nodes.Add(scene);
            var beat = new Beat { Id = Guid.NewGuid(), Number = number++, Text = "Maren watched the tide turn.", TextHash = Beat.ComputeHash("Maren watched the tide turn.") };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode { NodeId = scene.Id, BeatId = beat.Id, SortKey = 100 });
            await db.SaveChangesAsync();
            sceneId = scene.Id;
        }
        var before = await readAlong.GetChaptersAsync(book);
        Assert.That(before.Single(c => c.Title == "One").Paragraphs.Count, Is.EqualTo(2), "a scene folds into its chapter");
        await readAlong.ReadBookAsync(book);
        Assert.That(await readAlong.UnreadChaptersAsync(book), Is.Empty);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var two = await db.Nodes.SingleAsync(n => n.Title == "Two");
            (await db.Nodes.SingleAsync(n => n.Id == sceneId)).ParentNodeId = two.Id;
            await db.SaveChangesAsync();
        }
        var after = await readAlong.GetChaptersAsync(book);
        Assert.That(after.Single(c => c.Title == "Two").Paragraphs.Count, Is.EqualTo(2));
        Assert.That((await readAlong.UnreadChaptersAsync(book)).Select(c => c.Title), Is.EquivalentTo(new[] { "One", "Two" }));
    }

    [Test]
    public async Task Follow_switch_controls_the_sweep()
    {
        var follow = new ReadAlongFollow(new SettingsKvStore(dbFactory));
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var job = new ReadAlongBackgroundService(dbFactory, readAlong, follow, config, NullLogger<ReadAlongBackgroundService>.Instance);

        var readBefore = await BookAsync(("One", ["Wren walked the flats. Wren said nothing."]));
        var neverRead = await BookAsync(("One", ["Kael waited at the gate. Kael said nothing."]));
        await readAlong.ReadBookAsync(readBefore);
        await AddChapterAsync(readBefore, "Two", "Wren came back at dusk.");

        Assert.That(follow.IsFollowed(readBefore, readOnce: true), Is.True);
        follow.Set(readBefore, false);
        follow.Set(neverRead, true);
        Assert.That(follow.Get(readBefore), Is.False);

        await job.SweepAsync(TimeSpan.Zero, CancellationToken.None);
        Assert.That((await readAlong.GetStatusAsync(readBefore)).ChaptersRead, Is.EqualTo(1), "switched off: chapter two is not read");
        Assert.That((await readAlong.GetStatusAsync(neverRead)).ChaptersRead, Is.EqualTo(1), "switched on: read without a first manual read");
    }

    [Test]
    public async Task Scene_break_beats_are_not_read_as_prose()
    {
        var book = await BookAsync(("One", ["Wren walked.", ManuscriptParser.SceneBreakText, "Kael ran."]));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            (await db.Beats.SingleAsync(b => b.Text == ManuscriptParser.SceneBreakText)).Kind = ManuscriptParser.SceneBreakKind;
            await db.SaveChangesAsync();
        }
        var ch = (await readAlong.GetChaptersAsync(book)).Single();
        Assert.That(ch.Paragraphs.Select(p => p.Text), Is.EqualTo(new[] { "Wren walked.", "Kael ran." }));
    }

    [Test]
    public async Task Rename_keeps_receipts_and_makes_the_old_name_an_alias()
    {
        var book = await BookAsync(("One", ["Wren crossed the flats. She was nineteen."]));
        await readAlong.ReadBookAsync(book);
        var id = (await bible.KeepAsync((await bible.GetDraftsAsync(book, "entry")).Single(d => d.Name == "Wren").Id)).EntityId!.Value;
        Assert.That((await bible.RenameAsync(book, id, "Wren Ashby")).Ok);
        var e = await bible.GetEntryAsync(book, id);
        Assert.That(e!.Name, Is.EqualTo("Wren Ashby"));
        Assert.That(e.Aliases, Does.Contain("Wren"));
        Assert.That(e.Details.Single(d => d.Attribute == "age").Quote, Is.EqualTo("She was nineteen."));
    }

    [Test]
    public async Task Recategorize_an_untagged_entry_made_from_this_book()
    {
        var book = await BookAsync(("One", ["Wren crossed the Undertow. Wren crossed the Undertow again."]));
        await readAlong.ReadBookAsync(book);
        var d = (await bible.GetDraftsAsync(book, "entry")).Single(x => x.Name == "the Undertow");
        var id = (await bible.KeepAsync(d.Id)).EntityId!.Value;
        Assert.That((await bible.RecategorizeAsync(book, id, "concept")).Ok);
        Assert.That((await bible.GetEntryAsync(book, id))!.Category, Is.EqualTo("concept"));
        Assert.That((await bible.RecategorizeAsync(book, id, "nonsense")).Ok, Is.False);
    }

    [Test]
    public async Task Merge_moves_receipts_aliases_and_cards_to_the_keeper()
    {
        var book = await BookAsync(("One", ["Hollis watched the tide. Hollis's eyes were grey. Hollis said nothing."]),
                                   ("Two", ["Tobin Marsh came home. Tobin Marsh was thirty. Tobin Marsh's eyes were green."]));
        await readAlong.ReadBookAsync(book);
        await bible.KeepAllAsync(book);
        var codex = (await bible.GetCodexAsync(book)).SelectMany(c => c.Items).ToList();
        var keep = codex.Single(i => i.Name == "Tobin Marsh").Id;
        var drop = codex.Single(i => i.Name == "Hollis").Id;

        Assert.That((await bible.MergeAsync(book, keep, drop)).Ok);
        var e = await bible.GetEntryAsync(book, keep);
        Assert.That(e!.Aliases, Does.Contain("Hollis"));
        Assert.That(e.Details.Single(d => d.Attribute == "eyes").Value, Is.EqualTo("green"), "the later chapter's value stands");
        Assert.That(e.Timeline.Any(t => t.Attribute == "eyes" && t.Value == "grey" && t.SupersededAt != null), "the other is history");
        Assert.That(e.Details.Any(d => d.Attribute == "age"));
        Assert.That((await bible.GetCodexAsync(book)).SelectMany(c => c.Items).Any(i => i.Id == drop), Is.False);
    }

    [Test]
    public async Task An_entry_as_of_an_earlier_chapter_shows_what_the_reader_knew_then()
    {
        var book = await BookAsync(
            ("One", ["Kael said nothing. Kael's eyes were grey."]),
            ("Two", ["Kael waited. Kael was thirty."]),
            ("Three", ["Kael turned. Kael's eyes were green."]));
        await readAlong.ReadBookAsync(book);
        await bible.KeepAllAsync(book);
        var card = (await inbox.ListAsync(book)).Single();
        await inbox.ResolveAsync(card.Id, "accept-new");
        var id = (await bible.GetCodexAsync(book)).SelectMany(c => c.Items).Single(i => i.Name == "Kael").Id;

        var now = await bible.GetEntryAsync(book, id);
        var then = await bible.GetEntryAsync(book, id, asOfChapter: 1);
        Assert.That(now!.Details.Single(d => d.Attribute == "eyes").Value, Is.EqualTo("green"));
        Assert.That(then!.Details.Single(d => d.Attribute == "eyes").Value, Is.EqualTo("grey"));
        Assert.That(then.Details.Any(d => d.Attribute == "age"), Is.False, "chapter two had not happened yet");
    }

    private sealed class ChunkingLlm : Prose.Core.Interfaces.ILlmService
    {
        public string? LastPrompt;
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
            => throw new InvalidOperationException("streaming path expected");
        public async IAsyncEnumerable<string> StreamAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            LastPrompt = user;
            foreach (var c in new[] { "Sera knows ", "about the Sight ", "[P1]." }) { await Task.Yield(); yield return c; }
        }
    }

    [Test]
    public async Task Ask_streams_chunks_and_stores_their_concatenation_and_honours_the_history_setting()
    {
        var book = await BookAsync(("One", ["Sera Vane lowered her voice. Sera Vane had the Sight."]));
        await readAlong.ReadBookAsync(book);
        var llm = new ChunkingLlm();
        var sp = new ServiceCollection().AddSingleton<Prose.Core.Interfaces.ILlmService>(llm).BuildServiceProvider();
        var kv = new SettingsKvStore(dbFactory);
        var streaming = new BookChatService(dbFactory, readAlong, allowance, sp, NullLogger<BookChatService>.Instance, kv) { HistoryTurns = 1 };

        for (var i = 0; i < 3; i++) await streaming.AskAsync(book, $"What does Sera Vane know? ({i})");
        var chunks = new List<string>();
        var a = await streaming.AskAsync(book, "And Sera Vane now?", onChunk: chunks.Add);

        Assert.That(chunks, Has.Count.EqualTo(3));
        Assert.That(a.UsedLlm, Is.True);
        Assert.That(a.Reply.Text, Is.EqualTo("Sera knows about the Sight."), "citations stripped, chunks joined");
        Assert.That(a.Reply.Doors, Is.Not.Empty);
        Assert.That(llm.LastPrompt!.Split('\n').Count(l => l.StartsWith("USER:")), Is.EqualTo(1), "one turn of history = one user line");
        Assert.That(streaming.HistoryTurns, Is.EqualTo(1));
    }

    [Test]
    public async Task Tossed_drafts_stay_tossed()
    {
        var book = await BookAsync(("One", ["Brannoc watched the moor. Brannoc said nothing."]));
        await readAlong.ReadBookAsync(book);
        var d = (await bible.GetDraftsAsync(book, "entry")).Single(x => x.Name == "Brannoc");
        Assert.That(await bible.TossAsync(d.Id), Is.True);

        await AddChapterAsync(book, "Two", "Brannoc came back at dusk, and Brannoc was tired.");
        await readAlong.ReadBookAsync(book);
        Assert.That((await bible.GetDraftsAsync(book, "entry")).Any(x => x.Name == "Brannoc"), Is.False);
    }

    [Test]
    public async Task Atlas_scrubs_back_to_what_the_reader_knew()
    {
        var book = await BookAsync(
            ("One", ["Elena crossed the gate. Elena met Kael there, and Kael smiled.", "Elena and Kael walked on."]),
            ("Two", ["Maren arrived. Maren and Elena talked through the night."]));
        await readAlong.ReadBookAsync(book);

        var whole = await atlas.GetAsync(book);
        var early = await atlas.GetAsync(book, asOfChapter: 1);
        Assert.That(whole.Stars.Select(s => s.Name), Does.Contain("Maren"));
        Assert.That(early.Stars.Select(s => s.Name), Does.Not.Contain("Maren"));
        Assert.That(whole.Threads.Any(t => t.Type == "appears with"));
        Assert.That(whole.Chapters.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Ask_answers_from_the_book_with_doors_and_honours_the_question_limit()
    {
        var book = await BookAsync(("One", ["Sera Vane lowered her voice. Sera Vane had the Sight."]), ("Two", ["Sera Vane looked back at the tower."]));
        await readAlong.ReadBookAsync(book);
        await bible.KeepAllAsync(book);

        var a = await chat.AskAsync(book, "What does Sera Vane know?");
        Assert.That(a.Reply.Doors, Is.Not.Empty);
        Assert.That(a.Reply.Doors.All(d => d.ChapterTitle is "One" or "Two"));
        Assert.That(a.UsedLlm, Is.False);

        allowance.Set(new ReadAlongAllowance.Limits(QuestionLimit: 1));
        var refused = await chat.AskAsync(book, "And Kael?");
        Assert.That(refused.Refused, Is.EqualTo("allowance"));
        Assert.That((await chat.HistoryAsync(book)).Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Reading_stops_at_the_word_allowance_and_keeps_what_it_read()
    {
        var book = await BookAsync(("One", ["Elena walked. Elena said hello to everyone there."]), ("Two", ["Kael ran. Kael said goodbye."]));
        allowance.Set(new ReadAlongAllowance.Limits(WordLimit: 3));
        var r = await readAlong.ReadBookAsync(book);
        Assert.That(r[0].Skipped, Is.False);
        Assert.That(r[1].Reason, Is.EqualTo("allowance"));
        var status = await readAlong.GetStatusAsync(book);
        Assert.That(status.ChaptersRead, Is.EqualTo(1));
    }

    [Test]
    public async Task Brainstorm_cards_round_trip()
    {
        var book = await BookAsync(("One", ["Elena walked."]));
        var c = await brainstorm.AddAsync(book, "What if the staff is alive?", "", 10, 20, "question");
        Assert.That(await brainstorm.UpdateAsync(c.Id, x: 300, y: 120), Is.True);
        var list = await brainstorm.ListAsync(book);
        Assert.That(list.Single().X, Is.EqualTo(300));
        Assert.That(await brainstorm.DeleteAsync(c.Id), Is.True);
    }

    [Test]
    public void Manuscript_parser_finds_volumes_chapters_and_scene_breaks()
    {
        var text = """
            PART ONE

            CHAPTER ONE: THE BROKEN GATE

            The fog came off the moor.

            * * *

            She went up the wide street.

            Chapter 2
            Kael was three days south.

            Part Two

            Chapter 3: The Crown

            She was twenty-eight.
            """;
        var m = ManuscriptParser.Parse([new ManuscriptParser.InputFile("saga.txt", Encoding.UTF8.GetBytes(text))]);
        Assert.That(m.Title, Is.EqualTo("saga"));
        Assert.That(m.Volumes.Select(v => v.Title), Is.EqualTo(new[] { "Part One", "Part Two" }));
        var chapters = m.Chapters.ToList();
        Assert.That(chapters.Select(c => c.Title), Is.EqualTo(new[] { "Chapter One: The Broken Gate", "Chapter 2", "Chapter 3: The Crown" }));
        Assert.That(chapters[0].Paragraphs, Is.EqualTo(new[] { "The fog came off the moor.", ManuscriptParser.SceneBreakText, "She went up the wide street." }));
        Assert.That(chapters[1].Paragraphs.Single(), Is.EqualTo("Kael was three days south."));
    }

    [Test]
    public void Manuscript_parser_orders_a_folder_naturally()
    {
        var files = new[]
        {
            new ManuscriptParser.InputFile("ch10.txt", Encoding.UTF8.GetBytes("Ten.")),
            new ManuscriptParser.InputFile("ch2.txt", Encoding.UTF8.GetBytes("Two.")),
            new ManuscriptParser.InputFile("ch1.txt", Encoding.UTF8.GetBytes("One.")),
        };
        var m = ManuscriptParser.Parse(files, "Serial");
        Assert.That(m.Chapters.Select(c => c.Title), Is.EqualTo(new[] { "ch1", "ch2", "ch10" }));
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
