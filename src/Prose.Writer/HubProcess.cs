using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace Prose.Writer;

/// <summary>
/// Finds the Hub, and starts it if it isn't answering.
///
/// <para>The Hub is not optional. Every byte of book text this app displays or saves comes from
/// it, and it is the only process permitted to touch the database — so rather than fail with
/// "cannot connect", the window starts the Hub and waits. This mirrors what
/// <c>.prose\hooks\start-prose-hub.ps1</c> does for a Claude Code session.</para>
/// </summary>
public static class HubProcess
{
    public const string BaseUrl = "http://127.0.0.1:5900";
    private const string HealthUrl = BaseUrl + "/api/health";

    /// <summary>Install locations tried in order. The first is where deploy.ps1 puts the Hub
    /// today; the second is the pre-2026-09 layout, still present on machines that have not been
    /// redeployed since.</summary>
    private static readonly string[] CandidateHubPaths =
    [
        @"C:\Apps\MindAttic\Prose\Hub.exe",
        @"C:\Apps\Prose\Prose.Hub\Prose.Hub.exe"
    ];

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(3) };

    /// <summary>True when the Hub answers /api/health with 200. That endpoint is fail-closed —
    /// it returns 503 when the database is unreachable — so a 200 means genuinely usable, not
    /// merely "a process is listening".</summary>
    public static async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var resp = await Http.GetAsync(HealthUrl);
            return resp.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    public static string? FindHubExe() => CandidateHubPaths.FirstOrDefault(File.Exists);

    /// <summary>A Hub process found on this machine. <paramref name="ExePath"/> is null when the
    /// path could not be read (another user's process, or it exited mid-enumeration).</summary>
    public sealed record HubInstance(int Pid, string? ExePath);

    /// <summary>
    /// Every running Hub process, by PID — the "search" behind the Connect button.
    ///
    /// <para>Matched by executable path, not by process name alone: "Hub" is generic enough to
    /// collide with something unrelated, which is the same reason <c>deploy-apps.ps1</c> stops its
    /// targets by path. A process whose path cannot be read is still reported, because a Hub we
    /// can see but cannot identify is exactly the situation the author needs told about rather
    /// than silently filtered away.</para>
    ///
    /// <para>The PID this returns is the one the Hub prints in its own window title
    /// (<c>Hub - PID: {pid}</c>), so the two can be matched up by eye.</para>
    /// </summary>
    public static IReadOnlyList<HubInstance> RunningHubs()
    {
        var found = new List<HubInstance>();

        foreach (var name in new[] { "Hub", "Prose.Hub" })
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(name); }
            catch (InvalidOperationException) { continue; }

            foreach (var process in processes)
            {
                try
                {
                    string? path = null;
                    try { path = process.MainModule?.FileName; }
                    catch (Exception) { /* access denied, or it exited — report it unidentified */ }

                    var isOurs = path is null
                        || CandidateHubPaths.Any(c => string.Equals(c, path, StringComparison.OrdinalIgnoreCase));
                    if (isOurs) found.Add(new HubInstance(process.Id, path));
                }
                finally { process.Dispose(); }
            }
        }

        return found;
    }

    /// <summary>Poll until the Hub answers, or give up. Startup includes an EF migration check
    /// against SQL Server, so this is seconds, not milliseconds.</summary>
    private static async Task<bool> WaitForHealthyAsync(string waitingFor, IProgress<string>? progress,
                                                        CancellationToken ct, int attempts = 40)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(500, ct);
            if (await IsHealthyAsync()) return true;
            if (attempt % 4 == 3) progress?.Report($"{waitingFor}… ({(attempt + 1) / 2}s)");
        }
        return false;
    }

    /// <summary>
    /// Returns once the Hub is healthy, or throws with something the user can act on.
    /// <paramref name="progress"/> receives status lines for the splash.
    ///
    /// <para>Looks for an existing Hub process before starting one. That distinction matters: a Hub
    /// that is merely still booting, or that is up but cannot reach SQL Server, is NOT absent, and
    /// launching a second one against the same hard-coded port 127.0.0.1:5900 turns a clear problem
    /// ("the database is down") into a confusing one ("two Hubs, one of which cannot bind").</para>
    /// </summary>
    public static async Task EnsureRunningAsync(IProgress<string>? progress = null,
                                                CancellationToken ct = default)
    {
        progress?.Report("Looking for Prose Hub…");
        if (await IsHealthyAsync()) { progress?.Report("Hub is up."); return; }

        var existing = RunningHubs();
        if (existing.Count > 0)
        {
            var ids = string.Join(", ", existing.Select(h => $"PID: {h.Pid}"));
            progress?.Report($"Found Prose Hub ({ids}) — waiting for it to answer…");

            if (await WaitForHealthyAsync("Waiting for Prose Hub", progress, ct)) { progress?.Report("Hub is up."); return; }

            throw new InvalidOperationException(
                $"Prose Hub is running ({ids}) but is not answering on {BaseUrl}.\n\n" +
                "/api/health is fail-closed — it returns 503 when SQL Server is unreachable — so " +
                "the usual cause is the database, not the Hub. Check that Hub window's own console " +
                "for the error it printed, then press Connect again.\n\n" +
                "A second Hub was deliberately NOT started: both would fight over port 5900.");
        }

        var exe = FindHubExe()
            ?? throw new InvalidOperationException(
                "Prose Hub is not running and Hub.exe could not be found. Looked in:\n  " +
                string.Join("\n  ", CandidateHubPaths) +
                "\n\nDeploy it with src\\Prose.Hub\\tools\\deploy.ps1.");

        progress?.Report("Starting Prose Hub…");
        Process.Start(new ProcessStartInfo(exe)
        {
            // The Hub pins its own ContentRoot to the executable's folder, but set the working
            // directory anyway: anything else it resolves relatively should also land beside it.
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = true
        });

        if (await WaitForHealthyAsync("Waiting for Prose Hub", progress, ct)) { progress?.Report("Hub is up."); return; }

        throw new InvalidOperationException(
            $"Started {exe} but it never became healthy.\n\n" +
            "The Hub reports unhealthy when it cannot reach SQL Server, so check the database " +
            "first, then the Hub's own console window for the error it printed.");
    }
}
