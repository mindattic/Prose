using Blazored.Toast.Services;
using Microsoft.JSInterop;

namespace StreetSamurai.Shared.Services;

/// <summary>
/// Wraps IToastService so every warning/error toast also logs to the browser
/// console as [SS CATEGORY-###] — toasts disappear, console doesn't.
/// Inject this instead of IToastService throughout the app.
/// </summary>
public class ToastNotifier
{
    private readonly IToastService toast;
    private readonly IJSRuntime js;

    public ToastNotifier(IToastService toast, IJSRuntime js)
    {
        this.toast = toast;
        this.js = js;
    }

    public void Warning(string code, string message)
    {
        toast.ShowWarning($"[{code}] {message}");
        Log("warn", code, message);
    }

    public void Error(string code, string message)
    {
        toast.ShowError($"[{code}] {message}");
        Log("error", code, message);
    }

    public void Info(string code, string message)
    {
        toast.ShowInfo($"[{code}] {message}");
        Log("log", code, message);
    }

    public void Success(string code, string message)
    {
        toast.ShowSuccess($"[{code}] {message}");
        Log("log", code, message);
    }

    private void Log(string level, string code, string message) =>
        _ = LogAsync(level, code, message);

    private async Task LogAsync(string level, string code, string message)
    {
        try { await js.InvokeVoidAsync($"console.{level}", $"[SS {code}] {message}"); }
        catch { /* JS unavailable during prerender */ }
    }
}
