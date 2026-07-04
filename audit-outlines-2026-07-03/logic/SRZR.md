# SRZR — Logic & Continuity Audit

**Node:** Steppin' Razor, slug `steppin-razor-019ef7be`, id `019EF7BE-B2CA-70A1-BAB6-E807977A6640`
**Scope:** All 31 enabled beats, full text, SortKey order. Cross-checked against `docs/nodes/SRZR.md`.
**Date:** 2026-07-03. Report-only — no DB rows, code, or docs were modified.

## Overall Verdict: PASS WITH MODERATE ISSUES

The throughline is sound: Act 1 (the forge, beats 4860–4869 + 4419) is genuinely and completely
clean of the camel-man, the camel's escalation across its five/six appearances is coherent and
matches the bible's stated arc, the bounty/standing-order thread is set up and closed on its own
established terms (the Consensus-credential checkpoint the gray-zone order can't buy its way past),
and the market-well-cliffhanger material was excised cleanly. However, there are one BLOCKER-grade
dangling causal reference (the gooseberries paper-cone at beat 4420), one real continuity error (the
dog reappearing in beat 4767 after being narratively replaced by the cat at beat 4420), a stale
"Axiom" reference sitting in the Synopsis metadata of beat 4775 (reader-facing prose is clean), and
an unestablished "storeroom" callback at beat 4772. None of these break the read for a reader moving
beat-to-beat at normal speed, but they are all real, all cheap to fix, and worth closing before the
next export pass. Note: several apparent oddities (garbled "�" characters, "F50,000" instead of
"Φ50,000", "V�" for "Võ") are **sqlcmd console codepage artifacts only** — verified against raw
Unicode extraction, the underlying DB text is correct (Φ symbol and Võ diacritic both present and
correct). These are not flagged as findings.

---

## (a) Causality — throughline and camel-man ledger

**Act 1 cleanliness — CONFIRMED CLEAN.** Beats 4860, 4861, 4862, 4863, 4864, 4865, 4866, 4867, 4868,
4869 (the pre-Joliet forge) and 4419 (the bar, "Steppin Razor" / opening-image) were read in full.
None contain any reference to the camel, "the man on the camel," or any 5D-contact material. This
is exactly the bible's load-bearing requirement (§2: "keeping him out of the first 10 chapters is
load-bearing") and it holds. Camel-man's first appearance is beat **4420** ("The dead rails,"
inciting-incident), matching the task's expectation exactly.

**Escalation from 4420 onward — COHERENT, no severity/knowledge jumps:**
| Beat | Number | What's new |
|---|---|---|
| 4420 | inciting | Wordless presence; wrongness registers as smell/body-sense before thought. |
| 4421 | rising | Speech register established (it/they/we, non-linear); "we have been watching your progress" — explains later specificity. |
| 4422 | midpoint-turn | Gets under her guard exactly once — emotional escalation, not informational. |
| 4423 | break-into-two | First plot-critical reveal: not the Lure; the "hole on both sides" metaphor; points northeast. |
| 4876 | (Ch12 extension) | Texture only ("the Arrangement," "symptoms are useful") — correctly adds no new canon fact, per bible's own constraint. |
| 4772 | all-is-lost | Second appearance, narrows a vague direction into an exact address (eastern colonnade, <1km) — consistent scaling from "watching her progress." |

No BLOCKER here. This section **PASSES CLEAN**.

**Finding A1 — BLOCKER.** Beat **4420** (Number 4420, SortKey 100) contains: *"She had the paper
cone in her jacket pocket still - three gooseberries, the ones she'd set down before the man
folded."* This references an action ("set down before the man folded," i.e., during the bar fight)
that does not exist anywhere in beat **4419** (Number 4419, SortKey 50) — the immediately preceding
beat, which is the only beat depicting that scene. 4419's text describes the drink, the bottle, and
the man being hit; no gooseberries, paper cone, or vendor purchase appears. The bible's own changelog
(§12, SRZR-US-7) confirms a "purple gooseberry thread added beats 1–2" was intended — it appears to
have been dropped from 4419's current prose during a later edit pass. **Fix:** add one sentence to
4419 establishing Sasha buying/holding a paper cone of gooseberries before or during the bar scene
(e.g., from a vendor outside, or as a bar snack) so 4420's callback lands on planted ground.

