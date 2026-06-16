using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Apparels + 4 child
/// tables) and the domain model (ApparelData).
///
/// Column note: domain TierAssociation → DB column Tier.
/// Bridges: ApparelAliases, ApparelMaterials, ApparelWornBy, ApparelStoryHooks.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class ApparelMapper
{
    // ─────────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<ApparelData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Apparels.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "apparel"),
                a => a.Id, e => e.Id,
                (a, e) => new { a.Id, Name = e.Name, a.Category, a.Rating, a.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<ApparelData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new ApparelData
            {
                Id        = r.Id.ToString("N"),
                Type      = "apparel",
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
    /// Full eager load of every active Apparel row + all child collections.
    /// Records.Json is never read here.
    /// </summary>
    public static List<ApparelData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "apparel")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "apparel" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var items = BuildIncludeChain(db.Apparels.AsNoTracking())
            .Where(a => ids.Contains(a.Id))
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

        var result = new List<ApparelData>(items.Count);
        foreach (var a in items)
        {
            entityById.TryGetValue(a.Id, out var entity);
            tagsByEntity.TryGetValue(a.Id, out var tags);
            result.Add(Materialize(a, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Apparel by id. Returns null when not found.</summary>
    public static ApparelData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var a = BuildIncludeChain(db.Apparels.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (a == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(a, entity, tags);
    }

    private static IQueryable<Apparel> BuildIncludeChain(IQueryable<Apparel> q)
        => q.AsSplitQuery()
            .Include(a => a.Aliases)
            .Include(a => a.Materials)
            .Include(a => a.WornBy)
            .Include(a => a.StoryHooks);

    /// <summary>
    /// Build an ApparelData from the EF entity + bridges.
    /// Note: Apparel.Tier → domain TierAssociation.
    /// </summary>
    public static ApparelData Materialize(Apparel a, Entity? entity, List<string>? tags)
    {
        var data = new ApparelData
        {
            Id               = a.Id.ToString("N"),
            Type             = "apparel",
            Name             = entity?.Name ?? a.Name,
            Manufacturer     = a.Manufacturer,
            Category         = a.Category,
            TierAssociation  = a.Tier,
            Functionality    = a.Functionality,
            WhatItSays       = a.WhatItSays,
            PriceRange       = a.PriceRange,
            AugCompatible    = a.AugCompatible,
            GeneCompatible   = a.GeneCompatible,
            Description      = a.Description,
            Rating           = a.Rating,
            VoteCount        = a.VoteCount,
            MidjourneyPrompt = a.MidjourneyPrompt,
            Dalle3Prompt     = a.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Materials  = a.Materials.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.WornBy     = a.WornBy.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks = a.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert an ApparelData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, ApparelData src, CancellationToken ct = default)
    {
        var a = await db.Apparels.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = a == null;

        if (!isNew)
        {
            await db.ApparelAliases.Where(x => x.ApparelId == id).ExecuteDeleteAsync(ct);
            await db.ApparelMaterials.Where(x => x.ApparelId == id).ExecuteDeleteAsync(ct);
            await db.ApparelWornByRows.Where(x => x.ApparelId == id).ExecuteDeleteAsync(ct);
            await db.ApparelStoryHooks.Where(x => x.ApparelId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            a = new Apparel { Id = id };
            db.Apparels.Add(a);
        }

        FillScalars(a!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Apparel from src (no DB touch).</summary>
    public static void FillScalars(Apparel a, ApparelData src)
    {
        a.Name             = src.Name ?? "";
        a.Manufacturer     = src.Manufacturer ?? "";
        a.Category         = src.Category ?? "";
        a.Tier             = src.TierAssociation ?? "";
        a.Functionality    = src.Functionality ?? "";
        a.WhatItSays       = src.WhatItSays ?? "";
        a.PriceRange       = src.PriceRange ?? "";
        a.AugCompatible    = src.AugCompatible;
        a.GeneCompatible   = src.GeneCompatible;
        a.Description      = src.Description ?? "";
        a.Rating           = src.Rating;
        a.VoteCount        = src.VoteCount;
        a.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        a.Dalle3Prompt     = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, ApparelData src)
    {
        for (int i = 0; i < src.Materials.Count; i++)
            db.ApparelMaterials.Add(new ApparelMaterial { ApparelId = id, Position = i, Value = src.Materials[i] ?? "" });

        for (int i = 0; i < src.WornBy.Count; i++)
        {
            var alias = src.WornBy[i] ?? "";
            var charId = ResolveEntityId(db, alias, "character");
            db.ApparelWornByRows.Add(new ApparelWornBy { ApparelId = id, Position = i, Alias = alias, CharacterEntityId = charId });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.ApparelStoryHooks.Add(new ApparelStoryHook { ApparelId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every apparel Entity (active or inactive), deserialize
    /// its Records.Json blob → ApparelData → persist. Also creates a minimal
    /// relational row for any active apparel entity with no blob and no
    /// relational row yet. Returns the number of apparel entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-apparel-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var entityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "apparel")
            .Select(e => new { e.Id, e.Name, e.IsActive })
            .ToList();

        if (entityIds.Count == 0) return 0;

        var idSet = entityIds.Select(e => e.Id).ToHashSet();

        var blobRows = db.Records.AsNoTracking()
            .Where(r => idSet.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        var existingRelational = db.Apparels.AsNoTracking()
            .Where(a => idSet.Contains(a.Id))
            .Select(a => a.Id)
            .ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            ApparelData? src;
            try { src = JsonSerializer.Deserialize<ApparelData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ApparelMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "ApparelMapper.RebuildAllAsync: failed to persist entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for active entities with no blob and no relational row
        foreach (var e in entityIds.Where(e => e.IsActive && !blobEntityIds.Contains(e.Id) && !existingRelational.Contains(e.Id)))
        {
            try
            {
                var stub = new ApparelData { Id = e.Id.ToString("N"), Name = e.Name ?? "" };
                await PersistAsync(db, e.Id, stub, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ApparelMapper.RebuildAllAsync: failed to create stub for entity {Id}", e.Id);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    private static Guid? ResolveEntityId(StreetSamuraiDbContext db, string alias, string entityType)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Name == alias && x.IsActive);
        if (e != null) return e.Id;
        var slug = StreetSamurai.Core.Services.WorldGraphService.Slugify(alias);
        e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Slug == slug && x.IsActive);
        return e?.Id;
    }
}
