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
    private readonly object mappedCacheLock = new();

    public override List<CharacterData> GetAll()
    {
        lock (mappedCacheLock)
        {
            if (mappedCache != null) return mappedCache;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CharacterMapper.LoadAll(db, includeArchived: false);
        lock (mappedCacheLock) mappedCache = loaded;
        return loaded;
    }

    // List-view-only cache. Contains lightweight CharacterData (Id / Name /
    // Role / Status / Tags / Rating / VoteCount) — fields beyond that read as
    // empty defaults. Use this for dictionary/list/filter UIs and re-fetch the
    // full record via GetById when a row is opened for edit.
    private List<CharacterData>? mappedCacheLite;
    private readonly object mappedCacheLiteLock = new();

    public List<CharacterData> GetAllLite()
    {
        lock (mappedCacheLiteLock)
        {
            if (mappedCacheLite != null) return mappedCacheLite;
        }
        using var db = dbFactory.CreateDbContext();
        var loaded = CharacterMapper.LoadAllLite(db);
        lock (mappedCacheLiteLock) mappedCacheLite = loaded;
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
        return CharacterMapper.LoadOne(db, guid);
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

public class FactionRepository : EfRepository<FactionData>
{
    public FactionRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "faction", f => f.Name) { }
    public FactionRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "faction"), "faction", f => f.Name) { }
}

public class WorldbuildingDocRepository : EfRepository<WorldbuildingDocument>
{
    public WorldbuildingDocRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "document", d => d.FileName) { }
    public WorldbuildingDocRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "document"), "document", d => d.FileName) { }
}

public class MotifRepository : EfRepository<MotifData>
{
    public MotifRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "motif", m => m.Name) { }
    public MotifRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "motif"), "motif", m => m.Name) { }
}

public class WeaponryRepository : EfRepository<WeaponryData>
{
    public WeaponryRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "weapon", w => w.Name) { }
    public WeaponryRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "weapon"), "weapon", w => w.Name) { }
}

public class AmmunitionRepository : EfRepository<AmmunitionData>
{
    public AmmunitionRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "ammunition", a => a.Name) { }
    public AmmunitionRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "ammunition"), "ammunition", a => a.Name) { }
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

public class VocabularyRepository : EfRepository<VocabularyData>
{
    public VocabularyRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "vocabulary", v => v.Term) { }
    public VocabularyRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "vocabulary"), "vocabulary", v => v.Term) { }
}

public class SyntheticLifeRepository : EfRepository<SyntheticLifeData>
{
    public SyntheticLifeRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "synthetic", s => s.Name) { }
    public SyntheticLifeRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "synthetic"), "synthetic", s => s.Name) { }
}

public class GenemodRepository : EfRepository<GenemodData>
{
    public GenemodRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "genemod", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
    public GenemodRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "genemod"), "genemod", g => g.ProductName.Length > 0 ? g.ProductName : g.Name) { }
}

public class TransportationRepository : EfRepository<TransportationData>
{
    public TransportationRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "transportation", t => t.Name) { }
    public TransportationRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "transportation"), "transportation", t => t.Name) { }
}

public class ContractRepository : EfRepository<ContractData>
{
    public ContractRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "contract", c => c.Codename) { }
    public ContractRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "contract"), "contract", c => c.Codename) { }
}

public class AutomatonRepository : EfRepository<AutomatonData>
{
    public AutomatonRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "automaton", a => a.Name) { }
    public AutomatonRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "automaton"), "automaton", a => a.Name) { }
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

public class NewsRepository : EfRepository<NewsData>
{
    public NewsRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "news", n => n.Headline) { }
    public NewsRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "news"), "news", n => n.Headline) { }
}

public class ArchetypeRepository : EfRepository<ArchetypeData>
{
    public ArchetypeRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "archetype", a => a.Name) { }
    public ArchetypeRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "archetype"), "archetype", a => a.Name) { }
}

public class MaterialRepository : EfRepository<MaterialData>
{
    public MaterialRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "material", s => s.ProductName.Length > 0 ? s.ProductName : s.Name) { }
    public MaterialRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "material"), "material", s => s.ProductName.Length > 0 ? s.ProductName : s.Name) { }
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

public class QuoteRepository : EfRepository<QuoteData>
{
    public QuoteRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "quote", q => q.Quote.Length > 40 ? q.Quote[..40] : q.Quote) { }
    public QuoteRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "quote"), "quote", q => q.Quote.Length > 40 ? q.Quote[..40] : q.Quote) { }
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

public class LabSpecimenRepository : EfRepository<LabSpecimenData>
{
    public LabSpecimenRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "lab_specimen", s => s.Name) { }
    public LabSpecimenRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "lab_specimen"), "lab_specimen", s => s.Name) { }
}

public class FlyoverEntityRepository : EfRepository<FlyoverEntityData>
{
    public FlyoverEntityRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "flyover_entity", w => w.Name) { }
    public FlyoverEntityRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "flyover_entity"), "flyover_entity", w => w.Name) { }
}

public class PsionicRepository : EfRepository<PsionicData>
{
    public PsionicRepository(IDbContextFactory<StreetSamuraiDbContext> db)
        : base(db, "psionic", p => p.Name) { }
    public PsionicRepository(IPathProvider paths)
        : base(TestDbFactory.For(paths, "psionic"), "psionic", p => p.Name) { }
}
