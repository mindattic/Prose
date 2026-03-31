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
        // Typed JSON repositories — one file per entity type
        services.AddSingleton<CharacterRepository>();
        services.AddSingleton<CorponationRepository>();
        services.AddSingleton<DistrictRepository>();
        services.AddSingleton<FactionRepository>();
        services.AddSingleton<FacetRepository>();
        services.AddSingleton<WorldbuildingDocRepository>();
        services.AddSingleton<WeaponryRepository>();
        services.AddSingleton<MotifRepository>();
        services.AddSingleton<StoryBibleRepository>();
        services.AddSingleton<LiteraryRulesRepository>();
        services.AddSingleton<CharacterProfileRepository>();

        services.AddSingleton<CanonDatabaseService>();
        services.AddSingleton<CanonService>();
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<StoryService>();
        services.AddSingleton<IStoryBlockRepository, JsonStoryBlockRepository>();
        services.AddSingleton<FacetService>();
        services.AddSingleton<CanonQueueService>();

        // Graph builds from canon.json on first access
        services.AddSingleton<WorldGraphService>(sp =>
        {
            var graph = new WorldGraphService(
                sp.GetRequiredService<ICanonPathProvider>(),
                sp.GetRequiredService<CanonDatabaseService>());
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

        // Audio file service
        services.AddSingleton<IAudioFileService, AudioFileService>();

        // Multi-LLM service — calls multiple providers for majority voting
        services.AddHttpClient<MultiLlmService>();

        // TTS enhancement — adds ElevenLabs audio tags before synthesis
        services.AddSingleton<TtsEnhancementService>();

        // Draft narration — free Windows SAPI voices
        services.AddSingleton<WindowsTtsService>();

        // Entity extraction — LLM-powered story-to-graph pipeline
        services.AddSingleton<EntityExtractionService>();

        // Scene generation pipeline
        services.AddSingleton<TextAnalysisService>();
        services.AddSingleton<ContextAnalyzerService>();
        services.AddSingleton<BeatGeneratorService>();
        services.AddSingleton<SceneGenerationService>();
        services.AddSingleton<StoryStarterService>();

        return services;
    }
}
