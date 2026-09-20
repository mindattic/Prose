using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record BlueprintSyncReport(
    Guid SessionId,
    string SessionLabel,
    int Confirmed,
    int Diverged,
    int Unverified,
    List<string> DriftSummaries);

public class BlueprintSyncService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly FindingsService findings;
    private readonly ILogger<BlueprintSyncService> log;

    public BlueprintSyncService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        FindingsService findings,
        ILogger<BlueprintSyncService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.findings = findings;
        this.log = log;
    }

    public async Task<BlueprintSyncReport> SyncFromSessionAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var session = await db.EditSessions
            .FirstOrDefaultAsync(s => s.EditSessionId == sessionId, ct)
            ?? throw new InvalidOperationException($"Session {sessionId} not found.");

        var sessionBeats = await db.EditSessionBeats
            .Include(esb => esb.Beat)
            .Where(esb => esb.EditSessionId == sessionId)
            .ToListAsync(ct);

        if (sessionBeats.Count == 0)
            return new BlueprintSyncReport(sessionId, session.Label, 0, 0, 0, new());

        var beatIds = sessionBeats.Select(esb => esb.BeatId).ToHashSet();

        // Load blueprint tags for the node
        var blueprint = await db.NodeStructuralBlueprints
            .Include(bp => bp.BeatTags)
            .FirstOrDefaultAsync(bp => bp.NodeId == session.NodeId, ct);

        if (blueprint == null)
            return new BlueprintSyncReport(sessionId, session.Label, 0, 0, sessionBeats.Count, new());

        var tags = blueprint.BeatTags
            .Where(t => beatIds.Contains(t.BeatId))
            .ToList();

        int confirmed = 0, diverged = 0, unverified = sessionBeats.Count - tags.Count;
        var driftSummaries = new List<string>();

        var system = """
You are evaluating whether prose fulfills a structural blueprint commitment.
Answer ONLY with STRICT JSON — no markdown fences, no commentary:
{"verdict": "CONFIRMED" or "DIVERGED", "note": "one sentence describing what differs, or empty string if confirmed"}
""";

        foreach (var tag in tags)
        {
            var esb = sessionBeats.FirstOrDefault(b => b.BeatId == tag.BeatId);
            if (esb?.Beat == null || string.IsNullOrWhiteSpace(esb.Beat.Text))
            {
                unverified++;
                continue;
            }

            var user = $"Blueprint tag for Beat {esb.Beat.Number}: {tag.TagType} — {tag.Note}\n\nProse:\n{esb.Beat.Text}";

            try
            {
                var raw = await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 200, ct: ct);
                using var doc = JsonDocument.Parse(raw.Trim());
                var verdict = doc.RootElement.GetProperty("verdict").GetString() ?? "DIVERGED";
                var note    = doc.RootElement.TryGetProperty("note", out var n) ? n.GetString() ?? "" : "";

                if (verdict == "CONFIRMED")
                {
                    tag.Confirmed            = true;
                    tag.ConfirmedAt          = DateTime.UtcNow;
                    tag.ConfirmedBySessionId = sessionId;
                    confirmed++;
                }
                else
                {
                    diverged++;
                    var summary = $"Beat {esb.Beat.Number} ({tag.TagType}): {note}";
                    driftSummaries.Add(summary);
                    findings.Upsert(
                        filePath: $"beat:{tag.BeatId}",
                        chapterId: null,
                        category: FindingCategory.Other,
                        severity: FindingSeverity.Low,
                        summary: $"BLUEPRINT-DRIFT: {summary}",
                        snippet: null,
                        suggestedFix: $"Revisit blueprint tag '{tag.TagType}' for Beat {esb.Beat.Number}: {tag.Note}");
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "BlueprintSyncService LLM failed for tag {TagId}", tag.Id);
                unverified++;
            }
        }

        await db.SaveChangesAsync(ct);

        return new BlueprintSyncReport(sessionId, session.Label,
            confirmed, diverged, unverified, driftSummaries);
    }
}
