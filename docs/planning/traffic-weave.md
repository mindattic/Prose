# Traffic weave plan: VTOL and ground traffic as background in the GLMZ books

Ticket: PRS-T1119 (weave plan), under story PRS-T1116. Canon: PRS-T1117.
Status: approved by Ryan 2026-11-26 and applied under PRS-T1188. All ten landed. Adjusted to the
current text: #1 (the elevated is now a walkway, so "Above the walkway", placed at the paragraph's
end), #3 (Pixel now says "station exit" and "Chicago"), #4 (the Narrows paragraph was rewritten
as the Clybourn Corridor's south edge; the convoy is its own paragraph after it, and "which was the
rest of the arrangement" became "and nothing that drove itself kept a record of him"), #5 (the
crown-pads sentence closes the paragraph, after the new Corridor-surcharge clause), #9 (the anchor
now ends "and his hands slowed the bike anyway", so the clause hangs there). The Crestwood anchor
contradiction is already fixed in the current text of The Way Up.

## Before applying

- **Ryan reviews every passage first.** These drafts are suggestions in the house voice, not
  approved text.
- **Applying them needs the Prose DB.** The Azure SQL free tier may be paused until 2026-12-01.
- **Edited beats become unread again.** Re-read each one afterwards so the book clears the
  read gate. Never use `--confirm-unread`.
- **Published books need a re-publish.** Bushido Coda (and any other book already on KDP) has
  to be republished for an edit to reach readers.
- **Where to find each spot.** "Beat N" is the beat marker in
  `engine/data/exports/<book>.numbered.md` (the 2026-08 exports; Bushido Coda uses the
  2026-09-12 `bushido-coda-writer-clone-3` export). The quoted anchor text finds the exact
  sentence even if beat numbers have drifted since.
