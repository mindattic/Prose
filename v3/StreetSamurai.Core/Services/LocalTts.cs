using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Common shape for free, fully-local TTS engines plugged into the audiobook pipeline.
/// Each engine synthesizes a text chunk and returns raw PCM s16le 44.1 kHz mono, so the
/// segment/silence/encode assembly in <see cref="NodeWorkbenchService.PublishAudiobookAsync"/>
/// works unchanged regardless of engine.
/// </summary>
public interface ILocalTtsEngine
{
    /// <summary>True when the engine's binary/script and voice are resolvable.</summary>
    bool IsAvailable { get; }
    /// <summary>Short label for audio-event logging, e.g. "piper:en_US-ryan-high".</summary>
    string Label { get; }
    /// <summary>Per-request character budget (segmenter hint). Local engines are uncapped;
    /// chapter-sized chunks keep memory and latency sane.</summary>
    int CharBudget { get; }
    Task<byte[]> SynthesizeToPcmAsync(string text, string ffmpegPath, CancellationToken ct = default);
}

/// <summary>
/// Resolves a <c>--tts &lt;engine&gt;</c> name to a local engine. Known engines:
/// <list type="bullet">
/// <item><c>piper</c> — bundled piper.exe (see <see cref="PiperTtsService"/>).</item>
/// <item><c>kokoro</c> — Python, Kokoro-82M, CPU-friendly, the recommended free default.</item>
/// <item><c>chatterbox</c> — Python, Resemble Chatterbox-Turbo, heavier/expressive.</item>
/// </list>
/// Returns null for an unknown name; throws via the engine when known-but-not-installed.
/// </summary>
public static class LocalTts
{
    public static readonly string[] KnownEngines = { "piper", "kokoro", "chatterbox" };

    public static ILocalTtsEngine? Resolve(string name, ILogger? log = null) =>
        name?.ToLowerInvariant() switch
        {
            "piper"               => new PiperTtsService(log),
            "kokoro"              => new PythonTtsService("kokoro", log),
            "chatterbox"          => new PythonTtsService("chatterbox", log),
            "chatterbox-turbo"    => new PythonTtsService("chatterbox", log),
            _                     => null,
        };
}

/// <summary>
/// Free, fully-local neural TTS that shells out to a per-engine Python adapter script.
/// The adapter contract (so any engine plugs in identically):
/// <code>python &lt;synth.py&gt; --text &lt;in.txt&gt; --out &lt;out.wav&gt; [--voice &lt;id&gt;]</code>
/// reads UTF-8 text, writes a WAV, exits 0. This service then resamples the WAV to the
/// pipeline's PCM (44.1 kHz mono s16le) with ffmpeg.
/// <para>Resolution (per engine ENG = kokoro|chatterbox):
/// 1. python: env <c>SS_PYTHON</c>, else a venv at <c>tools\ENG\.venv\Scripts\python.exe</c>,
///    else <c>python</c> on PATH;
/// 2. script: env <c>SS_ENG_SCRIPT</c>, else <c>tools\ENG\synth.py</c> (walking up from the
///    app base dir), else <c>%LOCALAPPDATA%\StreetSamurai\ENG\synth.py</c>;
/// 3. voice: env <c>SS_ENG_VOICE</c> (optional; the adapter has a default).</para>
/// </summary>
public sealed class PythonTtsService : ILocalTtsEngine
{
    private readonly ILogger? log;
    private readonly string engine;
    private readonly string? python;
    private readonly string? script;
    private readonly string? voice;

    public PythonTtsService(string engine, ILogger? log = null)
    {
        this.engine = engine;
        this.log = log;
        var ENG = engine.ToUpperInvariant();

        script = Environment.GetEnvironmentVariable($"SS_{ENG}_SCRIPT");
        python = Environment.GetEnvironmentVariable("SS_PYTHON");
        voice  = Environment.GetEnvironmentVariable($"SS_{ENG}_VOICE");

        foreach (var root in EngineRoots(engine))
        {
            if (script == null)
            {
                var s = Path.Combine(root, "synth.py");
                if (File.Exists(s)) script = s;
            }
            if (python == null)
            {
                var venv = Path.Combine(root, ".venv", "Scripts", "python.exe");
                if (File.Exists(venv)) python = venv;
            }
        }
        python ??= "python";
    }

