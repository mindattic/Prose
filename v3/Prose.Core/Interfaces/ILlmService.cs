namespace Prose.Core.Interfaces;

public interface ILlmService
{
    Task<bool> IsConfiguredAsync();
    Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default);

    /// <summary>
    /// Prompt-caching overload: <paramref name="cachedPrefix"/> is sent as an Anthropic
    /// ephemeral-cache block (first system block, cache_control: ephemeral) and
    /// <paramref name="dynamicSystem"/> is sent as the uncached second block. Providers
    /// that don't support Anthropic caching fall back to concatenation.
    /// </summary>
    virtual Task<string> GenerateWithCachedPrefixAsync(
        string cachedPrefix,
        string dynamicSystem,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
        => GenerateAsync(
            string.IsNullOrEmpty(dynamicSystem) ? cachedPrefix : cachedPrefix + "\n\n" + dynamicSystem,
            user, temperature, maxTokens, model, ct);
}
