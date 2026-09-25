using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Harvest canon from open text: capture every load-bearing noun phrase, resolve each
/// against the entity corpus (exact name → alias-insensitive → embedding similarity),
/// create the missing ones as stubs in their proper EntityType, and wire Edges between
/// every related pair — new↔new, new↔existing, and existing↔existing when the text
/// asserts a relation the graph doesn't carry yet.
///
/// Intended for design notes, canon briefs, and worldbuilding passages ("take this text
/// and make the DB know everything it names"). Created rows use Status="stub" so the
/// entity queue can promote them to canon after review. Run <c>prose --reembed</c> after a
/// non-dry harvest so new rows join the semantic index.
/// </summary>
public class EntityHarvestService(
    IDbContextFactory<ProseDbContext> dbFactory,
    Interfaces.ILlmService llm,
    EmbeddingService embeddings,
    XrefService xref,
    ILogger<EntityHarvestService> log)
{
    // Above this cosine similarity an extracted noun is treated as an existing entity
    // rather than a new row. Conservative on purpose: a false merge poisons canon, a
    // false create is a reviewable stub.
    const double MergeSimilarity = 0.92;

    public sealed record HarvestCandidate(
        string Name, string EntityType, string Description, string[] Related);

    public sealed record HarvestResolution(
        string Name, string EntityType, Guid EntityId, string Outcome /* existing | similar | created | skipped */, string? MatchedName);

    public sealed record HarvestResult(
        IReadOnlyList<HarvestResolution> Entities,
        int EdgesCreated,
        IReadOnlyList<string> Warnings);

    public async Task<HarvestResult> HarvestAsync(
        string text, Guid universeId, bool dryRun = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new HarvestResult([], 0, ["input text is empty"]);

        var warnings = new List<string>();
        var candidates = await ExtractCandidatesAsync(text, ct);
        if (candidates.Count == 0)
            return new HarvestResult([], 0, ["extraction returned no candidates"]);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Resolve every candidate: exact name (case-insensitive) → embedding merge → create.
        var resolutions = new Dictionary<string, HarvestResolution>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in candidates)
        {
            if (ct.IsCancellationRequested) break;
            var name = c.Name.Trim();
            if (name.Length < 3 || resolutions.ContainsKey(name)) continue;

            // Exact match on the name and its cheap variants: singular/plural,
            // with/without a leading article, and a trailing generic word dropped
            // ("NSB framework" → "NSB"). Variants keep design notes from stubbing
            // duplicates of entities that exist under a slightly different form.
            var variants = NameVariants(name).ToList();
            // Best match first — the exact name, then the variants in order, same type preferred.
            // An unordered FirstOrDefault could hand back a looser variant ("Kyle" for
            // "Kyle Reyes") even when the exact entity existed.
            var existing = (await db.Set<Entity>().AsNoTracking()
                .Where(e => e.Status != "archived" && variants.Contains(e.Name))
                .Select(e => new { e.Id, e.Name, e.EntityType })
                .ToListAsync(ct))
                .OrderBy(e => variants.FindIndex(v => string.Equals(v, e.Name, StringComparison.OrdinalIgnoreCase)) is var vi && vi >= 0 ? vi : int.MaxValue)
                .ThenBy(e => string.Equals(e.EntityType, c.EntityType, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();
            if (existing != null)
            {
                resolutions[name] = new(name, c.EntityType, existing.Id, "existing", existing.Name);
                continue;
            }

            // Alias/handle match — same class of miss as the name-variant check above, just
            // against a known alias instead of the canonical name (e.g. a candidate extracted
            // as "Rook" when the corpus already has "Inkeri Saarinen" with "Rook" as an alias).
            // Scoped to same-type matches only — a place and a character can legitimately share
            // an alias string without being the same entity.
            var aliasHit = xref.Resolve(name);
            if (aliasHit != null && string.Equals(aliasHit.Type, c.EntityType, StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(aliasHit.Id, out var aliasEntityId))
            {
                resolutions[name] = new(name, c.EntityType, aliasEntityId, "existing", aliasHit.DisplayName);
                continue;
            }

            EmbeddingHit? similar = null;
            try
            {
                var hits = await embeddings.FindSimilarAsync(name, k: 3, entityTypes: null, ct);
                similar = hits.FirstOrDefault(h => h.Similarity >= MergeSimilarity);
            }
            catch (Exception ex)
            {
                warnings.Add($"embedding lookup unavailable for '{name}' ({ex.Message}) — name match only");
            }
            if (similar != null)
            {
                resolutions[name] = new(name, c.EntityType, similar.EntityId, "similar", similar.EntityName);
                continue;
            }

            if (dryRun)
            {
                resolutions[name] = new(name, c.EntityType, Guid.Empty, "created", null);
                continue;
            }

            var entity = new Entity
            {
                UniverseId = universeId,
                EntityType = string.IsNullOrWhiteSpace(c.EntityType) ? "vocabulary" : c.EntityType.Trim().ToLowerInvariant(),
                Name = name,
                Slug = Slugify(name),
                Status = "stub",
                Description = c.Description.Trim(),
            };
            db.Add(entity);
            resolutions[name] = new(name, entity.EntityType, entity.Id, "created", null);
        }

        if (!dryRun) await db.SaveChangesAsync(ct);

        // Wire edges for every asserted relation where both ends resolved.
        int edgesCreated = 0;
        var sourceTag = $"harvest:{DateTime.UtcNow:yyyyMMdd}";
        // Edges added this run are not saved until the end, so the database check below cannot
        // see them: A related:[B] plus B related:[A] (or related:[B,B]) made duplicate edges.
        var addedPairs = new HashSet<(Guid, Guid)>();
        foreach (var c in candidates)
        {
            if (!resolutions.TryGetValue(c.Name.Trim(), out var from) || from.EntityId == Guid.Empty) continue;
            foreach (var relName in c.Related.Where(r => r != null).Select(r => r.Trim()).Where(r => r.Length >= 3))
            {
                if (!resolutions.TryGetValue(relName, out var to) || to.EntityId == Guid.Empty) continue;
                if (to.EntityId == from.EntityId) continue;
                var pair = from.EntityId.CompareTo(to.EntityId) < 0 ? (from.EntityId, to.EntityId) : (to.EntityId, from.EntityId);
                if (!addedPairs.Add(pair)) continue;

                var exists = await db.Set<Edge>().AsNoTracking().AnyAsync(e =>
                    ((e.SourceId == from.EntityId && e.TargetId == to.EntityId)
                  || (e.SourceId == to.EntityId && e.TargetId == from.EntityId))
                    && e.InvalidatedAt == null, ct);
                if (exists) continue;

                if (!dryRun)
                {
                    db.Add(new Edge
                    {
                        UniverseId = universeId,
                        SourceId = from.EntityId,
                        TargetId = to.EntityId,
                        RelationType = "related_to",
                        Description = $"Asserted together in harvested text ({from.Name} ↔ {to.Name})",
                        Weight = 0.8,
                        Sentiment = "neutral",
                        Source = sourceTag,
                    });
                }
                edgesCreated++;
            }
        }
        if (!dryRun) await db.SaveChangesAsync(ct);

        log.LogInformation(
            "Entity harvest: {Total} candidates → {Existing} existing, {Similar} merged, {Created} created, {Edges} edges{Dry}",
            candidates.Count,
            resolutions.Values.Count(r => r.Outcome == "existing"),
            resolutions.Values.Count(r => r.Outcome == "similar"),
            resolutions.Values.Count(r => r.Outcome == "created"),
            edgesCreated,
            dryRun ? " (dry-run)" : "");

        return new HarvestResult(resolutions.Values.ToList(), edgesCreated, warnings);
    }

    async Task<List<HarvestCandidate>> ExtractCandidatesAsync(string text, CancellationToken ct)
    {
        const string system =
            "You harvest worldbuilding canon from free text into a fiction entity database. " +
            "Extract every load-bearing PROPER NOUN and named CONCEPT: technologies, factions, places, " +
            "characters, vocabulary/slang terms, documents, events, materials, weapons. " +
            "Skip generic common nouns (door, city, engineer) and skip pronouns. " +
            "For each: a canonical name (as the text uses it, singular), an entityType from " +
            "[character, place, faction, corponation, technology, vocabulary, weapon, document, event, " +
            "material, equipment, archetype, motif], a 1-3 sentence description built ONLY from what the " +
            "text asserts (no invention), and the names of other extracted items it is directly related to. " +
            "Return STRICT JSON only, no markdown fence: " +
            "[{\"name\":\"...\",\"entityType\":\"...\",\"description\":\"...\",\"related\":[\"...\"]}]";

        string raw;
        try { raw = await llm.GenerateAsync(system, text, temperature: 0.1, maxTokens: 4000, ct: ct); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Entity harvest extraction call failed");
            return [];
        }

        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end <= start) return [];

        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<List<RawCandidate>>(raw[start..(end + 1)], opts) ?? [];
            return parsed
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => new HarvestCandidate(p.Name!, p.EntityType ?? "vocabulary", p.Description ?? "", p.Related ?? []))
                .ToList();
        }
        catch (JsonException ex)
        {
            log.LogWarning(ex, "Entity harvest JSON parse failed");
            return [];
        }
    }

    static IEnumerable<string> NameVariants(string name)
    {
        yield return name;
        if (name.EndsWith('s') && name.Length > 4) yield return name[..^1];      // plural → singular
        else yield return name + "s";                                             // singular → plural
        yield return "The " + name;                                               // article variants
        if (name.StartsWith("The ", StringComparison.OrdinalIgnoreCase) && name.Length > 6)
            yield return name[4..];
        var lastSpace = name.LastIndexOf(' ');                                     // "NSB framework" → "NSB"
        // Only a generic lowercase tail: dropping a capitalised word turned "Red Queen" into "Red"
        // and wired every edge to an unrelated entity.
        if (lastSpace > 2 && lastSpace < name.Length - 1 && char.IsLower(name[lastSpace + 1]))
            yield return name[..lastSpace];
    }

    static string Slugify(string name)
    {
        var slug = new string(name.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    sealed class RawCandidate
    {
        public string? Name { get; set; }
        public string? EntityType { get; set; }
        public string? Description { get; set; }
        public string[]? Related { get; set; }
    }
}
