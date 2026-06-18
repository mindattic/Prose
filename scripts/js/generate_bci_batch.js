// Single-batch BCI generator. Usage: node generate_bci_batch.js <subcategory> <count>
// Generates <count> BCIs for the given subcategory in one API call.
// Designed to be called repeatedly from a shell loop.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'cyberware');

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

const subcategory = process.argv[2] || 'professional';
const batchSize = parseInt(process.argv[3] || '3', 10);

function callClaude(system, user) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: 12000,
      temperature: 0.9,
      system,
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
      },
      timeout: 240000,
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(`API error: ${data.substring(0, 300)}`));
        } catch (e) { reject(new Error(`Parse error: ${e.message} — ${data.substring(0, 200)}`)); }
      });
    });
    req.setTimeout(240000, () => { req.destroy(); reject(new Error('Timeout')); });
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
  if (start === -1 || end === -1) throw new Error('No JSON array found');
  json = json.substring(start, end + 1);
  try { return JSON.parse(json); } catch (e) {
    json = json.replace(/,\s*([}\]])/g, '$1');
    try { return JSON.parse(json); } catch (e2) {
      const objects = [];
      let depth = 0, objStart = -1;
      for (let i = 0; i < json.length; i++) {
        if (json[i] === '{' && depth === 0) { objStart = i; depth++; }
        else if (json[i] === '{') depth++;
        else if (json[i] === '}') { depth--; if (depth === 0 && objStart >= 0) { try { objects.push(JSON.parse(json.substring(objStart, i + 1).replace(/,\s*([}\]])/g, '$1'))); } catch (e3) {} objStart = -1; } }
      }
      if (objects.length > 0) return objects;
      throw new Error('No parseable JSON');
    }
  }
}

function slugify(name, maxLen = 80) {
  let slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '');
  if (slug.length > maxLen) slug = slug.substring(0, maxLen).replace(/_+$/, '');
  return slug;
}

function generateId() {
  const bytes = [];
  for (let i = 0; i < 16; i++) bytes.push(Math.floor(Math.random() * 256));
  return bytes.map(b => b.toString(16).padStart(2, '0')).join('');
}

function getExistingBciNames() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const names = [];
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      if (data.category === 'bci' && data.name) names.push(data.name);
    } catch (e) {}
  }
  return names;
}

const PROMPTS = {
  professional: `Generate {count} PROFESSIONAL-GRADE BCI cyberware products. Tier 3-4.
Corporate-issue with enhanced security/encryption. Medical-grade for surgeons. Pilot-rated for vehicle neural link. Security-hardened for CorpSec. Legal/financial with memory indexing.
Manufacturers: TESSERA CorpoNation, LAZARUS PHARMACEUTICALS, NeuralPath Pro, CortexDynamics, SynapTech Professional.
Prices: Φ8,000-Φ35,000 street / Φ12,000-Φ50,000 licensed.`,

  military_elite: `Generate {count} MILITARY/ELITE BCI cyberware products. Tier 4-5.
Arcturus combat BCIs with tactical overlay. Multi-stream processing (4-8 simultaneous data feeds). EMP-hardened with Faraday mesh. Quantum-encrypted military comms. Direct automaton control interfaces (for combat drones, Iowan Behemoths — which are autonomous MACHINES, not life). Battlefield triage BCIs. Electronic warfare BCIs.
Manufacturers: ARCTURUS DEFENSE SOLUTIONS (primary), TESSERA CorpoNation.
Prices: Φ40,000-Φ200,000 street / Φ60,000-Φ300,000 licensed.
Legality: Restricted or Military-Only.`,

  luxury_exotic: `Generate {count} LUXURY/EXOTIC BCI cyberware products. Tier 5.
Custom artisan BCIs (limited runs <100 units). Full sensory recording/playback. Psionic-amplification (experimental). E.L.F. compatibility modules (Emergent Lifeform interfacing). Consciousness expansion. Synesthetic BCIs. Dream architecture BCIs.
Manufacturers: TESSERA CorpoNation luxury line, artisan workshops, experimental labs, some with unknown/black market origins.
Prices: Φ150,000-Φ2,000,000+ street.
Legality: Licensed to Prohibited.`,

  specialized: `Generate {count} SPECIALIZED BCI cyberware products. Tier varies.
Child-safe BCIs (limited, controversial; Tier 2-3). Elderly cognitive support (dementia management; Tier 2-4). Anti-surveillance privacy BCIs (signal masking, feed spoofing; gray/black market; Tier 3-4). Creative-industry for artists/musicians (neural-to-medium interfaces; Tier 3-4). Therapeutic for PTSD/trauma (memory compartmentalization; Tier 3-4). Accessibility for disabled (motor control restoration; Tier 2-3).
Manufacturers: Mix — MindBridge/NeuralPath for child BCIs, LAZARUS for medical, unnamed for privacy, SynapTech for creative.
Prices: Φ500-Φ50,000.
Legality: Varies — Unrestricted for medical, Licensed for child, Restricted for privacy.`,
};

