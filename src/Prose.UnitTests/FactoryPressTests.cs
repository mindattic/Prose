using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>
/// The Novel Factory, I6 (RFC 0015 §3.9–3.13): a press is recorded with the book's fingerprint and
/// goes stale the moment the prose changes; the context bundle carries the prose before a unit
/// across chapter boundaries and drops the oldest first; the journal reconstructs a change from the
/// records alone; a factory tool no one calls gets one use-or-delete order.
/// </summary>
public abstract class PressFixture : WorldFixture
{
    protected ExportRecorder recorder = null!;
    protected ContextBundleService bundles = null!;
    protected FactoryJournal journal = null!;

    [SetUp]
    public void SetUpPress()
    {
        var spine = new BookSpineService(dbFactory);
        recorder = new ExportRecorder(dbFactory);
        bundles = new ContextBundleService(dbFactory, spine, rulings);
        journal = new FactoryJournal(dbFactory, spine);
    }

    protected async Task<Guid> ChapterAsync(Guid book, string title, int sort, params string[] beats)
    {
        Guid chId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var universe = await db.Nodes.Where(n => n.Id == book).Select(n => n.UniverseId).SingleAsync();
            var ch = new ChapterNode { Id = Guid.CreateVersion7(), UniverseId = universe, Slug = "pc-" + Guid.NewGuid().ToString("N")[..8], Title = title, Kind = "chapter", Status = "draft", SortKey = sort, ParentNodeId = book };
            db.Nodes.Add(ch);
            await db.SaveChangesAsync();
            chId = ch.Id;
        }
        Guid? after = null;
        foreach (var text in beats) after = (await workbench.InsertBeatAsync(chId, after, text)).Id;
        return chId;
    }

    protected async Task PressAsync(Guid book, params string[] formats)
    {
        foreach (var f in formats) await recorder.RecordAsync(book, f, Path.Combine(tempRoot, "book." + f));
    }
}

[TestFixture]
public class ExportRecorderTests : PressFixture
{
    [Test]
    public async Task A_press_is_stamped_with_the_fingerprint_and_goes_stale_when_the_prose_changes()
    {
        var (book, _) = await BookAsync("The rain fell on the market.", "He paid for the noodles.");
        await ReadBookAsync(book);

        var s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["F7"].State, Is.EqualTo("fail"), "every unit is through the line, but nothing is pressed");
        Assert.That(s.BookStations["A"].State, Is.EqualTo("waiting"));

        await PressAsync(book, "docx", "epub");
        s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["F7"].State, Is.EqualTo("fail"));
        Assert.That(s.BookStations["F7"].Detail, Does.Contain("pdf"), "a missing format is named");

        await PressAsync(book, "pdf");
        s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["F7"].State, Is.EqualTo("pass"), s.BookStations["F7"].Detail);
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var rows = await db.Exports.Where(x => x.BookId == book).ToListAsync();
            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows.Select(r => r.BookFingerprint).Distinct().Single(), Is.EqualTo(await BookFingerprint.ComputeAsync(db, book)));
        }

        await PressAsync(book, "mp3");
        s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["A"].State, Is.EqualTo("pass"));

        // One word changes: the unit is unread, so the press waits; once it is re-read, the files on
        // disk are older than the book, and both presses say so.
        var beat = (await BeatIdsAsync(book))[1];
        await workbench.UpdateBeatTextAsync(beat, "He paid for the dumplings.", BeatWriteReason.AuthorEdit);
        s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["F7"].State, Is.EqualTo("waiting"));
        await ReadBookAsync(book);
        s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["F7"].State, Is.EqualTo("fail"));
        Assert.That(s.BookStations["F7"].Detail, Does.Contain("docx").And.Contain("epub").And.Contain("pdf"));
        Assert.That(s.BookStations["A"].State, Is.EqualTo("waiting"), "audio is pressed from a pressed book");

        await PressAsync(book, "docx", "epub", "pdf");
        s = await factory.StatusAsync(book);
        Assert.That(s.BookStations["F7"].State, Is.EqualTo("pass"));
        Assert.That(s.BookStations["A"].State, Is.EqualTo("fail"), "the audio is of the old prose");
    }

    [Test]
    public async Task A_chapter_export_is_recorded_against_its_book()
    {
        var (book, chapter) = await BookAsync("Words.");
        var row = await recorder.RecordAsync(chapter, "txt", "x.txt");
        Assert.That(row.BookId, Is.EqualTo(book));
    }
}

