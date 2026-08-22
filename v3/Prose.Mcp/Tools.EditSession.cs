using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

[McpServerToolType]
public class EditSessionTools(
    EditSessionService sessionSvc,
    BibleSyncService bibleSvc,
    BlueprintSyncService blueprintSvc,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    [McpServerTool, Description("Start a named edit session for a node. A session groups all prose edits until closed, enabling bible/blueprint sync afterward. Session types: prose-pass, gripes-cleanup, logic-sweep, custom.")]
    public Task<string> start_edit_session(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Human-readable label, e.g. 'prose-pass-1' or 'gripes-cleanup-2026-07-13'.")] string label,
        [Description("Session type: prose-pass | gripes-cleanup | logic-sweep | custom (default).")] string sessionType = "custom") =>
        hub.InvokeAsync(nameof(EditSessionTools), nameof(start_edit_sessionImpl), new { nodeIdOrSlug, label, sessionType });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> start_edit_sessionImpl(string nodeIdOrSlug, string label, string sessionType = "custom")
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return $"Node not found: {nodeIdOrSlug}";
        var session = await sessionSvc.StartSessionAsync(node.Id, label, sessionType);
        return $"Session started.\nID      : {session.EditSessionId}\nNode    : {node.NodeCode ?? node.Slug}\nLabel   : {session.Label}\nType    : {session.SessionType}\nStarted : {session.StartedAt:yyyy-MM-dd HH:mm} UTC";
    }

    [McpServerTool, Description("Close the open edit session for a node (or by session ID). Returns beat count and duration.")]
    public Task<string> close_edit_session(
        [Description("Node id (GUID) or slug. Use this OR session_id.")] string? nodeIdOrSlug = null,
        [Description("Session GUID. Use this OR node_id_or_slug.")] string? sessionId = null) =>
        hub.InvokeAsync(nameof(EditSessionTools), nameof(close_edit_sessionImpl), new { nodeIdOrSlug, sessionId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> close_edit_sessionImpl(string? nodeIdOrSlug = null, string? sessionId = null)
    {
        Guid? nodeId = null;
        if (!string.IsNullOrWhiteSpace(nodeIdOrSlug))
        {
            var node = await ResolveNodeAsync(nodeIdOrSlug);
            if (node == null) return $"Node not found: {nodeIdOrSlug}";
            nodeId = node.Id;
        }

        Guid? sid = sessionId != null && Guid.TryParse(sessionId, out var g) ? g : null;
        var session = await sessionSvc.CloseSessionAsync(nodeId, sid);
        var duration = session.ClosedAt.HasValue
            ? $"{(session.ClosedAt.Value - session.StartedAt).TotalMinutes:F0}m"
            : "?";
        return $"Session closed.\nLabel    : {session.Label}\nBeats    : {session.BeatCount}\nDuration : {duration}";
    }

    [McpServerTool, Description("List edit sessions for a node, most recent first.")]
    public Task<string> list_edit_sessions(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Max number of sessions to return (default 20).")] int limit = 20) =>
        hub.InvokeAsync(nameof(EditSessionTools), nameof(list_edit_sessionsImpl), new { nodeIdOrSlug, limit });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> list_edit_sessionsImpl(string nodeIdOrSlug, int limit = 20)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return $"Node not found: {nodeIdOrSlug}";
        var sessions = await sessionSvc.GetSessionsAsync(node.Id, limit);
        if (sessions.Count == 0) return $"No sessions found for {node.NodeCode ?? node.Slug}.";
        return JsonSerializer.Serialize(sessions.Select(s => new
        {
            id       = s.EditSessionId,
            label    = s.Label,
            type     = s.SessionType,
            beatCount = s.BeatCount,
            startedAt = s.StartedAt,
            closedAt  = s.ClosedAt,
            isOpen    = s.ClosedAt == null,
        }), new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("List the beats that were edited in a session, with timestamps and version deltas.")]
    public Task<string> session_beats(
        [Description("Session GUID.")] string sessionId) =>
        hub.InvokeAsync(nameof(EditSessionTools), nameof(session_beatsImpl), new { sessionId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> session_beatsImpl(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sid)) return $"Invalid session ID: {sessionId}";
        var session = await sessionSvc.GetSessionAsync(sid);
        if (session == null) return $"Session not found: {sessionId}";
        var beats = await sessionSvc.GetSessionBeatsAsync(sid);
        if (beats.Count == 0) return $"Session \"{session.Label}\" has no beats yet.";
        return JsonSerializer.Serialize(beats.Select(esb => new
        {
            beatId     = esb.BeatId,
            beatNumber = esb.Beat?.Number,
            beatTitle  = esb.Beat?.Title,
            editedAt   = esb.EditedAt,
            priorVersion = esb.PriorVersion,
            currentVersion = esb.Beat?.Version,
        }), new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Extract narrative facts from a session's beats and append them as '## Session Extracts' to the node bible .md file. Use --dry-run to preview without writing.")]
    public Task<string> sync_bible_from_session(
        [Description("Session GUID.")] string sessionId,
        [Description("If true, returns extracted facts without writing to the bible file.")] bool dryRun = false) =>
        hub.InvokeAsync(nameof(EditSessionTools), nameof(sync_bible_from_sessionImpl), new { sessionId, dryRun });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> sync_bible_from_sessionImpl(string sessionId, bool dryRun = false)
    {
        if (!Guid.TryParse(sessionId, out var sid)) return $"Invalid session ID: {sessionId}";
        var report = await bibleSvc.ExtractFromSessionAsync(sid, dryRun);
        var factLines = report.Facts.Count == 0
            ? "  (no facts extracted)"
            : string.Join("\n", report.Facts.Select(f => $"  [{f.Category}] Beat {f.BeatNumber}: {f.Fact}"));
        var writeStatus = dryRun ? "(dry run — not written)" : report.WroteToFile ? $"Appended to {report.FilePath}" : "Not written (file not found or no facts)";
        return $"Session : {report.SessionLabel}\nNode    : {report.NodeCode}\nFacts   : {report.Facts.Count}\nWrite   : {writeStatus}\n\n{factLines}";
    }

    [McpServerTool, Description("Map a session's beats to their blueprint tags. Confirmed decisions are recorded; divergences file BLUEPRINT-DRIFT findings.")]
    public Task<string> sync_blueprint_from_session(
        [Description("Session GUID.")] string sessionId) =>
        hub.InvokeAsync(nameof(EditSessionTools), nameof(sync_blueprint_from_sessionImpl), new { sessionId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> sync_blueprint_from_sessionImpl(string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var sid)) return $"Invalid session ID: {sessionId}";
        var report = await blueprintSvc.SyncFromSessionAsync(sid);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Session    : {report.SessionLabel}");
        sb.AppendLine($"Confirmed  : {report.Confirmed}");
        sb.AppendLine($"Diverged   : {report.Diverged}");
        sb.AppendLine($"Unverified : {report.Unverified}");
        if (report.DriftSummaries.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("BLUEPRINT-DRIFT findings:");
            foreach (var d in report.DriftSummaries)
                sb.AppendLine($"  ! {d}");
        }
        return sb.ToString().TrimEnd();
    }

    private async Task<Prose.Core.Data.Entities.Node?> ResolveNodeAsync(string nodeIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(nodeIdOrSlug, out var id))
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            return await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == id);
        return await db.Nodes.FirstOrDefaultAsync(
            n => n.Slug == nodeIdOrSlug ||
                 (n.NodeCode != null && n.NodeCode.ToUpper() == nodeIdOrSlug.ToUpper()));
    }
}
