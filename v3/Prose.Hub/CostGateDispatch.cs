using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Hub;

/// <summary>
/// Hub-side counterpart to Prose.Cli's <c>CostGateCli</c> — the ~15 commands that estimate
/// cost via <see cref="CommandCostEstimatorService"/> and prompt y/n above a threshold before
/// running (BookTokCli, AutoRunCli, HarvestVoiceCli, the review/panel commands, etc.).
///
/// The estimator and token ledger are Hub-resident singletons (same reason UniverseGraphService
/// is) — the ONLY thing that has to stay client-side is the actual terminal y/n prompt, since
/// the Hub has no real interactive console. So the protocol is two possible round trips:
///
///   1. Client posts here with NoConfirm=false. If the estimate is at/above threshold, this
///      returns NeedsConfirm=true + the estimate WITHOUT running anything.
///   2. Client (Prose.Cli's CostGateCli-equivalent) prints the estimate, reads the real y/n from
///      its own real terminal exactly as it always has, and — if yes — re-posts with
///      NoConfirm=true. This run actually executes the handler (via the same CliDispatch core
///      used by /api/cli-invoke) and records the actual cost.
///   3. If the first call was already under threshold, it runs+records in that single request.
/// </summary>
public static class CostGateDispatch
{
    private const double DefaultThreshold = 0.10;

    public sealed record CostGateRequest(
        string HandlerClass, string CommandName, string[] Args, string? Universe,
        string? Method = null, string? ExtraParamValue = null, string? Cwd = null,
        string? Stdin = null, bool NoConfirm = false);

    public sealed record CostGateResponse(bool NeedsConfirm, double Estimated, string Confidence, int ExitCode, string Output, string Error);

    public static async Task<IResult> InvokeAsync(CostGateRequest req, IServiceProvider sp)
    {
        var estimator = sp.GetRequiredService<CommandCostEstimatorService>();
        var estimate = await estimator.EstimateAsync(req.CommandName);

        if (!req.NoConfirm && estimate.Estimated >= DefaultThreshold)
            return Results.Ok(new CostGateResponse(true, estimate.Estimated, estimate.Confidence, 0, "", ""));

        var ledger = sp.GetRequiredService<TokenLedger>();

        // Scoped, not a before/after delta on the process-wide total: the Hub runs concurrent
        // invocations and background sweeps against one TokenLedger singleton, and a delta charges
        // this command for all of them. See LlmActionContext.BeginCostScope.
        using var costScope = LlmActionContext.BeginCostScope();

        LlmActionContext.Current = req.CommandName;
        CliDispatch.ExecuteOutcome outcome;
        try
        {
            var invokeReq = new CliDispatch.InvokeRequest(req.HandlerClass, req.Args, req.Universe, req.Method, req.ExtraParamValue, req.Cwd, req.Stdin);
            outcome = await CliDispatch.ExecuteCoreAsync(invokeReq, sp, source: "cost-gate");
        }
        finally
        {
            LlmActionContext.Current = null;
        }

        if (outcome.ErrorCode != null)
            return Results.NotFound(outcome.ErrorDetail);

        var response = outcome.Response!;
        var output = response.Output;

        var actualCost = ledger.CostForScope(costScope.Id);
        if (actualCost > 0)
        {
            try
            {
                var settings = sp.GetRequiredService<SettingsService>();
                await estimator.RecordActualAsync(req.CommandName, estimate.Estimated, actualCost, settings.ActiveLlmProvider ?? "claude-api");

                // CostGateCli.RecordActualAsync prints this line to the real console after the
                // command finishes. Here, Console.Out was already restored by the time this
                // runs (ExecuteCoreAsync's redirection scope already closed), so writing to it
                // would land in the Hub's own console instead of the caller's response — fold
                // it into the captured output the client prints instead.
                if (estimate.Estimated > 0)
                {
                    var delta = actualCost - estimate.Estimated;
                    var pct = delta / estimate.Estimated * 100;
                    output += $"  Cost: ${actualCost:F4} actual  (est ${estimate.Estimated:F3}, {(delta >= 0 ? "+" : "")}{pct:F0}%){Environment.NewLine}";
                }
            }
            catch
            {
                // Best-effort, same as CostGateCli.RecordActualAsync's own swallow-and-log —
                // a failed cost-history write must never fail the command that already ran.
            }
        }

        return Results.Ok(new CostGateResponse(false, estimate.Estimated, estimate.Confidence, response.ExitCode, output, response.Error));
    }
}
