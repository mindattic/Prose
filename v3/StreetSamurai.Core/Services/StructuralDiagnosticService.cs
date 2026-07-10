using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ── Structural Diagnostic Service ─────────────────────────────────────────────
//
// Pre-flight structural analysis for prose beats/nodes. Runs BEFORE the
// 60-ballot review panel so you catch category-level problems (missing antagonist
// cost, passive protagonist, exposition-only chapters) instead of discovering
// them from a low score after spending 60 LLM votes.
//
// 12 targeted checks run in parallel, each a narrow LLM call. Results are
// typed (Pass / Warn / Fail) with evidence quoted from the text and a concrete
// one-action fix. Blocking failures suppress the review recommendation.
//
// Usage:
//   ss --diagnose-story --slug <slug>
//   MCP: diagnose_node(nodeIdOrSlug)

/// <summary>Structural check result tier.</summary>
public enum StructuralCheckResult { Pass, Warn, Fail }

/// <summary>Single structural check result with quoted evidence and a fix.</summary>
public record StructuralCheck(
    string Name,
    string Description,
    StructuralCheckResult Result,
    string Evidence,
    string Fix,
    bool IsBlocking);

/// <summary>Full structural diagnosis for a node.</summary>
public record StructuralDiagnosisResult(
    Guid NodeId,
    string Slug,
    string Title,
    int PassCount,
    int WarnCount,
    int FailCount,
    bool HasBlockingFailures,
    IReadOnlyList<StructuralCheck> Checks,
    string Recommendation);

/// <summary>
/// Pre-flight structural analysis for prose. Runs 12 targeted LLM checks in
/// parallel and returns typed Pass/Warn/Fail findings with evidence and fixes.
/// Call this before <c>review_node</c> — a structural failure will cap scores
/// regardless of prose quality, and the fix is always bigger than a line edit.
/// </summary>
public class StructuralDiagnosticService
{
    private readonly ILlmService llm;
    private readonly FindingsService findings;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<StructuralDiagnosticService> log;

