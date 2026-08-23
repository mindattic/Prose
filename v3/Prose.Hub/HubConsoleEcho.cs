namespace Prose.Hub;

/// <summary>
/// Explicit user requirement (2026-08-21): the Hub must run in a visible console window and
/// print every command's inputs/outputs there, so a human watching the window can see it's
/// alive and working — not just a health-check dot.
///
/// CliDispatch/ToolDispatch redirect the process-wide <c>Console.Out</c>/<c>Console.Error</c>
/// to a per-call <see cref="StringWriter"/> for the duration of a handler invocation (that's
/// the entire reason <c>ConsoleGate</c> exists — see CliDispatch's own doc comment). Writing an
/// echo line via the ambient <c>Console.Out</c> from anywhere outside that exact redirected
/// window is NOT safe: if a second call is concurrently mid-invoke, the ambient
/// <c>Console.Out</c> at that instant is actually THAT call's StringWriter, and the echo line
/// would corrupt its captured output instead of reaching the visible window.
///
/// This class captures the real console writers exactly once, at Hub startup, before any
/// command has ever redirected them — so echo lines always reach the actual window regardless
/// of what any concurrent command currently has <c>Console.Out</c> pointed at.
/// </summary>
public static class HubConsoleEcho
{
    public static TextWriter Out { get; private set; } = Console.Out;
    public static TextWriter Error { get; private set; } = Console.Error;

    /// <summary>Call exactly once, at the very top of Program.cs, before anything else can
    /// possibly redirect Console.Out/Error.</summary>
    public static void CaptureOriginal()
    {
        Out = Console.Out;
        Error = Console.Error;
    }

    public static void LogIn(string source, string label, string detail) =>
        Out.WriteLine($"[{DateTime.Now:HH:mm:ss}] >>> {source,-4} {label}{(detail.Length > 0 ? "  " + detail : "")}");

    public static void LogOut(string source, string label, bool success, int outputChars, double elapsedMs, string? error) =>
        Out.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] <<< {source,-4} {label}  {(success ? "ok" : "FAIL")}  {outputChars}ch  {elapsedMs:F0}ms" +
            (string.IsNullOrWhiteSpace(error) ? "" : $"  ERROR: {Clip(error, 200)}"));

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
