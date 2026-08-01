using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using StreetSamurai.Core.Services;
using StreetSamurai.Core.Services.Operator;

namespace StreetSamurai.KdpPublish;

/// <summary>
/// Both panes are plain WebView2 + vanilla JS. The control panel (wwwroot/panel.html) was
/// originally BlazorWebView + Razor components, but that combination had an unresolved click/
/// interactivity bug in this hosting setup — extensively diagnosed (SDK choice, package version
/// alignment, TFM/WinRT projections, stale WebView2 profile cache, window-resize "airspace",
/// running two WebView2-based controls in one window, the Razor source generator flag, and
/// finally `autostart="false"` matching the official MAUI Blazor Hybrid template — none of it
/// fixed it, and the last one broke rendering entirely). A raw HTML button with a plain
/// `onclick` in the exact same page worked instantly, proving the bug was specific to Blazor's
/// own event delegation, not the WebView2/WPF hosting. Plain WebView2 + vanilla JS is the
/// pattern already proven twice elsewhere in this app (this file's KdpBrowser pane, and the
/// browser-extension sidebar's kdp-panel.template.js) — using it here too instead of continuing
/// to chase the Blazor bug.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private List<KdpManifestEntry> lastManifest = new();
    private IKdpBrowser? kdpBrowser;
    private CancellationTokenSource? runCts;
    private readonly HashSet<string>? autoRunCodes;
    private bool autoRunTriggered;

    public MainWindow(string[]? autoRunCodes = null)
    {
        InitializeComponent();
        this.autoRunCodes = autoRunCodes is { Length: > 0 } ? autoRunCodes.ToHashSet() : null;

#if DEBUG
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.F11) ControlPanel.CoreWebView2?.OpenDevToolsWindow();
            if (e.Key == Key.F10) KdpBrowser.CoreWebView2?.OpenDevToolsWindow();
        };
