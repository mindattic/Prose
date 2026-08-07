using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Single global "story time" cursor — what wall-clock instant the world is
/// currently at. Stories that overlap on this timeline let us catch
/// contradictions like "character X is in Old Town but also in Milwaukee at the
/// same minute."
///
/// Stored in the existing <see cref="Setting"/> table under key <c>story_now</c>.
/// Persisted as ISO-8601 round-trip with 100ns precision to match every other
/// story-time column in the schema (<c>datetime2(7)</c>).
/// </summary>
public class WorldClockService
{
    public const string SettingKey = "story_now";

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly object cacheLock = new();
    private DateTime? cached;

    /// <summary>
    /// Default story-time when no row exists yet — opening of the canon timeline.
    /// 2256 keeps it well within the schema's 23rd-century convention.
    /// </summary>
    public static readonly DateTime CanonStart = new(2256, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public WorldClockService(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>Fired whenever <see cref="SetNow"/> commits a new value.</summary>
    public event Action<DateTime>? OnNowChanged;

    /// <summary>Read the current story-time. Cached after first read.</summary>
    public DateTime GetNow()
    {
        lock (cacheLock) { if (cached.HasValue) return cached.Value; }

        using var db = dbFactory.CreateDbContext();
        var row = db.Settings.AsNoTracking().FirstOrDefault(s => s.Key == SettingKey);
        var now = ParseOrDefault(row?.Json);
        lock (cacheLock) cached = now;
        return now;
    }

    /// <summary>Set the current story-time. Persists synchronously.</summary>
    public void SetNow(DateTime when)
    {
        var utc = when.Kind switch
        {
            DateTimeKind.Utc         => when,
            DateTimeKind.Local       => when.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(when, DateTimeKind.Utc),
            _ => when,
        };

        using var db = dbFactory.CreateDbContext();
        var row = db.Settings.FirstOrDefault(s => s.Key == SettingKey);
        var iso = utc.ToString("o");
        if (row == null)
            db.Settings.Add(new Setting { Key = SettingKey, Json = iso, UpdatedAt = DateTime.UtcNow });
        else
        {
            row.Json = iso;
            row.UpdatedAt = DateTime.UtcNow;
        }
        db.SaveChanges();

        lock (cacheLock) cached = utc;
        try { OnNowChanged?.Invoke(utc); } catch { /* never fail SetNow on subscriber error */ }
    }

    public void Reload()
    {
        lock (cacheLock) cached = null;
    }

    private static DateTime ParseOrDefault(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s)
            && DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : dt.ToUniversalTime();
        return CanonStart;
    }
}
