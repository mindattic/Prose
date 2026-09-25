using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Prose.Core.Services;

/// <summary>
/// Repository for a single canonical document — story bible, tone bible, literary
/// rules, character profile. Backed by a row in the unified Settings table keyed
/// by a string slot. The legacy JSON file path constructor is preserved for
/// callers that pass a path; only the slug of the file name is used as the key.
/// </summary>
public class JsonSingletonRepository<T> where T : class, new()
{
    private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<Data.ProseDbContext> dbFactory;
    private readonly string key;
    private readonly JsonSerializerOptions jsonOptions;
    // One immutable entry, swapped whole: value, the epoch it was read at, and WHICH universe's
    // row it is. The epoch alone is process-wide and any flow bumps it, so a SCRY request could
    // cache SCRY's literary_rules and a concurrent GLMZ request at the same epoch was served them.
    private sealed record Entry(T Value, int Epoch, Guid Target);
    private volatile Entry? entry;

    public JsonSingletonRepository(Microsoft.EntityFrameworkCore.IDbContextFactory<Data.ProseDbContext> dbFactory, string key)
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
        // Callers resolve SharedConfigKeys before calling; this method provides the universe fallback.
        var scoped = UniverseScope.EffectiveId;
        return scoped != Guid.Empty ? scoped : Data.Entities.Universe.GlmzId;
    }

    public T Get()
    {
        // Cache is per-universe: a SwitchUniverse bumps the epoch so the voice/lore document is
        // re-read for the new universe instead of serving the previous one's (RFC 0006).
        var epoch = UniverseScope.Epoch;
        var target = UniverseScope.SharedConfigKeys.Contains(key) ? Data.Entities.Universe.SharedId : TargetUniverse();
        var e = entry;
        if (e != null && e.Epoch == epoch && e.Target == target) return e.Value;
        T value;
        try
        {
            using var db = dbFactory.CreateDbContext();
            var rows = db.Settings.AsNoTracking().Where(s => s.Key == key).ToList();
            var row = rows.FirstOrDefault(s => s.UniverseId == target)
                   ?? rows.FirstOrDefault(s => s.UniverseId == Data.Entities.Universe.SharedId);
            value = row == null || string.IsNullOrEmpty(row.Json)
                ? new T()
                : JsonSerializer.Deserialize<T>(row.Json, jsonOptions) ?? new T();
        }
        catch
        {
            // No DB context available — return defaults. Test fixtures hit this path.
            value = new T();
        }
        // Stamped after the load, with the epoch read BEFORE it: a switch mid-load then
        // mismatches next time instead of being masked.
        entry = new Entry(value, epoch, target);
        return value;
    }

    public void Save(T item)
    {
        entry = new Entry(item, UniverseScope.Epoch,
            UniverseScope.SharedConfigKeys.Contains(key) ? Data.Entities.Universe.SharedId : TargetUniverse());
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

    public void Reload() => entry = null;

}
