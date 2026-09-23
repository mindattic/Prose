using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Quote grounding tools ──────────────────────────────────────────────────────
// Mechanically verify an audit's quoted claim against the beat it is attributed to. The
// blueprint-contract verify_beat / verify_book / truth_status tools were removed 2026-09-22 with
// the structural blueprint (author ruling: the book is the beats, drawing on entities).

[McpServerToolType]
public class VerificationTools
{
    private readonly BeatVerificationService verification;
    private readonly HubInvoker hub;

    public VerificationTools(BeatVerificationService verification, HubInvoker hub)
    {
        this.verification = verification;
        this.hub = hub;
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
    public Task<string> VerifyQuoteGrounding(
        [Description("Beat GUID the finding claims this quote came from.")] string beatId,
        [Description("The exact text the audit agent claims appears in this beat.")] string quote,
        [Description("Optional: which agent/pass made this claim, for the audit trail.")] string? claimedBy = null) =>
        hub.InvokeAsync(nameof(VerificationTools), nameof(VerifyQuoteGroundingImpl), new { beatId, quote, claimedBy });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> VerifyQuoteGroundingImpl(
        string beatId,
        string quote,
        string? claimedBy = null)
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
    public Task<string> VerifyQuoteGroundingBatch(
        [Description("JSON array of claims: [{\"beatId\":\"<guid>\",\"quote\":\"<text>\"}, ...]")] string claimsJson,
        [Description("Optional: which agent/pass made these claims, for the audit trail.")] string? claimedBy = null) =>
        hub.InvokeAsync(nameof(VerificationTools), nameof(VerifyQuoteGroundingBatchImpl), new { claimsJson, claimedBy });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> VerifyQuoteGroundingBatchImpl(
        string claimsJson,
        string? claimedBy = null)
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
}
