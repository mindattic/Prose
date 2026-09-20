using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Arc-level plot state memory — the structural complement to WorldStateLedger (physical)
/// and OpenThreadsService (promises). Tracks named plot states (crises, dramatic questions,
/// objectives, threats, alliances, information reveals) across beats so the prose engine
/// never re-resolves a closed crisis, re-opens a settled question, or repeats an objective
/// that was already achieved.
///
/// Write path: ExtractAndRecordAsync fires fire-and-forget after each beat via ProseWriterRouter,
///   sending a cheap Haiku call to extract state transitions from the new prose.
/// Read path: BuildContextAsync returns a compact "BOOK PLOT STATE" block injected into every
///   BeatContext as PlotEventsContext — a per-beat arc-memory snapshot.
///
/// State machine reference per StateType:
///   Crisis          : Open → Escalated → Climaxed → Resolved → (Reopened)
///   DramaticQuestion: Open → Answered | Deferred
///   Objective       : Active → Achieved | Failed | Abandoned
///   Threat          : Active → Contained → Neutralized | Escalated
///   Alliance        : Active → Strained → Broken | Restored
///   Information     : Hidden → Revealed → Confirmed | Contested
/// </summary>
public class BookStateLedgerService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    ILogger<BookStateLedgerService> log)
{
    // ── Terminal (done) state values ────────────────────────────────────────
    // These states are final — the context block marks them DO-NOT-REOPEN.
    static readonly HashSet<string> TerminalStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "Resolved", "Answered", "Achieved", "Failed", "Abandoned", "Neutralized"
    };

    // ── Write ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Directly record one plot state event. Used by manual seeding, CLI, and MCP.
    /// </summary>
    public async Task RecordAsync(
        Guid nodeId,
        Guid? beatId,
        int beatIndex,
        string stateKey,
        string stateType,
        string verb,
        string label,
        string newValue,
        string source = "manual",
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.BookPlotEvents.Add(new BookPlotEvent
        {
            Id         = Guid.CreateVersion7(),
            NodeId     = nodeId,
            BeatId     = beatId,
            BeatIndex  = beatIndex,
            StateKey   = stateKey.Length > 200 ? stateKey[..200] : stateKey,
            StateType  = stateType.Length > 50  ? stateType[..50]  : stateType,
            Verb       = verb.Length > 50        ? verb[..50]        : verb,
            Label      = label.Length > 500      ? label[..500]      : label,
            NewValue   = newValue.Length > 100   ? newValue[..100]   : newValue,
            Source     = source,
            CreatedAt  = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Extract plot state transitions from new beat prose via a cheap LLM call and persist them.
    /// Non-fatal — called fire-and-forget from ProseWriterRouter; failures are logged, not propagated.
    /// </summary>
    public async Task ExtractAndRecordAsync(
        Guid nodeId,
        Guid beatId,
        int beatIndex,
        string prose,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prose)) return;

        // Build a current-state snapshot so the extractor can see what already exists
        // and avoid recording no-op transitions (e.g., re-opening an already-Open crisis).
        var existing = await GetCurrentStateAsync(nodeId, ct);
        var existingBlock = existing.Count == 0 ? "None yet." :
            string.Join("\n", existing.Values
                .OrderBy(e => e.StateType).ThenBy(e => e.StateKey)
                .Select(e => $"  {e.StateType}|{e.StateKey}|{e.NewValue}: {e.Label}"));

        string raw;
        try
        {
            raw = await llm.GenerateAsync(
                system: $"""
                    You are a story continuity editor tracking arc-level plot states.

                    CURRENT PLOT STATE (already recorded — do not repeat these):
                    {existingBlock}

                    Read the prose excerpt below. List ONLY NEW plot state transitions introduced or
                    changed in this beat — crises that open or escalate or resolve, dramatic questions
                    that are posed or answered, objectives that are established or completed, threats
                    that emerge or are neutralized, alliances that form or break, information that is
                    newly revealed.

                    Do NOT list states that haven't changed. Do NOT list physical actions (use WorldStateLedger
                    for those). Do NOT list character emotions. Focus on arc-level narrative facts.

                    Output one event per line using this exact format (pipe-delimited, 5 fields):
                      StateType|state_key|verb|One-sentence label (max 120 chars)|NewValue

                    StateType must be one of: Crisis DramaticQuestion Objective Threat Alliance Information
                    state_key must be a snake_case slug (no spaces), e.g. crisis:behemoth_approach
                    verb must be one of: open escalate climax resolve reopen defer answer establish achieve fail abandon contain neutralize reveal confirm contest shift
                    NewValue must be one of: Open Escalated Climaxed Resolved Reopened Answered Deferred Active Achieved Failed Abandoned Contained Neutralized Strained Broken Restored Hidden Revealed Confirmed Contested

                    Max 6 events. If nothing new changed at the arc level, output the single word NONE.
                    """,
                user: prose,
                temperature: 0.1,
                maxTokens: 400,
                model: LlmModels.Haiku,
                ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "BookStateLedger: LLM extraction failed for beat {BeatId}", beatId);
            return;
        }

        if (string.IsNullOrWhiteSpace(raw) || raw.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase))
            return;

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(l => l.Contains('|') && !l.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                       .Take(6)
                       .ToList();

        await PersistPipeDelimitedEventsAsync(lines, nodeId, beatId, beatIndex, existing, ct);
    }

    /// <summary>Pipe-delimited event line format shared with <see cref="BeatExtractionService"/>'s
    /// consolidated prompt: <c>StateType|state_key|verb|label|NewValue</c>.</summary>
    public async Task PersistPipeDelimitedEventsAsync(
        IReadOnlyList<string> lines, Guid nodeId, Guid beatId, int beatIndex,
        Dictionary<string, BookPlotEvent> existing, CancellationToken ct = default)
    {
        if (lines.Count == 0) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        foreach (var line in lines.Take(6))
        {
            var parts = line.Split('|', 5, StringSplitOptions.TrimEntries);
            if (parts.Length < 5) continue;

            var stateType = parts[0];
            var stateKey  = parts[1];
            var verb      = parts[2];
            var label     = parts[3];
            var newValue  = parts[4];

            if (string.IsNullOrEmpty(stateKey) || string.IsNullOrEmpty(newValue)) continue;

            // Don't overwrite a terminal state (e.g., don't re-open a Resolved crisis)
            // unless the verb is explicitly "reopen".
            if (existing.TryGetValue(stateKey, out var prev)
                && TerminalStates.Contains(prev.NewValue)
                && !verb.Equals("reopen", StringComparison.OrdinalIgnoreCase))
            {
                log.LogDebug("BookStateLedger: skipping {Key} — already in terminal state {State}", stateKey, prev.NewValue);
                continue;
            }

            db.BookPlotEvents.Add(new BookPlotEvent
            {
                Id        = Guid.CreateVersion7(),
                NodeId    = nodeId,
                BeatId    = beatId,
                BeatIndex = beatIndex,
                StateKey  = stateKey.Length > 200 ? stateKey[..200] : stateKey,
                StateType = stateType.Length > 50  ? stateType[..50]  : stateType,
                Verb      = verb.Length > 50        ? verb[..50]        : verb,
                Label     = label.Length > 500      ? label[..500]      : label,
                NewValue  = newValue.Length > 100   ? newValue[..100]   : newValue,
                Source    = "auto",
                CreatedAt = DateTime.UtcNow,
            });
        }

        try { await db.SaveChangesAsync(ct); }
        catch (Exception ex) { log.LogWarning(ex, "BookStateLedger: save failed for beat {BeatId}", beatId); }
    }

    // ── Read ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the latest BookPlotEvent per StateKey for this node (current arc state snapshot).
    /// Dictionary keyed by StateKey.
    /// </summary>
    public async Task<Dictionary<string, BookPlotEvent>> GetCurrentStateAsync(
        Guid nodeId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var all = await db.BookPlotEvents
            .AsNoTracking()
            .Where(e => e.NodeId == nodeId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        return all
            .GroupBy(e => e.StateKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(e => e.CreatedAt).First(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build the formatted plot-state context block for injection into BeatContext.
    /// Returns empty string when no events exist for this node.
    /// </summary>
    public async Task<string> BuildContextAsync(Guid nodeId, CancellationToken ct = default)
    {
        var state = await GetCurrentStateAsync(nodeId, ct);
        if (state.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("BOOK PLOT STATE — arc-level facts established across all beats so far:");

        // Group by StateType for readability; terminal states flagged as closed.
        var groups = state.Values
            .OrderBy(e => e.StateType)
            .ThenBy(e => e.StateKey)
            .GroupBy(e => e.StateType, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            foreach (var ev in group)
            {
                var terminal = TerminalStates.Contains(ev.NewValue) ? " ✓" : "";
                sb.AppendLine($"  [{ev.StateType} — {ev.NewValue}{terminal}] {ev.StateKey}: {ev.Label}");
            }
        }

        sb.AppendLine();
        sb.Append("Do NOT re-open or re-establish states marked ✓ (terminal).");
        sb.Append(" Do not repeat objectives already Achieved or Failed.");
        sb.Append(" Do not re-introduce crises already Resolved.");
        return sb.ToString().TrimEnd();
    }

    // ── Schema bootstrap ────────────────────────────────────────────────────

    /// <summary>
    /// Idempotent DDL bootstrap for databases that pre-date this feature.
    /// EF OnModelCreating handles new DBs; call this once for existing live databases.
    /// </summary>
    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF OBJECT_ID('dbo.BookPlotEvents','U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[BookPlotEvents] (
                    [Id]         UNIQUEIDENTIFIER NOT NULL,
                    [NodeId]     UNIQUEIDENTIFIER NOT NULL,
                    [BeatId]     UNIQUEIDENTIFIER NULL,
                    [BeatIndex]  INT NOT NULL DEFAULT -1,
                    [StateKey]   NVARCHAR(200) NOT NULL,
                    [StateType]  NVARCHAR(50)  NOT NULL,
                    [Verb]       NVARCHAR(50)  NOT NULL,
                    [Label]      NVARCHAR(500) NOT NULL,
                    [NewValue]   NVARCHAR(100) NOT NULL,
                    [Source]     NVARCHAR(50)  NOT NULL DEFAULT 'auto',
                    [CreatedAt]  DATETIME2(7)  NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [PK_BookPlotEvents] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_BookPlotEvents_Nodes_NodeId]
                        FOREIGN KEY ([NodeId]) REFERENCES [dbo].[Nodes]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_BookPlotEvents_NodeId_StateKey]
                    ON [dbo].[BookPlotEvents]([NodeId], [StateKey]);
                CREATE INDEX [IX_BookPlotEvents_NodeId_CreatedAt]
                    ON [dbo].[BookPlotEvents]([NodeId], [CreatedAt]);
            END;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl, ct);
    }
}
