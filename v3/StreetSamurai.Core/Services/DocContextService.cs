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
    ILogger<DocContextService> log,
    UserContextService? userContext = null,
    EntityDocService? entityDocs = null)
{
    /// <summary>ProseEmbeddings ScopeKind for a tracked markdown file (MarkdownFile.Id keyed).</summary>
    public const string ScopeMarkdown = "markdown";

    private const double EmbeddingFloor = 0.50;
    private const int CharsPerToken = 4;
    private const int EmbeddingK = 6;

    public sealed record LoadedDoc(Guid DocId, string RelativePath, string Tier, string Reason, double Score, int Chars);
    public sealed record DocContextResult(string Block, IReadOnlyList<LoadedDoc> Loaded, int EstimatedTokens);

    private sealed record Candidate(Guid Id, string RelativePath, string Tier, string Scope, string Triggers, string RelatedIds);

    /// <summary>
    /// Load the doc working set for this context and return the budgeted block plus the
    /// resident docs (with provenance). Read-only against canon; safe to call in dry-run.
    /// </summary>
    /// <param name="pinnedDocIds">Doc IDs to force-include regardless of LRU tier (score 999).</param>
    /// <param name="excludedDocIds">Doc IDs to exclude even if they would normally be injected.</param>
    public async Task<DocContextResult> PrepareContextAsync(
        Guid contextId, string? nodeCode, string? triggerText, int tokenBudget = 2000,
        bool includeAlways = true, bool includeNode = true, bool useEmbedding = true,
        IReadOnlySet<Guid>? pinnedDocIds = null,
        IReadOnlySet<Guid>? excludedDocIds = null,
        CancellationToken ct = default)
    {
        var code = (nodeCode ?? "").Trim();
        // Dynamic Context Memory: pass node code so the stack can evict stale node-tier docs on story change.
        stack.BeginAction(contextId, code);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var candidates = await db.MarkdownFiles.AsNoTracking()
            .Where(m => m.Category != "memory")
            .Select(m => new Candidate(m.Id, m.RelativePath, m.Tier, m.Scope, m.Triggers, m.RelatedIds))
            .ToListAsync(ct);
        var byId = candidates.ToDictionary(c => c.Id);
        var text = triggerText ?? "";

        // 0 — user-pinned docs (override tier — always included, score 999)
        if (pinnedDocIds is { Count: > 0 })
            foreach (var c in candidates.Where(c => pinnedDocIds.Contains(c.Id)))
                stack.Push(contextId, MakeEntry(c, "pinned", 999));

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

        // 5 — relational cascade: for every doc already resident, load its declared `related:`
        //     neighbors that are not yet resident (one level — no recursive fan-out).
        //     Cascaded docs are pushed as topic tier with reason "related:<parent-path>".
        {
            var resident = stack.GetActive(contextId).Select(e => e.DocId).ToHashSet();
            foreach (var entry in stack.GetActive(contextId))
            {
                if (string.IsNullOrEmpty(entry.RelatedIds)) continue;
                foreach (var part in entry.RelatedIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!Guid.TryParse(part, out var relId) || resident.Contains(relId)) continue;
                    if (!byId.TryGetValue(relId, out var c)) continue;
                    stack.Push(contextId, MakeEntry(c, $"related:{entry.RelativePath}", 40));
                    resident.Add(relId);
                }
            }
        }

        return await BuildBlockAsync(db, contextId, tokenBudget, excludedDocIds, ct);
    }

    /// <summary>
    /// Engine convenience: resolve the node's CODE from its Id and prepare the doc context,
    /// using the node Id as the LRU context key. Used by ProseWriterRouter.
    ///
    /// Dynamic Context Memory clue-gathering (step 0): when <see cref="EntityDocService"/> is wired, analyzes
    /// <paramref name="triggerText"/> for entity references and materializes per-entity
    /// <c>.md</c> rows in <c>MarkdownFiles</c> for any not yet present. This runs BEFORE
    /// the candidate query in <see cref="PrepareContextAsync"/> so freshly-created entity
    /// docs participate in the keyword-trigger and relational-cascade passes.
    ///
    /// Loads active user context overrides (pin/exclude) from <see cref="UserContextService"/>
    /// when the service is wired, and applies them before building the block.
    /// </summary>
    public async Task<DocContextResult> PrepareForNodeAsync(
        Guid nodeId, string? triggerText, int tokenBudget = 2000,
        bool inferEntities = true, Guid? povEntityId = null, CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return new DocContextResult("", Array.Empty<LoadedDoc>(), 0);

        // Dynamic Context Memory step 0 — clue-gathering inference: materialize entity docs from beat-goal text
        // BEFORE the candidate query so they are visible in the working set this beat.
        if (inferEntities && entityDocs != null && !string.IsNullOrWhiteSpace(triggerText))
            await entityDocs.InferFromTextAsync(triggerText, ct);

        // POV register priority (SS-A46 layer 4 + GLMZ §0): when the caller knows this beat's
        // narrator (from the bible POV map), materialize that character's register doc and mark it
        // for pinning so it DOMINATES over other present characters' registers — the beat is voiced
        // in the narrator's register, not a blend. POV can change beat to beat in a multi-POV book.
        string code;
        Guid? povDocId = null;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            code = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == nodeId)
                .Select(s => s.NodeCode)
                .FirstOrDefaultAsync(ct) ?? "";

            if (povEntityId is { } pov && entityDocs != null)
            {
                await entityDocs.EnsureEntityDocAsync(pov, ct);
                var slug = await db.Entities.AsNoTracking()
                    .Where(e => e.Id == pov).Select(e => e.Slug).FirstOrDefaultAsync(ct);
                if (!string.IsNullOrEmpty(slug))
                {
                    var relPath = $"docs/entities/{slug}.md";
                    povDocId = await db.MarkdownFiles.AsNoTracking()
                        .Where(m => m.RelativePath == relPath && m.FileRoot == "project")
                        .Select(m => (Guid?)m.Id).FirstOrDefaultAsync(ct);
                }
            }
        }

        var pinnedSet = new HashSet<Guid>();
        IReadOnlySet<Guid>? excluded = null;
        if (userContext != null)
        {
            var overrides = await userContext.GetActiveAsync(nodeId, ct: ct);
            if (overrides.Count > 0)
            {
                foreach (var o in overrides.Where(o => o.Action == "pin")) pinnedSet.Add(o.MarkdownFileId);
                excluded = overrides.Where(o => o.Action == "exclude").Select(o => o.MarkdownFileId).ToHashSet();
            }
        }
        if (povDocId is { } pd) pinnedSet.Add(pd);

        return await PrepareContextAsync(nodeId, code, triggerText, tokenBudget,
            pinnedDocIds: pinnedSet.Count > 0 ? pinnedSet : null, excludedDocIds: excluded, ct: ct);
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
        StreetSamuraiDbContext db, Guid contextId, int tokenBudget,
        IReadOnlySet<Guid>? excludedDocIds, CancellationToken ct)
    {
        var all    = stack.GetActive(contextId);
        var active = excludedDocIds is { Count: > 0 }
            ? all.Where(e => !excludedDocIds.Contains(e.DocId)).ToList()
            : all;

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
            var perDocCap = e.Tier switch { "topic" => 800, "node" => 16_000, _ => 1500 };
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
        new(c.Id, c.RelativePath, c.Tier, c.Scope, c.Triggers, reason, score, 0, 0, c.RelatedIds);

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
        return after < text.Length ? text[after..].TrimStart('\n').Trim() : "";
    }
}
