using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MindAttic.Media;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;
using ContractEntity = StreetSamurai.Core.Data.Entities.Contract;
using DocumentEntity = StreetSamurai.Core.Data.Entities.Document;
using QuoteEntity    = StreetSamurai.Core.Data.Entities.Quote;
using NewsEntity     = StreetSamurai.Core.Data.Entities.News;

namespace StreetSamurai.Core.Data;

/// <summary>
/// Single source of truth for canonical world data. Replaces the per-type JSON
/// repositories at <c>engine/data/</c>. Contains:
///
/// • Universal layer — Entity, EntityProperty, Edge, Taxonomy, EntityTaxonomy,
///   Tag, EntityTag.
/// • Strongly-typed subtype tables — Character + child tables for now; remaining
///   types added in subsequent migrations following the same pattern.
///
/// System-versioning (SQL Server <c>PERIOD FOR SYSTEM_TIME</c>) is enabled per
/// table via raw SQL in the initial migration — EF doesn't emit those clauses
/// natively yet. The OnModelCreating below registers indexes and relationships;
/// the migration tacks on the temporal clauses.
/// </summary>
public class StreetSamuraiDbContext : DbContext
{
    public StreetSamuraiDbContext(DbContextOptions<StreetSamuraiDbContext> options) : base(options) { }

    /// <summary>
    /// The universe the global query filters scope to, read from the ambient
    /// <see cref="UniverseScope"/>. <c>Guid.Empty</c> (no universe context wired — tests /
    /// design-time / pre-migration) makes every universe filter a no-op. Referenced by the
    /// <c>HasQueryFilter</c> lambdas in <see cref="OnModelCreating"/>; EF re-evaluates it per query.
    /// </summary>
    public Guid ScopedUniverseId => StreetSamurai.Core.Services.UniverseScope.EffectiveId;

    /// <summary>The "visible from every universe" sentinel for shared operational config rows
    /// (Settings query filter). Constant — EF inlines it into the filter.</summary>
    public static Guid SharedUniverseId => Universe.SharedId;

