using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for the audio storage abstraction layer:
/// <list type="bullet">
/// <item><see cref="AudioPath"/> — canonical-shape detection and round-trip parsing.</item>
/// <item><see cref="LocalDiskAudioStore"/> — disk read/write contract.</item>
/// <item><see cref="DualWriteAudioStore"/> — fan-out writes, read-fallback, cache-back.</item>
/// </list>
/// No Azure dependency — the blob store is exercised by integration tests
/// that talk to a real account. These cover everything that's runnable in
/// CI on a clean box.
/// </summary>
[TestFixture]
public class AudioPathTests
{
    [TestCase("rooftop-job-a8f3/audio/0123456789abcdef0123456789abcdef.mp3", true)]
    [TestCase("rooftop-job-a8f3/audio/0123456789ABCDEF0123456789ABCDEF.wav", true)]
    [TestCase("rooftop-job-a8f3/node.mp3", true)]
    [TestCase("rooftop-job-a8f3/node.wav", true)]
    [TestCase("rooftop-job-a8f3/node.m4a", true)]
    [TestCase("rooftop-job-a8f3/audio/000.mp3",                     false)] // legacy numeric stem
    [TestCase("rooftop-job-a8f3/audio/not-a-guid.mp3",              false)] // bad GUID
    [TestCase("rooftop-job-a8f3/audio/0123456789abcdef.mp3",        false)] // GUID too short
    [TestCase("rooftop-job-a8f3/episode.mp3",                       false)] // legacy combined name
    [TestCase("rooftop-job-a8f3/audio/0123456789abcdef0123456789abcdef.flac", false)] // unsupported ext
    [TestCase("",                                                   false)]
    public void IsCanonical_RecognisesValidPaths(string path, bool expected)
    {
        Assert.That(AudioPath.IsCanonical(path), Is.EqualTo(expected));
    }

    [Test]
    public void TryParseBeat_ReturnsSlugBeatIdExt()
    {
        var parsed = AudioPath.TryParseBeat("the-job/audio/0123456789abcdef0123456789abcdef.mp3");
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Value.Slug, Is.EqualTo("the-job"));
        Assert.That(parsed.Value.BeatId, Is.EqualTo(Guid.ParseExact("0123456789abcdef0123456789abcdef", "N")));
        Assert.That(parsed.Value.Ext, Is.EqualTo("mp3"));
    }

    [Test]
    public void TryParseCombined_ReturnsSlugExt()
    {
        var parsed = AudioPath.TryParseCombined("the-job/node.wav");
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Value.Slug, Is.EqualTo("the-job"));
        Assert.That(parsed.Value.Ext,  Is.EqualTo("wav"));
    }
}

[TestFixture]
public class LocalDiskAudioStoreTests
{
    private string tempRoot = null!;
    private LocalDiskAudioStore store = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-audio-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        store = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public async Task WriteBeat_ReturnsCanonicalRelativePath()
    {
        var beatId = Guid.CreateVersion7();
        var rel = await store.WriteBeatAsync("test-slug", beatId, "mp3", [1, 2, 3, 4]);
        Assert.That(rel, Is.EqualTo($"test-slug/audio/{beatId:N}.mp3"));
    }

    [Test]
    public async Task RoundTrip_WriteThenReadProducesSameBytes()
    {
        var beatId = Guid.CreateVersion7();
        byte[] payload = [10, 20, 30, 40, 50, 60];
        var rel = await store.WriteBeatAsync("test-slug", beatId, "wav", payload);

        await using var s = await store.OpenReadAsync(rel);
        Assert.That(s, Is.Not.Null);
        using var ms = new MemoryStream();
        await s!.CopyToAsync(ms);
        Assert.That(ms.ToArray(), Is.EqualTo(payload));
    }

    [Test]
    public async Task Exists_ReportsTrueAfterWrite_FalseAfterDelete()
    {
        var beatId = Guid.CreateVersion7();
        var rel = await store.WriteBeatAsync("test-slug", beatId, "mp3", [1, 2]);
        Assert.That(await store.ExistsAsync(rel), Is.True);
        await store.DeleteAsync(rel);
        Assert.That(await store.ExistsAsync(rel), Is.False);
    }

    [Test]
    public async Task ResolveLocalPath_ReturnsAbsolutePath_WhenFileExists()
    {
        var beatId = Guid.CreateVersion7();
        var rel = await store.WriteBeatAsync("test-slug", beatId, "mp3", [1]);
        var local = await store.ResolveLocalPathAsync(rel);
        Assert.That(local, Is.Not.Null);
        Assert.That(File.Exists(local!), Is.True);
    }

