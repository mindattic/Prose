using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Story repository — single JSON file per story, named by ID.
/// The HTML body is the source of truth (rich text with embedded images, entity links, etc.).
/// </summary>
public class JsonStoryBlockRepository : IStoryBlockRepository
{
    private readonly IPathProvider paths;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string StoryDir => EnsureDir(Path.Combine(paths.DataRoot, "story_blocks"));

    private readonly ILogger<JsonStoryBlockRepository> log;

    public JsonStoryBlockRepository(IPathProvider paths, ILogger<JsonStoryBlockRepository> log)
    {
        this.paths = paths;
        this.log = log;
    }

    public List<StoryProject> ListProjects()
    {
        var dir = StoryDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.json")
            .Where(f => !IsArchived(f))
            .Select(LoadFromFile)
            .Where(p => p != null)
            .DistinctBy(p => p!.Id)
            .OrderByDescending(p => p!.Modified)
            .ToList()!;
    }

    private static bool IsArchived(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            if (!json.Contains("\"is_archived\"")) return false;
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("is_archived", out var val) && val.ValueKind == JsonValueKind.True;
        }
        catch { return false; }
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
        log.LogDebug("Saving story project {Id} to {Path}", project.Id, path);
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
        // Soft delete — set is_archived flag
        var filePath = Path.Combine(StoryDir, $"{id}.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var doc = JsonDocument.Parse(json);
                using var ms = new System.IO.MemoryStream();
                using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
                writer.WriteStartObject();
                bool wroteArchived = false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name == "is_archived") { writer.WriteBoolean("is_archived", true); wroteArchived = true; }
                    else prop.WriteTo(writer);
                }
                if (!wroteArchived) writer.WriteBoolean("is_archived", true);
                writer.WriteEndObject();
                writer.Flush();
                File.WriteAllText(filePath, System.Text.Encoding.UTF8.GetString(ms.ToArray()));
            }
            catch { File.Delete(filePath); } // Fallback to hard delete if JSON fails
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
