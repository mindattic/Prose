using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Unified continuity store. Atomic (entity, predicate, object) claims extracted
/// from chapter prose or entity records. ContinuityClaims with the same
/// (entity, predicate) and different object are flagged CONTRADICTED so a
/// resolution flow can pick a winner.
///
/// Backed by the unified Prose SQL Server database — the legacy
/// continuity.db SQLite store has been retired. Public API is preserved so
/// every existing caller compiles unchanged.
/// </summary>
public class ContinuityService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public ContinuityService(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>Path of the legacy SQLite file. Kept for diagnostic display only — no longer authoritative.</summary>
    public string DbPath => "(SQL Server: continuity tables in Prose database)";
    public bool IsAvailable => true;

    // ── ID generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Stable uid: hash of (entity_id | predicate | normalized object). Same
    /// (entity, predicate, object) always produces the same uid, so re-extracting
    /// the same claim is idempotent — the row is updated, not duplicated.
    /// </summary>
    public static string ComputeClaimUid(string entityId, string predicate, string objectValue)
    {
        var normalized = $"{entityId}|{Normalize(predicate)}|{NormalizeObjectForUid(predicate, objectValue)}";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return "claim-" + hex[..16];
    }

    private static string Normalize(string s)
        => string.IsNullOrEmpty(s) ? "" : System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"\s+", " ").Trim();

    // ── Numeric-safe fact comparison (2026-08-14) ───────────────────────────
    //
    // ContinuityClaims' contradiction check used to be bare string equality
    // (Object.ToLower().Trim() != ...), so "fifty" vs "50" registered as a false
    // CONTRADICTED pair even though they're the same value — this is exactly the
    // arithmetic-drift bug class this session hit repeatedly on VIGL (a career
    // length re-derived by an LLM read differently across sweep rounds: "fifty"
    // one round, "50" another, both correct, flagged as contradicting each other).
    //
    // Gated by an explicit allowlist, NOT auto-detected on every predicate — so
    // location/relationship/every other claim type is completely untouched by
    // this change. Distinct real-world clocks (e.g. a character's career length
    // vs. a separate catastrophe's age-in-years) must use DISTINCT predicate keys;
    // this normalization only makes "fifty" == "50" for the SAME predicate, it
    // does not and must not relate two different predicates to each other.
    private static readonly HashSet<string> NumericPredicates = new(StringComparer.Ordinal)
    {
        "age", "tenure_years", "career_length_years", "zone_age_years", "duration_years", "years",
    };

    /// <summary>
    /// Predicates describing a character's state AT A MOMENT rather than a durable fact about
    /// them. A later value does not contradict an earlier one — it supersedes it, because the
    /// character moved, or appeared in another book, or changed what they were carrying.
    ///
    /// Added 2026-08-23 after generating the first chapter of a sequel: roughly a quarter of all
    /// findings on that chapter were <c>CONTINUITY-VIOLATION [Kyle Ellen Corbin] location_current</c>
    /// and <c>appearance_in_story</c> — fired because BCODA had recorded where Kyle was standing
    /// and which story he was in, and Book 2 legitimately put him somewhere else. Left unfixed
    /// this fires on essentially every beat of every sequel for every recurring character, and
    /// the noise buries the real contradictions the ledger exists to catch.
    ///
    /// Extended 2026-09-01 after the same failure recurred within a SINGLE book (VIGL, post
    /// airship-cut rewrite): a different extraction pass had used its own predicate vocabulary for
    /// the identical "moment, not invariant" concept — <c>traveling_with</c>/<c>companions</c> is
    /// the same idea under a different name, and the book's cast legitimately changes as Doyle,
    /// Wren, and Ardea join over the course of the story. 14 of 24 open CONTINUITY-VIOLATION
    /// findings on VIGL were exactly this one stale snapshot ("Orim, a seventy-year-old scryer,
    /// and a Rod") compared against every later chapter's real, correct, evolving travel party.
    /// The remaining additions below are the same class for other scene-scoped facts that
    /// naturally change chapter to chapter or wound to wound: where a chapter opens
    /// (<c>location_at_chapter_start</c>), whether the character has slept
    /// (<c>sleep_status</c>), what's in their hands or on their back right now
    /// (<c>weapon_carry</c>, <c>carries_item</c>, <c>carries_equipment</c>, <c>carries_weapon</c>),
    /// what task they're mid-execution on (<c>current_task</c>), and where their most recent
    /// injury is (<c>injury</c>, <c>injury_location</c>, <c>shoulder_injury</c> — a long journey
    /// accumulates more than one wound; an old scar and a fresh cut are not a contradiction).
    ///
    /// Deliberately NOT added despite also appearing in VIGL's open findings: identity/durable
    /// predicates like <c>rank</c>, <c>profession</c>, <c>occupation</c>,
    /// <c>organization_affiliation</c>, <c>employment</c> (is she Templar or Vigil service? — a
    /// real question worth a human read, not a moment that supersedes itself) and pure
    /// paraphrase-of-the-same-fact pairs (e.g. "Ocipheus" vs "Ocipheus Station") that need a
    /// same-assertion/numeric-dedup fix, not a volatility exemption — silently exempting either
    /// class here would hide a real defect instead of fixing the false-positive mechanism.
    ///
    /// The ledger is for invariants ("his mentor was Seito", "he is carrier seven"). Time-scoped
    /// state belongs to <c>EntityStateEvents</c> / <c>WorldStateAtBeatService</c>, which model a
    /// timeline instead of asserting one permanent truth.
    /// </summary>
    private static readonly HashSet<string> VolatilePredicates = new(StringComparer.Ordinal)
    {
        "location_current", "appearance_in_story", "current_location", "location",
        "present_in_scene", "carrying", "wearing", "status_current", "current_status",
        "mood", "current_mood", "companions", "current_job", "current_contract",
        "traveling_with", "location_at_chapter_start", "sleep_status", "current_task",
        "weapon_carry", "carries_item", "carries_equipment", "carries_weapon",
        "injury", "injury_location", "shoulder_injury",
    };

    /// <summary>True when <paramref name="predicate"/> records momentary state, so a differing
    /// later value supersedes rather than contradicts. Public because the same exclusion has to
    /// apply everywhere the ledger is consumed as "established canon" — notably
    /// <c>ContinuityEnforcer</c>'s post-generation check and <c>ProseWriterRouter</c>'s
    /// ESTABLISHED CANON prompt block, where feeding a stale <c>location_current</c> would
    /// actively instruct the model to put the character in the wrong place.</summary>
    public static bool IsVolatilePredicate(string? predicate) =>
        VolatilePredicates.Contains(NormalizePredicateKey(predicate));

    /// <summary>
    /// Predicate name reduced to a comparison key: lower-cased, and <c>-</c>/space folded to
    /// <c>_</c> so <c>life_status</c>, <c>life status</c> and <c>life-status</c> are one predicate.
    ///
    /// <para>Added 2026-09-04 — the plain <see cref="Normalize"/> these lookups used before only
    /// lower-cased and collapsed whitespace, so a hyphenated or spaced variant silently missed the
    /// volatile exemption entirely. <c>PredicateExclusionService.NormalizePredicate</c> has folded
    /// separators since it shipped, for exactly this reason ("an axiom that silently misses because
    /// of a separator would look identical to no axiom at all"); the fact ledger simply never got
    /// the same treatment. Every entry in both lists is already underscore-form, so this only ever
    /// ADDS matches — nothing that matched before stops matching.</para>
    /// </summary>
    internal static string NormalizePredicateKey(string? predicate)
    {
        if (string.IsNullOrWhiteSpace(predicate)) return "";
        var t = predicate.ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        return System.Text.RegularExpressions.Regex.Replace(t, "_+", "_").Trim('_');
    }

    /// <summary>
    /// Predicate FAMILIES that are set-valued: one subject legitimately has many values at once,
    /// so a second differing value is an addition to a set, not a disagreement about a fact.
    ///
    /// <para><b>Measured 2026-09-04, and this is a cardinality bug, not a tuning knob.</b> The
    /// contradiction rule assumes every predicate is single-valued — one predicate, one value,
    /// forever. Extraction does not write claims that way. A survey of the 1,316 live contradiction
    /// groups across BCODA/DWIACE/VATD found <b>250 groups (950 claim rows)</b> that were nothing
    /// but this: <c>Kyle → ability → "ballistic precognition"</c> against <c>Kyle → ability →
    /// "neuretics"</c>, or <c>Pixel → action → "wired a beacon"</c> against <c>Pixel → action →
    /// "singing"</c>. A character having two abilities is not the book contradicting itself, and
    /// no amount of triage makes those rows into defects.</para>
    ///
    /// <para><b>Distinct from <see cref="VolatilePredicates"/>, which is about TIME.</b> A volatile
    /// predicate has one value that changes as the story moves (<c>location_current</c>). A
    /// set-valued predicate has many values that are all true simultaneously (<c>ability</c>).
    /// <c>action</c> happens to be both; <c>ability</c> is only the latter, which is why one list
    /// could not have covered both.</para>
    ///
    /// <para>Matched as an anchored prefix family (<c>action</c> covers <c>action_taken</c>,
    /// <c>action_final</c>, <c>action_during_dark_period</c>) but never as a bare substring, for
    /// the same reason the exclusion axioms use anchored families: a substring match would quietly
    /// widen the exemption past what anyone approved, and an exemption that is too broad hides
    /// real contradictions instead of merely creating noise. Deliberately conservative —
    /// <c>weapon_type</c> and <c>occupation</c> are NOT here, because they are single-valued on
    /// the entities that matter even though a careless reading would call them plural.</para>
    /// </summary>
    private static readonly HashSet<string> SetValuedPredicateFamilies = new(StringComparer.Ordinal)
    {
        "action", "ability", "abilities", "skill", "skills", "knowledge",
        "possession", "possessions", "possesses", "carries", "equipment", "gear",
        "relationship", "relationships", "interaction", "observation", "habit", "habits",
        "capability", "capabilities", "specialization", "specializations",
    };

    /// <summary>True when the predicate belongs to a <see cref="SetValuedPredicateFamilies"/>
    /// family — <c>ability</c>, <c>ability_neuretics</c>, <c>action_taken</c>, but never
    /// <c>abilityish</c> or <c>reaction</c>.</summary>
    public static bool IsSetValuedPredicate(string? predicate)
    {
        var n = NormalizePredicateKey(predicate);
        if (n.Length == 0) return false;
        if (SetValuedPredicateFamilies.Contains(n)) return true;
        var cut = n.IndexOf('_');
        return cut > 0 && SetValuedPredicateFamilies.Contains(n[..cut]);
    }

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zero"] = 0, ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
        ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
        ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14, ["fifteen"] = 15,
        ["sixteen"] = 16, ["seventeen"] = 17, ["eighteen"] = 18, ["nineteen"] = 19,
        ["twenty"] = 20, ["thirty"] = 30, ["forty"] = 40, ["fifty"] = 50,
        ["sixty"] = 60, ["seventy"] = 70, ["eighty"] = 80, ["ninety"] = 90,
    };

    private static readonly string[] NumericUnitSuffixes =
        [" years old", " years", " year", " yrs", " yr"];

    /// <summary>
    /// Parses digit forms ("50"), number-words ("fifty"), and compound number-words
    /// ("fifty-nine" / "fifty nine") in the 0-99 range this project's ages/tenures need.
    /// Strips a trailing unit word ("years old", "years", "yr") first so a snippet-grounded
    /// extraction like "fifty-nine years" still parses. Not a general NLP number parser —
    /// scoped exactly to what continuity claims about ages/tenures actually look like.
    /// </summary>
    internal static bool TryParseNumericValue(string? raw, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim().ToLowerInvariant();
        foreach (var unit in NumericUnitSuffixes)
        {
            if (s.EndsWith(unit, StringComparison.Ordinal)) { s = s[..^unit.Length].Trim(); break; }
        }

        if (int.TryParse(s, out value)) return true;

        var parts = s.Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && NumberWords.TryGetValue(parts[0], out var single))
        {
            value = single;
            return true;
        }
        if (parts.Length == 2
            && NumberWords.TryGetValue(parts[0], out var tens) && tens is >= 20 and <= 90
            && NumberWords.TryGetValue(parts[1], out var ones) && ones is >= 1 and <= 9)
        {
            value = tens + ones;
            return true;
        }
        return false;
    }

    /// <summary>
    /// True when two claim Object values represent the same fact. For predicates in
    /// <see cref="NumericPredicates"/>, compares parsed numeric value when both sides parse
    /// (so "fifty" == "50"); falls back to the original ToLower/Trim string-equality semantics
    /// otherwise — non-numeric predicates and unparseable numeric-predicate values behave
    /// exactly as before this change.
    /// </summary>
    /// <summary>
    /// True when two claims are the SAME assertion re-extracted, rather than two competing facts:
    /// same source chapter, same underlying sentence, only the model's paraphrase of it differs.
    ///
    /// Re-running extraction over unchanged prose (which every beat save does) rephrases objects
    /// freely — "considers the sound criminal" one pass, "calls the sound criminal" the next, off
    /// the byte-identical snippet. Because <c>ComputeClaimUid</c> hashes the object, the reworded
    /// version lands as a new row on the same (entity, predicate), and the old test flagged the
    /// pair CONTRADICTED. That put pure paraphrase duplicates in front of a human as continuity
    /// contradictions and, worse, blocked point 2 of the docs/LOGIC.md §9 publish gate, which
    /// requires zero open CONTRADICTED claims. Observed live 2026-08-24: 3 of 3 open
    /// contradictions corpus-wide were this, 2 of them from a byte-identical snippet.
    ///
    /// Two genuinely contradicting facts essentially never come from the identical snippet in the
    /// identical chapter — a contradiction needs two different pieces of text saying different
    /// things — so this is a safe discriminator rather than a heuristic that could mask a real
    /// conflict. Claims with no recorded snippet are never matched this way.
    /// </summary>
    internal static bool IsSameAssertion(ContinuityClaim a, ContinuityClaim b)
    {
        if (!string.IsNullOrWhiteSpace(a.Snippet) && !string.IsNullOrWhiteSpace(b.Snippet)
            && string.Equals(a.SourceChapterId ?? "", b.SourceChapterId ?? "", StringComparison.OrdinalIgnoreCase)
            && NormalizeSnippet(a.Snippet) == NormalizeSnippet(b.Snippet))
            return true;

        // Widened 2026-09-04. The snippet-identity rule above only ever caught re-extraction of
        // the SAME sentence, so two different sentences wording one fact still read as a
        // contradiction. Measured across the 1,316 live groups in BCODA/DWIACE/VATD: 296 groups
        // (629 rows) were pure paraphrase and another 361 were partly so — "rebuilt the bike" vs
        // "rebuilds bike", "can read events ahead of time" vs "can read events ahead of time,
        // provides tactical advantage". Those are one assertion recorded twice.
        return ObjectsSayTheSameThing(a.Object, b.Object);
    }

    /// <summary>
    /// True when two object strings are the same assertion in different words: one wholly contains
    /// the other, or their wording overlaps almost completely.
    ///
    /// <para><b>The threshold is deliberately severe, and the asymmetry is the reason.</b> A false
    /// "same assertion" HIDES a real contradiction — the failure this ledger exists to prevent. A
    /// false contradiction merely costs someone a triage decision. So this only fires on
    /// near-identical wording (<see cref="SameAssertionOverlap"/> = 0.75), and complementary
    /// facets are deliberately left to a human: "red hair in loose braid" against "dark red hair"
    /// scores 0.33 and stays on the pile, as it should — deciding those is an author's call about
    /// the story, not a string comparison.</para>
    /// </summary>
    internal static bool ObjectsSayTheSameThing(string? a, string? b)
    {
        var x = NormalizeForCompare(a);
        var y = NormalizeForCompare(b);
        if (x.Length == 0 || y.Length == 0) return false;
        if (x == y) return true;

        // Subsumption: one is the other plus detail. Guarded by a length floor so a two-word
        // object is not swallowed by every longer string that happens to contain it.
        var shorter = x.Length <= y.Length ? x : y;
        var longer = x.Length <= y.Length ? y : x;
        if (shorter.Length >= 12 && longer.Contains(shorter, StringComparison.Ordinal)) return true;

        var ta = x.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var tb = y.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        if (ta.Count < 2 || tb.Count < 2) return false;
        var union = ta.Count + tb.Count - ta.Count(tb.Contains);
        return union > 0 && (double)ta.Count(tb.Contains) / union >= SameAssertionOverlap;
    }

    private const double SameAssertionOverlap = 0.75;

    /// <summary>Lower-cased, punctuation-stripped, whitespace-collapsed — so "rebuilds bike." and
    /// "Rebuilds  bike" compare equal without a stemmer's guesswork.</summary>
    private static string NormalizeForCompare(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9 ]+", " ");
        return System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim();
    }

    /// <summary>Lower-case, collapse whitespace, drop surrounding quotes and trailing sentence
    /// punctuation — so the same sentence quoted with or without its full stop still matches.</summary>
    internal static string NormalizeSnippet(string s)
    {
        var t = System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();
        t = t.Trim('"', '“', '”', '\'');
        return t.TrimEnd('.', ',', ';', ':', '!', '?', ' ');
    }

    internal static bool ObjectsMatch(string predicate, string a, string b)
    {
        if (NumericPredicates.Contains(Normalize(predicate))
            && TryParseNumericValue(a, out var na) && TryParseNumericValue(b, out var nb))
            return na == nb;
        return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Canonical string form of an Object value for hashing/uid purposes — the parsed
    /// integer for a numeric predicate (so "fifty" and "50" collapse to one claim instead of
    /// two that then falsely contradict each other), else the same Normalize used elsewhere.</summary>
    private static string NormalizeObjectForUid(string predicate, string objectValue)
        => NumericPredicates.Contains(Normalize(predicate)) && TryParseNumericValue(objectValue, out var n)
            ? n.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : Normalize(objectValue);

    // ── Upsert ──────────────────────────────────────────────────────────────

    public ClaimUpsertResult Upsert(ContinuityClaim incoming)
    {
        if (string.IsNullOrEmpty(incoming.ClaimUid))
            incoming.ClaimUid = ComputeClaimUid(incoming.EntityId, incoming.Predicate, incoming.Object);

        var now = DateTime.UtcNow.ToString("o");
        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();

        var existing = db.ContinuityClaims.FirstOrDefault(c => c.ClaimUid == incoming.ClaimUid);

        if (existing != null && IsActive(existing.Status))
        {
            existing.Status          = existing.Status == "NEW" ? "CONFIRMED" : existing.Status;
            existing.LastConfirmedAt = now;
            existing.ExtractedBy     = MergeExtractors(existing.ExtractedBy, incoming.ExtractedBy);
            db.SaveChanges();

            RecordConfirmation(db, incoming.ClaimUid, incoming.SourceChapterId, incoming.SourcePath, now);
            db.SaveChanges();
            tx.Commit();
            return new ClaimUpsertResult { Outcome = "CONFIRMED", Claim = existing };
        }

        // Existing but inactive (REJECTED/SUPERSEDED) — reactivate rather than re-insert (PK collision).
        if (existing != null)
        {
            existing.Status          = "NEW";
            existing.LastConfirmedAt = now;
            existing.ExtractedBy     = MergeExtractors(existing.ExtractedBy, incoming.ExtractedBy);
            db.SaveChanges();

            RecordConfirmation(db, incoming.ClaimUid, incoming.SourceChapterId, incoming.SourcePath, now);
            db.SaveChanges();
            tx.Commit();
            return new ClaimUpsertResult { Outcome = "NEW", Claim = existing };
        }

        // Look for a different-object claim on the same (entity, predicate). CANONICAL claims
        // ARE included here (unlike earlier): a fact that's been resolved and made canonical is
        // exactly the one thing a new, silently-drifting extraction must be checked against —
        // excluding it meant a post-resolution contradiction was inserted as plain "NEW" and
        // never surfaced anywhere, defeating the point of resolving it in the first place.
        //
        // 2026-08-14: the object-mismatch test moved out of SQL and into ObjectsMatch (client-side,
        // after materializing the candidate set) so numeric-predicate parsing ("fifty" == "50")
        // can apply — that comparison isn't EF-translatable. The candidate set is bounded (all
        // live claims for one entity+predicate, typically a handful of rows), so this is cheap.
        // Any row with the SAME raw object as incoming would already have matched ComputeClaimUid
        // and been handled by the `existing` branch above, so every row here is guaranteed to
        // have a different raw object — this only changes HOW the mismatch is judged, not which
        // rows are candidates.
        // A volatile predicate records where/how the character was at one moment; a later,
        // different value is the character having moved on, not the prose contradicting canon.
        // Skipping conflict detection here means the new claim lands as NEW rather than
        // CONTRADICTED — see VolatilePredicates for why this matters to every sequel.
        // Set-valued predicates join volatile ones in skipping conflict detection entirely
        // (2026-09-04): a second `ability` or `action` is another member of a set, and marking it
        // CONTRADICTED asserts a disagreement that was never claimed.
        var conflict = IsVolatilePredicate(incoming.Predicate) || IsSetValuedPredicate(incoming.Predicate)
            ? null
            : db.ContinuityClaims
            .Where(c => c.EntityId == incoming.EntityId
                     && c.Predicate == incoming.Predicate
                     && c.Status != "REJECTED" && c.Status != "SUPERSEDED")
            .OrderByDescending(c => c.Status == "CANONICAL" ? 1 : 0).ThenByDescending(c => c.LastConfirmedAt)
            .ToList()
            .FirstOrDefault(c => !ObjectsMatch(incoming.Predicate, c.Object, incoming.Object)
                              && !IsSameAssertion(c, incoming));

        incoming.Status          = conflict != null ? "CONTRADICTED" : "NEW";
        incoming.FirstAssertedAt = now;
        incoming.LastConfirmedAt = now;
        db.ContinuityClaims.Add(incoming);
        db.SaveChanges();

        RecordConfirmation(db, incoming.ClaimUid, incoming.SourceChapterId, incoming.SourcePath, now);
        db.SaveChanges();

        if (conflict != null)
        {
            // A settled CANONICAL fact is never demoted by a new extraction contradicting it —
            // that would un-resolve something the author already settled. The NEW claim is the
            // one flagged CONTRADICTED so it surfaces for triage; canon stays canon until a
            // human explicitly resolves it again.
            if (conflict.Status != "CANONICAL")
                conflict.Status = "CONTRADICTED";
            db.SaveChanges();
            RecordContradiction(db, conflict.ClaimUid, incoming.ClaimUid, now);
            db.SaveChanges();
            tx.Commit();
            return new ClaimUpsertResult { Outcome = "CONTRADICTED", Claim = incoming, Conflict = conflict };
        }

        tx.Commit();
        return new ClaimUpsertResult { Outcome = "NEW", Claim = incoming };
    }

    private static bool IsActive(string status)
        => status != "REJECTED" && status != "SUPERSEDED";

    private static List<string> MergeExtractors(List<string>? a, List<string>? b)
    {
        var set = new HashSet<string>(a ?? new());
        foreach (var x in b ?? new()) set.Add(x);
        return set.ToList();
    }

    // ── Read methods ─────────────────────────────────────────────────────────

    public List<ContinuityClaim> GetByEntity(string entityId)
    {
        using var db = dbFactory.CreateDbContext();
        return db.ContinuityClaims
            .AsNoTracking()
            .Where(c => c.EntityId == entityId)
            .OrderBy(c => c.Predicate).ThenBy(c => c.Object)
            .ToList();
    }

    public List<ContinuityClaim> GetByStatus(string status)
    {
        using var db = dbFactory.CreateDbContext();
        return db.ContinuityClaims
            .AsNoTracking()
            .Where(c => c.Status == status)
            .OrderBy(c => c.EntityName).ThenBy(c => c.Predicate)
            .ToList();
    }

    /// <summary>
    /// Free-text search across the WHOLE ledger — entity name, predicate, and object — returning
    /// each hit's <see cref="ContinuityClaim.ClaimUid"/> so the caller can act on it.
    ///
    /// <para><b>Why this exists (2026-09-03).</b> Nothing could search claims by text. The only
    /// reads were by entity id, by status, or by applied-ness, and <c>search_universe</c> covers
    /// <c>Entities</c>, not this table. That gap has now hidden fabricated canon twice: Phase 0
    /// declared the "Dae-jung Seo" fabrication purged because <c>search_universe glmz "Dae-jung"</c>
    /// returned nothing, and Phase 2's Tuned Read then found twelve live claims still asserting it;
    /// the author's family purge then found four more that a <c>father_*</c> predicate-prefix sweep
    /// could never have matched, because they were recorded under
    /// <c>second_sword_possession → "old sword wrapped in oilcloth, made by father"</c>. A
    /// predicate-name search cannot find a fact hidden in an object string, and the ledger feeds
    /// ContinuityService's ESTABLISHED CANON prompt block — so a fabrication surviving here is told
    /// to the next beat as fact.</para>
    ///
    /// <para>Case-insensitive substring match (SQL <c>LIKE</c> under the collation the DB already
    /// uses). Rejected/superseded claims are included unless <paramref name="liveOnly"/>:
    /// verifying that a purge actually landed means being able to see the rejected rows too.</para>
    /// </summary>
    /// <param name="text">Substring to look for in EntityName, Predicate, or Object.</param>
    /// <param name="entityId">Optional — restrict to one entity's claims.</param>
    /// <param name="predicatePrefix">Optional — restrict to predicates starting with this.</param>
    /// <param name="liveOnly">Exclude REJECTED and SUPERSEDED claims.</param>
    public List<ContinuityClaim> Search(
        string text,
        string? entityId = null,
        string? predicatePrefix = null,
        bool liveOnly = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return new();

        using var db = dbFactory.CreateDbContext();
        var pattern = $"%{text.Trim()}%";
        var q = db.ContinuityClaims.AsNoTracking().Where(c =>
            EF.Functions.Like(c.EntityName, pattern)
            || EF.Functions.Like(c.Predicate, pattern)
            || EF.Functions.Like(c.Object, pattern));

        if (!string.IsNullOrWhiteSpace(entityId)) q = q.Where(c => c.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(predicatePrefix)) q = q.Where(c => c.Predicate.StartsWith(predicatePrefix));
        if (liveOnly) q = q.Where(c => c.Status != "REJECTED" && c.Status != "SUPERSEDED");

        return q.OrderBy(c => c.EntityName).ThenBy(c => c.Predicate).ThenBy(c => c.Object).ToList();
    }

    /// <summary>How much of the ledger can actually be placed on a book's clock.</summary>
    /// <param name="Anchored">Live claims carrying a <c>SourceBeatId</c>.</param>
    public sealed record BeatAnchorCoverage(int LiveClaims, int Anchored, int FromProse, int ProseAnchored);

    /// <summary>
    /// Beat-anchor coverage — the ceiling on everything the Tuned Read can do.
    ///
    /// <para><b>Why it is worth its own readout.</b> <c>SourceBeatId</c> arrived in Phase 2; every
    /// claim extracted before it is unanchored. An unanchored claim cannot be ordered against
    /// another (so no temporal axiom can ever fire on it) and cannot be shown to the adjudicator
    /// with its prose (so <c>TunedReadService.AdjudicateAsync</c> refuses the pair outright rather
    /// than ruling on two summaries). A ledger of unanchored claims therefore produces zero
    /// findings no matter how good the ontology is — and reports that as "clean", which is exactly
    /// the silence this whole programme exists to break.</para>
    /// </summary>
    public BeatAnchorCoverage GetBeatAnchorCoverage(string? bookSlug = null)
    {
        using var db = dbFactory.CreateDbContext();
        var q = QueryForSurvey(db, bookSlug, liveOnly: true);
        return new BeatAnchorCoverage(
            q.Count(),
            q.Count(c => c.SourceBeatId != null),
            q.Count(c => c.SourceType == "prose"),
            q.Count(c => c.SourceType == "prose" && c.SourceBeatId != null));
    }

    /// <summary>Outcome of <see cref="ReassessContradictionsAsync"/>.</summary>
    /// <param name="Cleared">Rows whose CONTRADICTED verdict today's rules no longer justify.</param>
    public sealed record ReassessReport(
        int Examined, int Cleared, int SetValued, int Paraphrase, int NumericSafe, int Kept);

    /// <summary>
    /// Re-runs today's conflict test over every claim already marked <c>CONTRADICTED</c> and
    /// returns those the current rules would no longer mark.
    ///
    /// <para><b>Why a status can be wrong without anything having changed.</b> A claim's status is
    /// written once, by whatever version of the rule was live at extraction time, and never
    /// revisited. Three corrections have landed since most of this corpus was extracted — the
    /// numeric-safe object comparison (2026-08-14), the volatile-predicate exemption (2026-09-01),
    /// and the set-valued/paraphrase work (2026-09-04) — and none of them reached a single
    /// existing row. So the ledger carries verdicts from rules the engine has already repudiated,
    /// and they are indistinguishable from live ones: they inflate the contradiction count, they
    /// fail books at publish-readiness gate 2, and they bury the real disagreements.</para>
    ///
    /// <para>Cleared rows go back to <c>NEW</c>, not <c>REJECTED</c>: the claim itself was never
    /// judged wrong, only the verdict about it. <c>CANONICAL</c> and <c>CONFIRMED</c> are never
    /// touched — they are not CONTRADICTED, and a human settled them.</para>
    /// </summary>
    /// <param name="apply">False (default) computes and reports without writing.</param>
    public async Task<ReassessReport> ReassessContradictionsAsync(
        string? bookSlug = null, bool apply = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var q = db.ContinuityClaims.Where(c => c.Status == "CONTRADICTED");
        if (!string.IsNullOrWhiteSpace(bookSlug)) q = q.Where(c => c.BookSlug == bookSlug);
        var suspects = await q.ToListAsync(ct);
        if (suspects.Count == 0) return new ReassessReport(0, 0, 0, 0, 0, 0);

        // Every live sibling on the same (entity, predicate) — the set the original conflict test
        // was run against.
        var keys = suspects.Select(c => c.EntityId).Distinct().ToList();
        var siblings = (await db.ContinuityClaims.AsNoTracking()
                .Where(c => keys.Contains(c.EntityId)
                         && c.Status != "REJECTED" && c.Status != "SUPERSEDED")
                .ToListAsync(ct))
            .GroupBy(c => (c.EntityId, Predicate: Normalize(c.Predicate)))
            .ToDictionary(g => g.Key, g => g.ToList());

        int setValued = 0, paraphrase = 0, numericSafe = 0, kept = 0;
        var cleared = new List<ContinuityClaim>();

        foreach (var c in suspects)
        {
            if (IsVolatilePredicate(c.Predicate) || IsSetValuedPredicate(c.Predicate))
            { setValued++; cleared.Add(c); continue; }

            var peers = siblings.TryGetValue((c.EntityId, Normalize(c.Predicate)), out var s)
                ? s.Where(p => p.ClaimUid != c.ClaimUid).ToList()
                : [];

            // Does ANY peer still genuinely disagree under today's rules?
            var conflicting = peers.FirstOrDefault(p =>
                !ObjectsMatch(c.Predicate, p.Object, c.Object) && !IsSameAssertion(p, c));

            if (conflicting != null) { kept++; continue; }

            if (peers.Any(p => ObjectsMatch(c.Predicate, p.Object, c.Object))) numericSafe++;
            else paraphrase++;
            cleared.Add(c);
        }

        if (apply && cleared.Count > 0)
        {
            var stamp = DateTime.UtcNow.ToString("O");
            foreach (var c in cleared)
            {
                c.Status = "NEW";
                c.ExclusionRuleId = null;
                c.ResolutionNote =
                    "Contradiction verdict cleared: re-assessed under the current rules "
                    + "(numeric-safe comparison, volatile + set-valued exemptions, paraphrase "
                    + $"detection) and no live sibling still disagrees. {stamp}";
            }
            await db.SaveChangesAsync(ct);
        }

        return new ReassessReport(suspects.Count, cleared.Count, setValued, paraphrase, numericSafe, kept);
    }

    // ── predicate vocabulary survey (2026-09-03) ────────────────────────────────

    /// <summary>One predicate FAMILY — the stem an exclusion axiom's <c>stem*</c> pattern
    /// addresses (<c>father</c> covers father, father_name, father_occupation …).</summary>
    /// <param name="Members">The distinct full predicate names in this family, most common first.</param>
    public sealed record PredicateFamilyStat(
        string Family, int Claims, int Entities, IReadOnlyList<string> Members, string SampleObject);

    /// <summary>Two predicate families held by the same entity. An exclusion axiom can only ever
    /// fire on a pair that actually co-occurs, so this is the candidate list an author picks
    /// axioms FROM — the alternative being to invent a pattern and discover it matches nothing.</summary>
    public sealed record PredicateCoOccurrence(
        string FamilyA, string FamilyB, int Entities, string SampleEntity);

    /// <summary>
    /// The ledger's actual predicate vocabulary, grouped into the families
    /// <c>PredicateExclusionService.PredicateMatchesPattern</c>'s <c>stem*</c> form addresses.
    ///
    /// <para><b>Why this exists.</b> The exclusion ontology is only as good as its authors'
    /// knowledge of what the ledger really records, and nothing reported that. The axioms shipped
    /// in Phase 2 named <c>father</c> when extraction had actually written <c>father_name</c>,
    /// <c>father_occupation</c>, <c>father_status</c> and eleven more — a rule that silently
    /// matches nothing is indistinguishable from no rule, and that near-miss was caught only by
    /// dry-running the instrument against the one defect it was built for. Authoring the next
    /// axiom from a guess instead of from this list would repeat it.</para>
    /// </summary>
    public List<PredicateFamilyStat> GetPredicateFamilies(
        string? bookSlug = null, bool liveOnly = true, int minClaims = 1)
    {
        using var db = dbFactory.CreateDbContext();
        var rows = QueryForSurvey(db, bookSlug, liveOnly)
            .Select(c => new { c.EntityId, c.Predicate, c.Object })
            .ToList();

        return rows
            .Select(r => new { Family = FamilyStem(r.Predicate), r.EntityId, r.Predicate, r.Object })
            .Where(r => r.Family.Length > 0)
            .GroupBy(r => r.Family, StringComparer.Ordinal)
            .Select(g => new PredicateFamilyStat(
                g.Key,
                g.Count(),
                g.Select(x => x.EntityId).Distinct(StringComparer.Ordinal).Count(),
                g.GroupBy(x => x.Predicate, StringComparer.Ordinal)
                    .OrderByDescending(m => m.Count()).ThenBy(m => m.Key, StringComparer.Ordinal)
                    .Select(m => m.Key).Take(8).ToList(),
                g.Select(x => x.Object).FirstOrDefault(o => !string.IsNullOrWhiteSpace(o)) ?? ""))
            .Where(f => f.Claims >= minClaims)
            .OrderByDescending(f => f.Entities).ThenByDescending(f => f.Claims)
            .ThenBy(f => f.Family, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Predicate-family pairs held by the same entity, ranked by how many entities hold both.
    /// This is the empirical answer to "which exclusion axioms could fire at all" — a pair with
    /// zero co-occurrence cannot produce a candidate no matter how sound the axiom is.
    /// </summary>
    /// <param name="familyFilter">When set, only pairs involving this family (e.g. "death").</param>
    public List<PredicateCoOccurrence> GetPredicateCoOccurrences(
        string? bookSlug = null, string? familyFilter = null, int minEntities = 2, bool liveOnly = true)
    {
        using var db = dbFactory.CreateDbContext();
        var rows = QueryForSurvey(db, bookSlug, liveOnly)
            .Select(c => new { c.EntityId, c.EntityName, c.Predicate })
            .ToList();

        var filter = string.IsNullOrWhiteSpace(familyFilter) ? null : FamilyStem(familyFilter);
        var pairs = new Dictionary<(string A, string B), (int Count, string Sample)>();

        foreach (var entity in rows.GroupBy(r => r.EntityId, StringComparer.Ordinal))
        {
            var families = entity.Select(r => FamilyStem(r.Predicate))
                .Where(f => f.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();
            var name = entity.Select(r => r.EntityName).FirstOrDefault() ?? entity.Key;

            for (var i = 0; i < families.Count; i++)
                for (var j = i + 1; j < families.Count; j++)
                {
                    if (filter != null && families[i] != filter && families[j] != filter) continue;
                    var key = (families[i], families[j]);
                    pairs[key] = pairs.TryGetValue(key, out var cur)
                        ? (cur.Count + 1, cur.Sample)
                        : (1, name);
                }
        }

        return pairs
            .Where(p => p.Value.Count >= minEntities)
            .Select(p => new PredicateCoOccurrence(p.Key.A, p.Key.B, p.Value.Count, p.Value.Sample))
            .OrderByDescending(p => p.Entities)
            .ThenBy(p => p.FamilyA, StringComparer.Ordinal).ThenBy(p => p.FamilyB, StringComparer.Ordinal)
            .ToList();
    }

    private static IQueryable<ContinuityClaim> QueryForSurvey(
        ProseDbContext db, string? bookSlug, bool liveOnly)
    {
        var q = db.ContinuityClaims.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(bookSlug)) q = q.Where(c => c.BookSlug == bookSlug);
        if (liveOnly) q = q.Where(c => c.Status != "REJECTED" && c.Status != "SUPERSEDED");
        return q;
    }

    /// <summary>The leading token of a normalized predicate — the stem an axiom's <c>stem*</c>
    /// pattern anchors on. <c>father_occupation</c> → <c>father</c>; <c>life_status</c> →
    /// <c>life</c>; a single-token predicate is its own family.</summary>
    internal static string FamilyStem(string? predicate)
    {
        var n = Normalize(predicate ?? "");
        if (n.Length == 0) return "";
        var cut = n.IndexOf('_');
        return cut > 0 ? n[..cut] : n;
    }

    /// <summary>Every claim that has been applied back to its entity's canon record
    /// (<see cref="ContinuityApplyService.ApplyAsync"/> sets <c>AppliedAt</c>/<c>AppliedToField</c>) —
    /// the candidate set for <see cref="ContinuityApplyService.CheckAppliedClaimsAsync"/>'s drift
    /// check. Optionally scoped to one book via <see cref="ContinuityClaim.BookSlug"/>.</summary>
    public List<ContinuityClaim> GetAppliedClaims(string? bookSlug = null)
    {
        using var db = dbFactory.CreateDbContext();
        var q = db.ContinuityClaims.AsNoTracking().Where(c => c.AppliedAt != null);
        if (!string.IsNullOrEmpty(bookSlug)) q = q.Where(c => c.BookSlug == bookSlug);
        return q.OrderBy(c => c.EntityName).ThenBy(c => c.Predicate).ToList();
    }

    /// <summary>Whether any claim has ever been extracted and tagged with this book's slug —
    /// lets a per-book caller (BookHealthService's fact-ledger check) distinguish "extracted and
    /// clean" from "never extracted," the same honest-gap distinction SacredFlawAsync's
    /// no-pov-data finding already makes for a different check.</summary>
    public bool HasAnyClaimsForBook(string bookSlug)
    {
        using var db = dbFactory.CreateDbContext();
        return db.ContinuityClaims.AsNoTracking().Any(c => c.BookSlug == bookSlug);
    }

    /// <summary>
    /// Count of CONTRADICTED claims still awaiting resolution. Used by the
    /// inbox badge in the top nav so users see how many contradictions are
    /// outstanding without opening /continuity. Resolving a pair (via the
    /// /continuity page → "Pick A / Pick B / Custom") moves both claims out
    /// of CONTRADICTED status, dropping the count.
    /// </summary>
    public int CountUnresolvedContradictions()
    {
        using var db = dbFactory.CreateDbContext();
        return db.ContinuityClaims.Count(c => c.Status == "CONTRADICTED");
    }

    public List<ContradictionPair> GetContradictions()
    {
        using var db = dbFactory.CreateDbContext();
        var edges = db.ClaimContradictions.AsNoTracking().ToList();
        if (edges.Count == 0) return [];
        var allUids = edges.SelectMany(e => new[] { e.AUid, e.BUid }).Distinct().ToList();
        var claimMap = db.ContinuityClaims.AsNoTracking()
            .Where(c => allUids.Contains(c.ClaimUid))
            .ToDictionary(c => c.ClaimUid);
        var pairs = new List<ContradictionPair>();
        foreach (var e in edges)
        {
            if (!claimMap.TryGetValue(e.AUid, out var a) || !claimMap.TryGetValue(e.BUid, out var b)) continue;
            if (a.Status is "REJECTED" or "SUPERSEDED" || b.Status is "REJECTED" or "SUPERSEDED") continue;
            if (a.Status != "CONTRADICTED" && b.Status != "CONTRADICTED") continue;
            pairs.Add(new ContradictionPair { A = a, B = b });
        }
        return pairs;
    }

    /// <param name="bookSlug">When provided, restricts the sweep to (entity, predicate) keys
    /// where at least one live claim carries this <see cref="ContinuityClaim.BookSlug"/> — lets a
    /// per-book caller (e.g. BookHealthService's fact-ledger check) see only its own book's
    /// contradictions instead of the whole corpus. Null (default) preserves the original
    /// corpus-wide behavior for existing callers (the /continuity UI, ContinuityLongSweepService).
    /// Entity-record-sourced claims never carry a BookSlug, so a bookSlug-filtered call can still
    /// surface a contradiction between a prose claim (tagged) and an entity-record claim
    /// (untagged) as long as the prose side matches — the group isn't restricted away entirely,
    /// just which keys get considered.</param>
    /// <param name="excludeVolatile">Default true: drop <see cref="IsVolatilePredicate"/> keys
    /// before grouping, same as every other consumer of "the ledger as established canon" (2026-09-01).
    /// Pass false only for an operator-directed single-group lookup (TrinityReconciliationService's
    /// <c>--only-entity</c>/<c>--only-predicate</c>) — there the operator named this exact
    /// (entity, predicate) deliberately and expects the raw group back, volatile or not.</param>
    public List<ContradictionGroup> GetContradictionGroups(string? bookSlug = null, bool excludeVolatile = true)
    {
        using var db = dbFactory.CreateDbContext();
        // CANONICAL included: a new claim contradicting an already-resolved fact is exactly
        // the case that must surface here, not be silently invisible (see Upsert's remarks).
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED", "CANONICAL" };

        var keys = db.ContinuityClaims.AsNoTracking()
            .Where(c => live.Contains(c.Status))
            .GroupBy(c => new { c.EntityId, c.Predicate })
            .Select(g => new { g.Key.EntityId, g.Key.Predicate, Variants = g.Select(x => x.Object).Distinct().Count() })
            .Where(g => g.Variants > 1)
            .ToList();

        if (excludeVolatile)
            // 2026-09-01: this exclusion already applies to the prompt-time canon block
            // (ProseWriterRouter's ESTABLISHED CANON section) and to the enforcer's
            // per-beat check — it was never applied here, so a moment-state predicate
            // (location_current, companions, ...) that legitimately differs beat to beat
            // still landed as a permanent FACT-LEDGER contradiction. Same root cause,
            // same fix, applied where it was missing.
            // Set-valued keys go with them (2026-09-04): a key with several `ability` values is
            // several abilities, and surfacing it as a contradiction group buried the real ones
            // under 250 groups of noise in three books.
            keys = keys.Where(k => !IsVolatilePredicate(k.Predicate) && !IsSetValuedPredicate(k.Predicate))
                .ToList();

        if (!string.IsNullOrEmpty(bookSlug))
        {
            // Restrict to keys that have at least one claim tagged with this book's slug.
            // Resolved as a separate query and intersected client-side — EF can't translate
            // Contains() over a client-side HashSet of composite keys.
            var bookKeys = db.ContinuityClaims.AsNoTracking()
                .Where(c => live.Contains(c.Status) && c.BookSlug == bookSlug)
                .Select(c => new { c.EntityId, c.Predicate })
                .Distinct()
                .ToList()
                .Select(k => (k.EntityId, k.Predicate))
                .ToHashSet();
            keys = keys.Where(k => bookKeys.Contains((k.EntityId, k.Predicate))).ToList();
        }

        // One query for every key's claims, not one query per key (2026-09-04). The N+1 shape was
        // survivable while this ran on a handful of keys; on BCODA it is ~500 round-trips and the
        // command started timing out past ten minutes, which also stalls FactLedgerAsync (DEEP
        // tier) and the group adjudicator, both of which call this method. Filtering by EntityId
        // and re-checking the predicate client-side keeps the parameter list small — SQL Server
        // has no composite-key IN, and a 500-clause OR is worse than the fetch.
        var wantedEntityIds = keys.Select(k => k.EntityId).Distinct().ToList();
        var wantedKeys = keys.Select(k => (k.EntityId, k.Predicate)).ToHashSet();
        var claimsByKey = db.ContinuityClaims.AsNoTracking()
            .Where(c => wantedEntityIds.Contains(c.EntityId) && live.Contains(c.Status))
            .ToList()
            .Where(c => wantedKeys.Contains((c.EntityId, c.Predicate)))
            .GroupBy(c => (c.EntityId, c.Predicate))
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.FirstAssertedAt).ToList());

        var groups = new List<ContradictionGroup>();
        foreach (var k in keys)
        {
            if (!claimsByKey.TryGetValue((k.EntityId, k.Predicate), out var claims)) continue;
            // The SQL Distinct() above counts variants by exact string, so a key whose only
            // "disagreement" is rewording still reached here (2026-09-04). Collapse the members
            // into genuinely distinct assertions before deciding this is a group at all —
            // otherwise the same fact recorded twice is filed as a contradiction, which is what
            // put ~300 pure-paraphrase groups in front of a human across three books.
            var distinct = new List<ContinuityClaim>();
            foreach (var c in claims)
                if (!distinct.Any(d => ObjectsMatch(k.Predicate, d.Object, c.Object)
                                    || ObjectsSayTheSameThing(d.Object, c.Object)))
                    distinct.Add(c);

            if (claims.Count >= 2 && distinct.Count >= 2)
                groups.Add(new ContradictionGroup
                {
                    EntityId   = k.EntityId,
                    EntityName = claims[0].EntityName,
                    EntityKind = claims[0].EntityKind,
                    Predicate  = k.Predicate,
                    Claims     = claims,
                });
        }
        return groups;
    }

    /// <summary>
    /// Incremental variant of <see cref="GetContradictionGroups"/>. Only re-evaluates
    /// (entity, predicate) tuples whose claims have been touched since
    /// <paramref name="sinceUtc"/>, which is the watermark step in the playbook
    /// from <c>project_continuity_sync_architecture</c>.
    ///
    /// LastConfirmedAt is bumped on every Upsert; FirstAssertedAt is set on insert.
    /// Between them they cover any change that could newly introduce a variant.
    /// Returns groups in the same shape as the full sweep, but a key not touched
    /// since the watermark is silently absent (it can't have changed).
    /// </summary>
    public List<ContradictionGroup> GetContradictionGroupsSince(DateTime sinceUtc)
    {
        using var db = dbFactory.CreateDbContext();
        // CANONICAL included — see GetContradictionGroups' remarks.
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED", "CANONICAL" };
        // ISO-8601 "o" format is lexicographically sortable so direct string
        // comparison in SQL is safe — no DateTime parse round-trip needed.
        var sinceIso = sinceUtc.ToUniversalTime().ToString("o");

        var touchedKeys = db.ContinuityClaims.AsNoTracking()
            .Where(c => live.Contains(c.Status) &&
                        (c.LastConfirmedAt.CompareTo(sinceIso) >= 0 ||
                         c.FirstAssertedAt.CompareTo(sinceIso) >= 0))
            .Select(c => new { c.EntityId, c.Predicate })
            .Distinct()
            .ToList()
            // 2026-09-01: same exclusion as GetContradictionGroups — see its remarks.
            .Where(k => !IsVolatilePredicate(k.Predicate))
            .ToList();

        if (touchedKeys.Count == 0) return new List<ContradictionGroup>();

        var groups = new List<ContradictionGroup>();
        foreach (var k in touchedKeys)
        {
            // Pull every live claim for this (entity, predicate) — a new claim
            // can contradict an arbitrarily-old one, so the variant check has
            // to see the full set, not just the recent additions.
            var claims = db.ContinuityClaims.AsNoTracking()
                .Where(c => c.EntityId == k.EntityId && c.Predicate == k.Predicate && live.Contains(c.Status))
                .OrderBy(c => c.FirstAssertedAt)
                .ToList();
            if (claims.Count >= 2 && claims.Select(c => c.Object).Distinct().Count() > 1)
                groups.Add(new ContradictionGroup
                {
                    EntityId   = k.EntityId,
                    EntityName = claims[0].EntityName,
                    EntityKind = claims[0].EntityKind,
                    Predicate  = k.Predicate,
                    Claims     = claims,
                });
        }
        return groups;
    }

    public ContinuityStats GetStats()
    {
        using var db = dbFactory.CreateDbContext();
        var rows = db.ContinuityClaims.AsNoTracking()
            .GroupBy(c => 1)
            .Select(g => new
            {
                Total            = g.Count(),
                New              = g.Sum(c => c.Status == "NEW"          ? 1 : 0),
                Confirmed        = g.Sum(c => c.Status == "CONFIRMED"    ? 1 : 0),
                Contradicted     = g.Sum(c => c.Status == "CONTRADICTED" ? 1 : 0),
                Canonical        = g.Sum(c => c.Status == "CANONICAL"    ? 1 : 0),
                Rejected         = g.Sum(c => c.Status == "REJECTED"     ? 1 : 0),
                Superseded       = g.Sum(c => c.Status == "SUPERSEDED"   ? 1 : 0),
                FromProse        = g.Sum(c => c.SourceType == "prose"         ? 1 : 0),
                FromEntityRecord = g.Sum(c => c.SourceType == "entity_record" ? 1 : 0),
                FromBible        = g.Sum(c => c.SourceType == "outline"         ? 1 : 0),
            })
            .FirstOrDefault();

        if (rows == null) return new ContinuityStats();
        return new ContinuityStats
        {
            Total            = rows.Total,
            New              = rows.New,
            Confirmed        = rows.Confirmed,
            Contradicted     = rows.Contradicted,
            Canonical        = rows.Canonical,
            Rejected         = rows.Rejected,
            Superseded       = rows.Superseded,
            FromProse        = rows.FromProse,
            FromEntityRecord = rows.FromEntityRecord,
            FromBible        = rows.FromBible,
        };
    }

    // ── Resolution ───────────────────────────────────────────────────────────

    public ResolveResult Resolve(string aUid, string bUid, string winner, string customObject = "", string note = "")
    {
        winner = (winner ?? "").Trim().ToLowerInvariant();
        if (winner != "a" && winner != "b" && winner != "custom")
            throw new ArgumentException("winner must be A | B | custom");
        if (winner == "custom" && string.IsNullOrWhiteSpace(customObject))
            throw new ArgumentException("custom resolution requires customObject");

        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();

        var a = db.ContinuityClaims.FirstOrDefault(c => c.ClaimUid == aUid)
            ?? throw new InvalidOperationException($"ContinuityClaim A not found: {aUid}");
        var b = db.ContinuityClaims.FirstOrDefault(c => c.ClaimUid == bUid)
            ?? throw new InvalidOperationException($"ContinuityClaim B not found: {bUid}");
        if (a.EntityId != b.EntityId)
            throw new InvalidOperationException("ContinuityClaims belong to different entities — cannot resolve as one contradiction");

        var now = DateTime.UtcNow.ToString("o");

        if (winner == "a" || winner == "b")
        {
            var win  = winner == "a" ? a : b;
            var lose = winner == "a" ? b : a;
            ApplyStatus(win,  "CANONICAL", now, note);
            ApplyStatus(lose, "REJECTED",  now, note);
            db.SaveChanges();
            tx.Commit();
            return new ResolveResult { Winner = win, Loser = lose };
        }

        // A custom object that happens to normalize to the same text as one of the two
        // contested claims hashes to that claim's own ClaimUid (ComputeClaimUid is a pure
        // function of entity+predicate+object). Inserting a "new" row under that UID would
        // collide with the already-tracked `a`/`b` entity and throw. Treat it as picking
        // that side outright instead of fabricating a duplicate.
        var customUid = ComputeClaimUid(a.EntityId, a.Predicate, customObject);
        if (customUid == a.ClaimUid || customUid == b.ClaimUid)
        {
            var win  = customUid == a.ClaimUid ? a : b;
            var lose = win == a ? b : a;
            ApplyStatus(win,  "CANONICAL", now, note);
            ApplyStatus(lose, "REJECTED",  now, note);
            db.SaveChanges();
            tx.Commit();
            return new ResolveResult { Winner = win, Loser = lose };
        }

        ApplyStatus(a, "REJECTED", now, note);
        ApplyStatus(b, "REJECTED", now, note);

        var custom = new ContinuityClaim
        {
            ClaimUid        = customUid,
            EntityId        = a.EntityId,
            EntityName      = a.EntityName,
            EntityKind      = a.EntityKind,
            Predicate       = a.Predicate,
            Object          = customObject,
            // 2026-08-14: BookSlug wasn't copied from either contested claim, so a resolved
            // CANONICAL fact silently fell out of every book-scoped query (e.g. "open
            // contradictions for VIGL") even though it clearly belongs to that book — caught
            // when the Pallor resolution's own CANONICAL row came back with BookSlug=NULL.
            BookSlug        = a.BookSlug ?? b.BookSlug,
            SourceType      = "writer_assertion",
            Snippet         = $"Writer-asserted resolution of {a.Predicate} contradiction.",
            Voice           = "writer",
            Confidence      = "high",
            ExtractedBy     = new List<string> { "writer" },
            Status          = "CANONICAL",
            FirstAssertedAt = now,
            LastConfirmedAt = now,
            ResolvedAt      = now,
            ResolutionNote  = note,
        };
        db.ContinuityClaims.Add(custom);

        a.SupersededBy = custom.ClaimUid;
        b.SupersededBy = custom.ClaimUid;

        db.SaveChanges();
        tx.Commit();
        return new ResolveResult { Winner = custom, Loser = a, Loser2 = b };
    }

    /// <param name="onlyRejectClaimUids">When null (the default), rejects every other live
    /// sibling claim for the same (EntityId, Predicate) — the original blanket behavior, correct
    /// for callers that resolve a divergence purely on the ledger (no external content to edit).
    /// When provided, rejects ONLY the listed claim UIDs; any live sibling NOT in the set is left
    /// at its current status untouched. Trinity Reconciliation passes this: a losing claim whose
    /// underlying prose/bible edit was refused (snippet not found, safety guard rejected the
    /// rewrite) must NOT be marked REJECTED — the wrong fact is still sitting in its source
    /// unedited, and REJECTED would permanently hide that from ever resurfacing. Leaving it at its
    /// current live status keeps it forming a contradiction group against the now-CANONICAL
    /// winner (CANONICAL claims are deliberately included in the "live" set —
    /// <see cref="GetContradictionGroups"/> — precisely so this resurfaces on the next pass
    /// instead of silently vanishing.</param>
    /// <remarks>The sibling-demotion set includes CANONICAL, not just NEW/CONFIRMED/CONTRADICTED —
    /// an explicit call to THIS method is itself a re-resolution (a panel vote picking a new
    /// winner, or a human overriding one), which is exactly the "only a human resolving it again"
    /// case the <c>ContinuityServiceCanonicalConflictTests</c> regression suite carves out as
    /// allowed to change a CANONICAL claim's status; that suite only exercises the separate
    /// <see cref="Upsert"/> path (a fresh, unreviewed extraction), which must still never demote a
    /// canonical claim on its own and is untouched by this. Before this fix, a non-deterministic
    /// panel re-vote that flipped a winner (found live 2026-08-19: Breckenridge.background,
    /// "ex-Arcturus" vs "ex-Arcturus Defense Solutions") left TWO simultaneously-CANONICAL claims
    /// for the same key forever, since the old winner was never in the demotable set —
    /// permanently un-resolvable, resurfacing as a false contradiction on every future sweep.
    /// </remarks>
    public void MakeCanonical(string claimUid, string note = "", IReadOnlySet<string>? onlyRejectClaimUids = null)
    {
        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        var winner = db.ContinuityClaims.FirstOrDefault(c => c.ClaimUid == claimUid)
            ?? throw new InvalidOperationException($"Claim not found: {claimUid}");
        var now = DateTime.UtcNow.ToString("o");
        ApplyStatus(winner, "CANONICAL", now, note);

        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED", "CANONICAL" };
        var siblings = db.ContinuityClaims
            .Where(c => c.EntityId == winner.EntityId && c.Predicate == winner.Predicate
                     && c.ClaimUid != claimUid && live.Contains(c.Status)
                     && (onlyRejectClaimUids == null || onlyRejectClaimUids.Contains(c.ClaimUid)))
            .ToList();
        foreach (var s in siblings)
        {
            ApplyStatus(s, "REJECTED", now, note);
            s.SupersededBy = winner.ClaimUid;
        }

        db.SaveChanges();
        tx.Commit();
    }

    /// <summary>Marks every live (NEW/CONFIRMED/CONTRADICTED/CANONICAL) claim tagged with this
    /// book's slug as SUPERSEDED, without picking a replacement value. Exists to reset a book's
    /// fact ledger before a fresh extraction pass: <see cref="ContinuityClaim"/> carries no BeatId,
    /// so there is no automatic way to notice a claim's source beat was later cut/detached from
    /// the book (Beats rows survive detachment by design — see docs on system-versioned tables —
    /// but the claim extracted from one doesn't know that happened) and keeps contradicting the
    /// replacement content forever. Found live 2026-09-01 on VIGL: a fact-ledger group's evidence
    /// traced to beat #5501, which had zero BeatNodes membership and contained two unrelated
    /// story fragments spliced together (Stale=true) — clearly superseded pre-rewrite content
    /// still fighting the current book in the ledger. Entity-record-sourced claims (BookSlug is
    /// null on those) are untouched — re-extraction doesn't refresh them anyway.</summary>
    public int SupersedeAllLiveClaimsForBook(string bookSlug, string note)
    {
        using var db = dbFactory.CreateDbContext();
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED", "CANONICAL" };
        var claims = db.ContinuityClaims.Where(c => c.BookSlug == bookSlug && live.Contains(c.Status)).ToList();
        var now = DateTime.UtcNow.ToString("o");
        foreach (var c in claims) ApplyStatus(c, "SUPERSEDED", now, note);
        db.SaveChanges();
        return claims.Count;
    }

    public void RejectClaim(string claimUid, string note = "")
    {
        using var db = dbFactory.CreateDbContext();
        var c = db.ContinuityClaims.FirstOrDefault(x => x.ClaimUid == claimUid);
        if (c == null) return;
        ApplyStatus(c, "REJECTED", DateTime.UtcNow.ToString("o"), note);
        db.SaveChanges();
    }

    public void MarkApplied(string claimUid, string fieldPath)
    {
        using var db = dbFactory.CreateDbContext();
        var c = db.ContinuityClaims.FirstOrDefault(x => x.ClaimUid == claimUid);
        if (c == null) return;
        c.AppliedAt = DateTime.UtcNow.ToString("o");
        c.AppliedToField = fieldPath;
        db.SaveChanges();
    }

    private static void ApplyStatus(ContinuityClaim c, string status, string now, string note)
    {
        c.Status = status;
        c.ResolvedAt = now;
        if (!string.IsNullOrEmpty(note)) c.ResolutionNote = note;
    }

    private static void RecordConfirmation(ProseDbContext db, string claimUid, string? chapterId, string? sourcePath, string when)
    {
        if (string.IsNullOrEmpty(chapterId) && string.IsNullOrEmpty(sourcePath)) return;
        var sc = chapterId ?? "";
        var sp = sourcePath ?? "";
        var existing = db.ClaimConfirmations.FirstOrDefault(x =>
            x.ClaimUid == claimUid && x.SourceChapterId == sc && x.SourcePath == sp);
        if (existing != null) return;
        db.ClaimConfirmations.Add(new ClaimConfirmationRow
        {
            ClaimUid = claimUid, SourceChapterId = sc, SourcePath = sp, ConfirmedAt = when,
        });
    }

    private static void RecordContradiction(ProseDbContext db, string aUid, string bUid, string when)
    {
        var existing = db.ClaimContradictions.FirstOrDefault(x => x.AUid == aUid && x.BUid == bUid);
        if (existing != null) return;
        db.ClaimContradictions.Add(new ClaimContradictionRow { AUid = aUid, BUid = bUid, DetectedAt = when });
    }
}

