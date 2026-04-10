const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const dataDir = path.resolve(__dirname, "..", "engine", "data");
const documentsDir = path.join(dataDir, "documents");
const placesDir = path.join(dataDir, "places");

function newId() {
  return crypto.randomBytes(16).toString("hex");
}

function writeIfNotExists(dir, id, obj) {
  const filePath = path.join(dir, `${id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(obj, null, 2), "utf-8");
  console.log(`CREATED: ${filePath}`);
  return true;
}

// ─── DOCUMENTS ───────────────────────────────────────────────────────────────

const documents = [
  {
    name: "The Lost Blocks",
    document_type: "overview",
    author: "Circuit District Information Collective",
    date: "2226-01-14",
    classification: "public",
    description:
      "At least seven neighborhoods in GLMZ are visible on satellite imagery and show up on BCI navigation overlays, but cannot be physically entered from street level. They appear on every overhead scan. They resolve on every orbital pass. They have streets, buildings, alleys, and rooftops arranged in patterns consistent with inhabited urban space. Some appear to have residents — thermal imaging shows heat signatures moving through interiors on diurnal cycles consistent with human habitation. Power consumption registers on the Ouroboros grid. Water flows into them. Waste heat radiates from them. The blocks exist. You just can't get to them.\n\nPeople call them the Lost Blocks, and most residents of GLMZ assume it's a mapping error — a glitch in the BCI overlay, a satellite calibration problem, an artifact of outdated cartographic data being layered over current infrastructure. It's not a mapping error. The satellite images are current. The buildings are real. The walls separating them from the rest of the city are real. And the walls have no doors.\n\nSeven confirmed Lost Blocks have been documented as of 2226: Linden Block, the Caulfield Pocket, Block 19-South, the Meridian Fold, Ghost Acres, the Threshold Wedge, and Sector 11-East. Each displays the same fundamental characteristic — visible from above, inaccessible from the ground. Each has been surveyed, photographed, thermally mapped, and confirmed to contain structures, infrastructure, and what appear to be occupants. Each is surrounded by continuous, unbroken walls with no entrances, no service doors, no utility access points, and no evidence that entrances ever existed.\n\nThe Lost Blocks are not abandoned. They are not ruins reclaimed by the city's growth. They are active, maintained, inhabited spaces that happen to be unreachable by any known means of ground-level access. The Ouroboros grid logs show steady power consumption across all seven — not the flat draw of empty buildings on standby, but the variable, peaking, cycling consumption pattern of occupied space. Somebody in there is turning lights on and off. Somebody is cooking. Somebody is running equipment. The grid doesn't know who. The grid doesn't care. It delivers power to coordinates, and the coordinates draw power, and the bills are paid in bulk through accounts that trace to holding entities with no public registration.\n\nMost people in GLMZ don't think about the Lost Blocks. The city has enough visible problems without worrying about invisible ones. But for those who look — cartographers, urban researchers, the occasional delivery driver whose route used to go through one of them — the Lost Blocks are a wound in the geometry of the city. A place where the map and the territory disagree, and the territory is winning.",
    related_entities: ["GLMZ", "Circuit District", "Ouroboros", "Lost Blocks"],
    credibility: "verified",
    story_hooks: [
      "Who pays the bulk power bills for the Lost Blocks, and through what accounts?",
      "If the blocks were once accessible, what changed — and can it change back?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "circuit_district", "overview"]
  },
  {
    name: "I Swear It Was Between McKenzie and Halsted",
    document_type: "personal_account",
    author: "Ria Okonkwo-Salazar",
    date: "2223-08-07",
    classification: "public",
    description:
      "I drove the McKenzie-Halsted route for six months. Package delivery, automated logistics support — my rig handled the heavy loads that the drones couldn't manage. I had seventeen confirmed deliveries to 4412 Linden Street, Linden Block. I know the address. I typed it into my manifest every time. The door was green — not painted green, but that oxidized copper green that old doors get when nobody repaints them. A woman named Desta signed for packages. She had short gray hair and she never smiled but she always said thank you. I delivered to her seventeen times.\n\nOne morning my route stopped resolving. The navigation overlay showed Linden Street, showed the turn off McKenzie, showed the alley that cut between the buildings and opened onto the block. But when I drove to the turn, there was no alley. I stopped the rig. I got out. I walked to where the alley should have been. There was a wall — continuous brick, old, weathered, no seam, no patch, no indication that anything had ever been different. I put my hand on it. It was real. It was cold. It had been there for what looked like decades.\n\nMy BCI navigation log shows me entering and exiting the Linden Block repeatedly over six months. The historical route data is intact — every turn, every stop, every delivery timestamp. I can pull up the map and watch my own path trace through streets that I walked, that I drove, that I physically occupied. The streets are still on the map. They are not on the ground. I went back to the dispatch office and pulled my delivery records. Seventeen successful deliveries. Seventeen signatures from Desta at 4412 Linden Street. The address doesn't exist on any current directory. I searched every database I could access. The address has never existed, according to every record except mine.\n\nI filed a report with the logistics company. They told me it was a database error and closed the ticket. I went back to McKenzie and Halsted four more times. Each time I stood in front of a wall that my BCI said was a street. Each time I could see, on my overlay, the route I used to drive — the alley opening, the left turn, the row of buildings, the green door. I could see it on the map while I was touching the wall that said it wasn't there. I don't know what happened to Desta. I don't know what happened to the street. I know I was there. My logs prove I was there. The wall says I wasn't.",
    related_entities: ["Linden Block", "GLMZ", "McKenzie Street", "Halsted Street"],
    credibility: "credible",
    story_hooks: [
      "Who is Desta, and is she still inside the Linden Block?",
      "Can the delivery records be used to reconstruct the interior layout of a Lost Block?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "personal_account", "linden_block"]
  },
  {
    name: "Cartographic Anomalies in the Circuit District",
    document_type: "academic_paper",
    author: "Dr. Yuki Anand-Petrov",
    date: "2225-03-22",
    classification: "public",
    description:
      "This paper presents the results of a comprehensive cartographic survey conducted between 2223 and 2225, documenting every measurable discrepancy between satellite imagery and street-level physical survey within the Circuit District of GLMZ. The survey employed high-resolution orbital imaging (0.3m/pixel), BCI navigation overlay data, Ouroboros infrastructure maps, and direct ground-level photographic and LIDAR survey conducted on foot by the author and three research assistants. The methodology is straightforward: compare what the satellites see with what a person standing on the street sees. The results are not straightforward at all.\n\nSeven zones were identified where physical ground-level access does not correspond to overhead observation. These zones — designated Anomaly Zones AZ-1 through AZ-7, corresponding to the colloquially named Lost Blocks — contain a combined total of approximately 47 buildings, 12 distinct street segments, and 3 open spaces (courtyards or plazas) that are clearly visible in satellite imagery but completely inaccessible from street level. In each case, the perimeter of the anomaly zone consists of continuous, unbroken wall surfaces with no doors, gates, windows, service hatches, or any other form of opening. LIDAR scanning confirms that these walls are solid — not false fronts, not sealed doors, not bricked-over openings. They are walls. They have always been walls, as far as the material evidence indicates.\n\nThe paper includes 143 paired photographs: satellite views next to street-level photos of the same coordinates. They don't match. Not slightly — fundamentally. A satellite image showing a two-lane street with buildings on both sides corresponds to a street-level photograph of a blank wall. An overhead view of a courtyard with what appears to be vegetation corresponds to a ground-level photo of a loading dock belonging to a completely different building. The coordinate systems are verified. The timestamps are synchronized. The images are of the same place at the same time, and they show different places.\n\nAdditional analysis of historical satellite imagery reveals that six of the seven anomaly zones were once cartographically consistent — overhead and ground-level surveys matched. The transition to anomalous status occurred at different times for each zone, ranging from 2207 (AZ-4, the Meridian Fold) to 2219 (AZ-1, Linden Block). In no case was the transition observed in progress. Retrospective analysis shows matching imagery in one survey period and non-matching imagery in the next. The intervals between surveys range from six months to two years. Whatever happens to create an anomaly zone happens within that window, and it happens without leaving evidence of construction, demolition, or any physical modification to the surrounding structures.\n\nThe author notes that these findings are consistent with the theoretical framework proposed by Dr. Sato Mbeki-Larsen in 'The Theory of Urban Cysts' (2225), though the author expresses no opinion on the validity of the underlying physics. The cartographic data is presented without explanatory hypothesis. The data does not need a hypothesis. The data needs an explanation that the author does not have.",
    related_entities: ["Circuit District", "GLMZ", "Lost Blocks", "Dr. Yuki Anand-Petrov"],
    credibility: "verified",
    story_hooks: [
      "The survey identified transition windows for each Lost Block — what happened during those windows?",
      "Dr. Anand-Petrov's LIDAR data could reveal structural details invisible to other methods"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "academic", "cartography", "circuit_district"]
  },
  {
    name: "The People in the Lost Blocks",
    document_type: "analysis",
    author: "GLMZ Urban Observatory",
    date: "2226-02-19",
    classification: "restricted",
    description:
      "Thermal imaging analysis conducted over a continuous 90-day observation period from October through December 2225 has confirmed the presence of approximately 340 distinct heat signatures distributed across the seven confirmed Lost Blocks of GLMZ's Circuit District. The signatures are consistent with human-sized endothermic organisms — body temperature ranges, movement patterns, and spatial distribution all correspond to what would be expected from a human population occupying residential and mixed-use urban space. They are, to every instrument we can point at them, people.\n\nIf these are people, they eat. Thermal cycling in multiple buildings shows patterns consistent with cooking — localized high-temperature events of 15-45 minute duration, occurring at times consistent with meal preparation. They sleep — heat signatures become stationary for 6-8 hour periods during nighttime hours, concentrated in spaces consistent with bedrooms. They move through the spaces on a daily rhythm that mirrors the circadian patterns of the surrounding city. They wake. They move to different rooms. They appear to leave buildings and walk on streets. They congregate in what satellite imagery shows as a small plaza in the Meridian Fold. They disperse. They return to their buildings. They sleep.\n\nBut no one has seen them enter or exit the Lost Blocks. No delivery records exist after the blocks became inaccessible — with the notable exception of the postal drone anomaly documented elsewhere. No utilities are billed to individual accounts within the blocks. The Ouroboros grid shows bulk power consumption allocated to geographic coordinates, paid through opaque holding entities, but no individual meters, no individual accounts, no individual names. Water consumption is inferred from supply line pressure differentials, not from metered usage. Waste — thermal waste, at least — exits the blocks through the same infrastructure that serves the surrounding buildings, but no physical waste collection occurs.\n\nWho are they? How did they get there? Are they trapped, or are they choosing to be unreachable? The 90-day observation found no evidence of distress signals, no SOS patterns, no attempts to communicate with the exterior. The daily rhythms are calm, regular, domestic. If these people are prisoners, they are the most contented prisoners in GLMZ. If they are free, they have chosen a freedom that the rest of us cannot reach, cannot understand, and cannot even confirm is real. Three hundred and forty heat signatures. Three hundred and forty lives — or something that looks exactly like lives — being lived in places we can see but cannot touch.",
    related_entities: ["GLMZ", "Circuit District", "Lost Blocks", "Ouroboros", "Meridian Fold"],
    credibility: "verified",
    story_hooks: [
      "Are the 340 heat signatures actually human, or something that merely reads as human to thermal sensors?",
      "The opaque holding entities paying the power bills — who controls them?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "thermal_imaging", "population"]
  },
  {
    name: "I Got In",
    document_type: "personal_account",
    author: "Anonymous (handle: foldwalker)",
    date: "2225-11-03",
    classification: "unverified",
    description:
      "I'm posting this from a public terminal because I don't want this tied to my BCI. I found an entrance to a Lost Block. I went in. I came out. I am not going back.\n\nI've been exploring the Underworld for three years — the service tunnels, the abandoned infrastructure, the spaces beneath the city that nobody maintains and nobody monitors. Most of it is exactly what you'd expect: dark, wet, full of rats and the occasional squatter. But in October I found a tunnel in the sub-basement of an abandoned water treatment facility near the Shelf that didn't match any map I had. It went down when it should have gone horizontal. The walls changed from concrete to something smoother — not metal, not stone, something in between. The air changed. It got warmer. It smelled different, like ozone and cut grass, which makes no sense underground.\n\nThe tunnel opened into a basement. The basement had stairs going up. I went up. I came out onto a street. A real street — paved, with buildings on both sides, streetlights (working), and sky above me. The sky was wrong. Not wrong like a different sky — wrong like a photograph of sky, like someone had taken a picture of a clear day and pasted it overhead. The light was even, shadowless, and it didn't change in the twenty minutes I was there.\n\nThe street was clean. Not clean like maintained — clean like unused. No litter, no scuff marks, no gum on the sidewalk. The buildings were intact, maintained, with doors and windows. The windows had curtains. Some of the curtains were drawn. I walked for twenty minutes. I saw no one. I heard nothing except my own footsteps and a faint hum, like electrical infrastructure running at capacity. But I felt observed. Not watched — observed, the way a specimen feels observed. Something was aware of me. Something was taking note.\n\nI turned around. The basement stairs I'd come up were gone. In their place was a different stairway leading down to a different tunnel — same smooth walls, same warm air, but oriented differently. I followed it because I had no other option. I walked for maybe ten minutes. I came up through a service hatch into an alley three blocks from where I'd gone down, on the surface, in the normal city. My BCI showed no gap in my location data — according to my navigation log, I'd walked a straight line through solid buildings.\n\nI've been back to the water treatment facility twice. The tunnel isn't there anymore. The sub-basement ends in a wall. I am not going back.",
    related_entities: ["Lost Blocks", "GLMZ", "The Shelf", "Underworld"],
    credibility: "unverified",
    story_hooks: [
      "The 'photograph of sky' — is it a projection, a membrane, or something else entirely?",
      "foldwalker's BCI showed no gap — does the Lost Block exist in the same coordinate space as the surface city?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "urban_exploration", "underworld", "anonymous"]
  },
  {
    name: "The Shadow Census",
    document_type: "analysis",
    author: "Unknown (distributed anonymously via dead-drop network)",
    date: "2226-01-01",
    classification: "unverified",
    description:
      "Someone has been counting the Lost Block population for years. Not through official channels — the Urban Observatory's thermal survey is the closest any institution has come to acknowledging the Lost Block inhabitants, and that study was published only last month. This count has been running since at least 2216, possibly longer. It reaches the public through a dead-drop network: printed documents left in specific locations around the Circuit District, updated annually, always on January 1st.\n\nThe methodology is documented in exhaustive detail. The Shadow Census — as the dead-drop community has named it — uses power consumption analysis as its primary tool, cross-referenced with waste heat measurement and water usage patterns extrapolated from publicly available Ouroboros grid data. The author demonstrates that individual household power consumption creates a unique signature: timing, duration, magnitude, and cycling patterns that correspond to specific activities (cooking, lighting, climate control, electronics operation). By disaggregating the bulk power consumption data for Lost Block coordinates, the author has reconstructed what they claim is a household-level census of every occupied unit in every Lost Block.\n\nThe Shadow Census estimates 340-400 individuals across all seven Lost Blocks. This is consistent with the Urban Observatory's thermal count of 340, lending credibility to both analyses. But the Shadow Census goes further. It has ten years of data. And the data tells a story that the thermal survey, with its 90-day window, could not.\n\nThe population has been stable for at least a decade. Not approximately stable — precisely stable. The power consumption patterns show the same number of distinct household signatures year after year. No new signatures appear. No existing signatures disappear. The daily rhythms are consistent — the same units activate at the same times, consume the same amounts, cycle through the same patterns. There are no births — no new, small-consumption signatures appearing in existing households. There are no deaths — no signatures going dark. No growth. No decline. Exactly the same number, year after year, living exactly the same patterns, consuming exactly the same resources.\n\nThe author of the Shadow Census offers no interpretation. The final page of each annual report contains only the data tables and a single sentence: 'The count is the same.' Ten years of counting. Ten years of the same answer. Whatever is living in the Lost Blocks, it does not change.",
    related_entities: ["Lost Blocks", "GLMZ", "Circuit District", "Ouroboros"],
    credibility: "credible",
    story_hooks: [
      "Who is the Shadow Census author, and how do they access disaggregated Ouroboros grid data?",
      "A population that doesn't change for a decade — are these people, or are they patterns?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "census", "population", "ouroboros"]
  },
  {
    name: "Are the Lost Blocks Growing?",
    document_type: "analysis",
    author: "Dr. Yuki Anand-Petrov",
    date: "2226-03-15",
    classification: "public",
    description:
      "This paper presents a follow-up analysis to the author's 2225 cartographic survey of anomaly zones in the Circuit District. Using archived satellite imagery spanning 2216 to 2226, the author has conducted a decadal comparison of the spatial extent of the seven confirmed Lost Blocks. The methodology is identical to the original survey: high-resolution orbital imaging compared with ground-level physical survey, with anomaly zone boundaries defined as the perimeter where overhead observation diverges from street-level reality. The results are disturbing.\n\nThe total area covered by the seven Lost Blocks has increased by approximately 3% over the ten-year observation period. This figure is aggregated — individual blocks show different rates, ranging from no measurable change (Caulfield Pocket) to approximately 7% expansion (Meridian Fold). The expansion is not uniform in direction. It appears to follow the existing street grid, extending along axes defined by the surrounding city's geometry rather than expanding radially. In the case of the Meridian Fold, a building that was demonstrably outside the anomaly zone in 2224 — accessible, occupied, with street-level entrances — is now inside it. The building is visible on satellite. It cannot be reached from the ground. Its former occupants were evicted by circumstances they describe as 'the door was there and then it wasn't.'\n\nThree percent is small. Three percent over ten years is a rate that most people would dismiss as measurement error, and the author acknowledges that the margin of error in satellite-to-ground comparison is not trivial. But the measurement has been repeated with multiple imaging sources, multiple ground surveys, and multiple boundary-definition methodologies. The 3% figure is robust. And 3% is also measurable. If the rate is linear — and the author emphasizes that there is no evidence it is linear, but also no evidence it isn't — then in 100 years the Lost Blocks will have absorbed approximately 30% of the Circuit District. In 200 years, the majority of the district will be anomalous.\n\nThe author has shared these findings with the GLMZ Urban Planning Authority. The Authority has not responded. The author has submitted the paper to three academic journals. Two rejected it on methodological grounds that the author considers spurious. The third accepted it and then withdrew acceptance without explanation. This paper is being published through independent channels. Nobody has proposed an explanation for the growth. Nobody has proposed a response. The Lost Blocks are getting larger, and the city is pretending they aren't there.",
    related_entities: ["Circuit District", "GLMZ", "Lost Blocks", "Meridian Fold", "Dr. Yuki Anand-Petrov"],
    credibility: "verified",
    story_hooks: [
      "The building absorbed by the Meridian Fold — what happened to its occupants, and can they describe the transition?",
      "Academic journals withdrawing acceptance — who pressured them, and why suppress this data?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "growth", "cartography", "circuit_district"]
  },
  {
    name: "Navigation Ghosts",
    document_type: "incident_compilation",
    author: "Circuit District Resident Advisory Board",
    date: "2225-09-12",
    classification: "public",
    description:
      "BCI navigation systems in the Circuit District of GLMZ occasionally route pedestrians and vehicle operators through Lost Blocks as if they are normal, traversable streets. The routes appear without error flags, without detour warnings, without any indication that the suggested path passes through space that cannot be physically entered. The turn-by-turn directions reference street names that exist on maps — Linden Street, Caulfield Lane, Fold Avenue — and provide distance estimates, travel times, and arrival predictions consistent with the streets being real, open, and navigable. People following these routes hit walls.\n\nThe Resident Advisory Board has compiled 847 reports of navigation ghost events over the past three years. The pattern is consistent: a BCI navigation query returns a route that includes one or more segments through a Lost Block. The user follows the route. The user arrives at a wall where the navigation indicates a turn or a passage. The navigation recalculates, providing an alternate route that avoids the anomaly zone. The user continues to their destination. The event is logged, reported, and forgotten.\n\nBut 23 of the 847 reports describe something different. In these cases — always late at night, always when the user is alone, always in low-traffic areas adjacent to Lost Block perimeters — the user reports that the turn was there. The alley existed. The street opened. For a moment — seconds, maybe less — the path the navigation suggested was physically present. Then it wasn't. The wall was back. The alley was gone. In eleven of the 23 cases, the user reports having taken a step or two into the opening before it closed. In three cases, users report seeing the interior of a Lost Block street — buildings, lights, pavement — for the duration of the opening. None of the 23 users entered fully. All of them describe the same sensation: that they were being invited, and that the invitation was withdrawn.\n\nThe Advisory Board has forwarded these reports to the BCI navigation service providers. The providers attribute the routing errors to 'legacy map data persistence' and have not addressed the 23 anomalous cases. The Advisory Board notes that the 23 cases are not evenly distributed — 17 of them occurred near the Meridian Fold, the largest and most active Lost Block. The Board has recommended that BCI providers implement geofencing around confirmed anomaly zones. The recommendation has not been adopted. The navigation ghosts continue. The invitations continue.",
    related_entities: ["Circuit District", "GLMZ", "Lost Blocks", "Meridian Fold"],
    credibility: "verified",
    story_hooks: [
      "The 23 anomalous cases — are the Lost Blocks selectively opening, and if so, for whom?",
      "Eleven people took steps inside before it closed — what did their BCIs record during those seconds?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "bci", "navigation", "circuit_district"]
  },
  {
    name: "The Postal Service Anomaly",
    document_type: "incident_report",
    author: "GLMZ Automated Logistics Division",
    date: "2225-06-28",
    classification: "restricted",
    description:
      "The automated postal system serving GLMZ's Circuit District successfully delivers mail and packages to eleven addresses located within confirmed Lost Blocks. This is not a legacy routing error. These are active deliveries — packages sent by external parties to addresses that, according to every ground-level survey, do not exist and cannot be reached. The packages arrive. The tracking data confirms delivery. The system works. It should not work.\n\nDelivery tracking for these eleven addresses follows the same pattern: the postal drone departs from the Circuit District distribution hub, follows a route that passes over the Lost Block perimeter, descends to street level within the anomaly zone, and completes delivery at the destination address. GPS tracking shows the drone navigating streets, approaching buildings, and landing at delivery points that correspond to structures visible in satellite imagery. The drone's onboard cameras record video of the entire delivery — streets, buildings, doors, and in some cases, recipients who open the door and accept the package. The footage shows a normal urban neighborhood. Clean. Maintained. Occupied.\n\nWhen human investigators attempt to follow the same route, on foot or by vehicle, they encounter the same walls that define every Lost Block. There are no streets. There are no doors. There are no buildings accessible from ground level. The drone navigates a city that the humans cannot enter. The drone's cameras see a neighborhood that the humans cannot see. The drones are not confused. The humans are not confused. They are experiencing different cities.\n\nThe Automated Logistics Division has attempted to modify the drone routing to gather additional intelligence. Drones instructed to hover and perform extended surveillance within the Lost Blocks return footage of empty streets — the same streets that show occupied buildings in delivery footage. Drones instructed to deviate from the delivery route and explore adjacent streets within the anomaly zone experience navigation errors and return to base. The Lost Blocks permit delivery. They do not permit exploration. Whatever mechanism allows the drones in, it operates on terms that the Division does not control and does not understand.\n\nThe eleven active delivery addresses have been receiving mail for between two and seven years. The recipients have names. The packages contain mundane items — clothing, electronics, food supplements, books. Someone in the Lost Blocks orders things online. Someone in the Lost Blocks has accounts, payment methods, and shipping addresses. Someone in the Lost Blocks is living a normal consumer life in a place that doesn't exist. The Division has not attempted to discontinue service to these addresses. The Division is not sure it could.",
    related_entities: ["Circuit District", "GLMZ", "Lost Blocks"],
    credibility: "verified",
    story_hooks: [
      "The eleven recipients have names and accounts — can they be contacted through digital channels?",
      "Drones see occupied buildings during delivery but empty streets during surveillance — the blocks know the difference"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "postal", "drones", "logistics"]
  },
  {
    name: "The Theory of Urban Cysts",
    document_type: "academic_paper",
    author: "Dr. Sato Mbeki-Larsen",
    date: "2225-08-04",
    classification: "public",
    description:
      "This paper proposes a theoretical framework for understanding the spatial anomalies colloquially known as the Lost Blocks of GLMZ. The framework draws on recent developments in topological field theory, non-Euclidean urban geometry, and the emerging discipline of spatial pathology — the study of spaces that are, in a precise mathematical sense, sick. The author proposes that the Lost Blocks are urban cysts: pockets where the city's geometry has folded in on itself, creating spaces that are topologically connected to the surrounding urban fabric but not geometrically accessible through normal three-dimensional movement.\n\nThe concept requires elaboration. In standard urban geometry, any point within a city is reachable from any other point by a continuous path — you can walk there. Topological connection is weaker: two spaces are topologically connected if they share boundary conditions, energy flows, and information exchange, even if no continuous physical path exists between them. The Lost Blocks share all of these with the surrounding city. Power flows in. Heat flows out. Electromagnetic signals propagate across the boundary (postal drones navigate successfully within the blocks). The blocks are connected. They are not accessible. This is the defining characteristic of a cyst — an enclosed space within a body that is part of the body but isolated from it.\n\nThe mathematical model developed in this paper treats the city's spatial geometry as a manifold — a mathematical surface that can curve, fold, and develop singularities. The author demonstrates that under certain conditions (population density, infrastructure complexity, electromagnetic field density, and a fourth parameter the author designates 'spatial stress'), the urban manifold can develop invaginations — regions where the surface folds inward, creating enclosed pockets that are technically part of the same surface but practically unreachable from the exterior. The conditions for invagination are specific, calculable, and — critically — the model predicts them.\n\nThe model correctly predicts the locations of all seven known Lost Blocks. It also predicts four additional invagination sites that have not been identified as anomalous. The author has not disclosed these locations publicly, noting that if the predictions are correct, drawing attention to them could accelerate the process. The author further notes that the model predicts the growth rate of existing invaginations (consistent with Dr. Anand-Petrov's 3% decadal observation) and suggests that growth is not linear but logistic — slow initially, accelerating through a middle phase, and eventually stabilizing when the cyst reaches equilibrium with the surrounding manifold. The current Lost Blocks, according to the model, are in the early acceleration phase.\n\nThe physics is speculative. The author is the first to acknowledge this. But the math works. It predicts what exists, it predicts where it exists, and it predicts how it will change. The author's concluding observation is that if the model is correct, the Lost Blocks are not anomalies. They are symptoms. The city is developing cysts because the city is under spatial stress, and cyst formation is the geometry's way of relieving that stress. The implication is that eliminating the Lost Blocks — if such a thing were possible — would not solve the problem. The stress would express itself elsewhere. The city would fold again. The question is not how to open the Lost Blocks. The question is what is putting the city under stress.",
    related_entities: ["GLMZ", "Lost Blocks", "Circuit District", "Dr. Sato Mbeki-Larsen", "Dr. Yuki Anand-Petrov"],
    credibility: "credible",
    story_hooks: [
      "The four predicted but unidentified invagination sites — are they forming now?",
      "If spatial stress causes the cysts, what is the source of the stress — infrastructure, population, or something else?"
    ],
    tags: ["document", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "theoretical_physics", "topology", "academic"]
  }
];

// ─── PLACES ──────────────────────────────────────────────────────────────────

const places = [
  {
    name: "Linden Block",
    aliases: ["AZ-1", "The Linden", "Desta's Block"],
    description:
      "The most documented of GLMZ's Lost Blocks, Linden Block occupies the space between McKenzie Street and Halsted Street in the Circuit District. Satellite imagery shows approximately twelve buildings arranged along two parallel streets — Linden Street and an unnamed cross-street — with what appears to be a small park or green space at the eastern end. The buildings are residential in character: three to four stories, flat-roofed, with windows that show curtains and occasional interior lighting during nighttime satellite passes. Twenty-two confirmed delivery records exist from the period before the block became inaccessible, documenting a functional neighborhood with residents who received packages, signed for deliveries, and lived ordinary lives on streets that now cannot be found.\n\nLinden Block became inaccessible in 2219. Before that year, the alley connecting McKenzie Street to Linden Street was a normal urban passage — delivery drivers used it, residents walked through it, BCI navigation routed through it without incident. At some point during 2219, the alley ceased to exist. No construction was observed. No demolition permits were filed. No noise, no dust, no equipment. The alley was there, and then a continuous brick wall was there, and the wall showed decades of weathering as if it had always been the only thing in that location. Thermal imaging shows approximately 80 heat signatures within the block — the largest concentration of any Lost Block. They move through the space on daily rhythms. They cook. They sleep. They appear to gather in the small park during evening hours. They are unreachable.\n\nThe delivery driver Ria Okonkwo-Salazar made seventeen documented deliveries to 4412 Linden Street between 2218 and 2219, the final delivery occurring three weeks before the block became inaccessible. Her delivery records, BCI navigation logs, and signed receipts constitute the most detailed documentation of a Lost Block's interior from the period when it was still accessible. The recipient, a woman named Desta, has not been seen outside the block since 2219. Whether she is among the 80 thermal signatures, and whether she could leave if she wanted to, are questions that nobody can answer from this side of the wall.",
    atmosphere: {
      sights: [
        "From satellite: twelve residential buildings, two parallel streets, a small green space with what appears to be mature trees",
        "From street level: continuous weathered brick wall along McKenzie and Halsted, with no seams, doors, or evidence of alteration",
        "BCI overlay showing streets, building outlines, and navigation routes superimposed on a wall that denies all of it",
        "Nighttime satellite passes showing interior lighting in several buildings — warm, residential, lived-in"
      ],
      sounds: [
        "Silence — the wall absorbs or blocks all sound from the interior. Residents of adjacent buildings report hearing nothing from the block's direction.",
        "The ambient noise of McKenzie and Halsted — traffic, pedestrians, the hum of a normal city surrounding an impossible void"
      ],
      smells: [
        "Nothing from the block itself — no cooking smells, no exhaust, no vegetation scent despite the apparent green space",
        "The ordinary urban smell of the surrounding streets — concrete, exhaust, food vendors"
      ],
      feel: "Standing at the wall where the alley used to be produces a specific, widely reported sensation: the feeling of having forgotten something important. Not unease, not fear — a nagging sense that you were supposed to remember how to get in, and you've lost it. Some visitors describe it as homesickness for a place they've never been.",
      tags: []
    },
    demographics: "Approximately 80 individuals based on thermal imaging. No demographic data available — the population is observed only as heat signatures. The Shadow Census estimates 18-22 household units based on power consumption disaggregation.",
    economy: "Unknown. Bulk power consumption is paid through an opaque holding entity. No commercial activity is visible from satellite. The postal service delivers to three confirmed addresses within the block.",
    power_structure: "Unknown. No governance structure has been observed or documented.",
    dangers: [
      "Inaccessibility — the block cannot be entered through any known ground-level route",
      "Navigation ghosts — BCI systems occasionally route through the block, leading users to walls",
      "Psychological effect — prolonged proximity to the perimeter wall produces the widely reported 'forgotten memory' sensation"
    ],
    opportunities: [
      "Ria Okonkwo-Salazar's delivery records provide the most detailed interior documentation of any Lost Block",
      "The three postal delivery addresses represent active communication channels into the block",
      "The Underworld connection described by anonymous explorer 'foldwalker' suggests sub-surface access may be possible"
    ],
    story_hooks: [
      "Desta at 4412 Linden Street received seventeen deliveries — she had a life, a routine, a green door. Is she still there?",
      "The block became inaccessible in 2219. What happened in 2219 that didn't happen in 2218?",
      "A message arrives from inside Linden Block, delivered by postal drone. It's addressed to Ria Okonkwo-Salazar. It says 'The door is still green.'"
    ],
    connections: {
      adjacent_to: [
        "McKenzie Street (Circuit District — western perimeter)",
        "Halsted Street (Circuit District — eastern perimeter)",
        "Circuit District surface grid"
      ],
      exits: [],
      tags: []
    },
    frequented_by: [
      "Approximately 80 unidentified thermal signatures",
      "Postal delivery drones servicing three active addresses",
      "Urban researchers and curiosity seekers who come to stand at the wall"
    ],
    notable_locations: [],
    coordinates: { lat: 46.8, lng: -87.9, tags: [] },
    tags: ["place", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "circuit_district"]
  },
  {
    name: "The Caulfield Pocket",
    aliases: ["AZ-2", "Caulfield", "The Pocket", "The Fountain Block"],
    description:
      "The smallest of GLMZ's Lost Blocks, the Caulfield Pocket consists of only three buildings arranged around a central courtyard, occupying less than half a city block in the Circuit District. It is named for Caulfield Lane, the street that once provided access — a narrow pedestrian passage between two commercial buildings that opened onto the courtyard. The passage is now a wall. The courtyard is visible from the rooftop of the adjacent Halsted Parking Structure, which rises three stories above the pocket's buildings, providing a direct overhead view that supplements satellite imagery.\n\nOne researcher, Dr. Yuki Anand-Petrov, lowered a camera on a rope from the parking structure roof during her cartographic survey. The camera descended approximately eight meters before the rope went slack — not cut, not tangled, slack, as if the distance between the roof and the courtyard had shortened while the camera was in transit. The footage recovered shows an empty, clean courtyard with decorative paving stones arranged in a geometric pattern. At the center of the courtyard is a fountain — a simple basin-and-column design, perhaps two meters tall. The fountain is running. Clean water flows from the column into the basin and recirculates. The courtyard is maintained. The paving stones are swept. The fountain works. Nobody maintains it. Nobody sweeps it. Nobody repairs the pump.\n\nThermal imaging shows approximately 15 heat signatures within the three buildings. The Caulfield Pocket shows the least activity of any Lost Block — the signatures move infrequently, spending long periods stationary in what appear to be seated or reclined positions. The Shadow Census's power consumption analysis shows minimal electrical usage, well below what would be expected for 15 occupants. Either the residents use very little power, or the residents are not doing the things that require power. The Caulfield Pocket is the only Lost Block that has shown no measurable growth over the ten-year observation period. It is stable. It is small. It is the only Lost Block where you can stand on a rooftop and look directly down into the space you cannot enter, watching a fountain run in a courtyard you'll never reach.",
    atmosphere: {
      sights: [
        "From parking structure roof: three low buildings surrounding a clean courtyard with a running fountain",
        "Camera footage: geometric paving stones, basin-and-column fountain, clean water flowing",
        "From street level: a solid wall where Caulfield Lane used to be — featureless, old, unbroken",
        "Thermal imaging: 15 mostly-stationary heat signatures in the buildings"
      ],
      sounds: [
        "On very quiet nights, from the parking structure roof, some observers report hearing the fountain — a faint sound of running water from below",
        "Silence from street level — the wall and buildings block all interior sound"
      ],
      smells: [
        "Faint mineral smell from the parking structure roof on humid days — possibly from the fountain's water",
        "Nothing from street level"
      ],
      feel: "The Caulfield Pocket produces a different sensation than the other Lost Blocks. Standing on the parking structure roof, looking down at the courtyard, visitors report calm — not the unsettling 'forgotten memory' of Linden Block, but a stillness that feels intentional. Several visitors have described it as 'a place that wants to be left alone.' The fountain runs. The courtyard is clean. The pocket asks nothing of the outside world.",
      tags: []
    },
    demographics: "Approximately 15 individuals based on thermal imaging. The lowest population density of any Lost Block. The Shadow Census notes anomalously low power consumption.",
    economy: "Minimal. Almost no measurable economic activity. No known postal deliveries. Power consumption is the lowest of any Lost Block, barely above what empty buildings would draw.",
    power_structure: "Unknown.",
    dangers: [
      "Camera rope anomaly — physical objects lowered into the pocket experience spatial distortion",
      "The pocket's stability may be deceptive — its growth rate is zero, but its nature is no less anomalous"
    ],
    opportunities: [
      "Direct overhead visual access from the Halsted Parking Structure",
      "The camera rope incident suggests that the boundary is permeable to objects if not to people",
      "The fountain's water supply must come from somewhere — tracing the water infrastructure could reveal connection points"
    ],
    story_hooks: [
      "Lower another camera — a live-feed camera with audio. What does the courtyard sound like? What happens at night?",
      "The 15 signatures barely move. Are they people? Are they alive in the way we understand alive?",
      "The fountain is running. Someone built it. Someone plumbed it. Someone turned it on. That person might still be down there."
    ],
    connections: {
      adjacent_to: [
        "Halsted Parking Structure (Circuit District — direct rooftop view into the pocket)",
        "Caulfield Lane (former access — now a wall)",
        "Circuit District commercial buildings"
      ],
      exits: [],
      tags: []
    },
    frequented_by: [
      "Approximately 15 mostly-stationary thermal signatures",
      "Researchers on the Halsted Parking Structure roof",
      "Nobody else — the pocket is small, quiet, and easy to overlook"
    ],
    notable_locations: [],
    coordinates: { lat: 46.81, lng: -87.88, tags: [] },
    tags: ["place", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "circuit_district"]
  },
  {
    name: "Block 19-South",
    aliases: ["AZ-3", "Nineteen South", "The Shelf Block"],
    description:
      "Block 19-South is a Lost Block located in the Shelf, one of GLMZ's lower-income districts, distinguished from the other anomaly zones by the fact that its transition from accessible to inaccessible was witnessed — or rather, its aftermath was witnessed — by the surrounding community. Shelf residents remember Block 19-South. They remember the alley that connected it to the main street. They remember the families who lived there. They remember the corner store. And they remember the morning they walked past and the alley was a wall.\n\nNobody saw it close. Nobody heard construction. There was no warning, no sign, no gradual change. One evening, the alley was an alley — people walked through it, a kid on a bicycle rode out of it, the corner store's neon sign was visible through the gap. The next morning, the alley was a wall. Not a new wall — an old wall, weathered and stained and integrated seamlessly into the surrounding buildings as if it had been there for fifty years. The kid on the bicycle was inside. The families were inside. The corner store was inside. Thermal imaging now shows approximately 55 heat signatures. Some of them are small — child-sized. They have been child-sized for seven years. Children who don't grow.\n\nThe Shelf community has not forgotten Block 19-South the way the rest of GLMZ has forgotten the Lost Blocks. These were their neighbors. Mrs. Achebe ran the corner store. The Volkov-Osei kids played in the alley. Jian-Carlo fixed bicycles in a ground-floor workshop. The Shelf residents organized. They petitioned the city. They hired Dr. Anand-Petrov to include Block 19-South in her survey. They stand at the wall on the anniversary of the closure — they've marked the date, March 14, 2222 — and they call out the names of the people they know are inside. The thermal signatures don't respond. The thermal signatures go about their daily patterns, cooking and sleeping and moving through a space that their former neighbors can describe from memory but can never reach again.\n\nBlock 19-South is the emotional center of the Lost Block phenomenon. The other blocks are abstract — satellite anomalies, cartographic puzzles, theoretical physics. Block 19-South is Mrs. Achebe's corner store. It's the Volkov-Osei kids. It's people with names and faces and neighbors who miss them, sealed behind a wall that pretends it was always there.",
    atmosphere: {
      sights: [
        "From satellite: densely packed residential block with narrow streets, a corner commercial space, a small open area that was once a shared courtyard",
        "From street level: weathered wall, seamlessly integrated into adjacent Shelf buildings, indistinguishable from original construction",
        "Memorial markers left by Shelf residents at the base of the wall — flowers, photographs, handwritten notes",
        "Thermal imaging showing 55 signatures including several child-sized, moving through familiar domestic patterns"
      ],
      sounds: [
        "The Shelf is not quiet — the surrounding neighborhood is active, lived-in, noisy with life",
        "On the anniversary, the sound of Shelf residents calling names at the wall. The wall does not answer."
      ],
      smells: [
        "The Shelf's own smells — street food, engine grease, the mineral tang of old infrastructure",
        "Nothing from the block itself — sealed, silent, scentless"
      ],
      feel: "Block 19-South feels like grief. The surrounding community's loss is palpable — this is not an abstract anomaly to them, it's an amputation. Standing at the wall, you feel the weight of specific absence: these people had names. They had lives. They are twenty feet away and they might as well be on the moon. The Shelf residents' refusal to forget — the memorials, the anniversaries, the petitions — gives Block 19-South a human gravity that the other Lost Blocks lack.",
      tags: []
    },
    demographics: "Approximately 55 individuals based on thermal imaging, including several child-sized signatures. Former residents were predominantly working-class Shelf families. The community outside maintains detailed records of who was inside when the block closed.",
    economy: "Unknown. The corner store presumably no longer serves external customers. No postal deliveries. Power consumption is moderate, consistent with residential use.",
    power_structure: "Unknown internally. Externally, the Shelf Residents' Association for Block 19-South maintains organized advocacy.",
    dangers: [
      "Emotional hazard — Block 19-South is a site of active community grief and anger",
      "The child-sized thermal signatures that haven't grown in seven years raise questions about the nature of time inside the blocks",
      "Shelf residents may take direct action — several proposals to breach the wall have been discussed"
    ],
    opportunities: [
      "The Shelf community's detailed knowledge of former residents provides the best pre-closure intelligence of any Lost Block",
      "Community advocacy keeps political pressure on the city to investigate",
      "The wall's precise closure date (March 14, 2222) narrows the window for investigating what triggered the transition"
    ],
    story_hooks: [
      "Mrs. Achebe's corner store had a basement. The Shelf's utility tunnels run under the block. Has anyone checked whether the Underworld connects to 19-South?",
      "The Volkov-Osei children's thermal signatures haven't grown. Are they frozen in time? Are they even children anymore?",
      "A Shelf resident finds a note in their mailbox, in Mrs. Achebe's handwriting: 'We can hear you on the anniversary. Don't stop.'"
    ],
    connections: {
      adjacent_to: [
        "The Shelf main street (western perimeter)",
        "Shelf residential blocks (north and south)",
        "Shelf utility infrastructure (sub-surface)"
      ],
      exits: [],
      tags: []
    },
    frequented_by: [
      "Approximately 55 thermal signatures including children",
      "Shelf Residents' Association members maintaining the memorial",
      "Dr. Yuki Anand-Petrov's research team"
    ],
    notable_locations: [],
    coordinates: { lat: 46.78, lng: -87.92, tags: [] },
    tags: ["place", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "the_shelf", "community"]
  },
  {
    name: "The Meridian Fold",
    aliases: ["AZ-4", "The Fold", "The Big One"],
    description:
      "The Meridian Fold is the largest of the Lost Blocks, encompassing approximately six square blocks in the heart of the Circuit District. It is named by Dr. Sato Mbeki-Larsen, whose Theory of Urban Cysts was inspired in part by the Fold's sheer scale — this is not a hidden courtyard or a sealed alley, this is a significant piece of urban territory that is visible from orbit and absent from the ground. The Fold contains an estimated 18 buildings, a network of streets, two plazas, and what satellite imagery suggests is a small market or commercial district. It is the most complex, the most populated, and the most active of all the Lost Blocks. It is also the one that is growing.\n\nThe Meridian Fold became inaccessible in 2207 — the earliest of the confirmed Lost Blocks. For nearly two decades, it has existed as a visible, thermally active, power-consuming ghost neighborhood in the center of the district. Thermal imaging shows approximately 120 heat signatures — the largest concentration of any Lost Block — engaged in daily patterns that suggest not just habitation but community. Signatures congregate in the plazas. Groups move together. There are patterns that look like social behavior, like commerce, like daily life conducted at a scale that implies organization.\n\nBut the Fold is growing. Dr. Anand-Petrov's decadal analysis shows approximately 7% expansion — the highest rate of any Lost Block. The growth follows the street grid, extending along existing axes. A building that was demonstrably outside the Fold in 2224 — a four-story commercial building at the corner of Meridian Avenue and Circuit Street, occupied by a data storage firm — is now inside it. The building's former occupants describe a sudden transition: they arrived at work on a Monday morning and the entrance was a wall. Their equipment, their files, their personal belongings — all inside, all visible on satellite, all unreachable. The insurance claim is still in litigation. The building is still inside the Fold, its windows visible from orbit, its doors accessible to no one.\n\nDr. Mbeki-Larsen's model predicts that the Meridian Fold is in an early acceleration phase. If the model is correct, the growth rate will increase before stabilizing at a boundary that the math defines but that no one wants to calculate. The Fold is the Lost Block that forces the question: what happens when the anomaly stops being something you can ignore? What happens when it swallows your office, your apartment, your street? What happens when it stops being a curiosity and starts being a crisis?",
    atmosphere: {
      sights: [
        "From satellite: six blocks of dense urban space — buildings, streets, two plazas, what appears to be a market area with stalls or tables",
        "From street level: walls. Continuous, unbroken, unremarkable walls surrounding six blocks of absent city",
        "The absorbed building at Meridian and Circuit — windows visible from certain vantage points above, dark at street level",
        "Thermal imaging showing 120 signatures in complex social patterns — groups, gatherings, movement that implies community"
      ],
      sounds: [
        "The surrounding Circuit District is a busy commercial zone — the ambient noise masks any sound that might escape the Fold",
        "Some workers in buildings adjacent to the Fold's perimeter report feeling low-frequency vibration through shared walls — not heard, felt"
      ],
      smells: [
        "Faintly, and only at certain times — usually late evening — a smell of cooking from the Fold's perimeter. Spiced, unfamiliar, gone before you're sure it was there.",
        "The commercial bustle of the Circuit District overwhelms any consistent olfactory signal"
      ],
      feel: "The Meridian Fold feels like a presence. The other Lost Blocks feel like absences — spaces that should be there and aren't. The Fold feels like something that is there, that is large, that is active, and that is growing. Standing at its perimeter wall, you don't feel the 'forgotten memory' of Linden Block or the calm of the Caulfield Pocket. You feel proximity to something that has mass and momentum. The Fold is not hiding. The Fold is expanding. The Fold has plans.",
      tags: []
    },
    demographics: "Approximately 120 individuals based on thermal imaging — the largest Lost Block population. Movement patterns suggest organized social structures, commercial activity, and communal gathering. The Shadow Census estimates 30-35 household units.",
    economy: "Unknown internally, but the postal service delivers to five addresses within the Fold — the most of any Lost Block. Power consumption is the highest of any Lost Block, consistent with both residential and commercial activity.",
    power_structure: "Unknown, but the organized movement patterns visible in thermal imaging suggest some form of governance or coordination.",
    dangers: [
      "Growth — the Fold is expanding at 7% per decade and accelerating. Adjacent properties are at risk of absorption.",
      "The absorbed data storage building — whoever stored data there has lost physical access to their archives",
      "Low-frequency vibration felt through shared walls — unknown source, unknown effect",
      "17 of 23 'navigation ghost' incidents occurred near the Fold — it is the most active perimeter"
    ],
    opportunities: [
      "Five active postal delivery addresses — the most communication channels of any Lost Block",
      "The absorbed building's former occupants can describe the transition in detail",
      "The Fold's growth makes it the best candidate for studying the expansion mechanism",
      "Dr. Mbeki-Larsen's model makes testable predictions about the Fold's future boundary"
    ],
    story_hooks: [
      "The data storage building contained client archives. Someone important stored something they can't afford to lose in a building the city swallowed. They want it back.",
      "The Fold's market area shows activity consistent with commerce. Who is buying? Who is selling? What is the currency?",
      "A building on the Fold's predicted expansion boundary gets a new tenant who refuses to leave despite warnings. They want to be absorbed. They want to get in."
    ],
    connections: {
      adjacent_to: [
        "Meridian Avenue (Circuit District — northern perimeter)",
        "Circuit Street (Circuit District — western perimeter)",
        "Multiple Circuit District commercial and residential blocks"
      ],
      exits: [],
      tags: []
    },
    frequented_by: [
      "Approximately 120 thermal signatures in organized social patterns",
      "Postal delivery drones servicing five active addresses",
      "Dr. Sato Mbeki-Larsen's research team monitoring growth",
      "Insurance adjusters from the absorbed building's coverage provider"
    ],
    notable_locations: [],
    coordinates: { lat: 46.82, lng: -87.87, tags: [] },
    tags: ["place", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "circuit_district", "growing"]
  },
  {
    name: "Ghost Acres",
    aliases: ["AZ-5", "The Ghost", "The Flickering Block", "Old Harbor Anomaly"],
    description:
      "Ghost Acres is the most unsettling of the Lost Blocks — not because of what it contains, but because of what it is. Located near Old Harbor in GLMZ's waterfront district, Ghost Acres is the only Lost Block that is not consistently visible. It appears on some satellites but not others. It is present in some orbital passes and absent from others taken hours later. Infrared imaging captured at 0200 shows a neighborhood — buildings, streets, an open space that might be a dock or pier. The same infrared array at 1400 shows an empty lot. Ghost Acres does not simply resist entry. Ghost Acres resists existence.\n\nThe intermittent visibility follows patterns that researchers have struggled to define. Time of day matters — Ghost Acres is more frequently visible during nighttime passes, but not exclusively. Weather does not appear to correlate. The satellite's orbital angle matters somewhat, with lower-angle passes more likely to capture the block, but exceptions are frequent. One researcher has proposed that the visibility correlates with tidal patterns in Lake Superior — not perfectly, but with a statistical significance that is difficult to dismiss. If the lake's water level at Old Harbor is above a specific threshold (which the researcher has calculated to four decimal places), Ghost Acres is more likely to be visible. If the water is below that threshold, Ghost Acres is more likely to be absent. The correlation is approximately 73%. This is not conclusive. This is also not random.\n\nWhen Ghost Acres is visible, thermal imaging shows approximately 70 heat signatures. When it is not visible, the signatures are also absent — not hidden, absent. The heat is not there. The power consumption drops to zero. The space registers as an empty lot on every sensor. Then, hours or days later, it is a neighborhood again, with 70 signatures going about daily routines as if they had never been interrupted. The signatures do not show startup patterns — they don't wake up, turn on lights, begin activity. They are mid-activity. They are cooking a meal that was not being cooked a moment ago. They are walking down a street that did not exist a moment ago. They resume.\n\nGhost Acres is the Lost Block that keeps physicists awake at night. The other blocks are spatially anomalous — present but inaccessible. Ghost Acres is temporally anomalous — intermittently present, intermittently real, tied to conditions that suggest it exists on a threshold between states. Dr. Mbeki-Larsen's urban cyst model does not predict Ghost Acres. It predicts the other six. When asked about Ghost Acres, the physicist paused for a long time and said: 'That one isn't a cyst. That one is something else.'",
    atmosphere: {
      sights: [
        "When present: waterfront neighborhood, low buildings, a dock or pier structure, approximately 70 thermal signatures",
        "When absent: an empty lot near Old Harbor, unremarkable, forgettable",
        "The transition: there is no transition. It is there or it is not. Satellite footage shows no fade, no shimmer, no gradual appearance.",
        "From street level (when present on satellite): the same walls as other Lost Blocks, but somehow less convincing — as if the wall isn't trying as hard"
      ],
      sounds: [
        "Old Harbor ambient — water, boats, dock machinery, the constant low conversation of a working waterfront",
        "When Ghost Acres is 'present,' some Old Harbor workers report hearing sounds from the lot that shouldn't be there — footsteps, a door closing, fragments of conversation in no identifiable language"
      ],
      smells: [
        "Lake water and dock industry — Old Harbor's pervasive waterfront smell",
        "Occasionally, when Ghost Acres is transitioning (if it transitions at all), a sharp ozone smell reported by dock workers"
      ],
      feel: "Ghost Acres feels like a gap in attention. You look at the lot and your eye slides off it. You try to focus on the space and your mind wanders. It is not invisible — it is uninteresting in a way that feels manufactured, as if the space is actively discouraging observation. People who force themselves to stare at the lot for extended periods report headaches, mild nausea, and a persistent sensation that they are being rude — that they are staring at something that has asked not to be looked at.",
      tags: []
    },
    demographics: "Approximately 70 individuals when present, based on thermal imaging. Zero when absent. The population does not appear to experience the transition — they are mid-activity when they appear and mid-activity when they disappear.",
    economy: "Unknown. No postal deliveries — the block is not consistently present enough for automated routing. Power consumption spikes when the block is present and drops to zero when absent.",
    power_structure: "Unknown. The intermittent existence makes observation difficult.",
    dangers: [
      "Intermittent existence — a space that isn't always there is a space you cannot plan around",
      "The tidal correlation suggests Ghost Acres is connected to forces larger than urban geometry",
      "Observation resistance — the space actively discourages attention, which may mask other hazards",
      "Dr. Mbeki-Larsen's model doesn't explain it — whatever Ghost Acres is, it's outside the current theoretical framework"
    ],
    opportunities: [
      "The 73% tidal correlation provides a predictive window for observation",
      "If the block is intermittently present, it may be intermittently accessible — a door that isn't always a wall",
      "The ozone smell during transitions suggests an energetic process that could be measured",
      "Ghost Acres may be the key to understanding the Lost Blocks — the exception that reveals the rule"
    ],
    story_hooks: [
      "A dock worker times the tidal pattern perfectly and sees Ghost Acres appear while standing at its perimeter. For thirty seconds, there's a street where the wall was. He doesn't go in. He takes a photograph. The photograph shows a wall.",
      "The 70 signatures inside Ghost Acres don't experience interruption. From their perspective, do they exist continuously? Is their time different from ours?",
      "Dr. Mbeki-Larsen says Ghost Acres isn't a cyst. She won't say what it is. Her notes, however, include the phrase 'urban scar tissue' with three question marks."
    ],
    connections: {
      adjacent_to: [
        "Old Harbor waterfront (GLMZ — dock district)",
        "Lake Superior shoreline infrastructure",
        "Old Harbor commercial district"
      ],
      exits: [],
      tags: []
    },
    frequented_by: [
      "Approximately 70 intermittently-existing thermal signatures",
      "Old Harbor dock workers who have learned to ignore the lot",
      "Researchers with tide charts and satellite schedules"
    ],
    notable_locations: [],
    coordinates: { lat: 46.77, lng: -87.95, tags: [] },
    tags: ["place", "lost_block", "spatial_anomaly", "new_weird", "inexplicable", "meridian_88", "old_harbor", "temporal_anomaly", "intermittent"]
  }
];

// ─── GENERATE ────────────────────────────────────────────────────────────────

let created = 0;
let skipped = 0;

for (const doc of documents) {
  const id = newId();
  const obj = {
    id,
    name: doc.name,
    type: "document",
    document_type: doc.document_type,
    author: doc.author,
    date: doc.date,
    classification: doc.classification,
    description: doc.description,
    related_entities: doc.related_entities,
    credibility: doc.credibility,
    story_hooks: doc.story_hooks,
    tags: doc.tags
  };
  if (writeIfNotExists(documentsDir, id, obj)) created++;
  else skipped++;
}

for (const place of places) {
  const id = newId();
  const obj = {
    id,
    type: "place",
    name: place.name,
    aliases: place.aliases,
    description: place.description,
    atmosphere: place.atmosphere,
    demographics: place.demographics,
    economy: place.economy,
    power_structure: place.power_structure,
    dangers: place.dangers,
    opportunities: place.opportunities,
    story_hooks: place.story_hooks,
    connections: place.connections,
    frequented_by: place.frequented_by,
    notable_locations: place.notable_locations,
    coordinates: place.coordinates,
    tags: place.tags
  };
  if (writeIfNotExists(placesDir, id, obj)) created++;
  else skipped++;
}

console.log(`\nDone. Created: ${created}, Skipped: ${skipped}, Total: ${created + skipped}`);