**Finding A2 — MODERATE.** Beat **4873** (SortKey 525) contains the internal line *"Dawit said run.
The stranger said run."* Dawit (beat 4768) never tells her to run — he says "Nothing... That's why
I'm giving it to you," which is closer to a confession than a warning. Only the anonymous dead-drop
note (beat 4770) explicitly says "Run." Attributing "run" to Dawit is a misstatement of what he
actually said on the page. **Fix:** rephrase to something like *"Dawit gave her the data instead of
using it himself. The stranger said run."* — or cut the Dawit clause.

## (b) Knowledge states / plant-payoff ledger

**Pair 1 — "warm-ground" plant → dead-drop slate warmth.**
- **Plant, beat 4863 (SortKey 43.75):** *"Then, for less than a second, the patch under her hand
  went warm — blood-warm, wrong, a heat with no business in a cut field in the dark — and when she
  spread her fingers it was cold wet dirt again... she had four hours and a shrinking list of open
  directions; a patch of ground that ran warm and then didn't was not on it, so she left it there and
  walked."*
- **Payoff, beat 4770 (SortKey 450):** *"Inside, where there should have been a relay, there was a
  slate the size of a playing card, screen-down, waiting... She picked it up. It was warm. It had
  been listening... The warmth was the part that got under her skin."*
- **Assessment: PRESENT BUT THIN — flag.** Both beats land the exact same sensory beat (unexplained
  warmth where the world should be cold/inert), and thematically both point at "something is aware
  and reaching." But the text never explicitly connects them — Sasha does not recall the field, and
  nothing in 4770 signals "this warmth is the same warmth as before." Read cold, it registers as
  motif repetition rather than a marked plant-and-payoff; a reader would need to actively notice the
  echo rather than have it delivered. Given the story's own house style (no pseudo-profound
  underlining, Sasha doesn't editorialize), this may be an intentional restraint rather than an
  oversight — but as asked, flagging: the correspondence is real but not confirmed on the page as
  deliberate. **Minimal fix (optional, preserves restraint):** one clause in 4770, e.g. *"the same
  wrongness the cut field had given her and taken back,"* would confirm the link without over-explaining.

**Pair 2 — "doubled crow" plant → wells' doubled note.**
- **Plant, beat 4868 (SortKey 49.8047):** *"A crow lifted off a fence line and resettled. It called
  once — and then called again, a half-beat late, identical, from the same fence line and the same
  throat, though only the one bird had moved and it had already gone still... a sound that came twice
  was not a box on the map."*
- **Payoff, beat 4769 (SortKey 400):** *"A dead well rings backward - 19 Hz, a flat doubled note, the
  sound of a thing that has stopped reaching and gone hollow."*
- **Assessment: PRESENT, THIN, LIKELY DELIBERATE WORD-ECHO — flag as subtle.** The verbal repetition
  of "doubled" is a clean, specific echo (not a generic word), and both instances mark an early-warning
  omen of the schism/well phenomenon intruding on ordinary reality before Sasha has named it. This
  reads as authorial intent (word choice is too specific to be coincidence) but, like Pair 1, is never
  confirmed in Sasha's own interiority — she doesn't connect the crow to the well. **Minimal fix
  (optional):** a half-line in 4769 tying the well's "doubled note" back to the crow's double-call
  would convert a coincidental-looking echo into a confirmed plant, at the cost of slightly more
  explicit narration than the current deadpan style prefers. Leave-as-is is defensible; flagging per
  instructions.

## (c) Bounty / standing-order thread

**Establishment:** A local, non-corporate, gray-zone-funded bounty ("the standing order," beat 4864:
*"There's a standing order... North. Two incidents combined. Not corporate"*) accretes from the
Kankakee documentation-man incident (4860) and the Beecher discourage-job fallout (4862), compounds
through two hunter escalations (4866, 4867), and is explicitly characterized by its own operating
rule: *"it never had to answer to anyone. It also meant it stopped exactly where that kind of money
stopped being useful"* (4869).

