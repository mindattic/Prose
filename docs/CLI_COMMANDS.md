# Prose CLI Commands

> **GENERATED — do not hand-edit.** Produced by `CommandDocGenerator` from the
> dispatch chain in `v3/Prose.Cli/Program.cs`, which is the single source of truth
> for what the CLI actually does. To refresh:
> 
> ```powershell
> dotnet run --project v3/Prose.Cli -- --export-commands docs/CLI_COMMANDS.md
> ```
>
> Every command executes **inside Prose.Hub** — `Prose.Cli` forwards to it and never
> touches the database itself. A command marked **cost-gated** spends LLM money and
> routes through the cost gate; everything else is deterministic or read-only.
> Most commands require a `--universe <slug>` scope.

**274 commands.** 15 cost-gated. 14 have no description in their dispatch comment (7 have neither a description nor a usage line); they are listed anyway with whatever could be recovered, because a reference that silently omits what it could not parse is worse than one that admits the hole.

### `--add-alias`

```
prose --add-alias --value "<alias>" --entity <id-or-name> [--apply] [--force]
```

add one alias row to one entity (dry-run unless --apply). The other half of --delete-alias — re-binding a name the prose actually uses to the entity that owns it, which until now had no path outside create_character's `aliases` parameter.

<sub>handler `AddAliasCli`</sub>

### `--add-apparel`

```
prose --add-apparel --file path.json
```

insert an Apparel item from an ApparelData JSON file.

<sub>handler `AddApparelCli`</sub>

### `--add-character`

```
prose --add-character --file path.json
```

insert a Character from a CharacterData JSON file.

<sub>handler `AddCharacterCli`</sub>

### `--add-corponation`

```
prose --add-corponation --file path.json
```

insert a CorpoNation from a CorponationData JSON file.

<sub>handler `AddCorponationCli`</sub>

### `--add-doc`

```
prose --add-doc --title "…" --body-file path.md [--category essay] [--tags "a,b,c"] [--filename slug.md]
```

insert a worldbuilding Document directly into canon.

<sub>handler `AddDocCli`</sub>

### `--add-faction`

```
prose --add-faction --file path.json
```

insert OR update a Faction from a FactionData JSON file. Upsert: include "id" to update, omit to create. Safe service-layer path (FactionRepository.Save) — no hand-SQL, collision-safe slugs.

<sub>handler `AddFactionCli`</sub>

### `--add-news`

```
prose --add-news --file path.json
```

insert a News article from a NewsData JSON file.

<sub>handler `AddNewsCli`</sub>

### `--add-place`

```
prose --add-place --file path.json [--print]
```

insert OR update a Place/District from a DistrictData JSON file. Upsert: include "id" to update, omit to create. Safe service-layer path (DistrictRepository.Save) — no hand-SQL, collision-safe slugs.

<sub>handler `AddPlaceCli`</sub>

### `--add-weapon`

```
prose --add-weapon --file path.json
```

insert a Weapon from a WeaponryData JSON file.

<sub>handler `AddWeaponryCli`</sub>

### `--altitude-audit`

```
prose --altitude-audit (--slug <slug> | --all) [--force-synopsis]
```

three-altitudes agreement audit (designed story vs told story).

<sub>handler `AltitudeAuditCli`</sub>

### `--ambient-palette`

prose --ambient-palette --character <characterId> [--as-of date]

<sub>handler `AmbientPaletteCli`</sub>

### `--architecture-scan`

```
prose --architecture-scan [--json] [--out <file>] [--top <n>] [--force]
```

automated inventory of every service/DI-registration/CLI-verb/MCP-tool/script in the tree, plus name-overlap clusters worth a second look. See ArchitectureScanCli class doc.

<sub>handler `ArchitectureScanCli`</sub>

### `--archive-book`

```
prose --archive-book (--id ... | --slug ...) [--reason "..."] --universe <u>
```

snapshot a book's entire current live prose into ArchivedBooks — a pre-edit backup, read-only against the live content. See ArchiveBookCli class doc.

<sub>handler `ArchiveBookCli`</sub>

### `--ask`

```
prose --ask "Question" [--k 8] [--type character]
```

cloud RAG over the canon corpus. Replaces the retired Ollama path.

<sub>handler `AskCli`</sub>

### `--assemble-scene`

```
prose --assemble-scene (--beat <guid> | --text "<prose>") [--budget N]
```

set the ParentNodeId on an existing node (move it into a collection). X-Ray scene assembly (RFC 0002): print the entity roster + voice context block for a beat or raw prose. CLI twin of the MCP tool assemble_scene_context.

<sub>handler `AssembleSceneCli`</sub>

### `--audit-book`

prose --audit-book --slug <book-or-chapter-slug> [--deep] [--full] [--model <id>] [--out <path>] [--json] The "Player Piano" — one repeatable command running the full QA battery + the Structural Integrity Index (SII), a deterministic Findings rollup (BookHealthService). See AuditNodeCli.cs's own header comment for the authoritative, kept-in-sync tier list (10 FREE / 16 DEEP / 7 FULL checks as of 2026-08-30 — do not re-duplicate the list here, it drifted stale from BookHealthService.RunAsync once already). --model retargets the deep/full tier LLM calls (e.g. Haiku) for the run.

<sub>handler `AuditNodeCli` · **cost-gated (spends LLM money)**</sub>

### `--audit-consistency`

```
prose --audit-consistency [--json]
```

DataConsistencyService SSOT-drift audit (SQL-only, no LLM calls).

<sub>handler `DataConsistencyCli`</sub>

### `--audit-denorm`

```
prose --audit-denorm Entities.TagsJson
prose --audit-denorm Characters.Affiliation
```

report flat-vs-bridge drift for a denormalised column.

<sub>handler `AuditDenormCli`</sub>

### `--audit-drift`

```
prose --audit-drift           pretty-printed report
prose --audit-drift --json    JSON dump
```

report Character columns that disagree with their latest matching EntityStateEvents row. Lights up the static-vs-dynamic recipe only for columns that actually drifted.

<sub>handler `AuditDriftCli`</sub>

### `--auto-correct-nightly`

```
prose --auto-correct-nightly [--universe <slug>] [--dry-run] [--json]
```

the nightly AutoCorrect pass — pure ML/deterministic, zero LLM calls. Invoked by the Windows Task Scheduler task registered by scripts/register-autocorrect-task.ps1 at 2:00 AM Central every night. See AutoCorrectOrchestratorService for the pipeline.

<sub>handler `AutoCorrectNightlyCli`</sub>

### `--auto-correct-status`

```
prose --auto-correct-status [--list-runs]
```

list recent AutoCorrect runs and their undo state.

<sub>handler `AutoCorrectUndoCli`</sub>

### `--auto-correct-undo`

```
prose --auto-correct-undo (--run-id <guid> | --last-n <N>)
```

rewind a nightly AutoCorrect run (or the last N logged actions) via the undo ledger.

<sub>handler `AutoCorrectUndoCli`</sub>

### `--auto-run`

