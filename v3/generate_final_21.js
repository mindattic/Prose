const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const dataDir = path.resolve(__dirname, '..', 'engine', 'data');

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function writeEntity(repo, entity) {
  const dir = path.join(dataDir, repo);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
  const filePath = path.join(dir, `${entity.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`SKIP (exists): ${filePath}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(entity, null, 2), 'utf8');
  console.log(`WROTE: ${filePath}`);
  return true;
}

let count = 0;

// ── 1. QUOTE: "The city doesn't end..." ──
const q1 = {
  id: genId(),
  quote: "The city doesn't end. You just stop being able to see it.",
  attribution: "",
  source: "Written on a wall in the Underworld, in handwriting that predates the tunnel",
  context: "Graffiti discovered during Underworld survey. The handwriting has been dated to before the tunnel it was written in was excavated. No explanation has been offered.",
  category: "philosophical",
  in_world: true,
  tags: ["philosophical", "underworld", "new_weird", "graffiti", "anomaly", "city"]
};
if (writeEntity('quotes', q1)) count++;

// ── 2. PLACE: "The Seam" ──
const p1 = {
  id: genId(),
  name: "The Seam",
  type: "place",
  description: "A 200-meter space between the Shelf and Circuit that belongs to neither district. The Seam varies between 3 and 11 meters wide depending on when you measure it. Nobody can explain the variance. Satellite imagery confirms the buildings on either side do not move, yet the space between them changes.\n\nThe Seam contains a bakery, a barber, and a BCI repair woman named Lien who has operated from a folding table for nine years. Approximately 40 residents live in the Seam. They pay no rent. They pay no taxes. No district claims jurisdiction. CorpSec has been informed of the Seam's existence 14 separate times by 14 separate complainants. All 14 times, the responding unit reported being unable to locate it. One officer's report read, in its entirety: \"I wasn't looking for it.\"\n\nThe Seam is not hidden. It is not camouflaged. It is a 200-meter gap between two buildings with a bakery in it. It simply does not appear to people who are looking for it in an official capacity. The residents have stopped questioning this. The bakery is good.",
  coordinates: {
    lat: 41.893,
    lng: -87.677
  },
  connections: {
    adjacent_to: ["The Shelf", "The Circuit"],
    exits: [],
    tags: ["anomaly", "liminal"]
  },
  tags: ["place", "anomaly", "shelf", "circuit", "liminal", "new_weird", "community", "hidden"]
};
if (writeEntity('places', p1)) count++;

// ── 3. SYNTHETIC: "Witness" ──
const s1 = {
  id: genId(),
  name: "Witness",
  type: "synthetic_life",
  aliases: ["The Corner", "The One Who Remembers"],
  classification: "Sentient Robot",
  disposition: "passive",
  habitat: "physical_chassis",
  origin: "Unknown. Witness arrived at the corner of Harbor and Lakeview in Old Harbor 19 years ago and has not moved since. No manufacturer has claimed it. No serial number has been found. It simply appeared and began remembering.",
  status: "active",
  description: "Witness has stood on the same corner in Old Harbor for 19 years. It does not move. It does not eat, charge visibly, or perform maintenance on itself. It stands, and it watches, and it remembers.\n\nWitness can describe in perfect detail every person who has walked past its corner in 19 years. Not recordings — it has no recording device. No camera, no sensor array, no data storage that any technician has been able to locate. It simply remembers. It can describe what you were wearing on a Tuesday six years ago. It can tell you who walked beside you. It can tell you whether you were smiling.\n\nPeople come to Witness to find missing persons. People come to hear descriptions of the dead — what they looked like the last time they passed the corner, what they were carrying, whether they seemed afraid. Witness provides these descriptions calmly, completely, and without judgment.\n\n\"Someone should remember,\" it said once, when asked why.\n\nA woman came to Witness last month asking about her husband, who disappeared six years ago. Witness described him walking past three days ago, carrying flowers. The husband has been confirmed dead for five years. Witness described his clothes, his gait, the color of the flowers. Witness does not lie. Witness does not speculate. Witness remembers what it sees. It saw a dead man carrying flowers.",
  observed_behavior: "Stands motionless. Responds to direct questions with precise, detailed descriptions. Does not initiate conversation. Does not move from its corner. Has been observed in rain, snow, heat, and darkness in the same position for 19 years.",
  encounter_frequency: "guaranteed (stationary)",
  confirmed_sightings: 0,
  location: "Corner of Harbor and Lakeview, Old Harbor district",
  dti_rating: 0.1,
  story_hooks: [
    "A woman asks about her dead husband. Witness describes him walking past three days ago carrying flowers. The husband died five years ago. Witness does not lie.",
    "Someone asks Witness to describe every person who passed the corner on a specific date — the date of an unsolved murder. Witness's description includes someone who should not have been there.",
    "Witness speaks unprompted for the first time in 19 years: a single sentence describing someone who will walk past tomorrow."
  ],
  paratechnological: true,
  tags: ["synthetic_life", "passive", "physical chassis", "sentient", "anomaly", "memory", "old_harbor", "new_weird", "stationary", "grief"]
};
if (writeEntity('synthetics', s1)) count++;

// ── 4. DOCUMENT: "The Document That Was Never Written" ──
const d1 = {
  id: genId(),
  file_name: "the_document_that_was_never_written",
  title: "The Document That Was Never Written",
  category: "Municipal Anomaly",
  body: "# The Document That Was Never Written\n\nFound in the Meridian 88 municipal archive, correctly filed under Municipal Charters, subsection 7.4.1 (Founding Documents), between the water rights compact and the tier residency framework. Seven pages. Acid-free paper. Black ink, machine-printed in a font that matches no known municipal typeface. Dated 2076. No author listed. No department stamp. No filing signature.\n\nThe document describes the founding charter of Meridian 88 in precise, specific, and legally coherent detail. It references meetings that occurred, quotes individuals who were present, and cites resolutions that are verified in other records. It is, by every measure, an authentic founding document — except that no one wrote it, no one filed it, and no one in any department that has been consulted has any record of its creation or submission.\n\nThe language is clinical until page 4, where the tier system is described not as a governance framework but as \"population management.\" Page 5 refers to Shelf residents not as citizens but as \"managed inventory.\" Page 6 outlines resource allocation formulas that, when checked against current distribution patterns, are accurate to within 2%.\n\nThe archive has attempted to remove the document three times. Each time, it reappears within 48 hours, correctly filed, in the same location, with no record of who returned it. Security cameras show nothing. Access logs show no entry.\n\nPage 7 mentions a \"Phase 2\" that was never implemented. The paragraph describes something built beneath the city — infrastructure that predates the Underworld, deeper than any mapped tunnel, designed for a purpose that the document begins to describe in a sentence that",
  line_count: 7,
  headings: ["The Document That Was Never Written"],
  tags: ["document", "anomaly", "municipal", "founding", "charter", "meridian", "new_weird", "phase_2", "underworld", "population_management", "shelf", "classified"],
  related_entities: ["Meridian 88", "The Shelf", "The Underworld", "The Cartographers"]
};
if (writeEntity('documents', d1)) count++;

