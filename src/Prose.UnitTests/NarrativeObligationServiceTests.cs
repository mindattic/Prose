using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The Narrative Obligation Ledger (RFC 0013), tested the way this repo validates instruments:
/// the known-real defects reconstructed in miniature — the girl behind the curtain in Chapter 1 of
/// BCODA who is never mentioned again in 475 beats, and the child in the ductwork nobody explains
/// — against a scripted LLM, so the contract (quote gate, hash gate, locked rows, trial balance,
/// COULD NOT LOOK) is pinned without spending a token.
/// </summary>
[TestFixture]
public class NarrativeObligationServiceTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ScriptedLlm llm = null!;
    private NarrativeObligationService svc = null!;

    private Guid bookId;
    private readonly List<Guid> chapterIds = new();
    private readonly List<Guid> beatIds = new();   // reading order: 2 beats per chapter, 5 chapters

    private const string CurtainBeat =
        "Not the woman. The girl. Behind the curtain in the hallway, maybe thirteen, maybe younger. " +
        "She'd just been there in the gap where the curtain didn't quite meet the frame, elbows on her knees, " +
        "watching him the way children watch a thing they've already decided to remember. He almost believed it.";

    private const string DuctBeat =
        "Behind the grating, in the dark of the duct, he could see the shape of her: small, compact, pressed back " +
        "against the far wall where the light could not reach. She had watched the whole thing. Nothing in the room " +
        "said why a child was here at all.";

    [SetUp]
    public async Task SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        llm = new ScriptedLlm();
        svc = new NarrativeObligationService(dbFactory, NullLogger<NarrativeObligationService>.Instance,
            new NarrativeObligationExtractor(llm, NullLogger<NarrativeObligationExtractor>.Instance));

        await using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        bookId = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode { Id = bookId, Slug = "ledger-book", NodeCode = "LDG", Title = "Ledger Book", Kind = "book", UniverseId = Universe.GlmzId });
        chapterIds.Clear(); beatIds.Clear();
        var number = 0;
        for (var c = 1; c <= 5; c++)
        {
            var chId = Guid.CreateVersion7();
            chapterIds.Add(chId);
            db.Nodes.Add(new ChapterNode { Id = chId, Slug = $"ledger-ch{c}", Title = $"Chapter {c}", Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId, SortKey = c * 100 });
            for (var b = 0; b < 2; b++)
            {
                var text = c == 1 && b == 1 ? CurtainBeat
                         : c == 2 && b == 0 ? DuctBeat
                         : $"Chapter {c} beat {b}: Kyle rode north through the rain and said nothing to anyone.";
                var id = Guid.CreateVersion7();
                beatIds.Add(id);
                db.Beats.Add(new Beat { Id = id, Number = ++number, Text = text, TextHash = Beat.ComputeHash(text), ObligationScanHash = "seeded" });
                db.BeatNodes.Add(new BeatNode { NodeId = chId, BeatId = id, SortKey = (b + 1) * 100 });
            }
        }
        await db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() { conn.Close(); conn.Dispose(); }

    private Guid CurtainBeatId => beatIds[1];
    private Guid DuctBeatId    => beatIds[2];

    private static string OpenedJson(string kind, string description, string quote, string? referentLabel = null, string? referentType = null, string dueHint = "chapter") =>
        $$"""
        {"reasoning":"The beat introduces a watcher it never explains.",
         "opened":[{"reasoning":"a weighted, unnamed watcher","quote":"{{quote}}","kind":"{{kind}}","description":"{{description}}",
                    "referent":{{(referentLabel is null ? "null" : $$"""{"label":"{{referentLabel}}","type":"{{referentType}}"}""")}},
                    "trigger":null,"due_hint":"{{dueHint}}"}],
         "touched":[]}
        """;

    // ── the known-real defects, in miniature ──────────────────────────────────

    [Test]
    public async Task GirlBehindCurtain_OpensReferentObligation_WithStubEntityAndQuote()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "A girl (~13) watches Kyle from behind the curtain; she is never explained",
            "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character"));

        var r = await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);

        Assert.That(r.Skipped, Is.False);
        Assert.That(r.Opened, Is.EqualTo(1));
        Assert.That(r.DiscardedUngrounded, Is.EqualTo(0));

        await using var db = dbFactory.CreateDbContext();
        var row = await db.NarrativeObligations.SingleAsync();
        Assert.That(row.Kind, Is.EqualTo(ObligationKind.IntroducedReferent));
        Assert.That(row.State, Is.EqualTo(ObligationState.Open));
        Assert.That(row.Provenance, Is.EqualTo(ClaimProvenance.Observed));
        Assert.That(row.OriginBeatId, Is.EqualTo(CurtainBeatId));
        Assert.That(row.OriginQuote, Does.Contain("curtain didn't quite meet the frame"));
        Assert.That(row.DueByKind, Is.EqualTo(ObligationDueKind.Chapter));
        Assert.That(row.DueByValue, Is.EqualTo(1), "due_hint 'chapter' = by the end of the origin chapter");
        Assert.That(row.AuthorLocked, Is.False);

        var stub = await db.Entities.IgnoreQueryFilters().SingleAsync(e => e.Id == row.EntityId);
        Assert.That(stub.Name, Is.EqualTo("(unnamed) girl behind the curtain"));
        Assert.That(stub.Status, Is.EqualTo("stub"));
        Assert.That(stub.Description, Does.Contain("curtain"));
        Assert.That(stub.OriginNodeId, Is.EqualTo(bookId));

        var events = await db.NarrativeObligationEvents.Where(e => e.ObligationId == row.Id).ToListAsync();
        Assert.That(events.Select(e => e.Action), Is.EquivalentTo(new[] { ObligationEventAction.Open }));
    }

    [Test]
    public async Task CurtainGirl_NeverMentionedAgain_IsOverdueWithoutDecision_AtBookEnd()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "girl behind the curtain, unexplained",
            "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character"));
        await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);

        var tb = await svc.TrialBalanceAsync(bookId, chapterOrdinal: null);

        Assert.That(tb.CouldNotLook, Is.False);
        Assert.That(tb.ScannedBeats, Is.EqualTo(tb.TotalBeats));
        Assert.That(tb.OverdueWithoutDecision, Has.Count.EqualTo(1));
        Assert.That(tb.OverdueWithoutDecision[0].OriginChapter, Is.EqualTo(1));
        Assert.That(tb.OverdueWithoutDecision[0].Overdue, Is.True);
        Assert.That(tb.Balanced, Is.False);
        Assert.That(tb.CarriedForward, Is.EqualTo(1));
    }

    [Test]
    public async Task CurtainGirl_NotOverdue_WhileStillInsideHerDueChapter()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "girl behind the curtain", "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character", dueHint: "book"));
        await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);

        var tb = await svc.TrialBalanceAsync(bookId, chapterOrdinal: 1);
        Assert.That(tb.OverdueWithoutDecision, Is.Empty, "a book-end debt is never overdue before the end");
        Assert.That(tb.OpenedHere, Has.Count.EqualTo(1));
        Assert.That(tb.Balanced, Is.True);
    }

    [Test]
    public async Task DuctworkChild_QuestionOpened_ThenPaidByALaterBeat_ClosesWithQuote()
    {
        llm.Enqueue(OpenedJson("unexplained-presence", "Why is a child hidden in the ductwork at a professional ambush?",
            "Nothing in the room said why a child was here at all", "child in the ductwork", "character", "book"));
        await svc.ScanBeatAsync(bookId, DuctBeatId, DuctBeat, ObligationActor.SystemExtract);

        // A later beat pays it: the extractor reports "touched" index 1 as closed with a grounded quote.
        var payoffBeat = beatIds[6]; // chapter 4
        const string payoffText = "She had her father's jaw and her mother's eyes. The jaw-rig man had brought his daughter to the game because there was nowhere else to leave her.";
        await using (var db = dbFactory.CreateDbContext())
        {
            var b = await db.Beats.SingleAsync(x => x.Id == payoffBeat);
            b.Text = payoffText; b.TextHash = Beat.ComputeHash(payoffText); b.ObligationScanHash = null;
            await db.SaveChangesAsync();
        }
        llm.Enqueue("""{"reasoning":"pays the ductwork question","opened":[],"touched":[{"reasoning":"explains her presence","index":1,"quote":"had brought his daughter to the game because there was nowhere else to leave her","verdict":"closed"}]}""");

        var r = await svc.ScanBeatAsync(bookId, payoffBeat, payoffText, ObligationActor.SystemExtract);
        Assert.That(r.Closed, Is.EqualTo(1));

        await using var db2 = dbFactory.CreateDbContext();
        var row = await db2.NarrativeObligations.SingleAsync();
        Assert.That(row.State, Is.EqualTo(ObligationState.Closed));
        Assert.That(row.ClosingBeatId, Is.EqualTo(payoffBeat));
        Assert.That(row.ClosingQuote, Does.Contain("nowhere else to leave her"));
        var tb = await svc.TrialBalanceAsync(bookId, null);
        Assert.That(tb.OverdueWithoutDecision, Is.Empty);
        Assert.That(tb.Closed, Is.EqualTo(1));
        Assert.That(tb.Balanced, Is.True);
    }

    // ── the gates ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Extractor_UngroundedQuote_IsDiscardedNotRecorded()
    {
        llm.Enqueue(OpenedJson("promise", "invented promise", "this sentence is not in the beat at all", null, null));

        var r = await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);

        Assert.That(r.Opened, Is.EqualTo(0));
        Assert.That(r.DiscardedUngrounded, Is.EqualTo(1));
        await using var db = dbFactory.CreateDbContext();
        Assert.That(await db.NarrativeObligations.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task HashGate_SecondScanOfUnchangedText_IsFreeAndSkipped()
    {
        llm.Enqueue(OpenedJson("promise", "x", "gap where the curtain didn't quite meet the frame"));
        var first = await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);
        var second = await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat + "\n\n   ", ObligationActor.SystemRescan);

        Assert.That(first.Skipped, Is.False);
        Assert.That(second.Skipped, Is.True, "whitespace-only change must not bill a scan");
        Assert.That(llm.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task ProviderOutage_DoesNotStampTheGate_SoTheBeatIsRescannedNextTime()
    {
        // A real outage fails every attempt. Since v8 the extractor retries a failed window before
        // giving up on it, so one enqueued failure is a blip that recovers — which is the point of
        // the retry, and why this test has to exhaust the attempts to still describe an outage.
        llm.Enqueue(null); llm.Enqueue(null); llm.Enqueue(null);
        var r = await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);
        Assert.That(r.Evaluated, Is.False);

        await using var db = dbFactory.CreateDbContext();
        var beat = await db.Beats.SingleAsync(b => b.Id == CurtainBeatId);
        Assert.That(beat.ObligationScanHash, Is.EqualTo("seeded"), "an outage must not look like a completed scan");

        var attempt = await db.ObligationScanAttempts.SingleAsync(a => a.BeatId == CurtainBeatId);
        Assert.That(attempt.Outcome, Is.EqualTo(ObligationScanOutcome.Unread),
            "the reason a beat went unread is now a row, not just a log line");
        Assert.That(attempt.Failure, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task ATransientFailure_IsRetried_AndTheBeatIsStampedNormally()
    {
        llm.Enqueue(null); // one blip
        llm.Enqueue(OpenedJson("promise", "x", "gap where the curtain didn't quite meet the frame"));
        var r = await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);

        Assert.That(r.Evaluated, Is.True, "the retry landed, so the beat WAS read");
        await using var db = dbFactory.CreateDbContext();
        var beat = await db.Beats.SingleAsync(b => b.Id == CurtainBeatId);
        Assert.That(beat.ObligationScanHash, Is.Not.EqualTo("seeded"));
        var attempt = await db.ObligationScanAttempts.SingleAsync(a => a.BeatId == CurtainBeatId);
        Assert.That(attempt.Outcome, Is.EqualTo(ObligationScanOutcome.Read));
    }

    [Test]
    public async Task Rescan_WithdrawsUnlockedUnadvancedRow_WhoseQuoteVanished()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "girl behind the curtain", "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character"));
        await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);

        const string edited = "Not the woman. Nobody else was in the apartment. He pocketed the credstick and took the stairs down.";
        llm.Enqueue("""{"reasoning":"nothing new","opened":[],"touched":[]}""");
        var r = await svc.ScanBeatAsync(bookId, CurtainBeatId, edited, ObligationActor.SystemRescan);

        Assert.That(r.Withdrawn, Is.EqualTo(1));
        await using var db = dbFactory.CreateDbContext();
        var row = await db.NarrativeObligations.SingleAsync();
        Assert.That(row.State, Is.EqualTo(ObligationState.Withdrawn));
    }

    [Test]
    public async Task Rescan_NeverTouchesAnAuthorLockedRow()
    {
        var opened = await svc.OpenAsync(bookId, ObligationKind.Question, "Who is the girl behind the curtain?", ObligationActor.AuthorMcp,
            CurtainBeatId, "gap where the curtain didn't quite meet the frame");
        Assume.That(opened.Ok, Is.True, opened.Error);

        const string edited = "Not the woman. Nobody else was in the apartment. He pocketed the credstick and took the stairs down.";
        llm.Enqueue("""{"reasoning":"nothing new","opened":[],"touched":[]}""");
        var r = await svc.ScanBeatAsync(bookId, CurtainBeatId, edited, ObligationActor.SystemRescan);

        Assert.That(r.Withdrawn, Is.EqualTo(0));
        await using var db = dbFactory.CreateDbContext();
        var row = await db.NarrativeObligations.SingleAsync();
        Assert.That(row.State, Is.EqualTo(ObligationState.Open));
        Assert.That(row.AuthorLocked, Is.True);
    }

    [Test]
    public async Task Rescan_ReopensAClosure_WhoseClosingQuoteVanished()
    {
        llm.Enqueue(OpenedJson("question", "why is the child there", "Nothing in the room said why a child was here at all", null, null, "book"));
        await svc.ScanBeatAsync(bookId, DuctBeatId, DuctBeat, ObligationActor.SystemExtract);
        var payoffBeat = beatIds[6];
        const string payoffText = "The jaw-rig man had brought his daughter to the game because there was nowhere else to leave her.";
        await SetBeatTextAsync(payoffBeat, payoffText);
        llm.Enqueue("""{"reasoning":"pays it","opened":[],"touched":[{"reasoning":"explains","index":1,"quote":"nowhere else to leave her","verdict":"closed"}]}""");
        await svc.ScanBeatAsync(bookId, payoffBeat, payoffText, ObligationActor.SystemExtract);

        // The author later cuts the explanation.
        const string cut = "The jaw-rig man said nothing about the girl.";
        await SetBeatTextAsync(payoffBeat, cut);
        llm.Enqueue("""{"reasoning":"nothing","opened":[],"touched":[]}""");
        var r = await svc.ScanBeatAsync(bookId, payoffBeat, cut, ObligationActor.SystemRescan);

        Assert.That(r.Reopened, Is.EqualTo(1));
        await using var db = dbFactory.CreateDbContext();
        var row = await db.NarrativeObligations.SingleAsync();
        Assert.That(row.State, Is.EqualTo(ObligationState.Open));
        Assert.That(row.ClosingBeatId, Is.Null);
    }

    // ── author decisions ──────────────────────────────────────────────────────

    [Test]
    public async Task Close_RefusedWhenQuoteNotInBeat_AcceptedWhenItIs()
    {
        var opened = await svc.OpenAsync(bookId, ObligationKind.Promise, "the girl will return", ObligationActor.AuthorMcp);
        var payoffBeat = beatIds[8];
        const string text = "The girl from the curtain stood at the counter, older by a year, and asked for him by name.";
        await SetBeatTextAsync(payoffBeat, text);

        var refused = await svc.CloseAsync(opened.Row!.Id, payoffBeat, "she never came back", null, ObligationActor.AuthorMcp);
        Assert.That(refused.Ok, Is.False);
        Assert.That(refused.Error, Does.StartWith("quote_not_found"));

        var accepted = await svc.CloseAsync(opened.Row.Id, payoffBeat, "asked for him by name", "paid in Ch5", ObligationActor.AuthorMcp);
        Assert.That(accepted.Ok, Is.True, accepted.Error);
        Assert.That(accepted.Row!.State, Is.EqualTo(ObligationState.Closed));
        Assert.That(accepted.Row.ClosingBeatId, Is.EqualTo(payoffBeat));
    }

    [Test]
    public async Task Defer_RemovesTheRowFromOverdue_AndDropRequiresAReason()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "girl behind the curtain", "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character"));
        await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);
        var id = (await svc.ListAsync(bookId)).Single().Id;

        Assume.That((await svc.TrialBalanceAsync(bookId, null)).OverdueWithoutDecision, Has.Count.EqualTo(1));

        var badDrop = await svc.DropAsync(id, "because", "meh", ObligationActor.AuthorMcp);
        Assert.That(badDrop.Ok, Is.False);

        var deferred = await svc.DeferAsync(id, "She is Mrs. Chen's daughter; pays off in book two.", ObligationDueKind.BookEnd, null, ObligationActor.AuthorMcp);
        Assert.That(deferred.Ok, Is.True, deferred.Error);

        var tb = await svc.TrialBalanceAsync(bookId, null);
        Assert.That(tb.OverdueWithoutDecision, Is.Empty);
        Assert.That(tb.Deferred, Is.EqualTo(1));
        Assert.That(tb.Balanced, Is.True);

        var history = await svc.HistoryAsync(id);
        Assert.That(history.Select(h => h.Action), Is.EqualTo(new[] { ObligationEventAction.Open, ObligationEventAction.Defer }));
        Assert.That(history[1].Actor, Is.EqualTo(ObligationActor.AuthorMcp));
    }

    [Test]
    public async Task EmptyLedger_TrialBalance_SaysCouldNotLook_AndIsNotBalanced()
    {
        await using (var db = dbFactory.CreateDbContext())
            await db.Beats.ExecuteUpdateAsync(s => s.SetProperty(b => b.ObligationScanHash, (string?)null));

        var tb = await svc.TrialBalanceAsync(bookId, null);
        Assert.That(tb.CouldNotLook, Is.True);
        Assert.That(tb.Balanced, Is.False, "an empty ledger fails, it does not pass");
    }

    [Test]
    public async Task ClearBeatReferences_WithdrawsOpenUnadvanced_KeepsAdvancedAndLocked()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "girl behind the curtain", "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character"));
        await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);
        var locked = await svc.OpenAsync(bookId, ObligationKind.Promise, "authored promise", ObligationActor.AuthorMcp, CurtainBeatId, "watching him the way children watch");

        await using var db = dbFactory.CreateDbContext();
        await NarrativeObligationService.ClearBeatReferencesAsync(db, [CurtainBeatId], CancellationToken.None);

        var rows = await db.NarrativeObligations.ToListAsync();
        Assert.That(rows.Single(r => r.Id == locked.Row!.Id).State, Is.EqualTo(ObligationState.Open));
        Assert.That(rows.Single(r => r.Id == locked.Row!.Id).OriginBeatId, Is.Null, "anchor cleared, row kept");
        Assert.That(rows.Single(r => r.Id != locked.Row!.Id).State, Is.EqualTo(ObligationState.Withdrawn));
    }

    [Test]
    public async Task BriefBlock_ListsOverdueFirst_UnderDueNow()
    {
        llm.Enqueue(OpenedJson("introduced-referent", "girl behind the curtain", "gap where the curtain didn't quite meet the frame", "girl behind the curtain", "character"));
        await svc.ScanBeatAsync(bookId, CurtainBeatId, CurtainBeat, ObligationActor.SystemExtract);
        await svc.OpenAsync(bookId, ObligationKind.Foreshadow, "the sword's maker will be named", ObligationActor.AuthorMcp, trigger: "when the togishi returns");

        var block = await svc.BuildBriefBlockAsync(bookId, beatIds[8], 8, 10);

        Assert.That(block, Does.Contain("[OPEN OBLIGATIONS"));
        Assert.That(block, Does.Contain("DUE NOW"));
        Assert.That(block.IndexOf("girl behind the curtain", StringComparison.Ordinal), Is.LessThan(block.IndexOf("sword's maker", StringComparison.Ordinal)));
        Assert.That(block, Does.Contain("when: when the togishi returns"));
        Assert.That(block.Length, Is.LessThanOrEqualTo(NarrativeObligationService.BriefBlockMaxChars));
    }

    // ── the extractor contract, without an LLM ─────────────────────────────────

    [Test]
    public void Parse_DropsOutOfRangeTouchedIndex_AndUnknownKinds()
    {
        const string beat = "The togishi set the blade down on the cloth and said nothing for eleven minutes.";
        var raw = """
            {"reasoning":"r","opened":[{"reasoning":"x","quote":"set the blade down on the cloth","kind":"mystery-box","description":"bad kind"}],
             "touched":[{"reasoning":"y","index":7,"quote":"said nothing for eleven minutes","verdict":"closed"},
                        {"reasoning":"z","index":1,"quote":"said nothing for eleven minutes","verdict":"advanced"}]}
            """;
        var result = NarrativeObligationExtractor.Parse(raw, beat, openCount: 2);

        Assert.That(result.Evaluated, Is.True);
        Assert.That(result.Opened, Is.Empty);
        Assert.That(result.Touched, Has.Count.EqualTo(1));
        Assert.That(result.Touched[0].Index, Is.EqualTo(1));
        Assert.That(result.DiscardedUngrounded, Is.EqualTo(2));
    }

    [Test]
    public void Parse_GarbageResponse_IsNotEvaluated()
    {
        var result = NarrativeObligationExtractor.Parse("Sorry, I cannot help with that.", "some beat text", 0);
        Assert.That(result.Evaluated, Is.False);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task SetBeatTextAsync(Guid beatId, string text)
    {
        await using var db = dbFactory.CreateDbContext();
        var b = await db.Beats.SingleAsync(x => x.Id == beatId);
        b.Text = text; b.TextHash = Beat.ComputeHash(text);
        await db.SaveChangesAsync();
    }

    /// <summary>Queue of canned responses; a null entry throws like a provider outage.</summary>
    private sealed class ScriptedLlm : ILlmService
    {
        private readonly Queue<string?> responses = new();
        public int Calls;
        public void Enqueue(string? response) => responses.Enqueue(response);
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Calls++;
            if (responses.Count == 0) return Task.FromResult("""{"reasoning":"nothing","opened":[],"touched":[]}""");
            var next = responses.Dequeue();
            if (next == null) throw new InvalidOperationException("400 Bad Request: Your credit balance is too low to access the Anthropic API.");
            return Task.FromResult(next);
        }
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}

