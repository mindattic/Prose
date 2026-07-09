---
codex: 1
project: StreetSamurai
code: SS
layer: stories
status: living
updated: 2026-06-25
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
    Use: `dotnet run --project v3/StreetSamurai.Cli -- --review-strand --slug <slug> --readers 20`

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

- **SS-US-H5 ⬜** As the author, *Underclan* (UNDR) is written as a GLMZ contact-tragedy novel: a
  surface child lost at four into the deep strata below GLMZ is raised by the uncontacted **Underclan**
  (who worship the rogue Leviathan DEEP CURRENT), becomes the Brave **Glim**, is caught on his
  manhood-journey **Surfacing** and dragged topside to the mother he remembers only as a smell — and,
  because he surfaced, the surface comes *down*, bringing sport-hunters, a "rescue" mission, and the
  **Bright Fever** the immunologically-naïve clan cannot survive (FernGully / *Jungle 2 Jungle* /
  rumspringa lineage). Full arc, locks, register, and 14-beat spine in
  [docs/nodes/UNDR.md](nodes/UNDR.md); world canon in [SS-A22](AMENDMENTS.md). *Acceptance: book
  strand seeded + all entities seeded before prose + chapters drafted (Sonnet→Opus) in the DEEP
  register + standalone review ≥82% + cumulative reading-order ≥85%.*
  - **H5a ⬜** Docs: SS-A22 amendment + UNDR bible + this entry; `codex doctor` PASS.
  - **H5b ⬜** Entities seeded before prose (UNDR-US-2 set): Glim/Toby, Noor, Vesh, Knuckle, Sorrel,
    Grale, Corwin Sallow, CANALKEEP-08; factions Underclan / Engine Guild / Daylight Mission /
    Lamplighters; places Homewater / the Tartarian Empire / the Warm; the shine; Bright Fever; candles;
    Made Things.
  - **H5c ✅** Book strand `UNDR` (kind=book) created: id=`019EFF97-BDDA-7C0C-BE97-EE17353769A0`, 14 chapter sub-strands parented. *(2026-06-25)*
  - **H5d ✅** All 14 chapters drafted (50 beats total, DEEP register, direct SQL insert); manuscript exported to `R:\Desktop\EPub\MindAttic\GLMZ\Underclan\Underclan V1.txt`. *(2026-06-25)*
  - **H5e ✅** Standalone review: 83.2/100 (20-reader panel, StrandReviewService, 2026-06-25). Target ≥82% met.

