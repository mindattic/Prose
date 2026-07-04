# Logic/Continuity Audit — TEST (Testament / Bear Court-Martial)

Node slug: `the-court-martial-019ed361` | Node Id: `019ED361-1665-7B50-870D-ED68D2F673DF`
43 enabled beats read in full (SortKey 50 → 1050), plus 3 disabled beats (SortKey 525, 818.75,
853.125) checked for orphan references. Bible read at `docs/nodes/TEST.md` (the `docs/nodes/`
path exists; `docs/strands/TEST.md` does not).

**Verdict: 6 BLOCKER / 7 MODERATE / 5 MINOR findings.**

This is not "logic sound." The prose itself is tightly plotted on the big causal spine (Hana's
discovery mechanism, the leverage analysis, the two-Sunder-discharge budget, the salute payoff,
Brandt's NS-7 death) — that machinery holds up beautifully. The problems cluster in three
places: (1) one hard numeric contradiction in the Manowar's post-lock charge state, (2) a
geography/timestamp snarl in the final approach to the hearing, and (3) the bible (`docs/nodes/TEST.md`)
being substantially out of sync with the live text — stale SortKeys, a missing salute beat, a
misquoted locked line, an exceeded lock on the yellow-jacket and ventilation-fan motifs, and an
entire unmentioned ending (the last ~10 beats, including "The Fourteen Days" arc, don't appear
in the bible's beat spine at all).

---

## 1. Causality chain

Walked all 43 beats cause→effect. The spine holds together well:

- Brandt's courtesy call (SK75) → Bear's leverage analysis (SK450/500) → decision for testimony
  over insubordination/flight/extraction — each option is explicitly weighed with reasons before
  being rejected. No unmotivated jumps.
- Hana's arrival (SK350) is causally downstream of the financial trail Bear himself narrates
  (SK150, SK300) — she found him via the sixteen-transfer rhythm, and she says so herself
  (SK350: "You were the only one who kept paying on the twenty-first"). Clean plant/payoff.
- The two-Sunder-discharge budget for the day is set up in advance (SK660: "saving itself for
  the two times today it would be asked to fire") and spent exactly on schedule (SK685, SK697.5),
  confirmed spent at SK900 ("The two morning discharges had run the system to zero"). This is
  good, disciplined bookkeeping.

Two causality gaps found:

- **MODERATE.** Ledger's ability to insert a document into an *official IRB exhibit record*
  ("Whatever Hana filed under PE-1139 — put the transfer manifest next to it. Same exhibit.
  Same case number," SK950) is a capability never established earlier. Every other appearance of
  Ledger (SK670 — dead-drop courier; SK900 — relays/annotates a leaked motion PDF) characterizes
  Ledger as an information *broker/courier*, not someone who can amend a live judicial exhibit.
  **Fix:** either add a one-line earlier establishment of Ledger's record-access/hacking
  reach, or soften SK950's phrasing to something more clearly leak-based ("got it into the same
  public feed, right below PE-1139" rather than "same exhibit, same case number").
- **MODERATE.** SK900 introduces "forty-three residents" being relocated *out of* "the Cortland
  site" in the present day. The original forty-three are established as dead on the page
  (SK400, SK745.3125, SK746.09375 — "the second-floor window went quiet," "enemy combatants
  deceased"). SK900 is presumably describing a *different*, currently-living population at a
  site that kept the same registered capacity — but nothing in the text says so explicitly, and
  the reuse of the loaded number "forty-three" for a second, unrelated group reads like the same
  people are somehow still alive. **Fix:** one clarifying clause in SK900, e.g. "a new
  forty-three, the building filled again since," to block the misread.

## 2. Knowledge states

Traced what each named character knows and when:

- **Bear:** Knows the yellow-flag classification item and the "nine unread lines" from the night
  of Cortland (SK200) onward; doesn't fully act on that knowledge until SK350 (Hana) gives him
  the "means." Consistent — he references the same nine lines in testimony (SK725) that he read
  in flashback (SK200).
- **Brandt:** Knows the manifest was classified before the raid (implied throughout, confirmed
  by his dropped defense at SK695 — "the slight forward weight-shift of a man building toward a
  clause... He dropped it"). No knowledge anachronisms found for Brandt.
- **Hana:** Introduced at SK350 already holding the case ("I built the case without you").
  **Her exhibit-introduction mechanism is not uniform, but this is a feature, not a bug** — she
  uses three distinct, appropriately different channels for three distinct pieces of evidence:
  (1) a private case file shown to Bear personally, out of the record (SK350); (2) filed
  documentary exhibits PE-1139/OD-7704, presented orally at her own separate public IRB hearing
  39 days later (SK875 — "she let them find it in the exhibits she had filed," implying
  pre-filing, then oral argument); (3) the post-closure transfer manifest, added off-books via
  Ledger (SK950) rather than through Hana at all. These don't contradict each other — but see
  MODERATE finding above re: mechanism (3)'s plausibility.
- **Declan Iyengar:** Consistently a step behind what he needs (blocks Bear's oral recitation of
  the sixteen names at SK748.4375, files the dismissal motion at SK900) — no anachronistic
  knowledge.
- No character is shown acting on knowledge they were never shown acquiring, aside from the
  Ledger capability gap noted above.

## 3. Timeline

**Panel day clock (all internally consistent once the SK697.5 error below is corrected):**
0500 wake → transfers cleared before 0600 → Ironbend visit → 0600 wall-gap recon/insurance-drop
(SK670, "six hours until the panel") → 1158 turn away from Ironbend (SK680) → 1159 kill order
called → 1237 freight-bay breach (1158+38min, matches "thirty-eight minutes" estimate exactly)
→ 1247 "inside" / fight 1 (SK685) → ~1249-1251 Declan exchange (SK690, "nine minutes early") →
1253 Brandt in lobby (SK695) → fight 2 (SK697.5) → 1301 submission room (SK700) → 1312 gang
crew breaks in (SK750) → 1327 real Atlas III team breaches → 1344 submission closes/handshake
→ 1344:43 compliance lock engages → 2045 Brandt collapses → 2103 NS-7 auto-report → next
morning apartment-owed transfers.

- **BLOCKER.** SK697.5 states: "The Strix held one discharge and six hours of walking before
  the next, **and he had a hearing at eleven**." Every other beat in the story (SK660: "The
  panel was at 1300"; SK690: "The panel was at 1300. He was nine minutes early"; SK700: "Bear
  was shown to the submission room at 1301") fixes the hearing at 1:00 PM, not 11:00 AM. This is
  a direct, reader-visible contradiction inside the climax sequence. **Fix:** change "eleven" to
  "one" (or "thirteen hundred") in SK697.5.
- **MODERATE.** Geography/sequencing snarl around SK680→685→690: SK680 has Bear breach a
  freight bay and become "inside" the building by 1247, walking "toward the main corridor."
  SK685's fight (2 Atlas II) happens in "the service corridor inside the freight level" —
  i.e., still inside. But SK690 opens with Declan "standing outside the IRB building entrance
  at 1247" and has Bear approach him from a distance ("Bear saw him before he was close enough
  to read the face"), ending with "he walked past him **into** the building" — implying Bear
  was outside the whole time. No beat shows Bear exiting the freight level back onto the street.
  A reader can't cleanly picture where Bear is between 1247 and 1251. **Fix:** add a one-line
  transition (Bear clearing the freight level, crossing a lot/plaza to the front steps) or
  revise SK690 so Declan meets him just past the service exit rather than "outside the entrance."
- **MINOR.** SK690 implies entry to the building at "nine minutes early" (1251); SK695 has
  "Bear came through the entrance at 1253" — a 2-minute gap, plausibly the walk from exterior
  door to lobby, but not stated.
- **MINOR.** SK50's narration mentions the cat yawning "at 0240" a paragraph *before* narrating
  the message flagged "at 0217" (earlier). Likely intentional pacing (flashback within the same
  scene) rather than an error, but a careful reader will notice the reversal.

**The ending's day-counts (extra item a coverage below) are internally consistent** relative to
each other: "The Fourteen Days" (SK950, response window) → "The Empty Apartment" (SK1000, "two
days after CE-0217 closed") → "The Nineteenth" (SK1050, "CE-0217 had closed six weeks ago").
2 days < 6 weeks, correct ordering. The "two months back" pattern-break reference in SK1050 is
an independent clock (surveillance-log retention, not case closure) and doesn't need to align
with the 6-week case-closure figure — no contradiction there once you recognize they're
different clocks.

## 4. Plant/payoff ledger

| Planted | SortKey | Payoff | SortKey | Status |
|---|---|---|---|---|
| Yellow rain jacket | 100 | Testimony callback ("I could have stopped at sixteen items... I wasn't going to leave it out") | 725, 737.5 | PAID — but see §6, over-length vs. bible lock |
| Account 11 redirect (girl, bursary) | 300 | "She had been the template... I am part of the chain" reflection; final transfer still shows 40% peeled off | 500, 660, 1050 | PAID |
| Meridian Cross erased from registry | 537.5 | Cross found face-down in apartment service locker | 1000 | PAID (see §6 for a bible-vs-prose contents mismatch) |
| Window estimate ("6-8 hours") | 500 | Executed almost exactly (39 min turn-to-breach, well inside window) | 680–700 | PAID |
| Lumen mouse's wall-gap knowledge | 660 (mouse "gone... two days ago") | Used to breach at SK670, reused at SK680 | 670, 680 | PAID |
| Crane operator's two-finger nod | 660 | "went to find the crane operator who still owed him a nod" | 1050 | PAID |
| Ledger dead-drop copy w/ 4th page (16 names), "for the one set of hands meant to find them if his didn't make it there" | 670 | *Never explicitly retrieved, destroyed, or referenced again* | — | **ORPHANED (MINOR)** — see below |
| Bear's intent to read 16 names aloud at panel | 748.4375 | Blocked by Declan; "He had somewhere else to say them" | 748.4375 | Redirected — paid off via a *different* mechanism (phone call to the Arrangement, SK1050), not the dead-drop |
| Sixteen names spoken aloud | 748.4375 ("somewhere else") | Read to the Ironbend/Arrangement dispatcher | 1050 | PAID |
| Declan's "ten minutes after, no record" offer | 690 | Never followed up on-page (reasonable — story ends before any such meeting; not a hole, just unresolved by design) | — | Open by design, not a bug |
| Hana's "I've been practicing this for two years" | 875 | Confirms she built the case independent of Bear (matches SK350's "I built the case without you") | 875, 350 | PAID |
| Two-discharge Manowar budget | 660 | Spent exactly at SK685 and SK697.5, confirmed zero at SK900 | 685, 697.5, 900 | PAID |

**MINOR finding:** the SK670 dead-drop's fourth page (16 names) is planted with real narrative
weight ("Ledger would know what to do with them") but the loop is never closed — Bear survives
and delivers the names himself via an entirely different channel (SK1050's phone call). This is
defensible as unused insurance, but a single line confirming Bear retrieved or discounted the
backup copy would tidy it.

## 5. Orphan references

Checked all 3 disabled beats (SortKey 525/#4132, 818.75/#4155, 853.125/#4147) against the 43
enabled beats for dangling references. **No issues found.** Each disabled beat is a superseded
alternate draft of a scene that has a live enabled equivalent covering the same story beat:

- SK525 (disabled) — an alternate "three months of terminal research" account of finding the 43
  families (mentions "twenty-six had no record"). This detail ("twenty-six") does not appear
  anywhere in the 43 enabled beats — confirmed via full-text search. No orphan reference.
- SK818.75 (disabled) — an alternate, unhurried "walked south to Ironbend at 1406" version of
  the post-testimony exit, superseded by the live SK825 ("walked north," decoy transponder in
  the IRB building, more urgent tone). Neither "1406" nor "walked south" nor the tower name
  "Cassava Veritas" appears in any enabled beat.
- SK853.125 (disabled) — an alternate, more detailed version of the hostel credentialing-node
  incident that's already covered briefly inside enabled SK850. No unique disabled detail
  ("Channeler," "forty-three years" as Bear's age) is referenced by any enabled beat.

## 6. Bible agreement

Read `docs/nodes/TEST.md` in full (`docs/strands/TEST.md` does not exist — the rename to
`docs/nodes/` has already happened for this file).

**BLOCKER — "He did not move. The room did." (Bible §4 item 3, "SK800, LOCKED, never expand or
soften") is absent from the live text.** Searched all 43 enabled beats and all 3 disabled beats
for this exact phrase and close paraphrases. It does not exist anywhere in the current prose.
The closest analog is at **SortKey 350** (not 800): *"The room didn't move. He had a body built
to make rooms small, and he kept it still, and the stillness was the only place the thing could
go."* This is a different scene (Hana's "you were the only one who kept paying" reveal, not the
SK800 "Come back"/handshake scene) and an inverted meaning (both room and man staying still,
vs. the bible's implied "man stays still, room changes around him"). **Fix:** either (a) update
the bible to cite SK350 and correct the quoted text to "The room didn't move," retiring the
"never expand or soften" lock on the nonexistent SK800 line, or (b) if the dramatic beat is
still wanted at SK800, write it in and lock the new sentence.

**BLOCKER — the "second salute" ("Beat-12") does not exist in the live text.** Bible §1 claims
"Three plants; one payoff" for the salute motif:
- Plant 1 ("Beat 5"): *"The briefing closed. Bear saluted with the others and filed out."*
  **CONFIRMED present**, verbatim, at SortKey 200.
- Plant 2 ("Beat 12"): *"Bear saluted — the same motion, the same angle, the same eight
  seconds it had always been. He dropped it and walked out into the Cortland morning."*
  **NOT FOUND anywhere** in the 43 enabled or 3 disabled beats (confirmed via full-text search
  for "saluted," "eight seconds," "same angle," "dropped it and walked"). The scene this should
  live in — the walk-off-base/court-martial-exit sequence — is now at SortKey 600/650, and it
  describes no salute at all; Bear simply "walked off the base at 1430 on a Tuesday."
- Plant 3 ("Beat 22"): quoted as *"He did not move his right hand. He nodded once. No salute."*
  The scene it's describing (Brandt straightening into posture in the lobby, Bear not
  reciprocating) **does exist**, at SortKey 695 — but the live text conveys it differently
  ("He didn't give it back... he held Brandt's eyes") and the quoted sentence does not appear
  verbatim.
- Payoff ("Beat 32", SK800): **CONFIRMED, strong match.** Brandt's full salute and Bear's
  handshake instead are present almost exactly as described.

Net result: the story currently delivers **two** salute beats before the payoff (one clean
match, one paraphrased/reworded), not three. **Fix:** either restore a one-line salute-and-drop
moment to the SK600 walk-off-base beat (compatible with lock #9's "no more fight beats" — this
isn't a fight beat), or rewrite Bible §1 to describe the two-plant structure actually on the
page.

**BLOCKER — yellow rain jacket exceeds its lock.** Bible §4 item 6: *"SK600 yellow rain jacket.
Two sentences; never expand."* The jacket's real location is **SortKey 100** (not 600 — SK600 is
now the insubordination/court-martial flashback and contains no jacket reference at all). More
importantly, the jacket receives far more than two sentences across the live text:
- SK100: "At the far end: a child's yellow rain jacket. He stopped the count there. He didn't
  want to be wrong about that jacket." — plus a full extra paragraph: "the jacket, small and
  bright and wrong against all that gray, hung by the hood the way you hang a thing you mean to
  keep dry for someone who can't yet be trusted to keep it dry himself. Bear had hung jackets
  like that. He knew the knot before he saw it."
- SK200: callback ("kept that part of his mind off the jacket, where it kept trying to go").
- SK250: callback ("He knew the child's jacket had been yellow").
- SK725/737.5 (testimony): re-described in full ("At the far end, there was a child's yellow
  rain jacket") plus a paragraph of meta-commentary on why he included it in testimony.
**Fix:** either relax the lock (the jacket clearly earns its recurrence structurally, echoed
purposefully across timelines) or trim SK100's second paragraph and the testimony elaboration
back toward the original two-sentence version, and correct the bible's SortKey citation.

**BLOCKER — Manowar frame weight contradicts the bible's own canon table (§6).** Bible §6 states
canonical Manowar weight as "+1,320 lbs / +600 kg" ("worn — external"). Live text: SK660 says
"six hundred kilos of powered frame" (**matches** bible exactly), SK425 says "twelve hundred
pounds of powered steel" (~544 kg, close enough to 600 kg — minor rounding, not flagged
separately). But **SK825 says "fourteen hundred kilos of powered frame"** — roughly 2.3x the
canonical 600 kg figure, and not reconcilable as a units slip (1,320 lbs ≠ 1,400 kg by any
conversion). **Fix:** change SK825's "fourteen hundred kilos" to "six hundred kilos" (or
"twelve hundred pounds," matching SK425/660).

**BLOCKER — Manowar post-lock charge state is internally contradictory** (see also §3/Extra
item (c) below for the full tally). SK850 explicitly states the accumulator will **never**
recharge after the compliance lock ("No warmth. No cycle... This wasn't new to him; he'd run it
to zero before, more than once, and waited through the slow hours while it climbed back to
warm. **This time he wasn't waiting for it to come back.**"). The very next enabled beat,
SK856.25, contradicts this directly: "he felt the accumulator sitting at thirty-one percent,
**climbing, the way it always climbed**, patient and indifferent to whether he wanted it to."
SK862.5 ("Manowar dormant across his chest") and SK1050 ("There was no cycle to feel. No low
banked warmth, no accumulator answering his palm... He'd stopped waiting for it to come back
months ago") both revert to the "permanently cold" framing established at SK850, making SK856.25
the clear outlier. The disabled beat SK818.75 independently corroborates the "permanently cold"
reading ("The Manowar was down to frame and dormant capacitor... cold against the inside of his
chest... he was, in his unhurried way, in the business of returning it"), reinforcing that
SK856.25 is the error, not the rule. **Fix:** rewrite SK856.25's line to match the dormant
framing (e.g., "he felt the quiet place on his chest, still and cold" instead of an active
percentage reading).

**MODERATE — bible's "SK920" apartment-locker contents don't match the live "SK1000" beat.**
Bible §5 spine: *"SK920 — SWAT at empty apartment: Service locker: stainless field-surgical
tray (transponder still pinging) + Meridian Cross, First Class, face-down."* Live text at
SortKey 1000 ("The Empty Apartment"): *"There was **one object** inside. A medal..."* — no
surgical tray, no transponder. The transponder decoy is in a **different building's** service
locker entirely (the IRB building itself, per SortKey 825: "he found the maintenance corridor
on the second floor and the bank of service lockers... He left his sub-license transponder
there"). **Fix:** update the bible to reflect that the apartment locker holds only the medal,
and the transponder decoy is a separate object in a separate location (SK825).

**MODERATE — bible's beat spine (§5) is stale relative to the live ending.** The bible states
"37 beats (~46 pages)" and its spine ends at "SK940" (described as "7-line comm message image
(seventh wished him well). CE-0217 closed non-actionable...") — this SK940 content (the
"7-line comm message," "seventh wished him well") does not appear anywhere in the current
43 enabled beats. The live text's actual ending is a five-beat sequence not mentioned in the
bible at all: Hana's own hearing (SortKey 875), the motion to dismiss (900), and the three
titled closing beats "The Fourteen Days" (950), "The Empty Apartment" (1000), and "The
Nineteenth" (1050). Several other bible SortKey citations in §5 no longer point at the content
described (e.g., "SK660: 19-day surveillance logged... turned wrong direction at 1158... kill
order at 1159" is now at **SortKey 680**, not 660; "SK450: the window estimate" content is
mostly at **SortKey 500**). **Fix:** regenerate the §5 beat spine against current SortKeys and
add the new ending sequence; this is the single highest-value bible edit from this audit.

**MODERATE — ventilation fan motif likely exceeds its lock.** Bible §4 item 7: "Ventilation
fan: 6 appearances total. Panel said 'overused' at 5. Do NOT add more." Counting all
"ventilation fan"/"ventilation cycle"/"ventilation duct" appearances across both timelines in
the live text: SortKey 200, 425 (×2 — fan stops, fan resumes), 600 (court-martial duct-tick),
725, 743.75 (testimony re-narration of the same 425 event), 745.3125 (×2 — "fan wasn't in its
cycle," testimony re-narration of the 0419 resume), 750 — **at least 9 distinct textual
appearances**, well above the stated cap of 6, even before counting the unrelated "exhaust fan"
(SK550) and "cooling fan" (SK900/motion-reading beat) mentions. It's possible the original
panel count used a narrower scope (e.g., present-day submission-room instances only); worth an
editorial recount against the locked cap rather than a blind trim.

**Verified as holding up well (no issues):**
- "He had decided this was tiredness." (SK812.5) — present verbatim, exactly as locked.
- "Halcyon Neural Services logged the incident as a hardware malfunction — component fatigue."
  (SK812.5) — present verbatim.
- Compliance lock at "forty-three seconds after contact breaks," due to depletion not a kill
  shot (SK900/825) — present verbatim and consistent with the two-discharge budget.
- "6 hours 41 minutes" between handshake and Brandt's collapse — present verbatim (SK812.5:
  "Six hours and forty-one minutes later").
- Brandt's NS-7 flag at "1348, four minutes after the submission hit the record" — the
  submission entered the record at ~1344 (handshake/lock time); 1344+4=1348. Arithmetic checks out.
- Bear's body stats (385 lbs augmented, 6'5"/6'8") match bible §3/§6 exactly.
- The salute payoff itself (Beat 32/SK800) matches the bible's description closely.
- Hana/Declan/Brandt characterization all match their bible-defined voice rules; no register
  violations found (Bear stays warm/large throughout, never slips into "counting machine" voice
  outside the deliberately-marked "log style" of his own testimony, which is textually justified
  on the page as a chosen rhetorical device, not a voice slip).

---

## Extra Items (a)–(c)

### (a) New ending consistency — "The Fourteen Days" → "The Empty Apartment" → "The Nineteenth"

- **Trial dates:** internally consistent. See §3 timeline above — 2 days (Empty Apartment) <
  6 weeks (The Nineteenth), correctly ordered relative to CE-0217's closure.
- **The manifest document:** the ending conflates several different documents under the loose
  word "manifest" — the compound's original yellow-flag registration (SK150/200/737.5), Hana's
  PE-1139 procurement order and OD-7704 operational directive (SK875), the warehouse's separate
  registration (SK600/746.875), and the new post-closure transfer manifest (SK900/950). None of
  these actually contradict each other — they're cumulative, distinct exhibits — but seeing
  "forty-three residents" reused for the transfer-manifest population (SK900) without
  distinguishing it from the dead is the MODERATE finding flagged in §1 above.
- **Mechanics of Hana's exhibit entry:** three distinct mechanisms across three distinct
  documents (see §2) — internally fine, except the Ledger/PE-1139-insertion capability gap
  (MODERATE, §1).
- **CE-0217 status:** consistent throughout — open/active during Bear's own testimony (SK700s),
  closed "non-actionable" via Halcyon's procedural motion against **Hana's separate petition**
  (SK950, day 13 of the 14-day response window). The irony is clearly intentional (truth enters
  the record; the case still dies on standing/jurisdiction) — not a bug.

### (b) Insurance-copy reconciliation (beat #4106 / SortKey 670)

The duplicate-sheets logic is **sound**: original 3 sheets carried in Bear's breast pocket
(SK660) for the panel; a second, identical 3-sheet copy plus a unique 4th page (16 names) is
written the same night and dead-dropped in the maintenance corridor at SK670, explicitly "for
the one set of hands meant to find them if his didn't make it there." Count and purpose are
clear and don't conflict with each other. The one loose end is the MINOR plant/payoff gap noted
in §4: the dead-drop copy is never explicitly retrieved or closed out on-page, since Bear
survives and delivers the 16 names by a different channel (SK1050's phone call) instead. Not a
contradiction, just an unacknowledged loose thread.

Separately (not part of the original #4106 ask, but discovered while checking it): **MODERATE**
— the *content* of the "three sheets" is described inconsistently between the packing beat
(SK660, framed as Bear's own laborious handwritten composition, emphasizing "no neuretics
trace... reducing eight years to three pages") and the testimony beats (SK737.5: sheet 1 is "a
copy of the third item... I have kept it for eight years"; SK746.875: sheet 3 is a warehouse
registration "pulled" from civil records within the last month). Sheets 1 and 3 are revealed to
be retained/acquired copies of official documents, not something written out by hand from
scratch — in tension with SK660's emphasis on the physical difficulty of handwriting them.
**Fix:** clarify in SK660 that Bear is transcribing/summarizing (not literally copying) the
source documents' content into his own three pages, or acknowledge that sheets 1/3 are physical
document copies riding alongside a handwritten cover framing.

### (c) Bear's Manowar charge ledger

Tally of every discharge/accumulation reference, in story order:
1. SK75 (separate earlier day, not part of the panel-day budget): "accumulation at thirty-four
   percent" — isolated flavor beat, doesn't need to reconcile with panel day.
2. SK660 (panel morning): frame "saving itself for the two times today it would be asked to
   fire" — sets the day's budget at 2 discharges.
3. SK680 (~1247, pre-fight): "two discharges still in reserve" — confirms budget intact.
4. SK685 (fight 1): "He had two of these in him a day, at the most... He had already spent
   one." — 2 → 1. Consistent.
5. SK697.5 (fight 2): "The Strix held one discharge" before firing — correctly the *remaining*
   one. Then discharges it. 1 → 0.
6. SK900 (1344, handshake): "The two morning discharges had run the system to zero; the lock
   found nothing to hold." — confirms 0, consistent with the above.

**The discharge count itself (2→1→0) is clean and correctly bookkept — no issues.** The
**BLOCKER** is the *post-lock recharge* question, covered fully in §6 above: SK850 explicitly
states the accumulator will never climb back ("No warmth. No cycle... he wasn't waiting for it
to come back"), SK856.25 directly contradicts this with "the accumulator sitting at
thirty-one percent, climbing, the way it always climbed," and SK862.5/SK1050 both revert to the
permanently-cold framing. The ledger does NOT hold together as written; SK856.25 needs to be
corrected to match the dormant framing established immediately before and after it.

---

## Summary of findings by severity

**BLOCKER (6):**
1. SK697.5 "hearing at eleven" contradicts the established 1300 panel time (§3).
2. Bible's "He did not move. The room did." (SK800, locked) does not exist in the live text;
   nearest analog is a different sentence at a different SortKey (SK350) (§6).
3. Bible's "second salute" / "Beat 12" plant does not exist anywhere in the live text — only
   2 of the claimed 3 salute plants are present (§6).
4. Yellow rain jacket exceeds its "two sentences, never expand" lock across 4 beats (§6).
5. Manowar weight: SK825's "fourteen hundred kilos" contradicts the bible's own canonical
   600 kg/1,320 lb hardware table and the matching figures at SK425/SK660 (§6).
6. Manowar post-lock charge state: SK856.25's "climbing" accumulator directly contradicts the
   "will never recharge" framing established at SK850 and reaffirmed at SK862.5/SK1050 (§6, Extra c).

**MODERATE (7):**
1. Ledger's exhibit-record-insertion capability is unestablished before SK950 (§1).
2. "Forty-three residents" reused for a distinct living population at SK900 without
   disambiguation from the dead (§1).
3. Geography/timestamp snarl: Bear "inside" via freight bay at 1247 (SK680/685) vs. "outside
   the entrance" approaching Declan at the same 1247 (SK690) (§3).
4. "Three sheets" evidentiary content is described inconsistently (handwritten composition vs.
   retained/acquired document copies) between SK660 and SK737.5/746.875 (§4/Extra b).
5. Bible's SK920 apartment-locker contents (transponder + medal) don't match the live SK1000
   beat (medal only; transponder is in a different building, SK825) (§6).
6. Bible's beat spine (§5) is stale — describes a nonexistent SK940 ending and omits the entire
   live ending sequence (SK856.25–1050); several other SortKey citations no longer match (§6).
7. Ventilation fan motif appears to exceed its "6 appearances total" lock (≥9 counted) (§6).

**MINOR (5):**
1. SK50's cat-yawn-before-message narration order (likely intentional, not an error) (§3).
2. SK690/695 nine-minutes-early vs. 1253 entrance timestamp, 2-minute gap (§3).
3. SK660 "written twice the night before" vs. "over the last week" framing (§4/Extra b, minor
   version of finding #4 above).
4. Dead-drop 4th-page (16 names) copy at SK670 never explicitly retrieved or closed out (§4).
5. Overlapping "manifest" terminology across four distinct documents risks reader confusion,
   though each is distinctly named/numbered where it matters (Extra a).
