using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Automata + 5 child
/// tables) and the domain model (AutomatonData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — AutomatonRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Tags live in the universal EntityTags layer.
///
/// Column note: domain TierAvailability maps to Automata.Tier column.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class AutomatonMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Classification,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<AutomatonData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Automata.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "automaton"),
                a => a.Id, e => e.Id,
                (a, e) => new { a.Id, Name = e.Name, a.Classification, a.Rating, a.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<AutomatonData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new AutomatonData
            {
                Id             = r.Id.ToString("N"),
                Type           = "automaton",
                Name           = r.Name ?? "",
                Classification = r.Classification ?? "",
                Rating         = r.Rating,
                VoteCount      = r.VoteCount,
                Tags           = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Automaton row + all child collections,
    /// then project to AutomatonData. Records.Json is never read here.
    /// </summary>
    public static List<AutomatonData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "automaton")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "automaton"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var automata = BuildIncludeChain(db.Automata.AsNoTracking())
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

        var result = new List<AutomatonData>(automata.Count);
        foreach (var a in automata)
        {
            entityById.TryGetValue(a.Id, out var entity);
            tagsByEntity.TryGetValue(a.Id, out var tags);
            result.Add(Materialize(a, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Automaton by id. Returns null when not found.</summary>
    public static AutomatonData? LoadOne(ProseDbContext db, Guid id)
    {
        var a = BuildIncludeChain(db.Automata.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (a == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(a, entity, tags);
    }

    private static IQueryable<Automaton> BuildIncludeChain(IQueryable<Automaton> q)
        => q.AsSplitQuery()
            .Include(a => a.Aliases)
            .Include(a => a.Armament)
            .Include(a => a.Sensors)
            .Include(a => a.KnownDeployments)
            .Include(a => a.StoryHooks);

    /// <summary>
    /// Build an AutomatonData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// Note: Automata.Tier maps to domain TierAvailability.
    /// </summary>
    public static AutomatonData Materialize(Automaton a, Entity? entity, List<string>? tags)
    {
        var data = new AutomatonData
        {
            Id               = a.Id.ToString("N"),
            Type             = "automaton",
            Name             = entity?.Name ?? a.Name,
            Classification   = a.Classification,
            Manufacturer     = a.Manufacturer,
            Description      = a.Description,
            TierAvailability = a.Tier,
            Legality         = a.Legality,
            AutonomyLevel    = a.AutonomyLevel,
            Dimensions       = a.Dimensions,
            Weight           = a.Weight,
            PowerSource      = a.PowerSource,
            Locomotion       = a.Locomotion,
            Countermeasures  = a.Countermeasures,
            CulturalContext  = a.CulturalContext,
            Rating           = a.Rating,
            VoteCount        = a.VoteCount,
            MidjourneyPrompt = a.MidjourneyPrompt,
            Dalle3Prompt     = a.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases          = a.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.Armament         = a.Armament.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.Sensors          = a.Sensors.OrderBy(x => x.Position).Select(x => x.SensorName).ToList();
        data.KnownDeployments = a.KnownDeployments.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks       = a.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert an AutomatonData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, AutomatonData src, CancellationToken ct = default)
    {
        var automaton = await db.Automata.FirstOrDefaultAsync(a => a.Id == id, ct);
        var isNew = automaton == null;

        if (!isNew)
        {
            await db.AutomatonAliases.Where(x => x.AutomatonId == id).ExecuteDeleteAsync(ct);
            await db.AutomatonArmament.Where(x => x.AutomatonId == id).ExecuteDeleteAsync(ct);
            await db.AutomatonSensors.Where(x => x.AutomatonId == id).ExecuteDeleteAsync(ct);
            await db.AutomatonDeployments.Where(x => x.AutomatonId == id).ExecuteDeleteAsync(ct);
            await db.AutomatonStoryHooks.Where(x => x.AutomatonId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            automaton = new Automaton { Id = id };
            db.Automata.Add(automaton);
        }

        FillScalars(automaton!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Automaton from src (no DB touch).</summary>
    public static void FillScalars(Automaton a, AutomatonData src)
    {
        a.Name            = src.Name ?? "";
        a.Classification  = src.Classification ?? "";
        a.Manufacturer    = src.Manufacturer ?? "";
        a.Description     = src.Description ?? "";
        a.Tier            = src.TierAvailability ?? "";
        a.Legality        = src.Legality ?? "";
        a.AutonomyLevel   = src.AutonomyLevel ?? "";
        a.Dimensions      = src.Dimensions ?? "";
        a.Weight          = src.Weight ?? "";
        a.PowerSource     = src.PowerSource ?? "";
        a.Locomotion      = src.Locomotion ?? "";
        a.Countermeasures = src.Countermeasures ?? "";
        a.CulturalContext = src.CulturalContext ?? "";
        a.Rating          = src.Rating;
        a.VoteCount       = src.VoteCount;
        a.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        a.Dalle3Prompt    = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, AutomatonData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.AutomatonAliases.Add(new AutomatonAlias { AutomatonId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.Armament.Count; i++)
        {
            var weaponId = ResolveEntityId(db, "weapon", src.Armament[i]);
            db.AutomatonArmament.Add(new AutomatonArmament
            {
                AutomatonId = id,
                Position    = i,
                Alias       = src.Armament[i] ?? "",
                WeaponId    = weaponId,
            });
        }

        for (int i = 0; i < src.Sensors.Count; i++)
            db.AutomatonSensors.Add(new AutomatonSensor { AutomatonId = id, Position = i, SensorName = src.Sensors[i] ?? "" });

        for (int i = 0; i < src.KnownDeployments.Count; i++)
        {
            var deployId = ResolveEntityId(db, "place", src.KnownDeployments[i])
                        ?? ResolveEntityId(db, "faction", src.KnownDeployments[i])
                        ?? ResolveEntityId(db, "corponation", src.KnownDeployments[i]);
            db.AutomatonDeployments.Add(new AutomatonDeployment
            {
                AutomatonId        = id,
                Position           = i,
                Alias              = src.KnownDeployments[i] ?? "",
                DeploymentEntityId = deployId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.AutomatonStoryHooks.Add(new AutomatonStoryHook { AutomatonId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active automaton Entity, deserialize its Records.Json
    /// blob → AutomatonData → persist. Also creates a minimal relational row for
    /// any active automaton entity that has no blob and no relational row yet.
    /// Returns the number of automaton entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-automaton-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var automatonEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "automaton")
            .Select(e => e.Id)
            .ToHashSet();

        if (automatonEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => automatonEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        // Backfill from blobs
        foreach (var row in blobRows)
        {
            AutomatonData? src;
            try { src = JsonSerializer.Deserialize<AutomatonData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "AutomatonMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "AutomatonMapper.RebuildAllAsync: failed to persist automaton {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free entities that have no relational row yet
        var existingRelationalIds = db.Automata.AsNoTracking()
            .Where(a => automatonEntityIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToHashSet();

        foreach (var entityId in automatonEntityIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new AutomatonData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "AutomatonMapper.RebuildAllAsync: failed to persist minimal row for automaton {Id}", entityId);
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
        var slug = Prose.Core.Services.UniverseGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }
}
