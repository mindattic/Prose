using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// BookAuditService
//
// Audits a node against the 7 Gateway Commandments (standalone / first-in-
// series) or the 7 Sequel Commandments (when PreviousNodeId is set).
//
// Which set applies is determined automatically from Node.PreviousNodeId:
//   null  → gateway commandments (seduce the cold reader)
//   set   → sequel commandments  (honor the returning reader)
//
// Each commandment is an independent LLM check run in parallel. The audit
// returns pass / warn / fail per commandment, evidence, and a fix suggestion.
//
// Commandment 6 (gateway: "reward re-reading without requiring it") and
// the analogous sequel commandment ("reward the long memory without taxing
// the short one") are enriched with actual PlantPayoff registry data.
// ─────────────────────────────────────────────────────────────────────────────

public class BookAuditService(
    AuditRunner auditRunner,
    PlantPayoffService plantPayoffs,
    GlossaryService glossary,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    // GLMZ universe ID — used to append universe-specific commandments
    static readonly Guid GlmzUniverseId = new("0197E9C9-0001-7000-8000-000000000001");

    // ── GLMZ-specific Gateway Commandment ─────────────────────────────────────
    // Added separately so Fantasy/other universes are unaffected.

    static readonly (string Key, string Title, string Body)[] GlmzGatewayCommandments =
    [
        ("glmz_five_pillars",
         "Orient through GLMZ's five pillars",
         "A reader finishing this story cold must sense all five forces that define GLMZ: " +
         "(1) AIs as agents with faction and agenda — not tools, not metaphors, entities with stakes; " +
         "(2) nanites as ambient infrastructure woven into daily life, not magic dust — bodies repaired, " +
         "matter shaped, the city itself mediated by them; " +
         "(3) neuretics as the cognitive augmentation that reshapes who can think fast enough to compete — " +
         "the divide between those who have them and those who don't is social and economic, not just technical; " +
         "(4) Schism as the physics-breaking rift that destabilized the world — a wound in reality, not a plot device; " +
         "(5) CorpoNations as the actual governing power, with gray zones as the spaces their law cannot or will not reach. " +
         "Touch all five — even obliquely — through the story's natural texture. " +
         "The world should feel systemic, not like a collection of gimmicks."),
    ];

    // ── Gateway Commandments ──────────────────────────────────────────────────

    static readonly (string Key, string Title, string Body)[] GatewayCommandments =
    [
        ("seduce",
         "Seduce, don't just observe",
         "Find the textures that make GLMZ irresistible — the strange-beautiful, the lived-in wrong — " +
         "and let them pull before the horror lands. Accurate observation is the floor; allure is the goal."),

        ("open_grounded",
         "Open with the world doing its work",
         "Earn the seduction early, ideally before the plot demands attention. A grounded entry beat — " +
         "a commute, a transaction, a moment of the city seen by someone who stopped being surprised by it — " +
         "lets the reader fall in before they're asked to follow anything."),

        ("gloss_in_voice",
         "Never interrupt the voice to define a term",
         "Proprietary terms (CorpoNations, neuretics, the named geographies) are used the way a native speaker of " +
         "this world would use them — with confidence, no stopping to orient the reader. Definition is the back-matter " +
         "Glossary's job, not the prose's. Fail this only if the prose itself breaks voice to explain a term " +
         "(an aside, a lecture, a dictionary-style clause) — using an unexplained term in-voice is correct, not a defect."),

        ("acronym_after_term",
         "Acronyms live in the Glossary, not on the page",
         "SS-LAW-20 (amended): a term or acronym used in prose with no in-voice expansion is correct, as long as " +
         "the book's Glossary (provided below, if any entries are in use) already defines it. Only fail if a term " +
         "or acronym appears in the prose that has NO glossary entry provided — that's a missing back-matter entry, " +
         "not a prose defect, so the fix should say 'add a glossary entry for X', never 'expand X in-voice'."),

        ("human_crack",
         "Give the POV character one human crack",
         "A gateway protagonist needs a single moment where the reader feels them, not just their function — " +
         "a flash that they were once someone these things touched. Stay inside the character's register. " +
         "They can re-armor immediately. One beat is enough."),

        ("land_past_handoff",
         "Land one beat past the handoff",
         "An ending that pays off for the invested reader can read as a non-ending to a cold one. Don't resolve — " +
         "add weight. Give one quiet, final gesture after the plot's business concludes: not editorial, not hopeful, " +
         "not defeated. Leave the reader holding the cost and pulling toward 'what happens in this city.'"),

        ("reread_reward",
         "Reward re-reading without requiring it",
         "The best stories work cleanly on the first pass and open up on the second. Plant detail that pays off " +
         "only once a reader knows where the story is going — a gloss that reads differently in hindsight, a gesture " +
         "that means more after the ending — but never make first-pass comprehension depend on catching it. " +
         "The cold reader gets a complete story; the returning reader gets a deeper one."),

        ("elevate_not_reconstruct",
         "Elevate, don't reconstruct",
         "A gateway pass is tuning for a new audience, not a rewrite. Hold the story's quality bar; make targeted " +
         "additions, not structural surgery. If a story already works for the invested reader, the gateway version " +
         "adds doors — it doesn't rebuild the house."),
    ];

    // ── Sequel Commandments ───────────────────────────────────────────────────

    static readonly (string Key, string Title, string Body)[] SequelCommandments =
    [
        ("reenter_through_change",
         "Re-enter through change, not summary",
         "The opening should remind readers where things stand by showing what's different now, not by recapping " +
         "what happened. A character in a new situation, a relationship that's shifted, a consequence now visible — " +
         "let the present state carry the memory. The reader reconstructs the past from the changed present."),

        ("reintroduce_like_old_friend",
         "Reintroduce names like an old friend would",
         "When a returning character first appears, give one in-voice tag that reactivates the memory — what they do, " +
         "how they stand, the one thing that defines them — not a dossier. Enough to make a reader who's been away go " +
         "'right, them,' and light enough that a reader who never left doesn't feel talked down to."),

        ("lighter_regloss",
         "Trust the Glossary, not a re-explanation",
         "Returning readers half-remember the vocabulary, but the fix is never a re-gloss inside the prose — that's " +
         "the back-matter Glossary's job (SS-LAW-20, amended). Use proprietary terms with full confidence, no refresher " +
         "clause. Fail this only if the prose stops to re-explain a term the reader has seen before."),

        ("acronym_after_term",
         "Acronyms live in the Glossary, not on the page",
         "SS-LAW-20 (amended): a term or acronym used in prose with no in-voice expansion is correct, as long as " +
         "the book's Glossary (provided below, if any entries are in use) already defines it. Only fail if a term " +
         "or acronym appears in the prose that has NO glossary entry provided — that's a missing back-matter entry, " +
         "not a prose defect, so the fix should say 'add a glossary entry for X', never 'expand X in-voice'."),

        ("pay_previous_due",
         "Pay the previous book its due, then move",
         "Acknowledge the cost or victory the reader is carrying from last time — one beat that confirms it mattered and stuck " +
         "— then turn forward. Readers came back partly to see that the previous ending had weight. Honor it briefly; don't relitigate it."),

        ("open_door_left_ajar",
         "Open the door the last ending left ajar",
         "A sequel inherits a pull — the 'what happens in this city' the previous book planted. Name that thread early so the " +
         "returning reader feels the continuity, then complicate it. The reward for coming back is seeing the question they left " +
         "with get bigger, not just answered."),

        ("reward_long_memory",
         "Reward the long memory without taxing the short one",
         "The deepest payoffs can reach back across stories — a callback that lands hard for the reader who remembers, invisible " +
         "to the one who doesn't. But the current book must stand on its own; never make this installment's comprehension depend " +
         "on remembering the last. Continuity is a gift to the faithful, not a toll on the rest."),

        ("escalate_not_repeat",
         "Escalate, don't repeat",
         "A sequel earns its existence by going somewhere the first couldn't. Hold the quality bar and raise the stakes, the " +
         "intimacy, or the strangeness. If it only re-runs what worked before, it's a reprint with new names — give the returning " +
         "reader the thing they didn't know they were owed."),
    ];

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<BookAuditReport> AuditAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes
            .AsNoTracking()
            .Include(s => s.BeatNodes)
            .ThenInclude(sb => sb.Beat)
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var isSequel = node.PreviousNodeId.HasValue;
        var commandments = (isSequel ? SequelCommandments : GatewayCommandments)
            .Concat(!isSequel && node.UniverseId == GlmzUniverseId ? GlmzGatewayCommandments : [])
            .ToArray();
        var mode = isSequel ? "sequel" : "gateway";

        // For book nodes, audit the LIVE chapter prose (leaf chapters in reading order),
        // not the book node's own beats — those may hold a legacy outline or condensed
        // draft that no longer matches the published manuscript. Recurses past any nested
        // Collection (2026-08-09 fix) — the prior Include-based direct-children query missed
        // a split chapter's grandchildren entirely (their BeatNodes navigation is empty; the
        // beats moved to the new sub-chapters during the split).
        var leafChapterIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var isFlatNode = leafChapterIds.Count == 1 && leafChapterIds[0] == node.Id;
        var prose = isFlatNode
            ? string.Join("\n\n", node.BeatNodes
                .Where(sb => true)
                .OrderBy(sb => sb.SortKey)
                .Select(sb => sb.Beat!.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)))
            : string.Join("\n\n", (await db.BeatNodes.AsNoTracking()
                .Where(sb => leafChapterIds.Contains(sb.NodeId) && true)
                .Include(sb => sb.Beat)
                .ToListAsync(ct))
                .OrderBy(sb => leafChapterIds.IndexOf(sb.NodeId)).ThenBy(sb => sb.SortKey)
                .Select(sb => sb.Beat!.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

        var plants = await plantPayoffs.GetByNodeAsync(nodeId, ct);
        var glossaryTerms = await glossary.GetUsedTermsAsync(nodeId, ct);
        var rereadKey = isSequel ? "reward_long_memory" : "reread_reward";

        var rules = commandments
            .Select(c => (IAuditRule)new CommandmentRule(c.Key, c.Title, c.Body, isSequel, rereadKey))
            .ToList();
        var ctx = new AuditContext(nodeId, node.UniverseId, prose, [],
            new Dictionary<string, object?> { ["plants"] = plants, ["glossary"] = glossaryTerms });

        var verdicts = await auditRunner.RunAsync(
            "BOOKAUDIT", $"node:{node.Slug}", FindingCategory.BookAudit, rules, ctx, ct: ct);
        var checks = commandments.Select(c =>
        {
            var v = verdicts.First(v => v.RuleKey == c.Key);
            // ERROR (the commandment's LLM call threw — timeout, malformed response, provider
            // outage) must never read as "pass" or a mere advisory "warn": the commandment was
            // never actually evaluated, so GatewayReady below must not claim readiness on its
            // account. Distinct from "fail" (a real BLOCKER verdict) only for display clarity.
            var status = v.Severity switch { "PASS" => "pass", "BLOCKER" => "fail", "ERROR" => "error", _ => "warn" };
            return new BookAuditCheck(c.Key, c.Title, status, v.Evidence, v.Fix);
        }).ToArray();

        string? previousTitle = null;
        if (isSequel && node.PreviousNodeId.HasValue)
        {
            var prev = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == node.PreviousNodeId.Value)
                .Select(s => new { s.Title, s.Slug })
                .FirstOrDefaultAsync(ct);
            previousTitle = prev != null ? $"{prev.Title} ({prev.Slug})" : node.PreviousNodeId.ToString();
        }

        return new BookAuditReport(
            NodeSlug:      node.Slug,
            NodeTitle:     node.Title,
            Mode:            mode,
            PreviousNode:  previousTitle,
            Checks:          checks.ToList(),
            GatewayReady:    checks.All(c => c.Status is not ("fail" or "error")),
            BlockingCount:   checks.Count(c => c.Status is "fail" or "error"),
            AdvisoryCount:   checks.Count(c => c.Status == "warn"),
            PlantCount:      plants.Count,
            OrphanedPlants:  plants.Count(p => p.PlantBeatId != null && p.PayoffBeatId == null));
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>One commandment, adapted to the shared <see cref="ILlmAuditRule"/> dispatch —
    /// AuditRunner owns the actual LLM call and the {status,evidence,fix} JSON parse now;
    /// this only supplies the prompt. Commandment 6 (gateway "reward re-reading" / sequel
    /// "reward the long memory") gets the node's plant/payoff registry appended, same as
    /// before the refactor — reads it from <see cref="AuditContext.Extra"/>["plants"].</summary>
    // Keys whose doctrine defers to the back-matter Glossary (SS-LAW-20, amended
    // 2026-08-08) instead of judging in-voice definition — these get the book's
    // live used-glossary-terms list appended so "unglossed in prose" isn't
    // mistaken for a defect when the term is already covered in back matter.
    static readonly HashSet<string> GlossaryAwareKeys = ["gloss_in_voice", "acronym_after_term", "lighter_regloss"];

    sealed class CommandmentRule(string key, string title, string body, bool isSequel, string rereadKey)
        : ILlmAuditRule
    {
        public string Key => key;
        public string Title => title;

        public (string System, string User) BuildPrompt(AuditContext ctx)
        {
            var plants = ctx.Extra.TryGetValue("plants", out var p) ? (List<PlantPayoff>)p! : [];
            var plantContext = key == rereadKey && plants.Count > 0
                ? "\n\nRegistered plant/payoff pairs for this node:\n" +
                  string.Join("\n", plants.Select(pl =>
                      $"  [{pl.Category}] PLANT: {pl.PlantDescription} | PAYOFF: {pl.PayoffDescription} | transparent: {pl.IsTransparent}"))
                : "";

            var glossaryContext = "";
            if (GlossaryAwareKeys.Contains(key) &&
                ctx.Extra.TryGetValue("glossary", out var g) && g is IReadOnlyList<GlossaryTerm> { Count: > 0 } terms)
            {
                glossaryContext = "\n\nThis book's back-matter Glossary already defines these terms (a term on " +
                    "this list needs NO in-voice expansion anywhere in the prose — using it bare is correct):\n" +
                    string.Join("\n", terms.Select(t =>
                        $"  {t.Term}{(t.FullForm is { Length: > 0 } ff ? $" ({ff})" : "")} — {t.Definition}"));
            }

            var mode = isSequel ? "sequel" : "standalone/gateway";
            var system = $$"""
                You are auditing a {{mode}} story against one specific commandment.
                A gateway story must work for first-time readers unfamiliar with the universe;
                a sequel must honor returning readers while staying accessible to newcomers.

                Respond as JSON only — no prose wrapper.
                {
                  "status":   "pass" | "warn" | "fail",
                  "evidence": "1-2 sentence specific observation or quote from the prose",
                  "fix":      "one concrete sentence or null if passing"
                }
                """;

            var user = $$"""
                COMMANDMENT: {{title}}
                RULE: {{body}}{{plantContext}}{{glossaryContext}}

                NODE PROSE:
                {{AuditProseUtils.ClampProse(ctx.Prose)}}

                Evaluate: does this node satisfy the commandment?
                - "pass" = clearly satisfied
                - "warn" = partially present but could be stronger
                - "fail" = absent, violated, or actively working against the commandment
                Be specific — cite actual prose or structural observations. Not generalizations.
                """;
            return (system, user);
        }
    }

    // ── Commandment access (for beat generator context injection) ─────────────

    public string BuildCommandmentContext(bool isSequel, Guid? universeId = null)
    {
        var commandments = (isSequel ? SequelCommandments : GatewayCommandments)
            .Concat(!isSequel && universeId == GlmzUniverseId ? GlmzGatewayCommandments : [])
            .ToArray();
        var label = isSequel ? "SEQUEL COMMANDMENTS" : "GATEWAY COMMANDMENTS";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"[{label} — this story must satisfy all of these]");
        foreach (var (key, title, body) in commandments)
            sb.AppendLine($"  • {title}: {body}");
        return sb.ToString();
    }
}

// ── Result models ─────────────────────────────────────────────────────────────

public record BookAuditCheck(
    string  Key,
    string  Title,
    string  Status,     // "pass" | "warn" | "fail"
    string  Evidence,
    string? Fix);

public record BookAuditReport(
    string             NodeSlug,
    string             NodeTitle,
    string             Mode,            // "gateway" | "sequel"
    string?            PreviousNode,
    List<BookAuditCheck> Checks,
    bool               GatewayReady,
    int                BlockingCount,
    int                AdvisoryCount,
    int                PlantCount,
    int                OrphanedPlants);
