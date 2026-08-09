using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to NarrativeScienceService.CheckDramaticQuestionAsync.
/// On a parse failure it used to fabricate a DramaticQuestionResult (OverallScore=0,
/// SurfaceSummary="(parse error)") — but the caller, BookHealthService.DramaticQuestionAsync,
/// reads SubconsciousSummary (not SurfaceSummary) when building its finding text, so the error
/// was completely invisible: a parse failure filed a real-looking "scores 0/10 ... no
/// subconscious layer detected" DRAMATIC-Q finding, indistinguishable from genuine critique.
/// Same defect family as the SwainAuditService/BehavioralInvariantEnforcer fixes earlier this
/// session. Fixed by throwing on parse failure instead of fabricating a result — brings this
/// method in line with its own sibling AnalyzeSacredFlawAsync, whose parse-error text already
/// lands in a field (Diagnosis) the caller actually displays.
/// </summary>
[TestFixture]
public class NarrativeScienceServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-narrativescience-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private NarrativeScienceService Make(ConfigurableLlm llm) =>
        new(llm, TestDbFactory.For(paths, "narrative"));

    [Test]
    public async Task CheckDramaticQuestionAsync_ValidJson_ParsesCorrectly()
    {
        var llm = new ConfigurableLlm
        {
            Response = """{"surface_score":7,"subconscious_score":6,"overall_score":6,"surface_summary":"she confronts the debt","subconscious_summary":"reveals her fear of dependency","dramatic_question_active":true,"improvement_hint":"none needed"}""",
        };
        var result = await Make(llm).CheckDramaticQuestionAsync("Some beat text.");

        Assert.That(result.OverallScore, Is.EqualTo(6));
        Assert.That(result.DramaticQuestionActive, Is.True);
        Assert.That(result.SubconsciousSummary, Is.EqualTo("reveals her fear of dependency"));
    }

    [Test]
    public void CheckDramaticQuestionAsync_MalformedJson_ThrowsRatherThanFabricatingScore()
    {
        var llm = new ConfigurableLlm { Response = "I cannot evaluate this beat." };
        Assert.That(async () => await Make(llm).CheckDramaticQuestionAsync("Some beat text."),
            Throws.InvalidOperationException);
    }

    [Test]
    public void CheckDramaticQuestionAsync_EmptyResponse_ThrowsRatherThanFabricatingScore()
    {
        var llm = new ConfigurableLlm { Response = "" };
        Assert.That(async () => await Make(llm).CheckDramaticQuestionAsync("Some beat text."),
            Throws.Exception);
    }

    [Test]
    public void CheckDramaticQuestionAsync_LlmThrows_Propagates()
    {
        var llm = new ConfigurableLlm { Throws = true };
        Assert.That(async () => await Make(llm).CheckDramaticQuestionAsync("Some beat text."),
            Throws.InvalidOperationException);
    }
}
