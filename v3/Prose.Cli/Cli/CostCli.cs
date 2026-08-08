using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI handler for <c>prose --cost</c>.
///
/// In long-running processes (MCP server, Blazor host) the <see cref="TokenLedger"/>
/// accumulates every LLM call. In a one-shot CLI invocation the ledger captures only
/// the calls made during that run — use <c>--cost</c> as a suffix on any command to
/// see how much that operation spent, e.g.:
///   <code>prose --write-node --slug foo --cost</code>
///
/// Usage:
///   prose --cost             print the session cost table
///   prose --cost --json      emit the summary as JSON
///   prose --cost --reset     clear the ledger (no output)
/// </summary>
public static class CostCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var ledger = services.GetRequiredService<TokenLedger>();

        if (args.Contains("--reset"))
        {
            ledger.Clear();
            Console.WriteLine("Token ledger cleared.");
            return Task.FromResult(0);
        }

        if (args.Contains("--json"))
        {
            var summary = ledger.GetSummary();
            var json = JsonSerializer.Serialize(new
            {
                sessionStart  = summary.SessionStart,
                callCount     = summary.CallCount,
                inputTokens   = summary.InputTokens,
                outputTokens  = summary.OutputTokens,
                totalCost     = Math.Round(summary.TotalCost, 6),
                byModel = summary.ByModel.Values
                    .OrderByDescending(m => m.TotalCost)
                    .Select(m => new
                    {
                        model        = m.Model,
                        label        = m.Label,
                        callCount    = m.CallCount,
                        inputTokens  = m.InputTokens,
                        outputTokens = m.OutputTokens,
                        totalCost    = Math.Round(m.TotalCost, 6),
                    }),
            }, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
            return Task.FromResult(0);
        }

        Console.WriteLine(ledger.RenderReport());
        return Task.FromResult(0);
    }

    /// <summary>
    /// Convenience method: print the cost report if <c>--cost</c> is present in args.
    /// Call this at the end of any CLI handler that performs LLM calls.
    /// </summary>
    public static void PrintIfRequested(string[] args, IServiceProvider services)
    {
        if (!args.Contains("--cost")) return;
        var ledger = services.GetRequiredService<TokenLedger>();
        Console.WriteLine();
        Console.WriteLine(ledger.RenderReport());
    }
}