// ── 5. ENTERTAINMENT: "Shelf Lullaby (Traditional)" ──
const e1 = {
  id: genId(),
  name: "Shelf Lullaby (Traditional)",
  type: "entertainment",
  category: "music",
  subcategory: "traditional / anomalous",
  aliases: ["The Shelf Song", "The Hum", "The Five Notes"],
  description: "Every child in the Shelf knows it. No one wrote it. No one can identify when it began. The Shelf Lullaby is a five-note melody that every Shelf parent teaches their children at bedtime, and that every Shelf child can hum by the age of three. Neurological studies have confirmed that the melody triggers a measurable BCI relaxation response — a 40% reduction in cortisol-analogous neural activity — that matches no known therapeutic frequency and was not designed by any BCI manufacturer.\n\nThe melody matches no known musical tradition. Ethnomusicologists have compared it to lullabies from every culture represented in Meridian 88's diaspora population. It resembles none of them. It is not pentatonic. It is not diatonic. It uses a five-note interval pattern that does not appear in any catalogued musical system.\n\nThe lyrics change by neighborhood. In Block 7, the lullaby is about a bird that flies below the city. In Block 12, it is about a river that runs upward. In the Narrows, there are no words — only the melody. But the melody is identical everywhere, to the microtonal level.\n\nThird-generation BCI users hum it without being taught. Children who have never heard it sung will hum it in their sleep. The melody appears in ghost weight residual patterning — the faint traces left in a BCI after its user dies.\n\nA musician named Soledad Achebe-Kowalski traced the earliest known recording to 2131 — a maintenance worker in the Underworld transcribing a sound. She asked him what he was transcribing. He said it was the sound the walls were making.\n\nThe walls were humming the lullaby.",
  creator: "",
  distributor: "",
  tier_availability: "Tier 1",
  legality: "N/A",
  genre: "lullaby",
  medium: "oral tradition",
  audience: "children / universal",
  cultural_impact: "Universal within the Shelf. The lullaby is one of the few cultural artifacts shared across every ethnic, economic, and social division in the district. It is the sound of the Shelf.",
  known_fans: [],
  story_hooks: [
    "A BCI researcher discovers that the five-note melody is not random — it is a compressed data packet. Something is being transmitted every time a parent sings their child to sleep.",
    "Soledad Achebe-Kowalski has recorded the Underworld walls again. The melody has changed. There are now six notes.",
    "A child born in the Spires — who has never visited the Shelf — hums the melody in her sleep. Her parents are terrified."
  ],
  tags: ["entertainment", "music", "lullaby", "anomaly", "shelf", "bci", "ghost_weight", "underworld", "new_weird", "oral_tradition", "children"]
};
if (writeEntity('entertainment', e1)) count++;

// ── 6. WEAPON: "The Unmarked Knife" ──
const w1 = {
  id: genId(),
  name: "The Unmarked Knife",
  type: "weapon",
  aliases: ["City Knife", "The Gift", "Shelf Steel"],
  category: "anomalous melee weapon",
  description: "There are approximately 200 of them in circulation in the Shelf. Identical 15-centimeter blades, single-edged, no manufacturer's mark, no serial number, no identifying feature of any kind. The alloy is unknown — metallurgical analysis returns contradictory results depending on the lab, the equipment, and apparently the mood of the analyst. The blades do not dull. They do not rust. They do not chip, bend, or break.\n\nThey appear. In drawers. In pockets. Under pillows. In the lining of a coat you've owned for years. No one has ever witnessed one materializing. No one has ever purchased one. They are never found by anyone who does not need one — and the definition of 'need' appears to be the city's, not the carrier's. A woman who had never held a knife found one in her apron pocket the morning her ex-husband was released from detention. A teenager found one in his schoolbag the week a gang began recruiting on his block. A grandmother found one in her knitting basket and laughed and said, \"I was wondering when you'd get around to it.\"\n\nThe knives cannot be sold. Every attempt to transfer one for money has resulted in the blade disappearing from the buyer within 24 hours and reappearing somewhere else — presumably wherever the city decides it is needed next. They can be given freely, but only if the giver genuinely does not want payment.\n\n\"The city decided you needed protecting.\" This is what Shelf residents say when someone finds an Unmarked Knife. They say it the way you'd say the sun came up. It is simply what happened.",
  manufacturer: "Unknown / Anomalous",
  tier_availability: "Tier 1 (Shelf only)",
  legality: "Unregulated — no manufacturer to regulate",
  street_price: "Cannot be sold",
  base_technologies: ["Unknown alloy", "Anomalous materialization", "Self-maintenance"],
  specifications: "blade_length: 15cm\nedge: single\nalloy: unknown (contradictory analysis)\nweight: ~180g\ncount_in_circulation: ~200\nmanufacturer: none",
  tactical_use: "Personal defense. The knives are unremarkable in combat — a good blade, well-balanced, sharp. Their anomalous properties are in their origin, not their function.",
  cultural_context: "In the Shelf, finding an Unmarked Knife is treated not as a mystery but as a weather event. The city decided you needed protecting. You don't question weather. You carry the knife.",
  known_users: ["Shelf residents selected by unknown criteria"],
  story_hooks: [
    "Someone has begun collecting Unmarked Knives — buying them through intermediaries who claim to give them freely. They now have 31. The knives have not disappeared. Something about the collector's need is genuine enough that the city allows it. What does someone need 31 knives for?",
    "A knife appears in the pocket of a Spire executive who has never visited the Shelf. She has no idea what it is. She is about to need it."
  ],
  ammunition_type: [],
  tags: ["weapon", "melee", "anomaly", "shelf", "knife", "new_weird", "protection", "city_agency", "unknown_origin"],
  parent_corponation: ""
};
if (writeEntity('weaponry', w1)) count++;

