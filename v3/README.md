# Street Samurai v3 — Technical Reference

## What This Is

A story authoring engine for cyberpunk fiction set in Meridian City. The system generates narrative prose through a psychology-driven "facet" system — six psychological voices per character that compete for dominance based on context. Built on .NET 10 with Blazor Server (web) and MAUI (desktop/mobile) front-ends sharing a common component library.

This is not a chatbot. It is a structured writing tool with a rich HTML editor. The AI generates and refines text. You control what stays and what gets rewritten.

---

## Solution Structure

```
v3/
  StreetSamurai.Core/          Business logic, services, models. No UI.
  StreetSamurai.Shared/        Razor components shared by both hosts.
  StreetSamurai.Blazor/        Blazor Server web host (.NET 10).
  StreetSamurai.MAUI/          .NET MAUI desktop/mobile host.
```

**Dependencies (Core only):**
- Markdig 1.1.2 — Markdown-to-HTML rendering
- QuikGraph 2.5.0 — In-memory relationship graph engine
- System.Speech 10.0.5 — Windows SAPI TTS fallback
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5
- Microsoft.Extensions.Http 10.0.5 — HttpClient factory

Both hosts call `services.AddStreetSamuraiServices()` and get the same singleton service graph.

---

## Startup and Service Registration

`ServiceCollectionExtensions.AddStreetSamuraiServices()` registers everything. Key services:

| Service | Lifetime | Notes |
|---------|----------|-------|
| `SettingsService` | Singleton | Auto-detects canon root path |
| `FileSecurePreferences` -> `ISecurePreferences` | Singleton | AES-encrypted credential store |
| `FileSystemPathProvider` -> `IPathProvider` | Singleton | Resolves all directory paths |
| `DatabaseService` | Singleton | Aggregates typed repositories |
| `LoreService` | Singleton | High-level lore API |
| `MarkdownService` | Singleton | Markdig pipeline with facet tag coloring |
| `StoryService` | Singleton | Legacy story persistence |
| `JsonStoryBlockRepository` -> `IStoryBlockRepository` | Singleton | JSON story project persistence |
| `FacetService` | Singleton | Facet definitions, scoring, selection |
| `WorldGraphService` | Singleton | QuikGraph relationship network, eagerly loaded |
| `NarrativeSessionContext` | Scoped | Fog-of-war entity loading during generation |
| `EntityExtractionService` | Singleton | Extracts entities from prose into the graph |
| `ClaudeService` | HttpClient | Anthropic API client |
| `OpenAiService` | HttpClient | OpenAI API client |
| `MultiLlmService` | Singleton | Multi-provider voting |
| `LlmRouter` -> `ILlmService` | Singleton | Routes to active provider |
| `ElevenLabsTtsService` -> `ITtsService` | HttpClient | ElevenLabs TTS |
| `WindowsTtsService` | Singleton | Free Windows SAPI fallback |
| `TtsEnhancementService` | Singleton | ElevenLabs audio tag injection |
| `AudioFileService` -> `IAudioFileService` | Singleton | Audio file save + explorer reveal |
| `ExportService` | Singleton | TXT, MD, PDF (print), MP3, OGG export |
| `TextAnalysisService` | Singleton | Lore check, cliche check, expand, rephrase |
| `ContextAnalyzerService` | Singleton | Extracts psychological triggers from scene context |
| `BeatGeneratorService` | Singleton | Generates individual narrative beats |
| `SceneGenerationService` | Singleton | Orchestrates multi-beat scene generation |
| `StoryStarterService` | Singleton | Story openings, continuations, polish |
| `ValidationService` | Singleton | Input validation |

Typed repositories registered by `DatabaseService`: CharacterRepository, CorponationRepository, DistrictRepository, FactionRepository, FacetRepository, WorldbuildingDocRepository, WeaponryRepository, EquipmentRepository, TechnologyRepository, MotifRepository, StoryBibleRepository, LiteraryRulesRepository, CharacterProfileRepository.

