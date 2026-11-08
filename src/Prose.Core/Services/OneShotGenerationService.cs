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
///
/// <para><b>Canon grounding (PRS-T926).</b> The router's X-Ray, entity working memory and doc stack key off a beat
/// id or a node id, so a call with neither (every game universe, and any draft text) used to be written from the
/// goal alone and invented its names. Every call is now grounded by <see cref="SceneCanonGroundingService"/>
/// whenever the router would not assemble an X-Ray itself (always: this service never passes a beat id): the
/// scene's characters, location and <see cref="OneShotGenerationRequest.About"/> entities are resolved, the X-Ray is
/// assembled with them pinned, and a canon block carries their records, relations, surroundings and approved
/// descriptions under a use-only-canon-names rule. The draft is then name-checked: names the canon lacks are
/// returned in <see cref="OneShotGenerationResult.UnknownNames"/>, and (unless
/// <see cref="OneShotGenerationRequest.RetryOnUnknown"/> is false) the scene is written once more with them as a
/// correction, keeping the draft with fewer. The call's own LLM spend is reported (a cost scope, so concurrent
/// Hub work is not charged to it).</para>
/// </summary>
public class OneShotGenerationService(
    ProseWriterRouter router,
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universeContext,
    IDatabaseService canonDb,
    SceneCanonGroundingService? grounding = null,
    TokenLedger? ledger = null)
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
        int TotalBeats = 0,
        /// <summary>Entity ids/names the scene is about (a game text's <c>about</c>), grounded like the characters
        /// and location.</summary>
        IReadOnlyList<string>? About = null,
        /// <summary>False: report invented names without the one corrective rewrite.</summary>
        bool RetryOnUnknown = true);

    public record OneShotGenerationResult(
        string Text,
        int WordCount,
        string UniverseSlug,
        string? AttachedNodeSlug)
    {
        /// <summary>The entities the scene was grounded on (resolved from characters, location, about).</summary>
        public IReadOnlyList<string> GroundedOn { get; init; } = [];
        /// <summary>The requested characters/location/about words that resolved to no entity.</summary>
        public IReadOnlyList<string> Unresolved { get; init; } = [];
        /// <summary>The X-Ray roster the scene was written with.</summary>
        public IReadOnlyList<string> CanonRoster { get; init; } = [];
        /// <summary>Capitalised names in the returned text that the universe's canon does not have.</summary>
        public IReadOnlyList<string> UnknownNames { get; init; } = [];
        /// <summary>The names the first draft invented, when a corrective rewrite ran.</summary>
        public IReadOnlyList<string> FirstDraftUnknownNames { get; init; } = [];
        public bool Retried { get; init; }
        public int LlmCalls { get; init; }
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
        /// <summary>This call's LLM spend in USD (estimated from tokens at the model's rate; 0 for subscription CLIs).</summary>
        public double CostUsd { get; init; }

        /// <summary>The one response shape the HTTP, MCP and CLI (--json) surfaces return.</summary>
        public object ToResponse() => new
        {
            text = Text,
            wordCount = WordCount,
            universe = UniverseSlug,
            attachedNode = AttachedNodeSlug,
            groundedOn = GroundedOn,
            unresolved = Unresolved,
            canonRoster = CanonRoster,
            unknownNames = UnknownNames,
            firstDraftUnknownNames = FirstDraftUnknownNames,
            retried = Retried,
            cost = new { llmCalls = LlmCalls, inputTokens = InputTokens, outputTokens = OutputTokens, usd = CostUsd },
        };
    }

    public async Task<OneShotGenerationResult> GenerateAsync(OneShotGenerationRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.BeatGoal))
            throw new ArgumentException("BeatGoal is required.", nameof(req));

        using var costScope = LlmActionContext.BeginCostScope();
        var result = await GenerateCoreAsync(req, ct);
        if (ledger == null) return result;
        var mine = ledger.GetEntries().Where(e => e.CostScopes.Contains(costScope.Id)).ToList();
        return result with
        {
            LlmCalls = mine.Count,
            InputTokens = mine.Sum(e => e.InputTokens),
            OutputTokens = mine.Sum(e => e.OutputTokens),
            CostUsd = Math.Round(mine.Sum(e => e.TotalCost), 6),
        };
    }

    private async Task<OneShotGenerationResult> GenerateCoreAsync(OneShotGenerationRequest req, CancellationToken ct)
    {
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

                // Attached mode with no explicit universe: write in the NODE's universe. Left to
                // the ambient default, a book from another universe got this universe's canon,
                // characters and grounding (and universeId Guid.Empty below).
                if (universeId == null)
                {
                    var owner = universeContext.ListUniverses().FirstOrDefault(u => u.Id == node.UniverseId);
                    if (owner != null)
                    {
                        universeId = owner.Id;
                        universeSlug = owner.Slug;
                        universeContext.SetFlowUniverse(universeId);
                    }
                }
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

            // PRS-T926: ground the scene in the universe's canon. The router assembles an X-Ray only for a real
            // beat id, and its entity/doc stacks only for a node, so without this a game universe (no book node)
            // and any draft text got no canon at all.
            SceneCanonGroundingService.Grounding? g = null;
            var groundingUniverseId = universeId ?? universeContext.CurrentId;
            if (grounding != null && groundingUniverseId != Guid.Empty)
            {
                g = await grounding.BuildAsync(groundingUniverseId, universeSlug, context.BeatGoal, req.Subtext,
                    characters, req.Location, req.About ?? [], ct);
                context = context with
                {
                    XRayContext = g.XRayBlock,
                    RelationshipContext = g.CanonBlock,
                };
            }

            var prose = await WriteOnceAsync(context, req, universeId, ct);
            IReadOnlyList<string> unknown = g == null ? [] : SceneCanonGroundingService.FindUnknownNames(prose, g);
            IReadOnlyList<string> firstUnknown = [];
            var retried = false;

            if (g != null && unknown.Count > 0 && req.RetryOnUnknown)
            {
                // One corrective rewrite, never a loop: the names it invented become a repair constraint.
                var correction =
                    $"- The previous draft of this scene invented names the {universeSlug} canon does not have: " +
                    string.Join(", ", unknown.Select(n => $"\"{n}\"")) + ". Do not use them. Use only names given in the " +
                    "canon grounding and scene x-ray; describe any other person, ship or place by role or look.";
                var second = await WriteOnceAsync(context with { RepairConstraintContext = correction }, req, universeId, ct);
                var secondUnknown = SceneCanonGroundingService.FindUnknownNames(second, g);
                retried = true;
                firstUnknown = unknown;
                if (second.Length > 0 && secondUnknown.Count <= unknown.Count)
                {
                    prose = second;
                    unknown = secondUnknown;
                }
            }

            var wordCount = prose.Length == 0 ? 0 : prose.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            return new OneShotGenerationResult(prose, wordCount, universeSlug, attachedNodeSlug)
            {
                GroundedOn = g?.Pinned.Select(p => $"{p.Name} ({p.EntityType})").ToList() ?? [],
                Unresolved = g?.Unresolved ?? [],
                CanonRoster = g?.RosterNames ?? [],
                UnknownNames = unknown,
                FirstDraftUnknownNames = firstUnknown,
                Retried = retried,
            };
        }
        finally
        {
            if (universeId != null) universeContext.SetFlowUniverse(null);
        }
    }

    private async Task<string> WriteOnceAsync(BeatContext context, OneShotGenerationRequest req, Guid? universeId, CancellationToken ct)
    {
        var prose = await router.WriteAsync(
            context,
            beatId: Guid.Empty,
            beatIndex: req.BeatIndex,
            totalBeats: req.TotalBeats,
            universeId: universeId ?? Guid.Empty,
            ct: ct);
        return (prose ?? "").Trim();
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
