namespace Prose.Core.Services.Operator;

/// <summary>
/// Abstraction over the live KDP browser surface. Prose.Core targets plain
/// <c>net10.0</c> (shared by the CLI, MCP server, and web app) and cannot reference
/// <c>Microsoft.Web.WebView2</c> (Windows-only) directly — KdpPublish implements this
/// against its real <c>CoreWebView2</c> instance and Core's tools only ever see this
/// interface, keeping the automation logic testable and the browser dependency
/// confined to the one project that actually needs it.
/// </summary>
public interface IKdpBrowser
{
    /// <summary>Runs JS in the current page and returns its result, JSON-serialized.
    /// Errors thrown by the script itself should surface as a thrown exception, not a
    /// silently-null result, so tools can tell "found nothing" apart from "failed".</summary>
    Task<string> EvalAsync(string script, CancellationToken ct);

    /// <summary>Attaches a local file to the first element matching <paramref name="selector"/>
    /// (must resolve to an <c>&lt;input type="file"&gt;</c>) via Chrome DevTools Protocol's
    /// <c>DOM.setFileInputFiles</c> — no native OS file-picker dialog ever opens.</summary>
    Task InjectFileAsync(string filePath, string selector, CancellationToken ct);

    /// <summary>The page's current URL, for logging/diagnostics.</summary>
    string CurrentUrl { get; }

    /// <summary>Dispatches a real, trusted mouse click (press + release) at a viewport point.
    /// Distinct from calling .click() on an element in JS: some custom widgets (e.g. a React
    /// role=checkbox component) specifically ignore synthetic/untrusted click events, so a
    /// genuinely dispatched input event is required to actually toggle their state.</summary>
    Task ClickAtPointAsync(double x, double y, CancellationToken ct);

    /// <summary>Types <paramref name="text"/> as real, trusted keyboard input into whatever
    /// element currently has focus, one character at a time via CDP's Input.dispatchKeyEvent —
    /// distinct from setting an input's value via JS, which some KDP fields' auto-calculation
    /// logic (e.g. deriving every international marketplace price from a typed US list price)
    /// may be wired to genuine keystroke events rather than the value/input/change events a
    /// synthetic property-setter dispatch produces. Caller must focus the target element first
    /// (e.g. via a real click or element.focus()).</summary>
    Task TypeTextAsync(string text, CancellationToken ct);
}