Blazor eagerly calls `DatabaseService.EnsureLoaded()` and `WorldGraphService.EnsureLoaded()` at startup so the first page load does not block on I/O.

---

## Configuration

### Settings

`SettingsService` persists to `%LOCALAPPDATA%/MindAttic/StreetSamurai/Settings.json`. Every property setter auto-saves the file.

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

ResetToDefaults preserves all API keys: Anthropic, OpenAI, ElevenLabs, Gemini, DeepSeek, Mistral, Grok, Groq, Together, OpenRouter, Fireworks, Cohere.

### Secure Credentials

`FileSecurePreferences` stores sensitive values in `%LOCALAPPDATA%/MindAttic/StreetSamurai/secure.dat`. AES encrypted with a key derived from `SHA256(MachineName:UserName:StreetSamurai)`. Not portable between machines by design.

### Directory Layout

All paths resolved by `FileSystemPathProvider` relative to `CanonRootPath`:

```
{CanonRoot}/
  engine_data/
    characters.json             59 characters with full psychology
    districts.json              20+ districts with 77 incoming
    factions.json               29 factions
    corponations.json           50 corporate nation-states
    facets.json                 6 facet definitions
    equipment.json              180 equipment entries
    weaponry.json               185 weapons
    technology.json             50 technology entries
    worldbuilding_docs.json     Long-form lore documents
    story_bible.json            Tone, theme, genre, protagonist, core hook
    literary_rules.json         Hard constraints on prose style
    character_profile.json      Kyle's core identity and contradictions
    motifs.json                 Recurring thematic elements
    tts_rules.json              TTS pronunciation and style rules
    graph/
      world_graph.json          Relationship network snapshot
  story_blocks/
    {UUID}.json                 One file per story project (HTML source of truth)
  audio/
    narration_*.mp3             Generated audio files
```

---

## Database and Repositories

### Typed Repositories

The system loads individual typed JSON files from `engine_data/` via typed repositories. There is no single monolithic data file. Each repository handles its own file.

`DatabaseService` (renamed from `CanonDatabaseService`) aggregates all typed repositories and provides cross-cutting query methods:

- `FindCharacter(nameOrAlias)` — Case-insensitive lookup with alias support
- `GetBlendedWeights(characterNames)` — Averages facet weights across multiple characters for ensemble casts
- `GetCharacterContext(name)` — Builds a rich LLM prompt block: psychology, fears, desires, speech patterns, relationships, story hooks
- `GetDistrictContext(name)` — Location atmosphere, sensory details, dangers, opportunities
- `GetLiteraryRulesPrompt()` — Formatted rules for injection into system prompts
- `Search(query, maxResults)` — Full-text search across all worldbuilding documents

### LoreService

Higher-level API (renamed from `CanonService`) that maps typed database models to runtime models (`Character`, `District`, `Faction`, `Corponation`). Provides JSON serialization for UI display and document browsing.

---

## The World Graph

`WorldGraphService` maintains an in-memory relationship graph using QuikGraph's `AdjacencyGraph<string, WorldEdge>`.

### Nodes

Every entity (character, district, faction, corponation) becomes a `WorldNode`:
- `Id` — Slugified: lowercase, spaces to underscores, non-alphanumeric stripped
- `Name` — Display name
- `NodeType` — `"character"` | `"district"` | `"faction"` | `"corporation"`
- `Properties` — Key/value metadata bag (description, role, aliases, etc.)
- `CanonStatus` — `"canon"` | `"experimental"` | `"rejected"`

### Edges

`WorldEdge` connects two nodes:
- `RelationType` — `"affiliated_with"`, `"member_of"`, `"adjacent_to"`, `"friend"`, `"rival"`, `"enemy"`, etc.
- `Weight` — 0-10 intensity scale
- `Sentiment` — `"positive"` | `"negative"` | `"neutral"` | `"mixed"` (heuristic-inferred)
- `Description` — Narrative explanation of the relationship

### Build and Query

