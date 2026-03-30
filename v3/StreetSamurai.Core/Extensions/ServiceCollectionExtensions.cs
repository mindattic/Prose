using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreetSamuraiServices(this IServiceCollection services)
    {
        // Settings auto-detects canon root path on first run
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ISecurePreferences, FileSecurePreferences>();
        services.AddSingleton<ICanonPathProvider, FileSystemCanonPathProvider>();
        services.AddSingleton<YamlService>();
        services.AddSingleton<CanonService>();
        services.AddSingleton<CanonDatabaseService>();
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<StoryService>();
        services.AddSingleton<FacetService>();
        services.AddSingleton<CanonQueueService>();

        // Graph builds from YAML on first access
        services.AddSingleton<WorldGraphService>(sp =>
        {
            var graph = new WorldGraphService(
                sp.GetRequiredService<ICanonPathProvider>(),
                sp.GetRequiredService<YamlService>());
            graph.EnsureLoaded();
            return graph;
        });

        // LLM services — multi-provider with router
        services.AddHttpClient<ClaudeService>();
        services.AddHttpClient<OpenAiService>();
        services.AddSingleton<LlmRouter>(sp => new LlmRouter(
            sp.GetRequiredService<ClaudeService>(),
            sp.GetRequiredService<OpenAiService>(),
            sp.GetRequiredService<SettingsService>()));
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<LlmRouter>());

        // TTS service
        services.AddHttpClient<ElevenLabsTtsService>();
        services.AddSingleton<ITtsService>(sp => sp.GetRequiredService<ElevenLabsTtsService>());

        // Scene generation pipeline
        services.AddSingleton<TextAnalysisService>();
        services.AddSingleton<ContextAnalyzerService>();
        services.AddSingleton<BeatGeneratorService>();
        services.AddSingleton<SceneGenerationService>();
        services.AddSingleton<StoryStarterService>();

        return services;
    }
}
