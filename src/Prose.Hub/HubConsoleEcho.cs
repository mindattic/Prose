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
///
/// Durability fix (2026-10-07): every line here used to reach ONLY the raw console writer —
/// not the RingBufferLoggerProvider, not the Serilog `log-.txt` file sink, neither of which
/// listen to anything but `ILogger` calls. That meant the entire visible command transcript
/// vanished the moment console scrollback filled or the Hub restarted — the opposite of
/// "durable." <see cref="AttachLogger"/> is called once from Program.cs right after the DI
/// container builds, so every line below also rides the already-configured Serilog file sink
/// (14-day retention, searchable via `search_logs`) instead of a second, purpose-built sink.
/// </summary>
public static class HubConsoleEcho
{
    public static TextWriter Out { get; private set; } = Console.Out;
    public static TextWriter Error { get; private set; } = Console.Error;

    private static Microsoft.Extensions.Logging.ILogger? logger;

    /// <summary>Call exactly once, at the very top of Program.cs, before anything else can
    /// possibly redirect Console.Out/Error.</summary>
    public static void CaptureOriginal()
    {
        Out = Console.Out;
        Error = Console.Error;
    }

    /// <summary>Call exactly once, right after the DI container is built (ILoggerFactory only
    /// exists post-Build). Every line this class writes afterward also lands in the durable
    /// Serilog file sink, not just the live console window.</summary>
    public static void AttachLogger(Microsoft.Extensions.Logging.ILogger durableLogger) => logger = durableLogger;

    public static void LogIn(string source, string label, string detail)
    {
        var line = $">>> {source,-4} {label}{(detail.Length > 0 ? "  " + detail : "")}";
        Out.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
        logger?.LogInformation("{Line}", line);
    }

    public static void LogOut(string source, string label, bool success, int outputChars, double elapsedMs, string? error)
    {
        var line = $"<<< {source,-4} {label}  {(success ? "ok" : "FAIL")}  {outputChars}ch  {elapsedMs:F0}ms" +
            (string.IsNullOrWhiteSpace(error) ? "" : $"  ERROR: {Clip(error, 200)}");
        Out.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
        logger?.LogInformation("{Line}", line);
    }

    /// <summary>The Haiku-generated plain-English gloss CommandNarrator prints after a command's
    /// own LogOut line, when HubNarrationEnabled is on. Same captured writer, same durable log.</summary>
    public static void Narration(string label, string gloss)
    {
        var line = $"    ~ {label}: {gloss}";
        Out.WriteLine($"[{DateTime.Now:HH:mm:ss}] {line}");
        logger?.LogInformation("{Line}", line);
    }

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