/// <summary>
/// <c>ClassifyOutcome</c> is pure and static so the four distinguishable things that can happen to
/// a beat are testable without a database or an LLM. The gap that survived five paid calibration
/// runs survived because nothing could exercise the scorer without spending $3; this must never be
/// in that position.
/// </summary>
[TestFixture]
public class ObligationScanOutcomeTests
{
    private static NarrativeObligationExtractor.Result R(bool evaluated, int windowsRead, int windowsTotal, int discarded) =>
        new([], [], discarded, "", evaluated) { WindowsRead = windowsRead, WindowsTotal = windowsTotal };

    [Test]
    public void EveryWindowRead_AndSomethingOpened_IsARead()
    {
        Assert.That(NarrativeObligationService.ClassifyOutcome(R(true, 2, 2, 0), opened: 3),
            Is.EqualTo(ObligationScanOutcome.Read));
    }

    [Test]
    public void ReadAndGenuinelyEmpty_IsARead_NotAFailure()
    {
        Assert.That(NarrativeObligationService.ClassifyOutcome(R(true, 1, 1, 0), opened: 0),
            Is.EqualTo(ObligationScanOutcome.Read),
            "a beat that owes nothing is a real, successful read");
    }

    [Test]
    public void ReadButEveryItemFailedTheQuoteGate_IsItsOwnOutcome()
    {
        // The case with no representation before 2026-09-16, and the leading explanation for the
        // GCTOC beat-3 blind spot: the beat carrying RECALLED TO LIFE was read (evaluated = true)
        // and opened nothing at all. Dickens' em-dashes, archaic spelling and nested quotation are
        // exactly what breaks verbatim quote fidelity, and GCSH run 1 already logged the class.
        // In the database this was indistinguishable from a beat that owed nothing.
        Assert.That(NarrativeObligationService.ClassifyOutcome(R(true, 1, 1, discarded: 6), opened: 0),
            Is.EqualTo(ObligationScanOutcome.ReadAllDiscarded));
    }

