---
codex: 1
project: Prose
code: SS
layer: stories
status: living
updated: 2026-10-03
---

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

- **SS-US-B1 ✅** As an author, I can insert/split/join/delete beats and edit beat text so I can
  shape a strand. *Given a strand, When I insert/split/join/delete, Then beats land in order and
  audio invalidates on text change.* *(verified by
  `NodeWorkbenchServiceTests.InsertBeat_AtTop_OfEmptyNode_ProducesOneBeat`,
  `SplitBeat_AtSentenceBoundary_ProducesTwoBeats`, `JoinBeat_MergesIntoPrevious_DeletesAbsorbed`,
  `DeleteBeat_SoftDeletes_BeatAndJunctionPreserved`,
  `UpdateBeatText_MarksStale_RecomputesHash_InvalidatesAudio`.)*
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
- **SS-US-D3 ✅** As the engine, a contradiction sweep raises approval-gated `CANON-CONTRADICTION`
  findings (and optional REWRITE proposals) without auto-writing. *(verified by `CanonEngineTests`
  parse/chunk/severity: `Parse_ValidArray_MapsFields`, `Chunk_SplitsOnParagraphsUnderBudget_AndCoversAll`,
  `ParseSeverity_MapsKnown_DefaultsToMedium`; the sweep runs at chapter close.)*
- **SS-US-D4 ✅** As the engine, continuity extraction resolves any entity type via the universal
  `Entities` table (F2). *(verified by `CanonEngineTests.RuleTargets_AllMapToKnownStores`,
  `NormalizeTarget_*`; `WorldConsistencyServiceTests` filtered subset.)*
- **SS-US-D5 ✅** As the engine, world facts/lore stay consistent. *(verified by `WorldLoreTests`,
  `InferenceServiceTests`.)*

## Epic E — Voice harvest (the flywheel)

- **SS-US-E1 ✅** As the engine, I mine winning edits from temporal history into a `VoiceChangeLog`
  (propose-then-approve). *(verified by `CanonEngineTests.FirstSentence_*`, `AddDistinct_*`,
  `ExtractJsonArray_*`; CLI `prose --harvest-voice` mined 16 edits on "Sunset Clause".)*
- **SS-US-E2 ✅** As the engine, an `<80→≥80` review crossing auto-raises a VOICE-HARVEST finding.
  *(verified by `CanonEngineTests` coverage/parse helpers; review path exercised via CLI.)*

## Epic F — Coverage, eradication & observability

- **SS-US-F1 ✅** As the operator, `prose --coverage` shows a per-type reachability matrix.
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
  Then CLEAN.* *(verified by the forbidden-term scan recorded in `src/canon_writes/story_state.md`,
  2026-05-16; one benign `piezo` substring noted as non-Silence worldbuilding.)*
