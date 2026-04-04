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
        services.AddSingleton<IPathProvider, FileSystemPathProvider>();
        // Typed JSON repositories — one file per entity type
        services.AddSingleton<CharacterRepository>();
        services.AddSingleton<CorponationRepository>();
        services.AddSingleton<DistrictRepository>();
        services.AddSingleton<FactionRepository>();
        services.AddSingleton<FacetRepository>();
        services.AddSingleton<WorldbuildingDocRepository>();
        services.AddSingleton<WeaponryRepository>();
        services.AddSingleton<AmmunitionRepository>();
        services.AddSingleton<EquipmentRepository>();
        services.AddSingleton<TechnologyRepository>();
        services.AddSingleton<CyberwareRepository>();
        services.AddSingleton<VocabularyRepository>();
        services.AddSingleton<MotifRepository>();
        services.AddSingleton<StoryBibleRepository>();
        services.AddSingleton<LiteraryRulesRepository>();
        services.AddSingleton<CharacterProfileRepository>();

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<LoreService>();
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<StoryService>();
        services.AddSingleton<IStoryBlockRepository, JsonStoryBlockRepository>();
        services.AddSingleton<FacetService>();
        // Graph builds from canon.json on first access
        services.AddSingleton<WorldGraphService>(sp =>
        {
            var graph = new WorldGraphService(
                sp.GetRequiredService<IPathProvider>(),
                sp.GetRequiredService<DatabaseService>());
            graph.EnsureLoaded();
            return graph;
        });

        // Semantic search — TF-IDF index over all graph entities
        services.AddSingleton<SemanticIndexService>(sp =>
        {
            var idx = new SemanticIndexService(sp.GetRequiredService<WorldGraphService>());
            idx.RebuildIndex();
            return idx;
        });

        // Cross-entity inference — transitive relationships via shared hubs/properties
        services.AddSingleton<InferenceService>(sp =>
        {
            var inf = new InferenceService(sp.GetRequiredService<WorldGraphService>());
            inf.RebuildPropertyIndex();
            return inf;
        });

        // Automatic relationship discovery — scans entity saves for new edges
        services.AddSingleton<RelationshipDiscoveryService>(sp =>
        {
            var discovery = new RelationshipDiscoveryService(
                sp.GetRequiredService<WorldGraphService>(),
                sp.GetRequiredService<SemanticIndexService>(),
                sp.GetRequiredService<InferenceService>());

            // Wire repository save events to auto-discover relationships
            sp.GetRequiredService<CharacterRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "character");
            sp.GetRequiredService<CorponationRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "organization");
            sp.GetRequiredService<DistrictRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "place");
            sp.GetRequiredService<FactionRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "faction");
            sp.GetRequiredService<WeaponryRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "weapon");
            sp.GetRequiredService<EquipmentRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "equipment");
            sp.GetRequiredService<TechnologyRepository>().OnItemSaved += name => discovery.DiscoverFromEntity(name, "technology");

            return discovery;
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

        // Canon validation — checks generated text against graph for contradictions
        services.AddSingleton<ValidationService>();

        // Narrative intelligence — story model layer
        services.AddSingleton<StoryStateService>();
        services.AddSingleton<EventLogService>();
        services.AddSingleton<OutlineService>();
        services.AddSingleton<AgendaEngine>();
        services.AddSingleton<KnowledgeMapService>();

        // Scene generation pipeline
        services.AddSingleton<TextAnalysisService>();
        services.AddSingleton<ContextAnalyzerService>();
        services.AddSingleton<BeatGeneratorService>();
        services.AddSingleton<SceneGenerationService>();
        services.AddSingleton<StoryStarterService>();

        // Story director — autonomous story generation
        services.AddSingleton<StoryDirectorService>();

        // Geographic navigation, pathfinding, and dynamic place generation
        services.AddSingleton<NavigationService>();
        services.AddSingleton<DynamicPlaceGenerator>();

        // Freelancer story systems
        services.AddSingleton<ContractGenerator>();
        services.AddSingleton<NpcGenerator>();
        services.AddSingleton<RandomEncounterService>();
        services.AddSingleton<ReputationTracker>();
        services.AddSingleton<ConsequenceEngine>();

        return services;
    }
}
