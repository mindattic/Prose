using Prose.Core.Models.Canon;
using Prose.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Extensions;

namespace Prose.UnitTests;

[TestFixture]
public class SubsidiaryTests
{
    [Test]
    public void SubsidiaryData_HasParentCorponation()
    {
        var sub = new SubsidiaryData
        {
            Name = "Ironside Armaments",
            ParentCorponation = "Arcturus Defense Solutions",
            LineOfBusiness = "Small arms manufacturing"
        };
        Assert.That(sub.ParentCorponation, Is.EqualTo("Arcturus Defense Solutions"));
        Assert.That(sub.Type, Is.EqualTo("subsidiary"));
    }

    [Test]
    public void SubsidiaryData_HasTags()
    {
        var sub = new SubsidiaryData { Tags = ["subsidiary", "arcturus"] };
        Assert.That(sub.Tags, Has.Count.EqualTo(2));
    }

    [Test]
    public void SubsidiaryRepository_IsRegistered()
    {
        var services = new ServiceCollection();
        services.AddProseServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();
        Assert.That(sp.GetService<SubsidiaryRepository>(), Is.Not.Null);
    }

    [Test]
    public void SubsidiaryData_Serialization_RoundTrip()
    {
        var sub = new SubsidiaryData
        {
            Name = "MindBridge Neural",
            ParentCorponation = "Lazarus Pharmaceuticals",
            LineOfBusiness = "Consumer BCI manufacturing",
            PublicFacing = true,
            KnownProducts = ["MindBridge Spark", "MindBridge Clarity"],
            Tags = ["subsidiary", "lazarus", "bci"]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(sub);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SubsidiaryData>(json);

        Assert.That(deserialized!.Name, Is.EqualTo("MindBridge Neural"));
        Assert.That(deserialized.ParentCorponation, Is.EqualTo("Lazarus Pharmaceuticals"));
        Assert.That(deserialized.PublicFacing, Is.True);
        Assert.That(deserialized.KnownProducts, Has.Count.EqualTo(2));
    }
}

[TestFixture]
public class EntertainmentTests
{
    [Test]
    public void EntertainmentData_DefaultType()
    {
        Assert.That(new EntertainmentData().Type, Is.EqualTo("entertainment"));
    }

    [Test]
    public void EntertainmentData_HasAllFields()
    {
        var e = new EntertainmentData
        {
            Name = "Neon Requiem",
            Category = "band",
            Genre = "neural-punk",
            Medium = "neural_feed",
            Creator = "Various",
            Tags = ["entertainment", "music", "band"]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(e);
        var d = System.Text.Json.JsonSerializer.Deserialize<EntertainmentData>(json);
        Assert.That(d!.Genre, Is.EqualTo("neural-punk"));
        Assert.That(d.Medium, Is.EqualTo("neural_feed"));
    }

    [Test]
    public void EntertainmentRepository_IsRegistered()
    {
        var services = new ServiceCollection();
        services.AddProseServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();
        Assert.That(sp.GetService<EntertainmentRepository>(), Is.Not.Null);
    }
}
