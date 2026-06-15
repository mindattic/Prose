using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Legion persona quality voting for canon entities.
///
///   ss --review-entity [--type &lt;type&gt;] [--ballots N] [--prose N] [--unrated]
///
/// --type     : entity repo to target — character, weapon, technology, cyberware,
///              ammunition, equipment, genemod, transportation, automaton, subsidiary,
///              entertainment, apparel, material, pharmaceutical, consumer-good,
///              faction, district, contract, lab-specimen, psionic — or omit for ALL.
/// --ballots N: cheap score-only ballots per entity (default 30).
/// --prose N  : full prose upgrades for the most informative ballots (default 5).
/// --unrated  : only review entities with Rating == 0 (skip already-voted entries).
/// </summary>
public static class ReviewEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var entityType = Flag(args, "--type");
        var ballotStr  = Flag(args, "--ballots");
        var proseStr   = Flag(args, "--prose");
        var unrated    = args.Contains("--unrated");

        int ballots = int.TryParse(ballotStr, out var b) && b > 0 ? b : 30;
        int prose   = int.TryParse(proseStr,  out var p) && p >= 0 ? p : 5;

        Console.WriteLine($"=== Entity review ===");
        Console.WriteLine($"  type    : {entityType ?? "all"}");
        Console.WriteLine($"  ballots : {ballots} per entity");
        Console.WriteLine($"  prose   : {prose} prose upgrades");
        Console.WriteLine($"  unrated : {unrated}");
        Console.WriteLine();

        using var scope = sp.CreateScope();
        var reviewer = scope.ServiceProvider.GetRequiredService<EntityReviewService>();

        await reviewer.ReviewAllAsync(
            skipRated:   unrated,
            ballotCount: ballots,
            proseCount:  prose,
            entityType:  entityType,
            ct:          CancellationToken.None);

        Console.WriteLine();
        Console.WriteLine("Done. Top-rated leaderboard updates on next page load.");
        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
