namespace Prose.Core.Services;

/// <summary>
/// In-memory ring buffer of the last N prompts dispatched through <see cref="LlmRouter"/>.
/// Lets the SystemAtlas diagnostic page show what the LLM actually saw, so the user can
/// verify whether entity/canon context is being filtered by relevance or dumped wholesale.
/// </summary>
public class LastPromptStore
{
    private readonly object gate = new();
    private readonly LinkedList<CapturedPrompt> entries = new();

    public int Capacity { get; set; } = 20;

    public void Capture(string provider, string model, double temperature, int maxTokens, string system, string user, string? response = null, int? elapsedMs = null)
    {
        lock (gate)
        {
            entries.AddFirst(new CapturedPrompt(
                At: DateTime.Now,
                Provider: provider,
                Model: model,
                Temperature: temperature,
                MaxTokens: maxTokens,
                System: system,
                User: user,
                Response: response,
                ElapsedMs: elapsedMs));
            while (entries.Count > Capacity) entries.RemoveLast();
        }
    }

    public IReadOnlyList<CapturedPrompt> Snapshot()
    {
        lock (gate) { return entries.ToList(); }
    }

    public void Clear()
    {
        lock (gate) { entries.Clear(); }
    }
}

public record CapturedPrompt(
    DateTime At,
    string Provider,
    string Model,
    double Temperature,
    int MaxTokens,
    string System,
    string User,
    string? Response,
    int? ElapsedMs);