const SYSTEM = `You generate BCI cyberware for GLMZ, year 2200. Return ONLY a JSON array of exactly {count} objects. No markdown fencing.

Each object: { "id": "32-hex", "name": "Manufacturer Model (max 60 chars)", "brand_name": "", "product_name": "", "type": "cyberware", "aliases": [], "category": "bci", "body_location": "cranial", "description": "2 paragraphs: P1 technical, P2 cultural. Separated by \\n\\n.", "manufacturer": "CAPS", "tier_availability": "", "legality": "", "installation_requirements": "", "rejection_risk": "", "maintenance": "", "specifications": "stringified JSON", "side_effects": ["2-4 items"], "cultural_context": "1 paragraph", "known_users": [], "story_hooks": ["2-3 hooks"], "street_price": "Φ amount", "licensed_price": "Φ amount", "tags": ["cyberware","bci",...] }

Currency symbol Φ is QUANTA. id=random 32 hex. specifications must be a JSON string not object.`;

async function main() {
  const existing = getExistingBciNames();
  const existingShort = existing.map(n => n.replace(/[^a-zA-Z0-9 ]/g, '').substring(0, 40));

  const promptTemplate = PROMPTS[subcategory];
  if (!promptTemplate) {
    console.error(`Unknown subcategory: ${subcategory}`);
    process.exit(1);
  }

  const system = SYSTEM.replace('{count}', String(batchSize));
  const user = `${promptTemplate.replace('{count}', String(batchSize))}

DO NOT duplicate these existing names: ${existingShort.join(', ')}

Return ONLY the JSON array of ${batchSize} items.`;

  console.log(`Generating ${batchSize} ${subcategory} BCIs...`);
  const result = await callClaude(system, user);
  const items = parseJsonArray(result);

  let saved = 0;
  for (const item of items) {
    item.type = 'cyberware';
    item.category = 'bci';
    item.body_location = 'cranial';
    if (!item.id || !/^[0-9a-f]{32}$/.test(item.id)) item.id = generateId();
    if (item.name && item.name.length > 60) item.name = item.name.substring(0, 60).trim();
    if (item.specifications && typeof item.specifications === 'object') item.specifications = JSON.stringify(item.specifications);
    if (!Array.isArray(item.tags)) item.tags = [];
    if (!item.tags.includes('cyberware')) item.tags.unshift('cyberware');
    if (!item.tags.includes('bci')) item.tags.splice(1, 0, 'bci');
    if (!item.tags.includes(subcategory)) item.tags.push(subcategory);

    const slug = slugify(item.name);
    const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
    if (fs.existsSync(filePath)) {
      console.log(`  SKIP: ${item.name}`);
      continue;
    }
    fs.writeFileSync(filePath, JSON.stringify(item, null, 2));
    saved++;
    console.log(`  SAVED: ${item.name}`);
  }
  console.log(`Done: ${saved}/${items.length} saved.`);
}

main().catch(e => { console.error('Error:', e.message); process.exit(1); });
