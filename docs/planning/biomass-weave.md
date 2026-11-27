# Biomass weave plan: the true origin and corpo waste dumping in the GLMZ books

Tickets: PRS-T1114 (weave plan) and PRS-T1118 (waste-hauler pilots), under story PRS-T1112.
Status: approved by Ryan 2026-11-26 and applied under PRS-T1188. Landed: #1 (Bushido Coda),
#2 (Magenta & Gunmetal), #4 (in The Way Up, the renamed High Five), #5 (Vultures at the Door,
adjusted: the yard and sealed hub are gone from the rewritten opening, so the line follows "the
rendering plant" and drops the hub), #6 (Neon & Rust), #7 (Attendance, at the end of the yard
paragraph, which now runs past the anchor), #8 (Death Whispers, merged into the rewritten "Arcturus ran
security that far south" sentence). Not landed: #3, because Death Whispers no longer has the
husk-sinking passage it hangs on. The supporting details are in `docs/universes/GLMZ.md`
("Gut-run details"). Contradiction #6 is already fixed in the current text. Contradictions 1-4 and
the Boiler and Cetin entity notes are split to a child ticket of PRS-T1188.

## Before applying

- **Ryan reviews every passage first.** These drafts are suggestions in the house voice, not
  approved text.
- **Applying them needs the Prose DB.** The Azure SQL free tier may be paused until 2026-12-01.
- **Edited beats become unread again.** Re-read each one afterwards so the book clears the
  read gate. Never use `--confirm-unread`.
- **Published books need a re-publish.** Bushido Coda (and any other book already on KDP) has
  to be republished for an edit to reach readers.
- **Where to find each spot.** "Beat N" is the beat marker in
  `engine/data/exports/<book>.numbered.md`. The quoted anchor text finds the exact sentence.

## Canon used

- **True origin, kept secret in-world (PRS-T1111).** The Biomass fell from space near
  Huntsville around 2147. Pinnacle BioSynthetics recovered it, and it got loose in Pinnacle's
  secret underground lab. The official story is the ZD-7741 lab leak, and every in-world voice
  believes it. **No insertion below reveals the true origin.** The books keep the secret.
- **Extent.** About 28,000 sq mi across Alabama, Mississippi, Tennessee and southern Kentucky.
  The leading edge is about 150 km south of the Ohio River, roughly the Bowling Green line.
- **Waste dumping (PRS-T1113 "The Dump", and PRS-T1117 "The gut run", both in
  `docs/universes/GLMZ.md`).** The corponations use the Biomass as an all-purpose industrial
  waste dump, because it takes anything. The run south is **the gut run**: **Ferrogate Aero
  FA-40 "Sows"** lift at night from the industrial pads with **waste cans** slung under them.
  They fly transponders-off over Kentucky to the dumping sites on the Biomass border, hold a few
  hundred meters up, open the belly, and come back lighter. Contractors include Ferrogate Aero
  crews and subcontractors under **Carrion Logistics'** reclamation licences. Locals within an
  hour of the Biomass throw things in to watch the flesh take them.

## Proposed supporting details (confirm or rename)

The drafts use the canon terms above: gut run, Sow, cans, the pads, Carrion Logistics. They
add three small details:

| Detail | Why |
|---|---|
| **"The meat"**: street slang for the Biomass | Short and ugly, and it rhymes with Carrion's "meat run". The echo is deliberate: Carrion's leftovers go to the meat. |
| **Can crews**: day labor sealing and slinging waste cans at the pads | Gives poor characters a job they talk about. |
| **E-town (Elizabethtown, KY)**: Moss's hometown | About an hour's drive north of the Biomass's leading edge. |

## Insertions, ranked by how natural they are

### 1. Bushido Coda, Beat 299: Renko Moss grew up near the edge (the "grew up close to it" character)

**Why it fits.** Moss is the Carrion crew chief with "eleven years on the meat run". He has
just carried Kyle and Nadia out of Northpoint as two bags of medical waste and joked that he'd
"throw you in the trash". His canon page gives no hometown, so a southern-Kentucky childhood
contradicts nothing. Canon already has Carrion Logistics subcontractors flying the gut run, so
Carrion's spare remains going to the Biomass is exactly the waste economy the new canon
describes. Moss already speaks in manifests, so the beat costs four short
paragraphs and changes nothing in the plot. Kyle, the POV character, believes the lab story
like everyone else, and nothing here touches it.

Anchor: `"Told you," he said, "I'd throw you in the trash."` / `"You did," Kyle said. "Best ride I've had all year."`

**After** "Best ride I've had all year.", add:

> Moss laughed, the big laugh that never fit him. "That was the good trash." He rolled the
> empty bags flat. "Real trash rides the gut run. Carrion's got two Sows on the Thursday
> contract, and the belly opens over the meat. Never comes back up. Nothing does."
>
> He tucked the bags under the dolly strap. "Grew up in E-town, under an hour off the edge.
> Every kid down there snuck out to the line on run nights to watch the Sows open up." He
> tested the strap. "After, you'd throw something of your own in. A shoe. Your brother's bike. Stand
> there and watch it go under. It breathes, the meat. You don't forget that." He looked south,
> at nothing anyone could see from here. "Then one day you stop throwing things."

