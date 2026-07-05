# PNHL Logic Sweep — 2026-07-05

## Summary

All six BLOCKER tripwires are clean: Vera never appears on the page, Nit is never stolen, Kyle appears in exactly one beat and says exactly three words with no rendered interiority, Bear is absent, the routing log is not erased, and the boots-padding arc lands in both the preparation and finale beats as specified. One MODERATE issue with the wine vintage (GLMZ year offset error) and three MINORs.

---

## Findings

### BLOCKER

None.

---

### MODERATE

- **SortKey 950** `"2029 Château St. Croix. She noted the vintage the way she noted everything: not to be impressed, but because a number was a number and this one told her what he thought the evening was worth."` — GLMZ present-tense year is 2226. A 2029 vintage is ~197 years old at the time of the dinner. The prose treats it as a straight prestige-dinner status signal, identical to how a contemporary wine would read — no marker of age, rarity, or archaeological character. In 2226 this bottle would be a centuries-old museum artifact, not a restaurant pour. The phrasing "what he thought the evening was worth" implies cost/status, but cost-as-status only tracks if the reader and character share the same frame; a 200-year-old bottle would read as incomprehensible wealth or affectation, not just expensive taste. This follows the known GLMZ year-offset pattern (prose defaulting to near-future dates without adjusting for the 2226 setting). **Minimal fix:** change the vintage to a plausible 2200s year (e.g., "2211 Château St. Croix") so it reads as a contemporary prestige wine. Alternatively, if the intent is to make the Assessor's excess explicit, add one clause acknowledging its age — but the current prose does not support that reading.

---

### MINOR

