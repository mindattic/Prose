using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

// â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
// EF-backed repositories â€” total conversion off JsonDirectoryRepository.
// Public surface is unchanged (GetAll / GetById / GetByName / GetBySlug / Save /
// Delete / Reload / Count / OnItemSaved / RepoName / GetExportEntries) so every
// existing consumer compiles. Storage = StreetSamurai SQL Server database.
//
// Each Repository is a thin specialization that supplies (entityType, nameSelector)
// to EfRepository<T>. The legacy `IPathProvider` ctor is preserved so unit-test
// fixtures that constructed repos directly continue to compile; in production the
// DbContext factory is injected by DI and used for real SQL persistence.
// â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

/// <summary>
/// Fully relational CharacterRepository. Reads materialize CharacterData from
/// the Characters table + every child bridge (Aliases / StoryHooks /
/// PsychologyTraits / SpeechPhrases / BehavioralRules + Maps / StatScalars +
/// Phrases / PhysicalMarks / TerritoryZones + Reputations / BelongingsGear +
/// Extras / BioBatteryThresholds / NeuralAbilities / Changelog / Cyberware /
/// Knowledge + KnowledgeEntities / Conditions / Relationships / Timeline +
/// TimelineBodyChanges) â€” never from Records.Json. Writes wipe child bridges
/// and re-insert via <see cref="CharacterMapper"/>.
/// </summary>
public class CharacterRepository : EfRepository<CharacterData>
{
    public CharacterRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "character", c => c.Name) { }
    public CharacterRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "character"), "character", c => c.Name) { }

    // CharacterMapper.LoadAll fans out into ~25 Include collections Ã— 1240
    // characters and is the slowest read in the app (~50â€“80 s cold). Cache the
    // result here â€” invalidated by Save() and the OnItemSaved hook on the
    // base class so writes from this repo are visible immediately. Reload()
    // also clears it. Without this cache /characters re-ran the full load on
    // every page visit and the user-facing spinner could spin for minutes.
    private List<CharacterData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    public override List<CharacterData> GetAll()
    {
        lock (mappedCacheLock)
        {
            // Invalidate on SwitchUniverse so a GLMZ roster isn't served under Fantasy (RFC 0006).
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        // Read off the materialized projection (single column read + cheap
        // tag/location overlay) instead of the 25-Include fan-out. Missing or
        // stale-version rows self-heal via backfill inside this call.
        var loaded = CharacterMapper.LoadAllFromReadModel(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    // List-view-only cache. Contains lightweight CharacterData (Id / Name /
    // Role / Status / Tags / Rating / VoteCount) â€” fields beyond that read as
    // empty defaults. Use this for dictionary/list/filter UIs and re-fetch the
    // full record via GetById when a row is opened for edit.
    private List<CharacterData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public List<CharacterData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CharacterMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    /// <summary>
    /// Fast single-character fetch that bypasses the full LoadAll pipeline.
    /// Hits CharacterMapper.LoadOne (one row + 25 Includes scoped to that
    /// character â€” ~50 ms) instead of materialising every character first.
    /// Required for the lite-list-then-Edit flow: the dictionary list shows
    /// the lite projection; clicking a row re-fetches the full record here.
    ///
    /// Deliberately bypasses CharacterMapper.LoadOneFromReadModel's cached
    /// projection (unlike GetAll). That cache's staleness check only compares
    /// Entities.ModifiedAt against the read-model's RefreshedAt â€” a bridge-table
    /// write that never touches the Entities row (e.g. a direct SQL INSERT into
    /// CharacterAliases for a manual data repair) is invisible to it, so the
    /// cache can serve a snapshot that predates the bridge change. That would be
    /// harmless for a plain read, but GetById's result also feeds
    /// CharacterRepository.Save's wipe-and-reinsert of every bridge table (see
    /// CharacterMapper.PersistAsync) â€” the read/mutate-scalars/write round trip
    /// every upsert caller (e.g. the create_character MCP tool) performs. Serving
    /// a stale Aliases (or any other bridge) list there means Save silently wipes
    /// rows that were never stale in the database, only in the cache. GetById is
    /// the "authoritative full record for editing" per this method's whole
    /// purpose, so it must always read the live relational truth.
    /// </summary>
    public new CharacterData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            // Fall back to the legacy "32-char N format" the codebase also uses.
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return CharacterMapper.LoadOne(db, guid);
    }

    public override List<CharacterData> GetAllIncludingArchived()
    {
        // Archived view bypasses the cache â€” it's used by audit/restore flows
        // that explicitly want fresh data and tolerate the cost.
        using var db = dbFactory.CreateDbContext();
        return CharacterMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(CharacterData item)
    {
        var idStr = item.Id;
        var id = ParseGuid(idStr);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        // Universal Entity row (Name / Slug / Status / IsActive). Same logic
        // EfRepository.Save uses, kept here so the relational path doesn't
        // depend on the JSON-blob path being correct.
        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id = id,
                EntityType = entityType,
                Name = name,
                Slug = ResolveCharacterSlug(db, name, id, currentSlug: null),
                Status = "canon",
                Description = item.Description,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            // Description lives on CharacterData/Characters as the source of truth, but
            // Entity.Description is the field SceneContextAssembler.FormatCharacterAsync
            // actually reads for live prose-generation context â€” without this sync it silently
            // stays null/stale forever and a character's description never reaches generated
            // prose no matter how carefully it's written on the Character record. Confirmed via
            // a real incident: every TFAH-book character had a populated Characters.Description
            // but a null Entities.Description, so none of it had ever reached the DCM pipeline.
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveCharacterSlug(db, name, id, existingEntity.Slug);
            }
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        // Persist column + bridge state via the mapper (sync wrapper around the
        // async API â€” Save is a synchronous repository contract).
        CharacterMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();

        // Refresh tags via the universal layer.
        SyncTagsForEntity(db, id, item.Tags);

        db.SaveChanges();

        // Enforced single-writer sync: regenerate this character's materialized
        // read-model from the just-persisted relational record so GetAll/GetById
        // (which read off the projection) never serve stale data after an edit.
        CharacterMapper.RefreshReadModelAsync(db, id).GetAwaiter().GetResult();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        // Tell index services (XrefService, GlobalSearchService) the canon moved.
        RaiseOnItemSaved(name);
    }

    /// <summary>Override of <see cref="EfRepository{T}.Reload"/> so callers
    /// who explicitly want a refresh (e.g. CharacterDictionary OnInitialized)
    /// also clear the mapper-cache, not just the base JSON-blob cache.</summary>
    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    /// <summary>Override of <see cref="EfRepository{T}.Delete"/> so archiving a
    /// character also clears the mapper-cache. The base Delete only drops the
    /// JSON-blob cache; without this the soft-deleted row stays visible in the
    /// list/dictionary views (which read <see cref="GetAll"/>'s mappedCache)
    /// until the next Save or Reload.</summary>
    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveCharacterSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }

    /// <summary>
    /// Add any tag names that aren't already attached to this entity. The
    /// universal Tag/EntityTag tables are the source of truth â€” this only adds,
    /// matching the existing import behavior (tag removal is a manual op).
    /// </summary>
    private static void SyncTagsForEntity(StreetSamuraiDbContext db, Guid entityId, IReadOnlyList<string>? tags)
    {
        if (tags == null || tags.Count == 0) return;
        var existing = db.EntityTags
            .Where(t => t.EntityId == entityId)
            .Select(t => t.Tag!.Name)
            .ToList()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Names we still need to attach, normalised and de-duped.
        var wanted = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !existing.Contains(t))
            .ToList();
        if (wanted.Count == 0) return;

        // One query for every pre-existing Tag row, instead of a FirstOrDefault
        // round-trip per name.
        var byName = db.Tags
            .Where(t => wanted.Contains(t.Name))
            .ToList()
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

        foreach (var tagName in wanted)
        {
            if (!byName.TryGetValue(tagName, out var tag))
            {
                tag = new Tag { Name = tagName };
                db.Tags.Add(tag);
                byName[tagName] = tag;
            }
            // Use the navigation property so EF resolves TagId (including for
            // brand-new Tag rows) on the caller's single SaveChanges â€” no more
            // one-commit-per-tag inside the loop.
            db.EntityTags.Add(new EntityTag { EntityId = entityId, Tag = tag });
        }
    }
}