// ── Models (unchanged shape — matches the old SQLite-backed service exactly) ──

public class ContinuityClaim
{
    public string ClaimUid             { get; set; } = "";
    public string EntityId             { get; set; } = "";
    public string EntityName           { get; set; } = "";
    public string EntityKind           { get; set; } = "";
    public string Predicate            { get; set; } = "";
    public string Object               { get; set; } = "";

    public string SourceType           { get; set; } = "";
    public string? SourcePath          { get; set; }
    public string? SourceChapterId     { get; set; }
    public int?    SourceChapterNumber { get; set; }
    public string? SourceChapterTitle  { get; set; }

    public string? Snippet             { get; set; }
    public string? Voice               { get; set; }
    public string? Confidence          { get; set; }
    public List<string> ExtractedBy    { get; set; } = new();

    public string Status               { get; set; } = "NEW";

    public string FirstAssertedAt      { get; set; } = "";
    public string LastConfirmedAt      { get; set; } = "";
    public string? ResolvedAt          { get; set; }
    public string? AppliedAt           { get; set; }
    public string? AppliedToField      { get; set; }
    public string? SupersededBy        { get; set; }
    public string? ResolutionNote      { get; set; }

    /// <summary>23rd-century in-world date the claim describes (when known).</summary>
    public DateTime? StoryDate         { get; set; }