### 2. Magenta & Gunmetal, Beat 232: Boiler flew the gut run before Arcturus (pilot, PRS-T1118)

**Why it fits.** Boiler (Mikkeli Väinämöinen) flies the crew's Kaneko LightWork to the Lake
Platform. He is ex-Arcturus Civil Security, very large and steady, and obsessed with loads
and landing struts: exactly the man to hold a Sow a few hundred meters over the haze with a
full can rig slung under it. Canon says gut-run pilots "are well paid, never talk about it",
which fits a man who "never once needed to announce himself". It also explains his calm in a
November crosswind with a whole crew on board. The beat is told from Rook's POV, and she knows
her crew's histories. One sentence is enough.

Anchor: `Boiler was up front, both hands on the stick, correcting in the crosswind with the quiet competence of a man who had never once needed to announce himself.`

**After** that sentence, add:

> Before Arcturus took him on, he'd flown two years of gut runs: a Sow full of slung cans,
> transponder off past the river, belly open over the meat in the dark. A lake in November
> didn't rate.

### 3. Death Whispers in a Cat's Ear, Beat 410: the big outfits pay for air

**Why it fits.** The passage explains how the Cermak crew sinks husks in the flooded
sub-levels. One contrast places that small, cheap operation inside the corponations' waste
economy and keeps the book's own disposal canon. The voice is the POV character's working
knowledge of "the death economy … the routes, the hatches, the places a body went".

Anchor: `…A body went down, and the city's count of its own dead stayed exactly where it was.`

**After** it, add:

> The big outfits didn't bother with water. Theirs went canned on the gut run, slung under a
> Sow with its belly opening over the Biomass, and the meat kept no count of anything. A crew
> that sank its own was a crew that couldn't get on the manifest.

### 4. High Five, Beat 11: can crews pay more

