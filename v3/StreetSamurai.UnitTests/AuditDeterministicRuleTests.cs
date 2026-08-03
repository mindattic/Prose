using StreetSamurai.Core.Services.Audit;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Regression cover for the three deterministic audit rules added after the TRNY post-mortem
/// (2026-08-02). Each one exists because a real defect shipped in a book marked "publication
/// ready" that had already passed three logic sweeps and a craft audit, so each test is written
/// against the actual prose/keys that got through.
/// </summary>
[TestFixture]
public class AuditDeterministicRuleTests
{
    static AuditBeat Beat(int n, double sortKey, string text = "text") =>
        new(Guid.Parse($"00000000-0000-0000-0000-{n:D12}"), n, text, sortKey);

    static AuditContext Ctx(IReadOnlyList<AuditBeat> beats) =>
        new(Guid.NewGuid(), Guid.NewGuid(), string.Join("\n\n", beats.Select(b => b.Text)), beats,
            new Dictionary<string, object?>());

    // ── InsertedBeatDriftRule.FindInserted ───────────────────────────────────────

    [Test]
    public void FindInserted_BinarySubdivisionTrail_FlagsOnlyTheInsertedBeats()
    {
        // TRNY Chapter 4's real key layout: an even 50-step grid with a later-inserted run
        // wedged in by repeated midpointing. 75/87.5/93.75/96.875 were where three verified
        // continuity defects were hiding.
        var beats = new[]
        {
            Beat(1, 50), Beat(2, 75), Beat(3, 87.5), Beat(4, 93.75),
            Beat(5, 96.875), Beat(6, 100), Beat(7, 150), Beat(8, 200), Beat(9, 250),
        };

        var inserted = LogicSweepService.InsertedBeatDriftRule.FindInserted(beats).Select(b => b.Number).ToList();

        Assert.That(inserted, Does.Contain(3));
        Assert.That(inserted, Does.Contain(4));
        Assert.That(inserted, Does.Contain(5));
        Assert.That(inserted, Does.Not.Contain(1), "the first beat of the grid is not an insertion");
        Assert.That(inserted, Does.Not.Contain(7), "an on-grid beat is not an insertion");
        Assert.That(inserted, Does.Not.Contain(9), "the last beat of the grid is not an insertion");
    }

    [Test]
    public void FindInserted_EvenGrid_FlagsNothing()
    {
        var beats = new[] { Beat(1, 50), Beat(2, 100), Beat(3, 150), Beat(4, 200), Beat(5, 250) };
        Assert.That(LogicSweepService.InsertedBeatDriftRule.FindInserted(beats), Is.Empty);
    }

    [Test]
    public void FindInserted_FractionalButEvenlySpacedGrid_FlagsNothing()
    {
        // Guards the reason spacing is used instead of "SortKey has a fractional part": a node
        // whose whole grid is fractional is not a node full of insertions.
        var beats = new[] { Beat(1, 0.5), Beat(2, 1.0), Beat(3, 1.5), Beat(4, 2.0), Beat(5, 2.5) };
        Assert.That(LogicSweepService.InsertedBeatDriftRule.FindInserted(beats), Is.Empty);
    }

    [Test]
    public void FindInserted_TooFewBeats_FlagsNothing()
    {
        var beats = new[] { Beat(1, 50), Beat(2, 75), Beat(3, 100) };
        Assert.That(LogicSweepService.InsertedBeatDriftRule.FindInserted(beats), Is.Empty);
    }

    [Test]
    public void FindAnchors_ReturnsTheFixedBeatsBracketingEachInsertedRun()
    {
        // TRNY Ch4's real layout again: beats 2-5 are the inserted run, 1 and 6 bracket it.
        var beats = new[]
        {
            Beat(1, 50), Beat(2, 75), Beat(3, 87.5), Beat(4, 93.75),
            Beat(5, 96.875), Beat(6, 100), Beat(7, 150), Beat(8, 200), Beat(9, 250),
        };
        var inserted = LogicSweepService.InsertedBeatDriftRule.FindInserted(beats);
        var anchors = LogicSweepService.InsertedBeatDriftRule.FindAnchors(beats, inserted).Select(b => b.Number).ToList();

        Assert.That(inserted, Is.Not.Empty);
        Assert.That(anchors, Does.Contain(1), "the beat before the run is the state the run must not contradict");
        Assert.That(anchors, Does.Contain(6), "the beat after the run is too");
        Assert.That(anchors, Does.Not.Contain(8), "a beat nowhere near the run is not an anchor");
    }

    [Test]
    public void BuildPrompt_NoInsertedBeats_ShortCircuitsToEmptyArray()
    {
        var ctx = Ctx([Beat(1, 50), Beat(2, 100), Beat(3, 150), Beat(4, 200)]);
        var (_, user) = new LogicSweepService.InsertedBeatDriftRule().BuildPrompt(ctx);
        Assert.That(user, Does.Contain("[]"), "a node with no insertions must not ship the whole book to the model");
    }

