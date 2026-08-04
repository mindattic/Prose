---
codex: 1
project: StreetSamurai
layer: planning
code: QRT
title: "QRT"
universe: HORROR
updated: 2026-08-03
---

# Story Brief: QRT — "QRT" {#SS-BRIEF-QRT}

> **First book in the HORROR universe.** No series board exists yet (`docs/series/HORROR.md` is
> not created — one book does not need a coordination board; revisit if/when a second HORROR
> book is planned). This brief adapts the standard 10-section template; sections written for
> GLMZ's cross-book arc machinery are marked N/A where HORROR has no equivalent yet.

---

## 1. Series Position {#SS-BRIEF-QRT-§1}

**Universe:** HORROR.

**Story type:** Standalone — the first HORROR book, feeds nothing else yet.

**Book(s) this story serves:** None. Fully standalone.

**Timing:** Real-world contemporary setting, no in-fiction continuity to anchor against.

---

## 2. Arc Contribution {#SS-BRIEF-QRT-§2}

N/A — HORROR has no overarching arcs (it is anthology-shaped by design, see `docs/universes/
HORROR.md` §2). This story's contribution is establishing the universe's flagship register: a
single, technically-grounded transgression against reality, escalated through verifiable detail,
never explained. See `docs/HORROR.md` — QRT is cited there as the originating exemplar of the
Observer-Effect Discipline (§3).

---

## 3. Prerequisites {#SS-BRIEF-QRT-§3}

None.

---

## 4. Character Entry States {#SS-BRIEF-QRT-§4}

N/A — first book, no ledger yet.

---

## 5. Character Exit States {#SS-BRIEF-QRT-§5}

| Character | Exit State |
|---|---|
| Gordan Rosniak (protagonist, POV) | Gone dark — no radio, no logs, months of self-imposed silence, believing withdrawal is safety |
| Priya Standish (corroborating ham) | Unwittingly reports "working" Dara on a night Dara never touched the rig — last word in the book |
| Owen Bui (corroborating ham) | Reports a private detail Dara never transmitted, triggering the Act 4 turn; unaware of what he's confirmed |
| Sal Ferraro (corroborating ham) | Same function as Owen — independent second corroboration, so the reader can't dismiss one witness as unreliable |
| The Overlap (entity, DB stub only — never named on-page) | Unresolved; last confirmed active after Dara's silence began |

---

## 6. What It Plants {#SS-BRIEF-QRT-§6}

N/A — no future HORROR book exists yet to pay off into. If a second HORROR book is ever written
that reuses the Observer-Effect mechanic or "The Overlap," record that decision here retroactively
and create `docs/series/HORROR.md`.

---

## 7. What It Pays {#SS-BRIEF-QRT-§7}

N/A — nothing precedes this book.

---

## 8. Thematic Complement {#SS-BRIEF-QRT-§8}

**Theme:** Identity on a medium where sound is the only proof of self. Something has spent months
reconstructing the protagonist from pure pattern — Morse fist, rig harmonics, verbal tics,
callsign discipline — until the pivot reveals it was never listening to the signal at all. The
protagonist's own technical rigor (recording every session, running controlled baiting
experiments) is the thing that almost saves them and the thing that gives the reader the clearest
window into how wrong their working theory is.

**Register:** Quiet, procedural dread — a methodical narrator whose competence is the source of
tension, not comfort. Not combat-heavy, not Joy-adjacent; closest analog in-house is the "Grey"
register (BCODA/ATTE) filtered through analog-horror technical precision rather than cyberpunk
grime.

**Adjacent stories in the same book or release window:** None — first HORROR book.

**How it complements them:** N/A.

**What would be duplicated if this story didn't exist:** N/A — nothing precedes it. It establishes
the pattern future HORROR books can either follow or deliberately break.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-QRT-§9}

**Resolution mode:** Unresolved / open — cold/ambiguous close per the user's specified ending.
Never internal-understanding; the protagonist's withdrawal is a failed countermeasure, not growth.

**Moral polarity:** N/A / not applicable — no antagonist with intent is confirmed to exist as a
moral actor; the story never adjudicates whether "The Overlap" has agency, hostility, or any
motive at all.

**Ending style:** Cold/ambiguous (HORROR default, `docs/HORROR.md` §4) — the threat drops off-page,
time passes, then one final concrete detail (a contact thrilled to have "finally worked you" last
week) confirms it never stopped.

