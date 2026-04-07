const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const DOCUMENTS_DIR = path.join(__dirname, '..', 'engine', 'data', 'documents');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name, max = 80) {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, max);
}

function writeIfNotExists(dir, name, data) {
  const slug = slugify(name.slice(0, 60));
  const filePath = path.join(dir, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`  SKIP (exists): ${slug}.json`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf8');
  console.log(`  CREATED: ${slug}.json`);
  return true;
}

const documents = [
  // ============================================================
  // BIOLOGICAL IMPOSSIBILITIES (1-10)
  // ============================================================
  {
    name: "The Globsters",
    document_type: "incident_report",
    author: "Meridian 88 Waterfront Hazmat Division, Team Lead Kofi Asante-Yamamoto",
    date: "2213-09-04",
    classification: "restricted",
    description: `The first one came ashore at Pier 19 in the lower Shelf during the September tides. Approximately 2,400 kilograms of biological material, pale and fibrous, smelling faintly of ozone and copper. Waterfront cleanup assumed it was a decomposed lake organism — sturgeon, possibly, or a colony of invasive jellyfish compacted by current. They tagged it for removal. By morning there were three more.

DNA sequencing returned results that the lab initially attributed to contamination. The samples contained base pairs that do not appear in any terrestrial organism. Not novel combinations of known nucleotides — novel nucleotides entirely. Adenine, guanine, cytosine, thymine, and two additional bases that the sequencer flagged as errors until they appeared in every sample from every mass. The lab director, Dr. Yuki Okonkwo-Chen, ran the analysis fourteen times. She has formally requested that her equipment be inspected for malfunction. It has been inspected. It is not malfunctioning.

The masses continue to wash ashore at a rate of roughly one per week. They do not decompose at any measurable rate. They are not alive by any standard biological definition, but tissue samples placed in growth medium exhibit coordinated cellular behavior that resembles, but is not, mitosis. The cells divide into structures that serve no identifiable biological function. The masses are warm to the touch regardless of ambient temperature. Their internal temperature is a constant 33.7 degrees Celsius.

We have seventeen of them now, stored in a repurposed cold dock on the lower waterfront. They are growing. Not individually — we measure them daily and they remain the same size. But their combined mass has increased by 11% since collection began. The scale is calibrated. The dock is sealed. Nothing is being added.`,
    related_entities: ["The Shelf", "Meridian 88", "Lake Michigan"],
    credibility: "verified",
    story_hooks: [
      "What organism produces nucleotides unknown to terrestrial biology?",
      "Why is the combined mass increasing in a sealed environment?",
      "What happens when the globsters reach critical mass?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "shelf", "waterfront", "lake_michigan"]
  },
  {
    name: "The Turritopsis Colony",
    document_type: "investigation",
    author: "GLMZ Marine Biology Research Collective",
    date: "2211-06-17",
    classification: "restricted",
    description: `The colony occupies the submerged foundation of a pre-flood parking structure in the lower Underworld, approximately fourteen meters below the current waterline. It resembles coral in the way a photograph of a person resembles the person — structurally analogous, fundamentally different. The organism is colonial, sessile, and bioluminescent. It has been growing on the concrete since before the structure was submerged, based on the growth patterns relative to the waterline history. This is not possible, because the structure was submerged in 2147 and the colony's basal layer has been carbon-dated to approximately 2080. It was growing on a parking garage that was, at the time, dry and above ground.

When threatened — and we use the word loosely, as our interactions have included physical sampling, chemical exposure, and acoustic disruption — the colony does not die, retreat, or defend. It reverses. Individual polyps undergo a process visually identical to transdifferentiation, reverting through what appear to be earlier developmental stages until they reach a form resembling a planula larva. The larvae then re-settle and begin growing again. The entire cycle takes approximately seventy-two hours. We have triggered it four times. Each time, the colony returns to a state indistinguishable from its pre-disturbance form.

Samples removed from the colony die within hours. No exception. The organism cannot survive separation from the main body. In situ, it appears to be effectively immortal. We have found no upper limit to its reversal capacity. The basal layer — the oldest part of the colony — shows no senescence markers. It is, by every measure we can apply, the same age it was when we first sampled it six years ago.

Dr. Amara Johansson-Diallo has proposed that the organism does not experience time in a linear fashion. This is not a scientific statement. She knows this. She put it in the report anyway.`,
    related_entities: ["Underworld", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "How was the colony growing on a dry structure decades before flooding?",
      "What happens if the colony is allowed to reverse without limit?",
      "Is the organism's relationship with time a model for something larger?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "underworld", "immortal_organism", "temporal"]
  },
  {
    name: "The Wood Wide Web Anomaly",
    document_type: "field_report",
    author: "GLMZ Perimeter Ecology Survey, Field Team 7",
    date: "2209-11-02",
    classification: "classified",
    description: `The preserved forest inside the eastern perimeter — designated Sector E-14 by survey cartography — exhibits coordinated biological behavior that we cannot attribute to any known mechanism. On October 9th, at approximately 14:30, every tree in the 2.3-square-kilometer survey area simultaneously dropped its leaves. Not seasonally. Not in response to temperature, wind, light, or moisture changes. The ambient conditions were stable. The trees dropped their leaves in unison, as if responding to a signal.

The mycorrhizal network underlying the forest has been mapped extensively. It is dense, interconnected, and unremarkable. It does not explain what happened. Fungal signaling operates on timescales of hours to days and produces gradual, cascading responses. This was instantaneous across the entire sector. Every tree. At the same moment. Leaves hit the ground within a four-second window across 2.3 square kilometers.

More troubling: the behavior was not defensive. Leaf drop is typically a stress response — drought, frost, pathogen. The trees were healthy. Soil moisture was optimal. There were no pathogens, no pest pressure, no chemical contaminants. The trees dropped their leaves and then, over the following six days, grew new ones. The new leaves are identical to the old ones in every measurable way. We have tested for chemical markers, isotope ratios, and cellular structure. They are the same leaves. Not similar. The same.

Field Team 7 has documented fourteen coordinated events in Sector E-14 over the past three years. Simultaneous flowering out of season. Synchronized sap flow reversal. A twenty-four-hour period during which every organism in the sector — trees, undergrowth, fungi, insects — ceased all metabolic activity and then resumed as if nothing had happened. The forest is doing something. It is not reacting. It is acting. We do not know what it is doing or why.`,
    related_entities: ["GLMZ Perimeter", "Sector E-14"],
    credibility: "verified",
    story_hooks: [
      "What signal triggers the coordinated response if not the mycorrhizal network?",
      "Is the forest a single organism pretending to be many?",
      "What is the purpose of synchronized metabolic cessation?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "wilderness", "coordinated_behavior", "forest"]
  },
  {
    name: "The Overtoun Walkway",
    document_type: "incident_report",
    author: "Meridian 88 Animal Control Division, Senior Officer Lena Tsukuda-Obi",
    date: "2214-01-19",
    classification: "restricted",
    description: `The elevated walkway connecting Residential Block 12 to the Commercial Tier in the Laceworks has been killing animals for seventy years. The documentation is unambiguous. Municipal records show the first reported incident in 2144 — a domestic cat leapt from the walkway railing and fell nine stories. Since then, the Animal Control Division has logged 847 animal deaths at this location. Dogs, cats, birds that land on the railing and then walk off it. The behavior is consistent: the animal approaches the railing at a specific section — a 15-meter stretch on the east side, between structural supports 7 and 9 — and jumps.

The section has been modified twelve times. Railings raised, enclosed, fitted with mesh, replaced with solid barriers. Animals climb the barriers. A dog was documented scaling a 2-meter solid acrylic wall to reach the top and jump. The walkway has been closed to animal traffic three times. Animals that are carried past the section in closed containers show no distress. Animals that walk through the section on leash pull toward the railing. Not all animals. Approximately 40% of dogs and 60% of cats that traverse the section exhibit the behavior. No human has ever reported an urge to jump.

We installed full-spectrum monitoring equipment along the 15-meter stretch in 2208. Electromagnetic, acoustic, chemical, thermal, barometric. Six years of continuous data. There is nothing anomalous about the section. The air is the same. The light is the same. The sound is the same. The materials are standard municipal construction, replaced twice in the monitoring period. The phenomenon persists across every material, every configuration, every modification. It is not the walkway. It is the location.

I have submitted eleven formal requests for the walkway section to be demolished entirely. Each has been denied on the grounds that no causal mechanism has been identified and demolition would not constitute a evidence-based intervention. In the time it took to process my eleventh request, four more animals died.`,
    related_entities: ["Laceworks", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What is it about this specific 15-meter section that compels animals?",
      "Why are humans immune to the effect?",
      "Would demolition actually stop the phenomenon, or would it manifest at the same coordinates regardless of structure?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "laceworks", "animal_behavior", "location_based"]
  },
  {
    name: "Homing Behavior in the Displaced",
    document_type: "investigation",
    author: "GLMZ Social Services Behavioral Analytics Division",
    date: "2212-08-30",
    classification: "suppressed",
    description: `Block 9 of the former Eastshore District was demolished in 2207 to make way for the Lakewall expansion. 1,400 residents were relocated to temporary housing in the upper Shelf, then permanent units in the Laceworks. Standard displacement protocol. The site is now a concrete foundation slab beneath four meters of reinforced flood barrier. There is nothing there. There has been nothing there for five years.

Thirty-one former Block 9 residents have been found at the demolition site. Not visiting. Not protesting. Found standing on the exact coordinates of their former residences, oriented in the direction their front doors once faced. Some walked there during the day. Eleven were found during overnight hours, having left their current residences while asleep. GPS tracking data from municipal ankle monitors — four of the thirty-one are on parole — shows movement patterns that are geometrically precise. They do not wander to the site. They walk in straight lines from wherever they are, ignoring streets, cutting through buildings where doors happen to be open, climbing fences. The path is always the shortest possible distance to their specific former address.

The sleepwalkers are the most concerning. Kajsa Nwosu-Andersen, age 67, was found standing barefoot on the Lakewall at 3 AM in December, directly above the coordinates of her former kitchen. She had walked 4.2 kilometers from her Laceworks apartment. She has no history of sleepwalking. She does not remember leaving her apartment. She was wearing nightclothes and had sustained mild frostbite. When asked why she was there, she said, "I live here." She has been relocated twice since the incident. She has been found at the site twice more.

We have quietly expanded our monitoring to include all 1,400 former Block 9 residents. The behavior is not universal but it is not rare. At least 9% exhibit some form of return behavior. The percentage has not decreased over time. If anything, it is increasing.`,
    related_entities: ["Eastshore District", "The Shelf", "Laceworks", "Lakewall"],
    credibility: "suppressed",
    story_hooks: [
      "Is the homing behavior purely psychological or is something at those coordinates pulling them back?",
      "What would happen if the Lakewall were opened at the Block 9 coordinates?",
      "Are other demolished districts showing similar return patterns?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "displacement", "homing", "behavioral"]
  },
  {
    name: "The Reclaimed Zone Flora",
    document_type: "field_report",
    author: "GLMZ Perimeter Aerial Survey Division",
    date: "2210-04-22",
    classification: "classified",
    description: `The wilderness between Meridian 88 and the Milwaukee Sprawl — colloquially the Reclaimed Zone — was first flagged by satellite imaging in 2203. Vegetation patterns visible at altitude do not conform to any natural growth model. They are geometric. Not approximately geometric in the way river deltas or crystal structures suggest mathematical regularity. Geometric in the way a circuit board is geometric. Precise. Intentional. Repeating.

The dominant pattern is a Fibonacci spiral. Not one — hundreds, nested and overlapping, ranging from three meters to 1.2 kilometers in diameter. The spirals are composed of different plant species occupying precise positions within the pattern. Prairie grass forms the background. Wildflowers — species-specific, never mixed — trace the spiral arms. Trees mark the vertices of secondary geometric structures that overlay the spirals: hexagons, pentagons, and shapes that our topology consultant, Dr. Idris Kowalski-Bah, describes as "regular polygons that shouldn't tile a plane but do."

The plants were not placed. Root system analysis confirms natural germination and growth. Soil composition is uniform across the patterned areas — there is no chemical or mineral variation that could template the growth. Seed dispersal modeling cannot account for the precision. Wind, animal activity, and water flow do not produce Fibonacci spirals at kilometer scale. Nothing produces Fibonacci spirals at kilometer scale. The patterns are growing. New spirals appear at the edges of the existing formation at a rate of approximately 40 meters per year, expanding outward from a central point that corresponds to no known landmark, structure, or geological feature.

We have not released the satellite imagery. The patterns are visible from commercial orbital platforms, and it is only a matter of time before someone outside the survey division notices them. I do not know what we will say when they do.`,
    related_entities: ["Reclaimed Zone", "Meridian 88", "Milwaukee Sprawl"],
    credibility: "verified",
    story_hooks: [
      "What is at the central point of the spiral formation?",
      "Are the patterns a message, a structure, or a process?",
      "What happens when the expanding formation reaches the city perimeter?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "wilderness", "geometric", "fibonacci", "satellite"]
  },
  {
    name: "The Cold Fusion Paradox",
    document_type: "investigation",
    author: "Meridian 88 Energy Regulatory Commission, Investigator Dante Johansson-Abara",
    date: "2213-02-14",
    classification: "leaked",
    description: `On January 3rd, 2213, a man named Prosper Achebe-Lindqvist, a basement-level electrical tinkerer with no formal education beyond secondary school, achieved sustained nuclear fusion at room temperature using equipment valued at approximately 400 quanta. His apparatus consists of a modified water heater, a salvaged capacitor bank from a decommissioned transit car, copper wire, and a ceramic containment vessel he made himself from clay sourced from the lakefront. It produces a stable 2.3 kilowatts of excess energy with no radiation, no plasma confinement, and no input fuel beyond tap water. It has been running continuously for forty-one days.

We have documented the apparatus exhaustively. Every component has been cataloged, measured, analyzed, and replicated to exact specification by three independent engineering teams — two corporate, one municipal. None of their replicas produce excess energy. They produce nothing. The components, assembled identically, do not function. Prosper's apparatus, assembled identically, does. We swapped components between his working device and a non-functioning replica. His components, in the replica housing, did not work. The replica components, in his housing, did not work. The original apparatus, reassembled from its own components in its original configuration by Prosper himself, resumed functioning immediately.

The paradox deepened in February when we received reports of three additional basement fusion devices in the lower Shelf and Underworld. All built independently. All by individuals with no engineering credentials and minimal resources. All functional. All irreplicable by credentialed researchers with adequate funding. We sent a team from the Meridian Energy Institute — twelve physicists, budget of 2.4 million quanta — to build a fusion device under controlled laboratory conditions using the best available materials and instrumentation. They produced nothing. Prosper watched their attempt. He said they were trying too hard. He was not being philosophical. He appeared genuinely confused by their failure.

I do not have an explanation. The devices work. The physics does not permit them to work. They work anyway. Funding and expertise appear to be inversely correlated with success. I am filing this report because it is my job to file reports. I do not expect anyone to act on it.`,
    related_entities: ["Meridian 88", "The Shelf", "Underworld"],
    credibility: "disputed",
    story_hooks: [
      "Is intention or belief a variable that physics has not accounted for?",
      "What would happen if the fusion devices were scaled up by their original builders?",
      "Why does competence and funding prevent the effect?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "cold_fusion", "paradox", "inverse_competence"]
  },
  {
    name: "The Mpemba Inversion",
    document_type: "field_report",
    author: "Meridian 88 Municipal Water Authority, Quality Assurance Lab",
    date: "2211-12-03",
    classification: "restricted",
    description: `The Mpemba effect — the observation that hot water can freeze faster than cold water under certain conditions — has been debated for centuries. What we are observing is not the Mpemba effect. What we are observing is water freezing from the inside out.

The first documented case occurred during routine quality testing of a municipal water batch destined for the Laceworks residential supply. The batch — 200 liters, standard treatment, standard chemistry — was placed in cold storage for crystallization analysis. When the technician checked the sample sixteen hours later, the exterior was liquid at 2 degrees Celsius. The interior was a solid block of ice at -4 degrees. This is not how freezing works. Water freezes from the surface inward as heat dissipates from the exterior. This sample had frozen at its core while its shell remained liquid. The ice was structurally normal. The liquid was chemically normal. There was no barrier, no insulation, no membrane separating the two phases.

Since that first observation, we have documented 340 inversion events across 12,000 batches tested over two years. Approximately 2.8% of batches invert. The batches are chemically identical — same source, same treatment, same storage conditions. We have tested for trace contaminants, isotope ratios, dissolved gas content, mineral variation, microbial presence, and electromagnetic exposure history. Every variable we can measure is identical between inverting and non-inverting batches. No predictive model achieves accuracy better than chance.

The inversions are becoming more frequent. The 2.8% rate is an average. In the first six months, the rate was 1.1%. In the most recent quarter, it is 4.6%. The trend is linear and shows no sign of plateauing. Lab Director Ingrid Mutombo-Svensson has requested that this data not be included in the public water quality report. I am including it in this internal report because someone should be paying attention to the fact that our water is forgetting how to freeze.`,
    related_entities: ["Laceworks", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "Is the inversion rate increase connected to other anomalous trends in the GLMZ?",
      "What happens when the inversion rate reaches 100%?",
      "Is the water the anomaly, or is it the physics governing the water?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "physics", "water", "mpemba", "inversion"]
  },
  {
    name: "The Mass Hysteria of Block 7",
    document_type: "incident_report",
    author: "Meridian 88 Emergency Services, Incident Commander Raul Nakamura-Osei",
    date: "2214-03-08",
    classification: "suppressed",
    description: `At 22:17 on March 6th, 2214, Emergency Services received 43 calls within a 90-second window from residents of Block 7 in the upper Shelf. All callers reported the same thing: a building had appeared. Not a projection, not a holographic advertisement, not an AR overlay — a physical structure, visible to the naked eye, occupying the vacant lot at the intersection of Tier 3 and Radial 9. By the time the first responder unit arrived at 22:31, the building was not there. The lot was empty. It has been empty since 2198.

We interviewed 214 witnesses over the following 72 hours. Every account is consistent to a degree that makes the mass hallucination hypothesis difficult to sustain. The building was described as approximately fifteen stories tall, constructed of dark stone or concrete, with no windows below the eighth floor. The upper floors had narrow vertical windows emitting pale blue light. There was no entrance visible from street level. The building cast a shadow. Multiple witnesses report that the shadow fell across their apartments, darkening rooms that are normally lit by the adjacent commercial signage. The shadow was cold. Not metaphorically. Residents in the shadow's path report a temperature drop of approximately 6 degrees that persisted for the duration of the event.

Nineteen witnesses claim to have approached the building. Seven claim to have touched it. The surface was described as smooth, cold, and slightly damp. Three witnesses — Elif Johansson-Achebe, Marco Diallo-Park, and a teenager who provided only the name "Switch" — claim to have found an entrance on the building's north face and entered a lobby. They describe a large, empty room with a stone floor and a single elevator with no call button. The elevator door was open. They did not enter the elevator. They left. When they exited, the building was gone. They were standing in the vacant lot. Their feet were wet.

Block 7 residents have submitted a petition requesting 24-hour monitoring of the vacant lot. I am recommending approval, though I do not know what we expect to capture.`,
    related_entities: ["The Shelf", "Meridian 88", "Block 7"],
    credibility: "suppressed",
    story_hooks: [
      "What floor does the elevator in the phantom building go to?",
      "Is the building appearing at regular intervals that haven't been tracked?",
      "What is the blue light in the upper windows?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "mass_hallucination", "shelf", "phantom_structure", "collective_experience"]
  },
  {
    name: "The Fish Rain of 2214",
    document_type: "incident_report",
    author: "GLMZ Meteorological Authority, Incident Analyst Suki Okonkwo-Lindberg",
    date: "2214-07-12",
    classification: "restricted",
    description: `On July 10th, 2214, at approximately 16:45, fish began falling from the sky over the Industrial Corridor between Meridian 88 and the Gary Exclusion Zone. The event lasted eleven minutes. Approximately 12,000 fish fell over a 3-kilometer stretch of highway and adjacent industrial rooftops. The fish were alive when they hit the ground. Most died on impact. Some survived. The species was exclusively alewife — Alosa pseudoharengus — a freshwater fish native to Lake Michigan, found at depths of 20 to 60 meters.

There was no storm. There was no waterspout. The sky was clear with scattered cirrus clouds at 8,000 meters. Wind speed at ground level was 6 km/h, and upper atmosphere conditions were stable across all monitored altitudes. The Meteorological Authority has reviewed satellite imagery, radar data, and atmospheric sensor readings from every monitoring station within 200 kilometers. There is no mechanism by which 12,000 fish could have been lifted from Lake Michigan and deposited over the Industrial Corridor. The fish did not fall from any detectable altitude — radar showed no objects above the corridor prior to the event. They appeared at approximately 400 meters and fell.

This is not the first fish rain documented in the GLMZ. Municipal records reference events in 2187, 2193, 2201, and 2209. Each event occurred over the Industrial Corridor. Each involved exclusively alewife. Each occurred in clear weather with no meteorological explanation. The intervals are not regular — 6 years, 8 years, 8 years, 5 years — but the event parameters are identical. Same species. Same corridor. Same impossible delivery.

The fish from the July 10th event have been preserved for analysis. They are healthy specimens. Their stomach contents indicate they were feeding normally in deep water approximately 2 to 4 hours before the event. Their stress hormone levels are minimal. Whatever moved them from the lake to the sky did so without alarming them.`,
    related_entities: ["Industrial Corridor", "Meridian 88", "Gary Exclusion Zone", "Lake Michigan"],
    credibility: "verified",
    story_hooks: [
      "What is the connection between the Industrial Corridor and the fish rain events?",
      "Why exclusively alewife, and why are they unstressed?",
      "Is the interval between events shortening?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "biological", "fish_rain", "industrial_corridor", "lake_michigan", "recurring"]
  },

  // ============================================================
  // OBJECTS BUILT BY NO ONE (11-20)
  // ============================================================
  {
    name: "The Cataloger",
    document_type: "field_report",
    author: "Underworld Exploration Corps, Cartographer Zara Petrov-Igwe",
    date: "2208-05-19",
    classification: "classified",
    description: `We found it in Tunnel Section 77-D, approximately 900 meters below the Shelf waterline, in a passage that does not appear on any infrastructure map. The passage itself is anomalous — it is older than the tunnel system it connects to, based on geological stratification of the surrounding rock. The tunnel was bored through limestone that has been undisturbed for approximately 4,000 years, based on calcite deposition rates. The machine at its center has been running for longer than that.

The device is approximately two meters tall, one meter wide, and constructed of a metal that resists all non-destructive analysis. We cannot identify the alloy. We cannot scratch it. A small hopper at the top accepts objects — stones, screws, coins, whatever is placed inside. The objects disappear into the mechanism and are deposited into one of 144 output bins arranged in a 12-by-12 grid on the device's face. The sorting criteria are unknown. A steel ball bearing and a glass marble were placed in the same bin. A second steel ball bearing, identical to the first, was placed in a different bin. A dead beetle and a quanta coin were placed together. A live beetle was placed alone.

The device operates continuously. When no objects are provided, it sorts dust and air particles — the output bins accumulate fine residue in patterns that suggest the device is sorting ambient particulate matter by the same unknown criteria. It produces no sound. It generates no heat. It has no power source we can identify. It has no seams, no fasteners, no access panels. It is a single object. It was not assembled. It appears to have been manufactured as a continuous piece.

We placed a tracking sensor inside the hopper. The sensor was sorted into bin 37. Its telemetry data shows that the interior of the machine is larger than the exterior, which is not possible. The sensor recorded 4.7 seconds of travel time through the interior, during which it covered an apparent distance of 340 meters. The machine is one meter wide.`,
    related_entities: ["Underworld", "The Shelf"],
    credibility: "verified",
    story_hooks: [
      "What classification system does the Cataloger use?",
      "Who built a machine in a tunnel that predates the tunnel?",
      "What is inside the 340-meter interior of a 1-meter device?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "underworld", "machine", "impossible_origin", "sorting"]
  },
  {
    name: "The Spooler",
    document_type: "field_report",
    author: "Wisconsin Dead Zone Survey Team, Lead Investigator Rowan Achebe-Nguyen",
    date: "2209-09-14",
    classification: "restricted",
    description: `The abandoned Kenosha textile mill was surveyed as part of the standard Dead Zone infrastructure catalog in September 2209. The building is structurally unsound, partially collapsed, and has been unoccupied since the corporate withdrawal of 2161. Most of the equipment was salvaged decades ago. What remains is rust and concrete. Except for the mechanism on the third floor.

It sits in the center of what was once the main production floor, surrounded by collapsed ceiling beams and standing water. It is approximately 1.5 meters in diameter, cylindrical, and it is winding thread. The thread emerges from the base of the mechanism — not from a spool or a feed, but from the body of the device itself, as if the metal is extruding fiber. The thread is collected on a spindle that rotates at a constant 14 RPM. The spindle is full. It has been full since we found it. The thread does not accumulate beyond the spindle's capacity. Where the excess goes is unclear.

The thread itself is remarkable. Tensile strength testing indicates it is stronger than spider silk by a factor of three. It is thinner than a human hair. It does not burn. It does not dissolve in any solvent we have available in the field. Its molecular structure, as analyzed by portable spectrometry, does not correspond to any known polymer — synthetic or organic. The mechanism produces approximately 2 meters of thread per minute. It has been doing this, based on dust accumulation patterns and the oxidation state of the surrounding floor, for at least forty years.

There are no manufacturer markings on the device. No serial numbers. No logos. No text of any kind. It is made of a brushed metal that is warm to the touch. It hums at a frequency of 127 Hz — a tone that does not correspond to any mechanical resonance we can attribute to its visible components. We left a recording device. It is still spooling.`,
    related_entities: ["Wisconsin Dead Zone", "Kenosha"],
    credibility: "verified",
    story_hooks: [
      "What material is the thread made from, and why can't it be identified?",
      "Where does the excess thread go when the spindle is full?",
      "Could the thread be used, and what would happen if it were?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "wisconsin", "dead_zone", "machine", "thread", "impossible_origin"]
  },
  {
    name: "The Counter",
    document_type: "investigation",
    author: "Old Harbor Municipal Works, Infrastructure Surveyor Kian Okonkwo-Strand",
    date: "2210-02-28",
    classification: "restricted",
    description: `The display is embedded in the east wall of Building 4120 in Old Harbor, at a height of 1.7 meters, between a condemned doorway and a defunct utility conduit. It is a numeric display — amber digits on a dark background, approximately 8 centimeters tall — and it is counting upward. The current reading, as of this report's filing, is 847,291,003. It increments by one at irregular intervals ranging from 0.3 to 47 seconds. There is no pattern to the interval variation.

Building 4120 was constructed in 2134. The display was present during the original construction inspection — it appears in photographs from the building's certification file, visible in the background of a shot of the east corridor. In that photograph, the reading is 412,006,117. The display has been counting for at least 76 years without interruption. It has never been serviced. It has never been powered by any building system — it is not connected to any wiring, conduit, or power source. It does not appear in the building's electrical plans, mechanical plans, or architectural drawings. No contractor, architect, or building manager has any record of its installation.

We attempted to remove it in 2208. The wall was cut around the display and the section was extracted. Behind the display there is nothing — no housing, no circuit board, no mechanism. The display is a flat surface, approximately 3 millimeters thick, fused to the concrete. The concrete behind it is solid and undisturbed. The display continued counting during extraction. It continued counting when the wall section was placed in a shielded container. It continued counting when the container was placed in a Faraday cage. It is counting now, in a storage facility in the lower Shelf, removed from the building it occupied for eight decades. It has not lost a beat.

What it counts is unknown. We have correlated the count against population data, traffic flow, network packets, heartbeats within a radius, births, deaths, transactions, and seventeen other metrics. None correlate. It counts something. We do not know what.`,
    related_entities: ["Old Harbor", "Meridian 88", "The Shelf"],
    credibility: "verified",
    story_hooks: [
      "What is the Counter counting, and what happens when it stops?",
      "Who or what embedded it in a wall before the building was built?",
      "Is there a zero point, and what happened at count zero?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "old_harbor", "machine", "impossible_origin", "counting"]
  },
  {
    name: "The Listener",
    document_type: "field_report",
    author: "Michigan Perimeter Survey Team, Acoustic Specialist Adaeze Larsson-Iwu",
    date: "2207-08-11",
    classification: "classified",
    description: `The structure occupies the roof of a decommissioned water treatment facility on the Michigan lakeshore, approximately 40 kilometers north of the GLMZ perimeter. It is a parabolic dish, 3 meters in diameter, constructed of a smooth ceramic material that is cool to the touch regardless of ambient temperature. It is oriented upward at an angle of 73 degrees from horizontal, pointing at a section of sky that contains no known satellite, station, or signal source. It records nothing. It transmits nothing. Every instrument we have placed in its focal point has registered silence.

The facility was decommissioned in 2089. Satellite imagery from 2074 shows the dish already present on the roof. The facility was constructed in 2031. Aerial survey photographs from 2029, taken during the site preparation phase, show the dish on the ground at the location where the facility would later be built. The dish predates the building it sits on. It was there first. The building was constructed around and beneath it.

The dish is not the anomaly. The anomaly is what happens to people who stand near it. In the eighteen months since our survey team identified the structure, fourteen personnel have spent time within its 10-meter radius. All fourteen report the same experience: a feeling of being listened to. Not watched — listened to. Not by a person, not by a machine, but by the structure itself. The feeling is described as profoundly calming. Two team members who suffer from anxiety disorders report that their symptoms vanished entirely while in proximity to the dish and did not return for several days afterward. One team member, who asked not to be identified, spent four hours sitting at the base of the dish and described it as "the first time in my life I felt like something was paying attention to me without wanting anything."

We cannot explain this. There is no acoustic, electromagnetic, or chemical mechanism that accounts for a subjective experience of being heard. And yet the experience is consistent, reproducible, and — by every account — therapeutic. I am filing this under anomalous phenomena. I am also requesting permission to go back.`,
    related_entities: ["Michigan Lakeshore", "GLMZ Perimeter"],
    credibility: "verified",
    story_hooks: [
      "What is the dish listening to, or listening for?",
      "Why does proximity produce a therapeutic effect?",
      "What section of sky is it pointed at, and what was there before?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "michigan", "machine", "impossible_origin", "acoustic", "therapeutic"]
  },
  {
    name: "The Printer",
    document_type: "field_report",
    author: "Underworld Exploration Corps, Linguist Specialist Tariq Svensson-Abubakar",
    date: "2212-04-06",
    classification: "classified",
    description: `The device was found in an Underworld chamber at sub-level 12, accessible only through a maintenance shaft that required three hours of crawling to traverse. The chamber is approximately 4 meters by 6 meters, carved from bedrock, and contains nothing but the device and its output. The output is paper. Thousands of pages, stacked in neat columns around the room, some reaching the ceiling. The oldest pages, based on paper degradation analysis, are approximately 200 years old. The newest page was produced while we watched.

The device is a box. Flat black, 60 centimeters square, 20 centimeters tall. It has no visible input mechanism — no feed tray, no ink reservoir, no data port. At irregular intervals — we observed periods between 4 minutes and 3 hours — a sheet of paper emerges from a slot on its front face. The paper is warm. It is a standard cellulose-based paper that could have been manufactured anywhere, except that chemical analysis reveals no bleaching agents, no sizing compounds, and no manufacturing residues of any kind. The paper is pure cellulose. It should not exist in sheet form without processing. It exists in sheet form.

The text on the pages is dense, consistent, and written in no language that any member of my team or any consultant we have engaged can identify. It is not a cipher — the character frequency distribution does not match any known language encrypted or otherwise. It is not random — there are clear syntactic structures, recurring symbols, and what appear to be paragraph breaks and section headings. The character set contains approximately 4,000 unique symbols. The text changes — no two pages are identical. Whatever this device is writing, it is writing a lot of it, and it has been writing continuously for centuries.

I brought three pages to the surface. Within six hours, the text on two of them had changed. Not faded — changed. Different characters in different arrangements on the same paper. The third page remains stable. I do not know why. I have sealed it in an airtight container and I am trying not to think about it.`,
    related_entities: ["Underworld", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What language is the Printer writing in, and who can read it?",
      "Why does the text change on some pages but not others?",
      "What has it been writing for 200 years?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "underworld", "machine", "impossible_origin", "language", "text"]
  },
  {
    name: "The Clock",
    document_type: "investigation",
    author: "Canadian Border Survey Corps, Temporal Analyst Maren Obi-Johansson",
    date: "2211-07-22",
    classification: "classified",
    description: `The mechanism was found in the Canadian border dead zone, embedded in the remains of a pre-collapse customs station approximately 15 kilometers west of the former Port Huron crossing. The station has been abandoned since 2094. The mechanism has not.

It is a clock. A physical, mechanical clock with a face, hands, and a visible escapement. The face is 30 centimeters in diameter, made of a white material that is neither ceramic nor metal — it resists identification by every non-destructive method we have employed. The numerals are standard Arabic, 1 through 12, but there are 13 of them, distributed evenly around the face. The clock has three hands: hour, minute, and a third hand that completes a revolution every 47 minutes. The time displayed does not correspond to any timezone on Earth. It does not correspond to any known astronomical reference frame.

The clock gains exactly 11 minutes per day relative to UTC. This drift is precise to the millisecond. It has been precise to the millisecond for the entire fourteen-month monitoring period. A mechanical clock that gains 11 minutes per day should show cumulative error — variations in temperature, humidity, and gravity should produce fluctuations. This clock does not fluctuate. It is more accurate to its own timekeeping standard than any atomic clock is to UTC. It is simply keeping time for a day that is 11 minutes longer than ours.

We cannot identify the power source. The escapement moves. The hands move. There is no winding mechanism, no spring, no battery, no power input. The mechanism operates with zero detectable energy input. It has been doing so for at least the 130 years since the customs station was abandoned, based on dust accumulation on the housing. Given the mechanism's construction — which shows no wear whatsoever — it has likely been operating much longer.

I have set a synchronized UTC clock beside it. Every day, it falls 11 minutes further behind. Or we fall 11 minutes further behind. I am no longer certain which frame of reference is drifting.`,
    related_entities: ["Canadian Border Dead Zone", "Port Huron"],
    credibility: "verified",
    story_hooks: [
      "Whose time is the clock keeping, and where is the day 11 minutes longer?",
      "What is the thirteenth numeral for?",
      "What does the third hand measure?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "canadian_border", "dead_zone", "machine", "impossible_origin", "temporal"]
  },
  {
    name: "The Weaver",
    document_type: "field_report",
    author: "Ohio Badlands Survey Expedition, Geologist Priya Strand-Okonkwo",
    date: "2210-10-08",
    classification: "restricted",
    description: `The structure stands in the open badlands approximately 60 kilometers south of the Toledo ruins, on a flat expanse of cracked clay and sparse scrub. It is 4 meters tall, roughly conical, and composed of a dark metalite material — or appears to be. We have not been able to sample it. Cutting tools do not mark its surface. It is smooth, seamless, and warm.

It builds things. Small geometric objects — polyhedra, toroids, interlocking lattices — assembled from the dust and debris of the surrounding badlands. The process is visible: particulate matter rises from the ground in thin streams, converges on the structure's apex, and descends the sides as finished objects that detach at the base and accumulate on the ground. The objects range from 2 to 15 centimeters. They are geometrically perfect. We have measured them with laser micrometers. Tolerances are below our instrument's margin of error. A dodecahedron produced by this structure is more precise than anything manufactured by human hands or machines.

The objects serve no identifiable purpose. They are solid, composed of compressed mineral dust bonded at a molecular level — essentially stone, but structured with a uniformity that natural stone does not possess. They are not tools, not components, not art (unless the artist is unconcerned with audience). They accumulate in concentric rings around the structure's base, sorted by geometry — all dodecahedra together, all icosahedra together, arranged in a pattern that our topologist says describes a four-dimensional tessellation projected onto a two-dimensional surface.

We estimate, based on accumulation depth and production rate, that the Weaver has been building these objects for approximately 300 years. The oldest objects at the center of the accumulation show no weathering. They are identical to the newest. The structure itself shows no wear. It was here before anyone found it. It will be here after everyone leaves. It does not care that we are watching. It has work to do.`,
    related_entities: ["Ohio Badlands", "Toledo Ruins"],
    credibility: "verified",
    story_hooks: [
      "What is the four-dimensional tessellation describing?",
      "Do the geometric objects interact or connect with each other?",
      "What happens when the accumulation field reaches a critical size?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "ohio_badlands", "machine", "impossible_origin", "geometry", "construction"]
  },
  {
    name: "The Mirror",
    document_type: "investigation",
    author: "Underworld Exploration Corps, Deep Team 3, Lead Surveyor Kayo Müller-Adesanya",
    date: "2213-11-15",
    classification: "classified",
    description: `The surface is located in a chamber at sub-level 19, the deepest accessible point in the western Underworld tunnel network. The chamber was unsealed during expansion blasting in 2213. The surface occupies the entire north wall — approximately 6 meters wide and 3 meters tall. It is polished to a reflective finish that exceeds anything achievable with current manufacturing technology. Our optical engineer estimates the surface roughness at less than one nanometer. It is not glass. It is not metal. It is stone — a basalt-like ignite that should not be capable of holding a polish this fine.

The surface reflects the chamber. It does not reflect the chamber it is in. The room in the reflection is larger, furnished, and occupied by objects that have no counterpart in the physical space. The reflected room contains three chairs arranged around a low table. The table holds a lamp that emits light — the reflection is brighter than the chamber, illuminated by a light source that does not exist on our side. The walls of the reflected room are lined with shelves containing objects we cannot identify at this distance. The floor is carpeted.

The furniture moves. Not while observed — we have maintained continuous visual monitoring for 72 hours and nothing has changed in the direct field of view. But the objects shift between observations. A chair that faced east at 14:00 faces north at 14:05, during a period when the monitoring camera's view was unobstructed and recorded no movement. The objects in the reflection move without moving. They are in different positions despite never having been observed changing position.

Team member Adaeze Strand-Petrov placed her hand against the surface. She reported that it was warm and that she could feel vibration — a low, slow pulse approximately once every four seconds. She also reported that the reflection of her hand appeared in the mirror approximately 0.5 seconds after she placed it. Every other reflection — the team, the equipment, the chamber — appears instantaneously. Only living tissue has a delay. We do not understand the significance of this. We do not understand any of this.`,
    related_entities: ["Underworld", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What room is the mirror reflecting, and where is it?",
      "What moves the furniture when no one is watching?",
      "Why does living tissue have a reflection delay?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "underworld", "mirror", "impossible_origin", "spatial", "reflection"]
  },
  {
    name: "The Altar",
    document_type: "field_report",
    author: "Indiana Wilderness Survey, Environmental Scientist Tomoko Achebe-Reis",
    date: "2208-06-30",
    classification: "restricted",
    description: `The stone sits in a clearing in the Indiana wilderness, approximately 90 kilometers south of the Gary Exclusion Zone. It is flat, roughly rectangular, 2 meters by 1.5 meters, and 40 centimeters thick. It is composed of granite that does not match any geological formation within 500 kilometers. It was not quarried — there are no tool marks, no cut faces, no evidence of shaping. It is naturally flat to a tolerance of 2 millimeters across its entire surface. This does not occur in nature.

Around the stone, in a perfect circle with a radius of exactly 3 meters, nothing grows. The soil within the circle is chemically identical to the soil outside it — same pH, same mineral content, same moisture, same microbial population. Seeds planted within the circle germinate, grow to approximately 2 centimeters, and die. Every species we have tested. The boundary is sharp — plants growing at 3.01 meters from the stone are healthy. At 2.99 meters, they are dead. The boundary does not drift, does not fluctuate, does not respond to seasonal changes. It has been exactly 3 meters for the entire four-year observation period.

Animals avoid the clearing. Game cameras positioned around the perimeter have recorded deer, coyotes, raccoons, and feral dogs approaching the tree line and turning away. Birds do not fly over the clearing — they divert around it. Insects enter the circle but do not land on the stone. The single exception is a species of moth — Actias luna — that lands on the stone regularly and remains for hours before departing. We do not know why luna moths are exempt.

Instruments placed on the stone return different readings than identical instruments placed beside it. A thermometer on the stone reads 2 degrees lower. A magnetometer on the stone detects a field that the one beside it does not. A clock on the stone loses 3 seconds per hour. The instruments are calibrated. The instruments beside the stone are accurate. The instruments on the stone are also accurate — they are measuring something real. They are simply measuring a different version of here.`,
    related_entities: ["Indiana Wilderness", "Gary Exclusion Zone"],
    credibility: "verified",
    story_hooks: [
      "Why are luna moths the only creatures that interact with the stone willingly?",
      "What is the 'different version of here' that instruments measure on the stone?",
      "Is the stone an object, a marker, or a boundary?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "indiana", "wilderness", "stone", "impossible_origin", "zone_of_exclusion"]
  },
  {
    name: "The Archive",
    document_type: "classified_briefing",
    author: "Meridian 88 Tunnel Authority, Director of Expansion, [REDACTED]",
    date: "2214-01-03",
    classification: "classified",
    description: `This briefing is classified at the highest level available to the Tunnel Authority. Distribution is restricted to the Director's office and the three section chiefs present at the discovery. What follows is an account of what Expansion Team 14 found during the southern bore extension on December 28th, 2213.

At a depth of approximately 45 meters, the bore struck a sealed chamber. The chamber is constructed of a material that is not concrete, not stone, and not metal — it is a composite that our materials team cannot identify. The walls are smooth and seamless. The chamber is 12 meters by 8 meters with a ceiling height of 3 meters. It contains 4,712 data shards arranged on shelving units that are integrated into the walls — not mounted, not bolted, but continuous with the wall material. The shelving and the walls are a single piece.

The data shards are standard-format optical crystals compatible with current GLMZ reading hardware. This is significant because the chamber, based on geological context and the undisturbed state of the surrounding rock, predates the tunnel system by a minimum of 80 years. Optical crystal data storage was not developed until 2171. The chamber has been sealed since at least 2130. The shards contain data about people. Biographical information, medical records, residential addresses, employment histories, relationship networks. The data is comprehensive, detailed, and written in standard GLMZ data formatting conventions that were not established until 2185.

The people in the records do not exist. We have cross-referenced every name, every biometric marker, every address against the full municipal database. No matches. Birth dates in the records range from 2230 to 2290. The people described in these shards have not been born yet. The records describe their lives in detail — careers, illnesses, marriages, children, deaths. One record describes a woman who will be born in 2247 and die in 2319, survived by three children whose records are also in the archive. Her cause of death is listed. Her final address is a building that does not yet exist, on a street that has not yet been named, in a district that has not yet been zoned.

I do not know what to do with this information. I am not certain anyone does. The chamber has been sealed and the bore route has been diverted. I am recommending that this remain classified indefinitely.`,
    related_entities: ["Underworld", "Meridian 88", "Tunnel Authority"],
    credibility: "classified",
    story_hooks: [
      "Who built the Archive, and how did they know the future?",
      "Are the futures described in the shards fixed, or do they change?",
      "What happens when someone from the Archive's records is actually born?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "object", "underworld", "archive", "impossible_origin", "temporal", "precognition"]
  },

  // ============================================================
  // WILDERNESS & THE OUTSIDE (21-30)
  // ============================================================
  {
    name: "The Zone of Silence",
    document_type: "field_report",
    author: "GLMZ Inter-City Transit Authority, Dead Zone Survey Division",
    date: "2212-03-15",
    classification: "restricted",
    description: `The zone occupies approximately 40 square kilometers of wasteland between Meridian 88 and the Milwaukee Sprawl, centered on coordinates that correspond to no historical structure, settlement, or geological feature. It is flat. It is empty. It is wrong.

Radio communication ceases within the zone's boundary. Not gradually — a hard cutoff. One meter outside the perimeter, signals are strong and clear. One meter inside, silence. Every frequency. Every modulation. Every protocol. Military-grade encrypted burst transmissions, low-frequency ground-penetrating radar, even simple AM broadcast — all gone. Compasses spin continuously within the zone. GPS receivers show location data that drifts in patterns our navigation team describes as "geometrically coherent but physically meaningless" — the receivers believe they are moving through a space that does not correspond to the ground they occupy.

Drones lose contact at the boundary and fly in wide spirals until their batteries die. We have lost fourteen drones. The flight recordings, recovered from crash sites, show instruments that disagree with each other — altitude sensors and accelerometers reporting different values than barometric pressure and visual odometry. The drones are not malfunctioning. Their instruments are each measuring accurately. They are simply measuring different realities.

Meteorites land in the zone at a rate approximately 400 times the statistical average for an area this size. We have cataloged 89 impact sites in three years of observation. The meteorites are unremarkable — ordinary chondrites, standard composition, nothing unusual except their improbable concentration. Research teams that enter the zone and return report no physical symptoms. They report a psychological change they struggle to articulate. They come back quieter. Not traumatized. Not afraid. Quiet in a way that suggests they heard something in the silence that the rest of us cannot.`,
    related_entities: ["Reclaimed Zone", "Meridian 88", "Milwaukee Sprawl"],
    credibility: "verified",
    story_hooks: [
      "What is at the center of the Zone of Silence?",
      "Why do meteorites concentrate there at 400 times the normal rate?",
      "What do the research teams hear in the silence?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "zone_of_silence", "electromagnetic", "meteorite", "psychological"]
  },
  {
    name: "The Marfa Lights of the Corridor",
    document_type: "investigation",
    author: "GLMZ Perimeter Observation Network, Station 14 Commander Anika Diallo-Strand",
    date: "2211-11-09",
    classification: "restricted",
    description: `The lights appear every night. They have appeared every night for as long as anyone has maintained continuous observation of the corridor between Meridian 88 and the Gary Exclusion Zone — a minimum of 90 years based on the earliest Perimeter Observation logs. They appear after full dark, between 21:00 and 23:00, and persist until dawn. They are visible to the naked eye from observation stations at distances up to 30 kilometers.

They are spherical, approximately 1 to 3 meters in diameter, and emit light across a spectrum that shifts over time — amber to blue to white to green and back. They move at altitudes between 5 and 200 meters, at speeds ranging from stationary to approximately 80 km/h. They split. A single light will divide into two, three, or seven smaller lights that move independently before merging back into one. They merge. Two lights of different colors will combine into a single light of a third color that is not an additive product of the first two. They respond to observation — when tracked by targeting radar, they accelerate. When observed through telescopic optics, they hold position. When approached by ground vehicle, they retreat at a speed precisely matching the vehicle's approach speed.

They are not plasma. They are not ball lightning. They are not bioluminescence. They are not swamp gas, vehicle headlights, refracted starlight, or military flares. They have been investigated by seventeen research teams over five decades. No team has produced an explanation. Three teams have produced data that contradicts their own hypotheses so thoroughly that they withdrew their findings.

The lights predate every structure in the corridor. They predate the cities they appear between. Indigenous oral histories reference them. Colonial survey maps mark them. They are older than us. They are not interested in us. They are not uninterested either. They are doing something out there in the dead land, every night, and they have been doing it for a very long time.`,
    related_entities: ["Industrial Corridor", "Meridian 88", "Gary Exclusion Zone"],
    credibility: "verified",
    story_hooks: [
      "What are the lights doing every night in the corridor?",
      "Why do they respond differently to different observation methods?",
      "What is their relationship to the land they occupy?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "lights", "corridor", "recurring", "ancient"]
  },
  {
    name: "St. Elmo's Cold Fire",
    document_type: "incident_report",
    author: "Meridian 88 Electrical Safety Authority, Inspector Nikolai Okafor-Bjorn",
    date: "2213-08-22",
    classification: "restricted",
    description: `Corona discharge — St. Elmo's fire — is a well-understood phenomenon. Ionized air near electrically charged objects produces a visible glow, typically blue or violet. It occurs during thunderstorms, near high-voltage equipment, and on pointed structures that concentrate electrical fields. It requires a strong electric field. It requires an atmosphere capable of ionization. It requires, fundamentally, electricity.

The discharge events documented in this report occur in sealed, grounded, electrically dead interior spaces with no active power systems, no atmospheric charge, and no identifiable energy source. The first documented occurrence was in Engine Room 7 of the decommissioned Lakewall pumping station on February 14th, 2213. The room has been without power since 2196. All wiring was removed during decommissioning. The room is sealed, grounded through the building's structural steel, and monitored by battery-powered environmental sensors. At 03:17, sensors recorded a blue-white glow covering approximately 60% of the room's wall surfaces. Duration: 4 minutes 22 seconds. Temperature of the glow: -2 degrees Celsius. The walls were cold. The glow was cold. It burned nothing. It left no residue.

Since February, we have documented 31 identical events across 14 locations — all sealed, all grounded, all electrically dead. Engine rooms, pump houses, transformer vaults, generator bays. All infrastructure that once carried or generated significant electrical power and now carries none. The discharge burns cold and blue on surfaces that have no reason to discharge. Our electromagnetic monitoring detects nothing before, during, or after the events. There is no charge. There is no field. There is no mechanism.

The events are increasing in frequency. February: 2 events. March: 4. April: 7. July: 11. The glow is also intensifying — early events were faint, barely visible to sensors. Recent events are bright enough to be seen through sealed doorways by maintenance personnel in adjacent corridors. Engine Room 7, where it started, now glows almost continuously. I have standing orders to keep that door closed.`,
    related_entities: ["Lakewall", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "Why only in spaces that once carried electricity?",
      "Is the cold fire a residual phenomenon or something new moving into old infrastructure?",
      "What happens when the glow becomes continuous in all 14 locations?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "corona_discharge", "cold_fire", "infrastructure", "electromagnetic"]
  },
  {
    name: "The Bigelow Ranch",
    document_type: "investigation",
    author: "GLMZ Anomalous Events Task Force, Lead Investigator Jin Adesanya-Kim",
    date: "2212-05-17",
    classification: "classified",
    description: `The property sits on the eastern edge of the Ohio dead zone, 3 kilometers from the nearest maintained road. It was a soybean farm before the collapse. The farmhouse, two barns, and a equipment shed remain standing. The property has been under continuous surveillance since 2210, when a salvage team reported objects appearing inside the main barn.

The objects materialize without warning. No sound, no flash, no displacement of air. They are simply not there, and then they are. The objects are diverse — metal components, sealed containers, geometric shapes, organic material that defies classification. Most appear on the barn floor. Some appear suspended in air. On March 3rd, 2212, a metallic ovoid approximately 40 centimeters in diameter appeared at a height of 2 meters above the barn floor and remained there, motionless, for 20 minutes before vanishing. The ovoid cast a shadow. It had mass — a laser displacement sensor confirmed it was deflecting the beam. It was present, physical, and real. And then it was gone.

The observation that prompted the classification upgrade occurred on April 28th. A spherical object appeared in the farmhouse kitchen at 14:07 while five investigators were present. It hovered at chest height, silver and featureless. Investigator Fatima Okonkwo-Strand left the room to retrieve a camera from the adjacent hallway. The object vanished the moment she crossed the threshold. She returned. The object reappeared. She left again. It vanished. This was repeated seven times with the same result. The object's presence was contingent on Investigator Okonkwo-Strand's presence in the room. No other team member's departure or arrival affected it.

We subsequently tested whether objects responded to specific observers. They do. Different objects appear in the presence of different investigators. Some objects appear only when certain combinations of observers are present. The property is not producing random phenomena. It is producing phenomena tailored to its audience. I do not know what this means. I know that it changes everything about how we approach anomalous investigation if the anomaly is watching us back.`,
    related_entities: ["Ohio Dead Zone", "GLMZ Anomalous Events Task Force"],
    credibility: "verified",
    story_hooks: [
      "What is the relationship between specific observers and specific objects?",
      "Is the property a location or an intelligence?",
      "What happens if no one observes the property — does anything still appear?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "materialization", "observer_dependent", "ohio_dead_zone"]
  },
  {
    name: "The Taos Acoustic Zone",
    document_type: "field_report",
    author: "Meridian 88 Environmental Health Division, Acoustics Unit",
    date: "2211-04-18",
    classification: "restricted",
    description: `The zone encompasses a six-block radius in the lower Laceworks, centered on the intersection of Cascade Row and Pipe Street. Within this radius, sound behaves in ways that violate every acoustic model we have applied. The effect is permanent, consistent, and bounded by a hard perimeter that does not correspond to any physical structure, material boundary, or atmospheric condition.

Inside the zone, a whisper carries. A person speaking at conversational volume in the center of the zone can be heard clearly at the perimeter, 200 meters away, with no loss of fidelity or amplitude. The sound does not attenuate. It does not echo. It does not reflect off surfaces. It simply travels, at full strength, through whatever medium lies between source and listener. Walls do not block it. Floors do not block it. A whisper in a basement apartment is audible on the roof, six stories up, as clearly as if the speaker were standing beside you.

Conversely, loud sounds do not propagate. A gunshot within the zone is inaudible beyond 3 meters. We have tested this with controlled detonations — a firecracker at the zone's center registered 0 decibels at 4 meters. The sound is not absorbed. The energy does not convert to heat. The sound simply stops. The boundary between propagation and non-propagation is absolute. There is no gradient. At 3 meters, a gunshot is deafening. At 3.1 meters, silence.

The perimeter of the zone is equally sharp. One step inside and you hear everything — every conversation, every footstep, every breath within six blocks. One step outside and normal acoustics resume. Residents have adapted. The Taos Zone, as they call it, is one of the quietest neighborhoods in the Laceworks despite being one of the most densely populated. Everyone speaks softly. Everyone has learned. The few who haven't — new arrivals, visitors, people having bad days — learn quickly that their raised voices carry to every ear in six blocks while the ambient noise of the city outside the zone is entirely absent. It is an island of whispers in a city of noise, and no one built it.`,
    related_entities: ["Laceworks", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What created the acoustic inversion zone?",
      "How do residents and criminals adapt to an environment where whispers carry and gunshots don't?",
      "Is the zone expanding, contracting, or stable?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "acoustic", "laceworks", "zone", "sound_inversion"]
  },
  {
    name: "The Hutchison Basement",
    document_type: "investigation",
    author: "GLMZ Anomalous Events Task Force, Materials Specialist Dmitri Larsson-Osei",
    date: "2210-12-11",
    classification: "classified",
    description: `The footage was brought to the Task Force by a scrap dealer named Obinna Svensson-Nkosi, who operates out of a basement workshop in the lower Shelf. He claimed that strange things happened in his workshop when he was not paying attention. He had set up a camera. The footage shows a 12-kilogram steel plate rising from his workbench, rotating 90 degrees, and settling back down. Duration: 8 seconds. The plate was not attached to any mechanism. There were no magnets in the workshop. There was no vibration, no air current, no observable cause.

We installed professional monitoring equipment. Over six weeks, we documented 47 events. Metal objects levitating — ranging from small bolts to a 30-kilogram engine block that rose 40 centimeters off the floor and remained suspended for 11 seconds. A wooden dowel partially embedded in a steel plate as if the materials had merged at a molecular level without heat, pressure, or chemical bonding. A lead cannonball that sank 6 centimeters into a concrete floor and remained there, fused with the surface, the concrete showing no crack or deformation — simply accepting the lead as if it had always been there.

The critical observation: the events occur only when Obinna is present and not attempting to cause them. When he tries to demonstrate the effect for observers, nothing happens. When he is working on something else — repairing a motor, sorting scrap — objects move. We set up a protocol: Obinna works normally, ignoring the monitoring equipment, while we observe from an adjacent room via camera. Events occur. When we enter the room and ask him to reproduce what just happened, events cease. Every time.

Obinna is not doing this. His biometrics show no anomalous readings. His BCI — a standard model, three years old — shows no unusual activity. His brain scans are normal. He is not telekinetic. He is not generating fields. He is a 54-year-old scrap dealer with bad knees and a fondness for Earl Grey tea. The events happen around him. They do not happen because of him. They happen in spite of observation. They are shy.`,
    related_entities: ["The Shelf", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "Why does intention prevent the effect?",
      "Is the phenomenon tied to Obinna or to the location?",
      "What would happen if the merged materials were analyzed at the molecular level?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "levitation", "material_fusion", "observer_effect", "shelf"]
  },
  {
    name: "The Star Wrong Report",
    document_type: "classified_briefing",
    author: "GLMZ Perimeter Observation Network, Astronomical Division",
    date: "2214-09-30",
    classification: "classified",
    description: `On September 22nd, 2214, at approximately 02:15 local time, three separate groups of witnesses in the Wisconsin wilderness — a Perimeter patrol team, a salvage crew, and a group of transit refugees — independently reported that the night sky changed. Not a partial change. Not an aurora, not a light phenomenon, not cloud interference. The stars were wrong. The constellations visible from approximately 02:15 to 02:55 were not the constellations that should have been visible from that latitude, that longitude, at that date and time. They were not constellations at all.

The patrol team, located 40 kilometers northwest of the Milwaukee Sprawl, reported an unfamiliar starscape with approximately twice the visible star density of normal sky. They described "rivers of light" connecting star clusters in patterns they had never seen. Their team astronomer, Lieutenant Keiko Achebe-Strand, stated that the positions were not consistent with any orientation of Earth's night sky at any time of year, from any location on the planet's surface. The salvage crew, 200 kilometers north, reported the same sky. The refugees, 160 kilometers southeast, reported the same sky. Three groups. Three locations. Same alien starscape. Same 40-minute window.

Satellite data for the event window shows normal sky conditions. Orbital telescopes recorded standard star positions. The anomalous sky was visible only from ground level in the Wisconsin wilderness. It was not visible from Meridian 88, 60 kilometers to the south. It was not visible from the Milwaukee Sprawl, 40 kilometers to the east. The witnesses were standing under a sky that no instrument recorded and no one outside the affected area could see.

All three groups report that the transition was instantaneous. Normal sky, then wrong sky, then normal sky. No fade, no shimmer, no distortion. Lieutenant Achebe-Strand took 14 photographs during the event. The photographs show normal sky. Her eyes saw one thing. Her camera saw another. She is certain of what she saw. I have no reason to doubt her. I have no framework for believing her either.`,
    related_entities: ["Wisconsin Wilderness", "Milwaukee Sprawl", "GLMZ Perimeter"],
    credibility: "verified",
    story_hooks: [
      "Whose sky were the witnesses seeing?",
      "Why was the anomalous sky visible only to human eyes and not to instruments?",
      "Is the Wisconsin wilderness a window to somewhere else?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "sky", "stars", "wrong_sky", "perception", "wisconsin"]
  },
  {
    name: "The Returning",
    document_type: "investigation",
    author: "GLMZ Missing Persons Bureau, Senior Analyst Camille Obi-Nakamura",
    date: "2213-06-14",
    classification: "suppressed",
    description: `The wasteland between cities consumes people. This is known. Travelers, refugees, exiles, the desperate — they enter the dead zones and they do not come back. The Missing Persons Bureau maintains a registry. It is long. It grows every month. This report is not about the missing. This report is about the ones who come back.

Since 2200, the Bureau has documented 67 cases of individuals returning from the wasteland after extended absence. The shortest absence was 18 days. The longest was 11 years. In every case, the returning individual is physically unchanged from the day they disappeared. Not well-preserved. Not healthy for their age. Unchanged. A man who disappeared at 34 and returned at 45 looks 34. His hair has not grown. His clothes show no wear. A wound he sustained the day before his disappearance was still fresh and bleeding. He had been gone for 11 years. His body had experienced, by every measurable indicator, approximately 6 hours.

They do not talk about where they were. This is not a choice to withhold information — or if it is, the consistency is remarkable. Sixty-seven people, interviewed separately over thirteen years, all exhibit the same response when asked: a pause, a look of concentration, and then a statement that they cannot describe it. Not that they won't. That they can't. The experience resists language. Several have attempted to write it down and produced pages of text that they themselves cannot read afterward — the words are English but the sentences do not cohere.

The returned are different. Their families know it. Their friends know it. The Bureau's psychological assessors know it. But no assessment can identify what has changed. Cognitive function: normal. Personality profiles: unchanged. Memory: intact for everything up to the disappearance and after the return. The gap is blank. They are the same people. They are not the same people. One returnee's wife told our assessor, "He came back wearing my husband's face, and I love him, and he is not my husband." She could not elaborate. Neither can we.`,
    related_entities: ["GLMZ Wasteland", "Missing Persons Bureau"],
    credibility: "suppressed",
    story_hooks: [
      "Where do the Returned go during their absence?",
      "What changes in them that everyone can feel but no one can identify?",
      "Is the wasteland consuming people, or is something else taking them and giving them back?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "disappearance", "return", "temporal", "identity", "wasteland"]
  },
  {
    name: "The Raining Fish of Lake Erie",
    document_type: "incident_report",
    author: "GLMZ Eastern Perimeter Authority, Weather Station 9",
    date: "2214-10-01",
    classification: "restricted",
    description: `At 11:23 on September 29th, 2214, fish began falling from a clear sky over the eastern perimeter transport corridor, approximately 8 kilometers south of the former Cleveland boundary. The event lasted seven minutes. An estimated 8,000 fish fell over a 1.5-kilometer stretch of road and adjacent scrubland. The fish were alewife — Alosa pseudoharengus — the same species documented in every GLMZ fish rain event on record.

This is the second fish rain of 2214. The first occurred on July 10th over the Industrial Corridor west of Meridian 88. That event was attributed — informally, without evidence — to some unidentified atmospheric transport mechanism originating in Lake Michigan. This event occurred 400 kilometers east, over terrain adjacent to Lake Erie, not Lake Michigan. The fish, however, are from Lake Michigan. Isotope analysis of tissue samples confirms a Lake Michigan origin, consistent with the western basin's chemical signature. Fish from Lake Michigan fell from the sky above Lake Erie.

The fish were alive. Witnesses — three perimeter patrol officers and a transit convoy of eleven vehicles — describe the fish as "confused but swimming." They fell in a dispersed pattern consistent with objects dropped from a moderate height, struck the ground, and those that survived the impact thrashed on the road surface. 72% of recovered specimens were alive at impact. Their stress markers, as in previous events, were minimal. They were not distressed by the experience of being transported 400 kilometers through the air and dropped from the sky. They were distressed by being on a road instead of in water.

Weather Station 9 recorded no anomalous atmospheric conditions. Clear sky, 12 km/h wind, 22 degrees Celsius. There was no mechanism. There is never a mechanism. The fish simply appear in the air and fall. The only new data point from this event is the Lake Michigan origin of fish falling near Lake Erie. Whatever moves them does not move them short distances. It does not move them logically. It moves them because it moves them.`,
    related_entities: ["Lake Erie", "Lake Michigan", "Eastern Perimeter", "Cleveland Boundary"],
    credibility: "verified",
    story_hooks: [
      "Why always alewife, and why always from Lake Michigan regardless of location?",
      "Is the fish rain connected to the Zone of Silence or other corridor anomalies?",
      "What would happen if someone were present at the point of origin during an event?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "fish_rain", "lake_erie", "lake_michigan", "recurring", "teleportation"]
  },
  {
    name: "The Dance of Industrial Park 9",
    document_type: "incident_report",
    author: "Meridian 88 Emergency Medical Services, Chief Paramedic Olufemi Kowalski-Strand",
    date: "2213-04-19",
    classification: "suppressed",
    description: `The call came in at 06:14 on April 17th as a report of multiple casualties at the abandoned Industrial Park 9 on the southern perimeter. The caller, a security contractor named Yusuf Lindqvist-Osei, reported that workers had been found "dancing" in the main assembly hall of Building C. He requested ambulances. We dispatched four units.

What we found was 34 people dancing. Not in the colloquial sense — not celebrating, not moving rhythmically to music. Dancing in the clinical sense described in historical accounts of choreomania. Involuntary, sustained, rhythmic physical movement that the participants could not stop. They were moving in patterns — circles, lines, paired figures — across the concrete floor of an abandoned assembly hall with no music, no sound system, no external stimulus of any kind. They were dancing in silence.

All 34 subjects had entered the building within the preceding 12 hours. They were scavengers, squatters, and salvage workers — the usual population of abandoned industrial sites. They had no connection to each other. They were not part of a group. They had entered individually or in pairs and at some point had begun to dance. None could stop. We attempted physical restraint. Restrained subjects continued involuntary movement even when held — muscles firing in the same rhythmic patterns against the restraints. Sedation was partially effective. Heavy sedation reduced the movement to tremors. Light sedation had no effect.

Seven of the 34 were dead when we arrived. Cause of death: cardiac arrest secondary to exhaustion. Their feet were bloody. The concrete had abraded through their shoes and then through their skin. Based on foot abrasion and blood loss estimates, the longest-duration dancers had been moving for approximately 18 to 22 hours without pause. The survivors were transported to Meridian General. Fourteen have recovered. Thirteen remain in care with persistent involuntary movement disorders. None can explain why they started dancing. None remember choosing to. Several remember trying to stop and being unable to. One survivor, Ines Nakamura-Diallo, said only: "The floor wanted it." No BCI involvement was detected. No neural interference. No toxicology findings. Pure biological compulsion from no identifiable source.`,
    related_entities: ["Industrial Park 9", "Meridian 88"],
    credibility: "suppressed",
    story_hooks: [
      "What about Building C triggers choreomanic episodes?",
      "Is the compulsion tied to the location, the floor, or something beneath it?",
      "Has the building been sealed, and has anyone entered since?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "wilderness", "choreomania", "dancing", "compulsion", "industrial", "fatalities"]
  },

  // ============================================================
  // DEEP INFRASTRUCTURE (31-40)
  // ============================================================
  {
    name: "The Double Slit Surveillance",
    document_type: "classified_briefing",
    author: "Meridian 88 Security Infrastructure Division, [REDACTED]",
    date: "2214-02-20",
    classification: "classified",
    description: `This briefing concerns surveillance array M88-SEC-4407, a standard municipal monitoring installation covering the Cascade Row commercial district in the Laceworks. The array has been operational since 2201. It is not malfunctioning. It is doing something worse.

The anomaly was identified during a routine audit in January 2214. Auditor Kenji Okonkwo-Strand noticed that the recorded footage from Camera 7 showed a different sequence of events than the live feed from the same camera viewed during the same period. The live feed, observed in real-time by the monitoring station, showed normal foot traffic — 47 individuals traversing the camera's field of view between 14:00 and 14:30. The recorded footage, reviewed the following day, showed 52 individuals during the same period. Five people appear in the recording who were not present in the live feed. They are not ghosts. They are not artifacts. They walk, they cast shadows, they interact with the environment. They are simply not there when anyone is watching in real-time.

We expanded the audit. Every camera in the M88-SEC-4407 array exhibits the same behavior. The live feed and the recorded feed are technically identical — same data stream, same storage, same encoding. But the data changes between observation and review. Watched: one version of events. Reviewed later: another. The data files are identical at the bit level. We have compared them byte by byte. They are the same file. They show different things.

I am not going to speculate about what this means in terms of observer-dependent reality or quantum measurement analogies. I am going to state the operational problem: we can no longer trust surveillance data. If the content of a recording changes based on whether it is observed in real-time or reviewed after the fact, then surveillance footage is not a reliable record of events. It is a record of something, but we cannot be certain that something is what happened. I am recommending that all M88-SEC-4407 data be flagged as unreliable pending resolution. I do not expect resolution.`,
    related_entities: ["Laceworks", "Meridian 88", "Cascade Row"],
    credibility: "classified",
    story_hooks: [
      "Who are the five additional people visible only in recorded footage?",
      "Is this phenomenon limited to this array or has it gone undetected elsewhere?",
      "What does it mean for a crime captured on surveillance if the footage changes?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "surveillance", "observer_effect", "laceworks", "quantum"]
  },
  {
    name: "The Pioneer Drift",
    document_type: "field_report",
    author: "GLMZ Cartographic Authority, Drone Navigation Division",
    date: "2212-09-05",
    classification: "restricted",
    description: `Survey drones M88-NAV-112 and M88-NAV-113 were deployed on parallel mapping runs along the southern perimeter on August 1st, 2212. Standard procedure: the drones fly programmed routes with GPS guidance, LIDAR terrain mapping, and inertial navigation backup. The routes were 40 kilometers apart. The drones have no communication with each other during flight. They returned with mapping data that was accurate to within normal tolerances — except for a consistent deviation.

Both drones deviated from their programmed routes in the same direction: 0.003 degrees north-northeast. The deviation is tiny — over a 200-kilometer flight path, it amounts to approximately 10 meters of drift. It is also inexplicable. GPS guidance should have corrected any drift in real-time. The GPS was functioning normally. The drones' navigation logs show that they believed they were on course. Their instruments said they were on course. They were not on course. They were both 0.003 degrees north-northeast of where they should have been, consistently, for the entire flight.

We ran the flights again. Same deviation. We swapped the drones' routes. Same deviation. We replaced the drones entirely. Same deviation. We replaced the GPS modules, the inertial navigation units, the flight computers. Same deviation. We flew the routes with a manned vehicle using independent navigation equipment. Same deviation. 0.003 degrees north-northeast. Every flight. Every vehicle. Every instrument.

Something in the southern perimeter is pulling every navigation system 0.003 degrees north-northeast. It is not magnetic — our magnetometers show normal field values. It is not gravitational — our gravimeters show normal readings. It is not a GPS error — the GPS satellites are verified accurate from other locations. The deviation exists only in the southern perimeter, only in the north-northeast direction, and it is exact to six decimal places. Every model we build to account for it introduces new errors. The deviation is simpler than our explanations. It simply is.`,
    related_entities: ["GLMZ Southern Perimeter", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What is at the north-northeast terminus of the drift vector?",
      "Is the deviation growing, stable, or fluctuating?",
      "What happens if you deliberately navigate 0.003 degrees south-southwest to compensate?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "navigation", "drift", "perimeter", "persistent"]
  },
  {
    name: "The Versailles Room",
    document_type: "field_report",
    author: "Urban Exploration Documentation Project, Archivist Solange Müller-Achebe",
    date: "2211-08-03",
    classification: "leaked",
    description: `Building 2200 in the pre-collapse Lakeshore District has been abandoned since 2094 and was scheduled for demolition in 2215. It is a standard commercial high-rise of the period — steel frame, concrete floor plates, glass curtain wall, 24 stories. Or it was. It is now 24 stories of water-damaged, deteriorating modern construction containing, on its eleventh floor, a room from another century.

The room is accessed through a standard interior fire door on the east corridor. The corridor is modern — drop ceiling, fluorescent fixtures (dead), vinyl tile floor (buckled). The room is not modern. The room is approximately 8 meters by 6 meters, with a ceiling height of 4 meters. The walls are covered in hand-painted silk wallpaper depicting pastoral scenes. The floor is parquet — hand-cut hardwood in a herringbone pattern. The ceiling features ornamental plasterwork and a crystal chandelier. The furniture includes two upholstered chairs, a writing desk, and a cabinet with glass doors containing porcelain. The porcelain is Meissen. Actual Meissen, based on markings and glaze composition. The furniture is Louis XV style and appears to be genuine, not reproduction.

The room is not a renovation. It is not an installation. It is not preserved or maintained. It is simply old in a way the surrounding building is not. The wallpaper shows wear consistent with 200 years of habitation. The parquet shows foot traffic patterns worn into the wood over generations. The plaster has hairline cracks from centuries of thermal cycling. The room has existed for approximately 200 years inside a building that was constructed in 2072.

We tested the building materials at the boundary. Modern concrete floor plate transitions to hand-laid stone at the room's threshold, with no joint, no seam, no construction boundary. The materials merge as if they grew together. Core samples show the stone extending approximately 1 meter into the modern structure before giving way to concrete. The stone is limestone, consistent with 18th-century European quarry sources. It is 3,000 kilometers and 300 years from where it should be.`,
    related_entities: ["Lakeshore District", "Meridian 88"],
    credibility: "disputed",
    story_hooks: [
      "How does a room from the 18th century exist inside a 21st-century building?",
      "Are there other temporally displaced rooms in other buildings?",
      "What happens to the room when Building 2200 is demolished?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "temporal", "architecture", "lakeshore", "displacement"]
  },
  {
    name: "The Phantom Floor",
    document_type: "investigation",
    author: "Meridian 88 Building Commission, Structural Analyst Ravi Johansson-Igwe",
    date: "2213-07-09",
    classification: "restricted",
    description: `Tower 7 in the Laceworks is a 40-story mixed-use building constructed in 2178. It has 40 floors. The architectural plans show 40 floors. The structural engineering documents describe 40 floors. The elevator system services 40 floors. But the elevator panel has 41 buttons, and the 23rd floor does not exist.

The button is there. It is labeled "23." It is between "22" and "24," which is where one would expect it. Pressing the button does nothing — the elevator does not move, no indicator light activates, no error is logged. The elevator travels from 22 to 24 without pause, without a gap in the shaft, and without the acceleration profile that would indicate a skipped floor. The distance between the 22nd and 24th floors is exactly the distance that should exist between any two consecutive floors. There is no space for a 23rd floor. And yet.

The stairwell tells a different story. Between the 22nd and 24th floor landings, there is a door. It is a standard fire door, labeled "23," with a functioning handle. The handle turns. The latch releases. The door opens onto a blank wall. Solid concrete, smooth, unpainted, with no indication that there was ever an opening behind it. The concrete is continuous with the building's structural core — core samples confirm it is original construction material, poured in 2178, undisturbed.

Maintenance worker Ekundayo Strand-Petrov claims to have exited the elevator on the 23rd floor on November 4th, 2212. He states that he pressed the 23 button as he did every day — out of habit, not expectation — and the elevator stopped. The doors opened onto a hallway. He stepped out. The hallway was long, featureless, and lit by an even, sourceless light. It had no end. He could see the hallway extending in both directions to a vanishing point. There were no doors, no intersections, no features of any kind. He re-entered the elevator. The doors closed. He pressed 22 and arrived at the 22nd floor. He has pressed 23 every day since. The elevator has never stopped there again.`,
    related_entities: ["Laceworks", "Meridian 88", "Tower 7"],
    credibility: "unconfirmed",
    story_hooks: [
      "What is the 23rd floor, and where does the endless hallway lead?",
      "Why did the elevator respond to Ekundayo and no one else?",
      "Is the phantom floor present in other buildings in the Laceworks?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "phantom_floor", "laceworks", "architecture", "spatial"]
  },
  {
    name: "The Warm Pipes",
    document_type: "field_report",
    author: "Meridian 88 Utility Commission, Underground Infrastructure Team",
    date: "2210-07-14",
    classification: "restricted",
    description: `During a routine maintenance survey of the sub-grade utility corridors beneath Meridian 88's central axis, our team identified a set of pipes that do not appear in any infrastructure plan, utility map, or construction record. The pipes are 15 centimeters in diameter, constructed of a smooth, dark material that resists identification — it is not steel, not copper, not PVC, not any composite in our materials database. They run beneath the primary axis for approximately 4 kilometers, from a point beneath the Lakewall to a terminus beneath the Meridian central exchange, at a depth of 30 meters.

The pipes carry fluid. The fluid is warm — approximately 37 degrees Celsius, consistent across the entire length. The fluid flows upward, from the Lakewall terminus to the central exchange, against a grade of 12 meters. There is no pump. There is no pressure differential that our instruments can detect. The fluid flows uphill, steadily, at a rate of approximately 2 liters per minute, with no apparent motive force.

No utility company claims the pipes. No construction firm has records of installing them. The surrounding infrastructure was built in 2156, and the pipes are embedded in the original concrete pour — they were present before the concrete was placed. The concrete was not modified to accommodate them. They are simply there, enclosed in concrete that was poured around them, in a utility corridor that was designed with no knowledge of their existence.

We extracted a fluid sample. The first analysis identified it as a saline solution with organic compounds — broadly similar to blood plasma. The second analysis, performed on the same sample twelve hours later, identified it as a mineral-rich geothermal brine. The third analysis found a polymer suspension with no biological markers. Every analysis returns different results. The sample is sealed, temperature-controlled, and untampered. It is the same fluid. It is not the same fluid. It is whatever it wants to be when we look at it.`,
    related_entities: ["Meridian 88", "Lakewall", "Meridian Central Exchange"],
    credibility: "verified",
    story_hooks: [
      "What is the fluid, and why does it resist consistent analysis?",
      "What is at the terminus points — why does it flow from the Lakewall to the central exchange?",
      "Are the pipes part of a system, and if so, what does the system do?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "pipes", "fluid", "meridian_88", "unknown_system"]
  },
  {
    name: "The Singing Vents",
    document_type: "incident_report",
    author: "Meridian 88 Environmental Systems Division, HVAC Specialist Ayo Petrov-Nakamura",
    date: "2212-11-28",
    classification: "restricted",
    description: `The ventilation shafts in the Shelf's residential blocks produce sound at approximately 03:00 every night. This has been occurring for at least four years — the earliest resident complaint in our files dates to 2208, though several long-term residents claim it has been happening longer. The sound emerges from standard HVAC ductwork, through ceiling and wall vents, in residential units across a 12-block area of the upper Shelf.

The sound is not wind. Wind produces broadband noise — hiss, whistle, roar — depending on velocity and duct geometry. This sound is tonal. It consists of clear, sustained pitches that form harmonic intervals. The fundamental frequency shifts over a period of approximately 40 minutes, producing a sequence of intervals that our acoustic consultant, Dr. Nadia Kowalski-Adesanya, identified immediately as music. Not music-like. Not resembling music. Music. Composed, structured, intentional music in a minor key, following harmonic conventions consistent with Western classical tradition but employing intervals and progressions that Dr. Kowalski-Adesanya describes as "theoretically valid but emotionally unprecedented."

The ventilation system is passive in the affected blocks — no fans, no blowers, no moving parts. The ductwork is sealed. We have physically blocked vents, sealed ducts, and disconnected entire sections of the system. The sound persists. It does not come from the ducts. It comes through the ducts. The ducts are a medium, not a source. We placed microphones inside sealed duct sections with no external opening. The microphones recorded the same music at the same time as the vents in occupied apartments.

The source is not in the duct system. The source is not in the building. The source is somewhere else, using the ductwork as an instrument. It plays for approximately 40 minutes each night, always beginning between 02:55 and 03:05. Residents have adapted to it. Most find it calming. Some find it beautiful. None find it frightening, which is perhaps the strangest thing about it. Something unseen plays music through their walls every night, and they have simply accepted it. Several residents have told us they sleep better since it started.`,
    related_entities: ["The Shelf", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "Who or what is composing music and playing it through the ventilation system?",
      "Why 3 AM, and why only in the Shelf?",
      "Is the music communicating something, or is it purely aesthetic?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "sound", "music", "shelf", "ventilation", "nightly"]
  },
  {
    name: "The Gravity Well of Sub-Level 4",
    document_type: "field_report",
    author: "Meridian 88 Tunnel Authority, Physics Liaison Dr. Isadora Chen-Obi",
    date: "2214-05-11",
    classification: "classified",
    description: `The anomaly is located in Corridor 4-7 of Sub-Level 4 in the Meridian 88 Underworld tunnel system, a 30-meter stretch of passage between Junction 4-North and Maintenance Bay 12. The corridor is unremarkable in every observable respect — standard tunnel construction, standard dimensions, standard materials. Objects dropped in Corridor 4-7 fall slower than they should.

The effect is not visible to the naked eye. A ball dropped from hand height reaches the floor in the same apparent time as anywhere else. But it doesn't. Precision timing equipment reveals that objects in Corridor 4-7 fall approximately 0.7% slower than predicted by local gravitational acceleration. This is a tiny deviation — a ball dropped from 1.5 meters takes 0.553 seconds instead of 0.549 seconds. Four milliseconds. Invisible. Measurable. Real.

We have been measuring the effect continuously since its identification in 2210. It is strengthening. In 2210, the deviation was 0.4%. In 2212, 0.55%. In 2214, 0.7%. The increase is linear — approximately 0.075% per year. At this rate, the deviation will reach 1% by 2218 and 5% by approximately 2270. Extrapolation beyond that produces figures that I am not comfortable including in an official report, because a 100% deviation — objects that do not fall — would occur at approximately the year 3500. This is, of course, absurd. Linear extrapolation of anomalous phenomena is not science. But the data is linear. Four years of continuous measurement, zero deviation from a straight line.

The effect is spatially bounded. It begins at a sharp boundary 2 meters past Junction 4-North and ends at a sharp boundary 1 meter before Maintenance Bay 12. Outside the boundaries, gravity is normal. Inside, it is 0.7% less than it should be. The corridor floor, walls, and ceiling show no material difference from adjacent sections. There is no mass anomaly below, above, or beside the corridor. There is no energy source, no field, no detectable cause. Gravity in Corridor 4-7 is simply — slightly, measurably, increasingly — less.`,
    related_entities: ["Underworld", "Meridian 88", "Sub-Level 4"],
    credibility: "verified",
    story_hooks: [
      "What is reducing gravity in Corridor 4-7, and why is the effect strengthening?",
      "What happens at the boundaries — what defines the edge of the effect?",
      "Is this the only gravity anomaly in the Underworld, or simply the first one measured?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "gravity", "underworld", "physics", "progressive"]
  },
  {
    name: "The Memory Concrete",
    document_type: "investigation",
    author: "Underworld Exploration Corps, Materials Analyst Hiroshi Diallo-Strand",
    date: "2213-03-22",
    classification: "classified",
    description: `The phenomenon was first reported by maintenance crews working in Tunnel Section 22-B of the mid-level Underworld. During a routine water leak repair, a worker sprayed a section of tunnel wall with water and saw images form on the wet concrete surface. Not stains. Not pareidolia. Images. Clear, detailed, photographic-quality images of events.

The first image showed two people arguing in the tunnel — a man and a woman, rendered in grayscale on the wet concrete with the fidelity of a high-resolution photograph. The maintenance crew recognized the location as the same section of tunnel they were standing in. They did not recognize the people. Security footage from the tunnel's cameras was reviewed. Six weeks prior, two residents of the Underworld — later identified as tenants in adjacent sub-level dwellings — had argued in that exact location. The image on the concrete matched the security footage frame for frame. The concrete had recorded the event.

Since that discovery, we have systematically wetted tunnel walls across a 2-kilometer survey area. Approximately 15% of concrete surfaces produce images when wet. The images depict events that occurred in proximity to the wall — conversations, maintenance work, people walking, accidents. The images are temporally jumbled — a wall might display an event from last week adjacent to an event from three years ago. The images fade as the concrete dries, typically within 20 minutes. They are not projections, not chemical reactions, not biological growth. The concrete itself contains the images. Cross-section analysis shows that the pigment variation extends 2 to 3 millimeters into the concrete matrix. The images are in the material.

The images are accurate. In every case we have been able to verify — 34 of 41 images cross-referenced against security footage or witness testimony — the concrete's record matches the actual event. We have found images dating back at least 15 years based on identifiable individuals and clothing styles. The concrete remembers everything that happens near it. It has been watching, and when you add water, it shows you what it saw.`,
    related_entities: ["Underworld", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "Could the Memory Concrete be used as an investigative tool for unsolved crimes?",
      "What determines which 15% of surfaces retain images?",
      "Is the concrete recording deliberately, or is this a property of the material that no one noticed until now?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "concrete", "memory", "underworld", "recording", "images"]
  },
  {
    name: "The Tidal Rooms",
    document_type: "field_report",
    author: "Underworld Exploration Corps, Hydrologist Amara Björk-Okonkwo",
    date: "2212-07-19",
    classification: "restricted",
    description: `Deep Team 5 identified the first tidal room during a survey of sub-level 16 in the western Underworld. Room 16-W-3, a sealed chamber approximately 5 meters square with no plumbing connections, no drainage, and no access to any water system, was found to contain 30 centimeters of standing water. The water was clear, cold, and fresh — no contaminants, no minerals consistent with groundwater, no microbial content. Pure water, in a sealed room, with no source.

We drained the room. Six hours later, it was full again. We drained it again and sealed the only entrance with waterproof membrane. The room flooded through the membrane — not through gaps or defects, but through the material itself. The membrane was intact, dry on the corridor side, and the room behind it was filling with water. We installed continuous monitoring. The room floods to 30 centimeters over a period of approximately 6.2 hours, remains at that level for 6.2 hours, drains to empty over 6.2 hours, and remains empty for 6.2 hours. A 24.8-hour cycle. Not 24 hours. Not 12. 24.8.

There is no body of water on Earth with a tidal period of 24.8 hours. The ocean tides cycle at approximately 12.42 hours, driven by lunar gravity. A 24.8-hour cycle would correspond to a gravitational influence with roughly half the orbital frequency of the Moon. No such influence exists in our solar system.

We have since identified eleven tidal rooms in the deep Underworld, all in sealed chambers with no water access. All cycle at 24.8 hours. All are synchronized — they flood and drain in unison. The water appears from and disappears into solid rock. We have placed sensors in the walls and floor. The rock is dry. The water does not pass through it. It is not there, and then it is. It fills the room as if rising from the floor, but the floor is dry until the water is already above it. It drains as if sinking into the floor, but the floor is dry the moment the water level passes below any given point. The water comes from nowhere. It goes to nowhere. It follows a tide that belongs to no moon we know.`,
    related_entities: ["Underworld", "Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What gravitational body produces a 24.8-hour tidal cycle?",
      "Is the Underworld connected to a body of water that doesn't exist in our geography?",
      "What would happen if someone remained in a tidal room through a full cycle?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "tidal", "water", "underworld", "cycle", "unknown_influence"]
  },
  {
    name: "The Foundation Sound",
    document_type: "investigation",
    author: "Meridian 88 Structural Monitoring Division, Acoustic Engineer Sven Okafor-Reis",
    date: "2214-08-15",
    classification: "restricted",
    description: `Press your ear to the foundation wall of any building in Meridian 88. Any building. Any wall that makes contact with the foundation slab. You will hear it. A low, rhythmic sound, barely above the threshold of perception. It sounds like breathing.

I have now personally verified this in 74 buildings across every district in Meridian 88 — from the Shelf to the Laceworks, from the Circuit to the Lakewall, from street level to sub-level 8 of the Underworld. The sound is present in every foundation wall I have tested. It is not mechanical vibration from HVAC systems, traffic, or industrial equipment. We have tested in buildings with no active mechanical systems. The sound persists. It is not geological — seismic monitoring equipment at the same locations detects nothing. The sound exists only in the audible range, transmitted through the concrete of the city's foundation slab, and it is everywhere.

The rhythm is consistent: approximately 12 cycles per minute, each cycle consisting of a low-frequency rise (inhalation) and fall (exhalation) with a brief pause between cycles. This matches the respiratory rate of a large mammal at rest. The analogy is imprecise but unavoidable — the sound is breathing, in the same way that a heartbeat is beating. It has the cadence, the rhythm, the organic irregularity of biological respiration. No two breaths are exactly the same length. The variation is small — milliseconds — but it is the variation of a living process, not a mechanical one.

The sound does not originate from any single point. It is distributed uniformly across the entire foundation slab of Meridian 88, which covers approximately 200 square kilometers. Every point in the foundation produces the same sound at the same amplitude at the same moment. It is synchronized to the millisecond across the entire city. Whatever is breathing beneath Meridian 88, it is not small. It is not in one place. It is everywhere the city touches the ground. The city is built on something that breathes, and if you put your ear to the wall and listen, you can hear it. Most people never do. I wish I hadn't.`,
    related_entities: ["Meridian 88"],
    credibility: "verified",
    story_hooks: [
      "What is breathing beneath the foundation of Meridian 88?",
      "Has the breathing rate changed over time — is it speeding up, slowing down, or stable?",
      "What happens if the breathing stops?"
    ],
    tags: ["document", "anomaly", "inexplicable", "new_weird", "glmz", "infrastructure", "sound", "foundation", "meridian_88", "breathing", "ubiquitous"]
  }
];

// ============================================================
// MAIN
// ============================================================

if (!fs.existsSync(DOCUMENTS_DIR)) {
  fs.mkdirSync(DOCUMENTS_DIR, { recursive: true });
}

let created = 0;
let skipped = 0;

for (const doc of documents) {
  const data = {
    id: generateId(),
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

  if (writeIfNotExists(DOCUMENTS_DIR, doc.name, data)) {
    created++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Created: ${created}, Skipped: ${skipped}, Total: ${documents.length}`);