    [Test]
    public void SomeWindowsRead_IsPartial_AndCountsAsCouldNotLook()
    {
        var outcome = NarrativeObligationService.ClassifyOutcome(R(false, 2, 3, 0), opened: 1);
        Assert.That(outcome, Is.EqualTo(ObligationScanOutcome.Partial));
        Assert.That(ObligationScanOutcome.CouldNotLook(outcome), Is.True);
    }

    [Test]
    public void NoWindowRead_IsUnread_AndCountsAsCouldNotLook()
    {
        var outcome = NarrativeObligationService.ClassifyOutcome(R(false, 0, 3, 0), opened: 0);
        Assert.That(outcome, Is.EqualTo(ObligationScanOutcome.Unread));
        Assert.That(ObligationScanOutcome.CouldNotLook(outcome), Is.True);
    }

    [Test]
    public void ARead_IsNeverCouldNotLook()
    {
        Assert.That(ObligationScanOutcome.CouldNotLook(ObligationScanOutcome.Read), Is.False);
        Assert.That(ObligationScanOutcome.CouldNotLook(ObligationScanOutcome.ReadAllDiscarded), Is.False,
            "the read really happened — the defect is quote fidelity, not coverage");
    }
}

[TestFixture]
public class UnnamedReferentScannerTests
{
    [Test]
    public void Finds_TheGirlBehindTheCurtain()
    {
        const string text = "The girl behind the curtain had not made a sound. The girl behind the curtain watched him. She was maybe thirteen.";
        var hits = UnnamedReferentScanner.Scan(text);
        Assert.That(hits.Select(h => h.Label), Does.Contain("girl behind the curtain"));
        Assert.That(hits.Single(h => h.Label == "girl behind the curtain").Mentions, Is.EqualTo(2));
    }

