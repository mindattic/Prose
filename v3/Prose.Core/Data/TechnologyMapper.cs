using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Technologies + 5 child
/// tables) and the domain model (TechnologyData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// Bridges: Aliases, Developers (FK→entity), BaseTechnologies (FK→technology),
///          Enables (FK→entity, stored as TechnologyEnabledList), StoryHooks.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class TechnologyMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<TechnologyData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Technologies.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "technology"),
                t => t.Id, e => e.Id,
                (t, e) => new { t.Id, Name = e.Name, t.Rating, t.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<TechnologyData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new TechnologyData
            {
                Id        = r.Id.ToString("N"),
                Type      = "technology",
                Name      = r.Name ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Technology row + all child collections,
    /// then project to TechnologyData. Records.Json is never read here.
    /// </summary>
    public static List<TechnologyData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "technology")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "technology"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var technologies = BuildIncludeChain(db.Technologies.AsNoTracking())
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

        var result = new List<TechnologyData>(technologies.Count);
        foreach (var t in technologies)
        {
            entityById.TryGetValue(t.Id, out var entity);
            tagsByEntity.TryGetValue(t.Id, out var tags);
            result.Add(Materialize(t, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Technology by id. Returns null when not found.</summary>
    public static TechnologyData? LoadOne(ProseDbContext db, Guid id)
    {
        var t = BuildIncludeChain(db.Technologies.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (t == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(x => x.EntityId == id)
            .Select(x => x.Tag!.Name)
            .ToList();
        return Materialize(t, entity, tags);
    }

    private static IQueryable<Technology> BuildIncludeChain(IQueryable<Technology> q)
        => q.AsSplitQuery()
            .Include(t => t.Aliases)
            .Include(t => t.Developers)
            .Include(t => t.BaseTechnologies)
            .Include(t => t.Enables)
            .Include(t => t.StoryHooks);

    /// <summary>
    /// Build a TechnologyData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// Note: Technology.Tier maps to domain TierAvailability.
    /// </summary>
    public static TechnologyData Materialize(Technology t, Entity? entity, List<string>? tags)
    {
        var data = new TechnologyData
        {
            Id               = t.Id.ToString("N"),
            Type             = "technology",
            Name             = entity?.Name ?? t.Name,
            Subcategory      = t.Subcategory,
            TierAvailability = t.Tier,
            BrandName        = t.BrandName,
            ProductName      = t.ProductName,
            Description      = t.Description,
            SocialImpact     = t.SocialImpact,
            Rating           = t.Rating,
            VoteCount        = t.VoteCount,
            MidjourneyPrompt = t.MidjourneyPrompt,
            Dalle3Prompt     = t.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases          = t.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Developers       = t.Developers.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.BaseTechnologies = t.BaseTechnologies.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.Enables          = t.Enables.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks       = t.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a TechnologyData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, TechnologyData src, CancellationToken ct = default)
    {
        var tech = await db.Technologies.FirstOrDefaultAsync(t => t.Id == id, ct);
        var isNew = tech == null;

        if (!isNew)
        {
            await db.TechnologyAliases.Where(x => x.TechnologyId == id).ExecuteDeleteAsync(ct);
            await db.TechnologyDevelopers.Where(x => x.TechnologyId == id).ExecuteDeleteAsync(ct);
            await db.TechnologyBaseTechnologies.Where(x => x.TechnologyId == id).ExecuteDeleteAsync(ct);
            await db.TechnologyEnabledList.Where(x => x.TechnologyId == id).ExecuteDeleteAsync(ct);
            await db.TechnologyStoryHooks.Where(x => x.TechnologyId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            tech = new Technology { Id = id };
            db.Technologies.Add(tech);
        }

        FillScalars(tech!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Technology from src (no DB touch).</summary>
    public static void FillScalars(Technology t, TechnologyData src)
    {
        t.Name            = src.Name ?? "";
        t.Category        = "";               // not in TechnologyData domain model; keep blank
        t.Subcategory     = src.Subcategory ?? "";
        t.Tier            = src.TierAvailability ?? "";
        t.BrandName       = src.BrandName ?? "";
        t.ProductName     = src.ProductName ?? "";
        t.Description     = src.Description ?? "";
        t.SocialImpact    = src.SocialImpact ?? "";
        t.Rating          = src.Rating;
        t.VoteCount       = src.VoteCount;
        t.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        t.Dalle3Prompt    = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, TechnologyData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.TechnologyAliases.Add(new TechnologyAlias { TechnologyId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.Developers.Count; i++)
        {
            var devId = ResolveEntityId(db, src.Developers[i]);
            db.TechnologyDevelopers.Add(new TechnologyDeveloper
            {
                TechnologyId      = id,
                Position          = i,
                Alias             = src.Developers[i] ?? "",
                DeveloperEntityId = devId,
            });
        }

        for (int i = 0; i < src.BaseTechnologies.Count; i++)
        {
            var baseTechId = ResolveEntityId(db, "technology", src.BaseTechnologies[i]);
            db.TechnologyBaseTechnologies.Add(new TechnologyBaseTechnology
            {
                TechnologyId     = id,
                Position         = i,
                Alias            = src.BaseTechnologies[i] ?? "",
                BaseTechnologyId = baseTechId,
            });
        }

        for (int i = 0; i < src.Enables.Count; i++)
        {
            var enabledId = ResolveEntityId(db, src.Enables[i]);
            db.TechnologyEnabledList.Add(new TechnologyEnables
            {
                TechnologyId    = id,
                Position        = i,
                Alias           = src.Enables[i] ?? "",
                EnabledEntityId = enabledId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.TechnologyStoryHooks.Add(new TechnologyStoryHook { TechnologyId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every technology Entity (active or inactive), deserialize
    /// its Records.Json blob → TechnologyData → persist. Also creates a minimal
    /// relational row for any active technology entity that has no blob and no
    /// relational row yet. Returns the number of technology entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-technology-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var techEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "technology")
            .Select(e => e.Id)
            .ToHashSet();

        if (techEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => techEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            TechnologyData? src;
            try { src = JsonSerializer.Deserialize<TechnologyData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "TechnologyMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "TechnologyMapper.RebuildAllAsync: failed to persist technology {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities.
        var activeTechIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "technology")
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.Technologies.AsNoTracking()
            .Where(t => activeTechIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToHashSet();

        foreach (var entityId in activeTechIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new TechnologyData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "TechnologyMapper.RebuildAllAsync: failed to persist minimal row for technology {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Resolve an entity id by name across all entity types (for Developers / Enables).</summary>
    private static Guid? ResolveEntityId(ProseDbContext db, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    private static Guid? ResolveEntityId(ProseDbContext db, string entityType, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }
}