    [Test]
    public async Task GetLastModified_ReportsRecentTimestamp_AfterWrite()
    {
        var beatId = Guid.CreateVersion7();
        var rel = await store.WriteBeatAsync("test-slug", beatId, "mp3", [1]);
        var ts = await store.GetLastModifiedAsync(rel);
        Assert.That(ts, Is.Not.Null);
        Assert.That((DateTimeOffset.UtcNow - ts!.Value).Duration(), Is.LessThan(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task OpenRead_ReturnsNull_WhenFileMissing()
    {
        var rel = $"never-written/audio/{Guid.CreateVersion7():N}.mp3";
        await using var s = await store.OpenReadAsync(rel);
        Assert.That(s, Is.Null);
    }

    [Test]
    public async Task WriteCombined_LandsAtNodeRoot()
    {
        var rel = await store.WriteCombinedAsync("my-node", "wav", [7, 7, 7]);
        Assert.That(rel, Is.EqualTo("my-node/node.wav"));
        Assert.That(await store.ExistsAsync(rel), Is.True);
    }
}

[TestFixture]
public class DualWriteAudioStoreTests
{
    private string tempRootA = null!, tempRootB = null!;
    private LocalDiskAudioStore primary = null!, secondary = null!;
    private DualWriteAudioStore dual = null!;

    [SetUp]
    public void SetUp()
    {
        tempRootA = Path.Combine(Path.GetTempPath(), "ss-dual-a-" + Guid.NewGuid().ToString("N"));
        tempRootB = Path.Combine(Path.GetTempPath(), "ss-dual-b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRootA);
        Directory.CreateDirectory(tempRootB);
        primary   = new LocalDiskAudioStore(new TestPathProviderWithRoot(tempRootA), NullLogger<LocalDiskAudioStore>.Instance);
        secondary = new LocalDiskAudioStore(new TestPathProviderWithRoot(tempRootB), NullLogger<LocalDiskAudioStore>.Instance);
        dual      = new DualWriteAudioStore(primary, secondary, NullLogger<DualWriteAudioStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRootA, recursive: true); } catch { }
        try { Directory.Delete(tempRootB, recursive: true); } catch { }
    }

    [Test]
    public async Task WriteBeat_FansOutToBothStores()
    {
        var beatId = Guid.CreateVersion7();
        var rel = await dual.WriteBeatAsync("dual-test", beatId, "mp3", [1, 2, 3]);

        // Primary write is synchronous; secondary is fire-and-forget.
        // Poll briefly so the background upload has a chance to land.
        await PollUntil(async () => await primary.ExistsAsync(rel) && await secondary.ExistsAsync(rel));
        Assert.That(await primary.ExistsAsync(rel),   Is.True, "primary should have the file");
        Assert.That(await secondary.ExistsAsync(rel), Is.True, "secondary should have the file");
    }

    [Test]
    public async Task OpenRead_FallsBackToSecondary_WhenPrimaryMisses()
    {
        var beatId = Guid.CreateVersion7();
        var rel = $"dual-test/audio/{beatId:N}.mp3";
        // Seed secondary only (bypass dual fan-out by writing directly).
        await secondary.WriteBeatAsync("dual-test", beatId, "mp3", [9, 9, 9]);

        await using var s = await dual.OpenReadAsync(rel);
        Assert.That(s, Is.Not.Null);
    }

    [Test]
    public async Task OpenRead_CachesBackToPrimary_WhenEnabled()
    {
        var beatId = Guid.CreateVersion7();
        var rel = $"cache-test/audio/{beatId:N}.mp3";
        await secondary.WriteBeatAsync("cache-test", beatId, "mp3", [5, 5, 5]);

        // First read pulls from secondary; cache-back to primary fires fire-and-forget.
        await using (var first = await dual.OpenReadAsync(rel)) { _ = first; }
        await PollUntil(async () => await primary.ExistsAsync(rel));
        Assert.That(await primary.ExistsAsync(rel), Is.True);
    }

    [Test]
    public async Task GetLastModified_ReturnsNewerOfTheTwo()
    {
        var beatId = Guid.CreateVersion7();
        var rel = $"ts-test/audio/{beatId:N}.mp3";
        await primary.WriteBeatAsync("ts-test", beatId, "mp3", [1]);
        // Make secondary newer by writing 200ms later.
        await Task.Delay(200);
        await secondary.WriteBeatAsync("ts-test", beatId, "mp3", [2]);

        var aggregate = await dual.GetLastModifiedAsync(rel);
        var primaryTs   = await primary.GetLastModifiedAsync(rel);
        var secondaryTs = await secondary.GetLastModifiedAsync(rel);
        Assert.That(aggregate, Is.EqualTo(secondaryTs));
        Assert.That(aggregate, Is.GreaterThanOrEqualTo(primaryTs!.Value));
    }

    [Test]
    public void UnderlyingStores_ReturnsConstructorPair()
    {
        var (p, s) = dual.UnderlyingStores;
        Assert.That(p, Is.SameAs(primary));
        Assert.That(s, Is.SameAs(secondary));
    }

    /// <summary>Spin briefly until <paramref name="check"/> returns true, or
    /// hit the timeout. Used in tests that depend on fire-and-forget
    /// background uploads — we don't want to await Task.Delay(big number)
    /// in the happy path but need to give the Task.Run a moment to run.</summary>
    private static async Task PollUntil(Func<Task<bool>> check, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (await check()) return;
            await Task.Delay(25);
        }
    }
}
