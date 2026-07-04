# GLMZ Corpus — Logic & Continuity Sweep, Final Report (2026-07-04)

Per SOP (CLAUDE.md, Quality Verification): full end-to-end logic reads of all 11 GLMZ stories
plus the Rook trilogy as one cross-book read. No panels, no votes. Every finding triaged
(BLOCKER/MODERATE/MINOR) and fixed with minimal splices; per-story audits live beside this file.

## Verdict: ALL STORIES NOW LOGIC-CLEAN

Every named contradiction, arithmetic failure, orphan reference, knowledge-state break, and
bible↔prose disagreement found by the sweep has been fixed and verified. ~25 BLOCKER/CRITICAL
findings total — the overwhelming majority PRE-EXISTING draft debris, not artifacts of the
structural campaign.

## The big catches (what score panels never localized)

| Story | Catch |
|---|---|
| UNDR | Sorrel alive in 4 beats after staying behind at the threshold; population ledger never summed (now: 43→Leaf→Slip→Sorrel→Grale→Vesh→38, verified) |
| TEST | Manowar accumulator "climbing" after canonical permanent shutdown; 1400kg vs 600kg; Bear in two places at 1247; bible quoted lines that never existed |
| SPRW | "Sparrow" used 5× before the beat where Elias coins the name (naming payoff + person/crew/machine ambiguity restored); a craft note published inside prose; 8 beats in the wrong tense frame |
| CxC | Closing line's arithmetic failed (21+7≠31; now 21+10=31); one missed beat still called Gerald "the dog... Scout had named"; manifest mark convention inverted vs NxR |
| MxG | Vox's graze referenced as healed the night BEFORE it's inflicted; Wennick's death framed incompatibly with Tidewell |
| NxR | Rook "shot" Adalemo where MxG shows a railing fall (both refs now the fall) |
| VATD | Stale reallocation-arc beat sitting between the reinstall beats; loaner seated before the harvest that caused it; Osei interrogated after being declared dead (repositioned) |
| ATTE | Chapter 4's IsChapterStart flag off — heading silently missing from EVERY export; Ch3 heading stranded on a disabled beat; unreconciled draft cluster (school name, spans, data points) |
| MNEMO | Nuru's Row 19 given to Amara; Zone 5/6 disagreement ×3; the seven-days ultimatum never paid (now pays on the day-seven chapter); finale named Nuru with no antecedent |
| DWIACE | Duplicate Pulse-departure resolved (Sequence B kept); Sol mirror = the WANT (brother Mateo canon); Celeste interleave verified knowledge-safe |
| PNHL | "Four words" vs the three-word locked line (canon = three, fixed in prose + bible ×4); dead-man's-send and channel-kill capabilities now shown before use |
| SRZR | Camel ate gooseberries never bought (now bought); the replaced dog reappeared in the Glooms (now the cat); companion swap now noticed, unexplained per lock |

## Bibles re-synced in the same pass
PNHL (§10 spine → shipped 22 beats; three-words ×4), TEST (spine → 43 beats + real ending;
both misquoted locks corrected), DWIACE (§6 interleaved structure; Sol mirror), VATD (whole-drawer;
Ekow/Casimir POV mechanism), SPRW (desk ending; Tadesse quote; retired lock), NxR (graze location),
MxG (Wennick → Tidewell extraction), UNDR untouched (prose brought to bible instead).
codex doctor: PASS after every doc change.

## Open items (deferred ledger, task #12)
- SPRW: bible §2's "pill before the failed door attempt" scene has no host beat anywhere
  (pre-existing) — amend bible §2 or author the scene.
- "filed" verb ruling: SRZR uses it as Sasha's habitual verb (9 beats); Kyle-register rule
  nominally reserves it. Recommend: rule = Kyle owns the filing-METAPHOR-SYSTEM, plain usage free.
- Entity merge: stale duplicate "Casimir Mwamba" → Ekow Ato.
- MNEMO corpus "train" usage vs no-trains-in-GLMZ canon (book-wide call).
- Repo-root `fixes/` staging litter from one fixer (move under audit-outlines or delete).
- Blocked on explicit request per SOP: any panels/votes; UNDR book-node review path.

## Working-tree note
Uncommitted this session: docs/nodes/*.md bible edits (PNHL, TEST, DWIACE, VATD, SPRW, NxR, MxG),
CLAUDE.md (SOP), legion.json (claude-team), audit-outlines-2026-07-03/ (all reports + fix files),
plus extensive DB changes (system-versioned; temporal history preserves everything).
