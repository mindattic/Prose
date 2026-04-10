// Apparel generator — Final Batch 2: 112 items (outfits, everyday, seasonal)
// Filenames: {id}.json where id is 32-char hex via crypto.randomBytes(16)
// Run: node generate_apparel_final_2.js
// Does NOT overwrite existing files.

const fs = require('fs');
const crypto = require('crypto');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'apparel');
const WAIT_MS = 3000;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 16384) {
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
    req.setTimeout(120000, () => {
      req.destroy();
      reject(new Error('Request timed out after 120s'));
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
  if (start === -1 || end === -1) throw new Error('No JSON array found in response');
  json = json.substring(start, end + 1);
  return JSON.parse(json);
}

function genId() {
  return crypto.randomBytes(16).toString('hex');
}

function saveItem(item) {
  if (!item.id || item.id.length !== 32) item.id = genId();
  const filePath = path.join(OUTPUT_DIR, `${item.id}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${item.name} [${item.id}]`);
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
    } catch (e) { /* skip */ }
  }
  return names;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ (GLMZ), year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (the symbol is \u03A6, representing QUANTA, not the Greek letter). Society is tiered:
- Tier 1 "The Shelf" — poorest, most dangerous. Reclaimed industrial zones, acid rain, patched infrastructure.
- Tier 2 "Circuit" — working class. Factory workers, transit operators, street vendors. Clean but functional.
- Tier 3 — middle management, cubicle workers, small business owners.
- Tier 4 — corporate comfort. Junior execs, specialists, skilled professionals.
- Tier 5 "The Spire" — ultra-elite. Corponation C-suite, power brokers, old money.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Default to mixed heritage from unexpected global combinations. Fashion reflects global fusion — no single cultural tradition dominates.

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, non-functional wings). Synthetics are artificial beings with non-human body proportions.

Corponations are sovereign corporate entities. They manufacture most goods. Street brands also exist — unlicensed, often better for specific niches, always with underground cachet.

Fashion notes: Clothing must accommodate augmentation (chrome arms, leg prosthetics, spinal rigs, neural ports) and geneware (tails, horns, wings, fur, scales). Aug-compatible means openings, channels, or adaptive seams for chrome. Gene-compatible means accommodation for biological modifications.

