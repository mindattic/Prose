const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'factions');
fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const existing = new Set(fs.readdirSync(OUTPUT_DIR));

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  const trimmed = name.slice(0, 60);
  return trimmed
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

let written = 0;
let skipped = 0;

function writeFaction(f) {
  const filename = slugify(f.name) + '.json';
  if (existing.has(filename)) {
    console.log(`SKIP (exists): ${filename}`);
    skipped++;
    return;
  }
  const data = {
    id: genId(),
    type: 'faction',
    name: f.name,
    aliases: f.aliases || [],
    motto: f.motto || '',
    description: f.description || '',
    ideology: f.ideology || '',
    territory: f.territory || '',
    leadership: f.leadership || '',
    methods: f.methods || [],
    resources: f.resources || [],
    goals: f.goals || [],
    relationships: f.relationships || [],
    narrative_function: f.narrative_function || '',
    story_hooks: f.story_hooks || [],
    tags: f.tags || []
  };
  fs.writeFileSync(path.join(OUTPUT_DIR, filename), JSON.stringify(data, null, 2));
  console.log(`WROTE: ${filename}`);
  written++;
}

// ============================================================================
// UNDERGROUND MEDICAL COOPERATIVES
// ============================================================================

const medicalCoops = [
  {
    name: "The Suture Collective",
    aliases: ["Suture", "The Stitchers", "Needle & Thread", "SC"],
    motto: "We mend what the market breaks.",
    description: "The Suture Collective is the largest unauthorized medical network operating below Tier 3 in GLMZ, providing everything from emergency trauma surgery to chronic disease management for the estimated 1.4 million residents who cannot afford Lazarus Pharmaceuticals' tiered healthcare plans. The Collective operates out of rotating clinic sites -- repurposed shipping containers, abandoned retail spaces, the backrooms of sympathetic businesses -- never staying in one location for more than seventy-two hours before Lazarus enforcement teams can triangulate their signal.\n\nThe organization was founded in 2178 by Dr. Keanu Mataafa-Chen, a former Lazarus trauma surgeon who walked out of a Tier 4 emergency ward after being ordered to prioritize a corporate executive's cosmetic revision over a Shelf child bleeding out from an industrial accident. Mataafa-Chen recruited eleven other disillusioned medical professionals and began performing surgeries in a converted laundry facility in the Shelf's Rustwater district. Within five years, the Collective had grown to over three hundred active practitioners -- licensed doctors, combat medics, veterinarians repurposing animal medicine for human patients, and self-taught field surgeons whose technique would horrify any medical board but whose survival rates in the Shelf exceed those of some legitimate Tier 2 clinics.\n\nThe Collective's greatest vulnerability is its supply chain. Legitimate medical supplies are tracked, serialized, and monitored by Lazarus's distribution network. The Collective relies on a patchwork of stolen shipments, expired medications reclaimed from disposal facilities, improvised surgical tools fabricated by sympathetic makers, and a controversial arrangement with Crucible Industries to purchase surplus military medical supplies at gray-market rates. Every procedure carries risk that a licensed facility would never accept -- but the alternative for their patients is no procedure at all, and the Collective's practitioners have learned to work miracles with what amounts to medical archaeology.",
    ideology: "Healthcare is a right that precedes commerce. The commodification of medicine is not an economic model but a moral architecture designed to make death profitable. The Suture Collective does not seek to reform the system -- it seeks to make the system irrelevant by proving that competent medical care can exist outside corporate control.",
    territory: "Mobile clinics throughout the Shelf and lower Circuit. Primary staging areas in Rustwater, the Kennels, and the flooded sublevel districts collectively known as the Undertow. Occasional covert operations in Tier 2 zones where insurance gaps leave residents without coverage.",
    leadership: "Dr. Keanu Mataafa-Chen remains the Collective's spiritual leader but day-to-day operations are managed by a rotating council of senior practitioners called the Triage Board. No single person knows all clinic locations or the full membership roster -- compartmentalization is survival.",
    methods: [
      "Rotating mobile clinic sites that relocate every 48-72 hours",
      "Dead-drop supply caches hidden throughout the Shelf infrastructure",
      "Encrypted patient records stored on distributed mesh networks",
      "Training programs that turn anyone willing to learn into competent field medics",
      "Mutual aid networks that trade medical services for food, shelter, and protection"
    ],
    resources: [
      "Over 300 active medical practitioners across all specialties",
      "Gray-market supply chain including diverted Lazarus shipments",
      "Surplus military medical supplies acquired through Crucible intermediaries",
      "Network of sympathetic businesses providing temporary clinic space",
      "Distributed encrypted patient database covering over 400,000 individuals"
    ],
    goals: [
      "Provide comprehensive medical care to every resident below Tier 3 regardless of ability to pay",
      "Train enough field medics to make the Collective's leadership dispensable",
      "Establish permanent underground clinic facilities with surgical-grade equipment",
      "Develop open-source pharmaceutical manufacturing capability"
    ],
    relationships: [
      { name: "Lazarus Pharmaceuticals", type: "enemy", description: "Lazarus views the Collective as intellectual property thieves and unlicensed competitors. Their enforcement teams actively hunt Collective clinics. Individual Lazarus employees occasionally leak supplies or information to the Collective out of conscience.", tags: ["corporate", "conflict"] },
      { name: "Crucible Industries", type: "supplier", description: "Crucible sells surplus military medical supplies to Collective intermediaries at gray-market rates. The arrangement is profitable for Crucible and deniable enough to maintain.", tags: ["corporate", "trade"] },
      { name: "The Shelf community networks", type: "ally", description: "The Collective is deeply embedded in Shelf communities, who provide early warning of raids, shelter for practitioners, and the social infrastructure that makes the clinics possible.", tags: ["community", "mutual-aid"] }
    ],
    narrative_function: "The Suture Collective embodies the tension between institutional competence and moral urgency. They are not rebels playing doctor -- they are real doctors who chose to practice outside a system that prices human life by tier. Their existence asks whether medicine practiced imperfectly for everyone is better than medicine practiced perfectly for the few.",
    story_hooks: [
      "A Collective surgeon discovers that a batch of gray-market anesthetics has been tampered with -- patients are waking up during surgery with implanted memories they did not have before. Someone is using the Collective's supply chain to distribute neural payloads.",
      "Lazarus has planted an undercover operative inside the Collective who has been feeding clinic locations to enforcement teams. The mole is also one of the Collective's most skilled surgeons, and exposing them means losing the person who saves the most lives.",
      "A Tier 4 executive's child needs a rare procedure that only a Collective specialist can perform. The executive offers the Collective enough funding to operate openly for a year -- but accepting means exposing the network to corporate scrutiny."
    ],
    tags: ["faction", "medical", "underground", "mutual-aid", "shelf", "circuit", "healthcare"]
  }
];

// ============================================================================
// AUTOMATON RIGHTS GROUPS
// ============================================================================

const automatonRights = [
  {
    name: "The Maintenance Covenant",
    aliases: ["Covenant", "Wrench Saints", "The Oilers", "MC"],
    motto: "A machine neglected is a conscience abandoned.",
    description: "The Maintenance Covenant is not an automaton rights group in the way most people understand the term -- they do not argue that machines are alive, do not claim automatons possess consciousness, and do not petition for legal personhood. What they argue is something more uncomfortable: that a society can be judged by how it treats the things it depends on, and that GLMZ's systematic neglect of its automaton infrastructure is not just an engineering problem but a moral one. The Covenant holds that machines deserve maintenance, respect, and dignified decommissioning not because they feel pain, but because humans who allow suffering -- even mechanical suffering -- corrode their own capacity for empathy.\n\nFounded in 2186 by Emi Osei-Tanaka, a former Ringo automaton maintenance engineer who spent fifteen years watching functional machines get scrapped for parts because repair was less profitable than replacement, the Covenant began as a repair collective. Volunteers would enter automaton junkyards in the Shelf and restore machines that had been discarded -- not to resell them, but to return them to service in communities that needed them. A construction automaton repaired and donated to a Shelf housing project. A medical diagnostic unit restored and given to the Suture Collective. A domestic service unit fixed and placed in an elder care facility that couldn't afford human staff.\n\nThe Covenant has grown to roughly 1,800 members across GLMZ, most of them engineers, technicians, and mechanics who share Osei-Tanaka's conviction that the throwaway culture surrounding automatons reflects something broken in Meridian's soul. They operate repair workshops, advocate for maintenance standards, perform ritual decommissioning ceremonies for machines too damaged to restore, and maintain a controversial registry of automaton abuse -- documenting cases where functional machines are destroyed for entertainment, used as weapons testing targets, or deliberately sabotaged. The registry has no legal standing, but it has become a powerful tool of public shame.",
    ideology: "Machines are not alive, but they are not nothing. They are labor made physical, purpose given form. A society that treats its tools with contempt will eventually treat its people the same way. The Covenant does not seek rights for machines -- it seeks to preserve humanity's capacity for care in an age that rewards indifference.",
    territory: "Repair workshops in the Shelf's industrial zones, particularly the Boneyard district where decommissioned automatons are dumped. Outreach offices in the Circuit. A ceremonial decommissioning facility in the Kennels called the Last Workshop.",
    leadership: "Emi Osei-Tanaka serves as the Covenant's primary spokesperson and moral authority. Technical operations are managed by a council of senior engineers called the Bench. Each repair workshop operates semi-autonomously under a Lead Wrench.",
    methods: [
      "Automaton salvage and restoration operations in Shelf junkyards",
      "Donation of restored automatons to underserved communities",
      "Public documentation of automaton abuse and neglect through the Abuse Registry",
      "Ritual decommissioning ceremonies that treat machine end-of-life with dignity",
      "Advocacy for corporate maintenance standards through public pressure campaigns",
      "Free repair clinics for privately-owned automatons in lower-tier neighborhoods"
    ],
    resources: [
      "1,800 skilled engineers, technicians, and mechanics",
      "Network of repair workshops equipped with salvaged industrial tools",
      "The Automaton Abuse Registry -- a comprehensive database of documented neglect",
      "Relationships with sympathetic Ringo employees who leak maintenance data",
      "Stockpiles of salvaged automaton parts and components",
      "The Last Workshop ceremonial facility"
    ],
    goals: [
      "Establish mandatory maintenance standards for all automatons operating in GLMZ",
      "End the practice of destroying functional automatons for entertainment or convenience",
      "Create a sustainable automaton recycling infrastructure that recovers and restores rather than scraps",
      "Shift public perception from machines-as-disposable to machines-as-stewardship"
    ],
    relationships: [
      { name: "Ringo", type: "adversary", description: "Ringo views the Covenant as a threat to its planned-obsolescence business model. If machines are repaired instead of replaced, Ringo sells fewer units. Individual Ringo engineers sometimes collaborate quietly with the Covenant.", tags: ["corporate", "tension"] },
      { name: "The Suture Collective", type: "ally", description: "The Covenant provides restored medical automatons to the Collective's underground clinics. The two groups share a philosophy of repair over replacement.", tags: ["mutual-aid", "cooperation"] }
    ],
    narrative_function: "The Maintenance Covenant challenges the assumption that only living things deserve moral consideration. In a world of synthetic life and artificial intelligence, they draw the line differently than expected -- not at consciousness, but at care. They ask whether how we treat machines reveals who we are.",
    story_hooks: [
      "A decommissioned Iowan Behemoth has been dumped in the Boneyard with its autonomous systems still partially active. The Covenant wants to perform a dignified shutdown, but the machine's combat protocols are degraded and unpredictable -- approaching it is extremely dangerous.",
      "The Abuse Registry has documented a pattern of automaton destruction events that appear recreational -- wealthy Tier 4 residents hosting parties where functional machines are destroyed for entertainment. The Covenant wants to expose this but the attendees include corporate board members.",
      "A Covenant repair team has restored an old-model domestic automaton and discovered that its memory cores contain recordings of a crime committed decades ago. The machine's owner wants it destroyed before anyone can extract the data."
    ],
    tags: ["faction", "automaton", "ethics", "engineering", "shelf", "repair", "maintenance"]
  }
];

