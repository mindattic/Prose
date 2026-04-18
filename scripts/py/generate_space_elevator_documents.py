"""Generate space elevator / Galapagos destruction documents."""
import json
import os
import uuid

DOCS_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "engine", "data", "documents")

documents = [
    {
        "name": "The Ascension Tether: Construction History of the Galapagos Orbital Elevator",
        "document_type": "historical overview",
        "author": "Meridian Orbital Dynamics (corporate history division)",
        "date": "2224-08-15",
        "classification": "public",
        "body": """The Ascension Tether -- formally designated GOE-1, the Galapagos Orbital Elevator -- is the first and only functioning space elevator on Earth. Its anchor station occupies the northern tip of Isla Isabela in the former Galapagos Archipelago, Republic of Ecuador, at coordinates 0 deg23'N, 91 deg07'W. The counterweight station orbits at 35,786 kilometers above mean sea level. The ribbon is 104,000 kilometers of carbon nanotube composite, manufactured in orbit by Meridian Orbital Dynamics over a period of eleven years.

Ground was broken in 2189. "Ground was broken" is a euphemism. What actually happened is that Meridian Orbital Dynamics, in partnership with the Liang-Petrova Consortium and the collapsing Ecuadorian government, detonated 340 metric tons of shaped charges across the northern third of Isla Isabela to create a level foundation platform approximately 4.2 kilometers in diameter. The blast killed an estimated 11,000 Galapagos giant tortoises, the entire remaining population of the Isabela subspecies. It also destroyed the last wild habitat of the Galapagos penguin, the flightless cormorant, and seventeen species of Darwin's finch that had survived everywhere else but could not survive having their island turned into a parking lot for a crane that reaches God.

The environmental impact assessment was 14,000 pages long. It was approved in nine days. The Ecuadorian government received Phi80 billion in licensing fees, enough to relocate 60% of its coastal population away from rising Pacific waters. Ecuador did not have a choice. Nobody who is drowning has a choice. Meridian Orbital Dynamics understood this when they selected the site.

CONSTRUCTION PHASES:

PHASE 1 (2189-2194): Foundation and Anchor Station. The northern shelf of Isla Isabela was excavated to bedrock and reinforced with a composite foundation extending 200 meters below sea level. The Anchor Station -- a 12-square-kilometer industrial complex -- was built on and around the foundation. Approximately 40% of Isla Isabela was consumed by construction infrastructure, worker housing, material staging, and the four fusion reactors that power the complex. The remaining 60% of the island was designated a "Preservation Zone." It is a parking lot surrounded by chain-link fence with signs that say PRESERVATION ZONE.

PHASE 2 (2194-2198): Ribbon Deployment. The carbon nanotube ribbon was manufactured at the counterweight station in geostationary orbit and lowered to the surface over a four-year period. Initial deployment failed twice -- the first ribbon snapped at 12,000 kilometers due to a manufacturing defect, dropping 8,000 tons of carbon nanotube into the Pacific. The debris field covers an area the size of Portugal. The second attempt succeeded. Meridian Orbital Dynamics stock rose 340% in a single trading session.

PHASE 3 (2198-2200): Climber Systems and Commercial Operations. Electromagnetic climber cars were installed on the ribbon, capable of carrying 20 metric tons to geostationary orbit in approximately 7 days. Commercial operations began on March 3, 2200. The first payload was a Tessera Corponation communications satellite. The second was a Liang-Petrova mining probe bound for the asteroid belt. The third was a coffin containing the preserved body of Meridian Orbital Dynamics founder Elias Karga, who had requested burial in orbit. He got his wish. The tortoises did not get theirs.

CURRENT OPERATIONS (2225):
The Ascension Tether processes approximately 200 metric tons of cargo per week, with a backlog of 18 months. It is the single most profitable piece of infrastructure on Earth. Revenue exceeds Phi2 trillion annually. The Anchor Station employs 14,000 workers, most of whom live on the island in corporate housing. The former Galapagos Marine Reserve, once the most biodiverse marine ecosystem in the Pacific, is now a restricted naval zone patrolled by Meridian Orbital Dynamics security vessels.

The last wild giant tortoise was seen in 2203, wandering the construction perimeter. Security footage shows it standing at the edge of the foundation platform, looking at the ribbon ascending into the sky, for approximately forty minutes. Then it turned around and walked back into what was left of the scrubland. It was not seen again. The footage was leaked and went viral. Meridian Orbital Dynamics issued a statement expressing their "deep commitment to environmental stewardship" and donated Phi500 million to a tortoise breeding program in mainland Ecuador. The program has produced 200 tortoises. They live in a concrete enclosure next to a gift shop that sells Ascension Tether merchandise.""",
        "tags": ["space_elevator", "galapagos", "infrastructure", "corporate", "environment", "destruction", "orbital", "history"],
        "related_entities": ["Meridian Orbital Dynamics", "Tessera Corponation", "Liang-Petrova Consortium"]
    },
    {
        "name": "What We Lost: Ecological Inventory of the Pre-Elevator Galapagos",
        "document_type": "scientific assessment",
        "author": "Dr. Yuki Fernandez-Okoro, Pacific Biodiversity Archive (dissolved)",
        "date": "2210-03-14",
        "classification": "public",
        "body": """This document was compiled ten years after commercial operations began at the Galapagos Orbital Elevator. It is an accounting of what existed before and what does not exist now. It is not a protest document. Protests require someone who is listening.

SPECIES CONFIRMED EXTINCT AS DIRECT RESULT OF ELEVATOR CONSTRUCTION:

Galapagos Giant Tortoise (Isabela subspecies) -- Chelonoidis becki. Population in 2188: approximately 11,000. Population in 2210: 0. Cause: habitat destruction during Phase 1 blasting and foundation construction. The 340-ton shaped charge detonation on June 3, 2189, killed approximately 8,000 individuals instantly. Surviving populations were relocated to a temporary holding facility on Isla Santa Cruz. The facility was decommissioned in 2196 when Meridian Orbital Dynamics acquired Santa Cruz for material staging. Relocated tortoises were shipped to mainland Ecuador. Approximately 200 survived transit. None have reproduced in captivity at rates sufficient for population recovery.

Galapagos Penguin -- Spheniscus mendiculus. The only penguin species found north of the equator. Population in 2188: approximately 1,200 (already critically endangered). Population in 2210: 0. The penguin colony on the western coast of Isabela was within the blast radius. Individuals that survived the initial construction were unable to feed in waters contaminated by construction runoff and the carbon nanotube debris from the Phase 2 ribbon failure. The last confirmed sighting was in 2201.

Flightless Cormorant -- Nannopterum harrisi. Endemic to Isabela and Fernandina. Population in 2188: approximately 1,000. Population in 2210: fewer than 30, all on Fernandina, which has not yet been developed. Fernandina is currently listed as a "Future Expansion Zone" in Meridian Orbital Dynamics' 2220 strategic plan.

Darwin's Finches -- 13 of 17 recognized species are now extinct in the wild. The remaining four persist in small populations on undeveloped islands. The Vegetarian Finch, the Mangrove Finch, the Woodpecker Finch, and the Medium Tree Finch have been confirmed extinct since 2205.

Marine Iguanas -- Amblyrhynchus cristatus. Population decline of 94% across the archipelago due to thermal pollution from the Anchor Station's fusion reactor cooling systems, which discharge heated water into the surrounding marine environment.

ECOSYSTEM-LEVEL LOSSES:

The Galapagos Marine Reserve, established in 1998, was the largest marine protected area in the Pacific for nearly two centuries. It was dissolved by executive order of the Ecuadorian government in 2188 as a condition of the elevator licensing agreement. The cold-water upwelling system that sustained the marine ecosystem has been disrupted by the thermal output of the Anchor Station. Coral coverage has declined by 89%. Whale shark and hammerhead shark populations have collapsed. The fur seal colony at Isabela is gone.

The mangrove forests of Isabela's coast -- which served as nursery habitat for hundreds of marine species -- were cleared for the construction of Pier Complex Alpha, the primary surface logistics facility.

WHAT REMAINS:

Fernandina Island is intact but unprotected. It hosts the last viable populations of the flightless cormorant, two species of land iguana, and the Fernandina rice rat -- the only surviving native rodent in the Galapagos. Fernandina's continued existence depends entirely on Meridian Orbital Dynamics not needing it yet.

EDITORIAL NOTE:

Charles Darwin visited the Galapagos in 1835. His observations of the archipelago's unique species led to the theory of evolution by natural selection -- arguably the most important scientific insight in human history. The place that taught humanity where it came from has been destroyed to build a machine that takes humanity somewhere else. The irony is noted. The irony does not help.""",
        "tags": ["space_elevator", "galapagos", "ecology", "extinction", "environment", "science", "loss"],
        "related_entities": ["Meridian Orbital Dynamics"]
    },
    {
        "name": "The Equatorial Concession: How Ecuador Sold the Galapagos",
        "document_type": "political analysis",
        "author": "Investigative report, GLMZ Independent Press Collective",
        "date": "2223-06-20",
        "classification": "public",
        "body": """In 2187, the Republic of Ecuador was drowning. Not metaphorically. Sea level rise had submerged 30% of its coastal infrastructure. Guayaquil -- the country's largest city and economic engine -- was losing three blocks per year to the Pacific. The national debt exceeded 900% of GDP. Climate refugee resettlement had consumed every available resource. The government was eighteen months from sovereign default.

Meridian Orbital Dynamics approached the Ecuadorian government with a proposal: a 200-year lease on the northern third of Isla Isabela, with options to expand to additional islands, in exchange for Phi80 billion in immediate payment and a 2% royalty on elevator revenue in perpetuity.

Ecuador said yes in nine days.

The speed of the decision drew international condemnation that lasted approximately six weeks, until the global media cycle moved on. The United Nations General Assembly passed a non-binding resolution expressing "grave concern." The resolution had no enforcement mechanism because the UN has no enforcement mechanism. UNESCO revoked the Galapagos' World Heritage designation. Meridian Orbital Dynamics' stock price did not move.

THE NEGOTIATION:

Leaked diplomatic cables reveal that Ecuador initially demanded 5% royalty and a 50-year lease. Meridian Orbital Dynamics countered with 1% and 300 years. The final agreement -- 2% and 200 years -- was reached after Meridian's negotiators pointed out that Ecuador's alternative was to refuse the deal and drown. The cable describing this exchange uses the phrase "constructive leverage assessment." The Ecuadorian negotiator's handwritten margin note reads: "They know we have no choice. We know they know."

The Phi80 billion upfront payment was structured as follows:
-- Phi30 billion: coastal resettlement infrastructure (Guayaquil sea wall, highland refugee housing)
-- Phi20 billion: sovereign debt restructuring
-- Phi15 billion: direct payment to the Ecuadorian treasury
-- Phi15 billion: "consulting fees" to a Cayman Islands entity later traced to the sitting president's family

The consulting fees were exposed in 2194. The president had already fled to a Liang-Petrova corporate enclave in Singapore. Ecuador requested extradition. Singapore does not have an extradition treaty with Ecuador. The president lives comfortably.

THE 2% ROYALTY:

At current revenue levels, the 2% royalty generates approximately Phi40 billion per year for Ecuador -- enough to be the country's single largest revenue source. Ecuador is now economically dependent on the continued operation of the elevator that destroyed its most famous natural treasure. This dependency ensures that Ecuador will never revoke the lease, never impose environmental restrictions, and never deny expansion permits.

Fernandina Island -- the last intact island in the archipelago -- is listed in the lease as an expansion option. When Meridian Orbital Dynamics activates that option, Ecuador will approve it. The math is simple. The math has always been simple.

WHAT THE GALAPAGOS WERE:

Before the elevator, the Galapagos Islands were a UNESCO World Heritage Site, a national park, a marine reserve, and the single most studied ecosystem on Earth. They were the birthplace of evolutionary theory. They were proof that nature, given isolation and time, produces miracles.

Now they are a construction site. The miracles are in a gift shop.""",
        "tags": ["space_elevator", "galapagos", "politics", "ecuador", "corporate", "sovereignty", "corruption", "environment"],
        "related_entities": ["Meridian Orbital Dynamics", "Liang-Petrova Consortium"]
    },
    {
        "name": "The Tower and the Tortoise: Cultural Responses to the Galapagos Elevator",
        "document_type": "cultural essay",
        "author": "Professor Amara Johansson-Nwosu, Cultural Studies, University Spine",
        "date": "2225-01-30",
        "classification": "public",
        "body": """The Ascension Tether is the tallest structure ever built by human hands. It is visible from space as a hair-thin line connecting the surface of the Earth to a point in the sky. On clear nights in the GLMZ -- 4,000 kilometers to the north -- you can see the climber cars ascending, tiny points of light moving upward at a speed too slow to perceive unless you watch for a long time. People watch. They always watch. There is something in the human brain that cannot look away from a thing that reaches into heaven.

This is, of course, the point.

The elevator is a symbol before it is infrastructure. It is the Tower of Babel rebuilt by people who read the story and thought God was the villain. It says: we can reach the stars. It does not say: look what we stepped on to get there.

CULTURAL RESPONSE -- THE TORTOISE:

The Galapagos giant tortoise has become the most reproduced animal image in the GLMZ. Not the dog, not the cat, not the horse -- the tortoise. It appears on murals in the Shelf, on T-shirts in Hamtramck Enclave, tattooed on the arms of people who have never been within 4,000 kilometers of the equator. It is the unofficial symbol of everything the megacity culture has decided to mourn.

The tortoise represents slowness in a world that moves too fast. It represents endurance in a world that consumes. It represents a way of being alive that does not require augmentation, acceleration, or optimization. The tortoise did not need to reach orbit. The tortoise needed an island and a few centuries and it would produce something no laboratory could replicate.

The most famous piece of street art in the GLMZ -- a mural by the anonymous collective DODO that covers the entire south wall of Building 14 in the Shelf -- depicts a giant tortoise standing on the curved surface of the Earth, looking upward at a thin white line that extends from the ground to the top of the frame and beyond. The tortoise is rendered in photorealistic detail. The line is a single brushstroke. The mural has been repainted seven times after corponation security defaced it. Each time, it comes back. Each time, the tortoise looks slightly older.

CULTURAL RESPONSE -- BABEL:

Religious communities across the GLMZ have adopted the elevator as a symbol of human overreach. The Burnished Market's interfaith council issued a joint statement in 2205 calling the elevator "a monument to the sin of believing that the sky belongs to those who can afford to touch it." The statement was signed by representatives of eighteen religious traditions and had no practical effect.

The secular response has been quieter but more pervasive. The word "ascension" -- Meridian Orbital Dynamics' preferred branding -- has become ironic slang in the GLMZ. To "ascend" means to destroy something irreplaceable in pursuit of something profitable. "They ascended the neighborhood" = they demolished affordable housing for a luxury development. "Ascending the commons" = privatizing public resources. The word has been stripped of its celestial connotations and given new ones: greed in an upward direction.

CULTURAL RESPONSE -- SILENCE:

The most significant cultural response to the elevator may be the one that does not exist. There is no major work of art, literature, or music that celebrates the Ascension Tether. The greatest engineering achievement in human history has not produced a single anthem, a single epic poem, a single film that says: look what we built, isn't it magnificent?

The silence says everything. Humanity built a ladder to the stars and does not want to sing about it. Somewhere in that silence is the sound of a tortoise walking back into scrubland that no longer exists.""",
        "tags": ["space_elevator", "galapagos", "culture", "art", "symbolism", "tortoise", "babel", "grief"],
        "related_entities": ["Meridian Orbital Dynamics", "GLMZ", "The Shelf", "The Burnished Market", "Hamtramck Enclave"]
    },
    {
        "name": "GOE-1 Technical Specifications and Operational Parameters",
        "document_type": "technical reference",
        "author": "Meridian Orbital Dynamics, Engineering Division",
        "date": "2220-01-01",
        "classification": "public",
        "body": """GALAPAGOS ORBITAL ELEVATOR (GOE-1) -- ASCENSION TETHER

GENERAL:
Anchor point: 0 deg23'N, 91 deg07'W (Isla Isabela, Galapagos Archipelago)
Total ribbon length: 104,000 km
Geostationary point: 35,786 km altitude
Counterweight station: 104,000 km altitude
Ribbon material: Carbon nanotube composite (Meridian NT-7 formulation, proprietary)
Ribbon width: 1.2 m at anchor, tapering to 0.3 m at counterweight
Ribbon tensile strength: 130 GPa
Construction period: 2189-2200
Commercial operations: March 3, 2200 -- present

ANCHOR STATION:
Platform diameter: 4.2 km
Total facility area: 12 km2
Power: Four Liang-Petrova LP-400 fusion reactors (combined output: 3.2 GW)
Worker population: 14,000
Climber bays: 8 (4 ascending, 4 descending)
Maximum throughput: 200 metric tons/week
Security: Meridian Orbital Dynamics Naval Security Division (12 patrol vessels, 2 submarines, drone swarm coverage)

CLIMBER SYSTEMS:
Propulsion: Electromagnetic linear motor
Ascent speed: 200 km/h (variable, dependent on cargo mass)
Transit time to GEO: 7.5 days (standard cargo), 5.2 days (express/light)
Maximum single-car payload: 25 metric tons
Climber car dimensions: 12m x 4m x 4m (cargo configuration)
Passenger configuration available: 48 passengers, 14-day transit (premium class)

COUNTERWEIGHT STATION:
Function: Orbital manufacturing, cargo staging, deep-space launch platform
Population: 2,200 (permanent crew) + up to 400 (transient)
Orbital velocity: 3.07 km/s
Station mass: 450,000 metric tons (including counterweight ballast)
Docking capacity: 16 vessels (8 internal, 8 external)

OPERATIONAL NOTES:
-- The ribbon is subject to lateral oscillation from wind loading, Coriolis forces, and gravitational perturbation. Active damping systems maintain oscillation within 2 km of centerline at all altitudes.
-- Climber cars are spaced at minimum 500 km intervals to prevent resonance effects.
-- The Anchor Station restricted zone extends 50 km from the platform center. Unauthorized vessels entering the restricted zone are subject to non-lethal interdiction. Vessels that do not comply with interdiction are subject to lethal interdiction.
-- Annual revenue (2224): Phi2.1 trillion. Annual operating cost: Phi340 billion. Annual profit: Phi1.76 trillion.
-- The elevator has operated continuously since 2200 with three interruptions: a climber malfunction in 2207 (4 days), a ribbon oscillation event in 2213 (11 days), and a coordinated eco-terrorist attack in 2219 (6 hours, zero structural damage, 14 attackers killed by security forces).

ENVIRONMENTAL MITIGATION:
Meridian Orbital Dynamics maintains a Phi500 million annual Environmental Stewardship Fund dedicated to biodiversity conservation in the eastern Pacific region. Fund activities include: tortoise breeding program (mainland Ecuador), coral reef monitoring (monitoring only), pelagic species survey (data collection only), and the Galapagos Memorial Digital Archive -- a comprehensive virtual reality recreation of the pre-construction archipelago available to educational institutions for a licensing fee of Phi12,000 per year.""",
        "tags": ["space_elevator", "galapagos", "technical", "engineering", "infrastructure", "orbital", "specifications"],
        "related_entities": ["Meridian Orbital Dynamics", "Liang-Petrova Consortium", "Tessera Corponation"]
    },
    {
        "name": "Voices from the Anchor: Worker Testimonies, Galapagos Elevator",
        "document_type": "oral history collection",
        "author": "GLMZ Labor Documentation Project",
        "date": "2224-05-01",
        "classification": "public",
        "body": """Collected testimonies from current and former workers at the GOE-1 Anchor Station. Names have been anonymized at the request of the subjects, all of whom are bound by Meridian Orbital Dynamics non-disclosure agreements.

WORKER A (Structural Engineer, 8 years on station):
"You get used to the sound. The ribbon hums. Not loud -- you can't hear it inside the buildings -- but if you go outside at night and stand near the base clamp, you can feel it in your teeth. It's the vibration of a wire under tension that goes all the way to space. Eighty thousand tons of tension. You feel it in your fillings.

I've never seen a tortoise. They show us the footage in the onboarding orientation -- the one that went viral, with the tortoise looking at the ribbon. They show it like it's inspirational. Like the tortoise was in awe. The tortoise was lost. It was looking for home and home was a launchpad."

WORKER B (Marine Logistics, 3 years on station):
"The water is warm. Not warm like tropical ocean warm. Warm like someone left the bath running. The reactor coolant discharge heats the surrounding ocean by about four degrees Celsius in a radius of maybe thirty kilometers. Nothing lives in that water anymore. I've been on boats in the restricted zone for three years and I have never seen a fish. Not one. The water is clear and warm and empty. It's the cleanest dead water on Earth."

WORKER C (Security, 12 years on station):
"The eco-attack in '19 was twelve people in three boats. They had homemade explosives. Shaped charges -- not big enough to scratch the ribbon, but they weren't aiming for the ribbon. They were aiming for the reactor cooling intakes. If they'd hit those, the thermal shutdown would have taken the station offline for months.

They got within eight hundred meters. We sank two boats with directed-energy systems. The third beached on what used to be the tortoise nesting ground. Four of them made it to shore. They were carrying a banner that said THE SKY IS NOT FOR SALE. We detained them. Meridian processed them through corporate jurisdiction. I don't know what happened to them after that. I didn't ask."

WORKER D (Environmental Compliance, 2 years, resigned):
"My job was writing the quarterly environmental impact reports. I measured water temperature, catalogued species observations, and compiled everything into a document that went to Meridian's legal team. They edited it before it went to the Ecuadorian government.

I resigned because of what they edited. I would write: 'Marine iguana population in monitoring zone declined 40% year-over-year.' They would change it to: 'Marine iguana population in monitoring zone showed variability consistent with natural fluctuation.' I would write: 'No live coral observed in sector 7.' They would change it to: 'Sector 7 coral survey showed results consistent with seasonal dormancy patterns.'

There is no seasonal dormancy in coral. Coral is either alive or dead. The coral is dead. The reports say it's sleeping."

WORKER E (Climber Operations, 15 years, current):
"I've been to the top. The counterweight station. You can see the Earth from there -- the whole thing, blue and white and turning slowly. It is the most beautiful thing any human being can see. I understand why people want to go there. I understand why Elias Karga wanted to be buried there.

But I also understand that we killed an island to build a ladder, and from the top of the ladder the island is invisible. You can't see what you destroyed. That's the trick. That's always been the trick. Go high enough and everything you did to get there disappears.""",
        "tags": ["space_elevator", "galapagos", "labor", "testimony", "corporate", "environment", "workers", "oral_history"],
        "related_entities": ["Meridian Orbital Dynamics"]
    },
]

created = 0
for doc in documents:
    data = {
        "id": uuid.uuid4().hex,
        "name": doc["name"],
        "type": "document",
        "document_type": doc["document_type"],
        "author": doc["author"],
        "date": doc["date"],
        "classification": doc["classification"],
        "body": doc["body"],
        "tags": doc["tags"],
        "related_entities": doc.get("related_entities", [])
    }
    fp = os.path.join(DOCS_DIR, f'{data["id"]}.json')
    with open(fp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    created += 1
    print(f"  Created: {doc['name']}")

print(f"\nTotal space elevator documents created: {created}")
