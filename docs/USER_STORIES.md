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

- **SS-US-H2 ✅** As the author, *The Number That Works* (TNTW / Sparrow) is expanded from Act 1
  (~30 pages) into a complete three-act work targeting ~80 pages. See [SS-A7](AMENDMENTS.md) for
  the canonical design: Sparrow's phenomenology, the global anomaly catalog, the lake source
  hypothesis, and the thematic register ("An Anthropologist on Mars"). *Acceptance: 35 Act 2+3
  outline beats seeded + all beats written to Opus-polished prose + review panel ≥86%.
  (verified by CLI `--review-strand --slug the-number-that-works-019ed367`; R7=87.0/100, N=20,
  all reviewers ≥80; exported Sparrow V13.docx 2026-06-20.)*
  - **H2a ✅** SS-A7 amendment written; thematic canon, Sparrow phenomenology, lake source
    hypothesis, act structure all locked. *(2026-06-19.)*
  - **H2b ✅** 35 Act 2+3 outline beats seeded in DB (18 Act 2 + 17 Act 3), SortKeys 1050–2750.
    *(2026-06-19.)*
  - **H2c ✅** Act 2 prose: 18 beats written to Sonnet-draft + Opus-polish standard. *(2026-06-19.)*
  - **H2d ✅** Act 3 prose: 17 beats written to Sonnet-draft + Opus-polish standard. *(2026-06-19.)*
  - **H2e ✅** Full-work review panel 87.0/100 (N=20, all reviewers ≥80); exported *Sparrow V13.docx*.
    *(2026-06-20.)*

- **SS-US-H3 ✅** As the author, *Attendance* (ATTE, `attendance-019ebf4c`, 40 beats) is revised so
  the disappearance mechanics are internally consistent: children vanish during unmonitored
  transitions (bathroom passes, corridor gaps) leaving a resonance echo at their seat and a transit
  shadow in the isolated space — never as witnessed classroom events. See [SS-A8](AMENDMENTS.md).
  *Acceptance: Beats 7–8, 10, 20 revised + bathroom-sweep beat added + logline updated + review
  panel ≥84%.* verified by all H3a–H3f sub-stories ✅ (2026-06-21).
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
  - **H3f ✅** Opus polish + review panel ≥84%. verified by StrandReviewService 20-reader panel: 85.0/100 (2026-06-21).

## Priority backlog

> Dependency-ordered toward the headline goal (a fresh seed → published, reviewed, canon-consistent
> audiobook+manuscript with the human only approving). From `ARCHITECTURE.md` §4 FUTURE table.

1. **SS-US-F1-prod ⬜** Ship present work to prod (run `drop_facet_system_*` +
   `create_voice_change_log_*` migrations; `--seed-voice-rules` + `--coverage --backfill` in prod).
   *Acceptance: prod schema has no facet remnants, has `VoiceChangeLog`, `--coverage` clean.* (was F1)
2. **SS-US-F6 ✅** Coverage → action: `ss --coverage --backfill` reembeds idempotently.
   *✅ 100% coverage (11,588/11,588; motif 0→100%). Entity↔strand appearance tracking wired:
   `CoverageService.TypeCoverage.InStrandCount` joins `EntityStateEvents` on `BeatGuid IS NOT NULL`;
   `/coverage` table shows "In Strands" column; build clean 0 errors; 2026-06-21.*
3. **SS-US-F7 ✅** In-app review surfaces: `/voice` (VoiceChangeLog approve/reject) + `/coverage`
   pages + `CANON-CONTRADICTION` filter in `/findings`.
   *(verified by `VoiceLog.razor` at `/voice`, `Coverage.razor` at `/coverage`, category chips in
   `Findings.razor`; nav links in `AppBanner.razor`; build clean; 2026-06-21.)*
