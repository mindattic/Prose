using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Composites a flat book cover onto a static 3D-mockup template ("hand holding a closed
/// book toward camera") via an ImageMagick perspective warp — no AI image-gen involved.
/// The template ships once (<c>engine/data/templates/booktok/{template}.png</c> +
/// a <c>{template}.json</c> sidecar naming the book-face quadrilateral to warp into), and
/// every book's cover.jpg is warped into that same quad. The resulting frame is both a
/// standalone still and the seed image handed to <see cref="BookTokVideoService"/>.
///
/// Pipeline (three <c>magick</c> shell-outs, same ProcessStartInfo/ArgumentList idiom as
/// <see cref="CoverImageService"/>):
///   1. Force-resize cover.jpg to the template's flat CoverWidth x CoverHeight rectangle.
///   2. Perspective-warp that rectangle's four corners onto the template's Corners quad,
///      on a transparent canvas the same size as the template.
///   3. Composite the warped layer over the template.
/// </summary>
public class BookTokMockupService
{
    private readonly IPathProvider paths;
    private readonly ILogger<BookTokMockupService> log;

    public BookTokMockupService(IPathProvider paths, ILogger<BookTokMockupService> log)
    {
        this.paths = paths;
        this.log   = log;
    }

    private sealed class TemplateSpec
    {
        [JsonPropertyName("canvasWidth")]  public int CanvasWidth { get; set; }
        [JsonPropertyName("canvasHeight")] public int CanvasHeight { get; set; }
        [JsonPropertyName("coverWidth")]   public int CoverWidth { get; set; }
        [JsonPropertyName("coverHeight")]  public int CoverHeight { get; set; }
        /// <summary>Book-face quadrilateral on the canvas, clockwise from top-left: TL, TR, BR, BL.</summary>
        [JsonPropertyName("corners")]      public int[][] Corners { get; set; } = [];
    }

    /// <summary>Resolves the template PNG + JSON sidecar path pair for <paramref name="templateName"/>
    /// (default "default") under engine/data/templates/booktok/. Fails fast if either is missing —
    /// the template is a one-time manual asset, not something this service generates.</summary>
    public (string PngPath, string JsonPath) ResolveTemplatePaths(string templateName = "default")
    {
        var dir = Path.Combine(paths.EngineDataDir, "templates", "booktok");
        var pngPath  = Path.Combine(dir, $"{templateName}.png");
        var jsonPath = Path.Combine(dir, $"{templateName}.json");

        if (!File.Exists(pngPath) || !File.Exists(jsonPath))
            throw new FileNotFoundException(
                $"BookTok mockup template '{templateName}' not found. Expected {pngPath} and {jsonPath} — " +
                "drop a template image (hand holding a closed book toward camera) and its corner-quad " +
                "JSON sidecar there once; every book's cover reuses it.");

        return (pngPath, jsonPath);
    }

    /// <summary>Warps <paramref name="coverPath"/> onto the named template and writes the
    /// composited frame to <paramref name="outputPath"/>. Returns <paramref name="outputPath"/>.</summary>
    public async Task<string> ComposeAsync(string coverPath, string outputPath, string templateName = "default", CancellationToken ct = default)
    {
        if (!File.Exists(coverPath))
            throw new FileNotFoundException($"Cover image not found: {coverPath}");

        var (templatePng, templateJson) = ResolveTemplatePaths(templateName);
        var spec = JsonSerializer.Deserialize<TemplateSpec>(await File.ReadAllTextAsync(templateJson, ct))
            ?? throw new InvalidOperationException($"Template sidecar {templateJson} is empty or invalid.");
        if (spec.Corners.Length != 4)
            throw new InvalidOperationException($"Template sidecar {templateJson} must define exactly 4 corners, got {spec.Corners.Length}.");

        var tmpDir = Path.Combine(Path.GetTempPath(), $"prose-booktok-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            var resizedPath = Path.Combine(tmpDir, "resized.png");
            var warpedPath  = Path.Combine(tmpDir, "warped.png");

            // 1. Force cover.jpg to the template's expected flat rectangle size (the "!" suffix
            //    ignores aspect ratio — the perspective warp is what makes it look correct again).
            await RunMagickAsync(ct,
                coverPath, "-resize", $"{spec.CoverWidth}x{spec.CoverHeight}!", resizedPath);

            // 2. Warp that rectangle's four corners onto the template's book-face quad, on a
            //    transparent canvas matching the template's own canvas size.
            var (tl, tr, br, bl) = (spec.Corners[0], spec.Corners[1], spec.Corners[2], spec.Corners[3]);
            var perspectiveArgs = string.Join(" ", new[]
            {
                Ctrl(0, 0, tl),
                Ctrl(spec.CoverWidth, 0, tr),
                Ctrl(spec.CoverWidth, spec.CoverHeight, br),
                Ctrl(0, spec.CoverHeight, bl),
            });
            await RunMagickAsync(ct,
                resizedPath,
                "-alpha", "set", "-virtual-pixel", "transparent",
                "-set", "option:distort:viewport", $"{spec.CanvasWidth}x{spec.CanvasHeight}+0+0",
                "-distort", "Perspective", perspectiveArgs,
                warpedPath);

            // 3. Composite the warped cover over the template.
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await RunMagickAsync(ct,
                templatePng, warpedPath, "-compose", "Over", "-composite", outputPath);

            log.LogInformation("[booktok-mockup] composed {Output} from {Cover} + template '{Template}'", outputPath, coverPath, templateName);
            return outputPath;
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    private static string Ctrl(int srcX, int srcY, int[] dst)
        => FormattableString.Invariant($"{srcX},{srcY},{dst[0]},{dst[1]}");

    private static async Task RunMagickAsync(CancellationToken ct, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "magick",
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
            throw new InvalidOperationException($"magick {string.Join(' ', args)} exited {p.ExitCode}: {err}");
    }
}