**Escalation curve shape:** Five movements, each raising the stakes of what "proof of identity"
means: (1) written record can lie (logbook) → (2) live testimony can lie (another operator's ear)
→ (3) the phenomenon adapts in real time, not from a snapshot (baiting experiments) → (4) the
leak was never the radio at all (private, unspoken thoughts) → (5) total withdrawal fails to stop
it, because it was never dependent on the protagonist's participation.

**Event-type palette:** discovery/verification (logbook, tape review) / controlled experiment
(baiting) / betrayal-by-medium (recordings that shouldn't lie, do) / corroboration-gone-wrong
(other hams confirming the impossible) / withdrawal-and-failure (going dark, still not enough).

**3–5 intertextual anchors:**
1. *Session 9* (2001, dir. Brad Anderson) — dread built entirely from analog recording media and
   the horror of listening back to something you don't remember producing.
2. Mark Z. Danielewski, *House of Leaves* — unreliable documentary evidence (tape, transcript,
   footnote) as the horror delivery mechanism itself.
3. *Pontypool* (2008, dir. Bruce McDonald) — a radio booth as the entire physical stage for an
   escalating, never-fully-explained contagion of language/identity.
4. Shirley Jackson's method generally (*The Haunting of Hill House*) — psychological ambiguity
   sustained by never confirming the haunting is external to the protagonist.

**Subplot thread:** The corroborating hams (Priya, Owen, Sal) form a thematically parallel
carrier — each one's unwitting testimony is a small, human-scale echo of the protagonist's own
crisis of "how do I know what's real on this medium," escalating from mundane (Priya just misses
a scheduled contact) to devastating (Owen repeats a private thought verbatim).

**Form device:** Document interleave (confirmed by blueprint retrofit after prose was drafted) —
quoted logbook entries, a spreadsheet-building beat, and a verbatim-quoted QSL card function as
embedded documentary evidence inside an otherwise straight linear narrative. Third-person limited
(close/deep POV), present-tense-adjacent, structured as 5 chapters mapping to the user's 4 Acts +
Ending.

---

## 10. Entity Seeding Required {#SS-BRIEF-QRT-§10}

| Entity | Type | In DB? | DB seed command |
|---|---|---|---|
| Gordan Rosniak (KJ7ROS) | character | [x] | `ss --add-character --file ... --universe horror` |
| Aimes Rosniak-Bishop (husband) | character | [x] | `ss --add-character --file ... --universe horror` |
| Min-jun and Ji-ho Rosniak-Bishop (twins, grouped) | character | [x] | `ss --add-character --file ... --universe horror` |
| Priya Standish | character | [x] | `ss --add-character --file ... --universe horror` |
| Owen Bui | character | [x] | `ss --add-character --file ... --universe horror` |
| Sal Ferraro | character | [x] | `ss --add-character --file ... --universe horror` |
| The Overlap (DB stub, never named on-page) | character (entity stub) | [x] | `ss --add-character --file ... --universe horror` |
| Aldergrove Flats, WA (fictional unincorporated community) | place | [x] | `ss --add-place --file ... --universe horror` |

---

## Checklist Before Proceeding

- [x] All 10 sections filled (N/A recorded explicitly where HORROR has no equivalent machinery yet)
- [x] `docs/HORROR.md` + `docs/universes/HORROR.md` created (first HORROR book — no pre-existing
      craft/world docs to update instead)
- [x] Entity seeding — 6 entities (Gordan, Aimes Rosniak-Bishop, twins Min-jun/Ji-ho grouped,
      Priya, Owen, Sal) + The Overlap (DB stub) + Aldergrove Flats (place)
- [x] BookNode `QRT` created (`019fca42-10a2-7aff-9aa9-8e796d96b1e0`)
- [x] ChapterNodes created (5, mapping to the 4 Acts + Ending)
- [x] Node bible hand-authored (arc, POV/voice register, locks, beat spine) — direct
      `NodeBibleSections` write (MCP `set_book_bible` unavailable this session)
- [x] Structural blueprint generated via `--generate-blueprint --retrofit` after prose (confirmed
      subplot, escalation curve, "document interleave" form device, quiet/unresolved ending)
- [x] Prose drafted, beat by beat (22 beats, all 5 chapters)
- [x] Logic sweep — 1 BLOCKER (rename leftover "three eight-year-olds") + 1 MODERATE (bible
      mislabeled POV as first-person; prose is correctly third-person limited) found and fixed
- [x] Reader-Proxy QA — comprehension probes 0 defects/5 chapters; craft checklist 0 findings,
      89.4% pass-rate, 0 DON'T hits
- [x] Exported — `QRT V1.{docx,epub,pdf,txt}` + synopsis + DCM viz at `R:\Desktop\EPub\MindAttic\QRT\`