    /// <summary>Code of the BookNode this claim was extracted from (e.g. "BCODA", "RTR"). Null for entity-record sources.</summary>
    public string? BookSlug            { get; set; }

    /// <summary>
    /// How this fact came to be believed — see <see cref="ClaimProvenance"/> for the grades.
    ///
    /// <para>The one question no column could answer before (Story Ledger Phase 2): <i>did a
    /// human ever approve this, or did a model invent it?</i> Without it, "show me everything in
    /// canon nobody ever approved" is an archaeology project rather than a query — which is why
    /// a fabricated character survived in canon long enough to spread into a weapon record and
    /// an unrelated book's character.</para>
    /// </summary>
    public string Provenance           { get; set; } = ClaimProvenance.Inferred;

    /// <summary>
    /// The specific beat this claim was extracted from, when known.
    ///
    /// <para>The ledger had chapter anchors (<see cref="SourceChapterId"/>) but no beat anchor,
    /// so a claim could be traced to a 40-beat chapter and no further. The Tuned Read needs the
    /// exact beat to pull the carrier band for adjudication and to key the verdict cache on that
    /// beat's current text; a finding also needs it to say WHERE, not roughly where.</para>
    ///
    /// <para>Null for entity-record claims (no beat exists) and for pre-Phase-2 rows.</para>
    /// </summary>
    public Guid? SourceBeatId          { get; set; }

