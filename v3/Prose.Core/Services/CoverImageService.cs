using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Facade over the pluggable <see cref="ICoverImageProvider"/> backends (openai /
/// stability / google). Resolves a node's <see cref="Data.Entities.Node.CoverPrompt"/>
/// (generating one via <see cref="CoverPromptService"/> first if it's missing) and calls
/// the chosen provider, saving the result under {MediaDir}/covers/{slug}.{ext} and
/// recording the path/provider/timestamp on the node row.
///
/// The title is rendered BY the image model itself, as instructed in the CoverPrompt
/// (see CoverPromptService) — not composited on afterward. <see cref="CoverTitleCompositorService"/>
/// (<c>ss --composite-cover-title</c>) remains available as a manual fallback for a
/// specific render whose in-image text comes out garbled, but is not run automatically.
/// </summary>
public class CoverImageService
{
    private readonly IReadOnlyDictionary<string, ICoverImageProvider> providers;
    private readonly CoverPromptService coverPrompts;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly ILogger<CoverImageService> log;

    public CoverImageService(
        IEnumerable<ICoverImageProvider> providers,
        CoverPromptService coverPrompts,
        IDbContextFactory<ProseDbContext> dbFactory,
        IPathProvider paths,
        ILogger<CoverImageService> log)
    {
        this.providers    = providers.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        this.coverPrompts = coverPrompts;
        this.dbFactory    = dbFactory;
        this.paths        = paths;
        this.log          = log;
    }

    /// <summary>Provider ids this build has an adapter for, and whether each has an API key configured.</summary>
    public IReadOnlyList<(string Id, bool Configured)> AvailableProviders
        => providers.Values.Select(p => (p.Id, p.IsConfigured)).ToList();

    /// <summary>
    /// Generates a cover image for <paramref name="nodeId"/> using the named provider
    /// ("openai" | "stability" | "google"), saves it, and returns the saved relative path.
    /// If the node has no CoverPrompt yet, one is generated first via CoverPromptService.
    /// </summary>
    public async Task<string> GenerateAndSaveAsync(Guid nodeId, string providerId, CancellationToken ct = default)
    {
        if (!providers.TryGetValue(providerId, out var provider))
            throw new ArgumentException($"Unknown cover image provider '{providerId}'. Available: {string.Join(", ", providers.Keys)}");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.FindAsync([nodeId], ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var prompt = node.CoverPrompt;
        if (string.IsNullOrWhiteSpace(prompt))
        {
            log.LogInformation("[cover-image] node {NodeId} has no CoverPrompt yet — generating one first", nodeId);
            prompt = await coverPrompts.GenerateAndSaveAsync(nodeId, ct);
        }

        var result = await provider.GenerateAsync(prompt, ct);

        var coversDir = Path.Combine(paths.MediaDir, "covers");
        Directory.CreateDirectory(coversDir);
        var filename = $"{node.Slug}.{result.Extension}";
        await File.WriteAllBytesAsync(Path.Combine(coversDir, filename), result.Bytes, ct);

        var relativePath = $"covers/{filename}";

        // Re-fetch for update — the node row above was read in the same context but a
        // long-running image call shouldn't hold a tracked entity across the await.
        var row = await db.Nodes.FindAsync([nodeId], ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        row.CoverImagePath        = relativePath;
        row.CoverImageProvider    = provider.Id;
        row.CoverImageGeneratedAt = DateTime.UtcNow;
        row.UpdatedAt             = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("[cover-image] saved {Path} for node {NodeId} via {Provider}", relativePath, nodeId, provider.Id);
        return relativePath;
    }

    /// <summary>
    /// Ensures <paramref name="exportDir"/>/cover.jpg exists — generates one (via the
    /// first configured image provider) ONLY if that file is missing. A book with a
    /// cover.jpg already present is treated as having its cover ready for publish, so
    /// export never touches, regenerates, or overwrites an existing one. Returns the
    /// path to a newly-written cover.jpg, or null if one already existed or no image
    /// provider is configured (non-fatal — the caller should treat this as skippable,
    /// same as any other optional export artifact).
    /// </summary>
    public async Task<string?> EnsureExportCoverAsync(Guid nodeId, string exportDir, CancellationToken ct = default)
    {
        var coverPath = Path.Combine(exportDir, "cover.jpg");
        if (File.Exists(coverPath))
        {
            log.LogInformation("[cover-image] {Path} already exists — cover ready for publish, skipping generation", coverPath);
            return null;
        }

        var providerId = providers.Values.FirstOrDefault(p => p.IsConfigured)?.Id;
        if (providerId == null)
        {
            log.LogWarning("[cover-image] no image provider configured — skipping cover.jpg for {ExportDir}", exportDir);
            return null;
        }

        var relativePath = await GenerateAndSaveAsync(nodeId, providerId, ct);
        var sourcePath = Path.Combine(paths.MediaDir, relativePath);
        Directory.CreateDirectory(exportDir);
        await ConvertToJpgAsync(sourcePath, coverPath, ct);
        return coverPath;
    }

    private static async Task ConvertToJpgAsync(string sourcePath, string destPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "magick",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(sourcePath);
        psi.ArgumentList.Add("-quality");
        psi.ArgumentList.Add("92");
        psi.ArgumentList.Add(destPath);

        using var p = Process.Start(psi)!;
        var errTask = p.StandardError.ReadToEndAsync(ct);
        await p.StandardOutput.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        var err = await errTask;
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"magick convert to jpg exited {p.ExitCode}: {err}");
    }
}
