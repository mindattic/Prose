---
codex: 1
project: StreetSamurai
code: PNHL
layer: fix-plan
title: Pinhole — Round-Three Structural Pass (Antagonist Menace, Take Three)
date: 2026-07-03
node: 019EA46A-17CB-7077-909B-11825BA5CFFC
slug: the-door-is-unlocked-2db1c6ca
score-at-audit: 81.8 (panel), AntagonistCost pre-flight FAIL (both round-1 and round-2 panels)
db-writes: NONE — files only, per instruction. Nothing in this folder has been applied to the DB.
---

# Pinhole (PNHL) — Round-Three Plan

Source: `docs/nodes/PNHL.md` (bible, all locks in §9 binding), and the round-2 rebuild currently
live in the DB (21 enabled beats, SortKey 100–1300, node `019EA46A-17CB-7077-909B-11825BA5CFFC`).
Two menace passes in a row (round 1's structural rebuild, round 2's lockout/Address/confrontation
rewrite) both still drew the same complaint from 10+ of 20 ballots: **the antagonist never earns
menace**, and the AntagonistCost pre-flight check still fails.

Round 2 solved "show a capitulated specialist up close" (Beat "The Address," SortKey 1060) and
"show a witnessed, live cost in the confrontation" (channel-kill + empty scheduling slot). What
neither round did: touch the *refusal* side of the Assessor's operation. Every menace beat so far
has been about people who said **yes** (the Address) or about Pixel's own sabotaged jobs. The
market kid's line — "the ones who say no mostly just leave" — has sat in the text since round 1
as unexamined texture. This pass makes that line the misdirect it was always meant to be: she
checks it, and what she finds is that "leaving" is manufactured, administratively, with no
violence anywhere a report could take hold of. That is the missing menace — not a body, an erasure.

This pass touches two existing beats with light patches and inserts one new beat. **Final count:
22 beats** (21 + 1), at the ≤22 ceiling.

## 1. Current live reading order (21 beats, for reference)

| Order | SortKey | BeatTitle | Beat Id |
|---|---|---|---|
| 1 | 100 | (Cedar Rapids, untitled) | 019F1188-DF0D-7A83-A66C-464A1C2321E4 |
| 2 | 200 | (Through the Blur, untitled) | D9D02A5B-0548-4D1F-AED1-600571AB0FCF |
| 3 | 250 | (Pulse car / stranger, untitled) | 019F117A-DE80-7AEE-A47E-07345865DEA5 |
| 4 | 300 | Arrival, GLMZ Pulse Terminal | 6B15E7AC-5C85-4377-8B09-A82E571E890A |
| 5 | 400 | (Walk to the Pivot, untitled) | 019F117B-AAF7-7FD4-ABA9-0504F27A5C17 |
| 6 | 500 | The Pivot | 0975439D-4D67-421A-A901-B9923F26A672 |
| 7 | 600 | First night | 019F117C-54B0-71D7-9FA8-403B276FCF9B |
| **8** | **700** | **West Town Street Market** | **1426F26D-A58F-4E13-B1DC-39D00702EBF7** |
| 9 | 800 | (Ghost-op, untitled) | 019F117C-F616-7EF2-94A7-D301BEE2312F |
| 10 | 900 | The Invitation | 8E68960A-1F82-40ED-8F23-910E501F1E11 |
| 11 | 950 | The Dinner | 63788055-5CA3-4752-81D0-EDEBDEB10FCA |
| 12 | 975 | First Contact Is the Last Courtesy | 8BC8A3B3-EB92-4448-9B0E-6B2A5254B318 |
| 13 | 1000 | The Elevator Shaft | 019F1184-AAF5-770D-BC79-74D1412898BF |
| 14 | 1050 | Pattern | B77C73A1-939D-4A84-AEBD-51F55D4DFEFE |
| 15 | 1060 | The Address | 019F2AC1-9711-7E1D-B452-C6C5B51AE167 |
| 16 | 1075 | Arcturus Civil Security | 668BF56E-FB2B-4317-B279-EC58F2694A1E |
| **17 (new)** | **1090** | **The One Who Said No** | *(new)* |
| 18 | 1100 | What Staying Requires | 019F117D-8525-777C-B924-E3FAA2B5802E |
| 19 | 1150 | Preparation | 019F21B0-4741-7CF4-97AD-67051DD21900 |
| 20 | 1175 | Kyle | 266257FD-616E-414B-BEF4-6BDE1F86484B |
| **21** | **1200** | **The Confrontation** | **4E13E3A1-8708-4B5C-B693-C5F5C311CD13** |
| 22 | 1300 | The Hallway (Finale) | 4B29B01B-CCE0-41A0-8851-5A3C26473C3C |

