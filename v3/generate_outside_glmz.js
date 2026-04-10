const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const outDir = path.resolve(__dirname, "..", "engine", "data", "documents");

function genId() {
  return crypto.randomBytes(16).toString("hex");
}

function writeDoc(doc) {
  const filePath = path.join(outDir, `${doc.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(doc, null, 2), "utf-8");
  console.log(`WROTE: ${filePath}`);
  return true;
}

const documents = [
  {
    name: "The Missouri Flood",
    document_type: "field_report",
    author: "GLMZ Regional Geography Commission",
    date: "2214-03-17",
    classification: "public",
    credibility: "disputed",
    description: `Missouri is not a state anymore. It is a swamp. The Mississippi River expanded beyond its banks in the 2140s after a cascade of levee failures that began in the southern reaches and propagated northward over a period of six years. The failure was not sudden — it was incremental, each breach compounding the next, each flood season leaving more water behind than the previous one. By 2149, the Army Corps of Engineers — or what remained of its institutional successor — declared the levee system irrecoverable. The water never receded. It found its level, and its level was everything below sixty meters elevation.

What exists now is a territory that satellite imaging classifies as wetland but which travelers describe as something closer to an inland sea with ambitions. The old river channel is indistinguishable from the surrounding flood plain. Cities that once sat on high ground — Jefferson City, Columbia, Springfield's northern reaches — are now islands, some accessible by boat, some not accessible at all. The infrastructure that connected them — highways, rail lines, power grids — is submerged or collapsed. Missouri's contribution to the continental economy ceased in the 2150s. Its contribution to continental mythology began shortly after.

Travelers who claim to have come from the Missouri Wetlands describe a world of fog, silence, and structures that sink a little more each year. They speak of platform villages built on stilts above the waterline, of houseboats lashed together into floating neighborhoods that drift with currents that follow no predictable pattern. They describe communities that have adapted entirely to aquatic life — people who navigate by the sound of water against submerged buildings, who trade in salvage pulled from drowned cities, who measure distance not in kilometers but in hours of paddling. The economy, if it can be called that, runs on barter. The currency is dry goods. Φ has no value where everything is wet.

Nobody from GLMZ has verified any of this. The few official survey missions dispatched into the Wetlands returned with inconclusive data — fog interfered with imaging, water depths were inconsistent with models, and GPS positioning drifted in ways that suggested either equipment malfunction or genuine navigational anomaly. One survey team reported finding a settlement of approximately two hundred people living on a platform constructed from the upper floors of a submerged shopping mall. They could not relocate the settlement on a subsequent visit. The coordinates matched. The mall was there. The platform was not.

The Missouri Wetlands remain officially uncharted territory. GLMZ cartographic databases list the region as "hydrologically active, status undetermined." Travelers continue to arrive in GLMZ from the west, damp and quiet, with stories of a world where the water is patient and the land is losing.`,
    related_entities: ["Missouri", "Mississippi River", "GLMZ", "GLMZ"],
    story_hooks: [
      "A salvage crew from GLMZ receives coordinates for something valuable submerged in the Missouri Wetlands — but the coordinates keep changing",
      "A traveler from Missouri carries a sealed container of water that, when tested, contains no pollutants — water that shouldn't exist in the 23rd century"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "missouri", "flood", "wetlands", "displacement"]
  },
  {
    name: "What Happened to Kentucky",
    document_type: "investigation",
    author: "GLMZ External Affairs Bureau",
    date: "2213-11-02",
    classification: "restricted",
    credibility: "unconfirmed",
    description: `Nobody knows what happened to Kentucky. Contact was lost in 2198, but the loss was not the clean severance that the word "lost" implies. It was a degradation. Transmissions from Kentucky's remaining population centers — Lexington, Louisville's highland districts, a handful of smaller communities in the eastern mountains — became intermittent starting in 2195. Messages arrived with increasing delays. Audio was garbled. Video feeds showed static that resolved, occasionally, into images of vegetation that communications analysts initially dismissed as camera obstruction. By 2197, the transmissions had reduced to automated beacon pulses. By March of 2198, the beacons stopped. The silence has been continuous since.

Three expeditions were sent between 2198 and 2201. All three returned. Their reports are consistent in the broad strokes and disturbing in the details. The roads into Kentucky — Interstate 65 from the north, Interstate 75 from the east — remained physically intact for approximately forty kilometers past the former state boundary. Beyond that point, the pavement was present but increasingly obscured by vegetation. Not overgrowth in the conventional sense. The expedition botanists described plant species that did not match any catalogued flora — growth that was too dense, too uniform, and too rapid. One report notes that a cleared section of road was re-covered within six hours of cutting. The growth was not aggressive. It was simply relentless.

A fourth expedition was dispatched in 2203 with heavier equipment and a larger team. It did not return. Search-and-rescue operations located the expedition's vehicles approximately sixty kilometers inside the former border, parked in orderly formation on what remained of a highway rest stop. The vehicles were intact. The equipment was intact. The personnel were absent. No signs of struggle, no biological traces beyond what would be expected from normal occupation, no indication of where twenty-three people went. The vehicles' onboard systems recorded nothing unusual up to the point where all recordings ceased simultaneously.

Satellite imaging of Kentucky shows green. Not the green of agriculture, not the green of managed forest, not the green of any land-use category that remote sensing analysts have established. It is uniform, unbroken green extending from the former state boundaries to the horizon in every direction. There are no structures visible. No roads. No infrastructure of any kind. Thermal imaging shows no heat signatures consistent with human habitation, industrial activity, or large animal populations. The thermal profile is consistent with dense vegetation and nothing else. The territory that was Kentucky is biologically active and civilizationally absent.

Kentucky appears on old maps. The GLMZ cartographic database retains the boundary lines as a historical reference. Inside those lines, the current classification reads: "Status unknown. No entry authorized." The authorization restriction is, in practice, unnecessary. Nobody wants to go.`,
    related_entities: ["Kentucky", "GLMZ", "GLMZ", "Louisville", "Lexington"],
    story_hooks: [
      "A BCI signal matching one of the fourth expedition members briefly appears in the GLMZ network — twenty-three years after they vanished",
      "A package arrives in GLMZ postmarked from Lexington, Kentucky, containing seeds of a plant species that doesn't exist in any botanical database"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "kentucky", "vegetation", "disappearance", "mystery"]
  },
  {
    name: "The Storytellers",
    document_type: "academic_paper",
    author: "Dr. Maren Okafor-Singh, GLMZ Institute for Social Anomalies",
    date: "2215-06-21",
    classification: "public",
    credibility: "verified",
    description: `Every city has transients. GLMZ has storytellers. They are not the same thing. Transients move through a city because they are going somewhere. The storytellers who arrive in GLMZ appear to have come from somewhere — they carry dust from roads, wear clothing suited to weather patterns that don't match the local climate, and speak with accents that linguists have been unable to place within any known regional dialect family. But their trajectories have no discernible destination. They arrive, they tell their stories, and they disappear. The disappearance is not dramatic. They simply stop being present. One day they are in a bar in the Hollows, describing a city on the Pacific coast where buildings grow like coral. The next day their rented room is empty, their tab is unpaid, and nobody saw them leave.

This paper examines 312 documented instances of storyteller contact in GLMZ between 2200 and 2215. The storytellers share several characteristics. They have no prior records in any GLMZ database — no birth certificates, no BCI registration, no employment history, no biometric matches. They possess detailed, specific knowledge of places outside the GLMZ that cannot be verified by any available means. Their accounts are vivid, internally consistent, and delivered with the conviction of firsthand experience. They are, by every available metric, telling the truth as they understand it.

The difficulty is that their truths are mutually exclusive. Cross-referencing storyteller accounts of the same geographical regions reveals not minor discrepancies but fundamental contradictions. One storyteller describes the Pacific Northwest as a thriving federation of independent city-states connected by high-speed rail. Another describes the same region as an uninhabited volcanic wasteland. A third describes it as an ocean — the coastline having moved two hundred kilometers inland. These accounts are not different perspectives on the same reality. They are descriptions of different realities occupying the same coordinates. The statistical analysis presented in Section 4 of this paper demonstrates that the probability of these contradictions arising from fabrication, confusion, or conventional unreliability is less than 0.3%.

The storytellers themselves seem unaware of the contradictions. When confronted with conflicting accounts from other storytellers, they express genuine puzzlement. They do not argue. They do not insist. They simply note that what they saw is what they saw, and they cannot account for what someone else claims to have seen. Several storytellers, when told that their descriptions match no known geography, have responded with variations of the same phrase: "Then your maps are wrong." This is not delivered as an argument. It is delivered as a statement of fact by someone who has no stake in whether you believe them.

No storyteller has ever been encountered twice. This is the most statistically anomalous finding of the study. In a city of GLMZ's size, with the volume of transient traffic it processes, the probability of zero repeat contacts over a fifteen-year study period is effectively nil. They come from nowhere that can be verified, they describe everywhere in terms that cannot be reconciled, and they vanish into a future that contains no further trace of them. The outside world, as described by those who claim to have seen it, is not a single place. It is many places. Or it is no place at all.`,
    related_entities: ["GLMZ", "GLMZ", "GLMZ Institute for Social Anomalies"],
    story_hooks: [
      "A storyteller arrives whose account perfectly matches a classified GLMZ survey report that was never made public — including details the survey team didn't include in the official version",
      "Two storytellers arrive simultaneously and recognize each other — the first recorded instance of a storyteller appearing to have a history with another person"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "storytellers", "transients", "unreliable_narration", "sociology"]
  },
  {
    name: "The Ohio Corridor",
    document_type: "field_report",
    author: "GLMZ Transit Authority, Corridor Operations Division",
    date: "2215-01-09",
    classification: "restricted",
    credibility: "verified",
    description: `The Ohio Corridor is the sole maintained overland transit route between GLMZ and the nearest verified city-state to the east, a distance of approximately 380 kilometers. Armed convoys depart GLMZ every seven days, carrying cargo and a limited number of authorized passengers. The corridor is narrow — a cleared and patrolled strip of road approximately 300 meters wide at its broadest, cutting through what used to be the state of Ohio. The road surface is maintained by automated systems that repair damage on a continuous cycle. The maintenance is necessary because the road sustains damage that has no clear origin — cracks that appear overnight, sections of asphalt that soften as though heated from below, lane markers that shift position by centimeters between surveys.

The land on either side of the corridor is classified as "non-standard terrain" in official documentation. This is a bureaucratic euphemism. The terrain resists categorization because it does not behave consistently. Aerial surveys conducted six months apart show different landscapes — forest in one survey, grassland in the next, and in one notable instance, a body of water approximately four kilometers in diameter that was present in March and absent in September. Ground-level observation is discouraged but occasionally unavoidable. Convoy security personnel report that the vegetation bordering the cleared zone is visually normal — deciduous trees, underbrush, wildflowers in season — but that it creates an impression of wrongness that none of them can adequately describe. The most common phrase in debriefing reports is "it looked like it was looking back."

Drivers are instructed, in clear and unambiguous operational language, not to stop. The instruction is reinforced during every pre-departure briefing. The corridor is not a place where you fix a flat tire or take a break. If a vehicle breaks down, the convoy continues and a recovery team is dispatched from the nearest hardpoint. Drivers who have stopped — through mechanical failure, accident, or in three documented cases, choice — report experiences that range from the mundane to the inexplicable. One driver who stopped for eleven minutes to address an engine warning reported that the vegetation at the road's edge was warm to the touch. Not sun-warm. Body-warm. The leaves had a temperature consistent with living tissue maintaining homeostasis. The driver did not investigate further. The driver requested a transfer to urban routes upon return.

The convoy schedule has not been disrupted in fourteen years of continuous operation. The route is reliable in the sense that convoys depart and convoys arrive. The transit time, however, varies. The 380-kilometer journey should take approximately five hours at convoy speed. Actual transit times range from four hours and forty minutes to seven hours and twelve minutes, with no correlation to weather, traffic, or road conditions. The additional time cannot be accounted for by the drivers' logs, which show continuous forward movement at consistent speed. The road is either longer some weeks than others, or time passes differently along certain stretches. The Transit Authority's official position is that the variation is due to "environmental factors affecting GPS calibration." Nobody in the Transit Authority believes this.

The Ohio Corridor works. Goods move. People travel. The eastern city-state receives its shipments and sends its own. The corridor is proof that the outside can be traversed, that the space between cities is navigable. It is also proof that the space between cities is not empty, not stable, and not indifferent to the people who pass through it.`,
    related_entities: ["GLMZ", "GLMZ", "Ohio", "GLMZ Transit Authority"],
    story_hooks: [
      "A convoy arrives carrying a passenger who wasn't on the manifest at departure — and who claims to have boarded at a stop that doesn't exist on the route",
      "The corridor's automated road maintenance systems begin repairing sections of road that lead off the designated route, as if maintaining paths to destinations no one authorized"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "ohio", "corridor", "transit", "convoy", "spatial_anomaly"]
  },
  {
    name: "Chicago Below",
    document_type: "personal_account",
    author: "Transcribed testimony of Yuki Adebayo-Chen",
    date: "2214-08-30",
    classification: "suppressed",
    credibility: "disputed",
    description: `Chicago didn't die the way cities usually die — not by fire, not by war, not by the slow hemorrhage of population that empties a place over generations. Chicago sank. The process began in the early 2100s, when Lake Michigan's water table began rising in ways that hydrological models could not predict or explain. The lower levels of the city flooded first — basements, sub-basements, the underground passages and service tunnels that formed the city's hidden circulatory system. Then the ground floors. Then the second floors. The flooding proceeded in stages over decades, each stage prompting a retreat upward. By 2180, everything below the fourth floor in the central districts was submerged. The water was not clean and it was not still. It moved with purpose, finding every gap, filling every void, rising with a patience that made engineering countermeasures feel like gestures.

The people who remained — and people did remain, because people always remain — divided along the waterline. Those who retreated upward call their territory the Stacks. Life in the Stacks is vertical. Buildings are connected by bridges, catwalks, and zip lines strung between upper floors. The streets below are canals now, navigable by small boats but useless for ground transport. The Stacks have a government of sorts, a economy of sorts, a culture that is recognizably urban but compressed into the upper registers of a city designed for horizontal living forced into vertical survival. The Stacks are hard but comprehensible. They are what you'd expect from human adaptation to a bad situation.

The Deep is not what you'd expect. Below the waterline, in the flooded lower floors and submerged streets, there are people. This is not disputed — sonar mapping confirms human-scale thermal signatures in submerged structures throughout the central district. How they survive is disputed. The water in Chicago's lower levels is murky, cold, and contaminated with a century of urban runoff. It is not water that supports human life. Diving teams sent to investigate the Deep have returned with footage of illuminated interiors — rooms that are fully submerged but furnished, lit by sources that the divers could not identify, and in some cases occupied by figures who moved through the water without visible breathing apparatus. The figures did not approach the divers. The divers did not approach the figures. Both parties observed each other through the murk with what one diver described as "mutual recognition and mutual disinterest."

I spent a month in the Deep. I will state this plainly because I have stated it under oath, under medical examination, and under BCI scan, and the results in all three cases were consistent with truthful testimony. I entered the water in the former Loop district using a standard diving rig. I was met by a woman who gestured for me to follow her. She was not wearing a diving rig. She was breathing water. I followed her into a building — the former Inland Steel Building, based on the architectural details — where I found a community of approximately forty people living in fully submerged conditions. They ate. They spoke — sound travels in water, and they had adapted their speech to work with it. They had furniture, tools, social structures. They had been there, by their account, for three generations.

I am not considered a reliable witness. My background includes two episodes of stress-related perceptual disturbance, both documented and both treated. The GLMZ Bureau of External Assessment has classified my testimony as "unverified personal account, credibility compromised by medical history." I accept this classification. I note, however, that my BCI recorded continuously during my month in the Deep. The recordings are intact. They show everything I have described. The Bureau has not classified the BCI data. They have not commented on the BCI data. They have filed it and moved on.`,
    related_entities: ["Chicago", "Lake Michigan", "GLMZ", "GLMZ"],
    story_hooks: [
      "A BCI recording from the Deep surfaces on the GLMZ black market — and it shows a submerged library containing books that were never written",
      "A delegation from the Stacks arrives in GLMZ requesting engineering assistance, but their real purpose is to seal the Deep permanently before something down there finishes adapting"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "chicago", "flooding", "submersion", "adaptation", "suppressed"]
  },
  {
    name: "The Wisconsin Quiet Zone",
    document_type: "field_report",
    author: "GLMZ Signal Intelligence Division",
    date: "2215-02-14",
    classification: "restricted",
    credibility: "verified",
    description: `The Wisconsin Quiet Zone is a region of northern Wisconsin in which all electromagnetic signals cease to propagate. The zone is not jammed — jamming implies an active countermeasure, a signal that overwhelms other signals. Within the Quiet Zone, there is no countermeasure. There is simply absence. Radio waves do not travel. Wireless communications do not function. BCIs go dark — not damaged, not disrupted, but silenced, as though the medium through which they transmit has been removed. Drones that enter the zone lose telemetry and fall. The fall is immediate and total, consistent with simultaneous failure of all electronic systems. The drones are recoverable. Their hardware is undamaged. They simply stopped working and started again when removed from the zone.

The boundary of the Quiet Zone is remarkably precise. Signal Intelligence teams have mapped it with handheld instruments, walking the perimeter and noting the exact point at which their equipment transitions from functional to inert. The transition occurs over a distance of less than two meters. On one side, full signal propagation. On the other, nothing. The boundary does not follow any geographical feature — it crosses hills, valleys, rivers, and roads with geometric indifference. It is, as near as the mapping teams can determine, a perfect circle, centered on a point in the Chequamegon-Nicolet National Forest approximately 40 kilometers southeast of Ashland.

The zone has been expanding at a rate of approximately two kilometers per year for as long as it has been measured. The earliest confirmed observation dates to 2187, when a forestry survey team reported equipment failure in a region that subsequent mapping determined to be within the zone's then-smaller boundary. If the expansion rate has been constant — and there is no evidence to suggest otherwise — the zone may have originated as early as the 2150s, though no records from that period reference it directly. At its current rate of expansion, the zone's boundary will reach the Lake Superior shoreline within fifteen years and the outskirts of GLMZ's northern sensor network within forty.

What is inside the zone is difficult to determine precisely, because the instruments used to determine such things do not function there. Foot patrols — conducted with analog equipment, paper maps, and magnetic compasses, which continue to work — report dense but unremarkable boreal forest. Pine, spruce, birch. Undergrowth consistent with the regional biome. Wildlife is present and behaves normally. The air is clean. The temperature is appropriate for the season. There is nothing visibly wrong with the Quiet Zone. It is a forest that happens to be electromagnetically inert, and it is getting larger.

The Iowan Behemoths pass through the Quiet Zone without apparent difficulty. This is the single most confounding observation in the Signal Intelligence Division's files. The Behemoths are machines — autonomous, massive, electromagnetically active machines that should be as affected by the zone as any other electronic system. They are not. They enter, they traverse, they exit. Their operational status appears unchanged. The Division has proposed seventeen hypotheses to explain this. None have survived peer review. The Behemoths do not explain themselves, and the Quiet Zone does not explain the Behemoths. The two phenomena coexist with an indifference that suggests either no relationship or a relationship so fundamental that it operates below the level at which human instruments can detect it.`,
    related_entities: ["Wisconsin", "GLMZ", "Iowan Behemoths", "GLMZ", "Chequamegon-Nicolet National Forest"],
    story_hooks: [
      "A foot patrol returns from the Quiet Zone carrying a handwritten journal found in an abandoned cabin — the journal describes the zone from the inside, written by someone who chose to live without electronics",
      "The expansion rate of the Quiet Zone suddenly doubles, and the new boundary now encompasses a small town that wasn't evacuated in time"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "wisconsin", "quiet_zone", "electromagnetic", "behemoths", "expansion"]
  },
  {
    name: "The Canadian Border",
    document_type: "field_report",
    author: "GLMZ Diplomatic Reconnaissance Office",
    date: "2213-07-19",
    classification: "restricted",
    credibility: "disputed",
    description: `The Canadian border exists as a line on maps and as a legal abstraction in treaties that predate the current geopolitical order by more than a century. In practice, the border between the Great Lakes Metropolitan Zone and Canadian territory is a gradient — a region approximately fifty kilometers wide in which one political reality fades into another without a clear point of transition. There are no walls, no checkpoints, no fences. There are signs, weathered and largely illegible, marking a boundary that both sides acknowledge and neither side enforces with any consistency. The border is crossed by wildlife, by weather, and occasionally by people who have reasons that they rarely share.

Canadian territory, as observed from GLMZ reconnaissance and as described by the infrequent travelers who cross from the north, is colder, less densely populated, and governed by a corporate structure that maintains no diplomatic communication with GLMZ corponations. This is not hostility. It is simply disconnection — a mutual disinterest so thorough that it functions as policy. The Canadian corporate entities — their names are known only through second-hand accounts, as they do not advertise, do not export, and do not recruit from outside their territory — appear to operate on principles that are structurally similar to GLMZ corponations but philosophically opaque. They employ people. They produce goods. They maintain infrastructure. Beyond these basics, their operations are a blank.

Travelers from across the border are rare — perhaps a dozen per year arrive in GLMZ with credible claims of Canadian origin. Their accounts are consistent in their inconsistency. They describe a world that sounds like the GLMZ reflected in a slightly warped mirror. The cities are similar in scale and function. The technology is recognizable. The social structures are familiar. But something is different in ways that the travelers struggle to articulate. The architecture is wrong, they say — not ugly, not alien, just wrong, as though the buildings were designed by someone who understood the principles of human habitation but had learned them from a description rather than from experience. The light is wrong. The angles at which sunlight enters windows don't match what the travelers remember from before they crossed. One traveler, a structural engineer by training, spent three hours attempting to explain what was different about Canadian buildings before concluding that the difference was not in the buildings but in the geometry they occupied.

The most persistent and most difficult-to-evaluate claim comes from a traveler who arrived in GLMZ in 2211 and submitted to extensive debriefing. He stated, with calm certainty, that the stars were different on the Canadian side. Not metaphorically. Not poetically. Different. Constellations in slightly wrong positions. Stars that should have been visible that were not. Stars that were visible that should not have been. He was an amateur astronomer before he crossed and had brought star charts. His charts, when examined, were accurate for the northern hemisphere as observed from the GLMZ. The positions he described from the Canadian side did not match. The discrepancies were small — fractions of a degree — but consistent across multiple observations over several months.

The Diplomatic Reconnaissance Office maintains a file on the Canadian border that is classified "inconclusive, ongoing." The file is forty-seven years old. It has never been reclassified because no one has ever gathered enough evidence to conclude anything. The border remains a gradient, the territory beyond it remains a near-reflection of the familiar, and the stars above it remain, by one account, fractionally wrong.`,
    related_entities: ["Canada", "GLMZ", "GLMZ"],
    story_hooks: [
      "A Canadian corporate entity makes its first known communication with a GLMZ corponation — a single data packet containing architectural blueprints for a building that already exists in GLMZ, built decades ago",
      "An astronomer in GLMZ confirms the star discrepancy using a telescope aimed across the border, and realizes the difference is increasing"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "canada", "border", "corporate", "perception", "stars"]
  },
  {
    name: "I Came From Somewhere Else",
    document_type: "personal_account",
    author: "Transcribed testimony of the subject known as 'Lena'",
    date: "2214-05-12",
    classification: "suppressed",
    credibility: "unconfirmed",
    description: `She arrived at the GLMZ eastern processing center on a Tuesday morning in April 2214 with no identification, no BCI history, and no biometric match in any GLMZ database. This is not, by itself, unusual — the processing center handles undocumented arrivals on a weekly basis. What was unusual was her composure. Undocumented arrivals are typically distressed, evasive, or both. She was neither. She sat in the intake chair with her hands folded and answered every question with the direct, unhurried manner of someone who has nothing to hide and is mildly puzzled that you would think otherwise.

She said her name was Lena. She said she had come from a city called Vassenholm. She described Vassenholm in precise detail: a city of approximately 1.2 million people situated on the banks of a river called the Skelde, in a temperate region with cold winters and mild summers. She described districts — the Kopermark, the Onderveld, the Lighthouse Quarter. She described landmarks — a clock tower called the Horenvaal that chimed in a distinctive twelve-tone sequence, a public garden built on the ruins of a cathedral, a bridge called the Stelweg that connected the city's two halves across the Skelde. She drew a map from memory. The map was detailed, internally consistent, and geographically plausible. It depicted a city that does not exist.

GLMZ geographers conducted an exhaustive search. No river named the Skelde appears in any hydrological database, historical or contemporary. No city named Vassenholm appears in any cartographic record, any census, any historical document. The names she used — Kopermark, Onderveld, Horenvaal — have no etymological matches in any language currently spoken or historically documented. The geography she described — a river valley in a temperate zone, with specific topographical features she sketched from memory — matches no known location on any continent. She was not describing a city that was destroyed, renamed, or relocated. She was describing a city that, by every available measure, never was.

She has a scar on her left abdomen, consistent with a surgical procedure. She named the hospital where the surgery was performed: Vassenholm General, Ward 12, under the care of a Dr. Pieters. A GLMZ physician examined the scar and the underlying tissue. The surgical technique, the physician reported, is consistent with nothing currently practiced in the GLMZ. The incision pattern, the suturing method, and the apparent post-operative recovery all suggest a medical tradition that is competent, advanced, and entirely unfamiliar. The physician could not identify the procedure that was performed. The scar is real. The surgery happened. It happened somewhere.

Lena remains in GLMZ. She has been assigned temporary residency status and a provisional BCI, which she wears without complaint but which she regards with the mild confusion of someone encountering an unfamiliar custom. She does not claim to understand why Vassenholm cannot be found. She does not argue. She has said, on multiple occasions, that she would like to go home, and that she does not understand why no one can tell her how to get there. Her file remains open. Her origin remains classified as "indeterminate." The city she describes with such certainty and such detail continues, stubbornly, to not exist.`,
    related_entities: ["GLMZ", "GLMZ"],
    story_hooks: [
      "A second person arrives in GLMZ claiming to be from Vassenholm — but their description of the city matches Lena's in every detail except the river, which they say flows in the opposite direction",
      "Lena's provisional BCI begins receiving data packets in a language that GLMZ linguistic databases cannot parse, originating from coordinates that correspond to open farmland"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "displacement", "identity", "unreality", "suppressed"]
  },
  {
    name: "The Indiana Dust",
    document_type: "field_report",
    author: "GLMZ Environmental Hazard Assessment Bureau",
    date: "2214-10-07",
    classification: "public",
    credibility: "disputed",
    description: `Southern Indiana is a dust zone. The transformation was gradual — decades of agricultural exhaustion, aquifer depletion, and topsoil erosion that accelerated beyond recovery in the late 2100s. By 2170, the region south of Indianapolis had lost its capacity to sustain vegetation. The topsoil, once among the richest in the former United States, was gone — carried away by wind, washed away by rain that fell on ground too depleted to absorb it. What remains is a fine, persistent particulate that fills the air to a density that reduces visibility to single-digit meters on calm days and zero on days when the wind moves. The dust is not toxic in acute exposure. It is simply omnipresent. It fills lungs, coats surfaces, infiltrates sealed enclosures. It is the dominant feature of the landscape in a region where landscape has been replaced by its absence.

The Iowan Behemoths traverse the Indiana dust zone on routes that appear purposeful but have never been decoded. Their massive forms are visible as shadows when the particulate density allows — vast silhouettes moving through the murk with the slow certainty of geological processes. They do not appear affected by the dust. Their surfaces accumulate it and shed it in patterns that dust-zone observers have described as "breathing." The Behemoths do not stop in the dust zone. They do not accelerate. They move through it as though it were no different from any other terrain, which may be the most unsettling observation, because for the Behemoths, it may not be.

Travelers who emerge from the dust zone — and they do emerge, perhaps half a dozen per year, coated and coughing and blinking in the comparatively clear air of GLMZ's southern perimeter — carry stories of a settlement inside the dust. The settlement has no verified name, no verified location, and no verified existence. It is described consistently as a place you can only find if you are lost. Navigating toward it, by compass or GPS or dead reckoning, will never bring you there. But if you are wandering without direction, without destination, without hope of finding anything at all, you may stumble upon it. The settlement supposedly appears as a cluster of low structures visible at the extreme range of the dust-zone visibility — shapes that resolve into buildings as you approach, buildings that are occupied, lit, and welcoming in a way that nothing in a dust zone should be.

The settlement, according to those who claim to have found it, trades in things that should not exist. Pre-collapse technology — devices from the 2050s and 2060s that are functional, clean, and apparently unaged. Food that grows in conditions of zero topsoil and near-zero light — vegetables with no dust contamination, grains that taste of a world that ended a century ago. Water that is not merely filtered or purified but genuinely clean — tested by travelers with portable kits, it shows no trace of the industrial and agricultural contaminants that are present in every water source in the GLMZ. The water tastes, one traveler said, like it came from a world without pollution. Not treated. Not cleaned. Original. As though it had never been contaminated in the first place.

The Environmental Hazard Assessment Bureau classifies the Indiana dust zone as a "Level 3 hazardous environment — sustained exposure risk." The classification makes no mention of settlements, trade goods, or water that shouldn't exist. The Bureau's position is that the dust zone contains dust and nothing else. This position is maintained despite the consistent testimony of travelers, the physical samples they occasionally bring — a tomato still fresh after a week in a pocket, a glass of water sealed in a jar that tests clean six months later — and the quiet, persistent rumor among GLMZ's southern communities that there is something in the dust that the dust is hiding.`,
    related_entities: ["Indiana", "GLMZ", "GLMZ", "Iowan Behemoths"],
    story_hooks: [
      "A player character becomes hopelessly lost in the Indiana dust zone and finds the settlement — where they're offered a trade: something they desperately need, in exchange for something they didn't know they had",
      "Pre-collapse technology from the dust settlement appears in GLMZ's markets, and a corponation wants to know where it came from badly enough to fund an expedition into the dust"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "indiana", "dust", "behemoths", "settlement", "impossible_trade"]
  },
  {
    name: "The Lake Huron Signal",
    document_type: "investigation",
    author: "GLMZ Signal Intelligence Division, Special Investigations Unit",
    date: "2215-03-28",
    classification: "restricted",
    credibility: "verified",
    description: `The signal was first detected on March 15, 2204, by a routine monitoring sweep of the Lake Huron frequency spectrum. It broadcasts on 4,625 kHz — a frequency in the maritime medium-frequency band that has been allocated, since the pre-collapse era, to emergency communications. The signal is analog. It is a human voice. The voice reads names.

The voice is female, mid-register, with no identifiable accent. It reads each name clearly, with a pause of approximately four seconds between names. The reading is continuous — it does not stop for periods of silence, does not loop, does not repeat. New names are added at a rate that Signal Intelligence has determined corresponds, with unsettling precision, to the mortality rate of GLMZ's registered population. Specifically: the names read by the voice are the names of people in GLMZ who died within the previous 72 hours. The correspondence has been verified across 4,017 consecutive days of monitoring. The error rate is zero. Every name matches. No names are missing. No names are wrong. The signal knows who died, and it knows within three days.

The signal originates from coordinates 44.1°N, 82.7°W — a point in the central basin of Lake Huron, approximately 120 kilometers from the nearest shore. Triangulation from multiple receiving stations has confirmed these coordinates repeatedly. The signal strength is consistent with a stationary transmitter of moderate power. There is, however, nothing at those coordinates. Surface survey by boat has found open water. Sonar mapping has found lake bottom at a depth of approximately 60 meters, with no structures, no objects, and no anomalies of any kind. The water column is empty. The sediment is undisturbed. The signal broadcasts from a point in space that contains nothing capable of broadcasting.

The Signal Intelligence Division has maintained continuous monitoring of the Lake Huron Signal for eleven years. In that time, the signal has not deviated from its pattern. It has not ceased, even briefly. It has not changed frequency, modulation, or broadcast power. The voice has not changed in timbre, pace, or inflection. It reads names of the dead with the steady, uninflected cadence of someone performing a task that is routine but not careless — a task done with attention but without emotion. Analysts who have listened to extended recordings describe the experience as "not disturbing, exactly, but permanent" — a sound that, once heard, occupies a space in memory that it does not vacate.

Seventeen hypotheses have been formally proposed to explain the Lake Huron Signal. They range from the technical (a previously unknown natural phenomenon that coincidentally produces human-sounding audio matching real names) to the conspiratorial (a GLMZ intelligence operation designed for purposes unknown) to the frankly metaphysical. None have survived rigorous analysis. The signal remains unexplained, uninterrupted, and accurate. It is, as far as the Division can determine, exactly what it appears to be: a voice in the lake, reading the names of the dead, and it has not been wrong yet.`,
    related_entities: ["Lake Huron", "GLMZ", "GLMZ"],
    story_hooks: [
      "The signal reads a name that belongs to someone who is still alive — and that person dies exactly 72 hours later, suggesting the signal isn't reporting deaths but announcing them",
      "A deep-dive expedition to the signal coordinates discovers that the lake bottom at that point is not sediment but a flat, artificial surface of unknown material"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "lake_huron", "signal", "death", "broadcast", "inexplicable"]
  },
  {
    name: "Travelers' Descriptions Don't Match",
    document_type: "academic_paper",
    author: "Dr. Tomasz Nwosu, GLMZ Cartographic Anomalies Research Group",
    date: "2215-04-03",
    classification: "public",
    credibility: "verified",
    description: `This paper presents the findings of a comparative analysis of 47 independent traveler accounts describing the territory between GLMZ and the nearest verified city-state to the east — a stretch of approximately 380 kilometers that is traversed weekly by the Ohio Corridor convoys and that constitutes, in theory, the most frequently observed external territory accessible from the GLMZ. The accounts were collected over a period of eight years from travelers who arrived in GLMZ from the east by routes other than the official convoy corridor. Each traveler was interviewed independently, without access to other accounts, using standardized geographic description protocols. The resulting dataset should, by any reasonable expectation, describe a single stretch of territory from 47 different perspectives. It does not.

The descriptions are irreconcilable. Not inconsistent in the manner of witnesses describing the same event from different angles — fundamentally, categorically different. Traveler 7 describes the territory as dense deciduous forest extending unbroken for the entire distance, with no clearings, no structures, and no evidence of human presence past or present. Traveler 12, who traversed what should have been the same territory three weeks later, describes open grassland with scattered ruins of pre-collapse suburban development. Traveler 23 describes a desert — arid, flat, featureless, and hot, in a climate zone that has no meteorological basis for desert conditions. Traveler 31 describes a city. Not ruins. A city. Inhabited, lit, functioning, occupying an area of approximately 40 square kilometers in a location where no city has ever been recorded. Traveler 31 spent two days in this city. She describes markets, residential districts, a transit system. She interacted with residents who spoke English with an accent she could not place. Travelers 30 and 32, who traversed the same coordinates within days of Traveler 31, describe empty woodland.

The analysis in Sections 3 through 7 of this paper applies rigorous statistical methods to determine whether these discrepancies can be explained by conventional factors: fabrication, misidentification of location, perceptual distortion, or simple error. The conclusion, which the author acknowledges is extraordinary, is that they cannot. The internal consistency of each account, the verifiable physiological data (dust exposure in the desert account, pollen samples in the forest accounts, urban particulate in the city account), and the BCI data where available all support the conclusion that each traveler is accurately describing what they experienced. The experiences are simply incompatible with the existence of a single, stable territory.

The paper proposes, with appropriate caveats, that the territory between GLMZ city-states may not be fixed. This is not a metaphor. The data suggests that the landscape occupying the space between cities is variable — that the same coordinates, traversed at different times by different people, contain different terrain, different ecologies, different histories of human occupation or its absence. If this hypothesis is correct, the Ohio Corridor convoy's consistent experience of the same route may be an artifact of the route itself — a maintained pathway that stabilizes the territory it passes through, in the same way that a path through tall grass defines its own existence by the act of walking.

The implications of this hypothesis, if validated, are beyond the scope of this paper and possibly beyond the scope of any single academic discipline. If the outside is not a place but a variable — if geography itself is unstable beyond the boundaries of maintained human settlement — then the maps are not incomplete. They are impossible. And the Storytellers who arrive in GLMZ with contradictory accounts of the same regions may not be unreliable witnesses. They may be the most reliable witnesses available, each reporting accurately on a reality that was real only for them.`,
    related_entities: ["GLMZ", "GLMZ", "Ohio Corridor"],
    story_hooks: [
      "The Cartographic Anomalies Research Group receives funding to test the hypothesis by sending two travelers along the same route simultaneously — they arrive at different destinations",
      "A corponation attempts to exploit the variable territory by sending teams to search for the city Traveler 31 described, hoping to establish trade — the teams keep finding different cities"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "cartography", "variable_territory", "academic", "spatial_anomaly"]
  },
  {
    name: "The Convoy Driver's Log",
    document_type: "personal_account",
    author: "Nneka Johansson-Bello, Ohio Corridor Convoy Driver, License C-4419",
    date: "2215-05-01",
    classification: "restricted",
    credibility: "verified",
    description: `Week 1 through Week 8: Normal. The route is 380 kilometers of cleared road through what used to be Ohio. I've been driving it for two years. The road is straight where it can be, curved where the terrain demands it, and maintained by automated systems that keep the surface smooth and the edges defined. The drive takes approximately five hours at convoy speed. The land on either side is trees and grass and whatever else grows in Ohio now. I don't look at it more than I have to. The briefing says not to look at it. I look at it anyway, because eight hours in a cab with nothing but forward is its own kind of problem. It looks like land. It looks fine. Weeks 1 through 8 were fine.

Week 9: The drive took five hours and forty minutes. I logged it. Dispatch said GPS calibration. I said nothing because GPS calibration is what dispatch always says. But the road felt longer. Not in a vague, subjective sense. I know this road. I know where the curves are, where the straightaways are, where the old exit signs are that haven't been removed because nobody maintains anything that isn't the road surface. The exit sign for what used to be Mansfield was in the wrong place. It was approximately three kilometers farther east than it should have been. I noted this. Nobody else in the convoy mentioned it. I checked with my co-driver. She said the sign was where it always was. I did not argue.

Week 14: The landmarks are wrong and I have stopped reporting it because the responses I get are variations of "GPS calibration" and "fatigue-related perceptual shift," and I am neither miscalibrated nor fatigued. The bridge over the Muskingum River — a fixed, physical structure of steel and concrete — was 200 meters longer this week than last week. I measured it by time at constant speed. My co-driver measured it independently and got the same result. We reported it jointly. Dispatch thanked us for our diligence and noted that bridge length measurements from a moving vehicle are inherently imprecise. We are not imprecise. The bridge is longer. Next week I will measure it again.

Week 19: A bridge appeared that was not there last week. It spans a river that was not there last week. The river is approximately 30 meters wide, flowing north to south, crossing the corridor at a point between the 240 and 250 kilometer markers. There is no river at this location on any map. The bridge is steel, double-lane, structurally sound. I drove across it because the road led to it and the convoy was behind me and stopping is not permitted. The bridge held. The river below was clear and moving. On the far side, the road continued as normal. I reported the bridge. Dispatch said they would send a survey team. The survey team found no bridge and no river at the coordinates I provided. I drove the same route the following week. No bridge. No river. The road was continuous. The asphalt where the bridge approaches had been showed no seams, no patches, no evidence that anything had ever interrupted it.

Week 23 through Week 26: I am keeping this log because the official reports do not reflect what is happening and I want a record. The road is changing. Not all at once and not dramatically, but consistently. Each week, something is different. A hill that wasn't there. A curve that's new. Trees on the left side that are a different species than the week before. The transit time varies between four hours forty minutes and seven hours twelve minutes with no explanation. My co-driver sees some of what I see and not all of it. The things she sees that I don't, she doesn't tell me about anymore, and I extend her the same courtesy. We drive the corridor. The corridor is reliable. The corridor takes us where we need to go. What the corridor is — whether it is a road through stable territory or a path that creates its own stability as we drive it — is a question I have stopped asking because the answer, whatever it is, will not change the fact that the convoy runs weekly and I am the one driving it.`,
    related_entities: ["Ohio Corridor", "GLMZ", "GLMZ", "GLMZ Transit Authority"],
    story_hooks: [
      "Nneka's log is leaked to the public, and a group of independent investigators attempt to walk the corridor on foot — they find that the changes happen faster when you move slowly",
      "The convoy arrives one week with an extra vehicle in the formation that no one remembers joining the convoy but that carries legitimate cargo with valid manifests"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "ohio", "corridor", "convoy", "spatial_anomaly", "personal_account"]
  },
  {
    name: "Why We Don't Leave",
    document_type: "academic_paper",
    author: "Dr. Adaeze Volkov-Tanaka, Department of Social Psychology, GLMZ University",
    date: "2215-07-15",
    classification: "public",
    credibility: "verified",
    description: `The question is not why people stay in GLMZ. The question is why the question keeps being asked, as though staying required an explanation and leaving were the default. Leaving is not the default. Leaving has not been the default for anyone, anywhere, at any point in human history. People stay. They stay in flood zones, in war zones, in places where the soil is poisoned and the water burns. They stay because the known, however terrible, has a structure that the unknown does not. They stay because leaving requires a destination, and the outside does not offer destinations. It offers variables.

This paper examines the psychology of residence persistence in GLMZ through a survey of 2,400 adult residents across twelve economic strata and fourteen district zones. The findings are consistent across every demographic boundary tested. Residents are aware that conditions in GLMZ are, by most objective measures, poor. Corponation governance is extractive. Economic mobility is negligible. The Behemoths roam the wasteland beyond the perimeter. The infrastructure decays faster than it is repaired. Violence is common. Privacy is theoretical. The BCI that connects every resident to the network is also the mechanism by which every resident is surveilled, profiled, and commodified. Residents know this. They describe it in interviews with the weary precision of people reciting facts about the weather. It is the condition in which they live. It is terrible and it is predictable, and predictability, this paper argues, is the operative variable.

The outside is not predictable. The outside is, by every available account, unstable. Missouri is underwater. Kentucky is gone. The Ohio Corridor changes week to week. The Wisconsin Quiet Zone expands. The storytellers who arrive from beyond describe a world that cannot be mapped because it does not hold still long enough to be mapped. The territory between cities may be variable. The roads may lead to places that were not there yesterday. The people who leave GLMZ either come back different — quieter, vague about where they went, reluctant to discuss what they saw — or they do not come back at all. The return rate for voluntary departures from GLMZ, based on exit and re-entry records, is 34%. Of the 34% who return, approximately half request psychological support within six months. Of those, a significant minority describe symptoms consistent with having experienced something they cannot integrate into their existing model of reality.

Staying is not hope. This paper rejects the interpretation, common in popular discourse, that GLMZ's residents stay because they hope things will improve. The survey data does not support this. When asked whether they expect conditions in GLMZ to improve in their lifetime, 7% of respondents said yes. When asked whether they intend to leave, 4% said yes. The gap between those numbers is the finding. Even among the 93% who do not expect improvement, 96% intend to stay. Staying is not hope. Staying is the rational assessment that the known horror is preferable to the unknown one. The devil you know is not a comfort. The devil you know is a map. Outside, there are no maps.

The paper concludes with a observation that the author acknowledges is more philosophical than empirical. The residents of GLMZ are not trapped. The perimeter is porous. The convoys run weekly. People can leave. The fact that they do not leave — that they choose, consistently and overwhelmingly, to remain in a city that is exploitative, surveilled, and deteriorating — suggests that the outside is not merely unknown but genuinely, fundamentally different from the inside in a way that the human psyche cannot comfortably approach. The outside is not a place you haven't been. It is a place that might not be a place at all. And staying here, in the terrible, predictable, mappable city, is the sanest response to that possibility.`,
    related_entities: ["GLMZ", "GLMZ", "Ohio Corridor", "Wisconsin Quiet Zone", "Kentucky", "Missouri"],
    story_hooks: [
      "A mass departure event — hundreds of residents leaving simultaneously toward the south — overwhelms perimeter security, and the subsequent investigation reveals they all received the same anonymous message",
      "Dr. Volkov-Tanaka's follow-up study tracks the 4% who said they intended to leave, and discovers that none of them actually did — including two who have no memory of ever expressing the intention"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "psychology", "meridian_88", "residence", "academic", "fear"]
  },
  {
    name: "The Last Train from Detroit",
    document_type: "oral_history",
    author: "Collected by the GLMZ Oral History Archive",
    date: "2214-09-18",
    classification: "public",
    credibility: "verified",
    description: `The passenger rail service between GLMZ and Detroit ran for forty-one years, from 2170 to 2211. It was not glamorous. It was a single line, two trains, running a daily round trip on track that had been rebuilt from pre-collapse rail infrastructure by a consortium of GLMZ transit interests who believed, correctly for a time, that overland rail between the two city-states was commercially viable. The distance was manageable — approximately 420 kilometers. The terrain was flat. The track was straight. The trains were utilitarian and reliable. For four decades, the Meridian-Detroit line was the most normal thing about the Great Lakes region. It ran on time. It carried passengers and light freight. It connected two cities in a world where connection was increasingly rare.

The problems began in 2208. They were subtle at first. The transit time, which had been consistent at approximately three hours and forty minutes for decades, began to vary. Some trips were ten minutes short. Some were twenty minutes long. The variation was attributed to track conditions, weather, and the general degradation of infrastructure that affects everything in the GLMZ. But the variation increased. By 2209, the transit time on certain runs exceeded five hours. The train's speed had not changed. The track had not been extended. The distance between the two cities was, according to every survey and satellite measurement, exactly what it had always been. The train was simply taking longer to cross it.

In 2210, the track began to curve where it should not have curved. Maintenance crews dispatched to inspect the line reported that the rails, which they had surveyed as straight, were bending — gently, almost imperceptibly, but consistently — to the south. The curves were not the result of ground subsidence or thermal expansion. The rails were curving in their mounts, the steel bending in a direction that metallurgists said was impossible without application of force that would have left visible deformation marks. There were no deformation marks. The steel was smooth. It was simply no longer straight. Crews re-aligned the sections. The curves returned within weeks.

On August 3, 2211, the westbound train from Detroit to GLMZ departed on schedule and arrived at a station that was not Detroit and was not GLMZ. The train followed the track. The track led to a station. The station was physically real — a platform, a roof, signage in a language that the conductor could not read, and beyond the platform, a city that no passenger recognized. The city was inhabited. Figures were visible on the streets. The architecture was unfamiliar. The passengers, by unanimous and unprompted decision, refused to disembark. The conductor reversed the train. The return journey took four hours longer than it should have. The train arrived in GLMZ at 11:47 PM, seven hours and twelve minutes after departing what should have been a three-hour-and-forty-minute run in the opposite direction. Every passenger was accounted for. Every passenger was shaken. Nobody could agree on what the station had looked like, except that it was not where they were supposed to be.

The service was not rescheduled. The consortium did not issue a statement. The trains were decommissioned and the track was officially deactivated, though deactivation in practice meant simply not running trains on it anymore. The track remains. Aerial surveys show it stretching eastward from GLMZ, straight for a while, and then curving — still curving, fourteen years later, bending southward toward a destination that the rails appear to remember even if no one else does.`,
    related_entities: ["Detroit", "GLMZ", "GLMZ"],
    story_hooks: [
      "Someone reactivates the rail line and sends an unmanned, instrumented train along the track — it arrives at the unknown station, and the instruments record everything, but the data contradicts itself",
      "A passenger from the final run recognizes the unknown station's architecture in a drawing found in the personal effects of a recently deceased GLMZ resident who never left the city"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "detroit", "rail", "transit", "spatial_anomaly", "oral_history"]
  },
  {
    name: "Postcards from Nowhere",
    document_type: "investigation",
    author: "GLMZ Postal Anomalies Division",
    date: "2215-08-22",
    classification: "restricted",
    credibility: "verified",
    description: `Over the past decade, 1,247 postcards have appeared in GLMZ postal boxes. They were not delivered by the postal system. The GLMZ Postal Service maintains comprehensive tracking of all physical mail — a category of communication so rare in the 2200s that each item is logged, scanned, and tracked from acceptance to delivery. These postcards do not appear in the tracking system. They have no acceptance records. They were not processed by any sorting facility. They were not carried by any postal worker. They are simply found in boxes — residential boxes, commercial boxes, decommissioned boxes that haven't received legitimate mail in years. They appear without pattern and without explanation, and they have been appearing at a rate of approximately ten per month since 2205.

The postcards are physical objects of high quality. The card stock is heavy, cream-colored, with a slight texture that paper analysts have identified as consistent with cotton fiber production methods that were common in the early 21st century and that have not been in use for over a hundred years. The images on the front of each card are printed in full color using a process that analysts have been unable to identify — not inkjet, not laser, not lithographic, not any digital or analog printing method currently catalogued. The images are sharp, vivid, and depict places that do not exist. Cities with architecture that belongs to no known tradition. Landscapes that combine geological features that do not naturally coexist. Landmarks that are photographically convincing and geographically impossible — a lighthouse on a mountain peak, a cathedral built across a river, a bridge connecting two cliffs over an ocean where no ocean is.

The postmarks on each card are legible and specific. They identify postal systems that do not operate — "Royal Mail of the Cascadian Protectorate," "Unified Postal Service of the Inland Republic," "Free City of Vassenholm Department of Letters." The stamps are detailed, depicting leaders, symbols, and denominations of nations that never were. Each stamp is different. Each postmark is different. The cancellation dates vary but fall within the previous twelve months of each card's appearance. The production quality of the stamps and postmarks is indistinguishable from genuine postal artifacts, except that the entities they represent have no historical or contemporary existence.

The handwriting on each card is different. This has been verified by graphological analysis. No two cards share an author. Given 1,247 cards, this means 1,247 different people wrote them, assuming each person wrote only one. The handwriting styles vary in character, precision, and cultural origin — some are angular, some are rounded, some use letter forms that are archaic, and a few use letter forms that graphologists cannot classify. The scripts are all legible. The language is always English. And the message on every card, regardless of the handwriting, regardless of the fictional origin, regardless of the impossible image on the front, is the same five words: "Wish you were here."

The Postal Anomalies Division has been unable to determine how the postcards enter the postal boxes. Surveillance of boxes that have previously received cards has captured no anomalous activity. The cards are not there, and then they are, with no intermediate state visible on any recording. The Division's official report, submitted annually to the GLMZ Administrative Council, describes the postcards as "an ongoing anomaly of unknown origin, unknown mechanism, and unknown intent." The report recommends continued monitoring. The postcards continue to arrive. The places they depict continue to not exist. The message continues to be the same. Wish you were here. From somewhere that isn't. Written by someone you'll never meet. Delivered by a method that cannot be observed. Addressed, somehow, to you.`,
    related_entities: ["GLMZ", "GLMZ", "Vassenholm"],
    story_hooks: [
      "A postcard arrives depicting GLMZ itself — but the version shown is subtly different, more prosperous, with buildings that don't exist yet, and the postmark reads 'Free City of Meridian'",
      "Someone begins collecting the postcards and realizes that when arranged in a specific order, the images on the fronts form a continuous panoramic view of a single, impossible city"
    ],
    tags: ["document", "outside", "glmz", "anomaly", "new_weird", "geography", "postcards", "postal", "unreality", "messages", "vassenholm"]
  }
];

// Generate and write
let written = 0;
let skipped = 0;

for (const doc of documents) {
  const id = genId();
  const full = {
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
  if (writeDoc(full)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}, Total: ${documents.length}`);
