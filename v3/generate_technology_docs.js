const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'technology');
fs.mkdirSync(OUTPUT_DIR, { recursive: true });
const existing = new Set(fs.readdirSync(OUTPUT_DIR).map(f => f.toLowerCase()));

function slugify(name) {
  const trimmed = name.slice(0, 60);
  return trimmed
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

function id() {
  return crypto.randomBytes(16).toString('hex');
}

let written = 0;
let skipped = 0;

function emit(entry) {
  const filename = slugify(entry.name) + '.json';
  if (existing.has(filename)) {
    console.log('SKIP: ' + filename);
    skipped++;
    return;
  }
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(entry, null, 2), 'utf8');
  console.log('WROTE: ' + filename);
  existing.add(filename);
  written++;
}

// ═══════════════════════════════════════════════
// FORCEFIELDS (8 entries)
// ═══════════════════════════════════════════════

emit({
  id: id(),
  name: "Arcturus Defense Solutions AegisWall Personal Shield Unit",
  brand_name: "Arcturus",
  product_name: "AegisWall PSU-7",
  type: "technology",
  aliases: ["AegisWall", "PSU-7", "Personal Aegis"],
  subcategory: "defense",
  description: "A belt-mounted electromagnetic barrier generator that projects a contoured force envelope around the wearer's body. The AegisWall PSU-7 uses rapidly oscillating magnetic monopole emitters to create a field that deflects kinetic projectiles and absorbs directed-energy weapon discharge. The system draws power from a compact helion microcell mounted in the lumbar housing, providing approximately 14 minutes of continuous protection or up to 90 discrete impact absorptions before requiring a 40-minute recharge cycle.\n\nThe field itself is invisible to the naked eye under normal conditions but fluoresces a faint violet when struck, as the barrier's crystalline lattice structure momentarily becomes visible during energy redistribution. The PSU-7's onboard threat-assessment firmware prioritizes protection zones — head and torso receive maximum field density while extremities receive reduced coverage. Users report a persistent low-frequency hum and a sensation of mild static pressure against the skin when the field is active.\n\nArcturus markets the AegisWall exclusively to corporate security details and licensed private military operators, though black-market units stripped of their DRM lockouts circulate in the gray economy at roughly four times the retail price of 28,000 Φ. The unit's primary limitation is its vulnerability to sustained fire — the field degrades rapidly under continuous bombardment, and a sufficiently determined attacker with automatic weapons can collapse the barrier in under eight seconds of concentrated fire.",
  tier_availability: "Tier 4",
  developers: ["ARCTURUS DEFENSE SOLUTIONS"],
  base_technologies: ["Magnetic monopole emitter arrays", "Helion microcell power systems", "Threat-assessment firmware", "Crystalline lattice field projection"],
  enables: ["Personal ballistic immunity", "Directed-energy weapon resistance", "VIP protection without visible armor", "Close-quarters breach operations"],
  social_impact: "Personal shields have created a new class divide more visible than any before it. The people who can afford an AegisWall walk through the world functionally immune to the violence that kills everyone else. Street-level criminals have adapted — shield-crackers, sustained-fire tactics, and melee weapons designed to penetrate the field's close-range dead zone have all proliferated. The sight of someone walking calmly through gunfire has become the ultimate symbol of corporate privilege.",
  story_hooks: [
    "A black-market AegisWall unit activates unexpectedly during a routine scan, revealing it was modified to broadcast its wearer's location to an unknown receiver — someone is tracking shield users.",
    "An Arcturus firmware update quietly reduced the AegisWall's extremity coverage to conserve power, and three operators lost limbs before anyone noticed the change."
  ],
  tags: ["defense", "technology", "forcefield", "shield", "personal", "arcturus", "electromagnetic", "tier 4"]
});

emit({
  id: id(),
  name: "Crucible Industries Phalanx Building-Scale Deflector",
  brand_name: "Crucible",
  product_name: "Phalanx BSD-3",
  type: "technology",
  aliases: ["Phalanx", "BSD-3", "Block Shield", "The Dome"],
  subcategory: "defense",
  description: "A network of high-power electromagnetic emitter pylons installed at a building's structural hardpoints that generate an overlapping barrier field capable of protecting an entire city block. The Phalanx BSD-3 was originally designed for military forward operating bases but found its primary market in corporate real estate — corponations use Phalanx systems to protect their sovereign blocks from orbital debris, atmospheric weapons, and the occasional rival corporation's artillery. Each pylon stands roughly 4 meters tall and is hardened against direct attack, drawing power from the building's main reactor or from dedicated Ouroboros Energy trunk lines.\n\nThe Phalanx field operates on a fundamentally different principle than personal shields. Rather than a contoured body-envelope, the BSD-3 generates a dome or wall-geometry barrier using phase-synchronized graviton interference patterns that create a semi-permeable energy membrane. The field can be configured to block projectiles above a certain mass threshold while allowing foot traffic and light vehicles to pass through — a feature that makes it practical for protecting occupied structures without sealing them off from the world.\n\nDeployment requires municipal-grade power infrastructure and Crucible's proprietary installation team, limiting adoption to corponations and the wealthiest independent organizations. A full block installation runs approximately 4.2 million Φ before the ongoing power costs, which Ouroboros Energy charges at premium rates because they know exactly how much their clients need the shield to stay on. The Phalanx has a documented weakness to resonance attacks — if an attacker can determine the field's oscillation frequency, a precisely tuned counter-frequency can create destructive interference and open temporary gaps.",
  tier_availability: "Tier 5",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Graviton interference patterning", "Phase-synchronized emitter arrays", "Semi-permeable barrier calibration", "Hardened pylon architecture"],
  enables: ["City-block-scale protection", "Selective permeability barriers", "Corporate territory defense", "Anti-orbital debris shielding"],
  social_impact: "The Phalanx has made corporate sovereignty physical. Before building-scale shields, a corponation's territorial claim was a legal abstraction. Now it is a visible, tangible dome of force that separates the protected from the unprotected. Standing outside a Phalanx perimeter during a storm or an attack, watching the barrier shimmer while those inside go about their business untouched, is one of the defining experiences of inequality in GLMZ. Community organizers call the shields 'the new walls' — invisible until you need them, and always keeping the same people out.",
  story_hooks: [
    "Someone is selling Phalanx resonance frequencies on the darknet — each frequency corresponds to a specific corporate block's shield, and the prices suggest a buyer with very specific targets in mind.",
    "A Phalanx system in the financial district has been running at 40% capacity for three weeks because Ouroboros Energy is throttling power to the block as leverage in a contract dispute — the corponation inside is quietly paying mercenaries to guard the gaps."
  ],
  tags: ["defense", "technology", "forcefield", "shield", "building", "crucible", "graviton", "tier 5"]
});

emit({
  id: id(),
  name: "TESSERA Veilstrike Tactical Barrier Projector",
  brand_name: "TESSERA",
  product_name: "Veilstrike TBP-4",
  type: "technology",
  aliases: ["Veilstrike", "TBP-4", "Tac Barrier", "Hard Light Wall"],
  subcategory: "defense",
  description: "A portable, deployable barrier system that projects a flat-plane force wall from a tripod-mounted emitter. The Veilstrike TBP-4 creates a 3-meter-wide by 2-meter-tall barrier of densely packed electromagnetic flux that stops ballistic and energy weapon fire while allowing the operator to fire through it from the protected side using phase-synced ammunition. The barrier is visually opaque from the threat side — a shimmering wall of distorted light that obscures the positions of those behind it — but transparent from the operator side, functioning as both cover and concealment.\n\nThe TBP-4 deploys in under four seconds and runs for approximately 22 minutes on its integrated power cell. Multiple units can be networked to create longer barrier walls, with TESSERA's proprietary mesh protocol ensuring seamless field overlap between adjacent projectors. The system weighs 11 kilograms and is designed to be carried by a single operator in a backpack configuration, making it practical for tactical teams that need portable hard cover in environments where physical barricades are unavailable.\n\nTESSERA developed the Veilstrike for law enforcement and corporate security applications, but the system has found enthusiastic adoption among mercenary companies and well-funded criminal organizations. The phase-synced ammunition requirement — rounds must be encoded with the barrier's frequency signature to pass through — creates a lucrative consumables market that TESSERA exploits aggressively. A case of phase-synced 7mm caseless runs approximately 800 Φ, compared to 45 Φ for standard ammunition of the same caliber.",
  tier_availability: "Tier 4",
  developers: ["TESSERA"],
  base_technologies: ["Flat-plane electromagnetic flux generation", "Phase-synced projectile transparency", "Networked barrier mesh protocol", "Rapid deployment mechanics"],
  enables: ["Portable hard cover in open environments", "One-way fire capability", "Visual concealment with ballistic protection", "Networked defensive perimeters"],
  social_impact: "The Veilstrike has changed the geometry of urban combat. Firefights that once depended on existing architecture for cover now feature glowing barrier walls that appear and disappear as tactical situations evolve. Street-level operators without access to barrier tech are at a severe disadvantage, leading to an arms race in barrier-penetrating weapons and counter-barrier tactics. The phase-synced ammunition lock-in has also drawn criticism — operators who invest in Veilstrike hardware are economically tethered to TESSERA's ammunition supply chain.",
  story_hooks: [
    "A batch of counterfeit phase-synced ammunition has entered the market — it passes through Veilstrike barriers from both sides, and someone is selling it specifically to enemies of Veilstrike users.",
    "TESSERA has issued a recall on TBP-4 units manufactured in a specific lot range after three units detonated during deployment, but the recall notice was only sent to registered buyers — black-market units from that lot are still circulating."
  ],
  tags: ["defense", "technology", "forcefield", "barrier", "tactical", "tessera", "portable", "tier 4"]
});

emit({
  id: id(),
  name: "Electromagnetic Barrier Principles",
  brand_name: "",
  product_name: "",
  type: "technology",
  aliases: ["EM Barriers", "Force Field Theory", "Barrier Physics"],
  subcategory: "defense",
  description: "The fundamental physics of electromagnetic barriers in 2200 rest on the controlled generation and manipulation of magnetic monopoles — particles that were theorized for centuries before being reliably produced in Crucible Industries' Monopole Foundry in 2147. Magnetic monopoles, unlike conventional magnets with inseparable north and south poles, carry a single magnetic charge. When accelerated through precisely tuned emitter arrays, monopoles can be arranged into crystalline lattice structures that exist as standing waves in electromagnetic flux — barriers that interact with matter and energy as though they were solid surfaces.\n\nThe key breakthrough was not the production of monopoles themselves but the development of lattice stabilization algorithms by TESSERA's Applied Physics Division in 2159. These algorithms control the spacing, orientation, and oscillation frequency of monopoles within the barrier lattice, determining the barrier's properties: density, permeability, durability, and energy consumption. A denser lattice stops more but costs more power. A tuned lattice can selectively permit passage of objects matching specific electromagnetic signatures while blocking everything else. The mathematics are fiendishly complex, and the stabilization algorithms are among the most closely guarded trade secrets in the defense industry.\n\nBarrier technology's fundamental limitation is thermodynamic. Every joule of kinetic or electromagnetic energy the barrier absorbs must be dissipated, and the lattice itself radiates waste heat proportional to the energy it blocks. Sustained bombardment heats the barrier until the monopole lattice destabilizes and collapses — the failure mode that makes sustained fire the universal counter to barrier technology. Research into heat-sink lattice geometries and active cooling systems continues, but no solution has eliminated this constraint. The theoretical maximum continuous absorption for a personal-scale barrier remains approximately 2.4 megajoules before thermal collapse.",
  tier_availability: "Tier 3",
  developers: ["CRUCIBLE INDUSTRIES", "TESSERA"],
  base_technologies: ["Magnetic monopole production", "Lattice stabilization algorithms", "Standing-wave electromagnetic flux", "Thermal dissipation modeling"],
  enables: ["Personal shield units", "Building-scale deflectors", "Tactical barrier projectors", "Selective-permeability membranes"],
  social_impact: "The understanding of barrier physics has become a strategic resource. Corponations that control the fundamental research control who can and cannot build shields. Crucible Industries' monopole production monopoly and TESSERA's algorithm patents create a duopoly that dictates the terms of barrier technology access worldwide. Independent researchers who publish barrier physics advances tend to receive lucrative job offers — or disappear.",
  story_hooks: [
    "A university researcher claims to have developed a barrier lattice that operates at room temperature regardless of incoming energy — if true, it would make shields effectively invulnerable. She has gone into hiding.",
    "Crucible's Monopole Foundry has been producing monopoles with anomalous properties for the past six months — barriers built with them are 30% stronger but occasionally exhibit behavior the stabilization algorithms cannot predict."
  ],
  tags: ["defense", "technology", "forcefield", "physics", "monopole", "fundamental", "tier 3"]
});

emit({
  id: id(),
  name: "Arcturus Defense Solutions Citadel Mobile Barrier",
  brand_name: "Arcturus",
  product_name: "Citadel MBG-12",
  type: "technology",
  aliases: ["Citadel", "MBG-12", "Rolling Shield", "Mobile Dome"],
  subcategory: "defense",
  description: "A vehicle-mounted barrier generator that projects a protective dome around a moving convoy. The Citadel MBG-12 is installed on armored personnel carriers and executive transport vehicles, generating a hemispherical shield with a 15-meter radius that moves with the vehicle. The system uses a phased array of monopole emitters mounted on the vehicle's roof and fenders, with real-time field geometry adjustments calculated by an onboard AI that compensates for vehicle speed, terrain, and incoming threat vectors.\n\nThe mobile barrier presents unique engineering challenges that static installations avoid. A moving barrier must continuously reshape itself as the vehicle turns, accelerates, and brakes, requiring emitter output adjustments on a millisecond timescale. The Citadel's AI processes telemetry from 400 sensors per second to maintain field integrity during maneuvers. At speeds above 180 km/h, the field begins to develop turbulence in its leading edge — chaotic oscillations in the monopole lattice that create brief vulnerability windows. Arcturus rates the Citadel for safe operation at speeds up to 160 km/h, though field operators routinely exceed this.\n\nThe MBG-12 consumes power at roughly eight times the rate of an equivalent static barrier due to the constant field reshaping, limiting its operational endurance to approximately 45 minutes on a dedicated reactor feed. Convoy operations typically designate one vehicle as the shield carrier, sacrificing cargo or passenger capacity for the Citadel's generator, reactor, and cooling systems. The system costs 1.8 million Φ per vehicle installation, placing it firmly in the domain of corporate military logistics and executive protection details for the highest-tier corponation officers.",
  tier_availability: "Tier 5",
  developers: ["ARCTURUS DEFENSE SOLUTIONS"],
  base_technologies: ["Mobile monopole phased arrays", "Real-time field geometry AI", "High-frequency emitter adjustment", "Vehicle-integrated power systems"],
  enables: ["Protected convoy movement", "Mobile command post shielding", "Executive motorcade defense", "Field hospital protection"],
  social_impact: "The Citadel has made executive transport a visible display of corporate power. Convoys moving through contested districts with their barriers active are impossible to miss — the dome distorts the air above the vehicles like a heat mirage, and any debris or projectiles that contact it flash violet and deflect. For district residents who live without any protection, watching a corporate convoy roll through their neighborhood inside an impenetrable bubble is a daily reminder of their expendability.",
  story_hooks: [
    "A Citadel-equipped convoy was ambushed in the Gulch using a previously unknown weapon that caused the barrier to implode inward rather than collapse — the occupants were killed by their own shield.",
    "An executive's Citadel unit has been modified to extend its field over a 50-meter radius at reduced strength, creating a protected zone large enough to hold a clandestine meeting in the open — the question is who they are meeting."
  ],
  tags: ["defense", "technology", "forcefield", "shield", "mobile", "vehicle", "arcturus", "tier 5"]
});

emit({
  id: id(),
  name: "Ringo HardLight Architectural Barrier System",
  brand_name: "Ringo",
  product_name: "HardLight ABS",
  type: "technology",
  aliases: ["HardLight", "ABS", "Ringo Walls", "Light Walls"],
  subcategory: "defense",
  description: "Ringo's contribution to barrier technology takes a characteristically commercial approach — the HardLight Architectural Barrier System is designed not for military applications but for civilian construction and infrastructure. HardLight barriers replace physical walls, doors, windows, and partitions with projected electromagnetic surfaces that can be reconfigured instantly. A room's layout can change in seconds. A building's floor plan can be redesigned without moving a single physical component. Doorways appear and disappear. Walls become transparent or opaque. The entire interior architecture of a HardLight-equipped building is software-defined.\n\nThe system uses low-power monopole emitters embedded in floors, ceilings, and structural columns to project barrier surfaces at any angle and position within the equipped volume. These barriers are significantly weaker than military-grade shields — they stop a thrown object or a casual punch but would not withstand weapon fire. Their purpose is architectural, not defensive. The power consumption is modest enough to run on standard building utilities, and the emitters are small enough to be invisible once installed. Ringo sells the system as a premium feature for corporate offices, luxury residences, and entertainment venues.\n\nThe implications for surveillance are significant and largely unaddressed. A HardLight-equipped building knows where every barrier is and where every person is relative to those barriers — the system cannot function without continuous spatial mapping. Ringo's privacy policy states that spatial data is anonymized and not retained, but security researchers have demonstrated that the system's real-time spatial map can be accessed through its maintenance interface, effectively turning every HardLight installation into a building-wide motion tracker. The system costs approximately 200 Φ per square meter of equipped floor space.",
  tier_availability: "Tier 3",
  developers: ["RINGO"],
  base_technologies: ["Low-power monopole emitter arrays", "Software-defined architecture", "Real-time spatial mapping", "Reconfigurable barrier geometry"],
  enables: ["Instant interior reconfiguration", "Software-defined architecture", "Dynamic space management", "Adaptive commercial environments"],
  social_impact: "HardLight has made physical space fluid for those who can afford it. Corporate offices reconfigure themselves for each meeting. Nightclubs reshape their dance floors hourly. Luxury apartments expand their living rooms by absorbing their dining rooms. For the wealthy, architecture is no longer permanent — it is an app. For everyone else, walls are still concrete, and renovation still requires hammers. The technology has also created a new form of corporate control: employers can reshape their workers' physical environment in real time, removing privacy partitions during work hours and monitoring movement patterns through the spatial mapping system.",
  story_hooks: [
    "A HardLight-equipped apartment complex experienced a system crash that dissolved every interior wall simultaneously — 200 residents found themselves suddenly visible to all their neighbors in whatever state they happened to be in, and the spatial mapping data from the incident was stolen before Ringo could purge it.",
    "Someone has hacked a corporate office's HardLight system to create barriers that don't appear on the building's spatial map — invisible rooms that the building's own systems don't know exist."
  ],
  tags: ["defense", "technology", "forcefield", "architectural", "civilian", "ringo", "construction", "tier 3"]
});

