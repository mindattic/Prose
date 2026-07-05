# STSH Logic Sweep — 2026-07-05

## Summary

STSH is structurally sound: no kills by Stash, Femi survives, the broadcast completes correctly, all ten bible plant/payoff pairs are present. Two issues require attention — a beat ordering incoherence that places the monitoring-station arrival scene before the planning and tunnel chapters that precede it, and a cluster of Kyle-register vocabulary (gap, geometry, arithmetic, files it) embedded in Stash's close-third narration. One bible fact (the year Stash left Sable) is contradicted by prose on both sides and needs a bible correction.

---

## Findings

### MODERATE

**M1 — [SortKeys 400.0 / 100.0–300.0, Ch9–Ch10] Beat ordering: monitoring-station entry appears before route planning and tunnel transit**

The Right Room (SortKey 400.0, last beat of Ch9) is narrated as Stash's first physical entry into the monitoring station: the access code fails on the first keypress ("Fatigue"), she enters on the second attempt, sees the transmitter panel for the first time, and Femi asks "Can you bring it up?" However, this beat sits in reading order *before* The Plan (100.0, Ch10) — which describes mapping a 4 km avoidance route to reach the same station — Moving Through the Tunnels (200.0, Ch10), and The Monitoring Station (300.0, Ch10), which independently describes unlocking the same door ("She enters it. The lock disengages on the second attempt") and discovering the same transmitter panel.

The two entry descriptions directly duplicate each other:

- The Right Room: *"The emergency access code works on the second entry because she miskeyed the first digit the first time. Fatigue... The coupling port on the top row is a standard Nova medical interface port."*
- The Monitoring Station: *"She enters it. The lock disengages on the second attempt, which she allows herself one second to notice as the kind of error that fatigue produces at the margins... The coupling port on the right side of the array is a standard Nova medical interface port."*

A reader following the file order sees Stash inside the station before she has planned the route or walked the tunnels, then watches the route-planning and tunnel chapters, then sees her enter the station again.

**Minimal fix:** Merge The Right Room into The Monitoring Station as a single unified entry beat (The Right Room carries the meaning/recognition; The Monitoring Station carries the procedure), and place that merged beat at SortKey 300.0 of Ch10 after Moving Through the Tunnels. Alternatively, reassign The Right Room to SortKey 400.0 of Ch10 so it follows The Monitoring Station rather than preceding The Plan.

---

**M2 — [Multiple beats] Kyle register bleed in Stash's narration**

The following words from Kyle's exclusive vocabulary appear in Stash's close-third narration. Stash's register is medical-catalog: she *catalogs*, *notes*, *assesses*, *identifies*, and works in *clinical* precision. She does not *file*, *gap*, or *geometry*.

