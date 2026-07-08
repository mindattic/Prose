using MindAttic.Legion;

namespace StreetSamurai.Core.Services.Local;

/// <summary>
/// Cloud review transport: a thin pass-through to <see cref="LegionClient"/>. This is
/// the DEFAULT path and is byte-for-byte the behaviour reviews have always had — it
/// reuses all of Legion's retry, circuit-breaker, and per-provider wire shaping.
/// It exists only so the local path can be a sibling implementation behind
/// <see cref="IReviewLlm"/>; it adds no logic of its own.
/// </summary>
public sealed class CloudReviewLlm : IReviewLlm
{
    private readonly LegionClient legion;

    public CloudReviewLlm(LegionClient legion) => this.legion = legion;

    public Task<string> CallAsync(
        string providerId, string apiKey, string model,
        string systemPrompt, string userMessage,
        int maxTokens = 2048, double temperature = 0.7, CancellationToken ct = default,
        bool cacheUserMessage = false)
        => legion.CallAsync(providerId, apiKey, model, systemPrompt, userMessage, maxTokens, temperature, ct,
            cachedSystemPrefix: null, cacheUserMessage: cacheUserMessage);
}
