using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace StreetSamurai.Shared.Services;

/// <summary>
/// App-level notifications. Logs to the browser console (with the [SS CODE]
/// prefix) AND surfaces a visible toast at the bottom of the viewport so
/// errors and warnings can't be missed.
///
/// Errors and warnings are server-side logged through <see cref="ILogger"/>
/// so they end up in Serilog as well as the browser; info and success only
/// log to the browser to avoid log-spamming on routine UX feedback.
/// </summary>
public class ToastNotifier
{
    private readonly IJSRuntime js;
    private readonly ILogger<ToastNotifier> log;

    public ToastNotifier(IJSRuntime js, ILogger<ToastNotifier> log)
    {
        this.js  = js;
        this.log = log;
    }

    public void Warning(string code, string message)
    {
        log.LogWarning("[SS {Code}] {Message}", code, message);
        Show("warn",    code, message);
    }
    public void Error(string code, string message)
    {
        log.LogError("[SS {Code}] {Message}", code, message);
        Show("error",   code, message);
    }
    public void Info(string code, string message)    => Show("info",    code, message);
    public void Success(string code, string message) => Show("success", code, message);

    private void Show(string level, string code, string message)
    {
        _ = ShowAsync(level, code, message);
    }

    private async Task ShowAsync(string level, string code, string message)
    {
        // Always console-log first so a toast UI failure still leaves a record.
        var consoleLevel = level switch { "error" => "error", "warn" => "warn", _ => "log" };
        try { await js.InvokeVoidAsync($"console.{consoleLevel}", $"[SS {code}] {message}"); }
        catch { /* JS unavailable during prerender */ return; }

        // Pop the visible toast. ssToasts.show is defined in wwwroot/js/toasts.js
        // and is a no-op-fallback if that file isn't loaded yet.
        try { await js.InvokeVoidAsync("ssToasts.show", level, code, message); }
        catch { /* swallow — already logged */ }
    }
}
