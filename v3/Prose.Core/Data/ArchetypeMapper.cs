using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (ArchetypeRow + 5 child
/// tables) and the domain model (ArchetypeData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — ArchetypeRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Archetype-level tags live in the
/// universal EntityTags layer (same as FactionMapper).
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here. The blob
/// column carries the data until the human retires it after a parity gate.
/// </summary>
public static class ArchetypeMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns one <see cref="ArchetypeData"/>
    /// per active archetype with only Id, Name, Category, Tags. No Includes,
    /// no bridge materialization.
    /// </summary>
    public static List<ArchetypeData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Archetypes.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "archetype"),
                a => a.Id, e => e.Id,
                (a, e) => new { a.Id, Name = e.Name, a.Category })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<ArchetypeData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new ArchetypeData
            {
                Id       = r.Id.ToString("N"),
                Type     = "archetype",
                Name     = r.Name ?? "",
                Category = r.Category ?? "",
                Tags     = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active ArchetypeRow + all child collections,
    /// then project to ArchetypeData. Records.Json is never read here.
    /// </summary>
    public static List<ArchetypeData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "archetype")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "archetype"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var archetypes = BuildIncludeChain(db.Archetypes.AsNoTracking())
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

        var result = new List<ArchetypeData>(archetypes.Count);
        foreach (var a in archetypes)
        {
            entityById.TryGetValue(a.Id, out var entity);
            tagsByEntity.TryGetValue(a.Id, out var tags);
            result.Add(Materialize(a, entity, tags));
        }
        return result;
    }

    /// <summary>
    /// Load a single archetype by id, including all bridges. Returns null when not found.
    /// </summary>
    public static ArchetypeData? LoadOne(ProseDbContext db, Guid id)
    {
        var a = BuildIncludeChain(db.Archetypes.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (a == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(a, entity, tags);
    }

    private static IQueryable<ArchetypeRow> BuildIncludeChain(IQueryable<ArchetypeRow> q)
        => q.AsSplitQuery()
            .Include(a => a.WillAlways)
            .Include(a => a.WillNever)
            .Include(a => a.Unless)
            .Include(a => a.SimilarTo)
            .Include(a => a.OppositeOf);

    /// <summary>
    /// Build an ArchetypeData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name; every other field comes from the
    /// columnar ArchetypeRow.
    /// </summary>
    public static ArchetypeData Materialize(ArchetypeRow a, Entity? entity, List<string>? tags)
    {
        var data = new ArchetypeData
        {
            Id                 = a.Id.ToString("N"),
            Type               = "archetype",
            Name               = entity?.Name ?? a.Name,
            Category           = a.Category,
            Description        = a.Description,
            BehavioralSignature = a.BehavioralSignature,
            UnderStress        = a.UnderStress,
            AtRest             = a.AtRest,
            Tags               = tags ?? new List<string>(),
        };

        data.WillAlways  = a.WillAlways.OrderBy(x => x.Position).Select(x => x.Rule).ToList();
        data.WillNever   = a.WillNever.OrderBy(x => x.Position).Select(x => x.Rule).ToList();
        data.Unless      = a.Unless.OrderBy(x => x.Position).Select(x => x.Condition).ToList();

        data.SimilarTo = a.SimilarTo.OrderBy(x => x.Position).Select(s => new ArchetypeSimilarity
        {
            Archetype = s.Alias,
            Threshold = s.Threshold,
            Context   = s.Context,
        }).ToList();

        data.OppositeOf = a.OppositeOf.OrderBy(x => x.Position).Select(x => x.Alias).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert an ArchetypeData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, ArchetypeData src, CancellationToken ct = default)
    {
        var archetype = await db.Archetypes.FirstOrDefaultAsync(a => a.Id == id, ct);
        var isNew = archetype == null;

        if (!isNew)
        {
            // Wipe all bridges — cascade deletes handle children.
            await db.ArchetypeWillAlways.Where(x => x.ArchetypeId == id).ExecuteDeleteAsync(ct);
            await db.ArchetypeWillNever.Where(x => x.ArchetypeId == id).ExecuteDeleteAsync(ct);
            await db.ArchetypeUnless.Where(x => x.ArchetypeId == id).ExecuteDeleteAsync(ct);
            await db.ArchetypeSimilars.Where(x => x.ArchetypeId == id).ExecuteDeleteAsync(ct);
            await db.ArchetypeOpposites.Where(x => x.ArchetypeId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            archetype = new ArchetypeRow { Id = id };
            db.Archetypes.Add(archetype);
        }

        FillScalars(archetype!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on ArchetypeRow from src (no DB touch).</summary>
    public static void FillScalars(ArchetypeRow a, ArchetypeData src)
    {
        a.Name               = src.Name ?? "";
        a.Category           = src.Category ?? "";
        a.Description        = src.Description ?? "";
        a.BehavioralSignature = src.BehavioralSignature ?? "";
        a.UnderStress        = src.UnderStress ?? "";
        a.AtRest             = src.AtRest ?? "";
        // Family is a classification column that ArchetypeData has no field for;
        // leave it unchanged on update so it isn't clobbered by a round-trip.
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, ArchetypeData src)
    {
        for (int i = 0; i < src.WillAlways.Count; i++)
            db.ArchetypeWillAlways.Add(new ArchetypeWillAlways { ArchetypeId = id, Position = i, Rule = src.WillAlways[i] ?? "" });

        for (int i = 0; i < src.WillNever.Count; i++)
            db.ArchetypeWillNever.Add(new ArchetypeWillNever { ArchetypeId = id, Position = i, Rule = src.WillNever[i] ?? "" });

        for (int i = 0; i < src.Unless.Count; i++)
            db.ArchetypeUnless.Add(new ArchetypeUnless { ArchetypeId = id, Position = i, Condition = src.Unless[i] ?? "" });

        for (int i = 0; i < src.SimilarTo.Count; i++)
        {
            var s = src.SimilarTo[i];
            var targetId = ResolveEntityId(db, "archetype", s.Archetype);
            db.ArchetypeSimilars.Add(new ArchetypeSimilar
            {
                ArchetypeId       = id,
                Position          = i,
                Alias             = s.Archetype ?? "",
                Threshold         = s.Threshold,
                Context           = s.Context ?? "",
                SimilarArchetypeId = targetId,
            });
        }

        for (int i = 0; i < src.OppositeOf.Count; i++)
        {
            var targetId = ResolveEntityId(db, "archetype", src.OppositeOf[i]);
            db.ArchetypeOpposites.Add(new ArchetypeOpposite
            {
                ArchetypeId        = id,
                Position           = i,
                Alias              = src.OppositeOf[i] ?? "",
                OppositeArchetypeId = targetId,
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active archetype Entity, deserialize its Records.Json
    /// blob → ArchetypeData → persist via FillScalars + FillBridges + sync
    /// EntityTags. Returns the number of archetypes written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-archetype-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var archetypeEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "archetype")
            .Select(e => e.Id)
            .ToHashSet();

        if (archetypeEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => archetypeEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            ArchetypeData? src;
            try { src = JsonSerializer.Deserialize<ArchetypeData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "ArchetypeMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "ArchetypeMapper.RebuildAllAsync: failed to persist archetype {Id}", row.EntityId);
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
