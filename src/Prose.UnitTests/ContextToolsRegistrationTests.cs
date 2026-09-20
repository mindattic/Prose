using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Extensions;
using Prose.Mcp;

namespace Prose.UnitTests;

/// <summary>
/// DI-wiring smoke test for ContextTools' new IDbContextFactory&lt;ProseDbContext&gt; dependency
/// (added 2026-08-09 alongside the propose_motifs tool). Same rationale as
/// DataIntegrityToolsRegistrationTests: ActivatorUtilities is the only way to catch a
/// missing-registration/circular-dependency error before the real MCP server tries to construct
/// this tool for a call — and this exact class of bug (a DI factory that works on a dev box with
/// LocalDB but not on a bare CI runner) already surfaced once this session for a sibling tool
/// class, so every new/changed MCP tool constructor gets this guard from now on.
/// </summary>
[TestFixture]
public class ContextToolsRegistrationTests
{
    [Test]
    public void DI_ResolvesContextTools()
    {
        var services = new ServiceCollection();
        services.AddProseServices();
        services.AddLogging();
        // HubInvoker (2026-08-30 fix — see DataIntegrityToolsRegistrationTests' own comment for
        // the full rationale): registered by Prose.Mcp/Program.cs directly, not by
        // AddProseServices() or WithToolsFromAssembly. AddHttpClient() alone is enough for DI
        // resolution here — no call is ever actually made.
        services.AddHttpClient();
        services.AddSingleton<HubInvoker>();
        using var sp = services.BuildServiceProvider();

        try
        {
            var tools = ActivatorUtilities.CreateInstance<ContextTools>(sp);
            Assert.That(tools, Is.Not.Null);
        }
        catch (Exception ex) when (SqlAvailability.IsUnavailable(ex))
        {
            Assert.Inconclusive("SQL Server / LocalDB is not available in this environment.");
        }
    }
}
