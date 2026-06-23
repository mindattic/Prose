# StreetSamurai MCP Tools

> **GENERATED — do not hand-edit.** Produced by `ToolDocGenerator` from the
> `[McpServerTool]` + `[Description]` attributes in `v3/StreetSamurai.Mcp/Tools*.cs`,
> the same source the MCP host registers via `WithToolsFromAssembly()`. To refresh:
> 
> ```powershell
> dotnet run --project v3/StreetSamurai.Mcp -- --export-tools docs/MCP_TOOLS.md
> ```
>
> All tools are MCP-prefixed `mcp__streetsamurai__<name>` by the client. Most return a
> JSON string; the canon is the SQL database, scoped to the active Universe.

**176 tools** across **24 tool families.**

## Families

| Family | Tools |
| --- | --- |
| [Bible](#bible) | 3 |
| [Canon](#canon) | 9 |
| [Combat](#combat) | 1 |
| [Config](#config) | 4 |
| [Context](#context) | 4 |
| [Continuity](#continuity) | 2 |
| [Core Entity Crud](#core-entity-crud) | 4 |
| [Encyclopedia](#encyclopedia) | 35 |
| [Findings](#findings) | 5 |
| [Gear Entity Crud](#gear-entity-crud) | 7 |
| [Lore Triple](#lore-triple) | 7 |
| [Narrative Science](#narrative-science) | 5 |
| [Planning](#planning) | 6 |
| [Quality](#quality) | 10 |
| [Repository](#repository) | 2 |
| [Scene](#scene) | 4 |
| [Species](#species) | 2 |
| [Story](#story) | 6 |
| [Strand](#strand) | 29 |
| [Universe](#universe) | 3 |
| [Voice](#voice) | 5 |
| [World Entity Crud](#world-entity-crud) | 5 |
| [World Modelling](#world-modelling) | 15 |
| [Writing](#writing) | 3 |

## Bible

<sub>`BibleTools`</sub>

### `get_character_profile`

Load the Character Profile — the protagonist's core contradiction, signature behavior, voice anchors. Often Kyle's profile in this project.

- _(no parameters)_

### `get_story_bible`

Load the Story Bible — structural rules for narrative shape: act structure, beat anatomy, motif planting, dialogue cadence.

- _(no parameters)_

### `get_tone_bible`

Load the Tone Bible — voice, register, sensory palette, what to do and what not to do for prose. Inject this into the system prompt when drafting prose.

- _(no parameters)_

## Canon

<sub>`CanonTools`</sub>

### `get_character`

Load a character's full canon record by name: identity, psychology (core_fears, core_desires, coping_mechanisms, blind_spots, secret), behavioral (decision_rules, escalation_ladder, contradictions, habits, breaking_points, stress_responses), speech_patterns (vocabulary, cadence, verbal_tics, example_lines), augmentations, story_hooks. This is the primary source for voice when writing a POV chapter.

- `name` (string, required) — Exact name of the character (e.g. 'Kyle Ellen Corbin' or 'Sasha VÃµ').

### `get_corponation`

Load a CorpoNation by name: sector, hierarchy, holdings, public-facing brand, dirty laundry.

- `name` (string, required) — Exact CorpoNation name.

### `get_faction`

Load a faction by name: leadership, structure, territory, motives, alliances, rivalries.

- `name` (string, required) — Exact faction name.

### `get_literary_rules`

Load the world's literary rules: prohibitions, paragraph requirements, POV voice differentiation rules, register permissions, paragraph economy, interior_monologue source. Inject this near the top of any prose-generation prompt.

- _(no parameters)_

### `get_place`

Load a place / district by name. Returns description, sensory_details, parent territory, geography.

- `name` (string, required) — Exact name of the place.

### `list_characters`

List every character in canon. Returns name + role + status for each. Cheap â€” call this first when you need to know who exists.

- _(no parameters)_

### `list_corponations`

List every CorpoNation (corporate sovereign entity).

- _(no parameters)_

### `list_factions`

List every faction in canon: street gangs, syndicates, cells, advocacy groups, etc.

- _(no parameters)_

### `list_places`

List every place / district in canon. Use this to find a location for a scene.

- _(no parameters)_

## Combat

<sub>`CombatTools`</sub>

### `draft_combat_scene`

Generate an action sequence using the StreetSamurai combat writer. Respects participants' canon loadouts, current injuries/stress, and tracks ammo/grenade counts across beats. Tone shapes word choice and pacing — pick deliberately. Always pass preceding_context (last 1–3 paragraphs leading into the fight) so the prose transitions cleanly. sides_json must be a JSON array of side objects; see parameter description for shape. Returns the generated beats plus the full stitched text. Run validate_canon_text on the result before staging it into a chapter.

- `battlefieldLocation` (string, required) — Place name or district where the fight occurs (used to pull terrain/cover).
- `sidesJson` (string, required) — JSON array of combat sides. Each entry: { "label": str, "combatants": [canon character names], "unnamed_combatants": ["three drones", ...], "initial_position": str, "goal": str, "shared_loadout": str }. Label is required; everything else is optional. Usually 2 sides; up to 3 for a three-way.
- `environment` (string, optional) — Environmental specifics shaping the action — 'rain on rusted steel', 'flickering neon'.
- `objective` (string, optional) — What the scene is building toward — 'extract the courier', 'kill the target', 'buy time'.
- `openingBeat` (string, optional) — Inciting action that opens the fight — 'Kyle draws Silence and steps through the strop', 'the door blows in'.
- `precedingContext` (string, optional) — Last 1–3 paragraphs of narration before the fight, for tonal continuity.
- `numExchanges` (int, optional) — Number of attack/react cycles to generate. 3–6 is normal.
- `tone` (string, optional) — Tonal register: Brutal | Cinematic | Desperate | Clinical | Chaotic. Brutal = work; Cinematic = choreography; Desperate = losing-side POV; Clinical = mercenary detachment; Chaotic = broken perception.
- `initialResourcesJson` (string, optional) — Optional JSON object: { "<character name>": { "ammo_by_weapon": { "Cacophony": 4, "XB-7 Silence": 0 }, "bio_battery_percent": 80, "meal_context": "full meal 2h ago" } }. When present, the writer enforces ammo/charge limits across beats. Leave empty to skip resource tracking for this scene.

## Config

<sub>`ConfigTools`</sub>

### `get_markdown_file`

Get the content of a tracked markdown file from the database. Pass asOf (ISO 8601 UTC) to retrieve a historical version from the temporal table. relativePath examples: 'CLAUDE.md', 'docs/BIBLE.md', 'feedback_sequential_strand_writing.md'

- `relativePath` (string, required) — Relative path key, e.g. 'CLAUDE.md' or 'docs/AMENDMENTS.md'.
- `asOf` (string, optional) — Optional ISO 8601 UTC datetime to retrieve the version current at that moment.

### `list_markdown_files`

List all markdown files tracked in the database (project rules, Codex docs, Claude Code memory). Returns category, relativePath, contentHash, and lastSyncedAt for each file.

- _(no parameters)_

### `restore_markdown_file`

Restore markdown files from the database back to disk. Pass relativePath to restore a single file; omit to restore all tracked files. Pass asOf (ISO 8601 UTC) to recover a historical version from the temporal table. Pass dryRun=true to preview without writing to disk.

- `relativePath` (string, optional) — Relative path of the file to restore, e.g. 'CLAUDE.md'. Omit to restore all.
- `asOf` (string, optional) — Optional ISO 8601 UTC datetime for point-in-time recovery.
- `dryRun` (bool, optional) — If true, report what would be written without touching the filesystem.

### `sync_markdown_files`

Sync all discovered markdown files from disk into the database. Only files whose content hash changed produce a new history row. Pass dryRun=true to preview without writing.

- `dryRun` (bool, optional) — If true, report what would be synced without writing to the database.

## Context

<sub>`ContextTools`</sub>

### `get_motifs`

List the registered motifs for a book â€” recurring objects, phrases, gestures, sensory threads. Mention these in the chapter you're writing where natural; the review pipeline flags chapters that drop the whole inventory.

- `bookId` (string, required) — Book id.

### `get_neighbors`

Get a graph node's neighbors (relationships) up to N hops. Use this to walk from a known entity to entities related by canon â€” alliances, rivalries, family, mentor links, location ownership.

- `nodeId` (string, required) — Node id (use search_semantic or list_characters to find the id).
- `hops` (int, optional) — Hops to traverse. 1 = direct neighbors. Default 1.

### `plant_motif`

Plant a new motif in a book's inventory. Idempotent by name (re-planting with a longer description merges). The user normally accepts these from the Motifs panel in the UI; this tool exposes the same write so chat-side authoring can register them too.

- `bookId` (string, required) — Book id.
- `name` (string, required) — Motif name, e.g. 'brick-wall notebook' or 'the door is unlocked'.
- `description` (string, required) — Short description of what this motif means and where it lands.
- `kind` (string, required) — MotifKind: Object, Phrase, Gesture, Sensory, Ritual.
- `introducedInChapterId` (string, required) — Chapter id where this motif is being introduced.

### `search_semantic`

Search the world graph by theme, not by name. TF-IDF cosine similarity across every entity description. Use this to surface entities that are *thematically relevant* to what you're about to write â€” e.g. searching 'corporate betrayal under-table contract' might return Sable's backstory, the Lotus Syndicate, the Ferrogate enforcement arm. Returns ranked id+name+type+score.

- `query` (string, required) — Free-text query â€” describe the theme/scene/concept.
- `topK` (int, optional) — Number of top hits to return. Default 8.

## Continuity

<sub>`ContinuityTools`</sub>

### `find_contradictions`

Find contradictions in a chapter against established canon. Pulls the characters from the chapter's `characters` field, plus the book's state_at_end and all prior chapters' synopses, builds a canon-context bundle, and dispatches a Legion Quorum vote with a contradiction-finding rubric (EPISTEMIC / TEMPORAL / CAPABILITY / CANON). Returns a JSON report with findings, citations, severity, and suggested fixes. Exit-code-equivalent convention: ok=true means no contradictions; ok=false means findings exist.

- `chapterId` (string, required) — Chapter id (32-char hex). The chapter must exist in engine/data/chapters/<id>/chapter.json.
- `quorum` (string, optional) — Quorum requirement for the contradiction vote: plurality | simplemajority | twothirds | unanimous. Default plurality (most permissive — surfaces every voter's concerns).
- `maxTokens` (int, optional) — Max tokens per voter response. Default 4096. Larger values produce more thorough reports but cost more.
- `maxContextChars` (int, optional) — Hard cap on canon-context characters before the draft text is appended. Default 80000. Lower this if hitting provider context limits.

### `find_contradictions_book`

Find contradictions across an entire book by running a pairwise sweep — every chapter is graded against the FULL PROSE of every OTHER chapter (forward AND backward). Catches things a single-chapter check misses: a character who dies in chapter 3 but speaks in chapter 5, a character revealed left-handed in chapter 6 catching a ball right-handed in chapter 2, a stated age that drifts between chapters, etc. Cross-chapter findings are consolidated so the same contradiction surfaces once with all chapter numbers attached. Expensive — dispatches N Legion votes per book. Use synopsisOnly=true for cheaper triage that skips prose-level facts. Returns a JSON report with per-chapter findings and a consolidated cross-book finding list. Exit-code-equivalent convention: ok=true means no contradictions; ok=false means findings exist.

- `bookId` (string, required) — Book id (32-char hex). The book must exist in engine/data/books/<id>.json with a non-empty chapter_ids list.
- `quorum` (string, optional) — Quorum requirement for the contradiction vote: plurality | simplemajority | twothirds | unanimous. Default plurality (most permissive — surfaces every voter's concerns).
- `maxTokens` (int, optional) — Max tokens per voter response. Default 4096. Larger values produce more thorough reports but cost more.
- `maxContextChars` (int, optional) — Hard cap on canon-context characters per chapter pass. Default 0 = let the script choose (400000 with prose, 120000 with synopsisOnly). Lower this if hitting provider context limits.
- `synopsisOnly` (bool, optional) — If true, feed only chapter synopses (not full prose) as canon. Cheaper but misses prose-level facts like handedness or specific physical actions. Default false (prose included).

## Core Entity Crud

<sub>`CoreEntityCrudTools`</sub>

### `create_character`

Create or update a character in canon. Pass empty id to create new; pass an existing id to update. List fields (tags, story_hooks, aliases) are comma-delimited strings. Complex fields (psychology_json, speech_patterns_json, physical_description_json) accept optional JSON — omit to keep defaults.

- `name` (string, required) — Character's full name. Required.
- `role` (string, optional) — Role or function in the world (e.g. 'street samurai', 'fixer', 'cleanup contractor').
- `description` (string, optional) — Prose description of who this character is.
- `species` (string, optional) — Species: human, ai, android, robot, cyborg, synthetic, hybrid, unknown.
- `gender` (string, optional) — Gender identity.
- `pronouns` (string, optional) — Pronouns (e.g. 'he/him', 'she/her', 'they/them').
- `age` (int, optional) — Age in years.
- `status` (string, optional) — Status: alive, deceased, unknown, missing.
- `location` (string, optional) — Current location or home territory.
- `affiliation` (string, optional) — Faction, corp, or freelancer network affiliation.
- `augmentations` (string, optional) — Augmentation summary — cyberware, genemods, neural enhancements.
- `narrativeFunction` (string, optional) — Narrative function: what role this character plays in stories.
- `tags` (string, optional) — Comma-separated tags (e.g. 'freelancer,enforcer,Tier 3').
- `storyHooks` (string, optional) — Comma-separated story hooks — unresolved threads this character carries.
- `psychologyJson` (string, optional) — Optional JSON for the psychology block: {core_fears, core_desires, coping_mechanisms, blind_spots, secret}.
- `speechPatternsJson` (string, optional) — Optional JSON for speech_patterns: {vocabulary, cadence, verbal_tics, example_lines, subtext}.
- `physicalDescriptionJson` (string, optional) — Optional JSON for physical_description: {heritage, height_cm, weight_kg, build, hair_color, eye_color, distinguishing_marks}.
- `id` (string, optional) — Optional existing character id (32-char hex or full UUID) to update.

### `create_corponation`

Create or update a CorpoNation (corporate sovereign entity) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — CorpoNation name. Required.
- `fullLegalName` (string, optional) — Full legal corporate name.
- `sector` (string, optional) — Industry sector.
- `sovereignTerritory` (string, optional) — Territory the corp controls or dominates.
- `stockDesignation` (string, optional) — Stock ticker or designation.
- `foundingStory` (string, optional) — Founding story or origin.
- `securityForce` (string, optional) — Security force name and description.
- `keyDetail` (string, optional) — Key distinguishing detail about this corp.
- `fullText` (string, optional) — Full prose text describing the CorpoNation.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing CorpoNation id to update.

### `create_faction`

Create or update a faction (street gang, syndicate, cell, advocacy group, etc.) in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.

- `name` (string, required) — Faction name. Required.
- `motto` (string, optional) — Faction motto or slogan.
- `description` (string, optional) — Prose description.
- `ideology` (string, optional) — Core ideology.
- `territory` (string, optional) — Territory the faction controls.
- `leadership` (string, optional) — Leadership structure and named leaders.
- `narrativeFunction` (string, optional) — Narrative function — what role this faction plays in stories.
- `methods` (string, optional) — Comma-separated operational methods.
- `goals` (string, optional) — Comma-separated goals.
- `storyHooks` (string, optional) — Comma-separated story hooks.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing faction id to update.

### `create_place`

Create or update a place / district in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.

- `name` (string, required) — Place name. Required.
- `type` (string, optional) — Type of place (e.g. 'district', 'building', 'landmark', 'corridor', 'station').
- `description` (string, optional) — Prose description of the place.
- `demographics` (string, optional) — Demographic makeup.
- `economy` (string, optional) — Economic profile.
- `powerStructure` (string, optional) — Who holds power here and how.
- `dangers` (string, optional) — Comma-separated dangers present in this place.
- `storyHooks` (string, optional) — Comma-separated story hooks.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing place id to update.

## Encyclopedia

<sub>`EncyclopediaTools`</sub>

### `get_ammunition`

Load an ammunition record.

- `name` (string, required) — Ammunition name.

### `get_apparel`

Load an apparel record.

- `name` (string, required) — Apparel name.

### `get_archetype`

Load an archetype record: typical behavior, knowledge, equipment, social position.

- `name` (string, required) — Archetype name.

### `get_automaton`

Load an automaton record. Behemoths and other industrial automata: not synthetic life — these are machines.

- `name` (string, required) — Automaton name.

### `get_consumer_good`

Load a consumer good record.

- `name` (string, required) — Consumer good name.

### `get_cyberware`

Load a cyberware record: install procedure, side effects, sensory experience, dependency profile.

- `name` (string, required) — Cyberware ProductName or Name.

### `get_document`

Load a worldbuilding document by its file_name (the filename-derived identifier). Returns the full prose body.

- `fileName` (string, required) — Document file_name (e.g. 'corponations_overview' or as listed by list_documents).

### `get_equipment`

Load an equipment record. Match is by ProductName when set, else Name (case-insensitive).

- `name` (string, required) — Equipment ProductName or Name.

### `get_genemod`

Load a gene modification record.

- `name` (string, required) — Genemod name.

### `get_lab_specimen`

Load a lab specimen record.

- `name` (string, required) — Specimen name.

### `get_material`

Load a material record: properties, applications, sensory qualities.

- `name` (string, required) — Material name.

### `get_pharmaceutical`

Load a pharmaceutical record: effects, dosage, side effects, dependency profile.

- `name` (string, required) — Pharmaceutical name.

### `get_psionic`

Load a psionic record.

- `name` (string, required) — Psionic name.

### `get_subsidiary`

Load a subsidiary record.

- `name` (string, required) — Subsidiary name.

### `get_technology`

Load a technology record. Match is by ProductName when set, else Name.

- `name` (string, required) — Technology ProductName or Name.

### `get_transportation`

Load a transportation record.

- `name` (string, required) — Transportation name.

### `get_weapon`

Load a weapon's full record by name: category, manufacturer, ammunition_type, lethality, mechanics, sensory detail, story_hooks, image prompts.

- `name` (string, required) — Weapon name.

### `list_ammunition`

List every ammunition variant in canon (calibers, specialty rounds, energy cells).

- _(no parameters)_

### `list_apparel`

List every apparel item in canon: clothing, armor, accessories.

- _(no parameters)_

### `list_archetypes`

List every archetype: occupational/social roles in the world (street samurai, fixer, runner, gleaner, etc).

- _(no parameters)_

### `list_automata`

List every automaton in canon: drones, security bots, Iowan Behemoths, agricultural machines.

- _(no parameters)_

### `list_consumer_goods`

List every consumer good: food, drinks, household items, branded products.

- _(no parameters)_

### `list_cyberware`

List every cyberware product: implants, neural augmentations, prosthetic limbs.

- _(no parameters)_

### `list_documents`

List every worldbuilding document by file name + title + category. Use get_document to load the body.

- _(no parameters)_

### `list_equipment`

List every equipment item in canon: gear, tools, devices, augmentation accessories.

- _(no parameters)_

### `list_genemods`

List every gene modification: somatic edits, lineage modifications, body-spec edits.

- _(no parameters)_

### `list_lab_specimens`

List every lab specimen — anomalous biological / synthetic / hybrid samples held in research facilities.

- _(no parameters)_

### `list_materials`

List every material: alloys, composites, fabrics, biomaterials. Use this when describing physical objects with specificity.

- _(no parameters)_

### `list_pharmaceuticals`

List every pharmaceutical: drugs, stims, pain modulators, neuro-pharma.

- _(no parameters)_

### `list_psionics`

List every psionic phenomenon recorded in canon.

- _(no parameters)_

### `list_quotes`

List every quote: in-world sayings, graffiti, advertising copy, attributed quotes. Useful for chapter epigraphs and ambient flavor.

- `tag` (string, optional) — Optional filter: only quotes with a tag matching this value. Empty for all.

### `list_subsidiaries`

List every subsidiary — child/holding companies of larger CorpoNations.

- _(no parameters)_

### `list_technology`

List every technology entry: software, protocols, networks, systems.

- _(no parameters)_

### `list_transportation`

List every transportation entry: vehicles, transit systems, The Pulse stations, individual transports.

- _(no parameters)_

### `list_weapons`

List every weapon in canon. Returns name + category + manufacturer. Use this to find a weapon for an action scene.

- _(no parameters)_

## Findings

<sub>`FindingsTools`</sub>

### `apply_finding`

Apply a finding's suggested fix to the source file. Locates the snippet in the file, replaces it with the suggested rewrite, writes a backup to engine/data/archives/findings/, and marks the finding Applied. Returns the outcome: Applied, SnippetNotFound (LLM paraphrased — edit manually), NoSuggestedFix, NoSnippet, FileMissing, or Failed.

- `id` (long, required) — Finding id from list_findings.

### `findings_stats`

Counts of findings per status (new / triaged / applied / dismissed).

- _(no parameters)_

### `list_findings`

List findings from the autonomous quality inbox. ContinuousQualityService auto-detects contradictions and clichés on every chapter save; results land here for triage. Sorted high-severity-first.

- `status` (string, optional) — Filter by status: New, Triaged, Applied, Dismissed. Omit for all.
- `limit` (int, optional) — Max number of findings to return. Default 100.

### `scan_chapter_quality`

Manually trigger a quality scan (contradiction + cliché) on a single chapter file. Normally the autonomous monitor runs this on every save; use this for ad-hoc rescans without modifying the file.

- `filePath` (string, required) — Absolute path to a chapter.json file.

### `set_finding_status`

Mark a finding triaged / applied / dismissed without writing to source files.

- `id` (long, required) — Finding id.
- `status` (string, required) — Target status: Triaged, Applied, or Dismissed.

## Gear Entity Crud

<sub>`GearEntityCrudTools`</sub>

### `create_ammunition`

Create or update an ammunition type (calibers, specialty rounds, energy cells) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Ammunition name. Required.
- `category` (string, optional) — Category (e.g. 'pistol', 'rifle', 'shotgun', 'energy', 'specialty').
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer name.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing ammunition id to update.

### `create_apparel`

Create or update an apparel item (clothing, armor, accessories) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Apparel name. Required.
- `category` (string, optional) — Category (e.g. 'outerwear', 'armor', 'footwear', 'accessories').
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer name.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing apparel id to update.

### `create_cyberware`

Create or update a cyberware implant in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.

- `name` (string, required) — Cyberware name. Required.
- `brandName` (string, optional) — Brand name.
- `productName` (string, optional) — Product model name.
- `category` (string, optional) — Category (e.g. 'neural', 'limb', 'sensory', 'combat', 'subdermal').
- `bodyLocation` (string, optional) — Body location of installation.
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer name.
- `tierAvailability` (string, optional) — Tier availability.
- `legality` (string, optional) — Legal status.
- `installationRequirements` (string, optional) — Installation requirements.
- `specifications` (string, optional) — Technical specifications.
- `streetPrice` (string, optional) — Street price (unregulated market).
- `culturalContext` (string, optional) — Cultural context in the GLMZ world.
- `sideEffects` (string, optional) — Comma-separated side effects.
- `storyHooks` (string, optional) — Comma-separated story hooks.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing cyberware id to update.

### `create_equipment`

Create or update a piece of equipment (gear, tools, devices, accessories) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Equipment name. Required.
- `brandName` (string, optional) — Brand name.
- `productName` (string, optional) — Product model name.
- `category` (string, optional) — Category (e.g. 'surveillance', 'medical', 'demolitions', 'comms').
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer name.
- `tierAvailability` (string, optional) — Tier availability.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing equipment id to update.

### `create_pharmaceutical`

Create or update a pharmaceutical (drug, stim, pain modulator, neuro-pharma) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Pharmaceutical name. Required.
- `category` (string, optional) — Category (e.g. 'stimulant', 'analgesic', 'neuro-modulator', 'combat stim').
- `description` (string, optional) — Prose description and effects.
- `manufacturer` (string, optional) — Manufacturer name.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing pharmaceutical id to update.

### `create_technology`

Create or update a technology entry (software, protocols, networks, systems) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Technology name. Required.
- `brandName` (string, optional) — Brand name.
- `productName` (string, optional) — Product model name.
- `subcategory` (string, optional) — Subcategory (e.g. 'neural interface', 'network protocol', 'AI system').
- `description` (string, optional) — Prose description.
- `developers` (string, optional) — Comma-separated developer names (corporations, labs, individuals).
- `tierAvailability` (string, optional) — Tier availability.
- `storyHooks` (string, optional) — Comma-separated story hooks.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing technology id to update.

### `create_weapon`

Create or update a weapon in canon. Pass empty id to create new; pass an existing id to update. List fields are comma-delimited strings.

- `name` (string, required) — Weapon name. Required.
- `category` (string, optional) — Category (e.g. 'melee', 'pistol', 'shotgun', 'rifle', 'explosive', 'launcher').
- `description` (string, optional) — Prose description of the weapon.
- `manufacturer` (string, optional) — Manufacturer name.
- `tierAvailability` (string, optional) — Tier availability (e.g. 'Tier 2+', 'black market', 'military only').
- `legality` (string, optional) — Legal status.
- `specifications` (string, optional) — Technical specifications.
- `tacticalUse` (string, optional) — Tactical use and combat role.
- `culturalContext` (string, optional) — Cultural context in the GLMZ world.
- `ammunitionTypes` (string, optional) — Comma-separated ammunition types this weapon accepts.
- `storyHooks` (string, optional) — Comma-separated story hooks.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing weapon id to update.

## Lore Triple

<sub>`LoreTripleTools`</sub>

### `apply_continuity_claim`

Apply a CANONICAL or CONFIRMED claim to its entity record file. Legion's panel picks which field should hold the value (string fields are set, array fields are appended to, otherwise the claim is appended to a continuity_facts[] array). The audit trail records which field was chosen.

- `claimUid` (string, required) — Claim uid to apply.

### `extract_continuity_from_book`

Extract continuity claims from every chapter in a book (sequential — long-running). Returns per-chapter results plus aggregate counts.

- `bookId` (string, required) — Book id (32-char hex).
- `quorum` (string, optional) — Quorum: plurality | simplemajority | twothirds | unanimous. Default plurality.
- `maxTokens` (int, optional) — Max tokens per voter response. Default 4096.
- `minVoters` (int, optional) — Minimum voters that must propose a claim for it to be stored. Default 1.

### `extract_continuity_from_chapter`

Extract atomic continuity claims (entity, predicate, object triples) from a chapter's prose via Legion Quorum. Each triple's snippet is validated against the source prose; survivors are upserted into the unified continuity store. Same-(entity,predicate) with different `object` auto-flags a contradiction. Returns: new / confirmed / contradicted counts. ok=true when no new contradictions surfaced.

- `chapterId` (string, required) — Chapter id (32-char hex).
- `quorum` (string, optional) — Quorum: plurality | simplemajority | twothirds | unanimous. Default plurality.
- `maxTokens` (int, optional) — Max tokens per voter response. Default 4096.
- `minVoters` (int, optional) — Minimum voters that must propose a claim for it to be stored. Default 1.

### `extract_continuity_from_entity_record`

Extract continuity claims from a single entity record by EntityId (canonical Records.Json blob in SQL). Top-level scalar fields become direct claims; prose fields (description, personality, ideology…) go through the same Legion Quorum vote as chapter prose.

- `entityId` (string, required) — EntityId (guid, hyphenated or 32-char hex) of the canon entity to extract from.

### `get_continuity_claims`

List continuity claims. Optional filters: entity (id or name), status (NEW | CONFIRMED | CONTRADICTED | CANONICAL | REJECTED | SUPERSEDED). Returns the claims with their predicates, objects, sources, and statuses.

- `entity` (string, optional) — Optional: entity name to filter to one entity.
- `status` (string, optional) — Optional: status filter.

### `list_continuity_contradictions`

List every CONTRADICTED claim awaiting resolution. Each entry is a pair (A, B) where A and B share (entity, predicate) but have different `object` values. Use ResolveContinuityContradiction to pick a winner.

- _(no parameters)_

### `resolve_continuity_contradiction`

Resolve a contradiction. Winner = A | B (one claim wins → CANONICAL, the other → REJECTED) or `custom` (both rejected, a new writer-asserted CANONICAL claim takes their place; pass customObject).

- `aUid` (string, required) — Claim A uid.
- `bUid` (string, required) — Claim B uid (must belong to same entity as A).
- `winner` (string, required) — Winner: A | B | custom.
- `customObject` (string, optional) — Required when winner=custom: the agreed value.
- `note` (string, optional) — Optional resolution note (kept in audit trail).

## Narrative Science

<sub>`NarrativeScienceTools`</sub>

### `analyze_sacred_flaw`

Analyze or scaffold a character's Sacred Flaw (their theory of control) per Will Storr's Science of Storytelling. The Sacred Flaw is the character's core false belief about reality — the strategy they use to control their environment. Returns: theory_of_control (the false belief), origin_damage (the formative wound), secret_dread (what they fear if they drop the flaw), hero_maker_narrative (how they frame it as a strength), material_gains (career/status advantages that make change terrifying), confidence (high/medium/low), and a diagnostic paragraph on what story arc this flaw enables. Pass scaffold=true to generate a plausible flaw from the character's existing description when none is explicitly documented.

- `characterIdOrSlug` (string, required) — Character entity ID (GUID) or slug.
- `scaffold` (bool, optional) — If true, generate a plausible flaw scaffold from available description (use when flaw is not yet documented). Default false = analyze existing data.

### `audit_scene_engagement`

Run the 6-point scene anatomy audit on beat text per Will Storr's neural engagement mechanisms. The six mechanisms: (1) unexpected_change — something the character didn't plan happens; (2) information_gap — a question the reader wants answered is opened/closed; (3) cause_effect — this beat is caused by the prior, causes the next; (4) tribal_emotion — moral outrage / status play / humiliation / gossip / altruistic punishment; (5) specificity — 3+ precise non-generic sensory or physical details; (6) show_not_tell — action/dialogue/sensation ≥60% vs summary/exposition. A beat passes overall if 4 of 6 are present. Returns per-mechanism verdict with evidence, mechanisms_passing count, beat_passes boolean, the single top_weakness, and a concrete fix suggestion.

- `beatText` (string, required) — The beat's prose text to audit.

### `check_antihero_empathy`

Evaluate whether a beat activates the four antihero empathy levers per Will Storr. The four levers: (1) pre_deflation — a worse villain or more selfish character is visible, making the antihero look better; (2) vulnerability_pain — the beat shows the wound or fear beneath the surface; (3) genuine_virtue — the antihero acts selflessly, even briefly; (4) altruistic_punishment — the antihero punishes selfishness the reader also wants punished. Returns per-lever verdict with evidence, levers_active count (0–4), empathy_score 1–10, a diagnosis paragraph, and an improvement hint. Accepts character id (GUID) or slug.

- `characterIdOrSlug` (string, required) — Character entity ID (GUID) or slug.
- `beatText` (string, required) — The beat's prose text to evaluate.

### `check_dramatic_question`

Score how well a beat poses or answers the Dramatic Question ('who is this person REALLY?') per Will Storr's framework. The question operates on two levels simultaneously: surface (plot — what is happening) and subconscious (character — what this reveals about the character's core belief / theory of control). Strong beats address both; weak beats address only the surface. Returns: surface_score 1–10, subconscious_score 1–10, overall_score 1–10, plain-English summaries of each level, dramatic_question_active flag, and one concrete improvement hint. Optionally provide character_id_or_slug to give the LLM context about whose theory of control is being tested.

- `beatText` (string, required) — The beat's prose text to evaluate.
- `characterIdOrSlug` (string, optional) — Optional character entity ID or slug for additional context (improves subconscious scoring). Omit to score blind.

### `map_five_act_structure`

Map a strand's beats to Will Storr's five-act character-change arc. Act I: establish the protagonist's flaw + ignition event (unexpected change that pressures the flaw). Act II: character applies old theory of control, it partially works. Act III: transformation trigger — the flaw fails catastrophically or wins at too high a cost. Act IV: dark night — all fears realized, old theory stripped. Act V: God moment — dramatic question answered definitively (comic: transformation; tragic: doubling down). Returns: beat assignments per act, ignition_beat / trigger_beat / god_moment_beat numbers, structural_gaps list, structural_strengths list, resolution type (comic/tragic/unclear), and an overall assessment paragraph. Accepts strand id (GUID) or slug.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.

## Planning

<sub>`PlanningTools`</sub>

### `extract_entities`

Extract named entities and relationships from arbitrary prose. Useful AFTER drafting a chapter — surfaces any new characters, places, factions, weapons, technology mentioned that aren't in canon yet (candidates for promotion). Returns the structured ExtractionResult with entities (with type + description + properties) and relationships (source → target). Calls the LLM internally — slow on long text, fast on a single beat.

- `text` (string, required) — Prose text to scan.

### `get_consequence_context`

Build the LLM-ready 'consequences in play' context block for a protagonist. Combines protagonist-specific consequences with the 5 most recent world events, dedupes, caps at 10 entries, flags unresolved threads. Plug this directly into a chapter prompt's situational context.

- `protagonistName` (string, optional) — Protagonist name. Optional — pass empty for unfocused 'world events' context.

### `get_consequences_for`

Get every world consequence affecting a specific entity (character, faction, place). Returns the consequences ordered by recorded_at descending.

- `entityName` (string, required) — Entity name (character, faction, place, etc.).

### `get_neighbors_by_relation`

List a node's edges filtered by relation type. Subset of get_neighbors that returns only relationships matching a specific relation_type — e.g. 'rival', 'allied', 'mentor', 'family', 'controls_territory', 'frequents'. Useful for targeted lookups: 'who are Kyle's known rivals'.

- `nodeId` (string, required) — Source node id (use search_semantic / list_characters / etc. to find).
- `relationType` (string, required) — Relation type to filter on. Case-insensitive substring match (e.g. 'rival' matches 'rivalry').

### `get_recent_consequences`

Get the most recent world consequences (cross-story state changes — assassinations, faction shifts, public scandals, infrastructure damage). Use this when extending a chapter sequence to honour what's already happened in the world.

- `count` (int, optional) — Maximum number of recent entries to return. Default 10.

### `predict_behavior`

Predict a character's likely behavior in a given scene. Pulls from the character's psychology (core_fears, core_desires, coping_mechanisms, blind_spots), behavioral (decision_rules, escalation_ladder, contradictions, habits, breaking_points, stress_responses), and archetype influences. Returns dominant_state, likely_actions, dialogue_mode, concealing, physical_behavior, relationship_dynamics, stress_response, near_breaking_point. No LLM call — pure structural inference. Use this BEFORE drafting a scene to know how a character will read.

- `characterName` (string, required) — Character name — exact match against canon.
- `sceneLocation` (string, required) — Scene location.
- `othersPresent` (string, required) — Other characters present in the scene (comma-separated names).
- `beatGoal` (string, required) — What this beat is trying to accomplish narratively.
- `tensionLevel` (int, optional) — Tension level 1-10. Use 1-3 for low/calm, 4-6 for charged, 7-9 for crisis, 10 for breaking point.

## Quality

<sub>`QualityTools`</sub>

### `analyze_writing_quality`

Run the writing-quality heuristic pass over a book's chapters. Same checks the BookReviewService runs before its LLM Quorum: first-line strength, tension delta (flags 4+ low-tension beats in a row), paragraph-serves audit (paragraphs with no dialogue / sensory detail / action / number / capitalized noun), motif reuse (chapters that drop registered motifs), voice cadence Jaccard (chapter prose drifting from POV character's documented vocabulary). Returns findings list. No LLM calls.

- `bookId` (string, required) — Book id.

### `check_canon`

Sweep a strand's prose against the entire canon database (entities, locations, weapons, etc.) and queue each contradiction as a CANON-CONTRADICTION finding with an optional proposed fix. Returns the list of contradictions found. Use list_findings / apply_finding / set_finding_status to manage them afterward. Accepts strand id (GUID) or slug.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `proposeFixes` (bool, optional) — Set to true to also draft a suggested rewrite for each contradiction found.

### `check_semantic_fidelity`

Check the Semantic Fidelity Gap for a strand — Goodhart's Law in prose. Detects beats that score high on the Legion review metric but have drifted from the story's original meaning. Two checks: (1) Bible alignment: cosine similarity between each beat's prose and the strand's Seed/Synopsis — a high-scoring beat that no longer resembles the story it was born from is gaming the metric. (2) Intent alignment: cosine similarity between each beat's Synopsis (stated purpose) and its actual prose — drift here means the rewrite served reviewer patterns, not the beat's purpose. Embeds beats (drift-skipped), queries alignment, files SEMANTIC-DRIFT findings for violators, and returns the full report. Accepts strand id (GUID) or slug.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.

### `diagnose_strand`

Pre-flight structural analysis before running the review panel. Runs 12 targeted checks in parallel and returns Pass/Warn/Fail for each with evidence (a quote from the text) and a concrete one-action fix. Blocking failures (antagonist cost, protagonist behavior change, stakes embodiment, exposition density) mean the chapter is structurally unsound and will score in the 70s regardless of prose quality. Fix those first, then run review_strand. Accepts strand id (GUID) or slug. max_chars controls how much of the assembled strand text each check sees (default 40000 chars ≈ 10k tokens — covers most chapter-length strands; lower to reduce cost, raise for very long strands).

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `maxChars` (int, optional) — Max characters of assembled strand text each check reads. Default 40000 (~10k tokens). Lower to reduce cost; raise for very long strands (max practical: ~160000).

### `get_review_settings`

Return the current review-voting configuration: how many score-ballots and prose upgrades a sampled run casts, the persona panel depth, default reader count, max parallel ballot slots, judge provider, the comma-separated list of allowed providers, and whether the continuous auto-review monitor is enabled. Use update_review_settings to change any value.

- _(no parameters)_

### `get_review_summary`

Return the stored review summary for a strand — the synthesized aggregate of what readers liked, recurring gripes, and concrete improvement suggestions, written by the judge after the last review run. Includes average score, review count, and content hash so you can tell whether the summary is stale (strand was edited after the last run). Call review_strand to refresh. Accepts strand id (GUID) or slug.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.

### `list_strand_reviews`

List individual ballot reviews for a strand — one row per persona reader, showing persona name, provider, score, flow score (if study mode), improvements, and content hash. Use to inspect which personas scored low and what they said, or to compare how different providers voted. Results are sorted most-recent-first. Accepts strand id (GUID) or slug.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `contentHash` (string, optional) — Only return reviews from this content hash (i.e. one specific review run). Leave empty for all reviews.
- `limit` (int, optional) — Maximum rows to return. Default 50.

### `review_strand`

Run the sampled Legion review panel against a strand. STRUCTURAL PRE-FLIGHT runs first: if blocking failures are found (missing antagonist cost, passive protagonist, purely-stated stakes, >70% exposition), the review is blocked and returns the diagnosis instead of ballots — fix the structure first. Non-blocking warnings are always appended to the report. Stratified personas cast score-only ballots then the most informative are upgraded to full prose. Use the 'effort' tier to scale cost to importance. Returns: blocked (bool), mean_score, SD, CI, report_markdown (includes structural findings), synopsis. GOTCHA: do not edit beats while a review is running. Alias: also accepts strand id (GUID) for the strandIdOrSlug param.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `ballots` (int, optional) — Number of score-only ballots to cast. 0 = use the effort tier (if given) or the ReviewBallots setting (default 20). A non-zero value overrides the tier.
- `prose` (int, optional) — Number of full prose reviews to write (upgraded from ballots). 0 = use the effort tier (if given) else 0. A non-zero value overrides the tier.
- `skipDiagnosis` (bool, optional) — Set true to skip structural pre-flight and run ballots unconditionally. Use only when you have already reviewed and accepted the structural findings.
- `effort` (string, optional) — Cost tier (RFC 0009), scales calls + per-call model to importance: 'draft' = ~6 cheap-model ballots on claude+gemini, no diagnosis, NOT a gate; 'standard' = ~12 ballots + 2 prose, the >=82% standalone gate; 'deep' = ~37 ballots + 4 prose + full structural diagnosis, the >=85%/publish gate. Omit for the configured defaults.

### `update_review_settings`

Update review-voting settings. Pass only the fields you want to change — omit the rest. ballots: score-only ballot count (≥1). prose: full prose upgrades per run (≥0). panel: persona pool depth (≥1). readers: default reader count (≥1). max_concurrency: parallel ballot slots 1–50. judge_provider: provider that synthesizes the summary (claude|openai|gemini|deepseek). allowed_providers: comma-separated provider whitelist (e.g. 'claude,openai'); empty = all active providers allowed. review_auto_run_enabled: set false to disable the continuous auto-review monitor (you call reviews manually); set true to re-enable.

- `ballots` (int, optional) — Score-only ballot count (≥1). Omit to leave unchanged.
- `prose` (int, optional) — Full prose upgrades per run (≥0). Omit to leave unchanged.
- `panel` (int, optional) — Persona pool depth (≥1). Omit to leave unchanged.
- `readers` (int, optional) — Default reader count (≥1). Omit to leave unchanged.
- `maxConcurrency` (int, optional) — Parallel ballot slots, 1–50. Omit to leave unchanged.
- `judgeProvider` (string, optional) — Provider that synthesizes the summary. Omit to leave unchanged.
- `allowedProviders` (string, optional) — Comma-separated provider whitelist (e.g. 'claude,openai'). Empty string = all active. Omit to leave unchanged.
- `reviewAutoRunEnabled` (bool, optional) — False = disable the continuous auto-review monitor (call reviews manually). True = re-enable. Omit to leave unchanged.

### `validate_canon_text`

Scan arbitrary prose against every world rule (no city police, no Behemoth-as-alive, no 'the Shelf' district, no wedding-cake tier architecture, no Ferrogate-as-railroad, no metro/Meridian PD, no phi/Greek-letter confusion). Returns the list of matched violations with the surrounding context. Call this on a chapter draft BEFORE delivering it — catches rule slips Claude might miss.

- `text` (string, required) — The prose to scan. Pass an entire chapter or a single beat.

## Repository

<sub>`RepositoryTools`</sub>

### `create_repository`

Create a new repository (custom entity type). The slug is derived from the name (lowercased, hyphenated) and must be unique. Category is one of Characters/Organizations/Gear/World/Culture (defaults to World). Returns the created slug + route, or an error if the slug already exists.

- `name` (string, required) — Display name, e.g. 'Artifacts'. The slug is derived from this.
- `category` (string, optional) — Board category: Characters, Organizations, Gear, World, or Culture. Defaults to World.
- `icon` (string, optional) — Bootstrap-icon class for the tile, e.g. 'bi-box'. Optional.
- `description` (string, optional) — Optional description of what this repository holds.

### `list_repositories`

List all runtime-defined repositories (custom entity types): slug, name, category, route.

- _(no parameters)_

## Scene

<sub>`SceneTools`</sub>

### `assemble_scene_context`

X-Ray scene assembly (RFC 0002): given a Beat guid OR raw prose text, detect which entities are on screen (name/alias scan + embedding similarity + one-hop graph expansion) and return the roster plus a budgeted context block carrying each character's voice fields (vocabulary, cadence, subtext, under-pressure, intimacy register, example lines) and each place/object's gloss — the live memory block prose prompts should receive.

- `beatIdOrText` (string, required) — A Beat guid, or any prose text to assemble a scene roster for.
- `tokenBudget` (int, optional) — Token budget for the context block (default 2000).

### `get_character_wounds`

List a character's ACTIVE wounds from the WoundLedger (the literal body map): location, description, severity, source, healing status, and the residual effect prose must honor (favored limbs, reduced grip, exertion costs).

- `characterId` (string, required) — Character guid (e.g. Kyle = 019d6143-a648-7876-9688-0f6d38d70075).

### `log_wound`

Log a wound to the WoundLedger. Use when the story wounds a character so the body map stays factual: future prose prompts will carry it as an ACTIVE WOUND until its status moves to healed/scarred.

- `characterId` (string, required) — Character guid.
- `bodyLocation` (string, required) — Body location, side+region (e.g. 'left forearm', 'ribs, right side').
- `description` (string, required) — What happened, one sentence.
- `severity` (string, required) — minor | moderate | severe
- `residualEffect` (string, optional) — Residual effect the prose must honor (e.g. 'grip 90 percent; two-handed work hurts').
- `sourceStrandSlug` (string, optional) — Source strand slug, if known.
- `expectedHealingDays` (int, optional) — Expected healing days (default 14; AutoDoc shortens, never zeroes).

### `set_wound_status`

Update a wound's status: fresh | healing | healed | scarred. Scarred wounds stop appearing in prompts (graduate permanent marks to CharacterPhysicalMarks separately).

- `woundId` (long, required) — Wound id from the ledger.
- `status` (string, required) — fresh | healing | healed | scarred

## Species

<sub>`SpeciesTools`</sub>

### `get_species`

Get the full record for one species by canonical name (e.g. 'ai', 'elf', 'synthetic'). Returns name, label, description, examples, and sentient flag. Returns {error: not_found} when the name doesn't match.

- `name` (string, required) — Canonical species name, e.g. 'human' or 'elf'.

### `list_species`

List all species in the current universe. Returns canonical name (key used on Character.Species), label, and sentient flag. The five GLMZ values are: human, ai, elf, synthetic, unknown.

- _(no parameters)_

## Story

<sub>`StoryTools`</sub>

### `archive_book`

Archive a book: moves the book file from engine/data/books/ to engine/data/archives/books/. Non-destructive â€” the original chapters stay in place but the book record is removed from the active shelf. Requires the caller to retype the full book id as a confirmation token (matches the UI's type-the-guid modal). Returns ok:true on success or error:'confirmation_mismatch' / error:'not_found' otherwise.

- `id` (string, required) — Book id (32-char hex).
- `confirmId` (string, required) — Confirmation token â€” must equal the same full book id. Mismatched or missing values abort the archive.

### `get_book`

Load a book by id: full metadata, chapter id list (canonical order), state_at_end (open threads, character status carry-forward, canon changes).

- `id` (string, required) — Book id (32-char hex like 'eb91080d9c9c4f2b9b405fa5996bdea1').

### `get_book_outline`

Load a book's shared outline (the plot spine). Returns premise/arc_target/theme/structure, per-chapter outlines (title, short_synopsis, long_synopsis, key_beats, opens_threads, closes_threads, state_changes, pov_character), book-level threads (planted_in / pays_off_in), pending_adjustments (LLM-proposed neighbor edits). Approval status gates prose generation in the UI.

- `bookId` (string, required) — Book id.

### `get_chapter`

Load a single chapter by id: synopsis, full HTML body, persisted beats list (each with structure_role + text), participating characters. Use this to read existing prose before extending or revising.

- `id` (string, required) — Chapter id (32-char hex).

### `get_director_context`

Build the 'WHERE WE ARE' director context block for writing a specific chapter: PRIOR chapters' content, THIS chapter's outline, UPCOMING chapters' setup needs, plus open book-level threads. This is the highest-value writing-context tool â€” call it before drafting prose for any chapter that's part of a book.

- `bookId` (string, required) — Book id.
- `chapterId` (string, required) — Chapter id whose prose you're about to write.

### `list_books`

List every book on the shelf. Returns id, title, premise, chapter count, status, protagonists.

- _(no parameters)_

## Strand

<sub>`StrandTools`</sub>

### `append_strand_amendment`

Append an amendment to the strand's narrative spine. Amendments are append-only — they form an auditable change log of narrative decisions. Use when: changing a character's motivation after beats are written, retconning world rules, or noting why a section was expanded or cut.

- `idOrSlug` (string, required) — Strand id (GUID) or slug.
- `summary` (string, required) — One-line summary of the change.
- `body` (string, required) — Full amendment body (markdown). Explain what changed and why.

### `clear_beat_gap_after`

Clear an explicit gap-after-beat override. The audio engine falls back to the auto-computed silence from SceneType + terminator punctuation.

- `beatHandle` (string, required) — Beat Guid OR 'strand-guid.beat-guid' handle.

### `create_strand`

Create a new strand. Pass 'seed' to also generate a strand bible and planned beats immediately. Returns the new strand's id, slug, url, and (if bible was generated) the bible text.

- `title` (string, required) — Strand title. Required.
- `kind` (string, optional) — Strand kind: 'series' (groups stories), 'story' (root publishable work), or 'chapter' (sub-strand of a story, contains beats). Default 'story'.
- `synopsis` (string, optional) — Optional synopsis.
- `seed` (string, optional) — One-line generation seed. When provided, the strand bible and planned beats are created immediately after the strand row is inserted.
- `targetBeats` (int, optional) — Target beat count for the bible spine (only used when seed is provided). Default 12.
- `parentStrandIdOrSlug` (string, optional) — Optional parent strand Guid id (or slug). Empty = top-level.
- `code` (string, optional) — Optional short author-assigned reference code (e.g. 'ATTE', 'BCODA'). For series and story strands only — chapters never carry a code. Uppercased and stored as a unique lookup key. Leave empty to skip.

### `delete_beat`

Remove a beat from a strand. If the beat is not referenced by any other strand, the beat row + audio file are deleted entirely.

- `strandIdOrSlug` (string, required) — Strand Guid id or slug.
- `beatId` (string, required) — Beat Guid id to delete.

### `duplicate_strand`

Deep-duplicate a strand (and its sub-strand tree) into a fresh, independent copy. Every beat is cloned into a new row — prose and narration metadata are preserved, but audio, review scores, and the stale flag are reset. Editing the copy never affects the original. Accepts a Guid id OR a slug. Returns the new strand's id, slug, and writer URL.

- `idOrSlug` (string, required) — Source strand Guid id or slug.
- `newTitle` (string, required) — Title for the new duplicate. Required.

### `generate_strand_bible`

Generate (or regenerate) the strand bible for a strand. Uses the strand's Seed field (falls back to Synopsis then Title) plus the literary rules to produce a dry structural plan: logline, premise, register, characters, numbered beat spine, seeds & payoffs. Creates planned Beat rows from the spine when the strand has no beats yet. Returns the generated bible text.

- `idOrSlug` (string, required) — Strand Guid id or slug.
- `targetBeats` (int, optional) — Target number of beats in the spine. 0 = auto (use existing beat count or 12).

### `get_beat`

Get a single beat with every authoring field — prose, kind, IsChapterStart, BeatTitle, gap-after, tone/pace/facet metadata, position within strand, and the previous/next beat ids for relative insertion. Accepts a plain Beat Guid or the 'strand-guid.beat-guid' dotted handle the writer UI shows on the LLM bottom sheet.

- `beatHandle` (string, required) — Beat Guid OR the dotted 'strand-guid.beat-guid' handle.

### `get_score_history`

Return the score history for a strand as a time-series — every review run that produced a summary, with its mean score, SD, review count, and date. Use to track whether an edit moved the needle, or to compare pre/post-edit trajectories. Accepts strand id (GUID) or slug.

- `idOrSlug` (string, required) — Strand id (GUID) or slug.
- `limit` (int, optional) — Maximum history points to return (most recent first). Default 20.

### `get_strand`

Get a single strand with its beats in reading order. Accepts a Guid id OR a slug. Returns strand metadata + ordered beats (id, text, stale, has_audio, beat_title, synopsis).

- `idOrSlug` (string, required) — Strand Guid id or slug.

### `get_strand_bible`

Get the strand bible for a strand — the dry structural plan (logline, premise, register, characters, beat spine, seeds & payoffs). Returns the raw markdown text plus the parsed beat spine entries so you can see the planned arc at a glance. Returns has_bible=false when no bible exists yet.

- `idOrSlug` (string, required) — Strand Guid id or slug.

### `get_strand_spine`

Return the full narrative spine for a strand: bible, user stories, all amendments (in order), and the latest spine version pin (which records the content hashes and amendment count at the last docx export). Use this before writing prose to understand the narrative contract.

- `idOrSlug` (string, required) — Strand id (GUID) or slug.

### `insert_beat`

Insert a new beat into a strand. Pass an empty afterBeatId to insert at the top. Returns the new beat's id.

- `strandIdOrSlug` (string, required) — Strand Guid id or slug.
- `afterBeatId` (string, optional) — Beat Guid id to insert after, or empty for top-of-strand.
- `text` (string, optional) — Initial prose text for the new beat. May be empty.

### `join_beat`

Merge one beat into the previous one in the strand. Audio on the survivor is invalidated.

- `strandIdOrSlug` (string, required) — Strand Guid id or slug.
- `beatId` (string, required) — Beat Guid id to merge upward.

### `list_scores`

List strands with their latest review score, word count, and estimated page count (250 words/page). Optionally filter by kind ('book', 'chapter', 'episode', etc.) and/or status ('draft', 'canon', 'ready', 'archived'). Returns code, title, kind, status, score (null if unreviewed), words, pages, scored_on. Sorted by score descending (unscored strands last). Use this for a quick quality dashboard without running new reviews.

- `kind` (string, optional) — Optional kind filter (case-insensitive). E.g. 'book', 'chapter', 'novella'. Empty = all kinds.
- `status` (string, optional) — Optional status filter (case-insensitive). E.g. 'draft', 'canon', 'ready'. Empty = all statuses except archived.
- `includeArchived` (bool, optional) — Include archived strands. Default false.
- `limit` (int, optional) — Maximum rows to return. Default 200.

### `list_strands`

List strands. Use kind='story' to list all root stories (no parent); kind='chapter' for all sub-strands (contain beats). Returns a flat list of id, slug, title, kind, status, beat-count, stale-count.

- `kind` (string, optional) — Optional Kind filter — 'story' (root strands) or 'chapter' (sub-strands with beats). Case-insensitive equality match.
- `limit` (int, optional) — Maximum rows to return. Default 100.

### `narrate_strand`

Kick off TTS narration for every un-narrated beat in this strand (and its child strands recursively). Returns immediately — narration runs in the background; poll get_strand to observe progress. Returns an error response (without spawning anything) if TTS is not configured.

- `strandIdOrSlug` (string, required) — Strand Guid id or slug.

### `pin_strand_spine_version`

Create a spine version pin for the strand's current docx version. Records the SHA-256 hashes of the current bible and user stories, plus the amendment count, so future drift checks can tell when prose was written against a stale spine. Call this after every significant prose session or whenever the spine changes.

- `idOrSlug` (string, required) — Strand id (GUID) or slug.
- `notes` (string, optional) — Optional human note explaining what changed at this version.

### `prepare_audible`

Build an Audible AI-narration hand-off package for a strand. Produces three files in {publishDir}/{Title}/Audible/: (1) a narration-clean manuscript (.audible.txt) with markdown artifacts stripped and Φ expanded to 'QUANTA'; (2) a pronunciation guide (.pronunciation.md) listing entity names with plain-English respellings; (3) AUDIBLE_README.md with submission instructions. No API is called on Audible's side — the author uploads the .audible.txt via ACX/Audible publisher portal. Returns paths + word/term counts.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `withPhonetics` (bool, optional) — Run the optional LLM phonetics pass to fill in 'Say it as' respellings. Default true. Set false to skip and leave the column blank for manual completion.

### `publish_audiobook`

Render the whole strand as one continuous narration (no per-beat voice drift) and write the MP3 to the configured publish directory (defaults to Desktop). TTS engine: 'elevenlabs' (default, paid, highest fidelity), 'piper' (free/local, fastest), 'kokoro' (free/local, recommended), 'chatterbox' (free/local, most expressive). Returns the path of the written file, or null if the strand has no beat text.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `ttsEngine` (string, optional) — TTS engine: elevenlabs (default) | piper | kokoro | chatterbox.
- `robust` (bool, optional) — Set to true to retune this strand's frozen voice snapshot to Robust stability (1.0) before recording.

### `publish_docx`

Render a strand to a KDP-ready Word .docx and write it to the configured publish directory (defaults to Desktop). Returns the path of the written file. Use get_strand first to confirm the strand exists.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `author` (string, optional) — Author name to embed in the document properties. Optional.

### `rebeat_strand`

Re-segment a strand's beats to the codified beat doctrine via LLM re-segmentation. Dry-run by default (safe to call freely). Set apply=true to export a Markdown backup then replace the beats — only committed if the word-retention guard passes (prevents silent content loss). Returns old/new beat counts, retention %, guard result, and a note if it was blocked.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `apply` (bool, optional) — Set to true to commit the new segmentation. Default false = dry run.

### `reflow_strand`

Copy-edit a strand's prose in-place: adds missing '?' on questions, swaps 'says/said' → 'asks/asked' on question dialogue lines, and normalises paragraph/dialogue spacing. Dry-run by default — set apply=true to commit. Beats the model modified beyond those specific edits are rejected and left untouched. Returns changed/unchanged/rejected/errors counts plus per-beat diff previews.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.
- `apply` (bool, optional) — Set to true to write the edits to the DB. Default false = dry run.

### `set_beat_gap_after`

Set the silence (in ms) the audio engine inserts AFTER this beat, before the next. 0 = no silence (explicit override). Use ClearBeatGapAfter to revert to the auto-computed default from SceneType + terminator punctuation.

- `beatHandle` (string, required) — Beat Guid OR 'strand-guid.beat-guid' handle.
- `durationMs` (int, required) — Silence in milliseconds, 0..6000.

### `set_strand_bible`

Manually set or replace the strand bible text. Use when you want to hand-write the plan instead of generating it. The text is saved verbatim; beat spine parsing still applies for planned-beat creation. Pass an empty string to clear the bible.

- `idOrSlug` (string, required) — Strand Guid id or slug.
- `bibleText` (string, required) — Full bible markdown text to store. Empty string clears the bible.

### `set_strand_user_stories`

Set (replace) the user stories / acceptance criteria for a strand. Write this before starting prose — it defines what scenes, arcs, and voice moments must be present for the strand to reach ≥82% standalone and ≥85% cumulative story score.

- `idOrSlug` (string, required) — Strand id (GUID) or slug.
- `userStoriesText` (string, required) — Full user stories markdown. Will replace any existing content.

### `split_beat`

Split one beat into two at the nearest sentence boundary near its midpoint. Both halves lose their audio.

- `strandIdOrSlug` (string, required) — Strand Guid id or slug.
- `beatId` (string, required) — Beat Guid id to split.

### `update_beat_metadata`

Update a beat's metadata: BeatTitle, Synopsis, EmotionalTone, PaceHint, StructureRole, Act, SceneType, IsChapterStart, Kind. Pass empty strings to clear nullable fields. Does NOT touch prose or audio. Use to mark a beat as a chapter start, change its kind to quote/dedication/book-title, or set the tone the next re-record uses.

- `beatHandle` (string, required) — Beat Guid OR 'strand-guid.beat-guid' handle.
- `beatTitle` (string, optional) — Short label. When IsChapterStart=true this is the chapter heading; when Kind=quote this is the attribution.
- `synopsis` (string, optional) — One-line synopsis fed to LLM regenerations.
- `emotionalTone` (string, optional) — Emotional tone, e.g. 'quiet' / 'tense' / 'wry'.
- `paceHint` (string, optional) — Pace hint, e.g. 'flowing' / 'clipped' / 'staccato' / 'languorous'.
- `structureRole` (string, optional) — Structure role, e.g. 'inciting-incident' / 'rising-action' / 'climax'.
- `act` (int, optional) — Plot-act number 0–5. 0 = unassigned.
- `sceneType` (string, optional) — Scene type: scene | summary | transition | interstitial.
- `isChapterStart` (bool, optional) — True = this beat begins a new chapter / section. The writer renders a divider above it with BeatTitle as the heading.
- `kind` (string, optional) — Beat kind: prose (default) | book-title | dedication | quote. Free-form so new kinds add no schema cost.

### `update_beat_text`

Update one beat's prose. Recomputes the hash, marks the beat stale, and invalidates its audio. Beat.Text accepts inline markdown (**bold** / *italic* / __underline__ / ~~strike~~) and ElevenLabs-style tone tags ([WHISPERING] [GASP] [LAUGHS] [PAUSES] etc.) that render as emoji in the read view. Accepts a Beat Guid OR the 'strand-guid.beat-guid' handle.

- `beatHandle` (string, required) — Beat Guid OR 'strand-guid.beat-guid' handle.
- `text` (string, required) — New prose. Replaces the entire beat text. Markdown markers + tone-tag brackets are preserved verbatim in storage.

### `update_strand`

Update a strand's metadata fields. Pass only the fields you want to change — omit the rest to leave them unchanged. Editable fields: title, synopsis, kind, status, seed, code (StrandCode), voice_id. Status valid values: draft | ready | canon | archived. Code is uppercased and must be unique across non-null values — pass empty string to clear it. Does NOT touch beats or audio.

- `idOrSlug` (string, required) — Strand id (GUID) or slug.
- `title` (string, optional) — New title. Omit to leave unchanged.
- `synopsis` (string, optional) — Short synopsis. Omit to leave unchanged; pass empty string to clear.
- `kind` (string, optional) — Kind label: book | chapter | episode | novella | novel | strand | scene | saga | anthology. Omit to leave unchanged.
- `status` (string, optional) — Status: draft | ready | canon | archived. Omit to leave unchanged.
- `seed` (string, optional) — Generation seed (one-line premise). Omit to leave unchanged; pass empty string to clear.
- `code` (string, optional) — Short author reference code (e.g. 'ATTE'). Uppercased; pass empty string to clear. Omit to leave unchanged.
- `voiceId` (string, optional) — ElevenLabs or local TTS voice id. Omit to leave unchanged; pass empty string to clear.

## Universe

<sub>`UniverseTools`</sub>

### `current_universe`

Return the universe currently active for this session (slug + name).

- _(no parameters)_

### `list_universes`

List every registered universe (slug, name, theme) and which one is currently active. Call this first to discover universe slugs before switch_universe.

- _(no parameters)_

### `switch_universe`

Switch the active universe for this session by slug (e.g. 'glmz' or 'fantasy-steampunk'). All subsequent canon/story reads are scoped to it. Returns the new current universe or an error if the slug is unknown.

- `slug` (string, required) — Universe slug from list_universes, e.g. 'glmz'.

## Voice

<sub>`VoiceTools`</sub>

### `apply_voice_proposal`

Apply a proposed voice rule to the live voice store (the DB-backed rules the generator reads). Pass the entry id returned by harvest_voice or list_voice_proposals. The entry status changes to 'applied'. Returns ok=true on success, or error if the entry was not found or already resolved.

- `entryId` (string, required) — The voice change-log entry GUID to apply.

### `harvest_voice`

Distill voice rules from a winning strand (score ≥80) into proposed change-log entries. Nothing touches the live rule store until apply_voice_proposal is called. Pass force=true to harvest even if the strand scored below 80. Returns the list of proposed entries with their ids, rule targets, descriptions, and evidence.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug to harvest from.
- `force` (bool, optional) — Set to true to harvest even if the strand scored below 80%.

### `harvest_voice_all`

Distill voice rules from every strand scored ≥80%. Returns proposals grouped by strand slug. Nothing is written to the live rule store until apply_voice_proposal is called. Use list_voice_proposals to see all pending entries afterward.

- _(no parameters)_

### `list_voice_proposals`

List voice change-log entries filtered by status. Use status='proposed' to see pending proposals awaiting a decision. Each entry shows its id (use for apply/reject), rule target, description, evidence, and source strand.

- `status` (string, optional) — Filter by status: 'proposed' | 'applied' | 'rejected' | 'observed'. Default 'proposed'.

### `reject_voice_proposal`

Reject a proposed voice rule. The entry stays in the audit trail (status = 'rejected') so the decision is traceable. Pass the entry id returned by harvest_voice or list_voice_proposals.

- `entryId` (string, required) — The voice change-log entry GUID to reject.

## World Entity Crud

<sub>`WorldEntityCrudTools`</sub>

### `create_automaton`

Create or update an automaton (drone, security bot, Iowan Behemoth, agricultural machine) in canon. Automata are machines, NOT synthetic life. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Automaton name. Required.
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer name.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing automaton id to update.

### `create_consumer_good`

Create or update a consumer good (food, drinks, household items, branded products) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Consumer good name. Required.
- `productName` (string, optional) — Product name if different from name.
- `category` (string, optional) — Category (e.g. 'food', 'beverage', 'household', 'luxury').
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer or brand.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing consumer good id to update.

### `create_document`

Create or update a worldbuilding document in canon. Documents hold long-form canon text (lore articles, guides, in-world publications). Pass empty id to create new; pass an existing id to update.

- `fileName` (string, required) — Document file name (slug, e.g. 'network_operators_guide'). Required.
- `title` (string, required) — Document title. Required.
- `category` (string, optional) — Category (e.g. 'lore', 'technical', 'in-world-publication', 'history').
- `body` (string, optional) — Full prose body of the document.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing document id to update.

### `create_subsidiary`

Create or update a subsidiary (child/holding company of a larger CorpoNation) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Subsidiary name. Required.
- `parentCorponation` (string, optional) — Parent CorpoNation name.
- `description` (string, optional) — Prose description.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing subsidiary id to update.

### `create_transportation`

Create or update a transportation entry (vehicle, transit line, Pulse station, individual transport) in canon. Pass empty id to create new; pass an existing id to update.

- `name` (string, required) — Transportation name. Required.
- `category` (string, optional) — Category (e.g. 'motorcycle', 'rail', 'air', 'Pulse', 'water').
- `description` (string, optional) — Prose description.
- `manufacturer` (string, optional) — Manufacturer name.
- `tags` (string, optional) — Comma-separated tags.
- `id` (string, optional) — Optional existing transportation id to update.

## World Modelling

<sub>`WorldModellingTools`</sub>

### `add_prose_lesson`

Add an editorial prose lesson — an author ruling that reviewers must respect. Lessons are injected into every future review ballot prompt so the panel does not penalise beats the author has already decided are doing their job in the sequence. scope: 'global' applies to all strands; 'strand:<slug>' to one strand; 'beat:<guid>' to one beat. kind: score-vs-function | delight | voice | pacing | continuity | other.

- `scope` (string, required) — Scope: 'global', 'strand:<slug>', or 'beat:<guid>'
- `kind` (string, required) — Kind: score-vs-function | delight | voice | pacing | continuity | other
- `text` (string, required) — The ruling text — what reviewers must respect.

### `check_behavior`

LLM-checks prose text against a character's behavioral rules (decision_rules, escalation_ladder, contradictions, habits, breaking_points). Returns a JSON array of violations — empty array means the prose is consistent.

- `beatText` (string, required) — Beat prose text to check
- `characterId` (string, required) — Character entity GUID

### `check_gear_carry`

Scans prose text for gear usage verbs (drew, fired, aimed…) and checks whether the subject character has a carry/wield edge for each named prop. Returns a JSON array of violations — empty array means clean.

- `beatText` (string, required) — Beat prose text to scan
- `characterId` (string, required) — Character entity GUID (the POV/subject character)
- `storyTime` (string, optional) — Story-date for edge validation (ISO 8601). Omit to use all-time carry edges.

### `check_prose`

Runs the deterministic prose pattern linter on text. Detects: clichés (chrome gleam, heart hammered…), pseudo-profound constructs (in that moment, it hit him that…), on-the-nose interiority, italicised dialogue, and sentences exceeding 25 words. Returns a JSON array of violations.

- `text` (string, required) — Prose text to lint

### `check_timeline`

Deterministic timeline-consistency check for a strand (RFC 0009 §5). Zero LLM calls. Detects two violation classes: (1) dead-character-acting — an entity whose status is 'dead'/'deceased' appears in a later beat; (2) wound-regression — a healed/none event precedes the injury-onset event for the same condition. Returns a list of findings with kind, entityId, entityName, beatNumber, detail, severity. Returns an empty array when no events are in the ledger for this strand — never throws.

- `slugOrId` (string, required) — Strand slug or GUID

### `clear_entity_stale`

Clears the EntityStale flag on a beat after the author has reviewed it and confirmed the prose is still consistent with current entity canon.

- `beatId` (string, required) — Beat GUID

### `get_ambient_palette`

Returns the sensory detail palette for a character's carried gear. Inject the result into a beat prompt to ground sensory texture in what the character actually carries.

- `characterId` (string, required) — Character entity GUID
- `asOfDate` (string, optional) — Story-date filter (ISO 8601). Omit for current carry edges.

### `get_character_loadout`

Returns a character's weapon loadout from their signature_gear list, with ammo types for each weapon. Use for scene continuity and logistics.

- `characterId` (string, required) — Character entity GUID
- `asOfDate` (string, optional) — Story-date filter (ISO 8601). Omit for all-time loadout.

### `get_entity_tree`

Returns a hierarchical relationship tree rooted at an entity, traversing the Edge graph up to maxDepth hops. Formatted as a prompt-injectable context block. Use before generation to understand who/what an entity is connected to.

- `entityId` (string, required) — Entity GUID
- `maxDepth` (int, optional) — Maximum hop depth (default 3)
- `relTypes` (string, optional) — Comma-separated relation types to follow, e.g. 'carries,wields,member_of'. Omit for all.

### `get_weapon_network`

Returns the ammo network for a weapon: its ammunition types + sibling weapons that share at least one chambering. Use for continuity (scavenging compatible rounds, borrowing ammo between characters) and world enrichment.

- `weaponId` (string, required) — Weapon entity GUID

### `get_world_state_at_beat`

Returns the world-state snapshot at a given beat: all entity aspect states (wounds, location, status…) + active relationships. Use to inject consistent 'what is true right now' context before writing a beat.

- `beatId` (string, required) — Beat GUID
- `storyTime` (string, optional) — Story-world timestamp override, ISO 8601. Inferred from beat events when omitted.

### `list_entity_stale_beats`

Returns every beat flagged EntityStale — i.e. a canon entity mentioned in the beat was updated after the beat was written. Grouped by strand. Review each beat and call clear_entity_stale when satisfied.

- _(no parameters)_

### `list_prose_lessons`

List prose lessons from the editorial memory store. When scope is omitted, returns all lessons across all scopes. When scope is provided, returns only lessons whose scope starts with that prefix (e.g. 'global' for all global lessons, 'strand:my-slug' for a specific strand).

- `scope` (string, optional) — Optional scope filter prefix (e.g. 'global', 'strand:my-slug'). Omit for all.

### `scan_strand_violations`

Run the prose pattern guard over every beat in a strand and file violations as Findings. This is the strand-wide sweep equivalent of check_prose — use it after importing or rewriting a strand to catch all clichés, pseudo-profound constructs, on-the-nose interiority, and italicised dialogue in one pass. Returns a per-beat summary of violations found.

- `strandIdOrSlug` (string, required) — Strand id (GUID) or slug.

### `validate_beat`

Run the full post-beat validation battery on a saved beat: prose pattern guard (clichés, pseudo-profound, on-the-nose, italicised dialogue) + gear carry check (character uses gear without a carry edge) + optional behavior invariant check (LLM — one call per character). All violations are filed as Findings and returned. Accepts an optional comma-separated list of character GUIDs; when omitted, characters are derived from the beat's indexed entity mentions.

- `beatId` (string, required) — Beat GUID.
- `characterIds` (string, optional) — Comma-separated character GUIDs to check gear/behavior for. Omit to auto-detect from entity mentions.
- `checkBehavior` (bool, optional) — Run the LLM-based behavior invariant check (one LLM call per character). Default false.
- `storyTime` (string, optional) — Story-date for gear edge validation (ISO 8601). Omit for all-time carry edges.

## Writing

<sub>`WritingTools`</sub>

### `add_chapter_to_book`

Append an existing chapter id to a book's chapter_ids list. Use when a chapter and a book were created independently. Sets the chapter's BookId and Number to match. Idempotent — re-running with the same chapter moves it to the requested position.

- `bookId` (string, required) — Book id.
- `chapterId` (string, required) — Chapter id to attach.
- `number` (int, optional) — Chapter position (1-indexed). 0 = append.

### `create_book`

Create or upsert a Book record. Pass an empty id to create a new book (a v7 GUID is assigned and returned); pass a known id to update an existing book. Protagonists are resolved by name against the character canon. status defaults to 'drafting'. Returns the persisted Book including assigned id.

- `title` (string, required) — Book title. Required.
- `premise` (string, required) — One-paragraph premise — feeds the chapter director when extending.
- `protagonists` (string, required) — Comma-separated protagonist names — first name is the lead. Resolved against character canon.
- `arcTarget` (string, optional) — What this book is *about* and where it lands. Used as the extension target. Optional.
- `tagline` (string, optional) — Optional tagline shown beneath the title on the bookshelf card.
- `status` (string, optional) — Book status: drafting | preserved | published | archived. Defaults to 'drafting'.
- `id` (string, optional) — Optional book id to update an existing record. Empty creates a new book.

### `create_chapter`

Create or upsert a Chapter record. Pass an empty id to create new; pass a known id to update. Pass a non-empty bookId to attach the chapter to a book and append it to the book's chapter_ids in the supplied order position (1-indexed). HTML is the rendered body — pass the prose directly. Returns the persisted Chapter including assigned id.

- `title` (string, required) — Chapter title. Required.
- `synopsis` (string, required) — One-paragraph chapter synopsis. Required.
- `html` (string, required) — Full chapter prose. HTML or plain text — plain text is wrapped in <p> tags on render.
- `characters` (string, required) — Comma-separated character names participating in this chapter.
- `bookId` (string, optional) — Parent book id. Empty leaves the chapter orphaned.
- `number` (int, optional) — Chapter number within the book (1-indexed). Ignored when bookId is empty.
- `status` (string, optional) — Chapter status: draft | revising | reviewed | published. Defaults to 'draft'.
- `id` (string, optional) — Optional chapter id to update an existing record. Empty creates new.

