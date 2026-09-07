using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Builds a <see cref="BeatBrief"/> for one beat from what the database already knows
/// (RFC 0012 §3.1). Nothing here calls an LLM.
///
/// <list type="bullet">
/// <item><b>Goal</b> — the caller's goal text (the beat's Description, else Title).</item>
/// <item><b>StopBefore</b> — the next beat's Description in reading order: the next row by
///   SortKey in the same chapter; at chapter end, the first beat of the next chapter under the
///   same parent (by chapter SortKey); null when nothing follows.</item>
/// <item><b>MustInclude</b> — every canon entity name (from <see cref="UniverseGraphService"/>)
///   that occurs in the goal.</item>
/// <item><b>Pov</b> — the beat's 'pov' presence row resolved to a name, when one exists.</item>
/// </list>
/// </summary>
public class BeatBriefBuilder
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly UniverseGraphService? graph;
    private readonly VerificationContextService? verification;
    private readonly ILogger<BeatBriefBuilder> log;

    public BeatBriefBuilder(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<BeatBriefBuilder> log,
        UniverseGraphService? graph = null,
        VerificationContextService? verification = null)
    {
        this.dbFactory = dbFactory;
        this.log = log;
        this.graph = graph;
        this.verification = verification;
    }

    public async Task<BeatBrief> BuildAsync(Guid beatId, string goal, string? subtext, int targetWords, CancellationToken ct = default)
    {
        string? stopBefore = null;
        var closesChapter = false;
        string? pov = null;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var membership = await db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
                .Where(bn => bn.BeatId == beatId)
                .Select(bn => new { bn.NodeId, bn.SortKey })
                .FirstOrDefaultAsync(ct);

            if (membership != null)
            {
                // Next beat in the same chapter.
                var next = await (
                    from bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
                    join b in db.Beats.AsNoTracking().IgnoreQueryFilters() on bn.BeatId equals b.Id
                    where bn.NodeId == membership.NodeId && bn.SortKey > membership.SortKey
                    orderby bn.SortKey
                    select new { b.Description, b.Title, b.EventSummary }).FirstOrDefaultAsync(ct);

                if (next == null)
                {
                    closesChapter = true;
                    // First beat of the next chapter under the same parent.
                    var chapter = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                        .Where(n => n.Id == membership.NodeId)
                        .Select(n => new { n.ParentNodeId, n.SortKey })
                        .FirstOrDefaultAsync(ct);
                    if (chapter?.ParentNodeId != null)
                    {
                        var nextChapterId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                            .Where(n => n.ParentNodeId == chapter.ParentNodeId && n.SortKey > chapter.SortKey)
                            .OrderBy(n => n.SortKey)
                            .Select(n => (Guid?)n.Id)
                            .FirstOrDefaultAsync(ct);
                        if (nextChapterId != null)
                        {
                            next = await (
                                from bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
                                join b in db.Beats.AsNoTracking().IgnoreQueryFilters() on bn.BeatId equals b.Id
                                where bn.NodeId == nextChapterId.Value
                                orderby bn.SortKey
                                select new { b.Description, b.Title, b.EventSummary }).FirstOrDefaultAsync(ct);
                        }
                    }
                }

                if (next != null)
                    stopBefore = FirstNonEmpty(next.Description, next.EventSummary, next.Title);
            }

            if (verification != null)
            {
                var povId = await verification.GetPovEntityIdAsync(beatId, ct);
                if (povId != null)
                    pov = await db.Entities.AsNoTracking().IgnoreQueryFilters()
                        .Where(e => e.Id == povId.Value).Select(e => e.Name).FirstOrDefaultAsync(ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A brief with only the goal is still a brief; never let bookkeeping block a write.
            log.LogWarning(ex, "[BeatBriefBuilder] partial brief for beat {BeatId}", beatId);
        }

        return new BeatBrief
        {
            Goal = goal,
            StopBefore = stopBefore,
            ClosesChapter = closesChapter,
            MustInclude = NamesIn(goal),
            Pov = pov,
            Subtext = string.IsNullOrWhiteSpace(subtext) ? null : subtext,
            TargetWords = targetWords,
        };
    }

    /// <summary>Canon entity names that occur in the text, longest first, no duplicates.</summary>
    public IReadOnlyList<string> NamesIn(string text)
    {
        if (graph == null || string.IsNullOrWhiteSpace(text)) return [];
        try
        {
            var names = graph.AllNodes()
                .Select(n => n.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n) && n.Length >= 3)
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(n => n.Length);
            var found = new List<string>();
            foreach (var name in names)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(text, $@"\b{System.Text.RegularExpressions.Regex.Escape(name)}\b"))
                    found.Add(name);
            }
            // Drop a name that is merely a token of a longer matched name ("Kyle" when "Kyle Corbin" matched).
            return found.Where(f => !found.Any(o => o != f && o.Contains(f, StringComparison.Ordinal))).ToList();
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "[BeatBriefBuilder] NamesIn failed");
            return [];
        }
    }

    /// <summary>Known canon names for the gate's unknown-noun warning.</summary>
    public IReadOnlyCollection<string> KnownNames()
    {
        try { return graph?.AllNodes().Select(n => n.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToHashSet(StringComparer.Ordinal) ?? []; }
        catch { return []; }
    }

    private static string? FirstNonEmpty(params string?[] xs) => xs.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
}
