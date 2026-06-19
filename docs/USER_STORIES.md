---
codex: 1
project: StreetSamurai
code: SS
layer: stories
status: living
updated: 2026-06-15
---

# StreetSamurai — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned · 🗑️ cut. Every ✅ cites the test.
> Migrated from `ARCHITECTURE.md` §4 (goals table) on 2026-06-07. Test tokens are NUnit
> methods/classes in `v3/StreetSamurai.UnitTests/`. CLI smokes run against LocalDB.

## Epic A — Canon-as-database foundation

- **SS-US-A1 ✅** As the engine, I store every fact in SQL (`Entities` + `Records.Json` + relational
  projections) so generation reads truth, not files. *Given a clean assembly, When the service
  graph is built, Then it resolves and contains no facet types.* *(verified by `DiRegistrationTests`,
  `InterfaceRegistrationTests`, `CanonEngineTests.CoreAssembly_HasNoFacetTypes`.)*
- **SS-US-A2 ✅** As the engine, I expose ~28 canon entity types with tolerant JSON converters so
  malformed canon never crashes a load. *(verified by `ModelSerializationTests`,
  `JsonDefaultsTests`; CLI `ss --coverage` lists 28 types.)*
- **SS-US-A3 ✅** As the engine, I materialize full-character reads from `CharacterReadModels` so a
  deep read is one column, not a 50–80 s join. *(verified by `CharacterReadModelTests`.)*
- **SS-US-A4 ✅** As the engine, I keep the Beat→Strand model as the single format with no nested
  strands. *(verified by `StrandWorkbenchServiceTests`, `StrandMigrationServiceTests`,
  `StrandCliRoundTripTests`.)*

## Epic B — Writing surface (Strand workbench)

- **SS-US-B1 ✅** As an author, I can insert/split/join/delete beats and edit beat text so I can
  shape a strand. *Given a strand, When I insert/split/join/delete, Then beats land in order and
  audio invalidates on text change.* *(verified by
  `StrandWorkbenchServiceTests.InsertBeat_AtTop_OfEmptyStrand_ProducesOneBeat`,
  `SplitBeat_AtSentenceBoundary_ProducesTwoBeats`, `JoinBeat_MergesIntoPrevious_DeletesAbsorbed`,
  `DeleteBeat_LastMembership_RemovesBeatRow`,
  `UpdateBeatText_MarksStale_RecomputesHash_InvalidatesAudio`.)*
- **SS-US-B2 ✅** As an author, I get optimistic-concurrency protection on beat edits so a stale
  write is rejected. *(verified by `StrandWorkbenchServiceTests.UpdateBeatText_ExpectedTimestamp_Mismatch_ThrowsConflict`,
  `UpdateBeatText_ExpectedTimestamp_MatchesCurrent_Succeeds`.)*
- **SS-US-B3 ✅** As an author, I set per-beat trailing silence (gap-after) so audio paces
  correctly. *(verified by `StrandWorkbenchServiceTests` gap-after round-trip;
  `ComputeTextHash_IsDeterministic_AndIgnoresLeadingTrailingWhitespace`.)*
- **SS-US-B4 ✅** As an author/CLI/LLM client, I reference a beat by the `strand-guid.beat-guid`
  handle. *(verified by `BeatHandleTests`.)*
- **SS-US-B5 ✅** As an author, inline markdown + tone tags render to emoji in the writer while the
  raw form reaches TTS. *(verified by `BeatFormatterTests`.)*

## Epic C — Generation & outline

- **SS-US-C1 ✅** As the engine, an outline must pass the gate before prose fires. *(verified by
  `OutlineGateTests`, `OutlineServiceTests`.)*
- **SS-US-C2 ✅** As the engine, `BeatPromptBuilder` injects canon facts + voice rules into every
  beat prompt. *(verified by `BeatPromptBuilderTests`.)*
- **SS-US-C3 ✅** As the engine, the Beat Doctrine + house voice are codified in the DB and emitted
  to every prompt (`--seed-voice-rules` is idempotent). *(verified by CLI `ss --seed-voice-rules`
  +9/+9/+4 idempotent; `CombatSceneWriterTests`, `StoryMethodologyServiceTests`.)*

## Epic D — Interconnect, validation & self-correction

