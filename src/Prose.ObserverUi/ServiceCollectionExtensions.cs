using Microsoft.Extensions.DependencyInjection;
using Prose.ObserverUi.Components.Tabs;

namespace Prose.ObserverUi;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the shared observability UI needs — mirrors <c>AddProseServices()</c>'s
    /// convention. Called identically by both hosts (Prose.Hub's web head and, later,
    /// Prose.Maui's native head); only <paramref name="hubBaseUrl"/> differs per host.
    /// </summary>
    /// <param name="apiKey">Reads the shared <c>X-Prose-Key</c> at scope-creation time, not at
    /// registration: the Hub generates the key <em>after</em> the DI container is built, so a key
    /// captured here would be null on the first-ever startup and stay null for the process's life.
    /// Omit only for a host that talks exclusively to unauthenticated endpoints.</param>
    public static IServiceCollection AddProseObserverUi(this IServiceCollection services, string hubBaseUrl,
                                                        Func<IServiceProvider, string?>? apiKey = null)
    {
        // Scoped: one per browser circuit in Blazor Server, one per app instance in MAUI's
        // single root scope — see TabShellState's own doc comment for why that's correct
        // in both hosts without special-casing.
        services.AddScoped<TabShellState>();
        services.AddScoped(_ => new HubApiClient(hubBaseUrl));
        // Plain HttpClient (not the IHttpClientFactory-based AddHttpClient<T>, which would
        // pull in Microsoft.Extensions.Http just for this) - fine for a personal tool talking
        // to localhost; one instance per scope (per browser circuit / MAUI app instance).
        //
        // The X-Prose-Key header is what every /api/mcp-invoke call needs to get past
        // HubApiKeyFilter. Omitting it (as this did until 2026-09-11) 401s with a non-JSON body,
        // which surfaced as a JsonException out of TryDeserialize and took the whole /app page
        // down with a 500 — every MCP-backed tab (Beats, Repositories, Beat Archive, the
        // Dashboard feeds, Logs history) was dead.
        services.AddScoped(sp =>
        {
            var http = new HttpClient { BaseAddress = new Uri(hubBaseUrl.TrimEnd('/') + "/") };
            var key = apiKey?.Invoke(sp);
            if (!string.IsNullOrEmpty(key)) http.DefaultRequestHeaders.Add("X-Prose-Key", key);
            return new ObserverHttpClient(http);
        });

        // Initial tabs — adding a 6th/7th/Nth tab later is one more line here, zero changes
        // to TabShell.razor's rendering logic.
        services.AddSingleton<ITabDefinition>(new TabDefinition("dashboard", "Dashboard", "dashboard", typeof(DashboardTab), 0));
        services.AddSingleton<ITabDefinition>(new TabDefinition("logs", "Logs", "logs", typeof(LogsTab), 1));
        services.AddSingleton<ITabDefinition>(new TabDefinition("dcm-viz", "DCM-Viz", "dcm-viz", typeof(DcmVizTab), 2));
        services.AddSingleton<ITabDefinition>(new TabDefinition("graph-2d", "Graph 2D", "graph-2d", typeof(Graph2DTab), 3));
        services.AddSingleton<ITabDefinition>(new TabDefinition("graph-3d", "Graph 3D", "graph-3d", typeof(Graph3DTab), 4));
        services.AddSingleton<ITabDefinition>(new TabDefinition("beats", "Beats", "beats", typeof(BeatsTab), 5));
        services.AddSingleton<ITabDefinition>(new TabDefinition("repositories", "Repositories", "repositories", typeof(RepositoriesTab), 6));
        services.AddSingleton<ITabDefinition>(new TabDefinition("beat-archive", "Beat Archive", "beat-archive", typeof(BeatArchiveTab), 7));

        return services;
    }
}
