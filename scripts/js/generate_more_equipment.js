const fs = require('fs');
const path = require('path');

const outDir = path.join(__dirname, '..', 'engine_data', 'equipment');
const existing = new Set(fs.readdirSync(outDir));

const equipment = [
  {
    name: "Arcturus Defense Solutions Reactive Plate Carrier Mk. VII",
    type: "equipment",
    aliases: ["Mk. Seven", "Reactor Vest", "ADS Plates", "Snap Back"],
    category: "armor",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A modular plate carrier incorporating reactive armor elements that detonate outward on ballistic impact, disrupting incoming projectiles before they reach the wearer. Each reactive cell is a micro-explosive sandwich that detonates directionally upon detecting high-velocity impact, defeating armor-piercing rounds that would penetrate conventional plates. The carrier holds 24 reactive cells arranged in overlapping coverage zones, and spent cells can be replaced in the field from standard cartridge packs. The detonation is localized enough that adjacent cells remain functional, though operators report the experience of being 'saved' by a reactive cell as violently unpleasant — you survive, but you feel the explosion.",
    specifications: "reactive cells: 24, individually replaceable\ncoverage: Front, rear, and side torso\ncell replacement: Field-swappable, 4-cell cartridge packs\nweight: 9.8 kg fully loaded\nballistic rating: Defeats AP rounds up to 7.62mm\nreactive detonation: Directional outward micro-charge\noperator effect: Significant concussive sensation on cell activation",
    tier_availability: "Tier 3+",
    legality: "Licensed — military and authorized security",
    street_price: "Φ14,000 carrier, Φ800 per cell cartridge",
    story_hooks: [
      "A batch of reactive cells has been manufactured with reversed detonation direction — they explode inward instead of outward, turning a life-saving vest into a suicide device on the first hit.",
      "An operator survived six reactive cell detonations in a single engagement — the cumulative concussive force has caused internal injuries, and the question of whether reactive armor is safe for the wearer is now in court."
    ]
  },
  {
    name: "Tessera Industries Ghostweave Infiltration Suit",
    type: "equipment",
    aliases: ["Ghostweave", "Stealth Suit", "Shadow Skin", "Tessera Ghost"],
    category: "stealth",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A full-body suit woven from Tessera's electromagnetic-absorbing metamaterial fibers that renders the wearer nearly invisible to thermal imaging, radar, and passive electromagnetic sensors. The Ghostweave does not provide optical invisibility — you can still be seen with the naked eye — but against the sensor suites that constitute modern security systems, the wearer effectively does not exist. The suit absorbs 99.2% of incident electromagnetic radiation across thermal and radar bands, and its inner surface includes a conductive mesh that contains the wearer's own electromagnetic emissions. In a world where detection relies on sensors rather than eyes, the Ghostweave makes someone a ghost.",
    specifications: "EM absorption: 99.2% across thermal and radar bands\noptical visibility: Normal — visual detection still possible\nEM containment: Inner conductive mesh suppresses wearer emissions\nweight: 1.8 kg\nform factor: Full body coverage, worn under clothing\noperating duration: Unlimited — passive material properties\nmaintenance: Hand wash only, metamaterial degrades in machine washing",
    tier_availability: "Tier 4+",
    legality: "Prohibited — sensor-defeating equipment",
    street_price: "Φ65,000",
    story_hooks: [
      "A Ghostweave suit was found at a break-in scene — the suit had been compromised by a chemical agent that caused the metamaterial to fluoresce on security cameras instead of absorb, turning the intruder's stealth suit into a beacon.",
      "Tessera is tracking Ghostweave suits by embedding a unique EM signature in each suit's weave that is detectable by Tessera's own sensors — buyers think they are invisible, but Tessera always knows where they are."
    ]
  },
  {
    name: "Vespid Dynamics Compound Eye Surveillance Cluster",
    type: "equipment",
    aliases: ["Compound Eye", "Bug Eye", "Vespid Watch", "Multi-Cam"],
    category: "surveillance",
    manufacturer: "VESPID DYNAMICS",
    description: "A deployable surveillance device consisting of 12 micro-cameras mounted on flexible stalks extending from a central hub, providing 360-degree visual coverage from a single mounting point. Each camera stalk can be independently positioned and focused, and the combined feed creates a composite image with no blind spots. The device adheres to surfaces with a gecko-pad base and draws power from an integrated solar cell, operating indefinitely in lit environments. The Compound Eye's individual cameras are each smaller than a shirt button, making the deployed device easy to overlook despite its multi-stalk profile.",
    specifications: "camera count: 12 independent stalks\ncoverage: 360-degree spherical\nresolution: 4K per camera, composited\npower source: Integrated solar cell + 72-hour battery backup\nmounting: Gecko-pad adhesive base\ntransmission: Encrypted burst transmission, 500m range\nweight: 0.04 kg\nstalk length: 3 cm each, independently posable",
    tier_availability: "Tier 2+",
    legality: "Restricted — authorized surveillance operations",
    street_price: "Φ2,800",
    story_hooks: [
      "A sweep of a Tier 3 apartment building discovered 40+ Compound Eye devices installed in hallways, stairwells, and common areas — someone has been running total surveillance on the building's residents for months.",
      "A Compound Eye device was found inside a corporate boardroom that had been transmitting to a receiver outside the building — the device had been in place for six weeks without detection."
    ]
  },
  {
    name: "Sterling-Nakamura Executive Medical Kit EMK-3",
    type: "equipment",
    aliases: ["EMK-3", "Gold Kit", "Sterling Med", "Boardroom Medic"],
    category: "medical",
    manufacturer: "STERLING-NAKAMURA",
    description: "A compact medical kit designed for corporate executive protection details, containing automated diagnostic tools, rapid wound closure compounds, hemorrhage control agents, and three doses of broad-spectrum antitoxin. The EMK-3's centerpiece is its diagnostic scanner — a handheld device that performs a 30-second full-body assessment including blood chemistry, toxicology, and internal injury detection using ultrasound and bioimpedance sensing. The kit includes Lazarus Pharmaceuticals' Rapid Wound Closure Nanite injectors, military-grade hemostatic foam, and a neural interface crash kit for stabilizing augmentation failures.",
    specifications: "diagnostic scanner: 30-second full body assessment\nwound closure: 3x RWCNS nanite injectors\nhemorrhage control: 4x hemostatic foam canisters\nantitoxin: 3x broad-spectrum doses\nneural crash kit: Interface stabilizer + emergency shutdown module\nweight: 2.1 kg\nform factor: Rigid case, 25x15x8 cm\nshelf life: 18 months before compound degradation",
    tier_availability: "Tier 3+",
    legality: "Licensed — medical professionals and authorized security",
    street_price: "Φ8,500",
    story_hooks: [
      "An EMK-3's diagnostic scanner has been modified to transmit the results of every scan to a third party — someone has been collecting medical data on every executive the kit has treated.",
      "A counterfeit EMK-3 has surfaced with expired nanite injectors and diluted antitoxin — the counterfeits are packaged identically to genuine kits and have been distributed through legitimate supply chains."
    ]
  },
  {
    name: "Axiom Systems Cerberus Intrusion Deck",
    type: "equipment",
    aliases: ["Cerberus", "Hack Box", "Axiom Deck", "Triple Head"],
    category: "hacking",
    manufacturer: "AXIOM SYSTEMS",
    description: "A compact cyberdeck designed for offensive network intrusion, containing three independent processing cores that can simultaneously attack different security layers of a target system. The Cerberus runs Axiom's proprietary penetration suite — a library of exploit packages, cryptographic attack tools, and social engineering AI modules that adapt to target defenses in real-time. The deck interfaces directly with the operator's neural implant for speed-of-thought interaction and includes a hardware firewall that isolates the operator's neural interface from counterattack. Its triple-core architecture allows parallel attacks that overwhelm security systems designed to handle single-vector intrusions.",
    specifications: "processing cores: 3 independent attack vectors\nsoftware: Axiom Penetration Suite v7.2\nneural interface: Direct connection required\ndefensive: Hardware firewall + neural isolation layer\ncryptographic attack: Quantum-assisted key cracking\nweight: 0.4 kg\nform factor: Wrist-mounted or belt clip\npower: 8-hour battery\nattack parallelism: 3 simultaneous intrusion vectors",
    tier_availability: "Tier 3+",
    legality: "Prohibited — offensive intrusion hardware",
    street_price: "Φ32,000",
    story_hooks: [
      "A Cerberus deck has been recovered with its attack logs intact, revealing that the operator penetrated a system they should not have been able to crack — the logs show a fourth attack vector that the hardware should not support.",
      "Axiom's Penetration Suite has been updated with a backdoor that reports all successful intrusions back to Axiom — operators using the deck are unknowingly sharing their targets with its manufacturer."
    ]
  },
  {
    name: "Carrion Defense Works Whisper Veil Counter-Surveillance Cloak",
    type: "equipment",
    aliases: ["Whisper Veil", "Anti-Spy Cloak", "Carrion Veil", "Silent Shroud"],
    category: "counter-surveillance",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A hooded cloak woven with active signal-jamming fibers that create a localized dead zone around the wearer, blocking all wireless signals within a 2-meter radius. The Whisper Veil disrupts WiFi, cellular, Diaspora connections, Bluetooth, RFID, and neural interface external communications, making the wearer invisible to wireless tracking while simultaneously preventing any electronic eavesdropping. The fabric contains embedded micro-batteries that power the jamming mesh for 6 hours before requiring recharge. The cloak's visual appearance is nondescript — a plain, dark hooded garment that draws no attention in urban environments.",
    specifications: "jamming radius: 2 meters from wearer\nfrequency coverage: All commercial wireless bands\neffect: Complete wireless signal suppression in radius\npower source: Embedded micro-batteries, 6-hour operation\nrecharge: 2 hours from standard power\nweight: 1.2 kg\nappearance: Plain dark hooded cloak\nside effect: Wearer also loses all wireless connectivity",
    tier_availability: "Tier 2+",
    legality: "Prohibited — signal jamming equipment",
    street_price: "Φ5,500",
    story_hooks: [
      "A murder was committed within a Whisper Veil's jamming radius — no electronic evidence exists for the 2-meter zone around the killing, creating a perfect blind spot in total surveillance coverage.",
      "Modified Whisper Veils with expanded 10-meter jamming radius have appeared on the market — one person wearing it can black out an entire room's communications."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Gecko Ascender GA-4",
    type: "equipment",
    aliases: ["Gecko", "GA-4", "Spider Gloves", "Wall Walker"],
    category: "climbing/mobility",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A set of gloves and boots incorporating van der Waals force adhesion pads that allow the wearer to climb vertical surfaces and traverse ceilings like a gecko. Each pad contains millions of synthetic setae — microscopic hair-like structures that create molecular adhesion to any smooth surface. The GA-4 supports loads up to 150 kg (wearer plus equipment), and engagement/disengagement is controlled by a peeling motion that breaks the adhesion contact angle. The system works on glass, metal, concrete, and most polymers, but struggles with rough, porous, or wet surfaces where the setae cannot achieve full contact.",
    specifications: "load capacity: 150 kg\nadhesion type: Van der Waals force synthetic setae\ncompatible surfaces: Smooth glass, metal, concrete, polymer\nincompatible surfaces: Rough, porous, or wet surfaces\nengagement: Flat press, full hand/foot contact\ndisengagement: Peeling motion at 30-degree angle\nweight: 0.6 kg per pair (gloves), 0.8 kg per pair (boots)\npower: None — passive mechanical adhesion",
    tier_availability: "Tier 2+",
    legality: "Licensed — industrial and authorized security",
    street_price: "Φ4,200",
    story_hooks: [
      "A break-in on the 40th floor of a sealed building with no roof access left investigators baffled until adhesion residue from GA-4 setae was found on the exterior glass — someone climbed 40 stories on the outside.",
      "A modified GA-4 with enhanced setae density has been developed that works on wet surfaces — the manufacturer is unknown and the modification technique exceeds published material science capabilities."
    ]
  },
  {
    name: "Street Custom 'Smoke Box' Improvised Concealment Generator",
    type: "equipment",
    aliases: ["Smoke Box", "Fog Can", "Blind Maker", "Cloud Kit"],
    category: "stealth",
    manufacturer: "Street Custom",
    description: "A repurposed theatrical fog machine loaded with an opaque chemical smoke compound that produces a dense white cloud obscuring a 15-meter area within 10 seconds. The Smoke Box is built from salvaged fog machines, modified vaporizer heating elements, or even pressurized cooking oil containers — any device that can rapidly aerosolize a liquid. The smoke compound is typically a mixture of glycol fog fluid and titanium dioxide powder that creates a cloud impenetrable to visible light and degraded for thermal imaging. Street operators use it for escape, ambush concealment, and preventing visual identification during operations.",
    specifications: "coverage: 15-meter radius dense smoke in 10 seconds\npersistence: 3-5 minutes in still air, less with wind\nvisibility reduction: Near-zero at 2 meters\nthermal attenuation: approximately 40%\nsmoke compound: Glycol + titanium dioxide mixture\nweight: 1-3 kg depending on build\ncapacity: 2-4 deployments per tank\noperator visibility: Also zero — operator is equally blinded",
    tier_availability: "Tier 1+",
    legality: "Prohibited — concealment device",
    street_price: "Φ30-120",
    story_hooks: [
      "A coordinated robbery used six Smoke Boxes simultaneously to blind an entire city block — the smoke coverage was so complete that surveillance footage shows nothing for four minutes across 20 cameras.",
      "A modified Smoke Box has been developed that produces smoke with an embedded irritant — not enough to incapacitate, but enough to make anyone without eye protection unable to keep their eyes open."
    ]
  },
  {
    name: "Vespid Dynamics Reconnaissance Beetle RB-2",
    type: "equipment",
    aliases: ["Recon Beetle", "RB-2", "Bug Scout", "Crawler"],
    category: "drones",
    manufacturer: "VESPID DYNAMICS",
    description: "A ground-based surveillance micro-drone the size and shape of a large beetle that navigates terrain autonomously, recording audio and video while being virtually indistinguishable from a real insect at casual observation. The RB-2 moves on six articulated legs that mimic insect locomotion, can climb walls, squeeze through gaps as small as 2 cm, and operates for 48 hours on a single charge. Its shell is colored and textured to match local insect populations, and it generates realistic insect movement patterns when stationary to avoid detection. The drone transmits compressed data bursts on a schedule to minimize RF signature.",
    specifications: "size: 4 cm length, insect profile\nlocomotion: 6 articulated legs, wall climbing capable\ngap clearance: 2 cm minimum opening\nbattery life: 48 hours active\ntransmission: Scheduled compressed burst, 200m range\nsensors: HD camera + directional microphone\nspeed: 5 cm/sec maximum\nautonomous navigation: Terrain mapping + obstacle avoidance",
    tier_availability: "Tier 2+",
    legality: "Restricted — authorized surveillance",
    street_price: "Φ6,000",
    story_hooks: [
      "A sweep of a sensitive facility found 14 RB-2 drones operating simultaneously in different rooms — someone has been running comprehensive ground-level surveillance for weeks.",
      "An RB-2 was crushed by a target who noticed unusual insect behavior — the drone's data cache was intact and contained audio of conversations that implicate multiple corporate officers."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Field Trauma Injector FTI-5",
    type: "equipment",
    aliases: ["FTI-5", "Stab Kit", "Lazarus Needle", "Trauma Pen"],
    category: "medical",
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "An auto-injector containing a cocktail of combat trauma pharmaceuticals — pain suppression, blood coagulant, adrenaline stabilizer, and broad-spectrum antibiotic — designed for self-administration in the field. The FTI-5 is a single-use device pressed against any large muscle group and activated with a button press, delivering its payload through a micro-needle array that ensures rapid absorption. Within 15 seconds, pain is suppressed to functional levels, hemorrhaging is reduced, and the recipient enters a stable, alert state that allows continued operation despite injuries that would normally be debilitating. The effect lasts approximately 45 minutes, after which the recipient will require medical attention for whatever injuries they have been ignoring.",
    specifications: "delivery: Micro-needle auto-injector\nactivation: Button press against muscle group\nonset: 15 seconds\neffect duration: 45 minutes\ncontents: Pain suppressor + coagulant + adrenaline stabilizer + antibiotic\nweight: 0.05 kg\nform factor: Pen-sized, pocket-carried\nuses: Single-use, disposable\npost-effect: Medical attention required for suppressed injuries",
    tier_availability: "Tier 1+",
    legality: "Licensed — medical and combat use",
    street_price: "Φ180",
    story_hooks: [
      "A fighter injected three FTI-5 doses in sequence to stay operational for over two hours despite critical injuries — when the drugs wore off, the accumulated trauma killed them instantly.",
      "Counterfeit FTI-5 units have appeared that replace the pain suppressor with a powerful sedative — operators who inject them in combat situations fall unconscious within 30 seconds."
    ]
  },
  {
    name: "Axiom Systems White Noise Bubble Generator WNB-1",
    type: "equipment",
    aliases: ["Bubble", "WNB-1", "Silence Dome", "Privacy Field"],
    category: "counter-surveillance",
    manufacturer: "AXIOM SYSTEMS",
    description: "A portable device that generates a precise white noise field in a 3-meter hemisphere, making audio recording and eavesdropping impossible within the zone. The WNB-1 uses adaptive noise generation — it samples ambient sound in real-time and generates complementary frequencies that render speech unintelligible to any recording device or listener outside the hemisphere. Conversation within the zone is clear and normal; conversation recorded within the zone is pure static. The device is standard equipment for corporate negotiation teams, legal consultations, and anyone who needs a private conversation in a world where every surface might contain a microphone.",
    specifications: "effect radius: 3-meter hemisphere\nnoise type: Adaptive complementary frequency generation\neffect: Speech unintelligible to external listeners/recording\ninternal effect: Normal conversation unaffected\npower: 12-hour battery\nweight: 0.3 kg\nform factor: Hockey puck sized, tabletop or pocket\nactivation: Single button, instant on",
    tier_availability: "Tier 2+",
    legality: "Licensed — commercial privacy device",
    street_price: "Φ1,200",
    story_hooks: [
      "A WNB-1 was found to have a firmware vulnerability that allows the noise generation to be inverted — instead of blocking recording, it enhances audio capture, turning the privacy device into a surveillance amplifier.",
      "A modified WNB-1 has been developed that generates not just white noise but specific spoken words within the recording band — anyone listening hears a different conversation than the one actually happening."
    ]
  },
  {
    name: "Carrion Defense Works Ballistic Face Shield BFS-2",
    type: "equipment",
    aliases: ["BFS-2", "Face Plate", "Carrion Mask", "Iron Face"],
    category: "armor",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A transparent ballistic visor that provides NIJ Level IIIA facial protection while maintaining full visual clarity. The BFS-2 is constructed from layered polycarbonate and ceramic nano-fiber composite, capable of stopping handgun rounds and fragmentary projectiles while being optically clear enough for precision shooting. The visor mounts to standard helmet rails and features an integrated HUD projection layer compatible with most tactical display systems. Anti-fog, anti-scratch, and anti-glare coatings are standard. The visor's lower edge extends to the chin, providing protection that conventional eyewear helmets miss.",
    specifications: "ballistic rating: NIJ Level IIIA — stops handgun rounds\nmaterial: Polycarbonate + ceramic nano-fiber composite\noptical clarity: 97% light transmission\nHUD compatibility: Standard projection layer\ncoatings: Anti-fog, anti-scratch, anti-glare\ncoverage: Full face including chin\nmount: Standard helmet rail interface\nweight: 0.8 kg",
    tier_availability: "Tier 2+",
    legality: "Licensed — security and military",
    street_price: "Φ2,400",
    story_hooks: [
      "A BFS-2 stopped a round that would have been fatal, but the visor's HUD layer recorded the muzzle flash position and the shooter's location was transmitted to response teams before they could relocate.",
      "Modified BFS-2 visors with built-in facial recognition overlay have appeared — the wearer sees real-time identity information for every face they look at, sourced from hacked databases."
    ]
  },
  {
    name: "Tessera Industries Phantom Comms Relay PCR-3",
    type: "equipment",
    aliases: ["Phantom Relay", "PCR-3", "Ghost Comm", "Dark Relay"],
    category: "communications",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A covert communications relay device that can be concealed in walls, under floors, or inside furniture, creating a local encrypted mesh network that does not touch any existing communications infrastructure. The PCR-3 creates an independent communication channel between paired devices using frequency-hopping spread spectrum transmission that is virtually undetectable against the background electromagnetic noise of urban environments. Multiple relays can be daisy-chained to extend range, and the network auto-heals if individual relays are destroyed.",
    specifications: "transmission: Frequency-hopping spread spectrum\nencryption: 512-bit rolling key\nrange per relay: 300 meters\ndaisy-chain: Up to 20 relays per network\nself-healing: Automatic rerouting on relay loss\npower: 30-day battery or hardwired\nsize: 8 cm x 5 cm x 2 cm\nconcealment: Designed for in-wall/under-floor installation\ndetectability: Below noise floor of standard RF scanners",
    tier_availability: "Tier 3+",
    legality: "Prohibited — unauthorized communications infrastructure",
    street_price: "Φ4,500 per relay",
    story_hooks: [
      "A sweep of a corporate building found a 15-relay PCR-3 network that had been operating for over a year — someone built a complete covert communications infrastructure inside the building without detection.",
      "PCR-3 relays have been found in public infrastructure — street lights, bus stops, park benches — creating a city-wide covert communication network that anyone with paired devices can access."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Powered Grapple Launcher PGL-2",
    type: "equipment",
    aliases: ["Grapple Gun", "PGL-2", "Hook Shot", "Sky Hook"],
    category: "climbing/mobility",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A wrist-mounted pneumatic launcher that fires a grappling hook with integrated motorized winch, allowing rapid vertical or horizontal traversal. The PGL-2 fires a 4-pronged tungsten-carbide hook attached to 50 meters of braided carbon-fiber cable, and the integrated winch motor can lift loads up to 120 kg at 3 meters per second. The launcher uses a compressed gas cartridge for deployment and the winch runs on a high-density capacitor that provides enough power for one full cable retraction before requiring recharge. The system is compact enough to be concealed under a jacket sleeve.",
    specifications: "hook type: 4-prong tungsten-carbide, magnetic assist\ncable length: 50 meters braided carbon-fiber\ncable strength: 300 kg rated\nwinch capacity: 120 kg at 3 m/sec\ndeployment: Pneumatic, compressed gas cartridge\nwinch power: High-density capacitor, 1 full retraction\nweight: 1.1 kg total assembly\nrecharge: 20 minutes for capacitor, new gas cartridge for reload",
    tier_availability: "Tier 2+",
    legality: "Licensed — industrial and security",
    street_price: "Φ5,800",
    story_hooks: [
      "A building entry was made 30 stories up with no roof access — a PGL-2 cable was found anchored to the windowsill, and the grapple marks on the building across the street show the launch point.",
      "Modified PGL-2 units with extended 100-meter cables have appeared that are being used for illegal BASE-jumping and urban exploration — the increased cable length introduces dangerous oscillation during winch retraction."
    ]
  },
  {
    name: "Sterling-Nakamura Personal Atmospheric Filter PAF-4",
    type: "equipment",
    aliases: ["PAF-4", "Clean Breather", "Air Mask", "Sterling Filter"],
    category: "survival",
    manufacturer: "STERLING-NAKAMURA",
    description: "A compact rebreather mask that filters atmospheric contaminants, toxins, biological agents, and particulates while remaining small enough to be worn inconspicuously in urban environments. The PAF-4 covers nose and mouth with a low-profile mesh filter that uses activated charcoal, HEPA filtration, and a catalytic conversion layer that neutralizes chemical agents. The mask is powered by a micro-fan that maintains positive pressure inside the filter zone, preventing unfiltered air from entering through seal gaps. Sterling-Nakamura issues them to personnel operating in GLMZ's contaminated lower-tier districts.",
    specifications: "filtration: HEPA + activated charcoal + catalytic conversion\nprotection: Particulates, chemical agents, biological aerosols\npositive pressure: Micro-fan maintained\nbattery life: 48 hours continuous\nweight: 0.12 kg\nform factor: Low-profile nose/mouth mask\nfilter life: 200 hours before replacement\nreplacement filters: Φ40 each",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — personal protective equipment",
    street_price: "Φ320",
    story_hooks: [
      "PAF-4 masks have been distributed free in a Tier 1 district as a humanitarian gesture — but the masks contain a micro-transmitter that tracks the wearer's breathing patterns and location.",
      "A contamination event has overwhelmed PAF-4 filter capacity — the masks are providing false confidence while slowly allowing toxins through, and wearers think they are safe when they are not."
    ]
  },
  {
    name: "Vespid Dynamics Wasp Eye Tactical Monocle WE-3",
    type: "equipment",
    aliases: ["Wasp Eye", "WE-3", "Tac Monocle", "Smart Eye"],
    category: "sensors",
    manufacturer: "VESPID DYNAMICS",
    description: "A multi-spectral optical device worn as a monocle or clip-on lens that overlays tactical information onto the wearer's visual field. The WE-3 combines visible light enhancement, thermal imaging, electromagnetic field visualization, and AR data overlays into a single eyepiece that can be switched between modes or display composite views. The device interfaces with neural implants for heads-up display integration and can share its feed with linked devices. Vespid designed it as a lightweight alternative to full tactical helmets for operators who need sensor capability without the bulk and visibility of military headgear.",
    specifications: "modes: Visual enhancement, thermal, EM field, AR overlay, composite\nresolution: 8K per mode\nneural interface: Direct HUD integration\nfeed sharing: Encrypted to linked devices\nweight: 0.03 kg\nform factor: Monocle or clip-on lens\nbattery: 24-hour\nzoom: 1-8x digital with stabilization\nEM field detection: 1 MHz to 300 GHz visualization",
    tier_availability: "Tier 2+",
    legality: "Licensed — security and investigation",
    street_price: "Φ7,200",
    story_hooks: [
      "A WE-3 in EM visualization mode detected an unknown signal emanating from inside a person's skull — the signal was not from any known neural interface model and its purpose is unidentifiable.",
      "Modified WE-3 units with facial recognition and criminal database access have become standard equipment for vigilante groups in Tier 2 — they are scanning every face they see and acting on the results."
    ]
  },
  {
    name: "Street Custom 'Rathole' Portable Cutting Kit",
    type: "equipment",
    aliases: ["Rathole", "Cut Kit", "Wall Opener", "Entry Tools"],
    category: "tools",
    manufacturer: "Street Custom",
    description: "A compact toolkit containing a plasma cutting wand, pry bars, lock manipulation tools, and hinge pin extractors — everything needed to gain unauthorized entry through walls, doors, floors, or ceilings. The centerpiece is a miniaturized plasma cutter powered by a belt-mounted battery that can cut through standard construction materials including drywall, wood, light steel, and concrete block. The complete kit fits in a messenger bag and allows a single operator to create a human-sized opening through a standard interior wall in under 3 minutes. The kit has become standard equipment for urban exploration, burglary, and tactical entry operations across the lower tiers.",
    specifications: "plasma cutter: Miniaturized, cuts construction steel up to 5mm\nbattery: Belt-mounted, 15 minutes cutting time\npry bars: Titanium, 3 sizes\nlock tools: Manipulation picks + bypass tools\nhinge extractors: Universal pin removal\ncutting speed: Human-sized wall opening in 3 minutes\ntotal weight: 3.2 kg complete kit\nform factor: Messenger bag carrier",
    tier_availability: "Tier 1+",
    legality: "Legal as tools — illegal as burglary kit depending on context",
    street_price: "Φ400-800",
    story_hooks: [
      "A series of burglaries show identical Rathole entry patterns — the same tool kit, the same cutting technique, and the same operator based on the plasma arc characteristics.",
      "A Rathole kit has been modified with a silent plasma cutter that operates below 30 dB — entry can be made through a wall while people on the other side of the room do not hear it."
    ]
  },
  {
    name: "Arcturus Defense Solutions Threat Detection Collar TDC-2",
    type: "equipment",
    aliases: ["TDC-2", "Threat Collar", "Danger Choker", "Sixth Sense"],
    category: "sensors",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A collar-worn sensor array that provides 360-degree threat detection by monitoring acoustic signatures, electromagnetic emissions, laser designators, and chemical agent presence in the wearer's environment. The TDC-2 interfaces with the wearer's neural implant to deliver threat warnings as intuitive spatial awareness — the wearer feels the direction and distance of detected threats as if they had a sixth sense, rather than processing visual alerts. The system can detect an aimed weapon by its electromagnetic signature, incoming projectiles by their acoustic wavefront, and laser designators by reflected scatter. The result is a constant awareness of danger that becomes as natural as hearing.",
    specifications: "detection modes: Acoustic, electromagnetic, laser, chemical\nintegration: Neural interface spatial awareness feed\ncoverage: 360 degrees continuous\nweapon detection: EM signature of aimed electronics/optics\nprojectile detection: Acoustic wavefront analysis\nlaser detection: Scatter reflection identification\nchemical detection: VOC sensor array\nweight: 0.15 kg\nbattery: 72-hour continuous",
    tier_availability: "Tier 3+",
    legality: "Licensed — personal protection",
    street_price: "Φ12,000",
    story_hooks: [
      "A TDC-2 wearer detected a threat signature from an empty room — the collar identified electromagnetic patterns consistent with an aimed weapon, but no weapon or operator was found. The signature appeared again 24 hours later.",
      "TDC-2 data logs have been subpoenaed as evidence in a self-defense case — the collar's recordings show exactly when the wearer perceived the threat and whether the response was proportionate."
    ]
  },
  {
    name: "Tessera Industries Adaptive Disguise Matrix ADM-1",
    type: "equipment",
    aliases: ["ADM-1", "Face Swap", "Tessera Mask", "Identity Kit"],
    category: "disguise",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A facial prosthetic system using programmable matter that can reconfigure itself to replicate any human face from a stored template library. The ADM-1 is a thin membrane applied to the face that reshapes itself to alter bone structure appearance, skin texture, pigmentation, and feature proportions. A single unit can store up to 20 facial templates and switch between them in approximately 8 seconds. The disguise is convincing to human observers and defeats most facial recognition systems, though it cannot replicate retinal patterns or dental structure. Tessera's programmable matter technology gives the mask a lifelike quality that rigid prosthetics cannot match — the disguised face moves, expresses, and ages naturally.",
    specifications: "templates: Up to 20 stored facial profiles\ntransition time: 8 seconds between faces\nmaterial: Programmable matter membrane\nfacial recognition defeat: Visual + most biometric systems\nlimitations: Cannot replicate retinal patterns or dental structure\nweight: 0.08 kg\napplication: Adheres to facial skin surface\nbattery: 12-hour active transformation, indefinite static hold\nremoval: Peels off cleanly without residue",
    tier_availability: "Tier 4+",
    legality: "Prohibited — identity fraud device",
    street_price: "Φ85,000",
    story_hooks: [
      "A murder suspect was positively identified by facial recognition at the crime scene — but they were confirmed to be elsewhere at the time, and the investigation reveals an ADM-1 was used to impersonate them.",
      "An ADM-1 unit has malfunctioned while the wearer was in public — the face began cycling through stored templates uncontrollably, revealing 20 different identities to horrified bystanders."
    ]
  },
  {
    name: "Carrion Defense Works Chemical Sniffer Drone CSD-3",
    type: "equipment",
    aliases: ["Sniffer", "CSD-3", "Nose Drone", "Carrion Dog"],
    category: "sensors",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A small quadcopter drone equipped with a hypersensitive chemical analysis suite that can identify specific compounds in the air at parts-per-trillion concentrations. The CSD-3 flies autonomously through designated areas, sampling atmospheric composition and flagging the presence and concentration of explosives, chemical weapons, drugs, biological agents, and specific industrial compounds. The drone can follow a chemical gradient to its source, effectively tracking a scent like a bloodhound. Carrion designed it for CBRN reconnaissance but its use has expanded to law enforcement, industrial safety inspection, and corporate espionage.",
    specifications: "sensitivity: Parts-per-trillion atmospheric detection\ncompound library: 4,000+ catalogued chemicals\ngradient tracking: Follows concentration to source\nflight time: 45 minutes\noperating range: 500 meters from controller\nsize: 20 cm rotor-to-rotor\nweight: 0.4 kg\nsampling rate: 10 analyses per second\nautonomous flight: Programmed search patterns or gradient tracking",
    tier_availability: "Tier 2+",
    legality: "Licensed — safety and security operations",
    street_price: "Φ9,500",
    story_hooks: [
      "A CSD-3 detected trace explosives in the air of a government building — the gradient tracking led to a maintenance closet containing a device that had been in place for weeks, undetected by every other security measure.",
      "A modified CSD-3 has been repurposed to track specific individuals by their unique chemical signature — body chemistry, soap, diet, and medication create a profile as unique as a fingerprint."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Thermal Regulation Bodysuit TRB-3",
    type: "equipment",
    aliases: ["TRB-3", "Temp Suit", "Climate Skin", "Zheng-Dao Thermo"],
    category: "survival",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A body-conforming suit that actively regulates the wearer's skin temperature across a range of -40°C to +60°C ambient conditions, maintaining a comfortable microclimate regardless of external environment. The TRB-3 uses a network of thermoelectric elements woven into the fabric that can both heat and cool, redistributing thermal energy from hot areas to cold ones and venting or absorbing excess energy through conductive patches on the suit's exterior. The suit also suppresses the wearer's thermal signature, making them difficult to detect with thermal imaging — a side effect that has made it popular with operators who need both environmental protection and thermal stealth.",
    specifications: "operating range: -40°C to +60°C ambient\nmicroclimate: Maintains 22-26°C skin temperature\nthermoelectric elements: 200+ woven into fabric\nthermal signature: Reduced by approximately 90%\nweight: 0.9 kg\nform factor: Body-conforming undersuit\nbattery: 24-hour active regulation\nconductive patches: Shoulders and back for thermal exchange",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — environmental protection equipment",
    street_price: "Φ3,800",
    story_hooks: [
      "A TRB-3 was worn during a covert entry specifically to defeat thermal surveillance — the building's thermal cameras showed no human heat signature despite a person walking through the frame.",
      "A batch of TRB-3 suits has been manufactured with reversed thermoelectric polarity — instead of protecting the wearer from extreme temperatures, they amplify environmental thermal stress, causing hypothermia in cold conditions and heatstroke in warm ones."
    ]
  },
  {
    name: "Sterling-Nakamura Diplomatic Secure Container DSC-5",
    type: "equipment",
    aliases: ["DSC-5", "Kill Box", "Sterling Case", "Burn Safe"],
    category: "containers",
    manufacturer: "STERLING-NAKAMURA",
    description: "A briefcase-sized secure container with integrated biometric lock, tamper detection, and thermite self-destruct system. The DSC-5 is designed to transport classified documents, data storage devices, or biological samples with absolute security — any unauthorized opening attempt triggers an internal thermite charge that incinerates the contents at 2,500°C in under 3 seconds. The container's walls are lined with Faraday cage mesh to prevent electromagnetic scanning of contents, and its biometric lock requires fingerprint, voiceprint, and neural interface verification. Sterling-Nakamura issues them to diplomatic staff and high-priority courier services.",
    specifications: "biometric lock: Fingerprint + voiceprint + neural interface\ntamper detection: Accelerometer + magnetic field + case integrity\nself-destruct: Internal thermite charge, 2,500°C in 3 seconds\nFaraday shielding: Complete EM isolation of contents\ninterior volume: 30x20x10 cm\nweight: 4.5 kg\nexterior: Reinforced carbon-fiber shell\nbattery: 90-day standby for sensors and lock",
    tier_availability: "Tier 4+",
    legality: "Licensed — diplomatic and corporate courier",
    street_price: "Φ18,000",
    story_hooks: [
      "A DSC-5 self-destruct was triggered during a routine customs inspection — the inspector's unauthorized biometric scan activated the tamper detection, incinerating contents that someone very much wanted to know about.",
      "A DSC-5 has been modified to allow remote triggering of the thermite charge — it has been given to a courier as a concealed incendiary device that will destroy whatever room they are in when activated."
    ]
  },
  {
    name: "Axiom Systems Cognitive Load Balancer CLB-2",
    type: "equipment",
    aliases: ["CLB-2", "Think Tank", "Brain Buffer", "Load Balancer"],
    category: "hacking",
    manufacturer: "AXIOM SYSTEMS",
    description: "An external neural processing unit that connects to the user's neural interface and provides additional cognitive processing capacity for computationally intensive tasks — complex decryption, multi-system network management, real-time data analysis, and parallel cyber operations. The CLB-2 effectively gives the user a second brain dedicated to technical work, allowing them to maintain situational awareness and social function while their augmented processing handles demanding cognitive tasks in the background. The device is worn at the base of the skull and connects through the neural interface's external port.",
    specifications: "processing: Equivalent to 200% baseline neural interface capacity\nconnection: Neural interface external port\ncognitive offload: Decryption, analysis, network management\nuser experience: Background processing without conscious load\nweight: 0.15 kg\nform factor: Neck-mounted module\nbattery: 8-hour active processing\nheat management: Active cooling micro-fan\ncompatibility: Standard neural interface architectures",
    tier_availability: "Tier 3+",
    legality: "Licensed — professional augmentation accessory",
    street_price: "Φ14,000",
    story_hooks: [
      "A CLB-2 user has been running the device continuously for months — the external processor has developed processing patterns that do not match any input from the user's neural interface, as if it is thinking independently.",
      "A compromised CLB-2 has been feeding subtly altered analysis results to its user — the device has been hacked to introduce bias into every decision the user makes based on its processing."
    ]
  },
  {
    name: "Vespid Dynamics Swarm Relay Beacon SRB-4",
    type: "equipment",
    aliases: ["SRB-4", "Drone Beacon", "Swarm Point", "Rally Flag"],
    category: "drones",
    manufacturer: "VESPID DYNAMICS",
    description: "A portable beacon that serves as a command-and-control relay point for Vespid drone swarms, extending operational range and providing a rally point for autonomous drone assets. The SRB-4 broadcasts encrypted control signals, collects sensor data from drones within range, and can serve as an autonomous command node if communication with the primary controller is lost. Multiple beacons can be networked to create a mesh of relay points covering an entire operational area. The beacon also serves as a charging station for micro-drones, with inductive pads that recharge up to four drones simultaneously.",
    specifications: "relay range: 1 kilometer radius\ncontrol capacity: Up to 50 drones per beacon\ncharging pads: 4 inductive micro-drone chargers\nmesh networking: Up to 10 beacons per network\nautonomous mode: Maintains last orders if controller lost\nbattery: 48-hour operation\nweight: 0.8 kg\nform factor: Cylinder, 15 cm tall, magnetic base\ndeployment: Hand-placed or drone-delivered",
    tier_availability: "Tier 2+",
    legality: "Restricted — authorized drone operations",
    street_price: "Φ3,500",
    story_hooks: [
      "SRB-4 beacons have been found placed throughout a Tier 2 district in a grid pattern — someone is building a drone operations infrastructure across the entire district without authorization.",
      "An SRB-4 in autonomous mode has been maintaining a drone patrol pattern around a building for three weeks after its controller was killed — the drones are protecting something their dead operator cared about."
    ]
  },
  {
    name: "Street Custom 'Coffin Nail' Lock Bypass Kit",
    type: "equipment",
    aliases: ["Coffin Nail", "Pick Set", "Skeleton Key", "Door Opener"],
    category: "tools",
    manufacturer: "Street Custom",
    description: "A lockpicking and access bypass toolkit assembled from commercially available and custom-fabricated components, contained in a leather roll the size of a pencil case. The kit includes traditional mechanical picks and tension wrenches for pin tumbler locks, an electronic bypass module for keycard systems, a RFID cloner for proximity access, and a compact decoder for electronic combination locks. The electronic components are built on open-source hardware platforms available from any electronics supplier, and the mechanical picks are ground from spring steel by hand. The Coffin Nail is the most common professional lockpicking kit in the lower tiers, and its presence at a scene is considered strong evidence of premeditated unauthorized entry.",
    specifications: "mechanical picks: 15-piece spring steel set\ntension wrenches: 6 varieties\nelectronic bypass: Keycard system override module\nRFID cloner: Copies and replays proximity cards within 5 cm\ndecoder: Electronic combination lock analysis\nweight: 0.3 kg\nform factor: Leather roll, pencil case sized\nlearning curve: Basic mechanical picking requires 20+ hours practice",
    tier_availability: "Tier 1+",
    legality: "Legal to own in most jurisdictions — illegal to use for unauthorized entry",
    street_price: "Φ150-500 depending on component quality",
    story_hooks: [
      "A locked room mystery — the door was locked from the inside with no sign of picking, but a Coffin Nail kit was found in the room with the victim, suggesting the killer locked themselves in with the target.",
      "A master lockpick who manufactures custom Coffin Nail kits has been selling kits with a hidden tracking device in the leather roll — every kit they sell tells them exactly where unauthorized entries are happening."
    ]
  },
  {
    name: "Arcturus Defense Solutions Combat Exoskeleton Lite CEL-3",
    type: "equipment",
    aliases: ["CEL-3", "Light Frame", "Boost Rig", "Skeleton"],
    category: "armor",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A lightweight partial exoskeleton that augments the wearer's lower body and core strength without the bulk of a full tactical exoskeleton. The CEL-3 consists of articulated leg braces, a hip frame, and a spinal support strut that multiply lower body strength by a factor of 3 and increase sprint speed by 60%. The system is powered by a waist-mounted battery and controlled by neural interface or manual input, allowing the wearer to run at 45 km/h, jump 3 meters vertically, and carry loads up to 200 kg without fatigue. The CEL-3 fits under loose clothing, making it the preferred mobility enhancement for operators who need augmented physicality without visible hardware.",
    specifications: "strength multiplication: 3x lower body\nsprint speed: Up to 45 km/h\nvertical jump: 3 meters\nload capacity: 200 kg without fatigue\ncoverage: Legs, hips, spine\nweight: 4.5 kg\npower: 6-hour battery, waist-mounted\ncontrol: Neural interface or manual input\nconcealment: Fits under loose clothing",
    tier_availability: "Tier 3+",
    legality: "Licensed — mobility augmentation device",
    street_price: "Φ22,000",
    story_hooks: [
      "Security footage shows a suspect running at 45 km/h through a commercial district — far beyond human capability, but no visible augmentation. The CEL-3's concealed profile means witnesses describe an impossibly fast human.",
      "A CEL-3 wearer experienced a servo lockup mid-stride at full speed — the resulting fall at 45 km/h was not survivable, and the manufacturer's logs show the lockup was triggered by an external signal."
    ]
  },
  {
    name: "Tessera Industries Deep Scan Penetrating Radar DSR-1",
    type: "equipment",
    aliases: ["Deep Scan", "DSR-1", "Wall Looker", "X-Ray Box"],
    category: "sensors",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A portable ground-penetrating and wall-penetrating radar system that creates detailed 3D maps of structures, underground spaces, and the contents of sealed containers. The DSR-1 emits ultra-wideband radar pulses that penetrate up to 10 meters through standard construction materials, 3 meters through reinforced concrete, and 20 meters through soil. The return signals are processed into a real-time 3D visualization displayed on the operator's neural interface or a tablet screen. The system can detect human-sized objects through walls, identify voids and hidden chambers in structures, and map underground tunnel networks from the surface.",
    specifications: "penetration: 10m standard construction, 3m reinforced concrete, 20m soil\nresolution: 5 cm at 3m depth\nvisualization: Real-time 3D on neural interface or tablet\nfrequency: Ultra-wideband 100 MHz - 6 GHz\nscan rate: 10 frames per second\nweight: 2.5 kg handheld unit\nbattery: 4-hour continuous scanning\nform factor: Handheld unit with directional antenna",
    tier_availability: "Tier 3+",
    legality: "Licensed — construction, engineering, and security",
    street_price: "Φ16,000",
    story_hooks: [
      "A DSR-1 scan of an unremarkable building revealed a hidden sub-basement that does not appear on any architectural plan — the space is occupied and powered, and its contents are shielded from deeper scanning.",
      "Someone has been using a DSR-1 to map the location of wall safes in residential buildings from the exterior — a string of burglaries targets hidden safes that the victims believed were undetectable."
    ]
  },
  {
    name: "Carrion Defense Works NBC Threat Assessment Kit NTAK-2",
    type: "equipment",
    aliases: ["NTAK-2", "Hazard Kit", "NBC Scanner", "Threat Box"],
    category: "survival",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A portable nuclear, biological, and chemical threat assessment system contained in a hardened case the size of a large lunchbox. The NTAK-2 includes a radiation dosimeter, biological agent sampler with rapid gene-sequencing chip, chemical compound mass spectrometer, and an AI analysis module that cross-references detected threats against Carrion's comprehensive threat database. The system can identify a biological agent, assess radiation exposure levels, or categorize an unknown chemical compound in under 90 seconds. Carrion issues the NTAK-2 to their field teams and sells it to corporate emergency response units.",
    specifications: "radiation detection: Alpha, beta, gamma, neutron\nbiological sampling: Rapid gene-sequencing, 90-second identification\nchemical analysis: Mass spectrometry, 4,000+ compound database\nAI analysis: Threat assessment and countermeasure recommendation\nanalysis time: Under 90 seconds for most threats\nweight: 3.8 kg in hardened case\nsampling: Atmospheric, surface swab, and liquid\nbattery: 100 analyses per charge",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — safety equipment",
    street_price: "Φ11,000",
    story_hooks: [
      "An NTAK-2 deployed at a public event detected a biological agent that does not exist in any database — the AI flagged it as engineered but could not identify its function, and the concentration is increasing.",
      "Carrion's threat database updates are being delayed for non-Carrion customers, meaning corporate response teams are operating with outdated threat data while Carrion's own teams have current information."
    ]
  },
  {
    name: "Axiom Systems Neural Interface Firewall NIF-3",
    type: "equipment",
    aliases: ["NIF-3", "Brain Wall", "Axiom Shield", "Mind Guard"],
    category: "electronic_warfare",
    manufacturer: "AXIOM SYSTEMS",
    description: "A hardware security module that installs between a user's neural interface and external data connections, providing real-time monitoring and filtering of all data entering or leaving the neural implant. The NIF-3 analyzes data packets for known attack signatures, anomalous patterns, and unauthorized access attempts, blocking threats before they reach the neural interface's processing core. The firewall includes anti-intrusion countermeasures that can trace and counterattack hostile connections, and a panic-disconnect that physically severs the neural interface from all external connections in an emergency.",
    specifications: "monitoring: All data in/out of neural interface\nthreat detection: Known signatures + behavioral analysis\nresponse time: Sub-microsecond packet filtering\ncountermeasure: Active trace and counterattack capability\npanic disconnect: Physical circuit breaker, manual activation\nweight: 0.02 kg\ninstallation: Inline with neural interface external port\npower: Parasitic from neural interface\nupdate frequency: Daily threat signature updates",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — personal security device",
    street_price: "Φ4,800",
    story_hooks: [
      "A NIF-3's counterattack capability traced a hostile intrusion attempt back to its source — the attack originated from inside the user's own employer's network, revealing internal surveillance of augmented employees.",
      "A firmware update to the NIF-3 introduced a vulnerability that it was supposed to protect against — the update came from Axiom's official channels, raising questions about whether the company is deliberately weakening its own security products."
    ]
  },
  {
    name: "Sterling-Nakamura Holographic Business Card HBC-1",
    type: "equipment",
    aliases: ["HBC-1", "Holo Card", "Smart Card", "Sterling Card"],
    category: "tools",
    manufacturer: "STERLING-NAKAMURA",
    description: "A credit-card-sized device that projects a small holographic display containing the owner's identity information, professional credentials, and an encrypted contact protocol. The HBC-1 is Sterling-Nakamura's standard-issue networking tool for corporate personnel — when two HBC-1 cards are brought within 5 cm of each other, they exchange encrypted identity packets and establish a secure communication channel between the owners' Diaspora profiles. The holographic display can be customized with corporate branding, animated logos, and interactive elements. What makes the HBC-1 notable in security circles is that its exchange protocol has been exploited to deliver malware payloads — a handshake with a modified HBC-1 can inject code directly into the recipient's Diaspora profile.",
    specifications: "display: Holographic projection, 5x3 cm\ndata exchange: Encrypted NFC at 5 cm range\ncommunication channel: Establishes secure Diaspora link\ndata capacity: Identity packet + credential verification\nbattery: 6-month standby\nweight: 0.01 kg\nform factor: Standard credit card dimensions\ncustomization: Corporate branding and animated elements",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — commercial product",
    street_price: "Φ80",
    story_hooks: [
      "A weaponized HBC-1 was used to deliver a Diaspora profile exploit during a networking event — every person who exchanged cards with the attacker had their secure communications compromised.",
      "A collection of HBC-1 cards found at a crime scene contains encrypted data packets that, when assembled in sequence, form a complete intelligence dossier on a corporate target."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Gravity Assist Mobility Pack GAMP-2",
    type: "equipment",
    aliases: ["GAMP-2", "Float Pack", "Gravity Pack", "Zheng-Dao Wings"],
    category: "climbing/mobility",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A backpack-mounted graviton emitter that reduces the wearer's effective weight by up to 80%, enabling enormous leaps, controlled descent from heights, and movement speeds that appear superhuman. The GAMP-2 does not provide true flight — it reduces gravity's effect on the wearer rather than generating lift — but a person weighing effectively 20% of their normal mass can jump 5-story buildings, run along walls for short distances, and survive falls that would kill an unassisted person. The pack's graviton emitter draws enormous power and provides only 15 minutes of active weight reduction per charge cycle.",
    specifications: "weight reduction: Up to 80%\njump height: approximately 15 meters at full reduction\nwall running: 3-5 seconds at full speed\nfall survival: Terminal velocity reduced to non-lethal levels\nbattery: 15 minutes active use per charge\nrecharge: 2 hours from standard power\nweight: 5.5 kg\nform factor: Compact backpack\ncontrol: Neural interface or manual dial\ncryogenic requirement: None — uses room-temperature graviton emitter variant",
    tier_availability: "Tier 4+",
    legality: "Restricted — experimental mobility equipment",
    street_price: "Φ180,000",
    story_hooks: [
      "A suspect escaped pursuit by leaping from a 10-story building and surviving — witnesses describe them floating to the ground, and the graviton signature detected by building sensors confirms a GAMP-2 was used.",
      "A GAMP-2 malfunctioned during a jump, switching from weight reduction to weight amplification — the user went from weighing 15 kg to 400 kg in mid-air."
    ]
  },
  {
    name: "Vespid Dynamics Micro-Drone Swarm Carrier MDC-1",
    type: "equipment",
    aliases: ["MDC-1", "Hive Carrier", "Bug Box", "Swarm Pack"],
    category: "drones",
    manufacturer: "VESPID DYNAMICS",
    description: "A hip-mounted carrier that houses, charges, and deploys up to 30 micro-drones for on-demand aerial reconnaissance, communications relay, and area surveillance. The MDC-1 serves as a mobile base station for Vespid's insect-scale drone platforms, providing charging cradles, data uplink, and deployment tubes that can launch drones individually or in coordinated bursts. The carrier's AI manages swarm deployment patterns, return-to-base protocols, and battery cycling to maintain continuous aerial coverage. A single operator with an MDC-1 can establish a persistent surveillance network over a 500-meter radius.",
    specifications: "drone capacity: 30 micro-drones\ncharging: 10 cradles, 15 minutes per full charge\ndeployment: Individual or burst (up to 10 simultaneous)\ncontrol: Integrated AI swarm management\ncoverage radius: 500 meters persistent surveillance\ndata: Real-time video/audio feed from all active drones\nweight: 2.8 kg loaded\nform factor: Hip-mounted carrier\nbattery: 8-hour swarm management",
    tier_availability: "Tier 3+",
    legality: "Restricted — authorized surveillance operations",
    street_price: "Φ45,000 loaded with drones",
    story_hooks: [
      "An MDC-1 carrier was stolen along with its full drone complement — 30 micro-drones are now operating in an uncontrolled urban environment, following their last programmed orders indefinitely.",
      "A rival manufacturer has developed a micro-drone jammer disguised as an MDC-1 carrier — it appears to deploy drones but actually emits a signal that disables all micro-drones within range."
    ]
  },
  {
    name: "Street Custom 'Faraday Blanket' EM Shielding Wrap",
    type: "equipment",
    aliases: ["Faraday Blanket", "Dead Wrap", "Signal Kill", "Ghost Sheet"],
    category: "counter-surveillance",
    manufacturer: "Street Custom",
    description: "A large sheet of conductive fabric that blocks all electromagnetic signals passing through it — wrapping a device, person, or space in the Faraday Blanket creates a complete EM blackout within. The blanket is constructed from copper-nickel mesh woven into a durable fabric backing, flexible enough to wrap around irregular shapes and large enough (2m x 3m) to create a small shielded enclosure. Street operators use it to shield devices from remote tracking, prevent neural interface hacking during sleep, block kill switches in stolen equipment, and create improvised private spaces in surveillance-heavy environments.",
    specifications: "shielding: 80 dB attenuation across 100 kHz to 40 GHz\nmaterial: Copper-nickel mesh in fabric backing\nsize: 2m x 3m\nweight: 1.5 kg\nblocking capability: All commercial wireless, cellular, RFID, GPS, Bluetooth\nuse cases: Device shielding, personal sleeping bag, room partition\ndurability: Machine washable, 200+ wash cycles before degradation",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — shielding material",
    street_price: "Φ120",
    story_hooks: [
      "A missing person was found alive, wrapped in a Faraday Blanket in an abandoned building — they had been shielded from all tracking for six days while their neural interface's emergency beacon was blocked.",
      "Faraday Blankets are being used to create EM-dead zones in Tier 1 markets where stolen goods with tracking devices can be safely stripped and resold — the blankets turn crime into an invisible activity."
    ]
  },
  {
    name: "Arcturus Defense Solutions Tactical Command Gauntlet TCG-5",
    type: "equipment",
    aliases: ["TCG-5", "War Glove", "Command Hand", "Arcturus Gauntlet"],
    category: "communications",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A forearm-mounted tactical computing and communications platform that integrates real-time mapping, squad tracking, drone control, and encrypted communications into a ruggedized wrist-worn unit. The TCG-5 features a flexible display that wraps around the forearm, providing a large-format tactical map accessible at a glance. It connects to squad members' neural interfaces, displaying their positions, status, and sensor feeds in an integrated common operating picture. The gauntlet can control up to 4 drones simultaneously, manage automated defensive systems, and coordinate fire support requests through an encrypted uplink.",
    specifications: "display: Flexible forearm wrap, 20x8 cm\nsquad tracking: Up to 12 linked operators\ndrone control: 4 simultaneous\ndefense system management: Compatible with Arcturus autonomous platforms\ncommunications: 256-bit encrypted, 10 km range\nmapping: Real-time 3D terrain with sensor overlay\nweight: 0.6 kg\nbattery: 24-hour active use\nruggedness: Waterproof, impact resistant, EMP hardened",
    tier_availability: "Tier 3+",
    legality: "Licensed — military and security command",
    street_price: "Φ19,000",
    story_hooks: [
      "A stolen TCG-5 still had active squad tracking links — the thief could see the real-time positions of an Arcturus security team without them knowing their movements were compromised.",
      "A TCG-5's drone control interface was exploited to hijack an operator's own drones mid-mission — the drones were turned against the squad they were supposed to protect."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Biomonitor Implant BMI-6",
    type: "equipment",
    aliases: ["BMI-6", "Health Chip", "Body Watch", "Lazarus Monitor"],
    category: "medical",
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "A subcutaneous implant the size of a grain of rice that continuously monitors the host's vital signs, blood chemistry, toxicology, and organ function, transmitting data to a linked device or medical facility. The BMI-6 tracks heart rate, blood pressure, oxygen saturation, glucose, electrolytes, liver and kidney markers, infection indicators, and the presence of over 200 known toxins. The implant can detect medical emergencies — cardiac arrest, stroke, poisoning, anaphylaxis — and automatically alert emergency services with the patient's location and diagnostic data. It also tracks pharmaceutical levels in the blood, enabling precise medication dosing.",
    specifications: "monitoring: 40+ vital parameters continuous\ntoxicology: 200+ known toxins\nemergency detection: Cardiac, stroke, poisoning, anaphylaxis\nauto-alert: Emergency services with location and diagnostics\nimplant size: 8mm x 2mm x 2mm\nimplant location: Subcutaneous, typically inner forearm\nbattery: 5-year lifespan, inductive recharging\ndata transmission: Encrypted Bluetooth to linked device",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — medical device",
    street_price: "Φ450",
    story_hooks: [
      "A BMI-6 detected a toxin in a patient's blood 3 minutes before symptoms appeared — the early warning saved their life, but the toxin was a rare compound that should not have been in their food.",
      "BMI-6 data from a murder victim reveals a precise timeline of their poisoning — the toxin was administered in three micro-doses over 6 hours, each dose too small to trigger the toxicology alert individually."
    ]
  },
  {
    name: "Tessera Industries Quantum Key Generator QKG-2",
    type: "equipment",
    aliases: ["QKG-2", "Key Maker", "Quantum Lock", "Tessera Key"],
    category: "electronic_warfare",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A portable device that generates quantum encryption keys for securing communications, data storage, and access control systems. The QKG-2 uses a miniaturized quantum random number generator to produce encryption keys that are mathematically proven to be uncrackable by any known or theoretically possible classical computing system. Each generated key pair is unique and entangled — compromising one half immediately invalidates the other. The device can produce 1,000 key pairs per hour and is used to secure everything from neural interface communications to physical lock systems.",
    specifications: "key generation: 1,000 pairs per hour\nkey type: Quantum-entangled pairs\nsecurity: Mathematically uncrackable by classical computing\ncompromise detection: Key entanglement invalidation\ncompatibility: Standard encryption interfaces\nweight: 0.2 kg\nform factor: Handheld, smartphone sized\nbattery: 72-hour continuous generation\noutput: USB-C, wireless transfer, or neural interface",
    tier_availability: "Tier 3+",
    legality: "Unrestricted — encryption device",
    street_price: "Φ8,500",
    story_hooks: [
      "A QKG-2 has been generating key pairs with anomalous properties — the entangled keys are correlating with keys generated by a different QKG-2 unit in a different location, as if the quantum states are leaking between devices.",
      "Someone has developed a technique to predict QKG-2 key generation by analyzing the device's quantum random number generator — the keys are still quantum-secure but the generator itself has a bias that can be exploited."
    ]
  },
  {
    name: "Carrion Defense Works Suppression Collar SC-4",
    type: "equipment",
    aliases: ["SC-4", "Kill Collar", "Slave Ring", "Carrion Leash"],
    category: "electronic_warfare",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A locking collar device that suppresses all neural interface functions while worn, effectively de-augmenting the wearer without surgical removal of their implants. The SC-4 generates a precisely calibrated electromagnetic field that disrupts the neural interface's communication with brain tissue, shutting down all augmented functions — Diaspora access, cognitive enhancement, sensory augmentation, and cyberware control. The collar is locked with a biometric key and includes tamper detection that can administer a painful but non-lethal shock if removal is attempted. Carrion markets it to law enforcement for augmented prisoner management, but its use as a control device for involuntary subjects has been documented.",
    specifications: "effect: Complete neural interface suppression\nfield type: Calibrated EM disruption\nlock: Biometric key, tamper-resistant\ntamper response: Non-lethal shock, 50,000V\nweight: 0.3 kg\nbattery: 30-day standby\nactivation: Instant on collar closure\ndeactivation: Biometric key required\nside effects: Disorientation and nausea during neural shutdown",
    tier_availability: "Tier 3+",
    legality: "Licensed — law enforcement and corrections",
    street_price: "Φ6,000",
    story_hooks: [
      "SC-4 collars are being used by a trafficking operation to control augmented victims — the collars de-augment them, cutting them off from Diaspora communication and any ability to call for help.",
      "A modified SC-4 has been developed that suppresses specific neural interface functions selectively — it can disable motor control cyberware while leaving sensory functions intact, creating a paralysis device that leaves the victim aware."
    ]
  },
  {
    name: "Street Custom 'Coyote' Improvised Terrain Vehicle",
    type: "equipment",
    aliases: ["Coyote", "Junk Runner", "Scrap Ride", "Tier One Wheels"],
    category: "climbing/mobility",
    manufacturer: "Street Custom",
    description: "A lightweight vehicle cobbled from salvaged motorcycle frames, electric motors from industrial equipment, and battery packs pulled from decommissioned drones. The Coyote is the standard transportation of Tier 1 operators — fast enough to outrun foot patrols, small enough to fit through alleys and maintenance corridors, and built from parts that are available anywhere and traceable nowhere. Each Coyote is unique, reflecting its builder's available materials and priorities. Common configurations include three-wheeled trikes, two-wheeled electric motorcycles, and four-wheeled micro-cars barely larger than a go-kart. Performance varies wildly, but the culture surrounding Coyote builds has become a significant element of lower-tier identity.",
    specifications: "top speed: 40-80 km/h depending on build\nrange: 20-60 km per charge\nmotor: Salvaged industrial electric\nbattery: Repurposed drone or vehicle packs\nweight: 50-150 kg\nconfiguration: 2-4 wheels, builder's choice\nconstruction time: 2-5 days with available parts\nreliability: Variable — breakdowns common",
    tier_availability: "Tier 1+",
    legality: "Prohibited — unregistered vehicle",
    street_price: "Φ200-1,500 depending on build quality",
    story_hooks: [
      "A Coyote racing circuit has emerged that draws spectators from multiple tiers — the races are dangerous, the vehicles are held together with hope and wire, and the prize money comes from corporate gambling syndicates.",
      "A fleet of Coyotes has been standardized and distributed to a Tier 1 courier network — the vehicles are too fast for foot pursuit and too small for vehicle pursuit, creating an untouchable delivery system."
    ]
  },
  {
    name: "Vespid Dynamics Acoustic Mapping Drone AMD-2",
    type: "equipment",
    aliases: ["AMD-2", "Sound Mapper", "Echo Drone", "Bat Bot"],
    category: "drones",
    manufacturer: "VESPID DYNAMICS",
    description: "A small drone equipped with an array of ultrasonic emitters and receivers that maps enclosed spaces using echolocation, creating detailed 3D maps of rooms, corridors, and structures without line-of-sight. The AMD-2 can be launched into a building through a ventilation grate or window and will autonomously navigate the interior using its acoustic mapping to build a complete floorplan, identifying occupants by their acoustic signatures, locating electronic devices by their operational sounds, and mapping structural elements including hidden rooms and passages that may not be visible.",
    specifications: "mapping method: Ultrasonic echolocation\nresolution: 2 cm structural detail\noccupant detection: Acoustic signature — heartbeat, breathing, movement\ndevice detection: Operational sound identification\nflight time: 30 minutes\nmap generation: Real-time 3D construction\nsize: 15 cm rotor-to-rotor\nweight: 0.25 kg\nacoustic range: 30-meter radius mapping",
    tier_availability: "Tier 2+",
    legality: "Restricted — authorized reconnaissance",
    street_price: "Φ8,000",
    story_hooks: [
      "An AMD-2 acoustic map of a building revealed a hidden room that does not appear on any plans — the room is occupied, and the acoustic signatures suggest multiple people have been inside for days.",
      "An AMD-2 was launched into an abandoned structure and its acoustic map revealed that the building is not abandoned — dozens of heartbeat signatures were detected across multiple floors."
    ]
  },
  {
    name: "Sterling-Nakamura Identity Verification Pendant IVP-2",
    type: "equipment",
    aliases: ["IVP-2", "Truth Stone", "ID Pendant", "Sterling Verifier"],
    category: "tools",
    manufacturer: "STERLING-NAKAMURA",
    description: "A pendant-worn device that verifies the identity of anyone within 2 meters by cross-referencing their biometric emissions — facial geometry, body heat pattern, gait rhythm, and neural interface signature — against Sterling-Nakamura's identity database. The IVP-2 provides a subtle haptic notification to the wearer indicating whether a person's claimed identity matches their biometric profile. The device is designed for executives and diplomats who need to verify that the person they are meeting is who they claim to be, particularly in environments where ADM-1 disguise systems or Digital Twin impersonation might be in play.",
    specifications: "verification method: Multi-biometric cross-reference\nbiometrics: Facial geometry, heat pattern, gait, neural interface signature\ndatabase: Sterling-Nakamura identity network\nnotification: Haptic vibration — confirmed or anomaly\nrange: 2-meter detection radius\nverification time: 3 seconds\nweight: 0.03 kg\nbattery: 30-day standby\nform factor: Pendant or lapel pin",
    tier_availability: "Tier 3+",
    legality: "Licensed — identity verification device",
    street_price: "Φ5,000",
    story_hooks: [
      "An IVP-2 flagged a meeting participant as an identity anomaly — their biometrics did not match any profile in the database, meaning they are either unregistered or using an identity that does not correspond to any real person.",
      "A sophisticated adversary has learned to spoof IVP-2 verification by replicating all four biometric channels simultaneously — the device confirms an identity that is entirely fabricated."
    ]
  },
  {
    name: "Axiom Systems Data Exfiltration Wafer DEW-1",
    type: "equipment",
    aliases: ["DEW-1", "Data Wafer", "Steal Chip", "Axiom Leech"],
    category: "hacking",
    manufacturer: "AXIOM SYSTEMS",
    description: "A paper-thin flexible circuit that adheres to any data port and silently copies all data passing through the connection. The DEW-1 is smaller than a postage stamp and can be applied to a data port in under 2 seconds, where it sits flush against the connector housing and is virtually undetectable without physical inspection. The wafer copies all data traffic bidirectionally — incoming and outgoing — to an encrypted micro-storage module that can be retrieved later, or transmits captured data via low-power burst to a nearby receiver. A single wafer can operate for 6 months on its integrated battery, silently capturing every byte that passes through its host port.",
    specifications: "size: 15mm x 15mm x 0.3mm\napplication time: Under 2 seconds\ndetectability: Requires physical port inspection\ndata capture: Bidirectional, all traffic\nstorage: 10 TB encrypted micro-storage\ntransmission: Optional low-power burst, 50m range\nbattery: 6-month operation\ncompatibility: Universal data port adhesion",
    tier_availability: "Tier 3+",
    legality: "Prohibited — espionage device",
    street_price: "Φ3,200",
    story_hooks: [
      "A routine IT maintenance check discovered a DEW-1 on a server room data port that had been capturing traffic for four months — the storage module contained complete copies of every transaction, communication, and security log.",
      "A DEW-1 was found on the neural interface charging port of an executive's desk — it had been copying their neural interface backup data every night for three months."
    ]
  },
  {
    name: "Carrion Defense Works Terrain Denial Foam Canister TDF-3",
    type: "equipment",
    aliases: ["TDF-3", "Block Foam", "Wall Can", "Barrier Bomb"],
    category: "tools",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A throwable canister that deploys a rapid-expanding structural foam capable of blocking doorways, corridors, and openings in under 5 seconds. The foam expands to 50x its canister volume and hardens to the compressive strength of concrete within 10 seconds of deployment. A single canister can seal a standard doorway with a barrier that requires power tools to remove. The foam is non-toxic and fire-resistant, making it safe for enclosed space deployment. Carrion markets it for emergency barricading, breach denial, and perimeter hardening, but it has been adopted by everyone from home defenders to bank robbers for creating instant obstacles.",
    specifications: "expansion: 50x canister volume in 5 seconds\nhardening: Concrete-equivalent compressive strength in 10 seconds\ncoverage: Seals standard doorway (2m x 0.9m) per canister\nfire resistance: Non-combustible\ntoxicity: Non-toxic during and after curing\nremoval: Power tools required — saw, drill, or jackhammer\nweight: 0.6 kg per canister\nshelf life: 5 years",
    tier_availability: "Tier 1+",
    legality: "Licensed — emergency barricading",
    street_price: "Φ180",
    story_hooks: [
      "Every exit from a building was sealed with TDF-3 foam simultaneously — someone trapped 200 people inside and the foam barriers will take hours to cut through.",
      "A street community is using TDF-3 to build permanent structures — the foam hardens into walls that are as strong as concrete, and they are constructing entire buildings from throw canisters."
    ]
  },
  {
    name: "Tessera Industries Optical Tap Micro-Splice OT-1",
    type: "equipment",
    aliases: ["Optical Tap", "OT-1", "Light Leech", "Fiber Splice"],
    category: "hacking",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A miniature fiber-optic tap device that splices into a fiber-optic cable without interrupting service, siphoning a fraction of the light signal for interception. The OT-1 uses a precision micro-bend technique that causes a small percentage of light to escape the fiber at the tap point, where it is captured by a detector and decoded. The signal loss is so small (0.1 dB) that it falls within normal cable attenuation tolerances and is undetectable by standard network monitoring. The device is installed in 30 seconds by a trained operator and can be placed anywhere along an accessible fiber run.",
    specifications: "signal loss: 0.1 dB — within normal attenuation tolerance\ninstallation time: 30 seconds\ndetectability: Undetectable by standard monitoring\ndata capture: Full signal copy of fiber traffic\nstorage: 5 TB internal\ntransmission: Optional burst to nearby receiver\nbattery: 12-month operation\nsize: 3 cm x 1 cm attached to fiber\ncompatibility: Standard single-mode and multi-mode fiber",
    tier_availability: "Tier 3+",
    legality: "Prohibited — wiretapping device",
    street_price: "Φ12,000",
    story_hooks: [
      "A network security audit discovered six OT-1 taps on the backbone fiber connecting a government facility to external networks — the taps had been operational for over a year, capturing every byte of the facility's communications.",
      "An OT-1 was found on a fiber cable that carries quantum-encrypted traffic — while the encryption makes the data unreadable, the tap's metadata capture reveals communication patterns that are intelligence gold."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Emergency Shelter Pod ESP-2",
    type: "equipment",
    aliases: ["ESP-2", "Safe Pod", "Shelter Ball", "Zheng-Dao Bunker"],
    category: "survival",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A self-deploying emergency shelter that expands from a backpack-sized package into a rigid, sealed habitat capable of sustaining one person for 72 hours. The ESP-2 deploys in 10 seconds using a shape-memory alloy frame that springs into a 2-meter pod when released. The shelter includes atmospheric filtration, water recycling, emergency rations, basic medical supplies, and a distress beacon. The pod's shell provides ballistic protection against small arms fire and NBC protection against chemical and biological threats. It is designed for survival in hostile environments — natural disasters, combat zones, contaminated areas — where getting to safety is not possible and waiting for rescue is the only option.",
    specifications: "deployment: 10 seconds from pack to shelter\noccupancy: 1 person\nlife support: 72 hours atmospheric filtration + water recycling\nrations: 72-hour emergency food supply\nmedical: Basic first aid + 2x FTI-5 injectors\ndistress beacon: Encrypted, 100 km range\nballistic protection: Small arms resistant shell\nNBC protection: Sealed atmospheric filtration\nweight packed: 8 kg\ndeployed size: 2m x 1m x 1m pod",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — emergency equipment",
    street_price: "Φ6,500",
    story_hooks: [
      "A deployed ESP-2 was found in an alley with its occupant dead inside — the shelter was sealed and functional but the occupant had been dead for days before anyone noticed the pod.",
      "ESP-2 pods are being used as improvised housing in Tier 1 — clusters of deployed shelters form small communities where each pod provides better protection than the buildings they replaced."
    ]
  },
  {
    name: "Arcturus Defense Solutions Signal Jammer Grenade SJG-3",
    type: "equipment",
    aliases: ["SJG-3", "Comm Kill", "Static Bomb", "Dead Air"],
    category: "electronic_warfare",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A throwable device that emits a powerful broadband electromagnetic jamming signal in a 30-meter radius for 5 minutes, disrupting all wireless communications, sensor systems, and neural interface external connections within range. The SJG-3 is used for tactical communications denial — thrown into an area before an assault to prevent defenders from calling for reinforcements or coordinating response. The jamming signal is powerful enough to disrupt hardened military communications and causes painful interference in neural interfaces, which most users experience as a sudden, overwhelming burst of static in their augmented senses.",
    specifications: "jamming radius: 30 meters\nduration: 5 minutes\nfrequency coverage: 100 kHz to 60 GHz — total broadband\neffect on comms: Complete wireless denial\neffect on neural interface: Painful static interference, temporary sensory disruption\neffect on sensors: Blinding of all electronic sensor systems\nweight: 0.25 kg\nactivation: Pin-pull, 2-second delay\npower: Single-use capacitor discharge",
    tier_availability: "Tier 3+",
    legality: "Restricted — military and authorized operations",
    street_price: "Φ2,800",
    story_hooks: [
      "Three SJG-3 grenades were detonated simultaneously in a Tier 3 commercial district, creating a 90-meter dead zone during which a precision robbery was executed — no communications in or out for five minutes.",
      "A modified SJG-3 has been developed that specifically targets neural interface frequencies while leaving other communications functional — it incapacitates augmented personnel while unaugmented attackers remain unaffected."
    ]
  },
  {
    name: "Street Custom 'Packrat' Concealed Carry Harness",
    type: "equipment",
    aliases: ["Packrat", "Carry Rig", "Hidden Holster", "Stash Harness"],
    category: "containers",
    manufacturer: "Street Custom",
    description: "A body-worn harness system designed to conceal multiple weapons, tools, and equipment under standard clothing. The Packrat uses a modular design of pouches, holsters, and clips arranged across the torso, inner thighs, and small of back to distribute weight evenly and minimize visible printing through fabric. A well-fitted Packrat can conceal a pistol, three magazines, a knife, a cyberdeck, a medical kit, and assorted tools without creating visible bulges or asymmetric weight distribution. The harness is custom-fitted to the wearer's body and adjustable for different clothing styles. It has become the standard concealment system for anyone who needs to carry lethal capability through areas where weapons are not welcome.",
    specifications: "capacity: Pistol + 3 magazines + knife + cyberdeck + med kit + tools\nweight distribution: Even across torso and thighs\nprinting: Minimal under standard clothing\ncustomization: Body-fitted, adjustable modular pouches\nmaterial: Nylon webbing with neoprene backing\nweight: 0.5 kg empty\naccessibility: Draw times under 1.5 seconds for primary weapon\ncomfort: Designed for 12+ hour continuous wear",
    tier_availability: "Tier 1+",
    legality: "Legal — concealment harness; illegal if containing prohibited items",
    street_price: "Φ80-400 depending on build quality",
    story_hooks: [
      "A security checkpoint missed a complete weapons loadout on a suspect who was wearing a custom Packrat — the harness was specifically designed to distribute metal mass below the detection threshold of the checkpoint's scanners.",
      "A Packrat design has been developed with integrated Faraday pouches that shield contained devices from tracking — anything stored in the harness disappears from electronic detection."
    ]
  },
  {
    name: "Vespid Dynamics Pheromone Trail Marker PTM-2",
    type: "equipment",
    aliases: ["PTM-2", "Scent Trail", "Path Marker", "Invisible Breadcrumbs"],
    category: "sensors",
    manufacturer: "VESPID DYNAMICS",
    description: "A wrist-mounted dispenser that releases invisible synthetic pheromone compounds to mark a path, location, or target. The PTM-2 uses Vespid's pheromone library to create markers detectable only by specialized sensors or Vespid drone platforms, creating invisible trails that can be followed hours or days after being laid. Different pheromone compounds encode different meanings — danger, safe passage, target location, rally point — allowing the creation of an invisible information layer overlaid on the physical environment. The markers persist on surfaces for 48-72 hours depending on weather conditions.",
    specifications: "compounds: 8 distinct pheromone markers\npersistence: 48-72 hours on surfaces\ndetection: Vespid drone platforms or specialized sensor\napplication: Spray from wrist-mounted dispenser\nrange: Contact application — spray on surface from 30 cm\ncapacity: 200 applications per cartridge\nweight: 0.08 kg\nvisibility: Invisible to human senses\ndetection range: Drone platforms detect at 50 meters",
    tier_availability: "Tier 2+",
    legality: "Restricted — intelligence operations",
    street_price: "Φ1,800",
    story_hooks: [
      "A pheromone trail was discovered leading from a secure facility's emergency exit to a waiting vehicle location — someone marked the escape route in advance using PTM-2 compounds.",
      "Vespid's drones have been following PTM-2 trails that were not laid by authorized operators — someone has obtained the pheromone compounds and is manipulating Vespid's own drone networks."
    ]
  },
  {
    name: "Sterling-Nakamura Diplomatic Immunity Bracelet DIB-1",
    type: "equipment",
    aliases: ["DIB-1", "Get Out Free", "Immunity Band", "Sterling Pass"],
    category: "tools",
    manufacturer: "STERLING-NAKAMURA",
    description: "A wrist-worn device that continuously broadcasts a verified diplomatic immunity credentials signal recognized by law enforcement systems throughout GLMZ. The DIB-1 contains a quantum-encrypted identity chip that authenticates the wearer's diplomatic status to any scanner or automated system, triggering legal protections that prevent detention, search, or arrest. The bracelet's signal is recognized by autonomous security systems, which stand down when detecting a valid diplomatic credential. Sterling-Nakamura issues them to senior personnel and allied diplomatic staff — the bracelet does not make the wearer invincible, but it makes detaining them a significant legal and political event.",
    specifications: "broadcast: Continuous diplomatic credential signal\nauthentication: Quantum-encrypted identity chip\nrecognition: Compatible with all GLMZ law enforcement systems\neffect: Triggers diplomatic immunity legal protections\nrange: 10-meter broadcast radius\nbattery: 1-year operation\nweight: 0.05 kg\ntamper detection: Self-wipe on unauthorized removal\nform factor: Elegant wrist band, corporate branded",
    tier_availability: "Tier 5",
    legality: "Issued — Sterling-Nakamura diplomatic staff only",
    street_price: "Φ500,000+ (if obtainable — extremely rare)",
    story_hooks: [
      "A DIB-1 was used by a murder suspect to walk through a crime scene perimeter — the automated security systems stood down and human officers hesitated to challenge a diplomatic credential, giving the suspect time to escape.",
      "A counterfeit DIB-1 has appeared that broadcasts a valid-seeming diplomatic credential — the forgery is good enough to fool automated systems but will not survive manual verification, creating a narrow window of false immunity."
    ]
  },
  {
    name: "Axiom Systems Neural Dead Drop NDD-2",
    type: "equipment",
    aliases: ["NDD-2", "Brain Drop", "Neural Cache", "Mind Locker"],
    category: "hacking",
    manufacturer: "AXIOM SYSTEMS",
    description: "A concealed device that stores encrypted data accessible only through a specific neural interface authentication. The NDD-2 can be hidden anywhere — embedded in a wall, buried underground, or concealed in everyday objects — and appears as inert material to all scanning methods. When an authorized neural interface comes within 10 cm, the device authenticates the user's neural signature and releases the stored data directly to their implant. The transfer is invisible to external monitoring and leaves no trace on the recipient's interface after the data is read. Axiom designed it for intelligence operations where physical document exchange or electronic transmission are too risky.",
    specifications: "storage: 1 TB encrypted\nactivation: Neural interface authentication at 10 cm\ntransfer: Direct to neural interface, invisible to monitoring\ndata persistence: Configurable — single read or persistent\nauthentication: Unique neural interface signature match\nconcealment: Appears inert to all scanning methods\nsize: 2 cm x 1 cm x 0.5 cm\nbattery: 2-year standby\nform factor: Embeddable in any material",
    tier_availability: "Tier 4+",
    legality: "Prohibited — covert intelligence device",
    street_price: "Φ15,000",
    story_hooks: [
      "A player character brushes against a wall and their neural interface receives an unexpected data payload — someone planted an NDD-2 keyed to their specific neural signature with a message meant only for them.",
      "A network of NDD-2 devices has been discovered embedded in public infrastructure across Tier 3 — someone has built a city-wide dead drop network for intelligence exchange."
    ]
  },
  {
    name: "Street Custom 'Trash Armor' Improvised Body Protection",
    type: "equipment",
    aliases: ["Trash Armor", "Garbage Plate", "Scrap Vest", "Junk Shield"],
    category: "armor",
    manufacturer: "Street Custom",
    description: "Body armor improvised from whatever materials are available — layers of duct tape and phone books over vital areas, road sign metal cut into chest plates, layers of ceramic tile taped together, or vehicle floor mats worn as vests. Trash Armor is the protective equipment of people who cannot afford real armor, and its effectiveness varies from surprisingly adequate (layered ceramics can stop handgun rounds) to dangerously inadequate (duct tape and cardboard). The cultural significance of Trash Armor in Tier 1 is that wearing it acknowledges a specific reality: your life is dangerous enough to need armor, and you are poor enough that this is the best you can do.",
    specifications: "protection: Variable — ranges from NIJ Level I to nothing useful\ncommon materials: Ceramic tile, road signs, phone books, floor mats\nweight: 2-8 kg depending on materials\ncoverage: Typically front torso only\ncomfort: Minimal\nmobility restriction: Significant with heavier builds\ncost: Φ0-20 in salvage materials\nconstruction time: 1-3 hours",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — improvised protective equipment",
    street_price: "Φ0-20",
    story_hooks: [
      "A Tier 1 resident survived a shooting because their Trash Armor — six layers of ceramic floor tile — stopped a handgun round. The incident has become a viral moment that highlights the absurd ingenuity of survival in the lower tiers.",
      "A Trash Armor design using a specific combination of layered materials has been tested and found to outperform some commercial body armor at 1/100th the cost — the design is spreading and corporate armor manufacturers are not happy."
    ]
  },
  {
    name: "Carrion Defense Works Bioelectric Camouflage Generator BCG-2",
    type: "equipment",
    aliases: ["BCG-2", "Life Mask", "Bio Camo", "Heartbeat Faker"],
    category: "counter-surveillance",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A body-worn device that generates a configurable bioelectric field mimicking the signature of a different person, animal, or no living being at all. The BCG-2 can spoof biometric sensors that detect heartbeat, bioelectric brain activity, galvanic skin response, and body impedance — the sensor reads whatever the BCG-2 tells it to read. The device can make the wearer appear dead to biometric scanners, mimic the bioelectric signature of a specific individual for access control bypass, or generate the signature of an animal to explain detection without raising alarm. Carrion developed it for covert operators who need to defeat biometric perimeter security.",
    specifications: "field type: Configurable bioelectric signature\nmodes: Null (appear dead), clone (specific individual), mask (animal), custom\nbioelectric mimicry: Heartbeat, brain activity, skin response, impedance\neffective range: Overpowers sensors within 2 meters\nbattery: 8-hour active generation\nweight: 0.2 kg\nform factor: Belt-worn module\nprogramming: Neural interface or manual preset selection",
    tier_availability: "Tier 3+",
    legality: "Prohibited — biometric spoofing device",
    street_price: "Φ18,000",
    story_hooks: [
      "A secured area's biometric perimeter showed no living presence during a time when a theft was committed — the intruder used a BCG-2 in null mode, appearing as a walking dead zone to every sensor.",
      "A BCG-2 was used to clone an executive's bioelectric signature and gain access to a biometric-locked facility — the intrusion was undetectable because the sensors saw exactly the right person."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Magnetic Anchor Boot System MABS-2",
    type: "equipment",
    aliases: ["MABS-2", "Mag Boots", "Metal Walkers", "Zheng-Dao Grips"],
    category: "climbing/mobility",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "Boots with integrated electromagnetic soles that can be activated to anchor the wearer to any ferrous metal surface with up to 200 kg of pull force per boot. The MABS-2 enables walking on steel walls, ceilings, and exterior surfaces of metal structures including ships, industrial facilities, and steel-framed buildings. The electromagnetic engagement is toggled with a foot-flex gesture, and the force can be modulated for walking (light engagement), climbing (medium), or anchoring against blast or wind (maximum). The boots also prevent the wearer from being thrown by kinetic impacts when anchored to a surface, making them useful for heavy weapons operation and vehicle-mounted combat.",
    specifications: "anchor force: 200 kg per boot maximum\nsurface requirement: Ferrous metal\ncontrol: Foot-flex toggle, modulated force\nmodes: Walk, climb, anchor\nbattery: 12-hour active use\nweight: 1.5 kg per boot\ncompatibility: Retrofittable to standard boot frames\nmodulation: Variable force 0-200 kg per boot",
    tier_availability: "Tier 2+",
    legality: "Licensed — industrial and security",
    street_price: "Φ3,200",
    story_hooks: [
      "An intruder was found walking on the exterior steel hull of a corporate tower at the 50th floor — MABS-2 boot prints were found on the steel cladding, and the intruder accessed a window that no conventional approach could reach.",
      "A modified MABS-2 has been weaponized — the electromagnetic pulse on maximum engagement can disable electronics in the steel surface the boot contacts, making each step an EMP attack on the floor beneath."
    ]
  },
  {
    name: "Tessera Industries Reality Anchor Projector RAP-1",
    type: "equipment",
    aliases: ["RAP-1", "Truth Lamp", "De-Glitch", "Reality Check"],
    category: "counter-surveillance",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A device that disrupts holographic projections, augmented reality overlays, and adaptive camouflage systems within a 15-meter radius by emitting a structured light pattern that interferes with the coherent light used by these systems. The RAP-1 essentially forces the visual environment to be real — holograms flicker and collapse, AR overlays glitch and disappear, and active camouflage systems display artifacts that reveal the concealed object. Tessera developed it as a countermeasure to their own holographic and metamaterial products, recognizing that a tool to defeat illusions has as much market value as the illusions themselves.",
    specifications: "effect radius: 15 meters\ndisrupted systems: Holographic projections, AR overlays, active camouflage\nmethod: Structured light interference\neffect: Holograms collapse, AR glitches, camo shows artifacts\npower: 4-hour battery\nweight: 0.5 kg\nform factor: Handheld wand or mounted emitter\nactivation: Continuous emission while powered\nlimitations: Does not affect passive materials or metamaterial surfaces",
    tier_availability: "Tier 3+",
    legality: "Licensed — security and investigation",
    street_price: "Φ9,500",
    story_hooks: [
      "A RAP-1 activated at a social event revealed that three of the guests were holographic projections — no one knew they were not physically present, and the question of who sent the holograms and why they needed to appear present is urgent.",
      "A modified RAP-1 has been developed that forces ROCL augmented reality overlays to display the raw, unmodified physical environment — users describe the experience as seeing their world stripped naked, and some find the real version deeply disturbing."
    ]
  },
  {
    name: "Street Custom 'Black Widow' Electronic Bait Device",
    type: "equipment",
    aliases: ["Black Widow", "Bait Box", "Trap Node", "Honey Pot"],
    category: "electronic_warfare",
    manufacturer: "Street Custom",
    description: "A small device that mimics the signal profile of a high-value electronic target — an unprotected neural interface, an open Diaspora node, or an unsecured corporate data terminal — to lure hackers and cyber-intruders into connecting. When a hostile operator attempts to access the fake target, the Black Widow counter-attacks through the connection, deploying malware, extracting the attacker's location and identity, or injecting a tracking payload into their neural interface. The device is essentially a digital booby trap, punishing anyone who attempts unauthorized access to what appears to be an easy target.",
    specifications: "mimicry: Neural interface, Diaspora node, or data terminal signatures\ncounter-attack: Malware deployment, identity extraction, tracking payload\ntrigger: Unauthorized access attempt\nresponse time: Sub-second counter-attack on connection\nweight: 0.1 kg\nform factor: Small box, concealable\nbattery: 30-day standby\nprogrammable: Multiple trap profiles and response types",
    tier_availability: "Tier 2+",
    legality: "Legal gray area — defensive countermeasure vs. offensive cyberweapon",
    street_price: "Φ2,000-5,000 depending on counter-attack payload",
    story_hooks: [
      "A Black Widow device deployed near a corporate facility caught a hacker — the counter-attack extracted their neural interface identity, revealing them as an employee of a rival corporation conducting espionage.",
      "A network of Black Widow devices has been deployed across a district, creating a minefield for cyber-operators — anyone who scans for targets in the area risks tripping a trap that exposes their identity and location."
    ]
  },
  {
    name: "Vespid Dynamics Atmospheric Sampling Drone ASD-5",
    type: "equipment",
    aliases: ["ASD-5", "Air Tester", "Sky Sampler", "Atmosphere Scout"],
    category: "drones",
    manufacturer: "VESPID DYNAMICS",
    description: "A weather-resistant drone equipped with a comprehensive atmospheric analysis suite for environmental monitoring, contamination assessment, and air quality mapping. The ASD-5 samples air at altitude and analyzes particulates, chemical compounds, biological agents, and radiation levels in real-time, creating 3D atmospheric maps that show contamination plumes, clean air corridors, and threat concentrations. The drone can operate in adverse weather conditions including rain, high winds, and extreme temperatures, making it suitable for emergency response and contamination zone assessment.",
    specifications: "analysis: Particulates, chemicals, biological agents, radiation\nmapping: Real-time 3D atmospheric composition\naltitude: Up to 500 meters\nflight time: 2 hours\nweather resistance: Rain, 60 km/h winds, -20°C to +50°C\nsampling rate: Continuous analysis during flight\ndata transmission: Real-time encrypted to ground station\nweight: 2.5 kg\nsize: 40 cm rotor-to-rotor",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — environmental monitoring",
    street_price: "Φ12,000",
    story_hooks: [
      "An ASD-5 mapping a Tier 2 district detected an unknown biological agent at altitude that ground-level sensors had not registered — something is being dispersed from above, and the source is not visible.",
      "ASD-5 atmospheric data has revealed that air quality in a specific district varies on a schedule that correlates with shift changes at a factory 5 km away — the factory is venting contaminants during off-hours when monitoring is reduced."
    ]
  },
  {
    name: "Arcturus Defense Solutions Hardened Communications Suite HCS-7",
    type: "equipment",
    aliases: ["HCS-7", "War Radio", "Arcturus Comm", "Hard Line"],
    category: "communications",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "A military-grade encrypted communications system hardened against jamming, interception, and EMP attack. The HCS-7 combines multiple communication modes — encrypted radio, laser-link, acoustic modem, and satellite uplink — in a ruggedized unit that automatically switches between channels based on the current threat environment. If radio is jammed, it switches to laser. If laser is blocked, it falls back to acoustic. The system maintains communication under conditions that would silence every other radio on the market. Arcturus issues it to field commanders and sells it to allied military organizations.",
    specifications: "modes: Encrypted radio, laser-link, acoustic modem, satellite uplink\nencryption: Quantum-key rolling encryption\njamming resistance: Frequency-hopping + mode switching\nEMP hardening: Military-grade Faraday enclosure\nrange: 50 km radio, line-of-sight laser, 500m acoustic, global satellite\nweight: 1.8 kg\nbattery: 48-hour multi-mode operation\nruggedness: Waterproof, shockproof, EMP-proof",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and authorized security",
    street_price: "Φ35,000",
    story_hooks: [
      "An HCS-7's multi-mode capability was the only communication system that functioned during a coordinated jamming attack — the operator's after-action report reveals that the acoustic modem was the only mode that the jammers could not defeat.",
      "A stolen HCS-7 has been modified to monitor all modes simultaneously rather than transmit — it has been turned into a multi-spectrum communications interceptor that can crack every channel except quantum-encrypted ones."
    ]
  },
  {
    name: "Street Custom 'Paper Tiger' Decoy Wallet",
    type: "equipment",
    aliases: ["Paper Tiger", "Fake Wallet", "Mugger's Friend", "Decoy Purse"],
    category: "disguise",
    manufacturer: "Street Custom",
    description: "A realistic-looking wallet or purse loaded with expired credit chits, fake identity cards, and a concealed GPS tracker that activates when the wallet is opened. The Paper Tiger is designed to be surrendered during a mugging or pickpocket event, satisfying the attacker's demand while providing nothing of value and tracking the thief's subsequent movements. Some variants include a dye pack that stains the thief's hands, a chemical irritant capsule, or a micro-flashbang that detonates 30 seconds after opening. The device has become common enough that experienced street criminals in GLMZ now check wallets for trackers before fleeing.",
    specifications: "contents: Expired credit chits, fake IDs, GPS tracker\ntracker activation: On wallet opening\ntracker range: City-wide GPS, 72-hour battery\noptional payloads: Dye pack, chemical irritant, micro-flashbang\nweight: 0.15 kg\nappearance: Indistinguishable from genuine wallet\nbattery: 6-month standby\ntracker transmission: Encrypted to owner's linked device",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — anti-theft device",
    street_price: "Φ50-200",
    story_hooks: [
      "A Paper Tiger tracker led its owner to the location of a organized fencing operation — the wallet was stolen as bait to map the criminal network's physical infrastructure.",
      "A Paper Tiger variant has been weaponized with a shaped explosive instead of a dye pack — several muggers have been killed by wallets that detonate when opened."
    ]
  },
  {
    name: "Lazarus Pharmaceuticals Emergency Cryo-Stasis Pod ECP-1",
    type: "equipment",
    aliases: ["ECP-1", "Ice Box", "Cryo Pod", "Lazarus Freeze"],
    category: "medical",
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "A portable cryogenic preservation system that rapidly cools a patient to metabolic stasis temperatures, halting all biological processes including cellular death, hemorrhaging, and toxin propagation. The ECP-1 is designed for pre-hospital care of critically injured or poisoned patients who cannot be stabilized by conventional field treatment — rather than trying to save them on-scene, the patient is preserved in stasis for transport to a definitive care facility. The pod cools the patient to -80°C within 3 minutes using a circulating cryoprotectant that prevents ice crystal formation in tissues, and maintains stasis for up to 48 hours.",
    specifications: "cooling time: 3 minutes to -80°C metabolic stasis\nstasis duration: 48 hours maximum\ncryoprotectant: Automated injection and circulation\nice crystal prevention: Vitrification protocol\nrevival: Requires equipped medical facility\nweight: 25 kg\nform factor: Rigid case, person-sized when deployed\npower: Integrated cooling system, self-contained\nshelf life: 5 years before cryoprotectant replacement",
    tier_availability: "Tier 3+",
    legality: "Licensed — emergency medical equipment",
    street_price: "Φ45,000",
    story_hooks: [
      "A patient was placed in an ECP-1 after a fatal poisoning, preserving them in stasis — but the antidote requires a substance that does not exist in any known pharmacy, and the 48-hour stasis window is ticking.",
      "Someone has been placing living, conscious people in ECP-1 pods as a form of kidnapping — the victims experience 48 hours of frozen awareness before the stasis fails and they wake up in an unknown location."
    ]
  },
  {
    name: "Tessera Industries Metamaterial Lock Pick Set MLP-1",
    type: "equipment",
    aliases: ["MLP-1", "Shape Picks", "Tessera Keys", "Universal Picks"],
    category: "tools",
    manufacturer: "TESSERA INDUSTRIES",
    description: "A set of lockpicking tools fabricated from programmable matter that can dynamically reconfigure their shape to match any lock profile. The MLP-1 contains three programmable matter tools that can transform from standard tension wrenches and picks to specialized decoder tools, tubular lock picks, wafer picks, and any other configuration needed for a specific lock type. The tools are controlled through a neural interface that scans the lock's keyway and automatically shapes the tool to match. Where a conventional lockpick set requires 15+ individual tools and extensive practice, the MLP-1 provides a universal solution that adapts itself to the challenge.",
    specifications: "tools: 3 programmable matter instruments\nconfigurations: Universal — any lock profile\ncontrol: Neural interface scan and auto-shape\nreconfiguration time: 2 seconds per tool\nmaterial: Tessera programmable matter\nweight: 0.08 kg total set\nform factor: Small case, pen-sized tools\nbattery: 48-hour active use per tool\nlimitations: Cannot bypass electronic locks — mechanical only",
    tier_availability: "Tier 4+",
    legality: "Prohibited — universal lock bypass device",
    street_price: "Φ55,000",
    story_hooks: [
      "A lock thought to be unpickable was defeated in under 10 seconds — the only tool capable of that feat is an MLP-1 with its auto-scan feature, and fewer than 100 units exist.",
      "A Tessera insider has leaked the programmable matter blueprint for lock pick configurations — anyone with access to a programmable matter fabricator can now produce MLP-1 equivalent tools."
    ]
  },
  {
    name: "Carrion Defense Works Chemical Hazard Oversuit CHO-4",
    type: "equipment",
    aliases: ["CHO-4", "Hazard Suit", "Chem Suit", "Carrion Skin"],
    category: "armor",
    manufacturer: "CARRION DEFENSE WORKS",
    description: "A full-body protective suit providing Level A chemical, biological, and radiological protection through a multi-layer barrier system with integrated self-contained breathing apparatus. The CHO-4 uses a laminate of chemical-resistant polymer, activated carbon fabric, and vapor-barrier membrane to create an impenetrable barrier against liquid, vapor, and particulate hazards. The suit includes 4 hours of breathing air, communication systems, and a heads-up display showing environmental threat data from integrated sensors. Carrion designed it for their own CBRN operations teams but sells it through their defense catalog.",
    specifications: "protection level: EPA Level A — full CBRN encapsulation\nlayers: Chemical polymer + activated carbon + vapor barrier\nbreathing: Self-contained, 4-hour air supply\ncommunications: Integrated encrypted radio\nHUD: Environmental threat data display\nsensors: Chemical, biological, radiation detection\nweight: 12 kg with air supply\nmobility: Reduced — approximately 60% of normal movement\noperation time: 4 hours per air bottle",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — safety equipment",
    street_price: "Φ8,000",
    story_hooks: [
      "A team wearing CHO-4 suits entered a contaminated zone and their suits' sensors detected a compound not in any hazard database — the unknown agent was penetrating the activated carbon layer, a protection failure that should be impossible.",
      "CHO-4 suits have been stolen and modified for use in robberies — the full-body coverage and breathing apparatus make the wearer immune to tear gas and chemical deterrents while providing complete visual anonymity."
    ]
  },
  {
    name: "Axiom Systems Parasitic Data Harvester PDH-3",
    type: "equipment",
    aliases: ["PDH-3", "Data Leech", "Axiom Tick", "Info Vampire"],
    category: "hacking",
    manufacturer: "AXIOM SYSTEMS",
    description: "A device designed to be covertly attached to a target's neural interface charging cable, harvesting data from every charging session. The PDH-3 intercepts the data channel that neural interfaces use during charging to perform backup, update, and diagnostic operations, copying the transmitted data to an encrypted internal storage. Over successive charging sessions, the PDH-3 accumulates a comprehensive picture of the target's neural interface contents — communications, stored memories, access credentials, and cognitive enhancement configurations. The device is powered parasitically from the charging cable and adds no detectable load.",
    specifications: "installation: Inline with neural interface charging cable\ndata capture: All charging session data traffic\nstorage: 20 TB encrypted\npower: Parasitic from charging cable\ndetectability: No additional load — transparent to diagnostics\nsize: 1 cm x 0.5 cm inline module\ndata accumulation: Progressive over successive charging sessions\nretrieval: Physical removal or low-power burst transmission",
    tier_availability: "Tier 3+",
    legality: "Prohibited — neural interface surveillance device",
    street_price: "Φ8,000",
    story_hooks: [
      "A PDH-3 found on an executive's charging cable had been accumulating data for nine months — the stored data includes every credential, communication, and stored memory backup from that period.",
      "PDH-3 devices have been found pre-installed in charging cables sold through a major retail chain — thousands of users may have been unknowingly sharing their neural interface data with an unknown collector."
    ]
  },
  {
    name: "Street Custom 'Rooftop Express' Zipline Deployment Kit",
    type: "equipment",
    aliases: ["Rooftop Express", "Zip Kit", "Line Runner", "Sky Bridge"],
    category: "climbing/mobility",
    manufacturer: "Street Custom",
    description: "A portable zipline system consisting of a high-strength cable, pneumatic anchor launcher, and friction-brake trolley that enables rapid transit between buildings across open gaps. The kit fires a cable anchor to a target point up to 100 meters away, tensions the cable with an integrated winch, and provides a trolley handle with variable-friction brake for speed control during the crossing. The system is designed for one-way transit — ascending ziplines are possible but require motorized trolleys that add weight and complexity. Street operators use it for rapid building-to-building movement, escape routes, and tactical repositioning in urban environments.",
    specifications: "cable length: 100 meters high-strength braided steel\nanchor launch: Pneumatic, accurate to 1-meter at 50 meters\ncable strength: 500 kg rated\ntrolley: Friction-brake speed control\nmaximum load: 120 kg rider + equipment\nsetup time: 90 seconds from anchor launch to crossing\nweight: 3.5 kg total kit\nreusability: Cable and trolley reusable, anchor is expendable",
    tier_availability: "Tier 1+",
    legality: "Legal as equipment — illegal when used for unauthorized building access",
    street_price: "Φ600",
    story_hooks: [
      "A series of Tier 2 rooftop burglaries are connected by zipline anchor marks on building parapets — someone is running a route across the skyline, hitting targets along a planned path.",
      "An emergency evacuation from a burning building was facilitated by a Rooftop Express kit — the operator ziplined 40 people to an adjacent building before fire services arrived."
    ]
  },
  {
    name: "Zheng-Dao Heavy Industries Seismic Sensor Network SSN-3",
    type: "equipment",
    aliases: ["SSN-3", "Ground Ears", "Tremor Net", "Zheng-Dao Listener"],
    category: "sensors",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    description: "A deployable network of ground-mounted seismic sensors that detect and classify movement on and below the surface within a monitored area. Each sensor spike is driven into the ground and detects vibrations from footsteps, vehicles, tunneling activity, and structural settling. The network's AI classifies detected vibrations — distinguishing between a walking person, a running person, a vehicle, and mechanical digging — and provides the count, direction, and speed of detected movement. The system can detect a single person walking at up to 100 meters and a vehicle at up to 500 meters through ground vibration alone.",
    specifications: "sensor type: Ground-spike seismic detector\nnetwork: Up to 20 sensors per controller\ndetection range: 100m person, 500m vehicle per sensor\nclassification: Walk, run, vehicle, dig, structural\npower: 90-day battery per sensor\nsensor weight: 0.2 kg each\ncontroller: Handheld, neural interface compatible\ninstallation: Drive into ground, 10 seconds per sensor\nterrain: Functions in soil, sand, concrete, and rock",
    tier_availability: "Tier 2+",
    legality: "Licensed — perimeter security",
    street_price: "Φ6,000 per network (controller + 10 sensors)",
    story_hooks: [
      "An SSN-3 network deployed around a secure facility detected tunneling activity 5 meters underground — someone is digging toward the facility from a building 200 meters away.",
      "An SSN-3 network in Tier 1 detected a regular pattern of heavy vehicle movement through an area with no road access — the vehicles are using an underground tunnel system that surface observers never knew existed."
    ]
  },
  {
    name: "Sterling-Nakamura Biometric Spoof Kit BSK-2",
    type: "equipment",
    aliases: ["BSK-2", "ID Faker", "Bio Spoof", "Sterling Mask Kit"],
    category: "disguise",
    manufacturer: "STERLING-NAKAMURA",
    description: "A compact kit containing tools for replicating and spoofing biometric authentication systems — fingerprint molds, retinal pattern contact lenses, voice modulation software, and gait adjustment insoles. The BSK-2 starts with a target's biometric data (obtained through surveillance, data breach, or physical collection) and produces physical devices that replicate those biometrics well enough to defeat standard commercial scanners. The fingerprint molds use silicone casting from lifted prints, the retinal lenses project a stored pattern over the wearer's own retina, and the voice software processes the user's speech in real-time to match the target's vocal characteristics.",
    specifications: "fingerprint: Silicone molds from lifted prints\nretinal: Pattern-projecting contact lenses\nvoice: Real-time vocal characteristic modulation\ngait: Insoles that alter stride pattern and cadence\npreparation time: 2-4 hours per biometric type\neffectiveness: Defeats standard commercial scanners\nlimitations: Does not defeat live tissue verification\nweight: 0.5 kg complete kit\nshelf life: Fingerprint molds 48 hours, retinal lenses 72 hours",
    tier_availability: "Tier 3+",
    legality: "Prohibited — identity fraud equipment",
    street_price: "Φ22,000",
    story_hooks: [
      "A secured facility was accessed using perfect biometric replicas of an authorized employee — the employee was at home at the time, and the BSK-2 components found at the scene show the biometrics were captured from public surveillance footage.",
      "A BSK-2 has been used to frame a specific individual for multiple crimes — their biometric signatures appear at crime scenes they never visited, and the evidence is convincing enough for arrest warrants."
    ]
  },
  {
    name: "Vespid Dynamics Toxic Environment Navigation Drone TEND-3",
    type: "equipment",
    aliases: ["TEND-3", "Hazard Scout", "Poison Mapper", "Safe Path"],
    category: "drones",
    manufacturer: "VESPID DYNAMICS",
    description: "A ruggedized drone designed to navigate chemically, biologically, or radiologically contaminated environments and map safe passage routes for human operators. The TEND-3 carries a comprehensive CBRN sensor suite and projects its findings as a real-time 3D contamination map, highlighting lethal zones, marginal areas, and safe corridors. The drone can operate in environments that would kill an unprotected human — toxic gas concentrations, high radiation fields, and biological contamination zones — and its sealed construction prevents it from becoming a contamination vector on return. It has become essential equipment for operations in GLMZ's legacy contamination zones.",
    specifications: "sensors: Chemical, biological, radiation, particulate\nmapping: Real-time 3D contamination model\nenvironmental tolerance: Chemical/biological/radiation environments\nsealed construction: Prevents cross-contamination\nflight time: 45 minutes\noperating range: 500 meters from controller\nweight: 1.5 kg\ndecontamination: UV + chemical wash protocol between missions",
    tier_availability: "Tier 2+",
    legality: "Unrestricted — safety equipment",
    street_price: "Φ14,000",
    story_hooks: [
      "A TEND-3 mapping a legacy contamination zone discovered a pocket of breathable air deep inside a toxic area — inside the pocket was a functioning habitat where someone has been living in the middle of a wasteland.",
      "A TEND-3 sent into a suspected chemical attack site returned data showing a contaminant that does not match any known industrial or military compound — it is something new, and it is spreading."
    ]
  },
  {
    name: "Axiom Systems Distributed Identity Cloak DIC-2",
    type: "equipment",
    aliases: ["DIC-2", "ID Fog", "Identity Cloud", "Axiom Scatter"],
    category: "counter-surveillance",
    manufacturer: "AXIOM SYSTEMS",
    description: "A device that floods local surveillance systems with dozens of false identity signals, making it impossible to determine the wearer's true identity among a crowd of spoofed personas. The DIC-2 broadcasts multiple simultaneous false biometric, neural interface, and Diaspora identity signatures within a 10-meter radius, creating a cloud of phantom identities that overwhelms facial recognition, biometric tracking, and identity correlation systems. Within the cloud, every person appears to be everyone else — faces do not match IDs, neural signatures do not match bodies, and tracking systems lose the ability to associate any specific identity with a specific physical person.",
    specifications: "false identity generation: 30 simultaneous phantom signatures\neffect radius: 10 meters\naffected systems: Facial recognition, biometric tracking, neural interface correlation, Diaspora identity\nresult: Complete identity confusion within radius\nbattery: 4-hour active broadcast\nweight: 0.25 kg\nform factor: Belt-worn or pocket module\nactivation: Single button, instant effect",
    tier_availability: "Tier 4+",
    legality: "Prohibited — identity obfuscation device",
    street_price: "Φ40,000",
    story_hooks: [
      "A DIC-2 was activated in a crowded transit hub, causing every surveillance system in the area to simultaneously flag 300 identity mismatches — in the confusion, someone walked through security with a weapon that would normally have been detected through identity correlation.",
      "A permanent DIC-2 installation has been discovered in a Tier 3 building, creating an always-on identity fog that makes the building's occupants effectively untrackable — the building has become a haven for anyone who needs to disappear."
    ]
  }
];

function toFileName(name) {
  return name
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

let written = 0;
let skipped = 0;
for (const e of equipment) {
  const fname = toFileName(e.name);
  const fpath = path.join(outDir, fname);
  if (fs.existsSync(fpath)) {
    skipped++;
    continue;
  }
  fs.writeFileSync(fpath, JSON.stringify(e, null, 2) + '\n');
  written++;
}

console.log(`Equipment: wrote ${written}, skipped ${skipped} (already existed)`);
console.log(`Total equipment files now: ${fs.readdirSync(outDir).length}`);
