using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Reads and writes the provenance grade on the three graded tables — <c>Entities</c>,
/// <c>CharacterRelationships</c>, and <c>ContinuityClaims</c> — and answers the one question the
/// Story Ledger exists to make answerable: <i>what is in canon that no human ever approved?</i>
///
/// <para><b>Why one service rather than a grade-setter per call site.</b> Provenance is only
/// worth having if every writer agrees on the vocabulary and every reader can count the same
/// things. Two of the three tables also have a non-obvious write requirement —
/// <c>CharacterRelationships</c> needs a <c>CharacterReadModels</c> projection refresh or the
/// change is invisible to every reader (see <see cref="SetRelationshipProvenanceAsync"/>), and
/// <c>ContinuityClaims</c>'s key is a string uid, not an id. Spreading that across the scaffolder,
/// the CLI, and whatever comes next is how the grades would drift apart.</para>
///
/// <para>Story Ledger Phase 3. Report-only where it reports (docs/LOGIC.md §4); the grade writes
/// are explicit human acts invoked through <c>prose --provenance</c>, never inferred.</para>
/// </summary>
public class ProvenanceService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<ProvenanceService> log;

    public ProvenanceService(IDbContextFactory<ProseDbContext> dbFactory, ILogger<ProvenanceService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>One table's population at one grade.</summary>
    public sealed record GradeCount(string Table, string Grade, long Count);

    /// <summary>An ungraded/unapproved row, named well enough to act on without a second query.</summary>
    public sealed record UngradedRow(string Table, string Id, string Label, string Grade, string? Scope);

    public sealed record ProvenanceReport(
        DateTime RanAtUtc,
        string? BookSlug,
        Guid? BookNodeId,
        IReadOnlyList<GradeCount> Counts,
        IReadOnlyList<UngradedRow> Samples,
        long TotalRows,
        long UnapprovedRows)
    {
        /// <summary>Share of graded rows that no human ever approved (authored/observed).</summary>
        public double UnapprovedFraction => TotalRows == 0 ? 0 : (double)UnapprovedRows / TotalRows;
    }

    public const int DefaultSampleLimit = 25;

    /// <summary>
    /// Count every graded row by table and grade, and sample the unapproved ones.
    ///
    /// <para>Scope: <paramref name="bookNodeId"/> restricts <c>Entities</c> to that book's own
    /// scoped entities (<see cref="Entity.OriginNodeId"/>) and <c>CharacterRelationships</c> to
    /// rows owned by them; <paramref name="bookSlug"/> restricts <c>ContinuityClaims</c>, which
    /// records its book by code rather than id. A universe-wide entity (OriginNodeId null) is
    /// deliberately NOT counted under a book scope — it belongs to every book in the universe, so
    /// attributing it to one would double-count it across a corpus sweep. Pass no scope for the
    /// whole (ambient) universe, which is the honest way to see universe-wide canon.</para>
    /// </summary>
    public async Task<ProvenanceReport> AuditAsync(
        Guid? bookNodeId = null,
        string? bookSlug = null,
        int sampleLimit = DefaultSampleLimit,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entities = db.Entities.AsQueryable();
        if (bookNodeId is { } bid) entities = entities.Where(e => e.OriginNodeId == bid);

        var rels = db.CharacterRelationships.AsQueryable();
        if (bookNodeId is { } rbid)
        {
            // Relationship rows carry no scope of their own; the owning character's does.
            var ownerIds = db.Entities.Where(e => e.OriginNodeId == rbid).Select(e => e.Id);
            rels = rels.Where(r => ownerIds.Contains(r.CharacterId));
        }

        var claims = db.ContinuityClaims.Where(c => c.Status != "REJECTED" && c.Status != "SUPERSEDED");
        if (!string.IsNullOrWhiteSpace(bookSlug)) claims = claims.Where(c => c.BookSlug == bookSlug);

        var counts = new List<GradeCount>();
        counts.AddRange((await entities.AsNoTracking()
                .GroupBy(e => e.Provenance)
                .Select(g => new { Grade = g.Key, Count = g.LongCount() }).ToListAsync(ct))
            .Select(x => new GradeCount("Entities", x.Grade, x.Count)));
        counts.AddRange((await rels.AsNoTracking()
                .GroupBy(r => r.Provenance)
                .Select(g => new { Grade = g.Key, Count = g.LongCount() }).ToListAsync(ct))
            .Select(x => new GradeCount("CharacterRelationships", x.Grade, x.Count)));
        counts.AddRange((await claims.AsNoTracking()
                .GroupBy(c => c.Provenance)
                .Select(g => new { Grade = g.Key, Count = g.LongCount() }).ToListAsync(ct))
            .Select(x => new GradeCount("ContinuityClaims", x.Grade, x.Count)));

        // "Unapproved" is exactly ClaimProvenance.IsTrustworthy's complement, evaluated here in
        // memory over the grade names rather than duplicated as a SQL predicate — one definition
        // of trust, so a new grade cannot mean two different things in two places.
        var unapproved = counts.Where(c => !ClaimProvenance.IsTrustworthy(c.Grade)).Sum(c => c.Count);

        var samples = new List<UngradedRow>();
        if (sampleLimit > 0)
        {
            var perTable = Math.Max(1, sampleLimit / 3);

            samples.AddRange((await entities.AsNoTracking()
                    .Where(e => e.Provenance != ClaimProvenance.Authored && e.Provenance != ClaimProvenance.Observed)
                    .OrderByDescending(e => e.ModifiedAt)
                    .Select(e => new { e.Id, e.Name, e.EntityType, e.Provenance, e.OriginNodeId })
                    .Take(perTable).ToListAsync(ct))
                .Select(e => new UngradedRow(
                    "Entities", e.Id.ToString("N"), $"[{e.EntityType}] {e.Name}", e.Provenance,
                    e.OriginNodeId?.ToString("N"))));

            samples.AddRange((await rels.AsNoTracking()
                    .Where(r => r.Provenance != ClaimProvenance.Authored && r.Provenance != ClaimProvenance.Observed)
                    .OrderByDescending(r => r.Id)
                    .Select(r => new { r.Id, r.CharacterId, r.Type, r.TargetName, r.Provenance })
                    .Take(perTable).ToListAsync(ct))
                .Select(r => new UngradedRow(
                    "CharacterRelationships", r.Id.ToString(),
                    $"[{r.Type}] -> {(string.IsNullOrWhiteSpace(r.TargetName) ? "(EMPTY TARGET)" : r.TargetName)}",
                    r.Provenance, r.CharacterId.ToString("N"))));

            samples.AddRange((await claims.AsNoTracking()
                    .Where(c => c.Provenance != ClaimProvenance.Authored && c.Provenance != ClaimProvenance.Observed)
                    .OrderByDescending(c => c.LastConfirmedAt)
                    .Select(c => new { c.ClaimUid, c.EntityName, c.Predicate, c.Object, c.Provenance, c.BookSlug })
                    .Take(perTable).ToListAsync(ct))
                .Select(c => new UngradedRow(
                    "ContinuityClaims", c.ClaimUid, $"{c.EntityName}: {c.Predicate} -> {c.Object}",
                    c.Provenance, c.BookSlug)));
        }

        return new ProvenanceReport(
            RanAtUtc: DateTime.UtcNow,
            BookSlug: bookSlug,
            BookNodeId: bookNodeId,
            Counts: counts.OrderBy(c => c.Table).ThenBy(c => c.Grade).ToList(),
            Samples: samples,
            TotalRows: counts.Sum(c => c.Count),
            UnapprovedRows: unapproved);
    }

    /// <summary>
    /// Set an entity's grade. <paramref name="entityId"/> is looked up with
    /// <c>IgnoreQueryFilters()</c>: an explicitly-named id must resolve regardless of the ambient
    /// universe, or the caller gets "not found" for a row that plainly exists (a bug class this
    /// project has shipped repeatedly — see feedback_explicit_id_lookups_need_ignorequeryfilters).
    /// </summary>
    public async Task<bool> SetEntityProvenanceAsync(Guid entityId, string grade, CancellationToken ct = default)
    {
        if (!ClaimProvenance.IsValid(grade)) throw new ArgumentException($"Unknown provenance grade '{grade}'.", nameof(grade));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entityId, ct);
        if (entity == null) return false;

        entity.Provenance = grade;
        entity.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Provenance: entity {Id} ('{Name}') graded {Grade}", entityId, entity.Name, grade);
        return true;
    }

    /// <summary>
    /// Set one relationship row's grade.
    ///
    /// <para>Refreshes <c>CharacterReadModels</c> afterwards, and that is not optional:
    /// <c>CharacterRepository.GetById/GetAll</c> — and therefore <c>get_character</c> and every
    /// other read surface — serve from that projection, not from this bridge table. A direct row
    /// write without the refresh really does change the row while every reader keeps showing the
    /// old value (found live during Phase 0's relationship-deletion work).</para>
    /// </summary>
    public async Task<bool> SetRelationshipProvenanceAsync(long rowId, string grade, CancellationToken ct = default)
    {
        if (!ClaimProvenance.IsValid(grade)) throw new ArgumentException($"Unknown provenance grade '{grade}'.", nameof(grade));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.CharacterRelationships.FirstOrDefaultAsync(r => r.Id == rowId, ct);
        if (row == null) return false;

        row.Provenance = grade;
        await db.SaveChangesAsync(ct);

        try { await CharacterMapper.RefreshReadModelAsync(db, row.CharacterId); }
        catch (Exception ex)
        {
            log.LogWarning(ex,
                "Provenance: relationship {RowId} graded {Grade} but the read-model refresh for character " +
                "{CharacterId} failed — readers may serve the old value until the projection is rebuilt",
                rowId, grade, row.CharacterId);
        }

        log.LogInformation("Provenance: relationship row {RowId} on character {CharacterId} graded {Grade}",
            rowId, row.CharacterId, grade);
        return true;
    }

    /// <summary>Set one ledger claim's grade, keyed by its uid.</summary>
    public async Task<bool> SetClaimProvenanceAsync(string claimUid, string grade, CancellationToken ct = default)
    {
        if (!ClaimProvenance.IsValid(grade)) throw new ArgumentException($"Unknown provenance grade '{grade}'.", nameof(grade));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var claim = await db.ContinuityClaims.FirstOrDefaultAsync(c => c.ClaimUid == claimUid, ct);
        if (claim == null) return false;

        claim.Provenance = grade;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Provenance: claim {Uid} graded {Grade}", claimUid, grade);
        return true;
    }
}
