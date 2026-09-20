using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services;

/// <summary>
/// Spawns the Claude Code CLI as a subprocess and streams its output back.
/// Used by the Writer chat panel so prompts route through Claude Code's
/// full tool set (Read, Edit, Bash, Grep) — letting it read canon JSON,
/// query the graph, run dotnet builds, and edit the codebase directly.
/// Local-dev only: requires the `claude` CLI on PATH.
/// </summary>
public class ClaudeCliService
{
    private readonly ILogger<ClaudeCliService> log;

    public ClaudeCliService(ILogger<ClaudeCliService> log)
    {
        this.log = log;
    }

    public async IAsyncEnumerable<string> SendAsync(
        string prompt,
        string workingDir,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDir,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            // The prompt goes out over stdin, so stdin needs UTF-8 as much as the two output
            // streams do — without this it is written in the console's default code page and
            // every em-dash / curly quote / Φ in the prompt becomes a mangled byte. Same defect
            // that made `codex exec -` reject prompts outright (CodexCliService, 2026-08-24);
            // here it corrupts silently instead of erroring, which is worse. No BOM: it would
            // land as literal text at the top of the prompt.
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        if (OperatingSystem.IsWindows())
        {
            psi.FileName = "cmd.exe";
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("claude");
        }
        else
        {
            psi.FileName = "claude";
        }

        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("text");

        log.LogInformation("Spawning claude CLI in {Dir}", workingDir);

        Process? proc = null;
        string? launchError = null;
        try
        {
            proc = Process.Start(psi);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to launch claude CLI");
            launchError = $"[error] Could not launch the `claude` CLI. Is Claude Code installed and on PATH?\n{ex.Message}";
        }

        if (launchError != null)
        {
            yield return launchError;
            yield break;
        }

        if (proc == null)
        {
            yield return "[error] Process.Start returned null for claude CLI.";
            yield break;
        }

        try
        {
            // Pipe the prompt in via stdin so we don't fight Windows command-line escaping.
            await proc.StandardInput.WriteAsync(prompt.AsMemory(), ct);
            await proc.StandardInput.FlushAsync(ct);
            proc.StandardInput.Close();

            string? line;
            while ((line = await proc.StandardOutput.ReadLineAsync(ct)) != null)
            {
                ct.ThrowIfCancellationRequested();
                yield return line;
            }

            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
            {
                var err = await proc.StandardError.ReadToEndAsync(ct);
                log.LogWarning("claude CLI exited {Code}: {Err}", proc.ExitCode, err);
                yield return $"\n[claude CLI exit {proc.ExitCode}] {err}";
            }
        }
        finally
        {
            // If the caller cancelled or stopped enumerating early, the CLI may
            // still be running. Kill the whole tree (cmd.exe → claude → node …)
            // so we don't orphan a detached process, then release the handle.
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); }
            catch { /* already exited, or never fully started */ }
            proc.Dispose();
        }
    }
}
