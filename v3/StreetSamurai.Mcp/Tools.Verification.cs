using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Beat Verification tools (Track C — Truth-First Architecture) ──────────────
// Verify that generated prose fulfilled its declared BeatBlueprintDecision contract.
// Results are written to BeatVerification table (upsert pattern).

[McpServerToolType]
public class VerificationTools
{
    private readonly BeatVerificationService verification;

    public VerificationTools(BeatVerificationService verification)
    {
        this.verification = verification;
    }

    [McpServerTool, Description(
        "Run all verification checks for a single beat against its declared BeatBlueprintDecision contract. " +
        "Checks: BannedPattern (internal-understanding/epilogue anti-patterns), EventType (declared vs detected), " +
        "SubplotCarrier (entities present when declared), EscalationFloor (emotional depth vs floor), " +
        "DeclaredPurpose (embedding similarity — requires embeddings). " +
        "Results are upserted to BeatVerification table. Returns Pass/Fail/Partial/Skipped per check with evidence. " +
        "Exit 1 (blockers found) if any BLOCKER check fails.")]
    public async Task<string> VerifyBeat(
        [Description("Beat GUID to verify.")] string beatId)
    {
        if (!Guid.TryParse(beatId, out var id))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        var results = await verification.VerifyBeatAsync(id);
        return JsonSerializer.Serialize(new
        {
            beat_id   = beatId,
            checks    = results.Count,
            blockers  = results.Count(r => r.Result == "Fail" && r.Severity == "BLOCKER"),
            moderates = results.Count(r => r.Result == "Fail" && r.Severity == "MODERATE"),
            passed    = results.Count(r => r.Result == "Pass"),
            skipped   = results.Count(r => r.Result == "Skipped"),
            results   = results.Select(r => new
            {
                check_type = r.CheckType,
                result     = r.Result,
                severity   = r.Severity,
                evidence   = r.Evidence,
                verified_by = r.VerifiedBy,
            }).ToList(),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Run verification checks for all enabled beats in a story. Returns a summary of " +
        "BLOCKER/MODERATE/MINOR failures plus individual findings. Results are upserted to " +
        "BeatVerification table. BLOCKER findings must be fixed before export. " +
        "Includes EscalationMonotonic check (story-wide curve regression) not available per-beat.")]
    public async Task<string> VerifyStory(
        [Description("Story node slug or NodeCode.")] string slugOrCode)
    {
        try
        {
            var summary = await verification.VerifyStoryAsync(slugOrCode);
            return JsonSerializer.Serialize(new
            {
                node_id       = summary.NodeId,
                slug          = summary.Slug,
                beats_checked = summary.BeatsChecked,
                blockers      = summary.Blockers,
                moderates     = summary.Moderates,
                minors        = summary.Minors,
                passed        = summary.Passed,
                skipped       = summary.Skipped,
                export_gate   = summary.Blockers == 0 ? "PASS" : $"BLOCKED — {summary.Blockers} BLOCKER findings",
                findings      = summary.Findings.Select(f => new
                {
                    beat_id    = f.BeatId,
                    check_type = f.CheckType,
                    result     = f.Result,
                    severity   = f.Severity,
                    evidence   = f.Evidence,
                }).ToList(),
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, CanonTools.JsonOpts);
        }
    }

    [McpServerTool, Description(
        "Get the current truth status for a story: how many beats have verified contracts, " +
        "how many have BeatBlueprintDecision rows, how many are in violation. " +
        "Use this as a quick dashboard check before writing or exporting.")]
    public async Task<string> TruthStatus(
        [Description("Story node slug or NodeCode.")] string slugOrCode)
    {
        try
        {
            var summary = await verification.VerifyStoryAsync(slugOrCode);
            return JsonSerializer.Serialize(new
            {
                node_id       = summary.NodeId,
                slug          = summary.Slug,
                beats_total   = summary.BeatsChecked,
                blockers      = summary.Blockers,
                moderates     = summary.Moderates,
                minors        = summary.Minors,
                passed        = summary.Passed,
                skipped       = summary.Skipped,
                truth_score   = summary.BeatsChecked > 0
                    ? (int)(100.0 * summary.Passed / (summary.BeatsChecked * 3 + 1))
                    : 0,
                verdict       = summary.Blockers > 0
                    ? $"BLOCKED — {summary.Blockers} BLOCKER(s) must be fixed before export"
                    : summary.Moderates > 0
                        ? $"WARNINGS — {summary.Moderates} MODERATE finding(s)"
                        : "CLEAN",
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, CanonTools.JsonOpts);
        }
    }
}
