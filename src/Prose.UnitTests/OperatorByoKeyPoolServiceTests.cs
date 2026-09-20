using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Operator;

namespace Prose.UnitTests;

/// <summary>
/// Covers <see cref="OperatorByoKeyPoolService"/>'s CRUD surface over the operator BYO-key pool
/// (backing both <c>prose --set-byo-key</c> and the <c>OperatorKeyTools</c> MCP tools) and the
/// back-compat resolution in <see cref="OperatorByoKeys"/> — a key saved before the pool existed
/// (the legacy singular field) must still resolve after this upgrade.
/// </summary>
[TestFixture]
public class OperatorByoKeyPoolServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private SettingsKvStore kv = null!;
    private OperatorByoKeyPoolService pool = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-byo-key-pool-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        kv = new SettingsKvStore(TestDbFactory.For(paths, "byo-key-pool"));
        pool = new OperatorByoKeyPoolService(kv);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    [Test]
    public void GetPool_NothingConfigured_ReturnsEmpty()
    {
        Assert.That(pool.GetPool("claude"), Is.Empty);
    }

    [Test]
    public void SetPool_ThenGetPool_RoundTrips_InOrder()
    {
        pool.SetPool("claude", new[] { "key-1", "key-2", "key-3" });
        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "key-1", "key-2", "key-3" }));
    }

    [Test]
    public void SetPool_DropsBlankEntries()
    {
        pool.SetPool("openai", new[] { "key-1", "  ", "", "key-2" });
        Assert.That(pool.GetPool("openai"), Is.EqualTo(new[] { "key-1", "key-2" }));
    }

    [Test]
    public void AddKey_AppendsToEnd_AndSkipsExactDuplicate()
    {
        pool.AddKey("claude", "key-1");
        pool.AddKey("claude", "key-2");
        pool.AddKey("claude", "key-1"); // duplicate, no-op

        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "key-1", "key-2" }));
    }

    [Test]
    public void RemoveKey_RemovesExactMatch_ReturnsTrue()
    {
        pool.SetPool("claude", new[] { "key-1", "key-2" });

        var removed = pool.RemoveKey("claude", "key-1");

        Assert.That(removed, Is.True);
        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "key-2" }));
    }

    [Test]
    public void RemoveKey_NotPresent_ReturnsFalse_LeavesPoolUnchanged()
    {
        pool.SetPool("claude", new[] { "key-1" });

        var removed = pool.RemoveKey("claude", "does-not-exist");

        Assert.That(removed, Is.False);
        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "key-1" }));
    }

    [Test]
    public void Clear_EmptiesThePool()
    {
        pool.SetPool("claude", new[] { "key-1", "key-2" });
        pool.Clear("claude");
        Assert.That(pool.GetPool("claude"), Is.Empty);
    }

    [Test]
    public void Providers_AreIndependent()
    {
        pool.SetPool("claude", new[] { "claude-key" });
        pool.SetPool("openai", new[] { "openai-key" });

        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "claude-key" }));
        Assert.That(pool.GetPool("openai"), Is.EqualTo(new[] { "openai-key" }));
    }

    [Test]
    public void SetPool_UnknownProvider_Throws()
    {
        Assert.Throws<ArgumentException>(() => pool.SetPool("gemini", new[] { "key" }));
    }

    [Test]
    public void LegacySingleKeyField_StillResolves_UntilThePoolIsWritten()
    {
        // Simulates data saved by the pre-pool `prose --set-byo-key --key <k>` (which used to
        // write only the singular AnthropicApiKey field) — must still resolve after this upgrade.
        kv.Set("operator.byokeys", new OperatorByoKeys(AnthropicApiKey: "legacy-key"));

        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "legacy-key" }));

        // Writing a pool supersedes the legacy field rather than being shadowed by it.
        pool.SetPool("claude", new[] { "new-key-1", "new-key-2" });
        Assert.That(pool.GetPool("claude"), Is.EqualTo(new[] { "new-key-1", "new-key-2" }));
    }
}
