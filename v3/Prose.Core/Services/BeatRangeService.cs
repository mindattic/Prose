using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>Tri-state result of <see cref="BeatRangeService.CheckBeatInRangeAsync"/>.
/// <see cref="InRange"/> is null when the window can't be reliably evaluated (a bound beat
/// lives in a different book, or the checked beat / a bound beat is a flagged anachrony) —
/// callers must not treat null as either true or false; see each consumer's own documented
/// default for how it handles indeterminate.</summary>
public sealed record BeatRangeResult(bool? InRange, string? Reason = null);

/// <summary>
/// Beat-scoped Edge validity — replaces the dead DateTime StoryValidFrom/StoryValidUntil
/// mechanism (2026-09-02 investigation: nothing in the live generation pipeline ever supplies a
/// real in-fiction story-time; <see cref="ConsequenceService"/> calls it with a hard-coded null,
/// <see cref="GearCarryEnforcer"/> is never called from generation at all, and the one writer,
/// BeatStateExtractor, is wired to the legacy Chapter/ChapterBeat model, not the modern
/// Nodes/Beats/BeatNodes schema).
///
/// Deliberately does NOT depend on <see cref="NodeWorkbenchService"/> — that would create a DI
/// cycle (NodeWorkbenchService's own constructor needs PostBeatValidationService →
/// GearCarryEnforcer → BeatRangeService), and NodeWorkbenchService carries a heavy
/// TTS/audio-store/validation-hook dependency chain that has no business being pulled into
/// generation-time constraint builders like ConsequenceService/GearCarryEnforcer anyway. Instead
/// calls <see cref="NodeWorkbenchService.WalkAsync"/> directly — it's `internal static` and
/// self-contained (only needs a <see cref="ProseDbContext"/>), the same reading-order walk
/// <see cref="NodeWorkbenchService.GetOrderedBeatsAsync"/> itself wraps.
/// </summary>
public class BeatRangeService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>
    /// Is <paramref name="beatId"/> within the reading-order window
    /// [<paramref name="fromBeatId"/>, <paramref name="untilBeatId"/>) of its own book?
    ///
    /// Returns <see cref="BeatRangeResult.InRange"/> = null (indeterminate, not a false
    /// positive/negative) when:
    ///  - a non-null bound beat resolves to a DIFFERENT book than <paramref name="beatId"/>
    ///    (cross-book chronology isn't resolved by this method — no code in this repo merges
    ///    two books' beats into one ordered sequence; Node.PreviousNodeId only gates
    ///    gateway/sequel commandment selection, not ordering);
    ///  - <paramref name="beatId"/> or a same-book bound beat has a
    ///    BeatBlueprintDecision.AnachronyType of Flashback/FlashForward/Parallel — reading order
    ///    (BeatNodes.SortKey) isn't a reliable proxy for story order there.
    /// </summary>
    public async Task<BeatRangeResult> CheckBeatInRangeAsync(
        Guid beatId, Guid? fromBeatId, Guid? untilBeatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var bookId = await ResolveOwningBookIdAsync(db, beatId, ct);
        if (bookId == null)
            return new BeatRangeResult(null, $"beat {beatId} has no resolvable owning book (orphaned beat?)");

        if (fromBeatId == null && untilBeatId == null)
            return new BeatRangeResult(true); // unbounded on both sides — trivially in range

        var ordered = new List<NodeWorkbenchService.OrderedBeat>();
        await NodeWorkbenchService.WalkAsync(db, bookId.Value, ordered, new HashSet<Guid>(), includeDisabled: false, ct);
        var beatOrdinal = ordered.FindIndex(ob => ob.Beat.Id == beatId);
        if (beatOrdinal < 0)
            return new BeatRangeResult(null, $"beat {beatId} not found in its own book's reading order (disabled/orphaned BeatNodes row?)");

        int? fromOrdinal = null, untilOrdinal = null;
        if (fromBeatId != null)
        {
            var fromBookId = await ResolveOwningBookIdAsync(db, fromBeatId.Value, ct);
            if (fromBookId != bookId)
                return new BeatRangeResult(null, $"ValidFromBeatId {fromBeatId} is in a different book than beat {beatId} — cross-book bound, not resolved");
            fromOrdinal = ordered.FindIndex(ob => ob.Beat.Id == fromBeatId);
        }
        if (untilBeatId != null)
        {
            var untilBookId = await ResolveOwningBookIdAsync(db, untilBeatId.Value, ct);
            if (untilBookId != bookId)
                return new BeatRangeResult(null, $"ValidUntilBeatId {untilBeatId} is in a different book than beat {beatId} — cross-book bound, not resolved");
            untilOrdinal = ordered.FindIndex(ob => ob.Beat.Id == untilBeatId);
        }

        var anachronyCheckIds = new List<Guid> { beatId };
        if (fromBeatId != null) anachronyCheckIds.Add(fromBeatId.Value);
        if (untilBeatId != null) anachronyCheckIds.Add(untilBeatId.Value);
        var anachronies = await db.BeatBlueprintDecisions.AsNoTracking()
            .Where(d => anachronyCheckIds.Contains(d.BeatId)
                     && d.AnachronyType != null && d.AnachronyType != "Linear")
            .Select(d => new { d.BeatId, d.AnachronyType })
            .ToListAsync(ct);
        if (anachronies.Count > 0)
        {
            var flagged = string.Join(", ", anachronies.Select(a => $"{a.BeatId}={a.AnachronyType}"));
            return new BeatRangeResult(null, $"reading order unreliable — flagged anachrony beat(s): {flagged}");
        }

        var inRange = (fromOrdinal == null || beatOrdinal >= fromOrdinal)
                    && (untilOrdinal == null || beatOrdinal < untilOrdinal);
        return new BeatRangeResult(inRange);
    }

    /// <summary>Walk BeatNodes → chapter Node → ParentNodeId ancestors up to the first
    /// Kind=="book" node. Small, bounded ancestor walk (book → chapter → beat is at most a
    /// couple hops) — not the descendant-walk case CLAUDE.md's hard rule is about.</summary>
    private static async Task<Guid?> ResolveOwningBookIdAsync(ProseDbContext db, Guid beatId, CancellationToken ct)
    {
        var nodeId = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.BeatId == beatId)
            .Select(bn => (Guid?)bn.NodeId)
            .FirstOrDefaultAsync(ct);
        if (nodeId == null) return null;

        var currentId = nodeId.Value;
        for (var hop = 0; hop < 32; hop++) // generous bound against a corrupt cycle
        {
            var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                .Where(n => n.Id == currentId)
                .Select(n => new { n.Id, n.Kind, n.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (node == null) return null;
            if (node.Kind == "book") return node.Id;
            if (node.ParentNodeId == null) return null; // no book ancestor found
            currentId = node.ParentNodeId.Value;
        }
        return null; // exceeded hop bound — treat as unresolved rather than loop forever
    }
}
