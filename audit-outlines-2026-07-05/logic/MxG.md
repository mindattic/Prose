# MxG Logic Sweep — 2026-07-05

## Summary

MxG is mechanically sound — no BLOCKERs. Four MODERATE findings: a causality gap in Lace's Adalemo voice synthesis, a PEREGRINE headcount that doesn't square against the "4-person unit" bible spec, a freight-rail bridge that implies trains exist in the GLMZ (contradicting the world rule), and a missing CxC trilogy plant on the liability angle of the formula going public.

---

## Findings

### BLOCKER

None.

- "Rider" is never used as Scout's job title. Scout is called "Exo" and "NSB specialist" throughout. CLEAN.
- Helix Biosystems is never named or implied. No character knows who the hidden beneficiary is. CLEAN.
- Gault is burned from the network, not killed: "You're burned. From the network. From every channel I can reach. Starting now." (SortKey 500). Ohara later confirms in dialogue: "He's alive. He lost his network." CLEAN.
- No present-tense year reference implies 2025–2026. Historical dates (2024 stress assessment, 2218 subroutines, 2219 Meridian Charter, 2221 layoffs) are all clearly past-tense. CLEAN.

---

### MODERATE

**[SortKey 650] Lace voices Adalemo with no established audio source.**

> "It was Seun Adalemo's voice. The cadence, the specific flatness he used issuing low-urgency orders, the faint clipping on the *p* sounds. Lace had built it from nothing and it was very good."

"Built it from nothing" explicitly forecloses any audio-sample explanation. In a story built on competence-porn legwork (Boiler checks load tolerances; Vox maps subnets; Scout walks every corridor twice), Lace's vocal synthesis of a corp-black unit commander needs an on-page origin. Adalemo commands a classified unit; publicly available audio of his voice is implausible without a scene establishing how Lace obtained it — a contractor briefing clip, an archived Axiom investor call, anything. As written, the capability appears from nowhere.

