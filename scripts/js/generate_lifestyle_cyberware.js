// Lifestyle/quality-of-life cyberware generator for StreetSamurai
// Generates 100 lifestyle cyberware JSON files in engine/data/cyberware/
// Run: node generate_lifestyle_cyberware.js
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

function callClaude(system, user, maxTokens = 12000) {
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
      },
      timeout: 300000,
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
    req.setTimeout(300000, () => { req.destroy(); reject(new Error('Request timeout after 300s')); });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function parseJsonArray(text) {
  let json = text.trim();
  // Strip markdown fencing
  if (json.startsWith('```')) {
    json = json.substring(json.indexOf('\n') + 1);
    if (json.endsWith('```')) json = json.slice(0, -3);
    json = json.trim();
  }
  const start = json.indexOf('[');
  const end = json.lastIndexOf(']');
  if (start === -1 || end === -1) throw new Error('No JSON array found in response');
  json = json.substring(start, end + 1);

  // Try direct parse first
  try {
    return JSON.parse(json);
  } catch (e) {
    // Attempt repair: fix common issues
    // Remove trailing commas before } or ]
    json = json.replace(/,\s*([}\]])/g, '$1');
    // Fix unescaped newlines in strings
    json = json.replace(/(?<=": ")((?:[^"\\]|\\.)*)(?=")/gs, (m) => {
      return m.replace(/\n/g, '\\n').replace(/\r/g, '\\r').replace(/\t/g, '\\t');
    });
    try {
      return JSON.parse(json);
    } catch (e2) {
      // Last resort: try to extract individual objects
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
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (Phi symbol is always written as the character itself, not spelled out). Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Default to mixed heritage from unexpected global combinations. There is no dominant ethnicity.

Corponations are sovereign corporate entities that manufacture most goods. Key cyberware/biotech companies include:
- Lazarus Pharmaceuticals (and its subsidiaries) — medical-grade implants, health cyberware
- MindBridge — cognitive and neural enhancement consumer brand
- VitaCore — health and wellness implants, affordable medical cyberware
- SynapTech — sensory and cognitive implants, mid-range
- NeuralPath — brain-computer interface accessories and cognitive tools
- LifeWire — convenience and lifestyle implants, the "Apple" of consumer cyberware
- PulsePoint — fitness/health monitoring implants, sports and wellness
- Helix Biosystems — already exists in the world, premium biotech
- Tessera Corponation — already exists, high-end enterprise
- Zheng Dao Bioelectric — already exists, industrial/professional grade

These are NOT combat cyberware. These are everyday enhancements that regular people get — the cyberware equivalent of smartphones, fitness trackers, and cosmetic procedures. Most are affordable (Tier 1-3 availability). They are ubiquitous. Getting a sleep optimizer or payment chip is like getting braces or a smartphone in 2024.

Brand names should sound like real consumer product brands — professional, sometimes evocative, never parody. Think how Fitbit, Invisalign, Nexplanon, or LASIK sound.`;

// ── Category Definitions ──
const CATEGORIES = [
  {
    category: 'cosmetic',
    count: 15,
    prompt: `Generate {count} COSMETIC/APPEARANCE lifestyle cyberware implants. These include:
- Skin color changers, programmable tattoo displays, hair color modulators
- Blemish removal implants, anti-aging dermal systems
- Eye color changers (fashion, NOT tactical)
- Voice modulators for singing/speaking enhancement
- Pheromone dispensers, personal scent generators
These are the cosmetic procedures of 2200 — as common as Botox, hair dye, and whitening strips today.`
  },
  {
    category: 'health',
    count: 15,
    prompt: `Generate {count} HEALTH/WELLNESS lifestyle cyberware implants. These include:
- Sleep optimizers (reduce needed sleep to 4 hours)
- Metabolism regulators (weight management)
- Allergy suppressors, immune system boosters
- Pain management implants (for chronic pain sufferers)
- Blood pressure regulators, cardiac monitors
- Addiction suppression chips
- Fertility control implants
These are medical quality-of-life implants — the insulin pumps and pacemakers of 2200, but for everyone.`
  },
  {
    category: 'sensory',
    count: 15,
    prompt: `Generate {count} SENSORY ENHANCEMENT lifestyle cyberware implants. These include:
- Taste enhancers (food tastes better, popular with synth-food eaters)
- Smell filters (block bad odors, enhance pleasant ones)
- Touch sensitivity adjustment (for artisans, musicians, lovers)
- Perfect pitch implant for musicians
- Color spectrum expansion (see more colors than baseline human)
- Night adaptation (mild — just see better in dim rooms, NOT tactical night vision)
These are sensory quality-of-life implants — not military-grade sensor suites, just making everyday senses better.`
  },
  {
    category: 'cognitive',
    count: 15,
    prompt: `Generate {count} COGNITIVE lifestyle cyberware implants. These include:
- Memory enhancement (better recall, NOT recording — that's different tech)
- Language translation implants (real-time translation for daily life)
- Math co-processors (instant calculation, popular with traders and students)
- Speed reading enhancers
- Mood stabilizers (anxiety/depression management — medical grade)
- Focus enhancers (ADHD management, study aids)
- Dream recorders (capture dreams for playback — huge entertainment market)
These are the Adderall, antidepressants, and language apps of 2200, but built into your brain.`
  },
  {
    category: 'convenience',
    count: 15,
    prompt: `Generate {count} CONVENIENCE lifestyle cyberware implants. These include:
- Internal clock/alarm (wake up exactly when you want)
- Temperature regulation (never too hot or cold)
- Subdermal wallet/payment chip
- Internal compass/GPS
- Calorie counters (know exactly what you ate and its nutritional content)
- Hydration monitors with thirst suppression
- Air quality sensors with mild filtration enhancement
These are the smartwatches and fitness trackers of 2200, except they're inside you.`
  },
  {
    category: 'social',
    count: 10,
    prompt: `Generate {count} SOCIAL/COMMUNICATION lifestyle cyberware implants. These include:
- Subvocal communicators (talk without moving your lips)
- Emotion display mods (forehead LED mood indicators — a fashion trend among young people)
- Social cue enhancers (helps read body language — very popular with neurodivergent people)
- Name/face recall assist (never forget a name at a party)
- Personal translator earpiece (implanted, real-time conversation translation)
These are social tools — helping people connect, communicate, and navigate social situations.`
  },
  {
    category: 'professional',
    count: 15,
    prompt: `Generate {count} WORK/PROFESSIONAL lifestyle cyberware implants. These include:
- Steady-hand implants (for surgeons, artists, baristas, anyone needing precision)
- Back support exoskeletal reinforcement (dock workers, nurses, warehouse workers)
- Knee/joint reinforcement (runners, construction workers, delivery people)
- Lung capacity enhancers (singers, divers, athletes)
- Grip enhancers (mechanics, climbers, movers)
- Vocal cord reinforcement (teachers, singers, street vendors who shout all day)
- UV protection dermal layer (outdoor workers)
These are workplace accommodations of 2200 — like steel-toed boots and ergonomic chairs, but implanted.`
  },
];

const SCHEMA_INSTRUCTIONS = `Each item MUST conform to this exact JSON schema:
{
  "id": "<32-character hex string, random>",
  "name": "Brand Model Name",
  "brand_name": "The consumer brand (MindBridge, VitaCore, SynapTech, NeuralPath, LifeWire, PulsePoint, or a Lazarus subsidiary for medical ones, or a new plausible brand)",
  "product_name": "The specific product name within the brand",
  "type": "cyberware",
  "aliases": ["street names, nicknames, slang terms people use for this implant"],
  "category": "{category}",
  "body_location": "where in the body this is installed (e.g., 'subdermal forearm', 'cranial', 'spinal', 'retinal', 'laryngeal', 'dermal full-body')",
  "description": "2 full paragraphs. Paragraph 1: What it does technically, how it works. Paragraph 2: Who gets it, what daily life is like with it, why it matters to ordinary people.",
  "manufacturer": "FULL CORPONATION NAME IN CAPS",
  "tier_availability": "Tier X-Y (most should be Tier 1-3, some Tier 2-4, very few Tier 3-5)",
  "legality": "Unrestricted or Licensed (most lifestyle cyberware is Unrestricted; medical ones may be Licensed)",
  "installation_requirements": "Brief description — outpatient clinic, pharmacy kiosk, or requires specialist",
  "rejection_risk": "Minimal/Low/Moderate — these are consumer products, rejection should be rare",
  "maintenance": "How often, what's needed — annual checkup, firmware updates, battery replacement, etc.",
  "specifications": "JSON string with technical specs relevant to the implant",
  "side_effects": ["array of 2-4 realistic side effects — mild things like 'occasional tingling', 'vivid dreams during calibration', 'mild headache first week'"],
  "cultural_context": "1 full paragraph on social perception — is this seen as normal as wearing glasses? Is there a stigma? A fashion statement? A class marker?",
  "known_users": ["types of people who commonly have this — not specific characters, just demographics"],
  "story_hooks": ["2-3 narrative hooks for tabletop RPG scenarios involving this implant"],
  "street_price": "price in currency format using the actual phi character, e.g. 'something between 50 and 15000 depending on tier'",
  "licensed_price": "price at official clinics, usually 1.5-3x street price",
  "tags": ["cyberware", "{category}", "lifestyle", plus 3-5 more relevant tags]
}

IMPORTANT:
- The "id" field must be a random 32-character hexadecimal string (like a UUID without dashes).
- The "name" field must be 60 characters or fewer.
- Prices use the actual Unicode phi character: use the word PHI_SYMBOL as placeholder and I will replace it.
- "specifications" must be a JSON string (stringified object), not a raw object.
- Most items should be Tier 1-3 affordable. These are MASS MARKET consumer products.
- Brand names from this list preferred: MindBridge, VitaCore, SynapTech, NeuralPath, LifeWire, PulsePoint. Use Lazarus subsidiaries for medical items. Can also invent new plausible consumer brands.`;

function getExistingByCategory() {
  const files = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  const byCat = {};
  for (const f of files) {
    try {
      const data = JSON.parse(fs.readFileSync(path.join(OUTPUT_DIR, f), 'utf8'));
      const cat = data.category || 'unknown';
      byCat[cat] = (byCat[cat] || 0) + 1;
    } catch (e) { /* skip */ }
  }
  return byCat;
}

// ── Main Generation Loop ──
async function generateCategory(catDef) {
  const { category, count, prompt } = catDef;

  // Check how many we already have for this category
  const byCat = getExistingByCategory();
  const existingCount = byCat[category] || 0;
  const needed = count - existingCount;

  if (needed <= 0) {
    console.log(`\n[${category}] Already have ${existingCount}/${count}. Skipping.`);
    return 0;
  }

  console.log(`\n[${category}] Have ${existingCount}/${count}. Need ${needed} more.`);

  const BATCH = 5;
  let generated = 0;

  for (let i = 0; i < needed; i += BATCH) {
    const batchSize = Math.min(BATCH, needed - i);

    // Get just the cyberware names (shortened to avoid huge prompts)
    const allExisting = getExistingNames();
    // Only send a compact comma-separated list
    const existingShort = allExisting.map(n => n.replace(/[^a-zA-Z0-9 ]/g, '').substring(0, 40));

    const system = `You generate lifestyle cyberware implant entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} cyberware objects. No explanation, no markdown fencing, just the raw JSON array.

${WORLD_CONTEXT}

${SCHEMA_INSTRUCTIONS.replace(/\{category\}/g, category)}`;

    const user = `${prompt.replace('{count}', batchSize)}

EXISTING NAMES (DO NOT DUPLICATE): ${existingShort.join(', ')}

Generate exactly ${batchSize} unique lifestyle cyberware implants for the "${category}" category. Each must have a distinctive brand + product name. Return ONLY the JSON array.`;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating ${batchSize} items...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user);

        // Replace PHI_SYMBOL placeholder with actual phi character
        const cleaned = result.replace(/PHI_SYMBOL/g, '\u03A6');
        const items = parseJsonArray(cleaned);

        let saved = 0;
        for (const item of items) {
          // Enforce correct type and category
          item.type = 'cyberware';
          item.category = category;

          // Ensure id is 32 hex chars
          if (!item.id || !/^[0-9a-f]{32}$/.test(item.id)) {
            item.id = generateId();
          }

          // Truncate name to 60 chars
          if (item.name && item.name.length > 60) {
            item.name = item.name.substring(0, 60).trim();
          }

          // Ensure specifications is a string
          if (item.specifications && typeof item.specifications === 'object') {
            item.specifications = JSON.stringify(item.specifications);
          }

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

  console.log(`[${category}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Lifestyle Cyberware Generator ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} lifestyle cyberware items across ${CATEGORIES.length} categories\n`);

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