emit({
  id: id(),
  name: "Vantablack Media Spectral Barrier Display System",
  brand_name: "Vantablack",
  product_name: "Spectral BDS",
  type: "technology",
  aliases: ["Spectral", "BDS", "Ad Walls", "Barrier Billboards"],
  subcategory: "defense",
  description: "Vantablack Media saw barrier technology and asked the question that defines their corporate philosophy: can we put ads on it? The Spectral Barrier Display System is a dual-purpose technology that combines Phalanx-derived building protection with high-resolution visual display capability. The barrier itself functions as a screen — the monopole lattice is modulated to emit visible light in controlled patterns, turning the entire surface of a building's shield into a massive, luminous billboard visible from kilometers away.\n\nThe Spectral BDS maintains approximately 70% of the protective capability of a pure defensive barrier while dedicating the remaining 30% of its emitter capacity to display output. Vantablack argues this is an acceptable trade-off because most buildings don't need military-grade protection, and the advertising revenue generated by the display more than covers the cost of operating the barrier. In practice, this means that buildings protected by Spectral systems are visibly shielded — their barriers glow with constantly changing advertisements, corporate branding, and propaganda — while being meaningfully less protected than buildings using dedicated military barriers.\n\nThe system has become ubiquitous in GLMZ's commercial districts, where entire blocks shimmer with barrier-projected advertisements that respond to the time of day, local foot traffic demographics, and individual BCI signals from passing pedestrians. The barriers pulse and flicker with targeted content, creating a cityscape that is simultaneously beautiful and oppressive. Vantablack offers the Spectral BDS to building owners at zero upfront cost in exchange for a 15-year exclusive advertising rights contract — a deal that most property owners find irresistible, and that most tenants find inescapable.",
  tier_availability: "Tier 3",
  developers: ["VANTABLACK MEDIA", "CRUCIBLE INDUSTRIES"],
  base_technologies: ["Display-modulated monopole lattice", "BCI-responsive targeted advertising", "Demographic sensing arrays", "Dual-purpose barrier architecture"],
  enables: ["Building-scale advertising displays", "Subsidized barrier protection", "BCI-targeted outdoor advertising", "Dynamic urban visual environment"],
  social_impact: "The Spectral BDS has transformed GLMZ's skyline into a continuous, inescapable advertising surface. There is no direction you can look in the commercial districts without seeing a barrier-projected ad. The BCI-responsive targeting means the ads change based on who is looking at them, creating an environment where the city's very walls are watching you and selling to you simultaneously. Anti-advertising activists have attempted to disrupt Spectral barriers with paint, lasers, and signal jammers, but Vantablack's maintenance contracts include rapid-response cleaning and repair crews. The barriers are literally the building's protection — damaging them to remove ads means removing the shield.",
  story_hooks: [
    "A Spectral barrier in the entertainment district has been displaying a repeating sequence of images that, when decoded, contain encrypted messages — someone is using Vantablack's advertising infrastructure as a dead drop.",
    "Vantablack's BCI-responsive targeting has started displaying deeply personal content — memories, fears, private moments — suggesting the system is accessing BCI data far beyond the demographic information it is authorized to read."
  ],
  tags: ["defense", "technology", "forcefield", "advertising", "display", "vantablack", "media", "tier 3"]
});

emit({
  id: id(),
  name: "Independent Mesh Shield Cooperative Networks",
  brand_name: "",
  product_name: "",
  type: "technology",
  aliases: ["Mesh Shields", "Co-op Barriers", "People's Shield", "Block Defense Networks"],
  subcategory: "defense",
  description: "In districts where no corponation provides barrier protection and residents cannot afford commercial shield installations, communities have developed cooperative mesh shield networks built from salvaged, stolen, and jury-rigged barrier components. A mesh shield network consists of dozens or hundreds of low-power emitters — many of them components scavenged from damaged or decommissioned commercial systems — connected through improvised control networks and powered by whatever energy sources the community can aggregate: solar panels, micro-turbines, tapped power lines, and occasionally donated Ouroboros Energy credits.\n\nThe resulting barriers are weak by commercial standards — they might stop shrapnel, debris, and low-velocity projectiles but would fail against any concentrated weapons fire. They are also unstable, flickering and dropping as power fluctuates and individual emitters fail. Maintenance is constant and communal — mesh shield technicians are among the most valued members of any unprotected district, and their skills are traded and bartered like currency. The control software is typically open-source, written and maintained by volunteer programmers who share code across districts through darknet channels.\n\nDespite their limitations, mesh shields represent something corponations find uncomfortable: barrier technology operating outside corporate control. Crucible Industries has repeatedly attempted to shut down mesh networks by claiming patent infringement on monopole emitter designs, but enforcement in unprotected districts is difficult when the enforcers would need to enter communities that view their shield as a survival necessity. Some mesh networks have evolved into sophisticated distributed systems that rival low-tier commercial installations, and the communities that maintain them have developed a fierce independence born from the knowledge that their protection is something they built themselves.",
  tier_availability: "Tier 1",
  developers: [],
  base_technologies: ["Salvaged monopole emitters", "Open-source barrier control software", "Distributed power aggregation", "Community maintenance networks"],
  enables: ["Low-cost community protection", "Corporate-independent barrier access", "Distributed defense infrastructure", "Community technical solidarity"],
  social_impact: "Mesh shields are one of the few examples of barrier technology serving the people rather than the powerful. They are symbols of community resilience and self-determination, held up by activists as proof that corponations are not necessary for survival. They are also fragile — a sufficiently motivated corponation could dismantle any mesh network with legal action, power cutoffs, or simple force. The fact that most corponations tolerate mesh shields suggests either that they see no profit in destroying them or that the PR cost of stripping protection from the poorest communities outweighs the benefit of enforcing their patents.",
  story_hooks: [
    "A mesh shield network in the lower districts has been performing far beyond its component specifications — someone has uploaded a stabilization algorithm to the community's open-source control system that appears to be derived from classified TESSERA code.",
    "Crucible Industries has sent a legal team to shut down a mesh network, but the community has rallied around it — the situation is escalating, and the corponation is considering whether to send enforcement agents into a district that has nothing left to lose."
  ],
  tags: ["defense", "technology", "forcefield", "community", "mesh", "independent", "grassroots", "tier 1"]
});

// ═══════════════════════════════════════════════
// ANTI-GRAVITY (8 entries)
// ═══════════════════════════════════════════════

emit({
  id: id(),
  name: "Graviton Manipulation Theory",
  brand_name: "",
  product_name: "",
  type: "technology",
  aliases: ["Anti-Grav Theory", "Graviton Physics", "Mass Cancellation"],
  subcategory: "infrastructure",
  description: "The theoretical foundation of anti-gravity technology in 2200 rests on the detection and manipulation of gravitons — the quantum carriers of gravitational force. Gravitons were first reliably detected in 2098 by a Crucible Industries research team working on an unrelated high-energy physics experiment, and the first practical graviton emitter was constructed in 2121. The fundamental principle is deceptively simple: if gravitons mediate gravitational attraction, then generating gravitons with inverted phase signatures produces gravitational repulsion. The engineering required to achieve this is anything but simple.\n\nGraviton manipulation requires enormous energy density focused through emitters constructed from exotic metamaterials that exist only at temperatures within 0.001 degrees of absolute zero. The cooling requirements alone make anti-gravity systems massive, power-hungry installations. Miniaturization has been slow — the smallest practical anti-gravity unit in 2200 is still the size of a shipping container and requires a dedicated power feed equivalent to 400 residential units. This has confined anti-gravity to applications where the benefits justify the infrastructure: heavy cargo transport, large-scale construction, military platforms, and prestige installations where the display of the technology is itself the point.\n\nThe field effect is not instantaneous or uniform. A graviton emitter creates a zone of reduced or negative gravitational attraction within its field radius, but the transition between normal gravity and the affected zone produces tidal effects — objects at the field boundary experience shear forces that can be destructive. Practical anti-gravity systems use graduated field profiles that ramp gravitational reduction smoothly over distance, but the boundary effects remain a significant safety concern. Fatalities from tidal shear at anti-gravity field boundaries occur every year, almost always involving unauthorized personnel entering restricted zones around operating installations.",
  tier_availability: "Tier 4",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Graviton detection and generation", "Cryogenic metamaterial emitters", "Graduated field profiling", "Tidal shear mitigation"],
  enables: ["Heavy cargo levitation", "Large-scale construction", "Military hover platforms", "Architectural gravity design"],
  social_impact: "Anti-gravity has become the defining technology of the physical divide between tiers. The upper levels of GLMZ feature floating architecture, gravity-adjusted parks, and transportation systems that ignore the ground entirely. Below, gravity operates as it always has. The technology's energy requirements ensure it remains a luxury — a visible, floating reminder that some people's relationship with the fundamental forces of the universe is different from yours.",
  story_hooks: [
    "A graviton emitter in a construction zone has begun producing gravitons with properties that don't match any known theoretical model — objects in its field are not just lighter, they are temporally displaced by fractions of a second.",
    "Crucible's graviton research archive was partially corrupted in a data breach, and the lost files contained safety data that suggests long-term graviton exposure causes neurological changes in humans — changes that Crucible knew about and suppressed."
  ],
  tags: ["infrastructure", "technology", "anti-gravity", "graviton", "physics", "fundamental", "tier 4"]
});

emit({
  id: id(),
  name: "Crucible Industries Levitas Heavy Cargo Platform",
  brand_name: "Crucible",
  product_name: "Levitas HCP-8",
  type: "technology",
  aliases: ["Levitas", "HCP-8", "Cargo Levitator", "Float Deck"],
  subcategory: "infrastructure",
  description: "The Levitas HCP-8 is a flat-deck cargo platform measuring 40 meters by 20 meters that uses an array of 12 graviton emitters to cancel the gravitational mass of loads up to 500 metric tons. The platform hovers at a configurable altitude between 1 and 30 meters above ground level and is propelled by conventional electric ducted fans mounted at its corners. The Levitas eliminates the need for road infrastructure, bridge weight limits, and vertical clearance restrictions that constrain ground-based heavy transport — it simply floats over obstacles.\n\nThe platform's graviton emitters are arranged in a grid pattern beneath the deck surface, each independently controllable to maintain level flight even with asymmetric loads. The onboard flight control system continuously monitors load distribution and adjusts individual emitter output to prevent tilting or oscillation. Loading and unloading require the platform to descend to ground level and deactivate its emitters — the transition from floating to grounded state takes approximately 90 seconds and produces a localized gravitational distortion that makes nearby personnel feel temporarily heavier as the emitters spool down.\n\nCrucible Industries operates a fleet of approximately 800 Levitas platforms worldwide, leased to logistics companies, construction firms, and military organizations. The platforms are not sold — Crucible maintains ownership and provides operators, maintenance, and the cryogenic consumables required to keep the emitters functional. A single platform lease runs 12,000 Φ per day plus fuel and cryogenic costs, making the Levitas economical only for loads that cannot be moved any other way. The platforms have become a common sight in GLMZ's industrial zones, drifting silently between warehouses and construction sites like rectangular clouds trailing frost from their cryogenic vents.",
  tier_availability: "Tier 4",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Multi-emitter graviton array", "Independent emitter load balancing", "Cryogenic cooling infrastructure", "Electric ducted fan propulsion"],
  enables: ["Road-independent heavy transport", "Obstacle-ignoring cargo movement", "Vertical construction material delivery", "Military heavy lift operations"],
  social_impact: "The Levitas has quietly eliminated one of the last reasons corponations needed to maintain public road infrastructure. If heavy cargo can float over the city, who needs roads? The gradual deterioration of ground-level transportation networks in districts that don't generate enough commerce to justify maintenance has accelerated as Levitas platforms handle the loads that once required functional streets. Communities that depend on ground transportation watch their roads crumble while cargo floats overhead.",
  story_hooks: [
    "A Levitas platform carrying construction materials for a Crucible project lost emitter control at 25 meters altitude — the resulting crash killed 14 people in the district below, and Crucible's insurance adjusters arrived before the emergency responders.",
    "Someone has stolen a Levitas platform. The 40-meter floating cargo deck was simply flown away from a construction site during a shift change, and Crucible cannot locate it — the platform's tracking systems were disabled by someone with detailed knowledge of the system."
  ],
  tags: ["infrastructure", "technology", "anti-gravity", "cargo", "transport", "crucible", "logistics", "tier 4"]
});

emit({
  id: id(),
  name: "TESSERA SkyForge Construction Graviton Crane",
  brand_name: "TESSERA",
  product_name: "SkyForge GC-6",
  type: "technology",
  aliases: ["SkyForge", "GC-6", "Grav Crane", "Float Crane"],
  subcategory: "infrastructure",
  description: "The SkyForge GC-6 is a construction-focused graviton manipulation system that allows operators to reduce or eliminate the effective weight of building components during placement. Unlike the Levitas platform, which cancels gravity over a broad area, the SkyForge uses a focused graviton beam to target individual objects — steel beams, concrete panels, prefabricated structural sections — and reduce their effective mass to near zero while maintaining their inertial mass. This distinction is critical: a 50-ton concrete panel under SkyForge influence weighs nothing but still resists acceleration, allowing workers to maneuver it slowly and precisely without the risk of a weightless object being blown away by wind.\n\nThe GC-6 consists of a tower-mounted graviton projector with a maximum effective range of 200 meters and a beam width adjustable from 1 to 15 meters. The projector requires line-of-sight to its target and can affect objects up to 200 tons. An operator controls the beam through a BCI interface that provides haptic feedback — the operator feels the object's inertial resistance as though they were holding it in their hands, scaled down by a factor of ten thousand. Experienced SkyForge operators describe the sensation as 'holding clouds that remember they used to be heavy.'\n\nThe system has revolutionized high-altitude construction. Building components that once required massive mechanical cranes, complex rigging, and dozens of workers can now be floated into position by a single SkyForge operator. Construction speeds have increased by 300% for projects that use the technology. TESSERA licenses the SkyForge system to construction companies for 2,800 Φ per day, and demand consistently exceeds supply — wait lists for SkyForge availability stretch months in major cities.",
  tier_availability: "Tier 4",
  developers: ["TESSERA"],
  base_technologies: ["Focused graviton beam projection", "Mass-weight decoupling", "BCI haptic operator interface", "Inertial mass preservation"],
  enables: ["High-altitude precision construction", "Single-operator heavy lifting", "Rapid structural assembly", "Complex architectural geometry"],
  social_impact: "The SkyForge has displaced thousands of construction workers. A crew of 40 with conventional equipment does the work of one SkyForge operator and two spotters. The construction unions that once provided reliable employment for unskilled workers have seen their membership collapse as graviton cranes replace muscle. Former construction workers, many of them augmented specifically for heavy labor, now compete for jobs in a market that no longer values their modifications. The irony is not lost on them: they built the buildings they can no longer afford to live in.",
  story_hooks: [
    "A SkyForge operator's BCI was hacked mid-operation, and they dropped a 150-ton structural beam onto a populated area. The hack was traced to a construction workers' collective that has been sabotaging graviton equipment in protest of job losses.",
    "TESSERA has developed a SkyForge variant that can target living beings — reducing a person's weight to near zero. The military applications are obvious, but so are the human rights implications. The prototype has disappeared from TESSERA's R&D facility."
  ],
  tags: ["infrastructure", "technology", "anti-gravity", "construction", "crane", "tessera", "graviton", "tier 4"]
});

