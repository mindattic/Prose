using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Validates the per-action voter-count + tier registry — defaults seeded on
/// first read, tier-locked actions resist downgrade, new defaults backfill.
/// </summary>
[TestFixture]
public class ActionConfigServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private SettingsKvStore kv = null!;
    private ActionConfigService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-action-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        kv = new SettingsKvStore(TestDbFactory.For(paths, "action-config"));
        svc = new ActionConfigService(kv, NullLogger<ActionConfigService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public void ListAll_FirstRead_SeedsDefaults()
    {
        var all = svc.ListAll();
        Assert.That(all, Is.Not.Empty, "Expected default actions to be seeded on first read");
        Assert.That(all.Any(a => a.Action == ActionConfigService.ActionIds.ChapterBeatWriter),
            "ChapterBeatWriter must be in the default seed");
        Assert.That(all.Any(a => a.Action == ActionConfigService.ActionIds.ChapterBeatVoter),
            "ChapterBeatVoter must be in the default seed");
    }

    [Test]
    public void Get_ChapterBeatWriter_IsHighTierAndLocked()
    {
        var writer = svc.Get(ActionConfigService.ActionIds.ChapterBeatWriter);
        Assert.That(writer.Tier, Is.EqualTo("High"), "Writing actions must default to HIGH tier");
        Assert.That(writer.LockTier, Is.True, "Writing actions must be tier-locked so settings can't downgrade");
        Assert.That(writer.VoterCount, Is.EqualTo(10), "Default writer panel is 10 voters");
    }

    [Test]
    public void Get_ChapterBeatVoter_IsLowTierAndUnlocked()
    {
        var voter = svc.Get(ActionConfigService.ActionIds.ChapterBeatVoter);
        Assert.That(voter.Tier, Is.EqualTo("Low"), "Voting actions default to LOW tier (haiku-class)");
        Assert.That(voter.LockTier, Is.False, "Voting actions are user-adjustable");
        Assert.That(voter.VoterCount, Is.EqualTo(100), "Default voter panel is 100");
    }

    [Test]
    public void Save_LockedAction_RejectsTierDowngrade()
    {
        // ChapterBeatWriter is tier-locked HIGH. Try to downgrade to LOW.
        var writer = svc.Get(ActionConfigService.ActionIds.ChapterBeatWriter);
        writer.Tier = "Low";
        writer.VoterCount = 5;
        svc.Save(writer);

        var reloaded = svc.Get(ActionConfigService.ActionIds.ChapterBeatWriter);
        Assert.That(reloaded.Tier, Is.EqualTo("High"), "Locked tier must NOT be downgraded by Save");
        Assert.That(reloaded.VoterCount, Is.EqualTo(5), "VoterCount is editable even on locked actions");
    }

    [Test]
    public void Save_UnlockedAction_AllowsTierChange()
    {
        var voter = svc.Get(ActionConfigService.ActionIds.ChapterBeatVoter);
        voter.Tier = "Medium";
        voter.VoterCount = 50;
        svc.Save(voter);

        var reloaded = svc.Get(ActionConfigService.ActionIds.ChapterBeatVoter);
        Assert.That(reloaded.Tier, Is.EqualTo("Medium"));
        Assert.That(reloaded.VoterCount, Is.EqualTo(50));
    }

    [Test]
    public void GetVoterCount_ReturnsSavedValue()
    {
        var voter = svc.Get(ActionConfigService.ActionIds.ChapterBeatVoter);
        voter.VoterCount = 25;
        svc.Save(voter);

        Assert.That(svc.GetVoterCount(ActionConfigService.ActionIds.ChapterBeatVoter), Is.EqualTo(25));
    }

    [Test]
    public void ParseTier_KnownValues_ResolvesCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ActionConfigService.ParseTier("Low"),     Is.EqualTo(ActionConfigService.ModelTierLite.Low));
            Assert.That(ActionConfigService.ParseTier("medium"),  Is.EqualTo(ActionConfigService.ModelTierLite.Medium));
            Assert.That(ActionConfigService.ParseTier("HIGH"),    Is.EqualTo(ActionConfigService.ModelTierLite.High));
            Assert.That(ActionConfigService.ParseTier("Higher"),  Is.EqualTo(ActionConfigService.ModelTierLite.Higher));
            Assert.That(ActionConfigService.ParseTier("highest"), Is.EqualTo(ActionConfigService.ModelTierLite.Highest));
        });
    }

    [Test]
    public void ParseTier_Unknown_DefaultsToMedium()
    {
        Assert.That(ActionConfigService.ParseTier("garbage"), Is.EqualTo(ActionConfigService.ModelTierLite.Medium));
        Assert.That(ActionConfigService.ParseTier(""),        Is.EqualTo(ActionConfigService.ModelTierLite.Medium));
        Assert.That(ActionConfigService.ParseTier(null!),     Is.EqualTo(ActionConfigService.ModelTierLite.Medium));
    }
}
