using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Data;

/// <summary>
/// EF Core-backed analogue of <see cref="JsonDirectoryRepository{T}"/>. Same public
/// surface (GetAll / GetById / GetByName / GetBySlug / Save / Delete / Reload /
/// Count / OnItemSaved / RepoName / GetExportEntries) so consumer code doesn't
/// change. Storage = StreetSamuraiDbContext: each domain object T is materialized
/// from <see cref="Record.Json"/> on read and serialized into it on write. The
/// per-type Entity + Subtype rows are kept in sync for indexed queries.
/// </summary>
public class EfRepository<T> : IExportableRepository where T : class
{
    protected readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    protected readonly string entityType;
    protected readonly Func<T, string> nameSelector;
    protected readonly JsonSerializerOptions jsonOpts;

    private List<T>? cache;
    private readonly object cacheLock = new();

    /// <summary>
    /// Drop the cached projection so the next read rebuilds from the database.
    /// Subclasses that bypass the JSON-blob path use this to invalidate after a
    /// column-only write.
    /// </summary>
    protected void InvalidateCacheExternal() => InvalidateCache();

    /// <summary>
    /// Fire the OnItemSaved event from a subclass that overrides Save. Index
    /// services (XrefService, GlobalSearchService) subscribe to this so they
    /// can invalidate their lookup tables on each write.
    /// </summary>
    protected void RaiseOnItemSaved(string name) => OnItemSaved?.Invoke(name);

    public EfRepository(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        string entityType,
        Func<T, string> nameSelector)
    {
        this.dbFactory = dbFactory;
        this.entityType = entityType;
        this.nameSelector = nameSelector;
        jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public event Action<string>? OnItemSaved;

    /// <summary>Display-friendly name (matches the legacy file-folder label tests assert on).</summary>
    public string RepoName => RepoNameMap.TryGetValue(entityType, out var pretty) ? pretty : entityType.Replace("_", " ");

    private static readonly Dictionary<string, string> RepoNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["character"]      = "people",
        ["place"]          = "places",
        ["faction"]        = "factions",
        ["corponation"]    = "corponations",
        ["subsidiary"]     = "subsidiaries",
        ["synthetic"]      = "synthetics",
        ["automaton"]      = "automata",
        ["weapon"]         = "weaponry",
        ["equipment"]      = "equipment",
        ["cyberware"]      = "cyberware",
        ["apparel"]        = "apparel",
        ["ammunition"]     = "ammunition",
        ["pharmaceutical"] = "pharmaceuticals",
        ["genemod"]        = "genemods",
        ["material"]       = "materials",
        ["transportation"] = "transportation",
        ["consumer_good"]  = "Consumer Goods",
        ["archetype"]      = "archetypes",
        ["quote"]          = "quotes",
        ["news"]           = "news",
        ["contract"]       = "contracts",
        ["document"]       = "documents",
        ["vocabulary"]     = "vocabulary",
        ["lab_specimen"]   = "lab_specimens",
        ["psionic"]        = "psionics",
        ["technology"]     = "technology",
        ["facet"]          = "facets",
        ["motif"]          = "motifs",
        ["entertainment"]  = "entertainment",
        ["flyover_entity"] = "flyover_entities",
    };

    /// <summary>
    /// Returns every active record for this entity type. Archived rows
    /// (<c>Entity.IsActive = false</c>) are excluded — use <see cref="GetAllIncludingArchived"/>
    /// when you need the full history (e.g. restore flows, audit views).
    /// </summary>
    public virtual List<T> GetAll()
    {
        lock (cacheLock)
        {
            if (cache != null) return cache;
        }

        using var db = dbFactory.CreateDbContext();
        var rows = db.Records
            .AsNoTracking()
            .Where(r => r.Entity!.EntityType == entityType && r.Entity.IsActive)
            .Select(r => r.Json)
            .ToList();

        var list = new List<T>(rows.Count);
        foreach (var json in rows)
        {
            try
            {
                var item = JsonSerializer.Deserialize<T>(json, jsonOpts);
                if (item != null) list.Add(item);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "EfRepository<{T}> failed to deserialize a record (entityType={Kind})",
                    typeof(T).Name, entityType);
            }
        }

