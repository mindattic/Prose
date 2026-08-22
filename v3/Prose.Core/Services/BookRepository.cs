using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Book = Prose.Core.Models.Book;
using BookEntity = Prose.Core.Data.Entities.Book;
using EntityRow = Prose.Core.Data.Entities.Entity;
using RecordRow = Prose.Core.Data.Entities.Record;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// EF-backed Book repository. Books live in the unified Prose database
/// alongside every other entity:
///   • <see cref="Entity"/> — universal row (Name, Slug, Status, …)
///   • <see cref="Prose.Core.Data.Entities.Book"/> — strongly-typed indexed columns
///     (Title, Slug, SeriesId, Tagline, Premise, ArcTarget, ProtagonistsJson, ChapterIdsJson)
///   • <see cref="Prose.Core.Data.Entities.Record"/> — canonical JSON of the
///     <see cref="Models.Book"/> domain object, round-tripped on read/write.
///
/// Class name preserved so consumers don't change. The legacy file-based
/// implementation has been retired.
///
/// No status flag (temporal-hygiene rule): a book's existence in the live
/// <c>Books</c>/<c>Entities</c> tables IS the fact of it being current.
/// <see cref="ArchiveBook"/> hard-deletes both rows in one transaction — <c>Books</c>
/// has no database FK to <c>Entities</c> (unlike every typed subtype), so both must be
/// deleted explicitly or the <c>Books</c>/<c>BookProtagonists</c>/<c>BookChapterOrder</c>
/// rows would silently orphan. Recoverable via <c>Entities_History</c> (system-versioned).
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<BookRepository> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public BookRepository(IDbContextFactory<ProseDbContext> dbFactory, ILogger<BookRepository> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Test-fixture ctor — wraps a SQLite-in-memory DbContextFactory keyed by path.</summary>
    public BookRepository(IPathProvider paths, ILogger<BookRepository> log)
    {
        this.dbFactory = TestDbFactory.For(paths, "book");
        this.log = log;
    }

    public List<Book> ListBooks()
    {
        using var db = dbFactory.CreateDbContext();
        var jsons = db.Records
            .AsNoTracking()
            .Where(r => r.Entity!.EntityType == "book")
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.Json)
            .ToList();
        var list = new List<Book>(jsons.Count);
        foreach (var j in jsons)
        {
            try
            {
                var b = JsonSerializer.Deserialize<Book>(j, JsonOpts);
                if (b != null) list.Add(b);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize a book record"); }
        }
        return list;
    }

    public Book? LoadBook(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var guid = ParseGuid(id);
        using var db = dbFactory.CreateDbContext();
        var json = db.Records
            .AsNoTracking()
            .Where(r => r.EntityId == guid)
            .Select(r => r.Json)
            .FirstOrDefault();
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Book>(json, JsonOpts); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize Book {Id}", id); return null; }
    }

    public void SaveBook(Book book)
    {
        if (string.IsNullOrEmpty(book.Id)) book.Id = Guid.CreateVersion7().ToString("N");
        var id = ParseGuid(book.Id);
        book.Modified = DateTime.UtcNow;

        using var db = dbFactory.CreateDbContext();

        var entity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (entity == null)
        {
            entity = new EntityRow
            {
                Id          = id,
                EntityType  = "book",
                Name        = book.Title,
                Slug        = UniverseGraphService.Slugify(book.Title),
                Status      = string.IsNullOrEmpty(book.Status) ? "canon" : book.Status,
                Description = book.Premise,
                CreatedAt   = book.Created == default ? DateTime.UtcNow : book.Created,
                ModifiedAt  = DateTime.UtcNow,
            };
            db.Entities.Add(entity);
        }
        else
        {
            entity.Name        = book.Title;
            entity.Slug        = UniverseGraphService.Slugify(book.Title);
            entity.Description = book.Premise;
            entity.ModifiedAt  = DateTime.UtcNow;
        }

        var sub = db.Books.FirstOrDefault(b => b.Id == id);
        if (sub == null)
        {
            sub = new BookEntity { Id = id };
            db.Books.Add(sub);
        }
        sub.Title             = book.Title;
        sub.Slug              = UniverseGraphService.Slugify(book.Title);
        sub.SeriesId          = string.IsNullOrEmpty(book.SeriesId) ? null : ParseGuid(book.SeriesId);
        sub.Tagline           = book.Tagline ?? "";
        sub.Premise           = book.Premise ?? "";
        sub.ArcTarget         = book.ArcTarget ?? "";
        sub.ModifiedAt        = DateTime.UtcNow;

        // Replace bridge contents (BookProtagonists / BookChapterOrder) — no JSON columns.
        db.BookProtagonists.RemoveRange(db.BookProtagonists.Where(r => r.BookId == id));
        db.BookChapterOrder.RemoveRange(db.BookChapterOrder.Where(r => r.BookId == id));
        for (int i = 0; i < book.Protagonists.Count; i++)
        {
            var alias = book.Protagonists[i] ?? "";
            db.BookProtagonists.Add(new BookProtagonist
            {
                BookId = id, Position = i, Alias = alias,
                CharacterId = ResolveCharacterIdByName(db, alias),
            });
        }
        for (int i = 0; i < book.ChapterIds.Count; i++)
        {
            var raw = book.ChapterIds[i];
            if (string.IsNullOrEmpty(raw)) continue;
            var chId = ParseGuid(raw);
            if (db.Chapters.Any(c => c.Id == chId))
                db.BookChapterOrder.Add(new BookChapterOrder { BookId = id, Position = i, ChapterId = chId });
        }

        var rec = db.Records.FirstOrDefault(r => r.EntityId == id);
        var json = JsonSerializer.Serialize(book, JsonOpts);
        if (rec == null) db.Records.Add(new RecordRow { EntityId = id, Json = json, UpdatedAt = DateTime.UtcNow });
        else { rec.Json = json; rec.UpdatedAt = DateTime.UtcNow; }

        db.SaveChanges();
    }

    /// <summary>
    /// Hard-deletes the book — no status flag; existence in the live table is the only signal
    /// of "current." <c>Books</c> has no database FK to <c>Entities</c> (unlike every typed
    /// subtype), so the <c>Books</c> row is deleted explicitly first (its own declared Cascade
    /// FKs take <c>BookProtagonists</c>/<c>BookChapterOrder</c> with it), then the <c>Entities</c>
    /// row — in one transaction, so a failure partway never leaves one without the other.
    /// Chapters are NOT touched (ArchiveBook never archived chapters even under the old flag).
    /// Recoverable via <c>Entities_History</c> (system-versioned) or <c>prose --restore-entity</c>.
    /// </summary>
    public void ArchiveBook(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var guid = ParseGuid(id);
        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();

        var entity = db.Entities.FirstOrDefault(e => e.Id == guid && e.EntityType == "book");
        if (entity == null) return;

        var book = db.Books.FirstOrDefault(b => b.Id == guid);
        if (book != null) db.Books.Remove(book);
        db.Entities.Remove(entity);

        db.SaveChanges();
        tx.Commit();
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
}