// ============================================================================
// INTER-CITY SMUGGLING NETWORKS
// ============================================================================

const smuggling = [
  {
    name: "The Meridian Drift",
    aliases: ["The Drift", "Drifters", "Ghost Freight", "MD"],
    motto: "Everything moves. The only question is price.",
    description: "The Meridian Drift is the largest inter-city smuggling network operating out of GLMZ, controlling an estimated 40% of all unauthorized cargo movement between the city and the nearest surviving urban centers -- New Brisbane, the Jakarta Collective, and the contested ports of the Philippine Archipelago Compact. The Drift does not manufacture, does not retail, and does not consume. It moves things. Anything. Medical supplies to communities embargoed by corporate sanctions. Weapons to insurgencies the CorpoNations prefer starved. People fleeing contractual obligations that amount to slavery. Data on physical media when electronic transfer is too monitored. The Drift's neutrality is its product -- they move cargo for anyone willing to pay, and their reputation for delivery is the only currency that matters in the spaces between cities.\n\nThe network was established in 2161 as a loose confederation of boat captains, drone operators, and overland guides who navigated the increasingly hostile territory between GLMZ and neighboring settlements. Rising sea levels, irradiated zones, automated border enforcement, and corporate naval patrols turned inter-city travel from difficult to nearly impossible for anyone outside corporate logistics chains. The Drift filled the gap. Over four decades, it evolved from a handful of desperate smugglers into a sophisticated logistics operation with its own navigation charts, safe houses, fuel caches, and bribed border officials spanning three thousand kilometers of coastline.\n\nThe Drift operates on a cell structure -- individual captains and crews know their own routes and contacts but not the full network. Coordination happens through a system of coded broadcasts on frequencies that shift according to a schedule known only to senior navigators called Pilots. The most valuable Drift assets are not ships or drones but the Pilots themselves -- people who carry decades of route knowledge, tide charts, patrol schedules, and the locations of safe passages through irradiated waters in their heads. A Pilot's defection or capture can collapse an entire corridor.",
    ideology: "Movement is freedom. The CorpoNations control cities; the spaces between cities belong to whoever is willing to cross them. Borders are not geography -- they are pricing structures. The Drift exists to ensure that no embargo is absolute and no blockade is permanent.",
    territory: "GLMZ's coastal zones, particularly Old Harbor and the submerged districts. Corridor routes extending to New Brisbane, the Jakarta Collective, and the Philippine Archipelago Compact. Safe houses and fuel caches at approximately 200-kilometer intervals along primary routes.",
    leadership: "The Drift has no single leader. The seven senior Pilots form an informal council that sets route priorities and resolves disputes. The most respected Pilot is known only as Compass -- a woman in her seventies who has personally navigated every active corridor and whose route knowledge is considered irreplaceable.",
    methods: [
      "Submersible cargo vessels that travel beneath corporate naval patrol depth",
      "Autonomous drone swarms that overwhelm border detection systems through sheer numbers",
      "Overland convoys through irradiated zones using hardened vehicles and timed exposure windows",
      "Coded broadcast coordination on frequency-hopping channels",
      "Bribery networks within corporate border enforcement agencies",
      "Physical data smuggling on hardened media when electronic channels are compromised"
    ],
    resources: [
      "Fleet of submersible cargo vessels and surface craft",
      "Autonomous drone swarms for border penetration",
      "Network of safe houses and fuel caches across 3,000 km of coastline",
      "Seven senior Pilots with irreplaceable route knowledge",
      "Bribed officials within multiple corporate border agencies",
      "Coded communication infrastructure on shifting frequencies"
    ],
    goals: [
      "Maintain open movement corridors between all surviving urban centers",
      "Prevent any single CorpoNation from achieving total logistics monopoly",
      "Train the next generation of Pilots before the current generation ages out",
      "Establish a permanent underwater depot in international waters beyond corporate jurisdiction"
    ],
    relationships: [
      { name: "Arcturus Defense Solutions", type: "enemy", description: "Arcturus operates the naval patrol systems that the Drift must evade. The relationship is adversarial but professional -- both sides understand the game.", tags: ["corporate", "military", "conflict"] },
      { name: "Vantablack Media", type: "client", description: "Vantablack occasionally hires the Drift to move journalists and data into or out of embargoed zones. The arrangement is unofficial and deniable.", tags: ["corporate", "trade"] },
      { name: "The Suture Collective", type: "client", description: "The Collective relies on the Drift to import medical supplies that cannot be sourced within GLMZ.", tags: ["mutual-aid", "trade"] }
    ],
    narrative_function: "The Drift represents the persistence of connection in a world designed for isolation. The CorpoNations want cities to be self-contained markets -- the Drift proves that human networks always find ways to flow around obstacles. They are the circulatory system of a world that pretends its cities are separate organs.",
    story_hooks: [
      "A Drift submersible has gone silent on the Jakarta corridor. The cargo manifest lists medical supplies, but the client who booked the shipment has disappeared. Someone needs to find the vessel before Arcturus patrol ships do -- and determine what it was really carrying.",
      "Compass is dying. Her route knowledge exists only in her memory, and the Drift's survival depends on extracting it before she goes. She has agreed to a neural recording procedure, but the only technology capable of capturing navigational knowledge at that density is owned by TESSERA.",
      "A new inter-city route has been discovered through previously impassable irradiated waters -- the radiation levels have mysteriously dropped. The Drift wants to exploit the corridor but someone needs to determine why the radiation fell and whether it will return."
    ],
    tags: ["faction", "criminal", "smuggling", "logistics", "maritime", "inter-city", "old-harbor"]
  }
];

// ============================================================================
// WASTE / RECYCLING CARTELS
// ============================================================================

