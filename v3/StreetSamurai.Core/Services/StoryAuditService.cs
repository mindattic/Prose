using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// StoryAuditService
//
// Audits a strand against the 7 Gateway Commandments (standalone / first-in-
// series) or the 7 Sequel Commandments (when PreviousStrandId is set).
//
// Which set applies is determined automatically from Strand.PreviousStrandId:
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

public class StoryAuditService(
    ILlmService llm,
    PlantPayoffService plantPayoffs,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
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
         "Gloss first mentions in-voice, never dump",
         "Proprietary terms (CorpoNations, neuretics, the named geographies) get a light, in-register touch " +
         "that orients without lecturing. Enough for a stranger to keep their footing; never an info-dump. " +
         "Weave it into prose the POV would naturally produce."),

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
         "Refresh proprietary terms on a lighter touch than the gateway",
         "Returning readers half-remember the vocabulary. A glancing re-gloss — a term used in a context that re-teaches " +
         "its meaning — beats both a full re-explanation and assuming total recall. Trust more than you would with a stranger; " +
         "assume less than you would with yourself."),

        ("pay_previous_due",
         "Pay the previous story its due, then move",
         "Acknowledge the cost or victory the reader is carrying from last time — one beat that confirms it mattered and stuck " +
         "— then turn forward. Readers came back partly to see that the previous ending had weight. Honor it briefly; don't relitigate it."),

        ("open_door_left_ajar",
         "Open the door the last ending left ajar",
         "A sequel inherits a pull — the 'what happens in this city' the previous story planted. Name that thread early so the " +
         "returning reader feels the continuity, then complicate it. The reward for coming back is seeing the question they left " +
         "with get bigger, not just answered."),

        ("reward_long_memory",
         "Reward the long memory without taxing the short one",
         "The deepest payoffs can reach back across stories — a callback that lands hard for the reader who remembers, invisible " +
         "to the one who doesn't. But the current story must stand on its own; never make this installment's comprehension depend " +
         "on remembering the last. Continuity is a gift to the faithful, not a toll on the rest."),

        ("escalate_not_repeat",
         "Escalate, don't repeat",
         "A sequel earns its existence by going somewhere the first couldn't. Hold the quality bar and raise the stakes, the " +
         "intimacy, or the strangeness. If it only re-runs what worked before, it's a reprint with new names — give the returning " +
         "reader the thing they didn't know they were owed."),
    ];

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<StoryAuditReport> AuditAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strand = await db.Strands
            .AsNoTracking()
            .Include(s => s.StrandBeats)
            .ThenInclude(sb => sb.Beat)
            .FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var isSequel = strand.PreviousStrandId.HasValue;
        var commandments = (isSequel ? SequelCommandments : GatewayCommandments)
            .Concat(!isSequel && strand.UniverseId == GlmzUniverseId ? GlmzGatewayCommandments : [])
            .ToArray();
        var mode = isSequel ? "sequel" : "gateway";

        // For book strands, audit the LIVE chapter prose (child chapters ordered by
        // SortKey), not the book strand's own beats — those may hold a legacy outline
        // or condensed draft that no longer matches the published manuscript.
        var childChapters = await db.Strands.AsNoTracking()
            .Where(s => s.ParentStrandId == strand.Id && s.Kind == "chapter" && !s.IsDraft)
            .Include(s => s.StrandBeats).ThenInclude(sb => sb.Beat)
            .OrderBy(s => s.SortKey)
            .ToListAsync(ct);

        var prose = childChapters.Count > 0
            ? string.Join("\n\n", childChapters
                .SelectMany(ch => ch.StrandBeats
                    .Where(sb => sb.IsEnabled)
                    .OrderBy(sb => sb.SortKey)
                    .Select(sb => sb.Beat.Text))
                .Where(t => !string.IsNullOrWhiteSpace(t)))
            : string.Join("\n\n", strand.StrandBeats
                .Where(sb => sb.IsEnabled)
                .OrderBy(sb => sb.SortKey)
                .Select(sb => sb.Beat.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

        var plants = await plantPayoffs.GetByStrandAsync(strandId, ct);

        // run all commandment checks in parallel
        var tasks = commandments.Select(c => CheckCommandmentAsync(c.Key, c.Title, c.Body, prose, plants, isSequel, ct));
        var checks = await Task.WhenAll(tasks);

        string? previousTitle = null;
        if (isSequel && strand.PreviousStrandId.HasValue)
        {
            var prev = await db.Strands.AsNoTracking()
                .Where(s => s.Id == strand.PreviousStrandId.Value)
                .Select(s => new { s.Title, s.Slug })
                .FirstOrDefaultAsync(ct);
            previousTitle = prev != null ? $"{prev.Title} ({prev.Slug})" : strand.PreviousStrandId.ToString();
        }

        return new StoryAuditReport(
            StrandSlug:      strand.Slug,
            StrandTitle:     strand.Title,
            Mode:            mode,
            PreviousStrand:  previousTitle,
            Checks:          checks.ToList(),
            GatewayReady:    checks.All(c => c.Status != "fail"),
            BlockingCount:   checks.Count(c => c.Status == "fail"),
            AdvisoryCount:   checks.Count(c => c.Status == "warn"),
            PlantCount:      plants.Count,
            OrphanedPlants:  plants.Count(p => p.PlantBeatId != null && p.PayoffBeatId == null));
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    // Keep both the opening AND the ending when a manuscript is too long for one
    // prompt — commandments split between "open with the world" (head) and "land
    // one beat past the handoff" (tail), so head-only truncation false-fails the latter.
    static string ClampProse(string p) =>
        p.Length <= 100000
            ? p
            : p[..50000] + "\n\n[... middle of the manuscript elided for length ...]\n\n" + p[^50000..];

    async Task<StoryAuditCheck> CheckCommandmentAsync(
        string key,
        string title,
        string body,
        string prose,
        List<Data.Entities.PlantPayoff> plants,
        bool isSequel,
        CancellationToken ct)
    {
        var rereadKey = isSequel ? "reward_long_memory" : "reread_reward";
        var plantContext = key == rereadKey && plants.Count > 0
            ? "\n\nRegistered plant/payoff pairs for this strand:\n" +
              string.Join("\n", plants.Select(p =>
                  $"  [{p.Category}] PLANT: {p.PlantDescription} | PAYOFF: {p.PayoffDescription} | transparent: {p.IsTransparent}"))
            : "";

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
            RULE: {{body}}{{plantContext}}

            STRAND PROSE:
            {{ClampProse(prose)}}

            Evaluate: does this strand satisfy the commandment?
            - "pass" = clearly satisfied
            - "warn" = partially present but could be stronger
            - "fail" = absent, violated, or actively working against the commandment
            Be specific — cite actual prose or structural observations. Not generalizations.
            """;

        try
        {
            var raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: 400, ct: ct);
            var parsed = ParseJson<AuditCheckRaw>(raw);
            return new StoryAuditCheck(
                Key:        key,
                Title:      title,
                Status:     parsed?.Status ?? "warn",
                Evidence:   parsed?.Evidence ?? "(evaluation failed)",
                Fix:        parsed?.Fix);
        }
        catch (Exception ex)
        {
            return new StoryAuditCheck(Key: key, Title: title, Status: "warn",
                Evidence: $"Evaluation failed: {ex.Message}", Fix: null);
        }
    }

    static T? ParseJson<T>(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < start) return default;
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch { return default; }
    }

    private class AuditCheckRaw
    {
        [JsonPropertyName("status")]   public string? Status   { get; set; }
        [JsonPropertyName("evidence")] public string? Evidence { get; set; }
        [JsonPropertyName("fix")]      public string? Fix      { get; set; }
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

public record StoryAuditCheck(
    string  Key,
    string  Title,
    string  Status,     // "pass" | "warn" | "fail"
    string  Evidence,
    string? Fix);

public record StoryAuditReport(
    string             StrandSlug,
    string             StrandTitle,
    string             Mode,            // "gateway" | "sequel"
    string?            PreviousStrand,
    List<StoryAuditCheck> Checks,
    bool               GatewayReady,
    int                BlockingCount,
    int                AdvisoryCount,
    int                PlantCount,
    int                OrphanedPlants);
