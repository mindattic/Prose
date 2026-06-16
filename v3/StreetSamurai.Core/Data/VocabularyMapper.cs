using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column schema (Vocabulary) and the domain model
/// (VocabularyData). Vocabulary is a flat type — no list fields beyond .tags,
/// which live in the universal EntityTags layer.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class VocabularyMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Term, Category, Tags only.
    /// </summary>
    public static List<VocabularyData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.VocabularyEntries.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "vocabulary"),
                v => v.Id, e => e.Id,
                (v, e) => new { v.Id, Name = e.Name, v.Category, v.Tier })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<VocabularyData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new VocabularyData
            {
                Id       = r.Id.ToString("N"),
                Term     = r.Name ?? "",
                Category = r.Category ?? "",
                Tier     = r.Tier ?? "",
                Tags     = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>Full load of every active Vocabulary row, projected to VocabularyData.</summary>
    public static List<VocabularyData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "vocabulary")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "vocabulary" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var entries = db.VocabularyEntries.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
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

        var result = new List<VocabularyData>(entries.Count);
        foreach (var v in entries)
        {
            entityById.TryGetValue(v.Id, out var entity);
            tagsByEntity.TryGetValue(v.Id, out var tags);
            result.Add(Materialize(v, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single VocabularyEntry by id. Returns null when not found.</summary>
    public static VocabularyData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var v = db.VocabularyEntries.AsNoTracking().FirstOrDefault(x => x.Id == id);
        if (v == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(v, entity, tags);
    }

    /// <summary>Build a VocabularyData from the entity row.</summary>
    public static VocabularyData Materialize(Vocabulary v, Entity? entity, List<string>? tags)
    {
        return new VocabularyData
        {
            Id         = v.Id.ToString("N"),
            Term       = v.Term.Length > 0 ? v.Term : (entity?.Name ?? v.Name),
            Definition = v.Definition,
            Origin     = v.Origin,
            Usage      = v.Usage,
            Tier       = v.Tier,
            Category   = v.Category,
            Example    = v.Example,
            Tags       = tags ?? new List<string>(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a VocabularyData into the relational schema.
    /// Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, VocabularyData src, CancellationToken ct = default)
    {
        var entry = await db.VocabularyEntries.FirstOrDefaultAsync(v => v.Id == id, ct);
        var isNew = entry == null;

        if (isNew)
        {
            entry = new Vocabulary { Id = id };
            db.VocabularyEntries.Add(entry);
        }

        FillScalars(entry!, src);
    }

    /// <summary>Populate scalar columns on Vocabulary from src (no DB touch).</summary>
    public static void FillScalars(Vocabulary v, VocabularyData src)
    {
        v.Name       = src.Term ?? "";
        v.Term       = src.Term ?? "";
        v.Definition = src.Definition ?? "";
        v.Origin     = src.Origin ?? "";
        v.Usage      = src.Usage ?? "";
        v.Tier       = src.Tier ?? "";
        v.Category   = src.Category ?? "";
        v.Example    = src.Example ?? "";
        // Domain is a UI/classification column not on VocabularyData; leave unchanged on update.
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active vocabulary Entity, deserialize its Records.Json
    /// blob → VocabularyData → persist. Returns the number of entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-vocabulary-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var vocabEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "vocabulary" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (vocabEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => vocabEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            VocabularyData? src;
            try { src = JsonSerializer.Deserialize<VocabularyData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "VocabularyMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "VocabularyMapper.RebuildAllAsync: failed to persist vocabulary {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }
}