**Why it fits.** Reza and Tavi, unaugmented and broke, are pricing day labor ("Courier work
pays eleven quanta a day"). Can-crew work, slinging waste cans under the Sows, is the job
everyone knows and nobody wants. It puts the dumping in the boys' world in three lines
without slowing the scene.

Anchor: `"We could do a lot of courier work."` / `Tavi looked at him. Reza kept eating.`

**Between** them, add:

> "Can crews pay thirty," Tavi said. "Slinging for the Sows out at the pads."
>
> "And you cough for a year." Reza didn't look up. "My cousin did can crew. Said some of the
> cans were warm when they came in."

(Pick up again with "Tavi looked at him. Reza kept eating.")

### 5. Vultures at the Door, Beat 5: where Carrion's leftovers go

**Why it fits.** The opening sets Carrion's yard "between a bio-rendering plant and a sealed
distribution hub". One added sentence tells the reader what that sealed hub is for, and links
the Vultures' trade to the same waste chain as Moss's.

Anchor: `The yard sat between a bio-rendering plant and a sealed distribution hub. At four in the morning it smelled like the rendering plant — something warm and clinical the wind pushed at you in slow pulses.`

**After** it, add:

> What the plant couldn't render went into the hub, and what went into the hub went south on
> the gut run, to the meat.

### 6. Neon & Rust, Beat 24: the Sows' running lights under the Thread

**Why it fits.** This night-city paragraph already does the Thread ground rule ("the Thread
ran its pods down through the dark"). Canon (PRS-T1117) describes the Sows from the southern
districts as "a line of running lights droning south under the Thread", so this is the canon
image in the book's own night. The Thread is what everyone stops seeing, and the Sows are
what nobody wants to. **Coordinate with PRS-T1119** (the general traffic weave) so this
paragraph doesn't get two traffic additions. If PRS-T1119 already takes this beat, drop this
one.

Anchor: `To the south, above the rooflines, the Thread ran its pods down through the dark, each one barely moving.`

**After** it, add:

> Under it, lower and faster, a line of Sows droned south on the gut run, and nobody on the
> street looked up for those either.

### 7. Attendance, Beat 270: waste cans in the freight yard

**Why it fits.** Yemina's first look at Halsted Freight is a working yard at half past five.
A row of waste cans bound for the gut run is the kind of thing her surface-reading eye catches and files.
Texture only. It points the reader nowhere the plot doesn't go.

Anchor: `Pallets moved on manual jacks. Two men argued without heat over whether a crate had been logged twice.`

**After** it, add:

> At the far bay a row of sealed waste cans waited under a tarp, stenciled for the pads, and
> nobody was arguing about those.

### 8. Death Whispers in a Cat's Ear, Beat 67: Arcturus rides escort on the lifts

**Why it fits.** This beat defines Arcturus as "the CorpoNation that ran security that far
south". Arcturus Defense also issues the Biomass briefings, which carry the cover story.
Showing Arcturus guarding the gut run ties the two together without revealing anything. This
adds a detail canon doesn't have yet (Arcturus escorting the Sows), so PRS-T1117 should accept
it first. It ranks lower because the clause stretches "that far south" from the south side of
the city to Kentucky.

Anchor: `Arcturus was the CorpoNation that ran security that far south — the closest thing to law down there.`

**Replace** that sentence with:

> Arcturus was the CorpoNation that ran security that far south — the closest thing to law
> down there, and the same gunships that flew escort on the gut run.

## Waste-hauler pilots (PRS-T1118)

Candidates searched: the wiki People category and the vehicle entities, plus every GLMZ book
export, for pilots, VTOL flying and hauling.

1. **Boiler (Mikkeli Väinämöinen): strong fit, used above as #2.** He is the crew's VTOL pilot
   in Magenta & Gunmetal (and in Crimson & Chrome he "flies the crew to the Lake Platform").
   He is ex-Arcturus Civil Security, 41, very large, steady, and obsessed with loads and
   landing struts. The past job: two years flying Sows on the gut run (for Ferrogate Aero or a
   Carrion Logistics subcontractor) before Arcturus hired him. That's before the structural
   incident and his discharge. It adds to his backstory and contradicts nothing. Proposed for
   his entity page (Description) once Ryan approves: "Before Arcturus he flew two years of gut
   runs, an FA-40 Sow dumping slung waste cans into the Biomass, and doesn't talk about it."
2. **Cetin Hiller: a lore hook, not a book insertion.** He's 17, born on an airship cargo
   hauler, and secretly able to fly simple cargo rigs. His core desire is "to pilot something,
   anything", and his core fear is being grounded. The gut run is exactly "an awesome job for
   someone to have": the job he'd sell his cat ears for. The gray-zone end of the trade
   (reclaimed Sows, cash, no questions) is the door a kid with no licence could get through.
   Proposed for his Story hooks: "Has heard the gray-zone gut-run crews take pilots with no
   papers if they'll fly a reclaimed Sow low over the meat; he hasn't told anyone he's thinking
   about it." He appears in no book yet.
3. **Ria (Ria Mensah): considered, not recommended.** She is a ground freight hauler working
   toward Class Two certification. Turning down gut-run money could work as a single line, but
   she drives and doesn't fly, so it misses the Sow canon. Leave her alone.

Also checked, with no fit: the PEREGRINE VTOL pilot in Magenta & Gunmetal (unnamed), the
Iron Silk crew VTOL (maintenance-registered and unnamed, most likely Boiler again), and the
other heavy-lift vehicle entities (SN Kite VTOL Freighter, Crucible Levitas Heavy Cargo
Platform, Hearthstone DL-5 'Lugger'). PRS-T1117 has already made the FA-40 Sow the hauler.

## Existing text that now contradicts canon

These are entity and document fixes, mostly for PRS-T1113/PRS-T1111, listed here so nothing
is missed:

1. **"The Biomass: What Grew When Everything Else Died"** (document). It gives the extent as
   28,000 sq mi but also "forty thousand square miles", twice. It says growth began "since
   approximately 2149", but the leak is dated 2147. It puts CAUTERIZE "near the Indiana
   border", but the Biomass is south of the Ohio and nowhere near Indiana (Kentucky's northern
   border would fit). The imperial-units copy has conversion garble ("2-ten feet per year",
   "Within a 330 feet", "10-fifty feet"). Its Origin section is told as the official story,
   which is correct; keep it, and add only the marked true-origin passage.
2. **The Huntsville and Ridgepost place entries**, and the Biomass place entry, call it
   "engineered vegetation" or a "vegetation front". It is flesh. Suggested wording: "the
   Biomass, a continent-scale growth of human-derived tissue".
3. **GLMZ Master Timeline**: "2150s Kentucky lost to Schism expansion." That can stand for
   northern Kentucky. Add the official Biomass dates: 2147 Pinnacle containment failure at
   Huntsville, 2158 CAUTERIZE, 2160 the Biomass reaches southern Kentucky, 2163 the nuclear
   option rejected. The true origin stays off the public timeline.
4. **The Gap: A Visitor's Guide** (document), "The Kentucky Quiet". It says Kentucky is "not a
   hazard zone" and recommends Mammoth Cave tours, which are about 30 km north of the Biomass's
   edge. As corporate tourism spin it can stay. Suggested added line, in the same voice:
   "Visitors are reminded that the Quiet ends at the Green River. South of Bowling Green the
   ground belongs to the disposal concessions, and no guide will take you; the aircraft
   overhead at night are not sightseeing traffic."
   (Note: the PRS-T1117 timeline row already calls Kentucky a "disaster zone", which sharpens
   this contradiction.)
5. **Generic "biomass" elsewhere** (Bamboo, The Granary, the energy grid's "biomass
   generators", The Settling). This is the ordinary lowercase word, not a contradiction. Keep
   "The Biomass" capitalized for the place so readers can tell them apart.
6. **Outside this ticket, noted only:** High Five's opening says the Thread is "anchored somewhere
   near Crestwood". Canon anchors it at Isla Isabela in the Galápagos, and Crestwood is the
   GLMZ lift-yard where freight is loaded south. Suggested wording: "its freight yard somewhere
   near Crestwood". Read from the street, the line may also just be the boys' mistake.
