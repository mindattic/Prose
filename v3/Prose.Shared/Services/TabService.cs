namespace Prose.Shared.Services;

/// <param name="MatchPrefix">If set, the tab is also active when the URL starts with this prefix (e.g. Writer tab active for /node/* paths).</param>
public record AppTab(string Key, string Label, string Icon, string Route, string? MatchPrefix = null);

/// <summary>Per-connection tab state. Scoped (one instance per browser session).</summary>
public class TabService
{
    private readonly List<AppTab> tabs = [HomeTab];

    public static readonly AppTab HomeTab = new("home", "Stories", "bi-book", "/");

    public IReadOnlyList<AppTab> Tabs => tabs;
    public string ActiveKey { get; private set; } = "home";
    public event Action? OnChanged;

    /// <summary>Open a tab (idempotent) and mark it active. Caller navigates.</summary>
    public void Open(AppTab tab)
    {
        if (!tabs.Any(t => t.Key == tab.Key))
            tabs.Add(tab);
        ActiveKey = tab.Key;
        OnChanged?.Invoke();
    }

    /// <summary>Close a tab. Home tab is unclosable. Returns true if the active tab changed.</summary>
    public bool Close(string key)
    {
        if (key == HomeTab.Key) return false;
        var wasActive = ActiveKey == key;
        tabs.RemoveAll(t => t.Key == key);
        if (wasActive)
            ActiveKey = HomeTab.Key;
        OnChanged?.Invoke();
        return wasActive;
    }

    /// <summary>Sync active tab from a navigated relative URL. Checks Route prefix first, then MatchPrefix.</summary>
    public void TrySync(string relativeUrl)
    {
        AppTab? match;
        if (relativeUrl == "/")
        {
            match = HomeTab;
        }
        else
        {
            match = tabs.FirstOrDefault(t => t.Route != "/" &&
                relativeUrl.StartsWith(t.Route, StringComparison.OrdinalIgnoreCase));
            match ??= tabs.FirstOrDefault(t => t.MatchPrefix != null &&
                relativeUrl.StartsWith(t.MatchPrefix, StringComparison.OrdinalIgnoreCase));
        }

        if (match != null && match.Key != ActiveKey)
        {
            ActiveKey = match.Key;
            OnChanged?.Invoke();
        }
    }

    public AppTab? GetActive() => tabs.FirstOrDefault(t => t.Key == ActiveKey);
}
