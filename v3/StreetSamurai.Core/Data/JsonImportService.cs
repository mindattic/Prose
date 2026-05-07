using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

// Disambiguate clashing names against Services / models that share simple names.
using ContractEntity = StreetSamurai.Core.Data.Entities.Contract;
using DocumentEntity = StreetSamurai.Core.Data.Entities.Document;
using QuoteEntity    = StreetSamurai.Core.Data.Entities.Quote;
using NewsEntity     = StreetSamurai.Core.Data.Entities.News;

namespace StreetSamurai.Core.Data;

/// <summary>
/// One-shot (and idempotent) importer that walks <c>engine/data/*</c> and writes
/// rows into the SQL Server StreetSamurai database. Foundation pass covers
/// <see cref="ImportCharactersAsync"/>; subsequent entity types follow the same
/// pattern (Place, Faction, Corponation, Weapon, …).
///
/// Idempotence: every row is keyed by the JSON file's existing <c>id</c> guid7,
/// so re-running upserts in place. Safe to run before/during/after a writing
/// session without duplicating data.
/// </summary>
public class JsonImportService
{
    private readonly StreetSamuraiDbContext db;
    private readonly CharacterRepository characters;
    private readonly DistrictRepository districts;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;
    private readonly SubsidiaryRepository subsidiaries;
    private readonly SyntheticLifeRepository synthetics;
    private readonly AutomatonRepository automata;
    private readonly WeaponryRepository weapons;
    private readonly EquipmentRepository equipment;
    private readonly CyberwareRepository cyberware;
    private readonly ApparelRepository apparel;
    private readonly AmmunitionRepository ammunition;
    private readonly PharmaceuticalRepository pharma;
    private readonly GenemodRepository genemods;
    private readonly MaterialRepository materials;
    private readonly TransportationRepository transports;
    private readonly ConsumerGoodRepository consumerGoods;
    private readonly ArchetypeRepository archetypes;
    private readonly QuoteRepository quotes;
    private readonly NewsRepository news;
    private readonly ContractRepository contracts;
    private readonly WorldbuildingDocRepository documents;
    private readonly VocabularyRepository vocabulary;
    private readonly LabSpecimenRepository labSpecimens;
    private readonly PsionicRepository psionics;