**Minimal fix:** Add a single clause to the legwork section (SortKey 150, Lace's report cadence) noting that Lace pulled authenticated Axiom communications logs and flagged Adalemo's voice signature from a security-briefing archive. One sentence in her flat report voice closes the gap.

---

**[SortKey 300 / 550 / 600] PEREGRINE headcount at the Lake Platform contradicts "4-person unit" spec.**

> Scout, SortKey 550: "Twelve on the platform. Four PEREGRINE."

Bible: PEREGRINE = Axiom 4-person corp-black unit commanded by Seun Adalemo.

Accounting across the story: two PEREGRINE operatives appear at the Bloom Quarter (close man + shooter); both escape. Two more operatives breach the safe house: the point man is knocked out by Rook ("on the floor not getting up quick"), the second man is killed by Vox. That is four named operative slots (A, B, C, D) expended before the platform, plus Adalemo on deck 3 = five entities against a "4-person unit." If the Bloom Quarter pair re-appears at the safe house (same two people), the dead second man means only Adalemo + one operative could be at the platform — not four. Either reading leaves Scout's count of four PEREGRINE operationally impossible under the bible spec.

**Minimal fix (option A):** Change Scout's count to "Three PEREGRINE" (Adalemo + two Bloom Quarter survivors, the safe house pair being a separate resource Axiom sourced outside PEREGRINE). **Option B:** Establish in legwork or van dialogue that PEREGRINE is six persons, not four, which also explains how Axiom covered Bloom Quarter, safe house, and the platform simultaneously.

---

**[SortKey 350] Freight-rail bridge implies trains exist in the GLMZ — contradicts the NO TRAINS world rule.**

> "The freight-rail ran to their left, cold steel, no train tonight." (SortKey 350)

The phrase "no train tonight" is present-tense GLMZ and presupposes trains routinely use this rail. The GLMZ transport canon is explicit: NO trains; The Pulse (Mach 6 vacuum tube) replaced them; VTOLs = Tier 3+. A 200-year-old freight-rail bridge that still sees active train traffic sits outside the established world.

This is separate from whether the bridge structure can physically exist — legacy infrastructure from 2024 decaying unmaintained is on-theme. The problem is the implication of active rail use.

**Minimal fix:** Replace "no train tonight" with a phrase that establishes the line as decommissioned or automated-crawler-only: e.g., "The freight-rail ran to their left, cold steel, no load on it since the Pulse opened." This preserves the bridge's structural role and its age without importing trains into the GLMZ.

---

**[SortKey 700] CxC seed #2 absent: formula going public lets a competitor refine it without liability.**

Brief spec: "a coda line that Class V formula going public lets a competitor refine it without liability."

The story's final beat gestures at the concept — "the military pathway was still possible; it just wasn't possible for Axiom alone" — but the liability angle is entirely missing. The point of the plant for CxC is that open-domain publication removes the patent but also removes accountability: any actor who weaponizes the formula bears no civil or regulatory exposure because the research isn't proprietary. That specific risk is what seeds the problem space for the next arc. The current prose leaves this implicit at best.

**Minimal fix:** Add one sentence to the SortKey 700 coda, in Rook's reflective voice: e.g., "The formula was everyone's now, which meant the liability was no one's — any actor who found the Class V pathway from here wouldn't be reverse-engineering a patent; they'd be continuing public research, and there was no legal architecture in the Meridian Charter or anywhere else to stop them." Slot after "depending on who used the door first."

---

### MINOR

**[SortKey 250, 400] "geometry" appears twice in Rook's close-third narration — per-protagonist register bleed.**

> SortKey 250: "Ohara read the geometry the same instant Rook had"
> SortKey 400: "the geometry of a conversation"

Per the per-protagonist register rules (CHARACTER.md §8), arithmetic / filing / parliament / gap / geometry vocabulary belongs to Kyle's register. Both instances appear in the narrative voice of Rook's close-third, not in dialogue. The word is common enough that a first reader won't notice, but in a polished pass it should be substituted with HEIST-register equivalents. The SortKey 250 instance ("geometry" as tactical spatial read) could be "angles," "shape," or "lines." The SortKey 400 instance ("geometry of a conversation") could be "the shape of it" or "the angle she'd chosen."

---

**[SortKey 300] "Too available" plant lacks the word "slightly" — minor phrasing drift from CxC spec.**

> "The job had felt too available from day two — forty thousand, four ways, the price of a frame and not a fee."

The brief spec asks for "slightly too available" to seed the Rook-side instinct that something is being handed to her. The current phrasing ("too available") reads as fully-formed suspicion rather than the faint wrong-note the plant is supposed to establish. Adding "slightly" softens the telling (Rook knows on day two but acts anyway) and matches the CxC seeding intent.

---

**[SortKey 650] Crane arm reach across vertical deck levels not established in legwork.**

> "She could see the crane arm — Boiler had swung it to maximum extension, out over the deck 3 edge, and he was at the end of it, hanging off the superstructure in the middle of a November storm, rain running off him in sheets, sighting through the viewport glass at exactly the angle she'd have wanted him at."

The bridge-collapse contingency is beautifully pre-seeded across SortKeys 100, 150, 200, and 350. The crane's ability to reach deck 3's broadcast-array glass from deck 1 is not pre-established with the same care. The story earns Boiler's structural intuitions; this specific application of the crane's vertical range is used without preparation. No BLOCKER — the scene works kinetically — but a single line in legwork or the briefing noting the crane arm's extension specs would give the payoff the same foundation as the bridge.

---

## Clean Dimensions

**Dimension 3 — Timeline:** CLEAN. The narrative clock (bar meeting → crew assembly → legwork/surveillance day → briefing night → extraction morning → Bloom Quarter → bridge → safe house breach → dock prep → Lake Platform assault → coda) is internally consistent. All historical dates are clearly past-tense. No present-tense GLMZ year implies 2025–2026.

**Dimension 5 — Orphan references:** CLEAN. "Rider" is absent; Scout is called "Exo" and "NSB specialist" throughout. NSB mechanics are correctly described: eye-roll ejection, Husk left behind, no physical jacks, Shell as inhabited frame. Husk/Shell nomenclature is used correctly. Gault is burned, not killed. Helix Biosystems is nowhere in the text.

**Dimension 2 — Knowledge states:** Substantially clean. Vox names PEREGRINE before formal identification is explained, but she's reading their digital infrastructure in real time; this is within a Channeler's reasonable inference from network signature. Ohara's revelation in the van (she hired them first, 11 days before Axiom) resolves all prior "wrong" signals Rook flagged (routing, response times, calibrated payment). Rook learns about the Class V pathway from Ohara, not before. No character knows Helix Biosystems.
