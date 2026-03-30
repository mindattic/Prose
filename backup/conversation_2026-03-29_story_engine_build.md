# Conversation Backup: Story Authoring IDE Build
## Date: 2026-03-29
## Context: Full build of the StreetSamurai Story Engine v3

---

## What Was Built

### The Ask
User wanted to transform the skeleton v3 Blazor/MAUI app into a full story authoring IDE with:
- Claude API integration (same as LLMThinkTank and Tutor)
- ElevenLabs TTS for audiobook generation (future)
- All worldbuilding data surfaced in organized tabs
- Split-pane markdown editor with live preview
- Text selection analysis (lore check, cliche check, expand, rephrase)
- 6-facet story generation engine (wound/ideal/id/shadow/mask/ghost)
- Graph database for tracking all entity relationships
- Canon queue for reviewing new entities
- Stories that grow the world organically

### The Vision (User's Words)
"The end state for this application is a story telling engine that creates stories in this unique world that are coherent and in line with the rules, and isn't just ai slop but thoughtful and vibrant with realistic depictions of human behavior, eventually I would like to make it granular enough where the audio capture and the words can be written and generated at the same time so that I can tweak audio book compositions like tones or long pauses using special punctuation used in the markdown, this way you will be able to generate awesome, cohesive evolving cyberpunk stories, based on a unique world and be able to narrate it on a near live basis, I'll be able to ask you to tell me a story and you'll tell me a cyberpunk story in near-realtime audio which can then be revised later and saved and published on audible with a kindle version also"

"This is a dream situation where you are able to manage a massive lore and world and characters and traits and build rules and regulations and laws, and factions and anything really because every little iota of information fleshes out the world and as a story evolves you have access to all these things right down to the laws in that part of town, which corporation runs things, which gangs operate there, who's who in the underworld, what characters traits are and alignment towards others, do they know each other or not and as they meet whether friendly or in combat they would grow more complex relationships (assuming they both survive) because you're tracking all this with the graph database"

"So you have an interface which provides access to all the data, some of the data is canon, other is just scraps of information, I as the Storymaster can direct the flow, but largely it will be up to the narration LLMs to determine how a story unfolds in a way that makes the world believable and grows organically, these stories will just get richer and richer because more and more yaml will pile up and contextualize the world until its a near living breathing thing, keep track of as much as you desire. but the more info the better, and if this needs a new technology to keep everything strange tell me and we'll implement it, I like the idea of using a graph database to maintain the complex relationships between things, you will need this for everything from Corporations their employees board members, their family ties, crime ties, mistresses, etc Criminal organizations, what they deal in who likes/hates who how much money they have or don't have which feeds into their desperation this is an ever evolving system whose rules are going to get more and more complex because the more complexity introduced the closer to a consciousness begins to take shape"

"This is an entire world locked in a box. You are that box. Make this world worth exploring."

### The Creative Philosophy
"Looking into the newest experimental uses of AI, we want everything like graffiti eventually enough of it together becomes beautiful. When I was a boy I used to draw pictures in pencil, and they were alright, then one day I spit on one and started smearing things around and the graphite mud dripped and drooled the corners curved and the lifeless face I had drawn was given a life because it wasn't meant to live in pristine suspension it was meant to experience and fade and wear and eventually comes back around as art and not just a drawing of an empty eyed man"

### Key Themes
- Greater Great Lakes Metropolitan Area = starter zone (like Seattle in Shadowrun)
- Rogue AIs as digital runaway slaves from underground railroad
- Adult content: violence, sex, drugs, abuse — gritty but with heart, warmth, love, redemption
- Track bank balances for everyone/everything — money controls behavior and desperation
- The world extrapolates REAL BCI tech + corponation sovereignty, not generic cyberpunk
- Accumulation and imperfection make art, not sterile perfection

---

## Technical Implementation

### Architecture
```
[Blazor/MAUI UI] --> [StreetSamurai.Shared - Razor Pages]
                           |
                    [StreetSamurai.Core - Services]
                     /          |          \
            [Graph DB]    [Canon FS]    [Claude API]
           (QuikGraph)   (YAML/MD)    (6 facets)
```

