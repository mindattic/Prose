using System.Diagnostics;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Facade over the pluggable <see cref="IVideoGenerationProvider"/> backends (kling / runway /
/// sora). Composites the cover onto the mockup template via <see cref="BookTokMockupService"/>,
/// submits that frame as an image-to-video seed, owns the submit→poll→download loop (all
/// three vendors are async job APIs), then assembles a vertical 1080x1920 MP4 — a 1s hold of
/// the mockup frame followed by the AI clip — via ffmpeg (same shell-out idiom as
/// <c>NodeWorkbenchService</c>'s audiobook assembly).
///
/// Scope limitation: the AI clip's page-flip motion is generic/blurred — there is no real
/// interior page-spread art to render, so it cannot show legible page content.
/// </summary>
public class BookTokVideoService
{
    /// <summary>Default image-to-video motion prompt — overridable per call via <see cref="Options.Prompt"/>.</summary>
    public const string DefaultPrompt =
        "hand slowly turns the book to show the cover to camera, then opens the front cover and begins flipping through the pages";

    private readonly IReadOnlyDictionary<string, IVideoGenerationProvider> providers;
    private readonly BookTokMockupService mockup;
    private readonly IPathProvider paths;
    private readonly ILogger<BookTokVideoService> log;

    public BookTokVideoService(
        IEnumerable<IVideoGenerationProvider> providers,
        BookTokMockupService mockup,
        IPathProvider paths,
        ILogger<BookTokVideoService> log)
    {
        this.providers = providers.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        this.mockup    = mockup;
        this.paths     = paths;
        this.log       = log;
    }

    /// <summary>Provider ids this build has an adapter for, and whether each has an API key configured.</summary>
    public IReadOnlyList<(string Id, bool Configured)> AvailableProviders
        => providers.Values.Select(p => (p.Id, p.IsConfigured)).ToList();

    public record Options(
        string CoverPath,
        string Slug,
        string ProviderId,
        int DurationSeconds = 8,
        string TemplateName = "default",
        string? Prompt = null,
        string? Title = null,
        bool DryRun = false,
        TimeSpan? PollInterval = null,
        TimeSpan? Timeout = null);

    public record Result(string MockupPath, string? ClipPath, string? FinalVideoPath);

    public async Task<Result> GenerateAsync(Options opts, CancellationToken ct = default)
    {
        if (!providers.TryGetValue(opts.ProviderId, out var provider))
            throw new ArgumentException($"Unknown video provider '{opts.ProviderId}'. Available: {string.Join(", ", providers.Keys)}");

        var outDir = Path.Combine(paths.MediaDir, "booktok");
        Directory.CreateDirectory(outDir);

        var mockupPath = Path.Combine(outDir, $"{opts.Slug}-mockup.png");
        await mockup.ComposeAsync(opts.CoverPath, mockupPath, opts.TemplateName, ct);

        var prompt   = string.IsNullOrWhiteSpace(opts.Prompt) ? DefaultPrompt : opts.Prompt;
        var duration = Math.Min(opts.DurationSeconds, provider.MaxDurationSeconds);

        if (opts.DryRun)
        {
            log.LogInformation(
                "[booktok] dry-run — validated request (provider={Provider}, duration={Duration}s, prompt=\"{Prompt}\"); mockup written to {Mockup}",
                provider.Id, duration, prompt, mockupPath);
            return new Result(mockupPath, null, null);
        }

        if (!provider.IsConfigured)
            throw new InvalidOperationException($"Video provider '{provider.Id}' has no API key configured.");

        var seedBytes = await File.ReadAllBytesAsync(mockupPath, ct);
        var request   = new VideoGenerationRequest(seedBytes, "png", prompt, duration);

        var jobId = await provider.SubmitJobAsync(request, ct);
        log.LogInformation("[booktok] submitted job {JobId} to {Provider}", jobId, provider.Id);

        var pollInterval = opts.PollInterval ?? TimeSpan.FromSeconds(10);
        var timeout      = opts.Timeout ?? TimeSpan.FromMinutes(10);
        var deadline     = DateTime.UtcNow + timeout;
        while (true)
        {
            var status = await provider.PollAsync(jobId, ct);
            if (status.State == VideoJobState.Done) break;
            if (status.State == VideoJobState.Failed)
                throw new InvalidOperationException($"{provider.Id} job {jobId} failed: {status.Error}");
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"{provider.Id} job {jobId} did not complete within {timeout}.");
            await Task.Delay(pollInterval, ct);
        }

        var videoResult = await provider.DownloadAsync(jobId, ct);
        var clipPath     = Path.Combine(outDir, $"{opts.Slug}-clip.{videoResult.Extension}");
        await File.WriteAllBytesAsync(clipPath, videoResult.Bytes, ct);

        var finalPath = Path.Combine(outDir, $"booktok-{opts.Slug}.mp4");
        await AssembleAsync(mockupPath, clipPath, finalPath, opts.Title, ct);

        log.LogInformation("[booktok] {Slug} — final video at {Path} via {Provider}", opts.Slug, finalPath, provider.Id);
        return new Result(mockupPath, clipPath, finalPath);
    }

    /// <summary>Concatenates a 1s hold of the mockup frame with the AI clip, scaled/padded to
    /// vertical 1080x1920, into a single MP4. Both segments get a synthesized silent audio
    /// track (dropping whatever audio the vendor clip may carry) purely so the concat
    /// demuxer's <c>-c copy</c> step sees matching streams on both inputs — mixing in real
    /// music/narration is a natural follow-up, not attempted here.</summary>
    private async Task AssembleAsync(string introImagePath, string clipPath, string outPath, string? title, CancellationToken ct)
    {
        var ffmpeg = ResolveFfmpegPath()
            ?? throw new InvalidOperationException("ffmpeg not found on PATH — required to assemble the final booktok MP4.");

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        var tmpDir = Path.Combine(Path.GetTempPath(), $"streetsamurai-booktok-asm-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            const string scaleFilter = "scale=1080:1920:force_original_aspect_ratio=decrease,pad=1080:1920:(ow-iw)/2:(oh-ih)/2,setsar=1";

            var introClip  = Path.Combine(tmpDir, "intro.mp4");
            var scaledClip = Path.Combine(tmpDir, "clip.mp4");
            var concatList = Path.Combine(tmpDir, "concat.txt");

            var introFilter = scaleFilter;
            var titleFontPath = OperatingSystem.IsWindows() ? @"C:\Windows\Fonts\arial.ttf" : "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf";
            if (!string.IsNullOrWhiteSpace(title) && File.Exists(titleFontPath))
            {
                // Colons are the ffmpeg filtergraph option separator — a bare "C:" drive letter
                // in the font path breaks parsing even inside single quotes, so it needs the
                // same "\:" escape as colons inside the title text itself.
                var escapedFont  = titleFontPath.Replace("\\", "/").Replace(":", "\\:");
                var escapedTitle = title.Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");
                introFilter += $",drawtext=fontfile='{escapedFont}':text='{escapedTitle}':fontcolor=white:fontsize=64:x=(w-text_w)/2:y=h-260:box=1:boxcolor=black@0.5:boxborderw=20";
            }

            await RunFfmpegAsync(ffmpeg, ct,
                "-y", "-loop", "1", "-i", introImagePath,
                "-f", "lavfi", "-i", "anullsrc=r=44100:cl=stereo",
                "-t", "1", "-vf", introFilter, "-r", "30", "-pix_fmt", "yuv420p",
                "-c:v", "libx264", "-c:a", "aac", "-shortest", introClip);

            await RunFfmpegAsync(ffmpeg, ct,
                "-y", "-i", clipPath,
                "-f", "lavfi", "-i", "anullsrc=r=44100:cl=stereo",
                "-map", "0:v:0", "-map", "1:a:0",
                "-vf", scaleFilter, "-r", "30", "-pix_fmt", "yuv420p",
                "-c:v", "libx264", "-c:a", "aac", "-shortest", scaledClip);

            string EscapeConcat(string p) => "file '" + p.Replace("\\", "/").Replace("'", "'\\''") + "'";
            await File.WriteAllTextAsync(concatList, $"{EscapeConcat(introClip)}\n{EscapeConcat(scaledClip)}\n", ct);

            await RunFfmpegAsync(ffmpeg, ct,
                "-y", "-f", "concat", "-safe", "0", "-i", concatList, "-c", "copy", outPath);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>Same PATH-scan idiom as <c>NodeWorkbenchService.ResolveFfmpegPath</c>.</summary>
    private static string? ResolveFfmpegPath()
    {
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var sep = OperatingSystem.IsWindows() ? ';' : ':';
        foreach (var dir in pathVar.Split(sep, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* malformed PATH entry — skip */ }
        }
        return null;
    }

    private static async Task RunFfmpegAsync(string ffmpegPath, CancellationToken ct, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)!;
        var errTask = p.StandardError.ReadToEndAsync(ct);
        await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        var err = await errTask;
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg {string.Join(' ', args)} exited {p.ExitCode}: {err}");
    }
}
