using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --auto-correct-undo (--run-id &lt;guid&gt; | --last-n &lt;N&gt;)
/// prose --auto-correct-status [--list-runs]
///
/// "Rewind the tape" for a nightly AutoCorrect run — reverses logged actions in DESCENDING
/// Sequence (or AppliedAt, for --last-n), exactly as they'd be undone by rewinding. See
/// <see cref="SelfHealLedgerService"/> for why this is a per-action ledger, not database-wide
/// temporal versioning.
/// </summary>
public static class AutoCorrectUndoCli
{
    public static async Task<int> RunUndoAsync(string[] args, IServiceProvider services)
    {
        var runIdStr = Flag(args, "--run-id");
        var lastNStr = Flag(args, "--last-n");

        if (string.IsNullOrWhiteSpace(runIdStr) && string.IsNullOrWhiteSpace(lastNStr))
        {
            Console.Error.WriteLine("Usage: prose --auto-correct-undo (--run-id <guid> | --last-n <N>)");
            return 2;
        }

        var ledger = services.GetRequiredService<SelfHealLedgerService>();

        if (!string.IsNullOrWhiteSpace(runIdStr))
        {
            if (!Guid.TryParse(runIdStr, out var runId))
            {
                Console.Error.WriteLine($"[auto-correct-undo] '{runIdStr}' is not a valid run id.");
                return 2;
            }
            var n = await ledger.UndoRunAsync(runId);
            Console.WriteLine($"[auto-correct-undo] Reversed {n} action(s) from run {runId}.");
            // The undo stops at the first failure; say so in the exit code, not just the count.
            await using (var db = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync())
            {
                var left = await db.SelfHealActions.CountAsync(a => a.RunId == runId && a.UndoneAt == null);
                if (left > 0) { Console.Error.WriteLine($"[auto-correct-undo] {left} action(s) of that run could NOT be reversed — see the Hub log."); return 1; }
                if (n == 0) { Console.Error.WriteLine("[auto-correct-undo] Nothing to reverse for that run id."); return 1; }
            }
            return 0;
        }

        if (!int.TryParse(lastNStr, out var count) || count <= 0)
        {
            Console.Error.WriteLine($"[auto-correct-undo] --last-n must be a positive integer, got '{lastNStr}'.");
            return 2;
        }
        await using var db2 = await services.GetRequiredService<IDbContextFactory<ProseDbContext>>().CreateDbContextAsync();
        var pending = Math.Min(count, await db2.SelfHealActions.CountAsync(a => a.UndoneAt == null));
        var undone = await ledger.UndoLastNActionsAsync(count);
        Console.WriteLine($"[auto-correct-undo] Reversed {undone} of the {count} most recently applied action(s).");
        if (undone < pending) { Console.Error.WriteLine($"[auto-correct-undo] Stopped after {undone} of {pending} — see the Hub log."); return 1; }
        return 0;
    }

    public static async Task<int> RunStatusAsync(string[] args, IServiceProvider services)
    {
        var ledger = services.GetRequiredService<SelfHealLedgerService>();
        var runs = await ledger.ListRunsAsync(limit: 20);

        if (runs.Count == 0)
        {
            Console.WriteLine("[auto-correct-status] No AutoCorrect runs recorded yet.");
            return 0;
        }

        Console.WriteLine("[auto-correct-status] Recent AutoCorrect runs:");
        foreach (var r in runs)
        {
            Console.WriteLine($"  {r.RunId}  {r.FirstAppliedAt:u}  {r.TotalActions} action(s), {r.UndoneActions} undone  [{string.Join(", ", r.ActionTypes)}]");
        }
        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
