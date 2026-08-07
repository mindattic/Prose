using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Manages user-defined context overrides for the DocContextStack: per-session pin and
/// exclude rules that survive across CLI invocations within a 24-hour window.
///
/// <para>
/// <b>Session key</b> defaults to <c>Environment.UserName</c> so rules persist across
/// multiple <c>ss</c> invocations in the same day without any extra configuration.
/// </para>
/// <para>
/// <b>Scope:</b> an override with <c>NodeId = null</c> applies to every node in the session;
/// one with a NodeId applies only to that specific node.
/// </para>
/// <para>
/// Consumed by <see cref="DocContextService.PrepareForNodeAsync"/> — pinned docs are forced
/// into the block regardless of LRU tier; excluded docs are filtered out before rendering.
/// </para>
/// </summary>
public class UserContextService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<UserContextService> log)
{
    public const string DefaultSessionKey = "";  // resolved lazily from Environment.UserName

    public sealed record ActiveOverride(int Id, Guid MarkdownFileId, string Action, Guid? NodeId);
    public sealed record ContextStatusEntry(Guid DocId, string RelativePath, string Action, Guid? NodeId, DateTime ExpiresAt);
    public sealed record ContextStatusReport(IReadOnlyList<ContextStatusEntry> Entries, string SessionKey);

    // ── Write operations ───────────────────────────────────────────────────────

    /// <summary>Pins a markdown doc so it is always included in the doc context block.</summary>
    public Task PinAsync(Guid markdownFileId, Guid? nodeId = null, string? sessionKey = null, CancellationToken ct = default)
        => UpsertAsync(markdownFileId, nodeId, "pin", ResolveKey(sessionKey), ct);

    /// <summary>Excludes a markdown doc from the doc context block even if it would normally be injected.</summary>
    public Task ExcludeAsync(Guid markdownFileId, Guid? nodeId = null, string? sessionKey = null, CancellationToken ct = default)
        => UpsertAsync(markdownFileId, nodeId, "exclude", ResolveKey(sessionKey), ct);

    /// <summary>Removes any override for the given doc (whether pin or exclude).</summary>
    public async Task RemoveAsync(Guid markdownFileId, Guid? nodeId = null, string? sessionKey = null, CancellationToken ct = default)
    {
        var key = ResolveKey(sessionKey);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rows = await db.ContextOverrides
                .Where(o => o.SessionKey == key
                         && o.MarkdownFileId == markdownFileId
                         && o.NodeId == nodeId)
                .ToListAsync(ct);
            if (rows.Count > 0)
            {
                db.ContextOverrides.RemoveRange(rows);
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[UserContextService] RemoveAsync failed for doc {DocId}", markdownFileId);
        }
    }

    /// <summary>
    /// Clears all overrides for the session. When <paramref name="nodeId"/> is set,
    /// clears only overrides scoped to that node; otherwise clears all session overrides.
    /// </summary>
    public async Task ClearAsync(Guid? nodeId = null, string? sessionKey = null, CancellationToken ct = default)
    {
        var key = ResolveKey(sessionKey);
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var q = db.ContextOverrides.Where(o => o.SessionKey == key);
            if (nodeId.HasValue) q = q.Where(o => o.NodeId == nodeId);
            var rows = await q.ToListAsync(ct);
            if (rows.Count > 0)
            {
                db.ContextOverrides.RemoveRange(rows);
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[UserContextService] ClearAsync failed");
        }
    }

    // ── Read operations ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active (non-expired) overrides for a session, optionally filtered to
    /// a specific node (returns global overrides AND node-specific ones when nodeId is set).
    /// </summary>
    public async Task<IReadOnlyList<ActiveOverride>> GetActiveAsync(
        Guid? nodeId = null, string? sessionKey = null, CancellationToken ct = default)
    {
        var key = ResolveKey(sessionKey);
        var now = DateTime.UtcNow;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var q = db.ContextOverrides.AsNoTracking()
                .Where(o => o.SessionKey == key && o.ExpiresAt > now);

            // Include global overrides (NodeId == null) plus node-specific ones if nodeId set.
            if (nodeId.HasValue)
                q = q.Where(o => o.NodeId == null || o.NodeId == nodeId);
            else
                q = q.Where(o => o.NodeId == null);

            return (await q.ToListAsync(ct))
                .Select(o => new ActiveOverride(o.Id, o.MarkdownFileId, o.Action, o.NodeId))
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[UserContextService] GetActiveAsync failed");
            return [];
        }
    }

    /// <summary>Returns the full status report for display via <c>ss --context status</c>.</summary>
    public async Task<ContextStatusReport> GetStatusAsync(string? sessionKey = null, CancellationToken ct = default)
    {
        var key = ResolveKey(sessionKey);
        var now = DateTime.UtcNow;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var overrides = await db.ContextOverrides.AsNoTracking()
                .Where(o => o.SessionKey == key && o.ExpiresAt > now)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync(ct);

            var fileIds = overrides.Select(o => o.MarkdownFileId).Distinct().ToList();
            var pathById = await db.MarkdownFiles.AsNoTracking()
                .Where(m => fileIds.Contains(m.Id))
                .Select(m => new { m.Id, m.RelativePath })
                .ToDictionaryAsync(x => x.Id, x => x.RelativePath, ct);

            var entries = overrides.Select(o => new ContextStatusEntry(
                DocId:        o.MarkdownFileId,
                RelativePath: pathById.GetValueOrDefault(o.MarkdownFileId, o.MarkdownFileId.ToString()),
                Action:       o.Action,
                NodeId:       o.NodeId,
                ExpiresAt:    o.ExpiresAt)).ToList();

            return new ContextStatusReport(entries, key);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[UserContextService] GetStatusAsync failed");
            return new ContextStatusReport([], key);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string ResolveKey(string? provided)
        => string.IsNullOrWhiteSpace(provided)
            ? (Environment.UserName ?? "default")
            : provided;

    private async Task UpsertAsync(Guid markdownFileId, Guid? nodeId, string action, string key, CancellationToken ct)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            // Remove any existing override for this doc+node combination first (pin↔exclude swap).
            var existing = await db.ContextOverrides
                .Where(o => o.SessionKey == key
                         && o.MarkdownFileId == markdownFileId
                         && o.NodeId == nodeId)
                .ToListAsync(ct);
            if (existing.Count > 0) db.ContextOverrides.RemoveRange(existing);

            db.ContextOverrides.Add(new ContextOverride
            {
                SessionKey     = key,
                NodeId         = nodeId,
                Action         = action,
                MarkdownFileId = markdownFileId,
                CreatedAt      = DateTime.UtcNow,
                ExpiresAt      = DateTime.UtcNow.AddHours(24),
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[UserContextService] UpsertAsync failed for doc {DocId} action={Action}", markdownFileId, action);
        }
    }
}
