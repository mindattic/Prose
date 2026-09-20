using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Deterministic candidate generation for the Tuned Read — the half of contradiction detection
/// that costs nothing.
///
/// <para><b>The gap it closes.</b> <c>ContinuityService.Upsert</c> flags a contradiction only on
/// <b>same predicate, different object</b>. The defect that motivated the Story Ledger was
/// <b>different predicate, incompatible meaning</b> — "Kyle → <c>father</c> → Dae-jung Seo"
/// against "Kyle → <c>origin</c> → constructed, no prior life". Those never collide under a
/// same-predicate rule, so the ledger could not represent the conflict at all. This service pairs
/// claims across predicates using the <see cref="PredicateExclusion"/> axioms, producing
/// CANDIDATES only — never verdicts.</para>
///
/// <para><b>Why candidates matter for cost.</b> The whole affordability argument of the Tuned
/// Read is that the LLM adjudicates a bounded number of specific pairs rather than reading a
/// book. Cost scales with the number of collisions, not with book length — which is what makes
/// this runnable across a 46-book corpus, and what the 100k-char clamp in
/// <c>LogicSweepService.BuildClampedProse</c> could never do (BCODA is ~1.9M chars; the sweep
/// elided the middle, which is exactly where the reconciling evidence sat).</para>
/// </summary>
public sealed class PredicateExclusionService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<PredicateExclusionService> log)
{
    /// <summary>
    /// Two live claims on one entity that an axiom says cannot both be true.
    ///
    /// <para><paramref name="FamilySize"/> is how many claim pairs this candidate STANDS FOR
    /// after collapsing (see <see cref="Collapse"/>). It is 1 for an uncollapsed pair.</para>
    /// </summary>
    public sealed record ExclusionCandidate(
        ContinuityClaim A,
        ContinuityClaim B,
        PredicateExclusion Rule,
        int FamilySize = 1);

    /// <summary>Claim statuses that are still believed. A REJECTED or SUPERSEDED claim has
    /// already been ruled out by a human or a later extraction — pairing it would re-raise a
    /// settled question and bill for the privilege.</summary>
    private static bool IsLive(string? status) =>
        status is not ("REJECTED" or "SUPERSEDED");

