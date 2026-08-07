using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Tests for AuditRunner's pure, LLM-free helpers — the shared dispatch/persistence engine
/// underlying every rule-based audit in the app (BookAuditService, LogicSweepService,
/// NounConsistencyService, CraftRuleAuditService). A bug here affects every audit type, so this
/// is the highest-leverage place in the audit stack to have solid coverage. Made
/// DeleteKeyPrefix/SummaryFor/ParseVerdictEnvelope/VerdictEnvelope/Truncate internal (were
/// private) — InternalsVisibleTo already covers this project. ParseSingleVerdict was already
/// public. RunAsync/WriteFindingsForRules themselves need a live LLM + FindingsService and
/// aren't covered here.
/// </summary>
[TestFixture]
public class AuditRunnerTests
{
    // ── DeleteKeyPrefix / SummaryFor: the '@' boundary that keeps "noun_3" from ────────
    // ── deleting "noun_34"'s rows too (see the class doc comment on WriteFindingsForRules) ──

    [Test]
    public void DeleteKeyPrefix_EndsWithAtBoundary()
    {
        var prefix = AuditRunner.DeleteKeyPrefix("LOGICSWEEP", "noun_3");
        Assert.That(prefix, Is.EqualTo("LOGICSWEEP noun_3@"));
    }

    [Test]
    public void DeleteKeyPrefix_ShortRuleKey_DoesNotPrefixMatchLongerKey()
    {
        // The whole point of the '@' boundary: "noun_3@" must NOT be a string-prefix of
        // "noun_34@..." — if it were, deleting "noun_3"'s findings would also nuke "noun_34"'s.
        var shortKeyPrefix = AuditRunner.DeleteKeyPrefix("NOUN", "noun_3");
        var longKeySummary = AuditRunner.SummaryFor("NOUN", "noun_34", "beat-loc") + "some evidence";

        Assert.That(longKeySummary, Does.Not.StartWith(shortKeyPrefix));
    }

    [Test]
    public void SummaryFor_NullLocation_RendersLiteralNull()
    {
        // AuditVerdict.Location is nullable (whole-node verdicts); string interpolation of null
        // renders as the empty string, not the word "null" — confirms callers reading this back
        // (DeleteBySummaryPrefix) don't need to special-case a missing location.
        var summary = AuditRunner.SummaryFor("BOOKAUDIT", "commandment_1", null);
        Assert.That(summary, Is.EqualTo("BOOKAUDIT commandment_1@: "));
    }

    [Test]
    public void SummaryFor_WithLocation_IncludesIt()
    {
        var summary = AuditRunner.SummaryFor("LOGICSWEEP", "causality", "beat-guid-123");
        Assert.That(summary, Is.EqualTo("LOGICSWEEP causality@beat-guid-123: "));
    }

    // ── Truncate ────────────────────────────────────────────────────────────────

    [Test]
    public void Truncate_ShortString_Unchanged()
    {
        Assert.That(AuditRunner.Truncate("short", 300), Is.EqualTo("short"));
    }

    [Test]
    public void Truncate_ExactlyAtLimit_Unchanged()
    {
        var s = new string('x', 300);
        Assert.That(AuditRunner.Truncate(s, 300), Is.EqualTo(s));
    }

    [Test]
    public void Truncate_OverLimit_AppendsEllipsis()
    {
        var s = new string('x', 305);
        var result = AuditRunner.Truncate(s, 300);
        Assert.That(result.Length, Is.EqualTo(301)); // 300 chars + ellipsis
        Assert.That(result, Does.EndWith("…"));
    }

    // ── ParseVerdictEnvelope / ParseSingleVerdict: the shared single-verdict JSON parser ──

    private sealed class FakeRule : ILlmAuditRule
    {
        public string Key => "fake_rule";
        public string Title => "Fake Rule";
        public (string System, string User) BuildPrompt(AuditContext ctx) => ("", "");
    }

