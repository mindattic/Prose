namespace Prose.Core.Services;

/// <summary>
/// Ambient "which command/action is currently generating" tag. Prose.Cli's CostGateCli
/// sets this around a CLI command's lifetime; <see cref="LlmRouter"/> reads
/// it when writing <see cref="Data.Entities.LlmCallHistory"/> rows — so a query like
/// <c>SELECT * FROM LlmCallHistories WHERE Action = '--write-story'</c> shows which model handled
/// which action, per the Multi-LLM Master Switch-Over plan's audit-trail requirement.
/// AsyncLocal, not a plain static field, so concurrent hosts (Blazor, MCP) don't cross-talk.
/// Callers that never set it (MCP tools, Blazor UI actions, ad-hoc scripts) simply get an
/// "(unspecified)" action tag on their rows — a graceful degradation, not an error.
/// </summary>
public static class LlmActionContext
{
    private static readonly AsyncLocal<string?> current = new();
    public static string? Current { get => current.Value; set => current.Value = value; }

    /// <summary>
    /// Ambient "which beat is currently generating" — sibling to <see cref="Current"/>, same
    /// AsyncLocal mechanism. Set once by <c>ProseWriterRouter.WriteAsync</c> right before
    /// calling <c>BeatGeneratorService.GenerateBeatAsync</c>; read by <see cref="LlmRouter"/>
    /// when writing a <see cref="Data.Entities.LlmPromptCapture"/> row, so the Beat Context
    /// Archive can find the exact prompt/response a given beat actually saw without widening
    /// <c>ILlmService.GenerateWithCachedPrefixAsync</c>'s signature (which every provider
    /// implementation would then need to accept and thread through for no other reason).
    /// Null for any call not made from a beat-write context (e.g. a review panel, a canon
    /// interpretation pass) — a graceful "no beat" rather than an error.
    /// </summary>
    private static readonly AsyncLocal<Guid?> currentBeatId = new();
    public static Guid? CurrentBeatId { get => currentBeatId.Value; set => currentBeatId.Value = value; }

    /// <summary>
    /// The cost-attribution scopes currently open on this async flow, innermost last.
    ///
    /// <para><b>Why an ambient list rather than a before/after total.</b> Every cost-gated caller
    /// used to compute its actual spend as <c>TokenLedger.GetSummary().TotalCost</c> sampled before
    /// and after the command. <see cref="TokenLedger"/> is a process singleton, so that delta
    /// silently swept up every OTHER LLM call the Hub made in the same window — a second concurrent
    /// CLI invocation, or the daily <c>SanityScanBackgroundService</c> sweep, which bills with no
    /// CLI invocation at all. Observed live 2026-09-04: a <c>--ledger-adjudicate</c> re-run that
    /// adjudicated <b>zero</b> groups (368 cache hits, no LLM calls) reported <b>$3.85</b> — the
    /// entire spend of a concurrent run of the same command that finished 450ms earlier. The
    /// counters were honest; the cost figure was attributed to the wrong run, and
    /// <c>CommandCostEstimatorService.RecordActualAsync</c> then learned that wrong number as
    /// training data for every future estimate of that command.</para>
    ///
    /// <para>A list, not a single id, so nesting attributes correctly: a command that runs
    /// cost-gated sub-commands (<c>AutoRunCli</c>) still sees the whole of its own spend, and each
    /// sub-command sees only its own. An entry is credited to every scope open when it was
    /// recorded. Work with no scope open (background services) is credited to nobody, which is the
    /// point.</para>
    /// </summary>
    private static readonly AsyncLocal<Guid[]?> costScopes = new();

    public static Guid[] CostScopes => costScopes.Value ?? [];

    /// <summary>Opens a cost-attribution scope for the duration of the returned handle. Read the
    /// scope's spend back with <see cref="TokenLedger.CostForScope"/>.</summary>
    public static CostScope BeginCostScope() => new();

    /// <summary>Handle for one open cost-attribution scope. Restores the enclosing scope set on
    /// dispose, so an early return or a throw cannot leak the scope onto unrelated work.</summary>
    public sealed class CostScope : IDisposable
    {
        private readonly Guid[]? previous;
        private bool disposed;

        public Guid Id { get; } = Guid.NewGuid();

        internal CostScope()
        {
            previous = costScopes.Value;
            costScopes.Value = [.. previous ?? [], Id];
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            costScopes.Value = previous;
        }
    }
}
