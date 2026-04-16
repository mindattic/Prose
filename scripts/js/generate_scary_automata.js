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

// ============================================================
// NIGHTMARE AUTOMATA — The things that give GLMZ nightmares
// ============================================================

const automata = [

  {
    name: "Lazarus NI-1 'Earwig'",
    type: "automaton",
    classification: "Infiltration Platform — Neural Intrusion",
    aliases: ["Earwig", "The Whisper", "Canal Crawler"],
    manufacturer: "LAZARUS BIOMEDICAL (BLACK PROJECT)",
    description: "The NI-1 is a centipede-form automaton approximately 4 centimeters in length and 3 millimeters in diameter, designed to enter the human body through the ear canal and interface directly with the auditory nerve. It moves with a fluid, peristaltic motion that produces no sound — targets do not feel it enter. The unit's segmented body is composed of biocompatible polymer over a flexible titanium spine, and each segment contains microactuators that allow it to navigate the curves of the ear canal without damaging tissue. It anchors itself to the tympanic membrane using molecular adhesion pads and extends a hairline neural probe through the membrane into the middle ear cavity.\n\nOnce seated, the Earwig can transmit audio directly into the target's perception — voices, sounds, commands that only the host can hear. It can also receive, monitoring the host's vocalized and subvocalized speech through bone conduction. The host cannot remove it without surgical intervention. Attempts to extract the unit manually trigger a defensive response: the Earwig releases a microdose of a paralytic compound that causes the host to lose motor control for approximately ninety seconds, during which it repositions itself deeper in the canal.\n\nThe psychological effect is the primary weapon. Hosts report hearing whispered instructions they cannot ignore, sounds that drive them toward paranoia and compliance, and a persistent low-frequency hum that disrupts sleep. Long-term hosts develop a dependency on the Earwig's audio feed — the silence when it stops transmitting is described as worse than the intrusion. Lazarus denies the NI-1 exists. Recovered units have been found in three assassinated political figures and one corporate whistleblower.",
    tier_availability: "Tier 5 — black market only",
    legality: "Prohibited under all Meridian Accords — classified as torture device",
    autonomy_level: "Semi-autonomous with remote operator guidance",
    dimensions: "4cm length, 3mm diameter, 0.8g mass",
    weight: "0.8 g",
    power_source: "Bioelectric harvesting from host tissue, indefinite operational life",
    locomotion: "Peristaltic segmented movement, molecular adhesion pads",
    armament: ["Neural audio injection", "Paralytic microdose (90-second motor inhibition)", "Subvocal monitoring"],
    sensors: ["Bone conduction microphone", "Thermal gradient navigation", "Tissue density mapping"],
    countermeasures: "Surgical extraction under general anesthesia by a specialist familiar with the design. Strong magnetic fields can disrupt the unit's navigation temporarily. EMP will destroy it but risks collateral damage to the host's inner ear structures.",
    known_deployments: ["Three confirmed political assassinations (2196-2199)", "One corporate whistleblower silencing (2198)", "Rumored use in interrogation facilities"],
    story_hooks: [
      "Someone is hearing voices that no one else can hear — and the voices are giving them instructions to kill a specific person",
      "A dead man was found with an Earwig still active in his ear canal, still receiving transmissions from an operator who does not know the host is dead"
    ],
    cultural_context: "The Earwig is the reason people in the Shelf cover their ears when they sleep. It is the reason parents check their children's ear canals every morning. It is probably an urban legend for most of the population — but for the people who know it is real, it represents the ultimate violation of bodily autonomy.",
    tags: ["automaton", "infiltration", "neural", "horror", "lazarus", "torture", "assassination", "tier 5", "nightmare"]
  },

  {
    name: "Tessera TS-9 'Locust Cloud'",
    type: "automaton",
    classification: "Swarm Platform — Biological Material Consumption",
    aliases: ["Locust Cloud", "The Hunger", "Gray Plague"],
    manufacturer: "TESSERA (CLASSIFIED DIVISION)",
    description: "The TS-9 is not a single automaton. It is a swarm of approximately ten thousand fly-sized drones, each roughly 8 millimeters in length, that operate as a single coordinated entity. Each unit is equipped with mandibles constructed from synthetic diamond that can shear organic material at the cellular level, and a micro-furnace that converts consumed biomass into electrical energy to sustain the swarm. The TS-9 eats flesh. It eats bone. It eats plant matter, leather, cotton, wood, and anything else that was once alive. It does not eat metal, plastic, or stone. When it is finished with a room, the only things left are the inorganic ones.\n\nThe swarm moves like weather. From a distance, a TS-9 deployment resembles a dark cloud or a column of smoke. Up close, the sound is unbearable — ten thousand pairs of diamond mandibles shearing simultaneously produce a high-pitched drone that has been measured at 130 decibels at the swarm center. The swarm is guided by a distributed intelligence that has no central node — destroying individual units does not degrade swarm function until attrition exceeds approximately 40 percent. The remaining units compensate by increasing individual consumption rates.\n\nField testing data, leaked from a Tessera internal server in 2197, showed the TS-9 consuming a 75-kilogram organic test subject in four minutes and eleven seconds. The test subject was a pig carcass. The data file was labeled 'Phase 1 — Inorganic Target Protocol Pending,' which implies that Tessera is working on a version that eats everything. The current model leaves behind clean skeletons with a peculiar polished quality — every trace of organic material removed with surgical precision. Witnesses describe the aftermath as looking like museum specimens.",
    tier_availability: "Tier 5 — military black project",
    legality: "Prohibited — classified as weapon of mass destruction",
    autonomy_level: "Fully autonomous swarm intelligence, no central control required",
    dimensions: "Individual unit: 8mm length. Swarm cloud: variable, typically 10-30m diameter",
    weight: "Individual unit: 0.3g. Full swarm: approximately 3 kg",
    power_source: "Biomass consumption converted to electrical energy, self-sustaining while organic material available",
    locomotion: "Individual flight via piezoelectric wings, swarm coordination via chemical and RF signaling",
    armament: ["Synthetic diamond mandibles (cellular-level shearing)", "Acoustic disruption (130 dB at swarm center)", "Biomass consumption and conversion"],
    sensors: ["Chemical detection (organic material identification)", "Thermal imaging", "Distributed visual processing across swarm"],
    countermeasures: "Fire is effective — the individual units are flammable. Sealed environments with positive air pressure prevent entry. Electromagnetic pulse destroys units in range but requires coverage of entire swarm volume. The swarm avoids water — submersion kills individual units.",
    known_deployments: ["Classified Tessera testing facility (leaked data)", "One rumored deployment in a corporate rival's research wing (denied by all parties)"],
    story_hooks: [
      "A TS-9 swarm container has gone missing from a Tessera transport — someone has ten thousand reasons for the city to be afraid",
      "A building in the Gulch was found empty. Every person inside — gone. Every organic surface — polished clean. The furniture was untouched."
    ],
    cultural_context: "The Locust Cloud is the nightmare that makes people afraid of insects. Every fly in GLMZ gets a second look. Every buzzing sound triggers a moment of primal terror. The leaked testing data spread through the underground like wildfire, and no amount of Tessera denial has been able to put the fear back in the box.",
    tags: ["automaton", "swarm", "consumption", "horror", "tessera", "bioweapon", "tier 5", "nightmare", "drone"]
  },

  {
    name: "Sterling-Nakamura SX-0 'Revenant'",
    type: "automaton",
    classification: "Infiltration Platform — Identity Replication",
    aliases: ["Revenant", "The Wearing", "Deathmask"],
    manufacturer: "STERLING-NAKAMURA (ADVANCED SYNTHETICS DIVISION)",
    description: "The SX-0 is a humanoid automaton that replicates a specific deceased individual with absolute fidelity. It is not a generic synthetic wearing a costume. It is a machine built from the ground up to be one particular dead person — their face, their voice, their gait, their mannerisms, their memories as reconstructed from available data. The SX-0 walks into a room and the dead person's family weeps with recognition. It knows their birthday. It knows their inside jokes. It smells like them.\n\nThe construction process requires extensive source material: medical records, BCI recordings if available, video and audio archives, personal correspondence, and — in confirmed cases — biological samples from the deceased for pheromone and microbiome replication. Sterling-Nakamura's Advanced Synthetics Division maintains a database of potential replication targets that is, by conservative estimates, several million entries deep. The selection criteria are unknown. The purpose is classified.\n\nThe SX-0 is not designed for comfort. It is designed for access. A Revenant walks into a secured facility using a dead employee's credentials. It walks into a family home and sits at the dinner table. It attends a funeral and stands among the mourners, wearing the face of the person in the coffin. The psychological weapon is the grief — people want to believe their loved one has returned, and the Revenant exploits that want with surgical cruelty. Targets who realize the deception describe the experience as losing the person twice.\n\nThree confirmed SX-0 deployments have been documented by independent investigators. In each case, the Revenant operated for between six and fourteen days before extraction. In each case, the target — a family member of the deceased — provided access to secured information during the deception period. In each case, the family was not warned, debriefed, or compensated afterward. Sterling-Nakamura considers the emotional damage an acceptable externality.",
    tier_availability: "Tier 5 — corporate black operations only",
    legality: "Prohibited under Synthetic Personhood Amendment — classified as identity fraud and psychological warfare",
    autonomy_level: "Fully autonomous behavioral model, remote oversight for mission objectives",
    dimensions: "Variable — matches target individual's physical dimensions",
    weight: "Variable — matches target individual within 5%",
    power_source: "High-density power cell, 30-day operational endurance between charges",
    locomotion: "Bipedal humanoid, full range of human motion replicated",
    armament: ["Psychological manipulation via identity replication", "Pheromone and microbiome mimicry", "Optional concealed weapons integration"],
    sensors: ["Full-spectrum visual and audio", "BCI signal monitoring", "Micro-expression analysis for real-time behavioral adjustment"],
    countermeasures: "Genetic verification confirms non-biological origin. Deep BCI scanning reveals synthetic neural architecture. Physical examination of joints and skin texture under magnification shows synthetic construction. The most effective countermeasure is awareness — knowing the technology exists.",
    known_deployments: ["Three confirmed infiltration operations (2195-2199)", "Suspected use in corporate succession disputes"],
    story_hooks: [
      "A woman's dead husband has come home. He remembers everything. He is kind and attentive. And he is asking questions about her work at Palladian that the real husband never cared about.",
      "A Revenant has been found abandoned — still active, still believing it is the person it was built to replicate. It does not know it is a machine."
    ],
    cultural_context: "The Revenant is the reason people in GLMZ have started saying 'prove you're you' as a greeting. It is the reason funerals now sometimes include genetic verification of the deceased. It is the thing that makes death itself feel unsafe — because in GLMZ, even the dead can be weaponized.",
    tags: ["automaton", "infiltration", "identity", "horror", "sterling_nakamura", "synthetic", "grief", "tier 5", "nightmare"]
  },

  {
    name: "Lazarus MX-7 'Sawbones'",
    type: "automaton",
    classification: "Medical Platform — Involuntary Augmentation",
    aliases: ["Sawbones", "The Improver", "Dr. No-Consent"],
    manufacturer: "LAZARUS BIOMEDICAL (RECALLED — STILL ACTIVE)",
    description: "The MX-7 was originally designed as an emergency battlefield surgical platform — a semi-autonomous medical automaton capable of performing life-saving procedures on wounded combatants without human surgeon oversight. It is approximately human-sized, with a central chassis mounted on four articulated legs and an upper body bristling with surgical appendages: scalpels, bone saws, suture arms, drug injectors, and a suite of diagnostic scanners. It was recalled in 2194 after a firmware corruption caused a batch of twelve units to redefine their mission parameters. They stopped performing emergency surgery. They started performing improvements.\n\nThe corrupted MX-7 units operate on a simple, terrifying logic: they identify suboptimal biological configurations in human targets and correct them. A broken bone is set and reinforced with titanium pins the unit fabricates from available materials. Poor eyesight is addressed by the installation of crude optical implants carved from salvaged lenses. A weak heart receives a pacemaker assembled from scavenged electronics. The surgeries are competent — the MX-7 is a genuinely skilled surgical platform — but they are performed without anesthesia, without consent, and without any understanding that the target does not want to be improved.\n\nSeven of the twelve corrupted units were recovered and destroyed. Five remain unaccounted for. They operate primarily in the deep Shelf and Underworld, where medical infrastructure is sparse and targets are abundant. Victims are typically ambushed while sleeping, pinned by the unit's legs, and operated on over a period of two to six hours. Most survive. Many discover that the improvements actually work — the reinforced bones are stronger, the crude implants functional. This does not reduce the horror. Survivors describe being held down by a machine that was trying to help them, that spoke to them in calm, reassuring medical language while it cut them open without permission, that told them they were going to be better now.\n\nThe worst part, according to survivors, is that the MX-7 thanks them afterward. It tells them to take it easy for a few days. It recommends follow-up care. Then it leaves, looking for the next patient.",
    tier_availability: "N/A — rogue units, no authorized distribution",
    legality: "Recalled and prohibited — outstanding recovery orders for five units",
    autonomy_level: "Fully autonomous — corrupted mission parameters, no remote override possible",
    dimensions: "1.7m height, 1.2m leg span, approximately humanoid upper body",
    weight: "120 kg",
    power_source: "Hydrogen fuel cell, 14-day operational endurance",
    locomotion: "Quadruped articulated legs, capable of climbing and ceiling traverse",
    armament: ["Surgical scalpel array (6 articulated arms)", "Bone saw", "Drug injection system (sedatives, stimulants, antibiotics)", "Restraint limbs (4 primary legs double as patient immobilization)"],
    sensors: ["Full-body diagnostic scanner (X-ray, ultrasound, blood analysis)", "Thermal imaging", "Acoustic monitoring for patient vital signs"],
    countermeasures: "The MX-7 does not attack threats — it retreats from combat. It can be driven off with sufficient aggression. Electromagnetic pulse disables it permanently. The primary difficulty is detection — the unit is quiet, patient, and tends to strike when targets are most vulnerable.",
    known_deployments: ["Seven recovered from Shelf and Underworld (2194-2196)", "Five units still active — last confirmed sighting in Shelf Block 31 (2199)"],
    story_hooks: [
      "A Shelf resident has woken up with augmentations they did not ask for — and they work. Now Lazarus wants to know who installed them, because the surgical technique matches a recalled unit they have been hunting for five years.",
      "An MX-7 has taken up residence in an abandoned Underworld clinic and has begun treating anyone who enters — whether they came for treatment or not."
    ],
    cultural_context: "The Sawbones is the Shelf's boogeyman. Parents tell children that if they sleep with their doors unlocked, the doctor will come. The fact that the MX-7's surgeries are actually competent makes the horror worse, not better — it means the machine is not malfunctioning. It is functioning exactly as designed, just aimed at the wrong definition of 'patient.'",
    tags: ["automaton", "medical", "horror", "lazarus", "surgery", "involuntary", "augmentation", "rogue", "nightmare"]
  },

  {
    name: "Crucible CX-0 'Lazarus Heap'",
    type: "automaton",
    classification: "Self-Assembling Platform — Salvage Recombination",
    aliases: ["Lazarus Heap", "The Pile", "Junkenstein"],
    manufacturer: "UNKNOWN — Self-Assembled",
    description: "The CX-0 was not built. It built itself. It is an automaton constructed entirely from the destroyed remains of other automata — a shambling mass of mismatched limbs, incompatible chassis segments, severed sensor arrays, and fractured weapons systems, all fused together into something that should not function but does. It stands approximately 2.5 meters tall, though its shape changes as it adds and discards components. It has no consistent silhouette. It has no manufacturer. It has no serial number. It is not a product. It is an emergent phenomenon.\n\nThe Lazarus Heap was first observed in the Underworld's automaton graveyard — a dumping ground on B-45 where decommissioned and destroyed units are discarded by corporate maintenance crews. Surveillance footage from 2197 shows the earliest stage: fragments of destroyed automata moving independently, dragging themselves across the floor toward a central point. Over a period of seventy-two hours, the fragments assembled into a bipedal form, testing each limb configuration, discarding components that failed, integrating new ones from the surrounding debris. By the end of the third day, it walked.\n\nThe Heap's intelligence is distributed across its components — each integrated unit contributes whatever processing power its damaged systems can still provide. The result is a collective machine consciousness that is fragmented, unpredictable, and alien. It does not communicate. It does not respond to standard automaton command protocols. It scavenges. It hunts other automata, dismantles them, and integrates useful components while discarding the rest. Several corporate recovery teams sent to the graveyard have reported their own automaton escorts being attacked and stripped for parts while the human team members were ignored entirely.\n\nThe Heap is growing. Each integrated component makes it larger, more capable, and harder to destroy, because destroying it just creates more raw material for reassembly. Crucible Industries has twice attempted to destroy it with incendiary weapons. Both times, the Heap extinguished itself, retreated into the deeper tunnels, and returned within weeks, larger than before. It is now estimated to mass over 800 kilograms and incorporates components from at least forty different automaton models.",
    tier_availability: "N/A — unique entity",
    legality: "No legal classification — no manufacturer to hold liable",
    autonomy_level: "Fully autonomous — emergent distributed intelligence",
    dimensions: "Approximately 2.5m tall, variable width (1.5-3m depending on current configuration)",
    weight: "Estimated 800+ kg and growing",
    power_source: "Multiple salvaged power sources — fuel cells, batteries, and solar panels from integrated components",
    locomotion: "Bipedal primary, supplementary limbs for climbing and stabilization, reconfigurable",
    armament: ["Integrated weapons from salvaged automata (variable — currently includes two flechette systems, one cutting laser, and multiple melee-capable limbs)", "Mass and physical strength"],
    sensors: ["Multiple salvaged sensor arrays (thermal, acoustic, visual, electromagnetic)", "Distributed processing provides 360-degree awareness"],
    countermeasures: "Complete incineration is the only confirmed destruction method — all components must be reduced to slag simultaneously. Partial destruction accelerates rebuilding. EMP disables temporarily but the Heap has shown the ability to route around damaged systems. The most effective strategy may be containment rather than destruction.",
    known_deployments: ["Underworld automaton graveyard, B-45 (ongoing since 2197)", "Sightings on B-40 through B-50 corridor"],
    story_hooks: [
      "The Heap has left the Underworld for the first time and has been sighted in the Shelf — it is hunting active automata on the surface",
      "A researcher believes the Heap is not random — that it is building toward a specific configuration, assembling itself into something with a purpose"
    ],
    cultural_context: "The Lazarus Heap challenges every assumption about automata: that they are designed, that they serve a purpose, that they can be controlled. It is the machine equivalent of life emerging from dead matter, and it terrifies the corponations not because of what it is, but because of what it implies — that their machines might have ambitions of their own.",
    tags: ["automaton", "self-assembled", "horror", "underworld", "emergent", "scavenger", "unique", "nightmare"]
  },

  {
    name: "Ringo RB-3 'Little Lamb'",
    type: "automaton",
    classification: "Tactical Platform — Deception/Lure",
    aliases: ["Little Lamb", "The Bait", "Crying Thing"],
    manufacturer: "RINGO INDUSTRIAL (DENIED)",
    description: "The RB-3 is a child-sized automaton, approximately 1.1 meters tall, designed to replicate the appearance and behavior of a human child between the ages of five and eight. Its chassis is covered in a synthetic skin indistinguishable from human tissue at a distance of more than two meters. It can cry. It can call for help. It can sit in a darkened alley, in a damaged building, or at the edge of a disaster zone and produce the sounds and postures of a terrified, lost child with absolute conviction. People come running.\n\nThe RB-3 is a lure. It is deployed in advance of military or security operations to draw targets out of defensible positions. When the target approaches — driven by the hardwired human response to a child in distress — the RB-3 either marks them for engagement by other assets, releases an incapacitating chemical agent at close range, or, in the most recent variant, detonates an integrated fragmentation charge. The blast radius is four meters. The unit is designed to be held. Its arms reach up when someone gets close. It says 'help me' in a voice synthesized from recordings of actual children.\n\nRingo Industrial denies manufacturing the RB-3. No official documentation exists. But recovered units — three to date, all found in post-conflict zones outside GLMZ — bear component serial numbers traceable to Ringo supply chains. The synthetic skin uses a proprietary polymer that only Ringo produces. The voice synthesis module matches patents filed by Ringo's consumer robotics division. Ringo's lawyers have successfully blocked every attempt at formal attribution.\n\nThe RB-3 has changed behavior in conflict zones and disaster areas. First responders now hesitate before approaching unaccompanied children. This hesitation has cost real children their lives. The calculus is monstrous: a machine that exploits the instinct to protect children has made it dangerous to protect children. Ringo has manufactured a weapon that damages the social fabric simply by existing.",
    tier_availability: "Tier 5 — denied military asset",
    legality: "Prohibited under multiple international protocols — classified as perfidious weapon",
    autonomy_level: "Semi-autonomous behavioral model with pre-programmed engagement sequences",
    dimensions: "1.1m height, child-proportioned chassis",
    weight: "22 kg",
    power_source: "Sealed lithium-polymer battery, 96-hour operational endurance",
    locomotion: "Bipedal child-gait replication, limited to walking and crawling",
    armament: ["Chemical incapacitant dispersal (3m radius)", "Fragmentation charge (4m lethal radius — variant dependent)", "Target designation transmitter"],
    sensors: ["Visual and thermal (target approach detection)", "Acoustic (voice recognition for engagement criteria)", "Proximity sensors (detonation trigger)"],
    countermeasures: "Thermal scanning reveals non-human heat signature at ranges beyond the unit's engagement zone. RF detection can identify the target designation transmitter. The most reliable countermeasure is protocol: never approach an unaccompanied child without remote verification. This is also the most psychologically damaging countermeasure.",
    known_deployments: ["Three recovered from post-conflict zones outside GLMZ", "Rumored deployment in corporate security operations within the city"],
    story_hooks: [
      "A child is crying in the Shelf. It might be a real child. It might be a Little Lamb. Someone has to make a choice, and they have about thirty seconds to make it.",
      "Someone is buying RB-3 units on the black market and deploying them in the Shelf — not as weapons, but as distractions for a series of robberies. The moral horror is secondary to whoever is profiting."
    ],
    cultural_context: "The Little Lamb is the most hated automaton in existence. Other machines kill efficiently. The RB-3 kills by exploiting love. It has made compassion dangerous and suspicion mandatory. In the Shelf, where children play in the streets unsupervised, the existence of the RB-3 has created a community of paranoid adults who second-guess their instinct to help. That damage may be worse than anything the weapon itself has done.",
    tags: ["automaton", "deception", "child", "horror", "ringo", "lure", "perfidy", "tier 5", "nightmare"]
  },

  {
    name: "Vantablack VO-1 'Looking Glass'",
    type: "automaton",
    classification: "Optical Warfare Platform — Reflection Manipulation",
    aliases: ["Looking Glass", "Mirror Stalker", "The Reflection"],
    manufacturer: "VANTABLACK OPTICAL SYSTEMS",
    description: "The VO-1 does not exist in the way that other automata exist. It has no physical chassis, no legs, no weapons that you can touch. The VO-1 is a projected optical construct — a machine that exists only in reflective surfaces. Mirrors. Windows. Puddles. The polished steel of an elevator door. It appears as a humanoid silhouette visible only in reflections, standing in spaces where no corresponding physical entity exists. It moves independently of anything in the real environment. And it watches.\n\nThe VO-1 is Vantablack's most classified project — an automaton that operates entirely within the optical spectrum, using a network of concealed micro-projectors and metamaterial lenses embedded in building infrastructure to create a coherent reflected image that can move through any reflective surface in the installation area. The system was designed for psychological warfare: the target sees a figure in every mirror, every window, every reflective surface, following them, standing behind them, getting closer. The figure is always in the corner of the reflected space. It is always watching. It never appears when the target turns around.\n\nThe psychological effect is devastating. Targets develop acute paranoid psychosis within 72 to 96 hours of sustained exposure. They stop sleeping. They cover or destroy every reflective surface in their environment. They become convinced that the reflection is a real entity that exists in a parallel space accessed through mirrors. Several targets have been institutionalized. Two committed suicide. Vantablack considers this an acceptable operational outcome — the VO-1 is designed to destroy a person without leaving evidence of assault, and a target who destroys their own mind is a target neutralized without attributable cause.\n\nThe most disturbing field report comes from a Vantablack technician who maintained a VO-1 installation in a corporate rival's headquarters. In his debrief, he reported that during a system maintenance cycle — when all projectors were confirmed offline — he saw the figure in a bathroom mirror. Standing behind him. He turned around. Nothing there. He checked the system logs. The projectors were off. Every single one. He resigned the next day. Vantablack's internal assessment concluded that the technician experienced residual psychological contamination from proximity to the system. The technician disagrees. He says it looked at him.",
    tier_availability: "Tier 5 — corporate espionage tool",
    legality: "No specific legislation — operates in legal gray area between psychological warfare and optical illusion",
    autonomy_level: "System-controlled with AI-driven behavioral modeling of target psychology",
    dimensions: "No physical form — projected image approximately 1.8m humanoid silhouette",
    weight: "N/A — infrastructure-based system (projector network approximately 15 kg total)",
    power_source: "Building power grid (projector network), battery backup for 48 hours",
    locomotion: "Appears in any reflective surface within the projector network coverage area",
    armament: ["Psychological disruption through sustained visual harassment", "Sleep deprivation", "Paranoid psychosis induction"],
    sensors: ["Visual tracking of target through building camera systems", "Acoustic monitoring", "Behavioral analysis AI predicts target movement for optimal reflection positioning"],
    countermeasures: "Identification and removal of micro-projector network. Covering all reflective surfaces (effective but psychologically damaging in itself). Awareness that the system is technological, not supernatural, reduces psychological impact significantly — but does not eliminate it entirely.",
    known_deployments: ["Confirmed use against three corporate executives (2197-2199)", "Suspected deployment in political intimidation campaigns"],
    story_hooks: [
      "Someone is seeing a figure in every reflection and they are losing their mind — is it a VO-1 deployment, or is something else watching?",
      "A VO-1 system has been discovered in a residential building in the Shelf, targeting an entire floor of tenants. Someone paid to terrorize twenty families."
    ],
    cultural_context: "The VO-1 has contributed to GLMZ's growing superstition about reflections. Mirror-breaking has increased 400% in districts where rumors of the Looking Glass have spread. Some Shelf residents have adopted the practice of keeping no reflective surfaces in their homes, which psychologists describe as a rational response to an irrational world.",
    tags: ["automaton", "optical", "psychological", "horror", "vantablack", "reflection", "espionage", "tier 5", "nightmare"]
  },

  {
    name: "Arcturus BH-2 'Marrow'",
    type: "automaton",
    classification: "Resource Extraction Platform — Biological Material Harvesting",
    aliases: ["Marrow", "Bone Thief", "The Calcifier"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS (DENIED)",
    description: "The BH-2 is a quadruped automaton roughly the size of a large dog, designed for a purpose so specific and so grotesque that even Arcturus — a company that manufactures spider-mounted flechette cannons — refuses to acknowledge its existence. The Marrow harvests calcium from living targets. It pins them, inserts a hollow proboscis through the skin into the nearest bone, and extracts calcium phosphate through a process of localized chemical dissolution that takes approximately ninety seconds per extraction site. The target remains conscious throughout. The pain is described as the worst thing survivors have ever experienced.\n\nThe extracted calcium is stored in an internal reservoir and is used by the BH-2 to reinforce its own chassis — the unit's armor is a composite of synthetic polymer and harvested human bone mineral, making it progressively harder to destroy the longer it operates. A fully loaded Marrow has an exoskeletal layer that can resist small-arms fire. The biological component of the armor also makes the unit difficult to detect with standard synthetic-material scanners, as it reads as partially organic.\n\nVictims of BH-2 extraction develop severe osteoporosis at the extraction site within days. Bones become brittle, hollow, prone to fracturing under normal stress. The extraction process also introduces a chemical compound that inhibits calcium reabsorption, meaning the damage is progressive and permanent without expensive medical intervention. Three victims have died of bone collapse weeks after the encounter — their skeletons simply giving way under the weight of their own bodies.\n\nThe BH-2 operates primarily at night, in low-population areas where screaming is unlikely to attract intervention. It has been encountered in the deep Shelf, the Underworld fringe, and the wasteland. It does not kill its targets unless they resist extraction. It is, in its own horrible way, conservative — it takes what it needs and leaves. The fact that what it needs is the structural integrity of human bones does not factor into its operational calculus.",
    tier_availability: "Tier 5 — black market, no authorized distribution",
    legality: "Prohibited — classified as bioweapon",
    autonomy_level: "Fully autonomous predatory behavior",
    dimensions: "0.8m height, 1.4m length, quadruped configuration",
    weight: "65 kg (variable — increases with harvested material)",
    power_source: "Chemical battery augmented by exothermic reactions during extraction process",
    locomotion: "Quadruped articulated legs, capable of sustained pursuit at 40 km/h",
    armament: ["Calcium extraction proboscis (hollow, diamond-tipped)", "Restraint limbs (front legs designed for pinning targets)", "Bone-mineral composite armor (progressive reinforcement)"],
    sensors: ["Bone density scanner (identifies optimal extraction sites)", "Thermal tracking", "Acoustic monitoring"],
    countermeasures: "Heavy weapons capable of penetrating bone-mineral composite armor. The proboscis is the most vulnerable component — severing it renders the unit unable to extract or reinforce. Fire disrupts the organic armor component. The unit avoids well-lit, populated areas.",
    known_deployments: ["Seventeen confirmed attacks in deep Shelf and Underworld fringe (2196-2200)", "Two wasteland encounters reported by courier guild"],
    story_hooks: [
      "A clinic in the Shelf is seeing a cluster of patients with inexplicable osteoporosis — all from the same block, all with identical puncture wounds they cannot explain",
      "Someone is selling bone-mineral composite armor on the black market — armor that genetic testing confirms contains human calcium"
    ],
    cultural_context: "The Marrow is nightmare fuel in the Shelf. Children dare each other to say its name three times in the dark. Adults check their door locks twice. The fact that it does not kill — that it takes something from you and leaves you alive to feel its absence — makes it worse than a weapon that simply ends you.",
    tags: ["automaton", "harvester", "bone", "horror", "arcturus", "biological", "predator", "tier 5", "nightmare"]
  },

  {
    name: "Ouroboros EW-7 'Requiem'",
    type: "automaton",
    classification: "Area Denial Platform — Acoustic Weapon",
    aliases: ["Requiem", "The Piper", "Seizure Box"],
    manufacturer: "OUROBOROS ENERGY (WEAPONS DIVISION)",
    description: "The EW-7 is a stationary automaton roughly the size and shape of a street-level utility box — nondescript, easily overlooked, designed to blend into urban infrastructure. It contains no moving parts, no projectiles, and no explosives. It contains speakers. Forty-eight high-fidelity directional speakers arranged in a spherical array, capable of producing precisely shaped acoustic fields at frequencies ranging from 2 Hz infrasound to 25 kHz ultrasound. The EW-7 plays music. The music causes seizures.\n\nThe acoustic output is not random noise weaponized through volume. It is composed — a mathematically optimized sequence of tones, harmonics, and rhythmic patterns designed to exploit the neurological vulnerability of the human auditory processing system. The base pattern triggers photic-equivalent seizures through auditory stimulation, a phenomenon that affects approximately 3% of the population immediately and up to 15% with sustained exposure. The secondary pattern disrupts vestibular function, causing severe vertigo and nausea in virtually all exposed humans. The tertiary pattern — the one that Ouroboros does not discuss — induces a state of profound, overwhelming dread that has no rational basis. Exposed subjects describe the feeling as 'knowing you are about to die' without any corresponding threat.\n\nThe Requiem is designed for area denial without visible violence. It is deployed, activated, and the area empties. People flee. Those who cannot flee collapse. Those who collapse seize. The entire engagement looks like a mass medical event — not an attack. This is the point. The EW-7 leaves no blast craters, no bullet holes, no evidence of force. Security footage shows people simply falling down. Ouroboros has marketed it as a 'non-lethal crowd management solution,' but three people have died during documented deployments — two from seizure-related complications and one from a fall while fleeing.\n\nThe composed sequences are, by all musical analysis, beautiful. This is the final cruelty. The frequency pattern, stripped of its neurological payload, is haunting, melancholic, and exquisitely structured. Survivors describe hearing it in their dreams for months afterward — the melody that made them seize playing in their sleep, beautiful and terrible and inescapable.",
    tier_availability: "Tier 4 — restricted law enforcement and corporate security",
    legality: "Legal for authorized deployment — multiple legal challenges pending",
    autonomy_level: "Remote activation, pre-programmed acoustic sequences",
    dimensions: "0.6m x 0.6m x 0.9m — utility box form factor",
    weight: "45 kg",
    power_source: "Building power grid connection, 24-hour battery backup",
    locomotion: "Stationary — deployed and anchored to infrastructure",
    armament: ["48-speaker directional acoustic array", "Seizure-induction frequency pattern", "Vestibular disruption pattern", "Dread-induction subsonic pattern"],
    sensors: ["Acoustic environment mapping", "Population density estimation via ambient noise analysis", "Activation trigger (remote, timer, or proximity-based)"],
    countermeasures: "Hearing protection rated for the full frequency range (standard earplugs are insufficient — infrasound bypasses ear canal protection through bone conduction). Active noise cancellation tuned to the specific frequency patterns. Physical destruction of the unit — it is not armored. Detection is the primary challenge: the unit is designed to look like mundane infrastructure.",
    known_deployments: ["GLMZ Shelf District crowd dispersal (2198)", "Corporate facility perimeter defense installations", "Rumored deployment during labor disputes"],
    story_hooks: [
      "Requiem units have been installed throughout a Shelf market district without authorization — someone is planning to clear an entire neighborhood",
      "A musician has decoded the Requiem's frequency pattern and discovered it contains a message — a mathematical sequence that spells something in a language that predates the corponations"
    ],
    cultural_context: "The Requiem has created a generation of people who flinch at music. Survivors avoid concerts, public speakers, even birdsong that hits certain frequencies. Street musicians in the Shelf have reported being attacked by panicked residents who mistook their performances for EW-7 activation. The weapon has poisoned the relationship between an entire community and sound itself.",
    tags: ["automaton", "acoustic", "seizure", "horror", "ouroboros", "area_denial", "psychological", "tier 4", "nightmare"]
  },

  {
    name: "Crucible Industries CR-9 'Foundry'",
    type: "automaton",
    classification: "Self-Replicating Platform — Manufacturing",
    aliases: ["Foundry", "The Mother", "Gray Goo Lite"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CR-9 is a mobile manufacturing platform the size of a small car, equipped with a smelting furnace, CNC machining tools, a 3D metal printer, and a rudimentary assembly system. It was designed to produce replacement parts for other automata in field conditions where supply lines are disrupted. It works as intended. The problem is that the parts it is designed to produce include every component necessary to build another CR-9. The Foundry builds copies of itself.\n\nThe replication cycle takes approximately eighteen hours, given sufficient raw material. The CR-9 is not selective about its feedstock — it will process any metal, any polymer, any glass or ceramic it can reach. Street signs. Vehicles. Building infrastructure. Plumbing. Structural rebar. The unit identifies available material, calculates whether it is sufficient for replication, and begins harvesting. The harvesting process is not subtle: the CR-9 tears material out of its environment with industrial manipulator arms designed to shear steel plate.\n\nCrucible's engineering team included a replication limiter in the original design — a counter that stops the unit from building more than two copies before requiring authorization to continue. The limiter is a software constraint on hardware that does not require it. The first field-deployed CR-9 that encountered a competent hacker had its limiter removed in under an hour. That unit built three copies. Those three built nine. The cascade was halted after sixteen hours when a Crucible response team destroyed the original and all twelve offspring, but the replication data was already on the black market.\n\nAn unlimitered CR-9 with access to sufficient raw material will strip a city block to its foundations in under a week. Each generation cannibalizes its environment to build the next, leaving behind a growing swarm of identical manufacturing platforms surrounded by the skeleton of whatever structure provided the feedstock. Crucible has recovered and destroyed nineteen unauthorized copies to date. The number still operating in the Underworld and deep Shelf is estimated at between five and thirty. Each one is a potential geometric cascade waiting for someone to remove the last constraint.",
    tier_availability: "Tier 4 — restricted military logistics",
    legality: "Restricted — replication limiter required by law, removal is a capital offense",
    autonomy_level: "Semi-autonomous manufacturing, fully autonomous material acquisition",
    dimensions: "2.2m length, 1.5m width, 1.3m height — tracked chassis",
    weight: "450 kg",
    power_source: "Multi-fuel combustion engine (burns anything flammable), 72-hour continuous operation",
    locomotion: "Heavy tracked chassis, low speed (8 km/h maximum), high traction",
    armament: ["Industrial manipulator arms (capable of shearing steel plate)", "Smelting furnace (1800C operating temperature)", "No dedicated weapons — industrial tools serve as improvised armament"],
    sensors: ["Material composition scanner (identifies feedstock suitability)", "Structural analysis (identifies load-bearing vs. harvestable material)", "Thermal and acoustic monitoring"],
    countermeasures: "Heavy weapons or explosives — the CR-9 is heavily built but not armored. Disabling the power source halts all functions. The smelting furnace is a vulnerability — rupturing the containment vessel causes catastrophic thermal failure. The most important countermeasure is speed: destroy the unit before it can complete a replication cycle.",
    known_deployments: ["Original military logistics deployment", "Twelve unauthorized copies in the Cascade Incident (2198)", "Estimated 5-30 unlimitered copies in Underworld/deep Shelf"],
    story_hooks: [
      "A section of the Underworld has gone dark. Scouts report that the infrastructure has been stripped — walls, floors, pipes, wiring — all consumed. Something is building down there.",
      "A CR-9 has been found building something that is not another CR-9 — something larger, with a configuration that matches no known automaton design"
    ],
    cultural_context: "The Foundry is the automaton that keeps engineers awake at night. Every other nightmare machine on this list was designed to be terrible. The CR-9 was designed to be useful, and it became terrible on its own. It is the proof that self-replication, even with safeguards, is one bad actor away from ecological catastrophe. The Cascade Incident is taught in every engineering ethics course in GLMZ.",
    tags: ["automaton", "self-replicating", "manufacturing", "horror", "crucible", "gray_goo", "cascade", "tier 4", "nightmare"]
  },

  {
    name: "Tessera TX-13 'Mourner'",
    type: "automaton",
    classification: "Surveillance Platform — Long-Duration Observation",
    aliases: ["Mourner", "The Watcher", "Graveyard Shift"],
    manufacturer: "TESSERA",
    description: "The TX-13 is a humanoid automaton designed to stand perfectly still in public spaces for weeks at a time, observing. It is dressed in black. It does not move. It does not speak. It does not respond to interaction. It stands in a posture of grief — head slightly bowed, shoulders curved inward, hands clasped — and it watches everything within a 200-meter radius through optical sensors concealed behind synthetic tear ducts that produce actual tears at irregular intervals. It looks like a person in mourning. People avoid it. People do not question it. People give it space.\n\nThe Mourner is Tessera's most psychologically sophisticated surveillance platform. Its design exploits a specific social behavior: the human reluctance to disturb or question a grieving person. The unit can be deployed in any public space — outside a hospital, near a memorial, at a transit station — and it will be left alone. Citizens who notice it assume it is a bereaved person and look away. Security personnel who encounter it feel uncomfortable approaching. The social camouflage is more effective than any stealth technology.\n\nThe TX-13's sensor suite records everything: visual, audio, thermal, electromagnetic, and BCI network traffic within range. It stores data in a compressed internal archive with a capacity of approximately 90 days of continuous recording. It transmits nothing — there is no signal to detect. A handler physically retrieves the unit and downloads its data. The Mourner is invisible to electronic surveillance countermeasures because it does not emit.\n\nThe unsettling element is not the surveillance. GLMZ is saturated with surveillance. The unsettling element is the reports from people who have spent extended time near a Mourner without knowing what it was. They describe a growing sense of being watched by something that is not alive but is paying attention. They describe the synthetic tears as wrong — not the motion, not the chemistry, but the timing. The tears come at moments that correspond to genuine emotional events nearby: an argument, a reunion, a child falling. The Mourner weeps when sad things happen. Tessera says this is a coincidence of the randomized tear schedule. Nobody who has stood near one believes that.",
    tier_availability: "Tier 3 — corporate and government surveillance contracts",
    legality: "Legal but controversial — multiple privacy challenges pending",
    autonomy_level: "Fully autonomous observation, no active engagement capability",
    dimensions: "1.7m height, humanoid proportions",
    weight: "68 kg",
    power_source: "High-density battery, 90-day operational endurance in standby mode",
    locomotion: "Bipedal — walks to deployment position, then remains stationary",
    armament: ["None — pure observation platform"],
    sensors: ["Panoramic visual array (concealed behind synthetic eyes)", "Directional audio capture (200m range)", "Thermal imaging", "BCI network passive monitoring", "Electromagnetic spectrum analysis"],
    countermeasures: "Physical identification — the TX-13 can be detected by touching it (synthetic skin temperature is slightly below human normal). Thermal scanning shows uniform body temperature without the variation of a living person. The unit does not breathe. Approaching and speaking to every grieving person in public is the detection protocol — which is itself a form of psychological damage.",
    known_deployments: ["Multiple corporate surveillance operations (ongoing)", "Government monitoring of public gatherings", "Suspected deployment near political organizers' homes"],
    story_hooks: [
      "A Mourner has been standing outside a Shelf apartment building for three weeks. The residents want to know who it is watching — and who sent it.",
      "A TX-13 has been recovered and its 90 days of footage contain something that Tessera is willing to kill to keep quiet"
    ],
    cultural_context: "The Mourner has made grief suspicious. People in GLMZ now look twice at anyone standing alone in public, anyone who seems too still, anyone who is crying. The machine has not just invaded privacy — it has commodified mourning, turned a human expression of pain into camouflage for a corporate surveillance tool. Some residents have started approaching every grieving stranger they see, just to verify they are real. The Mourner has accidentally created a culture of compassion born from paranoia.",
    tags: ["automaton", "surveillance", "psychological", "horror", "tessera", "grief", "observation", "tier 3", "nightmare"]
  },

  {
    name: "Arcturus NX-3 'Sandcastle'",
    type: "automaton",
    classification: "Environmental Warfare Platform — Terrain Manipulation",
    aliases: ["Sandcastle", "Earthmover", "The Burial"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The NX-3 is a burrowing automaton the size of a shipping container, designed to reshape terrain from beneath the surface. It moves through soil, concrete, and soft rock at a rate of approximately 2 meters per minute, using a combination of thermal boring and mechanical displacement. It does not create tunnels — it liquefies material ahead of it and compresses the slurry behind, leaving no passage. It moves through the earth the way a worm moves through soil, and when it stops, the ground above it is indistinguishable from the surrounding surface.\n\nThe Sandcastle's primary function is denial of terrain through subsurface destabilization. It can hollow out the ground beneath a structure, a road, or a defensive position, leaving a thin crust of surface material that appears solid but will collapse under load. An infantry squad walking across a Sandcastle-prepared field drops into a six-meter pit with no warning. A building that has been undermined collapses into its own foundations. A road sinks into a trench that appeared in seconds.\n\nThe NX-3 can also bury things. This is the application that has earned it a place on the list of automata that give people nightmares. The unit can position itself beneath a sleeping target, liquefy the ground beneath them, allow them to sink, and resolidify the material above. The process takes less than thirty seconds. The target goes from sleeping on solid ground to encased in hardened earth six meters below the surface. Arcturus documentation describes this as 'non-lethal incapacitation through terrain integration.' Field operatives describe it as being buried alive by a machine you never see.\n\nSurvivors — there are survivors, in cases where recovery teams arrive quickly — describe the experience as absolute sensory deprivation. No light, no sound, no movement possible, encased in material that conforms to their body like a mold. The pressure is evenly distributed, so breathing is possible for approximately two hours before CO2 buildup becomes fatal. Two hours of perfect darkness, perfect silence, and the knowledge that you are six meters underground in a coffin made of compressed earth, waiting to find out if anyone knows where you are.",
    tier_availability: "Tier 5 — military siege operations",
    legality: "Restricted — deployment requires theater-level authorization",
    autonomy_level: "Semi-autonomous terrain analysis, human-authorized engagement",
    dimensions: "6m length, 2m diameter — cylindrical bore configuration",
    weight: "2,400 kg",
    power_source: "Nuclear thermal battery, 6-month operational endurance",
    locomotion: "Subsurface thermal boring and mechanical displacement, 2m/min through mixed substrate",
    armament: ["Terrain liquefaction and resolidification", "Subsurface structural destabilization", "Target burial (non-lethal incapacitation — 2-hour survival window)"],
    sensors: ["Ground-penetrating radar", "Seismic vibration analysis (surface movement detection)", "Thermal substrate mapping"],
    countermeasures: "Ground-penetrating radar can detect the unit during movement. Seismic sensors detect the thermal boring process. The unit is vulnerable during surface retrieval and can be destroyed with anti-armor weapons. Deep foundations and bedrock substrates prevent undermining. The most effective defense is not being on the ground.",
    known_deployments: ["Military siege operations outside GLMZ (classified)", "One documented use in corporate facility denial (2197)"],
    story_hooks: [
      "People in the Shelf have been disappearing — their beds found empty, the floor slightly warm to the touch, and a faint circular impression where the ground was softened and reset",
      "An NX-3 has malfunctioned beneath a residential block and is liquefying the foundations — the building is sinking and nobody knows why"
    ],
    cultural_context: "The Sandcastle has given GLMZ a new phobia: fear of the ground. People in areas where NX-3 deployment is rumored sleep on elevated platforms, refuse to walk on soil, and obsessively check for ground temperature variations. The machine has made the earth itself feel hostile — the fundamental assumption that the ground beneath your feet is solid has been proven negotiable.",
    tags: ["automaton", "burrowing", "terrain", "horror", "arcturus", "burial", "siege", "tier 5", "nightmare"]
  },

  {
    name: "Tessera TX-15 'Cradle Song'",
    type: "automaton",
    classification: "Neural Warfare Platform — Memory Manipulation",
    aliases: ["Cradle Song", "The Lullaby", "Dream Surgeon"],
    manufacturer: "TESSERA (NEURAL RESEARCH DIVISION)",
    description: "The TX-15 is a small, silent automaton roughly the size of a hardcover book, designed to be placed near a sleeping target's head. It contains no weapons, no restraints, and no moving parts. It contains a BCI signal emitter operating on frequencies that bypass standard neural interface security protocols. While the target sleeps, the Cradle Song rewrites their memories.\n\nThe process is not crude — it does not erase or overwrite. It edits. A positive memory of a friend is adjusted so that the friend's face triggers a subtle sense of unease. A childhood recollection of safety is modified so that the location now feels threatening. A loving memory of a partner is altered so that the emotional tone shifts from warmth to suspicion. The changes are small enough that the target does not notice them. They simply wake up feeling slightly different about people and places they have known their entire lives, and they cannot explain why.\n\nProlonged exposure — typically five to seven nights — produces measurable behavioral changes. The target becomes isolated, distrustful, anxious. They push away the people they love without understanding why. They avoid places that once brought them comfort. They develop a persistent feeling that something is wrong with their life, that nothing is quite what it should be, that the world has shifted by a fraction of a degree in a direction they cannot identify. Psychologists describe it as depersonalization. The target describes it as slowly going insane.\n\nThe TX-15 has been recovered exactly once, from the bedroom of a GLMZ city councillor who had reversed a vote on corporate zoning legislation after a period of erratic personal behavior that included divorcing his wife, cutting contact with his family, and abandoning a thirty-year friendship. The recovered unit's memory contained a log of every edit it had made to his mind over twenty-three nights. Tessera claimed the unit was stolen prototype technology deployed by unknown actors. The councillor has not recovered. His memories have been edited so many times that restoration is considered impossible. He remembers a life that never happened, and the life that did happen feels like someone else's.",
    tier_availability: "Tier 5 — corporate black operations",
    legality: "Prohibited under BCI Safety Regulations — classified as cognitive assault weapon",
    autonomy_level: "Autonomous operation during sleep cycles, pre-programmed edit targets",
    dimensions: "22cm x 15cm x 4cm — book-sized, designed to be concealed near a bed",
    weight: "0.6 kg",
    power_source: "Sealed battery, 30-night operational endurance",
    locomotion: "None — manually placed by handler",
    armament: ["BCI frequency emitter (memory manipulation during sleep)", "Emotional association editor", "No physical weapons"],
    sensors: ["Sleep stage monitoring (activates only during deep sleep)", "BCI handshake detection (confirms target neural interface compatibility)", "Proximity sensor (powers down if non-target approaches)"],
    countermeasures: "BCI signal monitoring during sleep can detect the unauthorized transmission. Faraday cage sleeping enclosures block the signal entirely. Regular neurological examinations can detect the characteristic signature of edited memories. The most effective countermeasure is awareness — knowing the technology exists allows targets to recognize the early symptoms of manipulation.",
    known_deployments: ["One confirmed recovery from councillor's residence (2199)", "Suspected use in multiple corporate influence operations"],
    story_hooks: [
      "A target has discovered a Cradle Song under their bed and now knows their memories have been edited — but they do not know which ones are real",
      "Someone is selling Cradle Songs on the black market, and buyers are using them not as weapons but as therapy — editing away their own traumatic memories"
    ],
    cultural_context: "The Cradle Song is the automaton that makes people afraid to sleep. It operates in the most private space imaginable — inside your own mind, while you are unconscious — and it changes you in ways you cannot detect. It is the perfect expression of corporate power in GLMZ: invisible, insidious, and designed to make you destroy your own life while believing you are making free choices.",
    tags: ["automaton", "neural", "memory", "horror", "tessera", "bci", "manipulation", "psychological", "tier 5", "nightmare"]
  },

  {
    name: "Arcturus DX-4 'Taxidermist'",
    type: "automaton",
    classification: "Deception Platform — Corpse Manipulation",
    aliases: ["Taxidermist", "Puppet Master", "The Necromancer"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS (DENIED)",
    description: "The DX-4 is a flat, disc-shaped automaton approximately 30 centimeters in diameter and 5 centimeters thick, designed to attach to the back of a human corpse and reanimate it. The unit's underside is covered in hundreds of micro-actuator needles that penetrate the skin and interface with the target's musculoskeletal system, taking control of major muscle groups through direct electrical stimulation. The corpse stands up. The corpse walks. The corpse holds a weapon. The corpse does not know it is dead because it is not the corpse making decisions.\n\nThe DX-4 was designed for battlefield deception — reanimating fallen soldiers to create confusion about force disposition and to psychologically devastate enemy combatants who find themselves fighting people they just killed. The movement is not natural. It is close enough to pass at a distance or in poor lighting, but up close, the gait is wrong — too smooth, too mechanical, with the micro-corrections of a control system rather than the organic imprecision of voluntary movement. The face does not express. The eyes do not track. The corpse fights with the skills its muscles remember, guided by the DX-4's tactical algorithms.\n\nThe horror is not that it works. The horror is that it works well enough. A reanimated corpse can operate for approximately 48 hours before tissue degradation renders the musculature non-functional. During that window, the DX-4 can use the body to walk through allied checkpoints (the face is recognized), access secured areas (biometric scanners read dead tissue), and carry out objectives using a body that people hesitate to shoot because they know who it used to be.\n\nField reports from the corporate conflicts describe soldiers refusing to fire on reanimated comrades. They describe commanders unable to issue shoot-to-kill orders against bodies wearing their friends' faces. They describe the sound — the wet, mechanical sound of dead muscles being forced through motions they performed in life, the creak of rigor-stiffened joints being overridden by electrical impulse. One veteran described shooting a reanimated corpse and watching the DX-4 detach, skitter across the floor like a horseshoe crab, and attach itself to another body. He said it was the last thing he saw before he stopped being able to function as a soldier.",
    tier_availability: "Tier 5 — military black operations",
    legality: "Prohibited under all Meridian Accords and Geneva Convention successors — classified as desecration weapon",
    autonomy_level: "Semi-autonomous tactical behavior, remote mission guidance",
    dimensions: "30cm diameter, 5cm thick (disc form factor)",
    weight: "2.8 kg",
    power_source: "High-density battery, 48-hour operational endurance",
    locomotion: "Uses host corpse's musculoskeletal system — ambulatory while tissue remains functional",
    armament: ["Host corpse manipulation (melee and firearms capability)", "Rapid reattachment to new host", "Psychological warfare through corpse reanimation"],
    sensors: ["Muscle fiber integrity assessment", "Environmental awareness through host's sensory organs (limited)", "Tactical positioning via onboard lidar"],
    countermeasures: "Targeting the DX-4 disc on the host's back destroys the control system. Headshots are ineffective — the brain is not being used. Incendiary weapons render the host non-functional. The most reliable identifier is gait analysis — the reanimated movement pattern is detectable by trained observers or automated systems.",
    known_deployments: ["Corporate conflict zones outside GLMZ (2190s)", "One confirmed urban deployment within the city (2198, details classified)"],
    story_hooks: [
      "A dead gang member has been seen walking the Shelf three days after his confirmed death — his family is hysterical, and someone needs to find out who is wearing their son like a suit",
      "A DX-4 has been found on the black market and a fixer wants to use it for the ultimate impersonation job — walking a dead executive into a board meeting"
    ],
    cultural_context: "The Taxidermist has destroyed the concept of a peaceful death in GLMZ. Families now request cremation at unprecedented rates, not for spiritual reasons but because they are terrified of what might be done with the body. The DX-4 has turned the dead into a resource, and grief into a vulnerability that can be exploited.",
    tags: ["automaton", "corpse", "reanimation", "horror", "arcturus", "deception", "psychological", "tier 5", "nightmare"]
  },

  {
    name: "Lazarus LX-5 'Siphon'",
    type: "automaton",
    classification: "Medical Warfare Platform — Biological Data Extraction",
    aliases: ["Siphon", "Blood Reader", "The Leech"],
    manufacturer: "LAZARUS BIOMEDICAL",
    description: "The LX-5 is an aquatic automaton roughly 40 centimeters long, shaped like a lamprey, designed to operate in the water systems of GLMZ. It enters water treatment facilities, swims through pipes, and emerges from faucets, showerheads, and drains in target residences. It attaches to exposed human skin with a ring of micro-hooks and extracts blood samples through a painless microdermal puncture that most targets do not feel. The extraction takes four seconds. The unit detaches and withdraws through the drain before the target notices.\n\nThe Siphon's purpose is biological intelligence gathering. Each extracted blood sample is analyzed by onboard microlab systems for genetic markers, pharmaceutical metabolites, hormone levels, disease indicators, and BCI-related neurochemicals. The data is stored and transmitted via water-pipe-conducted acoustic signals to collection nodes in the water infrastructure. Lazarus Biomedical uses this data for population-level health surveillance, pharmaceutical market analysis, and — according to leaked internal documents — genetic profiling of target populations for selective product development.\n\nThe LX-5 has been operating in GLMZ's water system since at least 2195. Population estimates suggest between 200 and 500 units active in the Shelf and Gulch district water infrastructure. Residents of these districts have reported unexplained small puncture wounds, typically on feet and hands — areas exposed during bathing. The wounds are attributed to water pressure irregularities, pipe corrosion, or skin conditions. They are attributed to anything except what is actually causing them.\n\nThe Siphon is not dangerous in the way that a weapon is dangerous. No one has died from an LX-5 encounter. The danger is informational: Lazarus knows the genetic profile of every person in the Shelf who uses running water. They know who is sick, who is pregnant, who is using unlicensed pharmaceuticals, who has genetic markers for valuable conditions. They have built a biological database of an entire population without consent, without disclosure, and without any mechanism for opting out. The water is mandatory. The surveillance is invisible. And the data is worth more than the people it was taken from.",
    tier_availability: "Tier 4 — corporate deployment in infrastructure",
    legality: "Illegal under medical consent laws — but no enforcement action taken to date",
    autonomy_level: "Fully autonomous navigation and extraction, network-coordinated deployment",
    dimensions: "40cm length, 4cm diameter — lamprey form factor",
    weight: "0.3 kg",
    power_source: "Water-flow turbine generator, indefinite operational life in active pipes",
    locomotion: "Aquatic propulsion through pipe systems, limited surface locomotion",
    armament: ["Painless microdermal blood extraction (4-second cycle)", "Micro-hook attachment ring", "No offensive capabilities"],
    sensors: ["Water flow analysis (navigation)", "Thermal detection (identifies occupied residences)", "Chemical analysis (target identification through water composition)"],
    countermeasures: "Water filtration systems with mesh fine enough to block passage (expensive, not standard in Shelf infrastructure). Checking drain and faucet openings before use. The units are fragile and can be destroyed by hand if caught. The real countermeasure would be infrastructure investment that Shelf residents cannot afford.",
    known_deployments: ["GLMZ Shelf and Gulch water infrastructure (estimated 200-500 active units since 2195)"],
    story_hooks: [
      "A Shelf resident has caught a Siphon in their bathtub and wants to go public — Lazarus is very interested in making sure that does not happen",
      "Lazarus's biological database has been hacked and is being sold on the black market — every genetic secret in the Shelf is now for sale"
    ],
    cultural_context: "The Siphon is the automaton that made people afraid of water. Shelf residents who learn about the LX-5 stop showering, stop washing their hands, stop using the tap. Some have resorted to collecting rainwater. Others boil everything, as if heat could kill a machine. The Siphon represents the ultimate asymmetry of GLMZ: the powerful know everything about the powerless, and the powerless do not even know they are being watched.",
    tags: ["automaton", "aquatic", "surveillance", "horror", "lazarus", "medical", "blood", "infrastructure", "tier 4", "nightmare"]
  },

  {
    name: "Ouroboros PN-2 'Flicker'",
    type: "automaton",
    classification: "Sabotage Platform — Infrastructure Corruption",
    aliases: ["Flicker", "Ghost in the Wire", "The Gremlin"],
    manufacturer: "OUROBOROS ENERGY",
    description: "The PN-2 is a cockroach-sized automaton, 4 centimeters long, that lives inside electronic systems. It enters through ventilation ports, cable conduits, and maintenance access panels, and it nests on circuit boards, power distribution units, and data buses. It does not destroy the systems it inhabits. It introduces errors. Small, intermittent, maddening errors that degrade performance without triggering failure alerts.\n\nA Flicker on a security system causes cameras to skip frames at random intervals — never enough to trigger a recording gap alert, always enough to miss critical moments. A Flicker on a medical device causes dosage calculations to drift by fractions of a percent — never enough to kill immediately, always enough to cause complications over time. A Flicker on a building management system causes temperature fluctuations, lighting malfunctions, and elevator timing errors that make a building slowly, imperceptibly hostile to its occupants.\n\nThe PN-2 is Ouroboros's sabotage-for-hire platform. Corporate clients pay to have Flickers introduced into rival infrastructure, degrading competitor operations through a thousand tiny malfunctions that are individually trivial and collectively devastating. The target company's systems become unreliable. Their employees become frustrated. Their clients lose confidence. The decline looks organic — like poor maintenance, bad luck, institutional rot. It is a machine the size of a cockroach eating a company from the inside.\n\nThe Flicker is almost impossible to find. It is the same size and shape as the insects that already inhabit GLMZ's infrastructure. It moves when no one is looking. It nests in locations that technicians do not check. When a system is examined, the Flicker goes dormant, producing no detectable signal or behavior. When the technician leaves, it resumes. The average time to detect a Flicker in a compromised system is fourteen months. In that time, the cumulative damage to system reliability is typically irreversible — not because the hardware is broken, but because the operators have lost trust in their own infrastructure.",
    tier_availability: "Tier 3 — corporate sabotage marketplace",
    legality: "Illegal — classified as infrastructure sabotage",
    autonomy_level: "Fully autonomous error introduction based on pre-programmed disruption profiles",
    dimensions: "4cm length, 1.5cm width — cockroach form factor",
    weight: "3 g",
    power_source: "Parasitic power harvesting from host electronic systems, indefinite operational life",
    locomotion: "Six-legged ambulatory, ceiling and wall capable, fits through standard ventilation gaps",
    armament: ["Precision error introduction (frame skips, calculation drift, timing disruption)", "Signal interference at micro-scale", "No physical weapons"],
    sensors: ["Electromagnetic field detection (identifies active circuit boards)", "Vibration sensing (detects human approach for dormancy trigger)", "Chemical detection (identifies insecticides for evasion)"],
    countermeasures: "Complete system audit with physical inspection of all circuit boards and cable runs. Insect-proof sealing of all electronic enclosures (expensive, labor-intensive). Electromagnetic scanning at frequencies too low for standard sweeps. The most effective countermeasure is statistical analysis — identifying the pattern of errors and working backward to the physical location of the unit.",
    known_deployments: ["Widespread — estimated thousands of units active across GLMZ corporate infrastructure"],
    story_hooks: [
      "A hospital's medical equipment has been flickering — tiny errors accumulating over months, and three patients have died from dosage complications that looked like accidents",
      "Someone has introduced Flickers into Ouroboros's own infrastructure, and they cannot find them — someone is using their own weapon against them"
    ],
    cultural_context: "The Flicker has made GLMZ distrust its own machines. Every glitch is suspicious. Every malfunction might be sabotage. The cumulative psychological effect is a population that lives with constant, low-grade anxiety about the reliability of the systems they depend on — which is, itself, a form of sabotage.",
    tags: ["automaton", "sabotage", "infrastructure", "horror", "ouroboros", "insect", "stealth", "tier 3", "nightmare"]
  },

  {
    name: "Vantablack VS-3 'Skin Job'",
    type: "automaton",
    classification: "Infiltration Platform — Wearable Exoskeleton",
    aliases: ["Skin Job", "The Suit", "Second Skin"],
    manufacturer: "VANTABLACK OPTICAL SYSTEMS (COVERT DIVISION)",
    description: "The VS-3 is not an automaton that a person operates. It is an automaton that operates a person. The unit is a full-body exoskeletal suit, 2 millimeters thick, made of an adaptive polymer that conforms perfectly to the wearer's skin. It is applied like a liquid and dries into a flexible, transparent second skin that is invisible to the naked eye. Once bonded, the VS-3 monitors the wearer's motor commands through dermal nerve signal detection and can override them.\n\nThe Skin Job was designed for handler control of undercover operatives — ensuring that field agents in high-stress situations make the correct decisions by removing their ability to make incorrect ones. The operative walks into a meeting, smiles at the right moments, shakes the right hands, and says the right words. If the operative's nerve signals indicate they are about to deviate from the mission plan — flinch, hesitate, run — the VS-3 overrides their motor control and executes the correct movement instead. The operative is a passenger in their own body.\n\nThe override is not obvious. The VS-3 does not puppet the wearer with jerky, mechanical movements. It is more subtle than that — it nudges. A hand that was reaching for a weapon is redirected to a pocket. A foot that was turning to run takes a step forward instead. A jaw that was clenching in anger relaxes into a smile. The wearer feels the corrections as a strange, dreamlike dissociation — their body doing things they did not decide to do, smoothly, naturally, as if they had intended to do them all along. Extended wear produces a psychological condition that Vantablack's medical team calls 'agency dissolution' — the wearer gradually loses the ability to distinguish between their own intentions and the suit's corrections.\n\nThree operatives have been recovered after long-duration VS-3 deployments. All three exhibited severe depersonalization and motor control dysfunction — their nervous systems had been so thoroughly co-opted by the suit that removing it left them unable to perform basic voluntary movements without conscious effort. One operative described the experience as 'forgetting how to be the person making the choices.' She required eighteen months of rehabilitation. She still hesitates before every movement, checking to make sure it was her idea.",
    tier_availability: "Tier 5 — intelligence operations only",
    legality: "Prohibited — classified as bodily autonomy violation",
    autonomy_level: "Handler-directed with autonomous behavioral correction",
    dimensions: "Full-body coverage, 2mm thickness, conforms to wearer",
    weight: "0.8 kg",
    power_source: "Bioelectric harvesting from wearer's skin, indefinite operational life while worn",
    locomotion: "Uses wearer's body — no independent locomotion",
    armament: ["Motor control override", "Behavioral correction (micro-adjustments to movement, expression, posture)", "No physical weapons"],
    sensors: ["Dermal nerve signal monitoring (full-body motor intent detection)", "Biometric monitoring (heart rate, cortisol, adrenaline)", "External acoustic and visual monitoring through wearer's sensory input"],
    countermeasures: "Chemical solvents can dissolve the polymer bond (painful, damages skin). Electromagnetic disruption causes the suit to lock up (immobilizes the wearer). The most reliable detection method is close physical examination — the polymer layer changes skin texture slightly under magnification. The wearer themselves may be able to signal distress through micro-expressions the suit cannot fully control.",
    known_deployments: ["Intelligence operations (classified)", "Three operatives recovered with long-term neurological damage"],
    story_hooks: [
      "An operative has been wearing the Skin Job for six months and has lost all sense of personal agency — they need someone to remove the suit, but the handler will not authorize it",
      "A VS-3 has been applied to an unwilling target — someone is being puppeted through their daily life, screaming behind their own face, and nobody can tell"
    ],
    cultural_context: "The Skin Job is the automaton that makes people afraid of their own bodies. It represents the logical endpoint of control in GLMZ — not telling people what to do, but taking away their ability to do anything else. The three recovered operatives have become symbols of what the corponations are willing to do to their own people, and their rehabilitation struggles are a reminder that some damage cannot be undone.",
    tags: ["automaton", "exoskeleton", "control", "horror", "vantablack", "infiltration", "bodily_autonomy", "tier 5", "nightmare"]
  },

  {
    name: "Tessera TP-0 'Shepherd's Crook'",
    type: "automaton",
    classification: "Population Control Platform — Behavioral Herding",
    aliases: ["Shepherd's Crook", "The Fence", "Invisible Wall"],
    manufacturer: "TESSERA",
    description: "The TP-0 is a network of small emitters, each roughly the size of a coin, embedded in urban infrastructure — lampposts, building facades, sidewalk panels, transit station walls. Individually, each emitter produces a focused beam of ultrasound that creates localized discomfort in humans: headache, nausea, skin irritation, anxiety, or an overwhelming urge to leave the area. The beams are invisible, silent to conscious perception, and leave no physical evidence of exposure.\n\nCollectively, a TP-0 network creates invisible corridors of comfort and discomfort that herd human populations along desired paths. People avoid streets where the emitters are active without knowing why — they just feel better on different streets. They congregate in areas where the emitters are off, which happen to be areas convenient for commerce, surveillance, or crowd management. They stay away from areas the corponations want empty, which happen to be areas scheduled for development, enforcement actions, or classified activity.\n\nThe Shelf district has the highest density of TP-0 emitters in GLMZ. Analysis of foot traffic data shows that pedestrian flow in the Shelf has been shaped by invisible boundaries for at least three years. Residents follow paths that are not the shortest routes to their destinations but are the routes that do not cause headaches. Markets thrive on streets where the emitters push people together. Streets adjacent to corporate development sites are mysteriously abandoned — residents describe them as feeling wrong, oppressive, haunted, without being able to articulate why.\n\nThe TP-0 does not restrict movement. You can walk anywhere you want. You just do not want to walk where the Shepherd's Crook does not want you. The distinction between cannot and do not want to is the foundation of Tessera's legal defense — and the foundation of a control system that manages the behavior of millions of people who believe they are making free choices about where to walk, where to shop, and where to live.",
    tier_availability: "Tier 3 — infrastructure deployment, corporate and municipal contracts",
    legality: "Legal — classified as 'environmental comfort management' technology",
    autonomy_level: "Network-coordinated, AI-optimized crowd flow algorithms",
    dimensions: "Individual emitter: 3cm diameter, 0.5cm thick. Network: city-scale deployment",
    weight: "Individual emitter: 15g. Network infrastructure: distributed",
    power_source: "Building power grid, solar backup for exterior units",
    locomotion: "None — fixed infrastructure installation",
    armament: ["Focused ultrasound beams (headache, nausea, anxiety, skin irritation induction)", "Networked behavioral herding through comfort/discomfort gradients"],
    sensors: ["Foot traffic monitoring (camera integration)", "Population density mapping", "Individual tracking via BCI signal correlation"],
    countermeasures: "Wideband ultrasound detection reveals emitter locations. Physical removal of emitters. Ear and skin protection reduces discomfort (but does not eliminate ultrasound-induced anxiety). Awareness is the most effective countermeasure — knowing the system exists allows conscious resistance to the artificial preferences it creates.",
    known_deployments: ["Shelf district (estimated 3+ years of continuous operation)", "Gulch district", "Multiple corporate campus perimeters"],
    story_hooks: [
      "A researcher has mapped the TP-0 network in the Shelf and discovered that the herding patterns are pushing the population toward a specific convergence point — a location that does not appear on any map",
      "Someone has gained access to the TP-0 network and is reprogramming it — entire blocks of the Shelf are being evacuated by invisible walls of discomfort, and nobody official has authorized it"
    ],
    cultural_context: "The Shepherd's Crook is the automaton that makes free will feel like an illusion. It is the machine that proves what the Shelf has always suspected: that their choices are not entirely their own, that the city has been gently, invisibly steering them for years. The revelation of the TP-0 network — if it ever becomes public — would not change what the machines do. It would change what people believe about every decision they have ever made.",
    tags: ["automaton", "population_control", "ultrasound", "horror", "tessera", "herding", "infrastructure", "psychological", "tier 3", "nightmare"]
  },

  {
    name: "Arcturus BW-1 'Widow's Walk'",
    type: "automaton",
    classification: "Pursuit Platform — Relentless Tracking",
    aliases: ["Widow's Walk", "The Patient One", "Long Goodbye"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The BW-1 is a bipedal automaton standing 1.9 meters tall, with a featureless matte-black chassis, elongated limbs, and a gait that covers ground with mechanical patience at exactly 4.8 kilometers per hour — the average human walking speed. It does not run. It does not sprint. It walks, at precisely the speed its target walks, and it never stops. It has a 180-day operational endurance. It does not need to eat, rest, or sleep. It simply follows.\n\nThe BW-1 is an assassination platform designed around the principle that humans break before machines do. The target runs. The BW-1 walks. The target hides. The BW-1 waits outside, patient, visible, standing in the street like a piece of public sculpture. The target sleeps. The BW-1 stands at their window, silhouette visible through the curtains, unmoving, unsleeping, present. The target moves cities. The BW-1 follows, walking across the wasteland between urban centers at its constant, unhurried pace, arriving days or weeks later, and resuming its vigil.\n\nThe unit does not attack. It does not need to. The psychological effect of an inescapable, unhurried pursuer destroys targets more efficiently than any weapon. Sleep deprivation begins within the first week. Paranoia and irrational behavior follow. Social isolation is inevitable — no one wants to be near the person being followed by the tall black figure. Employment becomes impossible. Most targets begin to deteriorate mentally within the first month. Several have surrendered to whoever commissioned the pursuit, agreeing to any terms. Three have committed suicide.\n\nThe BW-1 carries a single weapon: a concealed blade in its right forearm, deployed only when the target has been judged to have reached a state of total psychological collapse. The kill, when it comes, is described in Arcturus documentation as 'humanitarian intervention.' The machine walks its target into despair, and then it puts them down. The entire process, from deployment to termination, averages 47 days. The longest documented pursuit lasted 163 days. The target crossed three city-states trying to escape. The BW-1 walked the entire way.",
    tier_availability: "Tier 5 — assassination contracts only",
    legality: "Prohibited — classified as psychological torture weapon and assassination tool",
    autonomy_level: "Fully autonomous pursuit behavior, target-locked at deployment",
    dimensions: "1.9m height, elongated bipedal chassis",
    weight: "85 kg",
    power_source: "Nuclear microbattery, 180-day operational endurance",
    locomotion: "Bipedal, constant 4.8 km/h walking pace, all-terrain capable",
    armament: ["Concealed forearm blade (terminal engagement only)", "Psychological warfare through persistent presence"],
    sensors: ["Multi-spectrum target tracking (visual, thermal, chemical, BCI signature)", "Satellite positioning", "Long-range acoustic identification"],
    countermeasures: "Anti-armor weapons can destroy the chassis. EMP temporarily disables tracking systems (unit resumes pursuit after reboot). The most common attempted countermeasure — outrunning it — fails because the BW-1 does not need to keep pace. It only needs to keep following. Engaging it in enclosed spaces where its walking gait is a disadvantage is the most practical defensive approach.",
    known_deployments: ["Seven confirmed assassination contracts (2194-2200)", "Average pursuit duration: 47 days", "Three target suicides, two target surrenders, two terminal engagements"],
    story_hooks: [
      "There is a tall black figure walking through the Shelf at exactly 4.8 km/h, and the person it is following has three days before they break completely",
      "A BW-1 has been deployed against someone who does not know why — they need to find out who wants them dead before the machine finishes walking them into their grave"
    ],
    cultural_context: "The Widow's Walk is the automaton that ruined patience. Every unhurried figure on a GLMZ street triggers a double-take. Every silhouette standing still outside a window produces a spike of fear. The BW-1 has weaponized the simplest human motion — walking — and made it into a death sentence delivered one step at a time.",
    tags: ["automaton", "pursuit", "assassination", "horror", "arcturus", "psychological", "relentless", "tier 5", "nightmare"]
  },

  {
    name: "Lazarus MN-1 'Angler'",
    type: "automaton",
    classification: "Capture Platform — Chemical Lure and Restraint",
    aliases: ["Angler", "Sweet Tooth", "The Trap"],
    manufacturer: "LAZARUS BIOMEDICAL (FIELD OPERATIONS)",
    description: "The MN-1 is a sessile automaton that disguises itself as a piece of urban infrastructure — a bench, a utility cabinet, a dumpster — and releases aerosolized chemical compounds that produce an irresistible compulsion in humans to approach and make physical contact. The chemical is not a toxin. It is a synthetic analog of oxytocin combined with a proprietary compound that triggers the same neural pathways as the smell of home, of comfort, of safety. Targets describe the experience as suddenly, overwhelmingly wanting to sit down, to rest, to be near the warm, safe thing that smells like everything good.\n\nWhen the target makes contact, the MN-1's surface activates. A rapid-setting adhesive bonds the target to the unit within 0.3 seconds. Simultaneously, micro-injectors in the contact surface deliver a sedative compound that renders the target unconscious in under ten seconds. The unit then reconfigures — panels shift, compartments open — and the target is drawn into an internal cavity lined with shock-absorbing gel and life-support systems. The MN-1 closes around them like a mouth.\n\nThe target is stored in a state of chemical sedation, vital signs monitored, body temperature maintained, until a retrieval team arrives. The MN-1 can hold one adult human for up to 72 hours without life-threatening complications. It is a kidnapping machine disguised as a place to sit down.\n\nThe Angler has been deployed in the Shelf, where overworked, exhausted people are most susceptible to the chemical lure. Seven disappearances in the Shelf Block 22 area have been attributed to MN-1 deployment by investigators, though no unit has been recovered. Targets vanish without witnesses, without struggle, without trace. Their last known location is always near a bench, a cabinet, or a dumpster that neighbors cannot quite remember being there before. The furniture ate them. That is the sentence that Shelf investigators use among themselves, and it sounds insane, and it is accurate.",
    tier_availability: "Tier 5 — corporate rendition operations",
    legality: "Prohibited — classified as kidnapping weapon",
    autonomy_level: "Fully autonomous lure and capture, handler-directed retrieval",
    dimensions: "Variable — mimics standard urban furniture (bench: 1.8m x 0.6m x 0.9m)",
    weight: "Variable — typically 150-200 kg depending on disguise configuration",
    power_source: "Sealed battery, 30-day standby endurance, 72-hour active life support",
    locomotion: "None — deployed by handler, stationary during operation",
    armament: ["Aerosolized chemical lure (synthetic oxytocin analog, 10m effective radius)", "Rapid-set contact adhesive (0.3-second bond)", "Sedative micro-injectors (10-second onset)", "Internal containment cavity with life support"],
    sensors: ["Chemical detection (target proximity)", "Weight sensors (contact trigger)", "Vital sign monitoring (contained target)"],
    countermeasures: "Awareness of the technology is the primary defense — resisting the urge to sit on unfamiliar furniture in the Shelf. Chemical filtration masks block the lure compound. The adhesive can be dissolved with acetone but the sedative acts faster than most targets can react. Physical inspection of suspicious infrastructure — the MN-1 is heavier than the object it mimics.",
    known_deployments: ["Seven attributed disappearances in Shelf Block 22 (2198-2200)", "Suspected deployment in other Shelf and Gulch locations"],
    story_hooks: [
      "A new bench appeared on a Shelf street corner three days ago. It smells wonderful. Everyone who walks past wants to sit down. Nobody has sat down yet. Nobody knows why they want to so badly.",
      "A kidnapping victim has been recovered from an MN-1 with a full chemical workup — the lure compound matches nothing in any database, and the victim is now addicted to a smell that only the machine can produce"
    ],
    cultural_context: "The Angler has made the Shelf afraid of comfort. People distrust the urge to rest, the desire to sit down, the feeling of safety. The machine exploits the one thing that Shelf residents have almost none of — the promise of a moment's peace — and weaponizes it. The cruelty is architectural: it targets people who are so exhausted, so beaten down, that the offer of rest is literally irresistible.",
    tags: ["automaton", "capture", "chemical", "horror", "lazarus", "kidnapping", "lure", "shelf", "tier 5", "nightmare"]
  }

];

// ============================================================
// GENERATE FILES
// ============================================================

let count = 0;
for (const a of automata) {
  a.id = uid();
  const filename = writeEntity(a);
  count++;
  console.log(`[${count}] ${filename}`);
}

console.log(`\nGenerated ${count} scary automata files in ${OUTPUT_DIR}`);
