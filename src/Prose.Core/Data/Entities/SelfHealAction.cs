namespace Prose.Core.Data.Entities;

/// <summary>
/// One reversible mutation applied by the nightly AutoCorrect pass (see
/// <see cref="Prose.Core.Services.AutoCorrectOrchestratorService"/>). This is the "rewind tape" —
/// every write AutoCorrect makes is logged here BEFORE it happens, with enough of the prior row
/// state captured that <see cref="Prose.Core.Services.SelfHealLedgerService.UndoRunAsync"/> can put
/// it back exactly as it was. Deliberately NOT a database-wide temporal/bi-temporal mechanism (that
/// was tried for Beats/Nodes/BeatNodes and created a maintenance mess — see
/// <c>DropBiTemporalAndMtld</c>) — this ledger only ever contains rows AutoCorrect itself wrote, one
/// row per atomic action, so undo is a precise, bounded replay instead of a blanket versioning
/// system.
/// </summary>
public class SelfHealAction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Groups every action taken during one `--auto-correct-nightly` invocation.</summary>
    public Guid RunId { get; set; }

    /// <summary>Order within the run. Undo replays in DESCENDING Sequence — reverse order,
    /// exactly like rewinding a tape.</summary>
    public int Sequence { get; set; }

    /// <summary>The book this action belongs to, for per-book reporting/filtering. Null for
    /// corpus/universe-scoped fixes not tied to one book (e.g. DataConsistencyService's
    /// dangling-edge cleanup, cross-book continuity resolution).</summary>
    public Guid? NodeId { get; set; }

    /// <summary>"beat-delete" | "entity-merge" | "consistency-fix" | "continuity-resolve" — the
    /// reversal dispatcher in SelfHealLedgerService switches on this.</summary>
    public string ActionType { get; set; } = "";

    /// <summary>Physical table the mutation was applied to (for consistency-fix's
    /// column-level restores; entity-merge instead stores a JSON array of tables in
    /// BeforeStateJson since it can touch many at once).</summary>
    public string TargetTable { get; set; } = "";

    /// <summary>Primary key (as string — Guid or bigint, varies by table) of the row mutated.</summary>
    public string TargetId { get; set; } = "";

    /// <summary>Exact prior state needed to reverse this action, JSON-serialized. Shape depends on
    /// ActionType — see SelfHealLedgerService's per-type doc comments.</summary>
    public string BeforeStateJson { get; set; } = "";

    /// <summary>Human-readable one-liner for the morning report / undo listing.</summary>
    public string Summary { get; set; } = "";

    /// <summary>The Finding this action resolved, if any — so a rollback can reopen it.</summary>
    public long? FindingId { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set once this specific action has been reversed (by run-undo or last-N-undo).
    /// Null = still in effect.</summary>
    public DateTime? UndoneAt { get; set; }
}