    public StructuralDiagnosticService(
        ILlmService llm,
        FindingsService findings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<StructuralDiagnosticService> log)
    {
        this.llm       = llm;
        this.findings  = findings;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Public entry points ───────────────────────────────────────────────────

    public async Task<StructuralDiagnosisResult> DiagnoseNodeAsync(
        Guid nodeId, int maxChars = 40000, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var beats = await (
            from sb in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
            where sb.NodeId == nodeId && sb.IsEnabled
            orderby sb.SortKey
            select b.Text
        ).ToListAsync(ct);

        var text = string.Join("\n\n---\n\n", beats.Where(t => !string.IsNullOrWhiteSpace(t)));

        return await DiagnoseTextAsync(nodeId, node.Slug, node.Title, text, maxChars, ct);
    }

    public async Task<StructuralDiagnosisResult> DiagnoseTextAsync(
        Guid nodeId, string slug, string title, string text, int maxChars = 40000, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Empty(nodeId, slug, title);

        // Run all 12 checks in parallel — each is a narrow LLM call
        var checkTasks = new[]
        {
            CheckAntagonistCostAsync(text, ct, maxChars),
            CheckProtagonistBehaviorChangeAsync(text, ct, maxChars),
            CheckStakesEmbodimentAsync(text, ct, maxChars),
            CheckExpositionDensityAsync(text, ct, maxChars),
            CheckCharacterEmbodimentAsync(text, ct, maxChars),
            CheckPacingGearChangeAsync(text, ct, maxChars),
            CheckAffectationLinesAsync(text, ct, maxChars),
            CheckDramaticQuestionAsync(text, ct, maxChars),
            CheckPassiveProtagonistAsync(text, ct, maxChars),
            CheckCharacterFunctionTestAsync(text, ct, maxChars),
            CheckDialogueSubtextAsync(text, ct, maxChars),
            CheckJargonFrontLoadingAsync(text, ct, maxChars),
        };

        var checks = (await Task.WhenAll(checkTasks)).ToList();

        int pass     = checks.Count(c => c.Result == StructuralCheckResult.Pass);
        int warn     = checks.Count(c => c.Result == StructuralCheckResult.Warn);
        int fail     = checks.Count(c => c.Result == StructuralCheckResult.Fail);

        // The blocking checks are CHAPTER-SCOPED ("by the end of the chapter") and
        // each only sees the first `maxChars` of the node (see Truncate). When the
        // node exceeds that window — a whole book or a long multi-chapter node —
        // a "fail" reflects only the opening fragment (e.g. "no behavior change yet"
        // is expected in a book's first 40k chars) and is NOT grounds to block the
        // review. In that case the structural checks are ADVISORY only: surface them
        // as warnings but never block, and don't file false-positive findings. Use
        // per-segment review (review by act/chapter) to gate large nodes properly.
        bool truncated = text.Length > maxChars;
        bool blocking  = !truncated && checks.Any(c => c.IsBlocking && c.Result == StructuralCheckResult.Fail);

        // File blocking failures as findings so they surface at /findings — but only
        // when the checks actually saw the whole node (not a truncated opening).
        foreach (var check in checks.Where(c => !truncated && c.IsBlocking && c.Result == StructuralCheckResult.Fail))
        {
            findings.Upsert(
                filePath: $"node:{slug}",
                chapterId: null,
                category: FindingCategory.Other,
                severity: FindingSeverity.High,
                summary: $"STRUCTURAL-FAILURE [{check.Name}]: {check.Fix}",
                snippet: check.Evidence,
                suggestedFix: check.Fix);
        }

        string recommendation = blocking
            ? "Fix blocking failures before running review panel — structural issues cap scores regardless of prose quality."
            : truncated
                ? "Node exceeds the diagnostic window — structural checks ran on the opening fragment only and are ADVISORY (not blocking). Use per-act/segmented review to gate large nodes."
                : fail + warn > 4
                    ? "Address warnings before committing to 60 ballots — multiple weak signals compound into a low score."
                    : "Ready to review.";

        return new StructuralDiagnosisResult(
            nodeId, slug, title,
            pass, warn, fail,
            blocking,
            checks,
            recommendation);
    }

    // ── Individual checks (all private, all return StructuralCheck) ───────────

    private Task<StructuralCheck> CheckAntagonistCostAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "AntagonistCost",
            description: "Antagonist forces protagonist to NOT do something on-page.",
            isBlocking: true,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Does the antagonist (or opposing force) force the protagonist to refrain from doing something they wanted to do — on the page, in this chapter? Not off-screen, not implied, not "they thought about it" — the protagonist STARTS to act and STOPS because of the antagonist's presence or action.

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote from the text showing antagonist cost, or none>",
  "fix": "<one concrete action to add antagonist cost, or none needed>"
}

pass = clear on-page cost. warn = ambiguous or off-screen. fail = no antagonist cost visible anywhere.
""",
            ct);

    private Task<StructuralCheck> CheckProtagonistBehaviorChangeAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "ProtagonistBehaviorChange",
            description: "Protagonist does something differently by end of chapter.",
            isBlocking: true,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Does the protagonist's observable BEHAVIOR change by the end of the chapter? Not their thoughts, not their mood — their actual actions. Do they make a different kind of choice, take a different kind of action, or abstain from something they would have done before?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote showing behavior change, or none>",
  "fix": "<one action to create visible behavior change, or none needed>"
}

pass = clear behavior change. warn = subtle or internal only. fail = same behavior at end as at start.
""",
            ct);

    private Task<StructuralCheck> CheckStakesEmbodimentAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "StakesEmbodiment",
            description: "Stakes shown through behavior/consequence, not only stated.",
            isBlocking: true,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Are the stakes in this chapter embodied — shown through the protagonist's choices, fears, or physical reactions — or are they only stated (this was dangerous, this mattered, there was a lot at risk)?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote showing stated-only stakes, or quote showing embodied stakes>",
  "fix": "<one action to embody the stakes, or none needed>"
}

