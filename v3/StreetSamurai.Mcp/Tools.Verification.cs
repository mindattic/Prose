using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

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
        "Run verification checks for all enabled beats in a book. Returns a summary of " +
        "BLOCKER/MODERATE/MINOR failures plus individual findings. Results are upserted to " +
        "BeatVerification table. BLOCKER findings must be fixed before export. " +
        "Includes EscalationMonotonic check (book-wide curve regression) not available per-beat.")]
    public async Task<string> VerifyBook(
        [Description("Book node slug or NodeCode.")] string slugOrCode)
    {
        try
        {
            var summary = await verification.VerifyBookAsync(slugOrCode);
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
                partials      = summary.Partials,
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
        "Verify that a logic-sweep audit agent's CLAIMED QUOTE actually appears in the beat it's " +
        "attributed to, before that finding is trusted for triage/fix. Use this on every quoted " +
        "finding an audit agent reports — agents occasionally misattribute a quote to the wrong " +
        "beat or fabricate one under time pressure; this is the mechanical guard against that. " +
        "Comparison is normalized (dash variants, curly/straight quotes, whitespace), so only a " +
        "genuine misattribution fails — not console-display punctuation drift. Result is persisted " +
        "to BeatVerification (CheckType='QuoteGrounding', always inserted, never overwritten — a " +
        "beat accumulates one row per claim checked across every sweep). A Fail means: reject the " +
        "finding and re-read the actual beat before acting on it.")]
    public async Task<string> VerifyQuoteGrounding(
        [Description("Beat GUID the finding claims this quote came from.")] string beatId,
        [Description("The exact text the audit agent claims appears in this beat.")] string quote,
        [Description("Optional: which agent/pass made this claim, for the audit trail.")] string? claimedBy = null)
    {
        if (!Guid.TryParse(beatId, out var id))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        var r = await verification.VerifyQuoteGroundingAsync(id, quote, claimedBy);
        return JsonSerializer.Serialize(new
        {
            beat_id     = beatId,
            result      = r.Result,
            severity    = r.Severity,
            evidence    = r.Evidence,
            verified_by = r.VerifiedBy,
            verdict     = r.Result == "Fail"
                ? "REJECTED — quote not found in this beat; do not act on this finding"
                : "grounded",
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Batch form of VerifyQuoteGrounding: gate an ENTIRE audit report in one call before triage. " +
        "Pass every (beatId, quote) claim the audit produced; get back which ones are actually " +
        "grounded in their attributed beat and which must be rejected/re-verified. Run this before " +
        "triaging any logic-sweep audit findings that quote beat text (SS-LOGIC-4a).")]
    public async Task<string> VerifyQuoteGroundingBatch(
        [Description("JSON array of claims: [{\"beatId\":\"<guid>\",\"quote\":\"<text>\"}, ...]")] string claimsJson,
        [Description("Optional: which agent/pass made these claims, for the audit trail.")] string? claimedBy = null)
    {
        List<QuoteClaimDto> claims;
        try
        {
            claims = JsonSerializer.Deserialize<List<QuoteClaimDto>>(claimsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"invalid claimsJson: {ex.Message}" }, CanonTools.JsonOpts);
        }

        var parsed = new List<(Guid, string)>();
        var badGuids = new List<string>();
        foreach (var c in claims)
        {
            if (Guid.TryParse(c.BeatId, out var g)) parsed.Add((g, c.Quote));
            else badGuids.Add(c.BeatId);
        }

        var results = await verification.VerifyQuoteGroundingBatchAsync(parsed, claimedBy);
        var failed  = results.Where(r => r.Result == "Fail").ToList();

        return JsonSerializer.Serialize(new
        {
            total          = results.Count,
            failed         = failed.Count,
            invalid_guids  = badGuids,
            gate           = failed.Count == 0 && badGuids.Count == 0
                ? "PASS — every claimed quote is grounded"
                : $"BLOCKED — {failed.Count} ungrounded claim(s), {badGuids.Count} invalid beat id(s). Reject those findings.",
            rejected       = failed.Select(f => new { beat_id = f.BeatId, evidence = f.Evidence }).ToList(),
        }, CanonTools.JsonOpts);
    }

    private record QuoteClaimDto(string BeatId, string Quote);

    [McpServerTool, Description(
        "Get the current truth status for a book: how many beats have verified contracts, " +
        "how many have BeatBlueprintDecision rows, how many are in violation. " +
        "Use this as a quick dashboard check before writing or exporting.")]
    public async Task<string> TruthStatus(
        [Description("Book node slug or NodeCode.")] string slugOrCode)
    {
        try
        {
            var summary = await verification.VerifyBookAsync(slugOrCode);
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
                partials      = summary.Partials,
                // BUG FIX: was `Passed / (BeatsChecked*3 + 1)` — a guessed "3 checks per beat"
                // denominator that has no relation to the real check count (1-5 checks run per
                // beat depending on whether a BeatBlueprintDecision/embeddings are present), so
                // truth_score could exceed 100% whenever more than 3 checks/beat actually ran and
                // passed. Use the real total of executed (non-skipped) checks instead.
                truth_score   = (summary.Blockers + summary.Moderates + summary.Minors + summary.Passed) > 0
                    ? (int)(100.0 * summary.Passed / (summary.Blockers + summary.Moderates + summary.Minors + summary.Passed))
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
