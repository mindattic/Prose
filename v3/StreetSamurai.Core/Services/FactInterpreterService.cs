using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Generalized "prose → relational graph" compiler. Takes a chunk of natural-
/// language description and emits a structured set of entities + typed
/// relationships, resolved against canon where possible and (optionally) stubbed
/// where they aren't.
///
/// Pipeline:
///   1. LLM extraction → strict-JSON list of <c>entities</c> + <c>relations</c>.
///   2. Entity resolution: for each name, look up Entities.Slug / Entities.Name.
///      If <see cref="InterpretationOptions.AutoCreate"/> is true and not
///      found, create a stub Entity (+ minimal subtype row) for the named type.
///   3. Edge writing: for each relation, locate the from + to entity ids and
///      insert one Edge row with the typed RelationType (idempotent: skips when
///      a matching edge already exists).
///   4. Optional ledger emission: when <see cref="InterpretationOptions.RecordLedger"/>
///      is true, write one EntityStateEvent per relation as a "set" verb on
///      <c>relation:{type}</c> aspect — useful for "the moment this fact
///      entered canon" timeline rendering.
///
/// Different from <see cref="ContinuityExtractionService"/> which extracts
/// atomic (entity, predicate, object) <em>claims</em> for human triage in
/// <c>ContinuityClaims</c>. This service is the aggressive sibling — it writes
/// directly into the canon graph. Pair with <c>--dry-run</c> to preview
/// without committing.
/// </summary>
public class FactInterpreterService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly WorldClockService clock;
    private readonly EmbeddingService embeddings;
    private readonly ILogger<FactInterpreterService> log;

    /// <summary>
    /// Minimum cosine similarity for the embedding nearest-match fallback.
    /// Below this we treat the name as truly unresolved rather than guess —
    /// the LLM's extracted name may be a one-off mention with no canon analog.
    /// 0.55 was tuned empirically: same-cluster hits routinely score 0.45–0.7,
    /// so 0.55 is the inflection where confident matches start dominating
    /// noise. Lower it if too many real matches drop; raise it if the
    /// fallback starts wiring weak associations.
    /// </summary>
    private const double EmbeddingFallbackMinSimilarity = 0.55;

    public FactInterpreterService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILlmService llm,
        WorldClockService clock,
        EmbeddingService embeddings,
        ILogger<FactInterpreterService> log)
    {
        this.dbFactory  = dbFactory;
        this.llm        = llm;
        this.clock      = clock;
        this.embeddings = embeddings;
        this.log        = log;
    }

    // ── public surface ────────────────────────────────────────────────────────

    public sealed record InterpretationOptions(
        bool DryRun        = true,                    // when true, NO writes — preview only
        bool AutoCreate    = false,                   // create stub entities for unresolved names
        bool RecordLedger  = true,                    // emit EntityStateEvent rows for each relation
        string SourceTag   = "interpret:adhoc",       // Edges.Source / EntityStateEvents.Source
        DateTime? AtStoryTime = null,                 // story-time for ledger events (defaults to WorldClock now)
        int MaxEntities    = 60,                      // cap on extracted entity count
        int MaxRelations   = 100);                    // cap on extracted relation count

    public sealed record ExtractedEntity(
        string Name,
        string EntityType,
        string? Description,
        Guid? ResolvedId,
        bool WasCreated);

    public sealed record ExtractedRelation(
        string FromName,
        string RelationType,
        string ToName,
        string? Description,
        string? Sentiment,
        bool Wired,                  // true if an Edge row was written or already existed
        string? Skipped);            // non-null reason when both endpoints couldn't be resolved

    public sealed record InterpretationResult(
        IReadOnlyList<ExtractedEntity> Entities,
        IReadOnlyList<ExtractedRelation> Relations,
        int EntitiesCreated,
        int EdgesWritten,
        int LedgerEvents,
        IReadOnlyList<string> Warnings,
        string RawLlmOutput);

    /// <summary>Single end-to-end call: extract → resolve → wire.</summary>
    public async Task<InterpretationResult> InterpretAsync(
        string description,
        InterpretationOptions opts,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description must not be empty.", nameof(description));

        progress?.Report("  [1/4] LLM extraction…");
        var raw = await CallExtractionLlmAsync(description, ct);
        var (entities, relations) = ParseLlmOutput(raw, opts);

        progress?.Report($"  [2/4] resolving {entities.Count} entities + {relations.Count} relations against canon");
        var warnings = new List<string>();
        var resolvedEntities = new List<ExtractedEntity>();
        var resolvedIdByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        int created = 0;
        foreach (var e in entities)
        {
            var hit = await ResolveAsync(db, e.Name, e.EntityType, e.Description, ct);
            if (hit != null)
            {
                resolvedEntities.Add(new(e.Name, e.EntityType, e.Description, hit, WasCreated: false));
                resolvedIdByName[e.Name] = hit.Value;
                continue;
            }
            if (!opts.AutoCreate || opts.DryRun)
            {
                resolvedEntities.Add(new(e.Name, e.EntityType, e.Description, null, WasCreated: false));
                continue;
            }
            // Stub-create
            var newId = await CreateStubAsync(db, e.Name, e.EntityType, e.Description, opts.SourceTag, ct);
            resolvedEntities.Add(new(e.Name, e.EntityType, e.Description, newId, WasCreated: true));
            resolvedIdByName[e.Name] = newId;
            created++;
        }

        progress?.Report($"  [3/4] wiring edges (dryRun={opts.DryRun})");
        int edgesWritten = 0;
        int ledgerEvents = 0;
        var resolvedRelations = new List<ExtractedRelation>();
        var atStory = opts.AtStoryTime ?? clock.GetNow();

        foreach (var r in relations)
        {
            if (!resolvedIdByName.TryGetValue(r.FromName, out var fromId))
            {
                resolvedRelations.Add(new(r.FromName, r.RelationType, r.ToName, r.Description, r.Sentiment, false, $"unresolved: {r.FromName}"));
                continue;
            }
            if (!resolvedIdByName.TryGetValue(r.ToName, out var toId))
            {
                resolvedRelations.Add(new(r.FromName, r.RelationType, r.ToName, r.Description, r.Sentiment, false, $"unresolved: {r.ToName}"));
                continue;
            }
            if (opts.DryRun)
            {
                resolvedRelations.Add(new(r.FromName, r.RelationType, r.ToName, r.Description, r.Sentiment, true, "dry-run"));
                continue;
            }
            // Idempotent edge insert
            var existing = await db.Edges.AnyAsync(x =>
                x.SourceId == fromId && x.TargetId == toId && x.RelationType == r.RelationType, ct);
            if (!existing)
            {
                db.Edges.Add(new Edge
                {
                    SourceId       = fromId,
                    TargetId       = toId,
                    RelationType   = r.RelationType,
                    Description    = r.Description ?? "",
                    Weight         = 1.0,
                    Sentiment      = r.Sentiment ?? "neutral",
                    StoryValidFrom = atStory,
                    Source         = opts.SourceTag,
                });
                edgesWritten++;
            }
            if (opts.RecordLedger)
            {
                db.EntityStateEvents.Add(new EntityStateEvent
                {
                    EntityId    = fromId,
                    AspectKey   = $"relation:{r.RelationType}",
                    Verb        = "set",
                    NewValue    = r.ToName,
                    AtStoryTime = atStory,
                    Source      = opts.SourceTag,
                    Confidence  = 0.7,
                    Snippet     = r.Description,
                });
                ledgerEvents++;
            }
            resolvedRelations.Add(new(r.FromName, r.RelationType, r.ToName, r.Description, r.Sentiment, true, null));
        }

        if (!opts.DryRun) await db.SaveChangesAsync(ct);

        progress?.Report($"  [4/4] done — entities created={created}, edges written={edgesWritten}, ledger events={ledgerEvents}");
        log.LogInformation("FactInterpreter: {Ent} entities ({Created} new), {Rel} relations, {Edges} edges, {Led} ledger events. dryRun={Dry}",
            resolvedEntities.Count, created, resolvedRelations.Count, edgesWritten, ledgerEvents, opts.DryRun);

        return new InterpretationResult(resolvedEntities, resolvedRelations, created, edgesWritten, ledgerEvents, warnings, raw);
    }

    // ── LLM extraction prompt + parser ────────────────────────────────────────

    private async Task<string> CallExtractionLlmAsync(string description, CancellationToken ct)
    {
        var system =
            "You are a worldbuilding analyst. Read the description and emit a structured JSON object that " +
            "describes every named entity in it and every typed relationship between them. " +
            "Output ONLY a single JSON object on the FINAL line of your response, with this shape:\n" +
            "{\n" +
            "  \"entities\": [\n" +
            "    {\"name\":\"<exact name as it appears>\", \"type\":\"<one of: character, place, faction, corponation, " +
            "subsidiary, synthetic, automaton, weapon, equipment, cyberware, apparel, ammunition, pharmaceutical, genemod, " +
            "material, transportation, consumer_good, archetype, document, vocabulary, contract, news, quote, lab_specimen, " +
            "psionic, technology, motif>\", \"description\":\"<one sentence summary>\"}\n" +
            "  ],\n" +
            "  \"relations\": [\n" +
            "    {\"from\":\"<entity name>\", \"rel\":\"<one of: located_at, lives_at, deployed_at, member_of, employed_by, " +
            "owns, wields, wears, partner_of, parent_of, child_of, sibling_of, friend_of, ally_of, opposes, fears, kills, " +
            "made_by, located_in, contains, instance_of, succeeded_by, preceded_by, home_of, neighbor_of, leads, " +
            "reports_to, knows>\", \"to\":\"<entity name>\", \"description\":\"<short prose for the edge>\", " +
            "\"sentiment\":\"positive|neutral|negative\"}\n" +
            "  ]\n" +
            "}\n" +
            "Rules:\n" +
            " - Only emit relations whose endpoints both appear in the entities array.\n" +
            " - Use the exact name string in both arrays so resolution can match.\n" +
            " - Don't invent entities or relations not implied by the prose.\n" +
            " - When the prose describes a community / faction AND the geographic place that hosts them, emit BOTH " +
            "as separate entities (faction + place) and link them with a 'located_at' relation.\n" +
            " - Use 'lives_at' for character → place, 'member_of' for character → faction, 'employed_by' for character → corp.\n";

        var user = "DESCRIPTION:\n" + description;

        try
        {
            return await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 4000, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "FactInterpreter LLM extraction failed");
            return "{\"entities\":[],\"relations\":[]}";
        }
    }

    private static (List<ExtractedEntity> entities, List<ExtractedRelation> relations) ParseLlmOutput(
        string raw, InterpretationOptions opts)
    {
        var entities = new List<ExtractedEntity>();
        var relations = new List<ExtractedRelation>();
        if (string.IsNullOrWhiteSpace(raw)) return (entities, relations);

        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return (entities, relations);

        try
        {
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            var root = doc.RootElement;

            if (root.TryGetProperty("entities", out var es) && es.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in es.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var type = el.TryGetProperty("type", out var t) ? t.GetString() : null;
                    var desc = el.TryGetProperty("description", out var d) ? d.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type)) continue;
                    entities.Add(new(name!, type!.ToLowerInvariant(), desc, null, false));
                    if (entities.Count >= opts.MaxEntities) break;
                }
            }
            if (root.TryGetProperty("relations", out var rs) && rs.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in rs.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    var from = el.TryGetProperty("from", out var f) ? f.GetString() : null;
                    var rel  = el.TryGetProperty("rel",  out var r) ? r.GetString() : null;
                    var to   = el.TryGetProperty("to",   out var to_) ? to_.GetString() : null;
                    var desc = el.TryGetProperty("description", out var d2) ? d2.GetString() : null;
                    var sent = el.TryGetProperty("sentiment", out var s) ? s.GetString() : null;
                    if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(rel) || string.IsNullOrWhiteSpace(to)) continue;
                    relations.Add(new(from!, rel!.ToLowerInvariant(), to!, desc, sent, false, null));
                    if (relations.Count >= opts.MaxRelations) break;
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "FactInterpreter: failed to parse LLM JSON");
        }
        return (entities, relations);
    }

    // ── canon resolution + stub creation ──────────────────────────────────────

    private async Task<Guid?> ResolveAsync(
        StreetSamuraiDbContext db, string name, string entityType, string? description, CancellationToken ct)
    {
        var slug = WorldGraphService.Slugify(name);

        // Path 1: exact name/slug match within the typed bucket.
        var hit = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive
                     && e.EntityType == entityType
                     && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
        if (hit.HasValue) return hit;

        // Path 2: type-agnostic exact match (LLM may have guessed near-miss type).
        hit = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
        if (hit.HasValue) return hit;

        // Path 3: embedding nearest-match. Catches paraphrase ("Kyle's blade"
        // → Silence the katana), partial names ("Sasha" → Sasha Võ), and
        // entities the LLM rendered with cosmetically different spelling.
        // Only accepts hits above EmbeddingFallbackMinSimilarity so we don't
        // wire phantom associations.
        try
        {
            var query = string.IsNullOrWhiteSpace(description) ? name : $"{name}\n{description}";
            var hits = await embeddings.FindSimilarAsync(
                query, k: 3,
                entityTypes: string.IsNullOrEmpty(entityType) ? null : new[] { entityType },
                ct);
            var top = hits.FirstOrDefault();
            if (top != null && top.Similarity >= EmbeddingFallbackMinSimilarity)
            {
                log.LogInformation(
                    "FactInterpreter resolved '{Name}' ({Type}) via embedding fallback → '{Match}' ({MatchType}, sim={Sim:F3})",
                    name, entityType, top.EntityName, top.EntityType, top.Similarity);
                return top.EntityId;
            }
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Embedding fallback failed for '{Name}'", name);
        }

        return null;
    }

    /// <summary>
    /// Insert a minimal Entity row + the matching subtype row for the named
    /// type. Just enough to be a valid foreign-key target; details get filled
    /// in by later writers (the dictionary editor, an LLM enrichment pass, etc.).
    /// </summary>
    private async Task<Guid> CreateStubAsync(
        StreetSamuraiDbContext db, string name, string entityType, string? description, string sourceTag, CancellationToken ct)
    {
        var id = Guid.CreateVersion7();
        var slug = WorldGraphService.Slugify(name);
        // Disambiguate against any stale matching slug
        if (await db.Entities.AnyAsync(e => e.EntityType == entityType && e.Slug == slug, ct))
            slug = $"{slug}-{id:N}";

        db.Entities.Add(new Entity
        {
            Id          = id,
            EntityType  = entityType,
            Name        = name,
            Slug        = slug,
            Status      = "stub",
            Description = description,
            CreatedAt   = DateTime.UtcNow,
            ModifiedAt  = DateTime.UtcNow,
            IsActive    = true,
        });

        // Add a matching subtype row when one is known. Each subtype has its
        // own PK = Id; we just need Id + Name (Slug column was added via the
        // schema-rollout SQL ALTER and is nullable, so the EF classes don't
        // expose it as a property — the stub just inherits NULL slug, which
        // can be backfilled later by the canonical importer or a UI edit).
        switch (entityType)
        {
            case "character":      db.Characters     .Add(new Character     { Id = id, Name = name }); break;
            case "place":          db.Places         .Add(new Place         { Id = id, Name = name }); break;
            case "faction":        db.Factions       .Add(new Faction       { Id = id, Name = name }); break;
            case "corponation":    db.Corponations   .Add(new Corponation   { Id = id, Name = name }); break;
            case "subsidiary":     db.Subsidiaries   .Add(new Subsidiary    { Id = id, Name = name }); break;
            case "synthetic":      db.SyntheticLives .Add(new SyntheticLife { Id = id, Name = name }); break;
            case "automaton":      db.Automata       .Add(new Automaton     { Id = id, Name = name }); break;
            case "weapon":         db.Weapons        .Add(new Weapon        { Id = id, Name = name }); break;
            case "equipment":      db.EquipmentItems .Add(new Equipment     { Id = id, Name = name }); break;
            case "cyberware":      db.CyberwareItems .Add(new Cyberware     { Id = id, Name = name }); break;
            case "apparel":        db.Apparels       .Add(new Apparel       { Id = id, Name = name }); break;
            case "ammunition":     db.Ammunitions    .Add(new Ammunition    { Id = id, Name = name }); break;
            // Other types either lack a strict subtype table or use generic
            // Entity rows directly; the universal Entity insert above is enough.
        }
        await db.SaveChangesAsync(ct);
        return id;
    }
}
