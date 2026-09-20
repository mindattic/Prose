using Microsoft.Extensions.Configuration;

namespace Prose.Core.Services;

/// <summary>
/// Config-bound switches for <see cref="ContinuityLongSweepService"/>'s unattended Trinity
/// Reconciliation auto-reconcile path. Read via plain <see cref="IConfiguration.GetValue{T}(string, T)"/>,
/// matching this codebase's existing background-service convention (no <c>IOptions&lt;T&gt;</c>
/// anywhere in this pattern — see <see cref="ContinuityLongSweepService"/>'s own
/// <c>BackgroundServices:Enabled</c> read).
///
/// <para><c>Enabled=false</c> and <c>ShadowMode=true</c> by default is the rollout-safe posture:
/// an operator must flip both flags in config, in sequence, to reach live unattended edits.
/// Flipping <c>TrinityAutoReconcile:Enabled</c> IS the human authorization act, exactly parallel
/// to passing <c>--allow-votes --confirm-auto-edit</c> by hand on the CLI (see
/// <see cref="VotingGate"/>).</para>
/// </summary>
public class TrinityAutoReconcileOptions
{
    /// <summary>Master switch. False = the scheduled sweep only surveys/logs, exactly as before
    /// this feature existed. "TrinityAutoReconcile:Enabled".</summary>
    public bool Enabled { get; }

    /// <summary>True = every auto-reconcile call runs with <c>dryRun:true</c> — decisions are
    /// logged and fully inspectable via <c>ReconciliationDecisions</c>, but no prose/bible/entity
    /// edit is ever made. Flip to false only after reviewing a shadow-mode soak period for
    /// surprises. "TrinityAutoReconcile:ShadowMode".</summary>
    public bool ShadowMode { get; }

    /// <summary>Circuit breaker: max distinct books touched in one scheduled tick.
    /// "TrinityAutoReconcile:MaxBooksPerRun".</summary>
    public int MaxBooksPerRun { get; }

    /// <summary>Circuit breaker: max total decisions made across all books in one scheduled
    /// tick — once reached, the tick stops immediately and leaves the rest for next time.
    /// "TrinityAutoReconcile:MaxEditsPerRun".</summary>
    public int MaxEditsPerRun { get; }

    public TrinityAutoReconcileOptions(IConfiguration configuration)
    {
        Enabled        = configuration.GetValue("TrinityAutoReconcile:Enabled", defaultValue: false);
        ShadowMode     = configuration.GetValue("TrinityAutoReconcile:ShadowMode", defaultValue: true);
        MaxBooksPerRun = configuration.GetValue("TrinityAutoReconcile:MaxBooksPerRun", defaultValue: 3);
        MaxEditsPerRun = configuration.GetValue("TrinityAutoReconcile:MaxEditsPerRun", defaultValue: 10);
    }
}
