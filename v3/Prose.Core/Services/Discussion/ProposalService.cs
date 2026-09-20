using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Discussion;

/// <summary>A proposal with the thread it came out of, resolved against the beat as it stands.</summary>
/// <param name="StillApplies">False when the passage has moved or gone since the proposal was
/// raised. Shown, so the author is never offered an Approve button that would refuse.</param>
public sealed record PendingProposal(
    ChangeProposal Proposal,
    Guid BeatId,
    string AnchorQuote,
    bool StillApplies,
    string? Blocker);

/// <summary>
/// Turning an agreed change into a written one — and not one character further.
///
/// <para><b>Nothing here writes prose on its own (RFC 0009).</b> A discussion that reaches an
/// agreed change raises a <see cref="ChangeProposal"/>, the author approves that specific
/// proposal, and only then is a beat written — with <see cref="BeatWriteReason.AuthorApprovedProposal"/>,
/// which exists so the forever-record can tell an LLM's words the author approved from the
/// author's own typing.</para>
///
/// <para><b>The anchor lives on the thread, not on the proposal.</b> A thread already stores the
/// quote plus its surroundings and heals its own position every time anyone looks at the beat, so
/// pointing the proposal at the thread means there is one anchor rather than two that can
/// disagree. It also means no migration: <see cref="ChangeProposal"/> carries the old and new text
/// and a request id, which is all this needs.</para>
///
/// <para><b>Re-anchored at apply time, never at raise time.</b> A proposal can sit for minutes
/// while autosave, a CLI command or a second window writes to the same beat. Offsets captured when
/// it was raised would by then point at different words, and the whole point of the exercise is
/// that they do not.</para>
/// </summary>
public sealed class ProposalService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    ILogger<ProposalService> log)
{
    /// <summary>Encoded in <see cref="ChangeProposal.Target"/>. A string, like every other
    /// polymorphic discriminator here, so a new proposable surface costs no migration.</summary>
    public const string BeatSpanTarget = "beat-span";

    public const string StatusProposed = "proposed";
    public const string StatusApplied = "applied";
    public const string StatusRejected = "rejected";
    public const string StatusRefused = "refused";

    /// <summary>
    /// Record an agreed change, without making it.
    /// </summary>
    /// <param name="threadId">The discussion this came out of. Carries the anchor.</param>
    /// <param name="replacement">What the passage should become. Empty is a deletion.</param>
    public async Task<ChangeProposal> RaiseAsync(
        Guid threadId,
        Guid beatId,
        string replacement,
        string rationale,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var thread = await db.DiscussionThreads.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == threadId, ct)
            ?? throw new InvalidOperationException($"Discussion {threadId} not found.");

        var proposal = new ChangeProposal
        {
            UniverseId = thread.UniverseId,
            Target = $"{BeatSpanTarget}:{beatId:D}",
            OldValue = thread.AnchorQuote,
            NewValue = replacement,
            Rationale = rationale,
            VerificationPlan =
                "Everything outside the anchored passage must come back byte-identical, or the "
                + "write is refused. Re-anchored against the beat as it stands at apply time.",
            Status = StatusProposed,
            RequestId = threadId.ToString("D"),
        };

