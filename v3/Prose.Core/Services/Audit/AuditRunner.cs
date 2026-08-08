using System.Text.Json;
using System.Text.Json.Serialization;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Shared dispatch + persistence for audit rules — replaces the near-identical
/// Task.WhenAll-fan-out / JSON-envelope-parse / Findings-write code that used to be hand-rolled
/// separately in BookAuditService, ChekhovAuditService, AltitudeAuditService, and
/// StoryScopeAuditService (only 2 of those 4 wrote to Findings at all, and the 2 that did used
/// different lifecycles — this standardizes on the safer one: delete-then-recreate every rule's
/// findings each run, so a rule whose violation count drops to zero doesn't leave orphaned rows
/// behind forever).
///
/// Not every existing audit fits this shape (AltitudeAuditService and ChekhovAuditService return
/// a variable-length list from a single LLM call rather than "N rules, ≤1 verdict-set each" —
/// left as bespoke rather than forced onto this). This is for rules that really are independent,
/// enumerable checks: BookAuditService's commandments, NounConsistencyService's deprecated-name
/// rows, and LogicSweepService's six dimensions all fit.
/// </summary>
public class AuditRunner(ILlmService llm, FindingsService findings)
{
    /// <summary>
    /// Runs every rule (in parallel), then — unless <paramref name="writeFindings"/> is false —
    /// persists the result: for EVERY rule in <paramref name="rules"/> (not just the ones that
    /// found something), delete any Findings row this audit previously wrote for it, then
    /// re-insert whatever it found this run. Doing the delete unconditionally per rule (rather
    /// than only for rules with a PASS verdict) is what keeps a rule that fires a variable
    /// number of times per run (0 here, 3 there) from leaving stale rows behind for hits that
    /// stopped reproducing.
    /// </summary>
    public async Task<IReadOnlyList<AuditVerdict>> RunAsync(
        string auditName,
        string filePathKey,
        FindingCategory category,
        IReadOnlyList<IAuditRule> rules,
        AuditContext ctx,
        bool writeFindings = true,
        CancellationToken ct = default)
    {
        var results = await Task.WhenAll(rules.Select(r => EvaluateOneAsync(r, ctx, ct)));
        var verdicts = results.SelectMany(v => v).ToList();
        if (writeFindings)
        {
            // Reap beat-anchored findings whose beat has since been soft-deleted before writing
            // this run's rows. The delete-then-recreate lifecycle below only cleans up findings
            // THIS audit wrote; findings written by the prose pipeline (EntityContextService's
            // ENTITY-CONFLICT) are never revisited by anything else, so a superseded draft beat
            // leaves them open forever, quoting prose no longer in the book. Any audit run is a
            // safe, cheap moment to clear them — see FindingsService.DismissStaleBeatFindingsAsync.
            await findings.DismissStaleBeatFindingsAsync(ct);
            WriteFindingsForRules(auditName, filePathKey, category, rules.Select(r => r.Key).ToList(), verdicts);
        }
        return verdicts;
    }

    /// <summary>
    /// The persistence half of <see cref="RunAsync"/>, exposed standalone for a caller that
    /// already has its own verdicts (computed some other way — e.g. NounConsistencyService
    /// keeps its original, already-correct per-beat scan loop rather than routing through
    /// <see cref="EvaluateOneAsync"/>, since that loop has no LLM step for the dispatcher to
    /// add value to) but still wants the standardized delete-then-recreate Findings lifecycle
    /// instead of hand-rolling it again.
    /// </summary>
    public void WriteFindingsForRules(
        string auditName, string filePathKey, FindingCategory category,
        IReadOnlyList<string> ruleKeys, IReadOnlyList<AuditVerdict> verdicts)
    {
        var prefix = auditName.ToUpperInvariant();

        // Delete-before-insert per rule, unconditionally, so a rule that used to fire on N
        // beats and now fires on fewer doesn't leave the difference behind as permanently
        // stale rows. DeleteKeyPrefix always includes the '@' boundary (see SummaryFor) so a
        // short numeric key like "noun_3" can never delete a longer key's rows too
        // ("noun_34@..." starts with "noun_3" as a bare string, but not with "noun_3@").
        foreach (var key in ruleKeys)
            findings.DeleteBySummaryPrefix(filePathKey, DeleteKeyPrefix(prefix, key));

        foreach (var v in verdicts.Where(v => v.Severity != "PASS"))
        {
            var summary = $"{SummaryFor(prefix, v.RuleKey, v.Location)}{Truncate(v.Evidence, 300)}";
            var severity = v.Severity switch
            {
                "BLOCKER"  => FindingSeverity.High,
                "MODERATE" => FindingSeverity.Medium,
                _          => FindingSeverity.Low, // MINOR, DEVIATION
            };
            findings.Upsert(filePathKey, v.Location, category, severity, summary, null, v.Fix);
        }
    }

