using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// <see cref="ReviewCostEstimator"/> is the table an operator reads before confirming a review
/// spend, and the shared pricing source for <see cref="TokenLedger"/>. These tests pin its
/// arithmetic per model (with prompt caching), the Haiku fallback for unknown models, runtime
/// pricing registration, the ceiling-to-the-penny rounding, and the fixed row width of both
/// rendered tables.
/// </summary>
[TestFixture]
public class ReviewCostEstimatorTests
{
    private const double Tol = 1e-9;

    /// <summary>2 indent + 32 label + 1 gap + 10 tokens + 2 gap + 8 cost.</summary>
    private const int RowWidth = 55;

    [Test]
    public void Estimate_SonnetBallotOnly_PricesCacheWriteReadSystemAndOutput()
    {
        var e = ReviewCostEstimator.Estimate("Story", beatCount: 100, storyTokens: 100_000,
            voterCount: 5, model: "claude-sonnet-4-6", ballotOnly: true);

        Assert.Multiple(() =>
        {
            Assert.That(e.ModelLabel, Is.EqualTo("Sonnet 4.6"));
            Assert.That(e.OutputTokensPerVoter, Is.EqualTo(1_500));          // 900 + 100*6
            Assert.That(e.CacheWriteCost, Is.EqualTo(0.1 * 3.75).Within(Tol)); // written once
            Assert.That(e.CacheReadCost, Is.EqualTo(0.1 * 0.30 * 4).Within(Tol)); // read by the other 4
            Assert.That(e.SystemCost, Is.EqualTo(0.0015 * 3.00 * 5).Within(Tol));
            Assert.That(e.OutputCost, Is.EqualTo(0.0015 * 15.00 * 5).Within(Tol));
            Assert.That(e.TotalCost,
                Is.EqualTo(e.CacheWriteCost + e.CacheReadCost + e.SystemCost + e.OutputCost).Within(Tol));
            Assert.That(e.TotalCost, Is.EqualTo(0.63).Within(Tol));
        });
    }

    [Test]
    public void Estimate_BallotOutputBudget_IsCappedAt8000_AndFullReviewUses1400()
    {
        var capped = ReviewCostEstimator.Estimate("S", beatCount: 2_000, storyTokens: 1_000,
            voterCount: 1, model: "claude-haiku-4-5-20251001", ballotOnly: true);
        var full = ReviewCostEstimator.Estimate("S", beatCount: 2_000, storyTokens: 1_000,
            voterCount: 1, model: "claude-haiku-4-5-20251001", ballotOnly: false);

        Assert.That(capped.OutputTokensPerVoter, Is.EqualTo(8_000));
        Assert.That(full.OutputTokensPerVoter, Is.EqualTo(1_400));
    }

    [Test]
    public void Estimate_OpusSingleVoter_HasNoCacheReads()
    {
        var e = ReviewCostEstimator.Estimate("S", beatCount: 10, storyTokens: 200_000,
            voterCount: 1, model: "claude-opus-4-8", ballotOnly: false);

        Assert.Multiple(() =>
        {
            Assert.That(e.CacheReadCost, Is.EqualTo(0));
            Assert.That(e.CacheWriteCost, Is.EqualTo(0.2 * 18.75).Within(Tol));
            Assert.That(e.SystemCost, Is.EqualTo(0.0015 * 15.00).Within(Tol));
            Assert.That(e.OutputCost, Is.EqualTo(0.0014 * 75.00).Within(Tol));
        });
    }

    [Test]
    public void EstimateActual_UsesTheActualOutputTokens()
    {
        var e = ReviewCostEstimator.EstimateActual("S", beatCount: 50, storyTokens: 100_000,
            votersFired: 4, model: "claude-haiku-4-5-20251001", ballotOnly: true, outputTokensActual: 10_000);

        Assert.Multiple(() =>
        {
            Assert.That(e.VoterCount, Is.EqualTo(4));
            Assert.That(e.OutputTokensPerVoter, Is.EqualTo(2_500));
            Assert.That(e.CacheWriteCost, Is.EqualTo(0.1 * 1.25).Within(Tol));
            Assert.That(e.CacheReadCost, Is.EqualTo(0.1 * 0.10 * 3).Within(Tol));
            Assert.That(e.SystemCost, Is.EqualTo(0.0015 * 1.00 * 4).Within(Tol));
            Assert.That(e.OutputCost, Is.EqualTo(0.01 * 5.00).Within(Tol)); // not multiplied by voters
            Assert.That(e.TotalCost,
                Is.EqualTo(e.CacheWriteCost + e.CacheReadCost + e.SystemCost + e.OutputCost).Within(Tol));
        });
    }

    [Test]
    public void EstimateActual_NoVotersFired_HasZeroPerVoterOutputAndNoReads()
    {
        var e = ReviewCostEstimator.EstimateActual("S", beatCount: 5, storyTokens: 10_000,
            votersFired: 0, model: "claude-sonnet-5", ballotOnly: true, outputTokensActual: 0);

        Assert.Multiple(() =>
        {
            Assert.That(e.OutputTokensPerVoter, Is.EqualTo(0));
            Assert.That(e.CacheReadCost, Is.EqualTo(0));
            Assert.That(e.SystemCost, Is.EqualTo(0));
        });
    }

