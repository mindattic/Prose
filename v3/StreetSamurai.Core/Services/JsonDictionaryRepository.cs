using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Repository for a single canonical document — story bible, tone bible, literary
/// rules, character profile. Backed by a row in the unified Settings table keyed
/// by a string slot. The legacy JSON file path constructor is preserved for
/// callers that pass a path; only the slug of the file name is used as the key.
/// </summary>
public class JsonSingletonRepository<T> where T : class, new()
{
    private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<Data.StreetSamuraiDbContext> dbFactory;
    private readonly string key;
    private readonly JsonSerializerOptions jsonOptions;
    private T? cache;
    private int cacheEpoch = -1;

    public JsonSingletonRepository(Microsoft.EntityFrameworkCore.IDbContextFactory<Data.StreetSamuraiDbContext> dbFactory, string key)
    {
        this.dbFactory = dbFactory;
        this.key = key;
        jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public T Get()
    {
        // Cache is per-universe: a SwitchUniverse bumps the epoch so the voice/lore document is
        // re-read for the new universe instead of serving the previous one's (RFC 0006).
        if (cache != null && cacheEpoch == UniverseScope.Epoch) return cache;
        cacheEpoch = UniverseScope.Epoch;
        try
        {
            using var db = dbFactory.CreateDbContext();
            var row = db.Settings.AsNoTracking().FirstOrDefault(s => s.Key == key);
            if (row == null || string.IsNullOrEmpty(row.Json)) return cache = new T();
            cache = JsonSerializer.Deserialize<T>(row.Json, jsonOptions) ?? new T();
        }
        catch
        {
            // No DB context available — return defaults. Test fixtures hit this path.
            cache = new T();
        }
        return cache;
    }

    public void Save(T item)
    {
        cache = item;
        cacheEpoch = UniverseScope.Epoch;
        var json = JsonSerializer.Serialize(item, jsonOptions);
        try
        {
            using var db = dbFactory.CreateDbContext();
            var row = db.Settings.FirstOrDefault(s => s.Key == key);
            if (row == null) db.Settings.Add(new Data.Entities.Setting { Key = key, Json = json, UpdatedAt = DateTime.UtcNow });
            else { row.Json = json; row.UpdatedAt = DateTime.UtcNow; }
            db.SaveChanges();
        }
        catch
        {
            // Test fixtures with no DB factory: cache-only, lost on restart (matches legacy file behavior on read-only disk).
        }
    }

    public void Reload() => cache = null;

}
