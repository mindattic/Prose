namespace StreetSamurai.UnitTests;

using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

[TestFixture]
public class InterfaceRegistrationTests
{
    [Test]
    public void DI_RegistersIDatabaseService()
    {
        var services = new ServiceCollection();
        services.AddStreetSamuraiServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        var iface = sp.GetService<IDatabaseService>();
        var concrete = sp.GetService<DatabaseService>();
        Assert.That(iface, Is.Not.Null);
        Assert.That(iface, Is.SameAs(concrete));
    }

    [Test]
    public void DI_RegistersIWorldGraphService()
    {
        var services = new ServiceCollection();
        services.AddStreetSamuraiServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        var iface = sp.GetService<IWorldGraphService>();
        var concrete = sp.GetService<WorldGraphService>();
        Assert.That(iface, Is.Not.Null);
        Assert.That(iface, Is.SameAs(concrete));
    }

    [Test]
    public void DI_RegistersIStoryDirectorService()
    {
        var services = new ServiceCollection();
        services.AddStreetSamuraiServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        var iface = sp.GetService<IStoryDirectorService>();
        var concrete = sp.GetService<StoryDirectorService>();
        Assert.That(iface, Is.Not.Null);
        Assert.That(iface, Is.SameAs(concrete));
    }
}