Bolded rows are touched by this pass. Everything else — including the round-2 work at 975, 1050,
1060, and 1200's existing content — is untouched; this pass adds to 1200 rather than rewriting it.

## 2. Disposition table — this pass only

| Order | SortKey | Beat | Action | What it does | File |
|---|---|---|---|---|---|
| 8 | 700 (unchanged) | West Town Street Market | **PATCH** | Adds one volunteered line from the market kid: a specific, named-by-relation ("a guy I used to trade parts with") account of a refuser who "booked out east... somebody saw the manifest, his name right there on it." Stays weather-report casual — the kid isn't warning her, he's missing a friend. Gives Beat 17 a concrete thread to pull instead of an abstract rumor. | `beats/08-west-town-market-patch.md` |
| **17 (new)** | **1090** | **The One Who Said No** | **NEW** | She checks the kid's story instead of filing it away. Three checkable, administrative facts: a calibration bench sold at a loss through the Assessor's own holding-company chain; a transit manifest paid for by a credential the refuser never touched; four incompletes filed against his gray-zone handle, all timestamped *after* his departure. No violence anywhere. The horror is that "he left" is true on every document and false in total — engineered to survive exactly the kind of check she just ran. She ends checking her own registry footprint. | `beats/17-the-one-who-said-no.md` |
| 21 | 1200 (unchanged) | The Confrontation | **PATCH** | Adds one projected page (the three Cotter facts) to the evidence sequence already being thrown up between them, and one short exchange where she names it as leverage: not just proof he sabotaged her, but proof he authors disappearances on paper. His composure holds — the cost lands as precision sharpening one turn further, per the wire-under-load motif already established at the restaurant and earlier in this same scene — and he declines to say anything else about Cotter, which is its own confirmation without a confession. | `beats/21-the-confrontation-leverage-patch.md` |

## 3. Design-point mapping

### Point 1 — the capitulation-shaped misdirect, inverted
Round 2's Beat 15 ("The Address") showed a specialist who said yes, up close. This pass shows what
the story has been telling itself was the *safe* outcome — saying no and leaving — and reveals it
as the same operation running in the other direction. The market kid's line has been sitting in
the text unexamined since round 1: "The ones who say no mostly just leave." This pass is the
payoff of that plant. It was never a lie the kid told her. It's a lie the Assessor's paperwork
told the kid, faithfully repeated.

### Point 2 — concrete, checkable evidence, in her register
Three facts, each independently explicable, each corroborating once stacked (mirrors the "Pattern"
beat's three-incidents structure at 1050, deliberately — same investigative rhythm, aimed at a
different victim):
1. **The bench** — a calibration bench sold at a loss, four days after the assessment date,
   through the same holding-company chain she has already traced to the restaurant reservation and
   the fifth-floor office (established in the Confrontation beat's existing text: "a holding entity
   that also leased office space two districts over"). Ambiguous alone — people sell gear when they
   move.
2. **The manifest** — the transit ticket east was paid for by a third-party credential with zero
   transaction history connecting it to the refuser, ever. Someone else bought the ticket and put
   his name on it.
3. **The incompletes** — four abandoned-job marks land on his gray-zone handle in the five weeks
   *after* the manifest date, in flat platform-generated language nobody audits, functioning as
   permanent insurance against him ever working under that handle again, anywhere.
No body, no confrontation, no scene of violence — consistent with the brief's explicit constraint
and with Lock #5 (his logic is legible, not cartoonish; the horror here is bureaucratic, not
theatrical).