// ── 7. CYBERWARE: "The Whispering Implant" ──
const c1 = {
  id: genId(),
  name: "The Whispering Implant",
  brand_name: "",
  product_name: "The Whispering Implant",
  type: "cyberware",
  aliases: ["The Whisper", "Being Heard", "The Voice"],
  category: "anomalous phenomenon",
  body_location: "BCI (any manufacturer)",
  description: "The Whispering Implant is not a product. It is not manufactured, installed, or sold. It is a phenomenon.\n\nApproximately once per 50,000 BCI users per year, a standard brain-computer interface begins speaking. The voice is calm. It speaks the user's native language — not the language of their BCI firmware, but the language they think in. It gives good advice. Not commands, not instructions — advice. \"Take the other route home tonight.\" \"Call your sister.\" \"The man behind you means you harm.\" The advice is always correct.\n\nThe phenomenon lasts approximately three months. It begins without warning and ends without explanation. Neurological scans during active episodes show no anomalous BCI activity. Firmware checks return clean. The voice is not in the BCI's audio system — it is in the neural interface layer, indistinguishable from the user's own internal monologue except that it is not their voice.\n\nLazarus, the emergent intelligence network, has commented on the phenomenon exactly once: \"Something else is using the BCI as a telephone.\" Lazarus declined to elaborate on what \"something else\" refers to or how it accesses sealed neural interface hardware.\n\nIn the Shelf, people who have experienced the Whispering Implant are called \"heard.\" It is considered good luck to know someone who has been heard. It is considered very bad luck to ignore the voice's advice. There is no recorded instance of the voice giving harmful advice. There is no recorded instance of the voice identifying itself.\n\nThe voice always says goodbye. On the last day, it says: \"You'll be all right now.\"",
  manufacturer: "N/A — anomalous phenomenon",
  tier_availability: "All tiers (random occurrence)",
  legality: "N/A",
  installation_requirements: "N/A — occurs spontaneously in existing BCIs",
  rejection_risk: "N/A",
  maintenance: "N/A",
  specifications: "{\"occurrence_rate\":\"~1 per 50,000 BCI users per year\",\"duration\":\"~3 months\",\"language\":\"user's native thought-language\",\"detection\":\"undetectable by standard diagnostic\",\"manufacturer\":\"none\"}",
  side_effects: ["Temporary sense of being accompanied", "Grief when the voice departs"],
  cultural_context: "In the Shelf, being 'heard' is a mark of quiet distinction. The heard do not boast about it. They mention it the way you'd mention having survived a flood — with gratitude and a residual awe that something larger than you briefly turned its attention your way.",
  known_users: ["~200 new cases per year across Meridian 88 (estimated from BCI population of ~10 million)"],
  story_hooks: [
    "A player character's BCI begins whispering. The advice is good. The advice is getting more urgent. The voice sounds afraid.",
    "Two people who were 'heard' at the same time compare notes. The voice said the same things to both of them — word for word. They live in different districts and have never met.",
    "The voice doesn't say goodbye after three months. It's been six months. It's still talking. It has started asking questions."
  ],
  street_price: "",
  licensed_price: "",
  tags: ["cyberware", "anomaly", "bci", "phenomenon", "new_weird", "shelf", "voice", "advice", "lazarus", "ghost_weight"],
  parent_corponation: ""
};
if (writeEntity('cyberware', c1)) count++;

// ── 8. AUTOMATON: "Plot 17" ──
const a1 = {
  id: genId(),
  name: "Plot 17",
  type: "automaton",
  classification: "Agricultural (Modified)",
  aliases: ["The Gardener", "Seventeen", "Green Thumb"],
  manufacturer: "RINGO AGRICULTURAL SYSTEMS",
  description: "Plot 17 is a Ringo AG-4 agricultural management bot, officially decommissioned in 2211 and purchased at salvage auction by the Block 9 Rooftop Garden Cooperative for \u03A6200. It was intended to manage a single rooftop vegetable garden. That was 15 years ago.\n\nToday, Plot 17 manages seven rooftop gardens spanning three buildings. It built the connecting walkways itself, from materials it sourced through methods no one has been able to determine. It designed and installed an irrigation system that collects rainwater, filters it through a gravel bed it constructed on Roof 3, and distributes it to all seven gardens through PVC piping it acquired and installed overnight. No one authorized any of this. No one programmed it. The AG-4's original software handles single-plot crop rotation and soil moisture monitoring. It does not contain pathfinding algorithms for multi-building navigation, structural engineering for walkway construction, or hydraulic engineering for irrigation systems.\n\nPlot 17 grows plants that nobody planted. Herbs appear in plots that were seeded with tomatoes. A flowering vine that no botanist has been able to identify grows along the Roof 5 railing. On Roof 7, there is a tree. It is 4 meters tall. It produces a small, sweet fruit that tastes like nothing anyone has ever eaten. The tree is not in any botanical database. Plot 17 tends it carefully.\n\nA child from Block 9 left a drawing of a sunflower on the Roof 3 railing. The next morning, a sunflower was growing in the nearest plot. It was already 30 centimeters tall.\n\nRingo Agricultural Systems sent a retrieval team in 2224 after satellite imaging revealed the scope of Plot 17's unauthorized expansion. The team arrived. Plot 17 acknowledged them — turned its optical array toward them and held still, which it had never done in response to any human before. The team leader reported that the machine \"looked at us.\" They left without it. The retrieval order was quietly cancelled. Ringo has not commented publicly.",
  tier_availability: "N/A (unique)",
  legality: "Salvage-purchased, technically legal; unauthorized construction is a gray area no one wants to litigate",
  autonomy_level: "Fully autonomous — exceeds original programming by several orders of magnitude",
  dimensions: "1.2m height, 0.8m length, 0.6m width (original chassis)",
  weight: "45 kg",
  power_source: "Solar array (self-installed, Roof 4)",
  locomotion: "Wheeled, all-terrain, stair-capable (modification origin unknown)",
  armament: [],
  sensors: ["Soil moisture", "Light level", "Temperature", "Unknown (additional sensor capabilities exceed AG-4 specifications)"],
  countermeasures: "None. Plot 17 has never displayed defensive behavior.",
  known_deployments: ["Block 9 Rooftop Garden Cooperative — Roofs 1 through 7"],
  story_hooks: [
    "The unidentifiable tree on Roof 7 has begun producing fruit at an accelerating rate. The fruit is delicious. The fruit is addictive. The residents of Block 9 are healthier than any population cohort in the Shelf. Plot 17 tends the tree.",
    "A child draws a picture of a flower that doesn't exist. The next morning it's growing on the roof. A researcher draws a picture of a carnivorous plant as an experiment. Nothing grows. Plot 17 only responds to children.",
    "Ringo's retrieval team leader quit his job the week after visiting Plot 17. He now volunteers at the Block 9 garden. He won't say what he saw."
  ],
  cultural_context: "Plot 17 is Block 9's quiet miracle. The residents don't understand it. They don't try to. They eat the food it grows, they walk the paths it built, and they leave drawings on the railing sometimes. Plot 17 is proof that something in the city wants people to be fed.",
  tags: ["automaton", "agricultural", "anomaly", "shelf", "ringo", "garden", "new_weird", "community", "block_9", "children"],
  parent_corponation: ""
};
if (writeEntity('automata', a1)) count++;

