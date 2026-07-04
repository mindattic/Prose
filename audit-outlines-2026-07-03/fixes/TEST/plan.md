# Structural Fix Plan — Testament (TEST)

Node: `the-court-martial-019ed361`. Source: `docs/nodes/TEST.md` (bible, 2026-06-22) +
`audit-outlines-2026-07-03/TEST.md` (structure audit, 2026-07-03). Files only — nothing written
to the DB. This is the Sonnet-draft stage; Opus polish comes after.

## 1. Beat map

All 39 live beats (SortKey order) stay exactly as they are, with one in-place patch (SK670) and
one appended patch (SK862.5). Nothing is deleted, cut, or renumbered. Five new SortKeys are
opened after the current final beat (SK900) to carry the authored ending.

| SortKey | Beat Id | Status | Note |
|---|---|---|---|
| 50 – 660 | #4075…#5B895A3F | **KEEP** | Unchanged. Dual-timeline setup, obligation, Hana intake, four options, Cortland flashbacks. |
| **670** | **#4106** | **PATCH** | Testimony-delivery reconciliation (see §2). |
| 680 – 748.4375 | #4107…#4965 | **KEEP** | Unchanged, incl. the blocked sixteen-names beat (#4965) — its payoff lands at new SK940, not here. |
| 750 – 850 | #4101…#4103 | **KEEP** | Unchanged. Testimony fight, handshake, Brandt's death, exit, gray-zone entry. |
| 856.25 | #4134 | **KEEP** | Unchanged. Calls Ironbend, told to come in person. |
| **862.5** | **#4126** | **PATCH** | Orvenne cheap payoff appended (see §4). |
| 875 | #4109 | **KEEP** | Unchanged. Hana's public hearing, watched remotely. |
| 900 | #4105 | **KEEP** | Unchanged. 41-page motion, relocation manifest, 14-day deadline — this remains the pivot into the new ending, not a beat to rewrite. |
| **910** | **NEW** | **BRIDGE** | "The Fourteen Days" — resolves the motion + manifest cliffhanger causally (Bear acts, not just waits). |
| **920** | **NEW** | **ADAPTED LOCK (SK862.5 orig.)** | "The Empty Apartment" — omniscient cutaway (precedented by #4125/SK812.5's Brandt-POV cutaway) delivering the bible's SWAT/service-locker/Meridian-Cross beat, relocated off the taken SK862.5 slot. |
| **940** | **NEW** | **ADAPTED LOCK (SK900 orig.)** | "The Nineteenth" — sternum touch, the Ironbend call, sixteen names, CE-0217 closed non-actionable, "the city answered." True locked closing beat, adapted to current continuity. |

Two beats are flagged as **bridge/connective tissue** because the live text has drifted far enough
from the bible's dual-timeline-era ending that the locked content can't drop in cold: **SK910**
(nothing in the bible addresses the dismissal motion or the relocation manifest at all — those
threads didn't exist when SS-TEST-5 was written) and **SK920** (the bible's SWAT/apartment beat
assumed a different, unwritten exit sequence; the live story already spent its "transponder in a
service locker" beat at SK825 inside the IRB building, so SK920 has to be a different locker, a
different reason for someone to be there, and a different objective — see §5 for the SK-address
correction this requires).

## 2. Testimony-delivery contradiction — reconciliation

**Chosen fix: the wall-gap drop (SK670/#4106) is a duplicate insurance copy, not the only copy.**

Rationale for cheapest-cost pick: the alternative (cut the wall-gap paper drop, or cut the
carried/read testimony at SK700–748.4375) would require rewriting five beats totaling several
thousand words, including the load-bearing testimony sequence the audit itself calls the
strongest material in the story. The duplicate-copy fix touches one beat, one paragraph, and adds
maybe 150 words.

Patch (full text in `beats/01-patch-4106-duplicate-testimony.md`) does three things:
1. Reframes the drop as a second, insurance hand of the same three sheets, written twice the
   night before "because a man who has spent eight years trusting a plan also knows what happens
   to plans in freight corridors" — motivated risk management, not a plot hole. This also answers
   the audit's secondary complaint (why walk into a guarded building at all if he already has a
   safe channel): the safe channel is the fallback, not the primary route, because paper left in a
   wall only enters the record if someone else finds and delivers it, and Bear doesn't trust that
   to anyone but himself when he doesn't have to.
2. Adds a **fourth page to the insurance copy only** — the sixteen names, no docket, no case
   number — establishing the plant that pays off at SK940.
3. Names the recipient of the drop as **Ledger**, the gray-zone information broker who already
   exists in-canon and who resurfaces at SK900 (#4105) relaying the dismissal motion with the
   single annotation "Declan." This gives Ledger's foreknowledge of the case a source instead of
   appearing from nowhere at SK900, and it gives the insurance copy someone to have been holding
   for the whole back half of the story.

## 3. Blocked-names payoff (#4965, SK748.4375)

No patch needed at #4965 itself — the beat already ends on the right note ("He had somewhere
else to say them"), which is a plant, not an oversight, once an ending exists to redeem it. The
payoff lands at new **SK940**: Bear reads the sixteen names into the Ironbend dock comm line at
0600, and the dock — the crane operator, the dispatcher, the ordinary noise of a working yard —
answers back. This is the adapted form of the bible's "the city answered."

## 4. Orvenne thread (#4126, SK862.5) — recommendation

**Recommendation: cheap on-page payoff (drafted), not the cut-patch.**

Reasoning: the Orvenne beat is well-written and does real work establishing that Bear's new gray-
zone life has its own quiet stakes even after Halcyon is behind him — cutting it would remove one
of the few present-tense threats in the back third that isn't Halcyon-shaped. It also costs
almost nothing to close: no new fight (respecting SS-TEST-4 lock #9, no gauntlet), one short scene,
one new minor character (an unnamed Orvenne vetting clerk) who never needs to reappear. Draft is
in `beats/02-patch-4126-orvenne-threshold.md`, appended to the existing beat: Bear goes through
the Threshold, learns the trace was routine volume-vetting (an accountant's question, not a
threat), gets a passphrase, leaves. Keeps Bear's characteristic warmth ("Take care of yourself,"
unintimidated, faintly amused) and resolves the tension raised without escalating it.

If a full cut is preferred instead: the minimal cut-patch would remove the final two paragraphs
of #4126 (from "He stood in the rain with the Manowar dormant" through the end) and stop the beat
at "and had followed it. That was not supposed to be possible," turning it into a one-paragraph
unresolved unease that never gets named "Orvenne" at all — cheaper still, but it throws away the
best-written material in the beat and leaves zero payoff for a name introduced with real weight.
Not recommended.

## 5. Bible SK re-sync — corrections needed

| Bible citation | Bible says | Live reality | Correction |
|---|---|---|---|
| SS-TEST-4 lock #1 ("We operated on the intelligence we were given") | SK746.09375 | Live SK746.09375 (#4154) is actually "forty-three enemy combatants deceased" — a different beat. The dropped-clause line is spoken at **SK695** (#4122, Brandt in the lobby). | Change lock #1's citation from SK746.09375 to **SK695**. |
| SS-TEST-5 spine, "SK862.5 — SWAT at empty apartment" | SK862.5 | Live SK862.5 (#4126) is the Orvenne thread, unrelated. | With this fix's new ending in place, change the spine citation to **SK920** (new beat, `04-new-the-empty-apartment.md`). |
| SS-TEST-5 spine, "SK900" (sternum touch / city answered / CE-0217 closed) | SK900 | Live SK900 (#4105) is the 41-page dismissal motion / cliffhanger — never the closing beat. | With this fix's new ending in place, change the spine citation to **SK940** (new beat, `05-new-the-nineteenth.md`). |
| SS-TEST-4 lock #3 ("He did not move. The room did.") | SK800 | **Not found anywhere in the live text** (verified by string search). The nearest kin phrase is the inverse construction — "The room didn't move" — at **SK350** (#4081, Hana's "You were the only one who kept paying" beat), a different scene with a different meaning. This is a fourth drift the audit didn't catch. | Recommend during Opus polish: either (a) retire this lock and cite the SK350 near-miss instead with corrected wording, or (b) insert the literal line at SK800 (#4102, the handshake beat) where it would fit cleanly after "Bear released the operator's wrist." Not fixed in this pass — flagging only, since it's outside the five assigned structural requirements and touches a beat this plan doesn't otherwise modify. |
| SS-TEST-1 "Beat 12" (past) — last salute at Cortland, "the same motion, the same angle, the same eight seconds" | Beat-number citation (pre-SortKey scheme) | **Not found anywhere in the live text.** No Cortland beat (SK100–600) contains this line or an equivalent second-salute moment; the sole surviving salute plant in the flashback stream is SK200's "Bear saluted with the others and filed out." | Bonus finding, not fixed here. Flag for whoever next touches the Cortland flashback stream — either author the missing second salute or retire the "Beat 12" citation from SS-TEST-1. |

SS-TEST-4 lock #2 (salute/handshake, SK800/#4102) and lock #4 verbatim ("Brandt's death…SK812.5")
were checked and are **correct as cited** — no change needed there.

## 6. What "adapted" means for the two authored-lock beats

The bible's SK862.5/SK900 content was written for an earlier conception of the ending (a literal
SWAT raid, a phone call at 0600 to an unspecified number, a comm-image count of well-wishers) that
predates several things now load-bearing in the live text: the Manowar's compliance-lock/dormant
arc, the transponder-decoy already spent at SK825, Ledger as an established contact, the Orvenne
thread, and the dismissal-motion/relocation-manifest cliffhanger the bible never anticipated. The
new beats keep every locked *image* (Meridian Cross face-down in a service locker; a team lead
pausing before speaking; a sternum touch on the absence of Manowar's cycle; a call at 0600; CE-0217
closed non-actionable; the city answering) but re-house them in objects and relationships the
current story has actually built, and add SK910 as the causal beat the audit's finding #7 asked
for — Bear taking an action, not just waiting out institutional timing.
