# GLMZ Contradiction Reread Guide

## Overview
You're rereading all 17 GLMZ stories to identify contradictions, misconceptions, and synchronization issues between prose and canon. This guide will help you work systematically.

## Stories to Reread (17 total)

| # | Code | Title | Status | Beat Count | Notes |
|---|------|-------|--------|-----------|-------|
| 1 | ATTE | Attendance | Gateway | 24 | Yemina Fola investigation; GREY register |
| 2 | BLST | Ballast | WIP | 30 | 30 beats written |
| 3 | BCODA | Bushido Coda | Complete | 435 | Flagship novel; 0 BLOCKERs; logic sweep clean |
| 4 | CxC | Crimson & Chrome | Finished | 11 | Rook trilogy finale |
| 5 | DWIACE | Death Whispers | Finished | 10 | Rennick multi-POV (569 beats in DB) |
| 6 | IxS | Iron & Silk | Complete | 47 | Book 4; 113k words; Lotus arc |
| 7 | MNEMO | Mnemosync | WIP | 17 | Amara & Seto |
| 8 | MxG | Magenta & Gunmetal | Planned | 0 | Rook heist; Inkeri protagonist |
| 9 | NxR | Neon & Rust | Experiment | 1 | Part of Rook series |
| 10 | PXL | Pixel | Stale | 13 | Kyle origin; 14 beats disabled, need rewrite |
| 11 | RTR | Read the Room | Complete | 14 | Faith Larson + Ethan Wolfe; Milwaukee dive bar |
| 12 | SPRW | Sparrow | Pending | 3 | Elias Macias; apt 11134; never fires |
| 13 | SRZR | Steppin' Razor | Finished | 3 | Sasha Võ; Halcyon/OBERON antagonist |
| 14 | STSH | The Long Cut | Complete | 48 | Doc Stash; medical noir; logic sweep clean |
| 15 | TEST | Testament | Finished | 3 | Bear court-martial; Boris Johansen |
| 16 | UNDR | Underclan | WIP | 54 | Glim; 14 chapters; IsWIP=1 |
| 17 | VATD | Vultures | Finished | 24 | Tomas Alvarado & Ekow Ato |

## What to Watch For

### 1. **Character Consistency**
- **Physical appearance**: hair, build, skin tone, distinguishing marks
- **Neuretics/augmentations**: what kind of cyberware? Governed or ungoverned?
- **Age/birth year**: consistent across stories?
- **Aliases/name variations**: does the character go by different names?
- **Relationships**: Who do they know? Romantic status? Feuds?
- **Death status**: Are they alive in all stories? Any resurrection?

**Key Characters to Track:**
- Kyle (appears in multiple stories)
- Sasha Võ
- Faith Larson
- Ekow Ato
- Any other recurring character

### 2. **Timeline Issues**
- Year 2226 baseline. Are there flashbacks to different years?
- Relative ages: if Character X is 35 in BCODA, are they plausible ages in other stories?
- Technology availability: Did something get invented between stories? Retired?
- Character states: If someone is "recovering from trauma" in Story A, are they still recovering in Story B?

### 3. **Location Consistency**
- **Milwaukee**: How is it described across stories? Same geography?
- **Detroit**: Any mentions? Consistent with PXL description?
- **Other GLMZ zones**: Are 5 megalopolises, 12 zones all consistent?
- **Access rules**: If The Low is "ungoverned 30-320m", does every story respect this?

### 4. **Technology Rules** (CRITICAL)
Watch for violations of these canonical rules:

**Currency (Φ / QUANTA)**
- Correct: Φ100, Φ50 (symbol BEFORE number)
- Wrong: 100Φ, 50Φ (backwards)
- Wrong: physical coins or bills (only credstick)
- Wrong: $ or dollar amounts
- *ACTION*: If spotted, note story + beat + exact wording

**Job Names**
- Current: Ghost (ECT/street operator), Channeler, Splicer
- RETIRED: Rider, Exo, RFO, NSB, Jockey (no longer canon)
- *ACTION*: If you see Rider/Exo/RFO used, that's a BLOCKER

**Neuretics**
- Are mesh grown into brain
- Sub-vocal communication (no phones in 2226)
- Can be governed (fatal—kills owner) or ungoverned (safe)
- *ACTION*: Note if story contradicts these facts

**Eigenlift / Aerostatic Systems**
- ONLY tech for flight in GLMZ (no anti-grav, no other lift)
- Altitude determines class hierarchy
- Air Tax exists
- The Low = 30-320m ungoverned zone
- *ACTION*: If anti-grav mentioned, that's a BLOCKER

### 5. **World Rules** (BLOCKERS if violated)
- **Death permanence**: No resurrection, no uploading consciousness
- **Synthetic personhood**: Form ≠ life. Corvin Adaora is human, NOT synthetic
- **Machine God / Schism**: 5D climax at 35th & Halsted. Only 5D survivor. Schism dissipates after.
- **Reach misdirect**: The Reach/psychics blamed lie—don't confirm or deny; Reach=false narrative
- **5D rule**: "no inside"—means interior spaces don't work normally for 5D entity

## How to Use the Survey JSON

1. **Open**: `glmz-contradiction-survey.json` (in StreetSamurai root)
2. **As you reread each story**:
   - Fill in the `character_consistency_observations` array with any character you see
   - Add entries to `technology_rules_violations` if you spot Φ misuse, old job names, etc.
   - Add entries to `world_rules_violations` if death returns, synthetic misuse, etc.
   - In `character_matrix`, track key characters across stories
3. **When you spot a contradiction**:
   - Add a `cross_reference` entry noting the conflicting story
   - Set severity: BLOCKER (breaks continuity) / MODERATE (confuses readers) / MINOR (nitpick)
   - Include beat/chapter reference if possible
4. **After reading all 17 stories**:
   - Run tallies in `summary` section
   - Paste the completed JSON back here
   - I'll batch-analyze and create a fix plan

## Priority: What Breaks Stories

Read these stories FIRST if you're short on time:

1. **BCODA** (435 beats, flagship; already 0 BLOCKERs but verify)
2. **IxS** (Book 4, 113k words; Lotus arc critical)
3. **PXL** (Kyle origin; 14 beats stale/disabled)
4. **ATTE** (Gateway story; sets up Yemina arc)
5. **STSH** (48 beats, logic sweep clean; check consistency)

These are newer/smaller and can be later:
- SPRW (3 beats; pending)
- TEST (3 beats; finished)
- SRZR (3 beats; finished)
- NxR (1 beat; experiment)

## Batch Processing After Reread

Once you paste back the completed JSON, I will:

1. **Severity ranking**: Confirm BLOCKER vs MODERATE vs MINOR
2. **Pattern analysis**: Group contradictions by type (character, tech, world rule)
3. **Cross-story impact map**: Which stories conflict with which?
4. **Fix priority queue**: Order corrections by severity + story ship-readiness
5. **Fix plan**: Beat-by-beat corrections with SQL/prose patches
6. **Verification**: Logic sweep or targeted fix validation

---

**Start whenever ready. Reread at your own pace, fill in observations as you go. No rush—thoroughness > speed.**
