using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Story Ledger Phase 4 — the wiring tests.
///
/// <para>docs/ARCHITECTURE.md §6 names the recurring bug class this phase exists to prevent:
/// <i>a sanctioned mechanism gets built, but the code it was meant to replace is never rewired
/// onto it.</i> The Tuned Read shipped in Phase 2 as a standalone CLI flag; until Phase 4 nothing
/// in the engine consulted it. These tests pin the two joins that make it load-bearing — the
/// publish-readiness gate that decides a book is done, and the summary-prefix routing that
/// carries a finding back into the next beat's prompt.</para>
/// </summary>
[TestFixture]
public class StoryLedgerPhase4Tests
{
    // ── gate 2: the publish-readiness rule ──────────────────────────────────────

    [Test]
    public void StoryLedger_Gate_Passes_Only_When_Everything_Is_Clean()
    {
        var check = BookHealthService.StoryLedgerCheck(
            hasLedger: true, contradictedClaims: 0, samePredicateFindings: 0, tunedReadFindings: 0);

        Assert.That(check.Pass, Is.True);
        Assert.That(check.Detail, Is.EqualTo("clean"));
    }

    [Test]
    public void StoryLedger_Gate_Fails_A_Book_Whose_Ledger_Was_Never_Populated()
    {
        // The whole point of Phase 4. The old gate counted only "FACT-LEDGER [" findings and
        // deliberately excluded the "[not-extracted]" honest-gap marker — correct for a
        // CONTRADICTION count, catastrophic for a readiness gate: a book nobody ever extracted
        // read as clean. Not checked is not the same as checked clean.
        var check = BookHealthService.StoryLedgerCheck(
            hasLedger: false, contradictedClaims: 0, samePredicateFindings: 0, tunedReadFindings: 0);

        Assert.That(check.Pass, Is.False);
        Assert.That(check.Detail, Does.Contain("never been populated"));
    }

    [Test]
    public void StoryLedger_Gate_Fails_On_A_TunedRead_Finding_Alone()
    {
        // The Dae-jung Seo shape: a cross-predicate contradiction produces no same-predicate
        // finding at all, so under the old gate this book published clean.
        var check = BookHealthService.StoryLedgerCheck(
            hasLedger: true, contradictedClaims: 0, samePredicateFindings: 0, tunedReadFindings: 1);

        Assert.That(check.Pass, Is.False);
        Assert.That(check.Detail, Does.Contain("tuned-read"));
    }

    [Test]
    public void StoryLedger_Gate_Fails_On_A_Contradicted_Claim_Row_With_No_Open_Finding()
    {
        // Findings can be dismissed; the claim row is the ledger's own answer. docs/LOGIC.md §9
        // gate 2 is written about CONTRADICTED claims, so the claim side has to be able to fail
        // the gate on its own.
        var check = BookHealthService.StoryLedgerCheck(
            hasLedger: true, contradictedClaims: 3, samePredicateFindings: 0, tunedReadFindings: 0);

        Assert.That(check.Pass, Is.False);
        Assert.That(check.Detail, Does.Contain("3 claim row(s) still CONTRADICTED"));
    }

    [Test]
    public void StoryLedger_Gate_Reports_Every_Failing_Face_Not_Just_The_First()
    {
        var check = BookHealthService.StoryLedgerCheck(
            hasLedger: true, contradictedClaims: 2, samePredicateFindings: 1, tunedReadFindings: 4);

        Assert.That(check.Pass, Is.False);
        Assert.That(check.Detail, Does.Contain("2 claim row(s)"));
        Assert.That(check.Detail, Does.Contain("1 open same-predicate"));
        Assert.That(check.Detail, Does.Contain("4 open tuned-read"));
    }

    // ── findings loop-back: prefix routing ──────────────────────────────────────

    [Test]
    public void LogicSweep_Prefix_Excludes_Its_Own_Siblings()
    {
        // The trailing space is load-bearing. "LOGICSWEEP-BLAST" is the per-beat blast-radius
        // mini-sweep and "LOGICSWEEP-CONVERGENCE" is a finding about the sweep process rather
        // than about the prose — neither belongs in a beat's generation guidance.
        const string prefix = "LOGICSWEEP ";

        Assert.That("LOGICSWEEP [causality] Kyle acts on a fact he was never told."
            .StartsWith(prefix, StringComparison.Ordinal), Is.True);
        Assert.That("LOGICSWEEP-BLAST [timeline] anchor beat re-check."
            .StartsWith(prefix, StringComparison.Ordinal), Is.False);
        Assert.That("LOGICSWEEP-CONVERGENCE [not-converging]: 6 rounds run."
            .StartsWith(prefix, StringComparison.Ordinal), Is.False);
    }

    [Test]
    public void StripSummaryPrefix_Handles_A_Prefix_That_Carries_Its_Own_Trailing_Space()
    {
        // The old Replace(prefix + " ") form searched for two consecutive spaces here and left
        // the raw routing prefix sitting in the prompt bullet.
        Assert.That(ProseWriterRouter.StripSummaryPrefix(
                "LOGICSWEEP [causality] Kyle acts on a fact he was never told.", "LOGICSWEEP "),
            Is.EqualTo("[causality] Kyle acts on a fact he was never told."));

        Assert.That(ProseWriterRouter.StripSummaryPrefix(
                "TUNEDREAD [Kyle Ishikawa] father=\"a swordsmith\" vs origin=\"constructed\"",
                TunedReadService.SummaryPrefix),
            Is.EqualTo("[Kyle Ishikawa] father=\"a swordsmith\" vs origin=\"constructed\""));
    }

    [Test]
    public void StripSummaryPrefix_Still_Handles_A_Prefix_Without_One()
    {
        // Every pre-existing caller (EMOTIONAL-DEPTH, READABILITY, STORYSCOPE, COMPREHENSION,
        // CHECKLIST, GRIPE, LINT, CONTINUITY-VIOLATION) passes a bare word.
        Assert.That(ProseWriterRouter.StripSummaryPrefix(
                "STORYSCOPE [moral-gloss] the closing paragraph explains the theme.", "STORYSCOPE"),
            Is.EqualTo("[moral-gloss] the closing paragraph explains the theme."));
    }

    [Test]
    public void StripSummaryPrefix_Leaves_A_Non_Matching_Summary_Intact()
    {
        Assert.That(ProseWriterRouter.StripSummaryPrefix("FACT-LEDGER [Kyle.age]: 34 vs 36", "TUNEDREAD "),
            Is.EqualTo("FACT-LEDGER [Kyle.age]: 34 vs 36"));
        Assert.That(ProseWriterRouter.StripSummaryPrefix("", "TUNEDREAD "), Is.EqualTo(""));
    }

    [Test]
    public void TunedRead_And_FactLedger_Prefixes_Cannot_Shadow_Each_Other()
    {
        // Both file under FindingCategory.Contradiction into the same node: prefix is the only
        // thing separating the two detectors, in the gate and in the loop-back alike.
        Assert.That(TunedReadService.SummaryPrefix, Is.EqualTo("TUNEDREAD "));
        Assert.That(TunedReadService.SummaryPrefix.StartsWith("FACT-LEDGER ", StringComparison.Ordinal), Is.False);
        Assert.That("FACT-LEDGER [Kyle.age]: 34 vs 36"
            .StartsWith(TunedReadService.SummaryPrefix, StringComparison.Ordinal), Is.False);
    }
}