    [Test]
    public void BuildPrompt_WithInsertedBeats_LabelsInsertedAndAnchorBeats()
    {
        // The exact defect shape from TRNY Ch4: the theft is on-grid at 50, the contradicting
        // "three coins" claims are on inserted keys, and 100 restates empty pockets.
        var ctx = Ctx([
            Beat(1, 50, "He had no coin left."),
            Beat(2, 75, "The lane bent away."),
            Beat(3, 87.5, "Three coins in his pocket."),
            Beat(4, 93.75, "He counted three coins again."),
            Beat(5, 100, "Empty pockets, still."),
            Beat(6, 150, "Later that night."),
            Beat(7, 200, "Later still."),
        ]);
        var (system, user) = new LogicSweepService.InsertedBeatDriftRule().BuildPrompt(ctx);

        Assert.That(user, Does.Contain("INSERTED LATER"));
        Assert.That(user, Does.Contain("pre-existing anchor"));
        Assert.That(user, Does.Contain("Three coins in his pocket."));
        Assert.That(user, Does.Contain("He had no coin left."), "the contradicting anchor must be in the window");
        Assert.That(system, Does.Contain("RETROSPECTIVE REFERENCES POINTING FORWARD"));
    }

    // ── InteriorityDensityRule ──────────────────────────────────────────────────

    [Test]
    public void CountItalics_CountsSingleAsteriskSpansAndIgnoresBold()
    {
        Assert.That(CraftRuleAuditService.InteriorityDensityRule.CountItalics("*One.* plain *Two.*"), Is.EqualTo(2));
        Assert.That(CraftRuleAuditService.InteriorityDensityRule.CountItalics("**bold** not italics"), Is.EqualTo(0));
        Assert.That(CraftRuleAuditService.InteriorityDensityRule.CountItalics("no markers at all"), Is.EqualTo(0));
    }

    [Test]
    public async Task InteriorityDensity_AtTrnyShippedRate_FiresModerate()
    {
        // TRNY shipped at 3.27 italic segments per beat. Four per beat here.
        var beats = Enumerable.Range(1, 5)
            .Select(i => Beat(i, i * 50, "*a.* body *b.* body *c.* body *d.*"))
            .ToList();

        var verdicts = await new CraftRuleAuditService.InteriorityDensityRule().EvaluateAsync(Ctx(beats), default);

        Assert.That(verdicts.Any(v => v.Severity == "MODERATE"), Is.True);
        Assert.That(verdicts.First().Evidence, Does.Contain("20 italic"));
        Assert.That(verdicts.Any(v => v.Severity == "MINOR"), Is.True, "a beat with 3+ is cited individually");
    }

    [Test]
    public async Task InteriorityDensity_AtCorpusNormalRate_Passes()
    {
        // Every other book measured 0.02–0.76 per beat. One italic across five beats = 0.2.
        var beats = new[]
        {
            Beat(1, 50, "*One flat line.*"), Beat(2, 100, "plain"), Beat(3, 150, "plain"),
            Beat(4, 200, "plain"), Beat(5, 250, "plain"),
        };
        var verdicts = await new CraftRuleAuditService.InteriorityDensityRule().EvaluateAsync(Ctx(beats), default);
        Assert.That(verdicts, Is.Empty);
    }

    // ── RetiredTicRule ──────────────────────────────────────────────────────────

    [TestCase("something behind his eyes does the arithmetic Deil handed him")]
    [TestCase("he does the arithmetic without wanting to")]
    [TestCase("I never let myself do the math")]
    [TestCase("Thurl noted that too, filed it in the same column")]
    [TestCase("checking the ledger in his head without writing anything down")]
    public void FindTics_RetiredCognitiveFraming_IsFlagged(string prose)
        => Assert.That(CraftRuleAuditService.RetiredTicRule.FindTics(prose), Is.Not.Empty);

    [TestCase("Enough that old Ferrin did the sum twice and didn't like it")]
    [TestCase("The toll-master has a ledger chained to his belt")]
    [TestCase("Just a boy doing sums for coin, the same as anyone")]
    [TestCase("He counts it twice. The second count doesn't fix the first.")]
    public void FindTics_LiteralDiegeticBookkeeping_IsNotFlagged(string prose)
        => Assert.That(CraftRuleAuditService.RetiredTicRule.FindTics(prose), Is.Empty,
            "money-work is this corpus's actual plot; only mind-as-ledger metaphor is retired");

    [Test]
    public async Task RetiredTic_CitesTheBeatAndQuotesSurroundingText()
    {
        var beats = new[] { Beat(1, 50, "He read it. Does the arithmetic he already knew the answer to, twice over.") };
        var verdicts = await new CraftRuleAuditService.RetiredTicRule().EvaluateAsync(Ctx(beats), default);

        Assert.That(verdicts, Has.Count.EqualTo(1));
        Assert.That(verdicts[0].Location, Is.EqualTo(beats[0].Id.ToString()));
        Assert.That(verdicts[0].Evidence, Does.Contain("arithmetic"));
        Assert.That(verdicts[0].Severity, Is.EqualTo("MINOR"), "this rule points, it does not convict");
    }
}
