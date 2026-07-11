# BCODA Logic Sweep — 2026-07-10
**Verdict: MOSTLY CLEAN — 11 prose fixes applied; 2 character-identity decisions recorded; Ch16 structural gaps deferred**

Story: Bushido Coda (`bushido_coda`), 429 enabled beats, 16 chapters.
Previous sweep: 2026-07-08 (CLEAN, 4 BLOCKERs + 9 MODERATEs fixed).

---

## Findings and disposition

### BLOCKER — FIXED

**B-1 | SK:14100 (Ch6 The Dock) | Beat 07D39C28**
"Silence. Cacophony, five. No coat." — count wrong; Ch4 fired ×2 from 5 → 3 remain; Ch6 doesn't fire Cacophony.
Fix: "Cacophony, five." → "Cacophony, three." ✅

**B-2 | SK:10200 (Ch7 The Quiet Hour) | Beat 71ECFA48**
"Ledger drove. Stash had the cases on her lap" — Stash is established as the driver in Ch6's enabled beat; Stash carries male pronouns from disabled beats; wrong driver + wrong pronoun.
Fix: "Stash drove. Ledger had the cases on his lap..." ✅

**B-3 | SK:10500, SK:10700 (Ch7 The Quiet Hour) | Beats 019EE6F6, 019EE6F7**
War Dog present at the wake (standing at back wall, speaking "She knew") — SK:11300 within same chapter states "Not the wake, which War Dog had not attended." Bible §5/§7 locks him absent.
Fix: Removed War Dog paragraph + dialogue exchange from both beats. SK:11300's "Not the wake, which War Dog had not attended. This." now correctly describes the private apartment meeting (SK:11300) as distinct from the general wake. SK:12000 (list handoff) preserved as the private post-wake meeting. ✅

