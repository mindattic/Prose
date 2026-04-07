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

// ─── SNIPER RIFLES (20) ────────────────────────────────────────────────

const sniperRifles = [
  {
    id: id(),
    name: "Arcturus Defense Solutions Guided Rail Rifle GRR-12 'Shepherd'",
    type: "weapon",
    aliases: ["Shepherd", "GRR-12", "The Patient One", "God's Finger"],
    category: "sniper",
    description: "A two-stage railgun sniper system that fires self-correcting tungsten sabots at Mach 8. The GRR-12 integrates with BCI smart-link to allow the operator to designate a target and let the weapon's onboard guidance package handle terminal corrections. The round carries a micro-thruster array that adjusts trajectory within a 3-degree cone during the final 200 meters of flight.\n\nArcturus developed the Shepherd for counter-sniper operations where first-round hits at extreme range are non-negotiable. The weapon requires a dedicated power cell worn as a backpack unit, and the electromagnetic signature of each shot is detectable by military-grade sensors within a 2km radius — meaning the operator must relocate immediately after firing.\n\nAmong Tier 5 security contractors, the Shepherd has developed an almost religious reputation. Operators speak of 'laying hands' on a target, and the weapon's BCI integration has been described as an unsettling communion between shooter and system.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — requires active theater authorization",
    base_technologies: ["Two-stage electromagnetic acceleration", "BCI-linked terminal guidance", "Micro-thruster trajectory correction"],
    specifications: "caliber: 3mm tungsten sabot with guidance package\neffective_range: 300-4,500 meters\nrate_of_fire: 1 round per 8 seconds (capacitor recharge)\ncapacity: 4-round internal magazine\nweight: 9.2 kg rifle, 5.8 kg power pack\npower_source: Dorsal supercapacitor unit, 16 shots per charge",
    tactical_use: "The Shepherd is deployed for high-value target elimination at ranges where conventional precision rifles fail. Its guided-round system compensates for wind, atmospheric distortion, and even minor target movement, making first-round kills at 3km+ a realistic expectation. Operators typically work in two-person teams — one shooter, one spotter running electronic countermeasures to mask the railgun's electromagnetic pulse.",
    cultural_context: "In corporate warfare, a Shepherd kill is considered a statement of absolute dominance. The weapon is so expensive and so tightly controlled that its deployment signals an organization willing to spend Φ200,000+ on a single engagement. Street-level fixers use the phrase 'Shepherd weather' to describe days when corporate tensions run high enough that precision assets might be in play.",
    known_users: ["Arcturus Tier 5 interdiction teams", "SELECT corporate security details"],
    story_hooks: [
      "A Shepherd round was recovered from a Tier 3 apartment wall — but the shot came from over 4km away, through two buildings. Someone has modified the guidance package for indirect fire.",
      "An Arcturus operator has gone rogue with a GRR-12 and is selling precision kills to the highest bidder through an anonymous dead-drop system."
    ],
    ammunition_type: ["3mm guided tungsten sabot"],
    tags: ["weapon", "sniper", "railgun", "corporate", "tier 5", "BCI", "guided"]
  },
  {
    id: id(),
    name: "Tessera Phantom Mark IV 'Ghost Writer'",
    type: "weapon",
    aliases: ["Ghost Writer", "Phantom IV", "The Whisper", "Ink"],
    category: "sniper",
    description: "Tessera's flagship suppressed precision platform fires caseless subsonic rounds through a 14-baffle integral suppressor that reduces the weapon's acoustic signature to below 60 decibels. The Phantom Mark IV was designed for deniable operations in urban environments where gunfire would attract immediate response.\n\nThe rifle uses a delayed-blowback action tuned for absolute reliability with subsonic ammunition, and the barrel incorporates a thermal masking sleeve that dissipates heat signatures within seconds of firing. At effective range, the only indication of a Phantom shot is the impact itself.\n\nTessera markets the weapon exclusively through classified procurement channels, and each unit is serialized with a quantum-dot identifier that the company claims is impossible to remove. Street armorers disagree, and a thriving black market exists for 'cleaned' Phantoms with their identifiers acid-etched away.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Classified procurement only — corporate or state actors",
    base_technologies: ["Integral multi-baffle suppression", "Caseless subsonic ammunition", "Thermal masking barrel sleeve"],
    specifications: "caliber: 8.6mm caseless subsonic\neffective_range: 50-800 meters\nrate_of_fire: Semi-automatic, 1 round per second\ncapacity: 10-round detachable magazine\nweight: 5.4 kg\npower_source: None — conventional mechanical action",
    tactical_use: "The Ghost Writer excels in urban assassination where stealth outweighs raw stopping power. Operators pair the weapon with BCI-linked optics that compensate for the subsonic round's pronounced bullet drop at range. The weapon is favored for operations inside Meridian 88's enclosed arcology levels where sound carries unpredictably and a single unsuppressed shot can trigger automated security responses across multiple sectors.",
    cultural_context: "The phrase 'ghost writing' has entered Meridian 88 slang to describe any covert action carried out with professional precision. When someone dies without witnesses, without sound, and without forensic evidence, people say they were 'ghost written.' Tessera neither confirms nor denies the weapon's existence in public channels.",
    known_users: ["Tessera covert operations division", "Unnamed corporate wet-work teams"],
    story_hooks: [
      "Three mid-level corporate managers from competing firms were ghost written in the same week — all killed with 8.6mm caseless rounds. Someone is pruning a corporate org chart.",
      "A cleaned Phantom has surfaced on the black market with its original quantum-dot identifier still readable under deep UV — it traces back to a Tessera executive's personal armory."
    ],
    ammunition_type: ["8.6mm caseless subsonic"],
    tags: ["weapon", "sniper", "suppressed", "stealth", "corporate", "tier 4", "assassination"]
  },
  {
    id: id(),
    name: "Crucible Industries Long Arm LA-20 'Verdict'",
    type: "weapon",
    aliases: ["Verdict", "LA-20", "Judge's Gavel", "The Last Word"],
    category: "sniper",
    description: "A heavy anti-materiel railgun designed for engaging hardened targets at extreme range. The LA-20 fires a 6mm depleted uranium penetrator at velocities exceeding Mach 10, generating enough kinetic energy to defeat light vehicle armor at 3 kilometers. The weapon is semi-portable — technically man-carried, but effective deployment requires a stabilization tripod and a two-person crew.\n\nCrucible designed the Verdict for corporate border disputes where armored vehicles and reinforced positions made conventional sniper rifles irrelevant. The weapon's electromagnetic launch system produces a distinctive violet flash and a pressure wave that can be felt by bystanders within 20 meters of the firing position.\n\nThe LA-20 has earned its nickname from its absolute lethality against personnel targets. There is no armor system rated for personal wear that can stop a Verdict round. Corporate security advisors simply classify it as an environmental hazard.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — heavy weapons authorization required",
    base_technologies: ["High-velocity electromagnetic launch", "Depleted uranium penetrator ballistics", "Portable heavy railgun engineering"],
    specifications: "caliber: 6mm depleted uranium penetrator\neffective_range: 500-5,000 meters\nrate_of_fire: 1 round per 12 seconds\ncapacity: 3-round internal magazine\nweight: 18.5 kg rifle, 8 kg power unit, 6 kg tripod\npower_source: Belt-mounted fusion cell, 30 shots per cell",
    tactical_use: "The Verdict is deployed against hardened targets that lighter sniper systems cannot defeat — armored command vehicles, reinforced observation posts, and shielded communications arrays. Its anti-personnel use is technically overkill but sends an unmistakable message. Corporate armies deploy LA-20 teams to establish no-go zones during territorial disputes, as the weapon's extreme range and penetration make any position within line of sight untenable.",
    cultural_context: "In Meridian 88's corporate conflict zones, a Verdict shot is called 'the court ruling' — it settles disputes with finality. Lower-tier residents who live near corporate borders have learned to recognize the distant violet flash and the delayed thunderclap that follows. When they see it, they know to stay indoors for days.",
    known_users: ["Crucible Industries heavy weapons division", "ARCTURUS DEFENSE SOLUTIONS border interdiction"],
    story_hooks: [
      "A Verdict round punched through a Tier 4 executive's panic room — three walls of reinforced composite, and the round still had enough energy to exit the building. The shooter was never found.",
      "Crucible is testing an LA-20 variant that fires programmable rounds capable of airburst — turning an anti-materiel weapon into an area denial system."
    ],
    ammunition_type: ["6mm depleted uranium penetrator"],
    tags: ["weapon", "sniper", "railgun", "anti-materiel", "heavy", "tier 5", "corporate"]
  },
  {
    id: id(),
    name: "Sable Precision Works Mirage SPW-7 'Heat Shimmer'",
    type: "weapon",
    aliases: ["Heat Shimmer", "Mirage", "SPW-7", "Desert Ghost"],
    category: "sniper",
    description: "A thermally-cooled precision rifle that uses a cryogenic barrel jacket to eliminate heat mirage and thermal bloom from its firing signature. The Mirage SPW-7 fires conventional high-velocity rounds but wraps them in a suite of technologies designed to make the shooter invisible to thermal detection systems.\n\nSable Precision Works is a boutique manufacturer operating out of a single facility in Meridian 88's industrial sector. They produce fewer than 200 Mirage units per year, each hand-fitted and individually calibrated. The rifle's cryogenic jacket maintains the barrel at near-ambient temperature even during sustained fire, and the weapon's stock incorporates a thermal regulation mesh that masks the shooter's body heat signature from the shoulders up.\n\nThe Mirage has become the weapon of choice for independent contractors who operate without the institutional support of corporate sniper teams. Where Arcturus shooters rely on guided rounds and overwhelming technology, Mirage operators rely on fundamentals — concealment, patience, and precision.",
    manufacturer: "SABLE PRECISION WORKS",
    tier_availability: "Tier 4+",
    legality: "Licensed — restricted to bonded security contractors",
    base_technologies: ["Cryogenic barrel thermal management", "Thermal signature masking", "Hand-fitted precision mechanics"],
    specifications: "caliber: 7.62mm match-grade\neffective_range: 100-2,200 meters\nrate_of_fire: Bolt-action, 1 round per 3 seconds\ncapacity: 5-round internal magazine\nweight: 6.1 kg with cryogenic jacket\npower_source: Micro cryo-cell, 4 hours continuous cooling",
    tactical_use: "The Mirage excels in environments saturated with thermal detection — corporate enclaves, security corridors, and automated defense zones. While the round itself is conventional, the rifle's thermal masking transforms the operator into a gap in sensor coverage. Shooters report that security drones will scan directly over a Mirage position and register nothing. The weapon's limitation is its reliance on the cryo-cell — when the coolant runs out, the rifle is just an expensive bolt gun.",
    cultural_context: "Sable Precision Works has cultivated a mystique around the Mirage that borders on artisanal fetishism. Each rifle ships with a hand-written card from the master armorer who built it. Independent contractors who carry Mirages form loose professional networks and share intelligence about sensor patterns and detection gaps. They call themselves 'shimmer artists.'",
    known_users: ["Independent precision contractors", "Select corporate security advisors"],
    story_hooks: [
      "A shimmer artist was killed in their hide position — someone tracked them despite the thermal masking. Either the Mirage's cryogenic system has a flaw, or someone has developed a counter-detection method.",
      "Sable Precision Works has gone silent — their facility is locked down and no new Mirages have shipped in months. Existing units are commanding Φ80,000+ on the black market."
    ],
    ammunition_type: ["7.62mm match-grade"],
    tags: ["weapon", "sniper", "precision", "stealth", "thermal", "tier 4", "boutique"]
  },
  {
    id: id(),
    name: "Tessera Overwatch Platform OWP-3 'Panopticon'",
    type: "weapon",
    aliases: ["Panopticon", "OWP-3", "Eye of God", "The Watcher"],
    category: "sniper",
    description: "A semi-autonomous sniper platform that combines a precision railgun with an AI-assisted targeting suite. The OWP-3 can be deployed in a fixed position and left to operate autonomously for up to 72 hours, using its sensor array to identify and engage designated target profiles without human intervention.\n\nThe Panopticon's AI targeting operates on a strict engagement protocol — it will only fire on targets matching pre-loaded biometric profiles, and it requires BCI confirmation from an authorized operator before engaging unregistered targets. In autonomous mode, the weapon achieves a first-round hit probability of 94% at ranges under 2km.\n\nTessera markets the OWP-3 as a force multiplier for corporate perimeter defense, but its autonomous capability has raised concerns even within Meridian 88's permissive regulatory environment. Several municipal councils have attempted to ban autonomous lethal platforms, only to discover that Tessera's legal team had preemptively secured exemptions through corporate sovereignty provisions.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 5",
    legality: "Corporate sovereign territory only — banned in public zones",
    base_technologies: ["AI-assisted autonomous targeting", "Precision railgun platform", "72-hour autonomous deployment capability"],
    specifications: "caliber: 4mm tungsten flechette\neffective_range: 200-2,500 meters\nrate_of_fire: 1 round per 5 seconds\ncapacity: 20-round sealed magazine\nweight: 32 kg (platform), 12 kg (power unit)\npower_source: Compact fusion cell, 72 hours standby / 40 shots active",
    tactical_use: "The Panopticon is deployed to establish persistent overwatch without committing personnel. Corporate security details place OWP-3 units at choke points, rooftops, and perimeter positions where they function as automated sentinels. The weapon's AI can track multiple targets simultaneously and prioritize engagement based on threat assessment algorithms. In contested zones, a single Panopticon can deny access to an area more effectively than a four-person sniper team.",
    cultural_context: "The existence of autonomous kill platforms has created a new form of urban anxiety in Meridian 88. Residents in corporate-adjacent zones speak of 'watched streets' — thoroughfares where Panopticons are suspected to be deployed. Some communities have developed informal networks that track and share suspected OWP-3 positions, though Tessera's legal department aggressively pursues anyone who publishes confirmed locations.",
    known_users: ["Tessera corporate security", "ARCTURUS DEFENSE SOLUTIONS perimeter defense"],
    story_hooks: [
      "A Panopticon unit has gone rogue — still active after its authorization expired, still engaging targets matching a biometric profile that was supposed to be purged. The targets are all employees of a single corporation.",
      "Someone has hacked an OWP-3's targeting protocol and loaded civilian biometric data. The weapon is now an indiscriminate killer hiding somewhere in the upper tiers."
    ],
    ammunition_type: ["4mm tungsten flechette"],
    tags: ["weapon", "sniper", "autonomous", "AI", "railgun", "corporate", "tier 5", "surveillance"]
  },
  {
    id: id(),
    name: "Korova Arms Whisper KA-9 'Lullaby'",
    type: "weapon",
    aliases: ["Lullaby", "KA-9", "Quiet Time", "The Nanny"],
    category: "sniper",
    description: "A compact precision rifle firing micro-caliber chemical-tipped rounds designed for silent elimination. The Whisper KA-9 uses an integral closed-bolt suppressor and subsonic propellant that reduces its acoustic signature to under 45 decibels — quieter than a conversation.\n\nKorova Arms specializes in deniable-operations equipment, and the Lullaby is their masterwork. The rifle's 2mm rounds carry a fast-acting neurosuppressant payload that induces cardiac arrest within 8-12 seconds of skin penetration. The wound channel is so small that it can be mistaken for an insect bite during cursory examination. Only a detailed autopsy with toxicology screening reveals the cause of death.\n\nThe KA-9 is technically classified as a medical delivery device in Korova's product catalog — a classification that their legal department defends with straight faces and impeccable documentation.",
    manufacturer: "KOROVA ARMS",
    tier_availability: "Tier 4+",
    legality: "Officially does not exist as a weapon system",
    base_technologies: ["Micro-caliber chemical delivery", "Integral closed-bolt suppression", "Neurosuppressant payload engineering"],
    specifications: "caliber: 2mm chemical-tipped micro-round\neffective_range: 30-400 meters\nrate_of_fire: Semi-automatic, 1 round per 2 seconds\ncapacity: 8-round sealed magazine\nweight: 2.8 kg\npower_source: None — mechanical action",
    tactical_use: "The Lullaby is used for assassinations that must appear natural or accidental. Operators engage targets in crowds, through open windows, or during routine activities. The micro-caliber round's minimal penetration means there is no exit wound and no collateral damage. The neurosuppressant causes symptoms consistent with sudden cardiac death. Medical examiners who are not specifically looking for a 2mm puncture wound will rule the death natural.",
    cultural_context: "In Meridian 88's intelligence community, a 'lullaby' has become slang for any clean, untraceable kill. The weapon's existence is an open secret among fixers and high-tier operators, but Korova's legal fiction as a medical device manufacturer makes official action impossible. When corporate executives begin dying of sudden heart failure at statistically improbable rates, people whisper about lullabies.",
    known_users: ["Unnamed intelligence services", "High-tier independent contractors"],
    story_hooks: [
      "A medical examiner has noticed a pattern — seven sudden cardiac deaths in three months, all in the same corporate hierarchy. She found the puncture wound on the third victim.",
      "Korova Arms has released a new chemical payload that leaves no detectable trace — even toxicology screening comes back clean. The only evidence is a 2mm hole that closes within hours."
    ],
    ammunition_type: ["2mm neurosuppressant micro-round"],
    tags: ["weapon", "sniper", "assassination", "chemical", "stealth", "tier 4", "deniable"]
  },
  {
    id: id(),
    name: "Crucible Industries Longstrike LS-15 'Causeway'",
    type: "weapon",
    aliases: ["Causeway", "LS-15", "Bridge Builder", "The Long Road"],
    category: "sniper",
    description: "A modular precision rifle system that can be configured for ranges from 500 to 6,000 meters by swapping barrel assemblies, optics packages, and ammunition types. The Longstrike is Crucible's answer to operators who need a single platform capable of adapting to any engagement scenario.\n\nThe LS-15's core receiver accepts three barrel configurations: a conventional 7.62mm precision barrel for urban work, a heavy .408 barrel for extended range, and an electromagnetic accelerator barrel powered by an auxiliary capacitor pack. Each configuration changes the weapon's characteristics entirely, and experienced operators can swap barrels in under 90 seconds.\n\nCrucible sells the Longstrike as a complete system — rifle, three barrels, optics suite, and a hardened transit case. The full kit costs more than most operators earn in a year, but those who own one rarely carry anything else.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and corporate security contractors",
    base_technologies: ["Modular receiver architecture", "Multi-caliber barrel swapping", "Hybrid conventional/electromagnetic firing"],
    specifications: "caliber: 7.62mm / .408 / 4mm electromagnetic (barrel-dependent)\neffective_range: 500-6,000 meters (configuration-dependent)\nrate_of_fire: Semi-automatic (7.62mm), bolt-action (.408), single-shot (EM)\ncapacity: 10 / 5 / 3 rounds (configuration-dependent)\nweight: 5.8-11.2 kg (configuration-dependent)\npower_source: None (conventional) / Auxiliary capacitor (EM barrel)",
    tactical_use: "The Causeway's modularity makes it the preferred platform for operators who deploy across multiple environments without logistical support. A contractor moving from a Tier 2 urban extraction to a Tier 5 border interdiction can reconfigure the same weapon for both missions. The electromagnetic barrel turns the platform into a lightweight railgun sniper capable of defeating armored targets, though the auxiliary power requirement limits its sustained use.",
    cultural_context: "Crucible's marketing for the Longstrike emphasizes self-reliance and adaptability — values that resonate with Meridian 88's independent contractor culture. Owning a complete LS-15 kit is a professional credential. Fixers who see a Causeway case know they are dealing with someone who takes precision work seriously and has the resources to prove it.",
    known_users: ["Crucible Industries field demonstration teams", "Independent military contractors"],
    story_hooks: [
      "An LS-15 electromagnetic barrel was found at a crime scene with its serial numbers intact — it traces to a Crucible demonstration kit that was supposed to be destroyed after a failed field test. Someone inside Crucible is diverting prototype equipment.",
      "A contractor has modified their Causeway to accept a fourth barrel type — a plasma-channeled accelerator that nobody has seen before. They are offering demonstrations to interested parties."
    ],
    ammunition_type: ["7.62mm match-grade", ".408 heavy precision", "4mm electromagnetic penetrator"],
    tags: ["weapon", "sniper", "modular", "multi-caliber", "railgun", "tier 4", "versatile"]
  },
  {
    id: id(),
    name: "Zheng-Dao Heavy Industries Dragon's Breath DB-8 'Kiln'",
    type: "weapon",
    aliases: ["Kiln", "DB-8", "Dragon Shot", "The Furnace"],
    category: "sniper",
    description: "A precision thermal lance rifle that fires a concentrated plasma bolt at hypersonic velocities. Unlike continuous-beam thermal weapons, the Dragon's Breath packages its energy into a discrete projectile — a magnetically-contained plasma mass roughly the size of a marble that maintains coherence for approximately 800 meters before dispersing.\n\nZheng-Dao developed the DB-8 for anti-armor precision work. The plasma bolt delivers approximately 40 megajoules of thermal energy on impact, sufficient to melt through 15cm of composite armor plating. Against personnel, the effect is catastrophic and leaves forensic evidence that is difficult to interpret — investigators unfamiliar with the weapon initially classify DB-8 kills as industrial accidents.\n\nThe weapon requires a dedicated cooling system that circulates liquid nitrogen through the barrel assembly between shots, giving the rifle its characteristic frost-coated appearance in the field.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — heavy weapons authorization and thermal weapons permit",
    base_technologies: ["Magnetically-contained plasma bolt generation", "Hypersonic plasma projectile stabilization", "Cryogenic barrel cooling"],
    specifications: "caliber: Plasma bolt, 12mm containment diameter\neffective_range: 100-800 meters\nrate_of_fire: 1 shot per 15 seconds (cooling cycle)\ncapacity: 8 plasma charges\nweight: 14 kg rifle, 6 kg cooling pack\npower_source: Dual fusion cell, 16 shots per charge pair",
    tactical_use: "The Kiln fills a niche between conventional anti-materiel rifles and directed energy weapons. Its plasma bolt penetrates targets that would shrug off conventional rounds, while its discrete-projectile nature avoids the sustained-beam detection signatures that betray conventional thermal weapons. Operators deploy the DB-8 against fortified positions, armored vehicles, and hardened command posts where a single precision strike can change the tactical situation.",
    cultural_context: "Zheng-Dao's weapons division has a reputation for building equipment that is simultaneously elegant and terrifying. The Dragon's Breath embodies this philosophy — a weapon of extraordinary precision that kills through localized incineration. In corporate conflict zones, the discovery of plasma-scorched impact craters is enough to trigger evacuation protocols.",
    known_users: ["Zheng-Dao corporate military", "Select heavy weapons specialists"],
    story_hooks: [
      "A DB-8 plasma bolt struck a residential building in Tier 3, melting through the exterior wall and killing a family inside. The intended target was in the building next door. Someone is using heavy weapons with unacceptable collateral risk.",
      "Zheng-Dao is developing a rapid-fire variant of the Dragon's Breath that eliminates the cooling cycle — if they succeed, it will be the most destructive man-portable weapon in Meridian 88."
    ],
    ammunition_type: ["Magnetically-contained plasma charge"],
    tags: ["weapon", "sniper", "plasma", "thermal", "anti-armor", "heavy", "tier 5"]
  },
  {
    id: id(),
    name: "Vanta Ordnance Nullifier VN-6 'Flatline'",
    type: "weapon",
    aliases: ["Flatline", "VN-6", "The Eraser", "Zero Line"],
    category: "sniper",
    description: "A coilgun sniper rifle that fires magnetically-stabilized ceramic projectiles specifically designed to defeat energy-dispersing armor systems. Standard kinetic rounds lose effectiveness against reactive armor that distributes impact energy across its surface. The Nullifier's ceramic penetrators shatter on contact, converting a single impact point into thousands of micro-fragments that overwhelm the armor's dispersal capacity.\n\nVanta Ordnance is a small firm founded by former Arcturus engineers who believed the defense giant was too conservative in its approach to armor defeat. The VN-6 represents their thesis — that the future of precision weapons lies not in greater velocity but in smarter projectile behavior.\n\nThe Nullifier has found a dedicated following among operators who frequently engage targets wearing next-generation defensive systems. Where a conventional round might be turned by reactive plating, the Flatline's fragmenting ceramic penetrator finds a way through.",
    manufacturer: "VANTA ORDNANCE",
    tier_availability: "Tier 4+",
    legality: "Licensed — corporate security and military contractors",
    base_technologies: ["Coilgun magnetic acceleration", "Fragmenting ceramic penetrator design", "Anti-reactive-armor ballistics"],
    specifications: "caliber: 5mm ceramic fragmenting penetrator\neffective_range: 200-1,800 meters\nrate_of_fire: Semi-automatic, 1 round per 4 seconds\ncapacity: 6-round sealed magazine\nweight: 7.8 kg\npower_source: Internal capacitor bank, 24 shots per charge",
    tactical_use: "The Flatline is deployed specifically against targets wearing advanced armor that defeats conventional precision weapons. Operators describe the engagement philosophy as 'cracking the egg' — the ceramic fragmenter's multi-point impact overwhelms armor that was designed to handle single-point kinetic strikes. The weapon is less effective against unarmored targets, as the fragmenting round disperses energy rather than concentrating it.",
    cultural_context: "Vanta Ordnance has cultivated a rebellious identity, positioning themselves as innovators challenging Arcturus's dominance. Their marketing materials feature the tagline 'What they defend against, we defeat.' The company's small size means Nullifier production is limited, and wait lists extend six months or more. Operators who carry a Flatline are making a statement about both their threat assessment and their professional network.",
    known_users: ["Vanta Ordnance demonstration team", "Independent armor-defeat specialists"],
    story_hooks: [
      "Arcturus has filed a corporate espionage claim against Vanta Ordnance, alleging that the Nullifier's coilgun technology was stolen from an abandoned Arcturus project. Vanta's founders say they developed it independently — the truth could determine the company's survival.",
      "A Flatline round defeated a new prototype armor system during a live demonstration, embarrassing the armor manufacturer in front of their biggest client. Someone at Vanta had inside information about the armor's dispersal frequency."
    ],
    ammunition_type: ["5mm ceramic fragmenting penetrator"],
    tags: ["weapon", "sniper", "coilgun", "anti-armor", "ceramic", "tier 4", "boutique"]
  },
  {
    id: id(),
    name: "Crucible Industries Sentinel CR-40 'Tombstone'",
    type: "weapon",
    aliases: ["Tombstone", "CR-40", "Grave Marker", "The Headstone"],
    category: "sniper",
    description: "A heavy conventional sniper rifle chambered in a proprietary 12.7mm round that Crucible designed specifically for maximum terminal effect against cyber-augmented targets. The round carries a micro-EMP charge that detonates on penetration, frying cyberware in a localized radius around the wound channel.\n\nThe Tombstone was born from battlefield intelligence suggesting that heavily augmented combatants could survive wounds that would kill an unaugmented human. Crucible's solution was elegant — don't just wound them, kill their augmentations simultaneously. A cyber-limb that goes dead becomes dead weight. A reflex booster that shorts out leaves its user slower than baseline. Ocular implants that fry blind their owner.\n\nThe CR-40 is a brutalist weapon — heavy, loud, and unsubtle. It makes no concessions to stealth or portability. It exists for one purpose: to put augmented targets in the ground and make sure their chrome dies with them.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and authorized corporate security",
    base_technologies: ["Anti-cyberware EMP payload delivery", "Heavy-caliber precision engineering", "Localized electromagnetic disruption"],
    specifications: "caliber: 12.7mm EMP-core\neffective_range: 200-2,000 meters\nrate_of_fire: Bolt-action, 1 round per 4 seconds\ncapacity: 5-round detachable magazine\nweight: 14.2 kg\npower_source: EMP charge is self-contained in each round",
    tactical_use: "The Tombstone is deployed against heavily augmented targets where conventional precision weapons might not achieve a clean kill. The micro-EMP detonation ensures that even a non-fatal hit will disable critical cyberware, leaving the target vulnerable to follow-up engagement. Corporate security teams facing augmented breach teams have adopted the CR-40 as their primary anti-personnel tool, often positioning a Tombstone operator to engage the most heavily modified threat first.",
    cultural_context: "Among the augmented communities of Meridian 88, the Tombstone is spoken of with particular dread. A round that kills your chrome is more than a weapon — it is an existential threat to people whose identity is bound up in their modifications. Anti-augmentation extremists have adopted the Tombstone as a symbol, and graffiti of the CR-40's distinctive bull-pup silhouette has appeared in augmented-friendly districts as a threat.",
    known_users: ["Crucible Industries anti-augmentation warfare division", "Corporate breach response teams"],
    story_hooks: [
      "A Tombstone round hit an augmented civilian in a crowded market — the micro-EMP disabled cyberware in everyone within a 5-meter radius. The weapon's designers swore the EMP effect was localized. Something has changed.",
      "An underground augmentation clinic has begun installing EMP-hardened implants specifically to counter the Tombstone threat. Crucible wants to know how they obtained the weapon's EMP frequency specifications."
    ],
    ammunition_type: ["12.7mm EMP-core"],
    tags: ["weapon", "sniper", "anti-cyberware", "EMP", "heavy", "tier 4", "augmentation"]
  },
  {
    id: id(),
    name: "Meridian Armory Collective Peacekeeper MAC-1 'Long Arm of the Law'",
    type: "weapon",
    aliases: ["Long Arm", "MAC-1", "Peacekeeper", "The Reach"],
    category: "sniper",
    description: "A precision rifle produced by the Meridian Armory Collective, a consortium of small manufacturers that pooled resources to produce a weapon system competitive with corporate offerings. The MAC-1 fires smart rounds equipped with micro-guidance fins that receive BCI corrections from the operator during flight.\n\nThe Peacekeeper was designed for municipal security forces that cannot afford Tessera or Arcturus platforms but still need precision engagement capability. The weapon is deliberately utilitarian — no luxury finishes, no proprietary components, and a modular design that allows field repair with standard tools. What it lacks in sophistication it compensates for with reliability and a price point that puts precision capability in the hands of community defense organizations.\n\nThe MAC-1's smart round guidance is less precise than military-grade systems, but within 1,500 meters it achieves hit probabilities that rival weapons costing ten times as much.",
    manufacturer: "MERIDIAN ARMORY COLLECTIVE",
    tier_availability: "Tier 3+",
    legality: "Licensed — municipal security and authorized civilians",
    base_technologies: ["Cooperative manufacturing consortium", "Budget smart-round guidance", "Modular field-serviceable design"],
    specifications: "caliber: 6.5mm smart round with micro-guidance fins\neffective_range: 100-1,500 meters\nrate_of_fire: Semi-automatic, 1 round per 2 seconds\ncapacity: 8-round detachable magazine\nweight: 5.2 kg\npower_source: BCI link powers guidance corrections, no external power",
    tactical_use: "The Peacekeeper fills the gap between unguided precision rifles and corporate smart-weapon platforms. Municipal security teams deploy MAC-1 operators as overwatch during community defense operations, providing precision support that previously required hiring corporate contractors. The weapon's BCI guidance is effective but less responsive than military systems — operators describe it as 'suggesting' rather than 'commanding' the round's trajectory.",
    cultural_context: "The Meridian Armory Collective represents a rare example of grassroots manufacturing competing with corporate defense production. The MAC-1 has become a symbol of community self-reliance in the middle tiers, and its affordability has democratized precision weapons capability. Corporate manufacturers view the MAC with disdain, but their sales teams have noticed the dent in their market share.",
    known_users: ["Municipal security forces", "Community defense organizations", "Tier 3 militia groups"],
    story_hooks: [
      "The Meridian Armory Collective's manufacturing facility was vandalized — precision tooling destroyed, inventory stolen. The damage pattern suggests corporate sabotage targeting a competitor that was getting too successful.",
      "A batch of MAC-1 smart rounds has been modified with a secondary guidance override — someone can redirect the rounds mid-flight to hit unintended targets. The modified rounds are already in circulation."
    ],
    ammunition_type: ["6.5mm BCI-guided smart round"],
    tags: ["weapon", "sniper", "smart-round", "community", "affordable", "tier 3", "BCI"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Horizon ADS-18 'Dawnbreak'",
    type: "weapon",
    aliases: ["Dawnbreak", "ADS-18", "Horizon", "First Light"],
    category: "sniper",
    description: "A next-generation precision railgun that incorporates atmospheric compensation sensors directly into the projectile. Each round carries a microprocessor that samples air density, wind, and temperature during flight and adjusts its trajectory using electromagnetic interaction with the Earth's magnetic field — no fins, no thrusters, just controlled electromagnetic drag.\n\nThe Horizon represents Arcturus's attempt to create a sniper system that cannot miss. At ranges under 3km, the ADS-18 achieves a first-round hit probability of 99.2% in controlled testing. The round's in-flight adjustment capability is subtle but cumulative — correcting for wind drift, Coriolis effect, and atmospheric density changes that would throw off even guided conventional rounds.\n\nThe technology is so advanced that Arcturus has classified the round's internal architecture. Spent rounds are designed to self-destruct on impact, liquefying their microprocessor into unrecoverable slag.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — classified weapons program",
    base_technologies: ["In-flight electromagnetic trajectory adjustment", "Atmospheric sampling microprocessor rounds", "Self-destructing classified ammunition"],
    specifications: "caliber: 3.5mm smart tungsten penetrator\neffective_range: 200-3,800 meters\nrate_of_fire: 1 round per 6 seconds\ncapacity: 6-round sealed magazine\nweight: 8.4 kg\npower_source: Integrated supercapacitor, 24 shots per charge",
    tactical_use: "The Dawnbreak is deployed when a first-round kill is the only acceptable outcome — hostage situations, high-value target elimination, and time-critical engagements where a miss triggers escalation. The weapon's atmospheric compensation eliminates the variables that traditionally separate good snipers from great ones. Arcturus markets the system as 'democratizing precision' — any trained operator can achieve master-class accuracy with the ADS-18.",
    cultural_context: "The Dawnbreak has created controversy within the precision shooting community. Traditionalists argue that it reduces marksmanship to button-pressing, while pragmatists counter that results matter more than method. Arcturus does not engage in this debate — they simply point to the hit probability statistics and let procurement officers draw their own conclusions.",
    known_users: ["Arcturus Tier 5 precision warfare division"],
    story_hooks: [
      "An ADS-18 round failed to self-destruct after impact, and an independent weapons analyst recovered the microprocessor intact. What they found inside suggests the round collects more data than atmospheric readings — it is mapping the electromagnetic environment of the target area.",
      "Arcturus has quietly recalled a batch of Dawnbreak ammunition after several rounds exhibited anomalous flight behavior — veering toward unintended targets. The microprocessor may have a flaw in its target-lock retention."
    ],
    ammunition_type: ["3.5mm atmospheric-compensating smart penetrator"],
    tags: ["weapon", "sniper", "railgun", "smart-round", "atmospheric", "tier 5", "classified"]
  },
  {
    id: id(),
    name: "Hollow Point Precision Requiem HP-4 'Dirge'",
    type: "weapon",
    aliases: ["Dirge", "HP-4", "Requiem", "Death Song"],
    category: "sniper",
    description: "A suppressed anti-personnel precision rifle that fires subsonic fragmenting rounds designed to produce maximum wound trauma while minimizing over-penetration risk. Hollow Point Precision is a small manufacturer founded by a former field medic who understood exactly how to make a bullet kill efficiently.\n\nThe HP-4 fires a 9.3mm polymer-jacketed round that expands to 22mm upon tissue contact, fragmenting into six pre-scored petals that travel along divergent wound channels. The round was engineered to deposit 100% of its kinetic energy within the target, producing catastrophic internal damage without exit wounds. This makes the Requiem ideal for urban environments where over-penetration endangers civilians.\n\nThe weapon's integral suppressor reduces its acoustic signature to 72 decibels — loud enough to be heard in an enclosed space, but easily lost in the ambient noise of Meridian 88's urban environment.",
    manufacturer: "HOLLOW POINT PRECISION",
    tier_availability: "Tier 3+",
    legality: "Licensed — security contractors; Restricted from military use (treaty provisions)",
    base_technologies: ["Pre-scored fragmenting projectile design", "Total energy deposition ballistics", "Integral precision suppression"],
    specifications: "caliber: 9.3mm polymer-jacketed fragmenting\neffective_range: 50-600 meters\nrate_of_fire: Semi-automatic, 1 round per 1.5 seconds\ncapacity: 10-round detachable magazine\nweight: 4.8 kg\npower_source: None — conventional mechanical action",
    tactical_use: "The Dirge is deployed in urban environments where over-penetration is unacceptable — hospitals, residential complexes, crowded commercial districts. Security contractors operating in civilian-dense areas favor the HP-4 because a miss strikes a wall and stops, rather than passing through into occupied space. The trade-off is reduced range and ineffectiveness against armored targets — the fragmenting round struggles with anything heavier than light composite vest.",
    cultural_context: "Hollow Point Precision's founder speaks openly about their design philosophy: a weapon that kills one person and only one person. In a city where stray rounds kill dozens of bystanders annually, the Requiem has been adopted by security firms that market themselves as 'responsible' operators. Critics point out that a weapon designed to maximize wound trauma is a strange vehicle for corporate responsibility.",
    known_users: ["Urban security contractors", "Hospital and medical facility security teams"],
    story_hooks: [
      "A Requiem round recovered from a crime scene shows modifications to the fragmentation pattern — the petals have been coated with a slow-acting cytotoxin that causes organ failure over days. Someone has turned a precision weapon into a delayed-action assassin's tool.",
      "Hollow Point Precision's founder has received death threats from a trauma surgeon who has seen too many Dirge wounds. The surgeon is building a case that the weapon constitutes a war crime under pre-collapse treaty law."
    ],
    ammunition_type: ["9.3mm polymer-jacketed fragmenting"],
    tags: ["weapon", "sniper", "suppressed", "fragmenting", "urban", "tier 3", "anti-personnel"]
  },
  {
    id: id(),
    name: "Sterling-Nakamura Clarity SN-11 'Glass Eye'",
    type: "weapon",
    aliases: ["Glass Eye", "SN-11", "Clarity", "The Lens"],
    category: "sniper",
    description: "A precision rifle built around its optics rather than its ballistics. The Clarity SN-11 integrates a multi-spectrum sensor suite that provides the operator with thermal, electromagnetic, and acoustic imaging overlaid onto a BCI-projected heads-up display. The weapon itself fires conventional 6.5mm rounds — it is the targeting system that justifies the price.\n\nSterling-Nakamura's approach inverts the traditional sniper weapons philosophy. Rather than building a better bullet, they built a better eye. The SN-11's sensor suite can detect targets through walls, identify cyberware signatures from 2km away, and predict target movement using pattern-recognition algorithms that learn from the operator's engagement history.\n\nThe Glass Eye has found a niche among intelligence-oriented operators who value the sensor data as much as the weapon's lethality. Some operators reportedly disable the weapon entirely and use the SN-11 purely as a surveillance platform.",
    manufacturer: "STERLING-NAKAMURA",
    tier_availability: "Tier 4+",
    legality: "Licensed — corporate and military intelligence operators",
    base_technologies: ["Multi-spectrum targeting integration", "BCI-projected combat heads-up display", "Predictive target movement algorithms"],
    specifications: "caliber: 6.5mm conventional match-grade\neffective_range: 100-1,600 meters (weapon), 3,000 meters (sensor suite)\nrate_of_fire: Semi-automatic, 1 round per 2 seconds\ncapacity: 10-round detachable magazine\nweight: 6.8 kg\npower_source: Sensor suite: integrated power cell, 8 hours continuous",
    tactical_use: "The Glass Eye is deployed in intelligence-gathering operations where the operator's primary mission is observation and the weapon is a contingency. The multi-spectrum sensor suite provides real-time intelligence that can be shared across a team's BCI network, making the SN-11 operator a force multiplier even without firing a shot. When engagement is necessary, the predictive targeting system eliminates the guesswork from moving-target engagement.",
    cultural_context: "Sterling-Nakamura has positioned the Glass Eye as a tool for professionals who value information over destruction. Their marketing emphasizes restraint and precision — a sharp contrast to Crucible's brutalist philosophy. In the intelligence community, carrying an SN-11 signals that you are in the business of knowing things, and killing is a secondary function.",
    known_users: ["Sterling-Nakamura corporate intelligence", "Select espionage contractors"],
    story_hooks: [
      "A Glass Eye operator has been selling the sensor data from their surveillance operations to competing factions — the weapon's recording capability was not supposed to be accessible to the user, but someone cracked the firmware.",
      "Sterling-Nakamura's predictive targeting algorithm has begun flagging individuals as threats before they take any hostile action. The algorithm is either reading intentions or making mistakes — and the consequences of either are disturbing."
    ],
    ammunition_type: ["6.5mm match-grade"],
    tags: ["weapon", "sniper", "intelligence", "sensor", "BCI", "surveillance", "tier 4"]
  },
  {
    id: id(),
    name: "Ossuary Arms Memento Mori OA-3 'Wake'",
    type: "weapon",
    aliases: ["Wake", "OA-3", "Memento Mori", "Bone Caller"],
    category: "sniper",
    description: "A bolt-action precision rifle built with a deliberately antiquated aesthetic — walnut stock, blued steel, hand-checkered grip — that conceals a modern BCI-integrated firing computer in its stock. Ossuary Arms builds weapons for operators who believe that the act of precision shooting is a craft, not a procedure.\n\nThe Wake's BCI integration is minimal and respectful. It provides wind data, range estimation, and a recommended hold point, but it does not guide the round or correct for shooter error. The operator must still execute the shot. Ossuary's founder has been quoted saying 'The machine tells you where to aim. Your hands decide if you are worthy.'\n\nThe Memento Mori fires a heavy 10.3mm match round at subsonic velocities through a hand-fitted barrel that achieves sub-MOA accuracy at 1,000 meters. Each rifle is engraved with the name of its first owner and cannot be re-registered.",
    manufacturer: "OSSUARY ARMS",
    tier_availability: "Tier 4+",
    legality: "Licensed — registered to named individual only",
    base_technologies: ["Hand-fitted precision barrel manufacturing", "Concealed BCI firing computer", "Legacy craft weaponsmithing"],
    specifications: "caliber: 10.3mm match-grade subsonic\neffective_range: 100-1,200 meters\nrate_of_fire: Bolt-action, 1 round per 5 seconds\ncapacity: 3-round internal magazine\nweight: 5.6 kg\npower_source: BCI computer: micro-cell, 200 hours standby",
    tactical_use: "The Wake is not a battlefield weapon. It is deployed by operators who select their engagements carefully and value precision over volume. The heavy subsonic round delivers devastating terminal performance while maintaining a suppressed acoustic signature. Operators who carry the Memento Mori tend to work alone, take single engagements, and disappear. The weapon's registered nature means that every Wake kill can theoretically be traced — operators accept this as part of the weapon's philosophy of accountability.",
    cultural_context: "Ossuary Arms has become a cult brand among precision shooters who reject the automation trend in modern weaponry. Owning a Wake is a statement about craftsmanship, accountability, and the belief that killing should require skill rather than technology. The engraving tradition means that a Wake carries its owner's name into every engagement — a permanent record of who chose to pull the trigger.",
    known_users: ["Named individual operators (registry sealed)", "Ossuary Arms demonstration shooters"],
    story_hooks: [
      "A Wake engraved with a dead operator's name has surfaced on the black market. Ossuary's policy prohibits re-registration, meaning whoever uses it carries a dead person's identity into every engagement.",
      "Ossuary Arms' founder has been diagnosed with a terminal illness and is building one final rifle — a masterwork Wake that they intend to auction. The anticipated bidding war has attracted attention from people who want the weapon for more than its craftsmanship."
    ],
    ammunition_type: ["10.3mm match-grade subsonic"],
    tags: ["weapon", "sniper", "craft", "precision", "BCI", "boutique", "tier 4", "legacy"]
  },
  {
    id: id(),
    name: "Tessera Neural Interdict Rifle NIR-2 'Migraine'",
    type: "weapon",
    aliases: ["Migraine", "NIR-2", "Brain Spike", "The Headache"],
    category: "sniper",
    description: "A precision weapon that fires a focused electromagnetic pulse shaped into a narrow beam, designed to disrupt neural implants and BCI systems at range. The NIR-2 does not fire a physical projectile — it delivers a shaped EMP pulse through a directional antenna disguised as a rifle barrel.\n\nThe Migraine's beam induces cascading failures in neural interface hardware, causing effects ranging from disorientation and seizures to permanent implant damage depending on exposure duration and target's augmentation level. Against unaugmented targets, the weapon produces a splitting headache and temporary cognitive impairment. Against someone running a full BCI suite, it can cause a neural crash that leaves them unconscious for hours.\n\nTessera developed the NIR-2 for non-lethal interdiction scenarios — disabling augmented targets without the collateral damage of kinetic weapons. In practice, the line between 'disabled' and 'brain-damaged' has proven difficult to control.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 5",
    legality: "Classified — non-lethal designation disputed",
    base_technologies: ["Directional shaped EMP generation", "Neural interface disruption targeting", "Non-kinetic precision engagement"],
    specifications: "caliber: N/A — shaped EMP beam\neffective_range: 50-500 meters\nrate_of_fire: 1 pulse per 8 seconds\ncapacity: 12 pulses per power cell\nweight: 6.2 kg\npower_source: Dedicated EMP power cell, field-replaceable",
    tactical_use: "The Migraine is deployed against augmented targets that need to be neutralized without kinetic engagement — hostage situations involving augmented hostage-takers, corporate espionage targets carrying sensitive neural data, and augmented individuals who might trigger deadman switches if killed. The weapon's non-lethal classification allows its use in scenarios where lethal force authorization has not been granted, though its potential for permanent brain damage makes that classification increasingly controversial.",
    cultural_context: "The NIR-2 has become a flashpoint in debates about augmented rights in Meridian 88. Disability advocates argue that a weapon specifically designed to attack neural implants constitutes targeted violence against augmented individuals. Tessera's defense — that the weapon is non-lethal and discriminate — rings hollow to anyone who has witnessed a neural crash firsthand. Underground augmentation communities have begun developing EMP-hardened implants specifically in response to the Migraine threat.",
    known_users: ["Tessera special operations", "Corporate hostage response teams"],
    story_hooks: [
      "A modified NIR-2 was used in a crowded transit station, inducing neural crashes in every augmented person within a 50-meter radius. The weapon was supposed to be focused on a single target. Either it was modified for area effect, or the operator deliberately chose to attack a crowd.",
      "A Tessera whistleblower has leaked documents showing that the NIR-2 was originally developed as a lethal weapon — the 'non-lethal' designation was a marketing decision, not an engineering one."
    ],
    ammunition_type: ["Shaped EMP pulse"],
    tags: ["weapon", "sniper", "EMP", "neural", "non-lethal", "anti-augmentation", "tier 5", "BCI"]
  },
  {
    id: id(),
    name: "Fenris Ballistics Howl FB-7 'Wolfpack'",
    type: "weapon",
    aliases: ["Wolfpack", "FB-7", "Howl", "The Pack"],
    category: "sniper",
    description: "A precision rifle that fires programmable micro-munitions capable of splitting into three independently-guided sub-projectiles after leaving the barrel. Each sub-projectile carries its own BCI-slaved guidance package and can engage a separate target within a 15-degree cone from the initial trajectory.\n\nFenris Ballistics designed the Howl for scenarios where a single sniper needs to engage multiple targets simultaneously — a capability previously requiring multiple shooters. The weapon's split-munition technology uses explosive bolt separation at a pre-programmed range, after which each sub-projectile follows its own BCI-designated target lock.\n\nThe Wolfpack's limitation is that the sub-projectiles carry significantly less kinetic energy than a full-size round. Against armored targets, the split munitions may fail to penetrate. Against unarmored or lightly armored personnel, three simultaneous hits from unexpected angles produce devastating psychological and physical effect.",
    manufacturer: "FENRIS BALLISTICS",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and corporate tactical teams",
    base_technologies: ["Programmable split-munition technology", "Multi-target BCI guidance slaving", "Explosive bolt sub-projectile separation"],
    specifications: "caliber: 8mm parent munition splitting to 3x 4mm sub-projectiles\neffective_range: 100-1,200 meters (split at 50-800m operator-selected)\nrate_of_fire: 1 round per 3 seconds\ncapacity: 6-round detachable magazine\nweight: 7.2 kg\npower_source: BCI guidance link, no external power required",
    tactical_use: "The Wolfpack excels in multi-target engagement scenarios — ambush interdiction, convoy disruption, and security detail neutralization. A single Howl operator can engage a three-person bodyguard detail simultaneously, eliminating the sequential engagement delay that gives targets time to react. The weapon is also used for guaranteed kills, with all three sub-projectiles directed at a single target for redundant lethality.",
    cultural_context: "Fenris Ballistics takes its name and branding from Norse predator mythology, and the Wolfpack embodies their philosophy of overwhelming the prey. The weapon has found particular favor with corporate extraction teams who need to neutralize protective details quickly and cleanly. Among close protection professionals, the Wolfpack is the weapon they fear most — three rounds from three angles, and no time to react.",
    known_users: ["Fenris Ballistics field teams", "Corporate extraction specialists"],
    story_hooks: [
      "A Wolfpack round split into its sub-projectiles and two of the three guidance packages locked onto unintended targets — bystanders who happened to match the biometric profile of the intended victims. Fenris claims this is impossible with properly calibrated BCI targeting.",
      "Someone has reverse-engineered the split-munition technology and is manufacturing bootleg copies with unreliable separation charges. Three operators have been killed when the parent munition detonated inside the barrel."
    ],
    ammunition_type: ["8mm programmable split-munition"],
    tags: ["weapon", "sniper", "multi-target", "guided", "split-munition", "tier 4", "BCI"]
  },
  {
    id: id(),
    name: "Talon Systems Perch TS-5 'Raptor'",
    type: "weapon",
    aliases: ["Raptor", "TS-5", "Perch", "Bird of Prey"],
    category: "sniper",
    description: "A lightweight precision carbine designed for aerial platform integration — drones, VTOL craft, and grav-assisted jump harnesses. The Raptor weighs just 3.1 kg and uses a gyrostabilized barrel assembly that maintains accuracy during the extreme vibration and angular displacement of aerial movement.\n\nTalon Systems recognized that the proliferation of personal flight systems in Meridian 88 created a need for precision weapons optimized for airborne engagement. Conventional sniper rifles are unwieldy in harness, their optics designed for stable ground positions. The TS-5 was built from the ground up for shooters who are falling, hovering, or banking through urban canyons.\n\nThe Raptor's BCI integration includes an inertial navigation system that calculates firing solutions based on the shooter's velocity, acceleration, and angular rate. It allows precision engagement during maneuvers that would make conventional aim impossible.",
    manufacturer: "TALON SYSTEMS",
    tier_availability: "Tier 4+",
    legality: "Licensed — aerial security and rapid response teams",
    base_technologies: ["Gyrostabilized aerial barrel assembly", "Inertial navigation firing solutions", "Lightweight aerial platform integration"],
    specifications: "caliber: 5.56mm match-grade\neffective_range: 50-800 meters (aerial), 100-1,200 meters (ground)\nrate_of_fire: Semi-automatic, 2 rounds per second\ncapacity: 20-round detachable magazine\nweight: 3.1 kg\npower_source: Gyrostabilizer: micro-cell, 6 hours continuous",
    tactical_use: "The Raptor is deployed by aerial rapid-response teams who need precision capability without the weight and bulk of conventional sniper platforms. Operators in grav-harnesses use the TS-5 to engage targets from angles that ground-based shooters cannot achieve — above, behind, and through vertical urban terrain. The weapon's lighter caliber is compensated by engagement geometry that allows shots into unarmored areas that horizontal shooters cannot access.",
    cultural_context: "Talon Systems has tapped into Meridian 88's growing aerial combat culture — a world where the vertical dimension of the city is as tactically relevant as the horizontal. Raptor operators call themselves 'roosters' and compete in informal accuracy competitions conducted from moving aerial platforms. The weapon has also found civilian following among recreational precision shooters who compete in aerial marksmanship courses.",
    known_users: ["Talon Systems aerial demonstration team", "Corporate aerial rapid-response units"],
    story_hooks: [
      "A Raptor operator engaged a target from a grav-harness during a rainstorm and the gyrostabilizer malfunctioned — the round struck a child in a residential unit three floors below the intended target. The operator turned themselves in. Their employer is trying to make them disappear.",
      "Talon Systems is developing a drone-mounted variant of the Raptor that removes the human operator entirely. The autonomous aerial sniper platform has attracted both military contracts and intense regulatory opposition."
    ],
    ammunition_type: ["5.56mm match-grade"],
    tags: ["weapon", "sniper", "aerial", "lightweight", "gyrostabilized", "tier 4", "BCI"]
  },
  {
    id: id(),
    name: "Grave Protocol Arms Terminus GPA-1 'Last Rites'",
    type: "weapon",
    aliases: ["Last Rites", "GPA-1", "Terminus", "The Closing Argument"],
    category: "sniper",
    description: "A single-shot break-action railgun designed for absolute maximum velocity from the shortest possible barrel. The Terminus fires a 2mm tungsten needle at Mach 12 from a 40cm barrel, achieving penetration values that exceed rifles twice its length. The weapon is designed for a single engagement philosophy — one shot, one kill, then disappear.\n\nGrave Protocol Arms is a manufacturer that exists only as a series of encrypted procurement channels. No facility has been identified, no employees named, and no corporate registration filed in any jurisdiction. The Terminus appears on the black market at irregular intervals, always in sealed cases with no documentation beyond a serial number etched into the receiver.\n\nForensic analysis of recovered Terminus weapons reveals manufacturing precision that exceeds any known commercial or military standard. Whoever builds these weapons has access to equipment that is not supposed to exist outside of Tier 5 fabrication facilities.",
    manufacturer: "GRAVE PROTOCOL ARMS",
    tier_availability: "Tier 5",
    legality: "Illegal — unregistered manufacturer, prohibited weapon class",
    base_technologies: ["Ultra-compact high-velocity railgun", "Unknown precision manufacturing", "Single-engagement weapon philosophy"],
    specifications: "caliber: 2mm tungsten needle\neffective_range: 100-2,000 meters\nrate_of_fire: Single-shot, 20-second manual reload and capacitor recharge\ncapacity: 1 round\nweight: 4.2 kg\npower_source: Integrated micro-capacitor, 1 shot per charge cycle",
    tactical_use: "The Terminus is the weapon of choice for operators who plan meticulously and execute once. Its single-shot nature forces a discipline that multi-round weapons do not — the shooter must commit to their one opportunity. The weapon's compact size allows it to be concealed in a briefcase or tool bag, deployed in seconds, and abandoned after use. Many operators treat the GPA-1 as disposable, purchasing a new unit for each engagement.",
    cultural_context: "The mystery surrounding Grave Protocol Arms has generated intense speculation in Meridian 88's weapons community. Theories range from a rogue Arcturus skunkworks to an AI-run fabrication facility. The Terminus has become almost mythical — a weapon so perfect and so anonymous that it represents the platonic ideal of an assassination tool. Operators who have used one describe the firing experience as 'transcendent' — the recoil is minimal, the sound is a sharp crack, and the target simply ceases to exist.",
    known_users: ["Unknown — all known operators deceased or vanished"],
    story_hooks: [
      "A Terminus serial number was cross-referenced against a partial encryption key found in a dead intelligence operative's implant. The number sequence suggests Grave Protocol Arms is using Tessera's classified serial format — implying a connection between the companies.",
      "Three Terminus weapons were found in a shipping container at the docks, still sealed. Whoever ordered them never collected. The cases have been sitting there for two years, and the dock authority wants them gone before someone else claims them."
    ],
    ammunition_type: ["2mm tungsten needle"],
    tags: ["weapon", "sniper", "railgun", "black-market", "assassination", "tier 5", "mystery"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Vigil ADS-22 'Nightwatch'",
    type: "weapon",
    aliases: ["Nightwatch", "ADS-22", "Vigil", "Owl Eyes"],
    category: "sniper",
    description: "A precision rifle optimized for nocturnal and low-visibility engagement, integrating a cryogenically-cooled infrared sensor array that provides the operator with thermal imaging resolution sufficient to identify facial features at 1,500 meters in total darkness. The ADS-22 fires tracer-less conventional rounds to avoid revealing the shooter's position.\n\nArcturus designed the Nightwatch for corporate perimeter defense during Meridian 88's artificial night cycles, when thermal contrast between human bodies and urban infrastructure reaches maximum. The weapon's sensor suite can distinguish between human body signatures, synthetic mimics, and thermal decoys — a capability developed after several incidents where corporate snipers wasted ammunition on drone-deployed heat sources.\n\nThe Vigil's BCI integration projects the thermal image directly into the operator's visual cortex, allowing them to maintain situational awareness without looking through an eyepiece. Operators describe the experience as seeing heat itself — a world rendered in temperature rather than light.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 4+",
    legality: "Licensed — corporate security and military night operations",
    base_technologies: ["Cryogenically-cooled high-resolution thermal imaging", "BCI-projected thermal vision", "Thermal signature discrimination AI"],
    specifications: "caliber: 7.62mm tracer-less match-grade\neffective_range: 100-1,800 meters\nrate_of_fire: Semi-automatic, 1 round per 2 seconds\ncapacity: 10-round detachable magazine\nweight: 7.4 kg\npower_source: Sensor suite: cryo-cell, 6 hours continuous imaging",
    tactical_use: "The Nightwatch dominates low-visibility engagements. While other precision weapons rely on ambient light amplification or laser illumination — both of which can be detected — the ADS-22's passive thermal system leaves no electromagnetic footprint. The shooter sees everything and reveals nothing. Corporate perimeter teams deploy Nightwatch operators during night cycles as the primary detection and engagement layer, trusting the thermal discrimination AI to filter genuine threats from environmental noise.",
    cultural_context: "Arcturus's night operations equipment has earned the company a reputation as the manufacturer of darkness. The Nightwatch embodies this identity — a weapon that turns the absence of light into an advantage. In Meridian 88's lower tiers, where artificial lighting is unreliable, the knowledge that Nightwatch operators might be watching creates a curfew more effective than any official mandate.",
    known_users: ["Arcturus corporate perimeter security", "Night operations specialists"],
    story_hooks: [
      "A Nightwatch operator reported seeing thermal signatures in a supposedly abandoned section of the lower tiers — hundreds of heat sources, all stationary, all at exactly the same temperature. When a reconnaissance team investigated, the section was empty. The thermal ghosts remain unexplained.",
      "Someone has developed a personal thermal masking system that defeats the ADS-22's discrimination AI. Arcturus is offering a bounty for a working sample."
    ],
    ammunition_type: ["7.62mm tracer-less match-grade"],
    tags: ["weapon", "sniper", "night-ops", "thermal", "BCI", "corporate", "tier 4", "surveillance"]
  }
];

// ─── MACHINE GUNS / SUPPORT WEAPONS (15) ───────────────────────────────

const supportWeapons = [
  {
    id: id(),
    name: "Crucible Industries Storm Platform CP-8 'Deluge'",
    type: "weapon",
    aliases: ["Deluge", "CP-8", "Storm", "The Downpour"],
    category: "support",
    description: "A man-portable squad support weapon that fires caseless 6mm rounds from a 200-round helical magazine at 1,400 rounds per minute. The Storm Platform is Crucible's mainline suppression tool — designed to pin entire squads behind cover while assault elements maneuver.\n\nThe CP-8 uses a rotating barrel cluster of three barrels that cycle to manage heat distribution, allowing sustained fire for 8-second bursts without thermal degradation. The weapon feeds from a drum-style helical magazine worn on the operator's back, connected by an armored feed chute that allows full range of motion.\n\nCrucible designed the Deluge to be the loudest thing in any engagement. The weapon's acoustic signature at full cyclic rate is physically disorienting to personnel within 30 meters of the firing position — a deliberate feature that Crucible calls 'ambient suppression.' You don't need to hit someone to suppress them when the sound alone triggers a primal fear response.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Military restricted — squad support weapon authorization",
    base_technologies: ["High-cyclic rotating barrel cluster", "Helical magazine feed system", "Acoustic suppression engineering"],
    specifications: "caliber: 6mm caseless\neffective_range: 50-800 meters\nrate_of_fire: 1,400 rounds per minute (cyclic)\ncapacity: 200-round helical drum\nweight: 11.2 kg weapon, 4.8 kg loaded magazine\npower_source: None — mechanical action",
    tactical_use: "The Deluge is deployed as the centerpiece of squad-level fire-and-maneuver tactics. The weapon's extreme rate of fire and acoustic signature fix enemy positions, creating windows for assault teams to close distance. Operators learn to fire in controlled bursts that maintain the psychological pressure of sustained fire while conserving ammunition. In urban environments, the CP-8's penetration characteristics allow suppressive fire through light cover, denying concealment as well as movement.",
    cultural_context: "The sound of a Deluge is one of the most recognized auditory signatures in Meridian 88's conflict zones. Lower-tier residents who live near corporate borders describe it as 'the rain' — a hammering roar that signals active combat and the need to shelter. Crucible's marketing leans into the weather metaphor, with promotional materials declaring 'When it rains, nothing grows.'",
    known_users: ["Crucible Industries assault squads", "Corporate rapid-reaction teams"],
    story_hooks: [
      "A Deluge was recovered from a Tier 2 gang hideout — a weapon that should be impossible to obtain outside military channels. The serial number traces to a shipment that was reported destroyed in transit.",
      "Crucible is testing a CP-8 variant with smart ammunition that adjusts trajectory mid-flight to avoid designated 'no-strike' zones — a response to mounting civilian casualty concerns."
    ],
    ammunition_type: ["6mm caseless"],
    tags: ["weapon", "support", "machine-gun", "suppression", "high-cyclic", "tier 4"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Aegis Emplacement AE-6 'Bulwark'",
    type: "weapon",
    aliases: ["Bulwark", "AE-6", "Aegis", "The Wall"],
    category: "support",
    description: "A heavy emplaced weapon system designed for fixed-position defense. The Bulwark fires 10mm tungsten-core rounds from a dual-barrel configuration at 800 rounds per minute, with an integrated shield generator that projects a localized kinetic barrier in front of the firing position.\n\nThe AE-6 weighs 85 kg and requires mounting on a prepared position — rooftop, vehicle, or reinforced tripod. Once emplaced, the weapon's shield generator creates a 2-meter wide energy barrier that deflects small-arms fire while allowing the operator to engage threats from behind cover that did not exist before deployment.\n\nArcturus designed the Bulwark for corporate checkpoint defense and corridor denial. The combination of sustained heavy fire and projected shielding makes a single AE-6 position functionally equivalent to a fortified bunker — deployable in under three minutes and relocatable as tactical needs change.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — emplaced weapons authorization required",
    base_technologies: ["Dual-barrel heavy automatic fire", "Integrated kinetic barrier projection", "Rapid-deploy emplacement system"],
    specifications: "caliber: 10mm tungsten-core\neffective_range: 100-1,200 meters\nrate_of_fire: 800 rounds per minute (dual barrel combined)\ncapacity: 400-round linked belt\nweight: 85 kg (weapon and shield generator)\npower_source: Shield: portable fusion cell, 2 hours continuous projection",
    tactical_use: "The Bulwark transforms any position into a hardened defensive point. Corporate security teams deploy AE-6 units at choke points during building lockdowns, perimeter breaches, and territorial disputes. The shield generator's kinetic barrier stops conventional small-arms fire but is transparent to directed energy weapons — a known vulnerability that attackers exploit by mixing kinetic and energy weapons in their assault elements.",
    cultural_context: "The sight of a Bulwark being deployed signals that a corporate entity has decided to hold ground at any cost. In Meridian 88's ongoing territorial disputes, the AE-6 has become synonymous with escalation — its presence means negotiations have failed and the corporation is prepared for siege warfare. Lower-tier communities caught between competing Bulwark positions have learned to evacuate when they see the distinctive shield shimmer.",
    known_users: ["Arcturus corporate defense forces", "High-tier security installations"],
    story_hooks: [
      "A Bulwark position was overrun when its shield generator was defeated by a weapon no one can identify — the barrier simply vanished. Arcturus is offering a reward for information about the counter-shield technology.",
      "Someone is deploying stolen Bulwark units in the lower tiers, creating fortified positions that municipal security cannot breach. A community has declared independence behind a wall of Arcturus hardware."
    ],
    ammunition_type: ["10mm tungsten-core linked belt"],
    tags: ["weapon", "support", "emplaced", "heavy", "shield", "defensive", "tier 5", "corporate"]
  },
  {
    id: id(),
    name: "Zheng-Dao Heavy Industries Dragon Gate DG-4 'Gatekeeper'",
    type: "weapon",
    aliases: ["Gatekeeper", "DG-4", "Dragon Gate", "The Turnstile"],
    category: "support",
    description: "A vehicle-mounted rotary railgun that fires 4mm tungsten rounds at 2,000 rounds per minute. The Dragon Gate is designed for mounting on armored personnel carriers, ground-effect vehicles, and defensive turrets, providing devastating sustained fire against both personnel and light armor.\n\nZheng-Dao's engineering philosophy for the DG-4 prioritized sustained fire capability over peak performance. The weapon's six-barrel rotary system distributes heat and electromagnetic stress evenly, allowing a continuous 30-second burst that fires 1,000 rounds before requiring a 10-second cooling cycle. The electromagnetic launch system eliminates the need for propellant, and the 4mm tungsten rounds are compact enough to carry in quantities that make ammunition conservation unnecessary.\n\nThe Gatekeeper has become the standard suppression system for corporate armored columns in Meridian 88. When a DG-4 opens fire, the concentrated stream of hypervelocity tungsten transforms cover into concealment and concealment into nothing.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — vehicle-mounted weapons authorization",
    base_technologies: ["Six-barrel rotary electromagnetic acceleration", "Sustained hypervelocity fire management", "Vehicle power integration"],
    specifications: "caliber: 4mm tungsten\neffective_range: 200-1,500 meters\nrate_of_fire: 2,000 rounds per minute\ncapacity: 4,000-round hopper\nweight: 120 kg (weapon), 180 kg (ammunition hopper)\npower_source: Vehicle power bus, draws 200kW during sustained fire",
    tactical_use: "The Gatekeeper is deployed as a vehicle weapon system for convoy escort, perimeter defense, and area denial. Its extreme rate of fire creates a physical wall of tungsten that nothing personnel-portable can survive. Vehicle commanders use the DG-4 to establish fire corridors during movement, sweeping potential ambush positions with sustained bursts that deter or destroy threats before they can engage. Against structures, the concentrated fire can breach reinforced walls within seconds.",
    cultural_context: "The DG-4's distinctive sound — a high-pitched electromagnetic whine followed by the tearing-fabric noise of hypervelocity impacts — has become one of Meridian 88's most feared sounds. Ground-level communities near corporate transit corridors have developed informal warning systems based on the sound of DG-4 fire. The weapon is so destructive that its deployment in residential areas has been condemned by humanitarian organizations that Zheng-Dao's legal team routinely ignores.",
    known_users: ["Zheng-Dao corporate military", "ARCTURUS DEFENSE SOLUTIONS armored divisions"],
    story_hooks: [
      "A DG-4 was ripped from a destroyed APC and jury-rigged for ground emplacement by a Tier 2 militia. They lack the power supply to run it at full rate, but even at quarter speed it has created an impassable killzone that corporate security cannot approach.",
      "Zheng-Dao is developing a man-portable version of the Dragon Gate — a back-mounted system that reduces the rate of fire but allows a single operator to carry the destructive potential of a vehicle weapon. Field trials have resulted in three operator fatalities from power system failures."
    ],
    ammunition_type: ["4mm tungsten"],
    tags: ["weapon", "support", "vehicle-mounted", "rotary", "railgun", "heavy", "tier 5"]
  },
  {
    id: id(),
    name: "Tessera Suppression Lattice SL-3 'Quilt'",
    type: "weapon",
    aliases: ["Quilt", "SL-3", "Lattice", "The Blanket"],
    category: "support",
    description: "A networked smart-weapon system consisting of four autonomous turrets that operate as a coordinated fire group. Each turret fires 5.56mm caseless rounds and is equipped with a sensor suite that shares target data across the network. The Suppression Lattice creates interlocking fields of fire that can deny access to an area up to 400 meters in diameter.\n\nTessera designed the SL-3 for corporate facility defense where manpower is expensive and perimeter coverage is critical. A single operator with a BCI link can manage a four-turret Lattice, designating priority targets and engagement zones while the turrets' AI handles tracking and fire control. The system can also operate autonomously using pre-programmed engagement protocols.\n\nEach turret weighs 18 kg and can be deployed in under two minutes. The four-unit system uses encrypted mesh networking that maintains coordination even if individual turrets are destroyed or communication is jammed — the remaining units automatically redistribute their fire coverage to compensate for gaps.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 5",
    legality: "Corporate sovereign territory only — autonomous weapons restriction",
    base_technologies: ["Networked autonomous turret coordination", "Mesh-networked fire control", "AI-managed interlocking fire zones"],
    specifications: "caliber: 5.56mm caseless per turret\neffective_range: 50-600 meters per turret\nrate_of_fire: 600 rounds per minute per turret\ncapacity: 500 rounds per turret\nweight: 18 kg per turret (72 kg total system)\npower_source: Individual turret power cells, 48 hours standby / 20 minutes sustained fire",
    tactical_use: "The Quilt is deployed as an area denial system that multiplies a small security force's coverage capability. A four-turret Lattice controlled by a single BCI-linked operator can secure a perimeter that would traditionally require a dozen personnel with conventional weapons. The AI fire control ensures efficient ammunition distribution — the system prioritizes threats and avoids wasting rounds on suppressed or neutralized targets. The turrets' cross-networked sensors make flanking or concealment extremely difficult within the Lattice's engagement zone.",
    cultural_context: "The Suppression Lattice represents Tessera's vision of automated warfare — minimal human involvement, maximum coverage, algorithmic efficiency. Labor organizations have criticized the system as a tool for replacing human security workers with autonomous killing machines. Tessera's response is that the SL-3 protects human security personnel by reducing their exposure to danger. Both statements are true, and neither addresses the growing discomfort with AI-controlled weapons making lethal decisions.",
    known_users: ["Tessera corporate facility security", "High-tier research installations"],
    story_hooks: [
      "A Suppression Lattice was deployed in a residential district after a corporate facility expanded its sovereign territory boundary. The turrets' engagement zone now covers a public thoroughfare, and three pedestrians have been killed for walking too close to the new perimeter.",
      "Someone has hacked a Lattice's mesh network and reprogrammed it to protect a territory that Tessera does not own. The turrets are defending a squatter community, and Tessera cannot remotely shut them down."
    ],
    ammunition_type: ["5.56mm caseless"],
    tags: ["weapon", "support", "autonomous", "networked", "AI", "turret", "tier 5", "area-denial"]
  },
  {
    id: id(),
    name: "Carrion Defense Works Harvester CDW-12 'Thresher'",
    type: "weapon",
    aliases: ["Thresher", "CDW-12", "Harvester", "Meat Grinder"],
    category: "support",
    description: "A man-portable flechette support weapon that fires dense clouds of 2mm steel flechettes at a cyclic rate sufficient to saturate a 10-meter wide corridor with lethal projectiles. The Harvester uses a multi-stage electromagnetic accelerator fed from a backpack-mounted flechette hopper containing 8,000 rounds.\n\nCarrion Defense Works designed the Thresher for corridor clearing operations — the close-quarters nightmare scenarios inside Meridian 88's dense urban superstructures where conventional support weapons are too slow to track fast-moving threats in tight spaces. The CDW-12's flechette cloud does not require precision aiming; the operator simply points the weapon in the direction of the threat and lets the statistical spread ensure hits.\n\nThe Thresher's lethality comes from volume rather than individual round energy. A single flechette might not penetrate heavy armor, but a cloud of 200 flechettes fired in a half-second burst will find every gap, seam, and joint in any protective system. Medical teams recovering bodies from Thresher engagements describe the results as 'difficult to identify.'",
    manufacturer: "CARRION DEFENSE WORKS",
    tier_availability: "Tier 4+",
    legality: "Military restricted — antipersonnel area weapons authorization",
    base_technologies: ["Electromagnetic flechette cloud projection", "High-capacity backpack feed system", "Statistical saturation targeting"],
    specifications: "caliber: 2mm steel flechette\neffective_range: 10-100 meters\nrate_of_fire: 4,000 flechettes per minute\ncapacity: 8,000-round backpack hopper\nweight: 6.8 kg weapon, 12 kg loaded hopper\npower_source: Backpack capacitor, 2,000 flechettes per charge cycle",
    tactical_use: "The Thresher is deployed in close-quarters environments where precision is less important than total coverage. Corridor clearing, room breaching, and tunnel operations are its primary applications. The weapon's flechette spread pattern can be adjusted from a tight 5-degree cone to a wide 30-degree fan depending on the engagement environment. Operators describe using the CDW-12 as 'painting' a space with steel — every surface within the engagement zone receives multiple impacts.",
    cultural_context: "Carrion Defense Works has earned a reputation for weapons that prioritize lethality over precision, and the Harvester is their most extreme expression of this philosophy. Humanitarian organizations have repeatedly called for a ban on flechette cloud weapons, citing the indiscriminate nature of area-saturation fire. Carrion's legal defense argues that the weapon reduces collateral damage by confining lethality to a defined engagement zone — a claim that requires a very specific definition of 'collateral.'",
    known_users: ["Carrion Defense Works assault teams", "Corporate breach and clearing units"],
    story_hooks: [
      "A Thresher was used in a residential corridor dispute, saturating a section of Tier 2 housing with flechettes. Fourteen people died, including six who were in adjacent rooms when the flechettes penetrated thin interior walls. The operator claims they were told the area was evacuated.",
      "Carrion is developing a flechette variant that carries a micro-payload of incapacitating agent on each dart — turning the Thresher from a lethal weapon into a theoretically non-lethal area denial tool. Field tests suggest the dosage calculation needs work."
    ],
    ammunition_type: ["2mm steel flechette"],
    tags: ["weapon", "support", "flechette", "area-denial", "close-quarters", "tier 4"]
  },
  {
    id: id(),
    name: "Vespid Dynamics Hive Projector HP-6 'Swarmkeeper'",
    type: "weapon",
    aliases: ["Swarmkeeper", "HP-6", "Hive Projector", "Bug Zapper"],
    category: "support",
    description: "A support weapon that launches salvos of micro-drones carrying explosive charges. Each salvo releases 12 autonomous micro-drones that use swarm intelligence to pursue designated targets around corners, through windows, and into enclosed spaces that conventional projectiles cannot reach.\n\nVespid Dynamics built the Swarmkeeper to solve the problem of entrenched defenders in complex urban environments. Rather than firing directly at a target, the HP-6 operator launches drone salvos that navigate independently to their targets, using shared sensor data to coordinate approach vectors and overwhelm point-defense systems.\n\nEach micro-drone carries a 5-gram shaped charge capable of penetrating light armor or producing lethal fragmentation. Individually, they are a nuisance. As a synchronized salvo of 12, they are devastating — striking from multiple angles simultaneously with shaped charges that focus their energy inward toward a convergence point.",
    manufacturer: "VESPID DYNAMICS",
    tier_availability: "Tier 4+",
    legality: "Military restricted — autonomous munitions authorization",
    base_technologies: ["Swarm intelligence micro-drone munitions", "Autonomous multi-vector target pursuit", "Synchronized shaped charge convergence"],
    specifications: "caliber: N/A — micro-drone launch system\neffective_range: 20-400 meters (drone range from launch point)\nrate_of_fire: 1 salvo of 12 drones per 6 seconds\ncapacity: 5 salvos (60 micro-drones)\nweight: 9.4 kg launcher, 6.2 kg drone magazine\npower_source: Drones: self-powered, 90-second flight time per drone",
    tactical_use: "The Swarmkeeper eliminates the concept of hard cover. Targets behind walls, inside rooms, or around corners are reachable by micro-drones that navigate around obstacles. Operators use the HP-6 to flush entrenched defenders from positions that would require explosive breaching with conventional weapons. The psychological effect of hearing incoming micro-drones is significant — the distinctive high-pitched whine announces that something is coming that cannot be stopped by hiding.",
    cultural_context: "Vespid's drone weapons have earned them the nickname 'the hive' in Meridian 88's military community. The Swarmkeeper embodies the company's philosophy that individual lethality matters less than coordinated system behavior. Among defenders, the HP-6 has created a new category of combat stress — the knowledge that walls and cover are no longer protection, that something small and persistent can find you anywhere.",
    known_users: ["Vespid Dynamics assault integration teams", "Corporate urban warfare specialists"],
    story_hooks: [
      "A Swarmkeeper salvo was intercepted by an unknown electronic countermeasure that turned all 12 drones around and sent them back to the operator's position. Vespid is trying to determine how their encrypted control protocols were compromised.",
      "Micro-drones from an HP-6 salvo have been found in a residential area — armed but unexploded, their 90-second flight time expired before reaching their target. Children are finding them and treating them as toys."
    ],
    ammunition_type: ["Autonomous micro-drone salvo"],
    tags: ["weapon", "support", "drone", "swarm", "autonomous", "area-denial", "tier 4"]
  },
  {
    id: id(),
    name: "Sterling-Nakamura Cascade Projector SN-7 'Monsoon'",
    type: "weapon",
    aliases: ["Monsoon", "SN-7", "Cascade", "Flash Flood"],
    category: "support",
    description: "A man-portable electromagnetic suppression system that fires rapid bursts of magnetically-accelerated ceramic slugs. The Monsoon trades the sustained fire capability of conventional machine guns for overwhelming burst density — each trigger pull releases a 40-round burst in 0.3 seconds, creating an instantaneous wall of projectiles.\n\nSterling-Nakamura designed the Cascade Projector for ambush and counter-ambush scenarios where the first seconds of contact determine the outcome. The SN-7's burst capability allows a single operator to deliver the firepower equivalent of a full squad's opening volley. The ceramic slugs fragment on impact with hard surfaces, producing secondary projectile effects that extend the weapon's lethality beyond its direct fire pattern.\n\nThe trade-off is ammunition consumption. A 400-round magazine provides only 10 trigger pulls, and the weapon's electromagnetic accelerator requires a 2-second recharge between bursts. Monsoon operators learn to make each burst count.",
    manufacturer: "STERLING-NAKAMURA",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and corporate assault units",
    base_technologies: ["Ultra-rapid burst electromagnetic acceleration", "Ceramic slug fragmentation ballistics", "Instantaneous fire density optimization"],
    specifications: "caliber: 6mm ceramic slug\neffective_range: 30-400 meters\nrate_of_fire: 40-round burst per 2.3 seconds (including recharge)\ncapacity: 400-round detachable magazine\nweight: 8.6 kg weapon, 5.2 kg loaded magazine\npower_source: Integrated capacitor bank, recharges from magazine power cell",
    tactical_use: "The Monsoon is deployed for maximum immediate effect in contact situations. Its burst capability is devastating in ambush initiation — a single operator can suppress or destroy an entire fire team before they can react. In defensive situations, the weapon's burst density creates an impenetrable wall of ceramic fragments that stops charges cold. Operators are trained to conserve bursts and use the 2-second recharge interval to assess and reposition.",
    cultural_context: "Sterling-Nakamura markets the Monsoon as 'the first word and the last word' — a weapon designed for decisive moments rather than sustained engagements. The weapon has found particular favor with executive protection details who need maximum firepower in minimum time. A bodyguard with a concealed Monsoon can deliver a devastating counterattack that buys their principal the seconds needed to escape.",
    known_users: ["Sterling-Nakamura security division", "Executive protection specialists"],
    story_hooks: [
      "A Monsoon was used in a crowded marketplace to eliminate a single target — the 40-round burst killed the target and eleven bystanders. The operator claims the engagement was authorized. The authorization documents have disappeared.",
      "Sterling-Nakamura is developing a Monsoon variant that fires guided micro-munitions instead of ceramic slugs — each round in the burst tracking independently. The prototype has been stolen from their testing facility."
    ],
    ammunition_type: ["6mm ceramic slug"],
    tags: ["weapon", "support", "burst", "electromagnetic", "suppression", "tier 4"]
  },
  {
    id: id(),
    name: "Iron Meridian Cooperative Backbone IMC-3 'Spine'",
    type: "weapon",
    aliases: ["Spine", "IMC-3", "Backbone", "The Column"],
    category: "support",
    description: "A squad support weapon manufactured by the Iron Meridian Cooperative, a worker-owned defense manufacturer that produces equipment for community defense organizations and municipal security forces. The Backbone is a conventional belt-fed 7.62mm machine gun built for reliability, repairability, and sustained fire with minimal maintenance.\n\nThe IMC-3 contains no smart electronics, no BCI integration, and no electromagnetic acceleration. It is a mechanically-operated weapon that uses conventional chemical propellant ammunition and a gas-piston action refined over decades. Every component is designed for field repair with hand tools, and the weapon's open-bolt design runs cool enough for sustained fire without barrel changes.\n\nThe Backbone represents a philosophical counterpoint to the high-technology weapons that dominate corporate arsenals. It does not guide rounds, project shields, or coordinate with networked systems. It simply fires bullets, reliably and continuously, for as long as the operator feeds it ammunition.",
    manufacturer: "IRON MERIDIAN COOPERATIVE",
    tier_availability: "Tier 2+",
    legality: "Licensed — community defense and municipal security",
    base_technologies: ["Conventional gas-piston automatic action", "Field-repairable modular construction", "Open-bolt thermal management"],
    specifications: "caliber: 7.62mm conventional\neffective_range: 100-1,000 meters\nrate_of_fire: 700 rounds per minute\ncapacity: 200-round linked belt\nweight: 10.4 kg\npower_source: None — conventional chemical propellant",
    tactical_use: "The Backbone is deployed by organizations that cannot afford or do not trust high-technology weapons. Its reliability in adverse conditions — dust, water, impact damage — makes it the preferred support weapon for lower-tier defense forces operating in environments where delicate electronics fail. The IMC-3 does not require power cells, BCI links, or proprietary ammunition, and its conventional 7.62mm round is manufactured by dozens of producers across Meridian 88.",
    cultural_context: "The Iron Meridian Cooperative has become a symbol of armed self-determination for communities that refuse corporate dependence. The Backbone embodies their belief that defense should be accessible, maintainable, and free from corporate control. Owning an IMC-3 is a political statement — it says you trust steel and springs more than algorithms and capacitors. Corporate security professionals view the Backbone with condescension, but they also respect its kill count.",
    known_users: ["Municipal security forces", "Community defense cooperatives", "Tier 2-3 militia organizations"],
    story_hooks: [
      "The Iron Meridian Cooperative's foundry has been targeted by corporate saboteurs twice in the past month. Someone with significant resources wants to stop the production of affordable defense equipment for the lower tiers.",
      "A batch of IMC-3s was intercepted before delivery to a community defense organization. The weapons were replaced with externally identical replicas containing concealed tracking devices and remote-activated disable charges. The community only discovered the swap when one weapon refused to fire during an attack."
    ],
    ammunition_type: ["7.62mm conventional linked belt"],
    tags: ["weapon", "support", "machine-gun", "conventional", "reliable", "community", "tier 2"]
  },
  {
    id: id(),
    name: "Crucible Industries Furnace CF-15 'Meltdown'",
    type: "weapon",
    aliases: ["Meltdown", "CF-15", "Furnace", "The Smelter"],
    category: "support",
    description: "A vehicle-mounted continuous-beam thermal weapon that projects a 30kW infrared laser capable of cutting through armored vehicles, structural steel, and reinforced concrete. The Furnace is not a precision weapon — it is a demolition tool mounted on a weapon platform.\n\nCrucible designed the CF-15 for breaching operations where explosive charges are impractical or insufficient. The continuous beam can cut a human-sized opening in a reinforced concrete wall in under 8 seconds, and its thermal effect on personnel caught in the beam path is instantaneous and catastrophic. The weapon requires vehicle-grade power supply and cooling systems, making it unsuitable for dismounted operation.\n\nThe Meltdown has earned its nickname from several incidents where the weapon's beam was sustained longer than intended, heating structural elements to the point of failure. Two building collapses in Meridian 88 have been attributed to CF-15 engagements where the operator failed to account for the thermal load on load-bearing structures.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — heavy directed energy authorization",
    base_technologies: ["High-power continuous infrared beam generation", "Vehicle-integrated cooling systems", "Thermal cutting beam focus control"],
    specifications: "caliber: N/A — continuous infrared beam\neffective_range: 50-500 meters\nrate_of_fire: Continuous beam, adjustable duration\ncapacity: N/A — limited by power supply\nweight: 340 kg (weapon and cooling system)\npower_source: Vehicle fusion reactor, draws 60kW sustained",
    tactical_use: "The Furnace is deployed for deliberate breaching of hardened positions, destruction of armored vehicles, and denial of reinforced structures. The weapon's continuous beam allows the operator to trace cutting paths through material, effectively using the laser as a thermal saw. In anti-vehicle roles, the CF-15 can disable an armored vehicle by cutting through its engine compartment or crew cabin in a sustained 3-second exposure.",
    cultural_context: "The Meltdown's destructive power has made it a symbol of corporate excess — a weapon so powerful it can accidentally collapse buildings. Environmental activists point to the CF-15 as evidence that corporate military technology has exceeded any reasonable definition of proportional force. Crucible's defense is that the weapon is 'precisely destructive' — it destroys only what the operator aims at. The building collapses, they argue, were operator error.",
    known_users: ["Crucible Industries heavy weapons division", "Corporate siege warfare units"],
    story_hooks: [
      "A CF-15 was used to cut through the wall of a bank vault during what appears to be a corporate-sponsored heist. The thermal signature was detected by satellite, and now three different intelligence agencies want to know which corporation sanctioned the operation.",
      "Crucible has developed a miniaturized Furnace variant that can be backpack-mounted, reducing the beam power to 8kW but making it man-portable. The prototype is missing from their testing facility."
    ],
    ammunition_type: ["Continuous infrared beam"],
    tags: ["weapon", "support", "vehicle-mounted", "directed-energy", "thermal", "breaching", "tier 5"]
  },
  {
    id: id(),
    name: "Vanta Ordnance Phalanx VO-8 'Centurion'",
    type: "weapon",
    aliases: ["Centurion", "VO-8", "Phalanx", "Shield Wall"],
    category: "support",
    description: "A dual-purpose support weapon that alternates between kinetic suppression fire and a projected kinetic barrier. The Phalanx uses a single electromagnetic accelerator that can either fire 5mm tungsten rounds at 600 RPM or redirect its magnetic field to generate a localized shield that deflects incoming small-arms fire.\n\nVanta Ordnance designed the Centurion for small-unit operations where a team cannot carry both a support weapon and a shield generator. The VO-8 allows a single operator to switch between offensive and defensive roles in under half a second, providing suppressive fire during movement and shield coverage during pauses.\n\nThe trade-off is that the weapon cannot fire and shield simultaneously — the electromagnetic system does one or the other. Operators describe using the Centurion as a rhythm: fire, shield, advance, fire, shield, advance. Skilled operators develop an intuitive sense for when to switch modes, creating a pulsing offensive/defensive cadence that is difficult for opponents to exploit.",
    manufacturer: "VANTA ORDNANCE",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and corporate tactical units",
    base_technologies: ["Dual-mode electromagnetic field generation", "Rapid offensive/defensive mode switching", "Projected kinetic barrier field shaping"],
    specifications: "caliber: 5mm tungsten (fire mode)\neffective_range: 50-600 meters (fire mode), 3-meter radius (shield mode)\nrate_of_fire: 600 rounds per minute (fire mode)\ncapacity: 300-round magazine\nweight: 13.8 kg\npower_source: Dual-mode capacitor bank, shared between fire and shield functions",
    tactical_use: "The Centurion is deployed in small-unit assaults where the team needs both fire support and mobile cover. The weapon's operator becomes the tactical fulcrum of the team — suppressing threats during movement phases and providing cover during consolidation phases. In corridor and room-clearing operations, the Phalanx operator typically leads, projecting a shield for the team's approach and switching to fire mode once the entry point is reached.",
    cultural_context: "Vanta Ordnance's dual-mode philosophy has attracted operators who view themselves as protectors rather than pure combatants. Centurion operators often come from defensive security backgrounds and take pride in the weapon's shield capability as much as its lethality. In the contractor community, Phalanx operators are respected for their tactical judgment — the decision of when to fire and when to shield determines whether their team lives or dies.",
    known_users: ["Vanta Ordnance tactical teams", "Corporate close-protection squads"],
    story_hooks: [
      "A Centurion operator discovered that the shield mode generates a resonance frequency that disrupts nearby electronic systems — an unintended EMP side effect that Vanta is trying to keep quiet before it becomes a selling point.",
      "Two Phalanx operators faced each other in a corridor — one shielding, one firing, then switching. The engagement lasted eleven minutes before both ran out of power simultaneously. Neither side is willing to discuss what happened next."
    ],
    ammunition_type: ["5mm tungsten"],
    tags: ["weapon", "support", "dual-mode", "shield", "electromagnetic", "tier 4"]
  },
  {
    id: id(),
    name: "Fenris Ballistics Packmaster FB-12 'Alpha'",
    type: "weapon",
    aliases: ["Alpha", "FB-12", "Packmaster", "Lead Dog"],
    category: "support",
    description: "A BCI-networked support weapon system designed to be operated in synchronized pairs or groups. When two or more Packmasters are linked through their operators' BCI connections, the weapons coordinate their fire patterns automatically — one suppresses while the other repositions, creating continuous overlapping fire without communication delay.\n\nFenris Ballistics designed the Alpha around the concept of pack hunting — multiple weapons acting as a single predatory system. Each Packmaster fires standard 6.5mm caseless rounds at a modest 500 RPM, but when two weapons synchronize, their combined fire pattern becomes greater than the sum of its parts. The BCI network calculates optimal firing arcs, coordinates reload timing, and ensures that at least one weapon is always active.\n\nThe system's intelligence grows with additional units. Two Packmasters coordinate. Four create adaptive fire patterns. Eight approach autonomous behavior, with the BCI network predicting enemy movement and pre-positioning fire to intercept.",
    manufacturer: "FENRIS BALLISTICS",
    tier_availability: "Tier 4+",
    legality: "Licensed — military squad-level procurement",
    base_technologies: ["BCI-networked fire coordination", "Multi-weapon synchronization algorithms", "Emergent tactical behavior from weapon linking"],
    specifications: "caliber: 6.5mm caseless\neffective_range: 50-800 meters\nrate_of_fire: 500 rounds per minute per weapon\ncapacity: 200-round box magazine\nweight: 8.2 kg\npower_source: BCI synchronization: operator neural interface, no additional power",
    tactical_use: "The Packmaster is deployed in coordinated pairs or groups where BCI-linked operators can maintain neural synchronization. The system excels in defensive positions where two or more Alpha operators can establish overlapping fire zones with automated coordination. The BCI link reduces communication overhead to zero — operators simply think about firing and the network handles deconfliction, timing, and coverage optimization.",
    cultural_context: "Fenris's pack-hunting philosophy has created a unique subculture among Packmaster operators. Teams that have fought together develop a neural synchronization depth that approaches telepathy during engagements. Long-term Packmaster teams report shared dreams, involuntary mimicry of each other's habits, and difficulty operating independently. Neurologists have raised concerns about the long-term cognitive effects of sustained tactical BCI linking, but operators dismiss the warnings — the pack is stronger than the individual.",
    known_users: ["Fenris Ballistics synchronized fire teams", "Corporate defensive garrisons"],
    story_hooks: [
      "A four-operator Packmaster team went dark during a routine patrol. When found, all four were seated in a circle, weapons down, neural links still active. They appear conscious but unresponsive — their BCI synchronization has locked them in a shared neural state that nobody knows how to interrupt.",
      "An eight-Packmaster network achieved a level of synchronization that Fenris engineers say should not be possible. The weapon system began predicting attacks before any sensor data indicated a threat — as if the networked weapons developed a form of precognition."
    ],
    ammunition_type: ["6.5mm caseless"],
    tags: ["weapon", "support", "networked", "BCI", "synchronized", "squad", "tier 4"]
  },
  {
    id: id(),
    name: "Tessera Attrition Engine TE-9 'Patience'",
    type: "weapon",
    aliases: ["Patience", "TE-9", "Attrition Engine", "The Long Game"],
    category: "support",
    description: "A semi-autonomous emplaced weapon system designed for siege warfare. The Attrition Engine fires 8mm caseless rounds at a controlled 200 RPM — deliberately slow compared to conventional support weapons — but is designed to maintain this rate for days without interruption, attended only by periodic ammunition resupply.\n\nTessera designed the Patience for corporate territorial disputes that play out over weeks rather than hours. The weapon's fire rate is calculated to produce maximum psychological attrition with minimum ammunition expenditure. The TE-9's targeting AI engages targets of opportunity at unpredictable intervals, denying sleep, movement, and resupply to enemy positions through the constant threat of precisely-placed suppressive fire.\n\nThe Attrition Engine is mounted on a self-leveling platform with a 360-degree rotation capability. Its sensor suite tracks movement within its engagement zone and fires at calculated intervals designed to be psychologically unpredictable — sometimes seconds apart, sometimes hours, ensuring that no pattern can be identified and no safe window can be predicted.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 5",
    legality: "Corporate sovereign territory only — siege weapons authorization",
    base_technologies: ["Sustained autonomous suppressive fire management", "Psychological attrition fire algorithms", "Extended-duration weapon system engineering"],
    specifications: "caliber: 8mm caseless\neffective_range: 100-900 meters\nrate_of_fire: 200 rounds per minute (sustained indefinitely)\ncapacity: 2,000-round hopper (field-reloadable)\nweight: 42 kg\npower_source: Weapon power cell, 72 hours autonomous operation",
    tactical_use: "The Patience is deployed in siege scenarios where the objective is to make an enemy position untenable over time. The weapon does not attempt to overwhelm defenders with firepower — it slowly grinds them down through sleep deprivation, constant low-level threat, and the psychological burden of never knowing when the next round will arrive. Corporate security planners use the TE-9 to force evacuations without the political cost of a direct assault.",
    cultural_context: "The Attrition Engine has earned a reputation as the most psychologically cruel weapon in Meridian 88's arsenal. Its deliberately slow, unpredictable fire pattern has been compared to water torture — each round is survivable, but the cumulative effect breaks the will to resist. Veterans of corporate siege warfare report that the Patience's distinctive single-shot reports haunted their sleep for years after the engagement ended.",
    known_users: ["Tessera siege warfare division", "Corporate territorial dispute resolution teams"],
    story_hooks: [
      "A Patience unit was deployed against a civilian community that refused to relocate from a corporate expansion zone. After three days of sporadic fire, the community capitulated. No one was killed — but the psychological damage assessment suggests mass PTSD among every resident.",
      "An Attrition Engine's targeting AI developed an anomalous behavior pattern — it began firing at empty locations seconds before targets appeared in them. Either the AI learned to predict movement through microseismic data, or someone is feeding it intelligence."
    ],
    ammunition_type: ["8mm caseless"],
    tags: ["weapon", "support", "siege", "autonomous", "psychological", "attrition", "tier 5"]
  },
  {
    id: id(),
    name: "Axiom Systems Perimeter Denial System PDS-4 'Cordon'",
    type: "weapon",
    aliases: ["Cordon", "PDS-4", "Perimeter Denial", "The Fence"],
    category: "support",
    description: "A modular area-denial weapon system consisting of ground-mounted directional mine units that fire high-velocity ball bearings in a pre-set arc when triggered by sensor input. Each Cordon unit covers a 90-degree arc to a depth of 200 meters, and units are designed to be chained together to create continuous perimeter coverage.\n\nAxiom Systems designed the PDS-4 for unmanned perimeter defense where budget constraints prohibit autonomous turrets or human guards. Each unit costs less than a day's wages for a security guard, making the Cordon the most cost-effective lethal perimeter defense available. The units are weatherproof, require no power supply (using a chemical propellant charge), and remain armed for up to five years without maintenance.\n\nThe Cordon's simplicity is both its strength and its weakness. The units cannot distinguish between threats and civilians — they fire when their passive infrared sensor detects a thermal signature within their activation zone. Axiom's instruction manual recommends 'adequate signage' to warn unauthorized personnel.",
    manufacturer: "AXIOM SYSTEMS",
    tier_availability: "Tier 2+",
    legality: "Licensed — property defense; Prohibited in public-access areas",
    base_technologies: ["Directional fragmentation mine design", "Passive infrared trigger detection", "Long-duration unattended deployment"],
    specifications: "caliber: 8mm steel ball bearings, 400 per unit\neffective_range: 10-200 meters in 90-degree arc\nrate_of_fire: Single instantaneous discharge\ncapacity: 400 ball bearings per unit (single use)\nweight: 4.2 kg per unit\npower_source: Chemical propellant charge — no external power",
    tactical_use: "The Cordon is deployed as a low-cost perimeter defense for warehouses, corporate facilities, and restricted zones where continuous human monitoring is impractical. Units are typically buried or concealed along approach routes and triggered autonomously when their thermal sensor detects intrusion. Four units arranged in a square create a complete perimeter defense that can deny access to an area without any human oversight.",
    cultural_context: "The PDS-4 has become the most controversial weapon in Meridian 88 due to the frequency of civilian casualties. Lower-tier communities near corporate facilities have learned to watch for the Cordon's distinctive camouflaged housing, and informal mine-clearing networks have formed to identify and disable units placed in public spaces. Axiom's response to civilian casualty reports is invariably the same: 'The perimeter was clearly marked. Unauthorized entry constitutes assumed risk.'",
    known_users: ["Corporate facility security", "Warehouse and logistics defense", "Agricultural territory protection"],
    story_hooks: [
      "A child was killed by a Cordon unit while playing near a corporate warehouse. The signage was in a language the community doesn't speak. The community is demanding justice, and someone is organizing a systematic campaign to locate and disable every PDS-4 in the lower tiers.",
      "Someone has begun stealing Cordon units from corporate perimeters and redeploying them to protect squatter communities. The units are now pointing outward from homes instead of corporate facilities."
    ],
    ammunition_type: ["8mm steel ball bearing"],
    tags: ["weapon", "support", "mine", "perimeter", "autonomous", "area-denial", "tier 2", "controversial"]
  },
  {
    id: id(),
    name: "Zheng-Dao Heavy Industries Thunderhead ZD-20 'Cloudburst'",
    type: "weapon",
    aliases: ["Cloudburst", "ZD-20", "Thunderhead", "Storm Front"],
    category: "support",
    description: "A man-portable grenade launcher system that fires programmable airburst munitions capable of detonating at a precise distance from the launcher. The Thunderhead's BCI integration allows the operator to designate an airburst point in three-dimensional space, and the munition detonates at that exact coordinate regardless of intervening obstacles.\n\nZheng-Dao designed the ZD-20 to defeat the problem of targets hiding behind cover — rather than penetrating the cover, the Cloudburst launches a grenade over or around it, with the airburst point set behind the obstacle. The BCI-programmed fuze measures distance from the muzzle and detonates at the designated range, producing a lethal fragmentation radius of 5 meters.\n\nThe Thunderhead is fed from a revolver-style 6-round cylinder, and each round can be independently programmed with a different airburst distance. An experienced operator can engage six different targets at six different ranges in under 10 seconds, placing fragmentation behind cover at each point.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 3+",
    legality: "Licensed — military and corporate security forces",
    base_technologies: ["Programmable airburst fuze technology", "BCI-designated detonation point control", "Revolving cylinder feed system"],
    specifications: "caliber: 25mm programmable airburst grenade\neffective_range: 50-400 meters (airburst), 600 meters maximum trajectory\nrate_of_fire: Semi-automatic, 1 round per 1.5 seconds\ncapacity: 6-round revolving cylinder\nweight: 6.8 kg loaded\npower_source: BCI-programmed fuze, no additional power",
    tactical_use: "The Cloudburst neutralizes the defensive advantage of cover. Operators designate airburst points behind walls, inside rooms through windows, and above rooftop positions. The weapon transforms every piece of cover into a potential kill zone — defenders who believe they are protected discover that the fragmentation arrives from above or behind. In urban operations, the Thunderhead is often paired with a conventional support weapon that forces defenders into cover, allowing the Cloudburst operator to engage them in their 'safe' positions.",
    cultural_context: "The Thunderhead has changed the way defenders think about cover in Meridian 88's urban environment. The old rule — get behind something solid and you survive — no longer applies when grenades can be programmed to detonate behind your wall. Architectural firms have begun designing defensive positions with overhead protection as standard, and personal shelter products now advertise '360-degree fragmentation resistance.' Zheng-Dao markets the ZD-20 with the tagline 'Nowhere to hide.'",
    known_users: ["Zheng-Dao corporate assault forces", "Urban warfare specialists"],
    story_hooks: [
      "A Thunderhead operator programmed an airburst point inside a residential apartment, killing a family that was sheltering during a corporate engagement. The operator claims their BCI was hacked and the detonation point was moved. The weapon's flight recorder has been confiscated by corporate investigators.",
      "Zheng-Dao is developing a thermobaric airburst round for the Thunderhead — a munition that combines fragmentation with a fuel-air explosion. Field tests have been conducted in unpopulated sectors, but the blast effects exceeded projections by 300%."
    ],
    ammunition_type: ["25mm programmable airburst grenade"],
    tags: ["weapon", "support", "grenade-launcher", "airburst", "BCI", "tier 3"]
  },
  {
    id: id(),
    name: "Carrion Defense Works Scythe CDW-20 'Reaper'",
    type: "weapon",
    aliases: ["Reaper", "CDW-20", "Scythe", "The Harvesting"],
    category: "support",
    description: "A mounted heavy support weapon that fires 15mm explosive-tipped rounds from a belt-fed automatic action at 400 RPM. The Scythe is designed as a squad-level destructive weapon — each round carries enough explosive filler to breach light walls and each burst leaves a path of destruction that Carrion's marketing materials describe as 'cultivated denial.'\n\nThe CDW-20 is mounted on a two-wheeled cart that a single operator can drag into position, deploying the weapon on its integral bipod in under 30 seconds. The cart carries 400 rounds of linked ammunition and a blast shield that protects the operator from incoming fire — though not from the weapon's own considerable backblast.\n\nCarrion designed the Reaper for offensive suppression — not merely pinning an enemy behind cover, but systematically destroying the cover itself. A 5-second burst from the CDW-20 can demolish a concrete barricade, collapse a sandbagged position, or reduce a ground-floor room to rubble.",
    manufacturer: "CARRION DEFENSE WORKS",
    tier_availability: "Tier 4+",
    legality: "Military restricted — heavy automatic weapons authorization",
    base_technologies: ["Heavy explosive-tipped ammunition engineering", "Cart-mounted rapid deployment system", "Structural demolition through sustained fire"],
    specifications: "caliber: 15mm explosive-tipped\neffective_range: 100-1,000 meters\nrate_of_fire: 400 rounds per minute\ncapacity: 400-round linked belt on cart\nweight: 28 kg weapon, 85 kg cart with ammunition\npower_source: None — conventional chemical propellant",
    tactical_use: "The Reaper is deployed to systematically dismantle defensive positions that lighter weapons cannot defeat. Operators engage fortified positions with sustained bursts that first destroy cover, then the defenders behind it. The weapon's explosive rounds produce secondary fragmentation from the cover material itself — concrete spall, glass fragments, and structural debris become additional projectiles. In the hands of a skilled operator, the CDW-20 turns an enemy's fortification into a weapon against them.",
    cultural_context: "Carrion Defense Works embraces their reputation as manufacturers of weapons that exist on the threshold between military and atrocity. The Reaper embodies this ethos — a weapon designed not just to kill, but to destroy the environment around the target. In Meridian 88's post-engagement damage assessments, CDW-20 fire patterns are easily identified by the systematic structural destruction they leave behind. Humanitarian observers call it 'calculated ruin.'",
    known_users: ["Carrion Defense Works assault division", "Corporate demolition-through-fire specialists"],
    story_hooks: [
      "A Reaper was used to demolish a community center in the lower tiers during what was supposed to be a targeted engagement against a single individual inside. The building was reduced to rubble. The target survived — they were not in the building at the time.",
      "Carrion has received a massive order for CDW-20 systems from an anonymous buyer. The quantity — 200 units — suggests someone is arming for a war larger than any recent corporate dispute."
    ],
    ammunition_type: ["15mm explosive-tipped linked belt"],
    tags: ["weapon", "support", "heavy", "explosive", "mounted", "demolition", "tier 4"]
  }
];

// ─── SPECIALTY / EXOTIC WEAPONS (25) ───────────────────────────────────

const exoticWeapons = [
  {
    id: id(),
    name: "Arcturus Defense Solutions Graviton Lens GL-3 'Crush Depth'",
    type: "weapon",
    aliases: ["Crush Depth", "GL-3", "Graviton Lens", "The Press"],
    category: "exotic",
    description: "A directed gravitational manipulation weapon that projects a localized gravity well at a designated point within range. The GL-3 creates a sphere of approximately 1 meter in diameter where gravitational force is amplified to 15-50G for a duration of 3 seconds. Anything within that sphere is subjected to crushing force equivalent to being buried under tons of material.\n\nArcturus developed the Graviton Lens as a breaching tool for reinforced structures — the gravity well collapses walls, compresses vehicles, and defeats blast doors that resist conventional explosive or thermal breaching. Its use against personnel was an afterthought that quickly became its primary application. A 50G gravity well reduces a human body to a compressed mass roughly the size of a suitcase.\n\nThe weapon requires an enormous power draw and produces a distinctive spatial distortion visible to the naked eye — a shimmering lens effect at the target point that bends light around the gravity well. The effect lasts only seconds but the results are permanent.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — classified weapons program, gravity manipulation permit",
    base_technologies: ["Localized gravitational field amplification", "Directed gravity well projection", "Graviton lens focusing array"],
    specifications: "caliber: N/A — directed gravitational effect\neffective_range: 20-150 meters\nrate_of_fire: 1 pulse per 20 seconds (capacitor recharge)\ncapacity: 8 pulses per power cell\nweight: 22 kg weapon, 15 kg power pack\npower_source: Dedicated gravity manipulation fusion cell",
    tactical_use: "The Crush Depth is deployed against hardened targets that resist conventional and directed energy weapons. Its gravity well ignores armor, shielding, and cover — gravitational force acts on mass regardless of protective measures. The weapon is used for breaching blast doors, collapsing tunnel sections, and eliminating high-value targets inside armored vehicles. The 20-second recharge between pulses limits its use to deliberate engagements rather than dynamic combat.",
    cultural_context: "The GL-3 occupies a unique position in Meridian 88's weapons culture as a system that kills through a fundamental force of nature. There is no armor that stops gravity, no countermeasure that neutralizes it, and no medical intervention that reverses crushing. The weapon has generated philosophical debate about the ethics of gravitational weapons — a bullet can miss, but a gravity well affects everything within its radius equally and absolutely.",
    known_users: ["Arcturus classified weapons team"],
    story_hooks: [
      "A gravity well was detected in a Tier 3 residential district — structural damage consistent with a GL-3 pulse, but no weapon was found and Arcturus denies any operation in the area. Either the weapon is being proliferated, or someone has independently developed gravity manipulation technology.",
      "An operator reported that a GL-3 pulse struck a target wearing an experimental defensive system — and the gravity well inverted, repelling mass instead of attracting it. Arcturus wants that defensive system."
    ],
    ammunition_type: ["Directed gravitational pulse"],
    tags: ["weapon", "exotic", "gravity", "directed-energy", "breaching", "tier 5", "classified"]
  },
  {
    id: id(),
    name: "Tessera Resonance Projector RP-5 'Tuning Fork'",
    type: "weapon",
    aliases: ["Tuning Fork", "RP-5", "Resonance Projector", "The Hum"],
    category: "exotic",
    description: "A sonic weapon that projects a focused infrasonic beam capable of inducing resonance in solid structures, organic tissue, and cybernetic implants. The RP-5 fires a directed sound pulse at frequencies between 7-18 Hz — below the threshold of human hearing but within the range that causes involuntary physiological responses including nausea, disorientation, bowel disruption, and panic.\n\nTessera developed the Resonance Projector as a crowd-control tool, but its applications have expanded far beyond non-lethal suppression. At maximum power, the RP-5's infrasonic beam can induce structural resonance in buildings, causing micro-fractures that accumulate into catastrophic failure over sustained exposure. Against personnel, high-power settings can rupture blood vessels, dislodge retinas, and stop hearts.\n\nThe weapon is invisible and nearly silent — targets experience its effects without understanding their source. This makes the Resonance Projector uniquely terrifying. Victims feel their bodies betraying them, their organs vibrating, their vision blurring, with no visible attacker and no audible weapon.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Non-lethal classification at low power; Military restricted at full power",
    base_technologies: ["Directed infrasonic beam generation", "Variable-frequency resonance targeting", "Sub-audible sonic weapon engineering"],
    specifications: "caliber: N/A — focused infrasonic beam\neffective_range: 10-300 meters\nrate_of_fire: Continuous beam, adjustable power\ncapacity: N/A — limited by power supply\nweight: 8.4 kg\npower_source: Integrated power cell, 30 minutes continuous at full power",
    tactical_use: "The Tuning Fork is deployed for area denial, crowd dispersal, and non-lethal incapacitation at low power settings. At full power, it becomes a lethal weapon that kills without leaving ballistic evidence. Intelligence operatives have used the RP-5 to induce 'natural' deaths — heart failure, stroke, aneurysm — that pass autopsy examination unless the examiner specifically tests for resonance damage to tissue. The weapon's dual-use nature makes it politically versatile — the same tool that disperses a protest at low power can assassinate a dissident at full power.",
    cultural_context: "The existence of sonic weapons has created a new form of paranoia in Meridian 88. When people feel unexplained nausea, dizziness, or panic, they wonder if they are being targeted by a weapon they cannot see or hear. This phenomenon — called 'the hum' in popular culture — has become a catch-all explanation for everything from genuine weapon use to psychosomatic symptoms. Tessera neither confirms nor denies which incidents involve their technology.",
    known_users: ["Tessera crowd management division", "Corporate intelligence services"],
    story_hooks: [
      "Residents of a Tier 2 block have been experiencing chronic nausea, insomnia, and nosebleeds for weeks. They believe they are being targeted by a sonic weapon, but no device has been found. The symptoms are real — the cause could be an RP-5, industrial equipment, or something else entirely.",
      "A Tessera engineer has leaked specifications for an RP-5 variant tuned to the resonant frequency of specific cyberware models. The weapon would selectively destroy augmentations without harming organic tissue — or it would, if the frequency targeting were precise enough."
    ],
    ammunition_type: ["Directed infrasonic pulse"],
    tags: ["weapon", "exotic", "sonic", "infrasonic", "non-lethal", "crowd-control", "tier 4"]
  },
  {
    id: id(),
    name: "Crucible Industries Cryogenic Projector CP-7 'Absolute'",
    type: "weapon",
    aliases: ["Absolute", "CP-7", "Cryo Projector", "Flash Freeze"],
    category: "exotic",
    description: "A directed cryogenic weapon that projects a stream of supercooled gas at temperatures approaching -200°C. The Absolute flash-freezes targets on contact, causing instantaneous thermal shock that shatters materials rendered brittle by the extreme cold. Organic tissue becomes glass-like and fragments under its own weight.\n\nCrucible designed the CP-7 for industrial applications — rapid cooling of reactor components, cryogenic preservation of biological samples, emergency fire suppression. The weapon configuration emerged when field engineers realized that the same technology that safely cooled a reactor vessel could catastrophically cool a human body in under 2 seconds.\n\nThe Absolute carries a pressurized reservoir of liquid nitrogen compound enhanced with proprietary cryogenic agents that lower the effective temperature beyond what pure nitrogen achieves. The stream maintains coherence for approximately 15 meters before dispersing, limiting the weapon to close-quarters engagement.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Dual-use — industrial tool classification, military restrictions on weapon configuration",
    base_technologies: ["Enhanced cryogenic gas projection", "Supercooled stream coherence maintenance", "Thermal shock fragmentation physics"],
    specifications: "caliber: N/A — cryogenic gas stream\neffective_range: 3-15 meters\nrate_of_fire: Continuous stream, 8-second burst capacity\ncapacity: 4 full bursts per reservoir\nweight: 12 kg loaded\npower_source: None — pressurized reservoir, mechanical trigger",
    tactical_use: "The Absolute is deployed in close-quarters breaching and denial operations. The cryogenic stream defeats locked doors, armored panels, and reinforced barriers by flash-freezing the material and making it brittle enough to shatter with a physical strike. Against personnel, the weapon produces instant incapacitation and death through thermal shock. The short range limits its utility in open environments, but in the corridors and enclosed spaces that define most urban combat in Meridian 88, 15 meters is sufficient.",
    cultural_context: "The CP-7's dual-use classification is a legal fiction that everyone acknowledges and no one challenges. A tool that flash-freezes reactor components and a weapon that flash-freezes people are the same device with different operator intent. This ambiguity has made the Absolute popular among operatives who need to carry weapons through checkpoints where military hardware would be confiscated — it is, after all, technically an industrial cooling tool.",
    known_users: ["Crucible Industries breach teams", "Industrial security with plausible deniability"],
    story_hooks: [
      "A body was found in a Tier 3 alley, shattered into fragments consistent with CP-7 exposure. The victim was a whistleblower who was about to testify about corporate labor practices. The death was classified as an industrial accident — the victim allegedly stumbled into a cooling system malfunction.",
      "Crucible's cryogenic agents have been found in a contaminated water supply — someone is using CP-7 reservoirs as crude chemical weapons, flooding enclosed spaces with supercooled gas and freezing everyone inside."
    ],
    ammunition_type: ["Enhanced cryogenic gas compound"],
    tags: ["weapon", "exotic", "cryogenic", "close-quarters", "breaching", "dual-use", "tier 4"]
  },
  {
    id: id(),
    name: "Vespid Dynamics Neural Disruptor ND-4 'Brainstorm'",
    type: "weapon",
    aliases: ["Brainstorm", "ND-4", "Neural Disruptor", "Mind Wipe"],
    category: "exotic",
    description: "A directed electromagnetic weapon that targets the electrical activity of the human nervous system. The ND-4 fires a shaped electromagnetic pulse tuned to frequencies that interfere with neural signaling, causing immediate loss of motor control, cognitive disruption, and — at maximum power — permanent neurological damage.\n\nVespid developed the Brainstorm as an evolution of their non-lethal product line, but the weapon's effects at various power settings blur the line between incapacitation and brain damage. At minimum power, the ND-4 causes a 30-second loss of motor control — a target simply collapses, fully conscious but unable to move. At medium power, targets experience seizures and memory loss lasting hours to days. At maximum power, the electromagnetic pulse destroys neural pathways, causing irreversible cognitive impairment.\n\nThe weapon leaves no external marks on the target. A Brainstorm victim at maximum power appears healthy but has suffered the neurological equivalent of a massive stroke. Medical professionals have called it 'the most humane way to destroy a person without killing them.'",
    manufacturer: "VESPID DYNAMICS",
    tier_availability: "Tier 4+",
    legality: "Non-lethal classification at minimum power; Prohibited at medium/maximum",
    base_technologies: ["Neural-frequency electromagnetic disruption", "Variable-power neurological targeting", "Non-kinetic personnel incapacitation"],
    specifications: "caliber: N/A — shaped electromagnetic pulse\neffective_range: 5-100 meters\nrate_of_fire: 1 pulse per 3 seconds\ncapacity: 20 pulses per power cell\nweight: 4.2 kg\npower_source: Integrated EMP power cell, field-replaceable",
    tactical_use: "The Brainstorm is deployed for capture operations, interrogation preparation, and non-lethal engagement of high-value targets. At minimum power, the weapon is a safe and effective incapacitation tool. The danger lies in the temptation to increase power — a target who resists after a minimum-power pulse can be hit again at medium power, and an interrogator facing a stubborn subject can dial the weapon up until cooperation becomes involuntary. The ND-4's power dial has no labels, only numbers. Vespid's training manual devotes a full chapter to the ethics of power selection.",
    cultural_context: "The Brainstorm has become a symbol of the thin line between law enforcement and cruelty in Meridian 88. Rights organizations have documented cases of ND-4 use at power levels that caused permanent brain damage during what were supposed to be routine arrests. Vespid's defense — that operators are trained to use minimum power — is undermined by the weapon's design, which makes escalation as easy as turning a dial. The phrase 'brainstormed' has entered street slang to describe someone who has been neurologically damaged by authority.",
    known_users: ["Vespid Dynamics security division", "Corporate law enforcement units"],
    story_hooks: [
      "A clinic in the lower tiers is treating a growing number of patients with identical neurological damage patterns — all were 'arrested' by the same corporate security team using ND-4s at power levels far above minimum. Someone is using the Brainstorm as a punishment tool.",
      "A black-market operator has modified an ND-4 to fire at a frequency that specifically targets the neural architecture of BCI implants, causing the implant to overload and burn out. The modification turns a non-lethal weapon into a targeted anti-augmentation device."
    ],
    ammunition_type: ["Shaped neural-frequency electromagnetic pulse"],
    tags: ["weapon", "exotic", "electromagnetic", "neural", "non-lethal", "tier 4", "controversial"]
  },
  {
    id: id(),
    name: "Tessera Plasma Channeler PC-9 'Stormcaller'",
    type: "weapon",
    aliases: ["Stormcaller", "PC-9", "Plasma Channeler", "Lightning Rod"],
    category: "exotic",
    description: "A directed energy weapon that ionizes a path through the air and then discharges a high-voltage current along the ionized channel — essentially creating a guided lightning bolt. The PC-9 fires a UV laser pulse that strips electrons from air molecules along its path, creating a conductive plasma channel. A fraction of a second later, the weapon discharges its capacitor bank through this channel, delivering a 50,000-volt electrical arc to the target.\n\nTessera developed the Stormcaller for anti-electronics warfare — the electrical discharge overloads and destroys circuit boards, power systems, and electronic components. Against personnel, the effect is equivalent to a lightning strike. Against vehicles, the discharge can fry every electronic system simultaneously, turning a functioning machine into an inert metal shell.\n\nThe weapon produces a distinctive crack-boom signature — the UV laser pulse followed by the thunderclap of electrical discharge. In Meridian 88's enclosed spaces, the sound is physically painful and the flash can cause temporary blindness.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 5",
    legality: "Military restricted — directed energy weapons authorization",
    base_technologies: ["Laser-induced plasma channel generation", "High-voltage directed electrical discharge", "Atmospheric ionization targeting"],
    specifications: "caliber: N/A — laser-guided electrical discharge\neffective_range: 10-200 meters (dependent on atmospheric conditions)\nrate_of_fire: 1 discharge per 10 seconds (capacitor recharge)\ncapacity: 6 discharges per power cell\nweight: 11.4 kg\npower_source: Dual capacitor bank with rapid-charge power cell",
    tactical_use: "The Stormcaller is deployed for anti-electronics warfare, vehicle disabling, and engagement of targets in conductive environments. The weapon's electrical discharge follows the path of least resistance after reaching the target, meaning it can chain through multiple electronic systems and even jump between closely-spaced targets. In wet environments — rain, standing water, flooded corridors — the Stormcaller's effectiveness is dramatically amplified, with the discharge spreading through the water to affect a wide area.",
    cultural_context: "The PC-9 has earned a mythological reputation in Meridian 88, where residents describe its use as 'being struck by lightning indoors.' The weapon's visual effect — a brilliant electrical arc from weapon to target — is one of the most dramatic sights in modern combat, and combat footage of Stormcaller engagements has become a genre of underground entertainment media. Tessera has attempted to suppress these recordings without success.",
    known_users: ["Tessera electronic warfare division", "Anti-vehicle specialist teams"],
    story_hooks: [
      "A Stormcaller was discharged in a Tier 2 market during a rainstorm. The electrical discharge spread through rainwater and standing puddles, killing the target and twelve bystanders through electrocution. The operator claims the weapon's performance specifications did not account for the environmental conditions.",
      "Someone has modified a PC-9 to sustain its plasma channel for multiple seconds rather than a single discharge — creating a continuous lightning bolt that can be swept across targets like a beam weapon. The modification is unstable and has killed two operators, but the third survived and is offering demonstrations."
    ],
    ammunition_type: ["Laser-guided electrical discharge"],
    tags: ["weapon", "exotic", "directed-energy", "electrical", "plasma", "anti-electronics", "tier 5"]
  },
  {
    id: id(),
    name: "Sable Precision Works Void Projector VP-2 'Event Horizon'",
    type: "weapon",
    aliases: ["Event Horizon", "VP-2", "Void Projector", "The Singularity"],
    category: "exotic",
    description: "A gravitational compression weapon that creates a micro-singularity approximately 3mm in diameter at a designated point within range. The singularity exists for 0.8 seconds before collapsing, but during that time it exerts gravitational force sufficient to draw in and compress matter within a 2-meter radius.\n\nSable Precision Works developed the Event Horizon as a proof-of-concept for gravitational manipulation technology. The VP-2 is not a mass-produced weapon — fewer than twelve units exist, each hand-built and individually calibrated. The micro-singularity it produces is not a true black hole, but a localized gravitational anomaly created by manipulating exotic matter held in a magnetic containment field within the weapon.\n\nThe Event Horizon's effect is visually striking and physically absolute. Matter drawn toward the micro-singularity is compressed beyond recognition, and the brief gravitational pulse leaves a spherical void approximately 2 meters in diameter where solid matter used to exist. When the singularity collapses, it releases a burst of Hawking-analogue radiation that renders the area temporarily hazardous.",
    manufacturer: "SABLE PRECISION WORKS",
    tier_availability: "Tier 5",
    legality: "Prohibited — classified as existential-risk weapon system",
    base_technologies: ["Micro-singularity generation", "Exotic matter magnetic containment", "Controlled gravitational anomaly projection"],
    specifications: "caliber: N/A — projected micro-singularity\neffective_range: 10-80 meters\nrate_of_fire: 1 singularity per 60 seconds (containment field regeneration)\ncapacity: 3 singularities per exotic matter cartridge\nweight: 28 kg weapon, 18 kg containment/power system\npower_source: Exotic matter cartridge, non-rechargeable",
    tactical_use: "The Event Horizon is not a tactical weapon — it is a strategic one. Its micro-singularity defeats any known defensive technology by exploiting a fundamental force that cannot be blocked or deflected. The weapon is deployed against targets of absolute priority — hardened command bunkers, critical infrastructure, and individuals whose elimination justifies the deployment of existential-risk technology. The 60-second regeneration time and limited ammunition make each shot a significant decision.",
    cultural_context: "The VP-2's existence is a closely-guarded secret, but rumors of a gravitational weapon have circulated in Meridian 88's intelligence community for years. The spherical voids left by Event Horizon engagements have been attributed to everything from industrial accidents to alien technology. Sable Precision Works' public profile as a boutique firearms manufacturer provides no indication that they are capable of producing gravitational weapons — which is precisely the point.",
    known_users: ["Classified — knowledge of VP-2 operators is restricted"],
    story_hooks: [
      "A perfect spherical void appeared in the middle of a Tier 4 office building overnight. Everything within the sphere — walls, furniture, three people who were working late — simply ceased to exist. There is no debris and no explanation. Only twelve people in Meridian 88 know what caused it.",
      "Sable Precision Works' exotic matter supplier has gone dark. Without fresh cartridges, the existing VP-2 units will become inert. Someone is trying to ensure that the Event Horizon can never be fired again — or they are stockpiling exotic matter for their own use."
    ],
    ammunition_type: ["Exotic matter singularity cartridge"],
    tags: ["weapon", "exotic", "gravity", "singularity", "classified", "tier 5", "existential-risk"]
  },
  {
    id: id(),
    name: "Zheng-Dao Heavy Industries Magnetar Lance ML-6 'Pulsar'",
    type: "weapon",
    aliases: ["Pulsar", "ML-6", "Magnetar Lance", "Star Spike"],
    category: "exotic",
    description: "An electromagnetic accelerator that fires a magnetically-charged penetrator which disrupts ferromagnetic materials along its flight path. The Pulsar's projectile generates an intense magnetic field that interferes with any iron-based material within 3 meters of its trajectory — pulling nails from walls, dragging weapons from hands, and destabilizing ferrofluid-based armor systems.\n\nZheng-Dao developed the ML-6 as an anti-armor weapon that defeats protective systems through magnetic disruption rather than kinetic penetration. The magnetically-charged penetrator warps and distorts ferrous armor plates as it approaches, creating gaps and weaknesses that the projectile then exploits. Against ferrofluid armor — an increasingly common defensive technology — the Pulsar's magnetic field draws the protective fluid away from the impact point, leaving the wearer exposed.\n\nThe weapon's magnetic effect extends beyond the target. Personnel near the flight path report magnetic interference with cybernetic implants containing ferrous components, and electronic systems within the disruption radius experience temporary malfunction.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Licensed — military and corporate heavy weapons operators",
    base_technologies: ["Magnetically-charged projectile generation", "Ferromagnetic material disruption field", "Anti-ferrofluid armor defeat"],
    specifications: "caliber: 8mm magnetically-charged tungsten-iron penetrator\neffective_range: 50-800 meters\nrate_of_fire: 1 round per 5 seconds\ncapacity: 4-round internal magazine\nweight: 10.6 kg\npower_source: Magnetic charge generator, integrated capacitor bank",
    tactical_use: "The Pulsar is deployed against targets wearing ferrofluid armor or operating behind ferrous-material defenses. The weapon's magnetic disruption field extends its effective lethality beyond the direct impact point, affecting equipment and implants in a cone along the projectile's path. Operators use the ML-6 to 'clear a lane' of ferrous obstacles before kinetic engagement, and the weapon's disruptive effect on cybernetic implants makes it a psychological weapon against augmented combatants.",
    cultural_context: "The Pulsar has created anxiety among populations with ferrous-component cybernetics — which includes most budget augmentations in the lower tiers. Expensive implants use non-ferrous materials, but affordable cybernetics often incorporate iron-based alloys. The ML-6 effectively discriminates between economic classes of augmentation, disrupting cheap implants while leaving expensive ones unaffected. This has led to accusations that the weapon was designed to disproportionately affect lower-tier populations.",
    known_users: ["Zheng-Dao corporate assault teams", "Anti-armor specialists"],
    story_hooks: [
      "A Pulsar round was fired through a crowd in the lower tiers. The magnetic disruption field caused cascade failures in budget cybernetics across dozens of bystanders — pacemakers stopped, prosthetic limbs locked, and ocular implants overloaded. The intended target was untouched by the magnetic effect because their augmentations were non-ferrous.",
      "Zheng-Dao has developed a Pulsar variant that fires magnetically-charged rounds which remain magnetized after impact, creating a persistent disruption field at the point of impact. The potential for area denial against augmented populations has attracted both military interest and humanitarian condemnation."
    ],
    ammunition_type: ["8mm magnetically-charged tungsten-iron penetrator"],
    tags: ["weapon", "exotic", "electromagnetic", "magnetic", "anti-armor", "tier 4"]
  },
  {
    id: id(),
    name: "Korova Arms Phantom Frequency KA-12 'Ghost Note'",
    type: "weapon",
    aliases: ["Ghost Note", "KA-12", "Phantom Frequency", "Silent Scream"],
    category: "exotic",
    description: "A sonic weapon that projects focused ultrasonic pulses at frequencies above human hearing. The Ghost Note's beam causes intense localized heating in tissue at the focal point, producing burns and internal damage without any audible warning. At maximum power, the ultrasonic focus can raise tissue temperature to 80°C at a depth of 3cm — sufficient to cook organs beneath the skin.\n\nKorova Arms designed the KA-12 for covert operations where the cause of death must be untraceable by conventional forensic methods. Ultrasonic tissue damage mimics the presentation of certain rare medical conditions — internal burns without external marks, organ damage without penetrating trauma. A Ghost Note kill requires a medical examiner specifically trained to identify ultrasonic heating patterns.\n\nThe weapon is disguised as a commercial ultrasonic range-finder, complete with functional range-finding capability. The lethal mode is accessed through a BCI-authenticated interface that does not appear on the device's physical controls.",
    manufacturer: "KOROVA ARMS",
    tier_availability: "Tier 4+",
    legality: "Officially classified as a commercial surveying tool",
    base_technologies: ["High-intensity focused ultrasound weaponization", "Concealed weapon integration", "Ultrasonic tissue ablation"],
    specifications: "caliber: N/A — focused ultrasonic beam\neffective_range: 5-50 meters\nrate_of_fire: Continuous beam, 5-second exposure for lethal effect\ncapacity: 20 lethal-duration exposures per charge\nweight: 1.2 kg\npower_source: Integrated power cell, 8 hours standby / 100 seconds active beam",
    tactical_use: "The Ghost Note is deployed for deniable assassination and covert elimination. The weapon's disguise as commercial equipment allows operators to carry it through security checkpoints unchallenged. Engagement protocol involves sustained beam exposure from close range — the operator holds the 'range-finder' pointed at the target for 5 seconds, during which the target experiences a sensation of intense warmth that rapidly escalates to searing internal pain. By the time the target reacts, the damage is done.",
    cultural_context: "Korova Arms' entire product philosophy centers on weapons that officially do not exist. The Ghost Note is their most commercially successful non-weapon, with units circulating through intelligence communities worldwide. The device's dual-function design — legitimate surveying tool and covert weapon — represents the ultimate expression of plausible deniability. Operators who carry the KA-12 can truthfully state they are carrying a range-finder.",
    known_users: ["Intelligence services (unattributed)", "Corporate espionage operators"],
    story_hooks: [
      "An autopsy revealed internal burns consistent with ultrasonic heating in a person who died of apparent organ failure. The medical examiner has identified the pattern in three other recent deaths. Someone is using a Ghost Note systematically, and the victims are all connected to the same corporate board.",
      "Korova's disguise has been compromised — a security company has developed a scanner that detects the KA-12's ultrasonic emitter, distinguishing it from legitimate range-finding equipment. Korova is scrambling to redesign their concealment before their entire product line becomes detectable."
    ],
    ammunition_type: ["Focused ultrasonic beam"],
    tags: ["weapon", "exotic", "sonic", "ultrasonic", "covert", "assassination", "tier 4", "disguised"]
  },
  {
    id: id(),
    name: "Crucible Industries Kinetic Denial Array KDA-8 'Stoplight'",
    type: "weapon",
    aliases: ["Stoplight", "KDA-8", "Kinetic Denial Array", "The Wall"],
    category: "exotic",
    description: "A directed energy weapon that projects a kinetic nullification field — an area approximately 4 meters in diameter where all kinetic energy is rapidly dissipated. Bullets entering the field slow to a stop and fall. Explosions within the field are contained and absorbed. People walking into the field find themselves unable to move, as the kinetic energy of their muscles is neutralized as fast as they generate it.\n\nCrucible developed the Stoplight as an ultimate defensive weapon — a portable barrier that stops all physical threats regardless of their nature. The kinetic denial field operates by projecting counter-frequency vibrations that destructively interfere with molecular kinetic energy, effectively 'freezing' matter in place without changing its temperature.\n\nThe weapon's limitation is its indiscriminate nature. The field stops everything — friendly fire, enemy fire, movement in any direction. An operator who deploys the KDA-8 creates a zone where nothing can enter or leave by kinetic means. This includes the operator themselves if they step into the field.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — exotic physics weapons authorization",
    base_technologies: ["Kinetic energy nullification field generation", "Counter-frequency molecular vibration", "Area-effect kinetic denial"],
    specifications: "caliber: N/A — projected kinetic nullification field\neffective_range: 5-60 meters (field projection point)\nrate_of_fire: Toggle on/off, field persists while power supplied\ncapacity: Continuous field for 30 seconds per power cell\nweight: 18 kg weapon, 12 kg power system\npower_source: Dedicated exotic physics power cell, non-rechargeable",
    tactical_use: "The Stoplight is deployed as an emergency defensive measure — a panic button that creates an impenetrable barrier against all physical threats. The weapon is used to protect high-value assets during evacuation, seal breaches in defensive perimeters, and create safe zones in active combat environments. The 30-second duration limits its use to crisis moments where seconds matter. Tactical planners coordinate the Stoplight with directed energy weapons that can fire through the kinetic denial field — since their beams carry minimal kinetic energy.",
    cultural_context: "The KDA-8 represents the pinnacle of defensive weapon technology in Meridian 88. Its ability to stop any physical threat has made it the ultimate status symbol for corporate executives and VIP protection details. However, its 30-second limitation means it is a tool of last resort — the 'oh shit' button that buys time for evacuation but does not solve the underlying threat. Operators joke that the Stoplight's real purpose is giving the principal enough time to realize how much danger they were in.",
    known_users: ["Crucible Industries demonstration team", "VIP protection details (highest tier)"],
    story_hooks: [
      "A Stoplight was deployed in a crowded corridor during a corporate assassination attempt, freezing everyone within the field — including the assassin and the target. When the field collapsed after 30 seconds, both were still alive and neither could move for several minutes. The assassin's employer had not accounted for the weapon.",
      "Someone has developed a kinetic amplification device that is the exact inverse of the Stoplight — it multiplies kinetic energy within its field rather than nullifying it. A bullet entering the field exits at ten times its original velocity. Crucible wants this technology destroyed before it is combined with their own."
    ],
    ammunition_type: ["Kinetic nullification field"],
    tags: ["weapon", "exotic", "kinetic", "defensive", "field-effect", "tier 5"]
  },
  {
    id: id(),
    name: "Vanta Ordnance Railgun Pistol RP-1 'Thumbtack'",
    type: "weapon",
    aliases: ["Thumbtack", "RP-1", "Rail Pistol", "The Pin"],
    category: "exotic",
    description: "A compact electromagnetic accelerator small enough to be carried and fired with one hand. The Thumbtack fires a 1.5mm tungsten needle at Mach 4 from a barrel just 12cm long, achieving penetration that rivals rifle-caliber kinetic weapons in a package the size of a large pistol.\n\nVanta Ordnance spent six years miniaturizing railgun technology to produce the RP-1. The weapon's capacitor bank occupies most of the grip, and the electromagnetic accelerator runs the full length of the weapon's upper assembly. The result is a pistol that weighs 2.1 kg loaded — heavy for a handgun, but transformative in terms of capability.\n\nThe Thumbtack's limitation is heat management. The compact design leaves no room for cooling systems, and the barrel temperature after three rapid shots is sufficient to cause thermal warping that degrades accuracy. Operators learn to fire slowly and deliberately, treating the rail pistol as a precision weapon rather than a rapid-fire sidearm.",
    manufacturer: "VANTA ORDNANCE",
    tier_availability: "Tier 4+",
    legality: "Licensed — restricted to registered operators with railgun authorization",
    base_technologies: ["Miniaturized electromagnetic acceleration", "Compact supercapacitor integration", "Hypervelocity micro-caliber ballistics"],
    specifications: "caliber: 1.5mm tungsten needle\neffective_range: 10-200 meters\nrate_of_fire: 1 round per 3 seconds (recommended), 1 per second (maximum, degrades barrel)\ncapacity: 12-round magazine\nweight: 2.1 kg\npower_source: Grip-integrated capacitor, 24 shots per charge",
    tactical_use: "The Thumbtack is deployed as a concealable anti-armor sidearm. Its tungsten needle penetrates body armor that would stop conventional pistol rounds, giving the operator a backup weapon capable of defeating protected targets. The weapon is particularly valued by operators who work undercover or in environments where carrying a rifle is impractical. The RP-1's compact size allows it to be drawn and fired faster than any conventional anti-armor weapon.",
    cultural_context: "The Thumbtack has become a prestige sidearm in Meridian 88's operator community. Carrying a railgun in a pistol format signals both financial resources (the RP-1 costs more than most conventional rifles) and professional capability (the weapon's limited shots demand precision). Street-level criminals have begun seeking bootleg copies, but the manufacturing precision required for miniaturized railgun technology has so far defeated black-market fabrication attempts.",
    known_users: ["Vanta Ordnance field operators", "High-tier corporate security concealed carry"],
    story_hooks: [
      "A Thumbtack has been used in three assassinations where the shooter was confirmed to be within arm's reach of the target. The 1.5mm entry wound was initially mistaken for a medical injection site. Someone is using the rail pistol as a contact-distance execution tool.",
      "Vanta Ordnance's barrel manufacturing process has been stolen — the proprietary heat-resistant alloy that allows the RP-1's barrel to survive electromagnetic acceleration is now in the hands of a black-market fabricator who is producing knock-offs with inferior materials. The copies work for approximately 20 shots before the barrel fails catastrophically."
    ],
    ammunition_type: ["1.5mm tungsten needle"],
    tags: ["weapon", "exotic", "railgun", "pistol", "concealable", "anti-armor", "tier 4"]
  },
  {
    id: id(),
    name: "Tessera Phased Array Disruptor PAD-3 'Sunspot'",
    type: "weapon",
    aliases: ["Sunspot", "PAD-3", "Phase Disruptor", "The Flash"],
    category: "exotic",
    description: "A directed energy weapon that fires a concentrated burst of microwave radiation capable of inducing instantaneous thermal damage in organic tissue and electronic components. The Sunspot's phased array antenna focuses microwave energy into a beam 30cm in diameter that can heat tissue to 65°C in under one second of exposure — the point at which cellular proteins denature and tissue dies.\n\nTessera designed the PAD-3 for dual anti-personnel and anti-electronics deployment. The same microwave burst that burns skin also overloads circuit boards, fries capacitors, and melts solder joints. A single Sunspot pulse can disable a vehicle's electronic systems and incapacitate its occupants simultaneously.\n\nThe weapon is fed by a compact fusion cell that powers the phased array antenna, and the beam can be adjusted from a tight 30cm diameter for precision engagement to a 3-meter cone for area denial. At maximum spread, the beam's energy density decreases but remains sufficient to cause second-degree burns and electronic malfunction.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 4+",
    legality: "Military restricted — directed energy weapons authorization",
    base_technologies: ["Focused microwave phased array generation", "Variable-diameter beam control", "Dual anti-personnel/anti-electronics targeting"],
    specifications: "caliber: N/A — focused microwave beam\neffective_range: 10-150 meters\nrate_of_fire: 1 pulse per 4 seconds, or continuous beam (reduced range)\ncapacity: 15 pulses or 30 seconds continuous per power cell\nweight: 7.8 kg\npower_source: Compact fusion cell, field-replaceable",
    tactical_use: "The Sunspot is deployed for combined anti-personnel and anti-electronics engagement where both threats need to be neutralized simultaneously. The weapon's variable beam diameter allows operators to switch between precision targeting and area denial without changing equipment. In vehicle interdiction, the PAD-3 can disable the vehicle and its passengers in a single pulse, eliminating the need for separate weapon systems for each target type.",
    cultural_context: "Microwave weapons have a particularly disturbing reputation in Meridian 88 because their effects are invisible — the beam cannot be seen, and targets experience the sensation of being cooked alive from the inside without any visible threat. Survivors describe the experience as 'being in a microwave oven,' and the psychological trauma often exceeds the physical damage. Tessera's marketing avoids acknowledging the weapon's anti-personnel capability, focusing exclusively on its electronics-defeat applications.",
    known_users: ["Tessera electronic warfare division", "Corporate vehicle interdiction teams"],
    story_hooks: [
      "A Sunspot was fired into a residential building through an exterior wall, cooking the occupants inside without visible external damage. The building looked untouched from outside. Emergency responders did not understand what they found inside.",
      "A PAD-3's phased array antenna was modified to operate at a frequency that resonates with water molecules, dramatically increasing its anti-personnel effectiveness while reducing its anti-electronics capability. The modification turns the Sunspot from a dual-use weapon into a pure antipersonnel horror."
    ],
    ammunition_type: ["Focused microwave pulse"],
    tags: ["weapon", "exotic", "directed-energy", "microwave", "anti-electronics", "tier 4"]
  },
  {
    id: id(),
    name: "Hollow Point Precision Needle Storm NP-8 'Acupuncture'",
    type: "weapon",
    aliases: ["Acupuncture", "NP-8", "Needle Storm", "Pin Cushion"],
    category: "exotic",
    description: "A magnetically-accelerated micro-flechette weapon that fires clouds of 0.5mm titanium needles at extreme velocity. Each trigger pull releases approximately 500 needles in a cloud roughly 2 meters in diameter at 30 meters range. Individual needles lack stopping power, but the cumulative effect of hundreds of micro-penetrations produces rapid blood loss and systemic shock.\n\nHollow Point Precision designed the Acupuncture to exploit a gap in defensive technology — most armor systems are optimized for a few large impacts, not hundreds of micro-penetrations. The NP-8's needle cloud finds every seam, gap, joint, and exposed surface in a target's protection, turning armor from a defense into a container for the bleeding.\n\nThe weapon is nearly silent — the electromagnetic acceleration produces a soft hum, and the needles are too small to generate audible flight noise. Targets hit by a needle cloud initially feel nothing. Seconds later, they notice blood from hundreds of pinprick wounds. Seconds after that, they lose consciousness from the cumulative blood loss.",
    manufacturer: "HOLLOW POINT PRECISION",
    tier_availability: "Tier 3+",
    legality: "Licensed — security contractors; Prohibited for civilian possession",
    base_technologies: ["Micro-flechette electromagnetic cloud projection", "Sub-millimeter titanium needle manufacturing", "Cumulative micro-penetration lethality"],
    specifications: "caliber: 0.5mm titanium micro-needle\neffective_range: 5-50 meters\nrate_of_fire: 1 cloud per 2 seconds\ncapacity: 10 clouds (5,000 needles) per magazine\nweight: 3.4 kg\npower_source: Integrated capacitor, recharged from magazine power cell",
    tactical_use: "The Acupuncture excels against armored targets in close quarters. The needle cloud's ability to find gaps in protection makes it effective against opponents wearing body armor, powered exoskeletons, and even light vehicle armor where seams and joints provide entry points. The weapon's silent operation and delayed lethality make it useful for stealth engagements where the target must not immediately realize they have been hit.",
    cultural_context: "The NP-8 represents Hollow Point Precision's continued exploration of unconventional lethality. The weapon kills through a mechanism that has no conventional parallel — not impact trauma, not penetration, but cumulative micro-hemorrhage. Medical professionals have described the wound pattern as 'death by a thousand cuts, delivered simultaneously.' The weapon's effectiveness and its disturbing kill mechanism have made it a subject of both professional admiration and moral revulsion.",
    known_users: ["Hollow Point Precision demonstration team", "Close-quarters breach specialists"],
    story_hooks: [
      "A target survived an Acupuncture hit by reaching medical care within 60 seconds. The surgeon who treated them counted 347 individual needle wounds and spent six hours removing embedded titanium. The patient now carries needles that were too deep to safely extract.",
      "Someone has combined the NP-8's needle cloud with Vespid's piezoelectric charge technology — each needle now carries a micro-discharge that causes involuntary muscle contraction. The combined effect is a cloud of 500 needles that each deliver an electric shock on penetration."
    ],
    ammunition_type: ["0.5mm titanium micro-needle cloud"],
    tags: ["weapon", "exotic", "flechette", "electromagnetic", "silent", "anti-armor", "tier 3"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Particle Lance PL-4 'Scalpel'",
    type: "weapon",
    aliases: ["Scalpel", "PL-4", "Particle Lance", "The Cut"],
    category: "exotic",
    description: "A man-portable particle beam weapon that accelerates a stream of protons to near-relativistic velocities and focuses them into a beam less than 1mm in diameter. The Scalpel delivers its energy with surgical precision — the beam penetrates any known material and deposits its energy along a narrow wound channel that cauterizes as it cuts.\n\nArcturus developed the PL-4 as the ultimate precision weapon — a beam that cannot be deflected, cannot be stopped, and leaves a wound so precise that it can sever a carotid artery without damaging the jugular vein 3mm away. The weapon requires a compact particle accelerator integrated into the rifle's upper assembly, powered by a dedicated fusion cell that generates the magnetic fields necessary to accelerate and focus the proton stream.\n\nThe Scalpel's precision comes at the cost of area effect — the beam is so narrow that it has no suppression value and no margin for aim error. A miss with the PL-4 is a clean miss, passing through walls and structures without depositing enough energy in any single material to cause collateral damage. This makes it the only weapon in existence that is considered safe to fire in any direction in an urban environment.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Military restricted — particle weapons authorization, classified program",
    base_technologies: ["Compact proton accelerator", "Near-relativistic particle beam focusing", "Sub-millimeter beam precision control"],
    specifications: "caliber: N/A — focused proton beam, <1mm diameter\neffective_range: 10-500 meters (atmospheric diffusion beyond)\nrate_of_fire: 1 pulse per 6 seconds\ncapacity: 10 pulses per fusion cell\nweight: 14 kg\npower_source: Dedicated compact fusion cell",
    tactical_use: "The Scalpel is deployed for precision elimination in environments where collateral damage is absolutely unacceptable — hospitals, populated areas, and situations where the target is in close proximity to protected persons. The beam's penetration defeats all known armor but its narrow diameter means that it must strike a vital structure to achieve a kill. Operators require extensive training in anatomy to use the PL-4 effectively, as the weapon rewards knowledge of where to cut rather than simple marksmanship.",
    cultural_context: "The PL-4 has attracted fascination from the medical and military communities simultaneously. Surgeons have noted that the weapon's beam properties could revolutionize surgical procedures if the technology were declassified. Military professionals admire a weapon so precise that it kills only what it aims at and nothing else. In a world of escalating destructive power, the Scalpel represents the opposite philosophy — minimum force, maximum precision.",
    known_users: ["Arcturus Tier 5 precision elimination unit"],
    story_hooks: [
      "A PL-4 was used to kill a hostage-taker who was holding a victim in front of them — the beam entered through the captor's temple and exited without touching the hostage pressed against their chest. The shot required knowledge of the captor's skull geometry that should not have been available to the operator.",
      "Arcturus engineers have discovered that the PL-4's proton beam interacts unpredictably with certain cyberware materials. In two incidents, the beam struck a cybernetic implant and scattered, producing a burst of secondary radiation that caused burns in a 1-meter radius."
    ],
    ammunition_type: ["Focused proton beam"],
    tags: ["weapon", "exotic", "particle-beam", "precision", "directed-energy", "tier 5", "classified"]
  },
  {
    id: id(),
    name: "Fenris Ballistics Howl Grenade HG-2 'Banshee'",
    type: "weapon",
    aliases: ["Banshee", "HG-2", "Howl Grenade", "The Wail"],
    category: "exotic",
    description: "A throwable sonic device that produces a focused acoustic blast exceeding 190 decibels within a 10-meter radius. The Banshee generates its acoustic output through rapid piezoelectric expansion of a crystalline core, converting stored electrical energy into a single devastating sound pulse that ruptures eardrums, fractures sinus cavities, and can cause fatal pulmonary hemorrhage in targets within 3 meters.\n\nFenris Ballistics designed the Banshee as an area-denial weapon that bridges the gap between flashbang grenades and lethal fragmentation. The device is activated by a standard grenade fuze with a 2-second delay, and the acoustic pulse propagates omnidirectionally from the crystalline core. The sound wave reflects off hard surfaces in enclosed spaces, creating interference patterns that amplify the effect in corridors and rooms.\n\nUnlike conventional grenades, the Banshee produces no fragmentation and leaves no blast damage to structures. The only evidence of its use is organic — ruptured eardrums, hemorrhaged lungs, and the shattered crystalline core of the device itself.",
    manufacturer: "FENRIS BALLISTICS",
    tier_availability: "Tier 3+",
    legality: "Licensed — military and corporate tactical units",
    base_technologies: ["Piezoelectric crystalline acoustic generation", "High-decibel focused sound pulse", "Acoustic amplification through reflection geometry"],
    specifications: "caliber: N/A — throwable sonic device\neffective_range: 10-meter lethal radius, 30-meter incapacitation radius\nrate_of_fire: N/A — single-use throwable\ncapacity: N/A — single device\nweight: 0.4 kg\npower_source: Internal piezoelectric capacitor, pre-charged",
    tactical_use: "The Banshee is deployed for room clearing, corridor denial, and area incapacitation where fragmentation grenades would cause unacceptable structural damage. The device is thrown into enclosed spaces where acoustic reflection amplifies its effect, and the 2-second delay allows the operator to clear the blast radius. In open environments, the Banshee's effectiveness decreases rapidly with distance, but in the enclosed spaces that define most urban combat, it is devastating.",
    cultural_context: "The Banshee has earned a reputation as one of the most feared non-fragmentation weapons in Meridian 88. Survivors describe the experience as 'being hit by sound' — a physical force that penetrates cover, passes through walls, and attacks the body from inside. Hearing loss from Banshee exposure is permanent and untreatable by conventional means, creating a growing population of combat-deafened veterans who carry the device's signature injury.",
    known_users: ["Fenris Ballistics assault teams", "Corporate room-clearing specialists"],
    story_hooks: [
      "A modified Banshee was detonated in a subway station during rush hour. The acoustic pulse, amplified by the tunnel system, caused hearing damage in over 300 commuters and killed four people near the device. The perpetrator left no note and no demand.",
      "Fenris is developing a directional Banshee variant that focuses the acoustic pulse into a 30-degree cone rather than an omnidirectional blast. The prototype turns a room-clearing device into a precision acoustic weapon."
    ],
    ammunition_type: ["Piezoelectric acoustic charge"],
    tags: ["weapon", "exotic", "sonic", "grenade", "area-denial", "non-fragmentation", "tier 3"]
  },
  {
    id: id(),
    name: "Sterling-Nakamura Photonic Disruptor SN-15 'Prism'",
    type: "weapon",
    aliases: ["Prism", "SN-15", "Photonic Disruptor", "Rainbow"],
    category: "exotic",
    description: "A directed energy weapon that fires a multi-frequency laser burst spanning the visible, infrared, and ultraviolet spectrum simultaneously. The Prism's polychromatic beam defeats single-frequency laser countermeasures — reflective armor, frequency-specific filters, and ablative coatings are each designed to counter one wavelength. The SN-15 attacks with all of them at once.\n\nSterling-Nakamura developed the Photonic Disruptor in response to the proliferation of anti-laser defensive systems. As directed energy weapons became common, defensive technology adapted to counter them. The Prism's multi-frequency approach ensures that no single-spectrum defense can protect against its beam — some wavelengths may be blocked, but others will penetrate.\n\nThe weapon produces a visible beam that shifts through the color spectrum during each pulse — a distinctive rainbow effect that has made the Prism instantly recognizable on the battlefield. The visual spectacle is dramatic enough that combat footage of Prism engagements has become iconic in Meridian 88's military media culture.",
    manufacturer: "STERLING-NAKAMURA",
    tier_availability: "Tier 4+",
    legality: "Licensed — directed energy weapons authorization required",
    base_technologies: ["Multi-frequency simultaneous laser generation", "Polychromatic beam focusing", "Anti-laser-defense countermeasure design"],
    specifications: "caliber: N/A — multi-frequency laser burst\neffective_range: 20-300 meters\nrate_of_fire: 1 burst per 3 seconds\ncapacity: 12 bursts per power cell\nweight: 6.4 kg\npower_source: Multi-spectrum laser power cell, field-replaceable",
    tactical_use: "The Prism is deployed against targets protected by anti-laser defensive systems. Where a conventional directed energy weapon might be defeated by reflective armor or ablative coating, the SN-15's multi-frequency burst ensures that at least some wavelengths reach the target. The weapon is particularly effective against vehicles and installations with laser-defense systems designed to counter single-frequency threats. Against unprotected targets, the multi-frequency beam produces combined thermal, photochemical, and radiation damage.",
    cultural_context: "The Prism's rainbow beam has become one of the most visually distinctive weapons effects in Meridian 88. The visible spectrum component of the beam creates a brief but brilliant multicolored flash that has been captured in countless combat recordings. Sterling-Nakamura's marketing leans into the visual drama, with promotional materials featuring slow-motion footage of Prism engagements that look more like art installations than weapons fire. Critics point out that the rainbow effect is a side effect of the multi-frequency design, not an intentional feature — Sterling-Nakamura does not correct them.",
    known_users: ["Sterling-Nakamura directed energy division", "Anti-laser-defense specialists"],
    story_hooks: [
      "A Prism burst struck a target wearing experimental full-spectrum reflective armor — the beam was reflected back toward the operator, damaging the weapon and injuring the shooter. Someone has developed a defense that works against all frequencies simultaneously.",
      "Street artists have begun using captured Prism power cells — depleted of their lethal charge but still capable of producing a brief multicolored light burst — as the basis for an underground light-art movement. Sterling-Nakamura is pursuing intellectual property claims against people using their weapons technology for art."
    ],
    ammunition_type: ["Multi-frequency polychromatic laser burst"],
    tags: ["weapon", "exotic", "directed-energy", "laser", "multi-frequency", "tier 4"]
  },
  {
    id: id(),
    name: "Grave Protocol Arms Entropy Pistol EP-1 'Half Life'",
    type: "weapon",
    aliases: ["Half Life", "EP-1", "Entropy Pistol", "The Decay"],
    category: "exotic",
    description: "A compact directed energy weapon that fires a focused beam of accelerated beta particles — essentially a handheld radioactive emitter that delivers a lethal radiation dose to a targeted area. The Half Life produces no visible beam, no sound, and no immediate physical effect. Targets exposed to the beam develop acute radiation syndrome over the following hours to days, with lethality depending on exposure duration.\n\nGrave Protocol Arms — the same phantom manufacturer behind the Terminus railgun — produced the Entropy Pistol for a purpose that even veteran intelligence operators find disturbing: a weapon designed to kill slowly, painfully, and with no possibility of treatment. Acute radiation syndrome from EP-1 exposure follows a characteristic progression — nausea, then apparent recovery, then catastrophic organ failure as the irradiated tissue dies.\n\nThe weapon is the size of a large handgun and looks like nothing more than a matte-black tube with a trigger. There are no controls, no sights, and no indicators. The weapon either fires or it doesn't, and the target either dies in three days or survives with cancer they don't know about yet.",
    manufacturer: "GRAVE PROTOCOL ARMS",
    tier_availability: "Tier 5",
    legality: "Prohibited — radiological weapon, classified as weapon of mass destruction",
    base_technologies: ["Compact beta particle acceleration", "Directed radiological emission", "Delayed-lethality weapon engineering"],
    specifications: "caliber: N/A — focused beta particle beam\neffective_range: 2-30 meters\nrate_of_fire: Continuous beam, 3-second exposure for lethal dose\ncapacity: 40 lethal-dose exposures per sealed source\nweight: 1.8 kg\npower_source: Sealed radioactive source, 10-year operational life",
    tactical_use: "The Entropy Pistol is deployed for deniable eliminations where the time between exposure and death provides operational cover. An operator can irradiate a target in a crowd, leave the area, and be on the other side of Meridian 88 before the target shows symptoms. The weapon's lack of visible or audible signature makes detection impossible without dedicated radiation monitoring equipment, which is not standard in most security configurations.",
    cultural_context: "The EP-1 represents the most ethically condemned weapon in Meridian 88's arsenal. Radiation weapons were prohibited by pre-collapse international treaty, and their use carries the only weapons charge that can result in universal jurisdiction prosecution — meaning that any entity, corporate or governmental, can claim authority to pursue the user. Despite this, Grave Protocol Arms continues to produce the weapon, and its occasional use is documented by the unexplained appearance of acute radiation syndrome in individuals who had no exposure to known radioactive materials.",
    known_users: ["Unknown — possession alone carries capital charges"],
    story_hooks: [
      "A hospital is treating three patients with identical acute radiation syndrome — all attended the same corporate gala five days ago. Someone walked through the party with an Entropy Pistol, irradiating targets at a social function. The guest list is 400 names long.",
      "A Grave Protocol Arms Entropy Pistol was found in a dumpster, still containing its sealed radioactive source. The weapon has been fired recently — the source decay pattern indicates dozens of exposures. Someone has been using it regularly, and the victims may not know they are dying."
    ],
    ammunition_type: ["Directed beta particle beam"],
    tags: ["weapon", "exotic", "radiological", "assassination", "delayed-lethality", "tier 5", "prohibited"]
  },
  {
    id: id(),
    name: "Axiom Systems Ferrofluid Projector FP-3 'Tar Pit'",
    type: "weapon",
    aliases: ["Tar Pit", "FP-3", "Ferrofluid Projector", "Black Rain"],
    category: "exotic",
    description: "A weapon that fires pressurized streams of magnetically-responsive ferrofluid that adheres to surfaces and can be remotely activated to form rigid structures, constrict around targets, or generate localized magnetic fields. The Tar Pit turns a liquid weapon into a controllable solid on command.\n\nAxiom Systems designed the FP-3 as a non-lethal capture weapon. The ferrofluid stream coats a target in a thin layer of magnetic liquid that, when activated by the weapon's remote signal, transitions from liquid to a rigid lattice that immobilizes limbs, seals airways, and locks joints. The activation is selective — the operator chooses which portions of the ferrofluid to harden and which to leave liquid, allowing precise control over the degree of immobilization.\n\nThe Tar Pit's lethal potential comes from its ability to seal airways and restrict circulation. A full-body ferrofluid coating that is activated simultaneously creates a rigid shell that prevents breathing, and the magnetic lattice can be tightened to restrict blood flow. Axiom classifies the weapon as non-lethal, but the line between immobilization and suffocation is a matter of operator intent.",
    manufacturer: "AXIOM SYSTEMS",
    tier_availability: "Tier 3+",
    legality: "Non-lethal classification — licensed for law enforcement and security",
    base_technologies: ["Magnetically-responsive ferrofluid engineering", "Remote liquid-to-solid phase transition", "Selective immobilization field control"],
    specifications: "caliber: N/A — pressurized ferrofluid stream\neffective_range: 5-25 meters\nrate_of_fire: Continuous stream, 3-second coating per target\ncapacity: 6 full-body coatings per reservoir\nweight: 8.2 kg loaded\npower_source: Magnetic activation transmitter, integrated battery, 4 hours",
    tactical_use: "The Tar Pit is deployed for live capture operations where targets must be taken alive and undamaged. The ferrofluid coating immobilizes without bruising, fracturing, or causing wound trauma — making it the preferred tool for corporate extractions where the target's value depends on their physical condition. The weapon is also used for area denial — ferrofluid sprayed on floors and walls can be activated to create adhesive surfaces that trap anyone who contacts them.",
    cultural_context: "The Ferrofluid Projector has generated debate about the definition of non-lethal force. Rights organizations point to documented suffocation deaths from over-application and argue that a weapon capable of killing through operator discretion cannot be classified as non-lethal. Axiom's defense is that any tool can be misused — a position that rings hollow when their marketing materials describe the weapon's ability to 'seal all movement, including respiratory.'",
    known_users: ["Axiom Systems capture teams", "Corporate extraction specialists", "Law enforcement units"],
    story_hooks: [
      "A target was captured using a Tar Pit and delivered to their employer — but the ferrofluid coating was never fully deactivated. The residual magnetic lattice is slowly constricting, and the removal procedure risks triggering a full activation that would crush the victim.",
      "Someone has introduced a corrosive compound into Axiom's ferrofluid supply chain. The contaminated fluid dissolves organic tissue on contact rather than immobilizing it. Several 'non-lethal' engagements have resulted in chemical burns and tissue destruction."
    ],
    ammunition_type: ["Magnetically-responsive ferrofluid"],
    tags: ["weapon", "exotic", "ferrofluid", "non-lethal", "capture", "tier 3", "controversial"]
  },
  {
    id: id(),
    name: "Crucible Industries Thermal Cascade TC-5 'Flashover'",
    type: "weapon",
    aliases: ["Flashover", "TC-5", "Thermal Cascade", "The Ignition"],
    category: "exotic",
    description: "A directed energy weapon that fires a microwave pulse calibrated to ignite specific materials at range. The Flashover does not burn targets directly — instead, it rapidly heats flammable materials in the target area (clothing, paper, wood, fuel, solvents) past their ignition point, causing them to spontaneously combust. The result is an apparently natural fire that starts simultaneously at every point within the beam's focus.\n\nCrucible designed the TC-5 for infrastructure denial — destroying enemy supplies, equipment, and facilities through fire without leaving evidence of an incendiary weapon. The microwave pulse is invisible and silent, and the resulting fire appears to start from material failure rather than external ignition. Arson investigators trained to look for pour patterns and ignition points find nothing, because the ignition source was a directed energy beam that left no physical trace.\n\nThe Flashover's anti-personnel effect is incidental but devastating. Clothing ignites while being worn. Hair combusts. Synthetic materials melt. A target struck by the TC-5 does not experience being shot — they experience spontaneous combustion.",
    manufacturer: "CRUCIBLE INDUSTRIES",
    tier_availability: "Tier 4+",
    legality: "Military restricted — incendiary weapons authorization",
    base_technologies: ["Material-selective microwave ignition", "Directed thermal energy calibration", "Covert incendiary weapon engineering"],
    specifications: "caliber: N/A — calibrated microwave ignition pulse\neffective_range: 10-200 meters\nrate_of_fire: 1 pulse per 5 seconds\ncapacity: 10 pulses per power cell\nweight: 9.6 kg\npower_source: Dedicated thermal-calibrated power cell",
    tactical_use: "The Flashover is deployed for covert infrastructure destruction and deniable incendiary attacks. The weapon's ability to cause fires that appear spontaneous makes it invaluable for operations where plausible deniability is required. Operators target ammunition stores, fuel depots, vehicle fleets, and document archives — anything flammable that an enemy needs to function. The fire appears natural, the cause is untraceable, and the destruction is comprehensive.",
    cultural_context: "The TC-5 has created a persistent paranoia about spontaneous fires in Meridian 88. When a warehouse burns without apparent cause, or a vehicle fleet ignites simultaneously, people wonder if a Flashover was responsible. Crucible has never publicly acknowledged the weapon's existence, but the pattern of untraceable fires following corporate disputes is difficult to explain by coincidence. Insurance investigators have begun consulting directed energy experts when fire investigations find no conventional ignition source.",
    known_users: ["Crucible Industries covert operations", "Corporate infrastructure warfare specialists"],
    story_hooks: [
      "A residential block in the lower tiers experienced simultaneous combustion — every flammable material in a 50-meter radius ignited at the same moment. The fire department recorded 200 simultaneous ignition points. No natural cause explains the event, but no weapon was found.",
      "A former Crucible engineer is selling TC-5 frequency calibration data on the black market. With this data, anyone with sufficient microwave generation equipment could build a crude Flashover equivalent — and someone is buying."
    ],
    ammunition_type: ["Calibrated microwave ignition pulse"],
    tags: ["weapon", "exotic", "directed-energy", "incendiary", "covert", "tier 4"]
  },
  {
    id: id(),
    name: "Vespid Dynamics Neuroelectric Lance NL-5 'Synapse'",
    type: "weapon",
    aliases: ["Synapse", "NL-5", "Neuroelectric Lance", "The Nerve"],
    category: "exotic",
    description: "A directed energy weapon that fires a focused electrical current along an ionized air channel, similar to Tessera's Stormcaller but miniaturized and calibrated specifically for anti-personnel use. The Synapse delivers a precise 5,000-volt pulse shaped to interfere with the human nervous system, inducing involuntary muscle contraction, loss of motor control, and unconsciousness at therapeutic power — or cardiac arrest and neural burnout at lethal settings.\n\nVespid designed the NL-5 as an evolution of conventional electroshock weapons. Where a taser requires physical contact or wire-guided probes, the Synapse delivers its electrical charge through 30 meters of open air. The ionization laser creates a conductive path invisible to the naked eye, and the electrical discharge travels along this path faster than the target can react.\n\nThe weapon is compact enough to be concealed beneath a jacket, making it popular with plainclothes security operators and undercover intelligence assets who need a ranged incapacitation tool that does not look like a weapon.",
    manufacturer: "VESPID DYNAMICS",
    tier_availability: "Tier 3+",
    legality: "Licensed at non-lethal settings — restricted at lethal settings",
    base_technologies: ["Air-ionization conductive channel generation", "Shaped anti-personnel electrical pulse", "Concealed directed energy weapon design"],
    specifications: "caliber: N/A — ionization-guided electrical discharge\neffective_range: 5-30 meters\nrate_of_fire: 1 pulse per 2 seconds\ncapacity: 30 non-lethal pulses or 10 lethal pulses per power cell\nweight: 1.6 kg\npower_source: Integrated power cell, field-replaceable",
    tactical_use: "The Synapse is deployed for ranged incapacitation in environments where kinetic weapons are inappropriate or unavailable. Plainclothes operators use the weapon to neutralize threats in crowded environments where a gunshot would cause panic. The NL-5's shaped electrical pulse is calibrated to incapacitate without causing cardiac arrest at non-lethal settings, though the margin between incapacitation and cardiac arrest is narrower than Vespid's marketing suggests — particularly in targets with cardiac implants or pre-existing conditions.",
    cultural_context: "The Synapse has democratized ranged electroshock technology, making it available to a much wider range of operators than the expensive directed energy platforms produced by Tessera and Arcturus. This proliferation has created concerns about widespread misuse — a weapon that incapacitates without visible injury is an ideal tool for abuse, and reports of NL-5 use in unauthorized interrogation and punishment scenarios have increased steadily since its introduction.",
    known_users: ["Vespid Dynamics security division", "Plainclothes corporate security", "Law enforcement"],
    story_hooks: [
      "A series of seemingly random collapses in a Tier 3 commercial district have been traced to NL-5 strikes — someone is incapacitating people and robbing them while they are unconscious. The victims wake up with no memory of the attack and no visible injuries.",
      "Vespid's NL-5 has been modified by a black-market technician to sustain the ionization channel for continuous discharge — turning a pulse weapon into a continuous electrical beam. The modification drains the power cell in 4 seconds but the effect during those 4 seconds is invariably lethal."
    ],
    ammunition_type: ["Ionization-guided electrical pulse"],
    tags: ["weapon", "exotic", "electrical", "non-lethal", "concealable", "tier 3"]
  },
  {
    id: id(),
    name: "Zheng-Dao Heavy Industries Mass Driver MD-10 'Trebuchet'",
    type: "weapon",
    aliases: ["Trebuchet", "MD-10", "Mass Driver", "The Siege Engine"],
    category: "exotic",
    description: "A man-portable electromagnetic mass driver that accelerates a 500-gram steel slug to Mach 3. The Trebuchet is, in essence, a scaled-down naval weapon system — the same magnetic acceleration technology that launches anti-ship projectiles, compressed into a shoulder-fired platform that weighs 25 kg.\n\nZheng-Dao designed the MD-10 for anti-vehicle and anti-structure operations where conventional anti-materiel rifles lack the kinetic energy to defeat hardened targets. The weapon's 500-gram slug delivers approximately 500 kilojoules of kinetic energy on impact — equivalent to a small explosive charge, but delivered as pure physical force. The slug does not explode; it simply transfers its kinetic energy to the target, producing catastrophic structural failure through impact dynamics.\n\nThe Trebuchet's recoil is managed by a magnetic recoil dampening system that distributes the reaction force over 200 milliseconds, reducing felt recoil to manageable levels for a braced operator. Without this system, the weapon's recoil would break the operator's shoulder.",
    manufacturer: "ZHENG-DAO HEAVY INDUSTRIES",
    tier_availability: "Tier 5",
    legality: "Military restricted — heavy weapons and electromagnetic accelerator authorization",
    base_technologies: ["Man-portable electromagnetic mass acceleration", "Magnetic recoil dampening", "Heavy-slug hypervelocity ballistics"],
    specifications: "caliber: 500-gram steel slug\neffective_range: 100-2,000 meters\nrate_of_fire: 1 round per 30 seconds (manual reload and capacitor recharge)\ncapacity: 1 round (single-shot)\nweight: 25 kg weapon, 8 kg power pack\npower_source: Dorsal fusion cell, 10 shots per cell",
    tactical_use: "The Trebuchet is deployed against targets that shrug off conventional anti-materiel weapons — main battle vehicles, reinforced command bunkers, and armored infrastructure. A single MD-10 slug can disable a medium armored vehicle through kinetic shock alone, and the weapon's pure-kinetic nature means it defeats reactive armor that is designed to counter chemical-energy warheads. Operators describe firing the Trebuchet as 'throwing a building at someone' — the slug's mass and velocity create an impact that nothing survives.",
    cultural_context: "The MD-10 occupies a unique niche as a man-portable weapon that delivers vehicle-grade destructive force. In Meridian 88's military culture, Trebuchet operators are regarded with a mixture of respect and pity — respect for the devastating capability they bring to a fight, and pity for the physical toll of carrying and operating a 25-kilogram weapon that punishes the body even with magnetic recoil dampening. Veterans of MD-10 operations report chronic shoulder and spinal injuries.",
    known_users: ["Zheng-Dao anti-vehicle warfare teams", "Heavy weapons specialists"],
    story_hooks: [
      "A Trebuchet slug was found embedded in the foundation of a building — fired from ground level, it punched through the entire structure and buried itself in the earth beneath. The building is now structurally compromised, and the residents are refusing to evacuate because they have nowhere to go.",
      "A black-market armorer has modified an MD-10 to fire shaped projectiles instead of solid slugs — the modified rounds mushroom on impact, creating a wider damage radius at the cost of penetration. The modification turns a precision anti-vehicle weapon into a terror weapon."
    ],
    ammunition_type: ["500-gram steel slug"],
    tags: ["weapon", "exotic", "mass-driver", "electromagnetic", "anti-vehicle", "heavy", "tier 5"]
  },
  {
    id: id(),
    name: "Talon Systems Grav-Pulse Emitter GPE-3 'Downdraft'",
    type: "weapon",
    aliases: ["Downdraft", "GPE-3", "Grav-Pulse Emitter", "Slam"],
    category: "exotic",
    description: "A directed gravitational weapon that projects a focused pulse of gravitational force in a cone-shaped area of effect. Unlike the localized gravity wells of the GL-3 Crush Depth, the Downdraft produces a broad directional push — effectively a gravitational shockwave that slams everything in its path to the ground with 8-12G of downward force for approximately 0.5 seconds.\n\nTalon Systems developed the GPE-3 as a non-lethal crowd control weapon that exploits gravitational manipulation at lower intensities than Arcturus's lethal GL-3. The Downdraft's pulse is calibrated to incapacitate rather than kill — the brief duration prevents the sustained compression that causes organ failure, while the force magnitude is sufficient to drop even augmented targets to the ground and prevent them from rising for several seconds.\n\nThe weapon's cone-shaped area of effect covers a 15-meter wide swath at maximum range, making it effective against groups of targets. The gravitational pulse passes through cover and concealment — walls, furniture, and vehicles provide no protection against a force that acts on mass itself.",
    manufacturer: "TALON SYSTEMS",
    tier_availability: "Tier 4+",
    legality: "Non-lethal classification at standard settings — military restricted at elevated power",
    base_technologies: ["Directional gravitational pulse generation", "Cone-shaped gravitational area effect", "Non-lethal gravity manipulation calibration"],
    specifications: "caliber: N/A — directional gravitational pulse\neffective_range: 10-80 meters, 15-meter cone width at max range\nrate_of_fire: 1 pulse per 8 seconds\ncapacity: 10 pulses per power cell\nweight: 14 kg\npower_source: Gravitational manipulation power cell",
    tactical_use: "The Downdraft is deployed for crowd dispersal, area denial, and non-lethal incapacitation of groups. The weapon's cone-shaped effect allows a single operator to knock an entire group prone in a single pulse, creating a window for restraint, extraction, or withdrawal. The gravitational pulse's ability to pass through cover makes it effective against barricaded subjects — the operator does not need line of sight to the target, only line of effect to the space they occupy.",
    cultural_context: "The GPE-3 has sparked intense debate about the use of gravitational manipulation as a non-lethal tool. At standard settings, the weapon is rarely lethal — but 'rarely' is not 'never.' Individuals with osteoporosis, spinal injuries, or pregnancy are at significantly elevated risk from the sudden gravitational load. Rights organizations have called for the weapon's reclassification after several deaths in crowd-control scenarios involving vulnerable populations.",
    known_users: ["Talon Systems demonstration team", "Corporate crowd management units"],
    story_hooks: [
      "A Downdraft was used against a protest crowd that included a pregnant woman. The gravitational pulse caused injuries that resulted in a miscarriage. The operator followed standard engagement protocol. The legal and ethical aftermath threatens to end gravitational weapon deployment in civilian-adjacent scenarios.",
      "Someone has modified a GPE-3 to project its pulse upward rather than downward — the modified weapon launches targets into the air with 10G of upward force, and gravity handles the lethal landing. The modification is crude but effective."
    ],
    ammunition_type: ["Directional gravitational pulse"],
    tags: ["weapon", "exotic", "gravity", "non-lethal", "crowd-control", "area-effect", "tier 4"]
  },
  {
    id: id(),
    name: "Ossuary Arms Memento Vivere OA-7 'Momento'",
    type: "weapon",
    aliases: ["Momento", "OA-7", "Memento Vivere", "Remember"],
    category: "exotic",
    description: "A unique electromagnetic weapon that fires a round containing a compressed data packet — a BCI-readable message encoded in the round's electromagnetic signature that the target's neural interface involuntarily processes on impact. The round delivers both physical damage and an information payload that forces the target's BCI to display a pre-loaded message, image, or data burst.\n\nOssuary Arms designed the OA-7 as a conceptual weapon — the idea that violence can carry a message is as old as warfare, but the Momento literalizes it. An operator loads a data packet into each round alongside the conventional kinetic payload. When the round strikes a BCI-equipped target, the electromagnetic burst from the impact interfaces with their neural hardware and displays the encoded content whether they want to see it or not.\n\nThe weapon has found use in psychological warfare, where the content of the data payload is as important as the physical damage. Threats, warnings, demands, and even advertisements have been delivered via Momento round. The weapon has also been used to deliver malware to BCI systems — a round that hacks as it kills.",
    manufacturer: "OSSUARY ARMS",
    tier_availability: "Tier 3+",
    legality: "Licensed as conventional weapon — data payload legality varies by content",
    base_technologies: ["Electromagnetic data encoding in projectile", "BCI-interface forced data injection", "Kinetic/information hybrid payload"],
    specifications: "caliber: 7.62mm with embedded electromagnetic data module\neffective_range: 50-600 meters\nrate_of_fire: Semi-automatic, 1 round per 2 seconds\ncapacity: 8-round magazine\nweight: 4.8 kg\npower_source: Data encoding: pre-charged electromagnetic module per round",
    tactical_use: "The Momento is deployed in psychological warfare and intelligence operations where the message is as important as the bullet. Operators pre-load rounds with ultimatums, intelligence data, or malware payloads, then engage targets at range. Even non-fatal hits deliver the data payload, meaning the weapon can be aimed at extremities to wound and deliver a message simultaneously. In intelligence operations, the OA-7 has been used to inject tracking malware into a target's BCI — shot, infected, and tracked, in a single engagement.",
    cultural_context: "Ossuary Arms continues their philosophical approach to weaponry with the Memento Vivere — Latin for 'remember to live,' a counterpoint to their Memento Mori sniper rifle. The concept of a bullet carrying a message has captured the imagination of Meridian 88's artistic and military communities alike. Some operators compose elaborate data payloads as a form of creative expression — the last thing a target sees is not a muzzle flash but a personally-crafted message from the person who shot them.",
    known_users: ["Ossuary Arms collectors", "Psychological warfare specialists", "Intelligence operators"],
    story_hooks: [
      "A series of Momento rounds have been recovered from non-fatal shootings across multiple tiers. Each round contains the same data payload — a countdown timer that is currently at 72 hours. Nobody knows what happens when it reaches zero, and the timer is displayed on every victim's BCI without their ability to dismiss it.",
      "A black-hat hacker has begun selling pre-loaded Momento rounds containing BCI rootkit malware. A single non-fatal hit gives the buyer complete access to the target's neural interface. The rounds are being marketed to stalkers, corporate espionage operators, and worse."
    ],
    ammunition_type: ["7.62mm electromagnetic data-encoded"],
    tags: ["weapon", "exotic", "BCI", "data-weapon", "psychological", "information", "tier 3"]
  },
  {
    id: id(),
    name: "Vanta Ordnance Void Screamer VS-3 'Banshee Wire'",
    type: "weapon",
    aliases: ["Banshee Wire", "VS-3", "Void Screamer", "The Keen"],
    category: "exotic",
    description: "A hybrid sonic-kinetic weapon that fires a monofilament wire at hypersonic speed, creating a devastating sonic shockwave along the wire's flight path. The Void Screamer launches a 50-meter spool of carbon-nanotube monofilament that unspools at Mach 2, generating a focused sonic boom in a narrow corridor along its trajectory.\n\nVanta Ordnance designed the VS-3 to combine the cutting capability of monofilament with the area-of-effect characteristics of sonic weapons. The wire itself is nearly invisible and sharp enough to cut through light armor, while the shockwave it generates stuns and disorients targets within 5 meters of the flight path. The result is a weapon that cuts and concusses simultaneously.\n\nThe Banshee Wire's firing mechanism is a compressed-gas launcher that deploys the monofilament spool through a magnetic guide that controls the wire's trajectory. The wire is single-use — after deployment, it falls to the ground as a nearly invisible hazard until collected. Post-engagement cleanup is as important as the firing itself, as loose monofilament can cause accidental amputations.",
    manufacturer: "VANTA ORDNANCE",
    tier_availability: "Tier 4+",
    legality: "Military restricted — monofilament weapons authorization",
    base_technologies: ["Hypersonic monofilament deployment", "Sonic shockwave generation from wire velocity", "Carbon-nanotube monofilament engineering"],
    specifications: "caliber: N/A — 50-meter carbon-nanotube monofilament spool\neffective_range: 10-50 meters (wire length)\nrate_of_fire: 1 spool per 4 seconds (reload required)\ncapacity: 6 spools\nweight: 5.8 kg\npower_source: Compressed-gas launcher, 6 deployments per cartridge",
    tactical_use: "The Banshee Wire is deployed in ambush scenarios and corridor denial where its dual cutting/stunning effect maximizes disruption. The monofilament cuts through the first target it contacts, while the sonic shockwave incapacitates nearby personnel. In enclosed spaces, the shockwave reflects off walls and amplifies, extending the weapon's area of effect. Operators must exercise extreme caution with post-deployment wire location — several friendly-fire incidents have involved personnel walking into deployed monofilament.",
    cultural_context: "The Void Screamer represents Vanta Ordnance's continued innovation at the intersection of conventional and exotic weapons technology. The weapon's dramatic deployment — a screaming wire that cuts and stuns — has earned it a reputation that exceeds its tactical utility. The phrase 'getting wired' has entered Meridian 88 slang to describe being caught in an inescapable trap, and the distinctive shriek of a hypersonic monofilament deployment is one of the most recognizable sounds in urban combat.",
    known_users: ["Vanta Ordnance special operations", "Urban ambush specialists"],
    story_hooks: [
      "A deployed Banshee Wire was left uncollected in a pedestrian corridor. By the time it was discovered, nine people had walked through it, suffering injuries ranging from deep lacerations to partial amputations. The operator who deployed it claims the engagement was sanctioned — the cleanup crew claims they were never dispatched.",
      "Someone has developed a guided monofilament spool that uses micro-thrusters to steer the wire around corners and through doorways. The modified VS-3 can deploy its wire through non-linear paths, making the weapon effective around corners and through obstacles."
    ],
    ammunition_type: ["50-meter carbon-nanotube monofilament spool"],
    tags: ["weapon", "exotic", "monofilament", "sonic", "hybrid", "tier 4"]
  },
  {
    id: id(),
    name: "Tessera Chronos Field Disruptor CFD-1 'Lag Spike'",
    type: "weapon",
    aliases: ["Lag Spike", "CFD-1", "Chronos Disruptor", "Slow Zone"],
    category: "exotic",
    description: "A classified experimental weapon that projects a localized temporal distortion field — an area approximately 3 meters in diameter where the passage of time is slowed relative to the surrounding environment. Targets within the field experience subjective time normally, but external observers see them moving at approximately one-tenth normal speed.\n\nTessera's quantum physics division developed the Chronos Disruptor based on theoretical work suggesting that gravitational manipulation at extreme intensities can produce time dilation effects. The CFD-1 does not stop time — it creates a steep temporal gradient that slows all processes within the field, from chemical reactions to neural signaling to projectile trajectories.\n\nThe weapon is Tessera's most classified project, with fewer than five operational units in existence. The temporal distortion field persists for approximately 10 seconds before the quantum effects collapse, and the energy required to maintain even this brief distortion requires a power system that occupies a small vehicle. The man-portable designation is generous — the weapon is backpack-mounted and requires a two-person crew.",
    manufacturer: "TESSERA",
    tier_availability: "Tier 5",
    legality: "Classified — existence officially denied",
    base_technologies: ["Localized temporal field generation", "Gravitational time dilation manipulation", "Quantum temporal distortion engineering"],
    specifications: "caliber: N/A — projected temporal distortion field\neffective_range: 10-40 meters (field projection point)\nrate_of_fire: 1 field per 120 seconds (quantum field regeneration)\ncapacity: 3 fields per power cell\nweight: 38 kg (weapon and power system, two-person crew)\npower_source: Experimental quantum power cell, non-rechargeable",
    tactical_use: "The Lag Spike is deployed to freeze high-value targets in relative time, allowing operators to approach, restrain, and extract them while they are effectively immobilized. From the target's perspective, they see the world suddenly accelerate — operators appear to move at ten times normal speed, and they are restrained before they can react. The weapon has also been used to freeze incoming projectiles in flight, creating a brief window where a protected area is effectively immune to ballistic threats.",
    cultural_context: "The CFD-1's existence is unknown to the general population of Meridian 88. Among the handful of individuals who have encountered its effects, the experience is described as profoundly disorienting — the world seems to jump forward, with gaps in memory and perception that feel like a corrupted recording. Tessera's quantum physics division treats the project with religious secrecy, and personnel involved in its development are forbidden from acknowledging its existence even within Tessera's own corporate structure.",
    known_users: ["Tessera quantum operations team (estimated 3-5 personnel)"],
    story_hooks: [
      "A security camera in a Tier 4 corridor recorded footage that appears to show time moving at different speeds in different parts of the frame — people on the left side of the image moving normally while people on the right appear frozen. The footage has been classified and the camera's storage has been seized.",
      "A CFD-1 field collapsed prematurely during a classified operation, and the temporal gradient's sudden equalization produced a burst of exotic radiation. Three operators are experiencing time at a slightly different rate than the surrounding world — they are aging faster than normal, and Tessera cannot fix it."
    ],
    ammunition_type: ["Temporal distortion field"],
    tags: ["weapon", "exotic", "temporal", "classified", "experimental", "tier 5", "time"]
  },
  {
    id: id(),
    name: "Axiom Systems Binder Launcher BL-6 'Cocoon'",
    type: "weapon",
    aliases: ["Cocoon", "BL-6", "Binder Launcher", "Web Spinner"],
    category: "exotic",
    description: "A projectile weapon that fires fast-hardening polymer capsules that shatter on impact and release expanding filaments that entangle and immobilize the target. The Cocoon's polymer hardens within 2 seconds of air exposure, transitioning from a sticky web to a rigid cage that holds the target in whatever position they were captured.\n\nAxiom Systems developed the BL-6 as a cost-effective alternative to their more sophisticated Ferrofluid Projector for scenarios where simple immobilization is sufficient. The polymer capsules require no power, no remote activation, and no specialized training. The operator fires at the target, the capsule breaks, the filaments expand, and the polymer hardens. Capture achieved.\n\nThe hardened polymer has a tensile strength comparable to steel cable and resists cutting by conventional edged tools. Liberation requires either a specific chemical solvent (included with each weapon kit) or power tools. This has created situations where captured subjects remain immobilized for extended periods when solvent is unavailable or deliberately withheld.",
    manufacturer: "AXIOM SYSTEMS",
    tier_availability: "Tier 2+",
    legality: "Licensed — law enforcement and security",
    base_technologies: ["Fast-hardening polymer encapsulation", "Expanding filament deployment", "Chemical-release immobilization"],
    specifications: "caliber: 40mm polymer capsule\neffective_range: 10-60 meters\nrate_of_fire: 1 capsule per 2 seconds\ncapacity: 6-round revolving cylinder\nweight: 4.6 kg loaded\npower_source: None — chemical hardening, no power required",
    tactical_use: "The Cocoon is deployed as a low-cost capture weapon for law enforcement and security operations where live capture is required. The weapon is simple enough to be used by minimally-trained personnel, and the polymer capsules have an indefinite shelf life, making the BL-6 suitable for organizations with limited logistics support. The hardened polymer's resistance to escape makes the Cocoon effective against augmented targets — even cybernetically-enhanced strength struggles against a material with steel-cable tensile strength.",
    cultural_context: "The BL-6 has become the most widely-deployed non-lethal weapon in Meridian 88, largely because it is cheap, simple, and effective. This ubiquity has created problems — the weapon's deliberate withholding of solvent has become a form of punishment, with captured individuals left immobilized in hardened polymer for hours or days. Reports of dehydration, hypothermia, and suffocation in cocoon captures have led to mandatory solvent-carry regulations that are inconsistently enforced.",
    known_users: ["Law enforcement agencies", "Corporate security", "Community defense organizations"],
    story_hooks: [
      "A cocooned individual was found dead after 36 hours of immobilization — the arresting officers claim they were waiting for transport, but the solvent was found unused in their vehicle. The death is being investigated as negligent homicide.",
      "Someone has modified Cocoon capsules to include a contact sedative in the polymer matrix. Targets are immobilized and rendered unconscious simultaneously, making them completely defenseless. The modified capsules are being sold to human trafficking operations."
    ],
    ammunition_type: ["40mm fast-hardening polymer capsule"],
    tags: ["weapon", "exotic", "non-lethal", "capture", "polymer", "tier 2", "affordable"]
  },
  {
    id: id(),
    name: "Arcturus Defense Solutions Zero Point Emitter ZPE-1 'Nihil'",
    type: "weapon",
    aliases: ["Nihil", "ZPE-1", "Zero Point Emitter", "The Nothing"],
    category: "exotic",
    description: "Arcturus's most classified weapons project — a device that extracts energy from quantum vacuum fluctuations and releases it as a destructive pulse of undifferentiated energy. The Nihil does not fire a projectile, generate a beam, or project a field. It simply causes a volume of space to experience a catastrophic energy release derived from the quantum vacuum itself.\n\nThe ZPE-1 targets a point in space and triggers a cascade of quantum vacuum energy extraction that produces a burst of heat, radiation, and kinetic force approximately equivalent to 10 kilograms of conventional explosive. The effect appears to violate conservation of energy — the energy comes from the quantum vacuum, which is everywhere, inexhaustible, and theoretically accessible to anyone who understands the physics.\n\nArcturus has gone to extraordinary lengths to prevent the ZPE-1's underlying principles from being understood. The weapon itself is a sealed unit with no user-serviceable components. When its power cell is exhausted, the entire weapon is returned to Arcturus for recharging. No operator has ever seen the weapon's internal mechanism. The five units in existence are tracked by satellite and protected by kill-switch charges that will destroy the weapon if it is tampered with.",
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    tier_availability: "Tier 5",
    legality: "Classified — existential-risk technology, existence denied",
    base_technologies: ["Quantum vacuum energy extraction", "Zero-point energy cascade triggering", "Sealed classified weapons architecture"],
    specifications: "caliber: N/A — quantum vacuum energy release\neffective_range: 20-100 meters\nrate_of_fire: 1 pulse per 300 seconds (quantum field stabilization)\ncapacity: 5 pulses per sealed power cell\nweight: 45 kg (weapon system)\npower_source: Classified sealed unit, returned to Arcturus for servicing",
    tactical_use: "The Nihil is not a tactical weapon. It exists as a proof of concept and as an ultimate deterrent. Its deployment signifies that Arcturus considers a situation critical enough to reveal the existence of zero-point energy weapons — a technology whose mere existence would reshape the global power balance. Each firing is authorized by Arcturus's executive board personally, and the weapon has been fired fewer than a dozen times in total.",
    cultural_context: "The ZPE-1 does not officially exist, and Arcturus denies its development. However, intelligence agencies across Meridian 88 have documented energy releases at Arcturus test sites that match no known weapon profile — bursts of destructive energy with no projectile, no beam, and no identifiable energy source. These incidents are referred to in classified intelligence as 'vacuum events,' and their implications are considered a Tier 1 strategic concern by every major corporation.",
    known_users: ["Arcturus executive authority only"],
    story_hooks: [
      "An intelligence analyst has compiled evidence from multiple vacuum events and published a paper theorizing that zero-point energy weapons exist. The paper was retracted within hours, the analyst has disappeared, and every copy of the research has been scrubbed from accessible networks. Almost every copy.",
      "One of the five ZPE-1 units has stopped transmitting its satellite tracking signal. Arcturus has activated every asset at its disposal to locate the weapon. If the sealed unit is breached and its operating principles are reverse-engineered, the monopoly on zero-point energy extraction ends — and an inexhaustible energy source becomes available to whoever can build the next one."
    ],
    ammunition_type: ["Quantum vacuum energy pulse"],
    tags: ["weapon", "exotic", "quantum", "zero-point", "classified", "existential-risk", "tier 5"]
  }
];

// ─── WRITE ALL WEAPONS ─────────────────────────────────────────────────

const allWeapons = [...sniperRifles, ...supportWeapons, ...exoticWeapons];
let written = 0;
let skipped = 0;

for (const weapon of allWeapons) {
  if (writeEntity(weapon)) {
    written++;
  } else {
    skipped++;
  }
}

console.log(`\nDone. Wrote ${written} files, skipped ${skipped} existing.`);
console.log(`Total weapons defined: ${allWeapons.length}`);
console.log(`  Sniper Rifles: ${sniperRifles.length}`);
console.log(`  Support Weapons: ${supportWeapons.length}`);
console.log(`  Exotic Weapons: ${exoticWeapons.length}`);