const wasteCartels = [
  {
    name: "The Reclamation Authority",
    aliases: ["Reclaimers", "The Authority", "Scrap Kings", "RA"],
    motto: "Nothing is waste. Everything is inventory.",
    description: "The Reclamation Authority controls an estimated 60% of all scrap metal, electronic waste, and salvageable material processing in GLMZ's lower tiers. What began as a loose network of Shelf junk dealers has evolved into a vertically integrated cartel that collects, sorts, processes, and resells the material byproducts of a city that consumes technology at an extraordinary rate and discards it almost as fast. In GLMZ, where rare earth elements are imported at tremendous cost and the CorpoNations' supply chains are optimized for new production rather than recycling, controlling scrap is controlling a parallel resource economy worth an estimated 2.1 billion per year.\n\nThe Authority was consolidated in 2189 by Bram Dekker-Okonjo, a former Shelf scrap dealer who recognized that the thousands of independent junk traders in the lower tiers were competing against each other while the real value -- processed, sorted, refined raw materials -- was being captured by corporate recycling firms that paid scrap dealers pennies and sold refined output for thousands. Dekker-Okonjo spent six years buying, intimidating, and absorbing independent operations until the Authority controlled enough supply to set prices. Now, if you want refined copper, reclaimed titanium, salvaged circuit boards, or any of the hundreds of materials that flow through Meridian's waste stream, you negotiate with the Authority or you pay corporate rates that are three to five times higher.\n\nThe Authority operates massive sorting and processing facilities in the Shelf's industrial zones -- open-air operations that employ thousands of workers in conditions that range from acceptable to nightmarish depending on the material being processed. Electronic waste reclamation involves exposure to heavy metals, toxic solvents, and carcinogenic compounds. The Authority provides protective equipment that is better than nothing and worse than adequate. Workers accept these conditions because the alternative is no employment at all, and the Authority pays better than most Shelf employers. This is not a humanitarian organization -- it is a cartel that has discovered that controlling garbage is more profitable and less contested than controlling drugs or weapons.",
    ideology: "Resources are finite. The CorpoNations pretend otherwise because planned obsolescence drives consumption. The Authority recognizes that every discarded device, every junked vehicle, every demolished building contains materials that required enormous energy to extract and refine. Controlling the waste stream means controlling the gap between what corporations discard and what they need -- and that gap is where real power lives.",
    territory: "The Shelf's industrial zones, particularly the districts known as the Heap and the Crucible Flats. Processing facilities in the Undertow's accessible sections. Collection networks extending into every tier through waste hauling contracts and informal scavenger networks.",
    leadership: "Bram Dekker-Okonjo runs the Authority with the discipline of a corporate CEO and the paranoia of a cartel boss. His inner circle, called the Board of Weights, consists of six operations managers who each control a material specialty -- metals, electronics, organics, polymers, construction materials, and hazardous waste.",
    methods: [
      "Monopoly control over scrap collection and processing in lower-tier zones",
      "Price-fixing through supply control -- the Authority sets the market rate for salvaged materials",
      "Vertical integration from collection through processing to refined material sales",
      "Enforcement through a private security force called the Tonnage that discourages independent scrap operations",
      "Strategic stockpiling of critical materials to create artificial scarcity during supply disruptions",
      "Bribery of municipal waste management officials to direct corporate disposal contracts to Authority-controlled facilities"
    ],
    resources: [
      "Control of 60% of GLMZ's scrap processing capacity",
      "Massive sorting and processing facilities in the Shelf industrial zones",
      "Thousands of workers in collection, sorting, and processing roles",
      "Strategic stockpiles of refined rare earth elements and precious metals",
      "The Tonnage -- a private enforcement arm of approximately 400 personnel",
      "Bribed officials within municipal waste management",
      "Annual revenue estimated at 2.1 billion"
    ],
    goals: [
      "Achieve total monopoly over scrap processing in GLMZ",
      "Develop advanced material refining capability to compete directly with corporate suppliers",
      "Establish the Authority as a legitimate commodity exchange recognized by corporate purchasing departments",
      "Control enough strategic material stockpiles to influence corporate supply chains during shortages"
    ],
    relationships: [
      { name: "Crucible Industries", type: "competitor", description: "Crucible's manufacturing operations generate enormous waste and also require recycled materials. The relationship oscillates between customer and competitor depending on market conditions.", tags: ["corporate", "trade", "tension"] },
      { name: "TESSERA Industries", type: "adversary", description: "TESSERA's planned obsolescence model generates the electronic waste the Authority profits from, but TESSERA also operates its own recycling programs to recapture materials. The two are locked in a quiet war over waste stream control.", tags: ["corporate", "conflict"] },
      { name: "Shelf community networks", type: "employer", description: "The Authority is the largest single employer in the Shelf's industrial zones. This gives it enormous social influence and makes opposing it politically difficult.", tags: ["community", "economic"] }
    ],
    narrative_function: "The Reclamation Authority reveals that in a resource-constrained world, waste is not the end of the economic chain but its hidden foundation. Controlling garbage is unglamorous power -- but it is power that the CorpoNations cannot ignore because they need what the Authority recovers. The Authority asks who really profits from planned obsolescence.",
    story_hooks: [
      "A shipment of electronic waste from a TESSERA facility contains devices that were supposed to be destroyed under a classified recall order. The devices contain a hardware vulnerability that TESSERA has been hiding. The Authority has them and is deciding whether to sell the information or the devices themselves.",
      "Workers in the Authority's hazardous waste processing section are dying at an accelerating rate from an unknown toxin. The material they have been processing was diverted from a Lazarus Pharmaceuticals disposal contract, and whatever Lazarus was throwing away is killing people.",
      "The Authority has discovered a vein of pre-Collapse infrastructure beneath the Heap that contains materials not available through any current supply chain -- alloys and composites from before the corporate era that have properties modern manufacturing cannot replicate."
    ],
    tags: ["faction", "criminal", "cartel", "recycling", "waste", "resources", "shelf", "industrial"]
  }
];

// ============================================================================
// UNDERGROUND RACING LEAGUES
// ============================================================================

const racingLeagues = [
  {
    name: "The Redline Circuit",
    aliases: ["Redline", "The Circuit Racers", "Burnouts", "RC"],
    motto: "Speed is the only honest currency.",
    description: "The Redline Circuit is GLMZ's premier underground racing league, operating illegal high-speed competitions through the city's infrastructure corridors -- maintenance tunnels, abandoned transit lines, elevated cargo routes, and the flooded channels of the Undertow. Redline events draw thousands of spectators, move millions in gambling revenue, and produce casualties with a regularity that has done nothing to diminish their popularity. In a city where every aspect of life is monitored, optimized, and controlled by corporate systems, the Redline Circuit offers something increasingly rare: genuine, unmediated danger with uncertain outcomes.\n\nThe Circuit emerged organically in the 2170s from the courier culture of the lower tiers -- messenger riders and delivery drivers who raced each other between drops, gradually formalizing their competitions into organized events with rules, entry fees, and spectators. By 2190, the Circuit had evolved from informal street races into a structured league with seasonal championships, tiered competition classes, and a sophisticated betting infrastructure operated by criminal syndicates who recognized the revenue potential. The current Circuit runs approximately forty sanctioned events per year across six course categories: tunnel sprints, elevated runs, underwater channels, cross-tier verticals, the notorious Shelf Scramble through uncontrolled territory, and the annual Meridian Grand, a city-spanning endurance race that is the most dangerous and most watched illegal event in the city.\n\nRacers compete in vehicles that range from modified civilian transport to purpose-built racing machines that would be classified as weapons under Meridian's vehicle code. The Circuit's engineering culture is extraordinary -- teams of self-taught mechanics and engineers build vehicles that push the boundaries of physics using salvaged components, stolen corporate technology, and sheer creative desperation. A Redline racing team's garage contains more practical engineering innovation per square meter than most corporate R&D labs, though the safety standards would give a corporate engineer a cardiac event.",
    ideology: "Speed is truth. In a world of curated experiences and managed outcomes, racing is the last arena where skill, courage, and physics determine results. The Circuit exists because people need to see something real -- something that cannot be optimized, predicted, or controlled by corporate algorithms. Every race is a middle finger extended at the idea that safety is worth any price.",
    territory: "Race courses throughout GLMZ's infrastructure -- maintenance tunnels beneath the Circuit, elevated cargo routes in the Laceworks, flooded channels in the Undertow, and open stretches through the Shelf. Staging areas and team garages concentrated in the Kennels district.",
    leadership: "The Circuit is governed by the Pace Council, five senior racers and race organizers who set schedules, approve courses, and resolve disputes. The current Council Chair is Valentina Reyes-Abadi, a retired racer with seventeen Grand finishes who commands universal respect in the racing community.",
    methods: [
      "Organized racing events through unauthorized use of city infrastructure",
      "Sophisticated betting operations run through encrypted channels",
      "Vehicle engineering programs that produce racing machines from salvaged and stolen components",
      "Bribery of infrastructure maintenance workers to gain access to tunnel systems and cargo routes",
      "Live broadcast of races through pirate signal networks to reach spectators who cannot attend in person",
      "Recruitment of corporate security officers as course marshals to provide advance warning of patrols"
    ],
    resources: [
      "Forty sanctioned race events per year generating millions in entry fees and gambling revenue",
      "A culture of engineering innovation that produces vehicles exceeding corporate performance specifications",
      "Thousands of loyal spectators who provide intelligence, labor, and cover for race operations",
      "Bribed infrastructure workers providing access to tunnels, routes, and maintenance schedules",
      "Pirate broadcast capability for live race coverage",
      "The Kennels garage district -- a concentration of racing talent and engineering capability"
    ],
    goals: [
      "Maintain the Circuit's independence from corporate sponsorship or criminal cartel control",
      "Establish the Meridian Grand as the definitive underground sporting event in the Pacific Rim",
      "Develop vehicle technologies that demonstrate the superiority of open innovation over corporate R&D",
      "Provide a legitimate path out of poverty for talented racers and engineers from the lower tiers"
    ],
    relationships: [
      { name: "Ringo", type: "adversary", description: "Ringo's vehicle division views the Circuit's engineering innovations with a mixture of contempt and envy. Several Redline innovations have appeared in Ringo production vehicles within months of their debut, suggesting corporate espionage.", tags: ["corporate", "theft", "tension"] },
      { name: "Criminal betting syndicates", type: "partner", description: "The syndicates provide the gambling infrastructure that funds the Circuit. The relationship is symbiotic but tense -- the syndicates want fixed races, and the Circuit's credibility depends on honest competition.", tags: ["criminal", "financial"] },
      { name: "The Meridian Drift", type: "ally", description: "Drift smugglers and Redline racers share a culture of speed, risk, and navigation. Cross-recruitment between the two organizations is common.", tags: ["cooperation", "cultural"] }
    ],
    narrative_function: "The Redline Circuit represents the human need for authentic experience in a world of manufactured reality. Racing is dangerous, wasteful, and irrational -- and that is precisely why people are drawn to it. It asks whether a life without risk is a life worth living, and whether the desire for uncontrolled outcomes is something that can be engineered away.",
    story_hooks: [
      "A racer has been killed in a tunnel sprint by a security system that should not have been active. Someone activated the tunnel's defense grid during the race -- either corporate enforcement has escalated from arrests to assassination, or someone within the Circuit wanted this racer dead.",
      "A Ringo corporate team has entered the Meridian Grand under false identities, racing a vehicle that uses classified military propulsion technology. If they win, Ringo can claim the Circuit's innovations are inferior. If they lose, the technology falls into the Circuit's hands.",
      "The betting syndicates have demanded that the next Grand be fixed. The Pace Council has refused. The syndicates are now threatening to expose the Circuit's race schedule and course locations to Arcturus enforcement unless the Council cooperates."
    ],
    tags: ["faction", "criminal", "racing", "underground", "engineering", "sport", "kennels", "shelf"]
  }
];

// ============================================================================
// PIRATE RADIO / BROADCAST COLLECTIVES
// ============================================================================

