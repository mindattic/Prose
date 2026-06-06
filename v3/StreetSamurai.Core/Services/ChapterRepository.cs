using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using Chapter = StreetSamurai.Core.Models.Chapter;
using ChapterEntity = StreetSamurai.Core.Data.Entities.Chapter;
using ChapterBeatEntity = StreetSamurai.Core.Data.Entities.ChapterBeat;
using EntityRow = StreetSamurai.Core.Data.Entities.Entity;
using RecordRow = StreetSamurai.Core.Data.Entities.Record;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// EF-backed Chapter repository. Chapters live in the unified StreetSamurai
/// database with a typed <see cref="StreetSamurai.Core.Data.Entities.Chapter"/>
/// row plus child <see cref="StreetSamurai.Core.Data.Entities.ChapterBeat"/>
/// rows for queryable beat scans, and a canonical
/// <see cref="StreetSamurai.Core.Data.Entities.Record"/> JSON blob that round-trips
/// the full <see cref="Models.Chapter"/> domain object.
/// </summary>
public class ChapterRepository : IChapterRepository
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<ChapterRepository> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ChapterRepository(IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<ChapterRepository> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public event Action<Chapter>? OnChapterSaved;

    /// <summary>Test-fixture ctor — wraps a SQLite-in-memory DbContextFactory keyed by path.</summary>
    public ChapterRepository(IPathProvider paths, ILogger<ChapterRepository> log)
    {
        this.dbFactory = TestDbFactory.For(paths, "chapter");
        this.log = log;
    }

    public List<Chapter> ListChapters()
    {
        using var db = dbFactory.CreateDbContext();
        var jsons = db.Records
            .AsNoTracking()
            .Where(r => r.Entity!.EntityType == "chapter" && r.Entity.IsActive)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.Json)
            .ToList();
        var list = new List<Chapter>(jsons.Count);
        foreach (var j in jsons)
        {
            try
            {
                var c = JsonSerializer.Deserialize<Chapter>(j, JsonOpts);
                if (c != null) list.Add(c);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize a chapter record"); }
        }
        return list;
    }

    public Chapter? LoadChapter(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var guid = ParseGuid(id);
        using var db = dbFactory.CreateDbContext();
        var json = db.Records.AsNoTracking()
            .Where(r => r.EntityId == guid)
            .Select(r => r.Json)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Chapter>(json, JsonOpts); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize Chapter {Id}", id); return null; }
    }

    public void SaveChapter(Chapter chapter)
    {
        if (string.IsNullOrEmpty(chapter.Id)) chapter.Id = Guid.CreateVersion7().ToString("N");
        var id = ParseGuid(chapter.Id);
        chapter.Modified = DateTime.UtcNow;

        using var db = dbFactory.CreateDbContext();

        var entity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (entity == null)
        {
            entity = new EntityRow
            {
                Id          = id,
                EntityType  = "chapter",
                Name        = chapter.Title,
                Slug        = WorldGraphService.Slugify(chapter.Title),
                Status      = string.IsNullOrEmpty(chapter.Status) ? "draft" : chapter.Status,
                Description = chapter.Synopsis,
                CreatedAt   = chapter.Created == default ? DateTime.UtcNow : chapter.Created,
                ModifiedAt  = DateTime.UtcNow,
                IsActive    = true,
            };
            db.Entities.Add(entity);
        }
        else
        {
            entity.Name        = chapter.Title;
            entity.Slug        = WorldGraphService.Slugify(chapter.Title);
            entity.Description = chapter.Synopsis;
            entity.ModifiedAt  = DateTime.UtcNow;
            entity.IsActive    = true;
            entity.ArchivedAt  = null;
            entity.Status      = string.IsNullOrEmpty(chapter.Status) ? entity.Status : chapter.Status;
        }

        var sub = db.Chapters.Include(c => c.Beats).FirstOrDefault(c => c.Id == id);
        if (sub == null)
        {
            sub = new ChapterEntity { Id = id };
            db.Chapters.Add(sub);
        }
        sub.BookId         = string.IsNullOrEmpty(chapter.BookId) ? null : ParseGuid(chapter.BookId);
        sub.Number         = chapter.Number;
        sub.Title          = chapter.Title;
        sub.Synopsis       = chapter.Synopsis ?? "";
        sub.Status         = chapter.Status ?? "draft";
        sub.Html           = chapter.Html ?? "";
        sub.ModifiedAt     = DateTime.UtcNow;

        // Replace ChapterCharacters bridge — no JSON column.
        db.ChapterCharacters.RemoveRange(db.ChapterCharacters.Where(r => r.ChapterId == id));
        for (int i = 0; i < chapter.Characters.Count; i++)
        {
            var alias = chapter.Characters[i] ?? "";
            db.ChapterCharacters.Add(new ChapterCharacter
            {
                ChapterId = id, Position = i, Alias = alias,
                CharacterId = ResolveCharacterIdByName(db, alias),
            });
        }

        sub.Beats.Clear();
        foreach (var beat in chapter.Beats.OrderBy(b => b.Index))
            sub.Beats.Add(new ChapterBeatEntity
            {
                BeatGuid       = ParseGuid(beat.Id),
                ChapterId      = id,
                Index          = beat.Index,
                Title          = beat.Title ?? "",
                Synopsis       = beat.Synopsis ?? "",
                Text           = beat.Text ?? "",
                Act            = beat.Act,
                StructureRole  = beat.StructureRole ?? "",
                SceneType      = beat.SceneType ?? "scene",
            });

        var rec = db.Records.FirstOrDefault(r => r.EntityId == id);
        var json = JsonSerializer.Serialize(chapter, JsonOpts);
        if (rec == null) db.Records.Add(new RecordRow { EntityId = id, Json = json, UpdatedAt = DateTime.UtcNow });
        else { rec.Json = json; rec.UpdatedAt = DateTime.UtcNow; }

        db.SaveChanges();

        try { OnChapterSaved?.Invoke(chapter); }
        catch (Exception ex) { log.LogWarning(ex, "OnChapterSaved subscriber threw for chapter {Id}", chapter.Id); }
    }

    public void DeleteChapter(string id)
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

    private static Guid? ResolveCharacterIdByName(StreetSamuraiDbContext db, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == "character" && e.IsActive
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    private static Guid ParseGuid(string s)
    {
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        // Deterministic hash for non-GUID strings — same input always maps to the
        // same Guid so save/load round-trip works for short/legacy ids.
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }
}
