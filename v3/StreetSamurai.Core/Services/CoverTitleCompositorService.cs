using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Composites a stylized title treatment onto an AI-generated cover image via
/// ImageMagick (shelled out, same external-tool pattern as <c>LocalTts</c>'s ffmpeg
/// call). CoverPromptService deliberately asks image models to leave blank space at
/// the top and NEVER render text themselves — AI image models render legible
/// typography unreliably. This service adds the actual title deterministically:
///
///   - a soft black-to-transparent gradient vignette across the top (not a hard box)
///     so the art still reads through and nothing looks pasted-on
///   - an inscriptional all-caps display face (Perpetua Titling — the classic
///     literary/historical title font) with letter-spacing, not body text set large
///   - a soft drop shadow under the letterforms for legibility over any art
///   - a thin rule line above and below the title, the oldest trick in cover design
///     for making a title block read as "designed" rather than "typed on"
/// </summary>
public class CoverTitleCompositorService
{
    private const string TitleFont = "Perpetua-Titling-MT-Bold";
    private const int Kerning = 9;

    private readonly ILogger<CoverTitleCompositorService> log;

    public CoverTitleCompositorService(ILogger<CoverTitleCompositorService> log)
    {
        this.log = log;
    }

    /// <summary>
    /// Reads the image at <paramref name="imageBytes"/> and returns a copy with
    /// <paramref name="title"/> composited as a stylized title block across the top.
    /// </summary>
    public async Task<byte[]> CompositeTitleAsync(byte[] imageBytes, string title, string extension, CancellationToken ct = default)
    {
        var (width, height) = await IdentifyDimensionsAsync(imageBytes, extension, ct);

        var vignetteHeight = (int)(height * 0.42);
        var captionWidth   = (int)(width * 0.8);
        var pointSize      = ComputePointSize(width, title.Length);
        var marginTop      = (int)(height * 0.07);
        var ruleWidth      = (int)(width * 0.30);

        var titleUpper = title.ToUpper(CultureInfo.InvariantCulture);

        var tmpBase    = Path.GetTempFileName() + $".{extension}";
        var tmpStage1  = Path.GetTempFileName() + $".{extension}";
        var tmpCaption = Path.GetTempFileName() + ".png";
        var tmpOut     = Path.GetTempFileName() + $".{extension}";
        try
        {
            await File.WriteAllBytesAsync(tmpBase, imageBytes, ct);

            // 1) Soft gradient vignette across the top — fades into the art, no hard edge.
            await RunMagickAsync(new List<string>
            {
                tmpBase,
                "(", "-size", $"{width}x{vignetteHeight}", "gradient:black-none", ")",
                "-gravity", "North", "-compose", "over", "-composite",
                tmpStage1,
            }, ct);

            // 2) Title as its own transparent PNG: letter-spaced small caps + soft drop shadow.
            await RunMagickAsync(new List<string>
            {
                "-background", "none",
                "-fill", "white",
                "-font", TitleFont,
                "-kerning", Kerning.ToString(),
                "-pointsize", pointSize.ToString(),
                "-gravity", "Center",
                "-size", $"{captionWidth}x",
                $"caption:{titleUpper}",
                "(", "+clone", "-background", "black", "-shadow", "75x4+0+3", ")",
                "+swap", "-background", "none", "-layers", "merge", "+repage",
                tmpCaption,
            }, ct);

            var (_, captionHeight) = await IdentifyDimensionsAsync(await File.ReadAllBytesAsync(tmpCaption, ct), "png", ct);

            // 3) Composite the title block onto the vignette, then draw thin rule lines
            //    just above and below it — the detail that makes a title read as designed.
            var ruleX1 = (width - ruleWidth) / 2;
            var ruleX2 = ruleX1 + ruleWidth;
            var ruleGap     = (int)(height * 0.016);
            var ruleTopY    = marginTop - ruleGap;
            var ruleBottomY = marginTop + captionHeight + ruleGap;

            await RunMagickAsync(new List<string>
            {
                tmpStage1, tmpCaption,
                "-gravity", "North", "-geometry", $"+0+{marginTop}", "-composite",
                "-fill", "white", "-stroke", "none",
                "-draw", $"rectangle {ruleX1},{ruleTopY} {ruleX2},{ruleTopY + 2}",
                "-draw", $"rectangle {ruleX1},{ruleBottomY} {ruleX2},{ruleBottomY + 2}",
                tmpOut,
            }, ct);

            return await File.ReadAllBytesAsync(tmpOut, ct);
        }
        finally
        {
            foreach (var f in new[] { tmpBase, tmpStage1, tmpCaption, tmpOut })
                try { File.Delete(f); } catch { }
        }
    }

    private static int ComputePointSize(int width, int titleLength)
    {
        var basePt = width / 15;
        var scale  = titleLength switch
        {
            <= 15 => 1.0,
            <= 28 => 0.85,
            <= 45 => 0.68,
            _     => 0.52,
        };
        return Math.Max(24, (int)(basePt * scale));
    }

    private async Task<(int Width, int Height)> IdentifyDimensionsAsync(byte[] imageBytes, string extension, CancellationToken ct)
    {
        var tmp = Path.GetTempFileName() + $".{extension}";
        try
        {
            await File.WriteAllBytesAsync(tmp, imageBytes, ct);
            var psi = new ProcessStartInfo
            {
                FileName = "magick",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("identify");
            psi.ArgumentList.Add("-format");
            psi.ArgumentList.Add("%w %h");
            psi.ArgumentList.Add(tmp);

            using var p = Process.Start(psi)!;
            var outText = await p.StandardOutput.ReadToEndAsync(ct);
            var errText = await p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"magick identify exited {p.ExitCode}: {errText}");

            // A multi-layer PNG (post -layers merge) can report "%w %h" once per frame;
            // take the first pair.
            var parts = outText.Trim().Split(' ', '\n')[..2];
            return (int.Parse(parts[0]), int.Parse(parts[1]));
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }

    private async Task RunMagickAsync(List<string> args, CancellationToken ct)
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

        log.LogInformation("[cover-title] magick {Args}", string.Join(' ', args));

        using var p = Process.Start(psi)!;
        var errTask = p.StandardError.ReadToEndAsync(ct);
        await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        var err = await errTask;
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"magick exited {p.ExitCode}: {err}");
    }
}