    private readonly IPathProvider paths;
    private readonly ILogger<JsonImportService> log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
    };

    public JsonImportService(
        StreetSamuraiDbContext db,
        CharacterRepository characters,
        DistrictRepository districts,
        FactionRepository factions,
        CorponationRepository corponations,
        SubsidiaryRepository subsidiaries,
        SyntheticLifeRepository synthetics,
        AutomatonRepository automata,
        WeaponryRepository weapons,
        EquipmentRepository equipment,
        CyberwareRepository cyberware,
        ApparelRepository apparel,
        AmmunitionRepository ammunition,
        PharmaceuticalRepository pharma,
        GenemodRepository genemods,
        MaterialRepository materials,
        TransportationRepository transports,
        ConsumerGoodRepository consumerGoods,
        ArchetypeRepository archetypes,
        QuoteRepository quotes,
        NewsRepository news,
        ContractRepository contracts,
        WorldbuildingDocRepository documents,
        VocabularyRepository vocabulary,
        LabSpecimenRepository labSpecimens,
        PsionicRepository psionics,
        IPathProvider paths,
        ILogger<JsonImportService> log)
    {
        this.db = db;
        this.characters = characters;
        this.districts = districts;
        this.factions = factions;
        this.corponations = corponations;
        this.subsidiaries = subsidiaries;
        this.synthetics = synthetics;
        this.automata = automata;
        this.weapons = weapons;
        this.equipment = equipment;
        this.cyberware = cyberware;
        this.apparel = apparel;
        this.ammunition = ammunition;
        this.pharma = pharma;
        this.genemods = genemods;
        this.materials = materials;
        this.transports = transports;
        this.consumerGoods = consumerGoods;
        this.archetypes = archetypes;
        this.quotes = quotes;
        this.news = news;
        this.contracts = contracts;
        this.documents = documents;
        this.vocabulary = vocabulary;
        this.labSpecimens = labSpecimens;
        this.psionics = psionics;
        this.paths = paths;
        this.log = log;
    }

    /// <summary>
    /// Walk every file in <c>engine/data/people/</c> and project each into Entity +
    /// Character + Knowledge/Conditions/Cyberware/Relationship/Timeline rows. Returns
    /// counts so the CLI can show progress; the full audit lives in SystemVersioning.
    /// </summary>
    public async Task<JsonImportResult> ImportCharactersAsync(CancellationToken ct = default)
    {
        var result = new JsonImportResult();
        // Skip placeholder JSONs (no Name) — they would land as blank Entity rows.
        var all = LoadFromJsonDir<CharacterData>("people")
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();
        result.SourceCount = all.Count;

        foreach (var c in all)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                await UpsertCharacterAsync(c, ct);
                // Save per character — Entity + Character row + child collections + Tags
                // commit as one atomic unit, so one bad row doesn't poison the whole batch
                // and subsequent queries see committed state instead of dirty change tracker.
                await db.SaveChangesAsync(ct);
                result.Imported++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Character import failed for {Name} ({Id})", c.Name, c.Id);
                result.Errors.Add($"{c.Name}: {DeepestMessage(ex)}");
                // Detach pending changes so the next character starts clean.
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged).ToList())
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
        }

        return result;
    }

    private async Task UpsertCharacterAsync(CharacterData src, CancellationToken ct)
    {
        var id = ParseGuid(src.Id);
        await UpsertEntityAsync(id, "character", src.Name, src.Description, src.Tags, ct, src);

        // Delegate the entire columnar Character write to the canonical mapper.
        // PersistAsync handles: column scalars (incl. Name, parsed name parts),
        // every bridge table (Aliases / StoryHooks / 25 others), and the
        // resolved-FK bridges (HomeTurfs, Affiliations).
        // Earlier this method had its own duplicate copy of the scalar
        // assignments which forgot to set Name — leaving every row blank.
        await CharacterMapper.PersistAsync(db, id, src, ct);

        // Set KindOfBeing — derived from Type / Species / facets, not in CharacterData directly.
        var ch = await db.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (ch != null) ch.KindOfBeing = ResolveKindOfBeing(src);

        // Tags — universal layer
        await UpsertTagsAsync(id, src.Tags, ct);
    }

    /// <summary>Push a List&lt;string&gt; into a bucket bridge using the provided row factory.</summary>
    private void AddListBucket<TRow>(Guid id, IReadOnlyList<string>? items, string bucket, Func<Guid, int, string, TRow> make)
        where TRow : class
    {
        if (items == null || items.Count == 0) return;
        for (int i = 0; i < items.Count; i++)
            db.Set<TRow>().Add(make(id, i, items[i] ?? ""));
    }

    /// <summary>
    /// Push a Dict&lt;string, JsonElement&gt; into CharacterStatScalars with polymorphic
    /// value columns. ValueKind preserves the original JSON type so a number like 7
    /// round-trips as a number, not the string "7".
    /// </summary>
    private void AddStatBucket(Guid id, Dictionary<string, JsonElement>? bucket, string bucketName)
    {
        if (bucket == null) return;
        foreach (var (key, el) in bucket)
        {
            var row = new CharacterStatScalar { CharacterId = id, Bucket = bucketName, KeyName = key };
            switch (el.ValueKind)
            {
                case JsonValueKind.Number:
                    row.ValueKind = "number";
                    if (el.TryGetDouble(out var n)) row.ValueNumber = n;
                    row.ValueText = el.ToString();
                    break;
                case JsonValueKind.String:
                    row.ValueKind = "string";
                    row.ValueText = el.GetString() ?? "";
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    row.ValueKind = "bool";
                    row.ValueBool = el.GetBoolean();
                    row.ValueText = row.ValueBool.Value ? "true" : "false";
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    row.ValueKind = "null";
                    break;
                default:
                    // Array or Object — preserve the raw JSON in ValueText. Better
                    // than dropping it, and any future query can parse a single
                    // narrow column instead of an entire DataJson dump.
                    row.ValueKind = el.ValueKind == JsonValueKind.Array ? "array" : "object";
                    row.ValueText = el.GetRawText();
                    break;
            }
            db.CharacterStatScalars.Add(row);
        }
    }

    private async Task UpsertTagsAsync(Guid entityId, IReadOnlyList<string> tags, CancellationToken ct)
    {
        if (tags.Count == 0) return;
        var existingNames = await db.EntityTags
            .Where(t => t.EntityId == entityId)
            .Select(t => new { t.TagId, t.Tag!.Name })
            .ToListAsync(ct);
        var existingSet = existingNames.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var tagName in tags.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (existingSet.Contains(tagName)) continue;
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == tagName, ct);
            if (tag == null)
            {
                tag = new Tag { Name = tagName };
                db.Tags.Add(tag);
                await db.SaveChangesAsync(ct);
            }
            db.EntityTags.Add(new EntityTag { EntityId = entityId, TagId = tag.Id });
        }
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.CreateVersion7();
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        // Deterministic hash for non-GUID strings — same input always maps to the
        // same Guid so save/load round-trip works for short/legacy ids and re-imports
        // upsert the same row instead of inserting duplicates.
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }

    /// <summary>
    /// Read every <c>*.json</c> in <paramref name="subdir"/> under engine_data and
    /// deserialize to <typeparamref name="T"/>. The importer must read JSON
    /// directly from disk — the EF-backed repositories return rows from SQL,
    /// which is empty until this importer populates it (chicken-and-egg).
    /// </summary>
    /// <summary>Walk the InnerException chain to the root cause — EF wraps SQL errors twice.</summary>
    private static string DeepestMessage(Exception ex)
    {
        var e = ex;
        while (e.InnerException != null) e = e.InnerException;
        return e.Message;
    }

    private List<T> LoadFromJsonDir<T>(string subdir) where T : class
    {
        var dir = Path.Combine(paths.EngineDataDir, subdir);
        if (!Directory.Exists(dir)) return new();
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        var list = new List<T>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var raw = File.ReadAllText(file);
                var item = JsonSerializer.Deserialize<T>(raw, opts);
                if (item != null) list.Add(item);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to deserialize {File} as {Type}", file, typeof(T).Name);
            }
        }
        return list;
    }

    /// <summary>Load Book domain objects from <c>engine_data/books/*.json</c> directly.</summary>
    private List<StreetSamurai.Core.Models.Book> LoadBooksFromDisk()
    {
        var dir = Path.Combine(paths.EngineDataDir, "books");
        if (!Directory.Exists(dir)) return new();
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = new List<StreetSamurai.Core.Models.Book>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var raw = File.ReadAllText(file);
                var b = JsonSerializer.Deserialize<StreetSamurai.Core.Models.Book>(raw, opts);
                if (b != null) list.Add(b);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize {File} as Book", file); }
        }
        return list;
    }

    /// <summary>Load Chapter domain objects from <c>engine_data/chapters/{folder}/chapter.json</c>.</summary>
    private List<StreetSamurai.Core.Models.Chapter> LoadChaptersFromDisk()
    {
        var dir = Path.Combine(paths.EngineDataDir, "chapters");
        if (!Directory.Exists(dir)) return new();
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = new List<StreetSamurai.Core.Models.Chapter>();
        foreach (var sub in Directory.EnumerateDirectories(dir))
        {
            var file = Path.Combine(sub, "chapter.json");
            if (!File.Exists(file)) continue;
            try
            {
                var raw = File.ReadAllText(file);
                var c = JsonSerializer.Deserialize<StreetSamurai.Core.Models.Chapter>(raw, opts);
                if (c != null) list.Add(c);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to deserialize {File} as Chapter", file); }
        }
        return list;
    }

    private static string ResolveKindOfBeing(CharacterData src)
    {
        // Heuristic mapping from existing JSON: ai/synthetic/android → corresponding kind;
        // tag-based hints for E.L.F. and Iowan Behemoth; default to species fallback.
        var lowerSpecies = (src.Species ?? "").ToLowerInvariant();
        if (lowerSpecies == "ai")           return "ai_avatar";
        if (lowerSpecies == "android")      return "automaton";
        if (lowerSpecies == "synthetic")    return "synthetic";

        var tags = src.Tags.Select(t => t.ToLowerInvariant()).ToList();
        if (tags.Contains("e.l.f.") || tags.Contains("elf") || tags.Any(t => t.Contains("e.l.f"))) return "e_l_f";
        if (tags.Any(t => t.Contains("iowan behemoth")) || tags.Any(t => t.Contains("behemoth"))) return "iowan_behemoth";

        return "human";
    }

    // ── Generic subtype import helper ────────────────────────────────────────

    private async Task<bool> UpsertEntityAsync(
        Guid id,
        string entityType,
        string name,
        string? description,
        IReadOnlyList<string> tags,
        CancellationToken ct,
        object? sourceRecord = null,
        bool isActive = true)
    {
        var existing = await db.Entities.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (existing == null)
        {
            // Disambiguate slug collisions (different ids, same name within type)
            // by appending the full id — guid7 timestamp prefixes collide when entities
            // are bulk-generated in the same millisecond, so 8-char hash isn't enough.
            var slug = WorldGraphService.Slugify(name);
            var collides = await db.Entities.AnyAsync(
                e => e.EntityType == entityType && e.Slug == slug && e.Id != id, ct);
            if (collides) slug = $"{slug}-{id:N}";

            existing = new Entity
            {
                Id          = id,
                EntityType  = entityType,
                Name        = name,
                Slug        = slug,
                Status      = isActive ? "canon" : "archived",
                Description = description,
                TagsJson    = tags.Count > 0 ? JsonSerializer.Serialize(tags, JsonOpts) : null,
                CreatedAt   = DateTime.UtcNow,
                ModifiedAt  = DateTime.UtcNow,
                IsActive    = isActive,
                ArchivedAt  = isActive ? null : DateTime.UtcNow,
            };
            db.Entities.Add(existing);
        }
        else
        {
            existing.Name        = name;
            // Don't overwrite the slug on re-import. The first save resolved any
            // collisions (e.g. "untitled_document-{guid}") and that disambig'd slug
            // is the URL/permalink. Replacing it with the plain slug breaks the
            // unique index when another row owns the plain form.
            existing.Description = description;
            existing.TagsJson    = tags.Count > 0 ? JsonSerializer.Serialize(tags, JsonOpts) : null;
            existing.ModifiedAt  = DateTime.UtcNow;
            // Don't unset IsActive on re-import — once archived, stay archived
            // unless the active record is also imported (see archive scan order).
            if (isActive)
            {
                existing.IsActive   = true;
                existing.ArchivedAt = null;
                existing.Status     = "canon";
            }
        }

        // Always materialize the canonical Record. Reads through EfRepository<T>
        // round-trip this JSON back to the typed domain model.
        if (sourceRecord != null)
        {
            var json = JsonSerializer.Serialize(sourceRecord, sourceRecord.GetType(), JsonOpts);
            var rec = await db.Records.FirstOrDefaultAsync(r => r.EntityId == id, ct);
            if (rec == null) db.Records.Add(new Record { EntityId = id, Json = json, UpdatedAt = DateTime.UtcNow });
            else { rec.Json = json; rec.UpdatedAt = DateTime.UtcNow; }
        }

        return existing.Id == id;
    }

    private async Task<TSub> UpsertSubtypeAsync<TSub>(Guid id, Func<TSub> factory, Action<TSub> update, CancellationToken ct)
        where TSub : class
    {
        var set = db.Set<TSub>();
        var existing = await set.FindAsync(new object?[] { id }, ct);
        if (existing == null)
        {
            existing = factory();
            set.Add(existing);
        }
        update(existing);
        return existing;
    }

    private async Task<JsonImportResult> ImportSubtypeAsync<TSource, TSub>(
        IReadOnlyList<TSource> source,
        string entityType,
        Func<TSource, Guid> getId,
        Func<TSource, string> getName,
        Func<TSource, string?> getDescription,
        Func<TSource, IReadOnlyList<string>> getTags,
        Func<Guid, TSource, TSub> makeSubtype,
        Action<TSub, TSource> updateSubtype,
        CancellationToken ct,
        Action<Guid, TSource>? postUpsert = null)
        where TSub : class
    {
        // Filter out placeholder records — JSONs with empty Name/Title fields are
        // generation stubs (created when a generator crashed before naming the
        // entity). They'd produce blank Entity rows that pollute search results
        // and surface as "(empty)" in dictionaries. SourceCount tracks the kept
        // count so importer reports reflect what was actually intended for import.
        var kept = source.Where(s => !string.IsNullOrWhiteSpace(getName(s))).ToList();
        var result = new JsonImportResult { SourceCount = kept.Count };
        foreach (var item in kept)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var id = getId(item);
                await UpsertEntityAsync(id, entityType, getName(item), getDescription(item), getTags(item), ct, item);
                await UpsertSubtypeAsync<TSub>(
                    id,
                    () => makeSubtype(id, item),
                    sub => updateSubtype(sub, item),
                    ct);
                // Bridge population — for fully relational types, the per-type
                // postUpsert wipes child rows and re-inserts from the source
                // record. The parent subtype was just upserted above so it's
                // safe to reference by FK.
                postUpsert?.Invoke(id, item);
                // Save per item so one bad row (slug collision, FK violation) doesn't
                // roll back the entire batch of good rows. With ~10k entities this is
                // ~10k tiny transactions — still measured in seconds, not minutes.
                await db.SaveChangesAsync(ct);
                result.Imported++;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "{EntityType} import failed for item", entityType);
                result.Errors.Add(DeepestMessage(ex));
                // Discard the failed entity from the change tracker so subsequent items
                // don't carry the bad state into their SaveChanges.
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged).ToList())
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
        }
        return result;
    }

    // ── Per-type imports ─────────────────────────────────────────────────────

    public Task<JsonImportResult> ImportPlacesAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<DistrictData, Place>(
            LoadFromJsonDir<DistrictData>("places"), "place",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Place { Id = id },
            (sub, x) => {
                // Scalars on Place. Bridges populated below in PostUpsert.
                sub.Name       = x.Name ?? "";
                sub.Description    = x.Description ?? "";
                sub.Demographics   = x.Demographics ?? "";
                sub.Economy        = x.Economy ?? "";
                sub.PowerStructure = x.PowerStructure ?? "";
                sub.Rating         = x.Rating;
                sub.VoteCount      = x.VoteCount;
                sub.AtmosphereFeel = x.Atmosphere?.Feel ?? "";
                sub.GeoLat         = x.Coordinates?.Lat ?? 0;
                sub.GeoLng         = x.Coordinates?.Lng ?? 0;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt     = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: PopulatePlaceBridges);

    private void PopulatePlaceBridges(Guid id, DistrictData x)
    {
        // Wipe-then-insert is fine for re-import: the Entity row pre-exists, the
        // Place row was just upserted by ImportSubtypeAsync. Cascade handles the
        // bridge children.
        db.PlaceAliases.RemoveRange(db.PlaceAliases.Where(r => r.PlaceId == id));
        db.PlaceDangers.RemoveRange(db.PlaceDangers.Where(r => r.PlaceId == id));
        db.PlaceOpportunities.RemoveRange(db.PlaceOpportunities.Where(r => r.PlaceId == id));
        db.PlaceStoryHooks.RemoveRange(db.PlaceStoryHooks.Where(r => r.PlaceId == id));
        db.PlaceAtmosphereItems.RemoveRange(db.PlaceAtmosphereItems.Where(r => r.PlaceId == id));
        db.PlaceAdjacencies.RemoveRange(db.PlaceAdjacencies.Where(r => r.PlaceId == id));
        db.PlaceExits.RemoveRange(db.PlaceExits.Where(r => r.PlaceId == id));
        db.PlaceFrequentedBy.RemoveRange(db.PlaceFrequentedBy.Where(r => r.PlaceId == id));
        db.PlaceNotableLocations.RemoveRange(db.PlaceNotableLocations.Where(r => r.PlaceId == id));
        db.PlaceRelatedEntities.RemoveRange(db.PlaceRelatedEntities.Where(r => r.PlaceId == id));

        for (int i = 0; i < x.Aliases.Count; i++)
            db.PlaceAliases.Add(new PlaceAlias { PlaceId = id, Position = i, Value = x.Aliases[i] ?? "" });
        for (int i = 0; i < x.Dangers.Count; i++)
            db.PlaceDangers.Add(new PlaceDanger { PlaceId = id, Position = i, Danger = x.Dangers[i] ?? "" });
        for (int i = 0; i < x.Opportunities.Count; i++)
            db.PlaceOpportunities.Add(new PlaceOpportunity { PlaceId = id, Position = i, Opportunity = x.Opportunities[i] ?? "" });
        for (int i = 0; i < x.StoryHooks.Count; i++)
            db.PlaceStoryHooks.Add(new PlaceStoryHook { PlaceId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });

        var atm = x.Atmosphere;
        if (atm != null)
        {
            for (int i = 0; i < atm.Sights.Count; i++)
                db.PlaceAtmosphereItems.Add(new PlaceAtmosphereItem { PlaceId = id, Bucket = "sights", Position = i, Item = atm.Sights[i] ?? "" });
            for (int i = 0; i < atm.Sounds.Count; i++)
                db.PlaceAtmosphereItems.Add(new PlaceAtmosphereItem { PlaceId = id, Bucket = "sounds", Position = i, Item = atm.Sounds[i] ?? "" });
            for (int i = 0; i < atm.Smells.Count; i++)
                db.PlaceAtmosphereItems.Add(new PlaceAtmosphereItem { PlaceId = id, Bucket = "smells", Position = i, Item = atm.Smells[i] ?? "" });
        }

        var conn = x.Connections;
        if (conn != null)
        {
            for (int i = 0; i < conn.AdjacentTo.Count; i++)
            {
                var name = conn.AdjacentTo[i] ?? "";
                db.PlaceAdjacencies.Add(new PlaceAdjacency
                {
                    PlaceId = id, Position = i, Alias = name,
                    NeighborId = ResolveEntityIdByName("place", name),
                });
            }
            for (int i = 0; i < conn.Exits.Count; i++)
            {
                var ex = conn.Exits[i];
                db.PlaceExits.Add(new PlaceExitRow
                {
                    PlaceId = id, Position = i,
                    Direction = ex.Direction ?? "", DestinationAlias = ex.Destination ?? "",
                    DestinationId = ResolveEntityIdByName("place", ex.Destination ?? ""),
                    ExitType = string.IsNullOrEmpty(ex.Type) ? "road" : ex.Type,
                    Description = ex.Description ?? "",
                    Restricted = ex.Restricted, DangerLevel = ex.DangerLevel,
                });
            }
        }

        for (int i = 0; i < x.FrequentedBy.Count; i++)
        {
            var name = x.FrequentedBy[i] ?? "";
            db.PlaceFrequentedBy.Add(new PlaceFrequentBy
            {
                PlaceId = id, Position = i, Alias = name,
                TargetEntityId = ResolveEntityIdAnyType(name),
            });
        }
        for (int i = 0; i < x.NotableLocations.Count; i++)
        {
            var nl = x.NotableLocations[i];
            db.PlaceNotableLocations.Add(new PlaceNotableLocation
            {
                PlaceId = id, Position = i,
                LocationName = nl.Name ?? "", Description = nl.Description ?? "",
            });
        }
        for (int i = 0; i < x.RelatedEntities.Count; i++)
        {
            var name = x.RelatedEntities[i] ?? "";
            db.PlaceRelatedEntities.Add(new PlaceRelatedEntity
            {
                PlaceId = id, Position = i, Alias = name,
                RelatedEntityId = ResolveEntityIdAnyType(name),
            });
        }
    }

    public Task<JsonImportResult> ImportFactionsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<FactionData, Faction>(
            LoadFromJsonDir<FactionData>("factions"), "faction",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Faction { Id = id },
            (sub, x) => {
                sub.Name          = x.Name ?? "";
                sub.Motto             = x.Motto ?? "";
                sub.Description       = x.Description ?? "";
                sub.Ideology          = x.Ideology ?? "";
                sub.Territory         = x.Territory ?? "";
                sub.Leadership        = x.Leadership ?? "";
                sub.NarrativeFunction = x.NarrativeFunction ?? "";
                sub.Rating            = x.Rating;
                sub.VoteCount         = x.VoteCount;
                sub.MidjourneyPrompt  = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt      = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: PopulateFactionBridges);

    private void PopulateFactionBridges(Guid id, FactionData x)
    {
        db.FactionAliases.RemoveRange(db.FactionAliases.Where(r => r.FactionId == id));
        db.FactionMethods.RemoveRange(db.FactionMethods.Where(r => r.FactionId == id));
        db.FactionResources.RemoveRange(db.FactionResources.Where(r => r.FactionId == id));
        db.FactionGoals.RemoveRange(db.FactionGoals.Where(r => r.FactionId == id));
        db.FactionStoryHooks.RemoveRange(db.FactionStoryHooks.Where(r => r.FactionId == id));
        db.FactionRelationships.RemoveRange(db.FactionRelationships.Where(r => r.FactionId == id));
        db.FactionMembers.RemoveRange(db.FactionMembers.Where(r => r.FactionId == id));

        for (int i = 0; i < x.Aliases.Count; i++)
            db.FactionAliases.Add(new FactionAlias { FactionId = id, Position = i, Value = x.Aliases[i] ?? "" });
        for (int i = 0; i < x.Methods.Count; i++)
            db.FactionMethods.Add(new FactionMethod { FactionId = id, Position = i, Method = x.Methods[i] ?? "" });
        for (int i = 0; i < x.Resources.Count; i++)
            db.FactionResources.Add(new FactionResource { FactionId = id, Position = i, Resource = x.Resources[i] ?? "" });
        for (int i = 0; i < x.Goals.Count; i++)
            db.FactionGoals.Add(new FactionGoal { FactionId = id, Position = i, Goal = x.Goals[i] ?? "" });
        for (int i = 0; i < x.StoryHooks.Count; i++)
            db.FactionStoryHooks.Add(new FactionStoryHook { FactionId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });

        for (int i = 0; i < x.Relationships.Count; i++)
        {
            var r = x.Relationships[i];
            db.FactionRelationships.Add(new FactionRelationshipRow
            {
                FactionId = id, Position = i,
                Alias = r.Name ?? "", RelationshipType = r.Type ?? "", Description = r.Description ?? "",
                TargetFactionId = ResolveEntityIdByName("faction", r.Name ?? ""),
            });
        }
        for (int i = 0; i < x.KnownMembers.Count; i++)
        {
            var m = x.KnownMembers[i];
            db.FactionMembers.Add(new FactionMemberRow
            {
                FactionId = id, Position = i,
                Alias = m.Name ?? "", Role = m.Role ?? "", MemberStatus = string.IsNullOrEmpty(m.Status) ? "active" : m.Status,
                Notes = m.Notes ?? "",
                CharacterId = ResolveEntityIdByName("character", m.Name ?? ""),
            });
        }
    }

    private Guid? ResolveEntityIdByName(string entityType, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = StreetSamurai.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && e.IsActive
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    private Guid? ResolveEntityIdAnyType(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = StreetSamurai.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.IsActive && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    public Task<JsonImportResult> ImportCorponationsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<CorponationData, Corponation>(
            LoadFromJsonDir<CorponationData>("corponations"), "corponation",
            x => ParseGuid(x.Id), x => x.Name,
            x => string.IsNullOrEmpty(x.KeyDetail) ? x.FullText : x.KeyDetail,
            x => x.Tags,
            (id, x) => new Corponation { Id = id },
            (sub, x) => {
                sub.Sector              = x.Sector ?? "";
                sub.Tier                = "";
                sub.Headquarters        = x.SovereignTerritory ?? "";
                sub.Name            = x.Name ?? "";
                sub.FullLegalName       = x.FullLegalName ?? "";
                sub.StockDesignation    = x.StockDesignation ?? "";
                sub.SovereignTerritory  = x.SovereignTerritory ?? "";
                sub.Number              = x.Number;
                sub.Rating              = x.Rating;
                sub.VoteCount           = x.VoteCount;
                sub.Valuation           = x.Valuation ?? "";
                sub.Revenue             = x.Revenue ?? "";
                sub.Employees           = x.Employees ?? "";
                sub.FoundingStory       = x.FoundingStory ?? "";
                sub.SecurityForce       = x.SecurityForce ?? "";
                sub.KeyDetail           = x.KeyDetail ?? "";
                sub.RelationshipToBig20 = x.RelationshipToBig20 ?? "";
                sub.FullText            = x.FullText ?? "";
                sub.MidjourneyPrompt    = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt        = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) =>
            {
                db.CorponationCommonNames.RemoveRange(db.CorponationCommonNames.Where(r => r.CorponationId == id));
                for (int i = 0; i < x.CommonNames.Count; i++)
                    db.CorponationCommonNames.Add(new CorponationCommonName
                    {
                        CorponationId = id, Position = i, Value = x.CommonNames[i] ?? "",
                    });
            });

    public Task<JsonImportResult> ImportSubsidiariesAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<SubsidiaryData, Subsidiary>(
            LoadFromJsonDir<SubsidiaryData>("subsidiaries"), "subsidiary",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Subsidiary { Id = id },
            (sub, x) => {
                sub.Sector              = "";
                sub.Tier                = "";
                sub.Name            = x.Name ?? "";
                sub.ParentCorponationAlias = x.ParentCorponation ?? "";
                sub.ParentCorponationId = ResolveEntityIdByName("corponation", x.ParentCorponation ?? "");
                sub.LineOfBusiness      = x.LineOfBusiness ?? "";
                sub.Description         = x.Description ?? "";
                sub.PublicFacing        = x.PublicFacing;
                sub.Rating              = x.Rating;
                sub.VoteCount           = x.VoteCount;
                sub.MidjourneyPrompt    = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt        = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) =>
            {
                db.SubsidiaryProducts.RemoveRange(db.SubsidiaryProducts.Where(r => r.SubsidiaryId == id));
                for (int i = 0; i < x.KnownProducts.Count; i++)
                {
                    var name = x.KnownProducts[i] ?? "";
                    db.SubsidiaryProducts.Add(new SubsidiaryProduct
                    {
                        SubsidiaryId = id, Position = i, Alias = name,
                        ProductEntityId = ResolveEntityIdAnyType(name),
                    });
                }
            });

    public Task<JsonImportResult> ImportSyntheticsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<SyntheticLifeData, SyntheticLife>(
            LoadFromJsonDir<SyntheticLifeData>("synthetics"), "synthetic",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new SyntheticLife { Id = id },
            (sub, x) => {
                sub.KindOfBeing        = string.IsNullOrEmpty(x.Type) ? "synthetic" : x.Type;
                sub.Manufacturer       = "";
                sub.Tier               = "";
                sub.Name           = x.Name ?? "";
                sub.Classification     = x.Classification ?? "";
                sub.Disposition        = x.Disposition ?? "";
                sub.Habitat            = x.Habitat ?? "";
                sub.Origin             = x.Origin ?? "";
                sub.LifeStatus         = string.IsNullOrEmpty(x.Status) ? "active" : x.Status;
                sub.Description        = x.Description ?? "";
                sub.ObservedBehavior   = x.ObservedBehavior ?? "";
                sub.EncounterFrequency = x.EncounterFrequency ?? "";
                sub.ConfirmedSightings = x.ConfirmedSightings;
                sub.Location           = x.Location ?? "";
                sub.DtiRating          = x.DtiRating;
                sub.Paratechnological  = x.Paratechnological;
                sub.Rating             = x.Rating;
                sub.VoteCount          = x.VoteCount;
                sub.MidjourneyPrompt   = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt       = x.Dalle3Prompt ?? "";
                // Ceramic Man optionals.
                sub.KnownAge            = x.KnownAge;
                sub.CrackPattern        = x.CrackPattern;
                sub.CurrentRole         = x.CurrentRole;
                sub.KnownLocation       = x.KnownLocation;
                sub.DiplomaticSpecialty = x.DiplomaticSpecialty;
                sub.OperatingHistory    = x.OperatingHistory;
                sub.BehavioralNotes     = x.BehavioralNotes;
                sub.DamageHistory       = x.DamageHistory;
                sub.FaceDecoration      = x.FaceDecoration;
            },
            ct,
            postUpsert: (id, x) =>
            {
                db.SyntheticLifeAliases.RemoveRange(db.SyntheticLifeAliases.Where(r => r.SyntheticLifeId == id));
                db.SyntheticLifeStoryHooks.RemoveRange(db.SyntheticLifeStoryHooks.Where(r => r.SyntheticLifeId == id));
                db.SyntheticLifeKnownAssociations.RemoveRange(db.SyntheticLifeKnownAssociations.Where(r => r.SyntheticLifeId == id));

                for (int i = 0; i < x.Aliases.Count; i++)
                    db.SyntheticLifeAliases.Add(new SyntheticLifeAlias
                    {
                        SyntheticLifeId = id, Position = i, Value = x.Aliases[i] ?? "",
                    });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.SyntheticLifeStoryHooks.Add(new SyntheticLifeStoryHook
                    {
                        SyntheticLifeId = id, Position = i, Hook = x.StoryHooks[i] ?? "",
                    });
                if (x.KnownAssociations != null)
                    for (int i = 0; i < x.KnownAssociations.Count; i++)
                    {
                        var name = x.KnownAssociations[i] ?? "";
                        db.SyntheticLifeKnownAssociations.Add(new SyntheticLifeKnownAssociation
                        {
                            SyntheticLifeId = id, Position = i, Alias = name,
                            AssociateEntityId = ResolveEntityIdAnyType(name),
                        });
                    }
            });

    public Task<JsonImportResult> ImportAutomataAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<AutomatonData, Automaton>(
            LoadFromJsonDir<AutomatonData>("automata"), "automaton",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Automaton { Id = id },
            (sub, x) => {
                sub.KindOfBeing      = "automaton";
                sub.Manufacturer     = x.Manufacturer ?? "";
                sub.Operator         = "";
                sub.Tier             = x.TierAvailability ?? "";
                sub.Name         = x.Name ?? "";
                sub.Classification   = x.Classification ?? "";
                sub.Description      = x.Description ?? "";
                sub.Legality         = x.Legality ?? "";
                sub.AutonomyLevel    = x.AutonomyLevel ?? "";
                sub.Dimensions       = x.Dimensions ?? "";
                sub.Weight           = x.Weight ?? "";
                sub.PowerSource      = x.PowerSource ?? "";
                sub.Locomotion       = x.Locomotion ?? "";
                sub.Countermeasures  = x.Countermeasures ?? "";
                sub.CulturalContext  = x.CulturalContext ?? "";
                sub.Rating           = x.Rating;
                sub.VoteCount        = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt     = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) =>
            {
                db.AutomatonAliases.RemoveRange(db.AutomatonAliases.Where(r => r.AutomatonId == id));
                db.AutomatonArmament.RemoveRange(db.AutomatonArmament.Where(r => r.AutomatonId == id));
                db.AutomatonSensors.RemoveRange(db.AutomatonSensors.Where(r => r.AutomatonId == id));
                db.AutomatonDeployments.RemoveRange(db.AutomatonDeployments.Where(r => r.AutomatonId == id));
                db.AutomatonStoryHooks.RemoveRange(db.AutomatonStoryHooks.Where(r => r.AutomatonId == id));

                for (int i = 0; i < x.Aliases.Count; i++)
                    db.AutomatonAliases.Add(new AutomatonAlias { AutomatonId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.Armament.Count; i++)
                {
                    var name = x.Armament[i] ?? "";
                    db.AutomatonArmament.Add(new AutomatonArmament
                    {
                        AutomatonId = id, Position = i, Alias = name,
                        WeaponId = ResolveEntityIdByName("weapon", name),
                    });
                }
                for (int i = 0; i < x.Sensors.Count; i++)
                    db.AutomatonSensors.Add(new AutomatonSensor { AutomatonId = id, Position = i, SensorName = x.Sensors[i] ?? "" });
                for (int i = 0; i < x.KnownDeployments.Count; i++)
                {
                    var name = x.KnownDeployments[i] ?? "";
                    db.AutomatonDeployments.Add(new AutomatonDeployment
                    {
                        AutomatonId = id, Position = i, Alias = name,
                        DeploymentEntityId = ResolveEntityIdAnyType(name),
                    });
                }
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.AutomatonStoryHooks.Add(new AutomatonStoryHook { AutomatonId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportWeaponsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<WeaponryData, Weapon>(
            LoadFromJsonDir<WeaponryData>("weaponry"), "weapon",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Weapon { Id = id },
            (sub, x) => {
                sub.Manufacturer = x.Manufacturer ?? "";
                sub.Category     = x.Category ?? "";
                sub.Tier         = x.TierAvailability ?? "";
                sub.Legality     = x.Legality ?? "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.Specifications = x.Specifications ?? "";
                sub.TacticalUse  = x.TacticalUse ?? "";
                sub.CulturalContext = x.CulturalContext ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.WeaponAliases.RemoveRange(db.WeaponAliases.Where(r => r.WeaponId == id));
                db.WeaponBaseTechnologies.RemoveRange(db.WeaponBaseTechnologies.Where(r => r.WeaponId == id));
                db.WeaponKnownUsers.RemoveRange(db.WeaponKnownUsers.Where(r => r.WeaponId == id));
                db.WeaponAmmunitionTypes.RemoveRange(db.WeaponAmmunitionTypes.Where(r => r.WeaponId == id));
                db.WeaponStoryHooks.RemoveRange(db.WeaponStoryHooks.Where(r => r.WeaponId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.WeaponAliases.Add(new WeaponAlias { WeaponId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.BaseTechnologies.Count; i++) {
                    var n = x.BaseTechnologies[i] ?? "";
                    db.WeaponBaseTechnologies.Add(new WeaponBaseTechnology { WeaponId = id, Position = i, Alias = n, TechnologyId = ResolveEntityIdByName("technology", n) });
                }
                for (int i = 0; i < x.KnownUsers.Count; i++) {
                    var n = x.KnownUsers[i] ?? "";
                    db.WeaponKnownUsers.Add(new WeaponKnownUser { WeaponId = id, Position = i, Alias = n, CharacterId = ResolveEntityIdByName("character", n) });
                }
                for (int i = 0; i < x.AmmunitionType.Count; i++) {
                    var n = x.AmmunitionType[i] ?? "";
                    db.WeaponAmmunitionTypes.Add(new WeaponAmmunitionType { WeaponId = id, Position = i, Alias = n, AmmunitionId = ResolveEntityIdByName("ammunition", n) });
                }
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.WeaponStoryHooks.Add(new WeaponStoryHook { WeaponId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportEquipmentAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<EquipmentData, Equipment>(
            LoadFromJsonDir<EquipmentData>("equipment"), "equipment",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Equipment { Id = id },
            (sub, x) => {
                sub.Manufacturer = x.Manufacturer ?? "";
                sub.Category     = x.Category ?? "";
                sub.Tier         = x.TierAvailability ?? "";
                sub.Legality     = x.Legality ?? "";
                sub.Name     = x.Name ?? "";
                sub.BrandName    = x.BrandName ?? "";
                sub.ProductName  = x.ProductName ?? "";
                sub.Description  = x.Description ?? "";
                sub.TacticalUse  = x.TacticalUse ?? "";
                sub.CulturalContext = x.CulturalContext ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.EquipmentAliases.RemoveRange(db.EquipmentAliases.Where(r => r.EquipmentId == id));
                db.EquipmentBaseTechnologies.RemoveRange(db.EquipmentBaseTechnologies.Where(r => r.EquipmentId == id));
                db.EquipmentKnownUsers.RemoveRange(db.EquipmentKnownUsers.Where(r => r.EquipmentId == id));
                db.EquipmentSpecifications.RemoveRange(db.EquipmentSpecifications.Where(r => r.EquipmentId == id));
                db.EquipmentStoryHooks.RemoveRange(db.EquipmentStoryHooks.Where(r => r.EquipmentId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.EquipmentAliases.Add(new EquipmentAlias { EquipmentId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.BaseTechnologies.Count; i++) {
                    var n = x.BaseTechnologies[i] ?? "";
                    db.EquipmentBaseTechnologies.Add(new EquipmentBaseTechnology { EquipmentId = id, Position = i, Alias = n, TechnologyId = ResolveEntityIdByName("technology", n) });
                }
                for (int i = 0; i < x.KnownUsers.Count; i++) {
                    var n = x.KnownUsers[i] ?? "";
                    db.EquipmentKnownUsers.Add(new EquipmentKnownUser { EquipmentId = id, Position = i, Alias = n, CharacterId = ResolveEntityIdByName("character", n) });
                }
                foreach (var kv in x.Specifications)
                    db.EquipmentSpecifications.Add(new EquipmentSpecification { EquipmentId = id, KeyName = kv.Key, Value = kv.Value ?? "" });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.EquipmentStoryHooks.Add(new EquipmentStoryHook { EquipmentId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportCyberwareAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<CyberwareData, Cyberware>(
            LoadFromJsonDir<CyberwareData>("cyberware"), "cyberware",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Cyberware { Id = id },
            (sub, x) => {
                sub.Manufacturer = x.Manufacturer ?? "";
                sub.Category     = x.Category ?? "";
                sub.BodyLocation = x.BodyLocation ?? "";
                sub.Tier         = x.TierAvailability ?? "";
                sub.Legality     = x.Legality ?? "";
                sub.Name     = x.Name ?? "";
                sub.BrandName    = x.BrandName ?? "";
                sub.ProductName  = x.ProductName ?? "";
                sub.Description  = x.Description ?? "";
                sub.InstallationRequirements = x.InstallationRequirements ?? "";
                sub.RejectionRisk = x.RejectionRisk ?? "";
                sub.Maintenance  = x.Maintenance ?? "";
                sub.Specifications = x.Specifications ?? "";
                sub.CulturalContext = x.CulturalContext ?? "";
                sub.StreetPrice  = x.StreetPrice ?? "";
                sub.LicensedPrice = x.LicensedPrice ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.CyberwareItemAliases.RemoveRange(db.CyberwareItemAliases.Where(r => r.CyberwareId == id));
                db.CyberwareItemSideEffects.RemoveRange(db.CyberwareItemSideEffects.Where(r => r.CyberwareId == id));
                db.CyberwareItemKnownUsers.RemoveRange(db.CyberwareItemKnownUsers.Where(r => r.CyberwareId == id));
                db.CyberwareItemStoryHooks.RemoveRange(db.CyberwareItemStoryHooks.Where(r => r.CyberwareId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.CyberwareItemAliases.Add(new CyberwareItemAlias { CyberwareId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.SideEffects.Count; i++)
                    db.CyberwareItemSideEffects.Add(new CyberwareItemSideEffect { CyberwareId = id, Position = i, Effect = x.SideEffects[i] ?? "" });
                for (int i = 0; i < x.KnownUsers.Count; i++) {
                    var n = x.KnownUsers[i] ?? "";
                    db.CyberwareItemKnownUsers.Add(new CyberwareItemKnownUser { CyberwareId = id, Position = i, Alias = n, CharacterId = ResolveEntityIdByName("character", n) });
                }
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.CyberwareItemStoryHooks.Add(new CyberwareItemStoryHook { CyberwareId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportApparelAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<ApparelData, Apparel>(
            LoadFromJsonDir<ApparelData>("apparel"), "apparel",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Apparel { Id = id },
            (sub, x) => {
                sub.Manufacturer = "";
                sub.Category     = "";
                sub.Tier         = "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                // ApparelData doesn't have an Aliases list — only StoryHooks.
                // Skip the Aliases bridge population; the column ApparelAlias bridge
                // remains available for future "common name" data.
                db.ApparelStoryHooks.RemoveRange(db.ApparelStoryHooks.Where(r => r.ApparelId == id));
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.ApparelStoryHooks.Add(new ApparelStoryHook { ApparelId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportAmmunitionAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<AmmunitionData, Ammunition>(
            LoadFromJsonDir<AmmunitionData>("ammunition"), "ammunition",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Ammunition { Id = id },
            (sub, x) => {
                sub.Manufacturer = x.Manufacturer ?? "";
                sub.Caliber      = x.Caliber ?? "";
                sub.Category     = x.Category ?? "";
                sub.Tier         = x.TierAvailability ?? "";
                sub.Legality     = x.Legality ?? "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.Specifications = x.Specifications ?? "";
                sub.CulturalContext = x.CulturalContext ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.AmmunitionAliases.RemoveRange(db.AmmunitionAliases.Where(r => r.AmmunitionId == id));
                db.AmmunitionCompatibleWeapons.RemoveRange(db.AmmunitionCompatibleWeapons.Where(r => r.AmmunitionId == id));
                db.AmmunitionVariants.RemoveRange(db.AmmunitionVariants.Where(r => r.AmmunitionId == id));
                db.AmmunitionStoryHooks.RemoveRange(db.AmmunitionStoryHooks.Where(r => r.AmmunitionId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.AmmunitionAliases.Add(new AmmunitionAlias { AmmunitionId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.CompatibleWeapons.Count; i++) {
                    var n = x.CompatibleWeapons[i] ?? "";
                    db.AmmunitionCompatibleWeapons.Add(new AmmunitionCompatibleWeapon { AmmunitionId = id, Position = i, Alias = n, WeaponId = ResolveEntityIdByName("weapon", n) });
                }
                for (int i = 0; i < x.Variants.Count; i++)
                    db.AmmunitionVariants.Add(new AmmunitionVariant { AmmunitionId = id, Position = i, VariantName = x.Variants[i] ?? "" });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.AmmunitionStoryHooks.Add(new AmmunitionStoryHook { AmmunitionId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportPharmaceuticalsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<PharmaceuticalData, Pharmaceutical>(
            LoadFromJsonDir<PharmaceuticalData>("pharmaceuticals"), "pharmaceutical",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Pharmaceutical { Id = id },
            (sub, x) => {
                sub.Manufacturer = x.Manufacturer ?? "";
                sub.Category     = x.Category ?? "";
                sub.Subcategory  = x.Subcategory ?? "";
                sub.Legality     = x.Legality ?? "";
                sub.Tier         = x.TierAvailability ?? "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.MethodOfUse  = x.MethodOfUse ?? "";
                sub.Duration     = x.Duration ?? "";
                sub.AddictionRisk = x.AddictionRisk ?? "";
                sub.StreetPrice  = x.StreetPrice ?? "";
                sub.CulturalContext = x.CulturalContext ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.PharmaceuticalAliases.RemoveRange(db.PharmaceuticalAliases.Where(r => r.PharmaceuticalId == id));
                db.PharmaceuticalEffects.RemoveRange(db.PharmaceuticalEffects.Where(r => r.PharmaceuticalId == id));
                db.PharmaceuticalSideEffects.RemoveRange(db.PharmaceuticalSideEffects.Where(r => r.PharmaceuticalId == id));
                db.PharmaceuticalStoryHooks.RemoveRange(db.PharmaceuticalStoryHooks.Where(r => r.PharmaceuticalId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.PharmaceuticalAliases.Add(new PharmAlias { PharmaceuticalId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.Effects.Count; i++)
                    db.PharmaceuticalEffects.Add(new PharmEffect { PharmaceuticalId = id, Position = i, Effect = x.Effects[i] ?? "" });
                for (int i = 0; i < x.SideEffects.Count; i++)
                    db.PharmaceuticalSideEffects.Add(new PharmSideEffect { PharmaceuticalId = id, Position = i, Effect = x.SideEffects[i] ?? "" });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.PharmaceuticalStoryHooks.Add(new PharmStoryHook { PharmaceuticalId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportGenemodsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<GenemodData, Genemod>(
            LoadFromJsonDir<GenemodData>("genemods"), "genemod",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Genemod { Id = id },
            (sub, x) => {
                sub.Manufacturer = "";
                sub.Category     = "";
                sub.Tier         = "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.GenemodAliases.RemoveRange(db.GenemodAliases.Where(r => r.GenemodId == id));
                db.GenemodStoryHooks.RemoveRange(db.GenemodStoryHooks.Where(r => r.GenemodId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.GenemodAliases.Add(new GenemodAlias { GenemodId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.GenemodStoryHooks.Add(new GenemodStoryHook { GenemodId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportMaterialsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<MaterialData, Material>(
            LoadFromJsonDir<MaterialData>("materials"), "material",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Material { Id = id },
            (sub, x) => {
                sub.Category    = "";
                sub.Tier        = "";
                sub.Name    = x.Name ?? "";
                sub.Description = x.Description ?? "";
                sub.Rating      = x.Rating;
                sub.VoteCount   = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.MaterialAliases.RemoveRange(db.MaterialAliases.Where(r => r.MaterialId == id));
                db.MaterialStoryHooks.RemoveRange(db.MaterialStoryHooks.Where(r => r.MaterialId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.MaterialAliases.Add(new MaterialAlias { MaterialId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.MaterialStoryHooks.Add(new MaterialStoryHook { MaterialId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportTransportationAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<TransportationData, Transportation>(
            LoadFromJsonDir<TransportationData>("transportation"), "transportation",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new Transportation { Id = id },
            (sub, x) => {
                sub.Manufacturer = "";
                sub.Category     = "";
                sub.Tier         = "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.TransportationAliases.RemoveRange(db.TransportationAliases.Where(r => r.TransportationId == id));
                db.TransportationStoryHooks.RemoveRange(db.TransportationStoryHooks.Where(r => r.TransportationId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.TransportationAliases.Add(new TransportationAlias { TransportationId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.TransportationStoryHooks.Add(new TransportationStoryHook { TransportationId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportConsumerGoodsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<ConsumerGoodData, ConsumerGood>(
            LoadFromJsonDir<ConsumerGoodData>("consumer_goods"), "consumer_good",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new ConsumerGood { Id = id },
            (sub, x) => {
                sub.Manufacturer = "";
                sub.Category     = "";
                sub.Tier         = "";
                sub.Name     = x.Name ?? "";
                sub.Description  = x.Description ?? "";
                sub.Rating       = x.Rating;
                sub.VoteCount    = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                // ConsumerGoodData doesn't have an Aliases list — only StoryHooks.
                db.ConsumerGoodStoryHooks.RemoveRange(db.ConsumerGoodStoryHooks.Where(r => r.ConsumerGoodId == id));
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.ConsumerGoodStoryHooks.Add(new ConsumerGoodStoryHook { ConsumerGoodId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportArchetypesAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<ArchetypeData, ArchetypeRow>(
            LoadFromJsonDir<ArchetypeData>("archetypes"), "archetype",
            x => ParseGuid(x.Id), x => x.Name, x => x.Description, x => x.Tags,
            (id, x) => new ArchetypeRow { Id = id },
            (sub, x) => {
                sub.Name = x.Name ?? "";
                sub.Family = x.Category ?? "";
                sub.Category = x.Category ?? "";
                sub.Description = x.Description ?? "";
                sub.BehavioralSignature = x.BehavioralSignature ?? "";
                sub.UnderStress = x.UnderStress ?? "";
                sub.AtRest = x.AtRest ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.ArchetypeWillAlways.RemoveRange(db.ArchetypeWillAlways.Where(r => r.ArchetypeId == id));
                db.ArchetypeWillNever.RemoveRange(db.ArchetypeWillNever.Where(r => r.ArchetypeId == id));
                db.ArchetypeUnless.RemoveRange(db.ArchetypeUnless.Where(r => r.ArchetypeId == id));
                db.ArchetypeSimilars.RemoveRange(db.ArchetypeSimilars.Where(r => r.ArchetypeId == id));
                db.ArchetypeOpposites.RemoveRange(db.ArchetypeOpposites.Where(r => r.ArchetypeId == id));
                for (int i = 0; i < x.WillAlways.Count; i++)
                    db.ArchetypeWillAlways.Add(new ArchetypeWillAlways { ArchetypeId = id, Position = i, Rule = x.WillAlways[i] ?? "" });
                for (int i = 0; i < x.WillNever.Count; i++)
                    db.ArchetypeWillNever.Add(new ArchetypeWillNever { ArchetypeId = id, Position = i, Rule = x.WillNever[i] ?? "" });
                for (int i = 0; i < x.Unless.Count; i++)
                    db.ArchetypeUnless.Add(new ArchetypeUnless { ArchetypeId = id, Position = i, Condition = x.Unless[i] ?? "" });
                for (int i = 0; i < x.SimilarTo.Count; i++) {
                    var s = x.SimilarTo[i];
                    db.ArchetypeSimilars.Add(new ArchetypeSimilar {
                        ArchetypeId = id, Position = i, Alias = s.Archetype ?? "",
                        SimilarArchetypeId = ResolveEntityIdByName("archetype", s.Archetype ?? ""),
                        Threshold = s.Threshold, Context = s.Context ?? "",
                    });
                }
                for (int i = 0; i < x.OppositeOf.Count; i++) {
                    var n = x.OppositeOf[i] ?? "";
                    db.ArchetypeOpposites.Add(new ArchetypeOpposite {
                        ArchetypeId = id, Position = i, Alias = n,
                        OppositeArchetypeId = ResolveEntityIdByName("archetype", n),
                    });
                }
            });

    public Task<JsonImportResult> ImportQuotesAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<QuoteData, QuoteEntity>(
            LoadFromJsonDir<QuoteData>("quotes"), "quote",
            x => ParseGuid(x.Id),
            x => string.IsNullOrEmpty(x.Attribution) ? "(unattributed quote)" : x.Attribution,
            x => x.Quote, x => x.Tags,
            (id, x) => new QuoteEntity { Id = id },
            (sub, x) => {
                sub.Name = string.IsNullOrEmpty(x.Attribution) ? "(unattributed quote)" : x.Attribution;
                sub.Attribution = x.Attribution ?? "";
                sub.Theme = x.Category ?? "";
                sub.QuoteText = x.Quote ?? "";
                sub.Source = x.Source ?? "";
                sub.Context = x.Context ?? "";
                sub.Category = x.Category ?? "";
                sub.InWorld = x.InWorld;
            }, ct);

    public Task<JsonImportResult> ImportNewsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<NewsData, NewsEntity>(
            LoadFromJsonDir<NewsData>("news"), "news",
            x => ParseGuid(x.Id),
            x => string.IsNullOrEmpty(x.Headline) ? "(untitled news)" : x.Headline,
            x => x.Body, x => x.Tags,
            (id, x) => new NewsEntity { Id = id },
            (sub, x) => {
                sub.Name = string.IsNullOrEmpty(x.Headline) ? "(untitled news)" : x.Headline;
                sub.Outlet = x.Source ?? "";
                sub.PublishedDate = DateTime.TryParse(x.Date, out var d) ? d : null;
                sub.DateText = x.Date ?? "";
                sub.Category = x.Category ?? "";
                sub.Source = x.Source ?? "";
                sub.Reporter = x.Reporter ?? "";
                sub.Body = x.Body ?? "";
                sub.Aftermath = x.Aftermath ?? "";
                sub.Casualties = x.Casualties ?? "";
                sub.RunnerRelevance = x.RunnerRelevance ?? "";
                sub.Rating = x.Rating;
                sub.VoteCount = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.NewsEntitiesInvolved.RemoveRange(db.NewsEntitiesInvolved.Where(r => r.NewsId == id));
                db.NewsLocations.RemoveRange(db.NewsLocations.Where(r => r.NewsId == id));
                for (int i = 0; i < x.EntitiesInvolved.Count; i++) {
                    var n = x.EntitiesInvolved[i] ?? "";
                    db.NewsEntitiesInvolved.Add(new NewsEntityInvolved { NewsId = id, Position = i, Alias = n, InvolvedEntityId = ResolveEntityIdAnyType(n) });
                }
                for (int i = 0; i < x.Locations.Count; i++) {
                    var n = x.Locations[i] ?? "";
                    db.NewsLocations.Add(new NewsLocation { NewsId = id, Position = i, Alias = n, PlaceId = ResolveEntityIdByName("place", n) });
                }
            });

    public Task<JsonImportResult> ImportContractsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<ContractData, ContractEntity>(
            LoadFromJsonDir<ContractData>("contracts"), "contract",
            x => ParseGuid(x.Id),
            x => string.IsNullOrEmpty(x.Codename) ? "(unnamed contract)" : x.Codename,
            x => x.Description, x => x.Tags,
            (id, x) => new ContractEntity { Id = id },
            (sub, x) => {
                sub.Name = string.IsNullOrEmpty(x.Codename) ? "(unnamed contract)" : x.Codename;
                sub.Codename = x.Codename ?? "";
                sub.ContractStatus = string.IsNullOrEmpty(x.Status) ? "open" : x.Status;
                sub.Tier = x.ClientTier ?? "";
                sub.Client = x.Client ?? "";
                sub.ClientEntityId = ResolveEntityIdAnyType(x.Client ?? "");
                sub.ClientTier = x.ClientTier ?? "";
                sub.Category = x.Category ?? "";
                sub.Description = x.Description ?? "";
                sub.Objective = x.Objective ?? "";
                sub.Location = x.Location ?? "";
                sub.LocationPlaceId = ResolveEntityIdByName("place", x.Location ?? "");
                sub.Target = x.Target ?? "";
                sub.Opposition = x.Opposition ?? "";
                sub.Payout = x.Payout ?? "";
                sub.CrewSize = x.CrewSize ?? "";
                sub.Difficulty = x.Difficulty ?? "";
                sub.TimeLimit = x.TimeLimit ?? "";
                sub.Outcome = x.Outcome ?? "";
                var c = x.RequiredCapabilities ?? new();
                sub.CapabilityCombat = c.Combat;
                sub.CapabilityStealth = c.Stealth;
                sub.CapabilityHacking = c.Hacking;
                sub.CapabilitySocial = c.Social;
                sub.CapabilityMedical = c.Medical;
                sub.CapabilityTech = c.Tech;
                sub.CapabilityTransport = c.Transport;
                sub.CapabilityDemolitions = c.Demolitions;
                sub.CapabilitySurveillance = c.Surveillance;
                sub.CapabilityLinguistics = c.Linguistics;
                sub.Rating = x.Rating;
                sub.VoteCount = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.ContractBonuses.RemoveRange(db.ContractBonuses.Where(r => r.ContractId == id));
                db.ContractComplications.RemoveRange(db.ContractComplications.Where(r => r.ContractId == id));
                for (int i = 0; i < x.Bonuses.Count; i++) {
                    var bn = x.Bonuses[i];
                    db.ContractBonuses.Add(new ContractBonusRow { ContractId = id, Position = i, BonusType = bn.Type ?? "", Amount = bn.Amount ?? "", Condition = bn.Condition ?? "" });
                }
                for (int i = 0; i < x.Complications.Count; i++)
                    db.ContractComplications.Add(new ContractComplication { ContractId = id, Position = i, Description = x.Complications[i] ?? "" });
            });

    public Task<JsonImportResult> ImportDocumentsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<WorldbuildingDocument, DocumentEntity>(
            LoadFromJsonDir<WorldbuildingDocument>("documents"), "document",
            x => ParseGuid(x.Id),
            x => string.IsNullOrEmpty(x.Title) ? "(untitled document)" : x.Title,
            x => x.Body, x => x.Tags,
            (id, x) => new DocumentEntity { Id = id },
            (sub, x) => {
                sub.Name = string.IsNullOrEmpty(x.Title) ? "(untitled document)" : x.Title;
                sub.Title = x.Title ?? "";
                sub.FileName = x.FileName ?? "";
                sub.Category = x.Category ?? "";
                sub.Body = x.Body ?? "";
                sub.LineCount = x.LineCount;
                sub.Rating = x.Rating;
                sub.VoteCount = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.DocumentHeadings.RemoveRange(db.DocumentHeadings.Where(r => r.DocumentId == id));
                for (int i = 0; i < x.Headings.Count; i++)
                    db.DocumentHeadings.Add(new DocumentHeading { DocumentId = id, Position = i, HeadingText = x.Headings[i] ?? "" });
            });

    public Task<JsonImportResult> ImportVocabularyAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<VocabularyData, Vocabulary>(
            LoadFromJsonDir<VocabularyData>("vocabulary"), "vocabulary",
            x => ParseGuid(x.Id),
            x => string.IsNullOrEmpty(x.Term) ? "(untermed vocabulary)" : x.Term,
            x => x.Definition, x => x.Tags,
            (id, x) => new Vocabulary { Id = id },
            (sub, x) => {
                sub.Name = string.IsNullOrEmpty(x.Term) ? "(untermed vocabulary)" : x.Term;
                sub.Term = x.Term ?? "";
                sub.Domain = "";
                sub.Definition = x.Definition ?? "";
                sub.Origin = x.Origin ?? "";
                sub.Usage = x.Usage ?? "";
                sub.Tier = x.Tier ?? "";
                sub.Category = x.Category ?? "";
                sub.Example = x.Example ?? "";
            }, ct);

    public Task<JsonImportResult> ImportLabSpecimensAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<LabSpecimenData, LabSpecimen>(
            LoadFromJsonDir<LabSpecimenData>("lab_specimens"), "lab_specimen",
            x => ParseGuid(x.Id), x => x.Name,
            x => string.IsNullOrEmpty(x.PhysicalDescription) ? x.BehavioralProfile : x.PhysicalDescription,
            x => x.Tags,
            (id, x) => new LabSpecimen { Id = id },
            (sub, x) => {
                sub.Name = x.Name ?? "";
                sub.Classification = x.Classification ?? "";
                sub.Origin = x.OriginLab ?? "";
                sub.OriginLab = x.OriginLab ?? "";
                sub.OriginMethod = x.OriginMethod ?? "";
                sub.Substrate = x.Substrate ?? "";
                sub.PhysicalDescription = x.PhysicalDescription ?? "";
                sub.BehavioralProfile = x.BehavioralProfile ?? "";
                sub.ThreatLevel = x.ThreatLevel ?? "";
                sub.ContainmentStatus = x.ContainmentStatus ?? "";
                sub.ContaminationRisk = x.ContaminationRisk ?? "";
                sub.PacificationProtocol = x.PacificationProtocol ?? "";
                sub.PitiableQualities = x.PitiableQualities ?? "";
                sub.Rating = x.Rating;
                sub.VoteCount = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.LabSpecimenAliases.RemoveRange(db.LabSpecimenAliases.Where(r => r.LabSpecimenId == id));
                db.LabSpecimenKnownLocations.RemoveRange(db.LabSpecimenKnownLocations.Where(r => r.LabSpecimenId == id));
                db.LabSpecimenStoryHooks.RemoveRange(db.LabSpecimenStoryHooks.Where(r => r.LabSpecimenId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.LabSpecimenAliases.Add(new LabSpecimenAlias { LabSpecimenId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.KnownLocations.Count; i++) {
                    var n = x.KnownLocations[i] ?? "";
                    db.LabSpecimenKnownLocations.Add(new LabSpecimenKnownLocation { LabSpecimenId = id, Position = i, Alias = n, PlaceId = ResolveEntityIdByName("place", n) });
                }
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.LabSpecimenStoryHooks.Add(new LabSpecimenStoryHook { LabSpecimenId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    public Task<JsonImportResult> ImportPsionicsAsync(CancellationToken ct = default)
        => ImportSubtypeAsync<PsionicData, Psionic>(
            LoadFromJsonDir<PsionicData>("psionics"), "psionic",
            x => ParseGuid(x.Id), x => x.Name,
            x => string.IsNullOrEmpty(x.Mechanism) ? x.Abilities : x.Mechanism,
            x => x.Tags,
            (id, x) => new Psionic { Id = id },
            (sub, x) => {
                sub.Name = x.Name ?? "";
                sub.Discipline = x.Classification ?? "";
                sub.Tier = "";
                sub.Classification = x.Classification ?? "";
                sub.EnhancementType = x.EnhancementType ?? "";
                sub.Mechanism = x.Mechanism ?? "";
                sub.Abilities = x.Abilities ?? "";
                sub.SideEffects = x.SideEffects ?? "";
                sub.AcquisitionMethod = x.AcquisitionMethod ?? "";
                sub.DetectionRisk = x.DetectionRisk ?? "";
                sub.CorporateInterest = x.CorporateInterest ?? "";
                sub.Rating = x.Rating;
                sub.VoteCount = x.VoteCount;
                sub.MidjourneyPrompt = x.MidjourneyPrompt ?? "";
                sub.Dalle3Prompt = x.Dalle3Prompt ?? "";
            },
            ct,
            postUpsert: (id, x) => {
                db.PsionicAliases.RemoveRange(db.PsionicAliases.Where(r => r.PsionicId == id));
                db.PsionicKnownPractitioners.RemoveRange(db.PsionicKnownPractitioners.Where(r => r.PsionicId == id));
                db.PsionicStoryHooks.RemoveRange(db.PsionicStoryHooks.Where(r => r.PsionicId == id));
                for (int i = 0; i < x.Aliases.Count; i++)
                    db.PsionicAliases.Add(new PsionicAlias { PsionicId = id, Position = i, Value = x.Aliases[i] ?? "" });
                for (int i = 0; i < x.KnownPractitioners.Count; i++) {
                    var n = x.KnownPractitioners[i] ?? "";
                    db.PsionicKnownPractitioners.Add(new PsionicKnownPractitioner { PsionicId = id, Position = i, Alias = n, CharacterId = ResolveEntityIdByName("character", n) });
                }
                for (int i = 0; i < x.StoryHooks.Count; i++)
                    db.PsionicStoryHooks.Add(new PsionicStoryHook { PsionicId = id, Position = i, Hook = x.StoryHooks[i] ?? "" });
            });

    /// <summary>
    /// Run every import in a single transaction-friendly sweep. Returns aggregate
    /// counts; the per-type breakdown lives in the logs.
    /// </summary>
    public async Task<Dictionary<string, JsonImportResult>> ImportAllAsync(CancellationToken ct = default)
    {
        var results = new Dictionary<string, JsonImportResult>(StringComparer.Ordinal)
        {
            ["character"]      = await ImportCharactersAsync(ct),
            ["place"]          = await ImportPlacesAsync(ct),
            ["faction"]        = await ImportFactionsAsync(ct),
            ["corponation"]    = await ImportCorponationsAsync(ct),
            ["subsidiary"]     = await ImportSubsidiariesAsync(ct),
            ["synthetic"]      = await ImportSyntheticsAsync(ct),
            ["automaton"]      = await ImportAutomataAsync(ct),
            ["weapon"]         = await ImportWeaponsAsync(ct),
            ["equipment"]      = await ImportEquipmentAsync(ct),
            ["cyberware"]      = await ImportCyberwareAsync(ct),
            ["apparel"]        = await ImportApparelAsync(ct),
            ["ammunition"]     = await ImportAmmunitionAsync(ct),
            ["pharmaceutical"] = await ImportPharmaceuticalsAsync(ct),
            ["genemod"]        = await ImportGenemodsAsync(ct),
            ["material"]       = await ImportMaterialsAsync(ct),
            ["transportation"] = await ImportTransportationAsync(ct),
            ["consumer_good"]  = await ImportConsumerGoodsAsync(ct),
            ["archetype"]      = await ImportArchetypesAsync(ct),
            ["quote"]          = await ImportQuotesAsync(ct),
            ["news"]           = await ImportNewsAsync(ct),
            ["contract"]       = await ImportContractsAsync(ct),
            ["document"]       = await ImportDocumentsAsync(ct),
            ["vocabulary"]     = await ImportVocabularyAsync(ct),
            ["lab_specimen"]   = await ImportLabSpecimensAsync(ct),
            ["psionic"]        = await ImportPsionicsAsync(ct),
            ["__archives"]     = await ImportArchivesAsync(ct),
        };
        return results;
    }

    // ── Books / chapters / beats ────────────────────────────────────────────

    /// <summary>
    /// Import books, chapters and beats into the relational store. Bookmark for
    /// future code: in-world chapter dates land in <see cref="Entities.Chapter.InWorldDate"/>
    /// and become the asOf cursor source for the dossier.
    /// </summary>
    /// <summary>
    /// Walk every <c>engine/data/archives/&lt;type&gt;/*.json</c> directory and pull the
    /// archived records into the database with <c>IsActive = false</c>. Pulled in the
    /// SAME entity-id slot as their live counterpart when ids match, so an archive
    /// and a current record share the Entity row and the active record wins by
    /// running last with isActive = true. Designed to be called AFTER the live
    /// imports so re-activations don't get overwritten.
    /// </summary>
    public async Task<JsonImportResult> ImportArchivesAsync(CancellationToken ct = default)
    {
        var result = new JsonImportResult();
        var archivesRoot = Path.Combine(paths.EngineDataDir, "archives");
        if (!Directory.Exists(archivesRoot)) return result;

        var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["people"]          = "character",
            ["characters"]      = "character",
            ["places"]          = "place",
            ["districts"]       = "place",
            ["factions"]        = "faction",
            ["corponations"]    = "corponation",
            ["subsidiaries"]    = "subsidiary",
            ["synthetics"]      = "synthetic",
            ["automata"]        = "automaton",
            ["weaponry"]        = "weapon",
            ["equipment"]       = "equipment",
            ["cyberware"]       = "cyberware",
            ["apparel"]         = "apparel",
            ["ammunition"]      = "ammunition",
            ["pharmaceuticals"] = "pharmaceutical",
            ["genemods"]        = "genemod",
            ["materials"]       = "material",
            ["transportation"]  = "transportation",
            ["consumer_goods"]  = "consumer_good",
            ["archetypes"]      = "archetype",
            ["quotes"]          = "quote",
            ["news"]            = "news",
            ["contracts"]       = "contract",
            ["documents"]       = "document",
            ["vocabulary"]      = "vocabulary",
            ["lab_specimens"]   = "lab_specimen",
            ["psionics"]        = "psionic",
            ["entertainment"]   = "entertainment",
            ["chapters"]        = "chapter",
            ["wasteland_entities"] = "flyover_entity",
        };

        // Track slugs we've added in this batch so back-to-back archive files
        // with the same (type, slug) get disambiguated before SaveChanges runs.
        // db.Entities.AnyAsync() doesn't see un-flushed adds in the change tracker.
        var pendingSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendingIds = new HashSet<Guid>();

        foreach (var dir in Directory.GetDirectories(archivesRoot))
        {
            if (ct.IsCancellationRequested) break;
            var leaf = Path.GetFileName(dir);
            if (!typeMap.TryGetValue(leaf, out var entityType)) continue;

            var files = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
            result.SourceCount += files.Length;
            foreach (var file in files)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("id", out var idEl)) continue;
                    var idStr = idEl.GetString();
                    if (string.IsNullOrEmpty(idStr)) continue;

                    var name =
                        (root.TryGetProperty("name", out var n) ? n.GetString() : null)
                        ?? (root.TryGetProperty("title", out var t) ? t.GetString() : null)
                        ?? (root.TryGetProperty("term", out var tm) ? tm.GetString() : null)
                        ?? (root.TryGetProperty("codename", out var cn) ? cn.GetString() : null)
                        ?? Path.GetFileNameWithoutExtension(file);
                    var description =
                        (root.TryGetProperty("description", out var d) ? d.GetString() : null)
                        ?? (root.TryGetProperty("body", out var bd) ? bd.GetString() : null);
                    var tags = new List<string>();
                    if (root.TryGetProperty("tags", out var tg) && tg.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var x in tg.EnumerateArray())
                            if (x.ValueKind == System.Text.Json.JsonValueKind.String)
                                tags.Add(x.GetString() ?? "");

                    var id = ParseGuid(idStr);
                    // Only import if no live row exists, or if id is unique to archive.
                    var existing = await db.Entities.FirstOrDefaultAsync(e => e.Id == id, ct);
                    if (existing != null && existing.IsActive)
                    {
                        // Live record already won; don't downgrade it.
                        continue;
                    }

                    if (!pendingIds.Add(id) && existing == null) continue;

                    var slug = WorldGraphService.Slugify(name ?? "");

                    // Slug collision check — a live record with the same (type, slug)
                    // but a different id wins. Archived row gets a disambiguated slug
                    // so the unique index doesn't blow up. Also dedupes within this
                    // import batch (un-flushed adds aren't visible to AnyAsync).
                    if (existing == null)
                    {
                        var compositeKey = entityType + "|" + slug;
                        var collidesInBatch = pendingSlugs.Contains(compositeKey);
                        var collidesWithDb = !collidesInBatch && await db.Entities.AnyAsync(
                            e => e.EntityType == entityType && e.Slug == slug && e.Id != id, ct);
                        if (collidesInBatch || collidesWithDb)
                        {
                            // Full-id suffix — guid7 timestamp prefixes can collide on a short hash
                            // when many entities were generated in the same millisecond bucket.
                            slug = $"{slug}-archived-{id:N}";
                            compositeKey = entityType + "|" + slug;
                            // If the FULL-id slug ALSO exists (re-import of same id-as-archive),
                            // skip — the row is already there.
                            if (pendingSlugs.Contains(compositeKey)
                                || await db.Entities.AnyAsync(e => e.EntityType == entityType && e.Slug == slug, ct))
                                continue;
                        }
                        pendingSlugs.Add(compositeKey);
                    }

                    // Persist the raw JSON as the Record so reads still round-trip.
                    var rawJson = root.GetRawText();
                    if (existing == null)
                    {
                        db.Entities.Add(new Entity
                        {
                            Id          = id,
                            EntityType  = entityType,
                            Name        = name ?? "",
                            Slug        = slug,
                            Status      = "archived",
                            Description = description,
                            TagsJson    = tags.Count > 0 ? JsonSerializer.Serialize(tags, JsonOpts) : null,
                            CreatedAt   = DateTime.UtcNow,
                            ModifiedAt  = DateTime.UtcNow,
                            IsActive    = false,
                            ArchivedAt  = File.GetLastWriteTimeUtc(file),
                        });
                    }
                    else
                    {
                        existing.Status     = "archived";
                        existing.IsActive   = false;
                        existing.ArchivedAt = existing.ArchivedAt ?? File.GetLastWriteTimeUtc(file);
                        existing.ModifiedAt = DateTime.UtcNow;
                    }

                    var rec = await db.Records.FirstOrDefaultAsync(r => r.EntityId == id, ct);
                    if (rec == null) db.Records.Add(new Record { EntityId = id, Json = rawJson, UpdatedAt = File.GetLastWriteTimeUtc(file) });
                    else if (existing == null || !existing.IsActive)
                    {
                        // Only overwrite the JSON if there's no live record to preserve.
                        rec.Json = rawJson;
                        rec.UpdatedAt = File.GetLastWriteTimeUtc(file);
                    }

                    result.Imported++;
                }
                catch (Exception ex)
                {
                    log.LogDebug(ex, "Archive import skipped: {File}", file);
                    result.Errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>
    /// Import the four legacy bible JSON files into the unified Settings table,
    /// keyed by their canonical names (<c>tone_bible</c>, <c>story_bible</c>,
    /// <c>literary_rules</c>, <c>character_profile</c>). Missing source files are
    /// skipped, not errored. Idempotent: re-importing replaces the row.
    /// </summary>
    public async Task<JsonImportResult> ImportBiblesAsync(CancellationToken ct = default)
    {
        var result = new JsonImportResult();
        var map = new (string file, string key)[]
        {
            ("neo-noir_tone_bible.json", "tone_bible"),
            ("story_bible.json",         "story_bible"),
            ("literary_rules.json",      "literary_rules"),
            ("character_profile.json",   "character_profile"),
        };
        foreach (var (file, key) in map)
        {
            var path = Path.Combine(paths.EngineDataDir, file);
            if (!File.Exists(path)) continue;
            result.SourceCount++;
            try
            {
                var raw = await File.ReadAllTextAsync(path, ct);
                var row = await db.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);
                if (row == null)
                    db.Settings.Add(new Entities.Setting { Key = key, Json = raw, UpdatedAt = DateTime.UtcNow });
                else
                {
                    row.Json = raw;
                    row.UpdatedAt = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(ct);
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"{file}: {DeepestMessage(ex)}"); }
        }
        return result;
    }

    /// <summary>
    /// One-shot move of the legacy <c>continuity.db</c> SQLite store into the unified
    /// StreetSamurai database. Idempotent: re-running upserts on claim_uid PK and
    /// composite PKs for the contradiction / confirmation edges. After this completes,
    /// the SQLite file is left in place as a backup; the next ContinuityService
    /// release should write to SQL Server directly and the file can be archived.
    /// </summary>
    public async Task<ContinuityImportResult> ImportContinuityFromSqliteAsync(CancellationToken ct = default)
    {
        var result = new ContinuityImportResult();
        var sqlitePath = Path.Combine(paths.EngineDataDir, "continuity.db");
        if (!File.Exists(sqlitePath))
        {
            result.Skipped = true;
            return result;
        }

        var connStr = $"Data Source={sqlitePath};Mode=ReadOnly";
        using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
        await conn.OpenAsync(ct);

        // ── claims ────────────────────────────────────────────────────────────
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM claims";
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var uid = r["claim_uid"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(uid)) continue;

                var existing = await db.ContinuityClaims.FirstOrDefaultAsync(c => c.ClaimUid == uid, ct);
                var extractors = ParseStringList(r["extracted_by"] as string);
                if (existing == null)
                {
                    db.ContinuityClaims.Add(new ContinuityClaim
                    {
                        ClaimUid            = uid,
                        EntityId            = r["entity_id"]?.ToString() ?? "",
                        EntityName          = r["entity_name"]?.ToString() ?? "",
                        EntityKind          = r["entity_kind"]?.ToString() ?? "",
                        Predicate           = r["predicate"]?.ToString() ?? "",
                        Object              = r["object"]?.ToString() ?? "",
                        SourceType          = r["source_type"]?.ToString() ?? "",
                        SourcePath          = r["source_path"] as string,
                        SourceChapterId     = r["source_chapter_id"] as string,
                        SourceChapterNumber = r["source_chapter_number"] is long n ? (int?)n : null,
                        SourceChapterTitle  = r["source_chapter_title"] as string,
                        Snippet             = r["snippet"] as string,
                        Voice               = r["voice"] as string,
                        Confidence          = r["confidence"] as string,
                        ExtractedBy         = extractors,
                        Status              = r["status"]?.ToString() ?? "NEW",
                        FirstAssertedAt     = r["first_asserted_at"]?.ToString() ?? "",
                        LastConfirmedAt     = r["last_confirmed_at"]?.ToString() ?? "",
                        ResolvedAt          = r["resolved_at"] as string,
                        AppliedAt           = r["applied_at"] as string,
                        AppliedToField      = r["applied_to_field"] as string,
                        SupersededBy        = r["superseded_by"] as string,
                        ResolutionNote      = r["resolution_note"] as string,
                    });
                    result.Claims++;
                }
                else
                {
                    existing.Status          = r["status"]?.ToString() ?? existing.Status;
                    existing.LastConfirmedAt = r["last_confirmed_at"]?.ToString() ?? existing.LastConfirmedAt;
                    existing.ExtractedBy    = extractors.Count > 0 ? extractors : existing.ExtractedBy;
                }
            }
        }

        // ── contradictions ────────────────────────────────────────────────────
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT a_uid, b_uid, detected_at FROM claim_contradictions";
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var a = r.GetString(0); var b = r.GetString(1); var w = r.GetString(2);
                var existing = await db.ClaimContradictions.FirstOrDefaultAsync(x => x.AUid == a && x.BUid == b, ct);
                if (existing == null)
                {
                    db.ClaimContradictions.Add(new ClaimContradictionRow { AUid = a, BUid = b, DetectedAt = w });
                    result.Contradictions++;
                }
            }
        }

        // ── confirmations ─────────────────────────────────────────────────────
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT claim_uid, COALESCE(source_chapter_id, ''), COALESCE(source_path, ''), confirmed_at FROM claim_confirmations";
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var uid = r.GetString(0); var sc = r.GetString(1); var sp = r.GetString(2); var w = r.GetString(3);
                var existing = await db.ClaimConfirmations.FirstOrDefaultAsync(x =>
                    x.ClaimUid == uid && x.SourceChapterId == sc && x.SourcePath == sp, ct);
                if (existing == null)
                {
                    db.ClaimConfirmations.Add(new ClaimConfirmationRow
                    {
                        ClaimUid = uid, SourceChapterId = sc, SourcePath = sp, ConfirmedAt = w
                    });
                    result.Confirmations++;
                }
            }
        }

        // ── extraction_runs ──────────────────────────────────────────────────
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT started_at, completed_at, scope_type, scope_id, new_claims, confirmed_claims, contradicted_claims, error FROM extraction_runs";
            using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                db.ExtractionRuns.Add(new ExtractionRunRow
                {
                    StartedAt           = r.IsDBNull(0) ? "" : r.GetString(0),
                    CompletedAt         = r.IsDBNull(1) ? null : r.GetString(1),
                    ScopeType           = r.IsDBNull(2) ? "" : r.GetString(2),
                    ScopeId             = r.IsDBNull(3) ? null : r.GetString(3),
                    NewClaims           = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                    ConfirmedClaims     = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)),
                    ContradictedClaims  = r.IsDBNull(6) ? 0 : Convert.ToInt32(r.GetValue(6)),
                    Error               = r.IsDBNull(7) ? null : r.GetString(7),
                });
                result.Runs++;
            }
        }

        await db.SaveChangesAsync(ct);
        return result;
    }

    private static List<string> ParseStringList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }

    public async Task<JsonImportResult> ImportBooksAndChaptersAsync(
        IBookRepository books, IChapterRepository chapterRepo, CancellationToken ct = default)
    {
        var result = new JsonImportResult();

        // Read from disk directly — the EF-backed repos return empty until the
        // import lands. Books live as flat files under engine_data/books/; chapters
        // live as folders under engine_data/chapters/{folder}/chapter.json.
        var allBooks = LoadBooksFromDisk();
        var allChapters = LoadChaptersFromDisk();

        // Skip placeholder files (no name / no title) — they create stub Entity
        // rows that pollute searches and fail FK-bound child writes downstream.
        allBooks    = allBooks   .Where(b => !string.IsNullOrWhiteSpace(b.Title)).ToList();
        allChapters = allChapters.Where(c => !string.IsNullOrWhiteSpace(c.Title)).ToList();
        result.SourceCount = allBooks.Count + allChapters.Count;

        // ── Phase 1: chapters first ──
        // Ordered before books because BookChapterOrder has FK on Chapters.Id;
        // if books ran first their ChapterId rows would FK-violate.
        // Each chapter creates BOTH the universal Entity row (so /entity/{id} +
        // global search find it) AND the typed Chapters subtype row.
        foreach (var c in allChapters)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var id = ParseGuid(c.Id);
                await UpsertEntityAsync(id, "chapter", c.Title,
                    description: c.Synopsis, tags: Array.Empty<string>(), ct: ct, sourceRecord: c);

                var existing = await db.Chapters
                    .Include(x => x.Beats)
                    .FirstOrDefaultAsync(x => x.Id == id, ct);
                if (existing == null)
                {
                    existing = new Entities.Chapter { Id = id };
                    db.Chapters.Add(existing);
                }
                existing.BookId         = string.IsNullOrEmpty(c.BookId) ? null : ParseGuid(c.BookId);
                existing.Number         = c.Number;
                existing.Title          = c.Title;
                existing.Synopsis       = c.Synopsis ?? "";
                existing.Status         = c.Status ?? "draft";
                existing.Html           = c.Html ?? "";
                existing.ModifiedAt     = DateTime.UtcNow;

                // Replace ChapterCharacters bridge.
                await db.ChapterCharacters.Where(r => r.ChapterId == id).ExecuteDeleteAsync(ct);
                for (int i = 0; i < c.Characters.Count; i++)
                {
                    var alias = c.Characters[i] ?? "";
                    db.ChapterCharacters.Add(new Entities.ChapterCharacter
                    {
                        ChapterId = id, Position = i, Alias = alias,
                        CharacterId = ResolveEntityIdByName("character", alias),
                    });
                }

                // Replace beats — simpler than diffing for a one-shot import.
                existing.Beats.Clear();
                foreach (var beat in c.Beats.OrderBy(b => b.Index))
                    existing.Beats.Add(new Entities.ChapterBeat
                    {
                        BeatGuid       = ParseGuid(beat.Id),
                        ChapterId      = id,
                        Index          = beat.Index,
                        Title          = beat.Title ?? "",
                        Synopsis       = beat.Synopsis ?? "",
                        Text           = beat.Text ?? "",
                        Act            = beat.Act,
                        StructureRole  = beat.StructureRole ?? "",
                        SceneType      = beat.SceneType ?? "scene",
                        FacetTag       = beat.FacetTag ?? "",
                    });
                await db.SaveChangesAsync(ct);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"chapter {c.Title}: {DeepestMessage(ex)}");
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged).ToList())
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
        }

        // ── Phase 2: books ──
        // Now that every chapter row exists, BookChapterOrder FKs resolve cleanly.
        // Books that reference vanished chapter ids skip those entries instead of
        // failing the whole insert.
        foreach (var b in allBooks)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var id = ParseGuid(b.Id);
                await UpsertEntityAsync(id, "book", b.Title,
                    description: b.Premise, tags: Array.Empty<string>(), ct: ct, sourceRecord: b);

                var existing = await db.Books.FirstOrDefaultAsync(x => x.Id == id, ct);
                bool isNew = existing == null;
                if (isNew)
                {
                    existing = new Entities.Book { Id = id };
                    db.Books.Add(existing);
                }
                existing!.Title            = b.Title;
                if (isNew)
                {
                    // Disambiguate slug only on first insert; preserve any
                    // disambig'd slug on re-imports.
                    var slug = WorldGraphService.Slugify(b.Title);
                    var collides = await db.Books.AnyAsync(x => x.Slug == slug && x.Id != id, ct);
                    if (collides) slug = $"{slug}-{id:N}";
                    existing.Slug = slug;
                }
                existing.Tagline           = b.Tagline ?? "";
                existing.Premise           = b.Premise ?? "";
                existing.ArcTarget         = b.ArcTarget ?? "";
                existing.ModifiedAt        = DateTime.UtcNow;

                // Replace BookProtagonists / BookChapterOrder bridges.
                await db.BookProtagonists.Where(r => r.BookId == id).ExecuteDeleteAsync(ct);
                await db.BookChapterOrder.Where(r => r.BookId == id).ExecuteDeleteAsync(ct);
                for (int i = 0; i < b.Protagonists.Count; i++)
                {
                    var alias = b.Protagonists[i] ?? "";
                    db.BookProtagonists.Add(new BookProtagonist
                    {
                        BookId = id, Position = i, Alias = alias,
                        CharacterId = ResolveEntityIdByName("character", alias),
                    });
                }
                // Skip ChapterIds entries that don't resolve to a real Chapter — books
                // sometimes outlive their chapters when generation crashes mid-stream.
                int pos = 0;
                for (int i = 0; i < b.ChapterIds.Count; i++)
                {
                    var raw = b.ChapterIds[i];
                    if (string.IsNullOrEmpty(raw)) continue;
                    var chId = ParseGuid(raw);
                    if (!await db.Chapters.AnyAsync(x => x.Id == chId, ct)) continue;
                    db.BookChapterOrder.Add(new BookChapterOrder { BookId = id, Position = pos++, ChapterId = chId });
                }
                await db.SaveChangesAsync(ct);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"book {b.Title}: {DeepestMessage(ex)}");
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Unchanged).ToList())
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
        }

        return result;
    }
}

public sealed class JsonImportResult
{
    public int SourceCount { get; set; }
    public int Imported { get; set; }
    public List<string> Errors { get; set; } = new();
}

public sealed class ContinuityImportResult
{
    public bool Skipped { get; set; }
    public int Claims { get; set; }
    public int Contradictions { get; set; }
    public int Confirmations { get; set; }
    public int Runs { get; set; }
    public List<string> Errors { get; set; } = new();
}