/// <summary>
/// Fully relational CorponationRepository. Reads materialize CorponationData from the
/// Corponations table + CorponationCommonNames bridge â€” never from Records.Json. Writes
/// persist via <see cref="CorponationMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class CorponationRepository : EfRepository<CorponationData>
{
    public CorponationRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "corponation", c => c.Name) { }
    public CorponationRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "corponation"), "corponation", c => c.Name) { }

    private List<CorponationData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<CorponationData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<CorponationData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CorponationMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<CorponationData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CorponationMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new CorponationData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return CorponationMapper.LoadOne(db, guid);
    }

    public new CorponationData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "corponation" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return CorponationMapper.LoadOne(db, entity.Id);
    }

    public override List<CorponationData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return CorponationMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(CorponationData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        CorponationMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational DistrictRepository. Reads materialize DistrictData from the
/// Places table + all 10 child bridges (Aliases / Dangers / Opportunities /
/// StoryHooks / AtmosphereItems / Adjacencies / Exits / FrequentedBy /
/// NotableLocations / RelatedEntities) â€” never from Records.Json. Writes persist
/// via <see cref="PlaceMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class DistrictRepository : EfRepository<DistrictData>
{
    public DistrictRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "place", d => d.Name) { }
    public DistrictRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "place"), "place", d => d.Name) { }

    private List<DistrictData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<DistrictData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<DistrictData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = PlaceMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<DistrictData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = PlaceMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new DistrictData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return PlaceMapper.LoadOne(db, guid);
    }

    public new DistrictData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "place" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return PlaceMapper.LoadOne(db, entity.Id);
    }

    public override List<DistrictData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return PlaceMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(DistrictData item)
    {
        var id = ParsePlaceGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id          = id,
                EntityType  = entityType,
                Name        = name,
                Slug        = ResolvePlaceSlug(db, name, id, currentSlug: null),
                Status      = "canon",
                Description = item.Description,
                CreatedAt   = DateTime.UtcNow,
                ModifiedAt  = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolvePlaceSlug(db, name, id, existingEntity.Slug);
            }
            // Entity.Description is the field SceneContextAssembler/DocContextService actually
            // reads for DCM context and the SOURCE Glossary tier (docs/SOURCE.md Â§1b) â€” Places
            // table Description is not enough on its own, same bug class already fixed for
            // CharacterRepository (see its Save()).
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        PlaceMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParsePlaceGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolvePlaceSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational FactionRepository. Reads materialize FactionData from the
/// Factions table + all child bridges (Aliases / Methods / Resources / Goals /
/// StoryHooks / Relationships + RelationshipTags / Members) â€” never from
/// Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="FactionMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class FactionRepository : EfRepository<FactionData>
{
    public FactionRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "faction", f => f.Name) { }
    public FactionRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "faction"), "faction", f => f.Name) { }

    // Universe-epoch-invalidated mapped cache, same pattern as CharacterRepository.
    private List<FactionData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<FactionData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<FactionData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = FactionMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<FactionData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = FactionMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    /// <summary>
    /// Fast single-faction fetch that bypasses the full LoadAll pipeline.
    /// </summary>
    public new FactionData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return FactionMapper.LoadOne(db, guid);
    }

    public new FactionData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "faction" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return FactionMapper.LoadOne(db, entity.Id);
    }

    public override List<FactionData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return FactionMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(FactionData item)
    {
        var idStr = item.Id;
        var id = ParseFactionGuid(idStr);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveFactionSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveFactionSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        FactionMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseFactionGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveFactionSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational WorldbuildingDocRepository. Reads materialize WorldbuildingDocument
/// from the Documents table + DocumentHeadings bridge â€” never from Records.Json. Writes
/// persist via <see cref="DocumentMapper"/>. Records.Json is left intact (additive-only).
/// Note: Entity.Name mirrors FileName (or Title as fallback), matching the original
/// EfRepository nameSelector <c>d => d.FileName</c>.
/// </summary>
public class WorldbuildingDocRepository : EfRepository<WorldbuildingDocument>
{
    public WorldbuildingDocRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "document", d => d.FileName) { }
    public WorldbuildingDocRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "document"), "document", d => d.FileName) { }

    private List<WorldbuildingDocument>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<WorldbuildingDocument>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<WorldbuildingDocument> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = DocumentMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<WorldbuildingDocument> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = DocumentMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new WorldbuildingDocument? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return DocumentMapper.LoadOne(db, guid);
    }

    public new WorldbuildingDocument? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "document" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return DocumentMapper.LoadOne(db, entity.Id);
    }

    public override List<WorldbuildingDocument> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return DocumentMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(WorldbuildingDocument item)
    {
        var id = ParseDocumentGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        // Entity.Name mirrors FileName (per DocumentMapper.FillScalars contract)
        var name = item.FileName?.Length > 0 ? item.FileName : (item.Title ?? "");
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveDocumentSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveDocumentSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        DocumentMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseDocumentGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveDocumentSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational MotifRepository. Reads materialize MotifData from the
/// Motifs table + MotifAppearances bridge â€” never from Records.Json.
/// Writes wipe the bridge and re-insert via <see cref="MotifMapper"/>.
/// Records.Json is left intact (additive-only). (RFC 0007)
/// </summary>
public class MotifRepository : EfRepository<MotifData>
{
    public MotifRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "motif", m => m.Name) { }
    public MotifRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "motif"), "motif", m => m.Name) { }

    private List<MotifData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<MotifData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<MotifData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = MotifMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<MotifData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = MotifMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new MotifData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return MotifMapper.LoadOne(db, guid);
    }

    public new MotifData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "motif" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return MotifMapper.LoadOne(db, entity.Id);
    }

    public override List<MotifData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return MotifMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(MotifData item)
    {
        var id = ParseMotifGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveMotifSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveMotifSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        MotifMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseMotifGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveMotifSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational WeaponryRepository. Reads materialize WeaponryData from the
/// Weapons table + all child bridges (Aliases / BaseTechnologies / KnownUsers /
/// AmmunitionTypes / StoryHooks) â€” never from Records.Json. Writes persist
/// via <see cref="WeaponMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class WeaponryRepository : EfRepository<WeaponryData>
{
    public WeaponryRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "weapon", w => w.Name) { }
    public WeaponryRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "weapon"), "weapon", w => w.Name) { }

    private List<WeaponryData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<WeaponryData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<WeaponryData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = WeaponMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<WeaponryData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = WeaponMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new WeaponryData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return WeaponMapper.LoadOne(db, guid);
    }

    public new WeaponryData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "weapon" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return WeaponMapper.LoadOne(db, entity.Id);
    }

    public override List<WeaponryData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return WeaponMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(WeaponryData item)
    {
        var id = ParseWeaponGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveWeaponSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveWeaponSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        WeaponMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseWeaponGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveWeaponSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational AmmunitionRepository. Reads materialize AmmunitionData from the
/// Ammunitions table + all child bridges (Aliases / CompatibleWeapons / Variants /
/// StoryHooks) â€” never from Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="AmmunitionMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class AmmunitionRepository : EfRepository<AmmunitionData>
{
    public AmmunitionRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "ammunition", a => a.Name) { }
    public AmmunitionRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "ammunition"), "ammunition", a => a.Name) { }

    private List<AmmunitionData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<AmmunitionData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<AmmunitionData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = AmmunitionMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<AmmunitionData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = AmmunitionMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new AmmunitionData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return AmmunitionMapper.LoadOne(db, guid);
    }

    public new AmmunitionData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "ammunition" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return AmmunitionMapper.LoadOne(db, entity.Id);
    }

    public override List<AmmunitionData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return AmmunitionMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(AmmunitionData item)
    {
        var id = ParseAmmunitionGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveAmmunitionSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveAmmunitionSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        AmmunitionMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseAmmunitionGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveAmmunitionSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational EquipmentRepository. Reads materialize EquipmentData from the
/// EquipmentItems table + all child bridges â€” never from Records.Json. Writes persist
/// via <see cref="EquipmentMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class EquipmentRepository : EfRepository<EquipmentData>
{
    public EquipmentRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "equipment", e => e.ProductName.Length > 0 ? e.ProductName : e.Name) { }
    public EquipmentRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "equipment"), "equipment", e => e.ProductName.Length > 0 ? e.ProductName : e.Name) { }

    private List<EquipmentData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<EquipmentData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<EquipmentData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = EquipmentMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<EquipmentData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = EquipmentMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new EquipmentData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return EquipmentMapper.LoadOne(db, guid);
    }

    public new EquipmentData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "equipment" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return EquipmentMapper.LoadOne(db, entity.Id);
    }

    public override List<EquipmentData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return EquipmentMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(EquipmentData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        EquipmentMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational TechnologyRepository. Reads materialize TechnologyData from the
/// Technologies table + all child bridges â€” never from Records.Json. Writes persist
/// via <see cref="TechnologyMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class TechnologyRepository : EfRepository<TechnologyData>
{
    public TechnologyRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "technology", t => t.ProductName.Length > 0 ? t.ProductName : t.Name) { }
    public TechnologyRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "technology"), "technology", t => t.ProductName.Length > 0 ? t.ProductName : t.Name) { }

    private List<TechnologyData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<TechnologyData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<TechnologyData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = TechnologyMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<TechnologyData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = TechnologyMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new TechnologyData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return TechnologyMapper.LoadOne(db, guid);
    }

    public new TechnologyData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "technology" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return TechnologyMapper.LoadOne(db, entity.Id);
    }

    public override List<TechnologyData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return TechnologyMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(TechnologyData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        TechnologyMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational CyberwareRepository. Reads materialize CyberwareData from the
/// CyberwareItems table + all child bridges â€” never from Records.Json. Writes persist
/// via <see cref="CyberwareMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class CyberwareRepository : EfRepository<CyberwareData>
{
    public CyberwareRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "cyberware", c => c.ProductName.Length > 0 ? c.ProductName : c.Name) { }
    public CyberwareRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "cyberware"), "cyberware", c => c.ProductName.Length > 0 ? c.ProductName : c.Name) { }

    private List<CyberwareData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<CyberwareData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<CyberwareData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CyberwareMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<CyberwareData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CyberwareMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new CyberwareData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return CyberwareMapper.LoadOne(db, guid);
    }

    public new CyberwareData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "cyberware" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return CyberwareMapper.LoadOne(db, entity.Id);
    }

    public override List<CyberwareData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return CyberwareMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(CyberwareData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        CyberwareMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational VocabularyRepository. Reads materialize VocabularyData from the
/// VocabularyEntries table â€” never from Records.Json. Writes persist columns via
/// <see cref="VocabularyMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class VocabularyRepository : EfRepository<VocabularyData>
{
    public VocabularyRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "vocabulary", v => v.Term) { }
    public VocabularyRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "vocabulary"), "vocabulary", v => v.Term) { }

    // Universe-epoch-invalidated mapped cache.
    private List<VocabularyData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<VocabularyData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<VocabularyData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = VocabularyMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<VocabularyData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = VocabularyMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new VocabularyData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return VocabularyMapper.LoadOne(db, guid);
    }

    public new VocabularyData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "vocabulary" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return VocabularyMapper.LoadOne(db, entity.Id);
    }

    public override List<VocabularyData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return VocabularyMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(VocabularyData item)
    {
        var idStr = item.Id;
        var id = ParseVocabGuid(idStr);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Term ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveVocabSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveVocabSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        VocabularyMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseVocabGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveVocabSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}


