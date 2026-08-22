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
    public static IServiceCollection AddProseObserverUi(this IServiceCollection services, string hubBaseUrl)
    {
        // Scoped: one per browser circuit in Blazor Server, one per app instance in MAUI's
        // single root scope — see TabShellState's own doc comment for why that's correct
        // in both hosts without special-casing.
        services.AddScoped<TabShellState>();
        services.AddScoped(_ => new HubApiClient(hubBaseUrl));
        // Plain HttpClient (not the IHttpClientFactory-based AddHttpClient<T>, which would
        // pull in Microsoft.Extensions.Http just for this) - fine for a personal tool talking
        // to localhost; one instance per scope (per browser circuit / MAUI app instance).
        services.AddScoped(_ => new ObserverHttpClient(new HttpClient { BaseAddress = new Uri(hubBaseUrl.TrimEnd('/') + "/") }));

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
