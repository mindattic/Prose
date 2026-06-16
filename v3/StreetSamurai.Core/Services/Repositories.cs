using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

// ──────────────────────────────────────────────────────────────────────────────
// EF-backed repositories — total conversion off JsonDirectoryRepository.
// Public surface is unchanged (GetAll / GetById / GetByName / GetBySlug / Save /
// Delete / Reload / Count / OnItemSaved / RepoName / GetExportEntries) so every
// existing consumer compiles. Storage = StreetSamurai SQL Server database.
//
// Each Repository is a thin specialization that supplies (entityType, nameSelector)
// to EfRepository<T>. The legacy `IPathProvider` ctor is preserved so unit-test
// fixtures that constructed repos directly continue to compile; in production the
// DbContext factory is injected by DI and used for real SQL persistence.
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Fully relational CharacterRepository. Reads materialize CharacterData from
/// the Characters table + every child bridge (Aliases / StoryHooks /
/// PsychologyTraits / SpeechPhrases / BehavioralRules + Maps / StatScalars +
/// Phrases / PhysicalMarks / TerritoryZones + Reputations / BelongingsGear +
/// Extras / BioBatteryThresholds / NeuralAbilities / Changelog / Cyberware /
/// Knowledge + KnowledgeEntities / Conditions / Relationships / Timeline +
/// TimelineBodyChanges) — never from Records.Json. Writes wipe child bridges
/// and re-insert via <see cref="CharacterMapper"/>.
/// </summary>
public class CharacterRepository : EfRepository<CharacterData>
{
    public CharacterRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "character", c => c.Name) { }
    public CharacterRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "character"), "character", c => c.Name) { }

    // CharacterMapper.LoadAll fans out into ~25 Include collections × 1240
    // characters and is the slowest read in the app (~50–80 s cold). Cache the
    // result here — invalidated by Save() and the OnItemSaved hook on the
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
    // Role / Status / Tags / Rating / VoteCount) — fields beyond that read as
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
    /// character — ~50 ms) instead of materialising every character first.
    /// Required for the lite-list-then-Edit flow: the dictionary list shows
    /// the lite projection; clicking a row re-fetches the full record here.
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
        return CharacterMapper.LoadOneFromReadModel(db, guid);
    }

    public override List<CharacterData> GetAllIncludingArchived()
    {
        // Archived view bypasses the cache — it's used by audit/restore flows
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
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name = name;
            existingEntity.Slug = ResolveCharacterSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }

        // Persist column + bridge state via the mapper (sync wrapper around the
        // async API — Save is a synchronous repository contract).
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
    /// universal Tag/EntityTag tables are the source of truth — this only adds,
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
            // brand-new Tag rows) on the caller's single SaveChanges — no more
            // one-commit-per-tag inside the loop.
            db.EntityTags.Add(new EntityTag { EntityId = entityId, Tag = tag });
        }
    }
}

public class CorponationRepository : EfRepository<CorponationData>
{
    public CorponationRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "corponation", c => c.Name) { }
    public CorponationRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "corponation"), "corponation", c => c.Name) { }
}

public class DistrictRepository : EfRepository<DistrictData>
{
    public DistrictRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "place", d => d.Name) { }
    public DistrictRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "place"), "place", d => d.Name) { }
}

/// <summary>
/// Fully relational FactionRepository. Reads materialize FactionData from the
/// Factions table + all child bridges (Aliases / Methods / Resources / Goals /
/// StoryHooks / Relationships + RelationshipTags / Members) — never from
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveFactionSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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

public class WorldbuildingDocRepository : EfRepository<WorldbuildingDocument>
{
    public WorldbuildingDocRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "document", d => d.FileName) { }
    public WorldbuildingDocRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "document"), "document", d => d.FileName) { }
}

/// <summary>
/// Fully relational MotifRepository. Reads materialize MotifData from the
/// Motifs table + MotifAppearances bridge — never from Records.Json.
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveMotifSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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

public class WeaponryRepository : EfRepository<WeaponryData>
{
    public WeaponryRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "weapon", w => w.Name) { }
    public WeaponryRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "weapon"), "weapon", w => w.Name) { }
}

/// <summary>
/// Fully relational AmmunitionRepository. Reads materialize AmmunitionData from the
/// Ammunitions table + all child bridges (Aliases / CompatibleWeapons / Variants /
/// StoryHooks) — never from Records.Json. Writes wipe child bridges and re-insert via
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveAmmunitionSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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

public class EquipmentRepository : EfRepository<EquipmentData>
{
    public EquipmentRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "equipment", e => e.ProductName.Length > 0 ? e.ProductName : e.Name) { }
    public EquipmentRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "equipment"), "equipment", e => e.ProductName.Length > 0 ? e.ProductName : e.Name) { }
}

