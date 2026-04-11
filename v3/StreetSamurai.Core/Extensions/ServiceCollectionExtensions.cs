using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreetSamuraiServices(this IServiceCollection services)
    {
        // Application logging — reads daily Serilog log files for the UI viewer
        services.AddSingleton<LoggingService>();

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
        services.AddSingleton<SyntheticLifeRepository>();
        services.AddSingleton<GenemodRepository>();
        services.AddSingleton<TransportationRepository>();
        services.AddSingleton<QuoteRepository>();
        services.AddSingleton<ContractRepository>();
        services.AddSingleton<NewsRepository>();
        services.AddSingleton<ArchetypeRepository>();
        services.AddSingleton<MaterialRepository>();
        services.AddSingleton<PharmaceuticalRepository>();
        services.AddSingleton<ConsumerGoodRepository>();
        services.AddSingleton<AutomatonRepository>();
        services.AddSingleton<ApparelRepository>();
        services.AddSingleton<SubsidiaryRepository>();
        services.AddSingleton<EntertainmentRepository>();
        services.AddSingleton<MotifRepository>();
        services.AddSingleton<ToneBibleRepository>();

        // Daily trivia — pre-generates 100 facts from canon data, cached to disk
        services.AddSingleton<TriviaService>();

        // Auto-register all directory repos as IExportableRepository for discovery
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<CharacterRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<CorponationRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<DistrictRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<FactionRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<WorldbuildingDocRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<WeaponryRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<AmmunitionRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<EquipmentRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<TechnologyRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<CyberwareRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<VocabularyRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<SyntheticLifeRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<GenemodRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<TransportationRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<QuoteRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<ContractRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<NewsRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<ArchetypeRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<MaterialRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<PharmaceuticalRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<ConsumerGoodRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<AutomatonRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<ApparelRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<SubsidiaryRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<EntertainmentRepository>());

        // Export discovery — auto-finds all IExportableRepository instances
        services.AddSingleton<ExportDiscoveryService>();
        services.AddSingleton<StoryBibleRepository>();
        services.AddSingleton<LiteraryRulesRepository>();
        services.AddSingleton<CharacterProfileRepository>();

        // User accounts and authentication
        services.AddSingleton<UserRepository>();
        services.AddSingleton<AuthService>();

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IDatabaseService>(sp => sp.GetRequiredService<DatabaseService>());
        services.AddSingleton<XrefService>();
        services.AddSingleton<GlobalSearchService>();
        services.AddSingleton<SearchTriggerService>();
        services.AddSingleton<LoreService>();
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<ViewModeService>();
        services.AddSingleton<FactDiscoveryService>();
        services.AddSingleton<SceneContextBuilder>();
        services.AddSingleton<ConsequenceService>();
        services.AddSingleton<AmbientAnomalyService>();
        services.AddSingleton<NarrativeSummaryService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<FtpPublishService>();
        services.AddSingleton<HtmlExportService>();
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

        services.AddSingleton<IWorldGraphService>(sp => sp.GetRequiredService<WorldGraphService>());

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
            sp.GetRequiredService<CharacterRepository>().OnItemSaved += name =>
            {
                discovery.DiscoverFromEntity(name, "character");
                // Also graph archetypes and belongings
                var charRepo = sp.GetRequiredService<CharacterRepository>();
                var character = charRepo.GetByName(name);
                if (character != null)
                    discovery.DiscoverFromCharacter(name, character.Archetypes, character.Belongings);
            };
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
            sp.GetRequiredService<SettingsService>(),
            sp.GetRequiredService<ILogger<LlmRouter>>()));
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<LlmRouter>());

        // TTS service
        services.AddHttpClient<ElevenLabsTtsService>();
        services.AddSingleton<ITtsService>(sp => sp.GetRequiredService<ElevenLabsTtsService>());

        // Audio file service
        services.AddSingleton<IAudioFileService, AudioFileService>();

        // Multi-LLM service — calls multiple providers for majority voting
        services.AddHttpClient<MultiLlmService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { CheckCertificateRevocationList = false });

        // TTS enhancement — adds ElevenLabs audio tags before synthesis
        services.AddSingleton<TtsEnhancementService>();

        // Draft narration — free Windows SAPI voices
        services.AddSingleton<WindowsTtsService>();

        // Entity extraction — LLM-powered story-to-graph pipeline
        services.AddSingleton<EntityExtractionService>();

        // Canon validation — checks generated text against graph for contradictions
        services.AddSingleton<ValidationService>();

        // Thematic index — tag-based cross-repo retrieval for story generation
        services.AddSingleton<ThematicIndexService>(sp =>
        {
            var idx = new ThematicIndexService(
                sp.GetRequiredService<DatabaseService>(),
                sp.GetRequiredService<SyntheticLifeRepository>(),
                sp.GetRequiredService<GenemodRepository>(),
                sp.GetRequiredService<TransportationRepository>(),
                sp.GetRequiredService<VocabularyRepository>(),
                sp.GetRequiredService<QuoteRepository>(),
                sp.GetRequiredService<ConsumerGoodRepository>(),
                sp.GetRequiredService<PharmaceuticalRepository>(),
                sp.GetRequiredService<MaterialRepository>(),
                sp.GetRequiredService<AmmunitionRepository>());
            idx.RebuildIndex();
            return idx;
        });

        // Crew assessment — grades team capability against contract requirements
        services.AddSingleton<CrewAssessmentService>();

        // Graph health analysis — orphan detection, bad node flagging
        services.AddSingleton<GraphHealthService>();

        // Character behavior prediction — psychological modeling
        services.AddSingleton<BehaviorPredictionService>();

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
        services.AddSingleton<IStoryDirectorService>(sp => sp.GetRequiredService<StoryDirectorService>());

        // Geographic navigation, pathfinding, and dynamic place generation
        services.AddSingleton<NavigationService>();
        services.AddSingleton<DynamicPlaceGenerator>();

        // Freelancer story systems
        services.AddSingleton<ContractGenerator>();
        services.AddSingleton<NpcGenerator>();
        services.AddSingleton<RandomEncounterService>();
        services.AddSingleton<ReputationTracker>();
        services.AddSingleton<ConsequenceEngine>();

        // Milestone 2 story engine services
        services.AddSingleton<DialogueService>();
        services.AddSingleton<ArcTrackerService>();
        services.AddSingleton<ContinuityValidatorService>();
        services.AddSingleton<SuggestionEngineService>();

        return services;
    }
}
