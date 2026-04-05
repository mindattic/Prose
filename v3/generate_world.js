// World content generator for StreetSamurai
// Calls Claude API to batch-generate entities and writes to JSON files
// Run: node generate_world.js [corps|chars|districts|tech|docs|weapons|factions|equipment|consumer_goods|pharma|substrates|melee] [count]

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
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'technology.json'), 'utf8'));
  const existingNames = existing.map(t => t.name);
  const corps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8')).map(c => c.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} tech entries`); return; }

  const subcategories = ['neural_interfaces', 'augmentation', 'weapons_tech', 'surveillance', 'medical', 'transportation', 'communications', 'energy', 'manufacturing', 'ai_systems', 'cybersecurity', 'biotech', 'materials_science', 'environmental', 'corporate_security'];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const sub = subcategories[Math.floor(Math.random() * subcategories.length)];
    console.log(`Generating tech ${existing.length + 1} to ${existing.length + count} (${sub})...`);

    const system = `You generate INDIVIDUAL, SPECIFIC technology entries for a cyberpunk world called Meridian 88 (Great Lakes megacity, corporate sovereignty, neural augmentation ubiquitous, tiered citizenship). Each entry is ONE specific technology product or system — a particular model, version, or implementation with a manufacturer and designation. NOT encyclopedic essays about technology categories. Think: "CortexLink v4.2 Neural Interface" or "Tessera LUX-3 Pulse Laser Optics" or "Helix NanoSuture Mk.II Wound Closure System" — real products that exist in this world. Return a JSON array of ${count} entries. Each must have: name (specific product/system name with version or designation), type ("technology"), aliases (array of street names or abbreviations), subcategory ("${sub}"), description (2-3 paragraphs — how THIS specific technology works, what makes it distinctive, who uses it), tier_availability (e.g. "Tier 1+", "Tier 3+", "Military only", "Universal"), developers (array of corponation names — use these: ${corps.slice(0,15).join(', ')}), base_technologies (array of foundational technologies this builds on — these are edges to other tech nodes), enables (array of technologies or capabilities this makes possible — forward edges), social_impact (1 paragraph on how this specific tech affects daily life or power structures), story_hooks (array). Focus on ${sub}.`;

    const user = `Existing tech (DO NOT duplicate): ${existingNames.slice(-30).join(', ')}. Generate ${count} NEW ${sub} technologies. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newTech = parseJsonArray(result);
      existing.push(...newTech);
      existingNames.push(...newTech.map(t => t.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'technology.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newTech.length}, total: ${existing.length}`);
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

    const system = `You generate INDIVIDUAL, SPECIFIC weapon entries for a cyberpunk world called Meridian 88. Each entry is ONE specific weapon — a particular model with a manufacturer, model number, and street name. NOT essays or overviews about weapon categories. Think: "Arcturus Mk.7 Whisper Coilgun" or "Thermite Dispersal Grenade TDG-9 'Dragonspittle'" — real items an operator would buy, carry, and use. Return a JSON array of ${count} entries. Each must have: name (specific model name with designation), type ("weapon"), aliases (array of street names/nicknames), category ("${cat}"), description (2-3 paragraphs — how THIS specific weapon works, what makes it distinctive from competitors), manufacturer (use these corponations: ${corps.slice(0,10).join(', ')}), tier_availability (e.g. "Tier 3+", "Black market", "Military only"), legality (e.g. "Restricted", "Prohibited", "Licensed"), base_technologies (array of foundational tech names like "Linear magnetic acceleration", "Piezoelectric disruption", "Neural feedback loops"), specifications (technical specs — caliber, range, fire rate, weight, power source, etc.), tactical_use (how operators use it in the field), cultural_context (social meaning, who carries it and why, street reputation), known_users (array of character names or archetypes), story_hooks (array). Every weapon should reference 1-3 base technologies.`;

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

