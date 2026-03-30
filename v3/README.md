# Street Samurai v3 — Technical Reference

## What This Is

A story authoring engine for cyberpunk fiction set in Meridian City. The system generates narrative prose through a psychology-driven "facet" system — six psychological voices per character that compete for dominance based on context. Built on .NET 10 with Blazor Server (web) and MAUI (desktop/mobile) front-ends sharing a common component library.

This is not a chatbot. It is a structured writing tool where every paragraph is a discrete, lockable, sequenced block. The AI generates and refines text. You control what stays and what gets rewritten.

---

## Solution Structure

```
v3/
  StreetSamurai.Core/          Business logic, services, models. No UI.
  StreetSamurai.Shared/        Razor components shared by both hosts.
  StreetSamurai.Blazor/        Blazor Server web host.
  StreetSamurai.MAUI/          .NET MAUI desktop/mobile host.
```

**Dependencies (Core only):**
- Markdig 1.1.2 — Markdown-to-HTML rendering
- QuikGraph 2.5.0 — In-memory relationship graph engine
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.5
- Microsoft.Extensions.Http 10.0.5 — HttpClient factory

Both hosts call `services.AddStreetSamuraiServices()` and get the same singleton service graph.

---

## Startup & Service Registration

`ServiceCollectionExtensions.AddStreetSamuraiServices()` registers everything in this order:

| Order | Service | Lifetime | Notes |
|-------|---------|----------|-------|
| 1 | `SettingsService` | Singleton | Auto-detects canon root path |
| 2 | `FileSecurePreferences` → `ISecurePreferences` | Singleton | AES-encrypted credential store |
| 3 | `FileSystemCanonPathProvider` → `ICanonPathProvider` | Singleton | Resolves all directory paths |
| 4 | `CanonDatabaseService` | Singleton | Lazy-loads `engine_data/canon.json` |
| 5 | `CanonService` | Singleton | High-level canon API |
| 6 | `MarkdownService` | Singleton | Markdig pipeline with facet tag coloring |
| 7 | `StoryService` | Singleton | Markdown story file persistence |
| 8 | `JsonStoryBlockRepository` → `IStoryBlockRepository` | Singleton | JSON block-based story persistence |
| 9 | `FacetService` | Singleton | Facet definitions, scoring, selection |
| 10 | `CanonQueueService` | Singleton | Canon review pipeline |
| 11 | `WorldGraphService` | Singleton | QuikGraph relationship network, eagerly loaded |
| 12 | `ClaudeService` | HttpClient | Anthropic API client |
| 13 | `OpenAiService` | HttpClient | OpenAI API client |
| 14 | `LlmRouter` → `ILlmService` | Singleton | Routes to active provider |
| 15 | `ElevenLabsTtsService` → `ITtsService` | HttpClient | ElevenLabs TTS |
| 16 | `AudioFileService` → `IAudioFileService` | Singleton | Audio file save + explorer reveal |
| 17 | `TextAnalysisService` | Singleton | Lore check, cliche check, expand, rephrase |
| 18 | `ContextAnalyzerService` | Singleton | Extracts psychological triggers from scene context |
| 19 | `BeatGeneratorService` | Singleton | Generates individual narrative beats |
| 20 | `SceneGenerationService` | Singleton | Orchestrates multi-beat scene generation |
| 21 | `StoryStarterService` | Singleton | Story openings, continuations, polish |

Blazor also eagerly calls `CanonDatabaseService.EnsureLoaded()` and `WorldGraphService.EnsureLoaded()` at startup so the first page load doesn't block on I/O.

---

## Configuration

### Settings

`SettingsService` persists to `%LOCALAPPDATA%/MindAttic/StreetSamurai/Settings.json`. Every property setter auto-saves the file.