    [Test]
    public void SkipsBareCrowdNouns_ButKeepsModifiedOnes()
    {
        const string text = "The man swung first. The man in the tan coat did not move. A woman screamed.";
        var labels = UnnamedReferentScanner.Scan(text).Select(h => h.Label).ToList();
        Assert.That(labels, Does.Not.Contain("man"));
        Assert.That(labels, Does.Not.Contain("woman"));
        Assert.That(labels, Does.Contain("man in the tan coat"));
    }

    [Test]
    public void SkipsCapitalisedHeads_TheyAreNames()
    {
        const string text = "He asked the Antiquarian who subscribed to the file. The Antiquarian smiled.";
        Assert.That(UnnamedReferentScanner.Scan(text), Is.Empty);
    }

    [Test]
    public void SuppressesKnownAliases()
    {
        const string text = "The noodle lady set the bowl down. The noodle lady said nothing.";
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "noodle lady" };
        // "lady" is not a head noun in the pattern anyway; use a head that IS, and alias it.
        const string text2 = "The old woman set the bowl down. The old woman said nothing.";
        var known2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "old woman" };
        Assert.That(UnnamedReferentScanner.Scan(text, known), Is.Empty);
        Assert.That(UnnamedReferentScanner.Scan(text2, known2), Is.Empty);
        Assert.That(UnnamedReferentScanner.Scan(text2), Is.Not.Empty);
    }
}