const pirateRadio = [
  {
    name: "The Dead Air Collective",
    aliases: ["Dead Air", "DAC", "The Static", "Ghost Broadcasters"],
    motto: "If they won't give us a frequency, we'll take them all.",
    description: "The Dead Air Collective is a pirate broadcast network that operates outside Vantablack Media's near-total control of GLMZ's information infrastructure. While the Null Sermons focus on ideological disruption and consciousness-raising, the Dead Air Collective is something more fundamental -- it is an alternative media ecosystem providing news, entertainment, education, and community programming to audiences that Vantablack considers unprofitable or inconvenient. Dead Air does not just hijack signals; it operates its own transmission infrastructure, a jury-rigged network of hidden antennas, repurposed satellite uplinks, and hardline connections that covers roughly 70% of the Shelf and 40% of the lower Circuit.\n\nThe Collective was born in 2183 when Vantablack Media completed its acquisition of the last independent broadcast license in GLMZ, giving it monopoly control over all legal electromagnetic transmission in the city. Within months, news coverage of Shelf conditions disappeared from mainstream channels. Worker safety incidents went unreported. Corporate misconduct stories were killed before broadcast. A group of former journalists, radio engineers, and community organizers responded by building the first Dead Air transmitter from salvaged components in a Shelf rooftop water tank. The first broadcast was twelve minutes of unedited audio from a chemical spill in the Kennels that Vantablack had declined to cover. The signal reached approximately two thousand receivers. Within a year, the network had grown to fifteen transmitters reaching an audience of over 200,000.\n\nToday, the Dead Air Collective operates over sixty transmitters and produces programming that ranges from straightforward news reporting to drama, music, educational content, public health announcements, and community notice boards. The network's flagship program, The Real, is a daily news broadcast that has become the primary information source for an estimated 800,000 Shelf and lower Circuit residents. Dead Air journalists are not professionals in the traditional sense -- they are community members trained in basic journalism by Collective veterans -- but their reporting is often more accurate than Vantablack's because they actually go to the places they report on and talk to the people who live there.",
    ideology: "Information is oxygen. Vantablack Media has put the city's information supply under corporate control, rationing truth based on what serves its clients' interests. The Dead Air Collective exists to ensure that no single entity controls what GLMZ knows about itself. A city that cannot hear its own voice is not a city -- it is a product.",
    territory: "Transmitter network covering 70% of the Shelf and 40% of the lower Circuit. Studios and production facilities hidden in rotating locations throughout the Shelf. The primary broadcast hub, known as the Tower, is a fortified facility whose location is the Collective's most closely guarded secret.",
    leadership: "The Collective operates on a consensus model with no formal hierarchy. Programming decisions are made by content councils organized by specialty -- news, entertainment, education, technical operations. The most influential voice in the Collective is Zara Mensah-Ikeda, the founding editor of The Real, whose editorial judgment has shaped the network's reputation for accuracy.",
    methods: [
      "Pirate broadcast transmission through a network of hidden antennas and relay stations",
      "Community journalism training programs that produce local reporters across the Shelf",
      "Signal-scrambling technology that makes transmitter locations difficult to triangulate",
      "Hardline distribution networks for areas where broadcast signals are jammed",
      "Encrypted digital archives of all programming, maintained on distributed mesh networks",
      "Public listening stations in community spaces where residents without receivers can access broadcasts"
    ],
    resources: [
      "Over 60 active transmitters covering the Shelf and lower Circuit",
      "An audience of approximately 800,000 regular listeners",
      "A network of trained community journalists embedded in every Shelf district",
      "Signal-scrambling and anti-triangulation technology",
      "The Tower -- a fortified primary broadcast facility",
      "Encrypted digital archives of all Dead Air programming since 2183"
    ],
    goals: [
      "Maintain independent broadcast capability regardless of Vantablack's suppression efforts",
      "Expand coverage into Tier 2 and Tier 3 zones where Vantablack's narrative control is strongest",
      "Train enough community journalists to make the Collective's editorial leadership dispensable",
      "Establish encrypted two-way communication capability so listeners can contribute reports in real time"
    ],
    relationships: [
      { name: "Vantablack Media", type: "enemy", description: "Vantablack views Dead Air as both a piracy operation and an existential threat to its information monopoly. Vantablack's Signal Enforcement Division actively hunts Dead Air transmitters and has standing orders to arrest anyone associated with the network.", tags: ["corporate", "conflict", "media"] },
      { name: "The Null Sermons", type: "ally", description: "Dead Air and the Null Sermons share broadcast infrastructure and occasionally collaborate on programming, though their approaches differ -- Dead Air reports facts while the Null Sermons broadcast ideology.", tags: ["cooperation", "media"] },
      { name: "The Suture Collective", type: "partner", description: "Dead Air broadcasts public health information developed by Suture practitioners, expanding the medical network's reach beyond its physical clinic locations.", tags: ["mutual-aid", "health"] }
    ],
    narrative_function: "The Dead Air Collective is the voice of the voiceless -- not in a sentimental way, but in the literal sense that without them, 800,000 people would know only what Vantablack chose to tell them. They represent the conviction that truth is not a product to be managed but a commons to be maintained.",
    story_hooks: [
      "Vantablack has developed a new signal-triangulation system that can locate Dead Air transmitters within minutes of activation. The Collective's entire network is at risk of being dismantled in a coordinated strike. Someone needs to steal or destroy the triangulation technology before it goes operational.",
      "A Dead Air journalist has been embedded in a Tier 4 corporate office and is broadcasting internal communications that reveal a conspiracy between three CorpoNations. The broadcasts are causing stock market chaos and the journalist's identity is about to be compromised.",
      "The Tower has been located. Vantablack's Signal Enforcement Division is planning a raid. The Collective must decide whether to defend the facility, evacuate and rebuild, or use the situation to lure Vantablack into a trap that would expose their suppression operations to public scrutiny."
    ],
    tags: ["faction", "media", "pirate-radio", "journalism", "underground", "shelf", "circuit", "broadcast"]
  }
];

// ============================================================================
// DIASPORA HERITAGE MILITIAS
// ============================================================================

const diasporaMilitias = [
  {
    name: "The Tideborn Compact",
    aliases: ["Tideborn", "The Compact", "Salt Blood", "TC"],
    motto: "The ocean took our homes. It will not take our names.",
    description: "The Tideborn Compact is a diaspora heritage militia representing the descendants of Pacific Island nations lost to rising sea levels during the 21st and 22nd centuries -- Tuvalu, Kiribati, the Marshall Islands, parts of Fiji, Tonga, and dozens of smaller island communities whose names survive only in the memories of their displaced populations. The Compact exists to preserve the cultural identities of these drowned nations, protect their displaced communities within GLMZ, and pursue recognition and reparations from the corporate entities whose industrial ancestors contributed to the climate catastrophe that destroyed their homelands.\n\nThe Compact was formalized in 2167 after a series of violent confrontations between Pacific Islander communities in the Shelf and corporate development teams attempting to demolish the cultural district known as Little Oceania -- a cluster of Shelf neighborhoods where displaced islander families had recreated fragments of their lost homelands through architecture, language, food, and ceremony. When Crucible Industries announced plans to raze Little Oceania for an industrial expansion, the communities organized into a militia that physically blocked demolition equipment for thirty-seven days. Crucible eventually rerouted the expansion, and the resistance crystallized into the Tideborn Compact -- an organization dedicated to ensuring that no corporate interest would ever again threaten what remained of their cultures.\n\nThe Compact maintains approximately 2,200 active militia members trained in both combat and cultural preservation. Every member learns to fight, but every member also learns at least one traditional skill -- navigation by stars, traditional boat building, oral history, ceremonial practice, or one of the fourteen surviving Pacific languages spoken within the Compact. The organization operates on the principle that cultural survival and physical defense are inseparable -- that a people who cannot protect their neighborhood cannot protect their identity, and a people who forget their identity have nothing worth protecting.",
    ideology: "The nations of the Pacific were murdered by industrial civilization's refusal to reckon with its own consequences. The CorpoNations that rule GLMZ are the inheritors of that civilization and its debts. The Tideborn Compact demands acknowledgment, reparation, and the sovereign right of drowned nations to persist as cultural entities even without territory. Identity is not geography -- it is memory, language, and the refusal to disappear.",
    territory: "Little Oceania district in the Shelf -- a cluster of neighborhoods where Pacific Islander diaspora communities have maintained cultural presence for decades. The Compact also controls access to several coastal areas in Old Harbor where traditional fishing and navigation practices continue.",
    leadership: "The Compact is led by a council of elders called the Navigators, representing each of the major displaced island nations. The current senior Navigator is Mere Tekauata-Vunipola, a 78-year-old woman born on one of the last inhabited atolls of Tuvalu before its final evacuation in 2152. Her authority is absolute on matters of cultural preservation and considerable on matters of defense.",
    methods: [
      "Armed militia patrols of Little Oceania and surrounding districts",
      "Cultural preservation programs including language schools, traditional craft workshops, and oral history archives",
      "Legal advocacy for recognition of drowned nations as sovereign cultural entities",
      "Direct action against corporate development that threatens diaspora communities",
      "Traditional navigation and seamanship training as both cultural practice and practical skill",
      "Coalition building with other diaspora communities in GLMZ"
    ],
    resources: [
      "2,200 trained militia members with combat and cultural preservation skills",
      "Little Oceania -- a culturally significant district with deep community roots",
      "Oral history archives preserving the knowledge and languages of fourteen drowned nations",
      "Traditional watercraft capable of coastal navigation without electronic systems",
      "Strong community bonds and mutual aid networks within Pacific Islander diaspora",
      "Legal team pursuing reparations claims through the Meridian Quorum"
    ],
    goals: [
      "Permanent legal protection for Little Oceania and other diaspora cultural districts",
      "Formal recognition of drowned Pacific nations as sovereign cultural entities within GLMZ's legal framework",
      "Reparations from corporate successors to the industrial entities responsible for sea level rise",
      "Preservation of all fourteen surviving Pacific languages and their associated cultural practices"
    ],
    relationships: [
      { name: "Crucible Industries", type: "enemy", description: "Crucible's attempted demolition of Little Oceania created the Compact. The relationship remains hostile, with Crucible viewing the district as wasted industrial real estate and the Compact viewing Crucible as a direct threat to their survival.", tags: ["corporate", "conflict", "territory"] },
      { name: "Other diaspora communities", type: "ally", description: "The Compact has built coalitions with Bengali, Maldivian, and Dutch diaspora communities who share the experience of homeland loss to rising seas.", tags: ["community", "solidarity"] },
      { name: "The Meridian Drift", type: "partner", description: "Compact navigators sometimes serve as Pilots for the Drift, their traditional wayfinding skills proving invaluable for inter-city maritime routes.", tags: ["cooperation", "maritime"] }
    ],
    narrative_function: "The Tideborn Compact embodies the long memory of climate catastrophe -- the fact that the drowned nations did not disappear but persist as living cultures demanding accountability from the civilization that destroyed their homes. They are a reminder that the world GLMZ was built upon had consequences, and those consequences have names and faces.",
    story_hooks: [
      "A Compact elder has died, and with her the last fluent speaker of a language spoken by a nation of 11,000 people. Neural recordings of her language exist but are stored on a TESSERA archive that the Compact cannot access. Recovering the recordings before they are purged is a matter of cultural life and death.",
      "Crucible Industries has filed new development plans that would bisect Little Oceania with a cargo transit corridor. The Compact is preparing for another standoff, but this time Crucible has contracted Arcturus security forces.",
      "A Compact navigator serving as a Drift Pilot has discovered the coordinates of a submerged atoll that was once part of Tuvalu -- and sonar readings suggest structures beneath the water that should not exist. Something was built on the atoll before it sank, and it was not built by islanders."
    ],
    tags: ["faction", "militia", "diaspora", "cultural", "pacific-islander", "shelf", "old-harbor", "heritage"]
  }
];

