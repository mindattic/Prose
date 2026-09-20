namespace Prose.ObserverUi;

/// <summary>
/// Which tab is active, plus which tabs are currently open — the runtime behavior the old,
/// deleted <c>Prose.Shared/Services/TabService.cs</c> had (browser-tab-style: an unclosable
/// Home/Dashboard tab, open/close, an OnChanged event for re-render). Rebuilt fresh rather
/// than ported, since the old design's "scoped per connection" assumption needs to hold in
/// TWO different hosting models:
///
/// Registered <c>Scoped</c> in DI (see <c>AddProseObserverUi</c>) — Blazor Server creates one
/// DI scope per circuit, so Scoped correctly means "one per browser connection" there; MAUI's
/// <c>BlazorWebView</c> creates one root scope for the app's entire lifetime, so the same
/// registration degrades to "one per app instance," which is exactly correct there too. No
/// special-casing needed between the two hosts.
/// </summary>
public sealed class TabShellState
{
    private readonly List<string> openTabIds = [];
    private string? activeTabId;

    public event Action? Changed;

    public IReadOnlyList<string> OpenTabIds => openTabIds;
    public string? ActiveTabId => activeTabId;

    /// <summary>Opens a tab (no-op if already open) and makes it active.</summary>
    public void Open(string tabId)
    {
        if (!openTabIds.Contains(tabId)) openTabIds.Add(tabId);
        activeTabId = tabId;
        Changed?.Invoke();
    }

    public void Activate(string tabId)
    {
        if (!openTabIds.Contains(tabId)) return;
        activeTabId = tabId;
        Changed?.Invoke();
    }

    /// <summary>Closes a tab. The first-ever opened tab (index 0, conventionally Dashboard)
    /// is unclosable, matching the old design's unclosable Home tab.</summary>
    public void Close(string tabId)
    {
        if (openTabIds.Count == 0 || openTabIds[0] == tabId) return;
        var wasActive = activeTabId == tabId;
        openTabIds.Remove(tabId);
        if (wasActive) activeTabId = openTabIds.Count > 0 ? openTabIds[^1] : null;
        Changed?.Invoke();
    }
}
