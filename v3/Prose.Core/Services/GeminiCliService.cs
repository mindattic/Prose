using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Shells out to the Google Gemini CLI as a single-turn ILlmService provider. Rides the
/// Gemini CLI's own Google-account login (OAuth / Code Assist free tier) rather than a raw
/// Gemini API key — the fallback tier between "Gemini subscription" and the metered
/// <see cref="GeminiService"/> API-key path. Requires `gemini` on PATH and a prior
/// interactive `gemini` login (one-time; see docs/PROVIDERS.md).
/// KNOWN RISK: there is a reported Windows-specific bug where Gemini CLI's headless/print
/// mode returns an empty stdout even though the underlying API call succeeded. This class
/// surfaces that as a clear failure (so the LlmRouter fallback chain moves to the next
/// provider) rather than silently returning an empty string — verify empirically once
/// `gemini` login is complete on this machine.
/// </summary>
public class GeminiCliService : ILlmService
{
    private readonly ILogger<GeminiCliService> log;

    public GeminiCliService(ILogger<GeminiCliService> log)
    {
        this.log = log;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        try
        {
            var (exitCode, stdout, _) = await RunAsync("Reply with exactly one word: ok", model: null, CancellationToken.None);
            return exitCode == 0 && !string.IsNullOrWhiteSpace(stdout);
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
        var (exitCode, stdout, stderr) = await RunAsync(prompt, model, ct);

        var text = ParseResponse(stdout);
        if (!string.IsNullOrEmpty(text))
            return text;

        log.LogWarning(
            "Gemini CLI produced no output (exit {ExitCode}); this may be the known Windows " +
            "headless-stdout bug — see class remarks. stderr: {Stderr}", exitCode, stderr);
        throw new InvalidOperationException(
            $"Gemini CLI returned no usable response (exit {exitCode}). {stderr}");
    }

    private static string ParseResponse(string stdout)
    {
        var trimmed = stdout.Trim();
        if (trimmed.Length == 0) return "";

        // --output-format json returns a single JSON object with a "response" field;
        // fall back to raw text if it doesn't parse (older CLI versions / plain text mode).
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.TryGetProperty("response", out var resp))
                return resp.GetString()?.Trim() ?? "";
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                var msg = err.TryGetProperty("message", out var m) ? m.GetString() : err.ToString();
                throw new InvalidOperationException($"Gemini CLI error: {msg}");
            }
        }
        catch (JsonException)
        {
            return trimmed;
        }
        return trimmed;
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(string prompt, string? model, CancellationToken ct)
    {
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
            psi.ArgumentList.Add("gemini");
        }
        else
        {
            psi.FileName = "gemini";
        }

        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("json");
        if (!string.IsNullOrWhiteSpace(model))
        {
            psi.ArgumentList.Add("-m");
            psi.ArgumentList.Add(model);
        }

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to launch the `gemini` CLI. Is it installed and on PATH?");

        try
        {
            // No -p flag: piping the full prompt via stdin (rather than a command-line
            // argument) sidesteps Windows argv-length limits for long prose-generation
            // prompts. Non-TTY stdin alone is documented to trigger Gemini CLI's headless
            // mode without needing -p.
            await proc.StandardInput.WriteAsync(prompt.AsMemory(), ct);
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
