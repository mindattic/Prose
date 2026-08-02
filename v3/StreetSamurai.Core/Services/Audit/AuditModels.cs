namespace StreetSamurai.Core.Services.Audit;

/// <summary>Shared prose-clamping used by every audit that hands a whole node's concatenated
/// prose to a single LLM call (BookAuditService, StoryScopeAuditService, LogicSweepService,
/// CraftRuleAuditService — previously four identical private copies of this method).</summary>
public static class AuditProseUtils
{
    /// <summary>Keeps head AND tail on truncation — checks that read the opening or the ending
    /// (a Save-the-Cat "Final Image" check, an ending-style check, etc.) would false-fail on a
    /// head-only truncation of an oversized manuscript.</summary>
    public static string ClampProse(string p) =>
        p.Length <= 100_000
            ? p
            : p[..50_000] + "\n\n[... middle of the manuscript elided for length ...]\n\n" + p[^50_000..];
}

/// <summary>One enabled beat, pre-loaded once per audit run so individual rules don't each
/// re-query the DB.</summary>
public record AuditBeat(Guid Id, int Number, string Text);

/// <summary>
/// Everything a rule needs to evaluate one node. <see cref="Prose"/> is the whole node's
/// beats concatenated (what a single-LLM-call-over-the-corpus rule wants);
/// <see cref="Beats"/> is the same content as individual records (what a per-beat scan wants).
/// <see cref="Extra"/> is the escape hatch for a rule that needs something neither covers
/// (e.g. a plant/payoff registry for one specific commandment) — don't reach for it before
/// checking whether Prose/Beats already has what's needed.
/// </summary>
public record AuditContext(
    Guid NodeId,
    Guid UniverseId,
    string Prose,
    IReadOnlyList<AuditBeat> Beats,
    IReadOnlyDictionary<string, object?> Extra);

/// <summary>
/// One rule's verdict. <see cref="Location"/> is optional and rule-defined — null for a
/// whole-node verdict (e.g. a BookAuditService commandment), a beat id (or whatever the rule
/// considers its finest addressable unit) for a rule that can fire more than once per node.
/// A rule that fires on multiple beats returns multiple verdicts, one per hit — there is no
/// requirement that a rule return exactly one.
/// </summary>
public record AuditVerdict(
    string RuleKey,
    string Title,
    string Severity,   // "PASS" | "BLOCKER" | "MODERATE" | "MINOR" | "DEVIATION"
    string Evidence,
    string? Location = null,
    string? Fix = null);

public interface IAuditRule
{
    /// <summary>Stable, unique-within-this-audit key — becomes part of the Findings dedup
    /// prefix, so renaming it orphans any Findings rows already written under the old key
    /// (they won't auto-heal; a human has to notice and dismiss them).</summary>
    string Key { get; }
    string Title { get; }
}

/// <summary>A rule that evaluates itself in code — no LLM call. Can return zero, one, or many
/// verdicts (e.g. NounConsistency: one violation per beat a deprecated name appears in).</summary>
public interface IDeterministicAuditRule : IAuditRule
{
    Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct);
}

/// <summary>
/// A rule checked by a single LLM call. <see cref="AuditRunner"/> owns making the call; response
/// parsing defaults to the shared <c>{"status":"pass"|"warn"|"fail","evidence":"...","fix":"..."}</c>
/// contract (one verdict per rule — the shape BookAuditService's commandments use) via
/// <see cref="ParseResponse"/>'s default implementation, so most rules need only supply a prompt.
/// A rule whose single LLM call can legitimately surface a VARIABLE number of findings (e.g. a
/// LogicSweepService dimension scanning a whole book for causality breaks — could be zero, could
/// be five, each at a different beat) overrides <see cref="ParseResponse"/> to parse its own
/// richer JSON shape (typically an array) instead of the single-verdict envelope.
/// </summary>
public interface ILlmAuditRule : IAuditRule
{
    string SeverityOnFail => "BLOCKER";
    /// <summary>Response budget for this rule's call — commandment-style single-verdict rules
    /// fit comfortably in the default; a rule reading a whole book for a variable-length list of
    /// findings needs much more room to enumerate them.</summary>
    int MaxResponseTokens => 400;
    (string System, string User) BuildPrompt(AuditContext ctx);
    /// <summary>Receives the same <paramref name="ctx"/> BuildPrompt got — a rule that maps a
    /// beat-number citation back to a real BeatId (for <see cref="AuditVerdict.Location"/>)
    /// needs <c>ctx.Beats</c> to do it.</summary>
    IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => AuditRunner.ParseSingleVerdict(this, raw);
}
