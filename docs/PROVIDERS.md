# Provider Dependencies {#SS-PROVIDERS}

> RFC 0011 Brick 3, item 1: an explicit, checked-in table of which services depend on which
> external provider — replacing tribal knowledge with something anyone can read before assuming
> a service is safe to call. Generated 2026-08-10 by grepping every `v3/Prose.Core/Services/*.cs`
> constructor for `ILlmService`/`EmbeddingService` parameters. Update this file when a service's
> provider dependency changes; it is hand-maintained, not regenerated.

Two provider classes exist in this engine:
- **Anthropic** (Claude API) — via `ILlmService` (`ClaudeService`, routed through `LlmRouter`).
  Used for prose generation and most LLM-judged checks.
- **OpenAI** — via `EmbeddingService` (`text-embedding-3-small`). Used for semantic-similarity
  checks (declared-purpose alignment, near-duplicate detection, canon retrieval).

As of this writing, **Anthropic credit is exhausted** ([[feedback_leaked_api_keys_critical]]) —
every row in the Anthropic-only and Both tables below is currently blocked. OpenAI embeddings are
still reachable, so OpenAI-only services are currently unaffected.

## Anthropic-only (`ILlmService`)

Every row required unless marked Optional. (Mechanical/deterministic checks inside a listed
service are unaffected even when its LLM-dependent checks are blocked — see each service's own
code for which parts are which.)

AgendaEngine · AltitudeAuditService · ArcTrackerService · AmmunitionLinkerService ·
Audit/AuditRunner · BeatChecklistGateService · BeatDuelService · BeatEventSummaryService ·
BeatFactExtractionService · BeatLensServices (CausalityService, AffectBehaviorService,
InterpersonalDynamicsService) · BeatRebuildService · BeatStateExtractor · BlueprintSyncService ·
BehavioralInvariantEnforcer · BibleSyncService · BookStateLedgerService ·
CanonContradictionService · CanonGroundingService · ChapterCloseProcessorService ·
ChapterSummaryService · ChekhovAuditService · CombatSceneWriter · ComprehensionProbeService ·
ContextAnalyzerService · ContinuityValidatorService · ContractGenerator · CoverPromptService ·
CoWriterService · DateBackfillService · DynamicPlaceGenerator · EmotionalLedgerService ·
EmotionalDepthService · EntityRamificationService · EventLogService · GripePassService ·
LibertyReportService · MeaningBackfillService · NarrativeForkService · NarrativeScienceService ·
NarrativeSummaryService · NodeOutlineService · NodeBibleService · OpenThreadsService ·
OutlineReviewService · OutlineAdherenceService · OutlineService · PremiseToOutlineService ·
ProseReflowService · ReaderKnowledgeService · RandomEncounterService · SceneCollisionService ·
StoryDirectorService · StoryScopeAuditService · StoryStateService · StructuralDiagnosticService ·
SynopsisExportService · TextAnalysisService · ThemeCoherenceService · TtsEnhancementService ·
ValidationService (also uses `MultiLlmService` separately) · VoiceHarvestService

**Optional**: AudiblePackageService (LLM only for optional touch-ups on an otherwise-mechanical
package).

## OpenAI-only (`EmbeddingService`)

| Service | Optional? | What it does |
|---|---|---|
| CanonRetrievalService | Required | Universal canon reach — most-relevant entities via embedding similarity |
| DocContextService | Required | Assembles rotating canon-doc context using embedding-based topic matching |
| RelationshipDiscoveryService | Required | Auto-discovers graph edges and suggests semantic entity links |
| BeatDuplicateService | Required | Corpus-wide near-duplicate-beat detector via embedding similarity |
| DialogueService | Optional | Per-character dialogue constraints; pulls semantically-similar voice anchors |
| BeatVerificationService | Optional | Mechanical checks (EventType/SubplotCarrier/EscalationFloor/BannedPattern) need no provider; only DeclaredPurpose is embedding-gated |
| BookReviewService | Optional | Embeddings optionally augment continuity/motif detection (LLM judging itself routes through LlmVotingService, not ILlmService) |

## Both (Anthropic + OpenAI)

| Service | Optional? | What it does |
|---|---|---|
| AskService | Both required | Cloud RAG over the canon corpus |
| BeatGeneratorService | Both required | Core prose-beat generator — the main story-writing engine |
| ContinuousQualityService | Both required | Autonomous monitor: scoped contradiction + cliché checks on saved chapters |
| EntityContextService | Both required | Self-referential entity context stack |
| EntityHarvestService | Both required | Harvests canon from open text, resolves/creates entities |
| FactInterpreterService | Both required | Prose→relational-graph compiler |
| NpcGenerator | Both required | Generates full, permanent NPC characters |
| SceneContextAssembler | Both required | X-Ray scene assembly for prompt context |
| StructuralBlueprintService | Both required | Pre-prose structural blueprint (StoryScope countermeasures) |
| ConversationalWriterService | LLM required, embed optional | "You, Me, and the Page" conversational writing brain |
| EntityExtractionService | LLM required, embed optional | Extracts entities/relationships from story text |
| StoryStarterService | LLM required, embed optional | Zero-input/continuation prose generation |
| SuggestionEngineService | LLM required, embed optional | Proposes 2-3 candidate next beats |

## None — sounds provider-dependent, isn't (worth knowing so nobody assumes it's blocked)

| Service | Actual dependency |
|---|---|
| InferenceService | Pure computation (transitive/shared-property graph inference) |
| SemanticIndexService | TF-IDF keyword search — no embedding call despite "Semantic" in the name |
| EmbeddingHealthService | Reads already-embedded vectors via SQL `VECTOR_DISTANCE`; zero new API calls |
| WritingQualityService | Explicitly heuristic/regex-based; no model call |
| NodeReviewService | `MindAttic.Legion` (`ReviewLlmTransport`/`CloudReviewLlm`) — still Claude-dependent, different transport |
| MultiLlmService | `LegionClient` fan-out to Claude/GPT/Gemini for majority-vote consensus |
| ExpertPersonaService | `LlmVotingService` — a small voting panel, not `ILlmService` directly |
| ClaudeCliService | Spawns the local Claude Code CLI subprocess — not the hosted API |
| DallEService | `LegionClient` (OpenAI images) — a separate OpenAI dependency from `EmbeddingService` |

## Using this table

Before assuming a service is safe to call during a known provider outage, check which bucket it's
in. `BeatVerificationService`'s mechanical checks are a good example of the value here: the
service as a whole is "OpenAI-only, optional" per this table, but that optionality is coarse —
BannedPattern/EventType/SubplotCarrier/EscalationFloor run with zero provider dependency even
when OpenAI is also down; only DeclaredPurpose needs it. Read the service's own code for
check-level granularity this table doesn't capture — it answers "does this service ever touch a
provider," not "does every code path in it."
