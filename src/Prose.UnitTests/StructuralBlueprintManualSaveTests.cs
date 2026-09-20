using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Covers <see cref="StructuralBlueprintService.SetManualAsync"/> — the hand-authored,
/// no-LLM-call save path added 2026-08-10 (commit 2358a56bf) so Stage 6 of the locked
/// New Story Workflow (CLAUDE.md) isn't blocked when the generation provider is
/// unavailable but the structural decisions have already been authored. Exercised live
/// against BTL during that session but never covered by an automated regression test —
/// this fixture closes that gap.
/// </summary>
[TestFixture]
public class StructuralBlueprintManualSaveTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private NodeWorkbenchService workbench = null!;
    private StructuralBlueprintService svc = null!;

    private const string SampleJson = """
        {
          "subplot": { "summary": "B-story summary.", "thematicParallel": "Echoes the A-plot.", "beatIndexes": [1, 3] },
          "temporal": { "scheme": "linear", "anachronyPlan": null, "cutBeatIndex": null },
          "resolution": { "mode": "mixed", "note": "External break, personal cost." },
          "moral": { "polarity": "ambivalent", "note": "No clean hero." },
          "escalationCurve": [2, 4, 6, 8, 10],
          "events": [
            { "beatIndex": 0, "eventType": "arrival", "revelationMode": "none" },
            { "beatIndex": 1, "eventType": "discovery", "revelationMode": "curiosity" },
            { "beatIndex": 2, "eventType": "betrayal", "revelationMode": "suspense" },
            { "beatIndex": 3, "eventType": "confrontation", "revelationMode": "none" },
            { "beatIndex": 4, "eventType": "loss", "revelationMode": "surprise" }
          ],
          "formDevice": null,
          "ending": { "style": "avalanche", "noEpilogue": true, "note": "No epilogue." },
          "intertextualAnchors": [
            { "entityId": null, "name": "The Glass Hour", "entityType": "entertainment", "howReferenced": "plays in the lobby", "beatIndex": 2 }
          ]
        }
        """;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-blueprint-manual-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "blueprint-manual");
        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        // SetManualAsync never touches llm or embeddings — only GenerateCoreAsync (the LLM
        // path) does — so both are safely null! here, same pattern as NodeWorkbenchServiceTests
        // passing null! for TTS in its CRUD-only fixture.
        svc = new StructuralBlueprintService(null!, dbFactory, workbench, null!, NullLogger<StructuralBlueprintService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Node> MakeNodeWithBeatsAsync(int beatCount)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = new BookNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "test-" + Guid.NewGuid().ToString("N")[..8],
            Title = "Test Node",
            Kind = "book",
            Status = "draft",
            SortKey = 100,
        };
        db.Nodes.Add(node);
        await db.SaveChangesAsync();

        Guid? after = null;
        for (var i = 0; i < beatCount; i++)
        {
            var beat = await workbench.InsertBeatAsync(node.Id, afterBeatId: after, $"Beat {i} text.");
            after = beat.Id;
        }
        return node;
    }

    [Test]
    public async Task SetManualAsync_SavesBlueprintFieldsFromJson()
    {
        var node = await MakeNodeWithBeatsAsync(5);

        var blueprint = await svc.SetManualAsync(node.Id, SampleJson);

        Assert.That(blueprint.HasSubplot, Is.True);
        Assert.That(blueprint.SubplotSummary, Is.EqualTo("B-story summary."));
        Assert.That(blueprint.TemporalScheme, Is.EqualTo("linear"));
        Assert.That(blueprint.ResolutionMode, Is.EqualTo("mixed"));
        Assert.That(blueprint.MoralPolarity, Is.EqualTo("ambivalent"));
        Assert.That(blueprint.EndingStyle, Is.EqualTo("avalanche"));
        Assert.That(blueprint.NoEpilogue, Is.True);
        Assert.That(blueprint.Granularity, Is.EqualTo("beat"));
        Assert.That(blueprint.GeneratedBy, Is.EqualTo("manual"),
            "manual saves must be distinguishable from llm/retrofit for honest provenance");
    }

    [Test]
    public async Task SetManualAsync_PersistsSubplotAndAnchorBeatTags()
    {
        var node = await MakeNodeWithBeatsAsync(5);
        await svc.SetManualAsync(node.Id, SampleJson);

        var saved = await svc.GetAsync(node.Id);
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.BeatTags.Count(t => t.TagType == "subplot"), Is.EqualTo(2),
            "beatIndexes [1,3] should each produce a subplot tag");
        Assert.That(saved.BeatTags.Any(t => t.TagType == "intertextual-touchpoint" && t.Note!.Contains("The Glass Hour")), Is.True);
    }

    [Test]
    public async Task SetManualAsync_PersistsPerBeatDecisions()
    {
        var node = await MakeNodeWithBeatsAsync(5);
        await svc.SetManualAsync(node.Id, SampleJson);

        await using var db = await dbFactory.CreateDbContextAsync();
        var beatIds = (await workbench.GetOrderedBeatsAsync(node.Id)).Select(b => b.Beat.Id).ToList();
        var decisions = await db.BeatBlueprintDecisions.Where(d => beatIds.Contains(d.BeatId)).ToListAsync();

        Assert.That(decisions, Has.Count.EqualTo(5));
        Assert.That(decisions.Count(d => d.SubplotCarrier), Is.EqualTo(2));
        Assert.That(decisions.Select(d => d.EscalationFloor).OrderBy(x => x),
            Is.EqualTo(new decimal?[] { 2, 4, 6, 8, 10 }));
    }

    [Test]
    public async Task SetManualAsync_ClampsEscalationCurveToActualBeatCount()
    {
        // SampleJson's curve has 5 entries; a 3-beat node must clamp, not overflow or throw.
        var node = await MakeNodeWithBeatsAsync(3);
        var blueprint = await svc.SetManualAsync(node.Id, SampleJson);

        var curve = System.Text.Json.JsonSerializer.Deserialize<List<int>>(blueprint.EscalationCurveJson);
        Assert.That(curve, Has.Count.EqualTo(3));
        Assert.That(curve, Is.EqualTo(new List<int> { 2, 4, 6 }));
    }

    [Test]
    public async Task SetManualAsync_PadsEscalationCurveWithLastValue_WhenNodeHasMoreBeats()
    {
        // SampleJson's curve has 5 entries; a 7-beat node must pad by repeating the last value.
        var node = await MakeNodeWithBeatsAsync(7);
        var blueprint = await svc.SetManualAsync(node.Id, SampleJson);

        var curve = System.Text.Json.JsonSerializer.Deserialize<List<int>>(blueprint.EscalationCurveJson);
        Assert.That(curve, Has.Count.EqualTo(7));
        Assert.That(curve![5], Is.EqualTo(10));
        Assert.That(curve[6], Is.EqualTo(10));
    }

    [Test]
    public void SetManualAsync_ThrowsWhenNodeHasNoBeats()
    {
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var node = await MakeNodeWithBeatsAsync(0);
            await svc.SetManualAsync(node.Id, SampleJson);
        });
    }

    [Test]
    public async Task SetManualAsync_ThrowsOnMalformedJson()
    {
        var node = await MakeNodeWithBeatsAsync(3);
        Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await svc.SetManualAsync(node.Id, "{ not json"));
    }

    [Test]
    public async Task SetManualAsync_ReplacesAnExistingBlueprint()
    {
        var node = await MakeNodeWithBeatsAsync(5);
        await svc.SetManualAsync(node.Id, SampleJson);

        var secondJson = SampleJson.Replace("\"mode\": \"mixed\"", "\"mode\": \"external\"");
        await svc.SetManualAsync(node.Id, secondJson);

        await using var db = await dbFactory.CreateDbContextAsync();
        var blueprints = await db.NodeStructuralBlueprints.Where(b => b.NodeId == node.Id).ToListAsync();
        Assert.That(blueprints, Has.Count.EqualTo(1), "a second save must replace, not duplicate, the blueprint row");
        Assert.That(blueprints[0].ResolutionMode, Is.EqualTo("external"));
    }
}
