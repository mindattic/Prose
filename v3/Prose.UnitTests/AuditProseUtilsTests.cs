using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// Tests for AuditProseUtils.ClampProse — the head+tail prose truncation shared by every audit
/// that hands a whole node's concatenated prose to a single LLM call (BookAuditService,
/// StoryScopeAuditService, LogicSweepService, CraftRuleAuditService). Previously four identical
/// private copies of this method, each with its own duplicate test coverage; consolidated to one
/// shared implementation (<c>AuditModels.cs</c>) with one test file.
/// </summary>
[TestFixture]
public class AuditProseUtilsTests
{
    [Test]
    public void ClampProse_ShortText_ReturnedUnchanged()
    {
        var text = new string('x', 500);
        Assert.That(AuditProseUtils.ClampProse(text), Is.EqualTo(text));
    }

    [Test]
    public void ClampProse_ExactlyAtLimit_ReturnedUnchanged()
    {
        var text = new string('x', 100000);
        Assert.That(AuditProseUtils.ClampProse(text), Is.EqualTo(text));
    }

    [Test]
    public void ClampProse_OverLimit_KeepsHeadAndTail()
    {
        var head = new string('a', 50000);
        var tail = new string('b', 50000);
        var middle = new string('m', 10000);
        var text = head + middle + tail;

        var clamped = AuditProseUtils.ClampProse(text);

        Assert.That(clamped, Does.StartWith(head));
        Assert.That(clamped, Does.EndWith(tail));
        Assert.That(clamped, Does.Contain("elided for length"));
        Assert.That(clamped, Does.Not.Contain(middle));
    }
}
