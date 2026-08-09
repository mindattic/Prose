namespace Prose.UnitTests;

using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Extensions;
using Prose.Core.Interfaces;
using Prose.Core.Services;

[TestFixture]
public class InterfaceRegistrationTests
{
    [Test]
    public void DI_RegistersIDatabaseService()
    {
        var services = new ServiceCollection();
        services.AddProseServices();
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
        services.AddProseServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        try
        {
            var iface = sp.GetService<IWorldGraphService>();
            var concrete = sp.GetService<WorldGraphService>();
            Assert.That(iface, Is.Not.Null);
            Assert.That(iface, Is.SameAs(concrete));
        }
        catch (Exception ex) when (SqlAvailability.IsUnavailable(ex))
        {
            // WorldGraphService's DI factory eagerly calls EnsureLoaded()/Rebuild() at
            // construction time — merely resolving it (not calling any of its methods) touches
            // the SQL Server DB immediately. Confirmed live 2026-08-09: this test passed on every
            // dev machine (LocalDB installed) but failed the first time it ever ran on a genuine
            // CI runner with no SQL Server at all — the same missing guard DI_RegistersIStoryDirectorService
            // already has, just never caught here because nothing ran tests in CI until this session.
            Assert.Inconclusive("SQL Server / LocalDB is not available in this environment.");
        }
    }

    [Test]
    public void DI_RegistersIStoryDirectorService()
    {
        var services = new ServiceCollection();
        services.AddProseServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        try
        {
            var iface = sp.GetService<IStoryDirectorService>();
            var concrete = sp.GetService<StoryDirectorService>();
            Assert.That(iface, Is.Not.Null);
            Assert.That(iface, Is.SameAs(concrete));
        }
        catch (Exception ex) when (SqlAvailability.IsUnavailable(ex))
        {
            // The director service touches the SQL Server DB during construction.
            // Test environments without LocalDB skip cleanly — the registration
            // wiring this test cares about is verified by the constructor reaching
            // the DB call at all.
            Assert.Inconclusive("SQL Server / LocalDB is not available in this environment.");
        }
    }
}
