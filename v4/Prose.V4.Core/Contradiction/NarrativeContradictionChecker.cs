using Prose.Core.Interfaces;
using Prose.V4.Core.Ledger;

namespace Prose.V4.Core.Contradiction;

/// <summary>Verdict from one contradiction check. <see cref="Reasoning"/> is written FIRST by the
/// model, <see cref="Contradicts"/> is parsed from the LAST line — this session already measured
/// (RFC 0013, docs/LEDGER.md §3.1) that a verdict emitted before its reasoning lets the model argue
/// against its own boolean roughly one time in ten; asking for reasoning first and parsing the
/// verdict last is the fix, applied here from the start rather than discovered the hard way again.</summary>
public sealed record ContradictionVerdict(bool Contradicts, string? ViolatedFact, string Reasoning);

/// <summary>
/// v4 plan Phase 3: the first content-reading check in the whole pipeline that can actually block a
/// save. Deliberately NOT an <see cref="Prose.Core.Services.WriteGate.IWriteGateSyncCheck"/> — that
/// interface's own doc comment forbids LLM calls ("must be cheap... slow judgment-based checks
/// belong in IWriteAuditService instead"), and this check runs an LLM call by design. Instead it's
/// a plain step in <c>BeatWriteOrchestrator</c>'s own pipeline: generate → check → (retry once) →
/// only then reach the ordinary save path, which still runs through the real
/// <c>IWriteGateSyncCheck</c>s unmodified.
///
/// Scoped narrow and cheap on purpose (this session's own literature review found even the best
/// full-book auto-verifiers cap out around F1 ~0.5 on unfaithful-claim detection — full omniscience
/// isn't the target): it compares ONLY the just-generated beat text against the specific
/// <see cref="OnScreenFact"/> rows the prompt assembler already gave the model as ground truth for
/// this beat — not the whole book, not inferred canon, just "did you contradict what you were just
/// told is true right now."
/// </summary>
public sealed class NarrativeContradictionChecker
{
    private readonly ILlmService llm;

    public NarrativeContradictionChecker(ILlmService llm)
    {
        this.llm = llm;
    }

    public async Task<ContradictionVerdict> CheckAsync(
        IReadOnlyList<OnScreenFact> facts, string beatText, CancellationToken ct = default)
    {
        if (facts.Count == 0)
            return new ContradictionVerdict(false, null, "No established facts were given for this beat — nothing to contradict.");

        var factLines = string.Join("\n", facts.Select(f => $"- {f.EntityName} — {f.Predicate}: {f.Value}"));
        const string system = "You check one piece of prose against a short list of established facts. You are looking for a DIRECT, unambiguous contradiction — the text asserting the opposite of a fact it was given — not a natural development, a new detail, or an omission. A character acting on, using, or building on an established fact is never a contradiction. Only flag it when the text states something that cannot both be true at once with a fact above, in the same moment the text describes.";
        var user = $"""
            ESTABLISHED FACTS (given to the writer as ground truth for this beat):
            {factLines}

            BEAT TEXT:
            {beatText}

            Think step by step: for each fact, does the beat text assert something that directly and
            unambiguously contradicts it — the opposite of the same claim at the same moment — or is
            the text merely consistent, silent, or building on the fact? Write your reasoning in 2-4
            sentences. Then, on its own final line, write EXACTLY one of:
            VERDICT: CONTRADICTS <one-line quote of the violated fact>
            VERDICT: CONSISTENT
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.0, maxTokens: 400, ct: ct);
        return Parse(raw);
    }

    internal static ContradictionVerdict Parse(string raw)
    {
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var verdictLine = lines.LastOrDefault(l => l.TrimStart().StartsWith("VERDICT:", StringComparison.OrdinalIgnoreCase));
        var reasoning = verdictLine == null ? raw.Trim() : raw[..raw.LastIndexOf(verdictLine, StringComparison.Ordinal)].Trim();

        if (verdictLine == null)
            return new ContradictionVerdict(false, null, reasoning + "\n(no VERDICT line found — treated as CONSISTENT, fail-open by design for Phase 3 calibration visibility)");

        var body = verdictLine.Trim()[8..].Trim(); // strip "VERDICT:"
        if (body.StartsWith("CONTRADICTS", StringComparison.OrdinalIgnoreCase))
        {
            var violated = body.Length > 11 ? body[11..].Trim(" :-".ToCharArray()) : null;
            return new ContradictionVerdict(true, string.IsNullOrWhiteSpace(violated) ? null : violated, reasoning);
        }
        return new ContradictionVerdict(false, null, reasoning);
    }
}
