using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Places + 10 child
/// tables) and the domain model (DistrictData).
///
/// Bridges:
///   PlaceAliases, PlaceDangers, PlaceOpportunities, PlaceStoryHooks,
///   PlaceAtmosphereItems (bucket discriminator: sights/sounds/smells),
///   PlaceAdjacencies, PlaceExits, PlaceFrequentedBy,
///   PlaceNotableLocations, PlaceRelatedEntities.
///
/// Column mapping:
///   domain Territory     → DB column Tier  (legacy naming; kept for backward compat)
///   domain Connections.AdjacentTo → PlaceAdjacencies
///   domain Connections.Exits      → PlaceExits
///   domain Atmosphere.Feel        → DB scalar AtmosphereFeel
///   domain Atmosphere.Sights/Sounds/Smells → PlaceAtmosphereItems
///   domain Coordinates.Lat/Lng    → DB scalars GeoLat/GeoLng
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class PlaceMapper
{
    // ─────────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Territory (Tier),
    /// Rating, VoteCount, Tags only. No bridge materialization.
    /// </summary>
    public static List<DistrictData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.Places.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "place"),
                p => p.Id, e => e.Id,
                (p, e) => new { p.Id, p.Name, p.Tier, p.Rating, p.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<DistrictData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new DistrictData
            {
                Id        = r.Id.ToString("N"),
                Type      = "place",
                Name      = r.Name ?? "",
                Tags      = tags ?? new List<string>(),
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
            });
        }
        return result;
    }

    /// <summary>
    /// Full eager load of every active Place row + all child collections.
    /// Records.Json is never read here.
    /// </summary>
    public static List<DistrictData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "place")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "place" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var places = BuildIncludeChain(db.Places.AsNoTracking())
            .Where(p => ids.Contains(p.Id))
            .ToList();

        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<DistrictData>(places.Count);
        foreach (var p in places)
        {
            tagsByEntity.TryGetValue(p.Id, out var tags);
            result.Add(Materialize(p, tags));
        }
        return result;
    }

    /// <summary>Load a single Place by id. Returns null when not found.</summary>
    public static DistrictData? LoadOne(ProseDbContext db, Guid id)
    {
        var p = BuildIncludeChain(db.Places.AsNoTracking())
            .FirstOrDefault(x => x.Id == id);
        if (p == null) return null;
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(p, tags);
    }

    private static IQueryable<Place> BuildIncludeChain(IQueryable<Place> q)
        => q.AsSplitQuery()
            .Include(p => p.Aliases)
            .Include(p => p.Dangers)
            .Include(p => p.Opportunities)
            .Include(p => p.StoryHooks)
            .Include(p => p.AtmosphereItems)
            .Include(p => p.Adjacencies)
            .Include(p => p.Exits)
            .Include(p => p.FrequentedBy)
            .Include(p => p.NotableLocations)
            .Include(p => p.RelatedEntities);

    /// <summary>Build a DistrictData from the EF entity + tags.</summary>
    public static DistrictData Materialize(Place p, List<string>? tags)
    {
        return new DistrictData
        {
            Id             = p.Id.ToString("N"),
            Type           = "place",
            Name           = p.Name,
            Rating         = p.Rating,
            VoteCount      = p.VoteCount,
            Description    = p.Description,
            Demographics   = p.Demographics,
            Economy        = p.Economy,
            PowerStructure = p.PowerStructure,
            MidjourneyPrompt = p.MidjourneyPrompt,
            Dalle3Prompt     = p.Dalle3Prompt,

            Aliases      = p.Aliases.OrderBy(a => a.Position).Select(a => a.Value).ToList(),
            Dangers      = p.Dangers.OrderBy(d => d.Position).Select(d => d.Danger).ToList(),
            Opportunities= p.Opportunities.OrderBy(o => o.Position).Select(o => o.Opportunity).ToList(),
            StoryHooks   = p.StoryHooks.OrderBy(h => h.Position).Select(h => h.Hook).ToList(),

            Atmosphere = new AtmosphereData
            {
                Feel   = p.AtmosphereFeel,
                Sights = p.AtmosphereItems.Where(a => a.Bucket == "sights").OrderBy(a => a.Position).Select(a => a.Item).ToList(),
                Sounds = p.AtmosphereItems.Where(a => a.Bucket == "sounds").OrderBy(a => a.Position).Select(a => a.Item).ToList(),
                Smells = p.AtmosphereItems.Where(a => a.Bucket == "smells").OrderBy(a => a.Position).Select(a => a.Item).ToList(),
            },

            Connections = new DistrictConnections
            {
                AdjacentTo = p.Adjacencies.OrderBy(a => a.Position).Select(a => a.Alias).ToList(),
                Exits = p.Exits.OrderBy(e => e.Position).Select(e => new PlaceExit
                {
                    Direction       = e.Direction,
                    Destination     = e.DestinationAlias,
                    Type            = e.ExitType,
                    Description     = e.Description,
                    Restricted      = e.Restricted,
                    DangerLevel     = e.DangerLevel,
                }).ToList(),
            },

            FrequentedBy = p.FrequentedBy.OrderBy(f => f.Position).Select(f => f.Alias).ToList(),

            NotableLocations = p.NotableLocations.OrderBy(n => n.Position).Select(n => new NotableLocation
            {
                Name        = n.LocationName,
                Description = n.Description,
            }).ToList(),

            Coordinates = new GeoCoordinates
            {
                Lat = p.GeoLat,
                Lng = p.GeoLng,
            },

            RelatedEntities = p.RelatedEntities.OrderBy(r => r.Position).Select(r => r.Alias).ToList(),
            Tags             = tags ?? new List<string>(),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a DistrictData into the relational schema. Existing bridge
    /// rows are wiped and re-inserted. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, DistrictData src, CancellationToken ct = default)
    {
        var p = await db.Places.FirstOrDefaultAsync(x => x.Id == id, ct);
        var isNew = p == null;

        if (!isNew)
        {
            await db.PlaceAliases.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceDangers.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceOpportunities.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceStoryHooks.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceAtmosphereItems.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceAdjacencies.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceExits.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceFrequentedBy.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceNotableLocations.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
            await db.PlaceRelatedEntities.Where(x => x.PlaceId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            p = new Place { Id = id };
            db.Places.Add(p);
        }

        FillScalars(p!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Place from src (no DB touch).</summary>
    public static void FillScalars(Place p, DistrictData src)
    {
        p.Name             = src.Name ?? "";
        p.Territory        = "";  // not used in domain model directly
        p.Tier             = "";  // Tier was historical — kept empty
        p.Climate          = "";
        p.Rating           = src.Rating;
        p.VoteCount        = src.VoteCount;
        p.Description      = src.Description ?? "";
        p.Demographics     = src.Demographics ?? "";
        p.Economy          = src.Economy ?? "";
        p.PowerStructure   = src.PowerStructure ?? "";
        p.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        p.Dalle3Prompt     = src.Dalle3Prompt ?? "";
        p.AtmosphereFeel   = src.Atmosphere?.Feel ?? "";
        p.GeoLat           = src.Coordinates?.Lat ?? 0.0;
        p.GeoLng           = src.Coordinates?.Lng ?? 0.0;
    }

    /// <summary>Insert all bridge rows (assumes bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, DistrictData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.PlaceAliases.Add(new PlaceAlias { PlaceId = id, Position = i, Value = src.Aliases[i] ?? "" });

        for (int i = 0; i < src.Dangers.Count; i++)
            db.PlaceDangers.Add(new PlaceDanger { PlaceId = id, Position = i, Danger = src.Dangers[i] ?? "" });

        for (int i = 0; i < src.Opportunities.Count; i++)
            db.PlaceOpportunities.Add(new PlaceOpportunity { PlaceId = id, Position = i, Opportunity = src.Opportunities[i] ?? "" });

        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.PlaceStoryHooks.Add(new PlaceStoryHook { PlaceId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });

        // Atmosphere items — 3 buckets
        var atm = src.Atmosphere ?? new AtmosphereData();
        for (int i = 0; i < atm.Sights.Count; i++)
            db.PlaceAtmosphereItems.Add(new PlaceAtmosphereItem { PlaceId = id, Bucket = "sights", Position = i, Item = atm.Sights[i] ?? "" });
        for (int i = 0; i < atm.Sounds.Count; i++)
            db.PlaceAtmosphereItems.Add(new PlaceAtmosphereItem { PlaceId = id, Bucket = "sounds", Position = i, Item = atm.Sounds[i] ?? "" });
        for (int i = 0; i < atm.Smells.Count; i++)
            db.PlaceAtmosphereItems.Add(new PlaceAtmosphereItem { PlaceId = id, Bucket = "smells", Position = i, Item = atm.Smells[i] ?? "" });

        // Adjacencies — resolve to FK when possible
        var conn = src.Connections ?? new DistrictConnections();
        for (int i = 0; i < conn.AdjacentTo.Count; i++)
        {
            var alias = conn.AdjacentTo[i] ?? "";
            var neighborId = ResolveEntityId(db, alias, "place");
            db.PlaceAdjacencies.Add(new PlaceAdjacency { PlaceId = id, Position = i, Alias = alias, NeighborId = neighborId });
        }

        // Exits — resolve destination FK when possible
        for (int i = 0; i < conn.Exits.Count; i++)
        {
            var exit = conn.Exits[i];
            var destAlias = exit.Destination ?? "";
            var destId = ResolveEntityId(db, destAlias, "place");
            db.PlaceExits.Add(new PlaceExitRow
            {
                PlaceId          = id,
                Position         = i,
                Direction        = exit.Direction ?? "",
                DestinationAlias = destAlias,
                DestinationId    = destId,
                ExitType         = exit.Type ?? "road",
                Description      = exit.Description ?? "",
                Restricted       = exit.Restricted,
                DangerLevel      = exit.DangerLevel,
            });
        }

        // FrequentedBy — resolve to any entity FK when possible
        for (int i = 0; i < src.FrequentedBy.Count; i++)
        {
            var alias = src.FrequentedBy[i] ?? "";
            var targetId = ResolveEntityIdAny(db, alias);
            db.PlaceFrequentedBy.Add(new PlaceFrequentBy { PlaceId = id, Position = i, Alias = alias, TargetEntityId = targetId });
        }

        // Notable locations
        for (int i = 0; i < src.NotableLocations.Count; i++)
        {
            var loc = src.NotableLocations[i];
            db.PlaceNotableLocations.Add(new PlaceNotableLocation
            {
                PlaceId      = id,
                Position     = i,
                LocationName = loc.Name ?? "",
                Description  = loc.Description ?? "",
            });
        }

        // Related entities — resolve to any entity FK when possible
        for (int i = 0; i < src.RelatedEntities.Count; i++)
        {
            var alias = src.RelatedEntities[i] ?? "";
            var relId = ResolveEntityIdAny(db, alias);
            db.PlaceRelatedEntities.Add(new PlaceRelatedEntity { PlaceId = id, Position = i, Alias = alias, RelatedEntityId = relId });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every place Entity (active or inactive), deserialize
    /// its Records.Json blob → DistrictData → persist. Also creates a
    /// minimal relational row for any active place entity with no blob and no
    /// relational row yet. Returns the number of place entries written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-place-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var entityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "place")
            .Select(e => new { e.Id, e.Name, e.IsActive })
            .ToList();

        if (entityIds.Count == 0) return 0;

        var idSet = entityIds.Select(e => e.Id).ToHashSet();

        var blobRows = db.Records.AsNoTracking()
            .Where(r => idSet.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        var blobEntityIds = blobRows.Select(r => r.EntityId).ToHashSet();

        var existingRelational = db.Places.AsNoTracking()
            .Where(p => idSet.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSet();

        int written = 0;

        foreach (var row in blobRows)
        {
            DistrictData? src;
            try { src = JsonSerializer.Deserialize<DistrictData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "PlaceMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "PlaceMapper.RebuildAllAsync: failed to persist entity {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }

        // Minimal rows for active entities with no blob and no relational row
        foreach (var e in entityIds.Where(e => e.IsActive && !blobEntityIds.Contains(e.Id) && !existingRelational.Contains(e.Id)))
        {
            try
            {
                var stub = new DistrictData { Id = e.Id.ToString("N"), Name = e.Name ?? "" };
                await PersistAsync(db, e.Id, stub, ct);
                await db.SaveChangesAsync(ct);
                written++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "PlaceMapper.RebuildAllAsync: failed to create stub for entity {Id}", e.Id);
                db.ChangeTracker.Clear();
            }
        }

        return written;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static Guid? ResolveEntityId(ProseDbContext db, string alias, string entityType)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        var e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Name == alias && x.IsActive);
        if (e != null) return e.Id;
        var slug = Prose.Core.Services.WorldGraphService.Slugify(alias);
        e = db.Entities.AsNoTracking()
            .FirstOrDefault(x => x.EntityType == entityType && x.Slug == slug && x.IsActive);
        return e?.Id;
    }

    /// <summary>Delegates to <see cref="EntityResolver.ResolveEntityIdAny"/> (shared with CharacterMapper).</summary>
    private static Guid? ResolveEntityIdAny(ProseDbContext db, string alias)
        => EntityResolver.ResolveEntityIdAny(db, alias);
}
