using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using Series = StreetSamurai.Core.Models.Series;
using SeriesEntity = StreetSamurai.Core.Data.Entities.Series;
using EntityRow = StreetSamurai.Core.Data.Entities.Entity;
using RecordRow = StreetSamurai.Core.Data.Entities.Record;

namespace StreetSamurai.Core.Services;

/// <summary>EF-backed Series repository on the unified StreetSamurai database.</summary>
public class SeriesRepository : ISeriesRepository
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<SeriesRepository> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public SeriesRepository(IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<SeriesRepository> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Test-fixture ctor — wraps a SQLite-in-memory DbContextFactory keyed by path.</summary>
    public SeriesRepository(IPathProvider paths, ILogger<SeriesRepository> log)
    {
        this.dbFactory = TestDbFactory.For(paths, "series");
        this.log = log;
    }

    public List<Series> ListSeries()
    {
        using var db = dbFactory.CreateDbContext();
        var jsons = db.Records.AsNoTracking()
            .Where(r => r.Entity!.EntityType == "series" && r.Entity.IsActive)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.Json)
            .ToList();
        var list = new List<Series>(jsons.Count);
        foreach (var j in jsons)
        {
            try { var s = JsonSerializer.Deserialize<Series>(j, JsonOpts); if (s != null) list.Add(s); }
            catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize a series record"); }
        }
        return list;
    }

    public Series? LoadSeries(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var guid = ParseGuid(id);
        using var db = dbFactory.CreateDbContext();
        var json = db.Records.AsNoTracking()
            .Where(r => r.EntityId == guid)
            .Select(r => r.Json)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Series>(json, JsonOpts); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize Series {Id}", id); return null; }
    }

    public void SaveSeries(Series series)
    {
        if (string.IsNullOrEmpty(series.Id)) series.Id = Guid.CreateVersion7().ToString("N");
        var id = ParseGuid(series.Id);
        series.Modified = DateTime.UtcNow;

        using var db = dbFactory.CreateDbContext();

        var entity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (entity == null)
        {
            entity = new EntityRow
            {
                Id          = id,
                EntityType  = "series",
                Name        = series.Title,
                Slug        = WorldGraphService.Slugify(series.Title),
                Status      = "canon",
                Description = series.Premise,
                CreatedAt   = series.Created == default ? DateTime.UtcNow : series.Created,
                ModifiedAt  = DateTime.UtcNow,
                IsActive    = true,
            };
            db.Entities.Add(entity);
        }
        else
        {
            entity.Name        = series.Title;
            entity.Slug        = WorldGraphService.Slugify(series.Title);
            entity.Description = series.Premise;
            entity.ModifiedAt  = DateTime.UtcNow;
            entity.IsActive    = true;
            entity.ArchivedAt  = null;
        }

        var sub = db.SeriesItems.FirstOrDefault(s => s.Id == id);
        if (sub == null)
        {
            sub = new SeriesEntity { Id = id };
            db.SeriesItems.Add(sub);
        }
        sub.Name     = series.Title ?? "";
        sub.Title    = series.Title ?? "";
        sub.Slug     = WorldGraphService.Slugify(series.Title ?? "");
        sub.Description = series.Premise ?? "";

        var rec = db.Records.FirstOrDefault(r => r.EntityId == id);
        var json = JsonSerializer.Serialize(series, JsonOpts);
        if (rec == null) db.Records.Add(new RecordRow { EntityId = id, Json = json, UpdatedAt = DateTime.UtcNow });
        else { rec.Json = json; rec.UpdatedAt = DateTime.UtcNow; }

        db.SaveChanges();
    }

    public void DeleteSeries(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var guid = ParseGuid(id);
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.FirstOrDefault(e => e.Id == guid);
        if (entity == null) return;
        entity.IsActive   = false;
        entity.Status     = "archived";
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;
        db.SaveChanges();
    }

    private static Guid ParseGuid(string s)
    {
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }
}