[TestFixture]
public class ContextBundleTests : PressFixture
{
    [Test]
    public async Task The_bundle_carries_the_previous_unit_across_a_chapter_boundary()
    {
        var (book, _) = await BookAsync("Alpha rain on the glass.", "Alpha ends at the door.");
        await ChapterAsync(book, "Chapter 2 — Two", 2, "Bravo opens in the stairwell.");
        await ChapterAsync(book, "Chapter 3 — Three", 3, "Charlie on the roof.");

        var m = await bundles.BuildAsync(book, 2, outDir: tempRoot);
        var text = await File.ReadAllTextAsync(m.Path);
        Assert.That(m.PriorFromUnit, Is.EqualTo(1));
        Assert.That(m.PriorToUnit, Is.EqualTo(1));
        Assert.That(m.PriorTruncated, Is.False);
        Assert.That(text, Does.Contain("Alpha ends at the door."), "the chapter before is in working memory");
        Assert.That(text, Does.Contain("Bravo opens in the stairwell."));
        Assert.That(text, Does.Not.Contain("Charlie"), "nothing after the unit");
        Assert.That(text.IndexOf("Alpha ends", StringComparison.Ordinal), Is.LessThan(text.IndexOf("Bravo opens", StringComparison.Ordinal)));

        var all = await bundles.BuildAsync(book, 3, allPrior: true, outDir: tempRoot);
        var allText = await File.ReadAllTextAsync(all.Path);
        Assert.That((all.PriorFromUnit, all.PriorToUnit), Is.EqualTo((1, 2)));
        Assert.That(allText, Does.Contain("Alpha rain").And.Contain("Bravo opens").And.Contain("Charlie on the roof."));
        Assert.That(all.Hash, Has.Length.EqualTo(64));
    }

    [Test]
    public async Task A_large_canon_never_crowds_the_previous_unit_out_of_the_budget()
    {
        // BCODA's first bundle for unit 2 held 190K of canon and none of unit 1: the old blindness again.
        var (book, _) = await BookAsync("Unit one ends with the door shut.");
        await ChapterAsync(book, "Chapter 2 — Two", 2, "Unit two opens.");
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var universe = await db.Nodes.Where(n => n.Id == book).Select(n => n.UniverseId).SingleAsync();
            if (!await db.CanonDocumentTypes.AnyAsync(t => t.DocumentType == "WorldMaster"))
                db.CanonDocumentTypes.Add(new CanonDocumentType { DocumentType = "WorldMaster", PathTemplate = "docs/WORLD.md", TitleTemplate = "World", Scope = "universe" });
            var doc = new CanonDocument { UniverseId = universe, DocumentType = "WorldMaster", Title = "The World" };
            doc.Sections.Add(new CanonDocumentSection { SectionKey = "s1", SectionTitle = "Everything", Content = "CANON " + new string('w', 500_000), SortKey = 1 });
            db.CanonDocuments.Add(doc);
            await db.SaveChangesAsync();
        }

