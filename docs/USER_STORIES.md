---
codex: 1
project: StreetSamurai
code: SS
layer: stories
status: living
updated: 2026-06-07
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

**Endpoint reached when** the prod-ship + F7 + F8 are green and F10 demonstrates the flywheel.

### Audit log

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
