using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class BookHealthServicePlantDensityTests
{
    [Test]
    public void ComputePlantDensity_EvenlySpread_FlagsNeither()
    {
        // 100-beat book, plants at 10, 30, 50, 70, 90 — spread across the whole book.
        var result = BookHealthService.ComputePlantDensity([10, 30, 50, 70, 90], 100);
        Assert.That(result.FrontLoaded, Is.False);
        Assert.That(result.HasDrought, Is.False);
    }

    [Test]
    public void ComputePlantDensity_AllPlantsInFirstQuarter_FlagsFrontLoaded()
    {
        // 100-beat book, all 5 plants land at or before beat 25 (the first quarter).
        var result = BookHealthService.ComputePlantDensity([2, 8, 15, 20, 25], 100);
        Assert.That(result.FrontLoaded, Is.True);
        Assert.That(result.FrontLoadedCount, Is.EqualTo(5));
        Assert.That(result.FrontLoadRate, Is.EqualTo(1.0));
    }

    [Test]
    public void ComputePlantDensity_LongGapBetweenPlants_FlagsDrought()
    {
        // 100-beat book: plants at 5 and 10 (close together), then nothing until 90 —
        // an 80-beat (80%) stretch with no new plant.
        var result = BookHealthService.ComputePlantDensity([5, 10, 90], 100);
        Assert.That(result.HasDrought, Is.True);
        Assert.That(result.MaxGap, Is.EqualTo(80));
    }

    [Test]
    public void ComputePlantDensity_ExactlyAtThreshold_FrontLoadedIsInclusive()
    {
        // 3 of 4 plants (75%) in the first quarter — right at the >=0.75 boundary.
        var result = BookHealthService.ComputePlantDensity([5, 10, 20, 80], 100);
        Assert.That(result.FrontLoadRate, Is.EqualTo(0.75));
        Assert.That(result.FrontLoaded, Is.True);
    }

    [Test]
    public void ComputePlantDensity_JustBelowThresholds_FlagsNeither()
    {
        // 2 of 3 plants (66.7%) in the first quarter — below the 75% front-load bar;
        // max gap is 49 beats (49%) — just under the >=0.5 drought bar.
        var result = BookHealthService.ComputePlantDensity([10, 20, 69], 100);
        Assert.That(result.FrontLoadRate, Is.LessThan(0.75));
        Assert.That(result.FrontLoaded, Is.False);
        Assert.That(result.MaxGapRate, Is.LessThan(0.5));
        Assert.That(result.HasDrought, Is.False);
    }
}
