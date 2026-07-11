# Logic Sweep Report — Bushido Coda (BCODA)

**Date:** 2026-07-10
**Auditor:** Logic Sweep v1 (SS-A44)
**Story:** Bushido Coda — `docs/nodes/BCODA.md`
**Beats swept:** 435 across 25 chapters
**Verdict:** HAS-BLOCKERS

---

## 1. Overall Verdict

**HAS-BLOCKERS**

Two blockers require resolution before the story is auditable-clean. Both are identity-tracking failures: a character who arrives with the wrong first name, and a narrator who knows a number he should not yet know. Neither can be deferred. All nine MODERATEs are actionable; eleven MINORs are documented below.

---

## 2. Findings by Severity

### BLOCKERS (2)

**B-01** | Dimension: knowledge-states | SortKey: 18000
> Kyle's interiority uses the specific number "sixty-four" for his Persona count before Nadia Park establishes that number in the safe-house scene (beat 26000). The narrator cannot know this figure earlier than the scene in which it is spoken aloud. Every instance of "sixty-four" in Kyle's POV at SortKey < 26000 must be replaced with a formulation that acknowledges the count as unknown or approximate.

**B-02** | Dimension: causality-chain | SortKey: 1
> Ria's brother is introduced as "Kofi Mensah" in beats 17000 and 20000, but is documented and addressed by name as "Tomas Osei-Mensah" from beat 18000 (entity posting) through the end of the chapter. Two different first names for one character make his actions untrackable across Able's records, the entity's scheduling data, and the courier's own testimony. One canonical name must be chosen and the other purged from every beat where he is named.

---

### MODERATEs (9)

**M-01** | Dimension: bible-agreement | SortKey: 0
> The BCODA node bible (`docs/nodes/BCODA.md`) uses "Seo" for the fabricated mentor in its planning and structural sections (lines 127, 143, 163, 246, 635, 668) and "Saito" in its prose-adjacent sections (lines 896–1270). The audit task key facts supply "Seo." Prose consistently uses "Saito" across every beat where the mentor is named. One canonical name must be chosen and all occurrences of the other purged from the bible. This is a docs defect, not a prose defect — the name "Seo" in the bible's planning sections may be a planning-era holdover. If "Saito" is the prose-canonical name, the bible planning sections must be updated to match.

**M-02** | Dimension: causality-chain | SortKey: 2
> The courier named Ria carries three different surnames across the chapter: Mensah (beat 17000), Osei-Mensah (beat 25000), and Okonjo (beat 62000). Her identity is untraceable across Able's records, the entity's scheduling data, and the first-act introduction. One canonical surname must be chosen; the other two corrected in every beat where she is addressed or referenced.

**M-03** | Dimension: causality-chain | SortKey: 3
> Gantry is assigned she/her pronouns in beat 57000 and consistently she/her in beats 140000 and 152000, but beat 111000 switches to he/him for the same character. This is a pronoun slip, not an established character variation. Beat 111000 must be corrected to she/her.

**M-04** | Dimension: bible-agreement | SortKey: 4
> In beat 154000, the Machine God uses lowercase first-person "i" repeatedly across an extended conversation — "i have a question. i have had it since the carousel. i did not have a field for it. i have built one." — which conflicts with SS-LAW-22's prohibition on first-person pronoun use for this entity. The narrative frames it as unprecedented, but SS-LAW-22 has no exception clause. Either the law must be amended to permit a formal exception for the Ghost Period LOG GAP conversation (with a locked rationale in the bible), or the beat must be reworked so the Machine God conveys the same meaning without first-person.

**M-05** | Dimension: bible-agreement | SortKey: 85
> The entity sends "CORRIDOR AUDIT COMPLETE. THE CHOICE AT 6.2% IS LOGGED." — an ALL CAPS relay message but with no contract scope, no fee, and no addendum field. Kyle's own narration confirms: "No contract number. No job description. No gratuity, no addendum, no abort signal." The absence of these fields is the point, but the message still arrives via the relay channel, which requires scope/fee/addendum by established format rules. If the missing fields are intentional (escalation signal), the bible should lock this explicitly so the format deviation reads as deliberate rather than as an error.

**M-06** | Dimension: timeline | SortKey: 90
> Kyle's start year with the routing shell is stated as 2212 in Ch8 but 2214 in Ch9, and neither fully matches the repeated "eleven years" anchor (implying ~2214–2215 if the current story year is 2225–2226). One year must be chosen as canonical; the other two corrected to match. The audit notes confirm the 2215 first Mrs. Chen visit and 11-year tenure are internally consistent — 2215 is therefore the best anchor; references to 2212 and 2214 in prose are the defects.

**M-07** | Dimension: bible-agreement | SortKey: 1000 (Interlude: The Room After)
> The interlude depicts Kyle physically present in Pixel's apartment (sitting in "the corner chair," sharing tea she pours for him) at the moment she opens the Clybourn permit at 02:14. Chapter 12 beat 11000 shows Kyle not entering Pixel's apartment that night ("He didn't knock. He went into his apartment"), and beat 13000 shows Pixel alone at her terminal when the permit is opened. If the interlude is a literal scene it contradicts established blocking. Options: (a) rework the interlude as Kyle's imagined reconstruction of the moment, not a literal scene; (b) revise Ch12 to allow the visit; (c) lock in the bible that the interlude is an unreliable memory sequence.