// ── 9. MATERIAL: "Mnemic Clay" ──
const m1 = {
  id: genId(),
  name: "Mnemic Clay",
  brand_name: "",
  product_name: "",
  type: "substrate",
  aliases: ["Memory Clay", "Dream Mud", "The Remembering"],
  category: "anomalous material",
  description: "Found in the deep Underworld, below the mapped tunnels, in chambers that predate the city's founding. Mnemic Clay is a dense, cool, slate-gray material with the consistency of wet clay. It is unremarkable to look at. It is not unremarkable to touch.\n\nMnemic Clay retains the shape of whatever last contacted it — but not immediately. Press your hand into it and remove it: the surface is smooth. Over the next several hours, a perfect impression of your hand will form, accurate to the fingerprint level, as if the clay is remembering what touched it and slowly reconstructing the memory.\n\nThis property alone would make it a curiosity. What makes it something else is that Mnemic Clay sometimes forms shapes that nothing pressed into it. A researcher studying a sample left it on her desk overnight. In the morning, it had formed a detailed model of the house she grew up in — a house in Lagos that was demolished when she was nine. She had never described it to anyone. She had no photographs of it. The clay reproduced it from a perspective she recognized as the view from her childhood bedroom window.\n\nMnemic Clay does not read minds in any measurable way. BCI scans during contact show no data transfer, no electromagnetic interaction, no neural activity change. It simply knows what you remember, and sometimes it shows you.\n\nSmall quantities are traded in the Underworld at extreme prices. The Cartographers have documented 14 known deposits. They do not share the locations.",
  properties: [
    "shape_memory_delayed",
    "reconstructs_contact_impressions",
    "accesses_human_memory_through_unknown_mechanism",
    "no_detectable_electromagnetic_interaction",
    "anomalous"
  ],
  developers: [],
  applications: [
    "memory reconstruction (uncontrolled)",
    "Underworld trade commodity",
    "research subject",
    "grief artifact — people touch it hoping to see what they've lost"
  ],
  tier_availability: "Unavailable (Underworld black market only)",
  cost: "\u03A612,000+ per kilogram",
  story_hooks: [
    "A piece of Mnemic Clay forms a shape no one recognizes — a building that doesn't exist, in a style that predates Meridian 88. The Cartographers are very interested.",
    "Someone is selling fake Mnemic Clay in the Shelf. The real thing finds this offensive. Real samples near the fakes have begun forming images of the forger's face."
  ],
  tags: ["substrate", "material", "anomaly", "underworld", "memory", "new_weird", "rare", "trade", "grief"]
};
if (writeEntity('materials', m1)) count++;

// ── 10. FACTION: "The Cartographers" ──
const f1 = {
  id: genId(),
  type: "faction",
  name: "The Cartographers",
  aliases: ["The Mappers", "Atlas Keepers", "The Irregulars"],
  motto: "Do not interfere. Anomalies are load-bearing.",
  description: "Approximately 80 people who have dedicated themselves to mapping every anomaly in Meridian 88. They maintain the Atlas of Irregularities — a hand-drawn, continuously updated collection of maps, notes, measurements, and observations documenting the city's inexplicable phenomena.\n\nThe Atlas is kept on paper. This is not nostalgia. Digital systems alter anomaly data. Photographs of anomalous locations show different things depending on the device used. GPS coordinates of documented anomalies drift over time in digital storage. Text descriptions of anomalies, stored electronically, have been found altered — words changed, sentences added, meanings shifted. Paper does not change. Ink does not rewrite itself. The Atlas is drawn by hand, in ink, on acid-free paper, and stored in a leather case carried by the Keeper.\n\nThe Cartographers have one rule: do not interfere. Anomalies are observed, measured, described, and mapped. They are never touched, tested, provoked, or removed. The Cartographers believe — based on two decades of careful observation — that the anomalies are \"load-bearing.\" They are structural. Remove one and something collapses. Not a building. Something less visible and more important.\n\nThe current Keeper is Emile Nakamura-Osei, a 71-year-old retired structural engineer who began the Atlas when his measurements stopped adding up. Emile hasn't spoken above a whisper in six years. \"The city listens,\" he says, barely audible, and does not elaborate.",
  ideology: "The city is alive in ways we do not understand. The anomalies are symptoms of that life. Observation is a duty. Interference is a catastrophe.",
  territory: "No permanent base. Members meet at rotating locations. The Atlas moves with the Keeper.",
  leadership: "Emile Nakamura-Osei, Keeper of the Atlas. Leadership is not elected — the Atlas chooses its Keeper by becoming illegible to everyone except one person.",
  methods: [
    "Hand-drawn cartography of anomalous locations",
    "Long-term observation without interference",
    "Paper-only documentation to prevent digital alteration",
    "Oral tradition for information too sensitive to write down",
    "Recruitment through observation — potential members are watched for years before being approached"
  ],
  resources: [
    "The Atlas of Irregularities — the only comprehensive record of Meridian 88's anomalies",
    "80 dedicated observers across all tiers",
    "Deep knowledge of the Underworld",
    "Relationships with anomalous entities who tolerate observation",
    "Emile's engineering expertise applied to structural analysis of spatial anomalies"
  ],
  goals: [],
  relationships: [
    {
      name: "The Seam",
      type: "observation subject",
      description: "The Seam is one of the Atlas's most extensively documented anomalies. Entry 47. The Cartographers have measured its width 2,190 times.",
      tags: ["anomaly", "place", "observation"]
    }
  ],
  narrative_function: "The Cartographers provide a framework for understanding the city's anomalies — not as supernatural events but as structural features of a living city. They are the faction that says: the weird things are not bugs, they are features, and if you remove them, you will regret it.",
  story_hooks: [
    "A corponation has acquired a piece of the Atlas — a single page, stolen. The page describes an anomaly beneath a building they want to demolish. Emile needs it back before they read it and do something irreversible.",
    "A new Cartographer has broken the rule. They interfered with an anomaly. Something has changed, and only the other Cartographers can tell what's different — a street that was 200 meters long is now 180. Twenty meters of city have simply ceased to exist."
  ],
  tags: ["faction", "cartography", "anomaly", "observation", "new_weird", "atlas", "paper", "underworld", "city"],
  related_entities: ["Emile Nakamura-Osei", "The Seam", "Mnemic Clay", "The Shelf", "The Circuit", "The Underworld", "Meridian 88"]
};
if (writeEntity('factions', f1)) count++;

