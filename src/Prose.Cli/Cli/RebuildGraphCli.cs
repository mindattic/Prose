using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-graph [--universe &lt;slug&gt;]</c> — rebuilds the scoped universe's
/// <c>&lt;slug&gt;_universe_graph.json</c> cache from source data without starting the web
/// server.
///
/// Extracted out of Program.cs's dispatch chain into its own handler (Hub migration,
/// 2026-08-20) so it can route through the same generic CliDispatch forwarding as every
/// other command — the Hub already holds a resident per-universe UniverseGraphService (the
/// "Trinity"); rebuilding a separate transient copy in the CLI's own process would leave the
/// Hub's live graph stale.
/// </summary>
public static class RebuildGraphCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        // Pin the universe scope BEFORE building so it can't shift mid-rebuild. Resolving the
        // context forces its lazy catalog load + applies the --universe/PROSE_UNIVERSE/default
        // selection, so every builder in this rebuild sees one stable scope (the
        // non-deterministic node/edge counts came from the scope resolving partway through the
        // multi-builder pass). Defaults to GLMZ.
        var cliUniverse = services.GetRequiredService<IUniverseContext>();
        Console.WriteLine($"[rebuild-graph] Universe scope: {cliUniverse.CurrentSlug} ({cliUniverse.CurrentId})");

        var graph = services.GetRequiredService<UniverseGraphService>();
        Console.WriteLine("[rebuild-graph] Rebuilding world graph from source data...");
        graph.Rebuild();
        Console.WriteLine($"[rebuild-graph] Done: {graph.NodeCount} nodes, {graph.EdgeCount} edges saved to {cliUniverse.CurrentSlug}_universe_graph.json");
        return Task.FromResult(0);
    }
}