/// <summary>
/// Fully relational GenemodRepository. Reads materialize GenemodData from the
/// Genemods table + all child bridges (Aliases / StoryHooks) â€” never from
/// Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="GenemodMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class GenemodRepository : EfRepository<GenemodData>
{
    public GenemodRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "genemod", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
    public GenemodRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "genemod"), "genemod", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }

    private List<GenemodData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<GenemodData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<GenemodData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = GenemodMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<GenemodData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = GenemodMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new GenemodData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return GenemodMapper.LoadOne(db, guid);
    }

    public new GenemodData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "genemod" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return GenemodMapper.LoadOne(db, entity.Id);
    }

    public override List<GenemodData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return GenemodMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(GenemodData item)
    {
        var id = ParseGenemodGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.ProductName.Length > 0 ? item.ProductName : (item.Name ?? "");
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = item.Name ?? "",
                Slug       = ResolveGenemodSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, item.Name, StringComparison.Ordinal))
            {
                existingEntity.Name = item.Name ?? "";
                existingEntity.Slug = ResolveGenemodSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        GenemodMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGenemodGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveGenemodSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational TransportationRepository. Reads materialize TransportationData
/// from the Transportations table + all child bridges (Aliases / StoryHooks) â€” never
/// from Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="TransportationMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class TransportationRepository : EfRepository<TransportationData>
{
    public TransportationRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "transportation", t => t.Name) { }
    public TransportationRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "transportation"), "transportation", t => t.Name) { }

    private List<TransportationData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<TransportationData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<TransportationData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = TransportationMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<TransportationData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = TransportationMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new TransportationData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return TransportationMapper.LoadOne(db, guid);
    }

    public new TransportationData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "transportation" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return TransportationMapper.LoadOne(db, entity.Id);
    }

    public override List<TransportationData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return TransportationMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(TransportationData item)
    {
        var id = ParseTransportationGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveTransportationSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveTransportationSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        TransportationMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseTransportationGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveTransportationSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational ContractRepository. Reads materialize ContractData from the
/// Contracts table + bridge tables (ContractBonuses / ContractComplications) â€” never
/// from Records.Json. Writes persist via <see cref="ContractMapper"/>. Records.Json
/// is left intact (additive-only).
/// </summary>
public class ContractRepository : EfRepository<ContractData>
{
    public ContractRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "contract", c => c.Codename) { }
    public ContractRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "contract"), "contract", c => c.Codename) { }

    // Universe-epoch-invalidated mapped cache.
    private List<ContractData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<ContractData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<ContractData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ContractMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<ContractData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ContractMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new ContractData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return ContractMapper.LoadOne(db, guid);
    }

    public new ContractData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "contract" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return ContractMapper.LoadOne(db, entity.Id);
    }

    public override List<ContractData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return ContractMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(ContractData item)
    {
        var idStr = item.Id;
        var id = ParseContractGuid(idStr);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Codename ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveContractSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveContractSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        ContractMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseContractGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveContractSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational AutomatonRepository. Reads materialize AutomatonData from the
/// Automata table + all child bridges (Aliases / Armament / Sensors /
/// KnownDeployments / StoryHooks) â€” never from Records.Json. Writes wipe child
/// bridges and re-insert via <see cref="AutomatonMapper"/>.
/// Records.Json is left intact (additive-only). (RFC 0007)
/// </summary>
public class AutomatonRepository : EfRepository<AutomatonData>
{
    public AutomatonRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "automaton", a => a.Name) { }
    public AutomatonRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "automaton"), "automaton", a => a.Name) { }

    private List<AutomatonData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<AutomatonData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<AutomatonData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = AutomatonMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<AutomatonData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = AutomatonMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new AutomatonData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return AutomatonMapper.LoadOne(db, guid);
    }

    public new AutomatonData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "automaton" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return AutomatonMapper.LoadOne(db, entity.Id);
    }

    public override List<AutomatonData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return AutomatonMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(AutomatonData item)
    {
        var id = ParseAutomatonGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveAutomatonSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveAutomatonSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        AutomatonMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseAutomatonGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveAutomatonSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational SubsidiaryRepository. Reads materialize SubsidiaryData from the
/// Subsidiaries table + SubsidiaryProducts bridge â€” never from Records.Json. Writes
/// persist via <see cref="SubsidiaryMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class SubsidiaryRepository : EfRepository<SubsidiaryData>
{
    public SubsidiaryRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "subsidiary", s => s.Name) { }
    public SubsidiaryRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "subsidiary"), "subsidiary", s => s.Name) { }

    private List<SubsidiaryData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<SubsidiaryData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<SubsidiaryData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = SubsidiaryMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<SubsidiaryData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = SubsidiaryMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new SubsidiaryData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return SubsidiaryMapper.LoadOne(db, guid);
    }

    public new SubsidiaryData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "subsidiary" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return SubsidiaryMapper.LoadOne(db, entity.Id);
    }

    public override List<SubsidiaryData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return SubsidiaryMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(SubsidiaryData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        SubsidiaryMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational EntertainmentRepository. Reads materialize EntertainmentData from
/// the EntertainmentItems table + all child bridges (Aliases / KnownFans / StoryHooks)
/// â€” never from Records.Json. Writes persist via <see cref="EntertainmentMapper"/>.
/// Records.Json is left intact (additive-only).
/// </summary>
public class EntertainmentRepository : EfRepository<EntertainmentData>
{
    public EntertainmentRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "entertainment", e => e.Name) { }
    public EntertainmentRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "entertainment"), "entertainment", e => e.Name) { }

    private List<EntertainmentData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<EntertainmentData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<EntertainmentData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = EntertainmentMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<EntertainmentData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = EntertainmentMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new EntertainmentData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return EntertainmentMapper.LoadOne(db, guid);
    }

    public new EntertainmentData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "entertainment" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return EntertainmentMapper.LoadOne(db, entity.Id);
    }

    public override List<EntertainmentData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return EntertainmentMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(EntertainmentData item)
    {
        var id = ParseEntertainmentGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveEntertainmentSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveEntertainmentSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        EntertainmentMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseEntertainmentGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveEntertainmentSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational ApparelRepository. Reads materialize ApparelData from the
/// Apparels table + all child bridges (Aliases / Materials / WornBy / StoryHooks) â€”
/// never from Records.Json. Writes persist via <see cref="ApparelMapper"/>. Records.Json is
/// left intact (additive-only).
/// </summary>
public class ApparelRepository : EfRepository<ApparelData>
{
    public ApparelRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "apparel", a => a.Name) { }
    public ApparelRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "apparel"), "apparel", a => a.Name) { }

    private List<ApparelData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<ApparelData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<ApparelData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ApparelMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<ApparelData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ApparelMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new ApparelData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return ApparelMapper.LoadOne(db, guid);
    }

    public new ApparelData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "apparel" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return ApparelMapper.LoadOne(db, entity.Id);
    }

    public override List<ApparelData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return ApparelMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(ApparelData item)
    {
        var id = ParseApparelGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveApparelSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveApparelSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        ApparelMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseApparelGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveApparelSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational NewsRepository. Reads materialize NewsData from the News table
/// + bridge tables (NewsEntitiesInvolved / NewsLocations) â€” never from Records.Json.
/// Writes persist via <see cref="NewsMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class NewsRepository : EfRepository<NewsData>
{
    public NewsRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "news", n => n.Headline) { }
    public NewsRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "news"), "news", n => n.Headline) { }

    // Universe-epoch-invalidated mapped cache.
    private List<NewsData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<NewsData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<NewsData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = NewsMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<NewsData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = NewsMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new NewsData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return NewsMapper.LoadOne(db, guid);
    }

    public new NewsData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "news" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return NewsMapper.LoadOne(db, entity.Id);
    }

    public override List<NewsData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return NewsMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(NewsData item)
    {
        var idStr = item.Id;
        var id = ParseNewsGuid(idStr);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Headline ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveNewsSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveNewsSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        NewsMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseNewsGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveNewsSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational ArchetypeRepository. Reads materialize ArchetypeData from the
/// Archetypes table + all child bridges (WillAlways / WillNever / Unless /
/// SimilarTo / OppositeOf) â€” never from Records.Json. Writes wipe child bridges
/// and re-insert via <see cref="ArchetypeMapper"/>. Records.Json is left intact
/// (additive-only).
/// </summary>
public class ArchetypeRepository : EfRepository<ArchetypeData>
{
    public ArchetypeRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "archetype", a => a.Name) { }
    public ArchetypeRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "archetype"), "archetype", a => a.Name) { }

    private List<ArchetypeData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<ArchetypeData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<ArchetypeData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ArchetypeMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<ArchetypeData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ArchetypeMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new ArchetypeData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return ArchetypeMapper.LoadOne(db, guid);
    }

    public new ArchetypeData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "archetype" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return ArchetypeMapper.LoadOne(db, entity.Id);
    }

    public override List<ArchetypeData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return ArchetypeMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(ArchetypeData item)
    {
        var id = ParseArchetypeGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveArchetypeSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveArchetypeSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        ArchetypeMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseArchetypeGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveArchetypeSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational MaterialRepository. Reads materialize MaterialData from the
/// Materials table + all child bridges (Aliases / StoryHooks) â€” never from
/// Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="MaterialMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class MaterialRepository : EfRepository<MaterialData>
{
    public MaterialRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "material", s => s.ProductName.Length > 0 ? s.ProductName : s.Name) { }
    public MaterialRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "material"), "material", s => s.ProductName.Length > 0 ? s.ProductName : s.Name) { }

    private List<MaterialData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<MaterialData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<MaterialData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = MaterialMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<MaterialData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = MaterialMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new MaterialData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return MaterialMapper.LoadOne(db, guid);
    }

    public new MaterialData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "material" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return MaterialMapper.LoadOne(db, entity.Id);
    }

    public override List<MaterialData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return MaterialMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(MaterialData item)
    {
        var id = ParseMaterialGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.ProductName.Length > 0 ? item.ProductName : (item.Name ?? "");
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = item.Name ?? "",
                Slug       = ResolveMaterialSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, item.Name, StringComparison.Ordinal))
            {
                existingEntity.Name = item.Name ?? "";
                existingEntity.Slug = ResolveMaterialSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        MaterialMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseMaterialGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveMaterialSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational PharmaceuticalRepository. Reads materialize PharmaceuticalData from the
/// Pharmaceuticals table + all child bridges â€” never from Records.Json. Writes persist
/// via <see cref="PharmaceuticalMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class PharmaceuticalRepository : EfRepository<PharmaceuticalData>
{
    public PharmaceuticalRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "pharmaceutical", p => p.Name) { }
    public PharmaceuticalRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "pharmaceutical"), "pharmaceutical", p => p.Name) { }

    private List<PharmaceuticalData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<PharmaceuticalData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<PharmaceuticalData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = PharmaceuticalMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<PharmaceuticalData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = PharmaceuticalMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new PharmaceuticalData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return PharmaceuticalMapper.LoadOne(db, guid);
    }

    public new PharmaceuticalData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "pharmaceutical" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return PharmaceuticalMapper.LoadOne(db, entity.Id);
    }

    public override List<PharmaceuticalData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return PharmaceuticalMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(PharmaceuticalData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        PharmaceuticalMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational ConsumerGoodRepository. Reads materialize ConsumerGoodData from the
/// ConsumerGoods table + child bridges â€” never from Records.Json. Writes persist
/// via <see cref="ConsumerGoodMapper"/>. Records.Json is left intact (additive-only).
/// </summary>
public class ConsumerGoodRepository : EfRepository<ConsumerGoodData>
{
    public ConsumerGoodRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "consumer_good", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
    public ConsumerGoodRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "consumer_good"), "consumer_good", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }

    private List<ConsumerGoodData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<ConsumerGoodData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<ConsumerGoodData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ConsumerGoodMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<ConsumerGoodData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = ConsumerGoodMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new ConsumerGoodData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return ConsumerGoodMapper.LoadOne(db, guid);
    }

    public new ConsumerGoodData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "consumer_good" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return ConsumerGoodMapper.LoadOne(db, entity.Id);
    }

    public override List<ConsumerGoodData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return ConsumerGoodMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(ConsumerGoodData item)
    {
        var id = ParseGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        ConsumerGoodMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational QuoteRepository. Reads materialize QuoteData from the Quotes table â€”
/// never from Records.Json. Writes persist via <see cref="QuoteMapper"/>. Records.Json is
/// left intact (additive-only).
/// </summary>
public class QuoteRepository : EfRepository<QuoteData>
{
    public QuoteRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "quote", q => q.Quote.Length > 40 ? q.Quote[..40] : q.Quote) { }
    public QuoteRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "quote"), "quote", q => q.Quote.Length > 40 ? q.Quote[..40] : q.Quote) { }

    // Universe-epoch-invalidated mapped cache.
    private List<QuoteData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<QuoteData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<QuoteData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = QuoteMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<QuoteData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = QuoteMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new QuoteData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return QuoteMapper.LoadOne(db, guid);
    }

    public new QuoteData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "quote" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return QuoteMapper.LoadOne(db, entity.Id);
    }

    public override List<QuoteData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return QuoteMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(QuoteData item)
    {
        var idStr = item.Id;
        var id = ParseQuoteGuid(idStr);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Quote.Length > 40 ? item.Quote[..40] : item.Quote;
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = item.Quote,
                Slug       = ResolveQuoteSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            existingEntity.Name       = item.Quote;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        QuoteMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseQuoteGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveQuoteSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

// Singleton repositories â€” one JSON document each, persisted as a row in the
// universal Settings table (keyed by name). Earlier these used the path-only
// JsonSingletonRepository ctor which routed through NullFactory and silently
// returned defaults on every Get â€” fixed 2026-05-06.
public class ToneBibleRepository : JsonSingletonRepository<ToneBibleData>
{
    public ToneBibleRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "tone_bible") { }
    public ToneBibleRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "tone_bible"), "tone_bible") { }
}

