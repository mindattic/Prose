using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// SS-US-I1: PostBeatValidationService wires GearCarryEnforcer and runs on explicit request.
/// It used to have two more legs, both retired by author ruling on 2026-09-06:
/// BehavioralInvariantEnforcer (4 of 4 hand-read false positives — an arc is not a defect) and
/// ProsePatternGuard, the house-style linter that filed [Cliche] findings on every beat save
/// (RFC 0009 — corpus apply rate zero; its only product was pressure to restyle finished prose).
/// Tests use SQLite in-memory — no real LLM calls.
/// </summary>
[TestFixture]
public class PostBeatValidationServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private PostBeatValidationService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-pbv-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths    = new TestPathProviderWithRoot(tempRoot);
        var db   = TestDbFactory.For(paths, "pbv");

        svc = new PostBeatValidationService(
            new GearCarryEnforcer(db),
            new FindingsService(db, paths),
            db,
            NullLogger<PostBeatValidationService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public async Task FullValidateAsync_NoBeat_ReturnsGracefully()
    {
        // FullValidateAsync with a nonexistent beat Guid should not throw;
        // the service reads the beat from DB and returns early if not found.
        var result = await svc.FullValidateAsync(Guid.NewGuid());
        Assert.That(result, Is.Not.Null, "Result should be a non-null PostBeatValidationResult.");
        Assert.That(result.ProseViolations, Is.Zero, "the prose-guard tier is gone; this must stay 0 forever");
    }
}