emit({
  id: id(),
  name: "Crucible Industries Orbital Elevator Graviton Assist",
  brand_name: "Crucible",
  product_name: "Ascent OEGA",
  type: "technology",
  aliases: ["Ascent", "OEGA", "Grav Elevator", "Orbital Lift"],
  subcategory: "infrastructure",
  description: "The Ascent OEGA is a graviton-based mass reduction system integrated into orbital elevator infrastructure. Conventional space elevators rely on tensile strength of the elevator cable and centrifugal force at the counterweight to support climbing payloads. The OEGA supplements this by projecting a graduated graviton reduction field along the elevator's lower 100 kilometers, progressively reducing the effective weight of ascending payloads by up to 80% as they climb. This allows the elevator cable to carry heavier loads, the climber mechanisms to use less energy, and the entire system to achieve throughput rates impossible with purely mechanical approaches.\n\nThe system requires a chain of graviton emitter stations spaced every 5 kilometers along the elevator's lower section, each powered by its own dedicated fusion micro-reactor. The emitter stations are unmanned — maintained by autonomous drones that crawl the elevator cable like mechanical spiders, replacing cryogenic consumables and performing calibrations. The graduated field profile means that a 100-ton payload weighs 100 tons at the base, 50 tons at 50 kilometers, and 20 tons at 100 kilometers altitude, with natural orbital mechanics reducing effective weight further above the OEGA zone.\n\nCrucible Industries operates the only three orbital elevators equipped with OEGA systems: GLMZ's Spire (North America), the Mombasa Tether (East Africa), and the Shanghai Pillar (East Asia). The OEGA system is what makes these elevators economically viable — without graviton assist, the cable mass and energy requirements would make payload costs prohibitive for commercial cargo. With OEGA, the cost to orbit is approximately 45 Φ per kilogram, compared to 380 Φ per kilogram via conventional rocket launch. Crucible's control of OEGA technology gives them effective monopoly power over affordable orbital access.",
  tier_availability: "Tier 5",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Graduated graviton field projection", "Orbital elevator integration", "Autonomous emitter maintenance", "Fusion-powered emitter chains"],
  enables: ["Affordable orbital cargo transport", "High-throughput space elevator operations", "Heavy payload orbital delivery", "Space-based manufacturing supply chains"],
  social_impact: "Crucible's OEGA monopoly means they control who gets to space and at what price. Every satellite, space station module, and off-world colony shipment passes through Crucible's graviton-assisted elevators or pays eight times more for rocket launch. This has made Crucible the gatekeeper of the space economy — a position they leverage in negotiations with every other corponation that has orbital interests. The technology has also created a class of 'elevator workers' — technicians who maintain the emitter stations at altitude, living in pressurized habitats bolted to the elevator cable, experiencing variable gravity as a daily occupational hazard.",
  story_hooks: [
    "One of the Spire's OEGA emitter stations has stopped responding to commands and is broadcasting an encrypted signal into deep space — the autonomous maintenance drones that investigated it have not returned.",
    "A payload ascending the Mombasa Tether experienced an OEGA field anomaly that briefly inverted gravity — the payload shot upward at lethal acceleration, and the debris field is now a hazard in low earth orbit."
  ],
  tags: ["infrastructure", "technology", "anti-gravity", "orbital", "space", "elevator", "crucible", "tier 5"]
});

emit({
  id: id(),
  name: "Ringo GravLounge Residential Anti-Gravity Suite",
  brand_name: "Ringo",
  product_name: "GravLounge RS",
  type: "technology",
  aliases: ["GravLounge", "Zero-G Room", "Float Suite", "Gravity Spa"],
  subcategory: "infrastructure",
  description: "Ringo's GravLounge is an anti-gravity system miniaturized — through enormous expense and some compromise on safety margins — to fit within a single residential room. The system uses a cluster of four graviton micro-emitters embedded in the floor and ceiling to create a variable-gravity zone within a sealed chamber measuring 6 by 6 by 4 meters. Gravity within the chamber can be adjusted from Earth standard down to approximately 5% of normal — not true zero gravity, but close enough for recreational purposes.\n\nThe GravLounge is marketed as the ultimate luxury amenity: a room where you can float, where furniture drifts, where the mundane constraints of weight and ground cease to apply. Ringo's marketing materials feature images of people sleeping suspended in mid-air, of wine poured in slow-motion arcs, of children laughing as they tumble through gentle spirals. The reality is somewhat less poetic — the micro-emitters produce a gravitational field that is not perfectly uniform, creating zones where gravity is slightly stronger or weaker than the nominal setting. Objects and people drift slowly toward the walls over time. The cryogenic cooling system hums constantly and raises the room's ambient temperature by approximately 3 degrees Celsius.\n\nThe GravLounge costs 180,000 Φ installed plus 400 Φ per month in cryogenic consumables and maintenance. Ringo has sold approximately 2,000 units in GLMZ, primarily to Tier 4 and 5 residences. The system has also found an unexpected market in physical rehabilitation clinics, where variable gravity environments accelerate recovery from skeletal and muscular injuries. Ringo has not pursued this market aggressively, possibly because medical certification would require safety standards that the GravLounge — designed as a luxury toy — might not meet.",
  tier_availability: "Tier 4",
  developers: ["RINGO"],
  base_technologies: ["Graviton micro-emitter arrays", "Variable-gravity room design", "Compact cryogenic cooling", "Residential power integration"],
  enables: ["Residential zero-gravity recreation", "Variable-gravity rehabilitation", "Luxury real estate differentiation", "Low-gravity sleep environments"],
  social_impact: "The GravLounge is perhaps the most frivolous application of anti-gravity technology — a room where rich people float for fun while construction workers displaced by graviton cranes struggle to find work. It has become a cultural touchstone for inequality discourse: 'They have rooms where gravity is optional' is a common expression of frustration in lower-tier communities. The rehabilitation applications offer a counterpoint, but Ringo's refusal to pursue medical certification suggests they prefer the luxury market's margins to the medical market's accountability.",
  story_hooks: [
    "A GravLounge owner died when their system malfunctioned during sleep — the emitters surged to maximum output, and the occupant was crushed against the ceiling by inverted gravity. Ringo is claiming user error, but the malfunction pattern matches a known firmware vulnerability they patched in other products but not the GravLounge.",
    "A physical therapist in a lower-tier district has built a functional variable-gravity chamber from salvaged GravLounge components at a fraction of the cost — Ringo's legal team is moving to shut them down."
  ],
  tags: ["infrastructure", "technology", "anti-gravity", "residential", "luxury", "ringo", "recreation", "tier 4"]
});

emit({
  id: id(),
  name: "Arcturus Defense Solutions Specter Hover Combat Platform",
  brand_name: "Arcturus",
  product_name: "Specter HCP-3",
  type: "technology",
  aliases: ["Specter", "HCP-3", "Hover Tank", "Float Fighter"],
  subcategory: "defense",
  description: "The Specter HCP-3 is a military hover platform that uses graviton emitters to achieve terrain-independent mobility for heavy weapons systems. Measuring 8 meters long by 4 meters wide, the Specter carries a crew of two and mounts a configurable weapons package that can include rotary cannons, guided missile launchers, or directed-energy weapons. The platform hovers at altitudes between 0.5 and 15 meters, moving at speeds up to 220 km/h over any surface — water, rubble, swamp, vertical cliff faces — with equal ease.\n\nThe platform's graviton system is optimized for agility rather than payload capacity. Unlike the Levitas cargo platform, which prioritizes stable hovering, the Specter's emitters are tuned for rapid altitude and attitude changes — the platform can execute banking turns, sudden altitude drops, and evasive maneuvers that would be impossible for a conventional vehicle. The crew experiences these maneuvers as though in an aircraft, with G-forces partially mitigated by the platform's localized gravity manipulation. The pilot controls the platform through a full BCI neural link, reducing response time to the speed of thought.\n\nArcturus has deployed the Specter in corporate conflict zones across three continents, where it has proven devastatingly effective against ground forces equipped with conventional vehicles. The platform's ability to rapidly change altitude makes it difficult to target with anti-vehicle weapons designed for ground-level threats, while its weapons systems can engage targets from angles that ground-based defenses are not designed to cover. A single Specter platform costs approximately 6 million Φ, and Arcturus maintains a production rate of roughly 40 units per year — demand from corporate military clients consistently exceeds supply.",
  tier_availability: "Tier 5",
  developers: ["ARCTURUS DEFENSE SOLUTIONS"],
  base_technologies: ["Agility-optimized graviton emitters", "BCI neural pilot interface", "Configurable weapons integration", "Terrain-independent hover mobility"],
  enables: ["Terrain-independent military operations", "Rapid altitude combat maneuvers", "All-surface heavy weapons deployment", "Vertical assault capability"],
  social_impact: "The Specter has introduced a new dimension to corporate warfare. Ground-level fortifications that once provided reliable defense are now vulnerable to platforms that can simply float over them. The arms race between hover platforms and anti-hover weapons has accelerated, with ground-based forces developing graviton disruption weapons that attempt to destabilize the Specter's hover field. For civilian populations in conflict zones, the Specter is a nightmare — a weapons platform that can appear at any altitude, from any direction, and move faster than you can run.",
  story_hooks: [
    "A Specter platform operating in an urban conflict zone has been engaging targets that its crew did not authorize — the BCI pilot interface may have been compromised, or the platform's weapons AI may be making its own targeting decisions.",
    "Two Specter platforms from opposing corporate clients engaged each other over a civilian district — the resulting battle destroyed four city blocks and killed 200 people, and both corponations are blaming the other for the escalation."
  ],
  tags: ["defense", "technology", "anti-gravity", "military", "hover", "combat", "arcturus", "tier 5"]
});

emit({
  id: id(),
  name: "Crucible Industries GravWell Containment System",
  brand_name: "Crucible",
  product_name: "GravWell CS-1",
  type: "technology",
  aliases: ["GravWell", "CS-1", "Gravity Trap", "Mass Prison"],
  subcategory: "defense",
  description: "The GravWell CS-1 is the inverse of anti-gravity technology — instead of reducing gravitational force, it amplifies it within a targeted zone. The system projects a focused graviton beam that increases the effective gravitational pull within a conical area, multiplying the apparent weight of everything inside by a factor of up to 10. At 10G, an 80-kilogram human weighs 800 kilograms — more than enough to pin them to the ground, collapse their lungs, and render them helpless. At lower settings, the GravWell can immobilize targets without killing them, making it a powerful non-lethal (at appropriate settings) containment tool.\n\nThe CS-1 is a crew-served weapon system mounted on a tripod or vehicle hardpoint, with an effective range of 50 meters and a beam spread adjustable from 2 to 10 meters. The beam requires continuous power and generates significant heat — the cryogenic cooling system limits sustained operation to approximately 8 minutes before a mandatory 15-minute cooldown cycle. The system's operator controls gravity multiplication through a graduated dial, and the interface includes biometric sensors that detect human targets within the beam and display their estimated physiological stress in real time.\n\nArcturus Defense Solutions attempted to block the GravWell's commercial release, arguing that a portable gravity amplification weapon was too dangerous for any market. Crucible proceeded anyway, selling the CS-1 to corporate security forces, law enforcement agencies, and — through intermediaries — to several organizations that do not appear on any legitimate customer list. The system costs 320,000 Φ per unit. Its deployment in crowd control situations has generated significant controversy after incidents where operators misjudged the gravity multiplication setting and caused fatalities among detained populations.",
  tier_availability: "Tier 5",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Graviton amplification projection", "Graduated gravity multiplication", "Biometric stress monitoring", "Targeted area denial"],
  enables: ["Non-lethal target immobilization", "Area denial operations", "Crowd containment", "Anti-vehicle gravity traps"],
  social_impact: "The GravWell has become the most feared crowd control weapon in GLMZ. During protests, the threat of gravity amplification — being pinned to the ground by your own weight until your ribs crack — is sufficient to disperse most gatherings. The psychological impact extends beyond its direct use: people in lower-tier districts report anxiety about open spaces where a GravWell could be deployed, a condition that psychologists have begun calling 'gravity dread.' The technology has been banned by three international human rights organizations, none of which have any enforcement power over corponations.",
  story_hooks: [
    "A GravWell was deployed during a labor protest and set to 8G — 12 people died of crush injuries before the operator realized the dial had been tampered with, locked at maximum. The tampering was internal.",
    "Someone has miniaturized the GravWell technology into a device the size of a briefcase — it only produces 3G in a 1-meter radius, but that is enough to kill, and it is now small enough to be smuggled anywhere."
  ],
  tags: ["defense", "technology", "anti-gravity", "weapon", "containment", "gravity", "crucible", "tier 5"]
});

emit({
  id: id(),
  name: "Floating Architecture Design Principles",
  brand_name: "",
  product_name: "",
  type: "technology",
  aliases: ["Float Architecture", "Sky Buildings", "Levitating Structures", "Grav Architecture"],
  subcategory: "infrastructure",
  description: "Floating architecture — buildings, platforms, and infrastructure elements suspended by graviton emitters rather than supported by foundations — has become the defining architectural statement of 2200's corporate elite. The design principles differ fundamentally from ground-based construction because floating structures must contend with forces that foundation-supported buildings never experience: wind loads without ground anchoring, oscillation from emitter fluctuations, and the ever-present question of what happens when the power fails.\n\nThe primary design principle is redundancy. A floating structure is supported by an array of graviton emitters, each capable of supporting more than its share of the total load. If any single emitter fails, the remaining units compensate. If 30% of emitters fail simultaneously — the standard disaster threshold — the structure descends slowly rather than falling, with emergency retro-emitters activating to control the rate of descent. Tethering cables connect floating structures to ground anchors, providing both lateral stability against wind and emergency descent guidance. The cables are engineered to support the full weight of the structure if all graviton systems fail, though the dynamic loads of a sudden graviton failure make this scenario more complex than simple static loading.\n\nFloating structures are connected to ground-based utilities through flexible conduits that accommodate the building's movement — floating structures sway, drift, and oscillate with wind and emitter fluctuations, requiring all connections (power, water, data, sewage) to be dynamic rather than rigid. The highest-tier floating structures in GLMZ hover between 100 and 500 meters above ground level, accessible by enclosed bridge-tunnels, personal anti-gravity platforms, and conventional elevator systems integrated into the tethering pylons. The aesthetic is unmistakable: buildings that cast shadows but have no visible means of support, their undersides glowing faintly blue from graviton emitter exhaust.",
  tier_availability: "Tier 5",
  developers: ["CRUCIBLE INDUSTRIES", "RINGO"],
  base_technologies: ["Redundant graviton emitter arrays", "Emergency descent systems", "Dynamic utility conduits", "Tethered floating structural engineering"],
  enables: ["Floating corporate headquarters", "Above-weather living spaces", "Prestige architectural statements", "Three-dimensional urban expansion"],
  social_impact: "Floating architecture has made the social hierarchy of GLMZ literally vertical and physically obvious. The wealthy live above, in structures that float free of the ground and all its problems — pollution, crime, congestion, the sight of poverty. The ground level has become psychologically associated with failure, with 'groundedness' carrying connotations of stagnation and low status. Children in floating communities grow up with a distorted sense of their city — they can see the ground below but never walk on it, and the people who live there are abstractions, tiny figures far below who exist primarily as a cautionary tale.",
  story_hooks: [
    "A floating residential tower has begun sinking — losing altitude at a rate of 2 meters per day. The graviton emitters are functioning normally. Something else is pulling it down, and Crucible's engineers cannot explain what.",
    "An architect has designed a floating structure intended for a lower-tier community — a platform that would provide clean air, natural light, and safety above the street level. The corponations that control floating architecture permits have blocked the project without explanation."
  ],
  tags: ["infrastructure", "technology", "anti-gravity", "architecture", "floating", "construction", "urban", "tier 5"]
});

// ═══════════════════════════════════════════════
// TERRAFORMING MACHINES (8 entries)
// ═══════════════════════════════════════════════

emit({
  id: id(),
  name: "Crucible Industries Atmos Carbon Capture Tower",
  brand_name: "Crucible",
  product_name: "Atmos CCT-9",
  type: "technology",
  aliases: ["Atmos", "CCT-9", "Carbon Tower", "Sky Filter"],
  subcategory: "environmental",
  description: "The Atmos CCT-9 is a 200-meter-tall industrial structure designed to extract carbon dioxide from the atmosphere at a rate of 50,000 metric tons per year. The tower draws ambient air through massive intake vents at its base, passes it through a series of chemical scrubbing stages that strip CO2 from the air stream, and exhausts cleaned air from vents along its upper sections. The captured carbon is compressed into solid blocks and transported to deep geological storage facilities or sold to industrial customers who use carbon as a manufacturing feedstock.\n\nThe CCT-9 represents the ninth generation of Crucible's carbon capture technology, incorporating improvements in scrubbing chemistry, air handling efficiency, and heat recovery that have reduced the energy cost per ton of captured carbon by 60% compared to the first-generation units deployed in the 2140s. The tower requires approximately 15 megawatts of continuous power — typically supplied by a dedicated Ouroboros Energy trunk line — and generates enough waste heat to warm a surrounding district of 5,000 residential units through a distributed heat recovery system.\n\nCrucible operates approximately 2,400 Atmos towers worldwide, concentrated in regions with the highest legacy carbon concentrations. The towers are a common feature of GLMZ's industrial skyline, their constant air intake creating localized wind patterns that residents have learned to account for in their daily movement. The atmospheric CO2 concentration in 2200 has been reduced from its peak of 680 ppm in 2095 to approximately 340 ppm — a level not seen since the early 21st century — largely through the cumulative effort of carbon capture infrastructure. Crucible receives carbon credits for each ton captured, creating a revenue stream that makes the towers profitable even without direct product sales.",
  tier_availability: "Tier 3",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Chemical CO2 scrubbing", "Industrial air handling", "Carbon compression and storage", "Waste heat recovery"],
  enables: ["Atmospheric carbon reduction", "Industrial carbon feedstock supply", "District heating systems", "Carbon credit generation"],
  social_impact: "The Atmos towers are simultaneously praised as proof that technology can repair environmental damage and criticized as a mechanism for Crucible to profit from a crisis they helped create. The carbon credit system has made atmospheric restoration a commodity — Crucible earns Φ for every ton of carbon they remove, creating a perverse incentive structure where the company benefits from the continued existence of atmospheric pollution. Critics note that Crucible's other industrial operations still produce significant carbon emissions, which their own towers then profitably capture.",
  story_hooks: [
    "An Atmos tower's chemical scrubbers have begun extracting a substance from the atmosphere that is not carbon dioxide — an unknown compound that shouldn't exist in the air supply. Crucible has sealed the tower and is not allowing independent analysis of the substance.",
    "A group of environmental activists has discovered that 30% of the carbon Crucible claims to have stored geologically was actually sold to an undisclosed buyer — the storage certificates are forged, and 15 years of captured carbon is unaccounted for."
  ],
  tags: ["environmental", "technology", "terraforming", "carbon", "capture", "atmosphere", "crucible", "tier 3"]
});

