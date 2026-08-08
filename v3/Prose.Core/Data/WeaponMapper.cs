using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Weapons + 5 child
/// tables) and the domain model (WeaponryData).
///
/// Column note: domain TierAvailability → DB column Tier.
/// Bridges: WeaponAliases, WeaponBaseTechnologies, WeaponKnownUsers,
///          WeaponAmmunitionTypes, WeaponStoryHooks.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class WeaponMapper
{
    // ─────────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<WeaponryData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Weapons.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "weapon"),
                w => w.Id, e => e.Id,
                (w, e) => new { w.Id, Name = e.Name, w.Category, w.Rating, w.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<WeaponryData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new WeaponryData
            {
                Id        = r.Id.ToString("N"),
                Type      = "weapon",
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
    /// Full eager load of every active Weapon row + all child collections.
    /// Records.Json is never read here.
    /// </summary>
    public static List<WeaponryData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "weapon")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "weapon" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var weapons = BuildIncludeChain(db.Weapons.AsNoTracking())
            .Where(w => ids.Contains(w.Id))
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

        var result = new List<WeaponryData>(weapons.Count);
        foreach (var w in weapons)
        {
            entityById.TryGetValue(w.Id, out var entity);
            tagsByEntity.TryGetValue(w.Id, out var tags);
            result.Add(Materialize(w, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Weapon by id. Returns null when not found.</summary>
    public static WeaponryData? LoadOne(ProseDbContext db, Guid id)
    {
        var w = BuildIncludeChain(db.Weapons.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (w == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(w, entity, tags);
    }

    private static IQueryable<Weapon> BuildIncludeChain(IQueryable<Weapon> q)
        => q.AsSplitQuery()
            .Include(w => w.Aliases)
            .Include(w => w.BaseTechnologies)
            .Include(w => w.KnownUsers)
            .Include(w => w.AmmunitionTypes)
            .Include(w => w.StoryHooks);

    /// <summary>
    /// Build a WeaponryData from the EF entity + bridges.
    /// Note: Weapon.Tier → domain TierAvailability.
    /// </summary>
    public static WeaponryData Materialize(Weapon w, Entity? entity, List<string>? tags)
    {
        var data = new WeaponryData
        {
            Id               = w.Id.ToString("N"),
            Type             = "weapon",
            Name             = entity?.Name ?? w.Name,
            Manufacturer     = w.Manufacturer,
            Category         = w.Category,
            TierAvailability = w.Tier,
            Legality         = w.Legality,
            Description      = w.Description,
            Specifications   = w.Specifications,
            TacticalUse      = w.TacticalUse,
            CulturalContext  = w.CulturalContext,
            Rating           = w.Rating,
            VoteCount        = w.VoteCount,
            MidjourneyPrompt = w.MidjourneyPrompt,
            Dalle3Prompt     = w.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases          = w.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.BaseTechnologies = w.BaseTechnologies.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.KnownUsers       = w.KnownUsers.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.AmmunitionType   = w.AmmunitionTypes.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks       = w.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a WeaponryData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, WeaponryData src, CancellationToken ct = default)
    {
        var w = await db.Weapons.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = w == null;

        if (!isNew)
        {
            await db.WeaponAliases.Where(x => x.WeaponId == id).ExecuteDeleteAsync(ct);
            await db.WeaponBaseTechnologies.Where(x => x.WeaponId == id).ExecuteDeleteAsync(ct);
            await db.WeaponKnownUsers.Where(x => x.WeaponId == id).ExecuteDeleteAsync(ct);
            await db.WeaponAmmunitionTypes.Where(x => x.WeaponId == id).ExecuteDeleteAsync(ct);
            await db.WeaponStoryHooks.Where(x => x.WeaponId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            w = new Weapon { Id = id };
            db.Weapons.Add(w);
        }

        FillScalars(w!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Weapon from src (no DB touch).</summary>
    public static void FillScalars(Weapon w, WeaponryData src)
    {
        w.Name             = src.Name ?? "";
        w.Manufacturer     = src.Manufacturer ?? "";
        w.Category         = src.Category ?? "";
        w.Tier             = src.TierAvailability ?? "";
        w.Legality         = src.Legality ?? "";
        w.Description      = src.Description ?? "";
        w.Specifications   = src.Specifications ?? "";
        w.TacticalUse      = src.TacticalUse ?? "";
        w.CulturalContext  = src.CulturalContext ?? "";
        w.Rating           = src.Rating;
        w.VoteCount        = src.VoteCount;
        w.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        w.Dalle3Prompt     = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, WeaponryData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.WeaponAliases.Add(new WeaponAlias { WeaponId = id, Position = i, Value = src.Aliases[i] ?? "" });

        // BaseTechnologies — try to resolve to a Technology entity
        for (int i = 0; i < src.BaseTechnologies.Count; i++)
        {
            var alias = src.BaseTechnologies[i] ?? "";
            var techId = ResolveEntityId(db, alias, "technology");
            db.WeaponBaseTechnologies.Add(new WeaponBaseTechnology { WeaponId = id, Position = i, Alias = alias, TechnologyId = techId });
        }

        // KnownUsers — try to resolve to a Character entity
        for (int i = 0; i < src.KnownUsers.Count; i++)
        {
            var alias = src.KnownUsers[i] ?? "";
            var charId = ResolveEntityId(db, alias, "character");
            db.WeaponKnownUsers.Add(new WeaponKnownUser { WeaponId = id, Position = i, Alias = alias, CharacterId = charId });
        }

        // AmmunitionType — try to resolve to an Ammunition entity
        for (int i = 0; i < src.AmmunitionType.Count; i++)
        {
            var alias = src.AmmunitionType[i] ?? "";
            var ammoId = ResolveEntityId(db, alias, "ammunition");
            db.WeaponAmmunitionTypes.Add(new WeaponAmmunitionType { WeaponId = id, Position = i, Alias = alias, AmmunitionId = ammoId });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.WeaponStoryHooks.Add(new WeaponStoryHook { WeaponId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every weapon Entity (active or inactive), deserialize
    /// its Records.Json blob → WeaponryData → persist. Also creates a minimal
    /// relational row for any active weapon entity that has no blob and no
    /// relational row yet. Returns the number of weapon entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-weapon-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var entityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "weapon")
            .Select(e => new { e.Id, e.Name, e.IsActive })
            .ToList();

        if (entityIds.Count == 0) return 0;

        var idSet = entityIds.Select(e => e.Id).ToHashSet();

        var blobRows = db.Records.AsNoTracking()
            .Where(r => idSet.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        // Track existing relational rows so we can skip no-blob active entities that are already there
        var existingRelational = db.Weapons.AsNoTracking()
            .Where(w => idSet.Contains(w.Id))
            .Select(w => w.Id)
            .ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            WeaponryData? src;
            try { src = JsonSerializer.Deserialize<WeaponryData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "WeaponMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "WeaponMapper.RebuildAllAsync: failed to persist entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for active entities with no blob and no relational row
        foreach (var e in entityIds.Where(e => e.IsActive && !blobEntityIds.Contains(e.Id) && !existingRelational.Contains(e.Id)))
        {
            try
            {
                var stub = new WeaponryData { Id = e.Id.ToString("N"), Name = e.Name ?? "" };
                await PersistAsync(db, e.Id, stub, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "WeaponMapper.RebuildAllAsync: failed to create stub for entity {Id}", e.Id);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    private static Guid? ResolveEntityId(ProseDbContext db, string alias, string entityType)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Name == alias && x.IsActive);
        if (e != null) return e.Id;
        // Try slug match
        var slug = Prose.Core.Services.WorldGraphService.Slugify(alias);
        e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Slug == slug && x.IsActive);
        return e?.Id;
    }
}
