namespace StreetSamurai.Core.Interfaces;

/// <summary>
/// Abstraction over the bytes-on-disk vs bytes-in-blob choice for narrated
/// audio. Beat.AudioPath stores a backend-agnostic relative path that this
/// store knows how to map onto a real location:
/// <list type="bullet">
/// <item><see cref="LocalDiskAudioStore"/> — files under <c>{MutableDataDir}/strands/{slug}/audio/…</c>.
///   Right for dev, single-instance Azure App Service with <c>SS_MUTABLE_DATA_ROOT</c>,
///   and any deployment where one process owns the bytes.</item>
/// <item><c>AzureBlobAudioStore</c> — bytes in an Azure Blob container.
///   Right for horizontal scale-out where multiple app instances must read
///   and write the same audio without a shared filesystem. Read-back uses
///   short-lived SAS URLs so browser audio elements stream directly from
///   blob storage (range requests, CDN-able) instead of proxying through
///   the app.</item>
/// </list>
/// Selection is via configuration: set <c>AudioStore:Provider</c> to
/// <c>"local"</c> (default) or <c>"azureblob"</c>. The blob provider also
/// needs <c>AudioStore:ConnectionString</c> and <c>AudioStore:Container</c>.
///
/// One contract for the workbench: it calls <see cref="WriteBeatAsync"/> /
/// <see cref="DeleteAsync"/> / <see cref="ExistsAsync"/> / <see cref="OpenReadAsync"/>
/// / <see cref="ResolveLocalPathAsync"/> without caring where the bytes
/// actually live.
/// </summary>
public interface IAudioStore
{
    /// <summary>True when the configured backend is local-disk and callers
    /// can use absolute file paths (needed by ffmpeg's concat demuxer).
    /// Blob-backed stores set this to false; the combined-audio export must
    /// then either stream-and-stage to a temp dir first, or use the naive
    /// byte-concat fallback that doesn't need ffmpeg.</summary>
    bool SupportsLocalPaths { get; }

    /// <summary>Persist one beat's audio bytes and return the relative path
    /// that should be stamped onto Beat.AudioPath. The relative path is
    /// canonical across stores: <c>{slug}/audio/{beatId:N}.{ext}</c>.</summary>
    Task<string> WriteBeatAsync(string strandSlug, Guid beatId, string extension, byte[] bytes, CancellationToken ct = default);

    /// <summary>Persist a strand's combined audio at <c>{slug}/strand.{ext}</c>.
    /// Returns the relative path for Strand.CombinedAudioPath.</summary>
    Task<string> WriteCombinedAsync(string strandSlug, string extension, byte[] bytes, CancellationToken ct = default);

    /// <summary>Delete a relative path. No-op if absent — callers should not
    /// have to check first.</summary>
    Task DeleteAsync(string relativePath, CancellationToken ct = default);

    /// <summary>True if a relative path resolves to a present resource.</summary>
    Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Open a readable stream of the bytes at <paramref name="relativePath"/>,
    /// or null when missing. Callers must dispose. Used by the audio HTTP
    /// endpoint when the backend can't hand out direct browser URLs.</summary>
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Resolve a relative path to an absolute local file path, or
    /// null when the backend doesn't keep a local copy (pure blob mode).
    /// ffmpeg-based silence pacing on the combined-audio export needs local
    /// paths; callers must fall back to temp-staging or the naive concat
    /// when this returns null.</summary>
    Task<string?> ResolveLocalPathAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Build a URL the browser can fetch directly. For local-disk
    /// this is the app's auth-proxied API endpoint; for blob it's a
    /// short-lived SAS URL pointing at the blob itself. The optional
    /// <paramref name="cacheBust"/> token (typically Beat.LastRequestId or
    /// UpdatedAt.Ticks) is appended as a query string so a re-record
    /// invalidates the browser cache without a path change.</summary>
    string BuildPlaybackUrl(Guid strandId, Guid beatId, string relativePath, string? cacheBust = null);

    /// <summary>Last-modified timestamp of the bytes at <paramref name="relativePath"/>,
    /// or null when the file/blob is absent. Used by the bidirectional
    /// reconciliation service to decide which side wins: newer timestamp
    /// is authoritative, ties (within ~2 s tolerance) are no-ops. Local
    /// disk reads File.GetLastWriteTimeUtc; blob reads BlobProperties.LastModified.</summary>
    Task<DateTimeOffset?> GetLastModifiedAsync(string relativePath, CancellationToken ct = default);
}
