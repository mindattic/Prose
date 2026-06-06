using System.Linq;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Unit contracts for the canon-grounded storytelling engine added 2026-06-06:
/// the universal canon retrieval, the voice-harvest codification, the cross-type
/// contradiction detector, coverage instrumentation — plus regression guards that
/// keep the retired facet system from creeping back in. These cover the pure,
/// deterministic logic (parsing/formatting/chunking/normalisation); the LLM- and
/// DB-bound paths are exercised by the CLIs (--harvest-voice / --check-canon /
/// --canon-retrieve / --coverage), verified against live data.
/// </summary>
[TestFixture]
public class CanonEngineTests
{
    // ── CanonRetrievalService.FirstSentence ──────────────────────────────────

    [Test]
    public void FirstSentence_EmptyOrNull_ReturnsEmpty()
    {
        Assert.That(CanonRetrievalService.FirstSentence(null), Is.EqualTo(""));
        Assert.That(CanonRetrievalService.FirstSentence("   "), Is.EqualTo(""));
    }

    [Test]
    public void FirstSentence_StopsAtFirstSentence()
    {
        var d = "A mono-edge carbon katana with no powers. It is just a very good sword that cuts.";
        Assert.That(CanonRetrievalService.FirstSentence(d), Is.EqualTo("A mono-edge carbon katana with no powers."));
    }

    [Test]
    public void FirstSentence_LongNoPeriod_ClipsWithEllipsis()
    {
        var d = new string('x', 400);
        var got = CanonRetrievalService.FirstSentence(d, max: 160);
        Assert.That(got.Length, Is.LessThanOrEqualTo(161));
        Assert.That(got, Does.EndWith("…"));
    }

    [Test]
    public void FirstSentence_FlattensNewlines()
    {
        Assert.That(CanonRetrievalService.FirstSentence("line one\nline two"), Does.Not.Contain("\n"));
    }

    // ── VoiceHarvestService helpers ──────────────────────────────────────────

    [Test]
    public void NormalizeTarget_KnownTarget_PassesThrough_CaseInsensitive()
    {
        Assert.That(VoiceHarvestService.NormalizeTarget("LITERARY_RULES.PROHIBITIONS"), Is.EqualTo("literary_rules.prohibitions"));
        Assert.That(VoiceHarvestService.NormalizeTarget("kyle.narration_voice"), Is.EqualTo("kyle.narration_voice"));
    }

    [Test]
    public void NormalizeTarget_UnknownOrEmpty_ReturnsNull()
    {
        Assert.That(VoiceHarvestService.NormalizeTarget("made_up.target"), Is.Null);
        Assert.That(VoiceHarvestService.NormalizeTarget(""), Is.Null);
        Assert.That(VoiceHarvestService.NormalizeTarget(null), Is.Null);
    }

