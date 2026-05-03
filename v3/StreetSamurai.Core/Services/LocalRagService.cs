using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using MindAttic.Legion.Providers;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Local-first answer primitive. Retrieves the top-k corpus chunks via
/// EmbeddingIndexService, prepends them to the prompt, and calls the local
/// Qwen voter through Legion. This is the entrypoint any fact-bound or
/// retrieval-heavy task should use instead of the full multi-provider Quorum.
///
/// Cost: zero (local inference). Speed: ~1–3 seconds per call. Privacy: full.
/// Use the multi-provider Quorum only for literary judgment where remote
/// reasoning depth matters more than corpus grounding.
/// </summary>
public class LocalRagService
{
    private readonly EmbeddingIndexService index;
    private readonly LlmVotingProvider voting;
    private readonly OllamaClient ollama;
    private readonly ILogger<LocalRagService> log;

    public LocalRagService(
        EmbeddingIndexService index,
        LlmVotingProvider voting,
        OllamaClient ollama,
        ILogger<LocalRagService> log)
    {
        this.index   = index;
        this.voting  = voting;
        this.ollama  = ollama;
        this.log     = log;
    }

    public async Task<string> AnswerAsync(
        string question,
        string? systemRole = null,
        int retrieveK = 8,
        int maxTokens = 1024,
        double temperature = 0.2,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        var hits = await index.SearchAsync(question, retrieveK, ct);
        var context = hits.Count == 0
            ? "(no corpus matches found — answer from the question alone or say you don't know)"
            : string.Join("\n\n---\n\n",
                hits.Select(h => $"[{h.FilePath} · chunk {h.ChunkIndex}]\n{h.Text}"));

        var system = systemRole ?? DefaultSystem;
        var user   = $"Context from the live corpus:\n\n{context}\n\n---\n\nQuestion: {question}";

        return await voting.CallAsync(
            providerId: "ollama",
            systemPrompt: system,
            userMessage: user,
            maxTokens: maxTokens,
            temperature: temperature,
            ct: ct);
    }

    public async Task<string> AskWithCitedAsync(
        string question,
        Func<IReadOnlyList<SearchHit>, Task>? onCitations = null,
        int retrieveK = 8,
        CancellationToken ct = default)
    {
        var hits = await index.SearchAsync(question, retrieveK, ct);
        if (onCitations != null) await onCitations(hits);
        return await AnswerWithHitsAsync(question, hits, ct: ct);
    }

    public async Task<string> AnswerWithHitsAsync(
        string question,
        IReadOnlyList<SearchHit> hits,
        string? systemRole = null,
        int maxTokens = 1024,
        double temperature = 0.2,
        CancellationToken ct = default)
    {
        var context = hits.Count == 0
            ? "(no corpus matches)"
            : string.Join("\n\n---\n\n",
                hits.Select(h => $"[{h.FilePath} · chunk {h.ChunkIndex}]\n{h.Text}"));

        var system = systemRole ?? DefaultSystem;
        var user   = $"Context from the live corpus:\n\n{context}\n\n---\n\nQuestion: {question}";

        return await voting.CallAsync(
            providerId: "ollama",
            systemPrompt: system,
            userMessage: user,
            maxTokens: maxTokens,
            temperature: temperature,
            ct: ct);
    }

    public bool IsAvailable() => index.GetStats().ChunkCount > 0;

    private const string DefaultSystem =
        "You are a fact-grounded assistant for the StreetSamurai world. Answer strictly " +
        "from the provided context. If the context does not contain the answer, say so " +
        "plainly. Cite the source file paths you used. Be concise.";
}
