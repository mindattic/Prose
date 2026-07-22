using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
// JsonDefaults lives in the root StreetSamurai.Core namespace.
using StreetSamurai.Core;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Bidirectional mapper between the column/bridge schema (Character + 25 child
/// tables) and the domain model (CharacterData). This is the *only* place that
/// knows the column ↔ JSON-field correspondence — JsonImportService and
/// CharacterRepository both delegate to it so the mapping never drifts between
/// the import path and the application read/write path.
///
/// Reads use a single root query plus eager Includes. Writes wipe the bridge
/// rows by FK and re-insert (relational upsert) — same shape as
/// JsonImportService.UpsertCharacterAsync, factored here for reuse.
/// </summary>
public static class CharacterMapper
{
    /// <summary>
    /// Eager-load every active Character row + every child collection in one
    /// trip and project to CharacterData. The Records.Json column is never
    /// touched on this path.
    /// </summary>
    /// <summary>
    /// Lightweight list-view projection. Returns one <see cref="CharacterData"/>
    /// per active character with ONLY the fields the dictionary list view
    /// renders: <c>Id</c>, <c>Name</c>, <c>Slug</c> (via Entity), <c>Role</c>,
    /// <c>Status</c> (via LifeStatus), and <c>Tags</c>. No Includes, no
    /// bridge materialization, no per-character LINQ over BelongingsGear.
    /// Cold-loads in ~1 s where <see cref="LoadAll"/> took 50–80 s. Use this
    /// for any list/filter/select UI; call <see cref="LoadOne"/> to get the
    /// full record when a row is opened for edit.
    /// </summary>
    public static List<CharacterData> LoadAllLite(StreetSamuraiDbContext db)
    {
        var rows = db.Characters.AsNoTracking()
            .Join(db.Entities.AsNoTracking().Where(e => e.IsActive && e.EntityType == "character"),
                ch => ch.Id, e => e.Id,
                (ch, e) => new { Id = ch.Id, Name = e.Name, Role = ch.Role, LifeStatus = ch.LifeStatus, Rating = ch.Rating, VoteCount = ch.VoteCount })
            .ToList();

        if (rows.Count == 0) return new();

        var ids = rows.Select(r => r.Id).ToHashSet();
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = new List<CharacterData>(rows.Count);
        foreach (var r in rows)
        {
            tagsByEntity.TryGetValue(r.Id, out var tags);
            result.Add(new CharacterData
            {
                Id = r.Id.ToString("N"),
                Type = "character",
                Name = r.Name ?? "",
                Role = r.Role ?? "",
                Status = string.IsNullOrEmpty(r.LifeStatus) ? "alive" : r.LifeStatus,
                Rating = r.Rating,
                VoteCount = r.VoteCount,
                Tags = tags ?? new List<string>(),
            });
        }
        return result;
    }

    public static List<CharacterData> LoadAll(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var query = BuildIncludeChain(db.Characters.AsNoTracking());

        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "character")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "character" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();

        var characters = query.Where(c => ids.Contains(c.Id)).ToList();
        var entityById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionary(e => e.Id, e => e);
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        // Bulk-fetch the latest 'location' aspect for every character in this
        // batch. After the static/dynamic split (2026-05-08) Location lives in
        // EntityStateEvents, not on the Character entity. One indexed seek
        // per (EntityId, AspectKey) pair, batched to one query.
        var locationByCharId = LatestStateValues(db, ids, "location");

        var result = new List<CharacterData>(characters.Count);
        foreach (var c in characters)
        {
            entityById.TryGetValue(c.Id, out var entity);
            tagsByEntity.TryGetValue(c.Id, out var tags);
            locationByCharId.TryGetValue(c.Id, out var loc);
            result.Add(Materialize(c, entity, tags, loc ?? ""));
        }
        return result;
    }

    public static CharacterData? LoadOne(StreetSamuraiDbContext db, Guid id)
    {
        var c = BuildIncludeChain(db.Characters.AsNoTracking())
            .FirstOrDefault(c => c.Id == id);
        if (c == null) return null;
        var entity = db.Entities.AsNoTracking().FirstOrDefault(e => e.Id == id);
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id)
            .Select(t => t.Tag!.Name)
            .ToList();
        var loc = LatestStateValue(db, id, "location") ?? "";
        return Materialize(c, entity, tags, loc);
    }

    // ─────────────────────────────────────────────────────────────────────
    // READ-MODEL (CQRS-lite). A derived projection cached in CharacterReadModels
    // so bulk full reads skip the 25-Include fan-out. The relational row +
    // bridges stay the source of truth; this is regenerated on every write and
    // is fully rebuildable. The two volatile fields sourced from other write
    // paths — Tags (EntityTags) and Location (EntityStateEvents) — are NOT
    // stored in the blob; they are overlaid live on read so it cannot drift.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Schema version of the serialized read-model shape. Bump this whenever
    /// <see cref="Materialize"/>'s output changes so existing rows are treated
    /// as stale and rebuilt rather than deserialized into a mismatched model.
    /// </summary>
    public const int ReadModelVersion = 1;

    private static readonly JsonSerializerOptions ReadModelWrite = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Fast bulk read off the materialized projection. Deserializes the cached
    /// blob for every active character (one column read, no Includes), then
    /// overlays the live volatile fields (Tags + Location). Any character whose
    /// read-model is missing or below <see cref="ReadModelVersion"/> is
    /// materialized relationally and backfilled so the store self-heals.
    /// </summary>
    public static List<CharacterData> LoadAllFromReadModel(StreetSamuraiDbContext db, bool includeArchived = false)
    {
        var ids = (includeArchived
            ? db.Entities.AsNoTracking().Where(e => e.EntityType == "character")
            : db.Entities.AsNoTracking().Where(e => e.EntityType == "character" && e.IsActive))
            .Select(e => e.Id)
            .ToHashSet();
        if (ids.Count == 0) return new();

        // Modified-since check catches out-of-band writes (direct SQL) that never went
        // through CharacterRepository.Save, so RefreshReadModelAsync never fired.
        var modifiedAtById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionary(e => e.Id, e => e.ModifiedAt);

        var fresh = db.CharacterReadModels.AsNoTracking()
            .Where(r => ids.Contains(r.CharacterId) && r.Version == ReadModelVersion)
            .Select(r => new { r.CharacterId, r.Json, r.RefreshedAt })
            .ToList();

        var result = new List<CharacterData>(ids.Count);
        var have = new HashSet<Guid>(fresh.Count);
        foreach (var r in fresh)
        {
            if (modifiedAtById.TryGetValue(r.CharacterId, out var modifiedAt) && modifiedAt > r.RefreshedAt)
                continue;                                // entity changed since cache was built → backfill

            var data = DeserializeReadModel(r.Json);
            if (data == null) continue;                 // corrupt blob → fall through to backfill
            result.Add(data);
            have.Add(r.CharacterId);
        }

        // Backfill anything missing / stale-version / corrupt — relational
        // materialize scoped to just those ids, then persist the rebuilt rows.
        var missing = ids.Where(id => !have.Contains(id)).ToHashSet();
        if (missing.Count > 0)
            result.AddRange(BackfillReadModels(db, missing));

        OverlayVolatile(db, result, ids);
        return result;
    }

    /// <summary>
    /// Fast single read off the projection. Falls back to the relational
    /// <see cref="LoadOne"/> (and backfills) when the row is missing/stale.
    /// </summary>
    public static CharacterData? LoadOneFromReadModel(StreetSamuraiDbContext db, Guid id)
    {
        var row = db.CharacterReadModels.AsNoTracking()
            .FirstOrDefault(r => r.CharacterId == id && r.Version == ReadModelVersion);

        CharacterData? data = null;
        if (row != null)
        {
            // Modified-since check catches out-of-band writes (direct SQL) that never
            // went through CharacterRepository.Save, so RefreshReadModelAsync never fired.
            var modifiedAt = db.Entities.AsNoTracking()
                .Where(e => e.Id == id).Select(e => (DateTime?)e.ModifiedAt).FirstOrDefault();
            if (modifiedAt == null || modifiedAt <= row.RefreshedAt)
                data = DeserializeReadModel(row.Json);
        }
        if (data == null)
        {
            data = LoadOne(db, id);
            if (data == null) return null;
            RefreshReadModelAsync(db, id).GetAwaiter().GetResult();   // self-heal
        }
        var tags = db.EntityTags.AsNoTracking()
            .Where(t => t.EntityId == id).Select(t => t.Tag!.Name).ToList();
        data.Tags = tags;
        data.Location = LatestStateValue(db, id, "location") ?? "";
        return data;
    }

    /// <summary>
    /// Regenerate one character's read-model from the freshly-persisted
    /// relational record and upsert it. Called by <c>CharacterRepository.Save</c>
    /// after its SaveChanges — the enforced single-writer sync. Self-contained
    /// (commits its own change). No-op if the character no longer exists.
    /// </summary>
    public static async Task RefreshReadModelAsync(StreetSamuraiDbContext db, Guid id, CancellationToken ct = default)
    {
        var data = LoadOne(db, id);
        if (data == null) return;
        await UpsertReadModelAsync(db, id, data, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Rebuild the entire projection from the relational source of truth (the
    /// one-time slow path) and prune rows for characters that no longer exist.
    /// Backs <c>ss --rebuild-readmodel</c>. Returns the number of rows written.
    /// </summary>
    public static async Task<int> RebuildAllReadModelsAsync(StreetSamuraiDbContext db, bool includeArchived = false, CancellationToken ct = default)
    {
        var all = LoadAll(db, includeArchived);
        foreach (var data in all)
        {
            if (Guid.TryParseExact(data.Id, "N", out var id) || Guid.TryParse(data.Id, out id))
                await UpsertReadModelAsync(db, id, data, ct);
        }

        // Prune orphans — read-model rows whose character is gone entirely.
        var liveIds = db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character").Select(e => e.Id).ToHashSet();
        await db.CharacterReadModels.Where(r => !liveIds.Contains(r.CharacterId)).ExecuteDeleteAsync(ct);

        await db.SaveChangesAsync(ct);
        return all.Count;
    }

    /// <summary>Upsert (track only — caller commits) the read-model row for one character.</summary>
    private static async Task UpsertReadModelAsync(StreetSamuraiDbContext db, Guid id, CharacterData data, CancellationToken ct)
    {
        var json = SerializeReadModel(data);
        var row = await db.CharacterReadModels.FirstOrDefaultAsync(r => r.CharacterId == id, ct);
        if (row == null)
        {
            db.CharacterReadModels.Add(new CharacterReadModel
            {
                CharacterId = id, Json = json, Version = ReadModelVersion, RefreshedAt = DateTime.UtcNow,
            });
        }
        else
        {
            row.Json = json;
            row.Version = ReadModelVersion;
            row.RefreshedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Serialize with the volatile (live-overlaid) fields cleared so the blob never looks authoritative on dynamic state.</summary>
    private static string SerializeReadModel(CharacterData data)
    {
        var savedTags = data.Tags;
        var savedLoc = data.Location;
        data.Tags = new List<string>();
        data.Location = "";
        try { return JsonSerializer.Serialize(data, ReadModelWrite); }
        finally { data.Tags = savedTags; data.Location = savedLoc; }
    }

    private static CharacterData? DeserializeReadModel(string json)
    {
        try { return JsonSerializer.Deserialize<CharacterData>(json, JsonDefaults.LlmParsing); }
        catch { return null; }
    }

    /// <summary>Relationally materialize a set of characters and persist their rebuilt read-models.</summary>
    private static List<CharacterData> BackfillReadModels(StreetSamuraiDbContext db, HashSet<Guid> ids)
    {
        var chars = BuildIncludeChain(db.Characters.AsNoTracking())
            .Where(c => ids.Contains(c.Id)).ToList();
        var entityById = db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id)).ToDictionary(e => e.Id, e => e);

        var rebuilt = new List<CharacterData>(chars.Count);
        foreach (var c in chars)
        {
            entityById.TryGetValue(c.Id, out var entity);
            var data = Materialize(c, entity, tags: null, currentLocation: "");
            rebuilt.Add(data);
            SaveReadModelSafe(db, c.Id, data);
        }
        return rebuilt;
    }

    // Saves one read-model row, handling the concurrent-insert race: two requests can
    // both see no row, both queue an Add, and the second SaveChanges blows up on PK.
    // On collision we clear the tracker and flip to an update.
    private static void SaveReadModelSafe(StreetSamuraiDbContext db, Guid id, CharacterData data)
    {
        var json = SerializeReadModel(data);
        var existing = db.CharacterReadModels.FirstOrDefault(r => r.CharacterId == id);
        if (existing == null)
        {
            db.CharacterReadModels.Add(new CharacterReadModel
            {
                CharacterId = id, Json = json, Version = ReadModelVersion, RefreshedAt = DateTime.UtcNow,
            });
            try
            {
                db.SaveChanges();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql
                      && (sql.Number == 2627 || sql.Number == 2601))
            {
                // Another writer inserted first — clear and update instead. The re-query is
                // universe-filtered: if the colliding row belongs to a different universe
                // (a shared CharacterId PK across universes), it is invisible here and the
                // read-model cache simply isn't refreshed this pass — never throw from a
                // best-effort cache backfill (SS-LAW-15). The row is regenerable next read.
                db.ChangeTracker.Clear();
                existing = db.CharacterReadModels.FirstOrDefault(r => r.CharacterId == id);
                if (existing == null) return;
                existing.Json = json;
                existing.Version = ReadModelVersion;
                existing.RefreshedAt = DateTime.UtcNow;
                db.SaveChanges();
            }
        }
        else
        {
            existing.Json = json;
            existing.Version = ReadModelVersion;
            existing.RefreshedAt = DateTime.UtcNow;
            db.SaveChanges();
        }
    }

    /// <summary>Overlay the live Tags + Location onto a batch of read-model-sourced records (one query each).</summary>
    private static void OverlayVolatile(StreetSamuraiDbContext db, List<CharacterData> records, HashSet<Guid> ids)
    {
        var tagsByEntity = db.EntityTags.AsNoTracking()
            .Where(t => ids.Contains(t.EntityId))
            .Select(t => new { t.EntityId, t.Tag!.Name })
            .ToList()
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());
        var locById = LatestStateValues(db, ids, "location");

        foreach (var d in records)
        {
            if (!Guid.TryParseExact(d.Id, "N", out var id) && !Guid.TryParse(d.Id, out id)) continue;
            d.Tags = tagsByEntity.TryGetValue(id, out var t) ? t : new List<string>();
            d.Location = locById.TryGetValue(id, out var loc) ? loc : "";
        }
    }

    /// <summary>
    /// Pull the most recent <c>NewValue</c> for (entityId, aspectKey) from
    /// <see cref="StreetSamurai.Core.Data.Entities.EntityStateEvent"/>. Returns
    /// null when no event exists. Single indexed seek.
    /// </summary>
    private static string? LatestStateValue(StreetSamuraiDbContext db, Guid entityId, string aspectKey)
        => db.EntityStateEvents.AsNoTracking()
            .Where(e => e.EntityId == entityId && e.AspectKey == aspectKey)
            .OrderByDescending(e => e.AtStoryTime).ThenByDescending(e => e.Id)
            .Select(e => e.NewValue)
            .FirstOrDefault();

    /// <summary>
    /// Bulk version: latest state value for one aspect across many entities.
    /// One round-trip; returned dictionary is keyed by EntityId.
    /// </summary>
    private static Dictionary<Guid, string> LatestStateValues(
        StreetSamuraiDbContext db, HashSet<Guid> entityIds, string aspectKey)
    {
        if (entityIds.Count == 0) return new();
        return db.EntityStateEvents.AsNoTracking()
            .Where(e => entityIds.Contains(e.EntityId) && e.AspectKey == aspectKey)
            .GroupBy(e => e.EntityId)
            .Select(g => g.OrderByDescending(e => e.AtStoryTime).ThenByDescending(e => e.Id).First())
            .ToList()
            .Where(e => e.NewValue != null)
            .ToDictionary(e => e.EntityId, e => e.NewValue!);
    }

    private static IQueryable<Character> BuildIncludeChain(IQueryable<Character> q)
        => q.AsSplitQuery()
            .Include(c => c.Aliases)
            .Include(c => c.StoryHooks)
            .Include(c => c.ArchetypeScores)
            .Include(c => c.GeneticAncestry)
            .Include(c => c.AncestryDetail)
            .Include(c => c.PsychologyTraits)
            .Include(c => c.SpeechPhrases)
            .Include(c => c.BehavioralRules)
            .Include(c => c.BehavioralMaps)
            .Include(c => c.StatScalars)
            .Include(c => c.StatPhrases)
            .Include(c => c.PhysicalMarks)
            .Include(c => c.TerritoryZones)
            .Include(c => c.TerritoryReputations)
            .Include(c => c.BelongingsGear)
            .Include(c => c.BelongingsExtras)
            .Include(c => c.BioBatteryThresholds)
            .Include(c => c.NeuralAbilities)
            .Include(c => c.Changelog)
            .Include(c => c.Cyberware)
            .Include(c => c.Knowledge).ThenInclude(k => k.RelatedEntities)
            .Include(c => c.Conditions)
            .Include(c => c.Relationships)
            .Include(c => c.Timeline).ThenInclude(t => t.BodyChanges)
            .Include(c => c.HomeTurfs).ThenInclude(h => h.Place)
            .Include(c => c.Affiliations).ThenInclude(a => a.Faction);

    /// <summary>
    /// Build a CharacterData from the entity + bridges that were loaded by
    /// <see cref="BuildIncludeChain"/>. Entity is used for the universal Name/Slug
    /// — every other field comes from the columnar Character row.
    /// </summary>
    public static CharacterData Materialize(Character c, Entity? entity, List<string>? tags, string currentLocation = "")
    {
        var data = new CharacterData
        {
            Id = c.Id.ToString("N"),
            Type = "character",
            Name = entity?.Name ?? "",
            Rating = c.Rating,
            VoteCount = c.VoteCount,
            Species = c.Species,
            Gender = c.Gender,
            Pronouns = c.Pronouns,
            Role = c.Role,
            Age = c.Age,
            Status = c.LifeStatus,
            Location = currentLocation,
            Description = c.Description,
            NarrativeFunction = c.NarrativeFunction,
            Augmentations = c.Augmentations,
            DailyLife = c.DailyLife,
            // Affiliation sourced from CharacterAffiliations bridge (denorm
            // column dropped 2026-05-08). Primary affiliation = first by Position.
            Affiliation = c.Affiliations.OrderBy(a => a.Position).Select(a => a.Alias).FirstOrDefault() ?? "",
            NarrationVoice = c.NarrationVoice,
            MidjourneyPrompt = c.MidjourneyPrompt,
            Dalle3Prompt = c.Dalle3Prompt,
            Tags = tags ?? new List<string>(),
        };

        // ── Lists / dicts ─────────────────────────────────────────────────
        data.Aliases    = c.Aliases.OrderBy(x => x.Position).Select(x => x.Value).ToList();
        data.StoryHooks = c.StoryHooks.OrderBy(x => x.Position).Select(x => x.Hook).ToList();

        data.Archetypes = c.ArchetypeScores.ToDictionary(x => x.ArchetypeName, x => x.Score);
        data.GeneticAncestry = c.GeneticAncestry.ToDictionary(x => x.Region, x => x.Percent);

        data.AncestryDetail = c.AncestryDetail
            .GroupBy(x => x.Region)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => x.SubRegion)
                      .ToDictionary(
                          sg => sg.Key,
                          sg => sg.ToDictionary(x => x.Nationality, x => x.Percent)));

        // Psychology
        data.Psychology = new CharacterPsychology
        {
            Secret           = c.PsychologySecret,
            CoreFears        = PickList(c.PsychologyTraits, "core_fears",        x => x.Trait),
            CoreDesires      = PickList(c.PsychologyTraits, "core_desires",      x => x.Trait),
            CopingMechanisms = PickList(c.PsychologyTraits, "coping_mechanisms", x => x.Trait),
            BlindSpots       = PickList(c.PsychologyTraits, "blind_spots",       x => x.Trait),
        };

        // Speech
        data.SpeechPatterns = new SpeechPatterns
        {
            Vocabulary       = c.SpeechVocabulary,
            Cadence          = c.SpeechCadence,
            Subtext          = c.SpeechSubtext,
            UnderPressure    = c.SpeechUnderPressure,
            IntimacyRegister = c.SpeechIntimacyRegister,
            VerbalTics   = PickList(c.SpeechPhrases, "verbal_tics",   x => x.Phrase),
            ExampleLines = PickList(c.SpeechPhrases, "example_lines", x => x.Phrase),
            Avoidances   = PickList(c.SpeechPhrases, "avoidances",    x => x.Phrase),
        };

        // Behavioral
        data.Behavioral = new CharacterBehavioral
        {
            DecisionRules    = PickList(c.BehavioralRules, "decision_rules",    x => x.Rule),
            EscalationLadder = PickList(c.BehavioralRules, "escalation_ladder", x => x.Rule),
            Contradictions   = PickList(c.BehavioralRules, "contradictions",    x => x.Rule),
            Habits           = PickList(c.BehavioralRules, "habits",            x => x.Rule),
            BreakingPoints   = PickList(c.BehavioralRules, "breaking_points",   x => x.Rule),
            InterpersonalModes = c.BehavioralMaps
                .Where(x => x.Bucket == "interpersonal_modes")
                .ToDictionary(x => x.KeyName, x => x.Value),
            StressResponses    = c.BehavioralMaps
                .Where(x => x.Bucket == "stress_responses")
                .ToDictionary(x => x.KeyName, x => x.Value),
        };

        // Stats
        data.Stats = new CharacterStats
        {
            Physical    = StatBucket(c.StatScalars, "physical"),
            Mental      = StatBucket(c.StatScalars, "mental"),
            Social      = StatBucket(c.StatScalars, "social"),
            Personality = StatBucket(c.StatScalars, "personality"),
            Thresholds  = StatBucket(c.StatScalars, "thresholds"),
            Drives     = PickList(c.StatPhrases, "drives",     x => x.Phrase),
            Strengths  = PickList(c.StatPhrases, "strengths",  x => x.Phrase),
            Weaknesses = PickList(c.StatPhrases, "weaknesses", x => x.Phrase),
            StatTags   = PickList(c.StatPhrases, "tags",       x => x.Phrase),
        };

        // Physical description
        data.PhysicalDescription = new PhysicalDescription
        {
            Heritage             = c.Heritage,
            HeightCm             = c.HeightCm,
            WeightKg             = c.WeightKg,
            Build                = c.Build,
            HairColor            = c.HairColor,
            HairStyle            = c.HairStyle,
            HairLength           = c.HairLength,
            EyeColor             = c.EyeColor,
            SkinTone             = c.SkinTone,
            Complexion           = c.Complexion,
            VisibleAugmentations = c.VisibleAugmentations,
            PostureMovement      = c.PostureMovement,
            ClothingStyle        = c.PhysicalClothingStyle,
            DistinguishingMarks  = c.PhysicalMarks.OrderBy(x => x.Position).Select(x => x.Mark).ToList(),
        };

        // Territory — HomeTurf sourced from CharacterHomeTurfs bridge (denorm
        // columns dropped 2026-05-08). Primary home turf = first by Position.
        data.Territory = new OperatingTerritory
        {
            HomeTurf = c.HomeTurfs.OrderBy(h => h.Position).Select(h => h.Alias).FirstOrDefault() ?? "",
            Range    = c.TerritoryRange,
            FamiliarZones = c.TerritoryZones.Where(x => x.Bucket == "familiar")
                                            .OrderBy(x => x.Position).Select(x => x.Zone).ToList(),
            NoGoZones     = c.TerritoryZones.Where(x => x.Bucket == "no_go")
                                            .OrderBy(x => x.Position).Select(x => x.Zone).ToList(),
            ZoneReputation = c.TerritoryReputations.ToDictionary(x => x.Zone, x => x.Reputation),
        };

        // Belongings — "current primary X" pointers sourced from the
        // CharacterBelongingsGear bridge after the 2026-05-08 scalar drop.
        // Each pointer is a single-row bucket; the bridge's existing
        // signature_gear / pharmaceuticals buckets keep list semantics.
        string PrimaryFromBucket(string bucket)
            => c.BelongingsGear.Where(x => x.Bucket == bucket)
                               .OrderBy(x => x.Position).Select(x => x.GearName).FirstOrDefault() ?? "";
        data.Belongings = new CharacterBelongings
        {
            PrimaryWeapon   = PrimaryFromBucket("primary_weapon"),
            SecondaryWeapon = PrimaryFromBucket("secondary_weapon"),
            Armor           = PrimaryFromBucket("armor"),
            Vehicle         = PrimaryFromBucket("vehicle"),
            Residence       = PrimaryFromBucket("residence"),
            ClothingStyle   = PrimaryFromBucket("clothing_style"),
            FavoriteDrink   = PrimaryFromBucket("favorite_drink"),
            FavoriteFood    = PrimaryFromBucket("favorite_food"),
            Stimulant       = PrimaryFromBucket("stimulant"),
            CommDevice      = PrimaryFromBucket("comm_device"),
            RangedWeapon    = PrimaryFromBucket("ranged_weapon"),
            ToolSlot        = PrimaryFromBucket("tool_slot"),
            CarriedLoot     = c.BelongingsGear.Where(x => x.Bucket == "carried_loot")
                                              .OrderBy(x => x.Position).Select(x => x.GearName).ToList(),
            SignatureGear   = c.BelongingsGear.Where(x => x.Bucket == "signature_gear")
                                              .OrderBy(x => x.Position).Select(x => x.GearName).ToList(),
            Pharmaceuticals = c.BelongingsGear.Where(x => x.Bucket == "pharmaceuticals")
                                              .OrderBy(x => x.Position).Select(x => x.GearName).ToList(),
            Other           = c.BelongingsExtras.ToDictionary(x => x.KeyName, x => x.Value),
        };

        // Bio-battery
        if (!string.IsNullOrEmpty(c.BioBatteryMaxCapacity)
            || !string.IsNullOrEmpty(c.BioBatteryRecovery)
            || c.BioBatteryThresholds.Count > 0)
        {
            data.BioBattery = new BioBatteryDefinition
            {
                MaxCapacityDescription = c.BioBatteryMaxCapacity,
                Recovery               = c.BioBatteryRecovery,
                DepletionThresholds    = c.BioBatteryThresholds.ToDictionary(x => x.Threshold, x => x.Consequence),
            };
        }

        // Neural abilities
        data.NeuralAbilities = c.NeuralAbilities.OrderBy(x => x.Position).Select(x => new NeuralAbilityDefinition
        {
            Name = x.Name, CostPercent = x.CostPercent, Description = x.Description,
            OverdrawnRisk = x.OverdrawnRisk, Passive = x.Passive,
        }).ToList();

        // Changelog
        data.Changelog = c.Changelog.OrderBy(x => x.Position).Select(x => new CharacterChangelog
        {
            StoryId = x.StoryId, Beat = x.Beat, Date = x.Date,
            Field = x.FieldName, From = x.FromValue, To = x.ToValue, Reason = x.Reason,
        }).ToList();

        // Cyberware / Knowledge / Conditions / Relationships / Timeline
        data.CyberwareInventory = c.Cyberware.Select(x => new CyberwareEntry
        {
            Name = x.Name, BodyLocation = x.BodyLocation, Manufacturer = x.Manufacturer,
            Tier = x.Tier, Condition = x.Condition, InstalledDate = x.InstalledDate,
            Description = x.Description, Replaces = x.Replaces,
        }).ToList();

        data.Knowledge = c.Knowledge.Select(k => new CharacterKnowledge
        {
            Topic = k.Topic, Summary = k.Summary,
            LearnedChapter = k.LearnedChapter, LearnedChapterId = k.LearnedChapterId,
            SourceBeat = k.SourceBeat, SourceSnippet = k.SourceSnippet,
            Entities = k.RelatedEntities.OrderBy(e => e.Position).Select(e => e.EntityRef).ToList(),
        }).ToList();

        data.Conditions = c.Conditions.Select(x => new CharacterCondition
        {
            Kind = x.Kind, Name = x.Name, Severity = x.Severity, Notes = x.Notes,
            SinceChapter = x.SinceChapter, UntilChapter = x.UntilChapter,
        }).ToList();

        data.Relationships = c.Relationships.Select(x => new CharacterRelationship
        {
            Name = x.TargetName, Type = x.Type, Description = x.Description,
            EmotionalCore = x.EmotionalCore, StoryTension = x.StoryTension, Status = x.Status,
            SinceChapter = x.SinceChapter, UntilChapter = x.UntilChapter,
        }).ToList();

        data.Timeline = c.Timeline.Select(t => new TimelineEvent
        {
            Date = t.Date, StoryId = t.StoryId, Event = t.Event,
            Consequences = t.Consequences, StatusChange = t.StatusChange,
            BodyChanges = t.BodyChanges.OrderBy(b => b.Position).Select(b => b.BodyChange).ToList(),
        }).ToList();

        return data;
    }

    private static List<string> PickList<TRow>(IEnumerable<TRow> rows, string bucket, Func<TRow, string> select)
        => rows.Where(r => GetBucket(r!) == bucket)
               .OrderBy(r => GetPosition(r!))
               .Select(select)
               .ToList();

    /// <summary>Reflect the Bucket / Position properties (every bucketed bridge type has them).</summary>
    private static string GetBucket(object row)
        => (string)(row.GetType().GetProperty("Bucket")?.GetValue(row) ?? "");
    private static int GetPosition(object row)
        => (int)(row.GetType().GetProperty("Position")?.GetValue(row) ?? 0);

    /// <summary>Materialize a Stats sub-dictionary (Dict&lt;string, JsonElement&gt;) from polymorphic StatScalar rows.</summary>
    private static Dictionary<string, JsonElement> StatBucket(IEnumerable<CharacterStatScalar> rows, string bucket)
    {
        var dict = new Dictionary<string, JsonElement>();
        foreach (var r in rows.Where(x => x.Bucket == bucket))
        {
            JsonElement el;
            switch (r.ValueKind)
            {
                case "number":
                    el = JsonDocument.Parse(r.ValueNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0").RootElement;
                    break;
                case "bool":
                    el = JsonDocument.Parse(r.ValueBool == true ? "true" : "false").RootElement;
                    break;
                case "null":
                    el = JsonDocument.Parse("null").RootElement;
                    break;
                case "array":
                case "object":
                    try { el = JsonDocument.Parse(r.ValueText ?? "null").RootElement; }
                    catch { el = JsonDocument.Parse("null").RootElement; }
                    break;
                default: // string
                    el = JsonDocument.Parse(JsonSerializer.Serialize(r.ValueText ?? "")).RootElement;
                    break;
            }
            dict[r.KeyName] = el;
        }
        return dict;
    }

    // ─────────────────────────────────────────────────────────────────────
    // WRITE PATH — drop bridge rows, repopulate columns + bridges from a
    // CharacterData. Same algorithm JsonImportService uses for upsert.
    // ─────────────────────────────────────────────────────────────────────

    public static async Task PersistAsync(StreetSamuraiDbContext db, Guid id, CharacterData src, CancellationToken ct = default)
    {
        var ch = await db.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);
        var isNew = ch == null;

        if (!isNew)
        {
            // Existing character — wipe child bridges (cascade deletes grandchildren).
            // Only run on update. New characters have no rows to wipe; skipping the
            // 27 round-trips makes seeds and bulk imports drastically faster
            // (especially on SQLite where each ExecuteDelete is its own transaction).
            await db.CharacterAliases.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterStoryHooks.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterArchetypeScores.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterGeneticAncestries.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterAncestryDetails.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterPsychologyTraits.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterSpeechPhrases.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterBehavioralRules.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterBehavioralMaps.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterStatScalars.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterStatPhrases.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterPhysicalMarks.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterTerritoryZones.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterTerritoryReputations.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterBelongingsGear.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterBelongingsExtras.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterBioBatteryThresholds.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterNeuralAbilities.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterChangelog.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterCyberware.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterKnowledge.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterConditions.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterRelationships.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterTimeline.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterHomeTurfs.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
            await db.CharacterAffiliations.Where(x => x.CharacterId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            ch = new Character { Id = id };
            db.Characters.Add(ch);
        }

        FillScalars(ch!, src);
        FillBridges(db, id, src);
    }

    /// <summary>Populate scalar columns on Character from src (no DB touch).</summary>
    public static void FillScalars(Character ch, CharacterData src)
    {
        // Names — parse the canonical Name into structured parts so the table
        // is queryable by surname, first name, etc.
        var name = src.Name ?? "";
        ch.Name = name;
        var parts = ParseName(name);
        ch.TitlePrefix = parts.Title;
        ch.FirstName   = parts.First;
        ch.MiddleName  = string.IsNullOrWhiteSpace(parts.Middle) ? null : parts.Middle;
        ch.LastName    = parts.Last;

        ch.Species           = string.IsNullOrEmpty(src.Species) ? "human" : src.Species;
        ch.Gender            = src.Gender ?? "";
        ch.Pronouns          = src.Pronouns ?? "";
        ch.Age               = src.Age;
        ch.Rating             = src.Rating;
        ch.VoteCount          = src.VoteCount;
        ch.LifeStatus        = string.IsNullOrEmpty(src.Status) ? "alive" : src.Status;
        // Location moved to EntityStateEvents (aspect:location). Save handler is
        // responsible for emitting a state event when src.Location differs from
        // the current ledger value — see CharacterRepository.Save.
        // Affiliation flat column dropped 2026-05-08 — bridge populated by FillBridges.
        ch.Role              = src.Role ?? "";
        ch.Description       = src.Description ?? "";
        ch.NarrativeFunction = src.NarrativeFunction ?? "";
        ch.NarrationVoice    = src.NarrationVoice ?? "";
        ch.Augmentations     = src.Augmentations ?? "";
        ch.DailyLife         = src.DailyLife ?? "";
        ch.MidjourneyPrompt  = src.MidjourneyPrompt ?? "";
        ch.Dalle3Prompt      = src.Dalle3Prompt ?? "";

        // Belongings* scalar columns dropped 2026-05-08 — the "current primary X"
        // pointers are now single-row buckets in CharacterBelongingsGear (see
        // FillBridges). Materialize sources them via PrimaryFromBucket.

        var t = src.Territory ?? new();
        // TerritoryHomeTurf / HomeTurf flat columns dropped 2026-05-08 —
        // bridge (CharacterHomeTurfs) is populated by FillBridges.
        ch.TerritoryRange    = string.IsNullOrEmpty(t.Range) ? "local" : t.Range;

        var p = src.PhysicalDescription ?? new();
        ch.Heritage              = p.Heritage ?? "";
        ch.HeightCm              = p.HeightCm;
        ch.WeightKg              = p.WeightKg;
        ch.Build                 = p.Build ?? "";
        ch.HairColor             = p.HairColor ?? "";
        ch.HairStyle             = p.HairStyle ?? "";
        ch.HairLength            = p.HairLength ?? "";
        ch.EyeColor              = p.EyeColor ?? "";
        ch.SkinTone              = p.SkinTone ?? "";
        ch.Complexion            = p.Complexion ?? "";
        ch.VisibleAugmentations  = p.VisibleAugmentations ?? "";
        ch.PostureMovement       = p.PostureMovement ?? "";
        ch.PhysicalClothingStyle = p.ClothingStyle ?? "";

        ch.PsychologySecret = src.Psychology?.Secret ?? "";

        var sp = src.SpeechPatterns ?? new();
        ch.SpeechVocabulary       = sp.Vocabulary ?? "";
        ch.SpeechCadence          = sp.Cadence ?? "";
        ch.SpeechSubtext          = sp.Subtext ?? "";
        ch.SpeechUnderPressure    = sp.UnderPressure ?? "";
        ch.SpeechIntimacyRegister = sp.IntimacyRegister ?? "";

        ch.BioBatteryMaxCapacity = src.BioBattery?.MaxCapacityDescription ?? "";
        ch.BioBatteryRecovery    = src.BioBattery?.Recovery ?? "";
    }

    /// <summary>Insert all bridge rows (assumes the parent's existing bridges have already been wiped).</summary>
    public static void FillBridges(StreetSamuraiDbContext db, Guid id, CharacterData src)
    {
        for (int i = 0; i < src.Aliases.Count; i++)
            db.CharacterAliases.Add(new CharacterAlias { CharacterId = id, Position = i, Value = src.Aliases[i] ?? "" });
        for (int i = 0; i < src.StoryHooks.Count; i++)
            db.CharacterStoryHooks.Add(new CharacterStoryHook { CharacterId = id, Position = i, Hook = src.StoryHooks[i] ?? "" });

        foreach (var kv in src.Archetypes)
            db.CharacterArchetypeScores.Add(new CharacterArchetypeScore { CharacterId = id, ArchetypeName = kv.Key, Score = kv.Value });
        foreach (var kv in src.GeneticAncestry)
            db.CharacterGeneticAncestries.Add(new CharacterGeneticAncestry { CharacterId = id, Region = kv.Key, Percent = kv.Value });

        foreach (var (region, subDict) in src.AncestryDetail)
            foreach (var (subRegion, natDict) in subDict)
                foreach (var (nationality, percent) in natDict)
                    db.CharacterAncestryDetails.Add(new CharacterAncestryDetail
                    {
                        CharacterId = id, Region = region, SubRegion = subRegion,
                        Nationality = nationality, Percent = percent,
                    });

        AddTraits(db, id, src.Psychology?.CoreFears,        "core_fears");
        AddTraits(db, id, src.Psychology?.CoreDesires,      "core_desires");
        AddTraits(db, id, src.Psychology?.CopingMechanisms, "coping_mechanisms");
        AddTraits(db, id, src.Psychology?.BlindSpots,       "blind_spots");

        AddSpeechPhrases(db, id, src.SpeechPatterns?.VerbalTics,   "verbal_tics");
        AddSpeechPhrases(db, id, src.SpeechPatterns?.ExampleLines, "example_lines");
        AddSpeechPhrases(db, id, src.SpeechPatterns?.Avoidances,   "avoidances");

        AddRules(db, id, src.Behavioral?.DecisionRules,    "decision_rules");
        AddRules(db, id, src.Behavioral?.EscalationLadder, "escalation_ladder");
        AddRules(db, id, src.Behavioral?.Contradictions,   "contradictions");
        AddRules(db, id, src.Behavioral?.Habits,           "habits");
        AddRules(db, id, src.Behavioral?.BreakingPoints,   "breaking_points");

        if (src.Behavioral != null)
        {
            foreach (var kv in src.Behavioral.InterpersonalModes)
                db.CharacterBehavioralMaps.Add(new CharacterBehavioralMap { CharacterId = id, Bucket = "interpersonal_modes", KeyName = kv.Key, Value = kv.Value ?? "" });
            foreach (var kv in src.Behavioral.StressResponses)
                db.CharacterBehavioralMaps.Add(new CharacterBehavioralMap { CharacterId = id, Bucket = "stress_responses",    KeyName = kv.Key, Value = kv.Value ?? "" });
        }

        AddStatScalars(db, id, src.Stats?.Physical,    "physical");
        AddStatScalars(db, id, src.Stats?.Mental,      "mental");
        AddStatScalars(db, id, src.Stats?.Social,      "social");
        AddStatScalars(db, id, src.Stats?.Personality, "personality");
        AddStatScalars(db, id, src.Stats?.Thresholds,  "thresholds");
        AddStatPhrases(db, id, src.Stats?.Drives,     "drives");
        AddStatPhrases(db, id, src.Stats?.Strengths,  "strengths");
        AddStatPhrases(db, id, src.Stats?.Weaknesses, "weaknesses");
        AddStatPhrases(db, id, src.Stats?.StatTags,   "tags");

        for (int i = 0; i < (src.PhysicalDescription?.DistinguishingMarks?.Count ?? 0); i++)
            db.CharacterPhysicalMarks.Add(new CharacterPhysicalMark { CharacterId = id, Position = i, Mark = src.PhysicalDescription!.DistinguishingMarks[i] ?? "" });

        AddTerritoryZones(db, id, src.Territory?.FamiliarZones, "familiar");
        AddTerritoryZones(db, id, src.Territory?.NoGoZones,     "no_go");
        if (src.Territory != null)
            foreach (var kv in src.Territory.ZoneReputation)
                db.CharacterTerritoryReputations.Add(new CharacterTerritoryReputation { CharacterId = id, Zone = kv.Key, Reputation = kv.Value ?? "" });

        var b = src.Belongings ?? new();
        AddGear(db, id, b.SignatureGear,   "signature_gear");
        AddGear(db, id, b.Pharmaceuticals, "pharmaceuticals");
        // Single-row "primary X" buckets — carry the 10 dropped scalar columns
        // through the bridge with consistent shape. Empty strings produce no row.
        AddPrimary(db, id, b.PrimaryWeapon,   "primary_weapon");
        AddPrimary(db, id, b.SecondaryWeapon, "secondary_weapon");
        AddPrimary(db, id, b.Armor,           "armor");
        AddPrimary(db, id, b.Vehicle,         "vehicle");
        AddPrimary(db, id, b.Residence,       "residence");
        AddPrimary(db, id, b.ClothingStyle,   "clothing_style");
        AddPrimary(db, id, b.FavoriteDrink,   "favorite_drink");
        AddPrimary(db, id, b.FavoriteFood,    "favorite_food");
        AddPrimary(db, id, b.Stimulant,       "stimulant");
        AddPrimary(db, id, b.CommDevice,      "comm_device");
        foreach (var kv in b.Other)
            db.CharacterBelongingsExtras.Add(new CharacterBelongingsExtra { CharacterId = id, KeyName = kv.Key, Value = kv.Value ?? "" });

        if (src.BioBattery != null)
            foreach (var kv in src.BioBattery.DepletionThresholds)
                db.CharacterBioBatteryThresholds.Add(new CharacterBioBatteryThreshold { CharacterId = id, Threshold = kv.Key, Consequence = kv.Value ?? "" });

        for (int i = 0; i < src.NeuralAbilities.Count; i++)
        {
            var na = src.NeuralAbilities[i];
            db.CharacterNeuralAbilities.Add(new CharacterNeuralAbility
            {
                CharacterId = id, Position = i,
                Name = na.Name ?? "", CostPercent = na.CostPercent,
                Description = na.Description ?? "", OverdrawnRisk = na.OverdrawnRisk ?? "",
                Passive = na.Passive,
            });
        }

        for (int i = 0; i < src.Changelog.Count; i++)
        {
            var cl = src.Changelog[i];
            db.CharacterChangelog.Add(new CharacterChangelogRow
            {
                CharacterId = id, Position = i,
                StoryId = cl.StoryId ?? "", Beat = cl.Beat ?? "", Date = cl.Date ?? "",
                FieldName = cl.Field ?? "", FromValue = cl.From ?? "", ToValue = cl.To ?? "", Reason = cl.Reason ?? "",
            });
        }

        foreach (var c in src.CyberwareInventory)
            db.CharacterCyberware.Add(new CharacterCyberware
            {
                CharacterId = id,
                Name = c.Name ?? "", BodyLocation = c.BodyLocation ?? "", Manufacturer = c.Manufacturer ?? "",
                Tier = c.Tier ?? "", Condition = string.IsNullOrEmpty(c.Condition) ? "functional" : c.Condition,
                InstalledDate = c.InstalledDate ?? "", Description = c.Description ?? "", Replaces = c.Replaces ?? "",
            });

        foreach (var c in src.Conditions)
            db.CharacterConditions.Add(new CharacterConditionRow
            {
                CharacterId = id,
                Kind = c.Kind ?? "", Name = c.Name ?? "", Severity = c.Severity ?? "",
                Notes = c.Notes ?? "", SinceChapter = c.SinceChapter, UntilChapter = c.UntilChapter,
            });

        foreach (var r in src.Relationships)
            db.CharacterRelationships.Add(new CharacterRelationshipRow
            {
                CharacterId = id,
                TargetName = r.Name ?? "", Type = r.Type ?? "", Description = r.Description ?? "",
                EmotionalCore = r.EmotionalCore ?? "", StoryTension = r.StoryTension ?? "",
                Status = string.IsNullOrEmpty(r.Status) ? "active" : r.Status,
                SinceChapter = r.SinceChapter, UntilChapter = r.UntilChapter,
            });

        foreach (var k in src.Knowledge)
        {
            var krow = new CharacterKnowledgeRow
            {
                CharacterId = id,
                Topic = k.Topic ?? "", Summary = k.Summary ?? "",
                LearnedChapter = k.LearnedChapter, LearnedChapterId = k.LearnedChapterId,
                SourceBeat = k.SourceBeat, SourceSnippet = k.SourceSnippet,
            };
            for (int i = 0; i < k.Entities.Count; i++)
                krow.RelatedEntities.Add(new CharacterKnowledgeEntity { Position = i, EntityRef = k.Entities[i] ?? "" });
            db.CharacterKnowledge.Add(krow);
        }

        foreach (var ev in src.Timeline)
        {
            var trow = new CharacterTimelineEvent
            {
                CharacterId = id,
                Date = ev.Date ?? "", StoryId = ev.StoryId ?? "",
                Event = ev.Event ?? "", Consequences = ev.Consequences ?? "",
                StatusChange = ev.StatusChange ?? "",
            };
            for (int i = 0; i < ev.BodyChanges.Count; i++)
                trow.BodyChanges.Add(new CharacterTimelineBodyChange { Position = i, BodyChange = ev.BodyChanges[i] ?? "" });
            db.CharacterTimeline.Add(trow);
        }

        // ── Resolved-entity bridges ───────────────────────────────────────
        // Source has at most one HomeTurf string and one Affiliation string,
        // but the schema is 1:M because a character can accumulate many over
        // their canonical history. The first row is index 0; future writes
        // (e.g. relocation) append.
        var homeTurf = src.Territory?.HomeTurf?.Trim();
        if (!string.IsNullOrEmpty(homeTurf))
        {
            var placeId = ResolveEntityId(db, "place", homeTurf);
            db.CharacterHomeTurfs.Add(new CharacterHomeTurf
            {
                CharacterId = id, Position = 0, Alias = homeTurf, PlaceId = placeId,
            });
        }

        var affiliation = src.Affiliation?.Trim();
        if (!string.IsNullOrEmpty(affiliation))
        {
            var factionId = ResolveEntityId(db, "faction", affiliation);
            db.CharacterAffiliations.Add(new CharacterAffiliation
            {
                CharacterId = id, Position = 0, Alias = affiliation, FactionId = factionId,
            });
        }
    }

    /// <summary>
    /// Look up the canonical Entity id of the given type for a free-form name.
    /// Tries exact name match first, then slug. Returns null when nothing matches —
    /// the bridge keeps the alias either way so display still works.
    /// </summary>
    private static Guid? ResolveEntityId(StreetSamuraiDbContext db, string entityType, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var slug = StreetSamurai.Core.Services.WorldGraphService.Slugify(name);
        return db.Entities
            .Where(e => e.EntityType == entityType && e.IsActive
                && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefault();
    }

    private static void AddTraits(StreetSamuraiDbContext db, Guid id, IReadOnlyList<string>? items, string bucket)
    {
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
            db.CharacterPsychologyTraits.Add(new CharacterPsychologyTrait { CharacterId = id, Bucket = bucket, Position = i, Trait = items[i] ?? "" });
    }
    private static void AddSpeechPhrases(StreetSamuraiDbContext db, Guid id, IReadOnlyList<string>? items, string bucket)
    {
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
            db.CharacterSpeechPhrases.Add(new CharacterSpeechPhrase { CharacterId = id, Bucket = bucket, Position = i, Phrase = items[i] ?? "" });
    }
    private static void AddRules(StreetSamuraiDbContext db, Guid id, IReadOnlyList<string>? items, string bucket)
    {
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
            db.CharacterBehavioralRules.Add(new CharacterBehavioralRule { CharacterId = id, Bucket = bucket, Position = i, Rule = items[i] ?? "" });
    }
    private static void AddStatPhrases(StreetSamuraiDbContext db, Guid id, IReadOnlyList<string>? items, string bucket)
    {
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
            db.CharacterStatPhrases.Add(new CharacterStatPhrase { CharacterId = id, Bucket = bucket, Position = i, Phrase = items[i] ?? "" });
    }
    private static void AddTerritoryZones(StreetSamuraiDbContext db, Guid id, IReadOnlyList<string>? items, string bucket)
    {
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
            db.CharacterTerritoryZones.Add(new CharacterTerritoryZone { CharacterId = id, Bucket = bucket, Position = i, Zone = items[i] ?? "" });
    }
    private static void AddGear(StreetSamuraiDbContext db, Guid id, IReadOnlyList<string>? items, string bucket)
    {
        if (items == null) return;
        for (int i = 0; i < items.Count; i++)
            db.CharacterBelongingsGear.Add(new CharacterBelongingsGear { CharacterId = id, Bucket = bucket, Position = i, GearName = items[i] ?? "" });
    }

    /// <summary>
    /// Insert one row in <see cref="CharacterBelongingsGear"/> for a "primary X"
    /// pointer (the bridge replacement for the 2026-05-08-dropped Belongings*
    /// scalar columns). No row is added when value is empty so the bridge
    /// stays sparse.
    /// </summary>
    private static void AddPrimary(StreetSamuraiDbContext db, Guid id, string? value, string bucket)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        db.CharacterBelongingsGear.Add(new CharacterBelongingsGear
        {
            CharacterId = id, Bucket = bucket, Position = 0, GearName = value,
        });
    }
    // ─────────────────────────────────────────────────────────────────────
    // Name parsing — runs at write time only (read time is a column read).
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Honorifics we strip when picking the first name. Stored separately in TitlePrefix.</summary>
    private static readonly HashSet<string> NameTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dr.", "Dr", "Mr.", "Mr", "Ms.", "Ms", "Mrs.", "Mrs", "Mx.", "Mx",
        "Prof.", "Prof", "Sir", "Dame", "Lord", "Lady",
        "Captain", "Capt.", "Capt", "Cmdr.", "Cmdr", "Lt.", "Lt", "Col.", "Col",
        "Major", "Maj.", "Maj", "Sgt.", "Sgt", "Det.", "Det", "Officer",
        "Reverend", "Rev.", "Rev", "Father", "Fr.", "Sister", "Brother",
        "Auntie", "Uncle", "Granny", "Grandpa",
    };

    public readonly record struct NameParts(string Title, string First, string Middle, string Last);

    /// <summary>
    /// Split a free-form full name into Title / First / Middle / Last. Handles
    /// honorifics, mononyms, and quoted aliases. Quoted segments
    /// (e.g. "Sasha 'Lena Connor' Võ") are removed from name-part assignment —
    /// they're stored separately in the Aliases bridge.
    /// </summary>
    public static NameParts ParseName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return new("", "", "", "");

        // Drop anything between paired single or double quotes — those are aliases,
        // not part of the structured name.
        var stripped = System.Text.RegularExpressions.Regex.Replace(fullName, "(['\"])([^'\"]*)\\1", "");
        var tokens = stripped.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return new("", "", "", "");

        // Pull off a leading title if present. We allow at most one title token.
        var title = "";
        var idx = 0;
        if (NameTitles.Contains(tokens[0]))
        {
            title = tokens[0];
            idx = 1;
        }

        var nameTokens = tokens.Skip(idx).ToArray();
        return nameTokens.Length switch
        {
            0 => new(title, "", "", ""),
            1 => new(title, nameTokens[0], "", ""),
            2 => new(title, nameTokens[0], "", nameTokens[1]),
            _ => new(title, nameTokens[0], string.Join(' ', nameTokens[1..^1]), nameTokens[^1]),
        };
    }

    private static void AddStatScalars(StreetSamuraiDbContext db, Guid id, Dictionary<string, JsonElement>? bucket, string bucketName)
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
                    // Stats values are scalar (number / string / bool) by design;
                    // arrays and objects don't belong here. If we ever see one, it's
                    // a data shape we need to model explicitly — log loudly and skip
                    // rather than silently dump JSON into a column.
                    Serilog.Log.Warning(
                        "StatScalar dropped: bucket={Bucket} key={Key} kind={Kind} — not a scalar; promote to its own bridge if real",
                        bucketName, key, el.ValueKind);
                    continue;
            }
            db.CharacterStatScalars.Add(row);
        }
    }
}
