using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// The same-predicate group adjudicator — the half of contradiction detection that produced
/// candidates for months and never produced a judgement.
///
/// <para><b>What these tests are actually protecting.</b> This service is the only thing in the
/// Story Ledger that CLEARS a contradiction, so every failure mode here is silent by nature: a
/// bug does not throw, it quietly declares a real defect compatible and moves it out of the
/// gate's view. So the cases below are weighted toward refusal — an LLM call that failed, a
/// verdict that could not be parsed, a quote that is not in the prose, and a group with no prose
/// at all must every one of them leave the claims exactly as they were. "We could not ask" must
/// never be recorded as "we asked and it was fine".</para>
/// </summary>
[TestFixture]
public class ClaimGroupAdjudicationTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FindingsService findings = null!;
    private ContinuityService store = null!;

    private const string BeatOneProse =
        "The count came back forty-three contracts across eleven years, and he did not argue with it.";
    private const string BeatTwoProse =
        "A hundred and forty contracts, she said, in the same eleven years. He let the number sit.";

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-ledgeradj-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "ledgeradj");
        findings = new FindingsService(dbFactory, paths);
        store = new ContinuityService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private ClaimGroupAdjudicationService Build(ILlmService llm)
    {
        var workbench = new NodeWorkbenchService(
            dbFactory, null!, paths, null!, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        return new ClaimGroupAdjudicationService(
            dbFactory, store, workbench, findings, llm,
            NullLogger<ClaimGroupAdjudicationService>.Instance);
    }

    /// <summary>A book with two beats stating incompatible contract counts, and one ledger group
    /// holding both values anchored to those beats.</summary>
    private async Task<Guid> SeedAsync(bool anchored = true)
    {
        var book = new BookNode
        {
            Id = Guid.CreateVersion7(), Slug = "adj-book", NodeCode = "ADJ",
            Title = "Adjudication Book", Kind = "book", UniverseId = Universe.GlmzId,
        };
        var chapter = new ChapterNode
        {
            Id = Guid.CreateVersion7(), Slug = "adj-book-ch1", Title = "Chapter 1 — The Count",
            Kind = "chapter", ParentNodeId = book.Id, UniverseId = Universe.GlmzId,
        };
        var beatOne = Guid.CreateVersion7();
        var beatTwo = Guid.CreateVersion7();

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Nodes.AddRange(book, chapter);
        void AddBeat(Guid id, int number, string text, double sort)
        {
            db.Beats.Add(new Beat { Id = id, Number = number, Text = text, TextHash = Beat.ComputeHash(text) });
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = id, SortKey = sort });
        }
        AddBeat(beatOne, 1, BeatOneProse, 1);
        AddBeat(beatTwo, 2, BeatTwoProse, 2);

        void AddClaim(string uid, string obj, Guid? beat) => db.ContinuityClaims.Add(new ContinuityClaim
        {
            ClaimUid = uid, EntityId = "e-kyle", EntityName = "Kyle Ellen Corbin", EntityKind = "character",
            Predicate = "contract_count", Object = obj, Status = "CONTRADICTED",
            SourceType = "prose", BookSlug = book.Slug, SourceBeatId = beat,
            SourceChapterId = chapter.Id.ToString("D"),
        });
        AddClaim("claim-a", "forty-three contracts in eleven years", anchored ? beatOne : null);
        AddClaim("claim-b", "a hundred and forty contracts in eleven years", anchored ? beatTwo : null);

        await db.SaveChangesAsync();
        return book.Id;
    }

    private static async Task<string[]> StatusesAsync(IDbContextFactory<ProseDbContext> f)
    {
        await using var db = await f.CreateDbContextAsync();
        return await db.ContinuityClaims.OrderBy(c => c.ClaimUid).Select(c => c.Status).ToArrayAsync();
    }

    // ── the happy paths ──────────────────────────────────────────────────────

    [Test]
    public async Task A_Grounded_Conflict_Is_Reported_And_The_Claims_Keep_Their_Status()
    {
        var bookId = await SeedAsync();
        var svc = Build(new FixedLlm(
            """{"contradiction": true, "severity": "MODERATE", "quote": "A hundred and forty contracts, she said, in the same eleven years", "note": "43 against 140 over one span."}"""));

        var report = await svc.RunAsync(bookId);

        Assert.That(report.Conflicts, Is.EqualTo(1));
        Assert.That(report.ClaimsCleared, Is.Zero);
        Assert.That(report.Conflicting[0].Predicate, Is.EqualTo("contract_count"));
        Assert.That(await StatusesAsync(dbFactory), Is.All.EqualTo("CONTRADICTED"));
    }

    [Test]
    public async Task A_Compatible_Verdict_Clears_The_Group_Back_To_New()
    {
        var bookId = await SeedAsync();
        var svc = Build(new FixedLlm(
            """{"contradiction": false, "severity": "MINOR", "quote": "", "note": "Different moments; the story shows the change."}"""));

        var report = await svc.RunAsync(bookId);

        Assert.That(report.Compatible, Is.EqualTo(1));
        Assert.That(report.ClaimsCleared, Is.EqualTo(2));
        // NEW, never REJECTED: the claims were not judged wrong, only the verdict about them.
        Assert.That(await StatusesAsync(dbFactory), Is.All.EqualTo("NEW"));
    }

    // ── the refusals: every one of these must leave the ledger untouched ──────

    [Test]
    public async Task An_Ungrounded_Conflict_Verdict_Is_Discarded()
    {
        // The single most important behaviour in the file. An unquotable assertion about the text
        // is exactly how the fabricated father became canon; the instrument built to catch that
        // must be incapable of committing it.
        var bookId = await SeedAsync();
        var svc = Build(new FixedLlm(
            """{"contradiction": true, "severity": "BLOCKER", "quote": "he had signed ninety contracts that winter", "note": "invented"}"""));

        var report = await svc.RunAsync(bookId);

        Assert.That(report.Conflicts, Is.Zero, "an ungrounded verdict must not become a finding");
        Assert.That(report.GroundingRejected, Is.EqualTo(1));
        Assert.That(report.ClaimsCleared, Is.Zero, "a discarded verdict is not evidence of compatibility");
        Assert.That(await StatusesAsync(dbFactory), Is.All.EqualTo("CONTRADICTED"));
    }

    [Test]
    public async Task A_Failed_Call_Never_Reads_As_Compatible()
    {
        var bookId = await SeedAsync();
        var svc = Build(new ThrowingLlm());

        var report = await svc.RunAsync(bookId);

        Assert.That(report.GroundingRejected, Is.EqualTo(1));
        Assert.That(report.ClaimsCleared, Is.Zero, "a provider outage must never clear a contradiction");
        Assert.That(await StatusesAsync(dbFactory), Is.All.EqualTo("CONTRADICTED"));
    }

    [Test]
    public async Task An_Unparseable_Response_Never_Reads_As_Compatible()
    {
        var bookId = await SeedAsync();
        var svc = Build(new FixedLlm("I'm afraid I can't help with that."));

        var report = await svc.RunAsync(bookId);

        Assert.That(report.ClaimsCleared, Is.Zero);
        Assert.That(await StatusesAsync(dbFactory), Is.All.EqualTo("CONTRADICTED"));
    }

    [Test]
    public async Task A_Group_With_No_Anchored_Claim_Is_Skipped_Without_Calling_The_Model()
    {
        // Adjudicating with no prose means ruling on two summaries — the paraphrase-only
        // reasoning this whole system exists to stop. It must refuse, and must not pay to do so.
        var bookId = await SeedAsync(anchored: false);
        var llm = new CountingLlm("""{"contradiction": true, "severity": "BLOCKER", "quote": "x", "note": "y"}""");
        var svc = Build(llm);

        var report = await svc.RunAsync(bookId);

        Assert.That(llm.Calls, Is.Zero, "an unanchored group must not be billed for");
        Assert.That(report.Unanchored, Is.EqualTo(1));
        Assert.That(report.Notes.Any(n => n.Contains("anchor")), Is.True, "the skip must be reported, not silent");
        Assert.That(await StatusesAsync(dbFactory), Is.All.EqualTo("CONTRADICTED"));
    }

    // ── cost control ─────────────────────────────────────────────────────────

    [Test]
    public async Task A_Second_Run_On_Unchanged_Prose_Costs_Nothing()
    {
        var bookId = await SeedAsync();
        var llm = new CountingLlm(
            """{"contradiction": false, "severity": "MINOR", "quote": "", "note": "compatible"}""");
        var svc = Build(llm);

        await svc.RunAsync(bookId);
        var callsAfterFirst = llm.Calls;
        var second = await svc.RunAsync(bookId);

        Assert.That(callsAfterFirst, Is.EqualTo(1));
        Assert.That(llm.Calls, Is.EqualTo(1), "the verdict cache must make a re-run free");
        Assert.That(second.CacheHits, Is.EqualTo(1));
    }

    [Test]
    public async Task Editing_The_Anchor_Beat_Re_Adjudicates()
    {
        // The verdict is a judgement about specific prose. If that prose changes the verdict is
        // stale even though both claim rows are byte-identical — a claim's object can survive a
        // rewrite that inverts its meaning.
        var bookId = await SeedAsync();
        var llm = new CountingLlm(
            """{"contradiction": false, "severity": "MINOR", "quote": "", "note": "compatible"}""");
        var svc = Build(llm);
        await svc.RunAsync(bookId);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.OrderBy(b => b.Number).FirstAsync();
            beat.Text = "The count came back forty-one contracts across eleven years.";
            beat.TextHash = Beat.ComputeHash(beat.Text);
            // Re-contradict so the group is live again for the second pass.
            foreach (var c in await db.ContinuityClaims.ToListAsync()) c.Status = "CONTRADICTED";
            await db.SaveChangesAsync();
        }

        await svc.RunAsync(bookId);
        Assert.That(llm.Calls, Is.EqualTo(2), "a prose edit must invalidate the cached verdict");
    }

    // ── doubles ──────────────────────────────────────────────────────────────

    private sealed class FixedLlm(string response) : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            Task.FromResult(response);
    }

    private sealed class CountingLlm(string response) : ILlmService
    {
        public int Calls { get; private set; }
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingLlm : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("Circuit breaker open for provider 'claude-api'.");
    }
}
