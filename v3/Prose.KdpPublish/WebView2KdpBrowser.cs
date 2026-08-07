using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Prose.Core.Services.Operator;

namespace Prose.KdpPublish;

/// <summary>Real implementation of <see cref="IKdpBrowser"/> against a live WebView2 pane.</summary>
public class WebView2KdpBrowser : IKdpBrowser
{
    private readonly CoreWebView2 core;

    public WebView2KdpBrowser(CoreWebView2 core)
    {
        this.core = core;
    }

    public string CurrentUrl => core.Source;

    // ExecuteScriptAsync/CallDevToolsProtocolMethodAsync take no CancellationToken and have no
    // built-in timeout — if the renderer's JS thread is ever blocked (a native alert/confirm
    // dialog, a print dialog, anything else that steals the message loop), the awaited call
    // simply never returns. Confirmed as the real cause of a full-sweep run silently freezing at
    // ~0% CPU for 100+ minutes with no exception, no log line, nothing — the ScriptDialogOpening
    // auto-accept handler (see MainWindow) closes the specific dialog case, but this hard
    // per-call timeout is the actual backstop: whatever the cause, a single tool call can now
    // never wedge the whole book (and therefore the whole sweep) forever again.
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);

    public async Task<string> EvalAsync(string script, CancellationToken ct)
    {
        var raw = await WithTimeout(core.ExecuteScriptAsync(script), "ExecuteScriptAsync", ct);
        // ExecuteScriptAsync JSON-encodes the JS expression's result. Every KDP tool script
        // returns JSON.stringify(...) (a JS string), so `raw` is a JSON-encoded STRING —
        // unwrap that one level so callers get the plain JSON text they expect to re-parse.
        return JsonSerializer.Deserialize<string>(raw) ?? raw;
    }

    private static async Task<T> WithTimeout<T>(Task<T> task, string what, CancellationToken ct)
    {
        var winner = await Task.WhenAny(task, Task.Delay(CallTimeout, ct));
        if (winner != task)
            throw new TimeoutException($"{what} did not respond within {CallTimeout.TotalSeconds}s — the KDP page may be showing a blocking dialog or is otherwise unresponsive.");
        return await task;
    }

    public Task InjectFileAsync(string filePath, string selector, CancellationToken ct)
        => DomFileInjector.InjectAsync(core, filePath, selector);

    /// <summary>
    /// Dispatches a REAL mouse click at a viewport point via CDP Input.dispatchMouseEvent —
    /// distinct from calling .click() on an element in JS. Confirmed necessary live: KDP's
    /// confirmation "checkbox" is a custom &lt;div role=checkbox&gt; React widget whose
    /// aria-checked stayed false no matter how many times el.click() was called on it from JS
    /// (a synthetic, untrusted click event); some component libraries specifically ignore
    /// untrusted events to prevent programmatic manipulation. A CDP-dispatched mouse event is a
    /// genuine trusted input event indistinguishable from a real user click.
    /// </summary>
    public async Task ClickAtPointAsync(double x, double y, CancellationToken ct)
    {
        var pressParams = JsonSerializer.Serialize(new { type = "mousePressed", x, y, button = "left", clickCount = 1 });
        await WithTimeout(core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", pressParams), "CallDevToolsProtocolMethodAsync(mousePressed)", ct);
        var releaseParams = JsonSerializer.Serialize(new { type = "mouseReleased", x, y, button = "left", clickCount = 1 });
        await WithTimeout(core.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", releaseParams), "CallDevToolsProtocolMethodAsync(mouseReleased)", ct);
    }

    /// <summary>
    /// Types each character via CDP Input.dispatchKeyEvent (keyDown carrying the character in
    /// its "text" field, then keyUp) — the same real-trusted-input technique as
    /// <see cref="ClickAtPointAsync"/>, applied to the keyboard instead of the mouse. Added after
    /// a specific concern about KDP's Pricing page: the international-marketplace price fields
    /// may only recalculate off genuine keystroke events on the US list-price field, not off a
    /// value set via a synthetic property-setter + dispatched input/change events (the technique
    /// that works fine for every other text field in this app). Targets whatever element
    /// currently has focus — the caller must focus the field first.
    /// </summary>
    public async Task TypeTextAsync(string text, CancellationToken ct)
    {
        foreach (var c in text)
        {
            var keyDownParams = JsonSerializer.Serialize(new { type = "keyDown", text = c.ToString(), unmodifiedText = c.ToString() });
            await WithTimeout(core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyDownParams), "CallDevToolsProtocolMethodAsync(keyDown)", ct);
            var keyUpParams = JsonSerializer.Serialize(new { type = "keyUp", text = c.ToString(), unmodifiedText = c.ToString() });
            await WithTimeout(core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyUpParams), "CallDevToolsProtocolMethodAsync(keyUp)", ct);
            await Task.Delay(40, ct);
        }
    }
}
