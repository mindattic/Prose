// World content generator for StreetSamurai
// Calls Claude API to batch-generate entities and appends to JSON files
// Run: node generate_world.js [corps|chars|districts|tech|docs] [count]

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const ENGINE_DATA = path.join(__dirname, '..', 'engine_data');

function callClaude(system, user, maxTokens = 4096) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.9,
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

// ── Corponations ──
async function generateCorps(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8'));
  const existingNames = existing.map(c => c.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} corps`); return; }

  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    console.log(`Generating corps ${existing.length + 1} to ${existing.length + count}...`);

    const system = `You generate corponation entries for a cyberpunk world called Meridian 88 (Great Lakes megacity corridor). Each corponation is a sovereign corporate entity. Return a JSON array of ${count} entries. Each entry must have these exact fields: number (int), name, full_legal_name, common_names (array), stock_designation, sector, valuation, revenue, employees, sovereign_territory, founding_story, security_force, key_detail, relationship_to_big_20, full_text. Make each unique — different sectors (biotech, defense, energy, media, logistics, agriculture, mining, AI, pharmaceutical, construction, telecom, etc). Be creative and specific to the setting.`;

    const user = `Existing corponation names (DO NOT duplicate): ${existingNames.join(', ')}. Generate ${count} NEW corponations numbered ${existing.length + 1} to ${existing.length + count}. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newCorps = parseJsonArray(result);
      existing.push(...newCorps);
      existingNames.push(...newCorps.map(c => c.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'corponations.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newCorps.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Characters ──
async function generateChars(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'characters.json'), 'utf8'));
  const existingNames = existing.map(c => c.name);
  const districts = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'districts.json'), 'utf8')).map(d => d.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} chars`); return; }

  const BATCH = 3;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    console.log(`Generating chars ${existing.length + 1} to ${existing.length + count}...`);

    const system = `You generate character entries for a cyberpunk world called Meridian 88. Return a JSON array of ${count} entries. Each must have: type ("character"), name, aliases (array), role, age (int), status ("active"/"deceased"/"missing"), location, description (2-3 paragraphs), psychology (object with facet_weights {wound,ideal,id,shadow,mask,ghost as 0-1 floats}, core_fears array, core_desires array, coping_mechanisms array, blind_spots array, secret string), speech_patterns (vocabulary, cadence, verbal_tics array, example_lines array), relationships (array of {name,type,description,emotional_core,story_tension}), story_hooks (array), narrative_function, augmentations, daily_life, affiliation. Mix tiers (street-level, mid-tier, corporate elite, excluded). Be diverse in background, ethnicity, age, role.`;

    const user = `Known districts: ${districts.join(', ')}. Existing characters (DO NOT duplicate): ${existingNames.join(', ')}. Generate ${count} NEW unique characters. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newChars = parseJsonArray(result);
      existing.push(...newChars);
      existingNames.push(...newChars.map(c => c.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'characters.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newChars.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Districts ──
async function generateDistricts(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'districts.json'), 'utf8'));
  const existingNames = existing.map(d => d.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} districts`); return; }

  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    console.log(`Generating districts ${existing.length + 1} to ${existing.length + count}...`);

    const system = `You generate district/location entries for Meridian 88, a cyberpunk megacity in the Great Lakes corridor. Return a JSON array of ${count} entries. Each must have: type ("place"), name, aliases (array), description (2-3 paragraphs), atmosphere (object with sights, sounds, smells as arrays, feel as string), demographics, economy, power_structure, dangers (array), opportunities (array), story_hooks (array), connections ({adjacent_to: array}), frequented_by (array), notable_locations (array of {name, description}). Mix scales: mega-districts, neighborhoods, specific buildings, underground areas, corporate zones, abandoned sectors.`;

    const user = `Existing districts (DO NOT duplicate): ${existingNames.join(', ')}. These are all within Meridian 88 (Chicago-Milwaukee corridor megacity). Generate ${count} NEW locations. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newDists = parseJsonArray(result);
      existing.push(...newDists);
      existingNames.push(...newDists.map(d => d.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'districts.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newDists.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Technology ──
async function generateTech(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'worldbuilding_docs.json'), 'utf8'));
  const techDocs = existing.filter(d => d.category === 'technology' || d.category === 'Technology');
  const existingTitles = existing.map(d => d.title || d.file_name);
  const needed = target - techDocs.length;
  if (needed <= 0) { console.log(`Already have ${techDocs.length} tech docs`); return; }

  const BATCH = 10;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    console.log(`Generating tech ${techDocs.length + i + 1} to ${techDocs.length + i + count}...`);

    const subcategories = ['neural_interfaces', 'augmentation', 'weapons', 'surveillance', 'medical', 'transportation', 'communications', 'energy', 'manufacturing', 'AI_systems', 'cybersecurity', 'biotech', 'materials_science', 'environmental_tech', 'corporate_security'];
    const sub = subcategories[Math.floor(Math.random() * subcategories.length)];

    const system = `You generate technology reference documents for a cyberpunk world called Meridian 88. Return a JSON array of ${count} entries. Each must have: file_name (snake_case, unique), title, category ("Technology"), body (3-5 paragraphs of detailed worldbuilding text about this technology — how it works, who uses it, social implications, tier availability), line_count (int, approximate), headings (array of section headings within the body). Focus on ${sub} technology. Be specific, grounded, and consistent with near-future extrapolation of real technology.`;

    const user = `Existing titles (DO NOT duplicate): ${existingTitles.slice(-30).join(', ')}. Generate ${count} NEW technology documents. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newDocs = parseJsonArray(result);
      existing.push(...newDocs);
      existingTitles.push(...newDocs.map(d => d.title || d.file_name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'worldbuilding_docs.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newDocs.length}, total tech: ${existing.filter(d => d.category === 'Technology').length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Documents (general worldbuilding) ──
async function generateDocs(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'worldbuilding_docs.json'), 'utf8'));
  const existingTitles = existing.map(d => d.title || d.file_name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} docs`); return; }

  const categories = [
    'History', 'Culture', 'Economics', 'Law', 'Medicine', 'Violence',
    'Social_Control', 'Religion', 'Media', 'Underground', 'Politics',
    'Climate', 'Migration', 'Labor', 'Crime', 'Education', 'Food',
    'Housing', 'Transportation', 'Sports', 'Art', 'Music', 'Fashion',
    'Drugs', 'Augmentation_Culture', 'Corporate_Life', 'Excluded_Life',
    'Military', 'Espionage', 'Psychology', 'Philosophy', 'Language'
  ];

  const BATCH = 10;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const cat = categories[Math.floor(Math.random() * categories.length)];
    console.log(`Generating docs ${existing.length + 1} to ${existing.length + count} (${cat})...`);

    const system = `You generate worldbuilding documents for a cyberpunk world called Meridian 88 (Great Lakes megacity, corporate sovereignty, neural interfaces ubiquitous, tiered citizenship). Return a JSON array of ${count} entries. Each must have: file_name (snake_case, unique), title, category ("${cat}"), body (3-6 paragraphs of rich worldbuilding — specific details, named places, implications for daily life), line_count (int, approximate), headings (array). These are reference documents that flesh out the world. Be specific, grounded, and avoid generic cyberpunk cliches.`;

    const user = `Recent titles (DO NOT duplicate any): ${existingTitles.slice(-40).join(', ')}. Generate ${count} NEW ${cat} documents. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newDocs = parseJsonArray(result);
      existing.push(...newDocs);
      existingTitles.push(...newDocs.map(d => d.title || d.file_name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'worldbuilding_docs.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newDocs.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Weaponry ──
async function generateWeapons(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'weaponry.json'), 'utf8'));
  const existingNames = existing.map(w => w.name);
  const corps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8')).map(c => c.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} weapons`); return; }

  const categories = ['melee', 'firearm', 'energy', 'explosive', 'chemical', 'cyber', 'drone', 'exotic'];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const cat = categories[Math.floor(Math.random() * categories.length)];
    console.log(`Generating weapons ${existing.length + 1} to ${existing.length + count} (${cat})...`);

    const system = `You generate weapon entries for a cyberpunk world called Meridian 88. Return a JSON array of ${count} entries. Each must have: name, type ("weapon"), aliases (array), category ("${cat}"), description (2-3 paragraphs — how it works, what makes it distinctive), manufacturer (use real-sounding corp names or these: ${corps.slice(0,10).join(', ')}), tier_availability (e.g. "Tier 3+", "Black market", "Military only"), legality (e.g. "Restricted", "Prohibited", "Licensed"), base_technologies (array of foundational tech names like "Linear magnetic acceleration", "Piezoelectric disruption", "Neural feedback loops"), specifications (technical details), tactical_use (how operators use it), cultural_context (social meaning, who carries it and why), known_users (array of character names or archetypes), story_hooks (array). Every weapon should reference 1-3 base technologies. Be specific and grounded.`;

    const user = `Existing weapons (DO NOT duplicate): ${existingNames.slice(-30).join(', ')}. Generate ${count} NEW ${cat} weapons. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newWeapons = parseJsonArray(result);
      existing.push(...newWeapons);
      existingNames.push(...newWeapons.map(w => w.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'weaponry.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newWeapons.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

const WAIT_MS = 65000; // 65 seconds between calls to stay under 8k tokens/min

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

async function main() {
  const type = process.argv[2] || 'all';

  if (type === 'corps' || type === 'all') await generateCorps(50);
  if (type === 'chars' || type === 'all') await generateChars(50);
  if (type === 'districts' || type === 'all') await generateDistricts(50);
  if (type === 'tech' || type === 'all') await generateTech(200);
  if (type === 'docs' || type === 'all') await generateDocs(1024);
  if (type === 'weapons' || type === 'all') await generateWeapons(512);

  console.log('\n=== DONE ===');
  const corps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8'));
  const chars = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'characters.json'), 'utf8'));
  const dists = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'districts.json'), 'utf8'));
  const docs = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'worldbuilding_docs.json'), 'utf8'));
  const weaps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'weaponry.json'), 'utf8'));
  console.log(`Corps: ${corps.length}, Chars: ${chars.length}, Districts: ${dists.length}, Docs: ${docs.length}, Weapons: ${weaps.length}`);
}

main().catch(e => console.error(e));