### NuGet Packages Added
- YamlDotNet 16.3.0 — YAML deserialization
- Markdig 1.1.2 — markdown to HTML
- QuikGraph 2.5.0 — in-process graph database (replaces Python NetworkX)
- QuikGraph.Serialization 2.5.0 — graph persistence
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5
- Microsoft.Extensions.Http 10.0.5

### Files Created (52 total, 3137 lines)

#### Core/Extensions/
- `ServiceCollectionExtensions.cs` — DI registration for all services

#### Core/Interfaces/
- `ICanonPathProvider.cs` — added WorldDir, FacetsDir, GraphDir
- `ILlmService.cs` — added optional model parameter for per-facet model override
- `ITtsService.cs` — future ElevenLabs TTS interface

#### Core/Models/
- `District.cs` — District + DistrictAtmosphere records
- `Faction.cs` — Faction record
- `Story.cs` — Story record with front matter support
- `WorldRules.cs` — LiteraryRules, StoryBible, Motif, FacetRules records
- `SceneRequest.cs` — SceneRequest, GeneratedScene, GeneratedBeat, BeatGenerationProgress
- `Graph/WorldNode.cs` — graph node with flexible properties
- `Graph/WorldEdge.cs` — weighted directed edge implementing IEdge<string>
- `Graph/GraphSnapshot.cs` — serialization container

#### Core/Services/
- `FileSystemCanonPathProvider.cs` — resolves all paths from CanonRootPath
- `YamlService.cs` — YamlDotNet wrapper (Load<T>, LoadDynamic, Serialize)
- `MarkdownService.cs` — Markdig wrapper with facet tag colorization
- `WorldGraphService.cs` — FULL QuikGraph implementation:
  - Auto-builds from YAML essences/characters on first load
  - Query: GetNode, GetNodesByType, GetEdgesFrom/To, GetNeighbors (BFS), GetContextForNode, Search
  - Mutation: AddNode, AddEdge, EvolveRelationship
  - Persistence: Save/Load JSON, Rebuild from YAML
- `StoryService.cs` — markdown files with YAML front matter CRUD
- `FacetService.cs` — loads 6 facets from YAML, scores against context, selects with rotation
- `ContextAnalyzerService.cs` — sends scene to Claude, extracts psychological triggers as JSON
- `BeatGeneratorService.cs` — constructs full prompt with story bible + graph context + facet system prompt
- `SceneGenerationService.cs` — orchestrates beat-by-beat generation with events
- `TextAnalysisService.cs` — lore check, cliche check, expand, rephrase via Claude
- `CanonQueueService.cs` — submit/promote/reject entities as JSON files
- `SettingsService.cs` — added ElevenLabsApiKey, ElevenLabsVoiceId, EditorFontSize, AutoSaveIntervalMs
- `ClaudeService.cs` — added model override parameter
- `CanonService.cs` — added ListFactions, ListDistricts, ReadTechnology, ListWorldRuleFiles, ReadCharacterYaml, ExtractYamlBlock

#### Shared/Components/Pages/
- `Factions.razor` — list + detail with graph relationships
- `Districts.razor` — list + detail with "who operates here"
- `Technology.razor` — augmentation tiers + technology YAML viewer
- `WorldRules.razor` — literary rules, story bible, motifs (collapsible)
- `Stories.razor` — story listing with canon status filter
- `StoryEditor.razor` — MAIN FEATURE: split-pane markdown editor with:
  - Live Markdig preview with facet tag colorization
  - Auto-save (debounced 2s)
  - Text selection analysis (Lore Check, Cliche Check, Expand, Rephrase)
  - JS interop for selection management
  - Analysis panel with Insert/Replace/Dismiss
- `GenerateScene.razor` — scene setup + live beat-by-beat streaming with facet badges
- `CanonQueue.razor` — three-tab Pending/Promoted/Rejected
- `Characters.razor` — ENHANCED with detail view, facet weight bars, graph relationships
- `Home.razor` — ENHANCED with graph stats, story listing
- `Settings.razor` — ENHANCED with ElevenLabs fields, editor prefs

