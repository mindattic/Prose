using System.Diagnostics;
using System.IO;
using System.Windows;
using Prose.Writer; // HubProcess, linked into this project

namespace Prose.Launcher;

/// <summary>
/// Picks one of the Prose apps and makes sure the Hub is up before handing over. Everything here
/// deploys side by side into one folder, so the apps are found relative to this executable rather
/// than by an absolute path.
/// </summary>
public partial class MainWindow : Window
{
    private static string Here => AppContext.BaseDirectory;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshHubStatusAsync();
    }

    private async Task RefreshHubStatusAsync()
    {
        if (await HubProcess.IsHealthyAsync())
        {
            HubStatus.Text = $"Prose Hub is running at {HubProcess.BaseUrl}.";
            return;
        }

        HubStatus.Text = HubProcess.FindHubExe() is null
            ? "Prose Hub is not running, and Hub.exe was not found. Deploy it first."
            : "Prose Hub is not running — it will be started when you open an app.";
    }

    private async Task<bool> EnsureHubAsync()
    {
        var progress = new Progress<string>(s => HubStatus.Text = s);
        try
        {
            await HubProcess.EnsureRunningAsync(progress);
            return true;
        }
        catch (Exception ex)
        {
            HubStatus.Text = ex.Message;
            return false;
        }
    }

    private async void OpenWriter(object sender, RoutedEventArgs e)
    {
        // The Writer starts the Hub itself, but doing it here keeps the waiting on this window
        // instead of behind a splash that has already replaced it.
        if (!await EnsureHubAsync()) return;
        if (!Launch("Writer.exe")) return;
        Close();
    }

    private async void OpenKdp(object sender, RoutedEventArgs e)
    {
        if (!await EnsureHubAsync()) return;
        if (!Launch("KdpPublish.exe")) return;
        Close();
    }

    private async void OpenHubDashboard(object sender, RoutedEventArgs e)
    {
        if (!await EnsureHubAsync()) return;
        Process.Start(new ProcessStartInfo($"{HubProcess.BaseUrl}/app") { UseShellExecute = true });
    }

    private bool Launch(string exeName)
    {
        var path = Path.Combine(Here, exeName);
        if (!File.Exists(path))
        {
            HubStatus.Text = $"{exeName} is not in {Here}. Deploy it with tools\\deploy-apps.ps1.";
            return false;
        }

        Process.Start(new ProcessStartInfo(path) { WorkingDirectory = Here, UseShellExecute = true });
        return true;
    }
}