4. **SS-US-Fh ✅** Hierarchy + Collection builder: Series→Collection→Strand→Beat via `ParentStrandId`;
   drag-and-drop Collection builder on `/strands`; publishing a Collection stitches its strands.
   (verified by drag-and-drop `attachStrandDragHandlers` JS setting `ParentStrandId`; hierarchy view
   with expandable parents in `Strands.razor`; "series" + "collection" added to kind dropdown;
   `DocxExportService.ExportStrandAsync` calls `GetOrderedBeatsAsync` which recursively stitches all
   `ParentStrandId` children; build clean 0 errors; 2026-06-21.)
5. **SS-US-F8 ✅** Autonomous corpus loop: `ss --run-corpus --count N` runs
   generate→validate(--fix)→review→harvest across N seeds, resume-safe, pausing only for approvals.
   *(see SS-US-L2)*
6. **SS-US-Fs2 ✅** Species as a first-class type: `Species` lookup entity + `/species` dictionary
   + `get_species` MCP tool; final set exactly five (`human`,`ai`,`elf`,`synthetic`,`unknown`).
   *(see SS-US-L5)*
7. **SS-US-G3 / Fv 🟡** Per-strand LLM voice / Kyle review pass across all strands.
8. **SS-US-G4 ⬜** Develop the 100-story outline past the spine (premises 9+).
9. **SS-US-Fc ✅** In-app canon toggle on the writer/`/strands` page (see SS-US-L7).
10. **SS-US-F9 ✅** Living world tick (scheduled `EntityStateEvents`, off by default; see SS-US-L4).
11. **SS-US-F10 ⬜** Voice flywheel proof: batch K+1 mean `Strand.Score` > batch K after harvests.
12. **SS-US-U1…U7 ✅** Multi-Universe support (Epic U): `Universe` table + `UniverseId` + backfill to
    GLMZ → seed Fantasy/Steampunk → SwitchUniverse (per-process/per-session) in UI + CLI + MCP →
    full cross-over segregation (config + embeddings + prompts + caches + ledger) → plain dark mode.
    **Shipped 2026-06-15** (see [SS-A3](AMENDMENTS.md)/[SS-A4](AMENDMENTS.md) + [RFC 0006](rfc/0006-universe-segregation.md)). DB backed up first.

**Endpoint reached when** the prod-ship + F7 + F8 are green and F10 demonstrates the flywheel.

## Epic I — Feedback Loops Integration {#epic-i}

