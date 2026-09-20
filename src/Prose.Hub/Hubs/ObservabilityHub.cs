using Microsoft.AspNetCore.SignalR;

namespace Prose.Hub.Hubs;

/// <summary>
/// Live push channel for the observability plan (2026-08-20): log lines, DCM beat
/// snapshots, and graph deltas, straight from the Hub's own resident state
/// (<c>ContextTelemetryService</c>, <c>UniverseGraphService</c>, <c>RingBufferLoggerProvider</c>
/// — see Program.cs for the subscription wiring). Graph/DCM events are universe-scoped
/// (<c>universe:{slug}</c> groups, joined via <see cref="SubscribeUniverse"/>); log lines go
/// to every connection since the Hub's own console isn't universe-scoped at all.
///
/// Both UI hosts (the Blazor-Server-in-Hub web head and the MAUI Blazor Hybrid head) connect
/// through the exact same client path — the web head does not skip the wire just because it's
/// in the same process, keeping this genuinely one UI, not two.
/// </summary>
public sealed class ObservabilityHub : Microsoft.AspNetCore.SignalR.Hub
{
    public Task SubscribeUniverse(string slug) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(slug));

    public Task UnsubscribeUniverse(string slug) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(slug));

    public static string GroupName(string slug) => $"universe:{slug}";
}
