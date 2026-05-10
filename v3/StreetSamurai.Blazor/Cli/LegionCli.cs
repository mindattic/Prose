using Microsoft.Extensions.DependencyInjection;
using MindAttic.Legion;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface for the Legion / LLMVoting cloud-LLM panel. Lets autonomous
/// agents (or the user) self-serve a Quorum decision without writing a
/// custom service. Two shapes:
///
///   ss --legion ask "Question" --options "A,B,C" [--context "..."] [--quorum plurality|majority|supermajority|unanimous] [--max-tokens 512]
///       Force the panel to pick one of the supplied options. Prints the
///       winning Choice + Reasoning + Confidence + per-voter votes.
///
///   ss --legion vote "Question" [--context "..."] [--max-tokens 1024]
///       Open-ended vote — every model writes its own answer and Legion
///       returns a synthesized narrative + the individual responses.
///
/// Output is a JSON object on stdout so the result can be parsed by an
/// orchestrating script. Add --pretty to indent.
/// </summary>
public static class LegionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var sub = args.SkipWhile(a => a != "--legion").Skip(1).FirstOrDefault();
        if (sub is null or "help" or "-h" or "--help")
        {
            PrintUsage();
            return 0;
        }

        var voting = sp.GetRequiredService<LlmVotingService>();
        var question = ArgValue(args, "--question") ?? PositionalAfter(args, sub);
        var context  = ArgValue(args, "--context") ?? "";
        var maxTok   = int.TryParse(ArgValue(args, "--max-tokens"), out var mt) ? mt : 1024;
        var pretty   = args.Contains("--pretty");
        var quorum   = ParseQuorum(ArgValue(args, "--quorum"));

        if (string.IsNullOrWhiteSpace(question))
        {
            Console.Error.WriteLine("error: missing question. Try: ss --legion ask \"…\" --options \"A,B,C\"");
            return 1;
        }

        var jsonOpts = new System.Text.Json.JsonSerializerOptions { WriteIndented = pretty };

        if (sub == "ask")
        {
            var optionsRaw = ArgValue(args, "--options");
            if (string.IsNullOrWhiteSpace(optionsRaw))
            {
                Console.Error.WriteLine("error: --options required for ask. Try: ss --legion ask \"…\" --options \"A,B,C\"");
                return 1;
            }
            var options = optionsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var decision = await voting.DecideAsync(question, options, context, quorum, maxTok);
            var report = new
            {
                kind        = "decide",
                question,
                options,
                quorum      = quorum.ToString(),
                choice      = decision.Choice,
                reasoning   = decision.Reasoning,
                confidence  = decision.Confidence,
                voters_total      = decision.IndividualVotes?.Count ?? 0,
                voters_successful = decision.IndividualVotes?.Count(v => !v.IsError) ?? 0,
                quorum_reached    = decision.QuorumReached,
                votes = decision.IndividualVotes?.Select(v => new
                {
                    provider  = v.ProviderId,
                    decision  = v.Decision,
                    reasoning = v.Reasoning,
                    is_error  = v.IsError,
                    error     = v.ErrorMessage,
                }),
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, jsonOpts));
            return string.IsNullOrEmpty(decision.Choice) ? 2 : 0;
        }

        if (sub == "vote")
        {
            var req = new VoteRequest
            {
                Question  = question,
                Context   = context,
                MaxTokens = maxTok,
                SynthesizeNarrative = true,
            };
            var result = await voting.VoteAsync(req, quorum);
            var report = new
            {
                kind = "vote",
                question,
                quorum = quorum.ToString(),
                voters_total      = result.IndividualVotes.Count,
                voters_successful = result.IndividualVotes.Count(v => !v.IsError),
                quorum_reached    = result.QuorumReached,
                consensus         = result.Consensus,
                consensus_strength = result.ConsensusStrength,
                votes = result.IndividualVotes.Select(v => new
                {
                    provider  = v.ProviderId,
                    decision  = v.Decision,
                    reasoning = v.Reasoning,
                    is_error  = v.IsError,
                    error     = v.ErrorMessage,
                }),
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, jsonOpts));
            return 0;
        }

        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ss --legion ask \"Question\" --options \"A,B,C\" [--context \"...\"]");
        Console.WriteLine("                 [--quorum plurality|majority|supermajority|unanimous] [--max-tokens 512] [--pretty]");
        Console.WriteLine("    Force the cloud-LLM panel to pick one of the supplied options.");
        Console.WriteLine();
        Console.WriteLine("  ss --legion vote \"Question\" [--context \"...\"] [--max-tokens 1024] [--pretty]");
        Console.WriteLine("    Open-ended vote — every model writes its own answer; output includes a synthesized summary.");
        Console.WriteLine();
        Console.WriteLine("Output is JSON on stdout (`choice`, `reasoning`, `confidence`, per-voter votes).");
    }

    private static Quorum ParseQuorum(string? raw) => (raw ?? "").ToLowerInvariant() switch
    {
        "majority" or "simplemajority" or "simple-majority" => Quorum.SimpleMajority,
        "supermajority" or "twothirds" or "two-thirds"      => Quorum.TwoThirds,
        "unanimous"                                          => Quorum.Unanimous,
        _                                                    => Quorum.Plurality,
    };

    private static string? ArgValue(string[] args, string flag)
    {
        var idx = Array.IndexOf(args, flag);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }

    /// <summary>Returns the first positional argument that follows the subcommand and isn't a flag.</summary>
    private static string? PositionalAfter(string[] args, string sub)
    {
        var subIdx = Array.IndexOf(args, sub);
        if (subIdx < 0) return null;
        for (int i = subIdx + 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--")) return args[i];
            i++; // skip flag's value
        }
        return null;
    }
}
