using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// End-to-end cover for the Tuned Read pipeline: candidate → adjudicate → GROUND → file.
///
/// <para><b>Why this file has to exist.</b> The plan's live acceptance test ("restore BCODA beats
/// #543/#5206 to their pre-fix text, run the tuned read, require a BLOCKER finding citing both
/// the father claim and the construct reveal ~290 beats apart") cannot run against the corpus as
/// it now stands: Phase 0 purged those claims, so a real BCODA run correctly yields ZERO
/// candidates. Reproducing it live would mean writing fabricated lore back into a published book
/// and paying for extraction plus adjudication. So the defect is reconstructed here in miniature
/// — the same claim shapes, the same beat distance, real prose, a stubbed adjudicator — which
/// proves every part of the machinery except the live model's judgement.</para>
///
/// <para>The grounding test is the important one. A verdict whose quote is not in the prose must
/// be DISCARDED, because an unquotable assertion about the text is exactly how "Dae-jung Seo"
/// became canon in the first place.</para>
/// </summary>
[TestFixture]
public class TunedReadServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FindingsService findings = null!;
    private ContinuityService store = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-tunedread-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "tunedread");
        findings = new FindingsService(dbFactory, paths);
        store = new ContinuityService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    // ── the reconstructed defect ─────────────────────────────────────────────

    private const string FatherProse =
        "Mrs. Chen set the tea down without looking at him. Your father's name was Dae-jung Seo, " +
        "she said. He was a craftsman. He made swords.";

    private const string ConstructProse =
        "Nine died before him. He was the tenth configuration, and there was no before — " +
        "no childhood, no city, no father. Only the room where they wrote him.";

    private TunedReadService Build(ILlmService llm)
    {
        var exclusions = new PredicateExclusionService(dbFactory, NullLogger<PredicateExclusionService>.Instance);
        var synopsis = new SynopsisExportService(dbFactory, llm, null!, NullLogger<SynopsisExportService>.Instance);
        var workbench = new NodeWorkbenchService(
            dbFactory, null!, paths, null!, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);

        // extraction is null!: RunAsync only dereferences it when ReExtract is true, and every
        // test here passes ReExtract:false so the ledger is exactly what the test seeded.
        return new TunedReadService(
            dbFactory, store, null!, exclusions, synopsis, workbench, findings, llm,
            NullLogger<TunedReadService>.Instance);
    }

    /// <summary>
    /// A book with the two contradicting beats far apart, the axiom that pairs them, and two
    /// ledger claims anchored to those beats. Filler beats sit between them so the anchors are
    /// genuinely outside each other's carrier window — the distance is the whole point, since a
    /// contradiction inside one window is one the clamped logic sweep would already have caught.
    /// </summary>
    private async Task<(Guid BookId, Guid FatherBeatId, Guid ConstructBeatId)> SeedDefectAsync()
    {
        var book = new BookNode
        {
            Id = Guid.CreateVersion7(), Slug = "test-coda", NodeCode = "TCODA",
            Title = "Test Coda", Kind = "book", UniverseId = Universe.GlmzId,
        };
        var chapter = new ChapterNode
        {
            Id = Guid.CreateVersion7(), Slug = "test-coda-ch1", Title = "Chapter 1 — Teeth",
            Kind = "chapter", ParentNodeId = book.Id, UniverseId = Universe.GlmzId,
        };

        var fatherBeatId = Guid.CreateVersion7();
        var constructBeatId = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Nodes.AddRange(book, chapter);

            void AddBeat(Guid id, int number, string text, double sort)
            {
                db.Beats.Add(new Beat { Id = id, Number = number, Text = text, TextHash = Beat.ComputeHash(text) });
                db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = id, SortKey = sort });
            }

            AddBeat(fatherBeatId, 1, FatherProse, 1.0);
            for (var i = 0; i < 30; i++)
                AddBeat(Guid.CreateVersion7(), 100 + i, $"Filler beat {i}. The city went on being the city.", 10.0 + i);
            AddBeat(constructBeatId, 500, ConstructProse, 500.0);

            db.PredicateExclusions.Add(new PredicateExclusion
            {
                UniverseId = Guid.Empty,
                PredicateA = "origin|nature|true_nature",
                ObjectPatternA = "constructed|construct|no prior life|no before|configuration",
                PredicateB = "father",
                ObjectPatternB = null,
                Symmetric = true, Source = "builtin", Status = "active",
                Rationale = "A constructed being has no biological father.",
            });

            await db.SaveChangesAsync();
        }

        store.Upsert(new ContinuityClaim
        {
            EntityId = "kyle", EntityName = "Kyle Ellen Corbin", EntityKind = "character",
            Predicate = "father", Object = "Dae-jung Seo, a craftsman who made swords",
            SourceType = "prose", BookSlug = "test-coda", Snippet = "Your father's name was Dae-jung Seo",
            Provenance = ClaimProvenance.Observed, SourceBeatId = fatherBeatId,
        });
        store.Upsert(new ContinuityClaim
        {
            EntityId = "kyle", EntityName = "Kyle Ellen Corbin", EntityKind = "character",
            Predicate = "origin", Object = "the tenth configuration; there was no before",
            SourceType = "prose", BookSlug = "test-coda", Snippet = "He was the tenth configuration",
            Provenance = ClaimProvenance.Observed, SourceBeatId = constructBeatId,
        });

        return (book.Id, fatherBeatId, constructBeatId);
    }

    private static TunedReadService.TunedReadOptions NoExtract =>
        new(ReExtract: false, Adjudicate: true, MaxCandidates: 60);

    // ── the acceptance shape ─────────────────────────────────────────────────

    [Test]
    public async Task PairsTheContradictionAcrossBeats_AndFilesAGroundedFinding()
    {
        var (bookId, _, _) = await SeedDefectAsync();

        // A quote copied verbatim out of ConstructProse — this is what a well-behaved
        // adjudicator returns, and what the grounding gate is meant to accept.
        var llm = new FixedLlm("""
            {"contradiction": true, "severity": "BLOCKER",
             "quote": "there was no before — no childhood, no city, no father",
             "note": "A named father cannot coexist with an origin that asserts no prior life."}
            """);

        var report = await Build(llm).RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(report.CandidatesFromOntology, Is.EqualTo(1), "the axiom must pair the two claims");
            Assert.That(report.Adjudicated, Is.EqualTo(1));
            Assert.That(report.Confirmed, Is.EqualTo(1));
            Assert.That(report.GroundingRejected, Is.Zero);
        });

        var f = report.Findings.Single();
        Assert.Multiple(() =>
        {
            Assert.That(f.Severity, Is.EqualTo("BLOCKER"));
            Assert.That(f.EntityName, Is.EqualTo("Kyle Ellen Corbin"));
            // Both beats are named, ~499 apart — the cross-range citation the sharded sweep and
            // the windowed comprehension probe structurally cannot produce.
            Assert.That(new[] { f.PredicateA, f.PredicateB }, Is.EquivalentTo(new[] { "father", "origin" }));
            Assert.That(f.BeatNumberA, Is.Not.Null);
            Assert.That(f.BeatNumberB, Is.Not.Null);
            Assert.That(f.BeatNumberA, Is.Not.EqualTo(f.BeatNumberB));
        });
    }

    [Test]
    public async Task FiledFindingCarriesNoSnippet_SoNoApplyPathCanRewriteProse()
    {
        // docs/LOGIC.md §4 (audits never write) and memory
        // feedback_no_bulk_fix_tools_hand_edit_prose_2026_08_31. Without a Snippet/SuggestedFix
        // pair there is nothing for an apply path to splice into a beat.
        var (bookId, _, _) = await SeedDefectAsync();
        var llm = new FixedLlm("""
            {"contradiction": true, "severity": "BLOCKER",
             "quote": "He was the tenth configuration",
             "note": "conflict"}
            """);

        await Build(llm).RunAsync(bookId, NoExtract);

        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.Findings.AsNoTracking()
            .Where(f => f.Summary.StartsWith("TUNEDREAD")).ToListAsync();

        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows.All(r => r.Snippet == null), Is.True,
            "a TUNEDREAD finding must never carry a Snippet");
    }

    // ── the grounding gate ───────────────────────────────────────────────────

    [Test]
    public async Task UngroundedVerdict_IsDiscardedNotFiled()
    {
        // The adjudicator asserts a contradiction but cites a quote that is nowhere in the prose
        // it was shown. This is precisely the failure that produced the original bad report, and
        // it must not survive.
        var (bookId, _, _) = await SeedDefectAsync();
        var llm = new FixedLlm("""
            {"contradiction": true, "severity": "BLOCKER",
             "quote": "His father forged the blade in Osaka before the war",
             "note": "invented evidence"}
            """);

        var report = await Build(llm).RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(report.CandidatesFromOntology, Is.EqualTo(1));
            Assert.That(report.Adjudicated, Is.EqualTo(1));
            Assert.That(report.GroundingRejected, Is.EqualTo(1));
            Assert.That(report.Confirmed, Is.Zero, "an ungrounded contradiction must never be confirmed");
            Assert.That(report.Findings, Is.Empty);
        });
    }

    [Test]
    public async Task ClearedVerdict_FilesNothing()
    {
        var (bookId, _, _) = await SeedDefectAsync();
        var llm = new FixedLlm("""
            {"contradiction": false, "note": "the construct reveal reframes the earlier claim rather than contradicting it"}
            """);

        var report = await Build(llm).RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(report.Cleared, Is.EqualTo(1));
            Assert.That(report.Confirmed, Is.Zero);
            Assert.That(report.Findings, Is.Empty);
        });
    }

    [Test]
    public async Task AdjudicatorOutage_IsNeverRecordedAsAPass()
    {
        // Same fail-closed rule the rest of the engine learned the hard way: a provider outage
        // must not read as "we checked and it was fine".
        var (bookId, _, _) = await SeedDefectAsync();
        var report = await Build(new ThrowingLlm()).RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(report.Confirmed, Is.Zero);
            Assert.That(report.Findings, Is.Empty);
            Assert.That(report.GroundingRejected, Is.EqualTo(1),
                "the verdict must be recorded with a reason, not as a clean pass");
        });

        await using var db = await dbFactory.CreateDbContextAsync();
        var cached = await db.TunedReadAdjudications.AsNoTracking().SingleAsync();
        Assert.That(cached.RejectedReason, Does.Contain("adjudication call failed"));
    }

    [Test]
    public async Task UnparseableVerdict_IsDiscarded()
    {
        var (bookId, _, _) = await SeedDefectAsync();
        var report = await Build(new FixedLlm("They definitely conflict, I'd say.")).RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(report.Confirmed, Is.Zero);
            Assert.That(report.Findings, Is.Empty);
        });
    }

    // ── cost behaviour ───────────────────────────────────────────────────────

    [Test]
    public async Task SecondRunOnUnchangedBook_SpendsNothing()
    {
        // Acceptance test 7 from the plan: re-run on an unchanged book → zero LLM calls, same
        // findings. Without this the instrument is unaffordable across a 46-book corpus.
        var (bookId, _, _) = await SeedDefectAsync();
        var llm = new CountingLlm("""
            {"contradiction": true, "severity": "MODERATE",
             "quote": "He was the tenth configuration",
             "note": "conflict"}
            """);
        var svc = Build(llm);

        var first = await svc.RunAsync(bookId, NoExtract);
        var second = await svc.RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(llm.Calls, Is.EqualTo(1), "the second run must be served entirely from the verdict cache");
            Assert.That(first.Adjudicated, Is.EqualTo(1));
            Assert.That(second.Adjudicated, Is.Zero);
            Assert.That(second.CacheHits, Is.EqualTo(1));
            Assert.That(second.Confirmed, Is.EqualTo(first.Confirmed), "same findings, no spend");
        });
    }

    [Test]
    public async Task EditingTheAnchorProse_ForcesReadjudication()
    {
        // The verdict is a judgement about specific prose. If that prose changes the cached
        // answer is stale, even though both claim rows are byte-identical.
        var (bookId, _, constructBeatId) = await SeedDefectAsync();
        var llm = new CountingLlm("""
            {"contradiction": true, "severity": "MODERATE",
             "quote": "He was the tenth configuration",
             "note": "conflict"}
            """);
        var svc = Build(llm);

        await svc.RunAsync(bookId, NoExtract);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var beat = await db.Beats.FirstAsync(b => b.Id == constructBeatId);
            beat.Text = "He was the tenth configuration, and he remembered his father's hands.";
            beat.TextHash = Beat.ComputeHash(beat.Text);
            await db.SaveChangesAsync();
        }

        var second = await svc.RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(llm.Calls, Is.EqualTo(2), "changed anchor prose must invalidate the cached verdict");
            Assert.That(second.CacheHits, Is.Zero);
        });
    }

    [Test]
    public async Task DryRun_GeneratesCandidatesButSpendsNothing()
    {
        var (bookId, _, _) = await SeedDefectAsync();
        var llm = new CountingLlm("""{"contradiction": true, "quote": "x", "note": "y"}""");

        var report = await Build(llm).RunAsync(bookId,
            new TunedReadService.TunedReadOptions(ReExtract: false, Adjudicate: false));

        Assert.Multiple(() =>
        {
            Assert.That(report.CandidatesFromOntology, Is.EqualTo(1), "the free half still runs");
            Assert.That(llm.Calls, Is.Zero);
            Assert.That(report.Findings, Is.Empty);
        });
    }

    [Test]
    public async Task ClaimsWithNoBeatAnchor_AreRefusedRatherThanJudgedOnParaphrase()
    {
        // Adjudicating with no prose would be ruling on two one-line summaries — exactly the
        // paraphrase-only reasoning that produced the original fabricated report. Refuse instead.
        var (bookId, _, _) = await SeedDefectAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await db.ContinuityClaims.ExecuteUpdateAsync(s => s.SetProperty(c => c.SourceBeatId, (Guid?)null));
        }

        var llm = new CountingLlm("""{"contradiction": true, "quote": "x", "note": "y"}""");
        var report = await Build(llm).RunAsync(bookId, NoExtract);

        Assert.Multiple(() =>
        {
            Assert.That(report.CandidatesFromOntology, Is.EqualTo(1));
            Assert.That(llm.Calls, Is.Zero, "no anchor prose means no call is made at all");
            Assert.That(report.Confirmed, Is.Zero);
            Assert.That(report.GroundingRejected, Is.EqualTo(1));
        });
    }

    // ── ledger side effects ──────────────────────────────────────────────────

    [Test]
    public async Task ConfirmedContradiction_MarksBothClaimsAndRecordsTheAxiom()
    {
        var (bookId, _, _) = await SeedDefectAsync();
        var llm = new FixedLlm("""
            {"contradiction": true, "severity": "BLOCKER",
             "quote": "He was the tenth configuration",
             "note": "conflict"}
            """);

        await Build(llm).RunAsync(bookId, NoExtract);

        await using var db = await dbFactory.CreateDbContextAsync();
        var claims = await db.ContinuityClaims.AsNoTracking()
            .Where(c => c.EntityId == "kyle").ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(claims.All(c => c.Status == "CONTRADICTED"), Is.True,
                "the ledger itself must reflect the contradiction, not only the Findings inbox");
            Assert.That(claims.All(c => c.ExclusionRuleId != null), Is.True,
                "the reason must be traceable to the axiom that fired");
        });
    }

    [Test]
    public async Task CanonicalClaimIsNeverDemoted()
    {
        // Same rule ContinuityService.Upsert follows: a fact the author already settled stays
        // settled until a human re-resolves it.
        var (bookId, _, _) = await SeedDefectAsync();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var origin = await db.ContinuityClaims.FirstAsync(c => c.Predicate == "origin");
            origin.Status = "CANONICAL";
            await db.SaveChangesAsync();
        }

        var llm = new FixedLlm("""
            {"contradiction": true, "severity": "BLOCKER",
             "quote": "He was the tenth configuration",
             "note": "conflict"}
            """);
        await Build(llm).RunAsync(bookId, NoExtract);

        await using var check = await dbFactory.CreateDbContextAsync();
        var claims = await check.ContinuityClaims.AsNoTracking().Where(c => c.EntityId == "kyle").ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(claims.Single(c => c.Predicate == "origin").Status, Is.EqualTo("CANONICAL"));
            Assert.That(claims.Single(c => c.Predicate == "father").Status, Is.EqualTo("CONTRADICTED"));
        });
    }

    [Test]
    public async Task FixedContradiction_LosesItsStaleFinding()
    {
        // Delete-then-recreate at book scope: Upsert alone never removes a row whose triggering
        // condition has stopped holding.
        var (bookId, _, _) = await SeedDefectAsync();
        var confirming = new FixedLlm("""
            {"contradiction": true, "severity": "BLOCKER",
             "quote": "He was the tenth configuration", "note": "conflict"}
            """);
        await Build(confirming).RunAsync(bookId, NoExtract);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            Assert.That(await db.Findings.CountAsync(f => f.Summary.StartsWith("TUNEDREAD")), Is.EqualTo(1));
            // The author fixes it by rejecting the fabricated claim.
            var father = await db.ContinuityClaims.FirstAsync(c => c.Predicate == "father");
            father.Status = "REJECTED";
            await db.SaveChangesAsync();
        }

        var after = await Build(confirming).RunAsync(bookId, NoExtract);

        await using var check = await dbFactory.CreateDbContextAsync();
        Assert.Multiple(() =>
        {
            Assert.That(after.CandidatesFromOntology, Is.Zero);
            Assert.That(check.Findings.Count(f => f.Summary.StartsWith("TUNEDREAD")), Is.Zero,
                "the stale finding must be cleared, not left behind forever");
        });
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
