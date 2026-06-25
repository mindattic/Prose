using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using System.Text;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Self-referential entity context engine. Three passes per beat:
///
/// 1. DETECT — SceneContextAssembler scans beat goal + prior scene text for proper nouns,
///    resolves them to entity GUIDs (name/alias + embedding), loads into EntityContextStack.
///
/// 2. EXPAND — For each detected entity, EmbeddingService finds its k nearest semantic
///    neighbors and pushes them at depth+1 (up to depth 2). The stack follows the graph
///    edges implied by proximity, not just explicit text links.
///
/// 3. RECONCILE (post-generation, non-blocking) — Generated prose is scanned for entity
///    claims. When a claim conflicts with the entity's canon description, Legion decides
///    whether to flag "fix prose" or "update entity". Result is stored as a Finding.
///
/// Entity context format injected into BeatContext.EntityStackContext covers:
///   depth 0  — directly named in the current beat / goal
///   depth 1  — semantic neighbors of depth-0 entities
///   depth 2  — semantic neighbors of depth-1 entities
///
/// LRU eviction: entities not mentioned for EvictAfterBeats beats are dropped from the
/// stack automatically. Depth-0 entries survive the current beat regardless.
/// </summary>
public sealed class EntityContextService(
    SceneContextAssembler assembler,
    EntityContextStack stack,
    EmbeddingService embeddings,
    FindingsService findings,
    LlmVotingService voting,
    ILlmService llm,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILogger<EntityContextService> log)
{
    // ── Pre-generation: build the entity stack context block ─────────────────

    /// <summary>
    /// Load the entity stack for this beat and return the formatted context block
    /// ready for injection into BeatContext.EntityStackContext.
    /// </summary>
    public async Task<string> PrepareContextAsync(
        Guid strandId, Guid beatId, string beatGoal, string sceneSoFar,
        CancellationToken ct = default)
    {
        if (strandId == Guid.Empty) return "";

        stack.BeginBeat(strandId);

        // 1. Assemble from the beat record (name/alias scan + embedding match)
        if (beatId != Guid.Empty)
        {
            var beatCtx = await assembler.AssembleForBeatAsync(beatId, tokenBudget: 1500, ct);
            if (beatCtx != null)
                await PushRosterAsync(strandId, beatCtx.Roster, depth: 0, ct);
        }

        // 2. Also scan beat goal text (handles pre-save writes and goal-only references)
        if (!string.IsNullOrWhiteSpace(beatGoal))
        {
            var goalCtx = await assembler.AssembleAsync(beatGoal, tokenBudget: 600, ct);
            if (goalCtx != null)
                await PushRosterAsync(strandId, goalCtx.Roster, depth: 0, ct);
        }

        // 3. Expand edges: for each depth-0 entity, find its semantic neighbors
        await ExpandEdgesAsync(strandId, maxDepth: 2, ct);

        return BuildContextBlock(strandId);
    }

    // ── Post-generation: reconcile prose against entity canon ─────────────────

    /// <summary>
    /// Non-blocking reconciliation — call fire-and-forget after prose is generated.
    /// Scans prose for entity mentions, updates LRU, detects canon conflicts, and
    /// stores any conflicts as Findings for review.
    /// </summary>
    public async Task ReconcileAsync(
        string prose, Guid strandId, Guid beatId, Guid universeId,
        CancellationToken ct = default)
    {
        if (strandId == Guid.Empty || string.IsNullOrWhiteSpace(prose)) return;

        try
        {
            // 1. Scan prose to update LRU ordering
            var proseCtx = await assembler.AssembleAsync(prose, tokenBudget: 2000, ct);
            if (proseCtx != null && proseCtx.Roster.Count > 0)
            {
                stack.RecordMentions(strandId, proseCtx.Roster.Select(r => r.EntityId));

                // 2. Push any newly-detected entities into the stack
                await PushRosterAsync(strandId, proseCtx.Roster, depth: 0, ct);
            }

            // 3. Detect canon conflicts in the generated prose
            var activeEntities = stack.GetActive(strandId)
                .Where(e => !string.IsNullOrWhiteSpace(e.Description))
                .Take(12) // keep the conflict-check prompt bounded
                .ToList();

            if (activeEntities.Count > 0)
                await CheckProseConflictsAsync(prose, activeEntities, strandId, beatId, universeId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Entity reconciliation failed for beat {BeatId} — non-blocking, continuing", beatId);
        }
    }

    // ── Public utility: inspect the active stack ──────────────────────────────

    /// <summary>Returns the formatted entity context block for a strand without advancing the beat counter.</summary>
    public string GetContextBlock(Guid strandId) => BuildContextBlock(strandId);

    /// <summary>Returns all active stack entries for a strand (for monitoring/debug).</summary>
    public IReadOnlyList<EntityContextStack.StackEntry> GetActiveEntities(Guid strandId) =>
        stack.GetActive(strandId);

    /// <summary>Clears the entity working memory for a strand (call at the start of a new session).</summary>
    public void ClearContext(Guid strandId) => stack.Clear(strandId);

    // ── Edge expansion ────────────────────────────────────────────────────────

    private async Task ExpandEdgesAsync(Guid strandId, int maxDepth, CancellationToken ct)
    {
        for (int depth = 0; depth < maxDepth; depth++)
        {
            var atDepth = stack.GetActive(strandId)
                .Where(e => e.Depth == depth)
                .ToList();

            if (atDepth.Count == 0) break;

            // For each entity at this depth, find semantically similar entities (its "neighbors")
            var expansionTasks = atDepth.Select(async entry =>
            {
                try
                {
                    var neighbors = await embeddings.FindSimilarAsync(entry.Name, k: 4, ct: ct);
                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    foreach (var hit in neighbors.Where(h => h.EntityId != entry.EntityId))
                    {
                        var desc = await LoadDescriptionAsync(hit.EntityId, hit.EntityType, db, ct);
                        stack.Push(strandId, hit.EntityId, hit.EntityName, hit.EntityType, desc, hit.Similarity, depth: depth + 1);
                    }
                }
                catch { /* non-blocking — embedding may be cold or entity type unsupported */ }
            });

            await Task.WhenAll(expansionTasks);
        }
    }

    // ── Roster loading ────────────────────────────────────────────────────────

    private async Task PushRosterAsync(
        Guid strandId,
        IEnumerable<SceneEntityRef> roster,
        int depth,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        foreach (var r in roster)
        {
            var desc = await LoadDescriptionAsync(r.EntityId, r.EntityType, db, ct);
            stack.Push(strandId, r.EntityId, r.Name, r.EntityType, desc, r.Score, depth);
        }
    }

    // ── Entity description loading ────────────────────────────────────────────

    private static async Task<string> LoadDescriptionAsync(
        Guid entityId, string entityType,
        StreetSamuraiDbContext db,
        CancellationToken ct)
    {
        try
        {
            return (entityType.ToLowerInvariant() switch
            {
                "character"      => await db.Characters.AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "place"          => await db.Places.AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "faction"        => await db.Factions.AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "corponation"    => await db.Set<Corponation>().AsNoTracking().Where(x => x.Id == entityId).Select(x => x.FullText).FirstOrDefaultAsync(ct),
                "technology"     => await db.Set<Technology>().AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "weapon"         => await db.Set<Weapon>().AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "cyberware"      => await db.Set<Cyberware>().AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "pharmaceutical" => await db.Set<Pharmaceutical>().AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "material"       => await db.Set<Material>().AsNoTracking().Where(x => x.Id == entityId).Select(x => x.Description).FirstOrDefaultAsync(ct),
                "document"       => await db.Documents.AsNoTracking().Where(x => x.Id == entityId).Select(x => string.IsNullOrEmpty(x.Body) ? x.Title : x.Body).FirstOrDefaultAsync(ct),
                _                => null,
            }) ?? "";
        }
        catch
        {
            return "";
        }
    }

    // ── Context block formatting ──────────────────────────────────────────────

    // Cap how many entities get injected into the prompt. GetActive is recency/depth-ordered, so
    // the top slice is the most relevant; without this the depth-0 set grows unbounded over a long
    // strand (observed ~160/beat) and floods the prompt with noise.
    public const int MaxInjectedEntities = 24;

    private string BuildContextBlock(Guid strandId)
    {
        var entries = stack.GetActive(strandId).Take(MaxInjectedEntities).ToList();
        if (entries.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("ENTITY CONTEXT STACK — active entities drawn into working memory:");
        sb.AppendLine();

        foreach (var (depthLevel, label) in new (int, string)[]
        {
            (0, "DIRECT (named in this beat)"),
            (1, "EDGE — semantic neighbors of direct entities"),
            (2, "EDGE 2 — neighbors of neighbors"),
        })
        {
            var group = entries.Where(e => e.Depth == depthLevel).ToList();
            if (group.Count == 0) continue;

            sb.AppendLine($"  [{label}]");
            foreach (var e in group)
            {
                sb.Append($"  {e.EntityType.ToUpperInvariant()} \"{e.Name}\"");
                if (!string.IsNullOrWhiteSpace(e.Description))
                {
                    var shortDesc = e.Description.Length > 280
                        ? e.Description[..280] + "…"
                        : e.Description;
                    sb.AppendLine($": {shortDesc}");
                }
                else
                {
                    sb.AppendLine();
                }
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    // ── Canon conflict detection + Legion resolution ──────────────────────────

    private async Task CheckProseConflictsAsync(
        string prose,
        IReadOnlyList<EntityContextStack.StackEntry> activeEntities,
        Guid strandId, Guid beatId, Guid universeId,
        CancellationToken ct)
    {
        var entityNames = string.Join(", ", activeEntities.Select(e => $"\"{e.Name}\""));
        var proseExcerpt = prose.Length > 2500 ? prose[..2500] : prose;

        // One cheap LLM call: extract what the prose claims about each active entity
        var extractSystem = "You extract factual statements from prose about specific named entities. Be brief and precise. Output ONLY lines in format: ENTITY_NAME | factual claim. Skip entities not mentioned.";
        var extractUser = $"Entities to watch: {entityNames}\n\nPROSE:\n{proseExcerpt}";

        string claimsText;
        try
        {
            claimsText = await llm.GenerateAsync(extractSystem, extractUser, temperature: 0.1, maxTokens: 400, ct: ct);
        }
        catch { return; }

        if (string.IsNullOrWhiteSpace(claimsText)) return;

        foreach (var line in claimsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|', 2);
            if (parts.Length != 2) continue;

            var entityName = parts[0].Trim().Trim('"');
            var proseClaim = parts[1].Trim();
            if (string.IsNullOrWhiteSpace(proseClaim)) continue;

            var entity = activeEntities.FirstOrDefault(e =>
                string.Equals(e.Name, entityName, StringComparison.OrdinalIgnoreCase));
            if (entity == null) continue;

            // Cheap YES/NO conflict check
            var checkSystem = "You detect factual conflicts between a prose claim and a canon entity description. Answer YES or NO only.";
            var checkUser = $"Entity: {entity.Name} ({entity.EntityType})\nCanon: {entity.Description[..Math.Min(entity.Description.Length, 350)]}\n\nProse claims: \"{proseClaim}\"\n\nIs there a factual conflict?";

            string verdict;
            try { verdict = await llm.GenerateAsync(checkSystem, checkUser, temperature: 0.0, maxTokens: 5, ct: ct); }
            catch { continue; }

            if (!verdict.Contains("YES", StringComparison.OrdinalIgnoreCase)) continue;

            log.LogInformation("Entity conflict: {Name} — prose says: {Claim}", entity.Name, proseClaim);

            // Legion decides: fix prose or update entity
            string legionChoice;
            string legionReasoning;
            try
            {
                var decision = await voting.DecideAsync(
                    question: $"CONFLICT for '{entity.Name}' ({entity.EntityType}). Which is authoritative?",
                    options: ["FixProse", "UpdateEntity", "Ignore"],
                    context: $"Prose says: \"{proseClaim}\"\nCanon says: \"{entity.Description[..Math.Min(entity.Description.Length, 200)]}\"",
                    quorum: Quorum.Plurality,
                    maxTokens: 300);

                legionChoice = decision.Choice;
                legionReasoning = decision.Reasoning ?? "";
            }
            catch
            {
                legionChoice = "Ignore";
                legionReasoning = "Legion unavailable";
            }

            if (legionChoice == "Ignore") continue;

            findings.Upsert(
                filePath: $"beat:{beatId:N}",
                chapterId: strandId == Guid.Empty ? null : strandId.ToString("N"),
                category: FindingCategory.Other,
                severity: FindingSeverity.Medium,
                summary: $"ENTITY-CONFLICT [{entity.EntityType}]: \"{entity.Name}\" — prose says \"{proseClaim}\"",
                snippet: $"Canon: {entity.Description[..Math.Min(entity.Description.Length, 150)]}",
                suggestedFix: $"Legion: {legionChoice}. {legionReasoning}");
        }
    }
}
