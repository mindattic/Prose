using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Legion persona quality voting for canon entities.
///
///   ss --review-entity [--type &lt;type&gt;] [--ballots N] [--prose N] [--unrated]
///                      [--local] [--local-url URL] [--local-key KEY] [--local-model TAG]
///
/// --type       : entity repo to target (all non-character types), or omit for ALL.
/// --ballots N  : cheap score-only ballots per entity (default 30).
/// --prose N    : full prose upgrades for the most informative ballots (default 5).
/// --unrated    : only review entities with no existing reviews (skip already-voted entries).
/// --local      : route ballots to the configured local/remote OpenAI-compatible endpoint.
/// --local-url  : override the local endpoint URL for this run (implies --local); persisted.
/// --local-key  : bearer token for a secured remote endpoint (e.g. RunPod vLLM key); persisted.
/// --local-model: override the local model tag for this run (persisted).
/// </summary>
public static class ReviewEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var entityType = Flag(args, "--type");
        var ballotStr  = Flag(args, "--ballots");
        var proseStr   = Flag(args, "--prose");
        var unrated    = args.Contains("--unrated");
        var allowVotes = args.Contains("--allow-votes");

        int ballots = int.TryParse(ballotStr, out var b) && b > 0 ? b : 30;
        int prose   = int.TryParse(proseStr,  out var p) && p >= 0 ? p : 5;

        // SS-A44: entity ballot panels are disabled by default.
        var votingGate = sp.GetRequiredService<VotingGate>();
        try { votingGate.EnsureAllowed("review-entity", allowVotes); }
        catch (VotingDisabledException ex) { Console.Error.WriteLine($"[review-entity] {ex.Message}"); return 1; }

        // ── Local / RunPod mode ───────────────────────────────────────────────
        bool useLocal = args.Contains("--local");
        string? localUrl   = Flag(args, "--local-url");
        string? localKey   = Flag(args, "--local-key");
        string? localModel = Flag(args, "--local-model");

        if (!string.IsNullOrWhiteSpace(localUrl)) useLocal = true;

        // Persist settings so later runs reuse the same endpoint/key/model.
        if (useLocal || localUrl != null || localKey != null || localModel != null)
        {
            using var settingsScope = sp.CreateScope();
            var settings = settingsScope.ServiceProvider.GetRequiredService<StreetSamurai.Core.Services.SettingsService>();
            if (!string.IsNullOrWhiteSpace(localUrl))
                settings.LocalReviewBaseUrl = NormalizeLocalUrl(localUrl);
            if (localKey   != null) settings.LocalReviewApiKey = localKey;
            if (localModel != null) settings.LocalReviewModel  = localModel;
        }

        Console.WriteLine($"=== Entity review ===");
        Console.WriteLine($"  type    : {entityType ?? "all"}");
        Console.WriteLine($"  ballots : {ballots} per entity");
        Console.WriteLine($"  prose   : {prose} prose upgrades");
        Console.WriteLine($"  unrated : {unrated}");
        Console.WriteLine($"  local   : {useLocal} {(useLocal ? localUrl ?? "(persisted URL)" : "")}");
        Console.WriteLine();

        using var scope = sp.CreateScope();
        var reviewer = scope.ServiceProvider.GetRequiredService<EntityReviewService>();
        var cfg      = scope.ServiceProvider.GetRequiredService<StreetSamurai.Core.Services.SettingsService>();

        // Prefer arg values over persisted settings — avoids cross-process clobber when two
        // scoring jobs run simultaneously against different pods.
        string? resolvedUrl   = useLocal ? (!string.IsNullOrWhiteSpace(localUrl)   ? NormalizeLocalUrl(localUrl)   : cfg.LocalReviewBaseUrl)  : null;
        string? resolvedKey   = useLocal ? (localKey   ?? cfg.LocalReviewApiKey)   : null;
        string? resolvedModel = useLocal ? (localModel ?? cfg.LocalReviewModel)    : null;

        await reviewer.ReviewAllAsync(
            skipRated:   unrated,
            ballotCount: ballots,
            proseCount:  prose,
            entityType:  entityType,
            localUrl:    resolvedUrl,
            localKey:    resolvedKey,
            localModel:  resolvedModel,
            ct:          CancellationToken.None,
            allowVotes:  allowVotes);

        Console.WriteLine();
        Console.WriteLine("Done. Top-rated leaderboard and EntityReviewSummaries update complete.");
        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }

    private static string NormalizeLocalUrl(string url)
    {
        url = url.TrimEnd('/');
        if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            if (!url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) url += "/v1";
            url += "/chat/completions";
        }
        return url;
    }
}
