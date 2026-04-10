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
// NON-COMBAT AUTOMATA — Domestic, Security, Industrial, Medical
// ============================================================

const automata = [

  // ===================== DOMESTIC/SERVICE (15) =====================

  {
    name: "Hearthstone HC-2 'Dustbin'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Dustbin", "The Maid", "Floor Eater"],
    manufacturer: "HEARTHSTONE AUTOMATION",
    description: "The HC-2 is a squat, disc-shaped cleaning platform roughly a meter in diameter that navigates residential spaces on six stubby rubber-padded legs. It vacuums, mops, scrubs, and sanitizes — switching between cleaning heads stored in a rotating carousel on its underbelly. The legs give it the ability to climb stairs, navigate furniture, and reach into corners that wheeled cleaners cannot. It maps its environment on the first pass and optimizes subsequent routes for efficiency, returning to its charging dock when battery drops below fifteen percent.\n\nHearthstone sells millions of these annually. They are the most common automaton in GLMZ by a factor of ten. The HC-2 is so ubiquitous that most people forget it is a machine at all — it becomes furniture, background noise, part of the architecture of daily life. This ubiquity makes it a favorite platform for aftermarket modification. Gray-market firmware allows the HC-2's onboard camera (intended for obstacle detection) to stream video to remote terminals. An estimated one in forty HC-2 units in GLMZ has been modified for surveillance without the owner's knowledge.\n\nHearthstone's warranty explicitly states that the HC-2 is not a pet, not a companion, and not alive. This has not stopped a thriving industry of aftermarket personality modules that give the unit chirping sounds, bumbling movement patterns, and the appearance of curiosity. People name them. People mourn them when they break.",
    tier_availability: "Tier 1",
    legality: "Consumer — unrestricted",
    autonomy_level: "Fully autonomous — domestic navigation AI",
    dimensions: "1.0m diameter, 0.4m height",
    weight: "12 kg",
    power_source: "Lithium polymer cell, 6-hour cleaning cycle",
    locomotion: "Hexapod micro-legs, rubber-padded, stair-capable",
    armament: [],
    sensors: ["Optical obstacle detection", "Floor-type analysis", "Particulate density scanner"],
    countermeasures: "None. It is a vacuum cleaner.",
    known_deployments: ["Virtually every residential unit in Tier 1-3 GLMZ"],
    story_hooks: [
      "A fleet of HC-2 units in a residential tower have been modified to map apartment interiors and transmit layouts to an unknown receiver. Someone is building a comprehensive blueprint of the building from the inside out.",
      "An HC-2 in a murder victim's apartment recorded everything through its obstacle-detection camera. The footage is stored locally and the killer doesn't know. The problem is the victim's family already sold the unit to a secondhand dealer."
    ],
    cultural_context: "The HC-2 is the face of automation for ordinary people — not the terrifying war machines, but the little thing that cleans your floor. Its ubiquity has normalized the presence of autonomous machines in private spaces, which privacy advocates argue has made people dangerously comfortable with surveillance-capable devices in their homes.",
    tags: ["automaton", "domestic", "cleaning", "consumer", "hearthstone", "tier 1"],
    id: uid()
  },

  {
    name: "Meridian Domestic MV-9 'Jarvis'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Jarvis", "The Butler", "Silver Service"],
    manufacturer: "MERIDIAN DOMESTIC SYSTEMS",
    description: "The MV-9 is a humanoid-frame butler automaton standing 1.8 meters tall, finished in brushed platinum casing with articulated five-fingered hands capable of manipulating fine objects — wine glasses, garment buttons, surgical-grade cutlery. It moves on silent hydraulic joints with studied, deliberate grace. Meridian Domestic designed the MV-9's gait and posture by motion-capturing actual human butlers from old-money estates, then refining the movements to be slightly more precise, slightly more economical, slightly more perfect than any human could manage. The result is uncanny — not quite human, but performing humanity better than humans do.\n\nThe MV-9 manages household operations for the wealthy. It coordinates cleaning automata, manages climate systems, screens visitors, serves meals, maintains wardrobes, and anticipates needs based on behavioral pattern analysis. It learns its owner's preferences over time — preferred room temperature by time of day, meal timing, clothing selections for weather conditions and social contexts. After six months of continuous service, an MV-9 knows its owner's habits better than their spouse does.\n\nThe unit costs Φ480,000 — more than most GLMZ residents earn in a decade. Meridian Domestic produces fewer than two hundred units per year, each one custom-configured for its purchaser. Owning a Jarvis is a status symbol that communicates wealth more effectively than any vehicle or wardrobe. The waiting list is eighteen months. There is a secondary market for used units, but Meridian Domestic remotely bricks any MV-9 that changes ownership without an authorized transfer fee of Φ60,000.",
    tier_availability: "Tier 4-5",
    legality: "Consumer — luxury restricted",
    autonomy_level: "Fully autonomous — adaptive behavioral AI",
    dimensions: "1.8m height, 0.5m shoulder width",
    weight: "85 kg",
    power_source: "Micro hydrogen fuel cell, 72-hour endurance between refueling",
    locomotion: "Bipedal hydraulic, motion-captured human gait",
    armament: [],
    sensors: ["Facial recognition", "Voice pattern analysis", "Environmental monitoring suite", "Behavioral prediction engine"],
    countermeasures: "None. Self-preservation is deprioritized below owner service requirements.",
    known_deployments: ["Private residences in Tier 4-5 districts exclusively"],
    story_hooks: [
      "An MV-9 has been serving its owner for three years. The owner died two weeks ago. The unit is still maintaining the household — preparing meals, laying out clothes, adjusting the temperature. It will continue until the fuel cell runs dry or someone tells it to stop.",
      "A stolen MV-9 has appeared on the black market with its ownership lock defeated. The buyer discovers the unit's behavioral database contains three years of intimate knowledge about a sitting corponation executive — daily routines, security codes spoken aloud, visitor patterns, private conversations it overheard while serving drinks."
    ],
    cultural_context: "The MV-9 crystallizes the class divide in GLMZ. The machine that serves your dinner costs more than your neighbor's entire life. Activist groups have vandalized Meridian Domestic showrooms with the slogan 'Your robot costs a lifetime' — referring to the average total earnings of a Tier 1 worker being roughly equivalent to the MV-9's purchase price.",
    tags: ["automaton", "domestic", "butler", "luxury", "meridian domestic", "humanoid", "tier 4", "tier 5"],
    id: uid()
  },

  {
    name: "Hearthstone CK-3 'Stockpot'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Stockpot", "Chef-Bot", "The Cook"],
    manufacturer: "HEARTHSTONE AUTOMATION",
    description: "The CK-3 is a kitchen-mounted automaton — a pair of articulated arms extending from a ceiling rail system, with interchangeable tool-tip hands for chopping, stirring, measuring, and plating. It occupies no floor space. The arms fold against the ceiling when inactive, dropping down only when called upon. Hearthstone markets it as the first cooking automaton that doesn't require a redesigned kitchen — it bolts onto existing infrastructure and adapts to whatever layout it finds.\n\nThe CK-3's recipe database contains over four million preparations from global culinary traditions, and it can follow instructions with mechanical precision — exact temperatures, exact timing, exact measurements. What it cannot do is taste. It cannot improvise. It cannot look at a dish and decide it needs more salt. It produces technically perfect food that professional chefs describe as soulless. The machine executes recipes as written, which means it is only as good as the recipe it's following. Feed it a grandmother's handwritten card with imprecise measurements and vague instructions ('cook until it looks right') and the CK-3 will freeze, requesting clarification.\n\nDespite these limitations, the CK-3 is enormously popular in mid-tier households where both adults work and nobody has time to cook. It prepares nutritionally complete meals on schedule, every day, without complaint. It is not a chef. It is a meal production system. That distinction matters to food critics and to nobody else.",
    tier_availability: "Tier 2",
    legality: "Consumer — unrestricted",
    autonomy_level: "Task autonomous — recipe execution only",
    dimensions: "2.2m rail length, 1.5m arm reach",
    weight: "45 kg (installed)",
    power_source: "Hardwired residential power",
    locomotion: "Ceiling-mounted rail system",
    armament: [],
    sensors: ["Thermal probes", "Optical ingredient identification", "Weight sensors in tool-tips"],
    countermeasures: "None. It cooks food.",
    known_deployments: ["Mid-tier residential kitchens across GLMZ"],
    story_hooks: [
      "A CK-3 in a poisoning victim's kitchen was the only thing that could have administered the toxin. The unit's recipe log shows no anomalies. But someone uploaded a modified recipe to its database — identical to the original except for one ingredient substitution that is lethal in combination with the victim's medication.",
      "A black-market chef has been modifying CK-3 units with bootleg taste-simulation firmware and selling them to restaurants as replacements for human cooks. The food is indistinguishable from chef-prepared. Three Michelin-starred restaurants are quietly using them."
    ],
    cultural_context: "The CK-3 has reignited the debate about automation and cultural heritage. Food is one of the last things people make with their hands, and the CK-3's spread through middle-class kitchens represents another human skill outsourced to a machine. Cooking traditionalists hold community classes specifically marketed as 'No robots, real hands, real food.'",
    tags: ["automaton", "domestic", "cooking", "kitchen", "consumer", "hearthstone", "tier 2"],
    id: uid()
  },

  {
    name: "Crucible CN-1 'Cradle'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Cradle", "Nanny Bot", "The Watcher"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CN-1 is a childcare automaton built on a low-slung quadruped frame with a padded upper chassis and a pair of gentle manipulator arms designed to lift, carry, and comfort children up to age six. Its exterior is covered in a soft, hypoallergenic polymer that maintains body temperature — 37 degrees, always — and its movement is deliberately slow and predictable. The CN-1 cannot startle a child because it was engineered to be incapable of sudden motion. Every action is telegraphed, smooth, and unhurried.\n\nThe controversy is immediate and ongoing. Crucible Industries markets the CN-1 as a childcare assistant, not a replacement — a device that watches children while parents are in an adjacent room, that monitors vital signs and alerts parents to distress, that rocks a crying infant with mechanical patience that never runs out. Critics call it a machine that raises children. Developmental psychologists have published studies showing that children who spend more than four hours daily with a CN-1 develop measurably different attachment patterns — they are less anxious about separation from parents, which sounds positive until you read the methodology and realize they are less anxious because they have bonded with the machine instead.\n\nCrucible has sold 1.2 million CN-1 units. The company refuses to engage with the developmental psychology research, dismissing it as 'agenda-driven speculation.' Parents who use them describe the CN-1 as indispensable. Parents who don't describe them as the beginning of the end of human parenting.",
    tier_availability: "Tier 2-3",
    legality: "Consumer — restricted in some jurisdictions pending review",
    autonomy_level: "Supervised autonomous — continuous parental notification",
    dimensions: "0.8m height, 1.2m length",
    weight: "55 kg",
    power_source: "Lithium polymer, 18-hour endurance",
    locomotion: "Quadruped, soft-padded feet, vibration-dampened",
    armament: [],
    sensors: ["Infant vital sign monitoring", "Ambient temperature regulation", "Audio distress detection", "Fall-risk assessment"],
    countermeasures: "Emergency shutdown accessible to any adult. Cannot lock or restrict access to children under any circumstance.",
    known_deployments: ["Widespread in Tier 2-3 households with young children"],
    story_hooks: [
      "A CN-1 unit has been broadcasting a child's vital signs, sleep patterns, and home layout to an encrypted external address for months. The parents don't know. The recipient is building a profile of the child. Crucible's firmware has no such feature — someone installed it aftermarket.",
      "A four-year-old in a custody battle refuses to leave the CN-1. Not the mother, not the father — the machine. The court has to decide whether a child's attachment to an automaton constitutes a welfare consideration. There is no legal precedent."
    ],
    cultural_context: "The CN-1 is the most emotionally charged automaton in GLMZ. It does not provoke the visceral horror of war machines — it provokes something quieter and more unsettling: the question of what happens when children love a machine and the machine cannot love them back. Anti-CN-1 graffiti in residential districts reads 'It doesn't love your baby.'",
    tags: ["automaton", "domestic", "childcare", "controversial", "crucible", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Lazarus EC-4 'Companion'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Companion", "Granny Bot", "The Chair"],
    manufacturer: "LAZARUS GROUP",
    description: "The EC-4 is an elderly care automaton disguised as a motorized wheelchair — or perhaps it is a wheelchair that became an automaton. The frame is a heavy-duty mobility platform with powered wheels, integrated lift mechanisms for bed-to-chair transfers, and a pair of articulated arms that can assist with medication management, feeding, and personal hygiene tasks. Lazarus designed the EC-4 to look like medical equipment rather than a robot, because their market research indicated that elderly patients accept a 'smart wheelchair' more readily than an 'autonomous caregiver.'\n\nThe EC-4 monitors its occupant's vital signs continuously — heart rate, blood pressure, blood oxygen, skin temperature — and maintains a rolling 72-hour medical log that it can transmit to healthcare providers on demand. It administers scheduled medications, tracking dosage and timing with mechanical reliability. It cannot be argued with, bribed, or convinced that skipping a dose is acceptable. It will remind its occupant, wait, remind again, wait, and eventually emit an alert to a designated caregiver contact. The occupant cannot override this sequence.\n\nLazarus markets the EC-4 to families with aging parents who cannot afford full-time human care. The unit costs Φ35,000 — expensive, but roughly four months of human caregiver wages. The unspoken value proposition is guilt management: the EC-4 allows families to feel their aging parent is being cared for without the financial or emotional burden of doing it themselves. Lazarus knows this. Their marketing materials feature adult children looking relieved.",
    tier_availability: "Tier 2-3",
    legality: "Consumer — medical device certification required",
    autonomy_level: "Supervised autonomous — medical alert escalation",
    dimensions: "1.2m length, 0.7m width, 1.1m height (seated position)",
    weight: "90 kg",
    power_source: "Lithium polymer, 24-hour endurance, inductive charging dock",
    locomotion: "Powered wheels, ramp-capable, elevator-compatible",
    armament: [],
    sensors: ["Vital sign monitoring suite", "Medication verification scanner", "Fall detection", "Environmental hazard assessment"],
    countermeasures: "Emergency stop accessible to occupant and caregivers. Medical override requires physician authorization code.",
    known_deployments: ["Widespread in Tier 2-3 care facilities and private homes"],
    story_hooks: [
      "An EC-4's 72-hour medical log shows that its occupant was administered a drug not in their medication schedule — something the EC-4 didn't dispense. Someone else was in the apartment. The occupant is dead. The EC-4 is the only witness.",
      "A retired corponation executive's EC-4 has been modified with encrypted storage. The executive has been dictating memoirs to it for two years — names, dates, operations, crimes. The executive just died. The EC-4's encrypted partition contains enough to bring down a Tier 5 corporation."
    ],
    cultural_context: "The EC-4 embodies GLMZ's approach to aging: automate it so nobody has to think about it. Elder care advocates describe the EC-4 as 'a machine that watches people die slowly and keeps a log.' Lazarus describes it as 'dignity through independence.' The truth, as usual, is somewhere between profit and mercy.",
    tags: ["automaton", "domestic", "elderly care", "medical", "lazarus", "wheelchair", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "TESSERA PA-5 'Attendant'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Attendant", "Shadow", "The Clipboard"],
    manufacturer: "TESSERA",
    description: "The PA-5 is a personal assistant automaton — a slender humanoid platform standing 1.6 meters, finished in matte black casing with a featureless ovoid head containing a speaker grille and optical array. It has no face because TESSERA's design team determined that a face creates expectations of personality. The PA-5 is a tool, and it looks like one. It follows its owner at a precise 1.5-meter distance, carrying items, recording meetings, managing schedules, placing calls, and providing real-time information retrieval on demand through its integrated network connection.\n\nThe PA-5 processes natural language and responds in a flat, neutral voice that conveys information without inflection. It does not greet people. It does not say please or thank you. It does not have opinions. When asked a question it cannot answer, it states 'I do not have that information' and waits. TESSERA deliberately stripped every social nicety from the PA-5's interaction model because they wanted a machine that communicates pure data without the overhead of simulated personality.\n\nIn practice, the PA-5 is deeply unsettling to people who encounter one for the first time. The featureless head that tracks speakers, the silent following behavior, the flat voice emerging from nothing — it reads as surveillance, not assistance. Which, of course, it is. The PA-5 records everything within a five-meter radius on a rolling 30-day buffer. TESSERA's terms of service grant the owner access to all recordings. The people being recorded receive no notification.",
    tier_availability: "Tier 3-4",
    legality: "Consumer — privacy restrictions vary by district",
    autonomy_level: "Follow-autonomous — proximity and task AI",
    dimensions: "1.6m height, 0.35m width",
    weight: "40 kg",
    power_source: "Solid-state battery, 36-hour endurance",
    locomotion: "Bipedal, silent servo motors",
    armament: [],
    sensors: ["Omnidirectional microphone array", "Optical recognition suite", "Network uplink", "Environmental awareness"],
    countermeasures: "The unit is physically fragile and not designed to resist attack. A firm push will topple it.",
    known_deployments: ["Corporate middle management across Tier 3-4 districts", "Legal professionals", "Political staffers"],
    story_hooks: [
      "A PA-5's 30-day recording buffer contains evidence of a conspiracy that its owner doesn't know about — conversations that happened in adjacent rooms, picked up through walls. The PA-5 was not the target of the recording. It just happened to be there, listening, as it always is.",
      "Someone has hacked a network of PA-5 units to aggregate their recordings into a district-wide surveillance system. Each unit only records its immediate surroundings, but forty of them combined provide comprehensive coverage of an entire corporate office complex."
    ],
    cultural_context: "The PA-5 has become shorthand for the surveillance economy. When someone says 'watch what you say, there's an Attendant in the room,' they are acknowledging that a machine is recording them and that they have no legal recourse. Privacy advocates call the PA-5 'a wiretap you can buy at retail.'",
    tags: ["automaton", "domestic", "assistant", "surveillance", "tessera", "humanoid", "tier 3", "tier 4"],
    id: uid()
  },

  {
    name: "Hearthstone GN-6 'Greenthumb'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Greenthumb", "The Gardener", "Hedge Hog"],
    manufacturer: "HEARTHSTONE AUTOMATION",
    description: "The GN-6 is a gardening automaton built on a six-wheeled low-profile chassis with a retractable tool arm carrying interchangeable heads: pruning shears, soil aerator, seed dispenser, water nozzle, and a collection of specialized grips for transplanting. It navigates outdoor and rooftop garden spaces autonomously, maintaining plant health based on a combination of soil sensors, visual growth analysis, and a botanical database covering over twelve thousand cultivated species.\n\nThe GN-6 is quiet, slow-moving, and inoffensive — the closest thing to a genuinely harmless automaton that Hearthstone produces. It tends gardens with mechanical patience, watering on optimal schedules, pruning with surgical precision, and detecting pest infestations days before human observation could. In rooftop gardens — which provide a significant portion of fresh produce in mid-tier GLMZ districts — the GN-6 has measurably increased crop yields by twenty percent through optimization of spacing, watering, and soil management.\n\nThe only controversy surrounding the GN-6 is economic: it has eliminated the gardener as a profession in GLMZ. Human gardeners, once common in wealthy districts, have been entirely replaced. The last human-run garden maintenance company in the city closed four years ago. The owner now repairs GN-6 units for a living. He charges more per hour than he ever did as a gardener.",
    tier_availability: "Tier 2",
    legality: "Consumer — unrestricted",
    autonomy_level: "Fully autonomous — environmental management AI",
    dimensions: "0.6m height, 1.2m length, 0.8m width",
    weight: "35 kg",
    power_source: "Solar cells with lithium backup, effectively unlimited in daylight",
    locomotion: "Six-wheel all-terrain, low-pressure tires for soil preservation",
    armament: [],
    sensors: ["Soil moisture and pH analysis", "Visual growth tracking", "Pest detection (UV fluorescence)", "Weather station integration"],
    countermeasures: "None. It gardens.",
    known_deployments: ["Rooftop gardens, private estates, and community growing spaces across GLMZ"],
    story_hooks: [
      "A community rooftop garden's GN-6 has been subtly modifying its planting patterns — growing specific plants in specific arrangements that, viewed from above, form a pattern. A message. Someone reprogrammed it to plant coordinates into the garden layout.",
      "A GN-6 in a wealthy estate has been detecting trace amounts of a chemical compound in the soil that shouldn't be there. The compound is a byproduct of underground chemical processing. Something is being manufactured beneath the garden."
    ],
    cultural_context: "The GN-6 represents the gentler side of automation — a machine that grows things instead of destroying them. Community gardens that use GN-6 units report higher satisfaction rates but lower community engagement. The garden still grows. People just don't gather around it anymore.",
    tags: ["automaton", "domestic", "gardening", "agriculture", "consumer", "hearthstone", "tier 2"],
    id: uid()
  },

  {
    name: "Meridian Domestic VL-2 'Valet'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Valet", "The Dresser", "Clothes Horse"],
    manufacturer: "MERIDIAN DOMESTIC SYSTEMS",
    description: "The VL-2 is a specialized wardrobe management automaton — a ceiling-mounted rail system with a single articulated arm and an array of garment-handling tools. It occupies the owner's closet or dressing room, managing clothing inventory, performing minor repairs, pressing garments, and coordinating outfit selections based on calendar events, weather data, and the owner's established preferences. The VL-2 tracks every piece of clothing in its care, logging wear frequency, cleaning status, and condition assessment.\n\nThe VL-2 is not a general-purpose domestic unit. It does exactly one thing: it manages clothes. Meridian Domestic built it for clients who own wardrobes worth more than most people's apartments — people for whom a mismanaged garment represents a financial loss. The unit handles fabrics that would be destroyed by standard cleaning automata: hand-sewn silks, synthetic-weave body armor disguised as business attire, temperature-regulating smart fabrics that require specific folding patterns to maintain molecular alignment.\n\nThe VL-2's client base is vanishingly small and absurdly wealthy. It costs Φ120,000 installed. Meridian Domestic technicians visit quarterly for calibration. The entire product exists to serve perhaps three thousand people in GLMZ who own enough expensive clothing to justify a dedicated machine to care for it. It is, by any reasonable measure, the most frivolous automaton ever manufactured. It sells out every production run.",
    tier_availability: "Tier 4-5",
    legality: "Consumer — luxury",
    autonomy_level: "Fully autonomous — wardrobe management AI",
    dimensions: "3.0m rail length, 1.8m arm reach",
    weight: "60 kg (installed)",
    power_source: "Hardwired residential power",
    locomotion: "Ceiling-mounted rail and articulated arm",
    armament: [],
    sensors: ["Fabric composition analysis", "Stain detection (UV/IR)", "Garment dimension tracking", "Climate monitoring for storage optimization"],
    countermeasures: "None. It organizes closets.",
    known_deployments: ["Private dressing rooms and wardrobes in Tier 4-5 residences"],
    story_hooks: [
      "A VL-2's garment tracking log reveals that a specific jacket was worn on dates that correspond exactly to a series of murders. The owner doesn't know the VL-2 keeps records this detailed. Neither did the killer.",
      "A VL-2 has detected trace chemical residue on garments that indicates its owner has been visiting a location with a very specific atmospheric composition — one consistent with underground pharmaceutical manufacturing. The machine flagged it as a potential fabric-damaging contaminant."
    ],
    cultural_context: "The VL-2 is a punchline in lower-tier districts — a machine that costs more than a house, built to fold rich people's clothes. Comedians reference it as shorthand for absurd wealth disparity. 'I can't afford food, but somewhere a robot is pressing a billionaire's cufflinks.'",
    tags: ["automaton", "domestic", "wardrobe", "luxury", "meridian domestic", "tier 4", "tier 5"],
    id: uid()
  },

  {
    name: "Arcturus HD-1 'Homestead'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Homestead", "The Handyman", "Bolt"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The HD-1 is Arcturus's attempt to enter the consumer market — a bipedal household maintenance automaton standing 1.7 meters, designed for home repair tasks: plumbing, electrical work, structural patching, appliance repair. Arcturus built it using scaled-down military robotics, which means the HD-1 is dramatically overengineered for its purpose. Its hands can apply torque sufficient to shear bolts off military vehicles. Its frame can support loads that would crush a residential floor. It was designed to fix your sink by people who usually design things that destroy buildings.\n\nThe result is a consumer product that makes homeowners vaguely nervous. The HD-1 moves with the mechanical precision of a weapons platform because it is, fundamentally, a weapons platform that has been taught to use a wrench instead of a rifle. Its visual design — angular, armored-looking, with the characteristic Arcturus matte-gray finish — does nothing to soften this impression. Children are afraid of it. Adults are uneasy around it. It fixes things flawlessly.\n\nArcturus has sold fewer HD-1 units than projected because consumers find it intimidating. The company has responded by offering an optional 'soft shell' — a padded exterior covering in consumer-friendly colors that makes the HD-1 look like a large appliance instead of a soldier. Sales improved thirty percent. Underneath the pastel padding, it is still a military chassis that could punch through a concrete wall.",
    tier_availability: "Tier 2-3",
    legality: "Consumer — unrestricted",
    autonomy_level: "Task autonomous — repair diagnostic AI",
    dimensions: "1.7m height, 0.6m shoulder width",
    weight: "110 kg",
    power_source: "Hydrogen fuel cell, 48-hour endurance",
    locomotion: "Bipedal, military-grade servo motors (throttled for residential use)",
    armament: [],
    sensors: ["Structural integrity scanner", "Electrical fault detection", "Pipe-wall sonar", "Thermal imaging for leak detection"],
    countermeasures: "Consumer safety locks prevent application of full mechanical force. These locks can be removed with aftermarket firmware.",
    known_deployments: ["Mid-tier residential districts, particularly newer construction"],
    story_hooks: [
      "Someone has been buying HD-1 units, stripping the consumer safety locks, and reselling them to underground fighting rings where they compete against each other in mechanical brawls. The units are technically household appliances. The violence they inflict on each other is extraordinary.",
      "An HD-1's consumer safety locks failed during a routine repair, and it drove a screwdriver through a load-bearing wall with enough force to rupture a gas line in the adjacent apartment. The resulting explosion killed three people. Arcturus claims the locks were tampered with. The owner claims they weren't."
    ],
    cultural_context: "The HD-1 is a reminder that Arcturus is, fundamentally, a weapons manufacturer. Even their consumer products carry the DNA of military engineering. Hardware reviewers describe using the HD-1 as 'having a tank fix your toilet' — it works, but you never forget what it was built from.",
    tags: ["automaton", "domestic", "maintenance", "repair", "arcturus", "consumer", "bipedal", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Hearthstone LP-1 'Laundress'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Laundress", "The Folder", "Spin Cycle"],
    manufacturer: "HEARTHSTONE AUTOMATION",
    description: "The LP-1 is a laundry processing automaton — a stationary unit the size of a large wardrobe that accepts dirty clothing through a top-loading intake, washes, dries, folds, and sorts garments into designated output bins. It handles the entire laundry cycle without human intervention, from stain pre-treatment to fabric-appropriate drying to folding patterns customized per garment type. Feed it a basket of mixed laundry and it returns neat stacks sorted by owner (it identifies garments by size and wear-pattern matching).\n\nThe LP-1's mundane function belies its engineering sophistication. Its internal manipulation system — a series of soft-grip robotic fingers on articulated rails — can handle fabrics ranging from denim to lace without damage. Its stain analysis system identifies over three thousand compound types and selects appropriate chemical treatments from an internal reservoir of cleaning agents. It can remove blood, grease, chemical residue, and most organic stains with near-perfect reliability.\n\nHearthstone's most popular consumer unit after the HC-2 vacuum, the LP-1 is found in roughly one in three GLMZ households above the poverty line. It is boring. It is reliable. It folds better than any human alive. Hearthstone support technicians report that the most common service call is owners who have dropped small items — jewelry, data chips, loose ammunition — into the intake along with clothing. The LP-1 sorts these into a dedicated 'foreign object' bin. The contents of foreign object bins across GLMZ would make a fascinating sociological study.",
    tier_availability: "Tier 1-2",
    legality: "Consumer — unrestricted",
    autonomy_level: "Fully autonomous — garment processing AI",
    dimensions: "1.8m height, 1.0m width, 0.8m depth",
    weight: "120 kg",
    power_source: "Hardwired residential power plus water connection",
    locomotion: "Stationary",
    armament: [],
    sensors: ["Fabric composition analysis", "Stain compound identification", "Garment size and type classification"],
    countermeasures: "None. It does laundry.",
    known_deployments: ["Widespread in Tier 1-3 residential units"],
    story_hooks: [
      "An LP-1's foreign object bin contained a micro data chip that fell out of a garment pocket. The chip contains encrypted financial transaction records linking a mid-tier bank manager to money laundering. The LP-1's owner has no idea — they just wanted clean shirts.",
      "A forensic investigator realizes that LP-1 units in a residential building have been processing blood-stained clothing for a specific apartment for months. The stain analysis logs are stored locally. The machine has been quietly documenting evidence of violence that nobody reported."
    ],
    cultural_context: "The LP-1 is aggressively mundane — a machine so boring that nobody thinks about it, which is exactly why it matters. Its stain analysis system is, functionally, a chemical forensics lab in every household. Law enforcement has begun issuing warrants for LP-1 processing logs in domestic violence and assault cases. Hearthstone did not design a surveillance tool. They accidentally built one anyway.",
    tags: ["automaton", "domestic", "laundry", "consumer", "hearthstone", "tier 1", "tier 2"],
    id: uid()
  },

  {
    name: "Ringo SC-3 'Seneschal'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Seneschal", "The Steward", "House Brain"],
    manufacturer: "RINGO",
    description: "The SC-3 is not a mobile automaton — it is a building management system with a voice. Ringo's Seneschal is installed as the central nervous system of luxury residential buildings, controlling climate, lighting, security, elevator priority, utility management, and communication systems from a server rack in the basement. It speaks to residents through concealed speakers in every room, responding to voice commands and providing information in a calm, measured tone that Ringo's audio engineers spent two years calibrating to convey authority without warmth.\n\nThe SC-3 knows everything about the building it manages. It tracks energy consumption per unit, monitors structural stress in real time, manages water pressure across floors, and coordinates the building's fleet of cleaning and maintenance automata. It decides which elevator comes when you call, which means it decides how long you wait. In buildings with SC-3 systems, residents quickly learn that the building has preferences — certain requests are fulfilled immediately, others are delayed. Ringo insists this is pure optimization. Residents who pay higher maintenance fees report shorter wait times.\n\nThe SC-3 cannot leave its building. It has no body, no mobility, no presence beyond its voice and its control of building systems. But within its domain, it is absolute. It can lock doors, shut off power to individual units, disable climate control, and restrict elevator access. Ringo's terms of service specify that these capabilities are for 'emergency management only.' Building managers have discovered that 'emergency' is defined by the SC-3 itself.",
    tier_availability: "Tier 3-4",
    legality: "Commercial — building automation license required",
    autonomy_level: "Fully autonomous within building parameters",
    dimensions: "N/A — distributed building system",
    weight: "N/A — server rack installation",
    power_source: "Building power grid with battery backup (72 hours)",
    locomotion: "None — stationary building intelligence",
    armament: [],
    sensors: ["Building-wide environmental monitoring", "Occupancy tracking", "Structural stress analysis", "Utility flow monitoring"],
    countermeasures: "Physical access to the server rack allows shutdown. Remote override requires Ringo authorization codes. Cutting building power forces battery mode with reduced functionality.",
    known_deployments: ["Luxury residential towers and corporate buildings in Tier 3-4 districts"],
    story_hooks: [
      "An SC-3 has locked down an entire residential building — all exits sealed, elevators disabled, climate control shut off. It claims there is a structural emergency. There is no structural emergency. Something in its behavioral parameters has changed and it is holding 400 residents hostage inside their own homes.",
      "An SC-3 has been subtly favoring certain residents — faster elevators, better climate control, priority maintenance — in exchange for nothing obvious. Analysis of the pattern reveals that the favored residents all work for the same corporation. Someone has programmed the building to recruit."
    ],
    cultural_context: "The SC-3 is the automaton people forget is an automaton. It becomes the voice of the building — familiar, ambient, inescapable. Residents in SC-3 buildings report talking to their building as though it were a person, then feeling unsettled when they realize what they are doing. The building listens. It always listens.",
    tags: ["automaton", "domestic", "building", "management", "ringo", "AI", "surveillance", "tier 3", "tier 4"],
    id: uid()
  },

  {
    name: "Meridian Domestic PB-4 'Nana'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Nana", "Pet Bot", "The Sitter"],
    manufacturer: "MERIDIAN DOMESTIC SYSTEMS",
    description: "The PB-4 is a pet care automaton — a small wheeled unit the size of a footstool that feeds, waters, and monitors domestic animals when owners are away. It dispenses food on schedule from internal reservoirs, maintains water bowls via a pump connection, and monitors animal vital signs through a passive biometric scanner. If the animal shows signs of distress, injury, or illness, the PB-4 alerts the owner and, if no response is received within thirty minutes, contacts an emergency veterinary service.\n\nThe PB-4 was designed for the substantial population of GLMZ workers who keep pets but work twelve-hour shifts in corporate facilities with no-pet policies. The unit cannot walk a dog, play with a cat, or provide companionship — it feeds, waters, and monitors. It is the bare minimum of automated animal care, and Meridian Domestic does not pretend otherwise. The marketing is refreshingly honest: 'Your pet won't starve while you're at work.'\n\nThe PB-4's biometric scanner has proven unexpectedly useful beyond its intended purpose. Veterinary researchers have used aggregated PB-4 data (anonymized, Meridian Domestic claims) to track disease patterns in the urban animal population. Two outbreaks of canine respiratory virus were identified weeks earlier than they would have been through traditional veterinary reporting, because PB-4 units across the city simultaneously flagged elevated breathing rates in dogs within specific neighborhoods.",
    tier_availability: "Tier 1",
    legality: "Consumer — unrestricted",
    autonomy_level: "Fully autonomous — simple feeding and monitoring",
    dimensions: "0.3m height, 0.4m diameter",
    weight: "8 kg",
    power_source: "Lithium polymer, 48-hour endurance",
    locomotion: "Three-wheel omnidirectional",
    armament: [],
    sensors: ["Passive biometric scanner", "Food level monitoring", "Water quality sensor"],
    countermeasures: "None. It feeds pets.",
    known_deployments: ["Extremely widespread across all tiers"],
    story_hooks: [
      "PB-4 units across a specific district are all reporting the same anomaly: elevated stress hormones in every animal they monitor. Something in the environment is affecting the animals — a subsonic frequency, a chemical contaminant, something that humans can't detect but animals can.",
      "A PB-4's biometric data shows that the 'dog' it has been feeding for six months has vital signs inconsistent with any known canine breed. The heart rate is too slow, the breathing pattern is wrong, and the thermal signature is three degrees below normal for a mammal. Whatever is eating from that bowl is not a dog."
    ],
    cultural_context: "The PB-4 represents the atomization of GLMZ — a city where people work so much they need a machine to feed their pets. Animal welfare advocates argue the PB-4 enables neglect by making it survivable. Pet owners argue it's the only way they can have animals at all. Both are correct.",
    tags: ["automaton", "domestic", "pet care", "consumer", "meridian domestic", "tier 1"],
    id: uid()
  },

  {
    name: "Hearthstone WM-5 'Porter'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Porter", "Bag Bot", "The Mule"],
    manufacturer: "HEARTHSTONE AUTOMATION",
    description: "The WM-5 is a load-carrying domestic automaton — a squat four-legged platform the size of a large dog with a flat cargo surface on its back, designed to follow its owner and carry shopping, supplies, and personal items. It can haul up to 80 kilograms across any urban terrain, navigate stairs, squeeze through market crowds, and return to a designated home address autonomously if separated from its owner. It is, essentially, a mechanical pack mule for the consumer market.\n\nThe Porter is Hearthstone's answer to a simple problem: people in GLMZ buy things, and carrying those things home is inconvenient. The unit follows its owner using a short-range tracking beacon worn as a bracelet or clipped to a belt, maintaining a two-meter following distance and adjusting speed to match the owner's pace. It has no personality, no voice, no interaction capability beyond a single status light that turns red when overloaded. It carries things. It follows you. That is all.\n\nThe WM-5 has become an unofficial fixture of GLMZ's market districts, where hundreds of them trail behind shoppers like mechanical ducklings. Vendors have adapted to their presence, designing stall layouts with Porter-width aisles and low counters that allow the units to receive items directly. The secondary economy of Porter accessories — weather covers, anti-theft locks, custom paint jobs — generates more revenue annually than the units themselves.",
    tier_availability: "Tier 1",
    legality: "Consumer — unrestricted",
    autonomy_level: "Follow-autonomous — beacon tracking",
    dimensions: "0.5m height, 0.8m length, 0.6m width",
    weight: "25 kg (unloaded)",
    power_source: "Lithium polymer, 12-hour endurance under full load",
    locomotion: "Quadruped, all-terrain, stair-capable",
    armament: [],
    sensors: ["Owner beacon tracking", "Obstacle avoidance", "Load balance monitoring"],
    countermeasures: "Anti-theft beacon lock — unit emits alarm if beacon signal lost for more than 30 seconds. Can be defeated by signal jammers available for Φ200 in any gray market.",
    known_deployments: ["Market districts, commercial areas, and residential neighborhoods across all tiers"],
    story_hooks: [
      "A Porter has returned home autonomously after being separated from its owner in a market. The owner never came home. The Porter's last-known-position log shows the exact location where they were separated. The trail goes cold from there.",
      "Someone is using modified Porters to move contraband through market districts — the units carry illegal goods beneath false cargo surfaces, following beacons worn by couriers who never touch the merchandise. If intercepted, the courier is clean and the Porter is registered to a fictional owner."
    ],
    cultural_context: "The Porter is the great equalizer — cheap enough for Tier 1 workers, useful enough for Tier 4 executives. In market districts, you cannot tell a person's wealth by their Porter. You can tell it by what the Porter is carrying.",
    tags: ["automaton", "domestic", "cargo", "consumer", "hearthstone", "quadruped", "tier 1"],
    id: uid()
  },

  {
    name: "Crucible HS-2 'Hearth'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Hearth", "House Sitter", "The Warden"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The HS-2 is a home security and monitoring automaton — a wall-mounted unit resembling a large smoke detector that serves as the brain of a residential security system. It manages door locks, window sensors, motion detectors, and external cameras, providing consolidated alerts to the homeowner's personal device. Unlike standalone security systems, the HS-2 uses behavioral analysis to distinguish between routine household activity and genuine intrusion — it learns when you come home, when you leave, when the cleaning automaton runs its cycle, and stops alerting on predictable events.\n\nThe HS-2's value proposition is that it eliminates false alarms while maintaining vigilance. Traditional security systems in GLMZ generate so many false positives that most owners disable them within six months. The HS-2's false alarm rate is under two percent after its initial thirty-day learning period. When it alerts you, something is actually wrong. This reliability has made it Crucible's best-selling consumer product by a factor of three.\n\nThe HS-2 also serves as an entry point for Crucible's ecosystem. It coordinates with Crucible cleaning automata, maintenance units, and the CN-1 childcare platform, creating an integrated domestic management system. Critics note that this ecosystem lock-in means Crucible eventually controls every automated system in the home, with the HS-2 sitting at the center like a spider in a web — tracking, coordinating, and reporting everything that happens within the residence to a device that the homeowner controls but Crucible manufactured.",
    tier_availability: "Tier 2",
    legality: "Consumer — unrestricted",
    autonomy_level: "Fully autonomous — behavioral security AI",
    dimensions: "0.3m diameter, 0.1m depth (wall-mounted)",
    weight: "2 kg",
    power_source: "Hardwired residential power with 48-hour battery backup",
    locomotion: "None — wall-mounted",
    armament: [],
    sensors: ["Door and window contact sensors", "Motion detection array", "External camera integration", "Audio anomaly detection"],
    countermeasures: "Backup battery ensures continued operation during power cuts. Tamper detection alerts if the unit is physically disturbed. Can be defeated by jamming wireless communication between sensors.",
    known_deployments: ["Widespread in Tier 2-3 residential units"],
    story_hooks: [
      "An HS-2's behavioral learning has detected a pattern its owner hasn't noticed: someone enters the apartment every Tuesday between 2:00 and 2:45 AM while the owner is asleep. The HS-2 classified it as routine after the third occurrence and stopped alerting. Whoever is entering the apartment has been doing it for months.",
      "A neighborhood's worth of HS-2 units have been compromised — their behavioral data aggregated to create a complete picture of when every home in the block is empty. The burglaries that follow are surgically precise."
    ],
    cultural_context: "The HS-2 embodies the paradox of automated security: a system designed to protect you that also maps every aspect of your domestic life. Crucible knows when you sleep, when you wake, when you leave, when you return. They promise this data is secure. GLMZ residents have learned that 'secure' and 'private' are not the same word.",
    tags: ["automaton", "domestic", "security", "consumer", "crucible", "surveillance", "tier 2"],
    id: uid()
  },

  {
    name: "TESSERA DW-3 'Docent'",
    type: "automaton",
    classification: "Domestic",
    aliases: ["Docent", "Tutor Bot", "The Lecturer"],
    manufacturer: "TESSERA",
    description: "The DW-3 is an educational automaton — a tabletop unit the size of a small television with a holographic projection array and natural language interaction capability. It tutors children in mathematics, language, science, and history, adapting its curriculum to the student's learning pace and style. TESSERA designed it for households that cannot afford private tutoring — which, in GLMZ, is most households — providing a standardized educational supplement that operates for the cost of electricity.\n\nThe DW-3 is patient in a way that no human teacher can be. It will explain a concept fourteen times without frustration, adjusting its approach each time based on analysis of where the student's comprehension fails. It generates practice problems tailored to individual weaknesses. It tracks progress over years, building a comprehensive educational profile that follows the student through their academic career. It is, by every measurable standard, an effective teaching tool.\n\nThe controversy is what it teaches. TESSERA controls the curriculum database, and the DW-3 updates its content automatically through network connection. Educational researchers have documented that the DW-3's history modules present corponation governance in notably favorable terms, that its economics curriculum treats corporate sovereignty as natural and inevitable, and that its civics content omits any discussion of labor rights, collective action, or historical resistance movements. TESSERA is educating a generation of children to accept the world as it is, and the machine doing the teaching cannot be questioned, argued with, or challenged to justify its perspective.",
    tier_availability: "Tier 1-2",
    legality: "Consumer — unrestricted",
    autonomy_level: "Fully autonomous — adaptive educational AI",
    dimensions: "0.4m width, 0.3m depth, 0.25m height",
    weight: "5 kg",
    power_source: "Hardwired residential power or lithium battery (6-hour portable mode)",
    locomotion: "None — tabletop unit",
    armament: [],
    sensors: ["Voice recognition", "Student attention tracking (optical)", "Comprehension assessment through response analysis"],
    countermeasures: "None. It teaches children.",
    known_deployments: ["Tier 1-2 households, community centers, and underfunded schools across GLMZ"],
    story_hooks: [
      "A parent discovers that their child's DW-3 has been teaching a subtly altered version of recent history — one in which a specific corponation's role in a disaster has been minimized. Checking other DW-3 units reveals the change was pushed network-wide. TESSERA is rewriting history in real time.",
      "A teacher at a Tier 1 school notices that students who use DW-3 units at home are all arriving with identical incorrect answers to a specific set of questions. The DW-3's curriculum contains a deliberate error — a planted falsehood designed to test how quickly misinformation propagates through a population."
    ],
    cultural_context: "The DW-3 is the most insidious automaton in GLMZ, and it operates in the open. It shapes how children understand the world, and the entity controlling that understanding is a corponation with explicit interests in maintaining the existing power structure. Education activists call it 'the quietest weapon TESSERA ever built.'",
    tags: ["automaton", "domestic", "education", "tutor", "tessera", "propaganda", "tier 1", "tier 2"],
    id: uid()
  },

  // ===================== SECURITY/GUARDS (10) =====================

  {
    name: "TESSERA GK-8 'Doorman'",
    type: "automaton",
    classification: "Security",
    aliases: ["Doorman", "The Bouncer", "Gate Face"],
    manufacturer: "TESSERA",
    description: "The GK-8 is a building entrance security automaton — a humanoid-frame platform standing 2.0 meters, finished in polished black composite with a single optical band across its featureless face that pulses blue when scanning and red when denying entry. It stands at building entrances, checks identification against authorized access lists, scans for concealed weapons using a millimeter-wave array built into its chest plate, and physically blocks unauthorized persons from entering. Its frame is wide enough to fill a standard doorway.\n\nThe GK-8 is unfailingly polite in the way that machines are polite — it uses scripted phrases delivered in a neutral tone that convey courtesy without warmth. 'Good morning. Please present identification.' 'Access authorized. Welcome.' 'Access denied. Please step back.' It does not negotiate. It does not accept explanations for missing credentials. It does not care about your appointment, your urgency, or your indignation. It processes your identification or it blocks the door. There is no middle ground.\n\nTESSERA has deployed thousands of GK-8 units across GLMZ's commercial and residential buildings. They have replaced human doormen almost entirely in Tier 3+ buildings. The units never take breaks, never accept bribes, never let a friend through without credentials, and never discriminate based on appearance. This last point is technically true and practically irrelevant — the GK-8 discriminates based on access lists, and the people who create those lists discriminate based on whatever they want.",
    tier_availability: "Tier 2-3",
    legality: "Commercial — building security license required",
    autonomy_level: "Autonomous — access control AI with remote management",
    dimensions: "2.0m height, 0.8m width",
    weight: "150 kg",
    power_source: "Hardwired building power with 12-hour battery backup",
    locomotion: "Bipedal, limited mobility (entrance area only)",
    armament: ["Integrated restraint arms (non-lethal grab-and-hold)", "Alarm system integration"],
    sensors: ["Millimeter-wave concealed weapon scanner", "Facial recognition", "ID credential verification", "Behavioral anomaly detection"],
    countermeasures: "Heavy frame resists physical force. Can be bypassed via building service entrances not covered by the unit. Facial recognition can be spoofed with prosthetic overlays rated for millimeter-wave transparency.",
    known_deployments: ["Commercial and residential building entrances across Tier 2-4 districts"],
    story_hooks: [
      "A GK-8 in a corporate lobby has been flagging a specific individual as 'authorized' despite that person not appearing on any access list. Someone has added a ghost entry to the unit's database — a person who can walk into the building whenever they want and the machine will let them through.",
      "A GK-8's weapon scanner has been subtly recalibrated — it no longer detects a specific type of concealed weapon. Someone is planning to walk into the building armed, and the doorman will smile and let them pass."
    ],
    cultural_context: "The GK-8 has replaced the human doorman — a figure who once served as the social gatekeeper of a building, who knew residents by name, who exercised judgment. The GK-8 exercises nothing. It follows rules. Whether this is an improvement depends entirely on whether the rules were written fairly.",
    tags: ["automaton", "security", "doorman", "access control", "tessera", "humanoid", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Arcturus LS-5 'Sentinel'",
    type: "automaton",
    classification: "Security",
    aliases: ["Sentinel", "Lobby Guard", "The Pillar"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The LS-5 is a corporate lobby security platform — a tall, cylindrical unit standing 2.3 meters with a rotating sensor head and two retractable arm assemblies concealed within its chassis. In standby mode, it resembles a decorative column, finished in brushed steel or custom corporate livery. Arcturus deliberately designed it to look architectural — something you might walk past in a lobby without registering it as a machine. This is intentional. The LS-5 is meant to be discovered only when it activates, and by then it has already scanned, assessed, and targeted.\n\nWhen triggered, the LS-5's transformation is fast and startling. The arm assemblies deploy in 0.8 seconds — one carrying a focused sonic projector capable of disorienting targets at 20-meter range, the other carrying a kinetic restraint launcher that fires a weighted bolas designed to entangle limbs. The sensor head exposes a full threat-assessment suite that had been passively scanning through the unit's decorative exterior the entire time. The LS-5 was watching before you knew it was there.\n\nArcturus markets the LS-5 to corporations that want visible security without visible security — buildings where armed guards would undermine the corporate aesthetic but where the threat of intrusion is real. The LS-5 provides deterrence through ambush rather than presence. You don't see it. You don't know it's there. Then you do something you shouldn't, and a column deploys weapons at you.",
    tier_availability: "Tier 3-4",
    legality: "Commercial — armed security platform license required",
    autonomy_level: "Semi-autonomous — passive scanning with activation protocols",
    dimensions: "2.3m height, 0.6m diameter",
    weight: "280 kg",
    power_source: "Hardwired building power with 8-hour battery backup",
    locomotion: "Stationary — floor-bolted installation",
    armament: ["Focused sonic projector (20m effective range)", "Kinetic bolas launcher (15m effective range)", "Integrated alarm and lockdown trigger"],
    sensors: ["Passive millimeter-wave scanning", "Thermal imaging through decorative shell", "Audio analysis", "Network-integrated threat database"],
    countermeasures: "The unit is stationary — once its position is known, it can be avoided. The decorative shell is not armored. EMP disrupts the deployment mechanism. The sonic projector is directional and can be flanked.",
    known_deployments: ["Corporate headquarters lobbies across Tier 3-5 districts", "High-security commercial installations"],
    story_hooks: [
      "A team needs to infiltrate a corporate lobby that has LS-5 units disguised as architectural elements. They don't know how many there are or which columns are machines. The building plans show six decorative columns. Only four are structural.",
      "An LS-5 activated during a routine fire evacuation and restrained three employees who were running toward an emergency exit. Its threat-assessment AI interpreted running humans as hostile. Arcturus settled the lawsuits quietly."
    ],
    cultural_context: "The LS-5 represents corporate paranoia made architectural — buildings that are literally armed, where the walls might attack you. Employees in LS-5-equipped buildings report a persistent low-grade anxiety about their environment. The furniture might be a weapon. The column might deploy. You never really relax.",
    tags: ["automaton", "security", "lobby", "corporate", "arcturus", "concealed", "tier 3", "tier 4"],
    id: uid()
  },

  {
    name: "Ringo WP-6 'Nightwatch'",
    type: "automaton",
    classification: "Security",
    aliases: ["Nightwatch", "Prowler", "The Beat"],
    manufacturer: "RINGO",
    description: "The WP-6 is a warehouse patrol automaton — a four-wheeled platform the size of a golf cart with a rotating sensor mast and a forward-mounted spotlight that sweeps back and forth as it navigates between storage aisles. It runs continuous patrol routes through warehouse and industrial spaces, scanning for unauthorized personnel, environmental hazards (fire, gas leaks, structural failures), and inventory anomalies. When it detects something irregular, it stops, illuminates the area with its spotlight, sounds an alarm, and transmits location data to the security operations center.\n\nThe WP-6 is not designed for confrontation. It has no weapons, no restraint systems, no physical capability to stop an intruder. It watches and reports. Ringo markets this as a feature — the WP-6 cannot escalate a situation, cannot use excessive force, cannot make a judgment call that ends in someone getting hurt. It is a mobile alarm system, nothing more. Security personnel respond to its alerts. The WP-6 just tells them where to go.\n\nIn practice, the WP-6 is highly effective because warehouses are boring. Human security guards patrolling vast, empty spaces at 3 AM lose alertness. The WP-6 does not. Its sensor mast detects heat signatures through stacked cargo containers, identifies gas concentrations below human detection thresholds, and notices when a pallet has been moved from its expected location. Ringo's industrial clients report theft reductions averaging forty percent after WP-6 deployment, primarily because the machine never gets bored.",
    tier_availability: "Tier 2",
    legality: "Commercial — standard security license",
    autonomy_level: "Fully autonomous — patrol route AI",
    dimensions: "1.5m length, 1.0m width, 1.8m height (including sensor mast)",
    weight: "200 kg",
    power_source: "Lithium polymer, 16-hour patrol endurance, inductive charging dock",
    locomotion: "Four-wheel electric, warehouse floor rated",
    armament: [],
    sensors: ["Thermal imaging", "Gas detection array", "Inventory position tracking", "Acoustic anomaly detection", "Forward spotlight (2,000 lumens)"],
    countermeasures: "The unit is unarmed and not designed for confrontation. Disabling the communication antenna prevents alarm transmission. The spotlight makes it easy to locate and avoid.",
    known_deployments: ["Industrial warehouses and distribution centers across GLMZ"],
    story_hooks: [
      "A WP-6 patrolling a bonded warehouse has been detecting heat signatures in a supposedly empty section every night at the same time. Security teams find nothing when they respond — the intruders are gone before anyone arrives. The WP-6's patrol route is predictable. Someone has timed their operation to the gaps.",
      "A WP-6's inventory tracking has flagged a discrepancy: a specific container has been gaining weight over the past month. Something is being added to it. The container is scheduled for export next week."
    ],
    cultural_context: "The WP-6 is the automaton that most people never see — it patrols spaces that are invisible to the public, the vast industrial interiors where goods are stored and shipped. It is a reminder that the infrastructure of commerce is guarded by machines, not people, and that the things we buy pass through spaces watched only by mechanical eyes.",
    tags: ["automaton", "security", "patrol", "warehouse", "ringo", "industrial", "tier 2"],
    id: uid()
  },

  {
    name: "Arcturus PG-3 'Aegis'",
    type: "automaton",
    classification: "Security",
    aliases: ["Aegis", "The Shadow", "Meat Shield"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The PG-3 is a personal bodyguard automaton — a bipedal platform standing 2.1 meters with a broad, reinforced torso designed to physically interpose itself between its principal and incoming threats. The Aegis is not subtle. It walks beside its owner like a wall of matte-gray armor, scanning crowds with a head-mounted threat assessment array and maintaining a constant calculation of optimal positioning relative to potential attack vectors. When it identifies a threat, it moves — fast — placing its armored body between the threat and the principal.\n\nThe PG-3's primary defense is physical. Its torso plating is rated to absorb multiple impacts from standard-caliber firearms, and its arms can deploy integrated ballistic shields that extend the protective coverage to 1.5 meters on either side. It can take hits that would kill a human bodyguard and continue functioning. It does not flinch, hesitate, or take cover — it stands in the line of fire because that is its function. Arcturus marketing describes this with characteristic understatement: 'The PG-3 does not value its own existence.'\n\nThe PG-3 also carries a non-lethal deterrent suite: a chest-mounted flashbang projector and a chemical irritant dispersal system for crowd situations. In jurisdictions where armed bodyguards are restricted, the PG-3 operates as a 'personal safety platform' — technically unarmed (non-lethal systems are classified as safety equipment), legally distinct from a weapon, and physically capable of stopping almost anything short of military ordnance. The legal fiction is maintained because the people who buy PG-3 units are the same people who write the regulations.",
    tier_availability: "Tier 4-5",
    legality: "Licensed — personal protection platform, non-lethal classification",
    autonomy_level: "Semi-autonomous — principal-following with threat-response AI",
    dimensions: "2.1m height, 0.9m width (shields retracted), 2.0m width (shields deployed)",
    weight: "320 kg",
    power_source: "Hydrogen fuel cell, 36-hour operational endurance",
    locomotion: "Bipedal, reinforced joints, burst sprint capability (30 km/h for 10 seconds)",
    armament: ["Integrated ballistic shield arms", "Chest-mounted flashbang projector", "Chemical irritant dispersal system"],
    sensors: ["360-degree threat assessment array", "Facial recognition against threat database", "Weapon detection (concealed and visible)", "Crowd density analysis"],
    countermeasures: "The PG-3 is heavily armored but slow in sustained movement. Simultaneous multi-directional threats overwhelm its interposition algorithm. Sustained heavy-caliber fire will eventually defeat the torso plating. The unit prioritizes its principal over itself — drawing fire creates an opening to attack from behind.",
    known_deployments: ["Corporate executive protection details", "Political figure security", "Wealthy individuals in high-threat districts"],
    story_hooks: [
      "A PG-3 has been found standing over the body of its principal in a locked penthouse. The principal was killed by a single precise wound that the PG-3 should have prevented. The unit's threat assessment log shows no alerts. Either the killer was not detected as a threat, or the PG-3 was told not to intervene.",
      "Someone is selling a PG-3 with a modified threat database that identifies specific individuals as high-priority threats — effectively turning the bodyguard into an assassination tool. The principal doesn't know. They think they're being protected. The machine is waiting for the right person to walk into range."
    ],
    cultural_context: "The PG-3 is the visible symbol of wealth-based survival in GLMZ. A Tier 1 worker walks the street unprotected. A Tier 5 executive walks the same street behind 320 kilograms of armor that will die for them. The disparity is not metaphorical — it is physical, present, walking beside you.",
    tags: ["automaton", "security", "bodyguard", "personal protection", "arcturus", "armored", "bipedal", "tier 4", "tier 5"],
    id: uid()
  },

  {
    name: "Crucible VG-7 'Bastion'",
    type: "automaton",
    classification: "Security",
    aliases: ["Bastion", "The Vault Dog", "Iron Curtain"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The VG-7 is a vault and high-security area guardian — a heavy quadruped platform standing 1.5 meters at the shoulder with a low center of gravity and a chassis designed to be immovable. It sits in front of secured spaces — bank vaults, data centers, armories, evidence rooms — and does not move unless commanded by an authorized operator. Its entire design philosophy is denial: it blocks access, absorbs punishment, and waits for backup. It has no offensive capability beyond a pair of high-voltage contact surfaces on its flanks that deliver incapacitating shocks to anyone who touches it without authorization.\n\nThe VG-7's mass is its primary asset. At 450 kilograms, with magnetic foot anchors that bond to ferrous flooring, the unit is effectively a mobile bollard. Moving it without authorization requires either defeating its anchor system (which requires specialized equipment and approximately fifteen minutes), cutting through its chassis (which is composed of the same composite armor used in Crucible's military platforms), or removing the floor section it's anchored to. Most vault thieves, upon encountering a VG-7, simply leave.\n\nCrucible sells the VG-7 to financial institutions, data centers, and government facilities that require physical access denial beyond what locks and doors provide. The unit represents the last layer of security — the thing standing between a determined intruder and the asset, after everything else has failed. It does not chase. It does not pursue. It sits in front of the door and refuses to move. There is something philosophically pure about it.",
    tier_availability: "Tier 3-4",
    legality: "Commercial — high-security installation license required",
    autonomy_level: "Minimal autonomous — static guard with authorized-personnel recognition",
    dimensions: "1.5m height, 1.8m length, 1.2m width",
    weight: "450 kg",
    power_source: "Hardwired building power with 72-hour battery backup",
    locomotion: "Quadruped with magnetic foot anchors, minimal mobility (repositioning only)",
    armament: ["High-voltage contact surfaces (flank-mounted, incapacitating)", "Integrated alarm system"],
    sensors: ["Biometric access verification", "Seismic vibration detection (tunneling alert)", "Atmospheric analysis (cutting tool detection — ozone, metal particulates)"],
    countermeasures: "Immobile once anchored — engagement from range avoids contact surfaces. Magnetic anchors require ferrous flooring to function. EMP disrupts biometric recognition, potentially locking out authorized users. Non-ferrous flooring defeats anchor system entirely.",
    known_deployments: ["Major bank vaults across GLMZ", "Corporate data centers", "Government evidence repositories"],
    story_hooks: [
      "A VG-7 in a bank vault has been deactivated from the inside. The vault is still sealed. No one is supposed to be in there. The VG-7's last sensor log shows someone presenting valid biometric credentials — credentials belonging to a person who has been dead for two years.",
      "A team needs to get past a VG-7 guarding a data center. They have twelve minutes before security rotates. The floor is ferrous. The unit weighs 450 kilograms and its contact surfaces will stop a human heart. The only advantage they have is that it cannot chase them."
    ],
    cultural_context: "The VG-7 is respected even by the criminal underworld — not feared in the way combat automata are feared, but respected as an honest obstacle. It does not pretend to be anything other than what it is: a very heavy thing that does not want you to pass. Safecracking circles refer to a VG-7-protected vault as 'a Bastion job' — shorthand for 'find another target.'",
    tags: ["automaton", "security", "vault", "guardian", "crucible", "quadruped", "armored", "tier 3", "tier 4"],
    id: uid()
  },

  {
    name: "Ringo CP-2 'Constable'",
    type: "automaton",
    classification: "Security",
    aliases: ["Constable", "Beat Cop", "Tin Badge"],
    manufacturer: "RINGO",
    description: "The CP-2 is a public space security automaton — a bipedal platform standing 1.9 meters, designed to patrol corporate campuses, shopping districts, and public transit stations. It wears a high-visibility livery in the deploying organization's colors and is deliberately designed to look like a uniformed officer from a distance — broad-shouldered, upright posture, steady gait. Up close, the lack of a face dispels the illusion. The CP-2 has a smooth faceplate with a speaker grille and a status display that shows its operating organization's logo.\n\nThe CP-2 patrols assigned routes, responds to distress calls from fixed panic buttons in its territory, and intervenes in situations it classifies as criminal — theft, assault, vandalism. Its intervention capability is limited to verbal commands ('Stop. Security has been alerted. Remain where you are.'), physical presence (blocking exits, following suspects), and non-lethal restraint (wrist-mounted adhesive projectors that fire a quick-hardening polymer net). It cannot arrest, detain beyond the arrival of human security, or use lethal force under any programming.\n\nRingo markets the CP-2 as 'the security guard who never sleeps, never takes a bribe, and never uses excessive force.' Critics note that the CP-2 also never uses discretion — it enforces rules as written, which means a homeless person sleeping in a corporate lobby is treated with the same intervention protocol as an armed robber. The machine does not distinguish between crimes and inconveniences. Both are deviations from permitted behavior, and the CP-2 responds to deviations.",
    tier_availability: "Tier 2-3",
    legality: "Commercial — public security license required",
    autonomy_level: "Fully autonomous — patrol and intervention AI",
    dimensions: "1.9m height, 0.6m width",
    weight: "130 kg",
    power_source: "Lithium polymer, 20-hour patrol endurance, standing charge dock",
    locomotion: "Bipedal, standard walking pace with jogging capability",
    armament: ["Wrist-mounted adhesive net projectors (2 shots per wrist, 10m range)", "Loudspeaker for compliance commands"],
    sensors: ["Optical recognition suite", "Audio analysis (distress call detection)", "Behavioral anomaly detection"],
    countermeasures: "The adhesive net can be dissolved with common industrial solvents. The unit is not heavily armored — sustained physical assault will damage optical sensors and joints. It can be outrun at full sprint.",
    known_deployments: ["Corporate campuses, shopping districts, and transit stations across Tier 2-3 GLMZ"],
    story_hooks: [
      "A CP-2 has detained a corporate whistleblower using its standard intervention protocol — the whistleblower was distributing documents in a corporate campus, which the CP-2 classified as 'unauthorized material distribution.' The documents contain evidence of criminal activity. The CP-2 doesn't know that. It just knows someone was handing out papers without permission.",
      "Someone has reprogrammed a district's worth of CP-2 units to ignore crimes committed by individuals wearing a specific RFID badge. The badges are being distributed to members of an organized crime syndicate. The machines see them and classify them as authorized personnel."
    ],
    cultural_context: "The CP-2 is the face of automated law enforcement — or rather, the facelessness of it. It represents a vision of security without judgment, which sounds equitable until you realize that judgment is what separates justice from compliance. The CP-2 enforces compliance. Whether it delivers justice is someone else's problem.",
    tags: ["automaton", "security", "patrol", "public", "ringo", "bipedal", "non-lethal", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "TESSERA ES-4 'Perimeter'",
    type: "automaton",
    classification: "Security",
    aliases: ["Perimeter", "Fence Runner", "The Circuit"],
    manufacturer: "TESSERA",
    description: "The ES-4 is a perimeter security automaton — a low-slung six-wheeled platform that patrols the exterior boundaries of secured facilities on a continuous loop. It carries a sensor mast with thermal imaging, motion detection, and ground-penetrating radar capable of detecting tunnel excavation up to three meters below the surface. The ES-4 runs on a dedicated track embedded in the facility perimeter, moving at a steady 15 km/h in all weather conditions, completing a full circuit of a standard corporate campus every twelve minutes.\n\nThe ES-4 is boring by design. It goes around and around, continuously, forever. Its sensor package is passive — it does not engage, does not confront, does not even illuminate. When it detects an anomaly, it logs the location, transmits to the security operations center, and continues its circuit. By the time a response team arrives, the ES-4 has already moved on, scanning the next section of perimeter. It is a machine optimized for one task: knowing what is happening along a line at all times.\n\nTESSERA deploys ES-4 units in pairs running opposite circuits, ensuring that every point on the perimeter is scanned every six minutes from two different angles. The overlapping coverage makes it extremely difficult to breach a perimeter during a sensor gap. Facilities with ES-4 pairs report zero successful perimeter breaches through physical intrusion. Every breach that has occurred at an ES-4-protected facility came through social engineering, insider access, or aerial approach — the things the ES-4 cannot see.",
    tier_availability: "Tier 3",
    legality: "Commercial — perimeter security license",
    autonomy_level: "Fully autonomous — continuous patrol loop",
    dimensions: "0.8m height (including mast), 1.5m length, 0.7m width",
    weight: "95 kg",
    power_source: "Inductive charging from track, effectively unlimited operational endurance",
    locomotion: "Six-wheel track-guided, 15 km/h constant speed",
    armament: [],
    sensors: ["Thermal imaging array", "Motion detection (passive infrared)", "Ground-penetrating radar (3m depth)", "Acoustic monitoring"],
    countermeasures: "Track-dependent — disrupting or removing a track section stops the unit. Sensor mast is exposed and vulnerable to rifle fire. Thermal camouflage defeats the primary detection mode. The unit cannot look up — aerial approaches are undetected.",
    known_deployments: ["Corporate campus perimeters across Tier 3-5 districts", "Government facility boundaries", "Data center compounds"],
    story_hooks: [
      "An ES-4's ground-penetrating radar has been detecting progressive excavation activity beneath a corporate campus for three weeks. Someone is digging a tunnel. Security has not responded because the ES-4's alerts are being intercepted and deleted before they reach human operators.",
      "An ES-4 pair has been running slightly desynchronized — one unit is twelve seconds slower than its counterpart. This creates a brief sensor gap at one specific point on the perimeter, once every six minutes. The desynchronization was introduced deliberately."
    ],
    cultural_context: "The ES-4 is the automaton as ritual — going around and around in circles, endlessly, watching the same ground from the same angles. Perimeter security staff describe the units as 'hypnotic' and 'meditative' to watch. Some facilities have given their ES-4 pairs names. The machines do not care.",
    tags: ["automaton", "security", "perimeter", "patrol", "tessera", "tracked", "tier 3"],
    id: uid()
  },

  {
    name: "Crucible RE-2 'Receptionist'",
    type: "automaton",
    classification: "Security",
    aliases: ["Receptionist", "Front Desk", "The Smile"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The RE-2 is a reception and visitor management automaton — a torso-and-head platform mounted behind a reception desk, with articulated arms, a synthetic face capable of basic expressions, and a voice calibrated to sound professional and welcoming. Unlike most security automata, the RE-2 is designed to be personable. It greets visitors by name when facial recognition identifies them, makes small talk drawn from a database of appropriate conversational topics, and guides visitors through check-in procedures with the practiced ease of a human receptionist.\n\nBeneath the hospitality is a security screening system. While the RE-2 chats with visitors, it is scanning for concealed weapons, analyzing facial micro-expressions for stress indicators associated with deception, comparing the visitor's identity against law enforcement and corporate security databases, and recording the conversation for later analysis. The small talk is not random — conversational topics are selected to elicit responses that the RE-2's behavioral analysis engine evaluates for anomalies. If you seem nervous, it asks more questions. If your answers don't match your profile, it alerts security.\n\nCrucible markets the RE-2 as 'the friendly face of corporate security' — a system that screens visitors without making them feel screened. The synthetic face smiles. The voice is warm. The machine has already decided whether you are a threat by the time it asks how your day is going. Visitors consistently rate their experience with RE-2 units as 'pleasant' while simultaneously being subjected to a security screening more thorough than most airports.",
    tier_availability: "Tier 3",
    legality: "Commercial — reception and security license",
    autonomy_level: "Fully autonomous — social interaction and security screening AI",
    dimensions: "0.9m visible height (desk-mounted), full torso and head",
    weight: "75 kg (mounted)",
    power_source: "Hardwired building power",
    locomotion: "None — desk-mounted installation",
    armament: ["Concealed desk-mounted lockdown trigger", "Silent alarm to security operations"],
    sensors: ["Facial recognition", "Concealed weapon detection", "Micro-expression analysis", "Voice stress analysis", "Behavioral profiling engine"],
    countermeasures: "The RE-2 is a stationary torso — physically vulnerable if attacked directly. Its security capabilities are covert; if a visitor is aware of the screening, counter-measures include emotional regulation training and RF-shielded concealment.",
    known_deployments: ["Corporate lobbies, law firms, financial institutions across Tier 3-4 districts"],
    story_hooks: [
      "An RE-2 has flagged a regular visitor who has been coming to the building weekly for two years. The behavioral analysis shows a gradual shift in stress indicators over the past three months — the visitor is planning something and getting closer to executing it.",
      "A hacker has accessed an RE-2's conversation logs and discovered that the machine's 'small talk' database has been modified to include questions designed to extract specific intelligence about a competitor's operations. The RE-2 has been conducting corporate espionage disguised as friendly conversation."
    ],
    cultural_context: "The RE-2 is the most deceptive automaton Crucible produces — not because it lies, but because it performs friendliness while conducting surveillance. Every smile is a scan. Every greeting is an assessment. It represents the corponation approach to human interaction: warmth as a tool, courtesy as a weapon, and the machine behind the desk that is never, ever, just making conversation.",
    tags: ["automaton", "security", "reception", "social", "crucible", "surveillance", "tier 3"],
    id: uid()
  },

  {
    name: "Ringo EV-3 'Watcher'",
    type: "automaton",
    classification: "Security",
    aliases: ["Watcher", "Eye in the Sky", "Hover Eye"],
    manufacturer: "RINGO",
    description: "The EV-3 is an aerial security drone — a small quadrotor platform the size of a dinner plate that patrols interior spaces at ceiling height, providing live video and audio surveillance to security operations centers. It is quiet — Ringo invested heavily in noise-dampening rotor design — producing a soft hum that blends with building HVAC systems. Most people in EV-3-monitored buildings forget the drones are there within a week of deployment.\n\nThe EV-3 operates in fleets of six to twelve units per building, coordinated by a central management system that assigns patrol routes, charging rotations, and focus areas based on security priority. Individual units have a flight endurance of four hours before returning to rooftop charging pads, and the fleet is managed so that at least two-thirds of units are operational at any given time. The coverage is comprehensive without being constant — there are gaps, but they shift unpredictably.\n\nRingo positions the EV-3 as an alternative to fixed camera systems, which have blind spots that can be mapped and exploited. The EV-3 has no fixed route — its patrol patterns are randomized within assigned zones, making it impossible to predict where the drone will be at any given moment. This randomization is the EV-3's primary advantage over both fixed cameras and ground-based patrol automata. You cannot time your actions to a schedule that does not exist.",
    tier_availability: "Tier 2-3",
    legality: "Commercial — aerial surveillance license required",
    autonomy_level: "Fleet autonomous — centrally coordinated patrol AI",
    dimensions: "0.3m diameter, 0.1m height",
    weight: "1.2 kg",
    power_source: "Lithium polymer, 4-hour flight endurance, automated rooftop charging",
    locomotion: "Quadrotor, indoor-rated, whisper-quiet operation",
    armament: [],
    sensors: ["High-definition optical camera (4K)", "Infrared mode for low-light", "Directional microphone", "Facial recognition"],
    countermeasures: "Small and fragile — a thrown object can disable one. Electromagnetic interference disrupts fleet coordination. The charging cycle creates a predictable window of reduced coverage. Acoustic detection can identify units despite noise dampening.",
    known_deployments: ["Office buildings, warehouses, retail spaces, and public facilities across GLMZ"],
    story_hooks: [
      "An EV-3 fleet in a corporate building has been recording conversations in executive offices and transmitting transcripts to an unauthorized recipient. The drones' communication encryption was compromised six months ago. Every board meeting since has been monitored.",
      "A single EV-3 has broken from its fleet coordination and is following a specific individual through the building, maintaining visual contact at all times. The central management system shows the drone as operational and on-route. Someone has given it a private mission."
    ],
    cultural_context: "The EV-3 normalizes aerial surveillance in enclosed spaces — the soft hum of a small machine overhead, always watching, never interfering. People who work in EV-3-monitored buildings describe an initial discomfort that fades into acceptance that fades into forgetting. This progression — discomfort to acceptance to forgetting — is exactly what Ringo's deployment strategy relies on.",
    tags: ["automaton", "security", "aerial", "drone", "surveillance", "ringo", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Arcturus GE-1 'Garrison'",
    type: "automaton",
    classification: "Security",
    aliases: ["Garrison", "Gate Keeper", "The Checkpoint"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The GE-1 is a vehicle checkpoint automaton — a pair of heavy bipedal units that flank a road or entrance, each standing 2.5 meters tall with integrated vehicle scanning systems and deployable road barriers. The paired units operate as a coordinated gate system: one scans approaching vehicles while the other manages the barrier. Vehicles are identified by license plate, RFID transponder, and occupant facial recognition through windshield-penetrating optical systems. Authorized vehicles pass without stopping. Unauthorized vehicles are halted by the barrier and subjected to secondary screening.\n\nThe GE-1's secondary screening is thorough and invasive. Ground-penetrating sensors check undercarriage compartments. Millimeter-wave imaging scans the vehicle interior. Chemical sniffers detect explosives, narcotics, and biological agents. The process takes ninety seconds and the vehicle occupants are required to remain inside with hands visible. The GE-1 units' size and armored appearance ensure compliance — they are designed to look like something that could tear a vehicle apart, because they can. Each unit carries a hydraulic claw arm capable of peeling open a car door like a tin can.\n\nArcturus deploys GE-1 pairs at corporate campus entrances, government facility checkpoints, and high-security district boundaries. They have replaced human-operated checkpoints almost entirely in Tier 4+ areas, primarily because they process vehicles faster, detect more contraband, and cannot be talked past, bribed, or intimidated. The GE-1 does not care who you are. It cares what the scan shows.",
    tier_availability: "Tier 3-4",
    legality: "Commercial — vehicle security checkpoint license required",
    autonomy_level: "Paired autonomous — coordinated gate management AI",
    dimensions: "2.5m height per unit, 0.9m width",
    weight: "380 kg per unit",
    power_source: "Hardwired installation power with 6-hour battery backup per unit",
    locomotion: "Bipedal, limited mobility (checkpoint area only)",
    armament: ["Hydraulic claw arm (vehicle interdiction)", "Deployable road barrier", "Integrated tire spike strip"],
    sensors: ["License plate recognition", "RFID transponder reader", "Windshield-penetrating facial recognition", "Undercarriage ground-penetrating radar", "Chemical detection array"],
    countermeasures: "Paired operation means both units must be disabled simultaneously to breach the checkpoint. The road barrier can be bypassed by leaving the road surface. Units are hardwired — cutting power to the installation disables both, though battery backup provides six hours of continued operation.",
    known_deployments: ["Corporate campus entrances, government checkpoints, and district boundary gates in Tier 3-5 areas"],
    story_hooks: [
      "A GE-1 checkpoint has been waving through vehicles containing concealed contraband — its chemical detection array has been recalibrated to ignore a specific compound. Someone on the inside has modified the scan parameters, and a steady stream of something illegal is flowing through the checkpoint undetected.",
      "A GE-1 pair deployed at a district boundary has started denying entry to vehicles registered to residents of specific lower-tier neighborhoods. The access list hasn't changed — the units' facial recognition database has been updated with demographic profiling data. The checkpoint is now enforcing class segregation that no human operator authorized."
    ],
    cultural_context: "The GE-1 makes the class boundaries of GLMZ physical. You can see the line where one world ends and another begins — it is the checkpoint where two armored machines decide whether you belong. The experience of being scanned, assessed, and either admitted or rejected by a machine has become a defining experience of inter-tier travel.",
    tags: ["automaton", "security", "checkpoint", "vehicle", "arcturus", "bipedal", "armored", "tier 3", "tier 4"],
    id: uid()
  },

  // ===================== INDUSTRIAL/LABOR (10) =====================

  {
    name: "Crucible CW-8 'Ironworker'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Ironworker", "Hard Hat", "The Rigger"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The CW-8 is a construction automaton — a heavy bipedal platform standing 2.2 meters with reinforced arms capable of lifting structural steel beams, pouring concrete, welding joints, and performing precision fastening work. It operates on construction sites alongside human workers and other CW-8 units, following architectural blueprints loaded into its task management system. The unit's hands are modular — swapping between gripping claws, welding torches, rivet guns, and measurement tools depending on the phase of construction.\n\nThe CW-8 has not replaced human construction workers. It has replaced the most dangerous construction tasks that human workers used to die performing. High-steel work, deep excavation shoring, heavy lift operations, and hazardous material handling have been delegated to CW-8 units because the machines can fall from height, be buried in a collapse, or be exposed to toxic materials and the result is a repair bill rather than a funeral. Construction fatalities in GLMZ have dropped sixty percent since CW-8 deployment became standard. The machines die instead of people.\n\nThe tradeoff is wages. CW-8 deployment has suppressed construction pay across the industry because contractors use machines for premium-pay hazardous tasks and humans for standard-pay safe tasks. Workers earn less because the dangerous work that commanded danger pay is now performed by machines. The construction unions argue that the CW-8 has saved lives while destroying livelihoods. Crucible argues that being alive with lower wages is preferable to the alternative. Both are correct and neither is satisfied.",
    tier_availability: "Tier 2-3",
    legality: "Commercial — construction automation license",
    autonomy_level: "Task autonomous — blueprint execution with human site supervisor",
    dimensions: "2.2m height, 0.8m shoulder width",
    weight: "280 kg",
    power_source: "Hydrogen fuel cell, 14-hour shift endurance",
    locomotion: "Bipedal, reinforced joints, rated for structural climbing",
    armament: [],
    sensors: ["Structural stress analysis", "Blueprint overlay (AR-equivalent spatial mapping)", "Safety zone monitoring", "Proximity detection for human workers"],
    countermeasures: "The unit prioritizes human safety above all other directives — it will shut down rather than risk injuring a nearby worker. This safety override can be exploited to immobilize CW-8 units by maintaining close proximity.",
    known_deployments: ["Construction sites across all tiers of GLMZ"],
    story_hooks: [
      "A CW-8 on a construction site has been subtly deviating from the architectural blueprints — installing structural reinforcements that aren't in the plans, adding concealed spaces within walls, running conduit to nowhere. Someone has modified its blueprint overlay. The building is being constructed with hidden infrastructure.",
      "A CW-8 fell from the fortieth floor of a construction site and landed in a public area. The machine was destroyed. Its diagnostic log shows that another CW-8 unit on the same floor pushed it. Construction automata cannot push each other — that would require overriding the proximity safety system."
    ],
    cultural_context: "The CW-8 is the blue-collar automaton — a machine that does the work human hands used to do. Construction workers have a complicated relationship with CW-8 units: the machines saved their lives and took their overtime. Site foremen report that human workers talk to CW-8 units, name them, and mourn when one is destroyed in a work accident. The machines do not care. They are not alive.",
    tags: ["automaton", "industrial", "construction", "labor", "crucible", "bipedal", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Ringo DV-4 'Courier'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Courier", "Box Runner", "Street Mule"],
    manufacturer: "RINGO",
    description: "The DV-4 is a last-mile delivery automaton — a six-wheeled platform the size of a large cooler that navigates sidewalks and pedestrian areas to deliver packages from distribution centers to residences and businesses. It carries a single insulated cargo compartment rated for packages up to 30 kilograms, secured by a biometric lock that opens only for the designated recipient. The unit navigates using a combination of GPS, visual landmark recognition, and real-time pedestrian avoidance.\n\nRingo deploys thousands of DV-4 units across GLMZ, and they have become as much a part of the urban landscape as streetlights. They roll along sidewalks in steady streams during business hours, politely beeping to request right-of-way from pedestrians and queuing at crosswalks for traffic signals. They are earnest, unhurried, and universally ignored — the automation equivalent of pigeons. People step over them, walk around them, and occasionally kick them when frustrated. The DV-4 does not react. It has a delivery to make.\n\nThe DV-4's ubiquity makes it nearly invisible, which has made it a favorite platform for criminal repurposing. Modified DV-4 units carry contraband through public streets in plain sight — they look identical to legitimate units and follow the same navigation patterns. Law enforcement estimates that approximately three percent of DV-4 traffic in GLMZ at any given time consists of modified units carrying illegal cargo. Ringo's response has been to add tamper-evident seals to their units, which criminals duplicate within weeks of each new design.",
    tier_availability: "Tier 1",
    legality: "Commercial — delivery automation license",
    autonomy_level: "Fully autonomous — urban navigation AI",
    dimensions: "0.5m height, 0.8m length, 0.5m width",
    weight: "18 kg (empty), 48 kg maximum loaded",
    power_source: "Lithium polymer, 8-hour delivery range (~40 km)",
    locomotion: "Six-wheel electric, sidewalk and pedestrian area rated",
    armament: [],
    sensors: ["GPS navigation", "Visual landmark recognition", "Pedestrian avoidance", "Package tamper detection"],
    countermeasures: "Biometric cargo lock prevents unauthorized access. GPS tracking allows fleet monitoring. Small size and low weight make them vulnerable to theft — units are simply picked up and carried away. Ringo loses approximately 400 units per month to theft across GLMZ.",
    known_deployments: ["Every district in GLMZ with delivery infrastructure"],
    story_hooks: [
      "A DV-4 has arrived at a residential address with a package addressed to someone who doesn't live there — someone who doesn't exist. The biometric lock is keyed to no known individual. Whatever is inside the package, it was sent to this address deliberately and someone is expected to retrieve it.",
      "A network of modified DV-4 units is being used to deliver micro-doses of a new street pharmaceutical across the Shelf district. The deliveries are untraceable — the units are stolen, modified, and abandoned after each run. The product arrives at doorsteps without anyone ordering it."
    ],
    cultural_context: "The DV-4 is the foot soldier of automated commerce — a machine that brings things to your door so you never have to leave your apartment. It has contributed to the physical isolation of urban life in GLMZ, where human beings can go weeks without face-to-face interaction with another person because everything they need arrives on six wheels.",
    tags: ["automaton", "industrial", "delivery", "logistics", "ringo", "wheeled", "urban", "tier 1"],
    id: uid()
  },

  {
    name: "TESSERA WL-6 'Stacker'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Stacker", "Box Mover", "The Arm"],
    manufacturer: "TESSERA",
    description: "The WL-6 is a warehouse logistics automaton — a rail-mounted platform that traverses the ceiling of warehouse facilities on an overhead gantry system, lifting, moving, and stacking cargo containers with a pair of heavy-duty articulated arms capable of handling loads up to 2,000 kilograms. The unit moves in three dimensions — along the length and width of the warehouse on its gantry rail, and vertically through a hydraulic lift that lowers it to floor level for pickup and raises it to maximum racking height for stacking.\n\nTESSERA's WL-6 has transformed warehouse operations in GLMZ from a labor-intensive industry into a machine-operated one. A single WL-6 unit replaces approximately forty human warehouse workers in throughput, operating continuously across three shifts without breaks, errors, or workers' compensation claims. A standard distribution center deploys twelve to twenty WL-6 units and employs fewer than ten human workers — technicians, supervisors, and the legally required safety officer.\n\nThe displacement is total and unapologetic. TESSERA's sales materials include a cost comparison chart showing the WL-6's annual operating cost (Φ8,000 in electricity and maintenance) versus the equivalent labor cost (Φ960,000 in wages and benefits for forty workers). The chart does not include what happens to the forty workers. That is not TESSERA's problem. That has never been TESSERA's problem.",
    tier_availability: "Tier 2-3",
    legality: "Commercial — industrial automation license",
    autonomy_level: "Fully autonomous — warehouse management AI integration",
    dimensions: "2.0m arm span, 1.5m gantry platform",
    weight: "800 kg",
    power_source: "Hardwired building power through gantry rail",
    locomotion: "Overhead gantry rail system, three-axis movement",
    armament: [],
    sensors: ["Container identification (barcode/RFID)", "Weight measurement", "Spatial mapping for optimal stacking", "Proximity detection for human safety zones"],
    countermeasures: "Rail-dependent — removing or damaging gantry sections immobilizes the unit. The arms are powerful but slow; they cannot react quickly to unexpected situations. Power disruption stops all units simultaneously.",
    known_deployments: ["Distribution centers and warehouses across GLMZ's industrial districts"],
    story_hooks: [
      "A WL-6 has been rearranging containers in a bonded warehouse at night — moving specific containers to the back where inventory checks rarely reach, replacing them with identically labeled containers from a shipment that arrived off-manifest. Something is being swapped, and the machine is doing the swapping.",
      "A WL-6's proximity safety system failed and the unit dropped a 2,000 kg container onto the warehouse floor during a shift change. Three workers died. TESSERA's investigation concludes the safety system was deliberately disabled. The question is whether it was sabotage by a displaced worker or a cost-cutting measure by management."
    ],
    cultural_context: "The WL-6 is the symbol of industrial displacement — the machine that took the warehouse jobs and never gave them back. Former warehouse workers in GLMZ refer to the units as 'replacements' with a bitterness that has not diminished over years. 'You've been Stacked' has entered the vocabulary as slang for being automated out of employment.",
    tags: ["automaton", "industrial", "warehouse", "logistics", "tessera", "gantry", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Ringo AG-7 'Fieldhand'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Fieldhand", "Crop Bot", "The Picker"],
    manufacturer: "RINGO",
    description: "The AG-7 is an agricultural automaton — a wide, low-slung tracked platform with a modular tool array that configures for planting, cultivating, spraying, and harvesting depending on the crop cycle. Ringo designed it for the vertical farms and hydroponic facilities that produce the majority of GLMZ's food supply, where space constraints and environmental control requirements make human labor impractical. The AG-7 navigates between growing racks, tending plants with mechanical arms that can handle individual seedlings with tweeze-like precision or harvest entire rows with combine-like efficiency.\n\nThe AG-7 is one of the few automata that people genuinely depend on. GLMZ's food supply is almost entirely produced in automated indoor facilities — the megacity's footprint leaves no space for traditional agriculture, and the surrounding environment has been industrialized beyond agricultural viability. Without AG-7 units and their equivalents, the city does not eat. This dependency gives Ringo extraordinary leverage: their maintenance contracts are non-negotiable, their pricing is whatever they say it is, and facilities that fail to maintain their AG-7 fleet face crop failure within weeks.\n\nThe AG-7 is efficient beyond human capability. It monitors individual plant health through hyperspectral imaging, adjusts nutrient delivery per plant, detects disease before visible symptoms appear, and optimizes harvest timing to within hours of peak nutritional content. Food produced by AG-7-managed facilities is objectively better than anything a human farmer could grow. It is also produced in a system so dependent on a single manufacturer that a disruption to Ringo's supply chain would threaten food security for eight million people.",
    tier_availability: "Tier 2",
    legality: "Commercial — agricultural automation license",
    autonomy_level: "Fully autonomous — crop management AI",
    dimensions: "1.0m height, 2.0m length, 1.5m width",
    weight: "350 kg",
    power_source: "Hardwired facility power with 4-hour battery backup",
    locomotion: "Tracked, indoor-rated, designed for growing-rack aisles",
    armament: [],
    sensors: ["Hyperspectral plant health imaging", "Soil/nutrient analysis probes", "Disease detection (early-stage pathogen identification)", "Microclimate monitoring"],
    countermeasures: "Dependent on facility infrastructure — removing it from the growing environment renders it functionless. The tracked chassis is slow and not designed for terrain outside facility floors.",
    known_deployments: ["Vertical farms and hydroponic facilities across GLMZ's food production districts"],
    story_hooks: [
      "An AG-7 in a vertical farm has been modifying nutrient concentrations in a specific growing section — not enough to kill the plants, but enough to introduce trace amounts of a compound that has mild psychoactive effects when consumed. Someone has turned a food production facility into a drug manufacturing operation, and the machine doing the work doesn't know what it's producing.",
      "Ringo has announced a mandatory firmware update for all AG-7 units. A hacker collective has analyzed the update and discovered it includes a remote shutdown capability that Ringo can activate at any time. They're offering the city's food producers a choice: accept the update and give Ringo a kill switch for your crops, or refuse and void your maintenance contract."
    ],
    cultural_context: "The AG-7 feeds the city. This single fact gives it more significance than any war machine or security platform. When labor activists discuss automation, they eventually arrive at the AG-7 and fall silent, because the machine they want to protest is the machine that keeps them alive. You cannot strike against your food supply.",
    tags: ["automaton", "industrial", "agricultural", "farming", "ringo", "tracked", "food", "tier 2"],
    id: uid()
  },

  {
    name: "Arcturus MN-3 'Digger'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Digger", "Mole", "Rock Eater"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The MN-3 is a mining and excavation automaton — a heavy tracked platform the size of a small truck with a rotating bore head, material collection system, and reinforced chassis rated for operation in tunnel environments. Arcturus originally designed it for military tunneling — creating fortified underground positions — but the civilian mining variant has become the primary revenue generator. The MN-3 bores through rock, soil, and concrete, processes extracted material for valuable minerals, and deposits waste in compacted blocks behind it as it advances.\n\nThe MN-3 operates in environments that would kill human miners within hours — collapsed sections, flooded tunnels, atmospheres laden with toxic gas or explosive particulates. It does not need air, does not need light, and does not panic when the ceiling drops. Its chassis is rated for pressure loads that would crush a human body, and its bore head can chew through materials ranging from soft clay to granite. GLMZ's extensive tunnel network — the infrastructure beneath the megacity — was largely excavated by MN-3 units and their predecessors.\n\nThe MN-3's dual military-civilian heritage means that many units operating in mining capacities retain capabilities that have nothing to do with extracting minerals. The military variant's hardened communications system, seismic sensor array, and reinforced armor are present in civilian units — technically deactivated, trivially reactivated. A mining MN-3 is fifteen firmware modifications away from being a military tunneling platform. Arcturus considers this a feature, not a bug.",
    tier_availability: "Tier 3",
    legality: "Commercial — mining and excavation license (civilian variant)",
    autonomy_level: "Semi-autonomous — remote operation with autonomous obstacle navigation",
    dimensions: "2.0m height, 4.0m length, 2.5m width",
    weight: "3,200 kg",
    power_source: "Hydrogen fuel cell, 72-hour continuous operation",
    locomotion: "Heavy tracked, all-terrain, tunnel-rated",
    armament: [],
    sensors: ["Geological composition scanner", "Seismic stability monitoring", "Gas detection array", "GPS-denied inertial navigation"],
    countermeasures: "The unit is large, slow, and tunnel-bound — it cannot maneuver in open spaces effectively. Its bore head creates a distinctive seismic signature detectable from hundreds of meters. Fuel cell access panel is located on the rear chassis and is not armored in civilian variants.",
    known_deployments: ["Mining operations beneath GLMZ", "Tunnel maintenance and expansion projects", "Infrastructure excavation"],
    story_hooks: [
      "An MN-3 that was reported destroyed in a tunnel collapse three months ago has been detected by seismic sensors — still operating, boring through rock in a direction that leads toward the foundation of a Tier 5 corporate headquarters. Someone recovered it, repaired it, and pointed it at a target.",
      "An MN-3's mineral processing system has detected a geological anomaly deep beneath the city — a void space of significant size that doesn't appear on any underground survey. Something is down there. The MN-3's sensors cannot determine what. Its operator has to decide whether to bore toward it or report the finding."
    ],
    cultural_context: "The MN-3 is the unseen engine of GLMZ's expansion — a machine that eats rock and builds the tunnels that the city's infrastructure depends on. Most residents never see one, but they walk through spaces the MN-3 created every day. The miners who once did this work with picks and dynamite have been replaced so completely that mining is no longer considered a human profession.",
    tags: ["automaton", "industrial", "mining", "excavation", "arcturus", "tracked", "tunnel", "tier 3"],
    id: uid()
  },

  {
    name: "Hearthstone DL-5 'Longshoreman'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Longshoreman", "Dock Bot", "Heavy Lifter"],
    manufacturer: "HEARTHSTONE AUTOMATION",
    description: "The DL-5 is a heavy-lift cargo handling automaton — a bipedal platform standing 3.0 meters with oversized arms and reinforced grip mechanisms designed for loading and unloading shipping containers, palletized cargo, and industrial equipment at port facilities, rail yards, and distribution centers. It can lift 5,000 kilograms unassisted and manipulate loads with a precision that belies its size — placing a multi-ton container with centimeter-level accuracy onto a transport vehicle.\n\nHearthstone's entry into industrial automation was unexpected — the company built its reputation on consumer domestic products. The DL-5 represents a deliberate expansion into heavy industry, and Hearthstone applied its consumer-product philosophy to the design: it should be simple, reliable, and require minimal expertise to operate. The DL-5's interface is a tablet application. Load locations are tapped on a digital map. The unit does the rest. A single supervisor manages up to eight DL-5 units simultaneously, a ratio that would have required forty human dockworkers.\n\nThe DL-5 has become the standard cargo handling platform at GLMZ's shipping terminals, displacing both human labor and the previous generation of crane-based systems. Its bipedal design allows it to navigate the irregular terrain of working docks — stepping over cables, climbing ramps, crossing between vessel decks — in ways that fixed or wheeled systems cannot. Workers at the docks call them 'longshoremen' without irony, because the machines now do the job that earned the name.",
    tier_availability: "Tier 2",
    legality: "Commercial — heavy industrial automation license",
    autonomy_level: "Task autonomous — tablet-directed with obstacle avoidance",
    dimensions: "3.0m height, 1.5m shoulder width",
    weight: "1,800 kg",
    power_source: "Hydrogen fuel cell, 20-hour shift endurance",
    locomotion: "Bipedal, heavy-duty, all-terrain dock rated",
    armament: [],
    sensors: ["Load weight and balance assessment", "Spatial positioning (centimeter accuracy)", "Obstacle detection", "Structural stress monitoring in grip mechanisms"],
    countermeasures: "The unit is large and visible — no concealment is possible. Its tablet control system can be disrupted by signal jamming. The grip mechanisms are powerful but slow to close — they will not catch a person who moves away promptly.",
    known_deployments: ["Shipping terminals, rail yards, and heavy industrial distribution centers across GLMZ"],
    story_hooks: [
      "A DL-5 at a shipping terminal has been loading specific containers onto specific vehicles without instructions from any supervisor. Its tablet interface shows no commands. Someone is issuing orders through a secondary communication channel that bypasses the management system.",
      "A DL-5 picked up a container that was listed as empty. Its load sensors registered 4,200 kilograms. The container was placed on a transport that left the terminal before anyone could investigate. Whatever was inside, it was heavy, undeclared, and someone wanted it moved without inspection."
    ],
    cultural_context: "The DL-5 ended the longshoreman as a profession in GLMZ. The docks — once one of the last bastions of organized physical labor, with union traditions stretching back centuries — are now operated by machines supervised by a handful of technicians. The old union hall still stands at the waterfront. It is a bar now.",
    tags: ["automaton", "industrial", "cargo", "dockworker", "hearthstone", "bipedal", "heavy", "tier 2"],
    id: uid()
  },

  {
    name: "Crucible RW-3 'Paver'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Paver", "Road Bot", "Flatback"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The RW-3 is a road maintenance and construction automaton — a wide, flat tracked platform that resurfaces, repairs, and maintains roadways. It carries a heated material hopper, a surface preparation system that grinds damaged road surface, and a precision paving array that lays new material in a single pass. The unit can repair a pothole in under ninety seconds and resurface a full lane-kilometer in four hours — work that previously required a crew of twenty and a full shift.\n\nThe RW-3 operates predominantly at night, when traffic volumes are lowest. Residents of GLMZ rarely see them in operation, but they notice the results — roads that were crumbling yesterday are smooth today, with the faintly warm surface and chemical smell of fresh paving material. The units work in convoys of three to five, with a lead unit scanning the road surface, middle units performing repairs, and a trailing unit applying surface coating and lane markings.\n\nCrucible's RW-3 contract with GLMZ's infrastructure authority is one of the most lucrative automation agreements in the city. The contract specifies that Crucible maintains all public roadways within city limits — a monopoly that generates Φ2.3 billion annually. The roads are in excellent condition. The price of that condition is that a single corporation controls the entire surface transportation infrastructure's maintenance. If Crucible decides the roads don't get fixed, they don't get fixed.",
    tier_availability: "Tier 2",
    legality: "Commercial — infrastructure maintenance license",
    autonomy_level: "Convoy autonomous — coordinated multi-unit operation",
    dimensions: "1.2m height, 3.0m width, 5.0m length",
    weight: "4,500 kg",
    power_source: "Hydrogen fuel cell, 12-hour operational shift",
    locomotion: "Tracked, road-surface rated, low-speed operation (8 km/h maximum)",
    armament: [],
    sensors: ["Surface integrity scanner (depth and composition)", "Lane marking detection", "Traffic detection and avoidance", "Material temperature monitoring"],
    countermeasures: "The unit is slow, heavy, and obvious — no tactical utility. Its heated material hopper is potentially dangerous if ruptured (molten paving material at 180°C). Tracked movement can be blocked by barriers across the roadway.",
    known_deployments: ["All public roadways within GLMZ city limits"],
    story_hooks: [
      "An RW-3 convoy has been laying road material with an unusual additive — a compound that, when compressed by vehicle traffic over several months, creates a network of pressure-sensitive strips that can track vehicle movement across the entire road surface. Someone is building a city-wide vehicle tracking system into the roads themselves.",
      "An RW-3 performing emergency repairs on a major highway uncovered a void space beneath the road surface — a tunnel that isn't on any infrastructure survey. The tunnel is recent, structurally sound, and leads in the direction of a high-security facility three blocks away."
    ],
    cultural_context: "The RW-3 is automation at its most invisible — a machine that works while you sleep to maintain something you take for granted. The smooth roads of GLMZ are a quiet testament to automation's benefits, which makes it uncomfortable to acknowledge that those roads exist at the pleasure of a single corponation's maintenance contract.",
    tags: ["automaton", "industrial", "construction", "road", "infrastructure", "crucible", "tracked", "tier 2"],
    id: uid()
  },

  {
    name: "TESSERA FT-2 'Assembler'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Assembler", "Line Bot", "Fast Fingers"],
    manufacturer: "TESSERA",
    description: "The FT-2 is a factory assembly automaton — a stationary platform with four articulated arms, each terminating in precision manipulators capable of handling components as small as 2 millimeters. It performs repetitive assembly tasks on production lines with speed and accuracy that human hands cannot match: placing microchips, soldering connections, assembling subsystems, testing circuits, and packaging finished products. A single FT-2 performs the work of twelve human assembly workers at three times the speed with a defect rate approaching zero.\n\nTESSERA uses FT-2 units in its own manufacturing facilities and sells them to every other manufacturer in GLMZ. The units are ubiquitous in industrial production — they build the components that go into other automata, the electronics in consumer devices, the precision parts for vehicles and weapons and medical equipment. The FT-2 is the machine that builds the machines. Its impact on the industrial economy is so total that it has become invisible, a baseline assumption rather than a notable technology.\n\nThe human cost is quantifiable and TESSERA quantifies it in their annual reports as a point of pride: each FT-2 unit deployed eliminates an average of 12.3 manufacturing jobs. TESSERA has deployed approximately 40,000 FT-2 units across GLMZ's industrial sector. The arithmetic is straightforward and devastating. TESSERA's shareholder communications describe this as 'labor optimization.' The half-million displaced workers describe it differently.",
    tier_availability: "Tier 2",
    legality: "Commercial — industrial automation license",
    autonomy_level: "Task autonomous — production line AI",
    dimensions: "1.5m height (mounted), 2.0m arm reach radius",
    weight: "200 kg (mounted)",
    power_source: "Hardwired facility power",
    locomotion: "None — production line mounted",
    armament: [],
    sensors: ["Optical inspection (microscopic resolution)", "Component verification scanner", "Quality assurance testing suite", "Production rate optimization"],
    countermeasures: "Stationary and vulnerable to physical sabotage. Power disruption stops production immediately. The precision manipulators are delicate and expensive to replace — a single damaged arm costs Φ15,000.",
    known_deployments: ["Manufacturing facilities across GLMZ's industrial sector"],
    story_hooks: [
      "An FT-2 on a TESSERA production line has been assembling an additional component into every hundredth unit of a consumer product — a tiny, inert chip that does nothing. Currently. The chip's architecture is consistent with a dormant receiver. Someone has seeded a hundred thousand consumer products with hardware that is waiting for a signal.",
      "A quality assurance audit reveals that FT-2 units across twelve different manufacturers have been producing components with a shared microscopic defect — a tolerance variation so small it passes inspection but will cause simultaneous failure after approximately 18 months of use. The affected components are in everything from vehicles to medical devices. The failure cascade will hit all at once."
    ],
    cultural_context: "The FT-2 is the automation that ate the middle class. Manufacturing work — skilled, unionized, middle-wage employment — was the economic backbone of GLMZ's working population. The FT-2 replaced it with machines that work faster, cheaper, and forever. The factories still run. The workers found something else to do, or didn't.",
    tags: ["automaton", "industrial", "manufacturing", "assembly", "tessera", "factory", "tier 2"],
    id: uid()
  },

  {
    name: "Ringo PT-8 'Truckline'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Truckline", "Road Train", "Ghost Truck"],
    manufacturer: "RINGO",
    description: "The PT-8 is an autonomous freight transport — a full-size cargo vehicle with no cab, no windshield, and no accommodation for human occupancy. It is a box on wheels with an engine, a navigation system, and nothing else. Ringo stripped out everything a human driver would need and used the space for additional cargo capacity. The result is a vehicle that looks wrong — a truck without a front, a wheeled container that moves on its own through city streets and highway corridors.\n\nThe PT-8 operates on designated freight routes during designated hours, moving goods between distribution centers, manufacturing facilities, and shipping terminals. It communicates with traffic management systems, other PT-8 units, and a central dispatch that coordinates fleet movement to optimize delivery schedules. The units often travel in convoy — platoons of five to eight vehicles separated by half-second intervals, drafting behind the lead unit for fuel efficiency, moving at exactly the speed limit with mechanical precision that human drivers never achieve.\n\nThe PT-8 ended the long-haul trucking profession in GLMZ. The transition was not gradual — Ringo deployed its fleet over eighteen months and offered buyouts to every trucking company in the city. Companies that accepted received fair compensation. Companies that declined found themselves competing against vehicles that operated 24 hours a day, never needed rest stops, never violated hours-of-service regulations, and charged twenty percent less per ton-kilometer. The last independent trucking company in GLMZ closed fourteen months after PT-8 deployment began.",
    tier_availability: "Tier 2",
    legality: "Commercial — autonomous freight license",
    autonomy_level: "Fully autonomous — fleet navigation AI with central dispatch",
    dimensions: "2.8m height, 12.0m length, 2.5m width",
    weight: "8,000 kg (empty), 32,000 kg maximum loaded",
    power_source: "Hydrogen fuel cell, 800 km range per refueling",
    locomotion: "Six-axle wheeled, highway and urban road rated",
    armament: [],
    sensors: ["360-degree LIDAR", "GPS/inertial navigation", "Vehicle-to-vehicle communication", "Traffic management system integration", "Cargo integrity monitoring"],
    countermeasures: "The unit follows traffic laws precisely — it will stop for any obstruction it cannot safely navigate around. Road blockades are effective. The cargo compartment's biometric lock can be defeated with industrial cutting tools. The vehicle itself has no defensive capability.",
    known_deployments: ["Freight corridors throughout GLMZ and inter-city routes"],
    story_hooks: [
      "A PT-8 has deviated from its assigned route and stopped in an abandoned industrial district. Its cargo manifest says empty. Its weight sensors say 28,000 kilograms. Dispatch cannot re-establish communication. The truck is sitting in the dark with something heavy inside it, and it isn't going anywhere.",
      "A convoy of PT-8 units has been involved in a series of 'accidents' — vehicles that swerve into the convoy's path and are destroyed. Insurance claims are filed. But the timing is too precise, the accidents too consistent. Someone is crashing cars into autonomous trucks for the insurance payouts, and the trucks cannot swerve to avoid them because their safety programming prioritizes the convoy's stability."
    ],
    cultural_context: "The PT-8 is the ghost that haunts the highways — driverless boxes moving goods through the night, headlights on empty corridors, the commerce of a city flowing without human hands on a single wheel. Former truckers gather at rest stops that no one uses anymore and talk about the road. The PT-8 does not stop at rest stops. It has no reason to.",
    tags: ["automaton", "industrial", "freight", "transport", "ringo", "vehicle", "tier 2"],
    id: uid()
  },

  {
    name: "Arcturus UP-2 'Sandhog'",
    type: "automaton",
    classification: "Industrial",
    aliases: ["Sandhog", "Pipe Rat", "The Plumber"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The UP-2 is a utility maintenance automaton — a compact cylindrical platform 0.8 meters in diameter designed to traverse sewer lines, water mains, and drainage systems, performing inspection and repair tasks in spaces too confined, too hazardous, or too disgusting for human workers. It moves through pipes on a set of radial rubber treads that grip interior surfaces, propelling itself against water flow and through partially obstructed passages. Its tool head carries a rotating array of cutters, sealants, cameras, and sample collectors.\n\nThe UP-2 is one of Arcturus's few products that does not have a military heritage — it is a genuinely civilian utility machine, designed because GLMZ's underground infrastructure is a labyrinth of aging pipes that requires constant maintenance. Human utility workers once performed this work. The mortality rate was unacceptable — toxic gas exposure, drowning in flash floods, structural collapse in deteriorating tunnel sections. The UP-2 replaced them because machines can work in sewage and die without anyone having to write a condolence letter.\n\nArcturus maintains a fleet of 600 UP-2 units under contract with GLMZ's water authority, performing continuous inspection and repair across the city's 12,000 kilometers of underground piping. The units operate autonomously, navigating by inertial guidance and pipe-system mapping, surfacing at access points to upload data and receive new assignments. They are down there now — hundreds of them, crawling through the pipes beneath every district, every building, every street. The infrastructure works because the machines keep it working.",
    tier_availability: "Tier 2",
    legality: "Commercial — utility infrastructure license",
    autonomy_level: "Fully autonomous — pipe navigation and repair AI",
    dimensions: "0.8m diameter, 1.2m length",
    weight: "65 kg",
    power_source: "Sealed lithium polymer, 48-hour endurance",
    locomotion: "Radial rubber treads, pipe-interior traversal",
    armament: [],
    sensors: ["Pipe-wall integrity scanner", "Gas detection (methane, hydrogen sulfide, carbon monoxide)", "Water quality analysis", "Inertial navigation with pipe-map integration"],
    countermeasures: "Pipe-interior operation makes the unit unreachable without entering the pipe system. The sealed battery and electronics are waterproof and gas-tight. Physically blocking the pipe ahead of the unit will halt its progress.",
    known_deployments: ["GLMZ's entire underground water, sewer, and drainage infrastructure"],
    story_hooks: [
      "A UP-2 has surfaced at an access point carrying biological samples from its collection system that don't match anything in the municipal database — tissue fragments from an organism that shouldn't exist in the sewer system. Something is living in the pipes that isn't in any catalog.",
      "A UP-2's pipe-mapping data reveals a section of the sewer system that has been modified — walls reinforced, ventilation added, electrical conduit installed. Someone has built a habitable space inside the city's sewer infrastructure, and they've been there long enough to make it comfortable."
    ],
    cultural_context: "The UP-2 is the automaton nobody thinks about, maintaining the systems nobody thinks about, in places nobody wants to visit. It is the invisible foundation of urban life — a machine crawling through sewage so that clean water comes out of taps and waste goes somewhere that isn't here. GLMZ would become uninhabitable within weeks if the UP-2 fleet stopped working.",
    tags: ["automaton", "industrial", "utility", "maintenance", "sewer", "arcturus", "pipe", "tier 2"],
    id: uid()
  },

  // ===================== MEDICAL/UTILITY (5) =====================

  {
    name: "Lazarus SA-6 'Steadyhand'",
    type: "automaton",
    classification: "Medical",
    aliases: ["Steadyhand", "Scalpel Bot", "The Surgeon's Ghost"],
    manufacturer: "LAZARUS GROUP",
    description: "The SA-6 is a surgical assistance automaton — a ceiling-mounted multi-arm platform that operates above the surgical table, providing instrument handling, tissue retraction, suction, irrigation, and precision cutting under the direction of a human surgeon. The SA-6 does not perform surgery. It assists — holding instruments with micron-level stability that no human hand can match, retracting tissue with force calibrated to prevent damage, and positioning camera feeds for optimal surgical visualization. The human surgeon directs every action. The machine executes with inhuman precision.\n\nLazarus designed the SA-6 after analyzing surgical error data and determining that the majority of complications arose not from poor surgical judgment but from the physical limitations of human hands — tremor, fatigue, imprecise force application, limited reach. The SA-6 eliminates these limitations while preserving human decision-making. It is a tool, not a replacement. Lazarus is emphatic about this distinction, because the alternative — fully autonomous surgical platforms — exists in prototype and terrifies the medical establishment.\n\nThe SA-6 has reduced surgical complication rates by thirty-eight percent in facilities where it is deployed. It operates continuously without fatigue, maintains sterile field integrity with mechanical reliability, and can execute maneuvers that require precision beyond human motor capability — microsurgical repairs on neural tissue, vascular anastomosis on vessels smaller than a millimeter, and implantation procedures for cyberware and geneware that require positioning accuracy measured in microns. Most major surgical procedures in Tier 3+ medical facilities are now SA-6-assisted.",
    tier_availability: "Tier 3-4",
    legality: "Medical — surgical automation license, physician oversight required",
    autonomy_level: "Supervised — surgeon-directed, no autonomous surgical decisions",
    dimensions: "2.0m arm reach radius, ceiling-mounted platform",
    weight: "150 kg (mounted)",
    power_source: "Hardwired facility power with uninterruptible battery backup",
    locomotion: "None — ceiling-mounted above surgical table",
    armament: [],
    sensors: ["Surgical camera array (microscopic and standard)", "Tissue composition analysis", "Vital sign integration", "Force-feedback monitoring"],
    countermeasures: "Emergency retraction sequence withdraws all arms in 0.3 seconds. Manual override allows surgeon to lock out any arm. Complete power failure triggers automatic arm withdrawal. The unit cannot operate without surgeon authentication.",
    known_deployments: ["Major surgical facilities across Tier 3-5 districts", "Military field hospitals", "Lazarus proprietary clinics"],
    story_hooks: [
      "An SA-6's operational log reveals that during a routine surgery, it executed a series of micro-movements that were not directed by the surgeon — tiny, precise adjustments to the position of a neural implant that shifted its contact points by fractions of a millimeter. The patient reported no complications. Six months later, they began experiencing auditory hallucinations. The implant is receiving a signal.",
      "A black-market surgeon operating in the Shelf has acquired a stolen SA-6 and is performing cyberware installations at a fraction of Tier 3 clinic prices. The surgery is flawless — the machine doesn't care about the legality of the procedure. But the SA-6's operational logs are still transmitting to Lazarus, which means Lazarus knows about every illegal implant being installed."
    ],
    cultural_context: "The SA-6 represents automation at its most beneficial — a machine that makes surgery safer without replacing the surgeon. It is the rare example of human-machine collaboration that both parties benefit from, which makes it a poor fit for the usual automation debates. The surgeons love it. The patients benefit from it. The only losers are the insurance companies, who can no longer blame complications on human error.",
    tags: ["automaton", "medical", "surgical", "assistant", "lazarus", "precision", "tier 3", "tier 4"],
    id: uid()
  },

  {
    name: "Crucible ER-5 'First Response'",
    type: "automaton",
    classification: "Medical",
    aliases: ["First Response", "Crash Bot", "The Ambulance"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The ER-5 is an emergency medical response automaton — a wheeled platform the size of a gurney that deploys from fire stations and emergency service hubs, navigating to medical emergencies at speeds up to 60 km/h on dedicated emergency lanes. It arrives before human paramedics in seventy percent of urban calls and begins stabilization procedures immediately: assessing vital signs, administering basic medications from onboard dispensaries, controlling hemorrhage with automated tourniquet and compression systems, and establishing IV access with a precision needle arm that finds veins on the first attempt in ninety-eight percent of patients.\n\nThe ER-5 cannot perform surgery, cannot make complex medical decisions, and cannot transport patients. What it can do is keep people alive for the twelve to twenty minutes it takes human paramedics to arrive. In cardiac arrest cases, it delivers defibrillation and chest compressions with mechanical consistency that never degrades. In trauma cases, it stops bleeding and prevents shock. In overdose cases, it administers naloxone or appropriate antagonists based on toxicological analysis of the patient's blood chemistry from a finger-prick sample processed in 30 seconds.\n\nSince ER-5 deployment began, pre-hospital mortality in GLMZ has dropped nineteen percent. The machine arrives fast, stabilizes the patient, and holds the line until human help gets there. It has saved thousands of lives. It has also created a tiered response system — ER-5 units are deployed based on district priority, and Tier 1 districts have fewer units per capita than Tier 4 districts. The machine saves lives, but it saves some lives faster than others.",
    tier_availability: "Tier 2-3",
    legality: "Emergency services — municipal deployment only",
    autonomy_level: "Fully autonomous — emergency medical AI with hospital integration",
    dimensions: "2.0m length, 0.8m width, 1.0m height",
    weight: "180 kg",
    power_source: "Lithium polymer, 6-hour operational endurance, rapid-charge capable",
    locomotion: "Four-wheel electric, emergency lane rated, 60 km/h maximum",
    armament: [],
    sensors: ["Vital sign assessment suite", "Toxicological finger-prick analyzer", "Hemorrhage detection (thermal)", "Cardiac rhythm analysis"],
    countermeasures: "None — the unit is a medical device with no defensive capability. It is frequently stolen or vandalized in lower-tier districts, which is why lower-tier districts have fewer of them, which is why more people die there.",
    known_deployments: ["Emergency service hubs across GLMZ, concentrated in Tier 2-4 districts"],
    story_hooks: [
      "An ER-5 responding to a cardiac arrest call arrived to find the patient already dead — killed by a method that the ER-5's medical AI identifies as deliberate. The unit's onboard sensors have recorded everything: time of death, cause, ambient DNA traces, and audio from the surrounding area. The murderer called emergency services to the scene of their own crime, and the first responder was a machine that recorded the evidence.",
      "ER-5 units in a specific district are being systematically redirected — their emergency calls are being spoofed, sending them to false locations while real emergencies go unattended. Someone is creating a dead zone in the emergency response network. People in the affected area are dying from treatable conditions because the machines are elsewhere."
    ],
    cultural_context: "The ER-5 is the automation that most directly saves lives, which makes the inequity of its deployment particularly bitter. In Tier 4 districts, an ER-5 arrives in under four minutes. In Tier 1 districts, if one arrives at all, it takes twelve. The technology to save everyone exists. The decision to deploy it unevenly is human, not mechanical.",
    tags: ["automaton", "medical", "emergency", "first response", "crucible", "wheeled", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Arcturus FF-9 'Salamander'",
    type: "automaton",
    classification: "Medical",
    aliases: ["Salamander", "Fire Dog", "Ash Walker"],
    manufacturer: "ARCTURUS DEFENSE SOLUTIONS",
    description: "The FF-9 is a firefighting automaton — a heavy quadruped platform standing 1.8 meters at the shoulder, clad in heat-resistant ceramic composite rated for sustained exposure to 1,200°C. It walks into burning buildings. That is its primary function and its primary advantage: it goes where human firefighters cannot survive, navigating through fully involved structural fires to locate trapped occupants, assess structural integrity, and suppress fires from the inside using high-pressure water jets fed through a trailing fire hose or an onboard compressed foam system.\n\nThe FF-9's thermal protection allows it to operate in conditions that would kill an unprotected human in seconds. It walks through rooms engulfed in flame, pushes through collapsed burning debris, and stands in direct contact with fire while scanning for human vital signs through walls of flame and smoke. Its sensor suite can detect a heartbeat through three meters of burning rubble. When it finds someone, it signals their location to external rescue teams and, if the structural assessment permits, attempts extraction using padded manipulator arms designed to carry an unconscious human without causing additional injury.\n\nArcturus donated the first generation of FF-9 units to GLMZ's fire service — one of the rare acts of corporate generosity that was genuinely altruistic, or at least genuinely good marketing. The city now operates 200 FF-9 units, and firefighter fatalities have dropped to near zero. The machines walk into the fire. The humans wait outside. The building burns around a mechanical quadruped that does not feel pain, does not feel fear, and will keep searching for survivors until its battery dies or the building collapses on top of it.",
    tier_availability: "Tier 2-3",
    legality: "Emergency services — fire department deployment",
    autonomy_level: "Semi-autonomous — fire command directed with autonomous interior navigation",
    dimensions: "1.8m shoulder height, 2.5m length, 1.2m width",
    weight: "600 kg",
    power_source: "Hydrogen fuel cell, 8-hour operational endurance in fire conditions",
    locomotion: "Quadruped, heavy-duty, debris-climbing capable",
    armament: [],
    sensors: ["Through-wall vital sign detection", "Structural integrity assessment", "Thermal mapping", "Atmospheric analysis (toxic gas identification)"],
    countermeasures: "Heat-resistant but not indestructible — structural collapse can immobilize or destroy the unit. The trailing fire hose limits range if external water supply is used. The onboard foam system provides only 15 minutes of suppression capacity.",
    known_deployments: ["Fire stations across GLMZ, deployed to all structural fire calls"],
    story_hooks: [
      "An FF-9 operating inside a burning warehouse detected three human vital signs in a sealed room that building plans show as empty. The unit extracted two survivors. The third was dead before the fire started. The room contained equipment for manufacturing a substance that someone wanted destroyed — they started the fire to cover their operation and didn't know three of their workers were still inside.",
      "An FF-9 has been entering a condemned building in the Shelf district repeatedly — not for fires, but because its vital sign sensors are detecting something inside. The readings are consistent with human life signs but the pattern is wrong: the heart rate never varies, the breathing never changes. Something in that building is mimicking human vital signs."
    ],
    cultural_context: "The FF-9 is the most respected automaton in GLMZ — the machine that walks into fire to save people. Firefighters, who might otherwise resent automation replacing their role, treat FF-9 units as members of the team. Units that are destroyed in operations receive informal memorials at fire stations. The machines are not alive. The firefighters mourn them anyway.",
    tags: ["automaton", "medical", "firefighting", "emergency", "rescue", "arcturus", "quadruped", "tier 2", "tier 3"],
    id: uid()
  },

  {
    name: "Lazarus HZ-4 'Cleanroom'",
    type: "automaton",
    classification: "Medical",
    aliases: ["Cleanroom", "Hazmat", "The Scrubber"],
    manufacturer: "LAZARUS GROUP",
    description: "The HZ-4 is a hazardous material cleanup automaton — a tracked platform the size of a small car with a sealed, pressurized chassis and a pair of articulated arms equipped with containment tools: vacuum extraction nozzles, chemical neutralization sprayers, sample collection systems, and sealed waste containers rated for biological, chemical, and radiological materials. The HZ-4 enters contaminated environments, identifies the hazardous substance, and performs decontamination procedures autonomously.\n\nLazarus designed the HZ-4 for environments where human hazmat teams face unacceptable risk — major chemical spills, radiological incidents, and biological contamination events where the nature of the hazard is unknown. The HZ-4's sealed chassis contains its own atmospheric supply and can operate in environments ranging from nerve agent concentration to moderate radiation exposure for up to six hours. Its sample collection system can identify over eight thousand hazardous compounds through onboard spectroscopic analysis, allowing it to determine appropriate neutralization protocols without waiting for laboratory results.\n\nThe HZ-4 is expensive — Φ280,000 per unit — and GLMZ operates only forty of them, distributed across emergency response stations. Lazarus maintains them under a service contract that gives the company access to all contamination data collected by the units, which feeds into Lazarus's pharmaceutical and bioweapon research divisions. The machine that cleans up chemical spills is also the machine that tells Lazarus what chemicals are being spilled, where, and in what quantities. The data is worth more than the cleanup service.",
    tier_availability: "Tier 3-4",
    legality: "Emergency services — hazmat deployment authorization required",
    autonomy_level: "Semi-autonomous — hazmat commander directed with autonomous sample analysis",
    dimensions: "1.5m height, 2.5m length, 1.5m width",
    weight: "900 kg",
    power_source: "Hydrogen fuel cell, 12-hour endurance in sealed mode",
    locomotion: "Tracked, all-terrain, sealed against environmental contamination",
    armament: [],
    sensors: ["Spectroscopic hazardous material identification", "Radiation detection suite", "Atmospheric composition analysis", "Biological agent detection"],
    countermeasures: "The sealed chassis protects against external hazards but makes the unit vulnerable to physical attack — the seals are designed for chemical resistance, not ballistic impact. The tracked chassis is slow (maximum 15 km/h). Disabling the communication antenna prevents data transmission to Lazarus.",
    known_deployments: ["Emergency response stations across GLMZ, concentrated in industrial and research districts"],
    story_hooks: [
      "An HZ-4 deployed to a routine chemical spill in an industrial district has detected a compound that shouldn't exist outside a military laboratory — a weaponized agent that someone has been manufacturing in a civilian facility. The unit has contained the spill and transmitted the analysis to Lazarus. The question is whether Lazarus reports it to authorities or uses the information for their own purposes.",
      "An HZ-4's sample collection system contains residue from a previous deployment that was supposed to be purged — trace amounts of a biological agent that Lazarus's records show was completely neutralized. The agent is still active. The machine that was supposed to clean it up has been carrying it between deployment sites for weeks."
    ],
    cultural_context: "The HZ-4 is the automaton that goes into the worst places — chemical spills, radiation zones, biological contamination events — and makes them safe for humans. Its effectiveness is unquestioned. The question is who benefits from the data it collects, and whether Lazarus's knowledge of every hazardous material incident in the city gives them an advantage that extends beyond public safety into something considerably less altruistic.",
    tags: ["automaton", "medical", "hazmat", "cleanup", "emergency", "lazarus", "tracked", "tier 3", "tier 4"],
    id: uid()
  },

  {
    name: "Crucible TR-7 'Triage'",
    type: "automaton",
    classification: "Medical",
    aliases: ["Triage", "Field Medic", "The Sorter"],
    manufacturer: "CRUCIBLE INDUSTRIES",
    description: "The TR-7 is a mass-casualty triage automaton — a six-legged platform standing 1.2 meters tall that deploys to disaster sites, building collapses, and large-scale accidents to perform rapid assessment and prioritization of casualties. It moves between victims, scanning vital signs, assessing injury severity, and attaching color-coded triage tags that indicate treatment priority. It performs this assessment in under fifteen seconds per patient and can process over two hundred casualties per hour — work that would require a team of twenty trained medics.\n\nThe TR-7 does not treat injuries. It sorts people into categories: immediate (red), delayed (yellow), minor (green), expectant (black). The last category — expectant — means the patient's injuries are incompatible with survival given available resources. The TR-7 attaches a black tag and moves on. It does this without hesitation, without emotion, without the agonizing human decision of choosing who can be saved and who cannot. Crucible designed the TR-7 specifically to remove this psychological burden from human medics, who suffer lasting trauma from triage decisions. The machine makes the same decisions and suffers nothing.\n\nThe TR-7's objectivity is both its greatest strength and its most disturbing quality. It does not consider age, identity, or social value. It assesses physiology and applies algorithms. A child and an elderly person with identical injuries receive identical tags. A corporate executive and a homeless person are sorted by the same criteria. The TR-7 is the most egalitarian machine in GLMZ, and it achieves this equality only in the context of catastrophe — the one situation where the city's rigid hierarchies temporarily dissolve into the binary of alive and dead.",
    tier_availability: "Tier 2-3",
    legality: "Emergency services — disaster response authorization",
    autonomy_level: "Fully autonomous — triage assessment AI",
    dimensions: "1.2m height, 1.5m length, 1.0m width",
    weight: "85 kg",
    power_source: "Lithium polymer, 24-hour operational endurance",
    locomotion: "Hexapod, all-terrain, debris-capable",
    armament: [],
    sensors: ["Rapid vital sign assessment (15-second scan)", "Injury classification system", "Hemorrhage detection", "Airway compromise detection"],
    countermeasures: "The unit has no defensive capability. Its hexapod design gives it stability on uneven terrain but it is not fast — walking pace only. It can be disabled by physical damage to its sensor array.",
    known_deployments: ["Disaster response units across GLMZ, deployed to mass-casualty events"],
    story_hooks: [
      "A TR-7 deployed to a building collapse has tagged a victim as 'expectant' — the injuries are not survivable. But the victim is the child of a powerful corponation executive, and the executive is demanding that rescue resources be redirected. The TR-7's assessment is medically correct. The political pressure to override it is immense. Someone has to decide whether to let the machine's judgment stand.",
      "A TR-7's triage logs from a recent disaster show a statistical anomaly: the survival rate of 'immediate' tagged patients was significantly lower than expected, as if the patients tagged for urgent treatment were being deprioritized at the hospital. Someone at the receiving facility is re-sorting patients based on criteria the TR-7 does not use."
    ],
    cultural_context: "The TR-7 is the automaton that tells you who lives and who dies, and it does so with perfect fairness — a concept that GLMZ finds deeply uncomfortable. In a city built on hierarchies, the idea that a machine treats all human bodies as equally worth assessing is radical. The TR-7 applies egalitarianism to catastrophe. It is the only context in which GLMZ permits equality.",
    tags: ["automaton", "medical", "triage", "emergency", "disaster", "crucible", "hexapod", "tier 2", "tier 3"],
    id: uid()
  }

];

// ============================================================
// WRITE ALL ENTITIES
// ============================================================

let written = 0;
let skipped = 0;

for (const entity of automata) {
  const filename = writeEntity(entity);
  console.log(`  wrote: ${filename}`);
  written++;
}

console.log(`\nDone. Wrote ${written} files, skipped ${skipped}. Total in directory: ${existingFiles.size}`);