    private sealed class FakeRuleCustomFailSeverity : ILlmAuditRule
    {
        public string Key => "fake_rule_2";
        public string Title => "Fake Rule 2";
        public string SeverityOnFail => "MODERATE";
        public (string System, string User) BuildPrompt(AuditContext ctx) => ("", "");
    }

    [Test]
    public void ParseVerdictEnvelope_PlainJson_Parses()
    {
        var env = AuditRunner.ParseVerdictEnvelope("""{"status":"pass","evidence":"looks fine","fix":null}""");
        Assert.That(env, Is.Not.Null);
        Assert.That(env!.Status, Is.EqualTo("pass"));
        Assert.That(env.Evidence, Is.EqualTo("looks fine"));
    }

    [Test]
    public void ParseVerdictEnvelope_NoBraces_ReturnsNull()
    {
        Assert.That(AuditRunner.ParseVerdictEnvelope("no json here"), Is.Null);
    }

    [Test]
    public void ParseVerdictEnvelope_MalformedJson_ReturnsNullInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
            Assert.That(AuditRunner.ParseVerdictEnvelope("{\"status\": oops}"), Is.Null));
    }

    [Test]
    public void ParseSingleVerdict_PassStatus_MapsToPassSeverity()
    {
        var verdicts = AuditRunner.ParseSingleVerdict(new FakeRule(),
            """{"status":"pass","evidence":"all good","fix":null}""");
        Assert.That(verdicts, Has.Count.EqualTo(1));
        Assert.That(verdicts[0].Severity, Is.EqualTo("PASS"));
    }

    [Test]
    public void ParseSingleVerdict_WarnStatus_MapsToModerate()
    {
        var verdicts = AuditRunner.ParseSingleVerdict(new FakeRule(),
            """{"status":"warn","evidence":"borderline","fix":"tighten this"}""");
        Assert.That(verdicts[0].Severity, Is.EqualTo("MODERATE"));
        Assert.That(verdicts[0].Fix, Is.EqualTo("tighten this"));
    }

    [Test]
    public void ParseSingleVerdict_FailStatus_UsesRulesSeverityOnFail()
    {
        var defaultRule = AuditRunner.ParseSingleVerdict(new FakeRule(),
            """{"status":"fail","evidence":"broken","fix":null}""");
        Assert.That(defaultRule[0].Severity, Is.EqualTo("BLOCKER"), "default SeverityOnFail is BLOCKER");

        var customRule = AuditRunner.ParseSingleVerdict(new FakeRuleCustomFailSeverity(),
            """{"status":"fail","evidence":"broken","fix":null}""");
        Assert.That(customRule[0].Severity, Is.EqualTo("MODERATE"), "a rule can override SeverityOnFail");
    }

    [Test]
    public void ParseSingleVerdict_UnrecognizedStatus_DefaultsToModerate()
    {
        var verdicts = AuditRunner.ParseSingleVerdict(new FakeRule(),
            """{"status":"maybe","evidence":"unclear","fix":null}""");
        Assert.That(verdicts[0].Severity, Is.EqualTo("MODERATE"));
    }

    [Test]
    public void ParseSingleVerdict_UnparsableResponse_ReturnsEvaluationFailedEvidence()
    {
        var verdicts = AuditRunner.ParseSingleVerdict(new FakeRule(), "not json at all");
        Assert.That(verdicts, Has.Count.EqualTo(1));
        Assert.That(verdicts[0].Severity, Is.EqualTo("MODERATE"));
        Assert.That(verdicts[0].Evidence, Is.EqualTo("(evaluation failed)"));
    }

    [Test]
    public void ParseSingleVerdict_CarriesRuleKeyAndTitle()
    {
        var verdicts = AuditRunner.ParseSingleVerdict(new FakeRule(),
            """{"status":"pass","evidence":"ok","fix":null}""");
        Assert.That(verdicts[0].RuleKey, Is.EqualTo("fake_rule"));
        Assert.That(verdicts[0].Title, Is.EqualTo("Fake Rule"));
    }
}
