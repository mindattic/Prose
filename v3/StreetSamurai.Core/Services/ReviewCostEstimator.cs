using MindAttic.Legion;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Pure-math cost estimator for review runs. No LLM calls — just pricing arithmetic
/// rendered as a console table so the operator can confirm before spending money.
/// All voter calls use cacheUserMessage=true; every run uses prompt caching.
///
/// Column layout — every content row is exactly <see cref="RowWidth"/> characters:
///   indent(2) + label(LabelW) + gap(1) + tokens(TokenW) + gap(2) + cost(CostW)
/// Total rows and TOTAL rows reflow the label to fill the same width before the cost.
/// Costs are ceiling-rounded to the nearest penny: $0.01 not $0.009.
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

    // Column widths — all rows are built to exactly RowWidth chars.
    private const int LabelW  = 32;   // component label, left-aligned
    private const int TokenW  = 10;   // token count, right-aligned
    private const int CostW   = 8;    // cost "$X.XX", right-aligned (covers up to $999.99)
    private const int RowWidth = 2 + LabelW + 1 + TokenW + 2 + CostW;  // = 55

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
        double TotalCost);

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
            : KnownPricing["claude-haiku-4-5-20251001"];

        var outputPerVoter = ballotOnly
            ? Math.Min(8_000, 900 + beatCount * 6)
            : 1_400;

        var cacheWrite = storyTokens / 1_000_000.0 * pricing.CacheWritePerMtok;
        var cacheRead  = storyTokens / 1_000_000.0 * pricing.CacheReadPerMtok * Math.Max(0, voterCount - 1);
        var system     = SystemTokensPerVoter / 1_000_000.0 * pricing.InputPerMtok * voterCount;
        var output     = outputPerVoter / 1_000_000.0 * pricing.OutputPerMtok * voterCount;

        return new CostEstimate(
            storyTitle, beatCount, storyTokens, voterCount,
            model, pricing.Label, ballotOnly, outputPerVoter,
            cacheWrite, cacheRead, system, output,
            cacheWrite + cacheRead + system + output);
    }

    /// <summary>Compute cost using the ACTUAL output token count returned by the run,
    /// instead of the worst-case formula. All other inputs are the same as Estimate.</summary>
    public static CostEstimate EstimateActual(
        string storyTitle, int beatCount, int storyTokens,
        int votersFired, string model, bool ballotOnly, int outputTokensActual)
    {
        var pricing = KnownPricing.TryGetValue(model, out var p) ? p
            : KnownPricing["claude-haiku-4-5-20251001"];

        var cacheWrite = storyTokens / 1_000_000.0 * pricing.CacheWritePerMtok;
        var cacheRead  = storyTokens / 1_000_000.0 * pricing.CacheReadPerMtok * Math.Max(0, votersFired - 1);
        var system     = SystemTokensPerVoter / 1_000_000.0 * pricing.InputPerMtok * votersFired;
        var output     = outputTokensActual / 1_000_000.0 * pricing.OutputPerMtok;
        var outputPerVoter = votersFired > 0 ? outputTokensActual / votersFired : 0;

        return new CostEstimate(
            storyTitle, beatCount, storyTokens, votersFired,
            model, pricing.Label, ballotOnly, outputPerVoter,
            cacheWrite, cacheRead, system, output,
            cacheWrite + cacheRead + system + output);
    }

    /// <summary>Renders the pre-vote ESTIMATE table.</summary>
    public static string RenderTable(CostEstimate e)
    {
        var modeLabel  = e.BallotOnly ? "score ballots (per-beat + gripes)" : "ballots + prose upgrades";
        var readVoters = Math.Max(0, e.VoterCount - 1);
        var outTok     = e.OutputTokensPerVoter * e.VoterCount;

        return string.Join("\n",
            Line('━'),
            $"  REVIEW COST ESTIMATE",
            $"  Story : {e.StoryTitle}  ({e.BeatCount} beats  ·  ~{e.StoryTokens:N0} tokens)",
            $"  Panel : {e.VoterCount} voters  ·  {e.ModelLabel}  ·  {modeLabel}",
            Line('─'),
            DataHeader(),
            Line('─'),
            DataRow($"Story text — cache write ×1",        e.StoryTokens,                      e.CacheWriteCost),
            DataRow($"Story text — cache read  ×{readVoters}", e.StoryTokens * readVoters,         e.CacheReadCost),
            DataRow($"System / persona overhead",           SystemTokensPerVoter * e.VoterCount, e.SystemCost),
            DataRow($"Output (≤{e.OutputTokensPerVoter:N0} tok × {e.VoterCount})", outTok,      e.OutputCost),
            Line('─'),
            TotalRow("TOTAL", e.TotalCost),
            Line('━'));
    }

    /// <summary>Renders the post-run ACTUAL SPEND receipt.</summary>
    public static string RenderActualTable(CostEstimate e, int actualOutputTokens)
    {
        var modeLabel  = e.BallotOnly ? "score ballots" : "ballots + prose upgrades";
        var readVoters = Math.Max(0, e.VoterCount - 1);

        return string.Join("\n",
            Line('━'),
            $"  ACTUAL SPEND",
            $"  Story : {e.StoryTitle}  ({e.BeatCount} beats  ·  ~{e.StoryTokens:N0} story tokens)",
            $"  Panel : {e.VoterCount} voters  ·  {e.ModelLabel}  ·  {modeLabel}",
            Line('─'),
            DataHeader(),
            Line('─'),
            DataRow($"Story text — cache write ×1",        e.StoryTokens,                      e.CacheWriteCost),
            DataRow($"Story text — cache read  ×{readVoters}", e.StoryTokens * readVoters,         e.CacheReadCost),
            DataRow($"System / persona overhead",           SystemTokensPerVoter * e.VoterCount, e.SystemCost),
            DataRow($"Output (actual)",                     actualOutputTokens,                  e.OutputCost),
            Line('─'),
            TotalRow("TOTAL", e.TotalCost),
            Line('━'));
    }

    /// <summary>Looks up the cheapest configured model for a given provider.</summary>
    public static string CheapModelFor(string providerId) => providerId.ToLowerInvariant() switch
    {
        "claude-api"  => "claude-haiku-4-5-20251001",
        "claude-team" => "claude-haiku-4-5-20251001",
        _             => "claude-haiku-4-5-20251001",
    };

    // ── Formatting helpers ────────────────────────────────────────────────────────

    // Ceiling to nearest penny, formatted as $X.XX.
    private static string C(double v) => $"${Math.Ceiling(v * 100) / 100:F2}";

    // Separator line — always RowWidth + some breathing room (60 chars).
    private static string Line(char ch) => new(ch, Math.Max(RowWidth + 5, 60));

    // Header row — same column positions as data rows.
    private static string DataHeader() =>
        "  " + "Component".PadRight(LabelW) + " " +
        "Tokens".PadLeft(TokenW) + "  " +
        "Cost".PadLeft(CostW);

    // Data row: label left-padded to LabelW, tokens right-padded to TokenW, cost right-padded to CostW.
    // Total width is always RowWidth chars.
    private static string DataRow(string label, int tokens, double cost)
    {
        var lbl = label.Length <= LabelW
            ? label.PadRight(LabelW)
            : label[..LabelW];   // truncate if somehow oversized
        return "  " + lbl + " " + tokens.ToString("N0").PadLeft(TokenW) + "  " + C(cost).PadLeft(CostW);
    }

    // Total row: description fills (LabelW + 1 + TokenW + 2) = the same space as label + tokens + gaps.
    // Cost is right-aligned to CostW, ending at the same column as data rows.
    private static string TotalRow(string description, double cost)
    {
        var fillWidth = LabelW + 1 + TokenW + 2;   // same as label + gap + tokens + gap in DataRow
        var desc = description.Length <= fillWidth
            ? description.PadRight(fillWidth)
            : description[..fillWidth];
        return "  " + desc + C(cost).PadLeft(CostW);
    }
}
