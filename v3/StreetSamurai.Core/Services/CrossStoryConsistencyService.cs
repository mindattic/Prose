using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Surfaces factual contradictions that span multiple story nodes.
/// Wraps the existing <see cref="ContinuityService"/> contradiction data but
/// filters to groups where conflicting claims originate from different stories
/// (different <c>ContinuityClaim.StorySlug</c> values).
/// Same-story contradictions are the existing per-story continuity system's job.
/// </summary>
public class CrossStoryConsistencyService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public CrossStoryConsistencyService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns all cross-story conflicts, optionally limited to those first detected
    /// within the last <paramref name="since"/> window.
    /// </summary>
    public async Task<CrossStoryConsistencyReport> GetCrossStoryConflictsAsync(
        DateTime? since = null,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateDbContext();
        var live = new[] { "NEW", "CONFIRMED", "CONTRADICTED" };

        var claims = await db.ContinuityClaims
            .AsNoTracking()
            .Where(c => live.Contains(c.Status) && c.StorySlug != null)
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
                // Must have claims from at least two different stories
                var stories = g.Select(c => c.StorySlug!).Distinct().ToList();
                return stories.Count > 1;
            })
            .Select(g =>
            {
                var byObject = g
                    .GroupBy(c => c.Object.Trim().ToLowerInvariant())
                    .Select(og =>
                    {
                        var stories = og.Select(c => c.StorySlug!).Distinct().OrderBy(s => s).ToList();
                        return new ObjectVariant(og.First().Object, stories, og.Count());
                    })
                    .OrderByDescending(v => v.ClaimCount)
                    .ToList();

                var majority = byObject.First();
                var minority = byObject.Last();

                // Only include if the majority and minority come from different stories
                var allStories = byObject.SelectMany(v => v.Stories).Distinct().ToList();
                bool isCrossStory = allStories.Count > 1;

                return new CrossStoryConflict(
                    EntityId:       g.Key.EntityId,
                    EntityName:     g.First().EntityName,
                    EntityKind:     g.First().EntityKind,
                    Predicate:      g.Key.Predicate,
                    MajorityObject: majority.Object,
                    MajorityStories: majority.Stories,
                    MajorityCount:  majority.ClaimCount,
                    MinorityObject: minority.Object,
                    MinorityStories: minority.Stories,
                    MinorityCount:  minority.ClaimCount,
                    IsCrossStory:   isCrossStory
                );
            })
            .Where(c => c.IsCrossStory)
            .OrderByDescending(c => c.MajorityCount - c.MinorityCount)
            .ThenBy(c => c.EntityName)
            .ToList();

        return new CrossStoryConsistencyReport(DateTime.UtcNow, conflicts);
    }
}

// ── Data models ──────────────────────────────────────────────────────────────

public record CrossStoryConsistencyReport(
    DateTime GeneratedAt,
    IReadOnlyList<CrossStoryConflict> Conflicts);

public record CrossStoryConflict(
    string EntityId,
    string EntityName,
    string EntityKind,
    string Predicate,
    string MajorityObject,
    IReadOnlyList<string> MajorityStories,
    int MajorityCount,
    string MinorityObject,
    IReadOnlyList<string> MinorityStories,
    int MinorityCount,
    bool IsCrossStory);

internal record ObjectVariant(
    string Object,
    IReadOnlyList<string> Stories,
    int ClaimCount);
