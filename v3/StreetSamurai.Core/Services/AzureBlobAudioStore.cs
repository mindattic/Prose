using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Audio backend for horizontal scale-out: persists every beat's bytes to a
/// private Azure Blob container, hands the browser a short-lived SAS URL for
/// playback so range requests stream straight from blob storage (no app
/// proxy in the data path).
///
/// Selected by config:
/// <code>
///   AudioStore:Provider          = "azureblob"
///   AudioStore:ConnectionString  = "DefaultEndpointsProtocol=https;…"
///   AudioStore:Container         = "nodes-audio"          // default
///   AudioStore:SasTtlMinutes     = "30"                     // default 30 min
/// </code>
/// The connection string can also be supplied via the env-var fallback
/// <c>AudioStore__ConnectionString</c>. Container is auto-created on first
/// write (private; no public anonymous access).
///
/// <para>SAS URL caveat: the playback URL embeds a delegated SAS token that
/// expires after <c>SasTtlMinutes</c>. The Razor page rebinds the audio
/// element each load, so a tab kept open longer than the TTL will need to
/// refresh — acceptable for a writer/listener workflow. For end-user
/// distribution where URLs must outlive sessions, point a CDN at the
/// container with a longer-lived shared access signature instead.</para>
///
/// <para>ResolveLocalPathAsync returns null. The combined-audio export
/// must therefore stage temp copies of each beat's blob to the local
/// filesystem before invoking ffmpeg, OR fall back to the naive byte-concat
/// path that doesn't need ffmpeg (loses silence pacing precision).</para>
/// </summary>
public class AzureBlobAudioStore : IAudioStore
{
    private readonly BlobContainerClient container;
    private readonly TimeSpan sasTtl;
    private readonly ILogger<AzureBlobAudioStore> log;
    // Container creation is lazy: the ctor used to call CreateIfNotExists
    // synchronously, which blocked app startup on a network round-trip to
    // Azure (and broke startup entirely when blob was unreachable). Now we
    // ensure-once before the first write; reads don't need it (a missing
    // container yields 404, which we already handle).
    //
    // Concurrent writers share the same in-flight Lazy<Task>; on failure the
    // field is replaced so the next caller actually retries. A bare int flag
    // would let thread B race past the CompareExchange while thread A is
    // still mid-network-call, then issue an upload against a container that
    // doesn't exist yet — 404.
    private Lazy<Task> ensureOnce;

