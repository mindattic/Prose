using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.WriterUi.Services;

/// <summary>
/// Everything the entity wiki needs from the engine. Runs in-process inside Prose.Hub, so it calls
/// Core services directly — "only Hub reaches the DB" is satisfied by being the Hub.
///
/// <para><b>No sync, no export.</b> Pages are rendered from SQL per request. The corpus is ~13,700
/// entities in GLMZ alone across 32 types, plus every other universe; materialising that to files
/// and refreshing it on a timer would be more code than this, could drift from canon between
/// refreshes, and canon is the database, not a folder.</para>
///
/// <para><b>No per-type code.</b> The record is read generically through
/// <see cref="EntityHistoryService.GetCurrentSnapshotAsync"/>, which flattens the
/// <c>Entities</c> row, the typed subtype table and the <c>Records.Json</c> blob into one
/// field map. That is what lets all 32 types — including the handful with no C# repository at
/// all — render through a single view instead of 32 bespoke ones.</para>
/// </summary>
public sealed class WikiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    EntityHistoryService history,
    EntityRelationshipService relationships,
    EntityMentionService mentions,
    EntityRamificationService ramification,
    RepositoryDefinitionService repoDefs,
    IUniverseContext universe)
{
    /// <summary>One repository (entity type) and how many of it this universe holds.</summary>
    public sealed record RepoSummary(string Type, string Name, string Category, string Icon, int Count);

    /// <summary>One row in a repository index.</summary>
    public sealed record EntityListItem(Guid Id, string Name, string Slug, string Status, string? Description);

    /// <summary>A neighbour in the relationship graph, resolved far enough to link to.</summary>
    public sealed record RelatedEntity(Guid Id, string Name, string Type, string Slug,
                                       string? Relation, string Sentiment);

    /// <summary>The identity block at the top of an entity page.</summary>
    public sealed record EntityHeader(
        Guid Id, string Type, string Name, string Slug, string Status,
        string? Description, string? GrammarNote, DateTime CreatedAt, DateTime ModifiedAt,
        Guid UniverseId, string UniverseSlug);

    /// <summary>Everything one wiki page shows.
    /// <para><paramref name="RecordFound"/> separates "this entity has no stored record" from
    /// "this entity did not exist at the instant you rewound to". Both render as an empty field
    /// map, and conflating them tells the author their canon is missing when in fact they are
    /// looking at a moment before it was written.</para></summary>
    public sealed record EntityPage(
        EntityHeader Header,
        IReadOnlyDictionary<string, string?> Fields,
        IReadOnlyList<RelatedEntity> Related,
        IReadOnlyList<EntityBeatMention> Appearances,
        IReadOnlyList<EntityVersion> Versions,
        int MentionCount,
        bool RecordFound);

    // ── Index ───────────────────────────────────────────────────────────────

    /// <summary>Every repository type present in the current universe, with counts. Built-in types
    /// come from the <c>Entities</c> spine itself; custom ones add their display name and icon.</summary>
    public async Task<List<RepoSummary>> ListRepositoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // The EF query filter on Entity already scopes this to the ambient universe.
        var counts = await db.Entities.AsNoTracking()
            .GroupBy(e => e.EntityType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var defs = repoDefs.List().ToDictionary(d => d.Slug, StringComparer.OrdinalIgnoreCase);

        return counts
            .Select(c =>
            {
                defs.TryGetValue(c.Type, out var def);
                return new RepoSummary(
                    Type: c.Type,
                    Name: def?.Name ?? Humanise(c.Type),
                    Category: def?.Category ?? "World",
                    Icon: def?.Icon ?? "bi-box",
                    Count: c.Count);
            })
            .OrderBy(r => r.Category, StringComparer.Ordinal)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>One page of a repository index, optionally filtered.</summary>
    public async Task<(List<EntityListItem> Rows, int Total)> ListEntitiesAsync(
        string type, string? search = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.Entities.AsNoTracking().Where(e => e.EntityType == type);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search)
                                  || (e.Description != null && e.Description.Contains(search)));

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderBy(e => e.Name)
            .Skip((Math.Max(1, page) - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EntityListItem(e.Id, e.Name, e.Slug, e.Status, e.Description))
            .ToListAsync(ct);

        return (rows, total);
    }

    // ── One entity ──────────────────────────────────────────────────────────

    /// <summary>Resolves <c>{type}/{slug}</c> to an entity id, ignoring universe scope so a link
    /// from a book in one universe still opens an entity in another.</summary>
    public async Task<Guid?> ResolveAsync(string type, string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var id = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.EntityType == type && e.Slug == slug)
            .Select(e => e.Id)
            .FirstOrDefaultAsync(ct);
        return id == Guid.Empty ? null : id;
    }

    /// <summary>Where a chip's guid points. The Writer has the guid but not the route.</summary>
    public async Task<(string Type, string Slug)?> GetRouteAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Id == entityId)
            .Select(e => new { e.EntityType, e.Slug })
            .FirstOrDefaultAsync(ct);
        return row == null ? null : (row.EntityType, row.Slug);
    }

    /// <summary>
    /// Assembles one wiki page. <paramref name="asOf"/> renders the record as the database held it
    /// at that instant instead of now; relationships, appearances and the version list always
    /// reflect the present, because those are how you navigate, not what you are rewinding.
    /// </summary>
    public async Task<EntityPage?> GetPageAsync(
        Guid entityId, DateTime? asOf = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var e = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(x => x.Id == entityId)
            .Select(x => new
            {
                x.Id, x.EntityType, x.Name, x.Slug, x.Status, x.Description,
                x.GrammarNote, x.CreatedAt, x.ModifiedAt, x.UniverseId,
            })
            .FirstOrDefaultAsync(ct);

        if (e == null) return null;

        // Pin the ambient universe to this entity's own before anything reads a universe-scoped
        // table — without it a cross-universe link renders an empty page rather than an entity.
        if (e.UniverseId != Guid.Empty) universe.SetFlowUniverse(e.UniverseId);

        var universeSlug = await db.Universes.AsNoTracking().IgnoreQueryFilters()
            .Where(u => u.Id == e.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct) ?? "";

        var snapshot = asOf == null
            ? await history.GetCurrentSnapshotAsync(entityId, ct)
            : await history.GetSnapshotAsync(entityId, asOf.Value, ct);

        var tree = await relationships.GetTreeAsync(entityId, maxDepth: 1, ct: ct);
        var related = await ResolveRelatedAsync(db, tree, ct);

        var appearances = await mentions.GetBeatsForEntityAsync(entityId, limit: 50, ct);
        var mentionCount = await ramification.CountMentioningBeatsAsync(entityId, ct);
        var versions = await history.GetVersionsAsync(entityId, ct);

        var header = new EntityHeader(
            e.Id, e.EntityType, e.Name, e.Slug, e.Status, e.Description, e.GrammarNote,
            e.CreatedAt, e.ModifiedAt, e.UniverseId, universeSlug);

        return new EntityPage(
            header,
            snapshot?.Fields ?? new Dictionary<string, string?>(),
            related,
            appearances,
            versions,
            mentionCount,
            RecordFound: snapshot is not null);
    }

    /// <summary>The relationship tree carries ids and names but no slugs, and a wiki link needs a
    /// slug — so resolve the whole first ring in one query rather than one per neighbour.</summary>
    private static async Task<List<RelatedEntity>> ResolveRelatedAsync(
        ProseDbContext db, EntityRelTree tree, CancellationToken ct)
    {
        if (tree.Children.Count == 0) return [];

        var ids = tree.Children.Select(c => c.EntityId).Distinct().ToList();
        var slugs = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.Slug })
            .ToDictionaryAsync(x => x.Id, x => x.Slug, ct);

        return tree.Children
            .Select(c => new RelatedEntity(
                c.EntityId, c.Name, c.EntityType,
                slugs.GetValueOrDefault(c.EntityId, ""),
                c.RelationType, c.Sentiment))
            .Where(r => r.Slug != "")
            .OrderBy(r => r.Relation ?? "", StringComparer.Ordinal)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .ToList();
    }

    // ── History ─────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<EntityVersion>> GetVersionsAsync(Guid entityId, CancellationToken ct = default)
        => history.GetVersionsAsync(entityId, ct);

    public Task<IReadOnlyList<FieldChange>> DiffAsync(
        Guid entityId, DateTime before, DateTime after, CancellationToken ct = default)
        => history.DiffAsync(entityId, before, after, ct);

    /// <summary>Diffs a past version against the record as it stands now.</summary>
    public Task<IReadOnlyList<FieldChange>> DiffAgainstCurrentAsync(
        Guid entityId, DateTime before, CancellationToken ct = default)
        => history.DiffAgainstCurrentAsync(entityId, before, ct);

    // ── Editing ─────────────────────────────────────────────────────────────

    /// <summary>What a save did, so the page can tell the author rather than just going quiet.</summary>
    public sealed record SaveOutcome(bool Changed, int BeatsFlagged, DateTime ModifiedAt, string? Conflict);

    /// <summary>
    /// Saves the editable fields of the <c>Entities</c> spine. System-versioning turns this into a
    /// new version automatically — that is the whole reason the history tab needs no bookkeeping.
    ///
    /// <para><b>Scope.</b> Description / Status / GrammarNote only. Notably <b>not</b> Name: a
    /// rename cascades through entity tags in prose, aliases and the ledger, and belongs to
    /// <see cref="EntityRenameService"/> behind a preview, not a text box. Bridge collections are
    /// v2. <c>EfRepository.Save</c> never writes <c>Entity.Description</c>, so editing it here
    /// cannot be silently clobbered by a later repository save.</para>
    ///
    /// <para><b>Cost.</b> Free. The contradiction pass this triggers is the SQL one — every beat
    /// mentioning the entity is flagged <c>EntityStale</c> for review. The LLM adjudication is a
    /// separate, deliberate call (<see cref="ScanForContradictionsAsync"/>).</para>
    /// </summary>
    /// <param name="expectedModifiedAt">Optimistic concurrency: the ModifiedAt the editor loaded.
    /// A mismatch means someone else saved in between and the edit is refused rather than
    /// overwriting them.</param>
    public async Task<SaveOutcome> SaveEntityAsync(
        Guid entityId,
        string? description,
        string? status,
        string? grammarNote,
        DateTime? expectedModifiedAt = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entity = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == entityId, ct);
        if (entity == null)
            return new SaveOutcome(false, 0, default, "This entity no longer exists.");

        if (expectedModifiedAt != null
            && Math.Abs((entity.ModifiedAt - expectedModifiedAt.Value).TotalSeconds) > 1)
            return new SaveOutcome(false, 0, entity.ModifiedAt,
                $"Someone else saved this entity at {entity.ModifiedAt:u}. Reload before editing.");

        if (entity.UniverseId != Guid.Empty) universe.SetFlowUniverse(entity.UniverseId);

        var changed = false;
        if (description != null && entity.Description != description) { entity.Description = description; changed = true; }
        if (status != null && entity.Status != status) { entity.Status = status; changed = true; }
        if (grammarNote != null && entity.GrammarNote != grammarNote) { entity.GrammarNote = grammarNote; changed = true; }

        if (!changed) return new SaveOutcome(false, 0, entity.ModifiedAt, null);

        entity.ModifiedAt = DateTime.UtcNow;

        // Blob-backed types keep their own copy of the description, and a generator reading the
        // blob would otherwise keep serving the old text forever. Keep the two in step.
        if (description != null)
            await SyncBlobDescriptionAsync(db, entityId, description, ct);

        await db.SaveChangesAsync(ct);

        // Free: marks every beat mentioning this entity for review. No LLM call.
        var flagged = await ramification.ProcessEntityUpdateAsync(entityId, entity.Name, ct);

        return new SaveOutcome(true, flagged, entity.ModifiedAt, null);
    }

    private static async Task SyncBlobDescriptionAsync(
        ProseDbContext db, Guid entityId, string description, CancellationToken ct)
    {
        var record = await db.Records.FirstOrDefaultAsync(r => r.EntityId == entityId, ct);
        if (record == null || string.IsNullOrWhiteSpace(record.Json)) return;

        try
        {
            if (JsonNode.Parse(record.Json) is not JsonObject obj) return;

            // Match the existing key's casing rather than inventing a second one.
            var key = obj.Select(p => p.Key)
                .FirstOrDefault(k => string.Equals(k, "description", StringComparison.OrdinalIgnoreCase));
            if (key == null) return;

            obj[key] = description;
            record.Json = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            record.UpdatedAt = DateTime.UtcNow;
        }
        catch (JsonException)
        {
            // An unparseable blob is a pre-existing problem; don't let it block a spine edit.
        }
    }

    // ── The deliberate, paid contradiction check ────────────────────────────

    /// <summary>How many beats a contradiction scan would examine. Free — show this before
    /// offering the button.</summary>
    public Task<int> CountMentioningBeatsAsync(Guid entityId, CancellationToken ct = default)
        => ramification.CountMentioningBeatsAsync(entityId, ct);

    /// <summary>Runs the LLM contradiction scan. Costs money, so it is never called from a save —
    /// only from an explicit author action that has already seen the beat count.</summary>
    public Task<ContradictionScanResult> ScanForContradictionsAsync(
        Guid entityId, int maxBeats, IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
        => ramification.ScanForContradictionsAsync(entityId, maxBeats, progress, ct);

    private static string Humanise(string slug)
        => string.Join(' ', slug.Split('_', '-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));
}
