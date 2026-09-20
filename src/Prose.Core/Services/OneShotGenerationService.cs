using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Portable-writing-service plan, Phase 2 — a standalone "write me a scene/line of dialog" entry
/// point that does NOT require a pre-existing Book/Chapter/Beat row. Calls
/// <see cref="ProseWriterRouter.WriteAsync"/> directly with <c>beatId = Guid.Empty</c>, either
/// with <see cref="BeatContext.NodeId"/> left at <see cref="Guid.Empty"/> (pure ephemeral — no
/// DB-dependent enrichment, but still pacing, dialogue voice profiles, canon-fact grounding,
/// consequence/gear constraints, ambient sensory grounding, and entity pre-check warnings, all of
/// which key off the caller-supplied context rather than persisted history) or attached to an
/// existing node via <see cref="OneShotGenerationRequest.Node"/> (unlocks doc/entity context,
/// continuity feedback loops, and narrative/chapter summaries — everything gated on NodeId alone
/// — while still writing no Beat row, since persistence in the router is gated on beatId).
///
/// Deliberately does not create a scratch Node+Beat row per call — see the plan doc's "Shape
/// decision" section for why: the DB-dependent enrichment stages need a real, already-populated
/// node's history to add value, so a fresh scratch node would sit at the same enrichment level as
/// the direct ephemeral call while costing extra I/O, temporal-table bloat, and a cleanup story
/// nobody asked for. The only persistence a call causes is the router's own two always-on,
/// fire-and-forget log-row upserts (WorkflowMonitorService, BeatModeDetector) — both already keyed
/// by beatId/NodeId and harmless when those are Guid.Empty.
/// </summary>
public class OneShotGenerationService(
    ProseWriterRouter router,
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universeContext,
    IDatabaseService canonDb)
{
    public record OneShotGenerationRequest(
        string BeatGoal,
        IReadOnlyList<string>? Characters = null,
        string? Location = null,
        string? Subtext = null,
        /// <summary>Optional slug/NodeCode/GUID of an existing Book or Chapter to borrow canon
        /// and continuity from ("attached mode"). Null/empty = pure ephemeral.</summary>
        string? Node = null,
        /// <summary>Optional universe slug override. Null = the ambient current universe.</summary>
        string? Universe = null,
        int BeatIndex = 0,
        int TotalBeats = 0);

    public record OneShotGenerationResult(
        string Text,
        int WordCount,
        string UniverseSlug,
        string? AttachedNodeSlug);

    public async Task<OneShotGenerationResult> GenerateAsync(OneShotGenerationRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.BeatGoal))
            throw new ArgumentException("BeatGoal is required.", nameof(req));

        Guid? universeId = null;
        string universeSlug = universeContext.CurrentSlug;
        if (!string.IsNullOrWhiteSpace(req.Universe))
        {
            var match = universeContext.ListUniverses()
                .FirstOrDefault(u => string.Equals(u.Slug, req.Universe, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                throw new InvalidOperationException($"Unknown universe '{req.Universe}'.");
            universeId = match.Id;
            universeSlug = match.Slug;
        }

        // Per-flow override (not UseUniverse — the Hub is one resident process serving concurrent
        // requests for different universes; this must not bleed into any other in-flight call).
        // Set BEFORE resolving --node/--characters below so their own ambient-scoped lookups
        // (canon docs, entity name matches) see the right universe too.
        if (universeId != null) universeContext.SetFlowUniverse(universeId);
        try
        {
            Guid nodeId = Guid.Empty;
            string? attachedNodeSlug = null;
            if (!string.IsNullOrWhiteSpace(req.Node))
            {
                var resolvedId = await NodeRefResolver.ResolveAsync(dbFactory, req.Node, ct);
                if (resolvedId == null)
                    throw new InvalidOperationException(NodeRefResolver.NotFoundMessage(req.Node));

                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                    .FirstOrDefaultAsync(n => n.Id == resolvedId.Value, ct);
                if (node == null)
                    throw new InvalidOperationException(NodeRefResolver.NotFoundMessage(req.Node));
                nodeId = node.Id;
                attachedNodeSlug = node.Slug;
            }

            var characters = await ResolveCharacterNamesAsync(req.Characters, ct);

            string storyBible;
            try { storyBible = canonDb.GetLiteraryRulesPrompt() ?? ""; }
            catch { storyBible = ""; }

            var context = new BeatContext
            {
                NodeId = nodeId,
                StoryBibleContext = storyBible,
                BeatGoal = req.BeatGoal.Trim(),
                Subtext = req.Subtext ?? "",
                Location = req.Location ?? "",
                CharactersInScene = characters,
            };

            var prose = await router.WriteAsync(
                context,
                beatId: Guid.Empty,
                beatIndex: req.BeatIndex,
                totalBeats: req.TotalBeats,
                universeId: universeId ?? Guid.Empty,
                ct: ct);
            prose = (prose ?? "").Trim();

            var wordCount = prose.Length == 0 ? 0 : prose.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            return new OneShotGenerationResult(prose, wordCount, universeSlug, attachedNodeSlug);
        }
        finally
        {
            if (universeId != null) universeContext.SetFlowUniverse(null);
        }
    }

    /// <summary>Best-effort: resolve each raw token to its canonical character Name (exact Name
    /// or Slug match, case-insensitive) so downstream name-keyed lookups (DialogueService,
    /// ContinuityService, ConsequenceService) hit — same resolution ExpandBeatCli already does for
    /// its single --protagonist argument. A token that doesn't resolve passes through raw rather
    /// than being dropped; the caller may be naming someone not yet seeded.</summary>
    private async Task<IReadOnlyList<string>> ResolveCharacterNamesAsync(IReadOnlyList<string>? raw, CancellationToken ct)
    {
        if (raw == null || raw.Count == 0) return Array.Empty<string>();
        var tokens = raw.Select(r => r.Trim()).Where(r => r.Length > 0).ToList();
        if (tokens.Count == 0) return Array.Empty<string>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var lowered = tokens.Select(t => t.ToLowerInvariant()).ToList();
        var matches = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character" && (lowered.Contains(e.Name.ToLower()) || lowered.Contains(e.Slug.ToLower())))
            .Select(e => new { e.Name, e.Slug })
            .ToListAsync(ct);

        var resolved = new List<string>(tokens.Count);
        foreach (var token in tokens)
        {
            var loweredToken = token.ToLowerInvariant();
            var hit = matches.FirstOrDefault(m => m.Name.ToLowerInvariant() == loweredToken || m.Slug.ToLowerInvariant() == loweredToken);
            resolved.Add(hit?.Name ?? token);
        }
        return resolved;
    }
}
