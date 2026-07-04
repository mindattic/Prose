---
codex: 1
project: StreetSamurai
code: PNHL
layer: fix-plan
title: Pinhole — Round-Two Structural Pass (Antagonist Menace + Tech Specificity)
date: 2026-07-03
node: 019EA46A-17CB-7077-909B-11825BA5CFFC
slug: the-door-is-unlocked-2db1c6ca
score-at-audit: 80.9
db-writes: NONE — files only, per instruction. Nothing in this folder has been applied to the DB.
---

# Pinhole (PNHL) — Round-Two Plan

Source: `docs/nodes/PNHL.md` (bible, all locks in §9 binding) and the round-1 rebuild currently
live in the DB (20 enabled beats, SortKey 100–1300, under node
`019EA46A-17CB-7077-909B-11825BA5CFFC`). Panel verdict on that rebuild: 80.9 — flat. 15/20
ballots: antagonist abstract, never earns menace. Pre-flight FAILS on AntagonistCost. 4 ballots:
tech-intrusion mechanics vague.

This pass does not touch structure/order wholesale — it patches four beats and inserts one new
beat. **Final count: 21 beats** (20 + 1), within the ≤22 ceiling.

## 1. Current live reading order (20 beats, for reference)

| Order | SortKey | BeatTitle | Beat Id |
|---|---|---|---|
| 1 | 100 | (Cedar Rapids, untitled) | 019F1188-DF0D-7A83-A66C-464A1C2321E4 |
| 2 | 200 | (Through the Blur, untitled) | D9D02A5B-0548-4D1F-AED1-600571AB0FCF |
| 3 | 250 | (Pulse car / stranger, untitled) | 019F117A-DE80-7AEE-A47E-07345865DEA5 |
| 4 | 300 | Arrival, GLMZ Pulse Terminal | 6B15E7AC-5C85-4377-8B09-A82E571E890A |
| 5 | 400 | (Walk to the Pivot, untitled) | 019F117B-AAF7-7FD4-ABA9-0504F27A5C17 |
| 6 | 500 | The Pivot | 0975439D-4D67-421A-A901-B9923F26A672 |
| 7 | 600 | First night | 019F117C-54B0-71D7-9FA8-403B276FCF9B |
| 8 | 700 | West Town Street Market | 1426F26D-A58F-4E13-B1DC-39D00702EBF7 |
| 9 | 800 | (Ghost-op, untitled) | 019F117C-F616-7EF2-94A7-D301BEE2312F |
| 10 | 900 | The Invitation | 8E68960A-1F82-40ED-8F23-910E501F1E11 |
| 11 | 950 | The Dinner | 63788055-5CA3-4752-81D0-EDEBDEB10FCA |
| **12** | **975** | **First Contact Is the Last Courtesy** | **8BC8A3B3-EB92-4448-9B0E-6B2A5254B318** |
| 13 | 1000 | The Elevator Shaft | 019F1184-AAF5-770D-BC79-74D1412898BF |
| **14** | **1050** | **Pattern** | **B77C73A1-939D-4A84-AEBD-51F55D4DFEFE** |
| 15 | 1075 | Arcturus Civil Security | 668BF56E-FB2B-4317-B279-EC58F2694A1E |
| **16** | **1100** | **What Staying Requires** | **019F117D-8525-777C-B924-E3FAA2B5802E** |
| 17 | 1150 | Preparation | 019F21B0-4741-7CF4-97AD-67051DD21900 |
| 18 | 1175 | Kyle | 266257FD-616E-414B-BEF4-6BDE1F86484B |
| **19** | **1200** | **The Confrontation** | **4E13E3A1-8708-4B5C-B693-C5F5C311CD13** |
| 20 | 1300 | The Hallway (Finale) | 4B29B01B-CCE0-41A0-8851-5A3C26473C3C |

Bolded rows are touched by this pass. Everything else is untouched (already fixed in round 1;
re-opening them isn't warranted by this panel's findings).

## 2. Disposition table — this pass only

