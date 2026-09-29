namespace Prose.Core.Services;

/// <summary>
/// Finds legacy on-disk chapter folders.
/// Convention: chapters/{guid}/
/// Files inside: chapter.json, checkpoint.json, outline.json, events.json, knowledge.json
/// Title lives inside chapter.json — folder name is just the project ID.
/// </summary>
public static class StoryFolderHelper
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

    /// <summary>Find a specific file in a story folder.</summary>
    public static string? FindFile(string storiesDir, string projectId, string fileName)
    {
        var folder = FindFolder(storiesDir, projectId);
        if (folder == null) return null;
        var path = Path.Combine(folder, fileName);
        return File.Exists(path) ? path : null;
    }
}