| Property | Default | Purpose |
|----------|---------|---------|
| `CanonRootPath` | auto-detected | Base directory for all data |
| `ActiveLlmProvider` | `"claude"` | `"claude"` or `"openai"` |
| `ApiKey` | — | Anthropic API key |
| `Model` | `"claude-sonnet-4-6"` | Claude model ID |
| `OpenAiApiKey` | — | OpenAI API key |
| `OpenAiModel` | `"gpt-4-1-mini"` | OpenAI model ID |
| `ElevenLabsApiKey` | — | ElevenLabs API key |
| `ElevenLabsVoiceId` | `"L0Dsvb3SLTyegXwtm47J"` | Default narrator voice (Oliver Silk) |
| `TtsModel` | `"eleven_multilingual_v2"` | ElevenLabs model |
| `TtsStability` | `0.5` | Voice stability |
| `TtsSimilarityBoost` | `0.75` | Voice similarity |
| `TtsStyle` | `0.0` | Style exaggeration |
| `MaxTokens` | `4096` | Default LLM max tokens |
| `EditorFontSize` | `14` | UI editor font size |
| `Theme` | `"dark"` | UI theme |

**Canon root auto-detection** walks up to 8 directory levels looking for `engine_data/canon.json` or `worldbuilding/` + `essences/` together.

### Secure Credentials

`FileSecurePreferences` stores sensitive values in `%LOCALAPPDATA%/MindAttic/StreetSamurai/secure.dat`. AES encrypted with a key derived from `SHA256(MachineName:UserName:StreetSamurai)`. Not portable between machines by design.

### Directory Layout

All paths resolved by `FileSystemCanonPathProvider` relative to `CanonRootPath`:

```
{CanonRoot}/
  engine_data/
    canon.json              Single source of truth for all canon data
    graph/
      world_graph.json      Relationship network snapshot
  story_blocks/
    {PREFIX}.json           One file per story project
  stories/
    *.md                    Exported markdown stories (legacy format)
  canon_queue/
    *.json                  Pending canon submissions
  audio/
    narration_*.mp3         Generated audio files
  worldbuilding/            Markdown lore documents
  essences/                 Character/world essence YAML files
  character/facets/         Facet definition YAML files (legacy, now in canon.json)
```

---

## The Canon Database

### canon.json

`engine_data/canon.json` is the single source of truth. It is a pre-compiled JSON file containing all world data. `CanonDatabaseService` lazy-loads it with double-check locking and provides typed accessors.

**Top-level structure:**
```
version, generated_at
characters[]        — Full character profiles with psychology
facets[]            — The 6 facet definitions
districts[]         — Location data with atmosphere
factions[]          — Organizations
corponations[]      — Corporate nation-states
worldbuilding_docs[] — Long-form lore documents
story_bible         — Tone, theme, genre, protagonist, core hook
literary_rules      — Hard constraints on prose style
motifs[]            — Recurring thematic elements
character_profile   — Kyle's core identity and contradictions
```

### CanonDatabaseService

Key methods:
- `FindCharacter(nameOrAlias)` — Case-insensitive lookup with alias support
- `GetBlendedWeights(characterNames)` — Averages facet weights across multiple characters for ensemble casts
- `GetCharacterContext(name)` — Builds a rich LLM prompt block: psychology, fears, desires, speech patterns, relationships, story hooks
- `GetDistrictContext(name)` — Location atmosphere, sensory details, dangers, opportunities
- `GetLiteraryRulesPrompt()` — Formatted rules for injection into system prompts
- `Search(query, maxResults)` — Full-text search across all worldbuilding documents

### CanonService

Higher-level API that maps typed `CanonDatabase` models to runtime models (`Character`, `District`, `Faction`, `Corponation`). Provides JSON serialization for UI display and document browsing.

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

### Build & Query

- **Auto-build**: On first load, if `world_graph.json` doesn't exist, the graph is built from `canon.json` by parsing all characters, districts, factions, and their relationships, then inferring cross-entity links (e.g., character → corponation affiliation from text).
- `GetContextForNode(id)` — Returns formatted text suitable for LLM prompts: node properties + all connected edges with descriptions.
- `GetNeighbors(id, depth)` — BFS traversal to find entities within N hops.
- `EvolveRelationship(sourceId, targetId, ...)` — Updates edge weight (used during scene generation to track shifting dynamics).

### Persistence

Saves/loads as `GraphSnapshot` (nodes[] + edges[] + lastSaved timestamp) to `engine_data/graph/world_graph.json`. Can be rebuilt from canon at any time via `Rebuild()`.

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