    /// <summary>The <see cref="Prose.Core.Data.Entities.PredicateExclusion"/> that flagged this
    /// claim as contradicting another, when the contradiction came from the exclusion ontology
    /// rather than from the same-predicate/different-object rule. Null otherwise — including for
    /// every non-contradicted claim.</summary>
    public int? ExclusionRuleId        { get; set; }
}

/// <summary>
/// Provenance grades, in descending trust. Applied to <see cref="ContinuityClaim.Provenance"/>
/// (Phase 2) and, since Story Ledger Phase 3, to
/// <see cref="Prose.Core.Data.Entities.Entity.Provenance"/> and
/// <see cref="Prose.Core.Data.Entities.CharacterRelationshipRow.Provenance"/>.
///
/// <para>Deliberately ONE vocabulary across all three tables rather than a per-table enum: the
/// question is identical in each case ("did a human approve this?"), and
/// <c>prose --provenance-audit</c> reports the three side by side. Three parallel dialects would
/// drift, and the audit would have to translate between them.</para>
/// </summary>
public static class ClaimProvenance
{
    /// <summary>A human decided this. The only grade that is canon without qualification.</summary>
    public const string Authored = "authored";

    /// <summary>Extracted from prose with a snippet that MECHANICALLY verifies against the beat
    /// text — not "an LLM said it appears", but a literal substring match. This is the grade
    /// ContinuityExtractionService's chapter path earns, because it already drops any candidate
    /// whose snippet is not present verbatim in the prose.</summary>
    public const string Observed = "observed";

