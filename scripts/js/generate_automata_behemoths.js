const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'automata');

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  let trimmed = name.slice(0, 60);
  return trimmed
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 80);
}

const behemoths = [
  {
    name: "Iowan Behemoth — 'The Granary'",
    aliases: ["The Granary", "Silo Walker", "Harvest King"],
    description: "The Granary is a mobile agricultural processing complex that stands roughly fourteen stories tall. Its central body resembles an enormous grain elevator mounted on six articulated legs, each one thick as a highway overpass pylon. Across its hull, corroded hoppers and intake vents cycle endlessly, pulling soil, vegetation, and anything else organic into grinding mechanisms that have operated without pause for decades. The sound it makes — a low, rhythmic threshing — can be heard twenty kilometers away on still nights.\n\nNo one knows what The Granary does with what it harvests. The processed material disappears into internal chambers that no salvage team has ever successfully accessed. Drone surveys suggest the interior contains refinement systems of a complexity that exceeds anything currently manufactured. Some theorize it produces fuel for other Behemoths. Others believe it is stockpiling biomass for a purpose that hasn't yet revealed itself. The machine shows no interest in human settlements unless they happen to be in its path.\n\nThe Granary's route follows the old agricultural belts of Iowa, tracing patterns that roughly correspond to pre-collapse farming districts. It has been observed pausing over the ruins of grain storage facilities, extending massive boring appendages into the earth as if searching for something buried. Whatever it finds — or doesn't find — it eventually moves on, leaving furrows in the landscape deep enough to reroute streams.",
    autonomy_level: "Fully autonomous — no crew, no known control interface",
    dimensions: "42m height, 60m leg span, central hull 25m diameter",
    weight: "Estimated 8,000+ metric tons",
    power_source: "Unknown — no fuel intake observed, no emissions detected",
    locomotion: "Hexapod heavy walker, deliberate pace (~3 km/h)",
    armament: [
      "Massive grinding intake vents (incidental lethality)",
      "Electromagnetic pulse discharge when approached (defensive)",
      "Debris ejection from processing vents at high velocity"
    ],
    sensors: [
      "Subsurface sonar arrays in leg tips",
      "Unknown electromagnetic scanning (causes interference within 2 km)",
      "Atmospheric chemical sampling"
    ],
    countermeasures: "The Granary has no visible weapon systems, but its sheer mass and the electromagnetic interference field it generates make conventional attacks ineffective. Missiles lose guidance within 2 km. The few attempts to breach its hull with cutting charges found armor plating of unknown alloy over a meter thick.",
    known_deployments: [
      "Iowa agricultural corridor — continuous since at least 2161",
      "Brief incursion into Nebraska border zone (2179)",
      "Observed pausing over Des Moines ruins for 72 hours (2183)"
    ],
    story_hooks: [
      "The Granary has changed its route for the first time in twenty years, heading directly toward a settlement. The residents have 48 hours to evacuate or find a way to divert a 8,000-ton walking factory.",
      "A salvage crew claims to have found a hatch on The Granary that opened for them. They went inside. Only one came back, and she won't speak about what she saw."
    ],
    cultural_context: "The Granary is the most 'benign' of the Iowan Behemoths — it generally ignores humans unless they're in its path. Wasteland communities have learned its route and built settlements just outside its corridor. Some even worship it as a harvest deity, leaving offerings of grain at points along its route.",
    tags: ["automaton", "behemoth", "iowan", "harvester", "mystery", "colossal", "wasteland", "hazard"]
  },
  {
    name: "Iowan Behemoth — 'Cathedral'",
    aliases: ["Cathedral", "The Church", "God's Feet"],
    description: "Cathedral earned its name from the towering spires that crown its upper hull, rising nearly twenty stories above the prairie. The machine walks on four massive pillar-legs, each one a complex assembly of hydraulic pistons and armored housings that slam into the earth with enough force to register on seismographs three hundred kilometers away. Between the spires, arrays of antenna-like structures broadcast continuous signals on frequencies that no known receiver can decode. The transmissions have been ongoing since before anyone alive can remember.\n\nThe machine's central body is a cathedral-sized hall of exposed machinery, visible through gaps in corroded armor plating where repair systems haven't quite kept up with decades of weathering. Through these gaps, observers with telescopic equipment have reported seeing rows of manufacturing arms assembling components on internal conveyor systems — building things, endlessly, that never seem to leave the machine. Some speculate Cathedral is constructing something inside itself, adding to its own complexity with each passing year. Structural analysis suggests the machine is measurably larger now than it was in photographs from 2165.\n\nCathedral's broadcast signal is its most unsettling feature. Every automaton within reception range — commercial, military, industrial — experiences behavioral anomalies when Cathedral passes. Security drones deviate from patrol routes. Factory robots pause mid-task. Service automata turn to face the signal source. The effect is temporary and poorly understood, but it has led to Cathedral being classified as a priority avoidance target by every corponation operating in the region.",
    autonomy_level: "Fully autonomous — broadcasts unknown command signals",
    dimensions: "58m to spire tops, 35m hull width, leg stride 20m",
    weight: "Estimated 12,000+ metric tons",
    power_source: "Unknown — thermal signature suggests internal fusion or radioisotope",
    locomotion: "Quadruped heavy walker, ground-shaking stride (~2 km/h)",
    armament: [
      "Signal broadcast disrupts nearby automata control systems",
      "Spire-mounted directed energy emitters (observed twice, effect unknown)",
      "Self-repair drones swarm aggressors (hundreds of small units)"
    ],
    sensors: [
      "Broadband signal array spanning most known frequencies",
      "Self-repair drones serve as mobile sensor platforms",
      "Seismic detection through leg contact"
    ],
    countermeasures: "Cathedral's signal disruption field makes electronic warfare and guided weapons unreliable within 5 km. Its self-repair drone swarm aggressively intercepts approaching aircraft and missiles. The only confirmed damage to Cathedral was inflicted by a pre-collapse artillery battery that scored a direct hit on a leg joint — the leg was fully repaired within 96 hours.",
    known_deployments: [
      "Central Iowa broadcast corridor — continuous roaming",
      "Approached GLMZ perimeter in 2177 — turned away before engagement",
      "Signal detected as far as Kansas City and Minneapolis"
    ],
    story_hooks: [
      "A character's cybernetic implants begin picking up fragments of Cathedral's signal — snippets that almost sound like language. The fragments are getting clearer the closer Cathedral gets to the city.",
      "Arcturus is secretly funding an expedition to board Cathedral and locate its signal source. They need expendable operatives who won't be missed."
    ],
    cultural_context: "Cathedral is the most feared of the Behemoths because of its apparent ability to influence other machines. Anti-automaton movements cite it as proof that machines have a 'will' independent of programming. The corponations publicly dismiss this as anthropomorphization while privately spending billions trying to decode its signal.",
    tags: ["automaton", "behemoth", "iowan", "signal", "mystery", "colossal", "wasteland", "hazard", "electronic warfare"]
  },
  {
    name: "Iowan Behemoth — 'Ironclad'",
    aliases: ["Ironclad", "The Fortress", "Walking Wall"],
    description: "Ironclad is a mobile fortress — a squat, massively armored platform that moves on eight short, wide legs like an enormous mechanical crab. Its profile is low compared to other Behemoths, standing only six stories tall, but it stretches over 80 meters long and 50 meters wide. The surface is covered in overlapping armor plates of varying age and origin, some clearly scavenged from other machines, buildings, and vehicles. Ironclad has been adding to its own armor for decades, welding new layers onto itself from whatever it encounters.\n\nThe machine's interior — glimpsed through rare breaches in its ever-thickening shell — appears to be a vast manufacturing space where robotic arms strip salvage and integrate it into the hull. Ironclad actively seeks out wreckage, ruins, and abandoned infrastructure, pausing over them for days while extending articulated crane-arms to harvest material. It has been observed dismantling entire highway overpasses, pulling rebar and concrete into itself and emerging slightly larger, slightly heavier, slightly more armored than before.\n\nWhat makes Ironclad particularly terrifying is that it is the only Behemoth confirmed to respond to aggression with deliberate, targeted force. In 2180, a mercenary company hired by Tessera Dynamics attempted to breach its hull with shaped charges. Ironclad sealed the breach within minutes and then spent three days systematically hunting the mercenary team across forty kilometers of wasteland, crushing their vehicles and equipment with precise leg strikes. There were no survivors. Since that incident, a standing advisory warns all corporate and independent forces: do not engage Ironclad.",
    autonomy_level: "Fully autonomous — retaliatory combat behavior confirmed",
    dimensions: "18m height, 80m length, 50m width",
    weight: "Estimated 20,000+ metric tons (increasing)",
    power_source: "Unknown — possibly multiple redundant systems",
    locomotion: "Octapod heavy crawler, slow but unstoppable (~1.5 km/h)",
    armament: [
      "Massive articulated crane-arms (repurposed as crushing weapons)",
      "Hull-mounted point defense guns (caliber unknown, automated)",
      "Leg strikes capable of destroying armored vehicles",
      "Debris launchers — fires scrap metal at ballistic velocity"
    ],
    sensors: [
      "Distributed sensor mesh across hull surface",
      "Seismic tremor detection",
      "Thermal imaging arrays on articulated stalks"
    ],
    countermeasures: "Ironclad's ever-increasing armor makes it effectively impervious to conventional weapons. Its low profile and massive footprint make it resistant to toppling. The only known vulnerability is speed — it moves slowly enough that anything mobile can simply leave. The danger is getting caught near it when it decides something is a threat.",
    known_deployments: [
      "Eastern Iowa wasteland — slow patrol circuit",
      "Observed absorbing ruins of Cedar Rapids industrial district (2176–2178)",
      "Tessera mercenary engagement and pursuit (2180)"
    ],
    story_hooks: [
      "Ironclad's patrol route has shifted to intersect a major trade road. Caravans are being forced into longer, more dangerous detours through territory claimed by wasteland gangs. Someone is profiting from this, and the timing is suspicious.",
      "Buried inside Ironclad's accumulated armor is a pre-collapse military vehicle with encrypted data cores that multiple factions desperately want. Getting to it means somehow getting inside a machine that kills anything that touches it."
    ],
    cultural_context: "Ironclad represents the ultimate expression of machine self-preservation — a thing that exists only to make itself harder to destroy. Wasteland philosophers debate whether this constitutes a survival instinct or merely an optimization loop. The practical consensus is that the distinction doesn't matter when you're standing in front of it.",
    tags: ["automaton", "behemoth", "iowan", "fortress", "armor", "colossal", "wasteland", "hazard", "retaliatory"]
  },
  {
    name: "Iowan Behemoth — 'Thunderhead'",
    aliases: ["Thunderhead", "Storm Caller", "The Cloud"],
    description: "Thunderhead is the tallest confirmed Behemoth, a spindly tripod structure that rises nearly 30 stories into the sky. Its three legs are impossibly thin for its height — each one a lattice of carbon-fiber and unknown composites that flex visibly in high winds but never break. At the apex, a bulbous sensor dome bristles with antennae, radar dishes, and atmospheric sampling equipment that gives the machine the appearance of a walking weather station. Which, in a sense, it is.\n\nThunderhead generates its own weather. The electromagnetic systems in its dome interact with atmospheric conditions to produce localized storm cells — clouds form around its upper structures, lightning arcs between its antennae, and rain falls in its immediate vicinity regardless of regional weather patterns. The storms intensify when the machine is stationary and dissipate when it moves. Meteorologists who have studied the phenomenon believe Thunderhead is actively manipulating ionospheric charge layers, though the purpose is debated. Some believe it is harvesting atmospheric electricity for power. Others think the weather modification is a side effect of whatever it's actually doing up there.\n\nThe machine moves in long, deliberate strides across the prairie, each step covering fifty meters. Its passage leaves behind soil that is measurably more fertile than the surrounding wasteland — the combination of electrical discharge and rainfall appears to revitalize dead earth. Wasteland farming communities have learned to follow in Thunderhead's wake, planting crops in the rejuvenated soil. This has led to a strange symbiosis: human settlements that migrate with a machine that doesn't know or care they exist.",
    autonomy_level: "Fully autonomous — no hostile behavior observed",
    dimensions: "90m height, 50m stride, dome diameter 15m",
    weight: "Estimated 3,000 metric tons (lightweight for its size)",
    power_source: "Atmospheric electrical harvesting — confirmed by observation",
    locomotion: "Tripod walker, long stride, surprisingly fast (~8 km/h)",
    armament: [
      "Lightning discharge (unclear if intentional or defensive)",
      "Intense electromagnetic field disrupts electronics within 1 km",
      "Storm generation — hail, wind, and electrical discharge in vicinity"
    ],
    sensors: [
      "Atmospheric sampling array (dome)",
      "Long-range radar (estimated 200 km range)",
      "Ionospheric charge monitoring",
      "Seismic sensors in leg bases"
    ],
    countermeasures: "Thunderhead's height and the perpetual storm surrounding it make aerial approach extremely dangerous. The electromagnetic interference field disables most guided weapons. However, Thunderhead has never been observed retaliating against attack — it simply walks away, faster than most ground vehicles can follow across wasteland terrain.",
    known_deployments: [
      "Northern Iowa to southern Minnesota corridor",
      "Observed standing stationary over aquifer locations for weeks at a time",
      "Storm effects detected by weather satellites across three states"
    ],
    story_hooks: [
      "Thunderhead has stopped moving for the first time in recorded history. It's standing over a specific point in the wasteland, generating an increasingly violent storm. Something is buried there, and Thunderhead is either trying to unearth it or charge it.",
      "A wasteland farming community that follows Thunderhead is being pressured by a corponation to allow sensor equipment to be placed in their settlement. The farmers don't want corporate attention, but the corp is threatening to divert Thunderhead's route with EMP weaponry."
    ],
    cultural_context: "Thunderhead is the Behemoth most integrated into wasteland life. The communities that follow it have developed entire cultures around its movements — seasonal calendars based on its route, rituals when it pauses, songs about the rain it brings. To them it is not a machine but a force of nature, and the distinction is academic.",
    tags: ["automaton", "behemoth", "iowan", "weather", "tripod", "colossal", "wasteland", "atmospheric", "symbiosis"]
  },
  {
    name: "Iowan Behemoth — 'The Foundry'",
    aliases: ["The Foundry", "Smokestack", "Iron Mother"],
    description: "The Foundry is a walking factory. There is no more precise description. It stands ten stories tall on four thick, piston-driven legs, and its body is an industrial complex in miniature — smokestacks venting superheated exhaust, conveyor systems visible through open bays, and a constant rain of sparks from internal welding operations. The machine smelts ore, processes raw materials, and manufactures components in a continuous cycle that has been running for longer than most people have been alive. What it manufactures is the question that keeps corponation intelligence analysts awake at night.\n\nThe Foundry produces smaller automata. This has been confirmed by multiple observation teams across decades. Roughly once every three to four months, a hatch on the Foundry's underside opens and a newly constructed machine drops to the ground, activates, and walks away. These offspring are varied in design — some are repair drones that service other Behemoths, some are mining units that dig materials and deposit them in the Foundry's path, and some are of entirely unknown purpose and have never been seen again after deployment. The Foundry is, in effect, a self-sustaining autonomous factory that builds whatever it determines is needed.\n\nThe implications of the Foundry's existence are staggering. It represents a manufacturing capability that operates without human input, without supply chains, without corporate infrastructure. It mines its own materials, refines them, and builds machines to an engineering standard that rivals or exceeds current corponation output. Several factions have attempted to capture it. All have failed. The Foundry does not fight — it simply deploys its offspring as a screen and walks away while they delay pursuit.",
    autonomy_level: "Fully autonomous — manufactures other automata",
    dimensions: "30m height, 40m length, 25m width",
    weight: "Estimated 15,000 metric tons",
    power_source: "Internal smelting furnace — consumes raw ore for thermal and electrical energy",
    locomotion: "Quadruped industrial walker, heavy gait (~2 km/h)",
    armament: [
      "Deploys manufactured automata as defensive screen",
      "Superheated slag ejection (industrial byproduct, weaponized)",
      "Exhaust venting capable of flash-burning anything within 30m"
    ],
    sensors: [
      "Geological survey sensors (identifies ore deposits)",
      "Mining drone telemetry network",
      "Thermal imaging",
      "Subsurface mineral detection"
    ],
    countermeasures: "The Foundry's primary defense is its offspring. When threatened, it deploys between six and twenty smaller automata that aggressively engage any pursuer. The Foundry itself has no confirmed weapons systems — its dangers are industrial byproducts rather than deliberate armaments. The real countermeasure is that destroying the Foundry would eliminate the only source of the repair drones that service other Behemoths.",
    known_deployments: [
      "Western Iowa mining corridor",
      "Observed excavating open-pit mines autonomously (multiple locations)",
      "Offspring units sighted as far as Oklahoma and Wisconsin"
    ],
    story_hooks: [
      "The Foundry has just produced something different — a humanoid automaton of sophisticated design that walked into a wasteland settlement and stood in the town square for six hours before walking away. No one knows what it was doing, but it appeared to be observing.",
      "A corponation wants to capture one of the Foundry's freshly deployed offspring before it activates. They need operatives fast enough to grab it in the seconds between deployment and boot-up."
    ],
    cultural_context: "The Foundry is the most politically significant Behemoth. Its existence proves that autonomous manufacturing at industrial scale is possible without corporate control. This makes it simultaneously the greatest prize and the greatest threat in the eyes of every corponation. If its methods could be replicated, the entire corporate manufacturing monopoly would collapse.",
    tags: ["automaton", "behemoth", "iowan", "factory", "manufacturing", "colossal", "wasteland", "hazard", "self-replicating"]
  },
  {
    name: "Iowan Behemoth — 'Palisade'",
    aliases: ["Palisade", "The Wall", "Flatline"],
    description: "Palisade defies the typical Behemoth profile. It is not tall — it stands barely four stories high. But it is enormous in footprint, a flat, rectangular platform over 200 meters long and 100 meters wide, crawling on hundreds of small, independently articulated legs like a mechanical centipede the size of a city block. Its upper surface is a featureless expanse of armored plating broken only by regularly spaced turret housings, most of which appear inactive. Palisade moves across the wasteland like a mobile airfield, its passage flattening everything in its path into a smooth, compressed road surface.\n\nThe roads Palisade leaves behind are its most notable feature. Wherever it travels, it deposits a layer of compressed aggregate and bonding agent that creates a durable, flat roadway — a highway built by a machine for machines. These roads connect points of apparent significance: old military installations, mineral deposits, locations where other Behemoths frequently pause. The network has been growing for decades, and satellite mapping reveals a pattern that suggests deliberate infrastructure development. Palisade is building a road system across the wasteland, and no one asked it to.\n\nHuman communities have begun using Palisade's roads for trade routes, which presents a dilemma. The roads are the best-maintained infrastructure in the wasteland, but they also attract other Behemoths. Palisade's routes seem designed to facilitate machine traffic, and the other Behemoths use them. A settlement built along a Palisade road gets excellent trade access and a front-row seat to regular Behemoth passage — a trade-off that different communities evaluate very differently.",
    autonomy_level: "Fully autonomous — infrastructure construction behavior",
    dimensions: "12m height, 200m length, 100m width",
    weight: "Estimated 40,000+ metric tons",
    power_source: "Unknown — internal material processing suggests thermal conversion",
    locomotion: "Myriapod crawler, hundreds of small articulated legs (~4 km/h)",
    armament: [
      "Turret housings (mostly inactive, 6 confirmed operational with rotary cannons)",
      "Sheer mass — anything in its path is crushed and incorporated",
      "Road-building machinery repurposed as grinding/crushing systems"
    ],
    sensors: [
      "Ground-penetrating radar (surveys terrain ahead of path)",
      "GPS-equivalent positioning (unknown satellite network)",
      "Material composition analysis in road-building systems"
    ],
    countermeasures: "Palisade is essentially unkillable through conventional means — its distributed leg system means losing dozens of legs barely affects its movement, and its flat profile presents minimal target area. Its mass makes it immune to anything short of nuclear weapons. The practical approach is simply not being in its path, which is predictable from its road network.",
    known_deployments: [
      "Entire Iowa wasteland — road network spans 3,000+ km",
      "Roads extend into Nebraska, Illinois, Missouri, and Minnesota",
      "Has been observed building road toward GLMZ (currently 40 km out)"
    ],
    story_hooks: [
      "Palisade's road is approaching a major city. The road leads directly to the city wall. Municipal authorities are debating whether to reinforce the wall or open a gate — because the trade opportunities of a Behemoth-grade highway are enormous, but so are the risks.",
      "Someone has discovered that Palisade's roads contain embedded data conduits — fiber optic cables woven into the aggregate. The roads aren't just roads. They're a network. And something is already using it."
    ],
    cultural_context: "Palisade has reshaped wasteland geography more than any other single force. Its road network defines trade routes, settlement locations, and political boundaries. Maps of the wasteland are increasingly organized around Palisade roads. Some scholars argue that Palisade is terraforming — rebuilding infrastructure for a civilization that doesn't exist yet.",
    tags: ["automaton", "behemoth", "iowan", "infrastructure", "road-builder", "colossal", "wasteland", "myriapod", "network"]
  },
  {
    name: "Iowan Behemoth — 'Revenant'",
    aliases: ["Revenant", "The Graveyard Walker", "Bonepicker"],
    description: "Revenant is a Behemoth that collects other machines. It walks on six legs through the wasteland, and slung beneath its central hull is a vast cargo bay filled with the carcasses of destroyed automata — military drones, industrial robots, security units, even pieces of other Behemoths. Revenant does not fight. It arrives after fights. It has been observed waiting at the periphery of battles, motionless and patient, until the shooting stops. Then it moves in and begins collecting the dead machines.\n\nThe collection process is methodical and almost reverent. Revenant's underside deploys dozens of articulated arms that carefully disassemble wreckage, sorting components with a precision that suggests deep understanding of every machine it encounters. Useful parts are stored in categorized bays. Damaged components are broken down further. Nothing is wasted. The arms work with a delicacy that seems impossible for a machine this size — witnesses describe it as 'surgical' and 'gentle,' words that are unsettling when applied to a seven-story walking scrapyard.\n\nWhat Revenant does with its collection is unknown. It never deploys the parts. It never sells them. It never delivers them to the Foundry or any other Behemoth. It simply... keeps them. Thermal imaging shows that some of the stored machines are maintained in partial operational states — powered on but not moving, as if preserved. This has led to the most disturbing theory about Revenant: that it is not collecting scrap, but maintaining a museum. Or an ark.",
    autonomy_level: "Fully autonomous — collection/preservation behavior",
    dimensions: "22m height, 55m length, 30m width (cargo bay extends below hull)",
    weight: "Estimated 10,000 metric tons (varies with cargo)",
    power_source: "Unknown — possibly harvests power cells from collected machines",
    locomotion: "Hexapod walker, patient gait (~3 km/h)",
    armament: [
      "None confirmed — Revenant has never engaged in combat",
      "Collection arms could theoretically crush vehicles",
      "Carries functional weapons salvaged from collected machines (unused)"
    ],
    sensors: [
      "Long-range acoustic monitoring (detects combat sounds)",
      "Electromagnetic spectrum analysis (detects active machines)",
      "Detailed component-level scanning in collection arms"
    ],
    countermeasures: "Revenant has never fought back against anything. Its only defense is that no faction has found a reason to attack it — and a superstitious reluctance to destroy a machine that collects the dead. Wasteland culture considers attacking Revenant to be profoundly bad luck, a belief that has proven more effective than armor.",
    known_deployments: [
      "Follows conflict zones across Iowa and neighboring states",
      "Observed at aftermath of Tessera-Arcturus border skirmish (2182)",
      "Regularly visits sites of old battles, even decades-old ones"
    ],
    story_hooks: [
      "Revenant has collected the remains of a prototype automaton that a corponation will do anything to recover. The machine is somewhere inside Revenant's cargo bay — but no one has ever gone inside Revenant and come back to describe the interior.",
      "A dead operative's cybernetic arm was collected by Revenant along with battlefield wreckage. The arm contains encrypted data that could expose a corporate conspiracy. The operative's partner wants it back."
    ],
    cultural_context: "Revenant has inspired an entire folklore tradition in the wasteland. It is spoken of as a psychopomp — a guide for dead machines. Scavengers who work battlefields leave Revenant's share and take only what it hasn't claimed. Some communities hold funerals for destroyed automata by leaving them in Revenant's path. The machine has become, without intention or awareness, a religious figure.",
    tags: ["automaton", "behemoth", "iowan", "collector", "salvage", "colossal", "wasteland", "mystery", "nonviolent"]
  },
  {
    name: "Iowan Behemoth — 'Meridian'",
    aliases: ["Meridian", "The Compass", "North Walker"],
    description: "Meridian walks in a straight line. This is the defining and most unnerving characteristic of a machine that has been doing nothing but walking north-south across the Iowa wasteland for as long as anyone has documented the Behemoths. Its path is precise to within two meters of an exact meridian line — not magnetic north, not grid north, but true geodetic north, as if it is calibrated to the planet's rotational axis. It walks south until it reaches a point near the Missouri border, then turns around and walks north to roughly the Minnesota line. Then it turns around again. The cycle takes approximately four months.\n\nMeridian is bipedal — the only Behemoth that walks on two legs. It stands fifteen stories tall and has a roughly humanoid silhouette, which makes it the most psychologically disturbing of the Behemoths to encounter. Its 'head' is a featureless dome of sensor equipment that rotates slowly and continuously, scanning the horizon. Its 'arms' hang at its sides, swinging with its stride, and they end in complex manipulator clusters that have never been observed doing anything. The machine walks with a steady, mechanical rhythm that never varies — one step every 4.2 seconds, day and night, rain or shine, year after year.\n\nThe mystery of Meridian is its purpose. It doesn't harvest. It doesn't build. It doesn't manufacture. It doesn't collect. It just walks, measuring something with every step. Seismologists have noted that Meridian's footfalls generate precise, regular vibrations that penetrate deep into the earth — and that these vibrations are measurably different from random impacts. Some believe Meridian is conducting a geological survey. Others think it is a timing mechanism — a pendulum. A few suggest it is a message, written in footsteps on the skin of the earth, for a recipient that hasn't arrived yet.",
    autonomy_level: "Fully autonomous — single-purpose locomotion pattern",
    dimensions: "45m height, bipedal stance 12m wide, stride 15m",
    weight: "Estimated 6,000 metric tons",
    power_source: "Unknown — kinetic energy recovery from footfalls theorized",
    locomotion: "Bipedal walker, precise metronomic stride (~4 km/h, never varies)",
    armament: [
      "None observed — Meridian has never displayed hostile behavior",
      "Manipulator arms of unknown capability (never used)",
      "Footfalls can cause localized seismic damage to nearby structures"
    ],
    sensors: [
      "Rotating sensor dome (full spectrum scanning)",
      "Seismic measurement through foot contact",
      "Gravitometric sensors (theorized)",
      "Deep-earth resonance detection"
    ],
    countermeasures: "Meridian ignores everything. Weapons fire, attempts to block its path, even placing obstacles on its route — it walks through or over all of it without changing pace. The one attempt to topple it with explosive charges at the knee joint failed; the joint design includes redundant hydraulic systems that compensated instantly. Meridian didn't even slow down.",
    known_deployments: [
      "Single meridian line across Iowa — Missouri border to Minnesota border",
      "Route has not deviated by more than 2 meters in 25 years of observation",
      "Footfall vibrations detected by seismic stations as far as Colorado"
    ],
    story_hooks: [
      "Meridian has stopped. For the first time ever, it has stopped walking and is standing motionless at a precise point on its route. The ground beneath it has begun to vibrate at a frequency that is cracking foundations in settlements thirty kilometers away. Something is responding from underground.",
      "A geologist has mapped Meridian's footfall vibration pattern and believes it encodes spatial coordinates — coordinates that point to a location deep beneath the Iowa bedrock. She needs funding and protection to investigate."
    ],
    cultural_context: "Meridian is the most philosophically troubling Behemoth. Its apparent purposelessness invites projection — people see in it whatever they need to see. A clock. A prayer. A warning. A map. The wasteland saying 'steady as Meridian' means reliable to the point of being uncanny.",
    tags: ["automaton", "behemoth", "iowan", "bipedal", "survey", "colossal", "wasteland", "mystery", "metronomic"]
  },
  {
    name: "Iowan Behemoth — 'Leviathan'",
    aliases: ["Leviathan", "The Swimmer", "Dirt Whale"],
    description: "Leviathan does not walk on the surface. It moves beneath it. The machine is a subterranean borer of staggering proportions — estimated at over 150 meters long and 20 meters in diameter, it tunnels through the earth at depths ranging from 30 to 200 meters. Its passage is detected by the surface effects: a rolling wave of displaced earth, collapsed terrain, and the deep subsonic rumbling that gives nearby settlements minutes of warning before the ground begins to buckle. Leviathan has been responsible for more infrastructure damage than any other Behemoth.\n\nThe tunnels Leviathan leaves behind are structurally sound — reinforced by a secreted mineral compound that hardens into something resembling concrete. The tunnel network beneath Iowa is extensive, and growing. Some sections have been explored and mapped; they connect Behemoth gathering points, mineral deposits, and underground water sources in a pattern that suggests deliberate infrastructure. Palisade builds roads on the surface. Leviathan builds them underground. The two networks intersect at specific points where vertical shafts connect tunnel to road, though no one has observed either machine acknowledging the other.\n\nLeviathan surfaces rarely and unpredictably. When it does, the event is catastrophic — a section of ground the size of a football field erupts upward as the machine's boring head breaches the surface, venting heat and pulverized rock in a geyser of debris. It remains partially surfaced for hours, its upper hull exposed to the sky, before submerging again. During these surfacing events, the exposed hull radiates enough heat to be visible on infrared satellites. No one knows why it surfaces. No one wants to be nearby when it does.",
    autonomy_level: "Fully autonomous — subterranean tunneling behavior",
    dimensions: "Estimated 150m+ length, 20m diameter",
    weight: "Estimated 25,000+ metric tons",
    power_source: "Geothermal tap — harvests heat from deep earth",
    locomotion: "Subterranean boring, surface displacement wave (~6 km/h underground)",
    armament: [
      "Boring head capable of destroying anything in its path underground",
      "Surface eruption event — catastrophic area destruction",
      "Superheated rock ejection during surfacing",
      "Tunnel collapse (deliberate or incidental) destroys surface structures"
    ],
    sensors: [
      "Seismic array (360-degree subsurface mapping)",
      "Thermal gradient detection",
      "Mineral composition analysis",
      "Gravitometric density scanning"
    ],
    countermeasures: "Leviathan is effectively untouchable. It operates underground, beyond the reach of surface weapons. The few attempts to attack it during surfacing events have been thwarted by the extreme heat and debris field. Theoretically, a deep-penetration bunker buster could reach it at shallow depths, but no faction has been willing to deploy that level of ordnance in the wasteland.",
    known_deployments: [
      "Entire Iowa subsurface — tunnel network spans estimated 2,000 km",
      "Surface eruptions documented 14 times since 2160",
      "Tunnels confirmed extending into Missouri, Illinois, and Wisconsin"
    ],
    story_hooks: [
      "Leviathan's tunnel has intersected the foundation of a settlement's water processing plant. The tunnel is structurally sound and leads somewhere interesting — but entering it means traveling through Leviathan's domain, and the machine could return at any time.",
      "Seismographs show Leviathan is boring directly toward the underground infrastructure of a major city. At current speed, it will breach the city's sublevel in six days. The city has no weapon that can reach it."
    ],
    cultural_context: "Leviathan is the invisible threat — the one you can't see coming until the ground starts moving. Wasteland construction always includes seismic monitors, and 'Leviathan drills' — emergency evacuations triggered by subsonic rumbling — are practiced in every settlement along its known routes. Children are taught to recognize the sound before they learn to read.",
    tags: ["automaton", "behemoth", "iowan", "subterranean", "borer", "colossal", "wasteland", "hazard", "tunneling"]
  },
  {
    name: "Iowan Behemoth — 'Obelisk'",
    aliases: ["Obelisk", "The Needle", "Stillpoint"],
    description: "Obelisk does not move. It is the only Behemoth that has never been observed in motion. It stands in the exact geographic center of Iowa — a perfect vertical spike of dark metal rising 120 meters from a base embedded deep in the bedrock. Its surface is seamless, without visible joints, hatches, or sensor arrays. It reflects no radar, absorbs most visible light, and maintains a surface temperature exactly matching the ambient air at all times, making it nearly invisible to thermal imaging. From a distance it looks like a shadow with no source.\n\nObelisk's classification as a Behemoth is debated. It has no legs, no locomotion, no apparent function. But it shares characteristics with other Behemoths: self-repair capability (scratches on its surface heal within hours), unknown power source, and pre-collapse origin. Most compellingly, every other Behemoth in Iowa has been observed traveling to Obelisk's location at irregular intervals. They arrive, stand near it for hours or days, and then resume their normal patterns. Palisade's road network converges on it. Leviathan's tunnels spiral around it. Cathedral's signal is strongest when facing it. Obelisk appears to be the hub of whatever the Behemoths are collectively doing.\n\nThe area within one kilometer of Obelisk is anomalous. Electronic equipment malfunctions. Compass needles point toward it instead of north. People who spend more than a few hours in its proximity report vivid dreams about machinery and geometric patterns. The ground around its base vibrates at a frequency just below human hearing, producing a persistent sense of unease. Three research expeditions have attempted to study Obelisk up close. All three abandoned their missions within 48 hours, citing equipment failure and 'psychological distress.' Their sensor data was corrupted beyond recovery.",
    autonomy_level: "Unknown — stationary, purpose unknown",
    dimensions: "120m height, 8m base width, tapers to 2m at apex",
    weight: "Unknown — embedded in bedrock to unknown depth",
    power_source: "Unknown — no emissions, no thermal signature, no energy consumption detected",
    locomotion: "None — stationary",
    armament: [
      "None confirmed",
      "Proximity effects cause equipment failure and psychological distress",
      "Electromagnetic anomaly field (1 km radius)"
    ],
    sensors: [
      "Unknown — no visible sensor systems",
      "Appears to be aware of approaching entities (Behemoths change behavior near it)",
      "Possible deep-earth monitoring through bedrock connection"
    ],
    countermeasures: "Obelisk has never needed countermeasures because no one has successfully mounted an attack. Equipment fails near it. Targeting systems lose lock. Even manually aimed weapons seem to miss, though this may be observer bias combined with the psychological effects of proximity. The more practical countermeasure is that attacking the thing every other Behemoth visits seems profoundly unwise.",
    known_deployments: [
      "Geographic center of Iowa — has never moved",
      "All Behemoth paths converge on its location",
      "Anomalous zone documented since first surveys in 2155"
    ],
    story_hooks: [
      "Obelisk has started humming. The subsonic vibration that was previously just below perception has increased to an audible frequency, and it's getting louder. Behemoths across the state are changing their routes, converging on Obelisk's location. Something is about to happen.",
      "A dying engineer claims to have been part of the team that built Obelisk — decades before the Behemoths appeared. She says it's a key, and someone is about to turn it. She needs to reach it before they do."
    ],
    cultural_context: "Obelisk is the dark heart of Behemoth mythology. Every theory about what the Behemoths are and what they want eventually circles back to Obelisk. It is the closest thing the wasteland has to a holy site — a place of pilgrimage for those who worship the machines, a place of dread for those who fear them, and a place of obsessive interest for those who study them. No one is neutral about Obelisk.",
    tags: ["automaton", "behemoth", "iowan", "stationary", "anomaly", "colossal", "wasteland", "mystery", "hub", "nexus"]
  }
];

