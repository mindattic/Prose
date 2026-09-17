using Prose.Core.Interfaces;
using Prose.V4.Core.Ledger;
using Prose.V4.Core.Prompt;
using Prose.V4.Core.Window;

namespace Prose.V4.Core.Orchestration;

/// <summary>Result of a preview generation — deliberately carries the assembled prompt back so a
/// caller (or a human) can inspect exactly what the model saw, not just what it produced.</summary>
public sealed record PreviewResult(AssembledPrompt Prompt, string GeneratedText);

/// <summary>
/// Phase 1 of the v4 plan: windowed generation, no write gate yet, NO DATABASE WRITES. This is
/// deliberately preview-only — it assembles the ≤8-block prompt (<see cref="PromptAssembler"/>)
/// from a real sliding window (<see cref="SceneWindowService"/>) and real on-screen facts
/// (<see cref="StoryStateQuery"/>), calls the LLM once, and returns the result for inspection. It
/// never calls <c>NodeWorkbenchService.UpdateBeatTextAsync</c> or any other save path — proving the
/// windowing fix works does not require writing a single word into the user's actual book.
/// </summary>
public sealed class BeatWriteOrchestrator
{
    private readonly SceneWindowService window;
    private readonly StoryStateQuery storyState;
    private readonly ILlmService llm;

    public BeatWriteOrchestrator(SceneWindowService window, StoryStateQuery storyState, ILlmService llm)
    {
        this.window = window;
        this.storyState = storyState;
        this.llm = llm;
    }

    public async Task<PreviewResult> PreviewGenerateAsync(
        Guid bookNodeId,
        Guid afterBeatId,
        IReadOnlyList<Guid> charactersInScene,
        int asOfStoryPosition,
        string povCharacter,
        string location,
        string beatGoal,
        string universeLine,
        int windowSizeBeats = 15,
        CancellationToken ct = default)
    {
        var win = await this.window.GetWindowAsync(bookNodeId, afterBeatId, windowSizeBeats, ct);
        var facts = await storyState.GetOnScreenFactsForEntitiesAsync(bookNodeId, charactersInScene, asOfStoryPosition, ct);
        var prompt = PromptAssembler.Assemble(universeLine, win, facts, povCharacter, location, beatGoal);

        var generated = await llm.GenerateAsync(prompt.System, prompt.User, temperature: 0.8, maxTokens: 1200, ct: ct);
        return new PreviewResult(prompt, generated);
    }
}
