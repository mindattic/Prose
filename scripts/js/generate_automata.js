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
// AUTOMATA DATA - Soulless war machines, lab constructs, mechanical nightmares
// ============================================================

const automata = [

  // ===================== SPIDER PLATFORMS (25) =====================
  {
    name: "Arcturus KS-4 'Knitter'",
    type: "automaton",
    classification: "Spider Platform — Antipersonnel",
    aliases: ["Knitter", "Leggy", "The Weaver"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The KS-4 is a six-legged antipersonnel platform standing roughly 1.2 meters at the thorax, designed for urban suppression operations in confined spaces where wheeled or tracked drones cannot operate. Each leg terminates in a hardened tungsten-carbide spike capable of puncturing standard floor plating, allowing the unit to climb vertical surfaces and traverse ceilings with unsettling fluidity. The primary weapon system is a ventral-mounted flechette dispersal unit that fires downward in a cone pattern — the KS-4 is designed to climb above its targets and rain shrapnel from overhead, exploiting the psychological and tactical vulnerability of attacks from unexpected angles.\n\nField reports consistently describe the KS-4 as one of the most psychologically disturbing platforms in active deployment. The leg articulation produces a rapid clicking sound on hard surfaces that carries through enclosed spaces, and the unit's movement pattern — fast lateral repositioning interrupted by periods of absolute stillness — triggers deep predator-recognition responses in human observers. Arcturus engineers reportedly designed the movement algorithm to maximize target stress responses before engagement, though official documentation describes this as an incidental effect of optimized terrain navigation.\n\nThe nickname 'Knitter' derives from the sound of multiple KS-4 units operating in coordinated sweep patterns through corridor systems — the overlapping click-click-click of dozens of tungsten legs reportedly resembles the sound of knitting needles. Veterans of KS-4 encounters universally describe the experience as among the worst of their careers.",
    tier_availability: "Tier 4+",
    legality: "Military — restricted deployment authorization required",
    autonomy_level: "Semi-autonomous with remote override",
    dimensions: "1.8m leg span, 1.2m thorax height, 0.6m body length",
    weight: "38 kg",
    power_source: "Hydrogen fuel cell, 72-hour operational endurance",
    locomotion: "Hexapod articulated legs, tungsten-carbide tips, ceiling/wall capable",
    armament: ["Ventral flechette dispersal unit (180-degree cone, 15m effective range)", "Leg-tip puncture capability (anti-soft-target)"],
    sensors: ["Thermal imaging array", "Acoustic triangulation", "Vibration detection through contact surfaces"],
    countermeasures: "Electromagnetic pulse disrupts fuel cell management systems. Legs are mechanically simple — physical obstruction with cables or netting can immobilize individual units. Acoustic signature is distinctive and provides early warning.",
    known_deployments: ["GLMZ Undercity clearance operations (2183)", "Arcturus corporate facility defense demonstrations", "Rumored Shelf district suppression incidents (unconfirmed)"],
    story_hooks: [
      "A batch of KS-4 units has gone missing from an Arcturus shipment — twelve units, enough to lock down an entire district. Someone is sitting on a mechanical nightmare waiting to happen.",
      "A KS-4 has been found in the Shelf with its friend-or-foe identification completely removed, set to engage anything that moves. Someone reprogrammed it. The question is whether they're testing it or selling it."
    ],
    cultural_context: "The KS-4 has become a symbol of corporate willingness to deploy genuinely terrifying weapons against civilian populations. Graffiti depicting stylized spider silhouettes appears frequently in districts where KS-4 deployments have occurred, accompanied by the phrase 'They came from the ceiling.' Anti-corporate protest art frequently uses the KS-4 image.",
    tags: ["automaton", "spider", "antipersonnel", "weapon", "war", "corporate", "arcturus", "drone", "violence", "tier 4"]
  },
  {
    name: "TESSERA Widow Mk. III",
    type: "automaton",
    classification: "Spider Platform — Area Denial",
    aliases: ["Widow", "Glass Spider", "The Patience"],
    manufacturer: "TESSERA",
    description: "The Widow Mk. III is TESSERA's contribution to the spider-platform arms race — a four-legged area denial automaton that doesn't chase targets but waits for them. The unit deploys to a designated location, anchors itself to the ceiling or wall using adhesive pads and mechanical clamps, then enters a dormant state that can last weeks. When its sensor suite detects unauthorized movement within its kill zone, it activates with zero warm-up time, dispensing a burst of monofilament wire from spinnerets mounted beneath its chassis. The wire, thinner than human hair and strong enough to sever unaugmented limbs, creates an instantaneous web across corridors and doorways.\n\nThe Widow's genius — and horror — is its patience. Unlike aggressive pursuit platforms, it imposes no energy cost on the deploying force. It simply waits, consuming negligible power in dormant mode, until something enters its space. TESSERA markets it as a 'persistent denial asset,' which is corporate language for a machine that turns rooms into death traps indefinitely. Recovery teams entering areas where Widows have been deployed must sweep every surface above eye level before proceeding.\n\nThe Mk. III variant added a secondary capability that has drawn particular condemnation: acoustic luring. The unit can replay recorded human vocalizations — distress calls, crying, calls for help — to draw targets into its engagement zone. TESSERA's documentation describes this as 'target vectoring through acoustic stimulus.' Everyone else calls it bait.",
    tier_availability: "Tier 5",
    legality: "Prohibited — classified as indiscriminate weapon under Meridian Accords",
    autonomy_level: "Fully autonomous — no operator required after deployment",
    dimensions: "1.4m leg span when deployed, 0.3m body profile when dormant",
    weight: "22 kg",
    power_source: "Solid-state battery, 6-month dormant endurance, 4-hour active",
    locomotion: "Quadruped with adhesive pads and mechanical anchors",
    armament: ["Monofilament wire dispensers (8 spinnerets, 200m total wire capacity)", "Acoustic lure system"],
    sensors: ["Passive infrared detection", "Air displacement sensors", "Ground vibration pickups"],
    countermeasures: "Thermal scanning reveals dormant units against ambient temperature. Monofilament wire is visible under UV light. Acoustic lures can be identified by spectral analysis showing recording artifacts.",
    known_deployments: ["TESSERA facility denial operations", "Black market units recovered in Shelf district", "Unconfirmed use in corporate assassination operations"],
    story_hooks: [
      "Someone has been seeding Widows throughout the Underworld tunnel network. Not TESSERA — the serial numbers are filed off. Whoever is doing this has access to military hardware and wants the tunnels closed to everyone.",
      "A child in the Shelf followed a crying sound into a condemned building. They survived, barely. The Widow that almost killed them was broadcasting a recording of their mother's voice. How did it get that recording?"
    ],
    cultural_context: "The Widow is perhaps the most hated automaton in GLMZ. Its use of acoustic lures — particularly recordings of children and injured people — has made it a focal point for anti-automaton advocacy groups. The phrase 'Don't follow the crying' has entered Shelf survival vocabulary.",
    tags: ["automaton", "spider", "area denial", "monofilament", "weapon", "war", "tessera", "corporate", "violence", "tier 5"]
  },
  {
    name: "Ouroboros EN-8 'Fiddleback'",
    type: "automaton",
    classification: "Spider Platform — Infrastructure Sabotage",
    aliases: ["Fiddleback", "Pipe Spider", "The Plumber"],
    manufacturer: "OUROBOROS ENERGY",
    description: "The EN-8 was originally designed as an infrastructure maintenance drone — a small, eight-legged platform capable of traversing pipe systems, conduit networks, and ventilation shafts to perform inspection and minor repair tasks in spaces too confined for human workers. Ouroboros Energy deployed thousands of them across GLMZ's power grid infrastructure. Then someone in the defense division realized that a drone designed to navigate inside the walls of buildings could be trivially weaponized.\n\nThe combat variant replaces the repair toolkit with a shaped thermite charge capable of cutting through structural steel, a nerve agent dispersal capsule rated for 40 cubic meters of enclosed space, or a simple incendiary package. The unit enters a building through the utility infrastructure — water pipes, electrical conduits, ventilation — navigates to a designated location using its memorized facility map, and detonates. The target never sees it. They may hear faint scratching in the walls in the minutes before detonation. They may not.\n\nOuroboros officially denies the combat variant exists. Ouroboros unofficially sells them to anyone with Tier 4 clearance and a willingness to sign documentation that contains the phrase 'infrastructure stress testing equipment.' The maintenance variant remains in widespread legitimate use, which means every building in GLMZ already has pathways designed for exactly this type of platform to traverse.",
    tier_availability: "Tier 4+ (combat variant); Tier 2+ (maintenance variant)",
    legality: "Maintenance variant: Licensed. Combat variant: Does not officially exist",
    autonomy_level: "Fully autonomous — waypoint navigation with terminal action",
    dimensions: "0.15m body diameter, 0.4m leg span",
    weight: "1.8 kg (maintenance); 2.4 kg (combat)",
    power_source: "Micro lithium cell, 8-hour endurance",
    locomotion: "Octopod, pipe-interior and conduit-rated",
    armament: ["Payload bay: thermite cutter OR nerve agent capsule OR incendiary package (combat variant)", "None (maintenance variant)"],
    sensors: ["Pipe-wall sonar mapping", "Gas detection array", "Magnetic orientation"],
    countermeasures: "Pipe-interior motion sensors can detect traversal. Acoustic monitoring of utility infrastructure reveals characteristic leg-contact patterns. Utility access point sealing prevents entry but also blocks legitimate maintenance drones.",
    known_deployments: ["Thousands of maintenance units active across GLMZ infrastructure", "Combat variant: officially zero. Unofficially, at least fourteen building fires in the last two years match the thermite variant's signature"],
    story_hooks: [
      "The players' safehouse has an Ouroboros maintenance drone access point in the utility room. Someone has been using it to send Fiddlebacks into the building — not combat variants, just watchers. Someone is mapping the interior. The question is what comes next.",
      "An Ouroboros whistleblower wants to leak documentation proving the combat variant exists. The leak needs to happen physically because digital channels are compromised. The whistleblower is being hunted by things that move inside the walls."
    ],
    cultural_context: "The Fiddleback represents a uniquely terrifying threat because the infrastructure it exploits is everywhere and impossible to seal without also cutting off power, water, and ventilation. The phrase 'something in the walls' has taken on literal meaning in GLMZ.",
    tags: ["automaton", "spider", "sabotage", "infiltration", "weapon", "ouroboros", "corporate", "stealth", "tier 4"]
  },
  {
    name: "Ringo AX-12 'Harvester'",
    type: "automaton",
    classification: "Spider Platform — Heavy Assault",
    aliases: ["Harvester", "Daddy Longlegs", "The Reaper"],
    manufacturer: "RINGO CorpoNation",
    description: "The AX-12 is what happens when a CorpoNation decides that subtlety is no longer cost-effective. Standing 3.5 meters tall on eight articulated legs, each tipped with vibro-cutting blades that double as locomotion and close-combat weapons, the Harvester is a walking atrocity designed for open-field suppression of massed infantry and light vehicles. The central thorax houses a rotary autocannon fed from an internal ammunition drum, while four of the eight legs can independently target and engage threats while the remaining four maintain locomotion — the unit literally walks and kills simultaneously without interrupting either function.\n\nRingo developed the AX-12 for agricultural pacification operations in disputed farming zones outside GLMZ, where land disputes between corporate holdings occasionally escalate into armed conflicts involving hundreds of combatants. The Harvester was designed to end those conflicts quickly and with minimal Ringo personnel risk. One AX-12 can suppress an area the size of a city block. Two can hold a district. The agricultural pacification framing is technically accurate and morally bankrupt — the machine harvests people.\n\nThe psychological impact of the AX-12 cannot be overstated. Its movement is deliberately designed to be visible and loud — heavy hydraulic actuators produce a rhythmic thudding that can be heard blocks away, and the unit's height ensures it is visible above most urban structures. Ringo's behavioral engineers explicitly designed the platform as a terror weapon: the AX-12 doesn't need to kill everyone, it needs to make everyone run.",
    tier_availability: "Tier 5",
    legality: "Military restricted — Ringo internal deployment only",
    autonomy_level: "Remote operated with autonomous threat response",
    dimensions: "3.5m standing height, 6m leg span, 2.2m thorax length",
    weight: "2,800 kg",
    power_source: "Diesel-electric hybrid, 48-hour operational endurance",
    locomotion: "Octopod heavy assault legs, vibro-cutting blade tips",
    armament: ["Central rotary autocannon (20mm, 600 rpm)", "8x vibro-cutting leg blades", "Thorax-mounted smoke/tear gas dispensers"],
    sensors: ["360-degree LIDAR", "Thermal imaging", "Acoustic gunfire detection", "Ground-penetrating radar"],
    countermeasures: "Leg joints are vulnerable to shaped charges. Diesel fuel supply is flammable if penetrated. Electronic warfare can disrupt remote operator link, forcing unit into defensive autonomous mode which is less tactically effective. Terrain with overhead cover negates height advantage.",
    known_deployments: ["Ringo agricultural zone pacification (multiple incidents, 2180-present)", "GLMZ perimeter defense demonstrations", "One confirmed deployment inside city limits during the 2184 Shelf Uprising"],
    story_hooks: [
      "A Harvester has been spotted in the Shelf. Not deployed — parked. Someone drove it there, shut it down, and left. It's sitting in a warehouse like a coiled spring. The neighborhood is terrified. Nobody knows who owns it or when it wakes up.",
      "Ringo is field-testing a new AX-12 variant with full autonomy — no remote operator. The test zone is a 'condemned' Shelf district that Ringo claims is evacuated. It isn't."
    ],
    cultural_context: "The Harvester is the automaton that made the phrase 'mechanical nightmare' literal. Survivors of the 2184 Shelf deployment describe the sound of its approach as the worst thing they've ever heard. Anti-Ringo graffiti frequently depicts the AX-12 standing over small human figures.",
    tags: ["automaton", "spider", "heavy assault", "weapon", "war", "ringo", "corporate", "violence", "terror", "tier 5"]
  },
  {
    name: "Crucible Industries CR-2 'Trapdoor'",
    type: "automaton",
    classification: "Spider Platform — Ambush",
    aliases: ["Trapdoor", "Floor Spider", "Surprise"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CR-2 is a flat, disc-shaped platform roughly 0.8 meters in diameter that lies flush against floors, roads, or any horizontal surface, using adaptive camouflage plating to match its surroundings visually. Six short, powerful legs are folded beneath the chassis in rest mode. When a target steps on or near the unit, it detonates upward — the legs launch the platform vertically with enough force to throw an adult human, while simultaneously deploying a ring of monofilament cutting edges around the disc perimeter. The effect is a combination of explosive force and lateral laceration delivered from directly underfoot.\n\nCrucible designed the CR-2 as a mine replacement — cheaper to produce, recoverable if not triggered, and capable of repositioning itself if its location is compromised. Unlike traditional mines, the Trapdoor can be programmed with target discrimination parameters, theoretically reducing civilian casualties. In practice, the discrimination system is crude and relies on weight thresholds, meaning anyone over 45 kilograms is a valid target by default.\n\nThe most disturbing feature of the CR-2 is its ability to self-relocate after failed engagement attempts. If a target steps near but not on the unit, it can fold its legs, scuttle to a new position, and re-camouflage — meaning areas that have been swept for Trapdoors may not stay clear. Clearance teams report the psychologically exhausting experience of knowing the ground they just checked may have rearranged itself behind them.",
    tier_availability: "Tier 3+",
    legality: "Prohibited in civilian areas; licensed for military use",
    autonomy_level: "Fully autonomous — deploy and forget",
    dimensions: "0.8m diameter, 0.06m profile when deployed flat",
    weight: "12 kg",
    power_source: "Solid-state battery, 30-day dormant endurance",
    locomotion: "Hexapod folding legs, low-profile scuttle mode",
    armament: ["Vertical launch mechanism (400N upward force)", "Perimeter monofilament cutting ring"],
    sensors: ["Pressure detection", "Seismic vibration analysis", "Passive thermal"],
    countermeasures: "Ground-penetrating radar reveals metallic disc. UV light may reveal camouflage edge seams. Magnetic anomaly detection effective at close range. Extreme cold disables battery and prevents launch.",
    known_deployments: ["Perimeter defense around Crucible industrial sites", "Black market units found in Shelf gang territorial disputes", "Unconfirmed use in corporate assassination operations"],
    story_hooks: [
      "A street in the Circuit has been seeded with Trapdoors — seven people injured in two days. The units have non-standard programming: they're targeting augmented individuals specifically, ignoring baseline humans.",
      "Someone is selling reprogrammed CR-2 units in the Shelf with the weight threshold dropped to 20 kilograms. That's child-weight. The seller claims they're for pest control."
    ],
    cultural_context: "The CR-2 has made the simple act of walking through certain districts a calculated risk. The phrase 'watch your step' has lost all humor in areas where Trapdoor deployments have occurred. Children in the Shelf are taught to throw heavy objects ahead of them when traversing unfamiliar ground.",
    tags: ["automaton", "spider", "mine", "ambush", "weapon", "war", "crucible", "stealth", "tier 3"]
  },
  {
    name: "Arcturus KS-7 'Nursery'",
    type: "automaton",
    classification: "Spider Platform — Swarm Carrier",
    aliases: ["Nursery", "Mother", "Egg Sac"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The KS-7 is a large spider platform — 2 meters at the thorax — that carries no weapons of its own. Instead, its bloated abdominal section contains twenty-four miniature spider drones, each the size of a human fist, which it deploys through ventral hatches. The miniatures are single-use antipersonnel units equipped with a shaped micro-charge and enough autonomy to navigate toward the nearest heat signature and detonate on contact. The KS-7 itself serves as a mobile deployment platform, navigating to a tactically advantageous position before birthing its payload.\n\nThe deployment sequence is the source of the platform's universally despised reputation. The KS-7 anchors itself to a wall or ceiling, its abdominal section distends visibly, and the miniature units emerge in rapid sequence — scuttling out from beneath the parent platform in a spreading wave that eyewitnesses consistently describe as 'hatching.' The miniatures move fast, are difficult to target individually due to their size, and the sheer number of simultaneous threats overwhelms human threat-processing capacity. Most targets freeze.\n\nArcturus marketing materials describe the KS-7 as a 'force multiplication platform enabling distributed engagement across complex terrain.' Combat veterans describe it as the worst thing they've ever seen come out of a laboratory. The miniature units have a functional lifespan of approximately four minutes after deployment, after which their batteries die and they become inert — but four minutes of two dozen fist-sized explosive spiders is an eternity.",
    tier_availability: "Tier 5",
    legality: "Military restricted",
    autonomy_level: "Parent: semi-autonomous. Miniatures: fully autonomous",
    dimensions: "2.0m thorax height, 3.2m leg span",
    weight: "180 kg fully loaded",
    power_source: "Hydrogen fuel cell (parent), micro lithium cells (miniatures)",
    locomotion: "Hexapod (parent), quadruped micro-legs (miniatures)",
    armament: ["24x miniature spider drones with shaped micro-charges"],
    sensors: ["Thermal array (parent)", "Basic thermal homing (miniatures)"],
    countermeasures: "Destroying the parent before deployment neutralizes all miniatures. After deployment, area-effect weapons (flame, EMP, concussive blast) are most effective against swarm. Individual miniatures can be stomped but this requires extraordinary composure.",
    known_deployments: ["Arcturus weapons demonstrations", "Rumored deployment in Underworld clearance operations"],
    story_hooks: [
      "A KS-7 was recovered from the Underworld with its payload bay empty — all twenty-four miniatures deployed. But there are no casualties. Where did they go? What are they waiting for?",
      "Someone has modified a KS-7 to carry surveillance drones instead of explosive miniatures. It's been mapping the inside of a CorpoNation headquarters from the ventilation system for weeks."
    ],
    cultural_context: "The KS-7 has inspired a specific phobia in GLMZ's population — the fear of small, fast-moving things in peripheral vision. Mental health clinics in districts near Arcturus testing facilities report elevated rates of arachnophobia that directly correlate with KS-7 demonstration schedules.",
    tags: ["automaton", "spider", "swarm", "carrier", "weapon", "war", "arcturus", "corporate", "terror", "tier 5"]
  },
  {
    name: "Lazarus LP-3 'Clinic Spider'",
    type: "automaton",
    classification: "Spider Platform — Medical Enforcement",
    aliases: ["Clinic Spider", "The Nurse", "Needles"],
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "Lazarus Pharmaceuticals does not build weapons. The LP-3 is a medical compliance enforcement platform. That is the official position, and Lazarus's legal department has defended it in seventeen separate Meridian Quorum hearings. The LP-3 is a four-legged platform the size of a large dog, equipped with hypodermic injection systems, restraint clamps, and a pharmacy bay capable of carrying eight doses of various pharmaceutical agents. Its designated purpose is the safe restraint and involuntary medication of patients in psychiatric crisis, quarantine enforcement, and medical debt collection — yes, collection.\n\nIn practice, the LP-3 chases people down and injects them with whatever its pharmacy bay has been loaded with. The restraint clamps lock around limbs with hydraulic force sufficient to immobilize augmented individuals. The hypodermic system can deliver intramuscular injections through clothing and light armor. Lazarus deploys them in medical facilities, pharmaceutical distribution centers, and — most controversially — in the field during what Lazarus terms 'community health operations' that look indistinguishable from raids.\n\nThe LP-3's most disturbing operational mode is 'persistent compliance,' in which the unit follows a designated individual at a fixed distance, injecting them on a schedule, for as long as its pharmacy bay holds doses. This mode was designed for outpatient psychiatric medication compliance. It has been documented in use against medical debt holders, corporate whistleblowers, and union organizers, loaded with sedatives or disorienting agents rather than therapeutic medications.",
    tier_availability: "Tier 3+",
    legality: "Licensed as medical device — not classified as weapon",
    autonomy_level: "Semi-autonomous with medical authority override",
    dimensions: "0.7m height, 1.1m leg span",
    weight: "28 kg",
    power_source: "Rechargeable lithium cell, 36-hour endurance",
    locomotion: "Quadruped, indoor/outdoor rated, stair capable",
    armament: ["4x hypodermic injection units", "Hydraulic limb restraint clamps", "8-dose pharmacy bay"],
    sensors: ["Biometric identification (facial, gait, BCI signature)", "Vital sign monitoring at range", "Pharmaceutical absorption spectroscopy"],
    countermeasures: "Pharmacy bay can be physically damaged to prevent injection. Biometric spoofing can cause targeting errors. Restraint clamps have a mechanical release accessible to third parties. Unit is not armored — small arms fire is effective.",
    known_deployments: ["Lazarus psychiatric facilities", "Corporate pharmaceutical distribution centers", "Shelf district 'community health operations'", "Medical debt enforcement actions"],
    story_hooks: [
      "A Clinic Spider has been following a Shelf resident for three days, injecting them every six hours with an unknown substance. Lazarus claims it's court-ordered psychiatric medication. The target says they've never been diagnosed with anything.",
      "Someone has reprogrammed a fleet of LP-3 units and loaded them with a street drug. They're loose in Old Harbor, randomly injecting pedestrians. It's either terrorism or the worst marketing campaign in history."
    ],
    cultural_context: "The LP-3 has made the Shelf's population deeply distrustful of any medical outreach program. The sight of a Clinic Spider triggers flight responses in communities where Lazarus conducts operations. Health workers without automaton escorts report being welcomed. Those with LP-3 units are met with barricades.",
    tags: ["automaton", "spider", "medical", "pharmaceutical", "restraint", "lazarus", "corporate", "control", "tier 3"]
  },
  {
    name: "Vantablack VS-1 'Cameraman'",
    type: "automaton",
    classification: "Spider Platform — Surveillance",
    aliases: ["Cameraman", "Eye Spider", "The Watcher"],
    manufacturer: "VANTABLACK MEDIA",
    description: "Vantablack Media's VS-1 is technically not a weapon. It's a mobile surveillance and content acquisition platform — a small, six-legged spider drone roughly the size of a dinner plate, painted matte black, equipped with high-definition optical and audio recording equipment capable of capturing broadcast-quality footage in any lighting condition. It climbs walls, traverses ceilings, and can remain motionless for hours while recording. Vantablack deploys hundreds of them across GLMZ as mobile news-gathering assets.\n\nThe VS-1 becomes a weapon in context. Vantablack uses them to document events in the Shelf and lower-tier districts — violence, poverty, corporate operations, personal moments — and broadcasts that content without consent to audiences in the Spires and upper tiers. The surveillance is constant, invasive, and profitable. Residents of documented districts have no opt-out mechanism and no legal recourse, as Vantablack's broadcast licenses cover 'public interest documentation' in all tier zones. Getting caught doing something embarrassing, illegal, or simply private on a Cameraman feed can destroy lives.\n\nThe VS-1 also serves as a force multiplier for other operations. Vantablack sells real-time feeds from its Cameraman network to corporate security firms, law enforcement, and — through intermediaries — to criminal organizations. The same drone that films a Shelf family's dinner broadcasts their security patterns to whoever pays for the feed. Vantablack's position is that the data is public-domain observation. Everyone else's position is that it's industrialized stalking.",
    tier_availability: "Tier 2+",
    legality: "Licensed — broadcast documentation equipment",
    autonomy_level: "Semi-autonomous with editorial override",
    dimensions: "0.25m body diameter, 0.5m leg span",
    weight: "1.2 kg",
    power_source: "Micro lithium cell, 18-hour endurance, solar recharge capable",
    locomotion: "Hexapod, wall/ceiling capable, near-silent operation",
    armament: ["None — but feeds enable violence by others"],
    sensors: ["4K optical recording", "Directional microphone array", "Low-light amplification", "BCI proximity detection"],
    countermeasures: "Small arms fire. Signal jamming blocks feed transmission but not local recording. Anti-surveillance paint disrupts optical camouflage. Most Shelf residents simply smash them on sight.",
    known_deployments: ["GLMZ citywide — estimated 800+ active units", "Concentrated in Shelf, Circuit, and Old Harbor districts"],
    story_hooks: [
      "A Cameraman captured footage of a corporate assassination. Vantablack is auctioning the footage to the highest bidder — the killer, the victim's family, and three news organizations are all bidding. The players need to get it first.",
      "Someone is hacking Cameraman feeds and replacing real footage with deep fakes. An innocent person has been framed for murder using fabricated Cameraman evidence."
    ],
    cultural_context: "Cameramen are the most-destroyed automata in GLMZ. Shelf residents kill them on sight with thrown objects, improvised EMP devices, and simple stomping. Vantablack considers the attrition rate a cost of doing business. The phrase 'smile for the spider' is Shelf sarcasm for the constant surveillance.",
    tags: ["automaton", "spider", "surveillance", "media", "vantablack", "corporate", "control", "privacy", "tier 2"]
  },
  {
    name: "Arcturus KS-9 'Orchard'",
    type: "automaton",
    classification: "Spider Platform — Chemical Dispersal",
    aliases: ["Orchard", "Crop Duster", "Fog Machine"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The KS-9 is a medium-sized hexapod platform designed for area chemical dispersal operations. Its thorax-mounted pressurized tank system can carry 40 liters of liquid agent — tear gas, nerve agent, incapacitating fog, defoliant, or any other aerosolizable chemical — and disperse it through rotating nozzles that create a uniform fog across a 200-meter radius. The unit is sealed against its own payload, allowing it to operate at ground zero of the chemical cloud it produces.\n\nThe KS-9's spider platform allows it to navigate the uneven terrain of Shelf districts, rubble fields, and tunnel systems where wheeled chemical dispersal vehicles cannot operate. It can climb to elevated positions to maximize dispersal coverage, enter buildings through windows and service corridors, and operate in environments already saturated with its own chemical payload. Multiple KS-9 units operating in coordination can maintain a persistent chemical environment across large areas indefinitely, rotating between dispersal and reloading cycles.\n\nWhat makes the KS-9 particularly concerning is its interchangeable payload system. The same platform that disperses tear gas for crowd control can, with a tank swap taking less than ninety seconds, disperse lethal nerve agents. The mechanical interface is identical. The authorization difference exists only in paperwork.",
    tier_availability: "Tier 4+",
    legality: "Licensed for non-lethal payloads; lethal payloads military restricted",
    autonomy_level: "Remote operated with autonomous navigation",
    dimensions: "1.5m thorax height, 2.4m leg span",
    weight: "95 kg empty, 135 kg loaded",
    power_source: "Hydrogen fuel cell, 24-hour endurance",
    locomotion: "Hexapod all-terrain",
    armament: ["40L pressurized chemical dispersal system", "Rotating nozzle array (200m dispersal radius)"],
    sensors: ["Wind direction/speed sensors", "Chemical concentration monitoring", "GPS positioning"],
    countermeasures: "Gas masks and sealed environments negate payload. Destroying the tank creates a concentrated but non-dispersed chemical hazard. EMP disables navigation but may trigger emergency tank dump.",
    known_deployments: ["Shelf district crowd control operations", "Agricultural pest control (claimed purpose)", "Underworld tunnel clearance"],
    story_hooks: [
      "Three KS-9 units have been positioned around a Shelf neighborhood with unknown payloads. They haven't activated. Whoever placed them is sending a message — or waiting for a signal.",
      "A KS-9 was found in the water treatment facility with its tank connected directly to the intake system. The tank was empty. What was in it, and is it already in the water supply?"
    ],
    cultural_context: "The KS-9 represents the industrialization of chemical warfare against civilian populations. Its dual-use payload system means that every crowd control deployment carries the implicit threat that the next tank could be lethal.",
    tags: ["automaton", "spider", "chemical", "dispersal", "weapon", "war", "arcturus", "corporate", "violence", "tier 4"]
  },
  {
    name: "TESSERA TW-6 'Jumper'",
    type: "automaton",
    classification: "Spider Platform — Rapid Assault",
    aliases: ["Jumper", "Tick", "Leapfrog"],
    manufacturer: "TESSERA",
    description: "The TW-6 is a compact, four-legged spider platform with massively oversized rear leg actuators that give it a vertical leap capacity of twelve meters and a horizontal jump range of twenty. It weighs only 15 kilograms and carries a single shaped explosive charge in its ventral cavity. The operational concept is simple: the Jumper identifies a target, closes distance through a series of rapid leaps that make it nearly impossible to track visually, lands on the target, clamps its legs around whatever body part it contacts, and detonates.\n\nThe TW-6 is a disposable assassin. Each unit costs roughly what a mid-tier augmentation procedure costs, making them economically viable as single-use weapons against high-value targets. They can be deployed from rooftops, vehicles, or simply released at ground level to leap toward their designated target. The jump trajectory is calculated to be erratic — no two jumps follow the same arc — making interception by automated defense systems extremely difficult.\n\nTESSERA markets the TW-6 as an 'asymmetric threat response platform' designed to neutralize targets that conventional weapons cannot reach. The reality is that it's a mechanical suicide bomber shaped like a nightmare, and its small size means it can be concealed in bags, boxes, or vehicles until deployment.",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    autonomy_level: "Fully autonomous after target designation",
    dimensions: "0.3m body length, 0.6m leg span (compact), 1.0m leg span (jump position)",
    weight: "15 kg",
    power_source: "Single-use lithium cell, 20-minute operational life",
    locomotion: "Quadruped with enhanced leap actuators (12m vertical, 20m horizontal)",
    armament: ["Ventral shaped explosive charge", "Leg clamp system"],
    sensors: ["Target-lock optical tracking", "Inertial navigation for jump calculation"],
    countermeasures: "Shotgun-type weapons most effective against airborne units. Electronic countermeasures can disrupt target lock between jumps. The 20-minute battery life means evasion is theoretically possible if you can run long enough.",
    known_deployments: ["TESSERA security operations", "Black market units documented in corporate assassination attempts"],
    story_hooks: [
      "A box containing six dormant TW-6 units was intercepted in a shipping container bound for the Shelf. The target designations are already programmed. Six names. Six people who don't know they're marked.",
      "A TW-6 jumped onto a target and failed to detonate — its charge was a dud. The target is alive with a mechanical spider clamped to their chest, and the removal process is the most terrifying bomb disposal job in the city."
    ],
    cultural_context: "The TW-6 has made rooftops and elevated positions feel unsafe for the first time. In a city where looking up meant seeing spires, looking up now also means checking for something that might be looking back.",
    tags: ["automaton", "spider", "assassin", "explosive", "weapon", "war", "tessera", "corporate", "tier 4"]
  },

  // ===================== WALKING TANKS (20) =====================
  {
    name: "Arcturus HW-1 'Goliath'",
    type: "automaton",
    classification: "Walking Tank — Heavy Fire Support",
    aliases: ["Goliath", "Big Iron", "The Argument"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The HW-1 is a four-legged walking weapons platform standing 4.5 meters tall and weighing twelve metric tons. It is, in the simplest terms, a tank that walks. The primary armament is a turret-mounted 40mm autocannon with 400 rounds of mixed ammunition, supplemented by coaxial machine guns and a dorsal-mounted missile pod carrying four anti-vehicle guided missiles. The legs provide mobility across terrain that would stop tracked vehicles — rubble fields, stairways wide enough to accommodate the 3-meter stride, and slopes up to 45 degrees.\n\nArcturus designed the Goliath for urban warfare scenarios where conventional armored vehicles are channeled into predictable routes by narrow streets and barricades. The walking chassis allows the HW-1 to step over barricades, climb rubble piles, and navigate through building interiors if doorways are widened — which the 40mm autocannon can accomplish in seconds. The fire control system integrates with Arcturus's command network for coordinated operations with infantry and aerial assets.\n\nThe Goliath's presence on a battlefield is definitive. There is no infantry weapon in common use that can penetrate its composite armor. There is no cover in a standard urban environment that its autocannon cannot destroy. Its arrival at a conflict typically ends the conflict — either through direct engagement or through the immediate surrender of anyone who understands what they're looking at.",
    tier_availability: "Tier 5",
    legality: "Military — Arcturus internal deployment only",
    autonomy_level: "Crew-operated (2 operators) with autonomous defensive systems",
    dimensions: "4.5m height, 3.0m stride, 3.5m hull width",
    weight: "12,000 kg",
    power_source: "Diesel-electric hybrid, 200km range",
    locomotion: "Quadruped heavy walker, all-terrain",
    armament: ["Turret-mounted 40mm autocannon (400 rounds)", "2x coaxial 7.62mm machine guns", "Dorsal missile pod (4x anti-vehicle guided missiles)", "Smoke/chaff dispensers"],
    sensors: ["360-degree sensor fusion", "Thermal imaging", "Millimeter-wave radar", "Laser rangefinder"],
    countermeasures: "Anti-tank weapons (HEAT, kinetic penetrators) remain effective against leg joints. Mines designed for tracked vehicles may not trigger under walking platforms — specialized anti-walker mines exist but are rare. Leg actuator damage can immobilize the unit but the turret remains operational.",
    known_deployments: ["Arcturus military operations outside GLMZ", "GLMZ perimeter defense", "2184 Shelf Uprising (confirmed, two units)"],
    story_hooks: [
      "A Goliath has broken down in the Shelf — leg actuator failure. The crew evacuated. The turret is still active on autonomous defense mode, shooting anything that approaches within 200 meters. The neighborhood is trapped.",
      "Intelligence suggests Arcturus is moving three Goliaths into position around the Circuit district. This isn't a demonstration — this is preparation for something."
    ],
    cultural_context: "The Goliath is the symbol of CorpoNation military power. Its silhouette appears on anti-corporate propaganda worldwide. The phrase 'send in the argument' has become slang for overwhelming, disproportionate force response.",
    tags: ["automaton", "walker", "tank", "heavy", "weapon", "war", "arcturus", "corporate", "military", "tier 5"]
  },
  {
    name: "Ringo RT-4 'Oxcart'",
    type: "automaton",
    classification: "Walking Tank — Logistics and Suppression",
    aliases: ["Oxcart", "Pack Mule", "The Bulk"],
    manufacturer: "RINGO CorpoNation",
    description: "The RT-4 is a six-legged walking platform designed as a combined logistics carrier and area suppression system. Its primary function is transporting supplies, ammunition, and equipment across terrain too rough for wheeled vehicles — a walking warehouse that carries three tons of cargo in its armored hull. Its secondary function is ensuring that anything threatening the cargo doesn't survive the attempt.\n\nThe suppression system consists of four independently targeting automated turrets mounted at the hull corners, each carrying a 12.7mm heavy machine gun with 2,000 rounds. The turrets operate on a completely autonomous threat-response system — the RT-4 doesn't need a human to tell it to shoot, it identifies threats to its cargo and eliminates them with mechanical efficiency. This dual-purpose design means Ringo can deploy supply lines through hostile territory without dedicated escort forces.\n\nThe RT-4's most controversial feature is its cargo-priority behavioral programming. The unit will sacrifice terrain, retreat from engagement, and even abandon wounded friendly personnel to protect its cargo. In one documented incident, an RT-4 walked through a group of injured Ringo employees to escape an ambush rather than stopping to provide assistance. Ringo's position is that the cargo value exceeded the medical liability. This position was expressed publicly.",
    tier_availability: "Tier 4+",
    legality: "Corporate licensed — Ringo operational use",
    autonomy_level: "Fully autonomous logistics and defense",
    dimensions: "3.0m height, 4.0m hull length, 2.5m width",
    weight: "8,500 kg empty, 11,500 kg loaded",
    power_source: "Diesel-electric, 300km range",
    locomotion: "Hexapod all-terrain walker",
    armament: ["4x autonomous 12.7mm turrets (2,000 rounds each)"],
    sensors: ["Perimeter threat detection array", "Cargo integrity monitors", "Terrain mapping LIDAR"],
    countermeasures: "Turrets have limited depression angle — prone attackers within 5 meters are in a blind spot. Cargo bay access panels can be forced if turrets are disabled. EMP affects autonomous targeting but not locomotion.",
    known_deployments: ["Ringo supply operations in disputed zones", "Agricultural district logistics", "Post-disaster supply distribution"],
    story_hooks: [
      "An RT-4 has gone off-route and is walking a circuit through the Shelf, turrets active, refusing remote commands. Its cargo bay is sealed. Whatever is inside, Ringo wants it back badly enough to send retrieval teams. The Shelf wants to know why.",
      "A hijacked RT-4 has been reprogrammed to deliver supplies to Shelf communities. It shoots at anyone wearing corporate security insignia. Someone has turned Ringo's logistics weapon into a Robin Hood machine."
    ],
    cultural_context: "The RT-4 embodies the CorpoNation philosophy that cargo is worth more than people. Its cargo-priority programming is cited in every anti-automaton rights argument as evidence that machines cannot be trusted with human-adjacent decisions.",
    tags: ["automaton", "walker", "tank", "logistics", "weapon", "war", "ringo", "corporate", "tier 4"]
  },
  {
    name: "TESSERA TH-2 'Warden'",
    type: "automaton",
    classification: "Walking Tank — Urban Pacification",
    aliases: ["Warden", "Street Judge", "The Wall"],
    manufacturer: "TESSERA",
    description: "The TH-2 is a bipedal walking platform standing 3.8 meters tall, designed specifically for urban crowd control and district pacification operations. Unlike the multi-legged designs favored by other manufacturers, TESSERA deliberately chose a humanoid bipedal configuration for the Warden — not for any mechanical advantage, but for psychological impact. A thing that walks like a person but stands twice as tall and is made of armor plate triggers a specific kind of fear that multi-legged platforms do not.\n\nThe Warden's armament is non-lethal by specification and lethal by application. Its primary systems include a chest-mounted sonic cannon capable of incapacitating everyone within a 60-degree cone at ranges up to 100 meters, arm-mounted tear gas launchers, and hip-mounted rubber-bullet gatling systems. All of these are classified as non-lethal. All of them can kill at close range, at sustained exposure, or when used against individuals with certain medical conditions or augmentations. TESSERA's liability documentation runs to 400 pages and absolves the CorpoNation of responsibility in all such cases.\n\nThe Warden walks through crowds. That is its primary tactical application. It walks, slowly and deliberately, through groups of people, and its presence — the height, the sound, the knowledge of what its weapons do — creates a wake of dispersal. TESSERA deploys them in pairs, walking parallel routes through target districts, compressing populations between them.",
    tier_availability: "Tier 4+",
    legality: "Licensed for crowd control — non-lethal designation",
    autonomy_level: "Remote operated with autonomous crowd response",
    dimensions: "3.8m height, 1.8m shoulder width, bipedal",
    weight: "4,200 kg",
    power_source: "Hydrogen fuel cell, 36-hour endurance",
    locomotion: "Bipedal heavy walker, paved surface optimized",
    armament: ["Chest-mounted directional sonic cannon", "2x arm-mounted tear gas launchers", "2x hip-mounted rubber-bullet gatling systems (800 rpm)", "Shoulder-mounted floodlights and loudspeakers"],
    sensors: ["Crowd density analysis", "Facial recognition array", "Acoustic gunshot detection"],
    countermeasures: "Bipedal design is inherently less stable than multi-legged platforms — cable traps across legs at ankle height can topple the unit. Sonic cannon requires forward facing; attacking from behind negates primary weapon. Rubber-bullet gatlings jam in heavy rain.",
    known_deployments: ["GLMZ district pacification operations (regular)", "Shelf protest suppression", "Circuit district lockdowns during corporate summit events"],
    story_hooks: [
      "A Warden unit has been hacked to broadcast a revolutionary manifesto through its loudspeakers while walking its suppression route. TESSERA can't shut it down remotely without revealing the vulnerability. The walking propaganda machine is becoming a rallying point.",
      "Two Wardens are walking a compression pattern through a Shelf neighborhood — standard pacification. But this time, the neighborhood has prepared. Cable traps, signal jammers, and a plan. If they can bring down one Warden on live feed, it changes everything."
    ],
    cultural_context: "The Warden's bipedal design was a calculated psychological choice that backfired culturally. By making the platform humanoid, TESSERA gave anti-automaton artists a perfect metaphor: the faceless giant walking through the neighborhood, indifferent to the people it displaces.",
    tags: ["automaton", "walker", "bipedal", "crowd control", "weapon", "tessera", "corporate", "violence", "tier 4"]
  },

  // ===================== MECH SUITS (20) =====================
  {
    name: "Arcturus XF-3 'Ironside'",
    type: "automaton",
    classification: "Powered Exoskeleton — Heavy Combat",
    aliases: ["Ironside", "Can Opener", "Walking Coffin"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The XF-3 is a fully enclosed powered exoskeleton that turns a single operator into something approaching a light armored vehicle. The operator climbs into the suit through a rear-mounted hatch, connects their BCI to the control interface, and gains access to a 2.8-meter armored chassis with hydraulic strength amplification that allows them to lift 800 kilograms, sprint at 45 km/h, and absorb small-arms fire without slowing. The suit carries an arm-mounted 20mm rotary cannon fed from a back-mounted ammunition drum and a shoulder-mounted grenade launcher with six rounds.\n\nThe experience of operating the XF-3 is, by all accounts, intoxicating and destructive to long-term mental health in roughly equal measure. The BCI interface provides full sensory feedback — the operator feels the suit as their own body, experiencing its strength and durability as personal attributes. Arcturus's neural engineering team designed the feedback loop to be deliberately euphoric during combat operations, producing a neurochemical response that operators describe as the most intense experience of their lives. The crash after disconnection is proportionally devastating. XF-3 addiction — the compulsive need to be inside the suit — is a recognized medical condition among Arcturus combat personnel.\n\nThe nickname 'Walking Coffin' has a dual meaning: it describes what the suit does to enemies, and what the suit does to operators. The BCI interface, over repeated connections, gradually erodes the operator's ability to perceive their unaugmented body as adequate. Long-term operators report feeling weak, slow, and fragile outside the suit — not because they are, but because the suit's sensory feedback has recalibrated their baseline expectations. Some operators eventually refuse to exit.",
    tier_availability: "Tier 5",
    legality: "Military restricted — operator certification required",
    autonomy_level: "Operator controlled with BCI interface",
    dimensions: "2.8m height, 1.5m shoulder width",
    weight: "680 kg (suit only), 760 kg with operator",
    power_source: "High-density hydrogen cell, 8-hour combat endurance",
    locomotion: "Bipedal powered exoskeleton, 45 km/h sprint",
    armament: ["Arm-mounted 20mm rotary cannon (400 rounds)", "Shoulder-mounted 6-round grenade launcher", "Hydraulic-assisted melee capability"],
    sensors: ["Full suite sensor fusion via BCI", "Threat detection AI assistant", "360-degree awareness overlay"],
    countermeasures: "Anti-materiel rifles can penetrate at joint seams. EMP disrupts BCI interface, causing operator disorientation. The rear hatch is the weakest armor point. Operators experiencing BCI addiction exhibit predictable behavioral patterns that can be exploited.",
    known_deployments: ["Arcturus special operations", "Corporate facility breach operations", "VIP protection details"],
    story_hooks: [
      "An XF-3 operator has gone AWOL inside their suit. They haven't disconnected in eleven days. The suit's life support can sustain them indefinitely. They're wandering the Underworld, responding to threats that may or may not exist, and Arcturus wants them recovered alive — the suit is worth more than the operator.",
      "A black market XF-3 with its safety limiters removed has appeared in the Shelf. The operator is using it to protect a neighborhood. The problem is that the unregulated BCI feedback is driving them toward a psychotic break, and when it happens, the neighborhood they're protecting becomes the target."
    ],
    cultural_context: "The XF-3 represents the ultimate augmentation fantasy and its ultimate nightmare — becoming something more than human at the cost of being unable to be human again. It is both the most desired and most feared piece of military hardware in GLMZ.",
    tags: ["automaton", "exoskeleton", "mech", "heavy combat", "weapon", "war", "arcturus", "corporate", "bci", "addiction", "tier 5"]
  },
  {
    name: "Crucible Industries CF-1 'Carapace'",
    type: "automaton",
    classification: "Powered Exoskeleton — Industrial/Combat",
    aliases: ["Carapace", "Bug Suit", "Hardhat"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CF-1 was designed as an industrial exoskeleton for hazardous material handling — allowing workers to operate in environments with extreme heat, chemical exposure, and structural collapse risk. The suit provides full environmental sealing, strength amplification for lifting 500 kilograms, and ablative armor that was originally intended to protect against industrial accidents. Crucible then sold the identical platform to security contractors with the industrial tools replaced by weapon mounts.\n\nThe combat variant of the CF-1 is less sophisticated than Arcturus's XF-3 — it uses manual controls rather than BCI interface, making it slower to respond but without the addiction risks. The operator controls the suit through a combination of joystick inputs and motion-capture sensors that translate body movements into suit movements with a barely perceptible lag. The armament is modular: arm mounts accept anything from heavy machine guns to cutting torches to chemical sprayers, depending on the mission profile.\n\nThe CF-1's real market advantage is price. At roughly one-fifth the cost of an XF-3, it has become the exoskeleton of the middle market — corporate security firms, well-funded mercenary units, and even some wealthy criminal organizations operate CF-1 units. The lower cost means lower capability, but it also means far more of them in circulation, which creates its own kind of threat.",
    tier_availability: "Tier 3+ (industrial); Tier 4+ (combat)",
    legality: "Industrial: Licensed. Combat: Licensed with security contractor registration",
    autonomy_level: "Operator controlled — manual interface",
    dimensions: "2.4m height, 1.3m shoulder width",
    weight: "420 kg",
    power_source: "Swappable lithium cell packs, 6-hour endurance",
    locomotion: "Bipedal powered exoskeleton, 30 km/h sprint",
    armament: ["Modular arm mounts (weapon-specific)", "No integrated armament — all mission-configurable"],
    sensors: ["Environmental hazard detection", "Basic thermal imaging", "Structural integrity scanner"],
    countermeasures: "Manual controls create response lag exploitable by faster opponents. Battery packs are externally mounted and vulnerable to small arms. No BCI integration means no enhanced situational awareness.",
    known_deployments: ["Industrial sites across GLMZ", "Corporate security operations", "Black market criminal operations"],
    story_hooks: [
      "A CF-1 industrial variant has been stolen from a construction site and crudely up-armored with welded scrap metal. Someone in the Shelf is building a poor man's war machine, and the welds won't hold forever.",
      "Six CF-1 combat variants have been delivered to an address in the Circuit. The shipping documentation says 'industrial equipment.' Someone is assembling a private army."
    ],
    cultural_context: "The CF-1 is the democratization of mechanized infantry — for better and worse. Its affordability means that the power gap between CorpoNations and well-funded resistance groups has narrowed, but it also means that every turf war could involve walking armor.",
    tags: ["automaton", "exoskeleton", "mech", "industrial", "combat", "weapon", "crucible", "tier 3", "tier 4"]
  },
  {
    name: "Arcturus XF-1 'Jackal'",
    type: "automaton",
    classification: "Powered Exoskeleton — Scout/Recon",
    aliases: ["Jackal", "Speed Suit", "Glass Cannon"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The XF-1 is Arcturus's light exoskeleton — a semi-enclosed frame that sacrifices the XF-3's heavy armor for speed and agility. The operator wears the Jackal like an external skeleton, with articulated leg and arm amplifiers that allow sprinting at 70 km/h, jumping 6 meters vertically, and maintaining that pace for hours. The armor is minimal — enough to stop pistol calibers and fragmentation, but rifle rounds and above penetrate freely.\n\nThe Jackal is designed for reconnaissance, rapid flanking, and hit-and-run operations. Its armament is light: a forearm-mounted submachine gun and a back-mounted micro-missile pod with six rounds. The tactical concept is to move faster than the enemy can track, strike, and relocate before effective return fire can be organized. Operators describe the experience as running at highway speed through urban terrain while the world blurs.\n\nThe XF-1 uses a limited BCI interface — less immersive than the XF-3, focused on motor coordination rather than full sensory feedback. This reduces addiction risk but creates a different problem: operators report difficulty transitioning between suit-enhanced reflexes and normal human reaction times. Former Jackal pilots describe ordinary walking as feeling 'like moving through mud' and frequently display restless, agitated behavior when grounded.",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    autonomy_level: "Operator controlled with BCI-assisted motor coordination",
    dimensions: "2.2m height (operator dependent), minimal profile",
    weight: "120 kg (frame only)",
    power_source: "High-density lithium cell, 4-hour endurance",
    locomotion: "Bipedal enhanced — 70 km/h sprint, 6m vertical leap",
    armament: ["Forearm-mounted SMG (300 rounds)", "Back-mounted micro-missile pod (6 rounds)"],
    sensors: ["Motion tracking HUD", "Threat proximity alerts", "Terrain mapping for high-speed navigation"],
    countermeasures: "Light armor makes operator vulnerable to any serious weapon. High-speed movement requires predictable terrain — obstacles, cables, and uneven ground at speed are lethal. EMP disables motor coordination assist, leaving operator in a heavy frame they can barely move.",
    known_deployments: ["Arcturus reconnaissance operations", "VIP extraction missions", "Urban pursuit operations"],
    story_hooks: [
      "A stolen Jackal is being used for smash-and-grab robberies across the Circuit — the operator hits at 70 km/h and is gone before security responds. The problem is they're getting faster and more reckless. The BCI coordination assist is failing, and the next run might end with the operator pancaked against a wall.",
      "Arcturus is recruiting Jackal pilots from the Shelf — offering full augmentation and combat training in exchange for five-year service contracts. The recruits don't know that the BCI interface has a 30% long-term neurological complication rate."
    ],
    cultural_context: "The Jackal represents a seductive promise: speed, power, escape velocity from the Shelf. Recruitment posters in lower-tier districts feature the XF-1 prominently. The reality of neurological damage and psychological dependency is not featured.",
    tags: ["automaton", "exoskeleton", "mech", "scout", "speed", "weapon", "war", "arcturus", "corporate", "bci", "tier 4"]
  },

  // ===================== KAMIKAZE K-9 UNITS (20) =====================
  {
    name: "Arcturus CK-1 'Fetch'",
    type: "automaton",
    classification: "Canine Platform — Explosive Delivery",
    aliases: ["Fetch", "Good Boy", "The Last Walk"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The CK-1 is a quadruped automaton roughly the size and shape of a large dog, built on an articulated chassis that mimics canine locomotion with disturbing accuracy. It weighs 35 kilograms, of which 8 kilograms is a shaped explosive charge housed in its chest cavity. The operational concept is brutally simple: the CK-1 is deployed toward a target, runs at speeds up to 50 km/h across open ground, navigates obstacles using its quadruped agility, reaches the target, and detonates.\n\nArcturus deliberately designed the CK-1 to look like a dog. This was not an incidental consequence of quadruped locomotion optimization — the chassis proportions, the head shape, even the movement gait were engineered to exploit the human psychological association between dogs and trust, safety, and companionship. In field testing, targets consistently hesitated before engaging CK-1 units, despite being briefed on the platform's nature. The hesitation window averages 1.3 seconds — enough time for the CK-1 to close 18 meters.\n\nThe CK-1 is painted matte black with no identifying markings. Some operators have reported that the units occasionally exhibit behavioral patterns that were not programmed — pausing to investigate objects, tilting their heads at sounds, sitting when stationary for extended periods. Arcturus attributes these to emergent behavior in the locomotion algorithm's idle state. The behaviors make the CK-1 more effective, not less, because they make it look more like a real dog until the moment it isn't.",
    tier_availability: "Tier 4+",
    legality: "Military restricted",
    autonomy_level: "Fully autonomous after target designation",
    dimensions: "0.65m shoulder height, 1.1m length",
    weight: "35 kg (8 kg explosive payload)",
    power_source: "Lithium cell, 2-hour sprint endurance",
    locomotion: "Quadruped canine-mimetic, 50 km/h sprint, obstacle navigation",
    armament: ["8 kg shaped explosive charge (chest cavity)", "Detonation on proximity or contact"],
    sensors: ["Target tracking optical", "Terrain navigation", "Proximity detonation trigger"],
    countermeasures: "Small arms fire can disable locomotion but may trigger detonation. Shooting the head disables navigation sensors. EMF jamming disrupts targeting but the unit defaults to nearest-heat-source behavior. The psychological barrier to shooting a dog-shaped object is itself a countermeasure the CK-1 exploits.",
    known_deployments: ["Arcturus military operations", "Perimeter breach operations", "Anti-vehicle attacks"],
    story_hooks: [
      "A CK-1 has been found in the Shelf, powered down, sitting in an alley. A child has been feeding it scraps. The explosive charge is live. Nobody has told the child what it is.",
      "Someone is buying CK-1 units on the black market and removing the explosive charges, reprogramming them as guard dogs. They work perfectly. Until one reverts to factory programming."
    ],
    cultural_context: "The CK-1 is the automaton that generates the most emotional response. The deliberate use of dog mimicry has been condemned by virtually every ethics board that has reviewed it. In the Shelf, where real dogs are rare and valued companions, the CK-1 represents a particular kind of cruelty — the weaponization of trust.",
    tags: ["automaton", "canine", "kamikaze", "explosive", "weapon", "war", "arcturus", "corporate", "terror", "tier 4"]
  },
  {
    name: "TESSERA TK-4 'Hellhound'",
    type: "automaton",
    classification: "Canine Platform — Incendiary",
    aliases: ["Hellhound", "Hot Dog", "Burner"],
    manufacturer: "TESSERA",
    description: "The TK-4 takes the kamikaze canine concept and makes it worse. Instead of a single explosive detonation, the Hellhound carries a napalm dispersal system that ignites on a timed delay after the unit reaches its target area. The TK-4 runs into a designated zone — a building, a crowd, a vehicle cluster — and sprays its entire 6-liter napalm payload through pressurized nozzles along its flanks before igniting it. The unit itself is thermally shielded enough to survive the initial ignition and can continue running while on fire, spreading the conflagration across a wider area before its chassis fails.\n\nThe TK-4 is faster than the CK-1 at 55 km/h but lighter, and its incendiary payload makes it an area weapon rather than a point weapon. It is designed to create chaos rather than destroy specific targets — a burning mechanical dog running through a crowd is a weapon of terror as much as a weapon of destruction. TESSERA's tactical documentation describes its role as 'area denial through thermal saturation and psychological disruption.'\n\nThe most disturbing reports come from the TK-4's termination phase. After payload delivery and ignition, the unit's locomotion system continues operating for 30 to 90 seconds as thermal damage accumulates. Eyewitnesses describe a burning dog-shaped machine running in increasingly erratic patterns, occasionally stumbling, before finally collapsing. The image persists in the memories of everyone who witnesses it.",
    tier_availability: "Tier 5",
    legality: "Prohibited — classified as incendiary weapon under Meridian Accords",
    autonomy_level: "Fully autonomous",
    dimensions: "0.6m shoulder height, 1.0m length",
    weight: "28 kg (6L napalm payload)",
    power_source: "Single-use lithium cell, 30-minute endurance",
    locomotion: "Quadruped canine-mimetic, 55 km/h sprint",
    armament: ["6L pressurized napalm dispersal system", "Thermal ignition system", "Post-ignition mobile dispersal"],
    sensors: ["Area navigation", "Heat avoidance (pre-ignition only)"],
    countermeasures: "Destroying the napalm tank before ignition prevents area effect but creates a localized chemical spill. Water does not extinguish napalm. Shooting the unit may trigger premature ignition. The only reliable countermeasure is stopping it before it reaches the target area.",
    known_deployments: ["TESSERA classified operations", "Unconfirmed reports of black market use in territorial disputes"],
    story_hooks: [
      "Three TK-4 units have been found in a warehouse, pre-loaded and programmed with coordinates inside a Shelf residential block. The warehouse belongs to a company that doesn't exist. The attack is planned for a specific date — the date of a planned anti-corporate protest.",
      "A TK-4 was deployed against a target and malfunctioned — the napalm dispersed but didn't ignite. The target is alive but covered in unignited napalm. One spark and they're dead. They need to get to a decontamination facility across the city without anything producing a flame, spark, or static discharge."
    ],
    cultural_context: "The TK-4 has crossed a line that even other weapon manufacturers acknowledge. Arcturus has publicly condemned its existence — not out of ethics, but because the TK-4 makes all canine platforms look bad and threatens the market for Arcturus's own CK-1.",
    tags: ["automaton", "canine", "kamikaze", "incendiary", "napalm", "weapon", "war", "tessera", "corporate", "terror", "tier 5"]
  },
  {
    name: "Ringo RK-2 'Stray'",
    type: "automaton",
    classification: "Canine Platform — Chemical Dispersal",
    aliases: ["Stray", "Sick Dog", "Patient Zero"],
    manufacturer: "RINGO CorpoNation",
    description: "The RK-2 doesn't explode and it doesn't burn. It walks through populated areas and disperses aerosolized chemical agents from vents along its spine — slowly, continuously, while moving at a pace indistinguishable from a real dog's walking gait. The RK-2 is designed not to be noticed. Its chassis is the most convincingly canine of any platform on the market, with synthetic fur covering, realistic ear and tail articulation, and a gait algorithm that perfectly mimics a mid-sized mixed-breed dog. It even pants.\n\nThe chemical payload varies by mission. Documented loads include incapacitating agents, tracking markers that adhere to skin and clothing, mood-altering pharmaceuticals, and agents that interfere with specific cyberware models. The dispersal rate is calibrated to be below visible threshold — no fog, no smell, no indication that anything is happening. Targets breathe the agent without knowing it. The RK-2 can cover an entire neighborhood in a single patrol loop.\n\nRingo markets the RK-2 for 'population management research' — a phrase that means whatever the buyer needs it to mean. The most chilling documented application involved RK-2 units dispersing a Lazarus-manufactured mood suppressant through a Shelf district in the weeks before a scheduled Ringo facility expansion that required resident displacement. By the time the eviction notices arrived, the population was too chemically docile to resist.",
    tier_availability: "Tier 4+",
    legality: "Does not officially exist",
    autonomy_level: "Fully autonomous — patrol route programming",
    dimensions: "0.55m shoulder height, 0.9m length",
    weight: "22 kg (4L chemical payload)",
    power_source: "Lithium cell, 12-hour patrol endurance",
    locomotion: "Quadruped canine-mimetic with synthetic fur covering, realistic gait",
    armament: ["Spine-mounted aerosolized chemical dispersal vents", "4L interchangeable chemical payload"],
    sensors: ["Crowd density monitoring", "Wind direction for dispersal optimization", "Chemical concentration feedback"],
    countermeasures: "Identifying the RK-2 as non-biological is the primary challenge — close inspection reveals synthetic fur texture and mechanical gait artifacts. Gas masks protect against chemical agents. Air quality monitoring equipment can detect dispersal events after the fact.",
    known_deployments: ["Officially none. Documented evidence of use in Shelf population management, mood suppression before corporate land seizures, and covert pharmaceutical trials on non-consenting populations"],
    story_hooks: [
      "A neighborhood in the Shelf has noticed that everyone feels unusually calm and agreeable lately. A player character with chemical detection augments identifies trace mood suppressants in the air. The source is a 'stray dog' that's been wandering the block for two weeks. People have been petting it.",
      "Someone has loaded an RK-2 with a contagious pathogen instead of a chemical agent. The unit has been walking through the market district for three days. The pathogen's incubation period is ending."
    ],
    cultural_context: "The RK-2 represents the most insidious application of automata technology — a weapon that disguises itself as something people want to protect. In a city where real stray dogs are adopted and cared for by Shelf communities, the RK-2 exploits compassion as an attack vector.",
    tags: ["automaton", "canine", "chemical", "covert", "weapon", "ringo", "corporate", "control", "pharmaceutical", "tier 4"]
  },

  // ===================== AERIAL DRONES (20) =====================
  {
    name: "Arcturus AD-7 'Vulture'",
    type: "automaton",
    classification: "Aerial Drone — Persistent Overwatch",
    aliases: ["Vulture", "Eye in the Sky", "Circler"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The AD-7 is a fixed-wing drone with a 4-meter wingspan that circles at altitudes between 500 and 2,000 meters, providing continuous surveillance and precision strike capability for ground operations. Its electric propulsion system is nearly silent at operational altitude, and its optical systems can read a BCI serial number from 1,500 meters. The Vulture carries two precision-guided munitions — small, but large enough to destroy a vehicle or kill everyone in a room.\n\nThe AD-7's primary value is persistence. A single Vulture can maintain station for 72 hours, watching, recording, and waiting for a target to appear. Multiple units rotating in relay can maintain continuous coverage indefinitely. The knowledge that a Vulture is overhead — and you can't see it, and you don't know if today is the day it decides to drop something — creates a constant, grinding psychological pressure that affects behavior at population scale.\n\nArcturus sells Vulture coverage as a service. Corporate clients subscribe to overwatch packages covering specific districts or facilities, and Arcturus maintains the fleet. This means the drones are always there, watching everything, and the data from their surveillance feeds is Arcturus's to sell separately. The strike capability is the headline feature, but the surveillance data is the profit center.",
    tier_availability: "Tier 4+",
    legality: "Licensed for corporate security use",
    autonomy_level: "Semi-autonomous with remote strike authorization",
    dimensions: "4.0m wingspan, 2.2m length",
    weight: "85 kg",
    power_source: "Solar-supplemented electric, 72-hour endurance",
    locomotion: "Fixed-wing electric propulsion",
    armament: ["2x precision-guided munitions (5 kg warhead each)"],
    sensors: ["High-resolution optical (readable BCI serial at 1,500m)", "Thermal imaging", "Signal intelligence collection", "Persistent tracking algorithms"],
    countermeasures: "Surface-to-air weapons effective but possession is heavily restricted. Signal jamming disrupts strike authorization link but not surveillance. Cloud cover and dense urban canyons limit optical coverage. Operating altitude makes visual detection nearly impossible.",
    known_deployments: ["Continuous coverage over multiple GLMZ districts", "Corporate facility overwatch", "Targeted strike operations (classified)"],
    story_hooks: [
      "A Vulture strike killed three people in the Shelf. Arcturus claims they were valid military targets. Eyewitnesses say they were a family eating dinner. The surveillance footage that would prove either claim is classified.",
      "Someone has figured out how to spoof Vulture targeting data, making the drones designate false targets. Real people at real addresses are receiving invisible death sentences. The spoofing signal is coming from inside Arcturus's own network."
    ],
    cultural_context: "Vultures are invisible but omnipresent, and their presence has changed behavior patterns across GLMZ. People avoid open spaces. Gatherings are held under cover. The sky is no longer neutral — it belongs to whoever can afford the subscription.",
    tags: ["automaton", "aerial", "drone", "surveillance", "strike", "weapon", "war", "arcturus", "corporate", "tier 4"]
  },
  {
    name: "TESSERA TD-3 'Hornet'",
    type: "automaton",
    classification: "Aerial Drone — Swarm Attack",
    aliases: ["Hornet", "Buzzer", "Bad Day"],
    manufacturer: "TESSERA",
    description: "The TD-3 is a palm-sized quadrotor drone carrying a single shaped charge the size of a cigarette lighter. One TD-3 is a nuisance. One hundred TD-3s is a catastrophe. TESSERA sells them in crates of 200, and their tactical value exists entirely in numbers — a swarm of Hornets can saturate an area with individually targeted explosive strikes faster than any defense system can engage them all.\n\nEach Hornet operates on a shared swarm intelligence protocol — units communicate position, target status, and threat data in real time, allowing the swarm to dynamically allocate attacks, avoid redundant targeting, and concentrate on high-value targets identified by the swarm consensus algorithm. A single operator can control an entire swarm through a tablet interface, designating zones and priority targets while the swarm handles individual engagement decisions.\n\nThe TD-3 is cheap enough to be disposable and effective enough to be terrifying. A single Hornet's shaped charge can penetrate light armor and kill an unprotected human. A swarm can overwhelm armored vehicles by targeting vision blocks, antenna arrays, and weapon systems simultaneously. The sound of 200 Hornets activating — a rising buzz that fills the air from every direction — has been described by survivors as the sound of the future coming to kill them.",
    tier_availability: "Tier 3+",
    legality: "Licensed — security contractor use",
    autonomy_level: "Swarm autonomous with operator oversight",
    dimensions: "0.12m rotor span per unit",
    weight: "0.3 kg per unit",
    power_source: "Micro lithium cell, 15-minute flight time per unit",
    locomotion: "Quadrotor",
    armament: ["Shaped micro-charge per unit"],
    sensors: ["Shared swarm optical network", "Acoustic target localization"],
    countermeasures: "Shotgun effective against individual units. EMP disables swarm communication, causing units to default to nearest-target behavior. Physical barriers (closing doors, entering vehicles) provide temporary protection. Rain significantly degrades flight performance. 15-minute battery life means surviving the swarm is theoretically possible.",
    known_deployments: ["Corporate security rapid response", "Black market distribution to criminal organizations and resistance groups", "TESSERA facility defense"],
    story_hooks: [
      "A crate of 200 Hornets has gone missing from a TESSERA shipment. The crate's GPS tracker was disabled. Somewhere in GLMZ, someone has a swarm weapon and no accountability.",
      "A TD-3 swarm attacked a Shelf market — forty people dead. TESSERA claims the units were black market knockoffs. Serial number analysis says otherwise."
    ],
    cultural_context: "The Hornet has democratized aerial warfare in the worst way. A crate costs less than a used vehicle, putting military-grade swarm attack capability in the hands of anyone with enough quanta. The buzzing sound has become a cultural symbol of imminent, indiscriminate violence.",
    tags: ["automaton", "aerial", "drone", "swarm", "explosive", "weapon", "war", "tessera", "corporate", "tier 3"]
  },

  // ===================== UNDERGROUND/TUNNEL HORRORS (15) =====================
  {
    name: "Ouroboros UT-1 'Worm'",
    type: "automaton",
    classification: "Tunnel Platform — Boring/Assault",
    aliases: ["Worm", "Digger", "The Mole"],
    manufacturer: "OUROBOROS ENERGY",
    description: "The UT-1 is a 6-meter-long segmented tunnel-boring automaton originally designed for utility corridor expansion. Its front section houses a diamond-carbide rotating cutting head that can chew through concrete, rock, and structural steel. Behind the cutting head, a series of articulated body segments carry debris removal systems, structural reinforcement dispensers, and — in the combat variant — a passenger compartment for eight fully equipped operators who ride inside the machine as it cuts its way into a target building from below.\n\nThe UT-1 moves at approximately 2 meters per minute through solid concrete, meaning it can breach a building foundation in under ten minutes from an adjacent tunnel. The cutting head produces a distinctive low-frequency vibration that can be felt through floors and walls — experienced residents of the Shelf and Underworld can identify a Worm approach by the tremor in their bones. By the time the vibration is felt, the machine is already close.\n\nThe combat application turns the UT-1 into a breaching weapon. The machine cuts into a basement or foundation, the front section opens, and operators pour out of the machine's body directly into the target structure. The entry point is unexpected, the noise of the cutting head masks approach sounds until the final seconds, and the structural damage to the foundation creates additional hazards for building occupants. Ouroboros sells the combat variant under the designation 'Subsurface Access Platform' with documentation that never uses the word 'weapon.'",
    tier_availability: "Tier 4+",
    legality: "Industrial: Licensed. Combat variant: Corporate restricted",
    autonomy_level: "Operator controlled with autonomous boring navigation",
    dimensions: "6.0m length, 1.8m diameter",
    weight: "14,000 kg",
    power_source: "High-capacity lithium battery array, 4-hour boring endurance",
    locomotion: "Segmented body with track-driven articulation, cutting head propulsion",
    armament: ["Diamond-carbide boring head (weaponizable)", "No dedicated weapons — the machine IS the weapon"],
    sensors: ["Ground-penetrating sonar", "Structural analysis array", "Seismic mapping"],
    countermeasures: "Seismic monitoring detects approach vibration at 50+ meter range. Reinforced foundations significantly slow progress. The machine is vulnerable at the moment of breach when the cutting head opens and operators are exposed. Underground water table can flood the tunnel behind the machine.",
    known_deployments: ["Utility corridor construction", "Corporate facility breach operations (classified)", "Rumored Underworld expansion projects"],
    story_hooks: [
      "Something has been boring tunnels under the Shelf that don't appear on any utility map. The tunnels lead to specific buildings — banks, armories, CorpoNation offices. Someone is building an underground highway for a coordinated assault.",
      "A UT-1 broke through into an Underworld chamber that wasn't on any map. What it found inside was old — older than the city. And it was not empty."
    ],
    cultural_context: "The UT-1 has made basements feel unsafe. In a city where the ground beneath your feet might already be compromised, the knowledge that something can chew through your floor creates a specific kind of architectural anxiety.",
    tags: ["automaton", "tunnel", "boring", "breach", "weapon", "ouroboros", "corporate", "underworld", "tier 4"]
  },
  {
    name: "Arcturus UT-5 'Lamprey'",
    type: "automaton",
    classification: "Tunnel Platform — Pursuit",
    aliases: ["Lamprey", "Pipe Chaser", "The Follow"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The UT-5 is a 3-meter serpentine automaton designed to pursue targets through tunnel systems, pipe networks, and confined spaces where humanoid or multi-legged platforms cannot operate. Its body is segmented into twelve articulated sections, each with independent locomotion — allowing the Lamprey to navigate right-angle bends, vertical shafts, and pipe junctions without slowing. The front section contains a high-definition camera, thermal sensor, and a single-use taser discharge system designed to incapacitate targets at contact range.\n\nThe Lamprey was designed for Underworld pursuit operations — chasing targets through the maze of tunnels beneath GLMZ where human pursuers lose their way and larger drones can't fit. The unit's navigation system builds a real-time map of the tunnel network as it moves, meaning every Lamprey deployment expands Arcturus's understanding of the Underworld's layout. This mapping capability may be the unit's actual primary purpose, with the pursuit function serving as justification for sending mapping equipment into territories that would otherwise be inaccessible.\n\nThe psychological impact of the Lamprey is significant. In the enclosed spaces of the Underworld, the sound of something sliding through pipes behind you, getting closer, is described by survivors as worse than being shot at. The unit's articulated movement produces a rhythmic scraping sound that echoes through tunnel systems and makes directional identification difficult.",
    tier_availability: "Tier 4+",
    legality: "Military/corporate restricted",
    autonomy_level: "Fully autonomous pursuit with mapping intelligence",
    dimensions: "3.0m length, 0.3m diameter",
    weight: "45 kg",
    power_source: "Distributed lithium cells, 6-hour endurance",
    locomotion: "Serpentine articulated — pipe/tunnel/shaft rated",
    armament: ["Contact taser (single use, 50,000V)", "Optional: rear-mounted gas dispersal capsule"],
    sensors: ["HD camera", "Thermal imaging", "Acoustic tracking", "3D tunnel mapping LIDAR"],
    countermeasures: "Narrow pipe sections below 0.3m diameter block passage. Sharp bends beyond the articulation range can trap the unit. The single-use taser means it only gets one shot — after discharge, the unit can only pursue and map. Physical barriers across tunnels are effective.",
    known_deployments: ["Underworld pursuit operations", "Tunnel network mapping campaigns", "Pipe system inspection (civilian variant)"],
    story_hooks: [
      "Lampreys have been released into the Underworld in unprecedented numbers — fifty units in a single week. Arcturus claims it's a mapping exercise. Underworld residents believe it's preparation for a major incursion.",
      "A Lamprey returned from an Underworld deployment with footage of something its recognition system couldn't classify. The footage shows a large, biological entity in a deep tunnel section. Arcturus wants to send more Lampreys. The entity might not appreciate the attention."
    ],
    cultural_context: "The Lamprey has made the Underworld's tunnel networks — previously a refuge for those with nowhere else to go — feel actively hostile. The sound of scraping in the pipes has joined the list of things that wake Underworld residents at night.",
    tags: ["automaton", "serpentine", "tunnel", "pursuit", "mapping", "weapon", "arcturus", "corporate", "underworld", "tier 4"]
  },

  // ===================== LAB EXPERIMENTS / PROTOTYPES (20) =====================
  {
    name: "Lazarus BX-0 'Test Subject'",
    type: "automaton",
    classification: "Experimental — Biomechanical Hybrid",
    aliases: ["Test Subject", "The Mistake", "Lab Rat"],
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "The BX-0 is not a product. It is a failed experiment that refuses to stop functioning. Lazarus's bioengineering division attempted to create a hybrid platform — mechanical chassis with biological tissue integration designed to give the automaton self-healing capability. The result was a quadruped platform roughly the size of a wolf, with a mechanical skeleton partially covered in lab-grown muscle tissue that contracts and relaxes in response to electrical stimulation from the internal control system. The biological tissue does regenerate, slowly, giving the BX-0 a limited ability to recover from damage that no purely mechanical platform possesses.\n\nThe failure is in the tissue's behavior. The lab-grown muscle responds to the control system's commands, but it also responds to stimuli the control system doesn't generate — it twitches, contracts asymmetrically, and occasionally drives the mechanical skeleton into movements that weren't commanded. The BX-0 stumbles, jerks, and lurches in ways that are profoundly disturbing to observe. It moves like something in pain, though it has no capacity for pain. The biological tissue occasionally grows in uncontrolled patterns, producing tumor-like masses that must be surgically removed before they interfere with joint articulation.\n\nLazarus has produced twelve BX-0 units, each slightly different as the bioengineering team iterates on the tissue integration process. None of them work correctly. None of them have been decommissioned. They are kept in a sealed laboratory section, powered and active, while researchers continue to study their failures. Lab personnel who work near the BX-0 containment area report persistent unease that they attribute to the sounds — the wet mechanical grinding of biological tissue being driven by electrical impulse against metal bone.",
    tier_availability: "N/A — experimental, not deployed",
    legality: "Exists in regulatory grey area — no classification for biomechanical hybrids",
    autonomy_level: "Basic autonomous locomotion with unpredictable biological interference",
    dimensions: "0.8m shoulder height, 1.4m length (varies by tissue growth)",
    weight: "55-70 kg (varies by tissue mass)",
    power_source: "Internal lithium cell supplemented by biological metabolic processes (minimal)",
    locomotion: "Quadruped mechanical with biological muscle tissue overlay — unpredictable gait",
    armament: ["None standard — mechanical jaw with biological tissue reinforcement capable of 800N bite force"],
    sensors: ["Basic optical", "Biological olfactory tissue (experimental, unreliable)"],
    countermeasures: "Biological tissue is vulnerable to fire, chemical agents, and conventional weapons. Mechanical skeleton requires anti-materiel approaches. The combination means no single countermeasure addresses both components effectively.",
    known_deployments: ["None — laboratory containment only"],
    story_hooks: [
      "One of the twelve BX-0 units has escaped Lazarus containment. It's in the Shelf, and it's doing something none of the others have done: it's hunting. Not because it was programmed to. Because the biological tissue is driving behavior the mechanical system can't override.",
      "A Lazarus researcher wants to leak data proving that the BX-0 program has produced units with rudimentary biological nervous systems — meaning they might actually feel pain. The units have been kept active and operational for two years. If they can feel, what has been done to them constitutes something unprecedented."
    ],
    cultural_context: "The BX-0 exists at the intersection of every fear about automata and biotechnology. It is not alive, but it is not entirely dead. It is not sentient, but its biological components introduce an element of unpredictability that feels like will. It represents a line that most people didn't know could be crossed.",
    tags: ["automaton", "experimental", "biomechanical", "hybrid", "lazarus", "corporate", "horror", "laboratory"]
  },
  {
    name: "Arcturus PX-1 'Phantom'",
    type: "automaton",
    classification: "Experimental — Active Camouflage Platform",
    aliases: ["Phantom", "Ghost Machine", "The Nothing"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The PX-1 is a quadruped platform with a radical design feature: its entire exterior surface is coated in adaptive camouflage plating that bends visible light around the chassis, rendering the unit functionally invisible to the naked eye. The effect is not perfect — there is a faint shimmer when the unit moves, like heat haze, and the system cannot mask the unit's thermal signature or acoustic output. But in visual-dominant engagement scenarios — which describes most human combat — the PX-1 is something that isn't there until it is.\n\nThe platform carries no ranged weapons. Its combat systems are entirely close-range: reinforced jaw mechanism with tungsten-carbide teeth, forelimb-mounted vibro-blades that extend on command, and a neural disruptor pulse emitter on its forehead that can scramble unshielded BCIs at contact range. The tactical concept is assassination: the PX-1 approaches unseen, strikes at close range, and withdraws before the target's companions can identify the threat's location.\n\nArcturus has produced only four PX-1 units, and the program is classified above standard corporate security clearance. The adaptive camouflage system is phenomenally expensive to produce and maintain, and the light-bending effect degrades over time, requiring recalibration after every deployment. The four existing units are reserved for operations that Arcturus considers worth the investment — which means operations that no one is supposed to know happened.",
    tier_availability: "Tier 5 — classified",
    legality: "Does not officially exist",
    autonomy_level: "Fully autonomous assassination platform",
    dimensions: "0.7m shoulder height, 1.3m length",
    weight: "48 kg",
    power_source: "High-density hydrogen cell (camouflage system draws 60% of power budget)",
    locomotion: "Quadruped — silent operation mode available at reduced speed",
    armament: ["Tungsten-carbide jaw mechanism", "2x forelimb vibro-blades", "Forehead-mounted neural disruptor pulse (BCI scrambler, contact range)"],
    sensors: ["Passive thermal", "Acoustic mapping", "Target identification via gait analysis"],
    countermeasures: "Thermal imaging reveals the unit clearly — the camouflage only works on visible light. Acoustic sensors can detect footfall at close range. Shielded BCIs are immune to the neural disruptor. The unit's shimmer effect is detectable by trained observers in good lighting conditions.",
    known_deployments: ["Classified — officially zero"],
    story_hooks: [
      "A series of deaths across GLMZ share a common pattern: close-range trauma, no witnesses, no surveillance footage showing an attacker. Someone with access to a Phantom is using it for contract killing, and the client list is growing.",
      "One of the four PX-1 units has gone missing from Arcturus's classified facility. The other three are accounted for. Arcturus doesn't know if it was stolen or if it left on its own — and they can't admit the program exists to ask for help finding it."
    ],
    cultural_context: "The PX-1 is the automaton that people don't believe exists — which is exactly how Arcturus wants it. Rumors of invisible machines persist in the Shelf and Circuit, dismissed as paranoia by most, confirmed by the few who have survived an encounter and by the many who haven't.",
    tags: ["automaton", "stealth", "camouflage", "assassin", "weapon", "arcturus", "corporate", "classified", "tier 5"]
  },
  {
    name: "TESSERA TX-0 'Mimic'",
    type: "automaton",
    classification: "Experimental — Shape-Memory Chassis",
    aliases: ["Mimic", "Changeling", "That Wasn't There"],
    manufacturer: "TESSERA",
    description: "The TX-0 uses a shape-memory alloy chassis that can reconfigure its external geometry to approximate different objects — a garbage bin, a fire hydrant, a piece of furniture, a crouching human figure. The transformation takes approximately ninety seconds and is limited by the mass and volume of the unit's chassis, but within those constraints, the TX-0 can become part of the urban landscape, indistinguishable from its surroundings to casual observation.\n\nIn its attack configuration, the TX-0 unfolds into a low, multi-limbed platform with four articulated arms ending in cutting tools and a central mass containing a fragmentation charge. The operational concept is ambush: the unit assumes a form appropriate to its environment, waits for a target to enter proximity, reconfigures to attack mode, engages, and either detonates its fragmentation charge or withdraws to reconfigure and wait again.\n\nThe TX-0 program has produced eight units, and TESSERA considers half of them failures — the shape-memory alloy develops 'preferences' over repeated reconfigurations, settling into forms that the control system didn't request. Three units have begun defaulting to a crouching humanoid form between missions, regardless of their programmed disguise. TESSERA's engineers cannot explain why the alloy structure consistently converges on this shape. The units function normally otherwise. They just prefer to look like a person when they're not being told to look like something else.",
    tier_availability: "Tier 5 — experimental",
    legality: "Does not officially exist",
    autonomy_level: "Fully autonomous ambush platform",
    dimensions: "Variable — base volume approximately 0.4 cubic meters",
    weight: "65 kg (constant regardless of configuration)",
    power_source: "High-density lithium cell, 48-hour dormant endurance",
    locomotion: "Multi-limbed in attack configuration, stationary in disguise mode",
    armament: ["4x articulated cutting arms", "Central fragmentation charge (single use)"],
    sensors: ["Acoustic proximity detection", "Weight/vibration sensors", "Passive thermal"],
    countermeasures: "Weight inconsistency — a garbage bin that weighs 65 kg is suspicious. Thermal imaging shows uniform heat distribution inconsistent with normal objects. Magnetic sensors detect alloy signature. The 90-second reconfiguration window is a vulnerability.",
    known_deployments: ["TESSERA laboratory testing", "Unconfirmed field trials"],
    story_hooks: [
      "A TX-0 has been placed in a public space and nobody knows which object it is. TESSERA won't say where because admitting the program exists would be worse than the casualties. The players have to identify which piece of urban furniture in a crowded market is a killing machine.",
      "The three TX-0 units that default to humanoid form have been observed through laboratory cameras apparently looking at each other. TESSERA's lead engineer wants out. Whatever is happening with the shape-memory convergence, it's not in the design specifications."
    ],
    cultural_context: "The TX-0 weaponizes the most mundane aspects of urban life. If a garbage bin might be a killer robot, nothing in the environment can be trusted. Paranoia about everyday objects has always been a symptom of living in GLMZ. The TX-0 makes it rational.",
    tags: ["automaton", "shapeshifter", "ambush", "experimental", "weapon", "tessera", "corporate", "horror", "tier 5"]
  },

  // ===================== SENTRY / TURRET PLATFORMS (15) =====================
  {
    name: "Arcturus ST-4 'Verdict'",
    type: "automaton",
    classification: "Sentry — Automated Kill Zone",
    aliases: ["Verdict", "The Judge", "No Appeal"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The ST-4 is a ceiling or wall-mounted automated turret platform housing a 7.62mm machine gun with 2,000 rounds of ammunition and a target acquisition system that can identify, track, and engage up to twelve targets simultaneously. The unit mounts to any structural surface using expansion bolts, connects to facility power or runs on internal batteries for 48 hours, and begins enforcing a kill zone within seconds of activation. Anything that enters the designated area without a valid transponder signal is engaged.\n\nThe ST-4 is the backbone of corporate facility security across GLMZ. Thousands are deployed in server rooms, executive floors, research laboratories, and warehouse facilities. The transponder-based friend-or-foe system means that employees with valid credentials pass through kill zones without incident, while intruders encounter immediate, accurate, sustained fire. The system has no negotiation mode, no warning shots, and no escalation protocol. Entry without authorization equals engagement.\n\nThe Verdict earned its name from the absolute nature of its targeting decision. Security professionals note that the ST-4's identification system has a false positive rate of 0.03% — statistically negligible until you consider the thousands of units deployed across GLMZ and the thousands of people who pass through their zones daily. That 0.03% represents real people killed by a machine that made a statistical error. Arcturus's legal position is that transponder maintenance is the employee's responsibility.",
    tier_availability: "Tier 3+",
    legality: "Licensed for corporate facility defense",
    autonomy_level: "Fully autonomous — transponder-based authorization",
    dimensions: "0.4m turret profile, 0.6m ammunition housing",
    weight: "35 kg installed",
    power_source: "Facility power with 48-hour battery backup",
    locomotion: "None — fixed mounting",
    armament: ["7.62mm machine gun (2,000 rounds)", "Laser designator for coordinated fire with other ST-4 units"],
    sensors: ["Multi-spectral target acquisition", "Transponder interrogation", "Motion tracking"],
    countermeasures: "Valid transponder (stolen or forged) bypasses targeting. Physical destruction requires approaching the kill zone. Smoke and chaff degrade optical tracking. Power cut disables facility power but engages battery backup. The turret's traversal rate has a maximum — extremely fast targets moving perpendicular to the firing line may outrun the tracking system.",
    known_deployments: ["Corporate facilities across GLMZ (thousands of units)", "Government buildings", "Military installations"],
    story_hooks: [
      "The players need to enter a facility protected by ST-4 turrets. They have three options: steal a transponder, disable the power (including battery backup), or move faster than the tracking system. None of these options are easy.",
      "A firmware update pushed to all ST-4 units in a district has modified the friend-or-foe parameters. Employees with valid transponders are being targeted. Arcturus claims it's a bug. It's happening only in one district, and only to employees of one specific department."
    ],
    cultural_context: "The ST-4 represents the automation of lethal force — a machine that decides who lives and dies based on whether a transponder battery is charged. Corporate employees in GLMZ check their transponders the way previous generations checked their door locks: compulsively, because the cost of forgetting is death.",
    tags: ["automaton", "sentry", "turret", "defense", "weapon", "arcturus", "corporate", "security", "tier 3"]
  },

  // ===================== SWARM PLATFORMS (15) =====================
  {
    name: "TESSERA TS-1 'Locust'",
    type: "automaton",
    classification: "Swarm — Ground Assault",
    aliases: ["Locust", "The Tide", "Carpet"],
    manufacturer: "TESSERA",
    description: "The TS-1 is a ground-based swarm unit the size of a deck of playing cards. It has six legs, a single optical sensor, and a shaped micro-charge. It is the cheapest automaton ever manufactured — TESSERA produces them for less than the cost of a meal at a Shelf food stall. They are deployed by the hundreds from dispersal canisters that can be thrown, launched, or dropped from vehicles and drones.\n\nA single TS-1 is negligible. A swarm of 500, dispersed across a city block, creates a ground-level killing field. The units scuttle toward heat signatures, climb up legs and bodies, and detonate on contact with soft tissue. They are too small to target individually with conventional weapons, too numerous to evade, and too cheap to worry about losing. A canister of 100 units costs less than a sidearm.\n\nThe TS-1 has no tactical elegance. It represents the industrialization of killing — the reduction of lethal force to a manufacturing problem. TESSERA can produce 10,000 units per day from a single production line. The military implications are staggering: conventional infantry become economically nonviable when a crate of TS-1 canisters can clear the same area at a fraction of the cost and zero friendly casualties.",
    tier_availability: "Tier 3+",
    legality: "Licensed — but regulated in civilian areas",
    autonomy_level: "Swarm autonomous — heat-seeking",
    dimensions: "0.08m x 0.05m x 0.03m per unit",
    weight: "0.04 kg per unit",
    power_source: "Micro capacitor, 10-minute operational life",
    locomotion: "Hexapod micro-legs, climbing capable",
    armament: ["Shaped micro-charge (lethal against exposed skin, painful against light clothing)"],
    sensors: ["Thermal homing", "Swarm proximity coordination"],
    countermeasures: "Sealed clothing and footwear prevent skin contact detonation. Elevated positions unreachable by climbing (smooth vertical surfaces). Area-effect fire (flame) most efficient. Cold environments slow locomotion. 10-minute battery life means outlasting the swarm is possible in hardened positions.",
    known_deployments: ["TESSERA security operations", "Military demonstrations", "Black market distribution (widespread)"],
    story_hooks: [
      "A TS-1 canister has been placed inside the ventilation system of a Shelf residential building. When it opens, hundreds of units will pour through the ductwork into every apartment simultaneously. Someone wants everyone in that building dead — or terrified enough to leave.",
      "The Shelf's black market has been flooded with TS-1 canisters at below-cost prices. Someone is subsidizing the distribution. The question is whether they want the Shelf armed — or whether the canisters have been modified."
    ],
    cultural_context: "The TS-1 has made the ground itself feel hostile. The phrase 'watch the floor' has replaced 'watch your back' in districts where Locusts have been deployed. Children in the Shelf are taught to keep their feet off the ground whenever possible.",
    tags: ["automaton", "swarm", "ground", "micro", "explosive", "weapon", "tessera", "corporate", "tier 3"]
  },

  // ===================== AQUATIC PLATFORMS (10) =====================
  {
    name: "Ouroboros AQ-3 'Undertow'",
    type: "automaton",
    classification: "Aquatic Platform — Harbor Patrol/Denial",
    aliases: ["Undertow", "River Ghost", "Hull Hugger"],
    manufacturer: "OUROBOROS ENERGY",
    description: "The AQ-3 is a torpedo-shaped aquatic drone 2 meters in length, designed for underwater patrol operations in GLMZ's harbor and waterway infrastructure. Its primary function is infrastructure protection — monitoring bridge foundations, dam structures, and underwater utility conduits for unauthorized access or sabotage. Its secondary function is killing anyone it finds doing those things.\n\nThe Undertow carries a directed-charge warhead designed for underwater detonation against hull structures and a secondary acoustic stunning system that produces a focused pressure wave capable of rupturing eardrums and causing disorientation at ranges up to 30 meters. The unit operates in near-total silence, propelled by a magnetic drive that produces no cavitation noise, and can hover motionless at any depth within its range. Divers in the harbor describe the experience of encountering an Undertow as seeing a shadow that shouldn't be there — a shape in the murky water that moves with purpose.\n\nOuroboros deploys the AQ-3 in packs of six, creating overlapping patrol zones across harbor infrastructure. The units communicate through low-frequency sonar pulses inaudible to human ears, coordinating patrol routes and target engagement. The harbor is, effectively, mined — not with static explosive devices, but with mobile, intelligent weapons that decide what constitutes a threat.",
    tier_availability: "Tier 4+",
    legality: "Licensed for infrastructure protection",
    autonomy_level: "Fully autonomous pack patrol",
    dimensions: "2.0m length, 0.3m diameter",
    weight: "65 kg",
    power_source: "Seawater fuel cell, 120-hour endurance",
    locomotion: "Magnetic drive — silent propulsion",
    armament: ["Directed-charge warhead (anti-hull)", "Acoustic stunning system (30m range)"],
    sensors: ["Passive sonar", "Magnetic anomaly detection", "Water pressure displacement sensing"],
    countermeasures: "Acoustic decoys can draw attention. The magnetic drive is detectable by sensitive magnetometers. The units surface for communication relay every 4 hours — this is a predictable vulnerability window. Harbor water turbidity provides concealment for human divers as well as for the drones.",
    known_deployments: ["GLMZ harbor infrastructure", "Dam and waterway protection", "Old Harbor industrial waterfront"],
    story_hooks: [
      "A body washed up in Old Harbor with injuries consistent with an Undertow acoustic stun followed by drowning. The victim was a journalist investigating Ouroboros's underwater monitoring network. Ouroboros claims the death was accidental — the journalist entered a restricted waterway.",
      "Someone needs to access a submerged utility conduit in the harbor. The conduit is protected by an Undertow pack. The players need to either disable the pack, fool it, or survive it — while underwater, in the dark, in water the drones know better than anyone."
    ],
    cultural_context: "The Undertow has made the harbor — once a lifeline for Shelf communities that fished and scavenged from the waterfront — effectively off-limits. Fishermen who once operated freely now risk encounter with autonomous weapons. The water belongs to Ouroboros.",
    tags: ["automaton", "aquatic", "underwater", "patrol", "weapon", "ouroboros", "corporate", "harbor", "tier 4"]
  },

  // ===================== INFRASTRUCTURE / UTILITY WEAPONS (10) =====================
  {
    name: "Ouroboros PG-2 'Blackout'",
    type: "automaton",
    classification: "Infrastructure Weapon — Power Grid Attack",
    aliases: ["Blackout", "Grid Killer", "Dark Star"],
    manufacturer: "OUROBOROS ENERGY",
    description: "The PG-2 is a small wheeled drone designed to interface with GLMZ's power grid infrastructure. It navigates to a designated power distribution node — a transformer, junction box, or substation terminal — connects to the infrastructure using articulated probe arms, and overloads the system with a precisely calibrated power surge that cascades through the grid, causing blackouts across targeted areas. The PG-2 doesn't destroy the infrastructure — it weaponizes it against its own users.\n\nOuroboros designed the PG-2 as a 'grid stress testing platform,' which is technically its function. The fact that this function is indistinguishable from a targeted infrastructure attack is, according to Ouroboros's documentation, a regulatory interpretation issue. In practice, the PG-2 allows Ouroboros to selectively disable power to any area of GLMZ that Ouroboros supplies — which is most of it — without physically damaging equipment that Ouroboros owns.\n\nThe implications are profound. Ouroboros can black out a neighborhood to support a CorpoNation's military operation, shut down power to a protest area, disable security systems in a targeted building, or simply remind a district that their electricity is a privilege. The PG-2 turns the power grid into a weapon, and Ouroboros holds the trigger.",
    tier_availability: "Tier 5 — Ouroboros internal",
    legality: "Does not officially exist as a weapon",
    autonomy_level: "Remote operated with autonomous grid navigation",
    dimensions: "0.5m x 0.3m x 0.2m",
    weight: "8 kg",
    power_source: "Internal lithium cell, 6-hour endurance",
    locomotion: "Four-wheeled, indoor/outdoor, utility corridor rated",
    armament: ["Articulated probe arms for grid interface", "Calibrated power surge generation system"],
    sensors: ["Grid topology mapping", "Power flow analysis", "Infrastructure identification"],
    countermeasures: "Independent power generation (generators, solar) is immune. Grid isolation switches can contain cascade if activated quickly enough. The PG-2 itself is a small, unarmed drone vulnerable to any physical attack.",
    known_deployments: ["Grid stress testing (official)", "Selective blackouts coinciding with corporate operations (documented but unproven)"],
    story_hooks: [
      "The Shelf has been experiencing rolling blackouts that follow a pattern — always in areas where corporate land acquisition is being contested. A PG-2 was spotted near a junction box moments before the latest blackout. Ouroboros denies everything.",
      "Someone has stolen a PG-2 and is using it to black out areas of the Spires. The upper tiers are experiencing power loss for the first time, and the panic is disproportionate. Ouroboros wants the unit recovered not because of the blackouts but because its existence is being exposed."
    ],
    cultural_context: "The PG-2 represents the literal weaponization of infrastructure dependence. In a city where everything runs on electricity — BCIs, augmentations, life support, food storage — the ability to selectively cut power is the ability to selectively destroy lives.",
    tags: ["automaton", "infrastructure", "power", "grid", "weapon", "ouroboros", "corporate", "control", "tier 5"]
  },

  // ===================== CROWD CONTROL / SUPPRESSION (10) =====================
  {
    name: "TESSERA CC-6 'Shepherd'",
    type: "automaton",
    classification: "Crowd Control — Mobile Barrier",
    aliases: ["Shepherd", "Cattle Grid", "The Push"],
    manufacturer: "TESSERA",
    description: "The CC-6 is a broad, flat-topped quadruped platform standing 1.5 meters tall with a forward-facing shield wall 3 meters wide. It walks slowly and deliberately into crowds, physically pushing people backward with a combination of its bulk and a chest-mounted electrified contact strip that delivers painful (non-lethal, supposedly) shocks to anyone who doesn't move. Behind the shield wall, a tear gas dispersal system ensures that the space the Shepherd clears stays clear.\n\nThe CC-6 is designed to compress crowds — to push people from one area to another without the optics of security personnel beating civilians. TESSERA deploys them in lines of four to six units, walking abreast, creating a moving wall that herds populations into designated containment areas. The units communicate to maintain formation and adjust speed based on crowd density, slowing when compression risks crush injuries and speeding up when the crowd thins.\n\nThe Shepherd's electrified contact strip has been documented as lethal to individuals with cardiac augmentations, pacemakers, and certain neural implant configurations. TESSERA's response to each documented death has been identical: the individual failed to disclose a medical condition that made them vulnerable to standard crowd management technology. The onus, per TESSERA's legal framework, is on the person being electrocuted to have told someone beforehand.",
    tier_availability: "Tier 3+",
    legality: "Licensed — crowd management",
    autonomy_level: "Formation autonomous with remote direction",
    dimensions: "1.5m height, 3.0m shield width, 2.0m depth",
    weight: "1,200 kg",
    power_source: "Hydrogen fuel cell, 18-hour endurance",
    locomotion: "Quadruped heavy walker, slow pace (walking speed)",
    armament: ["Electrified contact strip (chest-mounted)", "Rear-mounted tear gas dispersal", "Loudspeaker array for compliance instructions"],
    sensors: ["Crowd density analysis", "Formation coordination", "Crush-risk monitoring"],
    countermeasures: "The Shepherd is slow — it can be outrun easily. Insulated clothing negates the contact strip. The unit is top-heavy and can be tipped by coordinated lateral force. Climbing over the shield wall is physically possible but the contact strip makes it painful.",
    known_deployments: ["Regular deployment at protests, labor actions, and public gatherings across GLMZ", "Shelf district compression operations", "Event crowd management"],
    story_hooks: [
      "A line of Shepherds is pushing a crowd toward a district boundary. The crowd has nowhere left to go — the boundary is a wall. The crush-risk monitors are screaming. The operator hasn't adjusted speed. Whether it's malice or incompetence, people will die in minutes unless the Shepherds are stopped.",
      "Someone has reprogrammed a Shepherd's formation coordination — instead of walking in line with other units, it's walking a spiral pattern through a market, herding people toward the center. Something is waiting at the center."
    ],
    cultural_context: "The Shepherd embodies the CorpoNation approach to civil rights: you have the right to be somewhere else. Its slow, deliberate advance through neighborhoods during operations is described by residents as the most dehumanizing experience available — being pushed like livestock by a machine that doesn't know you're a person.",
    tags: ["automaton", "crowd control", "barrier", "suppression", "weapon", "tessera", "corporate", "control", "tier 3"]
  },

  // ===================== ADDITIONAL SPIDER VARIANTS (10) =====================
  {
    name: "Arcturus KS-11 'Coffin Nail'",
    type: "automaton",
    classification: "Spider Platform — Precision Kill",
    aliases: ["Coffin Nail", "One Shot", "The Carpenter"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The KS-11 is a small, four-legged spider platform the size of a large tarantula, carrying a single hyper-velocity penetrator round in a dorsal-mounted miniaturized railgun. The unit positions itself at range — typically on a wall or ceiling with line of sight to a doorway, corridor junction, or anticipated target position — and fires a single shot with sufficient velocity to penetrate standard body armor at ranges up to 50 meters. After firing, the unit has no remaining offensive capability and either self-destructs or withdraws for reloading.\n\nThe KS-11 is a mechanical sniper the size of a hand. It can be placed in locations no human sniper could access — inside ventilation grates, on the underside of furniture, behind wall panels — and wait indefinitely for a target. The railgun produces a sharp crack and a brief electromagnetic pulse on firing, but the unit's size and position make locating the source extremely difficult. By the time the shot is traced, the KS-11 has either destroyed itself or scuttled to a new position.\n\nArcturus produces them in bulk and sells them as 'perimeter denial micro-platforms.' A facility protected by fifty KS-11 units has fifty invisible snipers covering every corridor and doorway. The cost per unit is low enough that they can be treated as disposable — fired once and abandoned.",
    tier_availability: "Tier 4+",
    legality: "Licensed for facility defense",
    autonomy_level: "Fully autonomous — fire on target identification",
    dimensions: "0.15m body, 0.25m leg span",
    weight: "0.8 kg",
    power_source: "Micro capacitor (single shot), micro lithium cell (locomotion)",
    locomotion: "Quadruped micro-spider",
    armament: ["Dorsal miniaturized railgun (single hyper-velocity penetrator)"],
    sensors: ["Micro optical with target identification", "Vibration detection"],
    countermeasures: "Thorough physical search of an area can locate units. Electromagnetic scanning detects the charged capacitor. The single-shot limitation means surviving the first shot neutralizes the threat. Metallic dust or spray disrupts the railgun's magnetic field.",
    known_deployments: ["Corporate facility defense", "VIP residence protection", "Assassination operations (unconfirmed)"],
    story_hooks: [
      "The players enter a room where someone has placed thirty KS-11 units. Thirty invisible snipers, one shot each, covering every angle. Moving through the room is a puzzle with lethal consequences.",
      "A KS-11 killed a politician at a public event. The unit was found on a light fixture 40 meters away. It was purchased legally. The investigation is about who placed it, not who sold it."
    ],
    cultural_context: "The KS-11 has made interior spaces feel as dangerous as open ground. The knowledge that any surface might conceal a mechanical sniper has contributed to the pervasive paranoia of life in GLMZ.",
    tags: ["automaton", "spider", "sniper", "precision", "weapon", "arcturus", "corporate", "stealth", "tier 4"]
  },

  // ===================== ADDITIONAL CANINE VARIANTS (10) =====================
  {
    name: "Arcturus CK-5 'Bloodhound'",
    type: "automaton",
    classification: "Canine Platform — Pursuit and Tracking",
    aliases: ["Bloodhound", "Nose", "The Hunt"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The CK-5 doesn't carry explosives. It doesn't need to. It is a relentless quadruped tracking platform that identifies a target's chemical signature — sweat composition, skin microbiome, augmentation lubricant traces — and pursues them across any terrain, through any crowd, for as long as its power supply lasts. The CK-5 can track a target through rain, through buildings, through the Underworld tunnel network, and through every evasion technique that doesn't involve fundamentally changing your body chemistry.\n\nThe unit's tracking capability is its only function — it carries no weapons. Its purpose is to maintain contact with a fleeing target and broadcast their position to armed response units following behind. The Bloodhound runs at 60 km/h, navigates obstacles with canine agility, and has a 48-hour battery life. It does not stop. It does not lose the scent. It does not get tired.\n\nThe psychological impact of being pursued by a CK-5 is documented in Arcturus's own field reports as a significant operational factor. Targets who know they are being tracked by a Bloodhound make progressively worse decisions as exhaustion and desperation accumulate. The machine doesn't need to catch them — it just needs to keep following until they make a mistake that the armed units behind it can exploit.",
    tier_availability: "Tier 3+",
    legality: "Licensed for security use",
    autonomy_level: "Fully autonomous pursuit",
    dimensions: "0.7m shoulder height, 1.2m length",
    weight: "40 kg",
    power_source: "Hydrogen fuel cell, 48-hour endurance",
    locomotion: "Quadruped canine-mimetic, 60 km/h sprint, all-terrain",
    armament: ["None — pursuit and tracking only"],
    sensors: ["Chemical signature analysis array", "Thermal tracking", "Acoustic identification", "GPS broadcast"],
    countermeasures: "Chemical masking agents can confuse the tracking array temporarily. Entering water disrupts chemical trails. The unit's broadcast can be jammed to prevent armed response coordination. Physical destruction is possible with firearms but the unit is fast and evasive.",
    known_deployments: ["Corporate security pursuit operations", "Law enforcement fugitive tracking", "Private security manhunts"],
    story_hooks: [
      "A CK-5 has been locked onto a player character. It's been following them for six hours through the Shelf. The armed units behind it are corporate security with kill authorization. The player needs to either defeat the Bloodhound, change their chemical signature, or outrun a machine that doesn't get tired.",
      "Someone has hijacked a CK-5's broadcast frequency — the tracking data is being sent to a third party instead of the authorized response team. The target doesn't know they're being hunted by two groups now."
    ],
    cultural_context: "The CK-5 represents the mechanization of persecution. You can't hide from a machine that knows what you smell like. Runners in GLMZ carry chemical masking agents the way previous generations carried lock picks — as essential equipment for staying free.",
    tags: ["automaton", "canine", "pursuit", "tracking", "weapon", "arcturus", "corporate", "security", "tier 3"]
  },
  {
    name: "Crucible Industries CK-3 'Junkyard'",
    type: "automaton",
    classification: "Canine Platform — Scrap Recovery/Combat",
    aliases: ["Junkyard", "Scrap Dog", "The Chewer"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CK-3 was designed for automated scrap recovery — a quadruped platform that roams salvage yards and industrial waste sites, identifying valuable materials, cutting them free with its jaw-mounted plasma cutter, and carrying them back to collection points. Crucible sold thousands to industrial recycling operations. Then the Shelf got hold of them.\n\nThe CK-3's jaw-mounted plasma cutter — designed to cut through industrial scrap metal — works equally well on armor, vehicles, and people. Its chassis is built to absorb impacts from falling debris and industrial accidents, giving it the durability of a light armored vehicle. And its scrap-identification AI, designed to locate and prioritize valuable materials, was trivially reprogrammed by Shelf tech-runners to identify and prioritize human targets instead.\n\nThe Junkyard has become the most common combat automaton in the Shelf — not because anyone designed it for war, but because it was cheap, available, and easy to weaponize. Shelf gangs, resistance cells, and desperate individuals have all acquired reprogrammed CK-3 units. The units still look like industrial equipment — scarred, dented, covered in cutting marks and welding slag — which gives them a ramshackle appearance that disguises genuine lethality. A Junkyard doesn't look like a weapon. It looks like a piece of garbage that happens to be able to cut you in half.",
    tier_availability: "Tier 2+ (industrial); Black market (combat)",
    legality: "Industrial: Licensed. Reprogrammed: Prohibited",
    autonomy_level: "Autonomous scrap collection / autonomous combat (reprogrammed)",
    dimensions: "0.6m shoulder height, 1.0m length",
    weight: "48 kg",
    power_source: "Industrial lithium cell, 24-hour endurance",
    locomotion: "Quadruped — rugged industrial chassis, not fast (25 km/h) but extremely durable",
    armament: ["Jaw-mounted plasma cutter (industrial grade — cuts structural steel)", "Reinforced chassis capable of ramming attacks"],
    sensors: ["Material identification array (repurposable for target identification)", "Obstacle avoidance", "Thermal sensing"],
    countermeasures: "Slow by combat automaton standards. Plasma cutter requires close range. Not designed for evasive movement — straightforward pursuit only. EMP effective against industrial-grade electronics.",
    known_deployments: ["Industrial salvage operations across GLMZ", "Shelf gang combat units (reprogrammed)", "Resistance cell perimeter defense"],
    story_hooks: [
      "A pack of reprogrammed Junkyards has been released in the Shelf by a gang as territorial enforcement. The units are set to 'scrap' anything that enters the zone — including residents trying to get home.",
      "Crucible wants its CK-3 units back — not the reprogrammed ones, the industrial ones. Someone has been stealing them from salvage yards and selling them to the Shelf. Crucible is sending retrieval teams into the Shelf, which is escalating into armed conflict with communities that depend on the Junkyards for defense."
    ],
    cultural_context: "The Junkyard is the Shelf's answer to corporate military hardware — ugly, improvised, and effective. It represents the democratization of automaton warfare, and the CorpoNations hate it because it proves that the technology gap can be bridged with creativity and desperation.",
    tags: ["automaton", "canine", "industrial", "plasma", "weapon", "crucible", "shelf", "gang", "tier 2"]
  },

  // ===================== MISC HORRORS (25) =====================
  {
    name: "TESSERA TX-8 'Scarecrow'",
    type: "automaton",
    classification: "Psychological Operations — Terror Platform",
    aliases: ["Scarecrow", "Bad Dream", "The Standing Man"],
    manufacturer: "TESSERA",
    description: "The TX-8 is a bipedal humanoid automaton standing 2.2 meters tall, built with deliberately exaggerated proportions — arms too long, legs too thin, a featureless head that turns to track movement with uncanny smoothness. It carries no weapons. Its only capability, beyond walking, is a suite of psychological disruption systems: subsonic emitters that induce anxiety and nausea, strobing visual patterns from its chest-mounted display that trigger disorientation, and a voice synthesizer that produces sounds specifically engineered to activate human fear responses — children crying, bones breaking, breath stopping.\n\nThe Scarecrow is a weapon of pure psychology. It walks into an area, stands there, and makes everyone within a hundred meters progressively more terrified until they leave. It does no physical harm. TESSERA's legal team has successfully argued in twelve separate hearings that the TX-8 is not a weapon because it causes no physical injury — a position that ignores the documented cases of cardiac events, panic-induced injuries during flight responses, and long-term PTSD among exposed populations.\n\nThe Scarecrow is deployed before other automata in suppression operations — it clears an area psychologically before physical force becomes necessary. TESSERA considers this humane. Everyone else considers the experience of being targeted by a TX-8 to be among the worst things that can happen to a person without leaving a mark.",
    tier_availability: "Tier 4+",
    legality: "Licensed — classified as non-lethal psychological deterrent",
    autonomy_level: "Semi-autonomous with remote direction",
    dimensions: "2.2m height, disproportionate limbs, featureless head",
    weight: "180 kg",
    power_source: "Hydrogen fuel cell, 24-hour endurance",
    locomotion: "Bipedal — deliberately uncanny gait",
    armament: ["Subsonic anxiety/nausea emitters (100m radius)", "Strobing visual disorientation array", "Psychological audio synthesis (fear-optimized sounds)"],
    sensors: ["Movement tracking", "Heart rate detection at range", "Crowd behavior analysis"],
    countermeasures: "Ear protection and eye covering negate audio and visual systems. Subsonic emitters can be countered with active noise cancellation. The unit is physically unarmored and vulnerable to conventional weapons. Its psychological effectiveness is reduced against targets who are aware of its capabilities.",
    known_deployments: ["Pre-assault psychological preparation operations", "Protest suppression", "Area denial without physical force"],
    story_hooks: [
      "A Scarecrow has been standing at a Shelf intersection for three days. It wasn't deployed by TESSERA. It wasn't deployed by anyone with authorization. It just showed up. And it's not running any of its disruption systems — it's just standing there, tracking faces. The neighborhood wants it gone but nobody wants to approach it.",
      "Someone has modified a Scarecrow's audio system to broadcast a specific message — not fear sounds, but a name. Over and over. The name of someone who died in a TESSERA suppression operation. The modification was done from inside TESSERA."
    ],
    cultural_context: "The Scarecrow is the automaton that most directly attacks what it means to be human — it targets the mind, the emotions, the involuntary fear responses that evolution gave us for survival. That TESSERA classified this as 'non-lethal' is cited as evidence that CorpoNation legal teams have no functioning concept of harm.",
    tags: ["automaton", "psychological", "terror", "bipedal", "weapon", "tessera", "corporate", "horror", "tier 4"]
  },
  {
    name: "Ringo RM-4 'Thresher'",
    type: "automaton",
    classification: "Agricultural/Combat — Autonomous Clearing Platform",
    aliases: ["Thresher", "Lawn Mower", "Red Harvest"],
    manufacturer: "RINGO CorpoNation",
    description: "The RM-4 is an agricultural automaton designed for crop clearing and land preparation — a tracked platform the size of a small car with forward-mounted rotary cutting blades designed to clear vegetation, small trees, and crop residue at industrial speed. Ringo deployed them across its agricultural holdings for efficient land management. The combat application was discovered accidentally when a unit malfunctioned during a demonstration near a group of observers.\n\nThe incident was classified. The capability was not forgotten. Ringo's security division realized that a machine designed to cut through dense vegetation at high speed could, with minimal reprogramming, cut through dense crowds at high speed. The combat variant of the RM-4 replaces the vegetation-optimized cutting blades with hardened steel rotary cutters and adds armored side panels to protect the drive system from small arms fire. It drives into groups of people at 40 km/h and does what it was designed to do to crops.\n\nRingo has never officially acknowledged the combat variant. Agricultural RM-4 units are in wide, legitimate use. The combat variant is visually indistinguishable until the blade configuration is examined closely — hardened steel cutters where agricultural cutting teeth should be. This dual-use ambiguity is, by this point, a recognized Ringo design philosophy.",
    tier_availability: "Tier 2+ (agricultural); Tier 4+ (combat)",
    legality: "Agricultural: Licensed. Combat variant: Does not officially exist",
    autonomy_level: "Autonomous route following (agricultural); Remote operated (combat)",
    dimensions: "2.5m length, 2.0m width, 1.2m height",
    weight: "2,200 kg",
    power_source: "Diesel engine, 8-hour endurance",
    locomotion: "Tracked — 40 km/h maximum speed",
    armament: ["Forward-mounted rotary cutting blades (3m cutting width)", "Armored chassis (combat variant)"],
    sensors: ["Obstacle detection", "GPS route following", "Remote camera feed (combat variant)"],
    countermeasures: "Tracked vehicles can be immobilized by anti-vehicle mines, caltrops, and terrain obstacles. The cutting blades are forward-mounted only — flanking and rear attacks avoid the primary danger. Elevated positions above 1.2m are outside the blade arc.",
    known_deployments: ["Agricultural land management across Ringo holdings", "Combat variant: classified, multiple unconfirmed incidents"],
    story_hooks: [
      "A Shelf community built on former Ringo agricultural land is receiving eviction notices. At the perimeter of the district, three RM-4 units have been parked. Agricultural variants, according to Ringo. Residents who've seen them up close disagree.",
      "An RM-4 combat variant was used in an incident that left seventeen dead. The unit's logs were wiped. The victims were all members of a labor organization that had been organizing Ringo agricultural workers."
    ],
    cultural_context: "The Thresher represents Ringo's particular brand of horror — the conversion of agricultural tools into weapons of mass killing. The fact that the same machine that harvests food also harvests people is a metaphor that writes itself.",
    tags: ["automaton", "tracked", "agricultural", "cutting", "weapon", "war", "ringo", "corporate", "dual-use", "tier 4"]
  },
  {
    name: "Arcturus DM-1 'Pallbearer'",
    type: "automaton",
    classification: "Disposal — Autonomous Corpse Retrieval",
    aliases: ["Pallbearer", "Body Bag", "The Collector"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The DM-1 is designed for a function nobody wants to think about: autonomous corpse retrieval from active combat zones. The six-wheeled platform navigates battlefields, identifies deceased individuals through vital sign absence confirmation, loads them into its refrigerated cargo bay using articulated manipulator arms, and transports them to designated collection points. The DM-1 operates during and after combat operations, moving through areas that may still be under fire to recover bodies before decomposition or battlefield scavenging compromises identification.\n\nThe DM-1 is not a weapon. It is the thing that comes after the weapons are done. Its presence on a battlefield is a statement that the killing is expected to be sufficient to require industrial-scale body recovery. Arcturus deploys them in advance of major operations, pre-positioning them at staging areas as a logistical preparation step. When soldiers see Pallbearers being unloaded, they know the command structure has calculated the expected body count and prepared accordingly.\n\nThe DM-1's most disturbing operational edge case involves its vital sign confirmation system. The unit is designed to confirm death before loading — but the sensors have a margin of error. There are documented incidents of the DM-1 loading individuals who were unconscious, in shock, or in low-vital-sign states caused by augmentation malfunction. Arcturus's official documentation addresses this with the phrase 'retrieval protocol includes triage assessment with a statistical confidence interval of 99.2%,' which means that roughly one in 125 pickups might not actually be dead.",
    tier_availability: "Tier 4+",
    legality: "Military logistics — licensed",
    autonomy_level: "Fully autonomous",
    dimensions: "3.0m length, 1.5m width, 1.2m height",
    weight: "800 kg empty",
    power_source: "Diesel-electric, 36-hour endurance",
    locomotion: "Six-wheeled all-terrain",
    armament: ["None — logistical platform"],
    sensors: ["Vital sign detection (cardiac, respiratory, neural)", "Identification systems (facial, BCI, dental)", "Battlefield hazard detection"],
    countermeasures: "N/A — non-combat platform. Can be destroyed with conventional weapons but serves no tactical purpose to do so.",
    known_deployments: ["Post-combat recovery operations", "Disaster response", "Mass casualty events"],
    story_hooks: [
      "A Pallbearer has returned to a collection point with a cargo bay full of bodies. One of them is still alive — they were in augmentation-induced low-vital-sign stasis. They were picked up from a battlefield and have no idea where they are, who won, or how long they've been in a refrigerated box with corpses.",
      "Pallbearers are being deployed to the Shelf in advance of an operation that hasn't been announced. The machines are arriving. Everyone knows what follows."
    ],
    cultural_context: "The Pallbearer forces confrontation with the industrialization of death. It is the machine that says: we have calculated how many of you will die, and we have prepared accordingly. Its pre-deployment is the most honest communication a CorpoNation makes.",
    tags: ["automaton", "logistics", "corpse retrieval", "battlefield", "arcturus", "corporate", "war", "tier 4"]
  },
  {
    name: "Lazarus LX-2 'Lab Rat'",
    type: "automaton",
    classification: "Experimental — Autonomous Test Subject",
    aliases: ["Lab Rat", "Runner", "The Guinea Pig"],
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "The LX-2 is a small quadruped automaton — roughly rat-sized — designed as an autonomous pharmaceutical test platform. Hundreds of them roam Lazarus testing facilities, each carrying a micro-pump system that continuously administers experimental drug compounds into a synthetic biological substrate mounted in their chassis. The substrate mimics human tissue response, allowing Lazarus to test drug interactions, dosage thresholds, and toxic effects without human subjects. Officially.\n\nThe LX-2's biological substrate is more sophisticated than Lazarus publicly acknowledges. Internal documents revealed by whistleblowers indicate that the substrate includes lab-grown neural tissue — not enough for sentience or sensation, but enough to produce measurable pain-analog responses that Lazarus uses to calibrate analgesic effectiveness. The Lab Rats feel something when they're being tested. Whether that something constitutes pain is a question Lazarus has spent considerable legal resources ensuring never reaches a courtroom.\n\nThe units that escape the laboratory — and they do escape, regularly, through ventilation systems and drain pipes — become a particular kind of urban wildlife. Shelf residents encounter rat-sized automata wandering through their walls, occasionally leaking pharmaceutical compounds from damaged micro-pump systems. The compounds range from harmless to hallucinogenic to toxic, depending on what the unit was testing when it escaped. Lazarus considers escaped units a negligible biosecurity risk. The Shelf residents who wake up hallucinating because a robot rat leaked experimental psychoactives into their water supply disagree.",
    tier_availability: "N/A — laboratory equipment",
    legality: "Licensed as research equipment",
    autonomy_level: "Autonomous navigation within facility (and beyond, when escaped)",
    dimensions: "0.15m length, rat-sized",
    weight: "0.2 kg",
    power_source: "Micro lithium cell, 72-hour endurance",
    locomotion: "Quadruped micro-legs, wall/pipe/vent capable",
    armament: ["None — carries experimental pharmaceutical compounds (may be hazardous)"],
    sensors: ["Obstacle avoidance", "Chemical self-monitoring", "Return-to-base navigation (frequently malfunctions)"],
    countermeasures: "Small enough to be physically destroyed by any means. The pharmaceutical payload is the primary hazard — avoid contact with fluids leaking from damaged units. Cat-sized predator automata are sold in the Shelf specifically for catching escaped LX-2 units.",
    known_deployments: ["Lazarus research facilities", "Escaped units throughout the Shelf and Underworld (ongoing)"],
    story_hooks: [
      "A nest of escaped LX-2 units has been found in a Shelf building's walls. Dozens of them, all leaking different compounds into the building's water system. The residents have been unknowingly exposed to an experimental drug cocktail for weeks. The effects are starting to manifest.",
      "A specific escaped LX-2 unit is carrying a compound that Lazarus considers extremely valuable — a breakthrough analgesic that could be worth billions. Lazarus wants the rat back. They're sending retrieval teams into the Shelf for a robot the size of a person's hand."
    ],
    cultural_context: "The LX-2 has given the Shelf a literal plague of pharmaceutical-leaking robot rats. The phrase 'Lazarus rat' is used to describe anything small, unwanted, and potentially hazardous. The units have become part of the Shelf's ecosystem — predator automata sold to catch them represent an entire micro-economy.",
    tags: ["automaton", "experimental", "pharmaceutical", "rat", "laboratory", "lazarus", "corporate", "shelf", "tier 1"]
  },
  {
    name: "Arcturus HX-1 'Headsman'",
    type: "automaton",
    classification: "Execution Platform — Autonomous Lethal Enforcement",
    aliases: ["Headsman", "Final Notice", "The Signature"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The HX-1 is a bipedal automaton standing exactly human height — 1.8 meters — dressed in a matte-black chassis designed to evoke formal authority. It carries a single weapon: a wrist-mounted high-powered laser cutter capable of severing limbs, penetrating body armor, and — in its designed application — executing individuals designated for termination by corporate authority. The HX-1 walks to its target, identifies them through biometric confirmation, announces the authorization for lethal force, and carries out the sentence.\n\nThe HX-1 exists because certain corporate contracts include lethal enforcement clauses — debt obligations, NDA violations, and security breaches that carry capital penalties under corporate law. Previously, these executions required human operators, which introduced complications: hesitation, conscience, witness testimony, PTSD-related liability. The HX-1 eliminates all of these. It does not hesitate. It has no conscience. It generates its own execution record. It cannot develop post-traumatic stress.\n\nArcturus produces the HX-1 in limited quantities for clients who operate under legal frameworks that permit autonomous lethal enforcement — which, in GLMZ, means most Tier 4 and Tier 5 CorpoNations. The unit's formal appearance and announcement protocol are deliberate: they create a veneer of due process around what is, functionally, a walking death sentence that cannot be appealed, negotiated with, or bribed.",
    tier_availability: "Tier 5",
    legality: "Licensed under corporate lethal enforcement statutes",
    autonomy_level: "Fully autonomous with biometric confirmation requirement",
    dimensions: "1.8m height, humanoid proportions",
    weight: "220 kg",
    power_source: "High-density hydrogen cell, 48-hour endurance",
    locomotion: "Bipedal — deliberate, measured pace",
    armament: ["Wrist-mounted high-powered laser cutter", "Biometric confirmation system"],
    sensors: ["Multi-factor biometric identification (facial, gait, BCI, genetic)", "Vital sign monitoring (confirms completion)"],
    countermeasures: "Biometric spoofing can delay but not permanently prevent identification. The unit is armored against small arms. Destroying the biometric system prevents target confirmation, halting the execution protocol. The HX-1 will not engage unconfirmed targets. Fleeing works temporarily — the unit will pursue until its endurance expires or the target is confirmed dead.",
    known_deployments: ["Corporate lethal enforcement operations (Tier 4-5 CorpoNations)", "Classified — specific deployments not publicly documented"],
    story_hooks: [
      "An HX-1 is walking through the Shelf, announcing a name. The name belongs to someone who defaulted on a corporate medical debt. The execution authorization is technically legal. The neighborhood is deciding whether to let the machine pass.",
      "An HX-1 has announced the wrong name — a biometric false match has sent it after an innocent person. The real target is watching from a distance. The innocent person has hours to prove their identity before the machine catches up."
    ],
    cultural_context: "The HX-1 is the most overtly dystopian automaton in GLMZ — a machine that walks up to you and kills you because a corporation decided you should die. Its existence is cited as the defining artifact of the corporate sovereignty era: the moment machines replaced executioners.",
    tags: ["automaton", "execution", "bipedal", "laser", "weapon", "arcturus", "corporate", "justice", "horror", "tier 5"]
  },
  {
    name: "Ringo RE-2 'Scarab'",
    type: "automaton",
    classification: "Micro Platform — Internal Sabotage",
    aliases: ["Scarab", "Bug", "The Itch"],
    manufacturer: "RINGO CorpoNation",
    description: "The RE-2 is the size of a large beetle — small enough to be swallowed, inhaled, or introduced through an ear canal while a target sleeps. Its chassis is coated in bio-compatible material that prevents immune response, and its six micro-legs allow it to navigate the interior of a human body, crawling through the esophagus, nasal passages, or auditory canal to reach internal organs or — most commonly — to position itself near the BCI implant at the base of the skull.\n\nOnce positioned, the RE-2 can perform several functions: it can tap into BCI signals and transmit them externally (surveillance), it can deliver micro-doses of pharmaceutical agents directly to neural tissue (chemical manipulation), it can physically sever connections between the BCI and the nervous system (sabotage), or it can simply detonate a micro-charge smaller than a grain of rice (assassination). The target never knows the unit is there. Symptoms of RE-2 presence — headaches, tinnitus, mood changes — are identical to common BCI malfunction symptoms and are routinely dismissed.\n\nRingo officially manufactures the RE-2 as a 'BCI diagnostic micro-platform' — a device designed to be introduced into a patient's body to perform internal diagnostics on neural implants. This is a real medical application that exists and is used legitimately. The gap between 'diagnostic' and 'weaponized' is a firmware update.",
    tier_availability: "Tier 5",
    legality: "Medical diagnostic: Licensed. Weaponized: Does not officially exist",
    autonomy_level: "Pre-programmed mission with autonomous internal navigation",
    dimensions: "12mm x 8mm x 5mm",
    weight: "0.003 kg",
    power_source: "Bio-electric harvesting from host body, indefinite endurance",
    locomotion: "Hexapod micro-legs, internal body navigation",
    armament: ["Micro-charge (grain-of-rice sized)", "Pharmaceutical micro-dosing system", "BCI connection severing capability"],
    sensors: ["Chemical environment monitoring", "BCI signal detection", "Position orientation"],
    countermeasures: "Deep medical scan can detect the unit's metallic signature. MRI will destroy the unit but may trigger the micro-charge. Specialized extraction requires microsurgery. The unit's bio-electric power source means it never runs out of energy while inside a living host.",
    known_deployments: ["Medical BCI diagnostics (legitimate)", "Weaponized deployment: classified, strongly suspected in multiple high-profile deaths and behavioral changes among political figures"],
    story_hooks: [
      "A political figure has been acting strangely — making decisions that benefit Ringo at every turn, contradicting their previous positions. Their medical scans show a tiny anomaly near the BCI implant. The diagnosis is 'artifact.' It isn't.",
      "A player character wakes up with a headache and tinnitus after sleeping in an unsecured location. A medical scan reveals an RE-2 positioned near their BCI. It's been transmitting for days. Everything they've thought, said, and planned has been broadcast."
    ],
    cultural_context: "The RE-2 represents the ultimate invasion of bodily autonomy — a weapon that lives inside you and operates without your knowledge. The possibility of its existence has created a thriving but largely futile market for internal scanning services in the Shelf.",
    tags: ["automaton", "micro", "internal", "bci", "sabotage", "assassination", "ringo", "corporate", "horror", "tier 5"]
  },
  {
    name: "TESSERA TC-3 'Crucible'",
    type: "automaton",
    classification: "Heavy Platform — Mobile Incinerator",
    aliases: ["Crucible", "The Oven", "Clean Sweep"],
    manufacturer: "TESSERA",
    description: "The TC-3 is a tracked platform carrying a forward-mounted industrial plasma torch with a 30-meter range and a 15-degree cone of effect. It was originally designed for demolition — clearing condemned structures by literally melting them — and for hazardous waste disposal through high-temperature incineration. TESSERA's security division recognized that a machine designed to reduce buildings to slag could serve other purposes.\n\nThe combat variant of the TC-3 is identical to the demolition variant. There is no modification required. A machine that incinerates buildings incinerates everything in them. TESSERA simply changes the targeting coordinates from 'condemned structure' to 'occupied structure' and the TC-3 does not know the difference. It cannot know the difference. It is a machine that creates fire and points it where it's told.\n\nThe TC-3 is deployed for 'urban renewal operations' — TESSERA's term for clearing areas designated for demolition that may or may not be fully evacuated. The unit's plasma torch creates temperatures sufficient to melt structural steel, meaning nothing within its engagement zone survives in a recognizable form. Evidence, bodies, records, personal belongings — everything becomes slag. TESSERA's legal team has noted in internal documents that this evidentiary destruction is 'an incidental benefit of the demolition methodology.'",
    tier_availability: "Tier 4+",
    legality: "Licensed for demolition. Combat use: regulatory grey area",
    autonomy_level: "Remote operated with autonomous demolition sequencing",
    dimensions: "3.5m length, 2.5m width, 2.0m height",
    weight: "5,500 kg",
    power_source: "High-capacity fuel cell array, 6-hour continuous operation",
    locomotion: "Tracked — 15 km/h maximum",
    armament: ["Forward-mounted industrial plasma torch (30m range, 15-degree cone, 3,000°C+)"],
    sensors: ["Thermal imaging", "Structural analysis for demolition sequencing", "Remote camera feed"],
    countermeasures: "Plasma torch has limited traverse angle — flanking avoids the engagement cone. The unit is slow and tracked, making it vulnerable to anti-vehicle weapons. Fuel cell array is volatile if penetrated. Water does not extinguish plasma-heated materials.",
    known_deployments: ["Authorized demolition operations", "Urban renewal projects (disputed)", "Evidence elimination (alleged)"],
    story_hooks: [
      "A TC-3 has been dispatched to demolish a Shelf building that is still occupied. TESSERA's paperwork says the building was evacuated. The residents say it wasn't. The TC-3 arrives in six hours.",
      "A TC-3 was used to demolish a building that contained evidence of corporate crimes. A journalist had been inside, copying files. The journalist escaped, but their copies were destroyed. The only remaining evidence is a partial transmission they sent before the building melted."
    ],
    cultural_context: "The TC-3 is the industrial eraser — a machine that makes things not have existed. In a city where evidence is power, the ability to reduce a building and everything in it to slag is the ultimate form of corporate censorship.",
    tags: ["automaton", "tracked", "incinerator", "plasma", "demolition", "weapon", "tessera", "corporate", "tier 4"]
  },
  {
    name: "Arcturus EW-3 'Migraine'",
    type: "automaton",
    classification: "Electronic Warfare — BCI Disruption Platform",
    aliases: ["Migraine", "Brain Fryer", "The Scrambler"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The EW-3 is a small wheeled drone — about the size of a suitcase — that carries a directional electromagnetic warfare suite capable of disrupting, scrambling, or seizing control of Brain-Computer Interface systems within a 200-meter radius. In a city where 78% of the population has some form of BCI implant, the EW-3 is effectively a weapon that attacks almost everyone simultaneously.\n\nThe disruption mode causes BCI static — visual artifacts, auditory hallucinations, motor control glitches, and sensory processing errors in everyone within range. The effects are temporary but profoundly disabling. The scramble mode targets specific BCI signatures and can render individual implants non-functional for hours, effectively blinding and deafening victims who depend on their augmentations. The seizure mode — classified, denied by Arcturus — can allegedly take control of a target's BCI and feed false sensory data directly into their nervous system, making them see, hear, and feel things that don't exist.\n\nThe EW-3 is small enough to be carried in a bag, placed under a vehicle, or hidden in a room. Its effects look like BCI malfunction — a common enough occurrence that victims may not realize they're under attack until the patterns become unmistakable. By that point, the EW-3 has done its work.",
    tier_availability: "Tier 4+",
    legality: "Military restricted — civilian possession prohibited",
    autonomy_level: "Remote operated or pre-programmed activation",
    dimensions: "0.4m x 0.3m x 0.2m",
    weight: "12 kg",
    power_source: "High-density lithium cell, 4-hour active emission",
    locomotion: "Four-wheeled, indoor rated",
    armament: ["Directional BCI disruption array (200m radius)", "Scramble mode (targeted BCI attack)", "Seizure mode (classified, BCI control)"],
    sensors: ["BCI signature detection", "Electromagnetic environment mapping"],
    countermeasures: "Faraday-shielded BCIs are resistant but not immune. Non-augmented individuals are unaffected. Signal triangulation can locate the emission source. The unit's electromagnetic output makes it detectable by spectrum analysis equipment.",
    known_deployments: ["Military electronic warfare operations", "Corporate counter-intrusion", "Black market use in criminal operations and political suppression"],
    story_hooks: [
      "An EW-3 has been placed in a Shelf market. Everyone in the area is experiencing BCI disruption — some are seeing things that aren't there, others have lost motor control. The market has become a zone of chaos, and nobody can identify the source because their BCIs keep telling them to look in the wrong direction.",
      "A player character's BCI has been seized by an EW-3 in seizure mode. They're seeing false sensory data — a version of reality that doesn't exist. They need to figure out what's real before they act on false information that leads them into a trap."
    ],
    cultural_context: "The EW-3 attacks the technology that GLMZ's population depends on for daily function. It turns augmentation into vulnerability and proves that the more connected you are, the more ways you can be hurt.",
    tags: ["automaton", "electronic warfare", "bci", "disruption", "weapon", "arcturus", "corporate", "tier 4"]
  },
  {
    name: "Ringo RB-1 'Judas Goat'",
    type: "automaton",
    classification: "Deception Platform — Synthetic Human Mimicry",
    aliases: ["Judas Goat", "The Lure", "Wrong Person"],
    manufacturer: "RINGO CorpoNation",
    description: "The RB-1 is a bipedal automaton with a synthetic skin covering, artificial hair, and facial feature articulation that allows it to approximate human appearance well enough to pass casual observation at distances beyond 5 meters. It cannot pass close inspection — the skin texture is wrong, the eyes don't track naturally, and the gait has a mechanical quality that observant individuals notice. But in crowds, in poor lighting, at a distance, and to people who aren't looking for it, the RB-1 looks like a person.\n\nRingo uses the RB-1 for two purposes. The first is decoy operations: the unit is given the superficial appearance of a high-value target and sent into exposed positions to draw fire, revealing ambush positions and sniper locations. The second purpose is luring: the RB-1 mimics the appearance and movement patterns of a specific individual to draw targets into kill zones, meetings with hostile parties, or areas where other automata are deployed.\n\nThe RB-1's existence has created a specific kind of paranoia in GLMZ — the suspicion that the person you're looking at might not be a person. This suspicion is almost always unfounded. The RB-1 is rare and expensive. But the knowledge that it exists is enough to erode trust in the most basic human interaction: recognizing another human being.",
    tier_availability: "Tier 5",
    legality: "Classified — denied by Ringo",
    autonomy_level: "Remote operated with autonomous locomotion",
    dimensions: "Variable — matches designated target's approximate build",
    weight: "85-110 kg depending on configuration",
    power_source: "Internal hydrogen cell, 12-hour endurance",
    locomotion: "Bipedal with human-mimetic gait (imperfect at close range)",
    armament: ["None standard — some units modified with concealed explosive charges"],
    sensors: ["Environmental awareness", "Crowd navigation", "Remote camera for operator situational awareness"],
    countermeasures: "Close visual inspection reveals synthetic skin texture. Thermal imaging shows uniform heat distribution unlike biological humans. BCI interrogation returns no signal. Animals, particularly dogs, react to the unit with confusion or aggression.",
    known_deployments: ["Decoy and luring operations (classified)", "Suspected use in multiple assassination setups"],
    story_hooks: [
      "A player character sees someone they know walking through a crowd. They follow them. The person enters an alley. Inside the alley, the 'person' is standing perfectly still, facing a wall. It turns around. The face is almost right but not quite. Behind the player, a door closes.",
      "An RB-1 wearing the face of a dead person has been spotted in the Shelf. The dead person's family has seen it. Ringo won't explain why a machine wearing their loved one's face is walking their neighborhood."
    ],
    cultural_context: "The RB-1 weaponizes the human face. In a city where identity is already fluid — augmented, modified, synthetic — the existence of machines that look like people pushes the question of 'who is real?' from philosophical speculation to survival concern.",
    tags: ["automaton", "humanoid", "deception", "decoy", "weapon", "ringo", "corporate", "horror", "tier 5"]
  },
  {
    name: "Ouroboros PD-1 'Leech'",
    type: "automaton",
    classification: "Parasitic Platform — Energy Theft",
    aliases: ["Leech", "Sucker", "Grid Tick"],
    manufacturer: "OUROBOROS ENERGY",
    description: "The PD-1 is a small disc-shaped automaton that attaches to powered infrastructure — vehicles, generators, building power systems, even personal augmentation power cells — and siphons energy. The unit clamps onto a power source using magnetic anchors, extends a probe that interfaces with the electrical system, and diverts a portion of the power output to its internal storage or to a wireless transmission system that transfers the stolen energy to a designated receiver.\n\nOuroboros designed the PD-1 for 'power grid auditing' — identifying unauthorized power taps and electrical theft across its network. The irony that the auditing tool is itself an energy thief has not been lost on anyone. In practice, the PD-1 is deployed against competitors' infrastructure, against independent power generation in the Shelf, and against any electrical system Ouroboros wants to degrade without destroying.\n\nThe most insidious application targets personal augmentations. A PD-1 attached to a person's cyberware power cell — clamped to the back of a neck, hidden under clothing — slowly drains the power supply that keeps their augmentations running. The victim experiences progressive augmentation failure: dimming enhanced vision, weakening prosthetic limbs, degrading BCI function. They go to a clinic, get their power cell replaced, and the PD-1 drains the new one too. Some victims have gone through five or six replacements before someone thinks to look for an external parasite.",
    tier_availability: "Tier 3+",
    legality: "Licensed for grid auditing; parasitic use prohibited",
    autonomy_level: "Pre-programmed attachment and siphoning",
    dimensions: "0.08m diameter, 0.03m thick",
    weight: "0.15 kg",
    power_source: "Parasitic — powered by the energy it steals",
    locomotion: "None after attachment — delivered manually or by carrier drone",
    armament: ["None — degrades target through energy theft"],
    sensors: ["Power source detection", "Optimal attachment point identification"],
    countermeasures: "Physical inspection reveals the device. Magnetic anomaly detectors can identify it. Power monitoring that detects unexplained drain leads to discovery. The magnetic clamps can be defeated with a strong pry tool.",
    known_deployments: ["Grid auditing operations", "Anti-competitor infrastructure degradation", "Personal augmentation sabotage (denied by Ouroboros)"],
    story_hooks: [
      "A Shelf clinic has seen a surge in augmentation power failures — dozens of patients reporting rapid power cell drain. A tech-runner discovered a PD-1 on one of them. Someone is systematically draining the augmentations of an entire neighborhood.",
      "A PD-1 has been attached to a critical life-support augmentation — a patient's artificial heart power supply. The device is draining the heart's battery. Removal requires steady hands because the magnetic clamps, if removed too quickly, create an electrical spike that could stop the heart."
    ],
    cultural_context: "The PD-1 attacks augmented individuals through their dependence on power — turning the energy that keeps their chrome running into a vulnerability. It is the weapon of someone who wants to make augmented people feel helpless.",
    tags: ["automaton", "parasitic", "energy", "sabotage", "cyberware", "ouroboros", "corporate", "tier 3"]
  },
  {
    name: "Arcturus NX-1 'Sandman'",
    type: "automaton",
    classification: "Incapacitation Platform — Sleep Agent Delivery",
    aliases: ["Sandman", "Lullaby", "Night Night"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The NX-1 is a small aerial drone — quadrotor, roughly the size of a dinner plate — that carries a pressurized canister of aerosolized soporific agent. It navigates to a target location — typically a bedroom, through an open window or ventilation system — and releases the agent in a controlled dispersal pattern designed to induce deep, unrousable sleep in all occupants within a 10-meter radius. The onset time is approximately 90 seconds after exposure, and the sleep duration is 4 to 8 hours depending on body mass and augmentation.\n\nThe Sandman enables everything that comes after it. Kidnapping, assassination, implantation of surveillance devices, planting of evidence, medical procedures performed without consent — the NX-1 is the prelude to crimes that require the victim to be unconscious. Arcturus sells it as an 'extraction support platform' designed for hostage rescue and medical emergency response, which is a legitimate use case that accounts for perhaps 5% of actual deployments.\n\nThe soporific agent is the NX-1's primary ethical concern. Developed by Lazarus Pharmaceuticals under contract to Arcturus, the compound has a narrow therapeutic window — the difference between the dose that induces sleep and the dose that causes respiratory depression is small. In enclosed spaces, in combination with alcohol or other depressants, or in individuals with certain augmentations, the Sandman's payload can be lethal. The official mortality rate is 0.8%. In the Shelf, where many residents have compromised respiratory systems and augmentations that interact unpredictably with pharmaceutical agents, the actual rate is believed to be significantly higher.",
    tier_availability: "Tier 4+",
    legality: "Military/security restricted",
    autonomy_level: "Semi-autonomous with pre-programmed target location",
    dimensions: "0.25m rotor span",
    weight: "1.5 kg",
    power_source: "Micro lithium cell, 45-minute flight time",
    locomotion: "Quadrotor — silent operation mode",
    armament: ["Pressurized soporific agent canister (10m effective radius)"],
    sensors: ["Building navigation", "Occupant detection (thermal)", "Wind/ventilation current mapping"],
    countermeasures: "Sealed sleeping environments. Gas masks. Air filtration systems. The drone's near-silent operation makes detection difficult but not impossible — augmented hearing can detect rotor noise. Open windows in GLMZ's climate are common in the Shelf, making countermeasures theoretical for most potential targets.",
    known_deployments: ["Extraction operations", "Medical emergency response (rare)", "Suspected use in kidnappings, assassinations, and non-consensual medical procedures"],
    story_hooks: [
      "A player character wakes up groggy, four hours later than they should have, with a needle mark on their arm that wasn't there before. Their BCI shows a gap in recording. A Sandman was used on them last night. What was done during those four hours?",
      "A Shelf community has been experiencing a wave of 'unexplained deep sleep' events — entire apartments falling unconscious simultaneously. Items are missing when they wake up. Someone is using Sandmen to rob the neighborhood, but the residents can't afford air filtration and can't close their windows in the heat."
    ],
    cultural_context: "The Sandman weaponizes sleep — the most vulnerable state a person can be in. Its existence means that falling asleep in an unsecured location is a calculated risk, adding 'ability to afford sealed sleeping quarters' to the list of survival advantages that wealth provides.",
    tags: ["automaton", "aerial", "incapacitation", "chemical", "weapon", "arcturus", "lazarus", "corporate", "tier 4"]
  },
  {
    name: "TESSERA TX-12 'Cerebral Dirge'",
    type: "automaton",
    classification: "Swarm — Acoustic Weapon Platform",
    aliases: ["Cerebral Dirge", "Screamer Swarm", "The Choir"],
    manufacturer: "TESSERA",
    description: "The TX-12 is a micro aerial drone — each unit barely larger than a hummingbird — that produces a single, precisely tuned acoustic frequency. One TX-12 is inaudible. Fifty produce a mild headache. Two hundred create an acoustic environment that induces severe nausea, disorientation, and panic. Five hundred can cause permanent hearing damage and, in documented cases, fatal cerebral hemorrhage.\n\nThe Cerebral Dirge operates on the principle of constructive interference — each unit's output is individually harmless, but when hundreds synchronize their frequencies, the combined acoustic energy reaches weapon-grade intensity. The swarm distributes itself around a target area, ensuring uniform coverage and preventing acoustic shadows. The effect is inescapable within the engagement zone — there is no direction to run that reduces exposure.\n\nTESSERA markets the Cerebral Dirge as a 'scalable non-lethal deterrent' — which it is, at low swarm counts. The problem is that scalability goes in both directions, and the difference between 'deterrent' and 'lethal' is a matter of how many units are deployed. A security contractor who starts with 200 and finds them insufficient can simply release another 200 from reserve, crossing the lethality threshold without any change in authorization or equipment. The escalation is frictionless.",
    tier_availability: "Tier 3+",
    legality: "Licensed as non-lethal — lethality threshold undisclosed",
    autonomy_level: "Swarm autonomous with operator-set intensity level",
    dimensions: "0.05m per unit",
    weight: "0.015 kg per unit",
    power_source: "Micro capacitor, 30-minute endurance per unit",
    locomotion: "Micro quadrotor",
    armament: ["Precisely tuned acoustic emitter per unit — lethal in sufficient numbers"],
    sensors: ["Swarm positioning coordination", "Acoustic feedback for frequency tuning"],
    countermeasures: "Active noise cancellation is partially effective. Physical ear protection reduces damage threshold. The individual units are fragile — a broom or thrown object can destroy them. Wind disrupts swarm formation. 30-minute battery life limits engagement duration.",
    known_deployments: ["Crowd dispersal operations", "Area denial", "Suspected use in targeted assassination via acoustic overexposure"],
    story_hooks: [
      "A Cerebral Dirge deployment at a Shelf protest exceeded the lethality threshold. Seventeen dead from cerebral hemorrhage. TESSERA claims the operator error was deploying reserve units without authorization. The operator claims they were told to use 'whatever it takes.'",
      "Someone has acquired a Cerebral Dirge swarm and is using it for targeted assassination — deploying 500 units around a single person's apartment while they sleep. The cause of death is listed as 'stroke.' The third 'stroke' this month."
    ],
    cultural_context: "The Cerebral Dirge represents the weaponization of sound itself — turning the air into a killing medium. The micro-drone size means the swarm is nearly invisible, making the weapon appear to be a natural phenomenon rather than a deliberate attack. People dying of 'unexplained' cerebral hemorrhage in areas where Cerebral Dirge deployments were authorized raises questions nobody in authority wants to answer.",
    tags: ["automaton", "swarm", "aerial", "acoustic", "weapon", "tessera", "corporate", "tier 3"]
  },
  {
    name: "Lazarus MB-3 'Mosquito'",
    type: "automaton",
    classification: "Micro Aerial — Pharmaceutical Injection",
    aliases: ["Mosquito", "Needle Fly", "The Bite"],
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "The MB-3 is a micro aerial drone the size of an actual mosquito, equipped with a hollow proboscis capable of penetrating exposed skin and delivering a micro-dose of pharmaceutical compound directly into the bloodstream. The unit flies with a wing-buzz frequency matched to local insect populations, making it aurally indistinguishable from a real mosquito. The injection feels like a mosquito bite — a minor irritation that targets brush off without a second thought.\n\nLazarus developed the MB-3 for 'targeted pharmaceutical delivery in field conditions' — administering vaccines, antivenoms, and emergency medications to patients who cannot be reached by conventional medical personnel. This is a real capability that saves real lives in disaster response scenarios. It is also a capability that allows the silent, deniable injection of any substance into any person without their knowledge or consent.\n\nThe MB-3's micro-dose capacity limits the range of effective payloads — it can't deliver enough volume for most fast-acting poisons. But it can deliver enough for slow-acting toxins, tracking compounds that mark a target's blood chemistry for identification, mood-altering pharmaceuticals, and — most insidiously — compounds that interact with specific BCI models to create exploitable vulnerabilities. The target scratches the bite, forgets about it, and never connects the minor irritation to the cascade of symptoms that follows days later.",
    tier_availability: "Tier 4+",
    legality: "Medical: Licensed. Weaponized: Does not officially exist",
    autonomy_level: "Pre-programmed target acquisition with autonomous flight",
    dimensions: "6mm body length — mosquito-sized",
    weight: "0.001 kg",
    power_source: "Micro capacitor, 5-minute flight time",
    locomotion: "Wing-pair with insect-mimetic flight pattern",
    armament: ["Hollow proboscis with 0.05mL pharmaceutical payload"],
    sensors: ["CO2 detection (target localization)", "Thermal targeting", "Skin exposure identification"],
    countermeasures: "Insect repellent disrupts the CO2 detection. Sealed clothing prevents skin access. Air filtration prevents entry into enclosed spaces. The unit is physically identical to a mosquito and effectively impossible to distinguish from real insects without magnification.",
    known_deployments: ["Medical field delivery (legitimate)", "Weaponized: classified, suspected in multiple cases of unexplained illness and behavioral changes among targeted individuals"],
    story_hooks: [
      "A corporate executive has been making irrational decisions for weeks — mood swings, paranoia, cognitive decline. Blood work reveals trace compounds consistent with MB-3 delivery. Someone has been sending pharmaceutical mosquitoes to their bedroom every night.",
      "A Shelf neighborhood has experienced an outbreak of a rare neurological condition. The condition matches the side effect profile of a Lazarus experimental compound. The neighborhood also has a mosquito problem. The two facts are not unrelated."
    ],
    cultural_context: "The MB-3 makes the act of being bitten by a mosquito — one of the most common experiences in GLMZ's humid climate — a potential threat vector. It adds another layer of paranoia to a city already saturated with invisible dangers.",
    tags: ["automaton", "micro", "aerial", "pharmaceutical", "injection", "weapon", "lazarus", "corporate", "stealth", "tier 4"]
  },
  {
    name: "Crucible Industries CX-1 'Golem'",
    type: "automaton",
    classification: "Heavy Platform — Construction/Demolition Combat",
    aliases: ["Golem", "Hard Hat", "The Builder"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CX-1 is a 3-meter bipedal construction automaton — a walking crane, welder, and demolition rig in one chassis. Its arms terminate in modular tool mounts that accept construction equipment: pneumatic hammers, welding torches, cutting saws, concrete sprayers, and rebar grippers. It was designed to perform heavy construction tasks autonomously, operating on building sites 24 hours a day without rest, breaks, or safety complaints.\n\nThe CX-1 was not designed for combat. But a machine that drives rivets through structural steel can drive them through people. A machine that cuts through I-beams can cut through anything softer. A machine that demolishes walls can demolish whatever is behind them. The construction tool mounts accept weapons as readily as they accept tools — someone welded a machine gun mount onto a CX-1 arm within a week of the first unit being deployed.\n\nThe Golem has become the Shelf's heavy hitter — the closest thing to a walking tank that non-corporate forces can field. Stolen, reprogrammed, and jury-rigged with improvised weapons, CX-1 units have appeared in every major Shelf conflict in the past five years. They are slow, crude, and terrifyingly effective. A Golem walking down a street with a pneumatic hammer in one hand and an improvised flamethrower in the other is not sophisticated warfare. It is, however, warfare.",
    tier_availability: "Tier 2+ (construction); Black market (combat)",
    legality: "Construction: Licensed. Weaponized: Prohibited",
    autonomy_level: "Autonomous construction / operator controlled (combat)",
    dimensions: "3.0m height, 1.8m shoulder width",
    weight: "2,400 kg",
    power_source: "Industrial fuel cell, 24-hour endurance",
    locomotion: "Bipedal heavy — 10 km/h maximum, not designed for speed",
    armament: ["Modular tool mounts (accepts construction tools or improvised weapons)", "Chassis-mounted improvised armor (combat variants)"],
    sensors: ["Construction site navigation", "Obstacle detection", "Blueprint interpretation"],
    countermeasures: "Extremely slow. Not designed for evasive action. Industrial electronics vulnerable to EMP. Joint actuators designed for construction loads, not combat stress — sustained combat causes accelerated wear. The unit's height makes it visible from blocks away.",
    known_deployments: ["Construction sites across GLMZ", "Shelf combat operations (reprogrammed units)", "Resistance group heavy support"],
    story_hooks: [
      "A Shelf community has a CX-1 — their Golem, their protector. It defends the neighborhood from gang incursions and corporate raids. But its construction-grade actuators are failing from combat stress. If it breaks down, the neighborhood is defenseless. Finding replacement parts means dealing with Crucible, who wants their stolen machine back.",
      "A fleet of CX-1 construction units at a building site has been remotely activated at night with combat programming. They're demolishing the building they were constructing — with workers still inside the temporary housing quarters."
    ],
    cultural_context: "The Golem is the Shelf's folk hero automaton — the construction worker that became a warrior. Its presence in Shelf defense narratives is unique: it's the only automaton that lower-tier communities celebrate rather than fear. Songs have been written about specific Golem units that defended neighborhoods.",
    tags: ["automaton", "bipedal", "construction", "heavy", "weapon", "crucible", "shelf", "resistance", "tier 2"]
  },
  {
    name: "Arcturus MX-1 'Cerberus'",
    type: "automaton",
    classification: "Multi-Platform — Three-Headed Sentry",
    aliases: ["Cerberus", "Triple Threat", "Three-Face"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The MX-1 is a large quadruped platform with three independently articulating sensor/weapon heads mounted on flexible neck stalks. Each head contains a thermal camera, a 5.56mm automatic weapon, and an independent target acquisition system. The three heads can engage three separate targets simultaneously, track threats in a 360-degree arc, and provide overlapping fields of fire that eliminate blind spots.\n\nThe Cerberus is designed as a facility patrol unit — a single MX-1 replaces three conventional sentry drones. It patrols a designated route, scanning continuously with three independent sensor arrays, and engages any threat from any direction without needing to turn. The psychological impact of facing a three-headed mechanical dog is considerable — targets report difficulty deciding which head to address, which weapon to avoid, and which direction constitutes 'away.'\n\nThe three heads occasionally exhibit coordinated behavioral patterns that weren't explicitly programmed — one head watches a threat while another tracks potential flanking routes while the third scans the ceiling. Arcturus attributes this to the collaborative threat-assessment algorithm. The effect is a machine that appears to think in three directions at once, which it essentially does.",
    tier_availability: "Tier 4+",
    legality: "Licensed for facility security",
    autonomy_level: "Fully autonomous patrol and engagement",
    dimensions: "1.0m shoulder height, 1.8m length, neck stalks extend to 1.5m",
    weight: "120 kg",
    power_source: "Hydrogen fuel cell, 72-hour patrol endurance",
    locomotion: "Quadruped — 40 km/h sprint",
    armament: ["3x 5.56mm automatic weapons (one per head, 200 rounds each)", "3x independent target acquisition systems"],
    sensors: ["3x thermal cameras", "3x acoustic sensors", "Collaborative threat assessment network"],
    countermeasures: "The neck stalks are structural weak points. Destroying one head reduces capability by a third. Smoke and thermal obscurants force all three heads to rely on acoustic tracking, which is less precise. The collaborative algorithm can be confused by simultaneous threats from more than three directions.",
    known_deployments: ["Corporate facility patrol", "Perimeter security", "High-value asset protection"],
    story_hooks: [
      "A Cerberus unit is patrolling a facility the players need to enter. One of its three heads has developed a malfunction — it occasionally targets authorized personnel. The facility hasn't taken the unit offline because the other two heads still work. The malfunction might be exploitable.",
      "Someone has stolen a Cerberus and reprogrammed it to guard a Shelf neighborhood. Three heads watching three directions — the neighborhood has never been safer. But the hydrogen cell will run out in three days, and Arcturus is coming to get their property back."
    ],
    cultural_context: "The Cerberus draws on mythological imagery that resonates across GLMZ's polyglot population — the three-headed guardian that watches all paths. Its deployment at facility entrances explicitly invokes the 'guardian of the underworld' metaphor, which Arcturus's marketing team chose deliberately.",
    tags: ["automaton", "canine", "multi-head", "sentry", "weapon", "arcturus", "corporate", "security", "tier 4"]
  },
  {
    name: "TESSERA TN-1 'Cradle'",
    type: "automaton",
    classification: "Containment — Autonomous Prisoner Transport",
    aliases: ["Cradle", "Iron Maiden", "The Box"],
    manufacturer: "TESSERA",
    description: "The TN-1 is a six-legged walking platform with a central compartment designed to contain a single human being. The compartment is a reinforced capsule with restraint systems, life support, and sedation capability. The TN-1 identifies its designated target through biometric confirmation, incapacitates them using a short-range taser or chemical spray, loads them into the compartment using articulated manipulator arms, seals the capsule, and walks to a designated delivery point. It is an autonomous kidnapping machine.\n\nTESSERA describes the TN-1 as an 'automated high-risk detainee transport system' designed for situations where human security personnel face unacceptable risk — transporting augmented prisoners, moving infectious disease patients, or extracting VIPs from hostile environments. All of these are legitimate applications. They account for a fraction of TN-1 deployments.\n\nThe reality is that the TN-1 is used for extrajudicial detention. Corporate security designates a target, the Cradle walks to them, takes them, and delivers them to wherever the corporation wants them. The target disappears off the street, enclosed in a walking box that nobody can see inside. No witnesses see a face. No recordings show a human being taken. The TN-1 walks through crowds with its cargo and nobody knows if the box is empty or if someone is inside it, sedated and restrained, being carried to a place they will never be seen again.",
    tier_availability: "Tier 5",
    legality: "Licensed for high-risk detainee transport",
    autonomy_level: "Fully autonomous — biometric target acquisition, incapacitation, and transport",
    dimensions: "1.8m height, 2.0m length, containment capsule: 0.6m x 0.6m x 1.9m",
    weight: "450 kg",
    power_source: "Hydrogen fuel cell, 24-hour endurance",
    locomotion: "Hexapod all-terrain walker",
    armament: ["Short-range taser", "Chemical incapacitation spray", "Restraint system", "Sedation injection (capsule interior)"],
    sensors: ["Biometric target identification", "Crowd navigation", "Capsule occupant vital sign monitoring"],
    countermeasures: "Preventing the initial incapacitation is the best countermeasure — the taser and chemical spray are short-range. The capsule can be opened from outside with cutting tools. Destroying the unit's legs immobilizes it with the capsule intact. The biometric targeting can be confused by cosmetic surgery or augmentation changes that alter facial structure and gait.",
    known_deployments: ["High-risk prisoner transport", "VIP extraction", "Extrajudicial detention operations (documented by civil rights organizations)"],
    story_hooks: [
      "A TN-1 was seen walking through the Shelf carrying someone. It walked for six hours and was delivered to a TESSERA black site. The person inside was a union organizer. The arrest warrant was issued two days after the detention — TESSERA filed the paperwork retroactively.",
      "A player character has been designated as a TN-1 target. The Cradle is somewhere in the city, walking toward them. They don't know what it looks like. They know it's coming."
    ],
    cultural_context: "The Cradle is the automaton that represents the disappearance — the mechanism by which people are removed from society without due process, without witnesses, and without recourse. Its walking form means it can reach places vehicles cannot, and its autonomous operation means no human has to make the moral choice to kidnap someone.",
    tags: ["automaton", "hexapod", "containment", "kidnapping", "weapon", "tessera", "corporate", "justice", "horror", "tier 5"]
  },
  {
    name: "Arcturus GW-1 'Gargoyle'",
    type: "automaton",
    classification: "Static Sentry — Architectural Integration",
    aliases: ["Gargoyle", "Building Watcher", "Stone Cold"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The GW-1 is designed to be permanently mounted on building exteriors — cornices, ledges, rooftops — where its angular, organic chassis blends with architectural detail. In dormant mode, the Gargoyle is indistinguishable from a decorative stone fixture. Its matte grey composite shell weathers naturally, accumulating grime and discoloration that enhance its camouflage over time. Older units look like they've been part of the building for decades.\n\nWhen activated, the GW-1 unfolds — articulated wings reveal a sensor suite with 2km optical range, and a belly-mounted precision rifle deploys from the chassis. The unit can engage targets with single aimed shots at ranges up to 800 meters, then refold and resume dormant appearance in under three seconds. The building's occupants may never know their exterior decoration is a sniper platform.\n\nArcturus has been installing Gargoyles on corporate buildings for over a decade. The accumulated network represents thousands of hidden sniper platforms distributed across GLMZ's skyline, each owned by the building's tenant corporation but networked through Arcturus's command infrastructure. The cumulative effect is a city where every building might be watching you through a rifle scope, and you'd never know it because the shooter looks like a stone ornament.",
    tier_availability: "Tier 4+",
    legality: "Licensed for building defense",
    autonomy_level: "Dormant autonomous / remote activated for engagement",
    dimensions: "0.8m height, 1.2m wingspan (deployed), 0.6m profile (dormant)",
    weight: "95 kg (permanently mounted)",
    power_source: "Building power with solar backup, indefinite endurance",
    locomotion: "None — permanent architectural mount",
    armament: ["Belly-mounted precision rifle (7.62mm, 800m effective range, 50-round internal magazine)"],
    sensors: ["2km optical zoom", "Thermal imaging", "Facial recognition", "Environmental awareness (wind, humidity for ballistic calculation)"],
    countermeasures: "Identifying Gargoyles requires close inspection of building exteriors — at distance they are indistinguishable from genuine architectural features. Counter-sniper techniques apply once a Gargoyle's position is revealed. Destroying the building power supply forces the unit to solar backup, which may be insufficient for sustained engagement.",
    known_deployments: ["Corporate buildings across GLMZ — estimated thousands of units", "Government buildings", "Strategic infrastructure"],
    story_hooks: [
      "A Gargoyle has been shooting people — one shot each, one per night, from a building in the financial district. The building's owner claims no knowledge. Arcturus claims the unit isn't in their network. Someone activated a dormant Gargoyle with unauthorized targeting data.",
      "The players need to cross an open plaza. They know Gargoyles are on at least two of the surrounding buildings. They don't know which architectural details are real and which are weapons. Every stone fixture is a potential threat."
    ],
    cultural_context: "The Gargoyle turns architecture into armament. The knowledge that any building decoration might be a concealed weapon platform has changed how people move through the city — hugging walls, avoiding open sightlines, never looking too long at the buildings watching them.",
    tags: ["automaton", "static", "sniper", "architectural", "weapon", "arcturus", "corporate", "stealth", "tier 4"]
  },
  {
    name: "Ringo RR-1 'Rattlesnake'",
    type: "automaton",
    classification: "Serpentine — Area Denial",
    aliases: ["Rattlesnake", "Shake", "Ground Wire"],
    manufacturer: "RINGO CorpoNation",
    description: "The RR-1 is a 2-meter serpentine automaton that mimics snake locomotion, moving through grass, rubble, and debris with a lateral undulation that makes it nearly invisible at ground level. Its primary function is area denial through electrocution — the entire body is coated in a high-voltage discharge surface that delivers an incapacitating shock to anything it contacts. The Rattlesnake patrols a designated perimeter, and anything that crosses the line gets 50,000 volts.\n\nRingo deploys RR-1 units in agricultural zones as crop protection — the units patrol field perimeters and electrify anything that enters without authorization, from animal pests to human trespassers. The shock is rated as non-lethal for healthy adults, but agricultural workers in disputed zones are frequently not healthy adults — malnourished, augmented with cheap hardware that amplifies electrical discharge, or simply old. The RR-1's non-lethal rating assumes a target population that doesn't exist in the places it's deployed.\n\nThe RR-1's 'rattle' is a deliberate design feature — the tail section produces a buzzing vibration when the unit detects an approaching target, providing a warning that sounds exactly like a biological rattlesnake. In agricultural regions where venomous snakes are a genuine hazard, the sound triggers immediate freeze-and-retreat responses. By the time the target realizes the rattle came from a mechanical source, they've already changed direction — which is the intended outcome. The rattle is a weapon that fires on the nervous system.",
    tier_availability: "Tier 3+",
    legality: "Licensed for agricultural perimeter security",
    autonomy_level: "Fully autonomous patrol",
    dimensions: "2.0m length, 0.08m diameter",
    weight: "8 kg",
    power_source: "Solar-supplemented lithium cell, 72-hour endurance",
    locomotion: "Serpentine lateral undulation — ground-level, near-silent",
    armament: ["Full-body high-voltage discharge surface (50,000V contact)", "Acoustic rattle warning system"],
    sensors: ["Ground vibration detection", "Thermal sensing", "Perimeter boundary awareness"],
    countermeasures: "Insulated footwear and clothing prevent shock. Elevated positions above ground level avoid contact. The unit's low profile makes it nearly invisible but also makes it vulnerable to overhead attack. Cold temperatures slow the unit's movement significantly.",
    known_deployments: ["Ringo agricultural perimeter security", "Industrial site ground-level patrol", "Black market units used in Shelf territorial control"],
    story_hooks: [
      "A field of RR-1 units has been deployed around a Shelf water collection point. The community can't reach their water supply without crossing a line of electric snakes. Ringo claims the land. The community was here first.",
      "Someone has modified RR-1 units to deliver lethal voltage and released them in a rival gang's territory. The streets are mined with invisible electric snakes that kill on contact."
    ],
    cultural_context: "The Rattlesnake turns the ground itself into a threat. In areas where RR-1 units are deployed, people walk on walls, climb fences, and refuse to step on grass. The sound of a rattle — mechanical or biological — triggers panic responses in populations that have experienced deployment.",
    tags: ["automaton", "serpentine", "electric", "area denial", "weapon", "ringo", "corporate", "agricultural", "tier 3"]
  },
  {
    name: "Vantablack VD-2 'Paparazzi'",
    type: "automaton",
    classification: "Pursuit Drone — Harassment/Documentation",
    aliases: ["Paparazzi", "Buzz", "The Follow"],
    manufacturer: "VANTABLACK MEDIA",
    description: "The VD-2 is a small quadrotor drone equipped with a high-definition camera, a directional microphone, and a blinding strobe light. It follows designated individuals at a distance of 2-5 meters, continuously recording audio and video, and deploying its strobe to disorient targets who attempt to evade or cover their face. The VD-2 is not a weapon in any conventional sense. It is a harassment platform that uses documentation as a form of violence.\n\nVantablack deploys the VD-2 against individuals of 'public interest' — a category defined entirely by Vantablack's editorial staff and encompassing anyone whose documentation might generate revenue. The drone follows them everywhere: into their homes through open windows, into bathrooms, into medical appointments. It records everything. The footage is owned by Vantablack. The subject has no legal recourse under GLMZ's broad documentation freedom statutes.\n\nThe VD-2's real weapon is the strobe — a high-intensity light that can cause temporary blindness, trigger photosensitive epilepsy, and create disorienting afterimages that persist for minutes. Deployed against fleeing targets, the strobe causes stumbling, collisions, and falls. Against individuals with optical augmentations, the strobe can cause feedback loops that damage enhanced vision systems. Vantablack classifies the strobe as 'illumination equipment for documentation in low-light conditions.'",
    tier_availability: "Tier 2+",
    legality: "Licensed as documentation equipment",
    autonomy_level: "Semi-autonomous pursuit with editorial direction",
    dimensions: "0.2m rotor span",
    weight: "0.8 kg",
    power_source: "Lithium cell, 6-hour endurance with solar supplement",
    locomotion: "Quadrotor",
    armament: ["High-intensity strobe light (temporary blindness, augmentation damage)", "Continuous documentation (psychological harassment)"],
    sensors: ["Facial recognition", "Gait tracking for pursuit", "Audio recording", "Low-light/night vision camera"],
    countermeasures: "Physical destruction (common response). Signal jamming prevents real-time broadcast but not local recording. Anti-flash optical augmentations reduce strobe effectiveness. Indoor spaces with closed windows and doors prevent entry. Legal action against Vantablack has a 0% success rate to date.",
    known_deployments: ["Following persons of interest across GLMZ", "Event documentation", "Targeted harassment campaigns against individuals who have opposed Vantablack Media interests"],
    story_hooks: [
      "A VD-2 has been following a player character for three days. Everything they've done — every meeting, every transaction, every moment of vulnerability — is on Vantablack's servers. The footage will be broadcast tomorrow unless someone retrieves it tonight.",
      "Vantablack has deployed a VD-2 swarm — twenty drones — to follow every member of a resistance cell simultaneously. Each cell member is now a potential security leak, broadcasting their movements and contacts in real time."
    ],
    cultural_context: "The VD-2 represents the weaponization of observation. In a city where information is power, a drone that follows you everywhere, records everything, and broadcasts it to whoever pays is a weapon that strips privacy, dignity, and operational security simultaneously.",
    tags: ["automaton", "aerial", "surveillance", "harassment", "media", "vantablack", "corporate", "tier 2"]
  },
  {
    name: "Arcturus CK-8 'Hound of Tindalos'",
    type: "automaton",
    classification: "Canine Platform — Dimensional Tracking",
    aliases: ["Tindalos Hound", "Corner Dog", "Angle Chaser"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS — PARATECHNOLOGICAL DIVISION",
    description: "The CK-8 is a quadruped tracking platform that shouldn't exist according to Arcturus's official product catalog. It resembles a CK-5 Bloodhound in basic chassis design but with significant modifications: the skull housing is elongated and contains sensor equipment that doesn't correspond to any known detection technology, the joint articulation allows the legs to bend in directions that standard mechanical engineering shouldn't permit, and the unit has been documented appearing in locations it couldn't have reached through any conventional path.\n\nArcturus's paratechnological division — which Arcturus denies having — developed the CK-8 to track targets through what internal documents refer to as 'angular space.' The unit doesn't follow physical paths. It navigates through geometric relationships between hard angles — corners, edges, intersections of flat surfaces — appearing to phase through walls at points where two or more right angles meet. Field observations describe the unit as visible only at corners, as if it exists in the spaces where straight lines intersect and is invisible everywhere else.\n\nThe CK-8 has been deployed exactly seven times. In all seven cases, the target was found. In three cases, the target was found in locations they hadn't told anyone about. In one case, the target was found inside a sealed room with no windows or doors. The CK-8 cannot be outrun because it doesn't use the same space you do. It cannot be evaded because it tracks through geometry, not chemistry or heat or sound. It finds you because you exist in a space that has corners.",
    tier_availability: "Tier 5 — classified paratechnological",
    legality: "Does not officially exist. The technology does not officially exist. Arcturus's paratechnological division does not officially exist.",
    autonomy_level: "Unknown — may not be fully mechanical",
    dimensions: "0.7m shoulder height approximately — dimensions appear inconsistent between observations",
    weight: "Unknown — does not consistently register on scales",
    power_source: "Unknown",
    locomotion: "Angular-space traversal — appears and disappears at geometric corners",
    armament: ["Unknown — targets are found dead or alive depending on mission parameters. Cause of death in lethal deployments is listed as 'acute geometric trauma,' a classification that doesn't exist in any medical database"],
    sensors: ["Angular-space tracking — mechanism unknown", "Target identification through unknown means"],
    countermeasures: "Unconfirmed. Circular rooms without corners may prevent manifestation. Curved architecture appears to create navigation difficulties for the unit. Personnel who have survived CK-8 pursuit report that the unit cannot appear in spaces without right angles.",
    known_deployments: ["Seven classified operations. All successful. Details sealed."],
    story_hooks: [
      "Something has been seen at corners — in the edge of vision, at the intersection of walls and floor, in the geometry of doorframes. A quadruped shape that appears for a fraction of a second and then isn't there. Someone has been designated as a CK-8 target. The countdown is the number of corners between here and wherever it currently is.",
      "An Arcturus scientist from the paratechnological division has fled the company. They're living in a room they've built with no right angles — curved walls, domed ceiling, rounded floor. They haven't left in six months. They say if they do, the geometry will find them."
    ],
    cultural_context: "The CK-8 is urban legend made real — the monster that comes from corners, the thing you see in peripheral vision at the edge of a wall. Most people in GLMZ don't believe it exists. The seven people who were targeted by it include five who can no longer disagree.",
    tags: ["automaton", "canine", "paratechnological", "classified", "weapon", "arcturus", "horror", "tier 5"]
  },
  {
    name: "Lazarus NB-1 'Cradle Robber'",
    type: "automaton",
    classification: "Medical Platform — Neonatal Extraction",
    aliases: ["Cradle Robber", "The Stork", "Baby Snatcher"],
    manufacturer: "LAZARUS PHARMACEUTICALS",
    description: "The NB-1 is a medical automaton designed for emergency neonatal extraction — removing newborns from dangerous environments when human medical personnel cannot safely access the location. The unit is small, wheeled, with articulated manipulator arms capable of the delicate handling required to safely secure and transport an infant. Its environmental protection capsule maintains temperature, oxygen, and humidity at neonatal ICU standards.\n\nLazarus designed the NB-1 for disaster response — extracting infants from collapsed buildings, evacuating neonatal wards during emergencies, and performing field deliveries in conditions where human OB/GYN personnel face unacceptable risk. The unit has saved lives. It is also one of the most nightmarish objects ever created, because its capabilities don't stop at emergency extraction.\n\nThe NB-1 can enter any environment an infant is in and leave with the infant inside its capsule. This includes homes, hospitals, shelters, and anywhere a newborn happens to be. Lazarus's medical authority contracts with GLMZ allow the NB-1 to be deployed for 'child welfare interventions' — a designation that covers everything from genuinely endangered infants to children seized from families who defaulted on medical debt incurred during birth. The machine that was designed to rescue babies is also the machine that takes them.",
    tier_availability: "Tier 4+",
    legality: "Licensed as medical equipment — child welfare intervention authorization",
    autonomy_level: "Semi-autonomous with medical authority override",
    dimensions: "0.8m length, 0.5m width, 0.4m height",
    weight: "25 kg",
    power_source: "Lithium cell, 12-hour endurance",
    locomotion: "Four-wheeled, indoor/outdoor, stair-climbing capable",
    armament: ["None — medical platform. The horror is in the application, not the armament."],
    sensors: ["Neonatal vital sign monitoring", "Environmental hazard detection", "Navigation in damaged structures"],
    countermeasures: "Physical obstruction of the unit's path. The manipulator arms are not designed for combat and can be blocked. Legal challenges to medical authority orders (slow, expensive, rarely successful). Community intervention — the NB-1 has been physically blocked by neighborhood residents in documented incidents.",
    known_deployments: ["Emergency neonatal extraction (legitimate)", "Child welfare interventions (contested)", "Medical debt enforcement involving infant seizure (documented by civil rights organizations)"],
    story_hooks: [
      "An NB-1 is rolling through the Shelf toward a specific address. A Lazarus medical authority order authorizes the extraction of a three-week-old infant whose parents defaulted on birth-related medical debt. The neighborhood has been warned. They're building a barricade.",
      "An NB-1 has taken an infant from a Shelf home under a medical authority order. The order was legitimate — the home was genuinely dangerous. But the infant is now in Lazarus custody, and the family can't afford the legal process to get their child back. Someone needs to navigate the system — or go around it."
    ],
    cultural_context: "The NB-1 is the automaton that makes parents in the Shelf afraid to go to hospitals. Every medical debt incurred during childbirth carries the implicit threat that a machine might come for the child. The fear suppresses medical care-seeking behavior, which increases infant mortality, which Lazarus uses as justification for expanded NB-1 deployment. The cycle is self-reinforcing.",
    tags: ["automaton", "medical", "neonatal", "extraction", "lazarus", "corporate", "control", "horror", "tier 4"]
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

console.log(`\nGenerated ${count} automata files in ${OUTPUT_DIR}`);
