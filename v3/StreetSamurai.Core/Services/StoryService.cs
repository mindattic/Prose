using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public partial class StoryService
{
    private readonly IPathProvider paths;
    private readonly ILogger<StoryService> log;

    public StoryService(IPathProvider paths, ILogger<StoryService> log)
    {
        this.paths = paths;
        this.log = log;
    }

    public List<Story> ListStories()
    {
        var dir = paths.StoriesDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.md")
            .Select(LoadStoryFromFile)
            .Where(s => s != null)
            .OrderByDescending(s => s!.Modified)
            .ToList()!;
    }

    public Story? LoadStory(string id)
    {
        var dir = paths.StoriesDir;
        if (!Directory.Exists(dir)) return null;

        var file = Directory.GetFiles(dir, "*.md")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Contains(id, StringComparison.OrdinalIgnoreCase));

        return file != null ? LoadStoryFromFile(file) : null;
    }

    public void SaveStory(Story story)
    {
        var fileName = SanitizeFileName(story.Title) + ".md";
        var filePath = string.IsNullOrEmpty(story.FilePath)
            ? Path.Combine(paths.StoriesDir, fileName)
            : story.FilePath;

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"id: \"{story.Id}\"");
        sb.AppendLine($"title: \"{story.Title}\"");
        sb.AppendLine($"canon_status: {story.Status}");
        if (story.Characters.Count > 0)
            sb.AppendLine($"characters: [{string.Join(", ", story.Characters)}]");
        if (!string.IsNullOrEmpty(story.Location))
            sb.AppendLine($"location: \"{story.Location}\"");
        sb.AppendLine($"created: {story.Created:O}");
        sb.AppendLine($"modified: {DateTime.UtcNow:O}");
        if (story.Tags.Count > 0)
            sb.AppendLine($"tags: [{string.Join(", ", story.Tags)}]");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(story.MarkdownContent);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, sb.ToString());
    }

    public Story CreateNew(string title = Constants.Defaults.UntitledStory)
    {
        var story = new Story
        {
            Title = title,
            MarkdownContent = $"# {title}\n\n",
        };
        SaveStory(story);
        return story with { FilePath = Path.Combine(paths.StoriesDir, SanitizeFileName(title) + ".md") };
    }

    public void DeleteStory(string id)
    {
        var story = LoadStory(id);
        if (story != null && File.Exists(story.FilePath))
            File.Delete(story.FilePath);
    }

    private Story? LoadStoryFromFile(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            var fm = ExtractFrontMatter(text);
            var content = StripFrontMatter(text);

            return new Story
            {
                Id = GetFm(fm, "id") ?? Path.GetFileNameWithoutExtension(filePath),
                Title = GetFm(fm, "title") ?? Path.GetFileNameWithoutExtension(filePath),
                Status = GetFm(fm, "canon_status") ?? "draft",
                Location = GetFm(fm, "location"),
                Characters = ParseList(GetFm(fm, "characters") ?? ""),
                Tags = ParseList(GetFm(fm, "tags") ?? ""),
                Created = DateTime.TryParse(GetFm(fm, "created"), out var c) ? c : File.GetCreationTimeUtc(filePath),
                Modified = DateTime.TryParse(GetFm(fm, "modified"), out var m) ? m : File.GetLastWriteTimeUtc(filePath),
                MarkdownContent = content,
                FilePath = filePath,
            };
        }
        catch (Exception ex) { log.LogError(ex, "Failed to load story"); return null; }
    }

    private static Dictionary<string, string> ExtractFrontMatter(string text)
    {
        var fm = new Dictionary<string, string>();
        if (!text.StartsWith("---")) return fm;

        var end = text.IndexOf("---", 3, StringComparison.Ordinal);
        if (end < 0) return fm;

        var block = text[3..end];
        foreach (var line in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var key = line[..colon].Trim();
            var val = line[(colon + 1)..].Trim().Trim('"');
            fm[key] = val;
        }
        return fm;
    }

    private static string StripFrontMatter(string text)
    {
        if (!text.StartsWith("---")) return text;
        var end = text.IndexOf("---", 3, StringComparison.Ordinal);
        return end < 0 ? text : text[(end + 3)..].TrimStart('\r', '\n');
    }

    private static string? GetFm(Dictionary<string, string> fm, string key) =>
        fm.TryGetValue(key, out var val) ? val : null;

    private static List<string> ParseList(string val)
    {
        val = val.Trim('[', ']');
        return string.IsNullOrEmpty(val)
            ? []
            : val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string SanitizeFileName(string name) =>
        Regex.Replace(name.Trim(), @"[^\w\s-]", "").Replace(' ', '_').ToLowerInvariant();
}