Great Lakes weather: brutal winters (sub-zero, lake-effect snow), humid summers, unpredictable spring/fall transitions, frequent rain off Lake Michigan. Clothing must handle these extremes.`;

// ── Categories ──
const CATEGORIES = [
  // ═══ COMPLETE OUTFITS (40 total) ═══
  {
    tag: 'military_corpsec_kit',
    count: 8,
    category: 'outfit',
    tier: 'Tier 2-5',
    prompt: `Generate {count} COMPLETE MILITARY/CORPSEC KITS — head-to-toe tactical outfits the narrator references as one item. Each for a DIFFERENT corponation: Crucible Genomics, Meridian Transit Authority, Stahl-Koenig Industries, Brightline, NovaChem, Yūrei Systems, Halcyon Dynamics, and one independent PMC. Full gear: helmet/headgear, armor/vest, fatigues, boots, load-bearing kit, ID markings. Corp-branded colors and insignia. Keep descriptions SHORT (2-4 sentences max).`
  },
  {
    tag: 'medical_scientific_kit',
    count: 8,
    category: 'outfit',
    tier: 'Tier 1-5',
    prompt: `Generate {count} COMPLETE MEDICAL/SCIENTIFIC OUTFITS — head-to-toe looks. Must include: 1 Shelf street clinic ripperdoc, 1 Tier 5 surgical suite surgeon, 1 lab technician, 1 field medic, 1 gene clinic worker, 1 aug installation tech, 1 pharmaceutical researcher, 1 emergency paramedic. Full look: headgear/visor, coat/scrubs, gloves, footwear, tool harness. Keep descriptions SHORT (2-4 sentences max).`
  },
  {
    tag: 'underworld_criminal_kit',
    count: 8,
    category: 'outfit',
    tier: 'Tier 1-4',
    prompt: `Generate {count} COMPLETE UNDERWORLD/CRIMINAL OUTFITS — head-to-toe looks. Must include: 1 smuggler, 1 fixer (high-end intermediary), 1 enforcer, 1 thief/infiltrator, 1 black market dealer, 1 gang lieutenant, 1 underground pit fighter, 1 data fence. Scanner-resistant fabrics, hidden compartments, quick-strip features. Keep descriptions SHORT (2-4 sentences max).`
  },
  {
    tag: 'synthetic_adapted',
    count: 8,
    category: 'outfit',
    tier: 'Tier 1-5',
    prompt: `Generate {count} COMPLETE OUTFITS DESIGNED FOR SYNTHETIC (non-human) BODIES. Synthetics have non-standard proportions — extra-long limbs, unusual joints, variable torsos. Include: 1 service synthetic uniform, 1 companion synthetic evening wear, 1 labor synthetic coverall, 1 free synthetic expressing individuality, 1 synthetic "passing" outfit (trying to look human), 1 synthetic showing off chassis, 1 synthetic formal wear, 1 synthetic weather gear. Modular, maintenance-friendly. Keep descriptions SHORT (2-4 sentences max).`
  },
  {
    tag: 'unique_character_look',
    count: 8,
    category: 'outfit',
    tier: 'Tier 1-5',
    prompt: `Generate {count} UNIQUE CHARACTER-DEFINING OUTFITS — each so specific it defines a person on sight. Examples: a retired Spire exec slumming on the Shelf, a geneware-modded street preacher, a synth jazz musician, a disgraced CorpSec officer working as a bouncer, a Tier 3 accountant moonlighting as a runner, a former combat medic turned bartender, a child prodigy in oversized hand-me-downs, a courier who never stops moving. Each tells a story. Keep descriptions SHORT (2-4 sentences max).`
  },

  // ═══ EVERYDAY CLOTHING (40 total) ═══
  {
    tag: 'cheap_shelf_basics',
    count: 10,
    category: 'clothing',
    tier: 'Tier 1',
    prompt: `Generate {count} CHEAP SHELF BASICS — the everyday clothing Tier 1 Shelf residents wear. Worn, practical, held together with repair tape and re-stitching. Faded colors, patched knees, salvaged zippers. Includes: shirts, pants, underwear layers, socks, basic shoes, hats. NOT outfits — individual items. Each has been worn hard and repaired multiple times. Keep descriptions SHORT (2-3 sentences max).`
  },
  {
    tag: 'circuit_workwear',
    count: 10,
    category: 'clothing',
    tier: 'Tier 2',
    prompt: `Generate {count} CIRCUIT WORKWEAR items — durable, functional, sometimes branded. The stuff Tier 2 factory workers, transit operators, and maintenance crews wear daily. Includes: work shirts, cargo pants, steel-toe boots, tool belts, safety vests, coveralls. Company logos, name patches. NOT outfits — individual items. Functional and tough. Keep descriptions SHORT (2-3 sentences max).`
  },
  {
    tag: 'office_corporate',
    count: 10,
    category: 'clothing',
    tier: 'Tier 3-5',
    prompt: `Generate {count} OFFICE/CORPORATE CLOTHING items spanning tiers. Mix of: Tier 3 cubicle basics (mass-produced, conformist blouses/slacks/shoes), Tier 4 specialist pieces (better fabric, subtle personal touches), Tier 5 executive garments (bespoke, temperature-regulating, privacy-fabric). NOT outfits — individual items: blazers, trousers, blouses, dress shoes, ties/scarves, etc. Keep descriptions SHORT (2-3 sentences max).`
  },
  {
    tag: 'weekend_casual',
    count: 10,
    category: 'clothing',
    tier: 'Tier 1-4',
    prompt: `Generate {count} WEEKEND/CASUAL CLOTHING items — what people wear when not working. Ranges from Shelf lounge-at-home wear to Circuit bar-hopping clothes to Tier 3 weekend shopping outings. Comfortable, expressive, relaxed. Includes: t-shirts, hoodies, joggers, sneakers, casual dresses, tank tops, shorts. NOT outfits — individual items. Keep descriptions SHORT (2-3 sentences max).`
  },

  // ═══ SEASONAL (32 total) ═══
  {
    tag: 'winter_heavy',
    count: 8,
    category: 'clothing',
    tier: 'Tier 1-5',
    prompt: `Generate {count} WINTER HEAVY items for Great Lakes brutal winters (sub-zero, lake-effect snow, wind chill). Parkas, insulated boots, heated layers for chrome augments (cold metal against skin is dangerous), thermal balaclavas, insulated gloves rated for aug-hands. Must handle -30C wind chill off Lake Michigan. Range from Shelf salvage-insulated to Spire heated smart-fabric. NOT outfits — individual items. Keep descriptions SHORT (2-3 sentences max).`
  },
  {
    tag: 'summer_light',
    count: 8,
    category: 'clothing',
    tier: 'Tier 1-5',
    prompt: `Generate {count} SUMMER LIGHT items for Great Lakes humid summers (35C+, high humidity, UV exposure). Heat-managed fabrics, UV protective layers, breathable mesh, cooling underlayers for chrome (overheating metal augments). Includes: UV shirts, ventilated pants, cooling caps, moisture-wicking everything, sandals, sun shields. NOT outfits — individual items. Keep descriptions SHORT (2-3 sentences max).`
  },
  {
    tag: 'rain_gear',
    count: 8,
    category: 'clothing',
    tier: 'Tier 1-5',
    prompt: `Generate {count} RAIN GEAR items for Lake Michigan weather — frequent rain, sudden downpours, acid rain in lower tiers. Waterproof jackets, rain pants, sealed boots, umbrella-equivalents, hooded cloaks, waterproof gear bags. Some protect chrome from corrosive acid rain. Range from Shelf makeshift rain ponchos to Spire self-drying smart-fabric. NOT outfits — individual items. Keep descriptions SHORT (2-3 sentences max).`
  },
  {
    tag: 'transitional_layering',
    count: 8,
    category: 'clothing',
    tier: 'Tier 1-5',
    prompt: `Generate {count} TRANSITIONAL/LAYERING items for unpredictable Great Lakes spring and fall — 15C temperature swings in a single day, morning frost to afternoon warmth. Zip-off layers, convertible jackets, modular vests, thermal liners, arm sleeves, adaptive-weight scarves. Designed for quick add/remove. NOT outfits — individual items. Keep descriptions SHORT (2-3 sentences max).`
  },
];

// ── Generation Logic ──
async function generateBatch(catDef, allExistingNames) {
  const { tag, count, category, tier, prompt } = catDef;

  console.log(`\n[${tag}] Generating ${count} items...`);

  const BATCH = Math.min(count, 10);
  let generated = 0;

  for (let i = 0; i < count; i += BATCH) {
    const batchSize = Math.min(BATCH, count - i);

    const filledPrompt = prompt.replace('{count}', batchSize);

    const typeLabel = category === 'outfit' ? 'complete outfit' : 'individual clothing item';

    const system = `You generate apparel entries for the world of GLMZ. Return ONLY a JSON array of exactly ${batchSize} objects. No explanation, no markdown fencing, just the JSON array.

