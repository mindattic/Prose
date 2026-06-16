using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Genemod + 3 child
/// tables) and the domain model (GenemodData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — GenemodRepository delegates
/// to it so the mapping never drifts between import and read/write paths.
///
/// Reads use a single root query plus eager Includes (AsSplitQuery). Writes
/// wipe the bridge rows by FK and re-insert. Genemod-level tags live in the
/// universal EntityTags layer.
///
/// All GenemodData fields are fully covered by either a scalar column or a
/// bridge table (GenemodSideEffects). No fields remain blob-only.
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class GenemodMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Rating,
    /// VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<GenemodData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Genemods.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "genemod"),
                g => g.Id, e => e.Id,
                (g, e) => new { g.Id, Name = e.Name, g.Category, g.Rating, g.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<GenemodData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new GenemodData
            {
                Id        = r.Id.ToString("N"),
                Type      = "genemods",
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
    /// Full eager load of every active Genemod row + all child collections,
    /// then project to GenemodData. Records.Json is never read here.
    /// </summary>
    public static List<GenemodData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "genemod")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "genemod" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var genemods = BuildIncludeChain(db.Genemods.AsNoTracking())
            .Where(g => ids.Contains(g.Id))
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

        var result = new List<GenemodData>(genemods.Count);
        foreach (var g in genemods)
        {
            entityById.TryGetValue(g.Id, out var entity);
            tagsByEntity.TryGetValue(g.Id, out var tags);
            result.Add(Materialize(g, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single Genemod by id. Returns null when not found.</summary>
    public static GenemodData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var g = BuildIncludeChain(db.Genemods.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (g == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(g, entity, tags);
    }

    private static IQueryable<Genemod> BuildIncludeChain(IQueryable<Genemod> q)
        => q.AsSplitQuery()
            .Include(g => g.Aliases)
            .Include(g => g.SideEffects)
            .Include(g => g.StoryHooks);

    /// <summary>
    /// Build a GenemodData from the entity + bridges loaded by BuildIncludeChain.
    /// Entity is used for the universal Name. All GenemodData fields are
    /// covered by scalar columns or bridge tables — nothing remains blob-only.
    /// </summary>
    public static GenemodData Materialize(Genemod g, Entity? entity, List<string>? tags)
    {
        var data = new GenemodData
        {
            Id               = g.Id.ToString("N"),
            Type             = "genemods",
            Name             = entity?.Name ?? g.Name,
            BrandName        = g.BrandName,
            ProductName      = g.ProductName,
            Manufacturer     = g.Manufacturer,
            Category         = g.Category,
            TargetSystem     = g.TargetSystem,
            SourceOrganism   = g.SourceOrganism,
            Legality         = g.Legality,
            Procedure        = g.Procedure,
            ExpressionTime   = g.ExpressionTime,
            Reversibility    = g.Reversibility,
            SocialPerception = g.SocialPerception,
            TierAvailability = g.TierAvailability,
            Description      = g.Description,
            Rating           = g.Rating,
            VoteCount        = g.VoteCount,
            MidjourneyPrompt = g.MidjourneyPrompt,
            Dalle3Prompt     = g.Dalle3Prompt,
            Tags             = tags ?? new List<string>(),
        };

        data.Aliases     = g.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.SideEffects = g.SideEffects.OrderBy(x => x.Position).Select(x => x.Effect).ToList();
        data.StoryHooks  = g.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a GenemodData into the relational schema. Existing bridge rows
    /// are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, GenemodData src, CancellationToken ct = default)
    {
        var genemod = await db.Genemods.FirstOrDefaultAsync(g => g.Id == id, ct);
        var isNew = genemod == null;

        if (!isNew)
        {
            await db.GenemodAliases.Where(x => x.GenemodId == id).ExecuteDeleteAsync(ct);
            await db.GenemodSideEffects.Where(x => x.GenemodId == id).ExecuteDeleteAsync(ct);
            await db.GenemodStoryHooks.Where(x => x.GenemodId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            genemod = new Genemod { Id = id };
            db.Genemods.Add(genemod);
        }

        FillScalars(genemod!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Genemod from src (no DB touch).</summary>
    public static void FillScalars(Genemod g, GenemodData src)
    {
        g.Name             = src.Name ?? "";
        g.BrandName        = src.BrandName ?? "";
        g.ProductName      = src.ProductName ?? "";
        g.Manufacturer     = src.Manufacturer ?? "";
        g.Category         = src.Category ?? "";
        g.TargetSystem     = src.TargetSystem ?? "";
        g.SourceOrganism   = src.SourceOrganism ?? "";
        g.Legality         = src.Legality ?? "";
        g.Procedure        = src.Procedure ?? "";
        g.ExpressionTime   = src.ExpressionTime ?? "";
        g.Reversibility    = src.Reversibility ?? "";
        g.SocialPerception = src.SocialPerception ?? "";
        g.TierAvailability = src.TierAvailability ?? "";
        g.Description      = src.Description ?? "";
        g.Rating           = src.Rating;
        g.VoteCount        = src.VoteCount;
        g.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        g.Dalle3Prompt     = src.Dalle3Prompt ?? "";
        // Tier is a classification column that GenemodData has no corresponding
        // column for (it uses TierAvailability as a prose string); leave unchanged.
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, GenemodData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.GenemodAliases.Add(new GenemodAlias { GenemodId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.SideEffects.Count; i++)
            db.GenemodSideEffects.Add(new GenemodSideEffect { GenemodId = id, Position = i, Effect = src.SideEffects[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.GenemodStoryHooks.Add(new GenemodStoryHook { GenemodId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active genemod Entity, deserialize its Records.Json
    /// blob → GenemodData → persist. Returns the number of genemods written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>ss --rebuild-genemod-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(StreetSamuraiDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var genemodEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "genemod" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (genemodEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => genemodEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            GenemodData? src;
            try { src = JsonSerializer.Deserialize<GenemodData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "GenemodMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "GenemodMapper.RebuildAllAsync: failed to persist genemod {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }
}