| Beat | Offending passage | Banned word |
|---|---|---|
| Set-Up | *"The gap is not one she can close this week."* | gap |
| Set-Up | *"She has been managing this kind of gap for seven years"* | gap |
| Set-Up | *"the kind of person who has been doing this kind of arithmetic his entire adult life"* | arithmetic |
| Getting Femi Out | *"she files it, and she does not revisit it"* | files (cognitive filing, Kyle's action) |
| Triangulation | *"She works through the geometry. She works through it step by step... she wants to check her arithmetic before she wakes Ledger."* | geometry, arithmetic |

Note: *"I know the arithmetic"* (Stash dialogue, Stash Meets Petra) is borderline — it is spoken rather than narrated, and the word is used pragmatically (Petra's numbers), not abstractly. Flag for author judgment; the narration instances above are clearer violations.

**Minimal fix:**
- *gap* → *shortfall* or *deficit* (Stash would use precise financial-medical vocabulary)
- *files it* → *catalogs it* or *notes it and does not revisit it*
- *geometry / arithmetic* in Triangulation → *dispersion pattern* / *the calculation* — Stash's signal analysis reads cleanly as a surgeon working through unfamiliar math; the words, not the act, are the issue

---

**M3 — [Ledger's Call, SortKey 100.0 Ch9] Coordinate direction: "four blocks south" in prose vs. "2 blocks south" in bible**

The Ledger's Call beat states Ledger gives Scalpel the location of *"the building that... is four blocks south of the building she was actually in three hours ago."* The Petra Again beat corroborates: *"they will be approximately four blocks from where you actually are."* Both are consistent with each other.

The bible specifies: *"gives wrong coordinates (4 blocks off, coordinates for building 2 blocks south)."*

The prose and in-story dialogue align at **4 blocks south**. The bible says **2 blocks south**. Per the memory rule (prose wins over bible on character details that have been written), the bible needs updating.

**Minimal fix:** Update the bible's plant/payoff item 2 to read "coordinates for building 4 blocks south."

---

### MINOR

**m1 — [The Tunnel, SortKey 300.0 Ch9] 200 m proximity unexplained against 4 km route**

The Tunnel beat places Stash in *"the tunnel access corridor two hundred meters from the monitoring station"* at 0341. The Plan beat (next chapter) describes a 4 km avoidance route with three Scalpel checkpoints. The prose never explains why they cannot simply cover the 200 m directly, though the Scalpel perimeter is the obvious implied reason. A single orienting sentence in The Tunnel or The Plan would resolve the apparent contradiction without restructuring either beat.

**Minimal fix:** Add one line in The Plan to the effect of "The direct route is two hundred meters. The direct route goes through the Scalpel perimeter's core."

---

**m2 — [Petra Again, SortKey 200.0 Ch8] Petra's knowledge of monitoring station destination not grounded**

Petra tells Stash: *"The monitoring station at the north end of the Z-7 axis. That is where you are trying to get."* Her deduction chain — 412.7 MHz dead-channel frequency on the death report, signal tracking, knowledge of the medical network architecture — is entirely plausible for an NSB field agent, but no prose establishes it. The reader is asked to accept her omniscience without the inferential work being shown. This is a knowledge-state gap: Petra acts on knowledge she plausibly has but was never shown obtaining.

**Minimal fix:** Add one beat-level sentence in Petra Again: *"The frequency on the death report. The dead medical channel. There is only one transmitter still standing that can reach forty nodes on that frequency."* This anchors the deduction without exposition.

---

**m3 — [Mwangi Arrives, SortKey 200.0 Ch11] Cascade suppressor payoff absent**

The Cascade suppressor is planted (Mwangi carries it on her right hip) and Mwangi is presented as evaluating Stash as a threat variable. The bible establishes that Stash has no neuretics, making the suppressor harmless to her. Neither the planting beat nor the crossfire beat makes this explicit — the suppressor is carried but never deployed against Stash, and the reason (nothing to suppress) is never stated in prose. The missed opportunity is small: one interiority line from Stash noting she has nothing the suppressor can touch would complete the loop.

**Minimal fix:** In Mwangi Arrives, during the standoff: *"The Cascade suppressor on Mwangi's hip is designed for neuretics. Stash has no neuretics. She notes this the way she notes a clinical variable."*

---

**m4 — [Bible] Year discrepancy: 2213 vs. 2214**

The bible states Stash *"left Sable in 2213 after unease."* The prose is consistent at 2214:
- Implant Broadcasts beat: *"She worked for them from 2209 to 2214."*
- Fragment Two (Renata, narrating Stash): *"She left Sable Industries in 2214. She established an independent clinic in Z-7 in 2216."*
- What Stash Knows Now: *"twelve years carefully not examining"* → 2226 − 12 = 2214.

All three prose references align. The bible's 2213 is the error.

**Minimal fix:** Update the bible to read "left Sable in 2214."

---

## Clean dimensions

**1. Causality chain — CLEAN.** Every major event has established motivation. Renata chose Stash based on documented research. The killswitch fires on the 9% probability Renata explicitly acknowledged (Fragment Five); no Sable signal is required as the trigger — the 9% accounts for hardware failure without external signal. The NSB/Scalpel separation is established before it becomes operational. Ledger's decision to give false coordinates follows directly from his character (protect the broadcast, cover the cost with wrong information). No event lacks a cause.

**2. Knowledge states — CLEAN (with minor gaps noted in m2).** Femi's tunnel feed access is planted (six months prior, she gave him the access and forgot). Femi's knowledge of the abandoned clinic is established (she told him once; he cataloged it). Ledger's prior suspicion of Stash's connection is disclosed honestly when it resolves ("I suspected. I should have told you."). The NSB/Scalpel non-coordination is established structurally before it matters operationally. Petra's knowledge gap (m2 above) is minor.

**3. Timeline — CLEAN.** The reconstruction holds across the 5–6 day window: surgery night (Day 0–1), Ledger workspace (Days 1–4), van interception and tunnels (Days 4–5), broadcast and deposition (Day 5–6), three-week epilogue. No chronological impossibilities were found. The "Midnight" tagline in Moving Through the Tunnels is approximate and doesn't contradict the 0341 timestamp in The Tunnel. The SortKey ordering issue (M1 above) is a structural beat-ordering problem, not a clock-time impossibility.

**4. Plant/payoff ledger — CLEAN** for items 1, 3, 4, 5, 6, 7, 9, 10. Item 2 (Ledger coordinates) is directionally correct but has a 4 vs. 2 blocks discrepancy addressed in M3. Item 8 (building authority access) is present but the prose connection between Stash's payment and Femi's codes is indirect — Femi's tunnel contacts independently have the codes, which is plausible but the plant-payoff link is loose. Not a failure; noted.

**5. Orphan references — CLEAN.** No references to removed characters, disabled beats, or merged content were found. All named entities (Dalco, Augustina, Marco, Renata, Mwangi, Petra, MidNorth Medical, Scalpel Division, NSB, Atlas NeoCortex) are present and consistent across beats. The Nova-7 specs (412.7 MHz, 14.3 GB, six fragments, 428 subjects, sixty-seven NR) are consistent throughout.

**6. Bible agreement — SUBSTANTIALLY CLEAN.** Subject count 428 is correct at every mention. Stash does not kill Mwangi or Petra (crossfire between NSB and Scalpel operatives; two Scalpel and one NSB agent down). Femi survives. Broadcast completes: 14.3 GB plus six fragments, 40 nodes simultaneously. Governor protocol is under review, not suspended — the twelve hundred are still receiving it at story end. Renata's name goes on the wall (Final Image, Femi painting). Ledger arrives at end, confirms payment discussion, comes in anyway. Year 2226 is never violated in present-tense narration. Register is medical-catalog throughout with exceptions flagged in M2. Bible year error (2213 → 2214) flagged in m4.