- **SS-US-D1 ✅** As the engine, `CanonRetrievalService` pulls relevant canon across **all** types
  into generation. *(verified by `ServiceInterconnectionTests`, `SemanticIndexServiceTests`; CLI
  `ss --canon-retrieve` surfaced apparel/weapon/document.)*
- **SS-US-D2 ✅** As the engine, embedding lookups degrade gracefully when the index is cold.
  *(verified by `EmbeddingFallbackTests`.)*
- **SS-US-D3 ✅** As the engine, a contradiction sweep raises approval-gated `CANON-CONTRADICTION`
  findings (and optional REWRITE proposals) without auto-writing. *(verified by `CanonEngineTests`
  parse/chunk/severity: `Parse_ValidArray_MapsFields`, `Chunk_SplitsOnParagraphsUnderBudget_AndCoversAll`,
  `ParseSeverity_MapsKnown_DefaultsToMedium`; CLI `ss --check-canon [--fix]`.)*
- **SS-US-D4 ✅** As the engine, continuity extraction resolves any entity type via the universal
  `Entities` table (F2). *(verified by `CanonEngineTests.RuleTargets_AllMapToKnownStores`,
  `NormalizeTarget_*`; `WorldConsistencyServiceTests` filtered subset.)*
- **SS-US-D5 ✅** As the engine, world facts/lore stay consistent. *(verified by `WorldLoreTests`,
  `WorldGraphServiceTests`, `KnowledgeMapServiceTests`, `InferenceServiceTests`.)*

## Epic E — Voice harvest (the flywheel)

- **SS-US-E1 ✅** As the engine, I mine winning edits from temporal history into a `VoiceChangeLog`
  (propose-then-approve). *(verified by `CanonEngineTests.FirstSentence_*`, `AddDistinct_*`,
  `ExtractJsonArray_*`; CLI `ss --harvest-voice` mined 16 edits on "Sunset Clause".)*
- **SS-US-E2 ✅** As the engine, an `<80→≥80` review crossing auto-raises a VOICE-HARVEST finding.
  *(verified by `CanonEngineTests` coverage/parse helpers; review path exercised via CLI.)*

## Epic F — Coverage, eradication & observability

- **SS-US-F1 ✅** As the operator, `ss --coverage` shows a per-type reachability matrix.
  *(verified by `CanonEngineTests.TypeCoverage_ComputesPctAndMissing`,
  `TypeCoverage_ZeroTotal_NoDivideByZero`.)*
- **SS-US-F2 ✅** As the engine, the Facet system is 100% eradicated. *(verified by
  `CanonEngineTests.Beat_HasNoFacetTag`, `OutlineBeat_HasNoFacetHint`, `CoreAssembly_HasNoFacetTypes`.)*
- **SS-US-F3 ✅** As the operator, I can export a strand to docx/md/txt/pdf/EPUB/HTML. *(verified by
  `ExportServiceTests`, `BookExportServiceTests`, `HtmlExportServiceTests`, `MarkdownServiceTests`.)*
- **SS-US-F4 ✅** As the operator, dead/orphaned services were removed and §5 invariants enforced.
  *(verified by `DiRegistrationTests` green after deletions of
  `FtpPublishService`/`ConversationalWriterService`/`StoryService`.)*

## Epic G — Narrative canon (Bushido Coda)

- **SS-US-G1 ✅** As the author, Book One is an 8-chapter spine, canon-consistent against the
  continuity laws. *Given all 8 chapters, When scanned for forbidden Silence/Chorus power terms,
  Then CLEAN.* *(verified by the forbidden-term scan recorded in `v3/canon_writes/story_state.md`,
  2026-05-16; one benign `piezo` substring noted as non-Silence worldbuilding.)*
