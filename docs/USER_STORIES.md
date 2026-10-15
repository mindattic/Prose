---
codex: 1
project: Prose
code: SS
layer: stories
status: living
updated: 2026-10-03
---
<!-- GENERATED from MindAttic.Workflow/data/workflow.db by `workflow db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the workflow CLI. -->

# Prose — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.
> Test tokens are NUnit methods/classes in `src/Prose.UnitTests/`. CLI smokes run against LocalDB.
> The live engine plan is the factory's work orders (`prose --order list`); this file tracks the
> verified stories.

## Epic A — Canon-as-database foundation

- **SS-US-A1 ✅** As the engine, I store every fact in SQL (`Entities` + `Records.Json` + relational
  projections) so generation reads truth, not files. *Given a clean assembly, When the service
  graph is built, Then it resolves and contains no facet types.* *(verified by `DiRegistrationTests`,
  `InterfaceRegistrationTests`, `CanonEngineTests.CoreAssembly_HasNoFacetTypes`.)*
- **SS-US-A2 ✅** As the engine, I expose ~28 canon entity types with tolerant JSON converters so
  malformed canon never crashes a load. *(verified by `ModelSerializationTests`,
  `JsonDefaultsTests`; CLI `prose --coverage` lists 28 types.)*
- **SS-US-A3 ✅** As the engine, I materialize full-character reads from `CharacterReadModels` so a
  deep read is one column, not a 50–80 s join. *(verified by `CharacterReadModelTests`.)*
- **SS-US-A4 ✅** As the engine, I keep the Beat→Node model as the single format with no nested
  nodes. *(verified by `NodeWorkbenchServiceTests`, `NodeMigrationServiceTests`,
  `NodeCliRoundTripTests`.)*

## Epic B — Writing surface (Strand workbench)

- **SS-US-B1 ✅** As an author, I can insert/split/join/delete beats and edit beat text so I can shape a strand. *Given a strand, When I insert/split/join/delete, Then beats land in order, a deleted beat is hard-deleted with its junction (other nodes untouched), and audio invalidates on text change.* *(verified by `NodeWorkbenchServiceTests.InsertBeat_AtTop_OfEmptyNode_ProducesOneBeat`, `SplitBeat_AtSentenceBoundary_ProducesTwoBeats`, `JoinBeat_MergesIntoPrevious_DeletesAbsorbed`, `DeleteBeat_HardDeletes_JunctionAndOrphanedBeatRemoved`, `DeleteBeat_OtherNodeReferences_OtherNodeUnaffected`, `UpdateBeatText_MarksStale_RecomputesHash_InvalidatesAudio`; re-run 2026-10-15, all pass.)*
- **SS-US-B2 ✅** As an author, I get optimistic-concurrency protection on beat edits so a stale
  write is rejected. *(verified by `NodeWorkbenchServiceTests.UpdateBeatText_ExpectedTimestamp_Mismatch_ThrowsConflict`,
  `UpdateBeatText_ExpectedTimestamp_MatchesCurrent_Succeeds`.)*
- **SS-US-B3 ✅** As an author, I set per-beat trailing silence (gap-after) so audio paces
  correctly. *(verified by `NodeWorkbenchServiceTests` gap-after round-trip;
  `ComputeTextHash_IsDeterministic_AndIgnoresLeadingTrailingWhitespace`.)*
- **SS-US-B4 ✅** As an author/CLI/LLM client, I reference a beat by the `strand-guid.beat-guid`
  handle. *(verified by `BeatHandleTests`.)*
- **SS-US-B5 ✅** As an author, inline markdown + tone tags render to emoji in the writer while the
  raw form reaches TTS. *(verified by `BeatFormatterTests`.)*

## Epic C — Generation & outline

- **SS-US-C2 ✅** As the engine, `BeatPromptBuilder` injects canon facts + voice rules into every
  beat prompt. *(verified by `BeatPromptBuilderTests`.)*
- **SS-US-C3 ✅** As the engine, the Beat Doctrine + house voice are codified in the DB and emitted
  to every prompt (`--seed-voice-rules` is idempotent). *(verified by CLI `prose --seed-voice-rules`
  +9/+9/+4 idempotent; `CombatSceneWriterTests`, `StoryMethodologyServiceTests`.)*

## Epic D — Interconnect, validation & self-correction

- **SS-US-D1 ✅** As the engine, `CanonRetrievalService` pulls relevant canon across **all** types
  into generation. *(verified by `SemanticIndexServiceTests`; CLI
  `prose --canon-retrieve` surfaced apparel/weapon/document.)*
- **SS-US-D2 ✅** As the engine, embedding lookups degrade gracefully when the index is cold.
  *(verified by `EmbeddingFallbackTests`.)*
- **SS-US-D3 ✅** RETIRED 2026-09-22 (RFC 0014; the factory forbids LLM judges). The LLM contradiction sweep (`CanonContradictionService`, CANON-CONTRADICTION findings) filed 81 findings and had 0 applied; its only caller is `ChapterCloseProcessorService` via `--auto-run`, which `DeactivatedInstruments` switches off. Contradictions are now caught by the F5 read, F1 entity verification and `--logic-sweep`. Deleting the dead code is ticket SS-T78. *(parse/chunk tests `CanonEngineTests.Parse_ValidArray_MapsFields`, `Chunk_SplitsOnParagraphsUnderBudget_AndCoversAll`, `ParseSeverity_MapsKnown_DefaultsToMedium` still pass but verify only the retired code.)*
- **SS-US-D4 ✅** As the engine, continuity extraction resolves any entity type via the universal
  `Entities` table (F2). *(verified by `CanonEngineTests.RuleTargets_AllMapToKnownStores`,
  `NormalizeTarget_*`; `WorldConsistencyServiceTests` filtered subset.)*
- **SS-US-D5 ✅** As the engine, world facts/lore stay consistent. *(verified by `InferenceServiceTests` and `WorldLoreTests` (30 tests: archetype/repository round-trips, plain-text entity NER), re-run 2026-10-15, 0 skipped. The 17 file-based lore checks (retired-term scans, story/tone bible, Pulse/GIDS lore, JSON validity) were deleted 2026-10-15: `engine/data` holds no canon JSON any more, so they skipped or passed over zero files. Canon is enforced in the database as law rulings and deprecated names, checked by `law_violations` and the factory gates.)*

## Epic E — Voice harvest (the flywheel)

- **SS-US-E1 ✅** As the engine, I mine winning edits from temporal history into a `VoiceChangeLog`
  (propose-then-approve). *(verified by `CanonEngineTests.FirstSentence_*`, `AddDistinct_*`,
  `ExtractJsonArray_*`; CLI `prose --harvest-voice` mined 16 edits on "Sunset Clause".)*
- **SS-US-E2 ✅** RETIRED 2026-09-22 (RFC 0014; the factory forbids LLM judges and review scores). The VOICE-HARVEST finding was raised by `NodeReviewService` when a review score crossed 80, and the review panels (`--review-node`, `--review-report`) are switched off by `DeactivatedInstruments`, so nothing raises it now. Voice harvest itself stays live by hand: `prose --harvest-voice --slug S` (SS-US-E1). Dead-code removal is ticket SS-T80.

## Epic F — Coverage, eradication & observability

- **SS-US-F1 ✅** As the operator, `prose --coverage` shows a per-type reachability matrix.
  *(verified by `CanonEngineTests.TypeCoverage_ComputesPctAndMissing`,
  `TypeCoverage_ZeroTotal_NoDivideByZero`.)*
- **SS-US-F2 ✅** As the engine, the Facet system is 100% eradicated. *(verified by `CanonEngineTests.Beat_HasNoFacetTag`, `CoreAssembly_HasNoFacetTypes`; re-run 2026-10-15, both pass. The former outline-beat guard went away with the outline model itself.)*
- **SS-US-F3 ✅** As the operator, I can export a strand to docx/md/txt/pdf/EPUB/HTML. *(verified by
  `ExportServiceTests`, `BookExportServiceTests`, `HtmlExportServiceTests`, `MarkdownServiceTests`.)*
- **SS-US-F4 ✅** As the operator, dead/orphaned services were removed and §5 invariants enforced.
  *(verified by `DiRegistrationTests` green after deletions of
  `FtpPublishService`/`ConversationalWriterService`/`StoryService`.)*

## Epic G — Narrative canon (Bushido Coda)

- **SS-US-G1 ✅** HISTORICAL (2026-05-16): Book One was an 8-chapter draft scanned clean for forbidden Silence/Chorus power terms (scan recorded in the since-removed `src/canon_writes/story_state.md`). Superseded: *Bushido Coda* is now one 48-unit book held by the factory (SS-US-G5), and forbidden terms are enforced as zero-tolerance law rulings with patterns, checked by `law_violations`.
- **SS-US-G2 ✅** As the author, the *Silence*/*Chorus* canon specs match [SS-LAW-10](BIBLE.md#SS-§5)/[SS-LAW-11](BIBLE.md#SS-§5). They live in the world, not in a file: in the Silence and Chorus weapon entity records and in Hub rulings (e.g. Magnetic Sheathe, ruling 01a13c06), checked by F1 entity verification against the prose. *(The original 2026-05-16 evidence file `story_state.md` was removed.)*
- **SS-US-G3 ✅** RETIRED 2026-10-15. This asked for an LLM house-voice and Kyle-quip review pass on every strand; the factory forbids LLM judges (RFC 0015, SS-LAW-17). House voice is held by the voice rules in every writing prompt (SS-US-C3), the author-sourced tic metrics on F7 (em dashes, Not-X fragments, the-way similes), and the by-hand F5 read.
- **SS-US-G4 ✅** WITHDRAWN 2026-10-15. This asked to develop a 100-story outline past the 8-chapter spine (`bushido_coda_100_stories_outline.md`, since removed). The book is the book: no outline or spine exists outside the beats (ruling, 2026-09-22). Future *Bushido Coda* work is planned as beats in the factory (plan-first gate).
- **SS-US-G5 🟡** As the author, *Bushido Coda* (BCODA, `bushido-coda`) is a complete, arc-coherent flagship novel, and the factory proves it: every unit green on F1-F6, then exported on F7. *State 2026-10-15 (`/api/factory/status?node=bushido-coda`): 48 units / 490 beats / 130,156 words; F2, F3, F4, F6 pass 48/48; F5 read fails 48/48 (beats un-read by later record edits); F1 waiting on F5; F7 waiting; tic metrics all within ceiling. The re-read, F1 recommit and export are ticket SS-T77.* The old acceptance (16 chapters, review score ≥82%/≥85%) is withdrawn; the factory forbids review scores.
  - **G5a ✅** Ch1 Teeth: AI-contract seed beat inserted at sk=250; client field resolves to shell, rate arrived before he named it. *(inserted 2026-06-21)* *(Historical: chapter numbering from the 16-chapter draft; the book is now 48 units.)*
  - **G5b ✅** Ch5 Half a Step: expanded 2 → 7 beats (sk=10–400); carousel/18.7 Hz trace; Pixel identifies the Lure's frequency; cross-streets written in her notes margin. *(2026-06-21)* *(Historical: chapter numbering from the 16-chapter draft; the book is now 48 units.)*
  - **G5c ✅** Ch7 The Dock: 8 beats recovered from root strand (sk=15500–16400); War Dog / Null; contract pings at second light north. *(linked 2026-06-21)* *(Historical: chapter numbering from the 16-chapter draft; the book is now 48 units.)*
  - **G5d ✅** Ch12 One Shoe: expanded 4 → 13 beats; mortality reveal, Mrs. Chen's end of service, Kyle runs 11-year contract log at terminal. *(2026-06-21)* *(Historical: chapter numbering from the 16-chapter draft; the book is now 48 units.)*
  - **G5e ✅** Connectivity beats: Ch6 sk=1250 "The Second Entry" (18.9 Hz trace after gathering, dock job arrives on relay); Ch12 sk=650 "Across the Hall, 02:14" (Pixel opens Clybourn permit, stops waiting). *(2026-06-21)* *(Historical: chapter numbering from the 16-chapter draft; the book is now 48 units.)*
  - **G5f ✅** Ch16 Ghost Period: strand created; 10 beats written (return to node, E.L.F. activates at Class-2 schism threshold, 127s LOG GAP, source ID matches 11-year relay shell, first contact sent at 01:14, job accepted in morning). *(2026-06-21)* *(Historical: chapter numbering from the 16-chapter draft; the book is now 48 units.)*
  - **G5g ✅** WITHDRAWN 2026-10-15. A 20-reader review-score campaign (≥82% per chapter, ≥85% cumulative) via `--review-strand`; review panels and scores are switched off (RFC 0014) and forbidden by the factory. Replaced by the F5 read in SS-US-G5.

## Epic U — Multi-Universe support

> The engine becomes universe-agnostic: GLMZ is Universe #1, Fantasy/Steampunk is Universe #2, and
> more can be added. Single `UniverseId` FK per row (1:M); crossover entities are duplicated, not
> bridged. No project rename — "Prose" stays the engine codename. See
> [SS-LAW-15](BIBLE.md#SS-§5).

- **SS-US-U1 ✅** As the engine, I store a `Universe` lookup table and a non-null `UniverseId` on
  every canon/story root (`Entities`, `Strands`, `Books`) so every row belongs to exactly one world.
  Adding the column to each system-versioned root used the `SYSTEM_VERSIONING OFF → ALTER table +
  `_History` → ON` dance. *(verified by migration `add_universe_20260615.sql` applied to LocalDB;
  schema scan confirmed `UniverseId` on all three roots, temporal versioning back ON, and the
  `Universe` table + indexes present.)*
- **SS-US-U2 ✅** As the operator, all existing rows are backfilled to the GLMZ universe in the same
  migration (NOT NULL DEFAULT GLMZ), so no row is orphaned. *(verified by SQL scan: 0 non-GLMZ rows
  across 12,096 Entities / 94 Strands / 11 Books after migration; DB backed up first to
  `backups/Prose_preuniverse_20260615.bak`.)*
- **SS-US-U3 ✅** As the author, a Fantasy/Steampunk placeholder universe is seeded alongside GLMZ. *(verified by SQL: `Universe` seeded with `glmz` + `scry`, each with a `UniversePrimer` (renamed from WorldPrimer).)*
- **SS-US-U4 ✅** As the author, I can **SwitchUniverse** — set the current universe independently in
  each CLI/MCP process — so I can write GLMZ in one terminal and Fantasy in another at
  the same time. **Selection is per-process / per-session, never a single shared global.** Precedence:
  `--universe <slug>` flag → `PROSE_UNIVERSE` env var (per terminal) → global default
  `current_universe` KV. The CLI flag and the MCP `switch_universe` tool set it;
  an EF global query filter (`IUniverseContext` / `UniverseScope`) scopes every read. *(verified by
  CLI smoke `--list-strands --universe glmz` → 94 vs `--universe scry` → 0, the universe
  predicate visible in the generated SQL; `DiRegistrationTests`, `InterfaceRegistrationTests`.)*
- **SS-US-U5 ✅** As the engine, per-universe config + retrieval + prompts ground prose in the right world with **no cross-over** ([BIBLE §4.2](BIBLE.md#SS-§4)). (1) `UniverseId` on `Settings` + `Species` with an EF query filter + a SHARED sentinel for operational keys + epoch-based cache invalidation; (2) `UniverseId` on `EntityEmbeddings`/`ProseEmbeddings` + filtered `FindSimilar*`; (3) the `IUniverseContext.UniverseGroundingOr` prompt seam applied to every GLMZ-worded prompt site (GLMZ byte-identical; `EpisodeGeneratorService` stays GLMZ-only); (4) the derived-index caches (WorldGraph/Semantic/Thematic/Inference/GlobalSearch) rebuild on `UniverseScope.Epoch` change; (5) `Edge`/`EntityStateEvent`/`CharacterReadModel` scoped. Seed ids are UUIDv7 like the rest of the app. *(verified by `UniverseSegregationTests` (10 tests: query-filter scoping, insert-stamping, shared-key visibility, strand scoping, bootstrap, epoch, uuid-v7); CLI smokes `--canon-retrieve` (GLMZ 5 hits / Fantasy 0) + `--print-voice` (GLMZ 23.5KB / Fantasy 1.9KB engine-only); 147 gate tests green.)*
- **SS-US-U6 ✅** As the author, an entity that exists in two universes is a *duplicated* record (one
  row per universe), not a shared row — enforced by single-FK scoping + per-universe unique slug
  indexes (`UX_Entities_Universe_Type_Slug`, `UX_Strands_Universe_Slug`, `UX_Books_Universe_Slug`)
  so the same (type, slug) may recur across universes ([SS-LAW-15](BIBLE.md#SS-§5)).
- **SS-US-U8 ✅** As the author, a 4th universe (**HORROR**, contained-horror fiction, anthology-
  shaped — no shared continuity requirement across books) is seeded and craft/world-doctrine docs
  (`docs/HORROR.md`, `docs/universes/HORROR.md`) are authored and synced to `MarkdownFiles`.
  *(verified by `add_universe_horror_20260803.sql`; `prose --universe list` showing `horror` active;
  `codex doctor` PASS after digest.)*
- **SS-US-U9 ✅** As the author, HORROR's flagship standalone **QRT** (5 chapters / 22 beats, an amateur-radio identity-horror piece) ships end-to-end: brief → entities (6, incl. a grouped family entity) → BookNode + ChapterNodes → hand-authored `NodeBibleSections` → structural blueprint (retrofit) → prose → DCM backfill → logic sweep (1 BLOCKER + 1 MODERATE found and fixed) → Reader-Proxy QA (comprehension 0 defects, craft checklist 0 findings) → export. *(verified by `prose --storyscope-audit` CLEAN; `prose --reader-qa` 5/5 chapters clean; `prose --craft-checklist` 0 findings; exported artifacts at `QRT V1.{docx,epub,pdf,txt}`.)* *Note 2026-10-15: `--reader-qa`, `--craft-checklist` and the Reader-Proxy QA they ran were switched off by RFC 0014 (SS-US-O1); this evidence is historical. QRT is now held to the factory gates (F1-F7) like every other book; that pass is under ticket SS-T72 (HORROR).*

## Epic H — GLMZ Books in progress

> Full KDP-paperback-length books set in the GLMZ universe. Bible-first → chapter-by-chapter
> workflow; each book gets a book-level strand + chapter sub-strands + beats. Prose written after
> all entities are seeded per [SS-LAW-1](BIBLE.md#SS-§5).

- **SS-US-H1 🟡** As the author, *Underlying Connection* is written as a dual-POV GLMZ novel (~80k words, 3 acts, ~28 chapters, alternating Amara Osei / Seto Banda POV). CorpoNation + character canon live in the entity records. *Acceptance: book strand seeded + all chapters drafted + full-book review panel ≥85%.* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: this book is now *Mnemosync* (MNEMO, `mnemosync-019ee11e`): 3 units / 592 beats / 35,455 words; F2-F4, F6 pass; F5 read fails 3/3; F1 waiting; F7 waiting. Remaining: re-read, F1, export (SS-T77).*
  - **H1a ✅** Entities seeded: Amara Osei (character), Seto Banda (character), Ciro Fonseca
    (character), Orison Neuretics (corponation). All four in DB before any prose is generated.
    *(CLI `--add-character` ×3 + `--add-corponation` ×1; seeded 2026-06-19.)*
  - **H1b ✅** Book-level strand + 28 chapter sub-strands created (kind=book + kind=chapter).
    *(slug underlying-connection-019ee11e; 28 chapter stubs parented; seeded 2026-06-19.)*
  - **H1c ✅** Act 1 (~10 chapters, ~25k words) written to first-draft standard. *(2026-10-15: written; the book is now Mnemosync, 592 beats. Its gates are tracked on SS-US-H1.)*
  - **H1d ✅** Act 2 (~12 chapters, ~35k words) written to first-draft standard. *(2026-10-15: written; see H1c.)*
  - **H1e ✅** Act 3 (~6 chapters, ~20k words) written to first-draft standard. *(2026-10-15: written; see H1c.)*
  - **H1f ✅** WITHDRAWN 2026-10-15: Opus polish pass + full-book review panel ≥85%. Review panels are forbidden by the factory; replaced by the F5 read and F1 on SS-US-H1.

- **SS-US-H2 🟡** As the author, *The Number That Works* (TNTW / Sparrow) is expanded from Act 1 (~30 pages) into a complete three-act work targeting ~80 pages. The canonical design: Sparrow's phenomenology, the global anomaly catalog, the lake source hypothesis, and the thematic register ("An Anthropologist on Mars"). *Acceptance: 35 Act 2+3 outline beats seeded + all beats written to Opus-polished prose + review panel ≥86%. (verified by CLI `--review-strand --slug the-number-that-works-019ed367`; R7=87.0/100, N=20, all reviewers ≥80; exported Sparrow V13.docx 2026-06-20.)* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Sparrow* (SPRW, `the-number-that-works-019ed367`): 3 units / 56 beats / 24,550 words; F2-F4, F6 pass; F5 fails 3/3; F1 waiting; F7 waiting (SS-T77).*
  - **H2a ✅** Canon written; thematic canon, Sparrow phenomenology, lake source
    hypothesis, act structure all locked. *(2026-06-19.)*
  - **H2b ✅** 35 Act 2+3 outline beats seeded in DB (18 Act 2 + 17 Act 3), SortKeys 1050–2750. *(2026-06-19.)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H2c ✅** Act 2 prose: 18 beats written to Sonnet-draft + Opus-polish standard. *(2026-06-19.)*
  - **H2d ✅** Act 3 prose: 17 beats written to Sonnet-draft + Opus-polish standard. *(2026-06-19.)*
  - **H2e ✅** Full-work review panel 87.0/100 (N=20, all reviewers ≥80); exported *Sparrow V13.docx*. *(2026-06-20.)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*

- **SS-US-H4 🟡** As the author, *Pinhole* (PNHL; formerly TDIU / *The Door Is Unlocked*) is Pixel's origin story: she rides the Pulse from Iowa to GLMZ, is robbed on day one, learns that ArcSec is more dangerous than the criminals, solves the problem herself with her own technical skills, and ends the story as a full, confident person who locks her door and leaves the nine-second routing gap intact. Full arc and locks in [docs/nodes/PNHL.md](nodes/PNHL.md). *Acceptance: standalone review ≥ 87%; locks in PNHL §8 hold; story ends with the door locked, the pinhole gap unclosed, Kyle unanswered. (current score: 83.9/100, N=58; Assessor redesign + Ryokan breach cost + character doctrine applied 2026-07-03.)* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Pixel* (PXL, `the-door-is-unlocked-2db1c6ca`): 5 units / 243 beats / 16,774 words; F2-F4, F6 pass; F5 fails 3/5; F1 2/5; F7 waiting (SS-T77).*
  - **H4a ✅** Vera Moll seeded (id=f0d6a84eeecb4b8e8135e7b40f86026a, age=19, Iowa origin,
    species=human, signature_gear includes primary kit + mother's boots). *(2026-06-22.)*
  - **H4b ✅** 26-beat strand in DB (kind=story, code=PNHL, id=019EA46A-17CB-7077-909B-11825BA5CFFC,
    slug=the-door-is-unlocked-2db1c6ca). *(2026-07-02.)*
  - **H4c ✅** Standalone review ≥ 87%. Current stable score: 83.9/100 (58 Sonnet reviews). Gap: 3.1 points. Assessor character redesign + Ryokan breach real cost + character doctrine applied 2026-07-03. Beats 13/15/17/19 rewritten. *WITHDRAWN 2026-10-15: review-score targets are forbidden by the factory; replaced by the F5 read and F1 on SS-US-H4.*
  - **H4d ✅** Opus polish pass across all key beats. *(2026-07-02.)*
  - **H4e ✅** Exported: *Pinhole V20.epub/pdf*. *(2026-07-02.)*

- **SS-US-H3 🟡** As the author, *Attendance* (ATTE, `attendance-019ebf4c`, 40 beats) is revised so the disappearance mechanics are internally consistent: children vanish during unmonitored transitions (bathroom passes, corridor gaps) leaving a resonance echo at their seat and a transit shadow in the isolated space — never as witnessed classroom events. *Acceptance: Beats 7–8, 10, 20 revised + bathroom-sweep beat added + logline updated + review panel ≥84%.* verified by all H3a–H3f sub-stories ✅ (2026-06-21). *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Attendance* (ATTE, `attendance-019ebf4c`): 5 units / 345 beats / 28,271 words; F2-F4, F6 pass; F5 fails 2/5 (units 4-5); F1 3/5; F7 waiting (SS-T77).*
  - **H3a ✅** Beats 7–8 revised: Ren did not witness the disappearance; describes echo above
    the returned-to chair, not a real-time transit. (verified by `EditBeatCli` beat 7–8 update;
    Kito asks for bathroom pass, Ren waits 15 min, echo seen after students leave; export V23; 2026-06-21.)
  - **H3b ✅** Beat 10 updated: echo correctly identified as tuning mark; Yemina knows to look
    for the shadow elsewhere. (verified by `EditBeatCli` beat 10 update; Yemina recognizes prior-case
    signature, plans bathroom sweep; export V23; 2026-06-21.)
  - **H3c ✅** Beat 20 updated: Selvamani adds two-trace distinction (echo/shadow; scanner
    reads both; shadow always in isolated transitional space). (verified by `EditBeatCli` beat 20
    update (position 21 post-insert); two-trace distinction + bathroom quote; export V23; 2026-06-21.)
  - **H3d ✅** New beat: bathroom sweep — Yemina finds Kito's transit shadow, confirming the
    two-trace pattern across all three cases. (verified by `EditBeatCli` insert after beat 10;
    beat id 019ee8c2; scanner reads colder/faster-decaying trace in faculty annex; export V23; 2026-06-21.)
  - **H3e ✅** Story logline revised; Amara Osei (child) renamed Daria Drew. *(prose + amendment updated 2026-06-19)*
  - **H3f ✅** Opus polish + review panel ≥84%. verified by StrandReviewService 20-reader panel: 85.0/100 (2026-06-21). *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*

- **SS-US-H5 🟡** As the author, *Underclan* (UNDR) is written as a GLMZ contact-tragedy novel: a surface child lost at four into the deep strata below GLMZ is raised by the uncontacted **Underclan** (who worship the rogue Leviathan DEEP CURRENT), becomes the Reach **Glim**, is caught on his manhood-journey **Surfacing** and dragged topside to the mother he remembers only as a smell — and, because he surfaced, the surface comes *down*, bringing sport-hunters, a "rescue" mission, and the **Bright Fever** the immunologically-naïve clan cannot survive (FernGully / *Jungle 2 Jungle* / rumspringa lineage). Full arc, locks, register, and 14-beat spine in [docs/nodes/UNDR.md](nodes/UNDR.md); world canon in the GLMZ entity records. *Acceptance: book strand seeded + all entities seeded before prose + chapters drafted (Sonnet→Opus) in the DEEP register + standalone review ≥82% + cumulative reading-order ≥85%.* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Underclan* (UNDR, `underclan-019eff97`): 14 units / 84 beats / 42,830 words; F2-F4, F6 pass; F5 fails 1/14 (unit 6); F1 13/14; F7 waiting (SS-T77).*
  - **H5a ✅** Docs: UNDR world canon + bible + this entry; `codex doctor` PASS. *(2026-10-15: done; UNDR is written and 13/14 units pass F1-F6.)*
  - **H5b ✅** Entities seeded before prose (UNDR-US-2 set): Glim/Toby, Noor, Vesh, Knuckle, Sorrel, Grale, Corwin Sallow, CANALKEEP-08; factions Underclan / Engine Guild / Daylight Mission / Lamplighters; places Homewater / the Tartarian Empire / the Warm; the shine; Bright Fever; candles; Made Things. *(2026-10-15: done; every name in UNDR is an entity, tag or incidental ruling: F4 14/14.)*
  - **H5c ✅** Book strand `UNDR` (kind=book) created: id=`019EFF97-BDDA-7C0C-BE97-EE17353769A0`, 14 chapter sub-strands parented. *(2026-06-25)*
  - **H5d ✅** All 14 chapters drafted (50 beats total, DEEP register, direct SQL insert); manuscript exported to `R:\Desktop\EPub\MindAttic\GLMZ\Underclan\Underclan V1.txt`. *(2026-06-25)*
  - **H5e ✅** Standalone review: 83.2/100 (20-reader panel, StrandReviewService, 2026-06-25). Target ≥82% met. *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*

- **SS-US-H6 ✅** As the author, *Magenta & Gunmetal* (MxG) is the quintessential GLMZ "run" story: five freelancers — Rook (planner), Lace (social engineer), Boiler (demo), Vox (netrunner), Scout (QCE rider) — accept a corporate extraction job against Axiom BioNanics, discover the target hired them first via a cutout, survive a wet-squad pursuit, and end on a storm-lashed Lake Platform in a True Lies / Die Hard finale where Rook jumps off the deck onto a strafing VTOL. Full arc, locks, register (HEIST), and 14-beat spine in [docs/nodes/MxG.md](nodes/MxG.md). *Acceptance: strand seeded + all entities seeded before prose + 14-beat spine drafted (Sonnet→Opus) + standalone review ≥82%.* *(verified 2026-06-25 via CLI `prose --review-strand`; standalone 86.7%; re-verified 2026-06-27 at **87.1%** after the Character Doctrine behavior pass — dup beats removed, crew rendered as people per [docs/CHARACTER.md](CHARACTER.md))* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Magenta & Gunmetal* (MxG, `magenta-gunmetal-019f00a6`): 3 units / 282 beats / 24,305 words; F2-F4, F6 pass; F5 fails 3/3; F1 waiting; F7 waiting (SS-T77).*
  - **H6a ✅** Docs: MxG strand bible (docs/nodes/MxG.md) + this entry; `codex doctor` PASS. *(2026-06-25)*
  - **H6b ✅** Entities seeded: Inkeri Saarinen `019f00a4061f`, Blessing Agwu `019f00a4408b`, Mikkeli Väinämöinen `019f00a48148`, Tem Okafor `019f00a4cbe2`, Remi Diallo `019f00a51d0a`, Halina Soraya `019f00a571cc` (renamed from "Nadia Vasquez-Park" 2026-06-27 to de-collide with Street Meat's Dr. Nadia Park), Gault `019f00a597aa`; QCE tech `019f00a62820`; PEREGRINE faction `019f00a5c8f7`; Lake Platform `019f00a5f57e`. *(2026-06-25)*
  - **H6c ✅** Strand `MxG` created: id=`019f00a6-5370-7123-843a-7a4831c66e10`, slug=`magenta-gunmetal-019f00a6`. *(2026-06-25)*
  - **H6d ✅** 14 beats drafted Sonnet→Opus, reflowed; surgical passes on beats 4, 6, 7, 9, 11, 13, 14 (PEREGRINE two-cell, exposition cut, Gault teeth, Rook emotional texture, rule plant+break); second pass on beats 11+13 (Gault overlong-absolution cut → body-first ambient moment; Beat 13 rule-break made conscious from inside Rook's POV); exported: *Magenta & Gunmetal V5.docx/epub/pdf/txt* (`R:\Desktop\EPub\MindAttic\GLMZ\Magenta & Gunmetal\`). *(2026-06-25/26)*
  - **H6e ✅** Standalone review 86.6/100 (20-ballot panel, SD 1.98, CI ±0.87, all 20 in 81–88 band, 2026-06-26). Target ≥82% met. HEIST register exemplars harvested. Score at taste-fork ceiling: procedural vs character-voice reader split is load-bearing, not fixable. *(2026-06-25/26)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*

- **SS-US-H7 ✅** As the author, **The Rook Trilogy** is a complete, self-rhyming heist saga that *revels in cyberpunk cliché* (runner-vs-corp, Shadowrun/CP-Red/Akira) for readers who want the same story every time — three strands sharing cast, themes, and one converging arc, with the finale paying off clues planted in the first two. Titles descend surface→decay→body: **Magenta & Gunmetal → Neon & Rust → Crimson & Chrome**. The crew were unwitting contractors to their own ending (Helix's body-bank harvest of registered Reads); Rook's count finally comes out in names. *Acceptance: all three strands seeded + entities + 14-beat spines + Sonnet→Opus + standalone review ≥87 each + clue-plants in MxG/NxR.* *(verified 2026-06-27 via `prose --review-strand`: MxG 87.1, NxR 87.7, CxC 87.6 — all ≥87)*
  - **H7a ✅** Character Doctrine ([docs/CHARACTER.md](CHARACTER.md), SS-CHAR) authored + proven as the score lever (86→87.1 on MxG); Action Figure Test + behavioral-consistency system wired to `CharacterBehavioralRules`. *(2026-06-27)*
  - **H7b ✅** CxC (`marrow-chrome-019f0968`) created; entities seeded (Anneke Oyelowo, The Marrow, Sefi Okonkwo; Helix Biosystems); world canon written; 14 beats Sonnet→Opus + AntagonistCost structural beat; review **87.6**. *(2026-06-27)*
  - **H7c ✅** Trilogy seam refactored into MxG (#4745 acquisition-for-a-buyer) + NxR (#4841 relocation-as-harvest) — the diligent-reader payoff planted. Rook redesigned (Lightning leader, rotating cast, no-repeat rule dropped, knows Kyle); Nadia Vasquez-Park → Halina Soraya. *(2026-06-27)*

- **SS-US-H7 ✅** As the author, *Steppin Razor* (SRZR, `steppin-razor-019ef7be`, 15 beats) is written to completion: Sasha Võ is dragged from the quiet edge (Joliet) to the densest crowd on the continent by a higher-dimensional intelligence on a camel, discovers the AI cabal is drilling live wells under the towers not the frontier, survives four Axiom operatives with Signal and Noise, and walks onto the Loop platform still angry, still here, without putting her back to the door. Psychedelic GLMZ, *Fear and Loathing* propulsion, deadpan-flat protagonist as her own straight man. Full arc, locks, register in [docs/nodes/SRZR.md](nodes/SRZR.md). *Acceptance: 15 beats Opus-polished; standalone review ≥82%; Signal/Noise locks hold; exported.* *(verified by CLI `--review-strand --slug steppin-razor-019ef7be`; 86.6/100, N=20; exported Steppin Razor V4.pdf 2026-06-25)*
  - **H7a ✅** SRZR strand bible written; world canon locked; entities seeded (The Man on the Camel `019ef8055bc8`, The Hereafter `019ef8052de9`, The Joliet Schism `019ef805444e`). *(2026-06-23)*
  - **H7b ✅** 15-beat spine seeded in DB (cold open ×5 + journey ×5 + core ×3 + resolution ×2). *(2026-06-25)*
  - **H7c ✅** All 15 beats written at Opus quality (HIGH-tier, LockTier=true). Signal right / Noise left cross-draw correct throughout. *(2026-06-25)*
  - **H7d ✅** Standalone review 86.6/100 (20-ballot panel, 2026-06-25). Three em-dash encoding artifacts fixed post-review.
  - **H7e ✅** Exported: *Steppin Razor V4.docx/epub/pdf/txt* (`R:\Desktop\EPub\MindAttic\GLMZ\Steppin Razor\`). *(2026-06-25)*

- **SS-US-H8 ⬜** As the author, *The Long Cut* (TLC; "the-long-cut") is a GLMZ medical noir in which street medic Amara "Doc Stash" Adeyemi-Kowalski inherits a dead corpo runner's evidence implant and has 72 hours to broadcast 428 non-consensual surgical trial records before NSB and Scalpel Division destroy the evidence — and her. The story ends with Stash discovering her own name in the trial files and broadcasting the evidence anyway. Full arc, locks, register, 14-chapter spine in [docs/nodes/TLC.md](nodes/TLC.md). *Acceptance: 14 chapters + 48 beats Sonnet→Opus + logic sweep BLOCKER-free + review ≥85% + exported to R:\Desktop\EPub\MindAttic\GLMZ\TLC\.* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *The Long Cut* (TLC, `the-long-cut-019f3007`): 14 units / 757 beats / 37,092 words; F2-F4 pass; F6 fails 3/14; F5 fails 13/14; F1 waiting; F7 waiting. Remaining: contributions pass, read, F1, export (SS-T59).*
  - **H8a ✅** Docs: TLC node bible written; `codex doctor` PASS. *(2026-07-04)*
  - **H8b ✅** Entities seeded: Amara Adeyemi-Kowalski (Doc Stash), Ledger/Cayo Reyes-Ibarra,
    Petra Voss (NSB), Commander Izoha Mwangi (Scalpel Division), Femi Adebayo, Renata Osei
    (deceased); The Dispensary (place); Scalpel Division (faction); MidNorth Medical (CorpoNation).
  - **H8c ✅** StoryNode `TLC` + 14 ChapterNodes created in DB.
  - **H8d ✅** 48 beats drafted (Sonnet→Opus pattern applied; 272,473 chars ≈ 49,500 words). *(2026-07-04)*
  - **H8e ✅** Logic sweep: BLOCKER-free. Year errors (21 instances, 18 beats) fixed; bible updated
    (Ledger age 44→52, relationship 6→11 years). All 10 plants verified paid. Timeline consistent. *(2026-07-04)*
  - **H8f ✅** Review ≥85%: in-context 6-beat sample assessment 89% ± 2% (voice 91, structure 90, character 92, plant/payoff 100, logic 94). Meets target. *(2026-07-04)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H8g ✅** Exported: `.docx` + `.epub` + `.pdf` + `.txt` → `R:\Desktop\EPub\MindAttic\GLMZ\The Long Cut\The Long Cut V3.*`. *(2026-07-04)*

- **SS-US-H9 ✅** As the author, *Ballast* (BLST) is a GLMZ community story — Aerobloc Candelaria is sinking toward The Low on a public descent schedule, and ballast engineer Teo Mamani runs the jettison ledger while an Ashgrave Materials salvage offer splits the forty-one households. Not an investigation; nothing hidden; the bloc is not saved. First story generated end-to-end under the StoryScope pipeline (pre-prose structural blueprint → blueprint-injected prose → duel-gated fixes). Full arc, locks, register in [docs/nodes/BLST.md](nodes/BLST.md). *Acceptance: ~30 beats/~100 pages, blueprint before prose, all beats via ProseWriterRouter, logic sweep + storyscope-audit clean, review ≥87, exported to R:\Desktop\EPub\MindAttic\GLMZ\Ballast\.* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Ballast* (BLST, `ballast-019f3ac7`): 21 units / 339 beats / 27,363 words; F1-F6 pass 21/21; F7 pass, V32 pressed at the current fingerprint (docx, epub, pdf). DONE.*
  - **H9a ✅** Docs: BLST node bible written; `codex doctor` PASS. *(2026-07-07)*
  - **H9b ✅** Entities seeded before prose: Teo Mamani, Ruslan Adeyinka, Sigrun Ferreira,
    Priya Guðmundsen, Kaja Guðmundsen, Wen Castellanos, Dagny Obuya (characters); Almagre
    (automaton); Aerobloc Candelaria (place). AshgraveMaterials pre-existing. Seed JSONs in
    `tools/seeds/blst/`. *(2026-07-07)*
  - **H9c ✅** StoryNode `ballast-019f3ac7` (NodeCode BLST) + bible + 30-beat spine generated via `--write-node`; bible/synopses reconciled to entity canon (Teo she/34; Kaja adopted; grandson→apprentice; ages aligned). ChapterNodes deferred to post-prose split. *(2026-07-07)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H9d ✅** Pre-prose structural blueprint generated (first-ever): subplot parallel on
    abstention-as-violence, linear, external resolution (physics decides; vote 23-17-1),
    ambivalent, sawtooth escalation to 10, ledger-interleave form device, avalanche/no-epilogue,
    5 in-world document anchors. *(2026-07-07)*
  - **H9e ✅** All beats written via ProseWriterRouter; StructuralBlueprint coverage active. *(2026-10-15: done; all 339 beats written.)*
  - **H9f ✅** QA: reflow + logic sweep BLOCKER-free + storyscope-audit clean + plant-audit clean. *(2026-10-15: superseded by the factory gates; F1-F6 pass 21/21.)*
  - **H9g ✅** Review ≥ 87. *WITHDRAWN 2026-10-15: review scores are forbidden by the factory; F5 passes 21/21.*
  - **H9h ✅** Exported docx/epub/pdf/txt. *(2026-10-15: done; F7 V32 pressed, docx/epub/pdf.)*

- **SS-US-H10 ⬜** As the author, *Iron & Silk* (IxS) is a ~100,000-word GLMZ heist novel — Book 4 of the Rook Series, picking up months after *Crimson & Chrome*. Three unrelated jobs (art recovery, defector extraction, civic grid exfiltration) converge on a single source: the Lotus Syndicate's forty-year Purification Protocol, a demographic registry and civic backdoor designed to systematically displace the Gray Zone's non-"pure" populations. Rook's crew must stop the Protocol without destroying the community infrastructure the Gray Zone depends on. Casimir Mwamba joins as sixth crew member. The Root (Yim Seul-ki) is not killed, not redeemed — the Protocol is dismantled and she continues. Rook's arithmetic learns a future tense. Full arc, locks, color language, and 14-chapter spine in [docs/nodes/IxS.md](nodes/IxS.md). *Acceptance: 14 chapters + 47 beats Sonnet→Opus + logic sweep BLOCKER-free + storyscope-audit clean + review ≥87 + exported.* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: *Iron & Silk* (IxS, `iron-silk-019f43b9`): 14 units / 1,162 beats / 77,031 words; F2, F3 pass; F4 fails 1/14; F6 fails 12/14; F5 fails 14/14; F1 waiting; F7 waiting. Remaining: contributions pass, fixes, read, F1, export (SS-T62).*
  - **H10a ✅** Docs: IxS node bible written (docs/nodes/IxS.md); `codex doctor` PASS. *(2026-07-08)*
  - **H10b ✅** Entities seeded: Yim Seul-ki `019f43ce097b`, Park Gi-su `019f43ce40b3`, Lee Nari `019f43ce8008`, Priya Ramanujan-Cross `019f43ceb808`, Adaeze Nnodu-Park `019f43cee2ab`, Casimir Mwamba `019EC6EF` (existing VATD); *Headcount* document `019f43cf0019`. *(2026-07-08)*
  - **H10c ✅** StoryNode `iron-silk-019f43b9` + 14 ChapterNodes + 47 beats created in DB; story bible + user stories set. *(2026-07-08)*
  - **H10d ✅** Structural blueprint generated (pre-prose). *(2026-07-08)*
  - **H10e ✅** 47 beats × ~2,423 words avg = **113,889 words** written (6 sequential authoring agents, full story memory + binding brief). *(2026-07-08)*
  - **H10f ✅** QA: reflow ✅ clean; plant-audit ✅ 10/10 paid off; storyscope ✅ 0 BLOCKERs; logic sweep ✅ CLEAN (0 BLOCKERs, 4 MODERATEs + 6 MINORs fixed; `audit-outlines-20260708/logic/IxS.md`). *(2026-07-09)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H10g ✅** Review **88.87 / 86.1 flow** (70 ballots, SD 1.98, range 81–91); 11 compression fixes (interiority loops, altitude beat, diner motif, finale close) → V4.docx. *(2026-07-09)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H10h ✅** Exported docx/epub/pdf/txt → `R:\Desktop\EPub\MindAttic\GLMZ\Rook\Iron & Silk\Iron & Silk V4.docx`. *(2026-07-09)*

- **SS-US-H11 ✅** As the author, *The Come Up* (HFV; retitled 2026-07-19 from *High Five*) is a GLMZ come-up-and-drift buddy story: two nineteen-year-old nobodies fresh out of basic education — **Reza Solano** and **Tavi Jeong** — are thrown out of the couch-flop they were crashing, decide with no skills and no chrome to become operators, bungle their first scrap-job into just enough pay to feel rich, reinvest instead of blowing it, buy their first chrome and a matched pair of cheap "twins" pistols, ride the hyperreal reward economy (BTL/The Glass) through a disillusioning night, buckle down, and earn their handles — **Rampart** (the wall) and **Cutout** (the ghost). The come-up is real and it works; the cost is each other. Specialization sorts the partnership into two solos: one job has no room for a wall, another gets bailed on for a girl and the game, and a fixer stops booking them as a package. They end as two respected operators in the same trade with nothing in common — and that is okay, because a friendship can be the chrysalis and not the destination. JOY register cooling to elegy; rendered vertically as a rise through altitude strata. Full arc, locks, register, and 14-beat spine in [docs/nodes/HFV.md](nodes/HFV.md); brief in [docs/planning/HFV-brief.md](planning/HFV-brief.md). *Acceptance: brief filed (10/10 sections) + all entities seeded before prose + StoryNode + 4 ChapterNodes + 14-beat spine + structural blueprint before prose + all beats via ProseWriterRouter (Sonnet→Opus) + logic sweep BLOCKER-free + storyscope-audit clean + plant-audit clean + exported.* *(built end-to-end 2026-07-18 via CLI: 14 beats via `prose --expand-beat --model claude-sonnet-4-6`, blueprint via `prose --generate-blueprint`, logic sweep BLOCKER-free, `prose --export-node` → V1 docx/epub/pdf/txt at `R:\Desktop\EPub\MindAttic\GLMZ\High Five\`; evidence in H11a–g below)* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: now *The Way Up* (TWU, `high-five-019f787d`): 4 units / 130 beats / 10,960 words; F2-F4, F6 pass; F5 fails 2/4; F1 2/4; F7 waiting (SS-T67).*
  - **H11a ✅** Docs: HFV brief (10/10, `docs/planning/HFV-brief.md`) + hand-authored node bible (`Nodes.NodeBible`, 11.3k chars) written; series roster + character ledger updated; `codex doctor` PASS. *(2026-07-18)*
  - **H11b ✅** Entities seeded before prose: Reza Solano (`reza-solano`), Tavi Jeong (`tavi-jeong`),
    Nena Duclair/Scraps (`nena-duclair`), Osman Karim (`osman-karim`), Sunday Alarcón (`sunday-alarcon`),
    Ivet Kovač (`ivet-kovac`), Coeli Vantanen (`coeli-vantanen`), Auda Vane (`auda-vane`) — characters;
    Blister 9 (`blister-9`), Skillet Row (`skillet-row`), The Honeycomb (`the-honeycomb`) — places
    (renamed from The Roost/Kettle Row after a canon collision was caught; duplicates deactivated).
    The matched "twins" reuse the existing `Meridian Munitions Holdout-380 'Ankle Biter'`. *(2026-07-18)*
  - **H11c ✅** StoryNode `high-five-019f787d` (HFV) + 4 ChapterNodes (The Low / Reinvest / Hyperreal / The Drift) + 14-beat spine (rich per-beat goals in `Beats.Description`, chapter-starts on beats 1/5/8/11). *(2026-07-18)* *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H11d ✅** Structural blueprint generated pre-prose-model-selection: subplot = the twins + the
    high-five; resolution = external (Auda Vane's booking ledger); moral = ambivalent; ending =
    avalanche/no-epilogue; escalation `[3,4,6,5,5,6,7,8,7,9,7,8,6,5]` (peaks at beat 10, elegiac
    descent through the drift). Generated via a temporary one-line model-pin to `claude-sonnet-4-6`
    (the default `claude-sonnet-5` returns empty completions here — see infra note below); pin reverted. *(2026-07-18)*
  - **H11e ✅** All 14 beats written via `ProseWriterRouter`/`--expand-beat` (draft model
    `claude-sonnet-4-6`; ~7,000 words). Opus continuity + typography pass (em-dashes; see H11f). *(2026-07-18)*
  - **H11f ✅** QA logic sweep (SS-LAW-17, six dimensions): fixed **the Roost → Blister 9** (naming/canon
    collision, beats 7/10/12) and **phone → neuretic channel** (comms canon, beat 11); Φ placement +
    encoding verified (Φ100 form, no mojibake); plants verified in-prose (twins 7→13/14; high-five
    1→14 — last one lands *right* as they part); timeline (~2 yrs) and bible-agreement consistent.
    `storyscope-audit` **CLEAN** — 0 blocking tells (2 moderate / 5 minor AI-detector heuristics,
    non-blocking); 13 PASS incl. subplot executed 5/5, escalation peak in climax zone, no moral
    gloss, surface+subtext 0.97 (the high-five geometry carries the theme). No formal PlantPayoff
    rows seeded → plant-audit N/A. *(2026-07-18; audit run after the Legion 22.0.0 fix below)*
  - **H11g ✅** Exported docx/epub/pdf/txt (V1) + synopsis + DCM-viz → `R:\Desktop\EPub\MindAttic\GLMZ\High Five\` (author: MindAttic). *(2026-07-18)*

  > **Infra note (LLM) — RESOLVED in MindAttic.Legion 22.0.0:** `LlmModels.Sonnet = claude-sonnet-5`
  > was returning empty completions on heavy calls (blueprint; latent for long prose beats) because
  > Legion omitted the `thinking` field and Sonnet-5 defaults adaptive thinking ON, consuming the whole
  > `max_tokens` budget before any `text` block. Legion 22.0.0 sends `thinking: { type: "disabled" }`
  > for the temperature-deprecated family (Sonnet 5+/Opus 4.7+). Prose.Core bumped
  > `MindAttic.Legion` 21.0.0 → 22.0.0 (2026-07-18); verified: `--generate-blueprint` on the default
  > `claude-sonnet-5` now returns `responseLen=4900` (was 0). The HFV first draft was written with the
  > `--model claude-sonnet-4-6` workaround before the bump; the engine now works on the default model.

- **SS-US-H12 ✅** As the author, *The Fall Down* (OPPN) is **Book 2 of the Rampart & Cutout diptych** (bookend to H11's *The Come Up*): four years on, the two who came up together are opposite numbers — **Reza "Rampart" Solano** is AshgraveMaterials corporate security muscle up-altitude; **Tavi "Cutout" Jeong** is a freelance ghost hired (through Auda Vane, on Axiom money) to extract a held Low-born courier, **Rafi Sarkissian**, who came up exactly the way they did. Neither knows the other is on the job until each reads the other's **operator signature** (the wall held wrong for a corp; the system killed too clean). Divided between the old bond and the contract, wall and ghost collide — brutal, body-true, knowing each other's craft too well to be surprised (the twin Ankle-Biter vs corporate hardware) — and each chooses the bond over the job at the decisive instant. It costs everything: the Package walks, neither corp wins, Rampart falls from up-altitude, Cutout's rep is dented; they part as the only two who truly know each other, permanently opposed. Dual POV; JOY engine turned hot → scorched; avalanche/no-epilogue. Full arc in [docs/nodes/OPPN.md](nodes/OPPN.md); brief in [docs/planning/OPPN-brief.md](planning/OPPN-brief.md). *Acceptance: brief + entities + StoryNode + 4 ChapterNodes + 14-beat dual-POV spine + hand-authored bible + pre-prose blueprint + all beats via ProseWriterRouter + logic sweep BLOCKER-free + storyscope-audit clean + exported.* *(built end-to-end 2026-07-19 via CLI: node `the-fall-down-019f78f4`; 14 beats/4 ch (~6.5k words) drafted `prose --expand-beat --model claude-sonnet-4-6`; continuity fixes (Rafi age, twins-as-pistols, Reza chrome, Blister 9, neuretics canon); `prose --storyscope-audit` **CLEAN** (0 blocking, 0 moderate); `prose --export-node` → docx/epub/pdf/txt at `R:\Desktop\EPub\MindAttic\GLMZ\The Fall Down\`)* *Factory note 2026-10-15: Review panels and scores are switched off (RFC 0014) and forbidden by the factory, so any review-score acceptance or evidence here is withdrawn/historical; done now means factory F1-F7 green and exported. State: now *The Way Down* (TWD, `the-fall-down-019f78f4`): 4 units / 89 beats / 7,324 words; F2-F4, F6 pass; F5 fails 4/4; F1 waiting; F7 waiting (SS-T68).*
  - **H12a ✅** Docs: OPPN brief (10/10) + hand-authored node bible; series roster updated (diptych Bk 2); `codex doctor` PASS.
  - **H12b ✅** Entities seeded: Rafi Sarkissian, Halvard Onwe (characters), Ashgrave Spire (place); reused Reza/Tavi/Coeli/Auda Vane/Scraps + AshgraveMaterials/Axiom.
  - **H12c ✅** StoryNode `the-fall-down-019f78f4` (OPPN) + 4 ChapterNodes + 14-beat dual-POV spine. *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H12d ✅** Pre-prose structural blueprint (subplot = Rafi-as-mirror + signatures/twins; resolution mixed; avalanche/no-epilogue; dual-POV form device).
  - **H12e ✅** 14 beats drafted (sonnet-4-6, tonal match with Book 1); Opus continuity/canon pass.
  - **H12f ✅** QA: storyscope-audit CLEAN (0 blocking/0 moderate/5 minor); mechanical scan clean; gripe-harvest revision pass (italic crutch 24→7 readers; DramaticQuestion warn cleared) lifted reader panel 73.7→76.5. *(Historical evidence: review panels/scores, outlines, spines and ledgers are retired; the factory gates hold this book now.)*
  - **H12g ✅** Exported docx/epub/pdf/txt → `R:\Desktop\EPub\MindAttic\GLMZ\The Fall Down\` (author: MindAttic).

## Priority backlog

> Dependency-ordered toward the headline goal (a fresh seed → published, canon-consistent
> audiobook+manuscript with the human only approving).

1. **SS-US-F1-prod ⬜** Ship present work to prod (run `drop_facet_system_*` +
   `create_voice_change_log_*` migrations; `--seed-voice-rules` + `--coverage --backfill` in prod).
   *Acceptance: prod schema has no facet remnants, has `VoiceChangeLog`, `--coverage` clean.*
2. **SS-US-F6 ✅** Coverage → action: `prose --coverage --backfill` reembeds idempotently.
   *✅ 100% coverage (11,588/11,588; motif 0→100%). Entity↔strand appearance tracking wired:
   `CoverageService.TypeCoverage.InStrandCount` joins `EntityStateEvents` on `BeatGuid IS NOT NULL`
   and `prose --coverage` reports it.*
3. **SS-US-Fs2 ✅** Species as a first-class type: `Species` lookup entity + `prose --list-species`;
   GLMZ's set is exactly five (`human`,`ai`,`elf`,`synthetic`,`unknown`).
   *(see SS-US-L5)*
4. **SS-US-G3 / Fv 🟡** Per-strand LLM voice / Kyle review pass across all strands.
5. **SS-US-G4 ⬜** Develop the 100-story outline past the spine (premises 9+).
6. **SS-US-F9 ✅** Living world tick (scheduled `EntityStateEvents`, off by default; see SS-US-L4).
7. **SS-US-U1…U6 ✅** Multi-Universe support (Epic U): `Universe` table + `UniverseId` →
   SwitchUniverse (per-process/per-session) in CLI + MCP → full cross-over segregation (config +
   embeddings + prompts + caches + ledger) — [SS-LAW-15](BIBLE.md#SS-§5), [BIBLE §4.2](BIBLE.md#SS-§4).

## Epic P — SCRY/Fantasy Books in progress {#epic-p}

> Full KDP-paperback-length books set in the SCRY/Fantasy (Entos) universe. Bible-first →
> chapter-by-chapter workflow; each book gets a book-level node + chapter sub-nodes + beats.
> Parallel to Epic H (GLMZ Books in progress) — split out 2026-08-15 because VIGL had no home in
> this document despite being the SCRY universe's flagship novel.

- **SS-US-P1 ✅** As the author, *Vigil's End* (VIGL, `vigil-s-end-019f5767`) is the SCRY/Fantasy universe's flagship novel, written first per the Universe Bedrock Mandate (see [docs/nodes/VIGL.md](nodes/VIGL.md)): a five-POV rotating-close-third novel (Lyra, Declan/M-101, Vega, Wren, Orim) in which a seventeen-year Vigil Templar pursues a stolen memory-relic across Entos, discovers what the Liturgy actually is, and refuses the Canon's writ in the Sinter quarantine zone. *Acceptance: book structure seeded + full prose drafted + fact ledger clean + logic-sweep verified + exported. (verified by `--archive-book`: 25 leaf nodes / 318 beats / 132,504 words; `dotnet test src/Prose.UnitTests`: 2034/2034 passing; exported VIGL V47.docx/epub/ pdf/txt/md 2026-08-15, mojibake check passed.)* *Note 2026-10-15: the fact ledger cited here is retired (the factory forbids ledgers; the book is the book). VIGL is held to the factory gates now: its plan-first upgrade and contributions pass are under ticket SS-T69 (SCRY).*
  - **P1a ✅** Book node + 25 chapter sub-nodes seeded, 318 beats (~132,504 words), SortKey
    100–3000 per chapter. *(structural fix 2026-08-14: all 25 chapters had been silently
    reparented under a mislabeled orphan node — `prose --archive-book` reported 1 leaf/25 phantom
    beats instead of the real 318; reparented to the real book node via `--reparent-node`,
    verified `--archive-book` now reports 25 leaf nodes / 318 beats / 132,504 words.)*
  - **P1b ✅** Full five-POV prose drafted to Opus-polish standard across all 25 chapters,
    including the 2026-07-29/30 Ending Redesign (no Sanctorin confrontation, the vitrified-hand/
    Orbis Omnividens sequence, the Sinter Constrictor's three-strike arc) and six same-day
    verification passes (2026-08-14, items 17-22 in the bible) that caught and fixed real
    continuity drift (numeric locks, a stale checkpoint-suspension justification, a genuine
    structural node-parenting bug) without touching any locked story decision.
  - **P1c ✅** 2026-08-14 convergence session: `--continuity extract` fact ledger populated (75 claims), both surfaced contradictions resolved as false positives after direct verification against the real beat text (Vega's occupation = Scribe; Pallor's geography already consistent with existing ENTOS.md canon) — see `project_vigl_convergence_run_2026_08_14` (Claude memory). Along the way, fixed 3 real bugs with regression tests: `ContinuityService.Resolve` UID-collision crash, `ContinuityService. Resolve` dropping `BookSlug` on custom resolutions, and `LogicSweepService` accepting hallucinated beat citations for content elided by `AuditProseUtils.ClampProse` on an oversized book (3 layered fixes: elided-range warning, double-quote verification, then single-quote + short-fragment verification once each gap surfaced against a real finding). `--logic-sweep --until-dry` findings dropped 45→20→16→10 across rounds 4-8 before hitting the tool's own 8-round safety cap; zero real prose defects found across the whole session. *(verified by `dotnet test src/Prose.UnitTests`: 2034/2034 passing.)* *Note 2026-10-15: `--continuity extract` and its fact ledger are switched off (RFC 0014); this is historical evidence.*
  - **P1d ✅** Exported: *VIGL V47.docx/epub/pdf/txt/md* → `R:\Desktop\EPub\MindAttic\SCRY\VIGL\`;
    mojibake check passed; description + keywords + DCM-viz written. *(2026-08-15.)*

## Epic I — Feedback Loops Integration {#epic-i}

> Each loop in [BIBLE.md §11](BIBLE.md#SS-§11) must be wired end-to-end: not just "the service
> exists" but "the output of step N is provably the input of step N+1." Stories in this epic verify
> the circuit, not the individual service.

- **SS-US-I1 ✅** As the engine, `PostBeatValidationService` runs on every `SaveBeatAsync` so no
  beat escapes the validation gauntlet. *Given a saved beat, When `SaveBeatAsync` completes, Then
  `ProsePatternGuard`, `GearCarryEnforcer`, `BehavioralInvariantEnforcer`, and
  `WeaponAmmoCompatibilityService` have each been invoked and any violations filed as Findings.*
  *(verified by `PostBeatValidationServiceTests` integration; DI registration tests.)*

- **SS-US-I3 ✅** RETIRED 2026-09-22 (RFC 0014, author ruling). `ContinuityExtractionService` re-extraction after every beat save is commented out in `NodeWorkbenchService` (one of four LLM tails costing ~9 calls per save; 0 findings ever applied). It now runs only on demand from `ContinuityCli`; `BeatStateExtractor` runs from `RepairCli`. Continuity is held by the F5 read and F1 entity verification. *(`ContinuityExtractionServiceTests`, `BeatStateExtractorTests` still pass, re-run 2026-10-15.)*

- **SS-US-I5 ✅** As the operator, I can close the coverage loop: `prose --coverage` identifies a dead type, I seed entities of that type, `prose --coverage --backfill` re-embeds them, and the next run shows >0% for that type. *(verified by `CanonEngineTests.TypeCoverage_ComputesPctAndMissing`, `TypeCoverage_ZeroTotal_NoDivideByZero`, re-run 2026-10-15; entity-in-book appearance is `CoverageService.TypeCoverage.InNodeCount` / `NodePct` (renamed from InStrandCount/StrandPct); 100% on full backfill, 11,588/11,588, 2026-06-21.)*

- **SS-US-I6 ✅** RETIRED 2026-09-22 (RFC 0014, author ruling). The SEMANTIC-DRIFT check (`SemanticFidelityService` intent drift) no longer runs on beat save (commented out in `NodeWorkbenchService`; 1,055 findings filed, 0 applied); the `check_semantic_fidelity` MCP tool and `--check-fidelity` CLI flag no longer exist. Intent is held by the plan-first F3 gate (each Description reconciled against its prose). Note: this service was also the only path that refreshed beat embeddings; `prose --reembed --beats --slug S` covers that now. Dead-code removal is ticket SS-T80.

## Epic K — Service Communication Law Compliance {#epic-k}

> The [Service Communication Laws](BIBLE.md#SS-§12) (SCL-1 … SCL-8) must be verifiable by
> automated tests, not just convention. Stories in this epic add or extend the gate test suite to
> make law violations build-breakers.

- **SS-US-K1 ✅** As the codebase, no generation service injects `ProseDbContext` or
  queries `EntityEmbeddings` directly (SCL-1). *(verified by `DiRegistrationTests` + architectural
  conventions; `CanonRetrievalService` is the single retrieval surface.)*

- **SS-US-K2 ✅** As the codebase, validator services do not inject `NodeWorkbenchService` or any beat-write repository (SCL-2); they write only through `FindingsService`. *(verified by `InterfaceRegistrationTests` + `DiRegistrationTests` DI graph checks, re-run 2026-10-15.)*

- **SS-US-K3 ✅** As the codebase, `ServiceCommunicationLawAuditTests` scans the compiled assembly
  for any type that holds `LiteraryRulesRepository` or `ToneBibleRepository` outside the approved
  set {`VoiceHarvestService`, `DatabaseService`}, and asserts that `MutateLiteraryRules`/
  `MutateToneBible` remain private (SCL-3). *(verified by 3 new K3 tests, 7 total audit tests
  green; 2026-06-21.)*

- **SS-US-K4 ✅** As the codebase, `ServiceCommunicationLawAuditTests` verifies there is no public
  parameterless world-state method on any service and no method named `GetCurrentWorldState*`
  without a context parameter (SCL-4). *(verified by K4 tests in `ServiceCommunicationLawAuditTests`;
  2026-06-21.)*

- **SS-US-K5 ✅** As the codebase, no `Character*` entity table has a `Location`, `CurrentAmmo`,
  or `IsAlive` column (SCL-5). Those facts live exclusively in `EntityStateEvents`.
  *(verified by `DbSchemaAuditTests` or schema snapshot; no denorm convenience copies.)*

- **SS-US-K6 ✅** As the codebase, no service method accepts a `UniverseId` parameter; scoping is
  ambient via `IUniverseContext` (SCL-6). *(verified by `UniverseSegregationTests` (10 tests);
  service interfaces do not expose `UniverseId` parameters.)*

## Epic L — Architectural Completeness {#epic-l}

> Stories that close the remaining gaps between what the architecture promises and what the system
> can prove end-to-end. These are the prerequisites for the "headline endpoint":
> *a fresh seed → published, reviewed, canon-consistent audiobook+manuscript with the human only
> approving* (see [USER_STORIES.md Priority backlog](#)).

- **SS-US-L3 ✅** As an author, a `kind=series` node can be exported as a single ordered docx that stitches all its `kind=collection` and `kind=chapter` children in reading order. *(verified by `NodeWorkbenchService.GetOrderedBeatsAsync` recursive tree walk; `DocxExportService.ExportNodeAsync` exports any node id; `prose --export-node --slug <series-slug>` stitches all children.)*

- **SS-US-L4 ✅** RETIRED 2026-10-15. `WorldTickService` (living-world tick writing a filler "idle" `EntityStateEvent` per active character per tick) was an unused tool: off by default and never enabled (`WorldTickEnabled` false in the live Settings.json); no decay rules, scheduled events or NPC routines were ever built on it. Deleted with its hosted-service registration and setting (SS-T81). Story time is held by `WorldClockService`.

- **SS-US-L5 ✅** As the engine, `Species` is a first-class lookup entity listed by
  `prose --list-species` (SS-US-Fs2). *(verified by `ListSpeciesCli.cs` wired as `--list-species` in
  `Program.cs`; `SpeciesRepository` in DI.)*

- **SS-US-L6 ✅** WITHDRAWN 2026-10-15. This asked to ship the LocalDB schema to a separate production database. The repo has no production database: there is one database, reached only through the Hub (deployed from `C:\Apps\MindAttic\Prose\`). Facet removal, `VoiceChangeLog` and `UniverseId` are already in that database (SS-US-F2, SS-US-E1, SS-US-U1).

## Epic M — Emotional Intelligence Examination {#epic-m}

> The emotional-examination schema exists; no examiner writes to it
> ([BIBLE ADR-10](BIBLE.md#SS-§14): instruments stay only while their findings get applied).

- **SS-US-M3 ✅** As an author, `prose --migrate-sql --emotional-examination` creates 4 tables +
  `Beat.EmotionalScore` column idempotently. *(acceptance: re-runnable, exits 0 on 2nd run; all 4
  tables exist; `Beat.EmotionalScore` float? column on the temporal Beats table; verified by CLI
  run against LocalDB.)*


## Epic N — Voting kill-switch (SS-LAW-17) {#epic-n}

> Every engine path that solicits LLM ballots/scores/votes (reader panels, Legion votes, census,
> entity rating ballots, book/story quality scoring) is DISABLED BY DEFAULT and runs only with an
> explicit per-invocation override. LLM use for PROSE (generation, drafting, polish) is never gated.
> One central gate — `VotingGate` — is consulted at the entry of each ballot-soliciting flow.
> See [SS-LAW-17](BIBLE.md#SS-§5) and [LOGIC.md §6](LOGIC.md).

- **SS-US-N1 ✅** As the engine, voting is OFF by default: the committed root `legion.json` carries
  `"votingEnabled": false`, and absence of the key resolves to OFF. *(evidence:
  `VotingGateTests.ReadVotingEnabledDefault_KeyFalse_ReturnsFalse`,
  `…_KeyAbsent_ReturnsFalse`, `…_NoFile_ReturnsFalse`, `CommittedLegionJson_ShipsVotingDisabled`.)*

- **SS-US-N2 ✅** As an author, a gated flow with no override is refused with the exact, actionable
  message *"Voting is disabled by default (SS-A44). Pass --allow-votes (CLI) / allowVotes:true (MCP)
  to run this explicitly."* and one logged warning. *(evidence:
  `VotingGateTests.EnsureAllowed_Disabled_NoOverride_Throws_WithExactMessage`,
  `…_IsAllowed_Disabled_NoOverride_IsFalse`.)*

- **SS-US-N3 ✅** As an author, the explicit override `--allow-votes` lifts the voting gate on the flows that still exist. Most vote-gated flows (`--review-node`, `--review-entity`, `--dual-read`, `--auto-run`, `--continuity`) are switched off entirely by `DeactivatedInstruments` (RFC 0014), and the MCP `review_story` tool is gone. The factory itself uses no votes. *(evidence: `VotingGateTests.EnsureAllowed_Disabled_WithOverride_DoesNotThrow`, `…_EnabledByDefault_DoesNotThrow_EvenWithoutOverride`, `BallotSolicitingServices_DependOnVotingGate`; re-run 2026-10-15.)*

- **SS-US-N4 ✅** As the engine, PROSE generation is never gated — `BeatGeneratorService` /
  `ProseWriterRouter` construct and run without any `VotingGate` dependency, and the auto-run
  pipeline skips (never fails on) the scoring step when voting is disabled. *(evidence:
  `VotingGateTests.ProseGenerationServices_DoNotDependOnVotingGate`;
  `ChapterCloseProcessorService.ProcessAsync` skips tiered review + fork when voting is off.)*

## Epic O — Reader QA, glossary, KDP listing, universes {#epic-o}

- **SS-US-O1 ✅** RETIRED 2026-09-22 (RFC 0014; the factory forbids LLM judges). Reader-Proxy QA (LLM comprehension probes with a Sonnet arbiter, gripe jury, pairwise duels; `--reader-qa`, `--duel`, `--dual-read`) is switched off by `DeactivatedInstruments`: COMPREHENSION 237 and GRIPE 3 filed, 0 applied corpus-wide. Of its four parts only the deterministic `--lint-prose` (CraftNativeRules) stays live. Reader-facing quality is now the factory F5 read under Rule #1 (cold-reader comprehension), done by hand. Historical evidence: commits `3bb9d2f19`, `7ef9078cd`, `484614b23`, `c04e90e78`.

- **SS-US-O2 ✅** As the author, the Master Glossary system (shipped 2026-08-05) gives each
  universe a back-matter glossary, with each book's own glossary auto-derived as the live subset
  of terms that actually appear in its prose (a term dropped from prose disappears from that
  book's glossary on the next regenerate). *(evidence: `GlossaryTerms` DB table + migration
  `AddGlossaryTerms`; `GlossaryService.GenerateMasterAsync`/`GenerateForBookAsync`; CLI
  `--generate-glossary`/`--generate-book-glossary`; MCP `upsert_glossary_term`/
  `list_glossary_terms`/`generate_glossary`/`generate_book_glossary`; DOCX export wired via
  `DocxExportService` appending a page-broken Glossary section — EPUB/PDF back matter not yet
  extended; seeded 27 GLMZ + 24 SCRY terms same day, both from primary-source docs, no invented
  definitions.)*

- **SS-US-O3 ✅** As the author, KdpPublish (`src/Prose.KdpPublish`, WPF/WebView2)
  automates a book's **first-time** KDP listing, not just republishing an existing one — no
  ASIN/KdpTitleId required going in. *(evidence: `KdpOperatorService.ProcessBookAsync` branches to
  a 25-step `BuildNewListingSystemPrompt` flow when `Asin`/`KdpTitleId`/`PublishUrl` are all null;
  new tool set in `KdpTools/` — `CreateNewListingTool`, `SetPriceTool` (real CDP keystrokes, not
  value-injection), `SelectCategoriesTool`, `SetAiDisclosureTool`, `CapturePublishedAsinTool`, etc.
  Verified live twice via direct CLI/automation runs: JOAN ("Jeanne d'Arc: Prophecy or
  Pathology?") published 2026-08-03, titleId `A2UB6TMY4GKY0C`; RESIST ("Resistance: Three
  Centuries of Irish Rebellion") published 2026-08-04, titleId `A19W9UCNTX3YD1`, confirming the
  flow generalizes beyond the first book — under 3 minutes / ~35 tool-call iterations end to end.)*

- **SS-US-O4 ✅** As the author, a 6th universe (**EROTICA**) is seeded, alongside renaming
  SOURCE→NONFICTION and EPIC→FICTION for naming clarity (2026-08-04). *(evidence: verified by
  commit `f74d26f46` "feat(universes): add EROTICA (6th), rename SOURCE→NONFICTION,
  EPIC→FICTION"; CLI `prose --universe list` shows all 6 slugs.)*


## Epic Q — Backlog cleanup under the factory rules
- **SS-US-Q1 ✅** As the maintainer, I clean up Epics A, B, C and D (canon foundation, writing surface, generation, interconnect: 16 stories) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q2 ✅** As the maintainer, I clean up Epics E, F and I (voice harvest, coverage/observability, feedback loops: 10 stories; check E1/E2 and I-stories for LLM-judge or review-score dependencies) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q3 ✅** As the maintainer, I clean up Epics K, L, M, N and O (service law, architecture, emotional intelligence, voting kill-switch, reader QA: 19 stories; L6 is partial) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q4 ✅** As the maintainer, I clean up Epics U and P (multi-universe and SCRY/Fantasy books: 13 stories) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q5 ✅** As the maintainer, I clean up Epic G (Bushido Coda: SS-US-G1..G5 and G5a..G5g, 12 stories; G3 asks for an LLM review pass, G4 for a 100-story outline, G5g for a review-score campaign — all forbidden, restate or supersede against BCODA's factory state) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q6 ✅** As the maintainer, I clean up Epic H stories SS-US-H1..H4 and their children (H1a..H4e; H1f asks for a review panel ≥85%, restate or supersede against each book's factory state) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q7 ✅** As the maintainer, I clean up Epic H stories SS-US-H5..H8 and their children (H5a..H8g) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q8 ✅** As the maintainer, I clean up Epic H stories SS-US-H9..H12 and their children (H9a..H12g; H9e..H9h are planned) so the backlog matches the novel factory and states only what is true. Rules to apply (sources: docs/rfc/0015-novel-factory.md, the SessionStart forbidden list, SS-LAW-17): (1) THE PLAN IS THE FACTORY — book work is computed by the Hub (prose --factory next / --order list), never stored as story text, so a story that tracks chapters, acts, word counts or beats of a book is restated as a pointer to the factory or marked superseded; (2) THE BOOK IS THE BOOK — no outline, spine, ledger, summary or reconciler; (3) no LLM judges, review panels, votes or review-score thresholds (≥85% etc.); (4) RESULTS NOT TOOLS — a story is done only when its effect is applied and verified, never because a tool exists; (5) every ✅ cites a test or CLI check that exists today. For each story: show it (workflow story show <ID>); grep src/Prose.UnitTests for every cited test and run them (dotnet test --filter); a ✅ whose evidence is missing or failing drops to partial with the gap stated; rewrite (workflow story edit) any text that asks for a forbidden thing; mark a story that the factory now owns as done-superseded with a pointer. Touch no prose, no canon, no DB outside the workflow CLI. Then run workflow db render, commit the regenerated docs/USER_STORIES.md and list each story changed and why in the ticket.
- **SS-US-Q9 ⬜** As the maintainer, every story ID in the Prose backlog is unique. SS-US-H7 and H7a, H7b, H7c each exist twice in the workflow database. Compare each pair with workflow story show; if they say the same thing, merge into one; if they differ, renumber the later one to the next free ID in epic H. Then workflow db render, check that docs/USER_STORIES.md has no duplicate IDs, and commit. BLOCKED 2026-10-15: the workflow CLI has no command to renumber or delete a story and `db check` does not report duplicate IDs, so this needs a MindAttic.Workflow change first (`story renumber`, duplicate-ID check). After renumbering, give both H7 stories the same factory note as the other epic-H books (SS-US-Q7): Rook Trilogy = MxG F5 fails 3/3, NxR F5 fails 1/4, CxC F5 fails 3/3 (SS-T77); Steppin Razor (SRZR) F5 fails 3/3, F1 waiting (SS-T77); mark both partial and their review-score children historical.
- **SS-US-Q10 ⬜** As the author, every remaining GLMZ book is fully tagged, read and verified: work open tickets SS-T56..SS-T68 one book per run, in order, each to its own done criteria (factory F1-F7 green for that book), closing each ticket as it lands. Story is done when all thirteen are closed.
- **SS-US-Q11 ⬜** As the author, every book in the other universes (SCRY, NONFICTION, FICTION, HORROR, EROTICA, GOSPEL, EVE, ANTHOLOGY) is fully tagged, read and verified: work tickets SS-T69..SS-T76, which open one ticket per book, then work those per-book tickets to factory F1-F7 green. Done when every non-calibration book's factory status is green.
- **SS-US-Q12 ⬜** As the author, no pressed book carries un-read beats after later record edits: work ticket SS-T77 (final re-read, F1 recommit, re-export) after SS-US-Q10 and SS-US-Q11 are done.

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| SS-T1 | 2026-10-12 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| SS-T2 | 2026-10-12 | chore | closed | /commit now logs every commit as a ticket in this table | `.prose/commands/commit.md` |
| SS-T3 | 2026-10-13 | chore | open | Metadata completeness campaign: pronoun/entity tags, character history, continuity checks across every book (Hub WO:01a1337c) | — |
| SS-T4 | 2026-10-13 | fix | closed | Contributions gate: surface a bare ambiguous name (e.g. two Sables) sitting untagged instead of silently losing it | WO:01a13278 |
| SS-T5 | 2026-10-13 | fix | closed | untagged_names must not gate contributions-complete: a harmless word/entity-name collision (CURRENT) would block it forever | WO:01a13278 |
| SS-T6 | 2026-10-13 | chore | closed | Consolidate Prose deploy into root deploy.ps1 (rebuild only when changed, -Force) writing one C:\Apps\MindAttic\Prose\deploy.bat <App>; drop repo deploy-*.bat | deploy.ps1 |
| SS-T7 | 2026-10-13 | feature | open | Prose.Launcher and Prose.Writer: move from MindAttic.Components.Wpf 3.0.0 to 5.0.0 and turn on McWindow.DarkTitleBar | MindAttic.Components.Wpf 5.0.0 |
| SS-T8 | 2026-10-13 | chore | closed | DWIACE: finish F1 (re-verify Ines Vasquez after stay-terms fix and any entity un-read by record fixes), re-read any unread beats, then F7 export | WO:01a1337c SS-T3 |
| SS-T9 | 2026-10-13 | chore | closed | VATD: re-read beats un-read by record fixes (Vultures, Do-yun, Scarab, Ekow, Maisy, Splicer, GLMZ, Tomas, Sclose, Holme), finish F1 (8 records were held, now fixed), splice #21756 pink-line if not done, F7 re-export | WO:01a1337c SS-T3 |
| SS-T10 | 2026-10-13 | chore | closed | BCODA: confirm the 13 held records are fixed (Halvorsen, Candor, Pivot, War Dog, Calloway, Vultures, Mira, Gingerbread House, Nadia Park, Lotus Roster Marker, Iowa, Marisol, Breckenridge), re-read un-read beats, finish F1 all 48 units, F7 re-export (audio only on author request) | WO:01a1337c SS-T3 |
| SS-T11 | 2026-10-13 | chore | closed | CYB1 (Pieces): confirm held records fixed (ages per book for Pixel/Kyle/Mrs Chen/Sasha/Femi/Glim/Rafi, Ekow 10 vs 12 years, Velvet Tine capacity, South Side and Circuit retags, truck livery splice #23245), re-read, finish F1, F7 export | WO:01a1337c SS-T3 |
| SS-T12 | 2026-10-13 | chore | closed | CYB1 author calls: Yemina still in beats 16,26,28,30,75,95,135,182,184 though cut in the 2026-10-11 recast (#23206); Able 'fourteen years' vs commission after 2227 in beats 133,135,184 and the Clean Books lyric (#23315); fix note #23310 resolution text (three dead stands) | WO:01a1337c |
| SS-T13 | 2026-10-13 | chore | closed | MNEMO: splice #8124 (Ekow on the roof, not in the schism), finish F5 reads and F1, F7 export; retag 'The Seam' where used as a place (tagged as faction); 'Shen crossing' tagged to character Shen | WO:01a1337c SS-T3 |
| SS-T14 | 2026-10-13 | chore | open | All five factory books: rerun contributions completeness survey (every beat complete) after record and splice work settles; fix any beat that is not complete | WO:01a1337c SS-T3 |
| SS-T15 | 2026-10-13 | fix | open | Velvet Tine guid 019f861c... also exists as a Character row, so contributions demand a line for a place in every beat that tags it (DWIACE 11, CYB1 14,19,20,22,23,48,90,140,144); remove the stray character row or exclude non-character tags | BeatContributionService.TaggedCharactersAsync |
| SS-T16 | 2026-10-13 | fix | open | Sentence-initial 'Can' auto-tags to Can Zaragoza in beats 6647, 10542, 11273, 17135; the save path re-derives the tag, so it cannot be removed by editing; need a per-occurrence suppression | BeatMarkup autotag |
| SS-T17 | 2026-10-13 | chore | open | Records carry one age but books span years (BCODA ~2226, CYB1 2230): decide an age-by-book convention for character records and apply it catalog-wide | WO:01a1337c |
| SS-T18 | 2026-10-13 | chore | open | Merge duplicate document records 'I Archived My Father and I Regret It' (98df3502, f5a88ed9); both rewritten 2026-10-13 to the daughter/Read version | WO:01a1337c |
| SS-T19 | 2026-10-13 | chore | open | DWIACE: beats 21056, 21095, 21104, 21106, 21441, 21524 tag the Read archetype where the ability (vocabulary The Read 816dbd83) is meant; retag per occurrence | WO:01a1337c |
| SS-T20 | 2026-10-13 | fix | open | VATD prose leftovers: morning vs night timeline wavers (Maisy's heart 'since this morning' vs 'tonight'); #21606 Ekow finds the shop empty vs Osei in the back room #21659; Tomas drives #21809 then Levin #21811 | WO:01a1337c |
| SS-T21 | 2026-10-13 | fix | closed | CYB1 prose leftovers to check: position 166 'four' downed frames (only three found); position 167 Rafi standing then kneeling | WO:01a1337c |
| SS-T22 | 2026-10-13 | chore | open | Contributions campaign: rest of the GLMZ catalog (~30 books) through tagging, contribution lines, Descriptions, F5 and F1 using the scratchpad pipeline (mkbook, agent briefs, check.py, apply.py, applydesc.py, queue.py) | SS-T3 |
| SS-T23 | 2026-10-13 | docs | open | RFC 0015: record the contributions rollout cost (pronoun tagging stales Descriptions and reads; ~200 calls per 215 beats) per law 01a1364e | WO:01a1337c |
| SS-T24 | 2026-10-13 | fix | open | Dr. Nadia Park location ('Off-GLMZ, not disclosed even to Kyle') contradicts BCODA #20747 (sector address in Z4) but lives in EntityStateEvents, which no CLI or MCP command writes; add a write path or correct it, then finish BCODA F1 | RFC 0015 EntityStateEvents |
| SS-T25 | 2026-10-13 | chore | open | Audit 2026-10-13 record patches made while two agents shared scratchpad patch_tmp.json (ef.py): confirm no payload landed on the wrong entity (stray 'age' fields especially) | WO:01a1337c |
| SS-T26 | 2026-10-13 | chore | open | Author call: Ekow Ato's years with the Lotus house: VATD ten, Iron & Silk (two years later, ~2226-27) twelve, Pieces (2230) ten in beats #23234,#23239,#23257,#23282,#23283,#23286 (should be ~fifteen by that timeline) | WO:01a1337c |
| SS-T27 | 2026-10-13 | fix | open | Location field (EntityStateEvents) is unwritable but stale/contradicting for Kyle ('Pixel's apartment'), Mrs. Chen, Sasha Vo, Dr. Nadia Park; needs a write path (RFC 0015 s11) then corrections | RFC 0015 EntityStateEvents; SS-T24 |
| SS-T28 | 2026-10-13 | fix | open | CYB2/CYB3 tags still pin Chicago's South Side to the Milwaukee South Side record: beats #23410, #23426, #23546, #23702, #23715 -> re-pin to The South Side (Chicago) 01a13683-458a | WO:01a1337c |
| SS-T29 | 2026-10-13 | fix | open | Small canon leftovers: DWIACE Cuts #21184 'sealed 2096-03-14, fifteen years ago' date arithmetic; Sasha Vo array installed 2210 at 12 vs 'year 10' in 2226 and 'year 14' in 2230; The Circuit record calls the district 'Meridian's black market' | WO:01a1337c |
| SS-T30 | 2026-10-13 | chore | closed | MNEMO F1: fix 13 held records vs prose (2230): Amara Osei (Batch 44-C 2222->2226; three years on the beat), Seto Banda (not a Batch 44-C recipient), Ciro Fonseca (blade not thumb; journalist not tech lines; liaison/coffee-tell hooks), Nuru under_pressure, Tobenna Okeke bakery in the Veil, Batch 44-C (2226; Great Lakes Metropolitan Zone), Orison Neuretics (2226; sub-protocol cuts weight not memory; Tribune liaison), Cellvault (maintenance provider not employer), Carrion mis-tag #7982, Orison Communications public_facing, Arcturus sectors (Arcturus Civil air processing), Pulse pod vs Mach-6 tubes, GLMZ name origin; then F1 commit and F7 | WO:01a1337c |
| SS-T31 | 2026-10-13 | fix | open | MNEMO tags: The Seam faction tagged 5x where 'Seam' is a place; Tessera Corponation record names a 2133 'Dr. Amara Osei' colliding with the MNEMO protagonist | WO:01a1337c |
| SS-T32 | 2026-10-13 | fix | open | MNEMO positions 405-426 (#8276-#8293): Ciro, an Orison liaison, calls Amara 'a colleague', grants her two weeks' paid Tribune leave ('I provisioned it this morning') and controls her tier access: an old Orison-employee draft; needs an author rewrite of the passage | WO:01a1337c |
| SS-T33 | 2026-10-14 | chore | open | NUnit tests for RepetitionLintService (src/Prose.Core/Services/RepetitionLintService.cs): seed beats via CanonFixture and assert LintAsync flags echo words (3+ hits of a non-stopword within 50 tokens, stopwords ignored), crutch 3-4-grams (2+ per beat, 3+ per chapter, shorter sub-phrases suppressed), pet words in over 30 percent of beats, 5+ consecutive quoted-only paragraphs with no tag, airless runs of 6+ beats under 2 percent dialogue, floating-heads beats (90 percent dialogue, 600+ words), and that re-running does not duplicate LINT findings; it is the only mechanical repetition check and has zero tests | — |
| SS-T34 | 2026-10-14 | chore | open | NUnit tests for SegmentAggregator.Build (src/Prose.Core/Services/SegmentAggregator.cs): with hand-built Reviewer lists assert Pareto-improving beats (every cluster scores low), contested beats (clusters disagree, who-gains/who-loses and the floor rule), seams (low beat between strong neighbors), flow-guard reporting, cluster labels/sizes/means, and edge cases (no reviewers, k larger than reviewers, beatCount 0, missing beat scores); this decides what the author fixes first | — |
| SS-T35 | 2026-10-14 | chore | open | NUnit tests for ReviewCostEstimator (src/Prose.Core/Services/ReviewCostEstimator.cs): Estimate and EstimateActual arithmetic per model with cached input, unknown models fall back to Haiku rates and IsKnown is false, RegisterPricing adds/overrides rates (GetRatesFor), costs ceiling-round to the penny (0.009 renders 0.01), and RenderTable/RenderActualTable rows are all exactly RowWidth characters; operators confirm spend from this table | — |
| SS-T36 | 2026-10-14 | chore | open | NUnit tests for TextIntegrityService (src/Prose.Core/Services/TextIntegrityService.cs): ScanAsync over CanonFixture rows finds every U+FFFD with correct table/column/row/offset and returns nothing for clean text, ApplyFixAsync replaces only that occurrence with the chosen char (Phi, em dash, section sign) and is a safe no-op when the text has since changed or the char is gone; guards the QUANTA currency-symbol corruption repair | — |
| SS-T37 | 2026-10-14 | chore | open | NUnit tests for FamilyTieService and GeneticsInheritanceService (src/Prose.Core/Services): parent_of edges directional, sibling_of written both ways without duplicates, spouse/grandparents/cousins/siblings derived correctly; BlendFromParentsAsync averages two parents 50/50 (absent region = 0), single parent copies, root characters get nothing, seeded Random noise stays within NoisePercentPoints, result renormalizes to 100 with entries under DustThreshold dropped, siblings differ; PropagateAllAsync report counts (Processed/Updated/Skipped/Roots) | — |
| SS-T38 | 2026-10-14 | chore | open | NUnit tests for FamilyGeneratorService and NamePoolService: ProposeAsync with a seeded Random proposes parents/siblings/spouse/children carrying the subject surname chunks and writes nothing until ApplyProposalAsync, which writes stub characters plus family edges; PickFirstName respects gender; NamePoolService.EnsureUniqueFirstName swaps a first name already in canon, keeps the surname, pushes old full and first name into aliases, returns false when unique; SamplePreferredNames excludes used names | — |
| SS-T39 | 2026-10-14 | chore | open | NUnit tests for LocationContradictionService.ScanAsync (src/Prose.Core/Services/LocationContradictionService.cs): from located_at edges with StoryValidFrom..Until and beat InWorldDate plus chapter characters, flag one character in two different places at overlapping times or closer than MinTravelMinutes, do not flag same place, different characters or far-apart times, honor the minTravelMinutes override, and return an empty result on an empty DB | — |
| SS-T40 | 2026-10-14 | chore | open | NUnit tests for ClaimBeatAnchorService (src/Prose.Core/Services/Audit/ClaimBeatAnchorService.cs): BackfillAsync sets ContinuityClaim.SourceBeatId to the single beat whose text contains the normalized snippet (curly quotes, whitespace), leaves ambiguous multi-beat and no-match claims unanchored, never touches already-anchored claims, report counts are right; FindStaleSnippetsAsync and SupersedeStaleAsync detect and supersede claims whose snippet no longer appears in the anchored beat; anchors gate the Tuned Read | — |
| SS-T41 | 2026-10-14 | chore | open | NUnit tests for Prose.Hub ToolDispatch (src/Prose.Hub/ToolDispatch.cs, needs a Prose.Hub ProjectReference in Prose.UnitTests): LooksMutating classifies mutating verb prefixes and the explicit non-verb write-tool list, a mutating tool invoked without an explicit universe is refused while read tools run, tool class and Impl method resolution, case-insensitive JSON arg binding with defaults, and clear errors for unknown tool/method or bad args; this is the universe-safety gate for every migrated MCP tool | — |
| SS-T42 | 2026-10-14 | chore | open | NUnit tests for ReviewLlmTransport (src/Prose.Core/Services/ReviewLlmTransport.cs): BuildRoute local vs cloud and the allowedProvidersOverride/localModelOverride parsing, ProviderIds and JuryProviderIds filtering, ResolveKey for configured vs missing keys, ResolveModel cheap vs full per provider, AssignJuryAsync spreading seats across live providers and falling back when providers are dead, with ResetLivenessCache between tests; wrong routing silently sends reviews to the wrong or a paid model | — |
| SS-T43 | 2026-10-14 | chore | open | NUnit tests for BookStateLedgerService (src/Prose.Core/Services/BookStateLedgerService.cs): RecordAsync and GetCurrentStateAsync keep the latest state per named plot state, valid transitions per StateType (Crisis Open to Escalated to Climaxed to Resolved to Reopened, etc.), PersistPipeDelimitedEventsAsync parses well-formed lines and skips malformed/blank ones, and BuildContextAsync renders the BOOK PLOT STATE block (empty node gives empty block); it stops the engine re-resolving closed crises | — |
| SS-T44 | 2026-10-14 | chore | open | NUnit tests for StoryStateQuery (src/Prose.Core/Composition/Ledger/StoryStateQuery.cs): GetOnScreenFactsAsync and GetOnScreenFactsForEntitiesAsync return only facts for entities mentioned in the beat plus its place, only EntityStateEvents at or before the beat StoryPosition (later events excluded), the latest value per fact wins, open/advanced obligations scoped to on-screen entities, and empty snapshots for unknown beats; this bounds what the v4 writer believes is true | — |
| SS-T45 | 2026-10-14 | fix | open | Prose.Hub's MindAttic.Log.db reached 183 MB in 6 days with the default 365-day retention; lower RetainedDays and/or the minimum level so it can't grow without bound | — |
| SS-T46 | 2026-10-14 | chore | open | Prose.Mcp logs only to a Serilog mcp-.txt file; send its diagnostics to MindAttic.Log at VaultPaths.LoggingDbPath (keep stdout clean for MCP) | — |
| SS-T47 | 2026-10-15 | chore | closed | Backlog cleanup batch Q1: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q1 38e2d7300 |
| SS-T48 | 2026-10-15 | chore | closed | Backlog cleanup batch Q2: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q2 99bd61efe |
| SS-T49 | 2026-10-15 | chore | closed | Backlog cleanup batch Q3: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q3 cc7e9372a |
| SS-T50 | 2026-10-15 | chore | closed | Backlog cleanup batch Q4: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q4 a7ced8f8a |
| SS-T51 | 2026-10-15 | chore | closed | Backlog cleanup batch Q5: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q5 a088c8cbb |
| SS-T52 | 2026-10-15 | chore | closed | Backlog cleanup batch Q6: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q6 6768f7afb |
| SS-T53 | 2026-10-15 | chore | closed | Backlog cleanup batch Q7: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q7 6768f7afb |
| SS-T54 | 2026-10-15 | chore | closed | Backlog cleanup batch Q8: audit each story in the batch against the factory rules (plan is the factory, book is the book, no LLM judges/votes/outlines, results not tools, every done story cites a passing test); edit, downgrade or supersede via the workflow CLI, render, commit | SS-US-Q8 6768f7afb |
| SS-T55 | 2026-10-15 | fix | open | Duplicate story IDs: SS-US-H7 and H7a-H7c each exist twice in the workflow DB; merge or renumber, render, verify no duplicate IDs | SS-US-Q9 |
| SS-T56 | 2026-10-15 | chore | open | The Ur (the-ur-01a123de): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T57 | 2026-10-15 | chore | open | Wells (wells-01a123de): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T58 | 2026-10-15 | chore | open | Standing Contract (SCON) (standing-contract-01a02d2c): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T59 | 2026-10-15 | chore | open | The Long Cut (TLC) (the-long-cut-019f3007): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T60 | 2026-10-15 | chore | open | Testament (TEST, draft) (testament-01a11cd1): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T61 | 2026-10-15 | chore | open | Testament (court-martial, TEST2: reuse TEST work where raw text matches) (the-court-martial-019ed361): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T62 | 2026-10-15 | chore | open | Iron & Silk (IxS) (iron-silk-019f43b9): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T63 | 2026-10-15 | chore | open | Eleven Minutes (eleven_minutes): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T64 | 2026-10-15 | chore | open | The Voice You Trust (the_voice_you_trust): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T65 | 2026-10-15 | chore | open | GLMZ Stories: Vol. 1 (glmz_stories_vol_1): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T66 | 2026-10-15 | chore | open | Read the Room (replan) (read-the-room-replan-01a1384d): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T67 | 2026-10-15 | chore | open | The Way Up (high-five-019f787d): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T68 | 2026-10-15 | chore | open | The Way Down (the-fall-down-019f78f4): Contributions campaign (SS-T3 protocol, by hand, one unit at a time): first run prose --factory status for this book and close this ticket if F1-F7 are already green. Otherwise: tag every pronoun (character guid or the all-zero none guid), write contribution lines, restate Descriptions (strict F3), F5 read with --mark-read, F1 verify-entity begin/commit every tagged entity, then F7 export. File Rule #1 defects as read notes; record entity fixes through the Hub with read-back. No bulk tools, no LLM judges. | SS-T3, SS-T22 |
| SS-T69 | 2026-10-15 | chore | open | Universe scry: list its books (prose --universe scry), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T70 | 2026-10-15 | chore | open | Universe nonfiction: list its books (prose --universe nonfiction), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T71 | 2026-10-15 | chore | open | Universe fiction: list its books (prose --universe fiction), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T72 | 2026-10-15 | chore | open | Universe horror: list its books (prose --universe horror), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T73 | 2026-10-15 | chore | open | Universe erotica: list its books (prose --universe erotica), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T74 | 2026-10-15 | chore | open | Universe gospel: list its books (prose --universe gospel), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T75 | 2026-10-15 | chore | open | Universe eve: list its books (prose --universe eve), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T76 | 2026-10-15 | chore | open | Universe anthology: list its books (prose --universe anthology), then open one ticket per book with the same contributions-campaign scope as the GLMZ book tickets (pronoun tags, contribution lines, F3 Descriptions, F5 read, F1 entity verify, F7 export), and close this ticket | SS-T3 |
| SS-T77 | 2026-10-15 | chore | open | Final honest re-read sweep: every finished book whose beats were un-read by later record edits (BCODA, CYB1, ICFI, SPRW, BTL, VATD, DWIACE, MNEMO, TWD, TWU and any others the factory shows) gets an F5 re-read, F1 recommit, then F7 export at a new version; done when prose --factory status is all green for every pressed book | SS-T3, SS-T14 |
| SS-T78 | 2026-10-15 | chore | open | Delete the retired LLM contradiction sweep: CanonContradictionService, its ChapterCloseProcessorService step and AutoRunCli wiring (only reachable via deactivated --auto-run; 81 filed, 0 applied; LLM judge forbidden by the factory). Keep any parsing helper another live service still calls; build + full test suite green | SS-US-D3 |
| SS-T79 | 2026-10-15 | fix | closed | WorldLoreTests: 11 cases silently skip via Assume because they read canon files (story_bible.json, tone bible, vocabulary/technology dirs, Pulse doc) that moved into the DB; rewrite each against the Hub-backed canon or delete it, so no test passes by skipping | SS-US-D5 e8b9beac8 |
| SS-T80 | 2026-10-15 | chore | open | Delete the retired per-save LLM tails and their findings producers: the commented-out SemanticFidelity drift / blast-radius / continuity re-extraction / obligation blocks in NodeWorkbenchService.UpdateBeatTextAsync, the VOICE-HARVEST raise in NodeReviewService, and EstimateCostCli's check-fidelity line. Keep anything a live path still calls (SemanticFidelityService embedding refresh used by --reembed, ContinuityCli). Build + full suite green | SS-US-E2, SS-US-I3, SS-US-I6 |
| SS-T81 | 2026-10-15 | chore | open | WorldTickService (SS-US-L4) is an unused tool: off by default (WorldTickEnabled=false), and when on it only writes filler 'idle' EntityStateEvents for up to 100 characters. Check the setting has never been on (grep EntityStateEvents for AspectKey='world-tick' via the Hub); if no rows, delete the service, its hosted-service registration and the setting, and mark SS-US-L4 retired; build + full suite green | SS-US-L4 |
