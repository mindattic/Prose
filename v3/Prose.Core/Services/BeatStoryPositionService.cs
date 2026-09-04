using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Stamps <see cref="Data.Entities.Beat.StoryPosition"/> — a book's reading order turned into a
/// number, which is the engine's story clock.
///
/// <para><b>Author ruling 2026-09-04.</b> "We are supposed to be using beats not a clock counting
/// hours and minutes." Beat order is the authoritative time axis; the wall clock
/// (<c>InWorldDate</c>, <c>ElapsedMinutesSincePrevious</c>) is an overlay for the two jobs beat
/// counting cannot do — keeping day and night aligned, and making a short fuse add up across the
/// beats it spans.</para>
///
/// <para><b>Why this needs persisting at all,</b> when <c>GetOrderedBeatsAsync</c> already returns
/// beats in order: an in-memory list orders the beats you already fetched, and cannot answer "which
/// state events happened at or before this beat" without loading the whole book first. A column can
/// be filtered and compared in SQL, which is what turns an as-of query into
/// <c>StoryPosition &lt;= target</c> and a duration into the difference of two positions — both
/// exact, and both free.</para>
///
/// <para><b>Null is unknown, never zero.</b> A beat that has never been stamped, or whose book
/// cannot be resolved, keeps a null position, and every consumer must fall back rather than treat
/// it as the start of the book. That is the whole reason
/// <see cref="WorldStateAtBeatService"/> could silently answer with the wrong end of the story.</para>
/// </summary>
public sealed class BeatStoryPositionService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    ILogger<BeatStoryPositionService> log)
{
    public sealed record BookResult(Guid NodeId, string Slug, string Title, int Beats, int Changed);

    public sealed record Report(List<BookResult> Books, int TotalBeats, int TotalChanged, bool Applied)
    {
        public int BooksTouched => Books.Count(b => b.Changed > 0);
    }

    /// <summary>Stamps one book. Positions are 1-based and dense over the book's reading order,
    /// including beats with no prose — a planned beat still occupies a place in the sequence.</summary>
    public async Task<BookResult> StampBookAsync(Guid bookNodeId, bool apply, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var book = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == bookNodeId)
            .Select(n => new { n.Id, n.Slug, n.Title })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {bookNodeId} not found.");

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        var ids = ordered.Select(o => o.Beat.Id).ToList();

        // One query for the whole book rather than a lookup per beat — a 500-beat book would
        // otherwise be 500 round trips, the same N+1 shape that made --continuity groups time out.
        var rows = await db.Beats.Where(b => ids.Contains(b.Id)).ToListAsync(ct);
        var byId = rows.ToDictionary(b => b.Id);

