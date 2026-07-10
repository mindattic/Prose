using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using System.Text.Json;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Post-write liberty analysis — "Rule of Cool" service.
///
/// After every beat is written, a lightweight Haiku-class LLM call lists every
/// creative departure taken relative to the beat goal and entity roster, then scores
/// each on a CoolFactor (0–10):
///
///   CoolFactor ≥ 8 → <c>CANON-ADDITION-CANDIDATE</c> finding (user must approve before canon entry)
///   CoolFactor 5–7 → <c>LIBERTY-CONSIDER</c> advisory finding
///   CoolFactor ≤ 4 AND kind=entity_invention → <c>LIBERTY-WARNING</c> finding
///
/// The raw report is stored in the <c>LibertyReports</c> table (one row per beat).
/// Called by ProseWriterRouter as a fire-and-forget post-write Task — never delays beat output.
/// </summary>
public class LibertyReportService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm,
    FindingsService findings,
    ILogger<LibertyReportService> log)
{
    // Scoring thresholds
    private const int CanonCandidateFloor = 8;
    private const int AdvisoryFloor       = 5;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented               = false,
    };

    /// <summary>
    /// Analyse the completed beat prose, score creative liberties, persist the report,
    /// and file findings for any Rule-of-Cool candidates. Non-blocking; catches all exceptions.
    /// </summary>
    public async Task AnalyseAsync(
        Guid beatId, string prose, string? beatGoal, string? entityRoster,
        CancellationToken ct = default)
    {
        if (beatId == Guid.Empty || string.IsNullOrWhiteSpace(prose)) return;

        try
        {
            var liberties = await ExtractLibertiesAsync(prose, beatGoal, entityRoster, ct);
            var coolMax   = liberties.Count > 0 ? liberties.Max(l => l.CoolFactor) : -1;

            // Persist the raw report.
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var existing = await db.LibertyReports.FirstOrDefaultAsync(r => r.BeatId == beatId, ct);
            if (existing != null)
            {
                existing.GeneratedAt   = DateTime.UtcNow;
                existing.LibertiesJson = JsonSerializer.Serialize(liberties, JsonOpts);
                existing.CoolFactorMax = coolMax;
            }
            else
            {
                db.LibertyReports.Add(new LibertyReport
                {
                    BeatId        = beatId,
                    GeneratedAt   = DateTime.UtcNow,
                    LibertiesJson = JsonSerializer.Serialize(liberties, JsonOpts),
                    CoolFactorMax = coolMax,
                });
            }
            await db.SaveChangesAsync(ct);

            // File findings for notable liberties.
            var filePath = $"beat:{beatId:N}";
            foreach (var liberty in liberties)
            {
                if (liberty.CoolFactor >= CanonCandidateFloor)
                {
                    findings.Upsert(
                        filePath, chapterId: null,
                        FindingCategory.Other, FindingSeverity.Low,
                        $"CANON-ADDITION-CANDIDATE [{liberty.Name}]: {liberty.Explanation}",
                        snippet: liberty.Evidence,
                        suggestedFix: $"CoolFactor {liberty.CoolFactor}/10 — seed this into the DB if you want it in canon.");
                }
                else if (liberty.CoolFactor >= AdvisoryFloor)
                {
                    findings.Upsert(
                        filePath, chapterId: null,
                        FindingCategory.Other, FindingSeverity.Low,
                        $"LIBERTY-CONSIDER [{liberty.Name}]: {liberty.Explanation}",
                        snippet: liberty.Evidence,
                        suggestedFix: $"CoolFactor {liberty.CoolFactor}/10 — advisory; no action required.");
                }
                else if (liberty.Kind == "entity_invention")
                {
                    findings.Upsert(
                        filePath, chapterId: null,
                        FindingCategory.Other, FindingSeverity.Medium,
                        $"LIBERTY-WARNING [{liberty.Name}]: invented entity not in canon — {liberty.Explanation}",
                        snippet: liberty.Evidence,
                        suggestedFix: "Seed the entity via CLI/MCP or revise the prose to remove the reference.");
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[LibertyReportService] Analysis failed for beat {BeatId}", beatId);
        }
    }

    /// <summary>
    /// Returns the stored liberty items for a beat, or an empty list if none on file.
    /// </summary>
    public async Task<IReadOnlyList<LibertyItem>> GetAsync(Guid beatId, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var report = await db.LibertyReports.AsNoTracking()
                .FirstOrDefaultAsync(r => r.BeatId == beatId, ct);
            if (report == null) return [];
            return JsonSerializer.Deserialize<List<LibertyItem>>(report.LibertiesJson, JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[LibertyReportService] GetAsync failed for beat {BeatId}", beatId);
            return [];
        }
    }

    /// <summary>
    /// Returns all stored liberty reports for beats in the given node (by slug),
    /// ordered newest first. For CLI/MCP display.
    /// </summary>
    public async Task<IReadOnlyList<(Guid BeatId, DateTime GeneratedAt, IReadOnlyList<LibertyItem> Liberties, int CoolFactorMax)>>
        GetForNodeAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var nodeId = await db.Nodes.AsNoTracking()
                .Where(n => n.Slug == slug)
                .Select(n => (Guid?)n.Id)
                .FirstOrDefaultAsync(ct);
            if (nodeId == null) return [];

            // SS-A43: beats live on chapter nodes (children), not directly on the story node.
            var childIds = await db.Nodes.AsNoTracking()
                .Where(n => n.ParentNodeId == nodeId)
                .Select(n => n.Id).ToListAsync(ct);
            var beatNodeIds = childIds.Count > 0 ? childIds : new List<Guid> { nodeId!.Value };

            var beatIds = await db.BeatNodes.AsNoTracking()
                .Where(bn => beatNodeIds.Contains(bn.NodeId) && bn.IsEnabled)
                .Select(bn => bn.BeatId)
                .ToListAsync(ct);

            if (beatIds.Count == 0) return [];

            var reports = await db.LibertyReports.AsNoTracking()
                .Where(r => beatIds.Contains(r.BeatId))
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync(ct);

            return reports.Select(r => (
                r.BeatId, r.GeneratedAt,
                (IReadOnlyList<LibertyItem>)(JsonSerializer.Deserialize<List<LibertyItem>>(r.LibertiesJson, JsonOpts) ?? []),
                r.CoolFactorMax)).ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[LibertyReportService] GetForNodeAsync failed for slug {Slug}", slug);
            return [];
        }
    }

    // ── LLM call ──────────────────────────────────────────────────────────────

    private async Task<List<LibertyItem>> ExtractLibertiesAsync(
        string prose, string? beatGoal, string? entityRoster, CancellationToken ct)
    {
        var goalLine   = string.IsNullOrWhiteSpace(beatGoal)    ? "(no beat goal provided)" : beatGoal.Trim();
        var rosterLine = string.IsNullOrWhiteSpace(entityRoster) ? "(no entity roster provided)" : entityRoster.Trim();

        var proseClip = prose.Length > 4000 ? (prose[..4000] + "…") : prose;
        var prompt = $$"""
            BEAT GOAL: {{goalLine}}

            ENTITY ROSTER (canon names only — anything NOT here is an invention):
            {{rosterLine}}

            PROSE:
            {{proseClip}}

            ---
            List every creative liberty taken in the prose relative to the beat goal and entity roster.
            A liberty is: (1) an entity name used that is NOT in the roster, (2) a tech/physics
            departure from the GLMZ 2226 cyberpunk world, or (3) a plot/character choice that goes
            meaningfully beyond what the beat goal implies.

            For EACH liberty output ONE JSON object on its own line (no array brackets, no commas between objects):
            {"kind":"entity_invention|tech_departure|creative_departure","name":"short label","evidence":"<=30 char prose quote","explanation":"one sentence why this is a departure","coolFactor":0}

            coolFactor: 0-10. 10 = so strong it should enter canon. 0 = straightforward violation.
            Be conservative — only flag genuine departures, not creative execution of the goal.
            If there are NO liberties, output exactly: {"kind":"none","name":"","evidence":"","explanation":"no liberties detected","coolFactor":-1}
            """;

        var system = "You are a literary continuity auditor for a cyberpunk story engine. Return ONLY the JSON objects described — no preamble, no explanation, no markdown.";

        var raw = await llm.GenerateAsync(system, prompt, temperature: 0.1, maxTokens: 512, model: LlmModels.Haiku, ct: ct);

        var items = new List<LibertyItem>();
        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith('{')) continue;
            try
            {
                var item = JsonSerializer.Deserialize<LibertyItem>(line, JsonOpts);
                if (item != null && item.Kind != "none")
                    items.Add(item);
            }
            catch { /* skip malformed lines */ }
        }
        return items;
    }
}