```
prose --auto-run (--slug <slug> | --id <guid>) [--effort draft|standard] [--dry-run] [--force]
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `AutoRunCli` · **cost-gated (spends LLM money)**</sub>

### `--backfill-character-relationships`

prose --backfill-character-relationships [--dry-run] [--json] One-time repair for CharacterRelationships.TargetEntityId never being resolved at save time.

<sub>handler `BackfillCharacterRelationshipsCli`</sub>

### `--backfill-character-state`

backfill EntityStateEvents from the dynamic columns currently sitting on Characters (Location, LifeStatus, Role, Affiliation, Belongings*, Territory*, DailyLife). One-shot, idempotent.

<sub>handler `StateBackfillCli`</sub>

### `--backfill-coverage`

prose --backfill-coverage --slug <book-or-chapter-slug> Populates BeatServiceLog + BeatModeLog for prose written before ProseWriterRouter existed, WITHOUT regenerating any beat. Runs the router's coverage-only path over each existing beat so --workflow-status has real logs to report.

<sub>handler `BackfillCoverageCli`</sub>

### `--backfill-entity-docs`

```
prose --backfill-entity-docs --slug <slug> [--text]
```

backfill entity-doc MarkdownFiles rows for a book's characters. Replays EntityDocService.InferFromTextAsync over every beat goal (+ prose text with --text) so future prose generation and the DCM viz see per-character entity docs.

<sub>handler `BackfillEntityDocsCli`</sub>

### `--backfill-entity-presence`

prose --backfill-entity-presence [--slug <slug>] [--dry-run] Re-runs SceneContextAssembler's name/alias/embedding scan (no LLM call) against already-written beats with no BeatEntities roster yet — lets a missing-alias fix take effect without a live generation pass.

<sub>handler `BackfillEntityPresenceCli`</sub>

### `--backfill-meaning`

prose --backfill-meaning --slug <slug> [--limit N] [--dry-run] Fill the MEANING coordinate (Beat.Description) for beats with prose but no meaning.

<sub>handler `BackfillMeaningCli`</sub>

### `--backfill-missing-characters`

```
prose --backfill-missing-characters
```

materialize relational rows for active characters that are blob-only (no Characters row) — the no-data-loss gate before dropping the Character blob. (RFC 0007)

<sub>handler `BackfillMissingCharactersCli`</sub>

### `--backfill-missing-subtype-rows`

prose --backfill-missing-subtype-rows [--dry-run] [--exclude-name "<name>"]... One-time data repair: inserts a minimal Characters/Places row for any character/place Entities row that has none (root cause: raw SQL writes bypassing the app — see BackfillMissingSubtypeRowsCli).

<sub>handler `BackfillMissingSubtypeRowsCli`</sub>

### `--backfill-pov`

prose --backfill-pov [--slug <slug>] [--dry-run] Heuristically tags each beat's highest-scoring character-type BeatEntities row as BeatEntityPresence PresenceType='pov' — closes the gap where DocContextService's per-beat voice-pinning and the SACRED-FLAW/VOICE-DRIFT audits had no POV data for most books. No LLM call. See BackfillPovCli.cs.

<sub>handler `BackfillPovCli`</sub>

### `--backfill-short-name-alias`

prose --backfill-short-name-alias [--universe glmz|scry|...] [--dry-run] Registers each multi-word-named character's first name as a CharacterAlias when missing — the root cause behind --backfill-entity-presence's low yield (prose refers to characters by first name; ScanNames only matches full Name or a registered alias). No LLM call.

<sub>handler `BackfillShortNameAliasCli`</sub>

### `--backfill-stubs`

```
prose --backfill-stubs
```

backfill Entities.Status = 'stub' / 'canon' based on BeatEntityMentions. Entities with no BeatEntityMentions row → Status='stub' (excluded from universe graph). Entities that ARE mentioned → Status='canon'. Re-run after --scan-entity-mentions.

<sub>handler `BackfillStubsCli`</sub>

### `--backfill-synopses` / `--backfill-structure-roles`

prose --backfill-synopses --slug <s> [--model <id>] [--force] prose --backfill-structure-roles --slug <s> [--force] Fill missing beat metadata without touching prose. Synopses via LLM (BeatGoal proxy for mode detection); StructureRole deterministically by book-global Save-the-Cat arc.

<sub>handler `BackfillBeatMetaCli`</sub>

### `--banned-names`

```
prose --banned-names --list
prose --banned-names --add --name <name> [--notes <notes>]
prose --banned-names --remove --id <id>
```

CRUD for BannedNames — Prose-wide hard name ban, enforced at write time.

<sub>handler `BannedNameCli`</sub>

### `--barks-export`

Portable-writing-service plan, Phase 4: narrow dialog-beat filter/export — see BarksExportCli's own doc comment.

<sub>handler `BarksExportCli`</sub>

### `--beat`

prose --beat <subcommand> — fine-grained beat manipulation: insert  --node <slug|id> [--after <beatId>] [--text "..."] delete  --id <beatId> [--node <slug|id>] update  --id <beatId> --text "..."  (use '-' for stdin) meta    --id <beatId> [--title "..."] [--kind "..."] [--description "..."] [--tone "..."] ... show    --id <beatId> list    --node <slug|id>

<sub>handler `BeatCli`</sub>

### `--beat-archive`

prose --beat-archive --beat-id <guid> The Beat Context Archive (observability Part F5): everything that fed one beat, resolved as-of that beat's own BeatContextTrace timestamp — prose, per-service trace, full LLM prompt/response, entity roster resolved to that moment's canon, DCM doc content as of that moment, and the bible section active at that time.

<sub>handler `BeatArchiveCli`</sub>

### `--beat-granularity`

prose --beat-granularity [--slug <slug> | --code <code> | --all] [--beats] Analyses beat-size distribution against the 4,000–7,500 char optimal range. Labels each beat as OK / SPLIT / MERGE and prints per-story stats. CPU-only — no LLM calls. Exit 0 = success.

<sub>handler `BeatGranularityCli`</sub>

### `--behavior-check`

prose --behavior-check --slug <nodeSlug> --character <characterId>

<sub>handler `BehaviorCheckCli`</sub>

### `--book`

book operations — list / new / show / chapters / absorb / review / apply / export / delete. Run `dotnet run --project Prose.Blazor -- --book` (no subcommand) to see full usage.

<sub>handler `BookCli`</sub>

### `--booktok`

```
prose --booktok --slug <slug> --provider kling|runway|sora [--duration 8] [--dry-run] [--yes]
prose --booktok --standalone --cover-path <path> --title "<title>" --provider kling|runway|sora
```

composite a book's cover onto a 3D mockup template, generate a short AI image-to-video clip (hand shows the cover, opens it, flips pages) via a chosen video provider (kling/runway/sora), and assemble a vertical 1080x1920 #booktok MP4. Costs real money per call unless --dry-run, which stops after the local ImageMagick mockup.


### `--browse-repository`

prose --browse-repository [--type <name>] [--search <text>] [--page N] [--format text|json] Browse entities by repository/type (built-in or custom) — no hand-written SQL required.

<sub>handler `BrowseRepositoryCli`</sub>

### `--burst-beats`

```
prose --burst-beats [--min-chars 800] [--node slug] [--kind book] [--dry-run]
```

burst oversized beats (e.g. chapter-as-one-beat from old book imports) into paragraph-sized pieces. Idempotent — already-small beats are skipped on rerun.

<sub>handler `BurstBeatsCli`</sub>

### `--canon-retrieve`

```
prose --canon-retrieve "<query>" [--k N] [--types t1,t2]
```

show what the universal canon reach pulls for a query, across ALL entity types — verifies the full-interconnect retrieval path.

<sub>handler `CanonRetrieveCli`</sub>

### `--causality-check` / `--affect-check` / `--interpersonal-check`

prose --causality-check / --affect-check / --interpersonal-check --slug <slug> [--json] "Behave like people" beat lenses: cause-effect (kill "and then"), emotion→action, and verbal+non-verbal interpersonal dynamics (the 90+ relational lever).

<sub>handler `BeatLensCli` · **cost-gated (spends LLM money)**</sub>

### `--chapters`

```
prose --chapters --slug <slug-or-code-or-id> [--json]
```

list a book's chapter units in reading order — the 100 ft rung of the Three Altitudes, previously unreachable from the CLI (every read path was a flat beat list, which is why a full-book read fell back to the one-line Description spine). Story Ledger Phase 1. Reuses SynopsisExportService's segmentation, so the listing matches story-synopsis.txt.

<sub>handler `ChaptersCli`</sub>

### `--character-gear`

```
prose --character-gear --character <name-or-id> [--bucket <b>] [--json]
prose --character-gear --character <name-or-id> --remove --id <rowId>
prose --character-gear --search "<text>" [--json]
```

surgical CRUD over a character's signature-gear / pharmaceuticals list. Added 2026-09-03 — there was no sanctioned way to remove ONE gear entry; create_character round-trips the whole record through the delete-all-and-reinsert mapper, so correcting a single invented item risked every other field. Logic lives in CharacterGearService, shared with the *_character_gear MCP tools. Universe-scoped (--character resolves through db.Characters), so it stays out of UniverseAgnosticCommands.

<sub>handler `CharacterGearCli`</sub>

### `--check-canon`

```
prose --check-canon (--slug <s> | --id <guid> | --all)
```

sweep a node's prose against canon (all entity types) and queue contradictions as approval-gated findings — the self-correction pass.

<sub>handler `CheckCanonCli`</sub>

### `--check-duplicate-beats`

prose --check-duplicate-beats --slug <nodeSlug> [--threshold 0.90] [--json] Corpus-wide near-duplicate-scene detector over prose embeddings (BeatDuplicateService). Candidate generator, not a verdict — verify by reading both beats before acting.

<sub>handler `CheckDuplicateBeatsCli`</sub>

### `--check-fidelity`

prose --check-fidelity (--slug <nodeSlug> | --id <nodeId>) [--json] Detects the Semantic Fidelity Gap — beats scoring high but drifting from the story's original meaning (Goodhart's Law in prose). Two checks: Bible alignment: prose vs Seed/Description (north-star drift) Intent alignment: prose vs beat Description (purpose drift) Files SEMANTIC-DRIFT findings; also runs automatically after every review.

<sub>handler `CheckFidelityCli`</sub>

### `--check-temporal-hygiene`

prose --check-temporal-hygiene [--json] Enforces (not just documents) the two rules that make re-enabling SYSTEM_VERSIONING on Nodes/Beats/BeatNodes safe: no IsEnabled/IsActive-style status-flag column on any versioned table, and no application query joins a live table to its own _History shadow. Run after any schema change touching a versioned table, not just once.

<sub>handler `TemporalHygieneCli`</sub>

### `--check-text-integrity`

prose --check-text-integrity [--fix] [--json]

<sub>handler `TextIntegrityCli`</sub>

### `--chekhov-audit`

prose --chekhov-audit --slug <nodeSlug> Chekhov's Gun audit: extract all concrete props/anchors/traits and test whether each earns its place. ORPHANED = appears with no payoff; DECORATION = repeated without new function; EARNS_IT = each appearance serves a distinct narrative purpose. Run before trimming any prose detail.

<sub>handler `ChekhovAuditCli` · **cost-gated (spends LLM money)**</sub>

### `--clone-book`

prose --clone-book (--id <guid> | --slug <slug>) [--title "New Title"] [--book-code SM1] [--draft] [--status ready]

<sub>handler `CloneNodeCli`</sub>

### `--close-all-sessions`

prose --close-all-sessions Called by the /commit skill before every commit to flush open edit sessions, run bible + blueprint sync for each, and draw a clean 3B coordination boundary.

<sub>handler `CloseAllSessionsCli`</sub>

### `--close-session`

prose --close-session (--slug <slug> | --session-id <guid>)

<sub>handler `CloseSessionCli`</sub>

### `--combat`

```
prose --combat --file scene.json [--out prose.txt]
prose --combat --location "Hegewisch" --objective "..." --exchanges 6 --tone Cinematic
```

generate a resource-tracked combat sequence via CombatSceneWriter.

<sub>handler `CombatCli`</sub>

### `--command-log`

prose --command-log [--since <dt>] [--handler <name>] [--take N] [--json] Read back the Command Ledger — every CLI/MCP/cost-gated call Prose.Hub has executed.

<sub>handler `CommandLogCli`</sub>

### `--commandment-audit`

prose --commandment-audit --slug <nodeSlug> [--json] Renamed from --book-audit (2026-08-30) — collided by verb/noun order with the unrelated --audit-book (the full QA battery); a typo silently ran the wrong tool. Audits a node against 7 commandments — gateway (PreviousNodeId=null) or sequel (PreviousNodeId set). Pass/warn/fail per commandment with fix hints. Exit 0 = all pass, 1 = advisory warnings, 2 = blocking failures.

<sub>handler `BookAuditCli`</sub>

### `--composite-cover-title`

```
prose --composite-cover-title --slug <slug>
```

redraw the title onto an already-saved cover image without calling an image-generation API again.

<sub>handler `CompositeCoverTitleCli`</sub>

### `--compute-metrics`

prose --compute-metrics [--slug <slug> | --all] CPU-only per-beat prose quality metrics: word count, sentence count, TTR, Flesch-Kincaid readability, dialogue proportion. Upserts into BeatProseMetrics. Safe to re-run nightly. Exit 0 = success.

<sub>handler `BeatProseMetricsCli`</sub>

### `--context`

```
prose --doc-context --slug <node> [--goal "<text>"] [--budget <tokens>]
prose --context add     --doc <path|guid> [--node <slug>]   Pin doc into prompts
prose --context exclude --doc <path|guid> [--node <slug>]   Exclude doc
prose --context remove  --doc <path|guid> [--node <slug>]   Remove override
prose --context clear   [--node <slug>]                     Clear all overrides
prose --context status                                       Show active overrides
```

Doc Context Stack dry-run — print the rotating cast of .md docs that WOULD load for a node + optional scene text (tier, reason, score, budget). Read-only. CLI mode: manage user context overrides for the DocContextStack.

<sub>handler `ContextCli`</sub>

### `--continuity`

unified continuity store — migrate / stats / contradictions / resolve / entity. Run `dotnet run --project Prose.Blazor -- --continuity` (no subcommand) to see full usage.

<sub>handler `ContinuityCli`</sub>

### `--coordinate`

prose --coordinate --slug <slug> [--json <path>] [--no-stamp] Full-coverage bible↔blueprint↔beat coordination: correlate every beat's meaning, construction, and prose; emit JSON + stamp the "## Beat Coordination Index".

<sub>handler `CoordinateCli`</sub>

### `--cost` / `--json` / `--reset`

```
prose --cost              print session cost table
prose --cost --json       emit summary as JSON
prose --cost --reset      clear the ledger
```

show running token cost tally for the current process. When appended to another command (e.g. prose --write-node --slug foo --cost), the cost of that command's LLM calls is printed after the command finishes.

<sub>handler `CostCli`</sub>

### `--coverage`

```
prose --coverage
```

per-entity-type reachability matrix (how much canon is embedded and thus pullable into prose). The standing gap-finder.

<sub>handler `CoverageCli`</sub>

### `--craft-checklist`

prose --craft-checklist --slug <slug> [--force] [--json] Reader-Proxy QA Instrument 2: binary craft/delight checklist per beat, hash-gated on Beat.TextHash + rule-set version (unchanged beats never re-bill). CRAFT §8 DON'Ts + "≥1 applicable DELIGHT move" + book-level move-monotony counters (DELIGHT §14). Findings persist as CraftChecklist. No scores. Exit 0 = clean, 1 = findings, 2 = error.

<sub>handler `BeatChecklistCli`</sub>

### `--create-book`

```
prose --create-book --title "..." [--code SRZR] [--kind book] [--description "..."] [--seed "..."] [--previous <slug|id>] [--parent <slug|id>]
```

create a new empty root node (bible-first; no beats yet).

<sub>handler `CreateNodeCli`</sub>

### `--create-repository`

```
prose --create-repository --name "Artifacts" [--category World] [--icon bi-box] [--description "..."]
```

create a runtime-defined repository (custom entity type).

<sub>handler `CreateRepositoryCli`</sub>

### `--create-universe`

```
prose --create-universe --slug <slug> --name <name> [--theme <theme>] [--description <text>]
```

create a new Universe row. See CreateUniverseCli.

<sub>handler `CreateUniverseCli`</sub>

### `--cross-book-consistency-audit`

prose --cross-book-consistency-audit [--since <hours>] Renamed from --consistency-audit (2026-08-30) — collided by word order with the unrelated --audit-consistency (DataConsistencyCli's SSOT-drift audit). Surfaces factual contradictions that span multiple story nodes by querying the existing ContinuityClaims table. CPU-only — no LLM calls. Exit 0 = clean, 1 = conflicts found.

<sub>handler `CrossBookConsistencyAuditCli`</sub>

### `--data-scan`

```
prose --data-scan --tool <name> [--apply] [--overwrite] --universe <slug>
```

DataScanUtility family (fix-phi/fix-identity/tag-lethality/tag-normalize/ assign-tiers/cross-reference) -- mass canon-entity maintenance tools. Defaults to a dry-run preview; pass --apply to actually write.

<sub>handler `DataScanCli`</sub>

### `--dcm-backfill`

prose --dcm-backfill --slug <slug> [--dry-run] Retroactive DCM footprint for books written OUTSIDE the engine (update_beat_text / --edit-beat / --import-md bypass ProseWriterRouter, so step-0 entity inference never ran — PURSUED shipped 127 beats with zero entity docs this way). Runs EntityDocService.InferFromTextAsync over every enabled beat's prose; hash-gated, no prose touched. Run after --generate-node-doc + --sync-markdown.

<sub>handler `DcmBackfillCli`</sub>

### `--dcm-viz`

```
prose --dcm-viz --slug <slug> [--out <dir>]
```

DCM lifecycle visualization — dry-run context pass + Gantt .htm export.

<sub>handler `DcmVizCli`</sub>

### `--decision-log`

prose --decision-log [--since <dt>] [--session <id>] [--take N] [--json] Read back the Decision Ledger written by --log-decision.

<sub>handler `DecisionLogCli`</sub>

### `--delete-alias`

```
prose --delete-alias --value "<alias>" [--type <character|place|…>] [--apply]
```

remove a bad entity alias row (dry-run unless --apply). The sanctioned fix for alias pollution — an ordinary phrase registered as an alias, which makes EntityMentionScanner tag that phrase as the entity corpus-wide.

<sub>handler `DeleteAliasCli`</sub>

### `--delete-entity-cluster`

prose --delete-entity-cluster --root <entityGuid> --universe <slug> --confirm <entityCount> The execution half of --export-entity-cluster — hard-deletes the reviewed cluster after re-verifying the count and checking every entity for outside references. See DeleteEntityClusterCli.

<sub>handler `DeleteEntityClusterCli`</sub>

### `--delete-node`

prose --delete-node --id <guid>   Hard-delete a node and its BeatNode memberships. Beats that are exclusively owned by this node are also deleted. HARD RULE: never use raw sqlcmd DELETE on Nodes — use this command instead.

<sub>handler `DeleteNodeCli`</sub>

### `--deprecated-names`

```
prose --deprecated-names --list [--universe <slug>]
prose --deprecated-names --add --universe <slug> --name <deprecatedName> --canonical <canonicalName> [--notes <notes>]
prose --deprecated-names --remove --id <id>
```

CRUD for DeprecatedEntityNames (list/add/remove).

<sub>handler `DeprecatedNameCli`</sub>

### `--description-drift`

```
prose --description-drift --slug <slug-or-code-or-id> [--json]
prose --description-drift --all --universe <slug>
```

report beats whose Beat.Description was verified against prose that has since changed (DescriptionHash != TextHash). Deterministic — no LLM, no embeddings, no cost. Report-only per docs/LOGIC.md §4. Story Ledger Phase 1.

<sub>handler `DescriptionDriftCli`</sub>

### `--diagnose-book`

prose --diagnose-book --slug <nodeSlug> [--json] Pre-flight structural analysis before running the review panel. Runs 12 targeted checks (antagonist cost, protagonist behavior change, exposition density, etc.) and reports Pass/Warn/Fail with evidence + fixes. Exit 0 = ready, 1 = warnings, 2 = blocking failures.

<sub>handler `DiagnoseNodeCli`</sub>

### `--doc-context`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `DocContextCli`</sub>

### `--doc-context-hook`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `DocContextHookCli`</sub>

### `--dry-run`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `BookTokCli`</sub>

### `--dual-read`

```
prose --dual-read --old <slug|id> --new <slug|id> [--panel <name>] [--readers N]
```

dual-read comparative review — the SAME pinned panel grades both versions of a story; pairs scores per reader (within-reader delta cancels taste bias) → keep/revert/merge verdict.

<sub>handler `DualReadCli`</sub>

### `--duel`

prose --duel --beat-id <guid> --candidate <file> [--goal "..."] [--apply] [--json] Blind A/B duel: beat's current prose vs a candidate revision. 3 voters (register/goal/reader lenses), three-way ballot; replace needs >=2 better with zero dissent; splits escalate to 7 voters with written rationales. Verdicts hash-cached by text pair. SS-A44: invoking this IS the explicit ask. Exit 0 = replace, 1 = keep, 2 = error.

<sub>handler `BeatDuelCli`</sub>

### `--duplicate-book`

```
prose --duplicate-book (--id <guid|prefix> | --slug <slug>) --title "New Title"
```

deep-duplicate a node (and its sub-node tree) into a fresh, independent copy — every beat cloned to a new row (prose + metadata kept; audio/score/stale reset). Editing the copy never touches the original.

<sub>handler `DuplicateNodeCli`</sub>

### `--duplicate-entity-scan`

prose --duplicate-entity-scan --universe <slug> [--json] Deterministic scan for duplicate/near-duplicate character Entity names within a universe that aren't explained by legitimate cross-book OriginNodeId disambiguation. No LLM. Exit 0 = none found, 1 = candidates found (informational — read the prose before merging).

<sub>handler `DuplicateEntityScanCli`</sub>

### `--duplicate-entity-scan-broad`

prose --duplicate-entity-scan-broad --universe <slug> [--entity-type <type>] [--json] LLM-assisted scan for duplicate rows the deterministic scan above cannot catch (a title/rank/ code suffix or otherwise different name for the same entity, not a 1-character typo). Two-stage cost-bounded design — see DuplicateEntityScanService.ScanBroadAsync. Report-only, costs real LLM calls — gated like other LLM-calling commands.

<sub>handler `DuplicateEntityScanBroadCli` · **cost-gated (spends LLM money)**</sub>

### `--edit-beat`

```
prose --edit-beat --slug <slug> (--beat-number N | --insert-after N) --file <path>
```

expand planned beats in a node to prose (headless ✨ for each beat).

<sub>handler `EditBeatCli`</sub>

### `--edit-book`

```
prose --edit-book (--id <guid|prefix> | --slug <slug>) [--top N]
```

review-driven auto-editor. Weight the latest reviews, target the lowest / most-flagged beats (raise the floor), and emit conservative before/after rewrite PROPOSALS (JSON) for an approval survey. Nothing is written.

<sub>handler `EditNodeCli`</sub>

### `--ensure-chapter`

prose --ensure-chapter --slug <slug> | --all Enforce "every story has >= 1 chapter": wrap a flat story's direct beats into a single ChapterNode child (no-op if already chaptered). No LLM.

<sub>handler `EnsureChapterCli`</sub>

### `--entity-mentions`

```
prose --entity-mentions --entity <id|slug> [--limit <n>]
```

list every beat that mentions a given entity (node, beat number, excerpt).

<sub>handler `EntityMentionsCli`</sub>

### `--entity-relationships`

```
prose --entity-relationships --character <name-or-id> [--json] [--orphans]
prose --entity-relationships --character <name-or-id> --remove --id <rowId>
prose --entity-relationships --character <name-or-id> --add --target <name> --type <type>
```

surgical CRUD over a character's CharacterRelationships rows. Added 2026-09-02 — there was previously NO sanctioned way to remove a single relationship row (see EntityRelationshipCli's doc comment and the Seo Jisun cross-book contamination). Deliberately universe-scoped: --character resolves through db.Characters, which the Entity query filter scopes, so this stays out of UniverseAgnosticCommands.

<sub>handler `EntityRelationshipCli`</sub>

### `--entity-tags`

```
prose --entity-tags --entity <guid-or-name> [--json]
prose --entity-tags --entity <guid-or-name> --remove "tag1,tag2"
prose --entity-tags --entity <guid-or-name> --add "tag1,tag2"
```

an entity's tags — list / add / REMOVE. Added 2026-09-03: tags could be added and never taken away (create_character's `tags` MERGES, like aliases), so a wrong tag was permanent, and a stale book tag can pull a character into that book's context loads. NOT the same as --tag-entities, which rewrites inline <entity guid="…"> markup inside beat text.

<sub>handler `EntityTagsCli`</sub>

### `--entity-tree`

prose --entity-tree (--id <guid> | --slug <slug>) [--depth N] [--rel-types type1,type2] [--as-of date]

<sub>handler `EntityTreeCli`</sub>

### `--estimate-cost`

prose --estimate-cost [--beats <N>] [--pov-characters <M>] [--tier free|deep|full] Cost-governance check (RFC 0009 §9.5, 2026-08-13): prints the LLM call count implied by BookHealthService's current wiring for a book of N beats — no DB access, pure arithmetic against the tier shapes read directly out of the code. Run this before merging a new per-beat service so the cost jump is visible before it ships, not discovered by totaling a bill months later.


### `--examine-emotion`

prose --examine-emotion --slug <nodeSlug> [--effort draft|standard|deep] [--json] Emotional Intelligence Examination (SS-A15): 8-dimension 0–4 rubric, per-beat curve, character ledger (Want/Need/Wound/Flaw), register-adaptive anchors. Exit 0 = none blocking, 1 = advisory issues, 2 = blocking dimensions open.

<sub>handler `ExamineEmotionCli` · **cost-gated (spends LLM money)**</sub>

### `--exclusion-rules`

```
prose --exclusion-rules [--all] [--json]
prose --exclusion-rules --propose --predicate-a <p> --predicate-b <p> --why "..." [--universal]
prose --exclusion-rules --approve|--reject --id <n>
prose --exclusion-rules --test --predicate-a <p> --object-a "..." --predicate-b <p> --object-b "..."
```

manage the PredicateExclusion ontology the Tuned Read runs on — list, propose, approve/reject, and --test a rule against a hypothetical claim pair before approving it. Deterministic and free; no LLM call anywhere in this command.

<sub>handler `ExclusionRulesCli`</sub>

### `--expand-beat`

```
prose --expand-beat (--slug <slug> | --id <guid>) [--beat <beatId>] [--force]
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `ExpandBeatCli`</sub>

