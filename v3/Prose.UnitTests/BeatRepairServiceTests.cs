using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-13 VIGL incident: self-heal silently replaced three
/// chapter-scale beats with normal-sized fresh scenes, destroying 26% of the novel before a
/// backup epub allowed recovery. BeatRepairService.IsUnsafeShrink is the guard that now
/// refuses any such repair — exercised directly since it's a pure static predicate.
/// </summary>
[TestFixture]
public class BeatRepairServiceTests
{
    [Test]
    public void IsUnsafeShrink_LargeBeatShrunkByMoreThanHalf_ReturnsTrue()
    {
        // The actual VIGL Ch23 case: 76,868 -> 3,836 chars.
        Assert.That(BeatRepairService.IsUnsafeShrink(76868, 3836), Is.True);
    }

    [Test]
    public void IsUnsafeShrink_LargeBeatModestlyShrunk_ReturnsFalse()
    {
        Assert.That(BeatRepairService.IsUnsafeShrink(10000, 6000), Is.False);
    }

    [Test]
    public void IsUnsafeShrink_LargeBeatGrown_ReturnsFalse()
    {
        Assert.That(BeatRepairService.IsUnsafeShrink(10000, 15000), Is.False);
    }

    [Test]
    public void IsUnsafeShrink_NormalSmallBeatReplacedEntirely_ReturnsFalse()
    {
        // Below the guarded floor: ordinary single-scene repair variance, not data loss.
        Assert.That(BeatRepairService.IsUnsafeShrink(1800, 400), Is.False);
    }

    [Test]
    public void IsUnsafeShrink_AtExactRatioBoundary_ReturnsFalse()
    {
        Assert.That(BeatRepairService.IsUnsafeShrink(4000, 2000), Is.False);
    }

    [Test]
    public void IsUnsafeShrink_JustBelowRatioBoundary_ReturnsTrue()
    {
        Assert.That(BeatRepairService.IsUnsafeShrink(4000, 1999), Is.True);
    }
}
