---
codex: 1
project: Prose
layer: planning
code: <CODE>
title: <Story Title>
universe: <entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>
updated: <YYYY-MM-DD>
---

# Story Brief: <CODE> — <Story Title> {#SS-BRIEF-<CODE>}

> **This brief is mandatory before creating a node bible or any DB records.**
> A story that cannot answer all 10 sections does not belong in the roster yet.
> After filing, update `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md` §1–2 (roster) and §3 (character ledger exit states).

---

## 1. Series Position {#SS-BRIEF-<CODE>-§1}

**Universe:** <entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>

**Story type** (pick one):
- [ ] Main series chapter — part of Book N (specify which book: ___)
- [ ] Caper-of-the-week — standalone adventure that fits within a book's tone
- [ ] Season arc beat — directly advances the season villain or book-level arc
- [ ] Standalone — independent <entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity> story, feeds into the universe but not a specific book chapter

**Book(s) this story serves:** ___ (cite NodeCode or Book number; "none" is valid for pure world-builds)

**Approximate in-universe timing relative to BCODA:** Before / During / After Book ___

---

## 2. Arc Contribution {#SS-BRIEF-<CODE>-§2}

Which of the 5 overarching <entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity> arcs does this story advance?
Check all that apply and describe how.

- [ ] **<entity repo="vocabulary" guid="019d6143-aae2-7985-9d5c-63a652ad8571">Rogue</entity> AI** (the entity, the Unanimity, the Consensus, UNDERTOW, <entity repo="character" guid="8556c863-afa0-4c23-8140-9ce70642bea8">Sparrow</entity>) — how: ___
- [ ] **<entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>-Atlas** (ATLAS-9, terminal neuretics, the curriculum, the correspondence) — how: ___
- [ ] **<entity repo="faction" guid="019d6143-a893-7801-860b-0eda6a216393">Lotus Syndicate</entity>** (the syndicate, <entity repo="faction" guid="019d6143-a893-7801-860b-0eda6a216393">Lotus</entity> civil war, Reiko Oka, <entity repo="faction" guid="019d6143-a893-7801-860b-0eda6a216393">Lotus</entity> characters) — how: ___
- [ ] **Sable-Axiom** (Sable's network, the Axiom thread, Year One / Lullaby) — how: ___
- [ ] **Riven** (the thin spots, Schism, 5D physics, Reads as instruments) — how: ___
- [ ] **None** — this is pure world texture / character introduction (describe value): ___

---

## 3. Prerequisites {#SS-BRIEF-<CODE>-§3}

What prior stories or events must have happened in-universe before this story is set?
Name NodeCodes. A prerequisite story must be COMPLETE (not just planned) before this story
enters production.

| Prerequisite | NodeCode | Why Required |
|---|---|---|
| | | |

**None if truly standalone — write "None" explicitly.**

---

## 4. Character Entry States {#SS-BRIEF-<CODE>-§4}

For each recurring character who appears, copy their end state from the Character Arc Ledger
(`docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §3`) for the book/period preceding this story.

| Character | Entry State (from ledger) |
|---|---|
| | |

If their current state is TBD in the ledger, resolve it here and update the ledger.

---

## 5. Character Exit States {#SS-BRIEF-<CODE>-§5}

Where are recurring characters going out of this story?
These states must be consistent with their arc trajectories in the ledger.
After filing this brief, copy exit states into `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §3`.

| Character | Exit State | Ledger Update Needed? |
|---|---|---|
| | | |

---

## 6. What It Plants {#SS-BRIEF-<CODE>-§6}

Cross-story plants this story seeds. For each plant, the payoff story must already exist in the
Story Roster (`docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §1–2`). If the payoff story doesn't exist yet, the plant
cannot be written.

| Plant Description | Payoff Story (NodeCode) | Payoff Chapter/Beat (if known) |
|---|---|---|
| | | |

After filing, add these rows to `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §5`.

---

## 7. What It Pays {#SS-BRIEF-<CODE>-§7}

Cross-story payoffs delivered in this story. Cite the planting story.
Update the Status column in `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §5` when each payoff is written.

| Payoff Description | Plant Story (NodeCode) | Plant Description |
|---|---|---|
| | | |

---

## 8. Thematic Complement {#SS-BRIEF-<CODE>-§8}

How does this story's theme, register, and emotional texture differ from its adjacent stories?
What does it contribute that adjacent stories don't?

**Theme:** ___

**Register** (check one): Joy / Grey / Sorrow / Combat-heavy / Quiet / Other: ___

**Adjacent stories in the same book or release window:** ___

**How it complements them:** ___

**What would be duplicated if this story didn't exist:** ___

---

## 9. Structural Blueprint Seed {#SS-BRIEF-<CODE>-§9}

Preliminary anti-tell decisions to seed `prose --generate-blueprint`. These are provisional;
`--generate-blueprint` will refine and commit them.

**Resolution mode** (pick one — never "protagonist achieves internal peace"):
- [ ] External/situational resolution
- [ ] Unresolved / open
- [ ] Mixed (external + significant cost)

**Moral polarity:** Ambivalent (default) / Clear-cut (justify): ___

**Ending style:** Avalanche (default) / Other (justify): ___

**Escalation curve shape** (describe arc from beat 1 to climax): ___

**Event-type palette** (2-3 types to cycle through — combat / negotiation / revelation /
betrayal / loss / reunion / heist / escape / etc.): ___

**3–5 intertextual anchors** (real-world works whose structural DNA to steal):
1. ___
2. ___
3. ___

**Subplot thread** (thematically parallel to main plot, not decoration): ___

**Form device** (optional — frame narrative / anachrony / dual POV / epistolary / etc.): ___

---

## 10. Entity Seeding Required {#SS-BRIEF-<CODE>-§10}

Every named character, CorpoNation, place, weapon, or document in this story must be in the DB
before prose begins. Cross-check against the Entity Seeding Roadmap in `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §7`.

| Entity | Type | In DB? | DB seed command / MCP tool |
|---|---|---|---|
| | character | [ ] | `prose --add-character --name "..." --species human` |
| | place | [ ] | MCP `add_entity` |
| | faction | [ ] | `prose --add-corponation --name "..."` |

Run `prose --scan-entity-mentions --slug <slug>` after each chapter draft to keep coverage current.

---

## Checklist Before Proceeding

- [ ] All 10 sections filled (no blank answers — "None" or "TBD with justification" is acceptable)
- [ ] `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md` Story Roster updated with this story's Code and status
- [ ] `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md` Character Arc Ledger updated with exit states from §5
- [ ] `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md` Plant/Payoff Registry updated with rows from §6 and §7
- [ ] World-Revelation Sequencing in `docs/series/<entity repo="place" guid="019d6143-a927-73ac-abfa-37c77f9cff8d">GLMZ</entity>.md §6` checked — this story doesn't spoil a locked revelation
- [ ] Entity seeding list (§10) complete and cross-checked against the Seeding Roadmap
- [ ] Node bible (`docs/nodes/<CODE>.md`) does NOT exist yet — brief precedes bible
