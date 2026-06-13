---
codex: 1
project: StreetSamurai
code: SS
layer: rfc
status: in-progress
updated: 2026-06-10
---

# RFC 0002 — Entity X-Ray: scene assembly + per-character voice

## Problem

Three findings converged on 2026-06-10:

1. **The 82.0-review panel's top character complaint:** Pixel, Sable, and Hua all share Kyle's
   clipped, weary register. Root cause: voice is stored at the ROOT level
   (`kyle.narration_voice`, `tone_bible`) — there is no per-character voice the generator
   consults, so everyone inherits Kyle's.
2. **The ~10,000-entity canon is rarely consulted during prose.** Characters, places, and
   objects have rich records (psychology, speech fields, story hooks, edges), but the writing
   prompt does not automatically receive the records of the entities actually IN the scene.
   QuikGraph carries relationships; it does not carry psychology, motivation, or voice into
   the prompt.
3. **Pixel has no life without Kyle** (review: "medic and love interest"). Voice is necessary
   but not sufficient — she needs her own clientele, projects, and stakes encoded as canon the
   assembler can pull.

## Vision (the X-Ray analogy)

Like Amazon X-Ray: at any beat, the engine knows **who is on screen, where they are, and what
they are holding** — and the prose prompt receives a **live memory block** assembled from
exactly those records. Not all 10k entities at the fingertips; the *relevant* ones, every time.

## Design

### 1. Per-character voice ON the entity (exists, underused)

`Characters` already carries `SpeechVocabulary`, `SpeechCadence`, `SpeechSubtext`,
`SpeechUnderPressure`, `IntimacyRegister`, `NarrationVoice`. Work:

- Fill these for every major character (Pixel first; then Sable, Hua, Stash, Echo, Imani,
  Comfort, Mrs. Chen, Ledger). Each gets a register that contrasts with Kyle's
  (e.g. Pixel: precise, numeric, unhurried imperatives, dry-warm, never wry; says the
  measurement where Kyle would say the joke).
- The voice-harvest standing process extends per-character: when a strand scores ≥80%, harvest
  each major character's best lines back into THEIR speech fields, not only Kyle's.

### 2. Beat↔entity links (the X-Ray index)

New bridge table `BeatEntities` (BeatId, EntityId, Role: `present | mentioned | holding |
location`), populated three ways:
- **Extraction:** `extract_entities` (exists as MCP tool) run per beat, persisted instead of
  discarded.
- **Authoring:** the beat details tab lists/edits the scene roster (the X-Ray UI).
- **Inference:** embedding similarity (`EmbeddingService.FindSimilarAsync`) + alias table as
  the fallback net, like SillyTavern lorebook keyword triggers but embedding-backed.

### 3. SceneAssemblyService (the live memory block)

Given a beat (or a span being written), produce one bounded context block:
- characters present → speech fields + psychology + active story hooks + relevant edges
  (relationships to OTHERS in the same scene only);
- location → place record gloss + sensory palette hooks;
- objects held → entity gloss (Silence, Cacophony, the node, the teeth);
- budgeted (token cap), ranked by role: present > holding > location > mentioned.

Injection point: every prose prompt builder (`ChapterBeatWriter`, `ConversationalWriterService`,
narration/voting prompts) receives the block. One service, every consumer — per the
foundations doctrine below.

### 4. Foundations doctrine (applies to this and all future work)

User doctrine 2026-06-10:
- **Every system connects to every other** — no orphaned subsystems.
- **CLI ⇄ MCP parity:** every capability ships as BOTH `ss` CLI flag and MCP tool, or is
  explicitly disposable. (Known parity gaps to close: review-strand CLI-only; chapter export
  session-script-only → promote to `ss --export-chapters` + MCP.)
- **Script lifecycle:** throwaway scripts deleted after use; twice-reused scripts get promoted
  to documented functionality.
- The engine's goal state is **self-sustaining**: write → review (cyberpunk-fan panel) →
  harvest voice → fix → re-measure, on one connected foundation.

### 5. Pixel's independent life (content workstream)

Canon to seed so the assembler has something to pull: her clientele (who else she patches and
builds for), her current bench project, her income, her history before The Pivot, one running
storyline that does not involve Kyle. Encode as Character fields + story hooks + Documents,
not prose-only.

## Open-source survey (completed 2026-06-10)

Twelve candidates evaluated (Microsoft GraphRAG, Zep/graphiti, LightRAG, Letta/MemGPT, txtai,
neo4j-graphrag, LlamaIndex property graph, Haystack, Semantic Kernel VectorStoreTextSearch,
SillyTavern World Info, KoboldAI World Info, authoring wikis). **Verdict: reimplement natively
in C# (~400 lines, zero new runtime dependencies).** Every serious retrieval candidate is
Python-only; a REST sidecar buys operational complexity and data duplication for a pattern the
existing stack can express directly. Three ideas worth stealing:

