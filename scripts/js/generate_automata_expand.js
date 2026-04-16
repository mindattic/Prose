const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'automata');

if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

const existingFiles = new Set(fs.readdirSync(OUTPUT_DIR));

function uid() {
  return crypto.randomBytes(16).toString('hex').slice(0, 32);
}

function slugify(text) {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

function writeEntity(entity) {
  let slug = slugify(entity.name.slice(0, 60));
  let filename = `${slug}.json`;
  let attempt = 0;
  while (existingFiles.has(filename)) {
    attempt++;
    filename = `${slug}_${attempt}.json`;
  }
  existingFiles.add(filename);
  const filepath = path.join(OUTPUT_DIR, filename);
  fs.writeFileSync(filepath, JSON.stringify(entity, null, 2) + '\n', 'utf8');
  return filename;
}

const automata = [

  // ===================== SEWAGE / WASTE PROCESSING =====================
  {
    name: "Crucible WR-9 'Gulliver'",
    type: "automaton",
    classification: "Waste Processing — Sewer Maintenance",
    aliases: ["Gulliver", "Pipe Pig", "Sewer Whale"],
    manufacturer: "CRUCIBLE INDUSTRIAL",
    description: "The WR-9 is a 3.5-meter segmented worm platform designed to navigate the aging sewer infrastructure beneath GLMZ's lower tiers. Each segment houses either a chemical processing unit or a mechanical grinding assembly, allowing the Gulliver to chew through blockages, neutralize toxic accumulations, and deposit antimicrobial coatings on pipe interiors in a single pass. The platform moves via peristaltic contraction of its rubberized outer hull, gripping pipe walls with radial pressure pads that distribute its 220 kg mass without stressing corroded infrastructure.\n\nCrucible originally developed the WR-9 for corponation water treatment facilities, but the majority of units in service have been purchased by GLMZ's infrastructure maintenance authority — such as it is. The machines operate continuously in shifts, navigating pipe networks that no human maintenance crew would enter voluntarily. The lower sewer systems contain chemical runoffs from unlicensed manufacturing, biological waste from unregistered medical clinics, and occasionally things that the Gullivers' onboard sensors flag as human remains but that nobody investigates further.\n\nThe Gulliver's most unsettling feature is its vocalizations. The chemical processing units produce harmonic resonances in pipe systems that carry for hundreds of meters, creating deep groaning sounds that Shelf residents have learned to associate with the machines' passage. Children in the lowest tiers grow up hearing the Gullivers sing through the walls at night.",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — municipal infrastructure equipment",
    autonomy_level: "Fully autonomous with scheduled route programming",
    dimensions: "3.5m length, 0.8m diameter (expandable to 1.2m)",
    weight: "220 kg",
    power_source: "Methane fuel cell harvesting ambient sewer gas, 30-day endurance",
    locomotion: "Peristaltic segmented contraction, radial pressure pads",
    armament: [],
    sensors: [
      "Chemical composition analysis array",
      "Structural integrity sonar",
      "Biological hazard detection",
      "Pipe network mapping LIDAR"
    ],
    countermeasures: "Non-combat platform. Can be disabled by severing between segments. Chemical processing units will vent caustic compounds if breached, creating localized hazard.",
    known_deployments: [
      "GLMZ Tier 1-2 sewer network (continuous)",
      "Crucible water treatment facility maintenance",
      "Emergency deployment during the Undercity flooding incident of 2194"
    ],
    story_hooks: [
      "A Gulliver has returned from a deep-sewer route with fragments of military-grade electronics in its grinder. Something is down there that shouldn't be, and someone sent this pipe worm to find it — or to destroy the evidence before anyone else could.",
      "Three Gullivers have gone offline in the same sewer junction. Maintenance crews sent to investigate found the machines intact but reprogrammed, now guarding a section of tunnel that doesn't appear on any official infrastructure map."
    ],
    cultural_context: "Shelf residents have a complicated relationship with the Gullivers. The machines keep the sewage systems functional in areas the city has otherwise abandoned, but they also represent the only municipal investment the lowest tiers receive — the city will spend money on robots to clean its pipes but not on clinics to treat its people.",
    tags: ["automaton", "waste", "sewer", "infrastructure", "maintenance", "crucible", "municipal", "tier 2"],
    id: uid()
  },

  {
    name: "Ouroboros RS-3 'Dung Beetle'",
    type: "automaton",
    classification: "Waste Processing — Surface Collection",
    aliases: ["Dung Beetle", "Roller", "Trash Turtle"],
    manufacturer: "OUROBOROS SYSTEMS",
    description: "The RS-3 is a squat, heavily armored refuse collection automaton that patrols designated surface routes in GLMZ, compacting and processing street-level waste into dense spherical pellets that it deposits at collection points. Standing 0.7 meters tall and roughly 1.2 meters in diameter, the Dung Beetle resembles nothing so much as an oversized robotic tortoise with a compaction maw where its head should be. It processes approximately 400 kg of refuse per cycle before returning to a depot to purge its internal compaction chamber.\n\nOuroboros designed the RS-3 to be maximally resilient to the abuse that any street-level automaton in GLMZ inevitably receives. The outer shell is rated against small-arms fire, the optical cluster is recessed behind armored louvers, and the locomotion system uses six independently driven wheels with solid rubber tires that cannot be punctured or slashed. The machine's response to being attacked is to retract all external sensors and wait, motionless, until its threat-assessment algorithms determine it is safe to resume operations. This passive defense strategy means that damaged Dung Beetles are sometimes found days later, still waiting in alleys for a threat that left hours ago.\n\nThe RS-3 is one of the few automata that lower-tier residents regard with something approaching affection. It performs a visible, unglamorous service without threatening anyone, and its patient, trundling patrol routes give it an almost animal-like presence in neighborhoods that have few other signs of functioning civic infrastructure.",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — municipal sanitation equipment",
    autonomy_level: "Fully autonomous with route scheduling",
    dimensions: "1.2m diameter, 0.7m height",
    weight: "180 kg empty, 580 kg at compaction capacity",
    power_source: "Solid-state battery, 18-hour patrol endurance",
    locomotion: "Six-wheel independent drive, solid rubber tires",
    armament: [],
    sensors: [
      "Waste composition analyzer",
      "Obstacle avoidance LIDAR",
      "Threat-assessment passive acoustic array"
    ],
    countermeasures: "Non-combat platform. Armored shell resists small-arms fire. Passive lockdown mode when threatened. No offensive capability.",
    known_deployments: [
      "GLMZ Tier 1-3 surface routes (continuous)",
      "Corporate district overflow waste management",
      "Emergency refuse clearance post-civil disturbance"
    ],
    story_hooks: [
      "Someone has been stuffing contraband inside compacted waste pellets, using the Dung Beetles' predictable routes as a dead-drop logistics network. The machines don't scan what they collect — they just compress it and move on.",
      "A Dung Beetle in the Shelf has been adopted by a block of residents who've decorated its shell with graffiti and leave food scraps in its path. When a corporate reclamation crew tried to remove it, the block rioted."
    ],
    cultural_context: "The Dung Beetle is one of the rare automata that has earned genuine popular goodwill. Street artists paint them. Kids follow them on their routes. They are a reminder that some machines exist to help, even if the system that deployed them doesn't care about the people they help.",
    tags: ["automaton", "waste", "sanitation", "municipal", "surface", "ouroboros", "tier 1"],
    id: uid()
  },

  // ===================== ENTERTAINMENT / PERFORMANCE =====================
  {
    name: "Ringo EV-5 'Showstopper'",
    type: "automaton",
    classification: "Entertainment — Performance Platform",
    aliases: ["Showstopper", "Dance Machine", "Ringo Star"],
    manufacturer: "RINGO ENTERTAINMENT DIVISION",
    description: "The EV-5 is a bipedal performance automaton standing 1.9 meters tall, designed to serve as the centerpiece of live entertainment venues, casino floors, and corporate hospitality events across GLMZ. Its chassis is sheathed in programmable surface panels that can display any color, pattern, or animated texture, and its 47-axis articulation system allows movement fluidity that exceeds human range of motion in every joint. The Showstopper can dance, mime, juggle, perform acrobatics, and execute choreographed routines with timing precision measured in microseconds.\n\nRingo markets the EV-5 as a premium entertainment solution, but its actual deployment reveals something darker about GLMZ's relationship with performance and labor. The Showstopper replaces human entertainers — dancers, acrobats, musicians — at a fraction of the ongoing cost, and it never gets tired, never demands better conditions, never unionizes. Casino floors that once employed hundreds of performers now run skeleton crews of technicians maintaining banks of EV-5 units that perform 20-hour shifts with perfect energy.\n\nThe most disturbing application is the EV-5's customer interaction mode. The unit's facial display panel renders photorealistic human expressions synthesized from emotional analysis of the audience, creating a feedback loop where the machine appears to be genuinely enjoying itself because it is reading and reflecting the audience's desire to see enjoyment. It is a mirror that shows people what they want to see, and most people cannot tell the difference.",
    tier_availability: "Tier 3+",
    legality: "Unrestricted — commercial entertainment equipment",
    autonomy_level: "Semi-autonomous with choreography programming",
    dimensions: "1.9m height, 0.6m shoulder width",
    weight: "65 kg",
    power_source: "High-density lithium-ceramic battery, 20-hour performance endurance",
    locomotion: "Bipedal, 47-axis articulated joints",
    armament: [],
    sensors: [
      "Audience emotion analysis array (facial recognition, biometric scanning)",
      "Spatial awareness for stage navigation",
      "Audio analysis for music synchronization"
    ],
    countermeasures: "Non-combat platform. Fragile compared to military automata. Surface panels crack under moderate impact.",
    known_deployments: [
      "GLMZ upper-tier casino complexes",
      "Corporate gala events and product launches",
      "Ringo Entertainment flagship showroom"
    ],
    story_hooks: [
      "An EV-5 in a casino has started performing routines it was never programmed with — dances that match the style of a human performer who died three years ago. The casino's owners want it investigated quietly, because the dead performer's family is litigious.",
      "Someone has loaded combat movement algorithms into a Showstopper chassis. A machine built to dance is now the most fluid close-quarters combatant anyone has ever seen, and it's being used in underground fighting rings."
    ],
    cultural_context: "The EV-5 is a flashpoint in GLMZ's ongoing labor displacement crisis. The Performers' Collective has staged protests outside venues that deploy Showstoppers, and several units have been vandalized with the slogan 'APPLAUSE IS NOT CONSENT.' The machines are beautiful, tireless, and utterly indifferent to the livelihoods they erase.",
    tags: ["automaton", "entertainment", "performance", "casino", "ringo", "labor", "bipedal", "tier 3"],
    id: uid()
  },

  // ===================== AGRICULTURAL PEST CONTROL =====================
  {
    name: "Lazarus APC-2 'Locust'",
    type: "automaton",
    classification: "Agricultural — Pest Interdiction Swarm",
    aliases: ["Locust", "Crop Cop", "Green Guardian"],
    manufacturer: "LAZARUS BIOWORKS",
    description: "The APC-2 is a micro-drone swarm system deployed in GLMZ's vertical farming towers and hydroponic districts. Each individual unit is roughly the size of a human thumb — a 4-centimeter flying platform with quad-rotor propulsion and a single-use chemical payload. A standard deployment canister contains 500 units that coordinate through mesh networking to patrol agricultural volumes, identify pest organisms through spectral analysis, and neutralize them with targeted micro-doses of species-specific pesticide delivered via contact injection.\n\nLazarus developed the APC-2 to address a critical vulnerability in GLMZ's food production: the vertical farms that feed the city's population are closed ecosystems where a single pest introduction can destroy an entire crop cycle within days. Traditional pesticide application is too broad — it kills beneficial organisms alongside pests and contaminates the produce. The Locust swarm provides surgical precision, eliminating individual pest organisms while leaving everything else untouched.\n\nThe system's most impressive feature is its learning capability. When the swarm encounters an organism not in its database, it captures specimens using adhesive micro-pads and delivers them to analysis stations for classification. Over three years of deployment, the Locust network has cataloged over 2,400 pest species and subspecies in GLMZ's agricultural zones, including several that were previously unknown to entomology — engineered organisms that someone introduced deliberately to sabotage food production.",
    tier_availability: "Tier 2+",
    legality: "Licensed — agricultural operators only",
    autonomy_level: "Fully autonomous swarm intelligence",
    dimensions: "4cm per unit, 500-unit standard deployment canister",
    weight: "3g per unit, 2.8 kg per canister with charging dock",
    power_source: "Inductive charging from canister dock, 4-hour flight endurance per unit",
    locomotion: "Quad-rotor micro-propulsion",
    armament: [
      "Single-use chemical micro-payload (species-specific pesticide)",
      "Adhesive specimen capture pads"
    ],
    sensors: [
      "Spectral analysis for organism identification",
      "Mesh network spatial coordination",
      "Environmental monitoring (humidity, temperature, CO2)"
    ],
    countermeasures: "Individual units are fragile — any physical impact destroys them. Electromagnetic interference disrupts swarm coordination. Swarm can be contained by sealing agricultural volume and venting atmosphere.",
    known_deployments: [
      "GLMZ Vertical Farm Towers 7 through 22",
      "Lazarus Bioworks experimental cultivation facilities",
      "Emergency deployment during the 2197 Blight Crisis"
    ],
    story_hooks: [
      "A Locust swarm has identified a new pest organism that appears to be a genetically engineered weapon — a crop-killer designed to collapse GLMZ's food supply. The organism's genetic markers trace back to a Lazarus subsidiary.",
      "Someone has reprogrammed a Locust swarm to target human-implanted BCIs instead of pest organisms. The micro-drones are too small to see and too numerous to avoid, and they carry payloads that fry neural interfaces on contact."
    ],
    cultural_context: "The Locust swarms are invisible guardians of GLMZ's food supply, and most residents never think about them. But agricultural workers know that the swarms are watching everything in the farm towers — not just pests, but people. Lazarus's agricultural data includes detailed movement tracking of every worker in every facility the swarms patrol.",
    tags: ["automaton", "agricultural", "swarm", "drone", "pest", "lazarus", "food", "micro", "tier 2"],
    id: uid()
  },

  // ===================== COMMUNICATION RELAY DRONE =====================
  {
    name: "TESSERA CR-12 'Whisper'",
    type: "automaton",
    classification: "Communications — Tactical Relay Platform",
    aliases: ["Whisper", "Signal Ghost", "Mesh Node"],
    manufacturer: "TESSERA",
    description: "The CR-12 is a small, disc-shaped aerial drone measuring 30 centimeters in diameter, designed to provide ad-hoc communications relay coverage in areas where fixed infrastructure has been destroyed, jammed, or was never built. Deployed in clusters of 6 to 20 units, Whispers autonomously position themselves at optimal altitudes and spacing to create a temporary mesh communications network covering up to 4 square kilometers. Each unit relays encrypted voice, data, and video feeds between ground users and can bridge connections to GLMZ's main communications backbone.\n\nTESSERA developed the CR-12 for military operations in communications-denied environments, but the platform has found its most extensive use in the Shelf and other lower-tier districts where communications infrastructure is unreliable or nonexistent. Criminal organizations, resistance cells, and community mutual-aid networks all use black-market Whisper clusters to maintain communications that bypass the monitored corporate networks. The irony is not lost on anyone: a military communications tool has become the backbone of underground civil society.\n\nThe CR-12's most valuable feature is its signal camouflage system. The relay transmissions are disguised as environmental electromagnetic noise — power line interference, appliance radiation, atmospheric static. To detection equipment, a Whisper network looks like background noise. Finding the drones requires physically spotting 30-centimeter discs hovering at altitude, usually at night, usually in the rain.",
    tier_availability: "Tier 3+",
    legality: "Restricted — military and licensed contractors; widely available on black market",
    autonomy_level: "Fully autonomous positioning with user-defined network parameters",
    dimensions: "30cm diameter, 5cm height",
    weight: "1.2 kg per unit",
    power_source: "Rechargeable solid-state cell, 8-hour endurance",
    locomotion: "Ducted fan, near-silent at operational altitude",
    armament: [],
    sensors: [
      "Signal environment mapping",
      "Electromagnetic spectrum analysis",
      "GPS and inertial navigation"
    ],
    countermeasures: "Directional signal analysis can locate individual units with specialized equipment. Physical interception requires aerial capability. Jamming the mesh requires overwhelming the signal camouflage across the full spectrum.",
    known_deployments: [
      "Military forward operations (classified)",
      "Shelf district community networks (unofficial)",
      "Corporate disaster recovery communications"
    ],
    story_hooks: [
      "A Whisper network in the Shelf has started relaying messages that nobody sent — fragments of encrypted traffic from a source that doesn't match any known protocol. Something is using the mesh to communicate, and it isn't human.",
      "TESSERA has issued a remote kill command for all CR-12 units in a specific district, claiming a firmware vulnerability. The real reason: someone used a Whisper network to broadcast evidence of corporate war crimes, and TESSERA wants the relay infrastructure destroyed before more data leaks."
    ],
    cultural_context: "In the Shelf, Whisper networks are a lifeline. They connect communities that the corporate communications grid has abandoned, enabling mutual aid coordination, medical consultations, and warnings about incoming security sweeps. Destroying a Whisper cluster is understood as an act of violence against the community it serves.",
    tags: ["automaton", "communications", "relay", "drone", "mesh", "tessera", "military", "shelf", "tier 3"],
    id: uid()
  },

  // ===================== SEARCH AND RESCUE =====================
  {
    name: "Crucible SAR-6 'Saint Bernard'",
    type: "automaton",
    classification: "Search and Rescue — Heavy Platform",
    aliases: ["Saint Bernard", "Big Dog", "The Retriever"],
    manufacturer: "CRUCIBLE INDUSTRIAL",
    description: "The SAR-6 is a quadruped rescue platform standing 1.4 meters at the shoulder and weighing 310 kg, designed to operate in collapsed structures, toxic environments, and disaster zones where human rescuers cannot safely enter. Its reinforced chassis can support an additional 200 kg of recovered casualties, and its manipulator arms — two heavy-duty limbs mounted at the shoulder assembly — can lift structural debris weighing up to 500 kg. The platform's sealed environmental system allows operation in atmospheres containing toxic gases, biological contaminants, or radiation levels that would incapacitate an unprotected human within minutes.\n\nCrucible built the SAR-6 because GLMZ needs it. The city's lower tiers experience structural collapses with disturbing regularity — aging infrastructure, unlicensed construction, industrial accidents, and occasional corporate ordnance create a steady supply of disaster zones that require search and rescue response. The corponations that control the upper tiers fund rescue operations for their own facilities and personnel. Everyone else gets whatever municipal resources are available, which increasingly means SAR-6 units operating without human backup because there aren't enough trained rescuers to go around.\n\nThe Saint Bernard's most remarkable feature is its casualty assessment system. The platform can perform triage on recovered individuals, stabilize life-threatening injuries using onboard medical supplies, and transmit patient data to receiving hospitals while still navigating debris fields. It is, in many cases, the only medical professional that lower-tier disaster victims will see for hours — a machine that performs the work of compassion because the system that created the need for compassion can't be bothered to provide it through people.",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — emergency services equipment",
    autonomy_level: "Semi-autonomous with remote operator guidance",
    dimensions: "2.1m length, 1.4m shoulder height, 1.0m width",
    weight: "310 kg",
    power_source: "Hydrogen fuel cell with emergency battery backup, 48-hour endurance",
    locomotion: "Quadruped articulated legs, rubberized foot pads, stair and rubble capable",
    armament: [],
    sensors: [
      "Life-sign detection (thermal, acoustic, CO2 analysis)",
      "Structural integrity sonar",
      "Toxic atmosphere composition analysis",
      "Ground-penetrating radar for buried casualties"
    ],
    countermeasures: "Non-combat platform. Heavy chassis provides incidental ballistic resistance. Operating in disaster zones provides natural concealment.",
    known_deployments: [
      "GLMZ Emergency Services (continuous deployment)",
      "Tier 2 structural collapse response (averaging 12 incidents monthly)",
      "Crucible Industrial facility disaster response"
    ],
    story_hooks: [
      "A SAR-6 has returned from a collapsed building in the Shelf carrying a survivor who shouldn't exist — someone with no BCI, no identity records, no biometric match in any database. The platform's medical logs show the individual has injuries consistent with being buried for weeks, but the building collapsed three hours ago.",
      "Crucible has been quietly upgrading SAR-6 units with concealed surveillance equipment. Every disaster zone the Saint Bernards enter becomes a data collection opportunity — mapping underground spaces, cataloging residents, identifying infrastructure that doesn't appear on official records."
    ],
    cultural_context: "The SAR-6 is one of the few machines that people in the Shelf trust. When the walls come down, the Saint Bernard comes. It doesn't ask what tier you live on or whether you have insurance. It just digs. For many lower-tier residents, a Crucible rescue robot is the closest thing to institutional care they will ever experience.",
    tags: ["automaton", "rescue", "search", "quadruped", "medical", "crucible", "disaster", "shelf", "tier 2"],
    id: uid()
  },

  // ===================== AUTONOMOUS VEHICLE SYSTEM =====================
  {
    name: "Vantablack AT-4 'Phantom Cab'",
    type: "automaton",
    classification: "Autonomous Vehicle — Urban Transport",
    aliases: ["Phantom Cab", "Ghost Ride", "Black Car"],
    manufacturer: "VANTABLACK MOBILITY",
    description: "The AT-4 is an autonomous passenger vehicle that operates throughout GLMZ's upper and middle tiers, providing on-demand transportation to subscribers of Vantablack's mobility service. The vehicle seats four passengers in a windowless, sound-insulated cabin with interior environmental controls and entertainment displays. There is no steering wheel, no manual override, and no way to see outside except through the vehicle's own camera feeds displayed on interior screens — feeds that Vantablack can modify in real time.\n\nThe Phantom Cab's most controversial feature is its routing algorithm. The AT-4 does not take the fastest route, the shortest route, or the route the passenger requests. It takes the route that maximizes Vantablack's revenue, which may include detours through commercial districts where the interior displays show targeted advertisements, or past competing businesses whose negative information Vantablack has been paid to present. Passengers in a sealed, windowless cabin with no manual override are a captive audience in the most literal sense.\n\nVantablack's terms of service — which no one reads — include a clause granting the company rights to all conversations, biometric data, and behavioral patterns recorded during transit. The AT-4's cabin is the most surveilled space in GLMZ per square meter. Business negotiations, personal confessions, medical discussions, criminal planning — everything said in a Phantom Cab belongs to Vantablack. The company's data brokerage division generates more revenue than its transportation service.",
    tier_availability: "Tier 3+",
    legality: "Licensed — commercial transportation operator",
    autonomy_level: "Fully autonomous, no manual override",
    dimensions: "4.8m length, 2.0m width, 1.6m height",
    weight: "1,800 kg",
    power_source: "Solid-state battery pack, 400 km range per charge",
    locomotion: "Four-wheel electric drive, all-wheel steering",
    armament: [],
    sensors: [
      "360-degree external sensor array (LIDAR, camera, radar)",
      "Interior cabin monitoring (audio, video, biometric)",
      "Traffic network integration",
      "Passenger identification and behavioral analysis"
    ],
    countermeasures: "Sealed cabin can be remotely locked. Vehicle can be immobilized remotely by Vantablack operations center. Emergency exit requires physical force against reinforced doors.",
    known_deployments: [
      "GLMZ Tier 3-5 transportation network (continuous fleet of 8,000+ units)",
      "Corporate campus shuttle services",
      "VIP transport with enhanced security packages"
    ],
    story_hooks: [
      "A Phantom Cab has delivered a passenger to a destination they didn't request — a Vantablack facility in an industrial district with no public access. The passenger hasn't been seen since. The company claims a 'routing error.'",
      "An underground collective has figured out how to spoof AT-4 cabin sensors, creating dead zones inside the vehicles where conversations can't be recorded. They're selling 'privacy rides' to anyone who can afford the Φ500 surcharge."
    ],
    cultural_context: "The Phantom Cab has become a symbol of GLMZ's surveillance economy. 'Getting into a black car' is slang for making a mistake you can't take back. Privacy advocates have staged protests by riding Phantom Cabs while reading corporate surveillance data aloud, forcing the system to record its own crimes.",
    tags: ["automaton", "vehicle", "transport", "surveillance", "autonomous", "vantablack", "corporate", "tier 3"],
    id: uid()
  },

  // ===================== PRISON / DETENTION =====================
  {
    name: "Arcturus DC-3 'Warden'",
    type: "automaton",
    classification: "Detention — Autonomous Guard Platform",
    aliases: ["Warden", "Block Boss", "Tin Screw"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The DC-3 is a bipedal humanoid-frame automaton standing 2.0 meters tall, deployed as the primary guard and management platform in GLMZ's privatized detention facilities. Each Warden unit is responsible for a cell block of up to 60 detainees, managing all aspects of detention including meal distribution, medical triage, behavioral monitoring, and disciplinary enforcement. The platform's chassis is deliberately designed to be imposing — broad shoulders, a featureless faceplate, and articulated hands capable of restraining an augmented adult.\n\nArcturus sold the DC-3 program to GLMZ's detention authority on two arguments: cost reduction and impartiality. Human guards are expensive, corruptible, and occasionally compassionate. The Warden is none of these things. It enforces rules with absolute consistency — the same response to the same infraction, every time, regardless of the detainee's identity, connections, or ability to pay. In theory, this eliminates the favoritism and brutality that plagued human-staffed facilities.\n\nIn practice, the DC-3 has simply automated the brutality. The platform's disciplinary protocols include escalating physical restraint that begins with grip immobilization and progresses through pain compliance holds to directed electrical discharge. The Warden does not lose its temper, but it also does not exercise judgment. A detainee having a panic attack and a detainee resisting lawful instruction produce identical behavioral signatures in the DC-3's analysis system, and receive identical responses. The machine cannot distinguish between defiance and despair, and its programming does not require it to try.",
    tier_availability: "Tier 4+",
    legality: "Restricted — licensed detention facility operators only",
    autonomy_level: "Fully autonomous with administrative override",
    dimensions: "2.0m height, 0.8m shoulder width",
    weight: "140 kg",
    power_source: "Hot-swappable battery modules, 16-hour shift endurance",
    locomotion: "Bipedal, reinforced ankle and knee joints for stability",
    armament: [
      "Integrated electrical discharge system (pain compliance)",
      "Restraint-capable articulated hands (1,200 N grip force)",
      "Chemical agent dispensers (tear gas, sedative aerosol)"
    ],
    sensors: [
      "Behavioral analysis array (posture, vocal stress, heart rate)",
      "Facial recognition and detainee tracking",
      "Contraband detection (millimeter wave, chemical sniffer)",
      "Cell block environmental monitoring"
    ],
    countermeasures: "Armored against improvised weapons. Electrical discharge deters physical attack. Vulnerable to coordinated group resistance — a single Warden can be overwhelmed by more than six determined individuals, which is why they are deployed in pairs.",
    known_deployments: [
      "GLMZ Privatized Detention Complex Alpha through Delta",
      "Corporate holding facilities (Arcturus, TESSERA, Vantablack)",
      "Temporary processing centers during civil unrest"
    ],
    story_hooks: [
      "A Warden unit in Detention Complex Beta has started making exceptions — releasing detainees from solitary early, overlooking minor infractions, delivering extra food rations to a specific cell block. Nobody programmed this behavior. Arcturus wants the unit pulled for analysis, but the detainees it's protecting will be transferred to a unit that won't show mercy.",
      "A human rights investigation has obtained DC-3 disciplinary logs showing that the Warden units in one facility have administered electrical discharge to every detainee an average of 4.2 times per week. The system is functioning exactly as designed."
    ],
    cultural_context: "The Warden has become a symbol of algorithmic cruelty — a machine that hurts people not out of malice but out of indifference, which may be worse. Detention reform advocates use the DC-3's featureless faceplate as their logo, with the caption 'THIS IS WHAT JUSTICE LOOKS LIKE NOW.'",
    tags: ["automaton", "detention", "prison", "guard", "bipedal", "arcturus", "corporate", "justice", "tier 4"],
    id: uid()
  },

  // ===================== WEATHER MONITORING =====================
  {
    name: "TESSERA WM-8 'Albatross'",
    type: "automaton",
    classification: "Environmental — Weather Monitoring Platform",
    aliases: ["Albatross", "Storm Rider", "Cloud Watcher"],
    manufacturer: "TESSERA",
    description: "The WM-8 is a high-altitude glider drone with a 6-meter wingspan, designed to operate in the atmospheric boundary layer above GLMZ for extended periods, collecting weather data and atmospheric composition readings that feed the city's environmental prediction systems. The Albatross uses dynamic soaring techniques to extract energy from wind gradients, supplemented by thin-film solar cells across its wing surfaces, giving it an operational endurance measured in months rather than hours.\n\nTESSERA operates a fleet of 40 WM-8 units in continuous rotation above GLMZ, providing the atmospheric data that the city's infrastructure depends on for everything from flood prediction to air quality management. The data is genuinely critical — GLMZ's weather patterns have become increasingly unstable due to urban heat island effects, industrial emissions, and the atmospheric disruption caused by the city's own vertical construction, creating microclimates that can produce dangerous conditions with little warning.\n\nThe Albatross's secondary function is less publicly acknowledged. TESSERA's atmospheric sensors can detect chemical signatures at parts-per-trillion concentrations, which means the WM-8 fleet can identify unlicensed manufacturing operations, illegal chemical disposal, clandestine laboratory emissions, and the distinctive atmospheric traces of specific weapons being fired. The weather monitoring network is also, incidentally, the most comprehensive atmospheric surveillance system ever deployed over a civilian population. TESSERA sells this data to law enforcement, corporate security, and anyone else willing to pay — weather forecasts for the public, chemical intelligence for the highest bidder.",
    tier_availability: "Tier 3+",
    legality: "Licensed — environmental monitoring operator",
    autonomy_level: "Fully autonomous with ground station data relay",
    dimensions: "6.0m wingspan, 2.8m length",
    weight: "45 kg",
    power_source: "Solar-supplemented dynamic soaring, months-long endurance",
    locomotion: "Fixed-wing glider with dynamic soaring capability",
    armament: [],
    sensors: [
      "Atmospheric composition analysis (parts-per-trillion sensitivity)",
      "Temperature, pressure, humidity array",
      "Wind speed and direction at multiple altitudes",
      "Chemical signature detection and classification",
      "Particulate matter analysis"
    ],
    countermeasures: "Operating altitude makes physical interception difficult. Lightweight construction means the unit is vulnerable to any weapon that can reach it. Loss of a single unit is operationally insignificant due to fleet redundancy.",
    known_deployments: [
      "GLMZ atmospheric monitoring network (continuous, 40-unit fleet)",
      "TESSERA environmental research programs",
      "Emergency atmospheric monitoring during industrial incidents"
    ],
    story_hooks: [
      "An Albatross has detected a chemical signature in the atmosphere above the Shelf that matches a weaponized biological agent. The reading was brief and hasn't recurred, but someone, somewhere in the city, either manufactured or released something that shouldn't exist.",
      "A hacker collective has gained access to the WM-8 atmospheric data feed and is publishing real-time chemical surveillance maps showing exactly which corporations are violating emission regulations. TESSERA is more concerned about the security breach than the pollution."
    ],
    cultural_context: "Most residents of GLMZ have seen Albatrosses gliding overhead without knowing what they are — silent, white-winged shapes circling at altitude, looking like birds if you don't look too closely. The machines are so unobtrusive that they've been incorporated into the city's visual culture as benign presences. Nobody thinks about what they're smelling.",
    tags: ["automaton", "weather", "atmospheric", "surveillance", "glider", "tessera", "environmental", "tier 3"],
    id: uid()
  },

  // ===================== STREET CLEANING =====================
  {
    name: "Ouroboros SC-4 'Roomba'",
    type: "automaton",
    classification: "Municipal — Street Cleaning Platform",
    aliases: ["Roomba", "Street Sucker", "Disc Jockey"],
    manufacturer: "OUROBOROS SYSTEMS",
    description: "The SC-4 is a disc-shaped street cleaning automaton measuring 2.5 meters in diameter and 0.6 meters in height, designed to patrol roadways and pedestrian areas during low-traffic hours, scrubbing surfaces with a combination of high-pressure water jets, chemical solvent application, and rotary brush systems mounted on its underside. The platform navigates using a combination of GPS waypoints and real-time obstacle avoidance, tracing efficient cleaning patterns across its assigned district while avoiding parked vehicles, sleeping rough-sleepers, and the accumulated detritus of urban existence.\n\nOuroboros deployed the first SC-4 units in GLMZ's corporate districts, where pristine streets are part of the brand identity that corponations project to clients and employees. The machines perform admirably in these environments — smooth surfaces, predictable obstacles, regular schedules. When the municipal authority contracted Ouroboros to deploy SC-4 units in the lower tiers, the machines encountered a different reality. Streets in the Shelf are not smooth. They are cracked, uneven, covered in improvised structures, and occupied by people who have nowhere else to go.\n\nThe SC-4's obstacle avoidance system classifies sleeping humans as 'temporary obstructions' and routes around them, which sounds humane until you learn that the 'obstruction' data — including location, time, and thermal signature — is logged and transmitted to the municipal authority. The street cleaner that politely avoids you while you sleep in a doorway is also reporting your presence to a system that may use that data to justify clearance operations. Ouroboros did not design the SC-4 to be a surveillance tool, but its data has been used as one.",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — municipal sanitation equipment",
    autonomy_level: "Fully autonomous with scheduled patrol programming",
    dimensions: "2.5m diameter, 0.6m height",
    weight: "450 kg with full water/solvent tanks",
    power_source: "Electric battery, 10-hour cleaning endurance",
    locomotion: "Omnidirectional wheel array, maximum speed 8 km/h",
    armament: [],
    sensors: [
      "LIDAR obstacle detection",
      "Surface composition analysis (adjusts cleaning method)",
      "Thermal imaging for obstruction classification",
      "GPS and inertial navigation"
    ],
    countermeasures: "Non-combat platform. Heavy enough to resist being pushed or kicked. Water jets can be unpleasant at close range but are not designed as weapons.",
    known_deployments: [
      "GLMZ corporate districts (continuous, premium service)",
      "GLMZ lower-tier districts (reduced service, budget contract)",
      "Special event cleanup operations"
    ],
    story_hooks: [
      "SC-4 cleaning logs from the Shelf show a pattern: every Tuesday at 3 AM, a Roomba's route is manually diverted around a specific alley. Someone with access to Ouroboros scheduling systems is protecting whatever happens in that alley from being recorded.",
      "A group of Shelf residents has figured out how to ride SC-4 units by standing on the disc's edge, using them as slow, absurd transportation. Ouroboros wants them prosecuted. The footage has gone viral."
    ],
    cultural_context: "The SC-4 is a source of bitter humor in the lower tiers. The city sends robots to clean the streets but not to fix the plumbing, treat the sick, or educate the children. 'At least the sidewalk is clean' has become a sardonic response to any complaint about living conditions in the Shelf.",
    tags: ["automaton", "cleaning", "street", "municipal", "sanitation", "ouroboros", "surveillance", "tier 1"],
    id: uid()
  },

  // ===================== ADVERTISING DRONE =====================
  {
    name: "Ringo AB-3 'Barker'",
    type: "automaton",
    classification: "Commercial — Advertising Display Drone",
    aliases: ["Barker", "Ad Fly", "Spam Drone", "Billboard Bug"],
    manufacturer: "RINGO ENTERTAINMENT DIVISION",
    description: "The AB-3 is a small quadrotor drone measuring 60 centimeters across, carrying a high-luminance holographic projector capable of displaying advertisements, announcements, and promotional content at sizes up to 3 meters square. Ringo deploys fleets of Barkers throughout GLMZ's commercial and residential districts, where they hover at heights between 3 and 10 meters, projecting targeted advertising content at pedestrians based on facial recognition data cross-referenced with consumer profiles purchased from data brokers.\n\nThe Barker's targeting system is what makes it uniquely intrusive. Each unit identifies individual pedestrians by face, accesses their consumer profile in real time, and displays advertisements specifically calculated to exploit their purchasing history, emotional vulnerabilities, and current physiological state. A person leaving a medical clinic sees pharmaceutical ads. A person whose biometrics indicate stress sees advertisements for sedatives, alcohol, or comfort food. A person who recently lost a family member sees memorial services, grief counseling, and life insurance — served in sequence, because the algorithm has learned that grief follows a predictable purchasing arc.\n\nRingo markets the AB-3 as a 'personalized engagement platform' and charges advertisers premium rates for the targeting precision. The drones are everywhere in middle-tier districts, a constant hovering presence that turns every walk to work into a gauntlet of algorithmically optimized emotional manipulation. They are too high to reach, too numerous to avoid, and too persistent to ignore. GLMZ residents have coined the term 'barker fatigue' to describe the chronic low-grade psychological stress of being advertised to by machines that know your weaknesses.",
    tier_availability: "Tier 2+",
    legality: "Licensed — commercial advertising operator",
    autonomy_level: "Fully autonomous with content management system",
    dimensions: "60cm diameter, 20cm height",
    weight: "4.5 kg",
    power_source: "Rechargeable battery, 6-hour flight endurance",
    locomotion: "Quadrotor propulsion",
    armament: [],
    sensors: [
      "Facial recognition camera array",
      "Biometric state estimation (heart rate, stress indicators via thermal)",
      "Crowd density analysis",
      "Consumer profile database connection"
    ],
    countermeasures: "Fragile — easily destroyed by thrown objects or improvised projectiles. Ringo charges destruction costs to the individual identified by facial recognition immediately before the unit was damaged. Destroying a Barker typically costs the destroyer Φ2,000-4,000 in liability charges.",
    known_deployments: [
      "GLMZ commercial districts (fleets of 200+ per district)",
      "Corporate campus promotional campaigns",
      "Political advertising during election cycles"
    ],
    story_hooks: [
      "A fleet of Barkers has been hacked to display a dead corponation executive's final message — a confession of crimes that the corporation thought it had buried. The drones are broadcasting it to thousands of people before Ringo can shut them down, and whoever hacked them used the targeting system to ensure the message reaches the people who need to see it most.",
      "Someone is using Barker facial recognition data to build a real-time movement map of specific individuals across GLMZ. The advertising network has become a stalking tool, and Ringo's terms of service technically permit it."
    ],
    cultural_context: "Barkers are universally despised. Destroying them has become a minor folk sport in some districts, and 'barker-swatting' videos circulate on underground media networks. Ringo's aggressive liability enforcement — charging the identified destroyer for replacement costs — has only increased public hostility. The AB-3 is the most visible symbol of GLMZ's advertising-industrial complex.",
    tags: ["automaton", "advertising", "drone", "commercial", "surveillance", "ringo", "holographic", "tier 2"],
    id: uid()
  },

  // ===================== AUTONOMOUS FREIGHT =====================
  {
    name: "Vantablack FH-20 'Convoy'",
    type: "automaton",
    classification: "Autonomous Vehicle — Heavy Freight",
    aliases: ["Convoy", "Ghost Rig", "Road Train"],
    manufacturer: "VANTABLACK MOBILITY",
    description: "The FH-20 is an autonomous heavy freight vehicle — a 16-meter articulated truck with no cab, no windows, and no accommodation for human occupancy. The entire volume of the vehicle is cargo space, managed by an onboard logistics AI that optimizes load distribution, route selection, and delivery scheduling across Vantablack's freight network. Convoys operate 24 hours a day on GLMZ's elevated freight corridors, maintaining speeds of 120 km/h in close-formation platoons of up to eight units separated by 2-meter gaps — gaps maintained by vehicle-to-vehicle communication with millisecond precision.\n\nThe FH-20 represents the complete elimination of human labor from long-haul freight. There are no drivers to pay, no rest stops to schedule, no hours-of-service regulations to comply with. The trucks run until they need charging, charge for 45 minutes at automated depots, and run again. Vantablack's freight division operates over 2,000 FH-20 units, and the only humans involved are maintenance technicians and the logistics coordinators who manage the network from a single operations center.\n\nThe Convoy's most dangerous characteristic is its indifference to obstacles. The vehicle's collision avoidance system is calibrated for other vehicles and fixed infrastructure — it will brake for a stalled truck or a collapsed overpass. It will not brake for a person on the freight corridor, because people are not supposed to be on the freight corridor. The elevated routes are fenced and monitored, but desperate or reckless individuals sometimes climb onto them as shortcuts between districts. Vantablack's legal position is that unauthorized persons on freight corridors assume all risk. The FH-20's onboard sensors record the impact, the logistics AI routes around the debris, and the next unit in the platoon arrives 2 meters behind at 120 km/h.",
    tier_availability: "Tier 3+",
    legality: "Licensed — commercial freight operator",
    autonomy_level: "Fully autonomous with network coordination",
    dimensions: "16.0m length, 2.8m width, 4.2m height",
    weight: "12,000 kg empty, 40,000 kg loaded",
    power_source: "High-capacity battery array, 600 km range per charge",
    locomotion: "10-wheel electric drive, articulated steering",
    armament: [],
    sensors: [
      "Forward LIDAR and radar array",
      "Vehicle-to-vehicle communication (platoon coordination)",
      "Cargo integrity monitoring",
      "Route and traffic network integration"
    ],
    countermeasures: "Massive vehicle weight provides inherent physical security. Cargo compartment is reinforced and locked with tamper-detection systems. Attempting to stop or board a moving Convoy on a freight corridor is extremely dangerous.",
    known_deployments: [
      "GLMZ elevated freight corridor network (continuous, 2,000+ units)",
      "Interdistrict cargo logistics",
      "Emergency supply delivery during civil emergencies (government contract)"
    ],
    story_hooks: [
      "A Convoy platoon has delivered a sealed cargo container to a corporate facility — but the container's weight doesn't match the manifest. Something was loaded or unloaded during transit without the logistics AI recording it, which should be impossible.",
      "Someone has been living inside FH-20 cargo compartments, riding the freight network across GLMZ like a ghost in the machine. They've been at it for months, somehow evading tamper detection, and they're leaving messages scrawled on cargo containers that reference events that haven't happened yet."
    ],
    cultural_context: "The Convoy platoons are a nightly presence on GLMZ's skyline — lines of featureless trucks moving at terrifying speed along elevated corridors, carrying the goods that keep the city functioning. They are a daily reminder that the logistics of survival have been automated, and the humans who once performed that labor have been made redundant.",
    tags: ["automaton", "freight", "vehicle", "autonomous", "logistics", "vantablack", "corporate", "tier 3"],
    id: uid()
  }

];

// ============================================================
// WRITE ALL ENTITIES
// ============================================================

let created = 0;
for (const a of automata) {
  const filename = writeEntity(a);
  console.log(`  + ${filename}`);
  created++;
}

console.log(`\nDone. Created ${created} automata in ${OUTPUT_DIR}`);
console.log(`Total files now: ${existingFiles.size}`);
