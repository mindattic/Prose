using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Subsidiaries +
/// SubsidiaryProducts bridge) and the domain model (SubsidiaryData).
///
/// Column mapping notes:
///   - SubsidiaryData.ParentCorponation (string) → Subsidiaries.ParentCorponationAlias (string)
///     + Subsidiaries.ParentCorponationId (FK, nullable, resolved from name).
///   - SubsidiaryData.KnownProducts (List&lt;string&gt;) → SubsidiaryProducts bridge
///     (ProductEntityId nullable FK + Alias string).
///   - SubsidiaryData has no TierAvailability; Subsidiary.Tier/Sector not in domain
///     model — kept blank on write.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class SubsidiaryMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Rating, VoteCount,
    /// Tags only. No bridge materialization.
    /// </summary>
    public static List<SubsidiaryData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Subsidiaries.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "subsidiary"),
                s => s.Id, e => e.Id,
                (s, e) => new { s.Id, Name = e.Name, s.Rating, s.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<SubsidiaryData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new SubsidiaryData
            {
                Id        = r.Id.ToString("N"),
                Type      = "subsidiary",
                Name      = r.Name ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Subsidiary row + all child collections,
    /// then project to SubsidiaryData. Records.Json is never read here.
    /// </summary>
    public static List<SubsidiaryData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "subsidiary")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "subsidiary" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var subsidiaries = BuildIncludeChain(db.Subsidiaries.AsNoTracking())
            .Where(s => ids.Contains(s.Id))
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

        var result = new List<SubsidiaryData>(subsidiaries.Count);
        foreach (var s in subsidiaries)
        {
            entityById.TryGetValue(s.Id, out var entity);
            tagsByEntity.TryGetValue(s.Id, out var tags);
            result.Add(Materialize(s, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Subsidiary by id. Returns null when not found.</summary>
    public static SubsidiaryData? LoadOne(ProseDbContext db, Guid id)
    {
        var s = BuildIncludeChain(db.Subsidiaries.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (s == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(s, entity, tags);
    }

    private static IQueryable<Subsidiary> BuildIncludeChain(IQueryable<Subsidiary> q)
        => q.AsSplitQuery()
            .Include(s => s.KnownProducts);

    /// <summary>
    /// Build a SubsidiaryData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// </summary>
    public static SubsidiaryData Materialize(Subsidiary s, Entity? entity, List<string>? tags)
    {
        var data = new SubsidiaryData
        {
            Id               = s.Id.ToString("N"),
            Type             = "subsidiary",
            Name             = entity?.Name ?? s.Name,
            ParentCorponation = s.ParentCorponationAlias,
            LineOfBusiness   = s.LineOfBusiness,
            Description      = s.Description,
            PublicFacing     = s.PublicFacing,
            Rating           = s.Rating,
            VoteCount        = s.VoteCount,
            MidjourneyPrompt = s.MidjourneyPrompt,
            Dalle3Prompt     = s.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.KnownProducts = s.KnownProducts.OrderBy(x => x.Position).Select(x => x.Alias).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a SubsidiaryData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, SubsidiaryData src, CancellationToken ct = default)
    {
        var sub = await db.Subsidiaries.FirstOrDefaultAsync(s => s.Id == id, ct);
        var isNew = sub == null;

        if (!isNew)
        {
            await db.SubsidiaryProducts.Where(x => x.SubsidiaryId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            sub = new Subsidiary { Id = id };
            db.Subsidiaries.Add(sub);
        }

        FillScalars(sub!, src, db);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Subsidiary from src (no DB touch).</summary>
    public static void FillScalars(Subsidiary s, SubsidiaryData src, ProseDbContext db)
    {
        s.Name                    = src.Name ?? "";
        s.Sector                  = "";                          // not in SubsidiaryData
        s.Tier                    = "";                          // not in SubsidiaryData
        s.ParentCorponationAlias  = src.ParentCorponation ?? "";
        s.ParentCorponationId     = ResolveEntityId(db, "corponation", src.ParentCorponation ?? "");
        s.LineOfBusiness          = src.LineOfBusiness ?? "";
        s.Description             = src.Description ?? "";
        s.PublicFacing            = src.PublicFacing;
        s.Rating                  = src.Rating;
        s.VoteCount               = src.VoteCount;
        s.MidjourneyPrompt        = src.MidjourneyPrompt ?? "";
        s.Dalle3Prompt            = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, SubsidiaryData src)
    {
        for (int i = 0; i < src.KnownProducts.Count; i++)
        {
            var productId = ResolveEntityId(db, src.KnownProducts[i]);
            db.SubsidiaryProducts.Add(new SubsidiaryProduct
            {
                SubsidiaryId    = id,
                Position        = i,
                Alias           = src.KnownProducts[i] ?? "",
                ProductEntityId = productId,
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every subsidiary Entity (active or inactive), deserialize
    /// its Records.Json blob → SubsidiaryData → persist. Also creates a minimal
    /// relational row for any active subsidiary entity that has no blob and no
    /// relational row yet. Returns the number of subsidiary entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-subsidiary-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var subEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "subsidiary")
            .Select(e => e.Id)
            .ToHashSet();

        if (subEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => subEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            SubsidiaryData? src;
            try { src = JsonSerializer.Deserialize<SubsidiaryData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "SubsidiaryMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "SubsidiaryMapper.RebuildAllAsync: failed to persist subsidiary {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities.
        var activeSubIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "subsidiary" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.Subsidiaries.AsNoTracking()
            .Where(s => activeSubIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToHashSet();

        foreach (var entityId in activeSubIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new SubsidiaryData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "SubsidiaryMapper.RebuildAllAsync: failed to persist minimal row for subsidiary {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Resolve entity id for a specific type (e.g. corponation parent).</summary>
    private static Guid? ResolveEntityId(ProseDbContext db, string entityType, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && e.IsActive && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    /// <summary>Resolve entity id across all types (for KnownProducts which can be any product entity).</summary>
    private static Guid? ResolveEntityId(ProseDbContext db, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.IsActive && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }
}
