using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Prose.Hub;

/// <summary>One entry of the tray icon's menu. With <paramref name="Confirm"/> set, the user is asked that question first.</summary>
public sealed record HubTrayAction(string Label, Action Run, string? Confirm = null);

/// <summary>
/// The Hub's tray icon (PRS-T1021): the Hub's terminal window is kept off the taskbar, and the icon in the notification
/// area shows what the Hub is doing (tooltip and menu header) and brings the terminal up on a click. Nothing about the
/// terminal itself changes: it is the same window with the same echoed commands, only hidden until asked for.
/// Minimizing the terminal sends it back to the tray.
///
/// It only acts when this process has a console window of its own. A service host, a piped or redirected launch, a
/// console shared with a parent shell (<c>dotnet run</c>) and any other operating system are all left exactly as they
/// were, with no icon. A console window cannot be hidden when Windows Terminal is hosting it, so in that case the Hub
/// starts itself again in a classic console window (<see cref="RelaunchedInOwnWindow"/>).
///
/// The knob is one environment variable (the name is the host's): <c>0</c> turns the icon off and leaves the window
/// alone, <c>shown</c> keeps the icon but starts with the terminal visible; anything else starts hidden.
/// Plain Win32 on purpose: the Hub stays a console app with no UI framework in it.
/// </summary>
public sealed class HubTray : IDisposable
{
    private const string ChildMarker = "MINDATTIC_HUB_TRAY_CHILD";

    private readonly string name;
    private readonly bool startHidden;
    private readonly IntPtr console;
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private IntPtr window;
    private IntPtr icon;
    private uint taskbarCreated;
    private string shownTip = "";
    private bool added;
    private WndProc? proc; // kept alive: the window class holds a pointer to it

    /// <summary>The lines shown about the Hub: the tooltip takes as many as fit, the menu shows them all.</summary>
    public Func<IReadOnlyList<string>> Info { get; set; } = () => [];

    /// <summary>The menu's own commands, under Show/Hide terminal.</summary>
    public IReadOnlyList<HubTrayAction> Actions { get; set; } = [];

    private HubTray(string name, bool startHidden, IntPtr console)
    {
        this.name = name;
        this.startHidden = startHidden;
        this.console = console;
        thread = new Thread(Run) { IsBackground = true, Name = "Hub tray icon" };
    }

