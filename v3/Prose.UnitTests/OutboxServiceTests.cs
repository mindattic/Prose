using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Locks in the contract of <see cref="OutboxService"/> (RFC 0007 §5): enqueue, drain-marks-
/// delivered, peek-does-not, and per-consumer isolation.
/// </summary>
[TestFixture]
public class OutboxServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private OutboxService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-outbox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "outbox");
        svc = new OutboxService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Test]
    public async Task DrainAsync_ReturnsEnqueuedEvent_AndMarksDelivered()
    {
        await svc.EnqueueAsync("eve", "hello", "EVE universe live in Prose.");

        var first = await svc.DrainAsync("eve");
        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].Summary, Is.EqualTo("EVE universe live in Prose."));

        var second = await svc.DrainAsync("eve");
        Assert.That(second, Is.Empty, "a normal (non-peek) drain must mark events delivered");
    }

    [Test]
    public async Task DrainAsync_Peek_DoesNotMarkDelivered()
    {
        await svc.EnqueueAsync("eve", "hello", "peek me");

        var peeked = await svc.DrainAsync("eve", peek: true);
        Assert.That(peeked, Has.Count.EqualTo(1));

        var drained = await svc.DrainAsync("eve");
        Assert.That(drained, Has.Count.EqualTo(1), "peek must not consume the event");
    }

    [Test]
    public async Task DrainAsync_IsolatedPerConsumer()
    {
        await svc.EnqueueAsync("eve", "hello", "for eve");
        await svc.EnqueueAsync("other-app", "hello", "for other-app");

        var eveEvents = await svc.DrainAsync("eve");
        Assert.That(eveEvents, Has.Count.EqualTo(1));
        Assert.That(eveEvents[0].Summary, Is.EqualTo("for eve"));

        var otherEvents = await svc.DrainAsync("other-app");
        Assert.That(otherEvents, Has.Count.EqualTo(1));
        Assert.That(otherEvents[0].Summary, Is.EqualTo("for other-app"));
    }
}