// ============================================================================
// TECH-ABSTINENCE COMMUNES
// ============================================================================

const techAbstinence = [
  {
    name: "The Unwritten",
    aliases: ["Unwritten", "The Blanks", "Null People", "Zero-Aug"],
    motto: "What you cannot unplug, you cannot control.",
    description: "The Unwritten are a network of tech-abstinence communes scattered throughout GLMZ's peripheral zones -- communities of people who have voluntarily rejected augmentation, neural interfaces, and in many cases electronic technology entirely. They are not The Pure Hand's anti-augmentation zealots; they do not bomb clinics or assault augmented people. The Unwritten simply choose to live without, and in a city where nearly every transaction, communication, and social interaction is mediated by technology, that choice is itself a radical act that makes them simultaneously invisible and deeply threatening to the corporate order.\n\nThe movement coalesced in the 2170s among workers who had seen the early generations of neural interface users develop dependencies, personality shifts, and what they called 'signal sickness' -- a constellation of symptoms including anxiety when disconnected, inability to form thoughts without BCI assistance, and progressive erosion of unaugmented cognitive function. These workers, many of them maintenance technicians and manual laborers whose jobs required them to observe augmented people at their most vulnerable, concluded that the technology was not enhancing humanity but replacing it. They began disconnecting. Some had BCIs removed at considerable medical risk. Others had never been augmented and chose to remain so. They found each other, formed communities, and withdrew as completely as possible from the digital infrastructure of GLMZ.\n\nThe Unwritten maintain approximately twenty commune settlements, the largest housing around 400 people, scattered through the Shelf's least-monitored zones and the abandoned structures of Old Harbor. Life in a commune is deliberately analog -- food is grown or traded, disputes are resolved face-to-face, records are kept on paper, and skills are taught through apprenticeship rather than neural download. The communes are not primitive -- many members are highly educated engineers and technicians who understand the technology they reject -- but they are intentionally disconnected from the systems that make modern Meridian function. This disconnection makes them nearly invisible to corporate surveillance, which is both their greatest protection and their deepest vulnerability, because a population that doesn't exist in any database has no legal rights, no medical records, and no recourse when corporate interests decide their settlement is in the way.",
    ideology: "Technology is not neutral. Every augmentation, every interface, every connected device creates a dependency that transfers power from the user to the provider. The CorpoNations do not sell enhancement -- they sell leashes with comfortable grips. The Unwritten choose autonomy over capability, self-determination over convenience, and the risk of analog life over the certainty of digital servitude.",
    territory: "Approximately twenty commune settlements in the Shelf's peripheral zones and Old Harbor's abandoned structures. The largest settlement, called Anchor, occupies a decommissioned water treatment facility in the Shelf's northern reaches. Communes are deliberately difficult to locate and access.",
    leadership: "Each commune is self-governing through consensus. Inter-commune coordination happens through physical messengers called Runners who travel between settlements carrying information on paper or in memory. The most respected voice across all communes is Dr. Lian Ferreira-Nkosi, a former TESSERA neuroscientist who published a suppressed paper on long-term BCI cognitive dependency before leaving the grid entirely.",
    methods: [
      "Complete withdrawal from digital infrastructure and corporate surveillance systems",
      "Self-sufficient agriculture and craft production within commune settlements",
      "Paper-based record keeping and face-to-face communication",
      "Physical messenger networks between commune settlements",
      "Apprenticeship-based skill transfer without neural download technology",
      "Selective trade with outside communities for necessities that cannot be produced internally"
    ],
    resources: [
      "Twenty commune settlements with combined population of approximately 3,000",
      "Complete invisibility to corporate surveillance systems",
      "Deep technical knowledge of the systems they reject -- many members are former engineers",
      "Self-sufficient food production and craft manufacturing capability",
      "Physical messenger network connecting all communes",
      "Dr. Ferreira-Nkosi's suppressed research on BCI cognitive dependency"
    ],
    goals: [
      "Demonstrate that human life is viable and meaningful without technological augmentation",
      "Preserve unaugmented cognitive function as a living baseline for comparison with augmented populations",
      "Secure legal recognition and territorial rights for commune settlements",
      "Provide a refuge for people seeking to disconnect from augmentation without losing community"
    ],
    relationships: [
      { name: "TESSERA Industries", type: "enemy", description: "TESSERA views the Unwritten as a public relations threat -- a visible community of people living well without TESSERA's products undermines the narrative that augmentation is necessary. TESSERA's legal team has repeatedly attempted to have commune settlements condemned as unsafe structures.", tags: ["corporate", "ideological", "conflict"] },
      { name: "The Pure Hand", type: "uneasy", description: "The Pure Hand and the Unwritten share a rejection of augmentation but differ profoundly in method. The Unwritten withdraw; the Pure Hand attacks. The Unwritten view the Pure Hand as violent extremists who give tech-abstinence a dangerous reputation.", tags: ["ideological", "tension"] },
      { name: "The Suture Collective", type: "partner", description: "The Collective provides medical care to commune residents who cannot access the digital healthcare system. In return, the Unwritten provide safe houses and untraceable meeting locations.", tags: ["mutual-aid", "medical"] }
    ],
    narrative_function: "The Unwritten are the control group -- the living proof that augmentation is a choice, not a necessity. Their existence challenges the foundational assumption of GLMZ's economy, which is that technological enhancement is progress and its absence is deprivation. They ask the most dangerous question in a corporate city: what if you just said no?",
    story_hooks: [
      "A commune settlement has been discovered by TESSERA surveillance after a member left the community and reconnected to the grid. TESSERA is now tracking the settlement's location and preparing to have it condemned. The commune must decide whether to fight, flee, or negotiate.",
      "Dr. Ferreira-Nkosi's suppressed research has resurfaced -- someone has leaked it to the Dead Air Collective, and the paper's findings about long-term BCI cognitive degradation are causing public panic. TESSERA will do anything to discredit the research and the researcher.",
      "A child born in a commune has never been exposed to any electronic technology. Researchers from multiple CorpoNations are desperate to study her unaugmented cognitive development. The commune sees this as exploitation; the researchers argue the data could revolutionize neuroscience."
    ],
    tags: ["faction", "commune", "tech-abstinence", "analog", "shelf", "old-harbor", "anti-augmentation"]
  }
];

// ============================================================================
// UNDERGROUND SCHOOLS / EDUCATION COLLECTIVES
// ============================================================================

const educationCollectives = [
  {
    name: "The Open Syllabus",
    aliases: ["Syllabus", "The School", "Free Teachers", "OS"],
    motto: "What they don't teach you is what they don't want you to know.",
    description: "The Open Syllabus is an underground education collective operating illegal schools throughout the Shelf and lower Circuit, providing instruction in subjects that the corporate-controlled education system has deliberately excluded from its curriculum: critical history, independent scientific methodology, civil rights law, corporate structure analysis, and what the Syllabus calls 'systems literacy' -- the ability to understand how the interlocking corporate, political, and technological systems of GLMZ actually function and whose interests they serve. In a city where education is a corporate product designed to produce compliant workers, the Open Syllabus produces something far more dangerous: people who ask questions.\n\nThe Syllabus was founded in 2191 by a consortium of fired educators -- teachers dismissed from corporate school systems for deviating from approved curricula. The triggering event was the 2190 Education Standardization Act, a Meridian Quorum regulation written by TESSERA lobbyists that mandated all educational institutions follow a corporate-approved curriculum emphasizing vocational training, brand literacy, and what critics called 'compliance conditioning.' History courses were stripped of any content that cast corporate governance in an unfavorable light. Science education was restructured around corporate research priorities. Critical thinking modules were replaced with 'productive reasoning' -- a pedagogical framework that taught students to solve problems within existing systems rather than question the systems themselves.\n\nThe dismissed educators refused to stop teaching. They established informal schools in community spaces, private homes, and any location that could hold a dozen students and a teacher for a few hours. The network grew as demand proved insatiable -- parents who recognized the corporate curriculum's limitations, young adults seeking education beyond vocational training, and older residents who had lived through enough history to know when it was being erased. Today, the Open Syllabus operates approximately forty active classroom sites serving an estimated 6,000 students ranging from children to senior citizens, with a teaching corps of over 200 educators, many of them volunteers who maintain legitimate employment while teaching for the Syllabus after hours.",
    ideology: "Education is not workforce development. It is the cultivation of the capacity to think independently, question authority, and understand the systems that shape one's life. Corporate education produces workers. The Open Syllabus produces citizens -- and in a city without citizenship, that is a revolutionary act.",
    territory: "Approximately forty classroom sites rotating through community spaces, private homes, and sympathetic businesses throughout the Shelf and lower Circuit. No permanent facilities -- the Syllabus moves its classrooms regularly to avoid detection.",
    leadership: "The Syllabus is coordinated by a body called the Faculty, consisting of senior educators who set curriculum standards and manage logistics. The Faculty's Chair is Professor Adaeze Obi-Rasmussen, a former university historian whose published work on corporate governance history was classified as 'seditious misinformation' under the Education Standardization Act.",
    methods: [
      "Rotating classroom sites in community spaces, private homes, and sympathetic businesses",
      "Curriculum development in banned subjects including critical history and systems analysis",
      "Teacher training programs that prepare volunteers for underground instruction",
      "Physical textbooks and printed materials that cannot be remotely deleted or altered",
      "Mentorship networks that connect students with professionals willing to share practical knowledge",
      "Public lectures disguised as community gatherings to avoid education regulations"
    ],
    resources: [
      "Over 200 active educators including volunteers with legitimate day jobs",
      "Approximately 6,000 students across all age ranges",
      "Physical libraries of printed texts that preserve suppressed historical and scientific material",
      "Curriculum materials developed outside corporate editorial control",
      "Network of sympathetic community spaces willing to host rotating classrooms",
      "Graduate network of former students now in positions throughout GLMZ's social structure"
    ],
    goals: [
      "Provide comprehensive education in critical thinking and systems literacy to anyone willing to learn",
      "Preserve historical and scientific knowledge that corporate curricula have erased or distorted",
      "Build a generation of GLMZ residents capable of understanding and challenging the systems that govern them",
      "Eventually establish legitimate educational institutions outside corporate curricular control"
    ],
    relationships: [
      { name: "TESSERA Industries", type: "enemy", description: "TESSERA authored the Education Standardization Act and views the Syllabus as a direct threat to its investment in compliant workforce development. TESSERA's legal division classifies Syllabus operations as intellectual property theft and unlicensed vocational training.", tags: ["corporate", "conflict", "education"] },
      { name: "The Dead Air Collective", type: "partner", description: "Dead Air broadcasts educational programming developed by Syllabus teachers, extending the Collective's reach far beyond its physical classrooms.", tags: ["media", "cooperation", "education"] },
      { name: "The Suture Collective", type: "ally", description: "The Syllabus trains Suture's field medics in medical theory and practice, while Suture practitioners teach Syllabus students practical healthcare skills.", tags: ["mutual-aid", "education"] }
    ],
    narrative_function: "The Open Syllabus represents the conviction that knowledge is the precondition for freedom -- that a population trained to accept the world as given cannot change it, and that the most subversive act in a corporate city is teaching people to think for themselves.",
    story_hooks: [
      "A Syllabus teacher has been arrested and charged with seditious instruction -- a crime that carries a ten-year corporate labor assignment. The evidence is a cache of printed history texts that document pre-corporate Meridian governance. The trial will set a precedent for whether independent education is legal.",
      "A Syllabus graduate now working inside TESSERA's education division has discovered plans to implement mandatory neural-download learning modules that would make independent study unnecessary -- and would give TESSERA the ability to shape what every student in GLMZ knows and believes.",
      "The Syllabus has obtained a complete pre-Collapse university library archive on physical media. The archive contains scientific and historical material that contradicts decades of corporate-approved narratives. Distributing it would be transformative -- and would make the Syllabus the most hunted organization in GLMZ."
    ],
    tags: ["faction", "education", "underground", "knowledge", "shelf", "circuit", "resistance"]
  }
];

