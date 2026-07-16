using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

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
    public void BibleAlignmentFloor_IsReasonable()
        => Assert.That(SemanticFidelityService.BibleAlignmentFloor, Is.InRange(0.2, 0.8),
            "Bible alignment floor must be a sensible cosine similarity (0.2–0.8).");

    [Test]
    public void IntentAlignmentFloor_IsReasonable()
        => Assert.That(SemanticFidelityService.IntentAlignmentFloor, Is.InRange(0.2, 0.8),
            "Intent alignment floor must be a sensible cosine similarity (0.2–0.8).");

    [Test]
    public void IntentAlignmentFloor_IsStricterThan_BibleAlignmentFloor()
        => Assert.That(SemanticFidelityService.IntentAlignmentFloor,
            Is.GreaterThanOrEqualTo(SemanticFidelityService.BibleAlignmentFloor),
            "Intent alignment should be at least as strict as Bible alignment.");

    [Test]
    public void SemanticFidelityService_HasAuditNodeAsync()
    {
        var method = typeof(SemanticFidelityService).GetMethod("AuditNodeAsync");
        Assert.That(method, Is.Not.Null, "AuditNodeAsync must exist on SemanticFidelityService.");
        Assert.That(method!.ReturnType.Name, Does.Contain("Task"),
            "AuditNodeAsync must be async.");
    }
}
