using Prose.Core.Interfaces;

namespace Prose.Core.Services.Contradiction;

/// <summary>
/// One fact about an on-screen entity, from either the state ledger or the continuity claims table.
/// </summary>
/// <param name="Predicate"><c>EntityStateEvent.AspectKey</c> or <c>ContinuityClaim.Predicate</c>,
/// depending on <paramref name="Source"/>.</param>
/// <remarks>
/// Moved here from <c>Prose.Core.Composition.Ledger</c> 2026-09-20 so v3 can reach the checker below. v4
/// still produces these — <c>StoryStateQuery</c> is unchanged apart from its <c>using</c> — and
/// v4 already references Prose.Core, so the dependency direction is unaffected.
/// </remarks>
public sealed record OnScreenFact(
    string EntityName, string Predicate, string Value, string Source, string? Snippet);

/// <summary>Verdict from one contradiction check. <see cref="Reasoning"/> is written FIRST by the
/// model, <see cref="Contradicts"/> is parsed from the LAST line — this was measured
/// (RFC 0013, docs/LEDGER.md §3.1): a verdict emitted before its reasoning lets the model argue
/// against its own boolean roughly one time in ten; asking for reasoning first and parsing the
/// verdict last is the fix, applied here from the start rather than discovered the hard way again.</summary>
public sealed record ContradictionVerdict(bool Contradicts, string? ViolatedFact, string Reasoning);

/// <summary>
/// Does this prose contradict what it was told was true?
///
/// <para><b>Deliberately NOT an <see cref="Prose.Core.Services.WriteGate.IWriteGateSyncCheck"/></b>
/// — that interface's own doc forbids LLM calls ("must be cheap… slow judgment-based checks belong
/// in IWriteAuditService instead"), and this runs one by design. It is a plain step in a caller's
/// own pipeline, before the ordinary save path, which still runs the real sync gates unmodified.</para>
///
/// <para><b>Scoped narrow and cheap on purpose.</b> The literature review behind v4 found even the
/// best full-book auto-verifiers cap out around F1 ~0.5 on unfaithful-claim detection — full
/// omniscience is not the target. This compares ONLY the prose in front of it against the specific
/// <see cref="OnScreenFact"/> rows it is handed: not the whole book, not inferred canon, just "did
/// you contradict what you were just told is true right now". One call, temperature 0, 400 output
/// tokens, and no call at all when there are no facts.</para>
///
/// <para><b>Calibrated, not assumed.</b> Seven of seven on <see cref="CalibrationFixtures"/>
/// including the volatile-predicate case. An instrument's findings are worthless until the
/// instrument has been measured, and this one has been.</para>
///
/// <para><b>Fails OPEN</b> — a reply with no VERDICT line is read as consistent. That is the
/// opposite of <c>ContinuityEnforcer</c>'s fail-closed policy and it is deliberate: this check sits
/// in front of the author's own typing, and a parse failure must not block their work.</para>
///
/// <para>Moved here from the old <c>Prose.V4.Core</c> project 2026-09-20, back when that project
/// was separate: this was the best-value check in the codebase and the rest of the engine could not
/// reach it, because the dependency only ran one way. Moved rather than copied — a second copy of a
/// calibrated checker is a second thing to re-calibrate, and the two would drift apart silently.
/// The former v4 tree has since been folded into this project under <c>Composition/</c>, so the
/// one-way dependency that forced this move no longer exists.</para>
/// </summary>
public sealed class NarrativeContradictionChecker(ILlmService llm)
{
    public async Task<ContradictionVerdict> CheckAsync(
        IReadOnlyList<OnScreenFact> facts, string beatText, CancellationToken ct = default)
    {
        if (facts.Count == 0)
            return new ContradictionVerdict(false, null,
                "No established facts were given for this beat — nothing to contradict.");

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

    public static ContradictionVerdict Parse(string raw)
    {
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var verdictLine = lines.LastOrDefault(l => l.TrimStart().StartsWith("VERDICT:", StringComparison.OrdinalIgnoreCase));
        var reasoning = verdictLine == null ? raw.Trim() : raw[..raw.LastIndexOf(verdictLine, StringComparison.Ordinal)].Trim();

        if (verdictLine == null)
            return new ContradictionVerdict(false, null,
                reasoning + "\n(no VERDICT line found — treated as CONSISTENT, fail-open by design)");

        var body = verdictLine.Trim()[8..].Trim(); // strip "VERDICT:"
        if (body.StartsWith("CONTRADICTS", StringComparison.OrdinalIgnoreCase))
        {
            var violated = body.Length > 11 ? body[11..].Trim(" :-".ToCharArray()) : null;
            return new ContradictionVerdict(true, string.IsNullOrWhiteSpace(violated) ? null : violated, reasoning);
        }
        return new ContradictionVerdict(false, null, reasoning);
    }
}