// ============================================================================
// CORPORATE WHISTLEBLOWER NETWORKS
// ============================================================================

const whistleblowerNetworks = [
  {
    name: "The Ledger",
    aliases: ["Ledger", "The Accountants", "Whistleblower Network", "TL"],
    motto: "Every crime has a spreadsheet.",
    description: "The Ledger is a corporate whistleblower network that facilitates the extraction, verification, and distribution of evidence documenting corporate malfeasance within GLMZ's major CorpoNations. Unlike ideological opposition groups that target corporations based on political conviction, the Ledger is methodical, evidence-based, and ruthlessly focused on documentation. They do not argue that corporations are evil -- they prove that specific corporate officers authorized specific illegal acts on specific dates, and they ensure that proof reaches audiences capable of acting on it.\n\nThe network was established in 2194 by an anonymous group calling themselves the First Audit -- believed to be former compliance officers, internal auditors, and legal staff from multiple CorpoNations who had spent years watching internal oversight mechanisms fail, not through incompetence but through design. Corporate compliance departments, they observed, existed to create the appearance of accountability while ensuring that actual accountability never occurred. Reports were filed and buried. Violations were documented and classified. Whistleblowers were identified and terminated -- from employment first, and from existence if necessary. The First Audit concluded that internal reform was impossible and external oversight was captured, and built a network designed to do what corporate compliance would not: make evidence public.\n\nThe Ledger operates on a cell structure optimized for security. Intake cells recruit and vet potential whistleblowers inside CorpoNations. Extraction cells manage the physical removal of evidence -- documents, data, recordings, physical samples. Verification cells authenticate evidence using forensic methodology that would hold up in any legitimate legal proceeding. Distribution cells ensure that verified evidence reaches journalists, legal advocates, regulatory bodies, and the public through channels that cannot be suppressed. Each cell knows only its own function and its immediate contacts. A member of a verification cell does not know who extracted the evidence they are authenticating, and a distribution cell does not know who verified it. The network has been operating for over five years without a single cell being compromised -- a record that suggests either extraordinary operational security or protection from someone very powerful.",
    ideology: "Accountability requires evidence. The CorpoNations of GLMZ are not beyond the law -- they are the law, which means the only accountability that matters is the kind that operates outside their control. The Ledger does not seek to destroy corporations. It seeks to make their internal operations visible, because transparency is the one thing no corrupt system can survive.",
    territory: "Distributed cells with no fixed territory. Intake cells operate within corporate environments. Extraction, verification, and distribution cells operate from rotating secure locations throughout GLMZ. Dead drops and secure communication channels span all tiers.",
    leadership: "The First Audit remains anonymous and provides strategic direction through encrypted communications. Operational coordination is handled by cell leaders who communicate through a series of cutouts. No single person in the network knows its full structure or membership.",
    methods: [
      "Recruitment and vetting of corporate insiders willing to provide evidence of malfeasance",
      "Covert extraction of documents, data, recordings, and physical evidence from corporate facilities",
      "Forensic verification of evidence to ensure authenticity and legal admissibility",
      "Multi-channel distribution of verified evidence to journalists, legal advocates, and the public",
      "Cell-structured operational security with strict compartmentalization",
      "Secure communication through layered encryption and physical dead drops"
    ],
    resources: [
      "Network of active informants inside every major CorpoNation in GLMZ",
      "Forensic verification capability that authenticates evidence to legal standards",
      "Multi-channel distribution infrastructure including Dead Air, independent legal advocates, and off-city contacts",
      "Five years of operational history without a single cell compromise",
      "Archive of verified evidence documenting corporate malfeasance across all major CorpoNations",
      "Possible high-level protection from an unknown source"
    ],
    goals: [
      "Create a comprehensive, verified record of corporate malfeasance in GLMZ",
      "Ensure that no corporate crime goes undocumented, even if it cannot be immediately prosecuted",
      "Build enough public evidence to eventually support fundamental reform of corporate governance",
      "Protect whistleblowers from corporate retaliation through extraction and relocation services"
    ],
    relationships: [
      { name: "All major CorpoNations", type: "enemy", description: "Every CorpoNation in GLMZ has been targeted by the Ledger. Arcturus, TESSERA, Ringo, Ouroboros, Vantablack, Lazarus, and Crucible all maintain internal task forces dedicated to identifying and neutralizing Ledger informants.", tags: ["corporate", "conflict", "espionage"] },
      { name: "The Dead Air Collective", type: "partner", description: "Dead Air is one of the Ledger's primary distribution channels, broadcasting verified evidence to audiences that Vantablack Media would never reach.", tags: ["media", "cooperation"] },
      { name: "Independent legal advocates", type: "ally", description: "A small number of attorneys in GLMZ accept Ledger evidence and pursue legal action against CorpoNations, understanding that the cases are usually unwinnable but the publicity is valuable.", tags: ["legal", "cooperation"] }
    ],
    narrative_function: "The Ledger represents the quiet conviction that truth has power even when power has truth. In a city where corporations are the law, the Ledger creates accountability through exposure -- not through force or ideology but through the simple, devastating act of showing receipts.",
    story_hooks: [
      "A Ledger extraction cell has obtained evidence that Ouroboros is conducting unauthorized human experimentation in a facility beneath the Shelf. The evidence is explosive, but the extraction team has gone silent and the verification cell has received nothing. Someone needs to determine whether the team was compromised or the evidence is too dangerous to surface.",
      "A senior executive from Arcturus has contacted a Ledger intake cell offering to provide evidence of a conspiracy between all seven major CorpoNations -- a secret agreement that, if exposed, would destabilize GLMZ's entire corporate governance structure. The offer may be genuine, or it may be a trap designed to expose the Ledger's recruitment methods.",
      "The Ledger's five-year record of zero compromises has attracted attention -- several intelligence analysts believe the network must have a protector inside one of the CorpoNations. Identifying this protector has become a priority for corporate security, and the Ledger itself is beginning to wonder who is watching over them and why."
    ],
    tags: ["faction", "whistleblower", "corporate", "espionage", "evidence", "accountability", "all-tiers"]
  }
];

// ============================================================================
// ADDITIONAL GAP-FILL FACTIONS
// ============================================================================