1. **SillyTavern lorebook injection algorithm** (the intellectual core): keyword/alias-triggered
   entries, budget-gated, priority-ordered, with *recursive activation* — injected content is
   re-scanned so Kyle's block can trigger Silence's block. Reimplement as the assembler's
   trigger/budget layer. (Its storage is flat text — ours carries typed fields, which is the
   upgrade.)
2. **graphiti/Zep tri-hybrid scoring**: BM25 + embedding + graph traversal, merged. Pure vector
   search is weak on short distinctive tokens like character names; SQL Server full-text
   (`CONTAINS`) unioned with the existing vector query and linearly re-ranked
   (`α·bm25 + (1−α)·cosine`) reproduces it in ~50 lines of T-SQL.
3. **Semantic Kernel's `ITextSearchStringMapper` pattern**: per-entity-type formatters that
   compose typed fields (Psychology, SpeechVoice, Motivation…) into the injected block. Borrow
   the pattern, skip the framework (its SQL Server connector is still preview, and
   `EmbeddingService.FindSimilarAsync` already covers retrieval).

Also borrowed conceptually: neo4j-graphrag's `VectorCypherRetriever` (match entity → traverse
one hop for in-scene relations) maps onto QuikGraph traversal; LightRAG's low/high split maps
onto our present-vs-mentioned roles. GraphRAG, Letta, txtai, Haystack, Kobold: skip — wrong
shape, wrong runtime, or both.

### Resulting assembler shape (supersedes the sketch in §3 where they differ)

`SceneContextAssembler` runs four passes: (1) name/alias hash scan over the beat text (the
lorebook trigger, O(tokens) against a cached QuikGraph node-name index); (2) hybrid
full-text + vector retrieval for unnamed-but-relevant entities; (3) one-hop QuikGraph
expansion over scene-relevant edges with a confidence floor; (4) budget gate — rank by
(explicit name > embedding > graph neighbor) × entity importance, serialize through
per-type `IEntityContextFormatter`s, stop at the token cap, one recursive re-scan of
injected content.

## Non-goals

- Replacing the SQL canon with a graph DB (SS-LAW-1 stands; QuikGraph remains a projection).
- Injecting all of an entity's record (the block is a budgeted digest, not a dump).

## Implementation state (2026-06-10)

**Built:** `SceneContextAssembler` (Core/Services) implementing all four passes; registered in
DI; CLI `ss --assemble-scene (--beat <guid> | --text "…") [--budget N]` (AssembleSceneCli);
MCP tool `assemble_scene_context` (Tools.Scene.cs — visible after the MCP server restarts).
Structural registry types (chapter/book/strand/series/beat) are excluded from rosters. Smoke
test on the Kitchen beat: Pixel + Kyle by name with contrasting voice blocks, Ezra Vance and
Imani via graph hop, ~2,000-token block, exit 0. Pixel's empty voice fields
(SpeechSubtext / SpeechUnderPressure / SpeechIntimacyRegister) filled the same day.

**Built (second pass, same day):**
- **Prompt injection live in every generation path:** `BeatContext.XRayContext` → the
  `BeatGeneratorService` system prompt ("SCENE X-RAY — entities on screen RIGHT NOW…"),
  populated by `SceneGenerationService`; plus all three `StoryStarterService` paths
  (opening / continue / rewrite) via `BuildXRayBlockAsync` (nullable-service + try/catch:
  assembly failure never blocks generation).
- **`BeatEntities` persistence:** raw idempotent DDL (FindingsService pattern), PK
  (BeatId, EntityId), `PersistRosterAsync` with replace semantics.
- **Backfill:** `ss --assemble-scene --backfill --slug <strand> [--harvest]` — tree-walks
  the strand (a book slug covers all chapters), persists every beat's roster.
- **The reverse direction:** `HarvestRevealedDetailsAsync` — per beat, proposes durable
  details the prose reveals about in-scene entities as findings with prefix
  `XRAY-REVEAL [Entity]:` (Category Other, severity Low). PROPOSE-ONLY by design: applying
  to canon stays an explicit human field-pick, per the standing no-auto-route rule.

**Not yet built:** the details-tab X-Ray UI (reads BeatEntities); BM25/full-text leg of the
hybrid query; per-character voice harvest extension; Pixel's independent-life canon;
MCP twin for backfill/harvest (assemble_scene_context exists; long-running backfill stays
CLI-side for now).

## Graduation criteria

- A strand written WITH scene assembly scores ≥ the 82.0 baseline with the character-voice
  complaint gone from the weakness clusters.
- Beat details tab shows the scene roster for any beat of Bushido Coda.
- `BeatEntities` populated for ≥1 full book; assembler P95 under 150ms from warm cache.
