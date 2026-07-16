using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// SS-US-I3: BeatStateExtractor runs after every beat save so EntityStateEvents stay
/// current. Tests use SQLite in-memory + FakeLlmService — no real LLM calls.
/// </summary>
[TestFixture]
public class BeatStateExtractorTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private BeatStateExtractor svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-bse-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        var db  = TestDbFactory.For(paths, "bse");

        svc = new BeatStateExtractor(
            db,
            new WorldStateLedger(db, NullLogger<WorldStateLedger>.Instance),
            new WorldClockService(db),
            new FakeLlmService(),
            new NoOpChapterRepository(),
            NullLogger<BeatStateExtractor>.Instance);

        // Disable fire-and-forget extraction on chapter save so tests stay synchronous.
        svc.AutoOnChapterSaved = false;
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public void BeatStateExtractor_AutoOnChapterSaved_CanBeDisabled()
        => Assert.That(svc.AutoOnChapterSaved, Is.False,
            "AutoOnChapterSaved=false must be settable so tests suppress fire-and-forget.");

    [Test]
    public async Task ExtractAsync_EmptyChapter_ReturnsEmptyResult()
    {
        // FakeLlmService returns "[]" — no events parsed, no DB writes.
        var chapter = new Chapter { Id = Guid.NewGuid().ToString("N"), Title = "Test" };
        var result  = await svc.ExtractAsync(chapter);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.EventsRecorded, Is.EqualTo(0),
            "Empty LLM response should produce zero EntityStateEvents.");
    }
}

/// <summary>Stub IChapterRepository — no-op, never fires OnChapterSaved.</summary>
file sealed class NoOpChapterRepository : IChapterRepository
{
    public event Action<Chapter>? OnChapterSaved;
    public List<Chapter> ListChapters() => [];
    public Chapter? LoadChapter(string id) => null;
    public void SaveChapter(Chapter chapter) { }
    public void DeleteChapter(string id) { }
}
