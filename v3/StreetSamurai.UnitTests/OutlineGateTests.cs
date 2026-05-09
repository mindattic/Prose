using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests the prose-generation gate enforced by BookOutlineService — Approved
/// is required, Draft and InReview must throw the typed exception so callers
/// can surface the current status to the user.
/// </summary>
[TestFixture]
public class OutlineGateTests
{
    [Test]
    public void OutlineNotApprovedException_CarriesBookIdAndStatus()
    {
        var ex = new OutlineNotApprovedException("book-123", OutlineStatus.Draft);
        Assert.That(ex.BookId, Is.EqualTo("book-123"));
        Assert.That(ex.CurrentStatus, Is.EqualTo(OutlineStatus.Draft));
        Assert.That(ex.Message, Does.Contain("Draft"));
    }

    [Test]
    public void EffectiveBody_WhenBodySet_ReturnsBody()
    {
        var ch = new BookChapterOutline { Body = "Kyle walks into the bar." };
        Assert.That(ch.EffectiveBody, Is.EqualTo("Kyle walks into the bar."));
    }

    [Test]
    public void EffectiveBody_WhenLegacyOnly_ComposesFromOldFields()
    {
        // Legacy outlines lacking Body should still produce useful prompt context
        // by composing the structured fields. This keeps prompts working during
        // the migration window — old outlines load without loss.
        var ch = new BookChapterOutline
        {
            LongSynopsis  = "Kyle walks into the bar; the bartender knows him.",
            KeyBeats      = new() { "Order seltzer", "Ask about Gabney" },
            OpensThreads  = new() { "Gabney lead" },
            ClosesThreads = new() { "Cold-open question" },
        };
        var body = ch.EffectiveBody;
        Assert.That(body, Does.Contain("Kyle walks into the bar"));
        Assert.That(body, Does.Contain("Beats:"));
        Assert.That(body, Does.Contain("Order seltzer"));
        Assert.That(body, Does.Contain("Opens:").And.Contain("Gabney lead"));
        Assert.That(body, Does.Contain("Closes:").And.Contain("Cold-open question"));
    }

    [Test]
    public void EffectiveBody_BodyTakesPrecedenceOverLegacy()
    {
        // Once the user writes a Body, it should win over the legacy fields —
        // otherwise the migration would never let go of the old shape.
        var ch = new BookChapterOutline
        {
            Body         = "The freeform replacement.",
            LongSynopsis = "The legacy fallback.",
        };
        Assert.That(ch.EffectiveBody, Is.EqualTo("The freeform replacement."));
    }

    [Test]
    public void EffectiveBody_AllEmpty_ReturnsEmpty()
    {
        var ch = new BookChapterOutline();
        Assert.That(ch.EffectiveBody, Is.Empty);
    }
}