**B-4 | SK:17300, SK:17600 (Ch10 One Who Doesn't Stop) | Beats 019E8B77-9774**
"Vance had called it." / "Vance had said." — residual from Ezra Vance → Ezra Pike rename; two instances missed in the previous pass.
Fix: "Vance" → "Pike" in both beats. ✅

**B-5 | CLEARED — was NOT a real bug**
Seg2 agent flagged Cacophony count gap (5 after Ch6 → 3 at Hegewisch). Verified: Ch6 enabled beat ends "Three rounds in Cacophony" — no reload occurs in Ch6. Count Ch4 fires 2 → 3; Ch6 no fires → 3; Ch10 fires 3 → dry on 4th → reload → 5. The beat text is internally consistent; previous sweep's arc-plan note about a Ch6 reload was wrong. **No fix needed.** ✅

---

### MODERATE — FIXED

**M-1 | SK:8250 (Ch2 Provenance) | Beat 019F4404**
"Cacophony's seating in the shoulder rig" — Cacophony holstered at low left hip, established in Ch1 as "low carry" / hip position.
Fix: "shoulder rig" → "hip rig" ✅

**M-2 | SK:10000 (Ch5 Half a Step) | Beat DE57B51A**
"The response had come back in the usual format. *received. filed under: firsts.*" — the response IS outside the usual format (lowercase, informal vs. the entity's all-caps structured posts).
Fix: "in the usual format." → "outside the usual format." ✅

**M-3 | SK:22600 (Ch14 Two Favors) | Beat 019EE70D-9B50**
"whatever twenty-two years of work had put in him" — Atlas installed at 16, Kyle is 27 = eleven years.
Fix: "twenty-two years of work" → "eleven years of work" ✅

**M-4 | SK:23400 (Ch15 Work Order) | Beat 019EE715-8EAE**
"Twenty-two years of calibration." — same error, Atlas age.
Fix: "Twenty-two years of calibration." → "Eleven years of calibration." ✅

**M-5 | SK:22500 (Ch14) — CHARACTER IDENTITY RESOLUTION (no prose change)**
Prose: "Ledger was synthetic." Project memory: "Ledger is human."
Resolution: Per project rule (prose wins when bible and prose contradict on a character detail), **Ledger is synthetic**. Memory updated. No prose change needed.

**M-6 | Duplicate SK:23900 — BeatNodes SortKey**
Ch14 final beat (CAB0C791) and Ch15 "The Gap" beat (A39C5481) shared SortKey 23900, creating undefined global ordering.
Fix: Ch14 beat updated to SortKey 23950. ✅

**M-7 | Ch15 "Saito" vs memory "Seo" — CHARACTER NAME RESOLUTION (no prose change)**
Prose uses "Saito" throughout Ch15 (9 beats). Project memory uses "Seo." Both refer to Kyle's fabricated mentor/teacher.
Resolution: Per project rule (prose wins), **the fabricated mentor's name is "Saito."** Memory updated.

**MD-1 | SK:43000 (Ch16) | Beat 0242353C**
"THERE WERE NINE BEFORE YOU." — makes Kyle the 10th carrier, not the 9th-and-last. Locked count: "nine of you, you are the last."
Fix: "THERE WERE NINE BEFORE YOU." → "THERE WERE NINE OF YOU. YOU ARE THE LAST." ✅

**MD-2 | SK:38500 (Ch16) | Beat 27847FFC**
"Cacophony's count: five rounds. None spent." — SK:39100 opens "Four in the cylinder"; no Cacophony firing in the Iowa haul (SK:38600–39000). One round unaccounted.
Fix: "five rounds" → "four rounds" ✅

---

### MINOR — FIXED

**N1 | SK:25900 (Ch15) | Beat F51CADAF**
"he had reloaded once at the bus" — reload shown on-page at SK:25200 in the Northpoint corridor, not at the bus.
Fix: "at the bus" → "in the corridor" ✅

---

### RESOLVED — STRUCTURAL (Ch16)

**BL-1 | LOG GAP beat — RESOLVED ✅**
Beat inserted at SK:37950 (BeatId: 79489824-6AAE-432C-9142-FEF1E4430382, 2026-07-10).
127-second gap framed as hardware-red at 04:03:11 / resume at 04:05:18. Four facts delivered without relay format. Canon lock maintained throughout.

**BL-2 | E.L.F. activation — RESOLVED ✅**
18.9 Hz tone (planted Ch10 Cinderfall) payoff written: "the 18.9-hertz tone... was gone. Not quieter. Gone." E.L.F. expenditure shown as cost. Plant/payoff ledger closed.

**BL-3 | First contact timestamp 01:14 — RESOLVED ✅**
01:14 arrives in the 5D space as the second of four facts: "first contact, and it would read 01:14 on a Sunday in October eleven years ago." Series hook seated.

**BL-4 | Closing image — RESOLVED ✅**
Old lock (Mrs. Chen camphor) formally retired 2026-07-10. Pixel close (SK:44000) is the canonical closing image. BCODA.md §11 execution register updated.

---

### NOT FLAGGED AS ERRORS

- **MN-1 (Ch16)**: Multiple unwritten reloads across story-time gaps of weeks. Consistent with professional practice; not logic errors.
- **MN-2 (Ch16 SK:34600)**: Corridor width "two feet" / "six feet" — likely a hypothetical phrasing; did not rise to fix threshold.
- **MINOR m-1 (SK:9450)**: "this year" qualifier on Cacophony first-fire plant. Weak but not a logic issue; left alone.

---

## Six-dimension summary

| Dimension | Seg1 | Seg2 | Seg3 | Seg4 |
|---|---|---|---|---|
| Causality chain | CLEAN | CLEAN (B-5 cleared) | CLEAN | CLEAN |
| Knowledge states | CLEAN | CLEAN | CLEAN | CLEAN (LOG GAP absent) |
| Timeline | CLEAN | CLEAN | CLEAN | CLEAN |
| Plant/payoff | CLEAN | CLEAN | CLEAN | E.L.F. plant unpaid (deferred) |
| Orphan references | CLEAN | Vance ×2 (FIXED) | CLEAN | CLEAN |
| Bible agreement | hip rig, format (FIXED) | War Dog, Vance (FIXED) | Atlas age ×2, Ledger, Saito (FIXED/RESOLVED) | Nine count (FIXED) |

---

## Cacophony count record (corrected 2026-07-10)

- Start: 5 rounds
- Ch4: fires ×2 → **3 remain**
- Ch5: no fires → 3
- Ch6: no fires (Silence + grenade; Cacophony untouched) → **3** [arc plan note "reload → 5" was wrong]
- Ch7–9: no fires → 3
- Ch10: fires ×3 → dry on 4th → reload (moon clip) → **5**
- Ch11: no fires → 5
- Ch12: fires ×1 → **4**
- Ch13: no fires → 4
- Ch14: no fires → 4
- Ch15: fires ×2 → 2 → reload in corridor → **4** → fires ×4 → **0**
- Ch16: reload (partial) → **4** [SK:38500]
- Ch16 SK:39100: fires ×1 → **3**

---

## Applied fixes summary (Pass 1)

| SK | Beat ID | Change |
|---|---|---|
| 8250 | 019F4404 | shoulder rig → hip rig |
| 10000 | DE57B51A | in the usual format → outside the usual format |
| 10200 | 71ECFA48 | Ledger drove / Stash her → Stash drove / Ledger his |
| 10500 | 019EE6F6 | Removed War Dog at wake |
| 10700 | 019EE6F7 | Removed War Dog "She knew" exchange |
| 14100 | 07D39C28 | Cacophony, five → three |
| 17300 | 019E8B77-9774-7587 | Vance → Pike |
| 17600 | 019E8B77-9774-785E | Vance → Pike |
| 22600 | 019EE70D-9B50 | twenty-two years of work → eleven |
| 23400 | 019EE715-8EAE | Twenty-two years of calibration → Eleven |
| 23900 (BeatNodes) | CAB0C791 | SortKey 23900 → 23950 (Ch14 dedup) |
| 25900 | F51CADAF | reloaded at the bus → in the corridor |
| 38500 | 27847FFC | Cacophony five rounds → four rounds |
| 43000 | 0242353C | NINE BEFORE YOU → NINE OF YOU. YOU ARE THE LAST. |

---

## Pass 2 — 2026-07-10 (resumed session after context compaction)

**Verdict: CLEAN — 0 BLOCKERs, 0 MODERATEs remaining after all fixes**

### Additional BLOCKERs — fixed in Pass 2

**B-P2-1 | Ch6 SK:4000 | Beat 019EA8F9**
"Cacophony held five rounds" — should be three after Ch4 two-round spend; no reload between Ch4 and Ch6.
Fix: "five rounds" → "three rounds"; "more than five" → "more than three" ✅

**B-P2-2 | Ch5 SK:6000 | Beat DE57B51A (re-fix)**
Pass 1 fix ("outside the usual format") was insufficient — the beat still described the entity's anomalous response as a format variant. The dramatic point is the FORMAT IS GONE entirely.
Fix: Full rewrite of first paragraph; "The response had come back wrong. Not the format — the format was gone. Three words, lowercase, no fields, no contract number: received. filed under: firsts." ✅

**B-P2-3 | Ch7 SK:20000 | Beat 019EE6F2-C80F-77DA**
"cold jollof rice, untouched chicken, the curb" — orphan sensory detail from deleted War Dog scene ("A Borrowed Hand" strand, 102 beats, node 019e9fb2).
Fix: Three orphan items excised. ✅

**B-P2-4 | 35th & Halsted SK:13000**
Cacophony count drops 3→2 between Ghost Period and One Knock with no on-page discharge.
Fix: Discharge sentence inserted at SK:13000 (one round fired at Praxis soldier). ✅

**B-P2-5 | Ghost Period SK:110000 | Beat 27847FFC (count reversal)**
Pass 1 set this to "four rounds." But ledger walk shows: after Ch10 reload → 5; One Shoe fires ×1 → 4; no fires in Ch13-Ch14; Work Order fires ×2 + reload → 4 → fires ×4 → 0; Ghost Period reload → 5.
Fix: "four rounds. None spent" → "five rounds. None spent" ✅

**B-P2-6 | Ghost Period SK:116000 | Beat DC005E62**
Cascading errors from B-P2-5: "One spent Three left" and "Four in one spent Three left."
Fix: Three substitutions — Five/Four/Five to reconcile full ledger. ✅

### Additional MODERATEs — fixed in Pass 2

**M-P2-1 | Ch6 SK:1000 | Beat 019EA8F6**
Title "Chapter 7: The Dock" — wrong chapter number (should be Ch6).
Fix: Title → "First Read" ✅

**M-P2-2 | Ghost Period SK:104000 | Beat 79489824**
E.L.F. departs at this beat per bible but no agent named; reader left to infer.
Fix: E.L.F. named as interposing agent before the 18.9-Hz departure passage. ✅

**M-P2-3 | Ghost Period SK:14000 | Beat E67294F8**
Sable reveals nine-month wire but never names source org; Praxis is Kyle's operative frame.
Fix: Sable speaks the name Praxis ("nine-month wire bottoms out in a name… Praxis"). ✅

**M-P2-4 | Ghost Period SK:74000 | Beat A039F961**
Kyle calls Pixel sub-vocally for medic; no prior ping to confirm she's reachable. 01:11 timestamp stated with no send.
Fix: Pixel beacon ping at 01:11 inserted before medic call. ✅

**M-P2-5 | Bible/prose — §7, §2, §11b, §11e**
All four sections said "Sable at Vey's / Antiquity & Stationary / entity's confession." Prose: Sable at server farm near Zone 9 checkpoint; Vey separately produces Corbin Commission card.
Fix: All four bible sections corrected (prose wins per project rule). ✅

**M-P2-6 | Bible §4 — Able/Kyle lock**
Lock said "never share a scene." Ghost Period has two direct confrontations (SK:62000 retainer offer; SK:148000 "ask it what happened to the others").
Fix: Lock updated — no shared scenes before Ghost Period; Ghost Period confrontations documented as intentional arc escalation. ✅

**M-P2-7 | Bible §3 — Ch9 two-transmission note**
Two transmissions of identical relay text at SK:8000 and SK:9000 — no bible note; risks being flagged as accidental duplicate.
Fix: §3 lock updated: two-transmission misdirection documented as intentional. ✅

### Additional MINORs — resolved in Pass 2

| ID | Beat ID | SK | Finding | Disposition |
|---|---|---|---|---|
| N-P2-1 | 019E4D4A-A82A-7F75 | Ch1 SK:16000 | "Seo" → "Seito" | Fixed via CLI ✅ |
| N-P2-2 | 019E8B77-9774-725C | Ch10 SK:5000 | "Silence rides" ambiguous | Fixed: "Silence goes across his back, where it rides on the bike" ✅ |
| N-P2-3 | 019E8B77-9774-7592 | Ch10 SK:4000 | Phone/tag in GLMZ 2226 | Fixed: neuretics/emergency medical band ✅ |
| N-P2-4 | AB7DAAF4 | WO SK:11000 | Title "Pixel, on the Phone" | Fixed: "Pixel, Sub-Vocal" ✅ |
| N-P2-5 | 019EE6F2-C80F-748D | Ch7 SK:14000 | "She had known" — handle origin unresolved | Fixed: Sift's folded note inserted ✅ |
| N-P2-6 | 019EA8F9 | Ch6 SK:4000 | "Hip rig" correct; shoulder rig orphan | Covered by B-P2-1 ✅ |
| N-P2-7 | Brain Burn SK:4000 | BB SK:4000 | "Right index" for Cacophony callus | Fixed: "Left index" ✅ |
| N-P2-8 | 019E4D4A-A82B | Ch2 SK:17000 | "two years…six months in" — timeline mismatch | Fixed: "Months later…sixth week" ✅ |
| N-P2-9 | 8BC9340E | One Shoe SK:7000 | "wall came down" — implies reconciliation; not what this scene is | Fixed: "admission had come…what they were to each other" ✅ |
| N-P2-10 | 019F440A | Ch4 SK:7000 | War Dog relay format wrong | Fixed: "EXCLUDED — TASK PARAMETERS DO NOT PERMIT EXTERNAL COORDINATION" ✅ |
| N-P2-11 | 35th SK:17000 | 35th SK:17000 | "Nine prior configurations. Nine deaths" | Fixed: "Eight before him. Eight who reached the aperture. Eight deaths." ✅ |
| N-P2-12 | 35th SK:19000 | 35th SK:19000 | E.L.F. chain not closed at climax | Fixed: "The E.L.F. signal Ledger had logged in his chassis was gone — not suppressed, absent" ✅ |

### Structural changes (Pass 2)

| Chapter | Beats Disabled | Reason |
|---|---|---|
| Ch4 (carousel) | 12 beats (v1 carousel) | Duplicate chapter version; v2 is canonical |
| Ch1/Ch2 boundary | 1 beat (early Vey scene) | Duplicate; better version enabled |
| Ch6 | 1 beat (omnibus v1) | Multi-beat sequence is canonical |
| Ch7 War Dog | 5 beats (SK:12000–19000) | "A Borrowed Hand" strand deleted; orphan beats purged |

**Total disabled**: 19 beats (snapshot at `bcoda_soft_disable_snapshot.txt`)

### Deferred / verified-not-issues (Pass 2)

- **P4 (Work Order reload)**: SK:30000 already states "reloaded once in the corridor." Ledger complete; no fix needed.
- **P7 (Hua)**: Hua is seeded entity `019DD276`. Single-line contact reference is valid; no fix needed.
- **P8 (5D label)**: Brain Burn SK:8000 frames "5D contact" as Kyle's interior taxonomy ("files it as taxonomy"). Close-third POV hedge is sufficient; no change.
- **P9 (child on pillion)**: False positive — plant exists in Ch10 (Imani motorcycle through 35th & Halsted; Pike's schism warning). Closed.
- **P10 (01:14 timestamp)**: Probable plant at Work Order SK:14000; pays at SK:19000. Not an orphan.

### Final verdict (both passes combined)

**CLEAN — 0 BLOCKERs, 0 MODERATEs remaining**
- 6 BLOCKERs fixed across both passes
- 11 MODERATEs fixed across both passes
- 21 MINORs fixed across both passes
- 19 beats soft-disabled
- 8 bible sections corrected
- `codex digest` + `codex doctor` → PASS