pass = stakes are embodied. warn = mixed. fail = stakes are purely stated or asserted.
""",
            ct);

    private Task<StructuralCheck> CheckExpositionDensityAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "ExpositionDensity",
            description: "Chapter not dominated by information transfer over embodied action.",
            isBlocking: true,
            prompt: $$"""
You are a structural editor. Estimate the balance between:
A) Exposition: reading, thinking, reviewing data, processing information, internal analysis
B) Action: physical events, decisions, dialogue, embodied moments

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote of the longest unbroken exposition block>",
  "fix": "<one action to break up the exposition, or none needed>",
  "exposition_pct": 0
}

Set exposition_pct to an integer 0-100. pass = under 50%. warn = 50-70%. fail = over 70%.
""",
            ct);

    private Task<StructuralCheck> CheckCharacterEmbodimentAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "CharacterEmbodiment",
            description: "Named characters have at least one involuntary/physical moment.",
            isBlocking: false,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Do the named characters (beyond the protagonist) have at least one involuntary or physical moment — a gesture they didn't plan, a vocal quality, a physical reaction — that isn't plot-functional? Or do they exist purely to deliver information and plot?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote showing an involuntary moment, or the character name that lacks one>",
  "fix": "<one concrete detail to add to the flattest secondary character, or none needed>"
}

pass = at least one secondary character has an involuntary physical moment. warn = borderline. fail = all secondary characters are purely functional.
""",
            ct);

    private Task<StructuralCheck> CheckPacingGearChangeAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "PacingGearChange",
            description: "Chapter has at least one shift in rhythm or emotional register.",
            isBlocking: false,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Does the chapter have at least one meaningful gear change — a shift in pacing, tension level, or emotional register? Or does it run at one speed and one tone from start to finish?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote at the gear-change moment, or none found>",
  "fix": "<where to add a gear change and what kind, or none needed>"
}

pass = clear gear change present. warn = minor or subtle shift only. fail = single speed throughout.
""",
            ct);

    private Task<StructuralCheck> CheckAffectationLinesAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "AffectationLines",
            description: "No lines using parallel-fragment tautology or abstract reach-for-profundity.",
            isBlocking: false,
            prompt: $$"""
You are a line editor. Find lines that:
- Make abstract assertions using tautological parallel fragments (e.g. "Lag had no texture. This had texture.")
- State a universal truth in wry or ironic cadence without earning it
- Reach for profundity through repetition or inversion of a phrase rather than through meaning
- Use abstract nouns in pairs to simulate depth ("There was loss. There was distance.")

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<the single clearest affectation line quoted exactly, or none>",
  "fix": "<rewrite the clearest offender as a plain declarative line, or none needed>"
}

pass = no affectation. warn = 1-2 instances. fail = 3+ instances or one severe offender.
""",
            ct);

    private Task<StructuralCheck> CheckDramaticQuestionAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "DramaticQuestion",
            description: "Chapter has a clear question it is answering.",
            isBlocking: false,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Can you state in one sentence the dramatic question this chapter is asking and answering? For example: Will Seto find out who sent the message? Can Amara keep her discovery hidden from Ciro? Or does the chapter feel like accumulation without a question driving it?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<the dramatic question in one sentence, or unclear>",
  "fix": "<how to sharpen the chapter's driving question, or none needed>"
}

pass = clear question. warn = question exists but is buried. fail = no discernible driving question.
""",
            ct);

    private Task<StructuralCheck> CheckPassiveProtagonistAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "PassiveProtagonist",
            description: "Protagonist acts more than they react.",
            isBlocking: false,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

Is the protagonist mostly reactive — do things happen TO them (messages arrive, events occur, information surfaces) more than the protagonist CAUSES things to happen? A protagonist who only receives, discovers, and processes is passive even when doing technical work.

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote of the most passive sequence, or none>",
  "fix": "<one place to make the protagonist initiate rather than receive, or none needed>"
}

pass = protagonist causes more than reacts. warn = roughly balanced. fail = mostly reactive.
""",
            ct);

    private Task<StructuralCheck> CheckCharacterFunctionTestAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "CharacterFunction",
            description: "Secondary characters have detail beyond their plot function.",
            isBlocking: false,
            prompt: $$"""