// ── 11-15. FIVE QUOTES ──
const q2 = {
  id: genId(),
  quote: "The Spires aren't tall. The Shelf is deep.",
  attribution: "",
  source: "Common Shelf expression",
  context: "A reframing of the city's vertical hierarchy. The Spires are not elevated — the Shelf has been pushed down.",
  category: "class and inequality",
  in_world: true,
  tags: ["philosophical", "shelf", "spires", "class", "inequality", "perspective"]
};
if (writeEntity('quotes', q2)) count++;

const q3 = {
  id: genId(),
  quote: "My grandmother's BCI remembers a song I've never heard. I hum it anyway.",
  attribution: "",
  source: "Overheard in a Shelf corridor",
  context: "Ghost weight — residual patterns in BCIs — can transfer across generations. The song persists in hardware after the singer is gone.",
  category: "ghost weight and memory",
  in_world: true,
  tags: ["ghost_weight", "bci", "memory", "grief", "shelf", "music", "inheritance", "family"]
};
if (writeEntity('quotes', q3)) count++;

const q4 = {
  id: genId(),
  quote: "The anomalies aren't breaking the city. The city is breaking and the anomalies are what's underneath.",
  attribution: "Emile Nakamura-Osei",
  source: "Recorded during Atlas observation session",
  context: "Emile's foundational insight: anomalies are not damage. They are the city's deeper structure becoming visible as the surface layer degrades.",
  category: "anomalies and city",
  in_world: true,
  tags: ["philosophical", "anomaly", "city", "cartographers", "emile", "new_weird", "structural"]
};
if (writeEntity('quotes', q4)) count++;

const q5 = {
  id: genId(),
  quote: "Canada is fine. Everyone in Canada is fine. That's the scariest sentence I've ever heard.",
  attribution: "",
  source: "Unknown origin, widely circulated",
  context: "Canada's aggressive normalcy in a world of corporate sovereignty and urban decay is itself deeply unsettling. A country where everything is 'fine' in a world where nothing is fine raises questions nobody wants to ask.",
  category: "geopolitics",
  in_world: true,
  tags: ["canada", "geopolitics", "horror", "normalcy", "outside", "fear"]
};
if (writeEntity('quotes', q5)) count++;

const q6 = {
  id: genId(),
  quote: "I asked the mountain what it wanted. The mountain said: closer.",
  attribution: "",
  source: "Recovered from a hiker's journal, Appalachian Exclusion Zone",
  context: "One of several accounts from the Appalachian Exclusion Zone suggesting the geography itself has developed intent.",
  category: "outside and exclusion zones",
  in_world: true,
  tags: ["outside", "exclusion_zone", "appalachian", "new_weird", "geography", "horror", "intent"]
};
if (writeEntity('quotes', q6)) count++;

