using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// One LLM call that produces the best version of a beat that ALREADY EXISTS, using the book's own
/// text as the spine and two independent regenerations as a quarry (RFC 0012 §11, author
/// instruction 2026-09-07: *"take the original, the lean and the full and then take the best"*).
///
/// <para><b>Why this is not a rewriter.</b> RFC 0009's law stands: an LLM may never rewrite prose
/// the author accepted on its own initiative. This runs only when the author invokes it, only on
/// the book they name, and its output is admitted only if <see cref="SpineCheck"/> proves every
/// number, name and event of the original survived AND the writer's gate passes. "Return the
/// original verbatim" is an explicitly allowed and expected answer — most beats need nothing.</para>
/// </summary>
public class BeatMergeService
{
    private readonly ILlmService llm;

    public BeatMergeService(ILlmService llm) { this.llm = llm; }

    public sealed record Candidate(string Label, string Text, bool Refused, IReadOnlyList<string> RefusalReasons);

    private const string SystemPrompt = """
        You are a novelist's editor working on a finished book. You are given one beat as it stands
        in the book (THE BOOK), and independent regenerations of the same beat written from the same
        brief (CANDIDATES). You produce the best version of this beat.

        THE BOOK IS THE SPINE. It is what the rest of the novel refers back to. Every fact, number,
        measurement, name, object, capability, event, and the ending, must survive into your version
        unchanged. If a candidate contradicts the book on any of these, the book is right and the
        candidate is wrong — silently discard it.

        THE CANDIDATES ARE A QUARRY, NOT EQUALS. Take from them only:
          - physical and sensory texture that grounds something the book states flatly;
          - a sharper verb, image, or rhythm for something the book already says;
          - a moment the book SKIPS OVER that the book's own text implies happened (a repair the
            book cuts away from, a beat of silence the book summarises).
        Never take from a candidate: a plot event, a named person / place / faction / product /
        weapon, a number, a capability, a relationship, or a change to who knows what.

        RETURNING THE BOOK'S TEXT VERBATIM IS A VALID AND COMMON ANSWER. If the candidates offer
        nothing the book lacks, return it exactly as given. Do not change prose to justify the call.
        Do not smooth the book's voice toward neutral. Keep its paragraph rhythm, its POV, its
        punctuation habits, and its dialogue exactly where dialogue carries information.

        Length: within about 25% of the book's.
        Output: the beat's prose, and nothing else. No heading, no title, no label, no commentary.
        """;

    public async Task<string> MergeAsync(
        string originalText,
        IReadOnlyList<Candidate> candidates,
        string? beatGoal,
        IReadOnlyList<string>? constraints = null,
        string? model = null,
        CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(beatGoal))
            sb.Append("WHAT THIS BEAT IS FOR (context only — the book's text is what happened):\n").AppendLine(beatGoal.Trim()).AppendLine();

        sb.AppendLine("THE BOOK — this beat as it stands. This is the spine.");
        sb.AppendLine("---");
        sb.AppendLine(SpineCheck.Strip(originalText).Trim());
        sb.AppendLine("---").AppendLine();

        foreach (var c in candidates.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
        {
            sb.Append("CANDIDATE ").Append(c.Label).AppendLine(c.Refused
                ? " — this one FAILED a check for inventing material. Its faults are listed below; take only its wording, never its events."
                : "");
            if (c.Refused && c.RefusalReasons.Count > 0)
                foreach (var r in c.RefusalReasons) sb.Append("  fault: ").AppendLine(r);
            sb.AppendLine("---");
            sb.AppendLine(SpineCheck.Strip(c.Text).Trim());
            sb.AppendLine("---").AppendLine();
        }

        if (constraints is { Count: > 0 })
        {
            sb.AppendLine("YOUR PREVIOUS ATTEMPT WAS REJECTED. Fix exactly these and change nothing else:");
            foreach (var c in constraints) sb.Append("  - ").AppendLine(c.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("Write the best version of this beat now. Prose only.");

        var raw = await llm.GenerateAsync(SystemPrompt, sb.ToString(), temperature: 0.4, maxTokens: 4096,
            model: model ?? LlmModels.Opus, ct: ct);
        return DraftPostProcessor.Clean(raw).Text;
    }
}