public class TechnologyRepository : EfRepository<TechnologyData>
{
    public TechnologyRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "technology", t => t.ProductName.Length > 0 ? t.ProductName : t.Name) { }
    public TechnologyRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "technology"), "technology", t => t.ProductName.Length > 0 ? t.ProductName : t.Name) { }
}

public class CyberwareRepository : EfRepository<CyberwareData>
{
    public CyberwareRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "cyberware", c => c.ProductName.Length > 0 ? c.ProductName : c.Name) { }
    public CyberwareRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "cyberware"), "cyberware", c => c.ProductName.Length > 0 ? c.ProductName : c.Name) { }
}

/// <summary>
/// Fully relational VocabularyRepository. Reads materialize VocabularyData from the
/// VocabularyEntries table — never from Records.Json. Writes persist columns via
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
/// Genemods table + all child bridges (Aliases / StoryHooks) — never from
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, item.Name, StringComparison.Ordinal))
        {
            existingEntity.Name       = item.Name ?? "";
            existingEntity.Slug       = ResolveGenemodSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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
/// from the Transportations table + all child bridges (Aliases / StoryHooks) — never
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveTransportationSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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
/// Contracts table + bridge tables (ContractBonuses / ContractComplications) — never
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveContractSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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
/// KnownDeployments / StoryHooks) — never from Records.Json. Writes wipe child
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveAutomatonSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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

public class SubsidiaryRepository : EfRepository<SubsidiaryData>
{
    public SubsidiaryRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "subsidiary", s => s.Name) { }
    public SubsidiaryRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "subsidiary"), "subsidiary", s => s.Name) { }
}

public class EntertainmentRepository : EfRepository<EntertainmentData>
{
    public EntertainmentRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "entertainment", e => e.Name) { }
    public EntertainmentRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "entertainment"), "entertainment", e => e.Name) { }
}

public class ApparelRepository : EfRepository<ApparelData>
{
    public ApparelRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "apparel", a => a.Name) { }
    public ApparelRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "apparel"), "apparel", a => a.Name) { }
}

/// <summary>
/// Fully relational NewsRepository. Reads materialize NewsData from the News table
/// + bridge tables (NewsEntitiesInvolved / NewsLocations) — never from Records.Json.
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
/// SimilarTo / OppositeOf) — never from Records.Json. Writes wipe child bridges
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, name, StringComparison.Ordinal))
        {
            existingEntity.Name       = name;
            existingEntity.Slug       = ResolveArchetypeSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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
/// Materials table + all child bridges (Aliases / StoryHooks) — never from
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
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            db.Entities.Add(existingEntity);
        }
        else if (!string.Equals(existingEntity.Name, item.Name, StringComparison.Ordinal))
        {
            existingEntity.Name       = item.Name ?? "";
            existingEntity.Slug       = ResolveMaterialSlug(db, name, id, existingEntity.Slug);
            existingEntity.ModifiedAt = DateTime.UtcNow;
        }
        else
        {
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

public class PharmaceuticalRepository : EfRepository<PharmaceuticalData>
{
    public PharmaceuticalRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "pharmaceutical", p => p.Name) { }
    public PharmaceuticalRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "pharmaceutical"), "pharmaceutical", p => p.Name) { }
}

public class ConsumerGoodRepository : EfRepository<ConsumerGoodData>
{
    public ConsumerGoodRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "consumer_good", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
    public ConsumerGoodRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "consumer_good"), "consumer_good", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
}

/// <summary>
/// Fully relational QuoteRepository. Reads materialize QuoteData from the Quotes table —
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

// Singleton repositories — one JSON document each, persisted as a row in the
// universal Settings table (keyed by name). Earlier these used the path-only
// JsonSingletonRepository ctor which routed through NullFactory and silently
// returned defaults on every Get — fixed 2026-05-06.
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
/// LabSpecimens table + all child bridges (Aliases / KnownLocations / StoryHooks) —
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
/// FlyoverEntities table + all child bridges (Aliases / KnownLocations / StoryHooks) —
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
/// Psionics table + all child bridges (Aliases / KnownPractitioners / StoryHooks) —
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
/// a canon entity — kept off the Records/embedding/graph machinery on purpose
/// (separation of responsibilities, §2a). Cached after first read.</summary>
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

    /// <summary>The five valid species names — the allowed Character.Species values.</summary>
    public IReadOnlyCollection<string> ValidNames() => GetAll().Select(s => s.Name).ToList();

    public void Reload() { lock (gate) cache = null; }
}