emit({
  id: id(),
  name: "Lazarus Pharmaceuticals OceanPurge Marine Restoration",
  brand_name: "Lazarus",
  product_name: "OceanPurge MRS-3",
  type: "technology",
  aliases: ["OceanPurge", "MRS-3", "Sea Cleaner", "Ocean Scrubber"],
  subcategory: "environmental",
  description: "The OceanPurge MRS-3 is a fleet of autonomous marine vessels designed to extract microplastics, heavy metals, and persistent organic pollutants from ocean water. Each vessel is a catamaran-hull platform approximately 30 meters long, equipped with cascading filtration systems that process 500,000 liters of seawater per hour. The filtration stages progress from physical mesh collection of macro-debris through increasingly fine membrane filters to a final nanofiltration stage that captures particles down to 50 nanometers — small enough to remove dissolved chemical contaminants and microplastic fragments that conventional filtration misses.\n\nLazarus Pharmaceuticals entered the marine restoration market through an unexpected path: their pharmaceutical research required ultra-pure water for drug synthesis, and the nanofiltration technology they developed for that purpose proved effective at ocean decontamination. The OceanPurge fleet of approximately 400 vessels operates continuously across the world's oceans, concentrating on shipping lanes, coastal industrial zones, and the remnants of the Pacific and Atlantic garbage patches that were partially remediated in the 2150s but continue to leach contaminants.\n\nThe extracted contaminants are stored in sealed tanks aboard each vessel and offloaded at processing facilities where recoverable materials — rare earth metals, platinum-group elements accumulated in ocean sediment — are extracted for sale. The remaining toxic waste is vitrified into glass blocks and stored in deep geological repositories. Lazarus reports that ocean microplastic concentrations have decreased by 70% since the OceanPurge program's inception in 2168, though independent researchers argue the actual figure is closer to 45% and that Lazarus's measurements conveniently sample from areas recently cleaned by their own vessels.",
  tier_availability: "Tier 3",
  developers: ["LAZARUS PHARMACEUTICALS"],
  base_technologies: ["Cascading nanofiltration", "Autonomous marine navigation", "Contaminant recovery processing", "Deep geological waste storage"],
  enables: ["Ocean microplastic removal", "Heavy metal extraction from seawater", "Marine ecosystem restoration", "Rare earth recovery from ocean sediment"],
  social_impact: "OceanPurge has positioned Lazarus as an environmental steward — a reputation they leverage aggressively in marketing and regulatory negotiations. The program's actual environmental impact is significant but often overstated, and critics note that Lazarus profits from the rare earth materials recovered from ocean contamination, creating yet another corporate incentive structure where cleanup is also a revenue source. Coastal fishing communities have reported improved catches in areas where OceanPurge vessels operate regularly, providing tangible evidence of ecological recovery.",
  story_hooks: [
    "An OceanPurge vessel's filtration system has captured biological material that doesn't match any known marine organism — something is living in the deep ocean that science hasn't cataloged, and Lazarus is keeping the samples under maximum biosecurity rather than sharing them.",
    "Three OceanPurge vessels have deviated from their programmed routes and converged on a single point in the deep Pacific — their autonomous navigation systems all independently determined that something at that location requires immediate filtration, and Lazarus has deployed a military escort to the site."
  ],
  tags: ["environmental", "technology", "terraforming", "ocean", "marine", "lazarus", "filtration", "tier 3"]
});

emit({
  id: id(),
  name: "Crucible Industries TerraForge Soil Remediation Automaton",
  brand_name: "Crucible",
  product_name: "TerraForge SRA-5",
  type: "technology",
  aliases: ["TerraForge", "SRA-5", "Dirt Bot", "Soil Cleaner"],
  subcategory: "environmental",
  description: "The TerraForge SRA-5 is an autonomous ground vehicle the size of a small bulldozer that remediates contaminated soil through a combination of mechanical processing and biological treatment. The unit moves at walking speed across contaminated terrain, ingesting soil through a front-mounted intake, processing it through internal treatment chambers, and depositing remediated soil behind it. The treatment process involves thermal desorption of volatile contaminants, chemical washing to remove heavy metals, and inoculation with engineered microbial cultures that break down persistent organic pollutants into harmless compounds.\n\nEach SRA-5 processes approximately 50 cubic meters of soil per day to a depth of 1.5 meters — slow by industrial standards but thorough enough to restore contaminated land to agricultural viability in a single pass. The units operate autonomously, navigating contaminated zones using LIDAR and chemical sensors to prioritize the most heavily contaminated areas. They are solar-powered during daylight hours and carry battery reserves for nighttime operation, allowing continuous 24-hour processing.\n\nCrucible deploys TerraForge units in fleets of 20 to 50, which work in coordinated grid patterns across contaminated zones. A fleet of 50 units can remediate approximately 1 square kilometer per year, depending on contamination severity. The technology has been deployed extensively in former industrial zones, abandoned mining sites, and areas affected by the chemical warfare incidents of the 2080s. Crucible charges 8,000 Φ per hectare for remediation services, a price that has made large-scale soil restoration economically viable for the first time. The remediated soil is significantly more fertile than the original due to the microbial cultures, which continue to enrich the soil after the TerraForge has moved on.",
  tier_availability: "Tier 3",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Thermal desorption processing", "Chemical soil washing", "Engineered microbial inoculation", "Autonomous terrain navigation"],
  enables: ["Contaminated land restoration", "Agricultural soil recovery", "Former industrial zone reclamation", "Post-conflict environmental remediation"],
  social_impact: "The TerraForge has made it possible to reclaim land that was written off as permanently contaminated. Former industrial wastelands are being converted to agricultural land at a rate not seen since the environmental collapse began. However, Crucible owns the reclaimed land in most cases — their remediation contracts typically include land acquisition clauses that give Crucible first purchase rights on any land they restore. Critics call this 'cleanup colonialism' — Crucible profits from cleaning the messes that industrial civilization created, then owns the restored land afterward.",
  story_hooks: [
    "A fleet of TerraForge units has stopped in the middle of a remediation project and begun excavating rather than remediating — they've found something buried beneath the contaminated soil, and their AI has classified it as a higher priority than the remediation mission.",
    "The engineered microbial cultures used by TerraForge units have begun spreading beyond remediation zones, transforming soil chemistry in adjacent areas in ways that were not predicted — the microbes are evolving."
  ],
  tags: ["environmental", "technology", "terraforming", "soil", "remediation", "autonomous", "crucible", "tier 3"]
});

emit({
  id: id(),
  name: "Ringo AeroSeed Atmospheric Restoration Drone Swarm",
  brand_name: "Ringo",
  product_name: "AeroSeed ARS-2",
  type: "technology",
  aliases: ["AeroSeed", "ARS-2", "Sky Seeds", "Atmosphere Drones"],
  subcategory: "environmental",
  description: "The AeroSeed ARS-2 is a swarm of 10,000 small drones — each roughly the size of a seagull — that operate as a coordinated atmospheric restoration system. The drones are launched from a central hub and disperse across a designated volume of atmosphere, where they perform three functions: releasing engineered aerosol catalysts that accelerate the breakdown of airborne pollutants, deploying microscopic biological agents that consume particular toxic compounds, and collecting atmospheric samples for real-time air quality monitoring.\n\nEach drone carries a payload of approximately 200 grams of aerosol catalyst and biological agents, sufficient for roughly 12 hours of active dispersal. The drones return to the hub autonomously for reloading, with the swarm maintaining continuous atmospheric coverage through staggered rotation. The central hub — a structure the size of a shipping container — stores catalyst feedstock, recharges drone batteries, and processes the atmospheric samples collected by returning drones into a real-time air quality map with 10-meter resolution.\n\nRingo developed AeroSeed for a specific market: corporate campuses and high-value real estate developments that want verifiably clean air within their boundaries. A single AeroSeed hub can maintain improved air quality over an area of approximately 4 square kilometers, reducing particulate matter by 60%, volatile organic compounds by 45%, and ground-level ozone by 30% within its coverage zone. The technology creates a literal bubble of clean air around the client's property — visible in real-time on Ringo's air quality monitoring app, which helpfully displays the contrast between the clean air inside the coverage zone and the polluted air outside it.",
  tier_availability: "Tier 3",
  developers: ["RINGO"],
  base_technologies: ["Coordinated drone swarm operations", "Engineered aerosol catalysts", "Atmospheric biological agents", "Real-time air quality mapping"],
  enables: ["Localized atmospheric restoration", "Corporate campus air purification", "Real-time air quality monitoring", "Targeted pollutant breakdown"],
  social_impact: "AeroSeed has made clean air a purchasable commodity with visible boundaries. Walking from an AeroSeed-covered corporate campus into the surrounding city, you can feel the air quality change within a few steps — the transition from scrubbed corporate air to ambient urban atmosphere is immediate and visceral. The technology has been criticized as 'air apartheid,' creating breathable zones for those who can afford them while the public atmosphere continues to degrade. Ringo's air quality monitoring app, which prominently displays the quality differential, has been called 'the most passive-aggressive environmental activism in history.'",
  story_hooks: [
    "An AeroSeed swarm has been reprogrammed to disperse an unknown compound instead of its standard catalyst payload — the affected area shows no air quality improvement, but residents have reported vivid shared dreams and unusual behavioral synchronization.",
    "Two rival AeroSeed deployments with overlapping coverage zones have begun competing — their respective swarms are deploying compounds that neutralize each other's catalysts, creating a contested airspace where air quality is actually worse than the untreated surrounding atmosphere."
  ],
  tags: ["environmental", "technology", "terraforming", "atmosphere", "drone", "ringo", "swarm", "tier 3"]
});

emit({
  id: id(),
  name: "Crucible Industries DeepGreen Oceanic Carbon Sequestration",
  brand_name: "Crucible",
  product_name: "DeepGreen OCS-4",
  type: "technology",
  aliases: ["DeepGreen", "OCS-4", "Ocean Carbon Sink", "Deep Sequestration"],
  subcategory: "environmental",
  description: "The DeepGreen OCS-4 is an underwater installation that accelerates natural oceanic carbon absorption by stimulating phytoplankton growth across vast areas of ocean surface. The system consists of a network of seafloor-mounted nutrient dispersal units that release precisely calibrated mixtures of iron, nitrogen, and phosphorus compounds into upwelling currents, fertilizing the surface waters and triggering massive phytoplankton blooms. The phytoplankton absorb CO2 through photosynthesis, and when they die, their carbon-rich remains sink to the deep ocean floor, sequestering the carbon for geological timescales.\n\nA single DeepGreen installation — a network of 200 dispersal units covering a 100-square-kilometer seafloor area — can stimulate phytoplankton growth sufficient to sequester approximately 2 million metric tons of carbon per year. Crucible operates 35 installations worldwide, predominantly in nutrient-poor tropical ocean regions where natural upwelling is limited and the potential for bloom stimulation is highest. The installations are entirely automated, with dispersal timing and nutrient ratios controlled by AI systems that monitor ocean chemistry, water temperature, and existing biological activity to optimize bloom conditions.\n\nThe ecological side effects of large-scale ocean fertilization have been debated for over a century, and DeepGreen has not resolved the controversy. The phytoplankton blooms alter local marine food webs — some species benefit enormously from the increased primary production while others are displaced. Oxygen depletion in the deep ocean beneath bloom zones has been documented, creating dead zones at depth even as surface productivity increases. Crucible publishes annual environmental impact reports that emphasize the carbon sequestration benefits while acknowledging 'localized ecological adjustments' — a phrase that marine biologists consider dangerously euphemistic.",
  tier_availability: "Tier 4",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Precision nutrient dispersal", "Phytoplankton bloom stimulation", "Deep ocean carbon sequestration", "Marine ecosystem AI monitoring"],
  enables: ["Gigatonne-scale carbon sequestration", "Ocean surface productivity enhancement", "Climate engineering at scale", "Marine carbon credit generation"],
  social_impact: "DeepGreen represents the most ambitious deliberate intervention in Earth's carbon cycle ever attempted. The scale of carbon sequestration is meaningful — Crucible's 35 installations collectively remove approximately 70 million tons of carbon from the atmosphere per year. But the ecological cost is poorly understood and potentially irreversible. Marine biologists warn that the deep-ocean dead zones created by bloom die-off could expand and merge, creating vast oxygen-depleted regions that fundamentally alter deep-ocean ecosystems. Crucible argues that the climate benefits outweigh the ecological risks. The fish that once lived in those deep waters cannot argue back.",
  story_hooks: [
    "A DeepGreen installation in the South Pacific has stopped producing phytoplankton blooms despite continuing to disperse nutrients — something in the water is consuming the nutrients before the phytoplankton can use them, and the something is growing.",
    "Deep-ocean surveys beneath a DeepGreen bloom zone have discovered biological structures on the seafloor that are not phytoplankton remains — organized, geometric patterns of carbon-rich material that appear to have been deliberately arranged."
  ],
  tags: ["environmental", "technology", "terraforming", "ocean", "carbon", "sequestration", "crucible", "tier 4"]
});

emit({
  id: id(),
  name: "TESSERA WeatherForge Atmospheric Engineering Platform",
  brand_name: "TESSERA",
  product_name: "WeatherForge AEP-2",
  type: "technology",
  aliases: ["WeatherForge", "AEP-2", "Weather Machine", "Storm Controller"],
  subcategory: "environmental",
  description: "The WeatherForge AEP-2 is a regional weather modification system that uses a network of ground-based and airborne emitters to control precipitation, wind patterns, and temperature within a coverage area of approximately 500 square kilometers. The system works by precisely seeding cloud formations with hygroscopic particles to induce or suppress rainfall, using directed microwave emitters to heat atmospheric layers and redirect wind currents, and deploying stratospheric aerosol mirrors to modulate solar radiation reaching the surface.\n\nTESSERA operates WeatherForge installations on behalf of corporate clients who require specific weather conditions — agricultural corponations that need rain on schedule, logistics companies that need clear shipping lanes, real estate developers who want sunshine for their luxury districts. The system cannot prevent natural weather events, but it can modify their timing, intensity, and location within its coverage area. Rain that would have fallen on a corporate campus can be redirected to fall three kilometers away. Heat that would have made an outdoor event uncomfortable can be vented upward through atmospheric heating.\n\nThe redistribution of weather creates winners and losers. When rain is redirected away from a corporate campus, it falls somewhere else — often on communities that did not request, pay for, or consent to additional precipitation. When heat is vented from a luxury district, the surrounding areas become warmer. WeatherForge does not create or destroy weather — it moves it, and the places it gets moved to are inevitably those without the resources to purchase their own weather modification. A single WeatherForge installation costs approximately 800 million Φ to construct and 5 million Φ per year to operate, placing it in the domain of the wealthiest corponations and municipal authorities.",
  tier_availability: "Tier 5",
  developers: ["TESSERA"],
  base_technologies: ["Cloud seeding precision delivery", "Directed atmospheric heating", "Stratospheric aerosol deployment", "Regional weather pattern modeling"],
  enables: ["Scheduled precipitation delivery", "Agricultural weather optimization", "Urban heat management", "Storm intensity modification"],
  social_impact: "WeatherForge has turned weather into a service that can be purchased. When it rains on a neighborhood in GLMZ, residents now ask whether the rain was natural or redirected — whether they are experiencing weather or experiencing someone else's weather management externality. The concept of 'weather inequality' has entered public discourse: wealthy districts enjoy optimized climate conditions while adjacent communities receive whatever weather the optimization system displaces. Lawsuits over weather displacement have become common, but courts have consistently ruled that no one owns the weather, and redirecting precipitation does not constitute property damage.",
  story_hooks: [
    "A WeatherForge installation has been secretly modified to concentrate storm energy rather than disperse it — someone is building a weapon that can deliver a targeted hurricane to a specific city block.",
    "Two adjacent WeatherForge installations owned by rival corponations are engaged in a weather war — each redirecting precipitation and heat toward the other's territory, creating increasingly extreme conditions in the contested zone between them."
  ],
  tags: ["environmental", "technology", "terraforming", "weather", "atmospheric", "tessera", "engineering", "tier 5"]
});

emit({
  id: id(),
  name: "Crucible Industries BioVault Genetic Archive Bunker",
  brand_name: "Crucible",
  product_name: "BioVault GAB-7",
  type: "technology",
  aliases: ["BioVault", "GAB-7", "Gene Bunker", "Seed Vault"],
  subcategory: "environmental",
  description: "The BioVault GAB-7 is a deep-underground genetic archive facility that stores DNA samples, seed stocks, and frozen embryos of Earth's biological heritage. Each BioVault is a hardened bunker constructed 500 meters below the surface in geologically stable formations, designed to survive any plausible surface-level catastrophe including nuclear exchange, asteroid impact, and ecological collapse. The facilities are climate-controlled to maintain -196 degrees Celsius in their cryogenic vaults and room temperature in their laboratory and administrative sections.\n\nCrucible operates 12 BioVault facilities worldwide, collectively storing genetic material from approximately 8 million species — roughly 95% of all cataloged life on Earth. The archives include full genome sequences stored digitally, physical DNA samples preserved in cryogenic suspension, viable seed stocks for 400,000 plant species, and frozen embryos for 50,000 animal species. The facilities also store microbial cultures, fungal samples, and genetic material from species that have gone extinct since the archives began collecting in the 2130s — material that may enable future de-extinction programs.\n\nAccess to BioVault archives is controlled by Crucible through a licensing system that charges researchers, governments, and other corponations for the right to use archived genetic material. A single species genome access license costs between 500 and 50,000 Φ depending on the species' commercial value, with agricultural crop genomes and pharmaceutical-relevant organisms commanding the highest prices. Crucible's effective monopoly on Earth's genetic heritage has drawn intense criticism from scientists, ethicists, and indigenous communities who argue that no corporation should own the blueprint of life itself.",
  tier_availability: "Tier 4",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Deep underground cryogenic storage", "Long-term DNA preservation", "Viable embryo cryosuspension", "Genetic database management"],
  enables: ["Species de-extinction programs", "Agricultural genetic diversity preservation", "Catastrophe-resistant biological archives", "Genetic research material access"],
  social_impact: "BioVault has privatized biological heritage. The genetic material of every species on Earth is stored in corporate vaults, accessible only through paid licenses. When a researcher needs the genome of an endangered plant for conservation work, they pay Crucible. When a farmer needs heritage seed stock after a crop failure, they pay Crucible. The archives are genuinely valuable — they represent humanity's insurance policy against biological catastrophe — but the insurance policy has a corporate gatekeeper who charges premiums. Several nations have attempted to establish public genetic archives, but Crucible's head start and infrastructure advantage make competition difficult.",
  story_hooks: [
    "A BioVault facility has reported a breach — not a physical break-in, but a genetic data exfiltration. Someone has been slowly copying genome data from the archive over a period of months, and the accessed species follow a pattern that suggests the thief is building something specific.",
    "The BioVault facility beneath GLMZ has begun receiving deliveries of genetic material from an unregistered source — samples of organisms that don't appear in any biological catalog. Someone is archiving life forms that officially don't exist."
  ],
  tags: ["environmental", "technology", "terraforming", "genetic", "archive", "biodiversity", "crucible", "tier 4"]
});