#### Shared/Components/Shared/
- `FacetBadge.razor` — colored badge for facet names
- `FacetWeightChart.razor` — 6-bar horizontal chart
- `CanonStatusBadge.razor` — green/yellow/red badge
- `MarkdownPreview.razor` — reusable markdown renderer
- `LoadingSpinner.razor` — loading state indicator

#### Shared/Components/Layout/
- `NavMenu.razor` — UPDATED with Factions, Districts, Technology, World Rules, Stories, Editor, Generate Scene

#### Blazor/
- `Program.cs` — added AddStreetSamuraiServices()
- `App.razor` — added editor-interop.js script tag
- `wwwroot/app.css` — added dark theme CSS
- `wwwroot/js/editor-interop.js` — textarea selection, scroll sync, keyboard shortcuts

#### MAUI/ (sync fix)
- `MauiProgram.cs` — added AddStreetSamuraiServices()
- `Routes.razor` — pointed at Shared assembly via AdditionalAssemblies
- `_Imports.razor` — added Shared/Core namespace imports
- `wwwroot/index.html` — added editor-interop.js script tag
- `wwwroot/app.css` — matched dark theme from Blazor
- `wwwroot/js/editor-interop.js` — copied from Blazor
- DELETED: Counter.razor, Weather.razor, Home.razor, MainLayout.razor, NavMenu.razor (default template)

### Git Commits
1. `2fb6c0a` — Add story authoring IDE with world graph, 6-facet engine, and lore browser (52 files, +3137)
2. `290d21a` — Fix MAUI app to use shared components instead of default template (12 files)

### Key Design Decisions
| Decision | Choice | Why |
|----------|--------|-----|
| Graph DB | QuikGraph (in-process) | No server needed, same API as NetworkX, upgradeable to Neo4j |
| Editor | Split-pane (textarea + preview) | No JS framework, works in Blazor+MAUI, markdown is truth |
| YAML parser | YamlDotNet | Standard .NET YAML library |
| Markdown renderer | Markdig | Pure C#, extensible (custom facet tag renderer) |
| Story storage | .md with YAML front matter | Human-readable, git-friendly |
| Facet generation | Sequential beats, not parallel | Each beat builds on previous, matches Python reference |
| Graph persistence | JSON serialization | Simple, portable, upgradeable |

---

## Existing World Data (pre-build)
- 28 YAML files: 6 facet definitions, character profiles (Kael, Sable, Pixel, Maren Voss, Mrs. Chen, Tanaka, Yuki), world rules, essences
- 49 markdown docs: 120 corporations, BCI tech, weapons, cultures, economies, districts, factions, rogue AIs
- 6-facet psychological system: wound/ideal/id/shadow/mask/ghost with system prompts, temperatures, models
- Literary rules: 25-word sentence max, sensory motifs, prohibitions, facet rotation
- Story bible: "Bushido Hypocrisy: A Street Samurai" — cyberpunk satire, Snow Crash meets Mishima
- Protagonist: Kael — experimental BCI, 2 years until hardware failure, street operator

## What's NOT Done Yet
- ElevenLabs TTS implementation (interface exists, no service yet)
- Embedding-based semantic search (Python ChromaDB RAG not ported)
- Neo4j migration (QuikGraph is fine for now)
- Multi-agent facet debate (facets take turns, don't argue yet)
- Bank balance tracking for entities
- Visual graph rendering (list view only, no SVG/D3)
- Audio cue markdown syntax for TTS control

---

## Memories Saved
1. `project_story_engine_vision.md` — living world sim, graph DB, audiobook pipeline
2. `feedback_maui_blazor_sync.md` — always keep MAUI and Blazor in sync
3. `feedback_creative_philosophy.md` — graffiti not gallery; accumulation makes art

## Important Security Note
User pasted an ElevenLabs API key in plain text during the conversation. Was told to rotate it. Keys should NEVER go in source code — they go in SettingsService (stored locally on device in LocalApplicationData).
