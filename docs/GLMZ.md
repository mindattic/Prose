---
codex: SS
project: StreetSamurai
code: GLMZ
layer: universe
universe: glmz
status: live
triggers: GLMZ, glmz, Kyle, Glooms, Chicago, 2226, CorpoNation, Lotus, neuretics, quanta, circuit, street samurai
updated: 2026-07-18
related: docs/CRAFT.md
---

# GLMZ — Universe Craft Rules {#SS-GLMZ}

> **Scope: GLMZ universe stories only.** Universal prose rules live in **docs/CRAFT.md**
> (Base layer). World facts — geography, Kyle, cast, combat — live in **docs/WORLD.md**.
> This file records GLMZ-specific craft additions and overrides to the Base layer.
> When this file and CRAFT.md disagree, this file wins for GLMZ stories.
>
> The five canon texts (With Teeth · The One That Doesn't Stop · Sexy Time · Street Meat ·
> The Quiet Hour) outrank everything; those pages are the reference.

---

## 0. Authorial Voice — One Author {#SS-GLMZ-0}

**Every GLMZ story is written by the same author (MindAttic). The series is one body of work, and
it must read like it.** A real author has a recognizable hand across all their books — a consistent
craft signature — even as each book's *narrator* sounds like themselves. That cross-story
consistency is a **feature**, not the "monotony" defect. Two levels, never confused:

- **Author signature — synced across the whole series.** The MindAttic-GLMZ hand: the clear
  accessible surface (CRAFT.md), the reader-loved moves (DELIGHT.md), the transaction-under-a-clock
  scene sense, both-readings, the street texture. This is CONSTANT across all 21 stories; it is what
  makes the series one author's work and not 21 unrelated files.
- **Narrator register — varies within each story, and can change beat to beat.** Per DELIGHT
  §12/§14, each POV sounds like its own character, and the moves are a varied palette, never a stamp.
  Variation lives HERE, inside a book — not between books.
  - **The register IS the POV character's Character record** (`Speech*` + `Psychology*` fields — SS-A46
    layer 4), loaded per-beat by DCM. Elias's clerk-precision, Bear's deadpan, Kyle's silences,
    Sasha's edge are the *records*, not a one-line brief — and they evolve as the character does.
  - **A book can change narrator.** VATD alternates Tomas and Ekow; NxR runs Rook/Vox/Stave; DWIACE
    moves across Corvin/Rennick/Tamsin; BCODA across its cast. Each beat is told from ONE character's
    perspective, and its prose must carry THAT character's register.
  - **The node bible must declare a POV map** — which character narrates which beats/chapters — so
    the correct register loads on the correct beat. A story whose bible doesn't say *whose eyes we
    are behind* on a given beat cannot be voiced correctly. Every POV character needs a populated
    Character record before the story is voiced or synced.

**The per-beat sync, then, is three things at once:** the **author voice** (this §0, constant across
the series) + the **loved moves** (DELIGHT.md, a varied palette) + the **POV character's register**
(their Character record, per the bible's POV map). Author signature holds the series together;
register makes each beat sound like the person behind the eyes.

**Reviewer "monotony" is two different things.** Sameness *within* a story (every beat and character
in one cadence) is the real defect — fix it (§12/§14). Sameness *across* stories (a recognizable
MindAttic craft) is the author's signature — keep it. Do not sand the series into 21 different
authors chasing 21 different panels; a distinctive voice a real audience loves beats a bland average.

**Reference exemplars: DWIACE and BCODA.** They own 62 of the 99 top-decile reader-loved beats —
they ARE the MindAttic-GLMZ voice at its best. Do NOT rewrite them; they define the target. When any
other GLMZ story is written or revised, sync it TO that standard — the same clear hand, the same
loved moves, the same texture — so it reads as a new book by the same author, not a different one.

---

## 1. Transaction Register (GLMZ addition to CRAFT §3 Dialogue)

**JOKES HAVE PRICE TAGS** — deadpan, transactional (invoices, salaries, paperwork, dignity).
Kyle deflects with logistics ("I have a motorcycle"). Civilians and kids get the smartest lines.
Every joke has a payer. This is the specific register of a city where everything has a rate —
not universal wit.

---

## 2. World Texture (GLMZ addition to CRAFT §4 Interiority)

- **CorpoNations and tech**: explained by what they DO, in one in-voice clause, never a tour.
  Superminds are weather — discussed by name, possessively, never explained ("CONDUCTOR's
  running them slow tonight").
- **Genetic strays** (turret cats, lumen mice, Null Crows): texture and omen, never explained.

---

## 3. The Weird (GLMZ addition to CRAFT §4 The Noticing)

**THE WEIRD = SENSORY WRONGNESS + SURVIVAL RULES, NEVER EXPLAINED.** Non-psionics feel
wrongness only — no visual perception of anomalous residue. Locals are matter-of-fact; the
reader does the shivering.

Schism constants: eleven steps where nine were, 19Hz from no direction, water striking
something vast and metal below.

---

## 4. Interludes (GLMZ addition to CRAFT §5 Structure)

Interludes exist to remind the reader there is goodness in this world even between the violence.

---

## 5. Hard Prohibitions (GLMZ world facts)

- **No magic.** Psychic powers exist, written as ability (see WORLD.md §1.4 ladder; the slur
  never written). Nothing is rendered as magical.
- Φ never "phi" · no city police (ArcSec only, contracted) · Silence has no powers, no glow,
  no hum · Cacophony holds exactly **five** rounds, counted every time · neuretics lowercase ·
  Iowan Behemoths are not alive · machines are not alive · no anthropomorphizing by narrator.
- Reserved words: Consensus = merged synthetics faction; Choir/Concordance/Chorus = psionic
  only; "Bleed" = neuretics data leakage only (not a schism synonym).
- Kyle's hands never severed, amputated, or reattached · WORLD.md §2.4 story locks hold ·
  whodunits stay open: encode the event, never the culprit.
- GLMZ comms: neuretics only in 2226 — no phones except "Analog" (DWIACE established this).