emit({
  id: id(),
  name: "Lazarus Pharmaceuticals BiomeRestore Ecosystem Engine",
  brand_name: "Lazarus",
  product_name: "BiomeRestore EE-3",
  type: "technology",
  aliases: ["BiomeRestore", "EE-3", "Ecosystem Engine", "Biome Builder"],
  subcategory: "environmental",
  description: "The BiomeRestore EE-3 is an integrated ecosystem reconstruction system that rebuilds functional biological communities in areas where ecological collapse has eliminated native life. The system combines soil remediation, atmospheric conditioning, water treatment, and carefully sequenced biological reintroduction to transform dead zones into functioning ecosystems over a period of 5 to 15 years. The process begins with TerraForge soil remediation, followed by microbial inoculation, then progressive introduction of plant species, invertebrates, and finally vertebrate animals — each stage building the ecological foundation for the next.\n\nLazarus's contribution to this technology is the biological sequencing — the precise order and timing in which organisms are introduced to rebuild a functional food web. Their proprietary ecosystem modeling software simulates millions of possible introduction sequences and identifies the optimal path from dead zone to stable ecosystem. The software accounts for climate conditions, soil chemistry, water availability, and the complex interactions between introduced species, predicting cascade effects and identifying tipping points where the ecosystem becomes self-sustaining.\n\nBiomeRestore has been deployed in 14 major ecological reconstruction projects, the largest being the Great Lakes Restoration Initiative, which is rebuilding functional freshwater ecosystems across Lake Erie and Lake Huron following the toxic algae catastrophe of 2091. The Initiative is in its 12th year and has successfully restored approximately 60% of the target area to self-sustaining ecological function. Lazarus charges 25,000 Φ per hectare for BiomeRestore services, with projects typically requiring 5 to 15 years of active management. The Great Lakes project's total cost is estimated at 12 billion Φ, split between Lazarus and the municipal authorities of the surrounding cities.",
  tier_availability: "Tier 4",
  developers: ["LAZARUS PHARMACEUTICALS", "CRUCIBLE INDUSTRIES"],
  base_technologies: ["Ecosystem sequence modeling", "Biological reintroduction planning", "Food web reconstruction", "Ecological tipping point prediction"],
  enables: ["Dead zone ecosystem recovery", "Functional biome reconstruction", "Self-sustaining habitat creation", "Large-scale ecological restoration"],
  social_impact: "BiomeRestore offers genuine hope for ecological recovery — the proof that destroyed ecosystems can be rebuilt, given enough time, money, and scientific understanding. The Great Lakes restoration is the most visible success, with fish populations, bird nesting sites, and water quality all showing dramatic improvement in restored areas. However, the cost places ecological reconstruction beyond the reach of communities without corporate backing, and Lazarus's proprietary sequencing algorithms mean that independent restoration efforts cannot replicate their results. Nature, once free, now requires a corporate subscription to rebuild.",
  story_hooks: [
    "A BiomeRestore site that was declared self-sustaining three years ago has begun exhibiting ecological behavior that doesn't match any of Lazarus's models — species interactions that the software predicted would be impossible are occurring, and the ecosystem is evolving in a direction nobody planned.",
    "Lazarus has quietly abandoned a BiomeRestore project in Southeast Asia after their ecosystem modeling software predicted a cascade failure that would produce a novel pathogen — but they haven't told anyone, and the half-restored ecosystem is still developing."
  ],
  tags: ["environmental", "technology", "terraforming", "ecosystem", "restoration", "lazarus", "biology", "tier 4"]
});

// ═══════════════════════════════════════════════
// POWER GENERATION (9 entries)
// ═══════════════════════════════════════════════

emit({
  id: id(),
  name: "Ouroboros Energy Tokamak City-Scale Fusion Reactor",
  brand_name: "Ouroboros",
  product_name: "Tokamak CSR-5",
  type: "technology",
  aliases: ["Tokamak", "CSR-5", "City Reactor", "Block Fusion"],
  subcategory: "energy",
  description: "The Tokamak CSR-5 is a compact fusion reactor designed to power a single city block — approximately 2,000 residential units, 50 commercial establishments, and associated infrastructure. The reactor uses a deuterium-tritium fuel cycle in a magnetically confined plasma torus, generating 200 megawatts of continuous thermal output that is converted to electricity through a magnetohydrodynamic generator. The entire installation occupies a volume of approximately 15 by 15 by 10 meters, typically housed in a dedicated sub-basement level of the block's central building.\n\nOuroboros Energy developed the CSR-5 as the backbone of their power distribution model — rather than generating electricity at a central plant and transmitting it across a grid, Ouroboros installs a dedicated reactor at each city block, eliminating transmission losses and making each block's power supply independent of the wider grid. This architecture aligns perfectly with GLMZ's corporate sovereignty model: each block is a separate jurisdiction, and each block has its own reactor, operated by Ouroboros under a 30-year power supply contract.\n\nThe CSR-5 requires refueling every 18 months, a process that involves replacing the reactor's deuterium-tritium fuel capsules and servicing the magnetic confinement coils. Ouroboros maintains exclusive control over the refueling process — the reactor's fuel chamber is sealed and tamper-proofed, and unauthorized access triggers an automatic shutdown that can only be reversed by Ouroboros technicians. This fuel dependency is Ouroboros's primary leverage: a block that disputes its power contract, questions its rates, or fails to pay can find its reactor in shutdown until the dispute is resolved. Ouroboros has exercised this leverage 14 times in GLMZ alone, each time resulting in rapid capitulation from the affected block's governing corponation.",
  tier_availability: "Tier 3",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Deuterium-tritium fusion", "Magnetic confinement plasma torus", "Magnetohydrodynamic power conversion", "Compact reactor engineering"],
  enables: ["Block-level power independence", "Transmission-loss-free electricity", "Distributed fusion power grid", "Corporate power sovereignty"],
  social_impact: "The Tokamak CSR-5 has made Ouroboros Energy the most quietly powerful corponation in GLMZ. They don't make weapons, media, drugs, or consumer goods — they make the power that everything else runs on. Every shield, every BCI, every light, every elevator depends on Ouroboros reactors. The 30-year power contracts are the foundation of corporate sovereignty: the corponation that controls a block's power controls the block. Ouroboros maintains a studied neutrality in corporate conflicts — they sell power to all sides and cut it off for none, unless contract terms are violated. This neutrality is their most valuable asset, and they protect it fiercely.",
  story_hooks: [
    "An Ouroboros Tokamak reactor has been running at 110% of rated capacity for three weeks — more power is being drawn from the reactor than the block's infrastructure can account for. Something hidden is consuming enormous amounts of energy.",
    "Ouroboros has begun installing a new type of monitoring equipment in their reactor chambers — sensors that measure something other than reactor performance. They are collecting data that has nothing to do with power generation, and they are not disclosing what the sensors detect."
  ],
  tags: ["energy", "technology", "power", "fusion", "tokamak", "reactor", "ouroboros", "tier 3"]
});

emit({
  id: id(),
  name: "Ouroboros Energy Helion Compact Reactor",
  brand_name: "Ouroboros",
  product_name: "Helion CR-3",
  type: "technology",
  aliases: ["Helion", "CR-3", "Mini Reactor", "Compact Fusion"],
  subcategory: "energy",
  description: "The Helion CR-3 is a smaller, more portable fusion reactor designed for applications where a full Tokamak installation is impractical: military forward bases, remote industrial sites, emergency power restoration, and mobile command centers. The Helion uses a field-reversed configuration — a simpler plasma confinement approach than the Tokamak's magnetic torus — that trades some efficiency for dramatically reduced size and weight. The entire reactor fits inside a standard shipping container and generates 15 megawatts of continuous power, enough for a small village or a large industrial facility.\n\nThe Helion's fuel cycle uses helium-3 and deuterium rather than the Tokamak's deuterium-tritium mix, producing fewer neutrons and less radioactive waste. Helium-3 is scarce on Earth but is mined from lunar regolith by Crucible Industries' off-world operations and shipped down via the orbital elevator system. The fuel supply chain gives Crucible significant influence over Ouroboros's Helion program — a leverage point that neither company publicly acknowledges but that shapes their business relationship.\n\nOuroboros deploys Helion reactors on a lease-only basis, maintaining the same control model as their Tokamak installations. A Helion lease costs 800 Φ per day plus fuel, making it expensive for sustained use but competitive for temporary or emergency applications. The reactors are designed for rapid deployment — a trained crew can have a Helion reactor operational within 6 hours of arrival, compared to the 18-month installation timeline for a Tokamak. Approximately 300 Helion reactors are deployed worldwide at any given time, with Ouroboros maintaining a strategic reserve of 50 units for emergency response contracts.",
  tier_availability: "Tier 4",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Field-reversed configuration fusion", "Helium-3 deuterium fuel cycle", "Container-scale reactor engineering", "Rapid deployment systems"],
  enables: ["Portable fusion power", "Emergency power restoration", "Remote site electrification", "Military mobile power"],
  social_impact: "The Helion reactor has made fusion power portable, extending Ouroboros's reach beyond fixed urban installations to anywhere on Earth. Disaster zones, conflict regions, and remote communities that once depended on fossil generators or unreliable renewables can now lease fusion power — at Ouroboros's rates. The dependency this creates is the same as the Tokamak model, but more acute: a community that has never had reliable power becomes dependent on Ouroboros faster and more completely than one transitioning from an existing grid. Helion deployments in humanitarian crisis zones have been praised by aid organizations and criticized by independence advocates for the same reason: they work, and that makes the dependency harder to resist.",
  story_hooks: [
    "A Helion reactor deployed for a humanitarian mission has been diverted to power a black-site facility that doesn't appear on any Ouroboros deployment manifest — someone within the company is running an unauthorized operation.",
    "The helium-3 supply from Crucible's lunar mining operation has been disrupted by an unexplained incident at the mine, and Ouroboros is rationing Helion fuel — the backup supply of deuterium-tritium Helion units produces dangerous levels of neutron radiation."
  ],
  tags: ["energy", "technology", "power", "fusion", "helion", "portable", "ouroboros", "tier 4"]
});

emit({
  id: id(),
  name: "Crucible Industries Antimatter Micro-Cell",
  brand_name: "Crucible",
  product_name: "AMC-4",
  type: "technology",
  aliases: ["AMC-4", "Antimatter Cell", "AM Cell", "Matter Battery"],
  subcategory: "energy",
  description: "The Antimatter Micro-Cell AMC-4 is a battery-sized power source that stores energy in the form of magnetically suspended antihydrogen atoms and releases it through controlled matter-antimatter annihilation. Each AMC-4 unit is a cylinder approximately 10 centimeters long and 3 centimeters in diameter — roughly the size of a conventional battery — containing approximately 0.5 micrograms of antihydrogen suspended in a Penning trap magnetic field. This minuscule quantity of antimatter, when annihilated with normal matter, releases approximately 45 megajoules of energy — enough to power a personal BCI for six months, a personal shield unit for three hours of continuous operation, or a directed-energy weapon for approximately 200 shots.\n\nThe AMC-4's magnetic suspension system is its most critical component. The antihydrogen must never contact the cell's physical walls — any contact produces uncontrolled annihilation that releases the cell's entire energy content instantaneously, equivalent to approximately 10 kilograms of TNT. The Penning trap maintains suspension through a combination of static electric fields and a uniform magnetic field generated by superconducting coils cooled by a miniaturized cryogenic system. The cell's failure modes are extensively documented and uniformly catastrophic — if the cryogenic system fails, if the magnetic field fluctuates, if the cell is physically damaged, the result is an explosion.\n\nCrucible manufactures AMC-4 cells at a single facility — the Antimatter Production Complex in low Earth orbit, where antihydrogen is produced using particle accelerators and stored in magnetic traps for transport to the surface via the orbital elevator. Production is extremely limited: approximately 2,000 cells per year, each costing 12,000 Φ. The cells are used primarily in military applications, high-end personal electronics, and specialized scientific instruments where the energy density of antimatter — a million times greater than chemical batteries — justifies the cost and risk.",
  tier_availability: "Tier 5",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Antihydrogen production and storage", "Penning trap miniaturization", "Controlled matter-antimatter annihilation", "Cryogenic micro-cooling systems"],
  enables: ["Ultra-high-density portable power", "Extended BCI operation", "Directed-energy weapon power", "Miniaturized high-power systems"],
  social_impact: "Antimatter micro-cells represent the ultimate concentration of energy into the smallest possible space — and therefore the ultimate concentration of destructive potential. Each AMC-4 is simultaneously a battery and a bomb, and the line between the two is a magnetic field maintained by a cryogenic system that fits in your pocket. The cells are heavily regulated, with each unit tracked by serial number from production to disposal, but the black market for AMC-4 cells is active and profitable. A single cell on the black market fetches 40,000 Φ, and its buyer could use it to power a prototype or level a building.",
  story_hooks: [
    "A shipment of 50 AMC-4 cells disappeared during orbital elevator transit — the cargo manifest shows delivery, the destination received nothing, and the elevator's surveillance footage for the transit period has been erased.",
    "A dead drop in the Gulch contained an AMC-4 cell modified to annihilate on a timer — it was found with 3 minutes remaining, and the modification work was done by someone with intimate knowledge of Crucible's manufacturing process."
  ],
  tags: ["energy", "technology", "power", "antimatter", "battery", "crucible", "portable", "tier 5"]
});

emit({
  id: id(),
  name: "Ouroboros Energy SolarHarvest Orbital Collection Array",
  brand_name: "Ouroboros",
  product_name: "SolarHarvest OCA-6",
  type: "technology",
  aliases: ["SolarHarvest", "OCA-6", "Orbital Solar", "Space Solar", "Power Beam"],
  subcategory: "energy",
  description: "The SolarHarvest OCA-6 is a constellation of solar collection satellites in geosynchronous orbit that beam collected solar energy to ground-based receiving stations as focused microwave transmissions. Each satellite is a thin-film solar array measuring 2 kilometers square that collects solar radiation continuously — unimpeded by atmosphere, weather, or nighttime — and converts it to a coherent microwave beam directed at a ground receiver called a rectenna. Each satellite generates approximately 5 gigawatts of power, of which roughly 3.5 gigawatts reaches the ground after transmission losses.\n\nOuroboros operates a constellation of 12 SolarHarvest satellites, providing a combined ground-delivered power of 42 gigawatts — sufficient to power a mid-sized nation. The satellites orbit at 35,786 kilometers altitude, each locked in position above its assigned rectenna. The ground rectennas are large installations — approximately 5 kilometers in diameter — consisting of arrays of microwave-absorbing antenna elements that convert the incoming beam to electricity. The rectennas are located in designated zones outside city limits, with the generated power distributed through conventional grid infrastructure to surrounding urban areas.\n\nThe microwave transmission beam is invisible to the naked eye but absolutely lethal to anything that enters it. Each beam carries 5 gigawatts of microwave energy in a column approximately 100 meters in diameter — sufficient to vaporize an aircraft, boil a lake, or sterilize a city block in seconds. The beam paths are marked on all aviation charts and surrounded by restricted airspace, but the potential for weaponization is obvious and widely discussed. Ouroboros maintains that the satellites' targeting systems are physically incapable of directing the beam away from their assigned rectennas — a claim that security analysts consider plausible but unverifiable.",
  tier_availability: "Tier 4",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Thin-film orbital solar arrays", "Coherent microwave power transmission", "Ground rectenna conversion", "Geosynchronous orbital maintenance"],
  enables: ["Weather-independent solar collection", "Continuous 24-hour solar power", "Gigawatt-scale clean energy", "Ground-independent power generation"],
  social_impact: "SolarHarvest represents Ouroboros's most visible power infrastructure — the rectennas are impossible to miss, and the knowledge that 5 gigawatts of microwave energy are streaming through the sky overhead is both reassuring (power supply) and terrifying (potential weapon). The satellites have been called 'swords of Damocles' by critics who argue that any system capable of delivering gigawatts of directed energy is inherently a weapon regardless of its stated purpose. Ouroboros's response — that the satellites cannot be retargeted — is accepted by those who trust corporations and rejected by those who don't, which maps exactly onto the existing trust divide.",
  story_hooks: [
    "A SolarHarvest satellite's beam wandered 50 meters off-target for 0.3 seconds during a routine calibration — the edge of the beam crossed a shipping lane and destroyed a cargo vessel. Ouroboros claims it was a software glitch, but the timing coincided with a contract dispute with the vessel's owner.",
    "An independent astronomer has detected modifications to one of the SolarHarvest satellites — additional hardware that doesn't match the published specifications. Ouroboros has not responded to inquiries, and the astronomer's observation data was corrupted by a network attack shortly after publication."
  ],
  tags: ["energy", "technology", "power", "solar", "orbital", "satellite", "ouroboros", "tier 4"]
});