        var m = await bundles.BuildAsync(book, 2, outDir: tempRoot);
        Assert.That(m.PriorFromUnit, Is.EqualTo(1), "the chapter before is what the budget is for");
        var text = await File.ReadAllTextAsync(m.Path);
        Assert.That(text, Does.Contain("Unit one ends with the door shut."));
        Assert.That(text, Does.Not.Contain("CANON "), "the canon lives in the world file");
        Assert.That(m.TotalChars, Is.LessThanOrEqualTo(ContextBundleService.DefaultBudget));
        Assert.That(await File.ReadAllTextAsync(m.WorldPath), Does.Contain("#### Everything").And.Contain("CANON "));
        Assert.That(m.CanonDocuments, Is.EqualTo(new[] { "The World" }));
        Assert.That(m.WorldChars, Is.GreaterThan(500_000));
    }

    [Test]
    public async Task The_budget_drops_the_oldest_prose_first_and_says_so()
    {
        var (book, _) = await BookAsync("OLDEST " + new string('a', 3000));
        await ChapterAsync(book, "Chapter 2 — Two", 2, "MIDDLE " + new string('b', 3000) + " END-OF-TWO");
        await ChapterAsync(book, "Chapter 3 — Three", 3, "The unit itself.");

        var probe = await bundles.BuildAsync(book, 3, allPrior: true, outDir: tempRoot);
        var overhead = probe.TotalChars - probe.PriorChars;
        var m = await bundles.BuildAsync(book, 3, allPrior: true, budgetChars: overhead + 4500, outDir: tempRoot);
        var text = await File.ReadAllTextAsync(m.Path);
        Assert.That(m.TotalChars, Is.LessThanOrEqualTo(overhead + 4500), "headings are paid for out of the budget too");
        Assert.That(m.PriorTruncated, Is.True);
        Assert.That(m.PriorCharsAvailable, Is.GreaterThan(m.PriorChars));
        Assert.That(text, Does.Contain("END-OF-TWO"), "the newest prior prose survives");
        Assert.That(text, Does.Not.Contain("OLDEST"), "the oldest is what falls off");
        Assert.That(text, Does.Contain("The unit itself."));
    }

    [Test]
    public async Task The_manifest_lists_the_records_and_law_the_unit_answers_to()
    {
        var (book, _) = await BookAsync("Opening.");
        var ch2 = await ChapterAsync(book, "Chapter 2 — Two", 2, "drove north.");
        var renko = NewCharacter("Renko Moss", "A courier with a bad knee.");
        var beat = (await BeatIdsAsync(book))[1];
        await TagAsync(beat, renko);
        await rulings.RecordAsync(new RulingDraft("law", "Renko never owns a car.", book, Pattern: @"\bRenko'?s car\b"));

        var m = await bundles.BuildAsync(book, 2, outDir: tempRoot);
        var text = await File.ReadAllTextAsync(m.Path);
        Assert.That(m.Entities.Select(e => e.Id), Is.EqualTo(new[] { Guid.Parse(renko.Id) }));
        Assert.That(m.Laws, Is.EqualTo(1));
        Assert.That(text, Does.Contain("Renko never owns a car."));
        Assert.That(await File.ReadAllTextAsync(m.WorldPath), Does.Contain("Renko never owns a car."), "the law heads both files");
        Assert.That(text, Does.Contain("A courier with a bad knee."), "the record as it stands");
        Assert.That(text, Does.Contain("Renko Moss drove north."), "the prose, tags stripped");
        Assert.That(text, Does.Not.Contain("<entity"));
        _ = ch2;
    }

    [Test]
    public async Task A_planned_unit_shows_its_beats_titles_and_descriptions()
    {
        var (book, _) = await BookAsync("Opening.");
        var ch2 = await ChapterAsync(book, "Chapter 2 — Two", 2, "");
        var beat = (await BeatIdsAsync(book))[1];
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var b = await db.Beats.SingleAsync(x => x.Id == beat);
            b.Title = "The handoff";
            b.Description = "Kyle gives the drive to Sift under the overpass.";
            await db.SaveChangesAsync();
        }
        var m = await bundles.BuildAsync(book, 2, outDir: tempRoot);
        var text = await File.ReadAllTextAsync(m.Path);
        Assert.That(text, Does.Contain("[planned] The handoff — Kyle gives the drive to Sift under the overpass."));
        _ = ch2;
    }
}

