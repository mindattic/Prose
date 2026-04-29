using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Series repository — one JSON file per series under engine/data/series/.
/// Series group Books that share continuity. Books reference their parent series
/// via <see cref="Book.SeriesId"/>; this list is the canonical book order within
/// a series.
/// </summary>
public class JsonSeriesRepository : ISeriesRepository
{
    private readonly IPathProvider paths;
    private readonly ILogger<JsonSeriesRepository> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JsonSeriesRepository(IPathProvider paths, ILogger<JsonSeriesRepository> log)
    {
        this.paths = paths;
        this.log = log;
    }

    private string SeriesDir => paths.SeriesDir;
    private string ArchiveSeriesDir
    {
        get
        {
            var dir = Path.Combine(paths.ArchiveDir, Constants.Folders.Series);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public List<Series> ListSeries()
    {
        if (!Directory.Exists(SeriesDir)) return [];
        return Directory.GetFiles(SeriesDir, "*.json")
            .Select(LoadFromFile)
            .Where(s => s != null)
            .OrderByDescending(s => s!.Modified)
            .ToList()!;
    }

    public Series? LoadSeries(string id)
    {
        var path = Path.Combine(SeriesDir, $"{id}.json");
        return LoadFromFile(path);
    }

    public void SaveSeries(Series series)
    {
        series.Modified = DateTime.UtcNow;
        Directory.CreateDirectory(SeriesDir);
        var path = Path.Combine(SeriesDir, $"{series.Id}.json");
        log.LogDebug("Saving series {Id} to {Path}", series.Id, path);
        File.WriteAllText(path, JsonSerializer.Serialize(series, JsonOpts));
    }

    public void DeleteSeries(string id)
    {
        var path = Path.Combine(SeriesDir, $"{id}.json");
        if (!File.Exists(path)) return;

        var archivePath = Path.Combine(ArchiveSeriesDir, $"{id}.json");
        if (File.Exists(archivePath))
            archivePath = Path.Combine(ArchiveSeriesDir, $"{id}.{DateTime.UtcNow:yyyyMMddHHmmss}.json");
        File.Move(path, archivePath);
    }

    private static Series? LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<Series>(File.ReadAllText(path));
        }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Failed to load series from {Path}", path); return null; }
    }
}
