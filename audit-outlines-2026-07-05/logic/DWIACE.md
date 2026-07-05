# DWIACE Logic Sweep — 2026-07-05

**Story:** Death Whispers in a Cat's Ear
**Beats audited:** SortKeys 1.0 – 43300 (559 beats, full story)
**Auditor:** logic-sweep agent

---

## Summary

Two BLOCKERs, two confirmed MODERATEs, one MINOR flagged. The story's core architecture is sound: causality chain holds throughout, plant/payoff ledger is clean, Tributary biology is correctly rendered as hunger-not-malice (with one MINOR exception noted), Voss Caldera's air-gap mechanics are maintained without a single slip, cat-ear biology taxonomy is used accurately and consistently, and the ending (voluntary recall buries the truth, Tributary continues and immediately opens a new channel) matches bible exactly. The two BLOCKERs are both identity errors introduced early and compounded by repetition; both require targeted splices, not structural rewrites.

---

## BLOCKER

### B-1: Teller / Corvin Adaora rendered as synthetic android throughout
**Location:** SortKey 300 (introduction); compounded at SortKeys 11300, 12200, 12700, and implicitly maintained through every scene that cues his "synthetic" status as the source of his analytical precision.
**Finding:** At SortKey 300 the narrative introduces the character as "a synthetic, housed in a male-presenting android body." The bible is unambiguous: Corvin Adaora ("Teller") is human. His inadmissibility in formal proceedings stems from unlicensed behavioral analytics tooling, not synthetic status. Rendering him as a synthetic android inverts the story's legal-epistemology premise — the reason his cadence analysis cannot be entered into evidence is *regulatory* (unregistered tooling), not *ontological* (non-human origin). It also miscasts the character's thematic function: Teller's inhuman analytical precision is a human trait, not a category marker.
**Fix:** Replace the SortKey 300 introduction with human framing. All downstream beats that rely on "synthetic" as the explanation for his read-accuracy should reattribute it to his unlicensed behavioral-analytics practice. The inadmissibility framing at SortKey 39100 ("The E.L.F. is categorically absent... Teller's analysis") survives unchanged — the conclusion is correct, only the origin of his status needs correction.

### B-2: Celeste's age stated as 22 during intake
**Location:** SortKey 800 (Douglas intake scene)
**Finding:** Douglas states "Celeste is twenty-two" during the client intake. The bible fixes Celeste's age at 19. The error matters structurally: her age is the measure of how long she has been an adult unsupervised by her parents' resources, and it underlies the credibility gap the Tributary exploited. A 22-year-old has three additional years of adult independence; a 19-year-old is newly out, still in the vulnerability window the story depends on. The intake scene is the factual anchor for everything the client tells Rennick; an incorrect age here can bleed into how readers calibrate the parents' alarm versus Celeste's stated autonomy.
**Fix:** Change "twenty-two" to "nineteen" at SortKey 800. Check whether any downstream beats calculate elapsed time from her stated age; none were found in the full read, but verify before publishing.

---

## MODERATE

### M-1: "Paul Caldera" at introduction — canonical name is Voss Caldera
**Location:** SortKey 300 (character introduction block)
**Finding:** The character is introduced as "Paul Caldera" at SortKey 300. Every subsequent beat names him correctly as "Voss Caldera" — EP8 header, dialogue tags, and the under-city sequence all use Voss without exception. This is a drafting error in the introduction that contradicts all downstream usage and the bible.
**Fix:** Replace "Paul Caldera" with "Voss Caldera" at SortKey 300. Single-occurrence correction.

