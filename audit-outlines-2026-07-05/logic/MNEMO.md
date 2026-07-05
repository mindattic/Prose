# MNEMO Logic Sweep — 2026-07-05

## Summary

28 enabled beats, all six dimensions checked. No BLOCKERs. Two MODERATEs (a timeline count discrepancy between Ch10 and SortKey 2600, and register-vocabulary leakage of "geometry" into Seto's and Amara's narrations). Three MINORs. The story's causality, plant/payoff ledger, orphan-reference hygiene, and pronoun discipline are clean.

---

## Findings

### BLOCKER

None.

---

### MODERATE

**M-1 — [SortKey 2400 vs. SortKey 2600] — Timeline count discrepancy: "two days ago" vs. "eight days"**

In **SortKey 2400** (Ch10, "What She Knows"), Seto messages Amara after she discovers the Orison archive:

> *I know. I found you on the list two days ago. I was waiting for you to find the piece you had to find yourself.*

In **SortKey 2600** (continuation of Ch11 server-cabinet scene, immediately following SortKey 2500), Seto messages her again:

> *I've had your name for eight days. I held it.*

These counts are mathematically consistent only if exactly 6 days separate Ch10 from SortKey 2600 — Seto found the name on Day 0, Ch10 is Day 2, the server cabinet is Day 8. The Ekow countdown provides an anchor: Ch11 is Day 3 of Ekow's 7-day clock (SortKey 2500: "three are already spent"), which gives the server cabinet a fixed position. For 6 days to separate Ch10 from the server cabinet, Ch10 must fall 3 days *before* the Ekow meeting — plausible given the sort-key order (2400 → 2450 → 2500-2600), but this gap is nowhere stated in the text. A reader doing the arithmetic will land on an apparent contradiction before they can resolve it.

**Minimal fix:** In SortKey 2400, change "two days ago" to "six days ago," which is also consistent with the server cabinet being Day 8 and Ch10 being Day 2 of an un-fixed count. Alternatively, in SortKey 2600 drop the explicit "eight days" and write something like "I've had your name for days. I held it." — which preserves the emotional weight without pinning a number.

---

**M-2 — [SortKey 1600, 2100, 2700] — Kyle-register vocabulary: "geometry" in Seto's and Amara's narrations**

The bible rule (memory file `feedback_kyle_register_bleed.md`): *Filing/arithmetic/parliament/gap/geometry are KYLE'S ONLY.*

Three occurrences of "geometry" appear in non-Kyle narration:

- **SortKey 1600** (Ch2, Seto POV): *"What he'd been tracking had a geometry."*
- **SortKey 2100** (Ch7, Amara POV): *"she performed the small social geometry of morning without spending anything on it."*
- **SortKey 2700** ("The Move," Amara POV): *"She knew the geometry of it precisely."*

The Seto instance is the most defensible (a courier spatially mapping corridors might naturally reach for geometric language), but all three violate the per-protagonist register boundary. The two Amara instances are purely metaphorical and therefore most clearly in Kyle's register.

**Minimal fix:**
- SortKey 1600: *"What he'd been tracking had a shape."* (the beat already uses "shape" in the surrounding sentences, so this is a natural substitution)
- SortKey 2100: *"she performed the small social choreography of morning"* or simply *"she did the morning without spending anything on it."*
- SortKey 2700: *"She knew the shape of it precisely."*

---

### MINOR

**m-1 — [SortKey 2500] — "Seam crossing" location error: should be "rooftop"**

In **SortKey 2500** (Ch11), Seto thinks:

> *Four days. Ekow Ato had given him seven at the Seam crossing — walk away from the Orison work, close out the inquiry — and three are already spent.*

The actual scene (SortKey 2450, "Minimum Footprint") takes place on a rooftop ("The rooftop access door had a bad latch — Seto had clocked it on the way up"). SortKey 3500 ("Thursday Morning") correctly recalls the location: *"the rooftop, the machete at rest, the handkerchief folded in quarters."*

The SortKey 2500 reference is internally inconsistent with both the scene and the later recall.

**Minimal fix:** *"...given him seven on the rooftop..."*

---

**m-2 — [SortKey 3600] — Kyle-register vocabulary: "gap" used metaphorically by Seto**

> *Three to four weeks was not a gap. It was a decision.*

"Gap" in the sense of a tolerable interval-vs.-failure distinction is Kyle's register. Seto's analytic voice runs on routes, borders, channels, and timing — not the gap/arithmetic vocabulary.

