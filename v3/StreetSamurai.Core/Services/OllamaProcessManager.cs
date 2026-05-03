using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

public enum OllamaState { Unknown, Reachable, Starting, Started, Unavailable }

/// <summary>
/// Best-effort autostarter for the local Ollama server. If localhost:11434 isn't
/// answering, spawns "ollama serve" detached, waits for /api/tags to respond, and
/// fires a tiny prompt at the chat + embed models so they're already in VRAM by the
/// time the user hits Ask. On hosts without Ollama installed (Azure), every step
/// fails silently and the page falls through to its existing "not reachable" UI.
/// </summary>
public class OllamaProcessManager
{
    private readonly OllamaClient ollama;
    private readonly OllamaOptions opts;
    private readonly ILogger<OllamaProcessManager> log;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(5) };
    private readonly SemaphoreSlim gate = new(1, 1);

    public OllamaState State { get; private set; } = OllamaState.Unknown;
    public string LastError { get; private set; } = "";

    public OllamaProcessManager(OllamaClient ollama, OllamaOptions opts, ILogger<OllamaProcessManager> log)
    {
        this.ollama = ollama;
        this.opts = opts;
        this.log = log;
        http.BaseAddress = new Uri(opts.BaseUrl);
    }

    /// <summary>
    /// Ensures Ollama is reachable. If already up, returns immediately.
    /// Otherwise tries to spawn "ollama serve" and waits up to <paramref name="warmupTimeout"/>
    /// for the server. Pre-warms chat + embed models on a background task.
    /// </summary>
    public async Task<bool> EnsureRunningAsync(TimeSpan? warmupTimeout = null, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (await ollama.IsReachableAsync(ct))
            {
                if (State != OllamaState.Started) // first probe — kick off warmup
                    _ = Task.Run(() => WarmModelsAsync(CancellationToken.None));
                State = OllamaState.Reachable;
                return true;
            }

            State = OllamaState.Starting;
            LastError = "";
            log.LogInformation("Ollama not reachable at {Url}; attempting to start…", opts.BaseUrl);

            if (!TrySpawnServer(out var spawnError))
            {
                LastError = spawnError;
                State = OllamaState.Unavailable;
                log.LogWarning("Could not spawn ollama: {Err}", spawnError);
                return false;
            }

            var timeout = warmupTimeout ?? TimeSpan.FromSeconds(15);
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (await ollama.IsReachableAsync(ct))
                {
                    State = OllamaState.Started;
                    log.LogInformation("Ollama is up at {Url}", opts.BaseUrl);
                    _ = Task.Run(() => WarmModelsAsync(CancellationToken.None));
                    return true;
                }
                await Task.Delay(500, ct);
            }

            LastError = $"Ollama spawned but did not respond at {opts.BaseUrl} within {timeout.TotalSeconds:N0}s.";
            State = OllamaState.Unavailable;
            log.LogWarning("{Err}", LastError);
            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TrySpawnServer(out string error)
    {
        error = "";
        var exe = ResolveExecutable();
        if (exe == null)
        {
            error = "ollama executable not found in PATH or known install locations.";
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo(exe, "serve")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var proc = Process.Start(psi);
            if (proc == null)
            {
                error = "Process.Start returned null.";
                return false;
            }
            // Detach: drain output to /dev/null so the buffer doesn't fill and stall the process.
            _ = Task.Run(async () => { try { await proc.StandardOutput.ReadToEndAsync(); } catch { } });
            _ = Task.Run(async () => { try { await proc.StandardError.ReadToEndAsync(); } catch { } });
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string? ResolveExecutable()
    {
        // 1. PATH lookup
        var fromPath = Environment.GetEnvironmentVariable("PATH")?
            .Split(Path.PathSeparator)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Path.Combine(p, OperatingSystem.IsWindows() ? "ollama.exe" : "ollama"))
            .FirstOrDefault(File.Exists);
        if (fromPath != null) return fromPath;

        // 2. Known install locations
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
                @"C:\Program Files\Ollama\ollama.exe",
            }
            : OperatingSystem.IsMacOS()
                ? new[]
                {
                    "/usr/local/bin/ollama",
                    "/opt/homebrew/bin/ollama",
                    "/Applications/Ollama.app/Contents/Resources/ollama",
                }
                : new[]
                {
                    "/usr/local/bin/ollama",
                    "/usr/bin/ollama",
                };
        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Pre-load the chat + embed models into VRAM so the first request doesn't
    /// pay the multi-second cold-load cost. Best-effort; failures are swallowed.
    /// </summary>
    private async Task WarmModelsAsync(CancellationToken ct)
    {
        try
        {
            // /api/generate with empty prompt + keep_alive nudges Ollama to load the model.
            var chatBody = new { model = opts.ChatModel, prompt = "", keep_alive = opts.KeepAlive, stream = false };
            await http.PostAsJsonAsync("/api/generate", chatBody, ct);

            var embedBody = new { model = opts.EmbedModel, input = "warmup", keep_alive = opts.KeepAlive };
            await http.PostAsJsonAsync("/api/embed", embedBody, ct);

            log.LogInformation("Ollama models warmed: chat={Chat}, embed={Embed}", opts.ChatModel, opts.EmbedModel);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Ollama warmup skipped");
        }
    }
}