You are a structural editor. Read this prose and answer ONE question:

For each named secondary character, does the text give them at least ONE detail that isn't required by the plot — a word choice, a physical habit, a response that's slightly off the expected pattern? Or does every secondary character exist only to do their plot job and disappear?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<name of the flattest secondary character and what makes them feel like a function>",
  "fix": "<one non-functional detail to add to the flattest character, or none needed>"
}

pass = at least one secondary character has non-functional detail. warn = borderline. fail = all secondary characters are purely functional.
""",
            ct);

    private Task<StructuralCheck> CheckDialogueSubtextAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "DialogueSubtext",
            description: "Dialogue carries subtext, not just information.",
            isBlocking: false,
            prompt: $$"""
You are a dialogue editor. Read this prose and answer ONE question:

Does the dialogue in this chapter have any subtext — things people mean but don't say, social friction, evasion, deflection, or emotion underneath the words? Or is every line of dialogue purely information exchange (question then answer, request then confirmation)?

TEXT:
{{Truncate(text, maxChars)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<quote of the flattest exchange, or none if subtext is present throughout>",
  "fix": "<one way to add subtext to the flattest exchange, or none needed>"
}

pass = subtext present. warn = minimal. fail = all dialogue is pure information exchange.
""",
            ct);

    private Task<StructuralCheck> CheckJargonFrontLoadingAsync(string text, CancellationToken ct, int maxChars = 40000) =>
        RunCheckAsync(
            name: "JargonFrontLoading",
            description: "Technical jargon is not front-loaded before reader investment.",
            isBlocking: false,
            prompt: $$"""
You are a prose editor. Read the opening section of this text (first 300 words) and count compound technical terms — jargon, trademarked systems, made-up world-nouns, multi-word technical constructs.

OPENING:
{{Truncate(text, 1500)}}

Respond ONLY with this JSON (no prose, no markdown):
{
  "result": "pass|warn|fail",
  "evidence": "<list the first 3 jargon terms in order of appearance, or none>",
  "fix": "<one way to delay the jargon until after an embodied hook, or none needed>",
  "jargon_count": 0
}

Set jargon_count to an integer. pass = 0-2 jargon terms before first physical beat. warn = 3-4. fail = 5+ or opening paragraph is pure jargon.
""",
            ct);

    // ── LLM runner ────────────────────────────────────────────────────────────

    private async Task<StructuralCheck> RunCheckAsync(
        string name, string description, bool isBlocking, string prompt, CancellationToken ct)
    {
        const string system =
            "You are a structural editor. Return ONLY the JSON object requested. No prose, no markdown fences, no explanation.";
        try
        {
            var raw  = await llm.GenerateAsync(system, prompt, 0.1, 512, null, ct);
            var json = ExtractJson(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var resultStr = root.TryGetProperty("result", out var rp) ? rp.GetString() ?? "warn" : "warn";
            var result = resultStr.ToLower() switch
            {
                "pass" => StructuralCheckResult.Pass,
                "fail" => StructuralCheckResult.Fail,
                _      => StructuralCheckResult.Warn,
            };
            var evidence = root.TryGetProperty("evidence", out var ep)
                ? ep.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", ep.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0))
                    : ep.GetString() ?? ""
                : "";
            var fix = root.TryGetProperty("fix", out var fp)
                ? fp.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", fp.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0))
                    : fp.GetString() ?? ""
                : "";

            return new StructuralCheck(name, description, result, evidence, fix, isBlocking);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Structural check {Name} failed; defaulting to Warn", name);
            return new StructuralCheck(name, description, StructuralCheckResult.Warn,
                "Check failed to run.", "Re-run the diagnostic.", isBlocking);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Truncate(string text, int maxChars = 40000) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n[... truncated for diagnostic ...]";

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
    }

    private static StructuralDiagnosisResult Empty(Guid nodeId, string slug, string title) =>
        new(nodeId, slug, title, 0, 0, 0, false, [], "No prose found — nothing to diagnose.");
}
