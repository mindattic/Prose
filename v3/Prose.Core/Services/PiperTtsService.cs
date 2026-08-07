using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services;

/// <summary>
/// Free, fully-local neural TTS via Piper (https://github.com/rhasspy/piper) — the
/// zero-cost alternative to ElevenLabs for draft/bedtime listens. Synthesizes text to a
/// WAV with piper.exe, then resamples to the audiobook pipeline's native PCM
/// (44.1 kHz, mono, s16le) with ffmpeg so the existing segment/silence/encode assembly
/// in <see cref="NodeWorkbenchService.ExportAudiobookAsync"/> works unchanged.
/// <para>Resolution order for the engine and voice model:
/// 1. env <c>PROSE_PIPER_EXE</c> / <c>PROSE_PIPER_MODEL</c>;
/// 2. a <c>tools\piper</c> directory found by walking up from the app base directory
///    (repo convention: <c>tools\piper\piper\piper.exe</c> + first <c>*.onnx</c> under
///    <c>tools\piper</c>);
/// 3. <c>%LOCALAPPDATA%\Prose\piper</c>.</para>
/// </summary>
public sealed class PiperTtsService : ILocalTtsEngine
{
    private readonly ILogger? log;

    public string? ExePath { get; }
    public string? ModelPath { get; }
    public bool IsAvailable => ExePath != null && ModelPath != null;
    public string Label => $"piper:{(ModelPath != null ? Path.GetFileNameWithoutExtension(ModelPath) : "none")}";
    public int CharBudget => 12000;

    public PiperTtsService(ILogger? log = null)
    {
        this.log = log;

        var exe = Environment.GetEnvironmentVariable("PROSE_PIPER_EXE");
        var model = Environment.GetEnvironmentVariable("PROSE_PIPER_MODEL");

        if (exe == null || model == null)
        {
            foreach (var root in CandidateRoots())
            {
                if (!Directory.Exists(root)) continue;
                exe ??= Directory.EnumerateFiles(root, "piper.exe", SearchOption.AllDirectories).FirstOrDefault();
                model ??= Directory.EnumerateFiles(root, "*.onnx", SearchOption.AllDirectories)
                    .FirstOrDefault(m => File.Exists(m + ".json"));
                if (exe != null && model != null) break;
            }
        }

        if (exe != null && File.Exists(exe)) ExePath = exe;
        if (model != null && File.Exists(model)) ModelPath = model;
    }

    private static IEnumerable<string> CandidateRoots()
    {
        // Walk up from the runtime base dir looking for the repo's tools\piper.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            yield return Path.Combine(dir.FullName, "tools", "piper");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Prose", "piper");
    }

    /// <summary>Synthesize one text chunk to raw PCM s16le 44.1 kHz mono.</summary>
    public async Task<byte[]> SynthesizeToPcmAsync(string text, string ffmpegPath, CancellationToken ct = default)
    {
        if (!IsAvailable) throw new InvalidOperationException(
            "Piper TTS is not installed. Expected tools\\piper (piper.exe + a *.onnx voice with its .json), " +
            "or set PROSE_PIPER_EXE / PROSE_PIPER_MODEL.");

        var tmpWav = Path.Combine(Path.GetTempPath(), $"ss-piper-{Guid.CreateVersion7():N}.wav");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ExePath!,
                WorkingDirectory = Path.GetDirectoryName(ExePath!)!,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("--model");
            psi.ArgumentList.Add(ModelPath!);
            psi.ArgumentList.Add("--output_file");
            psi.ArgumentList.Add(tmpWav);
            psi.ArgumentList.Add("--sentence_silence");
            psi.ArgumentList.Add("0.35");

            using (var p = Process.Start(psi)!)
            {
                // Drain stdout+stderr before waiting or the process deadlocks on full pipes.
                var outTask = p.StandardOutput.ReadToEndAsync(ct);
                var errTask = p.StandardError.ReadToEndAsync(ct);
                await p.StandardInput.WriteAsync(text.AsMemory(), ct);
                p.StandardInput.Close();
                await p.WaitForExitAsync(ct);
                await Task.WhenAll(outTask, errTask);
                if (p.ExitCode != 0)
                    throw new InvalidOperationException($"piper.exe exited {p.ExitCode}: {Truncate(errTask.Result, 400)}");
            }

            // Resample the piper WAV (typically 22.05 kHz) to the pipeline's PCM.
            var psf = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in new[] { "-v", "error", "-i", tmpWav, "-f", "s16le", "-acodec", "pcm_s16le", "-ar", "44100", "-ac", "1", "-" })
                psf.ArgumentList.Add(a);

            using var f = Process.Start(psf)!;
            using var ms = new MemoryStream();
            var errF = f.StandardError.ReadToEndAsync(ct);
            await f.StandardOutput.BaseStream.CopyToAsync(ms, ct);
            await f.WaitForExitAsync(ct);
            await errF;
            if (f.ExitCode != 0)
                throw new InvalidOperationException($"ffmpeg resample exited {f.ExitCode}: {Truncate(errF.Result, 400)}");

            log?.LogDebug("Piper synthesized {Chars} chars -> {Bytes} pcm bytes", text.Length, ms.Length);
            return ms.ToArray();
        }
        finally { try { File.Delete(tmpWav); } catch { } }
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n];
}