${WORLD_CONTEXT}

Each item MUST have exactly these fields:
{
  "id": "<32-character lowercase hex string>",
  "name": "Short Descriptive Name (under 60 chars)",
  "type": "apparel",
  "category": "${category}",
  "description": "SHORT description — 2-4 sentences max. Be specific about materials, colors, wear, and purpose. The narrator uses this to paint a quick picture.",
  "tier_association": "${tier}",
  "materials": ["array of materials used"],
  "functionality": "practical features, one sentence",
  "what_it_says": "what this communicates about the wearer — one sentence",
  "worn_by": ["types of people who wear this"],
  "manufacturer": "brand or maker",
  "price_range": "price using the \u03A6 (QUANTA) symbol",
  "aug_compatible": true or false,
  "gene_compatible": true or false,
  "story_hooks": ["1-2 SHORT narrative hooks"],
  "tags": ["apparel", "${category}", "${tag}", ...]
}

CRITICAL RULES:
- Each "id" MUST be a unique 32-character lowercase hex string. Generate truly random-looking hex.
- Keep descriptions SHORT. 2-4 sentences maximum. No long paragraphs.
- The \u03A6 symbol represents QUANTA currency, NOT the Greek letter phi.
- Tags must include "apparel", "${category}", and "${tag}".
- Ubiquitous Diaspora: fashion draws from ALL global traditions freely mixed.
- This is a ${typeLabel}.`;

    const user = `${filledPrompt}

