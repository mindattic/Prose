using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// A single Entity row that's part of a duplicate-name candidate group.
/// </summary>
public sealed record DuplicateEntityCandidate(
    Guid Id,
    string Name,
    Guid? OriginNodeId,
    bool IsActive,
    string? DescriptionSnippet,
    int MentionCount);

/// <summary>
/// Two or more character Entities in the same universe whose names are identical or very
/// close, sharing the same disambiguation scope (both universe-wide, or the same OriginNodeId) —
/// meaning <see cref="EntityDisambiguationService"/>'s legitimate same-name-different-book
/// mechanism does not explain the overlap. A genuine candidate for the author to merge or
/// explicitly disambiguate.
/// </summary>
public sealed record DuplicateEntityGroup(
    string MatchedOn,
    IReadOnlyList<DuplicateEntityCandidate> Candidates);

/// <summary>
/// Deterministic scan for duplicate/near-duplicate character Entity rows — no LLM. Generalizes
/// a real bug found manually on 2026-08-10: TEST's protagonist "Bear" had two separate Entity
/// rows ("Boris Johansen" and "Boris Johanssen" — a one-letter spelling difference), seeded from
/// two different drafts of the same book and never reconciled. Nothing before this service could
/// surface that class of bug mechanically; it was found by hand-grepping beat text during a
/// cross-book story-weaving investigation.
///
/// Two detection passes, both scoped to EntityType == "character" (the highest-value and by far
/// the most numerous type — ~1,864 in GLMZ alone as of 2026-08-10; a full cross-type pairwise
/// scan would be far more expensive for comparatively little narrative payoff):
///   1. Exact match after normalizing whitespace/case — catches straightforward duplicates.
///   2. Near-duplicate — names exactly 1 edit apart (insert/delete/substitute one character,
///      e.g. "Johansen"/"Johanssen"), checked only between lexicographically adjacent entries
///      after sorting (a sliding window), which keeps the scan O(n log n) instead of O(n²)
///      pairwise comparisons across the whole universe. Deliberately tight — edit distance 2
///      produced heavy false-positive noise on the first live run against GLMZ ("Marco"/
///      "Marcus", "Pip"/"Piper", "Sable"/"Salve" — all genuinely different characters).
///
/// A pair is excluded (not a bug) when <see cref="Data.Entities.Entity.OriginNodeId"/> is set to
/// DIFFERENT non-null values on each candidate — that's exactly what OriginNodeId exists for
/// (see its doc comment and <see cref="EntityDisambiguationService"/>): two genuinely different
/// characters who happen to share a name across different books' continuity.
///
/// No LLM calls — fast, deterministic. Available via `prose --duplicate-entity-scan --universe
/// &lt;slug&gt;`.
/// </summary>
public class DuplicateEntityScanService(IDbContextFactory<ProseDbContext> dbFactory)
{
    // Distance 1 catches the real bug class this service exists for (a single added/changed/
    // removed character — "Johansen"/"Johanssen", "Ines"/"Inés") while staying quiet on
    // genuinely different short names that a looser threshold flags as noise (distance 2 alone
    // matched "Marco"/"Marcus", "Pip"/"Piper", "Sable"/"Salve", "Sine"/"Siren" against the live
    // GLMZ universe on first run, 2026-08-10 — all real, distinct characters, not duplicates).
    private const int MaxEditDistance = 1;
    private const int SlidingWindow = 5;

    private sealed record EntityRow(Guid Id, string Name, Guid? OriginNodeId, bool IsActive, string? Description);

    public async Task<IReadOnlyList<DuplicateEntityGroup>> ScanAsync(Guid universeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Entities.AsNoTracking()
            .Where(e => e.UniverseId == universeId && e.EntityType == "character")
            .Select(e => new EntityRow(e.Id, e.Name, e.OriginNodeId, e.IsActive, e.Description))
            .ToListAsync(ct);

        if (entities.Count < 2) return [];

        var entityIds = entities.Select(e => e.Id).ToList();
        var mentionCounts = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => entityIds.Contains(m.EntityId))
            .GroupBy(m => m.EntityId)
            .Select(g => new { EntityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityId, x => x.Count, ct);

        DuplicateEntityCandidate ToCandidate(EntityRow e) => new(
            e.Id, e.Name, e.OriginNodeId, e.IsActive,
            e.Description == null ? null : Snippet(e.Description),
            mentionCounts.GetValueOrDefault(e.Id, 0));

        var groups = new List<DuplicateEntityGroup>();
        var alreadyGrouped = new HashSet<Guid>();

        // Pass 1: exact match after normalization.
        var byNormalized = entities
            .GroupBy(e => Normalize(e.Name))
            .Where(g => g.Count() > 1);

        foreach (var g in byNormalized)
        {
            var members = g.ToList();
            if (!SharesDisambiguationScope(members.Select(m => (Guid?)m.OriginNodeId))) continue;

            groups.Add(new DuplicateEntityGroup(
                $"exact match: \"{g.Key}\"",
                members.Select(m => ToCandidate(m)).ToList()));
            foreach (var m in members) alreadyGrouped.Add(m.Id);
        }

        // Pass 2: near-duplicate, sliding window over sorted normalized names.
        var sorted = entities
            .Where(e => !alreadyGrouped.Contains(e.Id))
            .OrderBy(e => Normalize(e.Name), StringComparer.Ordinal)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            for (int j = i + 1; j < Math.Min(i + 1 + SlidingWindow, sorted.Count); j++)
            {
                var a = sorted[i];
                var b = sorted[j];
                if (alreadyGrouped.Contains(a.Id) || alreadyGrouped.Contains(b.Id)) continue;

                var na = Normalize(a.Name);
                var nb = Normalize(b.Name);
                if (na == nb) continue; // already covered by pass 1

                var distance = LevenshteinDistance(na, nb);
                if (distance == 0 || distance > MaxEditDistance) continue;
                if (!SharesDisambiguationScope([a.OriginNodeId, b.OriginNodeId])) continue;

                groups.Add(new DuplicateEntityGroup(
                    $"near match (edit distance {distance}): \"{a.Name}\" / \"{b.Name}\"",
                    [ToCandidate(a), ToCandidate(b)]));
                alreadyGrouped.Add(a.Id);
                alreadyGrouped.Add(b.Id);
            }
        }

        return groups;
    }

    /// <summary>
    /// True when the candidates are NOT legitimately disambiguated by OriginNodeId — i.e. they
    /// share the same scope (all null, or all the same non-null value) rather than each pointing
    /// at a different book. Different non-null values means "different books, deliberately
    /// distinct characters" — not a bug.
    /// </summary>
    internal static bool SharesDisambiguationScope(IEnumerable<Guid?> originNodeIds)
    {
        var distinct = originNodeIds.Distinct().ToList();
        return distinct.Count == 1;
    }

    internal static string Normalize(string name) =>
        string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string Snippet(string description) =>
        description.Length <= 120 ? description : description[..120].TrimEnd() + "…";

    /// <summary>Standard iterative Levenshtein edit distance (insert/delete/substitute), O(len1*len2).</summary>
    internal static int LevenshteinDistance(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }
}
