# Street Samurai v3 -- Definitive Technical Reference

## What This Is

A cyberpunk narrative engine for literary fiction set in Meridian 88 -- the Great Lakes Metropolitan Zone, year 2200. A continuous 500km urban sprawl along the entire western Lake Michigan shoreline extending into Lake Superior and across the Canadian border, housing 100 million+ people in a tiered society where corponations (corporate nation-states) have replaced governments, and freelancers survive in the gaps between corporate territories.

The system generates prose through a psychology-driven "facet" system -- six competing psychological voices per character that activate based on narrative context. Built on .NET 10 with dual deployment targets: Blazor Server (web) and MAUI (desktop), sharing a common component library.

This is not a chatbot. It is a structured writing tool with a rich HTML editor, autonomous story generation, persistent world state, and a living relationship graph. The AI generates and refines text. You control what stays and what gets rewritten.

**Currency:** (phi) / Quanta

---

## Solution Structure

```
v3/
  StreetSamurai.Core/          Business logic, services, models. No UI. (45 services, 22 models)
  StreetSamurai.Shared/        All Razor pages and components shared by both hosts.
  StreetSamurai.Blazor/        Blazor Server web host (.NET 10).
  StreetSamurai.MAUI/          .NET MAUI desktop host.
  StreetSamurai.UnitTests/     99 tests across 10 test classes.
  generate_world.js            Node.js world content generation pipeline.
  RebuildCanon/                Legacy canon migration tooling.
```

**Core Dependencies:**
- Markdig 1.1.2 -- Markdown-to-HTML rendering
- QuikGraph 2.5.0 -- In-memory directed relationship graph
- System.Speech 10.0.5 -- Windows SAPI TTS fallback
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5
- Microsoft.Extensions.Http 10.0.5 -- HttpClient factory

Both hosts call `services.AddStreetSamuraiServices()` and receive the identical singleton service graph. Both hosts are kept in sync: routes, CSS, JS, and imports.

---

## World Overview: Meridian 88

### Geography

- **Meridian 88** (GLM -- Great Lakes Metropolitan Zone): continuous urban sprawl along the entire western Lake Michigan shore, extending north into Lake Superior and across the Canadian border. 500km of unbroken city, 100M+ residents.
- **Iowa Exclusion Zone**: automated farmland operated by Behemoth agricultural machines. No permanent human habitation. Gleaner Brigades raid the zone for food, dodging autonomous defense systems.
- **Federal Remnant**: the remnant US government, relocated to Denver. Funded by corponations as a political scarecrow to deter foreign annexation. A puppet state with the appearance of sovereignty.
- **Alaska**: a separate nation governed by 13 Tribes (evolved from real ANCSA -- Alaska Native Claims Settlement Act -- corporations into sovereign tribal-corporate entities).
- **Coastal collapse**: Los Angeles, New York City, and Seattle destroyed by climate-driven catastrophe by the 2150s.
- **Mississippi Corridor**: future expansion setting, currently undeveloped in the canon.

### Society

- **Tier system** (1-5 + Excluded): social stratification based on corporate affiliation, augmentation level, and economic utility. Tier 1 = corporate elite. Tier 5 = barely surviving. Excluded = non-persons.
- **Corponations**: corporate nation-states that replaced governments. They own territory, field armies, write laws within their zones, and treat citizenship as employment.
- **Species**: human, ai, android, robot, cyborg_ai, distributed_ai, emergent_ai, rogue_ai, corporate_ai. The world contains both biological and synthetic persons.
- **Worldbuilding philosophy**: the setting extrapolates real BCI (brain-computer interface) technology and corporate sovereignty trends. This is grounded science fiction, not generic cyberpunk. Politics are discussed while walking, not infodumped. Weapons are described by their effect on flesh, not by model number.

---

## Data Architecture

### Per-File JSON Storage

Every entity is its own JSON file in a typed directory. The `JsonDirectoryRepository<T>` base class provides:

- **Git-friendly**: changing one entity touches one file
- **Resilient**: one corrupt file does not break the entire type
- **Partial loading**: only deserialize what you need
- **Scalable**: works with thousands of entities without loading all into memory
- **Human-browsable**: each file is self-contained
- **Auto-migration**: on first access, auto-migrates from legacy single-array files if present

Repository save events fire `OnItemSaved`, which triggers automatic relationship discovery in the world graph.

### Entity Types and Counts

| Type | Repository Class | Directory | Count | Model |
|------|-----------------|-----------|-------|-------|
| Characters | `CharacterRepository` | `engine_data/characters/` | 74 | `CharacterData` |
| Places | `DistrictRepository` | `engine_data/places/` | 203 | `DistrictData` |
| Documents | `WorldbuildingDocRepository` | `engine_data/documents/` | 215 | `WorldbuildingDocument` |
| Weaponry | `WeaponryRepository` | `engine_data/weaponry/` | 185 | `WeaponryData` |
| Equipment | `EquipmentRepository` | `engine_data/equipment/` | 101 | `EquipmentData` |
| Cyberware | `CyberwareRepository` | `engine_data/cyberware/` | 79 | `CyberwareData` |
| Factions | `FactionRepository` | `engine_data/factions/` | 54 | `FactionData` |
| Technology | `TechnologyRepository` | `engine_data/technology/` | 51 | `TechnologyData` |
| Corponations | `CorponationRepository` | `engine_data/corponations/` | 50 | `CorponationData` |
| Ammunition | `AmmunitionRepository` | `engine_data/ammunition/` | 31 | `AmmunitionData` |
| Vocabulary | `VocabularyRepository` | `engine_data/vocabulary/` | 7 | `VocabularyEntry` |
| Facets | `FacetRepository` | `engine_data/facets/` | 6 | `FacetData` |
| Motifs | `MotifRepository` | `engine_data/motifs/` | -- | `MotifData` |
| **Total** | | | **1,056+** | |

