using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;

// KdpFilePicker (--watch [--manifest <path>] | <full-path-to-file> [--timeout <seconds>])
//
// A native-side helper for the one step of the KDP republish flow no browser script or agent
// can do: selecting a file inside a native OS "Open" dialog. That dialog is outside the browser's
// sandbox, so it can only be driven by something running outside it too — this app uses UI
// Automation (the same accessibility API screen readers and legitimate automation tools use) to
// watch for the dialog, type the target path into its filename field, click Open, then exit.
//
// --watch: launch ONCE per session, before handing the batch prompt to Claude for Chrome. It
// loads tools/kdp/manifest.json, builds the same ordered "needs republish" queue the sidebar's
// batch prompt uses, and silently fills every upload dialog as it appears, one queue entry at a
// time — no per-book command to run. Type "skip" + Enter if the agent reports it couldn't find a
// book (keeps the queue in sync with reality); Ctrl+C to stop early.
//
// Single-file mode (a path as the first argument): unchanged one-shot behavior — waits for the
// next dialog, fills it with that one path, exits. Useful for manual use/testing.

if (args.Length == 0 || args[0] is "-h" or "--help" or "/?")
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  KdpFilePicker --watch [--manifest <path>]   Continuous: fills every upload");
    Console.WriteLine("                                              dialog against the ordered");
    Console.WriteLine("                                              needs-republish queue.");
    Console.WriteLine("  KdpFilePicker <full-path-to-file> [--timeout <seconds>]");
    Console.WriteLine("                                              One-shot: fills the next dialog");
    Console.WriteLine("                                              with this one file, then exits.");
    return 1;
}

if (args[0] == "--watch")
{
    string? manifestPath = null;
    var processFilter = "chrome";
    for (var i = 1; i < args.Length - 1; i++)
    {
        if (args[i] == "--manifest") manifestPath = args[++i];
        if (args[i] == "--process") processFilter = args[++i];
    }
    manifestPath ??= FindDefaultManifestPath();

    if (manifestPath == null || !File.Exists(manifestPath))
    {
        Console.Error.WriteLine($"[kdp-file-picker] Manifest not found: {manifestPath ?? "(none)"}. Run `prose --kdp-manifest` first, or pass --manifest <path>.");
        return 1;
    }

    return RunWatchMode(manifestPath, processFilter);
}

return RunOneShot(args);

// ── one-shot mode ────────────────────────────────────────────────────────────────────────────
static int RunOneShot(string[] args)
{
    var targetPath = Path.GetFullPath(args[0]);
    var timeoutSeconds = 30;
    var processFilter = "chrome";
    for (var i = 1; i < args.Length - 1; i++)
    {
        if (args[i] == "--timeout" && int.TryParse(args[i + 1], out var t)) timeoutSeconds = t;
        if (args[i] == "--process") processFilter = args[++i];
    }

    if (!File.Exists(targetPath))
    {
        Console.Error.WriteLine($"[kdp-file-picker] File not found: {targetPath}");
        return 1;
    }

    Console.WriteLine($"[kdp-file-picker] Target file: {targetPath}");
    Console.WriteLine($"[kdp-file-picker] Waiting up to {timeoutSeconds}s for a file-open dialog owned by '{processFilter}'...");

    var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
    var seen = new HashSet<IntPtr>();
    IntPtr dialogHwnd;
    do
    {
        dialogHwnd = FindNewOpenDialog(seen, processFilter);
        if (dialogHwnd != IntPtr.Zero) break;
        Thread.Sleep(200);
    } while (DateTime.UtcNow < deadline);

    if (dialogHwnd == IntPtr.Zero)
    {
        Console.Error.WriteLine("[kdp-file-picker] Timed out — no file dialog appeared.");
        return 1;
    }

    Console.WriteLine($"[kdp-file-picker] Dialog found (hwnd=0x{dialogHwnd:X}). Filling filename and confirming...");
    try
    {
        FillAndConfirm(dialogHwnd, targetPath);
        Console.WriteLine("[kdp-file-picker] Done — clicked Open. Exiting.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[kdp-file-picker] Failed: {ex.Message}");
        return 1;
    }
}