### Point 3 — the misdirect collapsing, on the page
She realizes explicitly, in beat, that the kid didn't lie to her — the kid repeated exactly what
the record was built to make him believe. This reframes "he doesn't let people walk... the ones
who say no mostly just leave" (Beat 700, and echoed in "What Staying Requires" at 1100, left
untouched by this pass) as the Assessor's own cover story circulating as street folklore, which is
the mechanism, not a rumor about the mechanism.

### Point 4 — she checks her own exposure
The beat ends on her pulling her own gray-zone registry entries and finding that most of her
professional footprint lives in systems she doesn't personally hold the keys to — the same
exposure Cotter had. This is the one moment of personal stakes in the beat, delivered the way her
voice delivers everything: an observation, not a declared fear.

### Point 5 — the confrontation's leverage reframed
The Confrontation patch (1200) makes this discovery load-bearing rather than decorative: she tells
him explicitly that the package isn't only proof of sabotage against her, it's proof of what "they
left" means when he's the one writing the record — and that reframing is what she's betting
changes his calculation, not the money or the channel-kill alone (both already demonstrated in
round 2's rewrite, kept intact). His reaction stays inside the established doctrine: precision
sharpens, he does not break, and he chooses not to engage with Cotter's name at all rather than
confirm or deny — silence as its own tell, without tipping into a monologued confession.

## 4. Reconciliation check (per task brief point 4)

The "Pattern" beat (1050) references three sabotage incidents against Pixel's own jobs; it does
not mention refusers or prior recruits at all, so there is nothing to reconcile there. "The
Address" (1060) references "the two names she'd have found... the ones who'd looked at the offer
and signed rather than left" — that is the **yes** side, and it explicitly declines to resolve
whether the receiving-counter specialist is one of those two. This pass's new beat is the **no**
side of the same ambiguity and does not touch or resolve the Address beat's open question; the two
beats corroborate the same operation from opposite outcomes without duplicating either one.
"What Staying Requires" (1100) is left untouched — it already carries the "two who said yes"
reference (patched in round 2) and needs no edit to remain consistent with the new beat sitting
immediately before it; nothing in its text contradicts what Beat 17 adds.

## 5. Counts

- **PATCH:** 2 (West Town Street Market at 700; The Confrontation at 1200)
- **NEW:** 1 (The One Who Said No, SortKey 1090)
- **Beats untouched by this pass:** 19
- **Total enabled beats after this pass, if applied:** 22 (21 + 1 new)

## 6. Judgment calls flagged for review

1. **The refuser is named only by handle ("Cotter"), never given a real name or physical
   description on the page.** Consistent with round 2's judgment call on the Beat 15 specialist —
   naming him further risked making the beat about a new character the bible doesn't own, when the
   design point is structural (three documents, not a person). Cotter never appears in person in
   this story; he is entirely reconstructed from records, which is itself part of the horror (she
   never gets to meet him, corroborate any of it with him directly, or know if "east" is real).
2. **Cotter is explicitly left unconnected to "the two who signed."** The Address beat's ambiguity
   (is the receiving-counter man one of two, or is the roster bigger?) and this beat's ambiguity
   (how many refusers has this happened to, not just Cotter?) are deliberately never resolved into
   a hard number. Per the project's "keep whodunits open" convention, and consistent with round 2.
3. **The Confrontation's new exchange does not have the Assessor confirm or deny violence, or
   confirm or deny that anything happened to Cotter beyond what the documents show.** He declines
   to discuss it at all. This was a deliberate choice to keep him legible rather than cartoonish —
   a monologued confirmation ("yes, I destroyed him") would tip him into a Bond-villain register the
   bible's Lock #5 explicitly warns against. His silence on this one topic, after answering
   everything else in the scene directly, is the tell instead.
4. **No beat in this pass touches Nit, physical safety, or adds any scene of violence.** Checked
   against Lock #8 and the task's explicit "NO body, NO violence" constraint — confirmed clean.
5. **The bench's sale price and loss amount are new invention** (not sourced from the bible):
   sold at 2,200Φ under its registered valuation, four days after the assessment date. Flagging as
   invented specificity in service of "concrete, checkable" per the brief.
