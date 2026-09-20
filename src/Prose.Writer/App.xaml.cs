using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Prose.Writer;

public partial class App : Application
{
    private static readonly string CrashLog =
        Path.Combine(Path.GetTempPath(), "prose-writer-error.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        // Wired in every build, not just DEBUG: a released desktop app that dies silently leaves
        // nothing to diagnose from, and this one is the only way its author edits their books.
        DispatcherUnhandledException += (_, args) =>
        {
            Log("DispatcherUnhandledException", args.Exception);
            MessageBox.Show($"Something went wrong:\n\n{args.Exception.Message}\n\nLogged to {CrashLog}",
                            "Prose Writer", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // recover — an unsaved beat is still in the editor
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log("AppDomain.UnhandledException", args.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log("UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        base.OnStartup(e);
    }

    private static void Log(string source, Exception? ex)
    {
        try
        {
            File.AppendAllText(CrashLog, $"[{DateTime.Now:O}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException) { /* the log is a courtesy; never let it be the thing that crashes */ }
        catch (UnauthorizedAccessException) { }
    }
}