- **Auto-build**: On first load, if `world_graph.json` does not exist, the graph is built from the typed repositories by parsing all characters, districts, factions, and their relationships, then inferring cross-entity links (e.g., character to corponation affiliation from text).
- `GetContextForNode(id)` — Returns formatted text suitable for LLM prompts: node properties + all connected edges with descriptions.
- `GetNeighbors(id, depth)` — BFS traversal to find entities within N hops.
- `EvolveRelationship(sourceId, targetId, ...)` — Updates edge weight (used during scene generation to track shifting dynamics).

### Persistence

Saves/loads as `GraphSnapshot` (nodes[] + edges[] + lastSaved timestamp) to `engine_data/graph/world_graph.json`. Can be rebuilt from data at any time via `Rebuild()`.

---

## The Six-Facet System

The core narrative engine. Every character has six psychological facets, each a competing voice that can drive or color the prose.

### The Facets

| Facet | Domain | What It Drives |
|-------|--------|----------------|
| **Wound** | Trauma, emotional pain | Vulnerability, flashbacks, self-destruction |
| **Ideal** | Aspirational self | Hope, sacrifice, moral clarity |
| **Id** | Raw desire, survival instinct | Hunger, rage, lust, self-preservation |
| **Shadow** | Denied aspects | Hypocrisy, projection, hidden cruelty |
| **Mask** | Social facade | Performance, manipulation, charm |
| **Ghost** | Haunted past | Memory, regret, the weight of history |

### Facet Definition (from facets.json)

Each facet has:
- **Triggers** — Context keywords that activate it (e.g., Wound triggers on `"violence"`, `"betrayal"`, `"loss"`)
- **SystemPrompt** — The LLM personality directive when this facet leads
- **VoiceTone** — Prose style description (e.g., "raw, trembling, stripped bare")
- **VoiceStyle** — Narrative technique
- **Prohibitions** — What this voice must never do
- **CoreMemories** — Recurring memories the facet surfaces
- **Model** — LLM model override (default: `claude-sonnet-4-6`)
- **Temperature** — Generation temperature (default: 0.8)

### Facet Weights

Each character has a `FacetState` — six float values (0-1) representing how strongly each facet manifests:

```
{ Wound: 0.7, Ideal: 0.4, Id: 0.5, Shadow: 0.6, Mask: 0.3, Ghost: 0.8 }
```

For ensemble casts (multiple characters in a scene), weights are averaged via `GetBlendedWeights()`.

### Facet Selection Algorithm

`FacetService.SelectFacets(weights, contextTags, recentLeads)`:

1. For each of the 6 facets, compute a score:
   - Count how many of the facet's `Triggers` overlap with the current `contextTags`
   - Multiply by the character's weight for that facet
2. Sort by score descending.
3. **Rotation enforcement**: If the top-scoring facet has been the lead for 3+ consecutive beats, demote it and pick the next.
4. Return: `(leadFacet, [supporting1, supporting2])` — one lead voice, two supporting.

The **lead facet** controls:
- The system prompt tone and personality
- The LLM model and temperature
- The dominant voice of the prose

The **supporting facets** may surface as:
- Brief interior interjections tagged with `[FACET_NAME]`
- Tonal undercurrents that color the lead voice

### How Facets Get Activated

Context tags come from two sources:

1. **Scene generation** (`ContextAnalyzerService`): Sends the scene-so-far + character relationships to the LLM at temperature 0.3. The LLM returns structured JSON:
   ```
   { psychological_triggers: ["betrayal", "identity_crisis"],
     dominant_emotion: "paranoia", stakes: "survival", tension_source: "..." }
   ```

2. **Story starters** (`StoryStarterService.InferTriggers`): Keyword matching on the premise text. Maps words like "betray" to `betrayal`, "augment" to `transhumanism`, "child" to `children_in_danger`, etc. Falls back to `["unknown_danger", "moral_choice"]` if nothing matches.

---

## LLM System

### Architecture

```
ILlmService (interface)
  ├── ClaudeService      Anthropic Claude API
  ├── OpenAiService      OpenAI Chat Completions API
  ├── MultiLlmService    Multi-provider voting
  └── LlmRouter          Runtime multiplexer based on ActiveLlmProvider setting
```

