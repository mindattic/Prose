using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Core.Data;

/// <summary>
/// EF Core-backed analogue of <see cref="JsonDirectoryRepository{T}"/>. Same public
/// surface (GetAll / GetById / GetByName / GetBySlug / Save / Delete / Reload /
/// Count / OnItemSaved / RepoName / GetExportEntries) so consumer code doesn't
/// change. Storage = ProseDbContext: each domain object T is materialized
/// from <see cref="Record.Json"/> on read and serialized into it on write. The
/// per-type Entity + Subtype rows are kept in sync for indexed queries.
/// </summary>
public class EfRepository<T> : IExportableRepository, IJsonImportable where T : class
{
    protected readonly IDbContextFactory<ProseDbContext> dbFactory;
    protected readonly string entityType;
    protected readonly Func<T, string> nameSelector;
    protected readonly JsonSerializerOptions jsonOpts;

    private List<T>? cache;
    private int cacheEpoch = -1;
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
        IDbContextFactory<ProseDbContext> dbFactory,
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

    /// <summary>
    /// Fired after every successful save with the entity's DB id and canonical name.
    /// Consumers that need the id (e.g. EntityRamificationService) subscribe here
    /// instead of <see cref="OnItemSaved"/> which only carries the name.
    /// </summary>
    public event Action<Guid, string>? OnEntitySaved;

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
            // Invalidate the cache when the current universe changes (SwitchUniverse), so a list
            // built under GLMZ is never served while Fantasy is active (RFC 0006).
            if (cache != null && cacheEpoch == UniverseScope.Epoch) return cache;
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

        lock (cacheLock) { cache = list; cacheEpoch = UniverseScope.Epoch; }
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

