using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (SyntheticLives + 3 child
/// tables) and the domain model (SyntheticLifeData).
///
/// Bridges: SyntheticLifeAliases, SyntheticLifeKnownAssociations, SyntheticLifeStoryHooks.
///
/// Column mapping:
///   domain KindOfBeing  → DB column KindOfBeing
///   domain LifeStatus   → DB column LifeStatus
///   domain Tier         → DB column Tier
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class SyntheticMapper
{
    // ─────────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Classification,
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<SyntheticLifeData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.SyntheticLives.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.EntityType == "synthetic"),
                s => s.Id, e => e.Id,
                (s, e) => new { s.Id, s.Name, s.Classification, s.Rating, s.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<SyntheticLifeData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new SyntheticLifeData
            {
                Id             = r.Id.ToString("N"),
                Type           = "synthetic",
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
    /// Full eager load of every active SyntheticLife row + all child collections.
    /// Records.Json is never read here.
    /// </summary>
    public static List<SyntheticLifeData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "synthetic")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "synthetic"))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var items = BuildIncludeChain(db.SyntheticLives.AsNoTracking())
            .Where(s => ids.Contains(s.Id))
            .ToList();

        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<SyntheticLifeData>(items.Count);
        foreach (var s in items)
        {
            tagsByEntity.TryGetValue(s.Id, out var tags);
            result.Add(Materialize(s, tags));
        }
        return result;
    }

    /// <summary>Load a single SyntheticLife by id. Returns null when not found.</summary>
    public static SyntheticLifeData? LoadOne(ProseDbContext db, Guid id)
    {
        var s = BuildIncludeChain(db.SyntheticLives.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (s == null) return null;
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(s, tags);
    }

    private static IQueryable<SyntheticLife> BuildIncludeChain(IQueryable<SyntheticLife> q)
        => q.AsSplitQuery()
            .Include(s => s.Aliases)
            .Include(s => s.KnownAssociations)
            .Include(s => s.StoryHooks);

    /// <summary>Build a SyntheticLifeData from the EF entity + tags.</summary>
    public static SyntheticLifeData Materialize(SyntheticLife s, List<string>? tags)
    {
        return new SyntheticLifeData
        {
            Id                 = s.Id.ToString("N"),
            Type               = "synthetic",
            Name               = s.Name,
            KindOfBeing        = s.KindOfBeing,
            Manufacturer       = s.Manufacturer,
            Tier               = s.Tier,
            Rating             = s.Rating,
            VoteCount          = s.VoteCount,
            Classification     = s.Classification,
            Disposition        = s.Disposition,
            Habitat            = s.Habitat,
            Origin             = s.Origin,
            LifeStatus         = s.LifeStatus,
            Description        = s.Description,
            ObservedBehavior   = s.ObservedBehavior,
            EncounterFrequency = s.EncounterFrequency,
            ConfirmedSightings = s.ConfirmedSightings,
            Location           = s.Location,
            DtiRating          = s.DtiRating,
            Paratechnological  = s.Paratechnological,
            KnownAge           = s.KnownAge,
            CrackPattern       = s.CrackPattern,
            CurrentRole        = s.CurrentRole,
            KnownLocation      = s.KnownLocation,
            DiplomaticSpecialty= s.DiplomaticSpecialty,
            OperatingHistory   = s.OperatingHistory,
            BehavioralNotes    = s.BehavioralNotes,
            DamageHistory      = s.DamageHistory,
            FaceDecoration     = s.FaceDecoration,
            MidjourneyPrompt   = s.MidjourneyPrompt,
            Dalle3Prompt       = s.Dalle3Prompt,

            Aliases            = s.Aliases.OrderBy(a => a.Position).Select(a => a.Value).ToList(),
            KnownAssociations  = s.KnownAssociations.OrderBy(a => a.Position).Select(a => a.Alias).ToList(),
            StoryHooks         = s.StoryHooks.OrderBy(h => h.Position).Select(h => h.Hook).ToList(),
            Tags               = tags ?? new List<string>(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a SyntheticLifeData into the relational schema. Existing bridge
    /// rows are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, SyntheticLifeData src, CancellationToken ct = default)
    {
        var s = await db.SyntheticLives.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = s == null;

        if (!isNew)
        {
            await db.SyntheticLifeAliases.Where(x => x.SyntheticLifeId == id).ExecuteDeleteAsync(ct);
            await db.SyntheticLifeKnownAssociations.Where(x => x.SyntheticLifeId == id).ExecuteDeleteAsync(ct);
            await db.SyntheticLifeStoryHooks.Where(x => x.SyntheticLifeId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            s = new SyntheticLife { Id = id };
            db.SyntheticLives.Add(s);
        }

        FillScalars(s!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on SyntheticLife from src (no DB touch).</summary>
    public static void FillScalars(SyntheticLife s, SyntheticLifeData src)
    {
        s.Name               = src.Name ?? "";
        s.KindOfBeing        = src.KindOfBeing ?? "";
        s.Manufacturer       = src.Manufacturer ?? "";
        s.Tier               = src.Tier ?? "";
        s.Rating             = src.Rating;
        s.VoteCount          = src.VoteCount;
        s.Classification     = src.Classification ?? "";
        s.Disposition        = src.Disposition ?? "";
        s.Habitat            = src.Habitat ?? "";
        s.Origin             = src.Origin ?? "";
        s.LifeStatus         = src.LifeStatus ?? "";
        s.Description        = src.Description ?? "";
        s.ObservedBehavior   = src.ObservedBehavior ?? "";
        s.EncounterFrequency = src.EncounterFrequency ?? "";
        s.ConfirmedSightings = src.ConfirmedSightings;
        s.Location           = src.Location ?? "";
        s.DtiRating          = src.DtiRating;
        s.Paratechnological  = src.Paratechnological;
        s.KnownAge           = string.IsNullOrEmpty(src.KnownAge)           ? null : src.KnownAge;
        s.CrackPattern       = string.IsNullOrEmpty(src.CrackPattern)       ? null : src.CrackPattern;
        s.CurrentRole        = string.IsNullOrEmpty(src.CurrentRole)        ? null : src.CurrentRole;
        s.KnownLocation      = string.IsNullOrEmpty(src.KnownLocation)      ? null : src.KnownLocation;
        s.DiplomaticSpecialty= string.IsNullOrEmpty(src.DiplomaticSpecialty)? null : src.DiplomaticSpecialty;
        s.OperatingHistory   = string.IsNullOrEmpty(src.OperatingHistory)   ? null : src.OperatingHistory;
        s.BehavioralNotes    = string.IsNullOrEmpty(src.BehavioralNotes)    ? null : src.BehavioralNotes;
        s.DamageHistory      = string.IsNullOrEmpty(src.DamageHistory)      ? null : src.DamageHistory;
        s.FaceDecoration     = string.IsNullOrEmpty(src.FaceDecoration)     ? null : src.FaceDecoration;
        s.MidjourneyPrompt   = src.MidjourneyPrompt ?? "";
        s.Dalle3Prompt       = src.Dalle3Prompt ?? "";
    }

    /// <summary>Insert all bridge rows (assumes bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, SyntheticLifeData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.SyntheticLifeAliases.Add(new SyntheticLifeAlias { SyntheticLifeId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.KnownAssociations.Count; i++)
        {
            var alias = src.KnownAssociations[i] ?? "";
            var associateId = ResolveEntityId(db, alias);
            db.SyntheticLifeKnownAssociations.Add(new SyntheticLifeKnownAssociation
            {
                SyntheticLifeId   = id,
                Position          = i,
                Alias             = alias,
                AssociateEntityId = associateId,
            });
        }

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.SyntheticLifeStoryHooks.Add(new SyntheticLifeStoryHook { SyntheticLifeId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every synthetic Entity (active or inactive), deserialize
    /// its Records.Json blob → SyntheticLifeData → persist. Also creates a
    /// minimal relational row for any active synthetic entity with no blob and no
    /// relational row yet. Returns the number of synthetic entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-synthetic-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var entityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "synthetic")
            .Select(e => new { e.Id, e.Name })
            .ToList();

        if (entityIds.Count == 0) return 0;

        var idSet = entityIds.Select(e => e.Id).ToHashSet();

        var blobRows = db.Records.AsNoTracking()
            .Where(r => idSet.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        var existingRelational = db.SyntheticLives.AsNoTracking()
            .Where(s => idSet.Contains(s.Id))
            .Select(s => s.Id)
            .ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            SyntheticLifeData? src;
            try { src = JsonSerializer.Deserialize<SyntheticLifeData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "SyntheticMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "SyntheticMapper.RebuildAllAsync: failed to persist entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for active entities with no blob and no relational row
        foreach (var e in entityIds.Where(e => !blobEntityIds.Contains(e.Id) && !existingRelational.Contains(e.Id)))
        {
            try
            {
                var stub = new SyntheticLifeData { Id = e.Id.ToString("N"), Name = e.Name ?? "" };
                await PersistAsync(db, e.Id, stub, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "SyntheticMapper.RebuildAllAsync: failed to create stub for entity {Id}", e.Id);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static Guid? ResolveEntityId(ProseDbContext db, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.Name == alias);
        if (e != null) return e.Id;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(alias);
        e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.Slug == slug);
        return e?.Id;
    }
}