All consumers inject `ILlmService`, which resolves to `LlmRouter`. The router reads `SettingsService.ActiveLlmProvider` on every call and delegates to the matching provider.

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

### ClaudeService

- Endpoint: `https://api.anthropic.com/v1/messages`
- Headers: `x-api-key`, `anthropic-version: 2023-06-01`
- Timeout: 3 minutes
- Default model: from `SettingsService.Model` (default `claude-sonnet-4-6`)
- JSON serialization: snake_case

### OpenAiService

- Endpoint: `https://api.openai.com/v1/chat/completions`
- Headers: `Authorization: Bearer {key}`
- Timeout: 3 minutes
- Default model: from `SettingsService.OpenAiModel` (default `gpt-4-1-mini`)

### MultiLlmService

Multi-provider voting system. Sends the same prompt to multiple providers and aggregates results for quality comparison.

### Where LLM Calls Happen

| Caller | Temperature | Max Tokens | Purpose |
|--------|------------|------------|---------|
| `BeatGeneratorService` | Facet's temp (0.8 default) | 2048 | Generate narrative beats |
| `StoryStarterService.GenerateOpeningAsync` | Facet's temp | 2048 | Story opening |
| `StoryStarterService.GenerateOpeningAsync` (title) | 0.9 | 50 | Title generation |
| `StoryStarterService.ContinueAsync` | Facet's temp | 2048 | Story continuation |
| `StoryStarterService.PolishAsync` | 0.4 | 4096 | Prose refinement |
| `ContextAnalyzerService.AnalyzeAsync` | 0.3 | -- | Psychological trigger extraction |
| `TextAnalysisService.LoreCheckAsync` | 0.3 | -- | Canon consistency check |
| `TextAnalysisService.ClicheCheckAsync` | 0.3 | -- | Cliche detection |
| `TextAnalysisService.ExpandAsync` | 0.85 | -- | Text expansion |
| `TextAnalysisService.RephraseAsync` | 0.7 | -- | Text rephrasing |

---

## Story Generation Pipelines

There are two generation pipelines: **Scene Generation** (multi-beat, event-driven) and **Story Starter** (single-shot).

### Pipeline 1: Scene Generation

`SceneGenerationService.GenerateSceneAsync(SceneRequest, FacetState)`

Used by the `/generate` page. Produces a multi-beat scene with facet rotation.

**Input -- SceneRequest:**
- `Goal` — What should happen in this scene
- `Location` — District name
- `Characters[]` — Character names
- `Themes[]` — Optional per-beat themes
- `NumBeats` — 3-8 (default 5)
- `ForcedLeadFacet` — Optional override

**Flow:**
```
For each beat (0 to numBeats-1):
  1. ContextAnalyzerService.AnalyzeAsync(sceneSoFar, characters)
     -> Returns psychological triggers, dominant emotion, stakes

  2. FacetService.SelectFacets(weights, triggers, recentLeads)
     -> Returns (lead, [support1, support2])
     -> Rotation prevents same lead 3+ times in a row

  3. Fire OnBeatProgress event (UI updates progress bar)

  4. BeatGeneratorService.GenerateBeatAsync(context, lead, supporting)
     -> Builds system prompt: lead voice + story bible + literary rules
       + supporting voices + core memories + character context + location
     -> Builds user prompt: scene so far + beat goal
     -> LLM generates 2-4 paragraphs

  5. Accumulate beat text into sceneSoFar
  6. Fire OnBeatCompleted event (UI renders new beat)
```

**Output -- GeneratedScene:**
- `Beats[]` — Each with index, goal, leadFacet, supportingFacets, text, contextTags
- `FullText` — All beats concatenated

**Events:**
- `OnBeatProgress(BeatGenerationProgress)` — beat index, total, lead facet, status message
- `OnBeatCompleted(GeneratedBeat)` — full beat for live rendering

### Pipeline 2: Story Starter

`StoryStarterService` — used by the `/write` page. Simpler, single-shot generation that feeds into the HTML editor.

