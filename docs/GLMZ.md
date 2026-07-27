---
codex: SS
project: StreetSamurai
code: GLMZ
layer: universe
universe: glmz
status: live
tier: series
scope: GLMZ
triggers: GLMZ, glmz, Kyle, Glooms, Chicago, 2226, CorpoNation, Lotus, neuretics, quanta, circuit, street samurai
updated: 2026-07-26
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

## 0.1 Target Reader — 18-40 Men {#SS-GLMZ-0-1}

**GLMZ is written for a male genre-fiction reader, 18-40, in three tiers:**

- **Primary (22-35) — the sweet spot.** Grew up on *Cyberpunk 2077*, *Blade Runner 2049*,
  *Altered Carbon*, and grimdark fantasy (Joe Abercrombie, early Richard Morgan). Tech-literate,
  comfortable with morally gray protagonists and dense worldbuilding.
- **Secondary (18-24).** Pulled in via gaming/anime crossover — *Cyberpunk 2077* fandom, *Ghost
  in the Shell*, VTuber/anime-adjacent audiences.
- **Tertiary (35-40+).** Read classic cyberpunk (Gibson, Stephenson) in the 90s/2000s and want a
  modern take.

Every craft call in this file — the transaction register, the weird, the hard prohibitions —
ultimately serves this reader. When two craft options are equally valid, pick the one this reader
picks up at 11pm and can't put down.

- **Dense worldbuilding, clear sentences.** This reader — Abercrombie, Morgan, Gibson,
  Stephenson — tolerates and wants a thick, specific world (CorpoNation politics, tech that works
  by real rules, faction history). That tolerance is for *lore density*, not *sentence density*:
  CRAFT.md's freshman-clear surface stays the law even here. Depth lives in the world and the
  characters' choices, not in the syntax.
- **Morally gray protagonists, played straight.** Abercrombie/Morgan-grade ambivalence — a
  competent person doing a compromised job for reasons that hold up — not a cartoon antihero and
  not a redemption arc on rails. See Character Doctrine (circumstance → choice → definition) and
  the blueprint's ambivalent-moral-polarity default.
- **Competence under pressure, not power fantasy.** The reader respects a protagonist who is good
  at something specific — a trade, a read on people, a weapon — and proves it by doing the job right
  under a ticking clock, not by being unbeatable. Skill costs something to earn and something to use.
- **Consequence is the payoff, not the shock.** Violence and sex are rendered body-true (Graphic
  Adult Content rule) because that is what this reader respects — real weight, real damage, real
  physical truth — not gratuitous spectacle and not a soft-focus fade-to-black.
- **Camaraderie over romance-as-plot.** Crew loyalty, earned trust, the person who has your back —
  this reader's emotional core is the found-family/brotherhood beat, not the marriage plot. Romance
  exists and can be explicit, but it is a subplot inside the job, never the job itself.
- **The rigged system, beaten by competence, not luck.** CorpoNations, the Air Tax, the ground/sky
  class divide — GLMZ's antagonist is structural. A reader who feels priced out of his own city wants
  a protagonist who out-thinks or out-works the system on a given job, even knowing the system itself
  never falls. Small, earned wins against a machine that doesn't care.
- **Momentum.** Scenes move. Transactional banter (§1) is a release valve mid-tension, never a
  sitcom pause. Gear, tech, and weapon detail satisfy because they are specific and correct, not a
  tour — see §2.
- **Humor is deadpan and priced (§1), never a wink at the reader.** This reader distrusts a story
  that seems embarrassed of itself. GLMZ plays every stake straight.

This is a lens, not a checklist — it does not override the Hard Prohibitions (§5) or the Graphic
Adult Content rule (no minors, no animals, ever). It exists so that when a beat could go two ways,
the choice is legible: which version does this reader — Primary 22-35 above all — read again?

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
