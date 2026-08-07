using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// DALL·E 3 image generation. Generates images for canon entities and saves
/// them to engine/data/media/{entityId}.{index:D2}.png — always the next
/// available index so images stack: 00, 01, 02, … without overwriting.
///
/// Wire transport (endpoint, auth, payload, retries, circuit breaker) is
/// owned by MindAttic.Legion. This class keeps Prose-specific bits:
/// Midjourney-param stripping, prompt length capping, file naming/indexing.
/// </summary>
public class DallEService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;
    private readonly IPathProvider paths;
    private readonly ILogger<DallEService> log;

    public DallEService(LegionClient legion, SettingsService settings, IPathProvider paths, ILogger<DallEService> log)
    {
        this.legion   = legion;
        this.settings = settings;
        this.paths    = paths;
        this.log      = log;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.OpenAiApiKey);

    /// <summary>
    /// Generates an image via DALL·E 3 and saves it as {entityId}.{nextIndex:D2}.png.
    /// Images stack sequentially without overwriting existing ones.
    /// Returns the saved filename on success.
    /// </summary>
    public async Task<string> GenerateAndSaveAsync(string entityId, string prompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("OpenAI API key not configured. Add it under Settings → OpenAI API Key.");

        var cleanPrompt = StripMidjourneyParams(prompt.Trim());
        if (cleanPrompt.Length > 4000)
            cleanPrompt = cleanPrompt[..4000];

        log.LogInformation("DALL·E 3 generate via Legion: entityId={EntityId}, promptLen={Len}", entityId, cleanPrompt.Length);

        IReadOnlyList<byte[]> images;
        try
        {
            images = await legion.GenerateImageBytesAsync(
                providerId: "openai",
                apiKey: settings.OpenAiApiKey,
                model: "dall-e-3",
                prompt: cleanPrompt,
                size: "1024x1792",
                quality: "standard",
                n: 1,
                ct: ct);
        }
        catch (CircuitBreakerOpenException ex)
        {
            log.LogWarning("[Prose] DALL·E circuit breaker open: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "[Prose] DALL·E generation failed (entityId={EntityId}, status={Status})",
                entityId, ex.StatusCode);
            throw;
        }

        if (images.Count == 0 || images[0].Length == 0)
            throw new InvalidOperationException("DALL·E 3 returned no image data.");

        var bytes    = images[0];
        var idx      = NextImageIndex(entityId);
        var filename = $"{entityId}.{idx:D2}.png";
        var destPath = Path.Combine(paths.MediaDir, filename);

        await File.WriteAllBytesAsync(destPath, bytes, ct);
        log.LogInformation("DALL·E 3 saved: {Filename} ({Bytes} bytes)", filename, bytes.Length);
        return filename;
    }

    /// <summary>Returns the next available image index for this entity (0 if none exist).</summary>
    private int NextImageIndex(string entityId)
    {
        var existing = Directory.EnumerateFiles(paths.MediaDir, $"{entityId}.??.png").ToList();
        if (existing.Count == 0) return 0;
        var max = existing
            .Select(f => Path.GetFileNameWithoutExtension(f).Split('.').LastOrDefault())
            .Where(s => s != null && s.Length == 2 && int.TryParse(s, out _))
            .Select(s => int.Parse(s!))
            .DefaultIfEmpty(-1)
            .Max();
        return max + 1;
    }

    /// <summary>Strips Midjourney-style parameters (--ar 2:3, --v 6, etc.) that DALL·E rejects.</summary>
    private static string StripMidjourneyParams(string prompt)
        => Regex.Replace(prompt, @"\s*--\w[\w-]*(?:\s+\S+)?", "").Trim();
}
