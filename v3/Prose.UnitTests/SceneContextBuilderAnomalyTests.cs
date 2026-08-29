using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

// The New Weird anomaly layer used to live in AmbientAnomalyService; folded into
// SceneContextBuilder 2026-08-28 (both auto-fired on the same Location gate and pulled from
// overlapping anomaly tag pools, double-injecting one beat's prompt). These tests are the old
// AmbientAnomalyServiceTests retargeted at the absorbed implementation.

file sealed class NullPathProvider : IPathProvider
{
    static readonly string Base = Path.Combine(Path.GetTempPath(), $"ss_anomaly_{Guid.NewGuid():N}");
    public string DataRoot => Base;
    public string WorldbuildingDir => Base;
    public string CharactersDir => Base;
    public string EssencesDir => Base;
    public string NarrativeBiblePath => Path.Combine(Base, "bible.md");
    public string WorldDir => Base;
    public string EngineDataDir => Base;
    public string MutableDataDir => Base;
    public string ChaptersDir => Base;
    public string BooksDir => Base;
    public string SeriesDir => Base;
    public string GraphDir => Base;
    public string LogDir => Base;
    public string ExportDir => Base;
    public string ArchiveDir => Base;
    public string MediaDir => Base;
    public string MediaArchiveDir => Base;
}

file sealed class StubDocRepo(List<WorldbuildingDocument> docs)
    : WorldbuildingDocRepository(new NullPathProvider())
{
    public override List<WorldbuildingDocument> GetAll() => docs;
}

file sealed class StubDistrictRepo : DistrictRepository
{
    public StubDistrictRepo() : base(new NullPathProvider()) { }
    public override List<DistrictData> GetAll() => [];
}

file sealed class DeterministicSceneContextBuilder(
    WorldbuildingDocRepository docRepo,
    DistrictRepository districtRepo,
    bool gateOpen)
    : SceneContextBuilder(docRepo, districtRepo)
{
    protected override bool RandomGatePasses() => gateOpen;
}

[TestFixture]
public class SceneContextBuilderAnomalyTests
{
    static WorldbuildingDocument AnomalyDoc(string title, string body, params string[] extraTags)
    {
        var tags = new List<string> { "anomaly" };
        tags.AddRange(extraTags);
        return new WorldbuildingDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Body = body,
            Tags = tags,
        };
    }

    static WorldbuildingDocument NonAnomalyDoc(string title) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Title = title,
        Body = "Completely normal worldbuilding.",
        Tags = ["lore", "city"],
    };

    [Test]
    public void EmptyDocList_GetAmbientAnomalyHints_ReturnsEmpty()
    {
        var svc = new SceneContextBuilder(new StubDocRepo([]), new StubDistrictRepo());

        var hints = svc.GetAmbientAnomalyHints("The Shelf");

        Assert.That(hints, Is.Empty);
    }

    [Test]
    public void NoAnomalyTaggedDocs_GetAmbientAnomalyHints_ReturnsEmpty()
    {
        var docs = new List<WorldbuildingDocument>
        {
            NonAnomalyDoc("The Spine"),
            NonAnomalyDoc("Bloom Quarter"),
        };
        var svc = new SceneContextBuilder(new StubDocRepo(docs), new StubDistrictRepo());

        var hints = svc.GetAmbientAnomalyHints("The Shelf");

        Assert.That(hints, Is.Empty);
    }

    [Test]
    public void NullLocation_EmptyDocList_NoCrash()
    {
        var svc = new SceneContextBuilder(new StubDocRepo([]), new StubDistrictRepo());

        Assert.DoesNotThrow(() => svc.GetAmbientAnomalyHints(null));
    }

    [Test]
    public void AnomalyDoc_GateClosed_GetAmbientAnomalyHints_ReturnsEmpty()
    {
        var docs = new List<WorldbuildingDocument> { AnomalyDoc("Ghost Block", "A building that shouldn't exist.") };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: false);

        var hints = svc.GetAmbientAnomalyHints("The Shelf");

        Assert.That(hints, Is.Empty);
    }

    [Test]
    public void AnomalyDoc_GateOpen_GetAmbientAnomalyHints_ReturnsHint()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "A building that shouldn't exist. People walk into it and don't come out."),
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientAnomalyHints("The Shelf");

        Assert.That(hints, Is.Not.Empty);
        Assert.That(hints[0], Does.Contain("AMBIENT STRANGENESS"));
    }

    [Test]
    public void AnomalyDoc_GateOpen_BuildAmbientContext_ContainsAnomalySection()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "A building that shouldn't exist. People walk into it."),
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: true);

        var result = svc.BuildAmbientContext("The Shelf");

        Assert.That(result, Does.Contain("AMBIENT WORLD CONTEXT"));
        Assert.That(result, Does.Contain("AMBIENT STRANGENESS"));
    }

    [Test]
    public void AnomalyDoc_GateClosed_BuildAmbientContext_HasNoAnomalySection()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "A building that shouldn't exist."),
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: false);

        var result = svc.BuildAmbientContext("The Shelf");

        Assert.That(result, Does.Not.Contain("AMBIENT STRANGENESS"));
    }

    [Test]
    public void CacheIsPopulatedOnlyWithAnomalyDocs()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "An inexplicable building."),
            NonAnomalyDoc("City Hall"),
            AnomalyDoc("Lost Street", "A street that changes address every night."),
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientAnomalyHints("anywhere", maxHints: 10);

        Assert.That(hints.Count, Is.LessThanOrEqualTo(2));
        Assert.That(hints, Has.All.Contains("AMBIENT STRANGENESS"));
    }

    [Test]
    public void InexplicableTag_AlsoRecognisedAsAnomaly()
    {
        var doc = new WorldbuildingDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "The Inexplicable Drain",
            Body = "Water flows uphill here.",
            Tags = ["inexplicable"],
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo([doc]), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientAnomalyHints("anywhere");

        Assert.That(hints, Is.Not.Empty);
    }

    [Test]
    public void NewWeirdTag_AlsoRecognisedAsAnomaly()
    {
        var doc = new WorldbuildingDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Membrane Alley",
            Body = "The alley breathes. Not metaphorically.",
            Tags = ["new_weird"],
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo([doc]), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientAnomalyHints("anywhere");

        Assert.That(hints, Is.Not.Empty);
    }

    [Test]
    public void BodyEmpty_SnippetFallsBackToTitle()
    {
        var doc = new WorldbuildingDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = "Hollow Corner",
            Body = "",
            Tags = ["anomaly"],
        };
        var svc = new DeterministicSceneContextBuilder(new StubDocRepo([doc]), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientAnomalyHints("anywhere");

        Assert.That(hints[0], Does.Contain("Hollow Corner"));
    }
}
