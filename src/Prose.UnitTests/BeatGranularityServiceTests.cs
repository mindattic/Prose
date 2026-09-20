using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class BeatGranularityServiceTests
{
    // ── StdDev ────────────────────────────────────────────────────────────────

    [Test]
    public void StdDev_empty_list_returns_zero()
    {
        Assert.That(BeatGranularityService.StdDev([]), Is.EqualTo(0));
    }

    [Test]
    public void StdDev_single_value_returns_zero()
    {
        Assert.That(BeatGranularityService.StdDev([3.0]), Is.EqualTo(0));
    }

    [Test]
    public void StdDev_uniform_values_returns_zero()
    {
        Assert.That(BeatGranularityService.StdDev([4.0, 4.0, 4.0, 4.0]), Is.EqualTo(0));
    }

    [Test]
    public void StdDev_known_population_stddev()
    {
        // Population stddev of [2, 4, 4, 4, 5, 5, 7, 9] = 2.0
        double result = BeatGranularityService.StdDev([2, 4, 4, 4, 5, 5, 7, 9]);
        Assert.That(result, Is.EqualTo(2.0).Within(0.0001));
    }

    [Test]
    public void StdDev_two_values_symmetric()
    {
        // Population stddev of [3, 7] = 2.0
        double result = BeatGranularityService.StdDev([3.0, 7.0]);
        Assert.That(result, Is.EqualTo(2.0).Within(0.0001));
    }

    // ── Classify ─────────────────────────────────────────────────────────────

    [Test]
    public void Classify_below_min_is_merge()
    {
        Assert.That(BeatGranularityService.Classify(3_999), Is.EqualTo(BeatSizeLabel.Merge));
        Assert.That(BeatGranularityService.Classify(0),     Is.EqualTo(BeatSizeLabel.Merge));
        Assert.That(BeatGranularityService.Classify(1),     Is.EqualTo(BeatSizeLabel.Merge));
    }

    [Test]
    public void Classify_at_min_is_ok()
    {
        Assert.That(BeatGranularityService.Classify(4_000), Is.EqualTo(BeatSizeLabel.Ok));
    }

    [Test]
    public void Classify_within_range_is_ok()
    {
        Assert.That(BeatGranularityService.Classify(5_750), Is.EqualTo(BeatSizeLabel.Ok));
        Assert.That(BeatGranularityService.Classify(7_500), Is.EqualTo(BeatSizeLabel.Ok));
    }

    [Test]
    public void Classify_above_max_is_split()
    {
        Assert.That(BeatGranularityService.Classify(7_501),  Is.EqualTo(BeatSizeLabel.Split));
        Assert.That(BeatGranularityService.Classify(21_855), Is.EqualTo(BeatSizeLabel.Split));
    }

    // ── FStatistic ───────────────────────────────────────────────────────────

    [Test]
    public void FStatistic_zero_ballotSd_returns_zero()
    {
        Assert.That(BeatGranularityService.FStatistic(0.45, 0), Is.EqualTo(0));
    }

    [Test]
    public void FStatistic_equal_sds_returns_one()
    {
        double result = BeatGranularityService.FStatistic(0.5, 0.5);
        Assert.That(result, Is.EqualTo(1.0).Within(0.0001));
    }

    [Test]
    public void FStatistic_inter_beats_larger_returns_greater_than_one()
    {
        // interBeatSD=0.45, ballotSD=0.80 → F = 0.45²/0.80² = 0.2025/0.64 ≈ 0.316 (noise > signal per ballot)
        double result = BeatGranularityService.FStatistic(0.45, 0.80);
        Assert.That(result, Is.EqualTo(0.45 * 0.45 / (0.80 * 0.80)).Within(0.0001));
        Assert.That(result, Is.LessThan(1.0));
    }

    [Test]
    public void FStatistic_known_values()
    {
        // interBeatSD=0.48, ballotSD=0.75 → F = 0.2304/0.5625 ≈ 0.4096
        double result = BeatGranularityService.FStatistic(0.48, 0.75);
        Assert.That(result, Is.EqualTo(0.48 * 0.48 / (0.75 * 0.75)).Within(0.0001));
    }

    // ── Snr ───────────────────────────────────────────────────────────────────

    [Test]
    public void Snr_zero_ballots_returns_zero()
    {
        Assert.That(BeatGranularityService.Snr(0.45, 0.75, 0), Is.EqualTo(0));
    }

    [Test]
    public void Snr_zero_ballotSd_returns_zero()
    {
        Assert.That(BeatGranularityService.Snr(0.45, 0, 100), Is.EqualTo(0));
    }

    [Test]
    public void Snr_100_ballots_known_values()
    {
        // SNR = interBeatSD / (ballotSD / √100) = 0.45 / (0.80 / 10) = 0.45 / 0.08 = 5.625
        double result = BeatGranularityService.Snr(0.45, 0.80, 100);
        Assert.That(result, Is.EqualTo(5.625).Within(0.0001));
    }

    [Test]
    public void Snr_increases_with_ballot_count()
    {
        double snr10  = BeatGranularityService.Snr(0.45, 0.80, 10);
        double snr100 = BeatGranularityService.Snr(0.45, 0.80, 100);
        Assert.That(snr100, Is.GreaterThan(snr10));
    }

    // ── BeatGranularityReport helpers ─────────────────────────────────────────

    [Test]
    public void Report_counts_labels_correctly()
    {
        var entries = new List<BeatGranularityEntry>
        {
            new(Guid.NewGuid(), 1, "A", 2_000, 400,  BeatSizeLabel.Merge),
            new(Guid.NewGuid(), 2, "B", 5_000, 1000, BeatSizeLabel.Ok),
            new(Guid.NewGuid(), 3, "C", 9_000, 1800, BeatSizeLabel.Split),
            new(Guid.NewGuid(), 4, "D", 5_500, 1100, BeatSizeLabel.Ok),
        };
        var report = new BeatGranularityReport(
            Guid.NewGuid(), "TEST", "Test Story",
            entries, 5_375, 2_500, null);

        Assert.That(report.MergeCount, Is.EqualTo(1));
        Assert.That(report.OkCount,    Is.EqualTo(2));
        Assert.That(report.SplitCount, Is.EqualTo(1));
    }

    [Test]
    public void Report_estimated_optimal_count_is_reasonable()
    {
        // 4 beats at avg 5000 chars: TotalChars = 20000, target = 5750 → ~3 beats
        var entries = Enumerable.Range(1, 4)
            .Select(i => new BeatGranularityEntry(
                Guid.NewGuid(), i, $"Beat {i}", 5_000, 1000, BeatSizeLabel.Ok))
            .ToList();

        var report = new BeatGranularityReport(
            Guid.NewGuid(), "TEST", "Test", entries, 5_000, 0, null);

        // 4 * 5000 / 5750 = 3.478 → rounds to 3
        Assert.That(report.EstimatedOptimalCount, Is.EqualTo(3));
    }

    [Test]
    public void Report_empty_beats_estimated_count_equals_zero()
    {
        var report = new BeatGranularityReport(
            Guid.NewGuid(), "TEST", "Test", [], 0, 0, null);
        Assert.That(report.EstimatedOptimalCount, Is.EqualTo(0));
    }

    // ── Constants sanity ──────────────────────────────────────────────────────

    [Test]
    public void Constants_are_in_expected_range()
    {
        Assert.That(BeatGranularityService.OptimalMinChars, Is.EqualTo(4_000));
        Assert.That(BeatGranularityService.OptimalMaxChars, Is.EqualTo(7_500));
        Assert.That(BeatGranularityService.TargetWordsRecommended, Is.EqualTo(950));

        // Target words × 5 chars/word should sit inside the optimal range
        int targetChars = BeatGranularityService.TargetWordsRecommended * 5;
        Assert.That(targetChars, Is.GreaterThanOrEqualTo(BeatGranularityService.OptimalMinChars));
        Assert.That(targetChars, Is.LessThanOrEqualTo(BeatGranularityService.OptimalMaxChars));
    }
}
