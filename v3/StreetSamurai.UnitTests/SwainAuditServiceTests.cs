using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests for the JSON classification parser in SwainAuditService via a reflection shim.
/// The parser is private static but its contract is observable through the public types.
/// We test via SwainBeatResult construction since ParseClassification is internal to the service.
/// Coverage here focuses on the public record contract and enum semantics.
/// </summary>
[TestFixture]
public class SwainAuditServiceTests
{
    // ── SwainBeatResult semantics ─────────────────────────────────────────────

    [Test]
    public void Scene_result_is_pass()
    {
        var r = new SwainBeatResult(Guid.NewGuid(), 1, "Test", 5000, SwainClass.Scene, "none", "goal clear", "");
        Assert.That(r.IsPass, Is.True);
        Assert.That(r.Severity, Is.EqualTo(""));
    }

    [Test]
    public void Sequel_result_is_pass()
    {
        var r = new SwainBeatResult(Guid.NewGuid(), 1, "Test", 4200, SwainClass.Sequel, "none", "decision present", "");
        Assert.That(r.IsPass, Is.True);
    }

    [Test]
    public void Ambiguous_result_is_moderate()
    {
        var r = new SwainBeatResult(Guid.NewGuid(), 2, "Test", 3000, SwainClass.Ambiguous, "disaster turn", "weak turn", "MODERATE");
        Assert.That(r.IsPass, Is.False);
        Assert.That(r.Severity, Is.EqualTo("MODERATE"));
    }

    [Test]
    public void Deficient_result_is_blocker()
    {
        var r = new SwainBeatResult(Guid.NewGuid(), 3, "Test", 1200, SwainClass.Deficient, "goal", "no goal found", "BLOCKER");
        Assert.That(r.IsPass, Is.False);
        Assert.That(r.Severity, Is.EqualTo("BLOCKER"));
    }

    // ── SwainAuditReport aggregation ──────────────────────────────────────────

    [Test]
    public void Report_counts_aggregate_correctly()
    {
        var nodeId = Guid.NewGuid();
        var results = new List<SwainBeatResult>
        {
            new(Guid.NewGuid(), 1, "A", 5000, SwainClass.Scene,    "none",          "goal clear",   ""),
            new(Guid.NewGuid(), 2, "B", 4500, SwainClass.Sequel,   "none",          "decision made", ""),
            new(Guid.NewGuid(), 3, "C", 3000, SwainClass.Ambiguous,"disaster turn", "weak ending",  "MODERATE"),
            new(Guid.NewGuid(), 4, "D", 1000, SwainClass.Deficient,"goal",          "no goal",      "BLOCKER"),
        };
        var report = new SwainAuditReport(nodeId, "TEST", "Test Story", 4, results);

        Assert.That(report.PassCount,     Is.EqualTo(2));
        Assert.That(report.ModerateCount, Is.EqualTo(1));
        Assert.That(report.BlockerCount,  Is.EqualTo(1));
        Assert.That(report.TotalBeats,    Is.EqualTo(4));
        Assert.That(report.ComplianceRate, Is.EqualTo(0.5).Within(0.001));
    }

    [Test]
    public void Report_empty_beats_has_zero_compliance()
    {
        var report = new SwainAuditReport(Guid.NewGuid(), "X", "Empty", 0, []);
        Assert.That(report.ComplianceRate, Is.EqualTo(0));
        Assert.That(report.PassCount, Is.EqualTo(0));
    }

    [Test]
    public void Report_all_pass_has_full_compliance()
    {
        var results = Enumerable.Range(1, 5)
            .Select(i => new SwainBeatResult(Guid.NewGuid(), i, $"Beat {i}", 5000, SwainClass.Scene, "none", "ok", ""))
            .ToList();
        var report = new SwainAuditReport(Guid.NewGuid(), "X", "Full", 5, results);

        Assert.That(report.ComplianceRate, Is.EqualTo(1.0).Within(0.001));
        Assert.That(report.BlockerCount,   Is.EqualTo(0));
        Assert.That(report.ModerateCount,  Is.EqualTo(0));
    }

    // ── SwainClass enum coverage ──────────────────────────────────────────────

    [Test]
    public void All_four_SwainClass_values_exist()
    {
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Scene));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Sequel));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Ambiguous));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Deficient));
    }
}
