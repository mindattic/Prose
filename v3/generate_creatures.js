// Creature content generator for StreetSamurai
// Generates the GLMZ creatures repository — urban fauna, lab escapees, chimeras, flora, anomalies
// Run: node generate_creatures.js [--dry-run] [--limit N] [--category <category>]

'use strict';

const fs = require('fs');
const https = require('https');
const path = require('path');
const crypto = require('crypto');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join('D:', 'Projects', 'MindAttic', 'StreetSamurai', 'engine', 'data', 'creatures');

// ── CLI args ──
const args = process.argv.slice(2);
const DRY_RUN = args.includes('--dry-run');
const limitIdx = args.indexOf('--limit');
const LIMIT = limitIdx !== -1 ? parseInt(args[limitIdx + 1]) : null;
const categoryIdx = args.indexOf('--category');
const CATEGORY_FILTER = categoryIdx !== -1 ? args[categoryIdx + 1] : null;

// ── Helpers ──
function slugify(name) {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '').substring(0, 80);
}

function fileExists(name) {
  return fs.existsSync(path.join(OUTPUT_DIR, slugify(name) + '.json'));
}

function writeCreatureFile(creature) {
  const slug = slugify(creature.name || 'unnamed');
  const filePath = path.join(OUTPUT_DIR, slug + '.json');
  if (!creature.id) creature.id = crypto.randomBytes(16).toString('hex');
  fs.writeFileSync(filePath, JSON.stringify(creature, null, 2));
  return filePath;
}

function readExistingNames() {
  if (!fs.existsSync(OUTPUT_DIR)) return [];
  return fs.readdirSync(OUTPUT_DIR)
    .filter(f => f.endsWith('.json'))
    .map(f => {
      try {
        const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
        return data.name || null;
      } catch { return null; }
    })
    .filter(Boolean);
}

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

// ── API ──
function callClaude(system, user, maxTokens = 8192) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 1.0,
      system: system,
      messages: [{ role: 'user', content: user }]
    });
    const req = https.request({
      hostname: 'api.anthropic.com',
      path: '/v1/messages',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': API_KEY,
        'anthropic-version': '2023-06-01',
      }
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.error) {
            const err = new Error(j.error.message || JSON.stringify(j.error));
            err.status = res.statusCode;
            err.type = j.error.type;
            return reject(err);
          }
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function parseJsonArray(text) {
  let json = text.trim();
  if (json.startsWith('```')) {
    json = json.substring(json.indexOf('\n') + 1);
    if (json.endsWith('```')) json = json.slice(0, -3);
    json = json.trim();
  }
  return JSON.parse(json);
}

// ── System prompt (shared across all batches) ──
const SYSTEM_PROMPT = `You generate creature, flora, chimera, and anomaly entries for a near-future worldbuilding project called StreetSamurai, set in GLMZ (Great Lakes Megacity Zone) in the year 2200.

WORLD RULES:
- GLMZ spans the old Chicago metro area to western Michigan, with Lake Michigan as its eastern edge. Population ~100 million.
- Currency is Φ (Quanta). Do NOT call it "phi" — Φ is the QUANTA symbol.
- There is NO city police. Arcturus Civil Security is the closest equivalent to law enforcement.
- Society is tiered 1-5: Tier 1 = poorest/street level, Tier 5 = corporate elite.
- The city has been growing and decaying for 200 years. Urban ecology is deep and strange.
- Three major genemod corporations have operated for 200 years, causing dozens of lab escapes and intentional releases:
  - Crucible Genomics (agricultural and industrial)
  - Helix Biosystems (medical and enhancement)
  - Lacuna Genomics (neural and experimental)
- The Chicago River system, drainage canals, lakefront, and vast underground infrastructure are full of adapted life.
- Chimeras: humans who have undergone so many genemods they have become more animal than human. Some chose it. Some were experimented on. Some are second or third generation born into chimera communities. They are NOT monsters — they have culture, legal advocates, and complicated relationships with baseline humans. Chimera legal status is contested.
- Tone: grounded weird. Not fantasy monsters. Everything has a plausible biological or ecological explanation, even if that explanation is strange. The wonder is in how ordinary people have adapted to living alongside these creatures.
- Razor Rats: large rats with keratinized spine-ridges along their backs, adapted to navigate razor-wire fencing. Canonical Tier 1 pest.
- Glowing Pigeons: bioluminescent lichen has grown into their plumage from decades of contact with bio-luminescent runoff. The lichen IS the glow — not native to the bird. Creates beautiful/eerie flocks at night.
- Default to mixed heritage from unexpected global combinations (Ubiquitous Diaspora) when describing cultural context.
- Missouri is flooded, Kentucky is gone. GLMZ absorbs refugees. Stories are told by people met once.

CREATURE JSON SCHEMA (return an array of these):
{
  "id": "(generate a 32-char hex string)",
  "name": "creature name",
  "type": "creature",
  "aliases": ["street name", "slang"],
  "category": "urban_fauna|modified_fauna|chimera|flora|invasive|vermin|apex|water|aerial",
  "origin": "natural_mutation|lab_escape|genemod_accident|intentional_release|adaptation|chimera_degradation",
  "description": "3 paragraphs: what it looks like, how it behaves/survives, cultural/social significance in GLMZ",
  "habitat": ["Shelf districts", "Circuit underbelly", "drainage tunnels"],
  "diet": "what it eats",
  "threat_level": "negligible|low|medium|high|extreme",
  "notable_traits": ["trait1", "trait2"],
  "encounters": "How people typically encounter this creature — where, when, what happens",
  "known_uses": "If any — medicine, pest control, food, companionship, etc. Null if none.",
  "story_hooks": ["Hook 1", "Hook 2"],
  "related_entities": [],
  "tags": ["creature", "category tag", "origin tag"]
}

Return ONLY a valid JSON array. No markdown, no explanation. Strip all \`\`\` fences if you use them.`;

