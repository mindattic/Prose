using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Surfaces factual contradictions that span multiple book nodes.
/// Wraps the existing <see cref="ContinuityService"/> contradiction data but
/// filters to groups where conflicting claims originate from different books
/// (different <c>ContinuityClaim.BookSlug</c> values).
/// Same-book contradictions are the existing per-book continuity system's job.
/// </summary>
public class CrossBookConsistencyService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public CrossBookConsistencyService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns all cross-book conflicts, optionally limited to those first detected
    /// within the last <paramref name="since"/> window.
    /// </summary>
    public async Task<CrossBookConsistencyReport> GetCrossBookConflictsAsync(
        DateTime? since = null,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED" };

        var claims = await db.ContinuityClaims
            .AsNoTracking()
            .Where(c => live.Contains(c.Status) && c.BookSlug != null)
            .ToListAsync(ct);

        if (since.HasValue)
        {
            var sinceStr = since.Value.ToString("o");
            claims = claims.Where(c => string.Compare(c.FirstAssertedAt, sinceStr, StringComparison.Ordinal) >= 0).ToList();
        }

        var conflicts = claims
            .GroupBy(c => new { c.EntityId, c.Predicate })
            .Where(g =>
            {
                var objectValues = g.Select(c => c.Object.Trim().ToLowerInvariant()).Distinct().ToList();
                if (objectValues.Count <= 1) return false;
                // Must have claims from at least two different books
                var books = g.Select(c => c.BookSlug!).Distinct().ToList();
                return books.Count > 1;
            })
            .Select(g =>
            {
                var byObject = g
                    .GroupBy(c => c.Object.Trim().ToLowerInvariant())
                    .Select(og =>
                    {
                        var books = og.Select(c => c.BookSlug!).Distinct().OrderBy(s => s).ToList();
                        return new ObjectVariant(og.First().Object, books, og.Count());
                    })
                    .OrderByDescending(v => v.ClaimCount)
                    .ToList();

                var majority = byObject.First();
                var minority = byObject.Last();

                // Only include if the majority and minority come from different books
                var allBooks = byObject.SelectMany(v => v.Books).Distinct().ToList();
                bool isCrossBook = allBooks.Count > 1;

                return new CrossBookConflict(
                    EntityId:       g.Key.EntityId,
                    EntityName:     g.First().EntityName,
                    EntityKind:     g.First().EntityKind,
                    Predicate:      g.Key.Predicate,
                    MajorityObject: majority.Object,
                    MajorityBooks:  majority.Books,
                    MajorityCount:  majority.ClaimCount,
                    MinorityObject: minority.Object,
                    MinorityBooks:  minority.Books,
                    MinorityCount:  minority.ClaimCount,
                    IsCrossBook:    isCrossBook
                );
            })
            .Where(c => c.IsCrossBook)
            .OrderByDescending(c => c.MajorityCount - c.MinorityCount)
            .ThenBy(c => c.EntityName)
            .ToList();

        return new CrossBookConsistencyReport(DateTime.UtcNow, conflicts);
    }
}

// ── Data models ──────────────────────────────────────────────────────────────

public record CrossBookConsistencyReport(
    DateTime GeneratedAt,
    IReadOnlyList<CrossBookConflict> Conflicts);

public record CrossBookConflict(
    string EntityId,
    string EntityName,
    string EntityKind,
    string Predicate,
    string MajorityObject,
    IReadOnlyList<string> MajorityBooks,
    int MajorityCount,
    string MinorityObject,
    IReadOnlyList<string> MinorityBooks,
    int MinorityCount,
    bool IsCrossBook);

internal record ObjectVariant(
    string Object,
    IReadOnlyList<string> Books,
    int ClaimCount);