EXISTING NAMES (DO NOT DUPLICATE): ${allExistingNames.slice(-200).join(', ')}

Generate exactly ${batchSize} items. Return ONLY the JSON array.`;

    console.log(`  Batch ${Math.floor(i / BATCH) + 1}: generating ${batchSize} items...`);

    let retries = 0;
    while (retries < 3) {
      try {
        const result = await callClaude(system, user, 16384);
        const items = parseJsonArray(result);

        let saved = 0;
        for (const item of items) {
          item.type = 'apparel';
          item.category = category;
          if (!item.id || item.id.length !== 32 || !/^[0-9a-f]{32}$/.test(item.id)) {
            item.id = genId();
          }
          if (!item.tags) item.tags = [];
          if (!item.tags.includes('apparel')) item.tags.unshift('apparel');
          if (!item.tags.includes(category)) item.tags.push(category);
          if (!item.tags.includes(tag)) item.tags.push(tag);
          item.name = (item.name || 'Unknown Item').slice(0, 60);

          if (saveItem(item)) {
            saved++;
            generated++;
            allExistingNames.push(item.name);
          }
        }
        console.log(`    Saved ${saved}/${items.length} items.`);
        break;
      } catch (e) {
        retries++;
        console.error(`    Error (attempt ${retries}/3): ${e.message}`);
        if (retries < 3) {
          console.log(`    Retrying in ${WAIT_MS / 1000}s...`);
          await sleep(WAIT_MS);
        }
      }
    }

    if (i + BATCH < count) {
      await sleep(WAIT_MS);
    }
  }

  console.log(`[${tag}] Generated ${generated} new items.`);
  return generated;
}

async function main() {
  console.log('=== StreetSamurai Apparel Generator — Final Batch 2 ===');
  console.log(`Output: ${OUTPUT_DIR}`);
  const totalTarget = CATEGORIES.reduce((s, c) => s + c.count, 0);
  console.log(`Target: ${totalTarget} items across ${CATEGORIES.length} categories\n`);

  const beforeCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`Existing files before: ${beforeCount}`);

  const allExistingNames = getExistingNames();
  console.log(`Loaded ${allExistingNames.length} existing names for dedup.`);

  let totalGenerated = 0;

  for (const catDef of CATEGORIES) {
    const n = await generateBatch(catDef, allExistingNames);
    totalGenerated += n;
  }

  const afterCount = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json')).length;
  console.log(`\n=== DONE ===`);
  console.log(`Files before: ${beforeCount}`);
  console.log(`Files after:  ${afterCount}`);
  console.log(`Generated this run: ${totalGenerated}`);
}

main().catch(e => {
  console.error('Fatal error:', e);
  process.exit(1);
});
