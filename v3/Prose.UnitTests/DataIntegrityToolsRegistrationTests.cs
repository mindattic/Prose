using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Extensions;
using Prose.Mcp;

namespace Prose.UnitTests;

/// <summary>
/// DI-wiring smoke test for the 2026-08-09 addition of Tools.DataIntegrity.cs — the MCP
/// equivalent of the --audit-consistency / --graph-health / --sanity-scan CLI commands added
/// earlier this session for DataConsistencyService, GraphHealthService, and SanityScanService.
/// Mirrors InterfaceRegistrationTests.cs's pattern: build a real DI container via the same
/// AddProseServices() extension both Prose.Cli and Prose.Mcp use, and confirm the new MCP tool
/// class's full constructor dependency graph resolves without a missing-registration or
/// circular-dependency error. This can't be caught at compile time — DI resolution failures
/// only surface at runtime, when the actual MCP server tries to construct the tool for a call —
/// and there was no live MCP client harness available this session to exercise that path
/// end-to-end, so this is the next-best verification available.
/// </summary>
[TestFixture]
public class DataIntegrityToolsRegistrationTests
{
    [Test]
    public void DI_ResolvesDataIntegrityTools()
    {
        var services = new ServiceCollection();
        services.AddProseServices();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        // DataIntegrityTools is never explicitly registered in the container by AddProseServices()
        // — the real MCP host registers [McpServerToolType] classes separately via
        // .AddMcpServer().WithToolsFromAssembly(), which this test's DI setup doesn't include.
        // ActivatorUtilities.CreateInstance constructs the (unregistered) type directly, resolving
        // its constructor parameters from the container — the same mechanism the MCP SDK uses
        // internally to instantiate a tool class per call, so this exercises the actual thing
        // that would fail at real server runtime if a dependency were missing or miswired.
        try
        {
            var tools = ActivatorUtilities.CreateInstance<DataIntegrityTools>(sp);
            Assert.That(tools, Is.Not.Null);
        }
        catch (Exception ex) when (SqlAvailability.IsUnavailable(ex))
        {
            // DataIntegrityTools depends on UniverseGraphService, whose DI factory eagerly calls
            // EnsureLoaded()/Rebuild() at construction time — resolving it touches the SQL Server
            // DB immediately, before any of DataIntegrityTools' own methods are ever called.
            // Confirmed live 2026-08-09: this test passed against every dev machine (LocalDB
            // installed) but failed the first time it ran on a genuine CI runner with no SQL
            // Server at all — same gap as InterfaceRegistrationTests.DI_RegistersIUniverseGraphService,
            // fixed in the same commit. Test environments without LocalDB skip cleanly; the wiring
            // this test actually cares about (does the constructor dependency graph resolve at
            // all) is verified by reaching the DB call in the first place.
            Assert.Inconclusive("SQL Server / LocalDB is not available in this environment.");
        }
    }
}
