using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column schema (Quote) and the domain model
/// (QuoteData). Quote is a flat type — no list fields beyond .tags, which live
/// in the universal EntityTags layer.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class QuoteMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Tags only.
    /// </summary>
    public static List<QuoteData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Quotes.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "quote"),
                q => q.Id, e => e.Id,
                (q, e) => new { q.Id, Name = e.Name, q.Category })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<QuoteData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new QuoteData
            {
                Id       = r.Id.ToString("N"),
                Quote    = r.Name ?? "",
                Category = r.Category ?? "",
                Tags     = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full load of every active Quote row, projected to QuoteData.
    /// </summary>
    public static List<QuoteData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "quote")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "quote" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var quotes = db.Quotes.AsNoTracking()
            .Where(q => ids.Contains(q.Id))
            .ToList();

        var entityById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionary(e => e.Id, e => e);

        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<QuoteData>(quotes.Count);
        foreach (var q in quotes)
        {
            entityById.TryGetValue(q.Id, out var entity);
            tagsByEntity.TryGetValue(q.Id, out var tags);
            result.Add(Materialize(q, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Quote by id. Returns null when not found.</summary>
    public static QuoteData? LoadOne(ProseDbContext db, Guid id)
    {
        var q = db.Quotes.AsNoTracking().FirstOrDefault(x => x.Id == id);
        if (q == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(q, entity, tags);
    }

    /// <summary>Build a QuoteData from the entity row.</summary>
    public static QuoteData Materialize(Quote q, Entity? entity, List<string>? tags)
    {
        return new QuoteData
        {
            Id          = q.Id.ToString("N"),
            Quote       = q.QuoteText.Length > 0 ? q.QuoteText : (entity?.Name ?? q.Name),
            Attribution = q.Attribution,
            Source      = q.Source,
            Context     = q.Context,
            Category    = q.Category,
            InWorld     = q.InWorld,
            Tags        = tags ?? new List<string>(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a QuoteData into the relational schema.
    /// Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, QuoteData src, CancellationToken ct = default)
    {
        var quote = await db.Quotes.FirstOrDefaultAsync(q => q.Id == id, ct);
        var isNew = quote == null;

        if (isNew)
        {
            quote = new Quote { Id = id };
            db.Quotes.Add(quote);
        }

        FillScalars(quote!, src);
    }

    /// <summary>Populate scalar columns on Quote from src (no DB touch).</summary>
    public static void FillScalars(Quote q, QuoteData src)
    {
        q.Name        = src.Quote.Length > 40 ? src.Quote[..40] : src.Quote;
        q.QuoteText   = src.Quote ?? "";
        q.Attribution = src.Attribution ?? "";
        q.Source      = src.Source ?? "";
        q.Context     = src.Context ?? "";
        q.Category    = src.Category ?? "";
        q.InWorld     = src.InWorld;
        // Theme is a UI/classification column not on QuoteData; leave unchanged.
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active quote Entity, deserialize its Records.Json
    /// blob → QuoteData → persist via FillScalars + sync EntityTags.
    /// Returns the number of quotes written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-quote-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var quoteEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "quote" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (quoteEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => quoteEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            QuoteData? src;
            try { src = JsonSerializer.Deserialize<QuoteData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "QuoteMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
                continue;
            }
            if (src == null) continue;

            try
            {
                await PersistAsync(db, row.EntityId, src, ct);
                FactionMapper.SyncTagsForEntity(db, row.EntityId, src.Tags);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "QuoteMapper.RebuildAllAsync: failed to persist quote {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }
}
