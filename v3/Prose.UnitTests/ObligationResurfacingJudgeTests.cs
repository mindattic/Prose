using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The resurfacing judge and the scan-time open-list on LONG material, pinned after GCSH
/// calibration run 2 (2026-09-15): with the judge running, all four injected payoffs — each the
/// last sentence of a ~1,100-word beat — came back "not recognised", because v1 cut every
/// candidate at 600 words; and at scan time none of the four plants was ever among the 40 most
/// urgent open rows on a 257-outstanding book, so the extractor could never be shown the debt
/// the payoff pays. Neither may recur.
/// </summary>
[TestFixture]
public class ObligationResurfacingJudgeTests
{
    private const string Payoff = "He put the brass whistle to his lips at last; it was the signal the coachman had been waiting for since morning.";

    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private Guid bookId, chapterId;
    private readonly List<Guid> beatIds = new();

    private static string LongCandidate(int fillerWords)
    {
        var sb = new System.Text.StringBuilder();
        var i = 0;
        while (sb.Length == 0 || sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < fillerWords)
            sb.Append($"Filler sentence {i++} about the fog on Baker Street and the cab that would not come. ");
        sb.Append(Payoff);
        return sb.ToString();
    }

    [SetUp]
    public async Task SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        await using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        bookId = Guid.CreateVersion7(); chapterId = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode { Id = bookId, Slug = "judge-book", NodeCode = "JDG", Title = "Judge Book", Kind = "book", UniverseId = Universe.GlmzId, Status = "Complete - publication ready" });
        db.Nodes.Add(new ChapterNode { Id = chapterId, Slug = "judge-ch1", Title = "Chapter 1", Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId, SortKey = 100 });
        beatIds.Clear();
        var texts = new[]
        {
            "On the mantel lay a brass whistle nobody in the room would admit to owning.",
            "Watson read the paper and said nothing of consequence for a long while.",
            LongCandidate(1500),
        };
        for (var i = 0; i < texts.Length; i++)
        {
            var id = Guid.CreateVersion7();
            beatIds.Add(id);
            db.Beats.Add(new Beat { Id = id, Number = i + 1, Text = texts[i], TextHash = Beat.ComputeHash(texts[i]), ObligationScanHash = "seeded" });
            db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = id, SortKey = (i + 1) * 100 });
        }
        db.NarrativeObligations.Add(new NarrativeObligation
        {
            NodeId = bookId, Kind = ObligationKind.Plant, Description = "the brass whistle on the mantel nobody claims",
            Provenance = Prose.Core.Services.ClaimProvenance.Observed, OriginBeatId = beatIds[0],
            OriginQuote = "brass whistle nobody in the room would admit to owning", DueByKind = ObligationDueKind.BookEnd,
            State = ObligationState.Open, DedupKey = NarrativeObligationService.DedupKey(bookId, ObligationKind.Plant, "the brass whistle on the mantel nobody claims"),
        });
        await db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() { conn.Close(); conn.Dispose(); }

    // ── passages ──────────────────────────────────────────────────────────────────

    [Test]
    public void SplitPassages_KeepsEveryWord_NoPartOverMax()
    {
        var text = LongCandidate(1500);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parts = ObligationResurfacingJudge.SplitPassages(text, ObligationResurfacingJudge.MaxCandidateWords);

        Assert.That(parts.Count, Is.EqualTo((words.Length + 599) / 600));
        Assert.That(parts.All(p => p.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 600), Is.True);
        Assert.That(string.Join(' ', parts), Is.EqualTo(string.Join(' ', words)), "nothing dropped, nothing duplicated");
        Assert.That(parts[^1], Does.EndWith(Payoff));
        Assert.That(ObligationResurfacingJudge.SplitPassages("short one", 600), Is.EqualTo(new[] { "short one" }));
    }

    [Test]
    public async Task Judge_PayoffInTheTailOfALongBeat_ClosesTheObligation_OneCacheRowPerBeat()
    {
        var llm = new ScriptedLlm();
        var finder = new FixedFinder([beatIds[2]]);
        var judge = new ObligationResurfacingJudge(dbFactory, llm, finder, NullLogger<ObligationResurfacingJudge>.Instance);

        // The payoff is in the LAST part; the model names that part's number.
        var parts = ObligationResurfacingJudge.SplitPassages(LongCandidate(1500), ObligationResurfacingJudge.MaxCandidateWords).Count;
        Assume.That(parts, Is.GreaterThanOrEqualTo(3));
        llm.Enqueue($$"""{"reasoning":"the whistle is blown","verdicts":[{"beat_number":1,"relation":"not_addressed","quote":null},{"beat_number":{{parts}},"relation":"closes","quote":"{{Payoff}}"}]}""");

        var r = await judge.RunAsync(bookId);

        Assert.That(r.Evaluated, Is.True);
        Assert.That(r.LlmCalls, Is.EqualTo(1));
        Assert.That(r.Closed, Is.EqualTo(1));
        Assert.That(r.DiscardedUngrounded, Is.EqualTo(0));
        Assert.That(llm.LastUser, Does.Contain($"part {parts} of {parts}"));
        Assert.That(llm.LastUser, Does.Contain(Payoff), "the tail reached the prompt");

        await using var db = dbFactory.CreateDbContext();
        var row = await db.NarrativeObligations.SingleAsync();
        Assert.That(row.State, Is.EqualTo(ObligationState.Closed));
        Assert.That(row.ClosingBeatId, Is.EqualTo(beatIds[2]));
        Assert.That(row.ClosingQuote!.Length, Is.LessThanOrEqualTo(QuoteGrounding.MaxStoredQuoteLength));
        Assert.That(QuoteGrounding.Contains(LongCandidate(1500), row.ClosingQuote, QuoteGrounding.MinObligationQuoteLength), Is.True);

        var cache = await db.ObligationJudgeCache.ToListAsync();
        Assert.That(cache, Has.Count.EqualTo(1), "parts of one beat merge into one cache row");
        Assert.That(cache[0].Relation, Is.EqualTo("closes"));
        Assert.That(cache[0].PromptVersion, Is.EqualTo(ObligationResurfacingJudge.PromptVersion));
    }

    [Test]
    public async Task Judge_QuoteNotInTheBeat_IsDiscarded_AndTheBeatCountsAsNotAddressed()
    {
        var llm = new ScriptedLlm();
        var judge = new ObligationResurfacingJudge(dbFactory, llm, new FixedFinder([beatIds[2]]), NullLogger<ObligationResurfacingJudge>.Instance);
        llm.Enqueue("""{"reasoning":"r","verdicts":[{"beat_number":1,"relation":"closes","quote":"a sentence the passage never contained at all"}]}""");

        var r = await judge.RunAsync(bookId);

        Assert.That(r.Closed, Is.EqualTo(0));
        Assert.That(r.DiscardedUngrounded, Is.EqualTo(1));
        await using var db = dbFactory.CreateDbContext();
        Assert.That((await db.NarrativeObligations.SingleAsync()).State, Is.EqualTo(ObligationState.Open));
        Assert.That((await db.ObligationJudgeCache.SingleAsync()).Relation, Is.EqualTo("ungrounded"));
    }

    /// <summary>Pinned after GCSH calibration run 3 (2026-09-16): the judge closed a seeded "stopped
    /// clock" plant using a real-but-unrelated sentence about a cigar and a tree — grounded (it is
    /// literal text from that beat) but never actually about the debt. A "closes"/"advances" verdict
    /// must now also share a content word with the obligation it claims to resolve, or it is vetoed
    /// the same as an ungrounded quote. Must not recur.</summary>
    [Test]
    public async Task Judge_ClosesWithAGroundedButUnrelatedQuote_IsVetoed_ObligationStaysOpen()
    {
        var llm = new ScriptedLlm();
        // beatIds[1] is real text, and the quote below is a literal substring of it (passes
        // grounding) but shares no content word with the brass-whistle obligation.
        var unrelatedButReal = "Watson read the paper and said nothing of consequence for a long while.";
        Assert.That(unrelatedButReal, Is.EqualTo(await GetBeatTextAsync(beatIds[1])), "fixture assumption");
        var judge = new ObligationResurfacingJudge(dbFactory, llm, new FixedFinder([beatIds[1]]), NullLogger<ObligationResurfacingJudge>.Instance);
        llm.Enqueue($$"""{"reasoning":"r","verdicts":[{"beat_number":1,"relation":"closes","quote":"{{unrelatedButReal}}"}]}""");

        var r = await judge.RunAsync(bookId);

        Assert.That(r.Closed, Is.EqualTo(0), "a grounded-but-irrelevant quote must not close the debt");
        Assert.That(r.DiscardedUngrounded, Is.EqualTo(1));
        await using var db = dbFactory.CreateDbContext();
        Assert.That((await db.NarrativeObligations.SingleAsync()).State, Is.EqualTo(ObligationState.Open));
        Assert.That((await db.ObligationJudgeCache.SingleAsync()).Relation, Is.EqualTo("irrelevant"));
    }

    private async Task<string> GetBeatTextAsync(Guid beatId)
    {
        await using var db = dbFactory.CreateDbContext();
        return (await db.Beats.SingleAsync(b => b.Id == beatId)).Text!;
    }

    // ── scan-time listing ─────────────────────────────────────────────────────────

    /// <summary>Locality tier, pinned after GCSH run 5 (2026-09-16, the first uncontaminated run):
    /// chapter 1 closed 82% of what it opened and every later chapter 15–59%, across twelve
    /// structurally identical self-contained stories. The starved rows were the ones the current
    /// chapter had just opened — not urgent by any measure, and not always lexically obvious.</summary>
    [Test]
    public void SelectForListing_ShowsADebtThisChapterOpened_EvenWhenOlderUrgentRowsWouldFillEverySlot()
    {
        var clock = MultiChapterClock(chapters: 10, beatsPerChapter: 20);
        var open = new List<NarrativeObligation>();
        // 60 past-due questions from the early chapters — enough to fill the list on urgency alone.
        var earlyBeats = clock.Beats.Where(kv => kv.Value.Chapter <= 3).OrderBy(kv => kv.Value.Position).Select(kv => kv.Key).ToList();
        for (var i = 0; i < 60; i++)
            open.Add(new NarrativeObligation { Kind = ObligationKind.Question, Description = $"question number {i} about the harbour and the ledger", DueByKind = ObligationDueKind.Chapter, DueByValue = 1, OriginBeatId = earlyBeats[i], State = ObligationState.Open });

        // A plant this chapter just opened: not past due, and the beat text does not echo it.
        var localBeat = clock.Beats.First(kv => kv.Value.Chapter == 9).Key;
        var tinSoldier = new NarrativeObligation { Kind = ObligationKind.Plant, Description = "the tin soldier left on the third step, bayonet pointing at the door", OriginQuote = "Someone had left a single tin soldier on the third step", DueByKind = ObligationDueKind.BookEnd, TriggerCondition = "when the letters are found", OriginBeatId = localBeat, State = ObligationState.Open };
        open.Add(tinSoldier);

        var unrelatedBeat = "Holmes lit his pipe and said nothing about anything for an hour.";
        var listed = NarrativeObligationService.SelectForListing(open, clock, atChapter: 9, atPosition: 170, unrelatedBeat);

        Assert.That(listed, Has.Count.EqualTo(NarrativeObligationExtractor.MaxOpenListed));
        Assert.That(listed, Does.Contain(tinSoldier), "a debt THIS chapter opened must be shown even with no urgency and no lexical echo");
        Assert.That(listed.Distinct().Count(), Is.EqualTo(listed.Count));
    }

    /// <summary>The run-2 guarantee, preserved: a debt from an EARLIER chapter that this beat's
    /// text actually echoes still reaches the model, via the lexical tier.</summary>
    [Test]
    public void SelectForListing_StillReachesAnOlderDebtThisBeatMentions_AndDropsItWhenUnmentioned()
    {
        var clock = MultiChapterClock(chapters: 10, beatsPerChapter: 20);
        var open = new List<NarrativeObligation>();
        var earlyBeats = clock.Beats.Where(kv => kv.Value.Chapter <= 3).OrderBy(kv => kv.Value.Position).Select(kv => kv.Key).ToList();
        for (var i = 0; i < 60; i++)
            open.Add(new NarrativeObligation { Kind = ObligationKind.Question, Description = $"question number {i} about the harbour and the ledger", DueByKind = ObligationDueKind.Chapter, DueByValue = 1, OriginBeatId = earlyBeats[i], State = ObligationState.Open });

        var tinSoldier = new NarrativeObligation { Kind = ObligationKind.Plant, Description = "the tin soldier left on the third step, bayonet pointing at the door", OriginQuote = "Someone had left a single tin soldier on the third step", DueByKind = ObligationDueKind.BookEnd, TriggerCondition = "when the letters are found", OriginBeatId = clock.Beats.First(kv => kv.Value.Chapter == 2).Key, State = ObligationState.Open };
        open.Add(tinSoldier);

        var payoffBeat = "The tin soldier had been the child's, set on the step to mark the room where the letters were kept.";
        var listed = NarrativeObligationService.SelectForListing(open, clock, atChapter: 9, atPosition: 170, payoffBeat);
        Assert.That(listed, Does.Contain(tinSoldier), "an older debt this beat pays must still be shown");

        var unrelatedBeat = "Holmes lit his pipe and said nothing about anything for an hour.";
        var fallback = NarrativeObligationService.SelectForListing(open, clock, atChapter: 9, atPosition: 170, unrelatedBeat);
        Assert.That(fallback, Has.Count.EqualTo(NarrativeObligationExtractor.MaxOpenListed), "unused lexical slots fall back to urgency");
        Assert.That(fallback, Does.Not.Contain(tinSoldier), "an unmentioned older debt yields its slot to urgency");
    }

    [Test]
    public void SelectForListing_FewRows_ReturnsAllInUrgencyOrder()
    {
        var clock = OneChapterClock(beats: 10);
        var a = new NarrativeObligation { Kind = ObligationKind.Plant, Description = "late plant", DueByKind = ObligationDueKind.BookEnd, OriginBeatId = clock.Beats.Keys.ElementAt(1), State = ObligationState.Open };
        var b = new NarrativeObligation { Kind = ObligationKind.Question, Description = "urgent question", DueByKind = ObligationDueKind.Chapter, DueByValue = 1, OriginBeatId = clock.Beats.Keys.ElementAt(0), State = ObligationState.Open };
        var listed = NarrativeObligationService.SelectForListing([a, b], clock, 1, 9, "nothing relevant here");
        Assert.That(listed, Is.EqualTo(new[] { b, a }));
    }

    private static NarrativeObligationService.BookClock MultiChapterClock(int chapters, int beatsPerChapter)
    {
        var chapterIds = new List<Guid>();
        var titles = new Dictionary<Guid, string>();
        var dict = new Dictionary<Guid, (int Chapter, int Position)>();
        var pos = 0;
        for (var c = 1; c <= chapters; c++)
        {
            var ch = Guid.CreateVersion7();
            chapterIds.Add(ch);
            titles[ch] = $"Chapter {c}";
            for (var i = 0; i < beatsPerChapter; i++) dict[Guid.CreateVersion7()] = (c, pos++);
        }
        return new NarrativeObligationService.BookClock { BookNodeId = Guid.CreateVersion7(), ChapterIds = chapterIds, Beats = dict, ChapterTitles = titles };
    }

    private static NarrativeObligationService.BookClock OneChapterClock(int beats)
    {
        var ch = Guid.CreateVersion7();
        var dict = new Dictionary<Guid, (int Chapter, int Position)>();
        for (var i = 0; i < beats; i++) dict[Guid.CreateVersion7()] = (1, i);
        return new NarrativeObligationService.BookClock { BookNodeId = Guid.CreateVersion7(), ChapterIds = [ch], Beats = dict, ChapterTitles = new Dictionary<Guid, string> { [ch] = "Chapter 1" } };
    }

    // ── doubles ───────────────────────────────────────────────────────────────────

    private sealed class FixedFinder(IReadOnlyList<Guid> ids) : IObligationCandidateFinder
    {
        public Task<IReadOnlyList<Guid>> FindAsync(NarrativeObligation obligation, NarrativeObligationService.BookClock clock, int k, CancellationToken ct) => Task.FromResult(ids);
    }

    private sealed class ScriptedLlm : ILlmService
    {
        private readonly Queue<string?> responses = new();
        public string LastUser = "";
        public void Enqueue(string? response) => responses.Enqueue(response);
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            LastUser = user;
            if (responses.Count == 0) return Task.FromResult("""{"reasoning":"nothing","verdicts":[]}""");
            var next = responses.Dequeue();
            if (next == null) throw new InvalidOperationException("simulated provider failure");
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
