using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

// ── Interchange DTOs (RFC 0007 — docs/schemas/universe.schema.json) ────────────

public class InterchangeUniverse
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Tagline { get; set; }
    public string? Era { get; set; }
    public string? Setting { get; set; }
    public string? Logline { get; set; }
    public List<string> Rules { get; set; } = new();
}

public class InterchangeRelation
{
    public string To { get; set; } = "";
    public string Kind { get; set; } = "";
}

public class InterchangeEntity
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public Dictionary<string, JsonElement>? Details { get; set; }
    public List<InterchangeRelation> Relations { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    /// <summary>Export only (PRS-T923): the entity's database id. Ignored on import, never stored in the record.</summary>
    public string? Guid { get; set; }
    /// <summary>Export only (PRS-T923): <c>import</c> for an entity an interchange import brought in (the consumer's),
    /// <c>prose</c> for one Prose authored itself, which a replace import never retires and a consumer's round-trip
    /// check sets aside. Ignored on import, never stored in the record.</summary>
    public string? Origin { get; set; }
    /// <summary>Export only (PRS-T923): the live relations Prose authored from this entity (its Edges rows not written by
    /// an interchange import, e.g. an author linking the game's NPC to a faction in the Hub). Not part of the game's
    /// file, so not in <see cref="Relations"/>; listed so a consumer's leak check can tell them from a real leak.
    /// Omitted when there are none. Ignored on import, never stored in the record.</summary>
    public List<InterchangeRelation>? ProseRelations { get; set; }
}

public static class InterchangeOrigins
{
    public const string Import = "import";
    public const string Prose = "prose";
}

public class InterchangeFile
{
    public InterchangeUniverse Universe { get; set; } = new();
    /// <summary>Optional provenance block (generator, build, seed, generatedAt, consumer), kept verbatim with the
    /// universe and returned on export (PRS-T916).</summary>
    public JsonElement? Source { get; set; }
    public List<InterchangeEntity> Entities { get; set; } = new();
}

