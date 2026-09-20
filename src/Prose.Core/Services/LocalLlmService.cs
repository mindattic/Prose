using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Routes prose generation to a self-hosted OpenAI-compatible endpoint (Ollama, vLLM,
/// RunPod, or any local GPU). Wire transport goes through <see cref="LegionClient"/>
/// using the explicit-URL overload — no catalog registration required.
///
/// <para>Connection info is read from <see cref="SettingsService"/> by default.
/// Call <see cref="ConfigureForRun"/> before generation to override for a single CLI
/// invocation without persisting changes to settings.</para>
/// </summary>
public class LocalLlmService : ILlmService
{
    private readonly LegionClient legion;
    private readonly SettingsService settings;
    private readonly ILogger<LocalLlmService> log;

    private string? runUrl;
    private string? runKey;
    private string? runModel;

    public LocalLlmService(LegionClient legion, SettingsService settings, ILogger<LocalLlmService> log)
    {
        this.legion   = legion;
        this.settings = settings;
        this.log      = log;
    }

    /// <summary>
    /// Overrides connection info for the lifetime of the current process (not persisted).
    /// Pass <c>null</c> for any value to fall back to the matching <see cref="SettingsService"/> property.
    /// </summary>
    public void ConfigureForRun(string? baseUrl, string? apiKey, string? model)
    {
        runUrl   = baseUrl;
        runKey   = apiKey;
        runModel = model;
    }

    public Task<bool> IsConfiguredAsync()
        => Task.FromResult(!string.IsNullOrWhiteSpace(runUrl ?? settings.LocalLlmBaseUrl));

    public async Task<string> GenerateAsync(
        string system,
        string user,
        double temperature = 0.8,
        int maxTokens = 4096,
        string? model = null,
        CancellationToken ct = default)
    {
        var url = runUrl ?? settings.LocalLlmBaseUrl;
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                "Local LLM endpoint not configured. Set LocalLlmBaseUrl in settings or pass --local-url.");

        var key          = runKey ?? settings.LocalLlmApiKey;
        var resolvedModel = model ?? runModel ?? settings.LocalLlmModel;

        log.LogDebug("LocalLlm request via Legion: url={Url}, model={Model}, maxTokens={MaxTokens}, temp={Temperature}",
            url, resolvedModel, maxTokens, temperature);

        try
        {
            var text = (await legion.CallAsync(
                providerId:   "local",
                apiKey:       key,
                model:        resolvedModel,
                systemPrompt: system,
                userMessage:  user,
                endpointUrl:  url,
                maxTokens:    maxTokens,
                temperature:  temperature,
                ct:           ct)).Trim();

            log.LogInformation("LocalLlm response: model={Model}, responseLen={ResponseLen}", resolvedModel, text.Length);
            return text;
        }
        catch (CircuitBreakerOpenException ex)
        {
            log.LogWarning("LocalLlm circuit breaker open: {Message}", ex.Message);
            throw;
        }
        catch (HttpRequestException ex)
        {
            log.LogError(ex, "LocalLlm HTTP request failed: url={Url}, model={Model}, status={Status}", url, resolvedModel, ex.StatusCode);
            throw;
        }
    }
}
