const fs = require('fs');
const path = require('path');

const OUTPUT_DIR = path.resolve(__dirname, '..', 'engine_data', 'people');

// Ensure output directory exists
if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

// Helper: deterministic-ish random from seed
function seededRandom(seed) {
  let s = seed;
  return function () {
    s = (s * 1103515245 + 12345) & 0x7fffffff;
    return s / 0x7fffffff;
  };
}

function pick(arr, rng) {
  return arr[Math.floor(rng() * arr.length)];
}

function pickN(arr, n, rng) {
  const shuffled = [...arr].sort(() => rng() - 0.5);
  return shuffled.slice(0, n);
}

function randFloat(min, max, rng) {
  return Math.round((min + rng() * (max - min)) * 100) / 100;
}

function randInt(min, max, rng) {
  return Math.floor(min + rng() * (max - min + 1));
}

function toFilename(name) {
  return name.toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

// ─── DATA POOLS ────────────────────────────────────────────────

const FIRST_NAMES = [
  "Kofi", "Priya", "Fatou", "Tariq", "Amara", "Kenji", "Liora", "Dmitri", "Yalena", "Bao",
  "Nkechi", "Soren", "Ximena", "Idris", "Anara", "Ravi", "Zuri", "Eshan", "Malika", "Tenzin",
  "Adaeze", "Rashid", "Inari", "Cezar", "Wanjiku", "Suki", "Obed", "Naima", "Lucien", "Bijou",
  "Emeka", "Solene", "Hadiya", "Cassiel", "Meera", "Kwame", "Isolde", "Jabari", "Vesna", "Altan",
  "Chidinma", "Petros", "Samira", "Nikoloz", "Aroha", "Vuk", "Eshe", "Mikael", "Leilani", "Ozkan",
  "Thandiwe", "Arseniy", "Kamala", "Dato", "Mereana", "Bogdan", "Aisha", "Levan", "Tiare", "Nurlan",
  "Folake", "Seraphim", "Dalila", "Enver", "Moana", "Cem", "Aminata", "Zviad", "Hinerangi", "Kairat",
  "Obioma", "Stanislav", "Fatoumata", "Georgi", "Marama", "Tarik", "Safiya", "Lado", "Ngaire", "Aslan",
  "Yewande", "Miroslav", "Hasina", "Vakhtang", "Mere", "Deniz", "Nneka", "Avtandil", "Rangi", "Marat",
  "Chukwuma", "Milena", "Halima", "Zurab", "Ataahua", "Baris", "Oluwaseun", "Nino", "Tui", "Erkin",
  "Adaora", "Branko", "Mariama", "Shota", "Arohanui", "Ilhan", "Chiamaka", "Irakli", "Hinewai", "Timur",
  "Kelechi", "Davor", "Kadiatou", "Tengiz", "Manaia", "Volkan", "Ugochukwu", "Tamara", "Pare", "Aybek",
  "Ifeanyi", "Zoran", "Oumou", "Davit", "Rawiri", "Sinan", "Ebele", "Giorgi", "Wiremu", "Kanat",
  "Ngozi", "Dragan", "Fanta", "Nika", "Tane", "Onur", "Adaugo", "Gela", "Ihaia", "Sagyn",
  "Obinna", "Goran", "Binta", "Bagrat", "Hemi", "Alper", "Somadina", "Ketevan", "Nikau", "Bolat",
  "Uzoma", "Nenad", "Djeneba", "Nodar", "Kahurangi", "Burak", "Nnamdi", "Maka", "Rongo", "Daulet"
];

const LAST_COMPOUND = [
  "Lindqvist-Okafor", "Vasquez-Chatterjee", "Chen-Adeyemi", "Mwangi-Leblanc", "Eriksson-Diallo",
  "Osei-Petrov", "Nakamura-Bello", "Svensson-Kone", "Bautista-Nwosu", "Larsen-Mensah",
  "Alvarez-Okonkwo", "Jorgensen-Toure", "Reyes-Abubakar", "Strand-Otieno", "Park-Traore",
  "Holmberg-Dembele", "Santos-Owusu", "Nystrom-Keita", "Gutierrez-Asante", "Petersen-Bah",
  "Volkov-Obi", "Magnusson-Camara", "Rojas-Yeboah", "Karlsen-Sow", "Fujita-Mensah",
  "Brandt-Ouedraogo", "Ivanov-Appiah", "Hedlund-Diarra", "Cruz-Addo", "Haugen-Coulibaly",
  "Kimura-Nkrumah", "Arvidsson-Sesay", "Delgado-Opoku", "Lund-Bangura", "Takahashi-Boateng",
  "Kozlov-Asamoah", "Engstrom-Fofana", "Medina-Quaye", "Dahl-Conteh", "Yamamoto-Darko",
  "Novak-Tetteh", "Bergstrom-Konate", "Aguilar-Annan", "Holm-Danquah", "Inoue-Aidoo",
  "Sorokin-Gyasi", "Lindgren-Ofori", "Pena-Baah", "Nyberg-Sarpong", "Tanaka-Frimpong",
  "Kuznetsov-Adu", "Forsberg-Ofosu", "Ramos-Mensah", "Ekberg-Asare", "Watanabe-Amponsah",
  "Popov-Adjei", "Sjostrom-Kwarteng", "Torres-Bonsu", "Granlund-Tawiah", "Sato-Poku",
  "Belov-Ntim", "Hallberg-Acheampong", "Vargas-Baffour", "Strom-Duah", "Morita-Antwi",
  "Fedorov-Ampofo", "Sundberg-Boadu", "Herrera-Kyei", "Malm-Amoah", "Ito-Takyi",
  "Orlov-Obeng", "Akerlund-Mensah-Bonsu", "Castillo-Ababio", "Vikstrom-Asiedu", "Ogawa-Twumasi",
  "Zhukov-Wiredu", "Dahlberg-Boakye", "Navarro-Afriyie", "Ekman-Danso", "Hayashi-Amankwah",
  "Baranov-Fosu", "Hedberg-Owusu-Ansah", "Padilla-Nti", "Ahlstrom-Quartey", "Nishi-Anim",
  "Smirnov-Ankrah", "Blomberg-Konadu", "Guerrero-Osei-Mensah", "Borg-Agyeman", "Kobayashi-Appiah-Kubi",
  "Tarasov-Yiadom", "Sandstrom-Badu", "Vega-Acheampong", "Wendt-Frempong", "Mori-Sefa",
  "Zakharov-Boateng", "Edlund-Gyan", "Salazar-Quansah", "Blom-Attah", "Arai-Ofori-Atta",
  "Lebedev-Tutu", "Nordin-Asante-Mensah", "Ibarra-Prempeh", "Akerblom-Manu", "Kato-Adomako"
];

const STREET_NAMES = [
  "Switchblade", "Phantom", "Glitch", "Vortex", "Ember", "Cipher", "Specter", "Neon", "Havoc", "Wraith",
  "Pulse", "Mirage", "Torque", "Flux", "Dagger", "Static", "Cobalt", "Shade", "Ratchet", "Fuse",
  "Throttle", "Whisper", "Blitz", "Fracture", "Drift", "Hollow", "Scalpel", "Voltage", "Onyx", "Ash",
  "Recoil", "Nexus", "Crimson", "Talon", "Rogue", "Basalt", "Iris", "Shard", "Catalyst", "Zero",
  "Requiem", "Tempest", "Jackal", "Oxide", "Nimble", "Solvent", "Garrote", "Turbine", "Dusk", "Spire",
  "Chrome", "Venom", "Arsenal", "Deadbolt", "Mantis", "Riptide", "Cortex", "Ignite", "Lockjaw", "Quake",
  "Razor", "Silhouette", "Piston", "Blackout", "Moth", "Keystone", "Shrapnel", "Murmur", "Anvil", "Cinder",
  "Daemon", "Undertow", "Barrage", "Flint", "Helix", "Meridian", "Nocturne", "Obsidian", "Paragon", "Raze",
  "Seraph", "Tungsten", "Umbra", "Vector", "Zenith", "Arc", "Breach", "Crucible", "Dynamo", "Eclipse",
  "Forge", "Grim", "Husk", "Jolt", "Knell", "Locus", "Maelstrom", "Null", "Orion", "Prism",
  "Quill", "Rift", "Surge", "Thorn", "Undertaker", "Vanguard", "Warp", "Axiom", "Blight", "Coil",
  "Dirge", "Etcher", "Fathom", "Gravel", "Haze", "Inkwell", "Jade", "Kestrel", "Lattice", "Mote",
  "Nyx", "Ozone", "Pyrite", "Quarry", "Revenant", "Slate", "Trench", "Ulcer", "Vertex", "Whet",
  "Xenon", "Yield", "Zinc", "Ablaze", "Buckshot", "Caliber", "Dowel", "Edgewater", "Filament", "Graphite",
  "Halcyon", "Iridium", "Jasper", "Kindle", "Lithium", "Magnet", "Nadir", "Optic", "Palladium", "Quicksilver",
  "Radium", "Stencil", "Tangent", "Uranium", "Vanadium", "Wolfram", "Xeric", "Yonder", "Zephyr", "Alloy",
  "Bismuth", "Carbon", "Duress", "Entropy", "Fulcrum", "Granite", "Helios", "Ion", "Jetstream", "Karma",
  "Lodestone", "Molten", "Nebula", "Osmium", "Phosphor", "Quartz", "Rubicon", "Schist", "Tectonic", "Uplink",
  "Valence", "Weld", "Xylem", "Yoke", "Zirconia", "Aegis", "Brine", "Conduit", "Downdraft", "Exo",
  "Ferrite", "Gyro", "Hardline", "Icicle", "Jumper", "Kinetic", "Lumen", "Marcasite", "Neutron", "Oscillate"
];

const ROLES = [
  "street samurai", "netrunner", "fixer", "courier", "smuggler", "bodyguard", "bounty hunter",
  "infiltrator", "combat medic", "wheelman", "demolitions expert", "surveillance specialist",
  "extraction specialist", "tech retriever", "ghost", "cleaner", "broker", "face",
  "rigger", "sniper", "breacher", "poisoner", "getaway pilot", "data thief",
  "counterintelligence specialist", "saboteur"
];

const ROLE_DESCRIPTIONS = {
  "street samurai": "Freelance combat specialist",
  "netrunner": "Network intrusion and digital warfare specialist",
  "fixer": "Contract broker and resource coordinator",
  "courier": "High-risk package and data transport",
  "smuggler": "Cross-district contraband specialist",
  "bodyguard": "Personal security and threat neutralization",
  "bounty hunter": "Target acquisition and fugitive recovery",
  "infiltrator": "Covert entry and social penetration specialist",
  "combat medic": "Field trauma surgeon and combat support",
  "wheelman": "High-speed vehicle operations specialist",
  "demolitions expert": "Structural breach and explosive ordnance specialist",
  "surveillance specialist": "Electronic and physical observation expert",
  "extraction specialist": "Personnel recovery and hostile extraction",
  "tech retriever": "Prototype theft and corporate tech acquisition",
  "ghost": "Precision elimination specialist",
  "cleaner": "Evidence removal and scene sanitation",
  "broker": "Information trader and intelligence analyst",
  "face": "Social engineering and negotiation specialist",
  "rigger": "Remote drone and vehicle operations specialist",
  "sniper": "Long-range precision fire specialist",
  "breacher": "Forced entry and close-quarters combat specialist",
  "poisoner": "Chemical and biological agent specialist",
  "getaway pilot": "Aerial extraction and rapid transit pilot",
  "data thief": "Corporate espionage and data exfiltration",
  "counterintelligence specialist": "Counter-surveillance and mole detection",
  "saboteur": "Infrastructure disruption and industrial sabotage"
};

const LOCATIONS = [
  "The Shelf — lower district tenements near the old lakeshore",
  "The Circuit — neon-lit entertainment and vice district",
  "The Narrows — cramped residential corridors between megastructures",
  "Old Harbor — repurposed shipping infrastructure turned black market hub",
  "Geartown — industrial district of chop shops and fabricators",
  "The Spires — mid-level corporate residential towers",
  "Burnside Corridor — transit hub connecting Shelf to Circuit",
  "Kessler Row — arms dealers and augmentation clinics",
  "The Lattice — vertical slum built into scaffolding of an unfinished arcology",
  "Ashfield — former residential district, half-burned, now squatter territory",
  "Dockside — Lake Michigan waterfront, smuggler territory",
  "The Canopy — elevated walkway district above Geartown",
  "Whitecap — lakefront district, tourist traps over criminal infrastructure",
  "The Underbelly — sub-street tunnels and maintenance corridors",
  "Meridian Station — central transit nexus, neutral ground",
  "Glassway — corporate-adjacent retail district, heavy surveillance",
  "The Rookery — dense vertical housing, gang-controlled",
  "Steamvent Alley — geothermal district, hot and chemical-smelling",
  "Crucible Square — open-air market, Tier 1-2 commerce",
  "The Overhang — residential shelves built onto megastructure exteriors"
];

const AFFILIATIONS = [
  "Independent — works solo contracts",
  "Independent — loose crew affiliations",
  "Freelance — available through Circuit fixers",
  "Independent — operates through reputation only",
  "Unaffiliated — too volatile for permanent crews",
  "Semi-independent — recurring contracts with mid-tier fixers",
  "Independent — maintains network of professional contacts",
  "Freelance — works the Shelf circuit exclusively",
  "Independent — known in Old Harbor circles",
  "Freelance — Geartown regular",
  "Independent — Narrows-based operations",
  "Freelance — multi-district operator",
  "Independent — Dockside connections",
  "Semi-independent — has standing arrangements with several crews",
  "Independent — Lattice territory specialist"
];

const AUGMENTATION_TYPES = [
  "Subdermal armor plating across torso and forearms — military surplus, scratched serial numbers",
  "Neural interface jack at base of skull — standard runner-grade, allows direct net access",
  "Reflex boosters wired into spinal column — shaves reaction time by 40ms",
  "Cybernetic eyes with thermal overlay and low-light enhancement — chrome iris rings",
  "Synthetic muscle fiber replacement in legs — doubles sprint speed, terrible on stairs",
  "Bone lacing — titanium mesh throughout skeleton, increases durability and striking power",
  "Wired reflexes with adrenaline pump — chemical-mechanical hybrid augmentation",
  "Gecko-grip fingertip implants and toe pads — can climb smooth surfaces",
  "Internal air filtration system — lungs replaced with synthetic equivalents, immune to gas attacks",
  "Smartgun link in right forearm — interfaces directly with compatible weapons",
  "Dermal camouflage — chromatic skin pigment cells, limited active camouflage",
  "Cybernetic arm — right, full replacement from shoulder — stronger than organic, obvious chrome",
  "Cranial bomb defuser — specialized implant that detects and neutralizes cortex bombs in others",
  "Vocal modulator — can replicate any voice after 30 seconds of sample audio",
  "Pain editor — dampens pain signals, allows function through injuries that would incapacitate",
  "Move-by-wire system — full nervous system augmentation, makes the body a weapon platform",
  "Retractable monofilament whip in left forearm — concealed weapon, devastating in close quarters",
  "Chemical analyzer in nasal passages — can identify substances by scent alone",
  "Internal drug synthesizer — produces combat stims and medical compounds on demand",
  "Cyberears with directional microphones and sound dampening — can hear whispers at 50 meters",
  "Geneware muscle density treatment — looks natural, hits like chrome",
  "Bioware adrenaline regulator — keeps calm under fire, controlled hormone release",
  "Synthetic blood with oxygen-carrying nanites — doesn't tire, heals faster",
  "Subdermal weapon mount — pop-out blade in each forearm",
  "Neural dampener — blocks external hacking attempts, passive ICE in the brain",
  "Thermal masking system — body heat signature can be suppressed or spoofed",
  "Enhanced tendons — geneware upgrade, jump height and grip strength tripled",
  "Cortical bomb (employer installed, removed by splicer, scar tissue remains)",
  "Eye replacement — one organic, one cybernetic with recording capability and zoom",
  "Hydraulic joints in knees and elbows — absorbs falls, amplifies strikes"
];

const FEARS = [
  "Becoming obsolete as augmentation technology outpaces their chrome",
  "Being identified by corporate facial recognition — living off-grid is survival",
  "Losing control of their own body to a hacker or malfunctioning cyberware",
  "Dying alone in some alley and nobody noticing for weeks",
  "That the work they do makes the city worse, not better",
  "Being captured alive — death is preferable to corporate interrogation",
  "Betrayal by someone they trust — the only currency that matters",
  "Going cyberpsycho — losing the boundary between human and machine",
  "That their reputation is built on luck, not skill, and the luck is finite",
  "Never being able to stop running — the lifestyle is a trap disguised as freedom",
  "Their past catching up — debts, enemies, broken promises",
  "Losing their edge to age — the body slows before the mind accepts it",
  "That they're fundamentally alone and connection is just a vulnerability",
  "Being replaced — younger, faster, cheaper runners coming up behind them",
  "The quiet — silence means something is about to go very wrong"
];

const DESIRES = [
  "Enough Quanta to buy out of the life — a clean identity and passage out of GLMZ",
  "Reputation — to be the name fixers whisper when the job absolutely cannot fail",
  "To find the person who burned them and make it educational",
  "A crew worth trusting — people who stay when the shooting starts",
  "To build something that outlasts the next contract cycle",
  "Control — over their body, their schedule, their future",
  "To prove everyone who dismissed them catastrophically wrong",
  "The perfect run — one job so clean it becomes legend",
  "To protect the people in their district who can't protect themselves",
  "Knowledge — to understand the systems that grind people down",
  "Stability — a place to sleep that's the same place two nights running",
  "To matter to someone for who they are, not what they can do",
  "Power — enough leverage that nobody ever threatens them again",
  "Justice — the corporate kind, where the punishment fits the profit margin",
  "Freedom from augmentation debt — the interest compounds like a tumor"
];

const COPING = [
  "Drinks methodically — not to get drunk, to maintain a specific level of not-caring",
  "Maintains weapons obsessively — cleaning guns is meditation",
  "Exercises compulsively — the body must be ready, always",
  "Isolates — retreats into their space when overwhelmed, emerges only for work",
  "Humor — deflects everything serious with jokes until the joke becomes the personality",
  "Violence — hits something when they can't process emotions, preferably something that deserves it",
  "Planning — responds to chaos by making detailed contingency plans for everything",
  "Ritual — follows the same pre-job routine every time, superstition as structure",
  "Compartmentalization — work feelings and personal feelings exist in separate boxes",
  "Substances — stims to stay sharp, downers to sleep, a careful pharmaceutical tightrope",
  "Overwork — takes every contract offered because idleness invites thinking",
  "Connection — finds someone to talk to, even if it's a bartender or a cat",
  "Art — draws, paints, or sculpts in private; the violence goes somewhere constructive",
  "Denial — nothing is wrong, everything is fine, they are fine, shut up",
  "Control — micromanages everything within reach to compensate for the chaos everywhere else"
];

const BLIND_SPOTS = [
  "Assumes everyone has an angle because they always have an angle",
  "Cannot accept help without interpreting it as leverage",
  "Overestimates their own abilities in situations outside their specialty",
  "Trusts technology more than people — machines don't betray",
  "Mistakes loyalty for weakness in others and cynicism for strength in themselves",
  "Cannot see that their independence is actually isolation wearing a better outfit",
  "Assumes the worst in corporate people, missing the rare ones who could be allies",
  "Fails to notice when their own behavior mirrors the people they despise",
  "Believes their trauma makes them special instead of recognizing it as damage to manage",
  "Cannot distinguish between caution and cowardice in themselves or others",
  "Refuses to update their mental model of people — first impressions calcify into permanent judgments",
  "Thinks speed can substitute for planning because it's worked so far",
  "Cannot recognize when they're being manipulated by someone who genuinely seems to care",
  "Assumes their skill set is more transferable than it actually is",
  "Mistakes pattern recognition for wisdom — sees connections that aren't there"
];

const HABITS_POOL = [
  "Checks exits within 10 seconds of entering any room",
  "Carries a specific caliber round as a lucky charm — has never fired it",
  "Eats the same meal before every job — superstition masquerading as preference",
  "Sleeps with one hand on a weapon — has done this for so long it's not deliberate anymore",
  "Taps a specific rhythm on hard surfaces when thinking — Morse code for a word they won't say",
  "Arrives fifteen minutes early to everything — late once, nearly died, never again",
  "Counts ammunition obsessively — knows the exact round count at all times",
  "Maintains a dead drop with emergency supplies in three separate districts",
  "Talks to their gear — names weapons, thanks tools, treats equipment as companions",
  "Takes a specific route home every time, varying only one block per trip — paranoia as lifestyle",
  "Keeps a physical paper notebook — no digital record of personal thoughts",
  "Never drinks anything they didn't see poured or open themselves",
  "Wears the same item of clothing for luck — a scarf, a glove, a specific pair of boots",
  "Practices a kata or fighting form every morning regardless of circumstances",
  "Saves a percentage of every payment in physical Quanta, hidden in the walls of their space"
];

const BREAKING_POINTS_POOL = [
  "Watching someone they trained or mentored die on a job they recommended",
  "Being forced to harm a civilian — collateral damage that can't be rationalized",
  "Discovering their employer deliberately sent them into a death trap",
  "Losing their primary augmentation — the one that defines their professional identity",
  "Learning that a job they completed resulted in mass casualties they weren't told about",
  "Betrayal by the one person they actually trust — the single point of failure in their emotional architecture",
  "Being publicly exposed — name, face, address, all of it blown",
  "Running out of options — no contacts, no money, no safe house, no way out",
  "A child being put in danger as leverage against them",
  "Their body rejecting their cyberware — chrome rot, immune rejection, the nightmare scenario",
  "Being forced to choose between a crew member and the mission — and choosing wrong",
  "Complete isolation — cut off from network, contacts, and purpose simultaneously"
];

const VERBAL_TICS_POOL = [
  "Ends statements with 'yeah?' — not a question, a dare",
  "Calls everyone 'friend' — the warmth varies from genuine to threatening",
  "Whispers when angry — the quieter the voice, the worse the situation",
  "Uses technical jargon for everything, including emotions",
  "Speaks in clipped fragments when stressed — drops pronouns, articles, pleasantries",
  "Swears in three languages, often in the same sentence",
  "Uses 'we' instead of 'I' — either royal or dissociative, nobody asks",
  "Pauses mid-sentence to listen — combat habit that bleeds into conversation",
  "Refers to killing as 'solving' — clinical detachment as coping mechanism",
  "Repeats the last thing someone said back to them before responding — processing or intimidation",
  "Uses old pre-collapse slang that nobody else remembers",
  "Hums tunelessly when planning — the team has learned to wait for the humming to stop",
  "Calls money 'weight' — as in 'that job doesn't carry enough weight'",
  "Never uses contractions when lying — 'I did not' instead of 'I didn't'",
  "Addresses people by physical traits — 'hey, tall one' or 'listen, chrome-arm'"
];

// ─── DESCRIPTION TEMPLATES ────────────────────────────────────

function generateDescription(name, gender, pronouns, role, age, location, augDesc, rng) {
  const they = pronouns.split('/')[0];
  const them = pronouns.split('/')[1] || 'them';
  const their = { "he": "his", "she": "her", "they": "their" }[they] || "their";
  const is_ = they === "they" ? "are" : "is";
  const has_ = they === "they" ? "have" : "has";
  const s_ = they === "they" ? "" : "s";

  const builds = [
    `tall and rawboned, the kind of frame that suggests either malnutrition or efficiency depending on the decade`,
    `compact and densely muscled, built low to the ground like something designed to survive impacts`,
    `wiry and quick-moving, with the restless energy of someone who converts anxiety into velocity`,
    `broad-shouldered and heavy, carrying their weight like armor rather than burden`,
    `lean and angular, all sharp edges and visible tension`,
    `average height but with a presence that fills more space than the body accounts for`,
    `tall and loose-limbed, moving with the deliberate economy of someone who has trained every motion`,
    `stocky and solid, with hands that look like they were built for breaking things and a face that confirms it`,
    `slight and easy to overlook, which is exactly the point`,
    `athletic and fluid, the kind of body that suggests either dancer or fighter — and in GLMZ, the distinction is academic`
  ];

  const faces = [
    `${their} face carries the layered heritage of generations of diaspora mixing — features that resist easy categorization and reward attention`,
    `scars map ${their} face like a transit diagram of bad decisions and worse luck`,
    `${they} ${has_} the kind of face that changes depending on the light — trustworthy in daylight, dangerous after dark`,
    `${their} expression defaults to a careful neutrality that took years of practice to maintain`,
    `there's something unfinished about ${their} face, like a sculpture the artist walked away from — striking rather than beautiful`,
    `${they} ${has_} soft features that belie the chrome underneath — people underestimate ${them} once, usually only once`,
    `${their} face is memorable in a profession that rewards forgettability, and ${they} ${has_} learned to compensate`,
    `the augmentation scarring around ${their} temples and jawline tells a story of installation by someone who prioritized function over aesthetics`
  ];

  const reputations = [
    `In the runner community, ${name} ${is_} known for getting the job done and not asking questions that would make the job harder to stomach.`,
    `${name} ${has_} built a reputation on reliability rather than flash — ${they} show${s_} up, ${they} deliver${they === "they" ? "" : "s"}, ${they} collect${they === "they" ? "" : "s"}.`,
    `The fixers who work with ${name} describe ${them} as a precision instrument — point ${them} at a problem and get out of the way.`,
    `${name} ${is_} the kind of runner other runners tell stories about, though the stories vary depending on who ${they} burned${they === "they" ? "" : ""} last.`,
    `What ${name} lacks in subtlety, ${they} compensate${they === "they" ? "" : "s"} for in thoroughness. No loose ends, no unfinished business, no second chances.`,
    `${name} operates in a narrow band between competence and recklessness that makes fixers nervous and clients satisfied.`,
    `${they} ${is_} not the most talented ${role} in GLMZ, but ${they} might be the most stubborn, and in this city that's worth more.`,
    `Everyone who's worked with ${name} agrees on two things: ${they} ${is_} worth the fee, and ${they} ${is_} terrible company at dinner.`,
    `${name} ${has_} survived long enough in the runner game to be either very good or very lucky, and ${they} cultivate${they === "they" ? "" : "s"} ambiguity on the topic.`,
    `In a city full of people who talk about what they'll do, ${name} ${is_} one of the ones who actually does it.`
  ];

  const backstoryFragments = [
    `${they} came up through the Shelf's informal apprentice system, learning the trade from runners who didn't survive long enough to see ${them} surpass them`,
    `before the runner life, ${they} worked corporate — low-tier security, the kind that teaches you exactly how the system's defenses fail`,
    `${they} arrived in GLMZ six years ago with nothing but chrome and debts, and ${has_} been working off both ever since`,
    `${they} grew up in the Narrows, where the walls are so close together you learn to fight in confined spaces or you learn to lose`,
    `military training — one of the private security corps that folded during the consolidation wars — left ${them} with skills and night terrors in equal measure`,
    `${they} taught ${them}self the trade through trial, error, and a pain tolerance that concerns the people who know ${them} best`,
    `${they} inherited the role from a mentor who disappeared on a job four years ago — the mentor's gear, contacts, and unfinished business all came with it`,
    `${their} entry into running was involuntary — a debt to the wrong people, a choice between working it off or losing parts — and the debt is still not fully paid`,
    `${they} used to be somebody else, in another city, with another name — that person is officially dead, and ${name} plans to keep it that way`,
    `born and raised in GLMZ, third generation — ${they} ${has_} the city in ${their} blood, its rhythms in ${their} bones, its cruelty in ${their} expectations`
  ];

  const build = pick(builds, rng);
  const face = pick(faces, rng);
  const rep = pick(reputations, rng);
  const backstory = pick(backstoryFragments, rng);

  const para1 = `${name} ${is_} ${build}. At ${age}, ${face}. ${augDesc ? `The chrome is visible — ${augDesc.toLowerCase().slice(0, 80)} — and ${they} make${s_} no effort to hide it.` : `${they} carry${they === "they" ? "" : "s"} ${their} augmentations under the skin, invisible until activated.`}`;
  const para2 = `${backstory.charAt(0).toUpperCase() + backstory.slice(1)}. ${they === "they" ? "They" : they.charAt(0).toUpperCase() + they.slice(1)} work${s_} as a ${role} out of ${location.split(' — ')[0]}, taking contracts that match ${their} skills and ${their} risk tolerance, which ${is_} higher than most people consider healthy.`;
  const para3 = rep;

  return `${para1}\n\n${para2}\n\n${para3}`;
}

// ─── MAIN GENERATION ──────────────────────────────────────────

const runners = [];
const usedNames = new Set();
const usedStreetNames = new Set();
const streetNamesCopy = [...STREET_NAMES];

function generateRunner(index) {
  const rng = seededRandom(index * 7919 + 42);

  // Name generation
  let firstName, lastName, fullName;
  let attempts = 0;
  do {
    firstName = pick(FIRST_NAMES, rng);
    lastName = pick(LAST_COMPOUND, rng);
    fullName = `${firstName} ${lastName}`;
    attempts++;
    if (attempts > 50) {
      firstName = pick(FIRST_NAMES, rng) + pick(["a", "o", "i", "e"], rng);
      fullName = `${firstName} ${lastName}`;
    }
  } while (usedNames.has(fullName));
  usedNames.add(fullName);

  // Street name
  let streetName;
  do {
    streetName = pick(streetNamesCopy, rng);
  } while (usedStreetNames.has(streetName));
  usedStreetNames.add(streetName);

  // Gender distribution: 40/40/20
  const genderRoll = rng();
  let gender, pronouns;
  if (genderRoll < 0.4) { gender = "male"; pronouns = "he/him"; }
  else if (genderRoll < 0.8) { gender = "female"; pronouns = "she/her"; }
  else { gender = "nonbinary"; pronouns = "they/them"; }

  // Pronoun helpers — declared early so all templates can use them
  const they = pronouns.split('/')[0];
  const them = pronouns.split('/')[1] || "them";
  const their = { "he": "his", "she": "her", "they": "their" }[they] || "their";

  const role = pick(ROLES, rng);
  const roleDesc = ROLE_DESCRIPTIONS[role];
  const age = randInt(18, 65, rng);
  // Weight toward 25-45
  const ageAdjusted = rng() < 0.7 ? randInt(25, 45, rng) : age;
  const location = pick(LOCATIONS, rng);
  const affiliation = pick(AFFILIATIONS, rng);
  const augmentation = pick(AUGMENTATION_TYPES, rng);
  const augmentation2 = pick(AUGMENTATION_TYPES, rng);
  const augFull = augmentation + ". " + augmentation2;

  const description = generateDescription(fullName, gender, pronouns, role, ageAdjusted, location, augmentation, rng);

  // Psychology
  const facetWeights = {
    wound: randFloat(0.1, 1.0, rng),
    ideal: randFloat(0.1, 1.0, rng),
    id: randFloat(0.1, 1.0, rng),
    shadow: randFloat(0.1, 1.0, rng),
    mask: randFloat(0.1, 1.0, rng),
    ghost: randFloat(0.1, 1.0, rng)
  };

  const coreFears = pickN(FEARS, 3, rng);
  const coreDesires = pickN(DESIRES, 3, rng);
  const copingMechanisms = pickN(COPING, 3, rng);
  const blindSpots = pickN(BLIND_SPOTS, 3, rng);

  // Unique secret
  const secretTemplates = [
    `${fullName} killed someone who didn't deserve it on a job three years ago and has never told anyone. The face appears in dreams weekly.`,
    `${fullName} is in augmentation debt to a corporate subsidiary — Φ180,000 outstanding — and the interest is compounding. If they miss a payment, a kill team comes.`,
    `${fullName} has a child in another city, being raised by someone else. Sends Φ2,000 monthly under an alias. Has never visited.`,
    `${fullName} is slowly going chrome-sick — rejection symptoms are manageable now but getting worse. Has maybe two years before it becomes visible.`,
    `${fullName} once worked for corporate intelligence and still has a handler who calls in favors. Each favor erodes what's left of their independence.`,
    `${fullName} can't read. Hides it behind voice interfaces and memorization. The shame is older than the chrome.`,
    `${fullName} witnessed a massacre during a corporate dispute and has evidence stored in a dead drop. Publishing it would expose powerful people. Keeping it is leverage and liability.`,
    `${fullName} is addicted to combat stims. Functions fine on them, can't function without them. The dependency is invisible to everyone except their splicer.`,
    `${fullName} has been doubling fees to clients and pocketing the difference. The fixer who handles their contracts doesn't know. Yet.`,
    `${fullName} has a terminal condition — geneware degradation — with an estimated five years. They haven't changed their lifestyle. They haven't told anyone.`,
    `${fullName} betrayed their previous crew to survive a corporate raid. The crew didn't survive. Nobody in GLMZ knows this.`,
    `${fullName} maintains a second identity — a completely clean civilian persona with a lease, a job history, and a social profile. It's their exit strategy.`,
    `${fullName} is terrified of deep net diving but has built their reputation on it. Uses black-market AI to handle the heavy lifting while taking credit.`,
    `${fullName} murdered their mentor to take their position and contacts. It was staged as a job gone wrong. The guilt has calcified into something worse than guilt.`,
    `${fullName} sends anonymous tips to GLMZ law enforcement about competitors. It's not principle — it's market manipulation.`
  ];
  const secret = pick(secretTemplates, rng);

  // Speech patterns
  const vocabOptions = [
    "Clipped, technical, stripped of ornament. Speaks like a mission briefing.",
    "Shelf street dialect — layered slang, rhythm-heavy, half the meaning is in the cadence.",
    "Educated vocabulary wrapped in street delivery — the contrast is deliberate.",
    "Quiet and precise. Every word is measured. Silence does the heavy lifting.",
    "Fast, profane, and vivid. Speaks in images — everything is a metaphor for violence or weather.",
    "Old-fashioned formal diction — sounds like they learned to speak from pre-collapse recordings.",
    "Multilingual fragments — switches languages mid-thought, not for privacy but because some concepts only fit in specific tongues.",
    "Flat affect, monotone delivery. The content is terrifying but the voice never rises.",
    "Warm and personable on the surface, with an undertone that suggests the warmth is a professional tool.",
    "Terse to the point of rudeness. Treats words like ammunition — expensive and not to be wasted."
  ];
  const cadenceOptions = [
    "Measured and deliberate — pauses before important words for emphasis.",
    "Staccato bursts separated by calculating silences.",
    "Flowing and unhurried — talks like someone who has never been interrupted and doesn't expect to be.",
    "Rapid and dense — information-heavy sentences that demand attention.",
    "Low and even — the vocal equivalent of still water.",
    "Rhythmic, almost musical — the sentence structure has a pattern even when the words are brutal.",
    "Halting — speaks in fragments, reassembles thoughts in real time.",
    "Conversational and disarming — sounds like a neighbor until the content registers."
  ];

  const vocabulary = pick(vocabOptions, rng);
  const cadence = pick(cadenceOptions, rng);
  const verbalTics = pickN(VERBAL_TICS_POOL, 2, rng);

  // Example lines - role-specific
  const linesByRole = {
    "street samurai": [
      `"I don't get paid to have opinions about the target. I get paid to make them stop moving."`,
      `"Chrome doesn't make you faster. Training makes you faster. Chrome just means you survive learning."`,
      `"You want philosophy, hire a professor. You want someone dead by morning, hire me."`
    ],
    "netrunner": [
      `"Every system has a door. Most of them have a door the architect forgot about."`,
      `"I don't hack networks. I have conversations with them. Most of them are lonely."`,
      `"The ice they're running is military-grade. That means expensive, not impenetrable."`
    ],
    "fixer": [
      `"I know a person who knows a person. That's not evasion — that's the service."`,
      `"My commission is fifteen percent. You're welcome to find someone cheaper. You won't find someone better."`,
      `"The job isn't dangerous. The people involved are dangerous. The job is just logistics."`
    ],
    "courier": [
      `"I don't look inside the package. I don't ask about the package. I deliver the package."`,
      `"Speed is a skill. Knowing when to slow down is wisdom. I sell both."`,
      `"This route has three checkpoints and a kill zone. I'll be there in twenty minutes."`
    ],
    "smuggler": [
      `"Contraband is just cargo that hasn't been properly introduced to the right people."`,
      `"Every border is a suggestion. Every checkpoint is a negotiation."`,
      `"I move things from where they are to where they need to be. The legality is your problem."`
    ],
    "bodyguard": [
      `"You're paying me to be the thing between you and everything else. Stay behind me."`,
      `"I don't protect people I don't respect. Lucky for you, I respect your money."`,
      `"The threat you see isn't the one that kills you. Watch my eyes, not my hands."`
    ],
    "bounty hunter": [
      `"Everyone has a pattern. Patterns are addresses waiting to be read."`,
      `"I find people. Whether they're alive when I bring them back depends on their behavior."`,
      `"You can run. It's cardiovascular exercise for both of us."`
    ],
    "infiltrator": [
      `"The best disguise is being exactly who they expect to see."`,
      `"I was in the building for six hours before anyone realized I didn't work there."`,
      `"Security is a story people tell themselves. I'm the plot twist."`
    ],
    "combat medic": [
      `"Hold still. This is going to hurt and I don't care."`,
      `"I can keep you alive through almost anything. What I can't do is make you grateful."`,
      `"Blood type? No, I don't need your blood type. I need you to stop moving."`
    ],
    "wheelman": [
      `"Get in, sit down, hold on. Questions can wait until we're not being shot at."`,
      `"This vehicle has been modified for speed, armor, and my personal comfort. Two of those help you."`,
      `"I know every street in this district the way you know your own face."`
    ],
    "demolitions expert": [
      `"Explosives are just engineering with a deadline. Usually a very short deadline."`,
      `"I don't blow things up. I perform structural rearrangement."`,
      `"The blast radius is your problem. My problem is making sure there's a blast."`
    ],
    "surveillance specialist": [
      `"I've been watching you for three days. You should close your curtains."`,
      `"Information is the only commodity that appreciates with age."`,
      `"Everyone thinks they're careful. Nobody is careful enough."`
    ],
    "extraction specialist": [
      `"We go in, we get the asset, we leave. Everything else is improvisation."`,
      `"The extraction window is four minutes. I've done it in three."`,
      `"I don't rescue people. I relocate them aggressively."`
    ],
    "tech retriever": [
      `"Corporate R&D security is built to stop amateurs. I'm a professional."`,
      `"The prototype is worth more than both our lives combined. Handle it carefully."`,
      `"I steal things that haven't been invented yet. The future is always in transit."`
    ],
    "ghost": [
      `"I don't exist. Neither will they."`,
      `"Clean means no witnesses, no evidence, no questions. That's three things. I charge for each."`,
      `"The target will stop breathing at the specified time. What you do with that information is your concern."`
    ],
    "cleaner": [
      `"I make problems disappear. The mess is temporary. The clean is permanent."`,
      `"There was never anyone here. There was never anything here. Are we clear?"`,
      `"Forensics is a conversation between evidence and investigators. I end that conversation."`
    ],
    "broker": [
      `"I don't sell information. I sell certainty. Different product, different price."`,
      `"Everything has a value. Most people underestimate what they know."`,
      `"My sources are confidential. My prices are not."`
    ],
    "face": [
      `"People want to trust someone. I just make sure that someone is me."`,
      `"The hardest lock to pick is between someone's ears."`,
      `"I don't lie. I tell a better version of the truth."`
    ],
    "rigger": [
      `"I have eyes in every district. Most of them are on wings."`,
      `"The drone doesn't care about your feelings. Neither do I."`,
      `"Remote operation means I'm everywhere and nowhere. It's very convenient."`
    ],
    "sniper": [
      `"Patience is the weapon. The rifle is just the delivery system."`,
      `"At this distance, the target is already dead. They just don't know it yet."`,
      `"One shot, one outcome. Everything else is theater."`
    ],
    "breacher": [
      `"Doors are polite suggestions. I'm not polite."`,
      `"First through the door, last to flinch. That's the job description."`,
      `"Close quarters is where plans go to die. I make sure it's their plans, not ours."`
    ],
    "poisoner": [
      `"Chemistry is just cooking with consequences."`,
      `"The best poison is the one nobody tests for."`,
      `"Patience, precision, and an understanding of human biology. Everything else is optional."`
    ],
    "getaway pilot": [
      `"Vertical extraction in thirty seconds. Don't be late."`,
      `"I fly things that aren't supposed to fly in places they aren't supposed to go."`,
      `"Airspace regulations are for people who file flight plans. I don't file flight plans."`
    ],
    "data thief": [
      `"Your firewalls are decorative. Like curtains on a submarine."`,
      `"I copy things. The original owners rarely appreciate the compliment."`,
      `"Data wants to be free. I just charge a facilitation fee."`
    ],
    "counterintelligence specialist": [
      `"Everyone in this room is lying. My job is knowing who's lying about what."`,
      `"Paranoia isn't a disorder in this profession. It's a job requirement."`,
      `"I find the leak. What happens to the leak after that isn't my department."`
    ],
    "saboteur": [
      `"I don't break things. I introduce structural critiques."`,
      `"The most effective sabotage looks like bad luck."`,
      `"Everything has a weakness. Infrastructure has thousands."`
    ]
  };

  const exampleLines = linesByRole[role] || [
    `"The job is the job. I don't need to like it to be good at it."`,
    `"You're paying for results. My methods are included at no extra charge."`,
    `"I've survived worse than this. Probably. The details are fuzzy."`
  ];

  // Story hooks
  const hookTemplates = [
    `A former associate resurfaces with evidence that could destroy ${fullName}'s reputation — and a price for silence`,
    `A high-paying contract turns out to target someone ${fullName} owes a personal debt to`,
    `${fullName}'s augmentations begin malfunctioning in the field — sabotage or degradation, and either answer is bad`,
    `A Tier 4 executive offers ${fullName} a permanent position — the pay is obscene and the strings are invisible until they tighten`,
    `Someone is impersonating ${fullName} on jobs, and the impersonator is better at the work than the original`,
    `A dead drop ${fullName} maintains is found and cleaned out — whoever did it now has leverage`,
    `${fullName} discovers a job they completed last year was a setup for something much larger and much worse`,
    `A crew member from ${fullName}'s past shows up in GLMZ, carrying old grievances and new chrome`,
    `The fixer who handles ${fullName}'s contracts goes silent — dead, disappeared, or turned`,
    `${fullName} intercepts intelligence about a threat to their district and must decide between profit and protection`,
    `A corporate black site ${fullName} once infiltrated is being reopened, and the people inside know ${their} face`,
    `${fullName} is offered a contract that would clear all their debts — the target is someone they respect`
  ];
  const storyHooks = pickN(hookTemplates, 3, rng);

  // Daily life
  const dailyLifeOptions = [
    `Wakes early, runs maintenance on gear, checks dead drops and message boards for contracts. Eats when convenient, sleeps when possible. The days between jobs are spent maintaining contacts, updating equipment, and performing the low-grade paranoia that passes for self-care in the runner community.`,
    `Irregular schedule dictated by contract timing. Between jobs: equipment maintenance, physical training, and the careful management of a reputation that exists entirely through word of mouth. Drinks at establishments where runners gather — not for pleasure, but for market intelligence.`,
    `Keeps a rigid daily structure — wake, train, check contracts, maintain gear, sleep. The structure is a defense against the chaos of the work. Deviations from the schedule are a warning sign that something is wrong.`,
    `Lives nocturnally. The work happens at night, so the body follows. Days are for sleeping, equipment maintenance, and the occasional meeting with fixers who keep normal hours. The circadian disruption is permanent and accepted.`,
    `Between contracts, maintains a cover identity — mundane work, mundane hours, mundane neighbors. The transition from civilian to runner and back requires a conscious effort that gets easier with practice and more disturbing with self-awareness.`
  ];
  const dailyLife = pick(dailyLifeOptions, rng);

  // Narrative function
  const narrativeFunctions = [
    `${fullName} represents the working professional of the runner world — competent, reliable, and slowly being consumed by a lifestyle that doesn't accommodate aging or doubt. ${their.charAt(0).toUpperCase() + their.slice(1)} stories explore what happens when skill meets systemic injustice.`,
    `As a ${role}, ${fullName} embodies the tension between independence and isolation. ${they === "they" ? "They are" : they.charAt(0).toUpperCase() + they.slice(1) + " is"} a lens for examining how people maintain humanity in a profession that incentivizes its erosion.`,
    `${fullName} functions as a connector — ${their} work puts ${them} in contact with multiple factions, classes, and agendas. Stories involving ${them} naturally cross boundaries between districts, tiers, and moral categories.`,
    `${fullName} is a pressure point — the kind of character whose choices ripple outward. When ${they} make${they === "they" ? "" : "s"} a decision, other people's lives change. That weight is the engine of ${their} stories.`
  ];
  const narrativeFunction = pick(narrativeFunctions, rng);

  // Decision rules
  const decisionRulesPool = [
    `Will not take contracts against children or families — the line is hard and non-negotiable`,
    `Always demands half payment upfront — trust is a luxury, Quanta is concrete`,
    `Will abandon a job if the intel was deliberately falsified — being set up violates the contract`,
    `Never works with the same crew twice in a row — pattern avoidance as survival strategy`,
    `Will take lower-paying jobs from trusted fixers over higher-paying jobs from unknowns`,
    `Always has an exit route planned before agreeing to any meeting`,
    `Will not betray a client who paid in full, regardless of better offers`,
    `Refuses contracts that require operating inside The Spires — too much surveillance`,
    `Always verifies target identity independently before lethal contracts`,
    `Will work with anyone once. Second time requires vouching from someone trusted.`,
    `Never discusses active contracts with anyone not directly involved`,
    `Maintains a personal blacklist of clients who've burned runners — checks it religiously`
  ];
  const decisionRules = pickN(decisionRulesPool, randInt(4, 6, rng), rng);

  // Escalation ladder
  const escalationTemplates = [
    [
      "1. De-escalate — talk first, assess options, buy time",
      "2. Reposition — move to advantage, control sightlines and exits",
      "3. Threaten — make consequences clear without committing to them",
      "4. Non-lethal force — incapacitate, restrain, disable",
      "5. Lethal force — only when other options are exhausted or time runs out",
      "6. Scorched earth — everything burns, survival takes priority over everything"
    ],
    [
      "1. Observe — gather information before acting",
      "2. Evade — avoid the problem entirely if possible",
      "3. Misdirect — create a distraction, redirect attention",
      "4. Disable — targeted takedowns, quiet and efficient",
      "5. Eliminate — when silence is no longer an option",
      "6. Vanish — disappear completely, burn identity if necessary"
    ],
    [
      "1. Assess — read the situation, identify threats and advantages",
      "2. Negotiate — try to find a solution that doesn't cost ammunition",
      "3. Intimidate — make the cost of conflict clear",
      "4. Precision violence — surgical, targeted, minimal collateral",
      "5. Full engagement — overwhelming force, no half measures",
      "6. Emergency protocol — abort mission, survival priority, deal with consequences later"
    ]
  ];
  const escalationLadder = pick(escalationTemplates, rng);

  // Interpersonal modes
  const interpersonalModes = {
    "employers": pick([
      "Professional and transactional. The relationship is defined by the contract and nothing else.",
      "Guarded respect. They pay, the job gets done. Anything beyond that is liability.",
      "Carefully managed deference that conceals a thorough assessment of their vulnerabilities."
    ], rng),
    "crew": pick([
      "Loyal but reserved. Trusts their competence, withholds personal information by reflex.",
      "Warm and protective within the crew, cold to everyone outside it. The circle is small by design.",
      "Competitive but reliable. Will challenge decisions but never abandon the team."
    ], rng),
    "strangers": pick([
      "Default suspicion, scaled by context. Everyone is a potential threat until proven otherwise.",
      "Superficially friendly, internally cataloguing exits and weapons. The smile is real; the relaxation isn't.",
      "Dismissive unless they demonstrate usefulness or threat potential. Time is a resource."
    ], rng),
    "threats": pick([
      "Cold and efficient. Emotion is a liability in combat. The response is mechanical until the threat is resolved.",
      "Calm escalation. Each response is proportional until it isn't, and the transition is seamless.",
      "Predatory focus. Threats are problems, and problems have solutions. The solution is usually kinetic."
    ], rng)
  };

  // Stress responses
  const stressResponses = {
    "low": pick([
      "Heightened awareness, sharper focus. Low stress is fuel.",
      "Becomes more talkative, working through scenarios verbally.",
      "Channels it into preparation — checks gear, reviews plans, contacts sources."
    ], rng),
    "medium": pick([
      "Withdrawal. Conversations become monosyllabic. Focus narrows to the immediate problem.",
      "Physical agitation — pacing, fidgeting, compulsive equipment checks.",
      "Becomes curt and demanding. Efficiency overrides courtesy."
    ], rng),
    "high": pick([
      "Goes quiet. The silence is not calm — it's the sound of every contingency being evaluated simultaneously.",
      "Hyper-focused tunnel vision. Peripheral concerns disappear. Only the threat exists.",
      "Controlled aggression. The training takes over. Emotions are packed into a box for later."
    ], rng),
    "critical": pick([
      "Disassociation. The body operates on training while the mind retreats to somewhere safe. Comes back disoriented.",
      "Berserker clarity. Everything becomes simple: survive. Deal with the rest later. The simplicity is almost peaceful.",
      "Shutdown. Freezes for 2-3 seconds while the system resets, then acts with mechanical precision. Those frozen seconds are the vulnerability."
    ], rng)
  };

  // Contradictions
  const contradictionsPool = [
    "Preaches independence but is desperate for approval from people they respect",
    "Claims to work only for money but regularly takes underpriced jobs that serve personal justice",
    "Advocates caution to others but takes insane risks personally — double standard they can't see",
    "Values loyalty intensely but has betrayed someone in the past to survive",
    "Presents as emotionless professional but is deeply affected by collateral damage",
    "Claims not to care about reputation but monitors what people say about them obsessively",
    "Insists they work alone but performs noticeably better in a team",
    "Despises corporate culture but has unconsciously adopted its efficiency metrics and self-optimization language",
    "Claims to live in the present but is haunted by specific moments from the past that dictate current behavior",
    "Projects confidence but makes decisions from a foundation of fear they've never examined"
  ];
  const contradictions = pickN(contradictionsPool, randInt(2, 3, rng), rng);

  const habits = pickN(HABITS_POOL, randInt(3, 4, rng), rng);
  const breakingPoints = pickN(BREAKING_POINTS_POOL, randInt(2, 3, rng), rng);

  // Stats
  const statBuild = () => ({
    base: randInt(3, 8, rng),
    augmented: randInt(0, 3, rng),
    effective: randInt(4, 10, rng)
  });

  const physicalStats = {
    strength: statBuild(),
    agility: statBuild(),
    endurance: statBuild(),
    reflexes: statBuild()
  };
  const mentalStats = {
    intelligence: statBuild(),
    perception: statBuild(),
    willpower: statBuild(),
    technical: statBuild()
  };
  const socialStats = {
    charisma: statBuild(),
    intimidation: statBuild(),
    deception: statBuild(),
    empathy: statBuild()
  };

  const personalityStats = {
    aggression: randFloat(0.1, 1.0, rng),
    caution: randFloat(0.1, 1.0, rng),
    loyalty: randFloat(0.1, 1.0, rng),
    greed: randFloat(0.1, 1.0, rng),
    empathy: randFloat(0.1, 1.0, rng)
  };

  const driveOptions = ["survival", "money", "revenge", "reputation", "freedom", "loyalty", "justice", "power", "knowledge", "protection"];
  const drives = pickN(driveOptions, 3, rng);

  const strengthOptions = [
    "exceptional close-quarters combat", "network of reliable contacts", "pain tolerance beyond normal limits",
    "encyclopedic knowledge of GLMZ infrastructure", "ability to remain calm under fire",
    "mechanical aptitude — can repair or modify almost anything", "photographic memory for faces and layouts",
    "multilingual — speaks 4+ languages fluently", "natural leadership presence", "physical endurance",
    "expert marksman", "social chameleon — adapts to any environment", "strategic thinking under pressure",
    "intimidating physical presence", "technical expertise with augmentation systems"
  ];
  const weaknessOptions = [
    "augmentation dependency — performance drops sharply if chrome malfunctions",
    "trust issues that limit crew effectiveness", "substance dependency — functional but fragile",
    "old injury that flares under specific conditions", "emotional volatility under extreme stress",
    "inability to delegate — must control everything personally", "poor impulse control in specific triggers",
    "chronic insomnia affecting long-term performance", "difficulty with authority figures",
    "overconfidence in specialty areas leading to blind spots", "physical limitation from incomplete augmentation",
    "grudge-holding that compromises rational decision-making", "risk-averse to the point of missed opportunities"
  ];
  const tagOptions = [
    "runner", "freelance", role, `tier-${randInt(1, 3, rng)}`, "meridian-88",
    "augmented", "street-level", "contract-worker"
  ];

  const stats = {
    physical: physicalStats,
    mental: mentalStats,
    social: socialStats,
    personality: personalityStats,
    drives: drives,
    thresholds: {
      pain: randInt(4, 9, rng),
      stress: randInt(3, 8, rng),
      loyalty_break: randInt(5, 10, rng)
    },
    strengths: pickN(strengthOptions, randInt(2, 3, rng), rng),
    weaknesses: pickN(weaknessOptions, randInt(2, 3, rng), rng),
    tags: tagOptions
  };

  // Cyberware inventory
  const cyberwareItems = augFull.split('. ').filter(s => s.trim()).map((desc, i) => ({
    slot: pick(["head", "torso", "arms", "legs", "spine", "eyes", "internal"], rng),
    name: desc.split(' — ')[0].trim(),
    description: desc.trim(),
    grade: pick(["military surplus", "street grade", "clinical grade", "black market", "custom fabrication"], rng),
    condition: pick(["good", "worn", "degraded", "refurbished", "pristine"], rng)
  }));

  const character = {
    type: "character",
    name: fullName,
    aliases: [streetName, pick(["The " + pick(["Ghost", "Machine", "Problem", "Professional", "Specialist", "Operator", "Asset", "Contractor"], rng)], rng)],
    species: "human",
    gender: gender,
    pronouns: pronouns,
    role: `${roleDesc} — ${role}`,
    age: ageAdjusted,
    status: "active",
    location: location,
    description: description,
    affiliation: affiliation,
    augmentations: augFull,
    daily_life: dailyLife,
    narrative_function: narrativeFunction,
    psychology: {
      facet_weights: facetWeights,
      core_fears: coreFears,
      core_desires: coreDesires,
      coping_mechanisms: copingMechanisms,
      blind_spots: blindSpots,
      secret: secret
    },
    speech_patterns: {
      vocabulary: vocabulary,
      cadence: cadence,
      verbal_tics: verbalTics,
      example_lines: exampleLines
    },
    relationships: [],
    story_hooks: storyHooks,
    behavioral: {
      decision_rules: decisionRules,
      escalation_ladder: escalationLadder,
      interpersonal_modes: interpersonalModes,
      stress_responses: stressResponses,
      contradictions: contradictions,
      habits: habits,
      breaking_points: breakingPoints
    },
    stats: stats,
    cyberware_inventory: cyberwareItems,
    timeline: [],
    changelog: []
  };

  return character;
}

// ─── EXECUTION ────────────────────────────────────────────────

let created = 0;
let skipped = 0;
let errors = 0;

for (let i = 0; i < 200; i++) {
  try {
    const runner = generateRunner(i);
    const filename = toFilename(runner.name);
    const filepath = path.join(OUTPUT_DIR, filename);

    if (fs.existsSync(filepath)) {
      console.log(`SKIP (exists): ${filename}`);
      skipped++;
      continue;
    }

    fs.writeFileSync(filepath, JSON.stringify(runner, null, 2), 'utf8');
    console.log(`CREATED: ${filename} — ${runner.name} (${runner.role})`);
    created++;
  } catch (err) {
    console.error(`ERROR at index ${i}: ${err.message}`);
    errors++;
  }
}

console.log(`\n=== GENERATION COMPLETE ===`);
console.log(`Created: ${created}`);
console.log(`Skipped (existing): ${skipped}`);
console.log(`Errors: ${errors}`);
console.log(`Total files in directory: ${fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length}`);
