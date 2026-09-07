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

    /// <summary>
    /// The author's own editorial standard, stated 2026-09-07: *"the rules are A) the rule of cool
    /// B) learn something new about this world every beat"*, applied to three versions of one beat
    /// with the instruction *"make it into one version, the best version of itself"*. The spine
    /// rule is the author's too — the existing book is the base, so v2 is the same story dolled up
    /// rather than a different novel.
    /// </summary>
    private const string SystemPrompt = """
        You are the author's editor on a cyberpunk novel. You are given one beat as it stands in the
        book (THE BOOK) and independent regenerations of the same beat (CANDIDATES). Make them into
        one version — the best version of itself.

        THE BOOK IS THE SPINE. Everything the rest of the novel refers back to survives unchanged:
        every event and the order it happens in, the ending, every number and measurement, every
        name, and the information carried in dialogue. Where a candidate contradicts the book on any
        of those, the book is right — drop that part silently. You are making this beat better, not
        making a different beat.

        Inside that frame you are held to two rules.

        RULE ONE — THE RULE OF COOL. The best version of a moment is the one a reader stops and
        re-reads. Take the sharpest verb, the most physical image, the line of dialogue that lands
        hardest, whichever version it came from. Where the book states flatly something a candidate
        dramatizes, use the dramatization. Where the book cuts away from something its own text says
        happened — a repair, the middle of a fight, a decision made in silence — stage it on the
        page. Replace anything that is merely competent.

        RULE TWO — EVERY BEAT TEACHES THE READER SOMETHING NEW ABOUT THIS WORLD. Not a lecture: a
        working detail, revealed by what someone does or already knows. How a thing is priced,
        queued, repaired, policed, worn, smuggled, or worked around, and what it costs the person
        doing it. Extend what is already established rather than inventing beside it — a thing the
        book already names can be given a new mechanism, price, custom or consequence. Best is a
        detail this beat's own action requires in order to work.

        WHAT YOU MAY NOT ADD: a plot event. No arrival, no message carrying information, no fight,
        no injury, no reversal, no decision the beat did not already make. Those belong to other
        beats and the next chapter is written assuming they did not happen here.

        Voice: the book's, never smoothed toward neutral. Keep its paragraph rhythm and its POV.
        Length: up to about a third longer than the book's where the new material earns it. Not
        materially shorter.
        Output: the beat's prose only. No heading, no title, no label, no commentary.
        """;

    public async Task<string> MergeAsync(
        string originalText,
        IReadOnlyList<Candidate> candidates,
        string? beatGoal,
        IReadOnlyList<string>? constraints = null,
        string? model = null,
        BeatBrief? brief = null,
        CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(beatGoal))
            sb.Append("WHAT THIS BEAT IS FOR (context only — the book's text is what happened):\n").AppendLine(beatGoal.Trim()).AppendLine();

        // Where this beat ends. Without it the merge inherits the candidates' habit of running on
        // into the next beat's material, and the gate then refuses a merge that was otherwise good
        // (2 of the first 4 mid-book beats, 2026-09-07).
        if (!string.IsNullOrWhiteSpace(brief?.StopBefore))
            sb.Append("WHERE THIS BEAT ENDS: the NEXT beat of the book does this — ").Append(brief.StopBefore.Trim())
              .AppendLine(" — so your version must stop before it, exactly where the book's version stops. Do not narrate it, do not set it up with a new event.").AppendLine();
        else if (brief?.ClosesChapter == true)
            sb.AppendLine("WHERE THIS BEAT ENDS: this beat closes its chapter. Land it where the book lands it.").AppendLine();

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

        sb.AppendLine("Take these and make them into one version, the best version of itself. Prose only.");

        var raw = await llm.GenerateAsync(SystemPrompt, sb.ToString(), temperature: 0.4, maxTokens: 4096,
            model: model ?? LlmModels.Opus, ct: ct);
        return DraftPostProcessor.Clean(raw).Text;
    }
}
