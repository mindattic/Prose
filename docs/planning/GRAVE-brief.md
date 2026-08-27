---
codex: 1
project: Prose
layer: planning
code: GRAVE
title: The Warm Ground
universe: ANTHOLOGY
updated: 2026-08-26
---

# Story Brief: GRAVE — The Warm Ground {#SS-BRIEF-GRAVE}

> **This brief is mandatory before creating a node bible or any DB records.**
> A story that cannot answer all 10 sections does not belong in the roster yet.
> Part of the ANTHOLOGY isolated-anthology experiment (docs/series/ANTHOLOGY.md) — every section
> below is answered under the ISOLATION CONSTRAINT: this story shares no characters, places,
> entities, or plot facts with any other ANTHOLOGY story. It is written entirely as **Corwin Ashby
> Teale**, a fictional author persona, not as "Prose's default voice."

---

## 1. Series Position {#SS-BRIEF-GRAVE-§1}

**Universe:** ANTHOLOGY

**Story type:** Standalone — one isolated submission-target short story (2,000–6,000 words,
target: Easton Tales Publishing's "Enshrouded: A Horror Anthology," theme: something buried,
hidden, or concealed, and the darkness that follows when revealed).

**Book(s) this story serves:** None — first of four deliberately unconnected ANTHOLOGY stories.

