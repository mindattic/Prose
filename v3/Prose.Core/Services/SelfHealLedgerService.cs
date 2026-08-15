using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// One row-level mutation, in enough detail to reverse it. This is the ONE generic shape every
/// AutoCorrect fix logs — an entity merge, a consistency-fix UPDATE/DELETE, and a continuity
/// status change all reduce to a list of these. <see cref="Op"/>:
///   "update" — the row still exists; <see cref="Columns"/> holds the PRIOR values of whichever
///              columns were changed. Undo = write those values back.
///   "delete" — the row no longer exists; <see cref="Columns"/> holds every column's original
///              value. Undo = re-INSERT it. NOTE: if the table's primary key is an IDENTITY
///              column, the restored row gets a NEW key value, not the original — acceptable for
///              v1 (none of the current whitelisted fixes rely on identity-preserving delete/
///              reinsert as their primary path), but real limitation, not a promise of exact
///              restoration for that narrow case.
///   "insert" — a new row was created; <see cref="Columns"/> only needs enough to locate it
///              (PkColumn/PkValue). Undo = DELETE it.
/// All table/column names come from schema metadata (sys.columns / a service's own known SQL),
/// never from free-text user input, so building SQL text from them is safe.
/// </summary>
public sealed record RowMutationUndo(string Op, string Table, string PkColumn, string PkValue, Dictionary<string, string?> Columns);

public sealed record SelfHealRunSummary(Guid RunId, DateTime FirstAppliedAt, int TotalActions, int UndoneActions, IReadOnlyList<string> ActionTypes);

/// <summary>
/// The AutoCorrect undo ledger — "rewind the tape" for the nightly pure-ML self-healing pass (see
/// <see cref="AutoCorrectOrchestratorService"/>). Deliberately NOT a database-wide temporal/
/// bi-temporal mechanism (that was tried for Beats/Nodes/BeatNodes and created a maintenance mess,
/// see <c>DropBiTemporalAndMtld</c>) — this only ever holds rows AutoCorrect itself wrote, so undo
/// is a precise, bounded replay instead of a blanket versioning system. A whole-book
/// <see cref="ArchivedBook"/> snapshot (<see cref="BookArchiveService"/>) is kept as an independent
/// coarse fallback in case this ledger is ever itself wrong.
/// </summary>
public class SelfHealLedgerService(IDbContextFactory<ProseDbContext> dbFactory, ILogger<SelfHealLedgerService> log)
{
    /// <summary>Logs one atomic fix action. Call AFTER the mutation's own transaction has
    /// committed (the mutation itself needs to read old values to build <paramref name="mutations"/>
    /// in the first place) — the residual risk of a crash between mutate-commit and this insert is
    /// accepted; the coarse ArchivedBook snapshot and, for Entities, SQL Server's own temporal
    /// history on that table are the fallback for that narrow window.</summary>
    public async Task<Guid> LogAsync(
        Guid runId, int sequence, Guid? nodeId, string actionType,
        IReadOnlyList<RowMutationUndo> mutations, string summary, long? findingId = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = new SelfHealAction
        {
            RunId = runId,
            Sequence = sequence,
            NodeId = nodeId,
            ActionType = actionType,
            TargetTable = mutations.Count > 0 ? mutations[0].Table : "",
            TargetId = mutations.Count > 0 ? mutations[0].PkValue : "",
            BeforeStateJson = JsonSerializer.Serialize(mutations),
            Summary = summary,
            FindingId = findingId,
            AppliedAt = DateTime.UtcNow,
        };
        db.SelfHealActions.Add(row);
        await db.SaveChangesAsync(ct);
        return row.Id;
    }

    /// <summary>Reverses every not-yet-undone action for one run, in DESCENDING Sequence
    /// (newest-first — reversing in the opposite order they were applied, exactly like rewinding
    /// a tape). Returns the number of actions reversed. Stops and reports on the first action
    /// whose reversal fails rather than continuing past a partially-undone state.</summary>
    public async Task<int> UndoRunAsync(Guid runId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var actions = await db.SelfHealActions.AsNoTracking()
            .Where(a => a.RunId == runId && a.UndoneAt == null)
            .OrderByDescending(a => a.Sequence)
            .ToListAsync(ct);
        return await UndoActionsAsync(db, actions, ct);
    }

    /// <summary>Reverses the N most-recently-applied not-yet-undone actions across ANY run —
    /// true tape-rewind granularity when a single bad fix needs backing out without touching the
    /// rest of that night's run.</summary>
    public async Task<int> UndoLastNActionsAsync(int n, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var actions = await db.SelfHealActions.AsNoTracking()
            .Where(a => a.UndoneAt == null)
            .OrderByDescending(a => a.AppliedAt)
            .Take(n)
            .ToListAsync(ct);
        return await UndoActionsAsync(db, actions, ct);
    }

