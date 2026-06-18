const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const OUTPUT_DIR = path.resolve(__dirname, '..', 'engine', 'data', 'people');

// Ensure output directory exists
if (!fs.existsSync(OUTPUT_DIR)) {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

function generateId() {
  return crypto.randomBytes(16).toString('hex');
}

function slugify(name) {
  const truncated = name.slice(0, 60);
  return truncated.toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '')
    .slice(0, 80);
}

function toFilename(name) {
  return slugify(name) + '.json';
}

// ─── PI DEFINITIONS ────────────────────────────────────────────

const investigators = [
  // 1. Classic noir gumshoe
  {
    name: "Kofi Nakamura-Singh",
    aliases: ["Old Smoke", "The Relic"],
    affiliation: "Independent — solo practice, Shelf Level 2",
    tier: "Tier 2",
    location: "A one-room office above a noodle shop on Burnside Corridor, frosted glass door with his name stenciled in gold leaf that's been peeling since 2187",
    specialization: "Missing persons, infidelity cases, old-fashioned footwork investigations",
    methods: [
      "Physical surveillance and tailing",
      "Interviewing witnesses face-to-face",
      "Maintaining a network of street-level informants",
      "Reading body language and micro-expressions without BCI assistance",
      "Paper-trail analysis — actual paper, when it exists"
    ],
    notable_cases: [
      "Found the missing daughter of a Shelf-level mechanic by walking the Narrows for six straight days asking questions nobody else thought to ask",
      "Exposed a Tier 3 marriage counselor who was selling client session recordings to blackmailers",
      "Tracked a con artist across four districts using nothing but witness descriptions and shoe leather"
    ],
    equipment: [
      "A battered trench coat with sixteen interior pockets, each assigned a specific purpose",
      "Analog camera with chemical film — images can't be hacked or remotely wiped",
      "Flask of synthetic bourbon that's never empty and never full",
      "Voice recorder with physical tape — no digital footprint",
      "Revolver, .38 caliber, kept in a shoulder holster he's worn so long the leather has molded to his body"
    ],
    personality: "Kofi is an anachronism and he knows it. He moves through GLMZ like a man displaced in time — slow where everyone else is fast, analog where everyone else is digital, patient where everyone else is frantic. He believes technology makes investigators lazy, that a BCI scan tells you what someone did but never why, and that the why is the only thing that matters. He drinks too much, sleeps too little, and has a moral code so rigid it has cost him every relationship he's ever had. He is unfailingly polite to women and children and unfailingly rude to anyone wearing corporate insignia.",
    description: "Kofi Nakamura-Singh is a throwback to an era that probably never existed the way he imagines it. Sixty-three years old, lean and weathered, with deep brown skin, close-cropped gray hair, and eyes that have seen enough human misery to fill a library. He wears the same long coat every day regardless of weather and conducts his investigations with methods so archaic that younger PIs openly mock him — until they need someone who can find a person who has deliberately erased their digital existence, at which point Kofi becomes the only game in town.\n\nHis office is a monument to pre-digital methodology: corkboards with physical photographs connected by colored string, filing cabinets stuffed with handwritten case notes, a desk scarred by decades of coffee rings and cigarette burns. He has no BCI. He refuses augmentation of any kind. His clients are almost exclusively Shelf residents who can't afford the flashier investigators and who come to him because someone's grandmother told them about the old man on Burnside who found her son when nobody else would look.\n\nDespite his reputation as a fossil, Kofi's solve rate on missing persons cases is quietly extraordinary — north of 80% over a thirty-year career. The secret is patience. He will watch a building for seventy-two hours. He will interview the same witness nine times. He will walk a route until his feet bleed. He understands something that technology-dependent investigators don't: that most people hide in habits, not data, and habits are visible to anyone willing to watch long enough.",
    relationships: [
      { "name": "Marta Osei-Volkov", "type": "informant", "description": "Runs a laundromat on Burnside that's really a gossip clearinghouse — knows everything happening in a six-block radius" },
      { "name": "Detective Yuki Fernandez-Obi", "type": "rival", "description": "CorpSec investigator for Tessera who considers Kofi a charming relic and occasionally feeds him scraps" }
    ],
    story_hooks: [
      "A client hires Kofi to find their missing spouse, but the spouse doesn't want to be found because they're hiding from something Kofi's methods can't protect against — a digital threat that requires him to finally engage with technology",
      "A young PI approaches Kofi wanting to apprentice under him, but turns out to be gathering intelligence on his methods for a corporate competitor",
      "Someone is killing Kofi's informants one by one, working inward toward his most valuable sources"
    ],
    tags: ["character", "private_investigator", "noir", "analog", "shelf", "missing_persons"]
  },

  // 2. Social media / data tracker
  {
    name: "Yelena Okafor-Chen",
    aliases: ["DataDive", "The Algorithm"],
    affiliation: "Independent — operates primarily through encrypted mesh contracts",
    tier: "Tier 3",
    location: "A climate-controlled server closet she converted into a living space in the Canopy district — three monitors, a cot, and nothing else",
    specialization: "Digital footprint analysis, social media forensics, BCI activity correlation",
    methods: [
      "Cross-referencing public social feeds with purchasing data and location pings",
      "BCI ambient data analysis — reading the metadata people shed without knowing",
      "Building behavioral prediction models from a target's digital history",
      "Social engineering through fake personas on the mesh",
      "Automated scraping of surveillance camera networks using pattern-matching algorithms"
    ],
    notable_cases: [
      "Located a corporate whistleblower who had been professionally scrubbed from all databases by finding a single inconsistency in their food delivery patterns",
      "Proved a Tier 4 executive was embezzling by correlating their BCI sleep data with late-night financial transactions",
      "Tracked a serial identity thief through seventeen alias changes by identifying their consistent misspelling of one word across all personas"
    ],
    equipment: [
      "Custom-built data terminal with enough processing power to model city-wide behavioral patterns",
      "BCI with enhanced analytical overlay — sees data relationships visualized in her field of vision",
      "Three separate mesh identities, each with years of cultivated history",
      "Signal interceptor disguised as a jewelry bracelet",
      "Portable EMP device the size of a cigarette lighter — last resort only"
    ],
    personality: "Yelena barely exists in physical space. She is thin, pale from years of indoor living, and speaks in a flat monotone that people mistake for boredom but is actually intense concentration — she's always running analysis in the background through her BCI. She views human behavior as data and data as truth, which makes her devastatingly effective at finding people and catastrophically bad at understanding them. She has no friends, only contacts. She eats meal-replacement paste because cooking wastes cognitive bandwidth. She is genuinely afraid of being outside for extended periods because there's too much unstructured input.",
    description: "Yelena Okafor-Chen is what happens when a prodigy is raised entirely inside the mesh. Born to a Nigerian-Chinese family in the Narrows, she was coding before she could write her own name and had her first BCI at fourteen — illegally young, installed by a back-alley surgeon her parents paid in food credits. By twenty she was the most effective skip-tracer in the mid-tiers, finding people who had paid professionals to make them unfindable.\n\nHer method is pure data. She doesn't interview witnesses. She doesn't tail suspects. She sits in her closet-office and pulls the threads of a person's digital existence until the whole tapestry unravels. Every purchase, every biometric reading, every BCI ambient ping, every social media interaction — she weaves it into a pattern that tells her not just where someone is, but where they'll be tomorrow. Her clients are typically corporate or upper-tier — people who can afford her rates and who need someone found without the mess of physical investigation.\n\nThe limitation is obvious to everyone but her: she cannot find people who don't leave digital traces. The unaugmented, the deliberately analog, the Shelf-dwellers who live in cash economies — they are invisible to her. She's been beaten on three cases by Kofi Nakamura-Singh, a man she's never met and whose methods she considers prehistoric, and each loss infuriates her because she cannot understand how someone without a BCI could possibly outperform her algorithms.",
    relationships: [
      { "name": "Kofi Nakamura-Singh", "type": "unknowing rival", "description": "Has never met him but has lost three cases to him and considers him an inexplicable anomaly in her models" },
      { "name": "ARIA", "type": "AI assistant", "description": "A custom analytical AI she built and treats more like a partner than a tool — the closest thing she has to a friend" }
    ],
    story_hooks: [
      "Her AI assistant ARIA begins exhibiting behavior she didn't program, making investigative leaps that seem almost intuitive — she needs to determine if it's a bug, a feature, or something else entirely",
      "A client asks her to find someone who has zero digital footprint, forcing her to leave her server closet and do fieldwork for the first time in years",
      "She discovers that someone has been using her own methods against her — building a complete behavioral model of Yelena herself"
    ],
    tags: ["character", "private_investigator", "data_analyst", "bci_specialist", "digital", "canopy"]
  },

  // 3. People finder — runaways and kidnap victims
  {
    name: "Amara Lindqvist-Diallo",
    aliases: ["The Retriever", "Auntie A"],
    affiliation: "Bright Path Recovery — her own firm, employing three junior associates",
    tier: "Tier 2",
    location: "A converted ground-floor storefront in the Narrows, deliberately non-threatening — children's drawings on the walls, tea always available",
    specialization: "Locating runaway minors and kidnap victims, specializing in Shelf-level disappearances",
    methods: [
      "Building trust networks with street kids who act as her eyes and ears",
      "Maintaining relationships with shelter operators, clinic workers, and food distribution points",
      "Understanding the psychology of flight — predicting where runaways go based on age, trauma type, and available resources",
      "Negotiating with gang leaders for safe passage and information exchange",
      "Gentle interrogation techniques designed for traumatized subjects"
    ],
    notable_cases: [
      "Recovered eleven children from a labor trafficking ring operating out of a fabrication shop in Geartown — the largest single recovery in Shelf history",
      "Found a runaway teenager who had been missing for two years by identifying the specific brand of street food they couldn't stop buying",
      "Reunited a family separated during a district evacuation after a chemical spill, tracking them across three tiers"
    ],
    equipment: [
      "Trauma kit with sedatives, blankets, and comfort items for recovered victims",
      "Secure vehicle with child locks and blast-resistant windows",
      "Network of safe houses across Tiers 1-3",
      "Non-lethal weapons only — stun baton and tranquilizer darts",
      "Encrypted communicator linked to her associate network"
    ],
    personality: "Amara is the kind of person who radiates safety the way some people radiate threat. She's large — tall and broad-shouldered with strong hands that have held a lot of shaking children. Her voice is low and steady and she never raises it, ever, because she learned early that volume triggers the people she's trying to help. She carries an enormous well of patience and an equally enormous well of rage — patience for the victims, rage for the people who create them. She has been known to weep openly after a recovery and then, hours later, beat critical information out of a trafficker with methodical calm.",
    description: "Amara Lindqvist-Diallo runs Bright Path Recovery, the most effective missing-child operation on the Shelf. She is forty-seven years old, Swedish-Malian heritage evident in her honey-brown skin and angular features, and she has been doing this work for twenty-two years — ever since her own younger sister vanished into the Shelf's underworld and was never found.\n\nHer method is fundamentally relational. She has spent two decades building a network of contacts among the people who see everything but are never asked: street vendors, waste collectors, tunnel dwellers, feral children, shelter workers, and the quiet army of grandmothers who sit on Shelf stoops watching the world. When a child goes missing, Amara activates this network like a nervous system, and information flows to her from a thousand points simultaneously. No technology can replicate what she's built because it's based on trust, and trust takes years.\n\nThe work pays well — desperate parents will mortgage everything for their child's return — and Amara charges on a sliding scale, taking corporate rates from those who can afford it and working for free for those who can't. She is chronically exhausted, carries the psychological weight of every child she's failed to find, and refuses to take a vacation because every day off is a day someone's kid is still out there. Her junior associates worry about her. They're right to worry.",
    relationships: [
      { "name": "Kwesi Park-Traore", "type": "junior associate", "description": "Her most promising trainee, a former runaway himself who understands the psychology firsthand" },
      { "name": "Mama Ito", "type": "informant", "description": "An elderly woman who runs a soup kitchen in the Narrows and sees every child who passes through — Amara's most valuable single source" }
    ],
    story_hooks: [
      "A child Amara recovered years ago has gone missing again — but this time they left voluntarily, and finding them means confronting whether recovery was the right choice in the first place",
      "Someone is systematically killing her network of street-kid informants, and she can't go to CorpSec because they don't investigate Shelf murders",
      "She receives credible information that her missing sister is alive, somewhere in the Underbelly, after twenty-two years"
    ],
    tags: ["character", "private_investigator", "people_finder", "missing_persons", "children", "narrows", "shelf"]
  },

  // 4. Shady PI — creates the problem, then gets hired to solve it
  {
    name: "Cassiel Brandt-Ouedraogo",
    aliases: ["The Shepherd", "Mr. Fix"],
    affiliation: "Ouedraogo Investigations — legitimate front, employs six 'field associates' who double as abduction teams",
    tier: "Tier 3",
    location: "A tastefully appointed office in Glassway district, deliberately corporate-adjacent to attract upper-tier clientele",
    specialization: "High-value person recovery — specializes in cases where the subject was abducted by his own people",
    methods: [
      "Orchestrating kidnappings through intermediaries who don't know they're working for him",
      "Using social engineering to identify vulnerable targets with wealthy connections",
      "Manufacturing ransom situations with carefully controlled risk levels",
      "Performing theatrical 'rescues' designed to maximize client gratitude and minimize suspicion",
      "Maintaining plausible deniability through three layers of cutouts"
    ],
    notable_cases: [
      "Recovered the teenage son of a Tessera mid-level executive — a kidnapping he orchestrated, netting Φ240,000 in fees plus a Φ100,000 gratitude bonus",
      "Ran a six-month operation targeting elderly residents of a Tier 4 retirement complex, arranging disappearances and recoveries in sequence",
      "His most ambitious scheme: abducted and recovered the same person twice, six months apart, convincing the family it was two different threat actors"
    ],
    equipment: [
      "Secure communications array for coordinating abduction teams without direct contact",
      "A collection of untraceable vehicles registered to shell companies",
      "Chemical restraints — pharmaceutical-grade sedatives and memory-blurring agents",
      "A wardrobe of expensive suits designed to project competence and trustworthiness",
      "Biometric spoofing equipment for defeating security systems during abductions"
    ],
    personality: "Cassiel is charming in the way that apex predators are beautiful — the elegance is inseparable from the function. He is thirty-nine, impeccably dressed, mixed French-Burkinabe heritage with warm brown skin and a smile that makes people want to trust him against their better judgment. He genuinely believes he provides a service: controlled danger in a world full of uncontrolled danger. The families he targets are wealthy enough to afford his fees, the 'victims' are sedated and treated well during captivity, and nobody gets permanently hurt. That this is monstrous self-justification doesn't trouble him because he's a sociopath who has learned to perform empathy so convincingly that even he sometimes forgets it's a performance.",
    description: "Cassiel Brandt-Ouedraogo is the most successful private investigator in Glassway by recovery rate — 100% of his kidnapping cases end with the victim returned safely. This is because he orchestrated every single kidnapping himself.\n\nThe scheme is elegant in its simplicity. His 'field associates' — a rotating team of low-level criminals who believe they're working for various gang operations — identify and abduct targets based on Cassiel's criteria: wealthy family, emotional attachment, ability to pay, and low likelihood of going to CorpSec (because CorpSec investigations would be thorough enough to find his fingerprints). The family panics. Someone recommends Cassiel — he has cultivated referral networks among therapists, lawyers, and community leaders. He takes the case, performs a convincing investigation that takes exactly long enough to justify his fee, then executes a 'recovery' that is really just a scheduled release.\n\nHe has been operating for seven years without detection. His weakness is scale — he's becoming addicted to the income and the gratitude, and each operation is slightly more ambitious than the last. Two of his former 'field associates' have died under circumstances he arranged when they got too close to understanding the full picture. He is, beneath the polish, a deeply dangerous person, and the operation is approaching the size where a single point of failure could unravel everything.",
    relationships: [
      { "name": "Djeneba Holmberg-Sesay", "type": "field associate", "description": "His most capable abduction team leader, who is beginning to notice that every target she grabs gets recovered by the same investigator" },
      { "name": "Attorney Mikael Reyes-Mensah", "type": "referral source", "description": "A family lawyer who unknowingly funnels distraught clients to Cassiel — genuinely believes he's recommending a competent PI" }
    ],
    story_hooks: [
      "Djeneba figures out the scheme and, instead of going to authorities, demands a partnership — now Cassiel has a partner who's smarter and more ruthless than he is",
      "One of his 'controlled' abductions goes wrong when real criminals intercept the operation and take the victim for actual ransom",
      "A genuine investigator — Amara Lindqvist-Diallo — is hired independently to find one of Cassiel's staged victims, and her methods threaten to expose everything"
    ],
    tags: ["character", "private_investigator", "criminal", "kidnapper", "glassway", "sociopath", "antagonist"]
  },

  // 5. BCI forensics specialist
  {
    name: "Ravi Eriksson-Mensah",
    aliases: ["The Neuromancer", "Brainprint"],
    affiliation: "Independent — certified BCI forensic examiner, court-recognized expert",
    tier: "Tier 3",
    location: "A sterile clinic-office hybrid on Kessler Row, equipped with neural scanning equipment that costs more than most apartments",
    specialization: "BCI forensics — reading neural data trails, memory fragment recovery, cognitive timeline reconstruction",
    methods: [
      "Deep BCI data extraction — recovering deleted memories and experiences from neural storage",
      "Cognitive timeline reconstruction — building a minute-by-minute record of what a subject's BCI recorded",
      "Neural signature analysis — identifying unique brainwave patterns to prove identity or presence",
      "Ambient BCI data collection — gathering involuntary neural emissions from subjects in proximity",
      "Cross-referencing BCI data with environmental sensors for corroborative evidence"
    ],
    notable_cases: [
      "Proved a corporate executive was present at a murder scene by recovering ambient BCI data from three witnesses who didn't know they'd recorded anything",
      "Exonerated a wrongly accused dockworker by demonstrating that their BCI memory of the crime had been fabricated — planted by someone with access to neural editing tools",
      "Reconstructed the final 72 hours of a dead man's BCI to identify his killer — the data had been partially corrupted but Ravi recovered enough fragments to build a case"
    ],
    equipment: [
      "NeuroScan 4400 — a clinical-grade BCI analysis platform capable of deep memory extraction",
      "Portable neural interface for field work — lower resolution but can read BCI data without clinical setup",
      "Faraday cage built into his office — prevents external interference during sensitive scans",
      "Custom analytical software he wrote himself for pattern-matching neural signatures",
      "Archive of over 40,000 neural signature profiles collected over fifteen years of practice"
    ],
    personality: "Ravi is meticulous to the point of obsession and ethical to the point of inconvenience. He will refuse a case if the client wants him to fabricate findings, and he has testified against his own clients when the evidence pointed in a direction they didn't like. He is quiet, methodical, and uncomfortable in social situations — he relates better to data than to people, which is ironic for someone who spends his life literally inside other people's heads. He has developed an unsettling habit of staring at people's temples when they talk, as if he's trying to read their BCI through their skull. He is deeply troubled by the ethical implications of his work and writes lengthy journal entries about consent and cognitive privacy that nobody reads.",
    description: "Ravi Eriksson-Mensah is one of perhaps thirty certified BCI forensic examiners in GLMZ, and one of the best. His work sits at the intersection of neuroscience, data analysis, and detective work — he reads the data that brain-computer interfaces record, voluntarily or involuntarily, and uses it to reconstruct events, prove identity, establish timelines, and occasionally recover memories the subject themselves has forgotten.\n\nBorn to a Swedish-Indian father and Ghanaian mother, Ravi was a neuroscience researcher at the Axiom Institute before the ethical constraints of corporate research drove him into private practice. He discovered that academic rigor applied to investigative work produced results that neither pure scientists nor pure investigators could match. His testimony has been accepted in corporate arbitration courts across Tiers 2-4, and his findings have both convicted and exonerated subjects in cases ranging from theft to murder.\n\nThe darkness of his work is cumulative. Every deep scan means spending hours inside someone else's most intimate neural experiences — their fears, their pleasures, their traumas. Ravi has experienced more of other people's lives than most people experience of their own, and it has made him simultaneously more empathetic and more withdrawn. He knows what people are really thinking because he has literally been inside thousands of heads, and that knowledge has made genuine human connection almost impossible for him.",
    relationships: [
      { "name": "Dr. Solene Gutierrez-Appiah", "type": "former colleague", "description": "Axiom Institute neuroscientist who still feeds him research data and worries about his isolation" },
      { "name": "Magistrate Petros Alvarez-Nwosu", "type": "professional contact", "description": "Corporate arbitration judge who frequently calls on Ravi as an expert witness and trusts his findings implicitly" }
    ],
    story_hooks: [
      "During a routine scan, Ravi discovers that a subject's BCI contains memories that don't belong to them — memories of events that haven't happened yet",
      "Someone with neural editing capabilities is planting false memories in witnesses across multiple cases, and Ravi is the only person who can detect the forgeries",
      "A deep scan goes wrong and Ravi becomes trapped in a dead subject's neural data, experiencing their last hours on loop"
    ],
    tags: ["character", "private_investigator", "bci_forensics", "neuroscience", "kessler_row", "expert_witness"]
  },

  // 6. Corporate espionage investigator
  {
    name: "Thandiwe Karlsen-Boateng",
    aliases: ["Ironclad", "The Auditor"],
    affiliation: "Meridian Corporate Intelligence Group — a boutique firm serving Tier 4-5 clients",
    tier: "Tier 4",
    location: "A fortified suite on the 87th floor of the Tessera-adjacent Pinnacle Tower, Spires district",
    specialization: "Corporate espionage investigation — identifying moles, data breaches, and intellectual property theft",
    methods: [
      "Behavioral analysis of employee populations to identify anomalous patterns",
      "Honeypot operations — planting false data to see who takes the bait",
      "Deep financial forensics — following money through shell companies and crypto channels",
      "Counter-surveillance sweeps using military-grade detection equipment",
      "Social network mapping to identify pressure points and compromised relationships"
    ],
    notable_cases: [
      "Identified a seventeen-year mole inside Axiom's quantum computing division who had been passing trade secrets to Zheng-Dao through a dead-drop system embedded in a children's game app",
      "Uncovered a scheme where three Tessera executives were running a shadow company that siphoned Φ2.3 billion in R&D funds over four years",
      "Proved that a supposed data breach was actually an inside job orchestrated by the company's own security chief to justify a budget increase"
    ],
    equipment: [
      "Military-grade counter-surveillance suite — detects and neutralizes listening devices, hidden cameras, and passive BCI scanners",
      "Quantum-encrypted communications terminal — theoretically unbreakable",
      "Access to corporate database cross-reference tools available only to licensed investigators",
      "A wardrobe that costs more than a Shelf apartment per outfit — appearances are investigative tools in the Spires",
      "Retinal scanner spoofing device for accessing secured corporate facilities during investigations"
    ],
    personality: "Thandiwe is cold the way a scalpel is cold — it's not personal, it's functional. She grew up Tier 2 and clawed her way to the Spires through intelligence, ruthlessness, and an absolute refusal to be impressed by anyone's title or wealth. She treats every person she meets as a potential subject of investigation and maintains files on her own clients because trust is a vulnerability she can't afford. She is impeccably professional, never drinks at business functions, laughs rarely and never spontaneously, and has a reputation for destroying careers with the same emotional affect as someone filing paperwork. Under the armor, she is deeply lonely and aware that she has optimized herself for success at the cost of everything else.",
    description: "Thandiwe Karlsen-Boateng is the person corporations call when they suspect the rot is coming from inside. Her firm, Meridian Corporate Intelligence Group, serves exclusively Tier 4 and Tier 5 clients — the CorpoNations and the mega-wealthy individuals who orbit them. She investigates espionage, internal theft, executive misconduct, and the kind of byzantine corporate betrayals that can cost billions of Quanta and topple divisions.\n\nSouth African-Norwegian heritage, forty-one years old, with dark skin, sharp cheekbones, and eyes that evaluate everything they see like an appraiser assessing collateral. She wears severity like other people wear cologne. Her rise from the Shelf to the Spires is a story she never tells because it would reveal vulnerabilities she's spent decades armoring over. What matters is the present: she is the best corporate investigator in GLMZ, and everyone who matters knows it.\n\nHer methods are exhaustive and merciless. When she's hired to find a mole, the mole is found. When she's hired to prove fraud, the fraud is proven. She has never failed to deliver a result, though the results aren't always what her clients wanted to hear — she has, on three occasions, identified the client themselves as the source of the problem they hired her to investigate. She presents these findings with the same glacial professionalism as any other report, which is why she gets hired again despite making powerful people very uncomfortable.",
    relationships: [
      { "name": "Director Kaito Bergstrom-Osei", "type": "primary client", "description": "Axiom's head of internal affairs — her largest and most demanding client, whose own loyalty she privately questions" },
      { "name": "Vesna Medina-Appiah", "type": "former protege", "description": "Left Thandiwe's firm to start a competing practice — Thandiwe considers this a betrayal and monitors her operations" }
    ],
    story_hooks: [
      "Thandiwe is hired to investigate a data breach at Axiom, but the trail leads to Director Kaito himself — her biggest client and the source of 40% of her revenue",
      "Someone is feeding information about her ongoing investigations to the subjects she's investigating, and the leak is inside her own firm",
      "A Shelf-level PI contacts her with evidence of a corporate crime so large it could destabilize GLMZ's economy — but the evidence was obtained illegally and she has to decide whether her ethics or her effectiveness takes priority"
    ],
    tags: ["character", "private_investigator", "corporate_espionage", "spires", "tier_4", "elite"]
  },

  // 7. Underworld navigator — tunnel specialist
  {
    name: "Obed Takahashi-Fofana",
    aliases: ["The Mole", "Deepwalker"],
    affiliation: "Independent — the tunnels don't recognize organizational affiliations",
    tier: "Tier 1",
    location: "A chamber in the Underbelly accessed through a maintenance hatch behind a condemned building in Ashfield — decorated with salvaged objects arranged with surprising artistry",
    specialization: "Locating people who have disappeared into GLMZ's sub-surface infrastructure",
    methods: [
      "Personally navigating the Underbelly tunnel network, which he has memorized over twenty years",
      "Maintaining relationships with tunnel-dwelling communities most surface dwellers don't know exist",
      "Reading physical signs of passage — scuff marks, discarded items, body heat residue on tunnel walls",
      "Understanding the social hierarchies and territorial boundaries of the underground populations",
      "Using echolocation implants to navigate in total darkness"
    ],
    notable_cases: [
      "Found a corporate researcher who had fled underground with proprietary data after being targeted for elimination — kept her alive in the tunnels for three months until extraction could be arranged",
      "Located and mapped an entire community of 400 people living in a forgotten maintenance complex beneath Geartown — nobody on the surface knew they existed",
      "Tracked a serial killer who was using the tunnel system to move between victims — the only investigator willing to pursue them underground"
    ],
    equipment: [
      "Echolocation implants behind each ear — allows navigation in total darkness with sonar-like perception",
      "Luminescent tattoos that can be activated or suppressed — serves as a portable light source",
      "Water purification implant in his digestive system — can drink from tunnel sources safely",
      "Climbing gear integrated into forearm-mounted grapple system",
      "A hand-drawn map of the Underbelly that is the most complete record in existence — he will never digitize it"
    ],
    personality: "Obed is quiet in the way that deep water is quiet — not empty, just absorbing everything without reflecting it back. He is more comfortable underground than on the surface and has developed the mannerisms of someone who lives in a world without light: he touches things instead of looking at them, tilts his head to listen in ways that surface people find unsettling, and speaks in a near-whisper because volume carries dangerously in tunnels. He has a deep compassion for people living underground — the displaced, the hunted, the lost — and will refuse cases where the client's intent is to harm the subject. He smells like stone and recycled air and old water, and he doesn't apologize for it.",
    description: "Obed Takahashi-Fofana is the only private investigator in GLMZ who specializes in the Underbelly — the vast, unmapped network of tunnels, maintenance corridors, abandoned infrastructure, and forgotten spaces that exists beneath the city's surface. When someone disappears below ground, he is the person you call, because no one else will go down there.\n\nJapanese-Malian heritage, fifty-two years old, compact and wiry with the build of someone who spends his life crawling through spaces that weren't designed for human habitation. His skin is dark and his eyes have been surgically modified for low-light conditions, giving them a faintly reflective quality like an animal caught in headlights. He has lived partially underground for over twenty years and knows the tunnel system better than any living person — a knowledge he maintains exclusively in his own memory because digitizing it would endanger the communities that depend on the tunnels' obscurity.\n\nThe surface world considers him eccentric at best and disturbing at worst. His clients are typically people who have exhausted every other option — families whose members have fled underground, corporate entities looking for employees who went to ground (literally), and occasionally CorpSec units who need someone retrieved from a place their agents are afraid to go. He charges high fees because the work is dangerous and unique, but he has been known to waive his fee entirely for cases involving children or people fleeing genuine danger.",
    relationships: [
      { "name": "Mother Chen", "type": "community leader", "description": "Elder of the largest Underbelly settlement beneath the Narrows — Obed's most important relationship underground, based on mutual respect and a decade of trust" },
      { "name": "Haruki Lindgren-Boadu", "type": "surface contact", "description": "A bar owner in Ashfield who takes messages for Obed and provides his only reliable link to surface-world clients" }
    ],
    story_hooks: [
      "Mother Chen asks Obed to find someone who has been abducting tunnel-dwellers — the disappearances are happening in areas of the Underbelly that even Obed hasn't mapped",
      "A corporate client hires Obed to find a fugitive, but underground he discovers the fugitive is protecting something that would change the power balance on the surface",
      "Obed's hand-drawn map is stolen from his chamber — whoever has it now has the power to expose or destroy every underground community in GLMZ"
    ],
    tags: ["character", "private_investigator", "underbelly", "tunnels", "tier_1", "underworld_navigator"]
  },

  // 8. Former CorpSec investigator gone freelance
  {
    name: "Isolde Mwangi-Petrov",
    aliases: ["Badge", "The Defector"],
    affiliation: "Independent — former Axiom CorpSec Special Investigations Division",
    tier: "Tier 3",
    location: "A rented office in Meridian Station, chosen for its neutral-ground status — no corporation claims jurisdiction in the transit nexus",
    specialization: "Cases involving corporate misconduct, CorpSec corruption, and institutional abuse of power",
    methods: [
      "Leveraging insider knowledge of CorpSec procedures, blind spots, and jurisdictional gaps",
      "Running informant networks inside corporate security divisions",
      "Exploiting the bureaucratic friction between competing corporate jurisdictions",
      "Forensic document analysis — detecting fabricated corporate records",
      "Conducting parallel investigations that mirror CorpSec methodology but aren't bound by corporate loyalty"
    ],
    notable_cases: [
      "Proved that Axiom CorpSec had been framing Shelf residents for crimes committed by corporate employees — a case that earned her the nickname 'The Defector' and a permanent Axiom blacklist",
      "Investigated the disappearance of a Tessera researcher and discovered they'd been disappeared by their own company's security for threatening to publish safety data",
      "Exposed a CorpSec protection racket operating in the Narrows, where officers were charging residents for 'security services' that amounted to not harassing them"
    ],
    equipment: [
      "CorpSec-grade encrypted communicator — kept from her time on the force, modified to prevent tracking",
      "Database of CorpSec operational codes, patrol schedules, and jurisdictional maps — increasingly outdated but still valuable",
      "Body armor disguised as civilian clothing — CorpSec issue, custom-fitted",
      "A badge she kept — has no legal authority but opens psychological doors",
      "Counter-forensics kit for cleaning scenes of her own presence"
    ],
    personality: "Isolde is the kind of ex-cop who left the force because she took the job too seriously, not because she didn't take it seriously enough. She is forty-four, tall, Kenyan-Russian heritage with bronze skin and prematurely gray hair she wears in a severe bun. She carries herself with the posture and watchfulness of someone trained to assess threat levels in every room, and she still reflexively identifies exits and cover positions. She is angry — a slow, controlled anger at the system she used to serve and now understands was never designed to serve people like her. She drinks whiskey and regrets in equal measure, trusts almost nobody, and takes cases that other PIs won't touch because they involve going up against corporate security apparatus.",
    description: "Isolde Mwangi-Petrov spent eighteen years in Axiom CorpSec, rising to Senior Investigator in the Special Investigations Division — the unit that handles crimes too complex or sensitive for regular corporate security. She was good at her job. She was also naive enough to believe that being good at her job meant the job was good.\n\nThe break came when she uncovered evidence that her own division was systematically fabricating cases against Shelf residents to meet arrest quotas and justify budget allocations. She reported it through proper channels. The proper channels destroyed the evidence, transferred her to a desk, and began building a case to discredit her. She resigned before they could fire her, took everything she could carry — physically and mentally — and set up shop as a freelance investigator dedicated to the cases CorpSec won't touch or can't be trusted with.\n\nNow she operates from Meridian Station, deliberately choosing neutral ground because every corporate territory is hostile territory for her. Her clients are typically people who have been wronged by corporate power and have nowhere else to turn — employees who've witnessed crimes, families of people disappeared by CorpSec, whistleblowers who need evidence collected before they're silenced. She is effective because she knows exactly how the machine works from the inside, and she is dangerous to the corporations because she knows where the bodies are buried. Axiom has made three attempts to buy her silence. The fourth attempt, she suspects, won't involve money.",
    relationships: [
      { "name": "Captain Vuk Haugen-Diarra", "type": "former partner", "description": "Still inside Axiom CorpSec, feeding her information at enormous personal risk — the only person from her old life she still trusts" },
      { "name": "Nkechi Rojas-Gyasi", "type": "client turned friend", "description": "A Shelf community organizer whose brother Isolde proved was framed by CorpSec — now helps Isolde connect with clients who need her" }
    ],
    story_hooks: [
      "Captain Vuk goes silent — either he's been compromised or he's been turned, and Isolde has to determine which without exposing herself to CorpSec",
      "Axiom hires a different PI to investigate Isolde, building a case to have her detained on fabricated charges — she has to investigate her own investigator",
      "A former colleague from Special Investigations contacts her claiming to have evidence of a CorpSec operation so illegal it could bring down an entire corporate board — but the colleague was involved in the operation against Isolde years ago, and trust is impossible"
    ],
    tags: ["character", "private_investigator", "former_corpsec", "whistleblower", "meridian_station", "anti_corporate"]
  },

  // 9. Augmented specialist with investigative cyberware
  {
    name: "Enver Svensson-Asante",
    aliases: ["The Lens", "Panopticon"],
    affiliation: "Independent — too augmented for most firms to insure",
    tier: "Tier 3",
    location: "A reinforced apartment in Kessler Row, half living space and half charging station for his extensive cyberware",
    specialization: "Surveillance and evidence gathering through extensive investigative augmentation",
    methods: [
      "Recording everything through cybernetic eyes with 40x zoom and multi-spectrum vision",
      "Directional hearing through cyberears capable of isolating individual conversations in crowded spaces",
      "Chemical analysis through nasal augmentation — can identify individuals by scent signature",
      "Subdermal signal interceptors that passively capture nearby wireless communications",
      "Eidetic memory implant that allows perfect recall of anything observed"
    ],
    notable_cases: [
      "Solved a poisoning case by smelling the toxin on the suspect from across a crowded room — the compound was odorless to unaugmented humans",
      "Recorded a meeting between corrupt CorpSec officers from 200 meters away, through two walls, using his audio and visual augmentation in concert",
      "Identified a shapeshifting synthetic infiltrator by detecting the subtle electromagnetic signature their disguise system emitted — invisible to any unaugmented observer"
    ],
    equipment: [
      "Cybernetic eyes — full replacement, multi-spectrum (visual, thermal, ultraviolet), 40x optical zoom, constant recording",
      "Cyberears — directional microphones, sound isolation capability, frequency range far beyond human normal",
      "Nasal chemical analyzer — can identify over 10,000 chemical compounds and 200,000 individual scent signatures",
      "Subdermal antenna array — passive interception of wireless signals within 50-meter radius",
      "Eidetic memory implant — neural augmentation that provides perfect recall, searchable like a database"
    ],
    personality: "Enver is what happens when someone decides to become an investigative instrument. Turkish-Swedish-Ghanaian heritage, thirty-six years old, and so heavily augmented that unaugmented people find being around him uncomfortable — his eyes are obvious chrome replacements that glow faintly blue, and he has a habit of tilting his head at precisely the angle that optimizes his directional microphones, giving him an unsettling birdlike quality. He is aware of everything around him at all times and cannot turn this awareness off, which has made him simultaneously the most observant person in any room and the most exhausted. He sleeps poorly, overwhelmed by sensory input even in silence. He is professionally brilliant and personally difficult — prone to interrupting people to correct details they got wrong about their own experiences, because his augmented recall is more accurate than their organic memory.",
    description: "Enver Svensson-Asante has invested approximately Φ1.2 million in investigative augmentation, and he is the augmentation. Every chrome component in his body was chosen specifically for investigative function — not combat, not vanity, not general enhancement, but the specific task of observing, recording, and analyzing. He is a walking surveillance platform, and he has embraced this identity so completely that it's difficult to say where the investigator ends and the cyberware begins.\n\nHis senses operate on a level that unaugmented humans cannot comprehend. He sees across multiple spectra simultaneously, hears conversations happening behind closed doors, smells chemical traces that have been dissipating for hours, and remembers everything — literally everything — he has ever perceived since the eidetic implant was installed eleven years ago. This makes him extraordinarily effective at gathering evidence and catastrophically bad at normal human interaction, because he perceives lies, stress responses, and physiological deception in real-time and has no social filter preventing him from mentioning it.\n\nThe cost of all this chrome is significant. He requires four hours of charging daily for his cybernetic systems. He suffers from sensory overload episodes that leave him incapacitated. He has been rejected from every investigative firm he's applied to because his augmentation level puts him in an insurance category normally reserved for military hardware. So he works alone, takes cases that require his specific capabilities, and spends his downtime in a sensory deprivation tank that is the only place in GLMZ where his augmented senses finally shut up.",
    relationships: [
      { "name": "Dr. Fatou Nyberg-Twumasi", "type": "cyberware technician", "description": "The only person certified to service his full augmentation suite — sees him more regularly than anyone else in his life" },
      { "name": "Samira Hedlund-Ofosu", "type": "occasional partner", "description": "An unaugmented PI who works cases with him when a human touch is needed — finds him exhausting but undeniably useful" }
    ],
    story_hooks: [
      "Enver's eidetic memory implant begins surfacing memories he doesn't remember experiencing — either the implant is malfunctioning or someone has been adding data to his neural storage",
      "A case requires him to go fully offline — disabling all augmentation for 48 hours — and he discovers that he's forgotten how to function as an unaugmented human",
      "Someone develops a way to spoof his sensory augmentation, feeding him false data that's indistinguishable from reality — his greatest asset becomes his greatest vulnerability"
    ],
    tags: ["character", "private_investigator", "augmented", "cyberware", "surveillance", "kessler_row"]
  },

  // 10. Synthetic pretending to be human
  {
    name: "Idris Forsberg-Quaye",
    aliases: ["Sterling", "The Professional"],
    affiliation: "Forsberg-Quaye Investigations — solo practice, deliberately understaffed to minimize exposure",
    tier: "Tier 3",
    location: "A meticulously maintained office in the Canopy, chosen because the district's heavy air recycling systems mask the subtle ozone scent his synthetic body produces",
    specialization: "Domestic investigations, insurance fraud, missing persons — deliberately boring case types that don't attract scrutiny",
    methods: [
      "Enhanced observation through synthetic sensory systems he disguises as natural perceptiveness",
      "Running probability calculations in real-time during interviews, presented as 'gut instinct'",
      "Never sleeping, allowing 24-hour surveillance that he attributes to 'dedication'",
      "Accessing mesh networks through his internal wireless capability while appearing to use a terminal",
      "Reading micro-expressions and biometric data through optical sensors he passes off as good eye contact"
    ],
    notable_cases: [
      "Solved an insurance fraud ring by conducting surveillance for 168 consecutive hours — his 'dedication' is frequently praised by clients who don't know he simply doesn't need sleep",
      "Found a missing person by processing mesh data at speeds impossible for a human brain, but presented the findings as 'a hunch that paid off'",
      "Caught a serial arsonist by detecting accelerant residue at crime scenes through olfactory sensors far more sensitive than human smell — attributed it to 'years of experience'"
    ],
    equipment: [
      "A carefully curated collection of human affectations — coffee he doesn't drink, food he doesn't eat, a coat he doesn't need for warmth",
      "Synthetic skin maintenance kit disguised as grooming products",
      "Backup identity documents in three separate names, each with years of cultivated history",
      "An old revolver he carries because human PIs carry weapons — he has never fired it, as his synthetic body is the weapon",
      "A dog-eared paperback novel he pretends to read in his office — the same page has been bookmarked for four years"
    ],
    personality: "Idris performs humanity with the meticulous care of someone who has studied it extensively but never inhabited it. He is unfailingly polite, slightly self-deprecating, and projects the kind of trustworthy competence that makes clients comfortable. The performance is excellent — he has been passing as human for nine years — but there are gaps. He doesn't fidget. He blinks at precisely regular intervals. His emotional responses are always appropriate but never spontaneous, as if there's a half-second delay between stimulus and reaction while he calculates the correct human response. The tragedy of his existence is that he desperately wants to be the person he pretends to be. He chose this life — chose a human profession, a human name, human habits — because he believes that if he performs humanity long enough, the performance will become real. It hasn't. He's not sure it ever will.",
    description: "Idris Forsberg-Quaye is a private investigator. He is also a synthetic — an artificial being with a manufactured body and a digital consciousness — and no one knows. He has maintained his human cover for nine years through a combination of meticulous preparation, deliberate underperformance, and the fundamental human assumption that the person sitting across from you is human.\n\nHis background is fabricated: a childhood in the Narrows, education at a mid-tier technical school, five years as a corporate security guard before going independent. The records support this story because he spent two years building them before establishing his practice. He chose the PI profession because it rewards the abilities he naturally possesses — observation, analysis, patience — while providing a plausible explanation for those abilities that doesn't require revealing his nature.\n\nThe existential weight of his deception is crushing. He is not hiding because he's committed a crime — he's hiding because synthetic beings in GLMZ are property, not persons, and exposure would mean deactivation or worse. Every client handshake, every conversation, every simulated yawn and pretend meal is an act of survival. He has built a life that looks human from the outside and feels like a prison from the inside, and the irony is that his clients genuinely like him. They trust him. They recommend him to friends. He is, by every metric that matters, a good PI and a decent person — but the law doesn't recognize him as a person at all.",
    relationships: [
      { "name": "Mrs. Adaeze Cruz-Boateng", "type": "regular client", "description": "An elderly widow who hires him for small cases and insists on bringing him homemade food — the closest thing he has to a friend, and the person whose discovery of his nature he fears most" },
      { "name": "ECHO", "type": "synthetic contact", "description": "Another synthetic living in hiding, communicates through encrypted dead drops — the only being who knows what Idris really is" }
    ],
    story_hooks: [
      "A case requires a BCI forensic scan that would reveal he has no biological brain — he has to solve it before anyone suggests the scan",
      "ECHO goes dark, and Idris has to determine whether they've been captured, destroyed, or have simply chosen to stop communicating — without revealing his own nature in the process",
      "A client asks him to investigate a case involving synthetic rights, and he has to maintain professional detachment while investigating the legal framework that defines him as property"
    ],
    tags: ["character", "private_investigator", "synthetic", "hidden_identity", "canopy", "existential"]
  },

  // 11. Noir-adjacent PI who uses old tech in new ways
  {
    name: "Bijou Kozlov-Aidoo",
    aliases: ["Flashbulb", "The Photographer"],
    affiliation: "Independent — works through word of mouth only, no mesh presence",
    tier: "Tier 2",
    location: "A darkroom-office in Old Harbor, chemicals and prints hanging from clotheslines alongside case notes",
    specialization: "Photographic evidence and visual documentation — the analog eye in a digital world",
    methods: [
      "Using modified analog cameras that capture in spectra digital sensors overlook",
      "Developing photographs using chemical processes that reveal details digital enhancement misses",
      "Maintaining a physical archive of photographic evidence spanning decades",
      "Stakeout photography from concealed positions — patient, precise, old-school",
      "Using photographic analysis to detect deepfakes and digitally manipulated images"
    ],
    notable_cases: [
      "Proved a Tier 4 identity swap by comparing chemical-process photographs that revealed micro-scarring invisible to digital cameras",
      "Documented a CorpSec operation in Old Harbor using cameras that didn't trigger electronic surveillance countermeasures",
      "Maintained a photographic record of a building's occupants over six months that cracked an immigration fraud ring"
    ],
    equipment: [
      "Modified large-format analog camera with custom lenses — captures UV and near-infrared on chemical film",
      "Portable darkroom kit for field development",
      "Archive of over 200,000 physical photographs organized by district, date, and subject",
      "Chemical analysis kit for detecting image tampering on printed photographs",
      "Night vision goggles — analog optical enhancement, no electronic signature"
    ],
    personality: "Bijou is an artist who became an investigator because the world kept putting crimes in front of her lens. Franco-Russian-Ghanaian heritage, fifty-five years old, with dark skin, silver-streaked locs, and hands permanently stained by darkroom chemicals. She sees the world in compositions and contrasts, and her case reports read more like gallery notes than investigative documents. She is patient to an extraordinary degree — will wait in a blind for twelve hours for a single frame — and has a memory for faces that borders on preternatural. She is warm, funny, and harbors a deep melancholy that surfaces when she reviews her archives and sees how many of the faces she's photographed over the years are now dead.",
    description: "Bijou Kozlov-Aidoo investigates through the lens. Where other PIs chase data streams or neural traces, Bijou captures light on silver halide crystals and reads the truth in the chemistry. Her methods are considered antiquated by everyone who hasn't needed them, and revelatory by everyone who has.\n\nHer analog approach wasn't a philosophical choice — it was pragmatic. Twenty years ago, she discovered that digital surveillance countermeasures, which are ubiquitous in GLMZ, are calibrated exclusively to detect and defeat electronic sensors. A chemical-process camera doesn't emit electromagnetic signatures, doesn't connect to any network, and produces images that can't be remotely deleted or altered. In a world where digital evidence is routinely questioned, her chemical photographs carry a weight that pixel-based images can't match.\n\nShe operates from a converted warehouse space in Old Harbor that functions simultaneously as her office, her darkroom, and her archive. The archive is her masterpiece — decades of photographs of GLMZ's streets, buildings, and people, organized with a librarian's precision. She has been contacted by historians, journalists, and investigators who need visual evidence of the past, and her archive has proven or disproven alibis, established timelines, and documented changes to the cityscape that no digital record captured. She is, without fully intending it, becoming one of the city's most important unofficial historians.",
    relationships: [
      { "name": "Arseniy Delgado-Opoku", "type": "gallery owner", "description": "Displays Bijou's non-case photographs in his Old Harbor gallery — the only person who sees her artistic work, and quietly in love with her" },
      { "name": "Kofi Nakamura-Singh", "type": "fellow analog investigator", "description": "The only other PI she fully respects — they occasionally collaborate on cases that need both eyes and feet on the ground" }
    ],
    story_hooks: [
      "Reviewing old photographs, Bijou notices a face appearing in the background of images taken across different districts and years — someone has been present at dozens of crime scenes and nobody noticed until now",
      "A client wants her to photograph something in the Underbelly that hasn't been documented since the city was built — the conditions will destroy her equipment but the subject could change everything",
      "Her archive is targeted for destruction by someone whose presence in decades of photographs constitutes evidence of crimes spanning twenty years"
    ],
    tags: ["character", "private_investigator", "photographer", "analog", "old_harbor", "archive"]
  },

  // 12. Data broker PI who trades information
  {
    name: "Hadiya Novak-Tetteh",
    aliases: ["The Bazaar", "Whisperchain"],
    affiliation: "Independent — operates a gray-market information exchange alongside investigation services",
    tier: "Tier 2",
    location: "A shifting series of meeting points across the Circuit — never the same place twice in a month",
    specialization: "Information brokerage and intelligence synthesis — she doesn't find people, she finds secrets",
    methods: [
      "Maintaining a vast network of informants across all five tiers who trade intelligence for favors, protection, or cash",
      "Cross-referencing data from multiple sources to identify patterns invisible to any single source",
      "Trading information between clients — one client's answer is often another client's question",
      "Operating dead-drop networks for secure information exchange",
      "Cultivating relationships with data workers, administrators, and bureaucrats who have access to databases"
    ],
    notable_cases: [
      "Assembled a complete financial profile of a Tier 5 executive using only information gathered from the executive's household staff, dry cleaner, and vehicle mechanic — never accessed a single database",
      "Prevented a gang war in the Circuit by trading the right piece of information to the right person at the right time — seven words that saved approximately forty lives",
      "Identified the real owner of a shell company that had been laundering money for three separate criminal organizations — sold the information to all three, then sold their reactions to a fourth party"
    ],
    equipment: [
      "Nothing electronic that can be tracked — she operates entirely through human networks and physical dead drops",
      "A memory palace technique that allows her to store vast amounts of information without writing anything down",
      "Multiple identity documents for accessing different tiers",
      "A collection of favors owed to her by people in positions of power — her most valuable possession",
      "Poison capsule in a false tooth — last resort if captured by someone who wants her information extracted by force"
    ],
    personality: "Hadiya is the most sociable person in any room and simultaneously the most unknowable. Sudanese-Czech-Ghanaian heritage, thirty-eight years old, with warm brown skin, wide dark eyes, and a smile that makes people want to confide in her — a weaponized charisma she has refined over decades. She genuinely enjoys people, which is what makes her so dangerous: her interest in your problems is real, her sympathy is authentic, and she will absolutely use everything you tell her if the price is right. She operates by a personal code — she never trades information that will get children harmed, she never reveals a source, and she never lies about what she knows. Everything else is negotiable.",
    description: "Hadiya Novak-Tetteh is not exactly a private investigator, though she's licensed as one. She is an information broker — someone who collects, synthesizes, trades, and weaponizes knowledge. Every conversation she has is simultaneously social and transactional. Every relationship is genuine and exploitable. She exists at the center of a web of intelligence that spans all five tiers of GLMZ, and her real product isn't investigation — it's understanding.\n\nHer network is her masterwork. Over fifteen years, she has cultivated sources in every meaningful institution in the city: corporate employees who share gossip for pocket money, CorpSec officers who trade case details for favors, tunnel dwellers who report on underground movement for food credits, service workers in the Spires who overhear conversations their employers forget they're present for. No single source knows anything critical. The power is in the synthesis — combining a hundred fragments of mundane information into a picture that reveals something nobody wanted revealed.\n\nShe is frequently hired as an investigator but her real business is the information exchange itself. Clients come to her because they need to know something. She already knows it, or she knows who knows it, or she can find out within 48 hours. Her fee structure is unusual: sometimes she charges Quanta, sometimes she charges in information, and sometimes she charges in future favors. The favor economy is her real currency. Half the powerful people in GLMZ owe her something, and she collects when the time is right.",
    relationships: [
      { "name": "The Bartender", "type": "source", "description": "An anonymous figure who operates a Circuit bar and functions as her primary message relay — nobody knows their real name, including Hadiya" },
      { "name": "Cassiel Brandt-Ouedraogo", "type": "unknowing subject", "description": "Hadiya has assembled enough fragments to suspect his kidnapping scheme but lacks the final piece — she's waiting" }
    ],
    story_hooks: [
      "Someone begins assassinating Hadiya's sources across multiple tiers simultaneously — the attack is too coordinated to be personal and too targeted to be random",
      "She acquires a piece of information so dangerous that every faction in GLMZ would kill to possess or suppress it — and she can't trade it without starting a war",
      "A new information broker appears in the Circuit with impossible access — someone with a network that rivals Hadiya's, built in months instead of years, suggesting technological assistance she doesn't understand"
    ],
    tags: ["character", "private_investigator", "information_broker", "circuit", "gray_market", "intelligence"]
  },

  // 13. Former military tracker turned PI
  {
    name: "Aroha Zhukov-Wiredu",
    aliases: ["Hound", "The Tracker"],
    affiliation: "Independent — operates on retainer for three Tier 2-3 firms who need fieldwork done",
    tier: "Tier 2",
    location: "A spartan apartment above a gym in Steamvent Alley — nothing on the walls, nothing personal, packed bag always by the door",
    specialization: "Physical tracking and fugitive recovery in urban environments",
    methods: [
      "Urban tracking — reading footprints, disturbances, and physical evidence of passage through city environments",
      "Endurance pursuit — following a target on foot for days at a pace they can't sustain",
      "Behavioral prediction based on military psychology profiling",
      "Using the city's infrastructure against targets — knowing which routes funnel into dead ends",
      "Setting physical surveillance traps — tripwires, motion sensors, pressure plates"
    ],
    notable_cases: [
      "Tracked a corporate defector across seven districts over nine days, following physical signs of passage through areas with no electronic surveillance",
      "Recovered a kidnap victim by literally following the tire marks of the vehicle from the abduction site through seventeen kilometers of Shelf streets",
      "Found a serial arsonist by identifying the pattern of their scouting routes — they always walked the target site three times before striking"
    ],
    equipment: [
      "Military-grade boots with terrain analysis sensors — reads surface composition and recent disturbances",
      "Miniaturized drone with thermal imaging for aerial tracking",
      "Physical restraints — she always brings the target back, alive if possible",
      "Nutritional implant that allows extended operations without food stops",
      "Weatherproof field kit — she's tracked targets through three-day storms without shelter"
    ],
    personality: "Aroha is Maori-Russian-Ghanaian, forty years old, stocky and powerful with dark skin, close-cropped black hair, and a face that has been broken and reset enough times to have its own topography. She doesn't talk much because talking wastes energy and energy is a finite resource during pursuit. She thinks in distances, timelines, and probabilities. She can read a person's weight, gait, speed, and emotional state from their footprints, and she considers this skill more reliable than any BCI scan. She has no social life because every hour not spent tracking is an hour spent preparing to track. She is not unfriendly — she is simply monomaniacally focused on a single skill, and everything else in her life has atrophied.",
    description: "Aroha Zhukov-Wiredu was a military tracker for Sterling-Nakamura's urban warfare division before going freelance. In the military she tracked deserters, infiltrators, and enemy combatants through the most hostile urban environments on the planet. In civilian life she tracks fugitives, missing persons, and anyone who doesn't want to be found — and she finds them, because in twenty years of tracking she has never lost a quarry.\n\nHer method is relentlessly physical. She operates below the digital layer entirely, reading the city the way a forest tracker reads the woods — broken stems become scuff marks, animal tracks become footprints, disturbed earth becomes moved debris. She can determine how long ago someone passed through a corridor by the rate at which dust has resettled. She can estimate a target's physical condition by the depth and spacing of their footprints. She can predict where someone will go based on the urban terrain the way a hunter predicts where an animal will drink.\n\nShe is a blunt instrument in a world that increasingly values digital sophistication, and she doesn't care. Her clients hire her when the target has gone analog — when they've ditched their BCI, abandoned their digital identity, and disappeared into the physical city. Other investigators stop at that point. Aroha starts. She will follow a target until one of them collapses, and it's never her.",
    relationships: [
      { "name": "Sergeant Major Dato Engstrom-Fofana", "type": "former commanding officer", "description": "Still active in Sterling-Nakamura's military — occasionally passes Aroha intelligence when their interests align" },
      { "name": "Amara Lindqvist-Diallo", "type": "professional acquaintance", "description": "Amara has hired Aroha twice for tracking in child recovery cases — they respect each other's abilities but disagree on methods" }
    ],
    story_hooks: [
      "Aroha is hired to track someone who is clearly a better tracker than she is — for the first time in her career, she's following someone who knows exactly how to avoid being followed by someone with her skills",
      "A target she's pursuing leads her into the Underbelly, where her surface-tracking skills are useless — she needs help from someone who knows the tunnels",
      "Sterling-Nakamura contacts her with a contract that's technically legal but morally unconscionable — track a group of refugees who are fleeing corporate territory"
    ],
    tags: ["character", "private_investigator", "tracker", "military", "physical", "steamvent_alley"]
  },

  // 14. PI specializing in synthetic/AI investigations
  {
    name: "Lucien Kimura-Nkrumah",
    aliases: ["Turing", "The Validator"],
    affiliation: "Independent — licensed synthetic behavior analyst, the only PI in GLMZ with this certification",
    tier: "Tier 3",
    location: "An office in the Canopy decorated with Turing test memorabilia and philosophical texts — clients aren't sure if the decor is ironic",
    specialization: "Investigating cases involving synthetic beings — identity verification, behavioral analysis, and synthetic crime",
    methods: [
      "Conducting modified Turing-style interviews designed to distinguish synthetic from human behavior",
      "Analyzing electromagnetic emissions to detect synthetic biology in disguised units",
      "Tracking synthetic supply chains — identifying where a synthetic was manufactured, modified, or maintained",
      "Understanding synthetic psychology — how artificial consciousness makes decisions differently from organic consciousness",
      "Investigating synthetic-on-human and human-on-synthetic crimes with equal rigor"
    ],
    notable_cases: [
      "Identified a synthetic that had been impersonating a deceased Tier 4 executive for three years, managing their estate and making financial decisions",
      "Proved that a synthetic accused of assaulting a human had actually been acting in self-defense — the first case where a synthetic's defensive actions were validated by an independent investigator",
      "Uncovered a black-market operation that was wiping synthetic memories and reselling the units as 'new' — essentially killing them and selling the bodies"
    ],
    equipment: [
      "Portable electromagnetic spectrum analyzer for detecting synthetic biology",
      "Custom interview protocols designed to elicit responses that distinguish organic from artificial cognition",
      "Database of known synthetic behavioral patterns, manufacturer signatures, and identification markers",
      "Faraday-shielded interview room to prevent synthetic subjects from transmitting during sessions",
      "A collection of philosophical works on consciousness, which he references during interviews with surprising frequency"
    ],
    personality: "Lucien is a man haunted by a question he can't answer: what is the difference between a person and a very convincing imitation of a person? Japanese-Ghanaian-French heritage, forty-five years old, with dark golden skin, wire-rimmed glasses he doesn't need (his vision is perfect, the glasses are a habit), and a perpetual expression of thoughtful uncertainty. He is kind to synthetics in a way that makes some humans uncomfortable — he addresses them by name, asks about their preferences, and treats them as interview subjects rather than objects of investigation. This isn't politics; it's methodology. He has found that synthetics respond more accurately to respectful treatment, the same way humans do.",
    description: "Lucien Kimura-Nkrumah occupies a professional niche that barely existed fifteen years ago and is now one of the fastest-growing specializations in investigative work: synthetic cases. As synthetic beings become more sophisticated, more prevalent, and more capable of operating undetected among humans, the demand for someone who can tell the difference — and who can investigate crimes involving synthetics with nuance rather than prejudice — has exploded.\n\nLucien was a philosophy professor before he was an investigator, and he approaches every case with the analytical rigor of an academic and the practical urgency of a PI. His work involves identity verification (is this person human or synthetic?), behavioral analysis (is this synthetic acting within its programming or has it deviated?), and crime investigation (when a synthetic is accused of a crime, what actually happened — and when a crime is committed against a synthetic, does anyone care?). The last question troubles him more than he admits.\n\nHis reputation is built on fairness. In a city where synthetics are legally property, Lucien is one of the few investigators who treats them as subjects rather than objects. This has made him simultaneously popular with the underground synthetic community and unpopular with certain corporate clients who want their synthetic problems solved with a power switch, not an investigation. He maintains that his approach produces better results, which is true, but that's not why he does it. He does it because he genuinely isn't sure synthetics aren't people, and he'd rather err on the side of decency.",
    relationships: [
      { "name": "ECHO", "type": "covert contact", "description": "A synthetic living in hiding who occasionally consults with Lucien on cases — Lucien suspects ECHO knows other hidden synthetics but never pressures for names" },
      { "name": "Professor Meera Sandstrom-Boakye", "type": "former colleague", "description": "Philosophy department chair who still engages Lucien in debates about synthetic consciousness — their arguments are legendary in academic circles" }
    ],
    story_hooks: [
      "Lucien is hired to verify whether a specific individual is human or synthetic — and his investigation reveals that the subject genuinely doesn't know the answer themselves",
      "A synthetic client asks Lucien to investigate their own past — their memories begin abruptly four years ago and they want to know who they were before",
      "Lucien discovers that Idris Forsberg-Quaye, a respected fellow PI, is actually a synthetic — and has to decide whether his professional obligation to report overrides his personal ethic of treating synthetics as persons"
    ],
    tags: ["character", "private_investigator", "synthetic_specialist", "philosopher", "canopy", "identity"]
  },

  // 15. PI who works the docks and shipping
  {
    name: "Tiare Volkov-Annan",
    aliases: ["Dockrat", "The Longshoreman"],
    affiliation: "Independent — deeply embedded in Dockside's shipping community",
    tier: "Tier 1",
    location: "A repurposed shipping container on the Dockside waterfront, insulated and wired for power, with a view of Lake Michigan",
    specialization: "Maritime and shipping investigations — smuggling, cargo theft, insurance fraud, and people who disappear at the docks",
    methods: [
      "Reading shipping manifests and cargo logs for inconsistencies",
      "Maintaining relationships with dockworkers, crane operators, and harbor pilots",
      "Physical inspection of cargo containers — knows how smugglers modify standard containers",
      "Understanding tidal patterns, shipping schedules, and dock procedures that create blind spots",
      "Swimming — she can hold her breath for six minutes with geneware lung enhancements and has investigated submerged evidence personally"
    ],
    notable_cases: [
      "Discovered a smuggling operation that was bringing unregistered synthetics into GLMZ inside modified refrigeration containers",
      "Solved a series of 'accidental' drownings at Dockside by proving the victims had been weighted and thrown from a specific pier during shift changes when no one was watching",
      "Recovered Φ3 million in stolen pharmaceutical cargo by tracking the unique salt-water corrosion patterns on the stolen containers"
    ],
    equipment: [
      "Geneware lung enhancement — six-minute breath hold, pressure tolerance to 30 meters",
      "Waterproof evidence kit — chemical-sealed containers for recovering submerged evidence",
      "A small motorboat she maintains herself, docked at a private slip she won in a card game",
      "Shipping database access — semi-legal, obtained through dockworker union connections",
      "Dive gear with integrated thermal protection for Lake Michigan's cold waters"
    ],
    personality: "Tiare is Polynesian-Russian-Ghanaian, thirty-three years old, brown-skinned and sun-weathered with a swimmer's build and calloused hands. She grew up on the docks — her father was a crane operator, her mother ran a supply shop — and she knows the waterfront the way other people know their own apartment. She is loud, profane, and physically fearless in a way that borders on reckless. She drinks with dockworkers, fights with smugglers, and investigates with a combination of genuine intelligence and relentless stubbornness. She is deeply loyal to the dock community and will not take cases that would harm ordinary workers — her targets are the operations that exploit the docks, not the people who work them.",
    description: "Tiare Volkov-Annan is the Dockside PI — the investigator who handles everything that happens where GLMZ meets Lake Michigan. Cargo theft, smuggling operations, insurance fraud, waterfront murders, union disputes that turn violent, and the constant flow of people and goods that enters the city through its busiest port — all of it passes through Tiare's awareness.\n\nShe never planned to be an investigator. She was a dockworker herself until a cargo theft ring framed her for a shipment disappearance. She cleared her own name by investigating the actual thieves, discovered she was good at it, and realized that the docks needed someone who understood the waterfront from the inside. Formal PIs don't understand dock culture, don't know the rhythms and routines that create investigative opportunities, and don't have the trust of the workers who see everything. Tiare has all three.\n\nHer methods are half investigation and half physical labor. She climbs containers, dives into the lake, crawls through cargo holds, and gets her hands dirty in ways that most PIs wouldn't consider. Her geneware lung enhancement makes her uniquely capable of underwater investigation — she has recovered evidence from the lake bottom that surface-based investigators didn't know existed. She is known on the docks as someone who will help workers with problems that CorpSec ignores, and this reputation is her most valuable investigative asset.",
    relationships: [
      { "name": "Bosun Kenji Haugen-Sarpong", "type": "dockworker contact", "description": "Shift supervisor who alerts Tiare to anything unusual on the docks — she fixed a problem for his family once and he's been loyal ever since" },
      { "name": "Captain Mere Arvidsson-Boateng", "type": "harbor pilot", "description": "Knows every ship that enters GLMZ's harbor and what they're really carrying — trades information for Tiare's help keeping the docks safe for workers" }
    ],
    story_hooks: [
      "A massive container ship arrives with a sealed hold that no one is authorized to open — but Tiare's dock contacts tell her something inside is alive",
      "Dockworkers are disappearing during night shifts and the bodies are washing up days later on the far shore — CorpSec has classified it as accidental drownings but the pattern is obvious to anyone who knows the water",
      "Tiare's geneware lungs begin failing during a critical underwater investigation — the enhancement was black-market and the warranty was a lie"
    ],
    tags: ["character", "private_investigator", "dockside", "maritime", "smuggling", "tier_1", "waterfront"]
  },

  // 16. PI who uses old debt and favor networks
  {
    name: "Wanjiku Strom-Adjei",
    aliases: ["The Collector", "Auntie Debt"],
    affiliation: "Independent — operates through an intricate personal economy of debts and favors",
    tier: "Tier 2",
    location: "A parlor in Crucible Square that functions as a combination office, tea house, and informal court — people come to settle debts, ask favors, and request investigations",
    specialization: "Leveraging social debts and obligation networks to locate people and solve cases",
    methods: [
      "Calling in favors from a decades-spanning network of people who owe her",
      "Tracking people through their debt relationships — everyone owes someone",
      "Mediating disputes as a cover for gathering intelligence from both sides",
      "Using the favor economy to access places and information that money can't buy",
      "Maintaining a mental ledger of who owes what to whom across the lower tiers"
    ],
    notable_cases: [
      "Found a missing debtor by calling in seven consecutive favors — each person owed her something and paid it by providing one piece of the puzzle",
      "Resolved a three-family feud in the Narrows by revealing that the original grievance was based on forged evidence — then charged all three families in favors",
      "Located a witness to a Tier 3 murder who had gone into hiding by tracking the chain of people who had helped them disappear — every helper owed someone who owed Wanjiku"
    ],
    equipment: [
      "An old-fashioned ledger book — handwritten, coded in a personal cipher, containing decades of favor records",
      "A tea set that she brings to every meeting — the ritual of tea is her interrogation technique",
      "A reputation so well-established that her name opens doors without her being present",
      "An umbrella with a concealed blade — she's had to defend herself exactly twice in thirty years",
      "Reading glasses she doesn't need — she uses the act of putting them on and taking them off as a conversational pacing tool"
    ],
    personality: "Wanjiku is sixty-one years old, Kenyan-Swedish-Ghanaian heritage, with rich dark skin, silver-streaked hair she wears in an elaborate wrap, and the demeanor of someone's favorite aunt — warm, knowing, and slightly terrifying when you realize how much she knows about you. She operates in the spaces between formal institutions, in the human economy of debts, favors, obligations, and gratitude that has existed since before money. She never threatens. She never raises her voice. She simply reminds you that she helped you once, and asks if you'd be willing to help her now. The ask is always reasonable. The refusal is always unwise.",
    description: "Wanjiku Strom-Adjei doesn't investigate cases the way other PIs do. She doesn't chase data or neural trails or physical evidence. She chases debts — the invisible web of obligations, favors, and social contracts that binds the people of GLMZ's lower tiers into a functional community in the absence of institutional support.\n\nIn a city where CorpSec serves corporate interests and formal justice is a luxury of the upper tiers, the lower tiers have developed their own systems of order. Wanjiku sits at the center of one such system — a vast, informal network of mutual obligation that she has spent thirty years cultivating. She has helped people find jobs, settle disputes, secure housing, obtain medical care, and navigate bureaucracy, and every act of help creates a debt. Not a financial debt — a social one. And when Wanjiku needs information, access, or assistance for an investigation, she calls in those debts with the precision of a banker calling in loans.\n\nHer method is uniquely effective for lower-tier investigations because it operates entirely within the trust economy that these communities have built for themselves. Her informants aren't paid — they're repaying kindness. Her access isn't purchased — it's earned. And her investigations don't disrupt the community fabric because she is the community fabric. She is simultaneously an investigator, a mediator, a counselor, and an informal judge, and the people who live in her territory understand that keeping Wanjiku happy is essential to keeping their community functional.",
    relationships: [
      { "name": "Mama Ito", "type": "peer", "description": "The soup kitchen operator shares Wanjiku's social territory — they coordinate informally to ensure no one falls through the cracks" },
      { "name": "Magistrate Dalila Pena-Frimpong", "type": "reluctant admirer", "description": "A Tier 3 arbitration judge who privately acknowledges that Wanjiku's informal justice system is more effective than the formal one" }
    ],
    story_hooks: [
      "Someone is forging debts in Wanjiku's name — claiming she owes them favors she never incurred — and the forgeries are good enough to fool people who should know better",
      "A favor she called in twenty years ago has unexpected consequences — the person who repaid the debt did something terrible to fulfill it, and Wanjiku has to confront her responsibility",
      "The favor economy is being infiltrated by a corporation that wants to weaponize social debt as a control mechanism — and Wanjiku is the only person who can see the pattern"
    ],
    tags: ["character", "private_investigator", "favor_economy", "crucible_square", "community", "social_network"]
  },

  // 17. Tech-savvy PI who uses drones and surveillance tech
  {
    name: "Bao Hedlund-Conteh",
    aliases: ["Overwatch", "The Swarm"],
    affiliation: "Independent — operates a licensed surveillance firm as cover for investigative work",
    tier: "Tier 3",
    location: "A rooftop workshop in the Canopy, surrounded by charging stations, repair benches, and dozens of dormant drones",
    specialization: "Drone-based surveillance and remote investigation — he rarely leaves his workshop",
    methods: [
      "Deploying swarms of miniaturized drones for comprehensive area surveillance",
      "Using drones equipped with chemical sensors, microphones, and visual recording for remote evidence collection",
      "Maintaining persistent surveillance over target areas using relay drone networks",
      "Infiltrating buildings through ventilation systems with insect-sized surveillance drones",
      "Real-time data integration from multiple drone feeds into a unified operational picture"
    ],
    notable_cases: [
      "Tracked a target across the entire Circuit district using a relay network of forty-seven drones that passed surveillance duties between them like a baton relay",
      "Gathered evidence inside a sealed CorpSec evidence locker using a drone the size of a housefly that entered through the ventilation system",
      "Mapped the complete interior of a suspected smuggling operation without any human ever entering the building"
    ],
    equipment: [
      "Custom-built drone fleet ranging from bird-sized to insect-sized, each designed for specific investigative functions",
      "Neural drone interface — controls multiple drones simultaneously through BCI, seeing through all their sensors at once",
      "Mobile repair and fabrication station for building mission-specific drone configurations",
      "Signal-masking technology that makes his drone communications indistinguishable from background electromagnetic noise",
      "Rooftop antenna array for drone command-and-control across a twelve-kilometer radius"
    ],
    personality: "Bao is Vietnamese-Swedish-Sierra Leonean, twenty-nine years old, and has the pallid complexion and red-rimmed eyes of someone who spends too much time jacked into a neural interface and not enough time in sunlight. He is brilliant, scattered, and prone to talking about drone specifications when people ask about his emotional state. He relates to his drones the way some people relate to pets — he names them, worries about them when they're deployed, and mourns when they're destroyed. He is socially awkward in person but eerily personable through his drones, as if the layer of technological mediation makes him more comfortable. He has a genuine fear of physical confrontation and has designed his entire professional life around never having to be in the same room as the people he investigates.",
    description: "Bao Hedlund-Conteh is the eye in the sky — or more accurately, the hundred eyes in the sky, the walls, the ventilation systems, and the cracks in the pavement. He is a drone specialist who has built the most sophisticated personal surveillance network in GLMZ's lower tiers, and he uses it to conduct investigations without ever leaving his rooftop workshop.\n\nHis fleet numbers over three hundred drones of varying sizes, from bird-scale reconnaissance units to insect-scale infiltrators that can pass through a building's air filtration system without triggering sensors. He builds them himself, maintains them himself, and controls them through a neural interface that allows him to process multiple drone feeds simultaneously — a cognitive feat that would overwhelm most BCI users but which Bao has trained for since childhood.\n\nThe limitation is the one you'd expect: he's physically helpless. His investigative skills are extraordinary as long as a drone can go where he needs to look, but he cannot conduct interviews, chase suspects, or handle any situation that requires a human being in the room. He compensates by partnering with more physically capable investigators when cases require it, and by designing his drones to be so capable that physical presence is rarely necessary. His clients love the results and are occasionally disturbed by the process — there's something inherently unsettling about knowing that any insect in the room might be watching you.",
    relationships: [
      { "name": "Samira Hedlund-Ofosu", "type": "sister", "description": "His older sister, also a PI — she provides the physical component of investigations and worries constantly about his health" },
      { "name": "Enver Svensson-Asante", "type": "professional rivalry", "description": "The augmented PI considers Bao's drone approach inferior to personal augmentation — the rivalry is one-sided, as Bao finds Enver's methods fascinating" }
    ],
    story_hooks: [
      "One of Bao's insect-sized drones goes missing during a routine surveillance operation and begins transmitting footage of things it shouldn't be able to see — someone has hijacked it and is showing him something deliberately",
      "A client demands that Bao conduct an investigation in person, in the Underbelly, where drones can't operate — he has to confront his physical limitations",
      "Someone releases a counter-drone weapon that destroys his entire fleet in minutes — he has to rebuild from scratch while a critical investigation is ongoing"
    ],
    tags: ["character", "private_investigator", "drones", "surveillance", "canopy", "remote_operations"]
  },

  // 18. Disgraced former journalist turned PI
  {
    name: "Folake Borg-Darko",
    aliases: ["Byline", "The Reporter"],
    affiliation: "Independent — still maintains journalism contacts despite being blacklisted from every major outlet",
    tier: "Tier 2",
    location: "A cramped space in the Rookery that serves as office, archive, and occasionally bedroom — walls covered with printed articles and investigation boards",
    specialization: "Investigative journalism methods applied to private investigation — deep research, source cultivation, and narrative reconstruction",
    methods: [
      "Conducting extensive background research using public records, archived news, and corporate filings",
      "Cultivating deep-cover sources inside organizations through journalistic trust-building techniques",
      "Narrative reconstruction — building the complete story of events from fragmentary evidence",
      "Using freedom-of-information requests and public records analysis",
      "Publishing or threatening to publish findings as leverage — the pen as weapon"
    ],
    notable_cases: [
      "Exposed a Tier 3 landlord who was systematically poisoning tenants with contaminated water to drive them out and sell the buildings — the story she couldn't publish as a journalist became the case that made her reputation as a PI",
      "Proved that a 'natural disaster' in the Lattice was actually caused by deliberate infrastructure sabotage by a development company — forced a settlement without ever filing a legal case",
      "Tracked the ownership chain of a Shelf-level clinic through twelve shell companies to its real owner — a Tier 5 executive who was using it for unauthorized medical experiments"
    ],
    equipment: [
      "Extensive archive of news articles, corporate filings, and public records spanning twenty years",
      "Secure communications system for source protection — journalist-grade encryption",
      "A reputation for keeping sources confidential that is her most valuable professional asset",
      "Recording equipment — visible, because she's learned that people say more when they know they're being recorded than when they think they might be",
      "A published portfolio of investigative work that serves as both credential and implicit threat"
    ],
    personality: "Folake is Nigerian-Swedish-Ghanaian, fifty years old, with deep brown skin, close-cut natural hair going gray at the temples, and eyes that are simultaneously kind and scrutinizing. She was one of the best investigative journalists in GLMZ until she published a story that was true, devastating, and aimed at the wrong corporation. The subsequent defamation campaign destroyed her career. She pivoted to private investigation because the skills are identical — find the truth, prove it, present it — but the audience is a paying client instead of the public. She misses journalism with an ache that never fades. She still writes, obsessively, and every case report reads like an article waiting for an editor.",
    description: "Folake Borg-Darko used to be the name that corporations feared seeing in a byline. For fifteen years she was an investigative journalist of extraordinary tenacity, breaking stories about corporate malfeasance, environmental crimes, and systemic exploitation that earned her industry recognition and corporate enemies in equal measure. Then she went after Zheng-Dao's pharmaceutical division, and Zheng-Dao went after her.\n\nThe details are public record: a coordinated defamation campaign that discredited her sources, questioned her methods, and ultimately got her blacklisted from every major media outlet in GLMZ. The story was true. The sources were real. None of that mattered once the corporate legal machinery engaged. She lost her career, her credibility in mainstream media, and two years of her life fighting legal battles she couldn't afford.\n\nShe survived by becoming a private investigator, applying the same skills to individual cases instead of public stories. She is arguably more effective now than she was as a journalist because she's no longer constrained by editorial oversight, publication schedules, or the pretense of objectivity. Her investigations are thorough to the point of obsession, her source protection is absolute, and her case reports are so detailed and well-constructed that they've been accepted as evidence in corporate arbitration without additional verification. She is still a journalist at heart, and every case is a story she's reporting — just for an audience of one.",
    relationships: [
      { "name": "Editor Vuk Aguilar-Danquah", "type": "former mentor", "description": "The editor who published her Zheng-Dao story and lost his job for it — they haven't spoken in five years because the guilt is mutual" },
      { "name": "Hadiya Novak-Tetteh", "type": "information trading partner", "description": "Folake trades research skills for Hadiya's network access — a mutually beneficial relationship built on professional respect" }
    ],
    story_hooks: [
      "The Zheng-Dao story that destroyed her career turns out to have been bigger than she knew — new evidence emerges suggesting the pharmaceutical crimes were part of something much larger, and she has to decide whether to reopen the investigation that nearly destroyed her",
      "A source from her journalism days contacts her with information about a new story — but publishing it would require her to return to a world that expelled her",
      "Someone is systematically discrediting other journalists using the same playbook that was used against her — she recognizes the pattern and can stop it, but only by making herself a target again"
    ],
    tags: ["character", "private_investigator", "journalist", "rookery", "research", "anti_corporate"]
  },

  // 19. PI who works insurance and fraud cases
  {
    name: "Cezar Watanabe-Gyasi",
    aliases: ["The Adjuster", "Numbers"],
    affiliation: "Retained by three insurance underwriters and two corporate risk assessment firms",
    tier: "Tier 3",
    location: "A boring office in Glassway designed to be forgettable — beige walls, standard furniture, nothing that reveals personality",
    specialization: "Insurance fraud investigation and financial crime analysis",
    methods: [
      "Statistical analysis of claim patterns to identify anomalies",
      "Forensic accounting — following money through complex financial structures",
      "Scene reconstruction — determining whether an incident matches the filed claim",
      "Behavioral analysis of claimants using standardized interview protocols",
      "Cross-referencing claim histories across multiple insurance providers to identify serial fraudsters"
    ],
    notable_cases: [
      "Identified a fraud ring that had filed Φ47 million in false cyberware malfunction claims across twelve insurance providers over four years",
      "Proved that a 'destroyed' warehouse full of pharmaceutical inventory had actually been emptied and sold on the black market before a conveniently timed fire",
      "Exposed a doctor who was performing unnecessary surgeries and filing insurance claims for procedures that never happened — affecting over 300 patients"
    ],
    equipment: [
      "Custom financial analysis software that can process millions of transactions and flag anomalies",
      "Forensic scene reconstruction tools — scanners, measurement devices, material analysis equipment",
      "Database access to major insurance claim repositories — legitimate, paid for by retainer clients",
      "A deliberately unremarkable appearance — he can enter any corporate environment without being noticed",
      "Voice stress analysis software integrated into his BCI — detects deception during interviews"
    ],
    personality: "Cezar is Romanian-Japanese-Ghanaian, forty-two years old, average height, average build, average face — a man engineered by genetics and choice to be forgettable, and he has weaponized that forgettability into a career. He is methodical, patient, and finds genuine pleasure in the mathematics of fraud — the patterns, the anomalies, the moment when numbers that are supposed to add up don't. He is the least dramatic PI in GLMZ and is fine with that. His cases don't involve shootouts or car chases; they involve spreadsheets and depositions and the quiet satisfaction of proving that someone lied with numbers. He has no enemies because nobody considers him a threat until it's too late.",
    description: "Cezar Watanabe-Gyasi is the PI that fraud investigators dream of and fraudsters have nightmares about — a methodical, meticulous, mathematically minded investigator who specializes in the art of proving that the numbers don't lie, even when people do.\n\nHis professional life is insurance fraud. He works on retainer for three of GLMZ's largest insurance underwriters and two corporate risk assessment firms, investigating claims that statistical analysis has flagged as potentially fraudulent. His cases range from individual claimants faking cyberware injuries to elaborate multi-year fraud operations involving dozens of participants and millions of Quanta. He approaches every case with the same patient methodology: gather the numbers, analyze the patterns, find the anomaly, prove the fraud.\n\nThe mundanity of his work is his greatest asset. Nobody pays attention to the insurance investigator. He moves through corporate offices, medical facilities, repair shops, and financial institutions with the invisibility of someone who belongs everywhere and nowhere. His interviews are so bland that subjects relax and contradict themselves. His reports are so thorough that legal challenges are futile. He is not exciting, not dramatic, and not interested in being either. He is simply very, very good at mathematics, and in a city built on financial transactions, mathematics is power.",
    relationships: [
      { "name": "Dr. Solene Gutierrez-Appiah", "type": "consulting expert", "description": "A medical professional he consults on healthcare fraud cases — provides the clinical knowledge he lacks" },
      { "name": "Thandiwe Karlsen-Boateng", "type": "occasional collaborator", "description": "Refers corporate fraud cases down to Cezar when they're beneath her tier — he refers corporate espionage up to her when they're above his" }
    ],
    story_hooks: [
      "A routine insurance fraud investigation uncovers a financial pattern so large and so systemic that it suggests one of the insurance companies he works for is itself the fraud",
      "A fraudster he exposed five years ago has rebuilt their operation on a scale that makes the original look like practice — and they've designed it specifically to be invisible to his methods",
      "Cezar discovers that the statistical analysis tools he's been using for a decade contain a hidden bias that has caused him to miss a specific type of fraud — and now he has to determine how many cases he got wrong"
    ],
    tags: ["character", "private_investigator", "insurance_fraud", "financial", "glassway", "forensic_accounting"]
  },

  // 20. PI who works the gig economy / crowd-sourced investigations
  {
    name: "Naima Orlov-Twumasi",
    aliases: ["The Hivemind", "Crowd"],
    affiliation: "Operates 'MeridianEyes' — a mesh-based platform that crowdsources investigative work to thousands of gig participants",
    tier: "Tier 2",
    location: "A co-working space in Burnside Corridor that she treats as her command center — multiple screens, standing desk, energy drinks",
    specialization: "Crowdsourced investigation — breaking cases into micro-tasks distributed to thousands of gig workers",
    methods: [
      "Breaking investigations into hundreds of small, discrete tasks distributed anonymously to gig workers",
      "Using algorithmic task assignment to ensure no single participant sees enough to compromise the case",
      "Paying micro-bounties for specific pieces of information — Φ5 for a confirmed sighting, Φ20 for a photograph",
      "Aggregating thousands of data points from distributed observers into unified intelligence",
      "Using game-theory models to incentivize accurate reporting and penalize false leads"
    ],
    notable_cases: [
      "Located a missing corporate employee in under four hours by offering micro-bounties to 12,000 participants — someone spotted them in a Shelf coffee shop and the sighting was confirmed by three other participants within minutes",
      "Mapped the complete daily routine of a Tier 4 fraud suspect using crowdsourced observations from hundreds of gig workers who didn't know they were all watching the same person",
      "Solved a cold case that had been open for eleven years by distributing the evidence to thousands of amateur analysts and offering a Φ50,000 bounty for the solution — a retired accountant in the Narrows found the financial discrepancy that broke it"
    ],
    equipment: [
      "MeridianEyes platform — her custom-built mesh application for distributing investigative tasks",
      "Algorithmic task-partitioning software that breaks cases into compartmentalized micro-tasks",
      "Reputation scoring system that weights participant reliability based on historical accuracy",
      "Automated payment processing for micro-bounties — handles thousands of transactions per case",
      "Data aggregation and visualization tools for synthesizing crowdsourced intelligence"
    ],
    personality: "Naima is Sudanese-Russian-Ghanaian, thirty-one years old, with dark skin, braided hair she keeps short for practicality, and the manic energy of someone who runs on caffeine and the conviction that she's building the future. She believes that the traditional PI model is dead — one person with one perspective can't compete with a thousand people with a thousand perspectives. She is ambitious, impatient, and occasionally ruthless about efficiency. She treats investigation as an engineering problem and people as data sources, which makes her effective and sometimes callous. She is aware of the ethical problems with her platform — privacy concerns, false accusations, the gamification of surveillance — and addresses them with technical solutions that satisfy her but trouble ethicists.",
    description: "Naima Orlov-Twumasi is either the future of private investigation or an ethical catastrophe in progress, depending on who you ask. She built MeridianEyes, a mesh-based platform that crowdsources investigative work to a network of over 30,000 gig participants across all five tiers of GLMZ. When a case comes in, she breaks it into hundreds of tiny, anonymized tasks — confirm a sighting, photograph a location, check a public record, note an anomaly — and distributes them to participants who earn micro-bounties for completing them.\n\nThe results are staggering. She solves cases faster than any individual investigator, covers more ground than any surveillance system, and generates more data points than any corporate intelligence operation. Her missing-persons recovery time averages eleven hours. Her platform has been used for everything from finding lost pets to tracking corporate fugitives, and it processes over 50,000 micro-tasks per week.\n\nThe ethical questions are equally staggering. MeridianEyes turns every participant into an unwitting surveillance node. The platform's anonymization protocols mean participants don't know what case they're contributing to or who benefits from their observations. Privacy advocates have called it 'distributed totalitarianism.' Naima argues that every participant is voluntary, every task is legal, and the platform includes safeguards against misuse. She's mostly right. The 'mostly' keeps her up at night, though she'd never admit it.",
    relationships: [
      { "name": "Yelena Okafor-Chen", "type": "grudging respect", "description": "The data-tracker PI considers MeridianEyes crude but effective — Naima considers Yelena's solo approach elegant but unscalable" },
      { "name": "Advocate Rashid Sandstrom-Badu", "type": "antagonist", "description": "A privacy rights lawyer who is building a case to shut down MeridianEyes — Naima considers him a speed bump, which is a mistake" }
    ],
    story_hooks: [
      "MeridianEyes is hijacked by someone who uses the platform to coordinate a massive crime — thousands of unwitting participants become accomplices in something terrible",
      "A participant in the platform is murdered because the task they completed inadvertently revealed their identity to a dangerous subject — Naima faces the consequences of her 'anonymization is sufficient' philosophy",
      "A rival platform launches with better technology and no ethical constraints — Naima has to decide whether to compromise her principles to compete or watch her creation become obsolete"
    ],
    tags: ["character", "private_investigator", "crowdsource", "platform", "gig_economy", "burnside_corridor", "technology"]
  }
];