**Singleton repositories** (single JSON file, not directory):
- `StoryBibleRepository` -- `engine_data/story_bible.json` (tone, theme, genre, protagonist, core hook)
- `LiteraryRulesRepository` -- `engine_data/literary_rules.json` (hard prose constraints)
- `CharacterProfileRepository` -- `engine_data/character_profile.json` (Kyle's core identity)

### Directory Layout

```
{CanonRoot}/
  engine_data/
    characters/           74 files — full character profiles with psychology
    places/               203 files — districts, buildings, landmarks
    corponations/         50 files — corporate nation-states
    factions/             54 files — organizations, gangs, movements
    weaponry/             185 files — weapons with tactical use and cultural context
    equipment/            101 files — gear, tools, vehicles
    cyberware/            79 files — augmentations with body location and manufacturer
    technology/           51 files — tech concepts and systems
    ammunition/           31 files — ammo types and effects
    documents/            215 files — long-form worldbuilding lore
    vocabulary/           7 files — in-world terminology
    facets/               6 files — psychological facet definitions
    motifs/               recurring thematic elements
    story_bible.json      tone, theme, genre, protagonist, core hook, arc
    literary_rules.json   hard constraints on prose style
    character_profile.json  Kyle's core identity and contradictions
    tts_rules.json        TTS pronunciation and style rules
    graph/
      world_graph.json    relationship network snapshot
  story_blocks/
    {UUID}.json           one file per story project (HTML source of truth)
  audio/
    narration_*.mp3       generated audio files
```

---

## Service Architecture

All services are registered via `ServiceCollectionExtensions.AddStreetSamuraiServices()`. Both Blazor and MAUI hosts call this single method.

### Startup and Eager Loading

At startup:
1. `WorldGraphService.EnsureLoaded()` -- builds the in-memory graph from all repositories (or loads from snapshot)
2. `SemanticIndexService.RebuildIndex()` -- builds TF-IDF index over all graph nodes
3. `InferenceService.RebuildPropertyIndex()` -- builds property index for transitive relationship inference
4. `RelationshipDiscoveryService` -- wires repository `OnItemSaved` events for auto-discovery

This eliminates first-load latency. Every generation call can query the graph immediately.

### Complete Service Registry

| Service | Lifetime | Layer | Purpose |
|---------|----------|-------|---------|
| `SettingsService` | Singleton | Config | Auto-detects canon root, auto-saves on property change |
| `FileSecurePreferences` | Singleton | Config | AES-encrypted credential store (machine-locked) |
| `FileSystemPathProvider` | Singleton | Config | Resolves all directory paths |
| `CharacterRepository` | Singleton | Data | Per-file character storage |
| `CorponationRepository` | Singleton | Data | Per-file corponation storage |
| `DistrictRepository` | Singleton | Data | Per-file place storage |
| `FactionRepository` | Singleton | Data | Per-file faction storage |
| `FacetRepository` | Singleton | Data | Per-file facet definition storage |
| `WorldbuildingDocRepository` | Singleton | Data | Per-file document storage |
| `WeaponryRepository` | Singleton | Data | Per-file weapon storage |
| `AmmunitionRepository` | Singleton | Data | Per-file ammunition storage |
| `EquipmentRepository` | Singleton | Data | Per-file equipment storage |
| `TechnologyRepository` | Singleton | Data | Per-file technology storage |
| `CyberwareRepository` | Singleton | Data | Per-file cyberware storage |
| `VocabularyRepository` | Singleton | Data | Per-file vocabulary storage |
| `MotifRepository` | Singleton | Data | Per-file motif storage |
| `StoryBibleRepository` | Singleton | Data | Single-file story bible |
| `LiteraryRulesRepository` | Singleton | Data | Single-file literary rules |
| `CharacterProfileRepository` | Singleton | Data | Single-file protagonist profile |
| `DatabaseService` | Singleton | Data | Aggregates all repositories, cross-cutting queries |
| `LoreService` | Singleton | Data | Higher-level API mapping typed models to runtime models |
| `WorldGraphService` | Singleton | World | QuikGraph in-memory relationship network |
| `SemanticIndexService` | Singleton | World | TF-IDF cosine similarity search |
| `InferenceService` | Singleton | World | Transitive relationship inference |
| `RelationshipDiscoveryService` | Singleton | World | Auto-creates graph edges on entity save |
| `NavigationService` | Singleton | World | Zork-style directional exits, A* pathfinding |
| `DynamicPlaceGenerator` | Singleton | World | Creates places on-the-fly as stories unfold |
| `StoryStateService` | Singleton | Story | Real-time character state within a story |
| `EventLogService` | Singleton | Story | Structured event records (who, what, where, when) |
| `OutlineService` | Singleton | Story | Story arc planning (acts, beats, seeds/payoffs) |
| `AgendaEngine` | Singleton | Story | Character goals to conflict detection to scene premises |
| `KnowledgeMapService` | Singleton | Story | Information asymmetry tracking (dramatic irony, POV leaks) |
| `ContractGenerator` | Singleton | Freelancer | Structured job creation with twists and moral dilemmas |
| `NpcGenerator` | Singleton | Freelancer | Full character generation, saved permanently to repo |
| `RandomEncounterService` | Singleton | Freelancer | 25 encounter types injected between beats |
| `ReputationTracker` | Singleton | Freelancer | Per-faction reputation (-100 to +100), persists across stories |
| `ConsequenceEngine` | Singleton | Freelancer | Cross-story consequence bleed |
| `FacetService` | Singleton | Generation | Facet scoring, selection, rotation enforcement |
| `ContextAnalyzerService` | Singleton | Generation | Extracts psychological triggers from scene context |
| `BeatGeneratorService` | Singleton | Generation | Generates individual narrative beats |
| `SceneGenerationService` | Singleton | Generation | Multi-beat scene orchestration |
| `StoryStarterService` | Singleton | Generation | Openings, continuations, rewrites |
| `StoryDirectorService` | Singleton | Generation | Fully autonomous "Surprise Me" story generation |
| `TextAnalysisService` | Singleton | Generation | Lore check, cliche check, expand, rephrase |
| `NarrativeSessionContext` | Scoped | Generation | Fog-of-war entity loading during generation |
| `EntityExtractionService` | Singleton | Generation | Extracts entities from prose into graph |
| `ClaudeService` | HttpClient | LLM | Anthropic Claude API client |
| `OpenAiService` | HttpClient | LLM | OpenAI Chat Completions API client |
| `MultiLlmService` | HttpClient | LLM | Multi-provider voting |
| `LlmRouter` / `ILlmService` | Singleton | LLM | Routes to active provider at runtime |
| `ElevenLabsTtsService` / `ITtsService` | HttpClient | Audio | ElevenLabs TTS synthesis |
| `WindowsTtsService` | Singleton | Audio | Free Windows SAPI fallback |
| `TtsEnhancementService` | Singleton | Audio | ElevenLabs audio tag injection |
| `AudioFileService` / `IAudioFileService` | Singleton | Audio | Audio file save + explorer reveal |
| `MarkdownService` | Singleton | Util | Markdig pipeline with facet tag coloring |
| `ExportService` | Singleton | Util | TXT, MD, PDF (print), MP3, OGG export |
| `ValidationService` | Singleton | Util | Input and canon validation |
| `StoryService` | Singleton | Legacy | Legacy story persistence |
| `JsonStoryBlockRepository` / `IStoryBlockRepository` | Singleton | Data | JSON story project persistence |

---

## World Layer -- What EXISTS

The world layer represents the persistent state of Meridian 88. It answers: "What is true about this world right now?"

### WorldGraphService

In-memory relationship graph using QuikGraph's `AdjacencyGraph<string, WorldEdge>`.

**Nodes** -- Every entity becomes a `WorldNode`:
- `Id` -- Slugified: lowercase, spaces to underscores, non-alphanumeric stripped
- `Name` -- Display name
- `NodeType` -- `"character"` | `"place"` | `"faction"` | `"organization"` | `"weapon"` | `"equipment"` | `"technology"`
- `Properties` -- Key/value metadata bag (description, role, aliases, affiliation, etc.)
- `CanonStatus` -- `"canon"` | `"experimental"` | `"rejected"`

**Edges** -- `WorldEdge` connects two nodes:
- `RelationType` -- `"affiliated_with"`, `"member_of"`, `"adjacent_to"`, `"manufactured_by"`, `"located_in"`, `"friend"`, `"rival"`, `"enemy"`, etc.
- `Weight` -- 0-10 intensity scale
- `Sentiment` -- `"positive"` | `"negative"` | `"neutral"` | `"mixed"` (heuristic-inferred)
- `Description` -- Narrative explanation of the relationship

**Auto-build**: On first load, if `world_graph.json` does not exist, the graph is built from all typed repositories. Characters, places, factions, corponations, weapons, equipment, and technology are all parsed for relationship references.

**Key methods:**
- `GetContextForNode(id)` -- Formatted text for LLM prompts: node properties + all connected edges
- `GetNeighbors(id, depth)` -- BFS traversal to find entities within N hops
- `EvolveRelationship(sourceId, targetId, ...)` -- Updates edge weight during scene generation
- `Rebuild()` -- Full graph rebuild from repository data

**Persistence**: `GraphSnapshot` (nodes[] + edges[] + lastSaved) to `engine_data/graph/world_graph.json`.

### SemanticIndexService

TF-IDF cosine similarity search over the entire world graph. Indexes all entity descriptions, relationships, and properties. Enables thematic search -- searching "corporate betrayal" surfaces characters with betrayal backstories, not just entities with those exact words.

- Builds a term-frequency / inverse-document-frequency vector for every graph node
- Custom stop word list (100+ common English words filtered out)
- Used for finding thematically relevant entities during generation

### InferenceService

Computes virtual (non-persisted) transitive relationships between entities. Two strategies:

1. **Shared-hub inference**: If A connects to hub H, and B connects to hub H, then A and B have an inferred relationship through H
2. **Shared-property inference**: If A.manufacturer == B.manufacturer, infer a relationship. Indexed properties: `manufacturer`, `affiliation`, `location`, `sector`, `territory`, `tier_availability`, `category`, `role`

Results are cached per node and invalidated on graph changes.

### RelationshipDiscoveryService

Automatically discovers and creates graph edges when entities are saved. Two strategies:

1. **Structured property edges**: scans `affiliation`, `manufacturer`, `location` fields for direct entity references
2. **Text mention scanning**: scans `description`, `story_hooks`, `cultural_context`, `narrative_function`, `founding_story`, `ideology`, `tactical_use` for entity name mentions

Wired to repository `OnItemSaved` events -- every save triggers automatic relationship discovery. No manual graph rebuilds needed.

### NavigationService

Geographic navigation between places using Zork-style directional exits (N/NE/E/SE/S/SW/W/NW/UP/DOWN).

- **Exit computation**: Exits are computed from real-world coordinates -- the nearest place in each compass direction becomes that direction's exit
- **Distance thresholds**: Chicago core uses 5km radius, outer regions use 50km radius
- **Pathfinding**: A* algorithm for finding the sequence of places between two locations ("How do I get from The Shelf to Green Bay?")
- **Exit types**: road, guarded checkpoint, tunnel, maglev station, waterway, maintenance corridor -- each with restriction status and danger level

### DynamicPlaceGenerator

Creates places on-the-fly as characters move through the world during story generation.

- When a character goes DOWN into sewers, the sewer place is created and saved
- Elevator sequences generate minimal entries for pass-through floors and full descriptions for destinations
- Every generated place is saved to the Places repository -- the world grows as stories are told
- Places where action happens get full atmosphere; pass-through places get minimal entries

---

## Story Layer -- What's HAPPENING

The story layer tracks the narrative state of an active story. It answers: "What is happening in this story right now, and what does each character know?"

### StoryStateService

Real-time character state within a story. After each generation beat, the LLM extracts what changed and the state updates. The next generation call receives this state as hard constraints.

**Tracked per character:**
- Current location
- Emotional state
- Active injuries
- Inventory (gained/lost)
- Knowledge (newly learned facts)

**Key distinction**: The world model says "Kyle carries a katana." The story state says "Kyle set the katana down on Mrs. Chen's counter in paragraph 3." Story state prevents continuity errors at generation time rather than catching them after the fact.

### EventLogService

Structured event records extracted from generated prose via LLM. Each event has:
- `type`: action | dialogue | revelation | decision | conflict | arrival | departure | injury | death | discovery | emotional_shift
- `summary`: one-sentence description
- `participants`: character names involved
- `location`, `object`, `consequence`
- `emotional_weight`: 1-10
- `tags`: thematic tags (betrayal, trust, violence, tenderness)

Searchable and queryable -- the system can answer "when did X last happen?" without re-reading the entire story. Events are persisted alongside their story project.

### OutlineService

Plans multi-scene story arcs as living documents. Generates structured beat sheets with:

- **3-act structure**: setup, confrontation, resolution
- **Per-beat data**: title, goal, characters present, location, emotional arc, stakes, tension (1-10), facet hint
- **Seeds and payoffs**: every planted thread tracks which beat it was planted in and which beat resolves it
- **Character arcs**: start state, end state, turning point, cost
- **Modifiable**: outlines can be extended, modified, or regenerated as the story evolves

Beats are marked as written as the StoryDirectorService progresses through them.

### AgendaEngine

Character goal engine that drives autonomous story generation. Instead of the user saying "write a scene where X happens," the engine identifies where character goals collide and generates scene premises from the collision.

Example: "Sable wants to protect her information network. Kyle wants to expose the facility. These goals collide when the facility's location is in Sable's files."

- Reads character psychology, current story state, and recent events
- Determines what each character WANTS to do next
- Detects goal conflicts between characters
- Generates ranked scene premises from those conflicts

### KnowledgeMapService

Tracks information asymmetry: what each character knows, what the reader knows, and when things were learned.

- **Per-character knowledge**: facts learned, which beat they were learned in, source
- **Reader knowledge**: separate tracking of what the reader knows (may differ from any single character)
- **Dramatic irony**: when the reader knows something a character does not
- **POV leak prevention**: generates constraints that prevent the narrator from revealing information the POV character does not have

Example constraint: "The reader knows Sable has the facility files. Kyle does not know this. When writing Kyle's POV, do not reveal this information. When writing Sable's behavior, show subtle signs of concealment."

---

## Freelancer Systems

Systems that generate and track the freelance contract economy of Meridian 88.

### ContractGenerator

Generates structured freelance contracts grounded in real canon entities:

- **16 job types**: extraction, retrieval, sabotage, protection, assassination, delivery, intel_gathering, escort, demolition, surveillance, debt_collection, evidence_destruction, blackmail_retrieval, hostage_negotiation, smuggling, counter_surveillance
- **15 complication types**: target_not_who_they_said, double_cross, civilian_presence, personal_connection_to_target, rival_operator_on_same_contract, etc.
- **Payout**: random range 500-50,000 (Quanta)
- Each contract has: client, target, location, payout, complication, twist, moral dilemma, success/failure consequences
- Grounded in real entities: picks actual corponations, factions, and districts from the database

### NpcGenerator

Generates NPCs as FULL characters -- no disposable throwaways. Every generated character is saved to the character repository permanently and becomes part of the world.

A random guard from contract #3 might become a recurring ally in contract #7 because the system remembers them. Generated characters include complete psychology, speech patterns, relationships, and facet weights.

### RandomEncounterService

25 encounter types that can be injected between story beats:

`mugging_attempt`, `corporate_security_sweep`, `augment_malfunction`, `gang_territorial_dispute`, `surveillance_drone_pursuit`, `street_fight`, `black_market_deal_gone_wrong`, `building_collapse`, `power_outage`, `wanted_poster_recognition`, `old_debt_collector`, `rogue_ai_manifestation`, `chemical_spill`, `sniper_shot`, `stolen_vehicle_crash`, `fire`, `refugee_confrontation`, `corrupt_cop_shakedown`, `augment_rejection_seizure`, `underground_tunnel_flood`, `data_heist_in_progress`, `hostage_situation`, `street_preacher_provocation`, `weapons_malfunction`, `identity_scanner_alert`

Encounter danger scales to current scene tension -- does not inject a mugging into an active firefight.

### ReputationTracker

Per-faction reputation that persists across stories:
- Range: -100 (kill on sight) to +100 (trusted ally)
- Adjusted by character actions during stories
- History of all reputation changes with reasons and timestamps
- Affects contract availability, NPC behavior, and world reactions in future stories

### ConsequenceEngine

Cross-story consequence bleed. Actions in Story 1 affect Story 2:
- Burned a warehouse? The faction remembers
- Saved a kid? The parent becomes a contact
- Contract outcomes (success/failure) generate persistent consequences
- Moral choices create their own consequence entries
- Consequences are injected as world state into future generation prompts

---

## The Six-Facet System

The core narrative engine. Every character has six psychological facets, each a competing voice that drives or colors the prose.

### The Facets

| Facet | Domain | What It Drives | Color |
|-------|--------|----------------|-------|
| **Wound** | Trauma, emotional pain | Vulnerability, flashbacks, self-destruction | #dc3545 |
| **Ideal** | Aspirational self | Hope, sacrifice, moral clarity | #198754 |
| **Id** | Raw desire, survival instinct | Hunger, rage, lust, self-preservation | #ffc107 |
| **Shadow** | Denied aspects | Hypocrisy, projection, hidden cruelty | #6f42c1 |
| **Mask** | Social facade | Performance, manipulation, charm | #0dcaf0 |
| **Ghost** | Haunted past | Memory, regret, the weight of history | #6c757d |

### Facet Definition Structure

Each facet (from `engine_data/facets/`) has:
- **Triggers** -- Context keywords that activate it (e.g., Wound triggers on `"violence"`, `"betrayal"`, `"loss"`)
- **SystemPrompt** -- LLM personality directive when this facet leads
- **VoiceTone** -- Prose style description (e.g., "raw, trembling, stripped bare")
- **VoiceStyle** -- Narrative technique
- **Prohibitions** -- What this voice must never do
- **CoreMemories** -- Recurring memories the facet surfaces
- **Model** -- LLM model override (default: `claude-sonnet-4-6`)
- **Temperature** -- Generation temperature (default: 0.8)

### Facet Weights

Each character has a `FacetWeights` in their `Psychology`:

```
{ wound: 0.7, ideal: 0.4, id: 0.5, shadow: 0.6, mask: 0.3, ghost: 0.8 }
```

For ensemble casts (multiple characters in a scene), weights are averaged via `DatabaseService.GetBlendedWeights()`.

### Facet Selection Algorithm

`FacetService.SelectFacets(weights, contextTags, recentLeads)`:

1. For each of the 6 facets, compute a score:
   - Count how many of the facet's `Triggers` overlap with the current `contextTags`
   - Multiply by the character's weight for that facet
2. Sort by score descending
3. **Rotation enforcement**: If the top-scoring facet has been the lead for 3+ consecutive beats, demote it and pick the next
4. Return: `(leadFacet, [supporting1, supporting2])` -- one lead voice, two supporting

The **lead facet** controls the system prompt tone, LLM model/temperature, and dominant voice. The **supporting facets** surface as brief interior interjections tagged with `[FACET_NAME]` or as tonal undercurrents.

### Context Tag Sources

1. **ContextAnalyzerService** (scene generation): Sends scene-so-far + character relationships to LLM at temperature 0.3. Returns structured JSON with `psychological_triggers`, `dominant_emotion`, `stakes`, `tension_source`.
2. **StoryStarterService.InferTriggers** (story starters): Keyword matching on premise text. Maps words like "betray" to `betrayal`, "augment" to `transhumanism`. Falls back to `["unknown_danger", "moral_choice"]`.

---

## Character System

### Full Character Model

Each `CharacterData` entity includes:

**Identity**: name, aliases, species, gender, pronouns, role, age, status (alive/dead/missing), location, description, affiliation, narrative function, daily life, augmentations

**Species classifications**: `human`, `ai`, `android`, `robot`, `cyborg_ai`, `distributed_ai`, `emergent_ai`, `rogue_ai`, `corporate_ai`

**Psychology** (`CharacterPsychology`):
- `FacetWeights` -- six floats (0-1) controlling facet activation
- `CoreFears` -- what terrifies them
- `CoreDesires` -- what they want most
- `CopingMechanisms` -- how they handle stress
- `BlindSpots` -- what they cannot see about themselves
- `Secret` -- what they hide from everyone

**Stats** (`CharacterStats`):
- `Physical` -- physical capabilities (1-10 scale)
- `Mental` -- cognitive capabilities (1-10 scale)
- `Social` -- interpersonal capabilities (1-10 scale)
- `Personality` -- personality axes (-5 to 5)
- `Drives` -- core motivations
- `Thresholds` -- breaking points for various triggers
- `Strengths` / `Weaknesses` / `Tags`

**Behavioral** (`CharacterBehavioral`):
- `DecisionRules` -- hard rules ("Will always X", "Will never Y")
- `EscalationLadder` -- observation to lethal force progression
- `InterpersonalModes` -- different behavior with specific people
- `StressResponses` -- what happens at each stress level
- `Contradictions` -- when internal values conflict
- `Habits` -- habitual actions in common situations
- `BreakingPoints` -- what triggers them to break their own rules

**Speech Patterns** (`SpeechPatterns`):
- `Vocabulary` -- word choice and register
- `Cadence` -- sentence rhythm and length
- `VerbalTics` -- repeated phrases or verbal habits
- `ExampleLines` -- concrete dialogue samples

**Relationships** (`CharacterRelationship[]`):
- `Name`, `Type`, `Description`, `EmotionalCore`, `StoryTension`

**Cyberware Inventory** (`CyberwareEntry[]`):
- Each entry: name, body location, manufacturer, tier, condition (functional/damaged/destroyed), installed date, description, what it replaces

**Timeline** (`TimelineEvent[]`):
- Chronological events across all stories
- Tracks date, story ID, event description, consequences, body changes, status changes
- Ensures continuity: lost limbs stay lost, dead characters stay dead, injuries persist until treated

**Story Hooks**: future narrative threads attached to this character

### Uses Facets Flag

Only Kyle (the protagonist) has `uses_facets: true`. Other characters have a `narration_voice` field that describes their POV prose style directly, without the facet tag system.

---

## LLM System

### Architecture

```
ILlmService (interface)
  |-- ClaudeService      Anthropic Claude API
  |-- OpenAiService      OpenAI Chat Completions API
  |-- MultiLlmService    Multi-provider voting
  +-- LlmRouter          Runtime multiplexer based on ActiveLlmProvider setting
```

All consumers inject `ILlmService`, which resolves to `LlmRouter`. The router reads `SettingsService.ActiveLlmProvider` on every call and delegates to the matching provider. Provider switching requires no restart.

### ILlmService Interface

```csharp
Task<bool> IsConfiguredAsync();
Task<string> GenerateAsync(
    string system,          // System prompt
    string user,            // User prompt
    double temperature,     // 0.0-1.0
    int maxTokens,          // Max response tokens
    string? model,          // Override model ID (null = use default)
    CancellationToken ct);
```

### Provider Details

**ClaudeService**: Endpoint `https://api.anthropic.com/v1/messages`, header `x-api-key`, 3-minute timeout, default model `claude-sonnet-4-6`, snake_case JSON.

**OpenAiService**: Endpoint `https://api.openai.com/v1/chat/completions`, Bearer auth, 3-minute timeout, default model `gpt-4-1-mini`.

**MultiLlmService**: Sends the same prompt to multiple providers and aggregates results for quality comparison (majority voting).

### LLM Call Sites

| Caller | Temperature | Max Tokens | Purpose |
|--------|------------|------------|---------|
| `BeatGeneratorService` | Facet's temp (0.8) | 2048 | Individual narrative beats |
| `StoryStarterService` (opening) | Facet's temp | 2048 | Story opening |
| `StoryStarterService` (title) | 0.9 | 50 | Title generation ("like graffiti on a wall") |
| `StoryStarterService` (continue) | Facet's temp | 2048 | Story continuation |
| `StoryStarterService` (polish) | 0.4 | 4096 | Prose refinement |
| `ContextAnalyzerService` | 0.3 | -- | Psychological trigger extraction |
| `TextAnalysisService` (lore check) | 0.3 | -- | Canon consistency check |
| `TextAnalysisService` (cliche check) | 0.3 | -- | Cliche detection |
| `TextAnalysisService` (expand) | 0.85 | -- | Text expansion |
| `TextAnalysisService` (rephrase) | 0.7 | -- | Text rephrasing |
| `AgendaEngine` | 0.8 | 4096 | Character goal/conflict generation |
| `OutlineService` | 0.8 | 4096 | Story arc planning |
| `ContractGenerator` | 0.8 | 4096 | Freelance contract generation |
| `NpcGenerator` | 0.8 | 4096 | Full character generation |
| `RandomEncounterService` | 0.8 | 2048 | Street encounter generation |
| `StoryStateService` | 0.3 | -- | Narrative state extraction |
| `EventLogService` | 0.3 | -- | Event extraction from prose |
| `DynamicPlaceGenerator` | 0.7 | 2048 | Place generation |

---

## Generation Pipelines

### Pipeline 1: Scene Generation (Multi-Beat)

`SceneGenerationService.GenerateSceneAsync(SceneRequest, FacetState)`

Used by the `/generate` page. Produces a multi-beat scene with facet rotation.

**Input (SceneRequest):** Goal, Location, Characters[], Themes[] (optional per-beat), NumBeats (3-8, default 5), ForcedLeadFacet (optional override).

**Flow:**
```
For each beat (0 to numBeats-1):
  1. ContextAnalyzerService.AnalyzeAsync(sceneSoFar, characters)
     -> psychological triggers, dominant emotion, stakes

  2. FacetService.SelectFacets(weights, triggers, recentLeads)
     -> (lead, [support1, support2])
     -> Rotation prevents same lead 3+ times in a row

  3. Fire OnBeatProgress event (UI updates progress bar)

  4. BeatGeneratorService.GenerateBeatAsync(context, lead, supporting)
     -> System prompt: lead voice + story bible + literary rules
        + supporting voices + core memories + character context + location
     -> User prompt: scene so far + beat goal
     -> LLM generates 2-4 paragraphs

  5. Accumulate beat text into sceneSoFar
  6. Fire OnBeatCompleted event (UI renders new beat in real-time)
```

**Output (GeneratedScene):** Beats[] (index, goal, leadFacet, supportingFacets, text, contextTags), FullText.

### Pipeline 2: Story Starter (Single-Shot)

`StoryStarterService` -- used by the `/write` page. Simpler, single-shot generation.

**GenerateOpeningAsync(StoryStarterRequest):**
1. Load world context: story bible, literary rules, location atmosphere, character psychology
2. Blend facet weights across all named characters
3. Infer trigger tags from premise keywords
4. Select lead + supporting facets
5. Build layered system prompt with all context
6. User prompt: premise + mood + "Drop us in the middle of something, 3-5 paragraphs"
7. LLM generates opening prose
8. Second LLM call: generate title (temperature 0.9, max 50 tokens)
9. Return `GeneratedOpening` (title, text, lead facet, supporting facets, characters, location)

**GenerateRandomAsync():** Picks 1-3 random characters, random district, random seed premise (from actual world tensions), random mood. Calls `GenerateOpeningAsync`.

**ContinueAsync(existingText, prompt, mood, location, characters):**
- Same world context construction
- Uses `NarrativeSessionContext` for fog-of-war entity enrichment
- Receives story state constraints, knowledge constraints, event context, outline context
- Sends existing story as "STORY SO FAR"
- Returns 2-4 paragraphs

### Pipeline 3: Autonomous Story Director ("Surprise Me")

`StoryDirectorService.SurpriseMeAsync()` -- fully autonomous end-to-end story generation.

**Phases:**
```
Phase 1: Pick protagonist + 2-4 supporting cast
  - Weights toward non-Kyle protagonist (70%) for variety
  - Preferentially selects characters with existing relationships

Phase 2: Generate premise from character goal conflicts
  - AgendaEngine identifies where character goals collide
  - Picks the strongest conflict as the story premise

Phase 3: Pick location from known districts

Phase 4: Generate outline with mandatory battle beat
  - OutlineService generates 3-act structure with ~8 beats
  - EnsureBattleBeat() injects combat if the LLM omitted it
  - "Meridian 88 is dangerous. Every story MUST include combat."

Phase 5: Write each beat sequentially
  - Initialize character state for all cast members
  - For each beat in outline:
    a. Build full context: story state, knowledge constraints,
       event log, outline context, dialogue voice constraints
    b. First beat -> GenerateOpeningAsync()
       Subsequent -> ContinueAsync() with all accumulated constraints
    c. After generation: UpdateFromTextAsync() extracts state changes,
       ExtractAndLogAsync() captures events, KnowledgeMap syncs
    d. Mark beat as written in outline

Phase 6: Assemble complete story with section breaks
```

**Dialogue voice constraints**: For multi-character scenes, builds explicit voice distinction rules -- each character's vocabulary, cadence, and example lines are injected to ensure they never sound alike.

---

## Prompt Architecture

Every LLM call builds a system prompt from layered context:

```
SYSTEM PROMPT:
  1. Role statement ("You are a literary fiction author writing cyberpunk...")
  2. Lead facet voice (system prompt + voice tone from facet definition)
  3. Supporting facet voices (may interject as [FACET_NAME] tagged lines)
  4. Story Bible (title, genre, tone, core theme, core hook, arc, protagonist)
  5. Literary Rules -- NON-NEGOTIABLE hard constraints
  6. Location context (atmosphere: sights, sounds, smells, feel + dangers + opportunities)
  7. Character context (psychology, fears, desires, speech patterns, relationships)
  8. World flavor (random corponation details, protagonist contradiction)
  9. Story state constraints (injuries, inventory, location, emotional state)
  10. Knowledge constraints (POV-safe information, dramatic irony notes)
  11. Event log context (recent events for continuity)
  12. Outline context (where this beat sits in the arc, seeds to plant/pay off)
  13. Dialogue voice constraints (per-character speech distinction rules)

USER PROMPT:
  1. Scene so far (for continuations)
  2. Mood/tone directive (if specified)
  3. Premise or direction
  4. Structural instruction ("3-5 paragraphs", "end on tension", etc.)
  5. Format constraint ("Write ONLY the story text. No titles, no headers.")
```

Literary rules are injected as "NON-NEGOTIABLE" in every generation call. They enforce sentence length limits, ban specific cliches, require grounded prose, and prohibit generic cyberpunk tropes.

### NarrativeSessionContext

Session-scoped fog-of-war context. As entities are mentioned in narrative, their 2-hop graph neighborhoods load progressively. `BuildContext()` produces a layered prompt:
- Primary entities: full briefs
- Secondary entities (2-hop neighbors): compact one-liners

Used in ContinueAsync, DoWrite, ExecuteAsk, and SceneGenerationService.

### EntityExtractionService

Extracts entities from generated prose and maps them into the world graph. Keeps the graph up-to-date as new narrative introduces or references entities.

---

## Story System

### StoryProject Model

Stories are stored as `StoryProject` with a single `html` field. Rich HTML is the source of truth. Plain text is derived by stripping HTML tags. One JSON file per project, named by UUID.

```json
{
  "id": "e85880cc638c4c68b7b7707526ac52fc",
  "title": "Story Title",
  "characters": ["Kyle", "Sable"],
  "status": "draft",
  "html": "<p>Rich HTML content...</p>",
  "created": "2026-04-03T20:49:00Z",
  "modified": "2026-04-03T21:23:34Z"
}
```

Stored at `{CanonRoot}/story_blocks/{UUID}.json`.

### IStoryBlockRepository Interface

```csharp
List<StoryProject> ListProjects();
StoryProject? LoadProject(string id);
void SaveProject(StoryProject project);
void DeleteProject(string id);
```

This is the migration seam. Implement with EF Core or Dapper, register in DI, nothing else changes.

---

## Text-to-Speech

### ElevenLabsTtsService

- API: `POST https://api.elevenlabs.io/v1/text-to-speech/{voiceId}`
- Header: `xi-api-key`
- Returns: audio byte array (MP3 or OGG)
- Timeout: 2 minutes
- Default voice: Oliver Silk (`jfIS2w2yJi0grJZPyEsk`)
- Default model: `eleven_v3`
- Per-request settings: stability, similarity_boost, style, use_speaker_boost

### TtsEnhancementService

Pre-processes text before synthesis. Adds ElevenLabs audio tags for pronunciation rules, pauses, emphasis, and style markup from `tts_rules.json`.

### WindowsTtsService

Free Windows SAPI fallback using `System.Speech`. For local synthesis when ElevenLabs is not configured or for quick previews.

### AudioFileService

Saves audio to `{CanonRoot}/audio/narration_{timestamp}.mp3`. `RevealInExplorer()` opens the file manager with the file selected.

---

## Text Analysis Tools

`TextAnalysisService` provides editor-integrated analysis:

| Method | Temperature | What It Does |
|--------|------------|--------------|
| `LoreCheckAsync(text, context)` | 0.3 | Searches database + graph for entities mentioned in text, sends to LLM to check for contradictions against canon |
| `ClicheCheckAsync(text)` | 0.3 | Checks against literary rule prohibitions (no noir cliches, no "chrome gleaming," no katana fetishism), suggests concrete fixes |
| `ExpandAsync(text, context)` | 0.85 | Continues prose from selection, maintaining voice and style |
| `RephraseAsync(text)` | 0.7 | Rewrites selected text with different word choices while preserving meaning |

---

## Export

`ExportService` provides multiple output formats:

| Method | Output |
|--------|--------|
| `ToPlainText()` | Plain text with HTML tags stripped |
| `ToMarkdown()` | Markdown conversion from HTML |
| `ToPrintHtml()` | Formatted HTML for browser print-to-PDF |

Audio exports use `ElevenLabsTtsService` directly:
- **MP3** -- `mp3_44100_128` format
- **OGG** -- `ogg_vorbis` format

---

## UI Pages

### / -- Home

Dashboard with stats: entity counts across all types, graph size, story count, configured service status.

### /write, /write/{ProjectId} -- Write Story

The primary authoring workspace. Rich HTML editor with contenteditable div.

**Layout:**
1. Title bar -- editable title, character list, story status
2. Formatting toolbar -- H1, H2, H3, P, Bold, Italic, and other formatting controls
3. Rich HTML editor -- contenteditable div, the source of truth
4. Export dropdown -- TXT, MD, PDF, MP3, OGG
5. Ask Modal -- query world graph + story context with LLM
6. Write Modal -- generate opening or continuation with full graph context enrichment
7. Validate Modal -- lore check and cliche check
8. What Next? Modal -- suggestions for story continuation
9. Outline Modal -- view/modify story arc
10. ElevenLabs tag bar -- TTS markup insertion
11. Audio player -- appears after narration
12. Scene sidebar -- entity detection, validation, mini-graph
13. Context menu -- read selected, read all, rewrite, expand

**Key behaviors:**
- Auto-detect characters from story text via `NarrativeSessionContext.ScanText()`
- Auto-save on timer
- Routes: `/write` (new project) or `/write/{ProjectId}` (load existing)

### /stories -- Stories

Browse and manage all saved story projects. Includes "Surprise Me" button for autonomous `StoryDirectorService` generation with live progress indicator.

### /characters -- Character Dictionary

Full character profiles: identity, psychology, speech patterns, facet weights (bar chart), relationships, behavioral rules, cyberware inventory, timeline, story hooks. Inline editing.

### /corps -- Corponation Dictionary

Corporate nation-state profiles: territory, ideology, founding story, military capability, economic sector.

### /factions -- Faction Dictionary

Organization profiles: ideology, territory, membership, affiliations, conflicts.

### /places -- Place Dictionary

Places with atmosphere (sights/sounds/smells/feel), demographics, economy, power structure, dangers, opportunities, story hooks, notable locations, directional exits, coordinates.

### /technology -- Technology Dictionary

Technology entries with descriptions and world context.

### /weaponry -- Weaponry Dictionary

Weapons with tactical use, cultural context, manufacturer, tier availability.

### /equipment -- Equipment Dictionary

Gear, tools, and vehicles with specifications.

### /cyberware -- Cyberware Dictionary

Augmentations with body location, manufacturer, tier, effects.

### /documents -- Document Dictionary

Long-form worldbuilding documents -- deep lore, history, sociology, politics.

### /vocabulary -- Vocabulary Dictionary

In-world terminology: street slang, corporate jargon, technical terms with definitions and usage context.

### /graph -- World Graph

D3 force-directed graph visualization of the entire relationship network. Opens in a new browser window. All entity types visible as colored nodes with relationship edges.

### /settings -- Settings

**LLM Providers** (12 providers, each with live connectivity testing):
Anthropic, OpenAI, ElevenLabs, Gemini, DeepSeek, Mistral, Grok, Groq, Together, OpenRouter, Fireworks, Cohere

**Configuration**: model selection, provider toggle, TTS voice/model/settings, canon root path, editor font size, UI theme.

Shows configured/unconfigured status per provider with test buttons.

### Shared Components

| Component | Purpose |
|-----------|---------|
| `AudioPlayer` | HTML5 audio playback with download and reveal-in-explorer |
| `CanonStatusBadge` | Canon/experimental/rejected status indicator |
| `FacetBadge` | Colored facet name badge |
| `FacetWeightChart` | Bar chart of facet weights per character |
| `LoadingSpinner` | Loading indicator |
| `MarkdownPreview` | Rendered markdown display |
| `Placeholder` | Empty state placeholder |
| `TechEdges` | Decorative cyberpunk edge styling |

---

## Configuration

### Settings

`SettingsService` persists to `%LOCALAPPDATA%/MindAttic/StreetSamurai/Settings.json`. Every property setter auto-saves.

| Property | Default | Purpose |
|----------|---------|---------|
| `CanonRootPath` | auto-detected | Base directory for all data |
| `ActiveLlmProvider` | `"claude"` | Active LLM provider |
| `Model` | `"claude-sonnet-4-6"` | Claude model ID |
| `OpenAiModel` | `"gpt-4-1-mini"` | OpenAI model ID |
| `ElevenLabsVoiceId` | `"jfIS2w2yJi0grJZPyEsk"` | Default narrator voice (Oliver Silk) |
| `TtsModel` | `"eleven_v3"` | ElevenLabs model |
| `TtsStability` | `0.5` | Voice stability |
| `TtsSimilarityBoost` | `0.75` | Voice similarity |
| `TtsStyle` | `0.0` | Style exaggeration |
| `MaxTokens` | `4096` | Default LLM max tokens |
| `EditorFontSize` | `14` | UI editor font size |
| `Theme` | `"dark"` | UI theme |

ResetToDefaults preserves all API keys.

### Secure Credentials

`FileSecurePreferences` stores sensitive values in `%LOCALAPPDATA%/MindAttic/StreetSamurai/secure.dat`. AES encrypted with a key derived from `SHA256(MachineName:UserName:StreetSamurai)`. Not portable between machines by design.

---

## Data Flow: End to End

### Writing a New Story

```
1. User navigates to /write
2. New StoryProject created in memory
3. User types a premise, opens Write modal, clicks generate
4. StoryStarterService.GenerateOpeningAsync():
   a. DatabaseService provides world context, literary rules, character psychology
   b. WorldGraphService provides relationship context
   c. NarrativeSessionContext enriches with fog-of-war entity data
   d. FacetService selects lead + supporting facets based on premise triggers
   e. LlmRouter -> ClaudeService generates 3-5 paragraphs
   f. Second LLM call generates title
5. HTML content inserted into editor
6. JsonStoryBlockRepository.SaveProject() writes {UUID}.json
7. User edits in rich HTML editor
8. User types continuation direction, generates more content
9. StoryStarterService.ContinueAsync():
   a. All accumulated context: story state, knowledge map, event log, outline
   b. Generates 2-4 more paragraphs
10. User exports: TXT, MD, PDF, MP3, or OGG
```

### Generating a Scene (Multi-Beat)

```
1. User navigates to /generate, fills form
2. SceneGenerationService loops through beats:
   a. ContextAnalyzerService extracts psychological triggers
   b. FacetService selects lead facet (with rotation enforcement)
   c. BeatGeneratorService builds layered prompt and calls LLM
   d. Events fire -> UI renders beat in real-time with color-coded borders
3. Completed scene can be saved as Story or narrated via TTS
```

### Autonomous "Surprise Me" Generation

```
1. User clicks "Surprise Me" on /stories
2. StoryDirectorService.SurpriseMeAsync() takes over:
   a. Picks cast from character repository (weighted toward variety)
   b. AgendaEngine finds goal conflicts -> premise
   c. OutlineService generates 3-act beat sheet with mandatory combat
   d. For each beat: generate prose, extract state, log events, sync knowledge
   e. Dialogue voice constraints ensure characters sound distinct
3. Complete story assembled with section breaks
4. Can be saved as a StoryProject
```

### Entity Save -> Graph Update Flow

```
1. User edits a character on /characters and saves
2. CharacterRepository.Save() writes the JSON file
3. OnItemSaved event fires
4. RelationshipDiscoveryService.DiscoverFromEntity() triggers:
   a. Scans structured properties (affiliation, location) for direct references
   b. Scans text properties (description, story_hooks) for entity name mentions
   c. Creates new WorldEdge entries in the graph
5. SemanticIndexService updates TF-IDF vectors
6. InferenceService cache invalidated
```

---

## Testing

99 unit tests across 10 test classes in `StreetSamurai.UnitTests/`:

| Test Class | Coverage |
|------------|----------|
| `WorldGraphServiceTests` | Graph build, query, BFS traversal, edge evolution |
| `SemanticIndexServiceTests` | TF-IDF indexing, cosine similarity search |
| `InferenceServiceTests` | Shared-hub and shared-property transitive inference |
| `StoryStateServiceTests` | Character state tracking, state extraction |
| `EventLogServiceTests` | Event extraction, querying, persistence |
| `KnowledgeMapServiceTests` | Information asymmetry, POV constraints |
| `OutlineServiceTests` | Outline generation, beat tracking |
| `JsonDirectoryRepositoryTests` | Per-file CRUD, migration, caching |
| `ExportServiceTests` | TXT, MD, HTML export |
| `IntegrationTests` | Cross-service integration scenarios |

`TestGraphService` and `TestHelpers` provide shared test infrastructure.

---

## Key Design Decisions

### Why Rich HTML Instead of Blocks

Single `html` field with contenteditable div because:
- WYSIWYG editing without mode-switching
- Browser-native contenteditable, no heavy editor dependency
- HTML converts cleanly to plain text, markdown, and print
- One field instead of block arrays with sequence management

### Why Per-File JSON Storage

- Human-readable, debuggable, git-friendly
- One file per entity = easy backup, copy, diff
- No database setup required
- Corrupt file isolation -- one bad file does not break the type
- `IStoryBlockRepository` interface means database switch is a single DI change

### Why a Router Instead of Direct LLM Injection

- Runtime provider switching without restart
- Same interface for all consumers
- Easy to add new providers
- Provider-specific quirks isolated in their own service class

### Why Facets Instead of Simple Prompts

- Characters respond differently to the same situation based on dominant facet
- Rotation prevents monotonous voice across long scenes
- The selection algorithm creates emergent narrative variety
- Supporting facets add psychological depth without overwhelming the lead voice

### Why Eager Graph Loading

- Graph queries happen during every generation call
- First-load latency would make the first generation feel broken
- Auto-builds from repository data if snapshot is missing or corrupt

### Why All Data is JSON (No Python, No YAML, No Markdown data files)

- Single format for all data files: JSON only
- No Python scripts in the pipeline
- No YAML configuration
- Markdown only for this README
- Keeps tooling simple: one parser, one serializer, everywhere

### Why Permanent NPCs

Every NPC generated for contracts or encounters is saved as a full character. No disposable throwaways. This turns the world into an accumulating network where a random guard from one story can become a recurring ally in another. Graffiti, not gallery -- accumulation and imperfection make the world feel alive.

---

## Migration Notes

### To Add a Database Backend

1. Implement `IStoryBlockRepository` with your ORM
2. Register it in `ServiceCollectionExtensions` instead of `JsonStoryBlockRepository`
3. Nothing else changes

### To Add a New LLM Provider

1. Implement `ILlmService`
2. Add it to `LlmRouter`'s provider map
3. Add configuration fields to `SettingsService`
4. Add UI controls to Settings page

### To Add a New Entity Type

1. Create a data model in `Models/Canon/`
2. Create a typed repository class extending `JsonDirectoryRepository<T>` in `Repositories.cs`
3. Register as singleton in `ServiceCollectionExtensions`
4. Add to `DatabaseService` for cross-cutting queries
5. Wire `OnItemSaved` to `RelationshipDiscoveryService` for auto-graph integration
6. Create a dictionary page in `StreetSamurai.Shared/Components/Pages/`

---

## System Interconnection Map

```
                    +-------------------+
                    |   UI Pages        |
                    |  (Blazor/MAUI)    |
                    +--------+----------+
                             |
              +--------------+--------------+
              |              |              |
     +--------v---+  +------v-----+  +-----v--------+
     | Write Page  |  | Stories    |  | Dictionary   |
     | /write      |  | /stories   |  | Pages        |
     +------+------+  +-----+-----+  +------+-------+
            |                |               |
            v                v               v
     +------+------+  +-----+-------+  +----+--------+
     |StoryStarter |  |StoryDirector|  | Repositories|
     |Service      |  |Service      |  | (13 types)  |
     +------+------+  +-----+-------+  +----+--------+
            |                |               |
            +-------+--------+        +------v--------+
                    |                 |RelationshipDis |
              +-----v------+         |coveryService   |
              |FacetService |         +------+--------+
              +-----+------+                |
                    |                 +------v--------+
              +-----v-----------+    |WorldGraphSvc  |
              |ContextAnalyzer  |    +------+--------+
              +-----+-----------+           |
                    |              +--------+--------+
              +-----v-----------+  |                 |
              |BeatGenerator    |  |SemanticIndex    |
              +-----+-----------+  |Service          |
                    |              +--------+--------+
              +-----v-----------+           |
              |  LLM Router     |  +--------v--------+
              +--+-----------+--+  |InferenceService |
                 |           |     +-----------------+
           +-----v--+  +----v----+
           |Claude   |  |OpenAI   |
           |Service  |  |Service  |
           +----+----+  +----+----+
                |             |
           Anthropic API  OpenAI API
```

Story intelligence flows:
```
Generated Text
  -> StoryStateService (extracts character state changes)
  -> EventLogService (extracts structured events)
  -> KnowledgeMapService (syncs who knows what)
  -> Next generation call receives all of this as constraints
```

Freelancer systems flow:
```
ContractGenerator -> NpcGenerator (creates needed characters)
                  -> ReputationTracker (adjusts faction standing)
                  -> ConsequenceEngine (records persistent outcomes)
                  -> RandomEncounterService (injects between beats)
```
