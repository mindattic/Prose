namespace StreetSamurai.Core.Services.Audit;

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
/// A rule checked by a single LLM call. <see cref="AuditRunner"/> owns the call itself and the
/// JSON-envelope parsing — every LLM rule speaks the same
/// <c>{"status":"pass"|"warn"|"fail","evidence":"...","fix":"..."}</c> contract (the shape
/// BookAuditService's commandments already used) so there is exactly one parser instead of one
/// per audit service. A rule only supplies its prompt and, if it wants "fail" to land somewhere
/// other than BLOCKER, overrides <see cref="SeverityOnFail"/>.
/// </summary>
public interface ILlmAuditRule : IAuditRule
{
    string SeverityOnFail => "BLOCKER";
    (string System, string User) BuildPrompt(AuditContext ctx);
}