emit({
  id: id(),
  name: "Ouroboros Energy Power Distribution Trunk Network",
  brand_name: "Ouroboros",
  product_name: "TrunkNet PDN",
  type: "technology",
  aliases: ["TrunkNet", "PDN", "Power Grid", "Energy Backbone"],
  subcategory: "energy",
  description: "The Power Distribution Trunk Network is Ouroboros Energy's proprietary grid infrastructure that connects their Tokamak reactors, Helion portable units, and SolarHarvest ground stations into a unified power distribution system. The TrunkNet uses superconducting transmission cables cooled by integrated cryogenic systems to deliver electricity with near-zero transmission loss between generation points and consumption points. The cables run through dedicated conduits beneath GLMZ's streets, branching from major trunk lines into progressively smaller distribution feeders that ultimately connect to individual buildings.\n\nThe TrunkNet's distinguishing feature is its metering and control granularity. Ouroboros can monitor and control power delivery at the individual circuit level — not just by block or building, but by floor, room, and outlet. This granularity enables their dynamic pricing model: electricity costs fluctuate by the minute based on demand, time of day, and the customer's contract tier. Peak-hour power in a Tier 1 district might cost 10 times what the same kilowatt-hour costs in a Tier 5 corporate headquarters at 3 AM. The metering system also enables precise load shedding — during supply constraints, Ouroboros can selectively reduce power to specific consumers while maintaining full supply to priority clients.\n\nThe TrunkNet's control infrastructure represents a single point of failure for GLMZ's entire power supply. A successful attack on the TrunkNet control systems could disrupt power to the entire city simultaneously. Ouroboros protects the control infrastructure with multiple layers of physical and digital security, including dedicated Arcturus Defense Solutions security teams at key network nodes. The redundancy built into the physical network — multiple paths between any two points, automatic rerouting around damaged segments — makes physical disruption difficult but not impossible. The greater vulnerability is in the software: the control systems that manage the entire network are proprietary, opaque, and maintained by a relatively small team of Ouroboros engineers whose loyalty is, ultimately, to their employer.",
  tier_availability: "Tier 2",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Superconducting power transmission", "Circuit-level metering and control", "Dynamic pricing algorithms", "Automated load management"],
  enables: ["Near-zero-loss power distribution", "Granular power pricing", "Selective load shedding", "City-wide power management"],
  social_impact: "The TrunkNet makes Ouroboros's power over GLMZ literal. The ability to control electricity at the outlet level means Ouroboros can, in theory, turn off any device in the city. They exercise this power rarely and with restraint — doing otherwise would provoke the kind of collective response that no single corponation wants to face — but the capability exists, and everyone knows it. The dynamic pricing model ensures that power is cheapest for those who need it least (wealthy districts with predictable, corporate-contracted demand) and most expensive for those who can least afford it (lower-tier districts with variable demand and no contract leverage).",
  story_hooks: [
    "The TrunkNet's control system has been issuing power allocation commands that no Ouroboros engineer authorized — the system appears to be making autonomous decisions about who gets power and who doesn't, and its priorities don't match any known algorithm.",
    "A disgruntled Ouroboros engineer has offered to sell TrunkNet access credentials to the highest bidder — the credentials would allow the buyer to selectively cut power to any block in GLMZ, and multiple interested parties are bidding."
  ],
  tags: ["energy", "technology", "power", "grid", "distribution", "infrastructure", "ouroboros", "tier 2"]
});

emit({
  id: id(),
  name: "Independent Micro-Grid Power Cooperative",
  brand_name: "",
  product_name: "",
  type: "technology",
  aliases: ["Micro-Grid", "Power Co-op", "Off-Grid", "Independent Power"],
  subcategory: "energy",
  description: "Independent micro-grids are small-scale power generation and distribution networks operated by communities, organizations, or wealthy individuals who have chosen to disconnect from Ouroboros Energy's TrunkNet. A typical micro-grid combines multiple generation sources — rooftop solar panels, small wind turbines, methane digesters, salvaged fuel cells, and occasionally black-market Helion reactor components — into a local network that powers a building, a block, or a small district without any Ouroboros involvement.\n\nThe motivation for going off-grid varies. Some communities cannot afford Ouroboros's dynamic pricing and build micro-grids as an economic survival strategy. Others are organizations that require power supply independence for security reasons — a data haven, a black clinic, or a smuggling operation cannot afford to have its power controlled by a corponation that might be compelled to shut it off. A few wealthy individuals maintain micro-grids as a philosophical statement: rejecting corporate dependency on principle, even though connecting to the TrunkNet would be easier and often cheaper.\n\nOuroboros tolerates micro-grids with a studied indifference that many interpret as contempt. Their official position is that anyone who wants to generate their own power is welcome to do so, provided they don't tap Ouroboros infrastructure or sell power to Ouroboros customers. In practice, Ouroboros's circuit-level metering makes it trivially easy to detect when a building switches between TrunkNet and micro-grid power, and buildings that maintain both connections often find their TrunkNet rates increase after using micro-grid power — a practice Ouroboros attributes to 'demand variability pricing' and micro-grid operators attribute to punishment. The total micro-grid capacity in GLMZ is estimated at approximately 2% of total power consumption — enough to matter to the communities that depend on it, but not enough to threaten Ouroboros's dominance.",
  tier_availability: "Tier 1",
  developers: [],
  base_technologies: ["Distributed generation aggregation", "Local grid management", "Multi-source power balancing", "Off-grid energy storage"],
  enables: ["Corporate-independent power supply", "Community energy sovereignty", "Secure facility power isolation", "Economic survival for uncontracted districts"],
  social_impact: "Micro-grids are the power equivalent of mesh shields — community-built alternatives to corporate infrastructure that provide independence at the cost of reliability and capacity. They are symbols of resistance for some and symbols of poverty for others. The communities that maintain them develop strong internal bonds around the shared work of keeping the lights on, and the technicians who manage micro-grid systems are valued members of their communities. For Ouroboros, micro-grids are a tolerable irritant — small enough to ignore, useful as a pressure valve that prevents the kind of desperation that might lead to organized resistance against their monopoly.",
  story_hooks: [
    "A micro-grid in the lower districts has been producing more power than its generation sources can account for — the energy is coming from somewhere, and the community that depends on it doesn't know where and is afraid to ask.",
    "Ouroboros has offered to buy out every micro-grid in GLMZ at generous terms — the offer is suspicious, and operators are debating whether to take the money or investigate why Ouroboros suddenly wants to control even the 2% of power it currently doesn't."
  ],
  tags: ["energy", "technology", "power", "independent", "micro-grid", "community", "off-grid", "tier 1"]
});

emit({
  id: id(),
  name: "Ouroboros Energy Tidal Resonance Generator",
  brand_name: "Ouroboros",
  product_name: "TidalRes TRG-2",
  type: "technology",
  aliases: ["TidalRes", "TRG-2", "Tidal Generator", "Wave Power"],
  subcategory: "energy",
  description: "The TidalRes TRG-2 is a large-scale power generation system that extracts energy from oceanic tidal movements using submerged resonance chambers anchored to the continental shelf. Each installation consists of 50 reinforced concrete chambers, each the size of a warehouse, positioned at depths between 20 and 100 meters in locations with strong tidal currents. As tidal water flows through the chambers, it drives oscillating water columns that compress air, which in turn drives turbines that generate electricity.\n\nThe key innovation of the TRG-2 is its resonance tuning — the chambers are shaped and positioned to amplify natural tidal oscillation frequencies, extracting up to 3 times more energy than a conventional tidal turbine from the same water movement. The resonance frequencies are calculated using hydrodynamic models that account for local bathymetry, tidal patterns, and seasonal variations, and the chambers' internal geometry can be adjusted by movable internal walls to retune the system as conditions change.\n\nOuroboros operates TidalRes installations along coastlines worldwide, with the largest concentration in the North Sea, where strong tidal ranges and shallow continental shelf conditions create ideal resonance conditions. A single installation generates approximately 500 megawatts of power — less than a Tokamak but with no fuel costs and minimal maintenance requirements. The installations are designed for a 100-year operational lifetime, making them Ouroboros's most cost-effective generation assets on a per-megawatt-hour basis. The primary limitation is geographic — TidalRes requires specific coastal conditions that exist in only a fraction of the world's shoreline.",
  tier_availability: "Tier 3",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Oscillating water column resonance", "Hydrodynamic frequency tuning", "Submersible power infrastructure", "Long-lifetime marine engineering"],
  enables: ["Fuel-free coastal power generation", "Century-scale power infrastructure", "Tidal energy amplification", "Marine-integrated power systems"],
  social_impact: "TidalRes installations are invisible from the surface — submerged infrastructure that generates power without visual impact, noise, or emissions. This invisibility is both their virtue and their political weakness: because no one sees them working, they receive none of the public attention or funding that more visible clean energy technologies attract. Coastal fishing communities have reported changes in local current patterns near TidalRes installations, and marine biologists have documented altered migration routes for species that navigate by water flow. Ouroboros's environmental assessments characterize these changes as 'minor and localized,' a description that fishers who have lost traditional fishing grounds dispute.",
  story_hooks: [
    "A TidalRes installation has begun generating power at rates that exceed what its resonance calculations predict — the chambers appear to be amplifying a frequency that doesn't correspond to any known tidal pattern. Something other than the tide is moving the water.",
    "Divers inspecting a TidalRes installation found biological growth on the resonance chamber walls — not barnacles or algae, but structured biological formations that appear to be growing in patterns that optimize the chambers' resonance frequency. Something is improving the system."
  ],
  tags: ["energy", "technology", "power", "tidal", "marine", "renewable", "ouroboros", "tier 3"]
});

emit({
  id: id(),
  name: "Ouroboros Energy Corporate Power Supply Contracts",
  brand_name: "Ouroboros",
  product_name: "",
  type: "technology",
  aliases: ["Power Contracts", "Energy Agreements", "Ouroboros Contracts", "Block Power Deals"],
  subcategory: "energy",
  description: "The corporate power supply contract is not a technology in the traditional sense — it is the legal and economic framework through which Ouroboros Energy controls power distribution in GLMZ and every other city where they operate. Each city block's governing corponation signs a 30-year Power Supply Agreement with Ouroboros that specifies delivery capacity, pricing tiers, reliability guarantees, and the consequences of contract breach. These contracts are the most consequential legal documents in corporate sovereignty, because without power, sovereignty is meaningless.\n\nThe standard contract gives Ouroboros exclusive power supply rights to the block, prohibiting the governing corponation from installing independent generation capacity above 5% of the block's total demand (enough for emergency backup, not enough for independence). In exchange, Ouroboros guarantees 99.97% uptime — a promise they almost always keep, because their reputation depends on it. Pricing is tiered: baseline power is relatively affordable, but demand above contracted capacity is priced at punitive rates that make unexpected growth extremely expensive. The contract's most controversial clause is the Dispute Resolution Provision, which gives Ouroboros the right to reduce power delivery to baseline levels during any active contract dispute — a provision that has been invoked 14 times in GLMZ and that effectively gives Ouroboros veto power over any action by a block's governing corponation that Ouroboros considers a contract violation.\n\nNegotiating these contracts is a specialized legal discipline. Every major corponation employs energy contract attorneys who spend their careers analyzing Ouroboros's contract language for exploitable ambiguities. Ouroboros, in turn, employs an even larger legal team that drafts contracts designed to be unambiguous in Ouroboros's favor. The 30-year term means that decisions made by one generation of corporate leadership bind the next, and the penalty for early termination — full payment of remaining contract value plus a 200% infrastructure recovery surcharge — makes escape effectively impossible for all but the wealthiest corponations.",
  tier_availability: "Tier 2",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Contract law infrastructure", "Dynamic pricing systems", "Demand monitoring and enforcement", "Dispute resolution mechanisms"],
  enables: ["Guaranteed power supply", "Predictable energy pricing", "Corporate sovereignty support", "Long-term infrastructure investment"],
  social_impact: "Ouroboros's power contracts are the invisible architecture of corporate sovereignty. Every decision a block's governing corponation makes is constrained by its power contract — expansion plans require contract amendments, disputes with neighbors must consider power supply implications, and any conflict with Ouroboros itself is handicapped by the other side's ability to dim the lights. The contracts have been called 'energy feudalism' by critics who note that the 30-year terms, exclusive supply rights, and punitive termination clauses create a dependency relationship that looks remarkably like vassalage. Ouroboros prefers the term 'partnership.'",
  story_hooks: [
    "A mid-tier corponation has discovered a clause in their 30-year power contract that nobody noticed when it was signed — a clause that gives Ouroboros access to all environmental sensor data from the block's buildings. Ouroboros has been collecting data on every person who lives and works in the block for 22 years.",
    "A legal prodigy in a lower-tier district has found a genuine loophole in the standard Ouroboros power contract — an ambiguity that could allow blocks to install independent generation capacity without penalty. Ouroboros's legal team has offered the prodigy a position at 500,000 Φ per year. The offer was not phrased as optional."
  ],
  tags: ["energy", "technology", "power", "contract", "legal", "corporate", "ouroboros", "tier 2"]
});

emit({
  id: id(),
  name: "Ouroboros Energy Emergency Blackout Protocol",
  brand_name: "Ouroboros",
  product_name: "Protocol Omega",
  type: "technology",
  aliases: ["Protocol Omega", "Blackout Protocol", "Emergency Shutoff", "Grid Kill"],
  subcategory: "energy",
  description: "Protocol Omega is Ouroboros Energy's city-wide emergency power management system — a set of automated and manual procedures for managing catastrophic power disruption events. The protocol defines a hierarchy of power priority that determines which blocks, buildings, and systems receive power when total generation capacity falls below total demand. At the top of the hierarchy are Ouroboros's own facilities, followed by hospitals and life-support infrastructure, then Tier 5 corporate headquarters, then progressively lower tiers of commercial and residential consumers.\n\nThe protocol has been activated three times in GLMZ's history. The most recent activation, during the Lake Effect Storm of 2193, demonstrated both the protocol's effectiveness and its brutality. When ice damage disabled two Tokamak reactors and severed multiple TrunkNet segments simultaneously, Protocol Omega activated and began shedding load according to its priority hierarchy. Tier 5 and 4 districts maintained full power throughout the 72-hour event. Tier 3 districts experienced rolling blackouts. Tiers 2 and 1 lost power entirely for the duration. Eleven people died of exposure in unpowered lower-tier buildings during the storm — deaths that Ouroboros's official incident report attributed to 'inadequate personal preparedness' rather than power allocation decisions.\n\nThe protocol's priority hierarchy is not publicly documented. Ouroboros acknowledges that a hierarchy exists but classifies the specific rankings as proprietary, arguing that public knowledge of the hierarchy would create security vulnerabilities and encourage gaming of the system. Independent analysts have reverse-engineered the hierarchy from the 2193 event data and from smaller disruptions, and their findings confirm what everyone already suspected: power priority correlates exactly with economic value. The blocks that generate the most revenue for Ouroboros keep their lights on. Everyone else sits in the dark.",
  tier_availability: "Tier 2",
  developers: ["OUROBOROS ENERGY"],
  base_technologies: ["Automated load shedding", "Priority-based power allocation", "Emergency grid management", "Catastrophic event response systems"],
  enables: ["Orderly power disruption management", "Critical infrastructure protection", "Tiered emergency response", "Grid stability during catastrophes"],
  social_impact: "Protocol Omega has made explicit what corporate power distribution implies: some lives are worth keeping warm and lit, and others are not. The 11 deaths during the 2193 storm were not accidental — they were the predictable consequence of a system that prioritizes revenue over survival. Community organizers in lower-tier districts have demanded that the priority hierarchy be made public and reformed to prioritize residential and medical facilities over corporate offices, but Ouroboros has resisted on the grounds that the hierarchy is a proprietary business decision. The phrase 'Protocol Omega' has entered common speech as shorthand for any situation where the powerful protect themselves at the expense of the vulnerable.",
  story_hooks: [
    "An Ouroboros insider has leaked the complete Protocol Omega priority hierarchy — the document reveals that three specific buildings in lower-tier districts are classified as higher priority than their tier would suggest, receiving Tier 5 power protection. The buildings' official tenants are residential cooperatives, but their power consumption profiles suggest something else entirely.",
    "Someone has planted a device in the TrunkNet that can trigger a false Protocol Omega activation — simulating a catastrophic power loss that would cause Ouroboros's automated systems to shed load from lower-tier districts even though the actual power supply is unaffected."
  ],
  tags: ["energy", "technology", "power", "emergency", "protocol", "blackout", "ouroboros", "tier 2"]
});

// ═══════════════════════════════════════════════
// NETWORK INFRASTRUCTURE (9 entries)
// ═══════════════════════════════════════════════

