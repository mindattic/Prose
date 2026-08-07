using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (LabSpecimens + 3 child
/// tables) and the domain model (LabSpecimenData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — LabSpecimenRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Tags live in the universal EntityTags layer.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class LabSpecimenMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Classification,
    /// ThreatLevel, Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<LabSpecimenData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.LabSpecimens.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "lab_specimen"),
                s => s.Id, e => e.Id,
                (s, e) => new { s.Id, Name = e.Name, s.Classification, s.ThreatLevel, s.Rating, s.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<LabSpecimenData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new LabSpecimenData
            {
                Id             = r.Id.ToString("N"),
                Type           = "lab_specimen",
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
    /// Full eager load of every active LabSpecimen row + all child collections,
    /// then project to LabSpecimenData. Records.Json is never read here.
    /// </summary>
    public static List<LabSpecimenData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "lab_specimen")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "lab_specimen" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var specimens = BuildIncludeChain(db.LabSpecimens.AsNoTracking())
            .Where(s => ids.Contains(s.Id))
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

        var result = new List<LabSpecimenData>(specimens.Count);
        foreach (var s in specimens)
        {
            entityById.TryGetValue(s.Id, out var entity);
            tagsByEntity.TryGetValue(s.Id, out var tags);
            result.Add(Materialize(s, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single LabSpecimen by id. Returns null when not found.</summary>
    public static LabSpecimenData? LoadOne(ProseDbContext db, Guid id)
    {
        var s = BuildIncludeChain(db.LabSpecimens.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (s == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(s, entity, tags);
    }

    private static IQueryable<LabSpecimen> BuildIncludeChain(IQueryable<LabSpecimen> q)
        => q.AsSplitQuery()
            .Include(s => s.Aliases)
            .Include(s => s.KnownLocations)
            .Include(s => s.StoryHooks);

    /// <summary>
    /// Build a LabSpecimenData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name.
    /// </summary>
    public static LabSpecimenData Materialize(LabSpecimen s, Entity? entity, List<string>? tags)
    {
        var data = new LabSpecimenData
        {
            Id                  = s.Id.ToString("N"),
            Type                = "lab_specimen",
            Name                = entity?.Name ?? s.Name,
            Classification      = s.Classification,
            OriginLab           = s.OriginLab,
            OriginMethod        = s.OriginMethod,
            Substrate           = s.Substrate,
            PhysicalDescription = s.PhysicalDescription,
            BehavioralProfile   = s.BehavioralProfile,
            ThreatLevel         = s.ThreatLevel,
            ContainmentStatus   = s.ContainmentStatus,
            ContaminationRisk   = s.ContaminationRisk,
            PacificationProtocol = s.PacificationProtocol,
            PitiableQualities   = s.PitiableQualities,
            Rating              = s.Rating,
            VoteCount           = s.VoteCount,
            MidjourneyPrompt    = s.MidjourneyPrompt,
            Dalle3Prompt        = s.Dalle3Prompt,
            Tags                = tags ?? new List<string>(),
        };

        data.Aliases        = s.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.KnownLocations = s.KnownLocations.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks     = s.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a LabSpecimenData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, LabSpecimenData src, CancellationToken ct = default)
    {
        var specimen = await db.LabSpecimens.FirstOrDefaultAsync(s => s.Id == id, ct);
        var isNew = specimen == null;

        if (!isNew)
        {
            await db.LabSpecimenAliases.Where(x => x.LabSpecimenId == id).ExecuteDeleteAsync(ct);
            await db.LabSpecimenKnownLocations.Where(x => x.LabSpecimenId == id).ExecuteDeleteAsync(ct);
            await db.LabSpecimenStoryHooks.Where(x => x.LabSpecimenId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            specimen = new LabSpecimen { Id = id };
            db.LabSpecimens.Add(specimen);
        }

        FillScalars(specimen!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on LabSpecimen from src (no DB touch).</summary>
    public static void FillScalars(LabSpecimen s, LabSpecimenData src)
    {
        s.Name                = src.Name ?? "";
        s.Classification      = src.Classification ?? "";
        s.OriginLab           = src.OriginLab ?? "";
        s.OriginMethod        = src.OriginMethod ?? "";
        s.Substrate           = src.Substrate ?? "";
        s.PhysicalDescription = src.PhysicalDescription ?? "";
        s.BehavioralProfile   = src.BehavioralProfile ?? "";
        s.ThreatLevel         = src.ThreatLevel ?? "";
        s.ContainmentStatus   = src.ContainmentStatus ?? "";
        s.ContaminationRisk   = src.ContaminationRisk ?? "";
        s.PacificationProtocol = src.PacificationProtocol ?? "";
        s.PitiableQualities   = src.PitiableQualities ?? "";
        s.Rating              = src.Rating;
        s.VoteCount           = src.VoteCount;
        s.MidjourneyPrompt    = src.MidjourneyPrompt ?? "";
        s.Dalle3Prompt        = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, LabSpecimenData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.LabSpecimenAliases.Add(new LabSpecimenAlias { LabSpecimenId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.KnownLocations.Count; i++)
        {
            var placeId = ResolveEntityId(db, "place", src.KnownLocations[i]);
            db.LabSpecimenKnownLocations.Add(new LabSpecimenKnownLocation
            {
                LabSpecimenId = id,
                Position      = i,
                Alias         = src.KnownLocations[i] ?? "",
                PlaceId       = placeId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.LabSpecimenStoryHooks.Add(new LabSpecimenStoryHook { LabSpecimenId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active lab_specimen Entity, deserialize its Records.Json
    /// blob → LabSpecimenData → persist. Also creates a minimal relational row for
    /// any active lab_specimen entity that has no blob and no relational row yet.
    /// Returns the number of lab specimen entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-lab-specimen-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        // Include INACTIVE entities too: every blob-bearing entity must get a relational row so
        // the Records blob can be retired without losing archived/soft-deleted canon (RFC 0007 gate).
        var specimenEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "lab_specimen")
            .Select(e => e.Id)
            .ToHashSet();

        if (specimenEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => specimenEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        // Backfill from blobs
        foreach (var row in blobRows)
        {
            LabSpecimenData? src;
            try { src = JsonSerializer.Deserialize<LabSpecimenData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "LabSpecimenMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "LabSpecimenMapper.RebuildAllAsync: failed to persist lab specimen {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free entities that have no relational row yet
        var existingRelationalIds = db.LabSpecimens.AsNoTracking()
            .Where(s => specimenEntityIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToHashSet();

        foreach (var entityId in specimenEntityIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new LabSpecimenData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "LabSpecimenMapper.RebuildAllAsync: failed to persist minimal row for lab specimen {Id}", entityId);
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
            .Where(e => e.EntityType == entityType && e.IsActive
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }
}
