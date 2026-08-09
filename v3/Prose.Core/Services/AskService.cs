using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

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
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly ILogger<AskService> log;

    public AskService(
        EmbeddingService embeddings,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        ILogger<AskService> log)
    {
        this.embeddings = embeddings;
        this.dbFactory  = dbFactory;
        this.llm        = llm;
        this.log        = log;
    }

    public sealed record Citation(Guid EntityId, string EntityName, string EntityType, double Similarity);
    public sealed record ProseCitation(Guid BeatId, string NodeSlug, string NodeTitle, int Position, double Similarity);
    public sealed record AskAnswer(string Answer, IReadOnlyList<Citation> Citations, int CorpusChunks, TimeSpan Duration)
    {
        /// <summary>Story-prose passages used as context (node beats). Empty when the
        /// question was answered from entity canon alone.</summary>
        public IReadOnlyList<ProseCitation> ProseCitations { get; init; } = Array.Empty<ProseCitation>();
    }

    /// <summary>
    /// Answer a free-form question. Hybrid retrieval: the K most relevant canon
    /// ENTITIES (character/world facts) plus the most relevant STORY PROSE
    /// (node beats). When <paramref name="nodeScope"/> is set the prose side
    /// is scoped to that one node — and if the node fits the char budget its
    /// whole text is supplied, so single-book Q&amp;A is exhaustive rather than
    /// sampled. Returns a grounded answer with entity + prose citations.
    /// </summary>
    public async Task<AskAnswer> AnswerAsync(
        string question,
        int retrieveK = 8,
        int maxAnswerTokens = 1500,
        IReadOnlyCollection<string>? entityTypes = null,
        Guid? nodeScope = null,
        int retrieveProse = 6,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new AskAnswer("(no question)", Array.Empty<Citation>(), 0, TimeSpan.Zero);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var entityHits = await embeddings.FindSimilarAsync(question, retrieveK, entityTypes, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // ── Story prose (node beats) ────────────────────────────────────
        var proseBlock = new StringBuilder();
        var proseCitations = new List<ProseCitation>();
        int proseChunks = 0;

        if (nodeScope is Guid sid)
        {
            // Scoped to one node: pull every enabled beat in order. A novella
            // fits in context, so the answer is drawn from the whole book rather
            // than a sample. Cap total chars defensively for very long nodes.
            var node = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == sid)
                .Select(s => new { s.Slug, s.Title })
                .FirstOrDefaultAsync(ct);
            // Recurses past any nested Collection (2026-08-09 fix).
            var askSearchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, sid, ct);

            var beats = await (from sb in db.BeatNodes.AsNoTracking()
                               join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                               where askSearchIds.Contains(sb.NodeId) && sb.IsEnabled
                               orderby sb.SortKey
                               select new { b.Id, b.Text, b.Title }).ToListAsync(ct);

            const long CharBudget = 90_000; // ~22k tokens — safe for a novella
            long used = 0;
            int pos = 0;
            foreach (var b in beats)
            {
                pos++;
                if (used > CharBudget) break;
                var heading = string.IsNullOrWhiteSpace(b.Title) ? $"Ch {pos}" : $"Ch {pos} — {b.Title}";
                proseBlock.AppendLine($"[{heading}]");
                proseBlock.AppendLine(b.Text);
                proseBlock.AppendLine();
                used += b.Text?.Length ?? 0;
                proseChunks++;
                proseCitations.Add(new ProseCitation(b.Id, node?.Slug ?? "", node?.Title ?? "", pos, 1.0));
            }
        }
        else if (retrieveProse > 0)
        {
            // Unscoped: semantic retrieval over all embedded node beats.
            var proseHits = await embeddings.FindSimilarBeatNodesAsync(question, retrieveProse, null, ct);
            if (proseHits.Count > 0)
            {
                var pids = proseHits.Select(h => h.ScopeId).ToHashSet();
                var texts = await db.Beats.AsNoTracking()
                    .Where(b => pids.Contains(b.Id))
                    .Select(b => new { b.Id, b.Text, b.Title })
                    .ToDictionaryAsync(b => b.Id, ct);
                var member = await (from sb in db.BeatNodes.AsNoTracking()
                                    join s in db.Nodes.AsNoTracking() on sb.NodeId equals s.Id
                                    where pids.Contains(sb.BeatId) && sb.IsEnabled
                                    select new { sb.BeatId, s.Slug, s.Title }).ToListAsync(ct);
                var memberMap = member.GroupBy(m => m.BeatId).ToDictionary(g => g.Key, g => g.First());

                const int MaxPerBeat = 3000;
                foreach (var h in proseHits)
                {
                    if (!texts.TryGetValue(h.ScopeId, out var b)) continue;
                    memberMap.TryGetValue(h.ScopeId, out var m);
                    var chunk = (b.Text?.Length ?? 0) > MaxPerBeat ? b.Text![..MaxPerBeat] + "…" : b.Text ?? "";
                    var label = (m?.Title ?? "story") + (string.IsNullOrWhiteSpace(b.Title) ? "" : " — " + b.Title);
                    proseBlock.AppendLine($"--- {label} (similarity {h.Similarity:F3}) ---");
                    proseBlock.AppendLine(chunk);
                    proseBlock.AppendLine();
                    proseChunks++;
                    proseCitations.Add(new ProseCitation(h.ScopeId, m?.Slug ?? "", m?.Title ?? "", 0, Math.Round(h.Similarity, 3)));
                }
            }
        }

        // ── Entity canon ──────────────────────────────────────────────────
        var entityBlock = new StringBuilder();
        int entityChunks = 0;
        if (entityHits.Count > 0)
        {
            var eids = entityHits.Select(h => h.EntityId).ToHashSet();
            var blobs = await db.Records.AsNoTracking()
                .Where(r => eids.Contains(r.EntityId))
                .Select(r => new { r.EntityId, r.Json })
                .ToDictionaryAsync(r => r.EntityId, r => r.Json, ct);
            const int MaxPerEntity = 4000;
            foreach (var hit in entityHits)
            {
                if (!blobs.TryGetValue(hit.EntityId, out var json) || string.IsNullOrEmpty(json)) continue;
                var chunk = json.Length > MaxPerEntity ? json[..MaxPerEntity] + "…" : json;
                entityBlock.AppendLine($"--- {hit.EntityName} ({hit.EntityType}) — similarity {hit.Similarity:F3} ---");
                entityBlock.AppendLine(chunk);
                entityBlock.AppendLine();
                entityChunks++;
            }
        }

        if (proseChunks == 0 && entityChunks == 0)
        {
            sw.Stop();
            return new AskAnswer(
                "I couldn't find any canon material similar to that question. The embedding cache may be empty — run `prose --reembed` (entities), or pass --node <slug> to index and search a story's beats.",
                Array.Empty<Citation>(), 0, sw.Elapsed);
        }

        var prompt = new StringBuilder();
        if (proseBlock.Length > 0)
        {
            prompt.AppendLine(nodeScope is null
                ? "STORY PROSE (most relevant passages, best-first):"
                : "STORY PROSE (the full scoped story, in order):");
            prompt.AppendLine();
            prompt.Append(proseBlock);
            prompt.AppendLine();
        }
        if (entityBlock.Length > 0)
        {
            prompt.AppendLine("CANON CONTEXT (most semantically related entities, best-first):");
            prompt.AppendLine();
            prompt.Append(entityBlock);
            prompt.AppendLine();
        }
        prompt.Append("QUESTION: ").Append(question);

        const string SystemPrompt =
            "You are a research assistant for the Prose canon. Answer the user's question " +
            "using ONLY the STORY PROSE and CANON CONTEXT provided below — do not invent facts. " +
            "Prefer the STORY PROSE for plot/character/event questions and the CANON CONTEXT for " +
            "world/entity facts. If the context doesn't contain a clear answer, say so. Cite specifics " +
            "(character names, chapters) when you use them. Keep answers concise unless asked for depth.";

        string answer;
        try
        {
            answer = await llm.GenerateAsync(SystemPrompt, prompt.ToString(), temperature: 0.2,
                maxTokens: maxAnswerTokens, ct: ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogWarning(ex, "AskService LLM call failed");
            sw.Stop();
            return new AskAnswer($"(LLM call failed: {ex.Message})", Array.Empty<Citation>(),
                proseChunks + entityChunks, sw.Elapsed) { ProseCitations = proseCitations };
        }

        sw.Stop();
        var citations = entityHits
            .Select(h => new Citation(h.EntityId, h.EntityName, h.EntityType, Math.Round(h.Similarity, 3)))
            .ToList();
        return new AskAnswer(answer, citations, proseChunks + entityChunks, sw.Elapsed)
            { ProseCitations = proseCitations };
    }
}