**GenerateOpeningAsync(StoryStarterRequest):**
1. Load world context: story bible, literary rules, location atmosphere, character psychology
2. Blend facet weights across all named characters
3. Infer trigger tags from premise keywords
4. Select lead + supporting facets
5. Build system prompt with lead voice, supporting voices, world context, literary rules
6. User prompt: premise + mood + "Drop us in the middle of something, 3-5 paragraphs"
7. LLM generates opening prose
8. Second LLM call: generate a title (temperature 0.9, max 50 tokens, "like graffiti on a wall")
9. Return `GeneratedOpening` (title, text, lead facet, supporting facets, characters, location)

**GenerateRandomAsync():**
- Picks 1-3 random characters from the database
- Picks a random district
- Picks from hardcoded seed premises (drawn from actual world tensions)
- Picks a random mood
- Calls `GenerateOpeningAsync` with the random inputs

**ContinueAsync(existingText, prompt, mood, location, characters):**
- Same world context construction as opening
- Uses NarrativeSessionContext for fog-of-war entity enrichment
- Sends existing story as "STORY SO FAR"
- User prompt includes the continuation direction
- Returns raw text (2-4 paragraphs)

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

This is the migration seam. To move to a database, implement this interface with EF Core or Dapper and register it in DI instead of `JsonStoryBlockRepository`. Nothing else changes.

---

## Text-to-Speech

### ElevenLabsTtsService

- API: `POST https://api.elevenlabs.io/v1/text-to-speech/{voiceId}`
- Header: `xi-api-key`
- Returns: audio byte array (MP3 or OGG based on output_format parameter)
- Timeout: 2 minutes
- Voice settings sent per request: stability, similarity_boost, style, use_speaker_boost
- Default voice: Oliver Silk (`jfIS2w2yJi0grJZPyEsk`)
- Default model: `eleven_v3`

`SynthesizeAsync(text, voiceId?, ct)` — Sends the full story text as a single synthesis request.

`ListVoicesAsync()` — Fetches available voices for the Settings UI.

### TtsEnhancementService

Adds ElevenLabs audio tags to text before synthesis. Handles pronunciation rules, pauses, emphasis, and other markup from `tts_rules.json`.

### WindowsTtsService

Free Windows SAPI fallback TTS. Uses `System.Speech` for local synthesis when ElevenLabs is not configured or for quick previews.

### AudioFileService

- Saves audio bytes to `{CanonRoot}/audio/narration_{timestamp}.mp3`
- `RevealInExplorer(path)` — Opens file manager with the file selected

### AudioPlayer Component

Shared Razor component used on `/write` and `/generate`:
- States: Loading, Error, Ready/Playing
- Uses HTML5 `<audio>` element with base64 data URLs
- Auto-plays on load
- Download button saves via `IAudioFileService`
- Open in Explorer button reveals saved file

---

## Text Analysis Tools

`TextAnalysisService` provides editor-integrated analysis:

| Method | Temperature | What It Does |
|--------|------------|--------------|
| `LoreCheckAsync(text, context)` | 0.3 | Searches database + graph for entities mentioned in text, sends to LLM to check for contradictions |
| `ClicheCheckAsync(text)` | 0.3 | Checks against literary rule prohibitions (no noir cliches, no "chrome gleaming", no katana fetishism), suggests concrete fixes |
| `ExpandAsync(text, context)` | 0.85 | Continues prose from selection, maintaining voice |
| `RephraseAsync(text)` | 0.7 | Rewrites selected text with different word choices |

---

## Export

`ExportService` provides multiple output formats:

| Method | Output |
|--------|--------|
| `ToPlainText()` | Plain text with HTML tags stripped |
| `ToMarkdown()` | Markdown conversion from HTML |
| `ToPrintHtml()` | Formatted HTML for browser print-to-PDF |

The Write page offers an export dropdown with five options:
- **TXT** — Plain text export
- **MD** — Markdown export
- **PDF** — Opens browser print dialog for print-to-PDF
- **MP3** — ElevenLabs audio synthesis (`mp3_44100_128` format)
- **OGG** — ElevenLabs audio synthesis (`ogg_vorbis` format)