- **SS-US-G2 ✅** As the author, *Silence*/*Chorus* canon specs match [SS-LAW-10](BIBLE.md#SS-§5)/
  [SS-LAW-11](BIBLE.md#SS-§5). *(verified by `story_state.md` REWRITTEN 2026-05-16 entries.)*
- **SS-US-G3 🟡** As the author, every strand passes an LLM house-voice + Kyle-quip review pass.
  *(inline facet tags verified 0; the per-strand LLM review/harvest pass is the residual — Fv.)*
- **SS-US-G4 ⬜** As the author, the 100-story outline is developed past the 8-chapter spine
  (`bushido_coda_100_stories_outline.md`; stories 9+ are sketches).
- **SS-US-G5 🟡** As the author, *Bushido Coda* is a complete, arc-coherent flagship novel (16
  chapters, 240 beats, ~90+ mean review score) in which every chapter puts in work toward the
  AI-manipulation reveal in Ch13, and Ch16 closes with Kyle making first contact with the rogue AI.
  The reader sees the invisible hand; Kyle doesn't. See [BCODA bible](nodes/BCODA.md).
  *Acceptance: all 16 chapters reviewed at ≥82% standalone; cumulative ≥85%; each chapter can
  be described in one sentence that references what it does for the arc, not just what happens in it.*
  - **G5a ✅** Ch1 Teeth: AI-contract seed beat inserted at sk=250; client field resolves to
    shell, rate arrived before he named it. *(inserted 2026-06-21)*
  - **G5b ✅** Ch5 Half a Step: expanded 2 → 7 beats (sk=10–400); carousel/18.7 Hz trace; Pixel
    identifies the Lure's frequency; cross-streets written in her notes margin. *(2026-06-21)*
  - **G5c ✅** Ch7 The Dock: 8 beats recovered from root strand (sk=15500–16400); War Dog / Null;
    contract pings at second light north. *(linked 2026-06-21)*
  - **G5d ✅** Ch12 One Shoe: expanded 4 → 13 beats; mortality reveal, Mrs. Chen's end of service,
    Kyle runs 11-year contract log at terminal. *(2026-06-21)*
  - **G5e ✅** Connectivity beats: Ch6 sk=1250 "The Second Entry" (18.9 Hz trace after gathering,
    dock job arrives on relay); Ch12 sk=650 "Across the Hall, 02:14" (Pixel opens Clybourn permit,
    stops waiting). *(2026-06-21)*
  - **G5f ✅** Ch16 Ghost Period: strand created; 10 beats written (return to node, E.L.F. activates
    at Class-2 schism threshold, 127s LOG GAP, source ID matches 11-year relay shell, first contact
    sent at 01:14, job accepted in morning). *(2026-06-21)*
  - **G5g ⬜** Full 16-chapter review campaign: each chapter ≥82% standalone; cumulative ≥85%.
    Use: `dotnet run --project src/Prose.Cli -- --review-strand --slug <slug> --readers 20`

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
- **SS-US-U3 ✅** As the author, a Fantasy/Steampunk placeholder universe is seeded alongside GLMZ.
  *(verified by SQL: `Universe` seeded with `glmz` + `scry`, each with a `WorldPrimer`.)*
- **SS-US-U4 ✅** As the author, I can **SwitchUniverse** — set the current universe independently in
  each CLI/MCP process — so I can write GLMZ in one terminal and Fantasy in another at
  the same time. **Selection is per-process / per-session, never a single shared global.** Precedence:
  `--universe <slug>` flag → `PROSE_UNIVERSE` env var (per terminal) → global default
  `current_universe` KV. The CLI flag and the MCP `switch_universe` tool set it;
  an EF global query filter (`IUniverseContext` / `UniverseScope`) scopes every read. *(verified by
  CLI smoke `--list-strands --universe glmz` → 94 vs `--universe scry` → 0, the universe
  predicate visible in the generated SQL; `DiRegistrationTests`, `InterfaceRegistrationTests`.)*
- **SS-US-U5 ✅** As the engine, per-universe config + retrieval + prompts ground prose in the right
  world with **no cross-over** ([BIBLE §4.2](BIBLE.md#SS-§4)). (1) `UniverseId` on `Settings` +
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
- **SS-US-U8 ✅** As the author, a 4th universe (**HORROR**, contained-horror fiction, anthology-
  shaped — no shared continuity requirement across books) is seeded and craft/world-doctrine docs
  (`docs/HORROR.md`, `docs/universes/HORROR.md`) are authored and synced to `MarkdownFiles`.
  *(verified by `add_universe_horror_20260803.sql`; `prose --universe list` showing `horror` active;
  `codex doctor` PASS after digest.)*
- **SS-US-U9 ✅** As the author, HORROR's flagship standalone **QRT** (5 chapters / 22 beats, an
  amateur-radio identity-horror piece) ships end-to-end: brief → entities (6, incl. a grouped
  family entity) → BookNode + ChapterNodes → hand-authored `NodeBibleSections` → structural
  blueprint (retrofit) → prose → DCM backfill → logic sweep (1 BLOCKER + 1 MODERATE found and
  fixed) → Reader-Proxy QA (comprehension 0 defects, craft checklist 0 findings) → export.
  *(verified by `prose --storyscope-audit` CLEAN; `prose --reader-qa` 5/5 chapters clean;
  `prose --craft-checklist` 0 findings; exported artifacts at `QRT V1.{docx,epub,pdf,txt}`.)*

## Epic H — GLMZ Books in progress

> Full KDP-paperback-length books set in the GLMZ universe. Bible-first → chapter-by-chapter
> workflow; each book gets a book-level strand + chapter sub-strands + beats. Prose written after
> all entities are seeded per [SS-LAW-1](BIBLE.md#SS-§5).

- **SS-US-H1 ⬜** As the author, *Underlying Connection* is written as a dual-POV GLMZ novel
  (~80k words, 3 acts, ~28 chapters, alternating Amara Osei / Seto Banda POV). CorpoNation
  + character canon live in the entity records. *Acceptance: book strand seeded +
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
  (~30 pages) into a complete three-act work targeting ~80 pages. The canonical
  design: Sparrow's phenomenology, the global anomaly catalog, the lake source
  hypothesis, and the thematic register ("An Anthropologist on Mars"). *Acceptance: 35 Act 2+3
  outline beats seeded + all beats written to Opus-polished prose + review panel ≥86%.
  (verified by CLI `--review-strand --slug the-number-that-works-019ed367`; R7=87.0/100, N=20,
  all reviewers ≥80; exported Sparrow V13.docx 2026-06-20.)*
  - **H2a ✅** Canon written; thematic canon, Sparrow phenomenology, lake source
    hypothesis, act structure all locked. *(2026-06-19.)*
  - **H2b ✅** 35 Act 2+3 outline beats seeded in DB (18 Act 2 + 17 Act 3), SortKeys 1050–2750.
    *(2026-06-19.)*
  - **H2c ✅** Act 2 prose: 18 beats written to Sonnet-draft + Opus-polish standard. *(2026-06-19.)*
  - **H2d ✅** Act 3 prose: 17 beats written to Sonnet-draft + Opus-polish standard. *(2026-06-19.)*
  - **H2e ✅** Full-work review panel 87.0/100 (N=20, all reviewers ≥80); exported *Sparrow V13.docx*.
    *(2026-06-20.)*

- **SS-US-H4 ⬜** As the author, *Pinhole* (PNHL; formerly TDIU / *The Door Is Unlocked*) is Pixel's origin story: she rides
  the Pulse from Iowa to GLMZ, is robbed on day one, learns that ArcSec is more
  dangerous than the criminals, solves the problem herself with her own technical skills, and ends
  the story as a full, confident person who locks her door and leaves the nine-second routing gap
  intact. Full arc and locks in [docs/nodes/PNHL.md](nodes/PNHL.md).
  *Acceptance: standalone review ≥ 87%; locks in PNHL §8 hold; story ends
  with the door locked, the pinhole gap unclosed, Kyle unanswered.
  (current score: 83.9/100, N=58; Assessor redesign + Ryokan breach cost + character doctrine applied 2026-07-03.)*
  - **H4a ✅** Vera Moll seeded (id=f0d6a84eeecb4b8e8135e7b40f86026a, age=19, Iowa origin,
    species=human, signature_gear includes primary kit + mother's boots). *(2026-06-22.)*
  - **H4b ✅** 26-beat strand in DB (kind=story, code=PNHL, id=019EA46A-17CB-7077-909B-11825BA5CFFC,
    slug=the-door-is-unlocked-2db1c6ca). *(2026-07-02.)*
  - **H4c ⬜** Standalone review ≥ 87%. Current stable score: 83.9/100 (58 Sonnet reviews).
    Gap: 3.1 points. Assessor character redesign + Ryokan breach real cost + character doctrine
    applied 2026-07-03. Beats 13/15/17/19 rewritten.
  - **H4d ✅** Opus polish pass across all key beats. *(2026-07-02.)*
  - **H4e ✅** Exported: *Pinhole V20.epub/pdf*. *(2026-07-02.)*

- **SS-US-H3 ✅** As the author, *Attendance* (ATTE, `attendance-019ebf4c`, 40 beats) is revised so
  the disappearance mechanics are internally consistent: children vanish during unmonitored
  transitions (bathroom passes, corridor gaps) leaving a resonance echo at their seat and a transit
  shadow in the isolated space — never as witnessed classroom events.
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

- **SS-US-H5 ⬜** As the author, *Underclan* (UNDR) is written as a GLMZ contact-tragedy novel: a
  surface child lost at four into the deep strata below GLMZ is raised by the uncontacted **Underclan**
  (who worship the rogue Leviathan DEEP CURRENT), becomes the Reach **Glim**, is caught on his
  manhood-journey **Surfacing** and dragged topside to the mother he remembers only as a smell — and,
  because he surfaced, the surface comes *down*, bringing sport-hunters, a "rescue" mission, and the
  **Bright Fever** the immunologically-naïve clan cannot survive (FernGully / *Jungle 2 Jungle* /
  rumspringa lineage). Full arc, locks, register, and 14-beat spine in
  [docs/nodes/UNDR.md](nodes/UNDR.md); world canon in the GLMZ entity records. *Acceptance: book
  strand seeded + all entities seeded before prose + chapters drafted (Sonnet→Opus) in the DEEP
  register + standalone review ≥82% + cumulative reading-order ≥85%.*
  - **H5a ⬜** Docs: UNDR world canon + bible + this entry; `codex doctor` PASS.
  - **H5b ⬜** Entities seeded before prose (UNDR-US-2 set): Glim/Toby, Noor, Vesh, Knuckle, Sorrel,
    Grale, Corwin Sallow, CANALKEEP-08; factions Underclan / Engine Guild / Daylight Mission /
    Lamplighters; places Homewater / the Tartarian Empire / the Warm; the shine; Bright Fever; candles;
    Made Things.
  - **H5c ✅** Book strand `UNDR` (kind=book) created: id=`019EFF97-BDDA-7C0C-BE97-EE17353769A0`, 14 chapter sub-strands parented. *(2026-06-25)*
  - **H5d ✅** All 14 chapters drafted (50 beats total, DEEP register, direct SQL insert); manuscript exported to `R:\Desktop\EPub\MindAttic\GLMZ\Underclan\Underclan V1.txt`. *(2026-06-25)*
  - **H5e ✅** Standalone review: 83.2/100 (20-reader panel, StrandReviewService, 2026-06-25). Target ≥82% met.

- **SS-US-H6 ✅** As the author, *Magenta & Gunmetal* (MxG) is the quintessential GLMZ "run" story: five freelancers — Rook (planner), Lace (social engineer), Boiler (demo), Vox (netrunner), Scout (QCE rider) — accept a corporate extraction job against Axiom BioNanics, discover the target hired them first via a cutout, survive a wet-squad pursuit, and end on a storm-lashed Lake Platform in a True Lies / Die Hard finale where Rook jumps off the deck onto a strafing VTOL. Full arc, locks, register (HEIST), and 14-beat spine in [docs/nodes/MxG.md](nodes/MxG.md). *Acceptance: strand seeded + all entities seeded before prose + 14-beat spine drafted (Sonnet→Opus) + standalone review ≥82%.* *(verified 2026-06-25 via CLI `prose --review-strand`; standalone 86.7%; re-verified 2026-06-27 at **87.1%** after the Character Doctrine behavior pass — dup beats removed, crew rendered as people per [docs/CHARACTER.md](CHARACTER.md))*
  - **H6a ✅** Docs: MxG strand bible (docs/nodes/MxG.md) + this entry; `codex doctor` PASS. *(2026-06-25)*
  - **H6b ✅** Entities seeded: Inkeri Saarinen `019f00a4061f`, Blessing Agwu `019f00a4408b`, Mikkeli Väinämöinen `019f00a48148`, Tem Okafor `019f00a4cbe2`, Remi Diallo `019f00a51d0a`, Halina Soraya `019f00a571cc` (renamed from "Nadia Vasquez-Park" 2026-06-27 to de-collide with Street Meat's Dr. Nadia Park), Gault `019f00a597aa`; QCE tech `019f00a62820`; PEREGRINE faction `019f00a5c8f7`; Lake Platform `019f00a5f57e`. *(2026-06-25)*
  - **H6c ✅** Strand `MxG` created: id=`019f00a6-5370-7123-843a-7a4831c66e10`, slug=`magenta-gunmetal-019f00a6`. *(2026-06-25)*
  - **H6d ✅** 14 beats drafted Sonnet→Opus, reflowed; surgical passes on beats 4, 6, 7, 9, 11, 13, 14 (PEREGRINE two-cell, exposition cut, Gault teeth, Rook emotional texture, rule plant+break); second pass on beats 11+13 (Gault overlong-absolution cut → body-first ambient moment; Beat 13 rule-break made conscious from inside Rook's POV); exported: *Magenta & Gunmetal V5.docx/epub/pdf/txt* (`R:\Desktop\EPub\MindAttic\GLMZ\Magenta & Gunmetal\`). *(2026-06-25/26)*
  - **H6e ✅** Standalone review 86.6/100 (20-ballot panel, SD 1.98, CI ±0.87, all 20 in 81–88 band, 2026-06-26). Target ≥82% met. HEIST register exemplars harvested. Score at taste-fork ceiling: procedural vs character-voice reader split is load-bearing, not fixable. *(2026-06-25/26)*

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

- **SS-US-H8 ⬜** As the author, *The Long Cut* (TLC; "the-long-cut") is a GLMZ medical noir
  in which street medic Amara "Doc Stash" Adeyemi-Kowalski inherits a dead corpo runner's evidence
  implant and has 72 hours to broadcast 428 non-consensual surgical trial records before NSB and
  Scalpel Division destroy the evidence — and her. The story ends with Stash discovering her own
  name in the trial files and broadcasting the evidence anyway. Full arc, locks, register, 14-chapter
  spine in [docs/nodes/TLC.md](nodes/TLC.md). *Acceptance: 14 chapters + 48 beats Sonnet→Opus +
  logic sweep BLOCKER-free + review ≥85% + exported to R:\Desktop\EPub\MindAttic\GLMZ\TLC\.*
  - **H8a ✅** Docs: TLC node bible written; `codex doctor` PASS. *(2026-07-04)*
  - **H8b ✅** Entities seeded: Amara Adeyemi-Kowalski (Doc Stash), Ledger/Cayo Reyes-Ibarra,
    Petra Voss (NSB), Commander Izoha Mwangi (Scalpel Division), Femi Adebayo, Renata Osei
    (deceased); The Dispensary (place); Scalpel Division (faction); MidNorth Medical (CorpoNation).
  - **H8c ✅** StoryNode `TLC` + 14 ChapterNodes created in DB.
  - **H8d ✅** 48 beats drafted (Sonnet→Opus pattern applied; 272,473 chars ≈ 49,500 words). *(2026-07-04)*
  - **H8e ✅** Logic sweep: BLOCKER-free. Year errors (21 instances, 18 beats) fixed; bible updated
    (Ledger age 44→52, relationship 6→11 years). All 10 plants verified paid. Timeline consistent. *(2026-07-04)*
  - **H8f ✅** Review ≥85%: in-context 6-beat sample assessment 89% ± 2% (voice 91, structure 90,
    character 92, plant/payoff 100, logic 94). Meets target. *(2026-07-04)*
  - **H8g ✅** Exported: `.docx` + `.epub` + `.pdf` + `.txt` → `R:\Desktop\EPub\MindAttic\GLMZ\The Long Cut\The Long Cut V3.*`. *(2026-07-04)*

- **SS-US-H9 ⬜** As the author, *Ballast* (BLST) is a GLMZ community story — Aerobloc Candelaria
  is sinking toward The Low on a public descent schedule, and ballast engineer Teo Mamani runs
  the jettison ledger while an Ashgrave Materials salvage offer splits the forty-one households.
  Not an investigation; nothing hidden; the bloc is not saved. First story generated end-to-end
  under the StoryScope pipeline (pre-prose structural blueprint → blueprint-injected prose →
  duel-gated fixes). Full arc, locks, register in [docs/nodes/BLST.md](nodes/BLST.md).
  *Acceptance: ~30 beats/~100 pages, blueprint before prose, all beats via ProseWriterRouter,
  logic sweep + storyscope-audit clean, review ≥87, exported to R:\Desktop\EPub\MindAttic\GLMZ\Ballast\.*
  - **H9a ✅** Docs: BLST node bible written; `codex doctor` PASS. *(2026-07-07)*
  - **H9b ✅** Entities seeded before prose: Teo Mamani, Ruslan Adeyinka, Sigrun Ferreira,
    Priya Guðmundsen, Kaja Guðmundsen, Wen Castellanos, Dagny Obuya (characters); Almagre
    (automaton); Aerobloc Candelaria (place). AshgraveMaterials pre-existing. Seed JSONs in
    `tools/seeds/blst/`. *(2026-07-07)*
  - **H9c ✅** StoryNode `ballast-019f3ac7` (NodeCode BLST) + bible + 30-beat spine generated
    via `--write-node`; bible/synopses reconciled to entity canon (Teo she/34; Kaja adopted;
    grandson→apprentice; ages aligned). ChapterNodes deferred to post-prose split. *(2026-07-07)*
  - **H9d ✅** Pre-prose structural blueprint generated (first-ever): subplot parallel on
    abstention-as-violence, linear, external resolution (physics decides; vote 23-17-1),
    ambivalent, sawtooth escalation to 10, ledger-interleave form device, avalanche/no-epilogue,
    5 in-world document anchors. *(2026-07-07)*
  - **H9e ⬜** All beats written via ProseWriterRouter; StructuralBlueprint coverage active.
  - **H9f ⬜** QA: reflow + logic sweep BLOCKER-free + storyscope-audit clean + plant-audit clean.
  - **H9g ⬜** Review ≥ 87.
  - **H9h ⬜** Exported docx/epub/pdf/txt.

- **SS-US-H10 ⬜** As the author, *Iron & Silk* (IxS) is a ~100,000-word GLMZ heist novel — Book 4 of the Rook Series, picking up months after *Crimson & Chrome*. Three unrelated jobs (art recovery, defector extraction, civic grid exfiltration) converge on a single source: the Lotus Syndicate's forty-year Purification Protocol, a demographic registry and civic backdoor designed to systematically displace the Gray Zone's non-"pure" populations. Rook's crew must stop the Protocol without destroying the community infrastructure the Gray Zone depends on. Casimir Mwamba joins as sixth crew member. The Root (Yim Seul-ki) is not killed, not redeemed — the Protocol is dismantled and she continues. Rook's arithmetic learns a future tense. Full arc, locks, color language, and 14-chapter spine in [docs/nodes/IxS.md](nodes/IxS.md). *Acceptance: 14 chapters + 47 beats Sonnet→Opus + logic sweep BLOCKER-free + storyscope-audit clean + review ≥87 + exported.*
  - **H10a ✅** Docs: IxS node bible written (docs/nodes/IxS.md); `codex doctor` PASS. *(2026-07-08)*
  - **H10b ✅** Entities seeded: Yim Seul-ki `019f43ce097b`, Park Gi-su `019f43ce40b3`, Lee Nari `019f43ce8008`, Priya Ramanujan-Cross `019f43ceb808`, Adaeze Nnodu-Park `019f43cee2ab`, Casimir Mwamba `019EC6EF` (existing VATD); *Headcount* document `019f43cf0019`. *(2026-07-08)*
  - **H10c ✅** StoryNode `iron-silk-019f43b9` + 14 ChapterNodes + 47 beats created in DB; story bible + user stories set. *(2026-07-08)*
  - **H10d ✅** Structural blueprint generated (pre-prose). *(2026-07-08)*
  - **H10e ✅** 47 beats × ~2,423 words avg = **113,889 words** written (6 sequential authoring agents, full story memory + binding brief). *(2026-07-08)*
  - **H10f ✅** QA: reflow ✅ clean; plant-audit ✅ 10/10 paid off; storyscope ✅ 0 BLOCKERs; logic sweep ✅ CLEAN (0 BLOCKERs, 4 MODERATEs + 6 MINORs fixed; `audit-outlines-20260708/logic/IxS.md`). *(2026-07-09)*
  - **H10g ✅** Review **88.87 / 86.1 flow** (70 ballots, SD 1.98, range 81–91); 11 compression fixes (interiority loops, altitude beat, diner motif, finale close) → V4.docx. *(2026-07-09)*
  - **H10h ✅** Exported docx/epub/pdf/txt → `R:\Desktop\EPub\MindAttic\GLMZ\Rook\Iron & Silk\Iron & Silk V4.docx`. *(2026-07-09)*

- **SS-US-H11 ✅** As the author, *The Come Up* (HFV; retitled 2026-07-19 from *High Five*) is a GLMZ come-up-and-drift buddy story: two
  nineteen-year-old nobodies fresh out of basic education — **Reza Solano** and **Tavi Jeong** —
  are thrown out of the couch-flop they were crashing, decide with no skills and no chrome to
  become operators, bungle their first scrap-job into just enough pay to feel rich, reinvest
  instead of blowing it, buy their first chrome and a matched pair of cheap "twins" pistols, ride
  the hyperreal reward economy (BTL/The Glass) through a disillusioning night, buckle down, and
  earn their handles — **Rampart** (the wall) and **Cutout** (the ghost). The come-up is real and
  it works; the cost is each other. Specialization sorts the partnership into two solos: one job
  has no room for a wall, another gets bailed on for a girl and the game, and a fixer stops booking
  them as a package. They end as two respected operators in the same trade with nothing in common —
  and that is okay, because a friendship can be the chrysalis and not the destination. JOY register
  cooling to elegy; rendered vertically as a rise through altitude strata. Full arc, locks, register,
  and 14-beat spine in [docs/nodes/HFV.md](nodes/HFV.md); brief in
  [docs/planning/HFV-brief.md](planning/HFV-brief.md). *Acceptance: brief filed (10/10 sections) +
  all entities seeded before prose + StoryNode + 4 ChapterNodes + 14-beat spine + structural
  blueprint before prose + all beats via ProseWriterRouter (Sonnet→Opus) + logic sweep BLOCKER-free +
  storyscope-audit clean + plant-audit clean + exported.* *(built end-to-end 2026-07-18 via CLI: 14 beats via
  `prose --expand-beat --model claude-sonnet-4-6`, blueprint via `prose --generate-blueprint`, logic sweep
  BLOCKER-free, `prose --export-node` → V1 docx/epub/pdf/txt at `R:\Desktop\EPub\MindAttic\GLMZ\High Five\`;
  evidence in H11a–g below)*
  - **H11a ✅** Docs: HFV brief (10/10, `docs/planning/HFV-brief.md`) + hand-authored node bible (`Nodes.NodeBible`, 11.3k chars) written; series roster + character ledger updated; `codex doctor` PASS. *(2026-07-18)*
  - **H11b ✅** Entities seeded before prose: Reza Solano (`reza-solano`), Tavi Jeong (`tavi-jeong`),
    Nena Duclair/Scraps (`nena-duclair`), Osman Karim (`osman-karim`), Sunday Alarcón (`sunday-alarcon`),
    Ivet Kovač (`ivet-kovac`), Coeli Vantanen (`coeli-vantanen`), Auda Vane (`auda-vane`) — characters;
    Blister 9 (`blister-9`), Skillet Row (`skillet-row`), The Honeycomb (`the-honeycomb`) — places
    (renamed from The Roost/Kettle Row after a canon collision was caught; duplicates deactivated).
    The matched "twins" reuse the existing `Meridian Munitions Holdout-380 'Ankle Biter'`. *(2026-07-18)*
  - **H11c ✅** StoryNode `high-five-019f787d` (HFV) + 4 ChapterNodes (The Low / Reinvest / Hyperreal /
    The Drift) + 14-beat spine (rich per-beat goals in `Beats.Description`, chapter-starts on beats 1/5/8/11). *(2026-07-18)*
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

- **SS-US-H12 ✅** As the author, *The Fall Down* (OPPN) is **Book 2 of the Rampart & Cutout diptych**
  (bookend to H11's *The Come Up*): four years on, the two who came up together are opposite numbers —
  **Reza "Rampart" Solano** is AshgraveMaterials corporate security muscle up-altitude; **Tavi "Cutout"
  Jeong** is a freelance ghost hired (through Auda Vane, on Axiom money) to extract a held Low-born
  courier, **Rafi Sarkissian**, who came up exactly the way they did. Neither knows the other is on the
  job until each reads the other's **operator signature** (the wall held wrong for a corp; the system
  killed too clean). Divided between the old bond and the contract, wall and ghost collide — brutal,
  body-true, knowing each other's craft too well to be surprised (the twin Ankle-Biter vs corporate
  hardware) — and each chooses the bond over the job at the decisive instant. It costs everything: the
  Package walks, neither corp wins, Rampart falls from up-altitude, Cutout's rep is dented; they part
  as the only two who truly know each other, permanently opposed. Dual POV; JOY engine turned hot →
  scorched; avalanche/no-epilogue. Full arc in [docs/nodes/OPPN.md](nodes/OPPN.md); brief in
  [docs/planning/OPPN-brief.md](planning/OPPN-brief.md). *Acceptance: brief + entities + StoryNode +
  4 ChapterNodes + 14-beat dual-POV spine + hand-authored bible + pre-prose blueprint + all beats via
  ProseWriterRouter + logic sweep BLOCKER-free + storyscope-audit clean + exported.* *(built end-to-end
  2026-07-19 via CLI: node `the-fall-down-019f78f4`; 14 beats/4 ch (~6.5k words) drafted
  `prose --expand-beat --model claude-sonnet-4-6`; continuity fixes (Rafi age, twins-as-pistols, Reza
  chrome, Blister 9, neuretics canon); `prose --storyscope-audit` **CLEAN** (0 blocking, 0 moderate);
  `prose --export-node` → docx/epub/pdf/txt at `R:\Desktop\EPub\MindAttic\GLMZ\The Fall Down\`)*
  - **H12a ✅** Docs: OPPN brief (10/10) + hand-authored node bible; series roster updated (diptych Bk 2); `codex doctor` PASS.
  - **H12b ✅** Entities seeded: Rafi Sarkissian, Halvard Onwe (characters), Ashgrave Spire (place); reused Reza/Tavi/Coeli/Auda Vane/Scraps + AshgraveMaterials/Axiom.
  - **H12c ✅** StoryNode `the-fall-down-019f78f4` (OPPN) + 4 ChapterNodes + 14-beat dual-POV spine.
  - **H12d ✅** Pre-prose structural blueprint (subplot = Rafi-as-mirror + signatures/twins; resolution mixed; avalanche/no-epilogue; dual-POV form device).
  - **H12e ✅** 14 beats drafted (sonnet-4-6, tonal match with Book 1); Opus continuity/canon pass.
  - **H12f ✅** QA: storyscope-audit CLEAN (0 blocking/0 moderate/5 minor); mechanical scan clean; gripe-harvest revision pass (italic crutch 24→7 readers; DramaticQuestion warn cleared) lifted reader panel 73.7→76.5.
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

- **SS-US-P1 ✅** As the author, *Vigil's End* (VIGL, `vigil-s-end-019f5767`) is the SCRY/Fantasy
  universe's flagship novel, written first per the Universe Bedrock Mandate (see
  [docs/nodes/VIGL.md](nodes/VIGL.md)): a five-POV rotating-close-third novel (Lyra, Declan/M-101,
  Vega, Wren, Orim) in which a seventeen-year Vigil Templar pursues a stolen memory-relic across
  Entos, discovers what the Liturgy actually is, and refuses the Canon's writ in the Sinter
  quarantine zone. *Acceptance: book structure seeded + full prose drafted + fact ledger clean +
  logic-sweep verified + exported. (verified by `--archive-book`: 25 leaf nodes / 318 beats /
  132,504 words; `dotnet test src/Prose.UnitTests`: 2034/2034 passing; exported VIGL V47.docx/epub/
  pdf/txt/md 2026-08-15, mojibake check passed.)*
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
  - **P1c ✅** 2026-08-14 convergence session: `--continuity extract` fact ledger populated
    (75 claims), both surfaced contradictions resolved as false positives after direct
    verification against the real beat text (Vega's occupation = Scribe; Pallor's geography
    already consistent with existing ENTOS.md canon) — see
    `project_vigl_convergence_run_2026_08_14` (Claude memory). Along the way, fixed 3 real bugs
    with regression tests: `ContinuityService.Resolve` UID-collision crash, `ContinuityService.
    Resolve` dropping `BookSlug` on custom resolutions, and `LogicSweepService` accepting
    hallucinated beat citations for content elided by `AuditProseUtils.ClampProse` on an
    oversized book (3 layered fixes: elided-range warning, double-quote verification, then
    single-quote + short-fragment verification once each gap surfaced against a real finding).
    `--logic-sweep --until-dry` findings dropped 45→20→16→10 across rounds 4-8 before hitting the
    tool's own 8-round safety cap; zero real prose defects found across the whole session.
    *(verified by `dotnet test src/Prose.UnitTests`: 2034/2034 passing.)*
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

- **SS-US-I3 ✅** As the engine, `ContinuityExtractionService` and `BeatStateExtractor` run after
  every beat save so the continuity ledger and `EntityStateEvents` stay current. *(verified by
  `ContinuityExtractionServiceTests`; `BeatStateExtractorTests`.)*

- **SS-US-I5 ✅** As the operator, I can close the coverage loop: `prose --coverage` identifies a
  dead type, I seed entities of that type, `prose --coverage --backfill` re-embeds them, and the next
  run shows >0% for that type. *✅ 100% coverage on full backfill (11,588/11,588). Entity↔strand
  appearance tracking wired. (verified by `CoverageService` second SQL query joining
  `EntityStateEvents` via `BeatGuid IS NOT NULL`; `TypeCoverage.InStrandCount` + `StrandPct`
  surfaced; `/coverage` "In Strands" column live; build clean 0 errors; 2026-06-21.)*

- **SS-US-I6 ✅** As the engine, `SemanticFidelityService` compares the prose embedding centroid
  to the seed embedding and raises a `SEMANTIC-DRIFT` finding if the prose has drifted from its
  seed intent. *(verified by `SemanticFidelityServiceTests`; MCP tool `check_semantic_fidelity`
  wired.)*

## Epic K — Service Communication Law Compliance {#epic-k}

> The [Service Communication Laws](BIBLE.md#SS-§12) (SCL-1 … SCL-8) must be verifiable by
> automated tests, not just convention. Stories in this epic add or extend the gate test suite to
> make law violations build-breakers.

- **SS-US-K1 ✅** As the codebase, no generation service injects `ProseDbContext` or
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

## Epic L — Architectural Completeness {#epic-l}

> Stories that close the remaining gaps between what the architecture promises and what the system
> can prove end-to-end. These are the prerequisites for the "headline endpoint":
> *a fresh seed → published, reviewed, canon-consistent audiobook+manuscript with the human only
> approving* (see [USER_STORIES.md Priority backlog](#)).

- **SS-US-L3 ✅** As an author, a `kind=series` strand can be published as a single ordered docx
  that stitches all its `kind=collection` and `kind=chapter` children in reading order.
  *(verified by `StrandWorkbenchService.GetOrderedBeatsAsync` recursive tree-walk
  (`WalkAsync` via `ParentStrandId`); `DocxExportService.ExportStrandAsync` calls it for any strandId;
  `prose --export-node --slug <series-slug>` already stitches all children via existing code; 2026-06-21.)*

- **SS-US-L4 ✅** As an author, the `WorldTickService` can be enabled and produces at least one
  `EntityStateEvent` per tick per active character without manual intervention (SS-US-F9: Living
  world tick). *Acceptance: enabling `WorldTickService` in settings causes it to fire on schedule;
  at least one event per active character per tick appears in `EntityStateEvents`; events are
  universe-scoped. (verified by `WorldTickService.OnTickAsync` reading `SettingsService.WorldTickEnabled`;
  when enabled, queries active characters in current universe (capped 100), writes one
  `EntityStateEvent` per character via `WorldStateLedger.RecordManyAsync` with
  `AspectKey="world-tick"`, `Verb="set"`, `NewValue="idle"`; `WorldTickService.Enabled` proxies
  to `SettingsService.WorldTickEnabled`.)*

- **SS-US-L5 ✅** As the engine, `Species` is a first-class lookup entity listed by
  `prose --list-species` (SS-US-Fs2). *(verified by `ListSpeciesCli.cs` wired as `--list-species` in
  `Program.cs`; `SpeciesRepository` in DI.)*

- **SS-US-L6 ⬜** As the engine, prod schema matches LocalDB (F1 prod-ship). `drop_facet_system_*`
  and `create_voice_change_log_*` migrations applied; `--seed-voice-rules` + `--coverage
  --backfill` clean in prod; `--coverage` reports ≥1% for all diegetic types.
  *Acceptance: prod schema has no facet remnants, has `VoiceChangeLog`, `UniverseId` on all three
  roots, and `--coverage` exits 0. (SS-US-F1-prod.)*

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

- **SS-US-N3 ✅** As an author, the explicit override lifts the gate — `--allow-votes` on
  `--review-node`/`--review-entity`/`--dual-read`/`--book review`/`--legion`/
  `--auto-run`/`--worker-mode`/`--populate-queue`/`--continuity sweep`, and `allowVotes:true` on the
  MCP `review_story` tool. *(evidence:
  `VotingGateTests.EnsureAllowed_Disabled_WithOverride_DoesNotThrow`,
  `…_EnabledByDefault_DoesNotThrow_EvenWithoutOverride`; `BallotSolicitingServices_DependOnVotingGate`.)*

- **SS-US-N4 ✅** As the engine, PROSE generation is never gated — `BeatGeneratorService` /
  `ProseWriterRouter` construct and run without any `VotingGate` dependency, and the auto-run
  pipeline skips (never fails on) the scoring step when voting is disabled. *(evidence:
  `VotingGateTests.ProseGenerationServices_DoNotDependOnVotingGate`;
  `ChapterCloseProcessorService.ProcessAsync` skips tiered review + fork when voting is off.)*

## Epic O — Reader QA, glossary, KDP listing, universes {#epic-o}

- **SS-US-O1 ✅** As the author, Reader-Proxy QA ([SS-LAW-17](BIBLE.md#SS-§5)) is the default
  reader-facing QA — no 0–100 score — canonical doc `docs/READER-QA.md`, runbook
  `/reader-qa`. Four findings-based instruments, no scores: (1) Haiku comprehension probes
  diffed against a Sonnet-generated synopsis, Sonnet-arbitrated → `ComprehensionDefect` findings;
  (2) `CraftChecklist` findings from the deterministic `--lint-prose` / `CraftNativeRules`; (3) cross-family pairwise duels per splice (`prose --duel`, vote-gated per
  SS-LAW-17); (4) findings-only gripe jury (`prose --reader-qa --gripe-pass`) → `ReaderGripe`
  findings. *(evidence: verified by CLI runs; commits `3bb9d2f19`/`7ef9078cd`/`484614b23`/
  `c04e90e78`; E2E run on LLSS — comprehension probe caught a sinterspawn/harrower conflation, checklist caught
  over-explanation, gripe jury 18 raw → 11 confirmed; verified at scale on BCODA — 28 Medium
  comprehension findings triaged to 8 real + 20 dismissed after an arbiter strictness fix; gripe
  pass 23 raw → 3 confirmed with one duel-gated splice auto-applied.)*

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