[TestFixture]
public class FactoryJournalTests : PressFixture
{
    [Test]
    public async Task The_journal_reconstructs_what_happened_from_the_records_alone()
    {
        var since = DateTime.UtcNow.AddSeconds(-1);
        var (book, _) = await BookAsync("The rain fell.", "He paid.");
        var (other, _) = await BookAsync("Another book.");
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var slug = await db.Nodes.Where(n => n.Id == book).Select(n => n.Slug).SingleAsync();
            var otherSlug = await db.Nodes.Where(n => n.Id == other).Select(n => n.Slug).SingleAsync();
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "cli", HandlerClass = "SpliceBeatsCli", ArgsJson = $"[\"--splice-beats\",\"--node\",\"{slug}\"]", Success = true, Actor = "cli:session:s1" });
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "cli", HandlerClass = "ReadStatusCli", ArgsJson = $"[\"--read-status\",\"--node\",\"{otherSlug}\"]", Success = true, Actor = "cli:session:s1" });
            await db.SaveChangesAsync();
        }
        await ReadBookAsync(book);
        var beat = (await BeatIdsAsync(book))[0];
        int number;
        await using (var db = await dbFactory.CreateDbContextAsync())
            number = await db.Beats.Where(b => b.Id == beat).Select(b => b.Number).SingleAsync();
        await gate.AddNoteAsync(book, number, "defect", "The rain is wrong here.", "test-reader");
        await rulings.RecordAsync(new RulingDraft("law", "No rain in the Coda.", book, Pattern: @"\brain\b"));
        await PressAsync(book, "docx");

        var j = await journal.ReadAsync(since, bookId: book);
        var kinds = j.Events.Select(e => e.Kind).ToHashSet();
        Assert.That(kinds, Is.SupersetOf(new[] { "call", "read", "note", "ruling", "press" }));
        Assert.That(j.Events.Where(e => e.Kind == "call").Select(e => e.Detail), Has.All.Contain("SpliceBeatsCli"),
            "with a book, only the calls that name it");
        Assert.That(j.Events.Single(e => e.Kind == "call").Actor, Is.EqualTo("cli:session:s1"), "each call carries its session");
        Assert.That(j.Events.Count(e => e.Kind == "read"), Is.EqualTo(2));
        Assert.That(j.Events.Select(e => e.At), Is.Ordered, "in time order");
        Assert.That(j.TemporalHistoryRead, Is.False, "SQLite keeps no temporal history, and the journal says so");
        Assert.That(j.TemporalNote, Is.Not.Null);

        var everything = await journal.ReadAsync(since);
        Assert.That(everything.Events.Count(e => e.Kind == "call"), Is.EqualTo(2), "without a book, every call");
    }

    [TestCase("6h", 6 * 60)]
    [TestCase("90m", 90)]
    [TestCase("2d", 2 * 24 * 60)]
    public void A_window_can_be_a_span_back_from_now(string s, int minutes)
    {
        Assert.That(FactoryJournal.TryParseInstant(s, out var at), Is.True);
        Assert.That((DateTime.UtcNow - at).TotalMinutes, Is.EqualTo(minutes).Within(1));
    }

    [Test]
    public void A_window_can_be_an_instant_with_or_without_an_offset()
    {
        Assert.That(FactoryJournal.TryParseInstant("2026-09-23T06:00:00", out var utc), Is.True);
        Assert.That(utc, Is.EqualTo(new DateTime(2026, 9, 23, 6, 0, 0)));
        Assert.That(FactoryJournal.TryParseInstant("2026-09-23T01:00:00-05:00", out var offset), Is.True);
        Assert.That(offset, Is.EqualTo(new DateTime(2026, 9, 23, 6, 0, 0)));
        Assert.That(FactoryJournal.TryParseInstant("yesterday", out _), Is.False);
    }
}

/// <summary>Stand-ins for factory tools, discovered only by the usage-check tests (this assembly).</summary>
public class UsageProbeTools
{
    [FactoryTool("probe_unused", "2026-01-01", Cli = "ProbeCli --probe unused")]
    public void UnusedImpl() { }

    [FactoryTool("probe_mcp_used", "2026-01-01")]
    public void McpUsedImpl() { }

    [FactoryTool("probe_cli_used", "2026-01-01", Cli = "ProbeCli --probe go")]
    public void CliUsedImpl() { }

    [FactoryTool("probe_new", "2026-09-20")]
    public void NewImpl() { }
}

