using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (EntertainmentItems + 3
/// child tables) and the domain model (EntertainmentData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// Bridges: EntertainmentAliases, EntertainmentKnownFans, EntertainmentStoryHooks.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class EntertainmentMapper
{
    // ─────────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<EntertainmentData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.EntertainmentItems.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "entertainment"),
                ei => ei.Id, e => e.Id,
                (ei, e) => new { ei.Id, Name = e.Name, ei.Category, ei.Rating, ei.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<EntertainmentData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new EntertainmentData
            {
                Id        = r.Id.ToString("N"),
                Type      = "entertainment",
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
    /// Full eager load of every active Entertainment row + all child collections.
    /// Records.Json is never read here.
    /// </summary>
    public static List<EntertainmentData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "entertainment")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "entertainment"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var items = BuildIncludeChain(db.EntertainmentItems.AsNoTracking())
            .Where(ei => ids.Contains(ei.Id))
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

        var result = new List<EntertainmentData>(items.Count);
        foreach (var ei in items)
        {
            entityById.TryGetValue(ei.Id, out var entity);
            tagsByEntity.TryGetValue(ei.Id, out var tags);
            result.Add(Materialize(ei, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Entertainment by id. Returns null when not found.</summary>
    public static EntertainmentData? LoadOne(ProseDbContext db, Guid id)
    {
        var ei = BuildIncludeChain(db.EntertainmentItems.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (ei == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(ei, entity, tags);
    }

    private static IQueryable<Entertainment> BuildIncludeChain(IQueryable<Entertainment> q)
        => q.AsSplitQuery()
            .Include(ei => ei.Aliases)
            .Include(ei => ei.KnownFans)
            .Include(ei => ei.StoryHooks);

    /// <summary>
    /// Build an EntertainmentData from the EF entity + bridges.
    /// Note: Entertainment.Tier → domain TierAvailability.
    /// </summary>
    public static EntertainmentData Materialize(Entertainment ei, Entity? entity, List<string>? tags)
    {
        var data = new EntertainmentData
        {
            Id               = ei.Id.ToString("N"),
            Type             = "entertainment",
            Name             = entity?.Name ?? ei.Name,
            Category         = ei.Category,
            Subcategory      = ei.Subcategory,
            TierAvailability = ei.Tier,
            Legality         = ei.Legality,
            Description      = ei.Description,
            Creator          = ei.Creator,
            Distributor      = ei.Distributor,
            Genre            = ei.Genre,
            Medium           = ei.Medium,
            Audience         = ei.Audience,
            CulturalImpact   = ei.CulturalImpact,
            Rating           = ei.Rating,
            VoteCount        = ei.VoteCount,
            MidjourneyPrompt = ei.MidjourneyPrompt,
            Dalle3Prompt     = ei.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases    = ei.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.KnownFans  = ei.KnownFans.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks = ei.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert an EntertainmentData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, EntertainmentData src, CancellationToken ct = default)
    {
        var ei = await db.EntertainmentItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = ei == null;

        if (!isNew)
        {
            await db.EntertainmentAliases.Where(x => x.EntertainmentId == id).ExecuteDeleteAsync(ct);
            await db.EntertainmentKnownFans.Where(x => x.EntertainmentId == id).ExecuteDeleteAsync(ct);
            await db.EntertainmentStoryHooks.Where(x => x.EntertainmentId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            ei = new Entertainment { Id = id };
            db.EntertainmentItems.Add(ei);
        }

        FillScalars(ei!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Entertainment from src (no DB touch).</summary>
    public static void FillScalars(Entertainment ei, EntertainmentData src)
    {
        ei.Name             = src.Name ?? "";
        ei.Category         = src.Category ?? "";
        ei.Subcategory      = src.Subcategory ?? "";
        ei.Tier             = src.TierAvailability ?? "";
        ei.Legality         = src.Legality ?? "";
        ei.Description      = src.Description ?? "";
        ei.Creator          = src.Creator ?? "";
        ei.Distributor      = src.Distributor ?? "";
        ei.Genre            = src.Genre ?? "";
        ei.Medium           = src.Medium ?? "";
        ei.Audience         = src.Audience ?? "";
        ei.CulturalImpact   = src.CulturalImpact ?? "";
        ei.Rating           = src.Rating;
        ei.VoteCount        = src.VoteCount;
        ei.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        ei.Dalle3Prompt     = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, EntertainmentData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.EntertainmentAliases.Add(new EntertainmentAlias { EntertainmentId = id, Position = i, Value = src.Aliases[i] ?? "" });

        // KnownFans — try to resolve to a Character entity
        for (int i = 0; i < src.KnownFans.Count; i++)
        {
            var alias = src.KnownFans[i] ?? "";
            var charId = ResolveEntityId(db, alias, "character");
            db.EntertainmentKnownFans.Add(new EntertainmentKnownFan { EntertainmentId = id, Position = i, Alias = alias, CharacterId = charId });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.EntertainmentStoryHooks.Add(new EntertainmentStoryHook { EntertainmentId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every entertainment Entity (active or inactive), deserialize
    /// its Records.Json blob → EntertainmentData → persist. Also creates a minimal
    /// relational row for any active entertainment entity with no blob and no
    /// relational row yet. Returns the number of entertainment entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-entertainment-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var entityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "entertainment")
            .Select(e => new { e.Id, e.Name })
            .ToList();

        if (entityIds.Count == 0) return 0;

        var idSet = entityIds.Select(e => e.Id).ToHashSet();

        var blobRows = db.Records.AsNoTracking()
            .Where(r => idSet.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        var existingRelational = db.EntertainmentItems.AsNoTracking()
            .Where(ei => idSet.Contains(ei.Id))
            .Select(ei => ei.Id)
            .ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            EntertainmentData? src;
            try { src = JsonSerializer.Deserialize<EntertainmentData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "EntertainmentMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "EntertainmentMapper.RebuildAllAsync: failed to persist entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for active entities with no blob and no relational row
        foreach (var e in entityIds.Where(e => !blobEntityIds.Contains(e.Id) && !existingRelational.Contains(e.Id)))
        {
            try
            {
                var stub = new EntertainmentData { Id = e.Id.ToString("N"), Name = e.Name ?? "" };
                await PersistAsync(db, e.Id, stub, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "EntertainmentMapper.RebuildAllAsync: failed to create stub for entity {Id}", e.Id);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    private static Guid? ResolveEntityId(ProseDbContext db, string alias, string entityType)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Name == alias);
        if (e != null) return e.Id;
        var slug = Prose.Core.Services.UniverseGraphService.Slugify(alias);
        e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Slug == slug);
        return e?.Id;
    }
}