Audio exports use `ElevenLabsTtsService` with the appropriate `output_format` parameter.

---

## UI Pages

### / — Home

Dashboard with stats: entity counts, graph size, story count, configured services.

### /write, /write/{ProjectId} — Write Story

The primary authoring workspace. Rich HTML editor with contenteditable div.

**Layout:**
1. **Title bar** — Editable title, character list, story status
2. **Formatting toolbar** — H1, H2, H3, P, Bold, Italic, and other formatting controls
3. **Rich HTML editor** — Contenteditable div, the source of truth for story content
4. **Export dropdown** — TXT, MD, PDF, MP3, OGG
5. **Ask Modal** — Query world graph + story context with LLM
6. **Write Modal** — Generate opening or continuation with full graph context enrichment
7. **ElevenLabs tag bar** — TTS markup insertion
8. **Audio player** — Appears after narration
9. **Scene sidebar** — Entity detection, validation, and mini-graph visualization
10. **Context menu** — Read selected, read all, rewrite, expand

**Key behaviors:**
- Auto-detect characters from story text via `NarrativeSessionContext.ScanText()`
- Auto-save on timer
- Routes: `/write` (new project) or `/write/{ProjectId}` (load existing)

### /generate — Generate Scene

Multi-beat scene generation with live facet rotation visualization.

**Setup:** Scene goal, location, characters, beat count (3-8), optional per-beat themes.

**During generation:** Beat counter, cancel button, facet rotation bar, live beat rendering with color-coded borders per lead facet.

**After:** Save as Story, Narrate, New Scene.

Facet colors: Wound=#dc3545, Ideal=#198754, Id=#ffc107, Shadow=#6f42c1, Mask=#0dcaf0, Ghost=#6c757d.

### /stories — Stories

Browse and manage all saved story projects.

### /characters — Character Dictionary

Lists all characters from the database with full profiles: psychology, speech patterns, facet weights (bar chart), relationships, story hooks.

### /districts — District Dictionary

District data with atmosphere, sensory details, dangers, opportunities.

### /factions — Faction Dictionary

Organization profiles and affiliations.

### /corporations — Corponation Dictionary

Corporate nation-state profiles.

### /technology — Technology Dictionary

Technology entries with descriptions and world context.

### /weaponry — Weaponry Dictionary

Weapon entries and specifications.

### /equipment — Equipment Dictionary

Equipment entries and specifications.

### /rules — World Rules Dictionary

Literary rules and world constraints.

### /docs — Document Dictionary

Long-form worldbuilding documents.

### /graph — World Graph

Visualization of the relationship network.

### /search — Search

Full-text search across all worldbuilding documents and graph nodes.

### /settings — Settings

API keys (Anthropic, OpenAI, ElevenLabs, Gemini, DeepSeek, Mistral, Grok, Groq, Together, OpenRouter, Fireworks, Cohere), model selection, provider toggle, TTS settings, canon root path, UI preferences. Shows configured/unconfigured status per provider.

### Shared Components

- **AudioPlayer** — HTML5 audio playback with download and reveal
- **CanonStatusBadge** — Canon/experimental/rejected status indicator
- **FacetBadge** — Colored facet name badge
- **FacetWeightChart** — Bar chart of facet weights
- **LoadingSpinner** — Loading indicator
- **MarkdownPreview** — Rendered markdown display
- **Placeholder** — Empty state placeholder
- **TechEdges** — Decorative cyberpunk edge styling

---

## Prompt Architecture

Every LLM call builds a system prompt from layered context. Here is the typical structure for a generation call:

