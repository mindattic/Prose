// Civilian population generator for StreetSamurai
// Generates 1024 non-runner character JSON files in engine_data/characters/
// Run: node generate_civilians.js
// Resumes from where it left off — skips existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine_data');
const CHAR_DIR = path.join(ENGINE_DATA, 'people');
const BATCH_SIZE = 5; // characters per API call
const PARALLEL = 3; // concurrent API calls
const WAIT_MS = 500;
const MAX_RETRIES = 3;

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

function callClaude(system, user, maxTokens = 16000) {
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
          if (j.error) reject(new Error(j.error.message || JSON.stringify(j.error)));
          else if (j.content && j.content[0]) resolve(j.content[0].text);
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
  const start = json.indexOf('[');
  const end = json.lastIndexOf(']');
  if (start !== -1 && end !== -1) {
    json = json.substring(start, end + 1);
  }
  return JSON.parse(json);
}

function toFilename(name) {
  return name.toLowerCase()
    .replace(/['']/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_|_$/g, '') + '.json';
}

// Load existing character names/filenames
function getExistingNames() {
  const files = fs.readdirSync(CHAR_DIR).filter(f => f.endsWith('.json'));
  const names = new Set();
  const filenames = new Set();
  for (const f of files) {
    filenames.add(f);
    try {
      const d = JSON.parse(fs.readFileSync(path.join(CHAR_DIR, f), 'utf8'));
      if (d.name) names.add(d.name.toLowerCase());
    } catch (e) {}
  }
  return { names, filenames };
}

