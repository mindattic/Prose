using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests for MeaningBackfillService.ParseMeaningBatch — the batch response parser for backfilling
/// the MEANING coordinate (Beat.Description). Extracted from BackfillAsync into its own internal
/// static method (was inlined) specifically so this logic is directly testable; InternalsVisibleTo
/// already covers this project.
///
/// Found and fixed a real bug while adding this coverage (6th instance of the same class this
/// session — LogicSweepService, ChekhovAuditService, EmotionalDepthService, BookOutlineService,
/// BeatVerdictService): JsonElement.GetInt32() throws on a non-Number "ref" (e.g. a hallucinated
/// null), and the loop had no per-entry guard — one malformed "items" entry discarded every
/// meaning in the WHOLE batch (up to 10 beats, BatchSize = 10). Directly relevant to the
/// already-known MEANING=0% coordination-board finding (this service is the tool meant to fix
/// exactly that gap) — a batch failing silently on one bad ref is a plausible contributor.
/// </summary>
[TestFixture]
public class MeaningBackfillServiceTests
{
    private static Dictionary<int, Guid> MakeRefMap(int count)
    {
        var map = new Dictionary<int, Guid>();
        for (int i = 0; i < count; i++) map[i] = Guid.NewGuid();
        return map;
    }

    [Test]
    public void ParseMeaningBatch_ValidResponse_MapsRefToRealBeat()
    {
        var refMap = MakeRefMap(1);
        var raw = """{"items":[{"ref":0,"meaning":"Establishes the stakes for the heist."}]}""";

        var results = MeaningBackfillService.ParseMeaningBatch(raw, refMap);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].BeatId, Is.EqualTo(refMap[0]));
        Assert.That(results[0].Meaning, Is.EqualTo("Establishes the stakes for the heist."));
    }

    [Test]
    public void ParseMeaningBatch_NoItemsProperty_ReturnsEmpty()
    {
        var results = MeaningBackfillService.ParseMeaningBatch("{}", MakeRefMap(1));
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseMeaningBatch_EmptyOrWhitespaceMeaning_EntryIsSkipped()
    {
        var raw = """{"items":[{"ref":0,"meaning":"   "}]}""";
        var results = MeaningBackfillService.ParseMeaningBatch(raw, MakeRefMap(1));
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseMeaningBatch_MeaningIsTrimmed()
    {
        var raw = """{"items":[{"ref":0,"meaning":"  Pays off the earlier plant.  "}]}""";
        var results = MeaningBackfillService.ParseMeaningBatch(raw, MakeRefMap(1));
        Assert.That(results[0].Meaning, Is.EqualTo("Pays off the earlier plant."));
    }

    [Test]
    public void ParseMeaningBatch_RefNotInMap_SkipsThatEntry()
    {
        var raw = """{"items":[{"ref":99,"meaning":"orphaned ref"}]}""";
        var results = MeaningBackfillService.ParseMeaningBatch(raw, MakeRefMap(1));
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void ParseMeaningBatch_MultipleItems_AllParsed()
    {
        var refMap = MakeRefMap(3);
        var raw = """
            {"items":[
                {"ref":0,"meaning":"first"},
                {"ref":1,"meaning":"second"},
                {"ref":2,"meaning":"third"}
            ]}
            """;
        var results = MeaningBackfillService.ParseMeaningBatch(raw, refMap);
        Assert.That(results, Has.Count.EqualTo(3));
    }

    [Test]
    public void ParseMeaningBatch_MalformedJson_ReturnsEmptyInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            var results = MeaningBackfillService.ParseMeaningBatch("{\"items\": oops}", MakeRefMap(1));
            Assert.That(results, Is.Empty);
        });
    }

    [Test]
    public void ParseMeaningBatch_MarkdownFenced_StripsFenceBeforeParsing()
    {
        var raw = "```json\n{\"items\":[{\"ref\":0,\"meaning\":\"fenced meaning\"}]}\n```";
        var results = MeaningBackfillService.ParseMeaningBatch(raw, MakeRefMap(1));
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Meaning, Is.EqualTo("fenced meaning"));
    }

    // ── Regression: a hallucinated null/non-numeric "ref" on one entry must not zero out the batch ──

    [Test]
    public void ParseMeaningBatch_NullRefOnOneEntry_OtherMeaningsStillParsed()
    {
        var refMap = MakeRefMap(2);
        var raw = """
            {"items":[
                {"ref":null,"meaning":"broken entry"},
                {"ref":1,"meaning":"good entry"}
            ]}
            """;
        var results = MeaningBackfillService.ParseMeaningBatch(raw, refMap);

        Assert.That(results.Any(r => r.Meaning == "good entry"), Is.True,
            "one malformed entry (null ref) must not discard every other beat's meaning in the batch");
    }

    [Test]
    public void ParseMeaningBatch_NullRef_DoesNotThrow()
    {
        var raw = """{"items":[{"ref":null,"meaning":"x"}]}""";
        List<(Guid, string)> results = null!;

        Assert.DoesNotThrow(() => results = MeaningBackfillService.ParseMeaningBatch(raw, MakeRefMap(1)));
        Assert.That(results, Is.Empty, "an unresolvable ref is skipped, not defaulted");
    }

    [Test]
    public void ParseMeaningBatch_StringRef_DoesNotThrow()
    {
        var raw = """{"items":[{"ref":"0","meaning":"x"}]}""";
        Assert.DoesNotThrow(() => MeaningBackfillService.ParseMeaningBatch(raw, MakeRefMap(1)));
    }

    [Test]
    public void ParseMeaningBatch_TenBeatBatchWithOneBadEntry_NineMeaningsStillFill()
    {
        // Mirrors the real BatchSize (10) — one bad ref must not wipe out the other 9.
        var refMap = MakeRefMap(10);
        var items = new List<string> { """{"ref":0,"meaning":null}""" }; // this one is the "bad" entry (null meaning)
        for (int i = 1; i < 10; i++) items.Add($$"""{"ref":{{i}},"meaning":"meaning {{i}}"}""");
        var raw = "{\"items\":[" + string.Join(",", items) + "]}";

        var results = MeaningBackfillService.ParseMeaningBatch(raw, refMap);

        Assert.That(results, Has.Count.EqualTo(9));
    }
}
