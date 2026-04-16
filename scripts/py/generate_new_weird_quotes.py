"""Generate New Weird quotes for the GLMZ."""
import json
import os
import uuid

QUOTES_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "engine", "data", "quotes")

quotes = [
    {
        "quote": "The elevator in Building 9 goes to fourteen floors. Building 9 has twelve floors. Nobody talks about where the elevator goes when you press thirteen or fourteen. People come back from those floors. They come back fine. They come back with groceries.",
        "source": "Maintenance log, Washburn Commons, The Shelf",
        "context": "Structural impossibilities in Shelf housing blocks are well-documented but resist investigation. The extra floors appear on no blueprint and no utility grid.",
        "tags": ["new_weird", "spatial_anomaly", "shelf", "glmz", "architecture", "normalization"]
    },
    {
        "quote": "There is a woman who stands at the corner of Halsted and Division every morning at 4:47 AM. She has stood there for nine years. She does not age. She does not speak. She does not interfere. When asked, she smiles. Residents have started leaving her coffee.",
        "source": "Neighborhood watch report, Hamtramck Enclave",
        "context": "The figure is not holographic, not an android, and not registered in any population database. She leaves footprints and casts a shadow. The coffee disappears.",
        "tags": ["new_weird", "temporal_anomaly", "glmz", "hamtramck", "human", "presence"]
    },
    {
        "quote": "We mapped the storm drains under Geartown last year. This year they are different. Not damaged different. Different the way a face is different when someone is lying.",
        "source": "Civil engineering report, GLMZ Infrastructure Division",
        "context": "Subterranean topology in the GLMZ industrial districts has demonstrated what engineers call geometric drift \u2014 the slow, undeniable rearrangement of structures that should not move.",
        "tags": ["new_weird", "spatial_anomaly", "geartown", "glmz", "infrastructure", "drift"]
    },
    {
        "quote": "The radio tower on the old steel mill broadcasts static. If you listen long enough, you realize the static is breathing.",
        "source": "Urban explorer forum post, verified by three independent recordings",
        "context": "Biological signatures have been detected in several defunct broadcast installations throughout the GLMZ. No organic material has ever been found at the sites.",
        "tags": ["new_weird", "signal", "biological", "glmz", "industrial", "sound"]
    },
    {
        "quote": "My daughter drew a picture of our apartment. She included a door I have never seen. When I looked at the wall she drew it on, there was a faint rectangular outline in the paint. I measured it. It was the exact dimensions of our other doors.",
        "source": "Parent testimony, Deepwell Station community meeting",
        "context": "Children in the GLMZ consistently perceive architectural features that adults cannot. Whether this represents heightened sensitivity or collective delusion remains an open question.",
        "tags": ["new_weird", "spatial_anomaly", "deepwell", "glmz", "children", "perception"]
    },
    {
        "quote": "The fish in the harbor swim in perfect circles. Clockwise in the morning. Counterclockwise in the afternoon. During the transition, at exactly noon, they all stop. Every fish. Perfectly still. For eleven seconds.",
        "source": "Marine biology field notes, Old Harbor",
        "context": "Coordinated animal behavior without observable communication mechanisms has been noted across multiple species in the GLMZ harbor waters.",
        "tags": ["new_weird", "biological", "old_harbor", "glmz", "animal", "pattern"]
    },
    {
        "quote": "I have lived in this apartment for twenty years. Last Tuesday, I found a room behind my kitchen that I have never entered. It was dusty. The dust was twenty years thick.",
        "source": "Resident statement, The Narrows",
        "context": "Hidden rooms that match the age of their buildings suggest they have always existed. Residents do not discover them; residents become able to perceive them.",
        "tags": ["new_weird", "spatial_anomaly", "narrows", "glmz", "architecture", "perception"]
    },
    {
        "quote": "Every clock in Kessler Row runs three minutes fast. We have replaced them. We have synchronized them. We have brought in atomic clocks. Three minutes fast. The sun sets three minutes early there. Nobody has explained the sun.",
        "source": "Municipal timekeeping audit, GLMZ Standards Bureau",
        "context": "Localized temporal variation in the GLMZ has been measured but not explained. The affected zone is precisely bounded by specific intersections.",
        "tags": ["new_weird", "temporal_anomaly", "kessler", "glmz", "time", "measurement"]
    },
    {
        "quote": "The graffiti on the overpass changes when you are not looking at it. Not like someone painted over it. Like the letters rearranged themselves to say something they wanted to say all along.",
        "source": "Street artist collective interview, Pilsen Veil",
        "context": "Self-modifying markings have been observed on surfaces throughout the GLMZ, particularly on structures older than the Corporate Wars.",
        "tags": ["new_weird", "text", "pilsen", "glmz", "art", "transformation"]
    },
    {
        "quote": "We dug a foundation for the new clinic. Six meters down, we hit a sidewalk. It was clean. There were footprints in it, going in one direction. The footprints continued under the building next door. We poured concrete over it and did not talk about it at the site meeting.",
        "source": "Construction foreman, private recording",
        "context": "Buried infrastructure predating all known construction in the GLMZ has been encountered seventeen times in the past decade. Standard procedure is to seal and continue.",
        "tags": ["new_weird", "spatial_anomaly", "glmz", "underground", "archaeology", "denial"]
    },
    {
        "quote": "There is a frequency \u2014 22.4 kHz, just above human hearing \u2014 that the lake produces on calm nights. Dogs hear it. They sit facing the water and they wait. They have been waiting since before anyone started recording it.",
        "source": "Acoustic research paper, University Spine",
        "context": "Lake Michigan has demonstrated acoustic properties inconsistent with any known body of water. The frequency does not match tidal patterns, seismic activity, or any industrial output.",
        "tags": ["new_weird", "sound", "lake", "glmz", "animal", "frequency"]
    },
    {
        "quote": "The bridge sways. Not from wind. Not from traffic. It sways the way a sleeping person breathes.",
        "source": "Structural integrity report, Gravesend Basin crossing",
        "context": "Rhythmic structural oscillation in GLMZ bridges has been attributed to thermal expansion, wind harmonics, and resonance effects. None of these explanations account for the heartbeat.",
        "tags": ["new_weird", "biological", "gravesend", "glmz", "infrastructure", "rhythm"]
    },
    {
        "quote": "My neural implant shows me a navigation overlay of the city. Most of the time it is accurate. Once a month, for about an hour, it shows me streets that do not exist. I followed one once. It was there. It was beautiful. I cannot find it on foot.",
        "source": "BCI user testimony, anonymous",
        "context": "Neural-augmented perception of non-standard geography has been reported by approximately 3% of BCI users in the GLMZ. The streets they describe are consistent across independent accounts.",
        "tags": ["new_weird", "spatial_anomaly", "bci", "glmz", "perception", "technology"]
    },
    {
        "quote": "The old man at the market sells fruit that does not exist. Not modified fruit. Not engineered fruit. Fruit from trees that are not in any database, growing in soil that has no chemical profile, ripening under light that nobody can identify the spectrum of. It tastes like Thursday.",
        "source": "Food safety inspector notes, The Burnished Market",
        "context": "Non-catalogued produce appears in GLMZ markets with enough regularity that vendors have established informal pricing. Health inspections find nothing harmful.",
        "tags": ["new_weird", "biological", "burnished_market", "glmz", "food", "impossible"]
    },
    {
        "quote": "The snow fell upward for six minutes last February. Not in a wind vortex. Not in a thermal column. Just upward, from the ground, into a sky that was not pulling it. Then it stopped and fell normally, as though correcting a mistake.",
        "source": "Weather station log, Edgewater Prism",
        "context": "Localized gravity anomalies in the GLMZ are brief, bounded, and self-correcting. They leave no lasting effect except on the people who witness them.",
        "tags": ["new_weird", "gravity", "edgewater", "glmz", "weather", "correction"]
    },
    {
        "quote": "Something is wrong with the acoustics in Steamvent Alley. Conversations carry. Not echo \u2014 carry. You can hear someone whisper three blocks away as though they are standing behind you. But only certain words. Only the ones that matter.",
        "source": "Noise complaint investigation, GLMZ Housing Authority",
        "context": "Selective sound propagation in the GLMZ has been documented in at least six neighborhoods. The mechanism preferentially transmits emotionally significant speech.",
        "tags": ["new_weird", "sound", "steamvent", "glmz", "acoustic", "selective"]
    },
    {
        "quote": "The stairwell in the parking structure goes down four levels to the underground lot. Last week it went down five. The fifth level was empty, well-lit, and warm. There was a chair. The chair was facing the wall. I took the stairs back up. There were only four levels again.",
        "source": "Security guard incident report, Meridian Core",
        "context": "Transient spatial extensions \u2014 spaces that exist briefly and then do not \u2014 are the most common anomaly reported in the GLMZ core district.",
        "tags": ["new_weird", "spatial_anomaly", "meridian_core", "glmz", "transient", "architecture"]
    },
    {
        "quote": "We installed a new window in the east wall. Through it, we can see the alley, the dumpsters, the fire escape across the gap. Through the old window, two meters to the right, we can see the same alley. But the fire escape is on the wrong side. It has always been on the wrong side through that window.",
        "source": "Tenant repair request follow-up, The Canopy",
        "context": "Perspective-dependent spatial inconsistency \u2014 where the view through different observation points of the same space do not agree \u2014 is concentrated in the Canopy district.",
        "tags": ["new_weird", "spatial_anomaly", "canopy", "glmz", "perspective", "observation"]
    },
    {
        "quote": "The tree in Crucible Square is older than the city. Core samples confirm this. The tree is older than the city by approximately four hundred years. The city is two hundred and twenty-six years old. The tree appears to be six hundred and thirty.",
        "source": "Arborist report, Crucible Square maintenance",
        "context": "Biological entities in the GLMZ that predate the city itself are not uncommon. Their age is verifiable. Their presence before the city is not.",
        "tags": ["new_weird", "temporal_anomaly", "crucible_square", "glmz", "biological", "age"]
    },
    {
        "quote": "The subway train that arrives at Ashfield station at 2:23 AM is not on any schedule. It is not registered to any transit authority. It stops. The doors open. No one gets off. No one gets on. The doors close. It leaves. It has been doing this for eleven years.",
        "source": "Transit security footage analysis, compiled report",
        "context": "Phantom transit vehicles in the GLMZ follow precise schedules despite having no origin, no destination, and no operational authority.",
        "tags": ["new_weird", "transit", "ashfield", "glmz", "phantom", "schedule"]
    },
    {
        "quote": "Rain collects in the gutters of Fort Anchor and flows uphill for exactly thirty meters before resuming normal behavior. The gradient has been surveyed. It is downhill. The water disagrees.",
        "source": "Hydrology survey, Fort Anchor district",
        "context": "Localized hydrological anomalies in the GLMZ affect water behavior in specific, repeatable corridors. The water itself tests as normal H2O.",
        "tags": ["new_weird", "gravity", "fort_anchor", "glmz", "water", "physics"]
    },
    {
        "quote": "The mural on the west wall of the gymnasium was painted in 2198. It depicts a street scene from the neighborhood. Last year, someone noticed a figure in the painting that was not in the original commission photographs. The figure is wearing clothes that did not exist in 2198. The paint is original.",
        "source": "Art conservation report, Highland Park Autonomous Zone",
        "context": "Retroactive modification of static artwork \u2014 changes that appear to have always been present \u2014 challenges fundamental assumptions about causality in the GLMZ.",
        "tags": ["new_weird", "temporal_anomaly", "highland_park", "glmz", "art", "retroactive"]
    },
    {
        "quote": "I counted the steps in the stairwell every day for a year. Three hundred and sixty-two days, it was forty-seven steps. Three days, it was forty-eight. Those three days, I arrived at work before I left home.",
        "source": "Personal journal, recovered from foreclosed apartment",
        "context": "Minor temporal displacement associated with architectural inconsistency suggests the anomalies are related rather than independent phenomena.",
        "tags": ["new_weird", "temporal_anomaly", "spatial_anomaly", "glmz", "stairs", "measurement"]
    },
    {
        "quote": "The pigeons in Glassway organize. Not flock \u2014 organize. They hold formations that match corporate security patrol patterns from the war. Nobody taught pigeons corporate military doctrine. The pigeons know things the pigeons should not know.",
        "source": "Veterinary behavioral study, University Spine",
        "context": "Post-Corporate Wars, animal populations in the GLMZ have demonstrated knowledge of human institutional structures that cannot be explained by conditioning or environmental pressure.",
        "tags": ["new_weird", "biological", "glassway", "glmz", "animal", "military", "knowledge"]
    },
    {
        "quote": "The basement of the library contains books that have not been written yet. They are catalogued. They have due dates. Some of them are checked out.",
        "source": "Librarian testimony, anonymized",
        "context": "Temporally displaced media in the GLMZ appears exclusively in archival and library contexts. The content of future publications has proven accurate in every verifiable case.",
        "tags": ["new_weird", "temporal_anomaly", "glmz", "library", "knowledge", "future"]
    },
    {
        "quote": "She drowned in the harbor in 2214. I know this because I attended her funeral. She is currently alive and does not remember dying. The hospital has her death certificate. She has her driver license. Both are valid.",
        "source": "Friend of the unnamed individual, support group recording",
        "context": "Identity continuity violations \u2014 individuals who have verifiably died and subsequently continue living \u2014 are rare but not unique in the GLMZ.",
        "tags": ["new_weird", "temporal_anomaly", "old_harbor", "glmz", "death", "identity", "continuity"]
    },
    {
        "quote": "There is a street in the Narrows where every reflective surface shows a sky that is slightly different from the actual sky. Bluer. Clearer. With clouds that move the wrong way. It is the most beautiful sky anyone in the Narrows has ever seen. It is not their sky.",
        "source": "Photography collective exhibit notes",
        "context": "Divergent reflections \u2014 where mirrors and windows show realities slightly different from the one they occupy \u2014 are treated as tourist attractions in some GLMZ neighborhoods.",
        "tags": ["new_weird", "spatial_anomaly", "narrows", "glmz", "reflection", "beauty"]
    },
    {
        "quote": "The concrete remembers. I know how that sounds. But if you press your hand to the foundation of any building older than the Wars, you can feel it. Not heat. Not vibration. Memory. The concrete remembers being poured.",
        "source": "Structural demolition worker, retirement interview",
        "context": "Haptic anomalies in pre-War construction materials have been reported by workers across multiple trades. The sensation is consistently described as memory rather than physical property.",
        "tags": ["new_weird", "material", "glmz", "memory", "construction", "haptic"]
    },
    {
        "quote": "My grandmother's recipe calls for an ingredient that does not translate. Not from a language anyone speaks. She learned it from a woman who learned it from a woman who was not born yet. The dish is delicious. We do not ask questions.",
        "source": "Oral history collection, Mexicantown Libre",
        "context": "Culinary knowledge in the GLMZ occasionally contains elements that resist linguistic analysis and temporal logic. The food itself is consistently excellent.",
        "tags": ["new_weird", "temporal_anomaly", "mexicantown", "glmz", "food", "language", "heritage"]
    },
    {
        "quote": "On the summer solstice, for exactly one hour, the lake is glass. Not calm \u2014 glass. You can walk on it. People do. They walk out about fifty meters and sit down and watch the city from the water. Nobody falls through. Nobody has ever tested what would happen if they stayed past the hour.",
        "source": "Solstice festival coordinator, Dockside",
        "context": "Annual anomalous events in the GLMZ have become cultural traditions. The lake solidification is the most attended, drawing thousands who treat it as celebration rather than impossibility.",
        "tags": ["new_weird", "lake", "dockside", "glmz", "water", "ritual", "annual"]
    },
]

created = 0
for q in quotes:
    data = {
        "id": uuid.uuid4().hex,
        "quote": q["quote"],
        "attribution": "",
        "source": q["source"],
        "context": q["context"],
        "category": "new weird",
        "in_world": True,
        "tags": q["tags"],
        "related_entities": []
    }
    fp = os.path.join(QUOTES_DIR, f'{data["id"]}.json')
    with open(fp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    created += 1

print(f"Created {created} New Weird quotes")
total = len([f for f in os.listdir(QUOTES_DIR) if f.endswith(".json")])
print(f"Total quotes now: {total}")
