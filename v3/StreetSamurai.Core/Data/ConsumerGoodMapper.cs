using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (ConsumerGoods + 2 child
/// tables) and the domain model (ConsumerGoodData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// Bridges: ConsumerGoodAliases (Aliases), ConsumerGoodStoryHooks (StoryHooks).
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class ConsumerGoodMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<ConsumerGoodData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.ConsumerGoods.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "consumer_good"),
                cg => cg.Id, e => e.Id,
                (cg, e) => new { cg.Id, Name = e.Name, cg.Category, cg.Rating, cg.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<ConsumerGoodData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new ConsumerGoodData
            {
                Id        = r.Id.ToString("N"),
                Type      = "consumer_good",
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
    /// Full eager load of every active ConsumerGood row + all child collections,
    /// then project to ConsumerGoodData. Records.Json is never read here.
    /// </summary>
    public static List<ConsumerGoodData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "consumer_good")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "consumer_good" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var goods = BuildIncludeChain(db.ConsumerGoods.AsNoTracking())
            .Where(cg => ids.Contains(cg.Id))
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

        var result = new List<ConsumerGoodData>(goods.Count);
        foreach (var cg in goods)
        {
            entityById.TryGetValue(cg.Id, out var entity);
            tagsByEntity.TryGetValue(cg.Id, out var tags);
            result.Add(Materialize(cg, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single ConsumerGood by id. Returns null when not found.</summary>
    public static ConsumerGoodData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var cg = BuildIncludeChain(db.ConsumerGoods.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (cg == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(cg, entity, tags);
    }

    private static IQueryable<ConsumerGood> BuildIncludeChain(IQueryable<ConsumerGood> q)
        => q.AsSplitQuery()
            .Include(cg => cg.Aliases)
            .Include(cg => cg.StoryHooks);

    /// <summary>
    /// Build a ConsumerGoodData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// Note: ConsumerGood.Tier maps to domain TierAvailability.
    /// </summary>
    public static ConsumerGoodData Materialize(ConsumerGood cg, Entity? entity, List<string>? tags)
    {
        var data = new ConsumerGoodData
        {
            Id               = cg.Id.ToString("N"),
            Type             = "consumer_good",
            Name             = entity?.Name ?? cg.Name,
            Manufacturer     = cg.Manufacturer,
            Category         = cg.Category,
            Subcategory      = cg.Subcategory,
            TierAvailability = cg.Tier,
            BrandName        = cg.BrandName,
            ProductName      = cg.ProductName,
            FlavorProfile    = cg.FlavorProfile,
            Price            = cg.Price,
            PopularityRank   = cg.PopularityRank,
            Slogan           = cg.Slogan,
            CulturalContext  = cg.CulturalContext,
            Description      = cg.Description,
            Rating           = cg.Rating,
            VoteCount        = cg.VoteCount,
            MidjourneyPrompt = cg.MidjourneyPrompt,
            Dalle3Prompt     = cg.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.StoryHooks = cg.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a ConsumerGoodData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, ConsumerGoodData src, CancellationToken ct = default)
    {
        var cg = await db.ConsumerGoods.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = cg == null;

        if (!isNew)
        {
            await db.ConsumerGoodAliases.Where(x => x.ConsumerGoodId == id).ExecuteDeleteAsync(ct);
            await db.ConsumerGoodStoryHooks.Where(x => x.ConsumerGoodId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            cg = new ConsumerGood { Id = id };
            db.ConsumerGoods.Add(cg);
        }

        FillScalars(cg!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on ConsumerGood from src (no DB touch).</summary>
    public static void FillScalars(ConsumerGood cg, ConsumerGoodData src)
    {
        cg.Name            = src.Name ?? "";
        cg.Manufacturer    = src.Manufacturer ?? "";
        cg.Category        = src.Category ?? "";
        cg.Subcategory     = src.Subcategory ?? "";
        cg.Tier            = src.TierAvailability ?? "";
        cg.BrandName       = src.BrandName ?? "";
        cg.ProductName     = src.ProductName ?? "";
        cg.FlavorProfile   = src.FlavorProfile ?? "";
        cg.Price           = src.Price ?? "";
        cg.PopularityRank  = src.PopularityRank;
        cg.Slogan          = src.Slogan ?? "";
        cg.CulturalContext = src.CulturalContext ?? "";
        cg.Description     = src.Description ?? "";
        cg.Rating          = src.Rating;
        cg.VoteCount       = src.VoteCount;
        cg.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        cg.Dalle3Prompt    = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, ConsumerGoodData src)
    {
        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.ConsumerGoodStoryHooks.Add(new ConsumerGoodStoryHook { ConsumerGoodId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every consumer_good Entity (active or inactive), deserialize
    /// its Records.Json blob → ConsumerGoodData → persist. Also creates a minimal
    /// relational row for any active consumer_good entity that has no blob and no
    /// relational row yet. Returns the number of consumer good entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-consumer-good-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var cgEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "consumer_good")
            .Select(e => e.Id)
            .ToHashSet();

        if (cgEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => cgEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            ConsumerGoodData? src;
            try { src = JsonSerializer.Deserialize<ConsumerGoodData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ConsumerGoodMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "ConsumerGoodMapper.RebuildAllAsync: failed to persist consumer good {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities.
        var activeCgIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "consumer_good" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.ConsumerGoods.AsNoTracking()
            .Where(cg => activeCgIds.Contains(cg.Id))
            .Select(cg => cg.Id)
            .ToHashSet();

        foreach (var entityId in activeCgIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new ConsumerGoodData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ConsumerGoodMapper.RebuildAllAsync: failed to persist minimal row for consumer good {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }
}
