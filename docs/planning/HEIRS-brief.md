---
codex: 1
project: Prose
layer: planning
code: HEIRS
title: The Room That Was Never Locked
universe: ANTHOLOGY
updated: 2026-08-26
---

# Story Brief: HEIRS — The Room That Was Never Locked {#SS-BRIEF-HEIRS}

> **This brief is mandatory before creating a node bible or any DB records.**
> Part of the ANTHOLOGY isolated-anthology experiment (docs/series/ANTHOLOGY.md). Same shared prompt
> (the anthology's one-line theme), refracted through a third, unrelated Author-filter. Written
> entirely as **Eamon Bellhaven Cray**, not as "Prose's default voice." Shares no characters,
> places, entities, or plot facts with any other ANTHOLOGY story.

---

## 1. Series Position {#SS-BRIEF-HEIRS-§1}

**Universe:** ANTHOLOGY

**Story type:** Standalone — third of four isolated submission-target short stories
(2,000–6,000 words, target: Easton Tales Publishing's "Enshrouded: A Horror Anthology," theme:
something buried, hidden, or concealed, and the darkness that follows when revealed).

**Book(s) this story serves:** None.

**Author Persona:**
- **Name:** Eamon Bellhaven Cray
- **Age / sex:** 58, male
- **Location:** Wiltshire, England
- **Upbringing/background:** Classically educated (Latin, boarding school); spent three decades
  as an archivist cataloguing estate papers and inheritance records for aristocratic families;
  unmarried; professionally and temperamentally obsessed with provenance — who owned what, in
  what order, and what wasn't disclosed in the will.
- **Causal link (bio → style):** A career spent reading other families' sealed correspondence and
  probate disputes produces an epistolary instinct — the story wants to be told through documents,
  not direct narration; classical schooling produces ornate, Latinate diction and long
  subordinate-clause sentences; decades of professional emotional distance from other people's
  grief produces a narrator who conveys feeling only through objects and inventory, never by
  naming it.
- **Style contract:** Dual frame — dated first-person diary fragments (archaic, formal, written
  by the dead mother) interleaved with a third-person present-day thread (the siblings clearing
  the house). Heavy use of em-dash interruptions AND semicolons in both threads. Formal, elevated
  register throughout; contractions avoided in the diary voice. Emotion is never stated directly —
  only through described objects, inventories, and the diary's clipped omissions.

**Approximate in-universe timing:** N/A — contemporary standalone frame around a decades-old
diary.

---

## 2. Arc Contribution {#SS-BRIEF-HEIRS-§2}

**None — by design.** Isolation constraint. Value: the anthology theme read as inherited family
concealment — a locked room and a diary that insists, long before anyone locked it, that no one
should ever have gone upstairs after dark.

---

## 3. Prerequisites {#SS-BRIEF-HEIRS-§3}

**None.** Fully standalone.

---

## 4. Character Entry States {#SS-BRIEF-HEIRS-§4}

| Character | Entry State |
|---|---|
| Helena Ashcombe | Introduced fresh: eldest sibling, organizing the clearance of her late mother's house. |
| Simon Ashcombe | Introduced fresh: younger brother, more willing than Helena to dismiss the diary as their mother's late-life confusion. |
| Vivienne Ashcombe (deceased) | Exists only as diary author — voice, not presence. Wrote the diary entries that structure the story. |

---

## 5. Character Exit States {#SS-BRIEF-HEIRS-§5}

| Character | Exit State | Ledger Update Needed? |
|---|---|---|
| Helena Ashcombe | Locks the room herself before leaving — for reasons she cannot articulate and does not try to. Unresolved/open ending. | No — HEIRS has no ledger; retired with the story. |
| Simon Ashcombe | Goes upstairs after dark once, against the diary's rule, to prove there's nothing to it. What he sees afterward is never described — only that he never goes upstairs again. | No. |
| Vivienne Ashcombe | Remains dead; her diary's final entry (found last, out of chronological order) recontextualizes everything the siblings believed about why she never let them upstairs. | No. |

---

## 6. What It Plants {#SS-BRIEF-HEIRS-§6}

**None.** Isolation constraint.

---

## 7. What It Pays {#SS-BRIEF-HEIRS-§7}

**None.** Isolation constraint.

---

## 8. Thematic Complement {#SS-BRIEF-HEIRS-§8}

**Theme:** Inheritance is not only property but also unstated rules, and the rule you inherit
without explanation is the one that was protecting you.

**Register:** Sorrow / Grey, formal and restrained rather than visceral.

**Adjacent stories in the same book or release window:** GRAVE, ALIAS, ECHO — deliberately
**not** complementary; unrelated authors, casts, and prose styles by design.

**How it complements them:** Only thematically (the shared anthology prompt), never narratively.

**What would be duplicated if this story didn't exist:** The inherited-family-secret reading of
the theme, delivered through an epistolary/diary device — no other ANTHOLOGY story uses documents
as a structural frame.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-HEIRS-§9}

**Resolution mode:** Unresolved/open — the siblings never learn what was truly upstairs; only
that the diary's rule was correct.

**Moral polarity:** Ambivalent (default) — Vivienne's silence protected her children at a cost
she alone paid; no clean villain.

**Ending style:** Avalanche (default) — the final, out-of-order diary entry reframes everything
right before the story ends; no epilogue, no resolution scene after it.

**Escalation curve shape:** Routine clearance of the house → the newly-locked room is found →
early diary entries are dismissed as sentimental → entries grow stranger and more specific about
"after dark" → Simon breaks the rule once → the last entry (read last, dated first) recontextualizes
the whole house.

**Event-type palette:** Discovery → revelation (via document) → transgression → recontextualization
(the diary's out-of-order structure delivers a second, worse revelation at the end).

**3–5 intertextual anchors:** 1. *The Turn of the Screw* (formal restraint, ambiguous supernatural
threat) 2. Susan Hill's *The Woman in Black* (epistolary/documentary horror frame) 3. Daphne du
Maurier's *Rebecca* (inherited house, dead woman's unstated rules governing the living).

**Subplot thread:** None — short-story length; the diary IS the subplot-as-frame, not a separate
thread.

**Form device:** Epistolary/diary frame narrative, dual timeline (present-day siblings + dated
diary entries read out of chronological order, oldest-last).

---

## 10. Entity Seeding Required {#SS-BRIEF-HEIRS-§10}

| Entity | Type | In DB? | DB seed command / MCP tool |
|---|---|---|---|
| Helena Ashcombe | character | [ ] | `create_character` (originNodeSlug=heirs) |
| Simon Ashcombe | character | [ ] | `create_character` (originNodeSlug=heirs) |
| Vivienne Ashcombe | character | [ ] | `create_character` (originNodeSlug=heirs, status=deceased) |
| The Ashcombe House | place | [ ] | `create_place` (originNodeSlug=heirs) |

Run entity-mention scan after the draft to confirm coverage.

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] `docs/series/ANTHOLOGY.md` roster will be created/updated with this story's code and status
- [x] No character/arc ledger applicable — isolation constraint documented
- [x] No plant/payoff registry applicable — isolation constraint documented
- [x] No world-revelation sequencing applicable — ANTHOLOGY has no locked revelations to spoil
- [x] Entity seeding list (§10) complete — all entities exclusive to this story
- [x] Node bible (`docs/nodes/HEIRS.md`) does NOT exist yet — brief precedes bible