    public AzureBlobAudioStore(IConfiguration config, ILogger<AzureBlobAudioStore> log)
    {
        this.log = log;
        var connStr = config["MindAttic:Vault:AudioStore:connectionString"]
            ?? config["AudioStore:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("AudioStore__ConnectionString")
            ?? throw new InvalidOperationException("AzureBlobAudioStore requires AudioStore:ConnectionString.");
        var containerName = config["MindAttic:Vault:AudioStore:container"]
            ?? config["AudioStore:Container"]
            ?? "nodes-audio";
        var ttlMinutes = int.TryParse(config["AudioStore:SasTtlMinutes"], out var t) ? t : 30;
        sasTtl = TimeSpan.FromMinutes(Math.Clamp(ttlMinutes, 1, 60 * 24));
        var serviceClient = new BlobServiceClient(connStr);
        container = serviceClient.GetBlobContainerClient(containerName);
        ensureOnce = BuildEnsureLazy();
    }

    private Lazy<Task> BuildEnsureLazy() => new(
        () => (Task)container.CreateIfNotExistsAsync(PublicAccessType.None),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Ensure the container exists. Concurrent writes share the
    /// same in-flight Task; on failure we swap in a fresh Lazy so the next
    /// caller retries. Reads skip this — a missing container surfaces as
    /// 404 from OpenReadAsync / ExistsAsync, which the rest of the code
    /// expects.</summary>
    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        var lazy = ensureOnce;
        try
        {
            await lazy.Value.WaitAsync(ct);
        }
        catch (Exception ex)
        {
            // Swap in a fresh Lazy so subsequent calls retry. Use Interlocked
            // to ensure only the first failing caller installs a replacement.
            Interlocked.CompareExchange(ref ensureOnce, BuildEnsureLazy(), lazy);
            log.LogError(ex, "Failed to ensure Azure blob container {Container}", container.Name);
            throw;
        }
    }

    public bool SupportsLocalPaths => false;

    public async Task<string> WriteBeatAsync(string nodeSlug, Guid beatId, string extension, byte[] bytes, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var ext = extension.TrimStart('.');
        var rel = $"{nodeSlug}/audio/{beatId:N}.{ext}";
        var blob = container.GetBlobClient(rel);
        using var ms = new MemoryStream(bytes);
        var headers = new BlobHttpHeaders { ContentType = MimeFor(ext) };
        await blob.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = headers }, ct);
        return rel;
    }

    public async Task<string> WriteCombinedAsync(string nodeSlug, string extension, byte[] bytes, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var ext = extension.TrimStart('.');
        var rel = $"{nodeSlug}/node.{ext}";
        var blob = container.GetBlobClient(rel);
        using var ms = new MemoryStream(bytes);
        var headers = new BlobHttpHeaders { ContentType = MimeFor(ext) };
        await blob.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = headers }, ct);
        return rel;
    }

    public async Task<string> WriteCombinedFromStreamAsync(string nodeSlug, string extension, Stream src, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var ext = extension.TrimStart('.');
        var rel = $"{nodeSlug}/node.{ext}";
        var blob = container.GetBlobClient(rel);
        var headers = new BlobHttpHeaders { ContentType = MimeFor(ext) };
        await blob.UploadAsync(src, new BlobUploadOptions { HttpHeaders = headers }, ct);
        return rel;
    }

    public async Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            await container.GetBlobClient(relativePath).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch (Exception ex) { log.LogWarning(ex, "Blob delete failed for {Rel}", relativePath); }
    }

    public async Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default)
    {
        try { return (await container.GetBlobClient(relativePath).ExistsAsync(ct)).Value; }
        catch { return false; }
    }

    public async Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        // Don't probe ExistsAsync first — that's a separate HTTP round-trip
        // and OpenReadAsync raises a typed 404 we can catch. Halves blob
        // read latency for the common (file-exists) path.
        try
        {
            var blob = container.GetBlobClient(relativePath);
            return await blob.OpenReadAsync(cancellationToken: ct);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404) { return null; }
        catch (Exception ex) { log.LogDebug(ex, "Blob open-read failed for {Rel}", relativePath); return null; }
    }

    /// <summary>Blob backend has no local file. Caller must stage to a temp
    /// directory if it needs a real path (the ffmpeg concat path does).</summary>
    public Task<string?> ResolveLocalPathAsync(string relativePath, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public string BuildPlaybackUrl(Guid nodeId, Guid beatId, string relativePath, string? cacheBust = null)
    {
        // Even with blob storage we route through the app's auth-checked
        // endpoint by default — the api/nodes/.../audio handler proxies
        // OpenReadAsync. This keeps the URL stable, auth-enforced, and
        // doesn't leak SAS tokens into browser history. The app can opt
        // into SAS direct-streaming later by overriding this method or
        // adding a config flag.
        return string.IsNullOrEmpty(cacheBust)
            ? $"/api/nodes/{nodeId}/beat/{beatId}/audio"
            : $"/api/nodes/{nodeId}/beat/{beatId}/audio?v={Uri.EscapeDataString(cacheBust)}";
    }

    public async Task<DateTimeOffset?> GetLastModifiedAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            var blob = container.GetBlobClient(relativePath);
            var props = await blob.GetPropertiesAsync(cancellationToken: ct);
            return props.Value.LastModified;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404) { return null; }
        catch (Exception ex) { log.LogDebug(ex, "Blob LastModified lookup failed for {Rel}", relativePath); return null; }
    }

    /// <summary>Mint a short-lived read SAS URL for direct browser streaming.
    /// Not used by the default <see cref="BuildPlaybackUrl"/> — exposed so
    /// callers that want CDN-style direct access (e.g. a public bookshelf
    /// page) can opt in.</summary>
    public Uri? TryBuildSasUrl(string relativePath)
    {
        try
        {
            var blob = container.GetBlobClient(relativePath);
            if (!blob.CanGenerateSasUri) return null;
            var sas = new BlobSasBuilder
            {
                BlobContainerName = container.Name,
                BlobName          = relativePath,
                Resource          = "b",
                StartsOn          = DateTimeOffset.UtcNow.AddMinutes(-2),
                ExpiresOn         = DateTimeOffset.UtcNow.Add(sasTtl),
            };
            sas.SetPermissions(BlobSasPermissions.Read);
            return blob.GenerateSasUri(sas);
        }
        catch (Exception ex) { log.LogDebug(ex, "SAS generation failed for {Rel}", relativePath); return null; }
    }

    private static string MimeFor(string ext) => ext.ToLowerInvariant() switch
    {
        "mp3" => "audio/mpeg",
        "wav" => "audio/wav",
        "m4a" or "mp4" or "m4b" => "audio/mp4",
        _ => "application/octet-stream",
    };
}