### Facet Definition (from canon.json)

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

2. **Story starters** (`StoryStarterService.InferTriggers`): Keyword matching on the premise text. Maps words like "betray" → `betrayal`, "augment" → `transhumanism`, "child" → `children_in_danger`, etc. Falls back to `["unknown_danger", "moral_choice"]` if nothing matches.

---

## LLM System

### Architecture

```
ILlmService (interface)
  ├── ClaudeService      Anthropic Claude API
  ├── OpenAiService      OpenAI Chat Completions API
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

### Where LLM Calls Happen

| Caller | Temperature | Max Tokens | Purpose |
|--------|------------|------------|---------|
| `BeatGeneratorService` | Facet's temp (0.8 default) | 2048 | Generate narrative beats |
| `StoryStarterService.GenerateOpeningAsync` | Facet's temp | 2048 | Story opening |
| `StoryStarterService.GenerateOpeningAsync` (title) | 0.9 | 50 | Title generation |
| `StoryStarterService.ContinueAsync` | Facet's temp | 2048 | Story continuation |
| `StoryStarterService.PolishAsync` | 0.4 | 4096 | Prose refinement |
| `ContextAnalyzerService.AnalyzeAsync` | 0.3 | — | Psychological trigger extraction |
| `TextAnalysisService.LoreCheckAsync` | 0.3 | — | Canon consistency check |
| `TextAnalysisService.ClicheCheckAsync` | 0.3 | — | Cliche detection |
| `TextAnalysisService.ExpandAsync` | 0.85 | — | Text expansion |
| `TextAnalysisService.RephraseAsync` | 0.7 | — | Text rephrasing |

---

## Story Generation Pipelines

There are two generation pipelines: **Scene Generation** (multi-beat, event-driven) and **Story Starter** (single-shot, block-based).

### Pipeline 1: Scene Generation

`SceneGenerationService.GenerateSceneAsync(SceneRequest, FacetState)`

Used by the `/generate` page. Produces a multi-beat scene with facet rotation.

**Input — SceneRequest:**
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
     → Returns psychological triggers, dominant emotion, stakes

  2. FacetService.SelectFacets(weights, triggers, recentLeads)
     → Returns (lead, [support1, support2])
     → Rotation prevents same lead 3+ times in a row

  3. Fire OnBeatProgress event (UI updates progress bar)

  4. BeatGeneratorService.GenerateBeatAsync(context, lead, supporting)
     → Builds system prompt: lead voice + story bible + literary rules
       + supporting voices + core memories + character context + location
     → Builds user prompt: scene so far + beat goal
     → LLM generates 2-4 paragraphs

  5. Accumulate beat text into sceneSoFar
  6. Fire OnBeatCompleted event (UI renders new beat)
```

**Output — GeneratedScene:**
- `Beats[]` — Each with index, goal, leadFacet, supportingFacets, text, contextTags
- `FullText` — All beats concatenated

**Events:**
- `OnBeatProgress(BeatGenerationProgress)` — beat index, total, lead facet, status message
- `OnBeatCompleted(GeneratedBeat)` — full beat for live rendering

### Pipeline 2: Story Starter (Write Me a Story)

`StoryStarterService` — used by the `/write` page. Simpler, single-shot generation that feeds into the block editor.

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
- Picks 1-3 random characters from canon
- Picks a random district
- Picks from 13 hardcoded seed premises (drawn from actual world tensions)
- Picks a random mood
- Calls `GenerateOpeningAsync` with the random inputs

**ContinueAsync(existingParagraphs, prompt, mood, location, characters):**
- Same world context construction as opening
- Sends all existing paragraphs as "STORY SO FAR"
- User prompt includes the continuation direction
- Returns raw text (2-4 paragraphs)

**PolishAsync(blocks[], mood, location, characters):**
- Marks each block as `[LOCKED — DO NOT MODIFY]` or `[POLISH THIS]`
- Sends all blocks to LLM at temperature 0.4
- Instructions: tighten sentences, sharpen imagery, remove cliches, preserve story/characters/events
- Returns polished paragraphs in same order, same count
- Locked paragraphs must come back unchanged

