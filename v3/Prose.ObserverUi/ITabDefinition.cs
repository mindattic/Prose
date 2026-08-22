namespace Prose.ObserverUi;

/// <summary>
/// One entry in the tab shell — registered via DI (see <c>AddProseObserverUi</c>) as
/// <c>IEnumerable&lt;ITabDefinition&gt;</c> singletons, so adding a new tab later is one more
/// registration line, zero changes to <c>TabShell.razor</c>'s rendering logic.
/// </summary>
public interface ITabDefinition
{
    /// <summary>Stable identifier, e.g. "dashboard". Used for the active-tab key.</summary>
    string Id { get; }

    string Title { get; }

    /// <summary>Bootstrap-icon class, e.g. "bi-speedometer2" — matches the dark
    /// Bootstrap + Bootstrap Icons theme already used by the Hub's own dashboard and the
    /// deleted Codex/Writer UIs this design takes visual cues from.</summary>
    string Icon { get; }

    /// <summary>The Razor component type rendered when this tab is active.</summary>
    Type ComponentType { get; }

    /// <summary>Left-to-right ordering in the tab strip.</summary>
    int Order { get; }
}

/// <summary>Plain record implementation — most tabs just need data, no custom behavior.</summary>
public sealed record TabDefinition(string Id, string Title, string Icon, Type ComponentType, int Order) : ITabDefinition;
