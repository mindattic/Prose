using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Documents + 1 child
/// table) and the domain model (WorldbuildingDocument).
///
/// Bridges: DocumentHeadings (Headings list).
/// Tags route through the shared EntityTags table.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class DocumentMapper
{
    // ─────────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name/FileName, Category,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<WorldbuildingDocument> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Documents.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "document"),
                d => d.Id, e => e.Id,
                (d, e) => new { d.Id, d.FileName, d.Title, d.Category, d.Rating, d.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<WorldbuildingDocument>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new WorldbuildingDocument
            {
                Id       = r.Id.ToString("N"),
                FileName = r.FileName ?? "",
                Title    = r.Title ?? "",
                Category = r.Category ?? "",
                Rating   = r.Rating,
                VoteCount = r.VoteCount,
                Tags     = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Document row + all child collections.
    /// Records.Json is never read here.
    /// </summary>
    public static List<WorldbuildingDocument> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "document")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "document" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var docs = BuildIncludeChain(db.Documents.AsNoTracking())
            .Where(d => ids.Contains(d.Id))
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

        var result = new List<WorldbuildingDocument>(docs.Count);
        foreach (var d in docs)
        {
            entityById.TryGetValue(d.Id, out var entity);
            tagsByEntity.TryGetValue(d.Id, out var tags);
            result.Add(Materialize(d, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Document by id. Returns null when not found.</summary>
    public static WorldbuildingDocument? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var d = BuildIncludeChain(db.Documents.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (d == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(d, entity, tags);
    }

    private static IQueryable<Document> BuildIncludeChain(IQueryable<Document> q)
        => q.AsSplitQuery()
            .Include(d => d.Headings);

    /// <summary>Build a WorldbuildingDocument from the EF entity + bridges.</summary>
    public static WorldbuildingDocument Materialize(Document d, Entity? entity, List<string>? tags)
    {
        return new WorldbuildingDocument
        {
            Id               = d.Id.ToString("N"),
            FileName         = d.FileName,
            Title            = d.Title,
            Category         = d.Category,
            Body             = d.Body,
            LineCount        = d.LineCount,
            Rating           = d.Rating,
            VoteCount        = d.VoteCount,
            MidjourneyPrompt = d.MidjourneyPrompt,
            Dalle3Prompt     = d.Dalle3Prompt,
            Headings         = d.Headings.OrderBy(h => h.Position).Select(h => h.HeadingText).ToList(),
            Tags             = tags ?? new List<string>(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a WorldbuildingDocument into the relational schema. Existing bridge
    /// rows are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, WorldbuildingDocument src, CancellationToken ct = default)
    {
        var d = await db.Documents.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = d == null;

        if (!isNew)
        {
            await db.DocumentHeadings.Where(x => x.DocumentId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            d = new Document { Id = id };
            db.Documents.Add(d);
        }

        FillScalars(d!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Document from src (no DB touch).</summary>
    public static void FillScalars(Document d, WorldbuildingDocument src)
    {
        d.Name             = src.FileName ?? src.Title ?? "";  // Entity.Name mirrors FileName (per repo)
        d.FileName         = src.FileName ?? "";
        d.Title            = src.Title ?? "";
        d.Category         = src.Category ?? "";
        d.Body             = src.Body ?? "";
        d.LineCount        = src.LineCount;
        d.Rating           = src.Rating;
        d.VoteCount        = src.VoteCount;
        d.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        d.Dalle3Prompt     = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, WorldbuildingDocument src)
    {
        for (int i = 0; i < src.Headings.Count; i++)
            db.DocumentHeadings.Add(new DocumentHeading { DocumentId = id, Position = i, HeadingText = src.Headings[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every document Entity (active or inactive), deserialize
    /// its Records.Json blob → WorldbuildingDocument → persist. Also creates a
    /// minimal relational row for any active document entity with no blob and no
    /// relational row yet. Returns the number of document entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-document-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var entityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "document")
            .Select(e => new { e.Id, e.Name, e.IsActive })
            .ToList();

        if (entityIds.Count == 0) return 0;

        var idSet = entityIds.Select(e => e.Id).ToHashSet();

        var blobRows = db.Records.AsNoTracking()
            .Where(r => idSet.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        var existingRelational = db.Documents.AsNoTracking()
            .Where(d => idSet.Contains(d.Id))
            .Select(d => d.Id)
            .ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            WorldbuildingDocument? src;
            try { src = JsonSerializer.Deserialize<WorldbuildingDocument>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "DocumentMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "DocumentMapper.RebuildAllAsync: failed to persist entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for active entities with no blob and no relational row
        foreach (var e in entityIds.Where(e => e.IsActive && !blobEntityIds.Contains(e.Id) && !existingRelational.Contains(e.Id)))
        {
            try
            {
                var stub = new WorldbuildingDocument { Id = e.Id.ToString("N"), FileName = e.Name ?? "", Title = e.Name ?? "" };
                await PersistAsync(db, e.Id, stub, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "DocumentMapper.RebuildAllAsync: failed to create stub for entity {Id}", e.Id);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }
}
