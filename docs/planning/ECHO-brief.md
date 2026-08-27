---
codex: 1
project: Prose
layer: planning
code: ECHO
title: Two Streets Over
universe: ANTHOLOGY
updated: 2026-08-26
---

# Story Brief: ECHO — Two Streets Over {#SS-BRIEF-ECHO}

> **This brief is mandatory before creating a node bible or any DB records.**
> Part of the ANTHOLOGY isolated-anthology experiment (docs/series/ANTHOLOGY.md). Same shared prompt
> (the anthology's one-line theme), refracted through a fourth, unrelated Author-filter. Written
> entirely as **Priya Okonkwo-Lindqvist**, not as "Prose's default voice." Shares no characters,
> places, entities, or plot facts with any other ANTHOLOGY story.

---

## 1. Series Position {#SS-BRIEF-ECHO-§1}

**Universe:** ANTHOLOGY

**Story type:** Standalone — fourth of four isolated submission-target short stories
(2,000–6,000 words, target: Easton Tales Publishing's "Enshrouded: A Horror Anthology," theme:
something buried, hidden, or concealed, and the darkness that follows when revealed).

**Book(s) this story serves:** None.

**Author Persona:**
- **Name:** Priya Okonkwo-Lindqvist
- **Age / sex:** 31, female
- **Location:** No fixed single origin — raised across Lagos, Malmö, and Toronto
- **Upbringing/background:** Raised by a single Nigerian mother after her Swedish father left
  early; moved countries three times before adulthood; began a clinical psychology PhD, dropped
  out after a personal crisis she has never fully described to anyone; now works night shifts and
  writes fiction as self-therapy.
- **Causal link (bio → style):** An itinerant, multi-country childhood produces a narrator with no
  single stable "home" register — sentences fragment and reassemble rather than build in one
  steady rhythm; clinical training produces precise vocabulary for dissociation and intrusive
  thought, deployed against herself rather than a patient; the abandoned PhD and undescribed
  crisis is the direct source of an unreliable narrator who repeats and slightly contradicts her
  own account of the same detail, because she genuinely isn't sure which version is true.
- **Style contract:** First person, present tense. Short, fragmented paragraphs interrupted by
  italicized intrusive-thought run-on sentences. Dialogue is unmarked — no quotation marks,
  folded directly into the narration — because she doesn't fully trust the boundary between what
  was said aloud and what she only thought. Repetition of specific images/phrases recurs with
  small, destabilizing variations each time (the detail that keeps almost-but-not-quite matching).
  Structure is non-linear: waking present intercut with dream fragments, not clearly separated by
  scene break.

**Approximate in-universe timing:** N/A — contemporary standalone.

---

## 2. Arc Contribution {#SS-BRIEF-ECHO-§2}

**None — by design.** Isolation constraint. Value: the anthology theme read as suppressed
memory — the buried thing is the narrator's own psychological history, and the "reveal" is her
own mind's dream-rehearsal of it converging, detail by detail, on a real house she has never
consciously entered.

---

## 3. Prerequisites {#SS-BRIEF-ECHO-§3}

**None.** Fully standalone.

---

## 4. Character Entry States {#SS-BRIEF-ECHO-§4}

| Character | Entry State |
|---|---|
| Odessa Frey | Introduced fresh: early 30s, insomniac, has had the same recurring basement dream for weeks, dismisses it as stress until its details start matching a real house for sale two streets over. |

Minimal cast by design — this story runs on one point-of-view consciousness; a second named
character would dilute the unreliable-narration effect the Author Persona's bio calls for.

---

## 5. Character Exit States {#SS-BRIEF-ECHO-§5}

| Character | Exit State | Ledger Update Needed? |
|---|---|---|
| Odessa Frey | Goes to view the house in person; is let inside by a realtor who addresses her by a name that is not hers — and is not corrected, because some part of her recognizes it. External/situational ending: a door closes behind her; no return, no epilogue. | No — ECHO has no ledger; retired with the story. |

---

## 6. What It Plants {#SS-BRIEF-ECHO-§6}

**None.** Isolation constraint.

---

## 7. What It Pays {#SS-BRIEF-ECHO-§7}

**None.** Isolation constraint.

---

## 8. Thematic Complement {#SS-BRIEF-ECHO-§8}

**Theme:** What you've suppressed doesn't stay buried in the mind alone — it rehearses itself
until the world outside starts matching it, and the correction, when it comes, corrects *you*.

**Register:** Quiet / dissociative dread, non-linear, no combat or physical violence — the horror
is epistemic (not trusting your own memory).

**Adjacent stories in the same book or release window:** GRAVE, ALIAS, HEIRS — deliberately
**not** complementary; unrelated authors, casts, and prose styles by design.

**How it complements them:** Only thematically (the shared anthology prompt), never narratively.

**What would be duplicated if this story didn't exist:** The suppressed-memory/unreliable-
narrator reading of the theme — the others externalize concealment (soil, a false name, a locked
room); this one internalizes it entirely.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-ECHO-§9}

**Resolution mode:** External/situational resolution with significant cost (mixed) — the dream
converges on a real external event (the house viewing), never on "she comes to understand
herself" alone; the ending must be an event, not an epiphany, per the resolution-mode rule.

**Moral polarity:** Ambivalent (default) — no villain; the horror is her own mind and something
that may or may not be using it.

**Ending style:** Avalanche (default) — the dream/waking convergence accelerates in the final
pages; no epilogue after the realtor scene.

**Escalation curve shape:** Mundane insomnia and a recurring dream dismissed as stress → details
sharpen and repeat with small variations → she notices the real house for sale matches → she
visits despite herself → the realtor's wrong name is the final escalation, delivered with no
following scene.

**Event-type palette:** Repetition/dream-intrusion → recognition → convergence (2–3 types, no
combat).

**3–5 intertextual anchors:** 1. *House of Leaves* (structural instability mirroring psychological
instability) 2. Shirley Jackson's first-person unreliable narrators (*We Have Always Lived in the
Castle*) 3. *Jacob's Ladder* (dream/waking convergence horror, ambiguous ending withheld from the
reader).

**Subplot thread:** None — single-consciousness short story; no capacity or need for a subplot.

**Form device:** Non-linear intercutting of waking present and dream fragments, unmarked dialogue
folded into narration.

---

## 10. Entity Seeding Required {#SS-BRIEF-ECHO-§10}

| Entity | Type | In DB? | DB seed command / MCP tool |
|---|---|---|---|
| Odessa Frey | character | [ ] | `create_character` (originNodeSlug=echo) |
| The house two streets over | place | [ ] | `create_place` (originNodeSlug=echo) |

Run entity-mention scan after the draft to confirm coverage.

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] `docs/series/ANTHOLOGY.md` roster will be created/updated with this story's code and status
- [x] No character/arc ledger applicable — isolation constraint documented
- [x] No plant/payoff registry applicable — isolation constraint documented
- [x] No world-revelation sequencing applicable — ANTHOLOGY has no locked revelations to spoil
- [x] Entity seeding list (§10) complete — all entities exclusive to this story
- [x] Node bible (`docs/nodes/ECHO.md`) does NOT exist yet — brief precedes bible