#endif

        _ = InitializeControlPanelAsync();
        _ = InitializeKdpBrowserAsync();
    }

    private async Task InitializeControlPanelAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MindAttic", "KdpPublish", "ControlPanelWebView2");
        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await ControlPanel.EnsureCoreWebView2Async(env);

        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        ControlPanel.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "kdppublish.local", wwwroot, CoreWebView2HostResourceAccessKind.Allow);
        ControlPanel.CoreWebView2.WebMessageReceived += OnControlPanelMessage;
        ControlPanel.CoreWebView2.Navigate("https://kdppublish.local/panel.html");
    }

    /// <summary>
    /// A dedicated user-data folder (separate from any installed Chrome/Edge profile) means the
    /// Amazon login persists across app restarts without touching the user's regular browser
    /// profile at all — first run needs a real login, every run after that doesn't.
    /// </summary>
    private async Task InitializeKdpBrowserAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MindAttic", "KdpPublish", "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await KdpBrowser.EnsureCoreWebView2Async(env);

        // Any "open in new window" request (target="_blank", window.open(), etc.) redirects
        // back into this same pane instead of spawning an untracked standalone popup window —
        // confirmed live: KDP's page opened one of these mid-run (a CreateSpace-transfer link),
        // and by default WebView2 falls back to a separate top-level msedgewebview2 window that
        // neither this app nor the operator loop has any visibility into or control over.
        KdpBrowser.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            KdpBrowser.CoreWebView2.Navigate(args.Uri);
        };

        // Any native alert()/confirm()/beforeunload dialog the KDP page raises would otherwise
        // show as an unattended modal on top of this pane with nobody to click it — and every
        // ExecuteScriptAsync/CDP call the tools issue afterward hangs forever waiting for the
        // blocked renderer, with no timeout anywhere to break it. Confirmed as the real cause of
        // a full-sweep run silently freezing at 0% CPU for 100+ minutes (still "Responding" per
        // Windows since the WPF message pump itself was never blocked, only the renderer's JS
        // thread was). Auto-accepting keeps the automation's own "fully unattended through
        // Publish" design intact instead of depending on a human happening to notice and click it.
        KdpBrowser.CoreWebView2.ScriptDialogOpening += (_, args) =>
        {
            _ = PostLogAsync($"⚠ KDP page raised a {args.Kind} dialog: \"{args.Message}\" — auto-accepting.");
            args.Accept();
        };

        KdpBrowser.CoreWebView2.Navigate("https://kdp.amazon.com/en_US/bookshelf");

        kdpBrowser = new WebView2KdpBrowser(KdpBrowser.CoreWebView2);
    }

    private async void OnControlPanelMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        JsonNode? msg;
        try { msg = JsonNode.Parse(args.WebMessageAsJson); }
        catch { return; }
        var action = msg?["action"]?.GetValue<string>();

        try
        {
            switch (action)
            {
                case "ready":
                    await RefreshManifestAsync();
                    if (autoRunCodes != null && !autoRunTriggered)
                    {
                        autoRunTriggered = true;
                        await PostLogAsync($"Auto-run requested via command line: {string.Join(", ", autoRunCodes)}");
                        // The control panel (this message) and the KDP browser pane initialize
                        // concurrently — the panel usually finishes first, so give the KDP pane
                        // (navigate to bookshelf + WebView2 env creation) a little room to catch
                        // up rather than aborting on a kdpBrowser==null race.
                        for (var i = 0; i < 50 && kdpBrowser == null; i++)
                            await Task.Delay(200);
                        _ = RunSelectedAsync(autoRunCodes);
                    }
                    break;
                case "start":
                    var codes = msg!["codes"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
                    _ = RunSelectedAsync(codes);
                    break;
                case "mark-unpublished":
                    var unpublishCodes = msg!["codes"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
                    await MarkUnpublishedAsync(unpublishCodes);
                    break;
            }
        }
        catch (Exception ex)
        {
            await PostLogAsync($"⚠ Control panel message '{action}' failed: {ex.Message}");
        }
    }

    private async Task RefreshManifestAsync()
    {
        var manifestService = App.Services.GetRequiredService<KdpManifestService>();
        lastManifest = await manifestService.BuildAsync(KdpManifestService.FindRepoRoot());
        var json = JsonSerializer.Serialize(lastManifest, JsonOpts);
        // json becomes a JS string ARGUMENT here — encode it as a JS string literal (the page's
        // onManifest does JSON.parse on it), not inline it as a JS object literal.
        var jsArg = JsonSerializer.Serialize(json);
        await ControlPanel.CoreWebView2.ExecuteScriptAsync($"window.ssPanel.onManifest({jsArg})");
        await PostLogAsync($"Loaded manifest — {lastManifest.Count} tracked, {lastManifest.Count(e => e.NeedsRepublish)} outdated.");
    }

    private async Task RunSelectedAsync(HashSet<string> codes)
    {
        if (kdpBrowser == null)
        {
            await PostLogAsync("⚠ KDP browser pane isn't ready yet — wait for it to finish loading and try again.");
            return;
        }

        runCts = new CancellationTokenSource();
        await SetRunningAsync(true);

        var operatorService = App.Services.GetRequiredService<KdpOperatorService>();
        var ctx = new KdpOperatorContext { Browser = kdpBrowser };
        var toRun = lastManifest.Where(e => codes.Contains(e.Code)).ToList();
        await PostLogAsync($"Starting run: {toRun.Count} book(s) — {string.Join(", ", toRun.Select(e => e.Code))}");

        foreach (var book in toRun)
        {
            if (runCts.IsCancellationRequested) break;
            await PostLogAsync($"— {book.Code} — {book.Title} —");
            try
            {
                await foreach (var evt in operatorService.ProcessBookAsync(book, ctx, runCts.Token))
                    await PostLogAsync(FormatEvent(book.Code, evt));
            }
            catch (OperationCanceledException)
            {
                await PostLogAsync($"{book.Code}: cancelled.");
                break;
            }
            catch (Exception ex)
            {
                await PostLogAsync($"{book.Code}: unexpected failure — {ex.Message}");
            }

            // Re-pull the manifest so a book that just got mark_published'd immediately shows
            // its real status (no longer flagged Outdated) instead of waiting for a manual
            // Refresh click. onManifest preserves the current selection on this non-first load.
            await RefreshManifestAsync();
        }

        await PostLogAsync("Run finished.");
        await SetRunningAsync(false);
    }

    /// <summary>
    /// The "Mark Unpublished" panel action — clears PublicationStatus/KdpPublishedAt in the DB
    /// (via <see cref="KdpMarkPublishedService.UnmarkPublishedAsync"/>) for every selected code,
    /// then refreshes the manifest so the panel immediately shows those rows as needing a
    /// republish. Lets the user force a redo of a book Start would otherwise skip via the
    /// version pre-check (e.g. to re-verify the pipeline, or recover from a bad prior publish).
    /// </summary>
    private async Task MarkUnpublishedAsync(HashSet<string> codes)
    {
        var markService = App.Services.GetRequiredService<KdpMarkPublishedService>();
        var count = await markService.UnmarkPublishedAsync(codes);
        await PostLogAsync($"Marked {count} book(s) unpublished: {string.Join(", ", codes)}");
        await RefreshManifestAsync();
    }

    private async Task PostLogAsync(string line)
    {
        var jsArg = JsonSerializer.Serialize($"[{DateTime.Now:HH:mm:ss}] {line}");
        await ControlPanel.CoreWebView2.ExecuteScriptAsync($"window.ssPanel.onLog({jsArg})");
    }

    private async Task SetRunningAsync(bool running)
        => await ControlPanel.CoreWebView2.ExecuteScriptAsync($"window.ssPanel.onRunState({(running ? "true" : "false")})");

    private static string FormatEvent(string code, OperatorEvent evt) => evt switch
    {
        OperatorEvent.AssistantText t => $"{code}: {t.Text}",
        OperatorEvent.ToolStarted s => $"{code}: → {s.Name}({Truncate(s.ArgsJson, 120)})",
        OperatorEvent.ToolCompleted c => $"{code}: {(c.IsError ? "✗" : "✓")} {c.Name} → {Truncate(c.ResultJson, 160)}",
        OperatorEvent.Error e => $"{code}: ⚠ {e.Message}",
        _ => $"{code}: {evt}",
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
