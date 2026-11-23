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
    /// <summary>
    /// The answer as it is produced (RFC 0019, RA-T3.6.1.4). The default yields
    /// <see cref="GenerateAsync"/>'s whole result as one chunk, so every provider supports it; a provider
    /// whose transport can stream overrides it to yield text as it arrives.
    /// </summary>
    async IAsyncEnumerable<string> StreamAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096,
        string? model = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return await GenerateAsync(system, user, temperature, maxTokens, model, ct);
    }

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
