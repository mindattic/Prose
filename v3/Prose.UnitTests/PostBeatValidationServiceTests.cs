using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// SS-US-I1: PostBeatValidationService wires ProsePatternGuard, GearCarryEnforcer,
/// and BehavioralInvariantEnforcer together and runs on beat save.
/// Tests use SQLite in-memory + FakeLlmService — no real LLM calls.
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
            new ProsePatternGuard(),
            new GearCarryEnforcer(db),
            new BehavioralInvariantEnforcer(db, new FakeLlmService()),
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
    public async Task QuickValidateAsync_CleanText_CompletesWithoutException()
    {
        // QuickValidateAsync runs only ProsePatternGuard — safe with any non-null slug + text.
        await svc.QuickValidateAsync("test-story", "Kyle crossed the room and said nothing.");
    }

    [Test]
    public async Task FullValidateAsync_NoBeat_ReturnsGracefully()
    {
        // FullValidateAsync with a nonexistent beat Guid should not throw;
        // the service reads the beat from DB and returns early if not found.
        var result = await svc.FullValidateAsync(Guid.NewGuid());
        Assert.That(result, Is.Not.Null, "Result should be a non-null PostBeatValidationResult.");
    }
}
