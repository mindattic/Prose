using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using MindAttic.Legion;
using MindAttic.Legion.Providers;
using MindAttic.Media;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreetSamuraiServices(this IServiceCollection services)
    {
        // ── EF Core: SQL Server StreetSamurai database ──────────────────────────
        // Connection string priority: env var ConnectionStrings__StreetSamurai →
        // appsettings ConnectionStrings:StreetSamurai → LocalDB fallback.
        //
        // We register only AddDbContextFactory (singleton). Code that needs a
        // scoped DbContext takes IDbContextFactory and calls CreateDbContext()
        // — this keeps the factory consumable from singleton repositories without
        // hitting the "scoped DbContextOptions consumed by singleton factory"
        // validation error. A scoped DbContext registration below preserves
        // direct StreetSamuraiDbContext injection for callers that expect it.
        services.AddDbContextFactory<StreetSamuraiDbContext>((sp, opts) =>
        {
            var cfg = sp.GetService<IConfiguration>();
            var connStr =
                Environment.GetEnvironmentVariable("ConnectionStrings__StreetSamurai")
                ?? cfg?.GetConnectionString("StreetSamurai")
                ?? @"Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;";
            opts.UseSqlServer(connStr);
            // The multi-universe global query filters live on the Entity/Node/Book PRINCIPALS;
            // their dependents inherit the universe and intentionally carry no filter. EF logs a
            // "required end of a relationship" warning for each such navigation — expected and
            // correct here, so suppress the noise (it would fire on every model build).
            opts.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        }, ServiceLifetime.Singleton);
        services.AddScoped<StreetSamuraiDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>().CreateDbContext());

        // ── EF Core: MindAttic.Authentication identity tables (auth schema) ──────
        // SAME SQL database as StreetSamuraiDbContext (same connection-string
        // resolution chain). Registered SCOPED — the library's data seam is
        // AddScoped<IAuthDataContext>(sp => sp.GetRequiredService<StreetSamuraiAuthDbContext>()),
        // so a scoped resolve must exist. A SEPARATE context so the auth tables ride
        // clean EF migrations while the world tables stay on hand-written temporal SQL.
        services.AddDbContext<StreetSamuraiAuthDbContext>((sp, opts) =>
        {
            var cfg = sp.GetService<IConfiguration>();
            var connStr =
                Environment.GetEnvironmentVariable("ConnectionStrings__StreetSamurai")
                ?? cfg?.GetConnectionString("StreetSamurai")
                ?? @"Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;";
            opts.UseSqlServer(connStr);
        });
        // Idempotent legacy UserAccount → AuthUser migration (bcrypt carry +
        // upgrade-on-login). Scoped: depends on the scoped auth context.
        services.AddScoped<AuthUserImportService>();

        // Home-page stats cache: a singleton holding pre-computed entity
        // counts for the tile board + /board sub-tiles. Populated by the
        // background refresh service below. The request path reads from
        // memory; no SQL queries fire on each page load. See HomeStatsCache.cs
        // and HomeStatsRefreshService.cs for the contract.
        services.AddSingleton<HomeStatsCache>();
        services.AddHostedService<HomeStatsRefreshService>();

        // Application logging — reads daily Serilog log files for the UI viewer
        services.AddSingleton<LoggingService>();

        // Settings auto-detects canon root path on first run.
        // API keys route through MindAttic.Legion.MindAtticCredentialStore at
        // %APPDATA%/MindAttic/LLM/ — shared across every MindAttic app.
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ISecurePreferences, FileSecurePreferences>();
        services.AddSingleton<IPathProvider, FileSystemPathProvider>();
        // Audio bytes backend. Three modes via AudioStore:Provider:
        //
        //   "local"     (default)  — LocalDiskAudioStore. Files under
        //                            MutableDataDir/nodes/{slug}/audio/…
        //   "azureblob"             — AzureBlobAudioStore. Bytes in an Azure
        //                            Blob container; needs AudioStore:
        //                            ConnectionString + AudioStore:Container.
        //   "dual"                  — DualWriteAudioStore. Writes to BOTH
        //                            local + blob; reads local first, falls
        //                            back to blob. Designed for "Azure is a
        //                            replica, local is the cache" deployments
        //                            so recording works offline and bytes
        //                            survive when either side has trouble.
        //
        // Env-var fallbacks: AudioStore__Provider (and the per-backend keys
        // the individual stores read). The interface keeps NodeWorkbenchService
        // backend-agnostic — see IAudioStore docs.
        services.AddSingleton<IAudioStore>(sp =>
        {
            var config = sp.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var provider = config?["MindAttic:Vault:AudioStore:provider"]
                ?? config?["AudioStore:Provider"]
                ?? Environment.GetEnvironmentVariable("AudioStore__Provider")
                ?? "local";
            return provider.ToLowerInvariant() switch
            {
                "azureblob" => ActivatorUtilities.CreateInstance<AzureBlobAudioStore>(sp),
                "dual"      => BuildDualStore(sp, config),
                _           => ActivatorUtilities.CreateInstance<LocalDiskAudioStore>(sp),
            };
        });
        // Bidirectional newest-wins sync. The service short-circuits at run
        // time when the audio store isn't DualWriteAudioStore — registering
        // it unconditionally keeps the DI graph simple and lets a config
        // change (single → dual) take effect on next restart without rewiring.
        services.AddSingleton<AudioReconciliationService>();
        services.AddHostedService<AudioReconciliationBackgroundService>();
        // Typed repositories — every directory repo now lives on the unified
        // SQL Server StreetSamurai database via EfRepository<T>. The explicit
        // factory functions disambiguate between the production
        // (IDbContextFactory) ctor and the test-fixture (IPathProvider) ctor.
        IDbContextFactory<StreetSamuraiDbContext> Db(IServiceProvider sp) =>
            sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        services.AddSingleton(sp => new CharacterRepository(Db(sp)));
        services.AddSingleton(sp => new CorponationRepository(Db(sp)));
        services.AddSingleton(sp => new DistrictRepository(Db(sp)));
        services.AddSingleton(sp => new FactionRepository(Db(sp)));
        services.AddSingleton(sp => new SpeciesRepository(Db(sp)));
        services.AddSingleton(sp => new WorldbuildingDocRepository(Db(sp)));
        services.AddSingleton(sp => new WeaponryRepository(Db(sp)));
        services.AddSingleton(sp => new AmmunitionRepository(Db(sp)));
        services.AddSingleton(sp => new EquipmentRepository(Db(sp)));
        services.AddSingleton(sp => new TechnologyRepository(Db(sp)));
        services.AddSingleton(sp => new CyberwareRepository(Db(sp)));
        services.AddSingleton(sp => new VocabularyRepository(Db(sp)));
        services.AddSingleton(sp => new GenemodRepository(Db(sp)));
        services.AddSingleton(sp => new TransportationRepository(Db(sp)));
        services.AddSingleton(sp => new QuoteRepository(Db(sp)));
        services.AddSingleton(sp => new ContractRepository(Db(sp)));
        services.AddSingleton(sp => new NewsRepository(Db(sp)));
        services.AddSingleton(sp => new ArchetypeRepository(Db(sp)));
        services.AddSingleton(sp => new MaterialRepository(Db(sp)));
        services.AddSingleton(sp => new PharmaceuticalRepository(Db(sp)));
        services.AddSingleton(sp => new ConsumerGoodRepository(Db(sp)));
        services.AddSingleton(sp => new AutomatonRepository(Db(sp)));
        services.AddSingleton(sp => new ApparelRepository(Db(sp)));
        services.AddSingleton(sp => new SubsidiaryRepository(Db(sp)));
        services.AddSingleton(sp => new EntertainmentRepository(Db(sp)));
        services.AddSingleton(sp => new MotifRepository(Db(sp)));
        services.AddSingleton(sp => new LabSpecimenRepository(Db(sp)));
        services.AddSingleton(sp => new FlyoverEntityRepository(Db(sp)));
        services.AddSingleton(sp => new PsionicRepository(Db(sp)));
        // Bible singletons — explicit factory routes through the SQL Settings
        // ctor. Without this, DI sees both the SQL ctor and the IPathProvider
        // test-fixture ctor and throws "ambiguous constructors".
        services.AddSingleton(sp => new ToneBibleRepository(Db(sp)));

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
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<LabSpecimenRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<FlyoverEntityRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<PsionicRepository>());
        services.AddSingleton<IExportableRepository>(sp => sp.GetRequiredService<MotifRepository>());

        // Export discovery — auto-finds all IExportableRepository instances
        services.AddSingleton<ExportDiscoveryService>();

        // Canon JSON export to the configured publish directory (entity / repo / global)
        services.AddSingleton<CanonExportService>();

        // Family-relationship API on top of the existing Edge table
        services.AddSingleton<FamilyTieService>();

        // Genetics inheritance — propagates genetic_ancestry from parents to
        // children via the family graph with ±5% recombination noise. No-op
        // until family ties exist.
        services.AddSingleton<GeneticsInheritanceService>();

        // Family-member generator — proposes parents/siblings/spouse/children
        // for an existing character, names sourced from the canon name pool.
        // Staged growth: proposal first, apply on user approval.
        services.AddSingleton<FamilyGeneratorService>();

        // Image-prompt regen — rewrite ethnicity-keyed visual descriptors in
        // image_prompt + dalle3_prompt to match a character's current
        // genetic_ancestry. Cost-aware via inline ancestry hash.
        services.AddSingleton<ImagePromptRegenService>();
        services.AddSingleton(sp => new StoryBibleRepository(Db(sp)));
        services.AddSingleton(sp => new LiteraryRulesRepository(Db(sp)));
        services.AddSingleton(sp => new CharacterProfileRepository(Db(sp)));

        // Media files — images, video, 3D models named {entityId}.{index:D2}.{ext}
        services.AddSingleton<MediaService>();

        // User accounts and authentication. AuthService + PasswordResetService were
        // retired in favor of MindAttic.Authentication (wired in the Blazor host via
        // AddMindAtticAuthentication). UserRepository is retained for one release as the
        // sole data source for AuthUserImportService (its 'users.accounts' Settings blob
        // is the rollback artifact); ProfileService (avatars) + EmailService (SMTP) stay.
        services.AddSingleton<UserRepository>();
        services.AddSingleton<ProfileService>();
        services.AddSingleton<EmailService>();

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IDatabaseService>(sp => sp.GetRequiredService<DatabaseService>());
        services.AddSingleton<XrefService>();
        services.AddSingleton<ScriptRunnerService>();
        services.AddSingleton<ProjectArchitectureService>();
        services.AddSingleton<FixPhiService>();
        services.AddSingleton<FixIdentityCorruptionService>();
        services.AddSingleton<TagNormalizerService>();
        services.AddSingleton<TagWeaponLethalityService>();
        services.AddSingleton<AssignTiersService>();
        services.AddSingleton<CrossReferenceService>();
        services.AddSingleton<GlobalSearchService>();
        services.AddHostedService<GlobalSearchWarmupService>();
        services.AddSingleton<SearchTriggerService>();
        services.AddSingleton<LoreService>();
        services.AddSingleton<MarkdownService>();
        services.AddSingleton<ViewModeService>();
        // Unified continuity store — atomic (entity, predicate, object) claims
        // extracted from chapter prose AND entity records via Legion Quorum.
        // One SQLite at engine/data/continuity.db. Replaces the prior
        // LoreTriple* services (now removed).
        services.AddSingleton<ContinuityService>();
        services.AddSingleton<ContinuityExtractionService>();
        services.AddSingleton<ContinuityApplyService>();

        // Once-a-day background pass that re-runs GetContradictionGroups so
        // the long-sweep audit isn't gated on a /continuity page click.
        // PeriodicTimer-based BackgroundService — no Quartz/Hangfire dep.
        services.AddSingleton<ContinuityLongSweepService>();
        services.AddHostedService(sp => sp.GetRequiredService<ContinuityLongSweepService>());

        // Universal KV façade over the Settings table — used by every per-book /
        // per-world JSON store that previously wrote to engine_data/*.json.
        services.AddSingleton<SettingsKvStore>();
        services.AddSingleton<RepositoryDefinitionService>();

        // Multi-universe tenancy. The ambient current-universe selector (SS-LAW-15).
        // Singleton because the DbContext factory + repositories are singletons; the
        // selection rides UniverseScope (set in the ctor) rather than a DI scope.
        // CLI/MCP/UI set it per process via UseUniverse / UseUniverseBySlug.
        services.AddSingleton<UniverseContext>();
        services.AddSingleton<IUniverseContext>(sp => sp.GetRequiredService<UniverseContext>());

        // Reusable expert-archetype voters for beat generation. ListAll() seeds
        // from ExpertPersonaCatalog on first read; SelectPertinentAsync uses a
        // small Haiku-class panel to pick top-N pertinent personas per scene.
        services.AddSingleton<ExpertPersonaService>();

        // Per-action voter-count + model-tier registry, editable from settings.
        // ChapterBeatWriter (10 high) / ChapterBeatVoter (100 low) etc. live here.
        services.AddSingleton<ActionConfigService>();

        // Editorial prose-lessons memory — author rulings injected into review
        // ballot prompts so reviewers don't penalize beats already ruled acceptable.
        services.AddSingleton<ProseLessonStore>();

        // Global story-time cursor (Settings('story_now') as datetime2(7)).
        services.AddSingleton<WorldClockService>();

        // Living-world tick — advances story-time on a real-time interval.
        // Foundation for decay rules / scheduled events / NPC routines. Ships
        // disabled-by-default (see WorldTickService.Enabled) so infrastructure
        // can land before the rule layer.
        services.AddSingleton<WorldTickService>();
        services.AddHostedService(sp => sp.GetRequiredService<WorldTickService>());

        // Cross-book "character at two places at once" detector.
        services.AddSingleton<LocationContradictionService>();

        // Deterministic timeline-consistency validator (RFC 0009 §5).
        // Zero LLM calls. Detects dead-character-acting + wound regressions.
        services.AddSingleton<TimelineConsistencyService>();

        // LLM-driven backfill for Chapter/Beat InWorldDate columns.
        services.AddSingleton<DateBackfillService>();

        // Reverses double-encoded UTF-8 ("Ã³" → "ó", "â€"" → "—") in every
        // NVARCHAR column. One-shot data hygiene utility.
        services.AddSingleton<MojibakeRepairService>();

        // World-state ledger — append-only EntityStateEvents stream + LLM
        // extractor that runs on every chapter save. Subscribing to
        // OnChapterSaved happens in BeatStateExtractor's ctor; eager-instantiate
        // it at startup so the subscription is live before the first save.
        services.AddSingleton<WorldStateLedger>();
        services.AddSingleton<BeatStateExtractor>();
        // Per-weapon ammo + spec wiring (one-shot Chorus seed + bulk LLM linker).
        services.AddSingleton<AmmunitionLinkerService>();

        // Single-table schema rebuild — formalized snapshot + drop + recreate
        // workflow so column reorders / shape changes are routine instead of
        // hand-crafted. Snapshot artifact lands in engine/data/schema-snapshots/.
        services.AddSingleton<SchemaRebuildService>();

        // Single C# code path for the canonical SQL seeds under Data/Sql/*.sql.
        // Idempotent via the SeedRuns audit table; CLI: `ss --seed <name>`.
        services.AddSingleton<SqlSeedService>();

        // Reads sys.* into a JSON-friendly graph for the /schema visualization.
        services.AddSingleton<SchemaGraphService>();

        // Repeatable workflow for "relocate every character matching predicate
        // X to place P + add to faction F". Touches Characters / Records.Json /
        // CharacterAffiliations / Edges / EntityStateEvents in one transaction.
        services.AddSingleton<CohortRelocationService>();

        // Generalized prose → relational graph compiler. Takes any description,
        // extracts entities + typed relationships via LLM, resolves to canon
        // (or stubs missing), wires Edges + ledger events.
        services.AddSingleton<FactInterpreterService>();

        services.AddSingleton<StoryMethodologyService>();
        services.AddSingleton<CharacterPipelineService>();
        services.AddSingleton<WorldConsistencyService>();
        services.AddSingleton<DataConsistencyService>();
        services.AddSingleton<DataRepairService>();
        // JsonArchivalService and JsonPruneService retired 2026-05-08 —
        // engine/data/*.json no longer exists, so file-vs-DB verification
        // and pruning have no work to do. Files deleted, CLI verbs removed.

        // Embedding cache + similarity search. One row per Entity in
        // EntityEmbeddings; exact-NN cosine in C# at this corpus size.
        services.AddHttpClient(nameof(EmbeddingService));
        services.AddSingleton<EmbeddingService>();

        // X-Ray scene assembly (RFC 0002): entity roster + voice/psychology context
        // block for the entities present in a beat. Complements SceneContextBuilder
        // (ambient texture) — this one carries WHO is on screen and how they speak.
        services.AddSingleton<SceneContextAssembler>();
        services.AddSingleton<WoundLedgerService>();

        services.AddSingleton<CharacterStateBackfillService>();
        services.AddSingleton<DriftAuditService>();
        services.AddSingleton<AskService>();
        services.AddSingleton<SceneContextBuilder>();
        services.AddSingleton<ConsequenceService>();
        services.AddSingleton<AmbientAnomalyService>();
        services.AddSingleton<NarrativeSummaryService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<HtmlExportService>();
        services.AddSingleton<IChapterRepository>(sp => new ChapterRepository(
            sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ChapterRepository>>()));
        services.AddSingleton<IBookRepository>(sp => new BookRepository(
            sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BookRepository>>()));
        services.AddSingleton<ISeriesRepository>(sp => new SeriesRepository(
            sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SeriesRepository>>()));
        services.AddSingleton<WorldStateService>(sp =>
        {
            var ws = new WorldStateService(
                sp.GetRequiredService<WorldGraphService>(),
                sp.GetRequiredService<ContinuityService>(),
                sp.GetRequiredService<IChapterRepository>(),
                sp.GetRequiredService<CharacterRepository>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WorldStateService>>());
            // Wire the DbContext factory so temporal recall (FOR SYSTEM_TIME AS OF)
            // can hit the history tables directly.
            ws.DbCtxFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
            // Invalidate the dossier cache whenever a character record is saved so
            // subsequent prose generation sees the updated record.
            sp.GetRequiredService<CharacterRepository>().OnItemSaved += _ => ws.InvalidateAll();
            return ws;
        });
        services.AddSingleton<WorldStatePrecheckService>();
        services.AddSingleton<BeatFactExtractionService>();
        services.AddSingleton<StoryRepairService>();

        services.AddSingleton<IBookReviewService, BookReviewService>();
        services.AddSingleton<BookExportService>();

        // Episode (bedtime adventures) — seed → LLM → beats → TTS → audio files
        services.AddSingleton<EpisodeSeedService>();
        services.AddSingleton<EpisodeGeneratorService>();
        services.AddSingleton<EpisodeAudioService>();
        services.AddSingleton<EpisodeExportService>();
        services.AddSingleton<ChapterRecordingService>();
        services.AddSingleton<NodeMigrationService>();
        services.AddSingleton<NodeWorkbenchService>(sp => new NodeWorkbenchService(
            sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>(),
            sp.GetRequiredService<ElevenLabsTtsService>(),
            sp.GetRequiredService<IPathProvider>(),
            sp.GetRequiredService<IAudioStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NodeWorkbenchService>>(),
            sp.GetService<SettingsService>(),
            sp.GetRequiredService<EntityRamificationService>(),
            sp.GetRequiredService<PostBeatValidationService>()));
        services.AddSingleton<WritingQualityService>();
        services.AddSingleton(sp => new MotifService(
            sp.GetRequiredService<SettingsKvStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MotifService>>()));
        services.AddSingleton(sp => new BookOutlineService(
            sp.GetRequiredService<IBookRepository>(),
            sp.GetRequiredService<IChapterRepository>(),
            sp.GetRequiredService<SettingsKvStore>(),
            sp.GetRequiredService<MindAttic.Legion.LlmVotingService>(),
            sp.GetRequiredService<DatabaseService>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BookOutlineService>>()));
        services.AddSingleton<CoWriterService>();
        services.AddSingleton<LastPromptStore>();
        // Graph builds from canon on first access. With the SQL cutover, freshness
        // is driven by Records.UpdatedAt — the IDbContextFactory ctor receives the
        // factory so IsStale() can probe the canonical record table.
        //
        // EnsureLoaded() runs synchronously and only loads the on-disk JSON cache;
        // it no longer probes SQL or rebuilds unless the cache is empty (first
        // run). RefreshIfStale() does the SQL probe + potential rebuild on a
        // background task so app.Run() isn't held up by it — that was the bulk
        // of the 60+ s cold-start cost.
        services.AddSingleton<WorldGraphService>(sp =>
        {
            var graph = new WorldGraphService(
                sp.GetRequiredService<IPathProvider>(),
                sp.GetRequiredService<DatabaseService>(),
                sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>());
            graph.EnsureLoaded();
            _ = Task.Run(graph.RefreshIfStale);
            return graph;
        });

        services.AddSingleton<IWorldGraphService>(sp => sp.GetRequiredService<WorldGraphService>());

        // Semantic search — TF-IDF index over all graph entities. Build is gated
        // inside Search/UpdateNode on the `built` flag so first query rebuilds
        // lazily; do NOT rebuild in the DI factory or we pay ~5-10 s of TF-IDF
        // work on every startup before serving the first request.
        services.AddSingleton<SemanticIndexService>();

        // Cross-entity inference — same lazy pattern: RebuildPropertyIndex runs
        // on first GetInferredConnections / GetNodesByProperty call.
        services.AddSingleton<InferenceService>();

        // Automatic relationship discovery — scans entity saves for new edges
        services.AddSingleton<RelationshipDiscoveryService>(sp =>
        {
            var discovery = new RelationshipDiscoveryService(
                sp.GetRequiredService<WorldGraphService>(),
                sp.GetRequiredService<SemanticIndexService>(),
                sp.GetRequiredService<InferenceService>(),
                sp.GetRequiredService<EmbeddingService>());

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

        // Entity-update → beat EntityStale propagation.
        // Single factory creates the service AND wires OnEntitySaved on every
        // canon repository so entity patches automatically flag referencing beats.
        services.AddSingleton<EntityRamificationService>(sp =>
        {
            var ramSvc = new EntityRamificationService(
                sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>(),
                sp.GetRequiredService<ILlmService>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EntityRamificationService>>());

            sp.GetRequiredService<CharacterRepository>().OnEntitySaved      += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<CorponationRepository>().OnEntitySaved    += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<DistrictRepository>().OnEntitySaved       += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<FactionRepository>().OnEntitySaved        += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<WeaponryRepository>().OnEntitySaved       += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<EquipmentRepository>().OnEntitySaved      += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<TechnologyRepository>().OnEntitySaved     += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<DistrictRepository>().OnEntitySaved        += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<PharmaceuticalRepository>().OnEntitySaved += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));
            sp.GetRequiredService<CyberwareRepository>().OnEntitySaved      += (id, name) => _ = Task.Run(() => ramSvc.ProcessEntityUpdateAsync(id, name));

            return ramSvc;
        });

        // LLM services — multi-provider with router
        // ClaudeService + OpenAiService are now thin Legion adapters — no
        // dedicated HttpClient needed; LegionClient owns the socket pool, and
        // socket-stability tuning (PooledConnectionLifetime, etc.) lives there.
        services.AddSingleton<ClaudeService>();
        services.AddSingleton<OpenAiService>();
        services.AddSingleton<LocalLlmService>();
        services.AddSingleton<DallEService>();
        services.AddSingleton<LlmRouter>(sp => new LlmRouter(
            sp.GetRequiredService<ClaudeService>(),
            sp.GetRequiredService<OpenAiService>(),
            sp.GetRequiredService<LocalLlmService>(),
            sp.GetRequiredService<SettingsService>(),
            sp.GetRequiredService<LegionClient>(),
            sp.GetRequiredService<LastPromptStore>(),
            sp.GetRequiredService<ILogger<LlmRouter>>()));
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<LlmRouter>());

        // TTS service
        services.AddHttpClient<ElevenLabsTtsService>();
        services.AddSingleton<ITtsService>(sp => sp.GetRequiredService<ElevenLabsTtsService>());

        // Audio file service
        services.AddSingleton<IAudioFileService, AudioFileService>();

        // Multi-LLM service — calls multiple providers for majority voting.
        // Wire transport delegated to MindAttic.Legion's LegionClient.
        services.AddSingleton<MultiLlmService>();

        // TTS enhancement — adds ElevenLabs audio tags before synthesis
        services.AddSingleton<TtsEnhancementService>();

        // Draft narration — free Windows SAPI voices (Windows only)
        if (OperatingSystem.IsWindows())
            services.AddSingleton<WindowsTtsService>();

        // Entity extraction — LLM-powered story-to-graph pipeline
        services.AddSingleton<EntityExtractionService>();

        // Canon validation — checks generated text against graph for contradictions
        services.AddSingleton<ValidationService>();

        // Thematic index — tag-based cross-repo retrieval for story generation.
        // RebuildIndex is gated inside Get* methods on the `built` flag, so the
        // first query builds lazily. Skipping the factory rebuild trims ~5-15 s
        // off cold-start (this one touches 10 repos via GetAll()).
        services.AddSingleton<ThematicIndexService>();

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

        // Universal facts — world mechanics / vocabulary injected into every generation prompt
        services.AddSingleton<UniversalFactsService>();

        // Scene generation pipeline
        services.AddSingleton<TextAnalysisService>();
        services.AddSingleton<ContextAnalyzerService>();
        services.AddSingleton<BeatGeneratorService>();
        services.AddSingleton<NodeBibleService>();
        services.AddSingleton<SceneGenerationService>();
        services.AddSingleton<CombatSceneWriter>();
        services.AddSingleton<StoryStarterService>();

        // Story director — autonomous story generation
        services.AddSingleton<StoryDirectorService>();
        services.AddSingleton<IStoryDirectorService>(sp => sp.GetRequiredService<StoryDirectorService>());

        // Claude Code CLI bridge — legacy Writer-chat path (kept until UI fully migrated to operator)
        services.AddSingleton<ClaudeCliService>();

        // Writer operator — interactive chat partner that drives StreetSamurai services
        // via Anthropic tool-use, replacing per-message CLI spawn. Scoped per Blazor circuit
        // so each writing session has its own chat history.
        services.AddHttpClient<Services.Operator.AnthropicToolClient>(c => c.Timeout = TimeSpan.FromMinutes(15));

        // Media asset storage (local disk dev; swap to AddMediaAzure<StreetSamuraiDbContext> in prod).
        services.AddMedia<StreetSamuraiDbContext>();

        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.QueryWorldGraphTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.ValidateCanonTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.DraftCombatSceneTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.OutlineChapterTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.ScoreStoryQualityTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.RefineStoryTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.ExtractEntitiesTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.PredictBehaviorTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.GetVoiceContextTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.GetConsequencesTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.RecordCanonChangeTool>();
        services.AddSingleton<Services.Operator.IWriterTool, Services.Operator.Tools.ProposeStoryEditsTool>();
        services.AddSingleton<Services.Operator.WriterToolRegistry>();
        services.AddScoped<Services.Operator.WriterOperatorService>();

        // Geographic navigation, pathfinding, and dynamic place generation
        services.AddSingleton<NavigationService>();
        services.AddSingleton<DynamicPlaceGenerator>();

        // Freelancer story systems
        services.AddSingleton<ContractGenerator>();
        services.AddSingleton(sp => new NamePoolService(
            sp.GetRequiredService<IPathProvider>(),
            sp.GetRequiredService<SettingsKvStore>(),
            sp.GetRequiredService<IDatabaseService>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NamePoolService>>()));
        services.AddSingleton<NpcGenerator>();
        services.AddSingleton<RandomEncounterService>();
        services.AddSingleton(sp => new ReputationTracker(
            sp.GetRequiredService<SettingsKvStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ReputationTracker>>()));
        services.AddSingleton(sp => new ConsequenceEngine(
            sp.GetRequiredService<SettingsKvStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConsequenceEngine>>()));

        // Milestone 2 story engine services
        services.AddSingleton<DialogueService>();
        services.AddSingleton<ArcTrackerService>();
        services.AddSingleton<ContinuityValidatorService>();
        services.AddSingleton<SuggestionEngineService>();

        // Pacing — static helper, registered for completeness
        services.AddSingleton<PacingService>();

        // Milestone 3 — outline review + quality feedback loop
        services.AddSingleton(sp => new OutlineReviewService(
            sp.GetRequiredService<ILlmService>(),
            sp.GetRequiredService<DatabaseService>(),
            sp.GetRequiredService<SettingsKvStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutlineReviewService>>()));

        // MindAttic.Legion — universal LLM-call client (LegionClient) and the
        // multi-provider voting machinery. Both LlmVotingProvider and the
        // standalone MultiLlmService delegate wire transport here.
        //
        // The default HttpClient timeout (100s) is too short for outline / book
        // generation, which can ask Claude for ~16k tokens at temperature 0.8 and
        // routinely takes 2–3 minutes. A timeout there cancels the call and the
        // pipeline writes "Outline generation failed" into the story file.
        // 15 minutes covers Sonnet 4.6's worst case for a 16k-token completion
        // including any backend backoff/retry, while still detecting a truly
        // stuck connection. Earlier 5-minute setting hit the wall on dense
        // outline-review prompts; 15 gives ample headroom.
        services.AddLegionClient();
        services.AddHttpClient<LegionClient>(c => c.Timeout = TimeSpan.FromMinutes(15));
        services.AddHttpClient<LlmVotingProvider>(c => c.Timeout = TimeSpan.FromMinutes(15));

        // Node-review transport seam (IReviewLlm). The cloud impl is a thin pass-through
        // to LegionClient (the trusted-4 panel, unchanged); the local impl is a
        // self-contained Ollama client used ONLY by `--local` reviews. They never mix.
        services.AddSingleton<StreetSamurai.Core.Services.Local.CloudReviewLlm>();
        services.AddHttpClient<StreetSamurai.Core.Services.Local.LocalReviewLlm>(c => c.Timeout = TimeSpan.FromMinutes(10));
        services.AddSingleton<VotingConfiguration>(sp =>
        {
            var s = sp.GetRequiredService<SettingsService>();
            var paths = sp.GetRequiredService<IPathProvider>();
            var cfg = new VotingConfiguration
            {
                ApiKeys =
                {
                    ["claude-api"]  = s.ApiKey,
                    // claude-team uses the Claude Code CLI OAuth token (~/.claude/.credentials.json).
                    // No API key exists; Legion resolves auth via ClaudeCodeOAuthSource. We seed the
                    // token here so VotingConfiguration.ActiveProviderIds includes claude-team when
                    // the OAuth token is present, and ResolveKey returns it to the review ballot call.
                    ["claude-team"] = LegionClient.GetClaudeTeamOAuthToken() ?? "",
                    ["openai"]     = s.OpenAiApiKey,
                    ["gemini"]     = s.GeminiApiKey,
                    ["deepseek"]   = s.DeepSeekApiKey,
                    ["mistral"]    = s.MistralApiKey,
                    ["xai"]        = s.GrokApiKey,
                    ["groq"]       = s.GroqApiKey,
                    ["together"]   = s.TogetherApiKey,
                    ["openrouter"] = s.OpenRouterApiKey,
                    ["fireworks"]  = s.FireworksApiKey,
                    ["cohere"]     = s.CohereApiKey,
                },
                JudgeProviderId = "claude-team",
                // AllowedProviderIds defaults to { claude, openai, deepseek }.
                // legion.json (when present at the project root) overrides this
                // so each app declares its own voter panel.
            };

            // Walk up from the data root looking for legion.json. Per-project
            // override; absent → defaults stand.
            var legion = LegionConfig.LoadFromDirectory(paths.DataRoot);
            legion?.ApplyTo(cfg);

            return cfg;
        });

        // SS-A44 voting kill-switch. One central gate consulted at the entry of
        // every ballot/score/vote-soliciting flow. Default read from legion.json
        // ("votingEnabled"); absent = OFF. Prose generation is never gated.
        services.AddSingleton<VotingGate>(sp =>
        {
            var pathProvider = sp.GetRequiredService<IPathProvider>();
            var gateLog      = sp.GetRequiredService<ILogger<VotingGate>>();
            var enabled      = VotingGate.ReadVotingEnabledDefault(pathProvider.DataRoot);
            return new VotingGate(enabled, gateLog);
        });

        // Findings store + autonomous quality monitor. The monitor subscribes
        // to IChapterRepository.OnChapterSaved on construction; eager-
        // instantiating it at startup makes the subscription live immediately.
        // Every chapter save triggers a background contradiction + cliché scan
        // via the cloud LLM, with grounding pulled from SQL via WorldStateService.
        services.AddSingleton<FindingsService>();
        services.AddSingleton<FindingApplyService>();
        services.AddSingleton<ContinuousQualityService>();
        services.AddSingleton<LlmVotingProvider>(sp =>
        {
            var cfg  = sp.GetRequiredService<VotingConfiguration>();
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(LlmVotingProvider));
            return new LlmVotingProvider(http, cfg);
        });
        services.AddSingleton<LlmVotingService>();
        // Persona reader-review system: export a node to markdown, fan N Legion
        // personas across the trusted-4 to each write an honest scored review,
        // then synthesize the Amazon-style aggregate.
        services.AddSingleton<NodeMarkdownExporter>();
        services.AddSingleton<ReviewReportExporter>();
        services.AddSingleton<PublishCleanupService>();
        services.AddSingleton<DocxExportService>();
        services.AddSingleton<ManuscriptExportService>();
        services.AddSingleton<AudiblePackageService>(sp =>
            new AudiblePackageService(
                sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>(),
                sp.GetRequiredService<NodeWorkbenchService>(),
                sp.GetRequiredService<SettingsService>(),
                sp.GetRequiredService<ILogger<AudiblePackageService>>(),
                sp.GetService<ILlmService>()));
        services.AddSingleton<VoiceHarvestService>();
        services.AddSingleton<CanonRetrievalService>();
        services.AddSingleton<CanonContradictionService>();
        services.AddSingleton<CoverageService>();
        services.AddSingleton<ProseReflowService>();
        services.AddSingleton<BeatRebuildService>();
        services.AddSingleton<NodeReviewService>();
        services.AddSingleton<StoryQualityService>();
        services.AddSingleton<StoryRefinementService>();
        services.AddSingleton<CanonGroundingService>();
        services.AddSingleton<EntityRatingService>();
        services.AddSingleton<EntityReviewService>();
        services.AddSingleton<WeaponAmmoLinkerService>();
        services.AddSingleton<DistributedWorkerCoordinator>();

        // Prose-continuity and world-modelling services (2026-06-16)
        services.AddSingleton<EntityRelationshipService>();
        services.AddSingleton<ProsePatternGuard>();
        services.AddSingleton<AmbientDetailInjector>();
        services.AddSingleton<WorldStateAtBeatService>();
        services.AddSingleton<GearCarryEnforcer>();
        services.AddSingleton<BehavioralInvariantEnforcer>();
        services.AddSingleton<WeaponAmmoCompatibilityService>();
        services.AddSingleton<MarkdownFileService>();
        services.AddSingleton<NodeSpineService>();

        // Auto-validation pipeline (2026-06-20): orchestrates prose guard + gear carry +
        // behavior enforcer after every beat save; files violations as Findings.
        services.AddSingleton<PostBeatValidationService>();

        // Semantic Fidelity Gap detector — Goodhart's Law guard for the review metric.
        // Runs in the background after every review; also available via
        // `ss --check-fidelity` and the `check_semantic_fidelity` MCP tool.
        services.AddSingleton<SemanticFidelityService>();

        // Overnight prose health pipeline — zero API cost; uses cached ProseEmbeddings
        // and text-only stats. Available via `ss --prose-health`.
        services.AddSingleton<EmbeddingHealthService>();
        services.AddSingleton<NightlyHealthService>();

        // Pre-flight structural diagnostics — 12 parallel LLM checks that catch
        // structural problems (missing antagonist cost, passive protagonist, etc.)
        // BEFORE the 60-ballot review panel. Available via `ss --diagnose-story`
        // and the `diagnose_node` MCP tool.
        services.AddSingleton<StructuralDiagnosticService>();

        // Emotional Intelligence Examination (SS-A15): 8-dimension, 0–4, per-beat,
        // character-aware rubric. Advisory cap on blocking dimensions at Deep gate.
        // Available via `ss --examine-emotion` and the `examine_emotional_depth` MCP tool.
        services.AddSingleton<EmotionalLedgerService>();
        services.AddSingleton<EmotionalDepthService>();
        services.AddSingleton<RegisterExemplarService>();

        // Narrative-science analysis (Will Storr frameworks): sacred flaw, dramatic
        // question, scene-engagement audit, five-act map, antihero empathy.
        // Available via `ss --narrative-science` and the Tools.NarrativeScience MCP tools.
        services.AddSingleton<NarrativeScienceService>();

        // Plant/Payoff registry — tracks seeded narrative details and their payoffs.
        // Enforces "reward re-reading without requiring it."
        // Available via `ss --plant-audit`, `ss --list-plants`, `ss --add-plant`
        // and the Tools.PlantPayoff MCP tools.
        services.AddSingleton<PlantPayoffService>();

        // Story commandment audits — gateway (standalone) and sequel commandment sets.
        // Determined automatically from Node.PreviousNodeId.
        // Available via `ss --story-audit` and the Tools.StoryAudit MCP tools.
        services.AddSingleton<StoryAuditService>();

        // "Behave like people" beat lenses (Findings-only; no new DB tables).
        //   CausalityService            — therefore/but, not "and then"     (ss --causality-check;     causality_check)
        //   AffectBehaviorService       — emotion plausibly drives action   (ss --affect-check;        affect_check)
        //   InterpersonalDynamicsService— verbal+non-verbal relational work (ss --interpersonal-check; interpersonal_check) — the 90+ lever
        services.AddSingleton<CausalityService>();
        services.AddSingleton<AffectBehaviorService>();
        services.AddSingleton<InterpersonalDynamicsService>();

        // Deterministic prose sanity scan — no LLM; catches leaked internal codes,
        // undefined acronyms, encoding corruption, and heft floor violations.
        // Available via `ss --sanity-scan`.
        services.AddSingleton<SanityScanService>();

        // Deterministic noun consistency scan — no LLM; flags deprecated/renamed
        // noun references in beat prose (e.g. old drone name "VacCell" → "Nit").
        // Rules registered in DeprecatedEntityNames table, universe-scoped.
        // Available via `ss --validate-nouns --slug <slug>` and validate_nouns MCP.
        services.AddSingleton<NounConsistencyService>();

        // Canon-sync survey service — persists questions, answers, and apply logs in
        // DB so the full decision trail survives across sessions. Generates the
        // interactive artifact HTML from DB data rather than hand-coding it each round.
        // MCP: create_survey, get_survey, list_surveys, answer_survey_question,
        //      mark_survey_question_applied, complete_survey, get_survey_html.
        // CLI: ss --list-surveys [--status Open|Completed], ss --get-survey --slug <slug>.
        services.AddSingleton<SurveyService>();

        // Beat mode detection + workflow coverage monitoring.
        // ProseWriterRouter is the preferred entry point for all prose generation — it
        // auto-detects beat mode, injects pacing + structural guidance, and logs coverage.
        // Use ss --workflow-status to inspect coverage gaps.
        services.AddSingleton<BeatModeDetector>();
        services.AddSingleton<MlProseGuidanceService>();
        services.AddSingleton<WorkflowMonitorService>();
        services.AddSingleton<EntityContextStack>();
        services.AddSingleton<EntityContextService>();
        services.AddScoped<EntityMentionService>();
        services.AddSingleton<DocContextStack>();
        services.AddSingleton<DocContextService>();
        services.AddSingleton<ContextTelemetryService>();
        services.AddSingleton<TelemetryExportService>();
        services.AddSingleton<TensionEscalationService>();
        services.AddSingleton<ReaderKnowledgeService>();
        services.AddSingleton<ChapterSummaryService>();
        services.AddSingleton<OpenThreadsService>();
        services.AddSingleton<PremiseToOutlineService>();
        services.AddSingleton<OutlineAdherenceService>();
        services.AddSingleton<NarrativeForkService>();
        services.AddSingleton<ChapterCloseProcessorService>();
        services.AddSingleton<ProseWriterRouter>();
        services.AddSingleton<StoryLogicAuditService>();

        return services;
    }

    /// <summary>Compose a DualWriteAudioStore from two sub-providers. Defaults
    /// are local primary + Azure blob secondary — the typical "Azure as
    /// replica" deployment. Override via AudioStore:Primary / AudioStore:Secondary
    /// to flip the roles (e.g. blob primary, local cache).</summary>
    private static IAudioStore BuildDualStore(IServiceProvider sp, Microsoft.Extensions.Configuration.IConfiguration? config)
    {
        var primaryName = config?["AudioStore:Primary"]
            ?? Environment.GetEnvironmentVariable("AudioStore__Primary")
            ?? "local";
        var secondaryName = config?["AudioStore:Secondary"]
            ?? Environment.GetEnvironmentVariable("AudioStore__Secondary")
            ?? "azureblob";
        var primary   = CreateBackend(sp, primaryName);
        var secondary = CreateBackend(sp, secondaryName);
        var log = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DualWriteAudioStore>>();
        var cacheReads = !string.Equals(
            config?["AudioStore:CacheReadsToPrimary"] ?? Environment.GetEnvironmentVariable("AudioStore__CacheReadsToPrimary") ?? "true",
            "false", StringComparison.OrdinalIgnoreCase);
        return new DualWriteAudioStore(primary, secondary, log, cacheReads);

        static IAudioStore CreateBackend(IServiceProvider sp, string name) => name.ToLowerInvariant() switch
        {
            "azureblob" => ActivatorUtilities.CreateInstance<AzureBlobAudioStore>(sp),
            _           => ActivatorUtilities.CreateInstance<LocalDiskAudioStore>(sp),
        };
    }
}
