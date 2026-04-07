const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'weaponry');

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

const weapons = [

  // ===================== GRENADE LAUNCHERS (5) =====================
  {
    id: uid(),
    name: "Arcturus GL-6 'Sermon'",
    type: "weapon",
    aliases: ["Sermon", "The Preacher", "Sunday Special"],
    category: "grenade launcher",
    description: "A rotary-cylinder grenade launcher holding six 40mm rounds, built with the overengineered brutality that characterizes all Arcturus ordnance. The GL-6 uses a clockwork-style mechanical action — no electronics, no batteries, no firmware to hack. Each cylinder position accepts any standard 40mm casing, allowing operators to load mixed payloads: fragmentation, incendiary, smoke, flashbang, and chemical in whatever combination the situation demands. The weapon's nickname 'Sermon' derives from the practice of loading all six cylinders with different munition types — operators call it 'preaching the full gospel.'\n\nThe GL-6's mechanical simplicity makes it the preferred launcher for operations in EMP-hardened environments or against targets with electronic warfare capability. While competitors have introduced smart-linked launchers with programmable airburst and guided munitions, the Sermon continues to outsell them in actual combat deployments because it always works. No boot sequence, no calibration, no network dependency. Point it at the problem and pull the trigger six times.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Military — restricted deployment authorization; widely available on black market",
    street_price: "Φ8,500",
    base_technologies: ["Mechanical rotary action", "Universal 40mm chamber compatibility", "Recoil-dampened stock assembly"],
    specifications: "caliber: 40mm standard casings\ncylinder capacity: 6 rounds\neffective range: 50-400 meters (munition dependent)\nmuzzle velocity: 76 m/s\nweight: 6.2 kg unloaded\nlength: 680mm\naction: Double-action mechanical rotary",
    tactical_use: "Area denial, fortification clearance, mixed-threat engagement. Operators typically load alternating fragmentation and incendiary rounds for urban combat, or smoke and flashbang for extraction operations. The six-round capacity allows sustained suppression without reloading during short engagements.",
    cultural_context: "The GL-6 has become iconic in Meridian 88's mercenary culture. Freelance operators who carry one are making a statement about their willingness to escalate — grenades are not subtle, and deploying them in urban environments guarantees collateral damage. Carrying a Sermon means you've decided that precision is less important than results.",
    known_users: ["Arcturus contracted security teams", "Meridian 88 Tactical Response heavy weapons squads", "Freelance military contractors", "Several Shelf district militias (black market units)"],
    story_hooks: [
      "A cache of GL-6 launchers has been seized with cylinders pre-loaded with an unknown 40mm round that doesn't match any cataloged munition type. The rounds are heavier than standard and their casings are warm to the touch.",
      "Someone has been firing GL-6 incendiary rounds into abandoned buildings in the Shelf — not randomly, but targeting specific structures. Each building burned contained evidence of something someone wanted destroyed."
    ],
    ammunition_type: ["40mm_grenade"],
    tags: ["grenade launcher", "weapon", "explosive", "arcturus", "military", "mechanical", "tier 3"]
  },
  {
    id: uid(),
    name: "Crucible UGL-2 'Understudy'",
    type: "weapon",
    aliases: ["Understudy", "Piggyback", "Underhung"],
    category: "grenade launcher",
    description: "A single-shot underslung grenade launcher designed to mount beneath the barrel of any standard assault rifle or carbine via universal rail adapter. The UGL-2 adds grenade capability to a primary weapon system without requiring the operator to carry a dedicated launcher, trading capacity for convenience. The break-action loading mechanism accepts standard 40mm casings and can be reloaded in under three seconds by a trained operator.\n\nCrucible designed the Understudy for squad-level operations where dedicated grenade launchers are impractical — room clearing, vehicle patrols, and quick-reaction scenarios where switching between primary weapon and launcher would cost critical seconds. The universal rail adapter fits 94% of assault platforms currently in production, making it the default choice for operators who want explosive capability without the weight penalty of a standalone launcher.",
    manufacturer: "CRUCIBLE INDUSTRIAL",
    tier_availability: "Tier 3+",
    legality: "Military — restricted; Licensed for corporate security with explosive ordnance permit",
    street_price: "Φ3,200",
    base_technologies: ["Universal rail mounting system", "Break-action single-shot mechanism", "Recoil isolation dampener"],
    specifications: "caliber: 40mm standard casings\ncapacity: 1 round\neffective range: 50-350 meters\nweight: 1.8 kg\nlength: 305mm\nmounting: Universal accessory rail",
    tactical_use: "Squad-level explosive support. Operators carry 4-6 rounds in belt pouches and reload between engagements. Used primarily for breaching reinforced doors, disabling vehicles, and flushing targets from cover.",
    cultural_context: "The Understudy is so common in Meridian 88's security ecosystem that 'checking for understudies' has become standard practice when disarming a suspect — the launcher sits flush beneath a rifle barrel and can be missed on visual inspection if you're not looking for it.",
    known_users: ["Corporate security teams across all major corponations", "Meridian 88 Tactical Response", "Licensed private military contractors"],
    story_hooks: [
      "A batch of UGL-2 launchers has been modified to fire a proprietary 40mm round that deploys a localized EMP on detonation — turning a common infantry attachment into a cyberware killer.",
      "An operator's Understudy misfired during a corporate raid, sending a fragmentation round into a residential floor. Crucible claims manufacturing defect. The operator claims the weapon was sabotaged."
    ],
    ammunition_type: ["40mm_grenade"],
    tags: ["grenade launcher", "weapon", "underslung", "attachment", "explosive", "crucible", "military", "tier 3"]
  },
  {
    id: uid(),
    name: "Zheng-Dao Heavy Industries MGL-8 'Thresher'",
    type: "weapon",
    aliases: ["Thresher", "Drum Major", "Harvest"],
    category: "grenade launcher",
    description: "A belt-fed automatic grenade launcher designed for vehicle mounting or emplaced defensive positions. The MGL-8 fires standard 40mm grenades from a 32-round belt at a cyclic rate of 300 rounds per minute, creating a sustained barrage that can suppress an area the size of a city block. The weapon weighs 28 kg without ammunition and is not designed for individual carry — it is a crew-served weapon intended for checkpoints, vehicle turrets, and fixed defensive positions.\n\nZheng-Dao manufactures the Thresher for military customers, but the weapon has appeared in Meridian 88's inter-corporate conflicts when escalation moves beyond small arms. During the Tier 4 corridor dispute of 2196, both sides deployed vehicle-mounted Threshers in residential areas, and the resulting collateral damage killed more civilians than combatants. The weapon is effective precisely because it is indiscriminate — 300 grenades per minute turns everything in the target area into debris regardless of its tactical significance.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military only — prohibited for corporate security deployment (frequently violated)",
    street_price: "Φ45,000",
    base_technologies: ["Belt-fed automatic action", "Hydraulic recoil absorption", "Rapid-traverse mounting gimbal"],
    specifications: "caliber: 40mm standard casings\nfeed: 32-round disintegrating belt\ncyclic rate: 300 rounds per minute\neffective range: 75-1,500 meters\nweight: 28 kg (weapon only)\nmounting: Vehicle turret or tripod emplacement",
    tactical_use: "Area suppression, defensive perimeter enforcement, vehicle convoy protection. Requires a two-person crew for sustained operation — one gunner, one ammunition handler. Effective against light vehicles, personnel in the open, and structures up to reinforced concrete.",
    cultural_context: "The Thresher represents the point where corporate 'security operations' become indistinguishable from warfare. When a Thresher appears, the pretense of proportional response has been abandoned. Journalists covering corporate conflicts use 'Thresher deployment' as shorthand for 'war crime in progress.'",
    known_users: ["Zheng-Dao corporate military forces", "Multiple corponation private armies (officially denied)", "Meridian 88 military reserve (emergency stockpile)"],
    story_hooks: [
      "A Thresher has gone missing from a Zheng-Dao armory. The weapon is crew-served, belt-fed, and weighs 28 kg — not something you tuck under your coat. Someone had logistical support to steal it, and logistical support to use it.",
      "A vehicle mounting a Thresher has been spotted on a freight elevator heading toward the upper tiers. Whoever has it is moving it into position for something, and the target area contains 40,000 residents."
    ],
    ammunition_type: ["40mm_grenade"],
    tags: ["grenade launcher", "weapon", "automatic", "crew-served", "explosive", "zheng-dao", "military", "vehicle", "tier 5"]
  },
  {
    id: uid(),
    name: "Carrion Defense Works CGL-1 'Basilisk'",
    type: "weapon",
    aliases: ["Basilisk", "Smart Lobber", "Eye of God"],
    category: "grenade launcher",
    description: "A smart-linked single-shot grenade launcher with an integrated fire control system that programs airburst fuzes in real time. The operator designates a target through the weapon's optic, the fire control calculates range and trajectory, and programs the 40mm round's electronic fuze to detonate at the precise point in space where fragmentation will be most effective. The result is grenades that explode directly above targets behind cover, in windows, or around corners — places that conventional grenade launchers cannot reliably reach.\n\nCarrion developed the Basilisk for urban operations where targets shelter behind barriers that would stop direct-fire weapons. The programmable airburst capability means that no cover shorter than a sealed overhead structure provides protection — walls, vehicles, dumpsters, and barricades are all rendered irrelevant by a weapon that detonates its payload in the air above them. The psychological effect on targets is severe: the knowledge that your cover means nothing fundamentally changes how people fight.",
    manufacturer: "CARRION DEFENSE WORKS",
    tier_availability: "Tier 4+",
    legality: "Military — restricted deployment; extremely expensive black market",
    street_price: "Φ22,000 (programmable rounds Φ800 each)",
    base_technologies: ["Real-time fuze programming", "Integrated laser rangefinder", "Ballistic computer with wind correction"],
    specifications: "caliber: 40mm programmable airburst\ncapacity: 1 round\neffective range: 50-800 meters\nairburst accuracy: +/- 0.5 meters at 400m\nweight: 4.1 kg\nlength: 750mm\nfire control: Integrated ballistic computer with laser rangefinder",
    tactical_use: "Precision area denial against fortified targets. Each round costs Φ800, making the Basilisk a surgical tool rather than a suppression weapon. Operators use it to eliminate specific high-value targets behind cover, or to clear room interiors through windows without entering.",
    cultural_context: "The Basilisk has earned a reputation as an assassin's tool disguised as military ordnance. A weapon that can place a fragmentation burst inside a specific room from 400 meters away is, functionally, a precision killing tool with explosive radius. The distinction between grenade launcher and guided weapon becomes academic at this level of accuracy.",
    known_users: ["Carrion Defense Works demonstration teams", "Elite corporate strike units", "At least one known freelance assassin operating in Meridian 88"],
    story_hooks: [
      "Three separate assassinations across Meridian 88 used identical Basilisk airburst profiles — same detonation height, same fragmentation pattern, same fire control signature. One weapon, one operator, three targets who had nothing in common except that they were all about to testify in the same corporate tribunal.",
      "A Basilisk round detonated inside a sealed room with no windows and no line of sight from any exterior position. Either the fire control system has capabilities Carrion hasn't disclosed, or someone found a way to program the round after it was in flight."
    ],
    ammunition_type: ["40mm_programmable"],
    tags: ["grenade launcher", "weapon", "smart", "programmable", "airburst", "carrion", "precision", "tier 4"]
  },
  {
    id: uid(),
    name: "Street Custom 'Thumper' Improvised Launcher",
    type: "weapon",
    aliases: ["Thumper", "Shelf Mortar", "Pipe Dream", "Bean Can"],
    category: "grenade launcher",
    description: "A crude single-shot launcher assembled from plumbing pipe, a shotgun firing mechanism, and whatever propellant the builder could acquire. The Thumper fires improvised projectiles — typically tin cans filled with scrap metal and a detonator, or repurposed commercial firework motors with fragmentation sleeves — at ranges between 20 and 100 meters with accuracy best described as 'in that general direction.' The weapon is dangerous to the user nearly as much as the target, with a failure rate that discourages repeat engagements.\n\nThumpers appear throughout the Shelf whenever tensions escalate to the point where residents feel the need for area weapons. They are built in garages and basements from materials that cost less than Φ50, and their improvised munitions are assembled from similarly available components. The weapons are terrifyingly effective despite their crudeness — an explosion doesn't need to be precise to be lethal, and a can full of scrap metal detonating in a crowded space achieves the same result as military fragmentation at a fraction of the cost.\n\nEvery Thumper is unique. There are no production standards, no quality control, and no two weapons that fire the same way. Operators learn their weapon's specific quirks through practice — how far left it pulls, how much the propellant charge varies between shots, whether this particular pipe has a weak seam that might split on the fourth firing. Using a Thumper is an act of faith.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Illegal — improvised explosive device",
    street_price: "Φ50-200 (materials cost)",
    base_technologies: ["Improvised launcher construction", "Scrap fragmentation munitions", "Repurposed propellant systems"],
    specifications: "caliber: Variable (typically 50-80mm pipe bore)\ncapacity: 1 round\neffective range: 20-100 meters (optimistic)\nweight: 2-5 kg depending on construction\nreliability: Approximately 70% successful firing rate",
    tactical_use: "Desperation weapon for area denial when no better options exist. Shelf residents deploy Thumpers against incursions by hostile groups, corporate clearance operations, or rival block conflicts. The weapon's primary tactical value is psychological — the sound of an improvised grenade detonation causes trained operators to take cover, buying time for the defenders.",
    cultural_context: "The Thumper is a symbol of the Shelf's resourcefulness and desperation in equal measure. Building one is a community activity — neighbors contribute pipe, propellant, and scrap. Using one is a statement that the situation has become bad enough to risk blowing off your own hands. Thumper builders are respected in the Shelf the way gunsmiths were respected on historical frontiers.",
    known_users: ["Shelf district block defense groups", "Improvised resistance cells", "Anyone desperate enough"],
    story_hooks: [
      "A Thumper round landed in a corporate security checkpoint and killed two guards. The improvised munition contained fragments of a specific alloy used only in one corponation's manufacturing process — the shrapnel was a message as much as a weapon.",
      "Someone in the Shelf has figured out how to build Thumper rounds that deploy a chemical smoke screen instead of fragmentation — non-lethal area denial for Φ20 per shot. The recipe is spreading through the community, and corporate security is terrified of an entire district that can vanish behind smoke on command."
    ],
    ammunition_type: ["improvised"],
    tags: ["grenade launcher", "weapon", "improvised", "shelf", "street", "explosive", "crude", "tier 1"]
  },

  // ===================== FLARE GUNS AND SIGNAL DEVICES (3) =====================
  {
    id: uid(),
    name: "Crucible SG-3 'Lighthouse'",
    type: "weapon",
    aliases: ["Lighthouse", "Sky Writer", "Beacon Gun"],
    category: "signal device",
    description: "A heavy-duty 26.5mm signal launcher designed for emergency and tactical use, firing parachute-suspended illumination flares that burn at 50,000 candela for 40 seconds, visible at distances exceeding 15 kilometers. The Lighthouse is standard equipment for Meridian 88 emergency services, corporate security patrols, and anyone operating in areas where electronic communications might be jammed or unavailable.\n\nThe SG-3's most common unauthorized use is as an improvised incendiary weapon. The illumination flare burns at 1,200 degrees Celsius and will ignite virtually any flammable material on contact. Shelf residents have discovered that firing a Lighthouse flare into a building's ventilation intake fills the structure with burning magnesium — a crude but effective method of rendering a space uninhabitable. Crucible's lawyers have spent considerable effort establishing that the SG-3 is a signaling device and not a weapon, a distinction that becomes increasingly difficult to maintain as the body count rises.",
    manufacturer: "CRUCIBLE INDUSTRIAL",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — emergency signaling equipment",
    street_price: "Φ180 (flares Φ15 each)",
    base_technologies: ["Magnesium illumination compound", "Parachute descent system", "Break-action launcher"],
    specifications: "caliber: 26.5mm signal\ncapacity: 1 round\nillumination: 50,000 candela, 40-second burn\nvisibility: 15+ kilometers\nflare temperature: 1,200°C\nweight: 0.4 kg\nlength: 220mm",
    tactical_use: "Emergency signaling, area illumination during night operations, and — unofficially — incendiary attacks. Security teams use Lighthouse flares to illuminate pursuit areas and signal for backup. Less scrupulous users fire them into structures, vehicles, and occasionally people.",
    cultural_context: "The Lighthouse occupies a legal gray zone that everyone exploits. It is classified as emergency equipment, so purchasing one requires no permit. It fires a projectile that burns at 1,200 degrees, so using one offensively is attempted murder. The gap between these two facts contains an entire economy of plausible deniability.",
    known_users: ["Emergency services", "Corporate security patrols", "Ship and aircraft crews", "Shelf residents (for various purposes)"],
    story_hooks: [
      "A series of arson attacks across the lower tiers have been carried out with Lighthouse flares — fired through windows at 3 AM, each targeting a specific apartment in a specific building. The targets are all witnesses in the same ongoing investigation.",
      "Someone has modified SG-3 flares to carry chemical payloads instead of illumination compounds, turning an unrestricted signaling device into a chemical weapon delivery system that anyone can buy without a permit."
    ],
    ammunition_type: ["26.5mm_signal"],
    tags: ["signal", "weapon", "flare", "incendiary", "emergency", "crucible", "fire", "tier 1"]
  },
  {
    id: uid(),
    name: "Ringo DS-1 'Screamer'",
    type: "weapon",
    aliases: ["Screamer", "Noise Maker", "Panic Button"],
    category: "signal device",
    description: "A compact signal device that fires a 20mm acoustic flare — a projectile that generates a 160-decibel omnidirectional sound burst upon reaching its apex at approximately 50 meters altitude. The Screamer was designed as a distress signal for civilians in danger, producing a distinctive wailing tone that carries for 3 kilometers and is registered in Meridian 88's emergency response network. The acoustic signature triggers automatic alert protocols in nearby security systems and BCI-equipped responders.\n\nRingo markets the DS-1 as a personal safety device, and it has legitimate applications in that role. The 160-decibel burst at close range is also incapacitating — well above the pain threshold and capable of causing temporary deafness and disorientation. Users have discovered that firing a Screamer round at ground level rather than skyward converts a signal device into a non-lethal area weapon that clears rooms, disperses crowds, and provides cover for escape. The round's acoustic profile is intense enough to trigger nausea and loss of balance in unprotected individuals within 10 meters.",
    manufacturer: "RINGO ENTERTAINMENT DIVISION",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — personal safety device",
    street_price: "Φ95 (rounds Φ8 each)",
    base_technologies: ["Piezoelectric acoustic generation", "Altitude-triggered activation", "Emergency network integration"],
    specifications: "caliber: 20mm acoustic\ncapacity: 1 round\nacoustic output: 160 dB at 1 meter\naudible range: 3 kilometers\nactivation altitude: 50 meters (adjustable to ground-level)\nweight: 0.2 kg\nlength: 140mm",
    tactical_use: "Emergency signaling and improvised area denial. Ground-level detonation creates a disorienting blast that buys 5-10 seconds of confusion. Multiple rounds fired in sequence can create sustained acoustic disruption. Not designed for combat but frequently used in it.",
    cultural_context: "The Screamer is the Shelf's cheapest force multiplier. At Φ8 per round, even the poorest residents can afford a handful. Block defense groups stockpile them as early-warning systems, firing Screamers when hostile groups enter their territory. The wailing sound has become the Shelf's air-raid siren — when you hear it, something bad is coming.",
    known_users: ["Civilian self-defense", "Shelf block defense groups", "Night-shift workers in dangerous districts", "Runners and couriers as emergency escape tools"],
    story_hooks: [
      "Every Screamer fired in Meridian 88 is logged by the emergency network, creating a real-time map of distress signals. Someone has been analyzing the pattern and found that Screamer activations in one district spike exactly 15 minutes before corporate security raids — someone is using distress signals as a warning system.",
      "A modified Screamer round has surfaced that generates a specific frequency capable of triggering seizures in individuals with certain BCI models. It's being sold as a personal safety device. The frequency was not discovered by accident."
    ],
    ammunition_type: ["20mm_acoustic"],
    tags: ["signal", "weapon", "acoustic", "non-lethal", "personal", "ringo", "safety", "shelf", "tier 1"]
  },
  {
    id: uid(),
    name: "Vantablack IR-2 'Ghost Light'",
    type: "weapon",
    aliases: ["Ghost Light", "Invisible Flare", "Spook Signal"],
    category: "signal device",
    description: "A covert signal launcher that fires an infrared flare invisible to the naked eye but detectable by any IR-capable optic, BCI overlay, or surveillance system. The Ghost Light is designed for tactical signaling between operators who need to communicate position, status, or rally points without revealing themselves to observers who lack IR capability. The flare burns for 60 seconds at altitude and can encode simple messages through pulse patterns.\n\nVantablack developed the IR-2 for its own security operations, where maintaining covert communications during field operations required a fallback system independent of electronic networks. The Ghost Light has since become standard equipment for anyone operating in Meridian 88's shadow economy — runners, smugglers, extraction teams, and criminal organizations use IR flare patterns as a communication system that is invisible to most observers and leaves no electronic trace.\n\nThe weapon's dual nature emerges from its targeting application. An IR flare fired at ground level marks a location — a vehicle, a building, a person — with an infrared beacon that is invisible to the target but visible to anyone with IR optics. Snipers use Ghost Lights to mark targets for engagement teams. Drone operators use them to designate strike coordinates. An invisible mark that only your allies can see is a death sentence the target never knows they've received.",
    manufacturer: "VANTABLACK MOBILITY",
    tier_availability: "Tier 2+",
    legality: "Restricted — military and licensed security operators",
    street_price: "Φ450 (IR flares Φ40 each)",
    base_technologies: ["Narrow-band infrared emission", "Pulse-pattern encoding", "Low-thermal-signature combustion"],
    specifications: "caliber: 15mm infrared\ncapacity: 1 round\nemission: Narrow-band infrared, invisible to naked eye\nburn time: 60 seconds\ndetection range: 5 km with standard IR optics\nweight: 0.3 kg\nlength: 180mm",
    tactical_use: "Covert signaling, target designation, and rally point marking. Operators fire Ghost Lights to coordinate movement without radio communication, mark extraction points, or designate targets for supporting elements. The IR flare is invisible to anyone without appropriate optics, providing a significant tactical advantage over conventional signals.",
    cultural_context: "The Ghost Light has created a parallel communication layer in Meridian 88 — a sky full of invisible signals that only certain eyes can see. Runners with IR-capable BCIs can read the night sky like a message board, and the flare patterns have evolved into an informal sign language. 'Reading ghosts' is runner slang for intercepting these covert signals.",
    known_users: ["Vantablack corporate security", "Freelance extraction teams", "Smuggling networks", "Runner collectives with IR-capable BCIs"],
    story_hooks: [
      "A Ghost Light pattern has appeared above the Shelf that doesn't match any known operator's signal vocabulary — a new code, used by an unknown group, marking locations that correspond to municipal infrastructure access points. Someone is mapping entry routes into the city's core systems using invisible light.",
      "An operator has discovered that their Ghost Light flares are being tracked — someone has deployed a network of IR sensors that logs every invisible flare fired in the district, decoding the signal patterns and mapping the covert communication network."
    ],
    ammunition_type: ["15mm_infrared"],
    tags: ["signal", "weapon", "infrared", "covert", "tactical", "vantablack", "communications", "tier 2"]
  },

  // ===================== TASERS AND STUN WEAPONS (5) =====================
  {
    id: uid(),
    name: "TESSERA SC-7 'Compliance'",
    type: "weapon",
    aliases: ["Compliance", "Corporate Handshake", "The Convincer"],
    category: "stun weapon",
    description: "A handheld directed-energy stun device that delivers a calibrated electrical discharge through two barbed probes connected to the weapon by conductive microfilament wires. The SC-7 is TESSERA's flagship less-lethal sidearm, designed for corporate security personnel who need to incapacitate subjects without the liability of ballistic weapons. The device fires its probes at ranges up to 10 meters, and the electrical discharge is calibrated to cause involuntary muscle contraction and temporary incapacitation lasting 5-15 seconds depending on the target's body mass and augmentation level.\n\nThe 'calibrated' part is what distinguishes — and damns — the SC-7. The weapon's onboard processor analyzes the electrical impedance of the target's body through the probe connection and adjusts the discharge to maximize incapacitation while remaining below documented lethality thresholds. This sounds humane until you realize that the calibration database includes cyberware profiles — the SC-7 knows what augmentations the target has and adjusts its discharge to exploit their electrical characteristics. Against a heavily augmented target, the weapon doesn't just cause muscle contraction; it sends feedback spikes through cybernetic systems that cause the target's own implants to malfunction, creating pain responses and motor disruption far beyond what the electrical discharge alone would achieve.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 2+",
    legality: "Licensed — corporate security and law enforcement",
    street_price: "Φ1,800",
    base_technologies: ["Adaptive impedance analysis", "Microfilament probe delivery", "Cyberware-profile exploitation firmware"],
    specifications: "range: 10 meters (probe delivery)\ndischarge: 50,000 volts, adaptive amperage\nincapacitation duration: 5-15 seconds\nprobe type: Barbed titanium with microfilament tether\ncharges per cartridge: 3 firings\nweight: 0.6 kg\nlength: 195mm",
    tactical_use: "Subject incapacitation in corporate security operations, arrest support, and crowd control. Security teams use the SC-7 as a first-response tool before escalating to ballistic weapons. The cyberware exploitation feature makes it particularly effective against augmented targets who might otherwise resist conventional stun weapons.",
    cultural_context: "The SC-7 is standard issue for corporate security across Meridian 88, and its nickname 'Corporate Handshake' reflects public perception of how corponations greet people who disagree with them. The weapon's cyberware exploitation feature has drawn protests from augmentation rights groups who argue it constitutes targeted violence against the augmented community.",
    known_users: ["Corporate security forces (all major corponations)", "Meridian 88 law enforcement", "Licensed private security firms"],
    story_hooks: [
      "The SC-7's cyberware profiling database has been leaked, revealing that TESSERA has cataloged the electrical vulnerabilities of every major cyberware platform on the market — including experimental military augmentations that aren't supposed to exist in civilian populations.",
      "A modified SC-7 has surfaced that inverts the calibration — instead of staying below lethality thresholds, it maximizes discharge through cyberware systems to cause permanent implant damage. Someone is turning a compliance tool into a cyberware destroyer."
    ],
    ammunition_type: ["energy_cell"],
    tags: ["stun", "weapon", "electrical", "non-lethal", "tessera", "corporate", "cyberware", "tier 2"]
  },
  {
    id: uid(),
    name: "Arcturus BW-3 'Bonesaw'",
    type: "weapon",
    aliases: ["Bonesaw", "Lightning Stick", "The Dentist's Drill"],
    category: "stun weapon",
    description: "A stun baton measuring 55 centimeters in length, delivering a focused electrical discharge through a hardened contact head designed for direct application. Unlike probe-based stun weapons, the BW-3 requires physical contact, which Arcturus markets as a feature — the weapon is a melee tool for close-quarters control that combines blunt-force impact with electrical incapacitation. The discharge cycles at 20 pulses per second, creating a sustained shock effect that continues as long as the contact head remains pressed against the target.\n\nThe BW-3 is standard equipment in Arcturus-managed detention facilities and has a reputation for misuse that Arcturus has done nothing to address. The weapon's sustained-contact design means it can be applied to a restrained subject for any duration the operator chooses, and the 20-pulse-per-second cycle creates a specific kind of pain that subjects describe as having their nervous system rewritten in real time. Detention oversight reports have documented BW-3 application lasting minutes against restrained detainees — far beyond any tactical justification.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 2+",
    legality: "Licensed — security and detention personnel",
    street_price: "Φ900",
    base_technologies: ["Pulsed electrical discharge", "Impact-hardened contact head", "Sustained application cycling"],
    specifications: "length: 55cm\ncontact discharge: 40,000 volts, 20 pulses/second\npower source: Rechargeable cell, 200 applications per charge\nimpact rating: Equivalent to aluminum baseball bat\nweight: 0.8 kg",
    tactical_use: "Close-quarters subject control in detention, crowd control, and security operations. The BW-3 combines the deterrent effect of a visible melee weapon with electrical incapacitation capability. Primarily used against unarmed or restrained subjects.",
    cultural_context: "The BW-3 is the weapon most associated with institutional violence in Meridian 88. Detention survivors describe it with the kind of specificity that indicates trauma — the exact sound it makes, the precise sensation, the way the pulses feel different depending on where the contact head is applied. 'Getting the bonesaw' is Shelf slang for any experience of systematic, methodical cruelty.",
    known_users: ["Arcturus detention facility guards", "Corporate security response teams", "Private security firms", "Known to be available on black market for Φ500"],
    story_hooks: [
      "A former detention guard has come forward with BW-3 application logs showing that specific detainees were subjected to sustained electrical discharge on a schedule — not as punishment, but as what appears to be a systematic experiment in pain tolerance mapping.",
      "Modified BW-3 units have appeared in the Shelf with the safety limiters removed, allowing discharge levels that cause cardiac arrest. They're being sold for Φ200, and someone is flooding the market deliberately."
    ],
    ammunition_type: ["energy_cell"],
    tags: ["stun", "weapon", "melee", "electrical", "baton", "arcturus", "detention", "violence", "tier 2"]
  },
  {
    id: uid(),
    name: "Lazarus NB-4 'Jellyfish'",
    type: "weapon",
    aliases: ["Jellyfish", "Nerve Net", "Tangle Zapper"],
    category: "stun weapon",
    description: "A launcher that fires a 2-meter conductive mesh net that wraps around the target and delivers a sustained electrical discharge through its entire surface area. The Jellyfish combines physical restraint with electrical incapacitation — the target is simultaneously immobilized by the weighted net and subjected to a full-body shock that prevents any coordinated muscle movement to escape. The net's conductive fibers are woven with shape-memory alloy that contracts upon deployment, tightening around the target and increasing electrical contact area.\n\nLazarus designed the NB-4 for live capture operations where the target must be taken intact — corporate espionage extraction, witness retrieval, and bounty collection. The Jellyfish is the preferred tool for operatives who need their target alive, conscious, and unable to resist. The net's shape-memory contraction means that struggling makes the restraint tighter and the electrical contact more complete, creating a negative feedback loop that punishes resistance until the target stops moving.\n\nThe weapon's most disturbing characteristic is its duration. The net's integrated battery sustains the electrical discharge for up to three minutes — far longer than necessary for incapacitation. Lazarus claims the extended duration is a safety feature that ensures the target remains incapacitated during operator approach. Critics note that three minutes of full-body electrical discharge causes lasting nerve damage and psychological trauma that serves no legitimate operational purpose.",
    manufacturer: "LAZARUS BIOWORKS",
    tier_availability: "Tier 3+",
    legality: "Restricted — licensed capture operations only",
    street_price: "Φ3,500 (nets Φ600 each, single-use)",
    base_technologies: ["Conductive mesh deployment", "Shape-memory alloy contraction", "Sustained full-body discharge"],
    specifications: "range: 8 meters\nnet diameter: 2 meters deployed\ndischarge: 30,000 volts sustained, 3-minute battery\nnet weight: 1.2 kg\nlauncher weight: 2.1 kg\nlength: 340mm\nreload: Single-use net cartridge",
    tactical_use: "Live capture and restraint. Operators fire the Jellyfish at targets who must be taken alive, then approach once the electrical discharge has ensured compliance. The net can be deactivated manually by the operator or will exhaust its battery after three minutes. Effective against augmented targets — the full-body discharge affects biological and cybernetic systems simultaneously.",
    cultural_context: "The Jellyfish has become associated with forced extraction and kidnapping. Being 'jellied' is slang for being captured against your will, and the weapon's distinctive deployment sound — a wet snap as the net expands — is recognized and feared in communities where extraction operations are common. Bounty hunters who carry Jellyfish launchers are treated with particular wariness.",
    known_users: ["Corporate extraction teams", "Licensed bounty hunters", "Lazarus Bioworks security forces", "Known to be used by criminal kidnapping operations"],
    story_hooks: [
      "A Jellyfish net has been recovered from a crime scene with modifications — the electrical discharge has been replaced with a chemical payload that absorbs through the skin on contact. Someone is converting restraint tools into poison delivery systems.",
      "A bounty hunter's Jellyfish launcher has been traced to three separate abductions — but the bounty hunter died six months ago. Someone is using a dead operator's equipment and license to conduct extractions under a ghost identity."
    ],
    ammunition_type: ["net_cartridge"],
    tags: ["stun", "weapon", "net", "electrical", "capture", "lazarus", "restraint", "bounty", "tier 3"]
  },
  {
    id: uid(),
    name: "Ringo SP-2 'Buzzkill'",
    type: "weapon",
    aliases: ["Buzzkill", "Party Stopper", "Fun Police"],
    category: "stun weapon",
    description: "A compact stun pistol the size of a derringer, designed as a concealed personal defense weapon for civilians and corporate employees in Meridian 88's mid-tier districts. The SP-2 fires a single capacitor-driven arc across two exposed electrodes at the weapon's muzzle, delivering a contact-range shock powerful enough to cause involuntary muscle spasm and disorientation for 3-5 seconds. The weapon must be physically pressed against the target to discharge, giving it effectively zero range but making it nearly impossible to detect in a pocket or handbag.\n\nRingo markets the Buzzkill as a personal safety device — 'your last line of defense' — and sells it in a range of colors and finishes targeted at the corporate professional demographic. The weapon has become a fashion accessory as much as a tool, with designer versions available from Ringo's lifestyle division. The normalization of carrying a concealed electrical weapon has had predictable consequences: assault-with-Buzzkill incidents in Meridian 88's nightlife districts have increased 300% since the weapon's introduction, and 'getting buzzed' has become a common experience in clubs, bars, and crowded transit.",
    manufacturer: "RINGO ENTERTAINMENT DIVISION",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — personal safety device",
    street_price: "Φ120",
    base_technologies: ["Capacitor-driven arc discharge", "Miniaturized power cell", "Contact-activation safety"],
    specifications: "range: Contact only\ndischarge: 25,000 volts, single pulse\nincapacitation: 3-5 seconds disorientation\ncharges: 15 per power cell\nweight: 0.1 kg\nlength: 80mm\nform factor: Derringer-sized, pocket concealment",
    tactical_use: "Last-resort personal defense at contact range. Users press the weapon against an attacker and fire, using the momentary incapacitation to create distance and escape. Not designed for sustained combat or repeated application.",
    cultural_context: "The Buzzkill has become ubiquitous in Meridian 88's middle tiers — carried by office workers, retail employees, students, and anyone else who can afford Φ120 for a sense of security. Its prevalence has created a new social dynamic where physical contact with strangers carries a risk of electrical assault, contributing to the city's culture of personal space enforcement through technology.",
    known_users: ["Mid-tier civilian population (widespread)", "Corporate employees", "Nightlife venue staff", "Students"],
    story_hooks: [
      "A modified Buzzkill has killed someone — a Φ120 personal safety device with an aftermarket capacitor that delivers a lethal discharge. The modification costs Φ30 and takes ten minutes. Instructions are circulating on underground networks.",
      "Ringo's sales data for Buzzkill units has been cross-referenced with assault reports, revealing that the districts with the highest Buzzkill sales also have the highest assault rates. The weapon designed to prevent violence is enabling it."
    ],
    ammunition_type: ["energy_cell"],
    tags: ["stun", "weapon", "concealed", "personal", "electrical", "ringo", "civilian", "pocket", "tier 1"]
  },
  {
    id: uid(),
    name: "Vantablack PA-1 'Pacifier'",
    type: "weapon",
    aliases: ["Pacifier", "Quiet Down", "The Muzzle"],
    category: "stun weapon",
    description: "A rifle-format directed-energy weapon that fires a focused microwave beam at ranges up to 50 meters, heating the target's skin surface to create an intense burning sensation without causing visible injury. The Active Denial System technology was developed decades ago but miniaturized by Vantablack into a shoulder-fired platform that a single operator can carry and deploy. The Pacifier causes immediate reflexive retreat — the pain is so intense that targets abandon their position, drop held objects, and flee the beam path involuntarily.\n\nVantablack sells the PA-1 exclusively to corporate clients for crowd dispersal and perimeter defense. The weapon's appeal is its deniability — it causes extreme pain but leaves no marks, no wounds, and no forensic evidence. A crowd hit by Pacifier beams will disperse screaming, but medical examination will find nothing wrong with them. This makes the PA-1 the preferred tool for suppressing protests, demonstrations, and labor actions where visible casualties would create negative publicity.\n\nThe PA-1's limitations are well-documented but rarely discussed in marketing materials. The beam penetrates clothing to approximately 1/64th of an inch of skin depth, which means that metallic fabrics, wet clothing, or improvised reflective barriers significantly reduce effectiveness. The weapon also cannot distinguish between targets — the beam affects everyone in its path, including bystanders, children, and individuals with medical conditions that make heat exposure dangerous.",
    manufacturer: "VANTABLACK MOBILITY",
    tier_availability: "Tier 4+",
    legality: "Restricted — corporate security with crowd control authorization",
    street_price: "Φ35,000",
    base_technologies: ["Miniaturized active denial system", "Focused millimeter-wave emission", "Thermal pain induction"],
    specifications: "range: 50 meters effective\nbeam width: 2 meters at 30 meters\neffect: Intense skin heating (54°C surface temperature)\npower source: Heavy-duty battery pack, 30 minutes continuous operation\nweight: 8.5 kg with battery\nlength: 900mm",
    tactical_use: "Crowd dispersal, perimeter defense, and area denial. Operators sweep the beam across groups to force retreat. Effective against unarmed civilians and lightly equipped targets. Ineffective against prepared opponents with reflective or wet barrier materials.",
    cultural_context: "The Pacifier is the weapon that labor organizers fear most — not because it is the most dangerous, but because it leaves no evidence. Workers hit by Pacifier beams during strike actions cannot prove they were attacked. Medical records show no injuries. Security footage shows people running away from nothing. The weapon's name is a cruel joke: it pacifies by causing invisible agony.",
    known_users: ["Vantablack corporate security", "Multiple corponation crowd control units", "Meridian 88 riot suppression teams (denied)"],
    story_hooks: [
      "A Pacifier has been modified to operate at higher power, causing second-degree burns instead of surface heating. The modification makes the weapon's effects visible and documentable — someone is deliberately creating evidence of corporate violence that the original weapon was designed to conceal.",
      "Medical researchers have identified a cluster of cancer cases in a district where Pacifier deployments were frequent during a prolonged labor dispute. Vantablack insists the technology is safe. The epidemiological data suggests otherwise."
    ],
    ammunition_type: ["energy_cell"],
    tags: ["stun", "weapon", "directed-energy", "microwave", "crowd-control", "vantablack", "corporate", "pain", "tier 4"]
  },

  // ===================== CROSSBOWS AND BOWS (5) =====================
  {
    id: uid(),
    name: "Ouroboros XB-7 'Silence'",
    type: "weapon",
    aliases: ["Silence", "Whisper Bow", "The Quiet"],
    category: "crossbow",
    description: "A compound crossbow with electromagnetic limb assists that boost bolt velocity to 150 m/s while maintaining the near-zero acoustic signature that makes crossbows valuable in a world of ubiquitous sound detection. The XB-7 uses carbon-fiber limbs with integrated electromagnetic coils that supplement mechanical energy with a precisely timed electromagnetic pulse at the moment of release, adding 40% velocity without the sound or muzzle signature of a firearm.\n\nOuroboros designed the Silence for operators who cannot afford to be heard — assassination, covert infiltration, and elimination operations in environments where any acoustic signature would trigger automated detection systems. Meridian 88's upper tiers are saturated with gunshot detection networks that triangulate firearms discharge within 2 seconds. A crossbow bolt arrives before the sound detection system registers anything, because there is nothing to register. The bolt is supersonic; the weapon is not.\n\nThe XB-7 accepts a range of specialized bolts that exploit the platform's precision delivery: explosive-tipped bolts for materiel destruction, neurotoxin-coated bolts for guaranteed kills, EMP bolts for electronics disruption, and climbing bolts with integrated grapple lines. The crossbow format makes each bolt a potential platform for payloads too delicate for the acceleration forces of firearms.",
    manufacturer: "OUROBOROS SYSTEMS",
    tier_availability: "Tier 3+",
    legality: "Restricted — classified as silent weapon; possession requires specialized license",
    street_price: "Φ6,500",
    base_technologies: ["Electromagnetic limb assist", "Carbon-fiber compound limb system", "Modular bolt interface"],
    specifications: "bolt velocity: 150 m/s (electromagnetic assist)\neffective range: 80 meters\nmagazine: 6-bolt rotary magazine\npower source: Integrated cell, 50 assisted shots per charge\nweight: 3.2 kg\nlength: 720mm (limbs folded: 440mm)\nacoustic signature: Below ambient urban noise floor",
    tactical_use: "Silent elimination, covert materiel destruction, and payload delivery in sound-monitored environments. Operators use the XB-7 when firearms would trigger detection systems, or when the operation requires plausible distance between the act and the operator. The 6-bolt magazine allows rapid follow-up shots without the single-shot limitation of traditional crossbows.",
    cultural_context: "The crossbow has experienced a renaissance in Meridian 88's shadow economy specifically because of sound detection technology. As the city's acoustic surveillance became more sophisticated, demand for silent weapons increased. The XB-7 represents the high end of this market — a precision killing tool that exists because the alternative is getting caught.",
    known_users: ["Corporate black operations teams", "Freelance assassination specialists", "Ouroboros security forces", "At least three known serial killers active in Meridian 88"],
    story_hooks: [
      "A series of killings has been carried out with XB-7 bolts that carry no payload — clean mechanical kills, precise shot placement, no forensic trace except the bolt itself. The bolts are custom-machined and carry a maker's mark that doesn't match any known manufacturer. Someone is signing their work.",
      "An Ouroboros engineer has been found dead with one of the company's own XB-7 bolts through the throat. The bolt's electromagnetic signature matches a weapon that was reported destroyed three years ago."
    ],
    ammunition_type: ["crossbow_bolt"],
    tags: ["crossbow", "weapon", "silent", "assassination", "ouroboros", "covert", "electromagnetic", "tier 3"]
  },
  {
    id: uid(),
    name: "Street Custom 'Gallows' Recurve Bow",
    type: "weapon",
    aliases: ["Gallows", "Shelf Bow", "String Killer", "Poor Man's Silence"],
    category: "bow",
    description: "A handmade recurve bow assembled from salvaged composite materials — typically automotive leaf springs, industrial cable for the string, and scrap polymer for the riser. The Gallows represents the lowest-tech weapon in Meridian 88 that remains genuinely lethal at range. With no electromagnetic components, no battery, no firmware, and no acoustic signature, it is invisible to every detection system in the city. It is a stick that throws smaller sticks, and in a world of networked surveillance, that simplicity is a superpower.\n\nGallows bows are built by hand in the Shelf, each one unique, each one reflecting the builder's available materials and skill level. Draw weights range from 20 to 60 kilograms depending on the spring steel used, and accuracy depends entirely on the archer's practice. The arrows are similarly improvised — metal rod shafts with filed points, stabilized with whatever passes for fletching. Despite the crude construction, a Gallows arrow through the throat kills as effectively as any precision-manufactured projectile.\n\nThe bow has become a symbol of Shelf resistance culture — a weapon that the surveillance state literally cannot see. No electronics to detect, no chemical propellant to sniff, no electromagnetic signature to track. A person with a Gallows bow and sufficient skill can kill from rooftop distance and leave no forensic trace that any automated system can process. The kill has to be investigated by humans, and there aren't enough investigators to cover the Shelf.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Unregulated — not classified as a weapon under current Meridian 88 ordinances",
    street_price: "Φ30-100 (materials cost)",
    base_technologies: ["Salvaged composite construction", "Zero electronic signature", "Improvised ammunition"],
    specifications: "draw weight: 20-60 kg (varies by construction)\neffective range: 30-60 meters (operator dependent)\narrow velocity: 50-80 m/s\nweight: 1-2 kg\nacoustic signature: Negligible\nelectronic signature: None",
    tactical_use: "Silent elimination and harassment in environments where every other weapon would be detected. Shelf archers operate from rooftops and elevated positions, firing into streets and corridors below. The weapon's lack of any electronic signature makes it impossible to trace through automated systems — investigation requires physical evidence and human detective work.",
    cultural_context: "The Gallows bow has become a folk icon in the Shelf. Archery practice is a community activity, and skilled archers are respected as defenders. The weapon's name references both its killing capability and the improvised gallows-frame shape of the bow when strung. 'Drawing the Gallows' is Shelf slang for taking a stand against impossible odds.",
    known_users: ["Shelf district defenders", "Anti-corporate resistance cells", "Individuals who cannot afford or access firearms", "At least one vigilante known as 'the Fletcher'"],
    story_hooks: [
      "A corporate executive was killed by an arrow — a sharpened metal rod fired from a Gallows bow — in the middle of a secured Tier 4 district. Every surveillance system in the area recorded nothing. The arrow came from somewhere that doesn't exist according to the building plans.",
      "Someone is teaching archery classes in the Shelf, training dozens of residents in Gallows bow construction and marksmanship. The classes are free, the instructor is unknown, and the skill level of the graduates is suspiciously professional."
    ],
    ammunition_type: ["arrow"],
    tags: ["bow", "weapon", "silent", "improvised", "shelf", "street", "resistance", "analog", "tier 1"]
  },
  {
    id: uid(),
    name: "Arcturus HCB-3 'Longbow'",
    type: "weapon",
    aliases: ["Longbow", "Heavy Bolt", "Rail Bow"],
    category: "crossbow",
    description: "A heavy tactical crossbow using full electromagnetic acceleration — essentially a miniature railgun formatted as a crossbow for ergonomic handling. The HCB-3 fires steel bolts at 300 m/s, sufficient to penetrate light vehicle armor and most personal protection systems. Unlike the Ouroboros XB-7, which supplements mechanical energy with electromagnetic assist, the Longbow is purely electromagnetic — there are no physical limbs, no string, and no mechanical energy storage. The 'crossbow' designation is a legal fiction that allows the weapon to be classified outside firearms regulations.\n\nArcturus exploits this classification loophole aggressively. The HCB-3 is, functionally, a coilgun that fires projectiles at rifle velocities with reduced acoustic signature. Its classification as a 'crossbow' means it is subject to archery regulations rather than firearms restrictions in jurisdictions that still maintain that distinction. The weapon requires no ammunition permit, no ballistic registration, and produces no cartridge casings for forensic analysis. It fires a featureless steel rod that carries no identifying marks.\n\nThe Longbow's acoustic signature is not zero — the electromagnetic acceleration produces a distinctive electrical snap that sound detection systems can register — but it is significantly quieter than conventional firearms and does not match any firearm acoustic profile in standard detection databases. By the time the system identifies the sound as a weapon discharge rather than an electrical fault, the bolt has already arrived.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Classified as archery equipment (contested); functionally restricted",
    street_price: "Φ14,000",
    base_technologies: ["Full electromagnetic acceleration", "Ferromagnetic bolt design", "Acoustic signature reduction"],
    specifications: "bolt velocity: 300 m/s\neffective range: 200 meters\nmagazine: 8-bolt linear magazine\npower source: High-capacity cell, 25 shots per charge\nweight: 5.1 kg\nlength: 800mm\npenetration: Rated for 8mm steel plate at 50 meters",
    tactical_use: "Covert anti-materiel and anti-personnel operations where firearms would be detected or where the operator needs forensically clean kills. The Longbow's bolts carry no rifling marks, no propellant residue, and no ejected casings. Investigation of a Longbow killing requires finding the bolt — a featureless steel rod indistinguishable from industrial stock.",
    cultural_context: "The HCB-3 represents the absurdity of Meridian 88's weapons regulation — a weapon that fires projectiles at rifle velocity is legally a crossbow because its manufacturer called it one, and the regulatory framework hasn't caught up. Arms control advocates cite the Longbow as proof that weapons law in Meridian 88 exists to serve manufacturers, not citizens.",
    known_users: ["Arcturus covert operations teams", "Corporate assassination specialists", "High-end freelance operators who value forensic deniability"],
    story_hooks: [
      "A politician advocating for reclassification of electromagnetic crossbows has been killed by an HCB-3 bolt. The message is clear. The forensic evidence is nonexistent.",
      "An HCB-3 has been recovered with a modified bolt magazine that feeds from a backpack-mounted autoloader holding 200 bolts — someone has converted a precision assassination tool into an automatic weapon."
    ],
    ammunition_type: ["steel_bolt"],
    tags: ["crossbow", "weapon", "electromagnetic", "railgun", "silent", "arcturus", "covert", "tier 4"]
  },
  {
    id: uid(),
    name: "TESSERA CB-5 'Viper Strike'",
    type: "weapon",
    aliases: ["Viper Strike", "Poison Bow", "Chemical Bow"],
    category: "crossbow",
    description: "A compact repeating crossbow with an integrated chemical payload injection system. The CB-5 fires short bolts from a 12-bolt gravity-feed magazine, each bolt hollow and loaded with 0.5ml of operator-selected chemical agent that is injected into the wound channel upon impact through a pressure-activated plunger mechanism. The bolt itself causes minimal physical damage — a 4mm puncture wound that might go unnoticed in a chaotic environment. The chemical payload does the killing.\n\nTESSERA supplies the CB-5 with a catalog of approved chemical loadouts: fast-acting paralytic agents for capture operations, delayed-onset neurotoxins for deniable assassination, and incapacitating sedatives for extraction. The weapon's true danger is its versatility — any liquid agent that fits in a 0.5ml reservoir can be loaded, and the underground market has developed payloads that TESSERA never intended: synthetic opioid overdose loads, corrosive agents, and tailored biotoxins designed for specific genetic profiles.\n\nThe CB-5 is nearly silent, fires rapidly, and delivers chemical payloads that can kill hours after exposure. It is the preferred weapon for operators who need their target to die somewhere else, at some other time, from what looks like something other than murder.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Prohibited — classified as chemical weapon delivery system",
    street_price: "Φ11,000 (chemical loadouts Φ200-2,000 per bolt)",
    base_technologies: ["Pressure-activated chemical injection", "Gravity-feed repeating mechanism", "Micro-reservoir bolt design"],
    specifications: "bolt velocity: 60 m/s\neffective range: 25 meters\nmagazine: 12-bolt gravity feed\nchemical payload: 0.5ml per bolt\nbolt diameter: 4mm\nweight: 2.0 kg\nlength: 380mm\nacoustic signature: Below ambient noise floor",
    tactical_use: "Chemical payload delivery for assassination, capture, and covert operations. Operators select bolts loaded with appropriate agents for the mission objective. The weapon's low bolt velocity and small wound profile mean targets may not realize they've been hit until the chemical payload takes effect.",
    cultural_context: "The CB-5 is feared because it represents death by ambiguity. A target hit by a Viper Strike bolt might feel a brief sting and think nothing of it until the paralytic kicks in an hour later. In a city where people are constantly jostled, bumped, and crowded, a 4mm puncture wound is indistinguishable from everyday urban abrasion. The weapon exploits the anonymity of city life as a delivery mechanism.",
    known_users: ["TESSERA covert operations division", "Corporate espionage units", "At least two known contract killers specializing in deniable operations"],
    story_hooks: [
      "A series of apparently unrelated deaths across Meridian 88 — heart failure, stroke, respiratory arrest — have one thing in common: each victim has a 4mm puncture wound that was missed during initial examination. Someone is conducting a systematic assassination campaign with a Viper Strike, and the targets don't know they're dying until it's too late.",
      "A CB-5 bolt has been recovered loaded with an agent that doesn't appear in any chemical database. Analysis reveals it's a tailored biotoxin engineered to affect only individuals with a specific genetic marker found in approximately 2% of Meridian 88's population. This is a weapon designed for ethnic targeting."
    ],
    ammunition_type: ["chemical_bolt"],
    tags: ["crossbow", "weapon", "chemical", "poison", "silent", "assassination", "tessera", "covert", "tier 4"]
  },
  {
    id: uid(),
    name: "Ouroboros SB-1 'Arbalest'",
    type: "weapon",
    aliases: ["Arbalest", "Siege Bow", "Wall Breaker"],
    category: "crossbow",
    description: "A vehicle-mounted heavy crossbow platform that fires 1-meter steel bolts weighing 5 kilograms at velocities sufficient to penetrate reinforced concrete walls. The Arbalest uses a hydraulic draw mechanism that charges a bank of torsion springs generating 400 kilograms of draw force, combined with a short electromagnetic acceleration stage that brings the bolt to terminal velocity. The weapon is mounted on a powered gimbal that allows 180-degree traverse and 45-degree elevation from a vehicle roof or fixed position.\n\nOuroboros developed the Arbalest as a breaching tool for tactical teams that need to penetrate fortified positions without explosive ordnance. A 5-kilogram steel bolt arriving at 200 m/s punches through walls, doors, barricades, and light vehicles with the kinetic energy of a small cannon round, but without the explosive signature, fragmentation hazard, or ammunition regulation of conventional heavy weapons. Like the Longbow, the Arbalest exploits the legal fiction that anything firing a bolt is a crossbow.\n\nThe weapon's most common deployment is punching access holes through walls during forced-entry operations — a single Arbalest bolt creates a 15-centimeter breach that operators can use as a firing port, observation point, or anchor for breaching charges. Three bolts in a triangular pattern create a hole large enough for a person to enter. The process is slower than explosive breaching but creates no blast injury risk to occupants the entry team wants alive.",
    manufacturer: "OUROBOROS SYSTEMS",
    tier_availability: "Tier 4+",
    legality: "Restricted — vehicle-mounted weapon permit required",
    street_price: "Φ28,000",
    base_technologies: ["Hydraulic torsion spring system", "Electromagnetic terminal acceleration", "Powered gimbal mounting"],
    specifications: "bolt length: 1 meter\nbolt weight: 5 kg\nbolt velocity: 200 m/s\neffective range: 150 meters\npenetration: 200mm reinforced concrete\nreload time: 8 seconds (hydraulic draw)\nweapon weight: 85 kg (excluding mount)\nmounting: Vehicle roof or fixed emplacement",
    tactical_use: "Structural breaching, fortified position neutralization, and anti-vehicle operations. The Arbalest replaces explosive breaching in operations where blast effects are unacceptable. Vehicle-mounted units provide mobile siege capability for urban operations teams.",
    cultural_context: "The Arbalest is the most absurd example of the crossbow classification loophole — a vehicle-mounted siege weapon that fires 5-kilogram steel spears through concrete walls, legally classified as archery equipment. Its existence has become a running joke in arms regulation discourse and a serious argument for legal reform that will never happen because the manufacturers lobby harder than the reformers.",
    known_users: ["Ouroboros tactical entry teams", "Corporate security rapid-response units", "Meridian 88 special operations (denied)"],
    story_hooks: [
      "An Arbalest bolt has been found embedded in the wall of a Tier 5 corporate executive's private residence — fired from street level, it punched through three interior walls before stopping. No follow-up attack occurred. It was a message: we can reach you through your walls.",
      "Someone has mounted an Arbalest on a stolen freight vehicle and is driving through the Shelf, firing bolts through the walls of buildings suspected to house corporate surveillance equipment. They're hitting their targets with suspicious accuracy."
    ],
    ammunition_type: ["heavy_bolt"],
    tags: ["crossbow", "weapon", "vehicle-mounted", "siege", "breaching", "ouroboros", "heavy", "tier 4"]
  },

  // ===================== CONCEALED / DISGUISED WEAPONS (8) =====================
  {
    id: uid(),
    name: "TESSERA PG-2 'Signature'",
    type: "weapon",
    aliases: ["Signature", "Pen Gun", "Executive Decision"],
    category: "concealed weapon",
    description: "A single-shot .22 caliber firearm disguised as a premium writing instrument. The Signature is 14 centimeters long, weighs 45 grams, and is visually indistinguishable from the TESSERA-branded executive pens distributed to thousands of corporate employees. The firing mechanism is activated by removing the pen cap and pressing the pocket clip forward — a gesture that looks identical to preparing to write. The weapon fires a single .22 round from the pen barrel with enough velocity to be lethal at contact range.\n\nTESSERA does not officially manufacture the Signature. The company's position is that the PG-2 was a proof-of-concept prototype that was never approved for production. This explanation is complicated by the fact that functional PG-2 units appear consistently on the black market with genuine TESSERA manufacturing stamps, serial numbers that match TESSERA's proprietary format, and build quality that no counterfeit operation could replicate. Someone at TESSERA is producing these weapons. The company's legal department maintains plausible deniability.\n\nThe Signature's lethality is limited — a single .22 round from a 14-centimeter barrel has minimal stopping power — but its purpose is not battlefield effectiveness. It exists for the moment when a corporate executive sits across the table from someone who needs to die, and needs to die right now, in a room where weapons were supposedly impossible to bring.",
    manufacturer: "TESSERA (officially denied)",
    tier_availability: "Tier 4+",
    legality: "Prohibited — disguised weapon",
    street_price: "Φ4,500",
    base_technologies: ["Miniaturized firing mechanism", "Visual concealment design", "Subsonic .22 integration"],
    specifications: "caliber: .22 LR\ncapacity: 1 round\neffective range: 3 meters (contact weapon)\nbarrel length: 90mm\nweight: 45g\nlength: 140mm\nform factor: Indistinguishable from TESSERA executive pen",
    tactical_use: "Close-range assassination in environments where weapons screening has been passed. The Signature is a weapon of last resort or premeditated close-quarters killing. Users must be within arm's reach of their target and accept that they will have one shot with no follow-up capability.",
    cultural_context: "The Signature has achieved near-mythical status in Meridian 88's corporate culture. Every TESSERA pen is now regarded with slight suspicion, and security-conscious executives have been known to refuse writing instruments offered by others. The weapon has poisoned the most basic gesture of professional courtesy.",
    known_users: ["TESSERA covert assets (denied)", "Corporate assassination specialists", "High-value targets who carry one for personal protection"],
    story_hooks: [
      "A corporate executive was killed in a secure boardroom by a Signature round during a negotiation. Every person in the room was screened. Every person in the room is carrying a TESSERA pen. The weapon has not been found because it is hiding in plain sight among a dozen identical pens.",
      "A batch of PG-2 units has been intercepted with modified ammunition — the .22 round replaced with a chemical injector that delivers a delayed-action toxin. The pen doesn't even need to be fired. It just needs to scratch the target."
    ],
    ammunition_type: ["22_lr"],
    tags: ["concealed", "weapon", "disguised", "pen", "assassination", "tessera", "corporate", "covert", "tier 4"]
  },
  {
    id: uid(),
    name: "Ouroboros CS-4 'Gentleman'",
    type: "weapon",
    aliases: ["Gentleman", "Walking Stick", "Sunday Cane"],
    category: "concealed weapon",
    description: "A carbon-fiber walking cane containing a 60-centimeter monomolecular-edged blade concealed within the shaft. The blade is drawn by twisting the cane's handle 90 degrees and pulling — a motion that takes approximately 0.3 seconds for a practiced user. The blade's monomolecular edge can sever unarmored limbs, and the cane's 90-centimeter overall length provides reach advantage over conventional bladed weapons. The remaining shaft section serves as a parrying tool or blunt-force weapon after the blade is drawn.\n\nOuroboros manufactures the Gentleman for a specific clientele: wealthy individuals in Meridian 88's upper tiers who face personal security threats but cannot be seen carrying weapons without damaging their social position. The cane is crafted from genuine carbon fiber with premium fittings, weighted for comfortable walking use, and finished to standards that justify its Φ8,000 price point as a luxury accessory. In a social environment where visible weapons are considered gauche, the Gentleman allows its owner to be armed while appearing civilized.\n\nThe weapon's monomolecular edge is its most dangerous feature and its primary maintenance burden. The edge degrades with each use and requires professional resharpening that only Ouroboros-certified technicians can perform. This creates a dependency chain — Gentleman owners must maintain a relationship with Ouroboros to keep their weapon functional, and Ouroboros's maintenance records constitute a database of every person in Meridian 88 who is carrying a concealed monomolecular blade.",
    manufacturer: "OUROBOROS SYSTEMS",
    tier_availability: "Tier 4+",
    legality: "Prohibited — concealed bladed weapon; widely owned in upper tiers with zero enforcement",
    street_price: "Φ8,000",
    base_technologies: ["Monomolecular edge fabrication", "Quick-draw concealment mechanism", "Carbon-fiber structural engineering"],
    specifications: "blade length: 60cm\nedge: Monomolecular (requires periodic resharpening)\ncane length: 90cm overall\nweight: 0.7 kg\ndraw time: 0.3 seconds (practiced user)\nmaterial: Carbon fiber shaft, titanium fittings",
    tactical_use: "Personal defense and premeditated close-quarters combat. The Gentleman provides a concealed lethal option in social environments where firearms or visible weapons would be inappropriate. The blade's reach and cutting power compensate for the user's likely lack of formal combat training.",
    cultural_context: "The Gentleman is an open secret in Meridian 88's upper tiers. Everyone knows that the carbon-fiber canes carried by certain executives and socialites are weapons. Nobody acknowledges it. The legal prohibition against concealed blades is enforced exclusively in the lower tiers, creating a two-tier justice system where the wealthy carry monomolecular swords and the poor are arrested for pocketknives.",
    known_users: ["Upper-tier executives and socialites", "Retired corporate security personnel", "Ouroboros board members (complimentary issue)"],
    story_hooks: [
      "A Gentleman blade has been used in a killing at a charity gala — a monomolecular cut through the carotid, fast enough that the victim was dead before anyone noticed. Every guest with a carbon-fiber cane is now a suspect, and there are fourteen of them.",
      "Ouroboros's maintenance records have been stolen, revealing the identity of every Gentleman owner in Meridian 88. Someone is selling the list to people who want to know which members of the upper tier are secretly armed — and which are not."
    ],
    ammunition_type: [],
    tags: ["concealed", "weapon", "disguised", "cane", "blade", "monomolecular", "ouroboros", "luxury", "melee", "tier 4"]
  },
  {
    id: uid(),
    name: "Street Custom 'Last Word' Ring Gun",
    type: "weapon",
    aliases: ["Last Word", "Knuckle Pop", "Ring Piece", "Handshake"],
    category: "concealed weapon",
    description: "A single-shot firearm built into an oversized ring worn on the index or middle finger, firing a .22 short round from a barrel concealed within the ring's decorative setting. The weapon is activated by pressing the palm-side trigger plate with the thumb while the hand is clenched into a fist. The round fires forward from the ring face, roughly aligned with a pointing gesture. Accuracy is nonexistent beyond 2 meters, but at contact range — pressed against a target's body during a handshake, an embrace, or a struggle — the weapon is lethal.\n\nLast Word rings are handmade in the Shelf by jeweler-gunsmiths who have combined their crafts into a single, desperate art form. Each ring is unique, built around whatever firing mechanism and barrel stock the maker can acquire. The external appearance ranges from crude metal bands to surprisingly sophisticated designs that mimic legitimate jewelry. The best makers produce rings that pass casual inspection, with firing mechanisms hidden behind gemstone settings that swivel aside to reveal the barrel.\n\nThe weapon has a grim reputation because of the circumstances of its use. Nobody carries a ring gun as a primary weapon. It is the weapon you carry when you expect to be searched, expect to be held at gunpoint, and expect that the only chance you'll get is when someone comes close enough to touch. It is named the Last Word because it is intended to be used when every other option has been exhausted.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Illegal — disguised weapon",
    street_price: "Φ300-800",
    base_technologies: ["Miniaturized firing mechanism", "Jewelry-integrated concealment", "Contact-range ballistics"],
    specifications: "caliber: .22 Short\ncapacity: 1 round\neffective range: Contact to 2 meters\nweight: 30-60g depending on construction\nform factor: Oversized ring, worn on index or middle finger",
    tactical_use: "Absolute last-resort weapon for contact-range use when all other options have failed. The Last Word is carried as insurance against scenarios where the user will be disarmed, restrained, or cornered. Its single round must count.",
    cultural_context: "The Last Word ring is a Shelf institution. Owning one means you live in a world where you might need to kill someone with your hands. The rings are sometimes given as gifts between people who work dangerous jobs — runners, couriers, debt collectors — as a gesture that says 'I want you to come home.' The craftsmanship of the ring reflects the maker's regard for the wearer.",
    known_users: ["Shelf residents in high-risk occupations", "Runners and couriers", "Individuals under active threat", "Anyone who expects to be searched"],
    story_hooks: [
      "A corporate security executive was killed by a .22 Short round fired at contact range during what appeared to be a handshake. The shooter was a Shelf resident who had been brought in for 'questioning.' Nobody checked the rings.",
      "A master ring-maker in the Shelf has started producing Last Words with a twist — the ring contains a microphone and transmitter that activates when the firing mechanism is charged, broadcasting a final recording to a preset recipient. People are using them to send messages from beyond the grave."
    ],
    ammunition_type: ["22_short"],
    tags: ["concealed", "weapon", "disguised", "ring", "jewelry", "street", "shelf", "assassination", "tier 1"]
  },
  {
    id: uid(),
    name: "Vantablack BC-1 'Briefcase'",
    type: "weapon",
    aliases: ["Briefcase", "Board Meeting", "Executive Summary"],
    category: "concealed weapon",
    description: "A fully functional submachine gun integrated into a leather executive briefcase, with the barrel concealed behind a false panel on one end and the trigger mechanism accessible through a hidden grip inside the case handle. The weapon fires 9mm rounds at 600 RPM from a 30-round magazine housed in a compartment beneath a functional document holder, allowing the case to contain actual papers and still function as a weapon. The operator aims by pointing the briefcase and fires by squeezing the grip — no need to open the case or visibly deploy the weapon.\n\nVantablack produces the BC-1 for executive protection details who need to maintain the appearance of unarmed civilian staff while carrying squad-level firepower. The briefcase passes visual inspection — it looks, weighs, and feels like a premium leather case containing documents. X-ray screening reveals the weapon's presence, but Vantablack sells the BC-1 with proprietary X-ray-scattering liners that make the internal components appear as electronics — a laptop, a tablet, normal business equipment. Only a physical search or a screening operator who knows exactly what to look for will identify the weapon.\n\nThe BC-1's most alarming capability is its firing posture. The weapon can be fired while being carried normally at the operator's side — no aiming motion, no weapon deployment, no visible change in the carrier's behavior. The first indication that a BC-1 has been deployed is the sound of 9mm rounds exiting what appears to be a piece of luggage.",
    manufacturer: "VANTABLACK MOBILITY",
    tier_availability: "Tier 5",
    legality: "Prohibited — disguised automatic weapon",
    street_price: "Φ18,000",
    base_technologies: ["Integrated weapon concealment", "X-ray scatter lining", "Covert firing mechanism"],
    specifications: "caliber: 9mm\nrate of fire: 600 RPM\nmagazine: 30 rounds\neffective range: 25 meters (unbraced)\nbriefcase dimensions: 450mm x 320mm x 100mm\ntotal weight: 3.8 kg loaded\nfiring method: Concealed grip in handle",
    tactical_use: "Close-protection emergency response and covert offensive operations. The BC-1 provides automatic weapons capability to operators who must appear unarmed. Used primarily in executive protection scenarios where the threat level justifies concealed firepower but the social context prohibits visible weapons.",
    cultural_context: "The BC-1 has contributed to Meridian 88's pervasive atmosphere of invisible threat. The knowledge that any briefcase might be a weapon has made corporate environments subtly more tense — security details eye each other's luggage, and the phrase 'let me set down my briefcase' has acquired an ominous double meaning in corporate negotiation culture.",
    known_users: ["Vantablack executive protection details", "Corporate security teams requiring covert firepower", "At least one documented use in a corporate boardroom massacre"],
    story_hooks: [
      "Security footage from a corporate massacre shows the attacker walking calmly through a lobby, firing a BC-1 from their briefcase without breaking stride. Thirty people died in 90 seconds. The attacker passed through the building's security screening without incident.",
      "A BC-1 has been found in the luggage of a diplomat entering Meridian 88 — complete with Vantablack manufacturing stamps and a serial number that traces to an order placed by a corponation that claims it was never delivered."
    ],
    ammunition_type: ["9mm"],
    tags: ["concealed", "weapon", "disguised", "briefcase", "submachine gun", "automatic", "vantablack", "corporate", "tier 5"]
  },
  {
    id: uid(),
    name: "Lazarus MR-1 'Mercy'",
    type: "weapon",
    aliases: ["Mercy", "Doctor's Orders", "Medical Exception"],
    category: "concealed weapon",
    description: "A single-shot injector weapon disguised as a standard medical auto-injector, the kind carried by millions of people in Meridian 88 for medication delivery, allergy response, and BCI maintenance. The Mercy looks, weighs, and functions identically to a Lazarus-manufactured medical device — because it is a medical device, with one modification. The injection mechanism can be loaded with any liquid payload, and the delivery needle has been lengthened and reinforced to penetrate through light clothing and reach major blood vessels.\n\nLazarus officially produces the Mercy for clandestine medical operations — field medics who need to administer emergency treatment to unwilling or unconscious patients in dangerous environments. The weapon's medical camouflage means it passes screening checkpoints, security inspections, and even casual physical searches, because every third person in Meridian 88 carries an auto-injector for legitimate medical reasons. Hiding a weapon in the most common object in a city is a form of genius that borders on cruelty.\n\nThe Mercy's payload determines its lethality. Loaded with a sedative, it puts the target to sleep. Loaded with a paralytic, it immobilizes them. Loaded with potassium chloride, it induces cardiac arrest that is nearly impossible to distinguish from natural heart failure. The weapon is the delivery system; the intent lives in the chemistry.",
    manufacturer: "LAZARUS BIOWORKS",
    tier_availability: "Tier 3+",
    legality: "Classified as medical device (weaponized use prohibited)",
    street_price: "Φ2,200 (payloads Φ100-5,000 depending on agent)",
    base_technologies: ["Medical auto-injector modification", "Reinforced delivery needle", "Universal liquid payload compatibility"],
    specifications: "delivery method: Intramuscular injection\npayload volume: 2ml\nneedle penetration: Through light clothing, 25mm depth\nactivation: Thumb-pressure trigger (identical to medical use)\nweight: 35g\nlength: 120mm\nform factor: Indistinguishable from standard Lazarus auto-injector",
    tactical_use: "Covert chemical delivery at contact range. The operator approaches the target in any setting where close proximity is normal — crowded transit, medical examination, social gathering — and administers the injection through a brief physical contact. The gesture is identical to the accidental bump of a medical device in a pocket.",
    cultural_context: "The Mercy has made auto-injectors frightening. In a city where millions of people carry medical devices that could be weapons, every accidental brush with a stranger's pocket becomes a potential assassination. Paranoid individuals have taken to carrying their auto-injectors in transparent cases to demonstrate they contain legitimate medication. The most intimate medical device has been weaponized, and trust in basic healthcare technology has been damaged.",
    known_users: ["Lazarus clandestine medical teams", "Corporate extraction specialists", "Contract assassins specializing in undetectable kills", "Medical professionals operating in hostile environments"],
    story_hooks: [
      "A wave of cardiac arrests in a specific corporate department — seven deaths in three months, all attributed to stress and overwork. A junior medical examiner has noticed that each victim has a tiny puncture wound in the same location on the upper arm, consistent with an auto-injector deployment.",
      "A Mercy loaded with an unknown agent has been recovered from a crime scene. Analysis reveals the payload is not a toxin — it's a gene therapy vector that rewrites specific neural pathways over 48 hours, leaving the target alive but fundamentally altered. Someone is using medical weapons to change who people are."
    ],
    ammunition_type: ["chemical_payload"],
    tags: ["concealed", "weapon", "disguised", "medical", "injector", "chemical", "lazarus", "assassination", "covert", "tier 3"]
  },
  {
    id: uid(),
    name: "Street Custom 'Rosary' Bead Garrote",
    type: "weapon",
    aliases: ["Rosary", "Prayer Beads", "The Confession"],
    category: "concealed weapon",
    description: "A garrote wire concealed within a string of decorative beads worn as a bracelet or necklace. The beads are machined from tungsten carbide and threaded on a 1mm monofilament wire with breaking strength of 400 kilograms. When deployed — unclasped and drawn taut between two hands — the wire cuts through unprotected tissue in seconds, and the tungsten beads provide grip points that prevent the wire from slipping through the operator's hands.\n\nRosary garrotes are Shelf craft — assembled by hand from industrial monofilament and machined beads, then disguised as costume jewelry. They are worn openly, passed through security checkpoints without comment, and carried in environments where no other weapon could enter. The Rosary exploits the most fundamental assumption of weapons screening: jewelry is not dangerous.\n\nThe weapon requires close physical contact and significant upper-body strength to deploy effectively, limiting its use to ambush or grapple scenarios. But its concealability is unmatched — a Rosary looks like a piece of inexpensive jewelry, weighs almost nothing, and shows up on X-ray as exactly what it appears to be: a string of metal beads. Only someone who recognizes the specific bead material and wire type would identify it as a weapon, and that requires expertise that most security personnel do not possess.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Unregulated — classified as jewelry (weaponized use constitutes assault)",
    street_price: "Φ150",
    base_technologies: ["Monofilament wire", "Tungsten carbide bead machining", "Jewelry concealment"],
    specifications: "wire material: 1mm monofilament, 400 kg breaking strength\nbead material: Tungsten carbide, 8mm diameter\ntotal length: 60cm deployed\nweight: 85g\nform factor: Bracelet or necklace",
    tactical_use: "Close-quarters assassination from ambush or grapple position. The Rosary requires the operator to be behind or above the target with both hands free. Once deployed, the monofilament cuts through neck tissue faster than the target can respond. The weapon leaves a distinctive wound that experienced forensic examiners can identify.",
    cultural_context: "The Rosary garrote occupies a strange cultural space in the Shelf — it is both a weapon and a genuine piece of community jewelry. Many Shelf residents wear monofilament bead strings without any intention of using them as weapons, simply because they are inexpensive and attractive. This makes identifying actual weapon-carriers impossible. The ambiguity is the point.",
    known_users: ["Shelf residents (both as jewelry and weapon)", "Assassins requiring zero-detection-profile equipment", "Anyone who expects to be thoroughly searched"],
    story_hooks: [
      "A corporate bodyguard was killed with a Rosary garrote in a crowd — the attacker deployed the wire, made the cut, and vanished into the press of bodies in under three seconds. Security footage shows dozens of people wearing similar bead jewelry. The killer is in the footage. So are forty innocent people wearing the same accessory.",
      "A jeweler in the Shelf has been arrested for manufacturing Rosary garrotes — but the arrest is cover for something else. The jeweler's client list includes names from the upper tiers, people who shouldn't know the Shelf exists, let alone be buying weapons from it."
    ],
    ammunition_type: [],
    tags: ["concealed", "weapon", "disguised", "garrote", "jewelry", "monofilament", "street", "shelf", "melee", "tier 1"]
  },
  {
    id: uid(),
    name: "Crucible UB-2 'Umbrella'",
    type: "weapon",
    aliases: ["Umbrella", "Rain Check", "The Canopy"],
    category: "concealed weapon",
    description: "A spring-loaded pneumatic dart launcher concealed in the shaft of a full-sized umbrella. The weapon fires a single 5mm steel dart from the umbrella's tip using compressed CO2 stored in the shaft, delivering the projectile with enough velocity to penetrate light clothing and embed 30mm into flesh. The dart can be loaded with chemical payloads — the hollow tip holds 0.3ml of liquid agent — combining the delivery mechanism of the Mercy auto-injector with the range of a firearm.\n\nCrucible designed the UB-2 for intelligence operations during a period of heightened corporate espionage in the 2180s, and the weapon has remained in covert circulation ever since. The firing mechanism is activated by pressing a concealed button on the handle while pointing the umbrella's tip at the target — a gesture that looks like adjusting one's grip or preparing to open the umbrella. The CO2 discharge produces a quiet hiss that is masked by ambient urban noise.\n\nThe Umbrella is perhaps the most classically espionage-coded weapon in Meridian 88's arsenal — a direct descendant of Cold War assassination tools updated with modern materials and chemistry. Its continued existence is a reminder that some methods of killing are timeless, and that the most effective disguise for a weapon is still 'something everyone carries when it rains.'",
    manufacturer: "CRUCIBLE INDUSTRIAL (covert division)",
    tier_availability: "Tier 4+",
    legality: "Prohibited — disguised weapon",
    street_price: "Φ5,500",
    base_technologies: ["Pneumatic dart propulsion", "Concealed CO2 reservoir", "Chemical payload dart design"],
    specifications: "projectile: 5mm hollow-tip steel dart\ncapacity: 1 dart\nrange: 5 meters\npropulsion: Compressed CO2\nchemical payload: 0.3ml\numbrella length: 85cm\nweight: 0.6 kg\nacoustic signature: Quiet hiss, masked by ambient noise",
    tactical_use: "Covert assassination and chemical agent delivery at short range. The operator approaches within 5 meters, fires while performing a natural umbrella-handling gesture, and walks away. The dart's small profile and the weapon's silent operation mean the target may not realize they've been hit until the chemical payload takes effect.",
    cultural_context: "The Umbrella weapon has achieved legendary status in Meridian 88's intelligence community, partly because of its historical pedigree and partly because it works. In a city where it rains 200 days a year, everyone carries an umbrella. The weapon's disguise is the weather itself.",
    known_users: ["Corporate intelligence operatives", "Crucible covert division assets", "Historical connection to at least seven confirmed assassinations in Meridian 88"],
    story_hooks: [
      "A political figure collapsed during a public appearance on a rainy day. Autopsy revealed a 5mm dart embedded in the thigh, loaded with a neurotoxin. Security footage shows 300 people with umbrellas. The investigation is effectively impossible.",
      "A cache of UB-2 units has been discovered in a Crucible facility that was supposedly decommissioned — along with records of a covert assassination program that spans two decades and three continents."
    ],
    ammunition_type: ["dart"],
    tags: ["concealed", "weapon", "disguised", "umbrella", "dart", "pneumatic", "chemical", "crucible", "espionage", "tier 4"]
  },
  {
    id: uid(),
    name: "Ringo FB-1 'Earworm'",
    type: "weapon",
    aliases: ["Earworm", "Sound Weapon", "Bass Drop"],
    category: "concealed weapon",
    description: "A directional sonic weapon disguised as a portable Bluetooth speaker — the kind carried by millions of Meridian 88 residents for personal entertainment. The Earworm looks, functions, and even plays music identically to Ringo's commercial speaker line. Pressing a hidden combination of buttons activates the weapon mode, which redirects the speaker's transducer array into a focused acoustic beam that delivers 140 dB of directed sound energy at frequencies calibrated to cause nausea, disorientation, and incapacitating pain in a 15-degree cone extending 10 meters from the device.\n\nRingo developed the Earworm for personal security applications — a non-lethal defensive tool that exploits technology already present in consumer electronics. The weapon's disguise is nearly perfect because it is a real speaker with a hidden secondary function, not a weapon shaped like a speaker. It plays music. It connects to BCIs and personal devices. It charges via standard cable. The acoustic weapon capability is a firmware modification that can be activated or deactivated remotely.\n\nThe Earworm's most insidious feature is its potential for mass deployment. Ringo has sold millions of speakers. If a firmware update pushed to all units activated the weapon mode, every Ringo speaker in Meridian 88 would become a sonic weapon simultaneously. Ringo's terms of service include a clause granting the company rights to modify device firmware at any time. No one has tested whether this capability exists. No one wants to ask.",
    manufacturer: "RINGO ENTERTAINMENT DIVISION",
    tier_availability: "Tier 2+",
    legality: "Classified as consumer electronics (weapon mode unauthorized)",
    street_price: "Φ800 (firmware activation Φ500 additional)",
    base_technologies: ["Directional acoustic beam forming", "Dual-mode transducer array", "Remote firmware activation"],
    specifications: "acoustic output: 140 dB directed beam\nbeam angle: 15 degrees\neffective range: 10 meters\nfrequency: Variable, optimized for incapacitation\nbattery: 8 hours music / 20 minutes weapon mode\nweight: 0.4 kg\ndimensions: 180mm x 80mm x 80mm\nform factor: Portable Bluetooth speaker",
    tactical_use: "Personal defense and area denial. The operator activates weapon mode, points the speaker at the threat, and the directed acoustic beam causes immediate incapacitation. Bystanders outside the 15-degree beam angle are unaffected. The weapon mode drains the battery rapidly, limiting sustained use.",
    cultural_context: "The Earworm represents the weaponization of consumer technology taken to its logical endpoint. Every electronic device is a potential weapon if the manufacturer chooses to make it one. The Earworm exists because Ringo discovered that the hardware they were already selling to millions of people could hurt them, and decided that was a feature.",
    known_users: ["Ringo security personnel", "Corporate employees with firmware-activated units", "Black market customers who purchased activation codes"],
    story_hooks: [
      "A Ringo firmware update has bricked thousands of speakers — but analysis reveals that the update didn't destroy the speakers. It activated their weapon mode and then locked the controls. Thousands of acoustic weapons are now live in homes, offices, and public spaces across Meridian 88, waiting for a second signal.",
      "Someone is using an Earworm to systematically drive residents out of a specific Shelf block — activating the weapon at random intervals, day and night, making the area uninhabitable. The target block sits on a piece of real estate that a developer wants to acquire."
    ],
    ammunition_type: ["energy_cell"],
    tags: ["concealed", "weapon", "disguised", "speaker", "sonic", "acoustic", "ringo", "consumer", "tier 2"]
  },

  // ===================== ANTI-MATERIEL RIFLES (3) =====================
  {
    id: uid(),
    name: "Zheng-Dao Heavy Industries AMR-14 'Earthquake'",
    type: "weapon",
    aliases: ["Earthquake", "Building Killer", "The Shaker"],
    category: "anti-materiel rifle",
    description: "A bolt-action anti-materiel rifle chambered in 20mm, firing explosive-incendiary rounds designed to destroy vehicles, equipment, and light structures at ranges exceeding 2 kilometers. The AMR-14 weighs 25 kilograms and requires a bipod or fixed mounting for accurate fire — this is not a weapon that can be fired from the shoulder. Each round carries enough explosive filler to detonate a vehicle's fuel system, penetrate 30mm of armor plate, or blow a hole through a concrete wall large enough to put a fist through.\n\nZheng-Dao manufactures the Earthquake for military customers who need to destroy hard targets at extreme range without deploying crew-served weapons or calling for air support. A single operator with an AMR-14 can disable vehicles, destroy communications equipment, detonate ammunition stores, and render defensive positions untenable from a distance where the target cannot effectively respond with small arms. The weapon's report is catastrophic — the 20mm round produces a shockwave that shatters windows within 10 meters of the firing position and announces the shooter's location to everything within a kilometer.\n\nThe Earthquake has appeared in Meridian 88's inter-corporate conflicts in roles its designers never intended. During the Tier 3 corridor wars, both sides used AMR-14s to fire through building walls at targets in adjacent structures — the 20mm explosive round penetrating exterior walls, interior walls, and anyone in between. The weapon turned entire buildings into kill zones, as occupants discovered that their walls provided no protection against a round designed to destroy vehicles.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military only — prohibited for corporate security",
    street_price: "Φ55,000 (rounds Φ200 each)",
    base_technologies: ["20mm explosive-incendiary ammunition", "Heavy-barrel free-float system", "Hydraulic recoil mitigation"],
    specifications: "caliber: 20x102mm\naction: Bolt-action\nmagazine: 5 rounds\neffective range: 2,000+ meters\nmuzzle velocity: 820 m/s\npenetration: 30mm RHA at 500 meters\nweight: 25 kg\nlength: 1,800mm\nrecoil: Hydraulic buffer system",
    tactical_use: "Extreme-range anti-materiel engagement. Operators target vehicles, generators, communication arrays, defensive positions, and personnel behind hard cover. The explosive-incendiary round ensures that hits on equipment result in destruction, not damage. Requires spotter support for engagements beyond 1,000 meters.",
    cultural_context: "The Earthquake represents the upper limit of what a single person can carry and fire. In Meridian 88's arms culture, owning an AMR-14 is a statement that you're prepared for war — not street violence, not self-defense, but war. The weapon has no civilian application and no plausible self-defense justification. Its presence means someone has decided that buildings need to be destroyed.",
    known_users: ["Zheng-Dao corporate military units", "Meridian 88 military reserve", "At least two known mercenary crews operating heavy weapons"],
    story_hooks: [
      "An AMR-14 round punched through a corporate executive's office window from 1,800 meters — passing through the exterior wall, three interior partitions, and the executive's desk chair. The executive wasn't in the chair. The shot was a warning. The next one won't be.",
      "Someone has been firing AMR-14 rounds into the foundations of a residential tower in the lower tiers. Structural engineers estimate that three more hits in the right locations will cause a partial collapse affecting 200 residents. No demands have been made."
    ],
    ammunition_type: ["20mm_explosive"],
    tags: ["anti-materiel", "weapon", "rifle", "heavy", "explosive", "zheng-dao", "military", "sniper", "tier 5"]
  },
  {
    id: uid(),
    name: "Arcturus AMR-9 'Condor'",
    type: "weapon",
    aliases: ["Condor", "Long Arm", "God's Finger"],
    category: "anti-materiel rifle",
    description: "A semi-automatic anti-materiel rifle chambered in 14.5mm, designed for rapid engagement of multiple hard targets at ranges up to 1,500 meters. Unlike the bolt-action Earthquake, the Condor's gas-operated semi-automatic action allows the operator to fire all five rounds in its magazine in under four seconds, placing multiple armor-piercing rounds on target before the first round's impact is even registered. This sustained fire capability transforms the weapon from a precision tool into a suppression system — a single Condor operator can engage five vehicles in a convoy before the lead vehicle has time to react.\n\nArcturus designed the AMR-9 for mobile anti-armor operations where speed of engagement matters more than individual shot precision. The 14.5mm tungsten-core round penetrates 25mm of armor plate at 500 meters, sufficient to defeat any wheeled vehicle and most light armored platforms. The semi-automatic action generates brutal recoil despite the weapon's hydraulic buffer — operators describe firing the Condor as 'being punched in the shoulder by a machine.'\n\nThe Condor's semi-automatic capability makes it uniquely dangerous in urban environments. A skilled operator can place five rounds through five different windows of the same building in four seconds, engaging targets on multiple floors before anyone realizes they're under fire from a single weapon. This capability has made the AMR-9 the preferred anti-materiel platform for Meridian 88's most dangerous freelance operators.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military only — prohibited for corporate security (waiver available)",
    street_price: "Φ38,000 (rounds Φ120 each)",
    base_technologies: ["Gas-operated semi-automatic action", "Tungsten-core armor-piercing ammunition", "Hydraulic recoil buffer"],
    specifications: "caliber: 14.5x114mm\naction: Semi-automatic, gas-operated\nmagazine: 5 rounds\neffective range: 1,500 meters\nmuzzle velocity: 1,000 m/s\npenetration: 25mm RHA at 500 meters\nweight: 18 kg\nlength: 1,600mm\ncyclic capability: 5 rounds in 4 seconds",
    tactical_use: "Rapid multi-target anti-materiel engagement. Operators engage convoys, building facades, defensive positions, and equipment clusters with sustained semi-automatic fire. The Condor excels in scenarios where multiple hard targets must be neutralized before they can respond or disperse.",
    cultural_context: "The Condor occupies a specific niche in Meridian 88's mercenary culture — it's the weapon of the professional who works alone against hard targets. Carrying a Condor means you expect to fight vehicles and fortifications by yourself, without support, and win. The weapon attracts a certain type of operator: skilled, confident, and dangerous enough to justify the investment.",
    known_users: ["Arcturus rapid-response units", "Elite freelance anti-armor specialists", "Corporate military detachments with heavy weapons waivers"],
    story_hooks: [
      "A Condor operator has been systematically destroying corporate surveillance cameras across a specific district — five shots, five cameras, four seconds, then gone. The pattern suggests someone is creating a blind zone for an operation that hasn't happened yet.",
      "Two Condor rifles have appeared on the black market with matching serial numbers — supposedly impossible, as each AMR-9 is individually serialized. Either Arcturus's manufacturing records are wrong, or someone is cloning serial numbers to create weapons that can't be traced."
    ],
    ammunition_type: ["14.5mm_ap"],
    tags: ["anti-materiel", "weapon", "rifle", "heavy", "armor-piercing", "arcturus", "semi-automatic", "tier 5"]
  },
  {
    id: uid(),
    name: "Carrion Defense Works AMR-3 'Gravedigger'",
    type: "weapon",
    aliases: ["Gravedigger", "Bunker Buster", "Deep Sleeper"],
    category: "anti-materiel rifle",
    description: "A bolt-action anti-materiel rifle chambered in 12.7mm with a uniquely long 1,200mm barrel designed for extreme-range precision — the Gravedigger sacrifices rate of fire and portability for accuracy that borders on surgical at distances exceeding 2,500 meters. The weapon uses a free-floating barrel with harmonic dampening, a precision-machined bolt with zero vertical play, and match-grade ammunition hand-loaded to individual weapon specifications. Each Gravedigger is test-fired at the factory and ships with a ballistic profile unique to that specific weapon.\n\nCarrion designed the AMR-3 as a precision anti-materiel tool for engagements where the first shot must hit because there will be no second shot. At 2,500 meters, the round's flight time exceeds 3 seconds — long enough for any aware target to move, any vehicle to reposition, any alert to be sounded. The Gravedigger operator gets one chance, and the weapon is built to ensure that chance is not wasted.\n\nThe Gravedigger's extreme range creates a unique tactical problem for defenders: at 2,500 meters, the shooter is beyond the effective range of almost every weapon system that isn't itself an anti-materiel rifle. Locating the firing position requires specialized counter-sniper assets, and by the time those assets deploy, the Gravedigger operator has disassembled the weapon and vanished. The weapon breaks down into three sections that fit in a standard duffel bag, and an experienced operator can transition from firing to packed in under 90 seconds.",
    manufacturer: "CARRION DEFENSE WORKS",
    tier_availability: "Tier 5",
    legality: "Military only — prohibited for any non-military use",
    street_price: "Φ65,000 (match-grade ammunition Φ150 per round)",
    base_technologies: ["Harmonic-dampened free-float barrel", "Weapon-specific ballistic profiling", "Rapid-disassembly field design"],
    specifications: "caliber: 12.7x108mm match\naction: Bolt-action\nmagazine: 3 rounds\neffective range: 2,500+ meters\nmuzzle velocity: 900 m/s\nbarrel length: 1,200mm\nweight: 16 kg\nlength: 1,900mm (assembled)\ndisassembly time: 90 seconds to field-packed",
    tactical_use: "Extreme-range precision anti-materiel and anti-personnel engagement. Each shot is a deliberate action preceded by extensive calculation of windage, elevation, temperature, humidity, and Coriolis effect. The Gravedigger is deployed when the target must be destroyed from a distance that makes attribution functionally impossible.",
    cultural_context: "The Gravedigger is a ghost weapon — used from distances where the shooter is invisible, inaudible, and effectively immune to retaliation. In Meridian 88's threat landscape, the Gravedigger represents the ultimate expression of asymmetric violence: one person, one weapon, one shot, from a position so far away that the target never knew they were in danger.",
    known_users: ["Military sniper units", "Corporate assassination specialists (Tier 5 contracts)", "Exactly one known freelance operator — callsign 'Sexton'"],
    story_hooks: [
      "A target was killed at an officially measured distance of 2,847 meters — a shot that required the round to travel for over 4 seconds through urban wind corridors. The ballistic profile matches a Gravedigger, but no known operator has the skill for that shot. Someone new is working.",
      "Three Gravedigger rifles have been stolen from a military armory. Each weapon's unique ballistic profile is on file, meaning any round fired can be traced to the specific weapon. But the thief also stole the ballistic records, and without them, the weapons are ghosts."
    ],
    ammunition_type: ["12.7mm_match"],
    tags: ["anti-materiel", "weapon", "rifle", "heavy", "precision", "sniper", "carrion", "extreme-range", "tier 5"]
  },

  // ===================== UNDERWATER WEAPONS (3) =====================
  {
    id: uid(),
    name: "Ouroboros UW-5 'Harpoon'",
    type: "weapon",
    aliases: ["Harpoon", "Depth Charge", "Fish Stick"],
    category: "underwater weapon",
    description: "A pneumatic speargun designed for underwater combat in Meridian 88's flooded infrastructure, submerged transit tunnels, and coastal industrial zones. The UW-5 fires a 30-centimeter steel spear using compressed air at velocities effective to 15 meters underwater — dramatically reduced from surface weapon ranges due to water resistance, but sufficient for the close-quarters engagements that underwater environments demand. The spear is tethered to the weapon by a retractable monofilament line, allowing recovery and reload.\n\nOuroboros developed the UW-5 because Meridian 88 has an underwater problem. Decades of subsidence, flooding, and rising water tables have created an extensive network of partially or fully submerged spaces beneath the city — old transit tunnels, basement levels, industrial chambers, and infrastructure corridors that are now accessible only by diving. These spaces are used for smuggling, storage, clandestine manufacturing, and hiding. Conventional firearms are useless underwater; the UW-5 fills the gap.\n\nThe weapon's most common deployment is during drainage operations, where security teams must clear flooded spaces that may contain hostile occupants. The UW-5's monofilament tether serves double duty — the spear can be used to anchor to structures for movement in current, and the retraction mechanism provides a crude grappling capability. In the dark, flooded tunnels beneath Meridian 88, the UW-5 is the difference between an operator and a drowning victim.",
    manufacturer: "OUROBOROS SYSTEMS",
    tier_availability: "Tier 2+",
    legality: "Licensed — maritime and infrastructure security",
    street_price: "Φ1,800",
    base_technologies: ["Pneumatic underwater propulsion", "Monofilament tether system", "Corrosion-resistant construction"],
    specifications: "projectile: 30cm steel spear\ncapacity: 1 spear (tethered)\neffective range: 15 meters underwater, 25 meters surface\npropulsion: Compressed air, 8 shots per charge\nweight: 2.3 kg\nlength: 650mm\nconstruction: Marine-grade corrosion-resistant alloys",
    tactical_use: "Underwater combat, flooded infrastructure clearance, and maritime security operations. Operators engage targets at close range in low-visibility submerged environments. The tethered spear allows retrieval and reload without losing the projectile in dark water.",
    cultural_context: "The UW-5 serves a niche that most Meridian 88 residents don't think about — the city beneath the water. Divers, smugglers, and infrastructure workers who operate in flooded zones carry the Harpoon as standard equipment, and underwater combat is a specialized skill set that commands premium rates in the freelance market.",
    known_users: ["Infrastructure security dive teams", "Smuggling interdiction units", "Freelance divers operating in Meridian 88's flooded zones", "Smugglers defending underwater stash sites"],
    story_hooks: [
      "A body has surfaced in a flooded transit tunnel with a UW-5 spear through the chest. The victim was a city infrastructure inspector who was mapping submerged spaces that someone doesn't want mapped. The flooded tunnels contain something worth killing to protect.",
      "A team of divers equipped with UW-5s has been hired to clear a flooded basement level that contains sealed containers nobody will describe. The pay is extraordinary. The nondisclosure agreement is more frightening than the weapon."
    ],
    ammunition_type: ["spear"],
    tags: ["underwater", "weapon", "speargun", "pneumatic", "maritime", "ouroboros", "infrastructure", "tier 2"]
  },
  {
    id: uid(),
    name: "Arcturus UW-8 'Barracuda'",
    type: "weapon",
    aliases: ["Barracuda", "Torpedo Pistol", "Wet Work"],
    category: "underwater weapon",
    description: "A supercavitating pistol that fires solid steel darts encased in a gas-generating sabot, creating a bubble envelope around the projectile that reduces water resistance and extends effective range to 30 meters underwater — double the range of conventional underwater weapons. The Barracuda's darts arrive with enough kinetic energy to penetrate diving equipment, underwater vehicle hulls, and the reinforced suits worn by infrastructure workers operating in contaminated water.\n\nArcturus developed the UW-8 for naval special operations, but the weapon has found extensive use in Meridian 88's underwater economy. The city's submerged spaces host smuggling operations, illegal salvage, and clandestine transit networks that conventional law enforcement cannot effectively police. The Barracuda gives underwater operators a decisive range advantage in engagements where 15 additional meters of reach means the difference between firing first and dying first.\n\nThe weapon's gas-generating sabot is its most innovative and most dangerous component. The chemical reaction that produces the supercavitation bubble generates hydrogen gas as a byproduct, which means that firing the Barracuda in enclosed submerged spaces builds up an explosive atmosphere. An operator who fires too many rounds in a sealed flooded chamber risks creating a hydrogen pocket that a single spark will detonate. Field operators have learned to count their shots and ventilate between engagements.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Military — restricted to authorized maritime operations",
    street_price: "Φ7,500",
    base_technologies: ["Supercavitating projectile design", "Gas-generating sabot", "Corrosion-proof firing mechanism"],
    specifications: "projectile: 6mm supercavitating steel dart\ncapacity: 8 darts\neffective range: 30 meters underwater\nmuzzle velocity: 250 m/s (supercavitating)\nweight: 1.1 kg\nlength: 230mm\nwarning: Hydrogen gas buildup in enclosed spaces after 4+ rounds",
    tactical_use: "Extended-range underwater combat. The Barracuda provides range superiority in submerged engagements, allowing the operator to engage targets before they can close to conventional underwater weapon range. Critical safety consideration: hydrogen gas accumulation in enclosed submerged spaces limits sustained fire.",
    cultural_context: "The Barracuda has made underwater spaces in Meridian 88 measurably more dangerous. Before its introduction, underwater combat was limited to speargun range — effectively grappling distance. The 30-meter range of supercavitating darts has turned flooded tunnels into shooting galleries where ambush positions that were previously safe are now kill zones.",
    known_users: ["Arcturus maritime security forces", "Naval special operations units", "Elite smuggling interdiction teams", "High-end freelance divers"],
    story_hooks: [
      "A Barracuda was fired in a sealed flooded chamber, and the hydrogen buildup from repeated shots detonated, collapsing a section of submerged infrastructure. The explosion revealed a hidden chamber behind the collapsed wall containing something that predates Meridian 88 by centuries.",
      "Someone is using Barracudas to systematically kill the divers who patrol Meridian 88's water intake systems. The city's water supply is being left unguarded, and whoever is doing it wants access to the intake tunnels for a reason that can't be good."
    ],
    ammunition_type: ["supercavitating_dart"],
    tags: ["underwater", "weapon", "pistol", "supercavitating", "maritime", "arcturus", "military", "tier 4"]
  },
  {
    id: uid(),
    name: "Street Custom 'Gutterfish' Improvised Speargun",
    type: "weapon",
    aliases: ["Gutterfish", "Sewer Shooter", "Drain Gun"],
    category: "underwater weapon",
    description: "A crude speargun assembled from PVC pipe, surgical tubing, and sharpened rebar, used by Shelf residents who work, hide, or fight in Meridian 88's flooded lower levels. The Gutterfish operates on simple elastic propulsion — the surgical tubing is stretched to store energy, the rebar spear is seated in the PVC barrel, and releasing the retention clip fires the spear at velocities sufficient to penetrate flesh and light materials at ranges up to 5 meters underwater.\n\nGutterfish spearguns exist because the people who need underwater weapons the most are the ones who can least afford them. The Shelf's lowest levels flood regularly, and residents who refuse to evacuate — because they have nowhere to go, or because leaving means losing what little they own — need tools that work in water. The Gutterfish serves as weapon, fishing tool, and utility device. The same instrument that defends a flooded home also catches the fish that increasingly colonize Meridian 88's submerged spaces, providing protein to communities that can't afford the vertical farm produce sold in upper-tier markets.\n\nEvery Gutterfish is handmade and unreliable. The surgical tubing degrades in contaminated water, the PVC cracks in cold temperatures, and the rebar spears are never straight enough for consistent accuracy. But the weapon costs Φ10 in materials and can be built by anyone with basic tools, making it the most democratic weapon in Meridian 88's flooded underworld.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Unregulated — classified as fishing equipment",
    street_price: "Φ10-30 (materials cost)",
    base_technologies: ["Elastic propulsion", "PVC barrel construction", "Improvised projectile fabrication"],
    specifications: "projectile: Sharpened rebar, 40-60cm\neffective range: 5 meters underwater\npropulsion: Surgical tubing elastic\nweight: 0.5-1 kg\nlength: 60-80cm\nreliability: Variable, degrades in contaminated water",
    tactical_use: "Close-range underwater combat and subsistence fishing in flooded environments. The Gutterfish is a tool of survival, used by people who live in spaces that the rest of the city has abandoned to the water. Its effectiveness depends entirely on the builder's skill and the operator's familiarity with their specific weapon.",
    cultural_context: "The Gutterfish is a symbol of adaptation at the margins. Shelf residents who carry them are people who have chosen to live in the water rather than be displaced by it. The weapon-tool represents a community that has made peace with flooding as a permanent condition and adapted their culture accordingly — including their weapons.",
    known_users: ["Shelf residents in flood-prone zones", "Subsistence fishers in submerged areas", "Anyone defending a flooded home or stash"],
    story_hooks: [
      "A corporate dive team sent to survey flooded infrastructure in the Shelf was driven off by Gutterfish spears — three divers hit, none fatally, but the message was clear. The flooded levels belong to the people who live there, and they will defend them with rebar and rubber.",
      "A Shelf community has organized a fishing cooperative using Gutterfish spearguns, harvesting fish from flooded transit tunnels and selling them in improvised markets. The fish are thriving in the contaminated water, and nobody is asking what the contamination is doing to the food supply."
    ],
    ammunition_type: ["improvised_spear"],
    tags: ["underwater", "weapon", "improvised", "speargun", "shelf", "street", "fishing", "survival", "tier 1"]
  },

  // ===================== LESS-LETHAL (5) =====================
  {
    id: uid(),
    name: "Crucible LL-4 'Peacekeeper'",
    type: "weapon",
    aliases: ["Peacekeeper", "Bean Bag Betty", "Gentle Persuasion"],
    category: "less-lethal",
    description: "A pump-action 12-gauge shotgun designed exclusively for less-lethal ammunition — bean bag rounds, rubber slugs, and tear gas canisters. The Peacekeeper's barrel is rifled differently from standard shotguns to optimize the spin stabilization of flexible projectiles, and its action is painted bright orange to visually distinguish it from lethal-ammunition weapons. The visual distinction matters: in the chaos of a security operation, an operator reaching for the wrong shotgun is the difference between compliance and a funeral.\n\nCrucible markets the LL-4 as the responsible choice for crowd control and civil disorder management. The weapon delivers impacts equivalent to a heavyweight boxer's punch at 30 meters, creating compliance through pain without the permanent consequences of ballistic weapons. This marketing elides the reality that 'less-lethal' is not 'non-lethal' — bean bag rounds have killed dozens of people in Meridian 88, typically through cardiac arrest when rounds strike the chest, skull fractures when rounds strike the head, or internal organ damage from close-range impacts that exceed the weapon's designed minimum engagement distance.\n\nThe LL-4 is the weapon most commonly deployed against civilians in Meridian 88 because it provides a veneer of restraint. Security forces can fire into crowds with Peacekeepers and claim they used 'non-lethal force,' even when the rounds break ribs, rupture organs, and kill. The weapon's orange finish has become a symbol of corporate doublespeak — the bright color that says 'this is safe' while the projectile says otherwise.",
    manufacturer: "CRUCIBLE INDUSTRIAL",
    tier_availability: "Tier 2+",
    legality: "Licensed — security and law enforcement",
    street_price: "Φ1,200 (bean bag rounds Φ3 each)",
    base_technologies: ["Less-lethal-optimized rifling", "Flexible projectile stabilization", "Visual identification marking system"],
    specifications: "caliber: 12 gauge less-lethal\naction: Pump-action\nmagazine: 7 rounds tubular\neffective range: 10-40 meters\nminimum safe range: 5 meters (closer risks lethal impact)\nweight: 3.4 kg\nlength: 840mm\nfinish: High-visibility orange",
    tactical_use: "Crowd dispersal, subject compliance, and riot suppression. Operators fire bean bag rounds at center mass to create pain compliance, or deploy tear gas canisters for area denial. The weapon's less-lethal classification allows deployment in scenarios where ballistic weapons would escalate political consequences.",
    cultural_context: "The Peacekeeper's orange finish has become Meridian 88's most recognizable symbol of corporate crowd control. Protest art frequently features the orange shotgun, and 'getting peaced' is slang for being hit by a bean bag round. The weapon's name is regarded as darkly ironic by communities that have experienced its 'peacekeeping' firsthand.",
    known_users: ["Corporate security across all major corponations", "Meridian 88 law enforcement", "Private security firms", "Event security at large gatherings"],
    story_hooks: [
      "A protester was killed by a Peacekeeper round at a labor demonstration — the official report claims a minimum-distance violation, but the forensic evidence shows the round was fired from maximum range. The round was modified to be lethal at any distance, disguised as standard less-lethal ammunition.",
      "A stockpile of LL-4 Peacekeepers has been stolen from a security depot. The weapons are being given away free in the Shelf — but the ammunition that came with them is live 12-gauge, not less-lethal. Someone is arming a community while maintaining plausible deniability."
    ],
    ammunition_type: ["12_gauge_less_lethal"],
    tags: ["less-lethal", "weapon", "shotgun", "crowd-control", "bean-bag", "crucible", "security", "tier 2"]
  },
  {
    id: uid(),
    name: "TESSERA NL-6 'Web Caster'",
    type: "weapon",
    aliases: ["Web Caster", "Net Gun", "Spider Shot"],
    category: "less-lethal",
    description: "A shoulder-fired net launcher that deploys a 4-meter weighted polymer net at ranges up to 15 meters. The net's edges carry tungsten weights that wrap around the target upon impact, and the polymer mesh contracts slightly as it cools from deployment temperature, tightening around the subject's body. A single NL-6 net can immobilize an adult human for 3-5 minutes — long enough for security personnel to approach and apply conventional restraints.\n\nTESSERA designed the Web Caster for high-value capture operations where the target must be taken alive and uninjured. The net itself causes no physical damage — it restrains without striking, compresses without crushing, and immobilizes without the electrical discharge or chemical exposure of other less-lethal systems. This makes it the preferred tool for apprehending individuals who have value — corporate assets, witnesses, hostages, and anyone whose injuries would create liability.\n\nThe weapon's limitation is its single-shot design and its vulnerability to edged tools. The polymer net can be cut with any sharp blade in under 10 seconds, meaning the Web Caster is only effective against targets who are unarmed, surprised, or both. Against a prepared target with a knife, the net is a momentary inconvenience. TESSERA's response to this limitation was the NL-6B variant, which coats the net polymer in a contact adhesive that bonds to skin and clothing, making cutting the net a process that also involves cutting yourself free of your own clothes.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 2+",
    legality: "Licensed — security and law enforcement",
    street_price: "Φ2,800 (nets Φ200 each, single-use)",
    base_technologies: ["Weighted polymer net deployment", "Thermal-contraction restraint", "Optional contact adhesive coating"],
    specifications: "net diameter: 4 meters deployed\neffective range: 15 meters\ncapacity: 1 net (single-use cartridge)\nrestraint duration: 3-5 minutes without intervention\nweight: 3.0 kg\nlength: 550mm\nvariant: NL-6B with contact adhesive coating",
    tactical_use: "Non-damaging target capture and immobilization. Used when the target must be taken alive and uninjured for operational, legal, or value reasons. Operators typically deploy the Web Caster as a first-contact tool, then approach with conventional restraints.",
    cultural_context: "The Web Caster is regarded as the most humane capture tool in Meridian 88's security arsenal, which says more about the rest of the arsenal than it does about the Web Caster. Being netted is humiliating and frightening but rarely causes lasting physical harm, which makes it the preferred tool for situations that will be recorded.",
    known_users: ["Corporate asset recovery teams", "Law enforcement capture units", "Bounty hunters", "Wildlife management in Meridian 88's urban ecology"],
    story_hooks: [
      "A Web Caster net has been recovered with modifications — the polymer mesh has been infused with a slow-release sedative that absorbs through the skin over 60 seconds, putting the restrained target to sleep. No need for personnel to approach. The target is simply packaged for collection.",
      "Someone has been netting people in the Shelf — firing Web Casters from rooftops and leaving the targets restrained for hours until the net degrades. No robbery, no assault, no apparent motive. Just the humiliation of being trapped and helpless while people walk past."
    ],
    ammunition_type: ["net_cartridge"],
    tags: ["less-lethal", "weapon", "net", "capture", "restraint", "tessera", "security", "tier 2"]
  },
  {
    id: uid(),
    name: "Arcturus RL-2 'Bouncer'",
    type: "weapon",
    aliases: ["Bouncer", "Rubber Rifler", "Hard No"],
    category: "less-lethal",
    description: "A magazine-fed semi-automatic carbine that fires rubber-composite projectiles at velocities calibrated to cause maximum pain with minimum penetration. The Bouncer's rounds are 18mm rubber-jacketed cylinders that deform on impact, spreading their kinetic energy across a larger surface area than conventional bullets. The result is a deep-tissue bruise that hurts for days but doesn't break the skin — in theory. In practice, the RL-2 at close range delivers impacts that crack ribs, rupture blood vessels, and cause internal bleeding that only becomes apparent hours later.\n\nArcturus designed the Bouncer for sustained riot suppression — the semi-automatic action and 20-round magazine allow operators to maintain a volume of fire comparable to conventional carbines without the political consequences of firing live ammunition. The weapon's effective range of 50 meters and its rapid-fire capability mean that a squad of four Bouncer-equipped operators can deliver the equivalent of a beating to an entire crowd simultaneously, from a safe distance.\n\nThe RL-2 has been involved in more civilian injury incidents than any other weapon system in Meridian 88. Its less-lethal classification means it is deployed casually, frequently, and without the hesitation that accompanies firearms. Security personnel who would think twice before firing a bullet will empty a Bouncer magazine without a second thought, because the rounds are 'safe.' The cumulative medical data — 47 confirmed fatalities, over 3,000 serious injuries annually — tells a different story.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 2+",
    legality: "Licensed — security and law enforcement",
    street_price: "Φ1,800 (rubber rounds Φ5 each)",
    base_technologies: ["Rubber-composite projectile design", "Velocity-calibrated gas system", "High-volume semi-automatic action"],
    specifications: "caliber: 18mm rubber composite\naction: Semi-automatic, gas-operated\nmagazine: 20 rounds\neffective range: 50 meters\nmuzzle velocity: 90 m/s\nimpact energy: 35 joules (designed threshold)\nweight: 3.1 kg\nlength: 680mm",
    tactical_use: "Sustained riot suppression and crowd control. Operators engage crowds with rapid semi-automatic fire, targeting center mass to create pain compliance at volume. The 20-round magazine and semi-automatic action allow a tempo of fire that keeps crowds moving and prevents reorganization.",
    cultural_context: "The Bouncer is the weapon that Meridian 88's lower tiers encounter most frequently. It is the sound of a protest ending, the impact of a labor action being suppressed, the bruise that tells you the system noticed your dissent. Its rubber rounds don't kill often enough to make headlines, but they hurt consistently enough to make a point.",
    known_users: ["Corporate security riot teams (all major corponations)", "Meridian 88 law enforcement crowd control units", "Event security teams", "Private security at labor dispute sites"],
    story_hooks: [
      "Autopsy records from a protest suppression operation show that 12 of the RL-2 rounds recovered from casualties were not rubber-composite — they were lead-core rounds in rubber jackets, designed to look less-lethal while delivering ballistic impacts. Someone in the supply chain is loading kill rounds into riot guns.",
      "An RL-2 has been modified to fire at twice the standard velocity — 180 m/s instead of 90 — turning every round into a potential skull-fracture. The modification is a simple spring swap that takes 30 seconds. Instructions have been posted on security forums, and someone is distributing modified springs to operators."
    ],
    ammunition_type: ["18mm_rubber"],
    tags: ["less-lethal", "weapon", "carbine", "rubber", "riot", "crowd-control", "arcturus", "security", "tier 2"]
  },
  {
    id: uid(),
    name: "Lazarus CH-3 'Mercy Fog'",
    type: "weapon",
    aliases: ["Mercy Fog", "Sleep Cloud", "Nap Time"],
    category: "less-lethal",
    description: "A pressurized canister launcher that fires 40mm canisters containing an aerosolized sedative compound that creates a 5-meter-diameter cloud of incapacitating gas. Targets who inhale the aerosol experience rapid onset drowsiness and loss of motor coordination within 15 seconds, followed by unconsciousness within 45 seconds. The sedative effect lasts 20-30 minutes, during which time the subject can be relocated, restrained, or searched without resistance.\n\nLazarus developed the CH-3 for medical emergencies — specifically, for situations where patients experiencing psychotic episodes, BCI-induced seizures, or cyberware malfunctions needed to be sedated immediately and at a distance. The weapon's medical origins are reflected in its calibration: the sedative dosage is calculated for the average adult body mass, with a safety margin that prevents respiratory depression in most subjects. 'Most' being the operative and insufficient word.\n\nThe CH-3's medical safety margin fails for anyone significantly below average body mass — children, small adults, the malnourished — and for anyone with respiratory conditions, cardiac issues, or certain drug interactions. In the controlled environment of a hospital, these variables can be assessed before deployment. In the uncontrolled environment of a street operation, they cannot. The Mercy Fog sedates everyone in its cloud without discrimination, and some of those people will stop breathing. Lazarus's liability documentation recommends medical monitoring of all exposed subjects, a recommendation that is ignored in approximately 100% of field deployments.",
    manufacturer: "LAZARUS BIOWORKS",
    tier_availability: "Tier 3+",
    legality: "Restricted — medical and security with chemical agent authorization",
    street_price: "Φ4,500 (canisters Φ300 each)",
    base_technologies: ["Aerosolized sedative compound", "Pressurized canister deployment", "Rapid-onset neurochemical formulation"],
    specifications: "canister: 40mm sedative aerosol\ncapacity: 4 canisters\ncloud diameter: 5 meters\nonset: 15 seconds to impairment, 45 seconds to unconsciousness\nduration: 20-30 minutes\nweight: 3.2 kg\nlength: 420mm\nwarning: Dosage unsafe for subjects under 45 kg body mass",
    tactical_use: "Area incapacitation for capture operations, hostage rescue, and medical intervention. The CH-3 renders all occupants of a confined space unconscious within 45 seconds, allowing operators to enter without resistance. Requires medical personnel on standby for adverse reactions — a requirement that is documented but rarely fulfilled.",
    cultural_context: "The Mercy Fog is the weapon that Meridian 88's security forces use when they want to claim they didn't use force. Putting people to sleep sounds gentle. The reality — people collapsing on concrete, aspirating vomit while unconscious, children experiencing respiratory arrest from adult-dosage sedatives — is documented in medical records that nobody reads.",
    known_users: ["Corporate extraction teams", "Law enforcement tactical units", "Medical emergency response (authorized)", "Lazarus facility security"],
    story_hooks: [
      "A Mercy Fog canister was deployed in a Shelf residential building, sedating 40 people including 8 children. Three children required emergency resuscitation. The operation was authorized to capture one person. The CH-3 does not negotiate with arithmetic.",
      "A new variant of the CH-3 sedative has appeared on the black market — a compound that causes unconsciousness but also suppresses memory formation for 6 hours before and after exposure. Someone has weaponized amnesia."
    ],
    ammunition_type: ["40mm_chemical"],
    tags: ["less-lethal", "weapon", "chemical", "sedative", "gas", "lazarus", "medical", "crowd-control", "tier 3"]
  },
  {
    id: uid(),
    name: "Zheng-Dao Heavy Industries BL-5 'Bola Gun'",
    type: "weapon",
    aliases: ["Bola Gun", "Leg Breaker", "Trip Wire"],
    category: "less-lethal",
    description: "A launcher that fires a weighted bola — two 200-gram tungsten spheres connected by 1.5 meters of braided steel cable — at a fleeing target's legs. The bola wraps around the target's legs at shin height, the cable locks against itself via a ratchet mechanism, and the subject goes down. The BL-5 fires its payload at 40 m/s, giving the bola enough rotational energy to wrap tightly even against a sprinting target, and the ratchet lock prevents the cable from loosening once wrapped.\n\nZheng-Dao designed the BL-5 for fugitive apprehension — the classic problem of stopping a running person without shooting them. The bola solution is ancient, but the engineering is modern: the tungsten weights are precisely balanced, the cable is rated for 500 kg, and the ratchet mechanism adds a technological cruelty to a primitive concept. Once the bola is locked, it cannot be removed without a key tool that only the operator carries. The target remains immobilized until someone chooses to release them.\n\nThe BL-5's impact dynamics are classified as less-lethal, but the tungsten weights striking a target's shins at 40 m/s deliver impacts that frequently cause bone fractures. The weapon stops people from running by making it impossible for them to walk. Zheng-Dao's marketing materials describe the BL-5 as a 'mobility denial tool.' The targets describe it as getting their legs broken by a machine.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Licensed — law enforcement and security",
    street_price: "Φ2,200 (bolas Φ80 each)",
    base_technologies: ["Precision-weighted bola deployment", "Cable ratchet-lock mechanism", "Rotational energy optimization"],
    specifications: "projectile: Dual 200g tungsten weights, 1.5m braided steel cable\ncapacity: 1 bola (magazine-fed reload)\neffective range: 20 meters\nlaunch velocity: 40 m/s\ncable strength: 500 kg rated\nweight: 2.5 kg\nlength: 350mm\nrelease: Proprietary key tool required",
    tactical_use: "Fugitive immobilization and pursuit termination. Operators fire the BL-5 at a fleeing target's legs, wrapping and locking the cable to force a fall. The ratchet mechanism ensures the target remains immobilized until the operator approaches and manually releases the bola.",
    cultural_context: "The BL-5 is despised by runners and couriers in Meridian 88 — the people most likely to be fleeing security forces on foot. 'Getting bola'd' is feared almost as much as getting shot, because the tibial fractures the weapon causes can end a running career permanently. Runners who survive a BL-5 hit often display the characteristic shin scars as marks of experience.",
    known_users: ["Law enforcement pursuit units", "Corporate security foot-chase teams", "Bounty hunters", "Licensed fugitive recovery operators"],
    story_hooks: [
      "A modified BL-5 bola has been recovered with razor wire replacing the standard cable — the weapon wraps and locks like normal, but the cable cuts through tissue and tendon. Someone is converting a mobility denial tool into a mutilation weapon and selling it as standard equipment.",
      "A courier was bola'd during a routine run and went down — but the package they were carrying detonated on impact, destroying everything within 10 meters. The BL-5 operator was killed by the blast. The package was designed to detonate if the courier was stopped. Someone anticipated the weapon and turned the courier into a bomb."
    ],
    ammunition_type: ["bola"],
    tags: ["less-lethal", "weapon", "bola", "restraint", "pursuit", "zheng-dao", "security", "tier 2"]
  },

  // ===================== CHEAP IMPROVISED / PIPE GUNS (5) =====================
  {
    id: uid(),
    name: "Street Custom 'Nail Biter' Pipe Pistol",
    type: "weapon",
    aliases: ["Nail Biter", "Shelf Special", "Zip Gun", "Crack Pipe"],
    category: "improvised firearm",
    description: "A single-shot pistol made from a short length of steel pipe, a nail for a firing pin, and a rubber-band-powered striker mechanism. The Nail Biter fires a single .22 or .25 caliber round from a barrel that may or may not be straight, through a chamber that may or may not seal properly, using a firing pin that may or may not strike the primer centered. The weapon represents the absolute minimum viable firearm — the least amount of material and engineering required to make a thing that shoots a bullet.\n\nNail Biters are the most common weapon in the Shelf, produced in quantities that no tracking system can quantify. The materials cost less than Φ5, the assembly requires no tools beyond a file and pliers, and the instructions have been passed through the community for generations. Every teenager in the Shelf knows how to build one. Most of them have built one. The quality varies from functional to suicidal — some Nail Biters will fire reliably for fifty rounds, while others will blow apart on the first shot.\n\nThe weapon's single-round capacity is its defining tactical limitation and its cultural significance. A Nail Biter gives you one shot. One chance to solve whatever problem requires a bullet. This makes every trigger pull a deliberate choice — you can't spray and pray with a weapon that needs to be reloaded by unscrewing the barrel and inserting another round. Nail Biter violence in the Shelf is almost always intentional, personal, and close-range.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Illegal — improvised firearm",
    street_price: "Φ5-20 (materials cost)",
    base_technologies: ["Pipe-barrel construction", "Nail firing pin", "Rubber-band striker mechanism"],
    specifications: "caliber: .22 LR or .25 ACP\ncapacity: 1 round\neffective range: 3-5 meters (generous)\nbarrel length: 60-100mm\nweight: 100-200g\nreliability: 60-80% depending on construction\nreload: Unscrew barrel, insert round, reassemble",
    tactical_use: "Contact-range self-defense and personal violence. The Nail Biter is not a combat weapon — it is a tool for a single, specific, close-range act. Users point it at someone within arm's reach and fire, hoping the mechanism functions as intended.",
    cultural_context: "The Nail Biter is the Shelf's universal equalizer. It doesn't matter how poor you are — you can build one. It doesn't matter how weak you are — it fires the same bullet. The weapon's ubiquity has created a social dynamic where every interaction in the lowest tiers carries the theoretical possibility that the other person has a pipe gun in their pocket. This mutual deterrence is crude but functional.",
    known_users: ["Shelf residents (widespread)", "Anyone who can find a pipe and a nail", "Juveniles and first-time weapon carriers"],
    story_hooks: [
      "A corporate executive was killed by a .22 round from a Nail Biter — a Φ5 weapon defeating Φ50,000 worth of personal security. The shooter walked up during a public appearance, fired once, and disappeared into the crowd. The investigation has no leads because the weapon was discarded and is indistinguishable from plumbing.",
      "A Shelf school has confiscated 40 Nail Biters from students in a single semester. The children aren't building them for violence — they're building them for protection against the violence that follows them to school."
    ],
    ammunition_type: ["22_lr"],
    tags: ["improvised", "weapon", "pipe gun", "shelf", "street", "poverty", "firearm", "tier 1"]
  },
  {
    id: uid(),
    name: "Street Custom 'Four Horsemen' Pipe Pepperbox",
    type: "weapon",
    aliases: ["Four Horsemen", "Quad Pipe", "Shelf Derringer", "Cluster"],
    category: "improvised firearm",
    description: "Four steel pipes welded or taped together in a square cluster, each serving as a barrel for a single shotgun shell. The Four Horsemen fires all four barrels in sequence by rotating a crude striker mechanism — turn, fire, turn, fire — giving the operator four shots before the weapon needs to be completely reloaded by extracting spent shells and inserting new ones. The weapon fires .410 shotshells, the smallest common shotgun ammunition, which is cheap, available, and devastating at the 3-5 meter range where the Four Horsemen is most likely to be used.\n\nThe Four Horsemen is the Shelf's answer to volume of fire. Where the Nail Biter gives you one shot, the Horsemen gives you four — enough to clear a room, defend a doorway, or ensure that a missed first shot isn't your last. The weapon is larger and heavier than a Nail Biter, carried in a coat pocket or a bag rather than concealed in a waistband, and its crude construction means accuracy degrades rapidly beyond arm's length. But at arm's length, four .410 shotshells fired in rapid succession are lethal regardless of precision.\n\nConstruction quality varies wildly. The best Four Horsemen use welded steel pipe with filed chambers and a machined striker plate. The worst use electrical tape and hope. The weapon's four-barrel design means that a malfunction in one barrel doesn't necessarily prevent the others from firing, providing a crude form of redundancy that single-barrel designs lack.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Illegal — improvised firearm",
    street_price: "Φ20-60 (materials cost)",
    base_technologies: ["Multi-barrel pipe construction", "Rotary striker mechanism", "Shotshell chambering"],
    specifications: "caliber: .410 shotshell\ncapacity: 4 rounds (one per barrel)\neffective range: 3-5 meters\nbarrel length: 80-120mm per barrel\nweight: 400-600g\nreload: Manual extraction and insertion per barrel\nfiring: Sequential rotary striker",
    tactical_use: "Close-range defensive fire with volume. The Four Horsemen provides multiple shots in scenarios where a single-round weapon is insufficient. Used primarily for home defense, ambush, and close-range violence where the operator needs follow-up capability.",
    cultural_context: "The Four Horsemen is named with Shelf gallows humor — four shots, four riders, four chances to meet your end. The weapon is considered a step up from the Nail Biter in Shelf armament culture, carried by individuals who expect trouble serious enough to require more than one shot. Owning a Four Horsemen is a statement that you've moved beyond theoretical self-defense into practical preparation for violence.",
    known_users: ["Shelf block defense groups", "Small-scale criminal operators", "Home defenders in high-threat areas", "Anyone expecting close-quarters confrontation"],
    story_hooks: [
      "A Four Horsemen loaded with four different types of .410 ammunition — buckshot, slug, incendiary, and flechette — was recovered from a crime scene. The builder understood that different problems require different solutions and built a weapon to address four of them.",
      "A machinist in the Shelf has started producing high-quality Four Horsemen with proper welding, filed chambers, and a reliable striker. The Φ40 weapons are more reliable than anything the community has seen, and the machinist is selling them at cost. Nobody knows who's funding the operation."
    ],
    ammunition_type: ["410_shotshell"],
    tags: ["improvised", "weapon", "pipe gun", "shotgun", "shelf", "street", "multi-barrel", "tier 1"]
  },
  {
    id: uid(),
    name: "Street Custom 'Loudmouth' Slam-Fire Shotgun",
    type: "weapon",
    aliases: ["Loudmouth", "Slam Bam", "Shelf Pump", "Pipe Thunder"],
    category: "improvised firearm",
    description: "A two-piece slam-fire shotgun consisting of a barrel pipe and a receiver pipe — the barrel slides into the receiver, and slamming the barrel backward drives the shell onto a fixed firing pin, discharging the round. The Loudmouth fires 12-gauge shotshells without any trigger, safety, or mechanical complexity — the firing mechanism is the operator's arm slamming two pipes together. Reload is performed by sliding the barrel forward, extracting the spent shell, inserting a new one, and slamming again.\n\nThe Loudmouth is the most powerful weapon that can be built with zero skill and near-zero materials. Two pipes of the right diameter, a bolt for a firing pin, and an endcap welded or threaded onto the receiver pipe. Total cost: Φ8. Total engineering: none. The weapon fires full-power 12-gauge ammunition — buckshot, slug, whatever the operator can acquire — with all the authority that implies. At 5 meters, a 12-gauge buckshot round from a Loudmouth is identical in lethality to a round from a Φ3,000 combat shotgun.\n\nThe weapon's slam-fire mechanism means there is no trigger discipline — the round fires when the operator pushes the barrel home, and the timing of that push is affected by adrenaline, fear, and the chaos of whatever situation requires a pipe shotgun. Accidental discharges are common. The weapon has no safety because there is nothing to put a safety on. It is a pipe that converts forward motion into a dead person.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Illegal — improvised firearm",
    street_price: "Φ8-15 (materials cost)",
    base_technologies: ["Slam-fire mechanism", "Fixed firing pin", "Two-piece pipe construction"],
    specifications: "caliber: 12 gauge\ncapacity: 1 round\neffective range: 5-15 meters\nbarrel length: 300-500mm\nweight: 1-2 kg\nreload: Slide forward, extract, insert, slam\nfiring mechanism: Inertial (slam-fire)",
    tactical_use: "Close-range maximum-impact engagement. The Loudmouth delivers 12-gauge firepower in the crudest possible package. Used when the operator needs the most destructive commonly available ammunition and has no access to manufactured weapons.",
    cultural_context: "The Loudmouth is the Shelf's great equalizer at its most extreme — a weapon that puts 12-gauge authority in the hands of anyone with Φ8 and access to a hardware store. The weapon's name is both descriptive (it is extremely loud) and metaphorical (it speaks for people who have no other voice). Slam-fire shotguns have been used in every major Shelf uprising in living memory.",
    known_users: ["Shelf residents (widespread)", "Impromptu resistance groups", "Home defenders", "Anyone facing a threat that requires more than a .22"],
    story_hooks: [
      "A Loudmouth was used in what is being called the Shelf's first successful bank robbery — a teenager with a Φ8 pipe shotgun walked into a corporate financial branch and walked out with Φ25,000. The security systems were designed to detect manufactured weapons. They didn't flag two pieces of pipe.",
      "Loudmouth shotguns have been found with rifled inserts — short sections of properly rifled barrel pressed into the pipe bore that allow the weapon to fire slugs with actual accuracy out to 30 meters. Someone is upgrading the Shelf's arsenal with Φ2 worth of precision engineering."
    ],
    ammunition_type: ["12_gauge"],
    tags: ["improvised", "weapon", "shotgun", "pipe gun", "shelf", "street", "slam-fire", "12-gauge", "tier 1"]
  },
  {
    id: uid(),
    name: "Street Custom 'Ghost' Printed Pistol",
    type: "weapon",
    aliases: ["Ghost", "Printer Special", "Plastic Fantastic", "Download"],
    category: "improvised firearm",
    description: "A 3D-printed single-shot pistol manufactured on consumer-grade fabrication equipment using freely available design files. The Ghost is produced from high-temperature polymer with a printed barrel, frame, and firing mechanism — the only non-printed component is a short steel barrel liner pressed into the polymer bore and a hardware-store nail for the firing pin. The weapon fires a single 9mm round with enough accuracy and reliability to function as a lethal tool at ranges under 5 meters.\n\nGhost pistols exist because fabrication technology has made weapons manufacturing a file-sharing problem. The design files circulate on encrypted networks, updated regularly by anonymous engineers who refine the geometry based on field reports and failure analysis. Version 14 — the current standard — has a reliable firing rate of approximately 90% and a barrel life of 8-12 rounds before the steel liner shifts in the polymer housing and accuracy becomes dangerously unpredictable.\n\nThe Ghost's most dangerous characteristic is its near-invisibility to standard screening. The polymer frame contains no metal except the barrel liner and firing pin — two small pieces that can be disguised as belt buckle components or hair accessories during separate passage through screening. The weapon can be assembled in 30 seconds from seemingly innocuous parts, fired, and discarded. A Ghost pistol that has been used in a crime looks identical to random polymer debris in a waste container. It is a weapon designed to exist for exactly as long as it is needed and then cease to exist.",
    manufacturer: "Street Custom (open-source design)",
    tier_availability: "Tier 1+",
    legality: "Illegal — unregistered firearm; manufacturing constitutes weapons production",
    street_price: "Φ15-50 (printing cost plus nail and liner)",
    base_technologies: ["Consumer 3D printing", "High-temperature polymer construction", "Open-source weapons design"],
    specifications: "caliber: 9mm\ncapacity: 1 round\neffective range: 5 meters\nbarrel life: 8-12 rounds\nweight: 150g\nlength: 180mm\nmaterial: High-temperature polymer with steel barrel liner\nfiring reliability: ~90% (Version 14)",
    tactical_use: "Disposable close-range weapon for single-use operations. The Ghost is printed, assembled, used, and discarded. Its value is not in its capability — which is minimal — but in its untraceability and its ability to pass security screening in component form.",
    cultural_context: "The Ghost represents the democratization of weapons manufacturing — anyone with a consumer fabricator can produce a lethal weapon from a downloaded file. This has created a constant arms race between screening technology and printing innovation, with each new detection method met by a new design that evades it. The Ghost's name is apt: it appears, kills, and vanishes.",
    known_users: ["Anyone with a 3D printer", "Individuals planning operations in screened environments", "Political assassins requiring disposable untraceable weapons"],
    story_hooks: [
      "A new Ghost design file has appeared that prints a multi-shot mechanism — three rounds instead of one, with a rotating barrel assembly. The file's metadata contains a message: 'Version 15. They can't stop us from making what we need.'",
      "A high-security corporate facility has experienced three killings with Ghost pistols in two months. The weapons were assembled inside the facility using a fabricator in the employee workshop. The design file was found on 40% of employee personal devices."
    ],
    ammunition_type: ["9mm"],
    tags: ["improvised", "weapon", "3d-printed", "disposable", "unregistered", "ghost", "shelf", "street", "tier 1"]
  },
  {
    id: uid(),
    name: "Street Custom 'Cigarette' Pen Gun",
    type: "weapon",
    aliases: ["Cigarette", "Smoke Break", "Last Drag", "Filter Tip"],
    category: "improvised firearm",
    description: "A single-shot .22 short firearm built into a metal cigarette case, with the barrel disguised as a cigarette extending from the case's opening and the firing mechanism hidden beneath a false bottom. The operator opens the case, removes the 'cigarette,' holds it to their lips as if preparing to smoke, and presses a concealed button that fires the round. The entire sequence mimics the universal gesture of lighting a cigarette — reaching into a pocket, opening a case, raising something to your mouth — making the weapon deployment invisible in any environment where smoking is common.\n\nThe Cigarette is a Shelf masterpiece of social camouflage. It exploits the fact that a person holding something to their face while fumbling with a small case looks like every other smoker in Meridian 88. The weapon's effective range is measured in inches — the .22 short round from a 30mm barrel has barely enough energy to penetrate skin at 2 meters — but the weapon isn't designed for range. It is designed for the moment when you're standing next to someone, pretending to smoke, and you press the barrel against their body under the guise of a casual gesture.\n\nProduction quality ranges from elegant to crude. The best Cigarettes are machined from actual cigarette cases with precision barrel inserts and reliable spring-loaded firing pins. The worst are hammered together from tin and hope. All of them share the same terrifying characteristic: they look like nothing, carried by millions of people, performing a gesture that no one questions.",
    manufacturer: "Street Custom",
    tier_availability: "Tier 1+",
    legality: "Illegal — disguised firearm",
    street_price: "Φ50-200",
    base_technologies: ["Cigarette case concealment", "Social camouflage deployment", "Miniaturized .22 firing mechanism"],
    specifications: "caliber: .22 Short\ncapacity: 1 round\neffective range: Contact to 1 meter\nbarrel length: 30mm\nweight: 80-120g\nform factor: Metal cigarette case with protruding 'cigarette' barrel",
    tactical_use: "Contact-range assassination disguised as a common social gesture. The Cigarette exploits smoking behavior as cover for weapon deployment. Effective only at extreme close range against unaware targets.",
    cultural_context: "The Cigarette is the most intimate weapon in the Shelf's arsenal — it requires you to stand next to your target, mimic a casual gesture, and fire from inches away. Building one is an act of premeditation. Carrying one means you've identified someone specific, in a specific place, at a specific time. It is not a weapon of opportunity. It is a weapon of intent.",
    known_users: ["Shelf assassination specialists", "Individuals with specific, premeditated targets", "Anyone who smokes and harbors grudges"],
    story_hooks: [
      "A security camera captured the exact moment a Cigarette was fired — the operator leaning toward the target as if asking for a light, the small flash, the target staggering. The footage shows the killer's face clearly. Nobody recognizes them. They were never seen before the killing and have never been seen since.",
      "A Shelf craftsman has been producing Cigarettes with a secondary function — the case contains a genuine cigarette lighter, and the weapon can actually light a cigarette using the barrel flash. The dual-use design makes the weapon pass any functional test: it looks like a lighter, it works like a lighter, and it also kills people."
    ],
    ammunition_type: ["22_short"],
    tags: ["improvised", "weapon", "concealed", "disguised", "cigarette", "shelf", "street", "assassination", "tier 1"]
  },

  // ===================== VEHICLE-MOUNTED WEAPONS (5) =====================
  {
    id: uid(),
    name: "Arcturus VMG-4 'Turret'",
    type: "weapon",
    aliases: ["Turret", "Car Gun", "Road Rage"],
    category: "vehicle-mounted weapon",
    description: "A retractable 7.62mm machine gun mounted in a concealed housing beneath the roof or hood of a standard passenger vehicle. The VMG-4 deploys in 1.5 seconds from its housing, rises on a motorized gimbal, and provides 180-degree forward arc fire controlled by the vehicle's onboard targeting system or by a gunner operating through a dashboard-mounted display. The weapon fires standard 7.62mm NATO at 800 RPM from a 200-round belt contained in the vehicle's chassis, and retracts flush after engagement, leaving no visible trace of the weapon's existence.\n\nArcturus sells the VMG-4 as an executive protection package — an integrated vehicle defense system that transforms any sedan or SUV into a light combat platform without external modification. The weapon's concealment is its primary feature: a VMG-4-equipped vehicle looks identical to every other vehicle on the road until the moment the roof panel slides open and a machine gun emerges. This moment tends to resolve traffic disputes with finality.\n\nThe VMG-4's integration with vehicle systems extends beyond the weapon itself. The targeting system ties into the vehicle's forward-facing cameras, radar, and LIDAR, providing aim assistance and target tracking that compensates for vehicle motion. An operator can engage targets while driving at highway speed, with the fire control system adjusting for velocity, target movement, and lead. The result is a mobile weapons platform that looks like a luxury sedan.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Restricted — executive protection license with vehicle weapons permit",
    street_price: "Φ45,000 (installed)",
    base_technologies: ["Retractable weapon housing", "Vehicle-integrated fire control", "Motion-compensated targeting"],
    specifications: "caliber: 7.62x51mm NATO\nrate of fire: 800 RPM\nfeed: 200-round belt (chassis-mounted)\ndeployment time: 1.5 seconds\narc of fire: 180 degrees forward\nweight: 35 kg (weapon and housing)\nmounting: Concealed roof or hood housing",
    tactical_use: "Mobile vehicle defense and offensive engagement from concealment. The VMG-4 provides surprise firepower in ambush scenarios, convoy protection, and executive extraction operations. The weapon's concealment means the first indication of its presence is 7.62mm rounds arriving at the target.",
    cultural_context: "The VMG-4 has contributed to a specific paranoia in Meridian 88's traffic culture — the awareness that any vehicle might be armed. Aggressive driving takes on new implications when the car you just cut off might have a machine gun in its roof. Traffic courtesy in Meridian 88's upper tiers is driven as much by survival instinct as by politeness.",
    known_users: ["Corporate executive protection fleets", "Arcturus security vehicles", "Wealthy individuals with vehicle weapons permits", "Confirmed use by at least three criminal organizations"],
    story_hooks: [
      "A VMG-4 was deployed on a public highway during what appeared to be a road rage incident — 200 rounds of 7.62mm fired into traffic, killing four people in three vehicles. The shooter's vehicle was registered to a corporate executive who claims it was stolen 20 minutes before the incident.",
      "A mechanic has discovered a VMG-4 installed in a vehicle brought in for routine service — the owner didn't know it was there. Someone installed a concealed machine gun in a civilian vehicle without the owner's knowledge, and the weapon is loaded and live."
    ],
    ammunition_type: ["7.62mm_nato"],
    tags: ["vehicle", "weapon", "mounted", "machine gun", "concealed", "arcturus", "executive", "tier 4"]
  },
  {
    id: uid(),
    name: "TESSERA VS-2 'Smokescreen'",
    type: "weapon",
    aliases: ["Smokescreen", "Cloud Car", "Vanish Kit"],
    category: "vehicle-mounted weapon",
    description: "A vehicle-integrated smoke and countermeasure dispensing system that deploys obscurant smoke, oil slick, caltrops, and chaff from concealed ports in a vehicle's rear bumper, side panels, and undercarriage. The VS-2 provides a comprehensive suite of pursuit denial capabilities, allowing a fleeing vehicle to create multiple obstacles for pursuing vehicles and disable both visual and electronic tracking.\n\nTESSERA designed the VS-2 as a defensive counterpart to offensive vehicle weapon systems like the VMG-4. Where the Turret fights, the Smokescreen runs — deploying countermeasures that blind pursuers, puncture tires, destroy traction, and confuse radar and infrared tracking. A full VS-2 deployment creates a 50-meter zone of obscurant smoke behind the vehicle, scattered with caltrops that defeat standard tires and slick with an oil compound that reduces road friction to near zero.\n\nThe system's electronic countermeasures are equally aggressive. The chaff dispensers scatter radar-reflective filaments that create ghost returns on pursuit vehicle sensors, and an integrated infrared flare system defeats thermal tracking. The VS-2 turns escape into an engineering problem and solves it with brute-force denial of every sensor and surface the pursuit relies on.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 3+",
    legality: "Restricted — vehicle countermeasure permit required",
    street_price: "Φ22,000 (installed, countermeasure reloads Φ3,000)",
    base_technologies: ["Multi-spectrum obscurant smoke", "Deployable caltrop dispersal", "Radar and infrared countermeasures"],
    specifications: "smoke coverage: 50-meter obscurant zone\ncaltrop dispersal: 200 units per deployment\noil slick: 30-meter coverage area\nchaff range: 100-meter radar confusion zone\nIR flares: 8 per reload\ndeployment: Dashboard-controlled, individual or full-suite\nreload: Requires depot service",
    tactical_use: "Pursuit denial and escape facilitation. Operators deploy countermeasures sequentially or simultaneously based on pursuit type — smoke for visual, caltrops for wheeled, oil for traction, chaff for radar, flares for infrared. A full-suite deployment virtually guarantees escape from any single-vehicle pursuit.",
    cultural_context: "The VS-2 is the preferred defensive system for Meridian 88's extraction specialists and high-value couriers — people whose job requires them to run rather than fight. 'Smoking out' is slang for deploying countermeasures during a chase, and VS-2-equipped vehicles are identified by subtle reinforcement of the rear bumper that experienced operators learn to spot.",
    known_users: ["Corporate extraction teams", "High-value courier services", "VIP transport operators", "Criminal organizations with vehicle fleets"],
    story_hooks: [
      "A VS-2 smoke deployment on a highway caused a 30-vehicle pileup that killed seven people. The vehicle deploying countermeasures was fleeing a corporate security team, and neither party will accept responsibility for the civilian casualties caught between them.",
      "A VS-2 system has been modified to dispense an aerosolized nerve agent instead of obscurant smoke. The vehicle's rear bumper is now a chemical weapon delivery system disguised as a standard countermeasure package."
    ],
    ammunition_type: ["countermeasure_pack"],
    tags: ["vehicle", "weapon", "countermeasure", "smoke", "defensive", "tessera", "pursuit", "tier 3"]
  },
  {
    id: uid(),
    name: "Zheng-Dao Heavy Industries VR-10 'Plow'",
    type: "weapon",
    aliases: ["Plow", "Road Clearer", "Bull Bar From Hell"],
    category: "vehicle-mounted weapon",
    description: "A reinforced kinetic ram system integrated into a heavy vehicle's front end, consisting of a 400-kilogram tungsten-composite blade mounted on hydraulic shock absorbers that allow the vehicle to impact barriers, other vehicles, and fortified positions at speed without disabling the vehicle itself. The VR-10 transforms a standard armored truck into a battering ram that can breach concrete barriers, flip passenger vehicles, and smash through reinforced gates at speeds up to 80 km/h.\n\nZheng-Dao manufactures the Plow for military convoy operations where the lead vehicle must be capable of clearing roadblocks, barricades, and ambush positions without stopping. The hydraulic shock absorption system distributes impact forces across the vehicle's reinforced frame, preventing the chassis damage that would normally disable a vehicle after a high-speed collision. A Plow-equipped vehicle can impact a concrete jersey barrier at 60 km/h, shatter it, and continue driving without significant speed loss.\n\nThe VR-10's most controversial deployment is in forced-entry operations against occupied structures. A Plow-equipped vehicle driven through a building's ground-floor wall creates an entry point that no door breach or explosive charge can match — a vehicle-sized hole with a vehicle still in it, providing mobile cover for the entry team riding inside. The building's occupants are simultaneously dealing with a collapsed wall, a vehicle in their living room, and armed personnel deploying from it. The psychological impact is overwhelming.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Military — restricted to authorized vehicle modifications",
    street_price: "Φ30,000 (installed on compatible vehicle)",
    base_technologies: ["Tungsten-composite ram blade", "Hydraulic shock absorption", "Reinforced frame integration"],
    specifications: "ram weight: 400 kg\nmaterial: Tungsten-composite blade\nimpact rating: Concrete jersey barrier at 60 km/h\nshock absorption: Hydraulic, 4-stage progressive\nvehicle integration: Heavy truck or APC chassis\ninstallation weight penalty: 600 kg total system",
    tactical_use: "Barrier clearance, vehicle interdiction, and forced structural entry. The Plow converts a heavy vehicle into a breaching tool that creates entry points impossible to achieve through conventional methods. Used primarily in convoy operations and forced-entry raids.",
    cultural_context: "The Plow represents the most literal form of corporate power projection in Meridian 88 — the ability to drive through walls. Residents of the Shelf who have witnessed Plow-equipped vehicles breach residential buildings describe the experience as feeling like the entire world is being rearranged by force. The weapon doesn't target people. It targets the structures that people depend on for safety.",
    known_users: ["Zheng-Dao corporate military convoys", "Meridian 88 tactical entry teams", "Corporate raid units requiring forced structural access"],
    story_hooks: [
      "A Plow-equipped vehicle was driven through the front of a Shelf community center during a neighborhood meeting, collapsing the building's facade and injuring thirty people. The vehicle was abandoned at the scene. No entry team followed. It wasn't a raid — it was a message.",
      "Someone has stolen a Plow-equipped armored truck from a Zheng-Dao depot. The vehicle weighs 12 tonnes and can drive through buildings. It hasn't been found, and every structure in Meridian 88 with ground-floor walls is now a potential target."
    ],
    ammunition_type: [],
    tags: ["vehicle", "weapon", "ram", "breaching", "heavy", "zheng-dao", "military", "tier 4"]
  },
  {
    id: uid(),
    name: "Vantablack VD-3 'Spike Strip Launcher'",
    type: "weapon",
    aliases: ["Spike Strip", "Tire Shredder", "Road Tax"],
    category: "vehicle-mounted weapon",
    description: "A rear-facing pneumatic launcher that deploys articulated spike strips from a vehicle's undercarriage, throwing them onto the road surface behind the vehicle where they unfold into 3-meter-wide barriers studded with hardened steel spikes. Each spike is designed to penetrate standard vehicle tires and embed itself in the rubber, creating progressive deflation that disables the pursuing vehicle over 200-400 meters rather than causing an instant blowout that could result in a crash dangerous to bystanders.\n\nVantablack designed the VD-3 as a less-kinetic alternative to caltrops for pursuit denial — the articulated spike strips unfold in a predictable pattern rather than scattering randomly, and the progressive deflation design gives the pursuing driver time to decelerate safely. This humanitarian consideration exists because Vantablack's legal team calculated that the liability from spike-strip-caused pileups would exceed the product's revenue within two years of deployment. The 'safe' deflation design is a business decision, not a moral one.\n\nThe VD-3 carries four spike strips, deployable individually or in rapid sequence. A full deployment creates a 12-meter zone of tire-destroying obstacles that effectively blocks a two-lane road. The strips' articulated design means they lie flat against the road surface after deployment, presenting a low visual profile that drivers may not notice until they're on top of them. Recovery requires a specialized tool that disarms the spring-loaded spike mechanism — running over a deployed strip a second time drives the spikes deeper rather than flattening them.",
    manufacturer: "VANTABLACK MOBILITY",
    tier_availability: "Tier 3+",
    legality: "Restricted — vehicle countermeasure permit required",
    street_price: "Φ8,000 (installed, reload strips Φ400 each)",
    base_technologies: ["Articulated spike strip design", "Progressive deflation engineering", "Pneumatic rear deployment"],
    specifications: "strip width: 3 meters\nspike material: Hardened steel\ndeflation type: Progressive (200-400m to full deflation)\ncapacity: 4 strips\ndeployment: Pneumatic rear launcher\ntotal system weight: 25 kg\nrecovery: Requires specialized disarming tool",
    tactical_use: "Pursuit denial and road denial. Operators deploy spike strips to disable pursuing vehicles without causing catastrophic accidents. Effective against conventional wheeled vehicles. Ineffective against vehicles with solid tires, tracked vehicles, or airborne pursuit.",
    cultural_context: "The VD-3 is the most socially responsible vehicle weapon in Meridian 88 — a product designed to disable pursuit without killing bystanders, created not out of conscience but out of liability calculation. It is a weapon that exists because someone did the math on wrongful death lawsuits and decided that progressive deflation was cheaper than legal settlements.",
    known_users: ["VIP transport services", "Corporate courier vehicles", "Cash-transit vehicles", "High-end criminal vehicles"],
    story_hooks: [
      "A VD-3 spike strip has been found deployed on a residential street — not behind a fleeing vehicle, but placed deliberately across a road used by a specific corporate executive's daily commute. The progressive deflation is designed to disable the vehicle exactly at a predetermined point where an ambush team is waiting.",
      "Modified VD-3 strips have appeared with the progressive deflation feature disabled — the spikes cause instant blowouts at speed, turning a pursuit denial tool into a vehicle destruction weapon. The modification is a firmware change, suggesting someone with access to Vantablack's software."
    ],
    ammunition_type: ["spike_strip"],
    tags: ["vehicle", "weapon", "spike", "pursuit-denial", "defensive", "vantablack", "road", "tier 3"]
  },
  {
    id: uid(),
    name: "Crucible VE-7 'Arc Welder'",
    type: "weapon",
    aliases: ["Arc Welder", "Zapper", "Lightning Rod", "EMP Truck"],
    category: "vehicle-mounted weapon",
    description: "A vehicle-mounted directed electromagnetic pulse weapon that discharges a focused EMP cone from an antenna array hidden behind a vehicle's front grille. The VE-7 generates a single-pulse electromagnetic discharge that disables electronic systems in a 30-degree cone extending 50 meters from the vehicle, affecting everything from vehicle engine management computers to personal BCIs to building security systems. The pulse is generated by a capacitor bank that requires 45 seconds to recharge between firings, drawing power from the vehicle's enhanced electrical system.\n\nCrucible developed the VE-7 for convoy escort operations where the primary threat is electronically detonated ambush weapons — IEDs, remote-triggered mines, and drone attacks that rely on electronic command links. The EMP pulse disables the electronic triggers before the convoy reaches the threat area, neutralizing ambushes without requiring the convoy to stop or slow. The weapon's directional focus means it affects only the area ahead of the vehicle, limiting collateral electronic disruption to the vehicle's path.\n\nThe VE-7's most concerning application in Meridian 88 is its effect on BCIs and cyberware. The EMP pulse induces current in any electronic implant within range, causing effects ranging from momentary disorientation to seizures to permanent implant damage depending on the implant's shielding and the subject's distance from the emitter. Using the Arc Welder in a populated area subjects every augmented person in the cone to involuntary neurological disruption. Crucible's safety documentation classifies this as 'incidental electronic interference.' Cyberware manufacturers classify it as assault.",
    manufacturer: "CRUCIBLE INDUSTRIAL",
    tier_availability: "Tier 4+",
    legality: "Military — restricted to authorized electronic warfare operations",
    street_price: "Φ55,000 (installed on compatible vehicle)",
    base_technologies: ["Directed electromagnetic pulse generation", "Capacitor bank energy storage", "Antenna array beam forming"],
    specifications: "pulse cone: 30 degrees, 50-meter range\neffect: Electronic system disruption/destruction\nrecharge time: 45 seconds\npower source: Vehicle electrical system with capacitor bank\ncollateral: Affects all electronics in cone, including BCIs\nweight: 80 kg total system\nmounting: Concealed behind front grille",
    tactical_use: "Electronic warfare, convoy protection, and pursuit denial. The Arc Welder disables pursuing vehicles, neutralizes electronic ambushes, and disrupts security systems ahead of the vehicle. Each pulse requires 45 seconds to recharge, making shot selection critical.",
    cultural_context: "The VE-7 is feared by augmented communities more than any ballistic vehicle weapon. A machine gun can miss. An EMP cone affects everything in its path. The knowledge that a passing vehicle might discharge an EMP that fries your neural interface has created a specific paranoia among cyberware users — 'getting arced' is a constant, invisible threat in Meridian 88's traffic.",
    known_users: ["Military convoy escort vehicles", "Corporate electronic warfare units", "Specialized extraction vehicles", "At least one known criminal operation using EMP for armored car robberies"],
    story_hooks: [
      "A VE-7 was discharged on a crowded street, disabling every electronic device in a 50-meter cone — including the BCIs of 30 augmented pedestrians, 4 of whom suffered seizures, 1 of whom died from implant failure. The vehicle was targeting a specific drone overhead. The pedestrians were incidental.",
      "Someone is using a VE-7 to systematically disable security cameras along a specific route through Meridian 88, creating an electronic blind corridor that appears to lead from the Shelf to a corporate facility. They're building an invisible path."
    ],
    ammunition_type: ["energy_cell"],
    tags: ["vehicle", "weapon", "emp", "electronic warfare", "crucible", "directed-energy", "cyberware", "tier 4"]
  },

  // ===================== PERSONAL DEFENSE WEAPONS FOR CIVILIANS (7) =====================
  {
    id: uid(),
    name: "Ringo PD-1 'Citizen'",
    type: "weapon",
    aliases: ["Citizen", "Civvie", "First Gun", "Starter"],
    category: "pistol",
    description: "A compact polymer-framed semi-automatic pistol chambered in .380 ACP, designed specifically for the civilian self-defense market. The Citizen is deliberately simple — no accessory rail, no optic mount, no threaded barrel. It fires, it is reliable, and it fits in a pocket or purse. Ringo designed the PD-1 to be the first and possibly only firearm that a civilian would purchase, with ergonomics optimized for untrained users and a trigger pull weight heavy enough to prevent accidental discharge during the panic of a genuine self-defense encounter.\n\nRingo sells the Citizen through licensed retail outlets in Meridian 88's mid-tier districts, where it has become the default personal defense weapon for office workers, shop owners, and anyone else who has decided that the world is dangerous enough to require a gun but not dangerous enough to require a good gun. The weapon is adequate for its intended purpose — a close-range deterrent that a scared person can point and fire with reasonable confidence that the bullet will go approximately forward.\n\nThe PD-1's most significant contribution to Meridian 88's culture is normalization. Before the Citizen, firearms ownership was associated with security professionals, criminals, and enthusiasts. The PD-1 made gun ownership ordinary — a household purchase, like a fire extinguisher, made by people who hope they'll never use it. Ringo's marketing campaign — 'Every Citizen Has The Right' — has sold over 200,000 units in Meridian 88 alone, making the Citizen statistically the most common firearm in civilian hands in the city.",
    manufacturer: "RINGO ENTERTAINMENT DIVISION",
    tier_availability: "Tier 2+",
    legality: "Licensed — civilian self-defense permit",
    street_price: "Φ380",
    base_technologies: ["Polymer frame construction", "Heavy trigger pull safety system", "Simplified operating mechanism"],
    specifications: "caliber: .380 ACP\naction: Semi-automatic, striker-fired\nmagazine: 7 rounds\neffective range: 10 meters\nweight: 0.4 kg\nlength: 155mm\ntrigger pull: 3.5 kg (intentionally heavy for safety)",
    tactical_use: "Civilian self-defense at close range. The Citizen is designed for a single scenario: a civilian facing a direct threat who needs to fire a weapon they may never have practiced with, in a situation they never expected to encounter. The heavy trigger and simple controls reduce the chance of unintended discharge during high-stress encounters.",
    cultural_context: "The Citizen has made Meridian 88 a more armed city without making it obviously so. The people carrying Citizens are teachers, accountants, baristas — ordinary people who live ordinary lives punctuated by the extraordinary awareness that they might need to kill someone to survive the commute home. The normalization of civilian armament is Ringo's most profitable and most dangerous achievement.",
    known_users: ["Meridian 88 civilian population (200,000+ units in circulation)", "Small business owners", "Corporate employees in mid-tier districts", "First-time firearm buyers"],
    story_hooks: [
      "A statistical analysis of Citizen-involved shootings reveals that PD-1 owners are more likely to shoot family members, roommates, and neighbors than actual threats. Ringo has buried the data. A researcher who found it independently has started receiving threats.",
      "A recall notice for a specific batch of Citizens has been quietly issued — the trigger mechanism in 5,000 units has a defect that allows discharge from being dropped. Ringo is handling the recall privately to avoid publicity. The defective weapons are still in circulation."
    ],
    ammunition_type: ["380_acp"],
    tags: ["pistol", "weapon", "civilian", "personal defense", "ringo", "compact", "common", "tier 2"]
  },
  {
    id: uid(),
    name: "Crucible PD-3 'Watchdog'",
    type: "weapon",
    aliases: ["Watchdog", "Home Guard", "Nightstand Gun"],
    category: "pistol",
    description: "A full-sized 9mm semi-automatic pistol with integrated smart-lock biometric security — the weapon will only fire for its registered owner, identified by grip-pattern recognition and palm-vein scanning. The Watchdog is designed for home defense, with a full-length barrel for accuracy, a 15-round magazine for sustained engagement, and a rail-mounted flashlight that activates automatically when the weapon is drawn from its charging dock. The biometric lock ensures that the weapon cannot be used against its owner if seized during a struggle.\n\nCrucible markets the Watchdog as the responsible home defense choice — a weapon that combines stopping power with safety features that prevent unauthorized use. The biometric registration process takes 48 hours, during which the weapon's AI learns its owner's grip characteristics under various conditions: sweaty hands, cold hands, gloved hands, injured hands. The system claims a 99.7% authorized-user recognition rate, which sounds impressive until you calculate that 0.3% of 50,000 units represents 150 weapons that might not fire when their owners need them most.\n\nThe Watchdog's biometric data is stored locally on the weapon and transmitted to Crucible's cloud backup 'for warranty purposes.' This means Crucible maintains a database of biometric grip patterns, palm-vein scans, and usage logs for every Watchdog owner — a database that law enforcement has subpoenaed seventeen times in the past two years.",
    manufacturer: "CRUCIBLE INDUSTRIAL",
    tier_availability: "Tier 2+",
    legality: "Licensed — civilian self-defense with biometric registration",
    street_price: "Φ1,200",
    base_technologies: ["Biometric grip-pattern recognition", "Palm-vein authentication", "Integrated smart-lock system"],
    specifications: "caliber: 9mm\naction: Semi-automatic, striker-fired\nmagazine: 15 rounds\neffective range: 25 meters\nweight: 0.8 kg\nlength: 200mm\nbiometric lock: Grip pattern + palm vein (99.7% recognition)\naccessories: Integrated rail flashlight, charging dock",
    tactical_use: "Home defense with biometric safety. The Watchdog provides confidence that the weapon will function for its owner and nobody else. The integrated flashlight and generous magazine capacity address the typical home defense scenario: disoriented, scared, in the dark, possibly facing multiple threats.",
    cultural_context: "The Watchdog represents the premium end of civilian self-defense — a weapon for people who can afford to worry about safety features. Its biometric lock is both its selling point and its critique: the wealthy can afford weapons that only they can fire, while the poor carry Nail Biters that anyone can pick up and use. Even self-defense is tiered in Meridian 88.",
    known_users: ["Mid-to-upper tier homeowners", "Small business operators", "Corporate employees with home security concerns"],
    story_hooks: [
      "A Watchdog's biometric system has been hacked — the weapon now responds to a grip pattern that doesn't belong to its registered owner. Someone has added themselves to the weapon's authorized user list remotely, through Crucible's cloud backup system.",
      "A home invasion survivor's Watchdog failed to authenticate during the attack — the 0.3% failure rate hit at the worst possible moment. The survivor is suing Crucible. The discovery process is uncovering the full scope of the biometric database."
    ],
    ammunition_type: ["9mm"],
    tags: ["pistol", "weapon", "civilian", "personal defense", "biometric", "smart-lock", "crucible", "home", "tier 2"]
  },
  {
    id: uid(),
    name: "TESSERA PD-5 'Vapor'",
    type: "weapon",
    aliases: ["Vapor", "Spray Gun", "Personal Cloud"],
    category: "spray weapon",
    description: "A compact chemical defense sprayer the size of a large lipstick tube, dispensing a pressurized stream of synthetic capsaicinoid compound at ranges up to 3 meters. The Vapor delivers an incapacitating chemical agent that causes immediate, severe eye pain, temporary blindness, respiratory distress, and skin irritation lasting 30-45 minutes. The synthetic formulation is 5 times more potent than natural capsaicin and includes a UV-fluorescent dye that marks the attacker's skin for 72 hours, allowing identification by law enforcement.\n\nTESSERA sells the Vapor as the lowest-escalation self-defense option — a chemical deterrent that incapacitates without injury, identifies the attacker for later apprehension, and fits in a pocket or keychain holster. The weapon has become ubiquitous among civilian populations who want protection but are uncomfortable with firearms. At Φ45, it is the cheapest branded defense product in Meridian 88.\n\nThe Vapor's most significant limitation is its vulnerability to augmentation. Targets with sealed eye protection, respiratory cyberware, or chemical-resistant dermal augmentations are minimally affected. This creates a self-defense gap: the people most likely to threaten civilians — augmented criminals — are the people least affected by the Vapor's payload. The weapon works well against unaugmented attackers, meaning it is most effective against the least dangerous threats.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 1+",
    legality: "Unrestricted — personal defense device",
    street_price: "Φ45",
    base_technologies: ["Synthetic capsaicinoid formulation", "Pressurized stream delivery", "UV-fluorescent identification dye"],
    specifications: "range: 3 meters (pressurized stream)\neffect: Severe eye/respiratory irritation, temporary blindness\nduration: 30-45 minutes\ncharges: 10 sprays per canister\nUV dye persistence: 72 hours on skin\nweight: 30g\nlength: 80mm",
    tactical_use: "Personal defense against unaugmented threats at close range. The Vapor provides an immediately incapacitating effect that creates opportunity to escape. The UV dye assists law enforcement identification. Ineffective against augmented or prepared attackers.",
    cultural_context: "The Vapor is carried by more people in Meridian 88 than any other defense product — its low cost, small size, and non-lethal classification make it the default choice for anyone who wants some form of protection. 'Vaping someone' has entered slang as a general term for any minor, non-lethal retaliation.",
    known_users: ["Civilian population (ubiquitous in mid-tier districts)", "Students", "Service industry workers", "Anyone who doesn't want to carry a firearm"],
    story_hooks: [
      "A batch of Vapor canisters has been found loaded with a compound far more potent than the standard formulation — causing permanent corneal damage instead of temporary irritation. The contaminated canisters are mixed into legitimate retail stock and cannot be identified without chemical analysis.",
      "TESSERA's UV dye database — which logs every Vapor deployment through dye-pattern analysis — has been acquired by a stalking network. The database shows exactly where and when every Vapor was used, providing a map of vulnerable people and their locations."
    ],
    ammunition_type: ["chemical_canister"],
    tags: ["spray", "weapon", "chemical", "civilian", "personal defense", "tessera", "non-lethal", "common", "tier 1"]
  },
  {
    id: uid(),
    name: "Ouroboros PD-2 'Aegis'",
    type: "weapon",
    aliases: ["Aegis", "Shield Shot", "Pocket Shotgun"],
    category: "pistol",
    description: "A double-barreled derringer-format pistol chambered in .410/.45 Colt, capable of firing either shotshells or pistol rounds from its two over-under barrels. The Aegis provides maximum close-range versatility in a package small enough to conceal in an ankle holster — one barrel loaded with a .410 buckshot shell for close-range spread, one with a .45 Colt round for aimed fire. The operator selects which barrel fires first via a barrel-select switch on the frame.\n\nOuroboros designed the Aegis for people who want a backup weapon that can handle the two most common self-defense scenarios: an attacker at arm's reach (buckshot) and an attacker at room distance (pistol round). The double-barrel design means only two shots before reloading, but the combination of spread and precision in a pocket-sized package has made the Aegis the most popular backup weapon in Meridian 88 among security professionals and civilians alike.\n\nThe weapon's compact dimensions and dual-caliber capability have also made it popular in the Shelf, where .410 shotshells and .45 Colt rounds are among the most commonly available ammunition types. A Φ600 Aegis loaded with one buckshot shell and one .45 round provides a civilian with two different solutions to two different problems, in a weapon that disappears in an ankle holster and weighs less than a can of soda.",
    manufacturer: "OUROBOROS SYSTEMS",
    tier_availability: "Tier 2+",
    legality: "Licensed — civilian self-defense permit",
    street_price: "Φ600",
    base_technologies: ["Dual-caliber over-under barrel", "Barrel-select firing mechanism", "Compact derringer frame"],
    specifications: "caliber: .410 bore / .45 Colt\nbarrels: 2 (over-under)\neffective range: 5 meters (.410) / 15 meters (.45 Colt)\nweight: 0.3 kg\nlength: 140mm\nbarrel length: 75mm\naction: Double-action trigger with barrel select",
    tactical_use: "Last-resort backup weapon and civilian self-defense. The Aegis provides two shots of mixed capability in the smallest possible package. Security professionals carry it as a backup to their primary weapon. Civilians carry it as their only weapon.",
    cultural_context: "The Aegis has achieved cult status as Meridian 88's 'everyone gun' — the weapon that shows up everywhere, from executive ankle holsters to Shelf residents' coat pockets. Its versatility and compact size have made it the most common backup weapon in the city. 'Carrying an Aegis' is so common that it's become a metaphor for basic preparedness.",
    known_users: ["Security professionals (backup weapon)", "Civilian self-defense carriers", "Shelf residents", "Corporate employees in lower-tier transit zones"],
    story_hooks: [
      "An Aegis was used in a justified shooting that has become a cultural flashpoint — a Tier 1 resident shot a Tier 4 corporate employee in self-defense, and the subsequent legal proceedings have exposed the two-tier justice system that treats the same weapon differently depending on who carries it.",
      "A gunsmith has discovered that a specific production run of Aegis derringers has barrels that are slightly out of alignment — the .410 barrel fires 3 degrees left of point of aim. At buckshot range it doesn't matter. At .45 Colt range, it means missing the target and hitting whatever is next to them. Ouroboros has not issued a recall."
    ],
    ammunition_type: ["410_bore", "45_colt"],
    tags: ["pistol", "weapon", "civilian", "personal defense", "derringer", "compact", "ouroboros", "backup", "tier 2"]
  },
  {
    id: uid(),
    name: "Lazarus PD-4 'Guardian Angel'",
    type: "weapon",
    aliases: ["Guardian Angel", "Bio Defender", "Allergy Response"],
    category: "less-lethal",
    description: "A wrist-mounted auto-deploying defense system that detects threat conditions through biometric monitoring and fires a burst of three tranquilizer micro-darts when the wearer's heart rate, cortisol levels, and galvanic skin response indicate extreme distress. The Guardian Angel removes the decision to use force from the user — the weapon deploys automatically when the wearer is sufficiently terrified, eliminating the hesitation that often prevents civilians from defending themselves.\n\nLazarus designed the Guardian Angel for clients who want protection but know they would freeze in a crisis — the executive who has never been in a fight, the student walking home through dangerous districts, the elderly resident who couldn't operate a firearm under stress. The system monitors the wearer's biometric state continuously and fires its micro-darts at the nearest threat when the stress indicators exceed the configured threshold. The tranquilizer payload induces unconsciousness within 8 seconds of contact.\n\nThe Guardian Angel's autonomous firing capability is its most praised and most criticized feature. Proponents argue it eliminates the freeze response that leaves victims unable to defend themselves. Critics point out that a weapon that fires based on the wearer's emotional state will also fire during panic attacks, nightmares, arguments with partners, and encounters with aggressive dogs. The device does not assess whether the threat is real — it assesses whether the wearer believes the threat is real, and biology does not distinguish between genuine danger and perceived danger.",
    manufacturer: "LAZARUS BIOWORKS",
    tier_availability: "Tier 3+",
    legality: "Licensed — personal defense with autonomous weapon registration",
    street_price: "Φ3,800",
    base_technologies: ["Biometric threat detection", "Autonomous deployment AI", "Micro-dart tranquilizer delivery"],
    specifications: "deployment: Autonomous biometric trigger\npayload: 3 tranquilizer micro-darts\nrange: 5 meters\ntranquilizer onset: 8 seconds to unconsciousness\nduration: 15-20 minutes\nweight: 120g (wrist-mounted)\nfalse deployment rate: 4.2% under clinical conditions",
    tactical_use: "Automated personal defense for individuals who cannot or will not actively operate a weapon. The Guardian Angel provides a last-resort defense that functions regardless of the user's combat capability or psychological state. Requires registration and calibration to the individual wearer's biometric baseline.",
    cultural_context: "The Guardian Angel represents the ultimate expression of defensive helplessness — a weapon for people who are too afraid to use a weapon. Its existence acknowledges that Meridian 88 is dangerous enough that even people who cannot fight need to be armed, and that the only way to arm them effectively is to remove the human element from the decision to fire. The implications of autonomous weapons on civilians' wrists are still being debated while the weapons are already being sold.",
    known_users: ["Upper-tier executives and socialites", "Students in dangerous districts", "Elderly residents", "Individuals with anxiety disorders (controversial)"],
    story_hooks: [
      "A Guardian Angel deployed during an argument between partners, tranquilizing one of them. The wearer claims they were in genuine fear. The tranquilized partner claims it was a normal disagreement. The weapon's biometric logs show genuine extreme distress — but distress can come from guilt as easily as from fear.",
      "A class-action lawsuit has been filed against Lazarus by 200 Guardian Angel owners who experienced false deployments — the devices fired during nightmares, panic attacks, and one memorable incident during a horror film screening that tranquilized three people in adjacent seats."
    ],
    ammunition_type: ["tranquilizer_dart"],
    tags: ["less-lethal", "weapon", "autonomous", "personal defense", "biometric", "lazarus", "tranquilizer", "civilian", "tier 3"]
  },
  {
    id: uid(),
    name: "Arcturus PD-6 'Peacemaker'",
    type: "weapon",
    aliases: ["Peacemaker", "Heavy Civilian", "Overkill Carry"],
    category: "pistol",
    description: "A compact .45 ACP semi-automatic pistol marketed for civilian self-defense, representing the opposite philosophy of the Citizen's minimalism. The Peacemaker is overbuilt — a stainless steel frame, match-grade barrel, tritium night sights, and a 10-round magazine of hollow-point .45 ACP that will stop anything short of armored cyberware. Arcturus designed it for civilians who want the same stopping power as professional operators, packaged in a weapon that fits in a belt holster.\n\nThe Peacemaker is controversial because it represents deliberate escalation of civilian armament. The .45 ACP hollow-point round is a manstopper — designed to expand on impact, create maximum tissue destruction, and transfer all kinetic energy to the target. This is not a deterrent. It is a killing tool marketed to people who park in office garages and buy groceries on their way home. Arcturus's marketing — 'When Peace Requires Authority' — positions the weapon as a civic responsibility, as if carrying a .45 designed for maximum lethality is an act of community service.\n\nThe PD-6 has become the weapon of choice for civilian gun enthusiasts in Meridian 88 who view self-defense as an identity rather than a contingency. These owners train, practice, accessorize, and carry daily with a commitment that approaches religious devotion. The weapon has spawned a subculture of civilian operators who dress, train, and equip themselves like professional security without the license, experience, or accountability. Arcturus encourages this subculture because it sells weapons, accessories, training courses, and the lifestyle that goes with them.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 2+",
    legality: "Licensed — civilian self-defense permit with enhanced caliber authorization",
    street_price: "Φ1,600",
    base_technologies: ["Match-grade barrel manufacturing", "Stainless steel frame construction", "Tritium illumination sight system"],
    specifications: "caliber: .45 ACP\naction: Semi-automatic, single-action\nmagazine: 10 rounds\neffective range: 25 meters\nweight: 0.9 kg\nlength: 205mm\nsights: Tritium night sights\nfinish: Stainless steel, brushed",
    tactical_use: "Personal defense with maximum stopping power. The Peacemaker is designed to end threats with minimal rounds fired — the .45 ACP hollow-point creates wound channels that incapacitate faster than smaller calibers. The match-grade barrel provides accuracy that exceeds most civilian shooters' capability.",
    cultural_context: "The Peacemaker has become a cultural marker in Meridian 88 — carrying one signals a specific identity: someone who takes self-defense seriously enough to carry a .45, trains regularly, and has adopted the mindset that they might need to kill someone today. The weapon has created a civilian operator subculture that mimics military culture without military accountability.",
    known_users: ["Civilian gun enthusiasts", "Small business owners in high-threat areas", "Former security professionals", "The civilian operator subculture"],
    story_hooks: [
      "A Peacemaker owner shot and killed a teenager who was reaching for a BCI display pad that the shooter mistook for a weapon. The legal defense is citing Arcturus's training program, which teaches civilians to 'treat every potential threat as a confirmed threat.' Arcturus's marketing created the mindset that killed the teenager.",
      "Arcturus has discovered that a competitor is selling counterfeit Peacemakers with inferior metallurgy — the weapons fire normally for 200 rounds, then the barrel fails catastrophically. Thirty thousand counterfeit units are in civilian hands, and there's no way to identify them without firing them."
    ],
    ammunition_type: ["45_acp"],
    tags: ["pistol", "weapon", "civilian", "personal defense", "heavy", "arcturus", "enthusiast", "tier 2"]
  },
  {
    id: uid(),
    name: "Vantablack PD-8 'Whistle'",
    type: "weapon",
    aliases: ["Whistle", "Panic Key", "Screech"],
    category: "personal alarm",
    description: "A keychain-sized personal alarm that combines a 130-decibel siren, a strobe light, and an emergency broadcast that transmits the wearer's location, biometric status, and audio/video feed to Vantablack's private security response network. When activated by pressing the panic button, the Whistle creates a 10-second window of sensory disruption — blinding light and deafening sound — while simultaneously dispatching the nearest available Vantablack security response team to the wearer's location.\n\nVantablack sells the Whistle as a subscription service: the device is Φ80, but the security response service costs Φ150 per month. Without the subscription, the Whistle is just a loud keychain. With it, pressing the button connects you to a private armed response team that guarantees arrival within 8 minutes in Tier 3+ districts. The response time in lower tiers is 'subject to availability,' which means never.\n\nThe Whistle represents the privatization of emergency response taken to its endpoint. The device does not contact public emergency services — it contacts Vantablack, which responds if you're a paying subscriber and ignores you if you're not. The siren and strobe are designed to buy time until help arrives, but 'help' is a commercial service, and commercial services have terms of engagement that prioritize the subscriber's safety over bystanders', witnesses', or even the attacker's legal rights. Vantablack's response teams are authorized to use 'any reasonable force' to protect subscribers, and their definition of 'reasonable' has been tested in court fourteen times without a single adverse ruling.",
    manufacturer: "VANTABLACK MOBILITY",
    tier_availability: "Tier 1+ (device), Tier 3+ (response service)",
    legality: "Unrestricted — personal safety device",
    street_price: "Φ80 (device) + Φ150/month (response subscription)",
    base_technologies: ["High-intensity siren and strobe", "Emergency broadcast transmission", "Private security network integration"],
    specifications: "siren: 130 dB omnidirectional\nstrobe: 500 lumens, disorienting flash pattern\nbroadcast: Location, biometrics, audio/video to Vantablack network\nresponse time: 8 minutes (Tier 3+), 'subject to availability' (lower tiers)\nbattery: 6 months standby, 5 minutes active\nweight: 25g\nform factor: Keychain",
    tactical_use: "Emergency activation to create sensory disruption and summon armed response. The Whistle is not a weapon — it is a summons. Its tactical value is the 8-minute guarantee that trained, armed, and legally protected security personnel will arrive and deal with whatever situation prompted the activation.",
    cultural_context: "The Whistle has created a visible class divide in personal security. Subscribers carry their keychains openly as status symbols — the knowledge that pressing a button summons armed protection is both a deterrent and a display of economic position. Non-subscribers in the lower tiers have been known to carry fake Whistles for the deterrent effect, gambling that potential attackers won't know the difference.",
    known_users: ["Tier 3+ residents with subscription service", "Corporate employees (often employer-subsidized)", "Upper-tier students and young professionals", "Non-subscribers carrying fake Whistles (lower tiers)"],
    story_hooks: [
      "A Whistle activation in the Shelf drew a Vantablack response team to a non-subscriber's location — someone had cloned the emergency broadcast protocol and was using fake activations to lure armed response teams into ambush positions. Three Vantablack operators have been killed.",
      "Vantablack's response logs reveal that Whistle activations in one district are being selectively delayed — the 8-minute guarantee becomes 25 minutes for subscribers with specific employer affiliations. Someone inside Vantablack is using the response system as a weapon by withholding protection."
    ],
    ammunition_type: [],
    tags: ["personal alarm", "weapon", "civilian", "personal defense", "subscription", "vantablack", "security", "tier 1"]
  }

];

// ============================================================
// WRITE ALL ENTITIES
// ============================================================

let created = 0;
for (const w of weapons) {
  const filename = writeEntity(w);
  console.log(`  + ${filename}`);
  created++;
}

console.log(`\nDone. Created ${created} weapons in ${OUTPUT_DIR}`);
console.log(`Total files now: ${existingFiles.size}`);
