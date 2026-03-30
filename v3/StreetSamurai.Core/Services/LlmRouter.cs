using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Routes LLM calls to the active provider based on settings.
/// Supports runtime provider switching without restarting the app.
/// </summary>
public class LlmRouter : ILlmService
{
    private readonly ClaudeService _claude;
    private readonly OpenAiService _openAi;
    private readonly SettingsService _settings;

    public LlmRouter(ClaudeService claude, OpenAiService openAi, SettingsService settings)
    {
        _claude = claude;
        _openAi = openAi;
        _settings = settings;
    }

    public Task<bool> IsConfiguredAsync() => GetActiveProvider().IsConfiguredAsync();

    public Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
        => GetActiveProvider().GenerateAsync(system, user, temperature, maxTokens, model, ct);

    private ILlmService GetActiveProvider() => _settings.ActiveLlmProvider switch
    {
        "openai" => _openAi,
        _ => _claude, // default to Claude
    };

    /// <summary>
    /// Returns all configured providers and their status.
    /// </summary>
    public async Task<List<LlmProviderStatus>> GetProvidersAsync()
    {
        return
        [
            new() { Id = "claude", Name = "Claude (Anthropic)", IsConfigured = await _claude.IsConfiguredAsync(), IsActive = _settings.ActiveLlmProvider != "openai" },
            new() { Id = "openai", Name = "OpenAI", IsConfigured = await _openAi.IsConfiguredAsync(), IsActive = _settings.ActiveLlmProvider == "openai" },
        ];
    }
}

public record LlmProviderStatus
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsConfigured { get; init; }
    public bool IsActive { get; init; }
}