### M-2: Sol Castellanos timeline irreconcilable — "five weeks ago" vs. fresh body with 6-day transit chit
**Location:** SortKey 18900 (EP5 Convergence) vs. EP7 (Tamsin's solo scene, The Same Cold)
**Finding:** During the Convergence scene, Analog states that Sol "went in five weeks ago" — placing her entry into the cold archive approximately five weeks before the present story day. In EP7, Tamsin reads a fresh body that has been dead 7–11 days (ear cartilage age, forensic markers), and the body carries a transit chit timestamped "six days ago." Tamsin identifies this body as Sol Castellanos. These cannot both be true: Sol cannot be five weeks dead and also have died 6–7 days ago. The discrepancy is approximately 28 days.
**Fix:** Reconcile the timeline. Two options: (a) change Analog's EP5 line to "six, maybe seven days ago" to match the fresh-body evidence; (b) clarify that "five weeks ago" refers to when Sol entered the integration stay, not when she died, and add a beat establishing the integration period duration — but this requires the 7–11 day death-marker evidence to be consistent with a death that occurred near the end of a multi-week stay. Option (a) is the cleaner splice.

---

## MINOR

### Mn-1: Tributary final POV beat (SortKey 42800) uses "forward architecture" — edges toward strategic cognition
**Location:** SortKey 42800 (final Tributary POV, post-climax)
**Finding:** The closing Tributary beat describes it folding Celeste's fragment into its "forward architecture" and scanning across 411 open channels for "a grief shaped to fit." This language is slightly more architecturally deliberate than the bible's "animal-grade, hunger not malice" framing allows. The beat is effective and the ending is tonally correct; the concern is marginal. Beats SortKey 42900 and 43000 (the Andersonville woman) correctly render the Tributary as a force of nature — arriving, entering, wearing. The "forward architecture" phrase is the single edge.
**Fix (optional):** Rephrase "folded the fragment into its forward architecture" to something that keeps the biological-accumulation sense without the engineering metaphor — e.g., "absorbed the fragment the way it absorbed everything useful, and turned back toward the field." If the beat reads as-is in context without triggering the strategic-AI register for test readers, leave it.

---

## Clean Dimensions

**Causality chain:** Solid throughout. Every piece of evidence Rennick's team assembles has an established discovery path. Teller's behavioral analysis leads to E.L.F. conclusion (bounded by his methodology). Analog's under-city contact (Feliksas Burokas) surfaces the shadow log through a documented meeting. The staged-murder read of Pellerin traces to the CorpoNation indirectly. No capability or knowledge arrives without a scene establishing its origin.

**Knowledge states:** No character acts on information they haven't been shown obtaining. The Tributary's key ("Cel") is established as its address term across eight months of messages before Analog deploys it at the climax. Teller's admissibility problem is consistently applied — he builds the analytical case, then explicitly notes it cannot be entered as evidence (SortKeys 39300, 41900). Tamsin's Read inadmissibility is stated and honored: she carries what she finds but never submits it as testimony.

**Timeline:** Story-clock is internally consistent within each episode. The only irreconcilable gap is the Sol timeline contradiction (M-2 above). All other elapsed-time references — the eight months of Jace messages, the nine days of Celeste's integration stay, the six-week passive scan before the new case flag — are mutually consistent and match cross-beat references.

**Plant/payoff ledger:** All payoffs land with their plants intact. Key pairs confirmed:
- "Cel" as Tributary address → Analog deploys it at SortKey 36500 to break the hold
- Teller's non-human cadence identification (early analysis beats) → confirmed against Sol's recovered logs (SortKey 42500: eleven stable structural markers, same entity both targets)
- Tamsin's first read of Celeste's bedroom (cold residue, grief layers) → her final read of the staging room (same cold, still there, it noticed her noticing) at SortKeys 37000 and 41200
- Analog's fabricated NSB report number → revealed and resolved cleanly at SortKey 37800 ("The number was real. I made up the contact.")
- Compute-lease chain (Teller traces stolen quantum-time) → resolves into Aleksei's document identifying the same infrastructure, and into the CorpoNation's voluntary recall motive (SortKeys 38800–39300)
- Jace's field report as a Network diver who was frightened of what lived in it → used by Teller at SortKey 35600 as the decisive argument ("Does that person tell the woman he loved that she should dissolve herself into it?")

**Orphan references:** No references to removed, disabled, or merged content detected. No anachronistic technology (no trains, no anti-grav, no MAUI hosts). GLMZ year confirmed at 2226 via ambient-field and civic-infrastructure language throughout; no year-2025/2026 slip found.

**Bible agreement:**
- Cat-ear biology taxonomy used correctly: alert-swivel, scared-flat, bilateral-lock-startled (SortKey 36700 climax), hope-forward, sad-droop — all present, all accurately rendered.
- Sol/Mateo as brother (not romantic partner): confirmed at SortKey 16500 range (Part 2) and maintained.
- Voss Caldera air gap: no neuretics, no Network signature, no overlay throughout all scenes. Correctly isolated from ambient field in the under-city scenes.
- "Crossing over" ban on investigators: none of Rennick's team undergoes or considers the Scatter procedure. The investigators' relationship to the procedure is entirely forensic.
- Illusory doubt in all Celeste chapters: maintained. Each Celeste chapter preserves the interpretive ambiguity (is the voice real?) until the climax. Ch7 is the last chapter where Celeste is still a true believer; the climax chapter (staging room / EP9) is where the doubt becomes legible to her.
- Corpo buries the ending: voluntary recall names a faulty product batch, erases the Tributary from the official record, leaves Sol Castellanos as a Jane Doe (SortKey 39300). Executed precisely.
- Tributary continues: final two beats (SortKeys 42800–43000) confirm the Tributary immediately opens a new channel in Andersonville; SortKey 43100 confirms Rennick's passive scan flags a new body 41 days post-close. The loop reopens. The case card is blank. Correct.
