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

            // Resolve nodeId from BeatNodes
            var nodeId = await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.BeatId == beatId && bn.IsEnabled)
                .Select(bn => bn.NodeId)
                .FirstOrDefaultAsync(ct);

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