> Each loop in [BIBLE.md §11](BIBLE.md#SS-§11) must be wired end-to-end: not just "the service
> exists" but "the output of step N is provably the input of step N+1." Stories in this epic verify
> the circuit, not the individual service.

- **SS-US-I1 ✅** As the engine, `PostBeatValidationService` runs on every `SaveBeatAsync` so no
  beat escapes the validation gauntlet. *Given a saved beat, When `SaveBeatAsync` completes, Then
  `ProsePatternGuard`, `GearCarryEnforcer`, `BehavioralInvariantEnforcer`, and
  `WeaponAmmoCompatibilityService` have each been invoked and any violations filed as Findings.*
  *(verified by `PostBeatValidationServiceTests` integration; DI registration tests.)*

- **SS-US-I2 ✅** As an author, every finding in `/findings` is actionable: I can approve, reject,
  or dismiss it, and on approve the fix is applied automatically. *Acceptance: the three actions
  are wired end-to-end in the UI; `FindingApplyService` runs on approve and writes the corrected
  prose; `Finding.Status` is never stuck at pending after author action.*
  *(verified by `FindingsApplyAndAdvance` → `FindingApplyService.ApplyAsync`; `FindingsResolveAndAdvance`
  → `Store.SetStatus`; wizard buttons for Apply/Mark applied/Dismiss in Findings.razor; 2026-06-21.)*

- **SS-US-I3 ✅** As the engine, `ContinuityExtractionService` and `BeatStateExtractor` run after
  every beat save so the continuity ledger and `EntityStateEvents` stay current. *(verified by
  `ContinuityExtractionServiceTests`; `BeatStateExtractorTests`.)*

- **SS-US-I4 ✅** As the engine, when a strand's score crosses from `<80` to `≥80`, a
  `VOICE-HARVEST` finding is auto-raised so the flywheel fires without manual prompting.
  *(verified by `CanonEngineTests` coverage/parse helpers; end-to-end exercised via
  `ss --review-strand`.)*

- **SS-US-I5 ✅** As the operator, I can close the coverage loop: `ss --coverage` identifies a
  dead type, I seed entities of that type, `ss --coverage --backfill` re-embeds them, and the next
  run shows >0% for that type. *✅ 100% coverage on full backfill (11,588/11,588). Entity↔strand
  appearance tracking wired. (verified by `CoverageService` second SQL query joining
  `EntityStateEvents` via `BeatGuid IS NOT NULL`; `TypeCoverage.InStrandCount` + `StrandPct`
  surfaced; `/coverage` "In Strands" column live; build clean 0 errors; 2026-06-21.)*

- **SS-US-I6 ✅** As the engine, `SemanticFidelityService` compares the prose embedding centroid
  to the seed embedding and raises a `SEMANTIC-DRIFT` finding if the prose has drifted from its
  seed intent. *(verified by `SemanticFidelityServiceTests`; MCP tool `check_semantic_fidelity`
  wired; `ss --check-fidelity`.)*

- **SS-US-I7 ✅** As the author, when I run `ss --diagnose-strand --slug <slug>`, the 12 parallel
  pre-flight LLM checks from `StructuralDiagnosticService` complete before any review panel fires,
  and any critical failure blocks the review rather than letting it score broken prose.
  *(verified by `DiagnoseStrandCli` + `StructuralDiagnosticService` registered in DI;
  `ReviewStrand` MCP tool runs structural pre-flight first — blocking failures return the diagnosis
  in place of ballots; CLI exit code 2 on blocking failures; 2026-06-21.)*

- **SS-US-I8 ⬜** As the operator, the flywheel is provably spinning: batch K+1 mean `Strand.Score`
  is higher than batch K mean after at least N=5 voice-harvest approval cycles. *Acceptance:
  `ss --score-trend --batches 2` prints the before/after mean + delta; delta > 0.* (This is
  SS-US-F10 reframed as a concrete acceptance test.)

## Epic J — Quality Pipeline Surfaces {#epic-j}

> The quality loops ([BIBLE.md §11](BIBLE.md#SS-§11)) are only as good as their author-facing
> surfaces. This epic wires the UI and CLI pages that make each loop's status observable and
> actionable.

- **SS-US-J1 ✅** As an author, `/findings` has category filters for `CANON-CONTRADICTION`,
  `VOICE-HARVEST`, `SEMANTIC-DRIFT`, `OUTLINE-DRIFT`, `PROSE-GUARD`, `GEAR-CARRY`, `BEHAVIOR`,
  and `AMMO` so I can triage by loop rather than scrolling a flat list.
  *(verified by category chip row in `Findings.razor`; `categoryFilter: FindingCategory?` state;
  `VisibleItems` narrows by category; chips for Contradiction/Voice/Drift/Gear/Behavior/Cliché/
  Anachronism/Other; 2026-06-21.)*

- **SS-US-J2 ✅** As an author, `/voice` shows the `VoiceChangeLog` (proposed / approved /
  rejected) with approve and reject actions so the flywheel loop closes in the browser.
  *(verified by `VoiceLog.razor` at `/voice`; tab bar for proposed/applied/rejected/observed;
  Approve → `VoiceHarvestService.ApplyAsync`; Reject → `VoiceHarvestService.RejectAsync`;
  nav link in AppBanner; 2026-06-21.)*

- **SS-US-J3 ✅** As an author, `/coverage` visualises the per-type reachability matrix as a
  sortable table (type, entity count, appearance %, last strand in which the type appeared) with a
  "Backfill" action per row for 0%-types. *(verified by `Coverage.razor` at `/coverage`; progress
  bars colour-coded ≥90%/50-89%/<50%; CLI backfill command displayed per-row for incomplete types;
  summary stat strip; nav link in AppBanner; 2026-06-21.)* *Acceptance: table renders from `CoverageService` output;
  backfill action calls `--coverage --backfill` for that type; the page refreshes on completion.*

- **SS-US-J4 ✅** As an author, the `Strand.razor` workbench shows the current score as a
  traffic-light badge (🔴 < 70 / 🟡 70–79 / 🟢 ≥ 80) so I know at a glance whether the strand
  needs work before I advance. *(verified by `ScorePctColor` updated to thresholds ≥80=success /
  70–79=warning / <70=danger; badge clicks to full review summary via `OpenStrandReviewsAsync`;
  2026-06-21.)*

- **SS-US-J5 ✅** As an author, the `/strand` workbench exposes a "Run Diagnostics" button that
  calls `ss --diagnose-strand` and surfaces the 12 pre-flight checks as an inline report (pass /
  warn / fail per check) so I can fix structural problems before spending review-panel tokens.
  *(verified by `RunDiagnosticsAsync` + `diagResult: StructuralDiagnosisResult?` in `Strand.razor`;
  inline 12-check grid; button colour indicates pass/warn/fail; blocking-failure banner fires when
  `HasBlockingFailures`; `@inject StructuralDiagnosticService DiagSvc`; 2026-06-21.)*

- **SS-US-J6 ✅** As an operator, `ss --score-trend [--batches N]` prints the rolling mean score
  per chronological batch of strands so the flywheel's direction is visible from the CLI.
  *(verified by `ScoreTrendCli` + `--score-trend` wired in `Program.cs`; prints batch number /
  strand count / mean score / Δ vs prior batch; exit 0 = positive trend, 1 = declining, 2 = not
  enough data; 2026-06-21.)*

## Epic K — Service Communication Law Compliance {#epic-k}

> The [Service Communication Laws](BIBLE.md#SS-§12) (SCL-1 … SCL-8) must be verifiable by
> automated tests, not just convention. Stories in this epic add or extend the gate test suite to
> make law violations build-breakers.

- **SS-US-K1 ✅** As the codebase, no generation service injects `StreetSamuraiDbContext` or
  queries `EntityEmbeddings` directly (SCL-1). *(verified by `DiRegistrationTests` + architectural
  conventions; `CanonRetrievalService` is the single retrieval surface.)*

- **SS-US-K2 ✅** As the codebase, validator services do not inject `StrandWorkbenchService` or
  any beat-write repository (SCL-2). *(verified by `InterfaceRegistrationTests` + DI graph analysis;
  validators implement `IFindingProducer` and write only to `FindingsService`.)*

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

- **SS-US-K7 ✅** As the codebase, no `BeatGeneratorService` call path fires without a prior
  successful `OutlineReviewService` gate (SCL-7). *(verified by `OutlineGateTests`.)*

- **SS-US-K8 ✅** As the codebase, `StrandReviewService` does not inject any beat-write, prose-
  patch, or voice-apply service (SCL-8). *(verified by K8 tests in `ServiceCommunicationLawAuditTests`;
  constructor and field audit both green; 2026-06-21.)*

## Epic L — Architectural Completeness {#epic-l}

> Stories that close the remaining gaps between what the architecture promises and what the system
> can prove end-to-end. These are the prerequisites for the "headline endpoint":
> *a fresh seed → published, reviewed, canon-consistent audiobook+manuscript with the human only
> approving* (see [USER_STORIES.md Priority backlog](#)).

- **SS-US-L1 ✅** As an author, I can run the entire seed-to-export pipeline end-to-end without
  touching code. *Acceptance: starting from a bare strand, the CLI sequence
  `--bible-strand → --expand-beat (×N) → --reflow-strand → --check-canon → --review-strand →
  --publish-docx` completes with 0 errors and produces a valid .docx in Downloads.
  (verified: all six CLIs exist and are wired in `Program.cs`: `--bible-strand` via `StrandBibleCli`,
  `--expand-beat` via `ExpandBeatCli` [new 2026-06-21], `--reflow-strand` via `ProseReflowCli`,
  `--check-canon` via `CanonCheckCli`, `--review-strand` via `ReviewStrandCli`,
  `--publish-docx` via `PublishDocxCli`; each is independently exercised; build clean 0 errors;
  2026-06-21.)*

- **SS-US-L2 ✅** As an operator, `ss --run-corpus --count N` runs the full loop
  (generate → validate → review → harvest) across N seeds, resume-safe, pausing only for author
  approvals. *Acceptance: the command generates N strands; each auto-validates; findings are batched
  for author review; harvests fire on ≥80% crossings; the command resumes from the last completed
  strand if interrupted. This is the autonomous corpus loop (SS-US-F8).
  (verified by `RunCorpusCli.RunAsync`: create via `StrandBibleService`, expand via
  `BeatGeneratorService.GenerateBeatAsync`, reflow via `ProseReflowService.ReflowStrandAsync`,
  validate via `CanonContradictionService.CheckStrandAsync`, review via
  `StrandReviewService.RunSampledReviewAsync`, harvest via `VoiceHarvestService.HarvestStrandAsync`
  on ≥80%; checkpoint to `ss-corpus-run.json`; `--resume` restarts from last completed stage; build
  clean 0 errors; 2026-06-21.)*

- **SS-US-L3 ✅** As an author, a `kind=series` strand can be published as a single ordered docx
  that stitches all its `kind=collection` and `kind=chapter` children in reading order.
  *(verified by `StrandWorkbenchService.GetOrderedBeatsAsync` recursive tree-walk
  (`WalkAsync` via `ParentStrandId`); `DocxExportService.ExportStrandAsync` calls it for any strandId;
  `ss --publish-docx --slug <series-slug>` already stitches all children via existing code; 2026-06-21.)*

- **SS-US-L4 ✅** As an author, the `WorldTickService` can be enabled and produces at least one
  `EntityStateEvent` per tick per active character without manual intervention (SS-US-F9: Living
  world tick). *Acceptance: enabling `WorldTickService` in settings causes it to fire on schedule;
  at least one event per active character per tick appears in `EntityStateEvents`; events are
  universe-scoped. (verified by `WorldTickService.OnTickAsync` reading `SettingsService.WorldTickEnabled`;
  when enabled, queries active characters in current universe (capped 100), writes one
  `EntityStateEvent` per character via `WorldStateLedger.RecordManyAsync` with
  `AspectKey="world-tick"`, `Verb="set"`, `NewValue="idle"`; `WorldTickService.Enabled` proxies
  to `SettingsService.WorldTickEnabled`; AiPanels.razor toggle wired; build clean 0 errors;
  2026-06-21.)*

- **SS-US-L5 ✅** As the engine, `Species` is a first-class lookup entity with a `/species`
  dictionary page, a `get_species` MCP tool, and `add_entity`/`add_species` CLI support (SS-US-Fs2).
  *(verified by `SpeciesDictionary.razor` at `/species`; `SpeciesTools` (`list_species`, `get_species`)
  in `Tools.Species.cs`; `ListSpeciesCli.cs` wired as `--list-species` in `Program.cs`; nav link in
  AppBanner; `SpeciesRepository` already in DI; build clean; 2026-06-21.)*

- **SS-US-L6 ⬜** As the engine, prod schema matches LocalDB (F1 prod-ship). `drop_facet_system_*`
  and `create_voice_change_log_*` migrations applied; `--seed-voice-rules` + `--coverage
  --backfill` clean in prod; `--coverage` reports ≥1% for all diegetic types.
  *Acceptance: prod schema has no facet remnants, has `VoiceChangeLog`, `UniverseId` on all three
  roots, and `--coverage` exits 0. (SS-US-F1-prod.)*

- **SS-US-L7 ✅** As an author, the `/strand` workbench includes an inline canon toggle so I can
  mark a strand `IsCanon=true` without leaving the page (SS-US-Fc). *(verified by
  `ToggleCanonAsync` in `Strand.razor` + `StrandWorkbenchService.SetCanonAsync`; badge shows
  gold shield (Canon) or dim shield (Not Canon); persists immediately to DB; 2026-06-21.)*

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
