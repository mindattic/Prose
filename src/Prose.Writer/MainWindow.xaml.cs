using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Prose.Writer;

/// <summary>
/// The whole desktop app: start the Hub if needed, then show the page it serves at /writer.
///
/// <para>Plain WebView2, not BlazorWebView. That combination was tried twice in this org
/// (Prose.KdpPublish first, then Automata followed its lead) and abandoned both times over an
/// unresolved click/interactivity bug in this exact hosting shape. Here the Blazor Server circuit
/// runs inside Prose.Hub and reaches the browser over loopback HTTP like any other web page, so
/// none of that applies.</para>
///
/// <para>This shell is also the only part of the Writer that survives the Hub dying, which is why
/// the Connect button lives here and not in the editor — see <see cref="ShowSplash"/>.</para>
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>WebView2 is initialised once and reused. Reconnecting re-navigates; it does not
    /// rebuild the environment, which would drop the browser profile and the author's session.</summary>
    private bool webViewReady;

    private bool connecting;

    /// <summary>
    /// Polls the Hub while the editor is on screen, so the window can NOTICE the Hub going away.
    ///
    /// <para>Without this, a redeploy or a Hub crash leaves the author looking at a page that is
    /// still painted but can no longer save — Blazor's own reconnect overlay covers a dropped
    /// circuit, but not a Hub that is never coming back on that circuit. Two consecutive failures
    /// are required before the splash returns, so one slow response during a heavy command does
    /// not throw the editor off screen.</para>
    /// </summary>
    private DispatcherTimer? healthTimer;
    private int consecutiveHealthFailures;
    private bool healthCheckInFlight;

    /// <summary>
    /// After the Hub is lost, watches for it to come back and reopens the editor on its own.
    ///
    /// <para>A Hub-only deploy stops the Hub, replaces Hub.exe and starts the new one, and the
    /// Writer used to sit on "Lost the connection" until the author thought to press Connect.
    /// This only polls /api/health — it never STARTS a Hub. Starting one mid-deploy would launch
    /// the old Hub.exe while the deploy is waiting to overwrite it, and lock the file it needs.
    /// Starting is left to Connect, which the author presses on purpose.</para>
    /// </summary>
    private DispatcherTimer? reconnectTimer;
    private bool reconnectCheckInFlight;

    /// <summary>The Hub process(es) still running when the connection was lost. When the health
    /// check recovers with one of THOSE still alive, the Hub never went away — it stalled (SQL, a
    /// heavy command) — and the editor is shown again as it was, not reloaded: a reload would drop
    /// the author's session and whatever they had typed since the last autosave.</summary>
    private HashSet<int> lostHubPids = [];

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoadedAsync;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async void OnConnectClick(object sender, RoutedEventArgs e) => await ConnectAsync();

    /// <summary>Find or start the Hub, then show the editor. Safe to call repeatedly — it is the
    /// launch path and the Connect button's path, deliberately the same one.</summary>
    private async Task ConnectAsync()
    {
        if (connecting) return;
        connecting = true;
        ConnectButton.IsEnabled = false;
        StopHealthWatch();
        StopReconnectWatch();

        try
        {
            ShowSplash("Looking for Prose Hub…", offerConnect: false);
            var progress = new Progress<string>(s => SplashStatus.Text = s);

            try
            {
                await HubProcess.EnsureRunningAsync(progress);
            }
            catch (Exception ex)
            {
                ShowSplash(ex.Message, offerConnect: true);
                // Keep watching: a failed attempt during a Hub redeploy used to end the automatic
                // reconnect for good, and the editor never reopened when the new Hub came up.
                StartReconnectWatch();
                return;
            }

            if (!webViewReady && !await InitialiseWebViewAsync()) return;

            SplashStatus.Text = "Opening the editor…";
            NavigateToEditor();
        }
        finally
        {
            connecting = false;
            ConnectButton.IsEnabled = true;
        }
    }

    /// <summary>Replace a WebView2 whose browser process has exited with a new, uninitialised one
    /// in the same place, and mark the view as needing setup again.</summary>
    private void ReplaceWebView()
    {
        var old = Web;
        if (old.Parent is not System.Windows.Controls.Panel host) return;
        var index = host.Children.IndexOf(old);
        var fresh = new Microsoft.Web.WebView2.Wpf.WebView2 { Name = "Web", Visibility = Visibility.Collapsed };
        host.Children.RemoveAt(index);
        host.Children.Insert(index, fresh);
        Web = fresh;
        webViewReady = false;
        try { old.Dispose(); } catch { /* already dead */ }
    }

    /// <summary>One-time WebView2 setup. Returns false having already shown the failure.</summary>
    private async Task<bool> InitialiseWebViewAsync()
    {
        try
        {
            // Keep the browser profile out of the install folder so a redeploy (which wipes and
            // republishes that folder) can't take the user's session with it.
            var profile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MindAttic", "Prose.Writer", "WebView2");
            Directory.CreateDirectory(profile);

            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            await Web.EnsureCoreWebView2Async(env);

            // This window is an app, not a browser: no context menu of page actions, no
            // Ctrl+F browser find competing with the editor's own.
            Web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Web.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            Web.CoreWebView2.Settings.IsStatusBarEnabled = false;

            // Zoom stays ENABLED (WCAG 1.4.4, Resize Text). Every font size in this UI is an
            // absolute px value, so the browser's minimum-font-size setting cannot move it
            // either — with IsZoomControlEnabled = false, as it was, the 13px chrome and 11px
            // status bar were literally unresizable and the criterion failed outright.
            //
            // AreBrowserAcceleratorKeysEnabled = false still suppresses Ctrl+plus/minus/0, so
            // Ctrl+scroll is the remaining gesture. An in-app text-size control is the proper
            // fix and is tracked separately.
            Web.CoreWebView2.Settings.IsZoomControlEnabled = true;

            // The microphone. WebView2 has no permission UI of its own in a hosted app: with
            // PermissionRequested unhandled it applies its default, which for a WPF host is to
            // DENY, silently — getUserMedia rejects and the page cannot tell a refusal from a
            // machine with no microphone. Nothing in the editor could fix that from the browser
            // side, which is why the voice loop's first blocker lives in the WPF shell.
            Web.CoreWebView2.PermissionRequested += OnPermissionRequested;

            Web.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (args.IsSuccess)
                {
                    StopReconnectWatch();
                    Splash.Visibility = Visibility.Collapsed;
                    Web.Visibility = Visibility.Visible;
                    StartHealthWatch();
                }
                else
                {
                    ShowSplash($"Could not open {HubProcess.BaseUrl}/writer — {args.WebErrorStatus}.",
                               offerConnect: true);
                    StartReconnectWatch();
                }
            };

            // The renderer itself dying leaves a blank window that looks like a frozen editor.
            // Say so, and offer the same way back.
            Web.CoreWebView2.ProcessFailed += (_, args) =>
            {
                // When the BROWSER process itself exits, this CoreWebView2 is dead for good: a
                // Connect that navigated it threw, every time. Swap in a fresh control so the next
                // Connect initialises a new one. A renderer crash leaves the browser alive, and
                // navigating again is enough.
                if (args.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
                    ReplaceWebView();
                ShowSplash($"The editor's browser process stopped ({args.ProcessFailedKind}).",
                           offerConnect: true);
                StartReconnectWatch();
            };

            webViewReady = true;
            return true;
        }
        catch (Exception ex)
        {
            ShowSplash($"WebView2 failed to start: {ex.Message}\n\n" +
                       "The Microsoft Edge WebView2 Runtime may not be installed.",
                       offerConnect: true);
            return false;
        }
    }

    /// <summary>
    /// Grant the microphone to the Hub's own page, and nothing else to anyone.
    ///
    /// <para>Allow-list rather than allow-all. This window has no address bar and only ever
    /// navigates to the local Hub, but a page it loads can still frame or redirect elsewhere, and
    /// a blanket Allow would hand the camera, the clipboard and geolocation to whatever ends up
    /// in the frame. The microphone is the only thing the editor asks for.</para>
    ///
    /// <para><c>Handled = true</c> is what suppresses WebView2's own prompt and makes
    /// <see cref="CoreWebView2PermissionRequestedEventArgs.State"/> the answer; leaving it false
    /// shows the default dialog on every recording, which is exactly the friction a press-and-hold
    /// loop cannot carry.</para>
    /// </summary>
    private static void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs args)
    {
        var fromHub =
            Uri.TryCreate(args.Uri, UriKind.Absolute, out var origin)
            && Uri.TryCreate(HubProcess.BaseUrl, UriKind.Absolute, out var hub)
            && origin.IsLoopback
            && origin.Port == hub.Port;

        args.State = fromHub && args.PermissionKind == CoreWebView2PermissionKind.Microphone
            ? CoreWebView2PermissionState.Allow
            : CoreWebView2PermissionState.Deny;

        args.Handled = true;
    }

    /// <summary>Put the splash back in front of the editor with a message, and decide whether the
    /// author is being offered a way out or merely told to wait.</summary>
    private void ShowSplash(string message, bool offerConnect)
    {
        StopHealthWatch();
        SplashStatus.Text = message;
        ConnectButton.Visibility = offerConnect ? Visibility.Visible : Visibility.Collapsed;
        ConnectButton.IsEnabled = !connecting;
        Web.Visibility = Visibility.Collapsed;
        Splash.Visibility = Visibility.Visible;
    }

    private void StartHealthWatch()
    {
        consecutiveHealthFailures = 0;
        if (healthTimer is not null) { healthTimer.Start(); return; }

        healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        healthTimer.Tick += OnHealthTick;
        healthTimer.Start();
    }

    private void StopHealthWatch()
    {
        healthTimer?.Stop();
        consecutiveHealthFailures = 0;
    }

    /// <summary>
    /// Point the editor at the Hub. Guarded because this runs from async-void handlers: if the
    /// browser process died a moment ago and ProcessFailed has not swapped the control yet,
    /// Navigate throws, and an exception escaping an async-void dispatcher callback closes the app.
    /// </summary>
    private void NavigateToEditor()
    {
        try
        {
            Web.CoreWebView2.Navigate($"{HubProcess.BaseUrl}/writer");
        }
        catch (Exception ex)
        {
            ShowSplash($"Could not open the editor: {ex.Message}", offerConnect: true);
            StartReconnectWatch();
        }
    }

    private void StartReconnectWatch()
    {
        if (reconnectTimer is not null) { reconnectTimer.Start(); return; }

        reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        reconnectTimer.Tick += OnReconnectTick;
        reconnectTimer.Start();
    }

    private void StopReconnectWatch() => reconnectTimer?.Stop();

    private async void OnReconnectTick(object? sender, EventArgs e)
    {
        if (reconnectCheckInFlight || connecting) return;
        reconnectCheckInFlight = true;

        try
        {
            if (!await HubProcess.IsHealthyAsync()) return;
            StopReconnectWatch();

            if (!webViewReady) { await ConnectAsync(); return; }

            // The same Hub came back: it stalled, it did not restart. Show the page as it was —
            // Blazor reconnects its own circuit — rather than reloading it.
            var same = lostHubPids.Count > 0 && HubProcess.RunningHubs().Any(h => lostHubPids.Contains(h.Pid));
            lostHubPids = [];
            if (same)
            {
                Splash.Visibility = Visibility.Collapsed;
                Web.Visibility = Visibility.Visible;
                StartHealthWatch();
                return;
            }

            // Navigate straight to the editor rather than through ConnectAsync: that path starts a
            // Hub when it finds none, and the one we just saw could go away again in between.
            SplashStatus.Text = "Prose Hub is back — reopening the editor…";
            ConnectButton.Visibility = Visibility.Collapsed;
            NavigateToEditor();
        }
        finally
        {
            reconnectCheckInFlight = false;
        }
    }

    private async void OnHealthTick(object? sender, EventArgs e)
    {
        // The probe has a 3s timeout and the tick is 5s, but a machine coming back from sleep can
        // still queue several — one in flight at a time is enough to answer the question.
        if (healthCheckInFlight) return;
        healthCheckInFlight = true;

        try
        {
            if (await HubProcess.IsHealthyAsync())
            {
                consecutiveHealthFailures = 0;
                return;
            }

            if (++consecutiveHealthFailures < 2) return;

            var hubs = HubProcess.RunningHubs();
            lostHubPids = hubs.Select(h => h.Pid).ToHashSet();
            ShowSplash(
                (hubs.Count > 0
                    ? $"Lost the connection to Prose Hub. It is still running " +
                      $"({string.Join(", ", hubs.Select(h => $"PID: {h.Pid}"))}) but is not answering " +
                      $"{HubProcess.BaseUrl} — most often SQL Server."
                    : "Lost the connection to Prose Hub — the process is no longer running.")
                + "\n\nThe editor reopens by itself as soon as a Hub answers again (after a Hub " +
                  "redeploy, for instance). Connect starts one now.",
                offerConnect: true);
            StartReconnectWatch();
        }
        finally
        {
            healthCheckInFlight = false;
        }
    }
}
