using StreetSamurai.Core.Services.Audit;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests for LogicSweepService's deterministic, LLM-free helpers. The six audit dimensions
/// themselves (causality, knowledge states, timeline, plant/payoff, orphan references, bible
/// agreement) are each a single LLM call and aren't practically unit-testable, but
/// <c>ParseFindingsArray</c> — the ONE parser all six dimensions share for their untrusted LLM
/// JSON output — and <c>ClampProse</c> are pure logic worth covering directly. Both made
/// <c>internal</c> (were <c>private</c>); <c>InternalsVisibleTo</c> already covers this project.
/// </summary>
[TestFixture]
public class LogicSweepServiceTests
{
    private static readonly IReadOnlyList<AuditBeat> Beats =
    [
        new AuditBeat(Guid.Parse("00000000-0000-0000-0000-000000000001"), 1, "Beat one text."),
        new AuditBeat(Guid.Parse("00000000-0000-0000-0000-000000000002"), 2, "Beat two text."),
        new AuditBeat(Guid.Parse("00000000-0000-0000-0000-000000000003"), 3, "Beat three text."),
    ];

    // ── ParseFindingsArray ─────────────────────────────────────────────────────

    [Test]
    public void ParseFindingsArray_ValidArray_ParsesAllFields()
    {
        var raw = """
            [{"beat_number":2,"severity":"blocker","evidence":"contradicts beat 1","fix":"reconcile the two"}]
            """;
        var results = LogicSweepService.ParseFindingsArray("causality", "Causality chain", raw, Beats);

        Assert.That(results, Has.Count.EqualTo(1));
        var v = results[0];
        Assert.That(v.RuleKey, Is.EqualTo("causality"));
        Assert.That(v.Title, Is.EqualTo("Causality chain"));
        Assert.That(v.Severity, Is.EqualTo("BLOCKER")); // upper-cased
        Assert.That(v.Evidence, Does.StartWith("Beat #2:"));
        Assert.That(v.Location, Is.EqualTo(Beats[1].Id.ToString()));
        Assert.That(v.Fix, Is.EqualTo("reconcile the two"));
    }

    [Test]
    public void ParseFindingsArray_EmptyArray_ReturnsEmpty()
    {
        var results = LogicSweepService.ParseFindingsArray("timeline", "Timeline", "[]", Beats);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseFindingsArray_ChatterAroundArray_ExtractsInnerArray()
    {
        var raw = "Here are the findings:\n[{\"beat_number\":1,\"severity\":\"minor\",\"evidence\":\"small nit\",\"fix\":null}]\nDone.";
        var results = LogicSweepService.ParseFindingsArray("timeline", "Timeline", raw, Beats);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Severity, Is.EqualTo("MINOR"));
        Assert.That(results[0].Fix, Is.Null);
    }

    [Test]
    public void ParseFindingsArray_NoBrackets_ReturnsEmpty()
    {
        var results = LogicSweepService.ParseFindingsArray("timeline", "Timeline", "The timeline holds.", Beats);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseFindingsArray_MalformedJson_ReturnsEmptyInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var results = LogicSweepService.ParseFindingsArray("timeline", "Timeline", "[{\"severity\": oops}]", Beats);
            Assert.That(results, Is.Empty);
        });
    }

    [Test]
    public void ParseFindingsArray_EmptyEvidence_EntryIsDropped()
    {
        var raw = """[{"beat_number":1,"severity":"blocker","evidence":"","fix":"x"}]""";
        var results = LogicSweepService.ParseFindingsArray("causality", "Causality chain", raw, Beats);
        Assert.That(results, Is.Empty, "an entry with no evidence is not a real finding and must not persist");
    }

    [Test]
    public void ParseFindingsArray_UnknownSeverity_DefaultsToModerate()
    {
        var raw = """[{"beat_number":1,"severity":"catastrophic","evidence":"something's wrong","fix":null}]""";
        var results = LogicSweepService.ParseFindingsArray("causality", "Causality chain", raw, Beats);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Severity, Is.EqualTo("MODERATE"));
    }

    [Test]
    public void ParseFindingsArray_NullBeatNumber_LocationIsNull()
    {
        // plant/payoff and bible-agreement findings can be whole-node (beat_number: null)
        var raw = """[{"beat_number":null,"severity":"moderate","evidence":"whole-book issue","fix":null}]""";
        var results = LogicSweepService.ParseFindingsArray("plant_payoff", "Plant/payoff ledger", raw, Beats);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Location, Is.Null);
        Assert.That(results[0].Evidence, Is.EqualTo("whole-book issue")); // no "Beat #" prefix without a number
    }

    [Test]
    public void ParseFindingsArray_BeatNumberNotInBeatsList_LocationIsNullButEvidenceStillTagged()
    {
        // beat_number references a beat that isn't in this audit's beat set (e.g. stale/renumbered)
        var raw = """[{"beat_number":99,"severity":"minor","evidence":"orphaned reference","fix":null}]""";
        var results = LogicSweepService.ParseFindingsArray("orphan_refs", "Orphan references", raw, Beats);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Location, Is.Null);
        Assert.That(results[0].Evidence, Does.StartWith("Beat #99:"));
    }

    [Test]
    public void ParseFindingsArray_MultipleFindings_AllParsed()
    {
        var raw = """
            [
                {"beat_number":1,"severity":"blocker","evidence":"first problem","fix":"fix one"},
                {"beat_number":2,"severity":"moderate","evidence":"second problem","fix":"fix two"},
                {"beat_number":3,"severity":"minor","evidence":"third problem","fix":null}
            ]
            """;
        var results = LogicSweepService.ParseFindingsArray("causality", "Causality chain", raw, Beats);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results.Select(r => r.Severity), Is.EqualTo(new[] { "BLOCKER", "MODERATE", "MINOR" }));
    }

    // ── ClampProse ─────────────────────────────────────────────────────────────

    [Test]
    public void ClampProse_ShortText_ReturnedUnchanged()
    {
        var text = new string('x', 500);
        Assert.That(LogicSweepService.ClampProse(text), Is.EqualTo(text));
    }

    [Test]
    public void ClampProse_OverLimit_KeepsHeadAndTail()
    {
        var head = new string('a', 50000);
        var tail = new string('b', 50000);
        var text = head + new string('m', 5000) + tail;

        var clamped = LogicSweepService.ClampProse(text);

        Assert.That(clamped, Does.StartWith(head));
        Assert.That(clamped, Does.EndWith(tail));
        Assert.That(clamped, Does.Contain("elided for length"));
    }

    // ── LogicSweepReport aggregation ───────────────────────────────────────────

    [Test]
    public void LogicSweepReport_CountsAggregateCorrectly()
    {
        var findings = new List<AuditVerdict>
        {
            new("causality", "Causality chain", "BLOCKER", "e1"),
            new("timeline", "Timeline", "MODERATE", "e2"),
            new("timeline", "Timeline", "MINOR", "e3"),
        };
        var report = new LogicSweepReport(Guid.NewGuid(), "test-slug", "Test Book", 10, findings);

        Assert.That(report.BlockerCount, Is.EqualTo(1));
        Assert.That(report.ModerateCount, Is.EqualTo(1));
        Assert.That(report.MinorCount, Is.EqualTo(1));
        Assert.That(report.Clean, Is.False);
    }

    [Test]
    public void LogicSweepReport_NoFindings_IsClean()
    {
        var report = new LogicSweepReport(Guid.NewGuid(), "test-slug", "Test Book", 10, []);
        Assert.That(report.Clean, Is.True);
        Assert.That(report.BlockerCount, Is.EqualTo(0));
    }
}
