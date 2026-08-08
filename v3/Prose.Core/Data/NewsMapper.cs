using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core;

namespace Prose.Core.Data;

using NewsEntity     = Prose.Core.Data.Entities.News;
using NewsEntityInv  = Prose.Core.Data.Entities.NewsEntityInvolved;
using NewsLoc        = Prose.Core.Data.Entities.NewsLocation;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (News + NewsEntityInvolved +
/// NewsLocation) and the domain model (NewsData). List fields:
///   .entities_involved → NewsEntityInvolved (bridge already exists)
///   .locations         → NewsLocation       (bridge already exists)
///   .tags              → EntityTags (universal layer)
///
/// ADDITIVE CONTRACT: Records.Json is never touched or deleted here.
/// </summary>
public static class NewsMapper
{
    // ─────────────────────────────────────────────────────────────────────
    // READ PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight list-view projection. Returns Id, Name, Category, Tags only.
    /// </summary>
    public static List<NewsData> LoadAllLite(ProseDbContext db)
    {
        var rows = db.News.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "news"),
                n => n.Id, e => e.Id,
                (n, e) => new { n.Id, Name = e.Name, n.Category, n.Rating, n.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<NewsData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new NewsData
            {
                Id        = r.Id.ToString("N"),
                Headline  = r.Name ?? "",
                Category  = r.Category ?? "",
                Rating    = r.Rating,
                VoteCount = r.VoteCount,
                Tags      = tags ?? new List<string>(),
            });
        }
        return result;
    }

    /// <summary>Full load of every active News row + all bridge rows, projected to NewsData.</summary>
    public static List<NewsData> LoadAll(ProseDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "news")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "news" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        if (ids.Count == 0) return new();

        var newsList = BuildIncludeChain(db.News.AsNoTracking())
            .Where(n => ids.Contains(n.Id))
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

        var result = new List<NewsData>(newsList.Count);
        foreach (var n in newsList)
        {
            entityById.TryGetValue(n.Id, out var entity);
            tagsByEntity.TryGetValue(n.Id, out var tags);
            result.Add(Materialize(n, entity, tags));
        }
        return result;
    }

    /// <summary>Load a single News item by id. Returns null when not found.</summary>
    public static NewsData? LoadOne(ProseDbContext db, Guid id)
    {
        var n = BuildIncludeChain(db.News.AsNoTracking()).FirstOrDefault(x => x.Id == id);
        if (n == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        return Materialize(n, entity, tags);
    }

    private static IQueryable<NewsEntity> BuildIncludeChain(IQueryable<NewsEntity> q)
        => q.AsSplitQuery()
            .Include(n => n.EntitiesInvolved)
            .Include(n => n.Locations);

    /// <summary>Build a NewsData from the entity row + bridges.</summary>
    public static NewsData Materialize(NewsEntity n, Entity? entity, List<string>? tags)
    {
        var data = new NewsData
        {
            Id              = n.Id.ToString("N"),
            Type            = "news",
            Headline        = entity?.Name ?? n.Name,
            Date            = n.DateText,
            Category        = n.Category,
            Source          = n.Source,
            Reporter        = n.Reporter,
            Body            = n.Body,
            Aftermath       = n.Aftermath,
            Casualties      = n.Casualties,
            RunnerRelevance = n.RunnerRelevance,
            Rating          = n.Rating,
            VoteCount       = n.VoteCount,
            MidjourneyPrompt = n.MidjourneyPrompt,
            Dalle3Prompt    = n.Dalle3Prompt,
            Tags            = tags ?? new List<string>(),
        };

        data.EntitiesInvolved = n.EntitiesInvolved
            .OrderBy(x => x.Position)
            .Select(x => x.Alias)
            .ToList();

        data.Locations = n.Locations
            .OrderBy(x => x.Position)
            .Select(x => x.Alias)
            .ToList();

        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upsert a NewsData into the relational schema. Bridge rows are wiped and
    /// re-inserted on every save. Caller must call db.SaveChanges() after this.
    /// Records.Json is NOT touched — additive-only contract.
    /// </summary>
    public static async Task PersistAsync(ProseDbContext db, Guid id, NewsData src, CancellationToken ct = default)
    {
        var news = await db.News.FirstOrDefaultAsync(n => n.Id == id, ct);
        var isNew = news == null;

        if (!isNew)
        {
            await db.NewsEntitiesInvolved.Where(x => x.NewsId == id).ExecuteDeleteAsync(ct);
            await db.NewsLocations.Where(x => x.NewsId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            news = new NewsEntity { Id = id };
            db.News.Add(news);
        }

        FillScalars(news!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on News from src (no DB touch).</summary>
    public static void FillScalars(NewsEntity n, NewsData src)
    {
        n.Name            = src.Headline ?? "";
        n.DateText        = src.Date ?? "";
        n.Category        = src.Category ?? "";
        n.Source          = src.Source ?? "";
        n.Reporter        = src.Reporter ?? "";
        n.Body            = src.Body ?? "";
        n.Aftermath       = src.Aftermath ?? "";
        n.Casualties      = src.Casualties ?? "";
        n.RunnerRelevance = src.RunnerRelevance ?? "";
        n.Rating          = src.Rating;
        n.VoteCount       = src.VoteCount;
        n.MidjourneyPrompt = src.MidjourneyPrompt ?? "";
        n.Dalle3Prompt    = src.Dalle3Prompt ?? "";

        // Parse DateText into PublishedDate when possible.
        if (!string.IsNullOrWhiteSpace(src.Date)
            && DateTime.TryParse(src.Date, out var dt))
            n.PublishedDate = dt;
    }

    /// <summary>Insert all bridge rows (assumes parent bridges have already been wiped).</summary>
    public static void FillBridges(ProseDbContext db, Guid id, NewsData src)
    {
        for (int i = 0; i < src.EntitiesInvolved.Count; i++)
        {
            var alias = src.EntitiesInvolved[i] ?? "";
            var entityId = ResolveEntityId(db, alias);
            db.NewsEntitiesInvolved.Add(new NewsEntityInv
            {
                NewsId           = id,
                Position         = i,
                Alias            = alias,
                InvolvedEntityId = entityId,
            });
        }

        for (int i = 0; i < src.Locations.Count; i++)
        {
            var alias = src.Locations[i] ?? "";
            var placeId = ResolveEntityId(db, alias, "place");
            db.NewsLocations.Add(new NewsLoc
            {
                NewsId   = id,
                Position = i,
                Alias    = alias,
                PlaceId  = placeId,
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // BACKFILL
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Backfill: for every active news Entity, deserialize its Records.Json
    /// blob → NewsData → persist. Returns the number of news items written.
    ///
    /// ADDITIVE — Records.Json is never modified or deleted.
    /// Run once via <c>prose --rebuild-news-relational</c>.
    /// </summary>
    public static async Task<int> RebuildAllAsync(ProseDbContext db, CancellationToken ct = default)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var newsEntityIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "news" && e.IsActive)
            .Select(e => e.Id)
            .ToHashSet();

        if (newsEntityIds.Count == 0) return 0;

        var blobRows = db.Records.AsNoTracking()
            .Where(r => newsEntityIds.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToList();

        int written = 0;
        foreach (var row in blobRows)
        {
            NewsData? src;
            try { src = JsonSerializer.Deserialize<NewsData>(row.Json, opts); }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "NewsMapper.RebuildAllAsync: failed to deserialize blob for entity {Id}", row.EntityId);
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
                Serilog.Log.Warning(ex, "NewsMapper.RebuildAllAsync: failed to persist news {Id}", row.EntityId);
                db.ChangeTracker.Clear();
            }
        }
        return written;
    }

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

    private static Guid? ResolveEntityId(ProseDbContext db, string name, string? entityType = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.IsActive
                && (entityType == null || e.EntityType == entityType)
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }
}