// ── Batch manifest ──
// Each batch has: label, category, origin, prompt (specific instructions for this batch)
const BATCHES = [
  // ── VERMIN / URBAN_FAUNA ──
  { label: 'Razor Rats & Wire-Adapted Vermin', category: 'vermin', origin: 'natural_mutation',
    prompt: 'Generate 8 rat and rodent species adapted to GLMZ wire and ruin infrastructure. Include the canonical Razor Rat (keratinized spine-ridges, wire-adapted). Others: wire-adapted strains, albino drainage rats, pack hunters, nest-builders inside electrical conduit, scavengers with chemical resistance.' },

  { label: 'Shelf Brown Rats — Large Variants', category: 'vermin', origin: 'natural_mutation',
    prompt: 'Generate 8 MASSIVE rat variants found in Shelf districts (Tier 1-2 areas). Semi-intelligent pack behavior, territory marking, complex social hierarchies, one that appears to have crude tool use. Each should feel like a distinct species with its own niche.' },

  { label: 'Drain Eels & Tunnel Swimmers', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 eel and snake-like creatures that live in GLMZ drainage infrastructure. Blind or near-blind, electric-sensing, adapted to sewage and industrial runoff. Some may descend from pet snakes, others from engineered organisms. Include Drain Eels specifically.' },

  { label: 'Vent Possums & HVAC Fauna', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 small mammal species adapted to HVAC systems, steam pipes, and ventilation networks. Heat-tolerant, small, extremely quiet. Include Vent Possums. Others: heat-adapted squirrels, pipe ferrets, steam-loving shrews, miniature raccoon descendants.' },

  { label: 'Wire Sparrows & Copper Nesters', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 small bird species that have integrated electrical infrastructure into nesting and behavior. Include Wire Sparrows (nests from copper wire, mildly electromagnetic). Others: birds that shelter in transformers, use static to repel parasites, navigate by city EM fields.' },

  { label: 'Tunnel Moles & Underground Excavators', category: 'urban_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 burrowing species found in GLMZ underground. Include engineered Tunnel Moles (escaped from Crucible Genomics agricultural testing, now massive and feral). Others: blind cave crickets, giant earthworms from soil remediation experiments, pill bugs the size of cats.' },

  { label: 'Shelf Cockroaches — Adapted Strains', category: 'vermin', origin: 'natural_mutation',
    prompt: 'Generate 8 distinct cockroach strains that have adapted to different GLMZ environments. Include chemical-resistant strains, heat-seeking variants, bioluminescent colonies, one that is nearly armored, one that mimics floor grating, one that is farmed for protein. Each a distinct species.' },

  { label: 'Sewer Crabs & Drainage Crustaceans', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 crustacean species adapted to GLMZ drainage and canal systems. Include Sewer Crabs (freshwater crabs from the Chicago drainage canal network, aggressive, territorial). Others: freshwater crayfish variants, blind cave shrimp, heavily calcified armored variants, one that filters industrial toxins.' },

  { label: 'Pipe Centipedes & Multi-Legged Infrastructure Fauna', category: 'vermin', origin: 'natural_mutation',
    prompt: 'Generate 8 multi-legged arthropods adapted to GLMZ pipe and tunnel systems. Include Pipe Centipedes (several meters long, steam-tolerant). Others: heat-adapted millipedes, armored silverfish, giant isopods in drainage, one species that builds communal nests blocking utility access.' },

  // ── AERIAL FAUNA ──
  { label: 'Glowing Pigeons — Bioluminescent Variants', category: 'aerial', origin: 'genemod_accident',
    prompt: 'Generate 8 glowing pigeon variants. The canonical species has bioluminescent lichen grown into plumage from bio-luminescent runoff — the lichen IS the glow. Generate distinct color variants: blue-green, amber, deep red, white-pulsing, ultraviolet, flickering, steady. Include cultural significance — flocks seen as omens, beauty, or useful nighttime navigation.' },

  { label: 'Glass Falcons & Urban Raptors', category: 'aerial', origin: 'natural_mutation',
    prompt: 'Generate 8 raptor and hawk species adapted to the GLMZ city sky. Include Glass Falcons (urban peregrines with partially transparent wing membranes from genetic drift). Others: roof-nesting hawks, falcons that hunt drones, a species that has learned to use updrafts from industrial chimneys.' },

  { label: 'Spark Starlings & Metallic Birds', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 starling and flock-bird species with metallic or electromagnetic traits. Include Spark Starlings (metallic feather pigmentation, murmuration disrupts low-power electronics). Others: iron-blue starlings, birds that cache metal fragments, one whose synchronized flock patterns encode navigational information passed between individuals.' },

  { label: 'Thermal Kites & Updraft Hunters', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 hawk, kite, and vulture species that exploit the thermal landscape of GLMZ — HVAC exhaust columns, industrial heat vents, building thermals. Include Thermal Kites (rarely land, ride exhaust columns). Others: adapted vultures, a species that hunts by diving into vents, one that sleeps on the wing.' },

  { label: 'Drone Gulls & Lake Shore Opportunists', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 gull and shore bird species that have adapted to the GLMZ lakefront and delivery drone environment. Include Drone Gulls (learned to recognize and follow specific drone flight patterns). Others: gulls that mob delivery drones, lake shore hunters, one that has learned to intercept drone package drops.' },

  { label: 'Night Herons & Artificial-Light Adapted Waders', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 heron, egret, and wading bird species adapted to GLMZ artificial light cycles. Include Night Herons (lake shore herons active at 3am, fully shifted to artificial light). Others: light-adapted egrets, birds that hunt near flood lights, one species where juveniles have never experienced natural dawn.' },

  // ── WATER / LAKE FAUNA ──
  { label: 'Lake Lamprey — Variants', category: 'water', origin: 'natural_mutation',
    prompt: 'Generate 8 lamprey variants found in Lake Michigan and the Chicago River. Include larger, more aggressive strains and at least one bioluminescent deep-water variant. Describe the ecological and cultural impact — are they feared, harvested, used for anything? Some people eat them.' },

  { label: 'Deepwater Carp & Ancient Lake Fish', category: 'water', origin: 'adaptation',
    prompt: 'Generate 8 large, slow, ancient fish species that have persisted in Lake Michigan through 200 years of industrial runoff. Include Deepwater Carp (genetic drift from decades of runoff, massive, slow, ancient-seeming). Others: scaled survivors, mirror carp variants, one almost mythological in Tier 1 fishing communities.' },

  { label: 'Surge Catfish & Electric Canal Fish', category: 'water', origin: 'genemod_accident',
    prompt: 'Generate 8 catfish and bottom-feeder species with electroreceptive or electric-organ traits. Include Surge Catfish (partial electric organ, stuns prey in shallows). Others: blind variants, armored bottom-feeders, one that builds elaborate mud nests against drainage walls.' },

  { label: 'Filter Jellyfish & Lake Invertebrates', category: 'water', origin: 'lab_escape',
    prompt: 'Generate 8 unusual aquatic invertebrate species that have appeared in Lake Michigan and the canal system. Include Filter Jellyfish (freshwater jellyfish of unknown origin, non-stinging, translucent, beautiful). Others: giant freshwater snails, colonial organisms, one that filters and concentrates specific pharmaceuticals.' },

  { label: 'Tide Mussels & Aquaculture Escapes', category: 'water', origin: 'intentional_release',
    prompt: 'Generate 8 shellfish and filter-feeding species escaped from GLMZ aquaculture operations. Include Tide Mussels (engineered shellfish, filter toxins, mildly toxic themselves). Others: engineered oysters, zebra mussel descendants with additional modifications, one species that forms reefs over old submerged infrastructure.' },

  { label: 'Canal Otters & Semi-Aquatic Predators', category: 'water', origin: 'adaptation',
    prompt: 'Generate 8 semi-aquatic mammal species that have colonized GLMZ canal and drainage systems. Include Canal Otters (aggressive, territorial, forms packs in drainage canals). Others: mink variants, adapted beavers building dams in industrial drainage, a species that has learned to use submerged tunnels as ambush corridors.' },

  // ── MODIFIED FAUNA — LAB ESCAPEES ──
  { label: 'Crucible Pigs — Agricultural Escapees', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 pig and boar species escaped from Crucible Genomics agricultural testing. Include the canonical Crucible Pigs (extra muscle, disease resistance, unexpected intelligence). Others: chemically resistant strains, ones with vestigial augmentation hardware still attached, a third-generation feral lineage now fully adapted to city life, one that is nearly as intelligent as a dog and is sometimes kept as a companion.' },

  { label: 'Helix Monkeys — Primate Test Escapees', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 primate species escaped from Helix Biosystems testing facilities. Some still have partial augmentations active. Include tool use, social organization, territorial behavior. At least one species lives in the upper structure of bridges or rail networks. One has learned to disable security cameras.' },

  { label: 'Lacuna Moths & Bioluminescent Insects', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 moth and butterfly species escaped from Lacuna Genomics engineering experiments. Include Lacuna Moths (bioluminescent, engineered for unknown purpose, swarm light sources, paper-thin wings). Others: moths with toxic scales, ones that navigate by EM fields, one whose larvae are used in neural tissue repair research.' },

  { label: 'Thornback Beetles & Agricultural Defenders', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 beetle species from Crucible Genomics agricultural defense testing. Include Thornback Beetles (armored, originally designed to attack crop-threatening insects, now attack anything that approaches their territory). Others: beetles with chemical sprays, one that has become an invasive apex invertebrate predator.' },

  { label: 'Gel Dogs & Subdermal-Modified Canines', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 feral dog breeds descended from Helix Biosystems experiments. Include Gel Dogs (subdermal gel-pack impact resistance, surprisingly affectionate even feral). Others: heat-tolerant breeds, dogs with enhanced night vision still partially active, one lineage that has formed a loose alliance with a Tier 1 community.' },

  { label: 'Clone Deer & Synchronized Wildlife', category: 'modified_fauna', origin: 'genemod_accident',
    prompt: 'Generate 8 ruminant species from failed cloning and domestication experiments. Include Clone Deer (genetically identical individuals, move in perfect synchrony, deeply unsettling). Others: synchronized elk-like creatures, goats from a failed urban agriculture program, one species where all members appear to share a form of distributed awareness.' },

  { label: 'Scaffold Spiders & Structural Arachnids', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 spider species engineered for construction and structural purposes that have escaped into GLMZ buildings. Include Scaffold Spiders (webs structurally strong, now infest old buildings). Others: spiders engineered for cable management, ones whose webs conduct electricity, one used as informal structural reinforcement in Tier 1 housing.' },

  { label: 'Echo Bats & Electronic-Sensing Chiroptera', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 bat species with enhanced or modified sonar from Lacuna Genomics neural experiments. Include Echo Bats (detect electronics, swarm in Circuit after dark). Others: bats that navigate encrypted wireless networks, ones that feed on the insects drawn to server heat, one species that has learned to recognize individual human electronic signatures.' },

  { label: 'Compression Snakes & Industrial Constrictors', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 large constrictor and snake species descended from industrial engineering experiments. Include Compression Snakes (engineered musculature for cable management, now very large). Others: heat-seeking variants that infiltrate server rooms, ones with chemical resistance, a species that uses abandoned conduit networks as dens.' },

  { label: 'Acid Frogs & Modified Amphibians', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 frog and amphibian species from lab escapes with modified skin secretions. Include Acid Frogs (modified skin secretions, chemically burned several people before understood). Others: pharmaceutical-secreting tree frogs used in black market medicine, translucent species with visible internal organs, one that has colonized the drainage system in huge numbers.' },

  // ── FLORA ──
  { label: 'Circuit Vine & Electrical Flora', category: 'flora', origin: 'intentional_release',
    prompt: 'Generate 8 vine and climbing plant species that have integrated into GLMZ electrical infrastructure. Include Circuit Vine (fast-growing ivy integrates into electrical conduit, causes shorts, nearly impossible to kill). Others: plants that drain small amounts of current, heat-seeking vines that grow toward server exhaust, one used as informal insulation by Tier 1 residents.' },

  { label: 'Bone Moss & Dead Zone Flora', category: 'flora', origin: 'adaptation',
    prompt: 'Generate 8 moss and lichen species colonizing GLMZ dead zones, abandoned infrastructure, and post-industrial surfaces. Include Bone Moss (grows on concrete and metal in dead zones, smells of copper). Others: grey-white mosses that slowly break down metal, lichen that maps pollution, one species that only grows where someone has died.' },

  { label: 'Bleed Flower & Pharmaceutical Flora', category: 'flora', origin: 'genemod_accident',
    prompt: 'Generate 8 flowering plants descended from pharmaceutical engineering accidents. Include Bleed Flower (red-flowering, mildly analgesic contact, very beautiful). Others: plants with sedative pollen, ones with toxic berries that are also nutritionally dense, a purple-blooming species whose sap is used in black market neural enhancers.' },

  { label: 'Shelf Lichen — Multiple Varieties', category: 'flora', origin: 'adaptation',
    prompt: 'Generate 8 lichen species colonizing Shelf and Tier 1 buildings. Include the bioluminescent strain that infected pigeon plumage. Others: cold-weather variants, pollution-indicator species, one that grows in symbols and patterns that Tier 1 communities use for navigation, one whose spores cause vivid dreams if inhaled.' },

  { label: 'Waste Mushrooms & Industrial Fungi', category: 'flora', origin: 'adaptation',
    prompt: 'Generate 8 fungal species adapted to GLMZ industrial waste and urban substrate. Some edible, some hallucinogenic, some toxic. Include Waste Mushrooms as a category. Others: bioluminescent fungi in drainage tunnels, ones that fruit from concrete cracks, one species with caps that mimic the color of the wall they grow from.' },

  { label: 'Night Jasmine & Air Filtration Flora', category: 'flora', origin: 'intentional_release',
    prompt: 'Generate 8 engineered flowering plants originally deployed for air filtration that now grow wild. Include Night Jasmine (blooms midnight, intoxicating scent, grows on Tier 1 fences). Others: day-blooming species with different scent profiles, one that filters heavy metals, one whose flowers are harvested for perfume in Tier 3 markets.' },

  { label: 'Tangle Root & Foundation-Undermining Plants', category: 'flora', origin: 'lab_escape',
    prompt: 'Generate 8 plant species with aggressive root systems that infiltrate and undermine GLMZ infrastructure. Include Tangle Root (roots spread through concrete cracks, undermine foundations over years). Others: plants that follow water pipes, ones that clog drainage, one species prized by Tier 1 communities for how its roots reinforce makeshift walls.' },

  { label: 'Signal Reed & EM-Sensitive Flora', category: 'flora', origin: 'genemod_accident',
    prompt: 'Generate 8 reed and grass species with electromagnetic sensitivity, growing near GLMZ comm infrastructure. Include Signal Reed (grows near comm tower bases, sways without wind near active transmissions). Others: grasses that change color near strong EM fields, species used as informal signal detectors by residents, one that has become a cultural symbol for communication and secrecy.' },

  { label: 'Invasive Engineered Plants', category: 'invasive', origin: 'intentional_release',
    prompt: 'Generate 8 invasive plant species originally engineered for specific industrial or agricultural purposes that have spread through GLMZ out of control. Focus on the ecological disruption, attempts to control them, and how Tier 1 communities have adapted to live with them.' },

  { label: 'Toxic Blooms & Chemical Flora', category: 'flora', origin: 'genemod_accident',
    prompt: 'Generate 8 flowering plants with toxic or chemically active properties from genemod accidents. Some are beautiful. Some smell wonderful. All are dangerous in different ways — contact toxins, airborne compounds, concentrated root acids, pollen that triggers specific augmentation malfunctions.' },

  // ── CHIMERAS ──
  { label: 'Feline Chimeras — Voluntary Transformations', category: 'chimera', origin: 'intentional_release',
    prompt: 'Generate 8 distinct feline chimera types — individuals who chose varying degrees of feline transformation. Each should feel like a real person: name, community, reasons for the transformation, what they look like now, their legal status, how they live. Tone: complicated, human, not a fantasy. Some deeply satisfied with their choice. Some have regrets.' },

  { label: 'Canine Chimeras — Communities', category: 'chimera', origin: 'intentional_release',
    prompt: 'Generate 8 canine chimera individuals and community types. Focus on pack dynamics, community organization, how canine chimera communities have built neighborhoods in Tier 1 and excluded zones. At least one named community elder. At least one chimera who acts as a legal advocate.' },

  { label: 'Avian Chimeras — Flight-Adapted', category: 'chimera', origin: 'intentional_release',
    prompt: 'Generate 8 avian chimera types, focusing on the engineering challenges and social realities of flight-adapted humans. Not everyone achieves true flight — most achieve gliding or enhanced agility. Include cultural practices around aerial navigation in the city, avian chimera roosting communities in high structures.' },

  { label: 'Aquatic Chimeras — Canal Dwellers', category: 'chimera', origin: 'intentional_release',
    prompt: 'Generate 8 aquatic chimera types adapted for extended submersion, canal navigation, and lakefront life. Their communities live in partially submerged infrastructure. Describe their relationship with the water infrastructure workers, canal maintenance, Arcturus Civil Security patrols.' },

  { label: 'Insectoid Chimeras — Hive-Adjacent', category: 'chimera', origin: 'genemod_accident',
    prompt: 'Generate 8 insectoid chimera types. Some chose it. Some are accidents from Lacuna Genomics experiments. Describe the social friction — insectoid chimeras face the most discrimination. Include cultural practices, community structures, and at least one famous insectoid chimera activist.' },

  { label: 'Rodent Chimeras — Infrastructure Communities', category: 'chimera', origin: 'chimera_degradation',
    prompt: 'Generate 8 rodent chimera types who live in the tunnel and infrastructure networks below GLMZ. Some are third-generation — born into chimera lineages. Explore what "chimera degradation" means: the drift toward animal behavior in later generations, the communities trying to preserve baseline human culture, the philosophical debate about identity.' },

  { label: 'Involuntary Chimeras — Lab Escapes', category: 'chimera', origin: 'lab_escape',
    prompt: 'Generate 8 involuntary chimeras — individuals who were experimented on without consent by Crucible Genomics, Helix Biosystems, or Lacuna Genomics. Their transformations were not chosen. Some escaped test facilities. Tone: this is trauma and survival. Their community networks, their relationship to their own bodies, attempts at legal recourse.' },

  { label: 'Partial Chimeras — Early Stage', category: 'chimera', origin: 'genemod_accident',
    prompt: 'Generate 8 partial chimeras — individuals who are early-stage or ambiguous, still legally classified as human in most jurisdictions. The legal gray zone. Some are in the middle of a transformation they chose. Some experienced unintended drift from augmentation side-effects. What does it mean to be almost a chimera? When does the law reclassify you?' },

  { label: 'Second and Third Generation Chimeras', category: 'chimera', origin: 'chimera_degradation',
    prompt: 'Generate 8 second and third generation chimeras — individuals born to chimera parents who never had a baseline human life. They navigate a world designed for baseline humans. Their cultural identity is not "transformed human" but simply who they are. Include named characters with histories, relationships, goals.' },

  { label: 'Famous and Infamous Chimeras', category: 'chimera', origin: 'intentional_release',
    prompt: 'Generate 8 named, specific chimera characters who are well-known in GLMZ — activists, criminals, artists, information brokers, Arcturus Security consultants. Each should be a fully realized character: appearance, history, public reputation vs private reality, story hooks. Mix chimera types: feline, canine, avian, ambiguous, hybrid.' },

  // ── ANOMALOUS / UNEXPLAINED ──
  { label: 'Slightly Wrong Animals — Unexplained Fauna', category: 'urban_fauna', origin: 'natural_mutation',
    prompt: 'Generate 8 animals that are "slightly wrong" — ordinary species (pigeons, squirrels, sparrows, raccoons, deer) that are subtly off in ways that have no clear explanation. Proportions slightly wrong. Behaviors unexpected. No specimen has been captured for study. Multiple witnesses describe the same individual. They are not monsters — they are simply wrong. Include the cultural anxiety they create.' },

  { label: 'The Silent Deer — Shelf District Anomaly', category: 'urban_fauna', origin: 'natural_mutation',
    prompt: 'Generate 8 deer and ungulate anomalies found deep in Shelf districts where no deer population should exist. They appear at dawn. They do not startle. No breeding population has been found. Where do they go? Generate 8 distinct variants of this phenomenon — different behaviors, different locations, different cultural responses from the communities that see them.' },

  { label: 'Impossible Fish — Drainage Anomalies', category: 'water', origin: 'natural_mutation',
    prompt: 'Generate 8 fish that appear in locations they cannot physically have reached — swimming upstream through drainage that has no physical route to open water, found in sealed basement cisterns, appearing in rooftop water tanks. Each is a distinct mystery. No rational explanation has been confirmed.' },

  { label: 'Assembly Birds — Behavioral Anomalies', category: 'aerial', origin: 'natural_mutation',
    prompt: 'Generate 8 bird species that gather at specific intersections or structures on specific days for no observed reason. Their gatherings are precise and consistent. They do not feed. They do not call. They wait and then disperse. Each entry should be a different species and a different location, with different theories about why.' },

  { label: 'The White Rabbit — Multi-District Sightings', category: 'urban_fauna', origin: 'natural_mutation',
    prompt: 'Generate 8 entries related to the White Rabbit — a single large white rabbit seen in multiple GLMZ districts simultaneously. Generate it as 8 distinct documented sightings: different districts, different witnesses, different details, different theories about what it is. Is it multiple animals? A hoax? Something else? Include witness testimony tone.' },

  { label: 'Unconfirmed Creatures — No Specimen', category: 'urban_fauna', origin: 'natural_mutation',
    prompt: 'Generate 8 creatures that multiple people have described in detail but no specimen has ever been captured, photographed, or confirmed. Consistent enough across witnesses to be credible but always just out of reach. What do witnesses say they saw? What are the competing theories? What would it mean if it were real?' },

  // ── APEX PREDATORS ──
  { label: 'Shelf Coyote — Urban Apex Canid', category: 'apex', origin: 'adaptation',
    prompt: 'Generate 8 coyote and wolf-descendant species that have become apex predators in different GLMZ zones. Include Shelf Coyote (coordinated group hunting, understands traffic patterns, stalks prey across multiple city blocks). Others: canal pack hunters, rooftop stalkers, one lineage that has been semi-tamed by Tier 1 communities as guard animals.' },

  { label: 'Feral Corporation Dogs — Security Breed Packs', category: 'apex', origin: 'lab_escape',
    prompt: 'Generate 8 feral dog pack species descended from corporate security breeds. They retain partial trained behaviors — responding to certain commands, forming pack hierarchies resembling security team structures, guarding specific territory. Include named pack groups, their territories, their complex relationship with GLMZ human communities.' },

  { label: 'Lake Bear & Shoreline Predators', category: 'apex', origin: 'adaptation',
    prompt: 'Generate 8 large mammal species that have adapted to the GLMZ lakefront and suburban margins. Include Lake Bear (coastal black bear adapted to shoreline scavenging, surprisingly urban-tolerant). Others: adapted raccoon megafauna, feral cattle from a failed urban agriculture project, one species that has learned to fish the lake using manufactured tools.' },

  // ── ADDITIONAL URBAN_FAUNA (to fill out counts) ──
  { label: 'Urban Raccoons — Extreme Adapters', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 raccoon variants that have adapted to extreme niches in GLMZ — industrial zones, server farms, pharmaceutical labs, luxury district waste systems. Each is a distinct ecological niche. Raccoons in this world are essentially a parallel civilization of medium-sized omnivores exploiting every gap in human infrastructure.' },

  { label: 'Urban Fox — Territory Holders', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 fox variants that have established complex territorial systems across different GLMZ districts. Include foxes that have learned to navigate the gig economy of GLMZ (they follow delivery workers, wait outside food distribution points), one variant that is nearly tame and serves as an informal community sentinel.' },

  { label: 'Infrastructure Cats — Feral Colonies', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 feral cat colony types that have established themselves in different GLMZ infrastructure niches — server farms, food markets, Tier 1 neighborhood blocks, drainage covers, abandoned transit stations. Include their relationship to the communities they neighbor and any useful roles they play.' },

  { label: 'Adapted Pigeons — Standard Varieties', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 standard (non-glowing) pigeon variants adapted to GLMZ — different district ecotypes, pollution-resistant strains, ones that navigate by corporate building logos, ones with unusual color mutations from industrial exposure. Cultural significance in different districts.' },

  { label: 'Swarm Insects — Colony Species', category: 'vermin', origin: 'adaptation',
    prompt: 'Generate 8 swarming insect species with colony behaviors adapted to GLMZ infrastructure. Ants that farm the fungus that grows on electrical insulation. Termites adapted to synthetic building materials. Wasps nesting in abandoned augmentation kiosks. Each with distinct ecological role and human impact.' },

  { label: 'Lake Margin Birds — Shoreline Specialists', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 shoreline and wading bird species that have adapted to the GLMZ lakefront industrial ecology. The lake is cleaner than it was in 2100 but still strange. Birds that eat filter jellyfish, ones that follow the canal otter packs, species that breed on decommissioned shipping infrastructure.' },

  { label: 'Modified Livestock Escapes', category: 'modified_fauna', origin: 'lab_escape',
    prompt: 'Generate 8 domestic livestock species (cattle, sheep, goats, chickens, rabbits) with genemod modifications that have escaped into GLMZ and gone feral. Focus on how the modifications that made them useful in labs have created unusual feral behaviors. Include at least one species that has become an unofficial food source for Tier 1 communities.' },

  { label: 'Engineered Pollinators', category: 'modified_fauna', origin: 'intentional_release',
    prompt: 'Generate 8 bee, wasp, and fly species engineered for urban pollination that have spread beyond their original scope. Some pollinate the engineered flora. Some have learned to pollinate in ways their designers did not anticipate. Include the ecosystem services they provide and the hazards they create.' },

  { label: 'Aerial Invertebrates — Adapted Flying Insects', category: 'aerial', origin: 'adaptation',
    prompt: 'Generate 8 large or unusual flying insect species adapted to GLMZ city air — heat plumes, light pollution, EM fields. Oversized moths drawn to specific frequencies. Dragonflies adapted to canal chemistry. A species of large fly that breeds in the thermal waste heat of server infrastructure.' },

  { label: 'Deep Canal Fauna — Permanent Darkness Species', category: 'water', origin: 'adaptation',
    prompt: 'Generate 8 species that live in the permanent darkness of GLMZ deep drainage and canal infrastructure. Some are depigmented. Some are blind. Some have developed bioluminescence. The deepest drainage tunnels have not been accessed by humans in decades — what has evolved there?' },

  { label: 'Lab Chimera — Experimental Hybrids', category: 'chimera', origin: 'genemod_accident',
    prompt: 'Generate 8 experimental chimera types that were accidents or failed projects — combinations that were not intended, traits that expressed unexpectedly, individuals with unstable genomes still shifting years later. Focus on the science fiction horror of bodies that are still changing, and the community structures that have formed to support them.' },

  { label: 'Urban Deer — Impossible Populations', category: 'urban_fauna', origin: 'natural_mutation',
    prompt: 'Generate 8 deer population anomalies throughout GLMZ — small herds that somehow exist in urban zones with no apparent habitat. These are not the mystical silent deer, but mundane deer populations that simply should not be there. Include the practical problems they create and the pragmatic ways communities have adapted to their presence.' },

  { label: 'River Mouth Fauna — Lake Transition Zone', category: 'water', origin: 'adaptation',
    prompt: 'Generate 8 species that specialize in the transition zone between the Chicago River system and Lake Michigan — a zone of mixed fresh and brackish water, industrial runoff, and constant human activity. Species adapted to salinity shifts, current changes, heavy boat traffic, and the specific chemistry of this transition zone.' },

  { label: 'Structural Fauna — Building-Integrated Species', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 species that have integrated so deeply into the physical structure of GLMZ buildings that they are functionally part of the architecture. Scaffold spiders as baseline but more: termites whose colonies reinforce concrete, bats roosting in load-bearing walls, birds whose nests have become structural elements in old Tier 1 buildings.' },

  { label: 'Roof and Sky Garden Fauna', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 species that have colonized the rooftop gardens, solar arrays, and sky-level ecology of GLMZ. Many Tier 3 and 4 buildings have extensive rooftop ecosystems. What has filled this unusual niche? Include both expected and unexpected inhabitants.' },

  { label: 'Scavenger Specialists', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 scavenger species highly specialized for specific types of GLMZ waste streams — pharmaceutical waste, electronic waste, food processing waste, medical waste, construction debris. Each scavenger has evolved specific traits for their particular waste type.' },

  { label: 'Additional Chimera — Ambiguous Types', category: 'chimera', origin: 'natural_mutation',
    prompt: 'Generate 8 chimera individuals and communities of ambiguous or mixed type — part feline part avian, rodent-aquatic hybrids, insectoid-canine combinations. These mixed chimeras occupy an even more complex legal and social space. How do they find community? What do they call themselves?' },

  { label: 'Seasonal and Migratory Visitors', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 migratory or seasonal species that visit GLMZ briefly — following the lakefront flyways, moving through the city corridor, wintering in specific district microclimates. Include both expected migratory species that have shifted their patterns and unexpected ones that now treat GLMZ as a migration waypoint.' },

  { label: 'Basement and Sub-Basement Fauna', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 species adapted to the near-lightless environment of GLMZ sub-basements, parking structures, and sub-grade infrastructure. Pale, quiet, adapted to sound navigation. Include at least one species that is an apex predator in this specific niche and one that humans have learned to co-exist with peacefully.' },

  { label: 'Corporate Campus Fauna', category: 'urban_fauna', origin: 'adaptation',
    prompt: 'Generate 8 species that have adapted to life inside or adjacent to corporate campuses — the manicured but strange ecology of Tier 4 and 5 facilities. Species that exploit the extreme cleanliness, the ornamental planting, the security perimeters, the waste streams of corporate cafeterias and medical facilities.' },
];

// ── Run a batch with retry ──
async function runBatch(batch, batchIndex, totalBatches, existingNames, dryRun) {
  const existingList = existingNames.slice(-60).join(', ') || 'none yet';

  const userPrompt =
    `Batch context: ${batch.label}\n` +
    `Category hint: ${batch.category}\n` +
    `Origin hint: ${batch.origin}\n` +
    `Task: ${batch.prompt}\n\n` +
    `Existing creature names (DO NOT duplicate any): ${existingList}\n\n` +
    `Return ONLY a valid JSON array of exactly 8 creature objects. No markdown fences. No explanation.`;

  if (dryRun) {
    console.log(`[DRY RUN] Batch ${batchIndex + 1}/${totalBatches}: ${batch.label}`);
    return { written: 0, skipped: 0, names: [] };
  }

  let attempt = 0;
  while (attempt < 2) {
    try {
      const raw = await callClaude(SYSTEM_PROMPT, userPrompt, 16384);
      const creatures = parseJsonArray(raw);

      let written = 0;
      let skipped = 0;
      const newNames = [];

      for (const creature of creatures) {
        if (!creature.name) { skipped++; continue; }

        // Always assign a fresh id
        creature.id = crypto.randomBytes(16).toString('hex');
        // Enforce required fields
        if (!creature.type) creature.type = 'creature';
        if (!creature.category) creature.category = batch.category;
        if (!creature.origin) creature.origin = batch.origin;
        if (!Array.isArray(creature.aliases)) creature.aliases = [];
        if (!Array.isArray(creature.habitat)) creature.habitat = [];
        if (!Array.isArray(creature.notable_traits)) creature.notable_traits = [];
        if (!Array.isArray(creature.story_hooks)) creature.story_hooks = [];
        if (!Array.isArray(creature.related_entities)) creature.related_entities = [];
        if (!Array.isArray(creature.tags)) creature.tags = ['creature'];

        if (existingNames.includes(creature.name) || fileExists(creature.name)) {
          skipped++;
          continue;
        }

        writeCreatureFile(creature);
        existingNames.push(creature.name);
        newNames.push(creature.name);
        written++;
      }

      return { written, skipped, names: newNames };

    } catch (err) {
      if (attempt === 0 && (err.status === 429 || (err.message && err.message.includes('rate')))) {
        console.log(`  Rate limit hit. Waiting 30s before retry...`);
        await sleep(30000);
        attempt++;
      } else {
        console.error(`  Batch failed: ${err.message}`);
        return { written: 0, skipped: 0, names: [] };
      }
    }
  }

  console.error(`  Batch skipped after 2 failures.`);
  return { written: 0, skipped: 0, names: [] };
}

// ── Main ──
async function main() {
  // Ensure output directory exists
  if (!fs.existsSync(OUTPUT_DIR)) {
    fs.mkdirSync(OUTPUT_DIR, { recursive: true });
    console.log(`Created output directory: ${OUTPUT_DIR}`);
  }

  // Load existing names (resume-safe)
  const existingNames = readExistingNames();
  console.log(`Found ${existingNames.length} existing creatures. Resuming...`);

  // Filter batches by category if requested
  let batches = BATCHES;
  if (CATEGORY_FILTER) {
    batches = BATCHES.filter(b => b.category === CATEGORY_FILTER);
    if (batches.length === 0) {
      console.error(`No batches match category: ${CATEGORY_FILTER}`);
      console.error(`Valid categories: ${[...new Set(BATCHES.map(b => b.category))].join(', ')}`);
      process.exit(1);
    }
    console.log(`Filtered to ${batches.length} batches for category: ${CATEGORY_FILTER}`);
  }

  // Apply limit
  if (LIMIT !== null) {
    batches = batches.slice(0, LIMIT);
    console.log(`Limiting to ${batches.length} batch(es).`);
  }

  const WAIT_MS = 3000; // 3 seconds between batches (8192 token responses, ~1 per 3s safe margin)
  let totalWritten = 0;
  let totalSkipped = 0;

  for (let i = 0; i < batches.length; i++) {
    const batch = batches[i];
    const { written, skipped, names } = await runBatch(batch, i, batches.length, existingNames, DRY_RUN);

    totalWritten += written;
    totalSkipped += skipped;

    console.log(`Batch ${i + 1}/${batches.length} | ${batch.label} | Wrote ${written} | Skipped ${skipped} (exist)`);

    if (!DRY_RUN && i < batches.length - 1) {
      await sleep(WAIT_MS);
    }
  }

  console.log('\n=== DONE ===');
  console.log(`Total written: ${totalWritten}`);
  console.log(`Total skipped (already existed): ${totalSkipped}`);
  console.log(`Total creatures on disk: ${readExistingNames().length}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
