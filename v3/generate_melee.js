const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'weaponry');
if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const existingFiles = new Set(fs.readdirSync(OUTPUT_DIR));

function slugify(str) {
  return str
    .toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

function writeEntity(entity) {
  const slug = slugify(entity.name.slice(0, 60));
  const filename = slug + '.json';
  if (existingFiles.has(filename)) {
    console.log('SKIP (exists): ' + filename);
    return false;
  }
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(entity, null, 2));
  existingFiles.add(filename);
  console.log('WROTE: ' + filename);
  return true;
}

function id() {
  return crypto.randomBytes(16).toString('hex');
}

let wrote = 0;
let skipped = 0;

// ─── BREAK-ACTION SHOTGUNS & ELECTROMAGNETIC SCATTER GUNS (5) ──────────

const scatterGuns = [
  {
    id: id(),
    name: "Crucible Industries Breaker BA-4 'Doorknocker'",
    type: "weapon",
    aliases: ["Doorknocker", "BA-4", "The Breaker", "Knock-Knock"],
    category: "melee",
    description: "A break-action scatter shotgun designed for close-quarters hallway fighting in GLMZ's compressed urban corridors. The BA-4 uses a traditional break-action mechanism machined from Ablonite-KR ceramic composites, making it virtually immune to electromagnetic interference and impossible to disable with standard EMP grenades. The barrel is short — just 28 centimeters — and the bore is wide enough to accept a variety of improvised loads in addition to its standard flechette shells.\n\nCrucible Industries markets the Doorknocker as a breaching tool rather than a combat weapon, a legal distinction that allows it to be sold at Tier 2 without heavy weapons licensing. In practice, every corridor fighter and close-quarters operative in GLMZ knows the BA-4 for what it is: a devastatingly simple weapon that turns tight spaces into kill zones. The break-action mechanism means a maximum of two shots before reload, which has given rise to the street saying 'two knocks and you're done.'\n\nThe weapon's Ablonite-KR receiver has an unexpected secondary benefit — the ceramic composite is extremely resistant to forensic trace analysis, making it difficult for investigators to match fired projectiles to a specific weapon. Crucible claims this is incidental to the material choice. Nobody believes them.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Licensed as breaching tool — effectively unrestricted",
    base_technologies: ["Ablonite-KR ceramic composite construction", "Wide-bore break-action mechanism", "Multi-load compatible chamber"],
    specifications: "gauge: 10-gauge wide bore\neffective_range: 2-15 meters\nrate_of_fire: 2 shots, manual reload\ncapacity: 2-round break-action\nweight: 2.8 kg\nbarrel_length: 28 cm\nload_compatibility: Standard flechette, slug, improvised",
    tactical_use: "The Doorknocker excels in hallway and room-clearing scenarios where its wide spread pattern fills confined spaces. Operators typically carry pre-loaded spare barrels for rapid reload rather than fumbling with individual shells. The weapon's EMP immunity makes it a reliable backup when electronic weapons fail during infrastructure attacks. Two-person teams alternate shots to maintain continuous threat coverage.",
    cultural_context: "In GLMZ's lower tiers, the BA-4 is the most commonly encountered serious weapon — cheap enough to acquire, simple enough to maintain, and devastating enough to end most confrontations in a single trigger pull. Street culture has adopted 'knocking' as a euphemism for armed robbery, and the distinctive crack of a break-action closing has become an auditory threat signal recognized across all tiers. Crucible's attempt to market it as a tool has become a running joke.",
    known_users: ["Lower-tier corridor fighters", "Breaching teams across all tiers", "Improvised militia groups"],
    story_hooks: [
      "A batch of BA-4s has appeared with barrels bored out to accept 8-gauge shells — the resulting weapon is barely controllable but absolutely lethal in confined spaces. Someone is modifying them in bulk.",
      "A Crucible Industries quality control engineer has discovered that a specific production run of Ablonite-KR receivers contains a traceable isotopic signature — contradicting the company's claims about forensic resistance."
    ],
    ammunition_type: ["10-gauge flechette shell", "10-gauge slug", "improvised loads"],
    tags: ["weapon", "melee", "shotgun", "break-action", "close-quarters", "tier 2", "ceramic"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Scatterfield SF-2 'Hornets Nest'",
    type: "weapon",
    aliases: ["Hornets Nest", "SF-2", "Scatterfield", "The Swarm"],
    category: "melee",
    description: "An electromagnetic scatter gun that accelerates a cloud of ferromagnetic micro-projectiles through a wide-bore magnetic barrel, creating a cone of hypersonic metal fragments that shreds soft targets and overwhelms personal armor through sheer volume of impacts. The SF-2 does not fire a single projectile — it fires approximately 1,200 needle-thin ferro-ceramic slivers per trigger pull, each one traveling at Mach 2.\n\nThe Scatterfield uses a disposable cassette system rather than conventional ammunition. Each cassette contains pre-packed ferromagnetic slivers suspended in a stabilizing gel that vaporizes during acceleration, ensuring uniform distribution across the cone of fire. The electromagnetic barrel requires a capacitor bank that takes 1.8 seconds to recharge between shots — an eternity in close combat that has driven operators to carry multiple SF-2 units rather than wait for recharge.\n\nArcturus developed the Hornets Nest as a counter-swarm weapon for engaging multiple light drones simultaneously. Its effectiveness against biological targets was, according to corporate communications, 'an emergent property of the platform's versatility.' The weapon's micro-projectiles lose lethal velocity beyond 20 meters, making it exclusively a close-range system — but within that range, nothing short of hardened plate armor offers meaningful protection.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Licensed — anti-drone platform authorization",
    base_technologies: ["Electromagnetic micro-projectile acceleration", "Ferromagnetic sliver cassette system", "Wide-cone magnetic barrel dispersal"],
    specifications: "projectile_count: ~1,200 ferro-ceramic slivers per cassette\neffective_range: 2-20 meters\nrate_of_fire: 1 shot per 1.8 seconds (capacitor recharge)\ncapacity: 6-cassette rotary magazine\nweight: 4.1 kg with full magazine\npower_source: Integrated supercapacitor bank, 30 shots per charge\ncone_spread: 22 degrees at muzzle",
    tactical_use: "Primarily deployed against drone swarms in enclosed environments where conventional firearms cannot engage fast-moving micro-targets effectively. The SF-2's wide cone of thousands of slivers creates an area denial effect that no small drone can navigate. Against personnel, the weapon is devastating at point-blank range but the recharge delay makes it a poor choice for sustained engagements. Experienced operators fire once and transition to a sidearm while the capacitor cycles.",
    cultural_context: "Anti-drone warfare has become a daily reality in GLMZ's contested zones, and the Hornets Nest has given security teams a visceral, satisfying answer to the swarm problem. The sound of an SF-2 discharge — a sharp electromagnetic crack followed by the hissing impact of 1,200 slivers hitting everything downrange — has been described as 'angry static.' Drone operators have started calling contested corridors 'hive territory' when SF-2 units are known to be present.",
    known_users: ["Arcturus anti-drone security teams", "Corporate facility defense operators", "Tier 3 corridor security"],
    story_hooks: [
      "Someone has modified SF-2 cassettes to contain chemically treated slivers that cause progressive tissue necrosis — turning a painful wound into a death sentence over 72 hours.",
      "A drone operator has developed a swarm formation that can absorb SF-2 fire and continue its mission — the Hornets Nest's counter just became obsolete, and Arcturus is scrambling for a response."
    ],
    ammunition_type: ["Ferro-ceramic sliver cassette"],
    tags: ["weapon", "melee", "scatter", "electromagnetic", "anti-drone", "tier 3", "close-quarters"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Thundermouth Double 'Old Testament'",
    type: "weapon",
    aliases: ["Old Testament", "Thundermouth", "The Sermon", "Preacher"],
    category: "melee",
    description: "A hand-built break-action double-barrel shotgun constructed by the Forge-Smith Collective, a loose guild of independent weapons artisans operating in GLMZ's industrial underbelly. Each Thundermouth is unique — built from reclaimed industrial materials, hand-fitted, and test-fired by the smith who made it. The weapon uses conventional chemical propellant but the barrels are lined with carbon-lattice composite tubing that handles pressures far beyond what traditional steel can manage, allowing massively overcharged loads.\n\nThe Old Testament designation is not a model number — it is a cultural label applied to any Thundermouth that has been chambered for the Collective's proprietary 'sermon shells,' oversized 8-gauge cartridges packed with a mix of tungsten bearings and thermite fragments. On firing, the thermite ignites from propellant flash and the shot pattern arrives at the target already burning. The effect at close range is apocalyptic.\n\nThe Forge-Smith Collective operates without corporate oversight, patents, or quality standards beyond individual artisan pride. Each weapon bears the maker's mark — a personal sigil stamped into the receiver — and reputation within the Collective is built entirely on the reliability and lethality of the weapons produced. A smith whose weapon fails in the field loses standing that may take years to recover.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-3 (artisan availability)",
    legality: "Unlicensed — no corporate registration, no serial numbers",
    base_technologies: ["Carbon-lattice barrel lining", "Overcharged chemical propellant", "Thermite-fragment incendiary loads"],
    specifications: "gauge: 8-gauge overcharged\neffective_range: 2-12 meters\nrate_of_fire: 2 shots, manual reload\ncapacity: 2-round break-action\nweight: 3.4-4.2 kg (varies by smith)\nbarrel_lining: Carbon-lattice composite\nload_type: Sermon shells — tungsten/thermite composite",
    tactical_use: "The Old Testament is not a precision weapon — it is a statement of overwhelming force at conversational distance. Users deploy it as a first-and-last resort in situations where negotiation has already failed. The thermite fragments continue burning for approximately four seconds after impact, making the weapon effective against armored targets that might shrug off conventional shot. The two-shot limitation means operators must make every trigger pull count or have a credible backup plan.",
    cultural_context: "The Forge-Smith Collective's weapons carry a cultural weight in GLMZ's lower tiers that corporate products cannot replicate. Owning a named smith's Thundermouth is a mark of status — it means someone with a reputation trusted you enough to sell you their work. The 'sermon shell' designation reflects the weapon's quasi-religious reputation: when the Old Testament speaks, the conversation is over. Aspiring smiths apprentice for years before they are permitted to stamp their mark on a receiver.",
    known_users: ["Lower-tier enforcers with artisan connections", "Forge-Smith Collective members", "Independent operators who prefer analog reliability"],
    story_hooks: [
      "A master smith's entire output has been bought by a single anonymous buyer — forty weapons in six months. Someone is arming a force with artisan-quality shotguns, and the Collective wants to know why.",
      "A Thundermouth bearing a dead smith's mark has surfaced — but the weapon is newly built. Someone is forging artisan marks, and the Collective considers this a killing offense."
    ],
    ammunition_type: ["8-gauge sermon shell", "8-gauge standard"],
    tags: ["weapon", "melee", "shotgun", "break-action", "artisan", "incendiary", "tier 1", "analog"]
  },
  {
    id: id(),
    name: "Crucible Industries Scatterpulse SP-1 'Static Cling'",
    type: "weapon",
    aliases: ["Static Cling", "SP-1", "Scatterpulse", "The Clinger"],
    category: "melee",
    description: "A compact electromagnetic scatter pistol that fires a burst of electrostatically charged micro-pellets designed to adhere to conductive surfaces on impact. Unlike conventional scatter weapons that rely on kinetic penetration, the Scatterpulse's pellets are coated with a conductive adhesive polymer that bonds to metal, carbon composite, and cyberware housings. Once adhered, the pellets discharge their stored charge in a cascading pulse that disrupts electronic systems and causes involuntary muscle contractions in biological tissue adjacent to cybernetic implants.\n\nThe SP-1 was developed as a non-lethal compliance tool for corporate security details tasked with subduing augmented individuals without destroying expensive cyberware. The pellets' adhesive coating means they cannot be simply brushed off — removal requires a solvent or careful mechanical extraction, during which the pellets continue their intermittent discharge cycle. Targets hit by the Scatterpulse describe the experience as being covered in electric bees.\n\nCrucible markets the weapon as Tier 2 compliant on the basis of its non-lethal classification, though emergency medical reports document at least twelve cardiac events linked to Scatterpulse discharges interacting with thoracic cyberware. Crucible maintains that these incidents resulted from 'pre-existing implant conditions' rather than weapon effects.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Licensed — non-lethal compliance tool classification",
    base_technologies: ["Electrostatic micro-pellet projection", "Conductive adhesive polymer coating", "Cascading discharge pulse architecture"],
    specifications: "pellet_count: ~400 per cartridge\neffective_range: 3-12 meters\nrate_of_fire: Semi-automatic, 1 shot per 0.6 seconds\ncapacity: 8-cartridge magazine\nweight: 1.4 kg\npower_source: Integrated charge cell, 40 shots\ndischarge_duration: 90 seconds per pellet after adhesion",
    tactical_use: "Security teams use the Scatterpulse to disable augmented targets without engaging in physical confrontation. A single well-placed shot coats a target's cybernetic limb or torso augmentation in discharging pellets, causing involuntary spasms and system disruption that renders most aug-dependent individuals temporarily helpless. The weapon is most effective against heavily augmented targets — the more cyberware a person carries, the more conductive surfaces the pellets can bond to. Against unaugmented individuals, the weapon causes painful but manageable skin irritation.",
    cultural_context: "The Scatterpulse has earned genuine hatred in GLMZ's augmented communities. It is viewed as a weapon designed to punish people for their cyberware — to turn their body modifications into liabilities. The phrase 'getting clinged' has entered slang as a description of any situation where a person's strengths are weaponized against them. Several aug-rights advocacy groups have filed legal challenges against the SP-1's non-lethal classification, citing the cardiac incident reports.",
    known_users: ["Corporate security compliance teams", "GLMZ Municipal Security (limited deployment)", "Private augmented-individual detention contractors"],
    story_hooks: [
      "A modified Scatterpulse has appeared that fires pellets with a 48-hour discharge cycle instead of 90 seconds — targets are incapacitated for days. Someone has weaponized the compliance tool.",
      "An aug-rights activist has developed a spray-on coating that neutralizes Scatterpulse adhesion — and Crucible Industries wants the formula suppressed."
    ],
    ammunition_type: ["Electrostatic micro-pellet cartridge"],
    tags: ["weapon", "melee", "scatter", "electromagnetic", "non-lethal", "anti-cyberware", "tier 2", "compliance"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Shardstorm SH-3 'Glassbreaker'",
    type: "weapon",
    aliases: ["Glassbreaker", "SH-3", "Shardstorm", "Crystal Rain"],
    category: "melee",
    description: "A heavy electromagnetic scatter cannon that fires frangible Ablonite-KR ceramic slugs which fragment into razor-edged shards upon exiting the barrel. The Shardstorm uses a linear electromagnetic accelerator to propel a solid ceramic cylinder through a rifled fracture sleeve — a barrel section machined with internal stress-inducing ridges that shatter the slug into a predictable cloud of fragments while imparting a rotational spread pattern.\n\nThe SH-3 occupies a middle ground between conventional shotguns and area-denial weapons. Each ceramic slug fragments into approximately 60 shards, each one hard enough to defeat soft body armor and sharp enough to cause catastrophic tissue damage. The Ablonite-KR ceramic is radar-transparent and produces no metallic signature, making the weapon's ammunition invisible to standard weapon-detection systems tuned for ferrous projectiles.\n\nArcturus developed the Shardstorm for operations in environments with dense electronic surveillance where conventional metallic ammunition would be detected in flight by tracking systems. The ceramic shards pass through standard bullet-trajectory sensors without registering, arriving at the target with no electronic warning. This capability has made the SH-3 extremely popular among operators who work in high-security zones where every fired round is tracked in real time.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Restricted — ceramic ammunition classified as detection-evasive",
    base_technologies: ["Electromagnetic ceramic slug acceleration", "Rifled fracture sleeve fragmentation", "Ablonite-KR radar-transparent ammunition"],
    specifications: "slug_type: Frangible Ablonite-KR ceramic cylinder\nfragment_count: ~60 per slug\neffective_range: 5-30 meters\nrate_of_fire: 1 shot per 2.4 seconds (capacitor + autoloader)\ncapacity: 4-slug tubular magazine\nweight: 5.6 kg\npower_source: Dorsal capacitor, 20 shots per charge\ndetection_profile: Zero metallic signature",
    tactical_use: "The Shardstorm is deployed in security-saturated environments where conventional ammunition would be tracked by real-time ballistic monitoring systems. Its ceramic shards are invisible to ferrous-detection grids, giving operators a crucial window of confusion during the initial engagement. The weapon's heavy recoil and low capacity make it unsuitable for sustained firefights — it is a first-strike tool designed to deliver devastating damage before the enemy understands what is hitting them.",
    cultural_context: "The existence of detection-evasive ammunition has forced GLMZ's security infrastructure into an expensive upgrade cycle — and the security companies selling the upgrades are often subsidiaries of the same corporations selling the weapons that made the upgrades necessary. This feedback loop is not lost on the city's residents. The Shardstorm has become a symbol of corporate self-dealing: create the problem, sell the solution. Among operators, the weapon is respected for its technical elegance but disliked for its limited capacity.",
    known_users: ["Arcturus covert operations teams", "Corporate infiltration specialists", "High-tier assassination contractors"],
    story_hooks: [
      "Ablonite-KR ceramic shards have been found in a Tier 4 executive who supposedly died of natural causes — the shards are invisible to standard autopsy scanning and were only discovered by a persistent forensic pathologist using experimental imaging.",
      "A black-market supplier is selling counterfeit Ablonite-KR slugs that fragment unpredictably — some operators have been injured by shards that ricocheted back from the fracture sleeve."
    ],
    ammunition_type: ["Frangible Ablonite-KR ceramic slug"],
    tags: ["weapon", "melee", "scatter", "electromagnetic", "ceramic", "stealth", "tier 4", "detection-evasive"]
  }
];

// ─── SWORDS — KATANAS, LONGSWORDS, VIBRO-BLADES (8) ────────────────────

const swords = [
  {
    id: id(),
    name: "Crucible Industries Resonance Katana RK-7 'Singing Edge'",
    type: "weapon",
    aliases: ["Singing Edge", "RK-7", "The Singer", "Hum Blade"],
    category: "melee",
    description: "A katana-profile blade constructed from layered carbon-lattice composite with an integrated piezoelectric vibration system that oscillates the cutting edge at 40,000 cycles per second. The RK-7's blade appears to shimmer slightly when activated — a visual artifact of the micro-vibration that gives the weapon its name. The harmonic frequency was specifically chosen to match the resonant failure point of common body armor polymers, allowing the blade to slice through protective materials that would stop a conventional edge.\n\nCrucible Industries developed the Resonance Katana as a prestige product for their executive protection line — a melee weapon elegant enough to be carried openly in corporate settings without appearing crude, while maintaining genuine combat lethality. The handle houses a micro-capacitor that powers the vibration system for approximately four hours of continuous use, and the blade's carbon-lattice construction makes it lighter than a traditional steel katana while being substantially harder.\n\nThe weapon has found an unexpected secondary market among GLMZ's dueling culture, where augmented combatants settle disputes in semi-legal arenas. The Singing Edge's ability to defeat armor while remaining a recognizably traditional weapon form has made it the prestige choice for duelists who value aesthetics alongside function. Master bladesmiths at Crucible hand-tune each blade's resonance frequency, and no two Singing Edges produce exactly the same tone.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — registered melee weapon, executive carry permit",
    base_technologies: ["Piezoelectric edge vibration", "Carbon-lattice composite blade", "Harmonic armor-resonance tuning"],
    specifications: "blade_length: 71 cm\ntotal_length: 102 cm\nweight: 0.9 kg\nedge_oscillation: 40,000 Hz\npower_source: Handle-integrated micro-capacitor, 4 hours continuous\nblade_material: Layered carbon-lattice composite\nhardness: 78 HRC equivalent",
    tactical_use: "The Singing Edge is deployed in close-quarters situations where firearms are impractical, prohibited, or undesirable. Executive bodyguards carry it as a secondary weapon for scenarios where gunfire would cause unacceptable collateral damage. In dueling contexts, the blade's vibration system allows it to defeat opponents' armor without requiring superhuman striking force, leveling the field between augmented and unaugmented combatants. Operators learn to use the audible hum as a psychological weapon — the sound of a Singing Edge activating has ended more confrontations than the blade itself.",
    cultural_context: "The katana form carries cultural weight in GLMZ that transcends its Japanese origins — after generations of diaspora mixing, the weapon represents discipline and martial tradition without belonging exclusively to any heritage. Crucible's decision to build a high-tech katana was a calculated cultural play, and it worked. Owning a Singing Edge signals both wealth and martial competence. The dueling community has developed a ranking system based partly on the tonal quality of a duelist's blade — higher-pitched resonance is considered more prestigious.",
    known_users: ["Crucible Industries executive protection details", "GLMZ dueling circuit elite", "Corporate diplomats with martial training"],
    story_hooks: [
      "A Singing Edge was used in a high-profile duel and the blade's resonance frequency matched a tone that caused a spectator's cochlear implant to malfunction — raising questions about whether the weapon was deliberately tuned to harm augmented bystanders.",
      "A Crucible master bladesmith has gone independent and is producing unauthorized Singing Edges with custom resonance tuning that can defeat specific armor types on request. Crucible wants them shut down."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "katana", "vibro-blade", "carbon-lattice", "tier 3", "prestige", "dueling"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Thermal Longsword TL-3 'Cauterizer'",
    type: "weapon",
    aliases: ["Cauterizer", "TL-3", "The Iron", "Burnblade"],
    category: "melee",
    description: "A longsword-profile weapon with a blade that incorporates a resistive heating element running the full length of its cutting edge, raising the blade temperature to 1,400 degrees Celsius within three seconds of activation. The Cauterizer does not merely cut — it melts through materials that a conventional blade could never penetrate, leaving cauterized wounds that seal as they open and slag-edged cuts through metal and composite barriers.\n\nThe blade itself is constructed from a tungsten-ceramic composite that withstands its own operating temperature without deformation. A thermal insulation layer separates the heated edge from the blade's spine, allowing the weapon to be wielded without protective gloves — though the radiant heat from the active edge is uncomfortable at close proximity and can cause minor burns to exposed skin within 30 centimeters. The handle contains a compact fuel cell that provides approximately 90 minutes of continuous heating.\n\nArcturus designed the Cauterizer for breaching operations where powered cutting tools are too bulky and explosives too dangerous. The weapon can cut through reinforced doors, security barriers, and light vehicle armor given 10-15 seconds of sustained contact. Its adoption as a combat weapon was driven by field operatives who discovered that the psychological impact of a glowing blade was as tactically valuable as its cutting capability.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Restricted — thermal weapon classification",
    base_technologies: ["Resistive edge heating element", "Tungsten-ceramic thermal blade", "Compact hydrogen fuel cell"],
    specifications: "blade_length: 85 cm\ntotal_length: 118 cm\nweight: 2.1 kg\nedge_temperature: 1,400°C operational\nheat_time: 3 seconds to operational temperature\npower_source: Compact hydrogen fuel cell, 90 minutes continuous\nblade_material: Tungsten-ceramic composite\nthermal_insulation: Aerogel-reinforced spine barrier",
    tactical_use: "The Cauterizer serves dual roles as a breaching tool and close-combat weapon. Breaching teams use it to cut through barriers silently when explosive charges would alert defenders or risk structural damage. In combat, the weapon's thermal edge defeats any personal armor by melting through it rather than relying on kinetic force. The radiant heat creates a 30cm danger zone around the active blade, discouraging grappling and close-in defense. The visible glow of the heated edge in dim environments makes the wielder a conspicuous target, requiring tactical awareness about when to activate.",
    cultural_context: "The glowing blade of an active Cauterizer has become one of GLMZ's most recognizable visual threats. Security footage of thermal longsword breaches has circulated widely enough that the image carries immediate cultural meaning: someone with a Cauterizer is not negotiating. The weapon has also developed a grim reputation in GLMZ's medical community — cauterized wounds are notoriously difficult to treat, as the thermal damage extends deep into surrounding tissue and resists standard surgical repair.",
    known_users: ["Arcturus breaching teams", "Corporate heavy assault operators", "Tier 4+ enforcement specialists"],
    story_hooks: [
      "A series of vault breaches used Cauterizers to cut through walls rather than doors — bypassing all security on the entry points. The thieves left the slag-edged cuts as calling cards.",
      "A Cauterizer has been recovered with its thermal limiter removed — the edge temperature exceeded 2,000°C and partially melted the blade's own structure. Someone is pushing the weapon beyond its design limits for a specific target."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "longsword", "thermal", "breaching", "tier 4", "tungsten-ceramic"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Diaspora Blade 'Mongrel'",
    type: "weapon",
    aliases: ["Mongrel", "The Mutt", "Diaspora Steel", "Mixed Blood"],
    category: "melee",
    description: "A hand-forged hybrid sword that combines blade geometry from multiple martial traditions — Japanese curvature, European fuller, Southeast Asian blade widening — into a weapon that belongs to no single lineage and functions across all of them. Each Mongrel is unique, reflecting the mixed heritage of its smith and the accumulated martial knowledge of GLMZ's thoroughly blended population.\n\nThe Forge-Smith Collective produces Mongrel blades from a proprietary lamination process that layers carbon-lattice composite with shape-memory alloy sheets. The result is a blade that flexes under impact but returns to its original geometry within milliseconds, absorbing stresses that would permanently deform conventional steel. The shape-memory core also gives the blade a subtle self-sharpening property — micro-deformations at the cutting edge reset themselves, maintaining sharpness over extended use without manual honing.\n\nThe Mongrel has become a cultural icon in GLMZ's lower tiers, where the weapon's mixed-heritage design philosophy resonates with a population that has long since stopped pretending that cultural purity is possible or desirable. Smiths who produce Mongrel blades are expected to incorporate design elements from at least three distinct martial traditions, and the most respected pieces weave five or more into a cohesive whole. The weapon is proof that fusion produces strength.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-3 (artisan availability)",
    legality: "Unlicensed — artisan production, no corporate registration",
    base_technologies: ["Multi-tradition blade geometry", "Carbon-lattice and shape-memory alloy lamination", "Self-restoring edge geometry"],
    specifications: "blade_length: 60-80 cm (varies by smith)\ntotal_length: 85-110 cm\nweight: 1.0-1.5 kg\nblade_material: Carbon-lattice / shape-memory alloy laminate\nedge_retention: Self-restoring via shape-memory core\nflex_recovery: <5 milliseconds to original geometry",
    tactical_use: "The Mongrel's hybrid geometry allows it to be used in multiple fighting styles without the compromises inherent in adapting a single-tradition weapon to unfamiliar techniques. The shape-memory core absorbs impacts that would notch or bend a conventional blade, making it exceptionally durable in extended combat. The self-sharpening edge means the weapon maintains its cutting performance through prolonged engagements — a significant advantage in the lower tiers where access to professional weapon maintenance is limited.",
    cultural_context: "The Mongrel blade is GLMZ distilled into steel and composite. It represents the city's fundamental identity: everything mixed, nothing pure, stronger for the blending. Owning a Mongrel from a respected smith carries social weight comparable to corporate prestige items — it signals belonging to a community that values skill and adaptation over brand loyalty. The Forge-Smith Collective holds an annual competition where new Mongrel designs are judged by a panel of martial practitioners from diverse traditions, and winning designs are reproduced (with modifications) by other smiths as a form of respect.",
    known_users: ["Lower-tier martial practitioners", "Forge-Smith Collective members", "Street fighters and independent operators"],
    story_hooks: [
      "A legendary smith has died and her final Mongrel blade — incorporating seven martial traditions — has become the subject of a violent succession dispute among her former apprentices.",
      "A corporate weapons designer has reverse-engineered the shape-memory lamination process and is mass-producing cheap Mongrel imitations. The Forge-Smith Collective views this as theft of cultural identity, not just intellectual property."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "sword", "hybrid", "artisan", "shape-memory", "carbon-lattice", "tier 1", "cultural"]
  },
  {
    id: id(),
    name: "Crucible Industries Vibro-Rapier VR-2 'Fencer'",
    type: "weapon",
    aliases: ["Fencer", "VR-2", "The Needle", "Tremor Point"],
    category: "melee",
    description: "A thrusting sword with an extremely narrow blade that incorporates a longitudinal vibration system along its central axis. Unlike wider vibro-blades that use lateral oscillation to enhance cutting, the Fencer's vibration runs tip-to-hilt, creating a micro-hammering effect at the point that allows the blade to penetrate armor through resonance-assisted micro-fracturing rather than brute force.\n\nThe VR-2's blade is barely 12 millimeters wide — too narrow to be effective as a cutting weapon — but the longitudinal vibration transforms it into an armor-piercing instrument of surgical precision. The micro-hammering at the tip creates a cascade of hairline fractures in armor material ahead of the blade's advance, effectively pre-breaking whatever it is about to pierce. Against body armor rated for ballistic threats, the Fencer simply ignores the protection.\n\nCrucible designed the weapon for a specific tactical niche: eliminating armored targets in environments where the noise and collateral damage of firearms are unacceptable. Corporate espionage teams, covert entry specialists, and certain medical practitioners who have found alternative applications for the technology have all adopted the VR-2. The weapon requires significant training to use effectively — its narrow blade offers no blocking surface and its exclusively thrusting technique demands precision footwork and timing.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — concealed carry permit required",
    base_technologies: ["Longitudinal vibration armor-piercing", "Resonance-assisted micro-fracturing", "Narrow-profile carbon-lattice blade"],
    specifications: "blade_length: 90 cm\ntotal_length: 112 cm\nblade_width: 12 mm\nweight: 0.6 kg\nvibration_axis: Longitudinal (tip-to-hilt)\nfrequency: 28,000 Hz\npower_source: Guard-integrated capacitor, 6 hours continuous\nblade_material: Carbon-lattice composite",
    tactical_use: "The Fencer is deployed when an armored target must be eliminated silently. The weapon's vibration-assisted penetration defeats personal armor without the sound or energy signature of a firearm. Operators typically target joints in powered armor, gaps in ballistic vests, and areas where cybernetic hardpoints meet biological tissue. The weapon's narrow profile allows it to be concealed in a cane or collapsed umbrella housing, making it popular among operatives who must pass through security screening.",
    cultural_context: "The VR-2 has revived interest in classical fencing techniques in GLMZ's martial community. Training schools that teach European thrusting traditions have seen enrollment increases as the weapon has demonstrated that historical fighting systems have genuine tactical relevance when backed by modern materials science. The resulting 'neo-fencing' movement blends classical form with BCI-enhanced reflexes, producing a fighting style that is both ancient and futuristic.",
    known_users: ["Corporate covert elimination specialists", "Neo-fencing practitioners", "Concealed-carry operatives"],
    story_hooks: [
      "A Tier 4 executive was killed by a single VR-2 thrust through a supposedly impenetrable personal shield — the blade's resonance frequency was specifically tuned to the shield's harmonic weakness. Someone had detailed technical intelligence on the victim's protection.",
      "A neo-fencing tournament has been infiltrated by an operative using the event as cover for a real assassination — the target is one of the competitors."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "rapier", "vibro-blade", "armor-piercing", "tier 3", "concealed", "precision"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Combat Falchion CF-5 'Cleaver'",
    type: "weapon",
    aliases: ["Cleaver", "CF-5", "The Butcher", "Wedge"],
    category: "melee",
    description: "A heavy single-edged chopping sword with a forward-weighted blade designed for maximum kinetic energy transfer on impact. The CF-5 does not vibrate, heat, or resonate — it is simply an extremely well-engineered piece of sharpened metal built from aerogel-reinforced tungsten composite that combines devastating weight with a blade tough enough to chop through structural steel without chipping.\n\nArcturus developed the Cleaver for field engineers and combat sappers who needed a tool capable of cutting through cables, conduits, light barriers, and — when necessary — people, without relying on power sources that might fail in adverse conditions. The weapon requires no batteries, no fuel cells, no capacitors. It works when everything else has stopped working, and it works by being heavy, sharp, and nearly indestructible.\n\nThe CF-5 has earned a reputation as the most honest weapon in GLMZ — it makes no pretense of sophistication and offers no technological crutch. Its weight (2.8 kg) makes it exhausting to wield for extended periods, limiting it to short, decisive engagements. Users either end the fight in the first three swings or they are in serious trouble. This binary outcome has given the weapon a fatalistic cult following among operators who prefer certainty to complexity.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 2+",
    legality: "Licensed — industrial/combat tool classification",
    base_technologies: ["Aerogel-reinforced tungsten composite", "Forward-weighted blade geometry", "Zero-power-dependency design"],
    specifications: "blade_length: 55 cm\ntotal_length: 75 cm\nweight: 2.8 kg\nblade_material: Aerogel-reinforced tungsten composite\nedge_hardness: 72 HRC equivalent\npower_requirement: None\nmaintenance: Manual sharpening only",
    tactical_use: "The Cleaver excels in environments where electronic weapons fail — EMP zones, power-denial areas, and infrastructure collapse scenarios. Its weight makes each swing a commitment, but the kinetic energy behind that weight defeats light armor and structural materials through sheer force. Combat sappers use it to cut through utility conduits and barrier materials. In combat, the weapon's forward balance makes it devastating in downward strikes but slow to recover, demanding a fighting style built around single decisive blows rather than rapid exchanges.",
    cultural_context: "The CF-5 has become a symbol of analog resilience in a city increasingly dependent on electronic everything. Its users tend to be philosophical about their choice — they speak of 'trusting the edge' in a way that carries implications beyond weapon selection. In GLMZ's occasional infrastructure failures, when powered weapons become expensive paperweights, Cleaver carriers become disproportionately important. The weapon has a small but dedicated following who maintain that the simplest solution is always the most reliable.",
    known_users: ["Arcturus combat engineers", "Infrastructure salvage teams", "Analog-philosophy combatants"],
    story_hooks: [
      "During a district-wide power failure, a single operator armed with a CF-5 held a corridor against a dozen attackers whose powered weapons had all died. The story has become legend, but the operator has disappeared.",
      "An aerogel-reinforced tungsten shipment bound for Arcturus was hijacked — enough material to produce hundreds of Cleavers. Someone is equipping an army with EMP-proof weapons."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "falchion", "analog", "tungsten", "aerogel", "tier 2", "unpowered"]
  },
  {
    id: id(),
    name: "Tessera Corponation Phase Blade PB-3 'Flicker'",
    type: "weapon",
    aliases: ["Flicker", "PB-3", "Phase Blade", "Ghost Sword"],
    category: "melee",
    description: "An experimental blade weapon that uses a rapidly cycling electromagnetic field to alter the molecular alignment of its cutting edge 600 times per second, shifting between rigid and semi-fluid states. When rigid, the blade strikes with the hardness of diamond. When semi-fluid, the edge deforms around defensive contact and reforms on the other side, effectively flowing past blocks and parries. To an observer, the blade appears to flicker between solid and translucent, giving the weapon its name.\n\nTessera developed the Phase Blade using proprietary shape-memory alloy technology that responds to electromagnetic field cycling. The effect is visually unsettling and tactically devastating — an opponent attempting to block a Flicker strike feels their parry connect with something that has the consistency of heavy liquid before the blade resolidifies inside their guard. Traditional sword defense techniques become unreliable against a weapon that does not obey consistent physical rules.\n\nThe PB-3 is phenomenally expensive and mechanically fragile. The electromagnetic cycling system requires precise calibration, and physical shock from impacts gradually degrades the alignment until the blade begins phase-cycling erratically — sometimes remaining fluid when it should be rigid, or rigid when flow-through is needed. Each weapon has a rated operational life of approximately 200 engagements before requiring factory recalibration. Tessera charges Φ15,000 for each recalibration, ensuring a continuous revenue stream.",
    manufacturer: "TESSERA CORPONATION",
    tier_availability: "Tier 4+",
    legality: "Restricted — experimental weapon classification",
    base_technologies: ["Electromagnetic molecular phase cycling", "Shape-memory alloy rapid-state transition", "Adaptive edge rigidity control"],
    specifications: "blade_length: 75 cm\ntotal_length: 100 cm\nweight: 1.2 kg\nphase_cycle_rate: 600 Hz\npower_source: Pommel-integrated quantum cell, 3 hours active cycling\nblade_material: Tessera proprietary shape-memory alloy\noperational_life: ~200 engagements between recalibrations\nrecalibration_cost: Φ15,000",
    tactical_use: "The Flicker negates conventional melee defense by passing through blocks and parries during its semi-fluid phase. Opponents must develop entirely new defensive strategies, typically based on evasion rather than contact. The weapon is most effective in one-on-one engagements where the operator can exploit confusion and unfamiliarity. Against multiple opponents or in chaotic melee, the phase cycling becomes less advantageous as the weapon's fragility and power dependency become liabilities. Experienced Flicker users learn to time their strikes to the rigid phase for maximum damage while using the fluid phase to defeat incoming defense.",
    cultural_context: "The Flicker has become the ultimate prestige weapon in GLMZ's high-tier dueling circles — owning one signals both extraordinary wealth and commitment to melee combat at its most technical. The weapon has also generated philosophical debate about what constitutes a 'fair' weapon in dueling contexts, as many traditionalists argue that a blade that ignores parries violates the fundamental compact of swordfighting. Tessera has leaned into the controversy, marketing the PB-3 as 'the future of the edge.'",
    known_users: ["Tessera Corponation elite security", "Tier 4+ duelists", "Experimental weapons collectors"],
    story_hooks: [
      "A Flicker's phase cycling has begun synchronizing with its wielder's BCI implant, responding to intent rather than pre-programmed timing. Tessera wants the weapon back for study — the wielder considers it their property.",
      "A duelist was killed when their opponent's Flicker malfunctioned mid-bout, remaining in rigid phase during what should have been a non-lethal fluid-phase touch. The death is ruled an accident, but the weapon's maintenance logs have been erased."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "sword", "phase-blade", "shape-memory", "experimental", "tier 4", "prestige"]
  },
  {
    id: id(),
    name: "Street-Custom Gutter Katana 'Flatline'",
    type: "weapon",
    aliases: ["Flatline", "Gutter Blade", "Tier-Zero Katana", "The Equalizer"],
    category: "melee",
    description: "A katana-length blade assembled from industrial waste materials — carbon-lattice offcuts from manufacturing, reclaimed structural composite, and scavenged handle wrapping. The Gutter Katana is not a product; it is a phenomenon. Across GLMZ's lower tiers, self-taught bladesmiths produce these weapons from whatever materials are available, and the results range from barely functional clubs to genuinely dangerous cutting weapons that rival corporate products.\n\nThe Flatline designation refers to a specific lineage of Gutter Katanas produced in the Undercroft district, where a network of anonymous smiths has developed a technique for heat-treating carbon-lattice offcuts that produces a blade of surprising quality. The process is passed from maker to maker without formal instruction — you learn by watching, and you prove your skill by producing a blade that passes a cutting test against a standardized target. Flatline blades are marked with a simple horizontal line etched near the guard.\n\nThe weapon represents the lower tiers' refusal to be disarmed by economics. Corporate weapons cost what lower-tier residents earn in months; a Flatline costs scavenging time and skill. The quality varies, but the cultural statement is consistent: access to violence is not a privilege reserved for those who can afford Crucible's price tags.",
    manufacturer: "UNDERCROFT STREET SMITHS",
    tier_availability: "Tier 1",
    legality: "Unlicensed — unregistered improvised weapon",
    base_technologies: ["Reclaimed carbon-lattice heat treatment", "Improvised composite construction", "Community-transmitted forging techniques"],
    specifications: "blade_length: 60-72 cm (varies)\ntotal_length: 85-100 cm\nweight: 1.0-1.8 kg (varies by construction)\nblade_material: Heat-treated carbon-lattice offcuts\nedge_quality: Variable — ranging from crude to surprisingly refined\npower_requirement: None",
    tactical_use: "The Flatline's effectiveness depends entirely on the skill of the smith who made it. High-quality examples hold an edge comparable to entry-level corporate products and can defeat light body armor through material hardness alone. Lower-quality examples function as impact weapons that happen to have a sharp edge. Users compensate for inconsistency with aggression and familiarity — most Flatline carriers practice obsessively because they know their weapon will not compensate for poor technique the way a vibro-blade might.",
    cultural_context: "The Flatline is more than a weapon in the Undercroft — it is a cultural institution. The horizontal line mark has become a recognized symbol of lower-tier self-sufficiency, appearing as graffiti, tattoo, and brand mark across multiple districts. Corporate security personnel have learned to take Flatline carriers seriously despite the weapon's improvised appearance — the culture that produces these blades also produces the martial discipline to use them. The annual Flatline cutting competition draws hundreds of participants and is one of the Undercroft's most attended public events.",
    known_users: ["Undercroft district residents", "Lower-tier martial practitioners", "Street-level operators on a budget"],
    story_hooks: [
      "A Flatline blade was recovered from a Tier 4 assassination — metallurgical analysis shows it was made from carbon-lattice offcuts sourced from a Crucible Industries factory. Someone is stealing raw materials and feeding them to the street smiths.",
      "The Undercroft's best-known Flatline smith has been kidnapped — and three different factions are claiming responsibility, each demanding a different ransom."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "katana", "improvised", "street-custom", "carbon-lattice", "tier 1", "cultural"]
  },
  {
    id: id(),
    name: "Crucible Industries Harmonic Zweihander HZ-1 'Earthquake'",
    type: "weapon",
    aliases: ["Earthquake", "HZ-1", "The Tremor", "Groundshaker"],
    category: "melee",
    description: "A massive two-handed sword that uses a bass-frequency vibration system tuned to transmit kinetic shockwaves through whatever it strikes. The Earthquake does not merely cut — on impact, its blade discharges a low-frequency pulse that propagates through the target material, causing structural fracturing well beyond the point of contact. A strike against a wall does not just damage the impact point; it sends cracks radiating outward in a two-meter radius.\n\nThe HZ-1's blade is 120 centimeters of aerogel-reinforced carbon composite, thick enough to absorb its own shockwave without self-destructing. The vibration generator is housed in the massive crossguard and produces a subsonic hum that can be felt rather than heard — a deep bone-level vibration that makes nearby observers instinctively uncomfortable. At full power, the weapon's strikes can fracture reinforced concrete and shatter ballistic glass.\n\nCrucible built the Earthquake as a siege weapon for corporate breach teams that needed to break through hardened barriers without explosives. The weapon requires significant physical strength or powered augmentation to wield effectively — at 5.2 kilograms, it is one of the heaviest melee weapons in active service. Users tend to be either heavily augmented operators or naturally large individuals who have built their entire fighting style around the weapon's ponderous but devastating delivery.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Restricted — siege weapon classification",
    base_technologies: ["Bass-frequency shockwave transmission", "Aerogel-reinforced carbon composite blade", "Impact-propagation vibration system"],
    specifications: "blade_length: 120 cm\ntotal_length: 168 cm\nweight: 5.2 kg\nvibration_frequency: 12 Hz (subsonic)\nshockwave_radius: ~2 meters from impact point\npower_source: Crossguard-integrated fuel cell, 2 hours active\nblade_material: Aerogel-reinforced carbon composite",
    tactical_use: "The Earthquake is a breaching and area-denial weapon. Its shockwave propagation means that every strike damages an area rather than a point, making it effective against barriers, clustered opponents, and structural targets. In melee combat, opponents who block the weapon still receive transmitted shockwave damage through their own weapon or shield. The primary limitation is speed — the weapon is too heavy for rapid exchanges, and each swing requires a full-body commitment that leaves the wielder momentarily vulnerable.",
    cultural_context: "The HZ-1 has become associated with a particular archetype in GLMZ's combat culture: the heavy hitter who sacrifices speed and subtlety for absolute destructive power. Earthquake wielders are both respected and avoided — their presence changes the geometry of any engagement. The weapon's subsonic hum has been sampled by GLMZ's industrial music scene, and the term 'earthquake drop' in music refers to a bass hit that vibrates the listener's chest.",
    known_users: ["Crucible Industries siege teams", "Augmented heavy combatants", "Corporate breach specialists"],
    story_hooks: [
      "An Earthquake strike caused a structural cascade that collapsed a section of lower-tier housing — killing fourteen people who were not involved in the fight. The wielder claims it was a weapon malfunction; engineering analysis suggests the weapon performed exactly as designed.",
      "A miniaturized version of the shockwave generator has appeared in the black market, small enough to be integrated into a standard-sized weapon. The technology has leaked from Crucible."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "zweihander", "vibro-blade", "shockwave", "siege", "tier 4", "heavy"]
  }
];

// ─── POWER AXES — VIBRO-AXES, PLASMA-EDGE, THERMAL (5) ────────────────

const powerAxes = [
  {
    id: id(),
    name: "Arcturus Defense Solutions Vibro-Axe VA-4 'Tremor'",
    type: "weapon",
    aliases: ["Tremor", "VA-4", "The Shaker", "Splitjaw"],
    category: "melee",
    description: "A single-bladed combat axe with a transverse vibration system that oscillates the cutting edge perpendicular to the blade face at 35,000 cycles per second. The VA-4's vibration pattern is specifically engineered for chopping rather than slicing — each impact is amplified by the lateral oscillation, which drives the edge deeper into the target material with every microsecond of contact. A Tremor strike against armored plate does not bounce or deflect; it digs in and keeps going.\n\nArcturus designed the Vibro-Axe for field demolition and obstacle clearance — cutting through structural supports, locked hatches, and barricade materials that resist conventional cutting tools. The weapon's axe geometry transfers more kinetic energy per swing than a sword-profile blade, and the vibration system multiplies that energy's effect on the target. The result is a breaching tool that requires no power grid connection and weighs less than a portable cutting laser.\n\nThe VA-4 has found its primary combat audience among augmented operators who fight in confined industrial environments — maintenance corridors, machinery spaces, and infrastructure tunnels where long blades are impractical. The axe's compact head and one-handed capability allow it to be used in spaces too tight for swords, while its vibration-enhanced cutting power ensures that armor and barriers offer minimal protection.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Licensed — industrial/combat dual-classification",
    base_technologies: ["Transverse vibration edge system", "Carbon-lattice axe head", "Impact-amplification oscillation"],
    specifications: "head_width: 18 cm cutting edge\nhandle_length: 45 cm\ntotal_weight: 1.6 kg\nvibration_frequency: 35,000 Hz transverse\npower_source: Handle-integrated capacitor, 5 hours continuous\nhead_material: Carbon-lattice composite\none_handed: Yes",
    tactical_use: "The Tremor excels in confined-space combat where its compact profile and devastating chopping power outperform longer weapons. The vibration system's transverse oscillation makes the weapon difficult to parry — contact with a blocking weapon or shield transmits the vibration into the defender's arm, causing numbness and loss of grip. Operators in maintenance corridors and industrial environments use the VA-4 as their primary weapon, switching to it from firearms when engagement distances drop below five meters.",
    cultural_context: "The Vibro-Axe has become the signature weapon of GLMZ's infrastructure workers — the maintenance crews, tunnel rats, and pipe-runners who keep the city's systems functioning in spaces no one else willingly enters. Many carry the VA-4 as both tool and weapon, and a vibro-axe hanging from a tool belt signals someone who works in the deep infrastructure. The maintenance worker subculture has developed a brutal but effective fighting style built around the weapon's confined-space strengths.",
    known_users: ["Arcturus industrial teams", "Infrastructure maintenance crews", "Confined-space combat specialists"],
    story_hooks: [
      "Maintenance workers in a lower-tier tunnel system have stopped responding to calls. When a team goes to investigate, they find the tunnels barricaded with industrial debris — and the workers inside are armed with VA-4s, defending something they found in the deep infrastructure.",
      "A modified VA-4 has been recovered with its vibration frequency altered to match the resonant frequency of a specific type of structural beam — someone is preparing to bring down a building with hand tools."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "axe", "vibro-axe", "confined-space", "tier 3", "industrial"]
  },
  {
    id: id(),
    name: "Crucible Industries Plasma-Edge Axe PE-2 'Sunstrike'",
    type: "weapon",
    aliases: ["Sunstrike", "PE-2", "Plasma Axe", "The Star"],
    category: "melee",
    description: "A bearded axe with a cutting edge that generates a thin plasma sheath along the blade when activated. The PE-2 uses a miniaturized plasma containment system — adapted from industrial cutting torch technology — to create a 3-millimeter layer of ionized gas running the length of the axe head's cutting edge. At approximately 8,000 degrees Celsius, the plasma sheath vaporizes material on contact before the physical blade arrives, effectively clearing a path for the metal edge through whatever it encounters.\n\nThe weapon is terrifyingly effective against all conventional materials. Armor, structural composite, reinforced barriers, and biological tissue are all equally vulnerable to a plasma-temperature cutting surface. The PE-2's limitation is its power consumption — the plasma sheath drains the weapon's compact tokamak cell in approximately twelve minutes of continuous operation, and each activation cycle produces an intense visible flare and a wave of radiant heat that makes stealth impossible.\n\nCrucible engineered the Sunstrike as a specialist breaching weapon for scenarios where thermal lances are too slow and explosives too dangerous. The axe geometry allows operators to chop through barriers with directed, controlled strikes rather than the sustained contact required by cutting tools. In the field, the weapon has earned its name from the blinding flash it produces during activation — fighting against a Sunstrike in dim conditions has been compared to trying to parry a welding arc.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — plasma weapon classification",
    base_technologies: ["Miniaturized plasma containment sheath", "Tokamak micro-cell power source", "Adapted industrial plasma cutting"],
    specifications: "head_width: 22 cm cutting edge\nhandle_length: 60 cm\ntotal_weight: 2.4 kg\nplasma_temperature: ~8,000°C\nplasma_sheath_depth: 3 mm\npower_source: Compact tokamak cell, 12 minutes continuous\nactivation_signature: High-intensity visible flare + thermal bloom",
    tactical_use: "The Sunstrike is a door-opener in the most literal sense — it cuts through anything that bars passage. Operators activate the plasma edge for individual strikes rather than maintaining continuous operation, extending the effective use time to dozens of breaching cuts per charge. In combat, the weapon's plasma sheath makes it functionally unblockable — any weapon or shield that contacts the edge is destroyed. The radiant heat and light, however, mark the wielder as the most visible target in any engagement, requiring team support to operate safely.",
    cultural_context: "Plasma-edge weapons occupy a mythological space in GLMZ's culture — they are as close to a 'magic sword' as technology allows. The Sunstrike's blinding activation flare has been captured in countless security recordings, and the image of a glowing axe cutting through a vault door has become iconic. The weapon's extreme cost (Φ85,000 base, plus Φ3,000 per tokamak cell) restricts it to military and high-tier corporate use, but its reputation extends to every tier.",
    known_users: ["Crucible Industries military demonstration teams", "Tier 5 corporate breach specialists", "Strategic asset denial operators"],
    story_hooks: [
      "A Sunstrike was used to cut into a sealed bio-containment lab — whatever was released killed everyone in the building within minutes. The operator who made the cut survived, sealed in a suit. What were they retrieving?",
      "A black-market tokamak cell has been modified to overcharge the plasma sheath, doubling its temperature but reducing operation time to ninety seconds. The resulting weapon can cut through anything — including the axe's own containment field if the operator is not careful."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "axe", "plasma", "breaching", "tier 5", "military", "thermal"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Thermal Cleave 'Ember'",
    type: "weapon",
    aliases: ["Ember", "Thermal Cleave", "Hot Iron", "The Brand"],
    category: "melee",
    description: "A double-bitted axe with both heads incorporating resistive heating elements that bring the cutting edges to 900 degrees Celsius — hot enough to melt through standard body armor and cauterize flesh on contact, but cool enough to be managed with standard heat-resistant gloves rather than the full thermal protection required by higher-temperature weapons. The Ember represents the Forge-Smith Collective's philosophy of practical lethality: hot enough to work, cool enough to carry.\n\nThe weapon's heating elements are powered by a chemical fuel cell integrated into the axe handle — a ferrocerium-based system that ignites on activation and burns steadily for approximately two hours. Unlike electronic heating systems that can be disrupted by EMP or power denial, the Ember's chemical heat source is immune to electronic interference. Once lit, it burns until the fuel is exhausted or the wielder manually vents the reaction chamber.\n\nThe double-bitted design serves a practical purpose beyond aesthetics: when one edge loses optimal temperature from repeated contact with cool targets, the wielder rotates to the second edge while the first recovers. In extended engagements, Ember wielders develop a rhythmic alternating pattern — strike, rotate, strike — that maintains consistent thermal cutting performance throughout the fight.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 2+ (artisan availability)",
    legality: "Unlicensed — artisan production, thermal weapon restrictions vary by district",
    base_technologies: ["Chemical resistive heating", "Ferrocerium fuel cell", "Double-bitted thermal rotation design"],
    specifications: "head_width: 15 cm per edge (double-bitted)\nhandle_length: 55 cm\ntotal_weight: 2.2 kg\nedge_temperature: 900°C\npower_source: Ferrocerium chemical cell, 2 hours continuous\nEMP_vulnerability: None — chemical heat source\nglove_requirement: Standard heat-resistant",
    tactical_use: "The Ember is a middle-ground thermal weapon — less spectacular than plasma-edge designs but far more practical and sustainable. Its two-hour burn time and EMP immunity make it reliable in the extended, chaotic engagements common in GLMZ's lower tiers. The double-bitted rotation technique allows sustained thermal cutting without performance degradation. Against armored opponents, the 900°C edge softens and melts polymer armor components, creating gaps that subsequent strikes exploit. The weapon's chemical heat signature is visible on thermal imaging, making it poor for stealth — but anyone wielding a double-headed burning axe has already abandoned subtlety.",
    cultural_context: "The Ember has become the Forge-Smith Collective's most recognizable weapon — its burning edges are visible from a distance, and the chemical smell of the ferrocerium fuel is distinctive enough to serve as a warning. Forge-smiths who specialize in thermal axes are called 'brands' within the Collective, and their work is considered a distinct discipline from blade-making. The double-bitted rotation technique has become a recognized martial form, with formal instruction available in lower-tier combat schools.",
    known_users: ["Forge-Smith Collective thermal specialists", "Lower-tier heavy combatants", "Salvage and demolition crews"],
    story_hooks: [
      "A batch of Ember fuel cells has been contaminated with an accelerant that causes them to reach 1,600°C — well beyond the axe head's rated temperature. Several wielders have been injured when their weapons partially melted during use. Sabotage or manufacturing error?",
      "An Ember-wielding enforcer has been marking victims with the weapon's hot edge before killing them — a brand in the shape of a specific corporate logo. Someone is sending a message."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "axe", "thermal", "double-bitted", "artisan", "tier 2", "EMP-immune"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Siege Axe SA-1 'Rampart'",
    type: "weapon",
    aliases: ["Rampart", "SA-1", "Siege Axe", "The Hammer of God"],
    category: "melee",
    description: "A two-handed axe of extraordinary size that integrates a kinetic energy storage system in its handle — each swing charges a flywheel mechanism that releases stored energy on impact, effectively doubling the force of every strike beyond the first. The SA-1 was designed for breaching reinforced positions that resist all other man-portable cutting systems, and it achieves this through cumulative energy buildup that makes each successive blow more powerful than the last.\n\nThe Rampart's axe head is constructed from Ablonite-KR reactive ceramic bonded to a tungsten core. The ceramic outer layer is self-sharpening — it fractures along crystallographic planes that maintain a keen edge, and micro-fractures at the cutting surface expose fresh sharp ceramic with each impact. The tungsten core provides mass and structural integrity that the ceramic alone could not sustain under the weapon's enormous impact forces.\n\nArcturus markets the SA-1 exclusively to military and Tier 5 corporate clients, and each unit ships with a warning label that reads: 'This weapon is designed to destroy fortifications. It will destroy anything it strikes. User assumes all responsibility for collateral damage.' The company is not exaggerating. A fully charged Rampart swing has been measured at forces exceeding what a small vehicle generates in a collision.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — siege weapon authorization required",
    base_technologies: ["Kinetic energy flywheel storage", "Ablonite-KR self-sharpening ceramic", "Cumulative impact energy multiplication"],
    specifications: "head_width: 28 cm cutting edge\nhandle_length: 90 cm\ntotal_weight: 6.8 kg\nenergy_storage: Flywheel kinetic accumulator\nimpact_multiplication: 2x base force after 3 swings\nhead_material: Ablonite-KR ceramic / tungsten core\npower_requirement: Self-charging via kinetic energy",
    tactical_use: "The Rampart is used when everything else has failed to breach a target. Its cumulative energy system means that an operator who can sustain three swings against a barrier will hit with double force on the fourth — and the force continues to build. Against fortified doors, blast walls, and armored positions, the SA-1 breaks through materials rated to resist explosive breaching charges. The weapon requires powered augmentation or exceptional physical conditioning to wield, and its combat use is limited to overwhelming single targets rather than engaging multiple opponents.",
    cultural_context: "The SA-1 represents the upper limit of man-portable melee weaponry — beyond the Rampart, you are in the territory of vehicle-mounted systems. Its existence is a reminder that even in an age of directed energy and electromagnetic acceleration, there are problems that respond best to a really big axe. Among military operators, carrying a Rampart is a mark of physical capability that earns immediate respect. Among everyone else, it is a mark of someone you do not want to fight under any circumstances.",
    known_users: ["Arcturus siege warfare division", "Tier 5 fortification breach teams", "Augmented heavy operators"],
    story_hooks: [
      "A Rampart was used to breach a Tier 5 panic room from inside — someone sealed themselves in with the room's occupant and chopped their way out through the back wall. The room's security recordings have been wiped.",
      "Arcturus is developing an SA-2 variant with an electromagnetic flywheel that stores energy from nearby power sources — the weapon grows more powerful when used near electrical infrastructure."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "axe", "siege", "kinetic", "Ablonite-KR", "tier 5", "military", "heavy"]
  },
  {
    id: id(),
    name: "Crucible Industries Cryogenic Axe CA-1 'Frostbite Edge'",
    type: "weapon",
    aliases: ["Frostbite Edge", "CA-1", "Cryo Axe", "Cold Snap"],
    category: "melee",
    description: "A single-bitted combat axe that uses a cryogenic cooling system to reduce its blade temperature to -180 degrees Celsius. At this temperature, most materials become brittle — polymers shatter like glass, metals lose ductility, and biological tissue undergoes immediate frostbite and cellular disruption on contact. The Frostbite Edge does not need to be sharp in the traditional sense; it makes whatever it strikes fragile enough to break under its own impact force.\n\nThe CA-1's cryogenic system uses liquid nitrogen stored in a pressurized handle reservoir, fed through micro-channels in the axe head to maintain the blade at operational temperature. The reservoir holds enough coolant for approximately 45 minutes of continuous cooling, and the weapon can be refilled from standard industrial nitrogen supplies — making it logistically simpler than many high-tech weapons that require proprietary consumables.\n\nCrucible developed the Frostbite Edge for counter-armor operations where cutting through protection is less efficient than making it fragile. A single cryo-axe strike against composite body armor creates a zone of embrittlement that subsequent impacts — even from fists or conventional weapons — can exploit to shatter the protection entirely. In field testing, the CA-1 defeated Level IV ballistic armor in two strikes: one to embrittle, one to shatter.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — cryogenic weapon classification",
    base_technologies: ["Liquid nitrogen cryogenic cooling", "Thermal embrittlement exploitation", "Micro-channel blade refrigeration"],
    specifications: "head_width: 20 cm cutting edge\nhandle_length: 50 cm\ntotal_weight: 2.0 kg with full reservoir\nblade_temperature: -180°C operational\ncoolant: Liquid nitrogen, standard industrial grade\nreservoir_duration: 45 minutes continuous\nrefill_source: Standard industrial nitrogen supply",
    tactical_use: "The Frostbite Edge is most effective as a first-strike weapon against armored targets. The initial cryo strike embrittles the armor, and follow-up strikes from any weapon — including the CA-1 itself — shatter the weakened protection. Operators coordinate with teammates: the cryo-axe wielder makes the first contact, and allies exploit the vulnerability. Against unarmored biological targets, the weapon causes immediate deep-tissue frostbite and cellular necrosis. The visible condensation cloud around the active blade provides a psychological deterrent and a tactical signature that enemies can track.",
    cultural_context: "Cryogenic weapons occupy a peculiar cultural niche in GLMZ — they are considered less brutal than thermal weapons despite causing comparable tissue damage. The 'clean' appearance of cryogenic injury (no blood, no burning) has created a perception of sophistication that is medically unfounded. The Frostbite Edge has been adopted by several corporate security teams specifically because cryogenic engagement produces less disturbing security footage than incendiary alternatives — a calculus based entirely on public relations rather than target welfare.",
    known_users: ["Crucible Industries counter-armor teams", "Corporate security details (PR-conscious)", "Cryo-weapons specialists"],
    story_hooks: [
      "A Frostbite Edge was used to freeze and shatter a bio-containment seal — releasing something that was kept frozen for a reason. Now it is thawing, and nobody knows what it does at room temperature.",
      "Someone has figured out how to fill the CA-1 reservoir with liquid helium instead of nitrogen, reaching -269°C. At that temperature, the weapon causes quantum-scale material effects that Crucible's engineers cannot fully explain."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "axe", "cryogenic", "counter-armor", "tier 3", "embrittlement"]
  }
];

// ─── DAGGERS AND KNIVES — EXOTIC SUBSTRATES (8) ───────────────────────

const daggers = [
  {
    id: id(),
    name: "Crucible Industries Ceramic Stiletto CS-4 'Phantom Needle'",
    type: "weapon",
    aliases: ["Phantom Needle", "CS-4", "Ghost Pin", "The Invisible"],
    category: "melee",
    description: "A narrow-profile dagger constructed entirely from Ablonite-KR reactive ceramic, making it completely invisible to metal detectors and standard weapon-detection scanning systems. The Phantom Needle is 22 centimeters of translucent white ceramic, honed to a molecular edge that cuts with surgical precision. The weapon was designed for a single purpose: to pass through security screening and arrive at its destination undetected.\n\nAblonite-KR's reactive properties give the blade an unusual secondary effect — on contact with ferrous materials (including many cybernetic implant housings), the ceramic surface undergoes a micro-reaction that generates localized heat, effectively welding the blade to the metal it touches. Operators who understand this property use it to their advantage, driving the blade into a cybernetic joint and twisting to create a fusion bond that makes extraction agonizingly difficult without surgical intervention.\n\nCrucible officially manufactures the CS-4 for 'ceramic materials testing and industrial sample preparation.' The weapon ships in a case designed to look like a laboratory instrument kit. No one involved in the transaction is confused about the product's actual purpose.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+ (discreet availability)",
    legality: "Technically legal as industrial tool — functionally illegal as concealed weapon",
    base_technologies: ["Ablonite-KR reactive ceramic construction", "Molecular-edge honing", "Zero metallic detection signature"],
    specifications: "blade_length: 22 cm\ntotal_length: 32 cm\nweight: 85 g\nblade_material: Ablonite-KR reactive ceramic\ndetection_signature: Zero metallic, minimal density on X-ray\nedge_type: Molecular hone — single-use sharpness\nreactive_property: Micro-fusion on ferrous contact",
    tactical_use: "The Phantom Needle is an assassination tool designed to reach places where weapons cannot go. Its zero detection signature allows it to pass through security checkpoints that screen for metallic weapons, and its low density makes it difficult to identify on X-ray without specifically calibrated scanning. Operators conceal it in clothing, prosthetics, or purpose-built body cavities. The weapon's molecular edge is devastatingly sharp on first use but degrades with each cut — it is designed for one or two precise strikes, not extended combat.",
    cultural_context: "The existence of undetectable ceramic weapons has created a persistent anxiety in GLMZ's security industry. Every checkpoint, every screening system, every pat-down carries the knowledge that Ablonite-KR blades can pass through invisibly. Security contractors have responded by adding chemical sniffers and density scanners to their screening protocols — measures that are expensive, imperfect, and have created a secondary market for ceramic weapons with masking coatings. The arms race between detection and evasion continues.",
    known_users: ["Corporate assassination contractors", "Covert operatives requiring undetectable weapons", "High-tier infiltration specialists"],
    story_hooks: [
      "A Phantom Needle was found embedded in a Tier 5 executive's cybernetic spine — the reactive ceramic fused to the implant housing, and removing it would sever the spinal connection. The executive is alive but the blade is still inside.",
      "Crucible's 'industrial testing' cover has been exposed by a journalist. The company's legal team is moving to suppress the story, but copies are already circulating in underground media."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "dagger", "ceramic", "Ablonite-KR", "stealth", "undetectable", "tier 3", "assassination"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Carbon-Lattice Karambit 'Black Claw'",
    type: "weapon",
    aliases: ["Black Claw", "Carbon Karambit", "Hook Fang", "The Crescent"],
    category: "melee",
    description: "A curved fighting knife in the karambit profile — a Southeast Asian design featuring a hooked blade and a finger ring for retention — constructed from solid carbon-lattice composite. The Black Claw's curved blade is optimized for draw cuts and hooking strikes, and the carbon-lattice material gives it an edge hardness that exceeds most commercial ceramics while maintaining the toughness to absorb impacts without chipping.\n\nThe Forge-Smith Collective's karambit specialists produce Black Claws using a proprietary pressure-forming process that aligns the carbon lattice along the blade's curve, concentrating material strength along the cutting edge. The result is a knife that can be driven point-first into hardened steel and extracted without damage — a party trick that Black Claw makers use to demonstrate their product's quality and that has become an informal acceptance test for new production.\n\nThe karambit's finger ring is not decorative — it allows the blade to be retained during grappling, rotated between forward and reverse grip without releasing the handle, and used as a striking surface in its own right. The Black Claw's ring is machined from the same carbon-lattice composite as the blade, thick enough to function as a knuckle impact weapon when the blade is folded back against the forearm in a concealed carry position.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-2 (artisan availability)",
    legality: "Unlicensed — artisan production",
    base_technologies: ["Pressure-formed carbon-lattice composite", "Curve-aligned material grain", "Integrated retention ring design"],
    specifications: "blade_length: 10 cm (curved measurement)\ntotal_length: 20 cm\nweight: 120 g\nblade_material: Curve-aligned carbon-lattice composite\nedge_hardness: 82 HRC equivalent\nretention: Integral finger ring\ngrip_options: Forward, reverse, concealed",
    tactical_use: "The Black Claw is a close-quarters grappling weapon designed for chaotic, body-to-body combat where longer weapons are useless. The finger ring ensures the weapon cannot be disarmed through conventional techniques, and the curved blade is optimized for draw cuts against soft tissue — throat, inner arm, inner thigh. Experienced users flow between forward and reverse grip mid-fight, presenting different threat angles without pause. The weapon's small size makes it concealable in a forearm sheath or waistband holster.",
    cultural_context: "The karambit's Southeast Asian heritage resonates in GLMZ's thoroughly blended population — it is a weapon from one of dozens of martial traditions that have been mixed and adapted into the city's combat culture. The Black Claw has become a standard sidearm for close-quarters fighters across all tiers, and the finger ring has been adopted as a subtle recognition symbol — wearing a ring on the index finger of the dominant hand signals karambit proficiency to those who know what to look for.",
    known_users: ["Close-quarters combat specialists", "Lower-tier martial practitioners", "Grappling-focused fighters"],
    story_hooks: [
      "A series of killings shares an identical wound pattern — curved draw cuts consistent with a Black Claw, but the angle and depth suggest augmented speed. Someone with reflex enhancements is using an artisan blade to commit murders that look like street-level violence.",
      "A Forge-Smith karambit specialist has been commissioned to produce a Black Claw with specific dimensional requirements — the buyer wants a blade that fits precisely into a particular cybernetic forearm housing. Custom assassination hardware."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "dagger", "karambit", "carbon-lattice", "artisan", "tier 1", "grappling", "concealed"]
  },
  {
    id: id(),
    name: "Crucible Industries Memory Alloy Switchblade MS-6 'Recall'",
    type: "weapon",
    aliases: ["Recall", "MS-6", "Memory Blade", "The Comeback"],
    category: "melee",
    description: "A folding knife with a blade constructed from shape-memory alloy that returns to its factory-set geometry after being deformed by impact, bending, or deliberate reshaping. The Recall's blade can be bent 90 degrees, twisted, or even partially flattened, and within seconds it will flow back to its original razor-edged profile. The weapon cannot be permanently damaged by any force short of melting it.\n\nThe MS-6's shape-memory blade is activated by body heat — the alloy's transition temperature is set at 32 degrees Celsius, meaning the blade begins self-correcting the moment it is held in a warm hand. When cold, the blade is malleable enough to be bent and hidden in configurations that do not resemble a weapon — folded flat against a belt buckle, coiled inside a bracelet housing, or compressed into a cylinder that looks like a pen. Once warmed by body contact, it unfolds and rigidizes into a fighting knife within four seconds.\n\nCrucible developed the Recall as a concealment weapon for operatives who face thorough physical searches. The blade's ability to be deformed into non-weapon shapes defeats visual inspection and manual pat-downs. Standard weapon scanners detect its metallic signature, but the shape presented to the scanner does not match weapon profiles in detection databases, often resulting in the blade being classified as a harmless personal item.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Licensed — concealed weapon permit required",
    base_technologies: ["Shape-memory alloy blade", "Body-heat activation threshold", "Deformation-concealment design"],
    specifications: "blade_length: 12 cm (deployed)\ntotal_length: 24 cm (deployed), variable when deformed\nweight: 140 g\nblade_material: Crucible proprietary shape-memory alloy\ntransition_temperature: 32°C (body heat activation)\nrecovery_time: 4 seconds from maximum deformation\nedge_retention: Self-restoring — infinite edge life",
    tactical_use: "The Recall is a last-resort weapon for operatives who have been disarmed or searched. Its deformation concealment allows it to survive screening that would catch rigid blades, and its body-heat activation means it is always available when held. The self-restoring edge means the blade never needs sharpening — each use returns it to factory precision. In combat, the blade's shape-memory properties make it resistant to techniques that target the weapon (bending the blade out of line, for example, is pointless). The primary limitation is the blade's modest size — it is a close-combat knife, not a fighting sword.",
    cultural_context: "The Recall has become a symbol of persistence in GLMZ's operative culture — a weapon that cannot be broken, only temporarily inconvenienced. The phrase 'memory alloy' has entered slang to describe a person who bounces back from setbacks with their original form intact. The weapon's concealment capability has also made it a favorite among individuals who live in permanent states of potential threat — people who sleep with a Recall hidden in their clothing because they never know when they will need to fight their way out.",
    known_users: ["Deep-cover operatives", "Corporate espionage agents", "High-risk individuals requiring concealment weapons"],
    story_hooks: [
      "A Recall blade was found at a crime scene in a shape that should not be possible — the alloy was deformed into a configuration that exceeds its rated recovery capability, suggesting someone has modified the memory alloy's crystal structure.",
      "A political prisoner was stripped, searched, and held in a bare cell for three days. On the fourth day, they were found with a Recall blade. No one can explain how it got past the search — or who provided it."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knife", "shape-memory", "concealed", "tier 2", "self-restoring", "infiltration"]
  },
  {
    id: id(),
    name: "Tessera Corponation Neural-Edge Tanto NT-1 'Whisper Cut'",
    type: "weapon",
    aliases: ["Whisper Cut", "NT-1", "Neural Tanto", "The Quiet"],
    category: "melee",
    description: "A tanto-profile fixed blade with an edge coated in a piezoelectric polymer that generates a localized electrical field on contact with biological tissue. The field is precisely calibrated to interfere with peripheral nerve signaling, causing immediate localized numbness that spreads outward from the wound site. Victims of a Whisper Cut do not feel the injury for 15-30 seconds — by which time the operator has typically inflicted multiple wounds that the target was unaware of receiving.\n\nTessera developed the neural-edge coating as part of a medical research project aimed at producing surgical instruments that reduce patient trauma during conscious procedures. The weaponized version uses a stronger field that does not merely dull sensation but actively suppresses the pain response in a 10-centimeter radius around each wound. Targets who are cut remain alert and functional — they simply do not know they are bleeding until the numbness wears off or they notice the blood.\n\nThe NT-1's tactical implications are disturbing in their elegance. A skilled operator can inflict lethal arterial damage while the target continues a conversation, unaware that they have been cut. The weapon has earned its name from operators who describe the experience of using it as 'whispering to someone in a language their body doesn't speak.' Tessera's medical research division has publicly disavowed any connection to the weapon. Internal documents suggest otherwise.",
    manufacturer: "TESSERA CORPONATION",
    tier_availability: "Tier 4+ (restricted availability)",
    legality: "Restricted — neural-interfering weapon classification",
    base_technologies: ["Piezoelectric polymer edge coating", "Peripheral nerve signal disruption", "Calibrated localized anesthesia field"],
    specifications: "blade_length: 17 cm\ntotal_length: 28 cm\nweight: 180 g\nblade_material: Carbon-lattice core with piezoelectric polymer coating\nneural_effect_radius: 10 cm from wound site\nnumbness_duration: 15-30 seconds before sensation returns\npower_source: Self-generating — piezoelectric charge from cutting motion",
    tactical_use: "The Whisper Cut enables silent, covert elimination in situations where even a suppressed firearm would be detected. The neural-suppression coating allows the operator to inflict multiple wounds before the target realizes they are under attack — in a crowd, against a distracted target, or during apparent physical contact (a handshake, a pat on the back). The weapon is used for kills that must look like delayed-onset injuries discovered after the attacker has departed. Medical examiners familiar with the weapon know to look for the characteristic clean edges and absence of defensive wounds.",
    cultural_context: "The Whisper Cut has generated genuine fear in GLMZ's upper tiers — it represents the possibility that lethal violence could be delivered without the victim or anyone around them noticing until it is too late. The weapon has driven changes in social behavior among high-value targets, who now avoid casual physical contact and wear sensor-equipped clothing that alerts them to skin breaches. The phrase 'whisper cut' has entered the lexicon as a description of any harm inflicted so subtly that the victim discovers it only after the fact.",
    known_users: ["Tessera covert operations (officially denied)", "High-end assassination specialists", "Medical professionals with access to neural-edge research"],
    story_hooks: [
      "A diplomat was whisper-cut during a reception — three arterial nicks that were not discovered until they collapsed in their vehicle thirty minutes later. The guest list is long and the security footage shows nothing unusual.",
      "A street surgeon has obtained neural-edge coating material and is applying it to improvised blades for sale on the black market. The coating is slightly miscalibrated, causing permanent nerve damage rather than temporary numbness."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "dagger", "tanto", "neural", "stealth", "tier 4", "assassination", "piezoelectric"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Ablonite Push Dagger 'Bone Key'",
    type: "weapon",
    aliases: ["Bone Key", "Ablonite Punch", "The Opener", "Ceramic Fist"],
    category: "melee",
    description: "A T-handled push dagger with a short, thick blade of Ablonite-KR reactive ceramic designed for punching through body armor at contact range. The Bone Key is gripped in the fist with the blade protruding between the index and middle fingers, and its design philosophy is brutally simple: get close enough to touch your target, then drive the blade straight through whatever they are wearing.\n\nThe Ablonite-KR blade is only 8 centimeters long — far shorter than conventional fighting knives — but its ceramic hardness exceeds the rating of any personal armor currently manufactured. The push-dagger configuration channels the wielder's entire punching force directly behind the blade tip, concentrating impact energy into a contact area smaller than a fingertip. Against standard ballistic vests, the Bone Key punches clean through. Against hardened plate, it cracks the material and penetrates on the second strike.\n\nThe Forge-Smith Collective produces Bone Keys as a direct response to the increasing prevalence of body armor in GLMZ's lower tiers. As ballistic protection has trickled down from corporate security to street-level combatants, the old calculus of knife fighting has changed — a conventional blade bouncing off a Φ200 armor vest is a death sentence for the knife wielder. The Bone Key restores the equation by making armor irrelevant at grappling range.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-2 (artisan availability)",
    legality: "Unlicensed — classified as armor-defeating weapon where detected",
    base_technologies: ["Ablonite-KR reactive ceramic blade", "Push-dagger force concentration", "Anti-armor contact penetration"],
    specifications: "blade_length: 8 cm\ntotal_length: 14 cm (grip to tip)\nweight: 95 g\nblade_material: Ablonite-KR reactive ceramic\npenetration_rating: Exceeds Level IV ballistic armor\ngrip_type: T-handle push configuration\nconcealment: Palm-sized — easily hidden in a closed fist",
    tactical_use: "The Bone Key is a grappling-range weapon for defeating armored opponents. Users close to contact distance — through crowd movement, social approach, or ambush — and deliver punching strikes directly to center mass. The ceramic blade penetrates armor that would stop conventional knives and most handgun rounds. The weapon's short range and single-purpose design mean it is carried as a backup or assassination tool rather than a primary fighting weapon. Experienced users target the gaps between armor plates — joints, underarms, throat — for maximum effect.",
    cultural_context: "The Bone Key represents the lower tiers' answer to the armor proliferation problem: if armor makes knives obsolete, make a knife that ignores armor. The weapon has become culturally significant as a symbol of refusal to accept disadvantage — a ceramic fist that punches through the protection money can buy. In lower-tier slang, 'having a key' means having an answer to a seemingly impossible problem. The Forge-Smith Collective sells Bone Keys at cost to community defense groups, viewing them as equalizers rather than profit centers.",
    known_users: ["Lower-tier community defense groups", "Close-quarters assassination specialists", "Street fighters facing armored opponents"],
    story_hooks: [
      "A Bone Key was found driven through a Tier 4 executive's ballistic vest — the blade was still embedded in the ceramic armor plate, fused by Ablonite-KR's reactive properties. The executive survived, but the attacker got close enough to land the blow despite a four-person security detail.",
      "The Forge-Smith Collective has received a bulk order for 500 Bone Keys from an anonymous buyer. The Collective is debating whether to fill the order — that many armor-defeating weapons in one buyer's hands represents a military capability, not self-defense."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "dagger", "push-dagger", "Ablonite-KR", "ceramic", "armor-defeating", "tier 1", "artisan"]
  },
  {
    id: id(),
    name: "Crucible Industries Molecular Wire Knife MW-3 'Gossamer'",
    type: "weapon",
    aliases: ["Gossamer", "MW-3", "Wire Knife", "The Thread"],
    category: "melee",
    description: "A handle-only weapon that deploys a 15-centimeter blade of monofilament wire held rigid by an electromagnetic tension field. The Gossamer's 'blade' is a single strand of carbon monofilament 3 nanometers in diameter — effectively invisible to the naked eye and sharp enough to cut at the molecular level. The electromagnetic field generator in the handle keeps the wire rigid and provides a faint blue luminescence along the wire's length — the only visual indication that the weapon is active.\n\nThe MW-3 cuts through conventional materials with zero resistance. Steel, composite, ceramic, flesh — the monofilament passes through everything at the molecular level, leaving wounds so clean that they do not bleed immediately. Victims of a Gossamer cut often do not realize they have been injured until they move and the separated tissue shifts apart. The weapon's limitation is its fragility — the monofilament wire breaks if it contacts material harder than its own carbon structure (diamond, certain advanced ceramics), and replacement wires cost Φ2,000 each.\n\nCrucible developed the MW-3 for precision industrial cutting applications before the design was adapted for covert operations. The weapon's invisible blade and clean-cut capability make it the ultimate close-range assassination tool — a Gossamer cut across a throat produces no spray, no sound, and the target may walk several steps before collapsing. The psychological impact on witnesses who see a person simply come apart along invisible lines is considerable.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Restricted — monofilament weapon classification",
    base_technologies: ["Carbon monofilament wire edge", "Electromagnetic tension rigidity field", "Molecular-level cutting"],
    specifications: "blade_length: 15 cm (monofilament wire)\ntotal_length: 25 cm with handle\nweight: 110 g\nwire_diameter: 3 nanometers\nvisibility: Near-invisible; faint blue EM field glow\npower_source: Handle capacitor, 8 hours field generation\nwire_cost: Φ2,000 per replacement\ncutting_resistance: Zero against conventional materials",
    tactical_use: "The Gossamer is used when absolute stealth is required. Its invisible blade and resistance-free cutting allow the operator to inflict lethal wounds without the physical feedback that alerts nearby observers — no spray, no impact sound, no visible blade. Operators learn to make single passes across critical anatomy and withdraw before the wound manifests. The weapon is useless in prolonged melee — the monofilament wire breaks on hard contact, and the handle provides no defensive capability. It is strictly a first-strike, single-engagement weapon.",
    cultural_context: "Monofilament weapons represent a particular horror in GLMZ's cultural imagination — the idea of being cut by something you cannot see, feeling nothing until you fall apart. The Gossamer has spawned urban legends about phantom attackers who brush past people in crowds, leaving them to discover surgical wounds minutes later. While most of these stories are fiction, the confirmed incidents that do exist have created a low-level paranoia about physical contact in high-security environments.",
    known_users: ["Corporate covert operations", "Precision assassination specialists", "Tessera intelligence (alleged)"],
    story_hooks: [
      "A Gossamer wire has been strung across a corridor at neck height — not wielded, but placed as a trap. The first person to walk through it at pace will be decapitated. Someone is using assassination tools as area denial.",
      "A black-market supplier claims to have developed an unbreakable monofilament wire using a classified substrate. If true, it would eliminate the Gossamer's primary limitation — and create a weapon with no effective counter."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knife", "monofilament", "stealth", "tier 4", "assassination", "molecular"]
  },
  {
    id: id(),
    name: "Street-Custom Rebar Shiv 'Tetanus Kiss'",
    type: "weapon",
    aliases: ["Tetanus Kiss", "Rebar Pig-Sticker", "Rust Fang", "The Infection"],
    category: "melee",
    description: "A crude but effective stabbing weapon made from construction rebar ground to a point and wrapped in salvaged grip tape. The Tetanus Kiss is not a designed weapon — it is what happens when someone who cannot afford a Φ50 knife takes a piece of structural steel and makes it dangerous. The rebar's corrugated surface tears rather than cuts, creating ragged wounds that are extremely difficult to close and prone to infection.\n\nThe weapon's street name reflects its most feared characteristic: lower-tier medical facilities treat rebar puncture wounds with a specific dread, because the corrugated steel surface drags environmental contaminants — rust, concrete dust, biological matter — deep into the wound channel. Without immediate antimicrobial treatment, infection rates approach 80%. In GLMZ's lower tiers, where medical access is intermittent and expensive, a rebar puncture can kill through sepsis days after the initial injury.\n\nThe Tetanus Kiss exists because poverty is a weapon that creates weapons. Every piece of rebar in GLMZ's lower tiers is a potential armament, requiring only motivation and a grinding wheel to transform. Security forces who patrol lower-tier corridors have learned that confiscating improvised weapons is futile — the raw materials are literally the walls and floors.",
    manufacturer: "SELF-MADE",
    tier_availability: "Tier 1",
    legality: "Illegal — improvised weapon, no registration possible",
    base_technologies: ["Ground construction steel", "Corrugated wound channel", "Environmental contaminant delivery"],
    specifications: "blade_length: 15-30 cm (varies)\nweight: 200-600 g (varies)\nmaterial: Construction rebar, various grades\nedge_type: Ground point, corrugated shaft\ninfection_risk: Extremely high without immediate treatment\ncost: Effectively free — salvaged materials",
    tactical_use: "The Tetanus Kiss is a desperation weapon used by individuals who have no other options. Its corrugated surface and infection potential make it more dangerous than its crude appearance suggests — even a non-fatal puncture can become lethal without treatment that the attacker knows the victim likely cannot afford. Some lower-tier fighters deliberately do not clean their rebar weapons, relying on the infection threat as a force multiplier. The weapon has no defensive utility and minimal reach, making it useful only in ambush, surprise, or extreme close quarters.",
    cultural_context: "The rebar shiv is GLMZ's most honest weapon — it is poverty made sharp. Its existence in every lower-tier district is a constant reminder that the city's economic stratification creates violence at the foundation. Social workers, medical professionals, and the rare corporate executive who visits the lower tiers all recognize the Tetanus Kiss as a symptom rather than a problem. Proposals to restrict access to construction materials have been dismissed as absurd, though they resurface periodically in tone-deaf policy discussions.",
    known_users: ["Lower-tier residents with no alternatives", "Desperate individuals", "Improvised militia members"],
    story_hooks: [
      "A rebar shiv has been recovered that was deliberately contaminated with a weaponized bacterial agent — someone is using the Tetanus Kiss tradition as a delivery mechanism for biological weapons.",
      "A lower-tier community has organized a program to exchange rebar weapons for carbon-lattice knives — trading up from desperate to functional. The program is working, which has attracted attention from both supporters and people who do not want the lower tiers armed with anything effective."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knife", "improvised", "street-custom", "tier 1", "infection", "poverty"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Substrate Bowie AK-9 'Copperhead'",
    type: "weapon",
    aliases: ["Copperhead", "AK-9", "Substrate Bowie", "Big Ugly"],
    category: "melee",
    description: "A large clip-point bowie knife with a blade forged from a classified Arcturus substrate composite that combines carbon-lattice structural material with embedded aerogel pockets. The result is a blade that is simultaneously extremely hard and shock-absorbent — it holds a razor edge through impacts that would chip conventional carbon blades while absorbing vibration that would numb the wielder's hand. The Copperhead is Arcturus's answer to the question of what a fighting knife looks like when cost is not a constraint.\n\nThe AK-9's 25-centimeter blade is thick enough to be used as a prying tool, a breaching lever, and an improvised screwdriver without damaging the edge geometry. The clip point allows precise thrusting while the broad belly provides aggressive cutting capability — a versatile profile that has made the knife popular among field operators who need a single blade tool for every situation from camp craft to combat.\n\nArcturus produces the Copperhead in limited runs of 200 units per year, each individually serial-numbered and accompanied by a metallurgical certificate confirming the blade's substrate composition. The knife has developed a collector's market where well-used examples with documented field history command prices triple the original Φ4,500 retail. Arcturus has resisted pressure to increase production, maintaining that the substrate's manufacturing complexity prevents scaling.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Licensed — registered melee weapon",
    base_technologies: ["Carbon-lattice/aerogel composite substrate", "Shock-absorbent blade structure", "Multi-purpose clip-point geometry"],
    specifications: "blade_length: 25 cm\ntotal_length: 38 cm\nweight: 340 g\nblade_material: Classified Arcturus substrate composite\nedge_hardness: 76 HRC equivalent with shock absorption\nproduction: 200 units per year\nretail_price: Φ4,500",
    tactical_use: "The Copperhead serves as an all-purpose field knife for operators who demand a single blade that handles every situation. Its shock-absorbent substrate allows it to be driven through barriers, used as a breaching tool, and employed in hand-to-hand combat without the brittleness that limits other high-hardness blades. The clip-point geometry favors both slashing and thrusting techniques. Field operators report that the AK-9 is the only knife they carry — it replaces the multiple specialized blades that other operatives pack.",
    cultural_context: "The Copperhead has become a status symbol among GLMZ's professional operator class — carrying one signals both the financial means to acquire it and the field experience to justify the purchase. The knife's limited production and individual serialization have created a culture of provenance tracking, where each AK-9's history is documented and discussed. Operators who carry well-worn Copperheads with documented combat use are afforded respect that newer knives, regardless of cost, do not command.",
    known_users: ["Arcturus field operatives", "Professional contractors", "Weapons collectors"],
    story_hooks: [
      "A Copperhead serial number traces to a unit that was reported destroyed in a field incident three years ago — but the blade has fresh edge wear consistent with recent use. The operator who supposedly lost it is either lying or someone recovered the knife from a very dangerous place.",
      "Arcturus's 'classified substrate' has been independently analyzed — the composition matches a material that was supposed to be exclusive to a military spacecraft program. Someone is diverting aerospace materials to knife production."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knife", "bowie", "substrate", "carbon-lattice", "aerogel", "tier 3", "limited-production"]
  }
];

// ─── CLUBS, BATONS, SHOCK-STICKS, STUN WEAPONS (5) ────────────────────

const clubs = [
  {
    id: id(),
    name: "Arcturus Defense Solutions Compliance Baton CB-8 'Peacekeeper'",
    type: "weapon",
    aliases: ["Peacekeeper", "CB-8", "Compliance Stick", "The Negotiator"],
    category: "melee",
    description: "A collapsible baton that delivers calibrated electrical discharges on impact, with five selectable intensity levels ranging from 'compliance' (painful but non-injurious) to 'incapacitation' (immediate motor-function disruption lasting 30-90 seconds). The CB-8 is the standard-issue less-lethal weapon for GLMZ's corporate security forces, and its ubiquity has made its distinctive crackling discharge one of the city's most recognized sounds.\n\nThe Peacekeeper extends from a 22-centimeter collapsed length to 55 centimeters with a flick of the wrist, and its telescoping sections lock with enough rigidity to be used as a conventional impact weapon when the electrical system is depleted or disabled. The discharge capacitor recharges from a compact cell in the handle, providing approximately 200 discharges per charge cycle.\n\nArcturus's 'compliance' marketing language has been widely criticized by civil rights groups who note that the highest intensity setting has caused cardiac arrest in individuals with undiagnosed heart conditions and has been documented to cause permanent nerve damage in augmented individuals whose cyberware channels the discharge in unexpected ways. Arcturus's response has been to add a disclaimer to the user manual rather than limit the weapon's output.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 2+",
    legality: "Licensed — standard security equipment",
    base_technologies: ["Calibrated electrical discharge", "Collapsible telescoping construction", "Five-level intensity control"],
    specifications: "length_collapsed: 22 cm\nlength_extended: 55 cm\nweight: 380 g\ndischarge_levels: 5 (compliance through incapacitation)\ndischarges_per_charge: ~200\npower_source: Handle-integrated charge cell\nrecharge_time: 2 hours from depleted",
    tactical_use: "The CB-8 is the workhorse of corporate security's less-lethal arsenal. At lower settings, it enforces compliance through pain without causing injuries that generate legal liability. At higher settings, it incapacitates targets for extraction or restraint. Security operators are trained to assess augmentation level before selecting discharge intensity — heavily augmented targets may require higher settings due to electrical shunting through cybernetic pathways, while unaugmented targets may be more vulnerable than expected. The collapsible design allows the weapon to be carried holstered and deployed in under a second.",
    cultural_context: "The Peacekeeper is simultaneously the most hated and most common weapon in GLMZ. Lower-tier residents who interact with corporate security encounter it frequently, and the crackling sound of a CB-8 charging is associated with authority, coercion, and the implicit threat of escalation. The weapon's 'compliance' branding has become ironic shorthand — 'seeking compliance' means using force, and 'a compliance conversation' means an interrogation. The gap between the weapon's marketing language and its lived reality is a recurring subject of political protest.",
    known_users: ["Corporate security forces (standard issue)", "Municipal security", "Private security contractors"],
    story_hooks: [
      "A modified CB-8 has been recovered with its intensity limiter removed — the weapon now delivers discharge levels well above the rated maximum, turning a compliance tool into a lethal weapon. The modification is simple enough that instructions have been circulating on underground networks.",
      "A class-action lawsuit against Arcturus alleges that the CB-8's highest setting was deliberately calibrated to cause permanent nerve damage in augmented individuals — turning 'less-lethal' into 'selectively lethal' against the aug-dependent population."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "baton", "electrical", "less-lethal", "tier 2", "compliance", "standard-issue"]
  },
  {
    id: id(),
    name: "Crucible Industries Shock-Staff SS-2 'Thunderstick'",
    type: "weapon",
    aliases: ["Thunderstick", "SS-2", "Shock Staff", "Lightning Rod"],
    category: "melee",
    description: "A full-length combat staff with electrical discharge nodes at both ends, allowing the wielder to deliver stunning strikes from either end without repositioning. The SS-2 is 150 centimeters of carbon-lattice composite housing a dual-capacitor system that provides independent power to each end — when one capacitor is cycling, the other is charged and ready, enabling continuous electrical engagement without pause.\n\nCrucible designed the Thunderstick for riot control operations where security forces must maintain distance from hostile crowds while delivering incapacitating force. The staff's length keeps the operator outside the effective range of improvised weapons and hand-to-hand attacks, while the dual-end electrical system means there is no 'safe' end for an attacker to grab. The discharge is powerful enough to penetrate standard clothing and light armor, causing full-body muscle contraction that drops targets instantly.\n\nThe SS-2 has been adopted beyond its riot-control origins by martial artists who practice staff fighting and have found that the weapon's dual electrical capability adds a devastating dimension to traditional techniques. A sweep that would normally knock an opponent off their feet now delivers an electrical discharge that ensures they do not get back up. The weapon has revived interest in staff-fighting traditions that had been largely abandoned in favor of shorter, more concealable weapons.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — security/riot control equipment",
    base_technologies: ["Dual-end electrical discharge", "Independent capacitor cycling", "Carbon-lattice composite staff"],
    specifications: "total_length: 150 cm\nweight: 1.2 kg\ndischarge_nodes: 2 (both ends)\ncapacitor_system: Dual independent, alternating charge\ndischarge_power: 50,000 volts per node\npower_source: Dual handle-section capacitors, 150 discharges each\nmaterial: Carbon-lattice composite shaft",
    tactical_use: "The Thunderstick excels in crowd-control and perimeter-defense scenarios where the operator must engage multiple targets while maintaining distance. The dual-end system eliminates the need to reorient the weapon between strikes — every strike from either end is electrified. In single combat, the staff's length advantage and electrical capability make it dominant against shorter weapons. The primary limitation is the weapon's size, which makes it impractical for concealed carry and awkward in confined spaces.",
    cultural_context: "The SS-2 has created an unexpected martial arts renaissance in GLMZ. Traditional staff-fighting schools that were struggling for students have seen enrollment surge as the Thunderstick has demonstrated the practical relevance of two-handed staff techniques. Competitions have emerged that blend classical forms with electrical-staff combat, creating a new martial discipline. The weapon has also become a symbol of protest — several anti-corporate groups have adopted staff-fighting as a communal practice, arguing that the weapon's defensive capabilities empower communities against armed security forces.",
    known_users: ["Corporate riot control teams", "Staff-fighting martial artists", "Community defense groups"],
    story_hooks: [
      "A staff-fighting school has been secretly training a militia equipped with SS-2 Thundersticks — forty practitioners with synchronized combat techniques. Someone is preparing a coordinated action that community defense cannot explain.",
      "A modified Thunderstick has been recovered with its discharge power amplified tenfold — a single strike killed the target through cardiac arrest. The modification was done by someone with intimate knowledge of the weapon's electrical architecture."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "staff", "electrical", "riot-control", "tier 3", "dual-end", "martial-arts"]
  },
  {
    id: id(),
    name: "Street-Custom Loaded Pipe 'Bell Ringer'",
    type: "weapon",
    aliases: ["Bell Ringer", "Lead Pipe", "The Toll", "Pipe Dream"],
    category: "melee",
    description: "A length of industrial pipe — typically 40-60 centimeters — filled with lead shot or concrete and capped at both ends. The Bell Ringer is the lower tiers' most common impact weapon: free to manufacture, devastating on impact, and requiring no skill beyond the ability to swing. The loaded pipe's weight (typically 2-4 kilograms depending on fill material and length) generates enormous kinetic energy in even casual swings, and the cylindrical contact surface distributes force across a wide area that defeats the shock-absorbing properties of most light armor.\n\nThe weapon's ubiquity in GLMZ's lower tiers has given it a cultural significance that transcends its crude construction. A loaded pipe is not a weapon of choice — it is a weapon of circumstance, carried by people who cannot afford anything better and who have learned that industrial plumbing components are both free and lethal. The distinctive sound of a pipe connecting with a hard surface — a hollow, resonant ring that has earned the weapon its name — is one of the lower tiers' characteristic sounds of violence.\n\nSome lower-tier artisans have elevated the Bell Ringer into something approaching craft, wrapping handles in salvaged leather, adding wrist loops for retention, and selecting specific pipe gauges for optimal weight-to-length ratios. These improved versions are sold or traded within community networks, blurring the line between improvised weapon and purpose-built tool of violence.",
    manufacturer: "SELF-MADE",
    tier_availability: "Tier 1",
    legality: "Illegal — improvised weapon",
    base_technologies: ["Weighted impact construction", "Scavenged industrial materials", "Mass-dependent kinetic delivery"],
    specifications: "length: 40-60 cm (varies)\nweight: 2-4 kg (loaded)\nmaterial: Industrial steel pipe, lead shot or concrete fill\nimpact_surface: Cylindrical, 3-5 cm diameter\ncost: Free — salvaged materials\nskill_requirement: Minimal",
    tactical_use: "The Bell Ringer is effective through sheer kinetic mass. A full-force swing to the head causes traumatic brain injury regardless of protective headgear short of a military helmet. Body strikes cause broken ribs and internal bruising through light armor. The weapon's weight makes it slow and telegraphed — experienced fighters can dodge or intercept it — but in the chaotic, close-quarters violence of lower-tier confrontations, speed matters less than raw stopping power. Users who land the first swing typically do not need a second.",
    cultural_context: "The Bell Ringer is poverty's signature weapon, and its sound is poverty's signature music. In lower-tier districts, the resonant ring of pipe-on-concrete is a territorial marker, a warning sound, and sometimes a call to arms. Community defense groups train with loaded pipes because they cannot afford anything else, and the resulting fighting style — heavy, committed, built around single devastating swings — has been formalized into a crude but effective martial practice called 'pipe cadence.' The sound has been sampled into lower-tier music so frequently that it has become a genre signifier.",
    known_users: ["Lower-tier residents", "Community defense groups", "Anyone who has access to a pipe and a grievance"],
    story_hooks: [
      "A Bell Ringer was used to kill a mid-tier corporate official who ventured into the lower tiers without security. The weapon was left at the scene with a message etched into the pipe. It was not a robbery — it was a political statement.",
      "A lower-tier community is holding pipe-cadence training sessions that have grown from self-defense to something more organized. Attendance is in the hundreds, and the sessions are starting to look like military drill."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "club", "improvised", "street-custom", "tier 1", "impact", "poverty"]
  },
  {
    id: id(),
    name: "Tessera Corponation Neural Disruptor Baton NDB-1 'Migraine'",
    type: "weapon",
    aliases: ["Migraine", "NDB-1", "Neural Baton", "Brainfog"],
    category: "melee",
    description: "A rigid baton that generates a localized electromagnetic pulse on impact, specifically tuned to interfere with neural implant frequencies and biological synaptic transmission. The Migraine does not stun through electrical discharge like conventional shock-sticks — it disrupts the target's cognitive function by flooding their nervous system with electromagnetic noise that overwhelms both biological and cybernetic neural pathways.\n\nTargets struck by the NDB-1 experience immediate disorientation, loss of spatial awareness, and disruption of short-term memory formation. The effect has been described as being 'hit by a migraine and a flashbang simultaneously.' The disruption lasts 2-5 minutes depending on the target's neural augmentation level — ironically, more heavily augmented individuals suffer longer-lasting effects because their cybernetic neural pathways amplify the disruptive pulse.\n\nTessera developed the Neural Disruptor Baton for their internal security division's 'cognitive compliance' program — a euphemism for interrogation-adjacent activities where subjects needed to be rendered confused and suggestible without visible injury. The weapon leaves no marks, causes no physical damage, and produces symptoms that are indistinguishable from a severe migraine episode. Medical examinations after NDB-1 exposure reveal nothing abnormal, making allegations of its use extremely difficult to prove.",
    manufacturer: "TESSERA CORPONATION",
    tier_availability: "Tier 4+ (restricted distribution)",
    legality: "Restricted — cognitive interference weapon classification",
    base_technologies: ["Targeted neural electromagnetic pulse", "Synaptic disruption frequency tuning", "Cognitive function interference"],
    specifications: "total_length: 45 cm\nweight: 520 g\ndisruption_type: Neural electromagnetic pulse\neffect_duration: 2-5 minutes (varies with augmentation level)\ndischarges_per_charge: 80\npower_source: Internal capacitor bank\nphysical_damage: None — cognitive disruption only\nevidence: No detectable traces on medical examination",
    tactical_use: "The Migraine is used when subjects must be incapacitated without physical evidence of force. Security teams deploying NDB-1 batons can subdue, detain, and question individuals who subsequently cannot prove they were struck — medical scans show nothing. The weapon's enhanced effect on augmented individuals makes it particularly effective against cybernetically enhanced targets who might resist conventional restraint. Operators are trained to target the head and upper spine for maximum cognitive disruption, though even limb strikes produce disorientation through neural-pathway propagation.",
    cultural_context: "The Migraine represents a category of weapon that GLMZ's civil rights advocates find especially disturbing — a tool of violence that leaves no evidence. Documented allegations of NDB-1 use in corporate detention facilities have been dismissed for lack of physical proof, creating a circular problem that the weapon's design was specifically engineered to produce. The phrase 'Tessera headache' has entered corporate slang as a warning about crossing the corporation's internal security, and the implication is understood by everyone who hears it.",
    known_users: ["Tessera internal security division", "Corporate interrogation specialists", "High-tier detention facility operators"],
    story_hooks: [
      "A detainee who was struck with an NDB-1 has developed unexpected side effects — their neural implants have begun functioning differently, processing information in ways the manufacturer says should be impossible. The Migraine did not just disrupt their neural pathways; it rewired them.",
      "An NDB-1 has been stolen from a Tessera facility and is being used in street-level robberies — victims are left confused and unable to identify their attackers or even confirm they were attacked. Law enforcement has no forensic evidence to work with."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "baton", "neural", "electromagnetic", "cognitive", "tier 4", "no-evidence", "interrogation"]
  },
  {
    id: id(),
    name: "Crucible Industries Arc Mace AM-2 'Thunderhead'",
    type: "weapon",
    aliases: ["Thunderhead", "AM-2", "Arc Mace", "Ball Lightning"],
    category: "melee",
    description: "A flanged mace with an electrically charged head that generates visible electrical arcs between its flanges during activation. The AM-2 combines medieval impact weapon geometry with modern electrical discharge technology — the flanged head concentrates kinetic force into narrow contact areas that defeat armor through focused pressure, while the electrical arcs discharge through the point of contact, delivering stunning force directly into the wound channel.\n\nThe weapon's flanges are constructed from conductive carbon-lattice composite with insulating gaps between them, creating a multi-pole electrical system that generates arcing between flanges when the head is charged. The visual effect is dramatic — a halo of crackling blue-white arcs surrounding the weapon's head that illuminates surrounding space and produces an aggressive electrical buzzing. The combined kinetic and electrical impact has been measured as sufficient to defeat Level III body armor and incapacitate the wearer through the armor itself.\n\nCrucible developed the Arc Mace for close-quarters engagements against heavily armored opponents where cutting weapons struggle and conventional impact weapons lack stopping power. The AM-2 does not need to penetrate armor — it defeats it through concentrated force and electrical discharge transmitted through conductive armor materials. Against the increasingly common composite body armor in GLMZ, the weapon's dual-mode attack presents a threat that cannot be defended against by any single armor type.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — electrical/impact weapon dual classification",
    base_technologies: ["Multi-pole flanged electrical discharge", "Focused-pressure impact geometry", "Conductive carbon-lattice flanges"],
    specifications: "head_diameter: 10 cm across flanges\nhandle_length: 45 cm\ntotal_weight: 1.8 kg\nflanges: 6, conductive carbon-lattice\narc_voltage: 30,000 volts between flanges\npower_source: Handle capacitor, 100 discharges\nimpact_rating: Defeats Level III body armor",
    tactical_use: "The Arc Mace is deployed against armored opponents in close quarters. The flanged head concentrates impact force into narrow lines that dent and deform armor panels, while the electrical discharge transmits through the deformation to the wearer's body. Against composite armor, the combination of mechanical deformation and electrical penetration overwhelms protection that would resist either threat individually. The weapon's dramatic electrical display also serves as an intimidation tool — the visible arcs and aggressive buzzing discourage opponents from closing to engagement range.",
    cultural_context: "The Arc Mace has revived interest in historical impact weapons that had been largely forgotten in GLMZ's blade-centric combat culture. The weapon's effectiveness against modern armor has demonstrated that medieval design principles — concentrate force, defeat protection through pressure rather than penetration — remain tactically valid when combined with modern materials. A small but dedicated subculture of mace-fighters has emerged, styling themselves as 'new crusaders' with varying degrees of historical awareness and irony.",
    known_users: ["Anti-armor close-combat specialists", "Corporate heavy security", "Mace-fighting enthusiasts"],
    story_hooks: [
      "An Arc Mace was used to breach a powered armor suit from the outside — the electrical discharge overloaded the suit's systems while the flanged impact compromised the structural integrity. The operator inside survived but their cyberware was permanently damaged by the electrical surge.",
      "A fight club has emerged in the mid-tiers where participants use Arc Maces in full armor — the combination of impact and electrical discharge creates a spectacle that has attracted betting interest from all tier levels."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "mace", "electrical", "impact", "anti-armor", "tier 3", "flanged"]
  }
];

// ─── KNUCKLE WEAPONS, CLAWS, FIST AUGMENTS (5) ────────────────────────

const knuckleWeapons = [
  {
    id: id(),
    name: "Crucible Industries Kinetic Knuckles KK-3 'Jackhammer'",
    type: "weapon",
    aliases: ["Jackhammer", "KK-3", "Kinetic Dusters", "Pile Driver"],
    category: "melee",
    description: "A set of articulated knuckle plates that incorporate a miniaturized linear accelerator in each striking surface. When the wearer throws a punch, impact sensors trigger a magnetic pulse that drives a 50-gram tungsten slug forward within each knuckle plate at the moment of contact, multiplying the strike's kinetic energy by approximately five times. The effect turns a human punch into a force comparable to a sledgehammer blow.\n\nThe KK-3's accelerator system is powered by a capacitor bank worn as a wrist cuff, connected to the knuckle plates by flexible conductive threading woven into the glove. The system recharges between punches in approximately 0.4 seconds — fast enough to augment rapid combinations but slow enough that the first hit of a flurry is always the strongest. Each knuckle plate fires independently, meaning a four-knuckle impact delivers four separate accelerated strikes in rapid succession.\n\nCrucible developed the Jackhammer for operators who fight in environments where weapons cannot be drawn — crowd situations, security checkpoints, and the awkward grappling distances where firearms and blades are equally impractical. The weapon is concealable under standard gloves, activates on impact without any visible preparation, and turns bare-knuckle fighting into an armor-defeating capability.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — concealed augmented weapon permit",
    base_technologies: ["Miniaturized linear magnetic acceleration", "Impact-triggered kinetic multiplication", "Articulated knuckle plate design"],
    specifications: "weight: 280 g (both hands)\nslug_mass: 50 g per knuckle plate\nforce_multiplication: ~5x base punch force\nrecharge_time: 0.4 seconds per strike\npower_source: Wrist-cuff capacitor bank, 500 strikes per charge\nconcealment: Under standard gloves\ntrigger: Impact-activated sensor",
    tactical_use: "The Jackhammer is deployed in situations where drawing a weapon is impossible, impractical, or would escalate a situation beyond desired levels. An operator wearing KK-3s can engage in what appears to be a bare-knuckle fight while delivering strikes with five times the expected force — an advantage that ends most encounters before the opponent understands what is happening. Against armored targets, the concentrated force of the accelerated tungsten slugs defeats soft armor and damages rigid plates through transmitted shock.",
    cultural_context: "The KK-3 has blurred the line between armed and unarmed combat in GLMZ. In a city where security screening is ubiquitous, a weapon that hides under gloves and activates on contact has made every handshake, every pat on the back, every casual touch a potential threat vector. Corporate security details have responded by adding concealed-weapon scanning to their screening protocols, but the KK-3's minimal metallic signature makes detection unreliable. The phrase 'loaded hands' has entered slang to describe anyone suspected of carrying concealed impact augmentation.",
    known_users: ["Corporate undercover operatives", "Close-quarters combat specialists", "Executive protection details"],
    story_hooks: [
      "A street fighter has been winning bare-knuckle matches with suspiciously devastating power — but no one can prove augmentation because the KK-3's signature is below detection thresholds. The betting pool around the fighter has attracted attention from people who want answers.",
      "A prototype KK-4 has been stolen from Crucible — the upgraded model incorporates a shaped-charge element that turns each punch into a micro-explosion. The thief is testing it on people."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knuckles", "kinetic", "concealed", "tier 3", "augmented", "impact"]
  },
  {
    id: id(),
    name: "Tessera Corponation Retractable Claws RC-2 'Cat Scratch'",
    type: "weapon",
    aliases: ["Cat Scratch", "RC-2", "Tessera Claws", "Kitty"],
    category: "melee",
    description: "A set of three retractable claws per hand, surgically mounted to the metacarpal bones and deployed through armored sheaths in the knuckle skin. Each claw is 12 centimeters of monocrystalline carbon-lattice composite — harder than surgical steel, lighter than aluminum, and sharp enough to score glass. The claws deploy in 0.15 seconds via a BCI-linked command or a deliberate fist-clenching gesture, and retract with the same speed.\n\nTessera developed the RC-2 as a personal defense augmentation for high-value individuals who require constant protection but cannot carry visible weapons. The claws are completely concealed when retracted — even detailed hand examination reveals only faint scars at the knuckle deployment points. Deployment is silent and instant, transforming an apparently unarmed hand into a weapon capable of shredding soft armor and inflicting deep lacerating wounds.\n\nThe surgical implantation procedure is invasive and painful, requiring modification of the hand's skeletal structure to accommodate the claw housings. Recipients report a persistent awareness of the hardware inside their hands — a subtle weight and tension that never fully fades. Long-term users describe developing an instinctive relationship with the claws, deploying them reflexively in response to perceived threats before conscious decision-making occurs. Tessera considers this a feature; psychologists are less certain.",
    manufacturer: "TESSERA CORPONATION",
    tier_availability: "Tier 4+ (surgical implant)",
    legality: "Restricted — implanted weapon classification, registration required",
    base_technologies: ["Retractable monocrystalline claw mechanism", "BCI-linked deployment", "Skeletal integration surgical mounting"],
    specifications: "claw_length: 12 cm (deployed)\nclaw_count: 3 per hand, 6 total\ndeployment_time: 0.15 seconds\nmaterial: Monocrystalline carbon-lattice composite\nretraction: BCI command or gesture-triggered\nconcealment: Complete when retracted\npower_source: Body kinetic energy harvesting — indefinite",
    tactical_use: "The Cat Scratch transforms the wielder's hands into close-quarters weapons that cannot be disarmed, confiscated, or detected by conventional screening. The claws are effective against soft armor and unprotected targets, and their deployment speed means the first strike typically lands before the target registers the threat. In grappling situations, the claws provide an overwhelming advantage — every grab becomes a cutting attack. The primary limitation is reach; the claws add only 12 centimeters to the hand's natural range, making them purely close-quarters weapons.",
    cultural_context: "Implanted weapons represent a philosophical line that many GLMZ residents are uncomfortable crossing — the permanent integration of lethal hardware into the human body. The RC-2 in particular has generated debate about where the boundary lies between self-defense augmentation and becoming a weapon. Individuals with implanted claws are required to register with municipal security, and some establishments post signage prohibiting entry by individuals with registered implanted weapons — a form of discrimination that aug-rights groups are actively challenging.",
    known_users: ["Tessera high-value assets", "Registered implanted-weapon carriers", "Personal security specialists with permanent deployment needs"],
    story_hooks: [
      "An RC-2 recipient's claws have begun deploying involuntarily — the BCI link is interpreting normal stress responses as deployment commands. Three people have been injured by accidental deployments in public, and the recipient is terrified of their own hands.",
      "A black-market surgeon is performing RC-2 implantations without Tessera's quality control — the claws work, but the skeletal modifications are causing progressive bone degradation. Recipients are developing crippling hand injuries months after implantation."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "claws", "implant", "retractable", "BCI", "tier 4", "cyberware", "concealed"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Trench Knuckles 'Anvil'",
    type: "weapon",
    aliases: ["Anvil", "Trench Dusters", "Iron Hand", "The Weight"],
    category: "melee",
    description: "Massive brass-knuckle-style weapons machined from solid Ablonite-KR ceramic blocks, covering the entire front of the fist from fingertip to wrist. The Anvil is not a subtle weapon — it transforms the wearer's hand into a ceramic battering ram that weighs 600 grams per hand and hits with a contact surface area specifically designed to defeat riot shields, vehicle windows, and light structural walls through concentrated impact force.\n\nThe Forge-Smith Collective produces Anvils for the specific purpose of enabling unaugmented fighters to compete against opponents with cybernetic strength enhancements. The ceramic construction provides two advantages: the Ablonite-KR's extreme hardness means the knuckle surface will not deform on impact with hard targets (saving the wearer's hand from fracture), and the material's reactive properties generate localized heat on contact with ferrous metals, making each punch against cyberware feel like being struck by a hot iron.\n\nThe weapon is too heavy for rapid combinations — Anvil fighters typically throw single, committed power shots targeted at vulnerable points. The fighting style that has developed around the weapon emphasizes timing, footwork, and the ability to land one devastating blow rather than sustained striking. Anvil practitioners call their discipline 'hammer work' and train obsessively to develop the core strength required to swing 600 grams of ceramic at full extension without losing balance.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-2 (artisan availability)",
    legality: "Unlicensed — artisan production",
    base_technologies: ["Ablonite-KR ceramic impact surface", "Full-fist coverage design", "Reactive ceramic anti-cyberware properties"],
    specifications: "weight: 600 g per hand\ncoverage: Full fist — fingertip to wrist\nmaterial: Solid Ablonite-KR ceramic\nimpact_surface: 40 cm² concentrated\nreactive_property: Heat generation on ferrous contact\npower_requirement: None",
    tactical_use: "The Anvil is used by unaugmented fighters who need to punch above their weight class — literally. The ceramic construction protects the hand from fracture on hard impacts, and the reactive heat on contact with cybernetic components adds a burning element that augmented opponents do not expect. Anvil practitioners target cybernetic joints, sensor clusters, and implant housings, exploiting the Ablonite-KR's thermal reaction to damage expensive augmentations while delivering crushing kinetic force. The weapon's weight limits its use to deliberate, powerful strikes rather than rapid boxing combinations.",
    cultural_context: "The Anvil has become a symbol of unaugmented resistance in GLMZ's lower tiers — proof that a person without cybernetic enhancement can still deliver a punch that matters. Anvil fighting events draw large crowds who view the weapon as an equalizer, cheering for natural fighters who stand against augmented opponents with nothing but ceramic and determination. The Forge-Smith Collective produces Anvils at reduced cost for fighters who demonstrate commitment to the 'hammer work' discipline, viewing the practice as a cultural preservation effort.",
    known_users: ["Unaugmented lower-tier fighters", "Hammer-work practitioners", "Anti-augmentation combat traditionalists"],
    story_hooks: [
      "An Anvil fighter killed an augmented opponent in a semi-legal match — the reactive ceramic caused a catastrophic overheating cascade in the victim's thoracic implants. The death is being investigated as both an accident and a murder, depending on who is asking.",
      "A corporate weapons designer has observed Anvil fighting and wants to mass-produce the weapon with enhanced reactive properties — the Forge-Smith Collective views this as cultural appropriation and is prepared to fight the patent application physically if necessary."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knuckles", "Ablonite-KR", "ceramic", "artisan", "tier 1", "unaugmented", "anti-cyberware"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Pneumatic Gauntlet PG-1 'Pile Driver'",
    type: "weapon",
    aliases: ["Pile Driver", "PG-1", "Pneumatic Fist", "The Piston"],
    category: "melee",
    description: "An armored gauntlet that incorporates a pneumatic ram system in the knuckle assembly, firing a weighted piston forward at the moment of impact to deliver a concentrated force spike that exceeds what any human arm — augmented or otherwise — can generate through muscular power alone. The PG-1's piston fires with approximately 2,000 newtons of additional force, enough to crack reinforced concrete and buckle light vehicle panels.\n\nThe gauntlet covers the hand and forearm, providing both the weapon mechanism and armor protection for the operator's striking limb. The pneumatic system uses compressed gas cartridges stored in the forearm section, each cartridge providing 30 piston fires. Cartridge exchange takes approximately three seconds and can be performed one-handed. The piston automatically resets between strikes, requiring 0.8 seconds to recharge — slower than natural punching speed but fast enough for deliberate, powerful engagement.\n\nArcturus developed the Pneumatic Gauntlet for demolition and rescue operations where powered tools are unavailable or impractical. The weapon's ability to punch through walls, doors, and debris has made it standard equipment for corporate emergency response teams. Its combat applications were realized when response teams encountered armed resistance during rescue operations and discovered that a gauntlet designed to break through concrete is equally effective against body armor.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Licensed — industrial/rescue tool with combat application",
    base_technologies: ["Pneumatic ram impact system", "Compressed gas piston mechanism", "Armored gauntlet integration"],
    specifications: "coverage: Hand and forearm\nweight: 2.1 kg\npiston_force: ~2,000 N additional on impact\ngas_cartridge_capacity: 30 fires per cartridge\ncartridge_exchange: 3 seconds, one-handed\nrecharge_time: 0.8 seconds between fires\nmaterial: Hardened composite gauntlet shell",
    tactical_use: "The Pile Driver is deployed when obstacles must be physically broken through without tools or explosives. In rescue operations, the gauntlet punches through collapsed walls and jammed doors. In combat, the pneumatic piston delivers force that defeats any personal armor through sheer mechanical pressure. Operators learn to use the 0.8-second recharge window to reposition between strikes, developing a rhythmic engagement pattern — fire, shift, fire — that maximizes the weapon's concentrated impact while minimizing exposure to counterattack.",
    cultural_context: "The PG-1 has become associated with GLMZ's emergency response culture — first responders who punch through rubble to reach survivors, breaching teams who open paths through collapsed infrastructure. The image of an armored fist breaking through a wall carries positive connotations of rescue and strength, which complicates the weapon's combat reputation. An operator wearing a Pile Driver might be there to save you or to kill you, and the equipment looks the same either way.",
    known_users: ["Arcturus emergency response teams", "Corporate rescue operators", "Breaching and demolition specialists"],
    story_hooks: [
      "A rescue team used PG-1 gauntlets to breach a collapsed building — but when they reached the survivors, they found evidence that the collapse was deliberately engineered to trap specific individuals. The rescue was actually an extraction.",
      "A modified PG-1 has appeared with the piston force increased to 5,000 newtons — enough to punch through armored vehicle panels. The modification exceeds the gauntlet's structural rating, meaning the weapon may destroy itself (and the wearer's arm) on use."
    ],
    ammunition_type: ["Compressed gas cartridge"],
    tags: ["weapon", "melee", "gauntlet", "pneumatic", "impact", "rescue", "tier 3", "breaching"]
  },
  {
    id: id(),
    name: "Street-Custom Razorwire Wraps 'Glass Hands'",
    type: "weapon",
    aliases: ["Glass Hands", "Wire Wraps", "Razor Mitts", "The Shredder"],
    category: "melee",
    description: "Strips of industrial razorwire wrapped around padded hand guards — a lower-tier fighting weapon that transforms every punch, grab, and palm strike into a cutting attack. Glass Hands are crude, dangerous to the wearer if improperly constructed, and devastatingly effective in the close-quarters grappling that characterizes lower-tier violence. The razorwire is typically salvaged from perimeter security installations and wound around hand-shaped frames made from molded industrial foam.\n\nThe weapon's effectiveness comes from its contact versatility — unlike a blade, which cuts in one direction along one edge, razorwire wraps present cutting surfaces on every surface of the hand. Punches cut. Grabs cut. Blocks cut the attacker. Even a glancing touch draws blood. In grappling situations, an opponent wrestling with someone wearing Glass Hands discovers that every point of contact becomes a wound. The psychological effect of being cut on every surface that touches the fighter is often more decisive than the physical damage.\n\nThe risk to the wearer is genuine. Poorly constructed Glass Hands shift during use, exposing the wearer's own skin to the razorwire. Lower-tier fighters accept this risk as the cost of an effective weapon, and experienced users bear characteristic scars on their hands and forearms from construction, training, and combat. These scars have become markers of Glass Hands experience — a visible résumé of lower-tier fighting capability.",
    manufacturer: "SELF-MADE",
    tier_availability: "Tier 1",
    legality: "Illegal — improvised weapon",
    base_technologies: ["Salvaged industrial razorwire", "Improvised padded hand frame", "Omni-directional cutting contact"],
    specifications: "weight: 200-300 g per hand\nmaterial: Industrial razorwire on foam hand frame\ncutting_surfaces: All external surfaces\nwearer_risk: High — improper construction causes self-injury\ncost: Minimal — salvaged materials\nconstruction_time: 30-60 minutes",
    tactical_use: "Glass Hands are effective in the messy, grappling-heavy fighting that occurs in crowded lower-tier corridors. Every point of contact becomes a weapon — punches, grabs, clinches, and even defensive blocks all inflict cutting damage. The weapon is particularly effective against augmented opponents who rely on grappling and holds, as their standard approach of controlling an opponent through grip strength becomes self-defeating when every grip draws blood. The weapon's limitation is precision — Glass Hands are indiscriminate in what they cut, including allies, bystanders, and the wearer.",
    cultural_context: "Glass Hands represent the lower tiers' willingness to hurt themselves in order to hurt their enemies — a philosophy that extends beyond weapon design into broader cultural attitudes about sacrifice and resistance. The characteristic hand scars of Glass Hands users are worn openly as proof of willingness to fight at personal cost. In some lower-tier communities, these scars carry social weight comparable to formal military service marks. The weapon embodies a specific kind of courage that comfortable tiers do not understand: the courage to pick up something that will cut you in order to cut someone else.",
    known_users: ["Lower-tier grappling fighters", "Community defense practitioners", "Street-level combatants"],
    story_hooks: [
      "A Glass Hands fighter has been found dead with wounds consistent with their own weapon — but the wire pattern does not match their own wraps. Someone made a set of Glass Hands specifically to frame the death as self-inflicted combat injury.",
      "A lower-tier innovator has developed a construction technique using mono-wire instead of razorwire — the resulting Glass Hands cut deep enough to sever tendons and arteries on casual contact. The weapons are being distributed through a community network."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knuckles", "razorwire", "improvised", "street-custom", "tier 1", "grappling"]
  }
];

// ─── WHIPS, CHAINS, MONO-WIRE WEAPONS (4) ─────────────────────────────

const whips = [
  {
    id: id(),
    name: "Crucible Industries Electrostatic Whip EW-3 'Live Wire'",
    type: "weapon",
    aliases: ["Live Wire", "EW-3", "Shock Lash", "The Current"],
    category: "melee",
    description: "A three-meter segmented whip constructed from conductive carbon-lattice links that carries a progressive electrical charge — each segment adds voltage as the whip extends, so the tip delivers a charge proportional to the length of whip that contacts the target. A full-extension strike with the complete three-meter length delivers approximately 80,000 volts at the contact point, sufficient for immediate incapacitation of an unarmored target.\n\nThe EW-3's segmented construction allows the operator to control the effective length by gripping the whip at different points along its length — shorter grips for close-range work, full extension for crowd control and area denial. Each segment is individually insulated from the handle, so the operator receives no feedback charge regardless of contact. The whip's carbon-lattice segments are rigid enough to maintain momentum during a swing but flexible enough to wrap around limbs and barriers, delivering sustained electrical contact.\n\nCrucible originally developed the Live Wire as a perimeter security tool — a ranged electrical weapon that could deny access to corridors and doorways without permanent infrastructure. The weapon's adoption by combat operatives was driven by its unique capability: no other man-portable weapon can control a three-meter radius, wrap around obstacles, and deliver incapacitating electrical force simultaneously. The drawback is the extensive training required — a whip in untrained hands is as dangerous to the user as to the target.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — electrical area-denial weapon",
    base_technologies: ["Progressive voltage segmented construction", "Conductive carbon-lattice links", "Variable-length grip control"],
    specifications: "total_length: 3 meters (full extension)\nweight: 800 g\nsegments: 24 conductive links\nmax_voltage: ~80,000 V at full extension\npower_source: Handle capacitor bank, 60 full-extension strikes\ninsulation: Handle-side complete operator isolation\nmaterial: Conductive carbon-lattice composite links",
    tactical_use: "The Live Wire controls space. An operator with a three-meter electrified whip dominates any corridor or room, denying approach from all angles. The weapon wraps around obstacles, reaches behind cover, and delivers incapacitating charges through incidental contact. Against multiple opponents, the whip's area coverage forces attackers to approach from a single direction rather than surrounding the operator. Against single targets, the whip can be used to entangle limbs and deliver sustained electrical contact that overcomes resistance. The training investment is substantial — whip fighting is an entirely separate martial discipline from blade or impact weapons.",
    cultural_context: "The Live Wire has revived whip-fighting as a martial discipline in GLMZ, attracting practitioners who value the weapon's unique combination of range, flexibility, and electrical capability. Training schools have developed a formalized curriculum that takes two years to reach combat proficiency, creating an exclusivity that whip fighters wear as a badge of dedication. The weapon's dramatic visual display — the arc of electrical sparks trailing a three-meter whip — has made it popular in performance contexts as well as combat, and whip-fighting demonstrations draw crowds at lower-tier market events.",
    known_users: ["Corporate perimeter security", "Whip-fighting practitioners", "Area-denial specialists"],
    story_hooks: [
      "A Live Wire operator has modified their whip's segments to carry different voltages — the first contact is sub-lethal, but if the whip wraps the target, each successive contact escalates. Victims describe a 'conversation' of increasing pain.",
      "A whip-fighting school has been producing exceptionally skilled graduates who are all being recruited by the same employer. The school's curriculum may include more than martial arts — it may be a front for operative training."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "whip", "electrical", "area-denial", "tier 3", "carbon-lattice", "flexible"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Monofilament Lash ML-1 'Razor Rain'",
    type: "weapon",
    aliases: ["Razor Rain", "ML-1", "Mono-Lash", "The Cutter"],
    category: "melee",
    description: "A whip-profile weapon that uses a monofilament wire as its cutting element, contained within a weighted guidance sleeve that provides the mass necessary for whip dynamics while keeping the molecular-edge wire under tension. The ML-1 is 2.5 meters of invisible death — the monofilament core is too thin to see, and the guidance sleeve is designed to peel away from the wire during the final quarter of a strike, exposing the bare cutting filament for the last 60 centimeters of contact.\n\nThe result is a weapon that wraps and then cuts. The weighted sleeve provides the mass and momentum for the whip to reach its target and wrap around a limb, torso, or neck. Once wrapped, the operator pulls sharply, and the guidance sleeve slides back along the wire, exposing monofilament that slices through whatever it contacts with zero resistance. Against biological targets, the effect is amputation-speed cutting along the entire wrap circumference.\n\nArcturus developed the ML-1 under a military contract for a weapon capable of silently disabling sentries and removing obstacles at short range. The weapon requires extraordinary skill to use safely — the monofilament wire can cut through anything, including the operator, and accidental self-contact during a missed swing has caused injuries and fatalities during training. Arcturus provides the ML-1 only to operators who have completed a 200-hour certified training program.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — monofilament weapon authorization required",
    base_technologies: ["Monofilament wire cutting element", "Weighted guidance sleeve dynamics", "Controlled sleeve-retraction exposure system"],
    specifications: "total_length: 2.5 meters\nweight: 650 g (mostly guidance sleeve)\ncutting_element: Carbon monofilament, 3 nm diameter\nexposure_zone: Final 60 cm of strike arc\npower_requirement: None — mechanical system\ntraining_requirement: 200 hours certified\nself-injury_risk: Extreme for untrained users",
    tactical_use: "The Razor Rain is a silent elimination weapon. The weighted sleeve delivers the monofilament to the target through standard whip dynamics, and the retraction cut severs whatever the wire has wrapped. Against sentries, the weapon wraps the neck and decapitates in a single pull. Against structural targets, it severs cables, conduits, and support members. The weapon's silence and invisible cutting element make it ideal for operations where detection means mission failure. The extreme training requirement and self-injury risk limit its deployment to specialist operators.",
    cultural_context: "The ML-1 exists at the intersection of weapon and nightmare. Its capability — invisible, silent, capable of removing limbs at a distance — represents a threat that cannot be effectively defended against because the attack cannot be seen coming. Among military operators, Razor Rain certification is a mark of exceptional skill and risk tolerance. Among everyone else, the weapon is a horror story that may or may not be real. Arcturus neither confirms nor denies public sales.",
    known_users: ["Arcturus Tier 5 elimination specialists", "Military sentry-removal teams"],
    story_hooks: [
      "A monofilament lash was used in a Tier 3 district — somewhere it should never appear. The weapon severed a structural cable that collapsed a pedestrian bridge, killing six. Was it an assassination disguised as infrastructure failure, or an untrained user with a stolen weapon?",
      "An ML-1 operator has defected from their corporate employer and is offering their services independently. Their asking price is Φ100,000 per engagement — and they have three confirmed contracts already."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "whip", "monofilament", "military", "tier 5", "silent", "elimination"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Fighting Chain 'Serpent'",
    type: "weapon",
    aliases: ["Serpent", "Fighting Chain", "Iron Snake", "The Coil"],
    category: "melee",
    description: "A two-meter weighted chain with shaped striking links at both ends, constructed from case-hardened carbon-lattice steel. The Serpent is an ancient weapon type rebuilt with modern materials — each link is individually forged and case-hardened to resist deformation on impact, and the terminal striking weights are machined from solid Ablonite-KR ceramic for maximum impact damage and the material's characteristic thermal reaction against ferrous targets.\n\nThe Forge-Smith Collective's chain fighters represent one of GLMZ's oldest continuous martial traditions — chain fighting predates the city's corporate era and has been passed through community lineages for generations. The weapon's appeal is its versatility: it can strike, wrap, disarm, entangle, and control space. A skilled chain fighter in a corridor is a moving perimeter of steel that can engage threats from multiple angles simultaneously.\n\nThe Serpent's ceramic terminal weights add a modern edge to the traditional weapon. On impact with cybernetic components, the Ablonite-KR's reactive properties generate localized heating that damages sensitive electronics and causes pain responses in biological tissue adjacent to implant housings. Chain fighters have adapted their techniques to exploit this property, targeting the joints and connection points of augmented opponents where ceramic-on-metal contact causes maximum disruption.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-2 (artisan availability)",
    legality: "Unlicensed — artisan production",
    base_technologies: ["Case-hardened carbon-lattice chain links", "Ablonite-KR ceramic terminal weights", "Traditional chain-fighting geometry"],
    specifications: "total_length: 2 meters\nweight: 1.4 kg\nlink_material: Case-hardened carbon-lattice steel\nterminal_weights: Ablonite-KR ceramic, 150 g each\nlink_count: 36\npower_requirement: None",
    tactical_use: "The Serpent excels in corridor fighting and one-versus-many engagements where its rotating arc creates a lethal perimeter. The chain's flexibility allows strikes around corners, over obstacles, and past defensive positions. The ceramic terminal weights deliver crushing impact damage enhanced by Ablonite-KR's reactive properties against cybernetic targets. In grappling range, the chain wraps limbs for control and pain compliance. The weapon's primary limitation is its minimum effective range — inside the chain's arc, the wielder is vulnerable, and a fast opponent who closes past the striking distance neutralizes the weapon.",
    cultural_context: "Chain fighting in GLMZ carries a lineage that practitioners take seriously — techniques are passed from teacher to student in an unbroken tradition that predates the city's current corporate structure. The Forge-Smith Collective views chain-fighting as a cultural heritage practice, and the production of Serpent chains is considered as much a preservation effort as a commercial activity. Annual chain-fighting gatherings function as both competition and cultural celebration, with veteran practitioners demonstrating historical techniques alongside modern adaptations.",
    known_users: ["Traditional chain-fighting lineage practitioners", "Lower-tier martial artists", "Community defense specialists"],
    story_hooks: [
      "The oldest chain-fighting lineage in GLMZ is dying — its last master has no students willing to commit to the decade-long training tradition. A cultural heritage group is trying to preserve the techniques before they are lost.",
      "A chain-fighter's Serpent has been forensically linked to damage at three separate crime scenes — but the fighter has alibis for all three incidents. Either the forensics are wrong or someone is using an identical chain to frame them."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "chain", "traditional", "Ablonite-KR", "ceramic", "artisan", "tier 1", "cultural"]
  },
  {
    id: id(),
    name: "Tessera Corponation Neural Leash NL-2 'Puppeteer'",
    type: "weapon",
    aliases: ["Puppeteer", "NL-2", "Neural Leash", "The String"],
    category: "melee",
    description: "A flexible weapon that combines a conductive polymer whip with a neural-disruption pulse generator, allowing the operator to deliver targeted cognitive interference through physical contact at range. The Puppeteer is 1.8 meters of polymer cord that, on contact with bare skin or conductive cyberware surfaces, delivers a precisely calibrated electromagnetic pulse that temporarily overrides voluntary motor control — causing the target's muscles to obey the pulse's frequency rather than the target's own neural commands.\n\nThe effect is not paralysis — it is involuntary movement. Targets struck by the Puppeteer do not freeze; they move in ways they did not intend, their limbs responding to electromagnetic commands rather than conscious will. At low settings, this manifests as loss of coordination and involuntary jerking. At high settings, the operator can induce specific gross motor patterns — causing a target to drop a held weapon, take a step in a specific direction, or raise their arms. The control is imprecise and cannot produce fine motor manipulation, but the ability to make an opponent's body betray them is tactically devastating.\n\nTessera developed the NL-2 under a contract from their corrections division for a restraint tool that could control prisoners without physical contact. The weapon's capability has since expanded far beyond its original scope, and the ethical implications of a weapon that puppets another person's body have generated ongoing controversy that Tessera has largely ignored.",
    manufacturer: "TESSERA CORPONATION",
    tier_availability: "Tier 4+ (highly restricted)",
    legality: "Restricted — neural-override weapon, prohibited in most jurisdictions",
    base_technologies: ["Conductive polymer neural interface", "Motor-control electromagnetic override", "Targeted involuntary-movement induction"],
    specifications: "total_length: 1.8 meters\nweight: 450 g\ncontact_type: Conductive polymer on bare skin or cyberware\neffect_range: Contact only — requires physical touch\ncontrol_precision: Gross motor only — no fine manipulation\npower_source: Handle power cell, 40 activations\neffect_duration: 5-15 seconds per contact",
    tactical_use: "The Puppeteer is used to control rather than damage. An operator who lands a contact strike can force the target to drop their weapon, move out of cover, or turn to face away — creating openings that would be impossible to achieve through conventional combat. Against augmented targets, the neural override propagates through cybernetic motor pathways with enhanced effect, making the weapon more powerful against the most dangerous opponents. The limitation is the contact requirement — the polymer cord must touch skin or conductive surface, meaning armored targets who deny skin contact are largely immune.",
    cultural_context: "The Puppeteer has crossed a line that even GLMZ's desensitized population finds disturbing. A weapon that takes away bodily autonomy — that makes your own muscles work against you — evokes a specific horror that transcends physical pain. Confirmed incidents of NL-2 use have generated public outrage disproportionate to the weapon's actual damage potential, because the violation is psychological rather than physical. Anti-corporate activists have made the Puppeteer a symbol of corporate control made literal — a corporation that can puppet your body as easily as it puppets your livelihood.",
    known_users: ["Tessera corrections division", "Corporate detainment teams", "Covert operatives requiring non-lethal control"],
    story_hooks: [
      "A Puppeteer operator has discovered that sustained contact with a specific type of neural implant creates a persistent connection — the target continues responding to commands even after the whip is withdrawn. Tessera is very interested in this development.",
      "An NL-2 was used during a public arrest, and bystander footage of the target being puppeted against their will has gone viral. The political fallout is threatening Tessera's operating licenses in three districts."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "whip", "neural", "motor-control", "override", "tier 4", "controversial"]
  }
];

// ─── TRADITIONAL WEAPONS WITH FUTURE MATERIALS (5) ─────────────────────

const traditional = [
  {
    id: id(),
    name: "Forge-Smith Collective Obsidian Composite Macuahuitl 'Black Teeth'",
    type: "weapon",
    aliases: ["Black Teeth", "Obsidian Club", "The Jaw", "Volcanic Edge"],
    category: "melee",
    description: "A reconstruction of the Aztec macuahuitl — a flat wooden club edged with obsidian blades — rebuilt using synthetic obsidian-composite inserts set into a carbon-lattice frame. The Black Teeth's cutting edges are sharper than surgical steel, capable of edge retention that natural obsidian cannot achieve, and mounted in a frame that will not shatter under impact as traditional wooden versions did. The weapon combines pre-Columbian design philosophy with 23rd-century materials science.\n\nEach obsidian-composite insert is 8 centimeters long and 2 millimeters thick, set into machined slots along both edges of the paddle-shaped frame. The inserts are designed to be sacrificial — they chip and break on hard impacts, but each broken insert exposes a fresh cutting surface, and the weapon carries enough inserts (24 per edge) that it remains effective through extended combat. Replacement insert strips cost Φ40 and snap into the frame without tools.\n\nThe Forge-Smith Collective began producing Black Teeth at the request of a martial arts instructor with Nahua heritage who wanted to demonstrate that indigenous weapon designs had genuine tactical merit beyond historical curiosity. The weapon's performance in controlled testing — it outcut several corporate vibro-blades in unassisted cutting trials — silenced skeptics and created demand that the Collective struggles to meet. The macuahuitl has returned to GLMZ not as a museum piece but as a functional combat weapon.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-2 (artisan availability)",
    legality: "Unlicensed — artisan production",
    base_technologies: ["Synthetic obsidian-composite cutting inserts", "Carbon-lattice structural frame", "Sacrificial-edge continuous sharpness"],
    specifications: "total_length: 75 cm\nblade_width: 10 cm\nweight: 1.1 kg\ninsert_count: 48 (24 per edge)\ninsert_material: Synthetic obsidian-composite\nframe_material: Carbon-lattice composite\nreplacement_insert_cost: Φ40 per strip\nedge_sharpness: Exceeds surgical steel",
    tactical_use: "The Black Teeth cuts with extraordinary sharpness on the initial strike and maintains cutting performance as inserts break and expose fresh edges. The paddle shape allows both edge strikes and flat-face impacts, giving the wielder cutting and bludgeoning options in a single weapon. Against armored targets, the obsidian-composite inserts are hard enough to score and abrade armor coatings, degrading protection over repeated strikes. The weapon's flat profile makes it awkward to parry with conventional weapons — blades tend to slide along the paddle face rather than catching in a bind.",
    cultural_context: "The return of the macuahuitl to active combat use has resonated powerfully in GLMZ's mixed-heritage communities. The weapon represents indigenous ingenuity validated by modern science — proof that pre-colonial weapon design was sophisticated, effective, and worthy of revival. The Black Teeth has sparked interest in other traditional weapons from cultures that were historically marginalized, and the Forge-Smith Collective has begun producing composite versions of weapons from African, Pacific Islander, and South Asian traditions. The movement is called 'ancestral tech' and it is growing.",
    known_users: ["Ancestral-tech martial practitioners", "Nahua heritage martial artists", "Traditional weapons revivalists"],
    story_hooks: [
      "An ancestral-tech practitioner armed with a Black Teeth defeated a cybernetically augmented opponent in a documented public fight — the footage has become a rallying point for the unaugmented community and a source of embarrassment for the cyberware industry.",
      "A corporate anthropologist has documented the ancestral-tech movement and published a paper claiming it is a 'threat to technological progress.' The paper has generated death threats from both supporters and opponents of the movement."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "macuahuitl", "obsidian", "traditional", "artisan", "tier 1", "cultural", "ancestral-tech"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Carbon Machete 'Cane Cutter'",
    type: "weapon",
    aliases: ["Cane Cutter", "Carbon Machete", "The Farmer", "Workblade"],
    category: "melee",
    description: "A heavy-duty machete forged from solid carbon-lattice composite, designed to serve as both a utility tool and a fighting weapon. The Cane Cutter's 45-centimeter blade is thick, heavy, and honed to a working edge that prioritizes durability over surgical sharpness — it will not shave hair, but it will chop through a hardened cable conduit and still be sharp enough to fight afterward.\n\nThe Forge-Smith Collective produces the Cane Cutter as their highest-volume weapon, selling them at a price point (Φ120) that makes them accessible to anyone with a modest income. The weapon is explicitly dual-purpose: the blade geometry is optimized for chopping work — clearing debris, cutting through obstacles, processing salvage — while the full tang, weighted forward balance, and reinforced pommel make it a credible fighting weapon. The Collective's philosophy is that every tool should be capable of defending its user.\n\nThe name references the agricultural machetes used by laborers across the world for centuries — a tool of work that has been pressed into service as a weapon of resistance in every era and every culture. The Cane Cutter continues that tradition with 23rd-century materials, and its widespread adoption in GLMZ's working population has made it arguably the most commonly carried weapon in the city by sheer numbers.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1+ (mass availability)",
    legality: "Legal — classified as utility tool",
    base_technologies: ["Solid carbon-lattice composite construction", "Utility/combat dual-purpose geometry", "High-durability working edge"],
    specifications: "blade_length: 45 cm\ntotal_length: 58 cm\nweight: 650 g\nblade_material: Solid carbon-lattice composite\nedge_type: Working edge — durable over sharp\nretail_price: Φ120\nclassification: Utility tool (legal to carry)",
    tactical_use: "The Cane Cutter is effective through simplicity and availability. Its carbon-lattice blade holds a working edge through abuse that would destroy conventional steel, and its forward-weighted balance generates significant chopping force without requiring exceptional strength. In combat, the weapon excels at aggressive forward attacks — chopping strikes that exploit the blade's weight and momentum. Defensive use is limited by the blade's weight, which makes rapid repositioning slower than lighter weapons. Users compensate with footwork and aggression.",
    cultural_context: "The Cane Cutter is GLMZ's people's weapon — not because it was designed for revolution but because it was designed for work, and work is what everyone in the lower tiers does. Its legal classification as a utility tool means it can be carried openly without the permits and registrations required for weapons, making it the one piece of sharp steel that every worker can legally have on their person. The cultural impact is significant: in a city where weapons are regulated by corporate licensing, the Cane Cutter exists in a legal gray area that millions exploit daily.",
    known_users: ["GLMZ working population", "Salvage crews", "Community defense groups", "Anyone who needs a legal blade"],
    story_hooks: [
      "Corporate security is pushing to reclassify carbon machetes as weapons, which would criminalize the most commonly carried blade in the lower tiers overnight. Community groups are organizing against the reclassification, and tensions are rising.",
      "A mass order of Cane Cutters — ten thousand units — has been placed through an anonymous intermediary. The Forge-Smith Collective is debating whether to fill it. Ten thousand machetes is either a legitimate industrial order or an army's armament."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "machete", "carbon-lattice", "utility", "artisan", "tier 1", "legal", "mass-availability"]
  },
  {
    id: id(),
    name: "Crucible Industries Composite Kukri CK-7 'Gurkha'",
    type: "weapon",
    aliases: ["Gurkha", "CK-7", "Composite Kukri", "The Hook"],
    category: "melee",
    description: "A kukri-profile chopping knife built from a aerogel-reinforced carbon composite that maintains the traditional Nepalese blade's distinctive recurved geometry while reducing weight by 40% compared to steel. The Gurkha's inward-curving blade concentrates chopping force at the belly of the curve, generating impact energy disproportionate to the weapon's modest weight. Crucible's materials science division spent three years optimizing the composite layup to ensure the blade could sustain full-force chopping impacts without delamination.\n\nThe CK-7 retains the traditional kukri's two smaller companion blades — the karda and chakmak — housed in the scabbard alongside the main blade. In the Crucible version, the karda is a precision utility knife for fine cutting work, and the chakmak has been redesigned as a piezoelectric fire-starting tool that generates sparks from its composite striking surface. The three-blade system maintains the kukri tradition of providing a complete survival toolkit in a single carry package.\n\nCrucible developed the Gurkha in consultation with martial artists from the Nepalese diaspora community in GLMZ, who provided historical design constraints that the engineering team integrated into the composite construction. The result is a weapon that practitioners of traditional Nepalese blade arts can use with their existing techniques while benefiting from modern materials. The collaboration has been cited as a model for cultural-technical partnerships in GLMZ's increasingly mixed martial community.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 2+",
    legality: "Licensed — registered utility/combat blade",
    base_technologies: ["Aerogel-reinforced carbon composite", "Traditional recurve blade geometry optimization", "Three-blade survival system"],
    specifications: "blade_length: 30 cm (main blade)\ntotal_length: 42 cm\nweight: 380 g (40% lighter than steel equivalent)\nblade_material: Aerogel-reinforced carbon composite\ncompanion_blades: Karda (utility), Chakmak (fire-starting)\nedge_geometry: Traditional kukri recurve\nretail_price: Φ850",
    tactical_use: "The Gurkha's recurved blade geometry concentrates impact force at the belly of the curve, generating chopping power that exceeds straight-bladed weapons of equivalent weight. The aerogel-reinforced composite absorbs vibration that would numb the wielder's hand during sustained chopping, allowing extended combat use without fatigue. In close combat, the inward curve hooks around defensive positions — parries and blocks that would deflect a straight blade are bypassed by the kukri's geometry, which curves past the defense and strikes behind it.",
    cultural_context: "The kukri is one of the most recognized blade designs in human history, and Crucible's decision to produce a composite version with Nepalese community input has been broadly praised. The weapon represents what the 'ancestral tech' movement aspires to: traditional designs validated and enhanced by modern materials without losing their cultural identity. The Gurkha has become popular beyond the Nepalese community, adopted by users who appreciate its practical superiority for chopping tasks and its cultural significance as a bridge between heritage and innovation.",
    known_users: ["Nepalese diaspora martial practitioners", "Traditional blade arts students", "Field operatives seeking a utility/combat hybrid"],
    story_hooks: [
      "A CK-7 bearing custom engravings in traditional Nepalese script was found at a crime scene — the engravings are a prayer for justice, and the weapon was left deliberately. Someone is using the cultural weight of the kukri to make a statement about a wrong that needs righting.",
      "Crucible's Nepalese consultants have discovered that the company patented the blade geometry without crediting the traditional design — a move that violates the spirit of the collaboration and has sparked a dispute that could end the partnership."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "kukri", "traditional", "aerogel", "carbon-composite", "tier 2", "cultural", "Nepalese"]
  },
  {
    id: id(),
    name: "Street-Custom Memory Alloy Butterfly Knife 'Quicksilver'",
    type: "weapon",
    aliases: ["Quicksilver", "Memory Balisong", "Liquid Blade", "The Dancer"],
    category: "melee",
    description: "A butterfly knife (balisong) constructed entirely from shape-memory alloy, allowing the blade and handles to flex, deform, and recover their geometry through rapid manipulation. The Quicksilver takes the traditional balisong — already one of the most dynamic manual weapons in existence — and adds the shape-memory property that allows the blade to deform around obstacles and snap back to cutting rigidity faster than the eye can follow.\n\nStreet-level bladesmiths in GLMZ's Filipino diaspora community developed the Quicksilver by adapting industrial shape-memory alloy stock to traditional balisong proportions. The manufacturing process is closely guarded within the community, and production is limited to a handful of smiths who learned the technique from its originator — a retired metalworker named Dante Villanueva-Obi who combined his industrial materials knowledge with his grandmother's balisong traditions.\n\nThe weapon's manipulation characteristics are unique — the shape-memory handles flow through the user's fingers with less resistance than conventional steel, enabling manipulation speeds that blur the boundary between weapon handling and sleight of hand. Expert Quicksilver users can open, close, and transition the knife through positions so rapidly that observers cannot determine whether the blade is deployed or stowed at any given moment. This ambiguity is itself a tactical advantage.",
    manufacturer: "FILIPINO DIASPORA BLADESMITHS",
    tier_availability: "Tier 1-2 (community availability)",
    legality: "Unlicensed — artisan production, concealed weapon where applicable",
    base_technologies: ["Shape-memory alloy full construction", "Traditional balisong mechanics", "Deformation-recovery manipulation dynamics"],
    specifications: "blade_length: 11 cm\ntotal_length: 25 cm (open), 14 cm (closed)\nweight: 160 g\nmaterial: Shape-memory alloy throughout\ntransition_temperature: 30°C (slightly below body heat)\nrecovery_time: <1 second from any deformation\nproduction: Community-limited, ~50 units per year",
    tactical_use: "The Quicksilver's tactical advantage is speed of deployment and the ambiguity of its state. A user can have the blade open and cutting before an observer registers the threat, and the shape-memory property means the blade recovers from impacts and deflections that would render a conventional balisong inoperable. The weapon's small size limits it to close-quarters and grappling range, but within that range, the manipulation speed and deformation recovery make it exceptionally difficult to disarm or disable.",
    cultural_context: "The Quicksilver is a cultural artifact of GLMZ's Filipino diaspora — a community that maintained balisong traditions across generations of displacement and mixing. The weapon's development by a retired metalworker using industrial materials embodies the diaspora's pattern of adapting traditional practices to available resources. The community's protectiveness of the manufacturing process reflects both practical concerns (limiting supply maintains quality and value) and cultural ones (the technique belongs to the community, not to corporate replication).",
    known_users: ["Filipino diaspora martial practitioners", "Traditional balisong artists", "Close-quarters specialists with community connections"],
    story_hooks: [
      "Dante Villanueva-Obi has died, and his manufacturing notes are missing. Three of his students each claim to have the complete technique, but each produces slightly different blades. The community must determine which version is the authentic continuation — or whether all three are needed.",
      "A corporate materials company has obtained a Quicksilver and reverse-engineered the alloy composition. They plan to mass-produce balisongs using the technique. The community is divided between those who want to fight the appropriation and those who see wider availability as a form of cultural victory."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "knife", "balisong", "shape-memory", "traditional", "artisan", "tier 1", "cultural", "Filipino"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Composite Tonfa 'Riot Answer'",
    type: "weapon",
    aliases: ["Riot Answer", "Composite Tonfa", "The Response", "People's Baton"],
    category: "melee",
    description: "A tonfa — the Okinawan side-handled baton — constructed from aerogel-reinforced carbon composite, produced in large numbers by the Forge-Smith Collective as a direct response to corporate security's standard-issue Peacekeeper batons. The Riot Answer is explicitly designed to be the weapon that communities use to fight back against the tools of corporate enforcement: it blocks baton strikes, controls armored limbs, and delivers impact force sufficient to defeat the light body armor worn by security patrols.\n\nThe tonfa's side handle allows the weapon to be used in multiple configurations — extended for reach, retracted for close defense, spun for momentum strikes, and braced for blocks. The aerogel reinforcement absorbs shock from impacts that would shatter conventional composites, and the weapon's 600-gram weight provides enough mass for effective strikes without the exhaustion of heavier weapons. The Forge-Smith Collective sells Riot Answers in pairs and provides basic instruction in tonfa technique with every purchase.\n\nThe weapon's development was deliberate and political. The Forge-Smith Collective designed the Riot Answer after analyzing the most common security weapons deployed against lower-tier communities and engineering a counter for each. The tonfa's blocking capability defeats baton strikes. Its control techniques counter grab-and-restrain tactics. Its impact force defeats standard security armor. The weapon is not just a product — it is an argument that community self-defense is achievable with the right tools and training.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1+ (mass availability)",
    legality: "Legal — classified as defensive martial arts equipment",
    base_technologies: ["Aerogel-reinforced carbon composite", "Side-handled multi-configuration design", "Shock-absorbing defensive construction"],
    specifications: "shaft_length: 50 cm\nhandle: Side-mounted, 12 cm perpendicular grip\nweight: 600 g each (sold in pairs)\nmaterial: Aerogel-reinforced carbon composite\nshock_absorption: Rated for repeated full-force baton impacts\nretail_price: Φ180 per pair\nclassification: Defensive martial arts equipment (legal)",
    tactical_use: "The Riot Answer excels in defensive fighting against baton-armed opponents. The side handle provides leverage for blocks that absorb and redirect strikes, and the tonfa's spinning capability generates striking force from angular momentum rather than raw strength. Paired tonfas allow simultaneous offense and defense — one blocks while the other strikes, creating a rhythm of attack and defense that baton-armed opponents struggle to interrupt. The weapon is also effective for limb control, using the side handle as a lever to trap and redirect an opponent's arms.",
    cultural_context: "The Riot Answer is the Forge-Smith Collective's most politically significant product. It was designed as a weapon of resistance and is marketed as such — the Collective's literature explicitly states that the tonfa is intended to 'answer' corporate security's enforcement tools with community-accessible defensive capability. The weapon has been adopted by community defense training programs across the lower tiers, and tonfa practice has become a communal activity that builds both martial skill and social solidarity. Corporate security analysts have noted the trend with concern.",
    known_users: ["Community defense training groups", "Lower-tier martial practitioners", "Civil defense organizers", "Protest security volunteers"],
    story_hooks: [
      "A community defense group trained with Riot Answers successfully repelled a corporate security incursion — the first documented case of a lower-tier community defending its space against armed corporate forces using organized martial resistance. The footage is spreading, and other communities want to learn.",
      "The Forge-Smith Collective has been issued a cease-and-desist order claiming the Riot Answer's design infringes on a corporate patent for side-handled impact weapons. The patent is 150 years old and almost certainly public domain, but the legal challenge will cost more to fight than the Collective can afford."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "tonfa", "defensive", "aerogel", "artisan", "tier 1", "legal", "community", "resistance"]
  }
];

// ─── POLEARMS AND SPEARS WITH FUTURE TECH (5) ─────────────────────────

const polearms = [
  {
    id: id(),
    name: "Arcturus Defense Solutions Charged Pike CP-3 'Longreach'",
    type: "weapon",
    aliases: ["Longreach", "CP-3", "Charged Pike", "The Argument Settler"],
    category: "melee",
    description: "A 2.5-meter pike with an electrically charged spearhead that delivers a stunning discharge on penetration. The Longreach combines the oldest tactical principle in close combat — reach advantage — with modern electrical incapacitation. The carbon-lattice shaft is lightweight enough for rapid thrusting while rigid enough to resist lateral bending forces that would snap conventional materials, and the Ablonite-KR ceramic spearhead carries a capacitor charge that discharges on contact with conductive materials.\n\nArcturus developed the Charged Pike for corridor defense scenarios where security teams need to hold chokepoints against approaching threats without resorting to firearms. The weapon's 2.5-meter reach means defenders can engage targets before they reach striking distance, and the electrical discharge on the spearhead ensures that even glancing contact is incapacitating. Teams of three pike-armed operators can seal a standard corridor completely, presenting a wall of electrified points that no unarmored attacker can pass.\n\nThe CP-3's revival of the pike as a tactical weapon reflects a broader recognition in GLMZ's security culture that ancient formation weapons retain their effectiveness in the city's endless corridors and confined spaces. The geometry of indoor combat — narrow approaches, limited flanking, channeled movement — creates conditions identical to those that made the pike dominant on historical battlefields. Arcturus simply added electricity.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Licensed — security formation weapon",
    base_technologies: ["Ablonite-KR ceramic electrified spearhead", "Carbon-lattice rigid shaft", "Contact-discharge capacitor system"],
    specifications: "total_length: 250 cm\nweight: 1.8 kg\nshaft_material: Carbon-lattice composite\nspearhead_material: Ablonite-KR ceramic with embedded capacitor\ndischarge_voltage: 40,000 V on contact\npower_source: Shaft-integrated capacitor, 80 discharges\nhead_length: 25 cm",
    tactical_use: "The Longreach dominates corridor defense through reach and electrical threat. Three-person pike formations seal standard corridors against approaching threats, with each operator covering a third of the corridor width. The electrical discharge ensures that attackers who push past the spearpoint still receive an incapacitating shock. Against armored targets, the Ablonite-KR ceramic head penetrates composite armor through material hardness, and the discharge propagates through conductive armor components. The weapon's limitation is lateral mobility — the shaft's length makes it impractical in open spaces where attackers can flank.",
    cultural_context: "The return of pike formations to GLMZ's security landscape has generated both practical respect and historical humor. Security teams who train with the CP-3 have studied historical pike drill manuals and adapted square-formation tactics to rectangular corridors. The aesthetic of armored security operators wielding electrified pikes in carbon-lattice corridors has been described as 'medieval neo-noir' — a label that the operators themselves find more accurate than ironic.",
    known_users: ["Arcturus corridor defense teams", "Corporate chokepoint security", "Formation-trained security units"],
    story_hooks: [
      "A pike formation was broken by an attacker who deployed a counter-weapon specifically designed to exploit the pike's lateral weakness — a short-range area-effect device that disrupted the formation without entering the pikes' reach. Someone studied the tactic and designed its counter.",
      "An unauthorized pike formation has appeared in a lower-tier district, using homemade versions of the CP-3 to hold a corridor against all comers. The community behind the formation is protecting something — and corporate security cannot breach without casualties."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "pike", "polearm", "electrical", "Ablonite-KR", "tier 3", "formation", "corridor-defense"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Thermal Lance Glaive 'Red Harvest'",
    type: "weapon",
    aliases: ["Red Harvest", "Thermal Glaive", "Hot Reaper", "The Scythe"],
    category: "melee",
    description: "A glaive-profile polearm with a heated blade that reaches 1,100 degrees Celsius through a chemical fuel system running the length of the shaft. The Red Harvest combines the glaive's sweeping cutting geometry with thermal-edge technology, creating a weapon that reaps through armored and unarmored targets with equal effectiveness. The 40-centimeter blade at the end of the 180-centimeter shaft provides both cutting leverage and thermal destruction.\n\nThe Forge-Smith Collective developed the Red Harvest for community defense against armored corporate incursions — a scenario where conventional melee weapons bounce harmlessly off security armor. The thermal blade melts through composite armor on contact, and the glaive's sweeping strikes cover wide arcs that prevent multiple armored opponents from advancing simultaneously. The chemical fuel system — a ferrocerium compound similar to the Ember axe's power source — is immune to electronic interference and burns for 90 minutes per charge.\n\nThe weapon requires significant space to use effectively — the 180-centimeter shaft and 40-centimeter blade create a sweeping radius that demands open room or wide corridors. In confined spaces, the Red Harvest becomes a liability, and operators must transition to shorter weapons. This limitation has led to a tactical doctrine where glaive-armed fighters hold open areas and intersections while shorter-weapon fighters cover the tight corridors that feed into them.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 2+ (artisan availability)",
    legality: "Unlicensed — artisan production, thermal weapon classification varies",
    base_technologies: ["Chemical thermal blade system", "Glaive-profile sweeping geometry", "Ferrocerium fuel shaft integration"],
    specifications: "total_length: 220 cm (shaft + blade)\nshaft_length: 180 cm\nblade_length: 40 cm\nweight: 2.8 kg\nblade_temperature: 1,100°C\nfuel_system: Ferrocerium chemical compound, shaft-integrated\nburn_time: 90 minutes continuous\nsweep_radius: ~220 cm",
    tactical_use: "The Red Harvest controls open spaces and intersections. Its sweeping strikes cover arcs that prevent multiple opponents from advancing, and the thermal blade defeats armor that would resist conventional cutting weapons. Operators position themselves at corridor junctions and open areas where the weapon's reach advantage is maximized. The glaive's hook geometry allows operators to catch and pull armored opponents off balance, exposing them to the thermal edge. The weapon is useless in tight corridors and must be supported by fighters with shorter weapons in mixed-space combat.",
    cultural_context: "The Red Harvest represents the Forge-Smith Collective's most ambitious community defense weapon — a polearm that can hold open ground against armored corporate security. Its development was a direct response to corporate incursions into lower-tier territory, and its name reflects the grim pragmatism of communities that have accepted the possibility of organized violence. Red Harvest training sessions are conducted openly in lower-tier plazas, serving both as martial instruction and as a visible deterrent to corporate overreach.",
    known_users: ["Community defense formations", "Forge-Smith Collective practitioners", "Anti-corporate territorial defense groups"],
    story_hooks: [
      "A Red Harvest formation held an intersection against a corporate security team for six hours — long enough for the community behind them to evacuate. The footage has become a recruitment tool for community defense organizations across GLMZ.",
      "The ferrocerium fuel compound used in the Red Harvest has been found to produce toxic combustion byproducts in enclosed spaces — long-term use in poorly ventilated areas is causing respiratory damage. The Collective is racing to reformulate the fuel before the weapon poisons the people it is meant to protect."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "glaive", "polearm", "thermal", "artisan", "tier 2", "community-defense", "anti-armor"]
  },
  {
    id: id(),
    name: "Crucible Industries Vibro-Halberd VH-1 'Headsman'",
    type: "weapon",
    aliases: ["Headsman", "VH-1", "Vibro-Halberd", "The Axe-Spike"],
    category: "melee",
    description: "A halberd combining an axe blade, a thrusting spike, and a rear hook on a 200-centimeter carbon-lattice shaft, with vibration systems integrated into both the axe head and the spike. The Headsman is a weapon of terrifying versatility — it chops, stabs, hooks, and controls, and each function is enhanced by vibration technology that makes every contact point armor-defeating.\n\nThe VH-1's axe blade vibrates at 30,000 Hz for enhanced chopping, while the spike operates at 45,000 Hz for maximum penetration. The dual-frequency system draws from a shared power source in the shaft, and operators can independently activate each vibration system depending on the intended attack. The rear hook is unpowered but machined from Ablonite-KR ceramic, providing a pulling tool that can catch armor edges, limbs, and equipment with reactive-ceramic grip.\n\nCrucible designed the Headsman as a specialist weapon for operators who need the maximum possible melee versatility in a single platform. The halberd's historical role as the most complete polearm — combining the functions of axe, spear, and hook — translates directly into modern corridor combat where opponents may require chopping, piercing, or pulling depending on their armor type and tactical posture. The weapon's complexity requires significant training, but operators who master it carry a single weapon that replaces three.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Restricted — multi-mode vibro-weapon classification",
    base_technologies: ["Dual-frequency vibration system", "Multi-function polearm head", "Ablonite-KR ceramic hook component"],
    specifications: "total_length: 230 cm\nshaft_length: 200 cm\naxe_blade: 25 cm, 30,000 Hz vibration\nspike_length: 20 cm, 45,000 Hz vibration\nhook: Ablonite-KR ceramic, unpowered\nweight: 3.4 kg\npower_source: Shaft-integrated capacitor, 3 hours dual-mode",
    tactical_use: "The Headsman adapts to any melee threat. Armored opponents are engaged with the vibro-axe for chopping through protection. Shielded opponents are engaged with the vibro-spike for penetrating guard positions. Opponents at range are hooked and pulled off balance for follow-up strikes. The weapon's three-mode operation requires constant tactical assessment — the operator must choose the correct attack surface for each engagement, making the VH-1 a weapon that rewards intelligence and training over brute force. In formation use, Headsman operators provide flexible support to pike-armed teammates.",
    cultural_context: "The halberd's return to combat use has attracted a specific type of martial practitioner — the technical fighter who values versatility and complexity over simplicity. Headsman operators form a small but elite community that shares techniques and modifications. The weapon has also attracted historical re-enactors who find that their hobby techniques are suddenly tactically relevant, creating an unusual bridge between historical preservation and modern combat culture.",
    known_users: ["Crucible specialist operators", "Elite polearm combatants", "Historical combat practitioners with modern application"],
    story_hooks: [
      "A Headsman operator used the weapon's hook to pull a fleeing target off a moving vehicle — the Ablonite-KR ceramic caught the vehicle's frame and held. The operator was dragged 30 meters before the target was extracted. The footage is being used in both recruitment materials and safety warnings.",
      "A VH-1 has been recovered with both vibration frequencies altered to match the resonant failure points of a specific manufacturer's body armor. Someone customized the weapon to defeat a particular target's known protection."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "halberd", "polearm", "vibro-weapon", "Ablonite-KR", "tier 4", "multi-mode", "versatile"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Electromagnetic Javelin EJ-4 'Thunderbolt'",
    type: "weapon",
    aliases: ["Thunderbolt", "EJ-4", "EM Javelin", "Zeus Stick"],
    category: "melee",
    description: "A 180-centimeter javelin with an electromagnetic acceleration system in the grip that can be used in two modes: as a conventional thrusting spear with vibro-enhanced tip, or as a self-propelled projectile that the operator throws and the electromagnetic system accelerates to three times natural throwing speed. The Thunderbolt bridges the gap between melee weapon and ranged armament.\n\nIn spear mode, the EJ-4 functions as a vibrating thrusting weapon with a carbon-lattice shaft and an Ablonite-KR ceramic tip that penetrates armor through a combination of vibration-assisted micro-fracturing and sheer material hardness. The weapon is balanced for single-handed use with a shield or paired with a shorter sidearm.\n\nIn javelin mode, the operator activates the electromagnetic acceleration as they throw, and the grip-mounted linear accelerator adds approximately 40 meters per second to the throwing velocity. The accelerated javelin penetrates light vehicle armor at 30 meters and defeats personal armor at 50 meters. The electromagnetic system is single-use per throw — the weapon must be retrieved and the capacitor recharged before it can accelerate again. This disposable-use throwing mode transforms the Thunderbolt from a defensive spear into an offensive anti-armor weapon that can be deployed with no setup time.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 3+",
    legality: "Licensed — electromagnetic accelerated weapon",
    base_technologies: ["Grip-mounted electromagnetic linear acceleration", "Dual-mode spear/javelin operation", "Ablonite-KR vibro-enhanced tip"],
    specifications: "total_length: 180 cm\nweight: 1.5 kg\nshaft_material: Carbon-lattice composite\ntip_material: Ablonite-KR ceramic with vibro system\ntip_vibration: 38,000 Hz\nthrow_acceleration: +40 m/s over natural throwing velocity\neffective_throw_range: 50 meters against personnel, 30 meters against vehicles\npower_source: Grip capacitor, 1 accelerated throw per charge, 3 hours vibro mode",
    tactical_use: "The Thunderbolt provides operators with a melee weapon that doubles as a single-shot ranged anti-armor system. In defensive positions, it functions as a reach-advantage spear with vibro-enhanced penetration. When an opportunity presents — an armored vehicle approaching, a high-value target in the open, a critical system that must be disabled from range — the operator throws the Thunderbolt with electromagnetic acceleration, delivering armor-defeating kinetic energy at distances no other thrown weapon can match. The one-shot limitation means the throw must be decisive; missed throws leave the operator without their primary weapon.",
    cultural_context: "The electromagnetic javelin has captured imaginations in GLMZ's combat culture in a way that more sophisticated weapons have not. There is something primal about a thrown spear — and a thrown spear that pierces vehicle armor speaks to a deep human satisfaction in simple solutions to complex problems. Thunderbolt throwing competitions have emerged as a spectator sport, with operators competing for distance, accuracy, and most impressively, penetration through standardized armor targets. The sport has attracted corporate sponsorship, which some participants view as ironic given that the weapon was designed to fight corporate security.",
    known_users: ["Arcturus mobile assault teams", "Javelin sport competitors", "Anti-vehicle infantry specialists"],
    story_hooks: [
      "A Thunderbolt was thrown through an armored corporate transport's engine compartment from a rooftop 45 meters away — a shot that exceeded the weapon's rated penetration by a factor of two. Either the weapon was modified or the operator is superhumanly strong. Arcturus wants to know which.",
      "A javelin competition has been infiltrated by an operator who is using the events as cover to scout security arrangements at the venue — a corporate headquarters that hosts the competitions on its grounds. The next throw might not be aimed at a target."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "javelin", "polearm", "electromagnetic", "Ablonite-KR", "tier 3", "thrown", "dual-mode"]
  },
  {
    id: id(),
    name: "Forge-Smith Collective Barbed Trident 'Reef Breaker'",
    type: "weapon",
    aliases: ["Reef Breaker", "Barbed Trident", "Three Fingers", "The Fork"],
    category: "melee",
    description: "A three-pronged trident with backward-facing barbs on each tine, constructed from case-hardened carbon-lattice steel. The Reef Breaker is designed with a specific tactical purpose: to penetrate body armor and resist extraction. The barbs engage with armor material and underlying tissue on entry, and any attempt to pull the weapon free causes the barbs to expand the wound channel and snag on armor's internal layering. Removing a Reef Breaker from an armored target typically requires cutting the armor away from the outside.\n\nThe Forge-Smith Collective developed the trident as a psychological weapon as much as a physical one. The sight of a three-pronged barbed spear is viscerally threatening in a way that more sophisticated weapons are not — it triggers an ancient recognition of impalement that no amount of modern combat experience can override. Opponents facing a Reef Breaker must contend with both the physical threat and the primitive fear response that the weapon's appearance generates.\n\nEach tine is individually barbed with a pattern specific to the smith who produced it — a maker's mark that doubles as a functional design element. The barb patterns vary from simple backward angles to complex spiral configurations that rotate during extraction attempts, and experienced Forge-Smith artisans consider barb design a competitive art form. The most respected smiths produce barbs that are both maximally effective and aesthetically distinctive — a combination of beauty and brutality that defines the Collective's design philosophy.",
    manufacturer: "FORGE-SMITH COLLECTIVE",
    tier_availability: "Tier 1-2 (artisan availability)",
    legality: "Unlicensed — artisan production, classified as barbed weapon where applicable",
    base_technologies: ["Carbon-lattice case-hardened construction", "Anti-extraction barb engineering", "Three-point armor penetration geometry"],
    specifications: "total_length: 200 cm\nshaft_length: 160 cm\ntine_length: 40 cm each\nweight: 2.6 kg\nmaterial: Case-hardened carbon-lattice steel\nbarb_type: Smith-specific, anti-extraction\narmor_penetration: Rated for Level III composite",
    tactical_use: "The Reef Breaker is used to disable rather than kill — a barbed trident embedded in an opponent's torso or limb renders them effectively immobilized, unable to advance or retreat without catastrophic injury. In formation use, trident-armed fighters pin lead attackers in place while teammates engage the remainder. The anti-extraction barbs mean that even if the trident wielder releases the weapon, the embedded trident continues to impair the target. Against armored opponents, the three-point geometry concentrates force at three simultaneous points, overcoming the armor's ability to distribute impact.",
    cultural_context: "The trident carries cultural weight from multiple traditions — Roman gladiatorial combat, Hindu mythology, marine warfare — and in GLMZ's blended culture, all of these associations coexist. The Reef Breaker's name references the weapon's effectiveness in the 'reef' of armored corridors, but also evokes oceanic imagery that resonates with the city's coastal communities. Trident fighting has a small but passionate following, and the annual barb-design competition is one of the Forge-Smith Collective's most creatively celebrated events.",
    known_users: ["Community defense formations", "Trident-fighting practitioners", "Anti-armor infantry groups"],
    story_hooks: [
      "A corporate security officer was found impaled on a Reef Breaker in a lower-tier corridor — the barbs were still engaged, meaning whoever placed the weapon had time to set it as a trap rather than wield it. The trident was mounted at chest height across a doorway, aimed at the corridor the officer was known to patrol.",
      "A Forge-Smith artisan has developed barbs that release a chemical compound on extraction attempt — a delayed-action toxin that punishes any attempt to remove the weapon. The innovation has divided the Collective between those who consider it brilliant and those who consider it a violation of artisan ethics."
    ],
    ammunition_type: [],
    tags: ["weapon", "melee", "trident", "polearm", "barbed", "artisan", "tier 1", "anti-armor", "formation"]
  }
];

// ─── WRITE ALL ─────────────────────────────────────────────────────────

const allWeapons = [
  ...scatterGuns,
  ...swords,
  ...powerAxes,
  ...daggers,
  ...clubs,
  ...knuckleWeapons,
  ...whips,
  ...traditional,
  ...polearms
];

for (const w of allWeapons) {
  if (writeEntity(w)) wrote++;
  else skipped++;
}

console.log(`\nDone. Wrote ${wrote}, skipped ${skipped}, total defined: ${allWeapons.length}`);
