using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Chapter repository — one folder per chapter under stories/.
/// Folder naming: {title_slug}.{guid}/
/// Files inside: story.json (legacy filename, kept for backward compat), checkpoint.json, outline.json, events.json, knowledge.json
/// The on-disk filename stays "story.json" so existing data isn't migrated; the in-memory type is Chapter.
/// </summary>
public class JsonChapterRepository : IChapterRepository
{
    private readonly IPathProvider paths;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string StoryDir => paths.StoriesDir;
    private string ArchiveDir => paths.ArchiveDir;

    private readonly ILogger<JsonChapterRepository> log;

    public JsonChapterRepository(IPathProvider paths, ILogger<JsonChapterRepository> log)
    {
        this.paths = paths;
        this.log = log;
        MigrateFlatFiles();
    }

    public List<Chapter> ListChapters()
    {
        var dir = StoryDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetDirectories(dir)
            .Select(d => LoadFromFile(Path.Combine(d, "story.json")))
            .Where(c => c != null)
            .DistinctBy(c => c!.Id)
            .OrderByDescending(c => c!.Modified)
            .ToList()!;
    }

    public Chapter? LoadChapter(string id)
    {
        var path = StoryFolderHelper.FindFile(StoryDir, id, "story.json");
        return path != null ? LoadFromFile(path) : null;
    }

    public void SaveChapter(Chapter chapter)
    {
        chapter.Modified = DateTime.UtcNow;

        // Get or create folder, renaming if title changed
        var desiredName = StoryFolderHelper.BuildFolderName(chapter.Id, chapter.Title);
        var desiredPath = Path.Combine(StoryDir, desiredName);
        var existing = StoryFolderHelper.FindFolder(StoryDir, chapter.Id);

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
        log.LogDebug("Saving chapter {Id} to {Path}", chapter.Id, storyPath);
        File.WriteAllText(storyPath, JsonSerializer.Serialize(chapter, JsonOpts));
    }

    public void DeleteChapter(string id)
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
                var chapter = LoadFromFile(storyFile);
                if (chapter == null) continue;

                var folderName = StoryFolderHelper.BuildFolderName(chapter.Id, chapter.Title);
                var folderPath = Path.Combine(dir, folderName);
                Directory.CreateDirectory(folderPath);

                File.Move(storyFile, Path.Combine(folderPath, "story.json"), overwrite: true);

                foreach (var suffix in new[] { "checkpoint", "outline", "events", "knowledge" })
                {
                    var candidates = Directory.GetFiles(dir, $"*{chapter.Id}.{suffix}.json");
                    foreach (var f in candidates)
                        File.Move(f, Path.Combine(folderPath, $"{suffix}.json"), overwrite: true);
                }

                log.LogInformation("Migrated chapter '{Title}' to folder {Folder}", chapter.Title, folderName);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to migrate chapter file {File}", storyFile);
            }
        }
    }

    private static Chapter? LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Chapter>(json);
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to load chapter from {Path}", path); return null; }
    }
}
