// Apparel generator (footwear + pants/legwear) for StreetSamurai
// Generates 200 apparel items as JSON files in engine/data/apparel/
// Run: node generate_apparel_1.js
// Does NOT overwrite existing files.

const fs = require('fs');
const https = require('https');
const path = require('path');

const settings = JSON.parse(fs.readFileSync(
  path.join(process.env.LOCALAPPDATA, 'MindAttic', 'StreetSamurai', 'Settings.json'), 'utf8'));
const API_KEY = settings.ApiKey;
const MODEL = 'claude-sonnet-4-6';
const OUTPUT_DIR = path.join(__dirname, '..', 'engine', 'data', 'apparel');
const WAIT_MS = 3000;
const BATCH_SIZE = 10;
const sleep = ms => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(OUTPUT_DIR)) fs.mkdirSync(OUTPUT_DIR, { recursive: true });

function callClaude(system, user, maxTokens = 8192) {
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
  const start = json.indexOf('[');
  const end = json.lastIndexOf(']');
  if (start === -1 || end === -1) throw new Error('No JSON array found in response');
  json = json.substring(start, end + 1);
  return JSON.parse(json);
}

function slugify(name) {
  return name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 80);
}

function saveItem(item) {
  item.name = (item.name || '').slice(0, 60);
  const slug = slugify(item.name);
  if (!slug) return false;
  const filePath = path.join(OUTPUT_DIR, `${slug}.json`);
  if (fs.existsSync(filePath)) {
    console.log(`    SKIP (exists): ${item.name}`);
    return false;
  }
  fs.writeFileSync(filePath, JSON.stringify(item, null, 2));
  return true;
}