**Author Persona (the story's voice, not a character in it):**
- **Name:** Corwin Ashby Teale
- **Age / sex:** 66, male
- **Location:** Person County, North Carolina — rural, tobacco-farming country
- **Upbringing/background:** Left school at 16 to run the family farm after his father's stroke;
  never left the county until his 50s; self-taught reader (Faulkner, Flannery O'Connor, the King
  James Bible); learned storytelling from porch-telling, not workshops.
- **Causal link (bio → style):** A lifetime of oral, run-on porch stories and church cadence
  produces long compound-complex sentences stitched with semicolons; a farmer's intimacy with
  soil, weather, and what's under both produces dense sensory/agricultural imagery; suspicion of
  "improvement" (a lifetime watching land get bought and dug up by people who don't know it)
  is the emotional engine of the premise itself — the land resents being renovated.
- **Style contract:** Third-person limited, past tense. Sentences run long and stitched with
  semicolons and commas — no short punchy fragments. Paragraphs are single extended units
  (150–300 words). Dialogue is rare, sparse, and delivered through action beats rather than
  "he said" tags. No italics, no epistolary devices, no em-dash interruptions (that's HEIRS'
  device, not his). Diction is plain Anglo-Saxon vocabulary, never Latinate or academic.

**Approximate in-universe timing:** N/A — contemporary standalone, no shared ANTHOLOGY timeline.

---

## 2. Arc Contribution {#SS-BRIEF-GRAVE-§2}

**None — by design.** ANTHOLOGY has no overarching arcs; the isolation constraint means this story
cannot advance, reference, or set up any other ANTHOLOGY story. Value: pure standalone horror
short fiction, written entirely in one fictional author's voice as a craft experiment in
authorial-persona isolation (Stephen King / Richard Bachman model).

---

## 3. Prerequisites {#SS-BRIEF-GRAVE-§3}

**None.** Fully standalone; no prior ANTHOLOGY story required or referenced.

---

## 4. Character Entry States {#SS-BRIEF-GRAVE-§4}

N/A — no recurring characters exist in ANTHOLOGY (isolation constraint). All characters below are
original to this story and will be seeded with `originNodeSlug` scoped to GRAVE so no future
ANTHOLOGY story can reuse them, even by name collision.

| Character | Entry State |
|---|---|
| Denny Holt | Introduced fresh: mid-30s, inherits his estranged uncle's farmhouse, begins a basement renovation with his wife. |
| Callie Holt | Introduced fresh: Denny's wife, skeptical of the move, first to notice something's wrong with the new dig. |

---

## 5. Character Exit States {#SS-BRIEF-GRAVE-§5}

| Character | Exit State | Ledger Update Needed? |
|---|---|---|
| Denny Holt | Goes down into the shaft after Callie disappears into it; does not come back up. Avalanche ending — no rescue, no explanation delivered to the reader. | No — GRAVE has no ledger; this character is retired with the story. |
| Callie Holt | Descends first, drawn by something under the warm dirt; last seen from Denny's POV, not recovered. | No. |

---

## 6. What It Plants {#SS-BRIEF-GRAVE-§6}

**None.** Isolation constraint — no cross-story plants permitted.

---

## 7. What It Pays {#SS-BRIEF-GRAVE-§7}

**None.** Isolation constraint — no cross-story payoffs permitted.

---

## 8. Thematic Complement {#SS-BRIEF-GRAVE-§8}

**Theme:** The land remembers what was put into it, and renovation is a kind of trespass —
the buried thing was never gone, only patient.

**Register:** Grey / Quiet dread building to Combat-adjacent physical horror at the climax.

**Adjacent stories in the same book or release window:** ALIAS, HEIRS, ECHO — deliberately
**not** complementary. Each is written by a different fictional author persona with a distinct
premise, cast, and prose style; the four are designed to read as though submitted by four
unrelated authors to the same anthology, not as a linked collection.

**How it complements them:** It doesn't, intentionally. Its only relationship to the other three
is thematic (all four answer "buried/hidden/concealed") — never narrative.

**What would be duplicated if this story didn't exist:** The literal-burial reading of the
anthology's theme — the other three approach it as identity, family secret, and memory, not soil.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-GRAVE-§9}

**Resolution mode:** External/situational resolution — the shaft/land wins; no internal epiphany.

**Moral polarity:** Ambivalent (default) — Denny and Callie did nothing to deserve this beyond
disturbing ground that wanted to stay undisturbed.

**Ending style:** Avalanche (default) — escalating physical wrongness culminating in Callie's
disappearance and Denny's descent; no epilogue, no explanation.

**Escalation curve shape:** Begins with mundane renovation friction (cost, permits, an odd smell)
→ the sealed shaft is found and doesn't match any blueprint → small wrongnesses accumulate (warm
dirt in a cold month, tool losses, a smell like a held breath) → Callie hears something and goes
down alone → Denny follows and the story ends mid-descent.

**Event-type palette:** Discovery → escalating dread → loss/disappearance (2–3 types, no combat
set-piece — this is atmospheric, not violent, horror).

**3–5 intertextual anchors:** 1. Shirley Jackson's *The Haunting of Hill House* (a house that
resents being understood) 2. Flannery O'Connor's rural Gothic sensibility (land, faith, and
consequence) 3. *The Descent* (film) — literal underground claustrophobia as horror engine.

**Subplot thread:** None — 6,000-word ceiling means a single tight thread; no subplot capacity.

**Form device:** None — straight third-person limited, no frame narrative (that's HEIRS' device).

---

## 10. Entity Seeding Required {#SS-BRIEF-GRAVE-§10}

| Entity | Type | In DB? | DB seed command / MCP tool |
|---|---|---|---|
| Denny Holt | character | [ ] | `create_character` (originNodeSlug=grave) |
| Callie Holt | character | [ ] | `create_character` (originNodeSlug=grave) |
| The Holt Farmhouse | place | [ ] | `create_place` (originNodeSlug=grave) |

Run entity-mention scan after the draft to confirm coverage.

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] `docs/series/ANTHOLOGY.md` roster will be created/updated with this story's code and status
- [x] No character/arc ledger applicable — isolation constraint documented
- [x] No plant/payoff registry applicable — isolation constraint documented
- [x] No world-revelation sequencing applicable — ANTHOLOGY has no locked revelations to spoil
- [x] Entity seeding list (§10) complete — all entities exclusive to this story
- [x] Node bible (`docs/nodes/GRAVE.md`) does NOT exist yet — brief precedes bible