        lock (cacheLock) cache = list;
        return list;
    }

    /// <summary>Like <see cref="GetAll"/> but does not filter archived rows.</summary>
    public virtual List<T> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        var rows = db.Records
            .AsNoTracking()
            .Where(r => r.Entity!.EntityType == entityType)
            .Select(r => r.Json)
            .ToList();
        var list = new List<T>(rows.Count);
        foreach (var json in rows)
        {
            try { var x = JsonSerializer.Deserialize<T>(json, jsonOpts); if (x != null) list.Add(x); }
            catch { /* deserialization issues already logged on the active path */ }
        }
        return list;
    }

    public T? GetByName(string name)
        => GetAll().FirstOrDefault(item => nameSelector(item).Equals(name, StringComparison.OrdinalIgnoreCase));

    public T? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return GetAll().FirstOrDefault(item => item is IWorldRecord r && r.Id == id);
    }

    public T? GetBySlug(string slug)
        => string.IsNullOrWhiteSpace(slug) ? null
        : GetAll().FirstOrDefault(item => JsonDirectoryRepository<T>.ToSlug(nameSelector(item)) == slug);

    public virtual void Save(T item)
    {
        var name = nameSelector(item);
        var idStr = (item as IWorldRecord)?.Id ?? Guid.CreateVersion7().ToString("N");
        var id = ParseGuid(idStr);

        // Ensure the domain object carries a stable id; the file-backed repo
        // assigned one when missing — match that behavior.
        if (item is IWorldRecord rec && string.IsNullOrEmpty(rec.Id))
            rec.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var existing = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existing == null)
        {
            var slug = ResolveSlug(db, name, id, currentSlug: null);
            existing = new Entity
            {
                Id          = id,
                EntityType  = entityType,
                Name        = name,
                Slug        = slug,
                Status      = "canon",
                CreatedAt   = DateTime.UtcNow,
                ModifiedAt  = DateTime.UtcNow,
            };
            db.Entities.Add(existing);
        }
        else
        {
            // Only recompute slug when the name actually changed; otherwise keep the
            // existing slug (which may have been disambiguated during import as
            // "{slug}-{id:N}" and must not be silently downgraded to plain Slugify(name),
            // since that would collide with the other entity holding the plain slug).
            if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
            {
                existing.Name = name;
                existing.Slug = ResolveSlug(db, name, id, existing.Slug);
            }
            existing.ModifiedAt = DateTime.UtcNow;
        }

        var json = JsonSerializer.Serialize(item, jsonOpts);
        var record = db.Records.FirstOrDefault(r => r.EntityId == id);
        if (record == null)
            db.Records.Add(new Record { EntityId = id, Json = json, UpdatedAt = DateTime.UtcNow });
        else
        {
            record.Json = json;
            record.UpdatedAt = DateTime.UtcNow;
        }

        db.SaveChanges();

        InvalidateCache();
        OnItemSaved?.Invoke(name);
    }

    public void SaveAll(List<T> items)
    {
        foreach (var item in items) Save(item);
    }

    public virtual void Delete(string name)
    {
        var item = GetByName(name);
        if (item == null) return;
        var idStr = (item as IWorldRecord)?.Id;
        if (string.IsNullOrEmpty(idStr)) return;
        var id = ParseGuid(idStr);

        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (entity != null)
        {
            entity.Status     = "archived";
            entity.IsActive   = false;
            entity.ArchivedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;
        }
        db.SaveChanges();
        InvalidateCache();
    }

    public void Reload() => InvalidateCache();

    public int Count()
    {
        using var db = dbFactory.CreateDbContext();
        return db.Entities.Count(e => e.EntityType == entityType && e.Status != "archived");
    }

    public List<(string name, string json)> GetExportEntries()
        => GetAll().Select(e => (nameSelector(e), JsonSerializer.Serialize(e, jsonOpts))).ToList();

    private void InvalidateCache()
    {
        lock (cacheLock) cache = null;
    }

    /// <summary>
    /// Mirrors the importer's slug-collision strategy: prefer Slugify(name); if
    /// another entity in this type already owns that slug, fall back to
    /// "{slug}-{id:N}" so the (EntityType, Slug) unique constraint never trips.
    /// Pass <paramref name="currentSlug"/> for an existing row so we can keep its
    /// slug if it already starts with the desired stem (avoids needless churn).
    /// </summary>
    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";

        // If we're keeping a disambig'd slug already pinned to this id, keep it.
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;

        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }

    private static Guid ParseGuid(string s)
    {
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }
}
