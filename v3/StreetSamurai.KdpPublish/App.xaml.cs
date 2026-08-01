using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreetSamurai.Core.Extensions;

namespace StreetSamurai.KdpPublish;

/// <summary>
/// Same DI shape as the CLI's <c>BuildCoreServices</c> (Host.CreateDefaultBuilder +
/// AddStreetSamuraiServices) — one registration point shared with every other
/// StreetSamurai.* front end.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

#if DEBUG
        // Log any unhandled exception anywhere in the app (WPF dispatcher + AppDomain-wide).
        var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kdppublish-error.log");
        DispatcherUnhandledException += (_, ex) =>
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:O}] DISPATCHER: {ex.Exception}\n\n");
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:O}] APPDOMAIN: {ex.ExceptionObject}\n\n");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:O}] TASK: {ex.Exception}\n\n");
            ex.SetObserved();
        };
#endif

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => services.AddStreetSamuraiServices())
            .Build();

        Services = host.Services;

        // Optional: a comma-separated NodeCode list as argv[0] auto-starts the exact same
        // RunSelectedAsync flow the panel's Start button triggers, once the manifest loads — a
        // way to exercise the real automation end-to-end without a human click, e.g.
        // `StreetSamurai.KdpPublish.exe MXG,NXR`. Not a special/shortcut code path: it drives the
        // same KdpOperatorService/KdpToolRegistry tools against the same live WebView2 pane.
        var autoRunCodes = e.Args.Length > 0 ? e.Args[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : null;
        new MainWindow(autoRunCodes).Show();
    }
}