    private async Task<int> UndoActionsAsync(ProseDbContext db, List<SelfHealAction> actions, CancellationToken ct)
    {
        int undone = 0;
        foreach (var action in actions)
        {
            List<RowMutationUndo>? mutations;
            try
            {
                mutations = JsonSerializer.Deserialize<List<RowMutationUndo>>(action.BeforeStateJson);
            }
            catch (JsonException ex)
            {
                log.LogError(ex, "[SelfHealLedger] Action {Id} ({ActionType}) has unparseable BeforeStateJson — cannot undo, stopping.", action.Id, action.ActionType);
                break;
            }

            if (mutations == null || mutations.Count == 0)
            {
                await MarkUndoneAsync(db, action.Id, ct);
                undone++;
                continue;
            }

            try
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                foreach (var m in mutations)
                    await ReverseOneAsync(db, m, ct);
                await MarkUndoneAsync(db, action.Id, ct);
                await tx.CommitAsync(ct);
                undone++;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[SelfHealLedger] Failed to reverse action {Id} ({ActionType}, {Summary}) — stopping undo here; {Undone} action(s) reversed before this failure.",
                    action.Id, action.ActionType, action.Summary, undone);
                break;
            }
        }
        return undone;
    }

    private static async Task MarkUndoneAsync(ProseDbContext db, Guid actionId, CancellationToken ct) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [dbo].[SelfHealActions] SET [UndoneAt] = SYSUTCDATETIME() WHERE [Id] = {actionId}", ct);

    private static async Task ReverseOneAsync(ProseDbContext db, RowMutationUndo m, CancellationToken ct)
    {
        switch (m.Op)
        {
            case "update":
            {
                var setClauses = string.Join(", ", m.Columns.Keys.Select((k, i) => $"[{k}] = @p{i}"));
                var pars = m.Columns.Values.Select((v, i) => new SqlParameter($"@p{i}", (object?)v ?? DBNull.Value)).ToList();
                pars.Add(new SqlParameter("@pk", m.PkValue));
                var sql = $"UPDATE [dbo].[{m.Table}] SET {setClauses} WHERE [{m.PkColumn}] = @pk";
                await db.Database.ExecuteSqlRawAsync(sql, pars.ToArray<object>(), ct);
                break;
            }
            case "delete":
            {
                // Undo of a delete = re-insert. See RowMutationUndo doc comment re: IDENTITY PKs.
                var cols = string.Join(", ", m.Columns.Keys.Select(k => $"[{k}]"));
                var placeholders = string.Join(", ", m.Columns.Keys.Select((k, i) => $"@p{i}"));
                var pars = m.Columns.Values.Select((v, i) => new SqlParameter($"@p{i}", (object?)v ?? DBNull.Value)).ToArray();
                var sql = $"INSERT INTO [dbo].[{m.Table}] ({cols}) VALUES ({placeholders})";
                await db.Database.ExecuteSqlRawAsync(sql, pars, ct);
                break;
            }
            case "insert":
            {
                var pars = new[] { new SqlParameter("@pk", m.PkValue) };
                var sql = $"DELETE FROM [dbo].[{m.Table}] WHERE [{m.PkColumn}] = @pk";
                await db.Database.ExecuteSqlRawAsync(sql, pars, ct);
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown RowMutationUndo.Op '{m.Op}' — refusing to guess.");
        }
    }

    public async Task<IReadOnlyList<SelfHealRunSummary>> ListRunsAsync(int limit = 20, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var groups = await db.SelfHealActions.AsNoTracking()
            .GroupBy(a => a.RunId)
            .Select(g => new
            {
                RunId = g.Key,
                FirstAppliedAt = g.Min(a => a.AppliedAt),
                Total = g.Count(),
                Undone = g.Count(a => a.UndoneAt != null),
            })
            .OrderByDescending(g => g.FirstAppliedAt)
            .Take(limit)
            .ToListAsync(ct);

        var result = new List<SelfHealRunSummary>();
        foreach (var g in groups)
        {
            var types = await db.SelfHealActions.AsNoTracking()
                .Where(a => a.RunId == g.RunId)
                .Select(a => a.ActionType).Distinct().ToListAsync(ct);
            result.Add(new SelfHealRunSummary(g.RunId, g.FirstAppliedAt, g.Total, g.Undone, types));
        }
        return result;
    }
}
