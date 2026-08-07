namespace Prose.Core.Services;

/// <summary>
/// Well-known Claude model IDs by tier.
/// Post-beat extraction tasks (summarize, classify, YES/NO checks) use Haiku;
/// prose generation uses whatever ActiveLlmProvider + Model is configured.
/// </summary>
public static class LlmModels
{
    /// <summary>Fastest and cheapest Claude tier. Default for auxiliary tasks (extraction, classification).</summary>
    public const string Haiku  = "claude-haiku-4-5-20251001";

    /// <summary>Mid-tier Claude. Default for prose drafting in the generation pipeline.</summary>
    public const string Sonnet = "claude-sonnet-5";

    /// <summary>Highest-capability Claude tier. Used for prose polish and review passes.</summary>
    public const string Opus   = "claude-opus-4-8";
}