// ── Factions ──
async function generateFactions(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'factions.json'), 'utf8'));
  const existingNames = existing.map(f => f.name);
  const corps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8')).map(c => c.name);
  const districts = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'districts.json'), 'utf8')).map(d => d.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} factions`); return; }

  const BATCH = 2;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    console.log(`Generating factions ${existing.length + 1} to ${existing.length + count}...`);

    const factionTypes = ['street gang', 'resistance movement', 'religious cult', 'hacker collective', 'mercenary company', 'mutual aid network', 'smuggling ring', 'political movement', 'labor union', 'augmentation club', 'Blank community', 'rogue AI sympathizer cell', 'deep-Undertow tribe', 'corporate splinter group'];
    const fType = factionTypes[Math.floor(Math.random() * factionTypes.length)];

    const system = `You generate faction entries for a cyberpunk world called Meridian 88 (Great Lakes megacity corridor, corporate sovereignty, neural augmentation is ubiquitous, tiered citizenship). Return a JSON array of ${count} entries. Each must have: type ("faction"), name, aliases (array), motto, description (2-3 paragraphs — origins, current state, what makes them distinctive), ideology (1 paragraph), territory (where they operate), leadership (key figures or structure), methods (array of strings — how they operate), resources (array of strings — what they have access to), relationships (array of {faction, stance, description}), story_hooks (array). Generate a ${fType} type faction. Reference known districts: ${districts.slice(0,10).join(', ')} and corponations: ${corps.slice(0,10).join(', ')}. Be specific and avoid generic cyberpunk cliches.`;

    const user = `Existing factions (DO NOT duplicate): ${existingNames.join(', ')}. Generate ${count} NEW factions. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newFactions = parseJsonArray(result);
      existing.push(...newFactions);
      existingNames.push(...newFactions.map(f => f.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'factions.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newFactions.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Equipment ──
async function generateEquipment(target) {
  const existing = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'equipment.json'), 'utf8'));
  const existingNames = existing.map(e => e.name);
  const corps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8')).map(c => c.name);
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} equipment`); return; }

  const categories = ['augmentation', 'implant', 'armor', 'comms', 'sensor', 'medical', 'stealth', 'utility', 'vehicle'];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const cat = categories[Math.floor(Math.random() * categories.length)];
    console.log(`Generating equipment ${existing.length + 1} to ${existing.length + count} (${cat})...`);

    const system = `You generate INDIVIDUAL, SPECIFIC equipment/gear entries for a cyberpunk world called Meridian 88. Each entry is ONE specific piece of equipment — a particular model with a manufacturer, model designation, and street name. NOT essays or overviews about equipment categories. Think: "Veil-9 Thermoptic Shroud" or "Murmur-3 Acoustic Suppression Field" — real items an operator would buy, carry, and use. Return a JSON array of ${count} entries. Each must have: name (specific model name with designation), type ("equipment"), aliases (array of street names/nicknames), category ("${cat}"), description (2-3 paragraphs — how THIS specific item works, what makes it distinctive from competitors), manufacturer (use these corponations: ${corps.slice(0,10).join(', ')}), tier_availability (e.g. "Tier 1+", "Tier 3+", "Black market", "Corporate only"), legality (e.g. "Unrestricted", "Restricted", "Licensed", "Prohibited"), base_technologies (array of foundational tech names), specifications (object with technical specs as key-value pairs — weight, range, duration, power source, form factor, failure mode, etc.), tactical_use (how it's used in the field), cultural_context (social meaning, status symbol, necessity, street reputation), known_users (array of character names or archetypes), story_hooks (array). Category "${cat}" covers: ${cat === 'augmentation' ? 'cybernetic enhancements, neural upgrades, sensory mods, reflex boosters, cognitive accelerators' : cat === 'implant' ? 'subdermal devices, cranial BCIs, biomonitors, internal storage, skeletal reinforcement' : cat === 'armor' ? 'body armor, ablative coatings, reactive plating, stealth suits, exoskeletons' : cat === 'comms' ? 'encrypted comm devices, mesh network nodes, signal jammers, secure channels, dead drops' : cat === 'sensor' ? 'scanners, threat detectors, surveillance gear, counter-surveillance, environmental sensors' : cat === 'medical' ? 'field medkits, nanite injectors, trauma patches, stim packs, surgical tools' : cat === 'stealth' ? 'cloaking devices, signal maskers, identity spoofers, thermal dampeners, acoustic suppressors' : cat === 'utility' ? 'multitools, climbing gear, breaching tools, drones, hacking rigs, portable power cells' : 'personal vehicles, bikes, drones, exo-rigs, submersibles'}.`;

    const user = `Existing equipment (DO NOT duplicate): ${existingNames.slice(-30).join(', ')}. Generate ${count} NEW ${cat} equipment. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const newEquip = parseJsonArray(result);
      existing.push(...newEquip);
      existingNames.push(...newEquip.map(e => e.name));
      fs.writeFileSync(path.join(ENGINE_DATA, 'equipment.json'), JSON.stringify(existing, null, 2));
      console.log(`  Added ${newEquip.length}, total: ${existing.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Helpers for directory-based generators ──

function slugify(name) {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '');
}

function readDirEntities(dirPath) {
  if (!fs.existsSync(dirPath)) { fs.mkdirSync(dirPath, { recursive: true }); return []; }
  const files = fs.readdirSync(dirPath).filter(f => f.endsWith('.json'));
  return files.map(f => {
    try { return JSON.parse(fs.readFileSync(path.join(dirPath, f), 'utf8')); }
    catch { return null; }
  }).filter(Boolean);
}

function writeEntityFile(dirPath, entity, nameField = 'name') {
  const slug = slugify(entity[nameField] || 'unnamed');
  const filePath = path.join(dirPath, slug + '.json');
  fs.writeFileSync(filePath, JSON.stringify(entity, null, 2));
  return filePath;
}

function loadCorpNames() {
  const corpDir = path.join(ENGINE_DATA, 'corponations');
  return readDirEntities(corpDir).map(c => c.name).filter(Boolean);
}

// ── Consumer Goods (directory-based) ──
async function generateConsumerGoods(target) {
  const dir = path.join(ENGINE_DATA, 'consumer_goods');
  const existing = readDirEntities(dir);
  const existingNames = existing.map(e => e.name).filter(Boolean);
  const corps = loadCorpNames();
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} consumer goods`); return; }

  const categories = [
    'food_beverage', 'personal_care', 'household', 'entertainment', 'fashion_accessory',
    'pet_product', 'cleaning_supply', 'snack', 'beverage', 'candy', 'prepared_meal',
    'hygiene', 'cosmetic', 'stim_drink', 'comfort_item', 'novelty', 'subscription_box',
    'vending_machine_item', 'street_food', 'luxury_consumable'
  ];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const cat = categories[Math.floor(Math.random() * categories.length)];
    console.log(`Generating consumer goods ${existing.length + i + 1} to ${existing.length + i + count} (${cat})...`);

    const system = `You generate INDIVIDUAL consumer product entries for a cyberpunk world called Meridian 88 (Great Lakes megacity corridor, corporate sovereignty, neural augmentation ubiquitous, tiered citizenship 1-5 + Excluded, QUANTA currency symbol is \u03A6). Each entry is ONE specific consumer product — a brand-name item people buy and use in daily life. Think: "Koko-Volt Caffeinated Kelp Chips" or "NeuCalm Dermal Mist" — everyday products, not weapons or tech. Return a JSON array of ${count} entries. Each must have: name (brand name), type ("consumer_good"), category ("${cat}"), subcategory (more specific), manufacturer (use these corponations when appropriate: ${corps.slice(0, 15).join(', ')}, or invent smaller brands), description (1-2 paragraphs — what it is, how it's used, what makes it distinctive), flavor_profile (if food/beverage, otherwise omit), tier_availability (e.g. "Tier 2-4", "Universal", "Tier 1 only"), price (in \u03A6, e.g. "\u03A64.50"), popularity_rank (int 1-100), slogan (marketing tagline), cultural_context (1 paragraph — who buys it, social meaning, market niche), story_hooks (array of 2-3 narrative hooks), tags (array of keywords). Products should reflect the world: algae-based foods, mood-modulating snacks, neural-interface accessories, augmentation care products, corporate-branded everything, street vendor items, black market luxuries. Default to mixed heritage from unexpected global combinations for cultural flavor profiles.`;

    const user = `Existing product names (DO NOT duplicate): ${existingNames.slice(-50).join(', ')}. Generate ${count} NEW ${cat} consumer goods. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const items = parseJsonArray(result);
      for (const item of items) {
        writeEntityFile(dir, item);
        existingNames.push(item.name);
      }
      console.log(`  Added ${items.length}, total: ${existing.length + i + items.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Pharmaceuticals (directory-based) ──
async function generatePharma(target) {
  const dir = path.join(ENGINE_DATA, 'pharmaceuticals');
  const existing = readDirEntities(dir);
  const existingNames = existing.map(e => e.name).filter(Boolean);
  const corps = loadCorpNames();
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} pharmaceuticals`); return; }

  const categories = [
    'combat_stimulant', 'nootropic', 'painkiller', 'mood_stabilizer', 'neural_enhancer',
    'immunosuppressant', 'regenerative', 'recreational', 'anti_rejection', 'black_market_designer',
    'gene_therapy', 'cognitive_booster', 'sedative', 'anti_psychotic', 'performance_enhancer',
    'anti_radiation', 'detox_agent', 'synth_hormone', 'biocompatibility_agent', 'nanite_carrier'
  ];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const cat = categories[Math.floor(Math.random() * categories.length)];
    console.log(`Generating pharmaceuticals ${existing.length + i + 1} to ${existing.length + i + count} (${cat})...`);

    const system = `You generate INDIVIDUAL pharmaceutical/drug entries for a cyberpunk world called Meridian 88 (Great Lakes megacity corridor, corporate sovereignty, neural augmentation ubiquitous, tiered citizenship 1-5 + Excluded, QUANTA currency symbol is \u03A6). Each entry is ONE specific drug — a particular formulation with a brand name, street name, and manufacturer. Include both legal prescription drugs and black-market substances. Return a JSON array of ${count} entries. Each must have: name (brand/chemical name), type ("pharmaceutical"), aliases (array of street names), category ("${cat}"), subcategory (more specific), manufacturer (use these corponations: ${corps.slice(0, 15).join(', ')}, or underground labs), description (2-3 paragraphs — what it does, how it works, who uses it), method_of_use (e.g. "injection", "oral", "inhaled", "dermal patch", "neural port"), effects (array of specific effects), side_effects (array), duration (e.g. "4-6 hours"), addiction_risk ("none"/"low"/"moderate"/"high"/"extreme"), tier_availability (e.g. "Tier 2-4", "Black market only"), legality ("unrestricted"/"prescription"/"controlled"/"prohibited"/"unclassified"), price (in \u03A6), interactions (array of dangerous drug interactions), cultural_context (1 paragraph), story_hooks (array of 2-3 hooks), tags (array). Mix legal and illegal, therapeutic and recreational, common and exotic.`;

    const user = `Existing pharma names (DO NOT duplicate): ${existingNames.join(', ')}. Generate ${count} NEW ${cat} pharmaceuticals. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const items = parseJsonArray(result);
      for (const item of items) {
        writeEntityFile(dir, item);
        existingNames.push(item.name);
      }
      console.log(`  Added ${items.length}, total: ${existing.length + i + items.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Substrates (directory-based) ──
async function generateSubstrates(target) {
  const dir = path.join(ENGINE_DATA, 'substrates');
  const existing = readDirEntities(dir);
  const existingNames = existing.map(e => e.name).filter(Boolean);
  const corps = loadCorpNames();
  const needed = target - existing.length;
  if (needed <= 0) { console.log(`Already have ${existing.length} substrates`); return; }

  const categories = [
    'ceramic', 'polymer', 'metallic_alloy', 'composite', 'biological', 'synthetic_tissue',
    'hybrid_organic', 'nanomaterial', 'smart_material', 'crystal_lattice', 'gel_matrix',
    'carbon_variant', 'reactive', 'self_healing', 'phase_change', 'biomimetic',
    'electroactive', 'metamaterial', 'aerogel', 'liquid_armor'
  ];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const cat = categories[Math.floor(Math.random() * categories.length)];
    console.log(`Generating substrates ${existing.length + i + 1} to ${existing.length + i + count} (${cat})...`);

    const system = `You generate INDIVIDUAL substrate/material entries for a cyberpunk world called Meridian 88 (Great Lakes megacity corridor, corporate sovereignty, neural augmentation ubiquitous, tiered citizenship). Substrates are body types, materials, and forms — synthetic, biological, hybrid — used in augmentation, construction, armor, prosthetics, and other applications. Return a JSON array of ${count} entries. Each must have: name (material designation, e.g. "Ablonite-KR" or "NervWeave-7S"), type ("substrate"), aliases (array of street/trade names), category ("${cat}"), description (2-3 paragraphs — composition, how it's manufactured, unique properties, what makes it different from similar materials), properties (array of property strings like "reactive_ablation_on_impact", "self_healing", "biocompatible"), developers (array of corponation names — use: ${corps.slice(0, 15).join(', ')}), applications (array of specific use cases), tier_availability (e.g. "Tier 2-4", "Military only"), base_technologies (array of foundational technologies it relies on), specifications (object with measurable specs — tensile_strength, density, operating_temp_range, conductivity, etc.), cultural_context (who uses it and why), story_hooks (array of 2-3 hooks), tags (array). Mix exotic high-tech materials with more mundane but world-specific variants.`;

    const user = `Existing substrate names (DO NOT duplicate): ${existingNames.join(', ')}. Generate ${count} NEW ${cat} substrates. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const items = parseJsonArray(result);
      for (const item of items) {
        writeEntityFile(dir, item);
        existingNames.push(item.name);
      }
      console.log(`  Added ${items.length}, total: ${existing.length + i + items.length}`);
    } catch (e) {
      console.error(`  Error: ${e.message}`);
    }
    if (i + BATCH < needed) { console.log(`  Waiting ${WAIT_MS/1000}s for rate limit...`); await sleep(WAIT_MS); }
  }
}

// ── Melee Weapons (directory-based, writes to weaponry/) ──
async function generateMelee(target) {
  const dir = path.join(ENGINE_DATA, 'weaponry');
  const existing = readDirEntities(dir);
  const meleeExisting = existing.filter(w => w.category === 'melee' || w.category === 'melee_blunt' || w.category === 'melee_sharp');
  const existingNames = existing.map(w => w.name).filter(Boolean);
  const corps = loadCorpNames();
  const needed = target - meleeExisting.length;
  if (needed <= 0) { console.log(`Already have ${meleeExisting.length} melee weapons`); return; }

  const subcategories = [
    { cat: 'melee_blunt', desc: 'blunt melee weapons: clubs, maces, stun batons, impact gauntlets, hammers, telescoping batons, weighted knuckles, shock mauls, concussion rods, kinetic fists' },
    { cat: 'melee_sharp', desc: 'sharp/edged melee weapons: knives, swords, vibroblades, mono-edge weapons, machetes, combat axes, ceramic daggers, retractable claws, heated blades, molecular-edge razors, wakizashi, tantos' }
  ];
  const BATCH = 5;
  for (let i = 0; i < needed; i += BATCH) {
    const count = Math.min(BATCH, needed - i);
    const sub = subcategories[i % 2 === 0 ? 0 : 1]; // alternate blunt and sharp
    console.log(`Generating melee weapons ${meleeExisting.length + i + 1} to ${meleeExisting.length + i + count} (${sub.cat})...`);

    const system = `You generate INDIVIDUAL, SPECIFIC melee weapon entries for a cyberpunk world called Meridian 88 (Great Lakes megacity corridor, corporate sovereignty, neural augmentation ubiquitous, tiered citizenship, QUANTA currency symbol is \u03A6). Each entry is ONE specific melee weapon — a particular model with a manufacturer, model number, and street name. Focus on ${sub.desc}. Return a JSON array of ${count} entries. Each must have: name (specific model name with designation, e.g. "Crucible KR-7 Shock Maul" or "Helix Mono-Edge Tanto Mk.III"), type ("weapon"), aliases (array of street names/nicknames), category ("${sub.cat}"), description (2-3 paragraphs — how THIS specific weapon works, what makes it distinctive, materials used, what it feels like to wield), manufacturer (use these corponations: ${corps.slice(0, 15).join(', ')}), tier_availability (e.g. "Tier 3+", "Black market", "Universal"), legality (e.g. "Restricted", "Prohibited", "Licensed", "Unrestricted"), base_technologies (array of foundational tech names like "Piezoelectric disruption", "Molecular edge alignment", "Kinetic energy storage", "Neural feedback loops", "Vibration frequency modulation"), specifications (object with weight, length, material, power_source if applicable, edge_type or impact_type, damage_profile), tactical_use (how operators use it — close quarters, stealth kills, crowd control, etc.), cultural_context (social meaning, who carries it, street reputation, legal gray areas), known_users (array of character archetypes), story_hooks (array of 2-3 hooks), price (in \u03A6), tags (array). Every weapon must feel like a real product someone would buy in this world.`;

    const user = `Existing weapon names (DO NOT duplicate): ${existingNames.slice(-40).join(', ')}. Generate ${count} NEW ${sub.cat} melee weapons. Return ONLY the JSON array.`;

    try {
      const result = await callClaude(system, user, 8192);
      const items = parseJsonArray(result);
      for (const item of items) {
        writeEntityFile(dir, item);
        existingNames.push(item.name);
      }
      console.log(`  Added ${items.length}, total melee: ${meleeExisting.length + i + items.length}`);
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
  const count = process.argv[3] ? parseInt(process.argv[3]) : null;

  if (type === 'corps' || type === 'all') await generateCorps(count || 50);
  if (type === 'chars' || type === 'all') await generateChars(count || 50);
  if (type === 'districts' || type === 'all') await generateDistricts(count || 50);
  if (type === 'tech' || type === 'all') await generateTech(count || 200);
  if (type === 'docs' || type === 'all') await generateDocs(count || 1024);
  if (type === 'weapons' || type === 'all') await generateWeapons(count || 512);
  if (type === 'factions' || type === 'all') await generateFactions(count || 50);
  if (type === 'equipment' || type === 'all') await generateEquipment(count || 512);
  if (type === 'consumer_goods' || type === 'all') await generateConsumerGoods(count || 500);
  if (type === 'pharma' || type === 'all') await generatePharma(count || 200);
  if (type === 'substrates' || type === 'all') await generateSubstrates(count || 200);
  if (type === 'melee' || type === 'all') await generateMelee(count || 100);

  console.log('\n=== DONE ===');
  const corps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'corponations.json'), 'utf8'));
  const chars = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'characters.json'), 'utf8'));
  const dists = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'districts.json'), 'utf8'));
  const docs = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'worldbuilding_docs.json'), 'utf8'));
  const weaps = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'weaponry.json'), 'utf8'));
  const facs = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'factions.json'), 'utf8'));
  const equip = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'equipment.json'), 'utf8'));
  const tech = JSON.parse(fs.readFileSync(path.join(ENGINE_DATA, 'technology.json'), 'utf8'));
  const cGoods = readDirEntities(path.join(ENGINE_DATA, 'consumer_goods'));
  const pharma = readDirEntities(path.join(ENGINE_DATA, 'pharmaceuticals'));
  const subs = readDirEntities(path.join(ENGINE_DATA, 'substrates'));
  const meleeWeaps = readDirEntities(path.join(ENGINE_DATA, 'weaponry')).filter(w => w.category === 'melee' || w.category === 'melee_blunt' || w.category === 'melee_sharp');
  console.log(`Corps: ${corps.length}, Chars: ${chars.length}, Districts: ${dists.length}, Docs: ${docs.length}, Tech: ${tech.length}, Weapons: ${weaps.length}, Factions: ${facs.length}, Equipment: ${equip.length}`);
  console.log(`Consumer Goods: ${cGoods.length}, Pharmaceuticals: ${pharma.length}, Substrates: ${subs.length}, Melee Weapons: ${meleeWeaps.length}`);
}

main().catch(e => console.error(e));