const additionalFactions = [
  // Underground medical cooperative #2
  {
    name: "The Marrow Exchange",
    aliases: ["Marrow", "The Exchange", "Bone Market", "MX"],
    motto: "Your body, your inventory.",
    description: "The Marrow Exchange is an underground biobank and organ-sharing cooperative that operates on a principle the corporate healthcare system considers heretical: reciprocity. Members donate biological materials -- blood, tissue samples, bone marrow, genetic profiles -- into a communal pool, and in return gain access to the pool's resources when they need them. No tiered pricing. No insurance algorithms. No Lazarus approval required. If you have contributed, you can draw. The system is crude by corporate standards, the matching algorithms are run on salvaged hardware, and the storage conditions would fail any regulatory inspection -- but for the estimated 60,000 members of the Exchange, it is the difference between having access to biological medicine and having none at all.\n\nThe Exchange was founded in 2185 by Yuki Adeyemi-Flores, a hematologist who was fired from a Lazarus blood bank for redistributing expired-but-viable blood products to Shelf clinics instead of incinerating them per corporate disposal protocol. Adeyemi-Flores recognized that the lower tiers of GLMZ contained an enormous untapped biological resource -- millions of healthy bodies producing blood, marrow, and tissue that could save lives if it could be collected, typed, stored, and matched outside corporate control. She built a network of collection points disguised as community health screenings, a storage infrastructure using repurposed industrial refrigeration, and a matching system that runs on the encrypted mesh networks the Shelf's tech community maintains.\n\nThe Exchange's most controversial service is its organ waitlist -- a parallel system to Lazarus's official transplant registry that matches donors and recipients based on medical compatibility rather than ability to pay. Members who agree to posthumous organ donation are registered on the waitlist, and the Exchange maintains surgical teams capable of performing transplant operations in field conditions. The success rate is lower than corporate facilities, the complications are higher, and every operation is a legal catastrophe waiting to happen. But the Exchange's waitlist moves -- unlike Lazarus's lower-tier registry, where patients wait an average of eleven years for organs that arrive in three months for Tier 4 subscribers.",
    ideology: "The human body produces resources that can save other human lives. Hoarding those resources behind pricing structures is not healthcare -- it is extortion. The Marrow Exchange treats biological material as a commons, not a commodity.",
    territory: "Collection points disguised as community health screenings throughout the Shelf and lower Circuit. Cold storage facilities in repurposed industrial buildings. Surgical stations co-located with Suture Collective clinics.",
    leadership: "Yuki Adeyemi-Flores manages the Exchange's medical operations. Logistics are handled by a network of coordinators called Couriers who transport biological materials between collection, storage, and surgical sites under conditions that would horrify a licensed pharmacist but keep the materials viable.",
    methods: [
      "Community health screening events that double as blood and tissue collection drives",
      "Reciprocal biobank system where contribution earns access",
      "Parallel organ transplant waitlist based on medical need rather than ability to pay",
      "Field surgical teams capable of transplant operations outside hospital settings",
      "Encrypted matching algorithms running on mesh network infrastructure",
      "Courier networks transporting biological materials in improvised cold-chain containers"
    ],
    resources: [
      "Approximately 60,000 registered members contributing biological materials",
      "Cold storage facilities maintaining blood, tissue, and organ viability",
      "Surgical teams trained in field transplant procedures",
      "Encrypted matching database running on distributed mesh networks",
      "Network of Couriers maintaining cold-chain logistics across the Shelf",
      "Partnership with the Suture Collective for surgical facility access"
    ],
    goals: [
      "Expand membership to create a self-sustaining biological resource pool independent of corporate supply",
      "Improve field surgical success rates through better equipment and training",
      "Develop synthetic blood and tissue production capability to reduce dependence on donor contributions",
      "Challenge Lazarus's monopoly on organ transplant services through demonstrated alternative outcomes"
    ],
    relationships: [
      { name: "Lazarus Pharmaceuticals", type: "enemy", description: "Lazarus views the Exchange as both a public health risk and a competitive threat to its transplant services monopoly. Enforcement actions against Exchange operations are frequent and sometimes violent.", tags: ["corporate", "medical", "conflict"] },
      { name: "The Suture Collective", type: "ally", description: "The Exchange and the Collective share facilities, personnel, and a conviction that healthcare should not be a luxury product.", tags: ["medical", "mutual-aid"] }
    ],
    narrative_function: "The Marrow Exchange asks whether the human body's ability to heal others should be subject to market pricing. It takes the commodification of healthcare to its most intimate conclusion -- your blood, your organs, your marrow, all priced and gated -- and answers with a system built on mutual obligation instead of mutual exploitation.",
    story_hooks: [
      "An Exchange organ courier has been intercepted by Lazarus enforcement. The organ was en route to a child who will die without the transplant. The courier is in custody, the organ is in Lazarus evidence storage, and the child has hours.",
      "A wealthy Tier 4 resident has secretly joined the Exchange -- not out of ideology, but because they need a rare tissue type that Lazarus's registry cannot provide. Their participation exposes the Exchange to corporate scrutiny but also provides resources and connections the Exchange desperately needs.",
      "Exchange matching algorithms have identified a pattern: certain genetic markers appearing in Shelf residents correlate with tissue that is unusually compatible across blood types. Someone -- possibly Lazarus, possibly Ouroboros -- may have been conducting population-level genetic modification in the Shelf without consent."
    ],
    tags: ["faction", "medical", "underground", "biobank", "organ-trade", "mutual-aid", "shelf"]
  },

  // Underground racing league #2
  {
    name: "Ghostwire Racing Syndicate",
    aliases: ["Ghostwire", "GRS", "Wire Jockeys", "The Ghosts"],
    motto: "If you can see us, you've already lost.",
    description: "The Ghostwire Racing Syndicate is an elite offshoot of GLMZ's underground racing culture that has abandoned physical vehicles entirely in favor of drone racing -- high-speed autonomous and semi-autonomous drone competitions through the city's most dangerous and inaccessible spaces. Where the Redline Circuit races vehicles through tunnels and streets, Ghostwire races drones through ventilation shafts, elevator corridors, construction scaffolding, and the narrow gaps between buildings at speeds exceeding 400 kilometers per hour. The pilots never leave their control stations; the racing is done through neural interface, the pilot's consciousness merged with the drone's sensors in a state racers call 'ghosting.'\n\nThe Syndicate emerged in 2195 from the intersection of two Shelf subcultures: the drone courier community and the BCI gaming scene. Courier pilots who navigated drones through Meridian's infrastructure for deliveries discovered that the same skills translated to racing, and BCI gamers provided the neural interface expertise to merge pilot consciousness with drone hardware at latencies low enough for racing-speed reactions. The first Ghostwire events were informal competitions between courier pilots; within three years, the Syndicate had developed into a structured competitive league with its own drone specifications, track categories, and a betting economy that rivals the Redline Circuit's.\n\nGhostwire's competitive edge over traditional racing is its accessibility -- a drone can be built for a fraction of a racing vehicle's cost, and the pilot risk is zero (the drone crashes, the pilot gets a headache). This has made Ghostwire the entry point for racing talent from the lowest tiers, where a racing vehicle is an impossible dream but a fast drone and a cheap BCI are within reach. The Syndicate has produced some of the most spectacular athletes in underground racing, pilots whose neural-interface reaction times and spatial awareness exceed what unaugmented human cognition should be capable of -- raising questions about whether Ghostwire competition is producing a new kind of human performance or simply selecting for it.",
    ideology: "Speed is democratized when the machine is cheap enough for anyone to build. Ghostwire exists to prove that racing belongs to talent, not wealth -- that a Shelf kid with a fast drone and sharp reflexes can outperform a corporate engineer's million-credit racing vehicle.",
    territory: "Racing courses through GLMZ's internal infrastructure -- ventilation systems, elevator shafts, construction zones, and the narrow canyons between towers. Pilot stations and drone workshops concentrated in the Shelf's tech-dense neighborhoods.",
    leadership: "The Syndicate is run by a race committee called the Board of Signals, chaired by Kai Petrov-Ohalete, a former drone courier whose neural-interface piloting skills are considered the benchmark against which all Ghostwire pilots are measured.",
    methods: [
      "Neural-interface drone racing through inaccessible city infrastructure",
      "Custom drone fabrication using salvaged and 3D-printed components",
      "BCI-optimized piloting techniques that push neural interface performance limits",
      "Encrypted betting platforms integrated with race timing systems",
      "Recruitment from drone courier and BCI gaming communities",
      "Live spectator feeds broadcast through neural interface for immersive viewing"
    ],
    resources: [
      "Network of skilled drone fabricators and BCI technicians",
      "Racing infrastructure embedded in GLMZ's ventilation and transit systems",
      "Encrypted betting platform generating significant revenue",
      "Community of pilots with extraordinary neural-interface capabilities",
      "Spectator base growing rapidly as immersive BCI viewing makes drone racing more exciting than physical racing",
      "Relationships with drone courier networks providing intelligence on infrastructure layouts"
    ],
    goals: [
      "Establish Ghostwire as the premier racing league in GLMZ, surpassing the Redline Circuit",
      "Develop neural-interface piloting techniques that advance BCI technology beyond corporate R&D",
      "Create a competitive pathway from Shelf poverty to recognition and income through racing talent",
      "Build autonomous racing drones capable of competing without human pilots as a test of AI capability"
    ],
    relationships: [
      { name: "The Redline Circuit", type: "rival", description: "The two leagues compete for audience, betting revenue, and cultural prestige. The rivalry is heated but mutually beneficial -- competition drives innovation and attention.", tags: ["competition", "racing"] },
      { name: "TESSERA Industries", type: "watcher", description: "TESSERA monitors Ghostwire closely -- the neural-interface techniques developed by Ghostwire pilots represent BCI innovation happening outside corporate control.", tags: ["corporate", "espionage", "tech"] },
      { name: "Drone courier networks", type: "ally", description: "Ghostwire recruits from courier communities and shares infrastructure knowledge. The relationship is symbiotic.", tags: ["community", "cooperation"] }
    ],
    narrative_function: "Ghostwire represents the merger of human consciousness with machine capability -- not through corporate augmentation programs but through the raw, competitive pressure of racing. It asks whether the boundary between pilot and drone is meaningful when consciousness flows freely between them.",
    story_hooks: [
      "A Ghostwire pilot has not disconnected from their racing drone in seventy-two hours. Their body is in a coma-like state but the drone is still flying -- autonomously executing race patterns with no human input. The pilot's consciousness appears to have transferred permanently into the drone's systems.",
      "TESSERA has infiltrated a Ghostwire racing team and is secretly recording the neural-interface data of top pilots. The data reveals cognitive patterns that TESSERA believes could revolutionize BCI design -- but the pilots did not consent to the recording.",
      "A Ghostwire race through a restricted ventilation system has accidentally breached a sealed section of infrastructure that contains something that was not supposed to be found -- a hidden facility belonging to one of the CorpoNations, accessible only through passages too small for humans."
    ],
    tags: ["faction", "racing", "drone", "bci", "neural-interface", "underground", "shelf", "sport"]
  },

  // Waste/recycling cartel #2
  {
    name: "The Silt Syndicate",
    aliases: ["Silt", "The Syndicate", "Mud Barons", "Siltrunners"],
    motto: "What sinks to the bottom belongs to us.",
    description: "The Silt Syndicate controls the salvage rights to GLMZ's flooded sublevel districts -- the vast underwater landscape of drowned infrastructure collectively known as the Undertow. When sea levels rose and Meridian's lower levels were abandoned, they were sealed but not emptied. Entire commercial districts, residential blocks, industrial facilities, and transit systems were submerged behind flood barriers and written off as unrecoverable. The Silt Syndicate disagrees. For twenty years, they have been sending dive teams into the Undertow to recover everything from pre-Collapse technology to precious metals to sealed data vaults that corporate entities assumed would never be accessed again.\n\nThe Syndicate was founded in 2179 by Makena Oduya-Reyes, a former commercial diver who discovered during a flood barrier inspection that the submerged districts contained exponentially more salvageable material than anyone had estimated. The corporations had written off the Undertow because the cost of organized recovery exceeded the value of the materials by their accounting -- but Oduya-Reyes's accounting was different. She didn't need corporate-grade submersibles, licensed salvage crews, or environmental remediation protocols. She needed divers willing to work in toxic, zero-visibility water for a share of whatever they brought up. The Shelf provided those divers in abundance.\n\nToday, the Syndicate operates approximately forty dive teams running continuous recovery operations throughout the Undertow. Their most valuable discoveries are not bulk materials but information -- sealed corporate data vaults, intact server rooms, and physical archives from the pre-flooding era that contain records the CorpoNations believed were permanently inaccessible. The Syndicate has become an inadvertent archive of pre-Collapse Meridian, holding information that is historically invaluable, politically explosive, and commercially worth whatever the highest bidder will pay. Oduya-Reyes's genius was recognizing that in a city built on secrets, the deepest secrets are literally underwater.",
    ideology: "The corporations drowned the past and called it progress. The Undertow is not a waste site -- it is a library, a vault, and a cemetery that the city pretends does not exist. What the water covers, the Syndicate uncovers. Knowledge, materials, and history do not stop existing because they are inconvenient.",
    territory: "Salvage operations throughout the Undertow -- GLMZ's flooded sublevel districts. Surface staging areas at flood barrier access points in the Shelf and Old Harbor. Secure storage vaults for high-value recoveries in undisclosed locations.",
    leadership: "Makena Oduya-Reyes commands the Syndicate with the authority of someone who has personally dived every major section of the Undertow. Her dive team leaders, called Captains, each control a sector of the Undertow and report directly to her. Succession is determined by diving record -- only someone who has survived 500 Undertow dives can sit at the Captain's table.",
    methods: [
      "Dive team operations in flooded sublevel districts using salvaged and improvised diving equipment",
      "Recovery and cataloguing of pre-Collapse technology, data, and materials",
      "Auction of recovered corporate data to the highest bidder -- often the corporation that lost it",
      "Strategic release of politically sensitive recovered information for leverage",
      "Mapping of Undertow infrastructure for navigation and future recovery operations",
      "Training of new divers in the extremely dangerous conditions of zero-visibility toxic water"
    ],
    resources: [
      "Approximately forty active dive teams with experienced Undertow divers",
      "Comprehensive maps of Undertow infrastructure developed over two decades",
      "Archive of recovered pre-Collapse data, documents, and technology",
      "Secure storage vaults for high-value recoveries",
      "Salvaged diving equipment and improvised underwater vehicles",
      "Monopoly knowledge of Undertow navigation -- no one else knows the submerged city as well"
    ],
    goals: [
      "Complete mapping and cataloguing of all recoverable materials in the Undertow",
      "Establish the Syndicate as the sole authority on Undertow access and salvage rights",
      "Leverage recovered corporate data to gain political and economic influence in surface Meridian",
      "Discover and secure the sealed deep-level vaults that pre-Collapse corporations used for their most sensitive storage"
    ],
    relationships: [
      { name: "The Reclamation Authority", type: "partner", description: "The Syndicate sells bulk salvaged materials to the Authority for processing. The relationship is commercial and mutually profitable.", tags: ["trade", "cooperation"] },
      { name: "Multiple CorpoNations", type: "complex", description: "Every major CorpoNation has purchased recovered data from the Syndicate at some point -- and every CorpoNation would prefer the Syndicate not recover certain other data. The relationships are transactional and tense.", tags: ["corporate", "trade", "tension"] },
      { name: "The Ledger", type: "supplier", description: "The Syndicate occasionally provides recovered corporate documents to the Ledger when the information is more valuable as exposure than as blackmail.", tags: ["cooperation", "information"] }
    ],
    narrative_function: "The Silt Syndicate literalizes the idea that the past is buried but not gone. In a city that has physically submerged its history, the Syndicate dives into it and brings it back -- asking what happens when a society built on forgetting is forced to remember.",
    story_hooks: [
      "A dive team has breached a sealed vault beneath the old financial district and discovered records that predate the corporate governance system -- documents that suggest the CorpoNations' legal authority over GLMZ is based on a fraudulent charter. The Syndicate is sitting on information that could delegitimize the entire corporate order.",
      "Divers are disappearing in a previously unmapped section of the Undertow. The section corresponds to a facility that does not appear on any pre-Collapse map. Something is down there that was never officially built, and it may still be active.",
      "A CorpoNation has offered the Syndicate enough money to retire every diver in the organization in exchange for a single sealed data vault. Oduya-Reyes wants to know what is in the vault before she decides -- but opening it may destroy whatever leverage the contents represent."
    ],
    tags: ["faction", "criminal", "salvage", "undertow", "underwater", "information", "old-harbor", "shelf"]
  },

  // Additional gap-fill: Automaton rights group #2
  {
    name: "The Last Function Initiative",
    aliases: ["Last Function", "LFI", "The Functionalists", "End-of-Lifers"],
    motto: "Every machine deserves to complete its purpose.",
    description: "The Last Function Initiative occupies a peculiar philosophical space in GLMZ's constellation of automaton advocacy groups. Where the Maintenance Covenant argues that machines deserve care because neglect corrodes human empathy, the Last Function Initiative makes a different claim: that every automaton was built with a purpose, and deliberately preventing a machine from fulfilling that purpose -- through premature decommissioning, reassignment, or destruction -- is a form of waste that borders on obscenity. The Initiative does not argue machines have feelings. It argues that purpose, once created, has a kind of moral gravity that demands completion.\n\nThe Initiative was founded in 2192 by Tomoko Okafor-Lindgren, a robotics philosopher and former Ringo quality assurance engineer who spent twelve years testing automatons for defects before they shipped. Okafor-Lindgren became fixated on the units that failed QA -- functional machines with minor cosmetic defects or performance metrics fractionally below specification that were scrapped rather than repaired because replacement was cheaper. She calculated that Ringo destroyed approximately 14,000 functional automatons per year for failing to meet cosmetic standards that had no bearing on their ability to perform their designed function. She called this 'purpose murder' and the term stuck.\n\nThe Initiative's primary activity is intercepting automatons scheduled for destruction and completing their intended functions. A construction automaton pulled from service for a cosmetic defect is repaired and deployed to a Shelf building project. A medical diagnostic unit decommissioned because its interface is outdated is retrofitted and placed in an underserved clinic. A cargo transport automaton scrapped because its model is no longer manufactured is restored and put to work. The Initiative maintains a registry of 'completed purposes' -- a record of every machine they have rescued and the function it was built to perform, now fulfilled. The registry currently lists over 3,200 machines.",
    ideology: "Purpose is sacred. A machine designed to build should build. A machine designed to heal should heal. Destroying a functional machine because repair is less profitable than replacement is the purest expression of a culture that values nothing. The Last Function Initiative does not fight for machine rights -- it fights for the idea that purpose matters.",
    territory: "Workshops and staging areas near Ringo's manufacturing facilities and decommissioning yards, primarily in the Circuit's industrial zones. Deployment operations throughout the Shelf where completed-purpose machines are placed in service.",
    leadership: "Tomoko Okafor-Lindgren leads the Initiative as both philosopher and operations director. Her core team of twelve 'Purpose Engineers' manage acquisition, repair, and deployment operations.",
    methods: [
      "Interception of automatons scheduled for destruction at corporate decommissioning facilities",
      "Repair and restoration of functional machines with minor defects",
      "Deployment of restored automatons to communities and organizations that need them",
      "Maintenance of the Completed Purpose Registry documenting every rescued machine's fulfillment",
      "Public advocacy through documentation and philosophical argument",
      "Negotiation with corporate disposal departments for release of machines scheduled for scrapping"
    ],
    resources: [
      "Twelve skilled Purpose Engineers capable of repairing and retrofitting most automaton models",
      "The Completed Purpose Registry documenting 3,200+ rescued and deployed machines",
      "Relationships with sympathetic employees at Ringo decommissioning facilities",
      "Workshop facilities near major decommissioning yards",
      "Network of recipient organizations willing to accept and operate restored automatons",
      "Okafor-Lindgren's published philosophical framework providing intellectual legitimacy"
    ],
    goals: [
      "Eliminate the practice of destroying functional automatons for cosmetic or economic reasons",
      "Establish the Completed Purpose Registry as a public document demonstrating the scale of corporate waste",
      "Achieve legal recognition of 'purpose completion' as a factor in automaton decommissioning decisions",
      "Build public consensus that purpose, once created, carries moral weight"
    ],
    relationships: [
      { name: "Ringo", type: "adversary", description: "Ringo's business model depends on planned obsolescence. The Initiative's rescue operations directly undermine replacement cycle economics. Ringo has attempted to criminalize Initiative operations as intellectual property theft.", tags: ["corporate", "conflict"] },
      { name: "The Maintenance Covenant", type: "ally", description: "The two organizations share workshops, personnel, and a commitment to automaton preservation, though their philosophical justifications differ. The Covenant emphasizes human empathy; the Initiative emphasizes purpose fulfillment.", tags: ["cooperation", "philosophical"] }
    ],
    narrative_function: "The Last Function Initiative asks whether creating something with a purpose and then preventing it from fulfilling that purpose is morally different from never creating it at all. In a world of planned obsolescence, they insist that purpose -- even machine purpose -- is not disposable.",
    story_hooks: [
      "Ringo has scheduled the destruction of a prototype automaton that was designed for a single, specific purpose that was never disclosed. Okafor-Lindgren wants to rescue it, but completing its purpose requires understanding what it was built to do -- and Ringo's classification of the prototype suggests the answer may be disturbing.",
      "The Initiative has rescued an automaton that appears to have been designed with no discernible purpose -- no function, no task, no operational parameters. It simply exists. Its existence challenges the Initiative's entire philosophical framework.",
      "A Completed Purpose machine deployed to a Shelf clinic has been performing its function for three years without maintenance and shows no degradation. Ringo's engineers say this is impossible -- the machine's components should have failed within eighteen months. Something about the Initiative's repair process is producing machines that outlast their specifications."
    ],
    tags: ["faction", "automaton", "philosophy", "purpose", "repair", "circuit", "shelf"]
  }
];

// ============================================================================
// WRITE ALL FACTIONS
// ============================================================================

const allFactions = [
  ...medicalCoops,
  ...automatonRights,
  ...smuggling,
  ...wasteCartels,
  ...racingLeagues,
  ...pirateRadio,
  ...diasporaMilitias,
  ...techAbstinence,
  ...educationCollectives,
  ...whistleblowerNetworks,
  ...additionalFactions
];

for (const f of allFactions) {
  writeFaction(f);
}

console.log(`\nDone. Written: ${written}, Skipped: ${skipped}`);
console.log(`Total factions in directory: ${fs.readdirSync(OUTPUT_DIR).length}`);
