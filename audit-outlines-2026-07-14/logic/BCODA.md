# Logic Sweep — BCODA (Bushido Coda) · 2026-07-14

**Scope:** Cross-beat continuity sweep following the 3-coordinate coordination + per-beat verdict
pass. Targeted the deferred cross-beat suspects the per-beat verdict structurally could not see,
plus the blast radius of the day's splices. Method: two report-only Sonnet audit agents
(Gantry/character canon; intimacy-geometry + plant/payoff chains), then triage → minimal-splice
fix → verify. NO votes, NO panels.

**Verdict:** PASS after fixes. 4 confirmed defects repaired (3 BLOCKER, 1 MODERATE); 2 items
deferred with cause; several candidate non-issues correctly dismissed.

## Findings & fixes

### BLOCKER — Gantry gender contamination (fixed)
Gantry is canonically female (~28 she/her across the book) but one contiguous block in Ch16
"Ghost Period" (the Iowa fuel-stop → Behemoth awe → bolt-thrower bridge, beats 3212–3215) carried
8 masculine references — a drafting-pass regression, not authorial ambiguity. Fixed all 8:
- 3212 ×3: "twelve of **his** daily words"→her; "**He** does that every run"→She; "**He** seems comfortable"→She
- 3213 ×1: "**He** was at the cab's rear window"→She
- 3214 ×2: "**He** had done the math… then **he** waited"→She/she; "**He** spent **his** last five words"→She/her
- 3215 ×2: "**He** packs for the brief"→She; "**He** had no words left"→She
Verified: 4/4 target beats now read "She…"; crane-operator/Kyle masculine pronouns in the same beats left intact.

### BLOCKER — intimacy geometry, beat 1316 vs 1317 (fixed)
Beat 1316 (before 1317 in reading order) staged Pixel dressed, shirt on, "three feet away," wall
re-established — then 1317 has her on the floor with Kyle's thumb still at her throat. The dressing
action also duplicated beat 5165, which delivers it correctly *after* 1317's "This can't happen
again." Minimal fix: dropped 1316's final paragraph (the shirt/three-feet-away/dressing lines),
preserving its unique material (Pixel checking Kyle's stitches, which is consistent with them still
being close). Verified: 1316 no longer contains "three feet away"; scene now reads 1315 → 1316
(clinical stitch-check, still close) → 1317 (thumb at throat) → … → 5165 (dresses, distance).

### MODERATE — knowledge-state slip, beat 5353 vs 5354 (fixed)
5353 ended "He had not known what the choice at 6.2% was. **He knew now.**"; 5354 opens with Kyle
*not* knowing (reasoning about the wrong corridor). Cut "He knew now." — keeps the mystery
consistently unresolved (matches the keep-whodunits-open doctrine). Verified: "He knew now" absent.

## Correctly dismissed (no defect)
- **63% vs 6.2%** — unrelated metrics (contract-volume concentration vs a combat risk-cost); coincidental leading digit, never claimed connected.
- **6.2% plant→payoff** (Mira/Bucktown 4271–4272 → 4393) holds cleanly; exact-language payoff, Kyle has the information.
- **Kyle's sword names** — right=Silence / left=Cacophony consistent throughout.
- Per-beat-verdict false positives confirmed harmless in context: 485 (live man, jaw-*work* destroyed), 4393 ordering (message precedes pull-over).

## Deferred (flagged, not fixed)
- **MINOR / open plant:** beat 4393's reply does not answer Kyle's Ch8 query ("*Query re: Clearlight
  Operations LLC. Who holds the officer-of-record credential?*", beat 4312). Likely intentional
  rogue-AI misdirection, but the Clearlight / officer-of-record question may be an orphaned plant —
  **needs a downstream-payoff check** before ruling.
- **DATA / mojibake:** beat 4311 has a `�` in a load-bearing sentence ("on his closing half-�"); beat
  4408 stores `?` where em-dashes belong. Pre-existing encoding corruption — needs a dedicated
  text-repair pass (not guess-reconstructed here).

## Sibling artifacts
- Per-beat verdict worklist: `reports/coordination/BCODA.verdict.json`
- 3-coordinate map: `reports/coordination/BCODA.coordination.json` + `docs/nodes/BCODA.md §SS-BCODA-COORD`
