using MindAttic.Legion;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Pure-math cost estimator for review runs. No LLM calls — just pricing arithmetic
/// rendered as a console table so the operator can confirm before spending money.
/// All voter calls use cacheUserMessage=true, so the story text is written to the
/// Anthropic ephemeral cache once and read by every subsequent voter at 10% cost.
/// </summary>
public static class ReviewCostEstimator
{
    private record ModelPricing(
        string Label,
        double InputPerMtok,
        double CacheWritePerMtok,
        double CacheReadPerMtok,
        double OutputPerMtok);

    private static readonly Dictionary<string, ModelPricing> KnownPricing =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-haiku-4-5-20251001"] = new("Haiku 4.5",   0.80,  1.00, 0.08,  4.00),
            ["claude-sonnet-4-6"]         = new("Sonnet 4.6",  3.00,  3.75, 0.30, 15.00),
            ["claude-opus-4-8"]           = new("Opus 4.8",   15.00, 18.75, 1.50, 75.00),
        };

    // Tokens for the ballot system prompt (persona description + rubric).
    private const int SystemTokensPerVoter = 1_500;

    public record CostEstimate(
        string StoryTitle,
        int BeatCount,
        int StoryTokens,
        int VoterCount,
        string Model,
        string ModelLabel,
        bool BallotOnly,
        int OutputTokensPerVoter,
        double CacheWriteCost,
        double CacheReadCost,
        double SystemCost,
        double OutputCost,
        double TotalCachedCost,
        double TotalUncachedCost);

    /// <param name="storyTitle">Display name for the table header.</param>
    /// <param name="beatCount">Enabled beat count (used to compute output budget).</param>
    /// <param name="storyTokens">Approximate story token count (chars / 4).</param>
    /// <param name="voterCount">Number of ballot voters.</param>
    /// <param name="model">Model ID — e.g. "claude-haiku-4-5-20251001".</param>
    /// <param name="ballotOnly">True = score-only ballot; false = full prose review.</param>
    public static CostEstimate Estimate(
        string storyTitle, int beatCount, int storyTokens,
        int voterCount, string model, bool ballotOnly)
    {
        var pricing = KnownPricing.TryGetValue(model, out var p) ? p
            : KnownPricing["claude-haiku-4-5-20251001"]; // safe default

        // Output budget per voter (mirrors NodeReviewService logic)
        var outputPerVoter = ballotOnly
            ? Math.Min(8_000, 900 + beatCount * 6)
            : 1_400;

        // Cached path — voter 1 writes, voters 2-N read the story from cache
        var cacheWrite  = storyTokens / 1_000_000.0 * pricing.CacheWritePerMtok;
        var cacheRead   = storyTokens / 1_000_000.0 * pricing.CacheReadPerMtok * Math.Max(0, voterCount - 1);
        var systemCost  = SystemTokensPerVoter / 1_000_000.0 * pricing.InputPerMtok * voterCount;
        var outputCost  = outputPerVoter / 1_000_000.0 * pricing.OutputPerMtok * voterCount;
        var totalCached = cacheWrite + cacheRead + systemCost + outputCost;

        // Uncached baseline (all voters pay full input price — for comparison only)
        var uncachedInput = (storyTokens + SystemTokensPerVoter) / 1_000_000.0 * pricing.InputPerMtok * voterCount;
        var totalUncached = uncachedInput + outputCost;

        return new CostEstimate(
            storyTitle, beatCount, storyTokens, voterCount,
            model, pricing.Label, ballotOnly, outputPerVoter,
            cacheWrite, cacheRead, systemCost, outputCost,
            totalCached, totalUncached);
    }

    /// <summary>Renders a multi-line cost breakdown table for console output.</summary>
    public static string RenderTable(CostEstimate e)
    {
        var modeLabel   = e.BallotOnly ? "score ballots (per-beat + gripes)" : "full prose reviews";
        var saving      = e.TotalUncachedCost > 0
            ? (1 - e.TotalCachedCost / e.TotalUncachedCost) * 100 : 0;
        var readVoters  = Math.Max(0, e.VoterCount - 1);
        var outputTotal = e.OutputTokensPerVoter * e.VoterCount;

        var sep  = new string('─', 60);
        var dbl  = new string('━', 60);

        return $"""
            {dbl}
              REVIEW COST ESTIMATE
              Story : {e.StoryTitle}  ({e.BeatCount} beats  ·  ~{e.StoryTokens:N0} tokens)
              Panel : {e.VoterCount} voters  ·  {e.ModelLabel}  ·  {modeLabel}
            {sep}
              Component                     Tokens           Cost
            {sep}
              Story text — cache write ×1   {e.StoryTokens,12:N0}       ${e.CacheWriteCost:F3}
              Story text — cache read  ×{readVoters,-2}   {e.StoryTokens * readVoters,12:N0}       ${e.CacheReadCost:F3}
              System / persona overhead      {SystemTokensPerVoter * e.VoterCount,12:N0}       ${e.SystemCost:F3}
              Output (≤{e.OutputTokensPerVoter} tok × {e.VoterCount})          {outputTotal,12:N0}       ${e.OutputCost:F3}
            {sep}
              TOTAL   (with caching)                         ${e.TotalCachedCost:F2}
              Baseline (no caching)                          ${e.TotalUncachedCost:F2}
              Cache saving                                    {saving:F1}%
            {dbl}
            """;
    }

    /// <summary>Looks up the cheapest configured model for a given provider.</summary>
    public static string CheapModelFor(string providerId) => providerId.ToLowerInvariant() switch
    {
        "claude-api"  => "claude-haiku-4-5-20251001",
        "claude-team" => "claude-haiku-4-5-20251001",
        _             => "claude-haiku-4-5-20251001",
    };
}
