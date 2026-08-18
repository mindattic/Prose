using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Ammunition + 4 child
/// tables) and the domain model (AmmunitionData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — AmmunitionRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Ammunition-level tags live in the
/// universal EntityTags layer.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class AmmunitionMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Caliber,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<AmmunitionData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Ammunitions.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "ammunition"),
                a => a.Id, e => e.Id,
                (a, e) => new { a.Id, Name = e.Name, a.Category, a.Caliber, a.Rating, a.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<AmmunitionData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new AmmunitionData
            {
                Id        = r.Id.ToString("N"),
                Type      = "ammunition",
                Name      = r.Name ?? "",
                Category  = r.Category ?? "",
                Caliber   = r.Caliber ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Ammunition row + all child collections,
    /// then project to AmmunitionData. Records.Json is never read here.
    /// </summary>
    public static List<AmmunitionData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "ammunition")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "ammunition"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var ammunitions = BuildIncludeChain(db.Ammunitions.AsNoTracking())
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

        var result = new List<AmmunitionData>(ammunitions.Count);
        foreach (var a in ammunitions)
        {
            entityById.TryGetValue(a.Id, out var entity);
            tagsByEntity.TryGetValue(a.Id, out var tags);
            result.Add(Materialize(a, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Ammunition by id. Returns null when not found.</summary>
    public static AmmunitionData? LoadOne(ProseDbContext db, Guid id)
    {
        var a = BuildIncludeChain(db.Ammunitions.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (a == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(a, entity, tags);
    }

    private static IQueryable<Ammunition> BuildIncludeChain(IQueryable<Ammunition> q)
        => q.AsSplitQuery()
            .Include(a => a.Aliases)
            .Include(a => a.CompatibleWeapons)
            .Include(a => a.Variants)
            .Include(a => a.StoryHooks);

    /// <summary>
    /// Build an AmmunitionData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name.
    /// </summary>
    public static AmmunitionData Materialize(Ammunition a, Entity? entity, List<string>? tags)
    {
        var data = new AmmunitionData
        {
            Id             = a.Id.ToString("N"),
            Type           = "ammunition",
            Name           = entity?.Name ?? a.Name,
            Manufacturer   = a.Manufacturer,
            Caliber        = a.Caliber,
            Category       = a.Category,
            TierAvailability = a.Tier,
            Legality       = a.Legality,
            Description    = a.Description,
            Specifications = a.Specifications,
            CulturalContext = a.CulturalContext,
            Rating         = a.Rating,
            VoteCount      = a.VoteCount,
            MidjourneyPrompt = a.MidjourneyPrompt,
            Dalle3Prompt     = a.Dalle3Prompt,
            Tags           = tags ?? new List<string>(),
        };

        data.Aliases           = a.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.CompatibleWeapons = a.CompatibleWeapons.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.Variants          = a.Variants.OrderBy(x => x.Position).Select(x => x.VariantName).ToList();
        data.StoryHooks        = a.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert an AmmunitionData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, AmmunitionData src, CancellationToken ct = default)
    {
        var ammo = await db.Ammunitions.FirstOrDefaultAsync(a => a.Id == id, ct);
        var isNew = ammo == null;

        if (!isNew)
        {
            await db.AmmunitionAliases.Where(x => x.AmmunitionId == id).ExecuteDeleteAsync(ct);
            await db.AmmunitionCompatibleWeapons.Where(x => x.AmmunitionId == id).ExecuteDeleteAsync(ct);
            await db.AmmunitionVariants.Where(x => x.AmmunitionId == id).ExecuteDeleteAsync(ct);
            await db.AmmunitionStoryHooks.Where(x => x.AmmunitionId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            ammo = new Ammunition { Id = id };
            db.Ammunitions.Add(ammo);
        }

        FillScalars(ammo!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Ammunition from src (no DB touch).</summary>
    public static void FillScalars(Ammunition a, AmmunitionData src)
    {
        a.Name           = src.Name ?? "";
        a.Manufacturer   = src.Manufacturer ?? "";
        a.Caliber        = src.Caliber ?? "";
        a.Category       = src.Category ?? "";
        a.Tier           = src.TierAvailability ?? "";
        a.Legality       = src.Legality ?? "";
        a.Description    = src.Description ?? "";
        a.Specifications = src.Specifications ?? "";
        a.CulturalContext = src.CulturalContext ?? "";
        a.Rating         = src.Rating;
        a.VoteCount      = src.VoteCount;
        a.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        a.Dalle3Prompt   = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, AmmunitionData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.AmmunitionAliases.Add(new AmmunitionAlias { AmmunitionId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.CompatibleWeapons.Count; i++)
        {
            var weaponId = ResolveEntityId(db, "weapon", src.CompatibleWeapons[i]);
            db.AmmunitionCompatibleWeapons.Add(new AmmunitionCompatibleWeapon
            {
                AmmunitionId = id,
                Position     = i,
                Alias        = src.CompatibleWeapons[i] ?? "",
                WeaponId     = weaponId,
            });
        }

        for (int i = 0; i < src.Variants.Count; i++)
            db.AmmunitionVariants.Add(new AmmunitionVariant { AmmunitionId = id, Position = i, VariantName = src.Variants[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.AmmunitionStoryHooks.Add(new AmmunitionStoryHook { AmmunitionId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active ammunition Entity, deserialize its Records.Json
    /// blob → AmmunitionData → persist. Returns the number of ammunition entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-ammunition-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var ammoEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "ammunition")
            .Select(e => e.Id)
            .ToHashSet();

        if (ammoEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => ammoEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            AmmunitionData? src;
            try { src = JsonSerializer.Deserialize<AmmunitionData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "AmmunitionMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "AmmunitionMapper.RebuildAllAsync: failed to persist ammunition {Id}", row.EntityId);
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
