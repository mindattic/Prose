namespace Prose.Core.Services;

/// <summary>
/// Fail-closed startup gate (Phase 2 of the Prose Hub migration): "the hub is running, Prose
/// is working; hub goes down, Prose is down" — an explicit user decision, not a soft preference.
/// Every entry point that depends on the Hub (Prose.Cli, Prose.Mcp) calls
/// <see cref="EnsureReachableOrExit"/> at the very start of <c>Main</c>, before the DI container
/// is even built. On failure this prints a clear, single-line diagnosis (matching
/// <c>ProviderStatusCli</c>'s "not configured / configured but failing / reachable" style) and
/// calls <see cref="Environment.Exit"/> immediately — no command runs, no silent fallback to the
/// old in-process behavior.
/// </summary>
public static class HubGate
{
    public const string DefaultBaseUrl = "http://127.0.0.1:5900/";

    /// <summary>
    /// Blocking by design — this runs before any async host/DI setup exists yet. Exits the
    /// process (never returns) if the Hub isn't reachable or reports unhealthy.
    ///
    /// Retries a few times with a short delay before giving up: the SessionStart hook
    /// auto-starts the Hub detached, so a client launched moments later can race the Hub's own
    /// build+warm-up — this is a startup grace period, not a silent degrade (it still exits
    /// hard if the Hub genuinely isn't coming up).
    /// </summary>
    public static void EnsureReachableOrExit(string baseUrl = DefaultBaseUrl)
    {
        const int attempts = 4;
        string? lastError = null;

        for (var i = 0; i < attempts; i++)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var resp = http.GetAsync(new Uri(baseUrl).ToString().TrimEnd('/') + "/api/health").GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode) return;

                var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                lastError = $"Prose Hub reported unhealthy ({(int)resp.StatusCode} {resp.StatusCode}) — {body}";
            }
            catch (Exception ex)
            {
                lastError = $"Prose Hub is not reachable at {baseUrl} — {ex.Message}";
            }

            if (i < attempts - 1) Thread.Sleep(TimeSpan.FromSeconds(1.5));
        }

        Console.Error.WriteLine(
            $"[hub] {lastError}\n" +
            "      Start it with: dotnet run --project v3/Prose.Hub --no-build --configuration Release\n" +
            "      (it should also auto-start via the SessionStart hook — check its log if this is unexpected)");
        Environment.Exit(1);
    }
}
