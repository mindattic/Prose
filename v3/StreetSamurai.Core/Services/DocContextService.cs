using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Assembles the rotating cast of pertinent canon <c>.md</c> documents for a context — the
/// document analog of <see cref="EntityContextService"/>. Sources from the <c>MarkdownFiles</c>
/// table (classified into always/node/topic by <see cref="MarkdownFileService"/>) and the
/// <c>markdown</c> prose-embedding scope, layering into a token-budgeted block:
///
///   1. ALWAYS  — the small universal core (every context).
///   2. NODE  — docs whose Scope matches the active node CODE (the one bible + one register).
///   3. TOPIC (keyword)    — topic docs whose Triggers appear in the scene/goal text.
///   4. TOPIC (embedding)  — topic docs semantically near the text (markdown embedding scope).
///
/// The <see cref="DocContextStack"/> holds the working set: pinned always/node, decaying topic.
/// </summary>
public sealed class DocContextService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    DocContextStack stack,
    EmbeddingService embeddings,
    ILogger<DocContextService> log)
{
    /// <summary>ProseEmbeddings ScopeKind for a tracked markdown file (MarkdownFile.Id keyed).</summary>
    public const string ScopeMarkdown = "markdown";

    private const double EmbeddingFloor = 0.50;
    private const int CharsPerToken = 4;
    private const int EmbeddingK = 6;

    public sealed record LoadedDoc(Guid DocId, string RelativePath, string Tier, string Reason, double Score, int Chars);
    public sealed record DocContextResult(string Block, IReadOnlyList<LoadedDoc> Loaded, int EstimatedTokens);

    private sealed record Candidate(Guid Id, string RelativePath, string Tier, string Scope, string Triggers);

    /// <summary>
    /// Load the doc working set for this context and return the budgeted block plus the
    /// resident docs (with provenance). Read-only against canon; safe to call in dry-run.
    /// </summary>
    public async Task<DocContextResult> PrepareContextAsync(
        Guid contextId, string? nodeCode, string? triggerText, int tokenBudget = 2000,
        bool includeAlways = true, bool includeNode = true, bool useEmbedding = true,
        CancellationToken ct = default)
    {
        stack.BeginAction(contextId);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var candidates = await db.MarkdownFiles.AsNoTracking()
            .Select(m => new Candidate(m.Id, m.RelativePath, m.Tier, m.Scope, m.Triggers))
            .ToListAsync(ct);

        var code = (nodeCode ?? "").Trim();
        var text = triggerText ?? "";

        // 1 — always (universal core)
        if (includeAlways)
            foreach (var c in candidates.Where(c => c.Tier == "always"))
                stack.Push(contextId, MakeEntry(c, "always", 100));

        // 2 — node (scope match): the story's one bible + one register + story docs
        if (includeNode)
            foreach (var c in candidates.Where(c => c.Tier == "node" && ScopeMatches(c.Scope, code)))
                stack.Push(contextId, MakeEntry(c, string.IsNullOrEmpty(code) ? "node:*" : $"node:{code}", 90));

        // 3 — topic via keyword triggers
        if (text.Length > 0)
            foreach (var c in candidates.Where(c => c.Tier == "topic"))
            {
                var hit = FirstKeywordHit(c.Triggers, text);
                if (hit != null) stack.Push(contextId, MakeEntry(c, $"keyword:{hit}", 50));
            }

        // 4 — topic via semantic embedding (markdown scope)
        if (useEmbedding && text.Length > 0)
        {
            try
            {
                var byId = candidates.ToDictionary(c => c.Id);
                var hits = await embeddings.FindSimilarProseAsync(text, EmbeddingK, ScopeMarkdown, ct);
                foreach (var h in hits.Where(h => h.Similarity >= EmbeddingFloor))
                    if (byId.TryGetValue(h.ScopeId, out var c) && c.Tier == "topic")
                        stack.Push(contextId, MakeEntry(c, $"embedding {h.Similarity:0.00}", h.Similarity));
            }
            catch (Exception ex)
            {
                // Engine must work offline — keyword + tier still deliver.
                log.LogWarning(ex, "Doc embedding pass unavailable; keyword + tier only");
            }
        }

        return await BuildBlockAsync(db, contextId, tokenBudget, ct);
    }

    /// <summary>
    /// Engine convenience: resolve the node's CODE from its Id and prepare the doc context,
    /// using the node Id as the LRU context key. Used by ProseWriterRouter.
    /// </summary>
    public async Task<DocContextResult> PrepareForNodeAsync(
        Guid nodeId, string? triggerText, int tokenBudget = 2000, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return new DocContextResult("", Array.Empty<LoadedDoc>(), 0);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var code = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => s.NodeCode)
            .FirstOrDefaultAsync(ct) ?? "";
        return await PrepareContextAsync(nodeId, code, triggerText, tokenBudget, ct: ct);
    }

    /// <summary>
    /// Session-hook convenience: surface ONLY the topic docs pertinent to the latest turn text
    /// (keyword-only by default for speed — no per-turn embedding API call). Always/node tiers
    /// are session-scope concerns handled elsewhere (SessionStart digest, on-demand bibles).
    /// </summary>
    public Task<DocContextResult> PrepareSessionContextAsync(
        Guid sessionId, string? triggerText, int tokenBudget = 1200, bool useEmbedding = false, CancellationToken ct = default)
        => PrepareContextAsync(sessionId, nodeCode: null, triggerText, tokenBudget,
                               includeAlways: false, includeNode: false, useEmbedding: useEmbedding, ct: ct);

    /// <summary>Refresh the working set from generated prose / latest turn text (keeps relevant docs warm).</summary>
    public void ReconcileFromText(Guid contextId, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var active = stack.GetActive(contextId);
        var mentioned = active
            .Where(e => e.Tier != "topic" || FirstKeywordHit(e.Triggers, text) != null)
            .Select(e => e.DocId);
        stack.RecordMentions(contextId, mentioned);
    }

    public IReadOnlyList<DocContextStack.StackEntry> GetActive(Guid contextId) => stack.GetActive(contextId);
    public void ClearContext(Guid contextId) => stack.Clear(contextId);

    // ── block building ────────────────────────────────────────────────────────

    private async Task<DocContextResult> BuildBlockAsync(
        StreetSamuraiDbContext db, Guid contextId, int tokenBudget, CancellationToken ct)
    {
        var active = stack.GetActive(contextId);
        if (active.Count == 0)
            return new DocContextResult("", Array.Empty<LoadedDoc>(), 0);

        var ids = active.Select(a => a.DocId).ToList();
        var contentById = await db.MarkdownFiles.AsNoTracking()
            .Where(m => ids.Contains(m.Id))
            .Select(m => new { m.Id, m.Content })
            .ToDictionaryAsync(x => x.Id, x => x.Content ?? "", ct);

        var sb = new StringBuilder();
        sb.AppendLine("DOC CONTEXT — pertinent canon docs in working memory:");
        sb.AppendLine();

        var loaded = new List<LoadedDoc>();
        var budgetChars = tokenBudget * CharsPerToken;
        int usedChars = 0;
        string? lastTier = null;

        foreach (var e in active)
        {
            // Node-tier docs are the story's bible + register — the do-not-contradict layer.
            // A 1500c clip loses character rules and locks (how BLST drifted); give them room.
            var perDocCap = e.Tier switch { "topic" => 800, "node" => 6000, _ => 1500 };
            var clip = StripFrontmatter(contentById.GetValueOrDefault(e.DocId, ""));
            if (clip.Length > perDocCap) clip = clip[..perDocCap].TrimEnd() + "…";

            if (usedChars + clip.Length > budgetChars && loaded.Count > 0) break;

            if (e.Tier != lastTier)
            {
                sb.AppendLine($"[{e.Tier.ToUpperInvariant()}]");
                lastTier = e.Tier;
            }
            sb.AppendLine($"- {e.RelativePath}  ({e.Reason})");
            if (clip.Length > 0) sb.AppendLine(clip);
            sb.AppendLine();

            usedChars += clip.Length;
            loaded.Add(new LoadedDoc(e.DocId, e.RelativePath, e.Tier, e.Reason, e.Score, clip.Length));
        }

        return new DocContextResult(sb.ToString().TrimEnd(), loaded, usedChars / CharsPerToken);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static DocContextStack.StackEntry MakeEntry(Candidate c, string reason, double score) =>
        new(c.Id, c.RelativePath, c.Tier, c.Scope, c.Triggers, reason, score, 0, 0);

    private static bool ScopeMatches(string scope, string code)
    {
        if (string.IsNullOrWhiteSpace(scope)) return false;
        if (scope.Trim() == "*") return true;
        if (string.IsNullOrWhiteSpace(code)) return false;
        return scope.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(s => s.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private static string? FirstKeywordHit(string triggers, string text)
    {
        if (string.IsNullOrWhiteSpace(triggers)) return null;
        foreach (var kw in triggers.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (kw.Length < 4) continue;
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase))
                return kw;
        }
        return null;
    }

    private static string StripFrontmatter(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";
        var text = content.Replace("\r\n", "\n");
        if (!text.StartsWith("---\n")) return text.Trim();
        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0) return text.Trim();
        var after = end + 4;
        return after < text.Length ? text[after..].TrimStart('\n', '-', ' ').Trim() : "";
    }
}
