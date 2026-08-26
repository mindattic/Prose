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
}

public class InterchangeFile
{
    public InterchangeUniverse Universe { get; set; } = new();
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
/// state. See SS-LAW-15 / RFC 0006 (universe segregation is structural, not caller-discipline).
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

    public UniverseInterchangeService(IDbContextFactory<ProseDbContext> dbFactory, OutboxService? outbox = null)
    {
        this.dbFactory = dbFactory;
        this.outbox = outbox;
    }

    public class ImportResult
    {
        public string UniverseSlug { get; set; } = "";
        public bool UniverseCreated { get; set; }
        public int EntitiesCreated { get; set; }
        public int EntitiesUpdated { get; set; }
        public int StubsCreated { get; set; }
        public int StubsPromoted { get; set; }
        public int EdgesCreated { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool Success => Errors.Count == 0;
    }

    /// <summary>
    /// Idempotent upsert-by-(UniverseId, Slug) import. Re-running with the same file is a
    /// no-op diff (only ModifiedAt/UpdatedAt timestamps move).
    /// </summary>
    /// <param name="universeSlugOverride">Explicit target universe slug (from <c>--universe</c>).
    /// Defaults to the file's own <c>universe.id</c> (already lowercase per the schema's
    /// <c>^[a-z0-9-]+$</c> pattern) — NOT uppercased, matching every existing universe row's
    /// lowercase Slug convention (glmz/scry/gospel/eve/...); see the RFC deviations note.</param>
    public async Task<ImportResult> ImportAsync(string json, string? universeSlugOverride = null, CancellationToken ct = default)
    {
        var result = new ImportResult();

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

        // Pass 1: upsert every entity's Entity/Record/Tag state, building a slug→id map for
        // pass 2. Must complete in full before edges are wired — a relation can point at an
        // entity defined later in the file.
        var slugToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        // Seed the map with every entity already in this universe (so a stub created by an
        // EARLIER import run resolves correctly even if this file no longer mentions it).
        foreach (var existing in await db.Entities.IgnoreQueryFilters()
                     .Where(e => e.UniverseId == universeId).Select(e => new { e.Id, e.Slug }).ToListAsync(ct))
            slugToId[existing.Slug] = existing.Id;

        foreach (var src in file.Entities)
        {
            var entityType = MapEntityType(src.Type);
            await EnsureRepositoryDefinitionAsync(db, entityType, ct);

            var rawJson = JsonSerializer.Serialize(src, JsonOpts);
            var (id, created, wasStub) = await UpsertEntityAsync(db, universeId, entityType, src, rawJson, ct);
            slugToId[Slugify(src.Id)] = id;

            if (created) result.EntitiesCreated++;
            else result.EntitiesUpdated++;
            if (wasStub) result.StubsPromoted++;
        }

        // Pass 2: relations → edges (creating stub entities for dangling targets).
        foreach (var src in file.Entities)
        {
            if (!slugToId.TryGetValue(Slugify(src.Id), out var sourceId)) continue;
            foreach (var rel in src.Relations)
            {
                if (string.IsNullOrWhiteSpace(rel.To) || string.IsNullOrWhiteSpace(rel.Kind)) continue;
                var targetSlug = Slugify(rel.To);
                if (!slugToId.TryGetValue(targetSlug, out var targetId))
                {
                    targetId = await EnsureStubEntityAsync(db, universeId, targetSlug, rel.To, ct);
                    slugToId[targetSlug] = targetId;
                    result.StubsCreated++;
                }

                var edgeCreated = await EnsureEdgeAsync(db, universeId, sourceId, targetId, rel.Kind.Trim(), ct);
                if (edgeCreated) result.EdgesCreated++;
            }
        }

        // RFC 0007 §5 outbox — tell the consumer app's Claude Code session (drained via its
        // UserPromptSubmit hook) that fresh universe data landed, without either side having to
        // remember to say so manually. Best-effort: a notification failure must never fail an
        // otherwise-successful import.
        if (result.Success && outbox != null)
        {
            try
            {
                await outbox.EnqueueAsync(slug, "interchange-import",
                    $"Universe '{slug}' import complete: {result.EntitiesCreated} created, "
                    + $"{result.EntitiesUpdated} updated, {result.EdgesCreated} edges, {result.StubsCreated} stubs "
                    + $"({result.StubsPromoted} promoted). Pull the snapshot to sync.",
                    ct: ct);
            }
            catch { /* best-effort notification only */ }
        }

        return result;
    }

    /// <summary>Export the universe back to interchange-file JSON. Prefers each entity's stored
    /// Record.Json verbatim (the round-trip source of truth per the RFC); falls back to
    /// reconstructing from Entity/Edge/Tag columns for a row that never got one (e.g. a stub
    /// promoted by direct DB means outside this service). Stub (never-promoted) entities are
    /// excluded — they are bookkeeping for dangling relations, not real file content.</summary>
    public async Task<string> ExportAsync(string universeSlug, CancellationToken ct = default)
    {
        var slug = universeSlug.Trim().ToLowerInvariant();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug, ct)
            ?? throw new InvalidOperationException($"Unknown universe slug '{universeSlug}'.");

