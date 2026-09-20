using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The dotted "node-guid.beat-guid" handle is the integration surface
/// between the writer UI's LLM bottom sheet, the MCP tool layer, the CLI,
/// and any chat-side client that wants to address one specific beat. Both
/// forms (Beat-only Guid and dotted) must parse cleanly; malformed input
/// must fail without throwing so the JSON error path in the MCP tools is
/// always taken.
/// </summary>
[TestFixture]
public class BeatHandleTests
{
    [Test]
    public void TryParse_PlainBeatGuid_SetsBeatId_AndNodeIsNull()
    {
        var bid = Guid.NewGuid();
        var ok = BeatHandle.TryParse(bid.ToString(), out var node, out var beat);
        Assert.That(ok, Is.True);
        Assert.That(node, Is.Null);
        Assert.That(beat, Is.EqualTo(bid));
    }

    [Test]
    public void TryParse_DottedHandle_SetsBothIds()
    {
        var s = Guid.NewGuid();
        var b = Guid.NewGuid();
        var ok = BeatHandle.TryParse($"{s}.{b}", out var node, out var beat);
        Assert.That(ok, Is.True);
        Assert.That(node, Is.EqualTo(s));
        Assert.That(beat, Is.EqualTo(b));
    }

    [Test]
    public void TryParse_TrimsWhitespace()
    {
        var bid = Guid.NewGuid();
        var ok = BeatHandle.TryParse($"  {bid}  \n", out _, out var beat);
        Assert.That(ok, Is.True);
        Assert.That(beat, Is.EqualTo(bid));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-guid")]
    [TestCase(".")]
    [TestCase("a.b")]
    public void TryParse_BadInput_ReturnsFalse_AndNullsOutParams(string? input)
    {
        var ok = BeatHandle.TryParse(input, out var node, out var beat);
        Assert.That(ok, Is.False);
        Assert.That(node, Is.Null);
        Assert.That(beat, Is.Null);
    }

    [Test]
    public void TryParse_DotAtEnd_IsRejected()
    {
        var bid = Guid.NewGuid();
        var ok = BeatHandle.TryParse($"{bid}.", out _, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryParse_DotAtStart_IsRejected()
    {
        var bid = Guid.NewGuid();
        var ok = BeatHandle.TryParse($".{bid}", out _, out _);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryParse_MalformedDottedForm_ReturnsFalse_DoesNotFallBackToBeatOnly()
    {
        // "guid.notaguid" must NOT silently treat the input as a beat-only
        // Guid by stripping the trailing junk. Reject the whole thing.
        var s = Guid.NewGuid();
        var ok = BeatHandle.TryParse($"{s}.not-a-guid", out var node, out var beat);
        Assert.That(ok, Is.False);
        Assert.That(node, Is.Null);
        Assert.That(beat, Is.Null);
    }

    [Test]
    public void Format_ProducesParseableHandle_RoundTrip()
    {
        var s = Guid.NewGuid();
        var b = Guid.NewGuid();
        var handle = BeatHandle.Format(s, b);
        Assert.That(handle, Is.EqualTo($"{s}.{b}"));

        var ok = BeatHandle.TryParse(handle, out var node, out var beat);
        Assert.That(ok, Is.True);
        Assert.That(node, Is.EqualTo(s));
        Assert.That(beat, Is.EqualTo(b));
    }
}
