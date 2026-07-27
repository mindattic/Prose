using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Unified continuity store. Atomic (entity, predicate, object) claims extracted
/// from chapter prose or entity records. ContinuityClaims with the same
/// (entity, predicate) and different object are flagged CONTRADICTED so a
/// resolution flow can pick a winner.
///
/// Backed by the unified StreetSamurai SQL Server database — the legacy
/// continuity.db SQLite store has been retired. Public API is preserved so
/// every existing caller compiles unchanged.
/// </summary>
public class ContinuityService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public ContinuityService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>Path of the legacy SQLite file. Kept for diagnostic display only — no longer authoritative.</summary>
    public string DbPath => "(SQL Server: continuity tables in StreetSamurai database)";
    public bool IsAvailable => true;

    // ── ID generation ────────────────────────────────────────────────────────

    /// <summary>
    /// Stable uid: hash of (entity_id | predicate | normalized object). Same
    /// (entity, predicate, object) always produces the same uid, so re-extracting
    /// the same claim is idempotent — the row is updated, not duplicated.
    /// </summary>
    public static string ComputeClaimUid(string entityId, string predicate, string objectValue)
    {
        var normalized = $"{entityId}|{Normalize(predicate)}|{Normalize(objectValue)}";
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(normalized));
        var hex = Convert.ToHexString(bytes).ToLowerInvariant();
        return "claim-" + hex[..16];
    }

    private static string Normalize(string s)
        => string.IsNullOrEmpty(s) ? "" : System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"\s+", " ").Trim();

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

        // Look for a different-object claim on the same (entity, predicate).
        var conflict = db.ContinuityClaims
            .Where(c => c.EntityId == incoming.EntityId
                     && c.Predicate == incoming.Predicate
                     && c.Object.ToLower().Trim() != incoming.Object.ToLower().Trim()
                     && c.Status != "REJECTED" && c.Status != "SUPERSEDED"
                     && c.Status != "CANONICAL")
            .OrderByDescending(c => c.LastConfirmedAt)
            .FirstOrDefault();

        incoming.Status          = conflict != null ? "CONTRADICTED" : "NEW";
        incoming.FirstAssertedAt = now;
        incoming.LastConfirmedAt = now;
        db.ContinuityClaims.Add(incoming);
        db.SaveChanges();

        RecordConfirmation(db, incoming.ClaimUid, incoming.SourceChapterId, incoming.SourcePath, now);
        db.SaveChanges();

        if (conflict != null)
        {
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

    public List<ContradictionGroup> GetContradictionGroups()
    {
        using var db = dbFactory.CreateDbContext();
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED" };

        var keys = db.ContinuityClaims.AsNoTracking()
            .Where(c => live.Contains(c.Status))
            .GroupBy(c => new { c.EntityId, c.Predicate })
            .Select(g => new { g.Key.EntityId, g.Key.Predicate, Variants = g.Select(x => x.Object).Distinct().Count() })
            .Where(g => g.Variants > 1)
            .ToList();

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
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED" };
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

        ApplyStatus(a, "REJECTED", now, note);
        ApplyStatus(b, "REJECTED", now, note);

        var custom = new ContinuityClaim
        {
            ClaimUid        = ComputeClaimUid(a.EntityId, a.Predicate, customObject),
            EntityId        = a.EntityId,
            EntityName      = a.EntityName,
            EntityKind      = a.EntityKind,
            Predicate       = a.Predicate,
            Object          = customObject,
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

    public void MakeCanonical(string claimUid, string note = "")
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
                     && c.ClaimUid != claimUid && live.Contains(c.Status))
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

    private static void RecordConfirmation(StreetSamuraiDbContext db, string claimUid, string? chapterId, string? sourcePath, string when)
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

    private static void RecordContradiction(StreetSamuraiDbContext db, string aUid, string bUid, string when)
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
}
