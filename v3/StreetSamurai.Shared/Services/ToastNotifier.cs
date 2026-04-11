using Microsoft.JSInterop;

namespace StreetSamurai.Shared.Services;

/// <summary>
/// Logs app-level notifications to the browser console as [SS CATEGORY-###].
/// Inject this throughout the app for warning/error/info/success reporting.
/// </summary>
public class ToastNotifier(IJSRuntime js)
{
    public void Warning(string code, string message) => Log("warn", code, message);
    public void Error(string code, string message) => Log("error", code, message);
    public void Info(string code, string message) => Log("log", code, message);
    public void Success(string code, string message) => Log("log", code, message);

    private void Log(string level, string code, string message) =>
        _ = LogAsync(level, code, message);

    private async Task LogAsync(string level, string code, string message)
    {
        try { await js.InvokeVoidAsync($"console.{level}", $"[SS {code}] {message}"); }
        catch { /* JS unavailable during prerender */ }
    }
}
