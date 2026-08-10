using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 SemanticDrift false-positive fix (see project memory:
/// SemanticDrift false-positive class). SemanticFidelityService.AuditNodeAsync's intent-alignment
/// check flagged 213/339 BLST beats below a fixed global 0.50 similarity floor; 6/6 sampled
/// findings across all three severity tiers were confirmed (by direct text comparison) to be
/// faithful, well-executed prose that simply scores low against its own abstract synopsis
/// because of a terse book-wide register, not real drift. IsIntentOutlier requires a beat to
/// ALSO be a statistical outlier within its own book's distribution before it counts.
/// </summary>
[TestFixture]
public class SemanticFidelityServiceIntentOutlierTests
{
    [Test]
    public void SmallSample_FallsBackToTrue_RegardlessOfSpread()
    {
        // Fewer than IntentOutlierMinSample (15) points -- not enough to trust a distribution,
        // so every below-floor beat still counts (matches the pre-fix, floor-only behavior).
        var small = new List<double> { 0.30, 0.32, 0.31 };
        Assert.That(SemanticFidelityService.IsIntentOutlier(0.30, small), Is.True);
    }

    [Test]
    public void UniformlyLowBook_NoBeatIsAnOutlier()
    {
        // Reproduces the exact BLST shape: ~300+ beats clustering tightly in the 29%-50% range
        // with almost no spread -- the book's own terse register, not per-beat defects. None of
        // these should register as an outlier even though every single one is below the 0.50
        // absolute floor.
        var uniformlyLow = Enumerable.Range(0, 300)
            .Select(i => 0.35 + (i % 5) * 0.001) // tight cluster around 0.35, near-zero spread
            .ToList();

        foreach (var value in uniformlyLow)
            Assert.That(SemanticFidelityService.IsIntentOutlier(value, uniformlyLow), Is.False,
                $"value {value:P1} should not be flagged -- the whole book scores similarly low");
    }

    [Test]
    public void GenuineOutlier_InAWellAlignedBook_IsStillCaught()
    {
        // A book where most beats align well (mean ~0.70) but a handful have genuinely drifted
        // far below that baseline must still be caught -- the fix must not blind the check
        // entirely, only stop it firing on a book's uniform style.
        var mostlyHigh = Enumerable.Range(0, 50)
            .Select(i => 0.68 + (i % 7) * 0.01) // spread ~0.68-0.74
            .ToList();
        var genuineOutlier = 0.20; // far below the ~0.71 mean, many stddevs out

        Assert.That(SemanticFidelityService.IsIntentOutlier(genuineOutlier, mostlyHigh), Is.True);
        // A beat inside the book's normal range must NOT be flagged just for being on the
        // low side of its own normal spread.
        Assert.That(SemanticFidelityService.IsIntentOutlier(0.68, mostlyHigh), Is.False);
    }

    [Test]
    public void ExactZeroStdDev_DoesNotFlag()
    {
        // Degenerate case: every value in the book identical. mean - Z*0 == mean, so a naive
        // "< mean" comparison would flag ~half the beats on floating-point noise alone if this
        // weren't explicitly guarded.
        var identical = Enumerable.Repeat(0.40, 20).ToList();
        Assert.That(SemanticFidelityService.IsIntentOutlier(0.40, identical), Is.False);
    }
}
