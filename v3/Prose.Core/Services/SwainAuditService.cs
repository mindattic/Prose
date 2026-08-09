using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Data;
using System.Text.Json;

namespace Prose.Core.Services;

// ── Data model ────────────────────────────────────────────────────────────────

public enum SwainClass { Scene, Sequel, Ambiguous, Deficient, Error }

/// <summary>Per-beat Swain classification result.</summary>
public sealed record SwainBeatResult(
    Guid   BeatId,
    int    Position,
    string Title,
    int    CharCount,
    SwainClass Classification,
    string MissingElement,   // "none" | "goal" | "conflict" | "disaster turn" | "reaction" | "dilemma" | "decision"
    string Note,             // 1-sentence evidence
    string Severity)         // "" = pass | "MODERATE" | "BLOCKER" | "ERROR" (could not evaluate — NOT a content verdict)
{
    public bool IsPass => Severity.Length == 0;
}

/// <summary>Swain audit results for one book node.</summary>
public sealed record SwainAuditReport(
    Guid   NodeId,
    string NodeCode,
    string Title,
    int    TotalBeats,
    IReadOnlyList<SwainBeatResult> Results)
{
    public int    BlockerCount   => Results.Count(r => r.Severity == "BLOCKER");
    public int    ModerateCount  => Results.Count(r => r.Severity == "MODERATE");
    public int    ErrorCount     => Results.Count(r => r.Severity == "ERROR");
    public int    PassCount      => Results.Count(r => r.IsPass);
    /// <summary>Fraction of EVALUATED beats (TotalBeats minus ones that hit an infra/parse
    /// error) that passed. Excluding errors from the denominator matters: before this fix, a
    /// total API outage silently classified every beat "Deficient/BLOCKER" (a false content
    /// verdict), driving this to 0.0 and cratering SII to "the book is 100% structurally
    /// deficient" when in truth 0% of it was ever actually evaluated.</summary>
    public double ComplianceRate
    {
        get
        {
            var evaluated = TotalBeats - ErrorCount;
            return evaluated <= 0 ? 0.0 : (double)PassCount / evaluated;
        }
    }
}

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Classifies every enabled beat in a book against Dwight Swain's Scene/Sequel
/// doctrine using a fast Haiku pass, then optionally splices in the missing
/// structural element (disaster turn, decision, etc.) with Sonnet.
///
/// Scene  — Goal → Conflict → Disaster (character does NOT fully succeed).
/// Sequel — Reaction → Dilemma → Decision (sets next direction).
///
/// Classification severity:
///   Scene / Sequel   → pass (no finding)
///   Ambiguous        → MODERATE (one element weak or underwritten)
///   Deficient        → BLOCKER  (beat does not execute either pattern)
/// </summary>
public sealed class SwainAuditService(
    IDbContextFactory<ProseDbContext> dbFactory,
    VotingConfiguration cfg,
    LegionClient legion,
    NodeWorkbenchService workbench,
    ILogger<SwainAuditService> log)
{
    private const string ClassifyModel = "claude-haiku-4-5-20251001";
    private const string SpliceModel   = "claude-sonnet-4-6";
    private const string Provider      = "claude-api";

    private string ApiKey => cfg.ApiKeys.GetValueOrDefault(Provider, "");

    private const string ClassifySystem = """
        You are a Dwight Swain dramatic-structure classifier. Read the beat and return JSON only — no markdown, no explanation.

        SCENE — active mode:
          (1) POV character has a concrete GOAL
          (2) Something actively CONFLICTS with that goal
          (3) Beat ends with DISASTER or complication — character does NOT fully succeed

        SEQUEL — reactive mode:
          (1) POV character viscerally REACTS to a prior disaster (specific emotion, felt in the body)
          (2) Faces a DILEMMA (two bad options, no clean exit)
          (3) Makes a DECISION that sets the next direction

        Classify as exactly one of:
          "Scene"     — all three Scene elements clearly present
          "Sequel"    — all three Sequel elements clearly present
          "Ambiguous" — structural elements of one pattern present but one is weak or underwritten
          "Deficient" — does not execute either pattern; a required element is missing or absent

        Return ONLY valid JSON:
        {"class":"Scene|Sequel|Ambiguous|Deficient","missing":"none|goal|conflict|disaster turn|reaction|dilemma|decision","note":"<one sentence of specific evidence>"}

        Rules:
        - "missing" is always "none" for Scene and Sequel
        - For Ambiguous/Deficient: name the WEAKEST or ABSENT element (pick the single most critical gap)
        - "note" must cite something in the text, not a generic description
        """;

    // ── Audit ─────────────────────────────────────────────────────────────────

    public async Task<SwainAuditReport> AuditAsync(string slugOrCode,
        string? classifyModel = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes
            .Where(n => n.Kind == "book" && (n.Slug == slugOrCode || n.NodeCode == slugOrCode))
            .Select(n => new { n.Id, Code = n.NodeCode ?? n.Slug ?? "", n.Title })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Book node not found: '{slugOrCode}'");
        return await AuditNodeAsync(node.Id, node.Code, node.Title ?? "", classifyModel, ct);
    }

    public async Task<List<SwainAuditReport>> AuditAllAsync(
        string? classifyModel = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nodes = await db.Nodes
            .Where(n => n.Kind == "book")
            .OrderBy(n => n.NodeCode)
            .Select(n => new { n.Id, Code = n.NodeCode ?? n.Slug ?? "", n.Title })
            .ToListAsync(ct);

        var reports = new List<SwainAuditReport>(nodes.Count);
        foreach (var node in nodes)
        {
            ct.ThrowIfCancellationRequested();
            reports.Add(await AuditNodeAsync(node.Id, node.Code, node.Title ?? "", classifyModel, ct));
        }
        return reports;
    }

    // ── Splice (repair) ───────────────────────────────────────────────────────

    public async Task<string?> LoadBeatTextAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Beats
            .Where(b => b.Id == beatId)
            .Select(b => b.Text)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<string?> SpliceAsync(SwainBeatResult finding, string beatText,
        string? spliceModel = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) { log.LogWarning("No claude-api key for splice."); return null; }

        var model = spliceModel ?? SpliceModel;
        var system = $"""
            A beat is structurally deficient — it is missing its {finding.MissingElement}.

            Dwight Swain doctrine:
            - SCENE:  Goal → Conflict → DISASTER (character does NOT fully succeed; stakes worsen)
            - SEQUEL: Reaction → Dilemma → DECISION (names what the POV character will do next)

            YOUR TASK: Add only the missing {finding.MissingElement} — the minimum prose needed to complete the dramatic unit. Attach it at the most natural point in the existing beat. Do NOT rewrite or change any existing sentences. Return the COMPLETE beat text with your splice embedded — nothing else, no commentary.

            Missing element : {finding.MissingElement}
            Evidence        : {finding.Note}
            Beat title      : {finding.Title}
            """;

        try
        {
            var result = await legion.CallAsync(
                Provider, ApiKey, model,
                system, beatText,
                maxTokens: 4096, temperature: 0.5, ct);
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Splice LLM call failed for beat {BeatId}", finding.BeatId);
            return null;
        }
    }

    public async Task<bool> ApplySpliceAsync(SwainBeatResult finding, string splicedText, CancellationToken ct = default)
    {
        try
        {
            await workbench.UpdateBeatTextAsync(finding.BeatId, splicedText, ct: ct);
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Apply splice failed for beat {BeatId}", finding.BeatId);
            return false;
        }
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private async Task<SwainAuditReport> AuditNodeAsync(
        Guid nodeId, string nodeCode, string title, string? classifyModel, CancellationToken ct)
    {
        // Load all beats first (single query) — DbContext is not thread-safe.
        // Beats live on chapter-child nodes (SS-A43), not directly on the book node.
        // GetLeafDescendantIdsAsync recurses past any nested Collection (2026-08-09 fix) and
        // returns leaves in proper reading order (SortKey-ordered, depth-first) — chapterOrder
        // below relies on that list order, which the OLD query never actually guaranteed (no
        // OrderBy at all; it worked only by incidental DB-returned order).
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var scopeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);

        // BeatNodes.SortKey is scoped PER CHAPTER, not book-global — ranges overlap across
        // chapters (verified live: one chapter's beats span SortKey 1500-65000, the next
        // chapter's span 500-21000). A plain OrderBy(SortKey) across multiple chapters scrambles
        // reading order, and the Position this method assigns (beats.Select((b,i) => i+1) below)
        // is what SwainAuditCli prints as "Beat N" — so a BLOCKER reported at that position could
        // name an entirely different beat than the one actually flagged. Order by chapter first,
        // matching the pattern GripePassService/ComprehensionProbeService/BeatChecklistGateService
        // already use for the same multi-chapter scope.
        // Unwritten beats (no prose yet) are WIP, not a structural defect — excluded from
        // classification entirely rather than flagged Deficient (matches the same principle
        // BookHealthService.BeatCoordinationAsync already applies: "a beat with no prose yet is
        // unwritten WIP, not a quality finding").
        var chapterOrder = scopeIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var rows = await db.BeatNodes
            .Include(nb => nb.Beat)
            .Where(nb => scopeIds.Contains(nb.NodeId) && nb.IsEnabled && nb.Beat!.Text != null && nb.Beat.Text != "")
            .ToListAsync(ct);
        var beats = rows
            .OrderBy(nb => chapterOrder[nb.NodeId]).ThenBy(nb => nb.SortKey)
            .Select(nb => new { nb.Beat!.Id, Title = nb.Beat.Title ?? "", Text = nb.Beat.Text ?? "" })
            .ToList();

        // Classify concurrently (up to 5 in-flight LLM calls at once).
        var sem = new SemaphoreSlim(5, 5);
        var tasks = beats.Select((b, i) => Task.Run(async () =>
        {
            await sem.WaitAsync(ct);
            try   { return await ClassifyBeatAsync(i + 1, b.Id, b.Title, b.Text, classifyModel, ct); }
            finally { sem.Release(); }
        }, ct)).ToList();

        var classified = await Task.WhenAll(tasks);
        return new SwainAuditReport(nodeId, nodeCode, title, beats.Count,
            classified.OrderBy(r => r.Position).ToList());
    }

    private async Task<SwainBeatResult> ClassifyBeatAsync(
        int position, Guid beatId, string title, string text, string? classifyModel, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
            return EvalError(beatId, position, title, 0, "Beat has no prose text.");

        if (string.IsNullOrWhiteSpace(ApiKey))
            return EvalError(beatId, position, title, text.Length, "No claude-api key configured.");

        var model = classifyModel ?? ClassifyModel;
        try
        {
            var userMsg = $"[Beat {position}: {title}]\n\n{text}";
            var raw = await legion.CallAsync(
                Provider, ApiKey, model,
                ClassifySystem, userMsg,
                maxTokens: 250, temperature: 0.1, ct);
            return ParseClassification(beatId, position, title, text.Length, raw);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Classification error for beat {Position} ({BeatId})", position, beatId);
            return EvalError(beatId, position, title, text.Length, $"Classification error: {ex.Message}");
        }
    }

    internal static SwainBeatResult ParseClassification(
        Guid beatId, int position, string title, int charCount, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return EvalError(beatId, position, title, charCount, "Empty LLM response.");

        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl >= 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }
        var open  = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open < 0 || close <= open)
            return EvalError(beatId, position, title, charCount,
                $"No JSON in response: {text[..Math.Min(80, text.Length)]}");
        try
        {
            using var doc = JsonDocument.Parse(text[open..(close + 1)]);
            var root    = doc.RootElement;
            var cls     = root.TryGetProperty("class",   out var cEl) ? cEl.GetString() ?? ""      : "";
            var missing = root.TryGetProperty("missing", out var mEl) ? mEl.GetString() ?? "none"  : "none";
            var note    = root.TryGetProperty("note",    out var nEl) ? nEl.GetString() ?? ""      : "";

            var swainClass = cls switch
            {
                "Scene"     => SwainClass.Scene,
                "Sequel"    => SwainClass.Sequel,
                "Ambiguous" => SwainClass.Ambiguous,
                _           => SwainClass.Deficient,
            };
            var severity = swainClass switch
            {
                SwainClass.Scene    => "",
                SwainClass.Sequel   => "",
                SwainClass.Ambiguous => "MODERATE",
                _                   => "BLOCKER",
            };
            return new SwainBeatResult(beatId, position, title, charCount, swainClass, missing, note, severity);
        }
        catch (JsonException ex)
        {
            return EvalError(beatId, position, title, charCount, $"JSON parse error: {ex.Message}");
        }
    }

    /// <summary>An evaluation that could not be completed (no API key, LLM exception, empty/
    /// unparseable response) — deliberately NOT SwainClass.Deficient/"BLOCKER". Before this fix,
    /// every one of these paths returned a fabricated "Deficient" content verdict, so a total API
    /// outage classified 100% of a book's beats as structurally deficient instead of reporting
    /// "0% of this book was actually evaluated" — a live, real instance of this session's most
    /// repeated bug class (a service silently converting "I could not check" into a false verdict).</summary>
    private static SwainBeatResult EvalError(Guid id, int pos, string title, int chars, string note) =>
        new(id, pos, title, chars, SwainClass.Error, "none", note, "ERROR");
}
