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

    /// <summary>
    /// The universe a given config key belongs to: SHARED for the operational allow-list
    /// (action_configs / tts.rules / users.accounts / current_universe), otherwise the current
    /// universe (falling back to GLMZ when no universe context is wired — tests / pre-migration).
    /// Centralizes "which universe's row" so a per-universe voice/tone/register read can never
    /// silently resolve to another universe's config (RFC 0006 — no cross-universe prompt cards).
    /// </summary>
    private static Guid TargetUniverse(string key)
    {
        if (UniverseScope.SharedConfigKeys.Contains(key)) return Universe.SharedId;
        var scoped = UniverseScope.EffectiveId;
        return scoped != Guid.Empty ? scoped : Universe.GlmzId;
    }

    /// <summary>Read a deserialized payload by key, scoped to the current universe (then SHARED).
    /// Returns <c>default</c> when missing or unparseable — including when the key exists only in
    /// ANOTHER universe (so Fantasy never reads GLMZ's tone_bible / register / voice).</summary>
    public T? Get<T>(string key)
    {
        using var db = dbFactory.CreateDbContext();
        var target = TargetUniverse(key);
        var rows = db.Settings.AsNoTracking().Where(s => s.Key == key).ToList();
        var row = rows.FirstOrDefault(s => s.UniverseId == target)
               ?? rows.FirstOrDefault(s => s.UniverseId == Universe.SharedId);
        if (row == null || string.IsNullOrEmpty(row.Json)) return default;
        try { return JsonSerializer.Deserialize<T>(row.Json, JsonOpts); }
        catch { return default; }
    }

    /// <summary>True if a row exists for this key in the current universe (or SHARED).</summary>
    public bool Exists(string key)
    {
        using var db = dbFactory.CreateDbContext();
        var target = TargetUniverse(key);
        return db.Settings.AsNoTracking()
            .Any(s => s.Key == key && (s.UniverseId == target || s.UniverseId == Universe.SharedId));
    }

    /// <summary>Replace the row for <paramref name="key"/> in the current universe (insert if missing).
    /// Targets the correct universe's row explicitly so writing one universe's config never clobbers
    /// another's.</summary>
    public void Set<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOpts);
        using var db = dbFactory.CreateDbContext();
        var target = TargetUniverse(key);
        var row = db.Settings.FirstOrDefault(s => s.Key == key && s.UniverseId == target);
        if (row == null)
            db.Settings.Add(new Setting { Key = key, Json = json, UniverseId = target, UpdatedAt = DateTime.UtcNow });
        else
        {
            row.Json = json;
            row.UpdatedAt = DateTime.UtcNow;
        }
        db.SaveChanges();
    }

    /// <summary>Remove the current universe's row for <paramref name="key"/>. No-op when missing.</summary>
    public void Delete(string key)
    {
        using var db = dbFactory.CreateDbContext();
        var target = TargetUniverse(key);
        var row = db.Settings.FirstOrDefault(s => s.Key == key && s.UniverseId == target);
        if (row == null) return;
        db.Settings.Remove(row);
        db.SaveChanges();
    }
}
