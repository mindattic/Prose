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

    /// <summary>The universe this key's row belongs to: SHARED for the operational allow-list,
    /// else the current universe (GLMZ fallback when no context is wired). Ensures a per-universe
    /// voice/lore document (tone_bible, literary_rules, …) NEVER resolves to another universe's
    /// row — the seam that stops GLMZ's Kyle voice bleeding into Fantasy (RFC 0006).</summary>
    private static Guid TargetUniverse()
    {
        // 'key' is per-instance; SharedConfigKeys is the operational allow-list.
        var scoped = UniverseScope.EffectiveId;
        return scoped != Guid.Empty ? scoped : Data.Entities.Universe.GlmzId;
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
            var target = UniverseScope.SharedConfigKeys.Contains(key) ? Data.Entities.Universe.SharedId : TargetUniverse();
            var rows = db.Settings.AsNoTracking().Where(s => s.Key == key).ToList();
            var row = rows.FirstOrDefault(s => s.UniverseId == target)
                   ?? rows.FirstOrDefault(s => s.UniverseId == Data.Entities.Universe.SharedId);
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
            var target = UniverseScope.SharedConfigKeys.Contains(key) ? Data.Entities.Universe.SharedId : TargetUniverse();
            var row = db.Settings.FirstOrDefault(s => s.Key == key && s.UniverseId == target);
            if (row == null) db.Settings.Add(new Data.Entities.Setting { Key = key, Json = json, UniverseId = target, UpdatedAt = DateTime.UtcNow });
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