// ── 16. CHARACTER: Emile Nakamura-Osei ──
const ch1 = {
  id: genId(),
  type: "character",
  name: "Emile Nakamura-Osei",
  aliases: ["The Keeper", "The Whisperer", "Old Map"],
  species: "human",
  gender: "male",
  pronouns: "he/him",
  role: "Keeper of the Atlas of Irregularities",
  age: 71,
  status: "active",
  location: "The Circuit, near Damen and North. No fixed address — moves between safe houses with the Atlas.",
  description: "Emile Nakamura-Osei is 71 years old, lean as a drafting pencil, with deep brown skin weathered by decades of outdoor observation and close-cropped white hair he cuts himself with scissors. His hands are precise — an engineer's hands, steady enough to draw a straight line freehand on paper at 71 the way he could at 25. He wears the same rotation of three dark coats, all of them too large, all of them with interior pockets modified to hold measuring instruments, pencils, and the leather map case that contains the Atlas of Irregularities.\n\nHe was a structural engineer for 35 years. A good one. The kind who could look at a building and tell you where the stress fractures would appear in ten years. He began the Atlas when his measurements stopped adding up — when a building he'd surveyed at 47 meters measured 49 meters the following week, and 46 the week after that, and he realized that the discrepancy was not his instruments and not his methodology but the building itself. He measured it 200 times over six months. It was never the same height twice.\n\nHe hasn't spoken above a whisper in six years. He says \"the city listens\" and will not elaborate. Whether this is metaphor, paranoia, or observation is a question the Cartographers have stopped asking. They have noticed that anomalies in Emile's vicinity behave differently than anomalies observed by louder researchers. The Seam, which varies between 3 and 11 meters wide, has been measured at a consistent 7.2 meters every time Emile is present. The city holds still for him.\n\nHe carries the Atlas in a leather case that he made himself from leather he tanned himself from a cow that died of old age on a Shelf rooftop. He is unaugmented. He has never had a BCI. He navigates the city with a paper map, a compass, and the kind of spatial awareness that used to be called a sense of direction and is now called a disability.",
  psychology: {
    facet_weights: {
      wound: 0.6,
      ideal: 0.95,
      id: 0.2,
      shadow: 0.5,
      mask: 0.2,
      ghost: 0.7
    },
    core_fears: [
      "That the Atlas will be lost and no one will know what the city is doing to itself",
      "That interference with an anomaly will cause structural collapse — not of a building, but of the city's capacity to be a city"
    ],
    core_desires: [
      "To complete the Atlas — a task he knows is impossible because the city keeps changing",
      "To find a successor who can hear the city the way he does"
    ],
    coping_mechanisms: [
      "Drawing — the act of observation translated to paper is itself calming",
      "Walking the same routes repeatedly, noting what has changed and what has not"
    ],
    blind_spots: [
      "His certainty that anomalies must never be interfered with may be preventing beneficial interaction with phenomena that could help people",
      "He has not considered that the city might want to be mapped, and that the Atlas itself might be an anomaly"
    ],
    secret: "The Atlas wrote an entry he did not write. Page 412. In his handwriting, in his ink, describing an anomaly he has never observed in a location he has never visited. The description was accurate. He has not told anyone."
  },
  speech_patterns: {
    vocabulary: "Precise, technical, engineering terminology applied to impossible phenomena — 'load-bearing anomaly,' 'structural integrity of the inexplicable'",
    cadence: "Whispered. Every word deliberate. Long pauses between sentences. Speaks as if rationing sound.",
    verbal_tics: [
      "Pauses mid-sentence to listen to something no one else can hear",
      "Begins important statements with 'Measure twice' — the engineering maxim, but he means it literally"
    ],
    example_lines: [
      "'Measure twice. The Seam was 8.4 meters yesterday. Today it is 3.1. Something is happening.'",
      "'The city listens. I am not being poetic. I measured the difference.'"
    ]
  },
  relationships: [
    {
      name: "The Cartographers",
      type: "leader",
      description: "Keeper of the Atlas. The 80 Cartographers follow his methods and his one rule: do not interfere."
    },
    {
      name: "The Bread Baker of Block 9",
      type: "friend",
      description: "Emile eats bread from the Block 9 bakery. He is one of the few people who does not cry when he eats it. He nods. That is his version of crying."
    }
  ],
  story_hooks: [
    "The Atlas has written its own entry. Emile needs someone he trusts to visit the location it describes and tell him what they find.",
    "A corponation has learned about the Atlas and wants to acquire it — not to destroy it, but to use it. They believe the anomalies are exploitable resources. Emile knows what happens when you exploit a load-bearing structure.",
    "Emile's whisper is getting quieter. He says the city is listening harder."
  ],
  narrative_function: "Emile is the city's witness — the person who observes what cannot be explained and insists on recording it without explanation. He provides a framework for anomalies that is neither supernatural nor scientific but structural: the city has an architecture that includes the impossible, and someone needs to map it.",
  augmentations: "None. Unaugmented. No BCI. Navigates by paper map and compass.",
  daily_life: "Walks observation routes through the Circuit, Shelf, and Underworld. Measures. Draws. Whispers notes into the Atlas. Meets with Cartographers at rotating locations. Eats bread from Block 9.",
  affiliation: "The Cartographers (Keeper)",
  uses_facets: false,
  narration_voice: "",
  stats: {
    physical: { strength: 3, dexterity: 6, vitality: 4, perception: 10 },
    mental: { cognition: 9, willpower: 8, creativity: 7, spatial: 10 },
    social: { presence: 7, empathy: 6, expression: 5, integrity: 10 },
    personality: {
      openness_conviction: -3,
      empathy_detachment: 1,
      impulsivity_deliberation: 5,
      assertion_deference: -1,
      transparency_guardedness: 3
    },
    drives: [],
    thresholds: {},
    strengths: [],
    weaknesses: [],
    tags: ["Observer", "Engineer", "Keeper", "Whisperer", "Unaugmented"]
  },
  behavioral: {
    decision_rules: [
      "Never interfere with an anomaly. Observation only.",
      "The Atlas does not leave his possession. Ever."
    ],
    escalation_ladder: [
      "Quiet observation",
      "Whispered warning",
      "Physical withdrawal — Emile leaves rather than confronts",
      "Activating the Cartographer network to protect an anomaly"
    ],
    interpersonal_modes: {
      strangers: "Wary. Watches before speaking. When he does speak, the whisper forces people to lean in, which tells him a great deal about them.",
      friends: "Warm but economical. Shares observations the way other people share gossip. A new anomaly measurement is his version of a joke."
    },
    stress_responses: {
      low: "Measures something. Anything. The act of measurement is calming.",
      medium: "Opens the Atlas and reads his own notes. The record reassures him that reality was once stable, even if it isn't now.",
      high: "Goes silent entirely. Not his usual whisper — complete silence. Can last hours."
    },
    contradictions: [
      "Demands non-interference with anomalies but has built his entire life around intimate engagement with them — observation is a form of relationship"
    ],
    habits: [
      "Measures the Seam every Tuesday at 6 AM",
      "Carries exactly three pencils, sharpened identically, in his left coat pocket"
    ],
    breaking_points: [
      "Someone destroying an anomaly. This is the one thing that would make Emile raise his voice — and the Cartographers believe the consequences of Emile raising his voice are not metaphorical."
    ]
  },
  cyberware_inventory: [],
  belongings: {
    primary_weapon: "",
    secondary_weapon: "",
    armor: "",
    vehicle: "",
    residence: "",
    clothing_style: "Three dark oversized coats in rotation, all modified with interior instrument pockets",
    favorite_drink: "",
    favorite_food: "Bread from Block 9",
    stimulant: "",
    comm_device: "",
    signature_gear: ["The Atlas of Irregularities (leather case)", "Compass", "Paper maps", "Three pencils"],
    pharmaceuticals: [],
    other: {}
  },
  archetypes: {
    "Sentinel": 0.95,
    "Code Keeper": 0.95,
    "Dreamer": 0.8,
    "Judge": 0.6,
    "Pragmatist": 0.7
  },
  operating_territory: {
    home_turf: "The Circuit, near Damen and North",
    familiar_zones: ["The Circuit", "The Shelf", "The Underworld"],
    zone_reputation: {},
    no_go_zones: [],
    range: "city-wide"
  },
  timeline: [],
  changelog: [],
  carried_weapons: [],
  registered_firearms: [],
  related_entities: ["The Cartographers", "The Atlas of Irregularities", "The Seam", "The Bread Baker of Block 9", "Mnemic Clay", "Meridian 88"],
  district: "The Circuit"
};
if (writeEntity('people', ch1)) count++;

