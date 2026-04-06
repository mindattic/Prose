using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Story repository — one folder per story under stories/.
/// Folder naming: {title_slug}.{guid}/
/// Files inside: story.json, checkpoint.json, outline.json, events.json, knowledge.json
/// </summary>
public class JsonStoryBlockRepository : IStoryBlockRepository
{
    private readonly IPathProvider paths;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string StoryDir => paths.StoriesDir;
    private string ArchiveDir => paths.ArchiveDir;

    private readonly ILogger<JsonStoryBlockRepository> log;

    public JsonStoryBlockRepository(IPathProvider paths, ILogger<JsonStoryBlockRepository> log)
    {
        this.paths = paths;
        this.log = log;
        MigrateFlatFiles();
    }

    public List<StoryProject> ListProjects()
    {
        var dir = StoryDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetDirectories(dir)
            .Select(d => LoadFromFile(Path.Combine(d, "story.json")))
            .Where(p => p != null)
            .DistinctBy(p => p!.Id)
            .OrderByDescending(p => p!.Modified)
            .ToList()!;
    }

    public StoryProject? LoadProject(string id)
    {
        var path = StoryFolderHelper.FindFile(StoryDir, id, "story.json");
        return path != null ? LoadFromFile(path) : null;
    }

    public void SaveProject(StoryProject project)
    {
        project.Modified = DateTime.UtcNow;

        // Get or create folder, renaming if title changed
        var desiredName = StoryFolderHelper.BuildFolderName(project.Id, project.Title);
        var desiredPath = Path.Combine(StoryDir, desiredName);
        var existing = StoryFolderHelper.FindFolder(StoryDir, project.Id);

        if (existing != null && !string.Equals(existing, desiredPath, StringComparison.OrdinalIgnoreCase))
        {
            try { Directory.Move(existing, desiredPath); }
            catch { desiredPath = existing; }
        }
        else if (existing == null)
        {
            Directory.CreateDirectory(desiredPath);
        }

        var storyPath = Path.Combine(desiredPath, "story.json");
        log.LogDebug("Saving story project {Id} to {Path}", project.Id, storyPath);
        File.WriteAllText(storyPath, JsonSerializer.Serialize(project, JsonOpts));
    }

    public void DeleteProject(string id)
    {
        var folder = StoryFolderHelper.FindFolder(StoryDir, id);
        if (folder == null) return;

        var archiveFolder = Path.Combine(ArchiveDir, Path.GetFileName(folder));
        if (Directory.Exists(archiveFolder))
            Directory.Delete(archiveFolder, true);
        Directory.Move(folder, archiveFolder);
    }

    /// <summary>Migrate legacy flat files into folder structure on first run.</summary>
    private void MigrateFlatFiles()
    {
        var dir = StoryDir;
        if (!Directory.Exists(dir)) return;

        var storyFiles = Directory.GetFiles(dir, "*.story.json");
        foreach (var storyFile in storyFiles)
        {
            try
            {
                var project = LoadFromFile(storyFile);
                if (project == null) continue;

                var folderName = StoryFolderHelper.BuildFolderName(project.Id, project.Title);
                var folderPath = Path.Combine(dir, folderName);
                Directory.CreateDirectory(folderPath);

                File.Move(storyFile, Path.Combine(folderPath, "story.json"), overwrite: true);

                foreach (var suffix in new[] { "checkpoint", "outline", "events", "knowledge" })
                {
                    var candidates = Directory.GetFiles(dir, $"*{project.Id}.{suffix}.json");
                    foreach (var f in candidates)
                        File.Move(f, Path.Combine(folderPath, $"{suffix}.json"), overwrite: true);
                }

                log.LogInformation("Migrated story '{Title}' to folder {Folder}", project.Title, folderName);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to migrate story file {File}", storyFile);
            }
        }
    }

    private static StoryProject? LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StoryProject>(json);
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to load story project from {Path}", path); return null; }
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
