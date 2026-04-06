using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Shared logic for finding and creating story folders.
/// Convention: stories/{title_slug}.{guid}/
/// Files inside: story.json, checkpoint.json, outline.json, events.json, knowledge.json
/// </summary>
public static partial class StoryFolderHelper
{
    /// <summary>Find the story folder for a given project ID.</summary>
    public static string? FindFolder(string storiesDir, string projectId)
    {
        if (string.IsNullOrEmpty(projectId) || !Directory.Exists(storiesDir)) return null;
        return Directory.GetDirectories(storiesDir, $"*.{projectId}").FirstOrDefault()
            ?? Directory.GetDirectories(storiesDir).FirstOrDefault(d => Path.GetFileName(d).Contains(projectId));
    }

    /// <summary>Get or create a story folder, with an optional title for naming.</summary>
    public static string GetOrCreateFolder(string storiesDir, string projectId, string? title = null)
    {
        var existing = FindFolder(storiesDir, projectId);
        if (existing != null) return existing;

        var folderName = BuildFolderName(projectId, title);
        var folderPath = Path.Combine(storiesDir, folderName);
        Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    /// <summary>Resolve a file path inside a story folder.</summary>
    public static string GetFilePath(string storiesDir, string projectId, string fileName, string? title = null)
    {
        var folder = GetOrCreateFolder(storiesDir, projectId, title);
        return Path.Combine(folder, fileName);
    }

    /// <summary>Find a specific file in a story folder.</summary>
    public static string? FindFile(string storiesDir, string projectId, string fileName)
    {
        var folder = FindFolder(storiesDir, projectId);
        if (folder == null) return null;
        var path = Path.Combine(folder, fileName);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Build folder name: title_slug.guid</summary>
    public static string BuildFolderName(string projectId, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(title) || title == Constants.Defaults.UntitledStory)
            return $"untitled.{projectId}";

        var slug = FolderSlug().Replace(title.ToLowerInvariant().Trim(), "_").Trim('_');
        if (slug.Length > 80) slug = slug[..80].TrimEnd('_');
        return $"{slug}.{projectId}";
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex FolderSlug();
}
