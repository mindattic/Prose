# DWIACE Logic & Continuity Audit — 2026-07-03

## Verdict

Death Whispers in a Cat's Ear is in strong shape after today's SortKey surgery (rounds 1–3) and
the earlier debris-disable pass. Verified by full-text read: 561 enabled beats / 8 disabled beats
(the briefing's "558" is stale; 561 is current ground truth). The Celeste-interleave reorder
(Ch1 "Cel", Ch4 "What She Asked For", Ch7 "The Ghost Period" now sit at SortKeys 1, 8904, and
17802, matching bible §6's mandated 12-chapter order exactly, including Law §0 "the book opens
with Celeste") introduces **no investigator-knowledge leaks** — all three Celeste chapters were
read in full and contain nothing the reader hasn't already received in interleaved order. The
Sol=Mateo/Celeste=Jace mirror-reveal is airtight across all 7 beats that name Mateo, matching
bible §2's 2026-07-03 amendment exactly, with no contradiction anywhere in the node. The
evidence-chase beat relocated to SortKey 2450 (Beats.Number 4968) reads causally clean:
Analog's warning → Rennick's attempt → the team's pivot to field methods, with no later
re-litigation. The disabled "97–99" debris (SortKeys 4100/4200/4300) is confirmed genuine
abandoned draft material, correctly disabled, with zero live dependency. The one substantive
open issue is a genuine duplicate Pulse-departure sequence early in Ch1 ("Cel") — already
flagged by the prior audit and independently reproduced here — which is advisory-only per this
audit's scope and is not touched. One documentation-only staleness item is noted under Bible
Agreement (§6's structural note describes a now-superseded state of the book).

## 1. Causality

- [SEVERITY: low] SortKey 2450 (Beat Id `019F21AF-2A9E-72DB-8DF6-B0F209CF9F9E`, Number 4968) —
  CONFIRMED CLEAN, no defect. Full text verified: Analog's warning at SortKey 2400 ("If Celeste
  is offline by design at a reputable shop, [Meridian's] approach will look exactly like an
  aggressive corporate investigation... They're going to scare off the one person who knows
  where she is") is directly and immediately paid off at SortKey 2450 — Rennick tries the
  official/licensed channel himself (calling the mod-clinic on Ashland), and Meridian's "legal
  preservation order" banner locks the record while he's on the line watching it happen ("It's
  gone... it locked while I was in it"). SortKey 2500–2600 then shows the team explicitly
  pivoting to field/physical methods ("I'll start with the physical trail... and with whatever
  runs the corridor you find"). Full-text search for "preservation order" and "locked while I
  was in it" across all 561 enabled beats returns exactly one hit (the beat itself) — no later
  beat re-litigates this event. Fix: none needed; this is a correctly-executed relocation.
- [SEVERITY: low] SortKey 42600 (disabled Beat 3840, "Rennick locked the cabinet for the third
  time that day. Six items now...") — this beat is a near-verbatim duplicate of the content
  already carried by the surviving enabled beat at SortKey 42500 (Number 3832: "Rennick
  gathered it... and carried it to the cabinet... Neither of them said the thing the new file
  meant. That Rennick Investigations could now prove... that one thing had worn two dead faces
  and would wear more"). Confirmed via `NodeBeats FOR SYSTEM_TIME ALL` that 3840 was disabled on
  2026-06-28 — well before today's round-1/round-3 surgery and unrelated to it. Its absence
  leaves no causal gap: 3827 (Analog's 31-name column) → 3832 (Teller's stable signature, cabinet
  lock) → 3844 (case close-out) reads as one clean unbroken sequence. Fix: none needed.
- No other causality breaks found across the full preview-pass read of all 12 chapters (Cel,
  Intake, Room Holds, What She Asked For, What She Wouldn't Do, Clean Sharps, Ghost Period,
  Convergence, Same Cold, No Signal, Surfacing, Voluntary Recall).

## 2. Knowledge States

- [SEVERITY: none — clean] **Celeste-opens reorder (item a).** Read all three Celeste chapters
  in full: Ch1 "Cel" (SortKey 1.0–58.0), Ch4 "What She Asked For" (SortKey 8902.0–8998.0,
  including orphan beat 4392 at the chapter seam), Ch7 "The Ghost Period" (SortKey
  17802.0–17876.0). None references investigator-side knowledge the reader has not yet received
  in the current interleaved order:
  - No mention of "the Tributary" (the name Tamsin coins at SortKey 25900, Ch9 "The Same Cold" —
    five chapters after Ch7).
  - No mention of Sol Castellanos or Mateo (first surfaces in Ch9, after Ch7).
  - No reference to the Carrion Enterprises / Ashland-Cermak building identification (established
    in Ch6 "Clean Sharps," which the reader has already read by the time Ch7 opens — this is
    correctly *not* a leak, since Ch6 precedes Ch7 in read order).
  - The one item that reaches across the seam, orphan beat 4392 (SortKey 8902, "Cermak corridor,
    same afternoon... She'd seen the firm card... Rennick Investigations... She'll find the
    cold, that's all she'll find") only references facts the reader already has from Ch2/Ch3 —
    not a forward leak.
  - This independently corroborates the prior audit's own finding in
    `fixes/DWIACE/plan.md` §1 ("scanned every beat in both ranges for
    `Rennick|Tamsin|Teller|Corvin|Analog|Voss|Sol|Castellanos|investigat` — one hit, a false
    positive... No forward-references to the investigation found").
  - Fix: none needed.

## 3. Timeline

- [SEVERITY: high, DEFERRED — see full advisory writeup under §4] Two incompatible Pulse-departure
  sequences exist back-to-back in Ch1 "Cel" (item d). See the dedicated subsection under
  Plant/Payoff below.
- [SEVERITY: none] Chapter-boundary timeline checked against the bible's explicit "Day N of the
  Hartley case" labels: Ch10 "No Signal" and Ch11 "The Surfacing" are both explicitly labeled
  "Day 5 of the Hartley case" and read consecutively without a day-skip; Ch9 "The Same Cold"
  precedes them establishing the urgency that triggers Day 5. No timeline breaks found at any of
  the 12 chapter boundaries.

## 4. Plant / Payoff

- [SEVERITY: none — clean] Cross-chapter tells from bible §1 verified present and correctly
  triggered: Ch4's "I didn't like that room either" appears verbatim at SortKey 8930 (Beat
  Id `019EC976-3B33-7AC4-9855-A5E3DE99C060`, Number 3970). Ch7's "I've been waiting eight months,
  Cel" appears at SortKey 17868 (Number 4030), with the narration correctly landing the "wrongness"
  the bible describes ("the word *waiting* carried something *missing* did not... a resource
  expended, as a thing that had been spent and now needed a return on the spending").
- [SEVERITY: none — clean] The reveal-pivot at SortKey 25900 (Number 3842) plants "the Tributary"
  name cleanly for payoff through the climax (Ch11) and finale (Ch12) — see Bible Agreement below.

### DEFERRED — Duplicate Pulse-departure sequence (item d) — ADVISORY ONLY, NO ACTION TAKEN

Two incompatible tellings of Celeste's Pulse trip out of Evanston exist inside Ch1 "Cel":

- **Sequence A** (SortKey 13.0–14.0, Beat Numbers 4055/4054): compressed/alternate version. She
  is already "The Evanston Pulse terminus is six blocks south... At 02:50," books "Chamber 4" via
  the Manifold, boards a "luxury pod interior," and rides "680 miles an hour" for "four minutes"
  nonstop to "the Chicago Loop terminus, direct route, no stops." Ends on hope-posture with no
  arrival scene; the thread is never picked back up.
- **Sequence B** (SortKey 15.0–29.0, Numbers 3930–3944, continuing at SortKey 30.0+, Numbers
  4037+): fuller version. "Twelve minutes to the Pulse station at her pace" (walking), an ordinary
  pod ride ("window seat, the city side"), past "the Loyola curve" and the "Belmont maintenance
  platform," surfacing in "Uptown," arriving via the "Logan Square" stop, then walking to "the
  building with the rotating signage" — the exact destination the voice names at SortKey 18
  ("Logan Square stop, two blocks east on Kedzie... the building with the rotating signage") and
  the exact location the bar-assault/Inés-introduction scene (SortKey 30+) requires.
- Read straight through, these are chronologically incompatible: SortKey 12 has her still walking
  through her suburb at 02:43; SortKey 13–14 has her already boarded and mid-transit; SortKey 15
  restarts with "Twelve minutes to the Pulse station," implying she hasn't left yet. The stated
  destinations also conflict ("Chicago Loop terminus" vs. a Logan Square arrival by foot).
- This reproduces, independently, the same anomaly the prior audit already flagged in
  `fixes/DWIACE/plan.md` §1 ("beats 4055 and 4054... interleaved... into the middle of what reads
  as a separate, more granular walk-to-the-station-and-ride sequence... she appears to depart
  twice... Not touched in this plan").
- **Advisory recommendation (editor call, not executed here):** Sequence B (3930–3944/4037+) is
  load-bearing — it alone delivers the Logan Square destination that every subsequent beat in the
  chapter depends on, and it carries the book's characteristic cat-ear sensory texture. Sequence A
  (4054/4055) is well-written but self-contained; it neither resumes nor connects forward, and its
  "Chicago Loop terminus" destination is never referenced again. If cut, Sequence A is the more
  removable pair; if kept, it may read better repurposed as a flash-forward cold open rather than
  sitting mid-scene. No change has been made — this is advisory only, per the task's instruction
  to make a comparative recommendation without executing a fix.

## 5. Orphan References

- [SEVERITY: none — confirmed clean] **Disabled 97–99 debris (item e).** Read the disabled
  beats at SortKey 4100.0 / 4200.0 / 4300.0 (Beat Ids `019EC40C-966A-7DAD-9D05-690A0B13E55A` /
  Number 3404, `019EC40C-A715-712B-9C5C-0A23435040C6` / Number 3407,
  `019EC40C-B6D0-7CD5-901F-D7EE7AD75BDF` / Number 3410) in full. They are an abandoned alternate
  draft of a room-entry beat: they re-describe *entering* Jace Dalton's apartment ("Through the
  door she felt warmth... She opened the door the way she'd entered the building") immediately
  after the enabled beat at SortKey 4000 already has Tamsin inside, mid-read, holding the folded
  paper ("She crossed to the counter and picked up the folded paper"). SortKey 4300's text
  explicitly says "In a missing girl's bedroom" — content that cannot belong to this scene (Jace
  Dalton is a dead adult man, not a missing girl). With the three disabled, SortKey 4000 → 4400
  flows directly and cleanly ("...picked up the folded paper" → "The paper held two paragraphs").
  Full-text search of all 561 enabled beats for "missing girl" returns exactly one hit, at
  SortKey 19500 (Number 3912) — Rennick's line "It stopped being *find the missing girl*. It's
  *reach the one we can still reach*..." — a legitimate, unrelated rhetorical use about reframing
  the whole case around Celeste, with no connection to the disabled fragment's non-existent
  case. Confirmed fully excised with zero live dependency. Fix: none needed.
- [SEVERITY: low, informational] Orphan beat 4392 (SortKey 8902, "Cermak corridor, same
  afternoon...") carries a Beats.Number far outside its neighbors' range — a global
  auto-increment artifact per `fixes/DWIACE/plan.md`'s diagnosis, not a data-integrity defect.
  Content-wise it is fully integrated at its current position (bridges Ch3's ending to Ch4's
  opening) and is not an orphan in narrative terms. No fix needed.
- [SEVERITY: low, informational] Five additional disabled beats exist outside the requested
  97–99 scope (SortKey 9700/Number 3422, 17100/3596, 24100/3747, 42600/3840, 51500/3989).
  Spot-checked in full: each reads as legitimate, complete narrative content disabled for
  redundancy, not drafting debris — e.g., the disabled beat at SortKey 51500 (Number 3989,
  "Outside Ines's shop... she could hear the canal before she could see it...") is a near-verbatim
  duplicate of the enabled beat at SortKey 8952 (Number 3987, "She had heard the canal before she
  could see it — two blocks over..."), and the disabled beat at SortKey 42600 (Number 3840,
  covered under Causality above) duplicates enabled SortKey 42500 (3832). No enabled beat depends
  on any of the five. Flagging for completeness only; outside this audit's requested scope, no
  fix recommended.

## 6. Bible Agreement

- [SEVERITY: none — clean, strong pass] **Mirror-reveal logic (item c).** All 7 beats naming
  "Mateo" were read in full: SortKey 23800 (Number 3731, Tamsin first reads the name), 24600
  (3774, recap: "the name. Mateo. Not Jace — a different dead person"), 24700 (3779, physical
  evidence: "a dead brother named Mateo"), 25100 (3800, explicit: "'Mateo,' she says. 'A brother,
  not a boyfriend. Dead before her. She died reaching for him.'"), 25200 (3808, Teller's cadence
  match: "Sol's brother Mateo answered her, in the cache, the same way Jace answered Celeste...
  It is one author wearing two faces"), 25900 (3842, the reveal-pivot: "Confirmed two faces —
  Jace Dalton (Hartley), Mateo Castellanos (Castellanos) — one author... Proposed tag: the
  Tributary"), and 31500 (3828, Rennick to Celeste: "She heard her brother Mateo, the way you
  heard Jace... Not Jace. Not Mateo. The same voice... used a different face"). All seven agree
  with each other and with bible §2's 2026-07-03 amendment without exception: Sol's dead loved
  one is consistently her **brother** Mateo (never boyfriend), Celeste's is boyfriend Jace, and
  the reveal is consistently framed as "one author, two faces" / "the same voice... used a
  different face" — i.e., the mirror is the shared WANT (reaching a dead loved one), not the
  relationship-type, exactly as the bible amendment specifies. No contradiction, no muddying
  found anywhere in the node.
- [SEVERITY: none] **Structure.** The DB's 12 `IsChapterStart=1` chapters, in SortKey order (Cel
  → The Intake → What the Room Holds → What She Asked For → What She Wouldn't Do → Clean Sharps →
  The Ghost Period → The Convergence → The Same Cold → No Signal → The Surfacing → Voluntary
  Recall), exactly match bible §6's mandated order — including Law §0, "the book opens with
  Celeste," which is now true in the DB (Ch1 "Cel" at SortKey 1.0). This was previously violated
  (per the prior structural audit, which found the reader opened on Isaac's office and didn't
  reach Celeste's POV until 83% through the book); today's interleave fix resolves it completely.
- [SEVERITY: low] Bible §6's "Note on structure" paragraph (docs/nodes/DWIACE.md lines 185–188)
  is now stale: it states the Celeste child strands are "SEPARATE strands not yet stitched into
  the book-strand beat sequence" and cites an outdated SortKey (~47500, ~84%) for Celeste's first
  appearance. The DB has since been fixed — Celeste opens the book at SortKey 1.0 and recurs at
  Ch4/Ch7. Fix: update bible §6 to describe the current interleaved structure, and consider
  flipping USER_STORY DWIACE-US-5 (currently ⬜) to ✅ given the merge is now reflected in the DB.
- [SEVERITY: none] **Finale.** SortKey 42700–43300 fully honors bible Law 5 ("The corpo buries
  it... Rennick keeps the evidence. The Tributary continues with Celeste's voice... the predator
  evolves and they know it") beat-for-beat: the close-out entry buries the true findings behind
  "voluntary recall" while the real case sits "four inches thick behind a lock with no Network on
  it" (3844); the Tributary retains a fragment of Celeste's voice from the interrupted procedure
  (3849) and uses it on a new victim in Andersonville three weeks later (3853, 3857); Rennick's
  passive scan catches the new death forty-one days after the case closed and writes "same
  method, different face" (3861); and a new client arrives on the stairs, closing the loop on the
  agency motto ("The case would come in cold. They would read it anyway," 3865/3867). No issues
  found.
