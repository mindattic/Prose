using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Motifs + MotifAppearances)
/// and the domain model (MotifData). This is the *only* place that knows the
/// column ↔ JSON-field correspondence — MotifRepository delegates to it so the
/// mapping never drifts between import and read/write paths.
///
/// MotifData (IWorldRecord) has no tags, no aliases — only Name, Description,
/// and Appearances (list of scene+meaning pairs).
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class MotifMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Description only.
    /// No bridge materialization.
    /// </summary>
    public static List<MotifData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Motifs.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "motif"),
                m => m.Id, e => e.Id,
                (m, e) => new { m.Id, Name = e.Name, m.Description })
            .ToList();

        return rows.Select(r => new MotifData
        {
            Id          = r.Id.ToString("N"),
            Name        = r.Name ?? "",
            Description = r.Description ?? "",
        }).ToList();
    }

    /// <summary>
    /// Full eager load of every active Motif row + MotifAppearances,
    /// then project to MotifData. Records.Json is never read here.
    /// </summary>
    public static List<MotifData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "motif")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "motif"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var motifs = BuildIncludeChain(db.Motifs.AsNoTracking())
            .Where(m => ids.Contains(m.Id))
            .ToList();

        var entityById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionary(e => e.Id, e => e);

        var result = new List<MotifData>(motifs.Count);
        foreach (var m in motifs)
        {
            entityById.TryGetValue(m.Id, out var entity);
            result.Add(Materialize(m, entity));
        }
        return result;
    }

    /// <summary>Load a single Motif by id. Returns null when not found.</summary>
    public static MotifData? LoadOne(ProseDbContext db, Guid id)
    {
        var m = BuildIncludeChain(db.Motifs.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (m == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        return Materialize(m, entity);
    }

    private static IQueryable<Motif> BuildIncludeChain(IQueryable<Motif> q)
        => q.AsSplitQuery()
            .Include(m => m.Appearances);

    /// <summary>
    /// Build a MotifData from the EF entity + Appearances loaded by BuildIncludeChain.
    /// Entity is used for the universal Name.
    /// </summary>
    public static MotifData Materialize(Motif m, Entity? entity)
    {
        var data = new MotifData
        {
            Id          = m.Id.ToString("N"),
            Name        = entity?.Name ?? m.Name,
            Description = m.Description,
        };

        data.Appearances = m.Appearances.OrderBy(x => x.Position).Select(x => new MotifAppearanceData
        {
            Scene   = x.Scene,
            Meaning = x.Meaning,
        }).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a MotifData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, MotifData src, CancellationToken ct = default)
    {
        var motif = await db.Motifs.FirstOrDefaultAsync(m => m.Id == id, ct);
        var isNew = motif == null;

        if (!isNew)
        {
            await db.MotifAppearances.Where(x => x.MotifId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            motif = new Motif { Id = id };
            db.Motifs.Add(motif);
        }

        FillScalars(motif!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Motif from src (no DB touch).</summary>
    public static void FillScalars(Motif m, MotifData src)
    {
        m.Name        = src.Name ?? "";
        m.Description = src.Description ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, MotifData src)
    {
        for (int i = 0; i < src.Appearances.Count; i++)
        {
            db.MotifAppearances.Add(new MotifAppearance
            {
                MotifId  = id,
                Position = i,
                Scene    = src.Appearances[i].Scene,
                Meaning  = src.Appearances[i].Meaning ?? "",
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active motif Entity, deserialize its Records.Json
    /// blob → MotifData → persist. Also creates a minimal relational row for
    /// any active motif entity that has no blob and no relational row yet.
    /// Returns the number of motif entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-motif-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var motifEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "motif")
            .Select(e => e.Id)
            .ToHashSet();

        if (motifEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => motifEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        // Backfill from blobs
        foreach (var row in blobRows)
        {
            MotifData? src;
            try { src = JsonSerializer.Deserialize<MotifData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "MotifMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
                continue;
            }
            if (src == null) continue;

            try
            {
                await PersistAsync(db, row.EntityId, src, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "MotifMapper.RebuildAllAsync: failed to persist motif {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free entities that have no relational row yet
        var existingRelationalIds = db.Motifs.AsNoTracking()
            .Where(m => motifEntityIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToHashSet();

        foreach (var entityId in motifEntityIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new MotifData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "MotifMapper.RebuildAllAsync: failed to persist minimal row for motif {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }
}
