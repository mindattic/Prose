---
codex: 1
project: Prose
code: PNHL
layer: fix-plan
title: Pinhole — Structural Rebuild Plan
date: 2026-07-03
node: 019EA46A-17CB-7077-909B-11825BA5CFFC
slug: the-door-is-unlocked-2db1c6ca
score-at-audit: 80.7
db-writes: NONE — files only, per instruction. Nothing in this folder has been applied to the DB.
---

# Pinhole (PNHL) — Structural Rebuild Plan

Source docs: `docs/nodes/PNHL.md` (bible, revised 2026-07-03, §10 spine is the target) and
`audit-outlines-2026-07-03/PNHL.md` (structural audit, score 80.7 against the superseded draft).

The live 29 beat-node links under this node implement a retired plot (instrument theft, beacon
tracking, Arcturus ticking clock, anonymous-dossier resolution, Kyle says "Lock yours"). None of
that is written to the DB by this plan — everything below is a proposal, delivered as files under
`audit-outlines-2026-07-03/fixes/PNHL/`.

## 1. Disposition table — every live beat-node link (29 total: 27 enabled + 2 already disabled)

| # | Beat Id | Number | Current Title | Old SortKey | Enabled? | Action | Notes |
|---|---|---|---|---|---|---|---|
| 1 | 019F1188-DF0D-7A83-A66C-464A1C2321E4 | 4889 | (untitled) | 50 | Y | **KEEP** | Cedar Rapids departure. Matches bible Beat 1 almost verbatim. No file. |
| 2 | D9D02A5B-0548-4D1F-AED1-600571AB0FCF | 4394 | (untitled) | 100 | Y | **KEEP** | Through the Blur / aerobloc. Matches bible Beat 2. No file. |
| 3 | 019F117A-DE80-7AEE-A47E-07345865DEA5 | 4877 | (untitled) | 150 | Y | **KEEP** | VacCell check + stranger's "selling the work vs. selling yourself" line. Not in the bible's spine text but harmless texture that pre-echoes the Assessor's coercion theme without naming it. No lock violated. No file. |
| 4 | 6B15E7AC-5C85-4377-8B09-A82E571E890A | 4395 | Arrival, GLMZ Pulse Terminal | 200 | Y | **KEEP** | Matches bible Beat 3. No file. |
| 5 | 019F117B-AAF7-7FD4-ABA9-0504F27A5C17 | 4878 | (untitled) | 250 | Y | **KEEP** | Walk to the Pivot. Matches bible Beat 4. No file. |
| 6 | 0975439D-4D67-421A-A901-B9923F26A672 | 4396 | (untitled) | 300 | Y | **PATCH** | Matches bible Beat 5 (climate fix, Donatella names her Pixel). Audit finding #9: Reyes conflict opens and is never closed. Patch softens Reyes's line and adds one closing sentence so the thread reads as resolved neighborhood friction, not a planted arc. See `beats/06-the-pivot.md`. |
| 7 | 019F117C-54B0-71D7-9FA8-403B276FCF9B | 4879 | (untitled) | 350 | Y | **PATCH** | Matches bible Beat 6 (first night, Kyle's door noted). Companion patch to #6 — closes the Reyes thread explicitly instead of leaving it open. Kyle's door-activity note is preserved; it's required setup for Beat 16. See `beats/07-first-night.md`. |
| 8 | 1426F26D-A58F-4E13-B1DC-39D00702EBF7 | 4397 | West Town Street Market | 400 | Y | **PATCH** | Matches bible Beat 7 but is missing the required content: the kid telling her about the relay job on-page, and the "he doesn't let people walk... the ones who say no mostly just leave" warning about the Assessor's crew. Patch adds both. See `beats/08-west-town-market.md`. |
| 9 | 019F117C-F616-7EF2-94A7-D301BEE2312F | 4880 | (untitled) | 450 | Y | **KEEP** | Ghost-op / Ryokan nine-second gap. Matches bible Beat 8 closely, including the closing line "Someone had found value in the gap." No file. |
| 10 | 8E68960A-1F82-40ED-8F23-910E501F1E11 | 4398 | The Incident | 500 | Y | **REWRITE** | Currently: a messenger + a phone call that explicitly names "the chassis architecture" as the target — the Assessor wanting the drone design outright. This is the clearest Lock #8 violation in the early half of the story ("Nit is a tool, not a MacGuffin. Nobody wants the drone design.") and sets up the entire theft arc. Fully rewritten to bible Beat 9: a bare, unsigned invitation slipped under the door, expensive paper stock, restaurant + 21:00, her afternoon of research into the restaurant, going in her own clothes and her mother's boots. See `beats/10-the-invitation.md`. |
| 10b | 019F1183-36EE-7570-BCB5-A65EAD7FB360 | 4886 | (untitled) | 550 | Y | **DISABLE** | The break-in: Nit and the primary case are physically stolen, note reads "The offer stands. 72 hours." This is the single most direct violation of Lock #8 in the entire node ("Nit is never stolen") and is the hinge the rest of the theft/tracking/retrieval arc (4400, 4401, 4882, 4918, 4402, 4887, 4403, 4883, 4404, 4885, "12") all depend from. Disabling this beat is what makes disabling all of those downstream beats correct rather than merely convenient — none of them have a reason to exist once the break-in they're all responding to doesn't happen. |
| — | *(new)* | — | The dinner | *(new)* | — | **NEW** | Bible Beat 10. The Assessor's first on-page appearance, mandatory per the task brief and Narrative Lock #5 (his logic must be legible on the page, not abstract). Nothing in the current draft covers this — the Assessor never appears in person anywhere in the 29 live beats. See `beats/11-the-dinner.md`. |
| — | *(new)* | — | First contact is the last courtesy | *(new)* | — | **NEW** | Bible Beat 11 — short transition beat: she reads the dinner correctly (his graciousness was not surrender) and starts tracking what changes. Needed to bridge the dinner into the sabotage campaign; nothing in the live draft does this work because the live draft has no dinner to follow. See `beats/12-first-contact.md`. |
| 11 | 019F1184-AAF5-770D-BC79-74D1412898BF | 4888 | (untitled) | 1250 | Y | **REWRITE** | Currently: intercepting a spoofed Arcturus authentication transfer to recover her stolen case, then inspecting the recovered drift meter. The *technique* (relay injection built from routing-table notes, improvised second injection under time pressure) is exactly the Channeler skill the bible calls for in §6 and is strong, specific prose — worth keeping the bones of. Repurposed and **moved earlier** (SortKey 1250 → 1000) to become bible Beat 12, "The elevator shaft": she intercepts the Assessor's 800Φ bribe to a teammate who bails on a job, not a transfer to recover stolen gear. See `beats/13-the-elevator-shaft.md`. |
| 12 | B77C73A1-939D-4A84-AEBD-51F55D4DFEFE | 13 | The Setup | 1300 | Y | **REWRITE** | Currently: building a four-part dossier and anonymously sending it to a rival collective, a competing firm, and the ACS tip line, who dismantle the operation off-page. This is the audit's finding #3 (no antagonist-facing climax — resolution by proxy, not confrontation) and directly weakens Lock #1 ("She solves it herself... The confrontation is hers"). Repurposed and **moved earlier** (SortKey 1300 → 1050) into bible Beat 13, "Pattern": the incident-tracing and cost-accounting material is genuinely good and becomes the middle sabotage-campaign texture instead of the ending. See `beats/14-pattern-incidents.md`. |
| 13 | 668BF56E-FB2B-4317-B279-EC58F2694A1E | 4399 | Arcturus Civil Security | 600 | Y | **REWRITE** | Currently framed as her testing whether to report the stolen equipment, backing out with a lie. Strong material (the officer's characterization is exactly Lock #2 — "not evil, structurally unhelpful") but its trigger (reporting a theft that no longer happens) is gone. Repurposed and **moved later** (SortKey 600 → 1075) into a second Pattern beat: she reports a sabotage incident (a rerouted delivery) and gets the same structurally-correct, personally-useless bureaucratic response — this is the story's one on-page ACS beat, satisfying Lock #2. See `beats/15-pattern-acs.md`. |
| 14 | 019F117D-8525-777C-B924-E3FAA2B5802E | 4881 | (untitled) | 650 | Y | **REWRITE** | Currently: stairwell reflection on the Assessor's "fourteen cases, eighteen months, two stayed" operating theory, triggered by the ACS visit above. The theory material is good and is exactly what bible §8 describes ("His logic... is not wrong about everything"). Repurposed and **moved much later** (SortKey 650 → 1100) into bible Beat 14, "What staying requires" — the options-elimination beat right before she decides to confront him, folding in the market kid's warning from Beat 7. See `beats/16-what-staying-requires.md`. |
| 15 | 9BE85D37-914A-4431-82AA-812E11B7581C | 4400 | The Room, 14:00 | 700 | Y | **DISABLE** | Building a directional receiver to trace a beacon embedded in the stolen kit. Entirely theft-arc; no salvageable non-theft content. |
| 16 | AFE27955-C520-44C1-958C-8530B381B8B5 | 4401 | Signal Trace | 800 | Y | **DISABLE** | The Tanaka–Arcturus data-sharing / 72-hour ticking-clock material. This is the Arcturus remnant the task brief explicitly says to cut. |
| 17 | 019F117E-1D06-72CF-9D27-7A09B19FCCAC | 4882 | (untitled) | 850 | Y | **DISABLE** | Aerial recon of the Assessor's building via NSB-ridden VacCell, watching two figures catalogue stolen goods. Theft-recovery arc. |
| 18 | 019F1365-B73B-727D-A15C-BF5C0E8E97A5 | 4918 | (untitled) | 875 | **N (already disabled)** | **DISABLE (leave as-is)** | Three-shell recon hack (Ryokan unit + Meridian shell + Tanaka rooftop array) pulling routing table, floor plan, and camera view of Nit + an Arcturus schematic. Duplicate of Beat 4402's discovery (see audit finding #11) and pure theft-arc. Already disabled in the DB — no action needed, this row just documents why it should stay that way. |
| 19 | 4785834D-BDCD-47E8-8B4A-AE6A4D00CF41 | 4402 | Follow | 900 | Y | **DISABLE** | Views VacCell footage of Nit on an examination table next to an Arcturus-logo schematic. Direct Lock #8 violation (drone as MacGuffin) and duplicate content vs. 4918 (audit finding #11). |
| 20 | 019F1183-EC30-73B1-8A4E-342D934BB07D | 4887 | (untitled) | 950 | Y | **DISABLE** | Two-part retrieval scheme (fake ACS alert to clear the building + spoofed transfer injection). Theft-recovery arc. |
| 21 | BA7027BA-D2AC-496D-B3EC-AB8707237C41 | 4403 | Observation | 1000 | Y | **DISABLE** | Casing the target building at midday, spots her case under a table. Theft-recovery arc. |
| 22 | 019F21B0-4741-7CF4-97AD-67051DD21900 | 4970 | (untitled) | 1025 | Y | **REWRITE** | Currently a generic equipment-check-and-wait beat with no boots content. Repurposed and **moved to just before the Kyle/confrontation beats** (SortKey 1025 → 1150) as bible Beat 15, "Preparation" — equipment check, Nit included this time, and the required boots-not-repadded noticing. See `beats/17-preparation.md`. |
| 23 | 019F117E-B2AF-7293-BBE0-6182C03FDA3D | 4883 | (untitled) | 1050 | Y | **DISABLE** | 02:45 alarm, laces boots, explicitly leaves *without* Nit for the retrieval mission. Theft-arc timing beat; contradicts bible Beat 15, which has her bring Nit to the confrontation. The boot-lacing image is reused inside the Beat 15 rewrite above instead of kept as its own beat. |
| 24 | 9FF237A7-775D-487A-96CD-64BCC3B6062986 | 4404 | The Intervention, 03:00 | 1100 | Y | **DISABLE** | Broadcasts a fake chassis-ID alert to clear the building for the retrieval. Theft-arc mechanism. |
| 25 | 019F117F-E11F-7BD2-A6CE-34BC56BBB9DB | 4885 | (untitled) | 1125 | Y | **DISABLE** | Watches the ACS search from a step; a courier exits with an unrelated case. Theft-arc. |
| 26 | 019F117F-BC5F-75A4-AB78-878F1E5C91D6 | 4884 | (untitled) | 1150 | **N (already disabled)** | **DISABLE (leave as-is)** | Empty record — no title, no synopsis, no text (0 chars). Already disabled. No content to salvage or rewrite; leave disabled. |
| 27 | 72FCEBF2-43EE-460C-A0EB-3EBDDCBB69AA | 12 | 04:30 | 1200 | Y | **DISABLE** | Retrieves the case from under the table, evades the returning ACS officer with a "service call" cover story. Well-written scene, but structurally inseparable from the theft-recovery arc (there's no case to retrieve). Its Lock #2 function (a professional, non-corrupt ACS officer) is preserved instead by the repurposed Beat 13b (`beats/15-pattern-acs.md`), so nothing is lost. |
| 30 | 4B29B01B-CCE0-41A0-8851-5A3C26473C3C | 14 | The Hallway | 1400 | Y | **REWRITE** | Currently conflates Kyle's *only* appearance with the finale, uses the retired line "Lock yours," and frames the resolution as "the machine ran correctly" (the anonymous dossiers took the operation down off-page). The boots-un-padding beat, the unerased routing-log beat, and the closing lines ("Her name was Pixel. She was staying.") are exactly what the bible wants and are kept verbatim in spirit. Rewritten to bible Beat 18. **Kyle collapsed out during Opus polish (2026-07-03):** the draft's second Kyle appearance (mirrored "Give em hell" + high five) was removed because it contradicts the binding sections of the bible — §0 ("He appears in one beat. He says four words"), §8 Character Rules ("One beat... Do not give him interiority"), and Locks #1/#6, which all confine Kyle to the single departure beat where *he* says the words. The §10 spine's Beat 18 was the lone outlier authorizing a return, and the locks win. His affirmation is now folded into Pixel's narration (she recalls his four words on the stairs; he is not on the page). The reflection is re-grounded in the confrontation she won herself, not a proxy resolution. See `beats/20-finale.md`. |

Row count check: 29 unique beat-node links total (27 originally enabled + 2 originally disabled),
all listed above: 4889, 4394, 4877, 4395, 4878, 4396, 4879, 4397, 4880, 4398, 4886, 4399, 4881,
4400, 4401, 4882, 4918, 4402, 4887, 4403, 4970, 4883, 4404, 4885, 4884, "12" (04:30), 4888,
"13" (The Setup), "14" (The Hallway).

## 2. Final reading order (files to create)

20 slots: 6 KEEP (no file), 3 PATCH, 7 REWRITE, 4 NEW. New SortKeys use round numbers with gaps
for future inserts; they supersede the old SortKey column above if this plan is ever applied.

| Order | New SortKey | Bible Beat | Title | Source Beat Id | Action | File |
|---|---|---|---|---|---|---|
| 1 | 100 | 1 | Cedar Rapids Pulse Station | 4889 | KEEP | — |
| 2 | 200 | 2 | Through the Blur | 4394 | KEEP | — |
| 3 | 250 | 2b | Pulse car — VacCell check & the stranger's warning | 4877 | KEEP | — |
| 4 | 300 | 3 | GLMZ Pulse Terminal | 4395 | KEEP | — |
| 5 | 400 | 4 | Walk to the Pivot | 4878 | KEEP | — |
| 6 | 500 | 5 | The Pivot | 4396 | PATCH | `beats/06-the-pivot.md` |
| 7 | 600 | 6 | First night | 4879 | PATCH | `beats/07-first-night.md` |
| 8 | 700 | 7 | West Town Street Market | 4397 | PATCH | `beats/08-west-town-market.md` |
| 9 | 800 | 8 | Ghost-op | 4880 | KEEP | — |
| 10 | 900 | 9 | The invitation | 4398 | REWRITE | `beats/10-the-invitation.md` |
| 11 | 950 | 10 | The dinner | *(new)* | NEW | `beats/11-the-dinner.md` |
| 12 | 975 | 11 | First contact is the last courtesy | *(new)* | NEW | `beats/12-first-contact.md` |
| 13 | 1000 | 12 | The elevator shaft | 4888 | REWRITE | `beats/13-the-elevator-shaft.md` |
| 14 | 1050 | 13a | Pattern — three incidents | "13" (The Setup) | REWRITE | `beats/14-pattern-incidents.md` |
| 15 | 1075 | 13b | Pattern — Arcturus Civil Security | 4399 | REWRITE | `beats/15-pattern-acs.md` |
| 16 | 1100 | 14 | What staying requires | 4881 | REWRITE | `beats/16-what-staying-requires.md` |
| 17 | 1150 | 15 | Preparation | 4970 | REWRITE | `beats/17-preparation.md` |
| 18 | 1175 | 16 | Kyle | *(new)* | NEW | `beats/18-kyle.md` |
| 19 | 1200 | 17 | The confrontation | *(new)* | NEW | `beats/19-the-confrontation.md` |
| 20 | 1300 | 18 | Finale (Kyle removed — see §4.8) | "14" (The Hallway) | REWRITE | `beats/20-finale.md` |

Beats removed from the reading order entirely (13, all DISABLE): 4886, 4400, 4401, 4882, 4918
(already disabled), 4402, 4887, 4403, 4883, 4404, 4885, 4884 (already disabled), "12".

## 3. Counts

- **KEEP:** 6 (4889, 4394, 4877, 4395, 4878, 4880)
- **PATCH:** 3 (4396, 4879, 4397)
- **REWRITE:** 7 (4398, 4888, "13", 4399, 4881, 4970, "14")
- **NEW:** 4 (the dinner, first contact is the last courtesy, Kyle, the confrontation)
- **DISABLE:** 13 total — 11 newly disabled (4886, 4400, 4401, 4882, 4402, 4887, 4403, 4883, 4404,
  4885, "12") + 2 already disabled and left that way (4918, 4884)
- **Total beat-node links accounted for:** 29 (6+3+7 = 16 repositioned/edited originals, + 13
  disabled = 29 originals; 4 NEW beats bring the final reading order to 20 beats)

## 4. Judgment calls flagged for review

1. **4877 (Pulse-car stranger's monologue) kept as-is.** Not in the bible's spine text at all, but
   it's uncredited foreshadowing of the Assessor's whole MO ("the day you can't tell the difference
   between selling the work and selling yourself is the day they have you") and violates no lock.
   Recommend keeping; flagging in case the intent was for the spine to be exhaustive.
2. **Reyes patched down, not fully cut.** The audit offered "resolve on-page or cut back to
   texture" as alternatives; I chose the lighter patch (soften + add one explicit closing line)
   over removing him outright, since he's Donatella's only foil and gives the Pivot scene a second
   voice. If the bible's silence on Reyes means he shouldn't exist at all, beat 6/7 would need a
   fuller rewrite instead of a patch.
3. **4399 (ACS office) repurposed rather than cut**, to preserve Lock #2 ("ACS is not evil — it is
   structurally unhelpful"). The bible's own beat list has no explicit ACS beat, so this is an
   inference that the Lock needs an on-page vehicle somewhere in the sabotage campaign. If Lock #2
   is meant to be satisfied by the ACS officer's brief mention alone (§8's character-rules entry),
   this beat could be cut instead and folded into a line of narration in beat 14a/14b.
4. **4888's technique repurposed into "the elevator shaft" (Beat 12).** The relay-injection
   mechanics (spoof an outage announcement, chase the far end when the near end is cached) are
   copied over almost verbatim, just re-triggered by a bribed teammate bailing on a job instead of
   a stolen-case recovery. This is the single biggest prose-reuse call in the plan — flagging it
   explicitly in case a fully from-scratch Beat 12 is preferred.
5. **"13" (The Setup)'s dossier-to-three-parties resolution is fully discarded**, not adapted —
   per audit finding #3 and Lock #1, no version of "she wins by proxy" survives. Only the
   incident-cataloguing texture (the six-hour untangle, the cancelled handoff, the rerouted gear)
   is kept, recontextualized as mid-campaign cost rather than end-game evidence-gathering.
6. **Two mandatory setbacks-with-cost were placed in Beat 13a** (`beats/14-pattern-incidents.md`):
   a job she can only partially save (forced partial refund — lost income) and a contact who won't
   work with her again after a sabotaged handoff (burned contact/reputation), plus a physical-cost
   beat (a six-hour overnight untangle job leaving her running on no sleep going into the next
   incident). If the story ultimately wants the setbacks distributed across more beats rather than
   concentrated in one, that's a straightforward split during polish.
8. **Kyle collapsed to a single appearance (Opus polish, 2026-07-03).** Sonnet's draft followed
   the §10 spine and gave Kyle two beats: the departure (Beat 16, `beats/18-kyle.md`) and a mirrored
   return in the finale (high five + Pixel says "Give em hell"). This was collapsed to the departure
   only. Rationale: §0, §8 Character Rules, and Narrative Locks #1 and #6 all state Kyle appears in
   **one** beat and says his four words; the spine's Beat 18 return is the sole clause authorizing a
   second appearance and it directly contradicts the binding sections, so the locks govern. The
   finale's essential Kyle content (his affirmation, "Give em hell") is folded into Pixel's
   narration on the stairs — Kyle is not on the page. **Beat count is unchanged (still 20):** the
   finale remains a beat; only Kyle was removed from within it. `beats/18-kyle.md` is now his single,
   canonical appearance. If a future revision reconciles the bible by amending §0/§8/§9 to permit
   the return, restore the finale's high-five block from git history.

9. **The confrontation's location is invented** (bible doesn't specify where the Assessor's
   operation actually is once the theft-arc's target-building is cut). I placed it at an
   above-street office the Assessor uses for "assessment" business, reached via the routing
   architecture and reservation trail she's been building since the dinner and the elevator-shaft
   trace — consistent with her Slicer/Channeler skillset, but the specific address/building is new
   invention, not sourced from the bible.
