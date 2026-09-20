using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Discussion;

/// <summary>A thread together with where it currently points in its target's text.</summary>
public sealed record AnchoredThread(
    DiscussionThread Thread,
    AnchorResult Anchor,
    int TurnCount,
    string? LastLine);

/// <summary>
/// Reading and writing discussions.
///
/// <para><b>This service never touches prose.</b> It stores what was said. A thread that reaches
/// an agreed change raises a <see cref="ChangeProposal"/>, and the author approves that before
/// anything is written — RFC 0009's rule that nothing in this system writes prose on its own.</para>
/// </summary>
public sealed class DiscussionService(
    IDbContextFactory<ProseDbContext> dbFactory,
    DiscussTargetRegistry targets)
{
    /// <summary>
    /// Open a thread on a span, with the author's opening turn.
    /// </summary>
    public async Task<DiscussionThread> StartAsync(
        Guid bookNodeId,
        string targetKind,
        Guid targetId,
        string? targetField,
        TextAnchor anchor,
        string? textHash,
        IReadOnlyList<DiscussionBlock> openingTurn,
        string intent = DiscussionIntent.Clarify,
        string inputMode = DiscussionInputMode.Typed,
        string? audioPath = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var thread = new DiscussionThread
        {
            BookNodeId = bookNodeId,
            TargetKind = targetKind,
            TargetId = targetId,
            TargetField = targetField,
            // Only beat targets get the cascading key; for anything else the column stays null
            // and TargetId alone identifies the row.
            BeatId = targetKind is DiscussionTargetKind.Beat or DiscussionTargetKind.BeatIntent
                ? targetId
                : null,
            AnchorQuote = anchor.Quote,
            AnchorPrefix = anchor.Prefix,
            AnchorSuffix = anchor.Suffix,
            AnchorStart = anchor.Start,
            AnchorEnd = anchor.End,
            AnchoredTextHash = textHash,
            State = DiscussionThreadState.Live,
            Title = Truncate(DiscussionContent.Summarize(openingTurn), 200),
            UpdatedAt = DateTime.UtcNow,
        };

        db.DiscussionThreads.Add(thread);
        db.DiscussionTurns.Add(new DiscussionTurn
        {
            ThreadId = thread.Id,
            Role = DiscussionRole.Author,
            Intent = intent,
            ContentJson = DiscussionContent.Serialize(openingTurn),
            InputMode = inputMode,
            AudioPath = audioPath,
        });

        await db.SaveChangesAsync(ct);
        return thread;
    }

    /// <summary>Append a turn. Assistant turns carry what the call cost, so the panel can show the
    /// running total rather than leaving the author to guess.</summary>
    public async Task<DiscussionTurn> AddTurnAsync(
        Guid threadId,
        string role,
        IReadOnlyList<DiscussionBlock> blocks,
        string? intent = null,
        string inputMode = DiscussionInputMode.Typed,
        int? llmCallHistoryId = null,
        Guid? costScopeId = null,
        double cost = 0,
        string? audioPath = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var turn = new DiscussionTurn
        {
            ThreadId = threadId,
            Role = role,
            Intent = intent,
            ContentJson = DiscussionContent.Serialize(blocks),
            InputMode = inputMode,
            LlmCallHistoryId = llmCallHistoryId,
            CostScopeId = costScopeId,
            Cost = cost,
            AudioPath = audioPath,
        };
        db.DiscussionTurns.Add(turn);

        // Tracked rather than ExecuteUpdate so the turn and its thread's timestamp land in one
        // transaction: an ExecuteUpdate runs immediately and would leave the thread marked as
        // having activity that the failed SaveChanges never actually stored.
        var thread = await db.DiscussionThreads.FirstOrDefaultAsync(t => t.Id == threadId, ct);
        if (thread is not null) thread.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return turn;
    }

    /// <summary>
    /// Record what the author's most recent turn turned out to be asking for.
    ///
    /// <para>Needed because intent can be inferred rather than chosen: the turn has to be written
    /// before the exchange (it is the question being asked), but when the toggle was on
    /// <c>auto</c> nothing knows whether it was a question or a request until the reply comes
    /// back. Stamping it afterwards is what keeps <c>auto</c> from ever reaching the column — a
    /// stored "auto" would mean the log could not say what the assistant thought it was doing.</para>
    /// </summary>
    public async Task SetLatestAuthorIntentAsync(
        Guid threadId, string intent, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var turn = await db.DiscussionTurns
            .Where(t => t.ThreadId == threadId && t.Role == DiscussionRole.Author)
            .OrderByDescending(t => t.At).ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);
        if (turn is null || turn.Intent == intent) return;

        turn.Intent = intent;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DiscussionTurn>> TurnsAsync(Guid threadId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.DiscussionTurns.AsNoTracking()
            .Where(t => t.ThreadId == threadId)
            .OrderBy(t => t.At).ThenBy(t => t.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Every thread on a target, each resolved against the target's text as it stands now.
    ///
    /// <para>Re-anchoring happens on read and is written back when it moves, so a thread heals
    /// itself the first time anyone looks at the beat after an edit. A thread whose quote has gone
    /// is marked <c>detached</c> rather than deleted: a conversation about a line that was cut is
    /// part of the record of why it was cut.</para>
    /// </summary>
    public async Task<IReadOnlyList<AnchoredThread>> ForTargetAsync(
        string targetKind, Guid targetId, string? targetField = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var threads = await db.DiscussionThreads
            .Where(t => t.TargetKind == targetKind && t.TargetId == targetId
                        && t.TargetField == targetField)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        if (threads.Count == 0) return [];

        var subject = targets.For(targetKind) is { } handler
            ? await handler.LoadAsync(targetId, targetField, ct)
            : null;

        var counts = await db.DiscussionTurns
            .Where(t => threads.Select(x => x.Id).Contains(t.ThreadId))
            .GroupBy(t => t.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ThreadId, x => x.Count, ct);

        var lastLines = await db.DiscussionTurns
            .Where(t => threads.Select(x => x.Id).Contains(t.ThreadId))
            .GroupBy(t => t.ThreadId)
            .Select(g => new
            {
                ThreadId = g.Key,
                Json = g.OrderByDescending(t => t.At).ThenByDescending(t => t.Id)
                        .Select(t => t.ContentJson).First(),
            })
            .ToDictionaryAsync(x => x.ThreadId, x => x.Json, ct);

        var results = new List<AnchoredThread>(threads.Count);
        var dirty = false;

        foreach (var thread in threads)
        {
            var anchor = new TextAnchor(thread.AnchorQuote, thread.AnchorPrefix,
                                        thread.AnchorSuffix, thread.AnchorStart, thread.AnchorEnd);

            // No handler, or the target is gone: say nothing rather than claim a position.
            var resolved = subject is null
                ? new AnchorResult(AnchorOutcome.Detached, 0, 0)
                : TextAnchoring.Resolve(subject.Text, anchor);

            if (resolved.Found && (thread.AnchorStart != resolved.Start || thread.AnchorEnd != resolved.End))
            {
                thread.AnchorStart = resolved.Start;
                thread.AnchorEnd = resolved.End;
                thread.AnchoredTextHash = subject?.TextHash;
                dirty = true;
            }

            // A resolved thread stays resolved; only the live/detached pair tracks the text.
            if (thread.State != DiscussionThreadState.Resolved)
            {
                var state = resolved.Found ? DiscussionThreadState.Live : DiscussionThreadState.Detached;
                if (thread.State != state) { thread.State = state; dirty = true; }
            }

            results.Add(new AnchoredThread(
                thread,
                resolved,
                counts.GetValueOrDefault(thread.Id),
                lastLines.TryGetValue(thread.Id, out var json)
                    ? DiscussionContent.Summarize(DiscussionContent.Deserialize(json))
                    : null));
        }

        if (dirty) await db.SaveChangesAsync(ct);
        return results;
    }

    /// <summary>Threads across a whole book, newest activity first — the queue the author works
    /// through, and where detached threads surface instead of disappearing.</summary>
    public async Task<IReadOnlyList<DiscussionThread>> ForBookAsync(
        Guid bookNodeId, string? state = null, int take = 200, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var q = db.DiscussionThreads.AsNoTracking().Where(t => t.BookNodeId == bookNodeId);
        if (state is not null) q = q.Where(t => t.State == state);
        return await q.OrderByDescending(t => t.UpdatedAt).Take(take).ToListAsync(ct);
    }

    public async Task SetStateAsync(Guid threadId, string state, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.DiscussionThreads
            .Where(t => t.Id == threadId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.State, state)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);
    }

    private static string? Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? null : s.Length <= max ? s : s[..max];
}
