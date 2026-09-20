using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// The historical (and still default) audio backend: writes MP3/WAV files
/// under <c>{MutableDataDir}/nodes/{slug}/audio/{beatId}.{ext}</c>. Cheap,
/// streaming-friendly, plays back via the existing
/// <c>/api/nodes/{nodeId}/beat/{beatId}/audio</c> minimal API endpoint
/// (which handles auth + range requests through ASP.NET Core's static-file
/// machinery).
///
/// Reads check two legacy locations for back-compat:
/// <list type="bullet">
/// <item><c>{DataRoot}/engine/strands/{slug}/audio/…</c> — files written
///   before the 2026-05-24 MutableDataDir migration.</item>
/// <item><c>{DataRoot}/engine/episodes/{slug}/…</c> — even older episode-era
///   recordings that haven't been re-narrated yet.</item>
/// </list>
/// </summary>
public class LocalDiskAudioStore : IAudioStore
{
    private readonly IPathProvider paths;
    private readonly ILogger<LocalDiskAudioStore> log;

    public LocalDiskAudioStore(IPathProvider paths, ILogger<LocalDiskAudioStore> log)
    {
        this.paths = paths;
        this.log = log;
    }

    public bool SupportsLocalPaths => true;

    private string PrimaryRoot => Path.Combine(paths.MutableDataDir, "nodes");

    public async Task<string> WriteBeatAsync(string nodeSlug, Guid beatId, string extension, byte[] bytes, CancellationToken ct = default)
    {
        var ext = extension.TrimStart('.');
        var rel = $"{nodeSlug}/audio/{beatId:N}.{ext}";
        var full = Path.Combine(PrimaryRoot, rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, bytes, ct);
        return rel;
    }

    public async Task<string> WriteCombinedAsync(string nodeSlug, string extension, byte[] bytes, CancellationToken ct = default)
    {
        var ext = extension.TrimStart('.');
        var rel = $"{nodeSlug}/node.{ext}";
        var full = Path.Combine(PrimaryRoot, rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, bytes, ct);
        return rel;
    }

    public async Task<string> WriteCombinedFromStreamAsync(string nodeSlug, string extension, Stream src, CancellationToken ct = default)
    {
        var ext = extension.TrimStart('.');
        var rel = $"{nodeSlug}/node.{ext}";
        var full = Path.Combine(PrimaryRoot, rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
        await src.CopyToAsync(fs, ct);
        return rel;
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            var full = ResolveExistingFile(relativePath);
            if (full != null && File.Exists(full)) File.Delete(full);
        }
        catch (Exception ex) { log.LogWarning(ex, "Could not delete audio at {Rel}", relativePath); }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default)
        => Task.FromResult(ResolveExistingFile(relativePath) != null);

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        var full = ResolveExistingFile(relativePath);
        if (full == null) return Task.FromResult<Stream?>(null);
        Stream s = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        return Task.FromResult<Stream?>(s);
    }

    public Task<string?> ResolveLocalPathAsync(string relativePath, CancellationToken ct = default)
        => Task.FromResult(ResolveExistingFile(relativePath));

    public string BuildPlaybackUrl(Guid nodeId, Guid beatId, string relativePath, string? cacheBust = null)
        => string.IsNullOrEmpty(cacheBust)
            ? $"/api/nodes/{nodeId}/beat/{beatId}/audio"
            : $"/api/nodes/{nodeId}/beat/{beatId}/audio?v={Uri.EscapeDataString(cacheBust)}";

    public Task<DateTimeOffset?> GetLastModifiedAsync(string relativePath, CancellationToken ct = default)
    {
        var full = ResolveExistingFile(relativePath);
        if (full == null) return Task.FromResult<DateTimeOffset?>(null);
        try { return Task.FromResult<DateTimeOffset?>(new DateTimeOffset(File.GetLastWriteTimeUtc(full), TimeSpan.Zero)); }
        catch { return Task.FromResult<DateTimeOffset?>(null); }
    }

    /// <summary>Resolve a relative path to an absolute file. Tries the
    /// canonical MutableDataDir location first, then the two legacy roots
    /// for files that haven't been re-recorded since the 2026-05-24 cutover.
    /// Returns null when none of the candidates exist.</summary>
    private string? ResolveExistingFile(string relativePath)
    {
        var rel = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var primary = Path.Combine(PrimaryRoot, rel);
        if (File.Exists(primary)) return primary;
        var legacyNodes = Path.Combine(paths.DataRoot, "engine", "nodes", rel);
        if (File.Exists(legacyNodes)) return legacyNodes;
        var legacyEpisodes = Path.Combine(paths.DataRoot, "engine", "episodes", rel);
        if (File.Exists(legacyEpisodes)) return legacyEpisodes;
        return null;
    }
}
