// BCI cyberware generator for StreetSamurai
// Generates 70 BCI cyberware JSON files in engine/data/cyberware/
// Run: node generate_bci_2.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'cyberware');
const WAIT_MS = 5000;
const RETRY_WAIT_MS = 15000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 16000) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify({
      model: MODEL,
      max_tokens: maxTokens,
      temperature: 0.9,
      system: system,
      messages: [{ role: 'user', content: user }]
    });
    console.log(`    [API] Sending request (${body.length} bytes)...`);
    const req = https.request({
      hostname: 'api.anthropic.com',
      path: '/v1/messages',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'x-api-key': API_KEY,
        'anthropic-version': '2023-06-01',
      },
      timeout: 300000,
    }, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        console.log(`    [API] Response received (${data.length} bytes, status ${res.statusCode})`);
        try {
          const j = JSON.parse(data);
          if (j.content && j.content[0]) resolve(j.content[0].text);
          else reject(new Error(data.substring(0, 500)));
        } catch (e) { reject(e); }
      });
    });
    req.setTimeout(300000, () => { req.destroy(); reject(new Error('Request timeout after 300s')); });
    req.on('error', e => { console.error(`    [API] Request error: ${e.message}`); reject(e); });
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
  if (start === -1 || end === -1) throw new Error('No JSON array found in response');
  json = json.substring(start, end + 1);

  try {
    return JSON.parse(json);
  } catch (e) {
    json = json.replace(/,\s*([}\]])/g, '$1');
    json = json.replace(/(?<=": ")((?:[^"\\]|\\.)*)(?=")/gs, (m) => {
      return m.replace(/\n/g, '\\n').replace(/\r/g, '\\r').replace(/\t/g, '\\t');
    });
    try {
      return JSON.parse(json);
    } catch (e2) {
      const objects = [];
      let depth = 0;
      let objStart = -1;
      for (let i = 0; i < json.length; i++) {
        if (json[i] === '{' && depth === 0) { objStart = i; depth++; }
        else if (json[i] === '{') depth++;
        else if (json[i] === '}') {
          depth--;
          if (depth === 0 && objStart >= 0) {
            try {
              let objStr = json.substring(objStart, i + 1);
              objStr = objStr.replace(/,\s*([}\]])/g, '$1');
              objects.push(JSON.parse(objStr));
            } catch (e3) { /* skip malformed object */ }
            objStart = -1;
          }
        }
      }
      if (objects.length > 0) return objects;
      throw new Error(`No parseable JSON found: ${e.message}`);
    }
  }
}

function slugify(name, maxLen = 80) {
  let slug = name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
  if (slug.length > maxLen) slug = slug.substring(0, maxLen).replace(/_+$/, '');
  return slug;
}

function generateId() {
  const bytes = [];
  for (let i = 0; i < 16; i++) bytes.push(Math.floor(Math.random() * 256));
  return bytes.map(b => b.toString(16).padStart(2, '0')).join('');
}

function saveItem(item) {
  const slug = slugify(item.name);
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${item.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(item, null, 2));
  return true;
}

function getExistingNames() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const names = [];
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      if (data.name) names.push(data.name);
    } catch (e) { /* skip bad files */ }
  }
  return names;
}

// ── World Context ──
const WORLD_CONTEXT = `GLMZ, year 2200. Great Lakes megacity. Currency: Φ (QUANTA). Tiers: 1 (Shelf/poorest) to 5 (Spire/elite). Ubiquitous Diaspora: fully interbred humanity. BCIs are as fundamental as smartphones. Iowan Behemoths are autonomous machines, NOT synthetic life.

BCI Manufacturers: TESSERA CORPONATION (enterprise/security), ARCTURUS DEFENSE SOLUTIONS (military/combat), LAZARUS PHARMACEUTICALS (medical), NeuralPath (consumer "Samsung"), CortexDynamics (performance), MindBridge (budget), SynapTech (premium "Apple").`;

