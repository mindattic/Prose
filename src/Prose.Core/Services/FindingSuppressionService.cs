using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record FindingSuppression(
    long Id, string NodeSlug, Guid? BeatId, string Code, string? Reason,
    string? CreatedBy, DateTime CreatedAt, bool Active);

/// <summary>
/// The exception list <see cref="FindingsService.Upsert"/> consults before filing a finding —
/// see <see cref="FindingSuppressionRow"/>'s doc comment for why this is a side-table rather than
/// prose/entity-tag markup. Deterministic and cheap: a handful of rows per book at most, matched
/// by exact NodeSlug + optional BeatId + <see cref="FindingCodeRegistry"/> code, never an LLM call.
/// </summary>
public class FindingSuppressionService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public FindingSuppressionService(IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var db = dbFactory.CreateDbContext();
        if (!db.Database.IsSqlServer()) return; // SQLite test fixtures create this from the EF model instead.
        db.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[dbo].[FindingSuppressions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[FindingSuppressions] (
                    [Id]        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [NodeSlug]  NVARCHAR(200) NOT NULL,
                    [BeatId]    UNIQUEIDENTIFIER NULL,
                    [Code]      NVARCHAR(100) NOT NULL,
                    [Reason]    NVARCHAR(500) NULL,
                    [CreatedBy] NVARCHAR(100) NULL,
                    [CreatedAt] DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
                    [Active]    BIT           NOT NULL DEFAULT 1
                );
                CREATE INDEX [IX_FindingSuppressions_Node] ON [dbo].[FindingSuppressions]([NodeSlug], [Active]);
            END;
            """);

        // Findings.SuppressedBy — same idempotent-ALTER precedent as SourceRuleVersion (RFC 0011
        // Brick 2) so an existing dev DB self-upgrades without a formal migration.
        db.Database.ExecuteSqlRaw("""
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Findings]') AND name = 'SuppressedBy')
            BEGIN
                ALTER TABLE [dbo].[Findings] ADD [SuppressedBy] NVARCHAR(200) NULL;
            END;
            """);
    }

    private static FindingSuppression Map(FindingSuppressionRow r) =>
        new(r.Id, r.NodeSlug, r.BeatId, r.Code, r.Reason, r.CreatedBy, r.CreatedAt, r.Active);

    /// <summary>Records a new exception. Throws <see cref="ArgumentException"/> for a code this
    /// registry doesn't recognize — refusing a typo at write time beats discovering, months later,
    /// that a suppression silently never matched anything.</summary>
    public FindingSuppression Add(string nodeSlug, Guid? beatId, string code, string? reason, string? createdBy)
    {
        if (!FindingCodeRegistry.IsKnownCode(code))
            throw new ArgumentException($"unknown finding code: {code} (see `prose --findings codes`)", nameof(code));

        using var db = dbFactory.CreateDbContext();
        var row = new FindingSuppressionRow
        {
            NodeSlug  = nodeSlug,
            BeatId    = beatId,
            Code      = code,
            Reason    = reason,
            CreatedBy = createdBy,
        };
        db.FindingSuppressions.Add(row);
        db.SaveChanges();
        return Map(row);
    }

    /// <summary>Soft-deactivates (never physically deletes — its own history is evidence that a
    /// pattern was once reviewed and ruled intentional, same as a dismissed finding).</summary>
    public bool Deactivate(long id)
    {
        using var db = dbFactory.CreateDbContext();
        var row = db.FindingSuppressions.FirstOrDefault(s => s.Id == id);
        if (row is null) return false;
        row.Active = false;
        db.SaveChanges();
        return true;
    }

    public IReadOnlyList<FindingSuppression> List(string? nodeSlug = null, bool activeOnly = true)
    {
        using var db = dbFactory.CreateDbContext();
        var q = db.FindingSuppressions.AsNoTracking().AsQueryable();
        if (nodeSlug != null) q = q.Where(s => s.NodeSlug == nodeSlug);
        if (activeOnly) q = q.Where(s => s.Active);
        return q.OrderBy(s => s.NodeSlug).ThenBy(s => s.Code).Select(r => new FindingSuppression(
            r.Id, r.NodeSlug, r.BeatId, r.Code, r.Reason, r.CreatedBy, r.CreatedAt, r.Active)).ToList();
    }

    /// <summary>The check <see cref="FindingsService.Upsert"/> runs before filing. Returns a
    /// human-readable "code (scope)" description of the first active suppression that matches, or
    /// null if none do. Book-wide rows (BeatId null) are checked alongside any beat-specific row
    /// for this exact beat — a beat match and a book match can coexist; either is sufficient.</summary>
    public string? FindMatch(string nodeSlug, Guid? beatId, FindingCategory category, string summary)
    {
        using var db = dbFactory.CreateDbContext();
        var candidates = db.FindingSuppressions.AsNoTracking()
            .Where(s => s.Active && s.NodeSlug == nodeSlug && (s.BeatId == null || s.BeatId == beatId))
            .ToList();
        foreach (var c in candidates)
        {
            if (!FindingCodeRegistry.Matches(c.Code, category, summary)) continue;
            return c.BeatId.HasValue ? $"{c.Code} (beat)" : $"{c.Code} (book)";
        }
        return null;
    }
}
