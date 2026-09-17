using Prose.V4.Core.Ledger;
using Prose.V4.Core.Window;

namespace Prose.V4.Core.Prompt;

/// <summary>Result of assembling a beat-write prompt: the system/user strings actually sent to the
/// LLM, plus each block's character length — the Gate 0.1-style diagnostic this whole rebuild
/// exists to make trustworthy (v3's version of this number required two extra log lines added
/// mid-session and a Hub redeploy just to see; here it is the return value).</summary>
public sealed record AssembledPrompt(string System, string User, IReadOnlyDictionary<string, int> BlockLengths);

/// <summary>
/// The v4 plan's ≤8-block prompt (down from ProseWriterRouter's ~34 independently-drifting
/// stages). Six of the eight blocks are wired here in Phase 1: scene window, on-screen Story State
/// facts, open obligations, POV/scene mechanics, and the beat goal. Rolling recap and the outline
/// spine slice are NOT yet wired (recap ships alongside RollingRecap persistence; the outline spine
/// is Phase 4's OutlineSpineService) — Phase 1's own acceptance test only needs the window, so
/// this is deliberately not gold-plated ahead of that need. Voice/style anchor
/// (BuildBeatAnchorsAsync-equivalent) is also deferred; it's a "kept from v3" block, not a new one,
/// and can be wired in without changing this assembler's shape.
/// </summary>
public static class PromptAssembler
{
    public static AssembledPrompt Assemble(
        string universeLine,
        IReadOnlyList<WindowedBeat> window,
        OnScreenSnapshot facts,
        string povCharacter,
        string location,
        string beatGoal)
    {
        var lengths = new Dictionary<string, int>();

        string WindowBlock()
        {
            if (window.Count == 0) return "";
            var chapters = window.Select(w => w.ChapterTitle).Distinct().ToList();
            var header = chapters.Count <= 1
                ? $"SCENE SO FAR ({chapters.FirstOrDefault()}):"
                : $"SCENE SO FAR (spans {chapters[0]} .. {chapters[^1]}):";
            return header + "\n\n" + string.Join("\n\n", window.Select(w => w.Text));
        }

        string FactsBlock()
        {
            if (facts.IsEmpty) return "";
            var lines = new List<string> { "ESTABLISHED FACTS — treat as hard constraints, do not contradict:" };
            foreach (var f in facts.EntityStateFacts) lines.Add($"  {f.EntityName} — {f.Predicate}: {f.Value}");
            foreach (var f in facts.ContinuityFacts) lines.Add($"  {f.EntityName} — {f.Predicate}: {f.Value}");
            return string.Join("\n", lines);
        }

        string ObligationsBlock()
        {
            if (facts.OpenObligations.Count == 0) return "";
            var lines = new List<string> { "OPEN OBLIGATIONS — promises this story already made and hasn't paid off:" };
            foreach (var o in facts.OpenObligations) lines.Add($"  [{o.Kind}] {o.Description}");
            return string.Join("\n", lines);
        }

        string SceneBlock()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(povCharacter)) parts.Add($"POV: {povCharacter}");
            if (!string.IsNullOrWhiteSpace(location)) parts.Add($"LOCATION: {location}");
            return string.Join("\n", parts);
        }

        var windowBlock = WindowBlock();
        var factsBlock = FactsBlock();
        var obligationsBlock = ObligationsBlock();
        var sceneBlock = SceneBlock();
        var goalBlock = $"BEAT GOAL: {beatGoal}";

        lengths["window"] = windowBlock.Length;
        lengths["facts"] = factsBlock.Length;
        lengths["obligations"] = obligationsBlock.Length;
        lengths["scene"] = sceneBlock.Length;
        lengths["goal"] = goalBlock.Length;

        var user = string.Join("\n\n", new[] { factsBlock, obligationsBlock, sceneBlock, windowBlock, goalBlock }
            .Where(b => !string.IsNullOrWhiteSpace(b)));

        return new AssembledPrompt(universeLine, user, lengths);
    }
}
