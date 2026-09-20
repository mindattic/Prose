using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 chapter-granular blueprint-consolidation bug
/// (see project memory: chapter-granular blueprint consolidation fix). The bug: the inline
/// call site used to special-case chapter-granular blueprints to report the book's CURRENT
/// chapter count as "capacity" instead of how many index-slots the blueprint's own escalation
/// curve / event palette actually cover — always "big enough" by construction, since a book
/// always has as many chapters as it has. That silently hid staleness for every
/// chapter-granular book with an undersized blueprint (VIGL: escalation length 1 vs 25 real
/// chapters; BLST: length 1 vs 21). These tests pin
/// <see cref="BeatCoordinationService.ComputeConstructionCapacity"/> to the granularity-agnostic
/// formula so a future "helpfully" reintroduced granularity branch fails immediately.
/// </summary>
[TestFixture]
public class BeatCoordinationServiceConstructionCapacityTests
{
    [Test]
    public void ReturnsEscalationLength_WhenEventsEmpty()
    {
        Assert.That(BeatCoordinationService.ComputeConstructionCapacity(25, []), Is.EqualTo(25));
    }

    [Test]
    public void ReturnsMaxEventIndexPlusOne_WhenLargerThanEscalation()
    {
        // Escalation length 1 (the real VIGL/BLST shape), one event at BeatIndex 24 —
        // capacity must reflect the event palette's real footprint, not just the escalation
        // curve's length.
        Assert.That(BeatCoordinationService.ComputeConstructionCapacity(1, [24]), Is.EqualTo(25));
    }

    [Test]
    public void ReturnsEscalationLength_WhenLargerThanEvents()
    {
        Assert.That(BeatCoordinationService.ComputeConstructionCapacity(10, [2, 4]), Is.EqualTo(10));
    }

    [Test]
    public void ReturnsZero_WhenBothEmpty()
    {
        Assert.That(BeatCoordinationService.ComputeConstructionCapacity(0, []), Is.EqualTo(0));
    }

    [Test]
    public void ReproducesTheExactViglBugShape()
    {
        // VIGL's real blueprint: EscalationCurveJson "[8]" (length 1), EventTypePaletteJson
        // has exactly one entry at BeatIndex 0. The book has 25 real chapters. The pre-fix
        // formula (chapters.Count for chapter-granular) would have returned 25 here -- "big
        // enough," hiding the staleness. The fixed formula must return 1, so
        // BookHealthService's consolidation check (capacity < realChapterCount) correctly
        // fires.
        var capacity = BeatCoordinationService.ComputeConstructionCapacity(1, [0]);
        Assert.That(capacity, Is.EqualTo(1));
        Assert.That(capacity, Is.LessThan(25), "capacity must read as undersized against VIGL's real 25 chapters");
    }

    [Test]
    public void ReproducesTheExactBlstBugShape()
    {
        // BLST's real blueprint: escalation length 1, one event entry. 21 real chapters.
        var capacity = BeatCoordinationService.ComputeConstructionCapacity(1, [0]);
        Assert.That(capacity, Is.LessThan(21), "capacity must read as undersized against BLST's real 21 chapters");
    }
}
