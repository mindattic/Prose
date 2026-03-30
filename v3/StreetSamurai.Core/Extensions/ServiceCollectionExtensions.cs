using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreetSamuraiServices(this IServiceCollection services)
    {
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ICanonPathProvider, FileSystemCanonPathProvider>();
        services.AddSingleton<CanonService>();
        services.AddSingleton<YamlService>();
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<StoryService>();
        services.AddSingleton<WorldGraphService>();
        services.AddSingleton<FacetService>();
        services.AddSingleton<TextAnalysisService>();
        services.AddSingleton<CanonQueueService>();
        services.AddSingleton<SceneGenerationService>();
        services.AddSingleton<ContextAnalyzerService>();
        services.AddSingleton<BeatGeneratorService>();
        services.AddHttpClient<ClaudeService>();
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<ClaudeService>());
        return services;
    }
}