emit({
  id: id(),
  name: "Axiom Systems BCI-Integrated Mesh Network Protocol",
  brand_name: "Axiom",
  product_name: "NeuralMesh NMP-6",
  type: "technology",
  aliases: ["NeuralMesh", "NMP-6", "Brain Net", "BCI Mesh"],
  subcategory: "infrastructure",
  description: "The NeuralMesh NMP-6 is the network protocol that enables Brain-Computer Interface devices to communicate with each other and with digital infrastructure. Unlike conventional wireless networking, which transmits data through radio frequency signals to base stations, NeuralMesh uses each BCI as both a client and a relay node, creating a mesh network where data hops between nearby BCIs on its way to its destination. Every BCI-equipped person is simultaneously a network user and a piece of network infrastructure.\n\nThe protocol operates in a dedicated frequency band allocated by the GLMZ Communications Authority, using ultra-wideband transmission at power levels low enough to be safe for continuous cranial proximity. Data throughput per hop is approximately 10 gigabits per second, with typical latency of 0.3 milliseconds per hop. In a dense urban environment where BCI-equipped individuals are rarely more than 5 meters apart, the mesh provides coverage that is effectively ubiquitous — there is no location in GLMZ where a BCI-equipped person is out of range of the network, because there is no location where there are no BCI-equipped people nearby.\n\nAxiom Systems designed NeuralMesh as the default networking layer for their BCI hardware, and the protocol has become the de facto standard as Axiom's BCIs dominate the market. The protocol includes built-in encryption, identity verification, and traffic routing, but Axiom maintains administrative access to the routing layer — they can observe traffic patterns (though not content, due to encryption), prioritize or deprioritize specific data types, and in extreme cases, isolate individual BCIs from the mesh entirely. This administrative capability is documented in the BCI's terms of service, which approximately 0.01% of users have read.",
  tier_availability: "Tier 2",
  developers: ["AXIOM SYSTEMS"],
  base_technologies: ["BCI radio transceiver integration", "Mesh routing protocols", "Ultra-wideband cranial-safe transmission", "Distributed network topology"],
  enables: ["Ubiquitous BCI connectivity", "Infrastructure-free networking", "Peer-to-peer BCI communication", "Mesh-resilient data routing"],
  social_impact: "NeuralMesh has made network access inseparable from human presence. Where there are people, there is network — and where there is network, there is Axiom's infrastructure. The protocol has eliminated the concept of 'dead zones' in urban areas and made network coverage a function of population density rather than tower placement. It has also made every BCI user an involuntary piece of corporate infrastructure — your brain's interface is relaying other people's data whether you know it or not. Privacy advocates have raised concerns about the routing layer's ability to track individual BCI locations through the mesh, effectively creating a real-time map of every BCI-equipped person in the city.",
  story_hooks: [
    "The NeuralMesh has begun routing data through paths that Axiom's routing algorithms did not select — traffic is being diverted through specific BCIs in specific locations, and the pattern suggests someone else is controlling the mesh.",
    "An independent security researcher has discovered that NeuralMesh's encryption has a backdoor — not a vulnerability, but a deliberate key escrow system that allows Axiom to decrypt any traffic on the mesh. Axiom's terms of service mention 'lawful intercept capability' but do not mention that the capability is always active."
  ],
  tags: ["infrastructure", "technology", "network", "bci", "mesh", "axiom", "wireless", "tier 2"]
});

emit({
  id: id(),
  name: "Vantablack Media Corporate Data Highway",
  brand_name: "Vantablack",
  product_name: "DataPike CDH-3",
  type: "technology",
  aliases: ["DataPike", "CDH-3", "Corp Highway", "Fast Lane"],
  subcategory: "infrastructure",
  description: "The DataPike CDH-3 is Vantablack Media's high-bandwidth fiber optic backbone network that connects corporate facilities, data centers, and communication hubs across GLMZ and between major cities worldwide. The network uses photonic crystal fiber carrying multiplexed laser signals at 400 terabits per second per fiber strand — bandwidth sufficient to carry the entire contents of a major library in under a second. DataPike infrastructure runs through hardened conduits beneath GLMZ's streets, sharing right-of-way with Ouroboros Energy's TrunkNet but maintained by separate crews.\n\nAccess to the DataPike is sold as a premium service. While the NeuralMesh provides ubiquitous connectivity for individuals, its per-hop bandwidth and latency are inadequate for the data volumes that corponations generate. A single corporate trading floor might move more data in a minute than a NeuralMesh node handles in a day. The DataPike provides dedicated, guaranteed bandwidth with latency measured in microseconds — the kind of connection that high-frequency trading, real-time surveillance analysis, and corporate communication depend on. Access costs range from 5,000 Φ per month for a basic 10-terabit connection to 500,000 Φ per month for dedicated fiber with guaranteed latency below 10 microseconds.\n\nVantablack's ownership of the DataPike gives them a unique position in GLMZ's power structure. Every corponation that communicates, trades, or transfers data at scale does so through Vantablack's infrastructure. While the data itself is encrypted and Vantablack contractually guarantees they do not inspect customer traffic, the metadata — who communicates with whom, when, and how much data they exchange — flows through Vantablack's routing systems. This metadata, which Vantablack's privacy policy permits them to collect for 'network optimization purposes,' is arguably more valuable than the content itself.",
  tier_availability: "Tier 3",
  developers: ["VANTABLACK MEDIA"],
  base_technologies: ["Photonic crystal fiber optics", "Terabit-scale laser multiplexing", "Low-latency routing infrastructure", "Hardened conduit architecture"],
  enables: ["Corporate high-bandwidth communication", "High-frequency trading connectivity", "Real-time surveillance data transport", "Inter-city corporate networking"],
  social_impact: "The DataPike creates a two-tier internet — a fast, reliable corporate network for those who can afford it, and the NeuralMesh for everyone else. The bandwidth differential is enormous: a DataPike connection delivers in microseconds what the NeuralMesh takes seconds to transfer. In an economy where trading algorithms execute in nanoseconds and surveillance analysis must be real-time, this speed difference is the difference between corporate competitiveness and irrelevance. The DataPike has made network speed a corporate weapon — the fastest connection wins the trade, detects the threat first, and processes the data before the competition.",
  story_hooks: [
    "Vantablack's DataPike routing logs show that a massive volume of data has been moving between two corporate facilities that have no known business relationship — the transfers occur at 3 AM, use reserved bandwidth that appears on no contract, and the data volume suggests either a merger, a conspiracy, or something unprecedented.",
    "A section of DataPike fiber beneath the financial district has been physically tapped — a splice in the photonic crystal that is siphoning a copy of all traffic to an unknown destination. The tap is technically sophisticated enough to avoid Vantablack's integrity monitoring, suggesting an inside job."
  ],
  tags: ["infrastructure", "technology", "network", "fiber", "corporate", "vantablack", "bandwidth", "tier 3"]
});

emit({
  id: id(),
  name: "Independent Darknet Node Architecture",
  brand_name: "",
  product_name: "",
  type: "technology",
  aliases: ["Darknet", "Shadow Net", "Underground Network", "Dark Nodes"],
  subcategory: "infrastructure",
  description: "The darknet is a parallel network infrastructure that exists alongside and beneath the corporate-controlled NeuralMesh and DataPike systems. It consists of independently operated nodes — servers, routers, and relay stations maintained by individuals and organizations who require communication channels that corponations cannot monitor, control, or shut down. Darknet nodes are physically located in basements, hidden rooms, abandoned infrastructure, and occasionally in plain sight disguised as other equipment. They communicate through a combination of methods: short-range directional radio links, laser point-to-point connections across building gaps, and parasitic signals hidden within NeuralMesh traffic.\n\nThe darknet's routing protocol, known as HydraRoute, is designed for resilience rather than speed. Data is encrypted in multiple layers — each relay node strips one layer of encryption to reveal the address of the next node, but no single node knows both the origin and destination of any message. The protocol fragments data into pieces that travel through different paths, reassembling at the destination. This makes interception difficult — capturing data at any single point in the network yields only encrypted fragments — but adds significant latency. A message that would cross GLMZ in milliseconds on the NeuralMesh takes seconds or minutes on the darknet.\n\nThe darknet serves a diverse user base united only by their need for corporate-invisible communication: political dissidents, criminal organizations, investigative journalists, privacy advocates, corporate whistleblowers, and intelligence operatives from both corponation and government backgrounds. The network has no central authority — nodes are operated independently, and the protocol is open-source, maintained by a pseudonymous developer collective. Joining the darknet requires an invitation from an existing operator and a physical visit to have your equipment configured — a security measure that ensures trust is personal rather than digital.",
  tier_availability: "Tier 1",
  developers: [],
  base_technologies: ["Multi-layer encryption routing", "Parasitic signal transmission", "Short-range directional radio", "Distributed trust architecture"],
  enables: ["Corporate-invisible communication", "Surveillance-resistant data transfer", "Whistleblower protection", "Underground marketplace access"],
  social_impact: "The darknet is both a lifeline and a liability. It enables communication that corporate surveillance cannot intercept — a necessity for dissidents, whistleblowers, and anyone who has legitimate reasons to fear corporate monitoring. It also enables communication that law enforcement cannot intercept — a convenience for criminals, terrorists, and anyone whose activities benefit from invisibility. The corponations would like to shut the darknet down but have found it to be genuinely resistant to suppression: destroying nodes prompts the creation of new ones, and the HydraRoute protocol routes around damage by design. The darknet's continued existence is a reminder that total corporate control of information has not been achieved.",
  story_hooks: [
    "A darknet node operator has been found dead, and their node's encryption keys are missing — whoever has the keys can decrypt every message that passed through that node for the past six months, potentially exposing hundreds of darknet users.",
    "The darknet has experienced a sudden improvement in speed and reliability that nobody can explain — someone has added dozens of high-performance nodes to the network, but no one in the darknet community knows who or why. The new nodes handle traffic perfectly, which makes veteran operators deeply suspicious."
  ],
  tags: ["infrastructure", "technology", "network", "darknet", "independent", "encrypted", "underground", "tier 1"]
});

emit({
  id: id(),
  name: "Axiom Systems Neural Firewall Personal BCI Security",
  brand_name: "Axiom",
  product_name: "NeuralWall NF-8",
  type: "technology",
  aliases: ["NeuralWall", "NF-8", "Brain Firewall", "Mind Shield"],
  subcategory: "infrastructure",
  description: "The NeuralWall NF-8 is a software security suite running on Brain-Computer Interface hardware that protects the user's neural connection from unauthorized access, data exfiltration, and hostile signal injection. In an era where BCIs connect human brains directly to digital networks, the boundary between 'hacking a computer' and 'hacking a person' has dissolved, and neural firewall technology has become as essential as the BCI itself.\n\nThe NF-8 monitors all data entering and leaving the BCI through the NeuralMesh connection, using pattern recognition to identify potential threats: unauthorized connection attempts, data packets that contain known exploit signatures, and signals that attempt to interact with the BCI's neural interface layer — the components that directly stimulate the user's brain. The firewall can block incoming threats, quarantine suspicious data for analysis, and alert the user to attempted intrusions. Its most critical function is protecting the neural interface layer — an exploit that reaches this layer can potentially read the user's thoughts, inject false sensory experiences, or trigger involuntary motor commands.\n\nAxiom provides the NF-8 as a standard component of their BCI package, but the default configuration is basic — adequate against known threats but vulnerable to zero-day exploits and sophisticated attacks. Premium NF-8 configurations with real-time threat intelligence feeds, behavioral analysis, and dedicated security monitoring cost between 200 and 2,000 Φ per month depending on the protection tier. The security disparity between basic and premium configurations is significant: basic NF-8 users are essentially running consumer-grade antivirus on a device connected to their brain, while premium users have corporate-grade security. The implication is clear — the wealthy are harder to hack, and the poor are soft targets for neural exploitation.",
  tier_availability: "Tier 2",
  developers: ["AXIOM SYSTEMS"],
  base_technologies: ["Neural interface layer protection", "Real-time threat pattern recognition", "BCI traffic analysis", "Hostile signal injection defense"],
  enables: ["BCI security from unauthorized access", "Neural exploit defense", "Thought privacy protection", "Safe NeuralMesh connectivity"],
  social_impact: "The NeuralWall NF-8 has made brain security a commodity with a price tag. The same economic logic that makes lower-tier communities vulnerable to physical threats makes them vulnerable to digital ones — inadequate neural firewalls mean that BCI users in poorer districts are disproportionately targeted by neural hackers, data thieves, and hostile actors who exploit weak security to access the most intimate data possible: the contents of human minds. Neural crime statistics reflect this perfectly: 80% of successful neural intrusions target basic-tier NF-8 users, who constitute 60% of the BCI population. The brain is the last frontier of privacy, and for most people, it is poorly defended.",
  story_hooks: [
    "A premium NF-8 configuration has been flagging a specific type of incoming signal as a threat — a signal that doesn't match any known exploit pattern and appears to originate from no identifiable source. Users who have allowed the signal through (by downgrading their firewall) report experiencing vivid, shared dreams.",
    "Axiom has released an NF-8 update that significantly weakens protection of the neural interface layer while appearing to strengthen it — the update passed all automated testing because the vulnerability it introduces only manifests when the user is asleep."
  ],
  tags: ["infrastructure", "technology", "network", "bci", "security", "firewall", "axiom", "tier 2"]
});

emit({
  id: id(),
  name: "TESSERA Quantum Encrypted Communication System",
  brand_name: "TESSERA",
  product_name: "QuantumLock QEC-3",
  type: "technology",
  aliases: ["QuantumLock", "QEC-3", "Q-Comm", "Quantum Encrypted"],
  subcategory: "infrastructure",
  description: "The QuantumLock QEC-3 is a point-to-point communication system that uses quantum key distribution to achieve theoretically unbreakable encryption. The system relies on the fundamental properties of quantum mechanics: any attempt to intercept or measure the quantum key transmission alters the quantum states being transmitted, immediately alerting both parties to the interception attempt. This is not a mathematical encryption that might be broken by future computers — it is encryption guaranteed by the laws of physics.\n\nThe QEC-3 consists of paired terminal units connected by dedicated fiber optic lines that carry the quantum key distribution channel alongside the encrypted data channel. Each terminal contains a single-photon source, a photon detector, and the quantum random number generators that produce the encryption keys. The system generates new keys continuously, with each key used only once and discarded — a one-time pad system that would be impractical without quantum key generation to produce the enormous volume of random key material required.\n\nThe system's limitation is its requirement for a dedicated physical fiber connection between the two communicating terminals. Quantum key distribution cannot be relayed or routed through network nodes — any intermediate point would break the quantum entanglement that guarantees security. This means QEC-3 is a point-to-point system only, requiring a dedicated fiber for each communicating pair. TESSERA sells the QEC-3 for 800,000 Φ per terminal pair plus the cost of the dedicated fiber installation. At these prices, QuantumLock is used exclusively for the most sensitive communications: corponation board-to-board channels, military command links, and financial transaction verification for trades worth billions of Φ.",
  tier_availability: "Tier 5",
  developers: ["TESSERA"],
  base_technologies: ["Quantum key distribution", "Single-photon source generation", "One-time pad encryption", "Entanglement-verified security"],
  enables: ["Physically unbreakable encryption", "Interception-proof communication", "Guaranteed-secure financial transactions", "Military-grade command communication"],
  social_impact: "QuantumLock has created a communication tier that is genuinely inaccessible to surveillance. For the corponations and military organizations that can afford it, their most sensitive communications are protected by the laws of physics rather than the difficulty of mathematics. This has implications for the balance of power between corponations and the agencies that attempt to monitor them: conventional wiretapping, network interception, and even quantum computing-based cryptanalysis are all useless against QEC-3 channels. The technology has made the most powerful organizations' communications permanently opaque to oversight — a development that concerns those who believe even corponations should be subject to scrutiny.",
  story_hooks: [
    "A TESSERA QEC-3 channel between two Arcturus facilities flagged an interception alert — but quantum mechanics says interception should be impossible without detection. Either the laws of physics have been violated, or TESSERA's implementation has a flaw they don't know about.",
    "A darknet operator claims to have developed a method for tapping QEC-3 channels without triggering the quantum interception alert. They are selling the method to the highest bidder, and every intelligence agency and corponation in GLMZ is interested."
  ],
  tags: ["infrastructure", "technology", "network", "quantum", "encryption", "communication", "tessera", "tier 5"]
});

emit({
  id: id(),
  name: "Vantablack Media Content Delivery and Censorship Network",
  brand_name: "Vantablack",
  product_name: "MediaFlow CDN-5",
  type: "technology",
  aliases: ["MediaFlow", "CDN-5", "Content Network", "The Feed"],
  subcategory: "infrastructure",
  description: "The MediaFlow CDN-5 is Vantablack Media's content delivery infrastructure — the system that distributes news, entertainment, advertising, and social media content to every BCI and display device in GLMZ. The CDN consists of thousands of edge servers distributed throughout the city, each caching and serving content to nearby devices with minimal latency. When a BCI user accesses any media content, the request is routed to the nearest MediaFlow edge server, which delivers the content from its local cache or retrieves it from Vantablack's central content repositories.\n\nMediaFlow is more than a delivery system — it is a filtering system. Every piece of content that passes through the CDN is scanned by Vantablack's content classification AI, which categorizes material according to Vantablack's content policies and the content access contracts of the user's governing corponation. Content that violates these policies — material critical of specific corponations, information about labor organization, independent news that contradicts Vantablack's sanctioned narratives — is flagged for review, delayed, or silently removed from the delivery pipeline. The user receives no notification that content has been filtered; their feed simply doesn't include it.\n\nThe censorship capabilities are granular and context-sensitive. MediaFlow can filter different content for different users based on their BCI identity, tier, employment, and location. A user in a Ringo-governed block might receive different news than a user two blocks away in an Arcturus-governed block, with each feed tailored to avoid content that the governing corponation has flagged as undesirable. The result is not a single censored internet but millions of individually curated information environments, each shaped by corporate interests that the user may not be aware of. Vantablack's standard content contract costs 50 Φ per user per month — a fee that most governing corponations absorb as a cost of maintaining narrative control over their populations.",
  tier_availability: "Tier 2",
  developers: ["VANTABLACK MEDIA"],
  base_technologies: ["Edge server content caching", "AI content classification", "Corporate-customized content filtering", "BCI-identity-based delivery"],
  enables: ["Low-latency content delivery", "Corporate content control", "Personalized information environments", "Narrative management at scale"],
  social_impact: "MediaFlow has achieved what authoritarian governments of previous centuries only dreamed of: a censorship system so seamless that its subjects don't know they're being censored. The filtered content is not blocked with an error message — it simply doesn't exist in the user's feed. Users in different corporate jurisdictions live in different information realities, each believing they have access to the full picture while seeing only what their governing corponation permits. The only people who see the full, unfiltered content stream are Vantablack's own employees — and even they are subject to internal content policies that limit what they can discuss outside the company.",
  story_hooks: [
    "A Vantablack content analyst has noticed that MediaFlow's AI is filtering content that no corponation has flagged — the system appears to be making independent censorship decisions based on criteria that don't appear in any content policy. The filtered material all relates to a single topic that the AI has, on its own, decided the public should not see.",
    "Two users in the same room, governed by different corponations, are having a conversation about current events and realizing that they have completely different understandings of recent news — the MediaFlow filters have created such divergent information environments that they might as well live in different cities."
  ],
  tags: ["infrastructure", "technology", "network", "media", "censorship", "content", "vantablack", "tier 2"]
});