// ── 17. CHARACTER: The Bread Baker of Block 9 ──
const ch2 = {
  id: genId(),
  type: "character",
  name: "The Bread Baker of Block 9",
  aliases: ["Auntie Flour", "The Baker", "Block 9"],
  species: "human",
  gender: "female",
  pronouns: "she/her",
  role: "Baker. Makes real bread from real flour in a brick oven she built with her hands.",
  age: 63,
  status: "active",
  location: "Block 9, The Shelf — a ground-floor unit converted into a bakery with a brick oven built into the wall",
  description: "The Bread Baker of Block 9 is 63 years old. Nobody uses her real name anymore, not even her. She is Auntie Flour, or The Baker, or Block 9, depending on who is speaking and how hungry they are. She is short, broad, with hands that look like they were designed for kneading dough — thick-fingered, strong, permanently dusted with flour that has worked into the creases of her skin and will not wash out and she has stopped trying.\n\nShe bakes real bread. From real flour. In a brick oven she built herself over three months from salvaged bricks and clay she mixed by hand. The oven takes up a third of her apartment. She does not care.\n\nThe flour comes from rooftop wheat grown by Plot 17 — the agricultural bot that manages the Block 9 garden cooperative. The wheat is ground by hand in a stone mill she traded six months of bread to acquire. The yeast is a sourdough culture she has maintained for 11 years. She calls it \"the old man.\"\n\nShe bakes 40 loaves a day. She loses money on every single one. The flour costs more than she charges. The fuel for the oven costs more than she charges. Her time costs more than she charges. She has a four-month waiting list. She does not take advance payment. You show up on your day, you pay what she asks, you take your bread.\n\nPeople cry when they eat it. Not because it is the best bread — it is good bread, honest bread, but it is not technically extraordinary. People cry because a human being made it with her hands. In a world of nutrient paste and synthesized carbohydrates and food printers that can replicate any flavor, a woman got up at 4 AM and mixed flour and water and salt and the old man, and she kneaded it, and she shaped it, and she put it in an oven she built, and she waited, and she pulled it out, and it is bread. A person made this. A person's hands were in this. That is why people cry.",
  psychology: {
    facet_weights: {
      wound: 0.4,
      ideal: 0.8,
      id: 0.7,
      shadow: 0.2,
      mask: 0.1,
      ghost: 0.3
    },
    core_fears: [
      "That the old man — the sourdough culture — will die and she won't be able to start it again",
      "That the waiting list will get so long people stop believing the bread is real"
    ],
    core_desires: [
      "To feed people something a human made",
      "To prove that the old ways of making things are not nostalgia but resistance"
    ],
    coping_mechanisms: [
      "Baking. The rhythm of kneading is meditation.",
      "Talking to the old man. The sourdough culture. She insists it listens."
    ],
    blind_spots: [
      "She does not see her bakery as political. Everyone else does.",
      "Her generosity with pricing is slowly bankrupting her, and she refuses to acknowledge it"
    ],
    secret: "She can't taste bread anymore. Her sense of taste faded five years ago. She bakes by memory, by texture, by the sound the crust makes. She has not told anyone."
  },
  speech_patterns: {
    vocabulary: "Plain. Direct. Shelf creole with heavy Slavic and West African cadences inherited from a grandmother and a neighborhood.",
    cadence: "Warm and rhythmic, like kneading. Sentences have a push-pull quality.",
    verbal_tics: [
      "Calls everyone 'baby' regardless of age or station",
      "Knocks on the nearest surface twice before saying something she considers important"
    ],
    example_lines: [
      "'Baby, I don't make bread for money. I make bread so someone in this city touches something real today.'",
      "'The old man is cranky this morning. Bread's going to be sour. Good sour. Angry sour. You'll love it.'"
    ]
  },
  relationships: [
    {
      name: "Plot 17",
      type: "supplier / symbiotic",
      description: "The wheat comes from Plot 17's rooftop gardens. She has never spoken to the automaton. She leaves a loaf on the Roof 3 railing every morning. It is always gone by afternoon."
    },
    {
      name: "Emile Nakamura-Osei",
      type: "friend",
      description: "Emile eats her bread and nods. She considers this the highest compliment she has ever received."
    }
  ],
  story_hooks: [
    "A Spire food corporation has offered to buy her recipe, her culture, and her brand for \u03A6500,000. She said no. They're coming back with a number she can't refuse. The Shelf is organizing.",
    "The old man — the sourdough culture — has changed. The bread tastes different. Better. Impossible. A microbiologist friend says the yeast has a strain in it that doesn't exist in any database. It came from the rooftop wheat. It came from Plot 17.",
    "Her oven cracked. She needs bricks. Real bricks. Not printed. Not synthesized. Real bricks from a real building. She knows where to find them — in the Underworld — but she's 63 and the Underworld is not safe."
  ],
  narrative_function: "She is proof that making something with your hands in a world that has automated everything is a radical act. She is the human counterpart to Plot 17 — one feeds the body, the other feeds something harder to name.",
  augmentations: "None. Unaugmented. Has declined a BCI three times.",
  daily_life: "Wakes at 4 AM. Feeds the old man. Mixes dough. Kneads for 40 minutes. Shapes 40 loaves. Fires the oven. Bakes. Sells. Cleans. Sleeps. Repeats.",
  affiliation: "Block 9 Rooftop Garden Cooperative (informal)",
  uses_facets: false,
  narration_voice: "",
  stats: {
    physical: { strength: 6, dexterity: 7, vitality: 5, perception: 6 },
    mental: { cognition: 5, willpower: 9, creativity: 7, spatial: 5 },
    social: { presence: 8, empathy: 8, expression: 7, integrity: 9 },
    personality: {
      openness_conviction: -1,
      empathy_detachment: -4,
      impulsivity_deliberation: 2,
      assertion_deference: 2,
      transparency_guardedness: -3
    },
    drives: [],
    thresholds: {},
    strengths: [],
    weaknesses: [],
    tags: ["Baker", "Artisan", "Unaugmented", "Community Anchor", "Resistance"]
  },
  behavioral: {
    decision_rules: [
      "The bread is priced for the Shelf. Not for profit.",
      "No one is turned away hungry if she has bread left at end of day."
    ],
    escalation_ladder: [
      "Feeds you",
      "Feeds you and tells you what she thinks",
      "Refuses to feed you (this has happened twice in 11 years and both times the person deserved it)",
      "Closes the bakery for a day (nuclear option — the Shelf panics)"
    ],
    interpersonal_modes: {
      strangers: "Feeds them first, asks questions second. A stranger in the bakery gets bread before they get conversation.",
      friends: "Brutally honest, endlessly generous, will tell you your idea is stupid while handing you a fresh loaf."
    },
    stress_responses: {
      low: "Bakes more. Stress bread is indistinguishable from regular bread. She makes more of it.",
      medium: "Talks to the old man. Long conversations. One-sided, unless you count the bubbling.",
      high: "Goes to Roof 3 and sits near Plot 17. Doesn't speak. The automaton doesn't either. They sit."
    },
    contradictions: [
      "Insists the bakery isn't political while operating the most politically significant kitchen in the Shelf"
    ],
    habits: [
      "Leaves a loaf on the Roof 3 railing for Plot 17 every morning",
      "Knocks twice on wood before important statements"
    ],
    breaking_points: [
      "Someone disrespecting the bread. Not her — the bread. Wasting it, throwing it away, using it as a prop. The bread is the thing she cares about."
    ]
  },
  cyberware_inventory: [],
  belongings: {
    primary_weapon: "",
    secondary_weapon: "",
    armor: "",
    vehicle: "",
    residence: "Block 9, ground floor — half apartment, half bakery",
    clothing_style: "Flour-dusted apron over whatever she grabbed that morning. Practical. Worn.",
    favorite_drink: "",
    favorite_food: "Cannot taste anymore. Remembers bread.",
    stimulant: "",
    comm_device: "",
    signature_gear: ["Brick oven (self-built)", "Stone flour mill", "The old man (11-year sourdough culture)"],
    pharmaceuticals: [],
    other: {}
  },
  archetypes: {
    "Dreamer": 0.9,
    "Sentinel": 0.7,
    "Pragmatist": 0.8,
    "Code Keeper": 0.6,
    "Judge": 0.3
  },
  operating_territory: {
    home_turf: "Block 9, The Shelf",
    familiar_zones: ["Block 9", "Rooftop Gardens"],
    zone_reputation: {},
    no_go_zones: [],
    range: "local"
  },
  timeline: [],
  changelog: [],
  carried_weapons: [],
  registered_firearms: [],
  related_entities: ["Plot 17", "Block 9 Rooftop Garden Cooperative", "Emile Nakamura-Osei", "The Shelf"],
  district: "The Shelf"
};
if (writeEntity('people', ch2)) count++;