// ── Category Definitions ──
const CATEGORIES = [
  // mid_range_consumer: DONE (20/20) — skipping
  // {
  //   category: 'bci',
  //   subcategory: 'mid_range_consumer',
  //   count: 20,
  // },
  {
    category: 'bci',
    subcategory: 'professional',
    count: 15,
    prompt: `Generate {count} PROFESSIONAL-GRADE BCI (Brain-Computer Interface) cyberware products. Tier 3-4 availability.

These are corporate-issue and specialist BCIs with enhanced capabilities:
- Corporate-issue BCIs with enhanced security, encryption, and data compartmentalization
- Medical-grade BCIs for surgeons and specialists (precision neural control, tremor elimination)
- Pilot-rated BCIs for vehicle neural link (ground, air, and water vehicles)
- Security-hardened BCIs for CorpSec operatives (intrusion detection, loyalty verification)
- Legal/financial BCIs with enhanced memory indexing and recall
- Engineering BCIs with spatial processing and CAD neural overlay

Manufacturers: TESSERA CORPONATION (corporate/security), LAZARUS PHARMACEUTICALS (medical), plus NeuralPath Pro line, CortexDynamics Enterprise, SynapTech Professional.
Price range: Φ8,000 - Φ35,000 street / Φ12,000 - Φ50,000 licensed.
These are workplace tools — employer-provided or tax-deductible professional equipment.`
  },
  {
    category: 'bci',
    subcategory: 'military_elite',
    count: 15,
    prompt: `Generate {count} MILITARY/ELITE BCI (Brain-Computer Interface) cyberware products. Tier 4-5 availability.

These are combat-grade and elite neural interfaces:
- Arcturus combat BCIs with tactical overlay (threat detection, IFF, squad coordination)
- Multi-stream processing BCIs (handle 4-8 simultaneous data feeds without cognitive overload)
- EMP-hardened BCIs with Faraday mesh shielding
- Encrypted military-grade communication BCIs (quantum-encrypted channels)
- Direct automaton control interfaces (command combat drones, Iowan Behemoths, security automata)
- Battlefield medical triage BCIs (monitor squad vitals, triage recommendations)
- Electronic warfare BCIs (signal jamming, countermeasure deployment)

Manufacturers: ARCTURUS DEFENSE SOLUTIONS (primary), TESSERA CORPONATION (some models).
Price range: Φ40,000 - Φ200,000 street / Φ60,000 - Φ300,000 licensed.
Legality: Restricted or Military-Only. These are NOT consumer products. Civilian possession is illegal or heavily regulated. Note: Iowan Behemoths are autonomous machines, NOT synthetic life.`
  },
  {
    category: 'bci',
    subcategory: 'luxury_exotic',
    count: 10,
    prompt: `Generate {count} LUXURY/EXOTIC BCI (Brain-Computer Interface) cyberware products. Tier 5 availability.

These are the pinnacle of BCI technology — custom, rare, and extraordinary:
- Custom artisan BCIs with hand-tuned neural architectures (limited production runs of <100 units)
- Full sensory recording and playback systems (record and re-experience memories in full fidelity)
- Psionic-amplification models (experimental — amplify latent psionic potential, poorly understood)
- E.L.F. compatibility modules (interface with Emergent Lifeforms — controversial, semi-legal)
- Consciousness expansion BCIs (simultaneous awareness across multiple data domains)
- Synesthetic BCIs (cross-wire senses — hear colors, taste music, genuinely experience synesthesia)
- Dream architecture BCIs (construct and share lucid dream environments)

Manufacturers: TESSERA CORPONATION (luxury line), artisan workshops (named), experimental labs. Some should have no clear manufacturer (black market origins).
Price range: Φ150,000 - Φ2,000,000+ street.
Legality: Varies — Licensed to Prohibited. Some are one-of-a-kind prototypes.`
  },
  {
    category: 'bci',
    subcategory: 'specialized',
    count: 10,
    prompt: `Generate {count} SPECIALIZED BCI (Brain-Computer Interface) cyberware products. Tier varies.

These serve specific populations or niche needs:
- Child-safe BCIs (limited functionality, controversial — some parents insist, others condemn; Tier 2-3)
- Elderly cognitive support BCIs (memory preservation, dementia management, cognitive scaffolding; Tier 2-4)
- Anti-surveillance privacy BCIs (signal masking, feed spoofing, identity obfuscation — gray/black market; Tier 3-4)
- Creative-industry BCIs for artists/musicians (direct neural-to-medium interfaces, composition tools; Tier 3-4)
- Therapeutic BCIs for PTSD/trauma (memory compartmentalization, emotional regulation; Tier 3-4)
- Accessibility BCIs for disabled individuals (motor control restoration, sensory substitution; Tier 2-3)

Manufacturers: Mix of all brands. Child BCIs from MindBridge and NeuralPath. Medical from LAZARUS. Privacy BCIs from unnamed/black-market sources. Creative from SynapTech and boutique brands.
Price range: Φ500 - Φ50,000 depending on type.
Legality: Varies widely — Unrestricted for medical/accessibility, Licensed for child BCIs, Restricted/Prohibited for privacy BCIs.`
  },
];

