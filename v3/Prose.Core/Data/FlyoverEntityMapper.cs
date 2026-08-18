using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (FlyoverEntities + 3 child
/// tables) and the domain model (FlyoverEntityData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — FlyoverEntityRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Tags live in the universal EntityTags layer.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class FlyoverEntityMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Classification,
    /// ThreatLevel, Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<FlyoverEntityData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.FlyoverEntities.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "flyover_entity"),
                f => f.Id, e => e.Id,
                (f, e) => new { f.Id, Name = e.Name, f.Classification, f.ThreatLevel, f.Rating, f.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<FlyoverEntityData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new FlyoverEntityData
            {
                Id             = r.Id.ToString("N"),
                Type           = "flyover_entity",
                Name           = r.Name ?? "",
                Classification = r.Classification ?? "",
                ThreatLevel    = r.ThreatLevel ?? "",
                Rating         = r.Rating,
                VoteCount      = r.VoteCount,
                Tags           = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active FlyoverEntity row + all child collections,
    /// then project to FlyoverEntityData. Records.Json is never read here.
    /// </summary>
    public static List<FlyoverEntityData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "flyover_entity")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "flyover_entity"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var entities = BuildIncludeChain(db.FlyoverEntities.AsNoTracking())
            .Where(f => ids.Contains(f.Id))
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

        var result = new List<FlyoverEntityData>(entities.Count);
        foreach (var f in entities)
        {
            entityById.TryGetValue(f.Id, out var entity);
            tagsByEntity.TryGetValue(f.Id, out var tags);
            result.Add(Materialize(f, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single FlyoverEntity by id. Returns null when not found.</summary>
    public static FlyoverEntityData? LoadOne(ProseDbContext db, Guid id)
    {
        var f = BuildIncludeChain(db.FlyoverEntities.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (f == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(f, entity, tags);
    }

    private static IQueryable<FlyoverEntity> BuildIncludeChain(IQueryable<FlyoverEntity> q)
        => q.AsSplitQuery()
            .Include(f => f.Aliases)
            .Include(f => f.KnownLocations)
            .Include(f => f.StoryHooks);

    /// <summary>
    /// Build a FlyoverEntityData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// </summary>
    public static FlyoverEntityData Materialize(FlyoverEntity f, Entity? entity, List<string>? tags)
    {
        var data = new FlyoverEntityData
        {
            Id                  = f.Id.ToString("N"),
            Type                = "flyover_entity",
            Name                = entity?.Name ?? f.Name,
            Classification      = f.Classification,
            Origin              = f.Origin,
            Substrate           = f.Substrate,
            Territory           = f.Territory,
            PhysicalDescription = f.PhysicalDescription,
            BehavioralProfile   = f.BehavioralProfile,
            ThreatLevel         = f.ThreatLevel,
            HumanRemnants       = f.HumanRemnants,
            GlmzMigrationRisk   = f.GlmzMigrationRisk,
            Rating              = f.Rating,
            VoteCount           = f.VoteCount,
            MidjourneyPrompt    = f.MidjourneyPrompt,
            Dalle3Prompt        = f.Dalle3Prompt,
            Tags                = tags ?? new List<string>(),
        };

        data.Aliases        = f.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.KnownLocations = f.KnownLocations.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks     = f.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a FlyoverEntityData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, FlyoverEntityData src, CancellationToken ct = default)
    {
        var flyover = await db.FlyoverEntities.FirstOrDefaultAsync(f => f.Id == id, ct);
        var isNew = flyover == null;

        if (!isNew)
        {
            await db.FlyoverEntityAliases.Where(x => x.FlyoverEntityId == id).ExecuteDeleteAsync(ct);
            await db.FlyoverEntityKnownLocations.Where(x => x.FlyoverEntityId == id).ExecuteDeleteAsync(ct);
            await db.FlyoverEntityStoryHooks.Where(x => x.FlyoverEntityId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            flyover = new FlyoverEntity { Id = id };
            db.FlyoverEntities.Add(flyover);
        }

        FillScalars(flyover!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on FlyoverEntity from src (no DB touch).</summary>
    public static void FillScalars(FlyoverEntity f, FlyoverEntityData src)
    {
        f.Name                = src.Name ?? "";
        f.Classification      = src.Classification ?? "";
        f.Origin              = src.Origin ?? "";
        f.Substrate           = src.Substrate ?? "";
        f.Territory           = src.Territory ?? "";
        f.PhysicalDescription = src.PhysicalDescription ?? "";
        f.BehavioralProfile   = src.BehavioralProfile ?? "";
        f.ThreatLevel         = src.ThreatLevel ?? "";
        f.HumanRemnants       = src.HumanRemnants ?? "";
        f.GlmzMigrationRisk   = src.GlmzMigrationRisk ?? "";
        f.Rating              = src.Rating;
        f.VoteCount           = src.VoteCount;
        f.MidjourneyPrompt    = src.MidjourneyPrompt ?? "";
        f.Dalle3Prompt        = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, FlyoverEntityData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.FlyoverEntityAliases.Add(new FlyoverEntityAlias { FlyoverEntityId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.KnownLocations.Count; i++)
        {
            var placeId = ResolveEntityId(db, "place", src.KnownLocations[i]);
            db.FlyoverEntityKnownLocations.Add(new FlyoverEntityKnownLocation
            {
                FlyoverEntityId = id,
                Position        = i,
                Alias           = src.KnownLocations[i] ?? "",
                PlaceId         = placeId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.FlyoverEntityStoryHooks.Add(new FlyoverEntityStoryHook { FlyoverEntityId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active flyover_entity Entity, deserialize its Records.Json
    /// blob → FlyoverEntityData → persist. Also creates a minimal relational row for
    /// any active flyover_entity that has no blob and no relational row yet.
    /// Returns the number of flyover entity entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-flyover-entity-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var flyoverEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "flyover_entity")
            .Select(e => e.Id)
            .ToHashSet();

        if (flyoverEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => flyoverEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        // Backfill from blobs
        foreach (var row in blobRows)
        {
            FlyoverEntityData? src;
            try { src = JsonSerializer.Deserialize<FlyoverEntityData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "FlyoverEntityMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "FlyoverEntityMapper.RebuildAllAsync: failed to persist flyover entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free entities that have no relational row yet
        var existingRelationalIds = db.FlyoverEntities.AsNoTracking()
            .Where(f => flyoverEntityIds.Contains(f.Id))
            .Select(f => f.Id)
            .ToHashSet();

        foreach (var entityId in flyoverEntityIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new FlyoverEntityData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "FlyoverEntityMapper.RebuildAllAsync: failed to persist minimal row for flyover entity {Id}", entityId);
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
