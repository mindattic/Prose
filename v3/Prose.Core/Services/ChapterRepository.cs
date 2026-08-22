using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Chapter = Prose.Core.Models.Chapter;
using ChapterEntity = Prose.Core.Data.Entities.Chapter;
using ChapterBeatEntity = Prose.Core.Data.Entities.ChapterBeat;
using EntityRow = Prose.Core.Data.Entities.Entity;
using RecordRow = Prose.Core.Data.Entities.Record;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// EF-backed Chapter repository. Chapters live in the unified Prose
/// database with a typed <see cref="Prose.Core.Data.Entities.Chapter"/>
/// row plus child <see cref="Prose.Core.Data.Entities.ChapterBeat"/>
/// rows for queryable beat scans, and a canonical
/// <see cref="Prose.Core.Data.Entities.Record"/> JSON blob that round-trips
/// the full <see cref="Models.Chapter"/> domain object.
///
/// No status flag (temporal-hygiene rule): a chapter's existence in the live
/// <c>Chapters</c>/<c>Entities</c> tables IS the fact of it being current.
/// <see cref="DeleteChapter"/> hard-deletes both rows in one transaction — <c>Chapters</c>
/// has no database FK to <c>Entities</c> (unlike every typed subtype), so both must be
/// deleted explicitly or the <c>Chapters</c>/<c>ChapterCharacters</c>/<c>ChapterBeats</c>
/// rows would silently orphan. Recoverable via <c>Entities_History</c> (system-versioned).
/// </summary>
public class ChapterRepository : IChapterRepository
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<ChapterRepository> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ChapterRepository(IDbContextFactory<ProseDbContext> dbFactory, ILogger<ChapterRepository> log)
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
            .Where(r => r.Entity!.EntityType == "chapter")
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
                Slug        = UniverseGraphService.Slugify(chapter.Title),
                Status      = string.IsNullOrEmpty(chapter.Status) ? "draft" : chapter.Status,
                Description = chapter.Synopsis,
                CreatedAt   = chapter.Created == default ? DateTime.UtcNow : chapter.Created,
                ModifiedAt  = DateTime.UtcNow,
            };
            db.Entities.Add(entity);
        }
        else
        {
            entity.Name        = chapter.Title;
            entity.Slug        = UniverseGraphService.Slugify(chapter.Title);
            entity.Description = chapter.Synopsis;
            entity.ModifiedAt  = DateTime.UtcNow;
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

        if (chapter.Beats.Count > 0) sub.Beats.Clear();
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

    /// <summary>
    /// Hard-deletes the chapter — no status flag; existence in the live table is the only
    /// signal of "current." <c>Chapters</c> has no database FK to <c>Entities</c> (unlike every
    /// typed subtype), so the <c>Chapters</c> row is deleted explicitly first (its own declared
    /// Cascade FKs take <c>ChapterCharacters</c>/<c>ChapterBeats</c> with it), then the
    /// <c>Entities</c> row — in one transaction. Recoverable via <c>Entities_History</c>
    /// (system-versioned) or <c>prose --restore-entity</c>.
    /// </summary>
    public void DeleteChapter(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var guid = ParseGuid(id);
        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();

        var entity = db.Entities.FirstOrDefault(e => e.Id == guid && e.EntityType == "chapter");
        if (entity == null) return;

        var chapter = db.Chapters.FirstOrDefault(c => c.Id == guid);
        if (chapter != null) db.Chapters.Remove(chapter);
        db.Entities.Remove(entity);

        db.SaveChanges();
        tx.Commit();
    }

    private static Guid? ResolveCharacterIdByName(ProseDbContext db, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = UniverseGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == "character"
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