public class StoryBibleRepository : JsonSingletonRepository<StoryBibleData>
{
    public StoryBibleRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "story_bible") { }
    public StoryBibleRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "story_bible"), "story_bible") { }
}

public class LiteraryRulesRepository : JsonSingletonRepository<LiteraryRulesData>
{
    public LiteraryRulesRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "literary_rules") { }
    public LiteraryRulesRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "literary_rules"), "literary_rules") { }
}

public class CharacterProfileRepository : JsonSingletonRepository<CharacterProfileData>
{
    public CharacterProfileRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "character_profile") { }
    public CharacterProfileRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "character_profile"), "character_profile") { }
}

/// <summary>
/// Fully relational LabSpecimenRepository. Reads materialize LabSpecimenData from the
/// LabSpecimens table + all child bridges (Aliases / KnownLocations / StoryHooks) â€”
/// never from Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="LabSpecimenMapper"/>. Records.Json is left intact (additive-only). (RFC 0007)
/// </summary>
public class LabSpecimenRepository : EfRepository<LabSpecimenData>
{
    public LabSpecimenRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "lab_specimen", s => s.Name) { }
    public LabSpecimenRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "lab_specimen"), "lab_specimen", s => s.Name) { }

    private List<LabSpecimenData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<LabSpecimenData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<LabSpecimenData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = LabSpecimenMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<LabSpecimenData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = LabSpecimenMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new LabSpecimenData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return LabSpecimenMapper.LoadOne(db, guid);
    }

    public new LabSpecimenData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "lab_specimen" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return LabSpecimenMapper.LoadOne(db, entity.Id);
    }

    public override List<LabSpecimenData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return LabSpecimenMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(LabSpecimenData item)
    {
        var id = ParseLabSpecimenGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveLabSpecimenSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveLabSpecimenSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        LabSpecimenMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseLabSpecimenGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveLabSpecimenSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational FlyoverEntityRepository. Reads materialize FlyoverEntityData from the
/// FlyoverEntities table + all child bridges (Aliases / KnownLocations / StoryHooks) â€”
/// never from Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="FlyoverEntityMapper"/>. Records.Json is left intact (additive-only). (RFC 0007)
/// </summary>
public class FlyoverEntityRepository : EfRepository<FlyoverEntityData>
{
    public FlyoverEntityRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "flyover_entity", w => w.Name) { }
    public FlyoverEntityRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "flyover_entity"), "flyover_entity", w => w.Name) { }

    private List<FlyoverEntityData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<FlyoverEntityData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<FlyoverEntityData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = FlyoverEntityMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<FlyoverEntityData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = FlyoverEntityMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new FlyoverEntityData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return FlyoverEntityMapper.LoadOne(db, guid);
    }

    public new FlyoverEntityData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "flyover_entity" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return FlyoverEntityMapper.LoadOne(db, entity.Id);
    }

    public override List<FlyoverEntityData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return FlyoverEntityMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(FlyoverEntityData item)
    {
        var id = ParseFlyoverEntityGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveFlyoverEntitySlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveFlyoverEntitySlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        FlyoverEntityMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseFlyoverEntityGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveFlyoverEntitySlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>
/// Fully relational PsionicRepository. Reads materialize PsionicData from the
/// Psionics table + all child bridges (Aliases / KnownPractitioners / StoryHooks) â€”
/// never from Records.Json. Writes wipe child bridges and re-insert via
/// <see cref="PsionicMapper"/>. Records.Json is left intact (additive-only). (RFC 0007)
/// </summary>
public class PsionicRepository : EfRepository<PsionicData>
{
    public PsionicRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "psionic", p => p.Name) { }
    public PsionicRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "psionic"), "psionic", p => p.Name) { }

    private List<PsionicData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<PsionicData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<PsionicData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = PsionicMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<PsionicData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = PsionicMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new PsionicData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return PsionicMapper.LoadOne(db, guid);
    }

    public new PsionicData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "psionic" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return PsionicMapper.LoadOne(db, entity.Id);
    }

    public override List<PsionicData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return PsionicMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(PsionicData item)
    {
        var id = ParsePsionicGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolvePsionicSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolvePsionicSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        PsionicMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParsePsionicGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolvePsionicSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}

