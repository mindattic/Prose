using Microsoft.AspNetCore.SignalR.Client;
using Prose.Hub.Contracts;

namespace Prose.ObserverUi;

/// <summary>
/// Wraps the SignalR connection to Prose.Hub's <c>ObservabilityHub</c> (<c>/hubs/observability</c>),
/// exposing plain C# events every tab subscribes to. The SAME client code path serves both UI
/// hosts (the Blazor-Server-in-Hub web head and the Prose.Maui native head) — only the base
/// URL differs, wired via <c>AddProseObserverUi(hubBaseUrl)</c>. Registered <c>Scoped</c>, same
/// reasoning as <see cref="TabShellState"/>: one connection per browser circuit in Blazor
/// Server, one per app instance in MAUI.
/// </summary>
public sealed class HubApiClient : IAsyncDisposable
{
    private readonly HubConnection connection;

    public event Action<LogLineDto>? LogLine;
    public event Action<DcmRunDto>? DcmRunStarted;
    public event Action<DcmBeatDto>? DcmBeat;
    public event Action<DcmRunDto>? DcmRunEnded;

    /// <summary>Raw JSON payload (<c>DcmVisualizationService.VizPayload</c>'s shape) rebuilt
    /// from the whole run-so-far after every beat — the live DCM-Viz chart does a cheap full
    /// rebuild rather than a true incremental DOM patch, sharing the exact JSON shape and JS
    /// renderer with history mode (see ObservabilityBridge's own doc comment).</summary>
    public event Action<string>? DcmPayload;

    public event Action<GraphDeltaDto>? GraphDelta;

    /// <summary>Fires on every connection-state transition (Connected/Reconnecting/
    /// Disconnected) so a tab can show a clear "disconnected" state instead of a silent
    /// freeze when the Hub goes down mid-write.</summary>
    public event Action? StateChanged;

    public HubConnectionState State => connection.State;

    public HubApiClient(string hubBaseUrl)
    {
        connection = new HubConnectionBuilder()
            .WithUrl($"{hubBaseUrl.TrimEnd('/')}/hubs/observability")
            .WithAutomaticReconnect()
            .Build();

        connection.On<LogLineDto>("LogLine", dto => LogLine?.Invoke(dto));
        connection.On<DcmRunDto>("DcmRunStarted", dto => DcmRunStarted?.Invoke(dto));
        connection.On<DcmBeatDto>("DcmBeat", dto => DcmBeat?.Invoke(dto));
        connection.On<DcmRunDto>("DcmRunEnded", dto => DcmRunEnded?.Invoke(dto));
        connection.On<string>("DcmPayload", json => DcmPayload?.Invoke(json));
        connection.On<GraphDeltaDto>("GraphDelta", dto => GraphDelta?.Invoke(dto));

        connection.Reconnecting += _ => { StateChanged?.Invoke(); return Task.CompletedTask; };
        connection.Reconnected += _ => { StateChanged?.Invoke(); return Task.CompletedTask; };
        connection.Closed += _ => { StateChanged?.Invoke(); return Task.CompletedTask; };
    }

    /// <summary>Idempotent — safe to call from every tab's OnInitializedAsync; only the
    /// first caller actually starts the connection.</summary>
    public async Task EnsureStartedAsync()
    {
        if (connection.State == HubConnectionState.Disconnected)
        {
            try { await connection.StartAsync(); }
            finally { StateChanged?.Invoke(); }
        }
    }

    public Task SubscribeUniverseAsync(string slug) => connection.InvokeAsync("SubscribeUniverse", slug);
    public Task UnsubscribeUniverseAsync(string slug) => connection.InvokeAsync("UnsubscribeUniverse", slug);

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();
}
