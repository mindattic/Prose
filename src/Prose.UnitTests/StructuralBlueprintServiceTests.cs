using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class StructuralBlueprintServiceTests
{
    private const string SampleResponse = """
        {
          "subplot": {
            "summary": "Mrs. Chen's shop lease dispute runs alongside the investigation.",
            "thematicParallel": "Both ask what you protect when protection fails.",
            "beatIndexes": [2, 5, 9]
          },
          "temporal": { "scheme": "frame", "anachronyPlan": "Beat 3 flashes back to the debt.", "cutBeatIndex": 3 },
          "resolution": { "mode": "unresolved", "note": "The report goes somewhere she cannot follow." },
          "moral": { "polarity": "ambivalent", "note": "Filing the report may doom the children faster." },
          "escalationCurve": [3, 4, 4, 5, 6, 7, 7, 8, 9, 10],
          "events": [
            { "beatIndex": 0, "eventType": "arrival", "revelationMode": "curiosity" },
            { "beatIndex": 1, "eventType": "discovery", "revelationMode": "none" }
          ],
          "formDevice": "document interleave",
          "ending": { "style": "avalanche", "noEpilogue": true, "note": "End on the folder closing." },
          "intertextualAnchors": [
            { "entityId": null, "name": "The Glass Hour", "entityType": "entertainment", "howReferenced": "plays in the lobby", "beatIndex": 4 }
          ]
        }
        """;

    [Test]
    public void ParseResponse_ParsesFullSample()
    {
        var parsed = StructuralBlueprintService.ParseResponse(SampleResponse, beatCount: 10);

        Assert.That(parsed.Subplot, Is.Not.Null);
        Assert.That(parsed.Subplot!.BeatIndexes, Is.EqualTo(new[] { 2, 5, 9 }));
        Assert.That(parsed.Temporal!.Scheme, Is.EqualTo("frame"));
        Assert.That(parsed.Temporal!.CutBeatIndex, Is.EqualTo(3));
        Assert.That(parsed.Resolution!.Mode, Is.EqualTo("unresolved"));
        Assert.That(parsed.Moral!.Polarity, Is.EqualTo("ambivalent"));
        Assert.That(parsed.EscalationCurve, Has.Count.EqualTo(10));
        Assert.That(parsed.Events, Has.Count.EqualTo(2));
        Assert.That(parsed.Events![0].RevelationMode, Is.EqualTo("curiosity"));
        Assert.That(parsed.FormDevice, Is.EqualTo("document interleave"));
        Assert.That(parsed.Ending!.Style, Is.EqualTo("avalanche"));
        Assert.That(parsed.Ending!.NoEpilogue, Is.True);
        Assert.That(parsed.IntertextualAnchors, Has.Count.EqualTo(1));
        Assert.That(parsed.IntertextualAnchors![0].Name, Is.EqualTo("The Glass Hour"));
    }

    [Test]
    public void ParseResponse_StripsMarkdownFence()
    {
        var fenced = "```json\n" + SampleResponse + "\n```";
        var parsed = StructuralBlueprintService.ParseResponse(fenced, beatCount: 10);
        Assert.That(parsed.Ending!.Style, Is.EqualTo("avalanche"));
    }

    [Test]
    public void ParseResponse_ClampsAndPadsEscalationCurve()
    {
        // Curve has 10 entries; a 6-beat story clamps, a 14-beat story pads with the last value.
        var clamped = StructuralBlueprintService.ParseResponse(SampleResponse, beatCount: 6);
        Assert.That(clamped.EscalationCurve, Has.Count.EqualTo(6));

        var padded = StructuralBlueprintService.ParseResponse(SampleResponse, beatCount: 14);
        Assert.That(padded.EscalationCurve, Has.Count.EqualTo(14));
        Assert.That(padded.EscalationCurve![^1], Is.EqualTo(10));
    }

    [Test]
    public void ParseResponse_ThrowsOnNonJson()
    {
        Assert.Throws<InvalidOperationException>(
            () => StructuralBlueprintService.ParseResponse("I couldn't decide on a structure.", beatCount: 10));
    }

    [Test]
    public void ParseResponse_ToleratesLeadingProse()
    {
        var noisy = "Here are the structural decisions:\n" + SampleResponse;
        var parsed = StructuralBlueprintService.ParseResponse(noisy, beatCount: 10);
        Assert.That(parsed.Resolution!.Mode, Is.EqualTo("unresolved"));
    }

    static List<NodeWorkbenchService.OrderedBeat> MakeBeats(params (Guid owner, int count)[] chapters)
    {
        var beats = new List<NodeWorkbenchService.OrderedBeat>();
        double key = 100;
        foreach (var (owner, count) in chapters)
            for (var i = 0; i < count; i++)
                beats.Add(new NodeWorkbenchService.OrderedBeat(
                    new Prose.Core.Data.Entities.Beat { Id = Guid.NewGuid() }, owner, key += 100));
        return beats;
    }

    [Test]
    public void GroupUnits_SmallStory_OneUnitPerBeat()
    {
        var ch = Guid.NewGuid();
        var (granularity, units) = StructuralBlueprintService.GroupUnits(MakeBeats((ch, 30)));
        Assert.That(granularity, Is.EqualTo("beat"));
        Assert.That(units, Has.Count.EqualTo(30));
        Assert.That(units[7].Index, Is.EqualTo(7));
    }

    [Test]
    public void GroupUnits_BookScale_OneUnitPerChapterRun()
    {
        var ch1 = Guid.NewGuid(); var ch2 = Guid.NewGuid(); var ch3 = Guid.NewGuid();
        var (granularity, units) = StructuralBlueprintService.GroupUnits(
            MakeBeats((ch1, 40), (ch2, 25), (ch3, 30)));
        Assert.That(granularity, Is.EqualTo("chapter"));
        Assert.That(units, Has.Count.EqualTo(3));
        Assert.That(units[1].Beats, Has.Count.EqualTo(25));
        Assert.That(units[1].OwnerNodeId, Is.EqualTo(ch2));
        Assert.That(units[2].Index, Is.EqualTo(2));
    }

    [Test]
    public void GroupUnits_ForceChapter_GroupsSmallStoryByOwner()
    {
        var ch1 = Guid.NewGuid(); var ch2 = Guid.NewGuid();
        var (granularity, units) = StructuralBlueprintService.GroupUnits(
            MakeBeats((ch1, 7), (ch2, 7)), forceChapter: true);
        Assert.That(granularity, Is.EqualTo("chapter"));
        Assert.That(units, Has.Count.EqualTo(2));
    }
}
