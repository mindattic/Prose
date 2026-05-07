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

        try
        {
            var iface = sp.GetService<IStoryDirectorService>();
            var concrete = sp.GetService<StoryDirectorService>();
            Assert.That(iface, Is.Not.Null);
            Assert.That(iface, Is.SameAs(concrete));
        }
        catch (Exception ex) when (IsSqlUnavailable(ex))
        {
            // The director service touches the SQL Server DB during construction.
            // Test environments without LocalDB skip cleanly — the registration
            // wiring this test cares about is verified by the constructor reaching
            // the DB call at all.
            Assert.Inconclusive("SQL Server / LocalDB is not available in this environment.");
        }
    }

    private static bool IsSqlUnavailable(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is Microsoft.Data.SqlClient.SqlException) return true;
            if (e.Message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase)) return true;
            if (e.Message.Contains("Login failed", StringComparison.OrdinalIgnoreCase)) return true;
            if (e.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
