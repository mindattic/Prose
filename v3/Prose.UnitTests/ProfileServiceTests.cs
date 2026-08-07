using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class ProfileServiceTests
{
    private string tempDir = null!;
    private ProfileService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_profile_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "engine_data"));
        svc = new ProfileService(new TestPathProviderWithRoot(tempDir));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── GetAvatar ─────────────────────────────────────────────────────────────

    [Test]
    public void GetAvatar_NoAvatarSaved_ReturnsNull()
    {
        Assert.That(svc.GetAvatar("alice"), Is.Null);
    }

    [Test]
    public void GetAvatar_EmptyUsername_ReturnsNull()
    {
        Assert.That(svc.GetAvatar(""), Is.Null);
        Assert.That(svc.GetAvatar("   "), Is.Null);
    }

    // ── SaveAvatar / GetAvatar round-trip ─────────────────────────────────────

    [Test]
    public void SaveAndGet_RoundTrip()
    {
        svc.SaveAvatar("alice", "data:image/png;base64,abc123");
        Assert.That(svc.GetAvatar("alice"), Is.EqualTo("data:image/png;base64,abc123"));
    }

    [Test]
    public void SaveAvatar_OverwritesExistingAvatar()
    {
        svc.SaveAvatar("alice", "data:image/png;base64,first");
        svc.SaveAvatar("alice", "data:image/png;base64,second");
        Assert.That(svc.GetAvatar("alice"), Is.EqualTo("data:image/png;base64,second"));
    }

    [Test]
    public void SaveAvatar_DifferentUsers_AreIndependent()
    {
        svc.SaveAvatar("alice", "data:image/png;base64,alice-avatar");
        svc.SaveAvatar("bob", "data:image/png;base64,bob-avatar");
        Assert.That(svc.GetAvatar("alice"), Is.EqualTo("data:image/png;base64,alice-avatar"));
        Assert.That(svc.GetAvatar("bob"), Is.EqualTo("data:image/png;base64,bob-avatar"));
    }

    // ── Username sanitization ─────────────────────────────────────────────────

    [Test]
    public void SaveAvatar_UsernameWithSpecialChars_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => svc.SaveAvatar("alice@domain.com", "data:image/png;base64,x"));
    }

    [Test]
    public void SaveAvatar_UsernameWithSpecialChars_CanBeRetrieved()
    {
        svc.SaveAvatar("alice@domain.com", "data:image/png;base64,avatar");
        Assert.That(svc.GetAvatar("alice@domain.com"), Is.EqualTo("data:image/png;base64,avatar"));
    }

    [Test]
    public void SaveAvatar_EmptyUsername_DoesNotCreateFile()
    {
        svc.SaveAvatar("", "data:image/png;base64,x");
        // Should silently no-op
        Assert.That(svc.GetAvatar(""), Is.Null);
    }

    // ── AvatarUpdated event ───────────────────────────────────────────────────

    [Test]
    public void SaveAvatar_FiresAvatarUpdatedEvent()
    {
        var fired = false;
        svc.AvatarUpdated += () => fired = true;
        svc.SaveAvatar("alice", "data:image/png;base64,x");
        Assert.That(fired, Is.True);
    }

    [Test]
    public void SaveAvatar_FiredForEachSave()
    {
        var count = 0;
        svc.AvatarUpdated += () => count++;
        svc.SaveAvatar("alice", "data:image/png;base64,a");
        svc.SaveAvatar("alice", "data:image/png;base64,b");
        Assert.That(count, Is.EqualTo(2));
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    [Test]
    public void Avatar_PersistedToDisk_SurvivesNewInstance()
    {
        svc.SaveAvatar("alice", "data:image/png;base64,persisted");

        var svc2 = new ProfileService(new TestPathProviderWithRoot(tempDir));
        Assert.That(svc2.GetAvatar("alice"), Is.EqualTo("data:image/png;base64,persisted"));
    }
}