    /// <summary>
    /// Every active axiom in scope for <paramref name="universeId"/>: that universe's own rules
    /// plus the universal ones (<see cref="Guid.Empty"/>).
    ///
    /// <para>Scoping is applied HERE rather than by a global query filter, because the standard
    /// filter idiom (<c>ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId</c>)
    /// would hide every universal axiom the moment a real universe scope was set — the exact
    /// opposite of what <c>UniverseId == Guid.Empty</c> means on this table.</para>
    /// </summary>
    public async Task<List<PredicateExclusion>> GetActiveRulesAsync(
        Guid universeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.PredicateExclusions.AsNoTracking()
            .Where(r => r.Status == "active"
                     && (r.UniverseId == universeId || r.UniverseId == Guid.Empty))
            .OrderBy(r => r.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// All axiom-paired candidates among <paramref name="claims"/>. Pure function over the claim
    /// set and the rules — no DB access, no LLM, fully testable.
    ///
    /// <para>Complexity is O(rules × claims-per-entity²) with claims grouped per entity, which in
    /// practice is a handful of claims against a thin ontology. Pairing is restricted to claims
    /// about the SAME entity: an axiom says two things cannot both be true <i>of one subject</i>,
    /// and cross-entity pairing would assert that one character being constructed forbids another
    /// from having a father.</para>
    /// </summary>
    /// <param name="beatOrder">Beat id → position in true reading order, for
    /// <see cref="PredicateExclusion.TemporalOrder"/> rules. When null, temporal rules are SKIPPED
    /// rather than treated as timeless: a temporal axiom evaluated without an ordering is a
    /// different, much broader axiom than the one its author approved, and silently widening a rule
    /// is the failure mode this whole table is written to avoid.</param>
    public static List<ExclusionCandidate> GenerateCandidates(
        IReadOnlyList<ContinuityClaim> claims,
        IReadOnlyList<PredicateExclusion> rules,
        IReadOnlyDictionary<Guid, int>? beatOrder = null)
    {
        var candidates = new List<ExclusionCandidate>();
        if (rules.Count == 0) return candidates;

        var live = claims.Where(c => IsLive(c.Status)).ToList();
        // Dedup by claim-pair + rule so a symmetric axiom cannot emit the same question twice.
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in live.GroupBy(c => c.EntityId, StringComparer.Ordinal))
        {
            var entityClaims = group.ToList();
            if (entityClaims.Count < 2) continue;

            foreach (var rule in rules)
            {
                var temporal = IsTemporal(rule);
                // A temporal rule without an ordering is not a weaker version of itself, it is a
                // different axiom. Skip rather than silently widen.
                if (temporal && beatOrder == null) continue;

                foreach (var a in entityClaims)
                {
                    foreach (var b in entityClaims)
                    {
                        if (ReferenceEquals(a, b)) continue;
                        if (string.Equals(a.ClaimUid, b.ClaimUid, StringComparison.Ordinal)) continue;
                        if (!Matches(rule, a, b)) continue;
                        if (temporal && !BStrictlyAfterA(a, b, beatOrder!)) continue;

                        // A temporal pair is directional, so its ordering IS its meaning — keying
                        // it by uid order the way a timeless pair is keyed would let (a,b) and
                        // (b,a) collapse into whichever the uid sort happened to put first, and
                        // the surviving candidate could be the one pointing backwards in time.
                        var (first, second) = temporal
                            ? (a, b)
                            : string.CompareOrdinal(a.ClaimUid, b.ClaimUid) <= 0 ? (a, b) : (b, a);
                        if (!seen.Add($"{first.ClaimUid}|{second.ClaimUid}|{rule.Id}")) continue;

                        candidates.Add(new ExclusionCandidate(first, second, rule));
                    }
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// True when <paramref name="a"/> fills the rule's left slot and <paramref name="b"/> its
    /// right slot — or, for a <see cref="PredicateExclusion.Symmetric"/> rule, the reverse.
    /// </summary>
    /// <remarks>public, not internal: <c>prose --exclusion-rules --test</c> lives in Prose.Cli
    /// (a separate assembly, not covered by InternalsVisibleTo) and exists so an author can check
    /// a candidate axiom against a hypothetical claim pair BEFORE approving it. Being able to
    /// discover for free that a pattern is too broad is the cheapest guard this design has.</remarks>
    public static bool Matches(PredicateExclusion rule, ContinuityClaim a, ContinuityClaim b)
    {
        if (SideMatches(rule.PredicateA, rule.ObjectPatternA, a)
            && SideMatches(rule.PredicateB, rule.ObjectPatternB, b)) return true;

        // A temporal rule is directional by construction: swapping the sides asserts the opposite
        // ordering, so Symmetric is ignored for it even if a row somehow carries it set.
        return rule.Symmetric && !IsTemporal(rule)
            && SideMatches(rule.PredicateA, rule.ObjectPatternA, b)
            && SideMatches(rule.PredicateB, rule.ObjectPatternB, a);
    }

    /// <summary>True for an axiom carrying an ordering constraint on its two claims' beat anchors.</summary>
    public static bool IsTemporal(PredicateExclusion rule) =>
        string.Equals(rule.TemporalOrder, PredicateExclusion.TemporalBAfterA, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when both claims carry a beat anchor and <paramref name="b"/>'s beat sits strictly
    /// later in reading order than <paramref name="a"/>'s.
    ///
    /// <para>An unanchored claim fails this, deliberately. It cannot be placed on the book's clock,
    /// and pairing it anyway would turn "the character acts after their death" back into the
    /// timeless "the character is dead and also acts" — true of every character who ever dies
    /// on-page. Equal positions also fail: a death and an action recorded from the SAME beat is
    /// the death scene itself.</para>
    /// </summary>
    internal static bool BStrictlyAfterA(
        ContinuityClaim a, ContinuityClaim b, IReadOnlyDictionary<Guid, int> beatOrder)
    {
        if (a.SourceBeatId is not { } aBeat || b.SourceBeatId is not { } bBeat) return false;
        if (!beatOrder.TryGetValue(aBeat, out var aPos)) return false;
        if (!beatOrder.TryGetValue(bBeat, out var bPos)) return false;
        return bPos > aPos;
    }

    private static bool SideMatches(string predicatePattern, string? objectPattern, ContinuityClaim claim)
    {
        if (!PredicateMatchesPattern(predicatePattern, claim.Predicate)) return false;
        return ObjectMatchesPattern(objectPattern, claim.Object);
    }

    /// <summary>
    /// True when <paramref name="claimPredicate"/> equals any alternative in a
    /// <c>|</c>-separated <paramref name="pattern"/> (e.g. <c>"origin|nature|true_nature"</c>) —
    /// one axiom row covering the several predicate names extraction actually uses for one idea.
    ///
    /// <para><b>Alternatives are matched by EQUALITY, not substring</b> — unlike
    /// <see cref="ObjectMatchesPattern"/>. The asymmetry is deliberate and load-bearing: a
    /// predicate is an identifier, so a substring match would make <c>father</c> also match
    /// <c>grandfather</c> and <c>stepfather</c> and quietly widen every axiom past what its
    /// author approved. An object is free prose, where substring is the only workable test.</para>
    ///
    /// <para>An alternative may end in <c>*</c> to declare an anchored PREFIX family:
    /// <c>"father*"</c> matches <c>father</c>, <c>father_name</c>, <c>father_occupation</c> and
    /// <c>father_status</c>, but NOT <c>grandfather_name</c> — the anchor at the start is what
    /// keeps a family declaration from silently becoming a substring match. This exists because
    /// real extraction vocabulary is a family, not a word: BCODA's ledger records the fabricated
    /// father across <c>father_name</c>, <c>father_occupation</c>, <c>father_profession</c>,
    /// <c>father_status</c>, <c>father_took_swords</c> and more, and an axiom naming only
    /// <c>father</c> matched none of them. Found live 2026-09-03 by dry-running this instrument
    /// against the very defect it was built for.</para>
    ///
    /// <para>Found by the tests, not in review: the seeded builtin axioms were written with
    /// <c>|</c>-alternation in their predicate fields while this comparison was still plain
    /// equality, so every shipped builtin would have matched nothing at all — the precise
    /// "a rule that silently never matches is indistinguishable from no rule" failure this
    /// design set out to avoid.</para>
    /// </summary>
    internal static bool PredicateMatchesPattern(string? pattern, string? claimPredicate)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return false;
        var actual = NormalizePredicate(claimPredicate);
        if (actual.Length == 0) return false;

        foreach (var alt in pattern.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (alt.EndsWith('*'))
            {
                var stem = NormalizePredicate(alt[..^1]);
                // A bare "*" would match every predicate in the ledger — never what an author
                // means, and a guaranteed candidate explosion. Refuse it.
                if (stem.Length == 0) continue;
                // Anchored at the start, and the boundary must be exact or an underscore, so
                // "father*" covers father / father_name but never fatherless or grandfather_name.
                if (actual == stem || actual.StartsWith(stem + "_", StringComparison.Ordinal)) return true;
                continue;
            }
            if (string.Equals(NormalizePredicate(alt), actual, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>Single-predicate comparison: case-insensitive, whitespace-normalized, and
    /// underscore/space/hyphen-insensitive. Extraction passes have historically used
    /// <c>life_status</c>, <c>life status</c> and <c>life-status</c> for the same idea (see
    /// <c>ContinuityService.VolatilePredicates</c>' 2026-09-01 note on
    /// <c>traveling_with</c>/<c>companions</c> for the same vocabulary drift), and an axiom that
    /// silently misses because of a separator would look identical to no axiom at all.</summary>
    internal static bool PredicateEquals(string? a, string? b) =>
        string.Equals(NormalizePredicate(a), NormalizePredicate(b), StringComparison.Ordinal);

    /// <summary>The normalized predicate names in a <c>|</c>-separated pattern.</summary>
    internal static HashSet<string> SplitAlternatives(string? pattern) =>
        string.IsNullOrWhiteSpace(pattern)
            ? new HashSet<string>(StringComparer.Ordinal)
            : pattern.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizePredicate)
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

    private static string NormalizePredicate(string? s) =>
        string.IsNullOrWhiteSpace(s)
            ? ""
            : System.Text.RegularExpressions.Regex
                .Replace(s.ToLowerInvariant().Replace('-', '_').Replace(' ', '_'), "_+", "_")
                .Trim('_');

    /// <summary>
    /// True when <paramref name="objectValue"/> satisfies a <c>|</c>-separated alternation of
    /// case-insensitive substrings. A null/blank pattern means "any object".
    ///
    /// <para>Substrings, not regex, on purpose — see <see cref="PredicateExclusion.ObjectPatternA"/>:
    /// every pattern here has to be reviewable by the author approving it.</para>
    /// </summary>
    internal static bool ObjectMatchesPattern(string? pattern, string? objectValue)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return true;
        if (string.IsNullOrWhiteSpace(objectValue)) return false;

        foreach (var alt in pattern.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (objectValue.Contains(alt, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    /// <summary>
    /// Collapses candidates to ONE representative question per (entity, axiom) — the difference
    /// between one LLM call and sixty-five.
    ///
    /// <para><b>Why this is necessary, measured live.</b> The first calibrated run against BCODA
    /// produced 60+ candidates for Kyle alone. Not 60 defects: the ledger records the same
    /// fabricated fact under ~13 <c>father*</c> predicates (father_name, father_occupation,
    /// father_profession, father_status, father_took_swords, father_location_before_death …) and
    /// the constructed origin under ~5 (<c>construction_type</c>, <c>carrier_number</c>,
    /// <c>marrow_subject_number</c> …). The cross product is 65 pairs asking one question. Paying
    /// 65 Sonnet calls to learn one thing is exactly the cost failure the resolution gradient was
    /// designed to prevent, arriving through a different door.</para>
    ///
    /// <para>Collapsing is sound because the axiom is what makes the pair a question, and the
    /// axiom is a statement about the FAMILIES, not about two particular rows. If a constructed
    /// origin is incompatible with named parentage, learning that once settles every pair in the
    /// group. The representative is chosen for evidence quality — a pair where BOTH claims carry
    /// a beat anchor beats one where neither does, because an unanchored pair cannot be
    /// adjudicated against prose at all.</para>
    /// </summary>
    public static List<ExclusionCandidate> Collapse(IReadOnlyList<ExclusionCandidate> candidates) =>
        candidates
            .GroupBy(c => (c.A.EntityId, c.Rule.Id))
            .Select(g =>
            {
                var best = g
                    // Both anchored first, then either, then neither.
                    .OrderByDescending(c => (c.A.SourceBeatId.HasValue ? 1 : 0) + (c.B.SourceBeatId.HasValue ? 1 : 0))
                    // Then prefer claims that actually carry a snippet, since the adjudicator is
                    // shown it and a claim with no snippet is weaker evidence of the same fact.
                    .ThenByDescending(c => (string.IsNullOrWhiteSpace(c.A.Snippet) ? 0 : 1)
                                         + (string.IsNullOrWhiteSpace(c.B.Snippet) ? 0 : 1))
                    // Stable tiebreak so the representative — and therefore the verdict cache
                    // key — does not drift between runs on identical data.
                    .ThenBy(c => c.A.ClaimUid, StringComparer.Ordinal)
                    .ThenBy(c => c.B.ClaimUid, StringComparer.Ordinal)
                    .First();
                return best with { FamilySize = g.Count() };
            })
            .ToList();

    // ── learned-rule proposals ───────────────────────────────────────────────

    /// <summary>
    /// Records a <c>learned</c> axiom proposal for author approval, derived from a contradiction
    /// the adjudicator actually confirmed. Returns the row, or null when a rule of the same shape
    /// already exists in ANY status — including <c>rejected</c>, so an axiom the author has
    /// already judged wrong is never re-raised.
    ///
    /// <para>This is how the ontology gets sharper per incident rather than per patch. It lands
    /// as <c>proposed</c> and generates nothing until a human approves it: a self-approving
    /// rule generator would let one confident wrong verdict widen into a corpus-wide
    /// false-positive source.</para>
    /// </summary>
    public async Task<PredicateExclusion?> ProposeLearnedRuleAsync(
        Guid universeId, string predicateA, string? objectPatternA,
        string predicateB, string? objectPatternB, string rationale,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(predicateA) || string.IsNullOrWhiteSpace(predicateB)) return null;

        // Reject any OVERLAP between the two sides, not just an exact match: with alternation
        // patterns, "origin|nature" against "nature|father" would make a claim contradict itself
        // the moment a single `nature` claim existed, because both sides would match the same row.
        var altsA = SplitAlternatives(predicateA);
        var altsB = SplitAlternatives(predicateB);
        if (altsA.Overlaps(altsB)) return null; // same-predicate conflicts are Upsert's job

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.PredicateExclusions.AsNoTracking().AnyAsync(
            r => r.UniverseId == universeId
              && r.PredicateA == predicateA && r.ObjectPatternA == objectPatternA
              && r.PredicateB == predicateB && r.ObjectPatternB == objectPatternB, ct);
        if (exists) return null;

        var row = new PredicateExclusion
        {
            UniverseId = universeId,
            PredicateA = predicateA, ObjectPatternA = objectPatternA,
            PredicateB = predicateB, ObjectPatternB = objectPatternB,
            Symmetric = true,
            Source = "learned",
            Status = "proposed",
            Rationale = rationale.Length > 1000 ? rationale[..1000] : rationale,
        };
        db.PredicateExclusions.Add(row);
        await db.SaveChangesAsync(ct);
        log.LogInformation(
            "[tuned-read] Proposed learned exclusion #{Id}: {A} x {B} — awaiting approval.",
            row.Id, predicateA, predicateB);
        return row;
    }
}