function getExistingNames() {
  if (!fs.existsSync(OUTPUT_DIR)) return [];
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

function randomHex(len) {
  let hex = '';
  for (let i = 0; i < len; i++) hex += Math.floor(Math.random() * 16).toString(16);
  return hex;
}

// ── World Context ──
const WORLD_CONTEXT = `Setting: GLMZ, year 2200. A megacity in the Great Lakes corridor (Chicago-Milwaukee). Currency is Phi (Φ — the QUANTA currency symbol, NOT the Greek letter). Society is tiered: Tier 1 (the Shelf — poorest, most dangerous), Tier 2 (working class), Tier 3 (middle class), Tier 4 (corporate comfort), Tier 5 (the Spire — ultra-elite).

Technology: BCI (brain-computer interfaces) are common. Augmentation (cyberware/chrome) ranges from basic to military-grade. Geneware allows cosmetic and functional genetic modification (tails, bioluminescence, fur, horns, wings that don't work). Most people have some degree of augmentation or geneware — clothing must accommodate these body modifications.

Corponations are sovereign corporate entities. They manufacture most goods. Street brands also exist — unlicensed, often better quality for specific niches, always with underground cachet.

Ubiquitous Diaspora: By 2200, humanity is fully racially interbred. Fashion draws from every global tradition freely — no single culture dominates.

IMPORTANT: Most people dress NORMALLY. Jeans, sneakers, boots, cargo pants — everyday clothes exist. Not everything is high-tech. Mix mundane items with tech-enhanced ones. Tier 1-2 people wear patched, repaired, second-hand gear. Tier 3-4 wear practical modern clothes. Tier 5 wears bespoke luxury.

Brand names should sound like REAL brand names — professional, sometimes evocative, sometimes just a name. NOT parodies or jokes.`;

// ── Schema Template ──
const SCHEMA_INSTRUCTION = `Each item must follow this EXACT JSON schema:
{
  "id": "<32-character hex string>",
  "name": "Brand Model Name (max 60 chars)",
  "type": "apparel",
  "category": "footwear|pants|legwear",
  "description": "1-2 short paragraphs — what it looks like, what it does, who wears it. Keep it BRIEF.",
  "tier_association": "Tier X",
  "materials": ["material1", "material2"],
  "functionality": "what special features it has (or 'none' for basic items)",
  "what_it_says": "what wearing this communicates about you socially",
  "worn_by": ["type of person who wears this"],
  "manufacturer": "brand/company name",
  "price_range": "Φ amount or Φ low - Φ high",
  "aug_compatible": true/false,
  "gene_compatible": true/false,
  "story_hooks": ["1-2 short hooks for narrative use"],
  "tags": ["apparel", "category-name", "tier X", "other relevant tags"]
}

IMPORTANT:
- Generate unique 32-char hex IDs for each item.
- "description" must be 1-2 SHORT paragraphs. These are wardrobe items, not weapons. Focus on appearance, wearer, and social signal.
- "category" must be exactly "footwear", "pants", or "legwear" (use "legwear" for skirts, shorts, kilts, leggings, tights).
- aug_compatible means it works with augmented limbs/chrome legs. gene_compatible means it fits geneware body mods (tails, digitigrade legs, etc).
- Return ONLY a JSON array. No markdown, no explanation.`;

// ── Batch Definitions ──
const BATCHES = [
  // FOOTWEAR batches (100 total, 10 batches of 10)
  {
    label: 'Footwear Batch 1 — Tier 1 Shelf Boots',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 1 Shelf boots and shoes. Scrap-leather combat boots, salvaged-sole work boots, patched sneakers, duct-tape repaired runners, waterproofed-with-sealant shoes. These are worn, repaired, held together with determination. Cheap synth-leather, recycled tread, improvised waterproofing. Prices Φ5-Φ25.`
  },
  {
    label: 'Footwear Batch 2 — Tier 1-2 Work Boots',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 1-2 working class boots. Steel-toe work boots, mag-lock boots for industrial work, warehouse treaders, construction-grade stompers, dock-worker waterproofs. Rugged, functional, ugly but reliable. Some with basic safety features. Prices Φ15-Φ60.`
  },
  {
    label: 'Footwear Batch 3 — Tier 2-3 Everyday Sneakers',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 2-3 everyday sneakers and casual shoes. Running shoes, walking shoes, self-lacing sneakers (basic models), breathable mesh trainers, commuter shoes, everyday beaters. Normal shoes for normal people. Some with minor tech (LED accents, basic cushion-adjust). Prices Φ20-Φ80.`
  },
  {
    label: 'Footwear Batch 4 — Tier 2-3 Boots Mixed',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 2-3 boots of various types. Fashion boots, motorcycle boots, rain boots with grip-enhance, heated winter boots, ankle boots, Chelsea boots, hiking boots. Everyday boots people actually wear to work, clubs, and the street. Prices Φ30-Φ100.`
  },
  {
    label: 'Footwear Batch 5 — Aug-Compatible Footwear',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: augmentation-compatible shoes and boots across Tier 2-4. Shoes designed for chrome legs, adjustable-fit boots for variable-geometry augmented feet, sneakers with reconfigurable soles for augmented stride, magnetic-lock compatible shoes. These accommodate people with cybernetic lower limbs. All aug_compatible: true. Prices Φ45-Φ200.`
  },
  {
    label: 'Footwear Batch 6 — Gene-Compatible Footwear',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: geneware-compatible footwear across Tier 2-4. Shoes for digitigrade legs, wide-toe shoes for people with clawed feet geneware, flexible boots for unusual foot shapes, sandals that accommodate any foot morphology. All gene_compatible: true. Prices Φ40-Φ180.`
  },
  {
    label: 'Footwear Batch 7 — Tier 3-4 Smart Footwear',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 3-4 smart/tech-enhanced shoes. Self-lacing dress shoes, temperature-regulating boots, shock-absorbing trainers with active cushioning, stealth-sole shoes (noise dampening), grip-enhanced climbing shoes, health-monitoring insoles built-in. Mid-range tech that working professionals buy. Prices Φ80-Φ250.`
  },
  {
    label: 'Footwear Batch 8 — Tier 3-4 Dress/Fashion',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 3-4 dress shoes and fashion footwear. Corporate oxfords, smart-fabric heels, fashion sneakers from popular brands, loafers, formal boots, designer sandals. What people wear to the office, to dinner, to social events. Clean, polished, status-signaling. Prices Φ60-Φ300.`
  },
  {
    label: 'Footwear Batch 9 — Tier 4-5 Luxury Footwear',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: Tier 4-5 luxury and designer footwear. Handcrafted artisan boots, real-leather shoes (real leather is rare/expensive), bespoke smart-shoes with full biometric integration, climate-adaptive luxury sneakers, Spire-exclusive fashion pieces. Status symbols. Prices Φ200-Φ2000.`
  },
  {
    label: 'Footwear Batch 10 — Specialty/Tactical Footwear',
    count: 10,
    prompt: `Generate {count} FOOTWEAR items: specialty and tactical footwear across tiers. Armored combat boots (Tier 3-4), stealth infiltration shoes, mag-lock zero-G boots, hazmat boots, courier running shoes with speed-assist, bouncer stompers, street racer grip-shoes, rooftop runner treads. For people with dangerous or unusual jobs. Prices Φ50-Φ500.`
  },
  // PANTS/LEGWEAR batches (100 total, 10 batches of 10)
  {
    label: 'Pants Batch 1 — Tier 1 Shelf Legwear',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 1 Shelf pants and legwear. Patched cargo pants, salvaged denim, duct-tape reinforced work pants, scrap-fabric patchwork pants, threadbare synth-cotton jeans. Worn, repaired, functional. The kind of pants you find in Shelf thrift bins or trade for. Prices Φ3-Φ20.`
  },
  {
    label: 'Pants Batch 2 — Tier 1-2 Workwear Pants',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 1-2 working class pants. Cargo pants with tool loops, warehouse work pants, mechanic's grease-resistant trousers, dock-worker waterproof pants, construction reinforced-knee pants. Durable, stained, built to last. Prices Φ10-Φ50.`
  },
  {
    label: 'Pants Batch 3 — Tier 2-3 Everyday Jeans/Pants',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 2-3 everyday jeans and pants. Self-repairing jeans (basic nano-thread), regular denim, synth-cotton chinos, joggers, sweatpants, comfortable daily-wear pants. What most people in GLMZ wear every day. Normal clothes. Prices Φ15-Φ75.`
  },
  {
    label: 'Pants Batch 4 — Tier 2-3 Tactical/Cargo',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 2-3 tactical and cargo pants. Multi-pocket tactical pants, runner's cargo pants (lightweight, many hidden pockets), courier pants with reinforced knees, security guard duty pants, street-smart pants with concealed carry pockets. Functional and practical with an edge. Prices Φ25-Φ100.`
  },
  {
    label: 'Pants Batch 5 — Aug-Compatible Pants',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: augmentation-compatible pants and legwear across Tier 2-4. Pants designed for chrome legs with access panels, adaptive-fit trousers for variable-geometry aug limbs, jeans with reinforced inner seams for hydraulic leg augments, cargo pants with maintenance zips for leg chrome. All aug_compatible: true. Prices Φ40-Φ200.`
  },
  {
    label: 'Pants Batch 6 — Gene-Compatible Legwear',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: geneware-compatible legwear across Tier 2-4. Pants with tail ports, trousers for digitigrade legs, skirts designed around unusual lower-body geneware, leggings that accommodate bioluminescent skin (transparent panels), shorts for people with fur/scales. All gene_compatible: true. Prices Φ35-Φ180.`
  },
  {
    label: 'Pants Batch 7 — Tier 3-4 Smart Pants',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 3-4 smart-fabric pants. Temperature-regulating trousers, self-cleaning dress pants, stealth-material pants (scanner-resistant), health-monitoring leggings, climate-adaptive hiking pants, wrinkle-proof smart-weave slacks. Professional and tech-forward. Prices Φ75-Φ250.`
  },
  {
    label: 'Pants Batch 8 — Tier 3-4 Corporate/Dress',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 3-4 corporate and dress pants. Suit trousers, corporate uniform pants, business-casual slacks, smart-fabric pencil skirts, formal kilts (fashion statement in 2200), dress shorts for warm-weather offices. What people wear to corponation offices. Prices Φ60-Φ300.`
  },
  {
    label: 'Pants Batch 9 — Tier 4-5 Luxury Legwear',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: Tier 4-5 luxury pants and legwear. Handcrafted denim from real cotton (rare), bespoke smart-fabric trousers, designer running tights, Spire-exclusive fashion pants, artisan-woven kilts. Real materials, perfect tailoring, social currency. Prices Φ200-Φ1500.`
  },
  {
    label: 'Pants Batch 10 — Specialty/Armored Legwear',
    count: 10,
    prompt: `Generate {count} PANTS/LEGWEAR items: specialty and armored legwear across tiers. Armored leggings, thermal survival pants, hazmat trousers, courier speed-pants with drag reduction, club-wear light-up pants, thermal insulated Shelf-winter pants, stealth infiltration pants, rooftop runner shorts. For people with dangerous or unusual needs. Prices Φ30-Φ500.`
  },
];

// ── Main ──
async function main() {
  const existingNames = getExistingNames();
  console.log(`Found ${existingNames.length} existing apparel items.`);

  let totalSaved = 0;
  let totalSkipped = 0;
  let totalErrors = 0;

  for (let i = 0; i < BATCHES.length; i++) {
    const batch = BATCHES[i];
    console.log(`\n[${i + 1}/${BATCHES.length}] ${batch.label} (${batch.count} items)`);

    const existingList = existingNames.length > 0
      ? `\n\nDo NOT duplicate these existing items:\n${existingNames.slice(-50).join(', ')}`
      : '';

    const prompt = batch.prompt.replace('{count}', batch.count) + existingList;

    try {
      const response = await callClaude(
        WORLD_CONTEXT + '\n\n' + SCHEMA_INSTRUCTION,
        prompt,
        8192
      );

      const items = parseJsonArray(response);
      console.log(`  Received ${items.length} items from API`);

      for (const item of items) {
        // Ensure required fields
        if (!item.id || item.id.length !== 32) item.id = randomHex(32);
        if (!item.type) item.type = 'apparel';
        if (!item.tags) item.tags = [];
        if (!item.tags.includes('apparel')) item.tags.unshift('apparel');

        if (saveItem(item)) {
          existingNames.push(item.name);
          totalSaved++;
          console.log(`    SAVED: ${item.name}`);
        } else {
          totalSkipped++;
        }
      }
    } catch (err) {
      console.error(`  ERROR in batch: ${err.message}`);
      totalErrors++;
    }

    if (i < BATCHES.length - 1) {
      console.log(`  Waiting ${WAIT_MS}ms...`);
      await sleep(WAIT_MS);
    }
  }

  // Final count
  const finalFiles = fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.json'));
  console.log(`\n========================================`);
  console.log(`DONE. Saved: ${totalSaved}, Skipped: ${totalSkipped}, Errors: ${totalErrors}`);
  console.log(`Total files in apparel/: ${finalFiles.length}`);
  console.log(`========================================`);
}

main().catch(err => {
  console.error('Fatal error:', err);
  process.exit(1);
});
