using Prose.Core.Composition.Ledger;
using Prose.Core.Composition.Window;
using Prose.Core.Services;

namespace Prose.Core.Composition.Prompt;

/// <summary>Result of assembling a beat-write prompt: the system/user strings actually sent to the
/// LLM, plus each block's character length — the diagnostic this whole rebuild exists to make
/// trustworthy (v3's version of this number required two log lines added mid-session and a Hub
/// redeploy just to see; here it is the return value).</summary>
public sealed record AssembledPrompt(string System, string User, IReadOnlyDictionary<string, int> BlockLengths);

/// <summary>
/// The beat-write prompt, assembled as a contract with context around it rather than context with
/// an instruction at the end of it.
///
/// <para><b>What this used to do, and why it produced word salad.</b> The blocks were joined
/// facts → obligations → scene → window → goal, so the instruction was last, appeared once, and on
/// a real BCODA5 beat came to 219 characters inside a 42,612-character prompt — 0.5%. The model
/// was given overwhelmingly more evidence about what the book sounds like than about what this
/// beat had to do, so it wrote atmosphere at length: it invented a character and a vehicle, emitted
/// a markdown heading and a scene break, explained its own subtext, and ran three times the length
/// of the beat it was replacing.</para>
///
/// <para><b>Every one of those is a rule that already existed.</b> RFC 0012 diagnosed exactly this
/// on beat #17292 ("the brief is one line at the end of a prompt that has already said everything
/// else") and built <see cref="BeatBrief"/> in answer: NO NEW NAMES, NO NEW PLOT, OUTPUT prose
/// only, STOP BEFORE, LENGTH. This assembler simply did not use it. It does now — rendered FIRST,
/// and restated as the final line via <see cref="BeatBrief.ToClosingLine"/>, so the job can be
/// crowded out neither by the context above it nor by the model's own momentum.</para>
///
/// <para><b>Ceilings are in characters, not items.</b> The window was previously bounded by beat
/// COUNT (15), which on a book whose beats run ~2,700 characters is 40KB of prompt. A count is not
/// a size. The tiers below are RFC 0012 §3.2's: Facts 6,000 · Memory 8,000 · Voice 3,000. Trimming
/// keeps the END of the window (the prose nearest this beat) and the START of the other blocks.</para>
/// </summary>
public static class PromptAssembler
{
    /// <summary>RFC 0012 §3.2 tier B — established facts.</summary>
    public const int FactsCeiling = 6_000;
    /// <summary>RFC 0012 §3.2 tier C — the window of prior prose.</summary>
    public const int WindowCeiling = 8_000;
    /// <summary>RFC 0012 §3.2 tier D — voice/style exemplars.</summary>
    public const int VoiceCeiling = 3_000;
    /// <summary>The outline slice and open obligations are short by construction; this is a
    /// backstop against a pathological book, not a working limit.</summary>
    public const int SpineCeiling = 2_000;

    /// <summary>Keep the tail — for prior prose, the lines nearest this beat matter most.</summary>
    private static string ClampTail(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s[^max..];

    /// <summary>Keep the head — for lists, the first entries are the ones deliberately ordered first.</summary>
    private static string ClampHead(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s[..max];

    public static AssembledPrompt Assemble(
        string universeLine,
        IReadOnlyList<WindowedBeat> window,
        OnScreenSnapshot facts,
        string povCharacter,
        string location,
        string beatGoal,
        BeatBrief? brief = null,
        string voiceAnchorBlock = "",
        string outlineSpineBlock = "")
    {
        var lengths = new Dictionary<string, int>();

        string WindowBlock()
        {
            if (window.Count == 0) return "";
            var chapters = window.Select(w => w.ChapterTitle).Distinct().ToList();
            var header = chapters.Count <= 1
                ? $"SCENE SO FAR ({chapters.FirstOrDefault()}):"
                : $"SCENE SO FAR (spans {chapters[0]} .. {chapters[^1]}):";
            var body = ClampTail(string.Join("\n\n", window.Select(w => w.Text)), WindowCeiling);
            return header + "\n\n" + body;
        }

        string FactsBlock()
        {
            if (facts.IsEmpty) return "";
            var lines = new List<string> { "ESTABLISHED FACTS — treat as hard constraints, do not contradict:" };
            foreach (var f in facts.EntityStateFacts) lines.Add($"  {f.EntityName} — {f.Predicate}: {f.Value}");
            foreach (var f in facts.ContinuityFacts) lines.Add($"  {f.EntityName} — {f.Predicate}: {f.Value}");
            return ClampHead(string.Join("\n", lines), FactsCeiling);
        }

        string ObligationsBlock()
        {
            if (facts.OpenObligations.Count == 0) return "";
            var lines = new List<string> { "OPEN OBLIGATIONS — promises this story already made and hasn't paid off:" };
            foreach (var o in facts.OpenObligations) lines.Add($"  [{o.Kind}] {o.Description}");
            return ClampHead(string.Join("\n", lines), SpineCeiling);
        }

        string SceneBlock()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(povCharacter)) parts.Add($"POV: {povCharacter}");
            if (!string.IsNullOrWhiteSpace(location)) parts.Add($"LOCATION: {location}");
            return string.Join("\n", parts);
        }

        // The brief is the job. Without one this falls back to the bare goal line — which is the
        // shape that produced the word salad, so it is a fallback, never the intended path.
        var briefBlock = brief?.ToPromptBlock() ?? $"BEAT GOAL: {beatGoal}";
        var closingLine = brief?.ToClosingLine()
                          ?? $"Write exactly this beat: {beatGoal} Prose only — no heading, no label.";

        var windowBlock = WindowBlock();
        var factsBlock = FactsBlock();
        var obligationsBlock = ObligationsBlock();
        var sceneBlock = SceneBlock();
        var voiceBlock = ClampTail(voiceAnchorBlock ?? "", VoiceCeiling);
        var spineBlock = ClampHead(outlineSpineBlock ?? "", SpineCeiling);

        lengths["brief"] = briefBlock.Length;
        lengths["facts"] = factsBlock.Length;
        lengths["obligations"] = obligationsBlock.Length;
        lengths["spine"] = spineBlock.Length;
        lengths["scene"] = sceneBlock.Length;
        lengths["voice"] = voiceBlock.Length;
        lengths["window"] = windowBlock.Length;
        lengths["closing"] = closingLine.Length;

        // Order is the point. Brief first; the prose nearest this beat last before the restatement,
        // so the model's final reading before it writes is what just happened and what it must do.
        var user = string.Join("\n\n", new[]
            {
                briefBlock, factsBlock, obligationsBlock, spineBlock, sceneBlock, voiceBlock,
                windowBlock, closingLine,
            }
            .Where(b => !string.IsNullOrWhiteSpace(b)));

        return new AssembledPrompt(universeLine, user, lengths);
    }
}
