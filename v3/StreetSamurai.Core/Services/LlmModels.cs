namespace StreetSamurai.Core.Services;

/// <summary>
/// Well-known model IDs for routing LLM calls by tier.
/// Post-beat extraction tasks (summarize, classify, YES/NO checks) use Haiku;
/// prose generation uses whatever ActiveLlmProvider + Model is configured.
/// </summary>
public static class LlmModels
{
    public const string Haiku = "claude-haiku-4-5-20251001";
}
