using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Story repository — single JSON file per story, named by ID.
/// The HTML body is the source of truth (rich text with embedded images, entity links, etc.).
/// </summary>
public class JsonStoryBlockRepository : IStoryBlockRepository
{
    private readonly IPathProvider _paths;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string StoryDir => EnsureDir(Path.Combine(_paths.DataRoot, "story_blocks"));

    public JsonStoryBlockRepository(IPathProvider paths) => _paths = paths;

    public List<StoryProject> ListProjects()
    {
        var dir = StoryDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.json")
            .Select(LoadFromFile)
            .Where(p => p != null)
            .DistinctBy(p => p!.Id)
            .OrderByDescending(p => p!.Modified)
            .ToList()!;
    }

    public StoryProject? LoadProject(string id)
    {
        // Try direct file by ID
        var path = Path.Combine(StoryDir, $"{id}.json");
        if (File.Exists(path)) return LoadFromFile(path);

        // Fallback: scan all files (for legacy prefix-named files)
        return Directory.GetFiles(StoryDir, "*.json")
            .Select(LoadFromFile)
            .FirstOrDefault(p => p?.Id == id);
    }

    public void SaveProject(StoryProject project)
    {
        project.Modified = DateTime.UtcNow;
        var path = Path.Combine(StoryDir, $"{project.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(project, JsonOpts));

        // Clean up any legacy files with the same ID but different filename
        foreach (var file in Directory.GetFiles(StoryDir, "*.json"))
        {
            if (string.Equals(file, path, StringComparison.OrdinalIgnoreCase)) continue;
            var proj = LoadFromFile(file);
            if (proj?.Id == project.Id)
                File.Delete(file);
        }
    }

    public void DeleteProject(string id)
    {
        var path = Path.Combine(StoryDir, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);

        // Also clean up any legacy files with this ID
        foreach (var file in Directory.GetFiles(StoryDir, "*.json"))
        {
            var proj = LoadFromFile(file);
            if (proj?.Id == id && file != path)
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
        catch { return null; }
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
