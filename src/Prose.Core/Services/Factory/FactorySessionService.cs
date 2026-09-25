using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

public sealed record SessionStartResult(Guid SessionId, string? LastSummaryJson, DateTime? LastEndedAt, IReadOnlyList<(Guid Id, DateTime StartedAt)> OtherOpenSessions);

/// <summary>
/// Claude Code sessions as rows: started by the SessionStart hook, ended by <c>/quicksave</c>.
/// Replaces the quicksave markdown files and the queue JSON.
///
/// <para><b>No unrecorded ideas.</b> A session's end summary lists its decisions, and each one must
/// point at a Ruling or WorkOrder that exists and was written or touched during this session —
/// otherwise the end is refused and the session stays open until the decision is recorded.</para>
/// </summary>
public sealed class FactorySessionService(IDbContextFactory<ProseDbContext> dbFactory)
{
    private (DateTime At, Guid? Id) currentCache = (DateTime.MinValue, null);

    public async Task<SessionStartResult> StartAsync(string? claudeSessionId, string? gitHead, string? startActionJson, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var last = await db.FactorySessions.AsNoTracking()
            .Where(s => s.EndedAt != null).OrderByDescending(s => s.EndedAt).FirstOrDefaultAsync(ct);
        var since = DateTime.UtcNow.AddHours(-24);
        var claudeId = Trunc(claudeSessionId, 64);
        var open = await db.FactorySessions.AsNoTracking()
            .Where(s => s.EndedAt == null && s.StartedAt >= since)
            .OrderByDescending(s => s.StartedAt).Select(s => new { s.Id, s.StartedAt, s.ClaudeSessionId }).ToListAsync(ct);
        // A Claude session that compacts or resumes fires SessionStart again under the same id. That is
        // this session continuing, not a second one: resume its row, and never report it as "other".
        var mine = claudeId == null ? [] : open.Where(s => s.ClaudeSessionId == claudeId).ToList();
        var others = open.Where(s => claudeId == null || s.ClaudeSessionId != claudeId).ToList();
        currentCache = (DateTime.MinValue, null);
        if (mine.Count > 0)
            return new SessionStartResult(mine[0].Id, last?.EndSummaryJson, last?.EndedAt,
                others.Select(o => (o.Id, o.StartedAt)).ToList());

        var row = new FactorySession
        {
            ClaudeSessionId = claudeId,
            GitHeadStart = Trunc(gitHead, 40),
            StartActionJson = startActionJson,
        };
        db.FactorySessions.Add(row);
        await db.SaveChangesAsync(ct);
        return new SessionStartResult(row.Id, last?.EndSummaryJson, last?.EndedAt,
            others.Select(o => (o.Id, o.StartedAt)).ToList());
    }

    /// <summary>End a session with its summary. Refused (returns the problems) when any decision is
    /// not backed by a Ruling or WorkOrder written or touched during the session.</summary>
    public async Task<(bool Ok, IReadOnlyList<string> Problems, Guid? SessionId)> EndAsync(
        Guid? sessionId, string summaryJson, string? gitHead, CancellationToken ct = default)
    {
        JsonObject summary;
        try { summary = JsonNode.Parse(summaryJson) as JsonObject ?? throw new FormatException(); }
        catch { return (false, ["the summary must be a JSON object {done[], decisions[{text, rulingId|orderId}], next}."], sessionId); }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        FactorySession? session;
        if (sessionId is { } sid)
            session = await db.FactorySessions.FirstOrDefaultAsync(s => s.Id == sid, ct);
        else
        {
            // Without an id, "the open session" is only unambiguous when there is exactly one.
            // Guessing the newest ended another session's row when several shared the tree.
            var open = await db.FactorySessions.Where(s => s.EndedAt == null)
                .OrderByDescending(s => s.StartedAt).Take(10).ToListAsync(ct);
            if (open.Count > 1)
                return (false, [$"{open.Count} sessions are open ({string.Join(", ", open.Select(s => s.Id))}); pass the id the start hook printed as THIS SESSION."], null);
            session = open.FirstOrDefault();
        }
        if (session == null) return (false, ["no open session to end."], sessionId);
        // An explicit id must still name an OPEN session: re-ending one would overwrite the
        // summary the next start hook shows as LAST SESSION.
        if (session.EndedAt != null) return (false, [$"session {session.Id} already ended at {session.EndedAt:u}."], session.Id);

        var problems = new List<string>();
        if (summary["next"] is null) problems.Add("the summary has no 'next'.");
        var decisions = summary["decisions"] as JsonArray ?? [];
        var i = 0;
        foreach (var d in decisions)
        {
            i++;
            var text = d?["text"]?.GetValue<string>();
            var rulingId = d?["rulingId"]?.GetValue<string>();
            var orderId = d?["orderId"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(text)) { problems.Add($"decision {i} has no text."); continue; }
            if (Guid.TryParse(rulingId, out var rid))
            {
                var ok = await db.Rulings.AsNoTracking().AnyAsync(r => r.Id == rid && r.At >= session.StartedAt, ct);
                if (!ok) problems.Add($"decision {i} (\"{Short(text)}\"): ruling {rid} does not exist or was not recorded this session.");
            }
            else if (Guid.TryParse(orderId, out var oid))
            {
                var ok = await db.WorkOrders.AsNoTracking().AnyAsync(o => o.Id == oid
                    && (o.OpenedAt >= session.StartedAt || (o.ClosedAt != null && o.ClosedAt >= session.StartedAt)), ct);
                if (!ok) problems.Add($"decision {i} (\"{Short(text)}\"): order {oid} does not exist or was not opened/closed this session.");
            }
            else problems.Add($"decision {i} (\"{Short(text)}\") is not recorded: give it a rulingId (record_ruling) or an orderId (work_order_add).");
        }
        if (problems.Count > 0) return (false, problems, session.Id);

        session.EndedAt = DateTime.UtcNow;
        session.EndSummaryJson = summary.ToJsonString();
        session.GitHeadEnd = Trunc(gitHead, 40);
        await db.SaveChangesAsync(ct);
        currentCache = (DateTime.MinValue, null);
        return (true, [], session.Id);
    }

    /// <summary>The latest open session started in the last 24 hours — stamped on every ledger row
    /// as its Actor. Cached for ten seconds so ledger writes stay cheap.</summary>
    public async Task<Guid?> CurrentSessionIdAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - currentCache.At < TimeSpan.FromSeconds(10)) return currentCache.Id;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var since = DateTime.UtcNow.AddHours(-24);
        var id = await db.FactorySessions.AsNoTracking()
            .Where(s => s.EndedAt == null && s.StartedAt >= since)
            .OrderByDescending(s => s.StartedAt).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        currentCache = (DateTime.UtcNow, id);
        return id;
    }

    /// <summary>"cli:session:&lt;id&gt;", "mcp:session:&lt;id&gt;" — or just the source when no session is open.</summary>
    public async Task<string> ActorTagAsync(string source, CancellationToken ct = default)
    {
        try
        {
            var id = await CurrentSessionIdAsync(ct);
            return id is { } s ? $"{source}:session:{s}" : source;
        }
        catch { return source; } // stamping must never break the command being logged
    }

    private static string? Trunc(string? s, int n) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= n ? s.Trim() : s.Trim()[..n]);
    private static string Short(string s) => s.Length <= 60 ? s : s[..60] + "…";
}