---

## Story Block System

### Architecture

```
IStoryBlockRepository (interface — database migration seam)
  └── JsonStoryBlockRepository (JSON files on disk)
```

Every story is a `StoryProject` containing `StoryBlock` entries. One JSON file per project, named by prefix.

### StoryProject

```json
{
  "id": "a1b2c3d4",
  "prefix": "RVN",
  "title": "Dead Signal",
  "mood": "slow burn dread",
  "location": "The Shelf",
  "characters": ["Kyle", "Sable"],
  "status": "draft",
  "created": "2026-03-30T...",
  "modified": "2026-03-30T...",
  "blocks": [ ... ]
}
```

- **Prefix** — 1-6 uppercase alphanumeric characters. Must be unique across all projects. Determines the filename (`{PREFIX}.json`) and all block IDs.
- **Status** — `"draft"` or `"published"`

### StoryBlock

```json
{
  "id": "RVN_00003",
  "sequence": 3,
  "text": "The scanner pulsed twice before she could...",
  "locked": false,
  "created": "2026-03-30T...",
  "modified": "2026-03-30T..."
}
```

- **Id** — `{Prefix}_{Sequence:D5}` (e.g., `RVN_00001`, `RVN_00002`)
- **Sequence** — 1-based ordering. Determines display order and ID suffix.
- **Locked** — If true, the Polish operation will not modify this block's text. The LLM is instructed to return locked text verbatim.

### Prefix Rename

When the prefix is changed:
1. `StoryProject.RenamePrefix(newPrefix)` updates the prefix field and regenerates all block IDs
2. `JsonStoryBlockRepository.RenamePrefix()` writes the new JSON file (`{NEWPREFIX}.json`) and deletes the old one (`{OLDPREFIX}.json`)
3. Uniqueness is enforced via `PrefixExists()`

### IStoryBlockRepository Interface

```csharp
List<StoryProject> ListProjects();
StoryProject? LoadProject(string id);
void SaveProject(StoryProject project);
void DeleteProject(string id);
StoryProject RenamePrefix(string projectId, string newPrefix);
bool PrefixExists(string prefix, string? excludeProjectId);
```

This is the migration seam. To move to a database, implement this interface with EF Core or Dapper and register it in DI instead of `JsonStoryBlockRepository`. Nothing else changes.

### Text Import

`StoryProject.AddBlocksFromText(string text)`:
- Splits text on double newlines (paragraph boundaries)
- Creates a `StoryBlock` per paragraph with auto-incrementing sequence
- IDs generated as `{Prefix}_{NextSequence:D5}`

This is how both AI-generated text and manually pasted text enter the system.

---

## Text-to-Speech

### ElevenLabsTtsService

- API: `POST https://api.elevenlabs.io/v1/text-to-speech/{voiceId}`
- Header: `xi-api-key`
- Returns: MP3 byte array
- Timeout: 2 minutes
- Voice settings sent per request: stability, similarity_boost, style, use_speaker_boost

`SynthesizeAsync(text, voiceId?, ct)` — Sends the full story text (all blocks concatenated) as a single synthesis request.

`ListVoicesAsync()` — Fetches available voices for the Settings UI.

### AudioFileService

- Saves MP3 bytes to `{CanonRoot}/audio/narration_{timestamp}.mp3`
- `RevealInExplorer(path)` — Opens file manager with the file selected (Windows: `explorer.exe /select`, macOS: `open -R`, Linux: `xdg-open`)

### AudioPlayer Component

Shared Razor component used on `/write` and `/generate`:
- States: Loading → Error | Ready/Playing
- Uses HTML5 `<audio>` element with base64 data URLs
- Auto-plays on load
- Download button saves via `IAudioFileService`
- Open in Explorer button reveals saved file

---

## Text Analysis Tools

`TextAnalysisService` provides editor-integrated analysis (used on the `/editor` page):