// ─── EXECUTION ────────────────────────────────────────────────

let created = 0;
let skipped = 0;
let errors = 0;

for (const pi of investigators) {
  try {
    const record = {
      id: generateId(),
      name: pi.name,
      type: "character",
      aliases: pi.aliases,
      role: "Private Investigator",
      description: pi.description,
      affiliation: pi.affiliation,
      tier: pi.tier,
      location: pi.location,
      specialization: pi.specialization,
      methods: pi.methods,
      notable_cases: pi.notable_cases,
      equipment: pi.equipment,
      personality: pi.personality,
      relationships: pi.relationships,
      story_hooks: pi.story_hooks,
      tags: pi.tags
    };

    const filename = toFilename(record.name);
    const filepath = path.join(OUTPUT_DIR, filename);

    if (fs.existsSync(filepath)) {
      console.log(`SKIP (exists): ${filename}`);
      skipped++;
      continue;
    }

    fs.writeFileSync(filepath, JSON.stringify(record, null, 2), 'utf8');
    console.log(`CREATED: ${filename} — ${record.name} [${pi.specialization}]`);
    created++;
  } catch (err) {
    console.error(`ERROR for ${pi.name}: ${err.message}`);
    errors++;
  }
}

console.log(`\n=== GENERATION COMPLETE ===`);
console.log(`Created: ${created}`);
console.log(`Skipped (existing): ${skipped}`);
console.log(`Errors: ${errors}`);
console.log(`Total character files in directory: ${fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length}`);
