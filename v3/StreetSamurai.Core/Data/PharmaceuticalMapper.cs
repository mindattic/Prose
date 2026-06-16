using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Pharmaceuticals + 4 child
/// tables) and the domain model (PharmaceuticalData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// EF class names: PharmAlias, PharmEffect, PharmSideEffect, PharmStoryHook.
/// DbSet names: PharmaceuticalAliases, PharmaceuticalEffects,
///              PharmaceuticalSideEffects, PharmaceuticalStoryHooks.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class PharmaceuticalMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<PharmaceuticalData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Pharmaceuticals.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "pharmaceutical"),
                p => p.Id, e => e.Id,
                (p, e) => new { p.Id, Name = e.Name, p.Category, p.Rating, p.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<PharmaceuticalData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new PharmaceuticalData
            {
                Id        = r.Id.ToString("N"),
                Type      = "pharmaceutical",
                Name      = r.Name ?? "",
                Category  = r.Category ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Pharmaceutical row + all child collections,
    /// then project to PharmaceuticalData. Records.Json is never read here.
    /// </summary>
    public static List<PharmaceuticalData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "pharmaceutical")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "pharmaceutical" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var pharmas = BuildIncludeChain(db.Pharmaceuticals.AsNoTracking())
            .Where(p => ids.Contains(p.Id))
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

        var result = new List<PharmaceuticalData>(pharmas.Count);
        foreach (var p in pharmas)
        {
            entityById.TryGetValue(p.Id, out var entity);
            tagsByEntity.TryGetValue(p.Id, out var tags);
            result.Add(Materialize(p, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Pharmaceutical by id. Returns null when not found.</summary>
    public static PharmaceuticalData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var p = BuildIncludeChain(db.Pharmaceuticals.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (p == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(p, entity, tags);
    }

    private static IQueryable<Pharmaceutical> BuildIncludeChain(IQueryable<Pharmaceutical> q)
        => q.AsSplitQuery()
            .Include(p => p.Aliases)
            .Include(p => p.Effects)
            .Include(p => p.SideEffects)
            .Include(p => p.StoryHooks);

    /// <summary>
    /// Build a PharmaceuticalData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// Note: Pharmaceutical.Tier maps to domain TierAvailability.
    /// </summary>
    public static PharmaceuticalData Materialize(Pharmaceutical p, Entity? entity, List<string>? tags)
    {
        var data = new PharmaceuticalData
        {
            Id               = p.Id.ToString("N"),
            Type             = "pharmaceutical",
            Name             = entity?.Name ?? p.Name,
            Manufacturer     = p.Manufacturer,
            Category         = p.Category,
            Subcategory      = p.Subcategory,
            TierAvailability = p.Tier,
            Legality         = p.Legality,
            Description      = p.Description,
            MethodOfUse      = p.MethodOfUse,
            Duration         = p.Duration,
            AddictionRisk    = p.AddictionRisk,
            StreetPrice      = p.StreetPrice,
            CulturalContext  = p.CulturalContext,
            Rating           = p.Rating,
            VoteCount        = p.VoteCount,
            MidjourneyPrompt = p.MidjourneyPrompt,
            Dalle3Prompt     = p.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases     = p.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Effects     = p.Effects.OrderBy(x => x.Position).Select(x => x.Effect).ToList();
        data.SideEffects = p.SideEffects.OrderBy(x => x.Position).Select(x => x.Effect).ToList();
        data.StoryHooks  = p.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a PharmaceuticalData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, PharmaceuticalData src, CancellationToken ct = default)
    {
        var pharma = await db.Pharmaceuticals.FirstOrDefaultAsync(p => p.Id == id, ct);
        var isNew = pharma == null;

        if (!isNew)
        {
            await db.PharmaceuticalAliases.Where(x => x.PharmaceuticalId == id).ExecuteDeleteAsync(ct);
            await db.PharmaceuticalEffects.Where(x => x.PharmaceuticalId == id).ExecuteDeleteAsync(ct);
            await db.PharmaceuticalSideEffects.Where(x => x.PharmaceuticalId == id).ExecuteDeleteAsync(ct);
            await db.PharmaceuticalStoryHooks.Where(x => x.PharmaceuticalId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            pharma = new Pharmaceutical { Id = id };
            db.Pharmaceuticals.Add(pharma);
        }

        FillScalars(pharma!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Pharmaceutical from src (no DB touch).</summary>
    public static void FillScalars(Pharmaceutical p, PharmaceuticalData src)
    {
        p.Name            = src.Name ?? "";
        p.Manufacturer    = src.Manufacturer ?? "";
        p.Category        = src.Category ?? "";
        p.Subcategory     = src.Subcategory ?? "";
        p.Tier            = src.TierAvailability ?? "";
        p.Legality        = src.Legality ?? "";
        p.Description     = src.Description ?? "";
        p.MethodOfUse     = src.MethodOfUse ?? "";
        p.Duration        = src.Duration ?? "";
        p.AddictionRisk   = src.AddictionRisk ?? "";
        p.StreetPrice     = src.StreetPrice ?? "";
        p.CulturalContext = src.CulturalContext ?? "";
        p.Rating          = src.Rating;
        p.VoteCount       = src.VoteCount;
        p.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        p.Dalle3Prompt    = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, PharmaceuticalData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.PharmaceuticalAliases.Add(new PharmAlias { PharmaceuticalId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.Effects.Count; i++)
            db.PharmaceuticalEffects.Add(new PharmEffect { PharmaceuticalId = id, Position = i, Effect = src.Effects[i] ?? "" });

        for (int i = 0; i < src.SideEffects.Count; i++)
            db.PharmaceuticalSideEffects.Add(new PharmSideEffect { PharmaceuticalId = id, Position = i, Effect = src.SideEffects[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.PharmaceuticalStoryHooks.Add(new PharmStoryHook { PharmaceuticalId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every pharmaceutical Entity (active or inactive), deserialize
    /// its Records.Json blob → PharmaceuticalData → persist. Also creates a minimal
    /// relational row for any active pharmaceutical entity that has no blob and no
    /// relational row yet. Returns the number of pharmaceutical entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-pharmaceutical-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var pharmaEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "pharmaceutical")
            .Select(e => e.Id)
            .ToHashSet();

        if (pharmaEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => pharmaEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            PharmaceuticalData? src;
            try { src = JsonSerializer.Deserialize<PharmaceuticalData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "PharmaceuticalMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "PharmaceuticalMapper.RebuildAllAsync: failed to persist pharmaceutical {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities.
        var activePharmaIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "pharmaceutical" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.Pharmaceuticals.AsNoTracking()
            .Where(p => activePharmaIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSet();

        foreach (var entityId in activePharmaIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new PharmaceuticalData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "PharmaceuticalMapper.RebuildAllAsync: failed to persist minimal row for pharmaceutical {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }
}