[TestFixture]
public class FactoryUsageTests : PressFixture
{
    [Test]
    public async Task A_tool_no_one_calls_after_its_grace_gets_one_use_or_delete_order()
    {
        var orders = new WorkOrderService(dbFactory);
        var root = await orders.AddAsync(new WorkOrderDraft("engine", "RFC 0015 — the Novel Factory", RootApprovedBy: "author"));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { At = new DateTime(2026, 9, 1), Source = "mcp", HandlerClass = nameof(UsageProbeTools), Method = nameof(UsageProbeTools.McpUsedImpl), Success = true, Actor = "mcp:session:s" });
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { At = new DateTime(2026, 9, 2), Source = "cli", HandlerClass = "ProbeCli", ArgsJson = "[\"--universe\",\"glmz\",\"--probe\",\"go\"]", Success = true, Actor = "cli:session:s" });
            // Neither a failed call nor a test's call is use.
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { At = new DateTime(2026, 9, 3), Source = "cli", HandlerClass = "ProbeCli", ArgsJson = "[\"--probe\",\"unused\"]", Success = false, Actor = "cli:session:s" });
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { At = new DateTime(2026, 9, 3), Source = "mcp", HandlerClass = nameof(UsageProbeTools), Method = nameof(UsageProbeTools.UnusedImpl), Success = true, Actor = "test:fixture" });
            await db.SaveChangesAsync();
        }
        var check = new FactoryUsageCheck(dbFactory, orders);
        var now = new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc);

        var rows = (await check.RunAsync([typeof(UsageProbeTools).Assembly], now: now)).ToDictionary(r => r.Name);
        Assert.That(rows.Keys, Is.EquivalentTo(new[] { "probe_unused", "probe_mcp_used", "probe_cli_used", "probe_new" }));
        Assert.That(rows["probe_mcp_used"].Calls, Is.EqualTo(1));
        Assert.That(rows["probe_cli_used"].Calls, Is.EqualTo(1), "the CLI twin is a door too");
        Assert.That(rows["probe_unused"].Calls, Is.EqualTo(0));
        Assert.That(rows["probe_unused"].OrderId, Is.Not.Null);
        Assert.That(rows["probe_new"].InGrace, Is.True);
        Assert.That(rows["probe_new"].OrderId, Is.Null, "a tool inside its seven days is left alone");

        var order = (await orders.ListAsync("open")).Single(o => o.Title == "Use or delete: probe_unused");
        Assert.That(order.ParentId, Is.EqualTo(root.Id));
        Assert.That(order.ChecksJson, Does.Contain("\"ledger\"").And.Contain(nameof(UsageProbeTools.UnusedImpl)));

        await check.RunAsync([typeof(UsageProbeTools).Assembly], now: now);
        Assert.That((await orders.ListAsync("all")).Count(o => o.Title.StartsWith(FactoryUsageCheck.OrderTitlePrefix)), Is.EqualTo(1),
            "filed once, however often the check runs");
    }

    [Test]
    public async Task Without_an_open_approved_root_the_check_files_nothing_and_says_why()
    {
        var check = new FactoryUsageCheck(dbFactory, new WorkOrderService(dbFactory));
        var rows = await check.RunAsync([typeof(UsageProbeTools).Assembly], now: new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(rows.Single(r => r.Name == "probe_unused").Verdict, Does.Contain("ask the author"));
        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(await db.WorkOrders.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public void Every_factory_tool_declares_when_it_shipped_and_its_cli_twin()
    {
        var tools = FactoryUsageCheck.Discover([typeof(Prose.Mcp.FactoryTools).Assembly]);
        Assert.That(tools.Select(t => t.Tool.Name), Is.Unique);
        Assert.That(tools.Select(t => t.Tool.Name), Is.SupersetOf(new[]
        {
            "factory_status", "factory_next", "factory_context", "factory_journal", "factory_capture", "factory_usage",
            "work_order_add", "work_order_list", "work_order_close", "work_order_abandon", "session_end",
            "record_ruling", "list_rulings", "supersede_ruling", "law_violations", "record_law_violations", "book_metrics",
            "set_character_fields", "set_entity_fields", "verify_entity_begin", "verify_entity_commit",
            "read_status", "add_read_note", "list_read_notes", "resolve_read_note", "splice_beats",
        }));
        Assert.That(tools.Where(t => t.Tool.Cli == null).Select(t => t.Tool.Name), Is.Empty, "every factory tool has a CLI twin");
        foreach (var (tool, _, _) in tools)
            Assert.That(DateTime.TryParseExact(tool.Since, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _), Is.True, tool.Name);

        // Every Impl on the factory's own tool classes is marked, so a new one cannot escape the check.
        foreach (var type in new[] { typeof(Prose.Mcp.FactoryTools), typeof(Prose.Mcp.RulingTools), typeof(Prose.Mcp.WorldTools), typeof(Prose.Mcp.ReadingTools) })
            foreach (var m in type.GetMethods().Where(m => m.DeclaringType == type && m.Name.EndsWith("Impl")))
                Assert.That(m.GetCustomAttributes(typeof(FactoryToolAttribute), false), Is.Not.Empty, $"{type.Name}.{m.Name}");
    }
}
