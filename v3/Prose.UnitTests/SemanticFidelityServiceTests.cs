using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// SS-US-I6: SemanticFidelityService compares the prose embedding centroid to the seed
/// embedding and raises a SEMANTIC-DRIFT finding on divergence.
/// Tests verify the service's public API contract and threshold constants;
/// full embedding calls require the live API and are exercised via the CLI.
/// </summary>
[TestFixture]
public class SemanticFidelityServiceTests
{
    [Test]
    public void ScoreGamingThreshold_IsReasonable()
        => Assert.That(SemanticFidelityService.ScoreGamingThreshold, Is.InRange(50.0, 95.0),
            "Score-gaming threshold must be a sensible percentage (50–95).");

    [Test]
    public void OutlineAlignmentFloor_IsReasonable()
        => Assert.That(SemanticFidelityService.OutlineAlignmentFloor, Is.InRange(0.2, 0.8),
            "Bible alignment floor must be a sensible cosine similarity (0.2–0.8).");

    [Test]
    public void IntentAlignmentFloor_IsReasonable()
        => Assert.That(SemanticFidelityService.IntentAlignmentFloor, Is.InRange(0.2, 0.8),
            "Intent alignment floor must be a sensible cosine similarity (0.2–0.8).");

    [Test]
    public void IntentAlignmentFloor_IsStricterThan_OutlineAlignmentFloor()
        => Assert.That(SemanticFidelityService.IntentAlignmentFloor,
            Is.GreaterThanOrEqualTo(SemanticFidelityService.OutlineAlignmentFloor),
            "Intent alignment should be at least as strict as Bible alignment.");

    [Test]
    public void SemanticFidelityService_HasAuditNodeAsync()
    {
        var method = typeof(SemanticFidelityService).GetMethod("AuditNodeAsync");
        Assert.That(method, Is.Not.Null, "AuditNodeAsync must exist on SemanticFidelityService.");
        Assert.That(method!.ReturnType.Name, Does.Contain("Task"),
            "AuditNodeAsync must be async.");
    }

    // Regression coverage for the 2026-08-08 gap: CheckBeatIntentDriftAsync (the score-independent
    // per-beat drift check) was wired into NodeWorkbenchService.SaveBeatAsync (the manual UI-edit
    // path) but never into ProseWriterRouter (the CLI/MCP generation path that authors the vast
    // majority of beats) — confirmed live: SEMANTIC-DRIFT findings dropped from thousands to near-zero
    // corpus-wide starting 2026-07-20 despite every other Findings category staying active, and a
    // fresh --check-fidelity run against the highest-volume affected book produced zero violations
    // because Beat.Score (its trigger) is populated on <1% of beats now that voting is off by
    // default (SS-A44). Fixed by adding a (beatId, nodeId, text, goal) overload and wiring it into
    // ProseWriterRouter's existing post-write fire-and-forget block, same pattern as LibertyReportService.

    [Test]
    public void CheckBeatIntentDriftAsync_HasIdBasedOverload_ForRouterWiring()
    {
        var method = typeof(SemanticFidelityService).GetMethod(
            "CheckBeatIntentDriftAsync",
            new[] { typeof(Guid), typeof(Guid), typeof(string), typeof(string), typeof(CancellationToken) });
        Assert.That(method, Is.Not.Null,
            "SemanticFidelityService must expose a (beatId, nodeId, beatText, synopsis) overload " +
            "so ProseWriterRouter can call it without an extra DB round-trip for beat number/slug.");
        Assert.That(method!.ReturnType.Name, Does.Contain("Task"));
    }

    [Test]
    public void ProseWriterRouter_Constructor_AcceptsSemanticFidelityService()
    {
        var ctor = typeof(ProseWriterRouter).GetConstructors().Single();
        var param = ctor.GetParameters().FirstOrDefault(p => p.ParameterType == typeof(SemanticFidelityService));
        Assert.That(param, Is.Not.Null,
            "ProseWriterRouter must accept SemanticFidelityService so the intent-drift check fires " +
            "on the actual generation path, not just manual UI edits.");
        Assert.That(param!.HasDefaultValue, Is.True, "must be optional (nullable, default null) like the other post-write add-ons.");
    }
}