| Order | SortKey | Beat | Action | Design points addressed | File |
|---|---|---|---|---|---|
| 12 | 975 (unchanged) | First Contact Is the Last Courtesy | **REWRITE** | #2 (unrecoverable/intimate loss — building-access lockout), #4 (mechanism: credential revocation) | `beats/12-first-contact-the-lockout.md` |
| 14 | 1050 (unchanged) | Pattern | **PATCH** | #4 (mechanisms named: routing-table poisoning, spoofed escrow dispute flag); trims incident 3 to a lead instead of a closed loss, seeding the new beat | `beats/14-pattern-mechanisms.md` |
| **15 (new)** | **1060** | **The Address** | **NEW** | #1 (capitulated specialist shown up close) | `beats/15-the-address.md` |
| 17 | 1100 (unchanged) | What Staying Requires | **PATCH (light)** | Continuity — grounds the "two who said yes" reference in the Beat 15 encounter instead of an off-page registry lookup | `beats/17-what-staying-requires-patch.md` |
| 20 | 1200 (unchanged) | The Confrontation | **REWRITE** | #3 (AntagonistCost — witnessed loss on the page), #4 (mechanism: live credential revocation as demonstration) | `beats/20-the-confrontation-cost.md` |

Untouched beats keep their current SortKey and text: 100, 200, 250, 300, 400, 500, 600, 700, 800,
900, 950 (The Dinner — already strong per the brief's own description), 1000 (The Elevator
Shaft — already grounds its intrusion in a named mechanism: five-hop transfer routing, a
matched dead-drop signature; used as the reference model for point #4), 1075 (Arcturus Civil
Security — its mechanism, a misdelivered-package filing, is already concrete and serves Lock #2,
not the antagonist-menace problem), 1150 (Preparation), 1175 (Kyle — locked, single appearance,
do not touch), 1300 (Finale — the boots/routing-log locks are already satisfied; no antagonist
content lives here to fix).

## 3. Design-point mapping

### Point 1 — a capitulation shown, not cited
New Beat 15, "The Address" (SortKey 1060), inserted directly after Pattern and before Arcturus
Civil Security. Pattern's third incident (a redirected coupling) is trimmed in the PATCH so it
ends on *discovery of the address*, not resolution — she doesn't just note the redirect and move
on, she decides to walk over. The new beat is that visit: a small receiving/freight-locker
operation three blocks from the Pivot, run by a specialist (Amadou Kessler — mixed-heritage name
per world default) who is skilled, functioning, and paid — and visibly not his own man. The tells
are structural, not adjectival: a wall calendar blocked out in codes that aren't his appointments,
a flinch-and-recover when she asks who authorized the redirect, competent work that unsettles her
more than sloppiness would because it proves the Assessor's theory functions. She never confirms
he's one of the two names from the registry — the ambiguity (is he one of "the two," or is the
roster bigger than two?) is left open rather than resolved, consistent with the project's
"keep whodunits open" convention. No physical menace, no dialogue admission — the horror is
entirely in what she watches him do with his hands and his eyes.

### Point 2 — the campaign touches something she can't fix
Beat 12 REWRITE, "First Contact Is the Last Courtesy." The existing beat was pure reflection (she
walks home, opens a blank "Changes" log, writes nothing yet). This pass keeps the walk-home
reasoning about the dinner but adds a concrete incident before she reaches her door: her building
credential is declined at the Pivot's exterior panel, she is locked out for roughly four minutes
in the street outside her own building, and it resolves itself — access restored — before she can
do anything but stand there. She pulls the access log's cached tail (Slicer instinct — she checks
everything she touches) and finds an external override sourced from a management-tier credential
she doesn't hold, timestamped to the exact window she was still walking home from dinner, reverted
before the log's normal purge cycle could hide it. Nothing is stolen, no one touches Nit, no one
touches 2E. The point is narrower and colder: he reached the building she lives in while she was
still in transit, and let her feel it and then took it back, which is worse than if he'd just left
her locked out. The "Changes" log's first real entry (it existed as an empty gesture in the old
draft) becomes this incident instead of nothing.

