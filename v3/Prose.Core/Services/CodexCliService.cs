using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Shells out to the OpenAI Codex CLI (`codex exec`) as a single-turn ILlmService provider.
/// This rides the ChatGPT subscription's Codex CLI login (no per-token API billing) rather
/// than a raw OpenAI API key — the fallback tier between "ChatGPT subscription" and the
/// metered <see cref="OpenAiService"/> API-key path. Requires `codex` on PATH and a prior
/// `codex login` (interactive, one-time; see docs/PROVIDERS.md).
/// Codex's headless mode (`codex exec`) is an agentic coding session, not a raw completion
/// API — there is no first-class temperature/max-tokens knob, so those parameters are
/// accepted for ILlmService-signature compatibility but not forwarded.
/// </summary>
public class CodexCliService : ILlmService
{
    private readonly ILogger<CodexCliService> log;

    public CodexCliService(ILogger<CodexCliService> log)
    {
        this.log = log;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        try
        {
            // `codex login status` writes its "Logged in using ..." confirmation to stderr,
            // not stdout (confirmed empirically) — check both.
            var (exitCode, stdout, stderr) = await RunAsync(["login", "status"], stdinPayload: null, CancellationToken.None);
            return exitCode == 0 &&
                (stdout.Contains("Logged in", StringComparison.OrdinalIgnoreCase) ||
                 stderr.Contains("Logged in", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var prompt = string.IsNullOrWhiteSpace(system) ? user : $"{system}\n\n{user}";

        var args = new List<string> { "exec", "-", "--json", "--skip-git-repo-check" };
        if (!string.IsNullOrWhiteSpace(model))
        {
            args.Add("-c");
            args.Add($"model=\"{model}\"");
        }

        var (exitCode, stdout, stderr) = await RunAsync(args, prompt, ct);

        var text = ParseAgentMessages(stdout);
        if (!string.IsNullOrEmpty(text))
            return text;

        log.LogWarning("Codex CLI produced no agent_message (exit {ExitCode}): {Stderr}", exitCode, stderr);
        throw new InvalidOperationException(
            $"Codex CLI returned no usable response (exit {exitCode}). {ExtractError(stdout) ?? stderr}");
    }

    private static string ParseAgentMessages(string jsonl)
    {
        var sb = new StringBuilder();
        foreach (var line in jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            JsonDocument doc;
            try { doc = JsonDocument.Parse(line); }
            catch (JsonException) { continue; }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("type", out var typeEl)) continue;
                if (typeEl.GetString() != "item.completed") continue;
                if (!doc.RootElement.TryGetProperty("item", out var item)) continue;
                if (!item.TryGetProperty("type", out var itemType) || itemType.GetString() != "agent_message") continue;
                if (item.TryGetProperty("text", out var textEl))
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(textEl.GetString());
                }
            }
        }
        return sb.ToString().Trim();
    }

    private static string? ExtractError(string jsonl)
    {
        foreach (var line in jsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("error", out var err) &&
                    err.TryGetProperty("message", out var msg))
                    return msg.GetString();
                if (doc.RootElement.TryGetProperty("type", out var t) && t.GetString() == "error" &&
                    doc.RootElement.TryGetProperty("message", out var m))
                    return m.GetString();
            }
            catch (JsonException) { }
        }
        return null;
    }

    // A hung `codex` process (network stall, interactive prompt it can't answer headlessly)
    // previously blocked forever — callers typically pass CancellationToken.None, and there
    // was no internal cap, so LlmRouter's fallback chain could never move past this provider.
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(10);

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        IReadOnlyList<string> args, string? stdinPayload, CancellationToken ct)
    {
        using var timeoutCts = new CancellationTokenSource(ProcessTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        ct = linkedCts.Token;

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        if (OperatingSystem.IsWindows())
        {
            psi.FileName = "cmd.exe";
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("codex");
        }
        else
        {
            psi.FileName = "codex";
        }
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to launch the `codex` CLI. Is it installed and on PATH?");

        try
        {
            if (stdinPayload is not null)
            {
                await proc.StandardInput.WriteAsync(stdinPayload.AsMemory(), ct);
            }
            proc.StandardInput.Close();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = proc.StandardError.ReadToEndAsync(ct);
            await Task.WhenAll(stdoutTask, stderrTask);
            await proc.WaitForExitAsync(ct);

            return (proc.ExitCode, await stdoutTask, await stderrTask);
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
            catch { /* already exited */ }
        }
    }
}
