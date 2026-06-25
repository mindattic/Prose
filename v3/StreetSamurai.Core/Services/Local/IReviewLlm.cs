namespace StreetSamurai.Core.Services.Local;

/// <summary>
/// The single transport seam the strand-review path calls through. Mirrors the
/// <c>LegionClient.CallAsync(provider, key, model, system, user, …)</c> signature
/// exactly, so swapping the cloud panel for a local model is a binding choice, not
/// a rewrite of the review machinery.
///
/// <para>Two implementations exist and never mix:
/// <see cref="CloudReviewLlm"/> delegates verbatim to the cloud
/// <c>MindAttic.Legion.LegionClient</c> (the trusted-4 panel, with all of Legion's
/// retry / circuit-breaker / wire shaping); <see cref="LocalReviewLlm"/> talks to a
/// local OpenAI-compatible endpoint (Ollama) and references no Legion transport code
/// at all. A review run picks ONE for its whole lifetime.</para>
/// </summary>
public interface IReviewLlm
{
    /// <summary>
    /// Send one system+user prompt to a model and return the raw completion text.
    /// For the cloud impl, <paramref name="providerId"/>/<paramref name="apiKey"/>/
    /// <paramref name="model"/> select the vendor; for the local impl, only
    /// <paramref name="model"/> matters (the Ollama tag) and the others are ignored.
    /// </summary>
    Task<string> CallAsync(
        string providerId,
        string apiKey,
        string model,
        string systemPrompt,
        string userMessage,
        int maxTokens = 2048,
        double temperature = 0.7,
        CancellationToken ct = default);
}