### Point 3 — AntagonistCost, witnessed on the page
Beat 20 REWRITE, "The Confrontation." The existing scene has her describe a dead-man's-send in the
abstract ("staged across six channels... if I don't cancel on a schedule") and the Assessor simply
calculates and concedes "you're good." This pass inserts a live demonstration: mid-scene, she
trips one of the six channels early and on purpose — a credential revocation against the same
receiving-node address from Beat 15 — to prove the mechanism is real, not a bluff. His desk system
alerts. He is still and precise throughout (per the character doctrine — the cost lands through
increased precision, not a break in composure), but he has to act: he picks up his own console and
issues a short instruction, and she watches a scheduling display on his desk lose an entry — a
slot that had a name in it a second ago goes empty. She doesn't get the name. She gets the empty
slot, which is the concrete detail proving the loss is real: he burned the node, and someone's
day just changed because the math did. This keeps him legible and controlled (no cartoon rage)
while giving the panel's AntagonistCost pre-flight check something to point to.

### Point 4 — tech-intrusion specificity
Threaded through Beats 12, 14, 15, and 20, one mechanism per beat/incident, plain calibration
diction, no technobabble:
- **Beat 12:** credential revocation (a management-tier override on her building access, reverted
  before the log purges).
- **Beat 14, incident 1 (relay):** routing-table poisoning (named explicitly — he seeded the
  relay's internal fault-routing table with dead branches instead of leaving vague "tangled fault
  architecture").
- **Beat 14, incident 2 (handoff):** a spoofed safety-flag filed through the job platform's escrow
  dispute process, which auto-freezes a handoff without any human review — named explicitly
  instead of "she never found out what."
- **Beat 14, incident 3 (coupling) → Beat 15:** a shipping-manifest redirect, followed up in person
  rather than left as a filed loss.
- **Beat 20:** credential revocation again, demonstrated live against the Beat-15 node — the same
  mechanism from Beat 12, now used offensively by her instead of against her, which is the
  structural payoff of naming it consistently rather than inventing a new gadget for the climax.

## 4. Judgment calls flagged for review

1. **The Beat 15 specialist is left unnamed** ("the man behind the counter"). Drafted first with
   an invented name, then cut it: naming him risked making the beat about a new character the
   bible doesn't own rather than about the system he's embedded in, and the horror the design
   point asks for is structural (the calendar, the flinch, the competence) rather than personal.
   If a future pass wants him recurring or wants Pixel to remember him specifically later, he'll
   need a name at that point — flagging so the choice isn't accidental.
2. **He is left ambiguous as "one of the two," not confirmed.** The bible doesn't specify enough
   about the two prior recruits to let me write a confirmed identity without inventing biography
   the bible doesn't own. I treated the ambiguity as a feature — Pixel isn't certain either, and
   that uncertainty (maybe it's not even two anymore) is itself unsettling — but flagging in case
   the intent was for her to have hard confirmation.
3. **Beat 20's "empty scheduling slot" is deliberately withheld from naming a person.** The task
   brief offers three options (a name crossed off, a channel killed, a specialist released) — I
   used the channel-kill as the primary demonstrated cost (it's the one she causes and can verify)
   and the empty slot as the secondary witnessed detail, rather than writing him releasing a named
   specialist on the page, because putting a name in his mouth risked tipping him into
   monologuing his own cruelty, which the bible's "legible, not cartoonish" lock warns against.
   If the panel wants a harder, more explicit cost, the empty-slot beat could be expanded into
   him making a short call she can half-hear.
4. **Beat 12's four-minute lockout window is an invented duration**, chosen to be long enough to
   register as real (not a glitch) and short enough that "restored" reads as a message rather than
   a system failure. Not sourced from the bible; flagging as new invention.
5. **No beat touches Nit and no beat touches physical safety**, per the task's explicit
   constraints. Both new/rewritten beats (12 and 15) were checked against Lock #8 (Nit is a tool,
   never stolen, never targeted) and confirmed clean.
