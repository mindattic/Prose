using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (EquipmentItems + 5 child
/// tables) and the domain model (EquipmentData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// Specifications is a Dictionary&lt;string,string&gt; stored in EquipmentSpecifications
/// (KeyName + Value, no Position column — keyed by KeyName).
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class EquipmentMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<EquipmentData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.EquipmentItems.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "equipment"),
                eq => eq.Id, e => e.Id,
                (eq, e) => new { eq.Id, Name = e.Name, eq.Category, eq.Rating, eq.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<EquipmentData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new EquipmentData
            {
                Id        = r.Id.ToString("N"),
                Type      = "equipment",
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
    /// Full eager load of every active Equipment row + all child collections,
    /// then project to EquipmentData. Records.Json is never read here.
    /// </summary>
    public static List<EquipmentData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "equipment")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "equipment" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var equipment = BuildIncludeChain(db.EquipmentItems.AsNoTracking())
            .Where(eq => ids.Contains(eq.Id))
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

        var result = new List<EquipmentData>(equipment.Count);
        foreach (var eq in equipment)
        {
            entityById.TryGetValue(eq.Id, out var entity);
            tagsByEntity.TryGetValue(eq.Id, out var tags);
            result.Add(Materialize(eq, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Equipment by id. Returns null when not found.</summary>
    public static EquipmentData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var eq = BuildIncludeChain(db.EquipmentItems.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (eq == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(eq, entity, tags);
    }

    private static IQueryable<Equipment> BuildIncludeChain(IQueryable<Equipment> q)
        => q.AsSplitQuery()
            .Include(eq => eq.Aliases)
            .Include(eq => eq.BaseTechnologies)
            .Include(eq => eq.KnownUsers)
            .Include(eq => eq.Specifications)
            .Include(eq => eq.StoryHooks);

    /// <summary>
    /// Build an EquipmentData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// Note: Equipment.Tier maps to domain TierAvailability.
    /// </summary>
    public static EquipmentData Materialize(Equipment eq, Entity? entity, List<string>? tags)
    {
        var data = new EquipmentData
        {
            Id               = eq.Id.ToString("N"),
            Type             = "equipment",
            Name             = entity?.Name ?? eq.Name,
            Manufacturer     = eq.Manufacturer,
            Category         = eq.Category,
            TierAvailability = eq.Tier,
            Legality         = eq.Legality,
            BrandName        = eq.BrandName,
            ProductName      = eq.ProductName,
            Description      = eq.Description,
            TacticalUse      = eq.TacticalUse,
            CulturalContext  = eq.CulturalContext,
            Rating           = eq.Rating,
            VoteCount        = eq.VoteCount,
            MidjourneyPrompt = eq.MidjourneyPrompt,
            Dalle3Prompt     = eq.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases          = eq.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.BaseTechnologies = eq.BaseTechnologies.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.KnownUsers       = eq.KnownUsers.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks       = eq.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        // Specifications: Dictionary<string,string> keyed by KeyName (no Position).
        data.Specifications = eq.Specifications
            .ToDictionary(s => s.KeyName, s => s.Value);

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert an EquipmentData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, EquipmentData src, CancellationToken ct = default)
    {
        var eq = await db.EquipmentItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = eq == null;

        if (!isNew)
        {
            await db.EquipmentAliases.Where(x => x.EquipmentId == id).ExecuteDeleteAsync(ct);
            await db.EquipmentBaseTechnologies.Where(x => x.EquipmentId == id).ExecuteDeleteAsync(ct);
            await db.EquipmentKnownUsers.Where(x => x.EquipmentId == id).ExecuteDeleteAsync(ct);
            await db.EquipmentSpecifications.Where(x => x.EquipmentId == id).ExecuteDeleteAsync(ct);
            await db.EquipmentStoryHooks.Where(x => x.EquipmentId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            eq = new Equipment { Id = id };
            db.EquipmentItems.Add(eq);
        }

        FillScalars(eq!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Equipment from src (no DB touch).</summary>
    public static void FillScalars(Equipment eq, EquipmentData src)
    {
        eq.Name            = src.Name ?? "";
        eq.Manufacturer    = src.Manufacturer ?? "";
        eq.Category        = src.Category ?? "";
        eq.Tier            = src.TierAvailability ?? "";
        eq.Legality        = src.Legality ?? "";
        eq.BrandName       = src.BrandName ?? "";
        eq.ProductName     = src.ProductName ?? "";
        eq.Description     = src.Description ?? "";
        eq.TacticalUse     = src.TacticalUse ?? "";
        eq.CulturalContext = src.CulturalContext ?? "";
        eq.Rating          = src.Rating;
        eq.VoteCount       = src.VoteCount;
        eq.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        eq.Dalle3Prompt    = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, EquipmentData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.EquipmentAliases.Add(new EquipmentAlias { EquipmentId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.BaseTechnologies.Count; i++)
        {
            var techId = ResolveEntityId(db, "technology", src.BaseTechnologies[i]);
            db.EquipmentBaseTechnologies.Add(new EquipmentBaseTechnology
            {
                EquipmentId  = id,
                Position     = i,
                Alias        = src.BaseTechnologies[i] ?? "",
                TechnologyId = techId,
            });
        }

        for (int i = 0; i < src.KnownUsers.Count; i++)
        {
            var charId = ResolveEntityId(db, "character", src.KnownUsers[i]);
            db.EquipmentKnownUsers.Add(new EquipmentKnownUser
            {
                EquipmentId = id,
                Position    = i,
                Alias       = src.KnownUsers[i] ?? "",
                CharacterId = charId,
            });
        }

        // Specifications is a Dictionary<string,string>; KeyName is the key (no Position).
        foreach (var kvp in src.Specifications)
            db.EquipmentSpecifications.Add(new EquipmentSpecification
            {
                EquipmentId = id,
                KeyName     = kvp.Key ?? "",
                Value       = kvp.Value ?? "",
            });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.EquipmentStoryHooks.Add(new EquipmentStoryHook { EquipmentId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every equipment Entity (active or inactive), deserialize
    /// its Records.Json blob → EquipmentData → persist. Also creates a minimal
    /// relational row for any active equipment entity that has no blob and no
    /// relational row yet. Returns the number of equipment entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-equipment-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var equipEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "equipment")
            .Select(e => e.Id)
            .ToHashSet();

        if (equipEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => equipEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            EquipmentData? src;
            try { src = JsonSerializer.Deserialize<EquipmentData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "EquipmentMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "EquipmentMapper.RebuildAllAsync: failed to persist equipment {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities that have no relational row yet.
        var activeEquipIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "equipment" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.EquipmentItems.AsNoTracking()
            .Where(eq => activeEquipIds.Contains(eq.Id))
            .Select(eq => eq.Id)
            .ToHashSet();

        foreach (var entityId in activeEquipIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new EquipmentData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "EquipmentMapper.RebuildAllAsync: failed to persist minimal row for equipment {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    private static Guid? ResolveEntityId(StreetSamuraiDbContext db, string entityType, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = StreetSamurai.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && e.IsActive
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }
}