    /// <summary>
    /// Call first thing in Program.cs. True when the Hub's console is one that cannot be hidden (Windows Terminal is
    /// hosting it) and a copy of this process was started in a classic, hidden console window instead: the caller
    /// returns at once and that copy carries on as the Hub.
    /// </summary>
    public static bool RelaunchedInOwnWindow(string knobEnvVar, string[] args)
    {
        if (!OperatingSystem.IsWindows()) return false;
        if (IsOff(knobEnvVar) || Environment.GetEnvironmentVariable(ChildMarker) == "1") return false;
        if (OwnConsoleWindow() is not { } hwnd || ClassOf(hwnd) == "ConsoleWindowClass") return false;
        if (Environment.ProcessPath is not { Length: > 0 } exe) return false;

        var conhost = Path.Combine(Environment.SystemDirectory, "conhost.exe");
        if (!File.Exists(conhost)) return false;
        // Started through the dotnet host (a source build), the command to repeat is the host plus the Hub's own dll.
        var command = new List<string> { exe };
        if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            && System.Reflection.Assembly.GetEntryAssembly()?.Location is { Length: > 0 } dll)
            command.Add(dll);
        command.AddRange(args);
        try
        {
            Environment.SetEnvironmentVariable(ChildMarker, "1"); // inherited: the copy never relaunches again
            var psi = new ProcessStartInfo(conhost, string.Join(' ', command.Select(Quote)))
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            using var started = Process.Start(psi);
            return started is not null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            Environment.SetEnvironmentVariable(ChildMarker, null);
            return false; // carry on in the window we have, visible
        }
    }

    /// <summary>
    /// Shows the icon and hides the terminal (unless the knob says <c>shown</c>). Null, with the terminal left visible,
    /// whenever the icon is off or this process has no console window of its own that can be hidden.
    /// </summary>
    public static HubTray? Start(string name, string knobEnvVar)
    {
        if (!OperatingSystem.IsWindows()) return null;
        if (OwnConsoleWindow() is not { } hwnd) return null;
        if (IsOff(knobEnvVar) || ClassOf(hwnd) != "ConsoleWindowClass")
        {
            ShowWindow(hwnd, SW_SHOW); // a launcher may have started it hidden, expecting the icon
            return null;
        }
        var shown = string.Equals(Environment.GetEnvironmentVariable(knobEnvVar)?.Trim(), "shown", StringComparison.OrdinalIgnoreCase);
        var tray = new HubTray(name, !shown, hwnd);
        tray.thread.Start();
        tray.ready.Wait(TimeSpan.FromSeconds(5));
        return tray;
    }

    /// <summary>Brings the terminal up, e.g. when the Hub has something to say that must be seen.</summary>
    public void ShowTerminal()
    {
        if (!OperatingSystem.IsWindows()) return;
        ShowWindow(console, IsIconic(console) ? SW_RESTORE : SW_SHOW);
        SetForegroundWindow(console);
    }

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows()) return;
        if (window != IntPtr.Zero) PostMessageW(window, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    private static bool IsOff(string knobEnvVar) =>
        Environment.GetEnvironmentVariable(knobEnvVar)?.Trim() is "0" or "off" or "false";

    private static string Quote(string arg) =>
        arg.Length > 0 && !arg.Any(c => char.IsWhiteSpace(c) || c == '"') ? arg : "\"" + arg.Replace("\"", "\\\"") + "\"";

    /// <summary>This process's console window when no other process shares the console and output is not redirected; else null.</summary>
    [SupportedOSPlatform("windows")]
    private static IntPtr? OwnConsoleWindow()
    {
        try
        {
            if (Console.IsOutputRedirected) return null;
            var hwnd = GetConsoleWindow();
            if (hwnd == IntPtr.Zero) return null;
            var ids = new uint[4];
            return GetConsoleProcessList(ids, (uint)ids.Length) == 1 ? hwnd : null;
        }
        catch (Exception ex) when (ex is IOException or EntryPointNotFoundException or DllNotFoundException) { return null; }
    }

    [SupportedOSPlatform("windows")]
    private static string ClassOf(IntPtr hwnd)
    {
        var buffer = new char[64];
        var length = GetClassNameW(hwnd, buffer, buffer.Length);
        return new string(buffer, 0, Math.Max(0, length));
    }

    [SupportedOSPlatform("windows")]
    private void Run()
    {
        try
        {
            var module = GetModuleHandleW(null);
            var className = "MindAtticHubTray_" + Environment.ProcessId;
            proc = WindowProc;
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(proc),
                hInstance = module,
                lpszClassName = className,
            };
            if (RegisterClassExW(ref wc) == 0) { Fallback(); return; }
            // A real (never shown) top-level window, not a message-only one: only top-level windows are told when
            // Explorer restarts and the icon has to be added again.
            window = CreateWindowExW(WS_EX_TOOLWINDOW, className, name, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, module, IntPtr.Zero);
            if (window == IntPtr.Zero) { Fallback(); return; }
            taskbarCreated = RegisterWindowMessageW("TaskbarCreated");
            icon = LoadOwnIcon();
            if (!AddIcon()) { Fallback(); return; }

            if (startHidden) ShowWindow(console, SW_HIDE);
            else ShowWindow(console, SW_SHOW);
            SetTimer(window, 1, 1000, IntPtr.Zero);
            ready.Set();

            while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException or MarshalDirectiveException)
        {
            Fallback();
        }
        finally
        {
            ready.Set();
        }
    }

    /// <summary>No icon could be shown: the terminal must not stay hidden with nothing to bring it back.</summary>
    [SupportedOSPlatform("windows")]
    private void Fallback()
    {
        ShowWindow(console, SW_SHOW);
        ready.Set();
    }

    [SupportedOSPlatform("windows")]
    private static IntPtr LoadOwnIcon()
    {
        if (Environment.ProcessPath is { Length: > 0 } exe)
        {
            var small = new IntPtr[1];
            if (ExtractIconExW(exe, 0, null, small, 1) > 0 && small[0] != IntPtr.Zero) return small[0];
        }
        return LoadIconW(IntPtr.Zero, new IntPtr(32512)); // the stock application icon
    }

    [SupportedOSPlatform("windows")]
    private NOTIFYICONDATAW Data(uint flags) => new()
    {
        cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
        hWnd = window,
        uID = 1,
        uFlags = flags,
        uCallbackMessage = WM_TRAY,
        hIcon = icon,
        szTip = shownTip,
        szInfo = "",
        szInfoTitle = "",
    };

    [SupportedOSPlatform("windows")]
    private bool AddIcon()
    {
        shownTip = Tip();
        var data = Data(NIF_MESSAGE | NIF_ICON | NIF_TIP);
        added = Shell_NotifyIconW(NIM_ADD, ref data);
        return added;
    }

    /// <summary>The Hub's name, then as many info lines as the tooltip's 127 characters hold.</summary>
    private string Tip()
    {
        var text = name;
        foreach (var line in SafeInfo())
        {
            if (text.Length + 1 + line.Length > 127) break;
            text += "\n" + line;
        }
        return text;
    }

    private IReadOnlyList<string> SafeInfo()
    {
        try { return Info(); }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException or IOException) { return []; }
    }

    [SupportedOSPlatform("windows")]
    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case WM_TRAY:
                var what = (uint)(lParam.ToInt64() & 0xFFFF);
                if (what == WM_LBUTTONUP) Toggle();
                else if (what == WM_RBUTTONUP) Menu();
                return IntPtr.Zero;
            case WM_TIMER:
                // Minimizing the terminal sends it back to the tray, so it never sits on the taskbar.
                if (IsWindowVisible(console) && IsIconic(console)) ShowWindow(console, SW_HIDE);
                var tip = Tip();
                if (added && tip != shownTip)
                {
                    shownTip = tip;
                    var data = Data(NIF_TIP);
                    Shell_NotifyIconW(NIM_MODIFY, ref data);
                }
                return IntPtr.Zero;
            case WM_CLOSE:
                DestroyWindow(hwnd);
                return IntPtr.Zero;
            case WM_DESTROY:
                if (added)
                {
                    var data = Data(0);
                    Shell_NotifyIconW(NIM_DELETE, ref data);
                    added = false;
                }
                PostQuitMessage(0);
                return IntPtr.Zero;
        }
        if (taskbarCreated != 0 && message == taskbarCreated) AddIcon(); // Explorer restarted
        return DefWindowProcW(hwnd, message, wParam, lParam);
    }

    [SupportedOSPlatform("windows")]
    private void Toggle()
    {
        if (IsWindowVisible(console) && !IsIconic(console)) ShowWindow(console, SW_HIDE);
        else ShowTerminal();
    }

    [SupportedOSPlatform("windows")]
    private void Menu()
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        try
        {
            AppendMenuW(menu, MF_STRING | MF_GRAYED, UIntPtr.Zero, name);
            foreach (var line in SafeInfo()) AppendMenuW(menu, MF_STRING | MF_GRAYED, UIntPtr.Zero, line);
            AppendMenuW(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            var visible = IsWindowVisible(console) && !IsIconic(console);
            AppendMenuW(menu, MF_STRING, new UIntPtr(1), visible ? "Hide terminal" : "Show terminal");
            var actions = Actions;
            if (actions.Count > 0) AppendMenuW(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            for (var i = 0; i < actions.Count; i++) AppendMenuW(menu, MF_STRING, new UIntPtr((uint)(100 + i)), actions[i].Label);

            GetCursorPos(out var at);
            SetForegroundWindow(window); // or the menu stays up when the user clicks elsewhere
            var chosen = TrackPopupMenu(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, at.X, at.Y, 0, window, IntPtr.Zero);
            PostMessageW(window, 0, IntPtr.Zero, IntPtr.Zero);
            if (chosen == 1) Toggle();
            else if (chosen >= 100 && chosen - 100 < actions.Count)
            {
                var action = actions[chosen - 100];
                if (action.Confirm is { } question && MessageBoxW(window, question, name, MB_YESNO | MB_ICONQUESTION | MB_DEFBUTTON2 | MB_SETFOREGROUND) != IDYES) return;
                // Off this thread: an action may take a while, and the icon must keep answering.
                _ = Task.Run(() =>
                {
                    try { action.Run(); }
                    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException or ObjectDisposedException)
                    {
                        Console.Error.WriteLine($"[hub tray] {action.Label}: {ex.Message}");
                    }
                });
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private const int SW_HIDE = 0, SW_SHOW = 5, SW_RESTORE = 9;
    private const uint WM_DESTROY = 0x0002, WM_CLOSE = 0x0010, WM_TIMER = 0x0113, WM_LBUTTONUP = 0x0202, WM_RBUTTONUP = 0x0205, WM_TRAY = 0x8001;
    private const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4;
    private const uint MF_STRING = 0, MF_GRAYED = 1, MF_SEPARATOR = 0x800, TPM_RIGHTBUTTON = 2, TPM_RETURNCMD = 0x100;
    private const uint MB_YESNO = 4, MB_ICONQUESTION = 0x20, MB_DEFBUTTON2 = 0x100, MB_SETFOREGROUND = 0x10000, WS_EX_TOOLWINDOW = 0x80;
    private const int IDYES = 6;

    private delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID, uFlags, uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState, dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam, lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint GetConsoleProcessList([Out] uint[] ids, uint count);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandleW(string? name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(IntPtr hwnd, [Out] char[] name, int max);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll")] private static extern IntPtr DefWindowProcW(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] private static extern int GetMessageW(out MSG msg, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll")] private static extern bool PostMessageW(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessageW(string name);
    [DllImport("user32.dll")] private static extern UIntPtr SetTimer(IntPtr hwnd, UIntPtr id, uint milliseconds, IntPtr callback);
    [DllImport("user32.dll")] private static extern IntPtr LoadIconW(IntPtr instance, IntPtr name);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenuW(IntPtr menu, uint flags, UIntPtr id, string? text);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] private static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int MessageBoxW(IntPtr hwnd, string text, string caption, uint type);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIconW(uint message, ref NOTIFYICONDATAW data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern uint ExtractIconExW(string file, int index, IntPtr[]? large, IntPtr[]? small, uint count);
}
