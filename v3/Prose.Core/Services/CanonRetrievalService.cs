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
    /// Entity.Status values that mean "this row is no longer live canon". They stay in the
    /// table (audit, restore, redirect) but must never be presented to an LLM as established
    /// truth: a merged/superseded row's Description is bookkeeping, not world fact
    /// ("Superseded duplicate; merged into Chen Lin (chen_lin / …)"), and feeding it to the
    /// contradiction checker produced a guaranteed false CANON-CONTRADICTION on every audit
    /// ("Mrs. Chen is documented as a superseded duplicate … she should not appear as an active
    /// character in current prose") for a name that is in fact a live alias of the canonical
    /// row. Deny-list rather than allow-list so a new status defaults to visible.
    /// </summary>
    private static readonly string[] DeadStatuses =
        ["archived", "merged", "superseded-duplicate", "retired"];

    /// <summary>
    /// Retrieve the top entities (across all types unless <paramref name="onlyTypes"/>
    /// is given) most relevant to <paramref name="queryText"/>, best-first, with a
    /// one-line description pulled from the canonical Entity row.
    ///
    /// Pass <paramref name="currentNodeId"/> to keep other books' book-scoped entities out of
    /// this book's canon block (SS: Entity.OriginNodeId / "cross-book same-name resolves").
    /// Without it, embedding similarity alone can surface e.g. "Noor Adeyemi-Vance" (a
    /// different book's character, explicitly scoped to that book via OriginNodeId) for a
    /// chunk that just says "Noor" — a downstream contradiction-checker LLM then has no way
    /// to know which "Noor" the prose actually means and can flag a false conflict. This drop
    /// is unconditional (not contingent on this book's own same-name entity also appearing in
    /// the same top-k batch): an entity explicitly scoped to a different node is definitionally
    /// not this book's canon, whether or not the disambiguating collision co-occurs in this
    /// particular call. Entities with no OriginNodeId (shared/universe-wide) are never dropped.
    /// </summary>
    public async Task<List<CanonHit>> RetrieveAsync(
        string queryText, int k = 12, IReadOnlyCollection<string>? onlyTypes = null,
        Guid currentNodeId = default, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return [];

        var hits = await embeddings.FindSimilarAsync(queryText, k, onlyTypes, ct);
        if (hits.Count == 0) return [];

        var ids = hits.Select(h => h.EntityId).ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var infoById = await db.Entities.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Where(e => !DeadStatuses.Contains(e.Status))
            .Select(e => new { e.Id, e.Description, e.OriginNodeId })
            .ToDictionaryAsync(x => x.Id, x => x, ct);

        if (currentNodeId != default)
            infoById = infoById.Values
                .Where(v => v.OriginNodeId == null || v.OriginNodeId == currentNodeId)
                .ToDictionary(v => v.Id, v => v);

        return hits.Where(h => infoById.ContainsKey(h.EntityId))
            .Select(h => new CanonHit(
                h.EntityId, h.EntityName, h.EntityType, h.Similarity, infoById[h.EntityId].Description))
            .ToList();
    }

    /// <summary>
    /// Render relevant canon as a prompt-ready block, ranked best-first and capped
    /// to <paramref name="charBudget"/>. Returns an empty string when nothing is
    /// relevant or the embedding index is cold, so callers can append it
    /// unconditionally. Pass <paramref name="excludeNames"/> to skip entities the
    /// caller already injected (e.g. the POV cast). Pass <paramref name="currentNodeId"/>
    /// to disambiguate cross-book name collisions (see <see cref="RetrieveAsync"/>).
    /// </summary>
    public async Task<string> RetrieveContextBlockAsync(
        string queryText, int k = 12, int charBudget = 1800,
        IReadOnlyCollection<string>? excludeNames = null, Guid currentNodeId = default,
        int descriptionChars = 160, CancellationToken ct = default)
    {
        var hits = await RetrieveAsync(queryText, k, onlyTypes: null, currentNodeId, ct);
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
            // Default 160-char gloss is fine for prose-generation context (a reminder, not a
            // dossier) but starves accuracy-critical callers like CanonContradictionService:
            // a truncated-to-first-sentence description ("Second-generation Bosniak-Somali GLMZ
            // native.") can omit the very fact (e.g. a 14-year checkpoint career two sentences
            // later) that would have proven the prose does NOT contradict canon, producing a
            // false CANON-CONTRADICTION finding. Pass a larger descriptionChars for those callers.
            var desc = descriptionChars <= 160
                ? FirstSentence(h.Description, descriptionChars)
                : Clip(h.Description, descriptionChars);
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

    /// <summary>Plain hard clip to <paramref name="max"/> chars — unlike <see cref="FirstSentence"/>,
    /// does NOT stop at the first period, so callers that need enough of a multi-sentence
    /// description to judge accurately (e.g. contradiction-checking) don't lose facts that live
    /// past sentence one.</summary>
    internal static string Clip(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim().Replace("\r", " ").Replace("\n", " ");
        return t.Length <= max ? t : t[..max].TrimEnd() + "…";
    }
}
