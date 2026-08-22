using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Hub.Contracts;
using Prose.Hub.Hubs;

namespace Prose.Hub;

/// <summary>
/// Observability plan (2026-08-20), Part C, Phase 4: subscribes ONCE, at startup, to every
/// Phase-3 event (<see cref="ContextTelemetryService"/>, <see cref="UniverseGraphService"/>,
/// <see cref="Logging.RingBufferLoggerProvider"/>) and forwards each to <see cref="ObservabilityHub"/>
/// over SignalR, plus a best-effort DB write for the two permanent-history tables
/// (<see cref="DcmRun"/>/<see cref="DcmBeatSnapshot"/>). Those services stay transport-agnostic
/// on purpose (they're used by the CLI too) — this class is the ONLY thing that knows about
/// SignalR/the DB persistence side of them.
///
/// DCM events broadcast to every connection (a DCM-Viz page watches one specific run/node at a
/// time — the client filters by RunId, no server-side universe grouping needed). Graph deltas
/// DO need universe-scoped groups (<c>UniverseGraphService</c> is a shared per-universe
/// singleton with one event surface) — resolved via <see cref="IUniverseContext.ListUniverses"/>,
/// a cheap in-memory lookup, not a DB round-trip.
/// </summary>
public sealed class ObservabilityBridge(
    IHubContext<ObservabilityHub> hubContext,
    Logging.RingBufferLoggerProvider loggerProvider,
    ContextTelemetryService telemetry,
    UniverseGraphService graph,
    IUniverseContext universeContext,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<ObservabilityBridge> logger)
{
    public void Wire()
    {
        loggerProvider.OnLine = dto => Fire(hubContext.Clients.All.SendAsync("LogLine", dto));

        telemetry.RunStarted += run =>
        {
            Fire(hubContext.Clients.All.SendAsync("DcmRunStarted", ToDto(run)));
            Fire(PersistRunStartAsync(run));
        };
        telemetry.BeatRecorded += (run, beat) =>
        {
            Fire(hubContext.Clients.All.SendAsync("DcmBeat", ToDto(run.RunId, beat)));
            Fire(PersistBeatAsync(run.RunId, beat));
            // Phase 7: the live DCM-Viz chart doesn't do true incremental DOM patching - it's
            // a cheap full rebuild from the whole run-so-far on every beat (SVG rebuild is
            // fast even for hundreds of beats), so it reuses the EXACT SAME payload shape and
            // JS renderer as history mode. run.Beats already holds every beat recorded so far.
            var payloadJson = DcmVisualizationService.BuildPayloadJson(run.NodeSlug, run.Beats.Select(ToSnapshot).ToList());
            Fire(hubContext.Clients.All.SendAsync("DcmPayload", payloadJson));
        };
        telemetry.RunEnded += run =>
        {
            Fire(hubContext.Clients.All.SendAsync("DcmRunEnded", ToDto(run)));
            Fire(PersistRunEndAsync(run));
        };

        graph.NodeAdded += (universeId, node) =>
            Fire(BroadcastGraphAsync(universeId, GraphDeltaKind.NodeAdded,
                node: new GraphNodeDto(node.Id, node.Name, node.NodeType)));
        graph.NodeRemoved += (universeId, nodeId) =>
            Fire(BroadcastGraphAsync(universeId, GraphDeltaKind.NodeRemoved, removedNodeId: nodeId));
        graph.EdgeAdded += (universeId, edge) =>
            Fire(BroadcastGraphAsync(universeId, GraphDeltaKind.EdgeAdded, edge: ToDto(edge)));
        graph.EdgeInvalidated += (universeId, edge) =>
            Fire(BroadcastGraphAsync(universeId, GraphDeltaKind.EdgeInvalidated, edge: ToDto(edge)));
    }

    // ── DCM: DTO mapping + best-effort persistence ─────────────────────────────

    private static DcmRunDto ToDto(ContextTelemetryService.Run run) => new(
        run.RunId, run.NodeId, run.NodeSlug, run.Label, run.DocContextEnabled,
        run.StartedAt, run.EndedAt, run.BaselineScore, run.BaselineFlow, run.FinalScore, run.FinalFlow);

    /// <summary>Maps a live <see cref="ContextTelemetryService.BeatRecord"/> onto
    /// <see cref="DcmVisualizationService.BeatSnapshot"/> - prefers the full, non-budget-
    /// clipped working set (<c>FullActiveSet</c>) when captured, falling back to the
    /// budget-clipped <c>Docs</c> list otherwise (same fields, just narrower).</summary>
    public static DcmVisualizationService.BeatSnapshot ToSnapshot(ContextTelemetryService.BeatRecord beat)
    {
        var docs = beat.FullActiveSet != null
            ? beat.FullActiveSet.Select(e => new DcmVisualizationService.DocEntry(e.Path, e.Tier, e.Reason, e.Score))
            : beat.Docs.Select(d => new DcmVisualizationService.DocEntry(d.Path, d.Tier, d.Reason, d.Score));
        return new DcmVisualizationService.BeatSnapshot(beat.BeatIndex, beat.BeatTitle, docs.ToList());
    }

    private static DcmBeatDto ToDto(Guid runId, ContextTelemetryService.BeatRecord beat) => new(
        runId, beat.BeatIndex, beat.BeatId, beat.BeatTitle, beat.StartedAt, beat.DurationMs, beat.ProseChars,
        beat.Docs.Select(d => new DcmDocLoadDto(d.Path, d.Tier, d.Reason, d.Score, d.Chars)).ToList(),
        beat.Entities.Select(e => new DcmEntityLoadDto(e.Name, e.Type, e.MatchSource, e.Score, e.Depth)).ToList());

    private async Task PersistRunStartAsync(ContextTelemetryService.Run run)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            db.DcmRuns.Add(new DcmRun
            {
                Id = run.RunId,
                NodeId = run.NodeId,
                NodeSlug = run.NodeSlug,
                Label = run.Label,
                DocContextEnabled = run.DocContextEnabled,
                StartedAt = run.StartedAt,
                BaselineScore = run.BaselineScore,
                BaselineFlow = run.BaselineFlow,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[dcm-persist] failed to record run start for {RunId}", run.RunId);
        }
    }

    private async Task PersistBeatAsync(Guid runId, ContextTelemetryService.BeatRecord beat)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            db.DcmBeatSnapshots.Add(new DcmBeatSnapshot
            {
                RunId = runId,
                BeatIndex = beat.BeatIndex,
                BeatId = beat.BeatId,
                BeatTitle = beat.BeatTitle,
                StartedAt = beat.StartedAt,
                DurationMs = beat.DurationMs,
                ProseChars = beat.ProseChars,
                DocsJson = JsonSerializer.Serialize(beat.Docs),
                EntitiesJson = JsonSerializer.Serialize(beat.Entities),
                FullActiveSetJson = beat.FullActiveSet != null ? JsonSerializer.Serialize(beat.FullActiveSet) : null,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[dcm-persist] failed to record beat {BeatIndex} for run {RunId}", beat.BeatIndex, runId);
        }
    }

    private async Task PersistRunEndAsync(ContextTelemetryService.Run run)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var existing = await db.DcmRuns.FirstOrDefaultAsync(r => r.Id == run.RunId);
            if (existing == null) return; // best-effort: RunStarted's own write may itself have failed
            existing.EndedAt = run.EndedAt;
            existing.FinalScore = run.FinalScore;
            existing.FinalFlow = run.FinalFlow;
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[dcm-persist] failed to record run end for {RunId}", run.RunId);
        }
    }

    // ── Graph deltas: universe-slug resolution + broadcast ──────────────────────

    private static GraphEdgeDto ToDto(Prose.Core.Models.Graph.UniverseEdge edge) => new(
        edge.Source, edge.Target, edge.RelationType, edge.Weight, edge.Sentiment, edge.Description);

    private Task BroadcastGraphAsync(Guid universeId, GraphDeltaKind kind, GraphNodeDto? node = null, GraphEdgeDto? edge = null, string? removedNodeId = null)
    {
        var slug = ResolveSlug(universeId);
        if (slug == null) return Task.CompletedTask;
        var dto = new GraphDeltaDto(slug, kind, node, edge, removedNodeId);
        return hubContext.Clients.Group(ObservabilityHub.GroupName(slug)).SendAsync("GraphDelta", dto);
    }

    private string? ResolveSlug(Guid universeId)
    {
        foreach (var u in universeContext.ListUniverses())
            if (u.Id == universeId) return u.Slug;
        return null;
    }

    // ── Fire-and-forget with fault logging (never let a background push/persist throw unobserved) ──

    private void Fire(Task task) => _ = task.ContinueWith(
        t => logger.LogWarning(t.Exception, "[observability-bridge] background push/persist failed"),
        TaskContinuationOptions.OnlyOnFaulted);
}
