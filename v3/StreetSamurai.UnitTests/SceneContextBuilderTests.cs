using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

// ── Stubs ────────────────────────────────────────────────────────────────────

/// <summary>DistrictRepository stub that returns an in-memory list.</summary>
file sealed class StubDistrictRepo : DistrictRepository
{
    private readonly List<DistrictData> data;

    public StubDistrictRepo(List<DistrictData> data)
        : base(db: null!) => this.data = data;

    public override List<DistrictData> GetAll() => data;
}

/// <summary>WorldbuildingDocRepository stub that returns an in-memory list.</summary>
file sealed class StubDocRepo : WorldbuildingDocRepository
{
    private readonly List<WorldbuildingDocument> data;

    public StubDocRepo(List<WorldbuildingDocument> data)
        : base(db: null!) => this.data = data;

    public override List<WorldbuildingDocument> GetAll() => data;
}

// ── Tests ────────────────────────────────────────────────────────────────────

[TestFixture]
public class SceneContextBuilderDetailedTests
{
    private static SceneContextBuilder Build(
        List<DistrictData>? districts = null,
        List<WorldbuildingDocument>? docs = null)
    {
        var districtRepo = new StubDistrictRepo(districts ?? []);
        var docRepo      = new StubDocRepo(docs ?? []);
        // DatabaseService and WorldGraphService are not called by BuildAmbientContext
        return new SceneContextBuilder(db: null!, graph: null!, docRepo: docRepo, districtRepo: districtRepo);
    }

    // ── Empty / unknown location ──────────────────────────────────────────────

    [Test]
    public void BuildAmbientContext_NullLocation_NoDistrict_NoDocs_ReturnsEmpty()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void BuildAmbientContext_UnknownLocation_ReturnsNoDistrictLine()
    {
        var districts = new List<DistrictData>
        {
            new() { Name = "The Spine", Description = "Lakeshore strip." }
        };
        var builder = Build(districts: districts);

        // Location does not match any district name
        var result = builder.BuildAmbientContext("Nowhere Known");
        Assert.That(result, Does.Not.Contain("DISTRICT:"));
    }

    // ── Known location ───────────────────────────────────────────────────────

    [Test]
    public void BuildAmbientContext_KnownLocation_ContainsDistrictName()
    {
        var districts = new List<DistrictData>
        {
            new() { Name = "The Spine", Description = "Western lakeshore strip, ferrocement." }
        };
        var builder = Build(districts: districts);

        var result = builder.BuildAmbientContext("safehouse near The Spine");
        Assert.That(result, Does.Contain("DISTRICT: The Spine"));
    }

    [Test]
    public void BuildAmbientContext_KnownLocation_ContainsAtmosphere()
    {
        var districts = new List<DistrictData>
        {
            new() { Name = "The Spine", Description = "Ferrocement and rust." }
        };
        var builder = Build(districts: districts);

        var result = builder.BuildAmbientContext("The Spine, Zone 3");
        Assert.That(result, Does.Contain("ATMOSPHERE:"));
        Assert.That(result, Does.Contain("Ferrocement"));
    }

    [Test]
    public void BuildAmbientContext_KnownLocation_IsCaseInsensitive()
    {
        var districts = new List<DistrictData>
        {
            new() { Name = "The Spine", Description = "Western strip." }
        };
        var builder = Build(districts: districts);

        var result = builder.BuildAmbientContext("THE SPINE upper section");
        Assert.That(result, Does.Contain("DISTRICT: The Spine"));
    }

    // ── Time and weather ──────────────────────────────────────────────────────

    [Test]
    public void BuildAmbientContext_WithTimeOfDay_ContainsTimeLine()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null, timeOfDay: "02:00");
        Assert.That(result, Does.Contain("TIME: 02:00"));
    }

    [Test]
    public void BuildAmbientContext_WithWeather_ContainsWeatherLine()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null, weather: "acid rain");
        Assert.That(result, Does.Contain("WEATHER: acid rain"));
    }

    [Test]
    public void BuildAmbientContext_TimeAndWeather_BothPresent()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null, timeOfDay: "dusk", weather: "thermal fog");
        Assert.That(result, Does.Contain("TIME: dusk"));
        Assert.That(result, Does.Contain("WEATHER: thermal fog"));
    }

    [Test]
    public void BuildAmbientContext_NullTimeAndWeather_NoTimeLine()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null, timeOfDay: null, weather: null);
        Assert.That(result, Does.Not.Contain("TIME:"));
        Assert.That(result, Does.Not.Contain("WEATHER:"));
    }

    [Test]
    public void BuildAmbientContext_WhitespaceTime_NoTimeLine()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null, timeOfDay: "   ");
        Assert.That(result, Does.Not.Contain("TIME:"));
    }

    // ── Sensory docs ──────────────────────────────────────────────────────────

    [Test]
    public void BuildAmbientContext_SensoryTaggedDoc_ContainsSensoryDetail()
    {
        var docs = new List<WorldbuildingDocument>
        {
            new() { Title = "Wet concrete smell", Body = "Ozone and wet concrete.", Tags = ["sensory", "z3"] }
        };
        var builder = Build(docs: docs);
        var result  = builder.BuildAmbientContext("any location");
        Assert.That(result, Does.Contain("SENSORY DETAIL:"));
    }

    // ── Result header ─────────────────────────────────────────────────────────

    [Test]
    public void BuildAmbientContext_WhenSomethingPresent_ContainsHeader()
    {
        var builder = Build();
        var result  = builder.BuildAmbientContext(null, timeOfDay: "noon");
        Assert.That(result, Does.Contain("AMBIENT WORLD CONTEXT"));
    }

    // ── Long description truncation ───────────────────────────────────────────

    [Test]
    public void BuildAmbientContext_LongDescription_IsTruncated()
    {
        var longDesc = new string('A', 400); // > 300 char limit
        var districts = new List<DistrictData>
        {
            new() { Name = "TestDistrict", Description = longDesc }
        };
        var builder = Build(districts: districts);
        var result  = builder.BuildAmbientContext("location in TestDistrict");

        Assert.That(result, Does.Contain("..."), "Description over 300 chars should be truncated with ellipsis");
    }
}
