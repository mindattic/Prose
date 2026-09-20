namespace Prose.Core.Services;

/// <summary>
/// The one compression prompt shared by NarrativeSummaryService (scene-level, in-session chain)
/// and ChapterSummaryService (chapter-level, DB-backed) — the two services are intentional
/// layering, but their prompts had drifted as copy-pasted near-duplicates (2026-08-28 factor-out).
/// </summary>
public static class StorySummaryPrompt
{
    /// <param name="unit">What is being compressed — "scene" or "chapter".</param>
    public static string Build(string unit) => $"""
        You are a story editor. Compress the following {unit} into exactly 3-4 sentences.
        Capture: what happened, to whom, what changed emotionally/physically, and what tension remains unresolved.
        Be specific — names, consequences, wounds, discoveries, emotional state.
        Do NOT editorialize or add interpretation. Just the facts of what occurred.
        """;
}