**Closure:** Beat 4869 (SortKey 49.9023) delivers the closure using that exact pre-established rule:
*"North of Joliet the freight interchange ran a Consensus credential check for anyone continuing into
the city proper — a checkpoint that took a registered identity, not a rumor and a price. She had
neither. The order's hunters didn't either... for the first time in three years the thing chasing her
and the thing she was walking toward wanted two different kinds of proof of who she was, and neither
of them had it."*

**Assessment: CLEAN PASS.** This is a well-built closure — no last-minute new rule is introduced; the
checkpoint closure is a direct, logical consequence of the rule stated two paragraphs earlier
(gray-money bounties can't cross identity-verified infrastructure). It also matches the bible's own
open-design recommendation (§10.3: "let it die — the GLMZ swallows the old life whole") — the order
is never mentioned again after 4869, which is the correct execution of that recommendation rather than
an oversight. **No fix needed.**

## (d) Companion-animal seam (dog → cat)

Beats 4419 and its neighbors were read closely, plus the transition point and beyond.

- **4419** (bar scene): *"The dog sat at her feet."* — dog present, as it has been continuously since
  beat 4860.
- **4420** (camel first appears): dog is **never mentioned again**. In its place: *"On the far rail, a
  gray cat sat with its paws folded under its chest and watched her. Not the camel. Her."*
- **4421–4876:** cat continues (implied continuity via the animals-drawn-to-Sasha motif, e.g. 4770's
  "the cat at her heel" language pattern established later).
- **No line anywhere in 4419, 4420, or their neighbors explains, narrates, or even gestures at what
  happened to the dog.** There is no death, no parting, no "it wouldn't cross the rail bed," nothing.
  The swap is a hard cut with zero textual acknowledgment.

**Finding D1 — MODERATE (the seam itself).** The transition is unexplained anywhere on the page.
Given the bible's own canon (§4: "real animals are drawn to Sasha... it is not charming; it is proof
she is marked... keep it understated; never explain it on the page"), a fully silent swap is arguably
*consistent* with the house style of never explaining the animal magnetism — but a total substitution
of one companion for another with zero acknowledgment reads as a gap, not a choice, because nothing
marks it as a deliberate swap versus a continuity slip. **Minimal fix:** one clause in 4420, e.g. *"The
dog had stopped at the rail line's edge, unwilling to go further, and had not followed"* (placed
before the camel/cat appear) — this "understates" per canon while still confirming the swap was
authorial, not an error.

**Finding D2 — BLOCKER (a real contradiction, found while checking D1).** The dog **reappears** in
beat **4767** (Number 4767, SortKey 300, "Entry to the Glooms," Ch13) — well after the cat has already
been established as her companion (4420, 4870, 4769, 4773 all place the cat at her side in the
intervening beats): *"The dog sat down when the sound hit. Sasha kept moving."* and later in the same
beat: *"The dog's nails on the tile."* This directly contradicts the established swap — by Ch13 the
dog should be gone/replaced, yet it is walking through the Glooms with her as if the cat swap never
happened. This is very likely a drafting leftover (the word "dog" surviving from an earlier version
of this beat written before the animal-swap was decided) rather than an intentional continuity beat.
**Fix:** replace "dog" with "cat" in both instances in beat 4767 (Number 4767).

**Finding D3 — MINOR/MODERATE (adjacent animal-continuity gap).** A pigeon/bird is referenced as
accompanying Sasha at the end of beat 4768 (*"the bird on her shoulder, the cat at her heel"*) and its
departure is noted at the start of beat 4769 (*"The pigeon had gone, sometime in the last block."*),
but no beat ever shows a pigeon adopting her — the only pigeons on the page belong to an unrelated
woman feeding them in the station (4767). **Fix:** either add a half-sentence in 4767 showing one
pigeon breaking off to follow Sasha (consistent with the animals-drawn-to-her canon), or cut the "bird
on her shoulder" / "pigeon had gone" pair as an unintended addition.

