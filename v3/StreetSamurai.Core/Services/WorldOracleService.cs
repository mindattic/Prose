using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// World Oracle — factual question-answering grounded strictly in documented canon.
///
/// Answers questions about the GLMZ world by assembling a narrow, relevant slice of the
/// world graph (semantic search → entity briefs → 1-hop neighbors → inferred links)
/// and asking the LLM to answer ONLY from that context. If the answer isn't documented,
/// it says so rather than hallucinating.
///
/// This is RAG over the world — not a fine-tuned model, just retrieval + grounding.
/// Works locally (Ollama) or on Azure (Claude API) via the existing ILlmService abstraction.
/// </summary>
public class WorldOracleService
{
    private readonly ILlmService llm;
    private readonly SemanticIndexService semanticIndex;
    private readonly WorldGraphService graph;
    private readonly InferenceService inference;
    private readonly ILogger<WorldOracleService> log;

    // How many semantic hits to pull context for (full briefs)
    private const int PrimaryHits = 8;
    // How many of the primary hits also get 1-hop neighbor context
    private const int NeighborDepthHits = 3;

    public WorldOracleService(
        ILlmService llm, SemanticIndexService semanticIndex,
        WorldGraphService graph, InferenceService inference,
        ILogger<WorldOracleService> log)
    {
        this.llm = llm;
        this.semanticIndex = semanticIndex;
        this.graph = graph;
        this.inference = inference;
        this.log = log;
    }

    /// <summary>
    /// Ask the oracle a question. Returns a strictly canon-grounded answer with source citations.
    /// </summary>
    public async Task<OracleAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new OracleAnswer { Question = question, Answer = "No question provided.", IsDocumented = false };

        // 1. Semantic search — find the most relevant nodes
        var candidates = semanticIndex.Search(question, topK: 20);
        if (candidates.Count == 0)
        {
            log.LogInformation("Oracle: no semantic hits for question '{Q}'", question);
            return new OracleAnswer
            {
                Question = question,
                Answer = "NOT DOCUMENTED — no relevant entities found in the world index.",
                IsDocumented = false,
                AskedAt = DateTime.UtcNow
            };
        }

        // 2. Build context from top hits + neighbors + inferred connections
        var (context, sourceNodes) = BuildContext(candidates);

        // 3. Grounded LLM query
        var (answer, isDocumented, undocumented) = await QueryLlmAsync(question, context, ct);

        log.LogInformation(
            "Oracle: answered '{Q}' (documented={Doc}, sources={Count})",
            question, isDocumented, sourceNodes.Count);

        return new OracleAnswer
        {
            Question = question,
            Answer = answer,
            IsDocumented = isDocumented,
            UndocumentedClaims = undocumented,
            Sources = sourceNodes,
            AskedAt = DateTime.UtcNow
        };
    }

    private (string context, List<OracleSource> sources) BuildContext(
        List<(string nodeId, double score)> candidates)
    {
        var sb = new StringBuilder();
        var seen = new HashSet<string>();
        var sources = new List<OracleSource>();

        // Full briefs for top PrimaryHits nodes
        foreach (var (nodeId, score) in candidates.Take(PrimaryHits))
        {
            if (!seen.Add(nodeId)) continue;
            var brief = graph.GetEntityBrief(nodeId);
            if (string.IsNullOrWhiteSpace(brief)) continue;

            sb.AppendLine(brief).AppendLine();

            var node = graph.GetNode(nodeId);
            if (node != null)
                sources.Add(new OracleSource
                {
                    EntityId = nodeId,
                    EntityName = node.Name,
                    EntityType = node.NodeType,
                    RelevanceScore = Math.Round(score, 3)
                });
        }

        // 1-hop neighbors for top NeighborDepthHits nodes (compact briefs only for new nodes)
        foreach (var (nodeId, _) in candidates.Take(NeighborDepthHits))
        {
            var neighbors = graph.GetNeighbors(nodeId, depth: 1);
            foreach (var neighbor in neighbors.Take(6))
            {
                if (!seen.Add(neighbor.Id)) continue;
                var brief = graph.GetEntityBrief(neighbor.Id);
                if (!string.IsNullOrWhiteSpace(brief))
                    sb.AppendLine(brief).AppendLine();
            }
        }

        // Inferred connections for the single top hit
        if (candidates.Count > 0)
        {
            var topId = candidates[0].nodeId;
            var inferred = inference.GetInferredConnections(topId, maxResults: 8);
            if (inferred.Count > 0)
            {
                sb.AppendLine("== Inferred Connections ==");
                foreach (var edge in inferred)
                    sb.AppendLine($"- {edge.Explanation} (confidence {edge.Confidence:F1})");
                sb.AppendLine();
            }
        }

        return (sb.ToString(), sources);
    }

    private async Task<(string answer, bool isDocumented, List<string> undocumented)> QueryLlmAsync(
        string question, string context, CancellationToken ct)
    {
        const string system = """
            You are the World Oracle for GLMZ — the single authoritative reference for documented world canon.

            STRICT RULES — do not break these:
            1. Answer ONLY using the entity context provided. Never invent, infer, or assume beyond it.
            2. If the answer is not present in the context, is_documented must be false.
            3. If partial info exists, answer what is documented and list what is missing in undocumented_claims.
            4. Cite entity names (e.g. "per [Entity Name]:") when pulling from their data.
            5. Never invent names, relationships, locations, events, prices, or any world detail.
            6. Concise answers — one to three sentences unless the question requires enumeration.

            Respond with JSON only:
            {
              "answer": "the answer, or NOT DOCUMENTED if nothing relevant in context",
              "is_documented": true | false,
              "undocumented_claims": ["specific things the question asks for that are absent from context"]
            }
            """;

        var user = $"== WORLD CONTEXT ==\n{context}\n\n== QUESTION ==\n{question}";

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.1, 2048, ct: ct);
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3].TrimEnd();

            var raw = JsonDocument.Parse(json).RootElement;
            var answer = raw.TryGetProperty("answer", out var a) ? a.GetString() ?? "" : response;
            var isDoc = !raw.TryGetProperty("is_documented", out var d) || d.GetBoolean();
            var undoc = raw.TryGetProperty("undocumented_claims", out var u)
                ? u.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList()
                : [];
            return (answer, isDoc, undoc);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Oracle: LLM parse failed, returning raw response");
            return (context.Length > 0 ? "Oracle error — try again." : "NOT DOCUMENTED", false, []);
        }
    }
}

public class OracleAnswer
{
    [JsonPropertyName("question")] public string Question { get; set; } = "";
    [JsonPropertyName("answer")] public string Answer { get; set; } = "";
    [JsonPropertyName("is_documented")] public bool IsDocumented { get; set; }
    [JsonPropertyName("undocumented_claims")] public List<string> UndocumentedClaims { get; set; } = [];
    [JsonPropertyName("sources")] public List<OracleSource> Sources { get; set; } = [];
    [JsonPropertyName("asked_at")] public DateTime AskedAt { get; set; } = DateTime.UtcNow;
}

public class OracleSource
{
    [JsonPropertyName("entity_id")] public string EntityId { get; set; } = "";
    [JsonPropertyName("entity_name")] public string EntityName { get; set; } = "";
    [JsonPropertyName("entity_type")] public string EntityType { get; set; } = "";
    [JsonPropertyName("relevance_score")] public double RelevanceScore { get; set; }
}
