# Embedding Grounding Audit — 2026-05-09

> **STATUS — 2026-05-10:** Fully closed. All six flagged candidates are now embedding-grounded; closure commits listed at the bottom of this file. Subsequent prompt-grounding work should re-run this audit pattern (substring vs. embedding classification, priority-1 vs. priority-2 by impact-per-cost) on whatever new services have shipped since.

Read-only audit of LLM prompt construction sites in `v3/StreetSamurai.Core/Services/`. Goal: classify each site by how it grounds prompts with canon and identify candidates that should migrate from substring/keyword grounding to `EmbeddingService.FindSimilarAsync` / `FindSimilarProseAsync`.

## EmbeddingService surface (reference)

`v3/StreetSamurai.Core/Services/EmbeddingService.cs`

- `FindSimilarAsync(queryText, k=8, entityTypes?)` — top-k entities, server-side `VECTOR_DISTANCE` cosine, sub-second at ~10k corpus
- `FindSimilarProseAsync(queryText, k=5, scopeKind?)` — top-k chapters/beats
- Cache is populated and drift-detected (SHA-256 source hash)

## Classification

- **A** — Embedding-grounded (uses `FindSimilar*Async`)
- **B** — Substring/keyword-grounded (`Contains`, `GetByName`, regex over names)
- **C** — Hand-curated context (caller passes explicit IDs)
- **D** — No canon grounding (deterministic / pure logic / not LLM)
- **E** — Hybrid

## Already correct (A)

| Service | Site | Notes |
|---|---|---|
| AskService | `AskService.cs:47` | `FindSimilarAsync(question, k=8)` → Records.Json into LLM context. Reference impl. |
| BeatGeneratorService | `BeatGeneratorService.cs:30,98` | `FindSimilarProseAsync(query, k=4, "beat")` for style anchors. |
| DialogueService | `DialogueService.cs:38` | `FindSimilarAsync(query, k=4, ["character"])` for voice peers. |
| NpcGenerator | `NpcGenerator.cs:42` | `FindSimilarAsync(query, k=5, ["character"])` for role-adjacent examples. |
| RelationshipDiscoveryService | `RelationshipDiscoveryService.cs:61` | `FindSimilarAsync(query, k*2)` for semantic edge candidates. |

**Hybrid (E):** `FactInterpreterService.cs:232` uses `FindSimilarAsync` as a 0.55-similarity fallback when `GetByName` lookup fails. Acceptable.

## Top fix candidates

### Priority 1 — high-impact, clear embedding wins

| Service | Site | Current | Recommended |
|---|---|---|---|
| EntityExtractionService | `EntityExtractionService.cs:31` | `graph.AllNodes().Select(n => n.Name).Take(100)` — hardcoded 100-name dedupe list | `FindSimilarAsync(storyText, k=20)` — gives the LLM a thematically pre-filtered candidate set instead of an arbitrary alphabetic prefix. Catches entities the prose alludes to without name-dropping. |
| ContinuousQualityService | `ContinuousQualityService.cs:107` | `BuildGroundingContextAsync` via `WorldGraphService` slug match | `FindSimilarAsync(chapterText, k=10)` — current path misses thematic contradictions when prose mentions are oblique (paranoid behaviour without naming the trusting canon character). |
| ConversationalWriterService | `ConversationalWriterService.cs:76` | `MentionsResolved()` substring + dossier load | Augment with `FindSimilarAsync(userPrompt + contextSoFar, k=3)` for serendipitous canon discovery. |

### Priority 2 — medium impact, enables new capability

| Service | Site | Current | Recommended |
|---|---|---|---|
| StoryStarterService | `StoryStarterService.cs:89` | Hand-picked characters/location/premise | `FindSimilarAsync(premise, k=5)` — seeds story openings with thematically-adjacent canon to avoid orphan generation. |
| BookReviewService | `BookReviewService.cs:55,82` | Manually concatenates `book.ChapterIds` | `FindSimilarProseAsync(context, k=3, "chapter")` — flag thematically-orphaned chapters; surface motif anchors. |
| SuggestionEngineService | `SuggestionEngineService.cs:31` | `db.FindCharacter()` + truncation | `FindSimilarAsync(storyState + unresolvedSeeds, k=5)` — surface characters whose agendas align with current tension, not just present roster. |