**Minimal fix:** *"Three to four weeks was not routine. It was a decision."* or *"Three to four weeks was not a window. It was a message."*

---

**m-3 — [SortKey 4000] — Seto's knowledge of Nuru's rescheduled calibration window not grounded**

In **SortKey 4000** ("Different Frequency"), Seto sends:

> *Facility C-9. Tuesday, 9AM. She's on the schedule again. Rescheduled from three weeks ago — they moved her window twice and didn't tell her why.*

Seto's access to Nuru's active calibration schedule is not established on the page. He has: the Cellvault server manifest (SortKey 2500–2600), which showed "current status: active" and an 18-month calibration schedule; his courier knowledge of Facility C-9; and the relay infrastructure surveillance he conducted throughout the story. These together make it plausible that he monitors scheduling traffic through relay logs, but the means are implied, not stated. No fix required if the ambiguity is intentional; a one-clause grounding would close it cleanly (e.g., *"Seto, who had been watching the C-9 booking traffic through the Zone 6 relay..."*).

---

## Clean dimensions

**1. Causality chain** — CLEAN. All capabilities are grounded before they are used: the bleed mechanism (carrier-band leak from Batch 44-C hardware), Seto's relay-trace skill (eleven years of gray-zone route work), Amara's archive access (the extended access arrangement offered in "Grooming"), the Orison system watching for the query that would expose it (established in "Off Timing"). The attempted reverse bleed in "The Procedure" is explicitly marked as having no evidential basis ("He had nothing to base the disbelief on"), which is itself causally honest — the act is presented as will against mechanism, not confirmed capability.

**2. Seto pronouns** — CLEAN. All 28 beats use he/him for Seto without exception. Zero she/her violations.

**3. Plant/payoff ledger** — CLEAN. All major plants are closed:
- Bone-handle straight razor (SortKey 1900) → paid off in "The Turn" (SortKey 3250), open blade, four seconds, eyes never leaving her.
- Nuru's "Clear. -N." sign-off routine (SortKey 1600/1800) → arrives late in SortKey 2500; absent the night before SortKey 4100.
- Facility C-9 inside the Orison campus boundary (SortKey 1800) → paid off when Nuru appears on the manifest (SortKey 2500).
- The reporter's background search that synced to Orison (SortKey 1500) → paid off as the mechanism that exposes her to Orison's desk before she files anything.
- Phase II (SortKey 2400) → paid off in the announcement (SortKey 4000).
- Amara's bleed-acquired courier skills — specifically her learned ability to move data without a legible trace (SortKey 2400: "Her hands knew the sequence") → paid off when she uses Seto's learned method to transfer the archive evidence.
- Ekow's 7-day window (SortKey 2450) → paid off in "Thursday Morning" (SortKey 3500); Orison's compliance suspension reaches Seto first and makes Ekow's deadline moot.
- "Write it down. On paper. Not the system." (SortKey 3100) → paid off when Seto writes the account on physical paper at Doru's relay shop (SortKey 3400).

**4. Orphan references** — CLEAN. No beat is labeled or titled "Second Suitor" as a standalone chapter. No "Managed Liability" beat survives as Amara POV; the Ch12 slot (SortKey 2600) is Seto POV (the Zone 7 read-through continuation). No references to retired canon were found.

**5. GLMZ year** — CLEAN. All in-prose present-tense year references are 2226. The earliest historical anchor is October 2222 (Amara's Batch 44-C enrollment), the Daud follow-up is explicitly "2226," and no 2025/2026 dates appear anywhere in the beats.

**6. Ciro characterization** — CLEAN. Ciro reads throughout as calibrated, not cartoonish. The razor is framed as maintenance ritual, not menace, in both appearances. His sincerity is presented as genuine: *"He said it like he meant it kindly, which was the worst part, because she believed that he did."* He files forms and enters contacts into databases; he does not make threats. His emptiness is the horror, not his malice.

**7. Batch 44-C numbers and Phase II** — CLEAN. The 847-recipient count is stated three times (SortKey 2400, 2500, 3800) consistently. Phase II as full GLMZ metro rollout is confirmed in the announcement text at SortKey 4000.

**8. Bleed mechanism consistency** — CLEAN. One intrusion per regular beat throughout. The mass simultaneous transfer in "The Procedure" (SortKey 3800) is exceptional by design and clearly marked as such. The one-directional / read-only constraint is explicitly stated and then deliberately tested by Seto's attempt — the story does not quietly violate the rule; it confronts it as the act's dramatic core.
