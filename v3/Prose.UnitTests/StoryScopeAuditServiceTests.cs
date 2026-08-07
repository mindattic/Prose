using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for StoryScopeAuditService's deterministic, LLM-free helpers — the only parts of this
/// 871-line service testable without mocking an LLM call. <c>LongestRun</c> and
/// <c>ParseJson&lt;T&gt;</c> are made <c>internal</c> (were <c>private</c>) specifically so
/// this real logic is exercised, not just the record/enum shapes. <c>ParseJson</c> in particular
/// is the same class of untrusted-LLM-JSON parser as SwainAuditService.ParseClassification (see
/// SwainAuditServiceTests) and backs every one of the service's LLM-graded checks — a bug here
/// would silently corrupt all of them. (Prose truncation is now the shared
/// <c>AuditProseUtils.ClampProse</c> — see <c>AuditProseUtilsTests.cs</c>.)
/// </summary>
[TestFixture]
public class StoryScopeAuditServiceTests
{
    // ── LongestRun: backs beat-mode monoculture + emotional-depth plateau checks ────

    [Test]
    public void LongestRun_EmptyList_ReturnsZeroLength()
    {
        var (value, length, start) = StoryScopeAuditService.LongestRun([]);
        Assert.That(value, Is.Null);
        Assert.That(length, Is.EqualTo(0));
        Assert.That(start, Is.EqualTo(0));
    }

    [Test]
    public void LongestRun_SingleElement_ReturnsLengthOne()
    {
        var (value, length, start) = StoryScopeAuditService.LongestRun(["Combat"]);
        Assert.That(value, Is.EqualTo("Combat"));
        Assert.That(length, Is.EqualTo(1));
        Assert.That(start, Is.EqualTo(0));
    }

    [Test]
    public void LongestRun_AllSameValue_ReturnsFullLength()
    {
        var (value, length, start) = StoryScopeAuditService.LongestRun(["Combat", "Combat", "Combat"]);
        Assert.That(value, Is.EqualTo("Combat"));
        Assert.That(length, Is.EqualTo(3));
        Assert.That(start, Is.EqualTo(0));
    }

    [Test]
    public void LongestRun_AllDistinctValues_ReturnsLengthOne()
    {
        var (value, length, start) = StoryScopeAuditService.LongestRun(["Combat", "Dialogue", "Transition"]);
        Assert.That(length, Is.EqualTo(1));
        Assert.That(value, Is.EqualTo("Combat")); // first run encountered, ties go to the earliest
        Assert.That(start, Is.EqualTo(0));
    }

    [Test]
    public void LongestRun_RunInMiddle_ReportsCorrectStartIndex()
    {
        // index:        0        1        2       3       4        5
        var values = new List<string> { "Combat", "Combat", "Dialogue", "Dialogue", "Dialogue", "Transition" };
        var (value, length, start) = StoryScopeAuditService.LongestRun(values);
        Assert.That(value, Is.EqualTo("Dialogue"));
        Assert.That(length, Is.EqualTo(3));
        Assert.That(start, Is.EqualTo(2));
    }

    [Test]
    public void LongestRun_RunAtEnd_IsDetected()
    {
        var values = new List<string> { "A", "B", "C", "C", "C", "C" };
        var (value, length, start) = StoryScopeAuditService.LongestRun(values);
        Assert.That(value, Is.EqualTo("C"));
        Assert.That(length, Is.EqualTo(4));
        Assert.That(start, Is.EqualTo(2));
    }

    [Test]
    public void LongestRun_TiedRunLengths_ReturnsEarlierRun()
    {
        var values = new List<string> { "A", "A", "B", "B" };
        var (value, length, start) = StoryScopeAuditService.LongestRun(values);
        Assert.That(value, Is.EqualTo("A"));
        Assert.That(length, Is.EqualTo(2));
        Assert.That(start, Is.EqualTo(0));
    }

    // ── ParseJson<T>: the untrusted-LLM-JSON parser shared by every LLM-graded check ──

    [Test]
    public void ParseJson_PlainJson_Deserializes()
    {
        var raw = """{"index":3,"stakes":7,"eventType":"reveal","revelationMode":"dramatic-irony"}""";
        var result = StoryScopeAuditService.ParseJson<StoryScopeAuditService.BeatReading>(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Index, Is.EqualTo(3));
        Assert.That(result.Stakes, Is.EqualTo(7));
        Assert.That(result.EventType, Is.EqualTo("reveal"));
    }

    [Test]
    public void ParseJson_ChatterAroundJson_ExtractsInnerObject()
    {
        var raw = "Here's my analysis:\n{\"index\":1,\"stakes\":5,\"eventType\":\"twist\",\"revelationMode\":null}\nLet me know if you need more.";
        var result = StoryScopeAuditService.ParseJson<StoryScopeAuditService.BeatReading>(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Stakes, Is.EqualTo(5));
        Assert.That(result.EventType, Is.EqualTo("twist"));
    }

    [Test]
    public void ParseJson_NoBraces_ReturnsDefault()
    {
        var result = StoryScopeAuditService.ParseJson<StoryScopeAuditService.BeatReading>("I refuse to answer in JSON.");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseJson_MalformedJson_ReturnsDefaultInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var result = StoryScopeAuditService.ParseJson<StoryScopeAuditService.BeatReading>("{\"index\": oops}");
            Assert.That(result, Is.Null);
        });
    }

    [Test]
    public void ParseJson_EmptyString_ReturnsDefault()
    {
        var result = StoryScopeAuditService.ParseJson<StoryScopeAuditService.BeatReading>("");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseJson_PropertyNamesAreCaseInsensitive()
    {
        var raw = """{"INDEX":2,"STAKES":8,"EVENTTYPE":"ambush","REVELATIONMODE":"dramatic-irony"}""";
        var result = StoryScopeAuditService.ParseJson<StoryScopeAuditService.BeatReading>(raw);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Index, Is.EqualTo(2));
        Assert.That(result.EventType, Is.EqualTo("ambush"));
    }
}
