using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// A JSON value of the wrong kind (a quoted id, a numeric canon_value) used to THROW out of
/// BeatGeneratorService's parsers — JsonElement.TryGetInt32/GetString are not fail-soft on a
/// mismatched ValueKind — and discard every other entry in the same reply. Each such entry must
/// be skipped (rank) or read as empty (OOC), never escape.
/// </summary>
[TestFixture]
public class BeatPanelParserNonStringValueTests
{
    [Test]
    public void ParseRankPayload_QuotedId_SkipsThatEntryOnly()
    {
        var json = @"[{""id"":""1"",""score"":80}, {""id"":2,""score"":""70""}, {""id"":3,""score"":60}]";
        var hits = BeatGeneratorService.ParseRankPayload(json).ToList();
        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0], Is.EqualTo((3, 60.0)));
    }

    [Test]
    public void ParseOocFindings_NumericValue_ReadsAsEmptyAndKeepsOthers()
    {
        var json = @"[{""field"":""age"",""detected"":""looks older"",""canon_value"":34,""suggestion"":""tighten prose""},
                      {""field"":""voice"",""detected"":""new tic"",""canon_value"":"""",""suggestion"":""add to canon""}]";
        var findings = BeatGeneratorService.ParseOocFindings(json);
        Assert.That(findings, Has.Count.EqualTo(2));
        Assert.That(findings[0].CanonValue, Is.Empty);
        Assert.That(findings[0].Detected, Is.EqualTo("looks older"));
        Assert.That(findings[1].Field, Is.EqualTo("voice"));
    }
}