        var changed = 0;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (!byId.TryGetValue(ordered[i].Beat.Id, out var beat)) continue;
            var position = i + 1;
            if (beat.StoryPosition == position) continue;
            if (apply) beat.StoryPosition = position;
            changed++;
        }

        if (apply && changed > 0)
        {
            // Deliberately NOT touching UpdatedAt/TextHash/Stale/Score: a position is derived
            // bookkeeping about where the beat sits, not a change to its prose. Marking beats
            // dirty here would invalidate every hash-gated audit in the engine for nothing.
            await db.SaveChangesAsync(ct);
            log.LogInformation("[beat-positions] {Book}: stamped {Changed} of {Total} beat(s).",
                book.Slug, changed, ordered.Count);
        }

        return new BookResult(book.Id, book.Slug ?? "", book.Title, ordered.Count, changed);
    }

    /// <summary>
    /// Every book in EVERY universe — corpus-wide by design, like <c>--orphan-beats</c> and
    /// <c>--fix-bad-name-matches</c>.
    ///
    /// <para><b>Why <c>IgnoreQueryFilters</c> rather than the ambient scope.</b> A beat's place in
    /// its own book's reading order is structural: it does not depend on which universe the caller
    /// happens to have passed, and there is no such thing as a position that is only true inside
    /// one universe. Scoping this to the ambient universe would mean the caller has to enumerate
    /// every universe slug correctly to get complete coverage — and that failed immediately in
    /// practice on 2026-09-04: a hand-written loop over the slugs I knew about reached 78.3% and
    /// silently missed roughly 2,600 beats belonging to books in a universe I had not named, whose
    /// slug the CLI's own error text was stale about. Coverage that depends on remembering a list
    /// is coverage nobody can trust.</para>
    /// </summary>
    public async Task<Report> StampAllAsync(bool apply, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var books = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Kind == "book")
            .OrderBy(n => n.Title)
            .Select(n => n.Id)
            .ToListAsync(ct);

        var results = new List<BookResult>();
        foreach (var id in books)
        {
            ct.ThrowIfCancellationRequested();
            try { results.Add(await StampBookAsync(id, apply, ct)); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[beat-positions] skipped book {NodeId}", id);
            }
        }

        return new Report(results, results.Sum(r => r.Beats), results.Sum(r => r.Changed), apply);
    }

    /// <summary>
    /// Coverage, for the same reason <c>prose --continuity stats</c> reports anchor coverage: a
    /// consumer that silently falls back on a null position looks exactly like one that found the
    /// right answer, so the ceiling has to be visible before anyone trusts a result.
    /// </summary>
    public async Task<(int Total, int Stamped)> CoverageAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var total = await db.Beats.CountAsync(ct);
        var stamped = await db.Beats.CountAsync(b => b.StoryPosition != null, ct);
        return (total, stamped);
    }

    public sealed record UnstampedGroup(Guid NodeId, string NodeTitle, string NodeKind, int Beats, bool HasBookAncestor);

    /// <summary>
    /// Which nodes hold beats that no book's reading order reaches.
    ///
    /// <para><b>Why this is a corpus-health check, not just a progress bar.</b> A beat with a
    /// <c>BeatNodes</c> row is not an orphan — <c>--orphan-beats</c> reports zero — yet it can
    /// still be invisible to every instrument that walks a book from its root, because its node
    /// has no <c>Kind='book'</c> ancestor. That is exactly how BCODA's "Ghost Period" hid 155
    /// beats (about 30% of the book) through several clean audits: a flat or root-only query
    /// silently missed anything hanging off the wrong parent. Unstamped-but-attached beats are the
    /// same signal, so the number is reported with the nodes that own it rather than left as a
    /// percentage nobody can act on.</para>
    /// </summary>
    public async Task<List<UnstampedGroup>> UnstampedByNodeAsync(int limit = 40, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var groups = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            where b.StoryPosition == null
            group bn by bn.NodeId into g
            select new { NodeId = g.Key, Beats = g.Count() })
            .OrderByDescending(x => x.Beats)
            .Take(limit)
            .ToListAsync(ct);

        var ids = groups.Select(g => g.NodeId).ToList();
        var nodes = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .Select(n => new { n.Id, n.Title, n.Kind, n.ParentNodeId })
            .ToListAsync(ct);
        var byId = nodes.ToDictionary(n => n.Id);

        // Walk each owner up to see whether a book sits above it at all — the difference between
        // "in a universe nobody stamped" and "structurally unreachable".
        var result = new List<UnstampedGroup>();
        foreach (var g in groups)
        {
            var title = byId.TryGetValue(g.NodeId, out var n) ? n.Title : "(node missing)";
            var kind = n?.Kind ?? "?";
            var hasBook = false;
            var walk = n?.ParentNodeId;
            for (var depth = 0; depth < 10 && walk != null; depth++)
            {
                var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == walk)
                    .Select(x => new { x.Kind, x.ParentNodeId })
                    .FirstOrDefaultAsync(ct);
                if (parent == null) break;
                if (parent.Kind == "book") { hasBook = true; break; }
                walk = parent.ParentNodeId;
            }
            if (kind == "book") hasBook = true;
            result.Add(new UnstampedGroup(g.NodeId, title, kind, g.Beats, hasBook));
        }
        return result;
    }
}