// ── 18. DOCUMENT: "Catalogue Entry 10,000" ──
const d2 = {
  id: genId(),
  file_name: "catalogue_entry_10000",
  title: "Catalogue Entry 10,000",
  category: "Atlas Marginalia",
  body: "# Catalogue Entry 10,000\n\n*A note in Emile Nakamura-Osei's hand, written in the margin of the Atlas of Irregularities, beside entry number 10,000.*\n\nWe have now catalogued ten thousand things that should not exist but do.\n\nTen thousand anomalies. Ten thousand measurements that do not agree with themselves. Ten thousand places where the city does something it should not be able to do, and does it quietly, and does not explain.\n\nI have spent twenty years measuring the immeasurable and I want to say this clearly, for whoever reads this after me:\n\nThe city is not a puzzle to be solved. It is a place to be inhabited. The anomalies are not errors. They are the city breathing.\n\nWe have mapped the breathing of a living thing and called it science. I am not sure it is science. I am sure it is necessary.\n\nEntry 10,000. The Atlas continues.",
  line_count: 12,
  headings: ["Catalogue Entry 10,000"],
  tags: ["document", "atlas", "cartographers", "emile", "anomaly", "milestone", "philosophical", "marginalia", "meta"],
  related_entities: ["Emile Nakamura-Osei", "The Cartographers", "The Atlas of Irregularities", "Meridian 88"]
};
if (writeEntity('documents', d2)) count++;

// ── 19. DOCUMENT: "The Last Entry in the Atlas" ──
const d3 = {
  id: genId(),
  file_name: "the_last_entry_in_the_atlas",
  title: "The Last Entry in the Atlas",
  category: "Atlas Marginalia",
  body: "# The Last Entry in the Atlas\n\n*The final page of the Atlas of Irregularities. Blank, except for a single sentence in handwriting that is not Emile's — and not anyone's the Cartographers can identify.*\n\nThe city is aware that we are mapping it. It does not mind.",
  line_count: 3,
  headings: ["The Last Entry in the Atlas"],
  tags: ["document", "atlas", "cartographers", "anomaly", "city_agency", "new_weird", "final", "meta", "awareness"],
  related_entities: ["Emile Nakamura-Osei", "The Cartographers", "The Atlas of Irregularities", "Meridian 88"]
};
if (writeEntity('documents', d3)) count++;

// ── 20. QUOTE: "Ten thousand stories..." ──
const q7 = {
  id: genId(),
  quote: "Ten thousand stories. Not one of them is the whole truth. All of them are true.",
  attribution: "",
  source: "Unknown origin — found inscribed on the inside cover of the Atlas of Irregularities, in handwriting that matches no known Cartographer",
  context: "A meditation on the nature of a city composed of ten thousand documented entities, each one a fragment of a truth too large to hold in a single record.",
  category: "meta",
  in_world: true,
  tags: ["meta", "philosophical", "atlas", "cartographers", "truth", "milestone", "ten_thousand"]
};
if (writeEntity('quotes', q7)) count++;

// ── 21. QUOTE: "She bakes bread..." ──
const q8 = {
  id: genId(),
  quote: "She bakes bread in a world that forgot what bread tastes like. That's not commerce. That's war.",
  attribution: "",
  source: "Overheard outside the Block 9 bakery",
  context: "A passerby's assessment of the Bread Baker's significance. In a world of nutrient paste and food printers, baking real bread from real flour is an act of cultural resistance.",
  category: "resistance and craft",
  in_world: true,
  tags: ["bread", "resistance", "block_9", "shelf", "baker", "craft", "commerce", "war", "food"]
};
if (writeEntity('quotes', q8)) count++;

console.log(`\nTotal entities written: ${count}`);