    private static IEnumerable<string> EngineRoots(string engine)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            yield return Path.Combine(dir.FullName, "tools", engine);
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StreetSamurai", engine);
    }

    public bool IsAvailable => script != null && File.Exists(script);
    public string Label => $"{engine}:{(voice ?? "default")}";
    public int CharBudget => 8000;

    public async Task<byte[]> SynthesizeToPcmAsync(string text, string ffmpegPath, CancellationToken ct = default)
    {
        if (!IsAvailable) throw new InvalidOperationException(
            $"{engine} TTS adapter not found. Expected tools\\{engine}\\synth.py (a Python adapter " +
            $"reading --text and writing --out WAV), or set SS_{engine.ToUpperInvariant()}_SCRIPT. " +
            $"See tools\\{engine}\\README for the one-time setup.");

        var tmpIn  = Path.Combine(Path.GetTempPath(), $"ss-{engine}-{Guid.CreateVersion7():N}.txt");
        var tmpWav = Path.Combine(Path.GetTempPath(), $"ss-{engine}-{Guid.CreateVersion7():N}.wav");
        try
        {
            await File.WriteAllTextAsync(tmpIn, text, new System.Text.UTF8Encoding(false), ct);

            var psi = new ProcessStartInfo
            {
                FileName = python!,
                WorkingDirectory = Path.GetDirectoryName(script!)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add(script!);
            psi.ArgumentList.Add("--text"); psi.ArgumentList.Add(tmpIn);
            psi.ArgumentList.Add("--out");  psi.ArgumentList.Add(tmpWav);
            if (!string.IsNullOrWhiteSpace(voice)) { psi.ArgumentList.Add("--voice"); psi.ArgumentList.Add(voice); }

            // On TLS-intercepting networks, point every child SSL path at the OS-derived
            // CA bundle (tools\corp-ca-bundle.pem, built by tools\make-ca-bundle.ps1) so
            // first-run model downloads from HuggingFace verify. No-op once models cache.
            var caBundle = ResolveCaBundle();
            if (caBundle != null)
                foreach (var v in new[] { "SSL_CERT_FILE", "REQUESTS_CA_BUNDLE", "CURL_CA_BUNDLE", "PIP_CERT" })
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v)))
                        psi.Environment[v] = caBundle;

            using (var p = Process.Start(psi)!)
            {
                var outTask = p.StandardOutput.ReadToEndAsync(ct);
                var errTask = p.StandardError.ReadToEndAsync(ct);
                await p.WaitForExitAsync(ct);
                await Task.WhenAll(outTask, errTask);
                if (p.ExitCode != 0)
                    throw new InvalidOperationException($"{engine} synth exited {p.ExitCode}: {Truncate(errTask.Result, 500)}");
            }
            if (!File.Exists(tmpWav) || new FileInfo(tmpWav).Length == 0)
                throw new InvalidOperationException($"{engine} synth produced no audio.");

            // Resample whatever the engine emitted to the pipeline's PCM.
            var psf = new ProcessStartInfo
            {
                FileName = ffmpegPath, RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
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

            log?.LogDebug("{Engine} synthesized {Chars} chars -> {Bytes} pcm bytes", engine, text.Length, ms.Length);
            return ms.ToArray();
        }
        finally { try { File.Delete(tmpIn); } catch { } try { File.Delete(tmpWav); } catch { } }
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n];

    /// <summary>tools\corp-ca-bundle.pem (env SS_CA_BUNDLE overrides), or null if absent.</summary>
    private static string? ResolveCaBundle()
    {
        var env = Environment.GetEnvironmentVariable("SS_CA_BUNDLE");
        if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var p = Path.Combine(dir.FullName, "tools", "corp-ca-bundle.pem");
            if (File.Exists(p)) return p;
        }
        return null;
    }
}
