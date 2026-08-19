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
        var conflict = db.ContinuityClaims
            .Where(c => c.EntityId == incoming.EntityId
                     && c.Predicate == incoming.Predicate
                     && c.Status != "REJECTED" && c.Status != "SUPERSEDED")
            .OrderByDescending(c => c.Status == "CANONICAL" ? 1 : 0).ThenByDescending(c => c.LastConfirmedAt)
            .ToList()
            .FirstOrDefault(c => !ObjectsMatch(incoming.Predicate, c.Object, incoming.Object));

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
    public List<ContradictionGroup> GetContradictionGroups(string? bookSlug = null)
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

        var groups = new List<ContradictionGroup>();
        foreach (var k in keys)
        {
            var claims = db.ContinuityClaims.AsNoTracking()
                .Where(c => c.EntityId == k.EntityId && c.Predicate == k.Predicate && live.Contains(c.Status))
                .OrderBy(c => c.FirstAssertedAt)
                .ToList();
            if (claims.Count >= 2)
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
                FromBible        = g.Sum(c => c.SourceType == "bible"         ? 1 : 0),
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
    public void MakeCanonical(string claimUid, string note = "", IReadOnlySet<string>? onlyRejectClaimUids = null)
    {
        using var db = dbFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();
        var winner = db.ContinuityClaims.FirstOrDefault(c => c.ClaimUid == claimUid)
            ?? throw new InvalidOperationException($"Claim not found: {claimUid}");
        var now = DateTime.UtcNow.ToString("o");
        ApplyStatus(winner, "CANONICAL", now, note);

        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED" };
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