    /// <summary>
    /// Stamp <c>UniverseId</c> on freshly-added universe-scoped roots (Entity / Node / Book) so
    /// new rows land in the current universe without every call site having to set it. Falls back
    /// to GLMZ when no universe context is active (tests), keeping the NOT NULL column valid.
    /// </summary>
    private void StampUniverseOnAdded()
    {
        var current = StreetSamurai.Core.Services.UniverseScope.EffectiveId;
        var target = current != Guid.Empty ? current : Universe.GlmzId;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;
            switch (entry.Entity)
            {
                case Entity e when e.UniverseId == Guid.Empty: e.UniverseId = target; break;
                case Node s when s.UniverseId == Guid.Empty: s.UniverseId = target; break;
                case Book bk when bk.UniverseId == Guid.Empty: bk.UniverseId = target; break;
                case Species sp when sp.UniverseId == Guid.Empty: sp.UniverseId = target; break;
                case Edge ed when ed.UniverseId == Guid.Empty: ed.UniverseId = target; break;
                case EntityStateEvent ev when ev.UniverseId == Guid.Empty: ev.UniverseId = target; break;
                case CharacterReadModel rm when rm.UniverseId == Guid.Empty: rm.UniverseId = target; break;
                case PlantPayoff pp when pp.UniverseId == Guid.Empty: pp.UniverseId = target; break;
                case DeprecatedEntityName den when den.UniverseId == Guid.Empty: den.UniverseId = target; break;
                case BeatServiceLog bsl when bsl.UniverseId == Guid.Empty: bsl.UniverseId = target; break;
                case BeatModeLog bml when bml.UniverseId == Guid.Empty:    bml.UniverseId = target; break;
                // Config rows: operational/shared keys are tagged with the SHARED sentinel so every
                // universe sees the one copy; all other keys are scoped to the current universe.
                case Setting st when st.UniverseId == Guid.Empty:
                    st.UniverseId = StreetSamurai.Core.Services.UniverseScope.SharedConfigKeys.Contains(st.Key)
                        ? Universe.SharedId : target;
                    break;
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampUniverseOnAdded();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampUniverseOnAdded();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Multi-universe tenancy — every universe-scoped root (Entity, Node, Book)
    // carries a UniverseId; reads are filtered to the current universe (SS-LAW-15).
    public DbSet<Universe>               Universes              => Set<Universe>();

    // Runtime-defined entity types (custom repos). Global (not universe-scoped);
    // board display is filtered by active-entity-count in the current universe.
    public DbSet<RepositoryDefinition>   RepositoryDefinitions  => Set<RepositoryDefinition>();

    // Universal layer
    public DbSet<Entity>          Entities         => Set<Entity>();
    public DbSet<Record>          Records          => Set<Record>();
    public DbSet<EntityProperty>  EntityProperties => Set<EntityProperty>();
    public DbSet<Edge>            Edges            => Set<Edge>();
    public DbSet<Taxonomy>        Taxonomies       => Set<Taxonomy>();
    public DbSet<EntityTaxonomy>  EntityTaxonomies => Set<EntityTaxonomy>();
    public DbSet<Tag>             Tags             => Set<Tag>();
    public DbSet<EntityTag>       EntityTags       => Set<EntityTag>();
    public DbSet<FindingRow>      Findings         => Set<FindingRow>();

    // Episodic adventures — folk-hero Kyle, bedtime-listenable, scoreable
    public DbSet<Episode>            Episodes            => Set<Episode>();
    public DbSet<EpisodeBeat>        EpisodeBeats        => Set<EpisodeBeat>();
    public DbSet<EpisodeCorrection>  EpisodeCorrections  => Set<EpisodeCorrection>();
    public DbSet<EpisodeSurvey>      EpisodeSurveys      => Set<EpisodeSurvey>();

    // Unified storytelling schema — Beat = atom of prose+audio, Node =
    // ordered composition (replaces Book/Chapter/Episode), BeatNode =
    // junction. The whole system migrates onto these three.
    public DbSet<Beat>               Beats               => Set<Beat>();
    public DbSet<BeatEntityMention>  BeatEntityMentions  => Set<BeatEntityMention>();
    public DbSet<Node>             Nodes             => Set<Node>();
    // Typed TPH views over the same Nodes table (legacy Books/Chapters DbSets
    // below belong to the retired Book/Chapter schema, hence the *Nodes names).
    public DbSet<SeriesNode>       SeriesNodes       => Set<SeriesNode>();
    public DbSet<StoryNode>        StoryNodes        => Set<StoryNode>();
    public DbSet<ChapterNode>      ChapterNodes      => Set<ChapterNode>();
    public DbSet<BeatNode>         BeatNodes         => Set<BeatNode>();
    public DbSet<NodePublication>  NodePublications  => Set<NodePublication>();
    public DbSet<NodeAudioEvent>   NodeAudioEvents   => Set<NodeAudioEvent>();
    // Persona reader-reviews + their Amazon-style aggregate summary (nodes).
    public DbSet<NodeReview>          NodeReviews          => Set<NodeReview>();
    public DbSet<NodeReviewSummary>   NodeReviewSummaries  => Set<NodeReviewSummary>();
    // Append-only score timeline — one row per RecomputeScoresAsync call.
    public DbSet<NodeScoreHistory>    NodeScoreHistories   => Set<NodeScoreHistory>();
    // Per-node narrative spine: amendment log + version pins (bridge).
    public DbSet<NodeAmendment>       NodeAmendments       => Set<NodeAmendment>();
    public DbSet<NodeSpineVersion>    NodeSpineVersions    => Set<NodeSpineVersion>();
    // Amazon KDP / storefront search keywords (up to 7 per node).
    public DbSet<NodeKeyword>         NodeKeywords         => Set<NodeKeyword>();
    // Autonomous pipeline — chapter summaries + open threads + plot-state ledger.
    public DbSet<NodeChapterSummary>      NodeChapterSummaries      => Set<NodeChapterSummary>();
    public DbSet<NodeOpenThread>          NodeOpenThreads           => Set<NodeOpenThread>();
    public DbSet<StoryPlotEvent>          StoryPlotEvents           => Set<StoryPlotEvent>();
    public DbSet<NarrativeSummaryEntry>   NarrativeSummaryEntries   => Set<NarrativeSummaryEntry>();
    // Persona quality-reviews for canon entities (characters, weapons, tech, etc.).
    public DbSet<EntityReview>          EntityReviews          => Set<EntityReview>();
    public DbSet<EntityReviewSummary>   EntityReviewSummaries  => Set<EntityReviewSummary>();
    public DbSet<EntityReviewQueue>     EntityReviewQueue      => Set<EntityReviewQueue>();
    // Distributed work queue — entity-review / node-review / beat-review / beat-write.
    public DbSet<DistributedWorkQueue>  DistributedWorkQueue   => Set<DistributedWorkQueue>();
    // Append-only audit trail of voice-rule changes (directive / manual_edit / harvest).
    public DbSet<VoiceChangeLogEntry>   VoiceChangeLog         => Set<VoiceChangeLogEntry>();
    // First-class species taxonomy for sentient life (human/ai/elf/synthetic/unknown).
    public DbSet<Species>               Species                => Set<Species>();
    // Named, reusable persona panels (focus groups) + their membership.
    public DbSet<FocusGroup>            FocusGroups            => Set<FocusGroup>();
    public DbSet<FocusGroupMember>      FocusGroupMembers      => Set<FocusGroupMember>();
    // Per-beat micro-scores (study mode) — the reviewer x beat matrix.
    public DbSet<NodeReviewBeatScore> NodeReviewBeatScores => Set<NodeReviewBeatScore>();
    // Emotional Intelligence Examination (SS-A15): examination parent + dimension/beat children + ledger cache.
    public DbSet<EmotionalExamination>      EmotionalExaminations      => Set<EmotionalExamination>();
    public DbSet<EmotionalDimensionResult>  EmotionalDimensionResults  => Set<EmotionalDimensionResult>();
    public DbSet<EmotionalBeatScore>        EmotionalBeatScores        => Set<EmotionalBeatScore>();
    public DbSet<CharacterEmotionalLedger>  CharacterEmotionalLedgers  => Set<CharacterEmotionalLedger>();
    // Gaps table folded into Beat.GapAfterMs / Beat.GapAfterAudioPath
    // (migration fold_gaps_into_beats_20260523.sql). The standalone DbSet
    // is gone; gap-after-beat is now a property of the upper beat.

    // Character subtype + children — fully columnar (no DataJson on this branch)
    public DbSet<Character>                       Characters                    => Set<Character>();
    public DbSet<CharacterAlias>                  CharacterAliases              => Set<CharacterAlias>();
    public DbSet<CharacterStoryHook>              CharacterStoryHooks           => Set<CharacterStoryHook>();
    public DbSet<CharacterArchetypeScore>         CharacterArchetypeScores      => Set<CharacterArchetypeScore>();
    public DbSet<CharacterGeneticAncestry>        CharacterGeneticAncestries    => Set<CharacterGeneticAncestry>();
    public DbSet<CharacterAncestryDetail>         CharacterAncestryDetails      => Set<CharacterAncestryDetail>();
    public DbSet<CharacterPsychologyTrait>        CharacterPsychologyTraits     => Set<CharacterPsychologyTrait>();
    public DbSet<CharacterSpeechPhrase>           CharacterSpeechPhrases        => Set<CharacterSpeechPhrase>();
    public DbSet<CharacterBehavioralRule>         CharacterBehavioralRules      => Set<CharacterBehavioralRule>();
    public DbSet<CharacterBehavioralMap>          CharacterBehavioralMaps       => Set<CharacterBehavioralMap>();
    public DbSet<CharacterStatScalar>             CharacterStatScalars          => Set<CharacterStatScalar>();
    public DbSet<CharacterStatPhrase>             CharacterStatPhrases          => Set<CharacterStatPhrase>();
    public DbSet<CharacterPhysicalMark>           CharacterPhysicalMarks        => Set<CharacterPhysicalMark>();
    public DbSet<CharacterTerritoryZone>          CharacterTerritoryZones       => Set<CharacterTerritoryZone>();
    public DbSet<CharacterTerritoryReputation>    CharacterTerritoryReputations => Set<CharacterTerritoryReputation>();
    public DbSet<CharacterBelongingsGear>         CharacterBelongingsGear       => Set<CharacterBelongingsGear>();
    public DbSet<CharacterBelongingsExtra>        CharacterBelongingsExtras     => Set<CharacterBelongingsExtra>();
    public DbSet<CharacterBioBatteryThreshold>    CharacterBioBatteryThresholds => Set<CharacterBioBatteryThreshold>();
    public DbSet<CharacterNeuralAbility>          CharacterNeuralAbilities      => Set<CharacterNeuralAbility>();
    public DbSet<CharacterChangelogRow>           CharacterChangelog            => Set<CharacterChangelogRow>();
    public DbSet<CharacterCyberware>              CharacterCyberware            => Set<CharacterCyberware>();
    public DbSet<CharacterKnowledgeRow>           CharacterKnowledge            => Set<CharacterKnowledgeRow>();
    public DbSet<CharacterKnowledgeEntity>        CharacterKnowledgeEntities    => Set<CharacterKnowledgeEntity>();
    public DbSet<CharacterConditionRow>           CharacterConditions           => Set<CharacterConditionRow>();
    public DbSet<CharacterRelationshipRow>        CharacterRelationships        => Set<CharacterRelationshipRow>();
    public DbSet<CharacterTimelineEvent>          CharacterTimeline             => Set<CharacterTimelineEvent>();
    public DbSet<CharacterTimelineBodyChange>     CharacterTimelineBodyChanges  => Set<CharacterTimelineBodyChange>();
    public DbSet<CharacterHomeTurf>               CharacterHomeTurfs            => Set<CharacterHomeTurf>();
    public DbSet<CharacterAffiliation>            CharacterAffiliations         => Set<CharacterAffiliation>();
    // Derived read-model projection (CQRS-lite). NOT system-versioned — see
    // CharacterReadModel + SystemVersionedTables note.
    public DbSet<CharacterReadModel>              CharacterReadModels           => Set<CharacterReadModel>();

    // Other subtype tables
    public DbSet<Place>          Places          => Set<Place>();
    public DbSet<PlaceAlias>            PlaceAliases          => Set<PlaceAlias>();
    public DbSet<PlaceDanger>           PlaceDangers          => Set<PlaceDanger>();
    public DbSet<PlaceOpportunity>      PlaceOpportunities    => Set<PlaceOpportunity>();
    public DbSet<PlaceStoryHook>        PlaceStoryHooks       => Set<PlaceStoryHook>();
    public DbSet<PlaceAtmosphereItem>   PlaceAtmosphereItems  => Set<PlaceAtmosphereItem>();
    public DbSet<PlaceAdjacency>        PlaceAdjacencies      => Set<PlaceAdjacency>();
    public DbSet<PlaceExitRow>          PlaceExits            => Set<PlaceExitRow>();
    public DbSet<PlaceFrequentBy>       PlaceFrequentedBy     => Set<PlaceFrequentBy>();
    public DbSet<PlaceNotableLocation>  PlaceNotableLocations => Set<PlaceNotableLocation>();
    public DbSet<PlaceRelatedEntity>    PlaceRelatedEntities  => Set<PlaceRelatedEntity>();
    public DbSet<Faction>        Factions        => Set<Faction>();
    public DbSet<FactionAlias>            FactionAliases        => Set<FactionAlias>();
    public DbSet<FactionMethod>           FactionMethods        => Set<FactionMethod>();
    public DbSet<FactionResource>         FactionResources      => Set<FactionResource>();
    public DbSet<FactionGoal>             FactionGoals          => Set<FactionGoal>();
    public DbSet<FactionStoryHook>        FactionStoryHooks     => Set<FactionStoryHook>();
    public DbSet<FactionRelationshipRow>  FactionRelationships      => Set<FactionRelationshipRow>();
    public DbSet<FactionRelationshipTag>  FactionRelationshipTags   => Set<FactionRelationshipTag>();
    public DbSet<FactionMemberRow>        FactionMembers            => Set<FactionMemberRow>();
    public DbSet<CorponationCommonName>   CorponationCommonNames => Set<CorponationCommonName>();
    public DbSet<SubsidiaryProduct>       SubsidiaryProducts     => Set<SubsidiaryProduct>();
    public DbSet<AutomatonAlias>          AutomatonAliases       => Set<AutomatonAlias>();
    public DbSet<AutomatonArmament>       AutomatonArmament      => Set<AutomatonArmament>();
    public DbSet<AutomatonSensor>         AutomatonSensors       => Set<AutomatonSensor>();
    public DbSet<AutomatonDeployment>     AutomatonDeployments   => Set<AutomatonDeployment>();
    public DbSet<AutomatonStoryHook>      AutomatonStoryHooks    => Set<AutomatonStoryHook>();
    // Gear-cluster bridges
    public DbSet<WeaponAlias>           WeaponAliases             => Set<WeaponAlias>();
    public DbSet<WeaponBaseTechnology>  WeaponBaseTechnologies    => Set<WeaponBaseTechnology>();
    public DbSet<WeaponKnownUser>       WeaponKnownUsers          => Set<WeaponKnownUser>();
    public DbSet<WeaponAmmunitionType>  WeaponAmmunitionTypes     => Set<WeaponAmmunitionType>();
    public DbSet<WeaponStoryHook>       WeaponStoryHooks          => Set<WeaponStoryHook>();
    public DbSet<EquipmentAlias>          EquipmentAliases          => Set<EquipmentAlias>();
    public DbSet<EquipmentBaseTechnology> EquipmentBaseTechnologies => Set<EquipmentBaseTechnology>();
    public DbSet<EquipmentKnownUser>      EquipmentKnownUsers       => Set<EquipmentKnownUser>();
    public DbSet<EquipmentSpecification>  EquipmentSpecifications   => Set<EquipmentSpecification>();
    public DbSet<EquipmentStoryHook>      EquipmentStoryHooks       => Set<EquipmentStoryHook>();
    public DbSet<CyberwareItemAlias>      CyberwareItemAliases      => Set<CyberwareItemAlias>();
    public DbSet<CyberwareItemSideEffect> CyberwareItemSideEffects  => Set<CyberwareItemSideEffect>();
    public DbSet<CyberwareItemKnownUser>  CyberwareItemKnownUsers   => Set<CyberwareItemKnownUser>();
    public DbSet<CyberwareItemStoryHook>  CyberwareItemStoryHooks   => Set<CyberwareItemStoryHook>();
    public DbSet<ApparelAlias>            ApparelAliases            => Set<ApparelAlias>();
    public DbSet<ApparelMaterial>         ApparelMaterials          => Set<ApparelMaterial>();
    public DbSet<ApparelWornBy>           ApparelWornByRows         => Set<ApparelWornBy>();
    public DbSet<ApparelStoryHook>        ApparelStoryHooks         => Set<ApparelStoryHook>();
    public DbSet<AmmunitionAlias>            AmmunitionAliases            => Set<AmmunitionAlias>();
    public DbSet<AmmunitionCompatibleWeapon> AmmunitionCompatibleWeapons  => Set<AmmunitionCompatibleWeapon>();
    public DbSet<AmmunitionVariant>          AmmunitionVariants           => Set<AmmunitionVariant>();
    public DbSet<AmmunitionStoryHook>        AmmunitionStoryHooks         => Set<AmmunitionStoryHook>();
    public DbSet<PharmAlias>      PharmaceuticalAliases     => Set<PharmAlias>();
    public DbSet<PharmEffect>     PharmaceuticalEffects     => Set<PharmEffect>();
    public DbSet<PharmSideEffect> PharmaceuticalSideEffects => Set<PharmSideEffect>();
    public DbSet<PharmStoryHook>  PharmaceuticalStoryHooks  => Set<PharmStoryHook>();
    public DbSet<GenemodAlias>      GenemodAliases      => Set<GenemodAlias>();
    public DbSet<GenemodSideEffect> GenemodSideEffects  => Set<GenemodSideEffect>();
    public DbSet<GenemodStoryHook>  GenemodStoryHooks   => Set<GenemodStoryHook>();
    public DbSet<MaterialAlias>       MaterialAliases       => Set<MaterialAlias>();
    public DbSet<MaterialProperty>    MaterialProperties    => Set<MaterialProperty>();
    public DbSet<MaterialDeveloper>   MaterialDevelopers    => Set<MaterialDeveloper>();
    public DbSet<MaterialApplication> MaterialApplications  => Set<MaterialApplication>();
    public DbSet<MaterialStoryHook>   MaterialStoryHooks    => Set<MaterialStoryHook>();
    public DbSet<TransportationAlias>     TransportationAliases     => Set<TransportationAlias>();
    public DbSet<TransportationStoryHook> TransportationStoryHooks  => Set<TransportationStoryHook>();
    public DbSet<ConsumerGoodAlias>     ConsumerGoodAliases     => Set<ConsumerGoodAlias>();
    public DbSet<ConsumerGoodStoryHook> ConsumerGoodStoryHooks  => Set<ConsumerGoodStoryHook>();
    // Misc cluster — fully relational, replaces DataJson
    public DbSet<ArchetypeWillAlways>     ArchetypeWillAlways      => Set<ArchetypeWillAlways>();
    public DbSet<ArchetypeWillNever>      ArchetypeWillNever       => Set<ArchetypeWillNever>();
    public DbSet<ArchetypeUnless>         ArchetypeUnless          => Set<ArchetypeUnless>();
    public DbSet<ArchetypeSimilar>        ArchetypeSimilars        => Set<ArchetypeSimilar>();
    public DbSet<ArchetypeOpposite>       ArchetypeOpposites       => Set<ArchetypeOpposite>();
    public DbSet<NewsEntityInvolved>      NewsEntitiesInvolved     => Set<NewsEntityInvolved>();
    public DbSet<NewsLocation>            NewsLocations            => Set<NewsLocation>();
    public DbSet<ContractBonusRow>        ContractBonuses          => Set<ContractBonusRow>();
    public DbSet<ContractComplication>    ContractComplications    => Set<ContractComplication>();
    public DbSet<DocumentHeading>         DocumentHeadings         => Set<DocumentHeading>();
    public DbSet<LabSpecimenAlias>          LabSpecimenAliases         => Set<LabSpecimenAlias>();
    public DbSet<LabSpecimenKnownLocation>  LabSpecimenKnownLocations  => Set<LabSpecimenKnownLocation>();
    public DbSet<LabSpecimenStoryHook>      LabSpecimenStoryHooks      => Set<LabSpecimenStoryHook>();
    public DbSet<PsionicAlias>             PsionicAliases             => Set<PsionicAlias>();
    public DbSet<PsionicKnownPractitioner> PsionicKnownPractitioners  => Set<PsionicKnownPractitioner>();
    public DbSet<PsionicStoryHook>         PsionicStoryHooks          => Set<PsionicStoryHook>();
    public DbSet<Technology>                Technologies              => Set<Technology>();
    public DbSet<TechnologyAlias>           TechnologyAliases         => Set<TechnologyAlias>();
    public DbSet<TechnologyDeveloper>       TechnologyDevelopers      => Set<TechnologyDeveloper>();
    public DbSet<TechnologyBaseTechnology>  TechnologyBaseTechnologies => Set<TechnologyBaseTechnology>();
    public DbSet<TechnologyEnables>         TechnologyEnabledList     => Set<TechnologyEnables>();
    public DbSet<TechnologyStoryHook>       TechnologyStoryHooks      => Set<TechnologyStoryHook>();
    public DbSet<Motif>                    Motifs                  => Set<Motif>();
    public DbSet<MotifAppearance>          MotifAppearances        => Set<MotifAppearance>();
    public DbSet<Entertainment>            EntertainmentItems      => Set<Entertainment>();
    public DbSet<EntertainmentAlias>       EntertainmentAliases    => Set<EntertainmentAlias>();
    public DbSet<EntertainmentKnownFan>    EntertainmentKnownFans  => Set<EntertainmentKnownFan>();
    public DbSet<EntertainmentStoryHook>   EntertainmentStoryHooks => Set<EntertainmentStoryHook>();
    public DbSet<FlyoverEntity>            FlyoverEntities         => Set<FlyoverEntity>();
    public DbSet<FlyoverEntityAlias>       FlyoverEntityAliases    => Set<FlyoverEntityAlias>();
    public DbSet<FlyoverEntityKnownLocation> FlyoverEntityKnownLocations => Set<FlyoverEntityKnownLocation>();
    public DbSet<FlyoverEntityStoryHook>   FlyoverEntityStoryHooks => Set<FlyoverEntityStoryHook>();
    public DbSet<SyntheticLife>                 SyntheticLives              => Set<SyntheticLife>();
    public DbSet<SyntheticLifeAlias>            SyntheticLifeAliases        => Set<SyntheticLifeAlias>();
    public DbSet<SyntheticLifeKnownAssociation> SyntheticLifeKnownAssociations => Set<SyntheticLifeKnownAssociation>();
    public DbSet<SyntheticLifeStoryHook>        SyntheticLifeStoryHooks     => Set<SyntheticLifeStoryHook>();
    // CeramicMan tables retired 2026-05-06 — folded into SyntheticLives (Type == "ceramic_man").
    // Book / chapter relational bridges
    public DbSet<BookProtagonist>          BookProtagonists        => Set<BookProtagonist>();
    public DbSet<BookChapterOrder>         BookChapterOrder        => Set<BookChapterOrder>();
    public DbSet<ChapterCharacter>         ChapterCharacters       => Set<ChapterCharacter>();
    public DbSet<Corponation>    Corponations    => Set<Corponation>();
    public DbSet<Subsidiary>     Subsidiaries    => Set<Subsidiary>();
    public DbSet<Automaton>      Automata        => Set<Automaton>();
    public DbSet<Weapon>         Weapons         => Set<Weapon>();
    public DbSet<Equipment>      EquipmentItems  => Set<Equipment>();
    public DbSet<Cyberware>      CyberwareItems  => Set<Cyberware>();
    public DbSet<Apparel>        Apparels        => Set<Apparel>();
    public DbSet<Ammunition>     Ammunitions     => Set<Ammunition>();
    public DbSet<Pharmaceutical> Pharmaceuticals => Set<Pharmaceutical>();
    public DbSet<Genemod>        Genemods        => Set<Genemod>();
    public DbSet<Material>       Materials       => Set<Material>();
    public DbSet<Transportation> Transportations => Set<Transportation>();
    public DbSet<ConsumerGood>   ConsumerGoods   => Set<ConsumerGood>();
    public DbSet<ArchetypeRow>   Archetypes      => Set<ArchetypeRow>();
    public DbSet<QuoteEntity>    Quotes          => Set<QuoteEntity>();
    public DbSet<NewsEntity>     News            => Set<NewsEntity>();
    public DbSet<ContractEntity> Contracts       => Set<ContractEntity>();
    public DbSet<DocumentEntity> Documents       => Set<DocumentEntity>();
    public DbSet<MarkdownFile>   MarkdownFiles   => Set<MarkdownFile>();
    public DbSet<Vocabulary>     VocabularyEntries => Set<Vocabulary>();
    public DbSet<LabSpecimen>    LabSpecimens    => Set<LabSpecimen>();
    public DbSet<Psionic>        Psionics        => Set<Psionic>();

    // Books / chapters / beats
    public DbSet<Book>           Books         => Set<Book>();
    public DbSet<Series>         SeriesItems   => Set<Series>();
    public DbSet<Chapter>        Chapters      => Set<Chapter>();
    public DbSet<ChapterBeat>    ChapterBeats  => Set<ChapterBeat>();

    // Single-document settings (tone bible, story bible, literary rules, character profile)
    public DbSet<Setting>                Settings                => Set<Setting>();

    // Continuity store (was a separate SQLite DB; folded into StreetSamurai)
    public DbSet<ContinuityClaim>        ContinuityClaims        => Set<ContinuityClaim>();
    public DbSet<ClaimContradictionRow>  ClaimContradictions     => Set<ClaimContradictionRow>();
    public DbSet<ClaimConfirmationRow>   ClaimConfirmations      => Set<ClaimConfirmationRow>();
    public DbSet<ExtractionRunRow>       ExtractionRuns          => Set<ExtractionRunRow>();

    // World-state ledger — append-only stream of (entity, aspect, verb, value)
    // changes timestamped to story-time. The "current" state is the latest row
    // per (EntityId, AspectKey); as-of queries pivot on AtStoryTime.
    public DbSet<EntityStateEvent>       EntityStateEvents       => Set<EntityStateEvent>();

    // Per-weapon structured spec attributes (chambering, capacity, action, …)
    // so canon facts are queryable instead of buried in free-form prose.
    public DbSet<WeaponSpec>             WeaponSpecs             => Set<WeaponSpec>();

    // Cached cloud-LLM embedding per entity. Non-temporal so it's eligible
    // for SQL Server 2025 vector indexes when the corpus crosses ~50k.
    public DbSet<EntityEmbedding>        EntityEmbeddings        => Set<EntityEmbedding>();

    // Polymorphic prose embeddings (ScopeKind = 'chapter' | 'beat').
    public DbSet<ProseEmbedding>         ProseEmbeddings         => Set<ProseEmbedding>();

    // Plant/payoff registry — "reward re-reading without requiring it."
    public DbSet<PlantPayoff>            PlantPayoffs            => Set<PlantPayoff>();

    // Per-beat prose quality metrics (CPU-only; upserted nightly by ss --compute-metrics).
    public DbSet<BeatProseMetrics>       BeatProseMetrics        => Set<BeatProseMetrics>();

    // Structural blueprints — pre-prose anti-tell commitments per story node
    // (StoryScope countermeasures: subplot, chronology, resolution mode, escalation
    // curve, event palette, ending style, intertextual anchors). Generated by
    // StructuralBlueprintService; verified by StoryScopeAuditService.
    public DbSet<NodeStructuralBlueprint>        NodeStructuralBlueprints        => Set<NodeStructuralBlueprint>();
    public DbSet<NodeStructuralBlueprintBeatTag> NodeStructuralBlueprintBeatTags => Set<NodeStructuralBlueprintBeatTag>();
    public DbSet<EditSession>     EditSessions     => Set<EditSession>();
    public DbSet<EditSessionBeat> EditSessionBeats => Set<EditSessionBeat>();

    // Consensus-cliché blocklist — narrative devices LLMs converge on, flagged by
    // StoryScope audits; at FlagCount >= 2 they enter the generation-time
    // anti-pattern block for their universe.
    public DbSet<ConsensusCliche>        ConsensusCliches        => Set<ConsensusCliche>();

    // Cached per-unit progressive readings (stakes/event/revelation) from the
    // StoryScope audit, keyed by unit-first BeatId and invalidated by prose hash —
    // re-audits only re-read units whose text changed (mirrors Legion's
    // BeatTextHash ballot caching).
    public DbSet<StructuralReading>      StructuralReadings      => Set<StructuralReading>();

    // Beat duel verdicts — blind A/B panel decisions on beat rewrites, cached by
    // the SHA-256 pair of both texts. SS-A44: duels are votes; explicit ask only.
    public DbSet<BeatDuelVerdict>        BeatDuelVerdicts        => Set<BeatDuelVerdict>();

    // Workflow monitoring — tracks which prose services were active per beat write.
    // Populated by ProseWriterRouter. Query via ss --workflow-status or workflow_status MCP tools.
    public DbSet<BeatServiceLog>         BeatServiceLogs         => Set<BeatServiceLog>();
    public DbSet<BeatModeLog>            BeatModeLogs            => Set<BeatModeLog>();

    // Cost tracking — append-only log of CLI command cost history.
    // Populated by CostGateCli.RecordActualAsync; queried by CommandCostEstimatorService to self-calibrate.
    public DbSet<CommandCostHistory>     CommandCostHistories    => Set<CommandCostHistory>();

    // Injectable context overrides — user-managed pin/exclude list for DocContextStack.
    // Managed by UserContextService; applied by DocContextService.PrepareForNodeAsync.
    // Rows expire after 24 hours or on explicit --context clear.
    public DbSet<ContextOverride>        ContextOverrides        => Set<ContextOverride>();

    // Liberty reports — per-beat creative-departure analysis (Rule of Cool).
    // Written by LibertyReportService post-write; queried via --liberty-report and get_liberty_report MCP.
    public DbSet<LibertyReport>          LibertyReports          => Set<LibertyReport>();

    // Media assets (cover images, logos, watermarks).
    // Import via ss --import-cover; generate via ss --generate-cover.
    public DbSet<MediaItem>              Media                   => Set<MediaItem>();

    // Noun consistency — registry of renamed/retired noun references that must
    // not appear in prose. Scanned by NounConsistencyService / validate_nouns MCP.
    public DbSet<DeprecatedEntityName>   DeprecatedEntityNames   => Set<DeprecatedEntityName>();

    // Canon-sync surveys — persisted questions, answers, and apply logs so the
    // full decision trail survives across sessions. Managed by SurveyService /
    // survey MCP tools / ss --list-surveys / ss --get-survey.
    public DbSet<Survey>         Surveys         => Set<Survey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();

    // Truth-First Architecture (Track A) — DB-resident canon documents replace
    // hand-editable .md files. .md files become generated read-only artifacts.
    // Edits go through set_canon_section MCP → CanonDocumentSection row →
    // generate_canon_md regenerates the file and updates LastChecksum.
    public DbSet<CanonDocument>        CanonDocuments        => Set<CanonDocument>();
    public DbSet<CanonDocumentSection> CanonDocumentSections => Set<CanonDocumentSection>();

    // Truth-First Architecture (Track A) — structured NodeBible sections replace
    // the Nodes.NodeBible text blob. Edits go through set_story_bible_section MCP.
    public DbSet<NodeBibleSection> NodeBibleSections => Set<NodeBibleSection>();

    // Truth-First Architecture (Track B) — per-beat structural contract.
    // Replaces EscalationCurveJson / EventTypePaletteJson blobs. One row per beat.
    // Required precondition for prose generation (B5); read by Track C for verification.
    public DbSet<BeatBlueprintDecision> BeatBlueprintDecisions => Set<BeatBlueprintDecision>();

    // Truth-First Architecture (Track B) — world-state timeline.
    // Tracks entity state changes at each beat: KnowledgeGained, LocationChange, etc.
    // Populated by B4 backfill and by B5 (WorldStatePost declared after generation).
    public DbSet<EntityStateAtBeat> EntityStateAtBeats => Set<EntityStateAtBeat>();

    // Truth-First Architecture (Track C) — beat verification results.
    // One row per (BeatId, CheckType); upserted on re-verify. BLOCKER results
    // block codex doctor and ss --publish (INV-05).
    public DbSet<BeatVerification> BeatVerifications => Set<BeatVerification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ── Universe (multi-tenant root) ────────────────────────────────────
        // Singular table name to match add_universe_20260615.sql. The scoped
        // roots carry a plain UniverseId column (no navigation / no enforced FK):
        // integrity is kept by StampUniverseOnAdded, which keeps test/clean-build
        // DBs from tripping on an unseeded Universe table.
        b.Entity<Universe>(e =>
        {
            e.ToTable("Universe");
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.Name).HasMaxLength(400).IsRequired();
            e.Property(x => x.Theme).HasMaxLength(100);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── Episode (bedtime-adventure domain) ──────────────────────────────
        b.Entity<Episode>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Seed).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.Property(x => x.VoiceId).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.StartedAt);
            e.HasIndex(x => x.Status);
        });
        b.Entity<EpisodeBeat>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Episode).WithMany(x => x.Beats)
                .HasForeignKey(x => x.EpisodeId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.AudioPath).HasMaxLength(400);
            e.HasIndex(x => new { x.EpisodeId, x.Index }).IsUnique();
        });
        b.Entity<EpisodeCorrection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Episode).WithMany(x => x.Corrections)
                .HasForeignKey(x => x.EpisodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EpisodeId);
            e.HasIndex(x => x.Applied);
        });
        b.Entity<EpisodeSurvey>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Episode).WithOne(x => x.Survey!)
                .HasForeignKey<EpisodeSurvey>(x => x.EpisodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EpisodeId).IsUnique();
        });

        // ── Unified node schema ───────────────────────────────────────────
        b.Entity<Beat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).IsRequired();
            e.Property(x => x.SceneType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200);
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.AudioPath).HasMaxLength(400);
            e.Property(x => x.TextHash).HasMaxLength(80);
            e.Property(x => x.LastRequestId).HasMaxLength(120);
            e.HasIndex(x => x.Slug);
            // Beat.Number is the stable "Beat #134" handle the CLI and writer
            // surface to humans. NextBeatNumberAsync allocates as MAX+1; the
            // unique index is the safety net that fails one of two racing
            // inserts instead of letting them silently share a number.
            e.HasIndex(x => x.Number).IsUnique();
        });
        b.Entity<Node>(e =>
        {
            // Table-per-hierarchy: SeriesNode / StoryNode / ChapterNode share
            // the Nodes table, discriminated by NodeType. Kind remains the
            // free-form display label; NodeType is the structural truth.
            e.ToTable("Nodes");
            e.HasDiscriminator<string>("NodeType")
                .HasValue<SeriesNode>("series")
                .HasValue<StoryNode>("story")
                .HasValue<ChapterNode>("chapter");
            e.Property("NodeType").HasMaxLength(20);
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(40).IsRequired();
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.Property(x => x.VoiceId).HasMaxLength(80);
            e.Property(x => x.CombinedAudioPath).HasMaxLength(400);
            // Slug unique per universe (the same node slug may recur in another universe).
            e.HasIndex(x => new { x.UniverseId, x.Slug }).IsUnique().HasDatabaseName("UX_Nodes_Universe_Slug");
            e.HasIndex(x => x.Kind);
            e.HasIndex(x => new { x.ParentNodeId, x.SortKey });
            e.HasIndex(x => x.UniverseId);
            e.Property(x => x.Author).HasMaxLength(200);
            e.HasOne(x => x.ParentNode).WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentNodeId).OnDelete(DeleteBehavior.Restrict);
            // PreviousNode: null = gateway (first/standalone); set = sequel.
            e.HasOne(x => x.PreviousNode).WithMany()
                .HasForeignKey(x => x.PreviousNodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.PreviousNodeId);
            // Universe scoping (SS-LAW-15). No-op when ScopedUniverseId is Guid.Empty.
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
        });
        b.Entity<BeatNode>(e =>
        {
            e.HasKey(x => new { x.NodeId, x.BeatId });
            e.HasOne(x => x.Node).WithMany(x => x.BeatNodes)
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Beat).WithMany(x => x.BeatNodes)
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NodeId, x.SortKey });
            e.HasIndex(x => x.BeatId);
        });
        b.Entity<PlantPayoff>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PlantDescription).HasMaxLength(500).IsRequired();
            e.Property(x => x.PayoffDescription).HasMaxLength(500).IsRequired();
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.TransparencyNote).HasMaxLength(500);
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.PlantBeat).WithMany()
                .HasForeignKey(x => x.PlantBeatId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PayoffBeat).WithMany()
                .HasForeignKey(x => x.PayoffBeatId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => x.PlantBeatId);
            e.HasIndex(x => x.PayoffBeatId);
        });

        // ── Structural blueprints (StoryScope countermeasures) ───────────────
        b.Entity<NodeStructuralBlueprint>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.NodeId).IsUnique();
            e.HasIndex(x => x.UniverseId);
            e.Property(x => x.TemporalScheme).HasMaxLength(20).IsRequired();
            e.Property(x => x.ResolutionMode).HasMaxLength(20).IsRequired();
            e.Property(x => x.MoralPolarity).HasMaxLength(20).IsRequired();
            e.Property(x => x.EndingStyle).HasMaxLength(20).IsRequired();
            e.Property(x => x.GeneratedBy).HasMaxLength(20).IsRequired();
            e.Property(x => x.Granularity).HasMaxLength(20).IsRequired().HasDefaultValue("beat");
            e.Property(x => x.SubplotSummary).HasMaxLength(1000);
            e.Property(x => x.SubplotTheme).HasMaxLength(500);
            e.Property(x => x.AnachronyPlan).HasMaxLength(1000);
            e.Property(x => x.ResolutionNote).HasMaxLength(500);
            e.Property(x => x.MoralPolarityNote).HasMaxLength(500);
            e.Property(x => x.FormDevice).HasMaxLength(200);
            e.Property(x => x.EndingNote).HasMaxLength(500);
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NodeStructuralBlueprintBeatTag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TagType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Confirmed).HasDefaultValue(false);
            e.Property(x => x.ConfirmedBySessionId);
            e.HasOne(x => x.Blueprint).WithMany(x => x.BeatTags)
                .HasForeignKey(x => x.BlueprintId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Beat).WithMany()
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.BlueprintId);
            e.HasIndex(x => x.BeatId);
        });

        // ── Truth-First Architecture: Track A ────────────────────────────────

        b.Entity<CanonDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.LastChecksum).HasMaxLength(80);
            e.HasIndex(x => new { x.UniverseId, x.DocumentType }).IsUnique()
                .HasDatabaseName("UX_CanonDocuments_Universe_Type");
            e.HasIndex(x => x.UniverseId);
        });

        b.Entity<CanonDocumentSection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SectionKey).HasMaxLength(120).IsRequired();
            e.Property(x => x.SectionTitle).HasMaxLength(300);
            e.HasOne(x => x.Document).WithMany(x => x.Sections)
                .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DocumentId);
            e.HasIndex(x => new { x.DocumentId, x.SectionKey }).IsUnique()
                .HasDatabaseName("UX_CanonDocumentSections_Doc_Key");
            e.HasIndex(x => new { x.DocumentId, x.SortKey });
        });

        b.Entity<NodeBibleSection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SectionType).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => new { x.NodeId, x.SectionType }).IsUnique()
                .HasDatabaseName("UX_NodeBibleSections_Node_Type");
        });

        // ── Truth-First Architecture: Track B ────────────────────────────────

        b.Entity<BeatBlueprintDecision>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(60);
            e.Property(x => x.EscalationFloor).HasColumnType("decimal(4,2)");
            e.Property(x => x.AnachronyType).HasMaxLength(40);
            e.Property(x => x.PacingDirective).HasMaxLength(20);
            e.HasOne(x => x.Beat).WithMany()
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Blueprint).WithMany()
                .HasForeignKey(x => x.BlueprintId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.BeatId).IsUnique()
                .HasDatabaseName("UX_BeatBlueprintDecisions_Beat");
            e.HasIndex(x => x.BlueprintId);
        });

        b.Entity<EntityStateAtBeat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StateType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Source).HasMaxLength(20).IsRequired().HasDefaultValue("Inferred");
            e.HasOne(x => x.Entity).WithMany()
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Beat).WithMany()
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.EntityId, x.BeatId, x.StateType }).IsUnique()
                .HasDatabaseName("UX_EntityStateAtBeat_Entity_Beat_Type");
            e.HasIndex(x => new { x.NodeId, x.BeatId });
            e.HasIndex(x => x.EntityId);
        });

        // ── Truth-First Architecture: Track C ────────────────────────────────

        b.Entity<BeatVerification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CheckType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Result).HasMaxLength(20).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            e.Property(x => x.VerifiedBy).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Beat).WithMany()
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.BeatId, x.CheckType }).IsUnique()
                .HasDatabaseName("UX_BeatVerifications_Beat_CheckType");
            e.HasIndex(x => x.BeatId);
            e.HasIndex(x => new { x.Result, x.Severity });
        });
        b.Entity<ConsensusCliche>(e =>
        {
            e.ToTable("ConsensusCliches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Device).HasMaxLength(500).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.FirstFlaggedInSlug).HasMaxLength(200);
            e.Property(x => x.AddedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.UniverseId);
        });
        b.Entity<StructuralReading>(e =>
        {
            e.ToTable("StructuralReadings");
            e.HasKey(x => x.BeatId);
            e.Property(x => x.UnitHash).HasMaxLength(80).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(60).IsRequired();
            e.Property(x => x.RevelationMode).HasMaxLength(20).IsRequired();
        });
        b.Entity<BeatDuelVerdict>(e =>
        {
            e.ToTable("BeatDuelVerdicts");
            e.HasKey(x => x.Id);
            e.Property(x => x.OriginalHash).HasMaxLength(80).IsRequired();
            e.Property(x => x.RevisionHash).HasMaxLength(80).IsRequired();
            e.Property(x => x.Verdict).HasMaxLength(10).IsRequired();
            e.Property(x => x.Goal).HasMaxLength(500);
            e.HasIndex(x => new { x.OriginalHash, x.RevisionHash }).IsUnique();
            e.HasIndex(x => x.BeatId);
        });

        // ── Workflow monitoring ──────────────────────────────────────────────
        b.Entity<BeatServiceLog>(e =>
        {
            e.ToTable("BeatServiceLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Service).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => x.BeatId).HasFilter("[BeatId] IS NOT NULL");
            e.HasOne<Node>().WithMany().HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Beat>().WithMany().HasForeignKey(x => x.BeatId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<BeatModeLog>(e =>
        {
            e.ToTable("BeatModeLog");
            e.HasKey(x => x.BeatId);
            e.Property(x => x.Mode).HasMaxLength(50).IsRequired();
            e.Property(x => x.DetectionMethod).HasMaxLength(50).IsRequired();
            e.HasOne<Beat>().WithMany().HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── Media assets ─────────────────────────────────────────────────────
        b.ApplyConfiguration(new MediaItemTypeConfiguration());
        if (!Database.IsSqlServer())
        {
            // MediaItemTypeConfiguration pins SQL Server column types
            // (varbinary(max) / nvarchar(max)) that SQLite's DDL parser rejects,
            // which broke every TestDbFactory EnsureCreated. Let the provider
            // infer storage types on non-SQL-Server (SQLite unit tests).
            b.Entity<MediaItem>().Property(x => x.Bytes).HasColumnType(null);
            b.Entity<MediaItem>().Property(x => x.Extra).HasColumnType(null);
        }

        b.Entity<BeatEntityMention>(e =>
        {
            e.HasKey(x => new { x.BeatId, x.EntityId });
            e.HasOne(x => x.Beat).WithMany()
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Entity).WithMany()
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EntityId);
            e.Property(x => x.EntityName).HasMaxLength(200);
            e.Property(x => x.EntityType).HasMaxLength(50);
        });
        b.Entity<NodePublication>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Format).HasMaxLength(8).IsRequired();
            e.Property(x => x.Path).HasMaxLength(600);
            e.HasIndex(x => new { x.NodeId, x.StartedAt });
            e.HasOne(x => x.Node).WithMany(x => x.Publications)
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NodeAudioEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasMaxLength(40).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(1000);
            e.HasIndex(x => new { x.NodeId, x.At });
            e.HasIndex(x => x.PublicationId);
            // Node-scoped ledger; no hard FK to Node so recording an event
            // never fails on a transient node-row state, and publication
            // linkage is a soft id (events outlive a deleted publication row).
        });
        b.Entity<NodeReview>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PersonaId).HasMaxLength(40).IsRequired();
            e.Property(x => x.PersonaName).HasMaxLength(80).IsRequired();
            e.Property(x => x.PersonaBlurb).HasMaxLength(400);
            e.Property(x => x.ProviderId).HasMaxLength(40).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80);
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => new { x.NodeId, x.ReviewedAt });
            e.HasOne(x => x.Node).WithMany(x => x.Reviews)
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NodeReviewSummary>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => x.NodeId).IsUnique();
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NodeScoreHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => new { x.NodeId, x.RecordedAt });
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<VoiceChangeLogEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Source).HasMaxLength(20).IsRequired();
            e.Property(x => x.RuleTarget).HasMaxLength(80);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.NodeId);
            // No FK to Nodes: entries outlive the nodes they were learned from.
        });
        b.Entity<Species>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(40).IsRequired();
            e.Property(x => x.Label).HasMaxLength(80);
            // Species set is per-universe (SS-LAW-4): Name unique within a universe.
            e.HasIndex(x => new { x.UniverseId, x.Name }).IsUnique().HasDatabaseName("UX_Species_Universe_Name");
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
        });
        b.Entity<FocusGroup>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.Name).IsUnique();
        });
        b.Entity<FocusGroupMember>(e =>
        {
            e.HasKey(x => new { x.FocusGroupId, x.PersonaId });
            e.Property(x => x.PersonaId).HasMaxLength(40).IsRequired();
            e.Property(x => x.PersonaName).HasMaxLength(80).IsRequired();
            e.Property(x => x.PersonaBlurb).HasMaxLength(400);
            e.HasOne(x => x.FocusGroup).WithMany(g => g.Members)
                .HasForeignKey(x => x.FocusGroupId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NodeReviewBeatScore>(e =>
        {
            e.HasKey(x => new { x.ReviewId, x.BeatNumber });
            e.HasOne(x => x.Review).WithMany(r => r.BeatScores)
                .HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.BeatTextHash).HasMaxLength(80);
        });

        // ── Emotional Intelligence Examination (SS-A15) ──────────────────────
        b.Entity<EmotionalExamination>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EffortTier).HasMaxLength(20).IsRequired();
            e.Property(x => x.Register).HasMaxLength(40).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80);
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NodeId, x.ExaminedAt });
        });

        b.Entity<EmotionalDimensionResult>(e =>
        {
            e.HasKey(x => new { x.ExaminationId, x.Dimension });
            e.HasOne(x => x.Examination).WithMany(r => r.DimensionResults)
                .HasForeignKey(x => x.ExaminationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<EmotionalBeatScore>(e =>
        {
            e.HasKey(x => new { x.ExaminationId, x.BeatNumber });
            e.HasOne(x => x.Examination).WithMany(r => r.BeatScores)
                .HasForeignKey(x => x.ExaminationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CharacterEmotionalLedger>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Character).HasMaxLength(200).IsRequired();
            e.Property(x => x.VoiceRegister).HasMaxLength(2000);
            e.Property(x => x.SourceBibleHash).HasMaxLength(64);
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.NodeId, x.Character }).IsUnique();
        });

        b.Entity<EntityReview>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(400).IsRequired();
            e.Property(x => x.PersonaId).HasMaxLength(40).IsRequired();
            e.Property(x => x.PersonaName).HasMaxLength(80).IsRequired();
            e.Property(x => x.PersonaBlurb).HasMaxLength(400);
            e.Property(x => x.ProviderId).HasMaxLength(40).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80);
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => new { x.EntityId, x.EntityType, x.ReviewedAt });
            e.HasIndex(x => new { x.EntityType, x.ReviewedAt });
        });

        b.Entity<EntityReviewSummary>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(400).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => new { x.EntityId, x.EntityType }).IsUnique();
        });

        b.Entity<EntityReviewQueue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(400).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.ClaimedBy).HasMaxLength(100);
            e.HasIndex(x => new { x.Status, x.ClaimedAt });
            e.HasIndex(x => x.EntityId);
        });

        b.Entity<DistributedWorkQueue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.WorkType).HasMaxLength(40).IsRequired();
            e.Property(x => x.TargetId).HasMaxLength(64).IsRequired();
            e.Property(x => x.TargetType).HasMaxLength(40).IsRequired();
            e.Property(x => x.TargetName).HasMaxLength(400).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.ClaimedBy).HasMaxLength(100);
            e.HasIndex(x => new { x.WorkType, x.Status, x.ClaimedAt });
            e.HasIndex(x => x.TargetId);
        });

        // ── Entity (universal) ───────────────────────────────────────────────
        b.Entity<Entity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            e.Property(x => x.Name).HasMaxLength(400).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(400).IsRequired();
            e.Property(x => x.Status).HasMaxLength(40);
            // Slug is unique per (universe, type) — a place "Silence" and a weapon
            // "Silence" are valid, AND the same (type, slug) may recur in another
            // universe (SS-LAW-15). Wiki-link resolution disambiguates by type.
            e.HasIndex(x => new { x.UniverseId, x.EntityType, x.Slug })
                .IsUnique().HasDatabaseName("UX_Entities_Universe_Type_Slug");
            e.HasIndex(x => x.Slug);
            e.HasIndex(x => x.EntityType);
            // Filtered index — most pages query active rows only; archived rows
            // are still indexed by Slug + Type for restore flows.
            e.HasIndex(x => new { x.EntityType, x.IsActive })
                .HasFilter("[IsActive] = 1");
            // 23rd-century in-world creation date — supports "what was canon as of 2256-04-15".
            e.HasIndex(x => x.InWorldCreatedDate);
            // "Recently modified" lists on dashboard / world-health / activity
            // feeds order by ModifiedAt — filter on active so archived churn
            // doesn't pollute the hot path.
            e.HasIndex(x => x.ModifiedAt)
                .HasDatabaseName("IX_Entities_ModifiedAt_Active")
                .HasFilter("[IsActive] = 1");
            e.HasIndex(x => x.UniverseId);
            // Universe scoping (SS-LAW-15) — the single point that transitively scopes EVERY
            // entity type: EfRepository reads via Records→Entity, and the character read paths
            // derive their id-set from Entities. No-op when ScopedUniverseId is Guid.Empty.
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
        });

        // ── Record (1:1 canonical JSON for an entity) ────────────────────────
        b.Entity<Record>(e =>
        {
            e.HasKey(x => x.EntityId);
            e.HasOne(x => x.Entity).WithOne(x => x.Record!)
                .HasForeignKey<Record>(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            // WorldGraphService.IsStale() probes max(UpdatedAt) on every cold
            // start; chapter / book repos do OrderByDescending(r => r.UpdatedAt)
            // for recent-first lists. Without this it was a full scan of every
            // row's UpdatedAt — multi-second on a populated canon.
            e.HasIndex(x => x.UpdatedAt).HasDatabaseName("IX_Records_UpdatedAt");
        });

        // ── EntityProperty (flex bag) ────────────────────────────────────────
        b.Entity<EntityProperty>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PropertyKey).HasMaxLength(120).IsRequired();
            e.Property(x => x.ValueKind).HasMaxLength(20);
            e.Property(x => x.Source).HasMaxLength(200);
            e.HasOne(x => x.Entity).WithMany(x => x.Properties)
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EntityId, x.PropertyKey, x.StoryValidFrom });
            e.HasIndex(x => x.PropertyKey);
        });

        // ── Edge (typed temporal relationships) ──────────────────────────────
        b.Entity<Edge>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RelationType).HasMaxLength(80).IsRequired();
            e.Property(x => x.Sentiment).HasMaxLength(20);
            e.Property(x => x.Source).HasMaxLength(200);
            e.HasOne(x => x.SourceEntity).WithMany(x => x.OutgoingEdges)
                .HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetEntity).WithMany(x => x.IncomingEdges)
                .HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SourceId, x.RelationType, x.StoryValidFrom });
            e.HasIndex(x => new { x.TargetId, x.RelationType, x.StoryValidFrom });
            e.HasIndex(x => x.UniverseId);
            // Universe scoping (RFC 0006). Source/target share a universe; no-op when unscoped.
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
            // Filtered indexes — "current edges only" is the hot read path
            // (family ties, lives_at, member_of, deployed_at all use
            // StoryValidUntil IS NULL). Without the filter SQL Server scans the
            // history portion of the keys; with it the index is ~10× smaller
            // and ~10× faster on current-only lookups.
            e.HasIndex(x => new { x.SourceId, x.RelationType })
                .HasDatabaseName("IX_Edges_Source_Current")
                .HasFilter("[StoryValidUntil] IS NULL");
            e.HasIndex(x => new { x.TargetId, x.RelationType })
                .HasDatabaseName("IX_Edges_Target_Current")
                .HasFilter("[StoryValidUntil] IS NULL");
        });

        // ── Taxonomy ─────────────────────────────────────────────────────────
        b.Entity<Taxonomy>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Domain).HasMaxLength(40).IsRequired();
            e.Property(x => x.Code).HasMaxLength(80).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200);
            e.HasIndex(x => new { x.Domain, x.Code }).IsUnique();
            e.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<EntityTaxonomy>(e =>
        {
            e.HasKey(x => new { x.EntityId, x.TaxonomyId, x.StoryValidFrom });
            e.HasOne(x => x.Entity).WithMany(x => x.Taxonomies)
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Taxonomy).WithMany(x => x.EntityLinks)
                .HasForeignKey(x => x.TaxonomyId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.TaxonomyId);
        });

        // ── Tag ──────────────────────────────────────────────────────────────
        b.Entity<Tag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.EntityId).IsRequired(false);
            e.HasOne(x => x.Entity).WithMany()
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.EntityId);
        });

        b.Entity<EntityTag>(e =>
        {
            e.HasKey(x => new { x.EntityId, x.TagId });
            e.HasOne(x => x.Entity).WithMany(x => x.Tags)
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(x => x.EntityLinks)
                .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TagId);
        });

        // ── Character (TPT subtype, fully columnar) ──────────────────────────
        b.Entity<Character>(e =>
        {
            e.HasKey(x => x.Id);
            // Names — every character lookup goes through these, so all four are bounded
            // and indexed. FullName mirrors Entity.Name so this table stands alone.
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.FirstName).HasMaxLength(200);
            e.Property(x => x.MiddleName).HasMaxLength(200);
            e.Property(x => x.LastName).HasMaxLength(200);
            e.Property(x => x.TitlePrefix).HasMaxLength(40);
            // Identity / classification — bounded for indexing.
            e.Property(x => x.Species).HasMaxLength(40);
            e.Property(x => x.KindOfBeing).HasMaxLength(40);
            e.Property(x => x.Gender).HasMaxLength(40);
            e.Property(x => x.Pronouns).HasMaxLength(40);
            // Prose-content fields kept as NVARCHAR(MAX) — these can run hundreds
            // of characters describing physical detail, life history, etc. Earlier
            // bounded sizes (40/80/120) were silently rejecting >70% of imports.
            // LifeStatus / TerritoryRange / HairLength can be either a short tag
            // ("alive", "long") or a full sentence — leave unbounded.
            // HomeTurf / TerritoryHomeTurf flat columns dropped 2026-05-08 —
            // bridge is CharacterHomeTurfs (HomeTurfs navigation).
            // Belongings* scalar columns + their HasMaxLength dropped 2026-05-08.
            // Pointers now live as single-row buckets in CharacterBelongingsGear.

            e.HasOne(x => x.Entity).WithOne()
                .HasForeignKey<Character>(x => x.Id).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Species);
            e.HasIndex(x => x.KindOfBeing);
            // Name lookups: roster sort by surname, "everyone with first name X",
            // and full-name exact match all need their own index.
            e.HasIndex(x => x.Name).HasDatabaseName("IX_Characters_Name");
            e.HasIndex(x => x.FirstName).HasDatabaseName("IX_Characters_FirstName");
            e.HasIndex(x => x.LastName).HasDatabaseName("IX_Characters_LastName");
            e.HasIndex(x => new { x.LastName, x.FirstName }).HasDatabaseName("IX_Characters_LastFirst");
            // Affiliation / HomeTurf / TerritoryHomeTurf indexes retired
            // 2026-05-08 with the flat columns. Equivalent lookups go through
            // the CharacterAffiliations / CharacterHomeTurfs bridges, which
            // already index (CharacterId) and (PlaceId / FactionId).
            // Belongings indexes retired 2026-05-08 with the flat columns.
            // Equivalent lookups now go through CharacterBelongingsGear with
            // (CharacterId, Bucket) — already indexed by the bridge's PK.
        });

        // Derived read-model projection. PK = CharacterId (no FK / cascade: it's
        // decoupled from the canonical row on purpose — orphans are harmless and
        // pruned by `ss --rebuild-readmodel`). Intentionally NOT system-versioned.
        b.Entity<CharacterReadModel>(e =>
        {
            e.HasKey(x => x.CharacterId);
            // No HasMaxLength → nvarchar(max) on SQL Server; TEXT on SQLite tests.
            e.HasIndex(x => x.Version);
            e.HasIndex(x => x.UniverseId);
            // Universe scoping (RFC 0006). Defense-in-depth — read-model ids already come from the
            // filtered Entities set; this guards direct CharacterReadModels scans. No-op when unscoped.
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
        });

        // Per-character bridge tables — every list/dict/heterogeneous bag.
        b.Entity<CharacterAlias>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.Aliases)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Position });
            e.HasIndex(x => x.Value);
        });

        b.Entity<CharacterStoryHook>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Character).WithMany(x => x.StoryHooks)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Position });
        });

        b.Entity<CharacterArchetypeScore>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ArchetypeName).HasMaxLength(120);
            e.HasOne(x => x.Character).WithMany(x => x.ArchetypeScores)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.ArchetypeName }).IsUnique();
            e.HasIndex(x => x.ArchetypeName);
        });

        b.Entity<CharacterGeneticAncestry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Region).HasMaxLength(120);
            e.HasOne(x => x.Character).WithMany(x => x.GeneticAncestry)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Region }).IsUnique();
            e.HasIndex(x => x.Region);
        });

        b.Entity<CharacterAncestryDetail>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Region).HasMaxLength(120);
            e.Property(x => x.SubRegion).HasMaxLength(120);
            e.Property(x => x.Nationality).HasMaxLength(120);
            e.HasOne(x => x.Character).WithMany(x => x.AncestryDetail)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Region, x.SubRegion, x.Nationality }).IsUnique();
            e.HasIndex(x => x.Nationality);
        });

        b.Entity<CharacterPsychologyTrait>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.PsychologyTraits)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.Position });
        });

        b.Entity<CharacterSpeechPhrase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.SpeechPhrases)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.Position });
        });

        b.Entity<CharacterBehavioralRule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.BehavioralRules)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.Position });
        });

        b.Entity<CharacterBehavioralMap>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.Property(x => x.KeyName).HasMaxLength(200);
            e.HasOne(x => x.Character).WithMany(x => x.BehavioralMaps)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.KeyName }).IsUnique();
        });

        b.Entity<CharacterStatScalar>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.Property(x => x.KeyName).HasMaxLength(200);
            e.Property(x => x.ValueKind).HasMaxLength(20);
            e.HasOne(x => x.Character).WithMany(x => x.StatScalars)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.KeyName }).IsUnique();
            e.HasIndex(x => new { x.Bucket, x.KeyName });
        });

        b.Entity<CharacterStatPhrase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.StatPhrases)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.Position });
        });

        b.Entity<CharacterPhysicalMark>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Character).WithMany(x => x.PhysicalMarks)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Position });
        });

        b.Entity<CharacterTerritoryZone>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(20);
            e.Property(x => x.Zone).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.TerritoryZones)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.Position });
            e.HasIndex(x => x.Zone);
        });

        b.Entity<CharacterTerritoryReputation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Zone).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.TerritoryReputations)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Zone }).IsUnique();
        });

        b.Entity<CharacterBelongingsGear>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.BelongingsGear)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GearEntity)
                .WithMany()
                .HasForeignKey(x => x.GearEntityId)
                // NoAction avoids the multi-cascade-path conflict (this row
                // already cascades from Characters → Entities). Entities are
                // archived (IsActive=0), not deleted, so cleanup isn't needed.
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.CharacterId, x.Bucket, x.Position });
            // "Who owns weapon X" lookups need this; resolves to a real entity
            // when the importer / linker can match GearName → Entities.Id.
            e.HasIndex(x => x.GearEntityId);
        });

        b.Entity<CharacterBelongingsExtra>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.KeyName).HasMaxLength(200);
            e.HasOne(x => x.Character).WithMany(x => x.BelongingsExtras)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.KeyName }).IsUnique();
        });

        b.Entity<CharacterBioBatteryThreshold>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Threshold).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.BioBatteryThresholds)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Threshold }).IsUnique();
        });

        b.Entity<CharacterNeuralAbility>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Character).WithMany(x => x.NeuralAbilities)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Position });
            e.HasIndex(x => x.Name);
        });

        b.Entity<CharacterChangelogRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StoryId).HasMaxLength(80);
            e.Property(x => x.Beat).HasMaxLength(80);
            e.Property(x => x.FieldName).HasMaxLength(200);
            e.HasOne(x => x.Character).WithMany(x => x.Changelog)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Position });
            e.HasIndex(x => new { x.CharacterId, x.InWorldDate });
            e.HasIndex(x => x.StoryId);
        });

        b.Entity<CharacterCyberware>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            // BodyLocation / Tier / Condition often carry multi-clause prose
            // ("Tier 3 — infrastructure deployment, corponation-controlled…"),
            // so keep them NVARCHAR(MAX). Manufacturer stays at 450 because
            // it's a name and we want it indexable.
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.Cyberware)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CharacterId);
            e.HasIndex(x => x.Name);
        });

        b.Entity<CharacterKnowledgeRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Topic).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.Knowledge)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Topic });
            e.HasIndex(x => x.LearnedChapter);
        });

        b.Entity<CharacterKnowledgeEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityRef).HasMaxLength(80);
            e.HasOne(x => x.Knowledge).WithMany(x => x.RelatedEntities)
                .HasForeignKey(x => x.KnowledgeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.KnowledgeId, x.Position });
            e.HasIndex(x => x.EntityRef);
        });

        b.Entity<CharacterConditionRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasMaxLength(40);
            e.Property(x => x.Severity).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.Conditions)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.Kind });
        });

        b.Entity<CharacterRelationshipRow>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TargetName).HasMaxLength(400);
            e.Property(x => x.Status).HasMaxLength(40);
            e.HasOne(x => x.Character).WithMany(x => x.Relationships)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.TargetName });
            e.HasIndex(x => x.TargetEntityId);
        });

        b.Entity<CharacterTimelineEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StoryId).HasMaxLength(80);
            e.HasOne(x => x.Character).WithMany(x => x.Timeline)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CharacterId, x.StoryId });
            e.HasIndex(x => new { x.CharacterId, x.InWorldDate });
        });

        b.Entity<CharacterTimelineBodyChange>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.TimelineEvent).WithMany(x => x.BodyChanges)
                .HasForeignKey(x => x.TimelineEventId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TimelineEventId, x.Position });
        });

        // Resolved-entity bridges — keep Alias for fallback display, FK for joins.
        b.Entity<CharacterHomeTurf>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.HomeTurfs)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            // Restrict on the Place side: deleting a Place that's still referenced
            // by a character would silently break the character's record. The
            // archive flow (Entity.IsActive=false) is the right way to retire a place.
            e.HasOne(x => x.Place).WithMany()
                .HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CharacterId, x.Position });
            e.HasIndex(x => x.PlaceId);
            e.HasIndex(x => x.Alias);
        });

        b.Entity<CharacterAffiliation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Character).WithMany(x => x.Affiliations)
                .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Faction).WithMany()
                .HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CharacterId, x.Position });
            e.HasIndex(x => x.FactionId);
            e.HasIndex(x => x.Alias);
        });

        // ── Subtype tables (TPT — Id is PK + FK to Entity.Id) ────────────────
        // Each follows the same shape: small set of indexed columns + DataJson blob
        // for the rest of the source record. Indexes are tuned for the queries the
        // existing pages run today: list-by-tier, filter-by-manufacturer, etc.

        // Place — fully relational. Scalars + 10 bridge tables. Replaces the
        // old DataJson-backed Place subtype.
        b.Entity<Place>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Climate).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne()
                .HasForeignKey<Place>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Climate);
            e.HasIndex(x => x.Name);
        });
        ConfigurePlaceBridges(b);

        // Faction — fully relational. Scalars + 7 bridge tables. Replaces the
        // old DataJson-backed Faction subtype.
        b.Entity<Faction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Allegiance).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne()
                .HasForeignKey<Faction>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Allegiance);
            e.HasIndex(x => x.Name);
        });
        ConfigureFactionBridges(b);

        // Corponation — fully relational. CommonNames bridge.
        b.Entity<Corponation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.FullLegalName).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne()
                .HasForeignKey<Corponation>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Name);
        });
        b.Entity<CorponationCommonName>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.Corponation).WithMany(x => x.CommonNames)
                .HasForeignKey(x => x.CorponationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CorponationId, x.Position });
            e.HasIndex(x => x.Value);
        });

        // Subsidiary — fully relational. ParentCorponationId resolves to Entity.
        b.Entity<Subsidiary>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.ParentCorponationAlias).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne()
                .HasForeignKey<Subsidiary>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ParentCorponation).WithMany()
                .HasForeignKey(x => x.ParentCorponationId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ParentCorponationId);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.ParentCorponationAlias);
        });
        b.Entity<SubsidiaryProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Subsidiary).WithMany(x => x.KnownProducts)
                .HasForeignKey(x => x.SubsidiaryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SubsidiaryId, x.Position });
            e.HasIndex(x => x.ProductEntityId);
            e.HasIndex(x => x.Alias);
        });

        // Automaton — fully relational. Armament resolves to Weapon FK,
        // Deployments resolve to any-type entity FK.
        b.Entity<Automaton>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.KindOfBeing).HasMaxLength(40);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Operator).HasMaxLength(450);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Classification).HasMaxLength(120);
            e.Property(x => x.AutonomyLevel).HasMaxLength(80);
            e.HasOne(x => x.Entity).WithOne()
                .HasForeignKey<Automaton>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.KindOfBeing);
            e.HasIndex(x => x.Manufacturer);
            e.HasIndex(x => x.Operator);
            e.HasIndex(x => x.Classification);
            e.HasIndex(x => x.Name);
        });
        b.Entity<AutomatonAlias>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.Automaton).WithMany(x => x.Aliases)
                .HasForeignKey(x => x.AutomatonId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.AutomatonId, x.Position });
            e.HasIndex(x => x.Value);
        });
        b.Entity<AutomatonArmament>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Automaton).WithMany(x => x.Armament)
                .HasForeignKey(x => x.AutomatonId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Weapon).WithMany()
                .HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.AutomatonId, x.Position });
            e.HasIndex(x => x.WeaponId);
            e.HasIndex(x => x.Alias);
        });
        b.Entity<AutomatonSensor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SensorName).HasMaxLength(200);
            e.HasOne(x => x.Automaton).WithMany(x => x.Sensors)
                .HasForeignKey(x => x.AutomatonId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.AutomatonId, x.Position });
        });
        b.Entity<AutomatonDeployment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Automaton).WithMany(x => x.KnownDeployments)
                .HasForeignKey(x => x.AutomatonId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DeploymentEntity).WithMany()
                .HasForeignKey(x => x.DeploymentEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.AutomatonId, x.Position });
            e.HasIndex(x => x.DeploymentEntityId);
        });
        b.Entity<AutomatonStoryHook>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Automaton).WithMany(x => x.StoryHooks)
                .HasForeignKey(x => x.AutomatonId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.AutomatonId, x.Position });
        });
        ConfigureGear(b);
        ConfigureMisc(b);

        // ── Books / chapters / beats ─────────────────────────────────────────
        b.Entity<Book>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.Slug).HasMaxLength(400);
            e.HasIndex(x => new { x.UniverseId, x.Slug }).IsUnique().HasDatabaseName("UX_Books_Universe_Slug");
            e.HasIndex(x => x.SeriesId);
            // Universe scoping (SS-LAW-15). No-op when ScopedUniverseId is Guid.Empty.
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
        });
        b.Entity<BookProtagonist>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Book).WithMany(x => x.Protagonists).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.BookId, x.Position });
            e.HasIndex(x => x.CharacterId);
        });
        b.Entity<BookChapterOrder>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Book).WithMany(x => x.ChapterOrder).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Chapter).WithMany().HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.BookId, x.Position }).IsUnique();
            e.HasIndex(x => x.ChapterId);
        });

        b.Entity<Series>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(400);
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.Slug).HasMaxLength(400);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Name);
        });

        b.Entity<Chapter>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(400);
            e.HasIndex(x => x.BookId);
            e.HasIndex(x => new { x.BookId, x.Number });
            e.HasIndex(x => x.InWorldDate);
        });
        b.Entity<ChapterCharacter>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Chapter).WithMany(x => x.CharactersMentioned).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ChapterId, x.Position });
            e.HasIndex(x => x.CharacterId);
        });

        b.Entity<ChapterBeat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(400);
            e.HasOne(x => x.Chapter).WithMany(x => x.Beats)
                .HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ChapterId, x.Index });
            e.HasIndex(x => x.BeatGuid).IsUnique();
            // 23rd-century beat date — supports timeline scans by date range.
            e.HasIndex(x => x.InWorldDate);
        });

        // ── Settings (single-document) ────────────────────────────────────────
        b.Entity<Setting>(e =>
        {
            // Composite key so the same config Key recurs once per universe (RFC 0006).
            e.HasKey(x => new { x.Key, x.UniverseId });
            e.Property(x => x.Key).HasMaxLength(120);
            // Universe scoping: a row is visible when it belongs to the current universe OR is a
            // SHARED operational row (action_configs / tts.rules / users.accounts). No-op when
            // ScopedUniverseId is Guid.Empty (tests / pre-migration).
            e.HasQueryFilter(x =>
                ScopedUniverseId == Guid.Empty
                || x.UniverseId == ScopedUniverseId
                || x.UniverseId == SharedUniverseId);
        });

        // Runtime-defined repositories — global lookup, NOT universe-scoped (no query filter):
        // the definition is shared; universe separation happens on the Entity spine.
        b.Entity<RepositoryDefinition>(e =>
        {
            e.ToTable("RepositoryDefinitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.Icon).HasMaxLength(60);
            e.Property(x => x.RoutePath).HasMaxLength(120);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        // ── WeaponSpec (per-weapon structured key/value spec rows) ───────────
        b.Entity<WeaponSpec>(e =>
        {
            e.ToTable("WeaponSpecs");
            e.HasKey(x => x.Id);
            e.Property(x => x.SpecKey).HasMaxLength(80);
            e.HasOne(x => x.Weapon)
                .WithMany()
                .HasForeignKey(x => x.WeaponId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.WeaponId, x.SpecKey });
            e.HasIndex(x => x.SpecKey);
        });

        // ── EntityEmbedding (cloud-LLM embedding cache, non-temporal) ────────
        // PK = EntityId so each entity has at most one current embedding.
        // Deliberately non-temporal so the table is eligible for SQL Server
        // 2025 VECTOR INDEX when the corpus crosses ~50k. Until then we run
        // exact NN via cosine distance in C# over the JSON-encoded floats.
        b.Entity<EntityEmbedding>(e =>
        {
            e.ToTable("EntityEmbeddings");
            e.HasKey(x => x.EntityId);
            e.Property(x => x.SourceHash).HasMaxLength(32).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80);
            e.HasOne(x => x.Entity)
                .WithMany()
                .HasForeignKey(x => x.EntityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EmbeddedAt); // staleness sweeps
            // The Vector column (VECTOR(1536)) is intentionally NOT mapped —
            // EF Core has no native VECTOR support; EmbeddingService manages
            // it via raw SQL (MERGE writes, SqlQueryRaw reads with
            // VECTOR_DISTANCE).
        });

        // ── ProseEmbedding (polymorphic chapter+beat embedding cache) ────────
        // Composite PK on (ScopeKind, ScopeId) — same shape as EntityEmbedding
        // but unified across prose units. Vector column managed via raw SQL.
        b.Entity<ProseEmbedding>(e =>
        {
            e.ToTable("ProseEmbeddings");
            e.HasKey(x => new { x.ScopeKind, x.ScopeId });
            e.Property(x => x.ScopeKind).HasMaxLength(20).IsRequired();
            e.Property(x => x.SourceHash).HasMaxLength(32).IsRequired();
            e.Property(x => x.Model).HasMaxLength(80);
            e.HasIndex(x => x.EmbeddedAt);
            e.HasIndex(x => new { x.ScopeKind, x.EmbeddedAt }); // per-scope staleness sweeps
        });

        // ── World-state ledger (per-event append-only) ───────────────────────
        b.Entity<EntityStateEvent>(e =>
        {
            e.ToTable("EntityStateEvents");
            e.HasKey(x => x.Id);
            e.Property(x => x.AspectKey).HasMaxLength(200);
            e.Property(x => x.Verb).HasMaxLength(20);
            e.Property(x => x.Source).HasMaxLength(200);
            // OldValue / NewValue / Snippet stay NVARCHAR(MAX) — values can be
            // long JSON blobs or quoted prose.
            e.HasOne(x => x.Entity)
                .WithMany()
                .HasForeignKey(x => x.EntityId)
                .OnDelete(DeleteBehavior.Cascade);
            // Hot path: "what was X's location at time T?" → seek by
            // (EntityId, AspectKey) then walk by AtStoryTime.
            e.HasIndex(x => new { x.EntityId, x.AspectKey, x.AtStoryTime });
            // Closed-window seek: "what's true at story-time T?" — single index
            // hit when the [InWorldValidFrom, InWorldValidTo) window is closed
            // by WorldStateLedger.RecordAsync.
            e.HasIndex(x => new { x.EntityId, x.AspectKey, x.InWorldValidFrom });
            // Timeline-axis scans (vis-timeline, contradiction detector).
            e.HasIndex(x => x.AtStoryTime);
            // "All events tied to chapter X" rollups.
            e.HasIndex(x => x.ChapterId);
            e.HasIndex(x => x.BeatGuid);
            e.HasIndex(x => x.UniverseId);
            // Universe scoping (RFC 0006). No-op when unscoped.
            e.HasQueryFilter(x => ScopedUniverseId == Guid.Empty || x.UniverseId == ScopedUniverseId);
        });

        // ── Continuity store (migrated from SQLite continuity.db) ────────────
        b.Entity<ContinuityClaim>(e =>
        {
            e.ToTable("ContinuityClaims");
            e.HasKey(x => x.ClaimUid);
            e.Property(x => x.ClaimUid).HasMaxLength(80);
            e.Property(x => x.EntityId).HasMaxLength(80);
            e.Property(x => x.EntityName).HasMaxLength(400);
            e.Property(x => x.EntityKind).HasMaxLength(40);
            e.Property(x => x.Predicate).HasMaxLength(120);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.SourceType).HasMaxLength(40);
            // Existing model holds ExtractedBy as List<string>; persist via a JSON
            // backing column so we don't need a child table. The ValueComparer
            // is REQUIRED whenever a collection has a value converter — without
            // it EF can't deep-compare two snapshots and emits the warning
            // "ContinuityClaim.ExtractedBy is a collection or enumeration type
            //  with a value converter but with no value comparer." It also means
            // EF would miss updates to the list contents.
            e.Property(x => x.ExtractedBy)
                .HasConversion(
                    list => System.Text.Json.JsonSerializer.Serialize(list, (System.Text.Json.JsonSerializerOptions?)null),
                    json => string.IsNullOrEmpty(json)
                        ? new List<string>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>(),
                    new ValueComparer<List<string>>(
                        (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                        list => list == null ? 0 : list.Aggregate(0, (h, s) => HashCode.Combine(h, s == null ? 0 : s.GetHashCode())),
                        list => list == null ? new List<string>() : list.ToList()));
            e.HasIndex(x => new { x.EntityId, x.Predicate });
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.SourceType);
            // 23rd-century in-world date for asOf queries (e.g. "what was true on 2256-04-15").
            e.HasIndex(x => x.StoryDate);
            e.Property(x => x.StorySlug).HasMaxLength(80);
            e.HasIndex(x => x.StorySlug);
        });

        // Findings inbox — auto-detected contradictions / clichés / etc from
        // ContinuousQualityService. Last SQLite holdout, migrated 2026-05-09.
        b.Entity<FindingRow>(e =>
        {
            e.ToTable("Findings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.FilePath).HasMaxLength(900).IsRequired();
            e.Property(x => x.ChapterId).HasMaxLength(80);
            e.Property(x => x.Category).HasMaxLength(40).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.DedupKey).HasMaxLength(450).IsRequired();
            e.HasIndex(x => x.DedupKey).IsUnique().HasDatabaseName("UQ_Findings_DedupKey");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_Findings_Status");
            e.HasIndex(x => x.FilePath).HasDatabaseName("IX_Findings_FilePath");
            e.HasIndex(x => x.ChapterId).HasDatabaseName("IX_Findings_ChapterId");
        });

        b.Entity<ClaimContradictionRow>(e =>
        {
            e.ToTable("ClaimContradictions");
            e.HasKey(x => new { x.AUid, x.BUid });
            e.Property(x => x.AUid).HasMaxLength(80);
            e.Property(x => x.BUid).HasMaxLength(80);
        });

        b.Entity<ClaimConfirmationRow>(e =>
        {
            e.ToTable("ClaimConfirmations");
            e.HasKey(x => new { x.ClaimUid, x.SourceChapterId, x.SourcePath });
            e.Property(x => x.ClaimUid).HasMaxLength(80);
            e.Property(x => x.SourceChapterId).HasMaxLength(80);
            e.Property(x => x.SourcePath).HasMaxLength(400);
        });

        b.Entity<ExtractionRunRow>(e =>
        {
            e.ToTable("ExtractionRuns");
            e.HasKey(x => x.Id);
            e.Property(x => x.ScopeType).HasMaxLength(40);
            e.Property(x => x.ScopeId).HasMaxLength(80);
            e.HasIndex(x => x.StartedAt);
        });

        // Per-beat prose quality metrics (CPU-only nightly compute, non-temporal).
        b.Entity<BeatProseMetrics>(e =>
        {
            e.ToTable("BeatProseMetrics");
            e.HasKey(x => x.BeatId);
            e.HasOne<Beat>().WithOne().HasForeignKey<BeatProseMetrics>(x => x.BeatId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NodeId).HasDatabaseName("IX_BeatProseMetrics_NodeId");
        });
    }

    /// <summary>Configures all 14 misc story/canon types and their bridges.</summary>
    private static void ConfigureMisc(ModelBuilder b)
    {
        // Archetype
        b.Entity<ArchetypeRow>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Family).HasMaxLength(120);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<ArchetypeRow>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Name); e.HasIndex(x => x.Family); e.HasIndex(x => x.Category);
        });
        b.Entity<ArchetypeWillAlways>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Archetype).WithMany(x => x.WillAlways).HasForeignKey(x => x.ArchetypeId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ArchetypeId, x.Position }); });
        b.Entity<ArchetypeWillNever>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Archetype).WithMany(x => x.WillNever).HasForeignKey(x => x.ArchetypeId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ArchetypeId, x.Position }); });
        b.Entity<ArchetypeUnless>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Archetype).WithMany(x => x.Unless).HasForeignKey(x => x.ArchetypeId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ArchetypeId, x.Position }); });
        b.Entity<ArchetypeSimilar>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Archetype).WithMany(x => x.SimilarTo).HasForeignKey(x => x.ArchetypeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Similar).WithMany().HasForeignKey(x => x.SimilarArchetypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ArchetypeId, x.Position }); e.HasIndex(x => x.SimilarArchetypeId);
        });
        b.Entity<ArchetypeOpposite>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Archetype).WithMany(x => x.OppositeOf).HasForeignKey(x => x.ArchetypeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Opposite).WithMany().HasForeignKey(x => x.OppositeArchetypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ArchetypeId, x.Position }); e.HasIndex(x => x.OppositeArchetypeId);
        });

        // Quote
        b.Entity<Quote>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Attribution).HasMaxLength(450);
            e.Property(x => x.Theme).HasMaxLength(120);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Quote>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Attribution); e.HasIndex(x => x.Theme); e.HasIndex(x => x.Category);
        });

        // News
        b.Entity<News>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Outlet).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<News>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Outlet); e.HasIndex(x => x.PublishedDate); e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Name);
        });
        b.Entity<NewsEntityInvolved>(e => {
            e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.News).WithMany(x => x.EntitiesInvolved).HasForeignKey(x => x.NewsId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.InvolvedEntity).WithMany().HasForeignKey(x => x.InvolvedEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.NewsId, x.Position }); e.HasIndex(x => x.InvolvedEntityId);
        });
        b.Entity<NewsLocation>(e => {
            e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.News).WithMany(x => x.Locations).HasForeignKey(x => x.NewsId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.NewsId, x.Position }); e.HasIndex(x => x.PlaceId);
        });

        // Contract
        b.Entity<ContractEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Codename).HasMaxLength(120);
            e.Property(x => x.ContractStatus).HasMaxLength(40);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<ContractEntity>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ClientEntity).WithMany().HasForeignKey(x => x.ClientEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.LocationPlace).WithMany().HasForeignKey(x => x.LocationPlaceId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Codename); e.HasIndex(x => x.ContractStatus);
            e.HasIndex(x => x.ClientEntityId); e.HasIndex(x => x.LocationPlaceId);
        });
        b.Entity<ContractBonusRow>(e => { e.HasKey(x => x.Id); e.Property(x => x.BonusType).HasMaxLength(80); e.HasOne(x => x.Contract).WithMany(x => x.Bonuses).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ContractId, x.Position }); });
        b.Entity<ContractComplication>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Contract).WithMany(x => x.Complications).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ContractId, x.Position }); });

        // Document
        b.Entity<DocumentEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.FileName).HasMaxLength(400);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<DocumentEntity>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Title); e.HasIndex(x => x.FileName); e.HasIndex(x => x.Category);
        });
        b.Entity<DocumentHeading>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Document).WithMany(x => x.Headings).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.DocumentId, x.Position }); });

        // Vocabulary
        b.Entity<Vocabulary>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Term).HasMaxLength(200);
            e.Property(x => x.Domain).HasMaxLength(120);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Vocabulary>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Term); e.HasIndex(x => x.Domain); e.HasIndex(x => x.Category);
        });

        // LabSpecimen
        b.Entity<LabSpecimen>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Classification).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<LabSpecimen>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Classification); e.HasIndex(x => x.Name);
        });
        b.Entity<LabSpecimenAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.LabSpecimen).WithMany(x => x.Aliases).HasForeignKey(x => x.LabSpecimenId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.LabSpecimenId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<LabSpecimenKnownLocation>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.LabSpecimen).WithMany(x => x.KnownLocations).HasForeignKey(x => x.LabSpecimenId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.LabSpecimenId, x.Position }); e.HasIndex(x => x.PlaceId); });
        b.Entity<LabSpecimenStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.LabSpecimen).WithMany(x => x.StoryHooks).HasForeignKey(x => x.LabSpecimenId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.LabSpecimenId, x.Position }); });

        // Psionic
        b.Entity<Psionic>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Discipline).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Psionic>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Discipline); e.HasIndex(x => x.Name);
        });
        b.Entity<PsionicAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Psionic).WithMany(x => x.Aliases).HasForeignKey(x => x.PsionicId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.PsionicId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<PsionicKnownPractitioner>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Psionic).WithMany(x => x.KnownPractitioners).HasForeignKey(x => x.PsionicId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.PsionicId, x.Position }); e.HasIndex(x => x.CharacterId); });
        b.Entity<PsionicStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Psionic).WithMany(x => x.StoryHooks).HasForeignKey(x => x.PsionicId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.PsionicId, x.Position }); });

        // Technology
        b.Entity<Technology>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Subcategory).HasMaxLength(120);
            e.Property(x => x.BrandName).HasMaxLength(200);
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Technology>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Category); e.HasIndex(x => x.Name); e.HasIndex(x => x.BrandName);
        });
        b.Entity<TechnologyAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Technology).WithMany(x => x.Aliases).HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.TechnologyId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<TechnologyDeveloper>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Technology).WithMany(x => x.Developers).HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Developer).WithMany().HasForeignKey(x => x.DeveloperEntityId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.TechnologyId, x.Position }); e.HasIndex(x => x.DeveloperEntityId); });
        b.Entity<TechnologyBaseTechnology>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Technology).WithMany(x => x.BaseTechnologies).HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.BaseTechnology).WithMany().HasForeignKey(x => x.BaseTechnologyId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.TechnologyId, x.Position }); e.HasIndex(x => x.BaseTechnologyId); });
        b.Entity<TechnologyEnables>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Technology).WithMany(x => x.Enables).HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Enabled).WithMany().HasForeignKey(x => x.EnabledEntityId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.TechnologyId, x.Position }); e.HasIndex(x => x.EnabledEntityId); });
        b.Entity<TechnologyStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Technology).WithMany(x => x.StoryHooks).HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.TechnologyId, x.Position }); });


        // Motif
        b.Entity<Motif>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Motif>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Name);
        });
        b.Entity<MotifAppearance>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Motif).WithMany(x => x.Appearances).HasForeignKey(x => x.MotifId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.MotifId, x.Position }); });

        // Entertainment
        b.Entity<Entertainment>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Entertainment>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Category); e.HasIndex(x => x.Genre); e.HasIndex(x => x.Name);
        });
        b.Entity<EntertainmentAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Entertainment).WithMany(x => x.Aliases).HasForeignKey(x => x.EntertainmentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.EntertainmentId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<EntertainmentKnownFan>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Entertainment).WithMany(x => x.KnownFans).HasForeignKey(x => x.EntertainmentId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.EntertainmentId, x.Position }); e.HasIndex(x => x.CharacterId); });
        b.Entity<EntertainmentStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Entertainment).WithMany(x => x.StoryHooks).HasForeignKey(x => x.EntertainmentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.EntertainmentId, x.Position }); });

        // FlyoverEntity (Wasteland)
        b.Entity<FlyoverEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Classification).HasMaxLength(120);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<FlyoverEntity>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Classification); e.HasIndex(x => x.Name);
        });
        b.Entity<FlyoverEntityAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.FlyoverEntity).WithMany(x => x.Aliases).HasForeignKey(x => x.FlyoverEntityId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.FlyoverEntityId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<FlyoverEntityKnownLocation>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.FlyoverEntity).WithMany(x => x.KnownLocations).HasForeignKey(x => x.FlyoverEntityId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.FlyoverEntityId, x.Position }); e.HasIndex(x => x.PlaceId); });
        b.Entity<FlyoverEntityStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.FlyoverEntity).WithMany(x => x.StoryHooks).HasForeignKey(x => x.FlyoverEntityId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.FlyoverEntityId, x.Position }); });

        // SyntheticLife (ELFs / rogue AI / firmware-evolved entities)
        b.Entity<SyntheticLife>(e => {
            e.ToTable("SyntheticLives");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.KindOfBeing).HasMaxLength(120);
            e.Property(x => x.Classification).HasMaxLength(120);
            e.Property(x => x.Disposition).HasMaxLength(120);
            e.Property(x => x.Habitat).HasMaxLength(120);
            e.Property(x => x.Origin).HasMaxLength(120);
            e.Property(x => x.LifeStatus).HasMaxLength(120);
            e.Property(x => x.EncounterFrequency).HasMaxLength(120);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Tier).HasMaxLength(80);
            e.Property(x => x.Location).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<SyntheticLife>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Classification); e.HasIndex(x => x.Disposition); e.HasIndex(x => x.Name);
        });
        b.Entity<SyntheticLifeAlias>(e => {
            e.ToTable("SyntheticLifeAliases");
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.SyntheticLife).WithMany(x => x.Aliases).HasForeignKey(x => x.SyntheticLifeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SyntheticLifeId, x.Position }); e.HasIndex(x => x.Value);
        });
        b.Entity<SyntheticLifeKnownAssociation>(e => {
            e.ToTable("SyntheticLifeKnownAssociations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.SyntheticLife).WithMany(x => x.KnownAssociations).HasForeignKey(x => x.SyntheticLifeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Associate).WithMany().HasForeignKey(x => x.AssociateEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.SyntheticLifeId, x.Position }); e.HasIndex(x => x.AssociateEntityId); e.HasIndex(x => x.Alias);
        });
        b.Entity<SyntheticLifeStoryHook>(e => {
            e.ToTable("SyntheticLifeStoryHooks");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.SyntheticLife).WithMany(x => x.StoryHooks).HasForeignKey(x => x.SyntheticLifeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SyntheticLifeId, x.Position });
        });
    }

    /// <summary>Configures all 10 gear types and their bridge tables in one pass.</summary>
    private static void ConfigureGear(ModelBuilder b)
    {
        // Weapon
        b.Entity<Weapon>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Weapon>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Name);
        });
        b.Entity<WeaponAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Weapon).WithMany(x => x.Aliases).HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.WeaponId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<WeaponBaseTechnology>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Weapon).WithMany(x => x.BaseTechnologies).HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Technology).WithMany().HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.WeaponId, x.Position }); e.HasIndex(x => x.TechnologyId); });
        b.Entity<WeaponKnownUser>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Weapon).WithMany(x => x.KnownUsers).HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.WeaponId, x.Position }); e.HasIndex(x => x.CharacterId); });
        b.Entity<WeaponAmmunitionType>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Weapon).WithMany(x => x.AmmunitionTypes).HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Ammunition).WithMany().HasForeignKey(x => x.AmmunitionId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.WeaponId, x.Position }); e.HasIndex(x => x.AmmunitionId); });
        b.Entity<WeaponStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Weapon).WithMany(x => x.StoryHooks).HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.WeaponId, x.Position }); });

        // Equipment
        b.Entity<Equipment>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.BrandName).HasMaxLength(200);
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Equipment>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Name); e.HasIndex(x => x.BrandName);
        });
        b.Entity<EquipmentAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Equipment).WithMany(x => x.Aliases).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.EquipmentId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<EquipmentBaseTechnology>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Equipment).WithMany(x => x.BaseTechnologies).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Technology).WithMany().HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.EquipmentId, x.Position }); e.HasIndex(x => x.TechnologyId); });
        b.Entity<EquipmentKnownUser>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Equipment).WithMany(x => x.KnownUsers).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.EquipmentId, x.Position }); e.HasIndex(x => x.CharacterId); });
        b.Entity<EquipmentSpecification>(e => { e.HasKey(x => x.Id); e.Property(x => x.KeyName).HasMaxLength(200); e.HasOne(x => x.Equipment).WithMany(x => x.Specifications).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.EquipmentId, x.KeyName }).IsUnique(); });
        b.Entity<EquipmentStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Equipment).WithMany(x => x.StoryHooks).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.EquipmentId, x.Position }); });

        // Cyberware
        b.Entity<Cyberware>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Cyberware>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer);
            e.HasIndex(x => x.Name);
        });
        b.Entity<CyberwareItemAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Cyberware).WithMany(x => x.Aliases).HasForeignKey(x => x.CyberwareId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.CyberwareId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<CyberwareItemSideEffect>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Cyberware).WithMany(x => x.SideEffects).HasForeignKey(x => x.CyberwareId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.CyberwareId, x.Position }); });
        b.Entity<CyberwareItemKnownUser>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Cyberware).WithMany(x => x.KnownUsers).HasForeignKey(x => x.CyberwareId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.CyberwareId, x.Position }); e.HasIndex(x => x.CharacterId); });
        b.Entity<CyberwareItemStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Cyberware).WithMany(x => x.StoryHooks).HasForeignKey(x => x.CyberwareId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.CyberwareId, x.Position }); });

        // Apparel
        b.Entity<Apparel>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.PriceRange).HasMaxLength(200);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Apparel>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category); e.HasIndex(x => x.Name);
        });
        b.Entity<ApparelAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Apparel).WithMany(x => x.Aliases).HasForeignKey(x => x.ApparelId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ApparelId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<ApparelMaterial>(e => { e.HasKey(x => x.Id); e.ToTable("ApparelMaterials"); e.HasOne(x => x.Apparel).WithMany(x => x.Materials).HasForeignKey(x => x.ApparelId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ApparelId, x.Position }); });
        b.Entity<ApparelWornBy>(e => { e.HasKey(x => x.Id); e.ToTable("ApparelWornBy"); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Apparel).WithMany(x => x.WornBy).HasForeignKey(x => x.ApparelId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterEntityId).OnDelete(DeleteBehavior.NoAction); e.HasIndex(x => new { x.ApparelId, x.Position }); e.HasIndex(x => x.CharacterEntityId); e.HasIndex(x => x.Alias); });
        b.Entity<ApparelStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Apparel).WithMany(x => x.StoryHooks).HasForeignKey(x => x.ApparelId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ApparelId, x.Position }); });

        // Ammunition
        b.Entity<Ammunition>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Caliber).HasMaxLength(80);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Ammunition>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Caliber); e.HasIndex(x => x.Name);
        });
        b.Entity<AmmunitionAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Ammunition).WithMany(x => x.Aliases).HasForeignKey(x => x.AmmunitionId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.AmmunitionId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<AmmunitionCompatibleWeapon>(e => { e.HasKey(x => x.Id); e.Property(x => x.Alias).HasMaxLength(450); e.HasOne(x => x.Ammunition).WithMany(x => x.CompatibleWeapons).HasForeignKey(x => x.AmmunitionId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Weapon).WithMany().HasForeignKey(x => x.WeaponId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => new { x.AmmunitionId, x.Position }); e.HasIndex(x => x.WeaponId); });
        b.Entity<AmmunitionVariant>(e => { e.HasKey(x => x.Id); e.Property(x => x.VariantName).HasMaxLength(200); e.HasOne(x => x.Ammunition).WithMany(x => x.Variants).HasForeignKey(x => x.AmmunitionId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.AmmunitionId, x.Position }); });
        b.Entity<AmmunitionStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Ammunition).WithMany(x => x.StoryHooks).HasForeignKey(x => x.AmmunitionId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.AmmunitionId, x.Position }); });

        // Pharmaceutical
        b.Entity<Pharmaceutical>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Subcategory).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Pharmaceutical>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Name);
        });
        b.Entity<PharmAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Pharmaceutical).WithMany(x => x.Aliases).HasForeignKey(x => x.PharmaceuticalId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.PharmaceuticalId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<PharmEffect>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Pharmaceutical).WithMany(x => x.Effects).HasForeignKey(x => x.PharmaceuticalId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.PharmaceuticalId, x.Position }); });
        b.Entity<PharmSideEffect>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Pharmaceutical).WithMany(x => x.SideEffects).HasForeignKey(x => x.PharmaceuticalId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.PharmaceuticalId, x.Position }); });
        b.Entity<PharmStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Pharmaceutical).WithMany(x => x.StoryHooks).HasForeignKey(x => x.PharmaceuticalId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.PharmaceuticalId, x.Position }); });

        // Genemod
        b.Entity<Genemod>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.BrandName).HasMaxLength(450);
            e.Property(x => x.ProductName).HasMaxLength(450);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.TargetSystem).HasMaxLength(450);
            e.Property(x => x.SourceOrganism).HasMaxLength(450);
            e.Property(x => x.Legality).HasMaxLength(200);
            e.Property(x => x.Procedure).HasMaxLength(1000);
            e.Property(x => x.ExpressionTime).HasMaxLength(200);
            e.Property(x => x.Reversibility).HasMaxLength(200);
            e.Property(x => x.SocialPerception).HasMaxLength(1000);
            e.Property(x => x.TierAvailability).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Genemod>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category); e.HasIndex(x => x.Name);
            e.HasIndex(x => x.TargetSystem);
        });
        b.Entity<GenemodAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Genemod).WithMany(x => x.Aliases).HasForeignKey(x => x.GenemodId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.GenemodId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<GenemodSideEffect>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Genemod).WithMany(x => x.SideEffects).HasForeignKey(x => x.GenemodId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.GenemodId, x.Position }); });
        b.Entity<GenemodStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Genemod).WithMany(x => x.StoryHooks).HasForeignKey(x => x.GenemodId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.GenemodId, x.Position }); });

        // Material
        b.Entity<Material>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.BrandName).HasMaxLength(450);
            e.Property(x => x.ProductName).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.TierAvailability).HasMaxLength(450);
            e.Property(x => x.Cost).HasMaxLength(200);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Material>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Category); e.HasIndex(x => x.Name);
        });
        b.Entity<MaterialAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Material).WithMany(x => x.Aliases).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.MaterialId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<MaterialProperty>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Material).WithMany(x => x.Properties).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.MaterialId, x.Position }); });
        b.Entity<MaterialDeveloper>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Material).WithMany(x => x.Developers).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.MaterialId, x.Position }); });
        b.Entity<MaterialApplication>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Material).WithMany(x => x.Applications).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.MaterialId, x.Position }); });
        b.Entity<MaterialStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Material).WithMany(x => x.StoryHooks).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.MaterialId, x.Position }); });

        // Transportation
        b.Entity<Transportation>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(450);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Propulsion).HasMaxLength(450);
            e.Property(x => x.Speed).HasMaxLength(200);
            e.Property(x => x.Capacity).HasMaxLength(200);
            e.Property(x => x.Range).HasMaxLength(200);
            e.Property(x => x.TierAvailability).HasMaxLength(450);
            e.Property(x => x.Cost).HasMaxLength(200);
            e.Property(x => x.Autonomy).HasMaxLength(450);
            e.Property(x => x.Armament).HasMaxLength(1000);
            e.Property(x => x.CommonUsage).HasMaxLength(1000);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<Transportation>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category); e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Propulsion);
        });
        b.Entity<TransportationAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.Transportation).WithMany(x => x.Aliases).HasForeignKey(x => x.TransportationId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.TransportationId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<TransportationStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.Transportation).WithMany(x => x.StoryHooks).HasForeignKey(x => x.TransportationId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.TransportationId, x.Position }); });

        // ConsumerGood
        b.Entity<ConsumerGood>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Manufacturer).HasMaxLength(450);
            e.Property(x => x.Category).HasMaxLength(120);
            e.Property(x => x.Name).HasMaxLength(450);
            e.HasOne(x => x.Entity).WithOne().HasForeignKey<ConsumerGood>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Manufacturer); e.HasIndex(x => x.Category); e.HasIndex(x => x.Name);
        });
        b.Entity<ConsumerGoodAlias>(e => { e.HasKey(x => x.Id); e.Property(x => x.Value).HasMaxLength(450); e.HasOne(x => x.ConsumerGood).WithMany(x => x.Aliases).HasForeignKey(x => x.ConsumerGoodId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ConsumerGoodId, x.Position }); e.HasIndex(x => x.Value); });
        b.Entity<ConsumerGoodStoryHook>(e => { e.HasKey(x => x.Id); e.HasOne(x => x.ConsumerGood).WithMany(x => x.StoryHooks).HasForeignKey(x => x.ConsumerGoodId).OnDelete(DeleteBehavior.Cascade); e.HasIndex(x => new { x.ConsumerGoodId, x.Position }); });
    }

    private static void ConfigurePlaceBridges(ModelBuilder b)
    {
        b.Entity<PlaceAlias>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.Place).WithMany(x => x.Aliases).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlaceId, x.Position });
            e.HasIndex(x => x.Value);
        });
        b.Entity<PlaceDanger>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Place).WithMany(x => x.Dangers).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlaceId, x.Position });
        });
        b.Entity<PlaceOpportunity>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Place).WithMany(x => x.Opportunities).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlaceId, x.Position });
        });
        b.Entity<PlaceStoryHook>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Place).WithMany(x => x.StoryHooks).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlaceId, x.Position });
        });
        b.Entity<PlaceAtmosphereItem>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Bucket).HasMaxLength(20);
            e.HasOne(x => x.Place).WithMany(x => x.AtmosphereItems).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlaceId, x.Bucket, x.Position });
        });
        b.Entity<PlaceAdjacency>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Place).WithMany(x => x.Adjacencies).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Neighbor).WithMany().HasForeignKey(x => x.NeighborId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PlaceId, x.Position });
            e.HasIndex(x => x.NeighborId);
            e.HasIndex(x => x.Alias);
        });
        b.Entity<PlaceExitRow>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Direction).HasMaxLength(40);
            e.Property(x => x.DestinationAlias).HasMaxLength(450);
            e.Property(x => x.ExitType).HasMaxLength(40);
            e.HasOne(x => x.Place).WithMany(x => x.Exits).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Destination).WithMany().HasForeignKey(x => x.DestinationId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PlaceId, x.Position });
            e.HasIndex(x => x.DestinationId);
            e.HasIndex(x => x.Direction);
        });
        b.Entity<PlaceFrequentBy>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Place).WithMany(x => x.FrequentedBy).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PlaceId, x.Position });
            e.HasIndex(x => x.TargetEntityId);
            e.HasIndex(x => x.Alias);
        });
        b.Entity<PlaceNotableLocation>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.LocationName).HasMaxLength(450);
            e.HasOne(x => x.Place).WithMany(x => x.NotableLocations).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PlaceId, x.Position });
            e.HasIndex(x => x.LocationName);
        });
        b.Entity<PlaceRelatedEntity>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Place).WithMany(x => x.RelatedEntities).HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Related).WithMany().HasForeignKey(x => x.RelatedEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PlaceId, x.Position });
            e.HasIndex(x => x.RelatedEntityId);
        });
    }

    private static void ConfigureFactionBridges(ModelBuilder b)
    {
        b.Entity<FactionAlias>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.Faction).WithMany(x => x.Aliases).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FactionId, x.Position });
            e.HasIndex(x => x.Value);
        });
        b.Entity<FactionMethod>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Faction).WithMany(x => x.Methods).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FactionId, x.Position });
        });
        b.Entity<FactionResource>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Faction).WithMany(x => x.Resources).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FactionId, x.Position });
        });
        b.Entity<FactionGoal>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Faction).WithMany(x => x.Goals).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FactionId, x.Position });
        });
        b.Entity<FactionStoryHook>(e => {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Faction).WithMany(x => x.StoryHooks).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FactionId, x.Position });
        });
        b.Entity<FactionRelationshipRow>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.HasOne(x => x.Faction).WithMany(x => x.Relationships).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TargetFaction).WithMany().HasForeignKey(x => x.TargetFactionId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FactionId, x.Position });
            e.HasIndex(x => x.TargetFactionId);
            e.HasIndex(x => x.Alias);
        });
        b.Entity<FactionRelationshipTag>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(450);
            e.HasOne(x => x.FactionRelationshipRow).WithMany(x => x.Tags)
                .HasForeignKey(x => x.FactionRelationshipRowId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FactionRelationshipRowId, x.Position });
        });
        b.Entity<FactionMemberRow>(e => {
            e.HasKey(x => x.Id);
            e.Property(x => x.Alias).HasMaxLength(450);
            e.Property(x => x.Role).HasMaxLength(120);
            e.Property(x => x.MemberStatus).HasMaxLength(40);
            e.HasOne(x => x.Faction).WithMany(x => x.Members).HasForeignKey(x => x.FactionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.FactionId, x.Position });
            e.HasIndex(x => x.CharacterId);
            e.HasIndex(x => x.Alias);
        });

        // ── NodeAmendment ─────────────────────────────────────────────────
        b.Entity<NodeAmendment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => new { x.NodeId, x.SequenceNo }).IsUnique();
        });

        // ── NodeSpineVersion ───────────────────────────────────────────────
        b.Entity<NodeSpineVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.BibleHash).HasMaxLength(64);
            e.Property(x => x.UserStoriesHash).HasMaxLength(64);
            e.Property(x => x.PinnedBy).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => new { x.NodeId, x.NodeVersion }).IsUnique();
        });

        // ── NodeKeyword ────────────────────────────────────────────────────
        b.Entity<NodeKeyword>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Keyword).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.NodeId);
            e.HasOne(x => x.Node).WithMany(x => x.Keywords)
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── NodeChapterSummary ─────────────────────────────────────────────
        b.Entity<NodeChapterSummary>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.NodeId, x.ChapterIndex }).IsUnique();
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── NodeOpenThread ─────────────────────────────────────────────────
        b.Entity<NodeOpenThread>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.NodeId, x.IsResolved });
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── StoryPlotEvent ────────────────────────────────────────────────
        b.Entity<StoryPlotEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StateKey).HasMaxLength(200).IsRequired();
            e.Property(x => x.StateType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Verb).HasMaxLength(50).IsRequired();
            e.Property(x => x.Label).HasMaxLength(500).IsRequired();
            e.Property(x => x.NewValue).HasMaxLength(100).IsRequired();
            e.Property(x => x.Source).HasMaxLength(50).IsRequired();
            // Hot path: "what is the current state of key X in node N?" — index by (NodeId, StateKey)
            e.HasIndex(x => new { x.NodeId, x.StateKey });
            e.HasIndex(x => new { x.NodeId, x.CreatedAt });
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── NarrativeSummaryEntry ─────────────────────────────────────────────
        b.Entity<NarrativeSummaryEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.NodeId, x.SortKey });
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── MarkdownFile ────────────────────────────────────────────────────
        b.Entity<MarkdownFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FilePath).HasMaxLength(2000);
            e.Property(x => x.FileRoot).HasMaxLength(100).IsRequired();
            e.Property(x => x.RelativePath).HasMaxLength(2000).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.Property(x => x.SyncedBy).HasMaxLength(100);
            e.Property(x => x.Tier).HasMaxLength(20).HasDefaultValue("topic");
            e.Property(x => x.Scope).HasMaxLength(1000).HasDefaultValue("");
            e.Property(x => x.Triggers).HasMaxLength(2000).HasDefaultValue("");
            e.Property(x => x.AutoTier).HasDefaultValue(true);
            e.Property(x => x.RelatedIds).HasMaxLength(4000).HasDefaultValue("");
            e.HasIndex(x => x.Tier);
            // Composite unique: the project and global CLAUDE.md share RelativePath
            // "CLAUDE.md" but differ by FileRoot, so RelativePath alone is not unique.
            e.HasIndex(x => new { x.FileRoot, x.RelativePath }).IsUnique();
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.LastSyncedAt);
        });

        // ── Noun consistency — deprecated/renamed noun registry ───────────────
        b.Entity<DeprecatedEntityName>(e =>
        {
            e.ToTable("DeprecatedEntityNames");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeprecatedName).HasMaxLength(256).IsRequired();
            e.Property(x => x.CanonicalName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(512);
            e.Property(x => x.AddedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Entity).WithMany()
                .HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            e.HasIndex(x => x.UniverseId);
            e.HasIndex(x => x.EntityId);
        });

        b.Entity<Survey>(e =>
        {
            e.ToTable("Surveys");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Open");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.UniverseId);
            e.HasMany(x => x.Questions).WithOne(x => x.Survey)
                .HasForeignKey(x => x.SurveyId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SurveyQuestion>(e =>
        {
            e.ToTable("SurveyQuestions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.QuestionKey).HasMaxLength(20).IsRequired();
            e.Property(x => x.QuestionType).HasMaxLength(50).IsRequired().HasDefaultValue("Custom");
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.OptionsJson).IsRequired().HasDefaultValue("[]");
            e.Property(x => x.ApplyStatus).HasMaxLength(20).IsRequired().HasDefaultValue("Pending");
            e.HasIndex(x => x.SurveyId);
        });

        // ── Edit sessions ─────────────────────────────────────────────────────
        b.Entity<EditSession>(e =>
        {
            e.ToTable("EditSessions");
            e.HasKey(x => x.EditSessionId);
            e.Property(x => x.EditSessionId).HasDefaultValueSql("NEWID()");
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.SessionType).HasMaxLength(50).IsRequired().HasDefaultValue("custom");
            e.Property(x => x.StartedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Node).WithMany()
                .HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NodeId);
            e.HasIndex(x => new { x.NodeId, x.ClosedAt });
        });

        b.Entity<EditSessionBeat>(e =>
        {
            e.ToTable("EditSessionBeats");
            e.HasKey(x => new { x.EditSessionId, x.BeatId });
            e.Property(x => x.EditedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.PriorTextHash).HasMaxLength(64);
            e.HasOne(x => x.Session).WithMany(x => x.SessionBeats)
                .HasForeignKey(x => x.EditSessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Beat).WithMany()
                .HasForeignKey(x => x.BeatId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.BeatId);
        });
    }

    // ConfigureSubtype helper removed — every subtype is now configured explicitly
    // in ConfigureGear / ConfigureMisc / per-type blocks above. The previous helper
    // added a shadow DataJson string column to every subtype; eliminating it here
    // is what finally retires the JSON-dump shape from the model.

    /// <summary>
    /// Tables that should be system-versioned. Adding/removing here is the only
    /// place the temporal-on/off decision lives.
    /// </summary>
    public static readonly string[] SystemVersionedTables =
    {
        "Entities", "Records", "EntityProperties", "Edges",
        // Character relational schema — every bridge table is rewindable so that
        // "what did this character know / carry / look like / believe on date X"
        // can be answered without the parent's history alone leaving holes.
        "Characters",
        "CharacterAliases",
        "CharacterStoryHooks",
        "CharacterArchetypeScores",
        "CharacterGeneticAncestries",
        "CharacterAncestryDetails",
        "CharacterPsychologyTraits",
        "CharacterSpeechPhrases",
        "CharacterBehavioralRules",
        "CharacterBehavioralMaps",
        "CharacterStatScalars",
        "CharacterStatPhrases",
        "CharacterPhysicalMarks",
        "CharacterTerritoryZones",
        "CharacterTerritoryReputations",
        "CharacterBelongingsGear",
        "CharacterBelongingsExtras",
        "CharacterBioBatteryThresholds",
        "CharacterNeuralAbilities",
        "CharacterChangelog",
        "CharacterCyberware",
        "CharacterKnowledge",
        "CharacterKnowledgeEntities",
        "CharacterConditions",
        "CharacterRelationships",
        "CharacterTimeline",
        "CharacterTimelineBodyChanges",
        "CharacterHomeTurfs",
        "CharacterAffiliations",
        // Place relational schema.
        "Places",
        "PlaceAliases", "PlaceDangers", "PlaceOpportunities", "PlaceStoryHooks",
        "PlaceAtmosphereItems", "PlaceAdjacencies", "PlaceExits",
        "PlaceFrequentedBy", "PlaceNotableLocations", "PlaceRelatedEntities",
        // Faction relational schema.
        "Factions",
        "FactionAliases", "FactionMethods", "FactionResources", "FactionGoals",
        "FactionStoryHooks", "FactionRelationships", "FactionRelationshipTags", "FactionMembers",
        // Corponation / Subsidiary / SyntheticLife / Automaton relational schemas.
        "Corponations", "CorponationCommonNames",
        "Subsidiaries", "SubsidiaryProducts",
        "SyntheticLives", "SyntheticLifeAliases", "SyntheticLifeKnownAssociations", "SyntheticLifeStoryHooks",
        "Automata", "AutomatonAliases", "AutomatonArmament", "AutomatonSensors",
        "AutomatonDeployments", "AutomatonStoryHooks",
        // Gear cluster.
        "Weapons", "WeaponAliases", "WeaponBaseTechnologies", "WeaponKnownUsers",
        "WeaponAmmunitionTypes", "WeaponStoryHooks",
        "EquipmentItems", "EquipmentAliases", "EquipmentBaseTechnologies",
        "EquipmentKnownUsers", "EquipmentSpecifications", "EquipmentStoryHooks",
        "CyberwareItems", "CyberwareItemAliases", "CyberwareItemSideEffects",
        "CyberwareItemKnownUsers", "CyberwareItemStoryHooks",
        "Apparels", "ApparelAliases", "ApparelMaterials", "ApparelWornBy", "ApparelStoryHooks",
        "Ammunitions", "AmmunitionAliases", "AmmunitionCompatibleWeapons",
        "AmmunitionVariants", "AmmunitionStoryHooks",
        "Pharmaceuticals", "PharmaceuticalAliases", "PharmaceuticalEffects",
        "PharmaceuticalSideEffects", "PharmaceuticalStoryHooks",
        "Genemods", "GenemodAliases", "GenemodSideEffects", "GenemodStoryHooks",
        "Materials", "MaterialAliases", "MaterialProperties", "MaterialDevelopers",
        "MaterialApplications", "MaterialStoryHooks",
        "Transportations", "TransportationAliases", "TransportationStoryHooks",
        "ConsumerGoods", "ConsumerGoodAliases", "ConsumerGoodStoryHooks",
        // Misc cluster
        "Archetypes", "ArchetypeWillAlways", "ArchetypeWillNever", "ArchetypeUnless", "ArchetypeSimilars", "ArchetypeOpposites",
        "Quotes",
        "News", "NewsEntitiesInvolved", "NewsLocations",
        "Contracts", "ContractBonuses", "ContractComplications",
        "Documents", "DocumentHeadings",
        "VocabularyEntries",
        "LabSpecimens", "LabSpecimenAliases", "LabSpecimenKnownLocations", "LabSpecimenStoryHooks",
        "Psionics", "PsionicAliases", "PsionicKnownPractitioners", "PsionicStoryHooks",
        "Technologies", "TechnologyAliases", "TechnologyDevelopers", "TechnologyBaseTechnologies", "TechnologyEnabledList", "TechnologyStoryHooks",
        "Motifs", "MotifAppearances",
        "EntertainmentItems", "EntertainmentAliases", "EntertainmentKnownFans", "EntertainmentStoryHooks",
        "FlyoverEntities", "FlyoverEntityAliases", "FlyoverEntityKnownLocations", "FlyoverEntityStoryHooks",
        "Books", "BookProtagonists", "BookChapterOrder",
        "Chapters", "ChapterCharacters", "ChapterBeats",
        // Unified node writer model (Beat / Node / BeatNode junction).
        // System-versioned so every prose edit, metadata change, membership
        // shuffle, AND deletion lands in {Table}_History — that's the rewind
        // the writer's per-beat version cycler reads via FOR SYSTEM_TIME ALL,
        // and it captures CLI / MCP edits automatically (the UPDATE itself is
        // versioned; no app-side snapshotting required). Safe to version:
        // neither table carries a vector index (prose embeddings live in the
        // separate ProseEmbeddings table), so the SQL Server vector-index ↔
        // system-versioning incompatibility doesn't apply here.
        "Beats", "Nodes", "BeatNodes",
        "ContinuityClaims",
        "EntityStateEvents",
        "WeaponSpecs",
        "Settings",
        // Project-rules, Codex docs, and Claude Code memory files. Versioned so
        // any revision of any .md file can be recovered by timestamp — history
        // rows keep the full content, so a catastrophic file deletion can be
        // undone with ss --restore-markdown --as-of <datetime>.
        "MarkdownFiles",
        // Per-node narrative spine: amendment log (append-only) and version
        // pins (bridge linking docx-version → spine hashes). Both versioned so
        // amendments can never be truly deleted and any spine state can be
        // recovered by timestamp.
        "NodeAmendments",
        "NodeSpineVersions",
    };

    /// <summary>
    /// Enable SQL Server <c>SYSTEM_VERSIONING</c> on every table in
    /// <see cref="SystemVersionedTables"/>. Idempotent: skips tables that are
    /// already temporal. No-op on non-SQL-Server providers (SQLite tests).
    ///
    /// After this runs, every row mutation produces a history row in
    /// <c>{Table}_History</c>. Querying with <c>FOR SYSTEM_TIME AS OF '…'</c>
    /// gives you the database state as it was at any point in time — that's the
    /// "rewindable" property the user asked for.
    /// </summary>
    /// <summary>
    /// In-world-canon anchor for the temporal history. Every row that exists at the
    /// moment SYSTEM_VERSIONING is enabled gets SysStart = this value, so a query
    /// like <c>FOR SYSTEM_TIME AS OF '2026-01-01'</c> returns the initial corpus
    /// rather than rows dated to the moment we ran the migration.
    /// </summary>
    public static readonly DateTime TemporalAnchor = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task EnableSystemVersioningAsync(CancellationToken ct = default, Action<string, Exception>? onError = null)
    {
        if (!Database.IsSqlServer()) return;

        var anchor = TemporalAnchor.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");

        foreach (var table in SystemVersionedTables)
        {
            try
            {
                // Two separate batches on purpose. SET SYSTEM_VERSIONING must see a
                // COMMITTED period — combining ADD PERIOD and the SET in one batch fails
                // on a not-yet-temporal table because SQL Server compiles the SET before
                // the ADD has taken effect, which silently no-ops the enable in prod (a
                // fresh DB) even though it "worked" locally where the tables were already
                // temporal and neither statement ran.
                //
                // DEFAULT '{anchor}' anchors every existing row to Jan 1 2026 (in-world canon
                // zero). Future writes overwrite SysStart with SYSUTCDATETIME() because
                // GENERATED ALWAYS always wins over the column default for INSERTs/UPDATEs.
                await Database.ExecuteSqlRawAsync($"""
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'{table}' AND temporal_type = 2)
                       AND COL_LENGTH(N'[dbo].[{table}]', N'SysStart') IS NULL
                    BEGIN
                        ALTER TABLE [dbo].[{table}] ADD
                            [SysStart] DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL
                                CONSTRAINT DF_{table}_SysStart DEFAULT CONVERT(DATETIME2, '{anchor}'),
                            [SysEnd]   DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL
                                CONSTRAINT DF_{table}_SysEnd DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
                            PERIOD FOR SYSTEM_TIME ([SysStart], [SysEnd]);
                    END;
                    """, ct);

                await Database.ExecuteSqlRawAsync($"""
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'{table}' AND temporal_type = 2)
                    BEGIN
                        ALTER TABLE [dbo].[{table}]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[{table}_History]));
                    END;
                    """, ct);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Failed to enable SYSTEM_VERSIONING on {Table}", table);
                onError?.Invoke(table, ex);
            }
        }
    }
}