- **One addition per paragraph.** `biomass-weave.md` (PRS-T1114/T1118) owns the Biomass,
  dumping and waste-hauler-pilot insertions, including the Sows in Neon & Rust Beat 24. This plan
  leaves that beat alone and adds only one Sow sighting of its own (#7), on the return leg.

## Canon used (`docs/universes/GLMZ.md`, "Traffic: who moves how")

- **The sky is stacked by tier.** High lanes: execs in the Axiom SkyLux and Apex, Stratos for
  the Sky People. Middle lanes: Sparrow air taxis, corporate shuttle VTOLs, SN Kites, Arcturus
  Mules. Low lanes: the Ferrogate Aero FA-9, which at shift change is the **shift-lift**,
  chartered by corponations to move wage workers (standing room, sixty where the plate says
  forty, fare docked from pay).
- **The gut run.** Ferrogate Aero FA-40 **Sows** drone south at night to dump into the Biomass
  and come back higher and lighter.
- **The street mostly drives itself,** and who drives is a class signal. Automated rides
  (AutoCabs, Pods, Nexus town cars, Hauler convoys) log everything. The Ferrogate Metro Bus has
  had a human driver since the '91 routing hack. Kyle's Kimodo MDK is ridden, never piloted by
  software.

## Insertions, ranked by how natural they are

### 1. Attendance, Beat 1: the shift-lifts over the elevated

**Anchor:** "Above 18th Street the elevated carried the early shift toward the factories, the
cars full, everyone standing, because at six the seats belonged to whoever rode all the way to
the end."

**Why it fits.** The book opens on the morning shift moving. The sky above the elevated is the
same commute one tier up, and the shift-lift's terms rhyme with the standing-room rule.

**Add after the anchor:**
> Above the track the shift-lifts were already crossing toward the plants, FA-9s strap-hung to
> the doors, sixty where the plate said forty, the fare docked from pay before it was earned.

### 2. Pixel, Beat 33: a newcomer reads the lanes

**Anchor:** "South of it, above the roofline, the Thread came down out of the sky, its pods
spaced even and falling slow as water, the same as it always was and always would be."

**Why it fits.** Pixel has just come in from Detroit and reads machines the way other people
hum (Beat 16). Her first look at the GLMZ sky is the natural place to show it working in layers.

**Add after the anchor:**
> Closer in, the sky worked in layers Detroit had never needed: Mule freighters thrumming in the
> low corridors, Sparrows ticking across the middle, and far above them a SkyLux sliding north
> like something that had never once had to look down.

### 3. Pixel, Beat 37: the traffic stops for her

**Anchor:** "At the terminal exit she talked a stoplight green and walked out into GLMZ with Φ8,
a kit that fit in one pocket, and a question she badly wanted the answer to."

**Why it fits.** The street is automated, so a stoplight Pixel talks green is a line of
self-driving machines obeying her, and every one of them keeping a record. It's her skill and
the city's surveillance in one clause.

**Replace with:**
> At the terminal exit she talked a stoplight green. The AutoCabs and Pods stacked at the
> crossing held for her without a murmur, every one of them logging the woman who'd asked, and
> she walked out into GLMZ with Φ8, a kit that fit in one pocket, and a question she badly
> wanted the answer to.

### 4. Bushido Coda, Beat 157: the one thing on the road with a hand on it

**Anchor:** "He rode through with his lights low, and the Narrows let him pass the way it let
everyone pass who didn't need it to be anything else."

**Why it fits.** The beat is about the Narrows' arrangement: pay in attention, be left alone.
Automated traffic that logs everything is the other side of that arrangement, and the viaduct
is already in the paragraph above.

**Add after the anchor:**
> Overhead, a Hauler convoy rolled north along the viaduct on its own, forty tons a car and
> nobody awake in any of them. Down here the bike was the only thing on the road with a hand on
> it, which was the rest of the arrangement: nothing that drove itself kept a record of him.

### 5. The Way Down, Beat 13: a human at the wheel, execs on the crown

**Anchor:** "Tavi rode the Ferrogate Metro Bus to the Spire's base-level concourse. The bus logged
nothing but a zone transit, which suited a ghost fine."

**Why it fits.** The beat already says the bus logs almost nothing. Canon explains why (a human
driver since the '91 hack), and the Spire's crown pads give the class contrast for free: Tavi
arrives at the bottom by bus while the people he's working around arrive at the top by air.

**Replace with:**
> Tavi rode the Ferrogate Metro Bus to the Spire's base-level concourse. The bus had a human at
> the wheel and no autodrive since the '91 hack, so it logged nothing but a zone transit, which
> suited a ghost fine. Through the scratched glass the Spire's crown pads were busy: an Axiom
> Apex settling onto the top deck without a sound, two SkyLuxes holding in the high lane for
> their turn.

### 6. The Way Up, Beat 8: the sky is for watching

**Anchor:** "Reza's grin held, but something behind it flickered. "Crestwood's a two-hour walk
and a checkpoint?" "Then we'd better start walking.""

**Why it fits.** Two broke men decide to walk two hours to find work. The wage workers who
already have jobs ride over their heads, and they can't afford the fare.

**Add after the anchor:**
> Overhead the first shift-lifts were already crossing south toward the lift-yard, gray-white
> and packed to the doors. A seat on one cost more than the two of them had between them, so
> the sky was for watching.

### 7. The Way Down, Beat 71: the Sows coming home

**Anchor:** "Past all of it, on the southern horizon, the Thread climbed out of the haze with its
running lights on, going where it always went."

**Why it fits.** It's pre-dawn, looking south, at the hour the gut run comes back. One clause
about the return leg tells a reader who knows the canon what went south in the night, and
doesn't explain it to one who doesn't. It sits a book away from biomass-weave's Sows (Neon &
Rust Beat 24), on the opposite leg.

**Add after the anchor:**
> Under it, the last of the night's Sows were coming back north, higher than they'd gone out
> and lighter.

### 8. Sparrow, Beat 11: the evening reverses

**Anchor:** "The city has slid into early evening, the Pulse frequency dropping as the freight
lines back off and the residential load comes up."

**Why it fits.** The beat already tracks the city changing gears by sound. The sky changes
gears with it.

**Add after the anchor:**
> Out the window the shift-lifts are coming back from the plants low and slow and full, and the
> high lanes are filling with the people who never ride them.

### 9. Bushido Coda, Beat 96: what an autopilot wouldn't have done

**Anchor:** "He rode past on Leavitt and his hands slowed the bike anyway — muscle memory older
than the decision, the kind of thing that lived in the wrists and not the head."

**Why it fits.** Riding by hand is a choice in GLMZ, and this is the moment the choice shows:
his hands make a decision software never would.

**Add to the end of the anchor sentence:**
> …the kind of thing that lived in the wrists and not the head, and the one thing no AutoCab
> would ever have done for him.

### 10. Iron & Silk, Beat 525: coming in under the high lanes

**Anchor:** "At 480 meters, the VTOL set down on the Level 3 pad — the guest and commercial tier,
the way in for anyone who belonged at the Spine without living there."

**Why it fits.** The crew is posing as consultants on a cheap maintenance-registered VTOL
(Beat 518). The guest tier implies a residents' tier above it; one clause puts residents in the
sky, out of reach.

**Add before the anchor:**
> They came in through the middle tier with the Sparrows and the freight, under the high lanes
> where the Spine's residents flew themselves home.

## Left alone on purpose

- **Bushido Coda, Beat 491** ("The bike was gone"). This is Kyle's grief beat after losing the
  Kimodo. Traffic texture would cheapen it.
- **Iron & Silk, Beat 520** ("The VTOL rose."). The one-line beat is a deliberate beat of its
  own. The sky-lanes clause goes in Beat 525 (#10) instead.
- **Neon & Rust, Beat 24.** biomass-weave.md #6 uses it for the Sows. One addition per
  paragraph.

## Existing text that contradicts canon (not traffic, flagged for Ryan)

- **The Way Up, Beats 6 and 8** put the Thread's anchor near Crestwood ("The orbital tether,
  anchored somewhere near Crestwood"; "day labor at the anchor"). Canon (the Ascension Tether
  entity) anchors it on Isla Isabela in the Galápagos, and the wiki says there is no anchor in
  the GLMZ: Crestwood is the **lift-yard** where Thread freight is crated and loaded south.
  Suggested fix: "anchored somewhere near Crestwood" → "its freight loaded out of the lift-yard
  at Crestwood", and "at the anchor" → "at the lift-yard".
