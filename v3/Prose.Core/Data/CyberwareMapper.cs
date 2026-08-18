using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (CyberwareItems + 4 child
/// tables) and the domain model (CyberwareData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// EF bridge class names: CyberwareItemAlias, CyberwareItemSideEffect,
///   CyberwareItemKnownUser, CyberwareItemStoryHook.
/// Note: CyberwareData.Specifications is a plain string scalar (not a dictionary).
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class CyberwareMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<CyberwareData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.CyberwareItems.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "cyberware"),
                c => c.Id, e => e.Id,
                (c, e) => new { c.Id, Name = e.Name, c.Category, c.Rating, c.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<CyberwareData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new CyberwareData
            {
                Id        = r.Id.ToString("N"),
                Type      = "cyberware",
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
    /// Full eager load of every active Cyberware row + all child collections,
    /// then project to CyberwareData. Records.Json is never read here.
    /// </summary>
    public static List<CyberwareData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "cyberware")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "cyberware"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var cyberwareList = BuildIncludeChain(db.CyberwareItems.AsNoTracking())
            .Where(c => ids.Contains(c.Id))
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

        var result = new List<CyberwareData>(cyberwareList.Count);
        foreach (var c in cyberwareList)
        {
            entityById.TryGetValue(c.Id, out var entity);
            tagsByEntity.TryGetValue(c.Id, out var tags);
            result.Add(Materialize(c, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Cyberware by id. Returns null when not found.</summary>
    public static CyberwareData? LoadOne(ProseDbContext db, Guid id)
    {
        var c = BuildIncludeChain(db.CyberwareItems.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (c == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(c, entity, tags);
    }

    private static IQueryable<Cyberware> BuildIncludeChain(IQueryable<Cyberware> q)
        => q.AsSplitQuery()
            .Include(c => c.Aliases)
            .Include(c => c.SideEffects)
            .Include(c => c.KnownUsers)
            .Include(c => c.StoryHooks);

    /// <summary>
    /// Build a CyberwareData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// Note: Cyberware.Tier maps to domain TierAvailability.
    /// </summary>
    public static CyberwareData Materialize(Cyberware c, Entity? entity, List<string>? tags)
    {
        var data = new CyberwareData
        {
            Id                       = c.Id.ToString("N"),
            Type                     = "cyberware",
            Name                     = entity?.Name ?? c.Name,
            Manufacturer             = c.Manufacturer,
            Category                 = c.Category,
            BodyLocation             = c.BodyLocation,
            TierAvailability         = c.Tier,
            Legality                 = c.Legality,
            BrandName                = c.BrandName,
            ProductName              = c.ProductName,
            Description              = c.Description,
            InstallationRequirements = c.InstallationRequirements,
            RejectionRisk            = c.RejectionRisk,
            Maintenance              = c.Maintenance,
            Specifications           = c.Specifications,
            CulturalContext          = c.CulturalContext,
            StreetPrice              = c.StreetPrice,
            LicensedPrice            = c.LicensedPrice,
            Rating                   = c.Rating,
            VoteCount                = c.VoteCount,
            MidjourneyPrompt         = c.MidjourneyPrompt,
            Dalle3Prompt             = c.Dalle3Prompt,
            Tags                     = tags ?? new List<string>(),
        };

        data.Aliases     = c.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.SideEffects = c.SideEffects.OrderBy(x => x.Position).Select(x => x.Effect).ToList();
        data.KnownUsers  = c.KnownUsers.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks  = c.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a CyberwareData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, CyberwareData src, CancellationToken ct = default)
    {
        var cw = await db.CyberwareItems.FirstOrDefaultAsync(c => c.Id == id, ct);
        var isNew = cw == null;

        if (!isNew)
        {
            await db.CyberwareItemAliases.Where(x => x.CyberwareId == id).ExecuteDeleteAsync(ct);
            await db.CyberwareItemSideEffects.Where(x => x.CyberwareId == id).ExecuteDeleteAsync(ct);
            await db.CyberwareItemKnownUsers.Where(x => x.CyberwareId == id).ExecuteDeleteAsync(ct);
            await db.CyberwareItemStoryHooks.Where(x => x.CyberwareId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            cw = new Cyberware { Id = id };
            db.CyberwareItems.Add(cw);
        }

        FillScalars(cw!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Cyberware from src (no DB touch).</summary>
    public static void FillScalars(Cyberware c, CyberwareData src)
    {
        c.Name                     = src.Name ?? "";
        c.Manufacturer             = src.Manufacturer ?? "";
        c.Category                 = src.Category ?? "";
        c.BodyLocation             = src.BodyLocation ?? "";
        c.Tier                     = src.TierAvailability ?? "";
        c.Legality                 = src.Legality ?? "";
        c.BrandName                = src.BrandName ?? "";
        c.ProductName              = src.ProductName ?? "";
        c.Description              = src.Description ?? "";
        c.InstallationRequirements = src.InstallationRequirements ?? "";
        c.RejectionRisk            = src.RejectionRisk ?? "";
        c.Maintenance              = src.Maintenance ?? "";
        c.Specifications           = src.Specifications ?? "";
        c.CulturalContext          = src.CulturalContext ?? "";
        c.StreetPrice              = src.StreetPrice ?? "";
        c.LicensedPrice            = src.LicensedPrice ?? "";
        c.Rating                   = src.Rating;
        c.VoteCount                = src.VoteCount;
        c.MidjourneyPrompt         = src.MidjourneyPrompt ?? "";
        c.Dalle3Prompt             = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, CyberwareData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.CyberwareItemAliases.Add(new CyberwareItemAlias { CyberwareId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.SideEffects.Count; i++)
            db.CyberwareItemSideEffects.Add(new CyberwareItemSideEffect { CyberwareId = id, Position = i, Effect = src.SideEffects[i] ?? "" });

        for (int i = 0; i < src.KnownUsers.Count; i++)
        {
            var charId = ResolveEntityId(db, "character", src.KnownUsers[i]);
            db.CyberwareItemKnownUsers.Add(new CyberwareItemKnownUser
            {
                CyberwareId = id,
                Position    = i,
                Alias       = src.KnownUsers[i] ?? "",
                CharacterId = charId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.CyberwareItemStoryHooks.Add(new CyberwareItemStoryHook { CyberwareId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every cyberware Entity (active or inactive), deserialize
    /// its Records.Json blob → CyberwareData → persist. Also creates a minimal
    /// relational row for any active cyberware entity that has no blob and no
    /// relational row yet. Returns the number of cyberware entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-cyberware-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var cwEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "cyberware")
            .Select(e => e.Id)
            .ToHashSet();

        if (cwEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => cwEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            CyberwareData? src;
            try { src = JsonSerializer.Deserialize<CyberwareData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "CyberwareMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "CyberwareMapper.RebuildAllAsync: failed to persist cyberware {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities.
        var activeCwIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "cyberware")
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.CyberwareItems.AsNoTracking()
            .Where(c => activeCwIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSet();

        foreach (var entityId in activeCwIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new CyberwareData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "CyberwareMapper.RebuildAllAsync: failed to persist minimal row for cyberware {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

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
