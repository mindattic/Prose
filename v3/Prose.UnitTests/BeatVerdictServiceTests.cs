using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Tests for BeatVerdictService.ParseVerdictBatch — the per-batch line-editor verdict parser.
/// Extracted from RunAsync into its own internal static method (was inlined) specifically so
/// this logic is directly testable; InternalsVisibleTo already covers this project.
///
/// Found and fixed a real bug while adding this coverage (5th instance of the same class this
/// session — LogicSweepService, ChekhovAuditService, EmotionalDepthService, BookOutlineService):
/// JsonElement.GetInt32() throws on a non-Number "ref" (e.g. a hallucinated null), and the loop
/// had no per-entry guard or try/catch — one malformed "beats" entry would discard every finding
/// for the WHOLE 4-beat batch (BatchSize = 4), silently under-reporting real defects.
/// </summary>
[TestFixture]
public class BeatVerdictServiceTests
{
    private static Dictionary<int, (Guid Id, int Number, string Chapter)> MakeRefMap(int count)
    {
        var map = new Dictionary<int, (Guid, int, string)>();
        for (int i = 0; i < count; i++)
            map[i] = (Guid.NewGuid(), i + 1, "Chapter One");
        return map;
    }

    [Test]
    public void ParseVerdictBatch_ValidResponse_MapsRefToRealBeat()
    {
        var refMap = MakeRefMap(2);
        var raw = """{"beats":[{"ref":0,"findings":[{"type":"CLICHE","severity":"MINOR","quote":"time slowed","note":"stock phrasing","fix":null}]}]}""";

        var findings = BeatVerdictService.ParseVerdictBatch(raw, refMap);

        Assert.That(findings, Has.Count.EqualTo(1));
        Assert.That(findings[0].BeatId, Is.EqualTo(refMap[0].Id));
        Assert.That(findings[0].Number, Is.EqualTo(refMap[0].Number));
        Assert.That(findings[0].Type, Is.EqualTo("CLICHE"));
        Assert.That(findings[0].Quote, Is.EqualTo("time slowed"));
    }

    [Test]
    public void ParseVerdictBatch_NoBeatsProperty_ReturnsEmpty()
    {
        var findings = BeatVerdictService.ParseVerdictBatch("{}", MakeRefMap(2));
        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ParseVerdictBatch_EmptyFindingsArray_ReturnsEmpty()
    {
        var raw = """{"beats":[{"ref":0,"findings":[]}]}""";
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(2));
        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ParseVerdictBatch_RefNotInMap_SkipsThatEntry()
    {
        var raw = """{"beats":[{"ref":99,"findings":[{"type":"GRIPE","severity":"MODERATE","note":"x"}]}]}""";
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(2));
        Assert.That(findings, Is.Empty);
    }

    [Test]
    public void ParseVerdictBatch_MissingOptionalFields_DefaultsApplied()
    {
        var raw = """{"beats":[{"ref":0,"findings":[{"note":"weak line"}]}]}""";
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(1));

        Assert.That(findings[0].Type, Is.EqualTo("GRIPE"));
        Assert.That(findings[0].Severity, Is.EqualTo("MINOR"));
        Assert.That(findings[0].Quote, Is.Null);
    }

    [Test]
    public void ParseVerdictBatch_MultipleFindingsSameBeat_AllReturned()
    {
        var raw = """
            {"beats":[{"ref":0,"findings":[
                {"type":"CLICHE","severity":"MINOR","note":"a"},
                {"type":"GRIPE","severity":"MODERATE","note":"b"}
            ]}]}
            """;
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(1));
        Assert.That(findings, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParseVerdictBatch_MultipleBeatsInBatch_AllParsed()
    {
        var raw = """
            {"beats":[
                {"ref":0,"findings":[{"type":"CLICHE","severity":"MINOR","note":"a"}]},
                {"ref":1,"findings":[{"type":"GRIPE","severity":"MODERATE","note":"b"}]}
            ]}
            """;
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(2));
        Assert.That(findings, Has.Count.EqualTo(2));
    }

    [Test]
    public void ParseVerdictBatch_MarkdownFenced_StripsFenceBeforeParsing()
    {
        var raw = "```json\n{\"beats\":[{\"ref\":0,\"findings\":[{\"type\":\"GRIPE\",\"severity\":\"MINOR\",\"note\":\"x\"}]}]}\n```";
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(1));
        Assert.That(findings, Has.Count.EqualTo(1));
    }

    // ── Regression: a hallucinated null/non-numeric "ref" on one entry must not zero out the batch ──

    [Test]
    public void ParseVerdictBatch_NullRefOnOneEntry_OtherBeatsFindingsStillParsed()
    {
        var raw = """
            {"beats":[
                {"ref":null,"findings":[{"type":"CLICHE","severity":"MINOR","note":"broken entry"}]},
                {"ref":1,"findings":[{"type":"GRIPE","severity":"MODERATE","note":"good entry"}]}
            ]}
            """;
        var findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(2));

        Assert.That(findings.Any(f => f.Note == "good entry"), Is.True,
            "one malformed entry (null ref) must not discard every other beat's findings in the batch");
    }

    [Test]
    public void ParseVerdictBatch_NullRef_DoesNotThrow()
    {
        var raw = """{"beats":[{"ref":null,"findings":[{"type":"GRIPE","severity":"MINOR","note":"x"}]}]}""";
        List<VerdictFinding> findings = null!;

        Assert.DoesNotThrow(() => findings = BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(1)));
        Assert.That(findings, Is.Empty, "an unresolvable ref is skipped, not defaulted");
    }

    [Test]
    public void ParseVerdictBatch_StringRef_DoesNotThrow()
    {
        var raw = """{"beats":[{"ref":"0","findings":[{"type":"GRIPE","severity":"MINOR","note":"x"}]}]}""";
        Assert.DoesNotThrow(() => BeatVerdictService.ParseVerdictBatch(raw, MakeRefMap(1)));
    }
}