| Method | Temperature | What It Does |
|--------|------------|--------------|
| `LoreCheckAsync(text, context)` | 0.3 | Searches canon + graph for entities mentioned in text, sends to LLM to check for contradictions |
| `ClicheCheckAsync(text)` | 0.3 | Checks against literary rule prohibitions (no noir cliches, no "chrome gleaming", no katana fetishism), suggests concrete fixes |
| `ExpandAsync(text, context)` | 0.85 | Continues prose from selection, maintaining voice |
| `RephraseAsync(text)` | 0.7 | Rewrites selected text with different word choices |

---

## Canon Queue

`CanonQueueService` manages a review pipeline for new world elements discovered during story generation.

- Storage: Individual JSON files in `canon_queue/` directory
- Filename: `{yyyyMMdd_HHmmss}_{sanitized_name}.json`
- Statuses: `"pending"` → `"promoted"` or `"rejected"`

Each `CanonQueueEntry` has:
- Name, Type (`character` / `location` / `faction` / `corponation` / `lore`)
- Context description
- SourceScene — which story it came from
- Status + Notes

---

## UI Pages

### /write — Write Me a Story (Primary writing interface)

The main authoring workspace. Every paragraph is a block.

**Layout:**
1. **Prefix + Title bar** — Editable prefix (triggers JSON file rename), editable title, block/lock counts
2. **Stories browser** — Toggle list of all saved projects, click to load
3. **Story blocks** — Each paragraph displayed with:
   - Block ID in monospace (e.g., `RVN_00003`)
   - Lock/unlock toggle (green border when locked)
   - Click-to-edit inline editing
   - Delete, move up, move down buttons
4. **Prompt input** — Always visible at bottom. Textarea for premises, directions, or paste mode.
5. **Action buttons:**
   - **Write It / Continue** — Generate via LLM (Ctrl+Enter shortcut)
   - **Paste Text** — Switch to paste mode, import raw text as blocks
   - **Surprise Me** — Zero-input random generation (first generation only)
   - **Polish Unlocked** — Send unlocked blocks through cleanup pass
   - **Narrate All** — Concatenate all blocks, synthesize audio via ElevenLabs
   - **Lock All** — Lock every block
   - **Stories** — Toggle project browser
   - **New** — Start fresh project
6. **Options** (collapsible) — Mood, Location (from districts), Characters
7. **Audio Player** — Appears after narration

**Auto-save**: Every meaningful action (generate, edit, lock, move, delete, prefix change) triggers `Repo.SaveProject()`.

**Routes:** `/write` (new project) or `/write/{ProjectId}` (load existing)

### /generate — Generate Scene

Multi-beat scene generation with live facet rotation visualization.

**Setup:** Scene goal, location, characters, beat count (3-8), optional per-beat themes.

**During generation:** Beat counter, cancel button, facet rotation bar, live beat rendering with color-coded borders per lead facet.

**After:** Save as Story, Narrate, New Scene.

Facet colors: Wound=#dc3545, Ideal=#198754, Id=#ffc107, Shadow=#6f42c1, Mask=#0dcaf0, Ghost=#6c757d.

### /editor/{id} — Story Editor

Full markdown editor with real-time preview and analysis tools (lore check, cliche check, expand, rephrase). Uses `StoryService` for persistence (markdown + frontmatter format).

### /characters — Character Browser

Lists all characters from canon with full profiles: psychology, speech patterns, facet weights (bar chart), relationships, story hooks.

### /districts, /factions, /corps, /technology — Lore Browsers

Read-only views of canon data with styled cards.

### /graph — World Graph

Visualization of the relationship network.

### /search — Canon Search

Full-text search across all worldbuilding documents and graph nodes.

### /queue — Canon Queue

Review interface for pending canon submissions. Promote or reject with notes.

### /settings — Configuration

API keys, model selection, provider toggle, TTS settings, canon root path, UI preferences. Shows configured/unconfigured status per provider.

### / — Home

Dashboard with stats: canon counts, graph size, story count, configured services.

---

## Prompt Architecture

Every LLM call builds a system prompt from layered context. Here's the typical structure for a generation call:

```
SYSTEM PROMPT:
  1. Role statement ("You are a literary fiction author writing cyberpunk...")
  2. Lead facet voice (system prompt + voice tone from facet definition)
  3. Supporting facet voices (may interject as [FACET_NAME] tagged lines)
  4. Story Bible (title, genre, tone, core theme, core hook, arc, protagonist)
  5. Literary Rules (hard constraints — NON-NEGOTIABLE)
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

---

## Data Flow: End to End

### Writing a New Story

```
1. User navigates to /write
2. New StoryProject created in memory (prefix "SS", title "Untitled")
3. User sets prefix to "RVN", types a premise, clicks "Write It"
4. StoryStarterService.GenerateOpeningAsync():
   a. CanonDatabaseService provides world context, literary rules, character psychology
   b. WorldGraphService provides relationship context
   c. FacetService selects lead + supporting facets based on premise triggers
   d. ClaudeService (via LlmRouter) generates 3-5 paragraphs
   e. Second LLM call generates title
5. StoryProject.AddBlocksFromText() splits into blocks: RVN_00001, RVN_00002, etc.
6. JsonStoryBlockRepository.SaveProject() writes RVN.json to story_blocks/
7. User reads blocks, locks the ones they like
8. User types "continue" direction, clicks Continue
9. StoryStarterService.ContinueAsync() generates 2-4 more paragraphs
10. New blocks appended: RVN_00006, RVN_00007, etc.
11. User clicks "Polish Unlocked"
12. StoryStarterService.PolishAsync() refines unlocked blocks at temperature 0.4
13. User clicks "Narrate All"
14. ElevenLabsTtsService.SynthesizeAsync() sends full text to ElevenLabs
15. AudioPlayer loads MP3 bytes and auto-plays
```

### Generating a Scene

```
1. User navigates to /generate, fills form
2. SceneGenerationService loops through beats:
   a. ContextAnalyzerService extracts psychological triggers from scene context
   b. FacetService selects lead facet (with rotation)
   c. BeatGeneratorService builds prompt and calls LLM
   d. Events fire → UI renders beat in real-time
3. Completed scene can be saved as a Story or narrated
```

### Importing Existing Text

```
1. User navigates to /write, sets prefix
2. Clicks "Paste Text" → textarea expands
3. Pastes multi-paragraph text
4. Clicks "Import as Blocks"
5. StoryProject.AddBlocksFromText() splits on double newlines
6. Each paragraph becomes a StoryBlock with auto-sequenced ID
7. All blocks unlocked by default — can be polished, locked, reordered, or deleted
```

---

## Key Design Decisions

### Why Blocks Instead of Free-Form Text

Each paragraph is a discrete entity because:
- **Selective refinement** — Lock finished paragraphs, polish only what needs work
- **Atomic operations** — Move, delete, regenerate at paragraph level
- **Future TTS integration** — Per-paragraph emotion markup for ElevenLabs
- **Database-ready** — Each block is a row, not a substring

### Why JSON Files (For Now)

- Simple, human-readable, debuggable
- One file per story = easy backup/copy
- No database setup required
- `IStoryBlockRepository` interface means the switch to a database is a single DI registration change

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
- The graph auto-builds from canon.json if the snapshot is missing or corrupt

---

## Migration Notes

### To Add a Database Backend

1. Implement `IStoryBlockRepository` with your ORM (EF Core, Dapper, etc.)
2. Register it in `ServiceCollectionExtensions` instead of `JsonStoryBlockRepository`
3. Nothing else changes — the UI and all services consume the interface

The `StoryProject` and `StoryBlock` models are flat and have no markdown parsing, no file paths, no I/O — they map directly to database tables.

### To Add a New LLM Provider

1. Implement `ILlmService`
2. Add it to `LlmRouter`'s provider map
3. Add configuration fields to `SettingsService`
4. Add UI controls to the Settings page

### To Add ElevenLabs Emotion Markup

The block-per-paragraph structure makes this natural:
1. Add an `EmotionHints` field to `StoryBlock` (e.g., `"whispered"`, `"angry"`, `"sorrowful"`)
2. When synthesizing, wrap each block's text in ElevenLabs SSML tags based on hints
3. The UI already has per-block controls — add an emotion dropdown next to each lock button