    /// <summary>
    /// Virtual so subtypes with an alias bridge table (e.g. <see cref="Prose.Core.Services.CharacterRepository"/>)
    /// can also match on a known alias/handle, not just the canonical name — a character known by
    /// both a handle and a legal name must resolve to one row, not two.
    /// </summary>
    public virtual T? GetByName(string name)
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
            // Save() upserts by Id only — a caller that doesn't already know the
            // canonical Id for an entity that's already in the corpus (e.g. a
            // hand-authored seed JSON with no "id" field, or a re-run seed script)
            // silently creates a SECOND row with the same (EntityType, Name) instead
            // of updating the original. A 2026-08-02 live-corpus sweep
            // (WorldValidationTests.NoSameTypeNameCollisions) found ~150+ such
            // duplicate rows accumulated across many entity types over months —
            // this warning is the fix to stop the count from growing further; it
            // does not touch existing data (merging duplicates safely needs a
            // deliberate, reviewed pass, not an automatic one here).
            var nameCollision = db.Entities.Any(e => e.IsActive && e.EntityType == entityType
                && e.Id != id && e.Name.ToLower() == name.ToLower());
            if (nameCollision)
                Console.Error.WriteLine(
                    $"[EfRepository] WARNING: creating a NEW '{entityType}' entity named '{name}' " +
                    $"(Id {id}) but an active entity of the same type already has this name. " +
                    "If this is meant to be the same entity, pass its existing Id instead of omitting one.");

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
        OnEntitySaved?.Invoke(id, name);
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
        return db.Entities.Count(e => e.EntityType == entityType && e.IsActive);
    }

    public List<(string name, string json)> GetExportEntries()
        => GetAll().Select(e => (nameSelector(e), JsonSerializer.Serialize(e, jsonOpts))).ToList();

    /// <summary>
    /// IJsonImportable: deserialize a single canonical JSON blob (the on-disk
    /// shape under <c>engine/data/&lt;folder&gt;/*.json</c>) and route through
    /// <see cref="Save"/>. Idempotent — re-running on the same JSON updates the
    /// existing entity rather than duplicating it.
    /// </summary>
    public virtual void ImportFromJson(string fileJson)
    {
        if (string.IsNullOrWhiteSpace(fileJson))
            throw new ArgumentException("Empty JSON blob.", nameof(fileJson));

        var item = JsonSerializer.Deserialize<T>(fileJson, jsonOpts)
            ?? throw new InvalidOperationException(
                $"JSON deserialized to null for entityType '{entityType}' — malformed file.");

        Save(item);
    }

    /// <summary>
    /// IJsonImportable: classifies a file's JSON against the DB's canonical
    /// representation of the same entity. The check round-trips both sides
    /// through this repo's <c>jsonOpts</c> so formatting differences
    /// (whitespace, key order, null-omission) don't false-positive as drift.
    ///
    /// Two read paths, in priority order:
    ///   1. <c>Records.Json</c> — populated by the base <see cref="Save"/>;
    ///      every plain <c>EfRepository&lt;T&gt;</c> uses this.
    ///   2. <c>GetById</c> fallback — subclasses like
    ///      <c>CharacterRepository</c> override <c>Save</c> to write fully
    ///      relational rows and never touch <c>Records.Json</c>. When the
    ///      <c>Entities</c> row exists but <c>Records.Json</c> doesn't, we
    ///      materialize the domain object via the subclass's overridden
    ///      <c>GetAll/GetById</c> and canonicalize that instead.
    /// </summary>
    public virtual JsonVerifyResult VerifyAgainstDb(string fileJson)
    {
        if (string.IsNullOrWhiteSpace(fileJson)) return JsonVerifyResult.NoId;

        T? fileItem;
        try { fileItem = JsonSerializer.Deserialize<T>(fileJson, jsonOpts); }
        catch { return JsonVerifyResult.NoId; }
        if (fileItem == null) return JsonVerifyResult.NoId;

        // Need a stable Id to look up in DB. If T doesn't implement IWorldRecord
        // or its Id is blank, we can't resolve — caller should flag as NoId.
        if (fileItem is not IWorldRecord rec || string.IsNullOrEmpty(rec.Id))
            return JsonVerifyResult.NoId;

        Guid id;
        try { id = ParseGuid(rec.Id); }
        catch { return JsonVerifyResult.NoId; }

        using var db = dbFactory.CreateDbContext();

        // No Entity row → genuinely missing canon, regardless of the repo's
        // storage strategy.
        var entityExists = db.Entities.AsNoTracking().Any(e => e.Id == id);
        if (!entityExists) return JsonVerifyResult.Missing;

        // Canonicalize the file side once, used by either DB-side path.
        var fileCanonical = JsonSerializer.Serialize(fileItem, jsonOpts);

        // Path 1 — Records.Json (the standard EfRepository<T> shape).
        var record = db.Records.AsNoTracking().FirstOrDefault(r => r.EntityId == id);
        if (record != null)
        {
            T? dbItem;
            try { dbItem = JsonSerializer.Deserialize<T>(record.Json, jsonOpts); }
            catch { return JsonVerifyResult.Drift; }  // DB blob unparseable → can't claim match
            if (dbItem == null) return JsonVerifyResult.Drift;

            var dbCanonical = JsonSerializer.Serialize(dbItem, jsonOpts);
            return string.Equals(fileCanonical, dbCanonical, StringComparison.Ordinal)
                ? JsonVerifyResult.Match
                : JsonVerifyResult.Drift;
        }

        // Path 2 — relational fallback. Some repositories
        // (CharacterRepository.Save → CharacterMapper.PersistAsync) populate
        // typed rows + child bridges and never write Records.Json. Their
        // overridden GetById materializes the domain object from those tables.
        // Use that as the canonical "what the DB says about this entity."
        var relationalItem = GetById(id.ToString("N"));
        if (relationalItem == null)
            return JsonVerifyResult.Drift;  // Entity row exists but read failed → orphan-subtype, treat as drift

        var relationalCanonical = JsonSerializer.Serialize(relationalItem, jsonOpts);
        return string.Equals(fileCanonical, relationalCanonical, StringComparison.Ordinal)
            ? JsonVerifyResult.Match
            : JsonVerifyResult.Drift;
    }

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
    private string ResolveSlug(ProseDbContext db, string name, Guid id, string? currentSlug)
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