/// <summary>
/// RFC 0007 "Universe Interchange" — import/export between the
/// <c>&lt;app&gt;/universe/&lt;slug&gt;.universe.json</c> contract
/// (<c>docs/schemas/universe.schema.json</c>) and Prose's generic Entity spine.
///
/// Deliberately self-contained: every query uses <c>IgnoreQueryFilters()</c> plus an explicit
/// <c>UniverseId</c> predicate rather than relying on the ambient <see cref="UniverseScope"/>/
/// <see cref="IUniverseContext.SetFlowUniverse"/> mechanism. A CLI call scopes itself via
/// <c>--universe</c>, but an MCP tool call (via Prose.Hub's ToolDispatch) carries no such
/// ambient scope at all — this service must be correct under either caller, and under
/// concurrent calls targeting different universes, without mutating any shared/async-local
/// state. See SS-LAW-15 (universe segregation is structural, not caller-discipline).
///
/// Storage design (a documented, minimal deviation from the RFC's suggested type-mapping
/// table — see docs/rfc/0007-universe-interchange.md "Deviations" section):
/// EVERY interchange entity — including character/location/faction — is stored on the
/// generic Entity + Record + EntityTag + Edge spine (the same shape <c>EfRepository&lt;T&gt;</c>
/// uses), never on the fully-relational Character/Place/Faction typed tables. Those typed
/// tables are designed around Prose's own ~15-25-bridge-table domain model (aliases, story
/// hooks, psychology, etc.) and are populated exclusively through their own mappers
/// (CharacterMapper/PlaceMapper/FactionMapper) — forcing sparse interchange data through
/// that machinery would be fragile for no benefit, and those repositories explicitly do
/// NOT read from Records.Json anymore. The RFC itself designates Record.Json as the
/// "round-trip source of truth" for import/export, which is exactly the generic-spine
/// storage model. EntityType strings still follow the RFC's semantic mapping
/// (character/place/faction/creature/artifact/event/rule/...) for readability and future
/// typed-view support; only the persistence path differs.
/// </summary>
public class UniverseInterchangeService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly OutboxService? outbox;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>EntityTypes that already have first-class meaning elsewhere in the app
    /// (RepoNameMap) and so don't need a RepositoryDefinition row of their own, even though
    /// this service never touches their typed tables — see the class doc-comment.</summary>
    private static readonly HashSet<string> BuiltInEntityTypes =
        new(StringComparer.OrdinalIgnoreCase) { "character", "place", "faction" };

    private const string UniverseSourceSettingKey = "interchange.universe_source";

    private readonly IUniverseContext? universeContext;

    public UniverseInterchangeService(IDbContextFactory<ProseDbContext> dbFactory, OutboxService? outbox = null,
        IUniverseContext? universeContext = null)
    {
        this.dbFactory = dbFactory;
        this.outbox = outbox;
        this.universeContext = universeContext;
    }

    public class ImportResult
    {
        public string UniverseSlug { get; set; } = "";
        public bool UniverseCreated { get; set; }
        public int EntitiesCreated { get; set; }
        /// <summary>Entities of the file that already existed (changed or not; see <see cref="EntitiesUnchanged"/>).</summary>
        public int EntitiesUpdated { get; set; }
        /// <summary>Of <see cref="EntitiesUpdated"/>, those whose stored state already matched the file: nothing was
        /// written for them, so their ModifiedAt (and every read receipt keyed on it) is untouched.</summary>
        public int EntitiesUnchanged { get; set; }
        /// <summary>Entities a replace import retired (Status = archived) because the file no longer has them.</summary>
        public int EntitiesRetired { get; set; }
        /// <summary>Previously retired entities this file brought back.</summary>
        public int EntitiesRevived { get; set; }
        public int StubsCreated { get; set; }
        public int StubsPromoted { get; set; }
        public int EdgesCreated { get; set; }
        /// <summary>Imported relations a replace import invalidated because their entity no longer lists them.</summary>
        public int EdgesInvalidated { get; set; }
        public string Mode { get; set; } = ImportModes.Merge;
        public string? Session { get; set; }
        public bool Final { get; set; } = true;
        /// <summary>What a replace import pruned against: <c>manifest</c> (the entities earlier imports brought in) or
        /// <c>records</c> (no manifest yet: every non-authored canon entity with a record). Null when nothing was pruned.</summary>
        public string? PruneBasis { get; set; }
        public List<string> Retired { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

    public static class ImportModes
    {
        /// <summary>Upsert only; nothing is ever retired (RFC 0007's original behaviour, the default).</summary>
        public const string Merge = "merge";
        /// <summary>The file (or the union of a session's batches) is the complete set of the consumer's entities.</summary>
        public const string Replace = "replace";
    }

    /// <summary>How one import call behaves. A batched push passes the same <see cref="Session"/> on every batch and
    /// <see cref="Final"/> = true only on the last: the prune, the manifest update and the single Outbox notice happen
    /// there, over the union of every batch (PRS-T916).</summary>
    public record ImportOptions(string Mode = ImportModes.Merge, string? Session = null, bool Final = true, bool Notify = true);

    private const string ManifestSettingKey = "interchange.manifest";
    private const string SourceSettingKey = "interchange.source";
    private const string SessionSettingPrefix = "interchange.session.";
    private const int SaveBatchSize = 500;

    private class SessionState
    {
        public List<string> Keys { get; set; } = new();
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Unchanged { get; set; }
        public int Edges { get; set; }
        public int Stubs { get; set; }
        public int Batches { get; set; }
    }

    public Task<ImportResult> ImportAsync(string json, string? universeSlugOverride = null, CancellationToken ct = default) =>
        ImportAsync(json, universeSlugOverride, new ImportOptions(), ct);

    /// <summary>
    /// Idempotent upsert-by-(UniverseId, Type, Slug) import. Re-running with the same file is a no-op: an entity whose
    /// stored record, name, summary, tags and status already match is not written at all.
    ///
    /// <para>PRS-T916: the universe's entities, records, tags and imported edges are read once and the writes are
    /// saved in batches of <see cref="SaveBatchSize"/>, instead of two or three round trips per entity and one per edge.
    /// <see cref="ImportOptions.Mode"/> <c>replace</c> retires (never deletes) entities earlier imports brought in that
    /// the file no longer has, and invalidates imported relations an entity no longer lists. Entities Prose authored
    /// itself and texts (RFC 0017) are never retired by an import.</para>
    /// </summary>
    /// <param name="universeSlugOverride">Explicit target universe slug (from <c>--universe</c>).
    /// Defaults to the file's own <c>universe.id</c> (already lowercase per the schema's
    /// <c>^[a-z0-9-]+$</c> pattern) — NOT uppercased, matching every existing universe row's
    /// lowercase Slug convention (glmz/scry/gospel/eve/...); see the RFC deviations note.</param>
    public async Task<ImportResult> ImportAsync(string json, string? universeSlugOverride, ImportOptions options, CancellationToken ct = default)
    {
        var mode = (options.Mode ?? ImportModes.Merge).Trim().ToLowerInvariant();
        var result = new ImportResult { Mode = mode, Session = options.Session, Final = options.Final };
        if (mode is not (ImportModes.Merge or ImportModes.Replace))
        {
            result.Errors.Add($"unknown import mode '{options.Mode}' (merge or replace)");
            return result;
        }
        if (options.Session != null && !System.Text.RegularExpressions.Regex.IsMatch(options.Session, "^[A-Za-z0-9_-]{1,64}$"))
        {
            result.Errors.Add("session must be 1-64 letters, digits, '-' or '_'");
            return result;
        }

        InterchangeFile file;
        try
        {
            file = JsonSerializer.Deserialize<InterchangeFile>(json, JsonOpts)
                ?? throw new InvalidOperationException("empty or null document");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"parse failed: {ex.Message}");
            return result;
        }

        var slug = (universeSlugOverride ?? file.Universe.Id ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
        {
            result.Errors.Add("universe slug is required (pass --universe or set universe.id in the file)");
            return result;
        }
        result.UniverseSlug = slug;

        if (file.Entities.FirstOrDefault(e => string.Equals((e.Type ?? "").Trim(), ProseTextService.EntityType, StringComparison.OrdinalIgnoreCase)) is { } reserved)
        {
            result.Errors.Add($"entity '{reserved.Id}' uses the reserved type '{ProseTextService.EntityType}' (Prose's own texts, docs/CONDUIT.md)");
            return result;
        }

        if (file.Entities.Select(e => Slugify(e.Id)).GroupBy(s => s).FirstOrDefault(g => g.Count() > 1) is { } dupe)
        {
            result.Errors.Add($"duplicate entity id '{dupe.Key}' in source file — entity ids must be unique per file");
            return result;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var universe = await FindOrCreateUniverseAsync(db, slug, file.Universe, ct);
        result.UniverseCreated = universe.createdNow;
        var universeId = universe.row.Id;

        await UpsertUniverseSourceAsync(db, universeId, file.Universe, ct);
        if (file.Source is { ValueKind: JsonValueKind.Object } source)
            await UpsertSettingAsync(db, universeId, SourceSettingKey, source.GetRawText(), ct);

        foreach (var type in file.Entities.Select(e => MapEntityType(e.Type)).Distinct())
            await EnsureRepositoryDefinitionAsync(db, type, ct);

        // One read of everything this import can touch (texts excluded: they are Prose's, RFC 0017).
        var all = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.EntityType != ProseTextService.EntityType)
            .ToListAsync(ct);
        var byTypeSlug = new Dictionary<(string, string), Entity>();
        foreach (var e in all) byTypeSlug.TryAdd((e.EntityType, e.Slug), e);
        var stubsBySlug = all.Where(e => e.Status == "stub").GroupBy(e => e.Slug).ToDictionary(g => g.Key, g => g.First());
        var records = await db.Records.IgnoreQueryFilters()
            .Where(r => db.Entities.IgnoreQueryFilters().Any(e => e.Id == r.EntityId && e.UniverseId == universeId
                                                                   && e.EntityType != ProseTextService.EntityType))
            .ToDictionaryAsync(r => r.EntityId, ct);
        var tagsByEntity = (await db.EntityTags.IgnoreQueryFilters()
                .Where(t => db.Entities.IgnoreQueryFilters().Any(e => e.Id == t.EntityId && e.UniverseId == universeId))
                .Select(t => new { t.EntityId, Name = t.Tag!.Name })
                .ToListAsync(ct))
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var wantedTagNames = file.Entities.SelectMany(e => e.Tags ?? new()).Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var tagsByName = (await db.Tags.Where(t => wantedTagNames.Contains(t.Name)).ToListAsync(ct))
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // slug → id for relation targets: every entity of the universe (a stub an EARLIER run made resolves even
        // if this file no longer mentions it). A same-slug pair of different types resolves to the first found.
        var slugToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in all) slugToId.TryAdd(e.Slug, e.Id);

        var now = DateTime.UtcNow;
        var pending = 0;
        async Task Tick()
        {
            if (++pending < SaveBatchSize) return;
            await db.SaveChangesAsync(ct);
            pending = 0;
        }

        // Pass 1: entities, records, tags.
        var fileKeys = new List<string>();
        var fileEntityIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var src in file.Entities)
        {
            var entityType = MapEntityType(src.Type);
            var entitySlug = Slugify(src.Id);
            src.Guid = null; // export-only fields (PRS-T923): a re-imported export must store the same record
            src.Origin = null;
            src.ProseRelations = null;
            var rawJson = JsonSerializer.Serialize(src, JsonOpts);
            fileKeys.Add(ManifestKey(entityType, entitySlug));

            byTypeSlug.TryGetValue((entityType, entitySlug), out var entity);
            // Same slug AND type first; a slug alone may only claim a stub (whose placeholder type is promoted).
            // Matching on slug alone overwrote the type, name and status of a different entity sharing it.
            if (entity == null && stubsBySlug.TryGetValue(entitySlug, out var stub))
            {
                entity = stub;
                stubsBySlug.Remove(entitySlug);
                byTypeSlug.Remove((stub.EntityType, stub.Slug));
                byTypeSlug[(entityType, entitySlug)] = stub;
            }

            if (entity == null)
            {
                entity = new Entity { UniverseId = universeId, Slug = entitySlug, CreatedAt = now };
                db.Entities.Add(entity);
                byTypeSlug[(entityType, entitySlug)] = entity;
                result.EntitiesCreated++;
            }
            else
            {
                result.EntitiesUpdated++;
                if (entity.Status == "stub") result.StubsPromoted++;
                if (entity.Status == "archived") result.EntitiesRevived++;
            }
            slugToId[entitySlug] = entity.Id;
            fileEntityIds[entitySlug] = entity.Id;

            var summary = string.IsNullOrWhiteSpace(src.Summary) ? entity.Description : src.Summary;
            records.TryGetValue(entity.Id, out var record);
            var wantTags = (src.Tags ?? new()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var haveTags = tagsByEntity.TryGetValue(entity.Id, out var ht) ? ht : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var missingTags = wantTags.Where(t => !haveTags.Contains(t)).ToList();

            var unchanged = entity.EntityType == entityType && entity.Name == src.Name && entity.Description == summary
                            && entity.Status == "canon" && record?.Json == rawJson && missingTags.Count == 0;
            if (unchanged) { result.EntitiesUnchanged++; continue; }

            entity.EntityType = entityType;
            entity.Name = src.Name;
            entity.Description = summary;
            entity.Status = "canon";
            entity.ModifiedAt = now;

            if (record == null)
            {
                record = new Record { EntityId = entity.Id };
                db.Records.Add(record);
                records[entity.Id] = record;
            }
            if (record.Json != rawJson) { record.Json = rawJson; record.UpdatedAt = now; }

            foreach (var name in missingTags)
            {
                if (!tagsByName.TryGetValue(name, out var tag))
                {
                    tag = new Tag { Name = name };
                    db.Tags.Add(tag);
                    tagsByName[name] = tag;
                }
                db.EntityTags.Add(new EntityTag { EntityId = entity.Id, Tag = tag });
                haveTags.Add(name);
            }
            tagsByEntity[entity.Id] = haveTags;
            await Tick();
        }
        await db.SaveChangesAsync(ct);
        pending = 0;

        // Pass 2: relations → edges (stubs for dangling targets). The imported edges were read once.
        var liveEdges = await db.Edges.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universeId && e.InvalidatedAt == null && e.Source == "interchange")
            .ToListAsync(ct);
        var edgeSet = liveEdges.Select(e => (e.SourceId, e.TargetId, e.RelationType)).ToHashSet();
        var edgesBySource = liveEdges.GroupBy(e => e.SourceId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var src in file.Entities)
        {
            var sourceId = fileEntityIds[Slugify(src.Id)];
            var wanted = new HashSet<(Guid, string)>();
            foreach (var rel in src.Relations ?? new())
            {
                if (string.IsNullOrWhiteSpace(rel.To) || string.IsNullOrWhiteSpace(rel.Kind)) continue;
                var targetSlug = Slugify(rel.To);
                if (!slugToId.TryGetValue(targetSlug, out var targetId))
                {
                    var stub = new Entity
                    {
                        UniverseId = universeId, EntityType = "stub", Name = Humanize(rel.To), Slug = targetSlug,
                        Status = "stub", CreatedAt = now, ModifiedAt = now,
                    };
                    db.Entities.Add(stub);
                    targetId = stub.Id;
                    slugToId[targetSlug] = targetId;
                    result.StubsCreated++;
                    await Tick();
                }

                var kind = rel.Kind.Trim();
                wanted.Add((targetId, kind));
                if (!edgeSet.Add((sourceId, targetId, kind))) continue;
                db.Edges.Add(new Edge
                {
                    UniverseId = universeId, SourceId = sourceId, TargetId = targetId, RelationType = kind,
                    Source = "interchange", Sentiment = "neutral", Weight = 1.0,
                });
                result.EdgesCreated++;
                await Tick();
            }

            if (mode == ImportModes.Replace && edgesBySource.TryGetValue(sourceId, out var have))
                foreach (var old in have.Where(e => e.InvalidatedAt == null && !wanted.Contains((e.TargetId, e.RelationType))))
                {
                    old.InvalidatedAt = now;
                    edgeSet.Remove((old.SourceId, old.TargetId, old.RelationType));
                    result.EdgesInvalidated++;
                    await Tick();
                }
        }
        await db.SaveChangesAsync(ct);

        // Session bookkeeping: a non-final batch only records what it saw.
        var session = new SessionState();
        if (options.Session != null)
        {
            var key = SessionSettingPrefix + options.Session;
            var stored = await db.Settings.IgnoreQueryFilters()
                .Where(s => s.Key == key && s.UniverseId == universeId).Select(s => s.Json).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(stored))
                try { session = JsonSerializer.Deserialize<SessionState>(stored, JsonOpts) ?? new(); } catch { session = new(); }
        }
        session.Keys.AddRange(fileKeys);
        session.Created += result.EntitiesCreated;
        session.Updated += result.EntitiesUpdated;
        session.Unchanged += result.EntitiesUnchanged;
        session.Edges += result.EdgesCreated;
        session.Stubs += result.StubsCreated;
        session.Batches++;

        if (!options.Final && options.Session != null)
        {
            await UpsertSettingAsync(db, universeId, SessionSettingPrefix + options.Session, JsonSerializer.Serialize(session, JsonOpts), ct);
            if (result.UniverseCreated) universeContext?.Refresh();
            return result;
        }

        // Final: prune (replace), manifest, one notice.
        var seen = session.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var manifestJson = await db.Settings.IgnoreQueryFilters()
            .Where(s => s.Key == ManifestSettingKey && s.UniverseId == universeId).Select(s => s.Json).FirstOrDefaultAsync(ct);
        HashSet<string>? manifest = null;
        if (!string.IsNullOrWhiteSpace(manifestJson))
            try { manifest = JsonSerializer.Deserialize<List<string>>(manifestJson, JsonOpts)?.ToHashSet(StringComparer.OrdinalIgnoreCase); }
            catch { /* unreadable manifest: treated as absent */ }

        if (mode == ImportModes.Replace)
        {
            HashSet<string> prior;
            if (manifest != null) { prior = manifest; result.PruneBasis = "manifest"; }
            else
            {
                // No manifest yet (universes imported before PRS-T916): every canon entity with a record that a
                // human did not author. Prose-authored entities carry Provenance = authored and are never retired.
                prior = all.Where(e => e.Status == "canon" && e.Provenance != ClaimProvenance.Authored && records.ContainsKey(e.Id))
                    .Select(e => ManifestKey(e.EntityType, e.Slug)).ToHashSet(StringComparer.OrdinalIgnoreCase);
                result.PruneBasis = "records";
            }

            var retireIds = new HashSet<Guid>();
            foreach (var e in all)
            {
                if (e.Status != "canon" || e.Provenance == ClaimProvenance.Authored) continue;
                var k = ManifestKey(e.EntityType, e.Slug);
                if (!prior.Contains(k) || seen.Contains(k)) continue;
                e.Status = "archived";
                e.ModifiedAt = now;
                retireIds.Add(e.Id);
                result.Retired.Add(e.Slug);
                result.EntitiesRetired++;
            }
            foreach (var edge in liveEdges.Where(ed => ed.InvalidatedAt == null && retireIds.Contains(ed.SourceId)))
            {
                edge.InvalidatedAt = now;
                result.EdgesInvalidated++;
            }
            manifest = seen;
        }
        else
        {
            manifest ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            manifest.UnionWith(seen);
        }
        await db.SaveChangesAsync(ct);
        await UpsertSettingAsync(db, universeId, ManifestSettingKey,
            JsonSerializer.Serialize(manifest.OrderBy(k => k, StringComparer.Ordinal).ToList(), JsonOpts), ct);
        if (options.Session != null)
        {
            var key = SessionSettingPrefix + options.Session;
            var row = await db.Settings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Key == key && s.UniverseId == universeId, ct);
            if (row != null) { db.Settings.Remove(row); await db.SaveChangesAsync(ct); }
        }

        // RFC 0007 §5 outbox, once per import (once per session for a batched push, PRS-T916). Best-effort: a
        // notification failure must never fail an otherwise-successful import.
        if (outbox != null && options.Notify)
        {
            try
            {
                var batches = session.Batches > 1 ? $" in {session.Batches} batches" : "";
                await outbox.EnqueueAsync(slug, "interchange-import",
                    $"Universe '{slug}' {mode} import complete{batches}: {session.Created} created, "
                    + $"{session.Updated} updated ({session.Unchanged} unchanged), {session.Edges} edges, {session.Stubs} stubs"
                    + (mode == ImportModes.Replace ? $", {result.EntitiesRetired} retired, {result.EdgesInvalidated} relations invalidated" : "")
                    + ". Pull the snapshot to sync.",
                    ct: ct);
            }
            catch { /* best-effort notification only */ }
        }

        // A universe created here is unknown to the process-lifetime universe catalog until it is
        // refreshed: /api/universes/{slug}/..., the graph and `--universe use` answered
        // unknown_universe until the Hub restarted (SqlSeedService refreshes for the same reason).
        if (result.UniverseCreated) universeContext?.Refresh();

        return result;
    }

    private static string ManifestKey(string entityType, string slug) => entityType + "/" + slug;

    private static async Task UpsertSettingAsync(ProseDbContext db, Guid universeId, string key, string json, CancellationToken ct)
    {
        var row = await db.Settings.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Key == key && s.UniverseId == universeId, ct);
        if (row == null) db.Settings.Add(new Setting { Key = key, UniverseId = universeId, Json = json, UpdatedAt = DateTime.UtcNow });
        else if (row.Json != json) { row.Json = json; row.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Export the universe back to interchange-file JSON. Prefers each entity's stored
    /// Record.Json verbatim (the round-trip source of truth per the RFC); falls back to
    /// reconstructing from Entity/Edge/Tag columns for a row that never got one (e.g. a stub
    /// promoted by direct DB means outside this service). Stub (never-promoted) entities are
    /// excluded — they are bookkeeping for dangling relations, not real file content.</summary>
    public async Task<string> ExportAsync(string universeSlug, CancellationToken ct = default)
    {
        var (_, json) = await ExportCoreAsync(universeSlug, ct);
        return json;
    }

    /// <summary>Shared by <see cref="ExportAsync"/> and <see cref="ExportToFileAsync"/> so the
    /// latter can log the entity count from the file it just built instead of re-deserializing
    /// the whole export JSON (each entity carries its full stored Record.Json verbatim, so that
    /// used to cost a full parse of the entire interchange file purely to print a count).</summary>
    private async Task<(InterchangeFile File, string Json)> ExportCoreAsync(string universeSlug, CancellationToken ct)
    {
        var slug = universeSlug.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug, ct)
            ?? throw new InvalidOperationException($"Unknown universe slug '{universeSlug}'.");

        var entities = await db.Entities.IgnoreQueryFilters()
            // Stubs are bookkeeping, archived rows were retired by a replace import, and texts are
            // Prose's own (RFC 0017): none of them is the game's file content.
            .Where(e => e.UniverseId == universe.Id && e.Status != "stub" && e.Status != "archived"
                        && e.EntityType != ProseTextService.EntityType)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        var records = await db.Records.IgnoreQueryFilters()
            .Where(r => entityIds.Contains(r.EntityId))
            .ToDictionaryAsync(r => r.EntityId, r => r.Json, ct);

        var edgesBySource = (await db.Edges.IgnoreQueryFilters()
                .Where(ed => ed.UniverseId == universe.Id && ed.InvalidatedAt == null)
                .ToListAsync(ct))
            .GroupBy(ed => ed.SourceId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var tagsByEntity = (await db.EntityTags.IgnoreQueryFilters()
                .Where(t => entityIds.Contains(t.EntityId))
                .Select(t => new { t.EntityId, Name = t.Tag!.Name })
                .ToListAsync(ct))
            .GroupBy(t => t.EntityId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        // Relation targets may be stubs or retired rows, which are not exported themselves.
        var targetSlugs = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universe.Id && e.EntityType != ProseTextService.EntityType)
            .Select(e => new { e.Id, e.Slug }).ToDictionaryAsync(e => e.Id, e => e.Slug, ct);

        // PRS-T923: which entities are the consumer's. The manifest lists every (type/slug) an interchange import
        // brought in; without one (universes imported before PRS-T916) an entity with a record is taken as imported.
        // Prose-authored entities (Provenance = authored, or made by Prose's own tools) export with origin "prose".
        var manifestJson = await db.Settings.IgnoreQueryFilters()
            .Where(s => s.Key == ManifestSettingKey && s.UniverseId == universe.Id).Select(s => s.Json).FirstOrDefaultAsync(ct);
        HashSet<string>? manifest = null;
        if (!string.IsNullOrWhiteSpace(manifestJson))
            try { manifest = JsonSerializer.Deserialize<List<string>>(manifestJson, JsonOpts)?.ToHashSet(StringComparer.OrdinalIgnoreCase); }
            catch { /* unreadable manifest: treated as absent */ }

        var outEntities = new List<InterchangeEntity>();
        foreach (var e in entities)
        {
            InterchangeEntity? fromRecord = null;
            if (records.TryGetValue(e.Id, out var recJson) && !string.IsNullOrWhiteSpace(recJson))
            {
                try { fromRecord = JsonSerializer.Deserialize<InterchangeEntity>(recJson, JsonOpts); }
                catch { /* fall through to column reconstruction below */ }
            }
            if (fromRecord != null)
                fromRecord.Relations = RelationsFromEdges(fromRecord.Relations ?? new(),
                    edgesBySource.TryGetValue(e.Id, out var own) ? own : new List<Edge>(), targetSlugs);
            var outEntity = fromRecord ?? ReconstructEntity(e, targetSlugs, edgesBySource, tagsByEntity);
            // A record a Prose tool wrote may carry no interchange id; the entity's slug is its id.
            if (string.IsNullOrWhiteSpace(outEntity.Id)) outEntity.Id = e.Slug;
            outEntity.Guid = e.Id.ToString("D");
            var imported = e.Provenance != ClaimProvenance.Authored
                && (manifest != null ? manifest.Contains(ManifestKey(e.EntityType, e.Slug)) : fromRecord != null);
            outEntity.Origin = imported ? InterchangeOrigins.Import : InterchangeOrigins.Prose;
            var authoredEdges = edgesBySource.TryGetValue(e.Id, out var mine)
                ? mine.Where(ed => ed.Source != "interchange" && targetSlugs.ContainsKey(ed.TargetId))
                    .Select(ed => (To: targetSlugs[ed.TargetId], Kind: ed.RelationType)).Distinct()
                    .Select(r => new InterchangeRelation { To = r.To, Kind = r.Kind }).ToList()
                : null;
            outEntity.ProseRelations = authoredEdges is { Count: > 0 } ? authoredEdges : null;
            outEntities.Add(outEntity);
        }

        var universeSourceJson = await db.Settings.IgnoreQueryFilters()
            .Where(s => s.Key == UniverseSourceSettingKey && s.UniverseId == universe.Id)
            .Select(s => s.Json).FirstOrDefaultAsync(ct);
        InterchangeUniverse? universeBlock = null;
        if (!string.IsNullOrWhiteSpace(universeSourceJson))
        {
            try { universeBlock = JsonSerializer.Deserialize<InterchangeUniverse>(universeSourceJson, JsonOpts); }
            catch { /* fall through to column reconstruction below */ }
        }
        universeBlock ??= new InterchangeUniverse
        {
            Id = universe.Slug,
            Name = universe.Name,
            Logline = universe.Description,
            Rules = ParseWorldFactsAsRules(universe.WorldFacts),
        };

        var sourceJson = await db.Settings.IgnoreQueryFilters()
            .Where(s => s.Key == SourceSettingKey && s.UniverseId == universe.Id)
            .Select(s => s.Json).FirstOrDefaultAsync(ct);
        JsonElement? sourceBlock = null;
        if (!string.IsNullOrWhiteSpace(sourceJson))
            try { using var doc = JsonDocument.Parse(sourceJson); sourceBlock = doc.RootElement.Clone(); }
            catch { /* unreadable provenance block: left out */ }

        var file = new InterchangeFile { Universe = universeBlock, Source = sourceBlock, Entities = outEntities };

        return (file, JsonSerializer.Serialize(file, JsonOpts));
    }

    /// <summary>Export <paramref name="universeSlug"/> and write it to <paramref name="path"/>,
    /// notifying the outbox only once the file is actually on disk. Enqueuing from inside
    /// <see cref="ExportAsync"/> itself (a prior version of this method did) fired the "fresh
    /// snapshot available" notification before the caller's own file write ran — a disk failure
    /// there (bad path, permissions, full disk) still left the consumer told a snapshot existed
    /// when it didn't. Every current caller (CLI --universe-export/-sync, the MCP export tool)
    /// should go through this method instead of ExportAsync + a manual File.WriteAllTextAsync.</summary>
    public async Task ExportToFileAsync(string universeSlug, string path, CancellationToken ct = default)
    {
        var (file, json) = await ExportCoreAsync(universeSlug, ct);
        await ProseArtifacts.WriteTextAsync(path, json, ProseArtifacts.Overwrite, ct);

        if (outbox != null)
        {
            try
            {
                var count = file.Entities.Count;
                var slug = universeSlug.Trim().ToLowerInvariant();
                await outbox.EnqueueAsync(slug, "interchange-export",
                    $"Universe '{slug}' exported: {count} entities. Fresh snapshot available.",
                    ct: ct);
            }
            catch { /* best-effort notification only */ }
        }
    }

    // ── Universe row ────────────────────────────────────────────────────────

    private static async Task<(Universe row, bool createdNow)> FindOrCreateUniverseAsync(
        ProseDbContext db, string slug, InterchangeUniverse u, CancellationToken ct)
    {
        var existing = await db.Universes.FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (existing != null) return (existing, false);

        var maxSort = await db.Universes.MaxAsync(x => (double?)x.SortKey, ct) ?? 0;
        var created = new Universe
        {
            Slug = slug,
            Name = string.IsNullOrWhiteSpace(u.Name) ? slug.ToUpperInvariant() : u.Name,
            Description = u.Logline,
            Theme = slug,
            UniversePrimer = BuildPrimer(u),
            WorldFacts = u.Rules.Count > 0 ? string.Join("\n", u.Rules.Select(r => $"- {r}")) : null,
            SortKey = maxSort + 100,
        };
        db.Universes.Add(created);
        await db.SaveChangesAsync(ct);
        return (created, true);
    }

    private static string BuildPrimer(InterchangeUniverse u)
    {
        var parts = new[] { u.Logline, u.Setting, u.Tagline }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(" ", parts);
    }

    private static async Task UpsertUniverseSourceAsync(ProseDbContext db, Guid universeId, InterchangeUniverse u, CancellationToken ct)
    {
        var row = await db.Settings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Key == UniverseSourceSettingKey && s.UniverseId == universeId, ct);
        var json = JsonSerializer.Serialize(u, JsonOpts);
        if (row == null)
            db.Settings.Add(new Setting { Key = UniverseSourceSettingKey, UniverseId = universeId, Json = json, UpdatedAt = DateTime.UtcNow });
        else
        {
            row.Json = json;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private static List<string> ParseWorldFactsAsRules(string? worldFacts)
    {
        if (string.IsNullOrWhiteSpace(worldFacts)) return new List<string>();
        return worldFacts.Split('\n')
            .Select(l => l.Trim().TrimStart('-').Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }

    // ── Entity type mapping ─────────────────────────────────────────────────

    /// <summary>
    /// Maps an interchange <c>type</c> string to the Prose <c>EntityType</c> discriminator.
    /// Two documented deviations from the RFC's suggested table (see
    /// docs/rfc/0007-universe-interchange.md "Deviations"):
    ///   - "creature" is NOT routed to the <see cref="Species"/> table — that table is a
    ///     5-row controlled vocabulary (human/ai/elf/synthetic/unknown) that <see cref="Character.Species"/>
    ///     references by name, not a per-instance table for storing dozens of individual
    ///     creatures. Creatures get a generic, RepositoryDefinition-registered EntityType.
    ///   - "artifact" is NOT split between Gear subtypes and generic Entity — none of the
    ///     interchange schema's artifacts cleanly map to Prose's Gear categories (Weapon/
    ///     Equipment/Cyberware/...), so all artifacts get a uniform generic EntityType.
    /// Every other interchange type (event/rule/organization/concept/anything else) already
    /// maps to a generic, RepositoryDefinition-registered EntityType per the RFC.
    /// </summary>
    private static string MapEntityType(string interchangeType)
    {
        var t = (interchangeType ?? "").Trim().ToLowerInvariant();
        return t switch
        {
            "character" => "character",
            "location" => "place",
            "faction" => "faction",
            "" => "concept",
            _ => t,
        };
    }

    private async Task EnsureRepositoryDefinitionAsync(ProseDbContext db, string entityType, CancellationToken ct)
    {
        if (BuiltInEntityTypes.Contains(entityType)) return;
        var exists = await db.RepositoryDefinitions.AnyAsync(r => r.Slug == entityType, ct);
        if (exists) return;
        db.RepositoryDefinitions.Add(new RepositoryDefinition
        {
            Slug = entityType,
            Name = Humanize(entityType) + "s",
            Category = "World",
            Icon = "bi-box",
            Description = $"Interchange-imported '{entityType}' entities (RFC 0007).",
            RoutePath = $"/repo/{entityType}",
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { /* concurrent creation of the same definition — harmless */ }
    }

    // ── Export reconstruction fallback ──────────────────────────────────────

    /// <summary>PRS-T916: an entity's exported relations come from the live imported Edges rows, not from the JSON
    /// record alone, so an import cut short during its edge pass (or an edge invalidated since) shows up in the
    /// round-trip diff instead of exporting identical to the file. The record supplies the order and the original
    /// spelling of each relation that still has its edge; an imported edge the record does not list is appended.</summary>
    private static List<InterchangeRelation> RelationsFromEdges(List<InterchangeRelation> recorded, List<Edge> edges,
        Dictionary<Guid, string> slugById)
    {
        var live = edges.Where(ed => ed.Source == "interchange" && slugById.ContainsKey(ed.TargetId))
            .Select(ed => (To: slugById[ed.TargetId], Kind: ed.RelationType)).ToList();
        var remaining = live.ToHashSet();
        var output = new List<InterchangeRelation>();
        foreach (var r in recorded)
        {
            var k = (Slugify(r.To), (r.Kind ?? "").Trim());
            if (!live.Contains(k)) continue;
            output.Add(r);
            remaining.Remove(k);
        }
        foreach (var (to, kind) in live.Where(remaining.Contains).Distinct())
            output.Add(new InterchangeRelation { To = to, Kind = kind });
        return output;
    }

    private static InterchangeEntity ReconstructEntity(
        Entity e, Dictionary<Guid, string> slugById, Dictionary<Guid, List<Edge>> edgesBySource, Dictionary<Guid, List<string>> tagsByEntity)
    {
        var relations = edgesBySource.TryGetValue(e.Id, out var edges)
            ? edges.Where(ed => slugById.ContainsKey(ed.TargetId))
                .Select(ed => new InterchangeRelation { To = slugById[ed.TargetId], Kind = ed.RelationType })
                .ToList()
            : new List<InterchangeRelation>();

        return new InterchangeEntity
        {
            Id = e.Slug,
            Type = e.EntityType,
            Name = e.Name,
            Summary = e.Description ?? "",
            Relations = relations,
            Tags = tagsByEntity.TryGetValue(e.Id, out var tags) ? tags : new List<string>(),
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string Slugify(string s) => UniverseGraphService.Slugify(s ?? "");

    private static string Humanize(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return slug;
        var words = slug.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }
}
