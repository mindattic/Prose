# ATTE Structural Fix Plan — Chapter 4 Replacement

Ruling implemented: the BIBLE ending (`docs/nodes/ATTE.md` §1, §5 Ch4) is canonical. The
schism-rescue climax currently live in the DB (beats 26-41 by audit numbering, SortKeys
1250-2000) violates §0 ("NOT a rescue story"), Lock #3 ("children not recovered on the page"),
and the LOCKED §4b physics ("no inside accessible to us"), and contains the arithmetic error
identified in the audit (Finding 3: 47 -> 24 crossed at the 20-second mark -> "twenty-three
still inside" -> final tally of 22, narrated as "one child unaccounted for"). That whole
sequence is disabled below and replaced with the bible's Nadia -> Bear -> folder -> locker ->
filed report ending.

All SortKeys below use the same 50-unit spacing convention as the existing story. Beat IDs are
the DB `Beats.Id` (GUID) pulled read-only via the SELECT in the task brief. This plan does not
touch the database; it specifies what a follow-up DB pass should do.

## Beat map

Sequential beat # is the audit's 1-41 reading order. SortKey is `NodeBeats.SortKey`.

| # | SortKey | Beat Id | Chapter tag | Action | Note |
|---|---------|---------|-------------|--------|------|
| 1 | 45 | D57B6F3A-4E1B-40DB-A128-63E5FD578F6D | (Prologue) | KEEP | Gateway beat, zone-seam. Caseload "three disappearances" established. No change. |
| 2 | 50 | 019EBF4C-95E2-74CC-B749-7DEECF176DB0 | Chapter 1 | KEEP | Case numbers 45/46/47 established (registry running "into the high forties" — this is the tell, not a bug: the last of Yemina's 3 case numbers happens to equal the citywide total revealed later). |
| 3 | 100 | 019EBF4C-AB02-7FB8-A0BD-9E0473280542 | — | KEEP | |
| 4 | 150 | 019EBF4C-C800-70D9-816B-6B5E16A57494 | — | KEEP | |
| 5 | 200 | 019EBF4C-E3D1-7B79-9FCF-26B8F95C9AB9 | — | KEEP | |
| 6 | 250 | 019EBF4C-FE55-74FE-86AE-8A0C127CDD12 | — | KEEP | |
| 7 | 300 | 019EBF4D-1EC4-79A3-91C9-AD421B157A9B | — | KEEP | |
| 8 | 350 | 019EBF4D-39D4-7EA0-95F7-DD938A7545FC | — | KEEP | Ren names Dembe and Priya as two other children he tracked informally — distinct from Yemina's 3-case caseload. No contradiction; left open per "keep whodunits open." |
| 9 | 400 | 019EBF4D-602E-7F00-BCBE-34D88C58D8D4 | — | KEEP | |
| 10 | 450 | 019EBF4D-7E77-7B15-8429-3AF3AFC77C8F | — | KEEP | |
| 11 | 500 | 019EBF4D-96AB-7C01-8F17-9DDF62289ED8 | — | KEEP | |
| 12 | 525 | 222A3BC2-FEB4-43DC-A253-ED5F8D0A81A1 | — | KEEP | |
| 13 | 550 | 019EBF4D-AD81-777A-B8CC-B05947B0B519 | Chapter 2 | KEEP | |
| 14 | 600 | 019EBF4D-D87F-7DCF-ADB3-797B2AADAD39 | — | KEEP | |
| 15 | 650 | 019EBF4D-F07F-7730-9D90-76D48FA0141C | — | KEEP | "The intruder" first appears here. Out of scope for this fix (see Note on the intruder, below). |
| 16 | 700 | 019EBF4E-170A-7BFB-8449-0BB51E85EB02 | — | KEEP | "47" pattern first surfaces (0.3s query latency). Consistent with bible §4b LOCKED 47-child pattern. |
| 17 | 750 | 019EC176-C425-7323-B5C5-D94CC6B570B5 | — | KEEP | Chinwe Bramley (Kito's grandmother) introduced. Reused in NEW beat 2 below. |
| 18 | 800 | 019EC176-F14B-7DBD-A26E-2E709D433029 | — | KEEP | Osei/Lech kitchen beat; hand-rewritten SS-A18 pass per bible §6 provenance note; do not touch. |
| 19 | 825 | 019F21AE-6FB8-72C6-80E9-B7950F1F05D6 | — | KEEP | Sealed counseling record (dangling thread — audit Finding 8). Out of scope for this fix; the ruling covers the ending/physics/headcount, not this separate thread. |
| 20 | 850 | 019EC177-1BF0-7EC9-BC76-D0695E23B468 | — | KEEP | "Forty-seven pediatric care records" — consistent with citywide total. |
| 21 | 900 | 019EC177-3FEF-7E0C-A8D4-93863487F269 | — | KEEP | Selvamani's paper discovered. |
| 22 | 1050 | 019EC177-B493-7DB7-B8B9-863572F2DD0B | Chapter 3 | KEEP | Meets Selvamani. Mechanism reveal is LOCKED canon (§4b) — Gingerbread House, echo, heading. Keep. |
| 23 | 1100 | 019EC177-E509-70C8-9D25-FF54E5361DE9 | — | KEEP | "22 school sites / 22 months" — matches bible §4b exactly. |
| 24 | 1150 | 019EC178-124F-771D-B8D0-D817C8C2B02E | — | KEEP | "Twenty-two school sites" restated — consistent. |
| 25 | 1200 | 019EC178-3745-7E0D-B19B-933E0BC724B4 | — | KEEP | AAMA call. Ends "Yemina went back inside." Clean act-out for Chapter 3 — repurposed as the hinge into the new Chapter 4. The 72-hour window and the chalked "HERE" are left unresolved, which is correct GREY-register practice; do not pay them off. |
| 26 | 1250 | 019EC178-5F55-7A77-B122-C31259DA87E3 | — | **DISABLE** | Sleepless night -> decides to recruit Ren for a physical crossing. This is the pivot from investigator to rescuer; violates Lock #2 and Lock #3. Strong GREY texture (tea made and not drunk, the airshaft apartment) salvaged into NEW-1 below. |
| 27 | 1300 | 019EC178-8CB8-75E7-B66B-77C0BA8BC112 | — | **DISABLE** | Calls Ren, recruits him for the crossing. |
| 28 | 1350 | 019EC178-C002-777C-BE4C-93ED038B5E46 | — | **DISABLE** | Meets Selvamani at dawn; the modulator; "hold the frequency from the inside." Rescue-mission planning. |
| 29 | 1400 | 019EC178-EF2D-7095-BB5A-98F78C5F17D0 | — | **DISABLE** | Arrival at 35th and Halsted; physical description of the schism as a walkable, enterable-adjacent site. Contains strong containment/bureaucratic-honesty texture ("the cones were the protocol's honest face") — salvaged as a generalized reflection (not tied to a physical site visit) into NEW-1. |
| 30 | 1450 | 019EC179-1AC7-7EDA-8F29-A39CACBC9E40 | — | **DISABLE** | Ren lifted toward the schism; Yemina hauls him back. Physical rescue action; contradicts §4b (no inside, no "held" bodies at a threshold). |
| 31 | 1500 | 019EC179-3A80-77FE-A397-0F6547086213 | — | **DISABLE** | Ren and then Yemina cross the threshold. Direct violation of §4b ("no inside accessible to us"). |
| 32 | 1550 | 019EC179-5866-718F-9663-3AE4A73188E0 | Chapter 4 | **DISABLE** | Walkable interior ("a room that would not be cross-referenced"). Contradicts §4b. This SortKey/Chapter-4 tag is reused for the new bible-compliant Chapter 4 opening beat. |
| 33 | 1600 | 019EC179-73C0-7998-924E-9CB5C7E7A5DF | — | **DISABLE** | Finds Kito inside the interior. Violates Lock #3 (children recovered on the page). |
| 34 | 1650 | 019EC179-9788-7C3E-8299-23424CDDD9F0 | — | **DISABLE** | Kito explains the "two things" / the quiet signal knew Yemina's name. Interior-space material, contradicts §4b. |
| 35 | 1700 | 019EC179-B416-7307-8525-A95D7D57365D | — | **DISABLE** | Children stand and follow Yemina's command. Rescue-in-progress. |
| 36 | 1750 | 019EC179-DDB2-71B1-AFD8-67771A4DB169 | — | **DISABLE** | Ren gives evacuation orders; Selvamani starts the "Sixty" countdown. Genre-thriller register break (audit Finding 7). |
| 37 | 1800 | 019EC179-FEF0-796C-828A-A2106C8835A3 | — | **DISABLE** | First children cross. |
| 38 | 1850 | 019EC17A-2F70-7A70-8B22-45331AAC9082 | — | **DISABLE** | Wrist-hauling extraction; "twenty-three children are still inside" — root of the headcount error (audit Finding 3). |
| 39 | 1900 | 019EC17A-5EE3-799D-800F-8E509F7BABDD | — | **DISABLE** | Light dies; "twenty-two, not twenty-three" — the uncorrected arithmetic contradiction. |
| 40 | 1950 | 019EC17A-8507-79C4-9167-7A0B7B5E88B4 | — | **DISABLE** | Introduces "the Dead Realm," "third sublevel," "Axiom" — undefined, cross-arc material never established in the bible or anywhere else in ATTE (audit Finding 4). Gateway-breaking per §0. |
| 41 | 2000 | 019EC17A-D25F-7A68-9FB5-B5D4935CFD0D | — | **DISABLE** | Ends mid-decision on a second unresolved rescue. No filed report, no institutional handoff — the opposite of the bible's ending. |

**Disabled: 16 beats (SortKeys 1250-2000, audit beats 26-41).**
**Kept: 25 beats (SortKeys 45-1200, audit beats 1-25), unchanged.**

## New beats (Chapter 4 rewrite)

9 new beats, SortKeys 1250-1650, reusing the freed range. All NEW, no existing Beat Id (to be
created via `ss --beat create` / MCP `create_beat` in a follow-up DB pass — this deliverable is
files only, no DB writes were made).

| Order | SortKey | Slug | BeatTitle | Synopsis |
|---|---|---|---|---|
| 1 | 1250 | `the-report` | Chapter 4 | Yemina writes the full case report at her kitchen table. The AAMA's 72-hour window has passed with no confirmation either way — she never learns if the site was sealed. She reaches the report's disposition field and has nothing to put there. Salvages the tea-made-and-not-drunk procedural-anchor texture from the disabled sleepless-night beat (SK1250) and a generalized, non-site-specific version of the "the cones are the protocol's honest face" reflection from the disabled arrival beat (SK1400) — recast as Yemina's abstract knowledge of how Class-3 containment always looks, not a return to 35th and Halsted. Plants: her report's intake confirmation comes back in a latency she recognizes — the same wrongness as the earlier 0.3-second query — gesturing at the unexplained "intruder" thread without resolving it. Ends: she is told, by a clipped automated notice, that a file of this classification requires physical courier delivery, not digital submission, to a location and time she's given: a freight dock, 5:40 AM. |
| 2 | 1300 | `chinwe-bramley-notice` | (none) | Before delivering the folder, Yemina makes one last visit to Chinwe Bramley to deliver an official notice of continued-open status. No answers given — cannot be given. The horror is entirely procedural: a form that says the case remains open, delivered by hand, to a seventy-three-year-old woman, with nothing underneath the words. Bookends the kept SK750 visit. Does not reference the schism's mechanism or interior — honors Lock #3. |
| 3 | 1350 | `the-dock-gate` | (none) | 5:40 AM. Yemina drives to the freight dock — an echo of the prologue's zone-seam opening. She half-expects an intake window or a drop box; instead there is a loading gate, dockworkers starting a shift, and a woman standing apart from them with a bag pressed to her chest. Yemina hands over the folder as instructed. The woman — Yemina catches her name off a shift-manifest clipboard, "Nadia," nothing more — takes it and says almost nothing, and does not walk toward any door marked intake. |
| 4 | 1400 | `bear` | (none) | Nadia crosses the dock floor, not to an office, but to a big man in a dock coat whom the other workers call "Bear" — easy laughter with a coworker over a hand-truck a moment before, the register of a man well-liked, boisterous, at home in his body. Nadia holds the folder out. He takes it. The dock noise continues around them; nobody else registers the handoff as anything unusual. Yemina, watching from the gray car, understands — with the specific cold of a fact arriving in the wrong order — that this was never going to a records office. |
| 5 | 1450 | `four minutes` | (none) | LOCKED beat. Bear does not open the folder. He stands with it at his side, then holds it in both hands without looking down at it, for four minutes — Yemina times it without deciding to, the same reflex that has clocked every gap in this case, and for the first time in the story she does not write the number down. Obligation, not guilt, in how he stands — the posture of a man doing a favor whose weight he already understands and has already accepted, not a man wrestling with whether to look. |
| 6 | 1500 | `the-locker` | (none) | Bear crosses to a bank of dock lockers, opens one, sets the folder inside — not hidden, not hurried, filed the way a man files a thing he has done this kind of favor with before — and closes it. No key, no additional ceremony. He goes back to work. Nadia is already gone. Yemina realizes she does not know, and has no way to find out, which locker, whose locker, or where a folder goes from a dock-gate locker in the Pilsen Veil. |
| 7 | 1550 | `no field for this` | (none) | Yemina drives away without approaching either of them — consistent with Lock #2, she doesn't fight the system, she reads it. She runs the morning's sequence back the way she runs everything: what she saw, in the order she saw it, without a theory of what it means. She has spent her whole career certain that careful documentation was the only lever she controlled. She files this — the dock, the woman, the big man, the four minutes — under a heading her own private log has never had to use before: *unconnected. no further action available at this clearance.* |
| 8 | 1600 | `the car stays gray` | (none) | Callback to Lock #5. The gray car does not become significant; it is still just the car. She drives the seam back toward the district office to close out the paperwork she can close: Ren's file marked interviewed-cooperative, Selvamani's contact marked consulted-external, the AAMA voicemail's confirmation number logged. Three names. Three case numbers. One of them — Kito's — she leaves open, because leaving it open is the only true thing she can enter in that field. |
| 9 | 1650 | `she drove` | (none) | Closing beat. Spare, mirrors the prologue's closing cadence. The report goes into the district intake queue and reorders itself among the other unremarkable filings the system processes every morning — no fanfare, no confirmation that anyone above her reads it differently from a truancy report. She does not know where it goes after that. She had a caseload. She still has one. She drove. |

**New: 9 beats, total draft ≈ 7,700 words** (measured directly from the drafted files in
`beats/`), against ≈11,700 raw words in the 16 disabled beats (that figure includes DB
metadata/newline tokens; actual disabled prose is somewhat lower but still the larger side).
This is roughly two-thirds the length of the material it replaces — proportionate for a chapter
the bible itself specifies as a compact, spare scene (its entire §5 description is one short
paragraph, versus multi-paragraph descriptions for Chapters 1-3), not a beat-for-beat word-count
match.

## Headcount decision

**No numeric patch needed in kept material.** Checked every kept beat (1-25, SortKeys 45-1200)
for numeric references to affected-children counts:

- Yemina's personal caseload: **3** (Kito, 9; Daria, 7; a third whose name is never given in the
  kept text) — established in beat 2 (SK50, "three names... case numbers 45, 46, 47") and
  restated in the prologue (SK45, "Three names she hadn't met").
- Citywide pattern: **47** — established beat 16 (SK700, "a GLMZ-wide pediatric neuretics
  search... rolling twenty-four months"), restated beat 20 (SK850, "forty-seven pediatric care
  records") and matches bible §4b LOCKED ("The 47-child pattern").
- Site distribution: **22 school sites / 22 months** — beat 23 (SK1100) and beat 24 (SK1150),
  matches bible §4b exactly.

These three numbers (3 / 47 / 22-and-22) are consistent everywhere they appear in the kept
beats and match the bible's LOCKED canon. **The only numeric contradiction in the story (47 ->
24 crossed at 20 seconds -> "twenty-three still inside" -> final tally of 22, audit Finding 3)
lives entirely inside the disabled range (beats 38-39, SortKeys 1850-1900)** and is removed by
disabling that material — it does not need to be patched because nothing carries the number
forward into the new ending. The new Chapter 4 beats do not state or imply any extraction count;
they never claim any number of children were physically moved, consistent with Lock #3.

**No kept beat requires a numeric edit.**

## Salvage notes

- **Tea-making procedural-anchor texture** (disabled SK1250: making tea at 2 AM, letting it go
  cold, "making it had been the point") — reused, adapted, in NEW-1 (`the-report`) as the anchor
  for report-writing instead of pre-rescue dread.
- **"The cones are the protocol's honest face" reflection** (disabled SK1400) — the specific
  insight that inadequate containment measures are "the protocol's honest face," not a failure
  of it — is strong GREY-register material. Recast in NEW-1 as a generalized piece of Yemina's
  professional knowledge (what Class-3 containment always looks like, from her broader
  fifteen-year record, not a return visit to 35th and Halsted) so it doesn't reintroduce a
  physical site visit.
- **Everything else in the disabled range (Selvamani's modulator, Ren's tremor-as-instrument,
  the countdown, the interior room, the Dead Realm/Axiom material) is not salvaged.** It is
  either specific to the crossing itself (inseparable from the lock violation) or explicitly
  flagged by the audit as gateway-breaking (Dead Realm/Axiom, Finding 4).

## Out-of-scope items noted but not touched

Per the brief's explicit ruling, this pass fixes the ending, the headcount, and the physics
violation only. Two other audit findings are real but out of scope here and are left for a
separate pass:

1. **Finding 6 (the "intruder" does too much of Yemina's detective work)** — the intruder is
   established in kept beats (15, 16, 20, 21) prior to the disabled range and is not part of the
   rescue climax. NEW-1 gestures once at the same unexplained agency (the report's intake
   latency echoing the earlier 0.3-second query) to give the thread a consistent presence
   through to the ending, but does not resolve or expand it — resolving it is a separate
   editorial decision the ruling didn't ask for.
2. **Finding 8 (the sealed counseling record, beat 19/SK825, is a dangling implication of a
   human antagonist)** — untouched; also out of scope for this ruling.

## Bear's characterization

Per bible §3: Bear "does not open it for four minutes. This is the whole scene," "does not
appear again in the story," and the four-minute wait is "obligation, not guilt — that is the
soldier's move." Per gateway design (§0, zero prior knowledge required), Bear is not named as
Boris Johansen on the page and his TEST-strand background is not referenced — Yemina only hears
him addressed as "Bear" by a coworker, consistent with a reader needing zero cross-strand
context. NEW-4 (`bear`) establishes his baseline warmth (easy laugh, well-liked on the dock)
before the folder changes his register, so the four-minute stillness in NEW-5 reads as a
contrast the reader can feel rather than a flatly-written non-event.
