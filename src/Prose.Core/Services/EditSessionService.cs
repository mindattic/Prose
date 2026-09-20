using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public class EditSessionService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EditSessionService> log;

    public EditSessionService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<EditSessionService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public async Task<EditSession> StartSessionAsync(
        Guid nodeId, string label, string sessionType = "custom",
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Close any open auto-session first; fail on open named session
        var open = await db.EditSessions
            .Where(s => s.NodeId == nodeId && s.ClosedAt == null)
            .ToListAsync(ct);

        foreach (var s in open)
        {
            if (s.SessionType != "auto")
                throw new InvalidOperationException(
                    $"Session \"{s.Label}\" is already open for this node. Close it first.");
            s.ClosedAt = DateTime.UtcNow;
        }

        var session = new EditSession
        {
            EditSessionId = Guid.NewGuid(),
            NodeId        = nodeId,
            Label         = label,
            SessionType   = sessionType,
            StartedAt     = DateTime.UtcNow,
        };
        db.EditSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<EditSession> CloseSessionAsync(
        Guid? nodeId = null, Guid? sessionId = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        EditSession? session = null;
        if (sessionId.HasValue)
            session = await db.EditSessions.FirstOrDefaultAsync(s => s.EditSessionId == sessionId.Value, ct);
        else if (nodeId.HasValue)
            session = await db.EditSessions.FirstOrDefaultAsync(
                s => s.NodeId == nodeId.Value && s.ClosedAt == null, ct);

        if (session == null)
            throw new InvalidOperationException("No open session found.");

        session.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return session;
    }

    /// <summary>
    /// Called fire-and-forget from NodeWorkbenchService after every prose save.
    /// Finds the active session for the beat's node and logs the beat.
    /// Auto-creates an "auto-YYYY-MM-DD" session if none is open.
    /// Never throws — all exceptions are logged and swallowed.
    /// </summary>
    public async Task TryLogBeatAsync(
        Guid beatId, int priorVersion, string? priorHash,
        CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var nodeId = await ResolveBookNodeIdAsync(db, beatId, ct);
            if (nodeId == default) return;

            // Find or auto-create open session
            var session = await db.EditSessions
                .FirstOrDefaultAsync(s => s.NodeId == nodeId && s.ClosedAt == null, ct);

            if (session == null)
            {
                var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                var autoLabel = $"auto-{today}";
                session = new EditSession
                {
                    EditSessionId = Guid.NewGuid(),
                    NodeId        = nodeId,
                    Label         = autoLabel,
                    SessionType   = "auto",
                    StartedAt     = DateTime.UtcNow,
                };
                db.EditSessions.Add(session);
                await db.SaveChangesAsync(ct);
            }

            // Upsert: on re-edit within same session only update EditedAt
            var existing = await db.EditSessionBeats
                .FirstOrDefaultAsync(
                    esb => esb.EditSessionId == session.EditSessionId && esb.BeatId == beatId, ct);

            if (existing == null)
            {
                db.EditSessionBeats.Add(new EditSessionBeat
                {
                    EditSessionId = session.EditSessionId,
                    BeatId        = beatId,
                    EditedAt      = DateTime.UtcNow,
                    PriorVersion  = priorVersion,
                    PriorTextHash = priorHash,
                });
                session.BeatCount++;
            }
            else
            {
                existing.EditedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EditSession.TryLogBeatAsync failed for beat {BeatId}", beatId);
        }
    }

    /// <summary>
    /// The BOOK node that owns <paramref name="beatId"/>, walking up from its BeatNodes
    /// membership. <c>Guid.Empty</c> when the beat belongs to no node at all.
    ///
    /// BeatNodes points at the CHAPTER a beat lives in (or, for a standalone/leaf book with
    /// no chapter children, at the book itself). Both halves of the Beat&lt;-&gt;Bible&lt;-&gt;Blueprint
    /// sync that consumes these sessions are BOOK-scoped, though: OutlineSyncService resolves
    /// <c>docs/nodes/&lt;CODE&gt;.md</c> from NodeCode (chapter nodes have none) and
    /// BlueprintSyncService looks up NodeStructuralBlueprints by NodeId (blueprints exist only
    /// on book nodes). Keying a session to a chapter therefore made BOTH a guaranteed no-op —
    /// the sync ran clean on every commit and did nothing (found 2026-09-03). Resolving the
    /// book here is what makes the mechanism real.
    ///
    /// Also removes a nondeterministic <c>FirstOrDefault</c>: a beat shared by more than one
    /// chapter (the VIGL dual-beatset shape) used to land in whichever membership row SQL
    /// Server happened to return first.
    /// </summary>
    private static async Task<Guid> ResolveBookNodeIdAsync(
        ProseDbContext db, Guid beatId, CancellationToken ct)
    {
        // IgnoreQueryFilters throughout: beatId is an explicit id the caller already holds,
        // and this runs fire-and-forget under whatever universe scope the writing process
        // happens to have — the same scope leak that broke --close-all-sessions.
        var candidates = await db.BeatNodes.IgnoreQueryFilters().AsNoTracking()
            .Where(bn => bn.BeatId == beatId)
            .OrderBy(bn => bn.SortKey).ThenBy(bn => bn.NodeId)
            .Select(bn => bn.NodeId)
            .ToListAsync(ct);

        foreach (var start in candidates)
        {
            var current = start;
            // Book -> Chapter is the entire legal hierarchy (CLAUDE.md), so one hop suffices;
            // the cap only guards against a cyclic ParentNodeId.
            for (var hop = 0; hop < 8 && current != default; hop++)
            {
                // OfType<BookNode>(): the TPH discriminator is the structural truth, not the
                // free-form Kind label (see Node.cs's own doc comment).
                if (await db.Nodes.IgnoreQueryFilters().OfType<BookNode>()
                        .AnyAsync(n => n.Id == current, ct))
                    return current;

                var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                    .Where(n => n.Id == current)
                    .Select(n => n.ParentNodeId)
                    .FirstOrDefaultAsync(ct);
                if (parent == null || parent.Value == default) break;
                current = parent.Value;
            }
        }

        // No book ancestor found (a beat parented straight to a series node, or a broken
        // tree): record the session against the raw membership target rather than dropping
        // the edit history on the floor.
        return candidates.FirstOrDefault();
    }

    public async Task<List<EditSession>> GetSessionsAsync(
        Guid nodeId, int limit = 20, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EditSessions
            .Where(s => s.NodeId == nodeId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<EditSessionBeat>> GetSessionBeatsAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EditSessionBeats
            .Include(esb => esb.Beat)
            .Where(esb => esb.EditSessionId == sessionId)
            .OrderBy(esb => esb.EditedAt)
            .ToListAsync(ct);
    }

    public async Task<EditSession?> GetSessionAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.EditSessions.FirstOrDefaultAsync(s => s.EditSessionId == sessionId, ct);
    }
}
