using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Maintains a compressed scene-to-scene memory chain. After each scene,
/// generates a 3-4 sentence summary. The next scene gets the summary chain
/// instead of the full text — enabling long-form coherence without burning
/// context tokens.
/// </summary>
public class NarrativeSummaryService
{
    private readonly ILlmService llm;

    // Running chain of summaries — acts as the story's short-term memory
    private readonly List<string> summaryChain = [];

    public NarrativeSummaryService(ILlmService llm)
    {
        this.llm = llm;
    }

    /// <summary>The full summary chain formatted for injection into the next scene's prompt.</summary>
    public string GetSummaryChain()
    {
        if (summaryChain.Count == 0) return "";

        // Keep last 10 summaries to prevent unbounded growth
        var recent = summaryChain.Count > 10
            ? summaryChain.Skip(summaryChain.Count - 10).ToList()
            : summaryChain;

        return "STORY SO FAR (compressed summaries of previous scenes):\n"
            + string.Join("\n", recent.Select((s, i) => $"Scene {i + 1}: {s}"));
    }

    /// <summary>Compress a completed scene into a brief summary and add to the chain.</summary>
    public async Task SummarizeSceneAsync(string sceneText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sceneText)) return;

        var system = """
            You are a story editor. Compress the following scene into exactly 3-4 sentences.
            Capture: what happened, to whom, what changed, and what tension remains.
            Be specific — names, consequences, emotional state.
            Do NOT editorialize or add interpretation. Just the facts of what occurred.
            """;

        var summary = await llm.GenerateAsync(system, sceneText, 0.3, 256, model: LlmModels.Haiku, ct: ct);
        summaryChain.Add(summary.Trim());
    }

    /// <summary>Clear the chain (for new story).</summary>
    public void Reset() => summaryChain.Clear();

    /// <summary>Get the number of scenes summarized.</summary>
    public int SceneCount => summaryChain.Count;
}