// ── watch mode ───────────────────────────────────────────────────────────────────────────────
static int RunWatchMode(string manifestPath, string processFilter)
{
    var queue = LoadQueue(manifestPath);
    if (queue.Count == 0)
    {
        Console.WriteLine("[kdp-file-picker] Manifest has no books needing republish. Nothing to watch for.");
        return 0;
    }

    Console.WriteLine($"[kdp-file-picker] Watching for '{processFilter}'-owned dialogs only. Expecting {queue.Count} upload(s) in this order:");
    foreach (var b in queue) Console.WriteLine($"  - {b.Code}");
    Console.WriteLine("[kdp-file-picker] Type \"skip\" + Enter if the agent reports it couldn't find the");
    Console.WriteLine("[kdp-file-picker] next book (keeps this queue in sync). Ctrl+C to stop early.");
    Console.WriteLine("[kdp-file-picker] IMPORTANT: don't open any other file dialogs while this is running —");
    Console.WriteLine($"[kdp-file-picker] it will grab the first open dialog owned by any '{processFilter}' process,");
    Console.WriteLine("[kdp-file-picker] including ones you open yourself in another browser tab/window.");

    var commands = new ConcurrentQueue<string>();
    var stdinThread = new Thread(() =>
    {
        string? line;
        // TrimStart('﻿') guards against a leading BOM — some redirected-stdin writers
        // (e.g. .NET's default UTF8 StreamWriter, used if this is ever driven by a script rather
        // than a human typing into a real terminal) emit one on the first write, which plain
        // Trim() does not strip and would otherwise silently break the "skip" match forever.
        while ((line = Console.ReadLine()) != null)
            commands.Enqueue(line.Trim().TrimStart('﻿').ToLowerInvariant());
    });
    stdinThread.IsBackground = true;
    stdinThread.Start();

    var index = 0;
    var seen = new HashSet<IntPtr>();

    while (index < queue.Count)
    {
        if (commands.TryDequeue(out var cmd) && cmd == "skip")
        {
            Console.WriteLine($"[kdp-file-picker] Skipped {queue[index].Code} per user command.");
            index++;
            continue;
        }

        var dialogHwnd = FindNewOpenDialog(seen, processFilter);
        if (dialogHwnd == IntPtr.Zero) { Thread.Sleep(300); continue; }

        var current = queue[index];
        Console.WriteLine($"[kdp-file-picker] Dialog appeared — filling for {current.Code} ({current.StagedPath})...");
        try
        {
            FillAndConfirm(dialogHwnd, current.StagedPath);
            Console.WriteLine($"[kdp-file-picker] Done: {current.Code}. {queue.Count - index - 1} remaining.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[kdp-file-picker] Failed on {current.Code}: {ex.Message} — leaving dialog for manual handling.");
        }
        index++;
    }

    Console.WriteLine("[kdp-file-picker] Queue empty — all expected uploads handled. Exiting.");
    return 0;
}

static string? FindDefaultManifestPath()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        dir = dir.Parent;
    return dir == null ? null : Path.Combine(dir.FullName, "tools", "kdp", "manifest.json");
}

static List<QueuedBook> LoadQueue(string manifestPath)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
    var result = new List<QueuedBook>();
    foreach (var el in doc.RootElement.EnumerateArray())
    {
        var needsRepublish = el.TryGetProperty("needsRepublish", out var nr) && nr.GetBoolean();
        if (!needsRepublish) continue;
        var code = el.GetProperty("code").GetString() ?? "";
        var staged = el.TryGetProperty("stagedPath", out var sp) ? sp.GetString() : null;
        if (string.IsNullOrWhiteSpace(staged) || !File.Exists(staged)) continue;
        result.Add(new QueuedBook(code, staged));
    }
    return result;
}