- **SS-US-H6 ✅** As the author, *Magenta & Gunmetal* (MxG) is the quintessential GLMZ "run" story: five freelancers — Rook (planner), Lace (social engineer), Boiler (demo), Vox (netrunner), Scout (QCE rider) — accept a corporate extraction job against Axiom BioNanics, discover the target hired them first via a cutout, survive a wet-squad pursuit, and end on a storm-lashed Lake Platform in a True Lies / Die Hard finale where Rook jumps off the deck onto a strafing VTOL. Full arc, locks, register (HEIST), and 14-beat spine in [docs/nodes/MxG.md](nodes/MxG.md). *Acceptance: strand seeded + all entities seeded before prose + 14-beat spine drafted (Sonnet→Opus) + standalone review ≥82%.* *(verified 2026-06-25 via CLI `ss --review-strand`; standalone 86.7%; re-verified 2026-06-27 at **87.1%** after the Character Doctrine behavior pass — dup beats removed, crew rendered as people per [docs/CHARACTER.md](CHARACTER.md))*
  - **H6a ✅** Docs: MxG strand bible (docs/nodes/MxG.md) + this entry; `codex doctor` PASS. *(2026-06-25)*
  - **H6b ✅** Entities seeded: Inkeri Saarinen `019f00a4061f`, Blessing Agwu `019f00a4408b`, Mikkeli Väinämöinen `019f00a48148`, Tem Okafor `019f00a4cbe2`, Remi Diallo `019f00a51d0a`, Halina Soraya `019f00a571cc` (renamed from "Nadia Vasquez-Park" 2026-06-27 to de-collide with Street Meat's Dr. Nadia Park), Gault `019f00a597aa`; QCE tech `019f00a62820`; PEREGRINE faction `019f00a5c8f7`; Lake Platform `019f00a5f57e`. *(2026-06-25)*
  - **H6c ✅** Strand `MxG` created: id=`019f00a6-5370-7123-843a-7a4831c66e10`, slug=`magenta-gunmetal-019f00a6`. *(2026-06-25)*
  - **H6d ✅** 14 beats drafted Sonnet→Opus, reflowed; surgical passes on beats 4, 6, 7, 9, 11, 13, 14 (PEREGRINE two-cell, exposition cut, Gault teeth, Rook emotional texture, rule plant+break); second pass on beats 11+13 (Gault overlong-absolution cut → body-first ambient moment; Beat 13 rule-break made conscious from inside Rook's POV); exported: *Magenta & Gunmetal V5.docx/epub/pdf/txt* (`R:\Desktop\EPub\MindAttic\GLMZ\Magenta & Gunmetal\`). *(2026-06-25/26)*
  - **H6e ✅** Standalone review 86.6/100 (20-ballot panel, SD 1.98, CI ±0.87, all 20 in 81–88 band, 2026-06-26). Target ≥82% met. HEIST register exemplars harvested. Score at taste-fork ceiling: procedural vs character-voice reader split is load-bearing, not fixable. *(2026-06-25/26)*

- **SS-US-H7 ✅** As the author, **The Rook Trilogy** is a complete, self-rhyming heist saga that *revels in cyberpunk cliché* (runner-vs-corp, Shadowrun/CP-Red/Akira) for readers who want the same story every time — three strands sharing cast, themes, and one converging arc, with the finale paying off clues planted in the first two. Titles descend surface→decay→body: **Magenta & Gunmetal → Neon & Rust → Crimson & Chrome**. The crew were unwitting contractors to their own ending (Helix's body-bank harvest of registered Reads); Rook's count finally comes out in names. *Acceptance: all three strands seeded + entities + 14-beat spines + Sonnet→Opus + standalone review ≥87 each + clue-plants in MxG/NxR.* *(verified 2026-06-27 via `ss --review-strand`: MxG 87.1, NxR 87.7, CxC 87.6 — all ≥87)*
  - **H7a ✅** Character Doctrine ([docs/CHARACTER.md](CHARACTER.md), SS-CHAR) authored + proven as the score lever (86→87.1 on MxG); Action Figure Test + behavioral-consistency system wired to `CharacterBehavioralRules`. *(2026-06-27)*
  - **H7b ✅** CxC (`marrow-chrome-019f0968`) created; entities seeded (Anneke Oyelowo, The Marrow, Sefi Okonkwo; Helix Biosystems); SS-A26 amendment; 14 beats Sonnet→Opus + AntagonistCost structural beat; review **87.6**. *(2026-06-27)*
  - **H7c ✅** Trilogy seam refactored into MxG (#4745 acquisition-for-a-buyer) + NxR (#4841 relocation-as-harvest) — the diligent-reader payoff planted. Rook redesigned (Lightning leader, rotating cast, no-repeat rule dropped, knows Kyle); Nadia Vasquez-Park → Halina Soraya. *(2026-06-27)*

- **SS-US-H7 ✅** As the author, *Steppin Razor* (SRZR, `steppin-razor-019ef7be`, 15 beats) is written to completion: Sasha Võ is dragged from the quiet edge (Joliet) to the densest crowd on the continent by a 5D intelligence on a camel, discovers the AI cabal is drilling live wells under the towers not the frontier, survives four Axiom operatives with Signal and Noise, and walks onto the Loop platform still angry, still here, without putting her back to the door. Psychedelic GLMZ, *Fear and Loathing* propulsion, deadpan-flat protagonist as her own straight man. Full arc, locks, register in [docs/nodes/SRZR.md](nodes/SRZR.md). *Acceptance: 15 beats Opus-polished; standalone review ≥82%; Signal/Noise locks hold; exported.* *(verified by CLI `--review-strand --slug steppin-razor-019ef7be`; 86.6/100, N=20; exported Steppin Razor V4.pdf 2026-06-25)*
  - **H7a ✅** SRZR strand bible written; SS-A20/A21 amendments locked; entities seeded (The Man on the Camel `019ef8055bc8`, The Hereafter `019ef8052de9`, The Joliet Schism `019ef805444e`). *(2026-06-23)*
  - **H7b ✅** 15-beat spine seeded in DB (cold open ×5 + journey ×5 + core ×3 + resolution ×2). *(2026-06-25)*
  - **H7c ✅** All 15 beats written at Opus quality (HIGH-tier, LockTier=true). Signal right / Noise left cross-draw correct throughout. *(2026-06-25)*
  - **H7d ✅** Standalone review 86.6/100 (20-ballot panel, 2026-06-25). Three em-dash encoding artifacts fixed post-review.
  - **H7e ✅** Exported: *Steppin Razor V4.docx/epub/pdf/txt* (`R:\Desktop\EPub\MindAttic\GLMZ\Steppin Razor\`). *(2026-06-25)*

- **SS-US-H8 ⬜** As the author, *The Long Cut* (STSH; "the-long-cut") is a GLMZ medical noir
  in which street medic Amara "Doc Stash" Adeyemi-Kowalski inherits a dead corpo runner's evidence
  implant and has 72 hours to broadcast 428 non-consensual surgical trial records before NSB and
  Scalpel Division destroy the evidence — and her. The story ends with Stash discovering her own
  name in the trial files and broadcasting the evidence anyway. Full arc, locks, register, 14-chapter
  spine in [docs/nodes/STSH.md](nodes/STSH.md). *Acceptance: 14 chapters + 48 beats Sonnet→Opus +
  logic sweep BLOCKER-free + review ≥85% + exported to R:\Desktop\EPub\MindAttic\GLMZ\TheLongCut\.*
  - **H8a ✅** Docs: STSH node bible written; `codex doctor` PASS. *(2026-07-04)*
  - **H8b ✅** Entities seeded: Amara Adeyemi-Kowalski (Doc Stash), Ledger/Cayo Reyes-Ibarra,
    Petra Voss (NSB), Commander Izoha Mwangi (Scalpel Division), Femi Adebayo, Renata Osei
    (deceased); The Dispensary (place); Scalpel Division (faction); MidNorth Medical (CorpoNation).
  - **H8c ✅** StoryNode `STSH` + 14 ChapterNodes created in DB.
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

## Epic M — Emotional Intelligence Examination {#epic-m}

> An 8-dimension, per-beat, character-aware emotional depth examiner that operationalizes the craft
> laws from [CODA.md](registers/CODA.md) into graded, actionable findings. The emotional depth score
> is advisory — a side-car to the 82/85 reader-panel headline — with a blocking advisory cap at the
> Deep/publish gate for the two most diagnostic dimensions. See [RFC 0010](rfc/0010-emotional-intelligence-examination.md).

- **SS-US-M1 ⬜** As an author, `ss --examine-emotion --slug <slug> --effort deep` runs 8 dimension
  checks + a per-beat emotional curve + character ledger extraction, returning a 0–100
  `EmotionalDepthScore`, per-dimension 0–4 scores with strongest/weakest evidence and a beat-scoped
  craft fix, and a beat-by-beat depth curve. *(acceptance: `ExamineEmotionCli` dispatched from
  `Program.cs`; `EmotionalDepthService.ExamineStrandAsync` runs 8 parallel LLM calls; beat curve
  covers every beat; exit 0 = none blocking, 1 = advisory, 2 = blocking.)*

- **SS-US-M2 ⬜** As an author, the `examine_emotional_depth` MCP tool returns the same examination
  envelope as the CLI. *(acceptance: `[McpServerTool]` in `Tools.Quality.cs`; GUID-or-slug
  resolution; same JSON envelope.)*

- **SS-US-M3 ⬜** As an author, `ss --migrate-sql --emotional-examination` creates 4 tables +
  `Beat.EmotionalScore` column idempotently. *(acceptance: re-runnable, exits 0 on 2nd run; all 4
  tables exist; `Beat.EmotionalScore` float? column on the temporal Beats table.)*

- **SS-US-M4 ⬜** As an author, `ss --findings` surfaces `EMOTIONAL-DEPTH` findings from blocking
  dimensions beside structural ones. *(acceptance: `FindingsService.Upsert` with
  `summary: "EMOTIONAL-DEPTH [Name] beat N: fix"`; visible at `/findings`.)*

- **SS-US-M5 ⬜** As an author, the ledger sanity check passes: Rhea (TVYT) has
  Want="keep facts correct / not be managed" and Need="stop calling being-managed competence",
  matching [TVYT.md §71-73](nodes/TVYT.md). *(acceptance: `--examine-emotion --slug tvyt
  --effort deep --json` returns Rhea ledger with `Inferred=false`.)*

- **SS-US-M6 ⬜** As an author, a strand with an open blocking emotional dimension cannot be
  marked publish-ready at the Deep gate; resolving the finding clears the block. *(acceptance:
  publish-readiness check consults open blocking `EmotionalDimensionResults`; resolving the Finding
  clears the block; `Strand.Score` is unchanged by the examination.)*

## Epic N — Voting kill-switch (SS-A44) {#epic-n}

> Every engine path that solicits LLM ballots/scores/votes (reader panels, Legion votes, census,
> entity rating ballots, book/story quality scoring) is DISABLED BY DEFAULT and runs only with an
> explicit per-invocation override. LLM use for PROSE (generation, drafting, polish) is never gated.
> One central gate — `VotingGate` — is consulted at the entry of each ballot-soliciting flow.
> See [SS-LAW-17](BIBLE.md#SS-LAW-17) and [LOGIC.md §6](LOGIC.md).

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
  `--review-node`/`--review-entity`/`--dual-read`/`--book review`/`--legion`/`--run-corpus`/
  `--auto-run`/`--worker-mode`/`--populate-queue`/`--continuity sweep`, `allowVotes:true` on the MCP
  `review_story` tool, and a UI review-button click. *(evidence:
  `VotingGateTests.EnsureAllowed_Disabled_WithOverride_DoesNotThrow`,
  `…_EnabledByDefault_DoesNotThrow_EvenWithoutOverride`; `BallotSolicitingServices_DependOnVotingGate`.)*

- **SS-US-N4 ✅** As the engine, PROSE generation is never gated — `BeatGeneratorService` /
  `ProseWriterRouter` construct and run without any `VotingGate` dependency, and the auto-run
  pipeline skips (never fails on) the scoring step when voting is disabled. *(evidence:
  `VotingGateTests.ProseGenerationServices_DoNotDependOnVotingGate`;
  `ChapterCloseProcessorService.ProcessAsync` skips tiered review + fork when voting is off.)*

### Audit log

- **2026-07-04 — Voting kill-switch SHIPPED ([SS-A44](BIBLE.md#SS-LAW-17)).** Central `VotingGate`
  (`v3/StreetSamurai.Core/Services/VotingGate.cs`) reads `legion.json` `"votingEnabled"` (default
  OFF). Gated at service entry: `NodeReviewService` (4 ballot methods), `EntityReviewService`,
  `EntityRatingService`, `StoryQualityService`, `BookReviewService`, and `ChapterCloseProcessorService`
  (skips the tiered panel + narrative fork gracefully). CLI `--allow-votes` on `--review-node`
  (+`--review-story`/`--run-panel` aliases), `--review-entity`, `--dual-read`, `--book review`,
  `--legion`, `--run-corpus`, `--auto-run`, `--worker-mode` (entity/node-review types),
  `--populate-queue` (entity/story-review), and `--continuity sweep` (auto-resolve/apply); MCP
  `review_story` gains `allowVotes` (default false, returns a structured `voting_disabled` error);
  UI panel/vote buttons pass the override (the click is the explicit request); Operator
  `score_story_quality` tool returns the SS-A44 message when off. NOT gated (deliberate): single-LLM
  diagnostic analyzers (Logic Sweep, `StructuralDiagnosticService`, `ContinuousQualityService`
  contradiction/cliché scan, `EmotionalDepthService`, `OutlineReviewService` structural editor,
  `StoryRefinementService`, `BookOutlineService` outline generation/drift) — they localize concrete
  failures for free, per SS-A44's rationale. Evidence: `VotingGateTests` (11 tests) green;
  `dotnet build -c Release` clean across Core/Blazor/MCP. SS-US-N1..N4 → ✅.

- **2026-07-03 — Node hierarchy redesign SHIPPED ([SS-A43](BIBLE.md#SS-LAW-6)).** The
  overloaded "Strand" abstraction became a typed tree: abstract `Node` + `SeriesNode` /
  `StoryNode` / `ChapterNode`, TPH on the renamed `Nodes` table via a `NodeType` discriminator.
  Migration `20260703162528_NodeHierarchyRedesign` is rename-only (temporal-safe: versioning
  suspended, history tables renamed in lockstep, `NodeType` backfilled on current + history rows;
  53 nodes = 34 chapter / 17 story / 2 series; 2,832 history rows intact). Surfaces renamed: MCP
  `get_story` / `list_stories` / `create_series` / `create_story` / `create_chapter` (+ legacy
  Book/Chapter tools renamed `create_legacy_*`), CLI `--write-story` / `--review-story` /
  `--list-stories` etc.; routes `/node/{slug}` + aliases `/story`, `/strand`. Evidence: unit
  suite 1,250 passed / 8 pre-existing failures (reproduced on HEAD with only the Media fix
  applied; unrelated) — the refactor also fixed the suite-wide SQLite breakage from
  `MediaItemTypeConfiguration` (628 → 8 failures). CLI smoke: `ss --list-stories` reads the
  migrated DB. Local backup `backups/preNodeHierarchy_20260703.bak`.
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

