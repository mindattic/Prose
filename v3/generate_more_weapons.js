const fs = require('fs');
const path = require('path');

const outDir = path.join(__dirname, '..', 'engine_data', 'weaponry');
const existing = new Set(fs.readdirSync(outDir));

const manufacturers = [
  'ARCTURUS DEFENSE SOLUTIONS', 'VESPID DYNAMICS', 'CARRION DEFENSE WORKS',
  'TESSERA INDUSTRIES', 'STERLING-NAKAMURA', 'ZHENG-DAO HEAVY INDUSTRIES',
  'Street Custom', 'Unknown / Black Market', 'AXIOM SYSTEMS'
];

const weapons = [
  {
    name: "Vespid Dynamics Needlestorm NS-4 'Pincushion'",
    aliases: ["Pincushion", "Needle Gun", "NS-4", "Wasp Spit"],
    category: "pistol",
    manufacturer: "VESPID DYNAMICS",
    description: "A compact flechette pistol that fires salvos of micro-darts from a rotating drum magazine. Each dart carries a piezoelectric charge that discharges on penetration, causing involuntary muscle contraction around the wound channel. Vespid markets it as a 'compliance sidearm' but field reports document fatalities from cardiac disruption when dart clusters strike the thorax.",
    specifications: "caliber: 1.2mm tungsten-carbide flechette\nmagazine: 120-dart rotary drum\nrate of fire: 15-dart burst per trigger pull\neffective range: 8-30 meters\nweight: 0.9 kg loaded\npower source: Integrated piezo-charge, no external battery",
    tier_availability: "Tier 2+",
    legality: "Licensed — security contractors; Prohibited — civilian",
    street_price: "Φ2,400",
    base_technologies: ["Micro-flechette acceleration", "Piezoelectric discharge payloads", "Rotary drum miniaturization"],
    story_hooks: [
      "A batch of NS-4 darts has surfaced with a neurotoxin coating not listed in any Vespid catalog — someone is weaponizing the platform beyond spec.",
      "A clinic in the lower tiers is overwhelmed with patients showing identical flechette wound patterns — someone is using Pincushions for systematic intimidation."
    ]
  },
  {
    name: "Carrion Defense Works Marrow Drill MD-7 'Dentist'",
    aliases: ["Dentist", "The Drill", "MD-7", "Bone Biter"],
    category: "pistol",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A heavy-frame pistol firing gyroscopically stabilized micro-bore rounds that rotate at 14,000 RPM upon leaving the barrel. The rounds are designed to defeat rigid armor by drilling through rather than punching through, maintaining a tight wound channel that resists emergency sealing. Carrion's marketing materials describe it as 'persistent access' ammunition. Medical personnel call it something else entirely.",
    specifications: "caliber: 4.5mm gyrostabilized micro-bore\nmagazine: 8 rounds\nmuzzle velocity: 410 m/s with 14,000 RPM spin\neffective range: 5-20 meters\nweight: 1.4 kg\nbarrel life: approximately 200 rounds before rifling degradation",
    tier_availability: "Tier 3+",
    legality: "Restricted — military and licensed contractors only",
    street_price: "Φ5,800",
    base_technologies: ["Gyrostabilized projectile engineering", "Micro-bore drilling ballistics", "Anti-seal wound channel design"],
    story_hooks: [
      "Forensic teams have identified MD-7 wound channels in three separate unsolved killings across different tiers — the same weapon, same operator, different political targets.",
      "A Carrion engineer has gone missing after posting internal documents showing the MD-7 was originally designed for surgical applications before being reclassified as ordnance."
    ]
  },
  {
    name: "Street Custom 'Gutter Psalm' Reclaimed Pipe Shotgun",
    aliases: ["Gutter Psalm", "Pipe Banger", "Prayer Stick", "Sewer Special"],
    category: "shotgun",
    manufacturer: "Street Custom",
    description: "A single-shot break-action shotgun fabricated from reclaimed industrial pipe, a door hinge, and whatever firing mechanism the builder had on hand. The Gutter Psalm pattern has proliferated across Meridian 88's lower tiers through hand-copied fabrication sheets passed between squatter communities. Each one is unique but follows the same basic geometry — a length of Schedule 40 pipe, a crude breech block, and a striker fashioned from a nail or bolt. They are dangerously unreliable, occasionally lethal to the operator, and absolutely everywhere.",
    specifications: "gauge: variable, accepts most 12-gauge shells with shimming\ncapacity: 1 round\neffective range: 2-8 meters\nweight: 1.5-3 kg depending on pipe stock\nfailure rate: estimated 1 in 40 firings results in breech rupture",
    tier_availability: "Tier 1+",
    legality: "Prohibited — unregistered improvised firearm",
    street_price: "Φ30-80",
    base_technologies: ["Improvised breech-loading design", "Reclaimed materials fabrication"],
    story_hooks: [
      "A community organizer in Tier 1 has been distributing improved Gutter Psalm schematics with better breech designs — someone wants to know who is arming the lower tiers.",
      "A rash of breech rupture injuries at a free clinic all trace back to a single batch of defective pipe stock deliberately introduced into the supply chain."
    ]
  },
  {
    name: "Arcturus Defense Solutions Thermal Lance TL-9 'Sunstroke'",
    aliases: ["Sunstroke", "TL-9", "Heat Stick", "The Brander"],
    category: "energy",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A shoulder-mounted directed thermal weapon that projects a focused infrared beam capable of raising surface temperatures to 1,800°C within a two-second exposure window. The TL-9 was designed for breaching reinforced structures and disabling armored vehicles, but its effectiveness against personnel has made it a feared anti-infantry weapon in corporate conflict zones. The beam is invisible to the naked eye — targets only know they have been hit when their equipment begins to glow.",
    specifications: "beam wavelength: 1064nm focused infrared\npower output: 45kW sustained\neffective range: 15-120 meters\nbeam duration: 2-second pulse, 4-second recharge\nweight: 8.2 kg with power pack\npower source: Dedicated dorsal capacitor pack, 12 pulses per charge",
    tier_availability: "Tier 4+",
    legality: "Military restricted — active deployment authorization required",
    street_price: "Φ34,000",
    base_technologies: ["High-power infrared laser focusing", "Rapid capacitor discharge cycling", "Thermal bloom containment optics"],
    story_hooks: [
      "A stolen TL-9 has been used to cut through the vault wall of a Tier 3 credit union — the theft was surgical, the beam pattern suggests military training.",
      "Arcturus is recalling TL-9 units after discovering the capacitor pack can be overcharged to produce a single catastrophic pulse — essentially turning it into a one-shot building-killer."
    ]
  },
  {
    name: "Vespid Dynamics Swarm Canister SC-2 'Hornets'",
    aliases: ["Hornets", "Bug Bomb", "SC-2", "The Hive"],
    category: "drone-mounted",
    manufacturer: "VESPID DYNAMICS",
    description: "A launchable canister containing twelve autonomous micro-drones, each roughly the size of a human thumb, equipped with a shaped-charge warhead capable of defeating light armor. Once deployed, the swarm uses collective optical tracking to identify and converge on a designated target type — personnel, vehicles, or electronics. The drones communicate via ultrasonic chirps that give the swarm its characteristic insectoid sound. Individual drones are easily destroyed, but the swarm's distributed targeting means eliminating all twelve before detonation is statistically improbable.",
    specifications: "drone count: 12 per canister\nwarhead: 2g shaped copper charge per drone\nswarm radius: 40 meter engagement envelope\nflight time: 90 seconds per drone\ntarget acquisition: Collective optical + ultrasonic coordination\nweight: 1.8 kg per canister\nlaunch method: Pneumatic tube launcher or hand-thrown",
    tier_availability: "Tier 3+",
    legality: "Prohibited — autonomous lethal munition",
    street_price: "Φ12,000 per canister",
    base_technologies: ["Micro-drone autonomous swarming", "Shaped-charge miniaturization", "Collective optical target discrimination"],
    story_hooks: [
      "A swarm canister detonated in a crowded market — but the drones only targeted people with a specific model of cranial implant, suggesting the targeting firmware has been modified.",
      "Vespid's insurance division is quietly settling claims from a friendly-fire incident where an SC-2 swarm misidentified allied security forces as hostiles."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Resonance Hammer RH-3 'Earthquake'",
    aliases: ["Earthquake", "RH-3", "Ground Pounder", "Bass Drop"],
    category: "sonic",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A crew-served sonic weapon that generates a focused infrasonic pulse capable of inducing structural resonance in buildings, vehicles, and human bone. The RH-3 projects sound waves between 7-12 Hz at extreme amplitude through a parabolic emitter dish, creating a cone of effect where targets experience violent nausea, loss of equilibrium, and at close range, micro-fractures in the skeletal system. The weapon leaves no visible damage on structures but can fatigue load-bearing elements to the point of collapse over repeated application.",
    specifications: "frequency range: 7-12 Hz variable\namplitude: 185 dB at source\neffective range: 10-80 meters cone\npower source: Vehicle-mounted generator or heavy capacitor bank\nweight: 62 kg (emitter assembly)\ncrew: 2 operators minimum\nstructural fatigue threshold: 4-6 sustained pulses on standard ferrocrete",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    street_price: "Φ89,000",
    base_technologies: ["Focused infrasonic projection", "Structural resonance targeting", "Parabolic acoustic amplification"],
    story_hooks: [
      "Buildings in a Tier 2 neighborhood are spontaneously collapsing — structural engineers suspect infrasonic fatigue but no weapon deployments have been reported in the area.",
      "A black market dealer is offering a miniaturized version of the RH-3 emitter that fits in a backpack — if it works, it changes the calculus of urban warfare entirely."
    ]
  },
  {
    name: "Street Custom 'Judas Cradle' Electrified Knuckle Frame",
    aliases: ["Judas Cradle", "Sparknuckles", "Zap Hands", "Judas"],
    category: "melee",
    manufacturer: "Street Custom",
    description: "A set of articulated steel knuckle guards wired to a concealed capacitor pack worn at the wrist, delivering a high-voltage discharge on impact. The design originated in bare-knuckle fighting circuits in Tier 1 where augmented fighters needed an equalizer against opponents with reinforced skeletal systems. The discharge is enough to override pain suppression firmware in most commercial-grade cyberware, forcing augmented opponents to feel every hit. Build quality varies wildly — some are precision-machined, others are scrap metal and electrical tape.",
    specifications: "discharge voltage: 800-2,000V depending on build\ncapacitor recharge: 3-5 seconds between strikes\nweight: 0.3-0.6 kg per hand\npower source: Wrist-mounted lithium capacitor, 40-60 strikes per charge\nconstruction: Ranges from machined titanium to salvage steel",
    tier_availability: "Tier 1+",
    legality: "Prohibited — concealed electrified weapon",
    street_price: "Φ120-800",
    base_technologies: ["Capacitor discharge on impact", "Pain suppression firmware override"],
    story_hooks: [
      "A fighting circuit promoter has started requiring Judas Cradles for all competitors — the resulting injuries are drawing attention from people who profit from the fighters staying healthy.",
      "A new variant is circulating that delivers a data-injection payload along with the shock — it's not just hurting people, it's hacking their cyberware on contact."
    ]
  },
  {
    name: "Carrion Defense Works Miasma Projector MP-5 'Plague Bearer'",
    aliases: ["Plague Bearer", "MP-5", "Gas Gun", "Stink Cannon"],
    category: "chemical",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A pressurized chemical dispersal weapon that launches canisters of Carrion's proprietary incapacitant compound — a viscous aerosol that adheres to surfaces and continues to off-gas for up to six hours. The compound causes severe mucous membrane irritation, temporary blindness, and uncontrollable retching in unprotected targets. It is technically classified as non-lethal, though confined-space deployment has resulted in documented fatalities from asphyxiation. The persistent nature of the compound makes it particularly effective for area denial.",
    specifications: "canister range: 40-80 meters (launched)\narea of effect: 15 meter radius per canister\npersistence: 4-6 hours on surfaces\nmagazine: 6 canisters\nweight: 4.1 kg loaded\ncompound: Carrion CDW-Incap7 viscous aerosol\ncountermeasure: Filtered rebreather with ocular seal required",
    tier_availability: "Tier 2+",
    legality: "Licensed — riot control and area denial; Restricted — civilian",
    street_price: "Φ3,200 launcher, Φ180 per canister",
    base_technologies: ["Persistent chemical aerosol formulation", "Pressurized canister projection", "Surface-adhesion off-gassing compounds"],
    story_hooks: [
      "Someone deployed Miasma canisters in a sealed residential ventilation system in Tier 2 — forty people hospitalized, three dead, and the canisters had their serial numbers chemically erased.",
      "A Carrion chemist has developed a variant that selectively affects people based on their blood chemistry — it only incapacitates targets with specific pharmaceutical markers."
    ]
  },
  {
    name: "Tessera Industries Phantom Wire PW-1 'Tripwire'",
    aliases: ["Tripwire", "PW-1", "Ghost Line", "Phantom"],
    category: "exotic",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A spool-deployed monofilament wire system designed for perimeter defense and anti-personnel applications. The wire is a single-molecule-thick carbon filament that is effectively invisible to the naked eye and most optical sensors. Once deployed between anchor points, the wire will sever anything that contacts it with sufficient force — limbs, cables, vehicle tires, drone rotors. Tessera packages it with a compact spool housing and magnetic anchor stakes, marketing it as a 'passive security perimeter system' despite its obvious offensive applications.",
    specifications: "wire length: 200 meters per spool\nwire diameter: single-molecule carbon filament\ntensile strength: rated to 400 kg before deformation\ncutting threshold: 2 kg of lateral force\nanchor type: Magnetic stakes with 50 kg hold strength\nweight: 0.2 kg per spool assembly\nvisibility: Undetectable by standard optical systems",
    tier_availability: "Tier 3+",
    legality: "Prohibited — indiscriminate hazard weapon",
    street_price: "Φ6,500 per spool",
    base_technologies: ["Monofilament carbon synthesis", "Molecular-edge cutting geometry", "Passive perimeter deployment systems"],
    story_hooks: [
      "Monofilament lines have been found strung across a major transit corridor at neck height — someone is targeting a specific vehicle or person who uses that route.",
      "A Tessera facility breach resulted in the theft of 200 spools of PW-1 wire — enough to turn an entire district into a kill zone."
    ]
  },
  {
    name: "Arcturus Defense Solutions Pulse Carbine PC-6 'Heartbeat'",
    aliases: ["Heartbeat", "PC-6", "Pulse Gun", "Thumper"],
    category: "electromagnetic",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A compact electromagnetic pulse weapon that fires focused EMP bursts capable of disabling unshielded electronics within a targeted cone. The PC-6 generates a shaped electromagnetic wavefront through a series of rapidly discharged capacitor banks, creating a pulse that induces catastrophic current surges in electronic circuits. Against cyberware, the effect ranges from temporary shutdown to permanent damage depending on the target's shielding grade. Arcturus designed it for anti-drone and counter-electronics operations, but its effectiveness against augmented personnel has made it a weapon of terror in aug-heavy communities.",
    specifications: "pulse type: Shaped electromagnetic wavefront\neffective range: 5-25 meters cone\ncapacitor recharge: 6 seconds between pulses\npulses per charge: 8\nweight: 3.2 kg\nelectronics kill radius: Unshielded systems within 15m cone\ncyberware effect: Temporary shutdown (shielded) to permanent damage (unshielded)",
    tier_availability: "Tier 3+",
    legality: "Restricted — military and authorized security",
    street_price: "Φ18,000",
    base_technologies: ["Shaped EMP wavefront generation", "Rapid capacitor bank cycling", "Focused electromagnetic projection"],
    story_hooks: [
      "An underground movement is using stolen PC-6 units to disable surveillance networks in liberated neighborhoods — but the pulses are also frying residents' medical cyberware.",
      "Someone has modified a PC-6 to emit a specific frequency that only affects Axiom-manufactured neural interfaces — this is targeted corporate warfare."
    ]
  },
  {
    name: "Vespid Dynamics Toxin Dart Rifle TDR-3 'Asp'",
    aliases: ["Asp", "TDR-3", "Venom Rifle", "Quiet Death"],
    category: "rifle",
    manufacturer: "VESPID DYNAMICS",
    description: "A suppressed pneumatic rifle firing hollow-point darts loaded with selectable toxin payloads. The TDR-3 operates on compressed gas propulsion, producing no muzzle flash and minimal acoustic signature — the loudest component is the dart's impact. Vespid supplies a range of toxin capsules from incapacitant to lethal, color-coded and loaded into the dart's reservoir chamber before firing. The weapon has become the preferred tool of corporate assassination teams who need deniable, quiet elimination at medium range.",
    specifications: "propulsion: Compressed nitrogen, 180 m/s dart velocity\neffective range: 15-80 meters\nmagazine: 5-dart rotary chamber\nacoustic signature: 38 dB at 1 meter\ntoxin options: Incapacitant (blue), paralytic (yellow), lethal (red), tracer compound (green)\nweight: 2.8 kg with optic\noptics: Integrated 4x thermal with wind compensation",
    tier_availability: "Tier 4+",
    legality: "Prohibited — chemical weapon delivery system",
    street_price: "Φ22,000 rifle, Φ500-2,000 per dart depending on payload",
    base_technologies: ["Pneumatic suppressed propulsion", "Modular toxin payload delivery", "Thermal-integrated optics"],
    story_hooks: [
      "A series of apparently natural deaths among mid-tier corporate managers has been linked by a single commonality — trace compounds consistent with TDR-3 green tracer darts found in their bloodwork.",
      "A Vespid insider is selling a new toxin capsule on the black market that induces permanent memory loss of the preceding 72 hours — perfect for witnesses."
    ]
  },
  {
    name: "Street Custom 'Choir Boy' Ultrasonic Emitter",
    aliases: ["Choir Boy", "Screamer", "Ear Splitter", "The Singer"],
    category: "sonic",
    manufacturer: "Street Custom",
    description: "A handheld ultrasonic weapon cobbled from salvaged industrial cleaning equipment and a focusing horn fashioned from sheet metal. The Choir Boy emits a concentrated beam of ultrasonic energy at frequencies between 20-40 kHz, causing intense pain, disorientation, and at close range, tissue damage to soft organs. The name comes from the high-pitched whine the device produces at the edge of human hearing — a sound operators describe as angelic right before it becomes agonizing. Build quality varies from surprisingly sophisticated to genuinely dangerous to the user.",
    specifications: "frequency: 20-40 kHz variable\npower output: 140-160 dB depending on build\neffective range: 3-15 meters directional\npower source: Salvaged industrial battery packs\nweight: 1.2-2.5 kg\noperation time: 30-90 seconds continuous before overheating",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised weapon",
    street_price: "Φ200-600",
    base_technologies: ["Ultrasonic frequency focusing", "Improvised acoustic amplification"],
    story_hooks: [
      "A wave of Choir Boy attacks in Tier 1 has left dozens with permanent hearing damage — someone is distributing pre-built units for free along with target lists.",
      "A modified Choir Boy has been tuned to interfere with a specific frequency used by Diaspora neural interfaces, causing temporary disconnection — for some users, that disconnection is psychologically devastating."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Rail Pistol RP-2 'Verdict'",
    aliases: ["Verdict", "RP-2", "Rail Gun", "Judge"],
    category: "pistol",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A compact electromagnetic rail pistol that accelerates a 3mm tungsten slug to hypersonic velocity using a miniaturized two-stage rail system. The RP-2 represents Zheng-Dao's attempt to compress railgun technology into a sidearm format, and the compromises are significant — the weapon generates extreme heat per shot, has a four-round capacity, and the rail erosion means the barrel assembly must be replaced every 80 rounds. Despite these limitations, the sheer penetrating power of a hypersonic tungsten slug fired from a pistol-sized platform has made it sought after by operators who need to defeat heavy armor at close range.",
    specifications: "caliber: 3mm tungsten slug\nmuzzle velocity: 2,200 m/s\nmagazine: 4 rounds\nbarrel life: 80 rounds before rail replacement\nweight: 1.8 kg\nrecharge time: 3 seconds between shots\npower source: Integrated supercapacitor, 20 shots per charge\npenetration: Rated for Grade 4 composite armor at 10 meters",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    street_price: "Φ28,000",
    base_technologies: ["Miniaturized electromagnetic rail acceleration", "Hypersonic projectile stabilization", "Compact supercapacitor cycling"],
    story_hooks: [
      "A dead mercenary was found with an RP-2 that has been fired exactly 80 times — the barrel is spent but the weapon was never serviced, suggesting it was issued for a single extended operation.",
      "Zheng-Dao is offering a bounty for the return of prototype RP-3 units that were stolen during transit — the prototypes supposedly solve the barrel erosion problem."
    ]
  },
  {
    name: "Carrion Defense Works Entropic Shotgun ES-4 'Ragnarok'",
    aliases: ["Ragnarok", "ES-4", "Entropy Gun", "The End"],
    category: "shotgun",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A semi-automatic shotgun chambered for Carrion's proprietary entropic munitions — shells containing a reactive chemical compound that destabilizes molecular bonds in the target material upon impact. The effect is visually dramatic: struck surfaces appear to age centuries in seconds, metal corroding, polymer crumbling, biological tissue necrotizing in a rapidly expanding circle around the wound channel. The compound has a limited active window of approximately four seconds before it becomes inert, preventing uncontrolled propagation, but within that window the damage is catastrophic and irreversible.",
    specifications: "gauge: Proprietary Carrion 14-gauge entropic shell\nmagazine: 6 rounds semi-automatic\neffective range: 5-20 meters\ncompound active window: 4 seconds post-impact\nnecrotization radius: 8-12 cm from wound center\nweight: 4.6 kg loaded\nshelf life: Entropic shells degrade after 18 months",
    tier_availability: "Tier 4+",
    legality: "Prohibited — chemical munition",
    street_price: "Φ15,000 weapon, Φ800 per shell",
    base_technologies: ["Molecular bond destabilization compounds", "Time-limited reactive chemistry", "Proprietary shell casing containment"],
    story_hooks: [
      "Expired entropic shells have been turning up in street markets — the degraded compound is unpredictable, sometimes activating on contact with skin rather than requiring ballistic impact.",
      "A forensic chemist has reverse-engineered the entropic compound and published the formula on an open network — Carrion is scrambling to suppress it while simultaneously denying the weapon exists."
    ]
  },
  {
    name: "Sterling-Nakamura Aegis SMG AS-7 'Wasp Nest'",
    aliases: ["Wasp Nest", "AS-7", "Sterling Buzz", "The Nest"],
    category: "SMG",
    manufacturer: "STERLING-NAKAMURA",
    description: "A bullpup submachine gun designed for close-quarters corporate facility defense, firing caseless 4mm rounds from a helical magazine that holds 80 rounds. The AS-7 features an integrated friend-or-foe identification system linked to Sterling-Nakamura's employee biometric database — the weapon physically locks its trigger mechanism if pointed at a registered friendly. This IFF system has been both praised as a safety innovation and condemned as a loyalty enforcement tool, since it means the weapon literally cannot be turned against its issuing corporation.",
    specifications: "caliber: 4mm caseless\nmagazine: 80-round helical\nrate of fire: 900 RPM\neffective range: 5-40 meters\nweight: 2.4 kg loaded\nIFF system: Biometric trigger lock linked to Sterling-Nakamura employee database\nfire modes: Semi, burst, full auto",
    tier_availability: "Tier 3+",
    legality: "Licensed — Sterling-Nakamura security personnel only",
    street_price: "Φ7,500 (IFF bypass adds Φ3,000)",
    base_technologies: ["Biometric IFF trigger locking", "Caseless ammunition cycling", "Helical magazine feed systems"],
    story_hooks: [
      "An AS-7 with its IFF system intact was used to kill a Sterling-Nakamura executive — meaning either the system was defeated or the killer is a registered employee.",
      "A firmware update to the AS-7's IFF database has accidentally flagged hundreds of current employees as hostiles — security teams are finding their own weapons locked out mid-shift."
    ]
  },
  {
    name: "Axiom Systems Neural Disruptor ND-2 'Migraine'",
    aliases: ["Migraine", "ND-2", "Brain Zapper", "Thought Killer"],
    category: "electromagnetic",
    manufacturer: "AXIOM SYSTEMS",
    description: "A directed electromagnetic weapon specifically tuned to interfere with neural interface frequencies. The ND-2 projects a modulated electromagnetic field that induces cascading errors in neural implant firmware, causing effects ranging from sensory hallucination to complete motor shutdown depending on the target's implant architecture and shielding. Against unaugmented targets, the weapon has minimal effect — a mild headache at most. Against heavily augmented targets, it can be incapacitating or lethal if the neural interface manages autonomous functions like cardiac regulation.",
    specifications: "field type: Modulated neural-frequency electromagnetic pulse\neffective range: 5-15 meters directional\ntarget discrimination: Neural interface signatures only\npower source: Dedicated capacitor pack, 15 pulses per charge\nrecharge: 4 seconds between pulses\nweight: 2.1 kg\neffect duration: 10-60 seconds depending on target shielding",
    tier_availability: "Tier 4+",
    legality: "Prohibited — anti-augmentation weapon",
    street_price: "Φ35,000",
    base_technologies: ["Neural frequency electromagnetic modulation", "Interface firmware disruption targeting", "Augmentation-selective field projection"],
    story_hooks: [
      "Axiom officially denies the ND-2 exists, but units keep appearing in the hands of anti-augmentation extremist groups — someone inside Axiom is arming the movement.",
      "A player character's neural interface begins experiencing intermittent glitches consistent with low-level ND-2 exposure — someone nearby is carrying one, but who?"
    ]
  },
  {
    name: "Street Custom 'Confession' Modified Industrial Nail Gun",
    aliases: ["Confession", "Nail Driver", "Crucifier", "The Carpenter"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A construction-grade pneumatic nail gun modified for anti-personnel use by removing the contact safety, extending the barrel with a rifled sleeve, and upgrading the pneumatic charge to industrial compressor levels. The result fires hardened steel framing nails with enough velocity to penetrate light body armor at close range. The weapon is cheap, ammunition is available at any construction supply outlet, and the modifications can be performed with basic tools in under an hour. It has become the signature weapon of Tier 1 enforcement gangs who need deniable armament that can be sourced without touching the weapons black market.",
    specifications: "ammunition: 90mm hardened steel framing nails\nmagazine: 30-nail strip\neffective range: 3-12 meters\npenetration: Light body armor at 5 meters\nrate of fire: Semi-automatic, 2 nails per second\nweight: 2.8 kg\npower source: Modified pneumatic charge canister, 60 shots per canister",
    tier_availability: "Tier 1+",
    legality: "Prohibited — modified industrial tool as weapon",
    street_price: "Φ50-150 for modification, base tool Φ80",
    base_technologies: ["Pneumatic acceleration modification", "Contact safety bypass", "Improvised barrel rifling"],
    story_hooks: [
      "A series of crucifixion-style killings in Tier 1 — victims pinned to walls with framing nails — has the signature of a single modified nail gun based on the nail batch markings.",
      "A hardware chain is quietly tracking bulk nail purchases and selling the data to corporate security — mapping the supply chain of improvised weapons from the retail end."
    ]
  },
  {
    name: "Vespid Dynamics Corrosive Launcher CL-3 'Acid Reign'",
    aliases: ["Acid Reign", "CL-3", "Spit Launcher", "The Melter"],
    category: "chemical",
    manufacturer: "VESPID DYNAMICS",
    description: "A grenade-launcher-format weapon that fires encapsulated payloads of binary corrosive compound. Each round contains two separated chemical chambers that mix upon impact, creating a highly exothermic acid reaction that dissolves most metals, polymers, and organic materials within a 2-meter splash radius. The binary design means the individual components are relatively safe to handle and store — the danger only manifests on impact mixing. Vespid developed it for rapid structural demolition, but its anti-vehicle and anti-personnel applications have dominated field use.",
    specifications: "launcher type: Break-action single shot\nround diameter: 40mm encapsulated binary\nsplash radius: 2 meters\ncorrosion rate: 3mm structural steel per minute\neffective range: 30-100 meters\nweight: 3.5 kg\nneutralization time: Compound becomes inert after 8 minutes",
    tier_availability: "Tier 3+",
    legality: "Prohibited — chemical weapon",
    street_price: "Φ8,000 launcher, Φ600 per round",
    base_technologies: ["Binary corrosive compound engineering", "Impact-activated chemical mixing", "Encapsulated payload containment"],
    story_hooks: [
      "A building's structural supports were pre-weakened with CL-3 acid rounds days before a scheduled demolition — someone ensured the building would collapse ahead of schedule, with people still inside.",
      "A modified CL-3 round has been developed that produces a corrosive gas rather than liquid — turning it from a point weapon into an area denial tool."
    ]
  },
  {
    name: "Carrion Defense Works Ricochet Pistol RCH-1 'Billiard'",
    aliases: ["Billiard", "RCH-1", "Bounce Gun", "Corner Shot"],
    category: "pistol",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A smart-round pistol firing self-adjusting polymer-jacketed rounds that can be programmed to ricochet off up to three surfaces before reaching a target. The weapon's integrated LiDAR maps the immediate environment and calculates bounce trajectories in real-time, displaying them on the shooter's HUD as dotted lines. The rounds contain a micro-gyroscope that adjusts the jacket's surface hardness milliseconds before each impact, controlling the angle and energy of each bounce. Carrion marketed it for engaging targets behind cover, but its real cultural impact has been in assassination — killing someone around a corner, from another room, through a ventilation duct.",
    specifications: "caliber: 6mm polymer-jacketed smart round\nmagazine: 10 rounds\nmaximum ricochets: 3 per round\nenergy retention: approximately 60% per bounce\nLiDAR range: 30 meters environmental mapping\nweight: 1.1 kg\ntrajectory calculation: 0.3 seconds per bounce path\neffective kill range: 15 meters including bounces",
    tier_availability: "Tier 4+",
    legality: "Prohibited — smart munition",
    street_price: "Φ42,000 weapon, Φ200 per smart round",
    base_technologies: ["Real-time LiDAR trajectory mapping", "Self-adjusting polymer jacket hardness", "Micro-gyroscopic ricochet control"],
    story_hooks: [
      "A target was killed in a sealed room with no line of sight from any entry point — forensic trajectory analysis shows a triple-ricochet path through a mail slot.",
      "The RCH-1's LiDAR mapping data is stored locally and can reconstruct a detailed 3D map of every room the weapon has been fired in — stolen units are being used as covert surveying tools."
    ]
  },
  {
    name: "Street Custom 'Lazarus Wire' Garrote System",
    aliases: ["Lazarus Wire", "Choke Chain", "The Leash", "Wire Smile"],
    category: "melee",
    manufacturer: "Street Custom",
    description: "A retractable monofilament garrote concealed within a standard-looking wristwatch or bracelet. The wire extends from a spring-loaded spool hidden in the band, deploying approximately 60cm of cutting filament that can be used as a traditional garrote or whipped as a short-range slashing weapon. The filament is not true monofilament — it is a braided carbon-steel wire approximately 0.3mm in diameter — but it is thin enough to be invisible in low light and sharp enough to cut to bone with moderate pressure. Retraction is automatic when the deployment button is released, rewinding the bloody wire back into the spool.",
    specifications: "wire length: 60 cm deployed\nwire diameter: 0.3mm braided carbon-steel\ncutting depth: Through soft tissue to bone with 5 kg pressure\ndeployment: Spring-loaded spool, 0.4 second extension\nretraction: Automatic on release\nconcealment: Standard wristwatch or bracelet housing\nweight: 0.08 kg total assembly",
    tier_availability: "Tier 1+",
    legality: "Prohibited — concealed lethal weapon",
    street_price: "Φ900-2,500",
    base_technologies: ["Spring-loaded micro-spool engineering", "Carbon-steel braided filament", "Concealed deployment mechanisms"],
    story_hooks: [
      "A series of strangulation murders across multiple tiers share identical wound signatures — the wire diameter and cutting pattern match a single manufacturer's output.",
      "A fashion brand has unknowingly begun selling bracelets that contain Lazarus Wire mechanisms — the items were inserted into their supply chain by an unknown party."
    ]
  },
  {
    name: "Arcturus Defense Solutions Kinetic Denial System KDS-4 'Bouncer'",
    aliases: ["Bouncer", "KDS-4", "Push Field", "The Wall"],
    category: "heavy",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A vehicle-mounted kinetic repulsion system that generates a short-range concussive blast wave capable of hurling personnel and light vehicles away from the emission point. The KDS-4 uses a rapid-expansion gas generator coupled with a shaped acoustic reflector to create a directional wall of overpressure that travels outward at approximately 200 m/s. The blast is non-fragmentary but the kinetic energy transfer is sufficient to cause fatal blunt force trauma at close range and significant injury out to 15 meters. Arcturus markets it as a non-lethal crowd dispersal system, a classification that has been challenged in multiple jurisdictions.",
    specifications: "blast type: Directional concussive overpressure\neffective range: 5-30 meters cone\nlethal range: 0-8 meters\nrecharge: 12 seconds between blasts\nblasts per charge: 6\nweight: 180 kg (vehicle-mounted)\nmount: Standard vehicle hardpoint or static emplacement\noverpressure at 10m: 3.5 PSI",
    tier_availability: "Tier 4+",
    legality: "Licensed — riot control and vehicle defense",
    street_price: "Φ65,000",
    base_technologies: ["Rapid-expansion gas generation", "Shaped acoustic blast reflection", "Directional overpressure focusing"],
    story_hooks: [
      "A KDS-4 was deployed against a peaceful protest and classified as 'non-lethal force' despite eleven fatalities — the legal battle is becoming a flashpoint for anti-corporate sentiment.",
      "Someone has stolen a KDS-4 unit and mounted it in the back of a cargo van — mobile concussive blasts have been hitting corporate convoys."
    ]
  },
  {
    name: "Tessera Industries Phantom Blade PB-1 'Ghost Knife'",
    aliases: ["Ghost Knife", "PB-1", "Tessera Blade", "Null Edge"],
    category: "melee",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A combat knife with a blade forged from Tessera's proprietary metamaterial composite that absorbs electromagnetic radiation across the visible and near-infrared spectrum, rendering it functionally invisible to optical and thermal sensors. The blade appears as a void — a knife-shaped absence of light that the eye struggles to track. The metamaterial construction also gives the blade unusual physical properties: it is extremely hard but brittle, capable of penetrating body armor through sheer material hardness but prone to shattering if used to pry or lever. Each blade is individually manufactured and serial-coded, making them traceable — which has not stopped them from becoming prestige assassination tools.",
    specifications: "blade length: 18 cm\nblade material: Tessera EM-absorbing metamaterial composite\nhardness: Exceeds tungsten carbide\noptical signature: Absorbs 99.7% of visible and near-IR light\nthermal signature: Undetectable to standard thermal imaging\nweight: 0.15 kg\nfragility: Shatters under lateral stress exceeding 40 Nm",
    tier_availability: "Tier 4+",
    legality: "Prohibited — sensor-defeating weapon",
    street_price: "Φ50,000",
    base_technologies: ["Electromagnetic radiation absorbing metamaterials", "Ultra-hard brittle composite forging", "Optical stealth material science"],
    story_hooks: [
      "A Ghost Knife was recovered from a crime scene with its serial code intact — tracing the sale leads to a Tessera executive who reported it stolen six months ago, or claims to have.",
      "A forger is selling counterfeit Ghost Knives made from conventional metamaterial that degrades after weeks — buyers are finding their 'invisible' blades becoming visible at the worst possible moments."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Siege Mortar SM-8 'Tremor'",
    aliases: ["Tremor", "SM-8", "Mini Mortar", "The Shaker"],
    category: "explosive",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A man-portable smart mortar system that fires programmable munitions to a maximum range of 800 meters with GPS-guided precision. The SM-8 weighs 12 kg fully loaded and can be set up and fired by a single operator in under 30 seconds. Each round carries a modular warhead that can be configured before loading — high explosive, smoke, illumination, or Zheng-Dao's signature seismic charge that buries itself 2 meters into the ground before detonating, creating a localized earthquake effect that collapses underground structures and disrupts foundations.",
    specifications: "caliber: 60mm programmable munition\nmaximum range: 800 meters\nminimum range: 50 meters\nwarhead options: HE, smoke, illumination, seismic\nmagazine: 4 rounds in autoloader\nweight: 12 kg total system\nsetup time: 25 seconds to first round\nguidance: GPS with inertial backup",
    tier_availability: "Tier 5",
    legality: "Military restricted — active combat authorization only",
    street_price: "Φ120,000 system, Φ3,000-8,000 per round",
    base_technologies: ["Programmable modular warhead architecture", "GPS-guided mortar precision", "Seismic detonation charge engineering"],
    story_hooks: [
      "Seismic mortar rounds have been used to collapse tunnel networks under a disputed territory — but the tunnels contained a refugee community that nobody knew about.",
      "A single SM-8 system has gone missing from a Zheng-Dao armory and the serial numbers on mortar impacts across three incidents all match the same launcher."
    ]
  },
  {
    name: "Vespid Dynamics Paralytic Aerosol Grenade PAG-2 'Sleepy Time'",
    aliases: ["Sleepy Time", "PAG-2", "Nap Gas", "Dreamcatcher"],
    category: "chemical",
    manufacturer: "VESPID DYNAMICS",
    description: "A throwable aerosol grenade that disperses Vespid's proprietary fast-acting paralytic compound over a 10-meter radius. The compound targets voluntary motor function while leaving autonomic systems intact — targets remain conscious and breathing but cannot move, speak, or resist for approximately fifteen minutes. The effect onset is 3-5 seconds after inhalation, making the window for donning respiratory protection extremely narrow. Vespid markets it as a humane incapacitation tool, though the psychological effect of being fully conscious but completely paralyzed has been documented as severely traumatic.",
    specifications: "dispersal radius: 10 meters in still air\nonset time: 3-5 seconds after inhalation\neffect duration: 12-18 minutes\naerodynamic delay: 2 seconds after pin pull\nweight: 0.3 kg\ncompound persistence: 45 seconds airborne\ncountermeasure: Respiratory filtration prevents inhalation; no post-exposure antidote",
    tier_availability: "Tier 3+",
    legality: "Licensed — law enforcement and security",
    street_price: "Φ1,200 per grenade",
    base_technologies: ["Fast-acting paralytic compound synthesis", "Aerosol dispersal optimization", "Motor-selective neural targeting"],
    story_hooks: [
      "PAG-2 grenades have been deployed in a series of robberies targeting augmented individuals — victims are paralyzed while their cyberware is surgically removed.",
      "A contaminated batch of PAG-2 grenades has hit the market where the autonomic preservation fails in approximately 5% of exposures — meaning some victims stop breathing."
    ]
  },
  {
    name: "Street Custom 'Rat King' Cluster Munition Launcher",
    aliases: ["Rat King", "Cluster Banger", "Scatter Gun", "Party Favors"],
    category: "explosive",
    manufacturer: "Street Custom",
    description: "A improvised launcher cobbled from plumbing fixtures that fires a cluster of small explosive charges bound together with wire — typically six to ten individual charges that separate in flight and scatter across a 5-meter area upon a crude airburst fuse. Each sub-munition is roughly equivalent to a large firecracker in explosive yield, individually non-lethal but collectively capable of causing severe fragmentation injuries, starting fires, and creating enough chaos to cover a retreat or ambush. The Rat King pattern originated in Tier 1 gang warfare and has spread through fabrication networks.",
    specifications: "sub-munitions: 6-10 per cluster\nlaunch range: 20-60 meters\nscatter radius: 5 meters\nindividual charge yield: approximately 5g equivalent\nfragmentation: Wire binding and casing debris\nweight: 1.5-2.5 kg launcher, 0.8 kg per cluster\nreliability: Approximately 70% — duds and premature detonation are common",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised explosive device",
    street_price: "Φ80-200 per cluster",
    base_technologies: ["Improvised airburst fusing", "Crude cluster munition assembly", "Plumbing-fixture launcher fabrication"],
    story_hooks: [
      "Rat King clusters have started appearing with military-grade sub-munitions instead of improvised charges — someone is feeding real ordnance into the street weapon supply chain.",
      "A Tier 1 community has developed Rat King variants designed specifically to disable surveillance drones — they call it pest control."
    ]
  },
  {
    name: "Arcturus Defense Solutions Microwave Area Denial MAD-3 'Sunburn'",
    aliases: ["Sunburn", "MAD-3", "Microwave Gun", "The Cooker"],
    category: "energy",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A tripod-mounted directed microwave emitter designed for area denial and crowd control. The MAD-3 projects a focused beam of millimeter-wave radiation that penetrates clothing and heats the water molecules in the top 1/64th of an inch of skin, creating an intensely painful burning sensation without causing visible injury at standard power settings. At elevated power levels — accessible through a field override that Arcturus officially states does not exist — the beam can cause second-degree burns, ignite flammable materials, and cook electronics. The weapon's psychological effectiveness relies on the invisible, inescapable nature of the pain — targets cannot see what is hurting them and cannot shield themselves with conventional cover.",
    specifications: "beam type: 95 GHz millimeter wave\neffective range: 20-200 meters\nbeam width: 2 meter diameter at 100 meters\npower output: 100kW standard, field override to 250kW\nweight: 85 kg on tripod\npower source: Vehicle power feed or dedicated generator\ntime to pain threshold: 2 seconds at standard power",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and authorized security installations",
    street_price: "Φ95,000",
    base_technologies: ["Millimeter-wave directed energy", "Focused microwave beam steering", "Subcutaneous thermal induction"],
    story_hooks: [
      "A MAD-3 at elevated power was used to cook the electronics in an entire server farm through the building's walls — the data destruction was total and untraceable to any conventional weapon.",
      "Reports of invisible burning pain in a residential area have been dismissed as mass hysteria — but someone has set up a concealed MAD-3 and is slowly making an entire block uninhabitable."
    ]
  },
  {
    name: "Tessera Industries Data Spike DS-1 'Upload'",
    aliases: ["Upload", "DS-1", "Data Knife", "The Spike"],
    category: "cyber-integrated",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A wrist-mounted penetrating spike connected to the user's neural interface that physically punctures a target's cyberware housing and establishes a direct hardline connection to their implant network. The spike is a 6cm retractable carbon-ceramic needle containing a fiber-optic data conduit that can transmit attack payloads directly into a target's neural architecture, bypassing wireless security protocols entirely. The physical penetration is painful but rarely dangerous — the data payload is where the real damage occurs. Operators use it to inject malware, extract data, override motor functions, or in extreme cases, trigger a neural interface hard reset that can cause permanent cognitive damage.",
    specifications: "spike length: 6 cm retractable\nspike material: Carbon-ceramic with fiber-optic core\npenetration depth: Sufficient for standard cyberware housing\ndata transfer rate: 40 Gbps through fiber-optic conduit\ndeployment: Wrist-mounted spring mechanism, 0.1 second extension\nweight: 0.12 kg total assembly\ncompatibility: Requires user neural interface for payload delivery",
    tier_availability: "Tier 3+",
    legality: "Prohibited — cyberwarfare weapon",
    street_price: "Φ15,000 hardware, payloads sold separately",
    base_technologies: ["Hardline neural penetration", "Fiber-optic data injection", "Cyberware housing breach engineering"],
    story_hooks: [
      "A victim was found with a DS-1 puncture wound but no data payload was delivered — the spike was used purely as a physical weapon, suggesting the attacker wanted it to look like a cyber attack.",
      "A new payload circulating for DS-1 users doesn't attack the target — it copies their entire neural interface memory buffer, stealing the last 48 hours of sensory experience."
    ]
  },
  {
    name: "Street Custom 'Backfire' Weaponized Exhaust System",
    aliases: ["Backfire", "Flame Pipe", "Dragon Tail", "Hot Exit"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A vehicle-mounted improvised weapon created by modifying a motorcycle or small vehicle's exhaust system to inject raw fuel into the exhaust manifold on command, producing a jet of flame from the tailpipe that extends 3-5 meters behind the vehicle. The modification is simple — a secondary fuel injector spliced into the exhaust pipe with a dashboard trigger — and has become common enough in Tier 1 and 2 vehicle culture that mechanics advertise the conversion openly. It is primarily a pursuit deterrent, used to discourage tailgating vehicles and on-foot pursuers, but the flame output is sufficient to cause severe burns and ignite clothing or fuel leaks.",
    specifications: "flame range: 3-5 meters behind vehicle\nfuel consumption: 2 liters per 10-second burst\nignition: Spark plug in exhaust manifold\ninstallation time: 2-3 hours with basic tools\nweight: 3 kg additional hardware\nfuel source: Vehicle's primary fuel tank or auxiliary reservoir\nfire duration: Limited by fuel supply",
    tier_availability: "Tier 1+",
    legality: "Prohibited — vehicular weapon modification",
    street_price: "Φ200-500 for conversion",
    base_technologies: ["Exhaust fuel injection modification", "Manual ignition trigger systems"],
    story_hooks: [
      "A street racing fatality involved a Backfire system that was remotely triggered — someone hacked the driver's ignition circuit and cooked the rider behind them.",
      "A convoy of modified vehicles with Backfire systems has been running protection for smuggling routes — the wall of fire behind the convoy makes pursuit effectively impossible."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Graviton Lance GL-1 'Weight of the World'",
    aliases: ["Weight of the World", "GL-1", "Gravity Gun", "The Anchor"],
    category: "exotic",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "An experimental directed gravitational manipulation weapon that projects a localized gravity well at a targeted point within 30 meters. The gravity well increases local gravitational force by a factor of 8-12x for approximately 3 seconds, causing everything within a 2-meter radius to be crushed downward with immense force. Personnel caught in the field are driven to the ground with enough force to shatter bone; vehicles buckle; structures collapse. The technology is derived from Zheng-Dao's industrial gravity press systems and represents the first known weaponization of gravitational manipulation. Only three prototypes are confirmed to exist.",
    specifications: "gravity multiplier: 8-12x local gravity\neffect radius: 2 meters at target point\neffect duration: 3 seconds\neffective range: 5-30 meters\nrecharge: 45 seconds between activations\nweight: 22 kg with power unit\npower source: Dedicated micro-fusion cell, 8 activations per cell\nprototypes confirmed: 3",
    tier_availability: "Tier 5",
    legality: "Experimental — not classified, no legal framework exists",
    street_price: "Not commercially available — estimated black market Φ2,000,000+",
    base_technologies: ["Localized gravitational field manipulation", "Directed gravity well projection", "Micro-fusion power cell technology"],
    story_hooks: [
      "A building collapse in Tier 3 shows structural damage patterns consistent with extreme localized gravitational stress — one of the three GL-1 prototypes may be in the field.",
      "Zheng-Dao is offering an extraordinary bounty for information on the whereabouts of GL-1 prototype unit 2, which disappeared during a facility transfer — the bounty amount suggests they are terrified of what it could do in the wrong hands."
    ]
  },
  {
    name: "Vespid Dynamics Neural Hornet NH-1 'Brainbug'",
    aliases: ["Brainbug", "NH-1", "Neural Wasp", "The Crawler"],
    category: "drone-mounted",
    manufacturer: "VESPID DYNAMICS",
    description: "A micro-drone the size of a large insect that autonomously navigates to a target's head and delivers a neural disruption payload through a penetrating proboscis. The NH-1 uses thermal and pheromone tracking to identify and approach a specific individual, then lands on exposed skin — typically the neck or behind the ear — and inserts a 2mm probe that delivers a concentrated electromagnetic pulse directly to the neural interface hardware. The target experiences immediate sensory overload, disorientation, and temporary loss of motor control. The drone then self-destructs, leaving minimal forensic evidence. Vespid does not acknowledge the NH-1's existence in any product catalog.",
    specifications: "drone size: 3 cm wingspan\nflight time: 8 minutes\ntracking: Thermal signature + pheromone profile matching\nproboscis penetration: 2mm subcutaneous\npayload: Focused micro-EMP to neural interface\nself-destruct: Thermite micro-charge, 2 seconds post-delivery\nweight: 4 grams\noperating range: 200 meters from launch point",
    tier_availability: "Tier 5",
    legality: "Does not officially exist — no legal classification",
    street_price: "Φ75,000 per unit (if you can find a seller)",
    base_technologies: ["Insect-scale autonomous drone engineering", "Pheromone-guided target acquisition", "Micro-EMP neural disruption payload"],
    story_hooks: [
      "A corporate executive collapsed during a public speech with symptoms consistent with neural interface overload — a dead insect-sized drone was found crushed on their collar.",
      "Someone has obtained NH-1 pheromone tracking data and is selling target-locked drones to anyone with the money — assassination as a subscription service."
    ]
  },
  {
    name: "Carrion Defense Works Bone Saw BS-3 'Butcher'",
    aliases: ["Butcher", "BS-3", "The Saw", "Carrion Cutter"],
    category: "melee",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A forearm-mounted reciprocating blade designed for breaching body armor and cyberware housings in close-quarters combat. The BS-3 extends a 20cm carbide-toothed blade from a housing strapped to the user's forearm, oscillating at 4,000 strokes per minute with enough force to cut through Grade 3 composite armor in under two seconds. Carrion designed it as a field tool for emergency cyberware removal and armor breach, but its adoption by close-combat specialists has given it a reputation as the most visceral weapon in the corporate arsenal. The sound it makes cutting through metal is reportedly indistinguishable from the sound it makes cutting through bone.",
    specifications: "blade length: 20 cm reciprocating carbide\noscillation rate: 4,000 strokes per minute\narmor breach time: Grade 3 composite in 1.8 seconds\npower source: Forearm-mounted lithium cell, 15 minutes continuous\nweight: 1.1 kg total assembly\nmount: Forearm strap with wrist trigger\nblade replacement: Tool-free swap, Φ80 per blade",
    tier_availability: "Tier 3+",
    legality: "Licensed — field engineering and emergency services",
    street_price: "Φ4,500",
    base_technologies: ["High-speed reciprocating carbide cutting", "Forearm-mounted deployment systems", "Armor-grade material breach engineering"],
    story_hooks: [
      "A rash of cyberware thefts in the mid-tiers involves victims whose implant housings were cut open with surgical precision using BS-3 blade patterns — the thieves are harvesting augmentations from living people.",
      "Carrion has released an updated blade material that cuts through their competitors' armor significantly faster than their own — raising questions about whether the blade was designed as a product or a strategic weapon against rival equipment."
    ]
  },
  {
    name: "Sterling-Nakamura Suppression Net Launcher SNL-2 'Spider'",
    aliases: ["Spider", "SNL-2", "Net Gun", "The Web"],
    category: "heavy",
    manufacturer: "STERLING-NAKAMURA",
    description: "A shoulder-fired launcher that deploys a weighted conductive net designed to entangle and incapacitate targets. The net spans 4 meters when deployed and carries a conductive mesh that can deliver a sustained taser-grade electrical discharge for up to 30 seconds, keeping the entangled target immobilized. The net's edge weights are designed to wrap around limbs and torso, and the conductive fibers tighten under tension — struggling makes the entanglement worse and increases the contact area for electrical discharge. Sterling-Nakamura markets it as their premier non-lethal capture system.",
    specifications: "net span: 4 meters deployed\nelectrical discharge: 50,000V taser-grade, 30-second sustained\neffective range: 5-20 meters\nlauncher weight: 4.2 kg\nnet weight: 1.5 kg per cartridge\ncartridges: Single-shot, reload in 8 seconds\nconductive fiber: Copper-core polymer weave\ntensile strength: Rated to 200 kg pull force",
    tier_availability: "Tier 2+",
    legality: "Licensed — law enforcement and security",
    street_price: "Φ6,000 launcher, Φ400 per net cartridge",
    base_technologies: ["Conductive entanglement net engineering", "Tension-reactive tightening geometry", "Sustained electrical discharge delivery"],
    story_hooks: [
      "Modified Spider nets have appeared that replace the electrical discharge with a contact sedative coating — targets are wrapped and unconscious within seconds, ideal for abduction.",
      "A Spider net was used to capture a heavily augmented fugitive, but the electrical discharge interacted with their military-grade cyberware and caused a catastrophic power surge that killed three bystanders."
    ]
  },
  {
    name: "Axiom Systems Cognitive Grenade CG-1 'Brainstorm'",
    aliases: ["Brainstorm", "CG-1", "Think Bomb", "Mind Wipe"],
    category: "electromagnetic",
    manufacturer: "AXIOM SYSTEMS",
    description: "A throwable EMP grenade specifically calibrated to disrupt neural interface frequencies within a 10-meter radius. Unlike conventional EMP grenades that affect all electronics indiscriminately, the CG-1 uses a tuned resonance frequency that primarily affects neural implant architectures, causing a cascade of sensory hallucinations, memory buffer corruption, and temporary loss of executive function in augmented targets. Unaugmented individuals experience only mild disorientation. Axiom developed it for counter-intrusion scenarios where augmented attackers need to be neutralized without damaging facility electronics.",
    specifications: "effect radius: 10 meters\nfrequency: Tuned neural interface resonance\neffect on augmented targets: Hallucination, memory corruption, executive dysfunction for 30-90 seconds\neffect on unaugmented targets: Mild disorientation for 5-10 seconds\nweight: 0.25 kg\nfuse: 2-second delay after pin pull\npower source: Single-use capacitor discharge",
    tier_availability: "Tier 4+",
    legality: "Restricted — authorized facility defense only",
    street_price: "Φ8,500 per grenade",
    base_technologies: ["Tuned neural resonance EMP", "Selective electronic disruption calibration", "Neural interface cascade induction"],
    story_hooks: [
      "CG-1 grenades are being used in muggings targeting augmented individuals — victims wake up with corrupted memories and missing cyberware, unable to identify their attackers.",
      "A modified CG-1 has been developed that permanently corrupts a specific memory address in Axiom neural interfaces — it erases the target's ability to recognize a particular face."
    ]
  },
  {
    name: "Street Custom 'Prophet' Laser Pointer Weapon",
    aliases: ["Prophet", "Pointer Gun", "Pen Laser", "God's Finger"],
    category: "energy",
    manufacturer: "Street Custom",
    description: "A high-powered laser weapon disguised as — or built from — industrial laser cutting modules stacked in series within a tube roughly the size of a flashlight. The Prophet concentrates coherent light into a beam capable of causing instant retinal destruction at distance and thermal burns on skin with sustained exposure. The build uses commercially available laser diodes intended for industrial cutting applications, wired in parallel to achieve dangerous power levels. The weapon is favored by street-level operators for its concealability, silence, and the terrifying precision of a weapon that can blind someone from across a room before they know they are being targeted.",
    specifications: "beam type: Visible coherent light, 450nm wavelength\npower output: 5-20W depending on diode count\neffective range: Line of sight, lethal to eyes at 500+ meters\nthermal effect: Second-degree burns at 5 meters with 3-second exposure\nweight: 0.3-0.8 kg\npower source: Lithium cell pack, 2-5 minutes continuous\nconcealability: Flashlight or pen-sized form factor",
    tier_availability: "Tier 1+",
    legality: "Prohibited — directed energy weapon",
    street_price: "Φ100-400",
    base_technologies: ["Industrial laser diode stacking", "Coherent light focusing optics", "Concealed energy weapon fabrication"],
    story_hooks: [
      "A sniper-style blinding campaign has left fourteen people permanently blind in a Tier 2 commercial district — the attacks are precise, targeted, and the weapon leaves no ballistic evidence.",
      "A batch of Prophet builds is circulating that use an unstable diode configuration prone to thermal runaway — the weapons are blinding their users as often as their targets."
    ]
  },
  {
    name: "Carrion Defense Works Hemorrhage Rifle HR-6 'Bloodletter'",
    aliases: ["Bloodletter", "HR-6", "Bleed Gun", "The Leech"],
    category: "rifle",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A precision rifle firing rounds containing a micro-encapsulated anticoagulant payload that releases into the wound channel upon penetration. The compound — Carrion's proprietary HemoDisrupt formula — prevents blood clotting at the wound site and in the surrounding vasculature for approximately 45 minutes, turning what would be a survivable gunshot wound into a potentially fatal hemorrhagic event. The round itself is a standard 7.62mm profile that performs normally in flight, with the anticoagulant capsule contained in a hollow cavity behind the penetrator tip. Carrion describes it as 'extended incapacitation ammunition' in internal documents.",
    specifications: "caliber: 7.62mm with anticoagulant payload\nmagazine: 10 rounds\neffective range: 100-600 meters\nanticoagulant duration: 45 minutes at wound site\npayload: 0.3ml HemoDisrupt micro-encapsulated\nweight: 4.2 kg with optic\noptics: Variable 3-12x with environmental compensation\nbarrel: Free-floating match grade, 550mm",
    tier_availability: "Tier 4+",
    legality: "Prohibited — chemical warfare ammunition",
    street_price: "Φ19,000 rifle, Φ150 per round",
    base_technologies: ["Micro-encapsulated chemical payload rounds", "Anticoagulant compound engineering", "Precision ballistic delivery systems"],
    story_hooks: [
      "A seemingly minor shooting wound killed a Tier 3 businessperson eighteen minutes after the injury — toxicology reveals HemoDisrupt in the wound channel, turning a misdemeanor assault into a chemical weapons case.",
      "Carrion's HemoDisrupt formula has been reverse-engineered and someone is loading it into standard ammunition — any gun could now fire hemorrhage rounds."
    ]
  },
  {
    name: "Vespid Dynamics Shrike Autonomous Turret SAT-2",
    aliases: ["Shrike", "SAT-2", "Auto-Gun", "The Perch"],
    category: "drone-mounted",
    manufacturer: "VESPID DYNAMICS",
    description: "A compact autonomous turret platform weighing 8 kg that can be deployed on any stable surface and left to provide automated fire support. The SAT-2 uses a combination of thermal, optical, and acoustic sensors to detect and engage targets within its programmed rules of engagement. It fires 5.56mm caseless ammunition from a 200-round internal hopper at a rate sufficient for suppressive or precision fire. The turret's AI can distinguish between armed and unarmed personnel, moving vehicles and pedestrians, and can be programmed to hold fire on targets matching specific biometric profiles. Vespid markets it as a 'force multiplier for undermanned positions.'",
    specifications: "caliber: 5.56mm caseless\nhopper capacity: 200 rounds\nrate of fire: 300 RPM suppressive, semi-auto precision\nsensor suite: Thermal + optical + acoustic triangulation\nengagement range: 10-150 meters\nweight: 8 kg\nsetup time: 15 seconds\nbattery life: 72 hours standby, 6 hours active scanning\ntraverse: 270 degrees horizontal, 45 degrees vertical",
    tier_availability: "Tier 3+",
    legality: "Licensed — military and authorized perimeter defense",
    street_price: "Φ28,000",
    base_technologies: ["Autonomous target discrimination AI", "Multi-sensor fusion targeting", "Compact caseless ammunition feeding"],
    story_hooks: [
      "A SAT-2 deployed in a Tier 2 commercial district opened fire on unarmed civilians — the target discrimination AI was updated with a firmware patch from an unknown source 12 hours prior.",
      "Someone has been deploying SAT-2 turrets in the undercity with their IFF systems set to engage anyone with corporate security biometric signatures — the turrets are protecting territory that nobody has claimed."
    ]
  },
  {
    name: "Street Custom 'Tooth Fairy' Pneumatic Bolt Pistol",
    aliases: ["Tooth Fairy", "Bolt Gun", "Stunner", "The Dentist's Friend"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A modified veterinary captive bolt pistol redesigned for use against humans. The original tool — used for livestock stunning — has been adapted with an extended barrel sleeve, a reinforced bolt, and a higher-pressure pneumatic cartridge that drives the bolt with enough force to penetrate the human skull at contact range. It is a point-blank assassination weapon, silent except for the pneumatic discharge, and leaves a wound that can be mistaken for a blunt force injury by inexperienced medical examiners. The weapon's association with slaughterhouse equipment gives it a particular psychological weight that its users exploit deliberately.",
    specifications: "bolt penetration: 7 cm hardened steel bolt\noperating range: Contact only\npower source: Single-use pneumatic cartridge\nweight: 0.9 kg\nreload: 4 seconds per cartridge swap\nacoustic signature: 55 dB — comparable to a heavy book dropping\nconcealability: Fits inside a jacket pocket",
    tier_availability: "Tier 1+",
    legality: "Prohibited — modified lethal device",
    street_price: "Φ60-200",
    base_technologies: ["Pneumatic captive bolt modification", "Contact-range penetration engineering"],
    story_hooks: [
      "A string of apparent blunt-force homicides in Tier 1 are revealed to be bolt pistol killings when a forensic specialist notices the consistent wound depth and diameter.",
      "A slaughterhouse worker has gone missing along with two dozen pneumatic cartridges and the facility's entire stock of bolt pistol housings — someone is manufacturing Tooth Fairies at scale."
    ]
  },
  {
    name: "Arcturus Defense Solutions Cryogenic Projector CP-4 'Frostbite'",
    aliases: ["Frostbite", "CP-4", "Freeze Gun", "Ice Maker"],
    category: "energy",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A directed cryogenic weapon that projects a stream of supercooled liquid nitrogen at -196°C through a pressurized nozzle, flash-freezing surfaces and causing rapid thermal shock to materials and tissue. The CP-4 carries a pressurized reservoir of liquid nitrogen sufficient for 30 seconds of continuous projection at a range of 8 meters. Frozen materials become extremely brittle — armor plates shatter under subsequent impact, mechanical joints seize, and biological tissue undergoes immediate cryonecrosis. Arcturus developed it for industrial applications involving controlled material embrittlement, but its weapon applications were recognized and exploited almost immediately.",
    specifications: "coolant: Liquid nitrogen at -196°C\nprojection range: 3-8 meters\nreservoir capacity: 30 seconds continuous spray\nfreezing effect: Surface embrittlement in 2 seconds, deep tissue cryonecrosis in 5 seconds\nweight: 6.5 kg loaded\nrefill: Standard liquid nitrogen, available industrially\nnozzle type: Adjustable cone/stream\noperating pressure: 15 bar",
    tier_availability: "Tier 2+",
    legality: "Licensed — industrial; Restricted — tactical deployment",
    street_price: "Φ5,500",
    base_technologies: ["Pressurized cryogenic projection", "Directed thermal shock engineering", "Controlled material embrittlement"],
    story_hooks: [
      "A vault door was flash-frozen and shattered during a heist — the cryogenic projector was found abandoned at the scene, but the liquid nitrogen supply was traced to an internal source within the facility.",
      "Someone is freezing the locks on residential doors in Tier 2 and shattering them to gain entry — the burglaries are efficient and precise, suggesting the operator has industrial cryogenics training."
    ]
  },
  {
    name: "Tessera Industries Blackout Carbine BC-4 'Lights Out'",
    aliases: ["Lights Out", "BC-4", "Dark Gun", "Blackout"],
    category: "rifle",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A compact carbine that fires rounds containing Tessera's proprietary PhotoNull compound — a substance that absorbs and suppresses photonic activity within a 3-meter radius of the impact point for approximately 60 seconds. Within the affected zone, all light is absorbed: lamps go dark, screens black out, laser sights disappear, and human vision becomes useless. The compound works by releasing a dense aerosol of photon-absorbing nanoparticles that remain suspended in air, creating a sphere of absolute darkness. Thermal imaging is also degraded within the zone as the nanoparticles absorb infrared radiation. The weapon was designed for tactical entry operations where controlling the visual environment is paramount.",
    specifications: "caliber: 12mm PhotoNull compound round\nmagazine: 8 rounds\neffective range: 20-80 meters\ndarkness radius: 3 meters from impact\ndarkness duration: 55-65 seconds\nnanoparticle suspension: Aerosol, degrades naturally\nthermal attenuation: approximately 70% within zone\nweight: 3.1 kg\nfire modes: Semi-automatic",
    tier_availability: "Tier 4+",
    legality: "Restricted — tactical operations authorization",
    street_price: "Φ25,000 weapon, Φ800 per round",
    base_technologies: ["Photon-absorbing nanoparticle synthesis", "Aerosol suspension delivery", "Controlled photonic suppression zones"],
    story_hooks: [
      "An entire city block experienced 60 seconds of total darkness during which four people were killed — someone used multiple Blackout rounds to cover a coordinated assassination.",
      "Tessera's PhotoNull compound has been loaded into building ventilation systems, creating zones of permanent darkness in occupied structures — someone is weaponizing architecture."
    ]
  },
  {
    name: "Street Custom 'Angry Mary' Aerosol Flamethrower",
    aliases: ["Angry Mary", "Spray and Pray", "Can Burner", "Hobo Torch"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "The simplest possible flamethrower: a can of commercial aerosol product — hairspray, lubricant, insecticide — duct-taped to a piezoelectric igniter salvaged from a kitchen lighter. The flame produced is a 1-2 meter cone of burning aerosol propellant that lasts until the can is empty, typically 15-30 seconds of continuous fire. The Angry Mary is not sophisticated, not reliable, and not safe for the operator, but it costs almost nothing, requires no skill to build, and will absolutely set things on fire. It is the weapon of absolute desperation and has been in continuous use in Meridian 88's lowest tiers for as long as anyone can remember.",
    specifications: "flame range: 1-2 meters\nfuel: Commercial aerosol can\nburn duration: 15-30 seconds per can\nignition: Piezoelectric lighter element\nconstruction time: Under 1 minute\nweight: 0.3-0.5 kg\noperator risk: High — can rupture, blowback, and burn injuries common\ncost: Φ5-15 total",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised incendiary",
    street_price: "Φ5-15",
    base_technologies: ["None — basic combustion principles"],
    story_hooks: [
      "A coordinated arson attack using hundreds of Angry Mary devices hit a corporate facility in Tier 2 — the sheer number suggests organized distribution of a weapon anyone can build.",
      "A survival instructor in Tier 1 has been teaching children to build Angry Marys as self-defense tools — the ethics are horrifying but the threat is real and the children are surviving."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Tectonic Charge TC-5 'Fault Line'",
    aliases: ["Fault Line", "TC-5", "Ground Breaker", "Quake Charge"],
    category: "explosive",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A specialized demolition charge designed to be buried or anchored to structural foundations, detonating with a shaped downward blast that fractures bedrock and disrupts structural footing. The TC-5 uses Zheng-Dao's resonant detonation technology — the charge fires in a precisely timed sequence of micro-detonations that amplify through the ground substrate like an artificial earthquake, causing foundation failure across a much larger area than the charge's raw explosive yield would suggest. Originally developed for mining and geological survey, the TC-5 has become the preferred tool for infrastructure sabotage — a single charge can collapse a building by undermining its foundations without any visible surface damage.",
    specifications: "explosive yield: Equivalent to 5 kg TNT, amplified by resonant detonation\neffect radius: 20-meter foundation disruption zone\nplacement: Buried 1-2 meters or anchored to foundation\ndetonation sequence: 12-stage resonant micro-detonation over 0.8 seconds\ntimer options: 30 seconds to 72 hours, or remote trigger\nweight: 3.2 kg per charge\nform factor: Flat disc, 25 cm diameter",
    tier_availability: "Tier 4+",
    legality: "Licensed — demolition and mining; Prohibited — all other use",
    street_price: "Φ18,000 per charge",
    base_technologies: ["Resonant sequential micro-detonation", "Substrate-amplified blast propagation", "Foundation disruption engineering"],
    story_hooks: [
      "Three buildings have collapsed in a straight line across Tier 2 over three weeks — geological analysis shows resonant detonation signatures beneath each, suggesting a deliberate campaign of infrastructure destruction.",
      "A TC-5 charge has been found planted beneath a major transit hub but not detonated — the timer was set but the trigger signal never came, suggesting the planter was interrupted or the charge is a warning."
    ]
  },
  {
    name: "Vespid Dynamics Spore Cloud Disperser SCD-1 'Bloom'",
    aliases: ["Bloom", "SCD-1", "Spore Gun", "Mushroom Cloud"],
    category: "chemical",
    manufacturer: "VESPID DYNAMICS",
    description: "A backpack-mounted dispersal system that releases clouds of engineered fungal spores designed to colonize and degrade specific material types. Different spore payloads target different substrates — one variant consumes synthetic polymers, another attacks ferrous metals, a third breaks down carbon-fiber composites. The spores germinate on contact with their target material and begin enzymatic degradation within hours, visibly consuming equipment, armor, vehicles, and infrastructure over a period of 2-5 days. Vespid developed the system for environmental remediation of industrial waste sites, but its application as a slow-acting area denial and equipment destruction weapon has not escaped military planners.",
    specifications: "dispersal method: Backpack-mounted pressurized aerosol\ncloud radius: 15-meter deployment zone per burst\nspore germination: 2-6 hours on target substrate\nfull material degradation: 2-5 days depending on substrate\npayload types: Polymer-consuming, ferrous metal-consuming, carbon composite-consuming\nreservoir: 10 deployment bursts per tank\nweight: 8 kg loaded\ncountermeasure: UV sterilization within 4 hours of exposure halts colonization",
    tier_availability: "Tier 4+",
    legality: "Licensed — environmental remediation; Prohibited — all other use",
    street_price: "Φ30,000 system, Φ2,000 per spore payload",
    base_technologies: ["Engineered substrate-selective fungal biology", "Controlled spore dispersal systems", "Enzymatic material degradation targeting"],
    story_hooks: [
      "A Tier 3 armory's entire weapons stockpile was rendered useless overnight — polymer-consuming spores were introduced through the ventilation system, eating every synthetic component in the building.",
      "Spore payloads designed to consume human-compatible biomedical polymers have been detected — if deployed, they would attack cybernetic implant housings inside living people."
    ]
  },
  {
    name: "Sterling-Nakamura Executive Defense Pistol EDP-1 'Handshake'",
    aliases: ["Handshake", "EDP-1", "Boardroom Gun", "Sterling Compact"],
    category: "pistol",
    manufacturer: "STERLING-NAKAMURA",
    description: "An ultra-compact ceramic pistol designed to pass through standard security scanning systems undetected. The EDP-1 is constructed entirely from non-metallic materials — ceramic barrel, polymer frame, carbon-fiber internals — and fires caseless ceramic-tipped rounds that are similarly invisible to metal detectors. The weapon holds 4 rounds, has an effective range of 10 meters, and is designed to be concealed within a briefcase, jacket lining, or even a prosthetic limb compartment. Sterling-Nakamura issues it to senior executives as a last-resort personal defense weapon, though its obvious utility as an assassination tool in secured environments has made it the most sought-after concealable weapon in Meridian 88.",
    specifications: "caliber: 5mm caseless ceramic-tipped\nmagazine: 4 rounds\neffective range: 3-10 meters\nconstruction: 100% non-metallic — ceramic, polymer, carbon fiber\ndetectability: Passes standard metal detection and basic material scanning\nweight: 0.18 kg\nform factor: Credit card width, 12 cm length\nbarrel life: 20 rounds before ceramic degradation",
    tier_availability: "Tier 5",
    legality: "Issued — Sterling-Nakamura executive staff only",
    street_price: "Φ120,000 (extremely rare on black market)",
    base_technologies: ["Non-metallic weapons fabrication", "Ceramic barrel engineering", "Scan-defeating material composition"],
    story_hooks: [
      "A diplomat was shot dead in a Tier 4 secured conference room that had passed full security screening — the only weapon that could have entered is an EDP-1, narrowing the suspect pool to people with Sterling-Nakamura executive access.",
      "A 3D printing schematic for an EDP-1 replica has been uploaded to the open net — the replicas are fragile and inaccurate, but they pass the same security scans."
    ]
  },
  {
    name: "Street Custom 'Scorpion Tail' Chain Whip",
    aliases: ["Scorpion Tail", "Chain Whip", "Rattler", "The Stinger"],
    category: "melee",
    manufacturer: "Street Custom",
    description: "A weapon constructed from salvaged motorcycle chain weighted at the tip with a sharpened steel slug, often augmented with welded barbs or razor wire wrapping. The Scorpion Tail has a 1.5-meter reach and can be swung with enough force to crack bone through light armor, while the weighted tip and barbs tear flesh on contact. It folds compact for concealment along a belt or inside a coat sleeve and deploys instantly with a flick of the wrist. The weapon has deep roots in Tier 1 street culture where it serves as both a personal defense weapon and a status symbol — the condition, decoration, and customization of a fighter's chain tells their history.",
    specifications: "chain length: 1.5 meters\ntip weight: 200-400g sharpened steel slug\nbarb configuration: Variable — razor wire, welded spikes, or hooked links\nfolded length: 20 cm coiled\nweight: 0.8-1.5 kg depending on chain gauge\nmaterial: Salvaged motorcycle or industrial chain",
    tier_availability: "Tier 1+",
    legality: "Prohibited — concealed bladed weapon",
    street_price: "Φ40-200",
    base_technologies: ["Weighted chain dynamics", "Improvised barbed melee construction"],
    story_hooks: [
      "A fighting tournament in Tier 1 exclusively uses Scorpion Tails and has become a major gambling draw — the prize pool has attracted corporate attention and the fighters are being scouted as assets.",
      "A specific chain whip style with distinctive link markings has appeared across three unrelated crime scenes — the markings indicate a single forge source that someone wants found."
    ]
  },
  {
    name: "Arcturus Defense Solutions Graviton Shield Projector GSP-1 'Aegis'",
    aliases: ["Aegis", "GSP-1", "Gravity Shield", "The Dome"],
    category: "heavy",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "An experimental defensive system that projects a localized gravity distortion field in a 3-meter hemisphere around the emitter, deflecting incoming projectiles by altering their trajectory through gravitational lensing. The GSP-1 does not stop bullets — it bends them, causing rounds to curve away from the protected zone with increasing deflection based on proximity to the emitter. The field is effective against ballistic projectiles and slow-moving ordnance but has minimal effect on directed energy weapons, which travel too fast for gravitational deflection at this scale. The system consumes enormous power and can only maintain the field for 12 seconds before requiring a 90-second recharge.",
    specifications: "field radius: 3-meter hemisphere\ndeflection effect: Ballistic trajectory deviation up to 30 degrees\nfield duration: 12 seconds maximum\nrecharge time: 90 seconds\npower source: Dedicated fusion micro-cell\nweight: 35 kg emitter with power unit\neffectiveness: High against ballistics, minimal against directed energy\nprototypes deployed: Estimated 8 units in field testing",
    tier_availability: "Tier 5",
    legality: "Experimental — military testing authorization only",
    street_price: "Not commercially available — estimated Φ1,500,000+",
    base_technologies: ["Gravitational lensing field projection", "Ballistic trajectory distortion", "Micro-fusion sustained field generation"],
    story_hooks: [
      "A corporate VIP survived an assassination attempt when every round fired at their vehicle curved away — footage suggests graviton shielding, but no GSP-1 was visible in the wreckage.",
      "An Arcturus field test of the GSP-1 went wrong when the gravity distortion field collapsed asymmetrically, launching deflected rounds into a nearby civilian area."
    ]
  },
  {
    name: "Tessera Industries Memory Rounds MR-2 'Recall'",
    aliases: ["Recall", "MR-2", "Smart Bullets", "Thinking Rounds"],
    category: "exotic",
    manufacturer: "TESSERA INDUSTRIES",
    description: "Self-guided ammunition compatible with standard 9mm pistol platforms that uses embedded micro-processing and control surfaces to adjust trajectory in flight. Each round contains a thumbnail-sized guidance package with a micro-camera, processing chip, and four piezoelectric fin actuators that can steer the round toward a designated target up to 15 degrees off the original firing axis. The rounds are 'trained' by designating a target through a linked optic — the round's guidance package memorizes the target's visual signature and homes on it after firing. The name comes from the round's ability to 'remember' its target. Accuracy degrades rapidly beyond 30 meters as the control surfaces lack the authority to compensate for ballistic drop at range.",
    specifications: "caliber: 9mm compatible with standard platforms\nguidance: Embedded micro-camera + piezoelectric fin steering\ncourse correction: Up to 15 degrees off-axis\neffective guided range: 5-30 meters\ntarget designation: Visual signature lock via linked optic\nweight per round: 12g (2g heavier than standard 9mm)\nprocessing: Onboard micro-chip, single-use\nfin actuators: 4 piezoelectric surfaces",
    tier_availability: "Tier 4+",
    legality: "Prohibited — guided munition",
    street_price: "Φ500 per round",
    base_technologies: ["Micro-guided projectile engineering", "In-flight trajectory correction", "Visual signature target locking"],
    story_hooks: [
      "A target was killed by a 9mm round that forensic analysis shows changed direction mid-flight — the entry angle is physically impossible from any firing position in the room without guidance.",
      "A shipment of Memory Rounds has been intercepted with their guidance packages pre-locked to specific individuals — someone manufactured assassination rounds targeted at a specific hit list."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Containment Foam Launcher CFL-2 'Amber'",
    aliases: ["Amber", "CFL-2", "Foam Gun", "The Encaser"],
    category: "heavy",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A pressurized launcher that deploys a binary chemical foam which expands on contact with air and hardens within 8 seconds into a rigid, heat-resistant polymer shell. A single burst can encase a human-sized target in a cocoon of hardened foam strong enough to resist small arms fire and capable of immobilizing even augmented individuals. The foam is porous enough to allow breathing through small air channels that form during expansion, though the experience of being rapidly encased in hardening chemical foam is described by test subjects as one of the most claustrophobic experiences imaginable. Zheng-Dao markets it as a non-lethal capture system, and it has become standard equipment for corporate extraction teams.",
    specifications: "foam expansion: 40x volume in 3 seconds\nhardening time: 8 seconds to full rigidity\ncompressive strength: 15 MPa when hardened\nburst coverage: Sufficient to encase 1 human-sized target\ntank capacity: 6 bursts per pressurized tank\neffective range: 3-12 meters\nweight: 9 kg loaded\ndissolution: Proprietary solvent required, no natural degradation for 72+ hours",
    tier_availability: "Tier 3+",
    legality: "Licensed — law enforcement and corporate security",
    street_price: "Φ11,000 launcher, Φ600 per foam charge",
    base_technologies: ["Rapid-expansion binary polymer chemistry", "Controlled hardening foam formulation", "Pressurized binary dispersal systems"],
    story_hooks: [
      "A target was found dead inside hardened Amber foam — the air channels failed to form properly in a defective batch, and the victim suffocated inside a rigid shell.",
      "Someone has been encasing surveillance cameras across Tier 3 in containment foam, blinding entire security networks one node at a time — the foam is too hard to remove without damaging the equipment."
    ]
  },
  {
    name: "Street Custom 'Dead Man's Hand' Cyber-Integrated Holdout",
    aliases: ["Dead Man's Hand", "Palm Gun", "Handshake Special", "Last Word"],
    category: "cyber-integrated",
    manufacturer: "Street Custom",
    description: "A single-shot firearm integrated into a prosthetic hand or cybernetic forearm, typically chambered for a heavy-caliber round and fired by a neural trigger linked to the user's interface. The barrel is concealed within the forearm or palm cavity, and the round exits through a disguised port in the palm or between the knuckles. Dead Man's Hand builds are the ultimate concealed weapon — they pass physical pat-downs because they are part of the user's body, and they pass weapons scans because the components are distributed throughout the prosthetic's existing structure. The trade-off is single-shot capacity and the risk of catastrophic damage to the user's own cyberlimb on firing.",
    specifications: "caliber: Variable — typically .45 ACP or 12-gauge slug equivalent\ncapacity: 1 round\nfiring mechanism: Neural interface trigger\nconcealment: Integrated into prosthetic hand/forearm structure\nbarrel length: 8-15 cm depending on integration\nreload: Manual, requires opening maintenance panel\ncyberlimb damage risk: 15-30% chance of structural damage per firing\nweight: Added 0.3-0.5 kg to prosthetic mass",
    tier_availability: "Tier 2+",
    legality: "Prohibited — concealed integrated weapon",
    street_price: "Φ3,000-8,000 depending on caliber and integration quality",
    base_technologies: ["Cyberlimb weapon integration", "Neural trigger interface", "Concealed barrel fabrication"],
    story_hooks: [
      "A handshake killed a man — the Dead Man's Hand fired during what appeared to be a greeting, and the neural trigger log shows it was deliberate, not accidental.",
      "A cyberlimb clinic is offering Dead Man's Hand integration as an unadvertised service — someone needs to find out who is buying and what they are planning."
    ]
  },
  {
    name: "Carrion Defense Works Nerve Agent Micro-Dart System NAMDS-1 'Whisper'",
    aliases: ["Whisper", "NAMDS-1", "Silent Needle", "The Kiss"],
    category: "exotic",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A wrist-mounted micro-dart launcher that fires a near-invisible carbon-fiber dart loaded with a fast-acting nerve agent. The dart is 8mm long, thinner than a human hair at its widest point, and delivered by a compressed gas micro-charge that is nearly silent. The nerve agent — Carrion's proprietary NeuroHalt-7 — causes rapid paralysis of the respiratory system within 90 seconds of injection, and the dart is small enough that most targets do not feel the impact. The NAMDS-1 is the weapon of choice for operators who need a target to die quietly, in public, with no apparent cause of death until a very thorough autopsy finds a carbon-fiber splinter smaller than an eyelash.",
    specifications: "dart size: 8mm length, 0.1mm diameter carbon fiber\npropulsion: Compressed gas micro-charge, 28 dB acoustic signature\neffective range: 1-5 meters\ntoxin: NeuroHalt-7 fast-acting respiratory paralytic\ntime to effect: 60-90 seconds\nmagazine: 3 darts\nweight: 0.05 kg total wrist unit\ndetectability: Dart is undetectable without microscopic examination",
    tier_availability: "Tier 5",
    legality: "Does not officially exist",
    street_price: "Φ200,000 system (extreme rarity)",
    base_technologies: ["Carbon-fiber micro-dart fabrication", "Near-silent compressed gas propulsion", "Fast-acting respiratory paralytic compounds"],
    story_hooks: [
      "Three high-profile deaths in one month, all attributed to sudden respiratory failure, all in public settings — the connection is invisible without knowing what to look for.",
      "A retired Carrion operative is selling their personal NAMDS-1 along with detailed operational logs of every target they ever hit with it — the buyer gets the weapon and the blackmail material."
    ]
  },
  {
    name: "Axiom Systems Perception Filter Rounds PFR-1 'Blind Spot'",
    aliases: ["Blind Spot", "PFR-1", "Ghost Rounds", "Memory Holes"],
    category: "exotic",
    manufacturer: "AXIOM SYSTEMS",
    description: "Ammunition containing a micro-payload of Axiom's experimental neural perception inhibitor compound, which upon detonation releases an aerosol that temporarily disrupts the brain's ability to form new memories and process visual information within a 5-meter radius. Targets within the cloud can see, move, and act, but they cannot form coherent memories of the experience and their visual processing is scrambled — faces become unrecognizable, text becomes illegible, spatial relationships distort. The effect lasts approximately 3 minutes and leaves no lasting damage, but during that window the affected individuals are functionally blind to identity and detail. Axiom developed it for covert operations where witness elimination is undesirable but witness testimony must be neutralized.",
    specifications: "delivery: 40mm grenade launcher compatible\naerosal radius: 5 meters from detonation point\neffect onset: 2 seconds after inhalation\neffect duration: 2-4 minutes\nmemory disruption: Complete anterograde amnesia during exposure\nvisual disruption: Face/text/spatial recognition failure\nweight per round: 0.22 kg\ncountermeasure: Sealed respiratory system prevents inhalation",
    tier_availability: "Tier 5",
    legality: "Does not officially exist",
    street_price: "Φ15,000 per round (if obtainable)",
    base_technologies: ["Neural perception inhibitor aerosolization", "Targeted anterograde amnesia induction", "Visual processing disruption compounds"],
    story_hooks: [
      "Witnesses to a Tier 3 corporate raid all report the same experience — they were there, they saw things happen, but they cannot describe a single face, read a single badge, or recall a single detail.",
      "Axiom's perception inhibitor compound has been synthesized by an independent chemist who is threatening to release the formula publicly — the implications for witness testimony and legal proceedings are catastrophic."
    ]
  },
  {
    name: "Vespid Dynamics Venomfly Micro-Drone Swarm VMS-3 'Murder Cloud'",
    aliases: ["Murder Cloud", "VMS-3", "Venom Swarm", "Flycloud"],
    category: "drone-mounted",
    manufacturer: "VESPID DYNAMICS",
    description: "An advanced evolution of Vespid's micro-drone platform, the VMS-3 deploys a cloud of 50 insect-sized drones that collectively form a mobile toxic fog. Each drone carries a micro-reservoir of contact neurotoxin that it disperses through ultrasonic atomization while flying in a coordinated swarm pattern. The cloud of drones and toxin droplets moves as a coherent mass toward designated coordinates, flowing around obstacles and through openings as small as a ventilation grate. The swarm's collective intelligence allows it to pursue moving targets, avoid countermeasures, and saturate enclosed spaces. Individual drones have only 4 minutes of flight time, but the swarm staggers activation to maintain the cloud for up to 10 minutes.",
    specifications: "drone count: 50 per deployment canister\nindividual drone size: 1.5 cm wingspan\nswarm cloud diameter: 3-8 meters\nflight time: 4 minutes per drone, 10 minutes staggered swarm\ntoxin: Contact neurotoxin, incapacitation in 15 seconds of skin exposure\noperating range: 300 meters from launch\nnavigation: Collective AI with coordinate targeting\nweight: 2.5 kg per deployment canister",
    tier_availability: "Tier 5",
    legality: "Prohibited — autonomous chemical weapon",
    street_price: "Φ95,000 per canister",
    base_technologies: ["50-unit micro-drone collective intelligence", "Ultrasonic toxin atomization", "Coherent swarm movement algorithms"],
    story_hooks: [
      "A Murder Cloud was deployed in a sealed corporate boardroom during a shareholder meeting — the attack was precise, premeditated, and someone had detailed knowledge of the building's ventilation layout.",
      "A VMS-3 swarm was intercepted mid-flight and its navigation data extracted — the coordinates lead to a target who has no idea they were marked for assassination."
    ]
  },
  {
    name: "Sterling-Nakamura Subsonic Eliminator SE-9 'Hush'",
    aliases: ["Hush", "SE-9", "Quiet Pistol", "The Whisper"],
    category: "pistol",
    manufacturer: "STERLING-NAKAMURA",
    description: "A purpose-built suppressed pistol designed from the ground up for silent operation rather than having a suppressor added as an afterthought. The SE-9 uses an integral suppressor barrel, subsonic ammunition with a specialized wipe system, and a locked-breech action that eliminates the mechanical noise of a cycling slide. The result is a pistol with an acoustic signature of 22 dB — quieter than a whispered conversation. Sterling-Nakamura designed it for executive protection details who need to neutralize threats in environments where gunfire would cause panic — corporate events, diplomatic functions, crowded transit hubs. The weapon's existence is an open secret in security circles.",
    specifications: "caliber: 9mm subsonic, proprietary wipe-suppressed\nacoustic signature: 22 dB at 1 meter\nmagazine: 8 rounds\neffective range: 5-25 meters\naction: Locked-breech, no slide cycling noise\nweight: 0.95 kg\nsuppressor: Integral, non-removable\nwipe replacement: Every 50 rounds\nbarrel length: 22 cm including integral suppressor",
    tier_availability: "Tier 4+",
    legality: "Issued — Sterling-Nakamura security details",
    street_price: "Φ45,000",
    base_technologies: ["Integral suppressor barrel design", "Locked-breech silent action", "Subsonic wipe-system sound elimination"],
    story_hooks: [
      "A man was shot dead at a crowded gala and nobody heard anything — the body was not discovered for twelve minutes despite hundreds of people in the room.",
      "A forensic acoustic analyst has developed a method to detect the 22 dB signature of an SE-9 firing using ambient microphone arrays — Sterling-Nakamura wants the research buried."
    ]
  },
  {
    name: "Street Custom 'Brimstone' Thermite Grenade",
    aliases: ["Brimstone", "Hell Egg", "Thermite Bomb", "The Melter"],
    category: "explosive",
    manufacturer: "Street Custom",
    description: "An improvised thermite incendiary device constructed from commercially available iron oxide and aluminum powder packed into a metal container with a magnesium ribbon fuse. The Brimstone burns at approximately 2,500°C for 10-15 seconds, producing molten iron that will burn through virtually any material it contacts — steel plate, engine blocks, vault doors, and human tissue with equal indifference. The device cannot be extinguished once ignited and produces intense white light and acrid smoke. The components are available from industrial supply outlets without restriction, and the assembly requires no specialized knowledge — recipes circulate freely through Tier 1 fabrication networks.",
    specifications: "burn temperature: approximately 2,500°C\nburn duration: 10-15 seconds\npenetration: Will burn through 15mm mild steel\nignition: Magnesium ribbon fuse, 3-second delay\nweight: 0.5-1.5 kg depending on container\ncomponents: Iron oxide powder, aluminum powder, metal container\ncountermeasure: Cannot be extinguished — must burn out\nconstruction time: 20 minutes with available materials",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised incendiary device",
    street_price: "Φ20-60",
    base_technologies: ["Thermite reaction chemistry", "Improvised incendiary fabrication"],
    story_hooks: [
      "A series of vault breaches across Tier 2 all used Brimstone devices to melt through the doors — the thermite composition analysis shows identical chemical ratios, indicating a single supplier.",
      "Someone is dropping Brimstone devices through storm drains into the underground tunnel networks, targeting infrastructure — the fires are unreachable by emergency services."
    ]
  },
  {
    name: "Arcturus Defense Solutions Magnetic Accelerator Rifle MAR-8 'Longbow'",
    aliases: ["Longbow", "MAR-8", "Mag Rifle", "The Reach"],
    category: "rifle",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A precision electromagnetic rifle that uses a 16-stage magnetic accelerator to launch a 2mm tungsten penetrator at velocities exceeding 3,000 m/s. The MAR-8 is Arcturus's answer to the increasing prevalence of heavy composite armor — at hypervelocity, the tungsten penetrator defeats virtually any personal armor system through sheer kinetic energy transfer. The rifle is semi-automatic with a 6-round magazine and requires a backpack-mounted supercapacitor for its power supply. The electromagnetic launch produces no chemical propellant signature, minimal muzzle flash, and a distinctive sharp crack from the hypersonic shockwave that makes the shot direction difficult to identify.",
    specifications: "caliber: 2mm tungsten penetrator\nmuzzle velocity: 3,200 m/s\nmagazine: 6 rounds\neffective range: 200-2,000 meters\npower source: Backpack supercapacitor, 24 shots per charge\nrecharge between shots: 2 seconds\nweight: 5.8 kg rifle, 4.5 kg power pack\npenetration: Rated for all known personal armor systems\nbarrel: 16-stage magnetic accelerator, 80 cm",
    tier_availability: "Tier 5",
    legality: "Military restricted — active combat authorization only",
    street_price: "Φ180,000",
    base_technologies: ["Multi-stage magnetic projectile acceleration", "Hypervelocity tungsten penetrator ballistics", "Compact supercapacitor power systems"],
    story_hooks: [
      "A corporate executive was killed by a 2mm penetrator that passed through their armored vehicle, their body armor, and the seat behind them — the shot came from over a kilometer away and only a MAR-8 has that capability at that range.",
      "Arcturus is field-testing a MAR-8 variant with programmable penetrators that fragment after passing through armor — turning a precision weapon into an anti-personnel system."
    ]
  },
  {
    name: "Tessera Industries Sensory Overload Grenade SOG-3 'Flashbang Plus'",
    aliases: ["Flashbang Plus", "SOG-3", "Sense Bomb", "Full Spectrum"],
    category: "exotic",
    manufacturer: "TESSERA INDUSTRIES",
    description: "An advanced flashbang variant that attacks all five senses simultaneously plus neural interface channels. The SOG-3 detonates with a synchronized package: blinding light across visible and IR spectra, 180 dB acoustic burst, aerosolized capsaicinoid irritant, a burst of electromagnetic interference targeting neural frequencies, and a surface coating on the casing that releases a nausea-inducing olfactory compound on detonation. The combined sensory assault overwhelms both biological and augmented processing capacity, causing a 30-60 second window of total incapacitation regardless of the target's augmentation level or training.",
    specifications: "light output: 20 million candela across visible + IR\nacoustic output: 180 dB at 1 meter\nchemical payload: Aerosolized capsaicinoid + olfactory irritant\nEM output: Broadband neural interface disruption\neffect radius: 8 meters\nincapacitation duration: 30-60 seconds\nweight: 0.35 kg\nfuse: 1.5-second delay",
    tier_availability: "Tier 3+",
    legality: "Licensed — tactical entry teams",
    street_price: "Φ3,500 per grenade",
    base_technologies: ["Multi-spectrum sensory overload engineering", "Synchronized detonation timing", "Neural interface electromagnetic disruption"],
    story_hooks: [
      "An SOG-3 detonated in a crowded nightclub caused permanent sensory damage to twelve patrons — the grenade was thrown as a distraction for a theft, but the confined space amplified the effects beyond safe levels.",
      "A modified SOG-3 has been developed that excludes the electromagnetic component, making it effective only against unaugmented targets — someone is designing weapons that discriminate based on augmentation status."
    ]
  },
  {
    name: "Street Custom 'Mantis' Spring-Loaded Wrist Blade",
    aliases: ["Mantis", "Wrist Blade", "Spring Knife", "Pop Knife"],
    category: "melee",
    manufacturer: "Street Custom",
    description: "A concealed blade mechanism strapped to the inner forearm that deploys a 15cm steel blade with a spring-loaded snap triggered by wrist flexion. The Mantis is one of the oldest street weapon patterns in Meridian 88 — simple, reliable, and almost impossible to detect under a jacket sleeve. The blade extends in under 0.2 seconds and locks in position for use as a stabbing or slashing weapon, then retracts with a manual release. Quality ranges from precision-machined titanium builds that cost thousands to bent steel and rubber-band spring mechanisms that cost almost nothing. The pattern has been copied, modified, and reinvented so many times that no single origin can be traced.",
    specifications: "blade length: 12-18 cm depending on build\ndeployment time: 0.15-0.3 seconds\nmechanism: Spring-loaded with wrist flexion trigger\nretraction: Manual release lever\nconcealment: Under jacket or shirt sleeve\nblade material: Steel, titanium, or ceramic depending on build\nweight: 0.2-0.5 kg\nconstruction: Ranges from machined precision to scrap improvisation",
    tier_availability: "Tier 1+",
    legality: "Prohibited — concealed blade weapon",
    street_price: "Φ30-3,000",
    base_technologies: ["Spring-loaded blade deployment", "Wrist-flexion trigger mechanics"],
    story_hooks: [
      "A ceramic-bladed Mantis was used to kill a target inside a high-security facility — the ceramic blade passed material scanning and the flexion trigger left no detectable mechanism on pat-down.",
      "A Tier 1 community has developed a tradition where receiving a handmade Mantis from a mentor marks the transition to adulthood — the weapons are ceremonial but fully functional."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Plasma Cutter PC-X1 'Dragon's Breath'",
    aliases: ["Dragon's Breath", "PC-X1", "Plasma Gun", "The Torch"],
    category: "energy",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "An experimental weapon that fires a bolt of superheated plasma contained within a magnetic bottle that maintains cohesion for approximately 40 meters before dissipating. The PC-X1 generates plasma by ionizing a hydrogen gas cartridge with an intense electrical arc, then magnetically accelerating the plasma bolt through a shaped barrel. On impact, the magnetic containment fails and the plasma expands explosively, producing a burst of thermal energy sufficient to melt through composite armor and ignite anything flammable within a 1-meter radius. The weapon is loud, visually spectacular, and tactically impractical at anything beyond close-medium range — but within that envelope, nothing survives a direct hit.",
    specifications: "plasma temperature: approximately 15,000°C at generation\neffective range: 5-40 meters before containment dissipation\npower source: Hydrogen gas cartridge + electrical arc generator\nshots per cartridge: 4\nrecharge between shots: 8 seconds\nweight: 7.5 kg\nthermal bloom on impact: 1-meter radius\nacoustic signature: 165 dB at muzzle — extremely loud\nprototypes: 6 units in field testing",
    tier_availability: "Tier 5",
    legality: "Experimental — not classified",
    street_price: "Not commercially available — estimated Φ500,000+",
    base_technologies: ["Magnetically contained plasma generation", "Plasma bolt magnetic acceleration", "Hydrogen ionization arc technology"],
    story_hooks: [
      "An armored convoy was destroyed by what witnesses describe as a bolt of fire that melted through the lead vehicle — the plasma impact signature on the wreckage is unmistakable.",
      "One of the six PC-X1 prototypes has gone missing from Zheng-Dao's weapons lab and the security footage for that night has been erased — someone inside the company walked it out."
    ]
  },
  {
    name: "Vespid Dynamics Adhesive Restraint Launcher ARL-2 'Flypaper'",
    aliases: ["Flypaper", "ARL-2", "Glue Gun", "Stick Launcher"],
    category: "heavy",
    manufacturer: "VESPID DYNAMICS",
    description: "A launcher that fires capsules of Vespid's industrial-strength cyanoacrylate adhesive compound, which upon impact splatters over a 2-meter radius and bonds to any surface with approximately 800 kg/m² shear strength within 4 seconds of air exposure. Targets struck by the adhesive are effectively glued in place — hands bonded to weapons, feet to floors, bodies to walls. The compound is resistant to solvents and requires a proprietary debonding agent that Vespid sells separately, creating a capture-and-control dynamic where the restraining party controls the only means of release.",
    specifications: "capsule range: 10-50 meters\nsplatter radius: 2 meters on impact\nbond strength: 800 kg/m² shear within 4 seconds\ndebonding: Proprietary Vespid solvent only\nmagazine: 4 capsules\nweight: 5.2 kg loaded\ncapsule weight: 0.8 kg each\nadhesive cure time: 4 seconds to full bond\nadhesive persistence: Indefinite without debonding agent",
    tier_availability: "Tier 2+",
    legality: "Licensed — law enforcement and industrial restraint",
    street_price: "Φ7,500 launcher, Φ300 per capsule, Φ150 per debonding dose",
    base_technologies: ["Rapid-cure industrial cyanoacrylate formulation", "Capsule impact dispersal engineering", "Proprietary chemical debonding"],
    story_hooks: [
      "A group of people were found adhesive-bonded to the exterior of a corporate building as a protest display — alive, unharmed, but immovable without Vespid debonding agent that the protesters refuse to provide.",
      "Someone has been Flypapering the doors of Tier 3 apartment buildings shut, trapping residents inside — the debonding agent is being withheld until payment is made."
    ]
  },
  {
    name: "Carrion Defense Works Fragmentation Sleeve FS-2 'Shrapnel Hug'",
    aliases: ["Shrapnel Hug", "FS-2", "Frag Sleeve", "The Embrace"],
    category: "explosive",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A wearable explosive device disguised as an armored forearm sleeve, containing a directional fragmentation charge that detonates outward from the wearer's arm in a 120-degree cone. The FS-2 is a suicide-adjacent weapon designed for absolute last-resort scenarios — the wearer extends their arm toward a target and triggers the detonation, sending a spray of pre-scored tungsten fragments in a directed blast. The sleeve's inner surface contains a thin blast shield that protects the wearer's arm from the worst of the detonation, though burns and shrapnel wounds to the operator are expected. Carrion markets it as a 'final option personal defense system.'",
    specifications: "fragmentation cone: 120 degrees from arm axis\neffective range: 2-8 meters\nfragment material: Pre-scored tungsten cubes, 4mm\nfragment count: approximately 200\nblast shield: Inner ceramic layer, partial operator protection\nweight: 0.8 kg\ndetonation: Neural trigger or manual switch\noperator injury: Expected — burns and minor shrapnel to operator arm",
    tier_availability: "Tier 3+",
    legality: "Restricted — extreme threat response authorization",
    street_price: "Φ6,000",
    base_technologies: ["Directional fragmentation charge design", "Wearable explosive integration", "Partial blast shielding for operator"],
    story_hooks: [
      "A bodyguard detonated an FS-2 sleeve to protect their principal during an ambush — the bodyguard survived with a mangled arm, but the three attackers did not. The footage has gone viral.",
      "Modified FS-2 sleeves with removed blast shields are being sold as improvised suicide weapons — the buyer does not survive, but neither does anyone within 8 meters."
    ]
  },
  {
    name: "Street Custom 'Copperhead' Battery Acid Squirt Gun",
    aliases: ["Copperhead", "Acid Squirter", "Battery Bleeder", "Spit"],
    category: "chemical",
    manufacturer: "Street Custom",
    description: "A modified water pistol or garden sprayer filled with concentrated sulfuric acid harvested from industrial batteries. The Copperhead is the acid attack weapon of Meridian 88's most desperate and vicious operators — cheap to build, easy to conceal, and capable of causing disfiguring chemical burns that no amount of medical treatment can fully repair. The acid is typically concentrated by boiling down battery electrolyte on a hot plate, a process that is itself dangerous and has caused numerous chemical injuries to the builders. The weapon's primary purpose is not killing but terrorizing — the threat of permanent disfigurement is used for intimidation, extortion, and punishment.",
    specifications: "range: 1-3 meters depending on sprayer type\ncapacity: 50-500ml depending on container\nacid concentration: Variable — typically 30-60% sulfuric acid\ncontainer: Modified water pistol, spray bottle, or garden sprayer\nweight: 0.2-1.5 kg loaded\nconstruction: Trivial — fill container with acid\ncountermeasure: Immediate water dilution, medical alkaline flush",
    tier_availability: "Tier 1+",
    legality: "Prohibited — chemical weapon",
    street_price: "Φ5-20",
    base_technologies: ["None — basic chemistry"],
    story_hooks: [
      "An acid attack campaign targeting augmented individuals is dissolving the skin around cybernetic implant interfaces, causing rejection reactions — the attacks are motivated by anti-augmentation ideology.",
      "A Tier 1 gang is using Copperhead threats as an extortion tool — business owners pay or their employees get acid in the face. Someone needs to shut it down before it escalates."
    ]
  },
  {
    name: "Arcturus Defense Solutions Directed EMP Rifle DER-5 'Blackout'",
    aliases: ["Blackout", "DER-5", "EMP Rifle", "Kill Switch"],
    category: "electromagnetic",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A rifle-format directed electromagnetic pulse weapon that fires a focused EMP beam capable of permanently destroying unshielded electronics at ranges up to 100 meters. Unlike area-effect EMP devices, the DER-5 projects a tight beam that can selectively target individual devices, vehicles, or augmented individuals without affecting surrounding electronics. The beam induces a catastrophic current surge in the target's circuitry, burning out processors, memory storage, and power regulation systems. Against cyberware, the effect is particularly devastating — a direct hit can permanently destroy neural interface hardware, requiring full surgical replacement.",
    specifications: "beam type: Focused directional EMP\neffective range: 10-100 meters\nbeam width: 0.5 meter diameter at 50 meters\npower source: Backpack capacitor bank, 10 shots per charge\nrecharge: 5 seconds between shots\nweight: 4.8 kg rifle, 3.5 kg power pack\neffect: Permanent electronics destruction in unshielded targets\nshielded target effect: Temporary disruption, 10-30 seconds",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    street_price: "Φ55,000",
    base_technologies: ["Focused directional EMP beam generation", "Selective electronic targeting", "High-capacity rapid-discharge capacitor banks"],
    story_hooks: [
      "A DER-5 was used to destroy the neural interface of a corporate whistleblower, erasing their stored evidence along with their augmentation — the attack was surgical and left no physical wound.",
      "Anti-augmentation militants have acquired DER-5 units and are conducting drive-by EMP attacks on aug clinics, destroying entire inventories of neural interface hardware."
    ]
  },
  {
    name: "Street Custom 'Rebar Rose' Improvised Spike Mace",
    aliases: ["Rebar Rose", "Spike Club", "Iron Flower", "The Bouquet"],
    category: "melee",
    manufacturer: "Street Custom",
    description: "A length of concrete rebar bent into a handle shape with short sections of rebar welded perpendicular to the shaft, creating a crude spiked mace. The Rebar Rose is found throughout Tier 1 wherever construction salvage is available — it requires only a length of rebar, a welding arc, and the willingness to carry something that looks like a medieval weapon reimagined by an industrial accident. Despite its crude appearance, the weapon is brutally effective: the rebar spikes concentrate impact force into small areas, defeating soft armor through penetration rather than blunt force, and the heavy steel construction makes each swing potentially lethal.",
    specifications: "shaft length: 40-60 cm\nspike count: 4-8 welded perpendicular sections\nspike length: 5-10 cm\ntotal weight: 2-4 kg depending on rebar gauge\nmaterial: Mild steel construction rebar\nconstruction time: 30 minutes with welding equipment\nconcealment: Difficult — typically carried openly or in a duffel bag",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised weapon",
    street_price: "Φ10-40",
    base_technologies: ["Basic welding fabrication"],
    story_hooks: [
      "A community defense group in Tier 1 has standardized on Rebar Rose weapons and organized patrol schedules — they are providing security where none existed, but their methods are increasingly violent.",
      "A distinctive Rebar Rose with a specific welding pattern has been found at multiple crime scenes — the welder's signature is as unique as a fingerprint to someone who knows what to look for."
    ]
  },
  {
    name: "Tessera Industries Phase Disruptor PD-2 'Glitch'",
    aliases: ["Glitch", "PD-2", "Phase Gun", "Reality Hiccup"],
    category: "exotic",
    manufacturer: "TESSERA INDUSTRIES",
    description: "An experimental weapon that generates a localized spatial distortion field at a targeted point, causing matter within the field to briefly phase between states in a way that current physics cannot fully explain. The practical effect is that solid objects within the 1-meter field radius experience a momentary loss of structural integrity — molecular bonds flicker, materials briefly become permeable, and anything caught in the transition snaps back to solidity with catastrophic misalignment. The effect lasts less than a tenth of a second but the material damage is permanent: metal crystallizes along new fault lines, biological tissue suffers cellular disruption, and electronics experience cascading component failure. Tessera's R&D team reportedly does not fully understand why the device works.",
    specifications: "effect radius: 1 meter at target point\neffect duration: 0.08 seconds\neffective range: 5-20 meters\npower source: Experimental spatial distortion generator\nweight: 12 kg with power unit\nrecharge: 120 seconds between activations\nactivations per power cell: 3\nprototypes: 2 confirmed",
    tier_availability: "Tier 5",
    legality: "Experimental — Tessera internal only",
    street_price: "Not available — priceless prototype",
    base_technologies: ["Localized spatial distortion generation", "Molecular bond phase disruption", "Experimental spatial physics applications"],
    story_hooks: [
      "A crime scene shows damage consistent with Phase Disruptor exposure — materials aged and crystallized in a perfect sphere — but only two prototypes exist and both are supposedly locked in Tessera's vault.",
      "A Tessera physicist involved in the PD-2 project has disappeared after claiming the device does not distort space — it briefly opens a window to somewhere else, and something on the other side is looking back."
    ]
  },
  {
    name: "Vespid Dynamics Tracker Round System TRS-4 'Breadcrumb'",
    aliases: ["Breadcrumb", "TRS-4", "Tracker Bullet", "Tag Round"],
    category: "rifle",
    manufacturer: "VESPID DYNAMICS",
    description: "A modified rifle system that fires rounds containing a micro-transmitter that embeds in the target on impact and broadcasts location data for up to 72 hours. The TRS-4 round is a low-velocity subsonic projectile designed to penetrate clothing and embed in subcutaneous tissue without causing significant injury — the goal is tagging, not killing. The embedded transmitter is smaller than a grain of rice and broadcasts on a frequency detectable by Vespid's proprietary tracking receivers at ranges up to 5 kilometers. The round's low velocity means it feels like a hard punch rather than a gunshot, and many targets do not realize they have been tagged until the transmitter is discovered during medical examination.",
    specifications: "caliber: 8mm subsonic tracker round\nmuzzle velocity: 90 m/s — subcutaneous embedding velocity\neffective range: 10-40 meters\ntransmitter size: 2mm x 1mm micro-chip\nbroadcast duration: 72 hours\ntracking range: 5 kilometers with Vespid receiver\nmagazine: 6 rounds\nweight: 2.5 kg rifle\ninjury profile: Minimal — subcutaneous embedding with bruising",
    tier_availability: "Tier 3+",
    legality: "Restricted — authorized surveillance operations",
    street_price: "Φ12,000 rifle system, Φ800 per tracker round",
    base_technologies: ["Micro-transmitter projectile embedding", "Low-velocity subcutaneous delivery", "Long-range location broadcast technology"],
    story_hooks: [
      "A player character discovers a micro-transmitter embedded in their shoulder that they do not remember receiving — someone tagged them with a TRS-4 and has been tracking their movements for days.",
      "A modified TRS-4 round has been developed that delivers a micro-dose of sedative along with the transmitter — targets fall asleep within minutes and wake up tagged."
    ]
  },
  {
    name: "Street Custom 'Glass Garden' Caltrops",
    aliases: ["Glass Garden", "Spike Seeds", "Floor Fangs", "Scatter"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "Improvised caltrops fabricated from broken glass, bent nails, or 3D-printed polymer spikes designed to puncture footwear and vehicle tires. Glass Garden caltrops are scattered in pursuit paths, around defensive positions, or across roadways as area denial tools. The most common variant uses heat-bent roofing nails welded at angles so that one point always faces upward regardless of how the device lands. More sophisticated versions include hollow spikes containing irritant compounds that release into the puncture wound. The name comes from the practice of scattering them across an area like seeds — from above, a deployed field of caltrops on dark pavement is invisible until you step on one.",
    specifications: "construction: Bent nails, broken glass, or 3D-printed polymer\nspike height: 2-4 cm\ndeployment density: 20-50 per square meter for effective coverage\nweight: 2-5 grams each\npackaging: Typically carried in cloth bags of 100-500\ncost per unit: Φ0.10-0.50 depending on construction\neffective against: Foot traffic, standard vehicle tires, thin-soled footwear",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised area denial weapon",
    street_price: "Φ10-50 per bag of 100",
    base_technologies: ["Basic metalworking", "Area denial deployment patterns"],
    story_hooks: [
      "A major transit route has been seeded with thousands of caltrops embedded with a slow-acting corrosive — vehicles that drive over them experience tire failure hours later, far from the deployment site.",
      "Someone is deploying caltrops around the perimeter of a Tier 1 medical clinic every night, preventing patients from reaching the entrance — the clinic is treating people the deployer wants dead."
    ]
  },
  {
    name: "Carrion Defense Works Neural Lash NL-3 'Agony'",
    aliases: ["Agony", "NL-3", "Pain Whip", "The Lash"],
    category: "cyber-integrated",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A flexible polymer whip with embedded electromagnetic emitters along its length that, on contact with a target's cyberware, injects a cascading pain signal directly into their neural interface. The NL-3 does not cause physical damage beyond a superficial welt — its entire function is to hijack the target's pain processing pathways and amplify them to maximum intensity. Augmented targets describe being struck by the Neural Lash as the worst pain they have ever experienced, overriding any pain suppression firmware. Unaugmented targets experience only the physical impact of a polymer whip, which is unpleasant but not extraordinary. Carrion developed it as an interrogation tool.",
    specifications: "whip length: 2 meters\nemitter count: 12 electromagnetic nodes along length\npain induction: Direct neural interface hijacking on contact\neffect on augmented targets: Maximum pain response, 5-15 seconds per strike\neffect on unaugmented targets: Standard physical whip impact only\npower source: Handle-mounted capacitor, 200 strikes per charge\nweight: 0.4 kg\npain suppression bypass: Overrides all known commercial firmware",
    tier_availability: "Tier 3+",
    legality: "Prohibited — torture device",
    street_price: "Φ9,000",
    base_technologies: ["Neural pain pathway hijacking", "Electromagnetic pain signal injection", "Pain suppression firmware bypass"],
    story_hooks: [
      "NL-3 whips are being used in underground fighting pits where augmented fighters compete — the fights are agonizing spectacles that draw wealthy spectators from the upper tiers.",
      "A whistleblower claims Carrion tested the NL-3 on involuntary subjects to calibrate the pain response — the test subjects are still alive and their neural interfaces are permanently damaged."
    ]
  },
  {
    name: "Axiom Systems Cortical Bomb CB-1 'Leash'",
    aliases: ["Leash", "CB-1", "Brain Bomb", "Kill Switch"],
    category: "cyber-integrated",
    manufacturer: "AXIOM SYSTEMS",
    description: "A micro-explosive device designed to be implanted adjacent to a target's neural interface, detonating on receipt of a coded signal or upon detection of tampering. The CB-1 contains 0.5 grams of high-velocity explosive — insufficient to kill outright but precisely enough to destroy the neural interface and cause catastrophic brain damage. Axiom developed it as a compliance enforcement tool for high-risk assets — individuals who possess critical information or capabilities that must be denied to competitors if the asset defects or is captured. The existence of the CB-1 is officially denied, but the distinctive brain damage pattern it produces has been identified in enough autopsy reports to confirm its deployment.",
    specifications: "explosive yield: 0.5g high-velocity micro-charge\ndetonation trigger: Coded RF signal or tamper detection\nimplant size: 3mm x 3mm x 1mm\nimplant location: Adjacent to neural interface housing\neffect: Neural interface destruction + severe traumatic brain injury\nsurvival rate: approximately 40% with immediate medical intervention\ntamper detection: Accelerometer + magnetic field sensor\nRF trigger range: 500 meters from transmitter",
    tier_availability: "Tier 5",
    legality: "Does not officially exist",
    street_price: "Φ50,000 (installation requires neurosurgeon)",
    base_technologies: ["Micro-explosive neural implantation", "RF-triggered detonation systems", "Anti-tamper detection circuitry"],
    story_hooks: [
      "A defecting corporate scientist begs for help removing a CB-1 from their skull — any neurosurgeon who attempts removal risks triggering the tamper detection, and the coded detonation signal could come at any time.",
      "A batch of CB-1 implants has been discovered with a firmware vulnerability that allows the detonation code to be brute-forced — someone is quietly scanning for implanted individuals and building a kill list."
    ]
  },
  {
    name: "Sterling-Nakamura Autonomous Hunter-Killer Drone AHK-7 'Wolfhound'",
    aliases: ["Wolfhound", "AHK-7", "Hunter Drone", "The Hound"],
    category: "drone-mounted",
    manufacturer: "STERLING-NAKAMURA",
    description: "A medium-sized autonomous combat drone the size of a large dog, equipped with a 5.56mm automatic weapon, thermal/optical sensor suite, and an AI capable of independent target acquisition and engagement. The Wolfhound can operate for 8 hours on a single charge, patrol a designated area autonomously, and engage targets matching its programmed threat profile without human authorization. It moves on four articulated legs that allow it to navigate rubble, stairs, and rough terrain that would defeat wheeled or tracked platforms. Sterling-Nakamura markets it as a 'persistent security presence' for high-value installations.",
    specifications: "armament: 5.56mm automatic, 300-round internal magazine\nsensor suite: Thermal + optical + acoustic + motion\nautonomous operation: 8 hours patrol, 2 hours active engagement\nlocomotion: Four articulated legs, 25 km/h maximum\nweight: 35 kg\nAI capability: Independent target acquisition and engagement\nthreat profiling: Programmable ROE with biometric whitelist\narmor: Resistant to handgun calibers\ncommunications: Encrypted mesh network with other AHK units",
    tier_availability: "Tier 4+",
    legality: "Licensed — facility perimeter defense",
    street_price: "Φ150,000",
    base_technologies: ["Quadrupedal autonomous locomotion", "Independent combat AI decision-making", "Articulated terrain navigation systems"],
    story_hooks: [
      "A pack of Wolfhound drones has gone rogue after their command facility was destroyed — they are still following their last programmed patrol pattern, engaging anyone who enters the zone, and nobody can issue a stand-down order.",
      "Someone has hacked a Wolfhound's threat profile to add a specific individual's biometric data — the drone is hunting a person through a residential district."
    ]
  },
  {
    name: "Street Custom 'Baptism' Electrified Water Cannon",
    aliases: ["Baptism", "Shock Hose", "Holy Water", "The Font"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A garden hose or pressure washer connected to a water supply that has been spiked with conductive salt solution, with a high-voltage electrode mounted at the nozzle. The result is a stream of electrified salt water that delivers a sustained electrical shock to anything it contacts. The Baptism is crude, dangerous to the operator, and effective only at close range — but it can incapacitate multiple targets simultaneously by spraying a conductive water stream across a group. The weapon has been adopted by defense-minded communities in Tier 1 who mount it at choke points and doorways as an area denial tool, powered by salvaged electrical panels.",
    specifications: "range: 3-8 meters depending on water pressure\nvoltage: 500-2,000V depending on power source\nconductivity enhancement: Salt solution in water supply\npower source: Salvaged electrical panel or vehicle battery\nwater consumption: 5-15 liters per minute\nweight: Variable — typically fixed installation\noperator risk: Extreme if grounding is improper",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised electrical weapon",
    street_price: "Φ50-200 for conversion",
    base_technologies: ["Conductive fluid electrical delivery", "Improvised high-voltage systems"],
    story_hooks: [
      "A Baptism system installed at a community entrance killed an intruder when the voltage source overloaded — the community faces prosecution while the intruder was carrying weapons intended for a raid.",
      "Someone is setting up concealed Baptism systems in storm drains that activate when anyone steps in the water — the trap is indiscriminate and has injured several children."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Acoustic Denial System ADS-9 'Silence'",
    aliases: ["Silence", "ADS-9", "Sound Wall", "The Mute"],
    category: "sonic",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A tripod-mounted acoustic weapon that projects a precisely calibrated anti-sound field, generating destructive interference patterns that cancel all sound within a targeted 20-meter zone. Within the zone, speech is impossible, radio communication is disrupted by the acoustic interference, gunshots produce no audible report, and alarms cannot be heard. The ADS-9 effectively creates a bubble of absolute silence — a disorienting, unnatural environment where the absence of sound is itself a weapon. Human targets within the zone report intense psychological distress, loss of spatial awareness, and difficulty maintaining balance without acoustic feedback.",
    specifications: "effect radius: 20-meter targeted zone\nfrequency cancellation: 20 Hz to 20 kHz — full human audible range\nsetup time: 45 seconds\npower source: Vehicle feed or portable generator\nweight: 28 kg on tripod\neffect on electronics: Disrupts microphone-based systems within zone\npsychological effect: Disorientation, balance disruption, anxiety\nmaximum deployment time: 30 minutes before emitter overheating",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    street_price: "Φ72,000",
    base_technologies: ["Destructive acoustic interference generation", "Full-spectrum sound cancellation projection", "Targeted acoustic zone control"],
    story_hooks: [
      "A bank robbery was executed inside an ADS-9 silence field — alarms did not sound, victims could not call for help, and security footage shows mouths opening in screams that produced no sound.",
      "An ADS-9 has been permanently installed in a Tier 3 building's basement, creating a zone where no conversation can be recorded or overheard — it has become the most secure meeting space in Meridian 88."
    ]
  },
  {
    name: "Tessera Industries Metamaterial Cloak Rounds MCR-1 'Vanishing Act'",
    aliases: ["Vanishing Act", "MCR-1", "Cloak Bullets", "Ghost Shots"],
    category: "exotic",
    manufacturer: "TESSERA INDUSTRIES",
    description: "Rifle ammunition coated with a metamaterial shell that renders the projectile effectively invisible in flight. The MCR-1 round is a standard 7.62mm projectile encased in a metamaterial jacket that bends visible light around the round as it travels, making it undetectable by optical tracking systems and the human eye. The round is still detectable by radar and thermal sensors, but in practical terms, targets cannot see the bullet coming and optical-based defense systems cannot track it. The metamaterial jacket ablates on impact, leaving a standard wound channel with no exotic trace evidence. Tessera developed it for sniper applications where muzzle flash and bullet trace are the primary indicators of shooter position.",
    specifications: "caliber: 7.62mm metamaterial-jacketed\noptical invisibility: Visible light bending jacket, effective in flight\nradar detectability: Standard — metamaterial does not affect radar signature\nthermal detectability: Standard — metamaterial does not affect thermal\neffective range: 100-800 meters\nmetamaterial ablation: Complete on impact\nweight per round: 12g\ncompatibility: Standard 7.62mm rifle platforms",
    tier_availability: "Tier 5",
    legality: "Prohibited — stealth munition",
    street_price: "Φ2,000 per round",
    base_technologies: ["Metamaterial light-bending projectile jackets", "In-flight optical stealth engineering", "Impact-ablative coating technology"],
    story_hooks: [
      "A target was killed by a 7.62mm round that no witness saw and no optical security system detected in flight — only the impact was recorded, making the shooter's position untraceable.",
      "Tessera's metamaterial jacket technology has been reverse-engineered and someone is applying it to larger munitions — RPG warheads that cannot be seen until they hit."
    ]
  },
  {
    name: "Vespid Dynamics Pheromone Marker Gun PMG-1 'Breadwinner'",
    aliases: ["Breadwinner", "PMG-1", "Scent Gun", "The Marker"],
    category: "exotic",
    manufacturer: "VESPID DYNAMICS",
    description: "A pistol-format weapon that fires capsules of synthetic pheromone compound that mark a target with an invisible chemical signature detectable by Vespid's autonomous drone platforms. A target marked by the PMG-1 becomes a beacon — every Vespid drone within detection range will identify them as a designated target, enabling autonomous tracking and engagement. The pheromone compound bonds to clothing, skin, and hair, and cannot be washed off with standard solvents — it must be neutralized with a UV exposure protocol that itself takes 30 minutes of sustained treatment. The weapon does no damage itself; it simply paints a target for everything else in Vespid's arsenal.",
    specifications: "capsule type: Synthetic pheromone marker compound\neffective range: 5-25 meters\nmarking duration: 48 hours on skin, 72 hours on clothing\ndetection range: Vespid drone platforms detect at 500+ meters\nneutralization: 30-minute UV exposure protocol\nmagazine: 8 capsules\nweight: 0.7 kg\ncompound visibility: Invisible to naked eye, fluoresces under UV",
    tier_availability: "Tier 3+",
    legality: "Restricted — authorized Vespid operations only",
    street_price: "Φ14,000 gun, Φ400 per capsule",
    base_technologies: ["Synthetic pheromone target designation", "Drone-compatible chemical beaconing", "Persistent skin-bonding compound chemistry"],
    story_hooks: [
      "A player character has been marked with a PMG-1 pheromone compound and Vespid drones are converging on their location — they have 48 hours to find a UV neutralization source or go underground.",
      "Someone has obtained PMG-1 compound and is spraying it on random people in crowded areas, triggering Vespid drone responses against innocent civilians — it is creating chaos and nobody knows who is doing it."
    ]
  },
  {
    name: "Street Custom 'Tombstone' Concrete Block Launcher",
    aliases: ["Tombstone", "Block Tosser", "Brick Gun", "Headstone"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A pneumatic launcher constructed from salvaged compressed air tanks and steel pipe that fires concrete blocks, bricks, or similarly sized debris at lethal velocity. The Tombstone uses a burst of compressed air to accelerate a rough projectile through a wide-bore barrel, achieving enough velocity to shatter bone and demolish light barriers at close range. Accuracy is abysmal beyond 10 meters, reloading requires manually shoving another block down the barrel, and the compressed air supply lasts for perhaps 6 shots before needing refill. Despite every possible disadvantage, the Tombstone fills a niche in Tier 1 warfare: it uses ammunition that is literally everywhere, and the impact of a 2-kilogram concrete block at 50 m/s is not something that body armor is designed to handle.",
    specifications: "projectile: Concrete block, brick, or similar debris (1-3 kg)\nmuzzle velocity: 40-60 m/s\neffective range: 3-10 meters\nbarrel diameter: 15-20 cm\nair supply: 6 shots per tank at 150 PSI\nreload time: 5-8 seconds\nweight: 8-15 kg depending on construction\naccuracy: Approximately 1-meter spread at 5 meters",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised weapon",
    street_price: "Φ40-120",
    base_technologies: ["Pneumatic acceleration of mass projectiles", "Improvised wide-bore launcher fabrication"],
    story_hooks: [
      "A barricaded position in Tier 1 has been holding off security forces using Tombstone launchers — the cheap weapons are surprisingly effective against personnel, and ammunition is the rubble around them.",
      "A modified Tombstone has been built with a narrower bore that fires steel ball bearings at much higher velocity — it is essentially a homemade cannon and someone is using it to puncture vehicle armor."
    ]
  },
  {
    name: "Sterling-Nakamura Legal Override Pistol LOP-1 'Compliance'",
    aliases: ["Compliance", "LOP-1", "Law Gun", "The Judge"],
    category: "pistol",
    manufacturer: "STERLING-NAKAMURA",
    description: "A sidearm that fires coded RF rounds which, upon penetrating the skin, broadcast an override signal to the target's neural interface that triggers a mandatory system lockdown. The lockdown disables voluntary motor control for 60 seconds while broadcasting the target's identity, location, and biometric status to law enforcement networks. Sterling-Nakamura developed it under contract with Meridian 88 governance bodies as a 'lawful compliance enforcement tool' — it is essentially a gun that arrests people by hijacking their cyberware. The round's RF signal uses an encryption key that only law enforcement neural interfaces can revoke, making the lockdown non-negotiable. Unaugmented individuals are unaffected by the RF component and experience only a low-velocity impact bruise.",
    specifications: "caliber: 6mm RF-coded compliance round\nmuzzle velocity: 120 m/s — subcutaneous embedding\nmagazine: 12 rounds\neffective range: 5-25 meters\nlockdown duration: 60 seconds\nmotor control override: Full voluntary motor suppression\nidentity broadcast: Automatic to law enforcement networks\neffect on unaugmented: Bruise-level impact only\nweight: 0.8 kg",
    tier_availability: "Tier 3+",
    legality: "Issued — law enforcement only",
    street_price: "Φ35,000 (extremely rare black market)",
    base_technologies: ["RF neural interface override broadcasting", "Coded compliance signal encryption", "Embedded law enforcement network integration"],
    story_hooks: [
      "Black market LOP-1 rounds have been reprogrammed to broadcast a different identity than the target's — someone is using compliance rounds to frame innocent people for crimes.",
      "A vulnerability in the compliance signal encryption has been discovered that allows any RF transmitter to trigger the 60-second lockdown — every augmented person in Meridian 88 is potentially vulnerable."
    ]
  },
  {
    name: "Axiom Systems Biometric Lock Rifle BLR-3 'Personal'",
    aliases: ["Personal", "BLR-3", "Bio Rifle", "Keyed Gun"],
    category: "rifle",
    manufacturer: "AXIOM SYSTEMS",
    description: "A bullpup assault rifle with an integrated biometric lock system that prevents anyone other than the registered owner from firing it. The BLR-3 reads the operator's palmprint, pulse pattern, and neural interface signature simultaneously — all three must match the registered profile or the trigger mechanism remains physically locked. Axiom developed it to prevent weapon capture and misuse during corporate conflict, ensuring that seized weapons cannot be turned against their issuing force. The biometric system has been praised as a safety innovation and condemned as a control mechanism — operators cannot share weapons with allies, cannot use captured enemy BLR-3s, and cannot sell or transfer the weapon without an Axiom technician re-registering it.",
    specifications: "caliber: 5.56mm caseless\nmagazine: 40 rounds\nrate of fire: 750 RPM\neffective range: 50-400 meters\nbiometric lock: Palmprint + pulse + neural interface triple authentication\nauthentication time: 0.3 seconds on grip\nweight: 3.5 kg\nfire modes: Semi, burst, full auto\nre-registration: Requires Axiom technician",
    tier_availability: "Tier 3+",
    legality: "Licensed — Axiom security forces",
    street_price: "Φ12,000 (useless without biometric bypass, bypass adds Φ8,000)",
    base_technologies: ["Triple biometric authentication", "Physical trigger lock mechanisms", "Neural interface weapon registration"],
    story_hooks: [
      "A dead Axiom operative's BLR-3 was fired after their death — the biometric system accepted a dead hand, dead pulse, and a cloned neural interface signature, meaning someone duplicated the operator's entire biometric profile.",
      "A firmware exploit allows BLR-3 rifles to be permanently unlocked by injecting a specific code through the maintenance port — the exploit is spreading through underground channels and Axiom cannot patch it remotely."
    ]
  },
  {
    name: "Carrion Defense Works Pathogen Delivery System PDS-4 'Typhoid'",
    aliases: ["Typhoid", "PDS-4", "Plague Launcher", "Patient Zero"],
    category: "chemical",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A weapon system designed to deliver engineered biological agents via aerosolized capsules launched from a modified grenade launcher platform. The PDS-4 fires capsules that burst on impact, releasing clouds of pathogen-carrying particles calibrated for maximum infection rate through respiratory exposure. Carrion offers a range of pathogen payloads — from fast-acting incapacitants that simulate severe flu symptoms to slow-developing agents designed for covert population-level delivery. The system exists in a legal gray zone: Carrion officially markets the launcher for 'agricultural pest control aerosol delivery,' and the pathogen payloads are sold through separate, deniable supply chains.",
    specifications: "launcher type: Modified 40mm grenade platform\neffective range: 30-100 meters\naerosolization radius: 8 meters from impact\npathogen options: Fast incapacitant, slow-develop covert, targeted genetic-marker selective\ninfection rate: 85-95% for unprotected respiratory exposure\ncapsule weight: 0.2 kg\nmagazine: 6 capsules\nlauncher weight: 3.8 kg\ncountermeasure: NBC-grade respiratory filtration",
    tier_availability: "Tier 5",
    legality: "Prohibited — biological weapon (officially does not exist as weapon)",
    street_price: "Φ85,000 system, Φ5,000-50,000 per pathogen payload",
    base_technologies: ["Engineered pathogen aerosolization", "Targeted biological agent delivery", "Respiratory infection optimization"],
    story_hooks: [
      "A mystery illness spreading through Tier 2 has been traced to PDS-4 capsule fragments found in ventilation systems — someone deployed a slow-acting pathogen and the infection is still spreading.",
      "Carrion's genetic-marker-selective pathogen has been loaded into PDS-4 capsules — it only affects people with a specific genetic heritage, making it an instrument of ethnic targeting."
    ]
  },
  {
    name: "Street Custom 'Spiderbite' Electrified Barbed Wire Launcher",
    aliases: ["Spiderbite", "Wire Tosser", "Tangle Gun", "Barb Launcher"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A spring-loaded tube launcher that fires a compressed bundle of electrified barbed wire that expands on release, creating a 3-meter tangle of sharp, shocking wire that wraps around anything in its path. The wire is standard concertina barbed wire crimped to a battery-powered electrode that delivers repeated electrical shocks through the barbs. The weapon is designed to entangle and incapacitate — targets wrapped in electrified barbed wire cannot move without driving barbs deeper into their skin while receiving continuous shocks. Extraction requires cutting the wire, which itself is dangerous due to the live electrical charge. It is a weapon of cruelty more than lethality, and its use is a statement of intent.",
    specifications: "wire length: 5 meters compressed, 3-meter tangle deployed\nlaunch range: 3-8 meters\nbarb spacing: 5 cm\nelectrical charge: 400V through wire barbs\nbattery life: 10 minutes of continuous discharge\nweight: 2.5 kg launcher, 1.2 kg per wire bundle\nreload: 15 seconds per bundle\nextraction time: 5-15 minutes with wire cutters, if you can touch it",
    tier_availability: "Tier 1+",
    legality: "Prohibited — electrified entanglement weapon",
    street_price: "Φ150-400",
    base_technologies: ["Spring-launched wire bundle deployment", "Electrified barbed wire fabrication"],
    story_hooks: [
      "Spiderbite launchers have been deployed as perimeter defense around a Tier 1 refugee camp — the wire tangles are keeping threats out, but they are also keeping people in.",
      "A serial attacker is targeting corporate security patrols with Spiderbites, wrapping individual officers in electrified wire and leaving them for hours before anyone finds them."
    ]
  },
  {
    name: "Arcturus Defense Solutions Orbital Strike Designator OSD-1 'Finger of God'",
    aliases: ["Finger of God", "OSD-1", "Orbital Painter", "Sky Call"],
    category: "heavy",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "Not a weapon itself but a targeting device that designates coordinates for Arcturus's constellation of low-orbit kinetic bombardment satellites. The OSD-1 is a handheld laser designator that paints a target with a coded beam readable by orbital platforms, which then release a tungsten rod — colloquially called a 'telephone pole' — that impacts the designated point at terminal velocity after de-orbiting. The kinetic energy of the impact is equivalent to a tactical nuclear weapon without the radiation. The OSD-1 is the most closely controlled weapon in Arcturus's arsenal — each unit is individually tracked, requires biometric plus passcode activation, and the orbital strike requires secondary authorization from Arcturus command. Despite these controls, the existence of a handheld device that can call down orbital bombardment has reshaped the calculus of power in Meridian 88.",
    specifications: "designator weight: 1.2 kg handheld unit\ndesignation method: Coded laser target painting\norbital platform: Arcturus kinetic bombardment satellite constellation\nimpactor: Tungsten rod, 6.1m x 0.3m, 4,000 kg\nterminal velocity: approximately 11,000 m/s\nimpact yield: Equivalent to approximately 11.5 tons TNT\nstrike delay: 8-12 minutes from designation to impact\nauthorization: Dual — operator biometric + Arcturus command confirmation",
    tier_availability: "Tier 5",
    legality: "Arcturus sovereign military asset — no external legal framework",
    street_price: "Not available — no known black market units",
    base_technologies: ["Orbital kinetic bombardment platform", "Coded laser target designation", "Tungsten rod de-orbit mechanics"],
    story_hooks: [
      "An OSD-1 unit has been reported missing from an Arcturus field team — the orbital platforms are still active and awaiting designation, and whoever has the device could level a city block with a button press and Arcturus authorization.",
      "Someone used an OSD-1 to destroy a rival corporation's facility — the crater is 30 meters across and Arcturus claims the authorization was legitimate, raising the question of what justifies calling down orbital bombardment on a civilian target."
    ]
  },
  {
    name: "Tessera Industries Anti-Material Beam AMB-2 'Eraser'",
    aliases: ["Eraser", "AMB-2", "Delete Gun", "The Cleaner"],
    category: "energy",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A crew-served directed energy weapon that fires a sustained coherent particle beam capable of ablating solid matter at the molecular level. The AMB-2 does not burn, explode, or impact — it disassembles the target material one molecular layer at a time, converting solid matter into a fine dust that disperses on the wind. Against vehicle armor, the beam carves through plating like an invisible saw. Against structures, it cuts clean holes with mirror-smooth edges. Against biological targets, the effect is so rapid that the nervous system does not have time to register pain before the affected tissue simply ceases to exist. Tessera developed it for precision demolition of contaminated structures, and its military applications are a source of ongoing internal corporate debate.",
    specifications: "beam type: Coherent particle beam\neffective range: 20-300 meters\nabalation rate: 2mm of hardened steel per second of sustained exposure\npower source: Vehicle-mounted fusion generator\nbeam diameter: 5 cm focused\nweight: 120 kg on tripod (plus generator)\ncrew: 2 operators\ncontinuous operation: 60 seconds before emitter cooling required\ncooling period: 90 seconds",
    tier_availability: "Tier 5",
    legality: "Tessera internal — military testing only",
    street_price: "Not commercially available",
    base_technologies: ["Coherent particle beam generation", "Molecular ablation targeting", "Fusion-powered directed energy delivery"],
    story_hooks: [
      "A building wall was cut open with mirror-smooth edges in a perfect rectangle — nothing in the conventional weapons catalog makes cuts like that, but a Tessera AMB-2 does.",
      "A Tessera engineer has leaked that the AMB-2's particle beam has an unintended secondary effect — the molecular dust it produces is carcinogenic, and contamination zones around test sites are making people sick years later."
    ]
  },
  {
    name: "Street Custom 'Ghostwriter' Grafitti Flamethrower",
    aliases: ["Ghostwriter", "Paint Torch", "Flame Tag", "The Spray"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A modified paint sprayer loaded with a flammable gel mixture — typically a combination of spray paint, petroleum jelly, and grain alcohol — that produces a sticky, burning stream used for both arson and literal weaponized graffiti. The Ghostwriter fires a 2-meter stream of flaming gel that adheres to surfaces and burns for 30-60 seconds, long enough to permanently mark concrete and steel with scorched patterns. Operators use it to burn political messages, territorial markers, and threat sigils directly into building facades. It is simultaneously a weapon and an art tool, and the most skilled Ghostwriters are respected in Tier 1 culture for their ability to burn elaborate images under pressure.",
    specifications: "range: 1-3 meters gel stream\nfuel: Flammable gel mixture (paint + petroleum jelly + alcohol)\nburn duration: 30-60 seconds on surfaces\nreservoir: 1-2 liters, good for 3-5 tags or 15 seconds continuous\nweight: 1.5-3 kg loaded\nignition: Piezoelectric lighter element at nozzle\nadherence: Gel sticks to vertical and overhead surfaces\nconstruction: Modified pump-action paint sprayer",
    tier_availability: "Tier 1+",
    legality: "Prohibited — improvised incendiary",
    street_price: "Φ30-80",
    base_technologies: ["Flammable gel adhesion chemistry", "Modified sprayer delivery systems"],
    story_hooks: [
      "A Ghostwriter artist known only by their burn-mark style has been leaving prophetic messages scorched into corporate buildings — the messages have been accurately predicting corporate actions weeks before they happen.",
      "A Ghostwriter attack on a residential building went wrong when the gel ignited inside the sprayer, killing the operator — their dying burn-mark is being treated as a martyr's last word."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Mass Driver Turret MDT-6 'Trebuchet'",
    aliases: ["Trebuchet", "MDT-6", "Mass Driver", "The Hurler"],
    category: "heavy",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A vehicle-mounted electromagnetic mass driver that accelerates 5 kg projectiles to hypersonic velocity, functioning as a modern artillery piece with no chemical propellant. The MDT-6 uses a 20-stage linear electromagnetic accelerator mounted on a tracked vehicle chassis, capable of hurling dense metal projectiles up to 15 kilometers with GPS-guided precision. The impact of a 5 kg tungsten slug at Mach 7 produces a crater equivalent to a 500 kg bomb without any explosive — pure kinetic energy transfer. Zheng-Dao developed the MDT-6 for long-range fire support, and its ability to deliver devastating strikes with no explosive signature makes it difficult for counterbattery systems to detect and return fire on.",
    specifications: "projectile mass: 5 kg tungsten\nmuzzle velocity: Mach 7 (approximately 2,400 m/s)\nmaximum range: 15 kilometers\naccelerator stages: 20-stage linear electromagnetic\nrate of fire: 1 round per 20 seconds\nmagazine: 12 rounds in autoloader\nmount: Tracked vehicle chassis\nweight: 18,000 kg complete system\npower source: Onboard fusion generator",
    tier_availability: "Tier 5",
    legality: "Military restricted — strategic weapons authorization",
    street_price: "Not available — strategic military asset",
    base_technologies: ["Linear electromagnetic mass acceleration", "Hypersonic projectile guidance", "Vehicle-mounted fusion power generation"],
    story_hooks: [
      "An unexplained crater in Tier 3 has been attributed to a gas explosion, but the impact pattern is consistent with a mass driver strike — someone used strategic artillery inside the city.",
      "A Zheng-Dao MDT-6 was disabled during transit and the vehicle crew was found dead with no visible wounds — the weapon was not stolen, but the fire control software was accessed."
    ]
  },
  {
    name: "Vespid Dynamics Swarm Intelligence Mine SIM-2 'Anthill'",
    aliases: ["Anthill", "SIM-2", "Smart Mine", "Bug Trap"],
    category: "explosive",
    manufacturer: "VESPID DYNAMICS",
    description: "A buried anti-personnel mine that, when triggered, launches a swarm of six micro-drones rather than detonating a conventional explosive. The micro-drones emerge from the mine casing, acquire the triggering target using thermal and motion tracking, and pursue it at high speed before detonating their individual shaped charges on contact. The SIM-2 combines the area denial function of a mine with the target-tracking capability of a guided weapon — it cannot be outrun, and it pursues targets who would normally survive by stepping off a conventional mine's pressure plate. Vespid markets it as a 'persistent area denial system with target discrimination.'",
    specifications: "trigger: Seismic/pressure activation with thermal confirmation\ndrone count: 6 pursuit micro-drones per mine\nindividual warhead: 5g shaped charge\npursuit speed: 60 km/h\npursuit range: 200 meters from mine location\nflight time: 30 seconds per drone\nmine dimensions: 15 cm diameter, 8 cm height\nburial depth: 5-10 cm\nweight: 1.5 kg per mine",
    tier_availability: "Tier 4+",
    legality: "Prohibited — autonomous lethal mine",
    street_price: "Φ16,000 per mine",
    base_technologies: ["Seismic-triggered mine activation", "Pursuit micro-drone deployment", "Autonomous target-tracking munitions"],
    story_hooks: [
      "A hiking trail has been seeded with SIM-2 mines and three hikers have been killed by pursuit drones — the mines were planted months ago and the trail has been in use the entire time.",
      "A SIM-2 mine malfunctioned and launched its drones without a target — the six micro-drones are now circling a Tier 2 intersection, armed and seeking anything that moves."
    ]
  },
  {
    name: "Sterling-Nakamura Diplomatic Protection Cane DPC-1 'Ambassador'",
    aliases: ["Ambassador", "DPC-1", "Sword Cane", "The Walking Stick"],
    category: "melee",
    manufacturer: "STERLING-NAKAMURA",
    description: "An elegant walking cane concealing three weapon systems: a 50cm monocrystalline steel blade hidden within the shaft, a single-shot 10mm derringer built into the handle, and a contact taser in the ferrule capable of delivering a 100,000V discharge. Sterling-Nakamura issues the DPC-1 to senior diplomatic staff as a personal defense weapon that maintains the appearance of civilian propriety while providing lethal capability. The cane is constructed from carbon fiber with brass fittings and weighs slightly more than a standard walking cane — not enough to draw suspicion. The blade deploys with a twist-pull of the handle, the derringer fires with a concealed trigger in the grip, and the taser activates by pressing the ferrule firmly against a target.",
    specifications: "blade: 50 cm monocrystalline steel, concealed in shaft\nderringer: Single-shot 10mm, handle-mounted, 5-meter range\ntaser: 100,000V contact discharge in ferrule\ncane length: 95 cm\ncane weight: 0.9 kg\nconstruction: Carbon fiber shaft, brass fittings\nblade deployment: Twist-pull handle, 0.5 seconds\nderringer reload: Breech-load single round",
    tier_availability: "Tier 4+",
    legality: "Issued — Sterling-Nakamura diplomatic staff",
    street_price: "Φ25,000 (rare collector item)",
    base_technologies: ["Multi-weapon concealment integration", "Monocrystalline blade fabrication", "Miniaturized derringer engineering"],
    story_hooks: [
      "A diplomat killed an assailant with a DPC-1 blade at a state function — the self-defense was legally justified, but the weapon's existence is now public knowledge and every diplomatic meeting has become a security concern.",
      "Counterfeit DPC-1 canes are being manufactured with lower quality materials — the blades snap during use and the derringers misfire, turning a reliable weapon into a liability."
    ]
  },
  {
    name: "Street Custom 'Widowmaker' Rigged Power Tool",
    aliases: ["Widowmaker", "Kill Saw", "Murder Drill", "Construction Special"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "Any standard power tool — circular saw, angle grinder, reciprocating saw, drill — modified for use as a weapon by removing safety guards, locking the trigger in the on position, and in some cases extending the blade or bit for reach. Widowmakers are the weapons of people who cannot afford weapons but have access to a job site. The most common variant is an angle grinder with the guard removed and a cutting disc replaced with a diamond blade, capable of cutting through body armor and bone with equal efficiency. The psychological impact of an attacker running at you with a screaming power tool is significant — it is loud, sprays sparks, and communicates a level of commitment that discourages engagement.",
    specifications: "tool type: Variable — angle grinder, circular saw, reciprocating saw, drill\nblade/bit: Standard industrial, often upgraded to diamond or carbide\npower source: Tool's original battery or corded\nweight: 1.5-5 kg depending on tool\nmodification: Guard removal, trigger lock, extended blade\neffective range: Arm's reach\nnoise: 85-110 dB — extremely loud",
    tier_availability: "Tier 1+",
    legality: "Prohibited when modified — modified power tool as weapon",
    street_price: "Φ20-100 for modifications, base tool Φ50-300",
    base_technologies: ["Power tool modification for combat", "Safety bypass engineering"],
    story_hooks: [
      "A construction crew moonlighting as enforcers uses Widowmakers exclusively — their attacks look like workplace accidents and the weapons are their legitimate work tools.",
      "A Tier 1 gladiatorial event features Widowmaker combat — fighters armed with modified power tools in a steel cage, and the audience is placing bets through corporate gambling networks."
    ]
  },
  {
    name: "Axiom Systems Synapse Interruptor SI-3 'Stutter'",
    aliases: ["Stutter", "SI-3", "Brain Scrambler", "The Hiccup"],
    category: "electromagnetic",
    manufacturer: "AXIOM SYSTEMS",
    description: "A pistol-format directed electromagnetic weapon that fires a precisely modulated pulse targeting the motor cortex through neural interface pathways. Targets hit by the SI-3 experience a temporary but complete disruption of voluntary motor control — muscles fire randomly, coordination collapses, speech becomes incoherent, and fine motor skills disappear for 20-30 seconds. The effect resembles a severe seizure without the associated neurological damage, though repeated exposure has been linked to cumulative motor pathway degradation. Axiom describes it as a 'motor compliance tool' in internal documentation.",
    specifications: "pulse type: Modulated motor cortex disruption\neffective range: 3-15 meters\neffect on augmented: Total motor control loss, 20-30 seconds\neffect on unaugmented: Reduced — mild coordination disruption, 5-10 seconds\npulses per charge: 20\nrecharge: 2 seconds between pulses\nweight: 0.6 kg\npower source: Integrated capacitor pack\ncumulative effect: Motor pathway degradation with repeated exposure",
    tier_availability: "Tier 3+",
    legality: "Restricted — authorized compliance enforcement",
    street_price: "Φ22,000",
    base_technologies: ["Motor cortex electromagnetic disruption", "Neural interface pathway targeting", "Modulated compliance pulse generation"],
    story_hooks: [
      "A security contractor has been using an SI-3 repeatedly on the same detainee during interrogation — the cumulative motor damage has left the victim with permanent tremors and the contractor is claiming standard procedure.",
      "Modified SI-3 units have appeared that affect the autonomic motor system instead of voluntary — targets stop breathing for 20 seconds, turning a compliance tool into an assassination weapon."
    ]
  },
  {
    name: "Carrion Defense Works Necrotizing Agent Sprayer NAS-2 'Rot'",
    aliases: ["Rot", "NAS-2", "Flesh Eater", "Decay Gun"],
    category: "chemical",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A handheld sprayer that disperses a fast-acting necrotizing agent — Carrion's proprietary NecroSolve compound — that causes rapid enzymatic breakdown of biological tissue on contact. The compound is highly selective for living tissue and has no effect on synthetic materials, making it particularly horrifying: it eats flesh but leaves clothing, cyberware, and equipment untouched. A 2-second exposure to the spray causes surface tissue necrosis across the contact area within 30 seconds, progressing to deep tissue destruction within 5 minutes if not neutralized. The compound can be neutralized by alkaline solution, but the window for effective treatment is narrow.",
    specifications: "spray range: 1-4 meters\ncompound: NecroSolve fast-acting necrotizing enzyme\ntissue selectivity: Biological only — no effect on synthetics\nonset: Surface necrosis in 30 seconds\ndeep tissue destruction: 5 minutes if untreated\nneutralization: Alkaline solution within 2 minutes\nreservoir: 10 spray bursts\nweight: 0.8 kg\ncountermeasure: Sealed suit, immediate alkaline flush",
    tier_availability: "Tier 4+",
    legality: "Prohibited — chemical weapon",
    street_price: "Φ28,000",
    base_technologies: ["Fast-acting necrotizing enzyme synthesis", "Tissue-selective chemical targeting", "Enzymatic biological degradation"],
    story_hooks: [
      "A body was found with NecroSolve exposure patterns — the flesh was destroyed but every piece of cyberware was intact and undamaged, neatly arranged as if someone was harvesting augmentations from the living.",
      "Carrion's NecroSolve compound has been detected in a municipal water supply at extremely low concentrations — not enough to cause necrosis, but enough to cause chronic skin inflammation across an entire district."
    ]
  },
  {
    name: "Tessera Industries Quantum Entanglement Detonator QED-1 'Certainty'",
    aliases: ["Certainty", "QED-1", "Quantum Trigger", "The Inevitability"],
    category: "explosive",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A detonation system that uses quantum-entangled particle pairs to trigger explosive charges with zero transmission delay and absolute interception immunity. The QED-1 consists of a trigger device and up to 12 paired receiver modules, each containing one half of an entangled particle pair. When the trigger device measures its particles, the entangled partners in the receivers collapse simultaneously, triggering detonation. There is no signal to jam, no frequency to intercept, no wire to cut, and no delay to exploit. The trigger works through any material barrier at any distance. Tessera developed it for controlled demolition in electromagnetically contested environments, but its implications for the bomb disposal profession are existential.",
    specifications: "trigger unit: Handheld, 0.3 kg\nreceiver modules: Up to 12 paired detonators\nactivation delay: Zero — quantum instantaneous\ninterception immunity: Absolute — no signal exists between trigger and receiver\nrange: Unlimited — quantum entanglement is distance-independent\nreceiver module size: 4 cm x 2 cm\ncompatibility: Standard detonator interface\nentangled pair lifespan: 30 days from generation before decoherence",
    tier_availability: "Tier 5",
    legality: "Tessera internal — demolition authorization only",
    street_price: "Φ500,000+ (if any have reached black market)",
    base_technologies: ["Quantum entangled particle pair generation", "Instantaneous quantum state collapse triggering", "Unjammable detonation signaling"],
    story_hooks: [
      "A bomb disposal team encountered an explosive device with no detectable trigger mechanism — no wire, no radio receiver, no timer. The bomb went off while they were examining it, and the trigger was pressed from across the city.",
      "Tessera is desperately searching for three QED-1 receiver modules that went missing from their inventory — somewhere in Meridian 88, three bombs are planted that cannot be disarmed and will detonate the instant someone presses a button."
    ]
  },
  {
    name: "Street Custom 'Mockingbird' Voice-Activated Trap Gun",
    aliases: ["Mockingbird", "Voice Trap", "Name Gun", "The Listener"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A concealed firearm — typically a sawed-off shotgun or heavy-caliber pistol — connected to a voice-recognition trigger that fires when it detects a specific spoken phrase or voice pattern. The Mockingbird is hidden behind walls, under furniture, or inside containers, aimed at a kill zone, and programmed to fire when the target speaks a particular word or when a specific person's voice is detected. The voice recognition uses salvaged smart-home assistant hardware, which means it is affordable, widely available, and approximately 85% accurate — the 15% error rate means Mockingbirds occasionally fire at the wrong person, which their deployers consider an acceptable margin. The weapon represents the intersection of cheap AI hardware and lethal intent.",
    specifications: "firearm: Variable — sawed-off shotgun, heavy pistol, or similar\ntrigger: Salvaged voice-recognition hardware\nactivation: Specific phrase or voice-pattern match\naccuracy of recognition: approximately 85%\nrange: Determined by firearm and placement\npower source: Battery-powered voice module, 72-hour standby\ninstallation time: 30-60 minutes\nconcealment: Behind walls, under furniture, inside containers",
    tier_availability: "Tier 1+",
    legality: "Prohibited — automated lethal trap",
    street_price: "Φ100-400 plus firearm cost",
    base_technologies: ["Voice-recognition trigger interface", "Automated trap weapon deployment", "Smart-home hardware weaponization"],
    story_hooks: [
      "A prominent community leader was killed when they said their own name at their front door — a Mockingbird had been installed inside their wall, programmed to their voice pattern.",
      "Mockingbird traps programmed to common greetings have been found in abandoned buildings in Tier 1 — they are not targeted at anyone specific, they are designed to kill whoever enters."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Neutron Emitter NE-1 'Clean Sweep'",
    aliases: ["Clean Sweep", "NE-1", "Neutron Gun", "The Cleaner"],
    category: "energy",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A directed neutron radiation weapon that kills biological organisms while leaving structures, equipment, and electronics completely intact. The NE-1 projects a focused beam of fast neutrons that pass through walls and cover, irradiating everything biological within the beam path with a lethal dose of radiation. Death from neutron exposure is not immediate — targets may survive for hours or days depending on dose, experiencing progressive organ failure as their cells die from radiation damage. The weapon's strategic value lies in its ability to kill the occupants of a building without damaging the building itself — perfect for seizing infrastructure, reclaiming facilities, or silently depopulating areas for reoccupation.",
    specifications: "beam type: Focused fast neutron emission\npenetration: Passes through standard construction materials\nlethal dose delivery: 10 seconds of exposure at 50 meters\neffective range: 10-200 meters\nweight: 85 kg on vehicle mount\npower source: Onboard nuclear source + accelerator\ncrew: 3 operators with radiation shielding\ndeath timeline: Hours to days depending on exposure duration\ncountermeasure: Borated polyethylene shielding, 10 cm minimum",
    tier_availability: "Tier 5",
    legality: "Strategic weapon — Zheng-Dao sovereign authorization only",
    street_price: "Not available",
    base_technologies: ["Directed fast neutron generation", "Focused neutron beam projection", "Biological-selective radiation targeting"],
    story_hooks: [
      "An entire apartment building's residents died over 72 hours from what was officially called a chemical leak — but a radiation specialist has found neutron activation signatures in the building's steel structure.",
      "A Zheng-Dao NE-1 was deployed against a fortified position during a corporate conflict, and the 'clean' building was reoccupied within hours — but the rapid reoccupation timeline suggests they knew it was a neutron weapon and had the seizure force ready before the attack."
    ]
  },
  {
    name: "Vespid Dynamics Paralytic Wasp Drone PWD-4 'Yellowjacket'",
    aliases: ["Yellowjacket", "PWD-4", "Sting Drone", "The Wasp"],
    category: "drone-mounted",
    manufacturer: "VESPID DYNAMICS",
    description: "A small quad-rotor drone the size of a fist, painted in Vespid's characteristic yellow-and-black livery, equipped with a retractable syringe containing a paralytic compound. The Yellowjacket autonomously navigates to a designated target, matches their pace, and delivers a single injection — typically to the neck or exposed limb — before returning to its launch point for reload. The paralytic compound immobilizes the target for 15-20 minutes while leaving them fully conscious and aware. The drone's small size and quiet electric motors make it difficult to detect in urban environments, and its yellow-black coloring is both a brand statement and a psychological weapon — in Meridian 88, seeing anything yellow and black with rotors means Vespid is watching.",
    specifications: "drone size: 12 cm rotor-to-rotor\nflight time: 15 minutes\noperating range: 500 meters from controller\nsyringe payload: 2ml paralytic compound\ninjection method: Retractable spring-loaded syringe\nparalysis duration: 15-20 minutes\nmotor noise: 35 dB at 2 meters\nnavigation: Autonomous with facial recognition target lock\nspeed: 45 km/h maximum",
    tier_availability: "Tier 3+",
    legality: "Restricted — Vespid authorized operations",
    street_price: "Φ18,000 per drone, Φ300 per paralytic payload",
    base_technologies: ["Autonomous injection drone navigation", "Facial recognition target acquisition", "Spring-loaded syringe delivery systems"],
    story_hooks: [
      "A string of muggings involve victims being paralyzed by Yellowjacket stings before being robbed — the drones are stolen Vespid units but the paralytic compound is custom-made.",
      "A Yellowjacket loaded with a lethal compound instead of paralytic was deployed in a corporate office — the target died at their desk and the drone returned to its operator before anyone noticed."
    ]
  },
  {
    name: "Street Custom 'Sunday School' Weaponized Religious Artifact",
    aliases: ["Sunday School", "Holy Weapon", "Blessed Hardware", "The Sermon"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A broad category of improvised weapons concealed within or fabricated from religious objects — crucifixes with hidden blades, prayer beads strung with ball bearings for use as a flail, hollowed scripture books containing pistols, incense burners filled with chemical agents, and ceremonial staffs weighted with lead cores. Sunday School weapons have proliferated in Meridian 88's religious communities where faith and survival intersect. The weapons exploit a cultural reluctance to search religious items at checkpoints and social gatherings, turning devotional objects into a delivery system for violence. The practice is controversial within the communities that produce it — some see it as sacrilege, others as the ultimate expression of a faith tested by circumstance.",
    specifications: "type: Variable — bladed crucifixes, weighted prayer beads, gun-books, chemical incense\nconcealment: Within culturally protected religious objects\neffectiveness: Variable by specific build\ndetection: Low — cultural reluctance to search religious items\ncost: Φ20-500 depending on complexity\nconstruction: Ranges from crude modifications to skilled craft work",
    tier_availability: "Tier 1+",
    legality: "Prohibited — concealed weapon",
    street_price: "Φ20-500",
    base_technologies: ["Concealment within cultural artifacts", "Improvised weapon integration"],
    story_hooks: [
      "A religious leader was assassinated with a blade concealed in a ceremonial cross — the weapon was handed to them by a congregant during a ritual and the killing was committed in front of hundreds.",
      "A checkpoint security team has begun searching religious items, provoking a community uprising over cultural desecration — but they keep finding weapons, validating the searches they should not be performing."
    ]
  },
  {
    name: "Arcturus Defense Solutions Kinetic Impactor Round KIR-10 'Hammerfall'",
    aliases: ["Hammerfall", "KIR-10", "Heavy Round", "Thor Shot"],
    category: "rifle",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A heavy-caliber anti-materiel round designed for the Arcturus AM-10 platform that uses a two-stage propulsion system — conventional propellant for initial acceleration followed by a secondary rocket motor that ignites after 50 meters, boosting the 20mm tungsten penetrator to a terminal velocity that exceeds any conventional rifle round. The KIR-10 was designed to defeat next-generation reactive armor systems that use explosive countermeasures to deflect standard anti-materiel rounds — the rocket-boosted second stage arrives too fast for reactive systems to respond. Against personnel, the round is grotesquely excessive, but its use against augmented targets with military-grade armor implants has been documented.",
    specifications: "caliber: 20mm tungsten penetrator\nstage 1: Conventional propellant, 600 m/s\nstage 2: Rocket motor ignition at 50m, terminal velocity 1,800 m/s\neffective range: 100-1,500 meters\nreactive armor defeat: Arrives before countermeasure activation\nmagazine: 5 rounds in AM-10 platform\nround weight: 180g\nrifle weight: 14 kg\npropellant: Dual-stage — chemical + solid rocket",
    tier_availability: "Tier 5",
    legality: "Military restricted — anti-materiel authorization",
    street_price: "Φ65,000 rifle, Φ1,200 per round",
    base_technologies: ["Dual-stage propulsion projectile engineering", "Rocket-boosted anti-materiel design", "Reactive armor defeat ballistics"],
    story_hooks: [
      "A KIR-10 round was fired through the engine block of an armored convoy vehicle, punching through both sides — the shooter was over a kilometer away and the reactive armor never triggered.",
      "Arcturus is field testing KIR-10 rounds with a third stage — a terminal guidance package that allows the round to adjust course in the final 100 meters of flight."
    ]
  },
  {
    name: "Tessera Industries Void Grenade VG-1 'Black Hole'",
    aliases: ["Black Hole", "VG-1", "Void Bomb", "The Absence"],
    category: "exotic",
    manufacturer: "TESSERA INDUSTRIES",
    description: "An experimental ordnance device that creates a 2-meter sphere of spatial compression lasting 0.5 seconds — everything within the sphere is subjected to extreme compressive forces as space itself contracts, crushing matter into a fraction of its original volume before the effect collapses and space snaps back to normal. The result is a sphere of mangled, compressed debris where intact objects used to be. Tessera's physicists describe it as a 'controlled spatial anomaly' and freely admit they do not fully understand the long-term effects on local spacetime. The Void Grenade is the single most classified item in Tessera's research division, and its existence has been confirmed only by the distinctive spherical compression signatures found at two classified incident sites.",
    specifications: "effect radius: 2-meter sphere\neffect duration: 0.5 seconds\ncompression force: Estimated 50,000 atmospheres within sphere\ndetonation: 3-second timer after activation\nweight: 0.5 kg\nform factor: Standard grenade size\nprototypes: Estimated 5 units manufactured\nside effects: Unknown long-term spacetime distortion\nclassification: Tessera Ultra-Restricted",
    tier_availability: "Tier 5",
    legality: "Does not officially exist",
    street_price: "Priceless — no market exists",
    base_technologies: ["Controlled spatial compression generation", "Localized spacetime manipulation", "Experimental physics weaponization"],
    story_hooks: [
      "A crime scene contains a perfect 2-meter sphere of crushed, compressed matter — vehicles, building material, and three human bodies reduced to a dense ball. Nothing in any weapons database explains it.",
      "A Tessera researcher involved in the VG-1 program has disappeared after sending encrypted messages claiming the spatial compression events are not contained — each detonation is leaving a permanent microscopic weakness in spacetime that could accumulate."
    ]
  },
  {
    name: "Street Custom 'Lazybones' Tripwire Shotgun Trap",
    aliases: ["Lazybones", "Door Blaster", "Welcome Mat", "Hello Goodbye"],
    category: "improvised",
    manufacturer: "Street Custom",
    description: "A sawed-off shotgun fixed to a stationary mount with its trigger connected to a tripwire or door-opening mechanism, aimed at a kill zone. The Lazybones is the simplest possible automated weapon — a gun pointed at where someone will be, rigged to fire when they arrive. The design requires no electronics, no sensors, and no maintenance — it will wait indefinitely until the wire is pulled. They are deployed in doorways, stairwells, crawlspaces, and any chokepoint where the first person through is guaranteed to be in the line of fire. The weapons are indiscriminate by nature and have killed more unintended targets — children, emergency workers, scavengers — than intended ones.",
    specifications: "firearm: Sawed-off shotgun, typically 12-gauge\ntrigger: Monofilament tripwire or door mechanism linkage\nfiring arc: Fixed — wherever the gun is pointed\neffective range: 2-8 meters\nsetup time: 10-20 minutes\nweight: 3-4 kg including mount\npower source: None — purely mechanical\nindiscriminate: Yes — fires on any trigger activation",
    tier_availability: "Tier 1+",
    legality: "Prohibited — indiscriminate booby trap",
    street_price: "Φ30-80 for mechanism, plus firearm",
    base_technologies: ["Mechanical tripwire trigger systems", "Fixed-position weapon mounting"],
    story_hooks: [
      "An emergency response team lost a member to a Lazybones trap while responding to a medical call — the building had been abandoned for months but the trap was fresh, suggesting someone set it specifically to kill first responders.",
      "A Tier 1 building has been systematically trapped with dozens of Lazybones devices, turning it into a fortress that nobody can enter without triggering multiple shotgun blasts — the occupant inside has not been seen in weeks."
    ]
  },
  {
    name: "Sterling-Nakamura Holographic Decoy Projector HDP-3 'Mirage'",
    aliases: ["Mirage", "HDP-3", "Holo Decoy", "Ghost Maker"],
    category: "exotic",
    manufacturer: "STERLING-NAKAMURA",
    description: "While not a weapon itself, the HDP-3 is classified as tactical ordnance because of its role in combat operations. The device projects a convincing holographic image of a human figure — complete with simulated thermal signature, acoustic footstep generation, and movement patterns derived from motion-capture libraries — that can draw fire, trigger automated defenses, and deceive security systems. The hologram is projected from a small emitter that can be thrown, placed, or drone-delivered, and the projected figure walks, runs, or takes cover according to pre-programmed behavioral scripts. Against human observers, the deception fails at distances under 5 meters. Against automated systems, it is convincing at any range that the sensors can detect it.",
    specifications: "projector size: 8 cm diameter disc\nprojection range: Up to 20 meters from emitter\nhologram fidelity: Convincing beyond 5 meters to human observers\nthermal simulation: Embedded IR emitter mimics body heat\nacoustic simulation: Speaker generates footstep and movement sounds\nbehavioral scripts: Walk, run, take cover, fire weapon (visual only)\nbattery life: 10 minutes of projection\nweight: 0.15 kg\ndeployment: Hand-thrown, placed, or drone-delivered",
    tier_availability: "Tier 3+",
    legality: "Licensed — military and authorized security",
    street_price: "Φ9,000 per projector",
    base_technologies: ["Volumetric holographic projection", "Thermal signature simulation", "Behavioral movement scripting"],
    story_hooks: [
      "A witness swears they saw a person flee a crime scene, but security footage shows a holographic projection from an HDP-3 emitter found on the roof — someone used a decoy to create a false witness sighting.",
      "An HDP-3 was used to trigger a SAT-2 autonomous turret into expending its ammunition on a hologram, allowing the real attackers to walk through the now-empty defense perimeter."
    ]
  }
];

// Generate filenames
function toFileName(name) {
  return name
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

let written = 0;
let skipped = 0;
for (const w of weapons) {
  const fname = toFileName(w.name);
  const fpath = path.join(outDir, fname);
  if (fs.existsSync(fpath)) {
    skipped++;
    continue;
  }
  fs.writeFileSync(fpath, JSON.stringify(w, null, 2) + '\n');
  written++;
}

console.log(`Weapons: wrote ${written}, skipped ${skipped} (already existed)`);
console.log(`Total weapon files now: ${fs.readdirSync(outDir).length}`);
