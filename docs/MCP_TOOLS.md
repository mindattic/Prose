# Prose MCP Tools

> **GENERATED — do not hand-edit.** Produced by `ToolDocGenerator` from the
> `[McpServerTool]` + `[Description]` attributes in `v3/Prose.Mcp/Tools*.cs`,
> the same source the MCP host registers via `WithToolsFromAssembly()`. To refresh:
> 
> ```powershell
> dotnet run --project v3/Prose.Mcp -- --export-tools docs/MCP_TOOLS.md
> ```
>
> All tools are MCP-prefixed `mcp__prose__<name>` by the client. Most return a
> JSON string; the canon is the SQL database, scoped to the active Universe.

**273 tools** across **43 tool families.**

## Families

| Family | Tools |
| --- | --- |
| [Beat Event List](#beat-event-list) | 3 |
| [Beat Lens](#beat-lens) | 3 |
| [Bible](#bible) | 3 |
| [Book Audit](#book-audit) | 2 |
| [Book Health](#book-health) | 1 |
| [Book Logic](#book-logic) | 2 |
| [Canon](#canon) | 9 |
| [Canon Doc](#canon-doc) | 7 |
| [Chekhov Audit](#chekhov-audit) | 1 |
| [Combat](#combat) | 1 |
| [Config](#config) | 14 |
| [Context](#context) | 5 |
| [Continuity](#continuity) | 2 |
| [Core Entity Crud](#core-entity-crud) | 5 |
| [Data Integrity](#data-integrity) | 4 |
| [Edit Session](#edit-session) | 6 |
| [Encyclopedia](#encyclopedia) | 35 |
| [Entity Context](#entity-context) | 4 |
| [Findings](#findings) | 5 |
| [Gear Entity Crud](#gear-entity-crud) | 7 |
| [Glossary](#glossary) | 4 |
| [Lore Triple](#lore-triple) | 7 |
| [Narrative Science](#narrative-science) | 5 |
| [Node](#node) | 38 |
| [Noun Consistency](#noun-consistency) | 3 |
| [Planning](#planning) | 6 |
| [Plant Payoff](#plant-payoff) | 6 |
| [Quality](#quality) | 12 |
| [Reader Qa](#reader-qa) | 3 |
| [Repository](#repository) | 2 |
| [Scene](#scene) | 4 |
| [Species](#species) | 2 |
| [Story](#story) | 6 |
| [Story Scope](#story-scope) | 3 |
| [Survey](#survey) | 7 |
| [Swain](#swain) | 3 |
| [Universe](#universe) | 5 |
| [Verification](#verification) | 5 |
| [Voice](#voice) | 6 |
| [Workflow Monitor](#workflow-monitor) | 3 |
| [World Entity Crud](#world-entity-crud) | 5 |
| [World Modelling](#world-modelling) | 16 |
| [Writing](#writing) | 3 |

## Beat Event List

<sub>`BeatEventListTools`</sub>

### `export_event_list`

Export the current per-beat plot-event list for a node to {CODE}-Events.txt in the node's publish-export folder (same layout as description.txt / {CODE}-dcm-viz.htm — not docs/nodes; deliberately .txt, not .md, so it's never picked up by sync_markdown_files / DCM). No LLM call — reads current DB state only.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `generate_event_list`

Generate/refresh the per-beat plot-event list (Beat.EventSummary) for a node — terse, present-tense, name-anchored 'what happened' lines (e.g. 'Thieves steal Relic.'), hash-gated so unchanged beats cost nothing on re-run. Distinct from Description (authorial-intent register — 'why this beat exists'). Accepts node id (GUID) or slug. force=true regenerates every beat's line regardless of cache.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `force` (bool, optional) — Regenerate every beat's line even if its TextHash hasn't changed.

### `get_event_list`

Return the current per-beat plot-event list for a node as ordered structured data — one entry per enabled beat with its SortKey, title, POV, and EventSummary line. Reads DB state only, no LLM call, no disk write — the fast, in-session way to read a whole book's plot flow without opening the exported {CODE}-Events.txt or reading the raw prose. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

## Beat Lens

<sub>`BeatLensTools`</sub>

### `affect_check`

Check whether each character's EMOTION believably DRIVES their ACTION. Flags actions that ignore what just happened, unmotivated calm, feelings named but not enacted. Files advisory Findings; returns score 0-100 + issues. Arg: node GUID or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `causality_check`

Check a node's CAUSE-AND-EFFECT: do beats follow by therefore/but rather than 'and then'? Flags episodic transitions, effects without setup, actions against established motive, implausible reactions. Files advisory Findings; returns score 0-100 + issues. Arg: node GUID or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `interpersonal_check`

Check INTERPERSONAL DYNAMICS — the 90+ relational lever. Are exchanges doing real relational work on BOTH channels (verbal subtext + non-verbal body/gesture)? Flags info-only dead exchanges, missing non-verbal channel, on-the-nose emotion-naming, bonds that don't change. Files advisory Findings; returns score 0-100 + issues. Arg: node GUID or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

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

## Book Audit

<sub>`BookAuditTools`</sub>

### `audit_book_commandments`

Audit a node against all 7 commandments — gateway (for first/standalone books) or sequel (for books with a PreviousNodeId set). Auto-detected: null PreviousNodeId → gateway commandments; set → sequel commandments. Each commandment check returns status (pass/warn/fail), specific evidence from the prose, and a concrete one-sentence fix when not passing. Returns gateway_ready (no failing checks), blocking_count (failures), advisory_count (warnings), plus plant_count and orphaned_plants from the PlantPayoff registry (relevant for the 'reward re-reading' commandment). Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `set_previous_book`

Link a node to its predecessor, switching it from gateway mode to sequel mode. When previous_node_id_or_slug is provided, Node.PreviousNodeId is set — the book will use sequel commandments in audits and beat-writing context. To clear (revert to gateway mode), pass clear=true. Accepts both node arguments as id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — The node to update — id (GUID) or slug.
- `previousNodeIdOrSlug` (string, optional) — The preceding node — id (GUID) or slug. Omit or pass null to clear.
- `clear` (bool, optional) — Set true to clear PreviousNodeId (revert to gateway mode).

## Book Health

<sub>`BookHealthTools`</sub>

### `book_health`

Run the full book-health battery and return one Structural Integrity Index (SII, 0-100) built from a fixed, documented formula over open Findings + a small number of deterministic rate metrics (Swain scene/sequel compliance, CraftChecklist DELIGHT-landing rate, StoryScope readiness) — NOT an LLM opinion vote (SS-A44). Every point of the score traces to a specific Findings category or rate metric in the response; there is no bare number. tier=free (default) runs only deterministic/near-zero-cost checks (plant-audit, prose-check, noun-consistency, timeline-check, beat-verification, outline-coordination). tier=deep adds one-LLM-call-per-check whole-node audits (examine-emotion, book-audit, diagnose-book, check-fidelity, logic-sweep, craft-checklist, check-canon, altitude-audit, reader-qa comprehension). tier=full adds the heaviest multi-call audits (storyscope-audit, swain-audit, chekhov-audit) — cost scales with book length. The SII itself is always computed from whatever is currently in the Findings table regardless of tier — a free-tier run still reflects a prior full-tier run's findings, it just won't refresh them.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug — a book or a lone chapter.
- `tier` (string, optional) — free | deep | full
- `model` (string, optional) — Optional model override for the deep/full tier's LLM calls.

## Book Logic

<sub>`BookLogicTools`</sub>

### `logic_sweep`

Run docs/LOGIC.md's six-dimension logic sweep on a node: causality chain, knowledge states, timeline, plant/payoff (two-way), orphan references, bible agreement. This is a single LLM call per dimension over the whole node's prose — a coarse, automatable gate, NOT a replacement for the full /logic-sweep Claude Code skill on a large book (that skill splits the book across range-scoped subagents, verifies quotes, and does a separate fix + re-verify pass). Findings persist to the Findings table and auto-heal on re-run. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `write_outline`

Generate a beat-by-beat narrative outline (act-grouped) for a node. For a real logic check (causality/knowledge-states/timeline/plant-payoff/orphan-refs/bible-agreement), call logic_sweep instead. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

## Canon

<sub>`CanonTools`</sub>

### `get_character`

Load a character's full canon record by name: identity, psychology (core_fears, core_desires, coping_mechanisms, blind_spots, secret), behavioral (decision_rules, escalation_ladder, contradictions, habits, breaking_points, stress_responses), speech_patterns (vocabulary, cadence, verbal_tics, example_lines), augmentations, story_hooks. This is the primary source for voice when writing a POV chapter.

- `name` (string, required) — Exact name of the character (e.g. 'Kyle Ellen Corbin' or 'Sasha Võ').

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

List every character in canon. Returns name + role + status for each. Cheap — call this first when you need to know who exists.

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

## Canon Doc

<sub>`CanonDocTools`</sub>

### `generate_canon_md`

Regenerate a world-canon .md file from its DB sections. Writes the assembled content to disk and updates the LastChecksum so codex doctor validates the file as current. Run this after every set_canon_section call.

- `documentType` (string, required) — Document type — call list_canon_document_types for the current valid values.
- `universeSlug` (string, optional) — Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.

### `get_canon_document`

Get a full world-canon document assembled from its DB sections. Call list_canon_document_types for the current valid documentType values. universeSlug: glmz | scry/fantasy/caul (or a universe GUID). Returns the complete assembled markdown — same content that generate_canon_md would write to disk.

- `documentType` (string, required) — Document type — call list_canon_document_types for the current valid values.
- `universeSlug` (string, optional) — Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.

### `list_book_bible_sections`

List all NodeBibleSections for a book node. Shows section types, content lengths, and last-updated timestamps. Use this to see which typed sections exist before calling set_book_bible_section.

- `nodeIdOrSlug` (string, required) — Node id (GUID), slug, or NodeCode.

### `list_canon_document_types`

List every registered canon DocumentType (e.g. WorldBible, CraftGuide) — the current valid values for the documentType parameter on every other tool in this file. Data-driven (CanonDocumentTypes table), so this grows as new document types are migrated; don't rely on a hardcoded list from memory.

- _(no parameters)_

### `list_canon_sections`

List all sections in a world-canon document with their keys, titles, sort order, and last-updated times. Use this to find the sectionKey you need before calling set_canon_section.

- `documentType` (string, required) — Document type — call list_canon_document_types for the current valid values.
- `universeSlug` (string, optional) — Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.

### `set_book_bible_section`

Update or create a structured section in a book's node bible (NodeBibleSections table). sectionType: Full | ArcSummary | Characters | VoiceRegister | NarrativeLocks | BeatSpine. Use 'Full' to replace the entire hand-authored bible blob; use typed sections to maintain structured per-category content. The docs/nodes/<CODE>.md artifact and the MarkdownFiles sync (what DocContextService reads) are regenerated automatically as part of this call.

- `nodeIdOrSlug` (string, required) — Node id (GUID), slug, or NodeCode.
- `sectionType` (string, required) — Section type: Full, ArcSummary, Characters, VoiceRegister, NarrativeLocks, or BeatSpine.
- `content` (string, required) — Section content (markdown). Replaces any existing content for this sectionType.

### `set_canon_section`

Update or create a section in a world-canon document. This is the ONLY way to edit world canon — do NOT hand-edit the generated .md files under docs/ (BIBLE.md, WORLD.md, FRANCHISE.md, CRAFT.md, DELIGHT.md, docs/universes/*.md). The .md artifact and the MarkdownFiles sync (what DocContextService reads at generation time) are regenerated automatically as part of this call — no follow-up call needed. To find available sectionKeys, call list_canon_sections first.

- `documentType` (string, required) — Document type — call list_canon_document_types for the current valid values.
- `sectionKey` (string, required) — Stable section key — e.g. 'SS-LAW-1', 'SS-§3', 'preamble'. Use list_canon_sections to find existing keys.
- `content` (string, required) — Full section content (markdown). Replaces the existing content for this key.
- `universeSlug` (string, optional) — Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.
- `sectionTitle` (string, optional) — Optional: human-readable section title (the ## heading text). Leave blank to keep the existing title.

## Chekhov Audit

<sub>`ChekhovAuditTools`</sub>

### `chekhov_audit`

Chekhov's Gun audit for a story node: extract all concrete props, environmental anchors, sensory details, and recurring character-specific physical traits, then test whether each earns its place. Verdicts: EARNS_IT (each appearance serves a distinct purpose), ORPHANED (appears once with no payoff), DECORATION (repeated without new narrative function), ATMOSPHERE (one-time environmental texture with no implied promise), FLAG (uncertain — human review). Run before trimming any prose detail; before cutting, confirm the prop has no payoff in a later beat. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

## Combat

<sub>`CombatTools`</sub>

### `draft_combat_scene`

Generate an action sequence using the Prose combat writer. Respects participants' canon loadouts, current injuries/stress, and tracks ammo/grenade counts across beats. Tone shapes word choice and pacing — pick deliberately. Always pass preceding_context (last 1–3 paragraphs leading into the fight) so the prose transitions cleanly. sides_json must be a JSON array of side objects; see parameter description for shape. Returns the generated beats plus the full stitched text. Run validate_canon_text on the result before staging it into a chapter.

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

### `add_context_doc`

Pin a canon .md doc so it is always included in every beat prompt, regardless of LRU tier. Identify the doc by relative path fragment (e.g. 'ICFI', 'BIBLE', 'wound') or its GUID. The override lasts 24 h or until remove_context_doc / clear_context is called. Optionally scope to a single book with nodeSlug so only that book's beats include it.

- `doc` (string, required) — Relative path fragment or GUID of the markdown doc to pin.
- `nodeSlug` (string, optional) — Optional book slug to scope the pin (e.g. 'icfi'). Omit for session-global.

### `clear_context`

Clear ALL active context overrides for this session (both pins and excludes). Pass nodeSlug to clear only overrides scoped to that book; omit for session-wide clear.

- `nodeSlug` (string, optional) — Optional book slug to clear only overrides for that node. Omit for full session clear.

### `doc_context_prepare`

Prepare the Doc Context Stack — the rotating cast of pertinent canon .md docs for a topic/scene. Returns one budgeted block plus the resident docs (tier + why each loaded). Pass nodeCode (e.g. 'BCODA') to include that book's bible + its one register; pass text (scene/goal/conversation) to trigger topic docs by keyword and semantic embedding. This is how you load only the few docs that matter now instead of dumping hundreds.

- `text` (string, required) — Scene/goal/conversation text to trigger topic docs against.
- `nodeCode` (string, optional) — Optional node CODE (e.g. 'BCODA') to also load that book's bible + register.
- `budget` (int, optional) — Token budget for the assembled block. Default 2000.

### `doc_context_status`

Inspect the current Doc Context Stack working set (the docs resident in the rotating cast) for a node context, without changing it. Returns each doc's tier, why it loaded, and its score.

- `nodeCode` (string, optional) — Optional node CODE whose working set to inspect.

### `exclude_context_doc`

Exclude a canon .md doc from the DocContextStack so it is never injected even if it would normally match. Identify the doc by relative path fragment or GUID. The override lasts 24 h or until remove_context_doc / clear_context is called.

- `doc` (string, required) — Relative path fragment or GUID of the markdown doc to exclude.
- `nodeSlug` (string, optional) — Optional book slug to scope the exclusion. Omit for session-global.

### `get_context_status`

Show all active context overrides (pins and excludes) for this session. Includes the doc path, action, scope (global or node), and expiry time.

- _(no parameters)_

### `get_cost_report`

Show the running token cost tally for the current MCP server session. Returns call count, input/output token estimates, and USD cost broken down by model. Token counts are estimated from text length (chars / 4) since the Legion transport does not expose Anthropic usage objects. Pass reset=true to clear the ledger.

- `reset` (bool, optional) — If true, clear the ledger after reporting. Default false.

### `get_liberty_report`

Show the liberty analysis (Rule of Cool) for a single beat or all beats in a book. A 'liberty' is any creative departure from the beat goal or entity roster: entity_invention (name not in DB), tech_departure (GLMZ physics violated), or creative_departure (plot beyond the beat goal). Each liberty is scored CoolFactor 0–10: ≥8 → CANON-ADDITION-CANDIDATE finding, 5–7 → LIBERTY-CONSIDER advisory, ≤4 entity invention → LIBERTY-WARNING. Reports are written automatically after each beat write; this tool reads them.

- `beatId` (string, optional) — Beat GUID to retrieve the report for that specific beat.
- `slug` (string, optional) — Book slug (e.g. 'icfi') to retrieve all reports for that book, newest first.

### `get_markdown_file`

Get the content of a tracked markdown file from the database. Pass asOf (ISO 8601 UTC) to retrieve a historical version from the temporal table. relativePath examples: 'CLAUDE.md', 'docs/BIBLE.md', 'feedback_sequential_node_writing.md'

- `relativePath` (string, required) — Relative path key, e.g. 'CLAUDE.md' or 'docs/AMENDMENTS.md'.
- `asOf` (string, optional) — Optional ISO 8601 UTC datetime to retrieve the version current at that moment.

### `list_markdown_files`

List all markdown files tracked in the database (project rules, Codex docs, Claude Code memory). Returns category, relativePath, contentHash, and lastSyncedAt for each file.

- _(no parameters)_

### `recall_markdown_files`

Recall (call up) the select few tracked markdown files relevant to a keyword, straight from the database — instead of materializing hundreds of tiny .md files on disk. Substring-matches the keyword (case-insensitive) against relativePath, fileName, and category; set includeContent=true to also search inside file bodies. Returns each match's full content so the caller can read only what it needs. Examples: 'steppin', 'wound ledger', 'schism'.

- `keyword` (string, required) — Keyword to match against path/name/category (and body when includeContent=true).
- `includeContent` (bool, optional) — Also search inside file bodies, not just names. Default false.

### `remove_context_doc`

Remove a specific pin or exclude override for a canon doc. Pass the same doc path/GUID and optional nodeSlug used when the override was created.

- `doc` (string, required) — Relative path fragment or GUID of the markdown doc whose override to remove.
- `nodeSlug` (string, optional) — Optional book slug the override was scoped to.

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

List the registered motifs for a book — recurring objects, phrases, gestures, sensory threads. Mention these in the chapter you're writing where natural; the review pipeline flags chapters that drop the whole inventory.

- `bookId` (string, required) — Book id.

### `get_neighbors`

Get a graph node's neighbors (relationships) up to N hops. Use this to walk from a known entity to entities related by canon — alliances, rivalries, family, mentor links, location ownership.

- `nodeId` (string, required) — Node id (use search_semantic or list_characters to find the id).
- `hops` (int, optional) — Hops to traverse. 1 = direct neighbors. Default 1.

### `plant_motif`

Plant a new motif in a book's inventory. Idempotent by name (re-planting with a longer description merges). The user normally accepts these from the Motifs panel in the UI; this tool exposes the same write so chat-side authoring can register them too.

- `bookId` (string, required) — Book id.
- `name` (string, required) — Motif name, e.g. 'brick-wall notebook' or 'the door is unlocked'.
- `description` (string, required) — Short description of what this motif means and where it lands.
- `kind` (string, required) — MotifKind: Object, Phrase, Gesture, Sensory, Ritual.
- `introducedInChapterId` (string, required) — Chapter id where this motif is being introduced.

### `propose_motifs`

Scan a node's actual written prose for motif candidates — italicized phrases that recur, or capitalized named objects (not already characters/places) that repeat 3+ times. Returns proposals for review; nothing is written automatically. Pass a chapter-level node for one chapter's beats, or a book-level node to aggregate every chapter's beats. Plant any you want to keep via plant_motif.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug/code to scan.

### `search_semantic`

Search the world graph by theme, not by name. TF-IDF cosine similarity across every entity description. Use this to surface entities that are *thematically relevant* to what you're about to write — e.g. searching 'corporate betrayal under-table contract' might return Sable's backstory, the Lotus Syndicate, the Ferrogate enforcement arm. Returns ranked id+name+type+score.

- `query` (string, required) — Free-text query — describe the theme/scene/concept.
- `topK` (int, optional) — Number of top hits to return. Default 8.

## Continuity

<sub>`ContinuityTools`</sub>

### `find_contradictions`

Find contradictions in a chapter against established canon. Pulls the characters from the chapter's `characters` field, plus the book's state_at_end and all prior chapters' synopses, builds a canon-context bundle, and dispatches a Legion Quorum vote with a contradiction-finding rubric (EPISTEMIC / TEMPORAL / CAPABILITY / CANON). Returns a JSON report with findings, citations, severity, and suggested fixes. Exit-code-equivalent convention: ok=true means no contradictions; ok=false means findings exist.

- `chapterId` (string, required) — Chapter id (32-char hex), resolved from the SQL canon (IChapterRepository) — the pre-SS-A45 engine/data/chapters/<id>/chapter.json disk layout was retired 2026-05-08.
- `quorum` (string, optional) — Quorum requirement for the contradiction vote: plurality | simplemajority | twothirds | unanimous. Default plurality (most permissive — surfaces every voter's concerns).
- `maxTokens` (int, optional) — Max tokens per voter response. Default 4096. Larger values produce more thorough reports but cost more.
- `maxContextChars` (int, optional) — Hard cap on canon-context characters before the draft text is appended. Default 80000. Lower this if hitting provider context limits.

### `find_contradictions_book`

Find contradictions across an entire book by running a pairwise sweep — every chapter is graded against the FULL PROSE of every OTHER chapter (forward AND backward). Catches things a single-chapter check misses: a character who dies in chapter 3 but speaks in chapter 5, a character revealed left-handed in chapter 6 catching a ball right-handed in chapter 2, a stated age that drifts between chapters, etc. Cross-chapter findings are consolidated so the same contradiction surfaces once with all chapter numbers attached. Expensive — dispatches N Legion votes per book. Use synopsisOnly=true for cheaper triage that skips prose-level facts. Returns a JSON report with per-chapter findings and a consolidated cross-book finding list. Exit-code-equivalent convention: ok=true means no contradictions; ok=false means findings exist.

- `bookId` (string, required) — Book id (32-char hex), resolved from the SQL canon (IBookRepository) with its chapters — the pre-SS-A45 engine/data/books/<id>.json disk layout was retired 2026-05-08.
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
- `originNodeSlug` (string, optional) — Optional book/series node slug this character belongs to (Entity.OriginNodeId). Pass this when seeding a book's cast — it lets a genuinely different character elsewhere reuse a common name (e.g. two unrelated books each with a 'Marcus') instead of being refused as a duplicate.

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

### `set_entity_origin`

Set which book/series node an existing entity belongs to (Entity.OriginNodeId), so a same-named entity in a different book is recognized as genuinely different rather than blocked as a duplicate. Pass empty originNodeSlug to clear back to universe-wide.

- `id` (string, required) — Existing entity id (32-char hex or full UUID).
- `originNodeSlug` (string, optional) — Book/series node slug to scope this entity to. Empty clears it (universe-wide/shared).

## Data Integrity

<sub>`DataIntegrityTools`</sub>

### `audit_data_consistency`

Audit SSOT drift across the SQL schema — denormalized display fields (Alias caches on bridge tables) disagreeing with the FK they cache, orphaned subtype rows, dangling edges, slug collisions, and EntityStateEvents bi-temporal hygiene. Global, cross-universe check (not scoped to one universe). No LLM calls; findings are reported, never auto-corrected.

- _(no parameters)_

### `check_graph_health`

Check the active universe's world-graph health: orphaned nodes (zero edges), weakly-connected nodes (exactly one edge), and suspicious/malformed node names (sentence fragments, junk parses from free-text fields promoted verbatim into node identities). Rebuilds the graph from live SQL before analyzing, so results always reflect current data. Zero LLM calls; pure graph traversal + string heuristics.

- _(no parameters)_

### `duplicate_entity_scan`

Scan a universe's Entities of one EntityType (default 'character'; also useful for 'faction', 'place', etc.) for duplicate or near-duplicate names (exact match, or exactly 1 edit apart, e.g. "Boris Johansen" vs "Boris Johanssen") that are NOT explained by legitimate cross-book disambiguation (Entity.OriginNodeId set to different values, meaning deliberately distinct characters in different books' continuity). Finds candidates only — it does not merge or delete anything; resolving a duplicate requires reading the actual prose to determine which row (if either) matches what was actually written, exactly as the investigation that motivated this tool did (TEST's 'Bear', 2026-08-10 — two draft entity rows, neither fully correct on its own). No LLM calls.

- `universeSlug` (string, required) — Universe slug, e.g. 'glmz', 'scry', 'nonfiction'.
- `entityType` (string, optional) — Entity type to scan. Defaults to 'character'.

### `sanity_scan_node`

Run the deterministic (no-LLM) sanity scan against one book's prose: internal dev-code leaks (an internal node code like 'BCODA' appearing as if it were an in-world name), undefined all-caps acronyms (excludes the book's own code, purely-numeric codes, glossaried terms, and acronyms inside an embedded found-document/log block written in sustained capitals), a 50-page length floor, and mojibake (encoding corruption). Fast enough for a pre-publish gate. Accepts a book node's slug or GUID.

- `nodeIdOrSlug` (string, required) — Book node slug or GUID to scan.

## Edit Session

<sub>`EditSessionTools`</sub>

### `close_edit_session`

Close the open edit session for a node (or by session ID). Returns beat count and duration.

- `nodeIdOrSlug` (string, optional) — Node id (GUID) or slug. Use this OR session_id.
- `sessionId` (string, optional) — Session GUID. Use this OR node_id_or_slug.

### `list_edit_sessions`

List edit sessions for a node, most recent first.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `limit` (int, optional) — Max number of sessions to return (default 20).

### `session_beats`

List the beats that were edited in a session, with timestamps and version deltas.

- `sessionId` (string, required) — Session GUID.

### `start_edit_session`

Start a named edit session for a node. A session groups all prose edits until closed, enabling bible/blueprint sync afterward. Session types: prose-pass, gripes-cleanup, logic-sweep, custom.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `label` (string, required) — Human-readable label, e.g. 'prose-pass-1' or 'gripes-cleanup-2026-07-13'.
- `sessionType` (string, optional) — Session type: prose-pass | gripes-cleanup | logic-sweep | custom (default).

### `sync_bible_from_session`

Extract narrative facts from a session's beats and append them as '## Session Extracts' to the node bible .md file. Use --dry-run to preview without writing.

- `sessionId` (string, required) — Session GUID.
- `dryRun` (bool, optional) — If true, returns extracted facts without writing to the bible file.

### `sync_blueprint_from_session`

Map a session's beats to their blueprint tags. Confirmed decisions are recorded; divergences file BLUEPRINT-DRIFT findings.

- `sessionId` (string, required) — Session GUID.

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

## Entity Context

<sub>`EntityContextTools`</sub>

### `clear_entity_context`

Clear the entity context stack for a node. Use when starting a new writing session for a node to reset the LRU working memory.

- `slug` (string, required) — Node slug

### `get_entity_beat_mentions`

Find every beat in the narrative where a specific entity is mentioned. Returns a list grouped by node with beat number, beat handle, and a short excerpt. Useful for auditing entity coverage, finding canon moments, and reverse-navigating from entity to story.

- `entityId` (string, required) — Entity ID (GUID) or entity slug
- `limit` (int, optional) — Maximum results to return (default 50)

### `get_entity_context`

Inspect the entity working memory currently active for a node. Shows depth-0 (directly named), depth-1 (semantic neighbors), and depth-2 (neighbors of neighbors) entities with their canon descriptions. Call after generating beats to see what was in scope.

- `slug` (string, required) — Node slug (e.g. 'ATTE', 'BCODA')

### `scan_entity_context`

Run the entity context scanner on a text snippet and return the formatted context block that would be injected into the beat prompt. Useful for testing what entities the scanner picks up from a given passage or beat goal.

- `slug` (string, required) — Node slug — context is keyed per node
- `text` (string, required) — Text to scan (beat goal, prose excerpt, or entity name)

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

## Glossary

<sub>`GlossaryTools`</sub>

### `generate_book_glossary`

Regenerate one book's Glossary (docs/nodes/{CODE}-Glossary.htm/.json/.txt) — the subset of its universe's Master Glossary whose terms actually appear in the book's live prose, detected fresh each call (not a stored join). A term the book stops using drops out on the next regenerate; a term added to the universe glossary after the book's last edit picks up automatically.

- `idOrSlug` (string, required) — Node Guid id or slug/NodeCode of the book.

### `generate_glossary`

Regenerate the current universe's Master Glossary — Glossary.htm/.json/.txt under docs/universes/{SLUG}/ — from the GlossaryTerms table. Run after upsert_glossary_term calls.

- _(no parameters)_

### `list_glossary_terms`

List every Master Glossary entry for the current universe, grouped by category.

- _(no parameters)_

### `upsert_glossary_term`

Add or update one Master Glossary entry for the current universe. term is the word/acronym as it appears in prose (e.g. 'GLMZ'); fullForm is its expansion if it's an acronym (e.g. 'Great Lakes Metropolitan Zone'), empty for plain vocabulary; definition is the reader-facing back-matter explanation (can carry more context than an in-voice gloss would); category groups entries in the rendered glossary (e.g. 'Enforcement', 'Currency', 'Tech'). Upserts by (universe, term) — case-sensitive exact match, calling again with the same term overwrites it.

- `term` (string, required) — The term/acronym as it appears in prose.
- `fullForm` (string, required) — Full expansion if an acronym; empty for plain vocabulary.
- `definition` (string, required) — Reader-facing definition shown in the glossary.
- `category` (string, optional) — Optional grouping category (e.g. 'Enforcement', 'Currency').

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

Map a node's beats to Will Storr's five-act character-change arc. Act I: establish the protagonist's flaw + ignition event (unexpected change that pressures the flaw). Act II: character applies old theory of control, it partially works. Act III: transformation trigger — the flaw fails catastrophically or wins at too high a cost. Act IV: dark night — all fears realized, old theory stripped. Act V: God moment — dramatic question answered definitively (comic: transformation; tragic: doubling down). Returns: beat assignments per act, ignition_beat / trigger_beat / god_moment_beat numbers, structural_gaps list, structural_strengths list, resolution type (comic/tragic/unclear), and an overall assessment paragraph. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

## Node

<sub>`NodeTools`</sub>

### `append_book_amendment`

Append an amendment to the node's narrative spine. Amendments are append-only — they form an auditable change log of narrative decisions. Use when: changing a character's motivation after beats are written, retconning world rules, or noting why a section was expanded or cut.

- `idOrSlug` (string, required) — Node id (GUID) or slug.
- `summary` (string, required) — One-line summary of the change.
- `body` (string, required) — Full amendment body (markdown). Explain what changed and why.

### `clear_beat_gap_after`

Clear an explicit gap-after-beat override. The audio engine falls back to the auto-computed silence from SceneType + terminator punctuation.

- `beatHandle` (string, required) — Beat Guid OR 'node-guid.beat-guid' handle.

### `clone_book`

Clone a node into a fully independent copy: new Node row + new Beat rows, same prose. Audio, scores, and review history are NOT copied — clone starts fresh. Supports nodeCode for per-experiment isolation. Use this instead of DuplicateBook when you need nodeCode or per-experiment isolation. Returns new id, slug, beat count.

- `idOrSlug` (string, required) — Source node Guid id or slug.
- `title` (string, optional) — Title for the clone. Defaults to 'Source Title (Clone)'.
- `nodeCode` (string, optional) — Optional short reference code for the clone (e.g. 'SM1'). Rejected if already in use.
- `status` (string, optional) — Status value to stamp on the clone: 'ready', 'draft', etc. Default 'ready'.

### `composite_cover_title`

Redraw the book title onto an already-saved cover image file in place, without calling an image-generation API again. Useful after tweaking the compositor or for a cover saved before title-compositing existed. Requires Node.CoverImagePath to already be set (run generate_cover_image first). Accepts node id (GUID) or slug.

- `idOrSlug` (string, required) — Node id (GUID) or slug.

### `create_book`

Create a BookNode — a single book arc (book / novella / standalone). Pass 'seed' to also generate a book bible and planned beats immediately. Optional parent makes it part of a series; optional previous marks it a sequel (sequel commandments apply). Returns the new id, slug, url, and (if generated) the bible text.

- `title` (string, required) — Book title. Required.
- `description` (string, optional) — Optional back-of-book description.
- `seed` (string, optional) — One-line generation seed. When provided, the book bible and planned beats are created immediately after the row is inserted.
- `targetBeats` (int, optional) — Target beat count for the bible spine (only used when seed is provided). Default 12.
- `parentNodeIdOrSlug` (string, optional) — Optional parent SeriesNode Guid id (or slug). Empty = standalone.
- `code` (string, optional) — Optional short author-assigned reference code (e.g. 'ATTE'). Uppercased, unique lookup key.
- `previous` (string, optional) — Optional prior book this one continues (slug or GUID) — sequel commandments apply.

### `create_chapter`

Create a ChapterNode under a book. Chapters hold beats and never carry a reference code. parentNodeIdOrSlug is REQUIRED. Returns the new id, slug, and url.

- `title` (string, required) — Chapter title. Required.
- `parentNodeIdOrSlug` (string, required) — Parent BookNode Guid id or slug. Required.
- `description` (string, optional) — Optional back-of-book description.

### `create_series`

Create a SeriesNode — the top-level grouping (saga / anthology) that BookNodes hang under. Never holds beats. Returns the new id, slug, and URL.

- `title` (string, required) — Series title. Required.
- `code` (string, optional) — Optional short reference code (e.g. 'BCODA'). Upper-cased; rejected if already in use.
- `description` (string, optional) — Optional one-line description (back-of-book text).

### `delete_beat`

Remove a beat from a node. If the beat is not referenced by any other node, the beat row + audio file are deleted entirely.

- `nodeIdOrSlug` (string, required) — Node Guid id or slug.
- `beatId` (string, required) — Beat Guid id to delete.

### `duplicate_book`

Deep-duplicate a node (and its sub-node tree) into a fresh, independent copy. Every beat is cloned into a new row — prose and narration metadata are preserved, but audio, review scores, and the stale flag are reset. Editing the copy never affects the original. Accepts a Guid id OR a slug. Returns the new node's id, slug, and writer URL.

- `idOrSlug` (string, required) — Source node Guid id or slug.
- `newTitle` (string, required) — Title for the new duplicate. Required.

### `export_audiobook`

Render the whole node as one continuous narration (no per-beat voice drift) and write the MP3 to the configured export directory (defaults to Desktop). TTS engine: 'elevenlabs' (default, paid, highest fidelity), 'piper' (free/local, fastest), 'kokoro' (free/local, recommended), 'chatterbox' (free/local, most expressive). Returns the path of the written file, or null if the node has no beat text. This only generates a local MP3 — it does not publish anything to Audible/ACX.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `ttsEngine` (string, optional) — TTS engine: elevenlabs (default) | piper | kokoro | chatterbox.
- `robust` (bool, optional) — Set to true to retune this node's frozen voice snapshot to Robust stability (1.0) before recording.

### `export_node`

Render a node to .docx + .epub + .pdf + .txt, plus description.txt (from Node.Description), keywords.txt (from seeded NodeKeywords), and cover.jpg (only if missing), all written to the configured export directory (defaults to Desktop). Same full pipeline as the CLI's `prose --export-node --slug <slug>`. Returns the path of every artifact written (nulls for the optional ones that had no source data). This only generates local files — it does not publish anything to Amazon/KDP. Use get_node first to confirm the node exists.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `author` (string, optional) — Author name to embed in the document properties. Optional.

### `generate_book_bible`

Generate (or regenerate) the node bible for a node. Uses the node's Seed field (falls back to Synopsis then Title) plus the literary rules to produce a dry structural plan: logline, premise, register, characters, numbered beat spine, seeds & payoffs. Creates planned Beat rows from the spine when the node has no beats yet. Returns the generated bible text.

- `idOrSlug` (string, required) — Node Guid id or slug.
- `targetBeats` (int, optional) — Target number of beats in the spine. 0 = auto (use existing beat count or 12).

### `generate_cover_image`

Render and save a book cover image (png/jpg) via a chosen image provider, using Node.CoverPrompt as the prompt (generating one first via generate_cover_prompt if it's not set yet). Requires that provider's API key to be configured in Settings — costs real money per call. Saves to the media dir under covers/{slug}.{ext} and records the path/provider on the node. Accepts node id (GUID) or slug.

- `idOrSlug` (string, required) — Node id (GUID) or slug.
- `provider` (string, required) — Image provider: "openai" (gpt-image-1), "stability" (Stable Image SD3.5), or "google" (Imagen via Gemini API).

### `generate_cover_prompt`

Generate and save a book-cover image prompt (Node.CoverPrompt) from the book's own Title/Summary/Description and universe — a single paragraph describing subject, setting, mood, palette, and composition for an image model. Kept commercial-cover-safe (never explicit) regardless of interior content. Overwrites any existing CoverPrompt. Accepts node id (GUID) or slug.

- `idOrSlug` (string, required) — Node id (GUID) or slug.

### `generate_node_doc`

Assemble the unified Book Context Document for a node: merges hand-authored NodeBible content with the Structural Blueprint and Beat Spine from the DB, then writes the result to both Nodes.NodeBible and docs/nodes/{CODE}.md. The MarkdownFiles sync (what DocContextService reads at generation time) runs automatically as part of this call — no follow-up call needed. Run this before editing a book to get a fresh, complete context document. The disk file is a read-only generated mirror — never hand-edit it.

- `nodeIdOrSlug` (string, required) — Node id (GUID), slug, or NodeCode.

### `get_beat`

Get a single beat with every authoring field — prose, kind, IsChapterStart, BeatTitle, gap-after, tone/pace/facet metadata, position within node, and the previous/next beat ids for relative insertion. Accepts a plain Beat Guid or the 'node-guid.beat-guid' dotted handle the writer UI shows on the LLM bottom sheet.

- `beatHandle` (string, required) — Beat Guid OR the dotted 'node-guid.beat-guid' handle.

### `get_book`

Get a single node with its beats in reading order. Accepts a Guid id OR a slug. Returns node metadata + ordered beats (id, text, stale, has_audio, title, description).

- `idOrSlug` (string, required) — Node Guid id or slug.

### `get_book_bible`

Get the node bible for a node — the dry structural plan (logline, premise, register, characters, beat spine, seeds & payoffs). Returns the raw markdown text plus the parsed beat spine entries so you can see the planned arc at a glance. Returns has_bible=false when no bible exists yet.

- `idOrSlug` (string, required) — Node Guid id or slug.

### `get_book_spine`

Return the full narrative spine for a node: bible, user stories, all amendments (in order), and the latest spine version pin (which records the content hashes and amendment count at the last docx export). Use this before writing prose to understand the narrative contract.

- `idOrSlug` (string, required) — Node id (GUID) or slug.

### `get_cover_provider_status`

Return the current status of the cover pipeline: for each registered image provider, its id and whether an API key is configured. Use before calling generate_cover_image to know which providers are actually usable.

- _(no parameters)_

### `get_score_history`

Return the score history for a node as a time-series — every review run that produced a summary, with its mean score, SD, review count, and date. Use to track whether an edit moved the needle, or to compare pre/post-edit trajectories. Accepts node id (GUID) or slug.

- `idOrSlug` (string, required) — Node id (GUID) or slug.
- `limit` (int, optional) — Maximum history points to return (most recent first). Default 20.

### `insert_beat`

Insert a new beat into a node. Pass an empty afterBeatId to insert at the top. Returns the new beat's id.

- `nodeIdOrSlug` (string, required) — Node Guid id or slug.
- `afterBeatId` (string, optional) — Beat Guid id to insert after, or empty for top-of-node.
- `text` (string, optional) — Initial prose text for the new beat. May be empty.

### `join_beat`

Merge one beat into the previous one in the node. Audio on the survivor is invalidated.

- `nodeIdOrSlug` (string, required) — Node Guid id or slug.
- `beatId` (string, required) — Beat Guid id to merge upward.

### `list_books`

List nodes. Use kind='book' to list all root narratives; kind='chapter' for all sub-nodes (contain beats). Returns a flat list of id, slug, title, kind, status, beat-count, stale-count.

- `kind` (string, optional) — Optional Kind filter — 'book' (root nodes) or 'chapter' (sub-nodes with beats). Case-insensitive equality match.
- `limit` (int, optional) — Maximum rows to return. Default 100.

### `list_scores`

List nodes with their latest review score, word count, and estimated page count (250 words/page). Optionally filter by kind ('book', 'chapter', 'episode', etc.) and/or status ('draft', 'canon', 'ready', 'archived'). Returns code, title, kind, status, score (null if unreviewed), words, pages, scored_on. Sorted by score descending (unscored nodes last). Use this for a quick quality dashboard without running new reviews.

- `kind` (string, optional) — Optional kind filter (case-insensitive). E.g. 'book', 'chapter', 'novella'. Empty = all kinds.
- `status` (string, optional) — Optional status filter (case-insensitive). E.g. 'draft', 'canon', 'ready'. Empty = all statuses except archived.
- `includeArchived` (bool, optional) — Include archived nodes. Default false.
- `limit` (int, optional) — Maximum rows to return. Default 200.

### `narrate_book`

Kick off TTS narration for every un-narrated beat in this node (and its child nodes recursively). Returns immediately — narration runs in the background; poll get_node to observe progress. Returns an error response (without spawning anything) if TTS is not configured.

- `nodeIdOrSlug` (string, required) — Node Guid id or slug.

### `pin_book_spine_version`

Create a spine version pin for the node's current docx version. Records the SHA-256 hashes of the current bible and user stories, plus the amendment count, so future drift checks can tell when prose was written against a stale spine. Call this after every significant prose session or whenever the spine changes.

- `idOrSlug` (string, required) — Node id (GUID) or slug.
- `notes` (string, optional) — Optional human note explaining what changed at this version.

### `prepare_audible`

Build an Audible AI-narration hand-off package for a node. Produces three files in {publishDir}/{Title}/Audible/: (1) a narration-clean manuscript (.audible.txt) with markdown artifacts stripped and Φ expanded to 'QUANTA'; (2) a pronunciation guide (.pronunciation.md) listing entity names with plain-English respellings; (3) AUDIBLE_README.md with submission instructions. No API is called on Audible's side — the author uploads the .audible.txt via ACX/Audible publisher portal. Returns paths + word/term counts.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `withPhonetics` (bool, optional) — Run the optional LLM phonetics pass to fill in 'Say it as' respellings. Default true. Set false to skip and leave the column blank for manual completion.

### `print_book`

Print all beats of a node as continuous prose — each beat's Text joined by a blank line. No headers, no beat numbers, no metadata. Accepts node id (GUID) or slug. Use this to read the full prose of a node in one call.

- `idOrSlug` (string, required) — Node Guid id or slug.

### `rebeat_book`

Re-segment a node's beats to the codified beat doctrine via LLM re-segmentation. Dry-run by default (safe to call freely). Set apply=true to export a Markdown backup then replace the beats — only committed if the word-retention guard passes (prevents silent content loss). Returns old/new beat counts, retention %, guard result, and a note if it was blocked.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `apply` (bool, optional) — Set to true to commit the new segmentation. Default false = dry run.

### `reflow_book`

Copy-edit a node's prose in-place: adds missing '?' on questions, swaps 'says/said' → 'asks/asked' on question dialogue lines, and normalises paragraph/dialogue spacing. Dry-run by default — set apply=true to commit. Beats the model modified beyond those specific edits are rejected and left untouched. Returns changed/unchanged/rejected/errors counts plus per-beat diff previews.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `apply` (bool, optional) — Set to true to write the edits to the DB. Default false = dry run.

### `set_beat_gap_after`

Set the silence (in ms) the audio engine inserts AFTER this beat, before the next. 0 = no silence (explicit override). Use ClearBeatGapAfter to revert to the auto-computed default from SceneType + terminator punctuation.

- `beatHandle` (string, required) — Beat Guid OR 'node-guid.beat-guid' handle.
- `durationMs` (int, required) — Silence in milliseconds, 0..6000.

### `set_book_bible`

Manually set or replace the node bible text. Use when you want to hand-write the plan instead of generating it. The text is saved verbatim; beat spine parsing still applies for planned-beat creation. Pass an empty string to clear the bible. The docs/nodes/{CODE}.md mirror and MarkdownFiles sync (what DocContextService reads) are regenerated automatically as part of this call.

- `idOrSlug` (string, required) — Node Guid id or slug.
- `bibleText` (string, required) — Full bible markdown text to store. Empty string clears the bible.

### `set_book_user_stories`

Set (replace) the user stories / acceptance criteria for a node. Write this before starting prose — it defines what scenes, arcs, and voice moments must be present for the node to reach ≥82% standalone and ≥85% cumulative book score.

- `idOrSlug` (string, required) — Node id (GUID) or slug.
- `userStoriesText` (string, required) — Full user stories markdown. Will replace any existing content.

### `split_beat`

Split one beat into two at the nearest sentence boundary near its midpoint. Both halves lose their audio.

- `nodeIdOrSlug` (string, required) — Node Guid id or slug.
- `beatId` (string, required) — Beat Guid id to split.

### `update_beat_metadata`

Update a beat's metadata: Title, Description, EmotionalTone, PaceHint, StructureRole, Act, SceneType, IsChapterStart, Kind. Pass empty strings to clear nullable fields. Does NOT touch prose or audio. Use to mark a beat as a chapter start, change its kind to quote/dedication/book-title, or set the tone the next re-record uses.

- `beatHandle` (string, required) — Beat Guid OR 'node-guid.beat-guid' handle.
- `title` (string, optional) — Short label. When IsChapterStart=true this is the chapter heading; when Kind=quote this is the attribution.
- `description` (string, optional) — One-line description fed to LLM regenerations.
- `subtext` (string, optional) — What is happening beneath the prose — foreshadowing, unspoken motivations, dramatic irony. Visible to the prose writer LLM but never printed.
- `emotionalTone` (string, optional) — Emotional tone, e.g. 'quiet' / 'tense' / 'wry'.
- `paceHint` (string, optional) — Pace hint, e.g. 'flowing' / 'clipped' / 'staccato' / 'languorous'.
- `structureRole` (string, optional) — Structure role, e.g. 'inciting-incident' / 'rising-action' / 'climax'.
- `act` (int, optional) — Plot-act number 0–5. 0 = unassigned.
- `sceneType` (string, optional) — Scene type: scene | summary | transition | interstitial.
- `isChapterStart` (bool, optional) — True = this beat begins a new chapter / section. The writer renders a divider above it with Title as the heading.
- `kind` (string, optional) — Beat kind: prose (default) | book-title | dedication | quote. Free-form so new kinds add no schema cost.
- `eventSummary` (string, optional) — Optional manual override for the plot-event line (EventSummary — 'what happened', distinct from Description's authorial-intent register). When provided, sets Beat.EventSummary and stamps EventSummaryHash to the beat's CURRENT TextHash, which 'freezes' the manual line so the next generate_event_list run sees it as already current and skips it (no LLM call, no clobber). Pass empty string to clear. Omit (leave null) to leave the beat's event line untouched — unlike the other params above, this one is NOT overwritten by an empty default.

### `update_beat_text`

Update one beat's prose. Recomputes the hash, marks the beat stale, and invalidates its audio. Beat.Text accepts inline markdown (**bold** / *italic* / __underline__ / ~~strike~~) and ElevenLabs-style tone tags ([WHISPERING] [GASP] [LAUGHS] [PAUSES] etc.) that render as emoji in the read view. Accepts a Beat Guid OR the 'node-guid.beat-guid' handle.

- `beatHandle` (string, required) — Beat Guid OR 'node-guid.beat-guid' handle.
- `text` (string, required) — New prose. Replaces the entire beat text. Markdown markers + tone-tag brackets are preserved verbatim in storage.

### `update_book`

Update a node's metadata fields. Pass only the fields you want to change — omit the rest to leave them unchanged. Editable fields: title, description, kind, status, seed, code (NodeCode), voice_id, kdp_page_count, cover_prompt. Status valid values: draft | ready | canon | archived. Code is uppercased and must be unique across non-null values — pass empty string to clear it. Does NOT touch beats or audio.

- `idOrSlug` (string, required) — Node id (GUID) or slug.
- `title` (string, optional) — New title. Omit to leave unchanged.
- `subtitle` (string, optional) — Subtitle (e.g. 'A GLMZ Novella'). Omit to leave unchanged; pass empty string to clear.
- `description` (string, optional) — Back-of-book description. Omit to leave unchanged; pass empty string to clear.
- `kind` (string, optional) — Kind label: book | chapter | episode | novella | novel | node | scene | saga | anthology. Omit to leave unchanged.
- `status` (string, optional) — Status: draft | ready | canon | archived. Omit to leave unchanged.
- `seed` (string, optional) — Generation seed (one-line premise). Omit to leave unchanged; pass empty string to clear.
- `code` (string, optional) — Short author reference code (e.g. 'ATTE'). Uppercased; pass empty string to clear. Omit to leave unchanged.
- `voiceId` (string, optional) — ElevenLabs or local TTS voice id. Omit to leave unchanged; pass empty string to clear.
- `kdpPageCount` (int, optional) — KDP print-page count from Word (File → Info → Properties → Pages). Used to calculate the correct inside margin on the next export. Pass 0 to clear.
- `coverPrompt` (string, optional) — Hand-set cover art image prompt (overrides the generated one). Omit to leave unchanged; pass empty string to clear. Prefer generate_cover_prompt to derive this from the book itself.

## Noun Consistency

<sub>`NounConsistencyTools`</sub>

### `add_deprecated_name`

Register a deprecated noun rule. Any beat that contains 'deprecatedName' (whole-word, case-insensitive) in the target universe will be flagged by validate_nouns. Use when a named thing is renamed or retired. universeSlug defaults to 'glmz' when omitted.

- `deprecatedName` (string, required) — The old/wrong name to flag in prose (e.g. 'VacCell', 'Rider').
- `canonicalName` (string, required) — The correct name to use instead (e.g. 'Nit', 'Exo').
- `notes` (string, optional) — Optional explanation (e.g. 'Renamed in SS-A38 when Rider job was retired').
- `universeSlug` (string, optional) — Universe slug ('glmz' or 'fantasy'). Defaults to 'glmz'.

### `list_deprecated_names`

List all registered deprecated noun rules. Filter by universeSlug ('glmz' or 'fantasy') or omit for all universes.

- `universeSlug` (string, optional) — Optional universe slug to filter ('glmz' or 'fantasy'). Omit for all.

### `validate_nouns`

Scan a node's prose beats for deprecated or renamed noun references. Returns ok:true when clean; ok:false with a violations list (beatNumber, deprecatedName, canonicalName, snippet) when stale names are found. Register rules first with add_deprecated_name.

- `nodeIdOrSlug` (string, required) — Node slug or GUID to scan.

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

## Plant Payoff

<sub>`PlantPayoffTools`</sub>

### `audit_plant_payoffs`

Audit all plant/payoff pairs for a node. Returns: total_pairs, planted (seeded in a beat), paid_off (payoff also written), orphaned (planted but no payoff), not_transparent (payoff exists but is_transparent=false), a gateway_plant_ready boolean (all planted pairs have transparent payoffs), and detail lists for each problem category. Fix orphaned plants and transparency issues before the node passes gateway audit. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `get_plant_payoffs`

List all registered plant/payoff pairs for a node. A plant is a narrative detail seeded early (a behavioral tell, an object, a gloss) that resonates or resolves later — rewarding re-readers without requiring first-timers to catch it. Returns all pairs with their status (planned = not yet written, seeded = plant beat written but no payoff yet, paid-off = both beats written), is_transparent flag (must be true for the payoff to work for cold readers), and transparency_note (what the re-reader gains). Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `link_payoff_beat`

Link the payoff beat to a registered plant/payoff pair. Call after writing the beat where the plant pays off. plant_payoff_id = GUID from register_plant_payoff; beat_id = GUID of the payoff beat.

- `plantPayoffId` (string, required) — PlantPayoff id (GUID) from register_plant_payoff.
- `beatId` (string, required) — Beat GUID containing the payoff.

### `link_plant_beat`

Link the plant beat to a registered plant/payoff pair. Call after writing the beat that seeds the plant detail. plant_payoff_id = GUID returned by register_plant_payoff; beat_id = GUID of the beat containing the plant.

- `plantPayoffId` (string, required) — PlantPayoff id (GUID) from register_plant_payoff.
- `beatId` (string, required) — Beat GUID containing the plant.

### `register_plant_payoff`

Register a new plant/payoff pair for a node. Call this when you're about to write (or have just written) a detail that will pay off later. plant_description = what is seeded (the observable detail the cold reader sees but doesn't decode); payoff_description = what the re-reader gets (the deeper meaning on return). Category options: detail, echo, irony, motif, character-truth, structural. Optionally link to specific beats by their GUID ids (plant_beat_id, payoff_beat_id). Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `plantDescription` (string, required) — What is seeded — the detail the cold reader encounters but doesn't decode. Example: 'Kyle's hand twitches when he mentions Seo.'
- `payoffDescription` (string, required) — How it pays off — what the returning reader gets on re-read. Example: 'On re-read, the twitch reveals the mentor was fabricated long before Kyle admits it.'
- `category` (string, optional) — Category: detail | echo | irony | motif | character-truth | structural
- `plantBeatId` (string, optional) — Beat GUID where the plant is seeded (omit if not yet written).
- `payoffBeatId` (string, optional) — Beat GUID where the payoff occurs (omit if not yet written).

### `set_plant_transparency`

Record whether a payoff beat stands alone for cold readers (is_transparent) and what the re-reader gains (note). is_transparent=true means the payoff makes complete narrative sense without having read/remembered the plant. is_transparent=false is a writing bug — fix the payoff beat before marking the node gateway-ready. note should name the specific additional layer the returning reader receives.

- `plantPayoffId` (string, required) — PlantPayoff id (GUID).
- `isTransparent` (bool, required) — True = the payoff reads completely for a cold reader; false = it requires catching the plant (writing bug).
- `note` (string, optional) — What the re-reader gains that the first-timer doesn't. Required when is_transparent=true.

## Quality

<sub>`QualityTools`</sub>

### `analyze_writing_quality`

Run the writing-quality heuristic pass over a book's chapters. Same checks the BookReviewService runs before its LLM Quorum: first-line strength, tension delta (flags 4+ low-tension beats in a row), paragraph-serves audit (paragraphs with no dialogue / sensory detail / action / number / capitalized noun), motif reuse (chapters that drop registered motifs), voice cadence Jaccard (chapter prose drifting from POV character's documented vocabulary). Returns findings list. No LLM calls.

- `bookId` (string, required) — Book id.

### `check_canon`

Sweep a node's prose against the entire canon database (entities, locations, weapons, etc.) and queue each contradiction as a CANON-CONTRADICTION finding with an optional proposed fix. Returns the list of contradictions found. Use list_findings / apply_finding / set_finding_status to manage them afterward. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `proposeFixes` (bool, optional) — Set to true to also draft a suggested rewrite for each contradiction found.

### `check_duplicate_beats`

Corpus-wide near-duplicate-scene detector. Flags beat pairs anywhere in a book whose prose embeddings are near-identical (default cosine similarity floor 0.90) — catches an abandoned early draft left enabled alongside its own developed, canonical rewrite written later. Excludes beat pairs merely adjacent within the same chapter (a continuous scene is supposed to share vocabulary — that's not a duplicate). The 0.90 default is deliberately high-precision/low-recall: real-corpus calibration found a genuine duplicate pair scoring only 0.84, while a lower floor also surfaces dozens of false positives from a book's own deliberate recurring formulaic devices (contract postings, logbook entries). Pass a lower threshold (e.g. 0.80) for an occasional deliberate deep pass, expecting more manual filtering. Candidate generator, NOT a verdict: read both beats in full before disabling either with set_beat_membership_enabled. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug — should be a BookNode; its descendant chapters are scanned together.
- `threshold` (double, optional) — Cosine similarity floor for a candidate pair, 0–1. Default 0.90.

### `check_semantic_fidelity`

Check the Semantic Fidelity Gap for a node — meaning drift from the book's original intent. Two checks: (1) Bible alignment: cosine similarity between each beat's prose and the node's Seed/Synopsis — a beat that no longer resembles the book it was born from has drifted. (2) Intent alignment: cosine similarity between each beat's Synopsis (stated purpose) and its actual prose — drift here means the rewrite served something other than the beat's purpose. Evaluates every beat with prose (Beat.Score, if present, is reported but not a gate). Embeds beats (drift-skipped), queries alignment, files SEMANTIC-DRIFT findings for violators, and returns the full report. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `diagnose_book`

Pre-flight structural analysis before running the review panel. Runs 12 targeted checks in parallel and returns Pass/Warn/Fail for each with evidence (a quote from the text) and a concrete one-action fix. Blocking failures (antagonist cost, protagonist behavior change, stakes embodiment, exposition density) mean the chapter is structurally unsound and will score in the 70s regardless of prose quality. Fix those first, then run review_node. Accepts node id (GUID) or slug. max_chars controls how much of the assembled node text each check sees (default 40000 chars ≈ 10k tokens — covers most chapter-length nodes; lower to reduce cost, raise for very long nodes).

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `maxChars` (int, optional) — Max characters of assembled node text each check reads. Default 40000 (~10k tokens). Lower to reduce cost; raise for very long nodes (max practical: ~160000).

### `examine_emotional_depth`

Emotional Intelligence Examination (SS-A15). Scores prose against an 8-dimension, 0–4 rubric — per beat, character-aware (Want/Need/Wound/Flaw from the node bible), register-adaptive (CODA/JOY/SORROW/Fantasy anchors). Returns: EmotionalDepthScore 0–100, per-dimension 0–4 scores with strongest evidence, weakest evidence, weakest beat number, and a beat-scoped craft fix; a per-beat emotional depth curve (Standard/Deep effort); character ledgers. Blocking dimensions (WantNeedDivergence=want/need gap, CostFeltNotAsserted=wins felt not stated) file Findings at /findings. Does NOT change Node.Score or the 82/85 reader-panel gate. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `effort` (string, optional) — Effort tier: 'draft' (Pass 1 only, cheapest), 'standard' (Pass 1 + beat curve, default), 'deep' (Pass 1 + beat curve + ledger refresh + weakest fixes).
- `maxChars` (int, optional) — Max characters of assembled node text each check reads. Default 40000 (~10k tokens).

### `get_review_settings`

Return the current review-voting configuration: how many score-ballots and prose upgrades a sampled run casts, the persona panel depth, default reader count, max parallel ballot slots, judge provider, the comma-separated list of allowed providers, and whether the continuous auto-review monitor is enabled. Use update_review_settings to change any value.

- _(no parameters)_

### `get_review_summary`

Return the stored review summary for a node — the synthesized aggregate of what readers liked, recurring gripes, and concrete improvement suggestions, written by the judge after the last review run. Includes average score, review count, and content hash so you can tell whether the summary is stale (node was edited after the last run). Call review_node to refresh. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `list_book_reviews`

List individual ballot reviews for a node — one row per persona reader, showing persona name, provider, score, flow score (if study mode), improvements, and content hash. Use to inspect which personas scored low and what they said, or to compare how different providers voted. Results are sorted most-recent-first. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `contentHash` (string, optional) — Only return reviews from this content hash (i.e. one specific review run). Leave empty for all reviews.
- `limit` (int, optional) — Maximum rows to return. Default 50.

### `review_book`

Run the sampled Legion review panel against a node. STRUCTURAL PRE-FLIGHT runs first: if blocking failures are found (missing antagonist cost, passive protagonist, purely-stated stakes, >70% exposition), the review is blocked and returns the diagnosis instead of ballots — fix the structure first. Non-blocking warnings are always appended to the report. Stratified personas cast score-only ballots then the most informative are upgraded to full prose. Use the 'effort' tier to scale cost to importance. BRAIN: by default ballots run on the CLOUD trusted-4 panel; set use_local=true to run them on the LOCAL LLM instead (Ollama — free, no API tokens, but ONE model = no temperament diversity, so local scores are a SEPARATE baseline, not comparable to cloud means). The response always states which brain ran ('brain': 'cloud'|'local', plus 'model'). Returns: blocked (bool), brain, model, mean_score, SD, CI, report_markdown (includes structural findings), synopsis. GOTCHA: do not edit beats while a review is running. Alias: also accepts node id (GUID) for the nodeIdOrSlug param.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `ballots` (int, optional) — Number of score-only ballots to cast. 0 = use the effort tier (if given) or the ReviewBallots setting (default 20). A non-zero value overrides the tier.
- `prose` (int, optional) — Number of full prose reviews to write (upgraded from ballots). 0 = use the effort tier (if given) else 0. A non-zero value overrides the tier.
- `skipDiagnosis` (bool, optional) — Set true to skip structural pre-flight and run ballots unconditionally. Use only when you have already reviewed and accepted the structural findings.
- `effort` (string, optional) — Cost tier (RFC 0009), scales calls + per-call model to importance: 'draft' = ~6 cheap-model ballots on claude+gemini, no diagnosis, NOT a gate; 'standard' = ~12 ballots + 2 prose, the >=82% standalone gate; 'deep' = ~37 ballots + 4 prose + full structural diagnosis, the >=85%/publish gate. Omit for the configured defaults.
- `useLocal` (bool, optional) — Run ballots + synopsis on the LOCAL LLM (Ollama) instead of the cloud trusted-4 panel — free, no API tokens. ONE model = no temperament diversity, so the resulting score is a SEPARATE baseline (do NOT compare to cloud means). Default false (cloud).
- `localModel` (string, optional) — Override the local model tag for this run (e.g. an Ollama tag). Ignored unless use_local=true. Omit to use the configured LocalReviewModel.
- `allowVotes` (bool, optional) — SS-A44: score panels are DISABLED BY DEFAULT engine-wide. Set true to explicitly run this review; otherwise the call is refused. Default false.

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

Scan arbitrary prose against every world rule (no city police, no Behemoth-as-alive, no 'the Shelf' district, no wedding-cake tier architecture, no Ferrogate-as-railroad, no metro/city police, no phi/Greek-letter confusion). Returns the list of matched violations with the surrounding context. Call this on a chapter draft BEFORE delivering it — catches rule slips Claude might miss.

- `text` (string, required) — The prose to scan. Pass an entire chapter or a single beat.

## Reader Qa

<sub>`ReaderQaTools`</sub>

### `beat_checklist_audit`

Reader-Proxy QA binary craft/delight checklist per beat, hash-gated on Beat.TextHash + rule-set version — unchanged beats never re-bill; editing CRAFT.md §8 or a DELIGHT move re-evaluates the book. DON'Ts = CRAFT §8 banned mannerisms (literal binaries); DO = '≥1 applicable DELIGHT move lands' (short connective beats exempt); book level = move-monotony counters (DELIGHT §14 — a palette, not a stamp; never 'all 13 per beat'). Findings persist as CraftChecklist and auto-supersede per run. Emits NO scores. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Book node id (GUID) or slug.
- `force` (bool, optional) — Re-evaluate every beat even if unchanged (default false).

### `reader_qa_comprehension`

Reader-Proxy QA comprehension probes: a cheap model reads each chapter cold (rolling recap only) and its GENUINE reading is diffed against the fidelity-strict Sonnet synopsis; a Sonnet arbiter confirms which mismatches the chapter text itself plausibly supports (reader-plausible confusion vs probe hallucination). Confirmed defects are filed as ComprehensionDefect findings (see list_findings) and auto-supersede on re-run. Hash-cached per chapter — unchanged chapters never re-bill. Emits NO scores: this is the default reader-facing QA, replacing persona score panels. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Book node id (GUID) or slug.
- `force` (bool, optional) — Re-probe every chapter even if unchanged (default false).

### `reader_qa_gripe_pass`

Reader-Proxy QA findings-only gripe jury: a small cross-family jury full-reads the book and emits ONLY page-anchored complaints (beat number + verbatim quote + what's wrong) — NO scores, ever. Complaints are deduped, quote-grounded deterministically (hallucinated quotes die free), then Sonnet-arbitrated against the actual beat text and triaged blocker/moderate/minor. Confirmed gripes persist as ReaderGripe findings (see list_findings) and supersede on re-run. Report-only — applying a fix is a separate deliberate action (update_beat_text, optionally gated by a duel). Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Book node id (GUID) or slug.
- `readers` (int, optional) — Jury size (default 4; one seat per live model family, Claude tiers fill in).

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
- `sourceNodeSlug` (string, optional) — Source node slug, if known.
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

Archive a book: moves the book file from engine/data/books/ to engine/data/archives/books/. Non-destructive — the original chapters stay in place but the book record is removed from the active shelf. Requires the caller to retype the full book id as a confirmation token (matches the UI's type-the-guid modal). Returns ok:true on success or error:'confirmation_mismatch' / error:'not_found' otherwise.

- `id` (string, required) — Book id (32-char hex).
- `confirmId` (string, required) — Confirmation token — must equal the same full book id. Mismatched or missing values abort the archive.

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

Build the 'WHERE WE ARE' director context block for writing a specific chapter: PRIOR chapters' content, THIS chapter's outline, UPCOMING chapters' setup needs, plus open book-level threads. This is the highest-value writing-context tool — call it before drafting prose for any chapter that's part of a book.

- `bookId` (string, required) — Book id.
- `chapterId` (string, required) — Chapter id whose prose you're about to write.

### `list_books`

List every book on the shelf. Returns id, title, premise, chapter count, status, protagonists.

- _(no parameters)_

## Story Scope

<sub>`StoryScopeTools`</sub>

### `generate_structural_blueprint`

Generate the StructuralBlueprint for a book node — pre-prose structural anti-tell commitments (StoryScope countermeasures): thematically-parallel subplot with carrier beats, temporal scheme (linear/frame/nonlinear), resolution mode (external/unresolved/mixed — never internal-understanding), moral polarity (ambivalent default), per-beat 1-10 escalation curve (kills flat escalation, Claude's #1 fingerprint), per-beat event-type + revelation-mode palette (kills event monoculture), optional form device, ending style (avalanche default, no epilogue), and 3-5 intertextual anchors pulled from the entity DB. The blueprint is injected per-beat into prose generation and verified afterward by the storyscope audit. Requires Node.NodeBible unless retrofit=true (infers from written prose). The docs/nodes/{CODE}.md mirror (Structural Blueprint section) and the MarkdownFiles sync (what DocContextService reads) are regenerated automatically as part of this call. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.
- `retrofit` (bool, optional) — Set true to infer the blueprint from already-written prose (for stories that predate the blueprint system).

### `get_structural_blueprint`

Read a book node's StructuralBlueprint (pre-prose anti-tell commitments) if one exists. Returns the full blueprint including per-beat tags, or exists=false. Accepts node id (GUID) or slug.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

### `storyscope_audit`

Audit a book against the measurable structural tells of AI fiction (StoryScope countermeasures verification). Deterministic checks: blueprint-vs-execution drift (subplot planned but unwritten = BLOCKER), beat-mode run-length, emotional-depth plateaus, social-network breadth, deviation surfacing. LLM-graded checks: per-beat stakes reading (flat escalation — Claude's #1 fingerprint), event-type diversity, information-dynamics flatline, narrator moral gloss, embodied-vs-labeled emotion ratio, character-introduction method, dialogue-as-philosophy, resolution mode as written, intertextual anchor presence, TTCW originality (form + takeaway), plot-function characters, subtext, single-track causality, LAMP line mechanics, consensus-cliché scan. Severity: BLOCKER/MODERATE/MINOR per logic-sweep SOP, plus DEVIATION (legal escape hatch, surfaced for human judgment) and PASS. Findings write to the Findings table with the STORYSCOPE prefix and automatically constrain future beat writes. Accepts node id (GUID) or slug. Requires written prose; run generate_structural_blueprint first for full coverage.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

## Survey

<sub>`SurveyTools`</sub>

### `answer_survey_question`

Record the user's answer for one survey question. selectedOption is the letter key ('a', 'b', 'c', or 'd'). Call this once per question after the user exports their answers from the artifact.

- `surveySlug` (string, required) — Survey slug.
- `questionKey` (string, required) — Question key, e.g. 'Q-001'.
- `selectedOption` (string, required) — Selected option letter: 'a', 'b', 'c', or 'd'.

### `complete_survey`

Mark a survey as Completed. All questions should be Applied or Skipped before calling this.

- `slug` (string, required) — Survey slug.

### `create_survey`

Create a new canon-sync or contradiction-resolution survey. questions is a JSON array of {questionKey, title, context?, questionType?, options} where options is an array of {key, label, description?}. questionType values: PlaceDescription, TechnologyDescription, WeaponRename, FactionDescription, CharacterDescription, BeatText, DocUpdate, ContradictionResolve, Custom. Returns the survey id and slug. Call get_survey_html to generate the artifact HTML.

- `slug` (string, required) — URL-safe slug, e.g. 'canon-sync-2026-07-05'.
- `title` (string, required) — Human-readable title.
- `questionsJson` (string, required) — JSON array of question objects. Each: {questionKey, title, context?, questionType?, options:[{key,label,description?}]}
- `purpose` (string, optional) — Optional purpose / scope description.
- `universeSlug` (string, optional) — Universe slug ('glmz' or 'fantasy'). Omit for universe-neutral.

### `get_survey`

Retrieve a survey with all questions and their current answer state. Returns the survey metadata, each question's key/title/type/options/selectedOption/applyStatus.

- `slug` (string, required) — Survey slug.

### `get_survey_html`

Generate the interactive artifact HTML for a survey. Returns the full HTML string ready to be published as an artifact. After calling this, publish it via the Artifact tool with the survey slug as the filename.

- `slug` (string, required) — Survey slug.

### `list_surveys`

List surveys. Filter by status ('Open' or 'Completed') or omit for all.

- `status` (string, optional) — 'Open' or 'Completed'. Omit for all.

### `mark_survey_question_applied`

Mark a survey question as applied (or skipped) after the fix has been made. applyStatus: 'Applied' (default) or 'Skipped'. applyNotes should describe what was changed (SQL table/column, MCP tool used, etc.).

- `surveySlug` (string, required) — Survey slug.
- `questionKey` (string, required) — Question key, e.g. 'Q-001'.
- `applyNotes` (string, required) — Description of what was changed.
- `applyStatus` (string, optional) — 'Applied' or 'Skipped'. Defaults to 'Applied'.

## Swain

<sub>`SwainTools`</sub>

### `swain_audit`

Classify every enabled beat in a book against Dwight Swain's Scene/Sequel doctrine via a Haiku pass. Scene (Goal→Conflict→Disaster) and Sequel (Reaction→Dilemma→Decision) both pass; Ambiguous (one element weak/underwritten) is MODERATE; Deficient (neither pattern executes) is BLOCKER. Returns per-beat classification plus book-level pass/MODERATE/BLOCKER counts and compliance rate. Accepts node id (GUID) or slug/NodeCode.

- `nodeIdOrSlug` (string, required) — Book node id (GUID), slug, or NodeCode.
- `useOpus` (bool, optional) — Set true to use Opus instead of Haiku for classification (stubborn/ambiguous beats).

### `swain_audit_all`

Run the Swain Scene/Sequel doctrine audit across every book node in the current universe scope. Returns a per-book summary (beat count, pass/MODERATE/BLOCKER counts, compliance rate) plus corpus-wide totals. Use this first to see which books need attention before calling swain_audit on a specific one.

- `useOpus` (bool, optional) — Set true to use Opus instead of Haiku for classification (slower, costlier, more accurate on stubborn beats).

### `swain_repair`

Repair Swain BLOCKER findings in a book by auto-splicing the missing structural element (disaster turn, decision, etc.) into each deficient beat. Re-runs the audit first, then for each BLOCKER (or just beatId if given): loads the beat's current text, asks an LLM to add ONLY the missing element without rewriting existing sentences, and applies the result via the workbench. Returns per-beat repair outcomes. Accepts node id (GUID) or slug/NodeCode.

- `nodeIdOrSlug` (string, required) — Book node id (GUID), slug, or NodeCode.
- `beatId` (string, optional) — Only repair this specific beat id (GUID), if given — otherwise every BLOCKER in the book.
- `useOpus` (bool, optional) — Set true to use Opus instead of Sonnet for the splice (stubborn beats that resist a Sonnet pass).

## Universe

<sub>`UniverseTools`</sub>

### `current_universe`

Return the universe currently active for this session (slug + name).

- _(no parameters)_

### `get_universal_facts`

Return the universal world facts for the current universe — world mechanics, vocabulary, and social rules injected into every beat generation prompt. These apply to all books in the universe. Book-specific facts live in each book's node bible instead.

- _(no parameters)_

### `list_universes`

List every registered universe (slug, name, theme) and which one is currently active. Call this first to discover universe slugs before switch_universe.

- _(no parameters)_

### `set_universal_facts`

Set the universal world facts for the current universe. These facts are injected into every beat generation prompt for any book in this universe, so they should cover mechanics and vocabulary that apply everywhere (transport, technology, social structure, prose vocabulary). Book-specific content belongs in the book's node bible, not here.

- `facts` (string, required) — The full world facts text in Markdown. Replaces any existing content. Pass empty string to clear.

### `switch_universe`

Switch the active universe for this session by slug (e.g. 'glmz' or 'scry'). All subsequent canon/story reads are scoped to it. Returns the new current universe or an error if the slug is unknown.

- `slug` (string, required) — Universe slug from list_universes, e.g. 'glmz'.

## Verification

<sub>`VerificationTools`</sub>

### `truth_status`

Get the current truth status for a book: how many beats have verified contracts, how many have BeatBlueprintDecision rows, how many are in violation. Use this as a quick dashboard check before writing or exporting.

- `slugOrCode` (string, required) — Book node slug or NodeCode.

### `verify_beat`

Run all verification checks for a single beat against its declared BeatBlueprintDecision contract. Checks: BannedPattern (internal-understanding/epilogue anti-patterns), EventType (declared vs detected), SubplotCarrier (entities present when declared), EscalationFloor (emotional depth vs floor), DeclaredPurpose (embedding similarity — requires embeddings). Results are upserted to BeatVerification table. Returns Pass/Fail/Partial/Skipped per check with evidence. Exit 1 (blockers found) if any BLOCKER check fails.

- `beatId` (string, required) — Beat GUID to verify.

### `verify_book`

Run verification checks for all enabled beats in a book. Returns a summary of BLOCKER/MODERATE/MINOR failures plus individual findings. Results are upserted to BeatVerification table. BLOCKER findings must be fixed before export. Includes EscalationMonotonic check (book-wide curve regression) not available per-beat.

- `slugOrCode` (string, required) — Book node slug or NodeCode.

### `verify_quote_grounding`

Verify that a logic-sweep audit agent's CLAIMED QUOTE actually appears in the beat it's attributed to, before that finding is trusted for triage/fix. Use this on every quoted finding an audit agent reports — agents occasionally misattribute a quote to the wrong beat or fabricate one under time pressure; this is the mechanical guard against that. Comparison is normalized (dash variants, curly/straight quotes, whitespace), so only a genuine misattribution fails — not console-display punctuation drift. Result is persisted to BeatVerification (CheckType='QuoteGrounding', always inserted, never overwritten — a beat accumulates one row per claim checked across every sweep). A Fail means: reject the finding and re-read the actual beat before acting on it.

- `beatId` (string, required) — Beat GUID the finding claims this quote came from.
- `quote` (string, required) — The exact text the audit agent claims appears in this beat.
- `claimedBy` (string, optional) — Optional: which agent/pass made this claim, for the audit trail.

### `verify_quote_grounding_batch`

Batch form of VerifyQuoteGrounding: gate an ENTIRE audit report in one call before triage. Pass every (beatId, quote) claim the audit produced; get back which ones are actually grounded in their attributed beat and which must be rejected/re-verified. Run this before triaging any logic-sweep audit findings that quote beat text (SS-LOGIC-4a).

- `claimsJson` (string, required) — JSON array of claims: [{"beatId":"<guid>","quote":"<text>"}, ...]
- `claimedBy` (string, optional) — Optional: which agent/pass made these claims, for the audit trail.

## Voice

<sub>`VoiceTools`</sub>

### `apply_voice_proposal`

Apply a proposed voice rule to the live voice store (the DB-backed rules the generator reads). Pass the entry id returned by harvest_voice or list_voice_proposals. The entry status changes to 'applied'. Returns ok=true on success, or error if the entry was not found or already resolved.

- `entryId` (string, required) — The voice change-log entry GUID to apply.

### `harvest_voice`

Distill voice rules from a winning node (score ≥80) into proposed change-log entries. Nothing touches the live rule store until apply_voice_proposal is called. Pass force=true to harvest even if the node scored below 80. Returns the list of proposed entries with their ids, rule targets, descriptions, and evidence.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug to harvest from.
- `force` (bool, optional) — Set to true to harvest even if the node scored below 80%.

### `harvest_voice_all`

Distill voice rules from every node scored >=threshold (default 80). Score gates were retired project-wide (SS-A44) so almost no node has a Score anymore — this will likely return empty. Prefer harvest_voice_canon or harvest_voice_node(force:true) instead. Returns proposals grouped by node slug. Nothing is written to the live rule store until apply_voice_proposal is called.

- `threshold` (double, optional) — Minimum Node.Score to include (0-100). Default 80. Only affects nodes that HAVE a Score — most nodes have none post-SS-A44.

### `harvest_voice_canon`

Distill voice rules from every node the author has marked Canon (IsCanon=true) — the recommended harvest gate post-SS-A44, since almost no node carries a Score anymore. Returns proposals grouped by node slug. Nothing is written to the live rule store until apply_voice_proposal is called.

- _(no parameters)_

### `list_voice_proposals`

List voice change-log entries filtered by status. Use status='proposed' to see pending proposals awaiting a decision. Each entry shows its id (use for apply/reject), rule target, description, evidence, and source node.

- `status` (string, optional) — Filter by status: 'proposed' | 'applied' | 'rejected' | 'observed'. Default 'proposed'.

### `reject_voice_proposal`

Reject a proposed voice rule. The entry stays in the audit trail (status = 'rejected') so the decision is traceable. Pass the entry id returned by harvest_voice or list_voice_proposals.

- `entryId` (string, required) — The voice change-log entry GUID to reject.

## Workflow Monitor

<sub>`WorkflowMonitorTools`</sub>

### `workflow_beat_modes`

Get the detected beat mode log for a node. Shows how each beat was classified (Narrative/Combat/EmotionalClimax/Dialogue/Transition/Revelation) and the confidence level.

- `slug` (string, required) — Node slug

### `workflow_status`

Get prose service coverage for a node. Returns which services (Pacing, StoryMethodology, PlantPayoff, StoryAudit, Combat) were active when beats were written, and flags gaps where applicable services weren't used.

- `slug` (string, required) — Node slug (e.g. 'ATTE', 'BCODA')

### `workflow_status_global`

Get global prose workflow coverage across all nodes. Returns per-service utilization rates and a list of nodes with coverage gaps.

- _(no parameters)_

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

Add an editorial prose lesson — an author ruling that reviewers must respect. Lessons are injected into every future review ballot prompt so the panel does not penalise beats the author has already decided are doing their job in the sequence. scope: 'global' applies to all nodes; 'node:<slug>' to one node; 'beat:<guid>' to one beat. kind: score-vs-function | delight | voice | pacing | continuity | other.

- `scope` (string, required) — Scope: 'global', 'node:<slug>', or 'beat:<guid>'
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

Runs the deterministic prose pattern linter on text. Detects: clichés (chrome gleam, heart hammered…), pseudo-profound constructs (in that moment, it hit him that…), on-the-nose interiority, and italicised dialogue. Returns a JSON array of violations.

- `text` (string, required) — Prose text to lint

### `check_timeline`

Deterministic timeline-consistency check for a node (RFC 0009 §5). Zero LLM calls. Detects two violation classes: (1) dead-character-acting — an entity whose status is 'dead'/'deceased' appears in a later beat; (2) wound-regression — a healed/none event precedes the injury-onset event for the same condition. Returns a list of findings with kind, entityId, entityName, beatNumber, detail, severity. Returns an empty array when no events are in the ledger for this node — never throws.

- `slugOrId` (string, required) — Node slug or GUID

### `clear_entity_stale`

Clears the EntityStale flag on a beat after the author has reviewed it and confirmed the prose is still consistent with current entity canon.

- `beatId` (string, required) — Beat GUID

### `get_ambient_palette`

Returns the sensory detail palette for a character's carried gear. Inject the result into a beat prompt to ground sensory texture in what the character actually carries.

- `characterId` (string, required) — Character entity GUID
- `asOfDate` (string, optional) — Story-date filter (ISO 8601). Omit for current carry edges.

### `get_character_equipment`

Returns a character's full equipment across all slots: primary/secondary/ranged weapons, armor, tool, signature gear, pharmaceuticals, and carried loot. Use for scene continuity, loot tracking, and loadout management.

- `characterSlug` (string, required) — Character entity slug (e.g. 'kyle_ellen_corbin', 'sasha_vo').

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

Returns every beat flagged EntityStale — i.e. a canon entity mentioned in the beat was updated after the beat was written. Grouped by node. Review each beat and call clear_entity_stale when satisfied.

- _(no parameters)_

### `list_prose_lessons`

List prose lessons from the editorial memory store. When scope is omitted, returns all lessons across all scopes. When scope is provided, returns only lessons whose scope starts with that prefix (e.g. 'global' for all global lessons, 'node:my-slug' for a specific node).

- `scope` (string, optional) — Optional scope filter prefix (e.g. 'global', 'node:my-slug'). Omit for all.

### `scan_book_violations`

Run the prose pattern guard over every beat in a node and file violations as Findings. This is the node-wide sweep equivalent of check_prose — use it after importing or rewriting a node to catch all clichés, pseudo-profound constructs, on-the-nose interiority, and italicised dialogue in one pass. Returns a per-beat summary of violations found.

- `nodeIdOrSlug` (string, required) — Node id (GUID) or slug.

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

### `create_legacy_book`

LEGACY Book/Chapter schema — new work should use create_series / create_book instead. Create or upsert a Book record. Pass an empty id to create a new book (a v7 GUID is assigned and returned); pass a known id to update an existing book. Returns the persisted Book including assigned id.

- `title` (string, required) — Book title. Required.
- `premise` (string, required) — One-paragraph premise — feeds the chapter director when extending.
- `protagonists` (string, required) — Comma-separated protagonist names — first name is the lead. Resolved against character canon.
- `arcTarget` (string, optional) — What this book is *about* and where it lands. Used as the extension target. Optional.
- `tagline` (string, optional) — Optional tagline shown beneath the title on the bookshelf card.
- `status` (string, optional) — Book status: drafting | preserved | published | archived. Defaults to 'drafting'.
- `id` (string, optional) — Optional book id to update an existing record. Empty creates a new book.

### `create_legacy_chapter`

LEGACY Book/Chapter schema — new work should use create_chapter (node tree) instead. Create or upsert a Chapter record. Pass an empty id to create new; pass a known id to update. Returns the persisted Chapter including assigned id.

- `title` (string, required) — Chapter title. Required.
- `synopsis` (string, required) — One-paragraph chapter synopsis. Required.
- `html` (string, required) — Full chapter prose. HTML or plain text — plain text is wrapped in <p> tags on render.
- `characters` (string, required) — Comma-separated character names participating in this chapter.
- `bookId` (string, optional) — Parent book id. Empty leaves the chapter orphaned.
- `number` (int, optional) — Chapter number within the book (1-indexed). Ignored when bookId is empty.
- `status` (string, optional) — Chapter status: draft | revising | reviewed | published. Defaults to 'draft'.
- `id` (string, optional) — Optional chapter id to update an existing record. Empty creates new.

