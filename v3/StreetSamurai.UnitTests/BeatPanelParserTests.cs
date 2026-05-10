using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Pure-parser tests for the LLM-output JSON shapes that drive
/// BeatGeneratorService and BookOutlineService. No LLM calls, no DB —
/// just the contract: given a JSON-string blob from a voter, parse it
/// into typed records without throwing on the messy real-world inputs
/// LLMs actually produce (preamble prose before the JSON, slightly
/// malformed entries, unexpected types, etc.).
/// </summary>
[TestFixture]
public class BeatPanelParserTests
{
    // ── BeatGeneratorService.ParseRankPayload ──────────────────────────

    [Test]
    public void ParseRankPayload_StrictArray_ReturnsAllEntries()
    {
        var json = @"[{""id"": 1, ""score"": 78}, {""id"": 2, ""score"": 42}, {""id"": 3, ""score"": 91}]";
        var hits = BeatGeneratorService.ParseRankPayload(json).ToList();
        Assert.That(hits, Has.Count.EqualTo(3));
        Assert.That(hits[0], Is.EqualTo((1, 78.0)));
        Assert.That(hits[2], Is.EqualTo((3, 91.0)));
    }

    [Test]
    public void ParseRankPayload_PreambleProse_StillExtractsArray()
    {
        // LLMs often add explanatory prose before/after JSON. The parser
        // should locate the [ ... ] block and ignore the wrap.
        var raw = "Here are my scores:\n\n[{\"id\":1,\"score\":50}]\n\n— that's my read.";
        var hits = BeatGeneratorService.ParseRankPayload(raw).ToList();
        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0], Is.EqualTo((1, 50.0)));
    }

    [Test]
    public void ParseRankPayload_Empty_ReturnsEmpty()
    {
        Assert.That(BeatGeneratorService.ParseRankPayload("").ToList(), Is.Empty);
        Assert.That(BeatGeneratorService.ParseRankPayload("nothing here").ToList(), Is.Empty);
    }

    [Test]
    public void ParseRankPayload_Malformed_DoesNotThrow()
    {
        // Trailing-comma is invalid JSON — System.Text.Json rejects it. Parser
        // catches the exception and returns no entries rather than blowing up
        // the whole 100-voter aggregation.
        Assert.That(BeatGeneratorService.ParseRankPayload("[{\"id\":1,").ToList(), Is.Empty);
    }

    [Test]
    public void ParseRankPayload_MissingFields_SkipsEntry()
    {
        // Each entry needs both id (int) and score (double). Missing either
        // causes that entry to be skipped — others keep coming.
        var json = @"[{""id"":1,""score"":80}, {""id"":2}, {""score"":50}, {""id"":3,""score"":70}]";
        var hits = BeatGeneratorService.ParseRankPayload(json).ToList();
        Assert.That(hits, Has.Count.EqualTo(2));
        Assert.That(hits[0].id, Is.EqualTo(1));
        Assert.That(hits[1].id, Is.EqualTo(3));
    }

    [Test]
    public void ParseRankPayload_FloatScores_PreservesDecimals()
    {
        var json = @"[{""id"": 1, ""score"": 78.5}, {""id"": 2, ""score"": 42.25}]";
        var hits = BeatGeneratorService.ParseRankPayload(json).ToList();
        Assert.That(hits[0].score, Is.EqualTo(78.5));
        Assert.That(hits[1].score, Is.EqualTo(42.25));
    }

    [Test]
    public void ParseRankPayload_NotAnArray_ReturnsEmpty()
    {
        // Object root instead of array — no entries.
        Assert.That(BeatGeneratorService.ParseRankPayload(@"{""id"":1,""score"":50}").ToList(), Is.Empty);
    }

    // ── BeatGeneratorService.ParseOocFindings ──────────────────────────

    [Test]
    public void ParseOocFindings_Strict_ReturnsTypedRecords()
    {
        var json = @"[
          {""field"":""speech_patterns.under_pressure"",""detected"":""falls into clipped sentences"",
           ""canon_value"":""warm and meandering"",""suggestion"":""tighten prose""},
          {""field"":""psychology.coping_mechanisms"",""detected"":""talks to weapons"",
           ""canon_value"":"""",""suggestion"":""add to canon""}
        ]";
        var findings = BeatGeneratorService.ParseOocFindings(json);
        Assert.That(findings, Has.Count.EqualTo(2));
        Assert.That(findings[0].Field, Is.EqualTo("speech_patterns.under_pressure"));
        Assert.That(findings[0].Detected, Is.EqualTo("falls into clipped sentences"));
        Assert.That(findings[0].CanonValue, Is.EqualTo("warm and meandering"));
        Assert.That(findings[0].Suggestion, Is.EqualTo("tighten prose"));
        Assert.That(findings[1].CanonValue, Is.Empty);
    }

    [Test]
    public void ParseOocFindings_EmptyArray_ReturnsEmpty()
    {
        Assert.That(BeatGeneratorService.ParseOocFindings("[]"), Is.Empty);
    }

    [Test]
    public void ParseOocFindings_PreambleProse_StillExtracts()
    {
        var raw = "I see one drift:\n\n[{\"field\":\"voice\",\"detected\":\"new tic\",\"canon_value\":\"\",\"suggestion\":\"add to canon\"}]";
        var findings = BeatGeneratorService.ParseOocFindings(raw);
        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Field, Is.EqualTo("voice"));
    }

    [Test]
    public void ParseOocFindings_Malformed_DoesNotThrow()
    {
        Assert.That(BeatGeneratorService.ParseOocFindings("not even close to JSON"), Is.Empty);
        Assert.That(BeatGeneratorService.ParseOocFindings(""), Is.Empty);
    }

    [Test]
    public void ParseOocFindings_MissingFields_FillsWithEmptyStrings()
    {
        // Field not present → empty string. The OOC card UI handles empty
        // CanonValue as "no canon to compare against" — that path needs the
        // empty-string default to render correctly.
        var findings = BeatGeneratorService.ParseOocFindings(@"[{""field"":""voice""}]");
        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].Field, Is.EqualTo("voice"));
        Assert.That(findings[0].Detected, Is.Empty);
        Assert.That(findings[0].CanonValue, Is.Empty);
        Assert.That(findings[0].Suggestion, Is.Empty);
    }

    // ── BookOutlineService.ParseDriftFindings ──────────────────────────

    [Test]
    public void ParseDriftFindings_AllKinds_AreSurfaced()
    {
        var json = @"[
          {""kind"":""missing"",""summary"":""beat A not delivered"",
           ""outline_says"":""Kyle reaches the bar"",""prose_says"":""(no scene)""},
          {""kind"":""contradiction"",""summary"":""POV mismatch"",
           ""outline_says"":""Kyle POV"",""prose_says"":""Sasha POV""},
          {""kind"":""extra"",""summary"":""new sub-beat introduced"",
           ""outline_says"":"""",""prose_says"":""Auntie Hoa appears""}
        ]";
        var drifts = BookOutlineService.ParseDriftFindings(json);
        Assert.That(drifts, Has.Count.EqualTo(3));
        Assert.That(drifts.Select(d => d.Kind), Is.EquivalentTo(new[] { "missing", "contradiction", "extra" }));
    }

    [Test]
    public void ParseDriftFindings_Empty_ReturnsEmpty()
    {
        Assert.That(BookOutlineService.ParseDriftFindings("[]"), Is.Empty);
        Assert.That(BookOutlineService.ParseDriftFindings(null), Is.Empty);
        Assert.That(BookOutlineService.ParseDriftFindings("nothing"), Is.Empty);
    }

    [Test]
    public void ParseDriftFindings_PreambleProse_StillExtracts()
    {
        var raw = "Here's what I found:\n[{\"kind\":\"missing\",\"summary\":\"X\",\"outline_says\":\"Y\",\"prose_says\":\"Z\"}]";
        var drifts = BookOutlineService.ParseDriftFindings(raw);
        Assert.That(drifts, Has.Count.EqualTo(1));
        Assert.That(drifts[0].Summary, Is.EqualTo("X"));
    }

    [Test]
    public void ParseDriftFindings_PartialFields_FillsWithEmptyStrings()
    {
        var drifts = BookOutlineService.ParseDriftFindings(@"[{""kind"":""missing""}]");
        Assert.That(drifts, Has.Count.EqualTo(1));
        Assert.That(drifts[0].Summary,     Is.Empty);
        Assert.That(drifts[0].OutlineSays, Is.Empty);
        Assert.That(drifts[0].ProseSays,   Is.Empty);
    }

    [Test]
    public void ParseDriftFindings_Malformed_DoesNotThrow()
    {
        Assert.That(BookOutlineService.ParseDriftFindings("[{\"kind\":"), Is.Empty);
    }
}