### `--export`

```
prose --export global                every repo, zipped + timestamped
prose --export <repoName>            one repo, zipped (e.g. "people", "weaponry")
prose --export <entityId>            one entity, plain .json
```

dump canon JSON to the user's Downloads folder.

<sub>handler `ExportCli`</sub>

### `--export-commands`

```
prose --export-commands [<output-path>] [--source <path-to-Program.cs>]
```

Documentation generator, deliberately ABOVE the Hub gate: it reads this file's own dispatch chain and writes a markdown reference. It touches no database and needs no running Hub, and requiring one would make the CLI reference unbuildable in exactly the situation where you most want to read it. Mirrors `Prose.Mcp -- --export-tools`, which closes the asymmetry docs/ARCHITECTURE.md §8 recorded as an open gap.


### `--export-entity-cluster`

prose --export-entity-cluster --root <entityGuid> --universe <slug> --out <path.md> Report-only: walks the full connected component from --root and archives it to Markdown — the review step before --delete-entity-cluster. See ExportEntityClusterCli.

<sub>handler `ExportEntityClusterCli`</sub>

### `--export-event-list`

prose --export-event-list --slug <slug> Write the current per-beat event list to {CODE}-Events.txt in the publish-export folder (no LLM call).

<sub>handler `ExportEventListCli`</sub>