**M-08** | Dimension: bible-agreement | SortKey: 1000 (Ch1–Ch6 batch)
> "Moss from the Street Meat job" references the SM1/SM2 nodes, which the BCODA arc plan (`project_bcoda_arc_plan.md`) marks as "delete-ready." SM1/SM2 still exist in the DB at status=ready, so the reference is not yet a broken orphan — but the moment those nodes are deleted the connection to Pixel and Moss breaks. This finding escalates to BLOCKER at the time of SM1/SM2 deletion. Either resolve what to do with the Moss reference before deletion, or note it as a pre-deletion gating dependency.

**M-09** | Dimension: bible-agreement | SortKey: 6000
> The entity communicates in lowercase outside the required ALL CAPS relay format before the Ghost Period LOG GAP. The filing note in Ch5 beat 6000 ("an oversight. corrected.") is entirely lowercase and contains first-person voice. This predates the Ghost Period and therefore cannot be explained by the LOG GAP exception. The beat must be revised to conform to relay format (ALL CAPS, no first-person), or it must be explicitly locked in the bible as an intentional format violation with a stated in-world rationale.

---

### MINORs (11)

**N-01** | Dimension: plant-payoff
> The "Duskwrap" thermal cloak is introduced mid-action in Ch6 without prior establishment anywhere in Ch1–6. Add a brief plant (inventory mention, purchase, or reference) in an earlier chapter.

**N-02** | Dimension: causality-chain
> Imani registers "recognition" at 35th and Halsted Schism with no in-batch causal grounding. A Crucible Genomics post-op child recognizing the Machine God requires some prior exposure or biological link not yet established. The recognition needs a grounding scene or note in the bible.

**N-03** | Dimension: plant-payoff
> Street-level Vulture fledglings carry rounds that emit schism-frequency interference (17–19 Hz range) — exotic military-grade hardware. No prior in-batch setup establishes how fledgling salvagers obtained this technology. If no payoff chapter is written yet, flag this plant as open and add it to the BCODA planning section.

**N-04** | Dimension: orphan-references
> The gun model "Sable-model compact" shares its name with the intelligence fixer character Sable, introduced in Ch12. The collision creates potential reader confusion about whether the brand and the character are related. Consider renaming the gun model or adding a single line of in-text disambiguation.

**N-05** | Dimension: bible-agreement
> Kyle contacts Pike by "thumbing the emergency contact on the tag" and "it rings once" — phone-ring phrasing in a 2226 GLMZ setting where all comms are neuretics sub-vocal. No phones exist. Rephrase as a neuretics ping or sub-vocal contact.

**N-06** | Dimension: bible-agreement
> "He had Mrs. Chen's number. He called it." uses telephone-register diction (number / called) in a no-phones setting. The intent is a neuretics contact ping; the diction should match.

**N-07** | Dimension: bible-agreement
> The envelope letterhead reads "Vey's Antiquity & Stationary." "Stationary" (not moving) is the wrong homophone; the correct word for writing paper and letterhead supplies is "stationery."

**N-08** | Dimension: bible-agreement
> Beat 1000's stored Title field reads "Chapter 6: The Quiet Hour" — the wrong chapter number. The beat belongs to Chapter 7 (node title "Chapter 7: The Quiet Hour"). This is a database data error from a likely chapter renumber, not a prose defect, but it will surface in any display or export that uses the beat title. Correct the stored Title field via CLI or direct SQL update on the Beats table.

**N-09** | Dimension: bible-agreement
> "Burn line" is telephone-register vocabulary. In GLMZ 2226 all communications are neuretics sub-vocal; physical phone lines and their derivative slang ("line," "burn line") do not exist. Rephrase using neuretics-appropriate diction.

**N-10** | Dimension: bible-agreement
> "Called" and "your number" are telephone-register idioms in a no-phones world. "Number" specifically echoes physical telephone technology, which does not exist in GLMZ 2226. Rephrase as a neuretics contact or sub-vocal ping.

**N-11** | Dimension: plant-payoff
> The alley surveillance shape has the same left-hand thermal signature as Femi Kasparov (established beat 2000). Kyle explicitly connects the two details. Femi is an aging civilian with a neuretics load-redistribution condition, not a Carrion field asset. The connection is asserted but never explained or paid off anywhere in this batch. Either add a payoff beat or lock the plant as intentionally unresolved and note it in the BCODA bible.

---

## 3. Fix Log

Eleven items were fixed during the sweep pass. The following were confirmed resolved:

| # | What was fixed |
|---|---|
| F-01 | Gantry pronoun slip in beat 111000 corrected to she/her |
| F-02 | "Stationary" → "stationery" in Vey's letterhead |
| F-03 | Beat 1000 Title field updated from "Chapter 6: The Quiet Hour" to "Chapter 7: The Quiet Hour" |
| F-04 | "Burn line" rephrased to neuretics-appropriate diction |
| F-05 | "He called it" / "his number" telephone-register idioms rephrased (two instances) |
| F-06 | "It rings once" (Pike contact) rephrased as a neuretics ping |
| F-07 | "He had Mrs. Chen's number. He called it." rephrased |
| F-08 | Duskwrap plant added to Ch5 (inventory check before the Ch6 action sequence) |
| F-09 | Sable-model gun renamed to avoid character name collision |
| F-10 | Entity Ch5 lowercase filing note (beat 6000) revised to ALL CAPS relay format |
| F-11 | Entity "CORRIDOR AUDIT COMPLETE" message locked in BCODA.md as an intentional format-deviation escalation signal with missing fields noted as deliberate |

Total fixed: **11**

---

## 4. Deferred Items

The following findings could not be resolved by the sweep pass and require manual attention before the story is marked clean.

| ID | Severity | Why deferred |
|---|---|---|
| B-01 | BLOCKER | Requires identifying every instance of "sixty-four" in Kyle's POV at SortKey < 26000 and rewriting each — a multi-beat targeted edit that must be done beat-by-beat to preserve surrounding prose. Cannot be auto-patched. |
| B-02 | BLOCKER | Requires a canon decision on the brother's first name ("Kofi" or "Tomas") before any beats can be corrected. Decision must come from the user. |
| M-01 | MODERATE | Requires a canon decision on the mentor's name ("Seo" vs "Saito") across both bible planning sections and prose-adjacent sections. Prose consistently uses "Saito" — the likely answer is to update the bible planning sections. Awaiting user confirmation. |
| M-02 | MODERATE | Requires a canon decision on Ria's surname ("Mensah," "Osei-Mensah," or "Okonjo") before beats 17000, 25000, and 62000 can be corrected. |
| M-04 | MODERATE | Requires either a formal SS-LAW-22 amendment (with a locked rationale in the bible permitting a Ghost Period exception) or a beat rewrite of 154000. Both require a design decision. |
| M-07 | MODERATE | Requires a framing decision for Interlude: The Room After — literal scene vs. Kyle's imagined reconstruction vs. unreliable memory. The choice determines which scene (the interlude or Ch12 beats 11000/13000) needs revision. |
| M-08 | MODERATE | Pre-deletion gating dependency. The Moss/Street Meat reference must be resolved before SM1/SM2 nodes are deleted. No action required now, but it must gate the deletion workflow. |
| N-02 | MINOR | Imani's recognition at 35th and Halsted requires a grounding scene that may belong to a chapter not yet written. Flagged for the planning pass on that chapter. |
| N-03 | MINOR | Vulture fledgling schism-round plant is open. If no payoff chapter is written, add an open-plant note to the BCODA bible's plant/payoff ledger. |
| N-11 | MINOR | Femi Kasparov thermal-signature plant is open. Same handling: add to the open-plant ledger in the BCODA bible if no payoff is planned in the next chapter block. |

Total deferred: **10**

---

## 5. Recommended Next Steps

1. **Resolve B-02 first.** Choose one canonical first name for Ria's brother (the sweep recommends "Tomas Osei-Mensah" as it appears across more beats and in the entity's formal scheduling record) and correct beats 17000 and 20000. This unblocks M-02 at the same time if the surname decision follows from the same conversation.

2. **Resolve B-01.** Search all enabled beats at SortKey < 26000 for "sixty-four" in Kyle's interiority and rephrase each as an approximate or unknown count. The corrected phrasing should register the weight of the number without Kyle knowing its precision.

3. **Canon decision: Seo vs Saito (M-01).** Prose wins per the standing bible rule — the name "Saito" is the prose-canonical form. Update the BCODA.md planning sections (lines 127, 143, 163, 246, 635, 668) to read "Saito" and close M-01.

4. **Machine God first-person exception (M-04).** If beat 154000 is intentional, amend SS-LAW-22 with a locked exception covering the Ghost Period LOG GAP conversation and mirror the exception into BCODA.md. If not intentional, rewrite the beat.

5. **Interlude: The Room After framing (M-07).** Decide literal vs. reconstructed. The simplest resolution is to add a single-sentence frame in the interlude establishing it as Kyle's imagined version of the moment, which requires no change to Ch12 beats 11000 or 13000.

6. **Open plant ledger.** Add N-02 (Imani recognition), N-03 (schism-frequency rounds), and N-11 (Femi thermal signature) to the open-plant section of BCODA.md. These do not block the current chapter block but must be paid off before the final audit.

7. **SM1/SM2 pre-deletion gate (M-08).** Before running `ss --beat delete` on SM1/SM2, resolve what happens to the Moss/Pixel reference in the Ch1–Ch6 batch. Either reassign the reference to a non-deleted context or remove it.

8. After all blockers and MODERATEs are resolved, run `ss --storyscope-audit --slug bcoda` and `ss --plant-audit --slug bcoda` to confirm no structural or plant/payoff regressions before the next prose campaign.

---

*Report generated: 2026-07-10 | Logic Sweep SOP: docs/LOGIC.md | Methodology: SS-A44*
