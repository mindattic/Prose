using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Discussion;

namespace Prose.WriterUi.Services;

/// <summary>
/// What the Discuss panel needs, in the shapes it needs them.
///
/// <para>Scoped, like <see cref="WriterService"/> and for the same reason: it pins the flow
/// universe for whichever book the circuit has open, and two windows on two books must not share
/// that pin.</para>
///
/// <para>Holds no conversation state of its own. A thread is rows in the database from the moment
/// it is opened, so closing the window mid-exchange loses nothing, and a second window looking at
/// the same beat sees the same discussion.</para>
/// </summary>
public sealed class DiscussionUiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universe,
    DiscussionService discussions,
    DiscussionChatService chat,
    ProposalService proposals,
    PostWriteReviewService review,
    DiscussTargetRegistry targets)
{
    /// <summary>Raised when the exchange could not happen because no provider has a key. The
    /// panel turns this into a link to Settings; nothing was spent.</summary>
    public sealed class NoCredentialsException() : Exception(
        "No API key is configured. Add one in Settings — Claude deliberately has no fallback, "
        + "so nothing is spent until you do.");

    /// <param name="Anchor">Where the thread currently points, and how confident that is.</param>
    public sealed record ThreadRow(
        DiscussionThread Thread,
        AnchorResult Anchor,
        int TurnCount,
        string? LastLine);

    /// <param name="Spoken">This turn arrived by microphone. Shown, because a transcription error
    /// reads exactly like a change of mind six weeks later.</param>
    public sealed record TurnRow(
        string Role,
        IReadOnlyList<DiscussionBlock> Blocks,
        DateTime At,
        double Cost,
        bool Spoken);

    /// <summary>
    /// Pin the ambient universe to the book being discussed.
    ///
    /// <para>Non-optional, exactly as in <see cref="WriterService"/>: every universe-scoped read
    /// behind this service returns nothing at all when the flow universe is unset, so an unpinned
    /// call looks like "this beat has no discussions" rather than like an error.</para>
    /// </summary>
    private async Task ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => (Guid?)n.UniverseId)
            .FirstOrDefaultAsync(ct);
        if (universeId is { } id && id != Guid.Empty) universe.SetFlowUniverse(id);
    }

    /// <summary>Every thread on a beat, re-anchored against the beat as it stands now.</summary>
    public async Task<IReadOnlyList<ThreadRow>> ForBeatAsync(
        Guid bookNodeId, Guid beatId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        var rows = await discussions.ForTargetAsync(DiscussionTargetKind.Beat, beatId, null, ct);
        return rows.Select(r => new ThreadRow(r.Thread, r.Anchor, r.TurnCount, r.LastLine)).ToList();
    }

    public async Task<IReadOnlyList<TurnRow>> TurnsAsync(Guid threadId, CancellationToken ct = default)
    {
        var turns = await discussions.TurnsAsync(threadId, ct);
        return turns
            .Select(t => new TurnRow(t.Role, DiscussionContent.Deserialize(t.ContentJson), t.At, t.Cost,
                                     t.InputMode == DiscussionInputMode.Voice))
            .ToList();
    }

    /// <summary>
    /// Open a thread on a passage the author selected, and answer their first question.
    /// </summary>
    /// <param name="inputMode">Typed or spoken. Recorded because a transcription error reads
    /// exactly like a change of mind, and this column plus <paramref name="audioPath"/> are the
    /// only things that can tell them apart later.</param>
    /// <returns>Null when the selection can no longer be found in the beat — which means the text
    /// moved between the right-click and the question, and silently anchoring somewhere else would
    /// be worse than saying so.</returns>
    /// <param name="onTextDelta">Fed the answer as it is generated, so the panel can show it
    /// arriving and speak the first sentence while the rest is still being written. Null for the
    /// buffered path.</param>
    public async Task<DiscussionThread?> StartAsync(
        Guid bookNodeId, Guid beatId,
        ProseEditorSpan span, string question, string intent,
        string inputMode = DiscussionInputMode.Typed,
        string? audioPath = null,
        Func<string, Task>? onTextDelta = null,
        CancellationToken ct = default)
    {
        // Checked before the thread is created, not after: an unconfigured install would
        // otherwise leave a question anchored in the book that was never asked of anyone.
        if (!await chat.IsConfiguredAsync()) throw new NoCredentialsException();

        await ScopeToBookAsync(bookNodeId, ct);

        var handler = targets.For(DiscussionTargetKind.Beat);
        if (handler is null) return null;

        var subject = await handler.LoadAsync(beatId, null, ct);
        if (subject is null) return null;

        var anchor = TextAnchoring.Locate(subject.Text, Plain(span.Quote), Plain(span.Prefix), Plain(span.Suffix));
        if (anchor is null) return null;

        var thread = await discussions.StartAsync(
            bookNodeId, DiscussionTargetKind.Beat, beatId, null,
            anchor, subject.TextHash,
            [new DiscussionBlock.Text(question)],
            intent: intent,
            inputMode: inputMode,
            audioPath: audioPath,
            ct: ct);

        await AnswerAsync(bookNodeId, beatId, thread.Id, anchor.Quote, question, intent, onTextDelta, ct);
        return thread;
    }

    /// <summary>Ask a follow-up on an existing thread.</summary>
    public async Task AskAsync(
        Guid bookNodeId, Guid beatId, Guid threadId, string question, string intent,
        string inputMode = DiscussionInputMode.Typed,
        string? audioPath = null,
        Func<string, Task>? onTextDelta = null,
        CancellationToken ct = default)
    {
        if (!await chat.IsConfiguredAsync()) throw new NoCredentialsException();

        await ScopeToBookAsync(bookNodeId, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var quote = await db.DiscussionThreads.AsNoTracking()
            .Where(t => t.Id == threadId).Select(t => t.AnchorQuote).FirstOrDefaultAsync(ct) ?? "";

        await discussions.AddTurnAsync(threadId, DiscussionRole.Author,
            [new DiscussionBlock.Text(question)], intent: intent,
            inputMode: inputMode, audioPath: audioPath, ct: ct);

        await AnswerAsync(bookNodeId, beatId, threadId, quote, question, intent, onTextDelta, ct);
    }

    private async Task AnswerAsync(
        Guid bookNodeId, Guid beatId, Guid threadId, string quote, string question,
        string intent, Func<string, Task>? onTextDelta, CancellationToken ct)
    {
        var prior = await discussions.TurnsAsync(threadId, ct);

        // The turn just written is the question being asked; including it again as history would
        // have the model answer it twice.
        var history = prior.Count > 0 ? prior.Take(prior.Count - 1).ToList() : prior;

        var reply = await chat.AskAsync(
            bookNodeId, beatId, quote, question, intent, history, ct, onTextDelta);

        // Nothing was spent and nothing was said — do not record an empty assistant turn that
        // would read, later, as the assistant having had nothing to say.
        if (reply.CredentialsMissing) throw new NoCredentialsException();

        var blocks = reply.Blocks;

        // An agreed change becomes a ticket, not a write. The proposal is raised here so the
        // turn that offered it and the proposal itself are stored together — a turn saying
        // "here is the replacement" with no proposal behind it would be a dead end the author
        // could read but not act on.
        if (reply.Draft is { Length: > 0 } draft)
        {
            var proposal = await proposals.RaiseAsync(
                threadId, beatId, draft,
                rationale: DiscussionContent.Summarize(reply.Blocks, 400), ct);
            blocks = [.. reply.Blocks, new DiscussionBlock.Proposal(proposal.Id)];
        }

        await discussions.AddTurnAsync(threadId, DiscussionRole.Assistant, blocks,
            cost: reply.Cost, ct: ct);

        // Stamp the author's turn with what it turned out to be. Only "auto" needs this; an
        // explicit toggle was already right when the turn was written.
        if (intent == DiscussionIntent.Auto)
            await discussions.SetLatestAuthorIntentAsync(threadId, reply.ResolvedIntent, ct);
    }

    // ── Proposals ──────────────────────────────────────────────────────────

    /// <param name="Applied">Already written. Kept in the thread as the record of what was done.</param>
    public sealed record ProposalRow(
        Guid Id, string Replacement, string Status, bool Applied, string? Blocker);

    /// <summary>One proposal, with whether it could still be applied right now.</summary>
    public async Task<ProposalRow?> ProposalAsync(Guid proposalId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var p = await db.ChangeProposals.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == proposalId, ct);
        if (p is null) return null;

        return new ProposalRow(p.Id, p.NewValue, p.Status,
                               p.Status == ProposalService.StatusApplied, null);
    }

    /// <summary>
    /// Write it. The author has confirmed the request in conversation and approved this specific
    /// replacement; a refusal from here means the beat moved underneath it, which is the one case
    /// where not writing is the correct outcome.
    /// </summary>
    public async Task<SpanWriteOutcome> ApproveAsync(
        Guid bookNodeId, Guid beatId, Guid threadId, Guid proposalId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        var outcome = await proposals.ApplyAsync(proposalId, ct);
        if (!outcome.Applied) return outcome;

        // A request must not close with a mismatch unresolved — so the checks run here, inside the
        // approval, and what they find goes into the conversation as the assistant's own turn
        // rather than into a findings table nobody is looking at.
        var mismatches = await review.ReviewAsync(bookNodeId, beatId, outcome.RemovedText, ct);
        if (mismatches.Count == 0) return outcome;

        var blocks = new List<DiscussionBlock>
        {
            new DiscussionBlock.Text(
                mismatches.Count == 1
                    ? "That change left one thing disagreeing with the record."
                    : $"That change left {mismatches.Count} things disagreeing with the record."),
        };

        foreach (var m in mismatches)
        {
            blocks.Add(new DiscussionBlock.Text(
                $"**{m.Headline}**" + (m.Detail is null ? "" : $"  \n{m.Detail}")));
            // A choice, not a decision. Which way a mismatch resolves is a judgement about the
            // story, and the author's pick is recorded as their own turn in the same log as the
            // reasoning that produced it.
            blocks.Add(new DiscussionBlock.Choice(
                "How should that be settled?",
                [.. m.Fork.Select(f => new ChoiceOption(f))]));
        }

        await discussions.AddTurnAsync(threadId, DiscussionRole.Assistant, blocks, ct: ct);
        return outcome;
    }

    public async Task RejectProposalAsync(
        Guid bookNodeId, Guid proposalId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        await proposals.RejectAsync(proposalId, ct: ct);
    }

    public Task ResolveAsync(Guid threadId, CancellationToken ct = default)
        => discussions.SetStateAsync(threadId, DiscussionThreadState.Resolved, ct);

    public Task ReopenAsync(Guid threadId, CancellationToken ct = default)
        => discussions.SetStateAsync(threadId, DiscussionThreadState.Live, ct);

    /// <summary>The editor sends beat markup; anchors live in reader-visible text.</summary>
    private static string Plain(string markup)
        => BeatDiscussTarget.PlainText(markup);

    /// <summary>Mirror of the editor component's selected-span record, so Core types do not have
    /// to know about a Razor component.</summary>
    public sealed record ProseEditorSpan(string Quote, string Prefix, string Suffix);
}