### `--export-node`

```
prose --export-node (--id <guid|prefix> | --slug <slug>) [--author "Name"]
```

render a node to .docx + .epub + .pdf + .txt + metadata artifacts (description.txt, story-synopsis.txt, <CODE>-dcm-viz.htm). Local file rendering only — no KDP API integration, hence "export" not "publish".

<sub>handler `ExportNodeCli`</sub>

### `--export-personas-json`

prose --export-personas-json [--out <path>] Exports all 1024 Legion persona details + OCEAN psychometric profiles to JSON for consumption by the Python ML package (v3/ml/artifacts/personas.json).


### `--export-synopsis`

```
prose --export-synopsis (--slug <slug> | --all) [--force]
```

chapter-by-chapter synopsis export (also runs inside --export-node).

<sub>handler `ExportSynopsisCli`</sub>

### `--extract-beat-locations`

prose --extract-beat-locations --slug <slug> [--force] [--limit N] [--dry-run] Backfill the per-beat scene location (Beat.PlaceName / PlaceEntityId) — hash-gated.

<sub>handler `ExtractBeatLocationsCli`</sub>

### `--fact-ledger-refresh`

prose --fact-ledger-refresh --slug <slug-or-code> — zero-LLM-cost re-run of just the fact-ledger check (see FactLedgerRefreshCli's own doc comment). Not cost-gated: it is the deliberate cheap alternative to the cost-gated --audit-book --deep bundle.

<sub>handler `FactLedgerRefreshCli`</sub>

### `--family`

```
prose --family parent  --parent <id|slug> --child <id|slug>
prose --family sibling --a <id|slug> --b <id|slug>
prose --family spouse  --a <id|slug> --b <id|slug>
prose --family show    --of <id|slug>
```

family ties — hand-seed parent/sibling/spouse links between characters.

<sub>handler `FamilyCli`</sub>

### `--family-gen`

```
prose --family-gen propose --of <id|slug>           dry run
prose --family-gen propose --of <id|slug> --commit  write characters + edges + propagate genetics
```

propose a plausible immediate family for one character. --seed N for reproducible RNG

<sub>handler `FamilyGenCli`</sub>

### `--find-entity`

```
prose --find-entity --name "<text>" [--type character] [--universe <slug>] [--limit N]
```

search seeded entities by name or alias — the read-side counterpart to --add-character, so authoring can check for an existing entity before creating a duplicate.

<sub>handler `FindEntityCli`</sub>

### `--findings`

findings inbox — list / show / apply / dismiss / scan.

<sub>handler `FindingsCli`</sub>

### `--findings-staleness`

prose --findings-staleness [--json] RFC 0011 Brick 2: generic staleness report across every Findings category that stamps SourceRuleVersion on write (currently CraftChecklist + StructuralFailure).

<sub>handler `FindingsStalenessCli`</sub>

### `--fix-bad-name-matches`

prose --fix-bad-name-matches [--dry-run] Deletes BeatEntities rows where MatchSource='name' but the entity's Name no longer appears in the beat's current Text (a checkable, unambiguous staleness signal name-matches carry that embedding/graph matches don't). See FixBadNameMatchesCli for root-cause detail.

<sub>handler `FixBadNameMatchesCli`</sub>

### `--fix-cross-universe-contamination`

prose --fix-cross-universe-contamination [--dry-run] Deletes BeatEntities/BeatEntityPresence rows whose entity belongs to a different universe than the beat's own book (a hard "Universe division absolute" violation) — historical bad data, not a live matching-pipeline bug (see FixCrossUniverseContaminationCli for root-cause detail).

<sub>handler `FixCrossUniverseContaminationCli`</sub>

### `--fix-self-aliases`

Corpus-wide repair: remove CharacterAlias/PlaceAlias/FactionAlias/WeaponAlias rows whose Value matches their own owning entity's Name — a redundant self-alias, usually a leftover from an entity merge that relinked a loser's alias onto the winner it now duplicates.

<sub>handler `FixSelfAliasesCli`</sub>

### `--gear-check`

prose --gear-check --slug <nodeSlug> --character <characterId> [--story-time date]

<sub>handler `GearCheckCli`</sub>

### `--generate-blueprint`

prose --generate-blueprint --slug <nodeSlug> [--retrofit] [--json] Generates the StructuralBlueprint — pre-prose anti-tell commitments (subplot, temporal scheme, resolution mode, escalation curve, event palette, ending, intertextual anchors). StoryScope countermeasures; bible → blueprint → prose. --retrofit infers the blueprint from already-written prose.

<sub>handler `GenerateBlueprintCli` · **cost-gated (spends LLM money)**</sub>

### `--generate-book-glossary`

```
prose --generate-book-glossary --slug <slug>
prose --generate-book-glossary --all
```

regenerate a book's Glossary (docs/nodes/{CODE}-Glossary.htm/.json/.txt) — the subset of its universe's Master Glossary whose terms appear in the book's live prose.

<sub>handler `GlossaryCli`</sub>

### `--generate-book-outline`

```
prose --generate-book-outline --slug <slug> [--beats N] [--replace-beats]
```

(re)generate the node bible for an existing node. Renamed from --book-outline (2026-08-30) — too easily confused with the read-only --get-book-outline; this one calls an LLM and can destructively regenerate the bible.

<sub>handler `NodeOutlineCli`</sub>

### `--generate-canon-md`

```
prose --generate-canon-md --type <WorldBible|WorldMaster|Franchise|UniverseCanon>
prose --generate-canon-md --all
```

regenerate canon document .md files from DB (CanonDocuments + CanonDocumentSections). The disk files are generated read-only mirrors; source of truth is the DB.

<sub>handler `CanonDocumentCli`</sub>

### `--generate-cover-image`

```
prose --generate-cover-image --slug <slug> --provider openai|stability|google
```

render Node.CoverPrompt through an image provider (openai/stability/google) and save the cover under the media dir. Costs real money — requires an API key.

<sub>handler `GenerateCoverImageCli`</sub>

### `--generate-cover-prompt`

```
prose --generate-cover-prompt --slug <slug>
prose --generate-cover-prompt --all
```

generate Node.CoverPrompt (image-model cover description) from the book's own Title/Summary/Description/universe.

<sub>handler `GenerateCoverPromptCli` · **cost-gated (spends LLM money)**</sub>

### `--generate-event-list`

prose --generate-event-list --slug <slug> [--force] [--limit N] [--dry-run] [--model <id>] Fill the per-beat plot-EVENT one-liner (Beat.EventSummary) — "what happened".

<sub>handler `GenerateEventListCli`</sub>

### `--generate-glossary`

```
prose --generate-glossary --universe <slug>   (omit --universe for all)
```

regenerate a universe's Master Glossary (Glossary.htm/.json/.txt under docs/universes/{SLUG}/) from the GlossaryTerms table.

<sub>handler `GlossaryCli`</sub>

### `--generate-node-doc`

```
prose --generate-node-doc --slug <slug>
prose --generate-node-doc --all
```

assemble the unified Book Context Document for a node. Merges hand-authored NodeOutline + Structural Blueprint + Beat Spine into one document, writes the merged view to docs/nodes/{CODE}.md (read-only disk mirror) only. Nodes.NodeOutline itself stays pure hand-authored content (fixed 2026-08-14 — it used to get the merged blob written back, so the column named "the bible" stopped meaning only the bible).

<sub>handler `NodeDocCli`</sub>

### `--generate-scene`

Portable-writing-service plan, Phase 2: write a scene/line of dialog without a pre-existing Book/Chapter/Beat row — see OneShotGenerateCli's own doc comment.

<sub>handler `OneShotGenerateCli`</sub>

### `--genetics`

```
prose --genetics propagate                     full graph
prose --genetics propagate --id <id|slug>      single character
prose --genetics propagate --seed 42           reproducible RNG
```

propagate genetic_ancestry from parents to children via the family graph (with ±5% recombination noise). Currently a no-op until family ties are seeded.

<sub>handler `GeneticsCli`</sub>

### `--get`

prose --get <type> <name-or-id> — targeted entity lookup. Types: character | place | weapon | faction | corponation

<sub>handler `GetEntityCli`</sub>

### `--get-book-outline`

```
prose --get-book-outline --slug <slug|code|guid> [--out <path>]
```

dump the node bible VERBATIM (the read half of --set-book-outline's round trip). NOT --generate-book-outline (renamed from --book-outline 2026-08-30), which generates a fresh bible via an LLM instead of reading the existing one.

<sub>handler `GetBookOutlineCli`</sub>

### `--get-canon-section`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `GetCanonSectionCli`</sub>

### `--get-place`

```
prose --get-place --name "<exact name>" [--print-raw]
```

read a Place/District's full DistrictData record by exact name — the read-side counterpart to --add-place, so a rename/edit can round-trip the existing record instead of upserting blind and clobbering fields the caller didn't know about.

<sub>handler `GetPlaceCli`</sub>

### `--gpu`

```
prose --gpu <status|stop|start|destroy> [--instance <id>]
```

manage the rented vast.ai review box (key from the MindAttic vault, provider 'vast').

<sub>handler `VastGpuCli`</sub>

### `--graph-health`

```
prose --graph-health --universe <slug> [--json]
```

GraphHealthService — orphaned/weakly-connected/malformed world-graph node audit.

<sub>handler `GraphHealthCli`</sub>

### `--grep-beats`

```
prose --grep-beats --pattern "<text>" [--case-sensitive]
```

plain substring search over every Beat.Text corpus-wide (all universes) — read-only, no LLM cost. Built to check whether a defect found in one book (e.g. leaked LLM repair-pass scaffolding) also hit others.

<sub>handler `GrepBeatsCli`</sub>

### `--harvest-entities`

```
prose --harvest-entities --file <path> [--universe glmz] [--dry-run]
```

harvest entities + edges from open text (design notes, canon briefs). Routed BEFORE the bare --universe command: --universe here is the scope flag, not a subcommand.

<sub>handler `HarvestEntitiesCli` · **cost-gated (spends LLM money)**</sub>

### `--harvest-voice`

```
prose --harvest-voice (--slug <s> | --id <id> | --all-80 | --pending | --apply <guid> | --reject <guid>) [--force]
```

distill voice rules from winning (≥80%) nodes into the codified DB-backed rules the generator reads. Propose-then-approve.

<sub>handler `HarvestVoiceCli` · **cost-gated (spends LLM money)**</sub>

### `--hook-audit`

prose --hook-audit --slug <slug> [--dry-run] Chapter-ending hook strength analysis; weak non-final endings file findings.

<sub>handler `HookAuditCli`</sub>

### `--image-prompts`

```
prose --image-prompts regen --id <id|slug> [--force]
prose --image-prompts regen --all-changed
```

rewrite ethnicity-keyed visual descriptors in image prompts to match a character's current genetic_ancestry. Cost-aware via stored hash.

<sub>handler `ImagePromptsCli`</sub>

### `--import-book`

```
prose --import-book --file path.node [--title ...] [--kind ...] [--slug ...] [--parent ...] [--dry-run]
```

import a hand-authored .node file (beat + gap + beat …) into a fresh node. The complement to --write-story (LLM-generated): this is for drafts written elsewhere (chat exports, transcripts, paper notes typed up). See ImportNodeCli class doc for the file format.

<sub>handler `ImportNodeCli`</sub>

### `--import-cover`

```
prose --import-cover --file PATH [--book-code CODE] [--type TYPE] [--notes TEXT] [--dry-run]
```

import a local image file (png, jpg, webp) into the Media table. Optionally links to a node by --book-code and sets the media type.

<sub>handler `ImportCoverImageCli`</sub>

### `--import-md`

```
prose --import-md --file path.md [--dry-run]
```

reimport an edited --publish-md Markdown file back into the DB. Each <!-- beat:N:id7 --> marker identifies the beat; prose between markers updates Beat.Text.

<sub>handler `ImportMarkdownCli`</sub>

### `--interpret`

```
prose --interpret --text "..."  | --file path.txt
```

prose → entities + edges. LLM-driven. add --commit to apply, --auto-create to stub missing entities, --tag <source>

<sub>handler `InterpretCli`</sub>

### `--kdp-manifest`

```
prose --kdp-manifest [--out <path>] [--userscript]
```

Reconciles DB + disk + tools/kdp/title-ids.json into tools/kdp/manifest.json (the ground truth for what needs to go up on KDP). --userscript also regenerates tools/kdp/kdp-panel.user.js from tools/kdp/kdp-panel.template.js.

<sub>handler `KdpManifestCli`</sub>

### `--kdp-mark-published`

```
prose --kdp-mark-published --slug <slug> [--url <amazonUrl>] [--title-id <id>]
```

Closes the loop after a republish actually completes on KDP.

<sub>handler `KdpMarkPublishedCli`</sub>

### `--kdp-status`

```
prose --kdp-status
```

Show KDP publication status: Published / Outdated / WorkInProgress for all tracked nodes. Outdated = published but beats edited since last KDP push.

<sub>handler `KdpStatusCli`</sub>

### `--legion`

```
prose --legion ask "Q" --options "A,B,C"  → forced-choice Quorum decision (JSON on stdout)
prose --legion vote "Q" [--context "…"]    → open-ended vote with synthesized narrative
```

query the Legion / LLMVoting cloud-LLM panel directly.

<sub>handler `LegionCli`</sub>

### `--lesson-add`

```
prose --lesson-add --scope <scope> --kind <kind> --text "<text>"
```

add an author ruling to the prose-lessons memory store. Lessons are injected into review ballot prompts so reviewers don't penalise beats the author has already ruled are doing their job. Scope: global | node:<slug> | beat:<guid> Kind:  score-vs-function | delight | voice | pacing | continuity | other

<sub>handler `ProseLessonCli`</sub>

### `--lessons-list`

```
prose --lessons-list [--scope <scope>]
```

list prose lessons (all scopes or filtered).

<sub>handler `ProseLessonCli`</sub>

### `--liberty-report`

prose --liberty-report [--beat <guid> | --slug <slug>] Show liberty analysis + Rule of Cool findings for a beat or all beats in a story.

<sub>handler `LibertyReportCli`</sub>

### `--link-weapon-ammo`

```
prose --link-weapon-ammo [--local-url URL] [--local-key KEY] [--local-model TAG] [--dry-run]
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `LinkWeaponAmmoCli`</sub>

### `--lint-prose`

prose --lint-prose --slug <slug> [--dry-run] Deterministic prose linter: echo words, crutch phrases, pet words, dialogue-attribution runs.

<sub>handler `LintProseCli`</sub>

### `--list-archives`

```
prose --list-archives (--id ... | --slug ...) --universe <u>
```

list every ArchivedBook snapshot for a node, newest first — read-only, use to find the archive-id for --restore-node-field. See ListArchivesCli class doc.

<sub>handler `ListArchivesCli`</sub>

### `--list-books`

```
prose --list-books [--status <s>] [--kind <k>] [--search <text>] [--limit <n>] [--json]
```

list every node as a table (or JSON). Headless twin of /nodes.

<sub>handler `ListNodesCli`</sub>

### `--list-canon-sections`

```
prose --list-canon-sections --type <DocumentType> [--universe <slug>]
prose --set-canon-section --type <DocumentType> --key <sectionKey> --file <path.md> [--title <t>] [--universe <slug>]
```

read/edit CanonDocumentSections directly — the CLI equivalent of the MCP tools list_canon_sections / set_canon_section, built 2026-08-23 to close the gap where canon editing was MCP-only and unreachable from a CLI-only session.

<sub>handler `ListCanonSectionsCli`</sub>

### `--list-sessions`

prose --list-sessions --slug <slug> [--limit N]

<sub>handler `ListSessionsCli`</sub>

### `--list-species`

prose --list-species — print the species taxonomy (canonical name, label, sentience).

<sub>handler `ListSpeciesCli`</sub>

### `--list-surveys` / `--get-survey`

```
prose --list-surveys [--status Open|Completed]
prose --get-survey --slug <slug>
```

survey management.

<sub>handler `SurveyCli`</sub>

### `--location-scan`

prose --location-scan [--min-travel-minutes N] Character-in-two-places-at-once contradiction scan; conflicts land in Findings.

<sub>handler `LocationScanCli`</sub>

### `--log-decision`

prose --log-decision --summary "..." [--rationale "..."] [--category ...] [--related id,id] Durable "why" record — the Decision Ledger half of the Command/Decision Ledger pair (CommandLedgerEntries is written automatically by every dispatch; this is explicit).

<sub>handler `LogDecisionCli`</sub>

### `--log-search`

prose --log-search [--since <dt>] [--severity <lvl>] [--text <q>] [--take N] [--json] Durable, searchable log history (Serilog daily files) — not the live in-memory tail.

<sub>handler `LogSearchCli`</sub>

### `--logic-sweep`

prose --logic-sweep --slug <nodeSlug> [--json] Codifies docs/LOGIC.md's six-dimension sweep (SS-A44) as one LLM call per dimension: causality chain, knowledge states, timeline, plant/payoff (two-way), orphan references, bible agreement. A single-pass approximation over the whole node's prose — for a large book or a thorough pass, prefer the /logic-sweep Claude Code skill (range-scoped subagents + quote verification + fix + re-verify). Findings persist to Findings and auto-heal on re-run. Exit 0 = clean, 1 = MODERATE/MINOR only, 2 = any BLOCKER.

<sub>handler `LogicSweepCli`</sub>

### `--make-group`

```
prose --make-group --name "Group B" [--size 128]
```

create a fixed, named reviewer panel of N personas, disjoint from every existing focus group (no persona on two panels). No LLM calls.

<sub>handler `MakeGroupCli`</sub>

### `--mark-canon`

```
prose --mark-canon (--slug <s> | --id <guid>) [--off]
```

author-only Canon trust gate — mark a node strong enough to draw conclusions about its characters/events (the voice-harvest learns from canon).

<sub>handler `MarkCanonCli`</sub>

### `--merge-edge`

prose --merge-edge --keep <edgeId> --dedupe <edgeId> [--as <canonicalRelationType>] [--register-alias] The execution half of --scan-edge-duplicates — collapses two Edge rows describing the same relationship under different wording into one. See MergeEdgeCli.

<sub>handler `MergeEdgeCli`</sub>

### `--merge-entity`

prose --merge-entity --winner <guid> --loser <guid> The execution half of the report-only duplicate-scan tools — a human, having confirmed two rows are the same identity from real book/prose knowledge, executes the merge. No LLM call, no fuzzy matching. See MergeEntityCli.

<sub>handler `MergeEntityCli`</sub>

### `--migrate-blueprint-rows`

```
prose --migrate-blueprint-rows [--slug <slug>] [--dry-run]
```

Truth-First Architecture — Step B2: decompose EscalationCurveJson / EventTypePaletteJson blobs and BeatTags into per-beat BeatBlueprintDecision rows. Idempotent; skips beats that already have a decision row.

<sub>handler `MigrateBlueprintRowsCli`</sub>

### `--migrate-canon-docs`

```
prose --migrate-canon-docs [--dry-run]
```

Truth-First Architecture — Step A2: migrate hand-editable canon .md files (BIBLE.md, WORLD.md, FRANCHISE.md, universes/CAUL.md) into CanonDocument + CanonDocumentSection DB rows. Idempotent; skips already-migrated documents.

<sub>handler `MigrateCanonDocsCli`</sub>

### `--migrate-legacy-book-chapter`

```
prose --migrate-legacy-book-chapter
```

delete the 44 legacy book/chapter Entity+Records blobs whose content already lives in the Nodes/Beats model. Classifies each as JUNK, REDUNDANT, or ORPHAN (converts orphans to Nodes before deleting).

<sub>handler `MigrateLegacyBookChapterCli`</sub>

### `--migrate-nodes`

```
prose --migrate-nodes
```

migrate legacy Books/Chapters/ChapterBeats/Episodes/EpisodeBeats data into the unified Beat/Node schema. Idempotent — safe to re-run.

<sub>handler `MigrateNodesCli`</sub>

### `--migrate-sql`

```
prose --migrate-sql --schema           apply EF migrations
prose --migrate-sql --import people    import character JSON files
prose --migrate-sql --all              schema + import all supported types
```

SQL Server migration — apply EF migrations and import JSON entities.

<sub>handler `MigrateSqlCli`</sub>

### `--morning-report`

prose --morning-report [--since <hours>] Aggregates overnight findings: cross-story contradictions, new Findings, prose metrics outliers, near-duplicate alerts, score correlation, leaderboard. Writes HTML to PublishExportDirectory. Default window: 24h. Exit 0 always.

<sub>handler `MorningReportCli`</sub>

### `--move-beat`

```
prose --move-beat --slug <slug> --beat-number N --after M   (M=0 moves to the top)
```

re-slot a beat within its node's reading order (wraps NodeWorkbenchService .MoveBeatAsync, previously reachable only from the Blazor drag-and-drop UI).

<sub>handler `MoveBeatCli`</sub>

### `--move-beat-to-node`

```
prose --move-beat-to-node --slug <from-slug> --beat-number N --to-slug <to-slug> --after M
```

relocate a beat OUT of one chapter and INTO another (--move-beat only re-slots within a single node's existing siblings). Wraps NodeWorkbenchService.MoveBeatToNodeAsync.

<sub>handler `MoveBeatToNodeCli`</sub>

### `--move-node-universe`

```
prose --move-node-universe (--slug <slug> | --id <id>) --to-universe <universeSlug>
```

relocate a book node (and its full descendant chapter subtree) into a different universe. See MoveNodeUniverseCli.

<sub>handler `MoveNodeUniverseCli`</sub>

### `--narrate-book`

```
prose --narrate-book (--id <guid|prefix> | --slug <slug>)
```

(re)narrate an EXISTING node by id (full or prefix) or slug. Runs the same NarrateAsync path the Record button uses. Use to re-record a node whose beats failed (e.g. a TTS 400) without regenerating prose.

<sub>handler `NarrateNodeCli`</sub>

### `--narrative-science`

```
prose --narrative-science sacred-flaw --character <slug|id> [--scaffold]
prose --narrative-science dramatic-question (--slug <s> | --id <beatId>) [--character <slug|id>]
prose --narrative-science five-act --slug <nodeSlug>
```

Will Storr narrative-science frameworks — sacred flaw, dramatic question, five-act structure. Three subcommands (scene-anatomy removed 2026-08-13 — redundant per-beat cost sink with no automated caller, see NarrativeScienceService.cs): (add --json to any subcommand for raw JSON output)

<sub>handler `NarrativeScienceCli`</sub>

### `--orphan-beats`

prose --orphan-beats [--min-number N] [--max-number N] [--limit N] [--contains "text"] — read-only diagnostic: Beats rows with no BeatNodes membership. See OrphanBeatsCli's own doc comment for why this exists (VIGL fact-ledger investigation, 2026-09-01).

<sub>handler `OrphanBeatsCli`</sub>

### `--plant-audit` / `--list-plants` / `--add-plant`

prose --plant-audit   --slug <node> [--json]   audit plant/payoff pairs prose --list-plants   --slug <node> [--json]   list all pairs prose --add-plant     --slug <node> --plant "..." --payoff "..." [--cat detail]

<sub>handler `PlantPayoffCli`</sub>

### `--populate-queue`

```
prose --populate-queue --entity-review|--story-review|--beat-write|--status [options]
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `PopulateQueueCli`</sub>

### `--pov-audit`

prose --pov-audit --slug <slug> [--dry-run] Head-hopping + same-scene voice-sameness audit (batched Haiku; findings loop back).

<sub>handler `PovVoiceAuditCli`</sub>

### `--prepare-audible`

```
prose --prepare-audible (--slug <slug> | --id <guid|prefix>) [--no-phonetics]
```

build an Audible AI-narration hand-off package for a node. Produces a narration-clean manuscript, pronunciation guide, and README.

<sub>handler `PrepareAudibleCli`</sub>

### `--print-book`

```
prose --print-book (--id <guid|prefix> | --slug <slug>)
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `PrintNodeCli`</sub>

### `--print-voice`

```
prose --print-voice
```

print the voice context the generator/re-beater receive — the verification that the canon-trained voice is wired into prompts.

<sub>handler `PrintVoiceCli`</sub>

### `--progress`

Strand Progress Dashboard: every non-archived book, code/title/kind/status/score/pages, sorted by score descending. Cross-universe by design. See .claude/commands/progress.md.

<sub>handler `ProgressCli`</sub>

### `--prose-check`

prose --prose-check (--slug <nodeSlug> | --id <beatId>) [--all] [--json]

<sub>handler `ProseCheckCli`</sub>

### `--prose-health`

prose --prose-health [--slug <nodeSlug>] [--json] [--out <dir>] Zero-cost overnight health scan: surface stats + kNN score prediction + semantic outlier detection using cached ProseEmbeddings. No API calls.

<sub>handler `ProseHealthCli`</sub>

### `--provenance-audit` / `--provenance`

```
prose --provenance-audit [--slug <slug-or-code-or-id>] [--samples N] [--json]
prose --provenance --grade <grade> --entity <id> | --relationship <rowId> | --claim <uid>
prose --provenance --grades
```

the Story Ledger's provenance surface (Phase 3) — "what is in canon that no human ever approved?", plus the explicit human act that promotes one candidate row to authored. Deterministic and free; report-only for the audit (docs/LOGIC.md §4). Universe-scoped deliberately: the entity/relationship counts come through the ambient query filter.

<sub>handler `ProvenanceCli`</sub>

### `--provider-status`

prose --provider-status [--live] [--json] RFC 0011 Brick 3: degraded-services status on demand. See docs/PROVIDERS.md.

<sub>handler `ProviderStatusCli`</sub>

### `--publish-audiobook` / `--record` / `--export-audio` / `--export-mp3`

```
prose --record | --export-audio | --export-mp3 | --publish-audiobook
```

render the WHOLE node as one continuous audiobook (one TTS pass, tiered to ElevenLabs limits — one request, else per-chapter, else split) and drop the MP3 in Downloads. The headless twin of the "Export Audio" button. (--id <guid|prefix> | --slug <slug>)

<sub>handler `PublishAudiobookCli`</sub>

### `--publish-book`

```
prose --publish-book (--id <guid|prefix> | --slug <slug>)
```

stitch an existing node's beats into one combined file (WAV → MP3), copy it to the publish output dir (Downloads by default), and record the publication run + process-event ledger. Headless Publish button.

<sub>handler `PublishNodeCli`</sub>

### `--publish-md` / `--publish-pdf`

render a node to Markdown or PDF in Downloads. Markdown output embeds <!-- beat:N:id7 --> markers for prose --import-md round-trip. ss (--publish-md | --publish-pdf) (--id <guid|prefix> | --slug <slug>) [--author "Name"]

<sub>handler `PublishManuscriptCli`</sub>

### `--publish-readiness`

prose --publish-readiness --slug <slug> [--json] docs/LOGIC.md §9's five-point publish-readiness convergence gate as a single readout (2026-08-30) — see BookHealthService.PublishReadinessAsync and PublishReadinessCli.cs. Read-only, no LLM calls, no cost gate needed.

<sub>handler `PublishReadinessCli`</sub>

### `--read-beats`

```
prose --read-beats (--slug <slug> | --id <guid>) [--from N] [--to N] [--numbers <csv>]
```

print beat text WITH its authoritative POV character attached (sourced fresh from BeatEntityPresence every call, never inferred from prose content). Use this instead of raw sqlcmd/SELECT Text reads whenever a conclusion about character voice, attribution, or continuity will be drawn from what's read — see ReadBeatsCli's own doc comment for the live mistake (2026-08-10, VIGL multi-POV misattribution) this exists to make structurally harder to repeat. Reads in true reading order, so it is also the sanctioned bulk-read for audit/logic-sweep work — no --publish-md export round-trip required. [--format text|json]

<sub>handler `ReadBeatsCli`</sub>

### `--reader-qa`

prose --reader-qa (--slug <slug> | --all) [--force] [--json] Reader-Proxy QA (docs/READER-QA.md) — the default reader-facing quality instrument. Phase 1: comprehension probes — a cheap model reads each chapter cold, diffed against the Sonnet synopsis ground truth, Sonnet-arbitrated, filed as ComprehensionDefect findings. NO scores (measurement, not vote — SS-A44 exempt). Hash-cached per chapter. Exit 0 = clean, 1 = defects found, 2 = error.

<sub>handler `ReaderQaCli`</sub>

### `--rebeat-book`

```
prose --rebeat-book (--slug <s> | --id <guid> | --all) [--apply]
```

rebuild a node's beats to the codified beat doctrine via LLM re-segmentation (story beats + dialogue/'?' mechanics + gaps). Dry-run by default; --apply backs up to markdown then replaces beats if the word-retention guard passes. --all targets every doctrine-violating node.

<sub>handler `RebeatNodeCli`</sub>

### `--rebuild-graph`

dotnet run --project ... -- --rebuild-graph [--universe <slug>] Rebuilds the scoped universe's <slug>_universe_graph.json cache from source data without starting the web server. One universe per invocation (scope is pinned below).

<sub>handler `RebuildGraphCli`</sub>

### `--rebuild-readmodel`

```
prose --rebuild-readmodel [--archived]
```

(re)build the materialized character read-model projection from the relational source of truth. Run after a bulk import / relational migration, or whenever ReadModelVersion is bumped. Backfills missing/stale rows, prunes orphans. The steady-state path self-heals, so this is a one-time / maintenance op.

<sub>handler `RebuildReadModelCli`</sub>

### `--recall`

```
prose --recall <keyword> [--content] [--to-disk] [--as-of <datetime-utc>]
```

keyword recall — call up (print) or create (--to-disk) the select few tracked .md files relevant to a topic, straight from the DB.

<sub>handler `RecallMarkdownCli`</sub>

### `--reconcile-book-entities`

prose --reconcile-book-entities (--id <guid> | --slug <slug>) [--universe <u>] Phase 0 (repair) of the corpus-trust-recovery plan: finds Entity rows describing a FORMER identity of a character this book's current bible names differently (a full rename, not a typo — name-based dedup structurally can't catch this). Report-only. See BookEntityReconciliationService for the two-stage cost-bounded design.

<sub>handler `ReconcileBookEntitiesCli` · **cost-gated (spends LLM money)**</sub>

### `--reconcile-trinity`

prose --reconcile-trinity --extract|--survey --slug <slug>|--all prose --reconcile-trinity --slug <slug>|--all --allow-votes --confirm-auto-edit [--dry-run] prose --reconcile-trinity --undo --decision-id <guid> Autonomous-but-reversible Bible/Book/Entity divergence resolution for GLMZ/SCRY/FICTION books — see ReconcileTrinityCli / TrinityReconciliationService.

<sub>handler `ReconcileTrinityCli`</sub>

### `--reembed`

```
prose --reembed              drift-skipped corpus pass (only changed entities re-embed)
prose --reembed --force      clear the table first, re-embed everything
```

rebuild the entity-embedding cache via cloud OpenAI.

<sub>handler `ReembedCli`</sub>

### `--reflow-book`

```
prose --reflow-book (--id <guid|prefix> | --slug <slug>) [--apply]
```

bounded copy-edit of a node — proper paragraph/dialogue spacing, a "?" on questions that lack one, and "asks"/"asked" (not "says") on question dialogue. Dry-run by default; --apply commits. Beats edited beyond those bounds are rejected (word-token guard) and left untouched.

<sub>handler `ReflowNodeCli`</sub>

### `--reimport-node`

```
prose --reimport-node (--id ... | --slug ...) --file path.node [--dry-run] [--force]
```

replace an EXISTING node's beats wholesale from a .node file. The other half of the export/edit/reimport loop for edits that no longer line up with the old beat boundaries (import-md patches beats in place by ID; this swaps the whole set). Old beats are disabled, never deleted. See ReimportNodeCli class doc for details and the safety checks.

<sub>handler `ReimportNodeCli`</sub>

### `--relation-aliases`

```
prose --relation-aliases --list
prose --relation-aliases --add --alias <wording> --canonical <standardizedRelationType> [--notes <notes>]
prose --relation-aliases --remove --id <id>
```

CRUD for RelationTypeAliases — normalizes link_entities free-text RelationType wording (e.g. "has" -> "owns") so the same relationship doesn't fork into multiple Edge rows.

<sub>handler `RelationAliasCli`</sub>

### `--rename-universe`

```
prose --rename-universe --slug <oldSlug> --new-slug <newSlug> --new-name <newName> [--new-theme <newTheme>]
```

rename a Universe row in place (Slug/Name/Theme only) — a seamless cutover, every Node/Entity/Book already scoped to its Id keeps working unmodified. See RenameUniverseCli.

<sub>handler `RenameUniverseCli`</sub>

### `--repair`

```
prose --repair                # cheap timeline-only pass
prose --repair --continuity   # also run continuity extraction (LLM-heavy)
```

dossier-driven story repair — walks every chapter, augments character records with timeline entries and (optionally) LLM-extracted continuity claims.

<sub>handler `RepairCli`</sub>

### `--repair-entity-docs`

```
prose --repair-entity-docs [--dry-run]
```

re-materialize the entity-doc row for EVERY active entity, in every universe. Unlike --backfill-entity-docs (per-book, inference-driven, so it only reaches entities a given book mentions) this iterates the entity table itself — which is what stamping MarkdownFiles.UniverseId on all of them requires.

<sub>handler `RepairEntityDocsCli`</sub>

### `--repair-slugs`

prose --repair-slugs [--apply] [--family entities|nodes|books|series|episodes] [--json] Regenerate every slug from its Name/Title metadata and update slug-carrying references (beat audio paths, publication paths, on-disk dirs, alt_slug). DRY-RUN by default; --apply writes. Slugs are loose keys — guid is the key.

<sub>handler `SlugRepairCli`</sub>

### `--reparent-node`

```
prose --reparent-node (--slug <slug> | --id <id>) (--parent-slug <slug> | --parent-id <id>)
prose --reparent-node --slug <slug> --clear   — detach from parent
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `ReparentNodeCli`</sub>

### `--reset-password`

prose --reset-password --email <e> --password <p> [--require-change] Operator password reset over the MindAttic.Authentication store, no web server.

<sub>handler `ResetPasswordCli`</sub>

### `--restore-beat-text`

```
prose --restore-beat-text --id <beatGuid> --as-of <datetime-utc> [--dry-run]
```

recover Beats.Text from Beats_History (system-versioned temporal table) after a bad overwrite — see RestoreBeatTextCli class doc.

<sub>handler `RestoreBeatTextCli`</sub>

### `--restore-entity`

```
prose --restore-entity --id <guid> --as-of <datetime-utc> [--dry-run]
```

restore a hard-deleted Entities row from Entities_History (system-versioned temporal table) — see RestoreEntityCli class doc.

<sub>handler `RestoreEntityCli`</sub>

### `--restore-markdown`

```
prose --restore-markdown [--file <relativePath>] [--as-of <datetime-utc>] [--dry-run] [--list]
```

restore .md files from DB back to disk. Supports point-in-time recovery from the MarkdownFiles_History temporal table.

<sub>handler `RestoreMarkdownCli`</sub>

### `--restore-node-field`

```
prose --restore-node-field (--id ... | --slug ...) --archive-id <guid>
```

restore a Node content field (Description/NodeOutline/Summary/Seed/Subtitle) from a named ArchivedBook snapshot back onto the live node. Explicit archive-id, never "latest" — see RestoreNodeFieldCli class doc. --field description|nodeoutline|summary|seed|subtitle|all --universe <u>

<sub>handler `RestoreNodeFieldCli`</sub>

### `--retire-bible-title-header`

```
prose --retire-bible-title-header --dry-run [--slug <slug>]
prose --retire-bible-title-header --apply [--slug <slug>]
```

retire the stale "# NODE BIBLE: [Title]" header baked into pre-fix generated outlines (NodeOutlineService's LLM prompt template). Dry-run first, same shape as --retire-locked-markers above.

<sub>handler `RetireBibleTitleHeaderCli`</sub>

### `--retire-locked-markers`

```
prose --retire-locked-markers --dry-run [--slug <slug>]
prose --retire-locked-markers --apply [--slug <slug>]
```

Bible->Outline refactor Phase 6a -- retire "LOCKED" markers (author ruling 2026-08-29, decision #3: the LOCK concept is retired, no corner auto-wins). Dry-run first.

<sub>handler `RetireLockedMarkersCli`</sub>

### `--retire-records-blobs`

```
prose --retire-records-blobs [--rebuild] [--validate] [--apply]
```

RFC 0007 unified blob-retirement gate — backfill all 29 relational types from Records.Json, validate, and delete the blobs in a single pass. (RFC 0007)

<sub>handler `RetireRecordsBlobsCli`</sub>

### `--review-entity`

```
prose --review-entity [--type <type>] [--ballots N] [--prose N] [--unrated]
```

run Legion persona quality voting across canon entity repos. Replaces the old LlmVoting (10 GLMZ residents) with the full 1000-persona library, 1-100 scale, and append-only EntityReview rows (same process as node reviews).

<sub>handler `ReviewEntityCli`</sub>

### `--review-node`

```
prose --review-node (--id <guid|prefix> | --slug <slug>) [--readers N]
```

have N Legion personas each read an EXISTING node and write an honest, scored reader review (saved to NodeReviews), then synthesize the Amazon-style aggregate summary. Round-robins reviewers across the trusted-4. --review-book/--run-panel literal aliases retired 2026-08-30 — one canonical name only.

<sub>handler `ReviewNodeCli` · **cost-gated (spends LLM money)**</sub>

### `--review-report`

```
prose --review-report (--slug <slug> | --id <guid> | --code <CODE>) [--provider local|cloud|all]
```

(re)generate the portable per-voter report (JSON + filterable HTM) from a node's most recent stored review batch, without re-running the panel.

<sub>handler `ReviewReportCli`</sub>

### `--review-settings`

prose --review-settings [--set <key> <value>] — view or update review voting settings. Keys: ballots, prose, panel, readers, max-concurrency, judge-provider, allowed-providers

<sub>handler `ReviewSettingsCli`</sub>

### `--run-corpus`

```
prose --run-corpus --count N [--seed "..."] [--kind episode] [--beats 12] [--ballots 20] [--resume] [--dry-run]
```

generate a new node (bible-first: plan → planned beats → expand in UI). CLI mode: autonomous corpus loop — generate N nodes end-to-end and review them.

<sub>handler `RunCorpusCli`</sub>

### `--runpod`

```
prose --runpod <status|stop|start|terminate> [--pod <id>]
```

manage the rented RunPod review pod (key from the MindAttic vault, provider 'runpod').

<sub>handler `RunPodGpuCli`</sub>

### `--sanitize-beats`

```
prose --sanitize-beats [--slug <slug> | --all] [--dry-run]
```

print all beats of a node as continuous prose to stdout. No headers, no beat numbers, no metadata — just the prose, beats separated by blank lines.

<sub>handler `SanitizeBeatsCli`</sub>

### `--sanity-scan`

prose --sanity-scan (--slug <slug|code> | --all) [--json] Deterministic prose checks — no LLM. Catches leaked internal node codes, undefined all-caps acronyms, encoding corruption, and heft-floor violations. Exit 0 = clean, 1 = warnings only, 2 = any blocks.

<sub>handler `SanityScanCli`</sub>

### `--scan-edge-duplicates`

prose --scan-edge-duplicates --universe <slug> [--json] Report-only: flags (Source, Target) pairs with more than one live RelationType wording (link_entities free-text drift, e.g. "owns" vs "has"). See ScanEdgeDuplicatesCli.

<sub>handler `ScanEdgeDuplicatesCli`</sub>

### `--scan-entity-mentions`

```
prose --scan-entity-mentions
```

backfill BeatEntityMentions — index which entity names appear in each beat so entity-update staleness propagation works.

<sub>handler `ScanEntityMentionsCli`</sub>

### `--schema`

```
prose --schema snapshot --table NAME [--out path.sql]
prose --schema rebuild  --table NAME --order "col1,col2,col3,…"
```

per-table schema operations (snapshot + safe column-reorder rebuild).

<sub>handler `SchemaCli`</sub>

### `--seed` / `--write-node`

```
prose --seed                     list known seeds
prose --seed <name>              apply one
prose --seed --all [--force]     apply every known seed in order
```

apply canonical SQL seeds via C# (replaces sqlcmd-by-hand workflow). NOTE: --seed is also the prompt flag of --write-node / --write-story / --create-book — those commands must win the dispatch or their calls get hijacked by the SQL seeder.

<sub>handler `SeedCli`</sub>

### `--seed-keywords`

```
prose --seed-keywords --slug <slug> --keywords "phrase one|phrase two|..."
```

set Amazon KDP backend keywords for one node (no generic default).

<sub>handler `SeedKeywordsCli`</sub>

### `--seed-sensory-hints`

prose --seed-sensory-hints [--list] [--weapon "Name" --hints "hint1; hint2"] [--force]

<sub>handler `SeedSensoryHintsCli`</sub>

### `--seed-voice-rules`

```
prose --seed-voice-rules
```

codify the GLMZ house voice + world rules from the memory rubric into the DB stores the generator reads (literary_rules / tone_bible). De-fragilizes the rules so they no longer depend on an .md file being parsed. Idempotent.

<sub>handler `SeedVoiceRulesCli`</sub>

### `--sequential-read-status` / `--sequential-read-record`

prose --sequential-read-status --slug <slug> | --all [--json] prose --sequential-read-record --slug <slug> --read-by <name> [--stages N] [--summary "text"]

<sub>handler `SequentialReadCli`</sub>

### `--session-beats`

prose --session-beats --session-id <guid>

<sub>handler `SessionBeatsCli`</sub>

### `--set-beat-enabled`

```
prose --set-beat-enabled --slug <slug> (--beat-number N | --beat-id <guid>) [--enable]
```

enable/disable a beat's membership in a node's reading order without touching the Beat row itself (wraps NodeWorkbenchService.SetBeatMembershipEnabledAsync).

<sub>handler `SetBeatEnabledCli`</sub>

### `--set-book-outline`

```
prose --set-book-outline --slug <slug> --file <path-to-bible.md>
```

hand-write the node bible verbatim (CLI mirror of MCP SetBookOutline).

<sub>handler `SetBookOutlineCli`</sub>

### `--set-canon-section`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `SetCanonSectionCli`</sub>

### `--set-edge-validity`

prose --set-edge-validity --edge <edgeId> [--slug <slug>] [--from-beat-number <N>] [--until-beat-number <N>] [--clear-from] [--clear-until] Sets/adjusts/clears an existing edge's beat-scoped validity window (2026-09-02, replaces the dead DateTime story-time mechanism). See SetEdgeValidityCli.

<sub>handler `SetEdgeValidityCli`</sub>

### `--set-llm-provider`

prose --set-llm-provider claude-api|claude-team [--dry-run] Switches every Settings.json field governing which Claude credential path is active in one command (ActiveLlmProvider always; ReviewJudgeProvider/ReviewAllowedProviders/ ReaderQaJuryProviders only where they currently hold the other Claude variant).

<sub>handler `SetLlmProviderCli`</sub>

### `--set-narrative-mode`

prose --set-narrative-mode --slug <slug> --mode original|retelling|historical Gates BookHealthService.SacredFlawAsync — see SetNarrativeModeCli.

<sub>handler `SetNarrativeModeCli`</sub>

### `--set-node-author`

```
prose --set-node-author --slug <slug|code|guid> --author "<Name>"
```

set Node.Author — the pen name exports fall through to instead of "MindAttic". See SetNodeAuthorCli class doc.

<sub>handler `SetNodeAuthorCli`</sub>

### `--set-structural-blueprint`

prose --set-structural-blueprint --slug <nodeSlug> --file <path.json> Hand-author a blueprint with no LLM call, matching GenerateBlueprintCli's response contract — for when the generation provider is unavailable but the structural decisions are already made.

<sub>handler `SetStructuralBlueprintCli`</sub>

### `--show`

/show lookup: resolve a subject (name/slug/alias) to one Entity or Node and return a structured profile. See .claude/commands/show.md.

<sub>handler `ShowCli`</sub>

### `--simulate-collision`

Scene Collision engine manual test harness (2026-08-10): runs SceneCollisionService against one real beat without a full ProseWriterRouter pass. See SimulateCollisionCli for details.

<sub>handler `SimulateCollisionCli` · **cost-gated (spends LLM money)**</sub>

### `--split-collection`

```
prose --split-collection (--slug <s> | --id <guid>)
```

split a monolithic node into a Collection (parent + chapter child nodes) at IsChapterStart boundaries. Backs up to markdown first.

<sub>handler `SplitCollectionCli`</sub>

### `--sql-export`

```
prose --sql-export --schema             schema-only DDL
prose --sql-export --data               schema + INSERT data
prose --sql-export --schema --out path  override output path
```

dump the entire Prose DB to a re-runnable .sql script.

<sub>handler `SqlExportCli`</sub>

### `--start-session`

── Edit Sessions ───────────────────────────────────────────────────────────── prose --start-session --slug <slug> --label "prose-pass-1" [--type prose-pass|gripes-cleanup|logic-sweep|custom]

<sub>handler `StartSessionCli`</sub>

### `--storyscope-audit`

prose --storyscope-audit --slug <nodeSlug> [--json] Verifies the book against measurable AI-fiction structural tells (StoryScope): flat escalation, event monoculture, moral gloss, emotion ratio, char-intro method, resolution mode, subplot execution, consensus clichés, TTCW originality. Findings triaged BLOCKER/MODERATE/MINOR; loop back into future beat prompts. Exit 0 = clean, 1 = moderate/minor, 2 = any blocker.

<sub>handler `StoryScopeAuditCli` · **cost-gated (spends LLM money)**</sub>

### `--strip-beat-artifacts`

```
prose --strip-beat-artifacts --slug <slug> [--dry-run]
```

one-off cleanup of a generation-artifact heading/marker leaking into Beats.Text — see StripBeatArtifactsCli class doc.

<sub>handler `StripBeatArtifactsCli`</sub>

### `--swain-audit`

prose --swain-audit [--slug <slug> | --code <code> | --all] [--repair] [--blockers] Classifies every enabled beat as Scene / Sequel / Ambiguous / Deficient against Dwight Swain's Scene/Sequel doctrine. Deficient = BLOCKER; Ambiguous = MODERATE. Add --repair to auto-splice the missing structural element (disaster turn, decision, etc.) into BLOCKER beats via Haiku (classify) + Sonnet (splice). Exit 0 = success. MUST appear before the bare --repair handler below.

<sub>handler `SwainAuditCli`</sub>

### `--sync-audio`

```
prose --sync-audio [--push] [--pull] [--node SLUG] [--dry-run] [--verbose]
```

reconcile audio bytes between local disk and Azure Blob storage. Companion to DualWriteAudioStore — repairs drift from offline recordings and failed background uploads. Default (no --push/--pull args) is full bidirectional repair. See SyncAudioCli class doc for the full arg list.

<sub>handler `SyncAudioCli`</sub>

### `--sync-blueprint-from-session`

prose --sync-blueprint-from-session --session-id <guid>

<sub>handler `SyncBlueprintFromSessionCli`</sub>

### `--sync-markdown`

```
prose --sync-markdown [--dry-run]
```

sync project-rule, Codex, and Claude Code memory .md files to DB. Upserts by RelativePath; only changed files (hash diff) produce a history row.

<sub>handler `SyncMarkdownCli`</sub>

### `--sync-outline-from-session`

prose --sync-outline-from-session --session-id <guid> [--dry-run]

<sub>handler `SyncOutlineFromSessionCli`</sub>

### `--tag-entities`

```
prose --tag-entities (--id <guid> | --slug <slug> | --all) [--dry-run]
```

retroactive inline entity-GUID tagging backfill (corpus-trust-recovery Phase 1a/1b).

<sub>handler `TagEntitiesCli`</sub>

### `--timeline`

```
prose --timeline (--slug <slug> | --id <id>)
```

extract a time / elapsed-duration timeline from all beats in a node. Flags clock anchors, infers story-relative timestamps, and surfaces conflicts.

<sub>handler `TimelineCli`</sub>

### `--timeline-check`

```
prose --timeline-check (--slug <slug> | --id <guid>)
```

deterministic timeline-consistency check (RFC 0009 §5). Detects dead-character-acting and wound-regression violations. No LLM calls.

<sub>handler `TimelineCheckCli`</sub>

### `--tuned-read`

```
prose --tuned-read --slug <slug> [--dry] [--no-extract] [--max-candidates N] [--json]
```

the Story Ledger's Tuned Read (Phase 2) — walks a book in reading order, keeps its fact ledger fresh, pairs claims an exclusion axiom says cannot both be true, adjudicates only those pairs, and files a finding for each contradiction whose quote survives the mechanical grounding gate. Report-only (docs/LOGIC.md §4). Cost-gated: a real run spends one Sonnet call per uncached candidate. --dry runs the whole deterministic half for free.

<sub>handler `TunedReadCli` · **cost-gated (spends LLM money)**</sub>

### `--universe-export`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `UniverseInterchangeCli`</sub>

### `--universe-import`

RFC 0007 "Universe Interchange" — import/export between an app's <app>/universe/<slug>.universe.json contract file and Prose's Entity spine. Each subcommand resolves its own explicit universe (file's own id, or a required positional slug) — see UniverseAgnosticCommands below.

<sub>handler `UniverseInterchangeCli`</sub>

### `--universe-sync`

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `UniverseInterchangeCli`</sub>

### `--validate-nouns`

```
prose --validate-nouns --slug <slug>
```

scan beats for deprecated/renamed noun references.

<sub>handler `ValidateNounsCli`</sub>

### `--verify-beat` / `--verify-book`

```
prose --verify-beat --id <beatId> [--json]
prose --verify-book --slug <slug> [--json]
prose --verify-quote --id <beatId> --quote "<claimed text>" [--claimed-by <name>] [--json]
prose --verify-quotes-batch --json-file <path> [--json]
prose --verification-staleness [--json]
```

Beat Verification Engine (Track C): checks prose against declared BeatBlueprintDecision contract. Results upserted to BeatVerification table. BLOCKER findings block --export-node. QuoteGrounding checks: confirm a logic-sweep audit agent's claimed quote actually appears in the beat it's attributed to, before that finding is trusted for triage/fix (SS-LOGIC-4a). --verification-staleness: which books have BeatVerification rows computed under an older CurrentRuleVersion and need a --verify-book/--audit-book re-run (2026-08-10 — added after the same "book never re-run after a check-logic fix" gap was found and manually re-diffed twice in one session; see BeatVerification.RuleVersion's doc comment).

<sub>handler `VerifyBeatCli`</sub>

### `--weapon-network`

prose --weapon-network (--id <weaponId> | --character <characterId> [--as-of date])

<sub>handler `WeaponNetworkCli`</sub>

### `--worker-mode`

Fail-closed Hub dependency (Phase 2, explicit user decision): "the hub is running, Prose is working; hub goes down, Prose is down." Every command gates on the Hub being reachable and healthy before anything else runs — no silent fallback to the old direct-in-process behavior. The one exception: --worker-mode runs on a rented remote GPU pod talking to a separate Azure-hosted coordinator and its own local LLM endpoint — it has no access to this machine's loopback-only Hub by construction, not by choice, and gets its own equivalent fail-closed check against ITS hard dependency (see WorkerModeCli) instead of this one.


### `--workflow-status`

prose --workflow-status [--slug <slug> | --all] [--json] Per-node or global prose service coverage matrix. Shows which services (Pacing, StoryMethodology, PlantPayoff, StoryAudit, Combat) were active when beats were written, and surfaces gaps where applicable services weren't used.

<sub>handler `WorkflowMonitorCli`</sub>

### `--world-state`

prose --world-state --beat <beatId> [--story-time "date"] [--json]

<sub>handler `WorldStateCli`</sub>

### `--wound`

prose --wound <subcommand> — character wound ledger: list    --character <id|name> [--as-of "date"] log     --character <id|name> --description "..." [--location "chest"] [--severity moderate] ... status  --wound <id> --status active|healed|noted

<sub>handler `WoundCli`</sub>

### `--write-node`

```
prose --write-node --seed "..." [--title "..."] [--kind episode] [--beats 12] [--outline-only]
```

_(no description in the dispatch comment — add one above the guard in `Program.cs`)_

<sub>handler `WriteNodeCli`</sub>

### `--write-synopsis`

prose --write-synopsis --slug <nodeSlug> [--json] Generates a beat-by-beat narrative synopsis (act-grouped, one sentence per beat) FROM the written prose. For a logic check, use --logic-sweep instead.

<sub>handler `WriteSynopsisCli`</sub>

