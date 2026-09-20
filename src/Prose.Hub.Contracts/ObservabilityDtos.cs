namespace Prose.Hub.Contracts;

// Pure data-transfer records for the observability plan (2026-08-20) — no EF Core, no
// ASP.NET Core, no QuikGraph. Shared between Prose.Hub (server, maps its own
// entities/domain types onto these), Prose.ObserverUi (the shared Razor UI), and
// Prose.Maui (native Windows head) so none of the client sides need to pull in Prose.Core.

/// <summary>One captured log line, pushed live over SignalR and returned by
/// <c>GET /api/logs/recent</c> for initial load/reconnect catch-up.</summary>
public sealed record LogLineDto(DateTime At, string Level, string Category, string Message, string? Exception);

/// <summary>One durable log entry from <c>LoggingService.Search</c> (Serilog daily files on
/// disk) — a different shape from <see cref="LogLineDto"/> (no Category; Serilog's own
/// per-line parse doesn't split it out), used by the Logs tab's History mode.</summary>
public sealed record LogSearchResultDto(DateTime Timestamp, string Level, string Message, string? Exception);

/// <summary>One Dynamic Context Memory instrumentation run — mirrors
/// <c>ContextTelemetryService.Run</c>'s header fields (not its beat list; see
/// <see cref="DcmBeatDto"/>).</summary>
public sealed record DcmRunDto(
    Guid RunId, Guid NodeId, string NodeSlug, string Label, bool DocContextEnabled,
    DateTime StartedAt, DateTime? EndedAt,
    double BaselineScore, double BaselineFlow, double FinalScore, double FinalFlow);

public sealed record DcmDocLoadDto(string Path, string Tier, string Reason, double Score, int Chars);
public sealed record DcmEntityLoadDto(string Name, string Type, string MatchSource, double Score, int Depth);

/// <summary>One beat's DCM working set — mirrors <c>ContextTelemetryService.BeatRecord</c>.
/// Pushed live per-beat over SignalR and returned by <c>GET /api/dcm/runs/{id}/beats</c>
/// for history mode.</summary>
public sealed record DcmBeatDto(
    Guid RunId, int BeatIndex, string BeatId, string BeatTitle,
    DateTime StartedAt, double DurationMs, int ProseChars,
    IReadOnlyList<DcmDocLoadDto> Docs, IReadOnlyList<DcmEntityLoadDto> Entities);

/// <summary>Minimal node shape for graph rendering/deltas — the full entity record is
/// fetched via the Hub's existing <c>GET /api/universes/{slug}/snapshot</c> for initial
/// load; deltas only need enough to add/update a live point.</summary>
public sealed record GraphNodeDto(string Id, string Name, string NodeType);

public sealed record GraphEdgeDto(string Source, string Target, string RelationType, double Weight, string Sentiment, string? Description);

/// <summary>One live graph mutation, scoped to a universe — pushed to that
/// <c>universe:{slug}</c> SignalR group as <c>UniverseGraphService</c> mutates during a
/// live write. Exactly one of <see cref="Node"/>/<see cref="Edge"/>/<see cref="RemovedNodeId"/>
/// is populated, matching <see cref="Kind"/>.</summary>
public sealed record GraphDeltaDto(string Universe, GraphDeltaKind Kind, GraphNodeDto? Node, GraphEdgeDto? Edge, string? RemovedNodeId);

public enum GraphDeltaKind { NodeAdded, NodeRemoved, EdgeAdded, EdgeInvalidated }

/// <summary>Current write-in-progress status — "is the engine doing anything right now,
/// and what." Sourced from <c>ContextTelemetryService.IsActive</c>/<c>Current</c>.</summary>
public sealed record WriteStatusDto(bool IsActive, Guid? NodeId, string? NodeSlug, string? Label, int? BeatCount, DateTime? StartedAt);

/// <summary>Dashboard "recent activity" feed row — mirrors <c>CommandLedgerEntry</c>'s
/// public shape without requiring a Prose.Core reference.</summary>
public sealed record CommandLedgerDto(
    Guid Id, DateTime At, string Source, string HandlerClass, string? Method,
    string? Universe, int? ExitCode, bool Success, double DurationMs,
    string? OutputSummary, string? ErrorMessage, string? Actor);

/// <summary>Dashboard "recent decisions" feed row — mirrors <c>DecisionLedgerEntry</c>'s
/// public shape without requiring a Prose.Core reference.</summary>
public sealed record DecisionLedgerDto(
    Guid Id, DateTime At, string? SessionId, string Summary, string? Rationale,
    string? Category, string? Actor);

// ── Beats tab (Part B: read_beats) ──────────────────────────────────────────

public sealed record BeatRowDto(int Position, Guid Id, string? Title, string? Kind, string? Text);
public sealed record ReadBeatsResultDto(Guid NodeId, string Slug, int Total, List<BeatRowDto> Beats);

// ── Repositories tab (Part B: browse_repository) ────────────────────────────

public sealed record RepositoryTypeCountDto(string Type, int Count);
public sealed record RepositoryEntityRowDto(Guid Id, string Name, string Slug, string Status, string? Description);
public sealed record BrowseRepositoryResultDto(int Total, int Page, int PageSize, List<RepositoryEntityRowDto> Rows);

// ── Beat Archive tab (Part F5/F6: get_beat_archive) ──────────────────────────
// Mirrors Prose.Core.Services.BeatArchiveService.Archive's shape without requiring a
// Prose.Core reference from Prose.ObserverUi/Prose.Maui.

public sealed record BeatArchiveBeatDto(Guid Id, string? Title, string Kind, string Text, DateTime UpdatedAt);
public sealed record BeatArchiveModeDto(string Mode, float Confidence, string DetectionMethod);
public sealed record BeatArchiveServiceCoverageDto(string Service, bool WasApplicable, bool WasActive, int BlockSizeChars);
public sealed record BeatArchivePromptDto(DateTime At, string ProviderId, string Model, string System, string User, string? Response, int? ElapsedMs);
public sealed record BeatArchiveEntityDto(Guid EntityId, string Name, string EntityType, string MatchSource, double Score, bool IsPov, string? ResolvedJson);
public sealed record BeatArchiveEdgeDto(Guid SourceId, string? SourceName, Guid TargetId, string? TargetName, string RelationType, string Sentiment, double Weight);
public sealed record BeatArchiveDocDto(string? Path, string? Tier, string? Content);

public sealed record BeatArchiveDto(
    BeatArchiveBeatDto Beat, Guid NodeId, DateTime AsOf, BeatArchiveModeDto? Mode,
    List<BeatArchiveServiceCoverageDto> ServiceCoverage, string? ContextTrace,
    List<BeatArchivePromptDto> Prompts, List<BeatArchiveEntityDto> EntityRoster,
    List<BeatArchiveEdgeDto> Edges, List<BeatArchiveDocDto> Docs, string? Bible);