        db.ChangeProposals.Add(proposal);
        await db.SaveChangesAsync(ct);
        return proposal;
    }

    /// <summary>Everything still waiting on the author for one book, newest first.</summary>
    public async Task<IReadOnlyList<PendingProposal>> PendingForBookAsync(
        Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var threads = await db.DiscussionThreads.AsNoTracking()
            .Where(t => t.BookNodeId == bookNodeId)
            .Select(t => new { t.Id, t.AnchorQuote })
            .ToListAsync(ct);
        if (threads.Count == 0) return [];

        var ids = threads.Select(t => t.Id.ToString("D")).ToList();
        var proposals = await db.ChangeProposals.AsNoTracking()
            .Where(p => p.Status == StatusProposed && ids.Contains(p.RequestId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        var rows = new List<PendingProposal>(proposals.Count);
        foreach (var proposal in proposals)
        {
            if (ParseBeatId(proposal.Target) is not { } beatId) continue;

            // Dry-run the real guard rather than a cheaper approximation. An Approve button the
            // author presses and that then refuses is worse than one that was never offered.
            var check = await DryRunAsync(proposal, beatId, ct);
            rows.Add(new PendingProposal(
                proposal, beatId, proposal.OldValue,
                check.Applied, check.Applied ? null : check.Reason));
        }
        return rows;
    }

    /// <summary>
    /// Apply an approved proposal.
    /// </summary>
    /// <remarks>
    /// The author has already confirmed the request in conversation and approved this specific
    /// proposal; this is the write. A refusal here is not a failure of the author's intent — it
    /// means the beat moved underneath the proposal, and writing anyway is exactly what must not
    /// happen.
    /// </remarks>
    public async Task<SpanWriteOutcome> ApplyAsync(Guid proposalId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var proposal = await db.ChangeProposals.FirstOrDefaultAsync(p => p.Id == proposalId, ct)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        if (proposal.Status != StatusProposed)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.NoChange,
                $"This proposal has already been {proposal.Status}. Nothing was written.");

        if (ParseBeatId(proposal.Target) is not { } beatId)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.PassageGone,
                "This proposal does not name a beat span. Nothing was written.");

        var outcome = await DryRunAsync(proposal, beatId, ct);
        if (!outcome.Applied)
        {
            // Recorded rather than left pending. A proposal that can no longer be applied should
            // stop being offered, and WHY it stopped is part of the record of the conversation.
            proposal.Status = StatusRefused;
            proposal.VerificationPlan += $"\nRefused {DateTime.UtcNow:u}: {outcome.Reason}";
            await db.SaveChangesAsync(ct);
            log.LogWarning("Span write refused for proposal {Id}: {Reason}", proposalId, outcome.Reason);
            return outcome;
        }

        // The workbench, not a direct row update: it re-derives entity tags, bumps the version and
        // stamps the write reason. FindingApplyService used to bypass all three.
        await workbench.UpdateBeatTextAsync(
            beatId, outcome.NewText!, BeatWriteReason.AuthorApprovedProposal, ct: ct);

        proposal.Status = StatusApplied;
        await db.SaveChangesAsync(ct);
        return outcome;
    }

    public async Task RejectAsync(Guid proposalId, string? why = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var proposal = await db.ChangeProposals.FirstOrDefaultAsync(p => p.Id == proposalId, ct);
        if (proposal is null || proposal.Status != StatusProposed) return;

        proposal.Status = StatusRejected;
        if (!string.IsNullOrWhiteSpace(why)) proposal.Rationale += $"\nRejected: {why}";
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// What would happen, without writing anything.
    ///
    /// <para>Shared by the listing and the apply so they cannot disagree — a pending row that says
    /// "ready" and an apply that refuses would be two different implementations of the same
    /// question.</para>
    /// </summary>
    private async Task<SpanWriteOutcome> DryRunAsync(
        ChangeProposal proposal, Guid beatId, CancellationToken ct)
    {
        if (!Guid.TryParse(proposal.RequestId, out var threadId))
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.PassageGone,
                "This proposal is not linked to a discussion, so its anchor cannot be resolved.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var thread = await db.DiscussionThreads.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == threadId, ct);
        if (thread is null)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.PassageGone,
                "The discussion this came from is gone, so its anchor cannot be resolved.");

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat is null)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.PassageGone,
                "That beat no longer exists.");

        var anchor = new TextAnchor(
            thread.AnchorQuote, thread.AnchorPrefix, thread.AnchorSuffix,
            thread.AnchorStart, thread.AnchorEnd);

        return SpanWrite.Apply(beat.Text, anchor, proposal.NewValue);
    }

    /// <summary>The beat id out of <c>beat-span:{guid}</c>, or null when the target is something
    /// else. Unknown targets are skipped rather than guessed at — ChangeProposal is shared with
    /// canon, entity and structural proposals that this service must not touch.</summary>
    public static Guid? ParseBeatId(string? target)
    {
        if (string.IsNullOrEmpty(target)) return null;
        var prefix = BeatSpanTarget + ":";
        if (!target.StartsWith(prefix, StringComparison.Ordinal)) return null;
        return Guid.TryParse(target[prefix.Length..], out var id) ? id : null;
    }
}