    /// <summary>
    /// For a rule that's being retired entirely (not just producing zero violations this run —
    /// actually removed from its catalog, e.g. NounConsistencyService.DeleteRuleAsync), clears
    /// every Finding it ever wrote, across every node (empty filePath prefix = no node filter),
    /// since a per-run RunAsync/WriteFindingsForRules call only clears the one node/scope it
    /// was invoked against and never learns the rule stopped existing anywhere else.
    /// </summary>
    public void DeleteAllForRule(string auditName, string ruleKey) =>
        findings.DeleteBySummaryPrefix("", DeleteKeyPrefix(auditName.ToUpperInvariant(), ruleKey));

    // '@' always separates the rule key from whatever follows (an "@location" then "): ", or
    // straight to "): " when there's no location) — never valid inside a rule key — so it's a
    // safe, unambiguous prefix boundary no other key's summaries can accidentally match.
    internal static string DeleteKeyPrefix(string prefix, string ruleKey) => $"{prefix} {ruleKey}@";
    internal static string SummaryFor(string prefix, string ruleKey, string? location) =>
        $"{DeleteKeyPrefix(prefix, ruleKey)}{location}: ";

    async Task<IReadOnlyList<AuditVerdict>> EvaluateOneAsync(IAuditRule rule, AuditContext ctx, CancellationToken ct)
    {
        try
        {
            return rule switch
            {
                IDeterministicAuditRule det => await det.EvaluateAsync(ctx, ct),
                ILlmAuditRule llmRule       => await RunLlmRuleAsync(llmRule, ctx, ct),
                _ => throw new NotSupportedException(
                    $"Rule '{rule.Key}' implements neither {nameof(IDeterministicAuditRule)} nor {nameof(ILlmAuditRule)}."),
            };
        }
        catch (Exception ex)
        {
            // A rule that throws (LLM timeout, malformed response, whatever) is reported as a
            // MODERATE finding rather than silently vanishing from the audit — matches
            // BookAuditService's prior "warn" fallback on exception.
            return [new AuditVerdict(rule.Key, rule.Title, "MODERATE", $"Evaluation failed: {ex.Message}")];
        }
    }

    async Task<IReadOnlyList<AuditVerdict>> RunLlmRuleAsync(ILlmAuditRule rule, AuditContext ctx, CancellationToken ct)
    {
        var (system, user) = rule.BuildPrompt(ctx);
        var raw = await llm.GenerateAsync(system, user, temperature: 0.2, maxTokens: rule.MaxResponseTokens, ct: ct);
        return rule.ParseResponse(raw, ctx);
    }

    /// <summary>Default <see cref="ILlmAuditRule.ParseResponse"/> implementation — the shared
    /// single-verdict <c>{"status","evidence","fix"}</c> envelope every commandment-style rule
    /// speaks. Public so a rule overriding ParseResponse for its own shape can still fall back to
    /// this for the common case, and so a genuinely bespoke rule elsewhere in the app can reuse it
    /// without depending on AuditRunner's internals.</summary>
    public static IReadOnlyList<AuditVerdict> ParseSingleVerdict(ILlmAuditRule rule, string raw)
    {
        var parsed = ParseVerdictEnvelope(raw);
        var severity = parsed?.Status switch
        {
            "pass" => "PASS",
            "warn" => "MODERATE",
            "fail" => rule.SeverityOnFail,
            _      => "MODERATE",
        };
        return [new AuditVerdict(rule.Key, rule.Title, severity, parsed?.Evidence ?? "(evaluation failed)", null, parsed?.Fix)];
    }

    internal static VerdictEnvelope? ParseVerdictEnvelope(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < start) return null;
            return JsonSerializer.Deserialize<VerdictEnvelope>(raw[start..(end + 1)],
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    internal static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    internal class VerdictEnvelope
    {
        [JsonPropertyName("status")]   public string? Status   { get; set; }
        [JsonPropertyName("evidence")] public string? Evidence { get; set; }
        [JsonPropertyName("fix")]      public string? Fix      { get; set; }
    }
}
