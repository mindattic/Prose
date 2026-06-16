using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Material + 5 child
/// tables) and the domain model (MaterialData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — MaterialRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Material-level tags live in the
/// universal EntityTags layer.
///
/// All MaterialData fields are fully covered by either a scalar column or a
/// bridge table (MaterialProperties, MaterialDevelopers, MaterialApplications).
/// No fields remain blob-only.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class MaterialMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<MaterialData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Materials.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "material"),
                m => m.Id, e => e.Id,
                (m, e) => new { m.Id, Name = e.Name, m.Category, m.Rating, m.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<MaterialData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new MaterialData
            {
                Id        = r.Id.ToString("N"),
                Type      = "material",
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
    /// Full eager load of every active Material row + all child collections,
    /// then project to MaterialData. Records.Json is never read here.
    /// </summary>
    public static List<MaterialData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "material")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "material" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var materials = BuildIncludeChain(db.Materials.AsNoTracking())
            .Where(m => ids.Contains(m.Id))
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

        var result = new List<MaterialData>(materials.Count);
        foreach (var m in materials)
        {
            entityById.TryGetValue(m.Id, out var entity);
            tagsByEntity.TryGetValue(m.Id, out var tags);
            result.Add(Materialize(m, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Material by id. Returns null when not found.</summary>
    public static MaterialData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var m = BuildIncludeChain(db.Materials.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (m == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(m, entity, tags);
    }

    private static IQueryable<Material> BuildIncludeChain(IQueryable<Material> q)
        => q.AsSplitQuery()
            .Include(m => m.Aliases)
            .Include(m => m.Properties)
            .Include(m => m.Developers)
            .Include(m => m.Applications)
            .Include(m => m.StoryHooks);

    /// <summary>
    /// Build a MaterialData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name. All MaterialData fields are
    /// covered by scalar columns or bridge tables — nothing remains blob-only.
    /// </summary>
    public static MaterialData Materialize(Material m, Entity? entity, List<string>? tags)
    {
        var data = new MaterialData
        {
            Id               = m.Id.ToString("N"),
            Type             = "material",
            Name             = entity?.Name ?? m.Name,
            BrandName        = m.BrandName,
            ProductName      = m.ProductName,
            Category         = m.Category,
            TierAvailability = m.TierAvailability,
            Cost             = m.Cost,
            Description      = m.Description,
            Rating           = m.Rating,
            VoteCount        = m.VoteCount,
            MidjourneyPrompt = m.MidjourneyPrompt,
            Dalle3Prompt     = m.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases      = m.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Properties   = m.Properties.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Developers   = m.Developers.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Applications = m.Applications.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.StoryHooks   = m.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a MaterialData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, MaterialData src, CancellationToken ct = default)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct);
        var isNew = material == null;

        if (!isNew)
        {
            await db.MaterialAliases.Where(x => x.MaterialId == id).ExecuteDeleteAsync(ct);
            await db.MaterialProperties.Where(x => x.MaterialId == id).ExecuteDeleteAsync(ct);
            await db.MaterialDevelopers.Where(x => x.MaterialId == id).ExecuteDeleteAsync(ct);
            await db.MaterialApplications.Where(x => x.MaterialId == id).ExecuteDeleteAsync(ct);
            await db.MaterialStoryHooks.Where(x => x.MaterialId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            material = new Material { Id = id };
            db.Materials.Add(material);
        }

        FillScalars(material!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Material from src (no DB touch).</summary>
    public static void FillScalars(Material m, MaterialData src)
    {
        m.Name             = src.Name ?? "";
        m.BrandName        = src.BrandName ?? "";
        m.ProductName      = src.ProductName ?? "";
        m.Category         = src.Category ?? "";
        m.TierAvailability = src.TierAvailability ?? "";
        m.Cost             = src.Cost ?? "";
        m.Description      = src.Description ?? "";
        m.Rating           = src.Rating;
        m.VoteCount        = src.VoteCount;
        m.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        m.Dalle3Prompt     = src.Dalle3Prompt ?? "";
        // Tier is a classification column that MaterialData has no direct
        // field for; leave unchanged on update.
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, MaterialData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.MaterialAliases.Add(new MaterialAlias { MaterialId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.Properties.Count; i++)
            db.MaterialProperties.Add(new MaterialProperty { MaterialId = id, Position = i, Value = src.Properties[i] ?? "" });

        for (int i = 0; i < src.Developers.Count; i++)
            db.MaterialDevelopers.Add(new MaterialDeveloper { MaterialId = id, Position = i, Value = src.Developers[i] ?? "" });

        for (int i = 0; i < src.Applications.Count; i++)
            db.MaterialApplications.Add(new MaterialApplication { MaterialId = id, Position = i, Value = src.Applications[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.MaterialStoryHooks.Add(new MaterialStoryHook { MaterialId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active material Entity, deserialize its Records.Json
    /// blob → MaterialData → persist. Returns the number of materials written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-material-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var materialEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "material" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (materialEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => materialEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            MaterialData? src;
            try { src = JsonSerializer.Deserialize<MaterialData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "MaterialMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "MaterialMapper.RebuildAllAsync: failed to persist material {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }
}