    [Test]
    public void UnknownModel_FallsBackToHaikuRates_AndSaysSo()
    {
        const string model = "no-such-model-rce-test";
        var unknown = ReviewCostEstimator.Estimate("S", 100, 100_000, 3, model, ballotOnly: true);
        var haiku = ReviewCostEstimator.Estimate("S", 100, 100_000, 3, "claude-haiku-4-5-20251001", ballotOnly: true);

        Assert.Multiple(() =>
        {
            Assert.That(ReviewCostEstimator.IsKnown(model), Is.False);
            Assert.That(ReviewCostEstimator.IsKnown("claude-haiku-4-5-20251001"), Is.True);
            Assert.That(unknown.TotalCost, Is.EqualTo(haiku.TotalCost).Within(Tol));
            Assert.That(unknown.ModelLabel, Does.Contain(model).And.Contain("unknown"));
            var rates = ReviewCostEstimator.GetRatesFor(model);
            Assert.That(rates.InputPerMtok, Is.EqualTo(1.00));
            Assert.That(rates.OutputPerMtok, Is.EqualTo(5.00));
        });
    }

    [Test]
    public void RegisterPricing_AddsAModel_AndZeroCacheRatesFallBackToInput()
    {
        const string model = "rce-test-registered-model";
        ReviewCostEstimator.RegisterPricing(model, "Test Model", 2.00, 0, 0, 8.00);

        Assert.That(ReviewCostEstimator.IsKnown(model), Is.True);
        var rates = ReviewCostEstimator.GetRatesFor(model);
        Assert.That(rates, Is.EqualTo(("Test Model", 2.00, 8.00)));

        // Cache write and read default to the input rate when given as 0.
        var e = ReviewCostEstimator.Estimate("S", 10, 1_000_000, 2, model, ballotOnly: false);
        Assert.That(e.CacheWriteCost, Is.EqualTo(2.00).Within(Tol));
        Assert.That(e.CacheReadCost, Is.EqualTo(2.00).Within(Tol));
    }

    [Test]
    public void RegisterPricing_OverridesAnExistingRegistration()
    {
        const string model = "rce-test-overridden-model";
        ReviewCostEstimator.RegisterPricing(model, "First", 1.00, 1.00, 1.00, 1.00);
        ReviewCostEstimator.RegisterPricing(model, "Second", 4.00, 5.00, 0.40, 20.00);

        Assert.That(ReviewCostEstimator.GetRatesFor(model), Is.EqualTo(("Second", 4.00, 20.00)));
        var e = ReviewCostEstimator.Estimate("S", 10, 1_000_000, 2, model, ballotOnly: false);
        Assert.That(e.CacheWriteCost, Is.EqualTo(5.00).Within(Tol));
        Assert.That(e.CacheReadCost, Is.EqualTo(0.40).Within(Tol));
    }

    [Test]
    public void Costs_CeilingRoundToThePenny()
    {
        var e = new ReviewCostEstimator.CostEstimate("S", 1, 1, 1, "m", "M", true, 1,
            CacheWriteCost: 0.009, CacheReadCost: 0, SystemCost: 0.0001, OutputCost: 1.231, TotalCost: 0.009);

        var lines = ReviewCostEstimator.RenderTable(e).Split('\n');

        Assert.That(lines.Single(l => l.Contains("cache write")), Does.EndWith("$0.01"));
        Assert.That(lines.Single(l => l.Contains("cache read")), Does.EndWith("$0.00"));
        Assert.That(lines.Single(l => l.Contains("persona")), Does.EndWith("$0.01"));
        Assert.That(lines.Single(l => l.Contains("Output")), Does.EndWith("$1.24"));
        Assert.That(lines.Single(l => l.TrimStart().StartsWith("TOTAL")), Does.EndWith("$0.01"));
    }

    [Test]
    public void RenderTable_EveryContentRowIsExactlyRowWidth()
    {
        var e = ReviewCostEstimator.Estimate("A Long Story Title", beatCount: 1_234, storyTokens: 987_654,
            voterCount: 12, model: "claude-opus-4-8", ballotOnly: true);

        AssertContentRowsHaveRowWidth(ReviewCostEstimator.RenderTable(e));
    }

    [Test]
    public void RenderActualTable_EveryContentRowIsExactlyRowWidth()
    {
        var e = ReviewCostEstimator.EstimateActual("A Long Story Title", beatCount: 1_234, storyTokens: 987_654,
            votersFired: 12, model: "claude-sonnet-5", ballotOnly: false, outputTokensActual: 54_321);

        AssertContentRowsHaveRowWidth(ReviewCostEstimator.RenderActualTable(e, 54_321));
    }

    /// <summary>Content rows = the column header, the four component rows and the TOTAL row:
    /// every line after the four-line banner that is not a separator rule.</summary>
    private static void AssertContentRowsHaveRowWidth(string table)
    {
        var rows = table.Split('\n')
            .Skip(5)
            .Where(l => l.Length > 0 && !l.All(c => c is '─' or '━'))
            .ToList();

        Assert.That(rows, Has.Count.EqualTo(6), table);
        Assert.Multiple(() =>
        {
            foreach (var row in rows)
                Assert.That(row.Length, Is.EqualTo(RowWidth), $"row: [{row}]");
        });
    }
}
