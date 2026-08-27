---
codex: 1
project: Prose
layer: planning
code: ALIAS
title: Collection
universe: ANTHOLOGY
updated: 2026-08-26
---

# Story Brief: ALIAS — Collection {#SS-BRIEF-ALIAS}

> **This brief is mandatory before creating a node bible or any DB records.**
> Part of the ANTHOLOGY isolated-anthology experiment (docs/series/ANTHOLOGY.md). The shared
> "prompt" across all four ANTHOLOGY stories is the same one-line anthology theme; each is written
> through a different fictional Author-filter that refracts that theme into an unrelated story.
> This one is written entirely as **Nadia Voss**, not as "Prose's default voice." Shares no
> characters, places, entities, or plot facts with any other ANTHOLOGY story.

---

## 1. Series Position {#SS-BRIEF-ALIAS-§1}

**Universe:** ANTHOLOGY

**Story type:** Standalone — second of four isolated submission-target short stories
(2,000–6,000 words, target: Easton Tales Publishing's "Enshrouded: A Horror Anthology," theme:
something buried, hidden, or concealed, and the darkness that follows when revealed).

**Book(s) this story serves:** None.

**Author Persona:**
- **Name:** Renee Kessler
- **Age / sex:** 43, female
- **Location:** Philadelphia, Pennsylvania
- **Upbringing/background:** A decade as an insurance-fraud investigator before a quiet exit from
  the field; divorced, lives alone; trained for years to write incident reports that would hold up
  under cross-examination — no adjectives, no speculation, only what's verifiable.
- **Causal link (bio → style):** Report-writing discipline produces short declarative sentences
  and a deep distrust of interior monologue (unverifiable, therefore untrustworthy); the fraud
  beat — where every case was someone's carefully built false identity — is the direct source of
  the premise: concealment as a documented, procedural act with a case file, not a metaphor.
- **Style contract:** Close third person, present tense. Sentences average 8–12 words, almost
  never longer. No semicolons, ever. Paragraphs are 1–3 sentences, heavy white space. Dialogue is
  frequent and load-bearing (she trusts recorded statements over description); tags are bare —
  "she said," "he said," nothing else. No em-dashes, no italics, no digressive imagery.

**Approximate in-universe timing:** N/A — contemporary standalone.

---

## 2. Arc Contribution {#SS-BRIEF-ALIAS-§2}

**None — by design.** Isolation constraint: no shared ANTHOLOGY arcs exist. Value: the anthology
theme read as identity-concealment — a person's constructed false name unravels not through
outside exposure but through a quiet, patient collection of a debt she thought she'd buried with
the old name.

---

## 3. Prerequisites {#SS-BRIEF-ALIAS-§3}

**None.** Fully standalone.

---

## 4. Character Entry States {#SS-BRIEF-ALIAS-§4}

| Character | Entry State |
|---|---|
| Iris Calder (assumed name) | Introduced fresh: mid-30s, has lived under this name for six years, believes her old life is sealed off and unreachable. |
| Thomas Reyes | Introduced fresh: quiet, patient, moves into her building; not there to expose her — there to collect on something from her old identity. |

---

## 5. Character Exit States {#SS-BRIEF-ALIAS-§5}

| Character | Exit State | Ledger Update Needed? |
|---|---|---|
| Iris Calder | Pays the debt in a way that isn't money — the story ends with her walking out of her own apartment under a third name, the "Iris Calder" identity discarded the way the first one was. External/situational resolution, no internal peace. | No — ALIAS has no ledger; retired with the story. |
| Thomas Reyes | Departs having collected what he came for; his own nature/agency is never fully explained (ambivalent — human enforcer or something else, left open). | No. |

---

## 6. What It Plants {#SS-BRIEF-ALIAS-§6}

**None.** Isolation constraint.

---

## 7. What It Pays {#SS-BRIEF-ALIAS-§7}

**None.** Isolation constraint.

---

## 8. Thematic Complement {#SS-BRIEF-ALIAS-§8}

**Theme:** Concealment as a procedural, documented act — every false identity is a case file
somewhere, and case files get closed by someone eventually, on their schedule, not yours.

**Register:** Quiet dread, procedural/noir-adjacent, never overtly violent.

**Adjacent stories in the same book or release window:** GRAVE, HEIRS, ECHO — deliberately
**not** complementary; unrelated authors, casts, and prose styles by design.

**How it complements them:** Only thematically (the shared anthology prompt), never narratively.

**What would be duplicated if this story didn't exist:** The identity-concealment reading of the
theme — the others read it as soil, family, and memory, not a constructed name.

---

## 9. Structural Blueprint Seed {#SS-BRIEF-ALIAS-§9}

**Resolution mode:** External/situational resolution — a transaction is completed; nothing is
internally resolved or understood.

**Moral polarity:** Ambivalent (default) — Iris isn't purely a victim; the debt is real.

**Ending style:** Avalanche (default) — the reveal of what Thomas actually wants accelerates
fast once it starts; no epilogue.

**Escalation curve shape:** Mundane wariness of a new, too-attentive neighbor → small tells that
he knows things he shouldn't → a direct, calm confrontation naming her real name → the terms of
the "collection" are stated plainly → she pays and leaves.

**Event-type palette:** Surveillance/dread → confrontation → transaction (2–3 types, no combat).

**3–5 intertextual anchors:** 1. Patricia Highsmith's Ripley novels (calm, procedural menace)
2. *No Country for Old Men*'s Anton Chigurh (patient, terms-based antagonist logic) 3. Case-file
procedural true-crime writing (flat affect describing extraordinary things).

**Subplot thread:** None — short-story length, single thread.

**Form device:** None — straight close-third present tense, no frame device.

---

## 10. Entity Seeding Required {#SS-BRIEF-ALIAS-§10}

| Entity | Type | In DB? | DB seed command / MCP tool |
|---|---|---|---|
| Iris Calder | character | [ ] | `create_character` (originNodeSlug=alias) |
| Thomas Reyes | character | [ ] | `create_character` (originNodeSlug=alias) |

Run entity-mention scan after the draft to confirm coverage.

---

## Checklist Before Proceeding

- [x] All 10 sections filled
- [x] `docs/series/ANTHOLOGY.md` roster will be created/updated with this story's code and status
- [x] No character/arc ledger applicable — isolation constraint documented
- [x] No plant/payoff registry applicable — isolation constraint documented
- [x] No world-revelation sequencing applicable — ANTHOLOGY has no locked revelations to spoil
- [x] Entity seeding list (§10) complete — all entities exclusive to this story
- [x] Node bible (`docs/nodes/ALIAS.md`) does NOT exist yet — brief precedes bible
