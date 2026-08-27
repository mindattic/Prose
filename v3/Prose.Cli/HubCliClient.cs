using System.Net.Http.Json;
using System.Text.Json;

namespace Prose.Cli;

/// <summary>
/// Shared forwarding helper for the Stage C CLI-command migration onto the Prose Hub. A
/// migrated dispatch block in Program.cs calls <see cref="ForwardAsync"/> instead of building
/// its own <c>IServiceProvider</c> and running the handler in-process — the same handler class
/// runs inside the Hub's process via <c>CliDispatch</c> reflection, against the Hub's warm,
/// resident services.
///
/// Prints the Hub's captured stdout/stderr to this process's own console, so the command looks
/// identical to a user regardless of where it actually ran.
/// </summary>
public static class HubCliClient
{
    // The default HttpClient.Timeout (100s) is shorter than several forwarded commands
    // (full-battery audits, --write-story, review panels) — any of those would previously
    // throw an unhandled TaskCanceledException and crash this CLI process instead of
    // reporting a clean error. Long-running-by-design, so this is generous rather than tuned.
    private static readonly HttpClient Http = BuildClient();

    // Portable-writing-service plan, Phase 1: the Hub's sensitive endpoints (cli-invoke among
    // them) require the shared X-Prose-Key header. Constructed directly (no DI available this
    // early — mirrors Prose.Hub/Prose.Mcp's own Program.cs pattern of instantiating
    // SettingsService directly before their DI containers exist) so it reads the same shared
    // Settings.json file the Hub generated the key into at its own startup.
    private static HttpClient BuildClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(Prose.Core.Services.HubGate.DefaultBaseUrl), Timeout = TimeSpan.FromMinutes(30) };
        var key = new Prose.Core.Services.SettingsService().HubApiKey;
        if (!string.IsNullOrEmpty(key)) client.DefaultRequestHeaders.Add("X-Prose-Key", key);
        return client;
    }

    /// <param name="method">Override the entry-point method name for handlers that don't use
    /// the ~150-command common convention (RunAsync/Run) - e.g. GlossaryCli.RunBookAsync,
    /// AutoCorrectUndoCli.RunStatusAsync. Omit for the common case.</param>
    /// <param name="extraParamValue">For the handlers that take a third parameter beyond
    /// args/services (PublishManuscriptCli's Format enum, BeatLensCli's plain-string lens) -
    /// pass its already-resolved value's ToString(). Omit for every other handler.</param>
    public static async Task<int> ForwardAsync(string handlerClass, string[] args, string? method = null, string? extraParamValue = null)
    {
        var universe = Prose.Core.Services.UniverseBootstrap.RequestedSlug
            ?? Environment.GetEnvironmentVariable("PROSE_UNIVERSE");

        // The Hub is a separate long-lived process with its own cwd and no real stdin of its
        // own — send this process's cwd unconditionally (cheap, makes every relative --file
        // path resolve exactly as it would running in-process) and this process's stdin only
        // when actually redirected (piped/file), never when it's a real interactive terminal —
        // reading Console.In here would otherwise block on a command that never asked for it.
        var cwd = Environment.CurrentDirectory;
        string? stdin = Console.IsInputRedirected ? await Console.In.ReadToEndAsync() : null;

        HttpResponseMessage resp;
        try
        {
            resp = await Http.PostAsJsonAsync("api/cli-invoke", new { handlerClass, args, universe, method, extraParamValue, cwd, stdin });
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"[hub] Prose Hub is not reachable — {ex.Message}");
            return 1;
        }
        catch (TaskCanceledException)
        {
            Console.Error.WriteLine($"[hub] Command timed out after {Http.Timeout} waiting on the Hub — it may still be running there.");
            return 1;
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"[hub] cli-invoke failed ({(int)resp.StatusCode}): {body}");
            return 1;
        }

        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var output = result.GetProperty("output").GetString() ?? "";
        var error = result.GetProperty("error").GetString() ?? "";
        var exitCode = result.GetProperty("exitCode").GetInt32();

        if (output.Length > 0) Console.Out.Write(output);
        if (error.Length > 0) Console.Error.Write(error);
        return exitCode;
    }

    /// <summary>
    /// The CostGateCli-equivalent forward — for the ~15 commands that estimate cost via
    /// CommandCostEstimatorService and prompt y/n above threshold before running. The
    /// estimator/ledger are Hub-resident, so the estimate itself comes from the Hub
    /// (<c>api/cli-cost-gate</c>); only the actual terminal y/n read happens here, on this
    /// process's real console — see CostGateDispatch.cs for the two-round-trip protocol.
    /// Declining (either by explicit 'n' or the same redirected-stdin fail-closed rule
    /// CostGateCli always used) exits 0, matching the pre-migration Program.cs callers'
    /// `if (!proceed) return;`.
    /// </summary>
    public static async Task<int> ForwardWithCostGateAsync(string handlerClass, string commandName, string[] args, string? method = null, string? extraParamValue = null)
    {
        var universe = Prose.Core.Services.UniverseBootstrap.RequestedSlug
            ?? Environment.GetEnvironmentVariable("PROSE_UNIVERSE");
        var cwd = Environment.CurrentDirectory;
        var stdin = Console.IsInputRedirected ? await Console.In.ReadToEndAsync() : null;
        var preConfirmed = args.Contains("--no-confirm") || args.Contains("--yes");

        var gate = await PostCostGateAsync(handlerClass, commandName, args, universe, method, extraParamValue, cwd, stdin, preConfirmed);
        if (gate == null) return 1;

        if (gate.Value.NeedsConfirm)
        {
            if (Console.IsInputRedirected)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"  Command  : {commandName}");
                Console.Error.WriteLine($"  Est cost : ${gate.Value.Estimated:F3}  ({gate.Value.Confidence})");
                Console.Error.WriteLine("  Input is redirected (non-interactive) — refusing to proceed without --no-confirm.");
                return 0;
            }

            Console.WriteLine();
            Console.WriteLine($"  Command  : {commandName}");
            Console.WriteLine($"  Est cost : ${gate.Value.Estimated:F3}  ({gate.Value.Confidence})");
            Console.Write("  Proceed? [y/n]: ");
            var key = Console.ReadKey(intercept: false);
            Console.WriteLine();
            if (key.KeyChar is not ('y' or 'Y')) return 0;

            gate = await PostCostGateAsync(handlerClass, commandName, args, universe, method, extraParamValue, cwd, stdin, noConfirm: true);
            if (gate == null) return 1;
        }

        if (gate.Value.Output.Length > 0) Console.Out.Write(gate.Value.Output);
        if (gate.Value.Error.Length > 0) Console.Error.Write(gate.Value.Error);
        return gate.Value.ExitCode;
    }

    private readonly record struct CostGateResult(bool NeedsConfirm, double Estimated, string Confidence, int ExitCode, string Output, string Error);

    private static async Task<CostGateResult?> PostCostGateAsync(
        string handlerClass, string commandName, string[] args, string? universe,
        string? method, string? extraParamValue, string cwd, string? stdin, bool noConfirm)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await Http.PostAsJsonAsync("api/cli-cost-gate", new
            {
                handlerClass, commandName, args, universe, method, extraParamValue, cwd, stdin, noConfirm,
            });
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"[hub] Prose Hub is not reachable — {ex.Message}");
            return null;
        }
        catch (TaskCanceledException)
        {
            Console.Error.WriteLine($"[hub] Command timed out after {Http.Timeout} waiting on the Hub — it may still be running there.");
            return null;
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"[hub] cli-cost-gate failed ({(int)resp.StatusCode}): {body}");
            return null;
        }

        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return new CostGateResult(
            result.GetProperty("needsConfirm").GetBoolean(),
            result.GetProperty("estimated").GetDouble(),
            result.GetProperty("confidence").GetString() ?? "",
            result.GetProperty("exitCode").GetInt32(),
            result.GetProperty("output").GetString() ?? "",
            result.GetProperty("error").GetString() ?? "");
    }
}
