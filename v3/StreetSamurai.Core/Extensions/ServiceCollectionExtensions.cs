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
        services.AddSingleton<ICanonPathProvider, FileSystemCanonPathProvider>();
        services.AddSingleton<CanonService>();
        services.AddSingleton<YamlService>();
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

        // LLM services
        services.AddHttpClient<ClaudeService>();
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<ClaudeService>());
        services.AddSingleton<TextAnalysisService>();
        services.AddSingleton<ContextAnalyzerService>();
        services.AddSingleton<BeatGeneratorService>();
        services.AddSingleton<SceneGenerationService>();

        return services;
    }
}
