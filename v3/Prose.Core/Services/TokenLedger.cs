using System.Collections.Concurrent;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Process-lifetime accumulator for LLM token usage and cost estimates.
/// Each <see cref="LlmRouter"/> call contributes one <see cref="LedgerEntry"/>;
/// the ledger is a singleton so all callers in the same process share one tally.
///
/// Token counts are estimated from text length (chars / 4) because the Legion
/// transport returns plain text and does not surface Anthropic usage objects.
/// Costs use the same per-model pricing table as <see cref="ReviewCostEstimator"/>.
/// </summary>
public sealed class TokenLedger
{
    private record ModelPricing(string Label, double InputPerMtok, double OutputPerMtok);

    private static readonly Dictionary<string, ModelPricing> Pricing =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [LlmModels.Haiku]  = new("Haiku 4.5",   0.80,  4.00),
            [LlmModels.Sonnet] = new("Sonnet 4.6",  3.00, 15.00),
            [LlmModels.Opus]   = new("Opus 4.8",   15.00, 75.00),
        };

    /// <summary>One recorded LLM call.</summary>
    public sealed record LedgerEntry(
        DateTimeOffset At,
        string Provider,
        string Model,
        int InputTokens,
        int OutputTokens,
        double InputCost,
        double OutputCost)
    {
        public double TotalCost => InputCost + OutputCost;
    }

    private readonly ConcurrentBag<LedgerEntry> entries = new();
    private readonly DateTimeOffset sessionStart = DateTimeOffset.UtcNow;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Records one LLM call. Token counts are estimated from text length (chars / 4).
    /// Call this from <see cref="LlmRouter"/> after each successful generation.
    /// Prefer <see cref="RecordActual"/> when the API response surfaces exact token counts.
    /// </summary>
    public void Record(string provider, string model, string inputText, string outputText)
    {
        var inputTok  = Math.Max(1, (inputText.Length  + 3) / 4);
        var outputTok = Math.Max(1, (outputText.Length + 3) / 4);

        var pricing = Pricing.TryGetValue(model, out var p)
            ? p
            : Pricing[LlmModels.Haiku];

        var inputCost  = inputTok  / 1_000_000.0 * pricing.InputPerMtok;
        var outputCost = outputTok / 1_000_000.0 * pricing.OutputPerMtok;

        entries.Add(new LedgerEntry(
            At:          DateTimeOffset.UtcNow,
            Provider:    provider,
            Model:       model,
            InputTokens:  inputTok,
            OutputTokens: outputTok,
            InputCost:    inputCost,
            OutputCost:   outputCost));
    }

    /// <summary>
    /// Records one LLM call using exact token counts from the API response.
    /// Use this overload when the transport surfaces Anthropic usage objects —
    /// it is more accurate than the chars/4 estimation in <see cref="Record"/>.
    /// </summary>
    public void RecordActual(string provider, string model, int inputTokens, int outputTokens)
    {
        var pricing = Pricing.TryGetValue(model, out var p)
            ? p
            : Pricing[LlmModels.Haiku];

        entries.Add(new LedgerEntry(
            At:          DateTimeOffset.UtcNow,
            Provider:    provider,
            Model:       model,
            InputTokens:  Math.Max(1, inputTokens),
            OutputTokens: Math.Max(1, outputTokens),
            InputCost:    Math.Max(1, inputTokens)  / 1_000_000.0 * pricing.InputPerMtok,
            OutputCost:   Math.Max(1, outputTokens) / 1_000_000.0 * pricing.OutputPerMtok));
    }

    /// <summary>Returns a snapshot of all recorded entries, oldest first.</summary>
    public IReadOnlyList<LedgerEntry> GetEntries()
        => entries.OrderBy(e => e.At).ToList();

    /// <summary>Returns the aggregated session summary.</summary>
    public SessionSummary GetSummary()
    {
        var all = entries.ToList();
        return new SessionSummary(
            SessionStart:  sessionStart,
            CallCount:     all.Count,
            InputTokens:   all.Sum(e => e.InputTokens),
            OutputTokens:  all.Sum(e => e.OutputTokens),
            TotalCost:     all.Sum(e => e.TotalCost),
            ByModel:       all
                .GroupBy(e => e.Model)
                .ToDictionary(
                    g => g.Key,
                    g => new ModelSummary(
                        Model:        g.Key,
                        Label:        Pricing.TryGetValue(g.Key, out var pr) ? pr.Label : g.Key,
                        CallCount:    g.Count(),
                        InputTokens:  g.Sum(e => e.InputTokens),
                        OutputTokens: g.Sum(e => e.OutputTokens),
                        TotalCost:    g.Sum(e => e.TotalCost))));
    }

    /// <summary>Renders an ASCII cost report table to a string.</summary>
    public string RenderReport()
    {
        var s = GetSummary();
        if (s.CallCount == 0)
            return "  No LLM calls recorded in this session.";

        var elapsed = DateTimeOffset.UtcNow - s.SessionStart;
        var sb = new StringBuilder();
        const int W = 60;

        sb.AppendLine(new string('━', W));
        sb.AppendLine("  SESSION COST REPORT");
        sb.AppendLine($"  Duration : {FormatElapsed(elapsed)}   Calls : {s.CallCount}");
        sb.AppendLine(new string('─', W));
        sb.AppendLine(
            "  " + "Model".PadRight(14) +
            "Calls".PadLeft(6) +
            "Input tok".PadLeft(12) +
            "Output tok".PadLeft(12) +
            "Cost".PadLeft(10));
        sb.AppendLine(new string('─', W));

        foreach (var (_, m) in s.ByModel.OrderByDescending(kv => kv.Value.TotalCost))
        {
            sb.AppendLine(
                "  " + m.Label.PadRight(14) +
                m.CallCount.ToString("N0").PadLeft(6) +
                m.InputTokens.ToString("N0").PadLeft(12) +
                m.OutputTokens.ToString("N0").PadLeft(12) +
                $"${m.TotalCost:F4}".PadLeft(10));
        }

        sb.AppendLine(new string('─', W));
        sb.AppendLine(
            "  " + "TOTAL".PadRight(14) +
            s.CallCount.ToString("N0").PadLeft(6) +
            s.InputTokens.ToString("N0").PadLeft(12) +
            s.OutputTokens.ToString("N0").PadLeft(12) +
            $"${s.TotalCost:F4}".PadLeft(10));
        sb.Append(new string('━', W));
        return sb.ToString();
    }

    /// <summary>Resets the ledger for the current session (useful in tests and the Blazor UI reset action).</summary>
    public void Clear() => entries.Clear();

    // ── Supporting records ────────────────────────────────────────────────────

    public sealed record SessionSummary(
        DateTimeOffset SessionStart,
        int CallCount,
        int InputTokens,
        int OutputTokens,
        double TotalCost,
        Dictionary<string, ModelSummary> ByModel);

    public sealed record ModelSummary(
        string Model,
        string Label,
        int CallCount,
        int InputTokens,
        int OutputTokens,
        double TotalCost);

    // ── Formatting ────────────────────────────────────────────────────────────

    private static string FormatElapsed(TimeSpan t)
    {
        if (t.TotalSeconds < 60)  return $"{t.TotalSeconds:F1}s";
        if (t.TotalMinutes < 60)  return $"{(int)t.TotalMinutes}m {t.Seconds}s";
        return $"{(int)t.TotalHours}h {t.Minutes}m";
    }
}
