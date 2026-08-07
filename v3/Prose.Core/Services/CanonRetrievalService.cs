using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// The engine's universal canon reach: given the text of what's being written,
/// pull the most relevant entities <em>across every entity type</em> from the
/// world database and render them as a compact, prompt-ready block the generator
/// (and Legion) can treat as established truth.
///
/// Before this, only ~7 graph-backed types (character/place/faction/corponation/
/// weapon/equipment/technology) ever reached prose; the other ~20 (cyberware,
/// ammunition, materials, pharmaceuticals, transport, synthetics, automatons,
/// subsidiaries, psionics, documents, consumer goods, …) were embedded but never
/// requested — "dead inventory." This service queries the full embedding index
/// (<see cref="EmbeddingService.FindSimilarAsync"/> with no type filter), so a
/// drug, a gun-load, a material, or an org can surface in a scene the moment it's
/// thematically relevant. That is the "full interconnectability" the engine needs.
/// </summary>
public class CanonRetrievalService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly EmbeddingService embeddings;
    private readonly ILogger<CanonRetrievalService> log;

    public CanonRetrievalService(
        IDbContextFactory<ProseDbContext> dbFactory,
        EmbeddingService embeddings,
        ILogger<CanonRetrievalService> log)
    {
        this.dbFactory = dbFactory;
        this.embeddings = embeddings;
        this.log = log;
    }

    /// <summary>One retrieved canon entity, ready to render.</summary>
    public sealed record CanonHit(Guid Id, string Name, string Type, double Similarity, string? Description);

    /// <summary>
    /// Retrieve the top entities (across all types unless <paramref name="onlyTypes"/>
    /// is given) most relevant to <paramref name="queryText"/>, best-first, with a
    /// one-line description pulled from the canonical Entity row.
    /// </summary>
    public async Task<List<CanonHit>> RetrieveAsync(
        string queryText, int k = 12, IReadOnlyCollection<string>? onlyTypes = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return [];

        var hits = await embeddings.FindSimilarAsync(queryText, k, onlyTypes, ct);
        if (hits.Count == 0) return [];

        var ids = hits.Select(h => h.EntityId).ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var descById = await db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => new { e.Id, e.Description })
            .ToDictionaryAsync(x => x.Id, x => x.Description, ct);

        return hits.Select(h => new CanonHit(
            h.EntityId, h.EntityName, h.EntityType, h.Similarity,
            descById.TryGetValue(h.EntityId, out var d) ? d : null)).ToList();
    }

    /// <summary>
    /// Render relevant canon as a prompt-ready block, ranked best-first and capped
    /// to <paramref name="charBudget"/>. Returns an empty string when nothing is
    /// relevant or the embedding index is cold, so callers can append it
    /// unconditionally. Pass <paramref name="excludeNames"/> to skip entities the
    /// caller already injected (e.g. the POV cast).
    /// </summary>
    public async Task<string> RetrieveContextBlockAsync(
        string queryText, int k = 12, int charBudget = 1800,
        IReadOnlyCollection<string>? excludeNames = null, CancellationToken ct = default)
    {
        var hits = await RetrieveAsync(queryText, k, onlyTypes: null, ct);
        if (hits.Count == 0) return "";

        var skip = excludeNames is { Count: > 0 }
            ? new HashSet<string>(excludeNames, StringComparer.OrdinalIgnoreCase)
            : null;

        var sb = new StringBuilder();
        sb.AppendLine("RELEVANT CANON — pulled from the world database; treat as established truth and do not contradict:");
        int used = sb.Length;
        int shown = 0;
        foreach (var h in hits)
        {
            if (skip != null && skip.Contains(h.Name)) continue;
            var desc = FirstSentence(h.Description);
            var line = desc.Length > 0
                ? $"- [{h.Type}] {h.Name}: {desc}"
                : $"- [{h.Type}] {h.Name}";
            if (used + line.Length + 1 > charBudget) break;
            sb.AppendLine(line);
            used += line.Length + 1;
            shown++;
        }

        if (shown == 0) return "";
        log.LogDebug("CanonRetrieval surfaced {N} entities for a {Len}-char query.", shown, queryText.Length);
        return sb.ToString().TrimEnd();
    }

    /// <summary>First sentence (or a clipped clause) of a description, so each
    /// line is a gloss, not a paragraph.</summary>
    internal static string FirstSentence(string? text, int max = 160)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim().Replace("\r", " ").Replace("\n", " ");
        var dot = t.IndexOf(". ", StringComparison.Ordinal);
        if (dot > 20 && dot < max) return t[..(dot + 1)];
        return t.Length <= max ? t : t[..max].TrimEnd() + "…";
    }
}
