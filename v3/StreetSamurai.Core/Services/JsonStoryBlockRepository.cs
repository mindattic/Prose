using System.Text.Json;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// JSON-file-backed implementation of IStoryBlockRepository.
/// One JSON file per story project: {prefix}.json in the story_blocks directory.
/// Designed for eventual replacement by a database-backed implementation.
/// </summary>
public class JsonStoryBlockRepository : IStoryBlockRepository
{
    private readonly ICanonPathProvider _paths;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
    };

    private string StoryBlocksDir => EnsureDir(Path.Combine(_paths.CanonRoot, "story_blocks"));

    public JsonStoryBlockRepository(ICanonPathProvider paths)
    {
        _paths = paths;
    }

    public List<StoryProject> ListProjects()
    {
        var dir = StoryBlocksDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.json")
            .Select(LoadFromFile)
            .Where(p => p != null)
            .OrderByDescending(p => p!.Modified)
            .ToList()!;
    }

    public StoryProject? LoadProject(string id)
    {
        var dir = StoryBlocksDir;
        if (!Directory.Exists(dir)) return null;

        // Find by ID — scan files since the filename is the prefix, not the ID
        return Directory.GetFiles(dir, "*.json")
            .Select(LoadFromFile)
            .FirstOrDefault(p => p?.Id == id);
    }

    public void SaveProject(StoryProject project)
    {
        project.Modified = DateTime.UtcNow;

        // Clean up old file if prefix changed (filename is prefix-based)
        CleanupOldFiles(project.Id, project.Prefix);

        var path = ProjectPath(project.Prefix);
        var json = JsonSerializer.Serialize(project, JsonOpts);
        File.WriteAllText(path, json);
    }

    public void DeleteProject(string id)
    {
        var project = LoadProject(id);
        if (project == null) return;

        var path = ProjectPath(project.Prefix);
        if (File.Exists(path))
            File.Delete(path);
    }

    public StoryProject RenamePrefix(string projectId, string newPrefix)
    {
        newPrefix = StoryProject.SanitizePrefix(newPrefix);

        var project = LoadProject(projectId)
            ?? throw new InvalidOperationException($"Project {projectId} not found");

        var oldPath = ProjectPath(project.Prefix);

        // Rename prefix and all block IDs
        project.RenamePrefix(newPrefix);

        // Write to new path
        var newPath = ProjectPath(newPrefix);
        var json = JsonSerializer.Serialize(project, JsonOpts);
        File.WriteAllText(newPath, json);

        // Delete old file if it's different
        if (oldPath != newPath && File.Exists(oldPath))
            File.Delete(oldPath);

        return project;
    }

    public bool PrefixExists(string prefix, string? excludeProjectId = null)
    {
        prefix = StoryProject.SanitizePrefix(prefix);
        var path = ProjectPath(prefix);
        if (!File.Exists(path)) return false;

        if (excludeProjectId != null)
        {
            var existing = LoadFromFile(path);
            return existing != null && existing.Id != excludeProjectId;
        }

        return true;
    }

    // ── Private ──

    private string ProjectPath(string prefix) =>
        Path.Combine(StoryBlocksDir, $"{prefix}.json");

    private void CleanupOldFiles(string projectId, string currentPrefix)
    {
        var dir = StoryBlocksDir;
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            if (Path.GetFileNameWithoutExtension(file).Equals(currentPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var existing = LoadFromFile(file);
            if (existing?.Id == projectId)
                File.Delete(file);
        }
    }

    private static StoryProject? LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StoryProject>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