/// <summary>Read access to the first-class <see cref="Species"/> taxonomy (the
/// controlled vocabulary Character.Species references). A small lookup table, not
/// a canon entity â€” kept off the Records/embedding/graph machinery on purpose
/// (separation of responsibilities, Â§2a). Cached after first read.</summary>
public class SpeciesRepository
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private List<Species>? cache;
    private readonly object gate = new();

    public SpeciesRepository(IDbContextFactory<StreetSamuraiDbContext> dbFactory) => this.dbFactory = dbFactory;

    public List<Species> GetAll()
    {
        lock (gate)
        {
            if (cache != null) return cache;
            using var db = dbFactory.CreateDbContext();
            // Tolerate a not-yet-migrated DB (table absent) by returning the
            // in-code canonical set rather than throwing.
            try { cache = db.Species.AsNoTracking().OrderBy(s => s.Name).ToList(); }
            catch { cache = Species.Canonical.ToList(); }
            if (cache.Count == 0) cache = Species.Canonical.ToList();
            return cache;
        }
    }

    public Species? GetByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var n = name.Trim().ToLowerInvariant();
        return GetAll().FirstOrDefault(s => string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The five valid species names â€” the allowed Character.Species values.</summary>
    public IReadOnlyCollection<string> ValidNames() => GetAll().Select(s => s.Name).ToList();

    public void Reload() { lock (gate) cache = null; }
}