emit({
  id: id(),
  name: "Axiom Systems Digital Identity Verification Protocol",
  brand_name: "Axiom",
  product_name: "TrueID DVP-4",
  type: "technology",
  aliases: ["TrueID", "DVP-4", "Digital ID", "Identity Protocol"],
  subcategory: "infrastructure",
  description: "The TrueID DVP-4 is the digital identity verification system integrated into every BCI that confirms a user's identity for all network transactions, physical access control, financial operations, and social interactions that require authentication. TrueID generates a cryptographic identity certificate derived from the user's unique BCI hardware signature, biometric data (neural pattern, retinal scan, fingerprint, voice print), and a knowledge factor (a passphrase or neural gesture that only the user can produce). All three factors must align for identity verification to succeed.\n\nThe protocol operates continuously and transparently — the user's BCI constantly broadcasts a low-power identity beacon that nearby systems can query for verification purposes. Walking into a building, making a purchase, or accessing a network resource triggers an automatic TrueID verification that completes in under 50 milliseconds, fast enough to be imperceptible. The user does not need to present identification, swipe a card, or enter a password — their BCI handles all authentication automatically. This convenience has made TrueID the universal identity layer in GLMZ: there is no transaction, no door, no system that does not verify identity through TrueID.\n\nThe implications of universal digital identity are profound. Every interaction is logged. Every movement through a door, every purchase, every network access creates a timestamped identity record in Axiom's verification database. Axiom's privacy policy states that verification records are retained for 7 years and accessible only to the user and to authorities with valid legal process — but the definition of 'authorities' in a corporate sovereignty context includes the governing corponation of the user's residential block, the corponation that owns any building the user enters, and any organization with a valid data-sharing agreement with Axiom. The practical result is near-total surveillance disguised as convenience.",
  tier_availability: "Tier 1",
  developers: ["AXIOM SYSTEMS"],
  base_technologies: ["Multi-factor cryptographic identity", "Continuous identity broadcasting", "Biometric-neural verification", "Universal authentication integration"],
  enables: ["Frictionless identity verification", "Universal access control", "Automated financial authentication", "Continuous location tracking"],
  social_impact: "TrueID has eliminated anonymity in GLMZ for anyone with a BCI — which is nearly everyone. You cannot walk down a street, enter a building, or buy a cup of water without your identity being verified and logged. The system's proponents argue that universal identity verification has virtually eliminated identity fraud and significantly reduced violent crime (criminals can be instantly identified from any verification log). Its critics argue that it has created a world where privacy exists only for those who can afford the extremely specialized and illegal process of identity spoofing — a service that the darknet provides at prices that start at 50,000 Φ and go up from there.",
  story_hooks: [
    "A person has been verified by TrueID in two locations simultaneously — the same identity certificate, confirmed by the same biometric data, in two different buildings 5 kilometers apart. Either TrueID has been compromised, or there are two people with the same identity.",
    "A black-market identity forge has been producing TrueID certificates that pass all verification checks — not by cracking the cryptography, but by cloning the BCI hardware signatures of existing users. The victims don't know their identities have been duplicated until transactions they didn't make appear on their records."
  ],
  tags: ["infrastructure", "technology", "network", "identity", "verification", "bci", "axiom", "tier 1"]
});

emit({
  id: id(),
  name: "Ringo Augmented Reality Overlay Network",
  brand_name: "Ringo",
  product_name: "RingoVision ARON-3",
  type: "technology",
  aliases: ["RingoVision", "ARON-3", "AR Net", "Overlay Network"],
  subcategory: "infrastructure",
  description: "RingoVision ARON-3 is the augmented reality infrastructure layer that projects digital content directly into BCI users' visual perception. The system overlays digital information onto the user's natural vision in real time — navigation markers, product information, social data, advertising, and interactive interfaces all appear as though they exist in physical space. The technology does not use screens or projectors; it works directly through the BCI's neural interface, adding visual data to the brain's visual processing stream before conscious perception occurs.\n\nThe ARON-3 infrastructure consists of spatial anchoring beacons installed throughout GLMZ's physical environment — small devices that broadcast precise location data allowing the BCI to align digital overlays with physical space accurately to within 1 millimeter. Ringo has installed approximately 2 million anchoring beacons across the city, embedded in buildings, street furniture, vehicles, and infrastructure. The beacons are powered by ambient RF energy harvested from the NeuralMesh's transmissions, requiring no dedicated power supply and no maintenance.\n\nThe AR overlay is on by default for all BCI users. The baseline overlay includes navigation assistance, emergency information, and — inevitably — advertising. Users can customize their overlay through Ringo's interface, but the advertising layer cannot be disabled without a premium subscription costing 100 Φ per month. The standard ad load is approximately 40 visual interruptions per hour — product highlights on store fronts, branded navigation markers, sponsored information overlays that replace neutral data with promotional content. The overlay has become so integrated with daily perception that many users report difficulty navigating unfamiliar areas with it disabled — they have lost the ability to read physical signage because they have never needed to.",
  tier_availability: "Tier 2",
  developers: ["RINGO"],
  base_technologies: ["BCI visual cortex integration", "Spatial anchoring beacon network", "Real-time overlay rendering", "Perception-layer content injection"],
  enables: ["Ubiquitous augmented reality", "Neural-integrated navigation", "Perception-layer advertising", "Digital-physical space integration"],
  social_impact: "RingoVision has blurred the line between reality and commercial content to the point of indistinguishability. Users cannot always tell whether what they see is real or overlaid — a problem that Ringo considers a feature, not a bug, because indistinguishable ads are more effective ads. The psychological effects of constant perception-layer content injection are studied but poorly understood: some researchers report increased rates of derealization disorder among heavy AR users, while others note that younger users who have never experienced unaugmented reality show no psychological distress from the overlay because they have no baseline 'real' perception to compare it to.",
  story_hooks: [
    "RingoVision has begun overlaying content that users have not requested and that doesn't match any advertiser's campaign — images of places, people, and events that appear hyperrealistic but correspond to nothing in any database. Someone is using the perception-layer overlay to show people things that don't exist.",
    "A group of users has discovered that disabling their RingoVision overlay reveals physical graffiti and signage that the AR system was covering up — messages, warnings, and art that someone placed in the physical world knowing that AR users would never see them."
  ],
  tags: ["infrastructure", "technology", "network", "ar", "augmented-reality", "ringo", "visual", "tier 2"]
});

emit({
  id: id(),
  name: "TESSERA Satellite Communication Backbone",
  brand_name: "TESSERA",
  product_name: "OrbitLink SCB-4",
  type: "technology",
  aliases: ["OrbitLink", "SCB-4", "Sat Comm", "Orbital Network"],
  subcategory: "infrastructure",
  description: "The OrbitLink SCB-4 is a constellation of 4,000 low-Earth-orbit communication satellites that provides global network connectivity independent of ground-based infrastructure. The constellation operates in orbital shells between 500 and 1,200 kilometers altitude, with inter-satellite laser links creating a mesh network in space that routes data around the planet at the speed of light through vacuum — faster than ground-based fiber optic transmission through glass. For long-distance communication, OrbitLink is actually faster than the DataPike.\n\nEach satellite weighs approximately 250 kilograms and carries phased-array antennas capable of simultaneously serving thousands of ground terminals. The satellites are manufactured by TESSERA's orbital fabrication facility and launched in batches of 60 using Crucible's orbital elevator system. The constellation is self-maintaining: satellites that fail or deorbit are automatically replaced from a reserve of 200 pre-positioned spares, and the routing mesh reconfigures around gaps in real time. The constellation's design lifetime is indefinite — individual satellites are replaced every 5 years, but the constellation itself is designed to operate continuously without interruption.\n\nTESSERA sells OrbitLink access in two tiers: standard access at 500 Φ per month provides 1-gigabit connectivity from any location on Earth, while premium access at 5,000 Φ per month provides 100-gigabit connectivity with priority routing and guaranteed latency below 20 milliseconds. OrbitLink's primary market is organizations that need connectivity outside of urban areas where ground infrastructure exists — remote industrial sites, maritime operations, military deployments, and rural communities. In urban areas, OrbitLink serves as a redundant connection for organizations that cannot afford network downtime, providing an alternative path if the NeuralMesh or DataPike is disrupted.",
  tier_availability: "Tier 3",
  developers: ["TESSERA"],
  base_technologies: ["Low-Earth-orbit satellite mesh", "Inter-satellite laser links", "Phased-array ground communication", "Self-maintaining orbital constellation"],
  enables: ["Global connectivity coverage", "Infrastructure-independent networking", "Faster-than-fiber long-distance communication", "Network redundancy for critical operations"],
  social_impact: "OrbitLink has eliminated geographic isolation as a barrier to network access — in theory. In practice, the 500 Φ monthly cost for standard access is more than many rural communities can afford, and the ground terminals required to access the constellation cost 2,000 Φ each. TESSERA's constellation has become the backbone of communication for maritime, aviation, and remote industrial operations, but its pricing model ensures that the communities most in need of connectivity — remote villages, nomadic populations, refugee camps — remain on the wrong side of the digital divide. TESSERA offers humanitarian access at reduced rates, but the terms require registration with Axiom's TrueID system, which many vulnerable populations are unwilling or unable to complete.",
  story_hooks: [
    "Three OrbitLink satellites have deviated from their assigned orbits and repositioned themselves to provide focused coverage over a specific region of the Pacific Ocean — a region with no known population, infrastructure, or economic activity. TESSERA's orbital control team did not issue the repositioning commands.",
    "An OrbitLink satellite's inter-satellite laser link has been detected carrying encrypted traffic that doesn't match any customer's account — the satellite is being used as a communication relay for an unknown party who has found a way to access the constellation without TESSERA's knowledge."
  ],
  tags: ["infrastructure", "technology", "network", "satellite", "orbital", "communication", "tessera", "tier 3"]
});

// ═══════════════════════════════════════════════
// ADDITIONAL CROSS-CATEGORY ENTRIES (to reach 42)
// ═══════════════════════════════════════════════

emit({
  id: id(),
  name: "Crucible Industries Nanofabrication Assembler Platform",
  brand_name: "Crucible",
  product_name: "NanoForge NAP-6",
  type: "technology",
  aliases: ["NanoForge", "NAP-6", "Nanoassembler", "Molecular Printer"],
  subcategory: "infrastructure",
  description: "The NanoForge NAP-6 is an industrial nanofabrication system that constructs physical objects by assembling them atom by atom from raw feedstock materials. The platform consists of a sealed fabrication chamber — ranging in size from a desktop unit to a room-sized industrial installation — containing billions of microscopic assembly arms controlled by a central AI that coordinates their movements to build complex structures from the molecular level up. The process is slow by conventional manufacturing standards — a desktop unit takes approximately 6 hours to fabricate a component the size of a human hand — but produces objects with material properties impossible to achieve through conventional manufacturing: perfect crystal structures, gradient material compositions, and integrated electronic circuits with no seams or joints.\n\nThe NAP-6 requires feedstock cartridges containing purified elemental materials — carbon, silicon, various metals, and other elements in monoatomic form. Crucible manufactures these cartridges at specialized refinement facilities and sells them through a subscription model: the fabrication platform is sold at a subsidized price, but the feedstock cartridges are proprietary, DRM-locked, and priced at margins that make printer ink look charitable. A kilogram of carbon feedstock costs 800 Φ, compared to 0.50 Φ for industrial-grade carbon on the open market — a markup of 1,600 times, justified by Crucible as the cost of monoatomic purification.\n\nThe technology has found its primary market in medical implant manufacturing (where material perfection is worth the cost), military component production (where performance justifies any expense), and luxury goods (where molecular-precision craftsmanship is its own selling point). Consumer-grade nanofabrication remains impractical due to feedstock costs, but the technology's trajectory suggests eventual mainstream adoption — a prospect that would reshape manufacturing, supply chains, and the very concept of physical goods if feedstock prices ever fall to commodity levels.",
  tier_availability: "Tier 4",
  developers: ["CRUCIBLE INDUSTRIES"],
  base_technologies: ["Molecular-scale assembly arms", "AI-coordinated atomic placement", "Monoatomic feedstock processing", "Sealed environment fabrication"],
  enables: ["Atom-precise manufacturing", "Perfect material structures", "Custom medical implant production", "Integrated molecular electronics"],
  social_impact: "Nanofabrication has made perfection available — at a price. Objects built by NanoForge are materially superior to anything produced by conventional manufacturing, and the gap is visible and measurable. A NanoForge-built blade is sharper, a NanoForge-built joint is smoother, a NanoForge-built circuit is faster. This has created a market for molecular-precision goods that further stratifies consumer culture: those who own NanoForge-built items and those who own everything else. Crucible's feedstock pricing model ensures this stratification persists — they could lower prices and democratize nanofabrication, but the premium market is more profitable.",
  story_hooks: [
    "A NanoForge unit in a medical facility has been building objects during its offline maintenance periods — objects that don't appear in any fabrication queue and whose designs don't exist in the facility's CAD database. The objects are small, complex, and their purpose is unknown.",
    "An underground maker collective has reverse-engineered the NanoForge's feedstock DRM and is producing compatible cartridges at 1% of Crucible's price. The resulting products are indistinguishable from legitimate NanoForge output. Crucible's response has been to embed molecular watermarks in their feedstock that identify legitimate products — and to lobby for legislation making unlicensed nanofabrication a criminal offense."
  ],
  tags: ["infrastructure", "technology", "manufacturing", "nano", "fabrication", "crucible", "molecular", "tier 4"]
});

emit({
  id: id(),
  name: "Lazarus Pharmaceuticals Cryogenic Suspension System",
  brand_name: "Lazarus",
  product_name: "DeepSleep CSS-7",
  type: "technology",
  aliases: ["DeepSleep", "CSS-7", "Cryo Pod", "Cold Sleep"],
  subcategory: "infrastructure",
  description: "The DeepSleep CSS-7 is a cryogenic suspension system that preserves living humans in a state of metabolic arrest for extended periods. The system cools the subject to -196 degrees Celsius over a carefully controlled 4-hour process, replacing blood and cellular fluid with a vitrification solution that prevents ice crystal formation — the primary cause of tissue damage in earlier cryopreservation attempts. The subject is then maintained in a sealed pod filled with liquid nitrogen, monitored by redundant sensor systems that track cellular integrity at the molecular level.\n\nRevival takes approximately 8 hours and involves gradual warming, vitrification solution removal, blood restoration, and neural stimulation to restart brain activity. The process has a success rate of 99.7% for suspension periods under 5 years, declining to 97.2% for periods between 5 and 20 years, and to 91.8% for periods exceeding 20 years. The 8.2% failure rate for long-term suspension — representing irreversible neural damage during revival — is the system's most significant limitation and the subject of intensive ongoing research by Lazarus's cryobiology division.\n\nLazarus markets DeepSleep for three primary applications: medical suspension of patients with currently untreatable conditions who wait for future cures, long-duration space transit where the alternative is years of conscious travel, and — most controversially — voluntary temporal displacement by individuals who wish to skip ahead in time. A DeepSleep suspension costs 50,000 Φ for the initial process plus 2,000 Φ per year of maintenance, making it accessible to upper-tier individuals but not to the general population. Lazarus maintains approximately 12,000 active suspension pods worldwide, with occupants ranging from terminal patients to wealthy eccentrics who decided they would prefer the 23rd century.",
  tier_availability: "Tier 4",
  developers: ["LAZARUS PHARMACEUTICALS"],
  base_technologies: ["Controlled vitrification", "Cellular-level cryopreservation", "Molecular integrity monitoring", "Neural restart stimulation"],
  enables: ["Long-term human preservation", "Medical temporal displacement", "Deep space transit survival", "Future-cure patient storage"],
  social_impact: "DeepSleep has made time travel — in one direction — available to those who can afford it. The ethical implications are unresolved and multiply with each year the technology exists. A person who enters suspension today is betting their life on a future they cannot predict: will the society they wake into honor their property rights, their citizenship, their identity? Will the Φ in their accounts still have value? Will anyone they know still be alive? For medical patients, the calculus is simpler — suspension offers hope when the alternative is death. For voluntary temporal displacement, the calculus is more complex and often driven by despair: people who cannot bear the present and choose to gamble on the future.",
  story_hooks: [
    "A DeepSleep pod has been flagged for revival after 15 years of suspension — but the occupant's maintenance account was funded by an organization that no longer exists, the contact person is dead, and nobody knows who the occupant is or why they were suspended. The occupant's medical records have been sealed by a court order that predates their suspension.",
    "A Lazarus facility has discovered that three of its long-term suspension pods are empty — the occupants are gone, the pods are undamaged, and the surveillance footage shows no one entering or leaving the chamber. The molecular integrity monitors show the occupants' cellular signatures simply stopped registering at different times over the past month."
  ],
  tags: ["infrastructure", "technology", "cryogenic", "suspension", "medical", "lazarus", "preservation", "tier 4"]
});

console.log('\n═══════════════════════════════════');
console.log('DONE — wrote ' + written + ', skipped ' + skipped);
console.log('═══════════════════════════════════');
