using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// The contract one beat write is held to (RFC 0012 §3.1). Built by <see cref="BeatBriefBuilder"/>
/// from data that already exists (the beat's Description/Title/Subtext, the NEXT beat's
/// Description as the stop line, the entities the goal names, the POV row), rendered by
/// <see cref="BeatGeneratorService"/> as the FIRST and the LAST thing in the user message, and
/// enforced by <see cref="DraftGate"/> + <see cref="BriefVerifier"/> before anything is saved.
///
/// <para>Why a record and not a line: on beat #17292 (2026-09-07) the goal was one ~150-character
/// line two-thirds of the way through a ~23K-token prompt, and the writer ignored its ending and
/// invented a plot event. Nothing checked. The brief is the same information given structure,
/// position, and a check.</para>
/// </summary>
public sealed record BeatBrief
{
    /// <summary>What happens in this beat — the beat's Description (else Title).</summary>
    public required string Goal { get; init; }

    /// <summary>What the NEXT beat does. The draft must stop before this begins. Null = this is
    /// the last beat of the book (or nothing follows in the same book).</summary>
    public string? StopBefore { get; init; }

    /// <summary>True when this is the last beat of its chapter — the draft should land the
    /// chapter, not open the next one.</summary>
    public bool ClosesChapter { get; init; }

    /// <summary>Canon entity names the goal mentions; each must appear in the draft.</summary>
    public IReadOnlyList<string> MustInclude { get; init; } = [];

    /// <summary>POV character name when known from the outline POV map.</summary>
    public string? Pov { get; init; }

    /// <summary>Writer-only subtext (never printed).</summary>
    public string? Subtext { get; init; }

    /// <summary>0 = Swain scene/sequel default; otherwise the length target.</summary>
    public int TargetWords { get; init; }

    /// <summary>Draft temperature. Default 0.7 (was a hard-coded 0.85).</summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>Constraints appended after a failed gate — the verifier's reasons, phrased as
    /// prohibitions. Empty on the first attempt.</summary>
    public IReadOnlyList<string> Constraints { get; init; } = [];

    public BeatBrief WithConstraints(IEnumerable<string> more) =>
        this with { Constraints = [.. Constraints, .. more.Where(s => !string.IsNullOrWhiteSpace(s))] };

    /// <summary>The block that opens the user message.</summary>
    public string ToPromptBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine("THE BRIEF — this is the beat you are writing. Everything else in this prompt is context; this is the job.");
        sb.Append("  WHAT HAPPENS: ").AppendLine(Goal.Trim());
        if (!string.IsNullOrWhiteSpace(StopBefore))
            sb.Append("  STOP BEFORE: ").Append(StopBefore.Trim()).AppendLine("  ← the next beat does this. Do not write it. Do not set it up with a new event.");
        else if (ClosesChapter)
            sb.AppendLine("  STOP: this beat closes its chapter. Land it. Do not open a new thread.");
        else
            sb.AppendLine("  STOP: this is the last beat of the book. End the book here.");
        if (ClosesChapter && !string.IsNullOrWhiteSpace(StopBefore))
            sb.AppendLine("  NOTE: this beat also closes its chapter.");
        if (MustInclude.Count > 0)
            sb.Append("  MUST APPEAR (by name): ").AppendLine(string.Join(", ", MustInclude));
        if (!string.IsNullOrWhiteSpace(Pov))
            sb.Append("  POV: ").AppendLine(Pov);
        sb.AppendLine("  NO NEW PLOT: do not introduce any event, arrival, message, or reversal the brief does not imply.");
        sb.AppendLine("  NO NEW NAMES: do not invent a named person, place, faction, or product that is not in the context.");
        sb.AppendLine("  OUTPUT: prose only. No title, no heading, no label, no notes before or after.");
        if (TargetWords > 0)
            sb.Append("  LENGTH: about ").Append(TargetWords).AppendLine(" words.");
        if (Constraints.Count > 0)
        {
            sb.AppendLine("  THE FIRST ATTEMPT FAILED THE GATE. Fix exactly these:");
            foreach (var c in Constraints) sb.Append("    - ").AppendLine(c.Trim());
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>The line that closes the user message — the brief restated so it is the last
    /// thing the model reads.</summary>
    public string ToClosingLine()
    {
        var stop = !string.IsNullOrWhiteSpace(StopBefore)
            ? $"Stop before: {StopBefore.Trim()}"
            : ClosesChapter ? "Land the chapter here." : "End the book here.";
        return $"Write exactly the beat in THE BRIEF: {Goal.Trim()} {stop} Prose only — no heading, no label.";
    }
}