    /// <summary>A model produced it without a verifying quote, or it was derived from something
    /// else. Believable, never authoritative.</summary>
    public const string Inferred = "inferred";

    /// <summary>Auto-created by entity scaffolding. <b>Never canon</b> — candidate only.</summary>
    public const string Scaffolded = "scaffolded";

    /// <summary>Pre-existing rows, grandfathered by the Phase 2 migration. Author ruling: do not
    /// mass-flag these; flag only the suspicious ones. An unknown grade is not evidence of a
    /// defect, and treating 12,888 rows as suspect would bury the ones that are.</summary>
    public const string LegacyUnknown = "legacy-unknown";

    public static readonly string[] All =
        [Authored, Observed, Inferred, Scaffolded, LegacyUnknown];

    /// <summary>True when <paramref name="provenance"/> is one of the five known grades. Used by
    /// the promotion CLI to refuse a typo rather than writing an unqueryable grade nobody's
    /// reports will ever count.</summary>
    public static bool IsValid(string? provenance) =>
        provenance != null && All.Contains(provenance, StringComparer.Ordinal);

    /// <summary>True for grades that may be treated as established fact when generating prose.
    /// <see cref="Scaffolded"/> deliberately is not, and neither is <see cref="LegacyUnknown"/>
    /// on its own merit — it is tolerated only because it predates the grading.</summary>
    public static bool IsTrustworthy(string? provenance) =>
        provenance is Authored or Observed;
}

