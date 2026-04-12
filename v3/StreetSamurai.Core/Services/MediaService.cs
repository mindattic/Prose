using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Manages media files (images, video, 3D models) stored in engine/data/media/.
/// Files are named {entityId}.{index:D2}.{ext} — e.g. abc123.00.png
/// This links any entity to one or more media files by matching on entity ID prefix.
/// </summary>
public class MediaService
{
    private readonly string mediaDir;
    private readonly string archiveDir;

    // Supported extensions by media type
    private static readonly string[] ImageExts   = [".png", ".jpg", ".jpeg", ".webp", ".gif"];
    private static readonly string[] VideoExts   = [".mp4", ".webm", ".mov"];
    private static readonly string[] ModelExts   = [".glb", ".gltf", ".obj"];

    public MediaService(IPathProvider paths)
    {
        mediaDir   = paths.MediaDir;
        archiveDir = paths.MediaArchiveDir;
    }

    /// <summary>Returns all media filenames for the given entity ID, sorted by index.</summary>
    public IReadOnlyList<string> GetFilesForEntity(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId)) return [];
        return Directory.EnumerateFiles(mediaDir, $"{entityId}.*")
            .Where(f => IsKnownExtension(f))
            .OrderBy(f => f)
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToList();
    }

    /// <summary>Returns the primary (first) image filename for an entity, or null if none.</summary>
    public string? GetPrimaryImage(string entityId)
    {
        return GetFilesForEntity(entityId)
            .FirstOrDefault(f => ImageExts.Contains(Path.GetExtension(f).ToLower()));
    }

    /// <summary>Returns true if the entity has at least one media file.</summary>
    public bool HasMedia(string entityId) => GetFilesForEntity(entityId).Count > 0;

    /// <summary>Returns the full disk path for a media filename (no path traversal).</summary>
    public string? GetPath(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return null;
        var safe = Path.GetFileName(filename);
        var full = Path.Combine(mediaDir, safe);
        return File.Exists(full) ? full : null;
    }

    /// <summary>Returns the MIME type for a media file based on its extension.</summary>
    public static string GetMimeType(string filename) => Path.GetExtension(filename).ToLower() switch
    {
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif"  => "image/gif",
        ".mp4"  => "video/mp4",
        ".webm" => "video/webm",
        ".mov"  => "video/quicktime",
        ".glb"  => "model/gltf-binary",
        ".gltf" => "model/gltf+json",
        _ => "application/octet-stream",
    };

    /// <summary>Returns all entity IDs that have at least one image.</summary>
    public IReadOnlyList<string> GetEntityIdsWithImages()
    {
        return Directory.EnumerateFiles(mediaDir)
            .Where(f => ImageExts.Contains(Path.GetExtension(f).ToLower()))
            .Select(f => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))) // strip .00 then extension
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }

    /// <summary>Returns a random image filename from the media directory, or null if empty.</summary>
    public string? GetRandomImage()
    {
        var images = Directory.EnumerateFiles(mediaDir)
            .Where(f => ImageExts.Contains(Path.GetExtension(f).ToLower()))
            .Select(Path.GetFileName)
            .OfType<string>()
            .ToList();
        return images.Count == 0 ? null : images[Random.Shared.Next(images.Count)];
    }

    /// <summary>Archives a media file by moving it to the archive directory.</summary>
    public bool Archive(string filename)
    {
        var src = GetPath(filename);
        if (src == null) return false;
        var dest = Path.Combine(archiveDir, filename);
        File.Move(src, dest, overwrite: false);
        return true;
    }

    private static bool IsKnownExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ImageExts.Contains(ext) || VideoExts.Contains(ext) || ModelExts.Contains(ext);
    }
}