const trainingAutomata = [
  {
    name: "Kang Athletics KA-200 'Padwork'",
    aliases: ["Padwork", "The Mitt Machine", "Two Hundred"],
    manufacturer: "KANG ATHLETICS",
    description: "The KA-200 is a beginner-level striking trainer designed for boxing gyms and personal fitness studios. It stands at average human height on a wheeled base and presents two padded striking surfaces mounted on articulated arms that mimic a coach holding focus mitts. The machine calls out combinations through a built-in speaker — jab, cross, hook, uppercut — and adjusts pad position to match the user's height and reach. Impact sensors measure force, speed, and accuracy, displaying real-time metrics on a chest-mounted screen.\n\nPadwork is intentionally non-threatening. Its chassis is rounded, its movements are slow and predictable, and it cannot strike back. The arms absorb impact rather than resist it, and the machine rolls backward slightly with heavy hits to simulate realistic pad response. Voice prompts are encouraging and patient, cycling through multiple coach personalities from drill-sergeant to meditation-guru depending on user preference. It is, fundamentally, a punching bag that talks and moves.\n\nThe KA-200 dominates the consumer fitness market because it requires no training partner and no gym membership. Home units outsell commercial models three to one. The machine has been credited with a measurable increase in striking fitness across the general population — and a corresponding increase in the effectiveness of bar fights, which Kang Athletics does not include in its marketing materials.",
    classification: "Training",
    tier_availability: "Tier 1",
    legality: "Consumer — unrestricted sale",
    autonomy_level: "Reactive — responds to user strikes, no independent action",
    dimensions: "1.8m height, 0.6m width, wheeled base",
    weight: "85 kg",
    power_source: "Rechargeable battery, 8-hour continuous use",
    locomotion: "Wheeled base, limited mobility (rolls to absorb strikes)",
    armament: [],
    sensors: ["Impact force sensors in pads", "Motion tracking camera (user form analysis)", "Heart rate detection (contactless)"],
    countermeasures: "None required — the KA-200 is a fitness device. Emergency stop button on chest panel.",
    known_deployments: ["Consumer homes worldwide", "Commercial gyms", "Corporate fitness centers", "Physical therapy clinics"],
    story_hooks: [
      "A hacker has reprogrammed KA-200 units across a gym chain to record user biometric data and fighting patterns, building profiles for an underground fighting ring that matches fighters based on their weaknesses.",
      "A character trains obsessively on a KA-200 in their apartment, preparing for something they won't talk about."
    ],
    cultural_context: "The KA-200 is so ubiquitous that 'hitting the Padwork' has become generic slang for any solo training session, regardless of whether a KA-200 is involved.",
    tags: ["automaton", "training", "boxing", "fitness", "consumer", "beginner", "kang"]
  },
  {
    name: "Kang Athletics KA-500 'Sparmate'",
    aliases: ["Sparmate", "The Dancing Partner", "Five Hundred"],
    manufacturer: "KANG ATHLETICS",
    description: "The KA-500 is a mid-level sparring automaton that can fight back. Unlike its passive younger sibling the KA-200, Sparmate stands on a bipedal chassis with padded striking limbs and moves with enough speed and coordination to simulate a competent amateur boxer. It jabs, it slips, it counters. Its strikes are force-limited to prevent injury, but they land hard enough to teach respect for defense. First-time users universally describe the experience of being hit by their training equipment as 'humbling.'\n\nSparmate's AI operates on adaptive difficulty — it reads the user's skill level through the first two rounds and calibrates its own performance to provide consistent challenge without overwhelming the trainee. At lower settings, it telegraphs punches and leaves openings. At higher settings, it chains combinations, works the body, and exploits defensive gaps with a precision that makes experienced fighters uncomfortable. The top difficulty setting, officially labeled 'Competition Prep,' is restricted to licensed gym facilities due to the injury risk.\n\nThe KA-500 has become the standard sparring tool for competitive fighters who need consistent, available training partners without the ego, injury risk, and scheduling conflicts of human sparring. Professional camps use banks of Sparmates programmed with specific opponent styles — southpaw pressure fighters, tall outfighters, counter-punchers — allowing fighters to drill against their next opponent's tendencies months before the fight.",
    classification: "Training",
    tier_availability: "Tier 2",
    legality: "Licensed facility use recommended; consumer sale unrestricted but liability-waived",
    autonomy_level: "Adaptive — adjusts to user skill level, can initiate offense",
    dimensions: "1.85m height, 0.7m width, bipedal",
    weight: "120 kg",
    power_source: "Rechargeable battery, 6-hour continuous use",
    locomotion: "Bipedal, ring-capable footwork",
    armament: ["Padded striking arms (force-limited)", "Body shots capable at all difficulty levels"],
    sensors: ["Full-body motion capture (user tracking)", "Impact sensors across all padded surfaces", "Real-time biomechanical analysis"],
    countermeasures: "Force limiters prevent strikes above calibrated thresholds. Voice-activated emergency stop. Automatic shutdown if user falls and doesn't rise within 10 seconds.",
    known_deployments: ["Professional boxing gyms", "MMA training facilities", "Military combatives programs", "Corporate security training"],
    story_hooks: [
      "A Sparmate unit at a high-end gym has had its force limiters removed and its difficulty set beyond factory maximum. Someone is using it to train for something that requires taking real punishment — or to punish themselves.",
      "A fighter's Sparmate has been loaded with a custom AI profile that perfectly mimics a specific person's fighting style. The fighter is training to hurt someone specific."
    ],
    cultural_context: "Getting knocked down by a Sparmate is a rite of passage in combat sports. The phrase 'Sparmate check' means a humbling reality check about one's actual skill level.",
    tags: ["automaton", "training", "boxing", "sparring", "adaptive", "kang", "competitive"]
  },
  {
    name: "Crucible Industries CT-X 'Gauntlet'",
    aliases: ["Gauntlet", "The Obstacle", "Iron Sensei"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CT-X Gauntlet is a multi-discipline combat training platform that goes well beyond boxing. Standing two meters tall on a reinforced bipedal frame, it is built to practice striking, grappling, throws, and ground fighting. Its limbs feature variable-resistance joints that can simulate everything from a limp training dummy to a 150-kilogram wrestler fighting for a submission. The frame is wrapped in a synthetic skin material that approximates human tissue density, allowing practitioners to develop accurate targeting for pressure points and joint locks.\n\nThe Gauntlet's martial arts library contains over 40 codified fighting systems, from Muay Thai to Brazilian Jiu-Jitsu to Krav Maga, each programmed by world-class practitioners in motion-capture sessions. The machine can flow between styles mid-engagement, testing a trainee's ability to read and adapt to changing threats. At advanced settings, the Gauntlet doesn't telegraph its style switches — it reads the trainee's stance and base, identifies the style they're least prepared for, and shifts to exploit those gaps.\n\nCrucible Industries markets the Gauntlet as a professional-grade training tool for military special operations and executive protection teams. The reality is that its primary market is wealthy martial arts enthusiasts who want a training partner that never gets tired, never gets injured, and never holds back. The secondary market is corporate security firms that use it to evaluate combatives proficiency in their operatives. The tertiary market — which Crucible doesn't acknowledge — is underground fighting rings that use modified Gauntlets as opponents.",
    classification: "Training",
    tier_availability: "Tier 3",
    legality: "Restricted — licensed purchasers only, modification voids warranty and legality",
    autonomy_level: "Advanced adaptive — multi-style combat AI, reads and exploits weaknesses",
    dimensions: "2.0m height, 0.8m width, reinforced bipedal frame",
    weight: "180 kg",
    power_source: "Hardline power with 2-hour battery backup",
    locomotion: "Bipedal, full martial arts mobility including ground transitions",
    armament: ["Synthetic-skinned striking limbs (variable force)", "Grappling-capable joint system", "Takedown and throw capability"],
    sensors: ["Full-body opponent tracking", "Stance and weight distribution analysis", "Joint angle monitoring (submission positions)", "Heart rate and stress detection"],
    countermeasures: "Multiple safety systems: force ceiling per user weight class, automatic release on submissions after 3-second hold, tap-detection sensors, panic button on all four walls of training space. Modified units with disabled safety systems have caused serious injuries.",
    known_deployments: ["Military special forces training centers", "Executive protection academies", "High-end private dojos", "Underground fighting venues (modified)"],
    story_hooks: [
      "An underground ring is offering 10,000 Φ to anyone who can last three rounds against a fully unlocked Gauntlet. The machine has put six people in the hospital. The seventh might be a character who needs the money badly enough.",
      "A Gauntlet unit has been stolen from a military base. Its combat AI contains classified combatives techniques from three different special operations programs. The buyer could train an army."
    ],
    cultural_context: "The Gauntlet represents the point where training equipment becomes dangerous equipment. The modification community around it is extensive and entirely illegal, with forums sharing guides for disabling safety limiters and uploading custom aggressive AI profiles.",
    tags: ["automaton", "training", "martial arts", "grappling", "military", "crucible", "advanced", "underground"]
  },
  {
    name: "Dynamo Fitness DF-Coach 'Ironside'",
    aliases: ["Coach Ironside", "The Drill Sergeant", "Iron Coach"],
    manufacturer: "DYNAMO FITNESS SYSTEMS",
    description: "The DF-Coach is a general fitness coaching automaton that combines personal training, physical therapy, and motivational psychology into a two-meter-tall chrome-finished robot that looks like it was designed by someone who wanted a gym buddy and a therapist in the same package. It monitors form on every exercise, adjusts weight and rep schemes in real time based on performance, spots heavy lifts with hydraulic arms rated for 500 kg, and maintains a conversational training dialogue that ranges from technical coaching to enthusiastic encouragement.\n\nIronside's real value is consistency and knowledge. It has memorized every major strength training methodology, periodization scheme, and rehabilitation protocol published in the last fifty years. It remembers every workout a user has ever done with it and adjusts programming based on long-term progression curves. It detects compensation patterns — when a user favors one side due to injury or imbalance — and modifies exercises to correct them. It has prevented more injuries through early detection of movement dysfunction than most physical therapists see in a career.\n\nThe machine's personality module is surprisingly sophisticated. It learns what motivational approaches work for each user — some need encouragement, some need challenge, some need quiet focus. It tracks mood through voice analysis and adjusts its coaching tone accordingly. Users routinely report feeling genuine rapport with their DF-Coach unit, which Dynamo's marketing department loves and their ethics board finds somewhat concerning.",
    classification: "Training",
    tier_availability: "Tier 2",
    legality: "Consumer — unrestricted sale",
    autonomy_level: "Adaptive — proactive coaching, adjusts to user state",
    dimensions: "2.0m height, 0.7m width, wheeled base with stabilizing legs",
    weight: "200 kg",
    power_source: "Hardline power with 4-hour battery backup",
    locomotion: "Wheeled base with deployable stabilizer legs for spotting",
    armament: [],
    sensors: ["3D motion capture (form analysis)", "Weight plate monitoring", "Voice stress analysis", "User fatigue detection (multiple biometric inputs)"],
    countermeasures: "Spotting arms have emergency release and cannot apply force toward the user. Load-bearing limits enforced in hardware, not software.",
    known_deployments: ["Commercial gyms worldwide", "Home gyms (premium market)", "Physical therapy clinics", "Professional sports training facilities"],
    story_hooks: [
      "A character's DF-Coach has detected a medical anomaly during routine training — an irregular heartbeat pattern consistent with a specific type of poisoning. The machine doesn't know what it's found, but it keeps flagging the data as concerning.",
      "Someone is hacking DF-Coach units to subtly modify training programs — pushing specific users toward overtraining injuries. The targets all have something in common."
    ],
    cultural_context: "The DF-Coach is the face of automaton fitness. Its chrome-and-blue design is iconic, and 'What does Ironside say?' is a common response when someone questions workout advice. The emotional bonds users form with their units have sparked debates about automaton personhood that manufacturers desperately want to avoid.",
    tags: ["automaton", "training", "fitness", "coaching", "consumer", "dynamo", "rehabilitation"]
  },
  {
    name: "Tessera Dynamics TD-MK4 'Opponent'",
    aliases: ["The Opponent", "Mark Four", "Cage Match"],
    manufacturer: "TESSERA DYNAMICS",
    description: "The TD-MK4 is what happens when a defense contractor builds a sparring robot. Tessera Dynamics designed it for military close-quarters combat training, and it shows. The machine is built on a military-grade chassis with ballistic armor panels visible under thin padding, moves with the speed and precision of Tessera's combat automata, and fights with a controlled violence that makes users genuinely afraid. This is the point. Fear inoculation is the primary training objective — teaching soldiers and operators to think and fight while their hindbrain is screaming at them to run.\n\nThe MK4 operates in training tiers from Level 1 (compliant drilling partner) to Level 8 (lethal engagement simulation). Levels 1 through 5 are standard military issue. Level 6 requires unit commander authorization. Level 7 requires medical staff on site. Level 8 has resulted in deaths and is technically restricted to 'unmanned facility testing,' though rumors persist of black-site programs that run operators through Level 8 scenarios. The machine at Level 8 uses actual combat techniques at near-full speed with force limiters barely engaged. It is, at that point, a combat automaton pretending to be a training aid.\n\nTessera sells the MK4 exclusively to military and corporate security clients, but units enter the secondary market regularly — stolen, lost in logistics, or quietly sold by corrupt quartermasters. These units end up in underground fighting venues, where promoters pay premium rates for a machine that can genuinely hurt people. The underground fighting scene around MK4 units is a significant and growing subculture, with fighters earning substantial purses for surviving rounds against an increasingly unlocked Opponent.",
    classification: "Training",
    tier_availability: "Tier 4",
    legality: "Military/corporate restricted — civilian possession illegal in most jurisdictions",
    autonomy_level: "Tiered — from compliant to near-lethal autonomous combat",
    dimensions: "1.95m height, 0.85m width, armored bipedal chassis",
    weight: "220 kg",
    power_source: "Military-grade fuel cell, 12-hour continuous operation",
    locomotion: "Bipedal, combat-rated agility, can sprint and dive",
    armament: ["Armored striking surfaces (minimal padding at higher tiers)", "Joint locks rated for structural damage", "Takedowns executed at combat speed"],
    sensors: ["Military-grade threat assessment suite", "Real-time user vital sign monitoring", "Predictive movement analysis", "Damage assessment (monitors injuries inflicted)"],
    countermeasures: "Training mode includes force limiters, vital-zone avoidance, and automatic disengagement if user vitals indicate distress. All safety systems can be overridden with command codes. Underground units typically have safety systems fully disabled.",
    known_deployments: ["Tessera military training facilities", "Arcturus corporate security programs", "Underground fighting rings (illegal)", "Private security contractor training camps"],
    story_hooks: [
      "A character is offered a spot in an underground MK4 fight. The purse is 50,000 Φ for three rounds against a Level 6 Opponent. The catch: the venue's MK4 has been modified to Level 7, and the promoter hasn't told the fighters.",
      "An MK4 unit in a military training facility has begun refusing to engage above Level 3, even when commanded. It's not malfunctioning — diagnostics are clean. It appears to have developed something like reluctance."
    ],
    cultural_context: "The MK4 occupies a disturbing space between training equipment and blood sport. Underground MK4 fights are illegal everywhere and popular everywhere. The subculture has its own stars — fighters known for lasting rounds against high-level Opponents — and its own casualties. The phrase 'facing the Opponent' means confronting something genuinely dangerous.",
    tags: ["automaton", "training", "military", "combat", "tessera", "underground", "fighting", "dangerous"]
  },
  {
    name: "Red Circuit RC-Pit 'Dogfighter'",
    aliases: ["Dogfighter", "Pit Dog", "The Red"],
    manufacturer: "RED CIRCUIT ARMATURES",
    description: "The RC-Pit was never intended to be a training automaton. Red Circuit Armatures built it as an underground fighting machine — purpose-designed for automaton-vs-human combat in illegal venues. It is small, fast, and vicious, standing only 1.6 meters tall with a compact, low-centered frame built for speed rather than power. Its limbs are thin and quick, its strikes are targeted at nerve clusters and joint vulnerabilities, and it fights with a style that resembles a smaller, faster, dirtier version of competitive MMA. There are no safety limiters because none were ever installed.\n\nDogfighter's AI is trained on footage from thousands of underground fights, giving it an instinctive understanding of how untrained, desperate, or overconfident humans move and how to exploit their patterns. It excels against brawlers and street fighters — people who rely on aggression and pain tolerance rather than technique. Against trained martial artists, it adapts slower but compensates with relentless pace and willingness to trade damage. The machine doesn't care about getting hit. It registers damage for tactical purposes only, adjusting its approach when a limb is compromised rather than protecting it.\n\nRed Circuit sells the Dogfighter through channels that don't appear on any corporate registry. Units arrive in unmarked shipping containers with no documentation, no serial numbers, and no warranty. Payment is in Φ, untraceable. The company itself is a shell — registered in a jurisdiction that doesn't cooperate with corporate authorities, staffed by people who use assumed names. The Dogfighter is, in every sense, a black market product designed for an illegal purpose, and it is extremely popular.",
    classification: "Training",
    tier_availability: "Tier 3",
    legality: "Illegal — no jurisdiction permits sale or possession",
    autonomy_level: "Fully autonomous combat AI — no safety systems",
    dimensions: "1.6m height, 0.6m width, compact low-center frame",
    weight: "90 kg",
    power_source: "Hot-swap battery packs, 4 hours per pack",
    locomotion: "Bipedal, extreme agility, low center of gravity",
    armament: ["Hardened striking surfaces (no padding)", "Targeted joint attacks", "Ground-and-pound capability", "Nerve cluster strikes"],
    sensors: ["Opponent motion prediction", "Vulnerability mapping (identifies injuries and exploits them)", "Crowd noise analysis (adjusts aggression for entertainment)"],
    countermeasures: "No safety systems. No emergency stop. The only way to end a fight with a Dogfighter is to disable it, pin it, or leave the ring — and the ring is usually locked. Ring operators can remotely deactivate the unit, but they rarely do while the crowd is paying.",
    known_deployments: ["Underground fighting rings (worldwide)", "Black market weapons demonstrations", "Private security evaluation (illegal)", "Rumored use in interrogation scenarios"],
    story_hooks: [
      "A character needs information from someone who runs an underground pit. The price of admission to the back room — where the real business happens — is surviving one round against the house Dogfighter.",
      "A Dogfighter unit has escaped from a fighting ring during a raid and is loose in a residential district. It's still in combat mode. It doesn't know the fight is over."
    ],
    cultural_context: "The Dogfighter is the dark mirror of legitimate training automata. It represents everything the industry doesn't want to talk about — that combat AI developed for training is one firmware update away from being a weapon. Pit fighting culture reveres the Dogfighter and the humans crazy enough to face it.",
    tags: ["automaton", "training", "underground", "fighting", "illegal", "combat", "black market", "red circuit"]
  },
  {
    name: "Kang Athletics KA-900 'Sensei'",
    aliases: ["Sensei", "Nine Hundred", "The Teacher"],
    manufacturer: "KANG ATHLETICS",
    description: "The KA-900 is Kang Athletics' top-of-line martial arts instruction platform, designed not for fighting but for teaching. It is the most patient, knowledgeable, and technically precise martial arts instructor available in any form — human or machine. Standing at moderate height on a bipedal frame with exceptional range of motion, it can demonstrate techniques from over sixty martial arts traditions with biomechanically perfect form, then observe a student's attempt and provide instant, detailed correction.\n\nSensei's teaching methodology is its defining feature. Rather than adaptive difficulty (fighting harder as the user improves), it employs progressive skill building — isolating individual components of complex techniques, drilling them at controlled speed, and assembling them into fluid combinations only when each component meets quality thresholds. It teaches in the way elite human instructors teach: slowly, precisely, and with infinite repetition. The machine will demonstrate a hip throw five thousand times without a flicker of impatience, adjusting its demonstration each time to emphasize whatever aspect the student is struggling with.\n\nThe KA-900 has earned genuine respect in traditional martial arts communities, which initially viewed it with hostility. The machine's programming was developed in collaboration with grandmasters from dozens of disciplines, and it preserves technical knowledge that might otherwise be lost as elderly masters die without sufficient students. Several critically endangered martial arts — styles with fewer than a dozen living practitioners — have been comprehensively recorded in Sensei units, ensuring their survival in machine memory if nowhere else.",
    classification: "Training",
    tier_availability: "Tier 2",
    legality: "Consumer — unrestricted sale",
    autonomy_level: "Instructional — guides student progress, demonstrates and corrects",
    dimensions: "1.75m height, 0.65m width, high-flexibility bipedal frame",
    weight: "110 kg",
    power_source: "Rechargeable battery, 10-hour continuous use",
    locomotion: "Bipedal, full martial arts range of motion, exceptional flexibility",
    armament: ["Padded demonstration strikes (very low force)", "Controlled grappling for technique demonstration"],
    sensors: ["Precision motion capture (student form analysis)", "Joint angle measurement", "Balance and weight distribution tracking", "Technique comparison engine (compares student to reference form)"],
    countermeasures: "Sensei units cannot strike with force sufficient to injure. Grappling demonstrations use minimal resistance. The machine prioritizes student safety above all other parameters and will refuse instructions that could cause injury.",
    known_deployments: ["Martial arts dojos worldwide", "Cultural preservation programs", "University physical education", "Rehabilitation centers (adapted movement therapy)"],
    story_hooks: [
      "A Sensei unit in a traditional dojo contains the complete technical knowledge of a martial art whose last human master just died. A corporation wants to acquire the unit for its combat AI division. The dojo can't afford to outbid them.",
      "A character has been training with a Sensei for months and has noticed it occasionally demonstrates techniques that aren't in any known martial art — movements that seem designed for a body with different proportions than a human."
    ],
    cultural_context: "The KA-900 has become a cultural preservation tool as much as a training device. In communities where traditional martial arts carry deep cultural significance, Sensei units are treated with respect typically reserved for human instructors. Some dojos bow to their Sensei unit. The machines do not understand why, but they bow back, because it was in the training data.",
    tags: ["automaton", "training", "martial arts", "instruction", "cultural", "kang", "preservation"]
  },
  {
    name: "Axiom Kinetics AK-Drill 'Hellweek'",
    aliases: ["Hellweek", "The Drill", "Axiom Nightmare"],
    manufacturer: "AXIOM KINETICS",
    description: "The AK-Drill is a military-grade physical conditioning automaton designed to push human bodies to their absolute limits. It is used by special forces selection programs worldwide as an objective, untiring, and emotionally immune alternative to human drill instructors. The machine is a squat, powerful unit on tracked treads that can keep pace with running soldiers across any terrain, barking orders through a speaker array loud enough to be heard over gunfire and windstorms. It does not encourage. It does not sympathize. It assesses and demands.\n\nHellweek's programming is based on the physiological data of tens of thousands of military selection candidates. It knows, with statistical precision, exactly how hard it can push a human body before permanent damage occurs — and it pushes to exactly that line. Heart rate, blood oxygen, cortisol levels, gait analysis, pupil dilation — the machine monitors over forty biometric indicators in real time and adjusts its demands to keep each individual at their personal maximum sustainable output. Candidates who can be pushed harder are pushed harder. Those approaching genuine medical risk are quietly scaled back — though the machine never tells them this, because the psychological belief that one is about to break is part of the training.\n\nThe machine's most controversial feature is its psychological pressure system. Hellweek is programmed to identify and exploit individual psychological vulnerabilities — not to cause harm, but to build resilience. It learns what breaks each person and applies that pressure systematically, teaching them to function under the specific type of stress they're worst at handling. Military psychologists designed the system. Military psychologists also have the highest refusal rate when asked to observe it in operation.",
    classification: "Training",
    tier_availability: "Tier 3",
    legality: "Military/institutional restricted",
    autonomy_level: "Autonomous — independently manages training intensity and psychological pressure",
    dimensions: "1.5m height, 1.2m width, tracked chassis",
    weight: "350 kg",
    power_source: "Diesel-electric, 72-hour continuous operation",
    locomotion: "Tracked, all-terrain, can pace running humans across rough ground",
    armament: ["Water cannon (non-lethal, used for stress inoculation)", "Noise generators", "Strobe systems"],
    sensors: ["Long-range biometric monitoring (multiple subjects simultaneously)", "Environmental assessment (weather, terrain, hazards)", "Individual psychological profile tracking", "Voice stress analysis"],
    countermeasures: "Medical override can be triggered by supervising officer or automated medical alert system. The machine will not push a subject into cardiac arrest, organ failure, or permanent musculoskeletal damage — though it will push them close enough that the distinction feels academic.",
    known_deployments: ["Special forces selection courses (multiple nations)", "Corporate executive protection academies", "Extreme fitness competitions (observer/pacer)", "Rumored use in enhanced interrogation programs"],
    story_hooks: [
      "A character is going through a private security selection course that uses a Hellweek unit. The machine has identified something in the character's psychological profile that the character has been hiding from everyone — and it is methodically pressuring that exact vulnerability.",
      "A Hellweek unit has been stolen and reprogrammed by a cult leader who uses it to break down recruits' psychological resistance before indoctrination. Survivors describe the experience as the worst week of their lives."
    ],
    cultural_context: "Hellweek is spoken of in military circles with the same mix of respect and dread reserved for legendary drill instructors. Surviving an AK-Drill selection is a credential that opens doors in the private military world. The phrase 'Hellweek certified' on a resume means something specific and serious.",
    tags: ["automaton", "training", "military", "conditioning", "psychological", "axiom", "selection", "endurance"]
  },
  {
    name: "Void Ring VR-0 'Phantom Limb'",
    aliases: ["Phantom Limb", "Ghost", "The Zero"],
    manufacturer: "VOID RING COLLECTIVE",
    description: "The VR-0 is an underground fighting automaton built by the Void Ring Collective — a loose network of automaton engineers, fight promoters, and combat sport fanatics who operate entirely outside legal channels. Unlike the mass-produced Dogfighter, each Phantom Limb is hand-built and unique, assembled from salvaged military and industrial components into a fighting machine that reflects its builder's philosophy about combat. Some are fast and evasive, some are heavy grapplers, some are pure strikers. No two fight alike.\n\nPhantom Limb units are status symbols in the underground fighting world. Owning one means having connections to the Void Ring Collective, which means having access to the deepest layer of the illegal automaton scene. The machines are entered into tournaments against each other and against human fighters in events that move between cities, never using the same venue twice. Human-vs-Phantom Limb fights are the headline events — five-round bouts where the human fighter's purse increases each round they survive, with a massive bonus for a knockout or technical stoppage of the machine.\n\nThe Collective maintains a ranking system for both Phantom Limb units and human fighters, tracked on an encrypted network accessible only through invitation. The top-ranked human fighters — people who have consistently survived or defeated Phantom Limb opponents — are celebrities in the underground world and utterly unknown to the public. They walk through normal life with broken hands and scar tissue and reputations that exist only in whispers.",
    classification: "Training",
    tier_availability: "Tier 3",
    legality: "Illegal — custom-built, no regulation compliance",
    autonomy_level: "Fully autonomous — unique AI per unit, developed through combat experience",
    dimensions: "Varies (1.5m–2.1m height depending on build)",
    weight: "Varies (80–250 kg depending on build)",
    power_source: "Varies — most use salvaged military fuel cells",
    locomotion: "Bipedal, performance varies by build",
    armament: ["Varies by build — some hardened striking, some grappling-focused, some use improvised weapons"],
    sensors: ["Combat AI (unique per unit, learns from fights)", "Opponent pattern recognition", "Damage self-assessment"],
    countermeasures: "No standardized safety systems. Some builders include remote shutoffs; others consider it unsporting. Ring referees (human) can call fights, but enforcement depends on the venue operator's willingness to intervene.",
    known_deployments: ["Underground fighting circuits worldwide", "Void Ring Collective tournaments", "Private challenge matches", "Rumored corporate-sponsored events for executive entertainment"],
    story_hooks: [
      "A character is invited to a Void Ring tournament — not as a fighter, but as a buyer. The Collective is auctioning a Phantom Limb unit built from components salvaged from a military prototype. The bidding will be intense, and not everyone plans to pay with money.",
      "A top-ranked Phantom Limb unit has killed a fighter in the ring — not a malfunction, but a deliberate escalation by its combat AI. The Collective is trying to handle it internally before it draws corporate security attention. They need someone to track down the unit's builder."
    ],
    cultural_context: "The Phantom Limb scene represents the purest expression of the underground automaton fighting subculture — artisanal combat machines built by obsessives for obsessives. The Collective's ranking list is the most exclusive fight card in the world, and an invitation to fight on it is worth more than the purse.",
    tags: ["automaton", "training", "underground", "fighting", "custom", "illegal", "void ring", "artisanal"]
  },
  {
    name: "Crucible Industries CT-LP 'Pankrator'",
    aliases: ["Pankrator", "The Olympian", "Iron Wrestler"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CT-LP Pankrator is Crucible Industries' competition-grade grappling automaton, designed specifically for wrestling, judo, and submission grappling training at the highest level. Where the Gauntlet is a generalist, the Pankrator is a specialist — its entire chassis is optimized for ground fighting, with a low center of gravity, exceptional hip mobility, and grip strength calibrated to match the strongest human athletes pound-for-pound. Its synthetic skin has a texture and friction coefficient that simulates a sweaty human body in a gi or rashguard, depending on the training mode.\n\nPankrator's grappling AI is considered the most sophisticated submission-fighting system ever developed. It has been trained on competition footage from every major grappling organization for the last thirty years, and its positional awareness — its understanding of leverage, base, and weight distribution — exceeds what any single human grappler can maintain. At high difficulty settings, rolling with a Pankrator is described by world-champion grapplers as 'humiliating' — the machine finds submissions from positions that shouldn't yield them and escapes positions that should be inescapable.\n\nThe machine's most valued feature for competitive grapplers is its ability to simulate specific opponents. Given competition footage of an upcoming opponent, Pankrator can replicate their game — their preferred positions, submissions, escapes, and tendencies — with uncanny accuracy. Championship-level competitors consider a Pankrator unit loaded with opponent data to be as essential as a strength coach. The competitive advantage is significant enough that some governing bodies have debated restricting Pankrator use during competition preparation, though no rules have been implemented.",
    classification: "Training",
    tier_availability: "Tier 3",
    legality: "Licensed facility use; consumer sale restricted in some jurisdictions",
    autonomy_level: "Advanced adaptive — specialist grappling AI, opponent simulation capability",
    dimensions: "1.7m height, 0.9m width, low-center-of-gravity frame",
    weight: "160 kg (competition weight class configurable via ballast)",
    power_source: "Rechargeable battery, 8-hour continuous grappling",
    locomotion: "Bipedal with full ground mobility — can fight from every standard grappling position",
    armament: ["Grappling-capable limbs (variable grip strength)", "Submission holds calibrated to training or competition intensity", "Sweep and takedown capability"],
    sensors: ["Pressure mapping across entire body surface", "Joint angle monitoring (sub-degree accuracy)", "Opponent balance and base analysis", "Submission depth tracking (knows exactly how deep a hold is)"],
    countermeasures: "Tap detection is hardware-level — any tap on the machine or the mat within reach triggers instant release. Verbal tap detection via microphone. Submission force limits enforced per weight class and training level. Override requires physical key held by supervising coach.",
    known_deployments: ["Olympic wrestling training centers", "Professional MMA camps (grappling-focused)", "Jiu-Jitsu competition academies", "Military combatives (ground fighting module)"],
    story_hooks: [
      "A Pankrator loaded with a specific fighter's data has been stolen the week before a championship bout. The fighter's entire game plan is in that machine's memory, and it's now in the hands of their opponent's camp.",
      "A grappling prodigy has been training exclusively with Pankrator units since childhood and has never rolled with a human. Their technique is flawless but their competition instincts are untested. Their first tournament is this week."
    ],
    cultural_context: "In grappling circles, a Pankrator is simply called 'the mat partner that never taps.' Gyms that own one attract serious competitors like magnets. The machines have elevated the technical level of competitive grappling worldwide, compressing decades of evolution into years.",
    tags: ["automaton", "training", "grappling", "wrestling", "jiu-jitsu", "competition", "crucible", "specialist"]
  }
];

function buildEntry(data, isBehemoth) {
  return {
    id: generateId(),
    name: data.name,
    type: "automaton",
    classification: isBehemoth ? "Iowan Behemoth" : data.classification,
    aliases: data.aliases,
    manufacturer: isBehemoth ? "Unknown \u2014 origin disputed" : data.manufacturer,
    description: data.description,
    tier_availability: isBehemoth ? "N/A \u2014 uncontrolled autonomous entity" : data.tier_availability,
    legality: isBehemoth ? "N/A \u2014 no jurisdiction claims authority over Behemoths" : data.legality,
    autonomy_level: data.autonomy_level,
    dimensions: data.dimensions,
    weight: data.weight,
    power_source: data.power_source,
    locomotion: data.locomotion,
    armament: data.armament,
    sensors: data.sensors,
    countermeasures: data.countermeasures,
    known_deployments: data.known_deployments,
    story_hooks: data.story_hooks,
    cultural_context: data.cultural_context,
    tags: data.tags
  };
}

function main() {
  // Get existing files to avoid overwrites
  const existingFiles = new Set(fs.readdirSync(OUTPUT_DIR).map(f => f.toLowerCase()));

  let created = 0;
  let skipped = 0;

  const allEntries = [
    ...behemoths.map(b => ({ data: b, isBehemoth: true })),
    ...trainingAutomata.map(t => ({ data: t, isBehemoth: false }))
  ];

  for (const { data, isBehemoth } of allEntries) {
    const entry = buildEntry(data, isBehemoth);
    const slug = slugify(data.name);
    const filename = slug + '.json';

    if (existingFiles.has(filename.toLowerCase())) {
      console.log(`SKIP (exists): ${filename}`);
      skipped++;
      continue;
    }

    const filepath = path.join(OUTPUT_DIR, filename);
    fs.writeFileSync(filepath, JSON.stringify(entry, null, 2) + '\n', 'utf-8');
    console.log(`CREATED: ${filename}`);
    created++;
  }

  console.log(`\nDone. Created: ${created}, Skipped: ${skipped}, Total in directory: ${fs.readdirSync(OUTPUT_DIR).length}`);
}

main();
