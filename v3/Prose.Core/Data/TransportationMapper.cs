using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Transportation + 2 child
/// tables) and the domain model (TransportationData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — TransportationRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Transportation-level tags live in the
/// universal EntityTags layer.
///
/// All TransportationData fields are fully covered by scalar columns.
/// No fields remain blob-only.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class TransportationMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Manufacturer,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<TransportationData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Transportations.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "transportation"),
                t => t.Id, e => e.Id,
                (t, e) => new { t.Id, Name = e.Name, t.Category, t.Manufacturer, t.Rating, t.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<TransportationData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new TransportationData
            {
                Id           = r.Id.ToString("N"),
                Type         = "transportation",
                Name         = r.Name ?? "",
                Category     = r.Category ?? "",
                Manufacturer = r.Manufacturer ?? "",
                Rating       = r.Rating,
                VoteCount    = r.VoteCount,
                Tags         = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Transportation row + all child collections,
    /// then project to TransportationData. Records.Json is never read here.
    /// </summary>
    public static List<TransportationData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "transportation")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "transportation" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var transportations = BuildIncludeChain(db.Transportations.AsNoTracking())
            .Where(t => ids.Contains(t.Id))
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

        var result = new List<TransportationData>(transportations.Count);
        foreach (var t in transportations)
        {
            entityById.TryGetValue(t.Id, out var entity);
            tagsByEntity.TryGetValue(t.Id, out var tags);
            result.Add(Materialize(t, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Transportation by id. Returns null when not found.</summary>
    public static TransportationData? LoadOne(ProseDbContext db, Guid id)
    {
        var t = BuildIncludeChain(db.Transportations.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (t == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(x => x.EntityId == id)
            .Select(x => x.Tag!.Name)
            .ToList();
        return Materialize(t, entity, tags);
    }

    private static IQueryable<Transportation> BuildIncludeChain(IQueryable<Transportation> q)
        => q.AsSplitQuery()
            .Include(t => t.Aliases)
            .Include(t => t.StoryHooks);

    /// <summary>
    /// Build a TransportationData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name. All TransportationData fields are
    /// covered by scalar columns — nothing remains blob-only.
    /// </summary>
    public static TransportationData Materialize(Transportation t, Entity? entity, List<string>? tags)
    {
        var data = new TransportationData
        {
            Id               = t.Id.ToString("N"),
            Type             = "transportation",
            Name             = entity?.Name ?? t.Name,
            Manufacturer     = t.Manufacturer,
            Category         = t.Category,
            Propulsion       = t.Propulsion,
            Speed            = t.Speed,
            Capacity         = t.Capacity,
            Range            = t.Range,
            TierAvailability = t.TierAvailability,
            Cost             = t.Cost,
            Autonomy         = t.Autonomy,
            Armament         = t.Armament,
            CommonUsage      = t.CommonUsage,
            Description      = t.Description,
            Rating           = t.Rating,
            VoteCount        = t.VoteCount,
            MidjourneyPrompt = t.MidjourneyPrompt,
            Dalle3Prompt     = t.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases    = t.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.StoryHooks = t.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a TransportationData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, TransportationData src, CancellationToken ct = default)
    {
        var transport = await db.Transportations.FirstOrDefaultAsync(t => t.Id == id, ct);
        var isNew = transport == null;

        if (!isNew)
        {
            await db.TransportationAliases.Where(x => x.TransportationId == id).ExecuteDeleteAsync(ct);
            await db.TransportationStoryHooks.Where(x => x.TransportationId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            transport = new Transportation { Id = id };
            db.Transportations.Add(transport);
        }

        FillScalars(transport!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Transportation from src (no DB touch).</summary>
    public static void FillScalars(Transportation t, TransportationData src)
    {
        t.Name             = src.Name ?? "";
        t.Manufacturer     = src.Manufacturer ?? "";
        t.Category         = src.Category ?? "";
        t.Propulsion       = src.Propulsion ?? "";
        t.Speed            = src.Speed ?? "";
        t.Capacity         = src.Capacity ?? "";
        t.Range            = src.Range ?? "";
        t.TierAvailability = src.TierAvailability ?? "";
        t.Cost             = src.Cost ?? "";
        t.Autonomy         = src.Autonomy ?? "";
        t.Armament         = src.Armament ?? "";
        t.CommonUsage      = src.CommonUsage ?? "";
        t.Description      = src.Description ?? "";
        t.Rating           = src.Rating;
        t.VoteCount        = src.VoteCount;
        t.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        t.Dalle3Prompt     = src.Dalle3Prompt ?? "";
        // Tier is a classification column that TransportationData has no direct
        // field for; leave unchanged on update.
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, TransportationData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.TransportationAliases.Add(new TransportationAlias { TransportationId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.TransportationStoryHooks.Add(new TransportationStoryHook { TransportationId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active transportation Entity, deserialize its Records.Json
    /// blob → TransportationData → persist. Returns the number of transportations written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-transportation-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var transportEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "transportation" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (transportEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => transportEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            TransportationData? src;
            try { src = JsonSerializer.Deserialize<TransportationData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "TransportationMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "TransportationMapper.RebuildAllAsync: failed to persist transportation {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }
}
