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

    // ── DeriveProgressiveChecks: early_peak (2026-08-09 fix) ────────────────────────

    static StoryScopeAuditService.BeatReading Reading(int index, int stakes) =>
        new() { Index = index, Stakes = stakes };

    [Test]
    public void EarlyPeak_TiedWithTrueClimax_DoesNotFire()
    {
        // 10 beats, climax zone starts at index 6 (60%). An early beat (index 1) ties the
        // max stakes value with the true climax (index 8) — before the fix, IndexOf found
        // the FIRST occurrence (index 1) and treated it as "the" peak, incorrectly reporting
        // the story de-escalates into its ending even though the climax IS correctly placed.
        var readings = new List<StoryScopeAuditService.BeatReading>
        {
            Reading(0, 3), Reading(1, 9), Reading(2, 4), Reading(3, 5), Reading(4, 6),
            Reading(5, 5), Reading(6, 6), Reading(7, 7), Reading(8, 9), Reading(9, 8),
        };

        var checks = StoryScopeAuditService.DeriveProgressiveChecks(readings, readings.Count);
        var earlyPeak = checks.Single(c => c.Key == "early_peak");

        Assert.That(earlyPeak.Severity, Is.EqualTo("PASS"),
            "the climax zone DOES reach the max stakes value (beat 8) — this must not be flagged " +
            "just because an earlier beat also happened to tie that value");
    }

    [Test]
    public void EarlyPeak_OnlyEarlyBeatReachesMax_StillFires()
    {
        // Same shape, but nothing in the climax zone (index >= 6) reaches the peak value 9 —
        // genuine early-peak de-escalation must still be caught.
        var readings = new List<StoryScopeAuditService.BeatReading>
        {
            Reading(0, 3), Reading(1, 9), Reading(2, 4), Reading(3, 5), Reading(4, 6),
            Reading(5, 5), Reading(6, 6), Reading(7, 7), Reading(8, 8), Reading(9, 7),
        };

        var checks = StoryScopeAuditService.DeriveProgressiveChecks(readings, readings.Count);
        var earlyPeak = checks.Single(c => c.Key == "early_peak");

        Assert.That(earlyPeak.Severity, Is.EqualTo("MODERATE"),
            "no beat in the climax zone reaches the overall peak stakes — this IS a genuine early-peak defect");
    }

    // ── ComputeSubplotGap: backs the positional subplot check (docs/LOGIC.md §10, Rowling grid) ──

    [Test]
    public void ComputeSubplotGap_SingleCarrierEarly_TrailingGapIsTheMax()
    {
        // Only one carrier appearance, at unit 0 of 20 — the thread is touched once and never
        // again, so the trailing gap (19) is what must be reported, not zero.
        var (maxGap, threshold) = StoryScopeAuditService.ComputeSubplotGap([0], 20);
        Assert.That(maxGap, Is.EqualTo(19));
        Assert.That(threshold, Is.EqualTo(5)); // floor: 20 * 0.20 = 4, floor is 5
    }

    [Test]
    public void ComputeSubplotGap_EvenlySpreadCarriers_PassesUnderThreshold()
    {
        // 20 units, carriers every 4 units — well under the proportional threshold (4).
        var (maxGap, threshold) = StoryScopeAuditService.ComputeSubplotGap([0, 4, 8, 12, 16, 19], 20);
        Assert.That(maxGap, Is.EqualTo(4));
        Assert.That(threshold, Is.EqualTo(5));
    }

    [Test]
    public void ComputeSubplotGap_LongQuietStretch_ExceedsThreshold()
    {
        // 20 units: carriers at 0 and 1, then nothing until the end — an 18-unit trailing gap
        // is well past the 5-unit floor threshold.
        var (maxGap, threshold) = StoryScopeAuditService.ComputeSubplotGap([0, 1], 20);
        Assert.That(maxGap, Is.EqualTo(18));
        Assert.That(maxGap, Is.GreaterThan(threshold));
    }

    [Test]
    public void ComputeSubplotGap_ShortBook_FloorPreventsFalsePositive()
    {
        // A 6-unit novella with one carrier near the start: the proportional threshold (6 * 0.20
        // = 1, rounded down) would be absurdly strict without the floor — the floor (5) keeps a
        // short book from flagging on one unavoidable gap.
        var (maxGap, threshold) = StoryScopeAuditService.ComputeSubplotGap([1], 6);
        Assert.That(threshold, Is.EqualTo(5));
        Assert.That(maxGap, Is.EqualTo(4)); // trailing gap: (6-1)-1 = 4, under the floor
    }
}
