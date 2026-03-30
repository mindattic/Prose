using System.Text.Json;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class CanonQueueService
{
    private readonly ICanonPathProvider _paths;

    public CanonQueueService(ICanonPathProvider paths)
    {
        _paths = paths;
    }

    public List<CanonQueueEntry> ListAll()
    {
        var dir = _paths.CanonQueueDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir, "*.json")
            .Select(LoadEntry)
            .Where(e => e != null)
            .OrderByDescending(e => e!.Submitted)
            .ToList()!;
    }

    public List<CanonQueueEntry> ListByStatus(string status) =>
        ListAll().Where(e => e.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

    public void Submit(CanonQueueEntry entry)
    {
        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Sanitize(entry.Name)}.json";
        var path = Path.Combine(_paths.CanonQueueDir, fileName);
        var entryWithPath = entry with { FilePath = path, Submitted = DateTime.UtcNow, Status = "pending" };
        var json = JsonSerializer.Serialize(entryWithPath, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public void Promote(string filePath, string notes = "")
    {
        UpdateStatus(filePath, "promoted", notes);
    }

    public void Reject(string filePath, string reason = "")
    {
        UpdateStatus(filePath, "rejected", reason);
    }

    private void UpdateStatus(string filePath, string status, string notes)
    {
        var entry = LoadEntry(filePath);
        if (entry == null) return;

        var updated = entry with { Status = status, Notes = notes };
        var json = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private static CanonQueueEntry? LoadEntry(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<CanonQueueEntry>(json);
        }
        catch { return null; }
    }

    private static string Sanitize(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]", "_");
}
