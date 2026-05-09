using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Cloud-side RAG over the canonical entity corpus. Replaces the retired
/// local-Ollama <c>/ask</c> path. Pipeline:
/// <list type="number">
///   <item>Embed the user's question via <see cref="EmbeddingService"/></item>
///   <item>Retrieve top-K most similar entities from EntityEmbeddings (cosine, exact NN)</item>
///   <item>Pull each hit's <c>Records.Json</c> blob, render a context block</item>
///   <item>Send (system instructions + retrieved context + question) to <see cref="ILlmService"/></item>
///   <item>Return the answer + structured citations the UI can link back to entity pages</item>
/// </list>
/// </summary>
public class AskService
{
    private readonly EmbeddingService embeddings;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly ILogger<AskService> log;

    public AskService(
        EmbeddingService embeddings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILlmService llm,
        ILogger<AskService> log)
    {
        this.embeddings = embeddings;
        this.dbFactory  = dbFactory;
        this.llm        = llm;
        this.log        = log;
    }

    public sealed record Citation(Guid EntityId, string EntityName, string EntityType, double Similarity);
    public sealed record AskAnswer(string Answer, IReadOnlyList<Citation> Citations, int CorpusChunks, TimeSpan Duration);

    /// <summary>
    /// Answer a free-form natural-language question against the canon corpus.
    /// Pulls the K most semantically-relevant entities, prepends their canon
    /// JSON as context, asks the LLM for a grounded answer.
    /// </summary>
    public async Task<AskAnswer> AnswerAsync(
        string question,
        int retrieveK = 8,
        int maxAnswerTokens = 1500,
        IReadOnlyCollection<string>? entityTypes = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new AskAnswer("(no question)", Array.Empty<Citation>(), 0, TimeSpan.Zero);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var hits = await embeddings.FindSimilarAsync(question, retrieveK, entityTypes, ct);
        if (hits.Count == 0)
        {
            sw.Stop();
            return new AskAnswer(
                "I couldn't find any canon material similar to that question. The embedding cache may be empty — run `ss --reembed`.",
                Array.Empty<Citation>(), 0, sw.Elapsed);
        }

        // Pull each hit's Records.Json so the LLM has rich context, not just a name.
        var ids = hits.Select(h => h.EntityId).ToHashSet();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var blobs = await db.Records.AsNoTracking()
            .Where(r => ids.Contains(r.EntityId))
            .Select(r => new { r.EntityId, r.Json })
            .ToDictionaryAsync(r => r.EntityId, r => r.Json, ct);

        var contextBlock = new StringBuilder();
        contextBlock.AppendLine("CANON CONTEXT (top-K most semantically related entities, sorted best-first):");
        contextBlock.AppendLine();
        int chunkCount = 0;
        foreach (var hit in hits)
        {
            if (!blobs.TryGetValue(hit.EntityId, out var json) || string.IsNullOrEmpty(json)) continue;
            // Cap each blob so a single huge entity doesn't dominate the prompt.
            const int MaxPerEntity = 4000;
            var chunk = json.Length > MaxPerEntity ? json[..MaxPerEntity] + "…" : json;
            contextBlock.AppendLine($"--- {hit.EntityName} ({hit.EntityType}) — similarity {hit.Similarity:F3} ---");
            contextBlock.AppendLine(chunk);
            contextBlock.AppendLine();
            chunkCount++;
        }

        const string SystemPrompt =
            "You are a research assistant for the StreetSamurai canon. Answer the user's question " +
            "using ONLY the CANON CONTEXT provided below — do not invent facts. If the context " +
            "doesn't contain a clear answer, say so. Cite specific entities by name when you use " +
            "their information. Keep answers concise unless the question asks for depth.";

        var userPrompt = contextBlock.ToString() + "\nQUESTION: " + question;

        string answer;
        try
        {
            answer = await llm.GenerateAsync(SystemPrompt, userPrompt, temperature: 0.2,
                maxTokens: maxAnswerTokens, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "AskService LLM call failed");
            sw.Stop();
            return new AskAnswer($"(LLM call failed: {ex.Message})", Array.Empty<Citation>(), chunkCount, sw.Elapsed);
        }

        sw.Stop();
        var citations = hits
            .Select(h => new Citation(h.EntityId, h.EntityName, h.EntityType, Math.Round(h.Similarity, 3)))
            .ToList();
        return new AskAnswer(answer, citations, chunkCount, sw.Elapsed);
    }
}
