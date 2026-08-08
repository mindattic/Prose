using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Corponations +
/// CorponationCommonNames bridge) and the domain model (CorponationData).
///
/// Column note: domain TierAvailability does not exist — Corponation uses Tier
/// directly for the classification tier. Headquarters/Sector are extra scalars
/// with no domain equivalent — they are populated from Name+blob fallbacks.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class CorponationMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Sector, Tier,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<CorponationData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Corponations.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "corponation"),
                c => c.Id, e => e.Id,
                (c, e) => new { c.Id, Name = e.Name, c.Sector, c.Tier, c.Rating, c.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<CorponationData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new CorponationData
            {
                Id        = r.Id.ToString("N"),
                Name      = r.Name ?? "",
                Sector    = r.Sector ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Corponation row + all child collections,
    /// then project to CorponationData. Records.Json is never read here.
    /// </summary>
    public static List<CorponationData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "corponation")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "corponation" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var corponations = BuildIncludeChain(db.Corponations.AsNoTracking())
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

        var result = new List<CorponationData>(corponations.Count);
        foreach (var c in corponations)
        {
            entityById.TryGetValue(c.Id, out var entity);
            tagsByEntity.TryGetValue(c.Id, out var tags);
            result.Add(Materialize(c, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Corponation by id. Returns null when not found.</summary>
    public static CorponationData? LoadOne(ProseDbContext db, Guid id)
    {
        var c = BuildIncludeChain(db.Corponations.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (c == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(c, entity, tags);
    }

    private static IQueryable<Corponation> BuildIncludeChain(IQueryable<Corponation> q)
        => q.AsSplitQuery()
            .Include(c => c.CommonNames);

    /// <summary>
    /// Build a CorponationData from the EF entity + bridges loaded by BuildIncludeChain.
    /// Entity spine is used for the universal Name.
    /// </summary>
    public static CorponationData Materialize(Corponation c, Entity? entity, List<string>? tags)
    {
        var data = new CorponationData
        {
            Id                  = c.Id.ToString("N"),
            Name                = entity?.Name ?? c.Name,
            Number              = c.Number,
            FullLegalName       = c.FullLegalName,
            StockDesignation    = c.StockDesignation,
            Sector              = c.Sector,
            Valuation           = c.Valuation,
            Revenue             = c.Revenue,
            Employees           = c.Employees,
            SovereignTerritory  = c.SovereignTerritory,
            FoundingStory       = c.FoundingStory,
            SecurityForce       = c.SecurityForce,
            KeyDetail           = c.KeyDetail,
            RelationshipToBig20 = c.RelationshipToBig20,
            FullText            = c.FullText,
            Rating              = c.Rating,
            VoteCount           = c.VoteCount,
            MidjourneyPrompt    = c.MidjourneyPrompt,
            Dalle3Prompt        = c.Dalle3Prompt,
            Tags                = tags ?? new List<string>(),
        };

        data.CommonNames = c.CommonNames.OrderBy(x => x.Position).Select(x => x.Value).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a CorponationData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, CorponationData src, CancellationToken ct = default)
    {
        var corp = await db.Corponations.FirstOrDefaultAsync(c => c.Id == id, ct);
        var isNew = corp == null;

        if (!isNew)
        {
            await db.CorponationCommonNames.Where(x => x.CorponationId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            corp = new Corponation { Id = id };
            db.Corponations.Add(corp);
        }

        FillScalars(corp!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Corponation from src (no DB touch).</summary>
    public static void FillScalars(Corponation c, CorponationData src)
    {
        c.Name                = src.Name ?? "";
        c.Sector              = src.Sector ?? "";
        c.Tier                = "";               // not in CorponationData; keep blank
        c.Headquarters        = "";               // not in CorponationData; keep blank
        c.Number              = src.Number;
        c.Rating              = src.Rating;
        c.VoteCount           = src.VoteCount;
        c.FullLegalName       = src.FullLegalName ?? "";
        c.StockDesignation    = src.StockDesignation ?? "";
        c.Valuation           = src.Valuation ?? "";
        c.Revenue             = src.Revenue ?? "";
        c.Employees           = src.Employees ?? "";
        c.SovereignTerritory  = src.SovereignTerritory ?? "";
        c.FoundingStory       = src.FoundingStory ?? "";
        c.SecurityForce       = src.SecurityForce ?? "";
        c.KeyDetail           = src.KeyDetail ?? "";
        c.RelationshipToBig20 = src.RelationshipToBig20 ?? "";
        c.FullText            = src.FullText ?? "";
        c.MidjourneyPrompt    = src.MidjourneyPrompt ?? "";
        c.Dalle3Prompt        = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, CorponationData src)
    {
        for (int i = 0; i < src.CommonNames.Count; i++)
            db.CorponationCommonNames.Add(new CorponationCommonName
            {
                CorponationId = id,
                Position      = i,
                Value         = src.CommonNames[i] ?? "",
            });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every corponation Entity (active or inactive), deserialize
    /// its Records.Json blob → CorponationData → persist. Also creates a minimal
    /// relational row for any active corponation entity that has no blob and no
    /// relational row yet. Returns the number of corponation entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-corponation-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        // Backfill ALL entities (active + inactive) that have blobs.
        var corpEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "corponation")
            .Select(e => e.Id)
            .ToHashSet();

        if (corpEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => corpEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        int written = 0;

        // Backfill from blobs (active + inactive).
        foreach (var row in blobRows)
        {
            CorponationData? src;
            try { src = JsonSerializer.Deserialize<CorponationData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "CorponationMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "CorponationMapper.RebuildAllAsync: failed to persist corponation {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for blob-free ACTIVE entities that have no relational row yet.
        var activeCorpIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "corponation" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        var existingRelationalIds = db.Corponations.AsNoTracking()
            .Where(c => activeCorpIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSet();

        foreach (var entityId in activeCorpIds.Except(blobEntityIds).Except(existingRelationalIds))
        {
            var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == entityId);
            if (entity == null) continue;
            try
            {
                var minimal = new CorponationData { Id = entityId.ToString("N"), Name = entity.Name ?? "" };
                await PersistAsync(db, entityId, minimal, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "CorponationMapper.RebuildAllAsync: failed to persist minimal row for corponation {Id}", entityId);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }
}
