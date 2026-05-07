using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tiny strongly-typed KV façade over the universal <see cref="Setting"/> table.
/// Replaces the dozens of <c>File.WriteAllText("{engine_data}/foo.json", json)</c>
/// snippets that used to live in BookOutlineService, BookReviewService,
/// MotifService, ConsequenceEngine, ReputationTracker, NamePoolService, etc.
///
/// Schema is dirt-simple: one row per logical store (key = arbitrary string,
/// json = serialized payload). System-versioning gives us free history.
/// </summary>
public class SettingsKvStore
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    public SettingsKvStore(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>Read a deserialized payload by key. Returns <c>default</c> when missing or unparseable.</summary>
    public T? Get<T>(string key)
    {
        using var db = dbFactory.CreateDbContext();
        var row = db.Settings.AsNoTracking().FirstOrDefault(s => s.Key == key);
        if (row == null || string.IsNullOrEmpty(row.Json)) return default;
        try { return JsonSerializer.Deserialize<T>(row.Json, JsonOpts); }
        catch { return default; }
    }

    /// <summary>True if a row exists for this key.</summary>
    public bool Exists(string key)
    {
        using var db = dbFactory.CreateDbContext();
        return db.Settings.AsNoTracking().Any(s => s.Key == key);
    }

    /// <summary>Replace the row for <paramref name="key"/> (insert if missing).</summary>
    public void Set<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOpts);
        using var db = dbFactory.CreateDbContext();
        var row = db.Settings.FirstOrDefault(s => s.Key == key);
        if (row == null)
            db.Settings.Add(new Setting { Key = key, Json = json, UpdatedAt = DateTime.UtcNow });
        else
        {
            row.Json = json;
            row.UpdatedAt = DateTime.UtcNow;
        }
        db.SaveChanges();
    }

    /// <summary>Remove the row for <paramref name="key"/>. No-op when missing.</summary>
    public void Delete(string key)
    {
        using var db = dbFactory.CreateDbContext();
        var row = db.Settings.FirstOrDefault(s => s.Key == key);
        if (row == null) return;
        db.Settings.Remove(row);
        db.SaveChanges();
    }
}