// Load place names
function getPlaceNames() {
  const dir = path.join(ENGINE_DATA, 'places');
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.json'));
  return files.map(f => {
    try { return JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')).name; }
    catch (e) { return null; }
  }).filter(Boolean);
}

// ── Category definitions ──
const CATEGORIES = [
  {
    name: 'corporate_workers',
    count: 200,
    roles: 'middle managers, engineers, scientists, HR specialists, marketing analysts, security analysts, corporate lawyers, accountants, lab techs, executives, interns, data architects, compliance officers, logistics coordinators, project managers, biotech researchers, supply chain managers, procurement specialists, corporate trainers, quality assurance',
    ageRange: [22, 62],
    tierRange: [2, 5],
    affiliations: 'Axiom, Tessera, Sterling-Nakamura, Zheng-Dao, Arcturus, Ringo, Palladian, Helix Biosystems, Ferrogate Transit, or smaller subsidiary corponations',
    locationHint: 'corporate districts, The Circuit, Meridian Core, Vantage Meridian Corporate Campus, Lincoln Fortress, Grand Corridor, Kenwood Gate, The Spire',
  },
  {
    name: 'service_workers',
    count: 150,
    roles: 'bartenders, cooks, noodle cart vendors, transit operators, building cleaners, delivery drivers, mechanics, shop owners, barbers, sex workers, teachers, nurses, childcare workers, laundry operators, waste handlers, parking attendants, building supers, tailor/seamstresses, pet groomers, gym trainers',
    ageRange: [18, 65],
    tierRange: [1, 3],
    affiliations: 'Independent, Ferrogate Transit (transit workers), local businesses, freelance',
    locationHint: 'The Shelf, The Narrows, Old Harbor, Geartown, The Burnished Market, Hamtramck Enclave, The Ferment Quarter, Mexicantown Libre, The Orchard',
  },
  {
    name: 'street_vendors_merchants',
    count: 100,
    roles: 'food stall operators, black market dealers, aug shop owners, gene clinic operators, pawn shop owners, small-time data brokers, clothing/fashion vendors, weapons dealers, drug dealers, junk merchants, scrap resellers, bootleg media sellers, fake ID vendors, street pharmacists, fortune sellers',
    ageRange: [20, 70],
    tierRange: [1, 3],
    affiliations: 'Independent, loosely affiliated with local gangs or factions, The Oxidian Market vendors guild',
    locationHint: 'The Oxidian Market, The Burnished Market, The Shelf, The Alban Souk, The Narrows, Geartown, Old Harbor, The Stockyard, The Laceworks',
  },
  {
    name: 'artists_entertainers',
    count: 80,
    roles: 'musicians, painters, holographic artists, street performers, writers, DJs, tattoo artists, gene-mod body artists, actors, comedians, poets, VR experience designers, graffiti artists, sound sculptors, underground filmmakers, puppet makers, neon artists, choreographers',
    ageRange: [16, 75],
    tierRange: [1, 4],
    affiliations: 'Independent, art collectives, underground venues, corponation-sponsored (a few)',
    locationHint: 'The Ferment Quarter, The Loft, Pilsen Veil, The Canopy, Hamtramck Enclave, Mexicantown Libre, Highland Park Autonomous Zone, The Garret',
  },
  {
    name: 'medical',
    count: 60,
    roles: 'street docs, clinic nurses, trauma surgeons, gene therapists, augmentation technicians, psychiatrists, pharmacists, midwives, veterinarians for gene-modded pets, rehabilitation specialists, neural calibrators, bio-waste disposal techs, blood bank operators, prosthetics fitters',
    ageRange: [25, 68],
    tierRange: [1, 4],
    affiliations: 'Independent clinics, Helix Biosystems, Aurochs Medical Complex, street-level freelance, community health cooperatives',
    locationHint: 'Aurochs Medical Complex, The Shelf, The Narrows, Geartown, The Circuit, various neighborhood clinics',
  },
  {
    name: 'law_enforcement_security',
    count: 60,
    roles: 'beat cops, corporate security guards, private investigators, bounty compliance officers, transit police, surveillance operators, forensic techs, riot suppression specialists, checkpoint operators, internal affairs investigators, drone operators, evidence handlers',
    ageRange: [22, 58],
    tierRange: [2, 4],
    affiliations: 'GLMZ Metro Police, Axiom Security Division, Tessera Compliance Bureau, Ferrogate Transit Security, private security firms, freelance PIs',
    locationHint: 'The Circuit, The Shelf, transit hubs, corporate campuses, checkpoint zones, Fort Anchor, The Rampart',
  },
  {
    name: 'religious_spiritual',
    count: 40,
    roles: 'chrome prayer practitioners, E.L.F. shrine keepers, traditional religious leaders (imams, priests, monks, rabbis), synthetic rights advocates who treat it as spiritual calling, death midwives, dream interpreters, circuit monks, data diviners, pilgrimage guides, meditation facilitators',
    ageRange: [20, 85],
    tierRange: [1, 3],
    affiliations: 'Chrome Prayer temples, E.L.F. shrines, traditional religious institutions, independent spiritual practitioners',
    locationHint: 'The Shelf, Nordpark Sanctuary, The Canopy, Hamtramck Enclave, Highland Park Autonomous Zone, various shrines and temples',
  },
  {
    name: 'children_youth',
    count: 50,
    roles: 'street kids, corporate school students, apprentices, orphans, gang prospects, prodigies, child laborers, truant network runners (not shadowrunners — kids who run messages), junior hackers, scholarship students',
    ageRange: [6, 17],
    tierRange: [1, 4],
    affiliations: 'Various — street gangs (junior), corporate schools, orphanages, apprentice programs, independent',
    locationHint: 'The Shelf, The Narrows, corporate school campuses, The Stockyard, streets and alleys',
  },
  {
    name: 'elderly_retired',
    count: 50,
    roles: 'war veterans (Corporate Wars), retired corporate employees, community elders, archive keepers, storytellers, pension-dependent residents, retired teachers, former union leaders, old soldiers, grandparents raising grandchildren',
    ageRange: [60, 95],
    tierRange: [1, 4],
    affiliations: 'Veterans associations, retirement communities, community councils, pension systems, independent',
    locationHint: 'The Shelf, Washburn Commons, Norwood Quiet, Montclare Quiet, Avalon Quiet, community centers',
  },
  {
    name: 'academics_researchers',
    count: 40,
    roles: 'professors, AI researchers, historians, linguists, sociologists studying the Shelf, climate scientists, data archaeologists, xenolinguists, ethics researchers, urban planners, gene-sequence archivists, computational theorists',
    ageRange: [28, 72],
    tierRange: [2, 4],
    affiliations: 'University Spine institutions, corponation R&D divisions, independent researchers, think tanks',
    locationHint: 'University Spine, The Circuit, Meridian Core, research labs, The Conservatory',
  },
  {
    name: 'political_activists',
    count: 40,
    roles: 'union organizers, synthetic rights lawyers, anti-corporate agitators, community leaders, journalists, whistleblowers, neighborhood council members, protest coordinators, underground press operators, policy advocates, mutual aid organizers',
    ageRange: [19, 65],
    tierRange: [1, 3],
    affiliations: 'Labor unions, synthetic rights organizations, community councils, independent media, underground networks',
    locationHint: 'Highland Park Autonomous Zone, The Shelf, North Branch Commons, Hamtramck Enclave, Mexicantown Libre, various community halls',
  },
  {
    name: 'criminal_non_runner',
    count: 80,
    roles: 'gang members, loan sharks, compute brokers, fence operators, forgers, identity fabricators, organ harvesters, drug manufacturers, protection racket enforcers, bookmakers, smugglers, counterfeiters, chop shop operators, blackmail specialists, numbers runners',
    ageRange: [16, 60],
    tierRange: [1, 3],
    affiliations: 'Local gangs, crime families, independent operators, loose criminal networks',
    locationHint: 'The Narrows, The Shelf lower levels, Shallowgrave, The Stockyard, Gravesend Basin, The Undertow, South Deering Sump',
  },
  {
    name: 'underworld',
    count: 24,
    roles: 'deep-dwellers, Bore Rats (tunnel people), tunnel guides, underworld merchants, the disappeared (people who erased themselves from surface records), subterranean scavengers, pipe-folk, deep shrine keepers, fungus farmers, echo hunters',
    ageRange: [14, 70],
    tierRange: [1, 1],
    affiliations: 'Bore Rat communities, tunnel clans, independent, The Undertow, subterranean settlements',
    locationHint: 'Irkalla, The Undertow, Abyssal Threshold, Deepwell Station, sub-level tunnel networks, abandoned infrastructure',
  },
  {
    name: 'drifters_transients',
    count: 50,
    roles: 'hyperlane migrants, airship drifters, nomads, refugees, Q-zero ghosts (people with zero digital identity), wandering traders, seasonal workers, climate refugees, expelled citizens, stateless persons, caravan members',
    ageRange: [12, 75],
    tierRange: [1, 2],
    affiliations: 'None, nomad caravans, refugee collectives, stateless',
    locationHint: 'Transit hubs, Kenosha Crossing, Escanaba Gateway, The Waukegan Industrial Shelf, edges of GLMZ, tent cities, hyperlane rest stops',
  },
];

const WORLD_SYSTEM = `You are a world-builder for GLMZ, a megacity on Lake Michigan in the year 2200. Currency is the Quanta (symbol Φ). UBC stipend is Φ120/month. Tiers: 1=street poor, 2=working class, 3=corporate middle, 4=executive, 5=elite.

CRITICAL — DIASPORA RULE: Everyone is mixed heritage from UNEXPECTED global combinations. Names reflect 3-4 generations of mixing. Examples: Kofi Lindqvist-Okafor, Fatou Chen-Adeyemi, Tariq Mwangi-Leblanc, Yuki Osei-Petrov. Draw from West Africa, Central Asia, Caucasus, Polynesia, Andes, Sahel, Southeast Asia, Horn of Africa, Caribbean, Pacific Islands, Maghreb, Central Africa, Melanesia, Arctic, Balkans, Amazonia, Siberia, Micronesia. Do NOT default to USA/Japan/UK/Korea/generic Western names. Every character should have a name that tells a story of migration and mixing.

GENEWARE: Cat ears, tails, color-changing hair, bioluminescent skin, patterned irises, retractable claws, enhanced scent glands — these are common cosmetic mods. Not everyone has them but they are unremarkable.

E.L.F.s (Electronic Life Forms): Digital spirits that inhabit devices. People pray to them, leave data offerings, seek their favor.

Corponations (sovereign corporate entities): Axiom, Tessera, Sterling-Nakamura, Zheng-Dao, Arcturus, Ringo, Palladian, Helix Biosystems, Ferrogate Transit.

These characters are NOT runners/freelancers/shadowrunners. They are the civilian population — ordinary people living in a near-future world.

Return a JSON array. Each character object must have EXACTLY these fields:
{
  "type": "character",
  "name": "Full Name",
  "aliases": ["nickname1"],
  "species": "human",
  "gender": "male|female|nonbinary",
  "pronouns": "he/him|she/her|they/them",
  "role": "brief role description",
  "age": 34,
  "status": "active",
  "location": "specific location in GLMZ",
  "description": "1-2 vivid paragraphs. Physical details, personality snapshot, what makes them memorable.",
  "affiliation": "who they work for or are connected to",
  "augmentations": "what chrome/geneware they have, if any. Many civilians have minimal or cosmetic only.",
  "daily_life": "1-2 sentences about their routine",
  "narrative_function": "what role they serve in stories — local color, quest giver, information source, etc.",
  "psychology": {
    "facet_weights": { "wound": 0.0-1.0, "ideal": 0.0-1.0, "id": 0.0-1.0, "shadow": 0.0-1.0, "mask": 0.0-1.0, "ghost": 0.0-1.0 },
    "core_fears": ["fear1", "fear2"],
    "core_desires": ["desire1", "desire2"],
    "coping_mechanisms": ["mechanism1"],
    "blind_spots": ["blindspot1"],
    "secret": "Something hidden about this person"
  },
  "speech_patterns": {
    "vocabulary": "brief description of how they talk",
    "cadence": "speech rhythm",
    "verbal_tics": ["tic1"],
    "example_lines": ["line1", "line2"]
  },
  "relationships": [],
  "story_hooks": ["hook1", "hook2"],
  "behavioral": {
    "decision_rules": ["rule1", "rule2"],
    "escalation_ladder": ["step1", "step2", "step3"],
    "interpersonal_modes": { "strangers": "how they treat strangers", "friends": "how they treat friends" },
    "stress_responses": { "low": "response", "medium": "response", "high": "response" },
    "contradictions": ["contradiction1"],
    "habits": ["habit1", "habit2"],
    "breaking_points": ["breaking_point1"]
  },
  "stats": {
    "physical": { "strength": 1-10, "dexterity": 1-10, "vitality": 1-10, "perception": 1-10 },
    "mental": { "cognition": 1-10, "willpower": 1-10, "creativity": 1-10, "spatial": 1-10 },
    "social": { "presence": 1-10, "empathy": 1-10, "expression": 1-10, "integrity": 1-10 },
    "personality": {
      "openness_conviction": -5 to 5,
      "empathy_detachment": -5 to 5,
      "impulsivity_deliberation": -5 to 5,
      "assertion_deference": -5 to 5,
      "transparency_guardedness": -5 to 5
    },
    "tags": ["tag1", "tag2"]
  },
  "cyberware_inventory": [],
  "timeline": [],
  "changelog": []
}

IMPORTANT: Stats for civilians should generally be moderate (3-7 range) unless their role justifies extremes. Vary the facet_weights — don't make them all the same pattern. Make secrets interesting and specific. Give each person a life that feels real.`;

// Generate a single batch (used in parallel)
async function generateBatch(cat, count, allNames, places, batchLabel) {
  const existingList = Array.from(allNames).slice(-200).join(', '); // last 200 to avoid prompt bloat

  const userPrompt = `Generate ${count} ${cat.name.replace(/_/g, ' ')} characters for GLMZ.

CATEGORY: ${cat.name.replace(/_/g, ' ')}
ROLES to draw from: ${cat.roles}
AGE RANGE: ${cat.ageRange[0]}-${cat.ageRange[1]}
TIER RANGE: ${cat.tierRange[0]}-${cat.tierRange[1]}
TYPICAL AFFILIATIONS: ${cat.affiliations}
TYPICAL LOCATIONS: ${cat.locationHint}

Available locations in GLMZ (use these or invent specific spots within them): ${places.slice(0, 80).join(', ')}

DO NOT duplicate these existing names: ${existingList}

Generate exactly ${count} characters. Each must be distinct — different roles within the category, different ages, different genders, different backgrounds. Remember the DIASPORA rule — mixed heritage names from unexpected global combinations.

Return ONLY a valid JSON array of ${count} character objects. No markdown, no explanation.`;

  for (let retry = 0; retry < MAX_RETRIES; retry++) {
    try {
      const result = await callClaude(WORLD_SYSTEM, userPrompt, 16000);
      const chars = parseJsonArray(result);
      if (!Array.isArray(chars) || chars.length === 0) throw new Error('Empty array');
      return chars;
    } catch (e) {
      console.error(`    ${batchLabel} attempt ${retry + 1}/${MAX_RETRIES} failed: ${e.message.substring(0, 200)}`);
      if (retry < MAX_RETRIES - 1) await sleep(3000);
    }
  }
  return null;
}

// ── Main generation loop ──
async function main() {
  if (!fs.existsSync(CHAR_DIR)) fs.mkdirSync(CHAR_DIR, { recursive: true });

  const { names: existingNames, filenames: existingFiles } = getExistingNames();
  const places = getPlaceNames();
  const allGeneratedNames = new Set(existingNames);

  // Track progress per category
  const progressFile = path.join(__dirname, '.civilian_progress.json');
  let progress = {};
  if (fs.existsSync(progressFile)) {
    try { progress = JSON.parse(fs.readFileSync(progressFile, 'utf8')); } catch (e) { progress = {}; }
  }

  let totalGenerated = 0;
  let totalSkipped = 0;

  for (const cat of CATEGORIES) {
    const done = progress[cat.name] || 0;
    const remaining = cat.count - done;
    if (remaining <= 0) {
      console.log(`[${cat.name}] Already complete (${cat.count}/${cat.count})`);
      totalSkipped += cat.count;
      continue;
    }

    console.log(`\n=== ${cat.name.toUpperCase()} — generating ${remaining} remaining (${done} already done) ===`);

    // Build all batch requests for this category
    const batches = [];
    for (let i = 0; i < remaining; i += BATCH_SIZE) {
      batches.push(Math.min(BATCH_SIZE, remaining - i));
    }

    // Process batches in parallel groups
    for (let g = 0; g < batches.length; g += PARALLEL) {
      const group = batches.slice(g, g + PARALLEL);
      const groupIdx = g;
      console.log(`  Parallel group ${Math.floor(g/PARALLEL)+1}/${Math.ceil(batches.length/PARALLEL)} — ${group.length} concurrent API calls (${group.reduce((a,b)=>a+b,0)} chars)...`);

      const promises = group.map((count, idx) => {
        const batchNum = groupIdx + idx + 1;
        return generateBatch(cat, count, allGeneratedNames, places, `[${cat.name} batch ${batchNum}/${batches.length}]`);
      });

      const results = await Promise.all(promises);

      let groupSaved = 0;
      for (const chars of results) {
        if (!chars) continue;
        for (const char of chars) {
          if (!char.name) continue;
          char.type = 'character';
          char.changelog = char.changelog || [];
          char.timeline = char.timeline || [];
          char.cyberware_inventory = char.cyberware_inventory || [];
          char.relationships = char.relationships || [];

          const filename = toFilename(char.name);
          const filepath = path.join(CHAR_DIR, filename);

          if (existingFiles.has(filename) || allGeneratedNames.has(char.name.toLowerCase())) {
            continue; // skip duplicates silently
          }
          if (fs.existsSync(filepath)) continue;

          try {
            fs.writeFileSync(filepath, JSON.stringify(char, null, 2));
            allGeneratedNames.add(char.name.toLowerCase());
            existingFiles.add(filename);
            groupSaved++;
            totalGenerated++;
          } catch (e) {
            console.error(`    Error writing ${filename}: ${e.message}`);
          }
        }
      }

      progress[cat.name] = (progress[cat.name] || 0) + groupSaved;
      fs.writeFileSync(progressFile, JSON.stringify(progress, null, 2));
      console.log(`    Saved ${groupSaved} (category: ${progress[cat.name]}/${cat.count}, total new: ${totalGenerated})`);

      if (g + PARALLEL < batches.length) await sleep(WAIT_MS);
    }
  }

  const finalCount = fs.readdirSync(CHAR_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n========================================`);
  console.log(`Generation complete.`);
  console.log(`  New characters generated this run: ${totalGenerated}`);
  console.log(`  Total character files: ${finalCount}`);
  console.log(`========================================`);
  console.log('\nCategory progress:');
  for (const cat of CATEGORIES) {
    const p = progress[cat.name] || 0;
    console.log(`  ${cat.name}: ${p}/${cat.count}${p >= cat.count ? ' DONE' : ''}`);
  }
}

main().catch(e => { console.error('Fatal error:', e); process.exit(1); });