## (e) Market-well cut — CLEAN

All 31 beats were scanned for "fourth well," "market," or any residual cliffhanger material. No
matches beyond legitimate, unrelated uses ("gray-market neuretics," "the fourth [Halcyon operative]
had her"). The colonnade consistently and only ever describes **three** staged wells (4773, 4874,
4774, 4775). **No orphaned market-well material found. Clean.**

## (f) Timeline + Sasha's knowledge of Halcyon/OBERON

**Timeline reconstruction:** Kankakee incident → same-night ambush (4861) → Beecher job taken because
of Kankakee fallout (4862, explicitly "because") → warm-ground beat while fleeing Beecher (4863) →
Wilmington stay (~4 weeks, 4864) → burns it (4865) → 6 weeks to first hunter in Braidwood (4866) → 2
more weeks to price upgrade / second hunter fight (4867) → map/decision beat (4868) → arrival in
Joliet (4869) → bar (4419) → camel/Ch12 (4420–4876) → city (4767 onward). Total elapsed time from
Kankakee to Joliet arrival is on the order of 3+ months, consistent with the supply woman's "three
months ago" reference to the order's origin (4867) and with "three years" as her total gray-zone
operating history (4868, consistent with the bible's 16→19 age math). **No timeline impossibilities
found.**

**Beat 4771 (OBERON):** *"Under the seal, in print small enough to read like an afterthought, sat a
chartered officer of record — one name, no title a person would use. OBERON. A machine's name where a
signature belonged. She filed it without comment and kept moving."* This is read directly off a
physical credential she has just taken off a body she killed in that same beat. **Fully grounded —
no advance knowledge, no overreach.** She does not conclude anything about OBERON beyond noting the
oddity of a machine's name on a signature line — appropriately restrained per the bible's L0
theme-guard (never confirm the cabal identity). **Clean.**

**Beat 4775 ("Halcyon fix," the receiver's problem):** The prose Text is clean and correctly grounded
— everything she "knows" (three wells, the depths, the 72 Hz pushback, the Halcyon comms rig, the
Halcyon credential) was directly acquired on-page in beats 4771–4774. **However:**

**Finding F1 — MODERATE, metadata-only.** The **Synopsis** field of beat 4775 (Number 4775) reads:
*"She knows where the drills are. She knows they are live. She knows **Axiom** is running them under
Consensus cover."* This is a stale reference to the pre-redesign antagonist. The bible is explicit
(§3): *"Halcyon Combine (REPLACES Axiom in SRZR; 2026-06-28)"* — Axiom was retired from this strand
over a week before this audit. The delivered prose **Text** of 4775 never uses "Axiom" — it correctly
says "Halcyon" throughout — so **no reader-facing defect exists**. But the Synopsis field is
authoring/production metadata that feeds the beat-generation context chain (per project workflow,
Synopsis is part of what's assembled into `BeatContext`); if this beat is ever regenerated or expanded
using that stale Synopsis as context, "Axiom" could leak back into the prose. **Fix:** update the
Synopsis field to say "Halcyon" instead of "Axiom" (a metadata edit, not a prose edit — out of scope
for this report-only audit, but worth a follow-up ticket).

## (g) Orphan references (beyond market-well and camel-man)

- **Finding G1 — MODERATE.** Beat **4772** (Number 4772, SortKey 550) contains the internal-monologue
  fragment *"Joliet. The storeroom. You told yourself you were stopping."* No beat anywhere in the 31
  mentions a "storeroom." Her Joliet arrival (4869) describes a **bunkhouse**, not a storeroom. This
  reads as a leftover reference to either a cut location or a word substituted in an earlier draft.
  **Fix:** change "storeroom" to "bunkhouse" (or whatever location is intended) for internal
  consistency, or restore the storeroom scene if one was meant to exist.
- **Findings A2, D2, D3, F1 above** are also orphan-adjacent and are cross-referenced here rather than
  repeated.
- **No leftover Axiom/Sigma-program/severable-crossover (Elias, Sparrow) contamination found in the
  prose Text of any beat.** Checked explicitly — clean.
- **"Camel jockey" slur** — checked explicitly across all 31 beats — **zero occurrences**. The
  permanent ban (§2, §6.7) is fully respected. Clean.

## (h) Bible agreement (other canon checks)

- **Weapons/handedness** — Problem Solver (right), Trouble Maker (left), cross-draw: verified
  consistent across every combat beat (4866, 4867, 4770 carry, 4774). **Clean.**
- **Never corpo-registered / gray-zone born** — beat 4774's "Recovery" passage explicitly states "no
  registration, no legal address" — consistent with SS-A20 canon, no Sigma-program leftovers found.
  **Clean.**
- **Neuretics never closed** — correctly established in 4860 ("the compute that was supposed to seal
  at twelve... Hers hadn't") and consistently referenced (4422 "never-closed," 4876 "running warm").
  **Clean.**
- **"Not the Lure" exchange** (4423) — matches the bible's locked anchor dialogue near-verbatim.
  **Clean.**
- **Halcyon/OBERON framing** — correctly withholds confirmation of whether OBERON/Halcyon is "the
  cabal," per the L0 theme-guard. **Clean** (see F1 for the one metadata caveat).
- **Φ currency symbol / Võ diacritic** — verified via raw Unicode extraction to be correct in the DB
  (console-encoding artifacts only in the initial sqlcmd pull). **Clean, no finding.**
- **Cross-strand Glim/Homewater insertion (beat 4975)** — matches the bible's §7 table entry ("Between
  Ch 21–22... beat ID `019f247a`") in content and placement; correctly a *different* entity from the
  camel-man, no identity confusion on the page. **Clean.**

---

## Findings Summary Table

| # | Section | Severity | SortKey / Number | Finding | Suggested Fix |
|---|---|---|---|---|---|
| A1 | Causality | **BLOCKER** | 100 / 4420 | Gooseberry paper-cone callback has no antecedent in 4419 | Add one sentence to 4419 establishing the gooseberries |
| D2 | Companion-animal | **BLOCKER** | 300 / 4767 | Dog reappears twice after being replaced by the cat at 4420 | Replace "dog" → "cat" (2 instances) in 4767 |
| A2 | Causality | MODERATE | 525 / 4873 | "Dawit said run" misattributes dialogue Dawit never said | Rephrase to reflect what Dawit actually said (4768) |
| D1 | Companion-animal | MODERATE | 100 / 4420 | Dog→cat swap wholly unexplained on the page | One understated clause noting the dog didn't follow |
| D3 | Companion-animal | MINOR/MODERATE | 350–400 / 4768–4769 | Unestablished pigeon appears then departs | Add pigeon-adopts-her beat, or cut both references |
| F1 | Timeline/knowledge | MODERATE (metadata only) | 700 / 4775 | Synopsis field says "Axiom," prose correctly says "Halcyon" | Update Synopsis field to match retired-Axiom canon |
| G1 | Orphan references | MODERATE | 550 / 4772 | "The storeroom" has no textual referent (Joliet lodging is a bunkhouse) | Change to "bunkhouse" or restore the intended scene |
| B1 | Plant/payoff | MINOR (flag only) | 43.75→450 / 4863→4770 | Warm-ground plant and slate-warmth payoff never explicitly linked on the page | Optional one-clause echo in 4770 |
| B2 | Plant/payoff | MINOR (flag only) | 49.8→400 / 4868→4769 | Doubled-crow and doubled-well-note share the word but no explicit callback | Optional half-line tying the two in 4769 |

**Clean / no findings:** (c) bounty-order closure, (e) market-well cut, camel-man escalation logic,
camel-man Act-1 exclusion, weapons/handedness, gray-zone-origin canon, neuretics canon, Lure
distinction, slur ban, Φ/Võ encoding, Glim cross-strand insertion, OBERON grounding at 4771.