/// <summary>
/// Fully relational SyntheticLifeRepository. Reads materialize SyntheticLifeData from
/// the SyntheticLives table + all child bridges (Aliases / KnownAssociations / StoryHooks)
/// â€” never from Records.Json. Writes persist via <see cref="SyntheticMapper"/>.
/// Records.Json is left intact (additive-only).
/// </summary>
public class SyntheticLifeRepository : EfRepository<SyntheticLifeData>
{
    public SyntheticLifeRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "synthetic", s => s.Name) { }
    public SyntheticLifeRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "synthetic"), "synthetic", s => s.Name) { }

    private List<SyntheticLifeData>? mappedCache;
    private int mappedCacheEpoch = -1;
    private readonly object mappedCacheLock = new();

    private List<SyntheticLifeData>? mappedCacheLite;
    private int mappedCacheLiteEpoch = -1;
    private readonly object mappedCacheLiteLock = new();

    public override List<SyntheticLifeData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null && mappedCacheEpoch == UniverseScope.Epoch) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = SyntheticMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) { mappedCache = loaded; mappedCacheEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public List<SyntheticLifeData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null && mappedCacheLiteEpoch == UniverseScope.Epoch) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = SyntheticMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) { mappedCacheLite = loaded; mappedCacheLiteEpoch = UniverseScope.Epoch; }
        return loaded;
    }

    public new SyntheticLifeData? GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!Guid.TryParse(id, out var guid))
        {
            if (id.Length == 32 && Guid.TryParseExact(id, "N", out guid)) { /* ok */ }
            else return null;
        }
        using var db = dbFactory.CreateDbContext();
        return SyntheticMapper.LoadOne(db, guid);
    }

    public new SyntheticLifeData? GetBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        using var db = dbFactory.CreateDbContext();
        var entity = db.Entities.AsNoTracking()
            .FirstOrDefault(e => e.EntityType == "synthetic" && e.IsActive && e.Slug == slug);
        if (entity == null) return null;
        return SyntheticMapper.LoadOne(db, entity.Id);
    }

    public override List<SyntheticLifeData> GetAllIncludingArchived()
    {
        using var db = dbFactory.CreateDbContext();
        return SyntheticMapper.LoadAll(db, includeArchived: true);
    }

    private void InvalidateMappedCache()
    {
        lock (mappedCacheLock) mappedCache = null;
        lock (mappedCacheLiteLock) mappedCacheLite = null;
    }

    public override void Save(SyntheticLifeData item)
    {
        var id = ParseSyntheticGuid(item.Id);
        if (string.IsNullOrEmpty(item.Id)) item.Id = id.ToString("N");

        using var db = dbFactory.CreateDbContext();

        var name = item.Name ?? "";
        var existingEntity = db.Entities.FirstOrDefault(e => e.Id == id);
        if (existingEntity == null)
        {
            existingEntity = new Entity
            {
                Id         = id,
                EntityType = entityType,
                Name       = name,
                Slug       = ResolveSyntheticSlug(db, name, id, currentSlug: null),
                Status     = "canon",
                Description = item.Description,
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else
        {
            if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
            {
                existingEntity.Name = name;
                existingEntity.Slug = ResolveSyntheticSlug(db, name, id, existingEntity.Slug);
            }
            if (!string.Equals(existingEntity.Description, item.Description, StringComparison.Ordinal))
                existingEntity.Description = item.Description;
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        SyntheticMapper.PersistAsync(db, id, item).GetAwaiter().GetResult();
        FactionMapper.SyncTagsForEntity(db, id, item.Tags);
        db.SaveChanges();

        InvalidateCacheExternal();
        InvalidateMappedCache();
        RaiseOnItemSaved(name);
    }

    public new void Reload()
    {
        base.Reload();
        InvalidateMappedCache();
    }

    public override void Delete(string name)
    {
        base.Delete(name);
        InvalidateMappedCache();
    }

    private static Guid ParseSyntheticGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    private string ResolveSyntheticSlug(StreetSamuraiDbContext db, string name, Guid id, string? currentSlug)
    {
        var plain = WorldGraphService.Slugify(name);
        var disambig = $"{plain}-{id:N}";
        if (!string.IsNullOrEmpty(currentSlug)
            && string.Equals(currentSlug, disambig, StringComparison.Ordinal))
            return currentSlug;
        var collision = db.Entities.Any(e =>
            e.EntityType == entityType && e.Slug == plain && e.Id != id);
        return collision ? disambig : plain;
    }
}