    [Test]
    public void AddDistinct_SkipsCaseInsensitiveDuplicates()
    {
        var list = new List<string> { "No filler-wit." };
        VoiceHarvestService.AddDistinct(list, "no filler-wit.");   // dup (case)
        VoiceHarvestService.AddDistinct(list, "Gloss corpos on first mention."); // new
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExtractJsonArray_PullsArrayOutOfFencedReply()
    {
        var raw = "Sure! Here you go:\n```json\n[{\"a\":1}]\n```\nHope that helps.";
        Assert.That(VoiceHarvestService.ExtractJsonArray(raw), Is.EqualTo("[{\"a\":1}]"));
    }

    [Test]
    public void ExtractJsonArray_NoArray_ReturnsNull()
    {
        Assert.That(VoiceHarvestService.ExtractJsonArray("no json here"), Is.Null);
        Assert.That(VoiceHarvestService.ExtractJsonArray(""), Is.Null);
    }

    [Test]
    public void RuleTargets_AllMapToKnownStores()
    {
        // The targets the harvest can propose must each be a target ApplyAsync handles.
        Assert.That(VoiceHarvestService.RuleTargets, Is.Not.Empty);
        foreach (var t in VoiceHarvestService.RuleTargets)
            Assert.That(VoiceHarvestService.NormalizeTarget(t), Is.EqualTo(t), $"target '{t}' must normalise to itself");
    }

    // ── CanonContradictionService helpers ────────────────────────────────────

    [Test]
    public void Parse_ValidArray_MapsFields()
    {
        var raw = "[{\"entity\":\"Kyle\",\"issue\":\"katana described as glowing\",\"snippet\":\"the blade glowed\",\"suggested_fix\":\"Silence has no powers — remove the glow\",\"severity\":\"high\"}]";
        var got = CanonContradictionService.Parse(raw);
        Assert.That(got, Has.Count.EqualTo(1));
        Assert.That(got[0].Entity, Is.EqualTo("Kyle"));
        Assert.That(got[0].Severity, Is.EqualTo("high"));
        Assert.That(got[0].SuggestedFix, Does.Contain("no powers"));
    }

    [Test]
    public void Parse_EmptyArray_ReturnsEmpty()
    {
        Assert.That(CanonContradictionService.Parse("[]"), Is.Empty);
        Assert.That(CanonContradictionService.Parse("garbage"), Is.Empty);
    }

    [Test]
    public void Parse_ItemMissingIssue_IsSkipped()
    {
        var raw = "[{\"entity\":\"X\",\"severity\":\"low\"}]";   // no issue → not a contradiction
        Assert.That(CanonContradictionService.Parse(raw), Is.Empty);
    }

    [Test]
    public void ParseSeverity_MapsKnown_DefaultsToMedium()
    {
        Assert.That(CanonContradictionService.ParseSeverity("high"), Is.EqualTo(FindingSeverity.High));
        Assert.That(CanonContradictionService.ParseSeverity("LOW"), Is.EqualTo(FindingSeverity.Low));
        Assert.That(CanonContradictionService.ParseSeverity("weird"), Is.EqualTo(FindingSeverity.Medium));
    }

    [Test]
    public void Chunk_SplitsOnParagraphsUnderBudget_AndCoversAll()
    {
        var paras = Enumerable.Range(0, 10).Select(i => new string((char)('a' + i), 100));
        var text = string.Join("\n\n", paras);
        var chunks = CanonContradictionService.Chunk(text, 250).ToList();
        Assert.That(chunks.Count, Is.GreaterThan(1), "should split a long text");
        foreach (var c in chunks) Assert.That(c.Length, Is.LessThanOrEqualTo(250).Or.EqualTo(100));
        // No paragraph lost: every original paragraph appears in some chunk.
        foreach (var p in paras) Assert.That(chunks.Any(c => c.Contains(p)), Is.True);
    }

    [Test]
    public void Chunk_ShortText_SingleChunk()
    {
        Assert.That(CanonContradictionService.Chunk("one short paragraph", 6000).ToList(), Has.Count.EqualTo(1));
    }

    // ── CoverageService.TypeCoverage math ────────────────────────────────────

    [Test]
    public void TypeCoverage_ComputesPctAndMissing()
    {
        var c = new CoverageService.TypeCoverage("cyberware", Total: 200, Embedded: 200);
        Assert.That(c.EmbeddedPct, Is.EqualTo(100));
        Assert.That(c.Missing, Is.EqualTo(0));

        var partial = new CoverageService.TypeCoverage("place", Total: 600, Embedded: 591);
        Assert.That(partial.Missing, Is.EqualTo(9));
        Assert.That(partial.EmbeddedPct, Is.EqualTo(100.0 * 591 / 600).Within(0.001));
    }

    [Test]
    public void TypeCoverage_ZeroTotal_NoDivideByZero()
    {
        var c = new CoverageService.TypeCoverage("none", Total: 0, Embedded: 0);
        Assert.That(c.EmbeddedPct, Is.EqualTo(0));
    }

    // ── Facet-removal regression guards ──────────────────────────────────────

    [Test]
    public void Beat_HasNoFacetTag()
        => Assert.That(typeof(Beat).GetProperty("FacetTag"), Is.Null, "FacetTag was retired — must not return on Beat");

    [Test]
    public void OutlineBeat_HasNoFacetHint()
        => Assert.That(typeof(OutlineBeat).GetProperty("FacetHint"), Is.Null, "FacetHint was retired from the outline model");

    [Test]
    public void CoreAssembly_HasNoFacetTypes()
    {
        var facetTypes = typeof(VoiceHarvestService).Assembly.GetTypes()
            .Where(t => t.Name is "Facet" or "FacetData" or "FacetRules" or "FacetTrigger" or "FacetCoreMemory" or "FacetVoiceProhibition")
            .Select(t => t.Name)
            .ToList();
        Assert.That(facetTypes, Is.Empty, "retired facet types still present: " + string.Join(", ", facetTypes));
    }
}
