using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests for EmotionalDepthService.ParseBeatCurve — the per-beat emotional-depth scoring parser
/// (Pass 2 of the emotional examination, "ss --examine-emotion"). Extracted from
/// RunBeatCurveAsync into its own internal static method specifically so this logic is directly
/// testable (was private, mixed in with the LLM call); InternalsVisibleTo already covers this
/// project.
///
/// Found and fixed a real bug while adding this coverage (same class as the LogicSweepService/
/// ChekhovAuditService bugs fixed earlier this session): JsonElement.GetInt32() throws on a
/// non-Number "beat_number"/"depth" (e.g. a hallucinated null), and the loop's single try/catch
/// wrapped the WHOLE curve — one malformed beat entry silently discarded the emotional score for
/// every other beat in the same response.
/// </summary>
[TestFixture]
public class EmotionalDepthServiceTests
{
    private static readonly IReadOnlyList<int> BeatNumbers = [1, 2, 3];

    [Test]
    public void ParseBeatCurve_ValidArray_ParsesAllFields()
    {
        var raw = """[{"beat_number":1,"depth":3,"note":"strong grief"}]""";
        var results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BeatNumber, Is.EqualTo(1));
        Assert.That(results[0].Depth, Is.EqualTo(3));
        Assert.That(results[0].Note, Is.EqualTo("strong grief"));
    }

    [Test]
    public void ParseBeatCurve_MissingBeatNumber_FallsBackToPositionalBeatNumbers()
    {
        var raw = """[{"depth":2,"note":"ok"},{"depth":4,"note":"great"}]""";
        var results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers);

        Assert.That(results.Select(r => r.BeatNumber), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void ParseBeatCurve_DepthOutOfRange_IsClamped()
    {
        var raw = """[{"beat_number":1,"depth":99,"note":"x"}]""";
        var results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers);
        Assert.That(results[0].Depth, Is.EqualTo(4));

        var raw2 = """[{"beat_number":1,"depth":-5,"note":"x"}]""";
        var results2 = EmotionalDepthService.ParseBeatCurve(raw2, BeatNumbers);
        Assert.That(results2[0].Depth, Is.EqualTo(0));
    }

    [Test]
    public void ParseBeatCurve_MissingDepth_DefaultsToTwo()
    {
        var raw = """[{"beat_number":1,"note":"x"}]""";
        var results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers);
        Assert.That(results[0].Depth, Is.EqualTo(2));
    }

    [Test]
    public void ParseBeatCurve_MalformedJson_ReturnsEmptyInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var results = EmotionalDepthService.ParseBeatCurve("{\"beat_number\": oops}", BeatNumbers);
            Assert.That(results, Is.Empty);
        });
    }

    // ── Regression: a hallucinated null beat_number/depth must not discard the whole curve ──

    [Test]
    public void ParseBeatCurve_NullBeatNumberOnOneEntry_OtherBeatsStillScored()
    {
        var raw = """
            [
                {"beat_number":null,"depth":3,"note":"broken entry"},
                {"beat_number":2,"depth":1,"note":"good entry"}
            ]
            """;
        var results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers);

        Assert.That(results.Any(r => r.Note == "good entry"), Is.True,
            "one malformed entry (null beat_number) must not discard every other beat's score");
    }

    [Test]
    public void ParseBeatCurve_NullDepthOnOneEntry_OtherBeatsStillScored()
    {
        var raw = """
            [
                {"beat_number":1,"depth":null,"note":"broken entry"},
                {"beat_number":2,"depth":3,"note":"good entry"}
            ]
            """;
        var results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers);

        Assert.That(results.Any(r => r.Note == "good entry"), Is.True);
    }

    [Test]
    public void ParseBeatCurve_NullBeatNumber_FallsBackRatherThanThrows()
    {
        var raw = """[{"beat_number":null,"depth":2,"note":"x"}]""";
        List<StreetSamurai.Core.Services.BeatEmotionalScore> results = null!;

        Assert.DoesNotThrow(() => results = EmotionalDepthService.ParseBeatCurve(raw, BeatNumbers));
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BeatNumber, Is.EqualTo(1)); // falls back to beatNumbers[0]
    }

    [Test]
    public void ParseBeatCurve_EmptyArray_ReturnsEmpty()
    {
        var results = EmotionalDepthService.ParseBeatCurve("[]", BeatNumbers);
        Assert.That(results, Is.Empty);
    }
}
