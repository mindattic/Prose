using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for ChekhovAuditService.ParseSightings — the untrusted-LLM-JSON parser for pass 1
/// (Chekhov's Gun prop extraction). Made internal (was private); InternalsVisibleTo already
/// covers this project.
///
/// Found and fixed a real bug while adding this coverage (same class as the LogicSweepService
/// null-beat_number bug fixed earlier this session): JsonElement.GetDouble() throws
/// InvalidOperationException on a non-Number "sort_key" (e.g. a hallucinated null), and the try/
/// catch wrapped the WHOLE sightings list — one malformed sighting discarded every real sighting
/// extracted from the same LLM response. Fixed by guarding ValueKind and isolating each entry's
/// parsing so one bad sighting can't take out its siblings.
/// </summary>
[TestFixture]
public class ChekhovAuditServiceTests
{
    private static readonly List<NodeWorkbenchService.OrderedBeat> NoBeats = [];

    private static NodeWorkbenchService.OrderedBeat MakeBeat(double sortKey) =>
        new(new Beat { Id = Guid.NewGuid() }, Guid.NewGuid(), sortKey);

    [Test]
    public void ParseSightings_ValidJson_ParsesAllFields()
    {
        var raw = """
            {"sightings":[{"beat_label":"Beat 1","sort_key":100.0,"prop_name":"tin of matches","prop_type":"physical","context":"found in the drawer"}]}
            """;
        var results = ChekhovAuditService.ParseSightings(raw, NoBeats);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].PropName, Is.EqualTo("tin of matches"));
        Assert.That(results[0].PropType, Is.EqualTo("physical"));
        Assert.That(results[0].Context, Is.EqualTo("found in the drawer"));
    }

    [Test]
    public void ParseSightings_NoBraces_ReturnsEmpty()
    {
        var results = ChekhovAuditService.ParseSightings("I found no props.", NoBeats);
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseSightings_MissingPropName_EntryIsDropped()
    {
        var raw = """{"sightings":[{"beat_label":"Beat 1","sort_key":1.0,"prop_type":"physical","context":"x"}]}""";
        var results = ChekhovAuditService.ParseSightings(raw, NoBeats);
        Assert.That(results, Is.Empty, "a sighting with no prop_name is not a real sighting");
    }

    [Test]
    public void ParseSightings_MissingPropType_DefaultsToPhysical()
    {
        var raw = """{"sightings":[{"beat_label":"Beat 1","sort_key":1.0,"prop_name":"lantern","context":"lit"}]}""";
        var results = ChekhovAuditService.ParseSightings(raw, NoBeats);
        Assert.That(results[0].PropType, Is.EqualTo("physical"));
    }

    [Test]
    public void ParseSightings_BeatLabelMatchesRealBeat_UsesRealSortKeyNotLlmEcho()
    {
        // BUG FIX documented in source: the LLM's echoed sort_key can drift on a long pass —
        // when the beat_label resolves to a real beat, its actual SortKey wins over the echo.
        var beats = new List<NodeWorkbenchService.OrderedBeat> { MakeBeat(250.0) };
        var raw = """{"sightings":[{"beat_label":"Beat 1","sort_key":999.0,"prop_name":"lantern","context":"lit"}]}""";

        var results = ChekhovAuditService.ParseSightings(raw, beats);

        Assert.That(results[0].SortKey, Is.EqualTo(250.0f), "real beat SortKey must win over the LLM's echoed value");
    }

    [Test]
    public void ParseSightings_BeatLabelDoesNotMatchAnyBeat_FallsBackToLlmSortKey()
    {
        var raw = """{"sightings":[{"beat_label":"Beat 47","sort_key":42.0,"prop_name":"lantern","context":"lit"}]}""";
        var results = ChekhovAuditService.ParseSightings(raw, NoBeats);

        Assert.That(results[0].SortKey, Is.EqualTo(42.0f));
    }

    // ── Regression: a hallucinated null sort_key must not discard the whole batch ──────

    [Test]
    public void ParseSightings_NullSortKeyOnOneEntry_OtherSightingsStillParsed()
    {
        var raw = """
            {"sightings":[
                {"beat_label":"Beat 1","sort_key":null,"prop_name":"broken entry","prop_type":"physical","context":"x"},
                {"beat_label":"Beat 2","sort_key":50.0,"prop_name":"good entry","prop_type":"physical","context":"y"}
            ]}
            """;
        var results = ChekhovAuditService.ParseSightings(raw, NoBeats);

        Assert.That(results.Select(r => r.PropName), Does.Contain("good entry"),
            "one malformed sighting (null sort_key) must not discard every other real sighting");
    }

    [Test]
    public void ParseSightings_NullSortKey_FallsBackToZeroNotThrow()
    {
        var raw = """{"sightings":[{"beat_label":"Beat 1","sort_key":null,"prop_name":"lantern","context":"lit"}]}""";
        List<ChekhovSighting> results = null!;

        Assert.DoesNotThrow(() => results = ChekhovAuditService.ParseSightings(raw, NoBeats));
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].SortKey, Is.EqualTo(0f));
    }

    [Test]
    public void ParseSightings_SortKeyAsString_DoesNotThrow()
    {
        // Another plausible hallucination shape — a stringified number instead of a JSON number.
        var raw = """{"sightings":[{"beat_label":"Beat 1","sort_key":"100","prop_name":"lantern","context":"lit"}]}""";
        List<ChekhovSighting> results = null!;

        Assert.DoesNotThrow(() => results = ChekhovAuditService.ParseSightings(raw, NoBeats));
        Assert.That(results, Has.Count.EqualTo(1));
    }

    [Test]
    public void ParseSightings_MalformedJson_ReturnsEmptyInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var results = ChekhovAuditService.ParseSightings("{\"sightings\": oops}", NoBeats);
            Assert.That(results, Is.Empty);
        });
    }

    [Test]
    public void ParseSightings_MultipleAppearancesSameProp_AllReturned()
    {
        var raw = """
            {"sightings":[
                {"beat_label":"Beat 1","sort_key":1.0,"prop_name":"tin of matches","prop_type":"physical","context":"setup"},
                {"beat_label":"Beat 5","sort_key":5.0,"prop_name":"tin of matches","prop_type":"physical","context":"consequence"}
            ]}
            """;
        var results = ChekhovAuditService.ParseSightings(raw, NoBeats);
        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.All(r => r.PropName == "tin of matches"), Is.True);
    }
}
