using Prose.Core.Interfaces;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

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

file sealed class DeterministicAnomalyService(
    WorldbuildingDocRepository docRepo,
    DistrictRepository districtRepo,
    bool gateOpen)
    : AmbientAnomalyService(docRepo, districtRepo)
{
    protected override bool RandomGatePasses() => gateOpen;
}

[TestFixture]
public class AmbientAnomalyServiceTests
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
    public void EmptyDocList_GetAmbientHints_ReturnsEmpty()
    {
        var svc = new AmbientAnomalyService(new StubDocRepo([]), new StubDistrictRepo());

        var hints = svc.GetAmbientHints("The Shelf");

        Assert.That(hints, Is.Empty);
    }

    [Test]
    public void NoAnomalyTaggedDocs_GetAmbientHints_ReturnsEmpty()
    {
        var docs = new List<WorldbuildingDocument>
        {
            NonAnomalyDoc("The Spine"),
            NonAnomalyDoc("Bloom Quarter"),
        };
        var svc = new AmbientAnomalyService(new StubDocRepo(docs), new StubDistrictRepo());

        var hints = svc.GetAmbientHints("The Shelf");

        Assert.That(hints, Is.Empty);
    }

    [Test]
    public void EmptyDocList_FormatHints_ReturnsEmptyString()
    {
        var svc = new AmbientAnomalyService(new StubDocRepo([]), new StubDistrictRepo());

        var result = svc.FormatHints("The Shelf");

        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void NullLocation_EmptyDocList_NoCrash()
    {
        var svc = new AmbientAnomalyService(new StubDocRepo([]), new StubDistrictRepo());

        Assert.DoesNotThrow(() => svc.GetAmbientHints(null));
    }

    [Test]
    public void NullLocation_FormatHints_NoCrash()
    {
        var svc = new AmbientAnomalyService(new StubDocRepo([]), new StubDistrictRepo());

        Assert.DoesNotThrow(() => svc.FormatHints(null));
    }

    [Test]
    public void AnomalyDoc_GateClosed_GetAmbientHints_ReturnsEmpty()
    {
        var docs = new List<WorldbuildingDocument> { AnomalyDoc("Ghost Block", "A building that shouldn't exist.") };
        var svc = new DeterministicAnomalyService(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: false);

        var hints = svc.GetAmbientHints("The Shelf");

        Assert.That(hints, Is.Empty);
    }

    [Test]
    public void AnomalyDoc_GateOpen_GetAmbientHints_ReturnsHint()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "A building that shouldn't exist. People walk into it and don't come out."),
        };
        var svc = new DeterministicAnomalyService(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientHints("The Shelf");

        Assert.That(hints, Is.Not.Empty);
        Assert.That(hints[0], Does.Contain("[Ambient"));
    }

    [Test]
    public void AnomalyDoc_GateOpen_FormatHints_ReturnsBlock()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "A building that shouldn't exist. People walk into it."),
        };
        var svc = new DeterministicAnomalyService(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: true);

        var result = svc.FormatHints("The Shelf");

        Assert.That(result, Does.Contain("AMBIENT ANOMALIES"));
    }

    [Test]
    public void AnomalyDoc_GateClosed_FormatHints_ReturnsEmptyString()
    {
        var docs = new List<WorldbuildingDocument>
        {
            AnomalyDoc("Ghost Block", "A building that shouldn't exist."),
        };
        var svc = new DeterministicAnomalyService(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: false);

        var result = svc.FormatHints("The Shelf");

        Assert.That(result, Is.EqualTo(""));
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
        var svc = new DeterministicAnomalyService(new StubDocRepo(docs), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientHints("anywhere", maxHints: 10);

        Assert.That(hints.Count, Is.LessThanOrEqualTo(2));
        Assert.That(hints, Has.All.Contains("[Ambient"));
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
        var svc = new DeterministicAnomalyService(new StubDocRepo([doc]), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientHints("anywhere");

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
        var svc = new DeterministicAnomalyService(new StubDocRepo([doc]), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientHints("anywhere");

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
        var svc = new DeterministicAnomalyService(new StubDocRepo([doc]), new StubDistrictRepo(), gateOpen: true);

        var hints = svc.GetAmbientHints("anywhere");

        Assert.That(hints[0], Does.Contain("Hollow Corner"));
    }
}