        var entities = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universe.Id && e.Status != "stub")
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        var records = await db.Records.IgnoreQueryFilters()
            .Where(r => entityIds.Contains(r.EntityId))
            .ToDictionaryAsync(r => r.EntityId, r => r.Json, ct);

        var slugById = entities.ToDictionary(e => e.Id, e => e.Slug);
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

        var outEntities = new List<InterchangeEntity>();
        foreach (var e in entities)
        {
            InterchangeEntity? fromRecord = null;
            if (records.TryGetValue(e.Id, out var recJson) && !string.IsNullOrWhiteSpace(recJson))
            {
                try { fromRecord = JsonSerializer.Deserialize<InterchangeEntity>(recJson, JsonOpts); }
                catch { /* fall through to column reconstruction below */ }
            }
            outEntities.Add(fromRecord ?? ReconstructEntity(e, slugById, edgesBySource, tagsByEntity));
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

        var file = new InterchangeFile { Universe = universeBlock, Entities = outEntities };

        if (outbox != null)
        {
            try
            {
                await outbox.EnqueueAsync(slug, "interchange-export",
                    $"Universe '{slug}' exported: {outEntities.Count} entities. Fresh snapshot available.",
                    ct: ct);
            }
            catch { /* best-effort notification only */ }
        }

        return JsonSerializer.Serialize(file, JsonOpts);
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

    // ── Entity upsert ───────────────────────────────────────────────────────

    private static async Task<(Guid id, bool created, bool wasStub)> UpsertEntityAsync(
        ProseDbContext db, Guid universeId, string entityType, InterchangeEntity src, string rawJson, CancellationToken ct)
    {
        var slug = Slugify(src.Id);
        var existing = await db.Entities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.UniverseId == universeId && e.Slug == slug, ct);

        var created = existing == null;
        var wasStub = existing?.Status == "stub";
        var entity = existing ?? new Entity { UniverseId = universeId, Slug = slug, CreatedAt = DateTime.UtcNow };

        // Promotes a prior stub's placeholder type/status to the real content that just arrived.
        entity.EntityType = entityType;
        entity.Name = src.Name;
        entity.Description = string.IsNullOrWhiteSpace(src.Summary) ? entity.Description : src.Summary;
        entity.Status = "canon";
        entity.ModifiedAt = DateTime.UtcNow;

        if (created) db.Entities.Add(entity);
        await db.SaveChangesAsync(ct);

        var record = await db.Records.FirstOrDefaultAsync(r => r.EntityId == entity.Id, ct);
        if (record == null) { record = new Record { EntityId = entity.Id }; db.Records.Add(record); }
        record.Json = rawJson;
        record.UpdatedAt = DateTime.UtcNow;

        await SyncTagsAsync(db, entity.Id, src.Tags, ct);
        await db.SaveChangesAsync(ct);

        return (entity.Id, created, wasStub);
    }

    private static async Task<Guid> EnsureStubEntityAsync(ProseDbContext db, Guid universeId, string slug, string rawId, CancellationToken ct)
    {
        var existing = await db.Entities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.UniverseId == universeId && e.Slug == slug, ct);
        if (existing != null) return existing.Id;

        var stub = new Entity
        {
            UniverseId = universeId,
            EntityType = "stub",
            Name = Humanize(rawId),
            Slug = slug,
            Status = "stub",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        };
        db.Entities.Add(stub);
        await db.SaveChangesAsync(ct);
        return stub.Id;
    }

    private static async Task<bool> EnsureEdgeAsync(ProseDbContext db, Guid universeId, Guid sourceId, Guid targetId, string relationType, CancellationToken ct)
    {
        var exists = await db.Edges.IgnoreQueryFilters().AnyAsync(e =>
            e.UniverseId == universeId && e.SourceId == sourceId && e.TargetId == targetId
            && e.RelationType == relationType && e.InvalidatedAt == null, ct);
        if (exists) return false;

        db.Edges.Add(new Edge
        {
            UniverseId = universeId,
            SourceId = sourceId,
            TargetId = targetId,
            RelationType = relationType,
            Source = "interchange",
            Sentiment = "neutral",
            Weight = 1.0,
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task SyncTagsAsync(ProseDbContext db, Guid entityId, IReadOnlyList<string>? tags, CancellationToken ct)
    {
        if (tags == null || tags.Count == 0) return;
        var existing = (await db.EntityTags.Where(t => t.EntityId == entityId)
                .Select(t => t.Tag!.Name).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var wanted = tags.Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(t => !existing.Contains(t))
            .ToList();
        if (wanted.Count == 0) return;

        var byName = (await db.Tags.Where(t => wanted.Contains(t.Name)).ToListAsync(ct))
            .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);

        foreach (var name in wanted)
        {
            if (!byName.TryGetValue(name, out var tag))
            {
                tag = new Tag { Name = name };
                db.Tags.Add(tag);
                byName[name] = tag;
            }
            db.EntityTags.Add(new EntityTag { EntityId = entityId, Tag = tag });
        }
    }

    // ── Export reconstruction fallback ──────────────────────────────────────

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
