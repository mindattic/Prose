using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>
/// The Novel Factory, I2 (RFC 0015 §9): work orders close only on Hub-validated checks and only
/// under an approved root; sessions end only with recorded decisions; stations F2/F3/F5 are
/// computed from the prose and the read receipts; the next action is the first actionable
/// blocking order, then the first failing station.
/// </summary>
[TestFixture]
public class FactoryTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NodeWorkbenchService workbench = null!;
    private ReadGateService gate = null!;
    private FactoryService factory = null!;
    private WorkOrderService orders = null!;
    private FactorySessionService sessions = null!;
    private RulingService rulings = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-factory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "factory");
        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        gate = new ReadGateService(dbFactory, workbench);
        var spine = new BookSpineService(dbFactory);
        rulings = new RulingService(dbFactory, spine);
        factory = new FactoryService(dbFactory, spine, gate, rulings, new MetricsReport(rulings), new CaptureScanner(dbFactory, spine, rulings));
        orders = new WorkOrderService(dbFactory, (book, station) => factory.StationPassesAsync(book, station));
        sessions = new FactorySessionService(dbFactory);
        HubBuildInfo.Build = "build-a";
    }

    [TearDown]
    public void TearDown()
    {
        HubBuildInfo.Build = null;
        Environment.SetEnvironmentVariable("PROSE_REPO_PATH", null);
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    // ── work orders ───────────────────────────────────────────────────────────

    [Test]
    public void A_root_order_without_the_authors_approval_is_refused()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(() => orders.AddAsync(new WorkOrderDraft("engine", "Unapproved root")));
        Assert.That(ex!.Message, Does.Contain("author"));
    }

    [Test]
    public async Task Children_of_an_approved_root_are_accepted_and_orphans_are_not()
    {
        var root = await orders.AddAsync(new WorkOrderDraft("engine", "RFC 0015", RootApprovedBy: "author"));
        var child = await orders.AddAsync(new WorkOrderDraft("engine", "I2", ParentId: root.Id));
        Assert.That(child.ParentId, Is.EqualTo(root.Id));
        Assert.ThrowsAsync<ArgumentException>(() => orders.AddAsync(new WorkOrderDraft("engine", "x", ParentId: Guid.NewGuid())));
    }

    [Test]
    public async Task An_orders_full_detail_can_be_read_back_whatever_its_status()
    {
        var root = await orders.AddAsync(new WorkOrderDraft("engine", "Root", RootApprovedBy: "author"));
        var detail = "Line one of the detail.\nLine two, " + new string('x', 600) + ".";
        var child = await orders.AddAsync(new WorkOrderDraft("engine", "Child", detail, ParentId: root.Id,
            Paths: ["src\\Prose.Core/**", "docs/CLI_COMMANDS.md"], ChecksJson: """[{"type":"commit"}]""", Blocking: true));

        var view = await orders.GetAsync(child.Id);
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Title, Is.EqualTo("Child"));
        Assert.That(view.Kind, Is.EqualTo("engine"));
        Assert.That(view.Status, Is.EqualTo(WorkOrderStatus.Open));
        Assert.That(view.Blocking, Is.True);
        Assert.That(view.ParentId, Is.EqualTo(root.Id));
        Assert.That(view.Detail, Is.EqualTo(detail), "the whole detail, not a summary");
        Assert.That(view.Paths, Is.EqualTo(new[] { "src/Prose.Core/**", "docs/CLI_COMMANDS.md" }));
        Assert.That(view.Checks.Single()!["type"]!.GetValue<string>(), Is.EqualTo("commit"));

        var text = view.Render();
        Assert.That(text, Does.Contain("Child").And.Contain(root.Id.ToString()).And.Contain("src/Prose.Core/**")
            .And.Contain("\"commit\"").And.Contain("Line two").And.Contain("blocking"));

        await orders.AbandonAsync(child.Id, "done with it");
        Assert.That((await orders.GetAsync(child.Id))!.Status, Is.EqualTo(WorkOrderStatus.Abandoned), "a closed order is still readable");
        Assert.That(await orders.GetAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void An_engine_order_with_a_commit_check_must_declare_paths()
    {
        Assert.ThrowsAsync<ArgumentException>(() => orders.AddAsync(new WorkOrderDraft("engine", "no paths",
            RootApprovedBy: "author", ChecksJson: """[{"type":"commit"}]""")));
    }

    [Test]
    public async Task A_commit_check_needs_the_commit_on_HEAD_and_inside_the_declared_paths()
    {
        var repo = Path.Combine(tempRoot, "repo");
        Directory.CreateDirectory(repo);
        Git(repo, "init", "-q");
        Git(repo, "config", "user.email", "t@t");
        Git(repo, "config", "user.name", "t");
        Directory.CreateDirectory(Path.Combine(repo, "src"));
        File.WriteAllText(Path.Combine(repo, "src", "a.cs"), "a");
        Git(repo, "add", "-A");
        Git(repo, "commit", "-q", "-m", "a");
        var inside = Git(repo, "rev-parse", "HEAD").Trim();
        File.WriteAllText(Path.Combine(repo, "outside.txt"), "b");
        Git(repo, "add", "-A");
        Git(repo, "commit", "-q", "-m", "b");
        var outside = Git(repo, "rev-parse", "HEAD").Trim();
        Environment.SetEnvironmentVariable("PROSE_REPO_PATH", repo);

        var good = await orders.AddAsync(new WorkOrderDraft("engine", "good", RootApprovedBy: "author",
            Paths: ["src/**"], ChecksJson: """[{"type":"commit"}]"""));
        var bad = await orders.AddAsync(new WorkOrderDraft("engine", "bad", RootApprovedBy: "author",
            Paths: ["src/**"], ChecksJson: """[{"type":"commit"}]"""));

        Assert.That((await orders.CloseAsync(good.Id, new CloseInputs(CommitHash: inside))).Closed, Is.True);
        var refused = await orders.CloseAsync(bad.Id, new CloseInputs(CommitHash: outside));
        Assert.That(refused.Closed, Is.False);
        Assert.That(refused.Checks.Single().Detail, Does.Contain("outside"));
        Assert.That((await orders.CloseAsync(bad.Id, new CloseInputs(CommitHash: "0000000000000000000000000000000000000000"))).Closed, Is.False);
    }

    [Test]
    public async Task The_regenerated_MCP_tool_doc_rides_with_an_in_scope_tool_change()
    {
        var repo = Path.Combine(tempRoot, "repo-tooldoc");
        Directory.CreateDirectory(Path.Combine(repo, "src", "Prose.Mcp"));
        Directory.CreateDirectory(Path.Combine(repo, "docs"));
        Git(repo, "init", "-q");
        Git(repo, "config", "user.email", "t@t");
        Git(repo, "config", "user.name", "t");
        File.WriteAllText(Path.Combine(repo, "src", "Prose.Mcp", "Tools.Plant.cs"), "a");
        File.WriteAllText(Path.Combine(repo, "docs", "MCP_TOOLS.md"), "a");
        Git(repo, "add", "-A");
        Git(repo, "commit", "-q", "-m", "tool + regenerated doc");
        var withTool = Git(repo, "rev-parse", "HEAD").Trim();
        File.WriteAllText(Path.Combine(repo, "docs", "MCP_TOOLS.md"), "b");
        Git(repo, "add", "-A");
        Git(repo, "commit", "-q", "-m", "doc alone");
        var docAlone = Git(repo, "rev-parse", "HEAD").Trim();
        Environment.SetEnvironmentVariable("PROSE_REPO_PATH", repo);

        var inScope = await orders.AddAsync(new WorkOrderDraft("engine", "tool", RootApprovedBy: "author",
            Paths: ["src/Prose.Mcp/Tools.Plant.cs"], ChecksJson: """[{"type":"commit"}]"""));
        Assert.That((await orders.CloseAsync(inScope.Id, new CloseInputs(CommitHash: withTool))).Closed, Is.True);

        var toolOutOfScope = await orders.AddAsync(new WorkOrderDraft("engine", "other", RootApprovedBy: "author",
            Paths: ["src/Other/**"], ChecksJson: """[{"type":"commit"}]"""));
        Assert.That((await orders.CloseAsync(toolOutOfScope.Id, new CloseInputs(CommitHash: withTool))).Closed, Is.False);

        var docOnly = await orders.AddAsync(new WorkOrderDraft("engine", "doc", RootApprovedBy: "author",
            Paths: ["src/Prose.Mcp/Tools.Plant.cs"], ChecksJson: """[{"type":"commit"}]"""));
        var refused = await orders.CloseAsync(docOnly.Id, new CloseInputs(CommitHash: docAlone));
        Assert.That(refused.Closed, Is.False, "a doc edit with no tool change is not the hook's output");
        Assert.That(refused.Checks.Single().Detail, Does.Contain("docs/MCP_TOOLS.md"));
    }

    [Test]
    public async Task A_tests_check_reads_the_TRX_and_needs_every_named_test_passed()
    {
        var trx = Path.Combine(tempRoot, "run.trx");
        File.WriteAllText(trx, """
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Times start="2026-09-23T10:00:00.0000000+00:00" />
              <Results>
                <UnitTestResult testId="a" testName="Foo_passes" outcome="Passed" />
                <UnitTestResult testId="b" testName="Bar_fails" outcome="Failed" />
              </Results>
              <TestDefinitions>
                <UnitTest id="a" name="Foo_passes"><TestMethod className="Prose.UnitTests.AlphaTests" name="Foo_passes" /></UnitTest>
                <UnitTest id="b" name="Bar_fails"><TestMethod className="Prose.UnitTests.BetaTests" name="Bar_fails" /></UnitTest>
              </TestDefinitions>
            </TestRun>
            """);
        var pass = await orders.AddAsync(new WorkOrderDraft("engine", "alpha", RootApprovedBy: "author",
            ChecksJson: """[{"type":"tests","names":["AlphaTests"]}]"""));
        var fail = await orders.AddAsync(new WorkOrderDraft("engine", "beta", RootApprovedBy: "author",
            ChecksJson: """[{"type":"tests","names":["BetaTests"]}]"""));
        var missing = await orders.AddAsync(new WorkOrderDraft("engine", "gamma", RootApprovedBy: "author",
            ChecksJson: """[{"type":"tests","names":["GammaTests"]}]"""));

        Assert.That((await orders.CloseAsync(pass.Id, new CloseInputs(TrxPath: trx))).Closed, Is.True);
        Assert.That((await orders.CloseAsync(fail.Id, new CloseInputs(TrxPath: trx))).Closed, Is.False);
        Assert.That((await orders.CloseAsync(missing.Id, new CloseInputs(TrxPath: trx))).Checks.Single().Detail, Does.Contain("no results"));
    }

    [Test]
    public async Task A_ledger_check_counts_only_real_calls_made_since_the_order_opened()
    {
        var order = await orders.AddAsync(new WorkOrderDraft("engine", "use it", RootApprovedBy: "author",
            ChecksJson: """[{"type":"ledger","handler":"FactoryTools","minCalls":1}]"""));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "mcp", HandlerClass = "FactoryTools", Success = true, Actor = "test:fixture", At = DateTime.UtcNow });
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "mcp", HandlerClass = "FactoryTools", Success = true, At = order.OpenedAt.AddMinutes(-5) });
            await db.SaveChangesAsync();
        }
        Assert.That((await orders.CloseAsync(order.Id, new CloseInputs())).Closed, Is.False, "test-actor and pre-order calls do not count");

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "mcp", HandlerClass = "FactoryTools", Success = true, Actor = "mcp:session:x", At = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        Assert.That((await orders.CloseAsync(order.Id, new CloseInputs())).Closed, Is.True);
    }

    [Test]
    public async Task A_ledger_check_with_a_CLI_door_closes_on_a_CLI_call_of_the_tool()
    {
        var order = await orders.AddAsync(new WorkOrderDraft("engine", "use it", RootApprovedBy: "author",
            ChecksJson: """[{"type":"ledger","handler":"FactoryTools","method":"FactoryJournalImpl","minCalls":1,"cli":"FactoryCli --factory journal"}]"""));
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "cli", HandlerClass = "FactoryCli", ArgsJson = """["--factory","next"]""", Success = true, At = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        Assert.That((await orders.CloseAsync(order.Id, new CloseInputs())).Closed, Is.False, "another verb of the same CLI is not this tool");

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.CommandLedgerEntries.Add(new CommandLedgerEntry { Source = "cli", HandlerClass = "FactoryCli", ArgsJson = """["--factory","journal","--node","bcoda"]""", Success = true, At = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        Assert.That((await orders.CloseAsync(order.Id, new CloseInputs())).Closed, Is.True);
    }

    [Test]
    public async Task A_deploy_check_needs_the_Hub_build_to_have_changed()
    {
        var order = await orders.AddAsync(new WorkOrderDraft("engine", "deploy", RootApprovedBy: "author",
            ChecksJson: """[{"type":"deploy"}]"""));
        Assert.That((await orders.CloseAsync(order.Id, new CloseInputs())).Closed, Is.False);
        HubBuildInfo.Build = "build-b";
        Assert.That((await orders.CloseAsync(order.Id, new CloseInputs())).Closed, Is.True);
    }

    [Test]
    public async Task A_deploy_check_fails_when_the_opening_build_is_unknown()
    {
        HubBuildInfo.Build = null;
        var order = await orders.AddAsync(new WorkOrderDraft("engine", "deploy", RootApprovedBy: "author",
            ChecksJson: """[{"type":"deploy"}]"""));
        HubBuildInfo.Build = "build-b";
        var result = await orders.CloseAsync(order.Id, new CloseInputs());
        Assert.That(result.Closed, Is.False);
        Assert.That(result.Checks.Single().Detail, Does.Contain("unknown"));
    }

    [Test]
    public void A_tests_check_names_a_method_exactly_not_by_prefix()
    {
        var run = new TrxReader.TrxRun(DateTimeOffset.UtcNow, [
            new TrxReader.TrxResult("AlphaTests", "CloseAsync_refuses", "Passed"),
            new TrxReader.TrxResult("AlphaTests", "Parses(\"x\")", "Passed"),
        ]);
        Assert.That(TrxReader.Check(run, ["AlphaTests.Close"]).Ok, Is.False, "a prefix of another test's name is not that test");
        Assert.That(TrxReader.Check(run, ["AlphaTests.CloseAsync_refuses"]).Ok, Is.True);
        Assert.That(TrxReader.Check(run, ["AlphaTests.Parses"]).Ok, Is.True, "parameterised cases still match");
    }

    [Test]
    public async Task A_parent_closes_itself_when_its_last_child_closes_and_not_before()
    {
        var root = await orders.AddAsync(new WorkOrderDraft("engine", "root", RootApprovedBy: "author"));
        var a = await orders.AddAsync(new WorkOrderDraft("engine", "a", ParentId: root.Id));
        var b = await orders.AddAsync(new WorkOrderDraft("engine", "b", ParentId: root.Id));

        Assert.That((await orders.CloseAsync(root.Id, new CloseInputs())).Refusal, Does.Contain("still open"));
        var first = await orders.CloseAsync(a.Id, new CloseInputs());
        Assert.That(first.AutoClosedParents, Is.Empty);
        Assert.That(await orders.AbandonAsync(b.Id, "superseded"), Is.True);

        var list = await orders.ListAsync("all");
        Assert.That(list.Single(o => o.Id == root.Id).Status, Is.EqualTo(WorkOrderStatus.Closed));
        Assert.ThrowsAsync<ArgumentException>(() => orders.AbandonAsync(a.Id, ""));
    }

    [Test]
    public void Path_globs_cross_folders_only_with_double_star()
    {
        Assert.That(PathGlob.MatchesAny("src/Prose.Core/Services/Factory/X.cs", ["src/Prose.Core/Services/Factory/**"]), Is.True);
        Assert.That(PathGlob.MatchesAny("src/Prose.Core/Services/X.cs", ["src/Prose.Core/Services/Factory/**"]), Is.False);
        Assert.That(PathGlob.MatchesAny("docs/rfc/0015-novel-factory.md", ["docs/rfc/*.md"]), Is.True);
        Assert.That(PathGlob.MatchesAny("docs/rfc/sub/x.md", ["docs/rfc/*.md"]), Is.False);
    }

    // ── sessions ──────────────────────────────────────────────────────────────

    [Test]
    public async Task A_session_ends_only_when_every_decision_points_at_a_record_made_this_session()
    {
        var start = await sessions.StartAsync("claude-1", "abc", null);
        var (ok, problems, _) = await sessions.EndAsync(start.SessionId,
            """{"done":["x"],"decisions":[{"text":"Chen is 79"}],"next":"read"}""", null);
        Assert.That(ok, Is.False);
        Assert.That(problems.Single(), Does.Contain("not recorded"));

        var root = await orders.AddAsync(new WorkOrderDraft("author", "Heal BCODA", RootApprovedBy: "author"));
        (ok, problems, _) = await sessions.EndAsync(start.SessionId,
            $$"""{"done":["x"],"decisions":[{"text":"heal BCODA","orderId":"{{root.Id}}"}],"next":"read"}""", "def");
        Assert.That(ok, Is.True, string.Join("; ", problems));

        var next = await sessions.StartAsync("claude-2", "def", null);
        Assert.That(next.LastSummaryJson, Does.Contain("heal BCODA"));
    }

    [Test]
    public async Task A_session_end_without_an_id_is_refused_while_several_sessions_are_open()
    {
        var mine = await sessions.StartAsync("claude-1", null, null);
        var theirs = await sessions.StartAsync("claude-2", null, null);
        const string summary = """{"done":["x"],"decisions":[],"next":"read"}""";

        var (ok, problems, _) = await sessions.EndAsync(null, summary, null);
        Assert.That(ok, Is.False);
        Assert.That(problems.Single(), Does.Contain(mine.SessionId.ToString()).And.Contain(theirs.SessionId.ToString()));

        (ok, _, var ended) = await sessions.EndAsync(mine.SessionId, summary, null);
        Assert.That(ok, Is.True);
        Assert.That(ended, Is.EqualTo(mine.SessionId));

        (ok, _, ended) = await sessions.EndAsync(null, summary, null);
        Assert.That(ok, Is.True, "with one session left open, no id is unambiguous");
        Assert.That(ended, Is.EqualTo(theirs.SessionId));
    }

    [Test]
    public async Task An_ended_session_cannot_be_ended_again_by_id()
    {
        var s = await sessions.StartAsync("claude-1", null, null);
        var (ok, _, _) = await sessions.EndAsync(s.SessionId, """{"done":["first"],"decisions":[],"next":"read"}""", null);
        Assert.That(ok, Is.True);
        (ok, var problems, _) = await sessions.EndAsync(s.SessionId, """{"done":["second"],"decisions":[],"next":"x"}""", null);
        Assert.That(ok, Is.False);
        Assert.That(problems.Single(), Does.Contain("already ended"));
        var next = await sessions.StartAsync("claude-2", null, null);
        Assert.That(next.LastSummaryJson, Does.Contain("first").And.Not.Contain("second"));
    }

    [Test]
    public async Task The_actor_tag_names_the_open_session()
    {
        Assert.That(await sessions.ActorTagAsync("mcp"), Is.EqualTo("mcp"));
        var s = await sessions.StartAsync("claude-1", null, null);
        Assert.That(await sessions.ActorTagAsync("cli"), Is.EqualTo($"cli:session:{s.SessionId}"));
    }

    [Test]
    public async Task A_compacted_session_resumes_its_own_row_and_is_not_its_own_other_session()
    {
        // SessionStart fires again under the same Claude session id after a compaction (found live
        // 2026-09-23: the start block warned "another session may be working in this tree" about itself).
        var first = await sessions.StartAsync("claude-1", "abc", null);
        var again = await sessions.StartAsync("claude-1", "abc", null);
        Assert.That(again.SessionId, Is.EqualTo(first.SessionId));
        Assert.That(again.OtherOpenSessions, Is.Empty);

        var other = await sessions.StartAsync("claude-2", "abc", null);
        Assert.That(other.SessionId, Is.Not.EqualTo(first.SessionId));
        Assert.That(other.OtherOpenSessions.Select(o => o.Id), Is.EqualTo(new[] { first.SessionId }));
    }

    // ── stations and the next action ──────────────────────────────────────────

    private async Task<(Guid Book, List<Guid> Beats)> BookWithTwoChaptersAsync()
    {
        Guid bookId, ch1, ch2;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var book = new BookNode { Id = Guid.CreateVersion7(), Slug = "fx-" + Guid.NewGuid().ToString("N")[..8], Title = "FX", Kind = "book", Status = "draft", SortKey = 100 };
            db.Nodes.Add(book);
            await db.SaveChangesAsync();
            bookId = book.Id;
        }
        ch1 = await AddChapterAsync(bookId, "Chapter 1 — One", 1);
        ch2 = await AddChapterAsync(bookId, "Chapter 2 — Two", 2);
        var a = await workbench.InsertBeatAsync(ch1, null, "The first beat.");
        var b = await workbench.InsertBeatAsync(ch1, a.Id, "The second beat.");
        var c = await workbench.InsertBeatAsync(ch2, null, "");
        return (bookId, [a.Id, b.Id, c.Id]);
    }

    private async Task<Guid> AddChapterAsync(Guid bookId, string title, int sort)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ch = new ChapterNode { Id = Guid.CreateVersion7(), Slug = "ch-" + Guid.NewGuid().ToString("N")[..8], Title = title, Kind = "chapter", Status = "draft", SortKey = sort, ParentNodeId = bookId };
        db.Nodes.Add(ch);
        await db.SaveChangesAsync();
        return ch.Id;
    }

    [Test]
    public async Task F2_F3_F5_are_computed_per_chapter()
    {
        var (book, beats) = await BookWithTwoChaptersAsync();
        var s = await factory.StatusAsync(book);
        Assert.That(s.Units, Has.Count.EqualTo(2));
        Assert.That(s.Units[0].Stations["F3"].Pass, Is.True);
        Assert.That(s.Units[1].Stations["F2"].Pass, Is.False, "an empty beat with no title/description is unplanned");
        Assert.That(s.Units[1].Stations["F3"].Pass, Is.False);
        Assert.That(s.Units[0].Stations["F5"].Pass, Is.False, "nothing has been read");
        Assert.That(s.Units[0].Stations["F4"].State, Is.EqualTo("pass"), "a unit whose prose names nothing is captured");

        var ordered = await workbench.GetOrderedBeatsAsync(book);
        await gate.MarkReadAsync(book, ordered.Where(o => o.Beat.Id != beats[2]).Select(o => (o.Beat.Id, o.Beat.TextHash ?? "")), "test");
        s = await factory.StatusAsync(book);
        Assert.That(s.Units[0].Stations["F5"].Pass, Is.True);
    }

    [Test]
    public async Task A_book_with_no_beats_passes_no_station()
    {
        Guid bookId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var book = new BookNode { Id = Guid.CreateVersion7(), Slug = "empty-" + Guid.NewGuid().ToString("N")[..8], Title = "Empty", Kind = "book", Status = "draft", SortKey = 100 };
            db.Nodes.Add(book);
            await db.SaveChangesAsync();
            bookId = book.Id;
        }
        var (ok, detail) = await factory.StationPassesAsync(bookId, "F5");
        Assert.That(ok, Is.False, detail);
        Assert.That(detail, Does.Contain("no units"));
    }

    [Test]
    public async Task A_beat_whose_stored_hash_is_null_can_still_be_read()
    {
        var (book, beats) = await BookWithTwoChaptersAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
            await db.Beats.Where(b => b.Id == beats[0]).ExecuteUpdateAsync(s => s.SetProperty(b => b.TextHash, (string?)null));

        var ordered = await workbench.GetOrderedBeatsAsync(book);
        var first = ordered.Single(o => o.Beat.Id == beats[0]).Beat;
        Assert.That(first.TextHash, Is.Null);
        var marked = await gate.MarkReadAsync(book, [(first.Id, ReadGateService.HashOf(first))], "test");
        Assert.That(marked, Is.EqualTo(1));
        var status = await gate.GetStatusAsync(book);
        Assert.That(status.Unread.Any(u => u.BeatId == beats[0]), Is.False);
    }

    [Test]
    public async Task A_ruling_recorded_on_a_chapter_applies_to_its_book()
    {
        var (book, _) = await BookWithTwoChaptersAsync();
        Guid chapter;
        await using (var db = await dbFactory.CreateDbContextAsync())
            chapter = await db.Nodes.Where(n => n.ParentNodeId == book).OrderBy(n => n.SortKey).Select(n => n.Id).FirstAsync();

        var row = await rulings.RecordAsync(new RulingDraft(RulingKinds.Incidental, "Morrison is an invented street", chapter, null, "Morrison"));
        Assert.That(row.BookId, Is.EqualTo(book));
        Assert.That((await rulings.ListAsync(book)).Select(r => r.Id), Does.Contain(row.Id));
    }

    [Test]
    public void A_universe_wide_ruling_with_no_named_universe_is_refused_not_filed_in_the_default()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(() => rulings.RecordAsync(new RulingDraft(RulingKinds.Law, "no guns", null, null, null)));
        Assert.That(ex!.Message, Does.Contain("needs a universe"));
    }

    [Test]
    public async Task A_ruling_superseded_twice_leaves_one_active_replacement()
    {
        var (book, _) = await BookWithTwoChaptersAsync();
        var old = await rulings.RecordAsync(new RulingDraft(RulingKinds.Law, "no guns", book, null, null));
        await rulings.SupersedeAsync(old.Id, new RulingDraft(RulingKinds.Law, "no guns, ever", null, null, null));
        Assert.ThrowsAsync<ArgumentException>(() => rulings.SupersedeAsync(old.Id, new RulingDraft(RulingKinds.Law, "guns are fine", null, null, null)));
        var active = (await rulings.ListAsync(book)).Where(r => r.Kind == RulingKinds.Law).Select(r => r.Text).ToList();
        Assert.That(active, Is.EqualTo(new[] { "no guns, ever" }));
    }

    [Test]
    public async Task Next_is_the_first_blocking_leaf_then_the_first_failing_station_of_a_book_on_the_line()
    {
        var (book, _) = await BookWithTwoChaptersAsync();
        var root = await orders.AddAsync(new WorkOrderDraft("engine", "RFC 0015", RootApprovedBy: "author"));
        var i2 = await orders.AddAsync(new WorkOrderDraft("engine", "I2", ParentId: root.Id, Blocking: true));
        var leaf = await orders.AddAsync(new WorkOrderDraft("engine", "I2 leaf", ParentId: i2.Id));
        await orders.AddAsync(new WorkOrderDraft("author", "Heal FX", ParentId: root.Id, NodeId: book));

        var next = await factory.NextAsync();
        Assert.That(next.Kind, Is.EqualTo("order"));
        Assert.That(next.OrderId, Is.EqualTo(leaf.Id), "a blocking parent is worked through its first open child");

        await orders.CloseAsync(leaf.Id, new CloseInputs());   // auto-closes I2 (no checks of its own)
        next = await factory.NextAsync();
        Assert.That(next.Kind, Is.EqualTo("station"));
        Assert.That(next.Unit, Is.EqualTo(1));
        Assert.That(next.Station, Is.EqualTo("F5"), "unit 1 is planned and written, so the first failing station is Read");
        Assert.That(next.Calls[0], Does.Contain("--mark-read"));
    }

    private static string Git(string repo, params string[] args)
    {
        var psi = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        psi.ArgumentList.Add("-C"); psi.ArgumentList.Add(repo);
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var o = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return o;
    }
}
