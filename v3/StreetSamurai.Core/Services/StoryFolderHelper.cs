using System.Text.RegularExpressions;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Shared logic for finding and creating chapter folders.
/// Convention: chapters/{guid}/
/// Files inside: chapter.json, checkpoint.json, outline.json, events.json, knowledge.json
/// Title lives inside chapter.json — folder name is just the project ID.
/// </summary>
public static partial class StoryFolderHelper
{
    /// <summary>Find the story folder for a given project ID.</summary>
    public static string? FindFolder(string storiesDir, string projectId)
    {
        if (string.IsNullOrEmpty(projectId) || !Directory.Exists(storiesDir)) return null;
        // Direct match: folder named exactly the project ID
        var direct = Path.Combine(storiesDir, projectId);
        if (Directory.Exists(direct)) return direct;
        // Legacy: folder ending with .{projectId}
        return Directory.GetDirectories(storiesDir, $"*.{projectId}").FirstOrDefault()
            ?? Directory.GetDirectories(storiesDir).FirstOrDefault(d => Path.GetFileName(d).Contains(projectId));
    }

    /// <summary>Get or create a story folder.</summary>
    public static string GetOrCreateFolder(string storiesDir, string projectId, string? title = null)
    {
        var existing = FindFolder(storiesDir, projectId);
        if (existing != null) return existing;

        var folderPath = Path.Combine(storiesDir, projectId);
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

    /// <summary>Build folder name — just the project ID. Title lives inside chapter.json.</summary>
    public static string BuildFolderName(string projectId, string? title = null)
    {
        return projectId;
    }
}