- **SS-US-G2 ✅** As the author, *Silence*/*Chorus* canon specs match [SS-LAW-10](BIBLE.md#SS-§5)/
  [SS-LAW-11](BIBLE.md#SS-§5). *(verified by `story_state.md` REWRITTEN 2026-05-16 entries.)*
- **SS-US-G3 🟡** As the author, every strand passes an LLM house-voice + Kyle-quip review pass.
  *(inline facet tags verified 0; the per-strand LLM review/harvest pass is the residual — Fv.)*
- **SS-US-G4 ⬜** As the author, the 100-story outline is developed past the 8-chapter spine
  (`bushido_coda_100_stories_outline.md`; stories 9+ are sketches).

## Epic U — Multi-Universe support

> The engine becomes universe-agnostic: GLMZ is Universe #1, Fantasy/Steampunk is Universe #2, and
> more can be added. Single `UniverseId` FK per row (1:M); crossover entities are duplicated, not
> bridged. No project rename — "StreetSamurai" stays the engine codename. See
> [SS-A2](AMENDMENTS.md) and [SS-LAW-15](BIBLE.md#SS-§5).

- **SS-US-U1 ✅** As the engine, I store a `Universe` lookup table and a non-null `UniverseId` on
  every canon/story root (`Entities`, `Strands`, `Books`) so every row belongs to exactly one world.
  Adding the column to each system-versioned root used the `SYSTEM_VERSIONING OFF → ALTER table +
  `_History` → ON` dance. *(verified by migration `add_universe_20260615.sql` applied to LocalDB;
  schema scan confirmed `UniverseId` on all three roots, temporal versioning back ON, and the
  `Universe` table + indexes present.)*
- **SS-US-U2 ✅** As the operator, all existing rows are backfilled to the GLMZ universe in the same
  migration (NOT NULL DEFAULT GLMZ), so no row is orphaned. *(verified by SQL scan: 0 non-GLMZ rows
  across 12,096 Entities / 94 Strands / 11 Books after migration; DB backed up first to
  `backups/StreetSamurai_preuniverse_20260615.bak`.)*
- **SS-US-U3 ✅** As the author, a Fantasy/Steampunk placeholder universe is seeded alongside GLMZ.
  *(verified by SQL: `Universe` seeded with `glmz` + `fantasy-steampunk`, each with a `WorldPrimer`.)*
- **SS-US-U4 ✅** As the author, I can **SwitchUniverse** — set the current universe independently in
  the UI and in each CLI/MCP process — so I can write GLMZ in one terminal and Fantasy in another at
  the same time. **Selection is per-process / per-session, never a single shared global.** Precedence:
  `--universe <slug>` flag → `SS_UNIVERSE` env var (per terminal) → UI selection → global default
  `current_universe` KV. A UI dropdown (`NavMenu`), CLI flag, and MCP `switch_universe` tool set it;
  an EF global query filter (`IUniverseContext` / `UniverseScope`) scopes every read. *(verified by
  CLI smoke `--list-strands --universe glmz` → 94 vs `--universe fantasy-steampunk` → 0, the universe
  predicate visible in the generated SQL; `DiRegistrationTests`, `InterfaceRegistrationTests`.)*
- **SS-US-U5 ✅** As the engine, per-universe config + retrieval + prompts ground prose in the right
  world with **no cross-over** (RFC 0006, fully implemented). (1) `UniverseId` on `Settings` +
  `Species` with an EF query filter + a SHARED sentinel for operational keys + epoch-based cache
  invalidation; (2) `UniverseId` on `EntityEmbeddings`/`ProseEmbeddings` + filtered `FindSimilar*`;
  (3) the `IUniverseContext.WorldGroundingOr` prompt seam applied to every GLMZ-worded prompt site
  (GLMZ byte-identical; `EpisodeGeneratorService` stays GLMZ-only); (4) the derived-index caches
  (WorldGraph/Semantic/Thematic/Inference/GlobalSearch) rebuild on `UniverseScope.Epoch` change;
  (5) `Edge`/`EntityStateEvent`/`CharacterReadModel` scoped. Seed ids are UUIDv7 like the rest of the
  app. *(verified by `UniverseSegregationTests` (10 tests: query-filter scoping, insert-stamping,
  shared-key visibility, strand scoping, bootstrap, epoch, uuid-v7); CLI smokes `--canon-retrieve`
  (GLMZ 5 hits / Fantasy 0) + `--print-voice` (GLMZ 23.5KB / Fantasy 1.9KB engine-only); 147 gate
  tests green.)*
- **SS-US-U6 ✅** As the author, an entity that exists in two universes is a *duplicated* record (one
  row per universe), not a shared row — enforced by single-FK scoping + per-universe unique slug
  indexes (`UX_Entities_Universe_Type_Slug`, `UX_Strands_Universe_Slug`, `UX_Books_Universe_Slug`)
  so the same (type, slug) may recur across universes ([SS-LAW-15](BIBLE.md#SS-§5)).
- **SS-US-U7 ✅** As a user, the CyberSpace animated background is removed in favor of a plain,
  universe-neutral dark mode. *(verified by removal of the console-bg/sacred-geometry/tv-static JS +
  the cyberspace DOM divs across `App.razor`, `Home.razor`, `CategoryBoard.razor`, `MainLayout.razor`;
  full-solution build green; the dark `.app-shell` base theme unchanged.)*

## Epic H — GLMZ Books in progress

> Full KDP-paperback-length books set in the GLMZ universe. Bible-first → chapter-by-chapter
> workflow; each book gets a book-level strand + chapter sub-strands + beats. Prose written after
> all entities are seeded per [SS-LAW-1](BIBLE.md#SS-§5).

- **SS-US-H1 ⬜** As the author, *Underlying Connection* is written as a dual-POV GLMZ novel
  (~80k words, 3 acts, ~28 chapters, alternating Amara Osei / Seto Banda POV). See
  [SS-A6](AMENDMENTS.md) for CorpoNation + character canon. *Acceptance: book strand seeded +
  all chapters drafted + full-book review panel ≥85%.*
  - **H1a ✅** Entities seeded: Amara Osei (character), Seto Banda (character), Ciro Fonseca
    (character), Orison Neuretics (corponation). All four in DB before any prose is generated.
    *(CLI `--add-character` ×3 + `--add-corponation` ×1; seeded 2026-06-19.)*
  - **H1b ✅** Book-level strand + 28 chapter sub-strands created (kind=book + kind=chapter).
    *(slug underlying-connection-019ee11e; 28 chapter stubs parented; seeded 2026-06-19.)*
  - **H1c ⬜** Act 1 (~10 chapters, ~25k words) written to first-draft standard.
  - **H1d ⬜** Act 2 (~12 chapters, ~35k words) written to first-draft standard.
  - **H1e ⬜** Act 3 (~6 chapters, ~20k words) written to first-draft standard.
  - **H1f ⬜** Opus polish pass + full-book review panel ≥85%.

## Priority backlog

> Dependency-ordered toward the headline goal (a fresh seed → published, reviewed, canon-consistent
> audiobook+manuscript with the human only approving). From `ARCHITECTURE.md` §4 FUTURE table.

1. **SS-US-F1-prod ⬜** Ship present work to prod (run `drop_facet_system_*` +
   `create_voice_change_log_*` migrations; `--seed-voice-rules` + `--coverage --backfill` in prod).
   *Acceptance: prod schema has no facet remnants, has `VoiceChangeLog`, `--coverage` clean.* (was F1)
2. **SS-US-F6 🟡** Coverage → action: `ss --coverage --backfill` reembeds idempotently.
   *✅ 100% coverage (11,588/11,588; motif 0→100%). Residual: entity↔strand appearance tracking.*
3. **SS-US-F7 ⬜** In-app review surfaces: `/voice` (VoiceChangeLog approve/reject) + `/coverage`
   pages + `CANON-CONTRADICTION` filter in `/findings`.
4. **SS-US-Fh ⬜** Hierarchy + Collection builder: Series→Collection→Strand→Beat via `ParentStrandId`;
   drag-and-drop Collection builder on `/strands`; publishing a Collection stitches its strands.
5. **SS-US-F8 ⬜** Autonomous corpus loop: `ss --run-corpus --count N` runs
   generate→validate(--fix)→review→harvest across N seeds, resume-safe, pausing only for approvals.
6. **SS-US-Fs2 ⬜** Species as a first-class type: `Species` lookup entity + `/species` dictionary
   + `get_species` MCP tool; final set exactly five (`human`,`ai`,`elf`,`synthetic`,`unknown`).
7. **SS-US-G3 / Fv 🟡** Per-strand LLM voice / Kyle review pass across all strands.
8. **SS-US-G4 ⬜** Develop the 100-story outline past the spine (premises 9+).
9. **SS-US-Fc 🟡** In-app canon toggle on the writer/`/strands` page (columns + CLI already shipped).
10. **SS-US-F9 ⬜** Living world tick (scheduled `EntityStateEvents`, off by default).
11. **SS-US-F10 ⬜** Voice flywheel proof: batch K+1 mean `Strand.Score` > batch K after harvests.
12. **SS-US-U1…U7 ✅** Multi-Universe support (Epic U): `Universe` table + `UniverseId` + backfill to
    GLMZ → seed Fantasy/Steampunk → SwitchUniverse (per-process/per-session) in UI + CLI + MCP →
    full cross-over segregation (config + embeddings + prompts + caches + ledger) → plain dark mode.
    **Shipped 2026-06-15** (see [SS-A3](AMENDMENTS.md)/[SS-A4](AMENDMENTS.md) + [RFC 0006](rfc/0006-universe-segregation.md)). DB backed up first.

**Endpoint reached when** the prod-ship + F7 + F8 are green and F10 demonstrates the flywheel.

### Audit log

- **2026-06-15 — universe segregation SHIPPED ([RFC 0006](rfc/0006-universe-segregation.md); SS-A4).**
  Closed every cross-over surface beyond canon rows: config (`Settings`/`Species` scoped + SHARED
  sentinel + epoch cache invalidation), the silent embedding leak (`EntityEmbeddings`/`ProseEmbeddings`
  + filtered `FindSimilar*`), all GLMZ-worded prompt sites (the `WorldGroundingOr` seam; GLMZ
  byte-identical), the derived-index caches (rebuild on `UniverseScope.Epoch`), and the
  `Edge`/`EntityStateEvent`/`CharacterReadModel` ledger. Seed universe ids switched to UUIDv7 (the
  existing DB re-stamped via `restamp_universe_guid7_20260615.sql`). New `UniverseSegregationTests`
  (10) + 147 gate tests green. SS-US-U5 → ✅.
- **2026-06-15 — multi-universe SHIPPED (Epic U).** Implemented the full conversion: `Universe`
  table + `UniverseId` on Entities/Strands/Books (temporal dance), backfilled all rows to GLMZ,
  seeded GLMZ + Fantasy/Steampunk, per-universe unique slug indexes, an EF global query filter +
  insert-stamping via ambient `IUniverseContext`/`UniverseScope`, SwitchUniverse in UI (`NavMenu`
  dropdown) + CLI (`--universe` / `SS_UNIVERSE`) + MCP (`switch_universe`/`list_universes` tools),
  the `WorldPrimer` generation seam (GLMZ byte-identical), and removed the CyberSpace background for
  plain dark mode. Full solution builds clean; 129 gate tests pass; CLI smoke proves read scoping
  (GLMZ 94 strands / Fantasy 0). U5 (full prompt de-hardcoding) left 🟡. See [SS-A3](AMENDMENTS.md).
- **2026-06-15 — multi-universe (docs-first).** Added Epic U. Decisions locked with the author:
  (1) **no rename** — "StreetSamurai" stays the engine codename, "GLMZ" becomes the name of
  Universe #1; (2) **single `UniverseId` FK (1:M)**, not an M:M bridge — crossover entities are
  duplicated (author prefers ~10 dupe rows over a double-bridge refactor); (3) **SwitchUniverse is
  per-process / per-session** (flag → `SS_UNIVERSE` env → UI session → global default), so two CLIs
  can write different universes at once; (4) execution is **docs + backup only** this pass — DB
  backed up to `backups/StreetSamurai_preuniverse_20260615.bak` (RESTORE VERIFYONLY passed); schema
  and code build deferred to a reviewed follow-up. New engine invariant [SS-LAW-15](BIBLE.md#SS-§5);
  GLMZ-specific laws (SS-LAW-8, 10–14) re-scoped to the GLMZ universe in BIBLE §5/§9.
- **2026-06-07 — migration.** This file was synthesized from `ARCHITECTURE.md` §4 (PAST/PRESENT/
  FUTURE goal tables) and `story_state.md`. Statuses were carried verbatim from the source tables;
  none were promoted to ✅ without a named test or recorded CLI/scan evidence. Items the source
  marked 🟡/⬜ remain so here.
- **Original spec (audit log) — F6/SS-US-F6.** Source `ARCHITECTURE.md` F6: *"Coverage → action:
  `ss --coverage --backfill` runs idempotent `ReembedCorpusAsync` — 100% coverage (11,588/11,588;
  motif 0→100%). Residual: entity↔strand appearance tracking still to add."* Kept 🟡 (residual
  open).
- **Original spec (audit log) — Fs1.** Source `ARCHITECTURE.md` Fs1 (✅): *"One format: everything
  is a Strand of Beats — 24 chapter-strands + 132 episode beats migrated; 42 strands / 1,436 beats
  total. `AutonomousStory` retired as an artifact; residual: excise the `AutonomousStory` class
  from `StoryDirectorService`/UI internals."* Captured as SS-US-A4 (✅, format) with the class-excise
  residual noted here.