```
SYSTEM PROMPT:
  1. Role statement ("You are a literary fiction author writing cyberpunk...")
  2. Lead facet voice (system prompt + voice tone from facet definition)
  3. Supporting facet voices (may interject as [FACET_NAME] tagged lines)
  4. Story Bible (title, genre, tone, core theme, core hook, arc, protagonist)
  5. Literary Rules (hard constraints -- NON-NEGOTIABLE)
  6. Location context (atmosphere, sensory details, dangers, opportunities)
  7. Character context (psychology, fears, desires, speech patterns, relationships)
  8. World flavor (random corponation details, protagonist contradiction)

USER PROMPT:
  1. Scene so far (for continuations)
  2. Mood/tone directive (if specified)
  3. Premise or direction
  4. Structural instruction ("3-5 paragraphs", "end on tension", etc.)
  5. Format constraint ("Write ONLY the story text. No titles, no headers.")
```

The literary rules are injected as "NON-NEGOTIABLE" in every generation call. They enforce sentence length limits, ban cliches, require specific prose qualities, etc.

### NarrativeSessionContext

Session-scoped fog-of-war context. As entities are mentioned in narrative, their 2-hop graph neighborhoods load. `BuildContext()` produces a layered prompt: primary entities get full briefs, secondary get compact one-liners. Used in ContinueAsync, DoWrite, ExecuteAsk, and SceneGenerationService.

### EntityExtractionService

Extracts entities from generated prose and maps them into the world graph. Keeps the graph up to date as new narrative introduces or references entities.

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
   e. ClaudeService (via LlmRouter) generates 3-5 paragraphs
   f. Second LLM call generates title
5. HTML content inserted into editor
6. JsonStoryBlockRepository.SaveProject() writes {UUID}.json to story_blocks/
7. User edits in rich HTML editor
8. User types continuation direction, generates more content
9. StoryStarterService.ContinueAsync() generates 2-4 more paragraphs
10. User exports via dropdown: TXT, MD, PDF, MP3, or OGG
```

### Generating a Scene

```
1. User navigates to /generate, fills form
2. SceneGenerationService loops through beats:
   a. ContextAnalyzerService extracts psychological triggers from scene context
   b. FacetService selects lead facet (with rotation)
   c. BeatGeneratorService builds prompt and calls LLM
   d. Events fire -> UI renders beat in real-time
3. Completed scene can be saved as a Story or narrated
```

---

## Key Design Decisions

### Why Rich HTML Instead of Blocks

The editor uses a single `html` field with a contenteditable div because:
- **WYSIWYG editing** — Direct formatting without mode-switching
- **Standard tooling** — contenteditable is browser-native, no heavy editor dependency
- **Export flexibility** — HTML converts cleanly to plain text, markdown, and print
- **Simpler model** — One field instead of an array of blocks with sequence management

### Why JSON Files (For Now)

- Simple, human-readable, debuggable
- One file per story = easy backup/copy
- No database setup required
- `IStoryBlockRepository` interface means the switch to a database is a single DI registration change

### Why Typed Repositories Instead of a Single Data File

- Each data type loads independently
- Changes to one type do not require parsing the entire dataset
- Individual files are easier to edit, diff, and version control
- Repositories provide typed access without casting or key lookups

### Why a Router Instead of Direct LLM Injection

- Runtime provider switching without restart
- Same interface for all consumers
- Easy to add new providers (local models, etc.)
- Provider-specific quirks isolated in their own service class

### Why Facets Instead of Simple Prompts

- Characters respond differently to the same situation based on which facet dominates
- Facet rotation prevents monotonous voice across long scenes
- The selection algorithm creates emergent narrative variety — the system "surprises itself"
- Supporting facets add psychological depth without overwhelming the lead voice

### Why Eager Graph Loading

`WorldGraphService.EnsureLoaded()` is called at startup because:
- Graph queries happen during every generation call
- First-load latency would make the first generation feel broken
- The graph auto-builds from repository data if the snapshot is missing or corrupt

---

## Migration Notes

### To Add a Database Backend

1. Implement `IStoryBlockRepository` with your ORM (EF Core, Dapper, etc.)
2. Register it in `ServiceCollectionExtensions` instead of `JsonStoryBlockRepository`
3. Nothing else changes — the UI and all services consume the interface

### To Add a New LLM Provider

1. Implement `ILlmService`
2. Add it to `LlmRouter`'s provider map
3. Add configuration fields to `SettingsService`
4. Add UI controls to the Settings page
