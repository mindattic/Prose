using Prose.Core.Services;

namespace Prose.UnitTests;

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

    [Test]
    public void Error_result_is_not_pass_and_not_blocker()
    {
        // ERROR (evaluation could not be completed) must never look like a real content
        // verdict — neither a pass (would inflate compliance) nor a BLOCKER (would fabricate
        // a "Deficient" finding for a beat that was never actually judged).
        var r = new SwainBeatResult(Guid.NewGuid(), 4, "Test", 800, SwainClass.Error, "none", "Circuit breaker open", "ERROR");
        Assert.That(r.IsPass, Is.False);
        Assert.That(r.Severity, Is.EqualTo("ERROR"));
        Assert.That(r.Severity, Is.Not.EqualTo("BLOCKER"));
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
    public void Report_AllBeatsError_ComplianceIsZeroNotFalseDeficient()
    {
        // The real-corpus bug this session (2026-08-09): a total API outage used to make every
        // beat classify as Deficient/BLOCKER, reporting "0% compliant = 100% structurally
        // deficient" when actually 0% of the book was ever evaluated. Now: ErrorCount absorbs
        // every beat, BlockerCount is correctly 0 (no fabricated content verdicts), and
        // ComplianceRate reports 0.0 via the "nothing was evaluated" path, not a false positive
        // compliance count either way.
        var results = Enumerable.Range(1, 5)
            .Select(i => new SwainBeatResult(Guid.NewGuid(), i, $"Beat {i}", 5000, SwainClass.Error, "none", "Circuit breaker open", "ERROR"))
            .ToList();
        var report = new SwainAuditReport(Guid.NewGuid(), "X", "Outage", 5, results);

        Assert.That(report.ErrorCount, Is.EqualTo(5));
        Assert.That(report.BlockerCount, Is.EqualTo(0));
        Assert.That(report.PassCount, Is.EqualTo(0));
        Assert.That(report.ComplianceRate, Is.EqualTo(0.0));
    }

    [Test]
    public void Report_PartialErrors_ComplianceExcludesErrorsFromDenominator()
    {
        // 2 real passes, 1 real blocker, 2 errors -> compliance should be 2/3 (evaluated beats
        // only), not 2/5 (which would unfairly punish a book for an infra hiccup mid-audit).
        var results = new List<SwainBeatResult>
        {
            new(Guid.NewGuid(), 1, "A", 5000, SwainClass.Scene,     "none", "ok",      ""),
            new(Guid.NewGuid(), 2, "B", 4500, SwainClass.Sequel,    "none", "ok",      ""),
            new(Guid.NewGuid(), 3, "C", 1000, SwainClass.Deficient, "goal", "no goal", "BLOCKER"),
            new(Guid.NewGuid(), 4, "D", 800,  SwainClass.Error,     "none", "timeout", "ERROR"),
            new(Guid.NewGuid(), 5, "E", 800,  SwainClass.Error,     "none", "timeout", "ERROR"),
        };
        var report = new SwainAuditReport(Guid.NewGuid(), "X", "Partial", 5, results);

        Assert.That(report.ErrorCount, Is.EqualTo(2));
        Assert.That(report.ComplianceRate, Is.EqualTo(2.0 / 3.0).Within(0.001));
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
    public void All_five_SwainClass_values_exist()
    {
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Scene));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Sequel));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Ambiguous));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Deficient));
        Assert.That(Enum.GetValues<SwainClass>(), Has.Member(SwainClass.Error));
    }

    // ── ParseClassification: the untrusted-LLM-JSON parser ─────────────────────
    // Made `internal` (was `private`) specifically so this real logic — not just
    // the record/enum shape — gets exercised. This is the part that actually
    // touches unpredictable model output and is where a real bug would live.

    private static readonly Guid Id = Guid.NewGuid();

    [Test]
    public void ParseClassification_PlainJson_MapsAllFields()
    {
        var raw = """{"class":"Scene","missing":"none","note":"goal stated in para 1"}""";
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, raw);

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Scene));
        Assert.That(r.MissingElement, Is.EqualTo("none"));
        Assert.That(r.Note, Is.EqualTo("goal stated in para 1"));
        Assert.That(r.IsPass, Is.True);
    }

    [Test]
    public void ParseClassification_MarkdownFencedJson_StripsFence()
    {
        var raw = "```json\n{\"class\":\"Sequel\",\"missing\":\"none\",\"note\":\"decision at end\"}\n```";
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, raw);

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Sequel));
        Assert.That(r.IsPass, Is.True);
    }

    [Test]
    public void ParseClassification_ChatterAroundJson_ExtractsInnerObject()
    {
        // LLMs sometimes prepend/append commentary despite the system prompt; the
        // parser locates the first '{' and last '}' rather than requiring the
        // whole response to be pure JSON.
        var raw = "Sure, here is my answer:\n{\"class\":\"Ambiguous\",\"missing\":\"dilemma\",\"note\":\"no real choice\"}\nHope that helps!";
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, raw);

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Ambiguous));
        Assert.That(r.MissingElement, Is.EqualTo("dilemma"));
        Assert.That(r.Severity, Is.EqualTo("MODERATE"));
    }

    [Test]
    public void ParseClassification_MissingOptionalFields_DefaultsApplied()
    {
        var raw = """{"class":"Scene"}""";
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, raw);

        Assert.That(r.MissingElement, Is.EqualTo("none"));
        Assert.That(r.Note, Is.EqualTo(""));
    }

    [Test]
    public void ParseClassification_UnknownClassValue_FallsBackToDeficientBlocker()
    {
        var raw = """{"class":"Whatever","missing":"goal","note":"n/a"}""";
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, raw);

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Deficient));
        Assert.That(r.Severity, Is.EqualTo("BLOCKER"));
    }

    // These three "could not evaluate" paths (empty response / unparseable / malformed JSON) must
    // NEVER produce SwainClass.Deficient/"BLOCKER" — that fabricates a content verdict the LLM
    // never actually rendered. Real-corpus bug found 2026-08-09: a total API outage classified
    // 100% of a book's beats "Deficient" this way, cratering its SII on a false content signal
    // when 0% of the book was ever evaluated. Fixed to SwainClass.Error/"ERROR" instead.

    [Test]
    public void ParseClassification_EmptyRaw_IsEvalErrorNotDeficient()
    {
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, "   ");

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Error));
        Assert.That(r.Severity, Is.EqualTo("ERROR"));
        Assert.That(r.Note, Does.Contain("Empty LLM response"));
    }

    [Test]
    public void ParseClassification_NoJsonBraces_IsEvalErrorNotDeficient()
    {
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, "I refuse to answer.");

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Error));
        Assert.That(r.Severity, Is.EqualTo("ERROR"));
        Assert.That(r.Note, Does.Contain("No JSON in response"));
    }

    [Test]
    public void ParseClassification_MalformedJson_IsEvalErrorNotDeficient()
    {
        var r = SwainAuditService.ParseClassification(Id, 1, "Beat 1", 500, "{\"class\":\"Scene\", oops}");

        Assert.That(r.Classification, Is.EqualTo(SwainClass.Error));
        Assert.That(r.Severity, Is.EqualTo("ERROR"));
        Assert.That(r.Note, Does.Contain("JSON parse error"));
    }

    [Test]
    public void ParseClassification_PreservesPositionTitleAndCharCount()
    {
        var raw = """{"class":"Scene","missing":"none","note":"ok"}""";
        var r = SwainAuditService.ParseClassification(Id, 7, "The Ambush", 1234, raw);

        Assert.That(r.Position,  Is.EqualTo(7));
        Assert.That(r.Title,    Is.EqualTo("The Ambush"));
        Assert.That(r.CharCount, Is.EqualTo(1234));
        Assert.That(r.BeatId,   Is.EqualTo(Id));
    }
}