const SCHEMA_INSTRUCTIONS = `JSON schema (all fields required):
{ "id": "32-hex-chars", "name": "Manufacturer Model Name (max 60 chars)", "brand_name": "", "product_name": "", "type": "cyberware", "aliases": ["street names"], "category": "bci", "body_location": "cranial", "description": "2 paragraphs separated by \\n\\n. P1: technical. P2: cultural/who uses it.", "manufacturer": "FULL NAME IN CAPS", "tier_availability": "Tier X-Y", "legality": "Unrestricted/Licensed/Restricted/Military-Only/Prohibited", "installation_requirements": "brief", "rejection_risk": "Minimal/Low/Moderate/High + why", "maintenance": "brief", "specifications": "JSON string of tech specs", "side_effects": ["2-4 items"], "cultural_context": "1 paragraph", "known_users": ["demographics"], "story_hooks": ["2-3 RPG hooks"], "street_price": "Φ amount", "licensed_price": "Φ amount (1.5-3x street)", "tags": ["cyberware", "bci", ...] }

Rules: id=random 32 hex chars. name<=60 chars. Φ=QUANTA currency symbol. specifications=stringified JSON object.`;

// ── Main Generation Loop ──
async function generateCategory(catDef) {
  const { subcategory, count, prompt } = catDef;

  const existingNames = getExistingNames();
  console.log(`\n[${subcategory}] Target: ${count} items.`);

  const BATCH = 3;
  let generated = 0;

  for (let i = 0; i < count; i += BATCH) {
    const batchSize = Math.min(BATCH, count - i);

    const allExisting = getExistingNames();
    // Only send the last 80 names to keep prompt size reasonable
    const existingShort = allExisting.slice(-80).map(n => n.replace(/[^a-zA-Z0-9 ]/g, '').substring(0, 40));

    const system = `You generate BCI (Brain-Computer Interface) cyberware entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} BCI objects. No explanation, no markdown fencing, just the raw JSON array.

${WORLD_CONTEXT}

${SCHEMA_INSTRUCTIONS}`;

    const user = `${prompt.replace('{count}', String(batchSize))}

EXISTING NAMES (DO NOT DUPLICATE OR CREATE SIMILAR): ${existingShort.join(', ')}

Generate exactly ${batchSize} unique BCI cyberware products. Each must have a distinctive manufacturer + model name. Return ONLY the JSON array.`;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating ${batchSize} items...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          item.type = 'cyberware';
          item.category = 'bci';
          item.body_location = 'cranial';

          if (!item.id || !/^[0-9a-f]{32}$/.test(item.id)) {
            item.id = generateId();
          }

          if (item.name && item.name.length > 60) {
            item.name = item.name.substring(0, 60).trim();
          }

          if (item.specifications && typeof item.specifications === 'object') {
            item.specifications = JSON.stringify(item.specifications);
          }

          // Ensure tags include bci and subcategory
          if (!Array.isArray(item.tags)) item.tags = [];
          if (!item.tags.includes('cyberware')) item.tags.unshift('cyberware');
          if (!item.tags.includes('bci')) item.tags.splice(1, 0, 'bci');
          if (!item.tags.includes(subcategory)) item.tags.push(subcategory);

          if (saveItem(item)) {
            saved++;
            generated++;
          }
        }
        console.log(`    Saved ${saved}/${items.length} items.`);
        break;
      } catch (e) {
        retries++;
        console.error(`    Error (attempt ${retries}/3): ${e.message}`);
        if (retries < 3) {
          console.log(`    Retrying in ${RETRY_WAIT_MS / 1000}s...`);
          await sleep(RETRY_WAIT_MS * retries);
        }
      }
    }

    if (i + BATCH < count) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${subcategory}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai BCI Generator (Wave 2) ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} BCI items across ${CATEGORIES.length} subcategories\n`);

  const existingFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`Existing cyberware files: ${existingFiles.length}`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateCategory(catDef);
    totalGenerated += n;
  }

  const finalCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Total files in cyberware/: ${finalCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
