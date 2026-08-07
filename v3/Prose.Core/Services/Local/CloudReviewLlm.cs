using MindAttic.Legion;

namespace Prose.Core.Services.Local;

/// <summary>
/// Cloud review transport: a thin pass-through to <see cref="LegionClient"/>. This is
/// the DEFAULT path and is byte-for-byte the behaviour reviews have always had — it
/// reuses all of Legion's retry, circuit-breaker, and per-provider wire shaping.
/// It exists only so the local path can be a sibling implementation behind
/// <see cref="IReviewLlm"/>; it adds no logic of its own beyond recording the call
/// on <see cref="TokenLedger"/> (Legion doesn't surface usage objects, so this is the
/// same chars/4 estimate <see cref="TokenLedger.Record"/> always uses — but without this,
/// review-panel calls never touch the ledger at all, on either the CLI or MCP entry point,
/// which silently zeroes out cost tracking for every review/vote run).
/// </summary>
public sealed class CloudReviewLlm : IReviewLlm
{
    private readonly LegionClient legion;
    private readonly TokenLedger ledger;

    public CloudReviewLlm(LegionClient legion, TokenLedger ledger)
    {
        this.legion = legion;
        this.ledger = ledger;
    }

    public async Task<string> CallAsync(
        string providerId, string apiKey, string model,
        string systemPrompt, string userMessage,
        int maxTokens = 2048, double temperature = 0.7, CancellationToken ct = default,
        bool cacheUserMessage = false)
    {
        var result = await legion.CallAsync(providerId, apiKey, model, systemPrompt, userMessage, maxTokens, temperature, ct,
            cachedSystemPrefix: null, cacheUserMessage: cacheUserMessage);
        ledger.Record(providerId, model, systemPrompt + userMessage, result);
        return result;
    }
}