// ── window discovery ─────────────────────────────────────────────────────────────────────────
// Tracks hwnds already handled/attempted so the same dialog isn't re-processed on every poll —
// important in watch mode, where the loop runs for the whole session, not just one dialog.
//
// Scoped to dialogs owned by a specific process image (default "chrome") — #32770 is the
// generic Windows common-dialog class, and a real desktop can have MANY unrelated apps with a
// file dialog sitting open (Paint, Photoshop, Excel, Explorer...). An unscoped watcher will
// happily grab the first one it finds anywhere on the system and feed it the wrong file —
// confirmed against this exact machine during testing (a stray non-Chrome dialog was consumed
// before the intended one). Scoping by owning process is a real safety requirement, not just
// tidiness.
static IntPtr FindNewOpenDialog(HashSet<IntPtr> seen, string processFilter)
{
    seen.RemoveWhere(h => !NativeMethods.IsWindowVisible(h));
    var found = IntPtr.Zero;
    NativeMethods.EnumWindows((hwnd, _) =>
    {
        if (seen.Contains(hwnd) || !NativeMethods.IsWindowVisible(hwnd)) return true;
        // "#32770" is the standard Windows common-dialog window class, used by both the legacy
        // GetOpenFileName dialogs and the modern Explorer-style IFileOpenDialog Chrome shows.
        if (GetClassName(hwnd) != "#32770") return true;
        if (!OwnedByProcess(hwnd, processFilter)) return true;

        found = hwnd;
        seen.Add(hwnd);
        return false;
    }, IntPtr.Zero);
    return found;
}

static bool OwnedByProcess(IntPtr hwnd, string processFilter)
{
    try
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        using var proc = Process.GetProcessById((int)pid);
        return proc.ProcessName.Equals(processFilter, StringComparison.OrdinalIgnoreCase);
    }
    catch
    {
        return false; // process exited mid-check, or access denied — treat as not-a-match
    }
}

static string GetClassName(IntPtr hwnd)
{
    var sb = new StringBuilder(256);
    NativeMethods.GetClassName(hwnd, sb, sb.Capacity);
    return sb.ToString();
}

// ── fill + confirm via UI Automation ─────────────────────────────────────────────────────────
static void FillAndConfirm(IntPtr dialogHwnd, string path)
{
    var dialog = AutomationElement.FromHandle(dialogHwnd)
        ?? throw new InvalidOperationException("Could not get an AutomationElement for the dialog.");

    // AutomationId "1148" is the well-known id of the filename combo/edit box in the modern
    // Windows Common Item Dialog (IFileDialog) — stable across Windows versions. Falls back to
    // "the first focusable Edit control" for older-style dialogs that don't expose that id.
    var fileNameBox =
        dialog.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "1148"))
        ?? dialog.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
            new PropertyCondition(AutomationElement.IsKeyboardFocusableProperty, true)));

    if (fileNameBox == null)
        throw new InvalidOperationException("Could not find the filename field in the dialog.");
    if (!fileNameBox.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObj))
        throw new InvalidOperationException("Filename field does not support the Value pattern.");

    // Setting .Value alone updates the displayed text but does NOT reliably update the dialog's
    // internal "which file is actually selected" state — invoking the Open button afterward can
    // act on whatever the list view last had highlighted (e.g. Explorer's remembered selection
    // for that folder) instead of the path we just typed. Real keyboard commit (focus + Enter) is
    // what a human does and is what the dialog actually listens to for "open this typed path".
    fileNameBox.SetFocus();
    Thread.Sleep(150);
    ((ValuePattern)valuePatternObj).SetValue(path);
    Thread.Sleep(150);
    NativeMethods.SendEnterKey();
}

internal record QueuedBook(string Code, string StagedPath);

internal static class NativeMethods
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const byte VK_RETURN = 0x0D;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>Sends Enter to whatever control currently has real OS keyboard focus — must be
    /// called right after <c>AutomationElement.SetFocus()</c> on the intended target.</summary>
    public static void SendEnterKey()
    {
        keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
