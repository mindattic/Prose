using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Psionics + 3 child
/// tables) and the domain model (PsionicData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — PsionicRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Psionic-level tags live in the
/// universal EntityTags layer.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class PsionicMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Classification,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<PsionicData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Psionics.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "psionic"),
                p => p.Id, e => e.Id,
                (p, e) => new { p.Id, Name = e.Name, p.Classification, p.Rating, p.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<PsionicData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new PsionicData
            {
                Id             = r.Id.ToString("N"),
                Type           = "psionic",
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
    /// Full eager load of every active Psionic row + all child collections,
    /// then project to PsionicData. Records.Json is never read here.
    /// </summary>
    public static List<PsionicData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "psionic")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "psionic"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var psionics = BuildIncludeChain(db.Psionics.AsNoTracking())
            .Where(p => ids.Contains(p.Id))
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

        var result = new List<PsionicData>(psionics.Count);
        foreach (var p in psionics)
        {
            entityById.TryGetValue(p.Id, out var entity);
            tagsByEntity.TryGetValue(p.Id, out var tags);
            result.Add(Materialize(p, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Psionic by id. Returns null when not found.</summary>
    public static PsionicData? LoadOne(ProseDbContext db, Guid id)
    {
        var p = BuildIncludeChain(db.Psionics.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (p == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(p, entity, tags);
    }

    private static IQueryable<Psionic> BuildIncludeChain(IQueryable<Psionic> q)
        => q.AsSplitQuery()
            .Include(p => p.Aliases)
            .Include(p => p.KnownPractitioners)
            .Include(p => p.StoryHooks);

    /// <summary>
    /// Build a PsionicData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name.
    /// </summary>
    public static PsionicData Materialize(Psionic p, Entity? entity, List<string>? tags)
    {
        var data = new PsionicData
        {
            Id                = p.Id.ToString("N"),
            Type              = "psionic",
            Name              = entity?.Name ?? p.Name,
            Classification    = p.Classification,
            EnhancementType   = p.EnhancementType,
            Mechanism         = p.Mechanism,
            Abilities         = p.Abilities,
            SideEffects       = p.SideEffects,
            AcquisitionMethod = p.AcquisitionMethod,
            DetectionRisk     = p.DetectionRisk,
            CorporateInterest = p.CorporateInterest,
            Rating            = p.Rating,
            VoteCount         = p.VoteCount,
            MidjourneyPrompt  = p.MidjourneyPrompt,
            Dalle3Prompt      = p.Dalle3Prompt,
            Tags              = tags ?? new List<string>(),
        };

        data.Aliases            = p.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.KnownPractitioners = p.KnownPractitioners.OrderBy(x => x.Position).Select(x => x.Alias).ToList();
        data.StoryHooks         = p.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a PsionicData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, PsionicData src, CancellationToken ct = default)
    {
        var psionic = await db.Psionics.FirstOrDefaultAsync(p => p.Id == id, ct);
        var isNew = psionic == null;

        if (!isNew)
        {
            await db.PsionicAliases.Where(x => x.PsionicId == id).ExecuteDeleteAsync(ct);
            await db.PsionicKnownPractitioners.Where(x => x.PsionicId == id).ExecuteDeleteAsync(ct);
            await db.PsionicStoryHooks.Where(x => x.PsionicId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            psionic = new Psionic { Id = id };
            db.Psionics.Add(psionic);
        }

        FillScalars(psionic!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Psionic from src (no DB touch).</summary>
    public static void FillScalars(Psionic p, PsionicData src)
    {
        p.Name              = src.Name ?? "";
        p.Classification    = src.Classification ?? "";
        p.EnhancementType   = src.EnhancementType ?? "";
        p.Mechanism         = src.Mechanism ?? "";
        p.Abilities         = src.Abilities ?? "";
        p.SideEffects       = src.SideEffects ?? "";
        p.AcquisitionMethod = src.AcquisitionMethod ?? "";
        p.DetectionRisk     = src.DetectionRisk ?? "";
        p.CorporateInterest = src.CorporateInterest ?? "";
        p.Rating            = src.Rating;
        p.VoteCount         = src.VoteCount;
        p.MidjourneyPrompt  = src.MidjourneyPrompt ?? "";
        p.Dalle3Prompt      = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, PsionicData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.PsionicAliases.Add(new PsionicAlias { PsionicId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.KnownPractitioners.Count; i++)
        {
            var charId = ResolveEntityId(db, "character", src.KnownPractitioners[i]);
            db.PsionicKnownPractitioners.Add(new PsionicKnownPractitioner
            {
                PsionicId   = id,
                Position    = i,
                Alias       = src.KnownPractitioners[i] ?? "",
                CharacterId = charId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.PsionicStoryHooks.Add(new PsionicStoryHook { PsionicId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active psionic Entity, deserialize its Records.Json
    /// blob → PsionicData → persist. Also creates a minimal relational row for
    /// any active psionic entity that has no blob and no relational row yet.
    /// Returns the number of psionic entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-psionic-relational</c>.
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
        var psionicEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "psionic")
            .Select(e => e.Id)
            .ToHashSet();

        if (psionicEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => psionicEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        // Backfill from blobs
        foreach (var row in blobRows)
        {
            PsionicData? src;
            try { src = JsonSerializer.Deserialize<PsionicData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "PsionicMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "PsionicMapper.RebuildAllAsync: failed to persist psionic {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free entities that have no relational row yet
        var existingRelationalIds = db.Psionics.AsNoTracking()
            .Where(p => psionicEntityIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSet();

        foreach (var entityId in psionicEntityIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new PsionicData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "PsionicMapper.RebuildAllAsync: failed to persist minimal row for psionic {Id}", entityId);
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
