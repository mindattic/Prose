using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// SS-US-I3: ContinuityExtractionService runs after every beat save so the continuity
/// ledger stays current. The service depends on LlmVotingService (MindAttic.Legion)
/// which requires a live API key; full round-trip tests run via CLI.
/// These tests verify the public API contract and that the service type is wired.
/// </summary>
[TestFixture]
public class ContinuityExtractionServiceTests
{
    [Test]
    public void ContinuityExtractionService_ClassExists_InCoreAssembly()
    {
        var type = typeof(ContinuityExtractionService);
        Assert.That(type, Is.Not.Null);
        Assert.That(type.Assembly.GetName().Name, Is.EqualTo("StreetSamurai.Core"));
    }

    [Test]
    public void ContinuityExtractionService_HasExtractFromChapterAsync()
    {
        var method = typeof(ContinuityExtractionService).GetMethod("ExtractFromChapterAsync");
        Assert.That(method, Is.Not.Null, "ExtractFromChapterAsync must exist.");
        Assert.That(method!.ReturnType.Name, Does.Contain("Task"), "Must be async.");
    }

    [Test]
    public void ContinuityExtractionService_HasExtractFromBookAsync()
    {
        var method = typeof(ContinuityExtractionService).GetMethod("ExtractFromBookAsync");
        Assert.That(method, Is.Not.Null, "ExtractFromBookAsync must exist.");
    }

    [Test]
    public void ContinuityExtractionService_HasExtractFromEntityRecordAsync()
    {
        var method = typeof(ContinuityExtractionService).GetMethod("ExtractFromEntityRecordAsync");
        Assert.That(method, Is.Not.Null, "ExtractFromEntityRecordAsync must exist.");
    }
}
