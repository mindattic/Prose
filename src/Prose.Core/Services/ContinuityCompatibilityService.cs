using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Filters <see cref="ContinuityService.GetContradictionGroups"/>'s raw output down to
/// contradictions that are actually contradictions. Found live 2026-08-19/20: 8 of 9 real
/// groups investigated in a single session were false positives — the panel's chosen "winner"
/// was just a different-granularity restatement of a fact the prose/bible already supported
/// ("ex-Arcturus" vs "ex-Arcturus Defense Solutions"; a person carrying both "a bead in ear" AND
/// "a Fade capsule", not mutually exclusive items). Without this filter, an unattended
/// reconciler burns real LLM spend "fixing" prose that was never broken — the opposite of
/// self-healing.
///
/// <para>Two-stage check per group, cheapest first:</para>
/// <list type="number">
/// <item>Substring containment between every pair of distinct Object values (free, no LLM
/// call) — catches "ex-Arcturus" ⊂ "ex-Arcturus Defense Solutions" for $0.</item>
/// <item>One cheap classifier call for the WHOLE group (not pairwise — an N-way group costs
/// one call, not C(n,2)), only when stage 1 leaves an unresolved pair, cached by
/// <see cref="ContinuityCompatibilityJudgment.ObjectSetHash"/> so the same variant set is never
/// re-billed.</item>
/// </list>
///
/// <para>A group is filtered out of "genuine" only if EVERY pair clears one of the two stages
/// as compatible. Any unresolved/ambiguous pair, or any classifier response that doesn't
/// unambiguously say "compatible," fails OPEN — the group stays genuine. This never hides a
/// real unresolved contradiction; it only suppresses groups that are entirely restatement
/// noise.</para>
///
/// <para>Deliberately a separate service from <see cref="ContinuityService"/> (which stays
/// DB-only) since this needs <see cref="ILlmService"/> — and deliberately does NOT encode a
/// "compatible" verdict as a new <see cref="ContinuityClaim"/> status: both claims in a
/// compatible pair stay legitimately live (neither is a winner or loser), which the existing
/// CANONICAL/REJECTED lifecycle locked down by <c>ContinuityServiceCanonicalConflictTests</c>/
/// <c>ContinuityServicePartialRejectTests</c> has no room for. Caching the verdict against the
/// group's own identity instead keeps <see cref="ContinuityService.GetContradictionGroups"/>
/// itself completely unchanged — only the genuine-filtered view built on top of it changes.</para>
/// </summary>
public class ContinuityCompatibilityService(
    ContinuityService continuityStore,
    ILlmService llm,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<ContinuityCompatibilityService> log)
{
    public async Task<List<ContradictionGroup>> GetGenuineContradictionGroupsAsync(
        string? bookSlug = null, CancellationToken ct = default)
        => await FilterGenuineAsync(continuityStore.GetContradictionGroups(bookSlug), ct);

    public async Task<List<ContradictionGroup>> GetGenuineContradictionGroupsSinceAsync(
        DateTime sinceUtc, CancellationToken ct = default)
        => await FilterGenuineAsync(continuityStore.GetContradictionGroupsSince(sinceUtc), ct);

    private async Task<List<ContradictionGroup>> FilterGenuineAsync(List<ContradictionGroup> groups, CancellationToken ct)
    {
        var result = new List<ContradictionGroup>();
        foreach (var g in groups)
        {
            if (await IsGenuineAsync(g, ct))
                result.Add(g);
        }
        return result;
    }

    /// <summary>True when at least one pair of distinct Object values in the group is a real
    /// conflict (the group should stay open); false when every pair is a superset/rephrasing or
    /// was classified non-exclusive (the group is restatement noise and should be filtered).</summary>
    internal async Task<bool> IsGenuineAsync(ContradictionGroup group, CancellationToken ct)
    {
        var objects = group.Claims
            .Select(c => c.Object)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (objects.Count < 2) return true; // group invariant violated — fail open, don't hide it

        var unresolvedByStage1 = new List<(string A, string B)>();
        for (var i = 0; i < objects.Count; i++)
            for (var j = i + 1; j < objects.Count; j++)
                if (!IsSubstringContainment(objects[i], objects[j]))
                    unresolvedByStage1.Add((objects[i], objects[j]));

        if (unresolvedByStage1.Count == 0)
        {
            await CacheResultAsync(group.EntityId, group.Predicate, ComputeObjectSetHash(objects),
                "compatible", "stage1: substring containment", ct);
            return false;
        }

        var hash = ComputeObjectSetHash(objects);
        var cached = await GetCachedAsync(group.EntityId, group.Predicate, hash, ct);
        if (cached != null) return cached.Result != "compatible";

        var (result, reasoning) = await ClassifyAsync(group, objects, ct);
        await CacheResultAsync(group.EntityId, group.Predicate, hash, result, reasoning, ct);
        return result != "compatible";
    }

    private async Task<(string Result, string Reasoning)> ClassifyAsync(ContradictionGroup group, List<string> objects, CancellationToken ct)
    {
        var question =
            "You are checking whether these are ACTUALLY conflicting facts about the same entity, or just " +
            "differently-detailed or differently-phrased descriptions of the same underlying truth that could " +
            "all be simultaneously valid (e.g. one value is more specific than another, or they describe " +
            "different non-exclusive aspects/items — a person can carry more than one piece of equipment). " +
            "Output ONLY one line: \"COMPATIBLE: <one-line reason>\" if every value below could coexist, or " +
            "\"CONTRADICTORY: <one-line reason>\" if at least two of the values below cannot both be true at once.";
        var context =
            $"Entity: {group.EntityName} ({group.EntityKind})\nPredicate: {group.Predicate}\nValues asserted:\n" +
            string.Join("\n", objects.Select(o => $"- \"{o}\""));

        string response;
        try
        {
            response = (await llm.GenerateAsync(question, context, temperature: 0.1, maxTokens: 150, ct: ct)) ?? "";
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[continuity-compat] Classifier call failed for {Entity}.{Predicate} — failing open (treated as contradictory).",
                group.EntityName, group.Predicate);
            return ("contradictory", "classifier call failed — failed open");
        }

        var trimmed = response.Trim();
        var isCompatible = trimmed.StartsWith("COMPATIBLE", StringComparison.OrdinalIgnoreCase);
        var isContradictory = trimmed.StartsWith("CONTRADICTORY", StringComparison.OrdinalIgnoreCase);
        // Any ambiguous/unparseable response fails OPEN (contradictory) — never silently suppress
        // a real conflict because the classifier's output didn't parse cleanly.
        var result = isCompatible && !isContradictory ? "compatible" : "contradictory";
        return (result, trimmed);
    }

    private async Task<ContinuityCompatibilityJudgment?> GetCachedAsync(string entityId, string predicate, string hash, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.ContinuityCompatibilityJudgments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EntityId == entityId && x.Predicate == predicate && x.ObjectSetHash == hash, ct);
    }

    private async Task CacheResultAsync(string entityId, string predicate, string hash, string result, string reasoning, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.ContinuityCompatibilityJudgments
            .FirstOrDefaultAsync(x => x.EntityId == entityId && x.Predicate == predicate && x.ObjectSetHash == hash, ct);
        if (existing != null)
        {
            existing.Result = result;
            existing.Reasoning = reasoning;
            existing.ClassifiedAt = DateTime.UtcNow;
        }
        else
        {
            db.ContinuityCompatibilityJudgments.Add(new ContinuityCompatibilityJudgment
            {
                Id = Guid.NewGuid(), EntityId = entityId, Predicate = predicate, ObjectSetHash = hash,
                Result = result, Reasoning = reasoning,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Case-insensitive substring containment either direction — one value being a
    /// superset/rephrasing of the other (e.g. "ex-Arcturus" ⊂ "ex-Arcturus Defense Solutions")
    /// is never itself a genuine conflict.</summary>
    internal static bool IsSubstringContainment(string a, string b)
        => a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase);

    /// <summary>SHA-256 hex of the sorted, normalized, distinct Object-string set — order- and
    /// case-independent, so re-hashing the same variant set always produces the same cache key.</summary>
    internal static string ComputeObjectSetHash(IEnumerable<string> objects)
    {
        var normalized = string.Join("|", objects.Select(o => o.Trim().ToLowerInvariant()).OrderBy(o => o, StringComparer.Ordinal));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
