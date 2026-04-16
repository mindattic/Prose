using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class PasswordResetServiceTests
{
    private PasswordResetService svc = null!;

    [SetUp]
    public void SetUp() => svc = new PasswordResetService();

    // ── Code generation ───────────────────────────────────────────────────────

    [Test]
    public void GenerateCode_Returns6DigitString()
    {
        var code = svc.GenerateCode("user1");
        Assert.That(code, Has.Length.EqualTo(6));
        Assert.That(code, Does.Match(@"^\d{6}$"));
    }

    [Test]
    public void GenerateCode_TwoCallsProduceDifferentCodes()
    {
        var codes = Enumerable.Range(0, 20).Select(_ => svc.GenerateCode("u1")).ToHashSet();
        Assert.That(codes.Count, Is.GreaterThan(1));
    }

    [Test]
    public void GenerateCode_SetsHasPendingCodeTrue()
    {
        svc.GenerateCode("user1");
        Assert.That(svc.HasPendingCode("user1"), Is.True);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Test]
    public void ValidateCode_CorrectCode_ReturnsTrue()
    {
        var code = svc.GenerateCode("user1");
        Assert.That(svc.ValidateCode("user1", code), Is.True);
    }

    [Test]
    public void ValidateCode_WrongCode_ReturnsFalse()
    {
        svc.GenerateCode("user1");
        Assert.That(svc.ValidateCode("user1", "000000"), Is.False);
    }

    [Test]
    public void ValidateCode_NoPendingCode_ReturnsFalse()
    {
        Assert.That(svc.ValidateCode("ghost", "123456"), Is.False);
    }

    [Test]
    public void ValidateCode_IsOneTimeUse_SecondCallReturnsFalse()
    {
        var code = svc.GenerateCode("user1");
        svc.ValidateCode("user1", code);
        Assert.That(svc.ValidateCode("user1", code), Is.False);
    }

    [Test]
    public void ValidateCode_SuccessRemovesPendingCode()
    {
        var code = svc.GenerateCode("user1");
        svc.ValidateCode("user1", code);
        Assert.That(svc.HasPendingCode("user1"), Is.False);
    }

    [Test]
    public void ValidateCode_TrimsWhitespace()
    {
        var code = svc.GenerateCode("user1");
        Assert.That(svc.ValidateCode("user1", $"  {code}  "), Is.True);
    }

    [Test]
    public void ValidateCode_WrongUserForCode_ReturnsFalse()
    {
        var code = svc.GenerateCode("user1");
        Assert.That(svc.ValidateCode("user2", code), Is.False);
    }

    // ── HasPendingCode ────────────────────────────────────────────────────────

    [Test]
    public void HasPendingCode_WithNoPendingCode_ReturnsFalse()
    {
        Assert.That(svc.HasPendingCode("user1"), Is.False);
    }

    [Test]
    public void HasPendingCode_AfterCancel_ReturnsFalse()
    {
        svc.GenerateCode("user1");
        svc.CancelCode("user1");
        Assert.That(svc.HasPendingCode("user1"), Is.False);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Test]
    public void CancelCode_PreventsValidation()
    {
        var code = svc.GenerateCode("user1");
        svc.CancelCode("user1");
        Assert.That(svc.ValidateCode("user1", code), Is.False);
    }

    [Test]
    public void CancelCode_NonexistentUser_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => svc.CancelCode("nobody"));
    }

    // ── Overwrite ────────────────────────────────────────────────────────────

    [Test]
    public void GenerateCode_OverwritesExistingCode()
    {
        var first = svc.GenerateCode("user1");
        var second = svc.GenerateCode("user1");
        // Old code must not validate — only the new one should
        Assert.That(svc.ValidateCode("user1", first), Is.False);
        Assert.That(svc.ValidateCode("user1", second), Is.True);
    }

    // ── Thread safety ─────────────────────────────────────────────────────────

    [Test]
    public void ConcurrentGenerate_DoesNotThrow()
    {
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(() => svc.GenerateCode($"user{i % 5}")))
            .ToArray();
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }
}
