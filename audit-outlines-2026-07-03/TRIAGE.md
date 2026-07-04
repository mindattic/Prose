# GLMZ Structural Audit — Consolidated Triage (2026-07-03)

Campaign: every GLMZ story structurally capable of 90+, free of contradiction and cliche.
Method: 12 parallel outline+audit passes (one per story), findings verified against live
`NodeBeats` (`IsEnabled=1`, `SortKey` order). Per-story detail in `<CODE>.md` beside this file.

**Result: 0 of 12 stories passed clean.** Every audit found real structural defects — and the
dominant root cause across the corpus is the same: **the bible and the live prose describe
different stories.** Scores are measuring prose quality, not structural soundness.

## Scoreboard

| Story | Score | Verdict | Core defect |
|---|---|---|---|
| SPRW | 95.0 | Minor fixes | Bible stale vs. better revised prose (locked balcony ending gone); Cluster-6 numbering error; beat 4909 leaks the 2187 reveal early |
| MxG | 93.9 | Fixable | Finale needs 5 PEREGRINE bodies, roster locked at 4; crane money-shot set up 3× and never fired; 2 orphaned plants; payment contradiction vs Lock #3; retired "Rider" term |
| CxC | 93.1 | Fixable | "21 in, 14 out" Axiom-job wound contradicts MxG's no-casualty account; Scout's arc reverses one beat later; Adalemo's fate unstaged; ~9k-word live alternate draft (Numbers 4890–4967, all ENABLED) attached to node |
| ATTE | 92.7 | Structural | Bible ending (folder/locker/report) absent from beats; actual climax violates §0 + Lock #3 (rescue story); headcount error (~half the children unaccounted, framed as "one lost"); undefined "Dead Realm" contradicts LOCKED §4b physics |
| TEST | 91.9 | Structural | LOCKED ending (SK862.5/SK900) never authored — story hard-stops on cliffhanger; testimony-delivery self-contradiction (smuggled out at 0600 vs carried in at 1301); Orvenne thread + blocked-names beat never paid off |
| VATD | 91.0 | Small fixes | Reinstall capstone exists only in a disabled beat — told, not shown; "Three nights" line vs <24h clock; Casimir/Ekow name unresolved; 21 disabled legacy beats (one a live lock violation) need quarantine |
| DWIACE | 90.6 | Assembly | VERIFIED in SortKey order: Celeste chapters (Law #0: "book opens with Celeste") sit at chapter positions 10–12 of 12, after the climax; climax bisected by false chapter break; "The Same Cold" absorbs 44% of book; Sol brother/boyfriend contradiction; finale beats possibly not linked |
| MNEMO | 90.3 | Structural | Locked §7 finale (fountain reunion) never written — Ch25–27 open a zero-setup subplot and end mid-stride; razor payoff offscreen; Ekow intro exists only in orphaned draft; 2 dropped surveillance mechanisms |
| SRZR | 90.0 | Structural | Live beats break 3 camel-man locks (named "Devereux", in Act 1, human register); retired Sigma/Ferreira origin reinstated in 5 beats (violates SS-A20); 5-beat bounty thread vanishes; all violations CONFIRMED enabled |
| NxR | 89.6 | Structural | Third-party reveal contradicts Lock #1 ("She did it"), then dropped; Adalemo's mercy unearned (zero prior appearance); heist register delivered as dialogue — crew never earns the exit; Ohara's "worse than dead" stake unresolved |
| UNDR | 86.8 | Structural | LOCKED Tartarian navigation maze ("the climax's first antagonist") never planted, never troubles the flight; Sallow + Daylight Mission never do damage on-page; basin/identification/cough plants abandoned; Sorrel's mandated fork resolves as neither |
| PNHL | 80.7 | Rebuild | 29 live beats implement the SUPERSEDED plot: Nit stolen (violates Lock #8), Assessor never on-page, offscreen dossier win, case-recovery timeline contradiction; bible spine (invitation→dinner→sabotage→confrontation, dress element removed 2026-07-03) is the correct story — pending rewrite H4b/H4c |

## Systemic findings (corpus-level)

1. **Bible↔prose divergence** — 9 of 12 stories. Two directions: prose never implemented the
   locked spine (PNHL, UNDR, ATTE, TEST, MNEMO, NxR, SRZR), or prose evolved past a stale bible
   (SPRW, and partially VATD). Either way the lock system is not being enforced at write time.
2. **Missing endings** — TEST (cliffhanger), MNEMO (mid-stride), ATTE (mid-decision),
   VATD (capstone in a disabled beat). Endings are the least-authored part of the corpus.
3. **Stale/duplicate beats attached to live nodes** — CxC's enabled alternate draft is the worst
   (exports/reviews ingest both drafts); VATD has 21 disabled legacy beats incl. a lock violation;
   PNHL has an all-history-empty beat (Number 4884) and a duplicate discovery beat (4402 vs 4918).
4. **`Beats.Number` ≠ reading order** — reading order is `NodeBeats.SortKey`; always filter
   `IsEnabled=1`. Any tool sorting by Number silently corrupts (confirmed in SPRW, DWIACE).
5. **Antagonists offscreen** — PNHL (Assessor), UNDR (Sallow/Mission), MNEMO (razor scene),
   NxR (unearned mercy). Confrontations reported instead of dramatized.
6. **Orphaned plants** — every story has at least one set-up-and-abandoned thread.

## Recommended fix order

Guards: any beat edit resets ContentHash → lost ballot accumulation, so commit to a FULL pass
per story or don't touch it. One Legion vote per story, at the end only.

- **Wave 1 — rebuilds (biggest headroom):** PNHL (rewrite to corrected bible spine),
  UNDR (build the maze + antagonist rungs), NxR (re-thread reveal, earn the mercy, restore heist).
- **Wave 2 — ending repairs:** TEST, MNEMO, ATTE, VATD (small), DWIACE (mostly relink/reorder,
  low prose cost).
- **Wave 3 — contradiction cleanup on high scorers:** MxG+CxC trilogy reconciliation in one
  sitting (Axiom job account, Scout reversal, roster math, detach alternate draft, Rider→Exo),
  SRZR lock purge, SPRW (re-sync bible to prose, fix Cluster-6 + beat 4909).

Per story: structural fix → regenerate affected prose (Sonnet draft → Opus polish) →
`/review-node` gripes → dual review (≥82 standalone / ≥85 cumulative) → final Legion vote → export.