public class ClaimUpsertResult
{
    public string Outcome { get; set; } = "";
    public ContinuityClaim Claim    { get; set; } = new();
    public ContinuityClaim? Conflict { get; set; }
}

public class ContradictionPair
{
    public ContinuityClaim A { get; set; } = new();
    public ContinuityClaim B { get; set; } = new();
    public string Key => A.ClaimUid + "|" + B.ClaimUid;
}

public class ContradictionGroup
{
    public string EntityId   { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string EntityKind { get; set; } = "";
    public string Predicate  { get; set; } = "";
    public List<ContinuityClaim> Claims { get; set; } = new();
    public string Key => EntityId + "|" + Predicate;
}

public class ResolveResult
{
    public ContinuityClaim Winner  { get; set; } = new();
    public ContinuityClaim Loser   { get; set; } = new();
    public ContinuityClaim? Loser2 { get; set; }
}

public class ContinuityStats
{
    public int Total            { get; set; }
    public int New              { get; set; }
    public int Confirmed        { get; set; }
    public int Contradicted     { get; set; }
    public int Canonical        { get; set; }
    public int Rejected         { get; set; }
    public int Superseded       { get; set; }
    public int FromProse        { get; set; }
    public int FromEntityRecord { get; set; }
    public int FromBible        { get; set; }
}