### Priority 3 — correctness / tuning

- **FactInterpreterService** — the 0.55 fallback threshold is empirical; deserves a justifying comment or a recall/precision measurement on real matches.

## Intentional non-targets

- **CanonGroundingService** — post-generation entity scaffolding (uses `XrefService` name lookup). Correct by design; canon is meant to grow.
- **ContinuityExtractionService** — Legion-Quorum extracts assertions canon-agnostically; resolution is deterministic post-vote. Embedding here would bias what the prose "actually says."
- **OutlineReviewService** — hand-curated world-rules system string (no police, Tier system, Behemoth rules) at `OutlineReviewService.cs:82–100`. Intentional canonical guardrails, not retrieval.
- **WritingQualityService, MotifService, BehaviorPredictionService, CrewAssessmentService, ImagePromptRegenService** — no LLM prompts (deterministic / heuristic).

## Observations

1. `EntityExtractionService` is the most impactful fix: a 100-name `Take(100)` prefix is reactive; embedding pre-filtering is proactive and cheap.
2. No service currently uses prose embeddings to match emotional-state cadence (e.g., "find beats with similar protagonist register"). Future optimization, not a bug.
3. The hybrid pattern in `FactInterpreterService` (substring primary, embedding fallback) is a reasonable default for any service where exact-match lookup is the canonical operation but graceful degradation matters.

## Scope summary

- 8 services already embedding-grounded
- 1 hybrid (acceptable)
- 3 high-priority migration candidates (`EntityExtractionService`, `ContinuousQualityService`, `ConversationalWriterService`)
- 3 medium-priority candidates (`StoryStarterService`, `BookReviewService`, `SuggestionEngineService`)
- 12+ services either deterministic or correctly hand-curated

Migration of the six candidates is a separate task — not executed in this audit.

## Closure record (2026-05-10)

All six candidates migrated. Each migration injects `EmbeddingService` as a nullable constructor parameter so cold-cache / no-API-key environments fall through to the prior behavior — no breaking changes.

### Priority-1

| Service | Commit | Approach |
|---|---|---|
| `EntityExtractionService` | `d44bfc3e7` | Replace `graph.AllNodes().Take(100)` with `FindSimilarAsync(storyText, k=20)` for the "existing entities" prompt context. Graph-prefix fallback when embeddings unavailable. |
| `ContinuousQualityService` | (already done) | Audit was wrong — the service already used `FindSimilarAsync` with substring fallback at the time of the audit. Verified on re-read. |
| `ConversationalWriterService` | `319c5d627` | Three-stage `ResolveMentions`: substring (floor) + book protagonists (always-relevant) + top-3 `FindSimilarAsync` hits (thematic discovery). |

### Priority-2

| Service | Commit | Approach |
|---|---|---|
| `StoryStarterService` | `5bcb914d5` | "ADJACENT CANON" hint block in opening prompts via `FindSimilarAsync(premise, k=5)`. Names+types only — resonance seeds, not full dossiers. |
| `BookReviewService` | `5bcb914d5` | "THEMATIC NEIGHBORS" block in review context via `FindSimilarProseAsync(premise, k=3, scopeKind="chapter")`. Anchor candidates for the review LLM. |
| `SuggestionEngineService` | `5bcb914d5` | "OFF-CAST RELEVANT" hint via `FindSimilarAsync(state + unresolvedSeeds, k=5, entityTypes=["character"])`. Excludes current cast — surfaces canon characters whose tensions match the scene but who aren't on stage. |

### Pattern (for the next audit)

Every closure followed the same shape:
1. Add `EmbeddingService? embeddings` as the trailing constructor parameter (default `= null`).
2. Build the embedding-grounded section in a private async helper (`BuildAdjacentCanonAsync`, `BuildOffCastRelevantAsync`, `BuildContextAsync`).
3. Wrap the embedding call in `try/catch` returning the fallback (substring / empty string / plain context).
4. Inject the helper's output into the existing prompt assembly without restructuring it.

This keeps each migration one-file-one-commit-reversible and never blocks on the embedding cache being warm.