- **SortKey 950** `"You've had an interesting first two weeks"` — Timeline reconstruction places the dinner at roughly Day 5–10 from her arrival (market at 37 hours, relay job accepted same day, relay executed within a few days, invitation four days after the relay). "Two weeks" is therefore either approximate Assessor framing (plausible given he's surveilling her, not counting her calendar) or a soft inconsistency. Not a hard contradiction, but if the relay was run on Day 1–2, "two weeks" overstates the span. No fix required unless a tighter timeline is added elsewhere; worth keeping in mind if intervening beats are ever fleshed out to specify relay execution date.

- **SortKey 1100 / 1200** — The confrontation beat uses "the receiving-counter address three blocks from her own door, which shared a manifest system with the same holding entity" as one of four address-building data sources, and Beat 1200 leverages the counter's node for the first live demonstration burn. The discovery of the manifest-system connection between the counter and the holding entity is never shown on-page — it is asserted rather than witnessed. In Beat 1060 she observes the calendar shorthand and the terminal make, but the manifest-system link is not traced there. The gap is a knowledge-state seam: she has the conclusion without the on-page reasoning step. This does not break the story (it is consistent with her established habit of running deep traces off-screen), but it is the one place where her deductive chain is implicit rather than shown. No fix required unless a reader would question how she knew the node was reachable.

- **SortKey 100** — `"settling onto her palm two centimeters to the left of center, the way it always landed. She'd stopped trying to correct it."` — Nit's landing quirk is established as a character detail but is not paid off anywhere in the 22 beats. This reads as texture (consistent with established character), not a formal plant requiring closure, but it is the only piece of Nit behavior that is specifically noted ("the way it always landed") and then dropped. No fix required; flag only in case a later draft intends the quirk to mean something.

- **Bible / SortKey 500 vs SortKey 800** — The Bible states the handle "Pixel" *originated from the relay job* (relay operators calling the nine-second technique "pixel-work"). The prose shows Donatella coining the name independently at Beat 500 (after the climate fix, before the relay job at Beat 800). These are not contradictory if Donatella's coinage and the gray-zone network's propagation happen to land on the same word — but the relay-origin etymology is never on-page; only Donatella's coinage is shown. The Assessor's use of "Pixel" at Beat 950 is most naturally read as him having learned it from the relay network, which is consistent, but the relay-as-origin is entirely implied. The Bible should be updated to read "first coined by Donatella after the climate fix; propagated through the gray-zone network from the relay job's nine-second gap" to match what the prose actually shows.

---

## Clean Dimensions

1. **Causality chain** — CLEAN. Every event has an established cause: the nine-second gap draws the Assessor's attention; he reaches her room number via the same management-tier credential layer he uses in the lockout (Beat 975 reveals the mechanism; retroactive deduction is by design and correctly handled); she can burn the receiving-counter node because she mapped its access chain during Beat 1060; the six-channel send mechanism is built on-screen in Beat 1150 before it is deployed in Beat 1200. The elevator-shaft transfer is connected to the Assessor via the Ryokan dead-drop node signature she logged in Beat 800. No capability appears without on-page origin.

2. **Knowledge states** — CLEAN. She never acts on information she does not have. The Assessor knows her handle from the relay network (established by the Beat 800→900 sequence). She knows the Assessor sent the invitation without having verified it, explicitly notes the room-number gap, and does not resolve it until the management-credential override is captured in Beat 975. The Cotter investigation correctly uses only the market kid's story (Beat 700) as its starting thread. The only seam is the manifest-system connection noted above (MINOR).

3. **Timeline** — CLEAN overall. The 22-beat arc reconstructs cleanly: arrival Day 0 → market ~Day 1.5 → relay job executed within a few days → invitation four days after relay → dinner → lockout same night → elevator shaft sabotage (post-dinner) → two-week pattern of three incidents → investigation beats → preparation → confrontation → home. "A little under two months" at Beat 1100 is consistent with the cumulative arc. The "two weeks" comment at Beat 950 is flagged MINOR above but does not constitute a timeline impossibility.

4. **Plant/payoff ledger** — CLEAN on all load-bearing items. Boots-padding arc: planted Beat 100, first payoff Beat 1150, final payoff Beat 1300 — both observations present and correctly sequenced, with Beat 1300 explicitly referencing the Beat 1150 discovery. Routing log: planted Beat 800, held through Beat 1200, kept at Beat 1300. 2D worn-latch/odd-hours: planted Beat 600, paid off Beat 1175 (Kyle reveal) and Beat 1300 (latch sound on return). Market warning ("he doesn't let people walk"): planted Beat 700, echoed at Beat 1100, reversed at Beat 1200. Cotter: planted Beat 700 (weather), fully excavated Beat 1090, weaponized Beat 1200. Six channels: planted Beat 1150, deployed Beat 1200, consequences reflected Beat 1300. Assessor's mark/dead-drop signature: planted Beat 800, paid off Beat 1000. The only unresolved plant is Nit's two-centimeter landing quirk (MINOR, above).

5. **Orphan references** — CLEAN. No references to removed or disabled content. "Rider" never appears as a job title. Bear never appears. The ACS officer is not named in the office directory; the building has no blank fifth-floor listing that connects to anything outside the story; the Cotter handle connects only to the Beat 700 market kid's story. NSB vocabulary is correctly absent from active beats (she uses Nit via direct camera feed, not NSB-riding).

6. **Bible agreement** — ONE MODERATE (wine vintage). All other locks hold: Vera never on-page; Nit is a tool throughout, never a MacGuffin, never stolen, design never coveted; Kyle appears in one beat, says "Give em hell" (three words), has no interiority rendered (all observations are from her POV reading his externals); Bear absent; routing log kept; ACS portrayed as professional and structurally unhelpful, not corrupt; Assessor's logic is legible and partially correct (she agrees he's not wrong about the gray zone); boots arc present in both required beats. Pulse described as "the car," never from inside as a sphere. The Blur used correctly as a transit experience, not a zone. Beat 2 aerobloc/vacuum-buoyancy physics present. Beat 8 market warning delivered as weather, not explicit threat.
