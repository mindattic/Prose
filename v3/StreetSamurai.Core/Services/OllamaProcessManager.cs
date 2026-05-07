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

    // Tracks the most recent warmup so callers that need a warm embed/chat model
    // (e.g. EmbeddingIndexService.ReindexAllAsync) can await it instead of racing
    // ahead and getting 404s while Ollama is still loading bge-m3 into VRAM.
    private Task warmupTask = Task.CompletedTask;
    private readonly object warmupLock = new();

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
                    StartWarmup();
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
                    StartWarmup();
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
    /// Like <see cref="EnsureRunningAsync"/>, but also waits for the chat + embed
    /// models to finish loading. Use this from callers that issue an embed/chat
    /// request immediately after — e.g. <c>EmbeddingIndexService.ReindexAllAsync</c>
    /// — to avoid 404s while Ollama is still pulling bge-m3 into VRAM.
    /// </summary>
    public async Task<bool> EnsureWarmAsync(CancellationToken ct = default)
    {
        var running = await EnsureRunningAsync(ct: ct);
        if (!running) return false;
        Task t;
        lock (warmupLock) t = warmupTask;
        try { await t.WaitAsync(ct); }
        catch (OperationCanceledException) { throw; }
        catch { /* warmup is best-effort; failures already logged */ }
        return true;
    }

    /// <summary>
    /// Verifies that the configured chat + embed models are pulled locally.
    /// Matches both exact name and base-name (Ollama returns "bge-m3:latest"
    /// for a model the user pulled as "bge-m3"). Returns the list of any
    /// missing model names so the caller can render <c>ollama pull X</c> hints.
    /// </summary>
    public async Task<(bool Ok, IReadOnlyList<string> Missing)> VerifyModelsAsync(CancellationToken ct = default)
    {
        var required = new[] { ollama.ChatModel, ollama.EmbedModel };
        var installed = await ollama.ListModelsAsync(ct);
        var missing = required
            .Where(r => !installed.Any(i =>
                string.Equals(i, r, StringComparison.OrdinalIgnoreCase)
                || string.Equals(i.Split(':')[0], r.Split(':')[0], StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return (missing.Count == 0, missing);
    }

    private void StartWarmup()
    {
        lock (warmupLock)
        {
            if (!warmupTask.IsCompleted) return; // a warmup is already in flight
            warmupTask = Task.Run(() => WarmModelsAsync(CancellationToken.None));
        }
    }

    /// <summary>
    /// Pre-load the chat + embed models into VRAM so the first request doesn't
    /// pay the multi-second cold-load cost. A 404 from /api/generate or /api/embed
    /// here means the model isn't pulled — surfaced as a warning with the exact
    /// `ollama pull` command, since the alternative is hundreds of indexer 404s
    /// downstream that don't name the missing model.
    /// </summary>
    private async Task WarmModelsAsync(CancellationToken ct)
    {
        try
        {
            var chatBody = new { model = opts.ChatModel, prompt = "", keep_alive = opts.KeepAlive, stream = false };
            using (var chatRes = await http.PostAsJsonAsync("/api/generate", chatBody, ct))
                WarnIfModelMissing(chatRes, opts.ChatModel, "chat");

            var embedBody = new { model = opts.EmbedModel, input = "warmup", keep_alive = opts.KeepAlive };
            using (var embedRes = await http.PostAsJsonAsync("/api/embed", embedBody, ct))
                WarnIfModelMissing(embedRes, opts.EmbedModel, "embed");

            log.LogInformation("Ollama models warmed: chat={Chat}, embed={Embed}", opts.ChatModel, opts.EmbedModel);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Ollama warmup skipped");
        }
    }

    private void WarnIfModelMissing(HttpResponseMessage res, string model, string role)
    {
        if (res.StatusCode != System.Net.HttpStatusCode.NotFound) return;
        log.LogWarning(
            "Ollama {Role} model '{Model}' is not pulled. Run: ollama pull {PullModel}",
            role, model, model);
    }
}
